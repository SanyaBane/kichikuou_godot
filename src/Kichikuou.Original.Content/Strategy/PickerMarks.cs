using System.Diagnostics;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// Ours (owner's decision 30, docs/ui/talk-marks.md §6, §8 step 8): the marks our Harem and Subordinates screens show — for
/// every girl <c>Sﾊｰﾚﾑ</c> offers and every unit <c>Sﾌﾞｶ</c> lets talk, whether her visit or its talk can bring anything
/// new. They are computed by the dry run (<see cref="SceneMarks"/>) once each time the picker opens
/// (<see cref="OnPicker"/>), from the state at that call — the picker's own state, <c>Ev_Entry</c> and the command's writes
/// included —, and kept until it opens again; never in the draw loop. Each mark's tree is kept, so a change of "New scenes
/// first" while a picker is open reads the marks again without a run (<see cref="SetNewScenesFirst"/>). It decides nothing
/// and changes nothing: the dry run runs on copies of the state.
/// </summary>
public sealed class PickerMarks(SceneMarks marks)
{
    private static readonly IReadOnlyDictionary<int, SceneMark> None = new Dictionary<int, SceneMark>();

    private Dictionary<int, SceneMarkReport> _visits = [], _talks = [];
    private readonly Queue<MarkTarget> _warm = new();
    private bool _warmQueued;

    /// <summary>The marks of the girls on offer at the harem's last opening, by her number.</summary>
    public IReadOnlyDictionary<int, SceneMark> Visits { get; private set; } = None;

    /// <summary>
    /// The marks of the units able to talk at the Subordinates screen's last opening, by unit; none when the week's talk
    /// was spent then (<see cref="SubordinatesScreen.TalkOpen"/>: nothing is run). The screen shows them through
    /// <see cref="SubordinatesScreen.TalkMarks"/>, which applies the rule again as it draws.
    /// </summary>
    public IReadOnlyDictionary<int, SceneMark> Talks { get; private set; } = None;

    /// <summary>Whether the marks are read with "New scenes first" on.</summary>
    public bool NewScenesFirst { get; private set; }

    /// <summary>The time the last computation took, in milliseconds.</summary>
    public double LastMilliseconds { get; private set; }

    /// <summary>The talks or visits whose dry run failed at the last computation (they get no mark), with the error.</summary>
    public IReadOnlyList<string> Failures { get; private set; } = [];

    /// <summary>
    /// A picker's event: when <c>Sﾊｰﾚﾑ</c> opens, the marks of the girls it offers; when <c>Sﾌﾞｶ</c> opens, those of the units
    /// it lets talk (none while the week's talk is spent). Returns whether marks were computed.
    /// </summary>
    public bool OnPicker(PickerScreenEvent e, GameState state, bool newScenesFirst)
    {
        if (!e.Open || e.Kind is not (PickerKind.Harem or PickerKind.Subordinates))
            return false;
        var clock = Stopwatch.StartNew();
        var failures = new List<string>();
        NewScenesFirst = newScenesFirst;
        if (e.Kind == PickerKind.Harem)
        {
            _visits = Run(state, SceneMarks.OfferedGirls(state).Select(MarkTarget.Visit), failures);
            Visits = Read(_visits);
        }
        else
        {
            _talks = SubordinatesScreen.TalkOpen(state)
                ? Run(state, SceneMarks.TalkingUnits(state).Select(MarkTarget.Talk), failures)
                : [];
            Talks = Read(_talks);
        }
        Failures = failures;
        LastMilliseconds = clock.Elapsed.TotalMilliseconds;
        return true;
    }

    /// <summary>"New scenes first" switched: the kept trees read with its rules (no run).</summary>
    public void SetNewScenesFirst(bool on)
    {
        if (on == NewScenesFirst)
            return;
        NewScenesFirst = on;
        Visits = Read(_visits);
        Talks = Read(_talks);
    }

    /// <summary>
    /// The scenes read ahead (§9, step 8): one talk or visit of <paramref name="state"/> run per call — the units able to
    /// act and the girls on offer then, queued at the first call —, its mark dropped; the first picker then opens without
    /// reading the scenes. Returns whether any is left. Errors are dropped: the picker's own run reports them.
    /// </summary>
    public bool Warm(GameState state)
    {
        if (!_warmQueued)
        {
            _warmQueued = true;
            foreach (int unit in SceneMarks.TalkingUnits(state))
                _warm.Enqueue(MarkTarget.Talk(unit));
            foreach (int girl in SceneMarks.OfferedGirls(state))
                _warm.Enqueue(MarkTarget.Visit(girl));
        }
        if (!_warm.TryDequeue(out var target))
            return false;
        try
        {
            marks.Explore(state, target);
        }
        catch (Exception)
        {
            // the picker's own run reports it
        }
        return _warm.Count > 0;
    }

    /// <summary>Whether the scenes are still to be read ahead (<see cref="Warm"/>).</summary>
    public bool Warming => !_warmQueued || _warm.Count > 0;

    /// <summary>The pill's or the slot's text: NEW, ???, or null when nothing new can happen (or no mark is known).</summary>
    public static string? Label(SceneMark? mark) => mark switch
    {
        SceneMark.New => "NEW",
        SceneMark.Maybe => "???",
        _ => null,
    };

    private Dictionary<int, SceneMarkReport> Run(GameState state, IEnumerable<MarkTarget> targets, List<string> failures)
    {
        var reports = new Dictionary<int, SceneMarkReport>();
        foreach (var target in targets)
        {
            try
            {
                reports[target.Number] = marks.Mark(state, target, NewScenesFirst);
            }
            catch (Exception ex)
            {
                failures.Add($"{target}: {ex.GetType().Name}: {ex.Message}");
            }
        }
        return reports;
    }

    private IReadOnlyDictionary<int, SceneMark> Read(Dictionary<int, SceneMarkReport> reports) =>
        reports.ToDictionary(r => r.Key, r => r.Value.Tree.Mark(SceneMarks.Rules(NewScenesFirst)));
}
