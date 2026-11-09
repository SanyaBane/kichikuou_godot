using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

// The map's drag drawn every frame (owner, 2026-11-09; docs/ui/command-screen.md, "The drag drawn every frame"): the script
// drags the map in steps a few frames apart (WAO_MAP.ADV op 1), so between them the screen draws the view its next step will
// show — at the pointer while the button drags, on the way to the next step while the view coasts (MapDragPreview). The
// script's camera, reads and steps stay the original's; upright on a phone the map is ours already (PortraitMapView).
public partial class GameScreen
{
    private readonly MapDragPreview _dragPreview = new();

    /// <summary>One tick of <c>Y 3</c> in seconds (<see cref="PauseAsync"/>).</summary>
    private const double PauseTick = 0.016;

    private static double Now => Time.GetTicksUsec() / 1e6;

    /// <summary>A script event on its way to the presenter: the drag's start, its steps and their pauses, or its end.</summary>
    private void ObserveMapDrag(ScriptEvent scriptEvent)
    {
        if (_presenter.State is { } state)
            _dragPreview.Observe(scriptEvent, state, Now);
    }

    /// <summary>A pointer read handed to the script: the drag's phase follows it.</summary>
    private void ObserveMapDragRead(MouseState read)
    {
        if (_presenter.State is { } state)
            _dragPreview.Read(read.Button.Value, state);
    }

    /// <summary>
    /// Once a frame, before the screen is composed: the view the script's next step shows, for the player's pointer as the
    /// next read would take it, drawn now. Not for unattended play's own pointer, nor upright on a phone.
    /// </summary>
    private void PreviewMapDrag(double delta)
    {
        if (_presenter.State is not { } state || PhonePortrait || SkippingScene || _options.Auto && !_options.AutoMapNorth)
            return;
        ushort buttons = _input.PeekMouse();
        var read = (_input.Position.X, ScriptY(_input.Position.Y, buttons, peek: true));
        if (_dragPreview.Corner(state, read, buttons, Now, StepSeconds(state, delta)) is { } corner)
            _presenter.ShowMapView(corner);
    }

    /// <summary>
    /// How long a step of the coasting takes: its pause (<c>Y 3, Ui_ScrollWait</c>, frames until its time is gone, one at
    /// least) and the frame its read waits for.
    /// </summary>
    private static double StepSeconds(GameState state, double frame)
    {
        if (frame <= 0)
            return 0;
        double pause = state.Ui_ScrollWait.Value * PauseTick;
        double frames = Math.Max(1, Math.Ceiling(pause / frame - 1e-6));
        return (frames + 1) * frame;
    }
}
