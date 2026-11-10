using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Kichikuou.TestSupport;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// "New scenes first" during real talks and visits (owner's decision 30, docs/ui/talk-marks.md §5, §8 step 5) on the
/// player's files: <c>Cﾌﾞｶ</c>'s Talk and <c>Cﾊｰﾚﾑ</c>'s visit run as the game runs them (our Subordinates screen's
/// order, our Harem screen's Summon) from a booted new game at story stage 1, the story and the read lines set as each
/// case needs; the dry run's mark from the same state says what the real run must play. Skipped without the decompile.
/// </summary>
public class SceneChoicesTests(ITestOutputHelper output)
{
    private static readonly ScriptRef CafeDraw = new("Gｶﾌｪ.ADV", 396);
    private static readonly ScriptRef HaureinDraw = new("Gﾊｳﾚｰﾝ.ADV", 2299);
    private static readonly ScriptRef KentarouDraw = new("Gｹﾝﾀﾛｳ.ADV", 608);
    private static readonly ScriptRef IsorokuDraw = new("Gｲｿﾛｸ.ADV", 4599);
    private static readonly ScriptRef KanamiDraw = new("Gｶﾅﾐ.ADV", 182);

    /// <summary>
    /// Cafe (unit 45) with Satella (unit 29) in the army and the children scene read (§3.2): the Satella scene
    /// (<c>Gｶﾌｪ:396</c>, 1 of 3) comes on the next talk whatever the generator draws — the mark's New and its choice.
    /// </summary>
    [ScenarioFact]
    public async Task Cafes_satella_scene_comes_on_the_next_talk()
    {
        var rig = await BootAsync();
        var start = CafeState(rig);
        var report = rig.Marks.Talk(start, 45, newScenesFirst: true);
        output.WriteLine(report.ToString());
        Assert.Equal(SceneMark.New, report.Mark);
        var choice = Choice(report, CafeDraw);
        Assert.Equal(DrawRule.HarmlessUnread, choice.Rule);
        Assert.Equal([1], choice.Values);
        var satella = LeafWith(report, CafeDraw, 1);

        for (int seed = 1; seed <= 6; seed++)
        {
            var play = rig.Play(start, new LoggingRandom(seed, []), on: true);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            await play.TalkAsync(45);
            output.WriteLine($"seed {seed}: {string.Join("; ", play.Choices.Draws)} ({clock.ElapsedMilliseconds} ms)");
            var draw = Assert.Single(play.Choices.Draws);
            Assert.Equal((CafeDraw, 1, DrawRule.HarmlessUnread), (draw.At, draw.Value, draw.Rule));
            Assert.All(satella.Path.Lines, line => Assert.True(play.State.IsLineRead(line), $"{line} not shown"));
            Assert.Equal(0, play.Choices.Fallbacks);
            Assert.False(play.Choices.Active);
        }
    }

    /// <summary>
    /// The generator moves exactly as in the original: the same draws, one for one, in the same order, with the rule
    /// on or off (a logging generator, several seeds); off, the script gets the generator's value (2: the children, not
    /// Satella), on the Satella scene's 1 from the same draw.
    /// </summary>
    [ScenarioFact]
    public async Task The_generator_draws_the_same_with_the_rule_on_or_off()
    {
        var rig = await BootAsync();
        var start = CafeState(rig);
        for (int seed = 1; seed <= 8; seed++)
        {
            List<string> on = [], off = [];
            var withRule = rig.Play(start, new LoggingRandom(seed, on), on: true);
            await withRule.TalkAsync(45);
            var without = rig.Play(start, new LoggingRandom(seed, off), on: false);
            await without.TalkAsync(45);
            output.WriteLine($"seed {seed}: {string.Join(", ", on)}");
            Assert.NotEmpty(on);
            Assert.Equal(off, on);
            Assert.Empty(without.Choices.Draws);
        }

        var satella = LeafWith(rig.Marks.Talk(start, 45, true), CafeDraw, 1).Path.Lines
            .Where(l => !start.IsLineRead(l) && !SceneMarks.FrameLines.Contains(l)).ToList();
        var original = rig.Play(start, new ConstantRandom(2), on: false);
        await original.TalkAsync(45);
        var ours = rig.Play(start, new ConstantRandom(2), on: true);
        await ours.TalkAsync(45);
        Assert.NotEmpty(satella);
        Assert.All(satella, line => Assert.False(original.State.IsLineRead(line)));
        Assert.All(satella, line => Assert.True(ours.State.IsLineRead(line)));
        Assert.Equal(original.Random.Draws, ours.Random.Draws);
    }

    /// <summary>
    /// Haurein (unit 12) past stage 1 with her S&amp;M Tower night and Barres (unit 6) in the army (§3.3): her kiss prank
    /// unread, the prank comes for certain; on the talk after it she leaves (<c>F_Haurein_Fate</c> 2, her unit gone).
    /// Off, with the same draws, she stays.
    /// </summary>
    [ScenarioFact]
    public async Task Haurein_leaves_on_the_talk_after_her_kiss()
    {
        var rig = await BootAsync();
        var start = rig.Start();
        start.StoryStage = 2;
        start.Units[12].Status = 1;
        start.Units[6].Status = 1;
        start.F_Haurein_Arc = 0;
        start.F_Haurein_State2 = 2;
        start.F_Haurein_Fate = 0;

        for (int seed = 1; seed <= 4; seed++)
        {
            var play = rig.Play(start, new LoggingRandom(seed, []), on: true);
            var kiss = rig.Marks.Talk(play.State, 12, true);
            await play.TalkAsync(12);
            var first = Assert.Single(play.Choices.Draws, d => d.At == HaureinDraw);
            output.WriteLine($"seed {seed}, talk 1: {string.Join("; ", play.Choices.Draws)}");
            Assert.Equal(DrawRule.HarmlessUnread, first.Rule);
            Assert.Contains(first.Value, Choice(kiss, HaureinDraw).Values);
            Assert.Equal((1, 0), (play.State.Units[12].Status.Value, play.State.F_Haurein_Fate.Value));

            var leaving = rig.Marks.Talk(play.State, 12, true);
            Assert.Equal(SceneMark.Maybe, leaving.Mark);
            await play.TalkAsync(12);
            var second = Assert.Single(play.Choices.Draws, d => d.At == HaureinDraw);
            output.WriteLine($"seed {seed}, talk 2: {string.Join("; ", play.Choices.Draws)}");
            Assert.Equal((1, DrawRule.Listed), (second.Value, second.Rule));
            Assert.Equal([second.Value], Choice(leaving, HaureinDraw).Values);
            Assert.Equal((0, 2), (play.State.Units[12].Status.Value, play.State.F_Haurein_Fate.Value));
            Assert.Equal(0, play.Choices.Fallbacks);
        }

        var original = rig.Play(start, new ConstantRandom(2), on: false);
        await original.TalkAsync(12);
        await original.TalkAsync(12);
        Assert.Equal((1, 0), (original.State.Units[12].Status.Value, original.State.F_Haurein_Fate.Value));
    }

    /// <summary>
    /// Kentarou (unit 31) at his state 5 (§3.5): the first training talk reads the scene (unread before the draw, so the
    /// draw keeps its odds — here 2, no step); each talk after it steps the state, 5 → 6 → 7, and the fourth plays
    /// Rance Attack (state 8, no draw): four training talks, not about seven. Off, with the same draws, he stays at 5.
    /// </summary>
    [ScenarioFact]
    public async Task Kentarou_learns_rance_attack_in_four_training_talks()
    {
        var rig = await BootAsync();
        var start = rig.Start();
        start.Units[31].Status = 1;
        start.F_Kentarou_State = 5;

        var play = rig.Play(start, new ConstantRandom(2), on: true);
        var original = rig.Play(start, new ConstantRandom(2), on: false);
        List<int> states = [], rules = [];
        for (int talk = 1; talk <= 4; talk++)
        {
            await play.TalkAsync(31);
            await original.TalkAsync(31);
            output.WriteLine($"talk {talk}: state {play.State.F_Kentarou_State.Value}; {string.Join("; ", play.Choices.Draws)}");
            states.Add(play.State.F_Kentarou_State.Value);
            rules.Add(play.Choices.Draws.Where(d => d.At == KentarouDraw).Select(d => (int)d.Rule).DefaultIfEmpty(-1).Single());
            Assert.Equal(5, original.State.F_Kentarou_State.Value);
        }
        Assert.Equal([5, 6, 7, 8], states);
        Assert.Equal([(int)DrawRule.Odds, (int)DrawRule.Listed, (int)DrawRule.Listed, -1], rules);
        Assert.Equal(0, play.Choices.Fallbacks);
    }

    /// <summary>
    /// Isoroku (unit 27) at her stage 2 with the Hospital built (§3.6): the first stage-2 visit reads it (unread after the
    /// draw, so an unread harmless value comes: no pregnancy); the second makes her stage 3. Off, with the same draws
    /// (2), she stays at 2.
    /// </summary>
    [ScenarioFact]
    public async Task Isoroku_is_pregnant_on_her_second_stage_2_visit()
    {
        var rig = await BootAsync();
        var start = rig.Start();
        start.StoryStage = 2;
        start.Units[27].Status = 1;
        start.N_Isoroku_Stage = 2;
        start.F_Isoroku_Fate = 0;
        start.Bld_Hospital = 1;

        var play = rig.Play(start, new ConstantRandom(2), on: true);
        var original = rig.Play(start, new ConstantRandom(2), on: false);
        List<int> stages = [];
        for (int visit = 1; visit <= 2; visit++)
        {
            await play.VisitAsync(44);
            await original.VisitAsync(44);
            output.WriteLine($"visit {visit}: stage {play.State.N_Isoroku_Stage.Value}; {string.Join("; ", play.Choices.Draws)}");
            stages.Add(play.State.N_Isoroku_Stage.Value);
            Assert.Equal(2, original.State.N_Isoroku_Stage.Value);
        }
        Assert.Equal([2, 3], stages);
        Assert.Equal(DrawRule.Listed, Assert.Single(play.Choices.Draws, d => d.At == IsorokuDraw).Rule);
        Assert.Equal(0, play.Choices.Fallbacks);
    }

    /// <summary>
    /// Kanami's visit with her usual visit read (§3.4): scene 2 (her <c>F_Kanami_Scene2</c> 0) or the tower scene comes
    /// for certain, whatever the generator draws — the mark's New and its choice.
    /// </summary>
    [ScenarioFact]
    public async Task Kanamis_scene_2_or_tower_scene_comes_once_her_usual_visit_is_read()
    {
        var rig = await BootAsync();
        var start = rig.Start();
        start.N_Kanami_State = 2;
        start.F_Kanami_Scene1 = 0;
        start.F_Kanami_Scene2 = 0;
        start.Bld_SMTower = 1;
        ReadAll(start, rig.Marks.Visit(start, 6, true), leaf => leaf.Path.Draws.Single(d => d.At == KanamiDraw).Value >= 3);
        var report = rig.Marks.Visit(start, 6, true);
        output.WriteLine(report.ToString());
        Assert.Equal(SceneMark.New, report.Mark);
        Assert.Equal([1, 2], Choice(report, KanamiDraw).Values);

        var seen = new HashSet<int>();
        for (int seed = 1; seed <= 8; seed++)
        {
            var play = rig.Play(start, new LoggingRandom(seed, []), on: true);
            await play.VisitAsync(6);
            output.WriteLine($"seed {seed}: {string.Join("; ", play.Choices.Draws)}; scene 1 {play.State.F_Kanami_Scene1.Value}");
            var draw = Assert.Single(play.Choices.Draws, d => d.At == KanamiDraw);
            Assert.Equal(DrawRule.Listed, draw.Rule);
            Assert.Contains(draw.Value, new[] { 1, 2 });
            if (draw.Value == 2)
                Assert.Equal(1, play.State.F_Kanami_Scene1.Value);     // the tower scene
            else
                Assert.Contains(play.Input.Menus, m => m.Any(item => item.File == "Gｶﾅﾐ.ADV"));   // scene 2's menu
            Assert.Equal(0, play.Choices.Fallbacks);
            seen.Add(draw.Value);
        }
        output.WriteLine($"values seen: {string.Join(", ", seen)}");
    }

    /// <summary>
    /// Suzume's and Wendy's draw (<c>Cﾊｰﾚﾑ:6</c>) comes before the visit and keeps the original's 1 in 4: with the rule
    /// on and the generator giving 3, Suzume's unread first meeting does not come, and the draw is not the rule's.
    /// </summary>
    [ScenarioFact]
    public async Task Suzumes_and_wendys_draw_keeps_its_odds()
    {
        var rig = await BootAsync();
        var start = rig.Start();
        start.F_Suzume_Met = 0;
        start.F_Wendy_Met = 0;

        var play = rig.Play(start, new ConstantRandom(3), on: true);
        await play.VisitAsync(3, meetings: false);                     // Maris (3)
        output.WriteLine(string.Join("; ", play.Choices.Draws));
        Assert.Equal((0, 0), (play.State.F_Suzume_Met.Value, play.State.F_Wendy_Met.Value));
        Assert.DoesNotContain(play.Choices.Draws, d => d.At is null || d.At.Value.File == "Cﾊｰﾚﾑ.ADV");
        Assert.Equal(3, play.Random.Draws[0].Value);
        Assert.Equal(4, play.Random.Draws[0].Count);
    }

    /// <summary>With the rule off nothing is noted and nothing is replaced: the choices stay inactive through a talk.</summary>
    [ScenarioFact]
    public async Task Off_the_rule_never_acts()
    {
        var rig = await BootAsync();
        var start = CafeState(rig);
        var play = rig.Play(start, new ConstantRandom(2), on: false);
        await play.TalkAsync(45);
        Assert.Empty(play.Choices.Draws);
        Assert.Empty(play.Choices.Decisions);
        Assert.Equal(new MarkTarget(MarkTargetKind.Talk, 45), play.Choices.Target);
        Assert.Equal(0, play.Choices.Fallbacks);
    }

    /// <summary>
    /// In a game imported from the original's save (docs/save-import.md, §7) the rule does not act though the player's
    /// option is on: Cafe's talk draws as the original's (2: the children, the Satella scene left unread).
    /// </summary>
    [ScenarioFact]
    public async Task In_a_game_imported_from_the_original_the_rule_never_acts()
    {
        var rig = await BootAsync();
        var start = CafeState(rig);
        start.ImportedFromOriginal = true;
        var play = rig.Play(start, new ConstantRandom(2), on: true);
        Assert.True(play.Script.NewScenesOption);
        Assert.False(play.Script.NewScenesFirst);
        await play.TalkAsync(45);
        Assert.Empty(play.Choices.Draws);
        Assert.Empty(play.Choices.Decisions);
        var satella = LeafWith(rig.Marks.Talk(start, 45, true), CafeDraw, 1).Path.Lines
            .Where(l => !start.IsLineRead(l) && !SceneMarks.FrameLines.Contains(l)).ToList();
        Assert.NotEmpty(satella);
        Assert.All(satella, line => Assert.False(play.State.IsLineRead(line)));
    }

    /// <summary>
    /// Where the dry run fails (here its scenes cannot be read), the talk goes on with the values drawn — nothing is
    /// thrown into the game — and the fallback is kept with its reason; the rule does not act again in that talk.
    /// </summary>
    [ScenarioFact]
    public async Task A_dry_run_that_fails_leaves_the_draws_as_drawn()
    {
        var rig = await BootAsync();
        var start = CafeState(rig);
        var play = rig.Play(start, new ConstantRandom(2), on: true);
        play.Script.Marks = new SceneMarks(new MissingScenes());
        await play.TalkAsync(45);
        output.WriteLine(play.Choices.LastFallback);
        Assert.Empty(play.Choices.Draws);
        Assert.Equal(1, play.Choices.Fallbacks);
        Assert.Contains("Talk 45", play.Choices.LastFallback);
        Assert.False(play.Choices.Active);
        var satella = LeafWith(rig.Marks.Talk(start, 45, true), CafeDraw, 1).Path.Lines
            .Where(l => !start.IsLineRead(l) && !SceneMarks.FrameLines.Contains(l)).ToList();
        Assert.All(satella, line => Assert.False(play.State.IsLineRead(line)));    // 2: the children
    }

    /// <summary>A scene source without files.</summary>
    private sealed class MissingScenes : ISceneSource
    {
        public ScriptProgram ReadProgram(string file) => throw new FileNotFoundException("No scenes in this test.", file);
    }

    /// <summary>Cafe (unit 45) after her first talk, Satella (unit 29) in the army, the children scene read.</summary>
    private static GameState CafeState(Rig rig)
    {
        var start = rig.Start();
        start.Units[45].Status = 1;
        start.Units[29].Status = 1;
        start.F_Cafe_State = 21;
        ReadAll(start, rig.Marks.Talk(start, 45, true), leaf => leaf.Path.Draws.Single(d => d.At == CafeDraw).Value != 1);
        return start;
    }

    /// <summary>The choice at the report's draw at <paramref name="at"/> (the first such node).</summary>
    private static DrawChoice Choice(SceneMarkReport report, ScriptRef at) =>
        report.Tree.Choose(DrawNodes(report.Tree.Root).First(n => n.Decision!.Value.At == at), SceneMarks.Rules(true));

    /// <summary>The report's one path whose draw at <paramref name="at"/> gave <paramref name="value"/>.</summary>
    private static MarkLeaf LeafWith(SceneMarkReport report, ScriptRef at, int value) =>
        Assert.Single(report.Tree.Leaves, l => l.Path.Draws.Any(d => d.At == at && d.Value == value));

    private static IEnumerable<MarkNode> DrawNodes(MarkNode node)
    {
        if (node.Decision is { Kind: PreviewDecisionKind.Draw })
            yield return node;
        foreach (var child in node.Children.Values)
            foreach (var draw in DrawNodes(child))
                yield return draw;
    }

    /// <summary>Marks every line of the report's paths (those <paramref name="which"/> takes) read in <paramref name="state"/>, but the command's own.</summary>
    private static void ReadAll(GameState state, SceneMarkReport report, Func<MarkLeaf, bool>? which = null)
    {
        foreach (var leaf in report.Tree.Leaves.Where(which ?? (_ => true)))
            foreach (var line in leaf.Path.Lines)
                if (!SceneMarks.FrameLines.Contains(line))
                    state.MarkLineRead(line);
    }

    /// <summary>The booted new game at story stage 1 and the player's scenes.</summary>
    internal static async Task<Rig> BootAsync()
    {
        var game = await EndGame.BootAsync();
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var scenes = new CachedSceneSource(scripts);
        return new Rig(game.State, scripts, scenes, new SceneMarks(scenes));
    }

    /// <summary>The booted game and the scenes shared by every run of a test.</summary>
    internal sealed record Rig(GameState Booted, ScenarioScripts Scripts, CachedSceneSource Scenes, SceneMarks Marks)
    {
        /// <summary>A copy of the booted game at stage 1, week 18, the gold for anything, every first meeting of the harem made.</summary>
        public GameState Start()
        {
            var s = ScriptState.Copy(Booted);
            s.StoryStage = 1;
            s.Week = 18;
            s.Gold = 1000;
            s.F_Suzume_Met = 1;                                         // Cﾊｰﾚﾑ:6 plays no first meeting
            s.F_Wendy_Met = 1;
            return s;
        }

        /// <summary>A game on a copy of <paramref name="from"/> with the rule <paramref name="on"/> or off, as <c>GameSession</c> wires it.</summary>
        public Play Play(GameState from, IScriptRandom random, bool on)
        {
            var state = ScriptState.Copy(from);
            var rules = new StrategyRules(Scripts, new RecordingRules { State = state });
            var runner = new SceneRunner(Scenes, rules, hooks: ScenePatches.Instance);
            var input = new EndInput(state);
            var counting = new CountingRandom(random);
            var view = new RecordingView();
            var script = new OriginalScriptContext(state, counting, view, input, runner)
            {
                Saves = new MemorySaveStore(),
                NewScenesOption = on,
                Marks = Marks,
            };
            runner.Bind(script);
            rules.Bind(script);
            return new Play(state, script, input, counting, view);
        }
    }

    /// <summary>One game: its talks and visits run through the commands, a week's orders given back before each.</summary>
    internal sealed record Play(GameState State, OriginalScriptContext Script, EndInput Input, CountingRandom Random, RecordingView View)
    {
        public SceneChoices Choices => Script.SceneChoices;

        /// <summary><c>Cﾌﾞｶ</c>'s Talk to <paramref name="unit"/>, as our Subordinates screen orders it.</summary>
        public async Task TalkAsync(int unit)
        {
            (State.Ui_CmdSubordAvail, State.CmdAvail_SubTroops, State.CmdAvail_SubTalk) = (1, 1, 1);
            SubordinateOrder? order = null;
            Input.Idle = () => order ??= SubordinateOrder.Start(State, SubordinateOrderKind.Talk, unit, 0, Input.Pointer);
            Input.OnKey = () => order?.AnswerKey();
            Input.Menu.Enqueue(items => order?.Choose(items) ?? throw new InvalidOperationException("A menu the order does not answer."));
            await Script.CallFileAsync("Cﾌﾞｶ.ADV");
            Assert.NotNull(order);
            Assert.Equal(0, State.CmdAvail_SubTalk.Value);
        }

        /// <summary><c>Cﾊｰﾚﾑ</c>'s visit of <paramref name="girl"/>, as our Harem screen summons her; a scene's menus take their first item.</summary>
        public async Task VisitAsync(int girl, bool meetings = true)
        {
            State.Ui_CmdHaremAvail = 1;
            if (meetings)
                (State.F_Suzume_Met, State.F_Wendy_Met) = (1, 1);
            bool clicked = false;
            Input.Idle = () =>
            {
                if (!clicked && State.Memory.Var(248 + girl) != 0)
                {
                    clicked = true;
                    HaremRoster.Summon(girl, Input.Pointer);
                }
            };
            Input.OnKey = null;
            for (int i = 0; i < 8; i++)
                Input.Menu.Enqueue(1);
            await Script.CallFileAsync("Cﾊｰﾚﾑ.ADV");
            Assert.True(clicked);
            Assert.Equal(girl, State.Harem_LastPick.Value);
        }
    }

    /// <summary>Each draw of 2 or more of the generator it wraps, with its count and value, in order.</summary>
    internal sealed class CountingRandom(IScriptRandom inner) : IScriptRandom
    {
        public List<(int Count, int Value)> Draws { get; } = [];

        public Sys3Int Next(Sys3Int n)
        {
            var value = inner.Next(n);
            if (n.Value > 1)
                Draws.Add((n.Value, value.Value));
            return value;
        }
    }

    /// <summary>Every draw of 2 or more gives <paramref name="value"/> (at most n).</summary>
    internal sealed class ConstantRandom(int value) : IScriptRandom
    {
        public Sys3Int Next(Sys3Int n) => n.Value <= 1 ? n : (Sys3Int)Math.Min(value, n.Value);
    }
}
