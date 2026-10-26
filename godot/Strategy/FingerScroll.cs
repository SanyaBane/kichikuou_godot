using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// A finger scrolling a list in the touch mode (owner, 2026-10-26; docs/ui/touch.md, "The lists scrolled by a finger"): a
/// press that wanders past <see cref="Slop"/> scrolls, the list following the finger from where it passed the slop (so it
/// does not jump by it); risen at <see cref="FlingStart"/> or more (measured over its last <see cref="FlingWindow"/>) the
/// scroll goes on as a flick that slows; a press while a flick goes on stops it and scrolls at once, never tapping. The
/// list keeps its own scroll values; this keeps the finger's, in its control's pixels.
/// </summary>
internal sealed class FingerScroll
{
    /// <summary>How far a press may wander (its control's pixels) and still tap or long-press: further, it scrolls.</summary>
    public const float Slop = 12;

    /// <summary>The moves a flick's speed is measured over: those of the last this many seconds before the release.</summary>
    private const double FlingWindow = 0.1;

    /// <summary>The slowest flick that goes on after the release, and the speed it stops at (pixels a second).</summary>
    private const float FlingStart = 150, FlingStop = 20;

    /// <summary>How fast a flick slows: the share of its speed it keeps after a second is e^-FlingDecay.</summary>
    private const float FlingDecay = 4;

    private readonly Queue<(ulong Usec, Vector2 At)> _moves = new();
    private Vector2 _fling;

    /// <summary>Whether a press is down and not released yet.</summary>
    public bool Down { get; private set; }

    /// <summary>Whether the press scrolls: it wandered past the slop, or it stopped a flick.</summary>
    public bool Dragging { get; private set; }

    /// <summary>Where the scroll is measured from: the press, then the place it passed the slop.</summary>
    public Vector2 Origin { get; private set; }

    /// <summary>Whether a flick goes on.</summary>
    public bool Flinging => _fling != Vector2.Zero;

    /// <summary>A press at <paramref name="at"/>; returns whether it stopped a flick (it then scrolls from here, no tap).</summary>
    public bool Press(Vector2 at)
    {
        bool stopped = Flinging;
        (_fling, Down, Dragging, Origin) = (Vector2.Zero, true, stopped, at);
        _moves.Clear();
        Moved(at);
        return stopped;
    }

    /// <summary>The pressed finger moved to <paramref name="at"/>; returns whether it starts scrolling with this move.</summary>
    public bool Move(Vector2 at)
    {
        if (!Down)
            return false;
        Moved(at);
        if (Dragging || at.DistanceTo(Origin) <= Slop)
            return false;
        (Dragging, Origin) = (true, at);
        return true;
    }

    /// <summary>
    /// The release at <paramref name="at"/>: a scroll's flick goes on when the finger rose fast enough (one that stood
    /// still before it rose flicks nothing). Returns whether the press scrolled — no tap then.
    /// </summary>
    public bool Release(Vector2 at)
    {
        bool scrolled = Down && Dragging;
        Down = Dragging = false;
        if (!scrolled)
            return false;
        Moved(at);
        var (usec, from) = _moves.Peek();
        double seconds = (Time.GetTicksUsec() - usec) / 1e6;
        var speed = seconds > 0.02 ? (at - from) / (float)seconds : Vector2.Zero;
        _fling = speed.Length() >= FlingStart ? speed : Vector2.Zero;        return true;
    }

    /// <summary>A frame of the flick, <paramref name="seconds"/> long: how far the finger would have moved, the flick slowing.</summary>
    public Vector2 Step(double seconds)
    {
        var move = _fling * (float)seconds;
        _fling *= Mathf.Exp(-FlingDecay * (float)seconds);
        if (_fling.Length() < FlingStop)
            _fling = Vector2.Zero;
        return move;
    }

    /// <summary>The flick stops across (its list is at that end, or does not scroll that way).</summary>
    public void StopX() => _fling.X = 0;

    /// <summary>The flick stops down or up (its list is at that end, or does not scroll that way).</summary>
    public void StopY() => _fling.Y = 0;

    /// <summary>The press and a flick end (the list hid or stopped taking the finger).</summary>
    public void Cancel()
    {
        Down = Dragging = false;
        _fling = Vector2.Zero;
    }

    /// <summary>A move to <paramref name="at"/>, kept while it is younger than <see cref="FlingWindow"/>.</summary>
    private void Moved(Vector2 at)
    {
        ulong now = Time.GetTicksUsec();
        _moves.Enqueue((now, at));
        while (_moves.Count > 1 && now - _moves.Peek().Usec > FlingWindow * 1e6)
            _moves.Dequeue();
    }
}
