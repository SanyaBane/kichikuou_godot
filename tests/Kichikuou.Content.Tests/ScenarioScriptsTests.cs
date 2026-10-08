using Kichikuou.Content.Setup;
using Kichikuou.Core.Golden;
using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// <c>ScenarioScripts</c>: the restricted reader of the data files, on a synthetic scenario and on
/// the player's own files (the boot built from them against what the original had in memory).
/// </summary>
public sealed class ScenarioScriptsTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("kichikuou-scripts").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private ScenarioScripts Write(string file, params string[] lines)
    {
        File.WriteAllLines(Path.Combine(_dir, file), lines);
        return new ScenarioScripts(_dir);
    }

    [Fact]
    public void Reads_every_supported_form()
    {
        var scripts = Write("DATA.ADV",
            "\t!VAR0158 : 2!",
            "\tZH 1:",
            "\tMS 8, \"Custom Castle\":",
            "\tR",
            "\tMM VAR0200 + VAR0158 - 1, 8:",
            "\t#L_00010, 1:",
            "\tF3, VAR0142, (VAR0158 - 1) * 2 + 1:",
            "\t%#M.ADV:",
            "\t%0:",
            "*L_00010:",
            "\t_L_00020:",
            "*L_00020:",
            "\t[5, 6,",
            "\t 7, 8]");

        var statements = scripts.ReadFile("DATA.ADV");

        Assert.Equal(7, statements.Count);
        Assert.Equal(new AssignStatement(new("DATA.ADV", 1), 158, new ConstExpr(2)), statements[0]);
        Assert.Equal(new SetStringStatement(new("DATA.ADV", 3), 8, "Custom Castle"), statements[1]);
        var mm = Assert.IsType<CopyStringStatement>(statements[2]);
        Assert.Equal(new BinaryExpr('-', new BinaryExpr('+', new VarExpr(200), new VarExpr(158)), new ConstExpr(1)), mm.Target);
        Assert.Equal([(Sys3Int)5, (Sys3Int)6, (Sys3Int)7, (Sys3Int)8], Assert.IsType<SetDataTableStatement>(statements[3]).Words);
        var f3 = Assert.IsType<ReadDataStatement>(statements[4]);
        Assert.Equal(new BinaryExpr('+',
            new BinaryExpr('*', new BinaryExpr('-', new VarExpr(158), new ConstExpr(1)), new ConstExpr(2)),
            new ConstExpr(1)), f3.Index);
        Assert.Equal("M.ADV", Assert.IsType<CallFileStatement>(statements[5]).File);
        Assert.IsType<ReturnStatement>(statements[6]);
    }

    [Theory]
    [InlineData("\t{VAR0001 = 1:")]
    [InlineData("\t!VAR0001 : VAR0002 = 3!")]
    [InlineData("\tY 4, 3:")]
    [InlineData("\t'A message'")]
    public void Rejects_anything_else_with_its_location(string line)
    {
        var scripts = Write("BAD.ADV", "\t!VAR0001 : 1!", line, "\t%0:");

        var e = Assert.Throws<ScenarioFormatException>(() => scripts.ReadFile("BAD.ADV"));

        Assert.Contains("BAD.ADV:2", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reads_one_statement_by_location()
    {
        var scripts = Write("SET.ADV", "\tZH 1:", "\tMS 1, \"General\\\"s Name\":");

        var statement = scripts.ReadStatement(new("SET.ADV", 2));

        Assert.Equal(new SetStringStatement(new("SET.ADV", 2), 1, "General\"s Name"), statement);
    }
}

/// <summary>The boot built from the player's decompile (skipped without it).</summary>
public class PlayerBootTests(ITestOutputHelper output)
{
    private static async Task<GameState?> BootAsync()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return null;
        var state = new GameState();
        var script = new ScriptContext(state, new NoRandom(), new NullView(), new NoInput(), new NoHooks());
        await new GameSetup(script, new ScenarioScripts(LocalData.ScenarioDirectory)).BootAsync();
        return state;
    }

    [Fact]
    public async Task The_boot_builds_the_initial_world()
    {
        var s = await BootAsync();
        if (s is null)
            return;

        Is(50, s.Unit_Cur);
        Is(199, s.Cmdr_Cur);
        Is(50, s.Item_Cur);
        Is(91, s.Prov_Cur);
        Is(26, s.Dgn_Cur);
        Is(90, s.Ui_MarkCount);
        Assert.Equal([1, 2, 3, 4], Enumerable.Range(1, 50).Where(u => s.Units[u].Status == 1));
        Assert.Equal(190, Enumerable.Range(1, 199).Count(c => s.Cmdrs[c].Status == 1));
        Assert.Equal(17, Enumerable.Range(1, 91).Count(p => s.Provs[p].Owner == 1));
        Is(30000, s.Units[32].MaxHP); // Miki: 10000 * 3
        Assert.Equal(635, Enumerable.Range(20, 966).Count(i => s.Strings[i].Length > 0));
    }

    /// <summary>
    /// The first prologue battle's entry dump holds the boot world as changed by one week of play:
    /// the troops and status of units 1/3/4 and the troops of commander 58 (survey-data §8.1).
    /// </summary>
    [Fact]
    public async Task The_boot_world_matches_the_original_up_to_the_first_week_of_play()
    {
        string capture = Path.Combine(LocalData.GoldenDirectory, "20261008-095048-01-FIGT_A.golden");
        var s = await BootAsync();
        if (s is null || !File.Exists(capture))
            return;
        var enter = GoldenCapture.Load(capture).Enter;

        var differ = new List<string>();
        var page = enter.Pages[1];
        for (int i = 0; i < page.Length; i++)
        {
            if (page[i] != s.Memory.ReadPage(1, i).Value)
                differ.Add($"{VariableCatalog.DescribeSlot(1, i)}: original {page[i]}, boot {s.Memory.ReadPage(1, i).Value}");
        }
        foreach (string d in differ)
            output.WriteLine(d);
        // Week 9's refill (+2 troops, MAIN.ADV:50-64) of units 1/3/4, the party sent to battle
        // (status 2, Sﾌﾞﾀｲ), and Iﾄｳｿﾞｸ's reset of the Civilian General to 10 troops.
        Assert.Equal(
            [
                "Units[1].Troops: original 52, boot 50",
                "Units[3].Troops: original 23, boot 21",
                "Units[4].Troops: original 19, boot 17",
                "Units[1].Status: original 2, boot 1",
                "Units[2].Status: original 2, boot 1",
                "Units[3].Status: original 2, boot 1",
                "Cmdrs[58].Troops: original 10, boot 500",
            ],
            differ);
        for (int n = 20; n <= 985; n++)
            Assert.Equal(enter.Strings.GetValueOrDefault(n, string.Empty), s.Strings[n]);
        // The 90 province markers SET_MAP adds through M.ADV op 4 (page 10), and the shadow slot.
        var markers = enter.Pages[10];
        for (int i = 0; i < markers.Length; i++)
            Assert.True(markers[i] == s.Memory.ReadPage(10, i).Value, VariableCatalog.DescribeSlot(10, i));
        Assert.Equal(enter.Vars[212], s.Ui_MarkCount.Value);
        Assert.Equal(500, enter.Vars[134]);
        Assert.Equal(500, s.Memory.Var(134).Value);
    }

    /// <summary>
    /// The boot captures (group <c>boot</c>, root <c>SET.ADV</c>) replayed through
    /// <c>GameSetup.SetAsync</c> with the record files from the player's decompile.
    /// </summary>
    [Fact]
    public async Task The_boot_captures_replay_through_SET()
    {
        string dir = Path.Combine(LocalData.GoldenDirectory, "boot");
        if (!Directory.Exists(dir) || !File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var captures = Directory.EnumerateFiles(dir, "*.golden").Order(StringComparer.Ordinal).ToList();
        foreach (string path in captures)
        {
            var report = await GoldenReplay.RunAsync(GoldenCapture.Load(path),
                entryPoint: script => new GameSetup(script, new ScenarioScripts(LocalData.ScenarioDirectory)).SetAsync());
            output.WriteLine($"{Path.GetFileName(path)}:");
            output.WriteLine(report.Describe(maxLines: 200));
            Assert.True(report.Passed, report.Describe());
        }
    }

    private static void Is(int expected, Sys3Int actual) => Assert.Equal(expected, (int)actual);

    private sealed class NoRandom : IScriptRandom
    {
        public Sys3Int Next(Sys3Int n) => throw new InvalidOperationException("The boot draws nothing.");
    }

    private sealed class NullView : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent) => Task.CompletedTask;
    }

    private sealed class NoInput : IScriptInput
    {
        public Task<Sys3Int> WaitForKeyAsync() => throw new InvalidOperationException();
        public Task<Sys3Int> PauseAsync(Sys3Int ticks) => throw new InvalidOperationException();
        public Task<Sys3Int> GetPlayingTrackAsync() => throw new InvalidOperationException();
    }

    private sealed class NoHooks : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry) => Task.CompletedTask;
    }
}
