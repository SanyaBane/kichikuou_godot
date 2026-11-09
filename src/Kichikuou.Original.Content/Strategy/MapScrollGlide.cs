using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// The camera's scroll to a target drawn every frame (owner, 2026-11-09; docs/ui/command-screen.md, "The scroll drawn every
/// frame"). <c>M.ADV</c> op 1 moves the view in steps of about ten pixels, each drawn and then held for <c>Y 3, 5</c>
/// (80 ms), so the picture jumped some twelve times a second. Nothing reads the input between its steps, so the whole way
/// is known when the scroll starts (<see cref="OriginalScriptContext.MapScrollCameras"/>, the script's own arithmetic): in
/// step k's pause the screen draws the view gliding from step k−1's to step k's (the first from the view before the
/// scroll), at step k's when the pause is over — the same way in the same time, a frame at a time. Drawn one step behind,
/// so it never shows a view the script has not shown. Ours, no rule changes: the script's camera, its scratch values, its
/// pauses and its stage stay the original's; the screen draws another view of the map over the stage only while the
/// script waits in a step's pause. It follows the script by its events (<see cref="Observe"/>) and its pauses' ends
/// (<see cref="Waited"/>).
/// </summary>
public sealed class MapScrollGlide
{
    /// <summary>The views' corners the scroll goes through: [0] the one before it, [k] step k's; null outside a scroll.</summary>
    private IReadOnlyList<(int X, int Y)>? _corners;

    /// <summary>How many of the scroll's steps have been drawn (their pauses begun).</summary>
    private int _drawn;

    /// <summary>Whether the script waits in the pause of the step last drawn.</summary>
    private bool _waiting;

    private int _ticks;
    private double _since;

    /// <summary>Whether the script waits in a step's pause of a scroll: <see cref="Corner"/> may draw.</summary>
    public bool Gliding => _corners is not null && _waiting;

    /// <summary>
    /// An event of the script, <paramref name="now"/> in seconds: <c>M.ADV</c> op 1 starts a scroll (its way from the state
    /// as the call finds it), a pause that shows the scroll's next step starts that step's glide, anything else ends it.
    /// </summary>
    public void Observe(ScriptEvent scriptEvent, GameState state, double now)
    {
        _waiting = false;
        switch (scriptEvent)
        {
            case MapEvent { Mode.Value: 1 }:
            {
                var cameras = OriginalScriptContext.MapScrollCameras(state.MapCamX, state.MapCamY, state.MapTargetX, state.MapTargetY);
                _corners = cameras.Count == 0 ? null : [CornerOf(state.MapCamX, state.MapCamY), .. cameras.Select(c => CornerOf(c.X, c.Y))];
                _drawn = 0;
                break;
            }
            case PauseEvent pause when _corners is { } corners && _drawn + 1 < corners.Count
                    && CornerOf(state.MapCamX, state.MapCamY) == corners[_drawn + 1]:
                _drawn++;                                               // M.ADV:99-113: the step drawn, then its pause
                (_waiting, _ticks, _since) = (true, pause.Ticks.Value, now);
                break;
            default:
                _corners = null;                                        // the scroll is over, or this pause is not its step
                break;
        }
    }

    /// <summary>The script's pause is over, early or not: its step's view stands until the next step's pause.</summary>
    public void Waited() => _waiting = false;

    /// <summary>
    /// The view's corner to draw now instead of the step's, or null to leave the step's view (outside a scroll's pause, or
    /// once its glide has arrived): the way from the step before's view to this one's that the time since the pause began
    /// (<paramref name="now"/>) has gone of the pause as the screen waits it (<see cref="PauseSeconds"/>; a tick
    /// <paramref name="tickSeconds"/>, 0 when pauses take no time; a frame <paramref name="frameSeconds"/>).
    /// </summary>
    public (int X, int Y)? Corner(double now, double frameSeconds, double tickSeconds)
    {
        if (_corners is not { } corners || !_waiting)
            return null;
        double seconds = PauseSeconds(_ticks * tickSeconds, frameSeconds);
        if (seconds <= 0)
            return null;
        double gone = Math.Clamp((now - _since) / seconds, 0, 1);
        var (from, to) = (corners[_drawn - 1], corners[_drawn]);
        (int X, int Y) at = (Between(from.X, to.X, gone), Between(from.Y, to.Y, gone));
        return at == to ? null : at;
    }

    /// <summary>
    /// How long the screen waits a pause of <paramref name="pause"/> seconds polled once a frame of
    /// <paramref name="frame"/> seconds: whole frames, ending at the frame nearest its end, one at least (what
    /// <c>GameScreen.KeyWaitAsync</c> waits) — 80 ms are five frames at 60 a second, two at 30.
    /// </summary>
    public static double PauseSeconds(double pause, double frame)
    {
        if (pause <= 0 || frame <= 0)
            return Math.Max(0, pause);
        double frames = Math.Max(1, Math.Ceiling(pause / frame - 0.5 - 1e-9));
        return frames * frame;
    }

    /// <summary>The view's corner for a camera (its centre), <c>M.ADV:3-10</c>.</summary>
    private static (int X, int Y) CornerOf(Sys3Int camX, Sys3Int camY)
    {
        var (x, y) = OriginalScriptContext.MapViewCornerOf(camX, camY);
        return (x.Value, y.Value);
    }

    private static int Between(int from, int to, double gone) => (int)Math.Round(from + (to - from) * gone);
}
