using Kichikuou.Content.Scripts;
using Kichikuou.Core.Script;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// <c>ScenarioScripts.ReadProgram</c>: scenario files as goto programs (part E1), on synthetic
/// files and on every scene file of the player's decompile.
/// </summary>
public sealed class ScenarioProgramTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("kichikuou-programs").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private ScriptProgram Read(params string[] lines)
    {
        File.WriteAllLines(Path.Combine(_dir, "SCENE.ADV"), lines);
        return new ScenarioScripts(_dir).ReadProgram("SCENE.ADV");
    }

    private static ScriptRef At(int line) => new("SCENE.ADV", line);

    private static VarExpr V(int n) => new(n);

    private static ConstExpr C(int n) => new((Sys3Int)n);

    [Fact]
    public void Blocks_jumps_and_calls_resolve_to_statement_indices()
    {
        var p = Read(
            "\t{VAR0520 = 1:",       // 0
            "\t\t@L_00020:",          // 1
            "\t}",
            "\t{VAR0520 = 2:",       // 2
            "\t\t@L_00030:",          // 3
            "\t}",
            "*L_00010:",
            "\t\\L_00040:",           // 4
            "*L_00020:",
            "\t{VAR0001:",           // 5
            "*L_00030:",
            "\t\t!VAR0002 : 3!",      // 6   (a label inside a block)
            "\t}",
            "\t%0:",                  // 7
            "*L_00040:",
            "\t\\0:",                 // 8
            "\t&#OVER.ADV:");         // 9

        Assert.Equal(10, p.Statements.Count);
        Assert.Equal(new IfStatement(At(1), new BinaryExpr("=", V(520), C(1)), 2), p.Statements[0]);
        Assert.Equal(new GotoStatement(At(2), "L_00020", 5), p.Statements[1]);
        Assert.Equal(new CallLabelStatement(At(8), "L_00040", 8), p.Statements[4]);
        Assert.Equal(new IfStatement(At(10), V(1), 7), p.Statements[5]);
        Assert.Equal(new AssignStatement(At(12), 2, C(3)), p.Statements[6]);
        Assert.IsType<ReturnStatement>(p.Statements[7]);
        Assert.IsType<ReturnLabelStatement>(p.Statements[8]);
        Assert.Equal("OVER.ADV", Assert.IsType<JumpFileStatement>(p.Statements[9]).File);
        Assert.Equal(6, p.Labels["L_00030"]);
        Assert.Equal(4, p.Labels["L_00010"]);
        Assert.Equal(
            [new SceneEntry((Sys3Int)1, "L_00020", 5), new SceneEntry((Sys3Int)2, "L_00030", 6)],
            p.Entries);
    }

    [Fact]
    public void Text_commands_form_runs_with_their_computed_values()
    {
        var p = Read(
            "\tZH 1:",
            "\t'Hello'",               // 2: run 1 starts
            "\tR",
            "\tX 1:",
            "\tZH 1:",
            "\tH 0, VAR0001 + 2:",
            "\tX VAR0154:",
            "\tZM 24:",                // 8: ends the run
            "\tZH 1:",
            "\tR",                     // 10: run 2 starts at the line break
            "\t'Next'",
            "\t\\L_00001:",
            "\tR",                     // 13: a lone line break
            "*L_00001:",
            "\t\\0:");

        Assert.Equal(
            new MessageStatement(At(2), new ScriptText(At(2), [new BinaryExpr("+", V(1), C(2))], [C(1), V(154)])),
            p.Statements[0],
            new MessageComparer());
        Assert.Equal(new EngineStatement(At(8), "ZM", [C(24)]), p.Statements[1], new EngineComparer());
        Assert.Equal(At(10), Assert.IsType<MessageStatement>(p.Statements[2]).Text.Start);
        Assert.IsType<CallLabelStatement>(p.Statements[3]);
        Assert.Equal(new LineBreakStatement(At(13)), p.Statements[4]);
    }

    [Fact]
    public void Menus_register_items_around_ordinary_text()
    {
        var p = Read(
            "*L_00010:",
            "\tIM VAR0202, VAR0203:",   // 0
            "\t{VAR0000:",              // 1
            "\t\t@L_00010:",            // 2
            "\t}",
            "\tZH 1:",
            "\t$L_00020$",              // 3
            "\t'Golden Axe'",           // 4
            "\t$",                      // 5
            "\tR",                      // 6
            "\t{VAR0300 = 1:",          // 7
            "\t\t$L_00030$",            // 8
            "\t\t\\L_00040:",           // 9  (the item's text printed by a call)
            "\t\t$",                    // 10
            "\t}",
            "\t]",                      // 11
            "\t@L_00010:",              // 12
            "*L_00020:",
            "\t%0:",                    // 13
            "*L_00030:",
            "\t%0:",                    // 14
            "*L_00040:",
            "\tX 2:",                   // 15
            "\t\\0:");                  // 16

        Assert.Equal(new MouseStatement(At(2), 202, 203), p.Statements[0]);
        Assert.Equal(new MenuItemStatement(At(7), "L_00020", 13), p.Statements[3]);
        Assert.Equal(At(8), Assert.IsType<MessageStatement>(p.Statements[4]).Text.Start);
        Assert.Equal(new MenuItemEndStatement(At(9)), p.Statements[5]);
        Assert.Equal(new LineBreakStatement(At(10)), p.Statements[6]);
        Assert.Equal(new MenuItemStatement(At(12), "L_00030", 14), p.Statements[8]);
        Assert.Equal(15, Assert.IsType<CallLabelStatement>(p.Statements[9]).Target);
        Assert.Equal(11, Assert.IsType<IfStatement>(p.Statements[7]).EndTarget);
        Assert.IsType<MenuStatement>(p.Statements[11]);
    }

    [Fact]
    public void Commands_become_their_statements()
    {
        var p = Read(
            "\tY 4, VAR0074 / 6:",
            "\tY 3, 40:",
            "\tY 1, 1:",
            "\tA",
            "\tIK 0:",
            "\tSS VAR0517:",
            "\tSR1, VAR0011:",
            "\tSC VAR0040:",
            "\tMS 10, \"Leazas \\\"Army\\\"\":",
            "\tMM 1, VAR0198 + (VAR0154 - 1) * 3:",
            "\tMA 4, 5:",
            "\tMH 6, 3, VAR0001:",
            "\tNC VAR0001, 3:",
            "\tNB VAR0010, VAR0020, 4:",
            "\tUS VAR0045, 4:",
            "\tUG VAR0045, 4:",
            "\tLP 1, VAR0100, 2:",
            "\tQC 2, 1:",
            "\tLT VAR0000, VAR0001:",
            "\tZT0 VAR0021:",
            "\tZT1 0:",
            "\tZT5 VAR0000:",
            "\tZZ0, 0:",
            "\tCC 0, 1, 2, 3, 4, 5:",
            "\t%#WAO_SUB.ADV:",
            "\t%0:");

        Assert.Equal(new RandomStatement(At(1), new BinaryExpr("/", V(74), C(6))), p.Statements[0]);
        Assert.Equal(new PauseStatement(At(2), C(40)), p.Statements[1]);
        Assert.Equal(new MessageResetStatement(At(3), C(1)), p.Statements[2]);
        Assert.IsType<HitAnyKeyStatement>(p.Statements[3]);
        Assert.IsType<KeyWaitStatement>(p.Statements[4]);
        Assert.Equal(new MusicStatement(At(6), V(517)), p.Statements[5]);
        Assert.Equal(new MusicInfoStatement(At(7), 1, 11), p.Statements[6]);
        Assert.Equal(new CdPositionStatement(At(8), 40), p.Statements[7]);
        Assert.Equal(new SetStringStatement(At(9), 10, "Leazas \"Army\""), p.Statements[8]);
        Assert.IsType<CopyStringStatement>(p.Statements[9]);
        Assert.Equal(new AppendStringStatement(At(11), C(4), C(5)), p.Statements[10]);
        Assert.Equal(new NumberToStringStatement(At(12), C(6), C(3), V(1)), p.Statements[11]);
        Assert.Equal(new ClearVariablesStatement(At(13), 1, C(3)), p.Statements[12]);
        Assert.Equal(new CopyVariablesStatement(At(14), 10, 20, C(4)), p.Statements[13]);
        Assert.Equal(new LocalVariablesStatement(At(15), true, 45, C(4)), p.Statements[14]);
        Assert.Equal(new LocalVariablesStatement(At(16), false, 45, C(4)), p.Statements[15]);
        Assert.Equal(new SaveDataStatement(At(17), "LP", C(1), 100, C(2)), p.Statements[16]);
        Assert.Equal(new SaveDataStatement(At(18), "QC", C(2), Other: C(1)), p.Statements[17]);
        Assert.Equal(new SaveDataStatement(At(19), "LT", V(0), 1), p.Statements[18]);
        Assert.Equal(new DateStatement(At(20), 21), p.Statements[19]);
        Assert.Equal(new TimerResetStatement(At(21), C(0)), p.Statements[20]);
        Assert.Equal(new TimerStatement(At(22), 5, 0), p.Statements[21]);
        Assert.Equal(new QuitStatement(At(23), C(0)), p.Statements[22]);
        Assert.Equal(new EngineStatement(At(24), "CC", [C(0), C(1), C(2), C(3), C(4), C(5)]), p.Statements[23], new EngineComparer());
        Assert.Equal(new CallFileStatement(At(25), "WAO_SUB.ADV"), p.Statements[24]);
    }

    /// <summary>
    /// The title's forms (<c>ｽﾀMENU.ADV</c>, <c>ﾌﾟﾛﾛｰｸﾞ.ADV</c>): <c>MC</c>, <c>ZZ3</c>, <c>WW</c>, <c>CE</c>;
    /// <c>#label, n</c> naming the n-th address word; word tables in the code area, one line or
    /// several, read as data the way <c>_label</c> and <c>"text"</c> lines are.
    /// </summary>
    [Fact]
    public void The_title_s_commands_tables_and_address_words_are_read()
    {
        var p = Read(
            "\tMC 1, 2:",                                   // 0
            "\tZZ3, VAR0001:",                              // 1
            "\tWW 3200, 800, 8:",                           // 2
            "\tCE 640, 0, 640, 400, 0, 0, 4, 0, 1:",        // 3
            "\t#L_00010, 1:",                               // 4
            "\t#L_00010, 2:",                               // 5
            "\t%0:",                                        // 6
            "*L_00010:",
            "\t_L_00020:",                                  // 7
            "\t_L_00030:",                                  // 8
            "*L_00020:",
            "\t[15, 7,",                                    // 9
            "\t 9]",                                        // 10
            "*L_00030:",
            "\t\"Don’t play\"",                             // 11
            "\t\"／／\"",                                    // 12
            "*L_00040:",
            "\t[1]");                                       // 13

        Assert.Equal(14, p.Statements.Count);
        Assert.Equal(new CompareStringsStatement(At(1), C(1), C(2)), p.Statements[0]);
        Assert.Equal(new DisplayInfoStatement(At(2), 1), p.Statements[1]);
        Assert.Equal(new EngineStatement(At(3), "WW", [C(3200), C(800), C(8)]), p.Statements[2], new EngineComparer());
        Assert.Equal([C(640), C(0), C(640), C(400), C(0), C(0), C(4), C(0), C(1)], Assert.IsType<EffectCopyStatement>(p.Statements[3]).Args);
        Assert.Equal([(Sys3Int)15, (Sys3Int)7, (Sys3Int)9], Assert.IsType<SetDataTableStatement>(p.Statements[4]).Words);
        Assert.Equal(["Don’t play", "／／"], Assert.IsType<SetStringTableStatement>(p.Statements[5]).Strings);
        Assert.All(p.Statements.Skip(7), s => Assert.IsType<DataStatement>(s));
        Assert.Equal(9, p.Labels["L_00020"]);
        Assert.Equal(13, p.Labels["L_00040"]);
    }

    [Fact]
    public void An_address_word_past_the_label_s_words_or_an_open_table_fails()
    {
        Assert.Contains("SCENE.ADV:1: no address word 2", Assert.Throws<ScenarioFormatException>(
            () => Read("\t#L_00010, 2:", "\t%0:", "*L_00010:", "\t_L_00020:", "*L_00020:", "\t[1]")).Message, StringComparison.Ordinal);
        Assert.Contains("word table is not closed", Assert.Throws<ScenarioFormatException>(
            () => Read("\t%0:", "*L_00010:", "\t[1, 2,")).Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("VAR0001 + 2 * 3", "(VAR0001 + (2 * 3))")]
    [InlineData("VAR0001 - VAR0002 \\ 0", "((VAR0001 - VAR0002) \\ 0)")]
    [InlineData("(VAR0001 = 1) * (VAR0002 \\ 3)", "((VAR0001 = 1) * (VAR0002 \\ 3))")]
    [InlineData("VAR0001 / 10 - 5 > 3", "(((VAR0001 / 10) - 5) > 3)")]
    [InlineData("1 - 2 - 3", "((1 - 2) - 3)")]
    [InlineData("VAR0001 & 3 = 1", "((VAR0001 & 3) = 1)")]
    [InlineData("VAR0001 < 2 = VAR0002 >= 3", "((VAR0001 < 2) = (VAR0002 >= 3))")]
    [InlineData("VAR0001 % 4 + 1", "((VAR0001 % 4) + 1)")]
    public void Expressions_follow_the_decompiler_precedence(string text, string expected)
    {
        var p = Read($"\t!VAR0009 : {text}!");

        Assert.Equal(expected, Show(Assert.IsType<AssignStatement>(p.Statements[0]).Value));
    }

    [Theory]
    [InlineData("\tY 2, 3:")]
    [InlineData("\tIK 1:")]
    [InlineData("\tIM 1, VAR0003:")]
    [InlineData("\tCC 0, 1:")]
    [InlineData("\tQQ 1:")]
    [InlineData("\tZH 0:")]
    [InlineData("\t@L_00099:")]
    [InlineData("\t}")]
    [InlineData("\t{VAR0001:")]
    [InlineData("\t$L_1$")]
    [InlineData("\tMP VAR0001, 4:")]
    [InlineData("\t!VAR0001 : VAR0002 && 3!")]
    [InlineData("\t#L_00010, 1:")]
    public void Rejects_anything_else_with_its_location(string line)
    {
        var e = Assert.Throws<ScenarioFormatException>(() => Read("*L_00001:", line, "\t%0:"));

        Assert.Contains("SCENE.ADV:2", e.Message, StringComparison.Ordinal);
    }

    private static string Show(ScriptExpr e) => e switch
    {
        ConstExpr c => c.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        VarExpr v => $"VAR{v.Var:D4}",
        BinaryExpr b => $"({Show(b.Left)} {b.Op} {Show(b.Right)})",
        _ => throw new ArgumentException(e.ToString()),
    };

    /// <summary>Records holding lists compare the lists by reference; these compare their items.</summary>
    private sealed class MessageComparer : IEqualityComparer<ScriptStatement>
    {
        public bool Equals(ScriptStatement? x, ScriptStatement? y) =>
            x is MessageStatement a && y is MessageStatement b && a.At == b.At && a.Text.Start == b.Text.Start
            && a.Text.Numbers.SequenceEqual(b.Text.Numbers) && a.Text.Strings.SequenceEqual(b.Text.Strings);

        public int GetHashCode(ScriptStatement obj) => 0;
    }

    private sealed class EngineComparer : IEqualityComparer<ScriptStatement>
    {
        public bool Equals(ScriptStatement? x, ScriptStatement? y) =>
            x is EngineStatement a && y is EngineStatement b && a.At == b.At && a.Command == b.Command && a.Args.SequenceEqual(b.Args);

        public int GetHashCode(ScriptStatement obj) => 0;
    }
}

/// <summary>
/// Every scene file of the player's decompile (the 136 <c>G*</c>/<c>I*</c> files) and the other
/// files that run through the event runtime (<c>ｵｰﾌﾟﾆﾝｸﾞ</c>, <c>FIGT表示</c>, <c>ﾛｰﾄﾞｾｰﾌﾞ</c>, <c>OVER</c>,
/// the title <c>ｽﾀMENU</c> and its logo <c>ﾌﾟﾛﾛｰｸﾞ</c>) read as programs, and every text they show
/// resolves (skipped without the decompile).
/// </summary>
public class ScenarioProgramSweepTests(ITestOutputHelper output)
{
    private static readonly string[] OtherFiles = ["ｵｰﾌﾟﾆﾝｸﾞ.ADV", "FIGT表示.ADV", "ﾛｰﾄﾞｾｰﾌﾞ.ADV", "OVER.ADV", "ｽﾀMENU.ADV", "ﾌﾟﾛﾛｰｸﾞ.ADV"];

    private static readonly string[] Milestone1 =
        ["Iﾄｳｿﾞｸ.ADV", "Iﾌﾟﾛﾛｰｸﾞ.ADV", "Iｼﾚｲﾌﾞ.ADV", "Iﾊﾝﾗﾝ.ADV", "Gｼｨﾙ.ADV", "Gｿｳﾙ.ADV", "Gﾊﾞｳﾝﾄﾞ.ADV", "Gｱﾅｾﾙ.ADV"];

    private static List<string> SceneFiles() =>
        Directory.EnumerateFiles(LocalData.ScenarioDirectory, "*.ADV")
            .Select(Path.GetFileName)
            .Where(f => f![0] is 'G' or 'I')
            .Order(StringComparer.Ordinal)
            .ToList()!;

    [Fact]
    public void Every_scene_file_reads_as_a_program_and_its_texts_resolve()
    {
        if (!Directory.Exists(LocalData.ScenarioDirectory))
            return;
        var scenes = SceneFiles();
        Assert.Equal(136, scenes.Count);
        Assert.Empty(Milestone1.Concat(OtherFiles).Except(scenes.Concat(OtherFiles)));
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var text = new ScenarioText(LocalData.ScenarioDirectory);
        var failures = new List<string>();
        var programs = new Dictionary<string, ScriptProgram>(StringComparer.Ordinal);
        foreach (string file in scenes.Concat(OtherFiles))
        {
            try
            {
                var program = scripts.ReadProgram(file);
                programs[file] = program;
                foreach (var t in program.Statements.OfType<MessageStatement>().Select(m => m.Text))
                {
                    var run = text.Read(t.Start);
                    int strings = run.Segments.Count(s => s is StringSlot);
                    if (run.NumberCount != t.Numbers.Count || strings != t.Strings.Count)
                        failures.Add($"{t.Start}: run prints {run.NumberCount} numbers / {strings} strings, the statement has {t.Numbers.Count} / {t.Strings.Count}");
                }
            }
            catch (Exception e) when (e is ScenarioFormatException or ScenarioTextException)
            {
                failures.Add(e.Message);
            }
        }
        foreach (string f in failures.Take(50))
            output.WriteLine(f);
        Assert.Empty(failures);

        // The survey's counts (survey-events.md §2.1, §5, §6.1) over the 136 scene files.
        var sceneStatements = scenes.SelectMany(f => programs[f].Statements).ToList();
        Assert.Equal(795, scenes.Sum(f => programs[f].Entries.Count));
        Assert.Equal(125, scenes.Count(f => programs[f].Entries.Count > 0));
        Assert.Equal(108, sceneStatements.Count(s => s is RandomStatement));
        Assert.Equal(123, sceneStatements.Count(s => s is MenuStatement));
        Assert.Equal(426, sceneStatements.Count(s => s is MenuItemStatement));
        Assert.Equal(14, sceneStatements.Count(s => s is JumpFileStatement { File: "OVER.ADV" }));
        output.WriteLine($"{programs.Count} files, {programs.Values.Sum(p => p.Statements.Count)} statements, "
            + $"{programs.Values.Sum(p => p.Statements.Count(s => s is MessageStatement))} messages.");
    }

    /// <summary>
    /// Every file the scenes call (the scene files, <c>ｵｰﾌﾟﾆﾝｸﾞ</c>, <c>FIGT表示</c>) runs somewhere:
    /// it is a scene, a file the router ports or a helper of the context — none falls to the
    /// unported hooks (<c>FIGTATTK</c>, the battle's animation, did until 2026-10-11).
    /// </summary>
    [Fact]
    public void Every_file_the_scenes_call_is_a_scene_a_port_or_a_helper()
    {
        if (!Directory.Exists(LocalData.ScenarioDirectory))
            return;
        string[] helpers = ["M.ADV", "WAO_MAP.ADV", "WAO_SUB.ADV", "PCMPLAY.ADV"];
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var missing = SceneFiles().Concat(["ｵｰﾌﾟﾆﾝｸﾞ.ADV", "FIGT表示.ADV"])
            .SelectMany(f => scripts.ReadProgram(f).Statements.OfType<CallFileStatement>())
            .Where(c => !SceneRunner.IsSceneFile(c.File) && !StrategyRules.PortedFiles.Contains(c.File) && !helpers.Contains(c.File))
            .GroupBy(c => c.File)
            .Select(g => $"{g.Key}: {g.Count()} calls, e.g. {g.First().At}")
            .ToList();
        Assert.Empty(missing);
    }

    /// <summary>
    /// The title's calls run somewhere too, but for the gallery and the music room (<c>CG</c>,
    /// <c>MMODE</c>; later, owner's decision 19), Fortune Check (<c>HAPPY_C</c>, step 3) and the dead
    /// <c>XSPEC</c> (<c>ｽﾀMENU.ADV:179</c>; the boot does not run it either, DAT-04).
    /// </summary>
    [Fact]
    public void The_title_calls_only_files_that_run_but_those_left_for_later()
    {
        if (!Directory.Exists(LocalData.ScenarioDirectory))
            return;
        string[] helpers = ["M.ADV", "WAO_MAP.ADV", "WAO_SUB.ADV", "PCMPLAY.ADV"];
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var missing = new[] { "ｽﾀMENU.ADV", "ﾌﾟﾛﾛｰｸﾞ.ADV" }
            .SelectMany(f => scripts.ReadProgram(f).Statements.OfType<CallFileStatement>())
            .Select(c => c.File)
            .Where(f => !SceneRunner.IsSceneFile(f) && !StrategyRules.PortedFiles.Contains(f) && !helpers.Contains(f))
            .Distinct()
            .Order(StringComparer.Ordinal);
        Assert.Equal(["CG.ADV", "HAPPY_C.ADV", "MMODE.ADV", "XSPEC.ADV"], missing);
    }

    /// <summary>
    /// The rule files of the strategic layer read as programs too, so the differential tests can
    /// run them against their ports (<c>COM</c>, <c>COMMAND</c>) and the unported ones can run
    /// as programs while a part is checked.
    /// </summary>
    [Fact]
    public void The_strategic_rule_files_read_as_programs_and_their_texts_resolve()
    {
        if (!Directory.Exists(LocalData.ScenarioDirectory))
            return;
        string[] files =
        [
            "MAIN.ADV", "COMMAND.ADV", "COM.ADV", "Cﾃｷﾄｼ.ADV", "Cﾃｷﾄｼ2.ADV", "Cﾌﾞｶ.ADV", "Cﾊｰﾚﾑ.ADV",
            "Cﾒｲｷｭｳ.ADV", "Cｼﾊｲﾄｼ.ADV", "Cｼｮｳﾆﾝ.ADV", "Cﾆﾝｼﾞｬ.ADV", "Cﾃｲﾄ.ADV", "Cﾀｶﾗ.ADV",
            "Sﾌﾞﾀｲ.ADV", "Sﾌﾞｶ.ADV", "Sﾊｰﾚﾑ.ADV", "Sｱｲﾃﾑ.ADV", "M.ADV", "WAO_MAP.ADV", "WAO_SUB.ADV",
            // Not yet: 各種DATA (ZB), PCMPLAY (SP).
        ];
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var text = new ScenarioText(LocalData.ScenarioDirectory);
        var failures = new List<string>();
        foreach (string file in files)
        {
            try
            {
                var program = scripts.ReadProgram(file);
                foreach (var t in program.Statements.OfType<MessageStatement>().Select(m => m.Text))
                    text.Read(t.Start);
            }
            catch (Exception e) when (e is ScenarioFormatException or ScenarioTextException)
            {
                failures.Add(e.Message);
            }
        }
        foreach (string f in failures)
            output.WriteLine(f);
        Assert.Empty(failures);
    }
}
