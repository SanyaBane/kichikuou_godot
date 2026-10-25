using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// Our battle screen (docs/ui/gallery.md, step 4): <see cref="GalleryBattle.Capture"/> replayed into a presenter of the
/// entry's own and held at a moment — the opening, a player's turn with our commands, an exchange's result with its
/// counters, an info box — and drawn by the board as the game draws it. The board's own layers are fed as
/// <c>GameScreen</c> feeds them: the commands begun on the turn's <c>FIGT_SEL</c> menu, given the player's pointer as
/// input in the board's layout pixels (the system pointer stays), the info box at its key wait.
/// </summary>
internal static partial class GalleryEntries
{
    private const string CaptureNote = "The battle of " + GalleryBattle.Capture + ", replayed and held.";

    /// <summary>A defence of story stage 1 with ten defences, the first early (the owner's capture run of 2026-10-10).</summary>
    private const string DefenceCapture = "20261010-213435-25-FIGT_B.golden";

    private static IEnumerable<GalleryEntry> BoardScreens()
    {
        yield return new("opening", Battle, "Battle: the opening", null, CaptureNote + " At the first round's header.",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Opening is not null));
        yield return new("battle-turn", Battle, "Battle: a player's turn", null, CaptureNote + " Round 1's first FIGT_SEL, our commands begun.",
            g => Board(g, Turn, then: (_, _, _) => { }));
        yield return new("menu", Battle, "Battle: the acting unit's menu", null, CaptureNote + " Its first item hovered.",
            g => Board(g, Turn, then: (p, m, board) =>
            {
                var model = new BattleCommands(m.Menu!);
                Click(board.Commands, BattleBoardView.CardArea(model.Acting).GetCenter(), right: true);
                if (board.Commands.ItemCentre(0) is { } first)
                    board.Commands.Move(first);
            }));
        yield return new("menu-orders", Battle, "Battle: another unit's orders", null, CaptureNote + " A unit not acting, its menu of orders.",
            g => Board(g, Turn, then: (p, m, board) =>
            {
                var model = new BattleCommands(m.Menu!);
                var other = Enumerable.Range(1, m.Menu!.PlayerSlotsUsed.Value).Select(n => new BattlePlace(BattleSide.Player, n))
                    .FirstOrDefault(place => place != model.Acting);
                if (other.Slot == 0)
                    throw new InvalidOperationException("The turn's party has one unit.");
                Click(board.Commands, BattleBoardView.CardArea(other).GetCenter(), right: true);
                if (board.Commands.ItemCentre(1) is { } second)
                    board.Commands.Move(second);
            }));
        yield return new("banners-E1", Battle, "Battle: a target's banners", null, CaptureNote + " The pointer on the first enemy card.",
            g => Board(g, Turn, then: (_, _, board) =>
                board.Commands.Move(BattleBoardView.CardArea(new BattlePlace(BattleSide.Enemy, 1)).Position + new Vector2(220, 75))));
        yield return new("retreat", Battle, "Battle: the retreat dialog", null,
            CaptureNote + " Made up: the capture's player did not retreat; the dialog over round 1's first turn.",
            g => Board(g, Turn, dialog: true));
        yield return new("exchange-counters", Battle, "Battle: an exchange's result", null, CaptureNote + " The first exchange's result held, its counters standing.",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Battle.ExchangeCounters is not null && p.BattleSceneShows),
            Settle: 0.5);
        yield return new("info-panel", Battle, "Battle: our narration panel", null, CaptureNote + " The first info box shown as our panel.",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Battle.InfoBox is { Kind: InfoBoxKind.Panel }));
        yield return new("info-defence", Battle, "Battle: a defence's ribbon", null,
            $"The battle of {DefenceCapture}, replayed and held at its first defence (the other battle has none).",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Battle.InfoBox is { Kind: InfoBoxKind.Defence }, file: DefenceCapture));
        yield return new("info-results", Battle, "Battle: the results plate", null, CaptureNote + " The battle's tally.",
            g => Board(g, Results));
        yield return new("info-level-up", Battle, "Battle: a level-up on the results plate", null, CaptureNote + " The level-up's page after the tally.",
            g => Board(g, Results, nth: 2));
    }

    /// <summary>The results plate at its key wait: the tally, then each level-up's page.</summary>
    private static bool Results(GamePresenter p, BattleMoment m) => m.Kind == MomentKind.Key && p.Battle.InfoBox is { Kind: InfoBoxKind.Results };

    /// <summary>A player's turn: <c>FIGT_SEL</c> with our battle screen up.</summary>
    private static bool Turn(GamePresenter p, BattleMoment m) => m.Kind == MomentKind.Commands && p.BattleSceneShows;

    /// <summary>A press and a release at <paramref name="at"/> (layout pixels) given to the commands as the pointer's.</summary>
    private static void Click(BattleCommandLayer commands, Vector2 at, bool right)
    {
        commands.Move(at);
        commands.Press(right, at);
        commands.Release(right, at);
    }

    /// <summary>
    /// Our battle board on the capture held at the <paramref name="nth"/> moment <paramref name="chosen"/> picks, followed
    /// each frame as the game follows it; at a <c>FIGT_SEL</c> moment with <paramref name="then"/> our commands are begun
    /// on its menu and <paramref name="then"/> plays the pointer once the board is laid out; with <paramref name="dialog"/>
    /// the retreat's confirmation is asked over it.
    /// </summary>
    private static Action<Rect2>? Board(GalleryStage g, Func<GamePresenter, BattleMoment, bool> chosen, int nth = 1,
        Action<GamePresenter, BattleMoment, BattleBoardView>? then = null, bool dialog = false, string file = GalleryBattle.Capture)
    {
        var (p, moment) = GalleryBattle.HeldAt(g.Fresh, chosen, nth, file);
        var s = p.State ?? throw new InvalidOperationException("The replay bound no state.");
        var board = g.Add(new BattleBoardView(g.Font, g.Faces) { Visible = true });
        board.LoadCrops();
        if (then is not null && moment.Menu is { } menu)
        {
            p.Scene.Choosing = true;
            board.Commands.Begin(new BattleCommands(menu), s, p.Battle.Text);
        }
        if (dialog)
            board.Dialog.Begin(BattleDialog.For(Original.Combat.Battle.RetreatMenu)!, p.Battle.Text);
        bool first = true, played = false;
        void Follow()
        {
            if (!GodotObject.IsInstanceValid(board))
                return;
            var hovered = board.Commands.Active ? board.Commands.Lit : null;
            p.Scene.Hover(board.Commands.Active ? board.Commands.Hovered : null);
            board.Refresh(p, [], screenChanged: first, hovered, instant: true, slidesInstant: true);
            board.Info.Show(p.Battle.InfoBox, moment.Kind == MomentKind.Key);
            first = false;
        }
        g.EachFrame(Follow);
        return area =>
        {
            board.Place(area.Position, area.Size.X / 1280f);
            first = true;
            Follow();
            if (played || then is null)
                return;
            played = true;
            g.AfterLayout(() => then(p, moment, board));
        };
    }
}
