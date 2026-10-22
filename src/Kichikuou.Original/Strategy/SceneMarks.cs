using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Strategy;

/// <summary>What the marks are on: a talk (<c>Cﾌﾞｶ</c>) or a visit (<c>Cﾊｰﾚﾑ</c>).</summary>
public enum MarkTargetKind
{
    /// <summary>The talk of a unit (Subordinates → Talk).</summary>
    Talk,

    /// <summary>The visit of a girl (the harem).</summary>
    Visit,
}

/// <summary>A talk of unit <paramref name="Number"/> or a visit of the harem's girl <paramref name="Number"/> (the picker's number, 1-50).</summary>
public readonly record struct MarkTarget(MarkTargetKind Kind, int Number)
{
    /// <summary>The talk of <paramref name="unit"/>.</summary>
    public static MarkTarget Talk(int unit) => new(MarkTargetKind.Talk, unit);

    /// <summary>The visit of the harem's girl <paramref name="girl"/>.</summary>
    public static MarkTarget Visit(int girl) => new(MarkTargetKind.Visit, girl);

    /// <inheritdoc />
    public override string ToString() => $"{Kind} {Number}";
}

/// <summary>A mark with what it came from: the paths' tree (each path's end, changes and why it is new).</summary>
public sealed record SceneMarkReport(MarkTarget Target, SceneMark Mark, MarkTree Tree)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"{Target}: {Mark}{(Tree.Truncated ? " (truncated)" : "")}" + string.Concat(Tree.Leaves.Select(l =>
            $"\n  [{string.Join(", ", l.Path.Decisions)}] {l.Path.End}{(l.Why is { } why ? $": {why}" : "")}"));
}

/// <summary>
/// Ours (owner's decision 30, docs/ui/talk-marks.md §2, §8 step 4): the marks on Talk and the harem — for a unit's talk
/// or a girl's visit, whether nothing new can happen (<see cref="SceneMark.Read"/>), something new is certain or reached
/// by a menu (<see cref="SceneMark.New"/>), or only a draw may bring it (<see cref="SceneMark.Maybe"/>, "???"). It runs
/// the talk or the visit as the command does (<see cref="SubordinatesCommand.PreviewTalkAsync"/>,
/// <see cref="HaremCommand.PreviewVisitAsync"/>) in the dry run (<see cref="ScenePreview{TState}"/>) on copies of the
/// state the picker sees, every draw and menu forked, and reads the paths' tree (<see cref="MarkTree"/>).
/// </summary>
/// <remarks>
/// <para>Each run has a context of its own over the run's parts: the scenes from the shared
/// <see cref="CachedSceneSource"/>, the helper files (<c>WAO_SUB</c>, <c>M</c>, <c>WAO_MAP</c>, <c>PCMPLAY</c>) in the
/// context as always; of the other files the talk and harem scenes call (docs/ui/talk-marks.md §9), <c>FIGTATTK</c> (the
/// exchange's animation, from <c>FIGT表示</c>) runs its port with its draws given 1 and no decision (its one draw,
/// <c>FIGTATTK.ADV:234</c>, is never read), and every other — the battles <c>FIGT_A</c>, <c>FIGT_B</c>, <c>FIGT</c>,
/// <c>死亡捕獲</c>, the party picker <c>Sﾌﾞﾀｲ</c>, any file not foreseen — stops the path (something happens there). A jump to
/// <c>OVER</c> (an ending) ends the path, which the marks leave out.</para>
/// <para>Nothing of the game is touched: the state is only read (copied, compared), no CG load counter is bound, the
/// game's generator, view, input and saves are not used. Not thread-safe (the programs' cache): call it from the game's
/// thread.</para>
/// </remarks>
public sealed class SceneMarks
{
    /// <summary>
    /// The draws that change the state (decision 30; docs/ui/talk-marks.md §3.3-3.6), each with its mark once its usual
    /// scene is read and "New scenes first" makes the change certain: Haurein's (unit 12) leaving (<c>Gﾊｳﾚｰﾝ:2299</c>),
    /// Kentarou's (unit 31) training step (<c>Gｹﾝﾀﾛｳ:608</c>), Isoroku's (unit 27) pregnancy (<c>Gｲｿﾛｸ:4599</c>) — ???,
    /// never New —, and Kanami's scene 2 and tower scene (<c>Gｶﾅﾐ:182</c>) — New.
    /// </summary>
    public static readonly IReadOnlyDictionary<ScriptRef, SceneMark> ListedDraws = new Dictionary<ScriptRef, SceneMark>
    {
        [new("Gﾊｳﾚｰﾝ.ADV", 2299)] = SceneMark.Maybe,
        [new("Gｹﾝﾀﾛｳ.ADV", 608)] = SceneMark.Maybe,
        [new("Gｲｿﾛｸ.ADV", 4599)] = SceneMark.Maybe,
        [new("Gｶﾅﾐ.ADV", 182)] = SceneMark.New,
    };

    /// <summary>
    /// The command's own lines around the scene, never unread for the marks: Rance's "Maybe I'll meet with X."
    /// (<c>Cﾌﾞｶ.ADV:1104</c>, before every talk past the prologue) and "Rance gained experience" (<c>Cﾊｰﾚﾑ.ADV:566</c>, after a
    /// visit that pays). Rance's self-talk (<c>Cﾌﾞｶ.ADV:1338-1377</c>) is his talk and counts.
    /// </summary>
    public static readonly IReadOnlySet<ScriptRef> FrameLines = new HashSet<ScriptRef>
    {
        new("Cﾌﾞｶ.ADV", 1104),
        new("Cﾊｰﾚﾑ.ADV", 566),
    };

    private readonly CachedSceneSource _scenes;

    /// <summary>The marks over the game's scenes (read once each, kept for every later mark).</summary>
    public SceneMarks(ISceneSource scenes)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        _scenes = scenes as CachedSceneSource ?? new CachedSceneSource(scenes);
    }

    /// <summary>The most paths a mark runs (<see cref="ScenePreview{TState}.MaxPaths"/>); past it the mark is at least ???.</summary>
    public int MaxPaths { get; init; } = ScenePreview<GameState>.DefaultMaxPaths;

    /// <summary>The rules of the marks with "New scenes first" on or off.</summary>
    public static MarkRules Rules(bool newScenesFirst) => newScenesFirst ? RulesOn : RulesOff;

    private static readonly MarkRules RulesOn = new(true, ListedDraws);

    private static readonly MarkRules RulesOff = new(false, ListedDraws);

    /// <summary>The units the picker <c>Sﾌﾞｶ</c> lets talk: those able to act (<c>Unit_Status</c> 1; <c>Cﾌﾞｶ.ADV:28-43</c>).</summary>
    public static IReadOnlyList<int> TalkingUnits(GameState state) =>
        [.. Enumerable.Range(1, 50).Where(unit => state.Units[(Sys3Int)unit].Status == 1)];

    /// <summary>The girls the picker <c>Sﾊｰﾚﾑ</c> offers: their <c>HaremAvail_*</c> flags, set by <c>Cﾊｰﾚﾑ</c> before it opens.</summary>
    public static IReadOnlyList<int> OfferedGirls(GameState state) =>
        [.. Enumerable.Range(1, 50).Where(girl => state.Memory.Var(248 + girl) != 0)];

    /// <summary>The mark of <paramref name="unit"/>'s talk in <paramref name="state"/> (the picker <c>Sﾌﾞｶ</c> open).</summary>
    public SceneMarkReport Talk(GameState state, int unit, bool newScenesFirst) => Mark(state, MarkTarget.Talk(unit), newScenesFirst);

    /// <summary>The mark of the visit of <paramref name="girl"/> in <paramref name="state"/> (the picker <c>Sﾊｰﾚﾑ</c> open).</summary>
    public SceneMarkReport Visit(GameState state, int girl, bool newScenesFirst) => Mark(state, MarkTarget.Visit(girl), newScenesFirst);

    /// <summary>The marks of every unit's talk the picker offers (<see cref="TalkingUnits"/>), by unit.</summary>
    public IReadOnlyDictionary<int, SceneMark> Talks(GameState state, bool newScenesFirst) =>
        TalkingUnits(state).ToDictionary(unit => unit, unit => Talk(state, unit, newScenesFirst).Mark);

    /// <summary>The marks of every girl's visit the picker offers (<see cref="OfferedGirls"/>), by her number.</summary>
    public IReadOnlyDictionary<int, SceneMark> Visits(GameState state, bool newScenesFirst) =>
        OfferedGirls(state).ToDictionary(girl => girl, girl => Visit(state, girl, newScenesFirst).Mark);

    /// <summary>The mark of <paramref name="entry"/> in <paramref name="state"/>, with its tree.</summary>
    public SceneMarkReport Mark(GameState state, MarkTarget entry, bool newScenesFirst)
    {
        var tree = Explore(state, entry);
        return new SceneMarkReport(entry, tree.Mark(Rules(newScenesFirst)), tree);
    }

    /// <summary>
    /// The paths' tree of <paramref name="entry"/> from <paramref name="state"/>, each path following
    /// <paramref name="forced"/> first (step 5: the decisions a real talk or visit has made so far; the tree's
    /// <see cref="MarkTree.NodeAt"/> of them is the draw being made).
    /// </summary>
    public MarkTree Explore(GameState state, MarkTarget entry, IReadOnlyList<PreviewDecision>? forced = null)
    {
        var leaves = new List<MarkLeaf>();
        Func<ScriptRef, bool> unread = line => !FrameLines.Contains(line) && !state.IsLineRead(line);
        var result = Preview(state, entry).Run(forced, (path, end) =>
            leaves.Add(new MarkLeaf(path, SceneMarkState.Changes(state, end, entry.Kind), unread)));
        return MarkTree.Build(leaves, result.Truncated);
    }

    /// <summary>The dry run of <paramref name="entry"/> from <paramref name="start"/> (a copy taken anew for every path).</summary>
    public ScenePreview<GameState> Preview(GameState start, MarkTarget entry)
    {
        if (entry.Number is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(entry), entry, "A unit or a girl is 1-50.");
        return new ScenePreview<GameState>(start, run => StartAsync(run, entry)) { MaxPaths = MaxPaths };
    }

    /// <summary>One run: a fresh context over the run's parts, then the talk or the visit.</summary>
    private Task StartAsync(PreviewRun<GameState> run, MarkTarget entry)
    {
        var random = new MutableRandom(run);
        var input = new GamePreviewInput(run);
        var router = new PreviewRouter(run, random);
        var runner = new SceneRunner(_scenes, router);                 // no ScenePatches: the files as the original
        var script = new OriginalScriptContext(run.State, random, run.View, input, runner) { Saves = run.Saves };
        runner.Bind(script);
        router.Script = script;
        return entry.Kind == MarkTargetKind.Talk
            ? new SubordinatesCommand(script).PreviewTalkAsync((Sys3Int)entry.Number)
            : new HaremCommand(script).PreviewVisitAsync((Sys3Int)entry.Number);
    }

    /// <summary>The run's generator, its draws given 1 without a decision while <see cref="Muted"/> (a presentation's draws).</summary>
    private sealed class MutableRandom(PreviewRun run) : IPlacedRandom
    {
        public bool Muted { get; set; }

        public Sys3Int Next(Sys3Int n) => Muted ? Mute(n) : run.Random.Next(n);

        public Sys3Int Next(Sys3Int n, ScriptRef at) => Muted ? Mute(n) : run.Random.Next(n, at);

        private Sys3Int Mute(Sys3Int n)
        {
            run.Step();
            return n.Value <= 1 ? n : (Sys3Int)1;
        }
    }

    /// <summary>
    /// The non-scene files the scenes call: <c>FIGTATTK</c> as its port, its draws muted; every other stops the path with
    /// its name.
    /// </summary>
    private sealed class PreviewRouter(PreviewRun run, MutableRandom random) : IStoryHooks
    {
        public OriginalScriptContext? Script { get; set; }

        public async Task RunEventAsync(string file, Sys3Int entry)
        {
            if (file != "FIGTATTK.ADV" || Script is null)
                run.Stop(file);
            random.Muted = true;
            try
            {
                await new Battle(Script, (IBattleInput)Script.Input).PlayExchangeAnimationAsync();
            }
            finally
            {
                random.Muted = false;
            }
        }
    }

    /// <summary>The run's input as the game's (<see cref="IGameInput"/>): a battle's command or menu stops the path (none is reached: the battles stop first).</summary>
    private sealed class GamePreviewInput(PreviewRun run) : PreviewInput(run), IGameInput
    {
        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
        {
            Run.Stop("a battle's command");
            return Task.FromResult(default(FigtSelChoice));
        }

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
        {
            Run.Stop("a battle's menu");
            return Task.FromResult<Sys3Int?>(null);
        }
    }
}
