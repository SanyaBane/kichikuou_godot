using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// A check for the tests (<see cref="GamePresenter.Audit"/>): while a screen is drawn by hand (a picker, All Data)
/// the presenter skips the file's drawing commands but the copies and fills that keep a page off the screen. A
/// reference stage runs every command, so the audit knows what each skipped command would have written; where that
/// lies off the screen and nothing the presenter ran since has covered it, the page is stale. A copy the presenter
/// runs that reads a stale page shows what an older page left there — Subordinates came back over one after a unit's
/// troop menu (2026-10-14). Each such read is a <see cref="Finding"/>, and so is a <c>G</c> the presenter draws at
/// another place than the reference (a skipped <c>J0</c>/<c>J1</c> moved it). Writes to the stage that do not go
/// through the file's drawing commands (the map, <c>WAO_SUB</c>'s pictures, the battle's own drawing) are not seen.
/// </summary>
public sealed class PageAudit
{
    private readonly EngineStage _reference;
    private readonly Recorder _referenceRects = new();
    private readonly List<(int X, int Y, int W, int H)> _ourRects = [];
    private readonly int[] _stale = new int[Width * Height];         // the origin's number + 1 per script pixel, 0 = clean
    private readonly List<Origin> _origins = [];
    private bool _inCommand;
    private int _sequence;

    private const int Width = 3200, Height = 800;

    /// <param name="cgs">The CGs, for the reference's <c>G</c>.</param>
    public PageAudit(CgLibrary cgs) => _reference = new EngineStage(new Surface(), cgs, _referenceRects);

    /// <summary>A skipped command whose page was left stale: the command, its arguments and the screen drawn by hand then.</summary>
    public sealed record Origin(int Sequence, string Command, string Args, string Screen);

    /// <summary>What went wrong: <c>stale read</c> or <c>G moved</c>; the command the presenter ran, and for a read the stale page's origin.</summary>
    public sealed record Finding(string Kind, int Sequence, string Command, string Args, string Context, Origin? Origin, int Pixels);

    /// <summary>The findings so far, in order.</summary>
    public List<Finding> Findings { get; } = [];

    /// <summary>Set by a test: where the run is (a capture, a seed), copied into each finding.</summary>
    public string Context { get; set; } = "";

    /// <summary>Before a drawing command: <paramref name="runs"/> whether the presenter runs it; <paramref name="screen"/> the screen drawn by hand, if any.</summary>
    internal void Before(string command, IReadOnlyList<Sys3Int> args, bool runs, string? screen)
    {
        _sequence++;
        int A(int i) => i < args.Count ? args[i].Value : 0;
        string text = string.Join(", ", args.Select(a => a.Value));
        if (runs && command is "CC" or "CE" or "CS" or "CM")
            CheckRead(command, text, A(0), A(1), A(2), A(3));
        _referenceRects.Rects.Clear();
        try
        {
            _reference.Draw(command, args);
        }
        catch (Exception)
        {
            // A CG the reference cannot draw: nothing is known of what it wrote.
            _referenceRects.Rects.Clear();
        }
        if (!runs)
        {
            Origin? origin = null;
            foreach (var r in _referenceRects.Rects)
            {
                origin ??= Add(new Origin(_sequence, command, text, screen ?? ""));
                Mark(r, _origins.Count, offScreenOnly: true);
            }
            return;
        }
        _ourRects.Clear();
        _inCommand = true;
        _pending = (command, text);
    }

    private (string Command, string Args) _pending;

    /// <summary>After a command the presenter ran: what it wrote is no longer stale; a <c>G</c> elsewhere than the reference's is a finding.</summary>
    internal void After()
    {
        if (!_inCommand)
            return;
        _inCommand = false;
        foreach (var r in _ourRects)
            Mark(r, 0, offScreenOnly: false);
        if (_pending.Command == "G" && _ourRects.Count > 0 && _referenceRects.Rects.Count > 0 && _ourRects[0] != _referenceRects.Rects[0])
            Findings.Add(new Finding("G moved", _sequence, "G", _pending.Args,
                $"{Context}; drawn at {_ourRects[0]}, the reference at {_referenceRects.Rects[0]}", null, 0));
    }

    /// <summary>A rectangle the presenter's stage drew while running a command.</summary>
    internal void Drawn(int x, int y, int w, int h)
    {
        if (_inCommand)
            _ourRects.Add((x, y, w, h));
    }

    private Origin Add(Origin origin)
    {
        _origins.Add(origin);
        return origin;
    }

    private void CheckRead(string command, string args, int x, int y, int w, int h)
    {
        int first = 0, pixels = 0;
        foreach (int i in Pixels(x, y, w, h))
        {
            if (_stale[i] == 0)
                continue;
            pixels++;
            if (first == 0)
                first = _stale[i];
        }
        if (pixels > 0)
            Findings.Add(new Finding("stale read", _sequence, command, args, Context, _origins[first - 1], pixels));
    }

    private void Mark((int X, int Y, int W, int H) r, int value, bool offScreenOnly)
    {
        foreach (int i in Pixels(r.X, r.Y, r.W, r.H))
        {
            if (offScreenOnly && i % Width < Surface.ScreenWidth && i / Width < Surface.ScreenHeight)
                continue;
            _stale[i] = value;
        }
    }

    private static IEnumerable<int> Pixels(int x, int y, int w, int h)
    {
        int x0 = Math.Max(0, x), y0 = Math.Max(0, y), x1 = Math.Min(Width, x + w), y1 = Math.Min(Height, y + h);
        for (int py = y0; py < y1; py++)
            for (int px = x0; px < x1; px++)
                yield return py * Width + px;
    }

    /// <summary>The reference stage's observer: the rectangles each command writes.</summary>
    private sealed class Recorder : IStageObserver
    {
        public List<(int X, int Y, int W, int H)> Rects { get; } = [];

        public void BeforeCopy(int sourceX, int width)
        {
        }

        public void Drawn(int x, int y, int width, int height, bool opaque) => Rects.Add((x, y, width, height));

        public void CgLoaded(int number)
        {
        }
    }
}
