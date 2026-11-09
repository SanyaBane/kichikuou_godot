using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// The camera's scroll to a target drawn every frame (owner, 2026-11-09; docs/ui/command-screen.md, "The scroll drawn every
// frame"): M.ADV op 1 moves the view in steps held 80 ms each, so in each step's pause the screen draws the view gliding from
// the step before's to this one's (MapScrollGlide), over the stage's, which stays the original's. Every device, both
// interfaces, outside a battle and in one; not while the message skip runs or pauses take no time. Upright on a phone the
// scroll is the original's picture at the width too: our map (PortraitMapView) is up only while the screen shows the
// map wait's map (_mapStrip), never during a scroll.
public partial class GameScreen
{
    private readonly MapScrollGlide _scrollGlide = new();

    /// <summary>A script event on its way to the presenter: a scroll's start, its steps' pauses, or its end.</summary>
    private void ObserveMapScroll(ScriptEvent scriptEvent)
    {
        if (_presenter.State is { } state)
            _scrollGlide.Observe(scriptEvent, state, Now);
    }

    /// <summary>Once a frame, before the screen is composed: the scroll's view for this moment of its step's pause.</summary>
    private void GlideMapScroll(double delta)
    {
        bool still = SkippingScene || _skip?.IsSkipping == true;
        _presenter.ShowScrollView(still ? null : _scrollGlide.Corner(Now, delta, _options.Fast ? 0 : PauseTick));
    }
}
