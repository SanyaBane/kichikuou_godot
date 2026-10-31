using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

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
        yield return new("cards-sliding", Battle, "Battle: the cards sliding in", null,
            CaptureNote + " At the first round's header, the cards' slide begun and held at 0.6 of the front row's way in.",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Opening is not null, then: (_, _, board) => board.HoldCardsSlide(0.6)));
        yield return new("battle-turn", Battle, "Battle: a player's turn", null,
            CaptureNote + " Round 1's first FIGT_SEL, our commands begun, the damage forecast on (the touch mode's rail shows it).",
            g => Board(g, Turn, then: (_, _, _) => { }, forecast: true));
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
        yield return new("order-toggles", Battle, "Battle: the touch mode's order toggles", null,
            CaptureNote + " Made up: a unit not acting set to Auto-Def; in the touch mode its Auto-Atk toggle tapped (its choice checked) and held.",
            g => Board(g, Turn, then: (p, m, board) =>
            {
                var s = p.State!;
                var model = new BattleCommands(m.Menu!);
                var other = Enumerable.Range(1, m.Menu!.PlayerSlotsUsed.Value).Select(n => new BattlePlace(BattleSide.Player, n))
                    .FirstOrDefault(place => place != model.Acting);
                if (other.Slot == 0)
                    throw new InvalidOperationException("The turn's party has one unit.");
                SetOrder(s, other.Slot, StandingOrder.AutoDefend);
                board.Commands.Begin(model, s, p.Battle.Text, TouchMode.On ? TouchTarget(s, model) : null);
                if (!TouchMode.On)
                    return;
                var attack = model.ContextMenu(other, StandingOrder.AutoDefend).First(c => c.Choice.Command == FigtSelCommand.OrderAttack).Choice;
                var at = board.Commands.RailCentre(attack) ?? throw new InvalidOperationException($"No Auto-Atk toggle on {other}'s card.");
                Click(board.Commands, at, right: false);
                if (board.Commands.Result != attack)
                    throw new InvalidOperationException($"The Auto-Atk toggle answered {board.Commands.Result}, not {attack}.");
                board.Commands.Begin(model, s, p.Battle.Text, TouchTarget(s, model));
                board.Commands.Move(at);
                board.Commands.Press(false, at);
            }));
        yield return new("banners-E1", Battle, "Battle: a target's banners", null,
            CaptureNote + " The pointer on the first enemy card, the damage forecast on.",
            g => Board(g, Turn, then: (_, _, board) =>
                board.Commands.Move(BattleBoardView.CardArea(new BattlePlace(BattleSide.Enemy, 1)).Position + new Vector2(220, 75)), forecast: true));
        yield return new("retreat", Battle, "Battle: the retreat dialog", null,
            CaptureNote + " Made up: the capture's player did not retreat; the dialog over round 1's first turn.",
            g => Board(g, Turn, dialog: true));
        yield return new("exchange-counters", Battle, "Battle: an exchange's result", null, CaptureNote + " The first exchange's result held, its counters standing.",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Battle.ExchangeCounters is not null && p.BattleSceneShows),
            Settle: 0.5);
        yield return new("battle-speech", Battle, "Battle: a fighter's line", null, CaptureNote + " The first line a player unit says.",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Battle.Speech is { Side: BattleSide.Player, Thought: false }));
        yield return new("battle-speech-enemy", Battle, "Battle: an enemy's line", null, CaptureNote + " The first line an enemy commander says.",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Battle.Speech is { Side: BattleSide.Enemy, Thought: false }));
        yield return new("battle-speech-read", Battle, "Battle: a fighter's line read before", null,
            CaptureNote + " Made up: every message read before; the line in the already-read colour, darkened on the white balloon.",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Battle.Speech is { Side: BattleSide.Player, Thought: false }, read: true));
        yield return new("battle-speech-enemy-read", Battle, "Battle: an enemy's line read before", null,
            CaptureNote + " Made up: every message read before.",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Battle.Speech is { Side: BattleSide.Enemy, Thought: false }, read: true));
        yield return new("info-panel", Battle, "Battle: our narration panel", null, CaptureNote + " The first info box shown as our panel.",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Battle.InfoBox is { Kind: InfoBoxKind.Panel }));
        yield return new("info-defence", Battle, "Battle: a defence's ribbon", null,
            $"The battle of {DefenceCapture}, replayed and held at its first defence (the other battle has none).",
            g => Board(g, (p, m) => m.Kind == MomentKind.Key && p.Battle.InfoBox is { Kind: InfoBoxKind.Defence }, file: DefenceCapture),
            Settle: 0.35);                                                      // the ribbon at its height, as the look dumps it
        yield return new("info-results", Battle, "Battle: the results plate", null, CaptureNote + " The battle's tally.",
            g => Board(g, Results));
        yield return new("info-level-up", Battle, "Battle: a level-up on the results plate", null, CaptureNote + " The level-up's page after the tally.",
            g => Board(g, Results, nth: 2));
        yield return new("battle-window", Battle, "Battle: a window after the field", null,
            CaptureNote + " The first window the battle opens off the field, our message window over the screen the presenter composes.",
            BattleWindow);
    }

    /// <summary>
    /// The capture held at the key wait of the first window the battle opens off the field (not 131, whose frame is the
    /// whole screen): our message window over the presenter's screen as <c>GameScreen.UpdateMessageWindow</c> shows them,
    /// the event CGs masked as in the agent's dumps.
    /// </summary>
    private static Action<Rect2>? BattleWindow(GalleryStage g)
    {
        GamePresenter Fresh()
        {
            var presenter = g.Fresh();
            presenter.CoverMessages = true;
            presenter.MaskEventCgs = true;
            return presenter;
        }
        var (p, _) = GalleryBattle.HeldAt(Fresh,
            (p, m) => m.Kind == MomentKind.Key && !p.BattleSceneShows && p.MessageWindow is { Kind: not MessageKind.CgBar });
        var s = p.State ?? throw new InvalidOperationException("The replay bound no state.");
        return MessageWindowOf(g, p.MessageWindow!, skipOn: false, g.Picture(p), (s.Ui_PortraitL != 0, s.Ui_PortraitR != 0));
    }

    /// <summary>The results plate at its key wait: the tally, then each level-up's page.</summary>
    private static bool Results(GamePresenter p, BattleMoment m) => m.Kind == MomentKind.Key && p.Battle.InfoBox is { Kind: InfoBoxKind.Results };

    /// <summary>A player's turn: <c>FIGT_SEL</c> with our battle screen up.</summary>
    private static bool Turn(GamePresenter p, BattleMoment m) => m.Kind == MomentKind.Commands && p.BattleSceneShows;

    /// <summary>
    /// The target the touch mode preselects on a turn, as <c>GameScreen.TouchTarget</c> does at a battle's first turn (no
    /// target remembered yet).
    /// </summary>
    private static BattlePlace? TouchTarget(GameState s, BattleCommands model)
    {
        var enemies = Enumerable.Range(1, 4).Select(n => BattleBoard.Record(s, new BattlePlace(BattleSide.Enemy, n))).ToList();
        return new TouchTargets().Preselect(BattleBoard.Record(s, model.Acting), enemies,
            n => model.Banners(new BattlePlace(BattleSide.Enemy, n)).Count > 0) is { } slot
            ? new BattlePlace(BattleSide.Enemy, slot)
            : null;
    }

    /// <summary>Player slot <paramref name="slot"/>'s standing order (<c>Bt_PlayerOrder1-4</c>), made up on the replay's own state.</summary>
    private static void SetOrder(GameState s, int slot, StandingOrder order)
    {
        var value = (Engine.Sys3.Sys3Int)(int)order;
        switch (slot)
        {
            case 1: s.Bt_PlayerOrder1 = value; break;
            case 2: s.Bt_PlayerOrder2 = value; break;
            case 3: s.Bt_PlayerOrder3 = value; break;
            default: s.Bt_PlayerOrder4 = value; break;
        }
    }

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
    /// on its menu and <paramref name="then"/> plays the pointer once the board is laid out — with <paramref name="forecast"/>
    /// the damage forecast on (else off, whatever the option), its figures waited for; with <paramref name="dialog"/>
    /// the retreat's confirmation is asked over it; with <paramref name="read"/> every message counts as read before.
    /// </summary>
    private static Action<Rect2>? Board(GalleryStage g, Func<GamePresenter, BattleMoment, bool> chosen, int nth = 1,
        Action<GamePresenter, BattleMoment, BattleBoardView>? then = null, bool dialog = false, string file = GalleryBattle.Capture,
        bool read = false, bool forecast = false)
    {
        GamePresenter Fresh()
        {
            var presenter = g.Fresh();
            if (read)
                presenter.ReadBefore = _ => true;
            return presenter;
        }
        var (p, moment) = GalleryBattle.HeldAt(Fresh, chosen, nth, file);
        var s = p.State ?? throw new InvalidOperationException("The replay bound no state.");
        var board = g.Add(new BattleBoardView(g.Font, g.Faces) { Visible = true });
        board.LoadCrops();
        var skip = PhoneLandscape || Device.Layout == DeviceLayout.PhoneUpright
            ? g.Add(new TouchPill(TouchPill.Icon.Skip, "Skip") { Visible = true }) : null;   // the left rail's top (LB2); upright its strip (PB3)
        board.Commands.Forecast = forecast;
        if (then is not null && moment.Menu is { } menu)
        {
            p.Scene.Choosing = true;
            var model = new BattleCommands(menu);
            board.Commands.Begin(model, s, p.Battle.Text, TouchMode.On ? TouchTarget(s, model) : null);
            board.Commands.ForecastsDone?.Wait();                          // the dump the same every time
        }
        if (dialog)
            board.Dialog.Begin(BattleDialog.For(Original.Combat.Battle.RetreatMenu)!, p.Battle.Text);
        if (p.Battle.InfoBox is { Kind: InfoBoxKind.Defence } defence)        // a defence flashes on its card, as GameScreen flashes it
            board.Info.Flash(defence, defence.Place is { } at && BattleBoard.Card(s, at) is { } card ? card.Name : "");
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
            if (skip is not null && Device.Held(skip) is { } phone)
            {
                var cell = phone.IsUpright ? PhoneBattleUpright.OnScreen(phone, PhoneBattleUpright.Skip) : PhoneBattle.SkipCell(phone);
                skip.Phone = phone;
                skip.Place(new Rect2(cell.X, cell.Y, cell.W, cell.H), area.Size.X / 1280f, 20);
            }
            first = true;
            Follow();
            if (played || then is null)
                return;
            played = true;
            g.AfterLayout(() => then(p, moment, board));
        };
    }
}
