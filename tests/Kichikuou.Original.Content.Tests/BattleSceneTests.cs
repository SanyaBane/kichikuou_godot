using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The armies' scene of our battle screen (docs/ui/battle-screen.md, "The scene between exchanges").</summary>
public class BattleSceneTests(ITestOutputHelper output)
{
    // One reader for every capture: loading the CGs again for each is what takes the time.
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    [Fact]
    public void The_commanders_rows_come_halfway_up_and_the_troops_row_stays()
    {
        Assert.Equal(80, BattleScene.Y(80));
        Assert.Equal(190, BattleScene.Y(300));
        Assert.Equal(171, BattleScene.Y(263));
        Assert.Equal(98, BattleScene.Y(117));
        Assert.True(BattleScene.Y(300) + 32 <= BattleScene.HpY);
    }

    /// <summary>
    /// Who the picture shows between exchanges (owner, 2026-10-13): an empty field at a round's start; the
    /// enemy's turn its actor, then the target it highlights, the target gone when it defends; the player's
    /// turn the acting unit, and while choosing the enemy whose card the pointer is on, never another own
    /// card; after an exchange nothing from the redraw on (the armies part), so the next actor comes in
    /// anew even when it fought in that exchange (its arrival changes).
    /// </summary>
    [Fact]
    public void The_scene_shows_the_turns_actor_its_target_and_the_hovered_enemy()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var scene = new BattleScene(new CgLibrary(missing));
        // Rance (unit 1) and Barres (unit 6) against Dodge (commander 5) and Melfeis (commander 4).
        var s = new GameState();
        (s.Bt_PlayerSlot1, s.Bt_PlayerSlot2, s.Bt_EnemySlot1, s.Bt_EnemySlot2) = (1, 6, 5, 4);
        var rance = new BattlePlace(BattleSide.Player, 1);
        var barres = new BattlePlace(BattleSide.Player, 2);
        var dodge = new BattlePlace(BattleSide.Enemy, 1);
        var melfeis = new BattlePlace(BattleSide.Enemy, 2);
        scene.Start(s);
        void See(ScriptEvent e, BattlePlace? acting = null) => scene.Observe(e, s, acting);

        See(new RoundHeaderEvent(0, 0, 0, 0, 0, 5));
        See(new BattlefieldBackgroundEvent(0));
        Assert.Equal((0, 0), scene.InFocus);

        See(new PanelHighlightEvent(5), dodge);                         // Dodge acts
        Assert.Equal((0, 5), scene.InFocus);
        See(new PanelHighlightEvent(2), dodge);                         // and picks Barres
        Assert.Equal((6, 5), scene.InFocus);
        See(new BattlefieldBackgroundEvent(0), dodge);                  // then defends: no exchange
        Assert.Equal((0, 5), scene.InFocus);

        See(new PanelHighlightEvent(1), rance);                         // Rance's turn
        Assert.Equal((1, 0), scene.InFocus);
        scene.Hover(melfeis);                                           // not choosing yet: nothing
        Assert.Equal((1, 0), scene.InFocus);
        scene.Choosing = true;
        scene.Hover(barres);                                            // an own card: Rance stays
        Assert.Equal((1, 0), scene.InFocus);
        scene.Hover(melfeis);
        Assert.Equal((1, 4), scene.InFocus);
        scene.Hover(null);                                              // off the card
        Assert.Equal((1, 0), scene.InFocus);
        scene.Hover(dodge);
        scene.Choosing = false;                                         // Rance attacks Dodge: he stays, no slide out and in
        Assert.Equal((1, 5), scene.InFocus);
        See(new PanelHighlightEvent(5), rance);
        Assert.Equal((1, 5), scene.InFocus);
        (s.Unit_Cur, s.Cmdr_Cur) = (1, 5);
        See(new EngagementSceneEvent(0, [], [], false, 0, 0), rance);
        Assert.Equal((1, 5), scene.InFocus);
        Assert.Equal((ExchangeAction.None, ExchangeAction.None), scene.Actions);
        (s.Bt_PlayerAction, s.Bt_EnemyAction) = (0, 2);                 // Melee against Magic
        See(new ActionLabelsEvent(new ScriptRef("FIGT.ADV", 1753), new ScriptRef("FIGT.ADV", 1794)), rance);
        Assert.Equal((ExchangeAction.Melee, ExchangeAction.Magic), scene.Actions);
        s.Bt_SpecialAtk = 3;                                            // a special: the enemy's line is blank
        See(new ActionLabelsEvent(new ScriptRef("FIGT.ADV", 1748), new ScriptRef("FIGT.ADV", 1781)), rance);
        Assert.Equal((ExchangeAction.Special, ExchangeAction.None), scene.Actions);
        s.Bt_SpecialAtk = 0;
        (s.Bt_EnemySlot1, s.Bt_EnemySlot2) = (4, 0);                    // Dodge falls: the slots compact
        int arrival = scene.PlayerArmy.Arrival;
        See(new BattlefieldBackgroundEvent(0), rance);                  // after the exchange the armies part
        Assert.Equal((0, 0), scene.InFocus);
        Assert.Equal((ExchangeAction.None, ExchangeAction.None), scene.Actions);

        See(new PanelHighlightEvent(1), rance);                         // Rance acts again: he comes back
        Assert.Equal((1, 0), scene.InFocus);
        Assert.Equal(arrival + 2, scene.PlayerArmy.Arrival);
        See(new BattlefieldBackgroundEvent(0), rance);                  // and defends: he stays
        Assert.Equal((1, 0), scene.InFocus);
        Assert.Equal(arrival + 2, scene.PlayerArmy.Arrival);

        See(new PanelHighlightEvent(2), barres);                        // Barres (Auto-Atk) acts, the exchange follows
        Assert.Equal((6, 0), scene.InFocus);
        See(new RoundHeaderEvent(0, 0, 0, 0, 0, 4));
        Assert.Equal((0, 0), scene.InFocus);
    }

    /// <summary>
    /// In every captured battle the scene is on from the field's first drawing; when the original's
    /// engagement stands whole with its counters (the slide's last step), the scene's rows 0-223 are the
    /// original's screen there (the HP counters, moved to row 224, aside); between exchanges it shows a
    /// player unit and an enemy commander that stand in the slots; the picture under the numbers changes
    /// only with <see cref="BattleScene.PictureVersion"/>; each side's army is the one in focus. The scene's
    /// picture is put together from the layers our screen shows (<see cref="BattleScene.ComposeWhole"/>), each
    /// drawn again only when it changed — so the comparison with the original's screen checks them.
    /// </summary>
    [CaptureTheory]
    [MemberData(nameof(Captures))]
    public async Task The_scene_shows_the_engagement_as_the_original_does(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var cgs = Cgs.Value;
        var presenter = new BattlePresenter(Texts.Value, cgs, new BoxGlyphs());
        var scene = new BattleScene(cgs);
        var order = new RoundOrder();
        GameState? state = null;
        int compared = 0, between = 0;
        int lastOffset = -1;
        var wrong = new List<string>();
        byte[]? picture = null;
        int pictureVersion = -1, pictures = 0, actors = 0;
        var view = new ObservingView(e =>
        {
            presenter.Show(e);
            order.Observe(e, state!);
            scene.Observe(e, state!, order.Acting);
            if (e is PanelHighlightEvent other && BattleBoard.PanelPlace(other.Panel.Value) is { } p && order.Acting is { } a
                && p.Side == a.Side && p != a)
                wrong.Add($"{p} highlighted on the side of the actor {a}: a turn not seen");
            if (e is PanelHighlightEvent && order.Acting is null)
                wrong.Add("a panel highlighted with no actor");
            if (e is RoundHeaderEvent && scene.InFocus != (0, 0))
                wrong.Add($"a round header shows {scene.InFocus}");
            if (e is PanelHighlightEvent h && BattleBoard.PanelPlace(h.Panel.Value) is { } lit && lit == order.Acting && ++actors > 0
                && scene.InFocus != (lit.Side == BattleSide.Player ? (BattleBoard.Record(state!, lit), 0) : (0, BattleBoard.Record(state!, lit))))
                wrong.Add($"the actor {lit} highlighted shows {scene.InFocus}");
            if (e is BattlefieldBackgroundEvent or CountersEvent)
            {
                // The picture under the numbers, put together from the layers, changes only with its version.
                scene.ComposeWhole();
                var now = scene.Surface.ReadArea(0, BattleScene.PictureY, BattleScene.Width, BattleScene.Height);
                bool same = picture is not null && now.AsSpan().SequenceEqual(picture);
                if (scene.PictureVersion == pictureVersion && !same && picture is not null)
                    wrong.Add($"the picture under the numbers changed without its version ({e.GetType().Name})");
                if (scene.PictureVersion != pictureVersion)
                    pictures++;
                picture = now;
                pictureVersion = scene.PictureVersion;
                var (unit, cmdr) = scene.InEngagement ? (state!.Unit_Cur.Value, state!.Cmdr_Cur.Value) : scene.InFocus;
                int shownUnit = scene.PlayerArmy.Record, shownCmdr = scene.EnemyArmy.Record;     // 0: no sprite drawn
                if ((shownUnit != 0 && shownUnit != unit) || (shownCmdr != 0 && shownCmdr != cmdr))
                    wrong.Add($"the armies' layers show {scene.PlayerArmy.Record} / {scene.EnemyArmy.Record}, the picture {unit} / {cmdr}");
            }
            if (e is EngagementSlideEvent slide)
                lastOffset = slide.Offset.Value;
            if (e is CountersEvent && lastOffset == 0 && scene.InEngagement)
            {
                lastOffset = -1;
                compared++;
                int differ = 0;
                for (int y = 0; y < BattleScene.HpY; y++)
                {
                    for (int x = 0; x < BattleScene.Width; x++)
                    {
                        var (r, g, b, _) = presenter.Surface.PixelAt(x, y);
                        var (r2, g2, b2, _) = scene.Surface.PixelAt(x, y);
                        if ((r, g, b) != (r2, g2, b2))
                            differ++;
                    }
                }
                if (differ > 0)
                    wrong.Add($"engagement {compared}: {differ} pixels differ");
            }
            if (e is BattlefieldBackgroundEvent && state is { } s)
            {
                between++;
                if (!scene.Active)
                    wrong.Add("the field is drawn but the scene is off");
                var (unit, cmdr) = scene.InFocus;
                bool unitIn = BattleBoard.Cards(s, BattleSide.Player).Any(c => c?.Record == unit);
                bool cmdrIn = BattleBoard.Cards(s, BattleSide.Enemy).Any(c => c?.Record == cmdr);
                if ((unit != 0 && !unitIn) || (cmdr != 0 && !cmdrIn))
                    wrong.Add($"redraw {between}: unit {unit} / commander {cmdr} not in a slot");
            }
        });
        var report = await GoldenReplay.RunAsync(capture, view: view, started: s =>
        {
            state = s;
            presenter.Start(s);
            scene.Start(s);
        });
        output.WriteLine($"{file}: {compared} engagements compared, {between} redraws, {pictures} pictures, "
            + $"{actors} actors highlighted; replay {(report.Passed ? "matches" : "diverges")}");
        Assert.Empty(wrong);
    }

    /// <summary>
    /// Without <see cref="BattleScene.Numbers"/> (our battle screen, owner 2026-10-15) the front shows no counter, shot
    /// or damage in the captured battles: through every exchange, until its wiped-out marks, it is the battlefield's
    /// overlay alone (the marks come after it). Some defences end before any exchange.
    /// </summary>
    [CaptureTheory]
    [MemberData(nameof(Captures))]
    public async Task Without_numbers_the_front_is_the_overlay_and_the_marks(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var scene = new BattleScene(Cgs.Value) { Numbers = false };
        var order = new RoundOrder();
        GameState? state = null;
        bool marked = false;
        int checkedFronts = 0, engagements = 0;
        var wrong = new List<string>();
        var view = new ObservingView(e =>
        {
            order.Observe(e, state!);
            scene.Observe(e, state!, order.Acting);
            if (e is EngagementSceneEvent)
                (marked, engagements) = (false, engagements + 1);
            if (e is WipedOutMarksEvent)
                marked = true;
            if (e is CountersEvent or ShotsFrameEvent or ExchangeResultsEvent && scene.InEngagement && !marked)
            {
                checkedFronts++;
                var front = scene.Surface.ReadArea(0, BattleScene.FrontY, BattleScene.Width, BattleScene.Height);
                var overlay = scene.Surface.ReadArea(BattleScene.OverlayX, BattleScene.FieldY, BattleScene.Width, BattleScene.Height);
                if (!front.AsSpan().SequenceEqual(overlay))
                    wrong.Add($"front {checkedFronts} ({e.GetType().Name}) shows more than the overlay");
            }
        });
        await GoldenReplay.RunAsync(capture, view: view, started: s =>
        {
            state = s;
            scene.Start(s);
        });
        output.WriteLine($"{file}: {engagements} exchanges, {checkedFronts} fronts checked");
        Assert.Empty(wrong);
        Assert.True(engagements == 0 || checkedFronts > 0, "no exchange's front was checked");    // some defences end before any exchange
    }

    /// <summary>
    /// Every capture in the full run (<see cref="DifferentialRuns.Full"/>), every eighth in the quick
    /// one: the presenter and the scene draw each battle whole (minutes for all of them).
    /// </summary>
    public static IEnumerable<object[]> Captures =>
        ScenarioTextGoldenTests.Captures.Where((_, i) => DifferentialRuns.Full || i % DifferentialRuns.QuickShare == 0);

    private sealed class ObservingView(Action<ScriptEvent> show) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            show(scriptEvent);
            return Task.CompletedTask;
        }
    }
}
