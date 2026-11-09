using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// The map's drag drawn every frame (owner, 2026-11-09; docs/ui/command-screen.md, "The drag drawn every frame"). The
/// script drags the map in steps (<c>WAO_MAP.ADV</c> op 1: a pointer read, the view copied at the new camera, then
/// <c>Y 3, Ui_ScrollWait</c> — three frames or so a step at 60 a second), so the picture moved only with them, the pointer's
/// way between two steps in one jump. Between its steps the screen shows the view the script's next step will show: while
/// the left button drags, the one for the pointer as the next read would take it; while the view coasts, a part of the way
/// from the script's camera to its next step's, by the time since the step. Ours, no rule changes: the script's camera, its
/// reads and its steps stay the original's (the step's arithmetic is the script's own,
/// <see cref="OriginalScriptContext.MapDragStep"/>), and each step lands where the picture already is. It follows the script
/// by its events (<see cref="Observe"/>) and its pointer reads (<see cref="Reading"/>, <see cref="Read"/>).
/// </summary>
public sealed class MapDragPreview
{
    private enum Phase
    {
        /// <summary>Not in <c>WAO_MAP</c> op 1, or nothing to draw ahead of it.</summary>
        None,

        /// <summary>Its next read is the drag's anchor (<c>WAO_MAP.ADV:6-19</c>: no anchor from <c>COMMAND</c>).</summary>
        Anchoring,

        /// <summary>The left button held: a read that moved is a step (<c>:20-44</c>).</summary>
        Dragging,

        /// <summary>Released (or another button): a read with nothing held is a step of the coasting (<c>:45-77</c>).</summary>
        Coasting,

        /// <summary>A press stopped the coasting: its next read is the new drag's anchor (<c>:53-55</c>, <c>:6-19</c>).</summary>
        Rearming,
    }

    private Phase _phase;
    private bool _pausing;
    private double _stepAt;
    private (int X, int Y)? _lastStep;

    /// <summary>
    /// Whether the picture may stand ahead of the script's camera: while the view coasts, and after a press stops it until
    /// the new drag's anchor is read. A press then is read against the script's camera, not the picture.
    /// </summary>
    public bool Ahead => _phase is Phase.Coasting or Phase.Rearming;

    /// <summary>An event of the script, <paramref name="now"/> in seconds: the drag starts, a step, its pause, or anything else, which ends it.</summary>
    public void Observe(ScriptEvent scriptEvent, GameState state, double now)
    {
        switch (scriptEvent)
        {
            case WaoMapEvent { Mode.Value: 1 }:
                _phase = state.Ui_DragPending != 0 ? Phase.Dragging : Phase.Anchoring;
                _pausing = false;
                _lastStep = null;
                break;
            case EngineEvent { Command: "CC" } copy when _phase is Phase.Dragging or Phase.Coasting && IsStep(copy.Args):
                _stepAt = now;
                _lastStep = (copy.Args[0].Value - 1280, copy.Args[1].Value);
                break;
            case PauseEvent when _phase is Phase.Dragging or Phase.Coasting:
                _pausing = true;                                        // :40, 69: the anchor moves, the coasting slows after it
                break;
            default:
                _phase = Phase.None;
                break;
        }
    }

    /// <summary><c>CC MapCamX + 1280, MapCamY, 640, 400, 0, 0</c>: a step's view onto the screen (<c>WAO_MAP.ADV:39, 68</c>).</summary>
    public static bool IsStep(IReadOnlyList<Sys3Int> args) =>
        args.Count == 6 && args[0].Value >= 1280 && args[2] == 640 && args[3] == 400 && args[4] == 0 && args[5] == 0;

    /// <summary>The script is about to read the pointer (<c>IM</c>): the pause after a step, if any, is over.</summary>
    public void Reading() => _pausing = false;

    /// <summary>The script read the pointer with <paramref name="buttons"/> held, <paramref name="state"/> as it is at the read.</summary>
    public void Read(int buttons, GameState state)
    {
        _pausing = false;
        _phase = _phase switch
        {
            Phase.Anchoring or Phase.Rearming => buttons == 16 ? Phase.Dragging : Phase.None,
            Phase.Dragging => buttons == 16 ? Phase.Dragging : Phase.Coasting,
            Phase.Coasting => buttons != 0 ? Phase.Rearming
                : OriginalScriptContext.MapCoastStopped(state.Tmp0021, state.Tmp0022, state.Tmp0023, state.Tmp0024) ? Phase.None
                : Phase.Coasting,
            _ => Phase.None,
        };
    }

    /// <summary>
    /// The view's corner to show now, or null to leave the picture as the script drew it; <paramref name="read"/> and
    /// <paramref name="buttons"/> are the pointer as the script's next read would take it (the view's coordinates). While
    /// the drag runs, the corner the script's next step shows for that read — none while another button is held, which
    /// puts the camera back where the drag began (<c>WAO_MAP.ADV:25-28</c>), drawing nothing. While the view coasts, the way
    /// from the script's camera to its next step's that the time since the last step (<paramref name="now"/>) has gone of a
    /// step's <paramref name="stepSeconds"/> (0: the whole way); a press stops the coasting at the view the script's last
    /// step showed, and so does it until the new drag's anchor is read.
    /// </summary>
    public (int X, int Y)? Corner(GameState state, (int X, int Y) read, int buttons, double now, double stepSeconds)
    {
        (int X, int Y) camera = (state.MapCamX.Value, state.MapCamY.Value);
        switch (_phase)
        {
            case Phase.Dragging when buttons is 0 or 16:
            {
                // In the pause (:40) the anchor is still the read before; after it, the pointer that read (:41-42).
                var (fromX, fromY) = _pausing ? (state.Ui_MouseX, state.Ui_MouseY) : (state.Ui_DragX, state.Ui_DragY);
                var (x, y) = OriginalScriptContext.MapDragStep(state.MapCamX, state.MapCamY, fromX, fromY, (Sys3Int)read.X, (Sys3Int)read.Y);
                return (x.Value, y.Value);
            }
            case Phase.Coasting when buttons == 0:
            {
                Sys3Int t21 = state.Tmp0021, t22 = state.Tmp0022, t23 = state.Tmp0023, t24 = state.Tmp0024;
                if (_pausing)
                {
                    // In the pause the differences are still the step's; they slow down after it (:72-76).
                    t21 = OriginalScriptContext.MapCoastDecay(t21, state.Tmp0028);
                    t22 = OriginalScriptContext.MapCoastDecay(t22, state.Tmp0028);
                    t23 = OriginalScriptContext.MapCoastDecay(t23, state.Tmp0028);
                    t24 = OriginalScriptContext.MapCoastDecay(t24, state.Tmp0028);
                }
                if (OriginalScriptContext.MapCoastStopped(t21, t22, t23, t24))
                    return null;                                        // the last step's view stays
                var (x, y) = OriginalScriptContext.MapScrolled(state.MapCamX, state.MapCamY, t21, t22, t23, t24);
                double gone = stepSeconds > 0 ? Math.Clamp((now - _stepAt) / stepSeconds, 0, 1) : 1;
                return (Between(camera.X, x.Value, gone), Between(camera.Y, y.Value, gone));
            }
            case Phase.Coasting or Phase.Rearming:
                return _lastStep;
            default:
                return null;
        }
    }

    private static int Between(int from, int to, double gone) => (int)Math.Round(from + (to - from) * gone);
}
