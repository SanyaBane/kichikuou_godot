using System.Text;
using Kichikuou.Content.Scripts;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The hand-ported nation files (<see cref="RebelTurn"/>, <see cref="RebelDefenders"/>, on the
/// template <see cref="NationFile"/>) against the files themselves run from the player's decompile
/// by <see cref="SceneRunner"/>, on random states: both runs must call the same files with the
/// same <c>Ev_Entry</c> and the same party, draw the same rolls (none), show the same events and
/// leave the same memory. Every other file (<c>FIGT_B</c>, <c>Gｴｸｽ</c>) is a hook that scribbles
/// over what a battle or a scene may change — the commanders, the owners, the cursors, scratch,
/// the retreat mode — and now and then ends the game by a jump. Skipped without the decompile.
/// </summary>
public class NationTurnDifferentialTests(ITestOutputHelper output)
{
    private const int Cases = 3000;

    [Fact]
    public Task The_rebel_turn_runs_exactly_as_TCﾘｰｻﾞｽ() =>
        CompareAsync(
            "TCﾘｰｻﾞｽ.ADV",
            (script, scenario) => new RebelTurn(script, scenario).RunAsync(),
            (state, rng) =>
            {
                state.F_LeazasRebels_State = (Sys3Int)rng.Next(0, 3);
                state.Prov_Cur = (Sys3Int)rng.Next(0, 101);
            },
            RebelTurnFeatures,
            ["call Gｴｸｽ.ADV", "A at 4", "A at 3", "A at 2", "A at 1", "B at 7", "B at 8", "B at 1", "C at 6", "C at 5",
             "C after B", "two attacks", "no attack", "jump", "record 0 picked"]);

    [Fact]
    public Task The_rebel_defenders_are_picked_exactly_as_TCﾘｰｻﾞｽ2() =>
        CompareAsync(
            "TCﾘｰｻﾞｽ2.ADV",
            (script, scenario) => new RebelDefenders(script, scenario).RunAsync(),
            (state, rng) => state.Prov_Cur = (Sys3Int)(rng.Next(6) == 0 ? rng.Next(0, 101) : rng.Next(2, 9)),
            RebelDefenderFeatures,
            ["B at 7", "B at 8", "B elsewhere", "A at 2", "A at 3", "A at 4", "C at 5", "C at 6", "stale B", "stale A",
             "empty party", "full party", "walls"]);

    private delegate IEnumerable<string> FeatureReader(GameState before, GameState after, List<string> log);

    private async Task CompareAsync(
        string file,
        Func<ScriptContext, ScenarioScripts, Task> port,
        Action<GameState, Random> aim,
        FeatureReader features,
        string[] expected)
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, file)))
            return;
        var scenario = new ScenarioScripts(LocalData.ScenarioDirectory);
        var program = scenario.ReadProgram(file);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int seed = 1; seed <= Cases; seed++)
        {
            var rng = new Random(seed);
            var state = RandomState(rng);
            aim(state, rng);
            var ported = await RunAsync(Copy(state), seed, script => port(script, scenario), null, file);
            var original = await RunAsync(Copy(state), seed, null, program, file);

            int at = FirstDifference(original.Log, ported.Log);
            Assert.True(at < 0, $"seed {seed}: the runs differ at entry {at}\n{Around(file, original.Log, ported.Log, at)}");
            var differences = Differences(file, original.State.Memory, ported.State.Memory).Take(10).ToList();
            Assert.True(differences.Count == 0, $"seed {seed}: memory differs\n  " + string.Join("\n  ", differences));

            foreach (string feature in features(state, ported.State, ported.Log).Distinct())
                seen[feature] = seen.GetValueOrDefault(feature) + 1;
        }

        output.WriteLine($"{file}: {Cases} runs");
        foreach (var (feature, count) in seen.OrderBy(f => f.Key, StringComparer.Ordinal))
            output.WriteLine($"  {feature}: {count}");
        var missed = expected.Where(e => !seen.ContainsKey(e)).ToList();
        Assert.True(missed.Count == 0, "never reached: " + string.Join(", ", missed));
    }

    private sealed record Run(GameState State, List<string> Log);

    private static async Task<Run> RunAsync(
        GameState state, int seed, Func<ScriptContext, Task>? port, ScriptProgram? program, string file)
    {
        var log = new List<string>();
        var hooks = new ScribblingHooks(state, seed, log);
        var input = new NoInput();
        var view = new LoggingView(log);
        var random = new LoggingRandom(seed, log);
        try
        {
            if (port is not null)
                await port(new ScriptContext(state, random, view, input, hooks));
            else
            {
                var runner = new SceneRunner(new OneProgram(file, program!), hooks, f => f == file);
                var script = new ScriptContext(state, random, view, input, runner);
                runner.Bind(script);
                await runner.RunFileAsync(file);
            }
            log.Add("return");
        }
        catch (ScenarioJumpException e)
        {
            log.Add($"jump {e.File}");
        }
        return new Run(state, log);
    }

    /// <summary>The army of a <c>FIGT_B</c> call, told by the priority list it was picked from.</summary>
    private static string Army(string call) =>
        call.Contains(" Tmp 3,5,4,0,", StringComparison.Ordinal) ? "A"
        : call.Contains(" Tmp 1,2,6,7,0,", StringComparison.Ordinal) ? "B"
        : call.Contains(" Tmp 1,2,3,6,4,5,7,", StringComparison.Ordinal) ? "C"
        : "?";

    private static IEnumerable<string> RebelTurnFeatures(GameState before, GameState after, List<string> log)
    {
        var battles = log.Where(e => e.StartsWith("call FIGT_B.ADV", StringComparison.Ordinal)).ToList();
        foreach (string call in battles)
        {
            string prov = call[(call.IndexOf(" Prov_Cur ", StringComparison.Ordinal) + 10)..].Split(' ')[0];
            yield return $"{Army(call)} at {prov}";
            if (Army(call) == "C" && before.Provs[5].Owner != 1)
                yield return "C after B";
        }
        if (battles.Count == 2)
            yield return "two attacks";
        if (battles.Count == 0)
            yield return "no attack";
        if (log.Any(e => e.StartsWith("call Gｴｸｽ.ADV", StringComparison.Ordinal)))
            yield return "call Gｴｸｽ.ADV";
        if (log.Any(e => e.StartsWith("jump ", StringComparison.Ordinal)))
            yield return "jump";
        if (before.Cmdrs[0].Status != 0)
            yield return "record 0 picked";
    }

    private static IEnumerable<string> RebelDefenderFeatures(GameState before, GameState after, List<string> log)
    {
        int prov = before.Prov_Cur.Value;
        string army = after.Tmp0003.Value switch { 4 => "A", 6 => "B", _ => "C" };
        yield return prov is >= 2 and <= 8 ? $"{army} at {prov}" : $"{army} elsewhere";
        if (army != "C" && after.CallArg < 100)
            yield return $"stale {army}";
        if (after.Bt_EnemyParty1 == 0)
            yield return "empty party";
        if (after.Bt_EnemyParty4 != 0)
            yield return "full party";
        if (prov == 6 && after.Bt_WallBonus != 0)
            yield return "walls";
    }

    private static int FirstDifference(List<string> a, List<string> b)
    {
        for (int i = 0; i < Math.Min(a.Count, b.Count); i++)
            if (a[i] != b[i])
                return i;
        return a.Count == b.Count ? -1 : Math.Min(a.Count, b.Count);
    }

    private static string Around(string file, List<string> original, List<string> port, int at)
    {
        var sb = new StringBuilder();
        for (int i = Math.Max(0, at - 8); i < at + 3; i++)
        {
            sb.Append($"  {i,5} {file}: {(i < original.Count ? original[i] : "-")}\n");
            sb.Append($"  {i,5} port: {(i < port.Count ? port[i] : "-")}\n");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Every unbound variable and the record pages random (commander record 0, which the picker
    /// and the troop sum read, among them), then: the rebel commanders 1-7 alive or not, with troops
    /// around the filler's 190 and the armies' 100 and 500 (near the 16-bit ceiling now and then),
    /// provinces 1-8 held by the player, the rebels or another owner, the walls of province 6, and a
    /// party and retreat mode left by an earlier battle.
    /// </summary>
    private static GameState RandomState(Random rng)
    {
        var state = new GameState();
        double zero = rng.NextDouble() * 0.6;
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            if (!GameState.IsBound(v))
                state.Memory.Var(v) = (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(5) == 0 ? rng.Next(0, 3000) : rng.Next(0, 12));
        for (int i = 0; i < state.Memory.PageSize(1); i++)
            state.Memory.WritePage(1, i, (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(0, 4)));
        for (int s = 0; s < state.Strings.Length; s++)
            state.Strings[s] = $"s{s}";

        for (int c = 0; c <= 7; c++)
        {
            if (c == 0 && rng.Next(3) != 0)
                continue;
            var cmdr = state.Cmdrs[c];
            cmdr.Status = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 3));
            cmdr.Troops = (Sys3Int)(rng.Next(12) switch
            {
                0 => rng.Next(60000, 65536),
                1 => rng.Next(0, 3000),
                2 => rng.Next(185, 195),
                _ => rng.Next(0, 400),
            });
        }
        for (int p = 1; p <= 8; p++)
            state.Provs[p].Owner = (Sys3Int)(rng.Next(8) switch { < 3 => 1, < 7 => 2, _ => rng.Next(0, 9) });
        state.Provs[6].BattleArea = (Sys3Int)rng.Next(0, 1000);
        state.Provs[6].Defense = (Sys3Int)rng.Next(0, 30);
        state.Bt_EnemyParty1 = (Sys3Int)rng.Next(0, 200);
        state.Bt_EnemyParty2 = (Sys3Int)rng.Next(0, 200);
        state.Bt_EnemyParty3 = (Sys3Int)rng.Next(0, 200);
        state.Bt_EnemyParty4 = (Sys3Int)rng.Next(0, 200);
        state.Bt_EnemyRetreatMode = (Sys3Int)(rng.Next(3) switch { 0 => 20, 1 => 50, _ => rng.Next(0, 7) });
        state.Bt_TroopLimit = (Sys3Int)rng.Next(0, 6000);
        state.Bt_WallBonus = (Sys3Int)rng.Next(0, 30);
        return state;
    }

    private static GameState Copy(GameState source)
    {
        var copy = new GameState();
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            copy.Memory.Var(v) = source.Memory.Var(v);
        foreach (int page in source.Memory.AllocatedPages)
            for (int i = 0; i < source.Memory.PageSize(page); i++)
                copy.Memory.WritePage(page, i, source.Memory.ReadPage(page, i));
        Array.Copy(source.Strings, copy.Strings, source.Strings.Length);
        return copy;
    }

    /// <summary>The file under test, read once.</summary>
    private sealed class OneProgram(string file, ScriptProgram program) : ISceneSource
    {
        public ScriptProgram ReadProgram(string name) =>
            name == file ? program : throw new InvalidOperationException($"{name} is a hook here.");
    }

    /// <summary>
    /// Draws from a generator of the seed, logged with their range: <c>Y 4, n</c> with n of 0 or 1
    /// gives n without drawing, as the engine's. The nation files of N1 draw nothing.
    /// </summary>
    private sealed class LoggingRandom(int seed, List<string> log) : IScriptRandom
    {
        private readonly Random _rng = new(seed * 17 + 3);

        public Sys3Int Next(Sys3Int n)
        {
            var value = n.Value <= 1 ? n : (Sys3Int)_rng.Next(1, n.Value + 1);
            log.Add($"rnd {n} {value}");
            return value;
        }
    }

    /// <summary>The nation files read no input: every request fails.</summary>
    private sealed class NoInput : ISceneInput
    {
        public Task<Sys3Int> WaitForKeyAsync() => throw new InvalidOperationException("IK 0");

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => throw new InvalidOperationException("Y 3");

        public Task<MouseState> GetMouseAsync() => throw new InvalidOperationException("IM");

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => throw new InvalidOperationException("IC");

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items) => throw new InvalidOperationException("]");

        public Task<Sys3Int> GetPlayingTrackAsync() => throw new InvalidOperationException("SR 0");

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new InvalidOperationException("ZT 0");

        public Task ResetTimerAsync(Sys3Int value) => throw new InvalidOperationException("ZT 1");

        public Task<Sys3Int> GetTimerAsync(int mode) => throw new InvalidOperationException("ZT n");

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => throw new InvalidOperationException("SC");

        public Task<Sys3Int> GetMidiTrackAsync() => throw new InvalidOperationException("SR 1");
    }

    /// <summary>
    /// The other files as hooks: each call is logged with what it sees (the party, the retreat
    /// mode, the cursors, the priority list in scratch, the army name), then writes what a battle or
    /// a scene writes — the same writes for the same (seed, call) in both runs; one battle in
    /// twelve loses Leazas Castle and jumps to the ending.
    /// </summary>
    private sealed class ScribblingHooks(GameState state, int seed, List<string> log) : IStoryHooks
    {
        private int _calls;

        public Task RunEventAsync(string file, Sys3Int entry)
        {
            if (state.Ev_Entry != entry)
                throw new InvalidOperationException($"{file}: Ev_Entry {state.Ev_Entry.Value}, entry {entry.Value}");
            log.Add($"call {file} entry {entry} RetVal {state.RetVal} CallArg {state.CallArg} Cmdr_Cur {state.Cmdr_Cur} " +
                    $"Prov_Cur {state.Prov_Cur} Tmp {state.Tmp0001},{state.Tmp0002},{state.Tmp0003},{state.Tmp0004},{state.Tmp0005}," +
                    $"{state.Tmp0006},{state.Tmp0007},{state.Tmp0008},{state.Tmp0009} Phase {state.Ev_PhaseNation} " +
                    $"Party {state.Bt_EnemyParty1},{state.Bt_EnemyParty2},{state.Bt_EnemyParty3},{state.Bt_EnemyParty4} " +
                    $"Retreat {state.Bt_EnemyRetreatMode} Rebels {state.F_LeazasRebels_State} Str10 {state.Strings[10]}");

            var rng = new Random(seed * 1000 + ++_calls);
            if (file == "FIGT_B.ADV" && rng.Next(12) == 0)
            {
                state.EndingId = 2;
                ScriptContext.JumpTo("OVER.ADV");
            }
            if (rng.Next(2) == 0)
                state.RetVal = (Sys3Int)rng.Next(0, 6);
            if (rng.Next(2) == 0)
                state.CallArg = (Sys3Int)rng.Next(0, 1000);
            for (int v = 1; v <= 9; v++)
                if (rng.Next(3) == 0)
                    state.Memory.Var(v) = (Sys3Int)rng.Next(0, 600);
            if (rng.Next(2) == 0)
                state.Cmdr_Cur = (Sys3Int)rng.Next(0, 200);
            if (rng.Next(3) == 0)
                state.Prov_Cur = (Sys3Int)rng.Next(0, 101);
            if (rng.Next(2) == 0)
                state.Bt_EnemyRetreatMode = (Sys3Int)(rng.Next(2) == 0 ? 20 : rng.Next(0, 60));
            if (rng.Next(3) == 0)
                state.Bt_EnemyParty1 = (Sys3Int)rng.Next(0, 8);
            if (rng.Next(6) == 0)
                state.Ev_Entry = (Sys3Int)rng.Next(0, 30);
            if (rng.Next(4) == 0)
                state.F_LeazasRebels_State = (Sys3Int)rng.Next(0, 3);
            for (int c = 1; c <= 7; c++)
            {
                if (rng.Next(4) == 0)
                    state.Cmdrs[c].Status = (Sys3Int)rng.Next(0, 3);
                if (rng.Next(3) == 0)
                    state.Cmdrs[c].Troops = (Sys3Int)rng.Next(0, 600);
            }
            for (int p = 1; p <= 8; p++)
                if (rng.Next(5) == 0)
                    state.Provs[p].Owner = (Sys3Int)(rng.Next(2) + 1);
            return Task.CompletedTask;
        }
    }

    /// <summary>Logs every event with its lists spelled out.</summary>
    private sealed class LoggingView(List<string> log) : IScriptView
    {
        public Task ShowAsync(ScriptEvent e)
        {
            if (e is ScreenEvent)
                return Task.CompletedTask;                              // the port's semantic events: no statement in the script
            log.Add(e switch
            {
                ValueMessageEvent v => $"ValueMessageEvent {v.Text} [{string.Join(",", v.Values)}] [{string.Join(",", v.StringVariables ?? [])}]",
                MessageEvent m => $"MessageEvent {m.Text} [{string.Join(",", m.StringVariables ?? [])}]",
                EngineEvent g => $"EngineEvent {g.Command} [{string.Join(",", g.Args)}]",
                _ => e.ToString(),
            });
            return Task.CompletedTask;
        }
    }

    private static IEnumerable<string> Differences(string file, ScriptMemory expected, ScriptMemory actual)
    {
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            if (expected.Var(v) != actual.Var(v))
                yield return $"VAR{v:D4} {VariableCatalog.Variables[v].Name}: {file} {expected.Var(v).Value}, port {actual.Var(v).Value}";
        foreach (int page in expected.AllocatedPages)
            for (int i = 0; i < expected.PageSize(page); i++)
                if (expected.ReadPage(page, i) != actual.ReadPage(page, i))
                    yield return $"page {page}[{i}]: {file} {expected.ReadPage(page, i).Value}, port {actual.ReadPage(page, i).Value}";
        for (int s = 0; s < expected.Strings.Length; s++)
            if (expected.Strings[s] != actual.Strings[s])
                yield return $"string {s}: {file} \"{expected.Strings[s]}\", port \"{actual.Strings[s]}\"";
    }
}
