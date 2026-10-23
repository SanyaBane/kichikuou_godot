using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Kichikuou.TestSupport;
using Xunit.Abstractions;
using static Kichikuou.TestSupport.Differential;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The hand port of <c>MAIN.ADV</c> (<see cref="WeekLoop"/>) against the file itself run from the
/// player's decompile by <see cref="SceneRunner"/>, on random states: both loops run for one to
/// three weeks — thieves' weeks and Leazas weeks with every phase gate open now and then — and must
/// make the same input requests and draws, call the same files with the same <c>Ev_Entry</c>,
/// show the same events and leave the same memory. Every other file is a hook that scribbles over
/// what <c>COMMAND</c>, the save screen, the battles, the nation turns and the scenes may change —
/// the units and commanders, the money, the cursors, scratch, the music, the story flags. A run
/// stops at the autosave of the week after the last one. Skipped without the decompile.
/// </summary>
public class WeekLoopDifferentialTests(ITestOutputHelper output)
{
    private const string Main = "MAIN.ADV";
    private const int Cases = 3000;

    [ScenarioFact]
    public async Task The_port_runs_MAIN_exactly_as_the_decompile()
    {
        var scenario = new ScenarioScripts(LocalData.ScenarioDirectory);
        var program = scenario.ReadProgram(Main);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        int cases = DifferentialRuns.Cases(Cases);

        for (int seed = 1; seed <= cases; seed++)
        {
            var rng = new Random(seed);
            var state = RandomState(rng);
            int weeks = rng.Next(1, 4);
            var port = await RunAsync(ScriptState.Copy(state), seed, weeks, null, scenario);
            var original = await RunAsync(ScriptState.Copy(state), seed, weeks, program, scenario);

            int at = FirstDifference(original.Log, port.Log);
            Assert.True(at < 0, $"seed {seed}: the runs differ at entry {at}\n{Around(Main, original.Log, port.Log, at)}");
            var differences = Differences(Main, original.State.Memory, port.State.Memory, v => VariableCatalog.Variables[v].Name).Take(10).ToList();
            Assert.True(differences.Count == 0, $"seed {seed}: memory differs\n  " + string.Join("\n  ", differences));

            foreach (string feature in Features(port.Log).Distinct())
                seen[feature] = seen.GetValueOrDefault(feature) + 1;
        }

        output.WriteLine($"{cases} runs");
        foreach (var (feature, count) in seen.OrderBy(f => f.Key, StringComparer.Ordinal))
            output.WriteLine($"  {feature}: {count}");
        string[] expected =
        [
            "call ﾛｰﾄﾞｾｰﾌﾞ.ADV", "call COMMAND.ADV", "call Iﾊﾝﾗﾝ.ADV", "stop weeks", "weeks 2+", "music", "no music", "key",
            "wait", "rnd", "leazas week", "thieves' week",
            "call Gﾌｳｶ.ADV", "call Gｶﾄﾞﾐｭｳﾑ.ADV", "call Gｸﾘｰﾑ.ADV", "call Gｶｾｲ.ADV", "call GAL.ADV",
            "call TCﾘｰｻﾞｽ.ADV", "call TJAPAN.ADV", "call Tﾍﾙﾏﾝ.ADV", "call Tﾊﾟｯﾄﾝ.ADV", "call Tｾﾞｽ.ADV",
            "call Tｹｲﾌﾞﾘｽ.ADV", "call Tﾙﾄﾞﾗ.ADV", "call FIGT_B.ADV", "call Gｼﾞｰｸ.ADV", "call Gｻｲｾﾞﾙ.ADV",
            "call Gｻﾃﾗ.ADV", "call Gﾚｲ.ADV", "call Gｶﾐｰﾗ.ADV", "call Iﾏｼﾞﾝ.ADV", "call Iﾏｼﾞﾝ2.ADV",
            "call Gｼｨﾙ.ADV", "call Gﾏｼﾞｯｸ.ADV",
        ];
        var missed = expected.Where(e => !seen.ContainsKey(e)).ToList();
        DifferentialRuns.AssertReached(missed, output);
    }

    private sealed record Run(GameState State, List<string> Log);

    private static async Task<Run> RunAsync(GameState state, int seed, int weeks, ScriptProgram? program, ScenarioScripts scenario)
    {
        var log = new List<string>();
        var hooks = new WeekHooks(state, seed, weeks, log);
        var input = new RandomInput(seed, log);
        var view = new LoggingView(log, e => e is ScreenEvent);
        var random = new LoggingRandom(seed * 17 + 3, log);
        try
        {
            if (program is null)
            {
                var script = new OriginalScriptContext(state, random, view, input, hooks);
                await new WeekLoop(script, scenario).RunAsync();
            }
            else
            {
                var runner = new SceneRunner(new OneProgram(program), hooks, file => file == Main);
                var script = new OriginalScriptContext(state, random, view, input, runner);
                runner.Bind(script);
                await runner.RunFileAsync(Main);
            }
            log.Add("end");    // MAIN never returns
        }
        catch (StopException)
        {
        }
        return new Run(state, log);
    }

    private static IEnumerable<string> Features(List<string> log)
    {
        int saves = 0;
        foreach (string entry in log)
        {
            if (entry.StartsWith("call ", StringComparison.Ordinal))
            {
                if (entry.StartsWith("call ﾛｰﾄﾞｾｰﾌﾞ", StringComparison.Ordinal))
                {
                    if (++saves == 2)
                        yield return "weeks 2+";
                    yield return entry.Contains(" Stage 0 ", StringComparison.Ordinal) ? "thieves' week" : "leazas week";
                }
                yield return entry[..entry.IndexOf(" entry", StringComparison.Ordinal)];
            }
            else if (entry.StartsWith("stop ", StringComparison.Ordinal))
                yield return entry;
            else if (entry.StartsWith("MusicEvent", StringComparison.Ordinal))
                yield return "music";
            else if (entry.StartsWith("key ", StringComparison.Ordinal))
                yield return "key";
            else if (entry.StartsWith("wait ", StringComparison.Ordinal))
                yield return "wait";
            else if (entry.StartsWith("rnd ", StringComparison.Ordinal))
                yield return "rnd";
        }
        if (!log.Any(e => e.StartsWith("MusicEvent", StringComparison.Ordinal)))
            yield return "no music";
    }

    /// <summary>
    /// Every unbound variable and the record and history pages random, then: the prologue or a
    /// later stage, the gates of the nation phases and the end-of-week checks open now and then,
    /// units 1-50 and commanders 1-200 with any status and troops near their maximum (and near the
    /// 16-bit ceiling now and then), Leazas and Zeth owning provinces, the history cursor where the
    /// harem may leave it, music on or off.
    /// </summary>
    private static GameState RandomState(Random rng)
    {
        var state = new GameState();
        double zero = rng.NextDouble() * 0.6;
        RandomStates.Variables(state, rng, zero);
        for (int i = 0; i < state.Memory.PageSize(1); i++)
            state.Memory.WritePage(1, i, (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(0, 4)));
        for (int i = 0; i < state.Memory.PageSize(2); i++)
            state.Memory.WritePage(2, i, (Sys3Int)rng.Next(0, 1000));
        RandomStates.NameStrings(state);

        state.StoryStage = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(10) == 0 ? 99 : rng.Next(1, 5));
        state.Week = (Sys3Int)(rng.Next(10) == 0 ? rng.Next(0, 400) : rng.Next(8, 60));
        state.Gold = (Sys3Int)(rng.Next(8) == 0 ? rng.Next(0, 70) : rng.Next(0, 3000));
        state.BgmEnabled = (Sys3Int)rng.Next(0, 2);
        state.Hist_Cur = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(0, 20) : 0);
        state.Hist_PrevCur = (Sys3Int)rng.Next(0, 3);
        state.Prov_Count = (Sys3Int)(rng.Next(10) == 0 ? rng.Next(0, 101) : 100);
        state.LaVaswald_Prov = (Sys3Int)rng.Next(0, 101);
        state.Dgns[(Sys3Int)1].MapX = (Sys3Int)rng.Next(0, 1920);
        state.Dgns[(Sys3Int)1].MapY = (Sys3Int)rng.Next(0, 800);
        state.MapCamX = (Sys3Int)rng.Next(0, 1920);
        state.MapCamY = (Sys3Int)rng.Next(0, 800);
        RandomFlags(state, rng);
        for (int u = 1; u <= 50; u++)
            if (u <= 6 || rng.Next(3) == 0)
                RandomUnit(state.Units[(Sys3Int)u], rng);
        for (int c = 1; c <= 200; c++)
            if (c is >= 90 and <= 98 || rng.Next(3) == 0)
                RandomCmdr(state.Cmdrs[(Sys3Int)c], rng);
        for (int p = 1; p <= 100; p++)
        {
            var prov = state.Provs[(Sys3Int)p];
            prov.Owner = (Sys3Int)(p is >= 66 and <= 70 ? rng.Next(2) * 6 + 1 : rng.Next(3) == 0 ? rng.Next(0, 9) : 1);
            prov.Economy = (Sys3Int)(rng.Next(20) == 0 ? rng.Next(60000, 65536) : rng.Next(0, 40));
            prov.MapX = (Sys3Int)rng.Next(0, 1920);
            prov.MapY = (Sys3Int)rng.Next(0, 800);
        }
        return state;
    }

    /// <summary>The flags of the phase gates and the end-of-week checks, each at a value that opens or closes its gate.</summary>
    private static void RandomFlags(GameState state, Random rng)
    {
        Sys3Int Pick(params int[] values) => (Sys3Int)values[rng.Next(values.Length)];
        state.F_Fuuka_State = Pick(0, 9, 3);
        state.F_Asuka_State = Pick(0, 1, 2);
        state.F_Cream_State = Pick(0, 5, 11, 15, 19, 20, 25);
        state.N_Mars_Score = Pick(0, 1, 2);
        state.N_ALChurch_Stage = Pick(0, 3, 18, 19, 20, 21, 100);
        state.F_MLand_Funded = Pick(0, 0, 1);
        state.F_Japan_State = Pick(0, 0, 1, 10);
        state.F_Helman_War = Pick(0, 0, 1, 2);
        state.F_PattonArmy_State = Pick(0, 0, 1, 5);
        state.F_Zeth_War = Pick(0, 0, 1, 2);
        state.F_Zeth_State = Pick(0, 2, 9);
        state.F_Kayblis_War = Pick(0, 1, 2, 3);
        state.N_DemonInvasion_Stage = Pick(0, 1, 2, 3, 4, 5, 6, 100, 101);
        state.F_Sieg_State = Pick(0, 1, 2);
        state.F_Seizel_State = Pick(0, 1, 2, 3);
        state.N_Miki_Stage = Pick(0, 1, 2);
        state.F_Cell_Arc = Pick(0, 1, 2, 3);
        state.N_Rei_Stage = Pick(0, 1);
        state.F_Camilla_State = Pick(0, 1, 2);
        state.F_SillKidnap_Stage = Pick(0, 1, 2, 3, 4, 5, 6, 7);
        state.F_Magic_State = Pick(0, 1, 2, 3);
        state.N_Xacalite_KillsLeft = Pick(0, 1, 3);
        state.N_Majin_EventTimer = Pick(0, 1, 2, 5);
        state.F_DemonWar_Stage = (Sys3Int)rng.Next(0, 13);
    }

    private static void RandomUnit(UnitRecord unit, Random rng)
    {
        unit.Status = (Sys3Int)rng.Next(0, 3);
        unit.MaxHP = (Sys3Int)rng.Next(0, 200);
        unit.HP = (Sys3Int)rng.Next(0, 200);
        unit.MaxTroops = (Sys3Int)(rng.Next(8) == 0 ? 65535 - rng.Next(0, 3) : rng.Next(0, 3000));
        unit.Troops = (Sys3Int)(rng.Next(3) == 0 ? Math.Max(0, unit.MaxTroops.Value - rng.Next(0, 4)) : rng.Next(0, 3000));
        unit.Recovery = (Sys3Int)(rng.Next(20) == 0 ? rng.Next(30000, 65536) : rng.Next(0, 60));
        unit.Item = (Sys3Int)(rng.Next(4) == 0 ? (rng.Next(2) == 0 ? 48 : 50) : rng.Next(0, 60));
    }

    private static void RandomCmdr(CmdrRecord cmdr, Random rng)
    {
        cmdr.Status = (Sys3Int)rng.Next(0, 3);
        cmdr.MaxHP = (Sys3Int)rng.Next(0, 200);
        cmdr.HP = (Sys3Int)(rng.Next(3) == 0 ? cmdr.MaxHP.Value : rng.Next(0, 200));
        cmdr.MaxTroops = (Sys3Int)(rng.Next(8) == 0 ? 65535 - rng.Next(0, 3) : rng.Next(0, 3000));
        cmdr.Troops = (Sys3Int)(rng.Next(3) == 0 ? Math.Max(0, cmdr.MaxTroops.Value - rng.Next(0, 4)) : rng.Next(0, 3000));
        cmdr.Recovery = (Sys3Int)(rng.Next(20) == 0 ? rng.Next(30000, 65536) : rng.Next(0, 60));
    }

    private sealed class StopException() : Exception("The run is over.");

    /// <summary>Keys and pauses answered at random and logged; <c>MAIN</c> reads no pointer.</summary>
    private sealed class RandomInput(int seed, List<string> log) : UnexpectedInput
    {
        private readonly Random _rng = new(seed * 31 + 7);
        private int _answers;

        public override Task<Sys3Int> WaitForKeyAsync()
        {
            Spend();
            var key = (Sys3Int)(_rng.Next(4) == 0 ? _rng.Next(1, 33) : 0);
            log.Add($"key {key}");
            return Task.FromResult(key);
        }

        public override Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            Spend();
            var key = (Sys3Int)(_rng.Next(5) == 0 ? 1 : 0);
            log.Add($"wait {ticks} {key}");
            return Task.FromResult(key);
        }

        private void Spend()
        {
            if (++_answers > 2000)
                throw new InvalidOperationException("The input budget ran out: the loop does not stop.");
        }
    }

    /// <summary>
    /// The other files as hooks: each call is logged with what it sees, then writes what the save
    /// screen, the player phase, the battles, the nation turns and the scenes write — the same
    /// writes for the same (seed, call) in both runs. The autosave after the last week ends the run.
    /// </summary>
    private sealed class WeekHooks(GameState state, int seed, int weeks, List<string> log) : ScribblingHooks<GameState>(state, seed, log)
    {
        private int _saves;

        protected override void Arrive(string file)
        {
            if (file == "ﾛｰﾄﾞｾｰﾌﾞ.ADV" && State.RetVal == 3 && ++_saves > weeks)
            {
                Log.Add("stop weeks");
                throw new StopException();
            }
        }

        protected override string Describe() =>
            $"RetVal {State.RetVal} CallArg {State.CallArg} Week {State.Week} Stage {State.StoryStage} " +
            $"Funds {State.History[State.Hist_Cur].Funds} Unit_Cur {State.Unit_Cur} Cmdr_Cur {State.Cmdr_Cur} Prov_Cur {State.Prov_Cur} " +
            $"Dgn_Cur {State.Dgn_Cur} Tmp {State.Tmp0001},{State.Tmp0002},{State.Tmp0003} Phase {State.Ev_PhaseNation} " +
            $"Done {State.Ev_WeeklyEventDone} Party {State.Bt_EnemyParty1},{State.Bt_EnemyParty2} Retreat {State.Bt_EnemyRetreatMode} " +
            $"Str10 {State.Strings[10]}";

        protected override void Scribble(string file, Sys3Int entry, Random rng)
        {
            if (rng.Next(2) == 0)
                State.RetVal = (Sys3Int)rng.Next(0, 6);
            if (rng.Next(2) == 0)
                State.CallArg = (Sys3Int)rng.Next(0, 14);
            if (rng.Next(2) == 0)
                State.Tmp0001 = (Sys3Int)rng.Next(0, 600);
            if (rng.Next(2) == 0)
                State.Tmp0021 = (Sys3Int)rng.Next(0, 600);
            if (rng.Next(2) == 0)
                State.Unit_Cur = (Sys3Int)rng.Next(0, 52);
            if (rng.Next(3) == 0)
                State.Cmdr_Cur = (Sys3Int)rng.Next(0, 200);
            if (rng.Next(3) == 0)
                State.Prov_Cur = (Sys3Int)rng.Next(0, 101);
            if (rng.Next(2) == 0)
                State.Dgn_Cur = (Sys3Int)rng.Next(0, 31);
            if (rng.Next(3) == 0)
                State.Ui_WinType = (Sys3Int)rng.Next(0, 2) * 43;
            if (rng.Next(4) == 0)
                State.BgmEnabled = (Sys3Int)rng.Next(0, 2);
            if (rng.Next(4) == 0)
                State.CurrentBgm = (Sys3Int)rng.Next(0, 20);
            if (rng.Next(4) == 0)
                State.FadeSpeed = (Sys3Int)rng.Next(0, 10);
            if (rng.Next(4) == 0)
                State.Ev_WeeklyEventDone = (Sys3Int)rng.Next(0, 2);
            if (rng.Next(6) == 0)
                State.Ev_Entry = (Sys3Int)rng.Next(0, 30);
            for (int c = 90; c <= 98; c++)
                if (rng.Next(8) == 0)
                    RandomCmdr(State.Cmdrs[(Sys3Int)c], rng);
            if (file == "COMMAND.ADV")
            {
                State.Gold = (Sys3Int)rng.Next(0, 300);
                if (rng.Next(3) == 0)
                    State.Hist_Cur = (Sys3Int)rng.Next(0, 20);     // the harem's girl index (CMD-14)
                for (int u = 1; u <= 6; u++)
                    if (rng.Next(3) == 0)
                        RandomUnit(State.Units[(Sys3Int)u], rng);
                if (rng.Next(5) == 0)
                    State.StoryStage = 99;                          // the lost duel
                else if (rng.Next(20) == 0)
                    State.StoryStage = (Sys3Int)rng.Next(0, 5);
            }
            if (file == "Iﾊﾝﾗﾝ.ADV")
            {
                State.Week = State.Week + 1;
                if (rng.Next(4) == 0)
                    State.StoryStage = 0;                           // back to the thieves, to go on looping
            }
            if (file == "Gｼｨﾙ.ADV" && entry.Value is >= 21 and <= 27 && rng.Next(3) != 0)
                State.F_SillKidnap_Stage = (Sys3Int)(entry.Value - 20);   // each sets the next stage first
            if (file is "Iﾏｼﾞﾝ.ADV" or "Iﾏｼﾞﾝ2.ADV" && rng.Next(2) == 0)
                State.F_DemonWar_Stage = (Sys3Int)rng.Next(0, 13);
            if (file is "Gｶﾐｰﾗ.ADV" or "Gｼﾞｰｸ.ADV" or "Gｻﾃﾗ.ADV" && rng.Next(3) == 0)
                State.F_Camilla_State = (Sys3Int)rng.Next(0, 2);
            if (file.StartsWith('T') && rng.Next(4) == 0)
                State.Strings[10] = $"army {rng.Next(10)}";
        }
    }
}
