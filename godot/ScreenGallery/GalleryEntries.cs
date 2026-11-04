using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// One screen of ours in one state (docs/ui/gallery.md, §3): <see cref="Id"/> names its dump (the name unattended play
/// dumps it by, where it does), <see cref="Save"/> the kept save its state starts from, <see cref="Note"/> what is made up
/// or missing. <see cref="Build"/> adds its views to the stage and returns how to place them over the picture's area
/// (the 1280×800 screen fitted), or null when it cannot be shown (its save is missing). <see cref="Settle"/>: seconds the
/// dump waits for a view that rises into place (a few frames otherwise).
/// </summary>
internal sealed record GalleryEntry(string Id, string Section, string Title, string? Save, string Note,
    Func<GalleryStage, Action<Rect2>?> Build, double Settle = 0);

/// <summary>
/// The screen gallery's entries (docs/ui/gallery.md, §7: step 1 Treasure, Harem, Subordinates, the message windows; step 2
/// the map, GalleryEntries.Map.cs; later All Data, GalleryEntries.Data.cs, and the title and the import screen,
/// GalleryEntries.Title.cs).
/// </summary>
internal static partial class GalleryEntries
{
    /// <summary>
    /// The owner's game at week 18 in <c>COMMAND</c>: the kept save <c>saves-week18</c> played through the week's start and
    /// saved on the map (<c>--save-command</c>, docs/ui/gallery.md, §2), so the screens show the week's figures and uses.
    /// </summary>
    public const string Week18 = "saves-week18-command";

    public static IReadOnlyList<GalleryEntry> All() =>
    [
        .. Treasure(),
        .. Harem(),
        .. Subordinates(),
        .. DeploymentScreens(),
        .. SaveLoadScreens(),
        .. AllDataScreens(),
        .. Messages(),
        .. MapScreens(),
        .. SceneScreens(),
        .. PhoneScreens(),
        .. Controls(),
        .. BattleScreens(),
        .. BoardScreens(),
        .. TitleScreens(),
    ];

    /// <summary>Script pixels a screen pixel of the picture's area: the 640×400 script screen fitted into it.</summary>
    private static float PixelsPerScript(Rect2 area) => area.Size.X / 640f;

    // ---------------------------------------------------------------- Treasure (docs/ui/pickers.md, round V)

    private static IEnumerable<GalleryEntry> Treasure()
    {
        yield return new("treasure", "Screens", "Treasure: the characters", Week18, "",
            g => TreasureScreen(g, (s, view, _) => GalleryStates.TreasureView(s, 0)));
        yield return new("treasure-hover", "Screens", "Treasure: a holder hovered", Week18, "",
            g => TreasureScreen(g, (s, view, _) =>
            {
                GalleryStates.TreasureView(s, 0);
                view.ShowHover(FirstHolder(s));
            }));
        yield return new("treasure-items", "Screens", "Treasure: the items, one hovered", Week18, "",
            g => TreasureScreen(g, (s, view, _) =>
            {
                GalleryStates.TreasureView(s, 1);
                view.ShowHover(s.Units[FirstHolder(s)].Item.Value);
            }));
        yield return new("treasure-which-item", "Screens", "Treasure: a unit picked first", Week18, "",
            g => TreasureScreen(g, (s, _, _) => GalleryStates.TreasureUnitPicked(s, FirstHolder(s))));
        yield return new("treasure-replace", "Screens", "Treasure: our question before a replace", Week18, "",
            g => TreasureScreen(g, (s, view, callout) =>
            {
                var holders = GalleryStates.MovableHolders(s);
                if (holders.Count < 2)
                    throw new InvalidOperationException("The save has fewer than two holders of an item that moves.");
                GalleryStates.TreasureItemPicked(s, s.Units[holders[0]].Item.Value);
                var lines = TreasureRoster.ReplaceQuestion(s, holders[1])
                    ?? throw new InvalidOperationException("No question for the second holder.");
                view.ShowHover(holders[1]);                         // touch: the holder selected, Equip pressed
                callout.OpenMenu(["Yes", "No"]);
                return (lines, holders[1], false);
            }));
        yield return new("treasure-question", "Screens", "Treasure: the script's question before a replace", Week18,
            "Sｱｲﾃﾑ.ADV:341-350's question in our words (TreasureRoster.Question).",
            g => TreasureScreen(g, (s, view, callout) =>
            {
                var holders = GalleryStates.MovableHolders(s);
                if (holders.Count < 2)
                    throw new InvalidOperationException("The save has fewer than two holders of an item that moves.");
                int item = s.Units[holders[1]].Item.Value;
                GalleryStates.TreasureUnitPicked(s, holders[0]);
                view.ShowHover(item);                               // touch: the other's item selected, Equip pressed
                callout.OpenMenu(["Yes", "No"]);
                return (TreasureRoster.Question(s, item) ?? throw new InvalidOperationException("No question for the other's item."),
                    item, false);
            }));
        yield return new("treasure-refusal", "Screens", "Treasure: the script's refusal", Week18,
            "The refusal's line is Sｱｲﾃﾑ.ADV:311's; in the touch mode the button greyed with it instead (docs/ui/pickers.md, §6).",
            g => TreasureScreen(g, (s, view, callout) =>
            {
                int unit = FirstHolder(s);
                GalleryStates.TreasureUnitPicked(s, unit);
                if (!TouchMode.On)
                    return (["Already equipped."], s.Units[unit].Item.Value, true);
                view.ShowHover(s.Units[unit].Item.Value);           // his own item selected: Equip greyed
                return null;
            }));
    }

    /// <summary>The first unit at status 1 or 2 holding an item that moves.</summary>
    private static int FirstHolder(GameState s) =>
        GalleryStates.MovableHolders(s) is [var first, ..] ? first
            : throw new InvalidOperationException("The save has no holder of an item that moves.");

    private static Action<Rect2>? TreasureScreen(GalleryStage g, Action<GameState, TreasureView, TreasureCallout> setUp) =>
        TreasureScreen(g, (s, view, callout) =>
        {
            setUp(s, view, callout);
            return null;
        });

    /// <summary>
    /// Our Treasure screen on the save's state as <paramref name="setUp"/> leaves it, and the callout it returns — its lines,
    /// the cell it is at, whether it waits for a click (a refusal) — at that cell, as <c>GameScreen.Treasure</c> shows it.
    /// </summary>
    private static Action<Rect2>? TreasureScreen(GalleryStage g,
        Func<GameState, TreasureView, TreasureCallout, (IReadOnlyList<string> Lines, int Cell, bool Waiting)?> setUp)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var view = g.Add(new TreasureView(g.Font, g.Faces) { Visible = true });
        view.LoadCrops();
        var callout = g.Add(new TreasureCallout(g.Font));
        var asked = setUp(s, view, callout);
        return area =>
        {
            view.Place(area.Position, PixelsPerScript(area));
            view.Refresh(s);
            if (asked is not var (lines, cell, waiting))
                return;
            view.ShowCallout(callout, lines, cell, waiting, area);
            view.Asked = cell;
        };
    }

    // ---------------------------------------------------------------- Harem (docs/ui/pickers.md, H2)

    private static IEnumerable<GalleryEntry> Harem()
    {
        yield return new("harem", "Screens", "Harem", Week18,
            "Royal Guards (13), the longest of Sﾊｰﾚﾑ's names (12 letters), put on offer and chosen, to see how it fits.", g =>
        {
            if (g.State(Week18) is not { } s)
                return null;
            LongestHaremName(s);
            var view = g.Add(new HaremView(g.Font, g.Faces) { Visible = true });
            var girls = g.Bound(s).HaremGirls();
            return area =>
            {
                view.Place(area.Position, PixelsPerScript(area));
                view.Refresh(s, girls);
            };
        });
    }

    /// <summary>
    /// The girl with the longest name of <c>Sﾊｰﾚﾑ</c>'s string table, Royal Guards (13; Sayori Unga next, 11 letters), put on
    /// offer (<c>HaremAvail_*</c>, <c>VAR0249</c> + 12) and made the one chosen (<c>Harem_LastPick</c>), so the grid and the
    /// panel show how it fits (owner, 2026-11-04).
    /// </summary>
    private static void LongestHaremName(GameState s)
    {
        const int royalGuards = 13;
        s.Memory.Var(249 + royalGuards - 1) = 1;
        s.Harem_LastPick = royalGuards;
    }

    // ---------------------------------------------------------------- Subordinates (docs/ui/subordinates.md)

    private static IEnumerable<GalleryEntry> Subordinates()
    {
        yield return new("subordinates", "Screens", "Subordinates", Week18, RosterNote,
            g => SubordinatesScreen(g, (_, _) => { }));
        yield return new("subordinates-question", "Screens", "Subordinates: Replenish asks first", Week18, RosterNote,
            g => SubordinatesScreen(g, (s, view) => view.Press(SubordinateOrderKind.Replenish, Neediest(s))));
        yield return new("subordinates-commander", "Screens", "Subordinates: the Commander tab", Week18,
            "The ledger's Commander tab sorted by Lvl (docs/ui/unit-ledger.md, B5). " + RosterNote,
            g => SubordinatesScreen(g, (_, view) => view.Table.Sort(DeploymentColumn.Level, true), LedgerTab.Commander));
        yield return new("subordinates-result", "Screens", "Subordinates: Replenish's plate", Week18,
            "The plate's figures are Replenish's for the unit as it is now; the order is not run (SubordinateResult.ReplenishPreview). " + RosterNote,
            g => SubordinatesScreen(g, (s, view) =>
            {
                int unit = Neediest(s);
                view.Table.Chosen = unit;                       // the order's unit, chosen as its click chose it
                view.ShowResult(SubordinateResult.ReplenishPreview(s, unit));
            }));
        yield return new("subordinates-new-troops", "Screens", "Subordinates: Add New Troops' plate", Week18,
            "The plate's figures are Add New Troops' third step (+100 on the week-18 save) for the unit as it is now; the order is " +
            "not run (SubordinateResult.AddPreview). " + RosterNote,
            g => SubordinatesScreen(g, (s, view) =>
            {
                int unit = Neediest(s);
                view.Table.Chosen = unit;
                view.ShowResult(SubordinateResult.AddPreview(s, unit, 2));
            }));
    }

    /// <summary>The ledgers' note: the roster the gallery lengthens so it scrolls (<see cref="GalleryStates.LongRoster"/>).</summary>
    private const string RosterNote = "Made up: unit records the save does not list (those with troops) switched on at full troops, "
        + "33 units in all, so the ledger scrolls on every device.";

    private static int Neediest(GameState s) =>
        GalleryStates.NeediestUnit(s) is var unit and > 0 ? unit
            : throw new InvalidOperationException("The save has no unit at status 1 short of troops.");

    /// <summary>
    /// Our Subordinates screen on the save's state, then <paramref name="then"/> once it is placed and filled, and filled
    /// again after it.
    /// </summary>
    private static Action<Rect2>? SubordinatesScreen(GalleryStage g, Action<GameState, SubordinatesView> then,
        LedgerTab tab = LedgerTab.Overview)
    {
        if (g.State(Week18) is not { } s)
            return null;
        GalleryStates.LongRoster(s);
        var view = g.Add(new SubordinatesView(g.Font, g.Faces) { Visible = true });
        view.LoadCrops();
        view.ShowTab(tab, remember: false);                     // the entry's tab, not the player's
        bool done = false;
        return area =>
        {
            view.Place(area.Position, PixelsPerScript(area));
            view.Refresh(s, 0);
            if (done)
                return;
            done = true;
            then(s, view);
            view.Refresh(s, 0);
        };
    }

    // ---------------------------------------------------------------- the message windows (docs/ui/messages.md)

    private const string WindowNote = "The lines are the gallery's own; the scene under the window is masked grey, the portraits are not drawn.";

    /// <summary>
    /// Each message window at a key wait; in the touch pass with the Skip and Log drawer the touch mode shows there, and
    /// once more with MsgSkip on (the drawer's other look; no entry in the other passes, which have no drawer).
    /// </summary>
    private static IEnumerable<GalleryEntry> Messages()
    {
        foreach (var sample in GalleryStates.Messages)
        {
            yield return new(sample.Id, "Messages", $"Message window: {sample.Kind}{(sample.Read ? ", read before" : "")}",
                null, WindowNote, g => MessageWindowOf(g, sample.Window(), skipOn: false, MaskedPicture(g)));
        }
        if (TouchMode.On)
        {
            var speech = GalleryStates.Messages.First(m => m is { Kind: MessageKind.SpeechRight, Read: false });
            yield return new("touch-skip-on", "Messages", "Message window: the drawer with MsgSkip on", null, WindowNote,
                g => MessageWindowOf(g, speech.Window(), skipOn: true, MaskedPicture(g)));
        }
    }

    /// <summary>
    /// <paramref name="window"/> at a key wait over <paramref name="picture"/> (laid over the picture's area) and, in the
    /// touch mode, Skip and Log at the screen's left edge, a window that would cover them drawn smaller (131), as
    /// <c>GameScreen.UpdateSkipButtons</c> shows them; on the phone held landscape in the left side, the Menu cell over
    /// them. <paramref name="portraits"/>: which portraits show, as <c>GameScreen.UpdateMessageWindow</c> tells the view.
    /// </summary>
    private static Action<Rect2>? MessageWindowOf(GalleryStage g, MessageWindow window, bool skipOn, Control picture,
        (bool Left, bool Right)? portraits = null)
    {
        var view = g.Add(new MessageWindowView(g.Font) { Docks = TouchMode.On });
        if (portraits is { } shown)
            view.Portraits = shown;
        view.Show(window, true, null, null);
        view.Complete();
        var drawer = TouchMode.On ? g.Add(new SkipDrawer { Visible = true, SkipOn = skipOn }) : null;
        var menu = TouchMode.On ? MenuCell(g) : null;
        return area =>
        {
            GalleryStage.Fill(picture, area);
            view.Place(area);
            drawer?.Place(area);
            menu?.Place();
        };
    }

    /// <summary>The scene under a sample window: grey with its label, as the agent's dumps mask an event CG (<c>GamePresenter.MaskEventCgs</c>).</summary>
    private static Control MaskedPicture(GalleryStage g)
    {
        var picture = g.Add(new ColorRect { Color = Color.Color8(96, 96, 96), MouseFilter = Control.MouseFilterEnum.Ignore });
        var label = new Label { Text = "CG (masked)" };
        label.AddThemeColorOverride("font_color", Colors.White);
        picture.AddChild(label);
        picture.Resized += () =>                                    // the mask's text at (16, 16) in script pixels, 24 high
        {
            float k = picture.Size.X / 1280;
            label.Position = new Vector2(32, 32) * k;
            label.AddThemeFontSizeOverride("font_size", Math.Max(8, (int)Math.Round(40 * k)));
        };
        picture.GetParent().MoveChild(picture, 0);                  // under the window
        return picture;
    }
}
