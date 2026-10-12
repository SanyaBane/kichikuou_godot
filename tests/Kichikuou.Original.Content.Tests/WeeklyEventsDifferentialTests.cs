using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The hand port of <c>COM.ADV</c> (<see cref="WeeklyEvents"/>) against the file itself run from
/// the player's decompile by <see cref="SceneRunner"/>, on random states: both runs must call the
/// same scenes in the same order, draw the same rolls and leave the same memory. The scene hook
/// scribbles over cursors, scratch and random variables, so conditions read after a scene (tier 1,
/// the tier-2 fall-throughs) are checked on the state the scene left. Most states are aimed at one
/// scene call of the file (<see cref="Guide"/>), so every trigger is reached. Skipped without the decompile.
/// </summary>
public class WeeklyEventsDifferentialTests(ITestOutputHelper output)
{
    private const string Com = "COM.ADV";
    private const int Cases = 2400;
    private const int AimAttempts = 8;
    private const int Repairs = 12;

    private static readonly int[] Interesting =
        [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 19, 20, 21, 22, 30, 40, 41, 42, 43, 49, 52, 90, 99, 100, 200];

    [Fact]
    public async Task The_port_runs_COM_exactly_as_the_decompile()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, Com)))
            return;
        var program = new ScenarioScripts(LocalData.ScenarioDirectory).ReadProgram(Com);
        var scenes = new OneProgram(program);
        var guide = new Guide(program);
        var reached = new Dictionary<(string File, int Entry), int>();
        int aimed = 0, hit = 0, cases = DifferentialRuns.Cases(Cases);

        for (int seed = 1; seed <= cases; seed++)
        {
            var state = RandomState(new Random(seed));
            int runSeed = seed;
            IReadOnlyList<int> rolls = [];
            GameState? aimedState = null;
            if (seed % 4 != 0)
            {
                aimed++;
                var target = guide.Calls[seed % guide.Calls.Count];
                for (int attempt = 0; attempt < AimAttempts; attempt++)
                {
                    var rng = new Random(seed * 100 + attempt);
                    var candidate = Copy(state);
                    RandomVariables(candidate, rng);
                    var wanted = guide.Aim(candidate, target.Index, rng);
                    bool reachedTarget = false;
                    for (int repair = 0; repair < Repairs; repair++)
                    {
                        var trial = await RunAsync(Copy(candidate), seed * 100 + attempt, scenes: null, wanted);
                        reachedTarget = trial.Calls.Contains((target.File, target.Entry));
                        if (reachedTarget || trial.Calls.Count == 0
                            || !guide.Repair(candidate, trial.Calls[0].File, trial.Calls[0].Entry, target.Index, rng, wanted))
                            break;
                    }
                    aimedState = candidate;
                    runSeed = seed * 100 + attempt;
                    rolls = wanted;
                    if (reachedTarget)
                    {
                        hit++;
                        break;
                    }
                }
                state = aimedState!;
            }

            var port = await RunAsync(Copy(state), runSeed, scenes: null, rolls);
            var original = await RunAsync(Copy(state), runSeed, scenes, rolls);

            Assert.True(original.Calls.SequenceEqual(port.Calls),
                $"seed {seed}: scene calls differ\n  COM.ADV: {string.Join(", ", original.Calls)}\n  port:    {string.Join(", ", port.Calls)}");
            Assert.True(original.Draws.SequenceEqual(port.Draws),
                $"seed {seed}: draws differ\n  COM.ADV: {string.Join(", ", original.Draws)}\n  port:    {string.Join(", ", port.Draws)}");
            var differences = Differences(original.State.Memory, port.State.Memory).Take(10).ToList();
            Assert.True(differences.Count == 0, $"seed {seed}: memory differs\n  " + string.Join("\n  ", differences));

            foreach (var call in port.Calls)
                reached[call] = reached.GetValueOrDefault(call) + 1;
        }

        var expected = guide.Calls.Select(c => (c.File, c.Entry)).ToHashSet();
        output.WriteLine($"{cases} states, {aimed} aimed at a call ({hit} reached it); {reached.Count} of {expected.Count} scene entries called");
        foreach (var ((file, entry), count) in reached.OrderBy(r => r.Key.File, StringComparer.Ordinal).ThenBy(r => r.Key.Entry))
            output.WriteLine($"  {file} {entry}: {count}");
        var missed = expected.Except(reached.Keys).Select(m => $"{m.Item1} {m.Item2}").ToList();
        DifferentialRuns.AssertReached(missed, output, "never called");
    }

    /// <summary><c>COM.ADV</c>, read once.</summary>
    private sealed class OneProgram(ScriptProgram program) : ISceneSource
    {
        public ScriptProgram ReadProgram(string file) =>
            file == Com ? program : throw new InvalidOperationException($"{file} is a hook here.");
    }

    private sealed record Run(GameState State, List<(string File, int Entry)> Calls, List<int> Draws);

    private static async Task<Run> RunAsync(GameState state, int seed, OneProgram? scenes, IReadOnlyList<int> rolls)
    {
        var random = new SeededRandom(seed, rolls);
        var story = new ScribblingStory(state, seed);
        if (scenes is null)
        {
            var script = new OriginalScriptContext(state, random, new RecordingView(), new ScriptedSceneInput(), story);
            await new WeeklyEvents(script).RunAsync();
        }
        else
        {
            var runner = new SceneRunner(scenes, story, file => file == Com);
            var script = new OriginalScriptContext(state, random, new RecordingView(), new ScriptedSceneInput(), runner);
            runner.Bind(script);
            await script.CallFileAsync(Com);
        }
        return new Run(state, story.Calls, random.Draws);
    }

    /// <summary>
    /// Every unbound variable and every slot of the record page random, biased to 0 and to the
    /// small values the conditions compare with; week 9 and stage 0 (the early exits) are rare.
    /// Most states are "quiet" — few flags at 0 and nearly everyone alive — since the tier-1
    /// triggers on zero flags and dead units would otherwise fire almost every week.
    /// </summary>
    private static GameState RandomState(Random rng)
    {
        bool quiet = rng.Next(4) != 0;
        double zero = quiet ? rng.NextDouble() * 0.3 : 0.55 + rng.NextDouble() * 0.44;
        var state = new GameState();
        RandomVariables(state, rng, zero);
        for (int i = 0; i < state.Memory.PageSize(1); i++)
            state.Memory.WritePage(1, i, Value(rng, zero));
        if (quiet)
        {
            for (int u = 0; u < 52; u++)
                if (rng.Next(12) != 0)
                    state.Units[(Sys3Int)u].Status = 1;
            for (int c = 0; c < 220; c++)
                if (rng.Next(12) != 0)
                    state.Cmdrs[(Sys3Int)c].Status = 1;
        }
        return state;
    }

    /// <summary>Every unbound variable random (biased to 0 with probability <paramref name="zero"/>); week 9 and stage 0 rare.</summary>
    private static void RandomVariables(GameState state, Random rng, double? zero = null)
    {
        double p = zero ?? rng.NextDouble() * 0.6;
        for (int v = 0; v < VariableCatalog.Variables.Count; v++)
            if (!state.IsBound(v))
                state.Memory.Var(v) = Value(rng, p);
        state.StoryStage = (Sys3Int)(rng.Next(20) == 0 ? 0 : rng.Next(1, 5));
        state.Week = (Sys3Int)(rng.Next(30) == 0 ? 9 : rng.Next(1, 90));
        if (rng.Next(4) == 0)
            state.Ev_WeeklyEventDone = 1;   // overwritten by :85 — must not matter
    }

    private static Sys3Int Value(Random rng, double zero) =>
        (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(5) == 0 ? rng.Next(0, 300) : Interesting[rng.Next(Interesting.Length)]);

    /// <summary>The variables and the record page (the only page the states here fill; strings stay empty).</summary>
    private static GameState Copy(GameState source)
    {
        var copy = new GameState();
        for (int v = 0; v < VariableCatalog.Variables.Count; v++)
            copy.Memory.Var(v) = source.Memory.Var(v);
        for (int i = 0; i < source.Memory.PageSize(1); i++)
            copy.Memory.WritePage(1, i, source.Memory.ReadPage(1, i));
        return copy;
    }

    /// <summary>
    /// Aims a state at one scene call of the program: the conditions of the blocks around the call
    /// are made true and those of the trigger blocks before it false, by writing the variable each
    /// comparison tests (through the record the script's cursor assignments select, and through
    /// <c>Tmp0001 := Unit_Status</c>-style copies), allowing for the counters the upkeep steps
    /// first. When a trial run calls an earlier scene instead, <see cref="Repair"/> falsifies a block
    /// around that call. A heuristic only: the runs decide what is reached.
    /// </summary>
    private sealed class Guide
    {
        private static readonly HashSet<int> Cursors = VariableCatalog.Bindings.Select(b => b.Cursor).ToHashSet();
        private static readonly int EvEntry =
            Enumerable.Range(0, VariableCatalog.Variables.Count).Single(v => VariableCatalog.Variables[v].Name == "Ev_Entry");

        private readonly IReadOnlyList<ScriptStatement> _statements;

        public Guide(ScriptProgram program)
        {
            _statements = program.Statements;
            var calls = new List<(int, string, int)>();
            int entry = -1;
            for (int i = 0; i < _statements.Count; i++)
            {
                if (_statements[i] is AssignStatement { Value: ConstExpr c } a && a.Var == EvEntry)
                    entry = c.Value.Value;
                if (_statements[i] is CallFileStatement call)
                    calls.Add((i, call.File, entry));
            }
            Calls = calls;

            // What the script has done before each block: cursors, copies, counter steps.
            _contexts = new Context?[_statements.Count];
            var cursors = new Dictionary<int, Sys3Int>();
            var copies = new Dictionary<int, (int Source, Dictionary<int, Sys3Int> Cursors)>();
            var deltas = new Dictionary<int, int>();
            for (int i = 0; i < _statements.Count; i++)
            {
                switch (_statements[i])
                {
                    case AssignStatement { Value: ConstExpr c } a when Cursors.Contains(a.Var):
                        cursors[a.Var] = c.Value;
                        break;
                    case AssignStatement { Value: VarExpr v } a:
                        copies[a.Var] = (v.Var, new(cursors));
                        break;
                    // The upkeep's counters (V := V ± k) run before the triggers test them.
                    case AssignStatement { Value: BinaryExpr { Op: "+" or "-", Left: VarExpr l, Right: ConstExpr k } b } a when l.Var == a.Var:
                        deltas[a.Var] = deltas.GetValueOrDefault(a.Var) + (b.Op == "+" ? k.Value.Value : -k.Value.Value);
                        break;
                    case IfStatement:
                        _contexts[i] = new Context(new(cursors), new(copies), new(deltas));
                        break;
                }
            }
        }

        private readonly Context?[] _contexts;

        /// <summary>Every scene call: its statement index, file and the <c>Ev_Entry</c> set before it.</summary>
        public IReadOnlyList<(int Index, string File, int Entry)> Calls { get; }

        /// <summary>Aims <paramref name="state"/> at the call at <paramref name="target"/>; returns the rolls (<c>RetVal</c>) the call needs.</summary>
        public List<int> Aim(GameState state, int target, Random rng)
        {
            for (int i = 0; i < target; i++)
                if (_statements[i] is IfStatement f && f.EndTarget <= target && HasCall(i, f.EndTarget) && rng.Next(400) != 0)
                    new Solver(state, _contexts[i]!, rng, null).Solve(f.Condition, false);
            var wanted = new List<int>();
            Want(state, target, rng, wanted);
            return wanted;
        }

        /// <summary>
        /// After a run in which a call before the target fired (<paramref name="file"/>,
        /// <paramref name="entry"/>): falsifies a block around that call, then makes the target's
        /// blocks true again.
        /// </summary>
        public bool Repair(GameState state, string file, int entry, int target, Random rng, List<int> wanted)
        {
            var sites = Calls.Where(c => c.Index < target && c.File == file && c.Entry == entry).ToList();
            if (sites.Count == 0)
                return false;
            int blocker = sites[rng.Next(sites.Count)].Index;
            var around = Enumerable.Range(0, blocker)
                .Where(i => _statements[i] is IfStatement f && f.EndTarget > blocker && f.EndTarget <= target).ToList();
            if (around.Count == 0)
                return false;
            int pick = rng.Next(3) != 0 ? around[^1] : around[rng.Next(around.Count)];
            new Solver(state, _contexts[pick]!, rng, null).Solve(((IfStatement)_statements[pick]).Condition, false);
            Want(state, target, rng, wanted);
            return true;
        }

        private void Want(GameState state, int target, Random rng, List<int> wanted)
        {
            for (int i = 0; i < target; i++)
                if (_statements[i] is IfStatement f && f.EndTarget > target)
                    new Solver(state, _contexts[i]!, rng, wanted).Solve(f.Condition, true);
        }

        private bool HasCall(int from, int to)
        {
            for (int i = from + 1; i < to && i < _statements.Count; i++)
                if (_statements[i] is CallFileStatement)
                    return true;
            return false;
        }
    }

    /// <summary>
    /// What the script has done before a condition: the cursors it set, the copies it made
    /// (<c>Tmp0001 := Unit_Status</c>) and the net change of the counters it stepped.
    /// </summary>
    private sealed record Context(
        Dictionary<int, Sys3Int> Cursors,
        Dictionary<int, (int Source, Dictionary<int, Sys3Int> Cursors)> Copies,
        Dictionary<int, int> Deltas);

    /// <summary>Writes variables so that a condition gets the wanted truth value (when it can).</summary>
    private sealed class Solver(GameState state, Context context, Random rng, List<int>? rolls)
    {
        private static readonly int RetVal =
            Enumerable.Range(0, VariableCatalog.Variables.Count).Single(v => VariableCatalog.Variables[v].Name == "RetVal");

        public void Solve(ScriptExpr condition, bool want)
        {
            switch (condition)
            {
                case BinaryExpr { Op: "*" } b when want:
                    Solve(b.Left, true);
                    Solve(b.Right, true);
                    break;
                case BinaryExpr { Op: "*" } b:
                    // A roll is drawn after the state is set: falsify another factor when there is one.
                    if (ReadsRetVal(b.Left) != ReadsRetVal(b.Right))
                        Solve(ReadsRetVal(b.Left) ? b.Right : b.Left, false);
                    else
                        Solve(rng.Next(2) == 0 ? b.Left : b.Right, false);
                    break;
                case BinaryExpr { Op: "+" } b when want:
                    Solve(rng.Next(2) == 0 ? b.Left : b.Right, true);
                    break;
                case BinaryExpr { Op: "+" } b:
                    Solve(b.Left, false);
                    Solve(b.Right, false);
                    break;
                case BinaryExpr { Left: VarExpr v } b when IsComparison(b.Op):
                    Write(v.Var, Pick(b.Op, want, Eval(b.Right)), context.Cursors);
                    break;
                case BinaryExpr { Right: VarExpr v } b when IsComparison(b.Op):
                    Write(v.Var, Pick(Mirror(b.Op), want, Eval(b.Left)), context.Cursors);
                    break;
            }
        }

        private static bool ReadsRetVal(ScriptExpr expr) => expr switch
        {
            VarExpr v => v.Var == RetVal,
            BinaryExpr b => ReadsRetVal(b.Left) || ReadsRetVal(b.Right),
            _ => false,
        };

        private static bool IsComparison(string op) => op is "=" or "\\" or "<" or ">" or "<=" or ">=";

        private static string Mirror(string op) => op switch { "<" => ">", ">" => "<", "<=" => ">=", ">=" => "<=", _ => op };

        /// <summary>A value x with (x op rhs) = want.</summary>
        private int Pick(string op, bool want, int rhs)
        {
            int Above(int from) => Math.Min(65535, from + rng.Next(1, 4));
            int Below(int to) => to <= 0 ? 0 : Math.Max(0, to - rng.Next(1, 4));
            return (op, want) switch
            {
                ("=", true) or ("\\", false) => rhs,
                ("=", false) or ("\\", true) => rhs == 0 || rng.Next(2) == 0 ? Above(rhs) : Below(rhs),
                (">", true) or ("<=", false) => Above(rhs),
                (">", false) or ("<=", true) => rhs == 0 ? 0 : rng.Next(2) == 0 ? rhs : Below(rhs),
                ("<", true) or (">=", false) => rhs == 0 ? 0 : Below(rhs),
                _ => rng.Next(2) == 0 ? rhs : Above(rhs),   // (<, false) / (>=, true)
            };
        }

        private int Eval(ScriptExpr expr) => expr switch
        {
            ConstExpr c => c.Value.Value,
            VarExpr v => Read(v.Var),
            BinaryExpr { Op: "+" } b => Math.Min(65535, Eval(b.Left) + Eval(b.Right)),
            BinaryExpr { Op: "-" } b => Math.Max(0, Eval(b.Left) - Eval(b.Right)),
            _ => 0,
        };

        private int Read(int var)
        {
            foreach (var (cursor, value) in context.Cursors)
                state.Memory.Var(cursor) = value;
            return state.ReadVariable(var).Value;
        }

        private void Write(int var, int value, Dictionary<int, Sys3Int> at)
        {
            if (var == RetVal)
                rolls?.Add(value);   // a roll's result: the generator is asked for it
            if (context.Copies.TryGetValue(var, out var copy))
            {
                Write(copy.Source, value, copy.Cursors);
                return;
            }
            if (context.Deltas.TryGetValue(var, out int delta) && rng.Next(4) != 0)
                value = Math.Clamp(value - delta, 0, 65535);
            foreach (var (cursor, cursorValue) in at)
                state.Memory.Var(cursor) = cursorValue;
            state.WriteVariable(var, (Sys3Int)value);
        }
    }

    /// <summary>
    /// Draws from a seeded generator, logging n: half the time one of the values the aimed call
    /// needs (if any, and ≤ n), else 1 (what most rolls test) four times in ten, else uniform in
    /// 1..n. The same seed gives both runs the same rolls.
    /// </summary>
    private sealed class SeededRandom(int seed, IReadOnlyList<int> wanted) : IScriptRandom
    {
        private readonly Random _rng = new(seed * 7919);

        public List<int> Draws { get; } = [];

        public Sys3Int Next(Sys3Int n)
        {
            if (n.Value <= 1)
                return n;
            Draws.Add(n.Value);
            var fits = wanted.Where(w => w >= 1 && w <= n.Value).ToList();
            if (fits.Count > 0 && _rng.Next(2) == 0)
                return (Sys3Int)fits[_rng.Next(fits.Count)];
            return (Sys3Int)(_rng.Next(10) < 4 ? 1 : _rng.Next(1, n.Value + 1));
        }
    }

    /// <summary>
    /// The scenes as hooks: records each call with the <c>Ev_Entry</c> it sees and then, like a
    /// scene, writes the cursors, <c>RetVal</c>, <c>Tmp0001</c>, <c>Ev_WeeklyEventDone</c> and a
    /// few random variables — the same writes for the same (seed, call) in both runs.
    /// </summary>
    private sealed class ScribblingStory(GameState state, int seed) : IStoryHooks
    {
        public List<(string File, int Entry)> Calls { get; } = [];

        public Task RunEventAsync(string file, Sys3Int entry)
        {
            if (state.Ev_Entry != entry)
                throw new InvalidOperationException($"{file}: Ev_Entry {state.Ev_Entry.Value}, entry {entry.Value}");
            Calls.Add((file, entry.Value));
            var rng = new Random(seed * 1000 + Calls.Count);
            if (rng.Next(2) == 0)
                state.Unit_Cur = (Sys3Int)rng.Next(0, 52);
            if (rng.Next(2) == 0)
                state.Cmdr_Cur = (Sys3Int)rng.Next(0, 220);
            if (rng.Next(2) == 0)
                state.Prov_Cur = (Sys3Int)rng.Next(0, 100);
            if (rng.Next(2) == 0)
                state.Item_Cur = (Sys3Int)rng.Next(0, 50);
            if (rng.Next(2) == 0)
                state.RetVal = (Sys3Int)rng.Next(0, 11);
            if (rng.Next(2) == 0)
                state.Tmp0001 = (Sys3Int)rng.Next(0, 3);
            if (rng.Next(2) == 0)
                state.Ev_WeeklyEventDone = (Sys3Int)rng.Next(0, 2);
            for (int k = rng.Next(0, 6); k > 0; k--)
            {
                int v = rng.Next(0, VariableCatalog.Variables.Count);
                if (!state.IsBound(v))
                    state.Memory.Var(v) = (Sys3Int)Interesting[rng.Next(Interesting.Length)];
            }
            return Task.CompletedTask;
        }
    }

    private static IEnumerable<string> Differences(ScriptMemory expected, ScriptMemory actual)
    {
        for (int v = 0; v < VariableCatalog.Variables.Count; v++)
            if (expected.Var(v) != actual.Var(v))
                yield return $"VAR{v:D4}: COM.ADV {expected.Var(v).Value}, port {actual.Var(v).Value}";
        foreach (int page in expected.AllocatedPages)
            for (int i = 0; i < expected.PageSize(page); i++)
                if (expected.ReadPage(page, i) != actual.ReadPage(page, i))
                    yield return $"page {page}[{i}]: COM.ADV {expected.ReadPage(page, i).Value}, port {actual.ReadPage(page, i).Value}";
        for (int s = 0; s < expected.Strings.Length; s++)
            if (expected.Strings[s] != actual.Strings[s])
                yield return $"string {s}: COM.ADV \"{expected.Strings[s]}\", port \"{actual.Strings[s]}\"";
    }
}
