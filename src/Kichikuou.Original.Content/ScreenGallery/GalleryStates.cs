using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.ScreenGallery;

/// <summary>A sample message window of the screen gallery: its dump's name, its kind, its lines as the script prints them.</summary>
public sealed record MessageSample(string Id, MessageKind Kind, IReadOnlyList<string> Lines, bool Read = false)
{
    /// <summary>The window, printed where <c>WAO_SUB</c> prints it.</summary>
    public MessageWindow Window() => MessageWindow.SampleOf(Kind, Lines, Read);
}

/// <summary>
/// The screen gallery's states (docs/ui/gallery.md, §4): edits of a copy of a kept save's state that put a screen of ours
/// into one of its states without playing to it, and the samples its windows show. No rule reads these states; each
/// edit is one the script makes on the way there (named at each), unless the entry using it says it is made up.
/// </summary>
public static class GalleryStates
{
    /// <summary>
    /// Every kind of our message windows, under the names unattended play dumps them by (CLAUDE.md, "Commands"). The
    /// lines are the gallery's own, not the game's.
    /// </summary>
    public static IReadOnlyList<MessageSample> Messages { get; } =
    [
        new("message-speech-left", MessageKind.SpeechLeft, ["The rebels are gathering in the", "south, King Rance."]),
        new("message-speech-right", MessageKind.SpeechRight, ["Then we strike before they are", "ready. Gather the troops!"]),
        new("message-thought-left", MessageKind.ThoughtLeft, ["(Hmm... something is not right", "about this...)"]),
        new("message-thought-right", MessageKind.ThoughtRight, ["(If only the king would listen", "to reason for once...)"]),
        new("message-narration", MessageKind.Narration, ["Three days later, the army", "reached the border."]),
        new("message-read", MessageKind.Narration, ["Read before: this text takes", "the already-read colour."], Read: true),
        new("message-place-title", MessageKind.PlaceTitle, ["Leazas Castle", "The Throne Room"]),
        new("message-cg-bar-speech", MessageKind.CgBar, ["Maris「The preparations are complete, King Rance.」"]),
        new("message-cg-bar-thought", MessageKind.CgBar, ["Rance（Gahahaha, this is going to be fun.）"]),
        new("message-cg-bar-narration", MessageKind.CgBar, ["The night passed quietly in the castle."]),
    ];

    /// <summary>The Treasure screen's view (<c>Ui_ItemViewMode</c>): 0 the characters, 1 the items, nothing picked.</summary>
    public static void TreasureView(GameState s, int view)
    {
        s.Ui_ItemViewMode = (Sys3Int)view;
        s.Ui_ItemUnitSel = 0;
        s.Ui_ItemSel = 0;
    }

    /// <summary>A unit picked first: <c>Sｱｲﾃﾑ</c> turns to the items and asks "Which item?" (<c>Ui_ItemUnitSel</c>).</summary>
    public static void TreasureUnitPicked(GameState s, int unit)
    {
        TreasureView(s, 1);
        s.Ui_ItemUnitSel = (Sys3Int)unit;
    }

    /// <summary>An item picked first: <c>Sｱｲﾃﾑ</c> turns to the characters and asks "Which character?" (<c>Ui_ItemSel</c>).</summary>
    public static void TreasureItemPicked(GameState s, int item)
    {
        TreasureView(s, 0);
        s.Ui_ItemSel = (Sys3Int)item;
    }

    /// <summary>The units at status 1 or 2 holding an item that moves, in their order (none: an empty list).</summary>
    public static IReadOnlyList<int> MovableHolders(GameState s) =>
        [.. Enumerable.Range(1, 50).Where(n => s.Units[n].Status.Value is 1 or 2
            && s.Units[n].Item.Value is var item && item != 0 && !TreasureRoster.Fixed(item))];

    /// <summary>The unit at status 1 with the fewest troops of its maximum, under it (0: none) — Replenish's likely one.</summary>
    public static int NeediestUnit(GameState s) =>
        Enumerable.Range(1, 50)
            .Where(n => s.Units[n].Status.Value == 1 && s.Units[n].Troops < s.Units[n].MaxTroops)
            .OrderBy(n => (double)s.Units[n].Troops.Value / s.Units[n].MaxTroops.Value)
            .FirstOrDefault();

    /// <summary>
    /// The week's commands spent: their flags at 0, as the commands leave them once used (<c>COMMAND.ADV:38-46</c> sets
    /// them at the week's start); <c>COMMAND</c>'s menu then offers ×Construction, ×Harem, △Merchant and △Subordinates.
    /// </summary>
    public static void CommandsSpent(GameState s)
    {
        s.Ui_CmdBuildAvail = 0;
        s.Ui_CmdHaremAvail = 0;
        s.Ui_CmdMerchantAvail = 0;
        s.Ui_CmdSubordAvail = 0;
    }

    /// <summary>
    /// The first menu of <paramref name="province"/>'s city file as <c>COMMAND</c> calls it on a click there, with
    /// <c>Prov_Cur</c> set to it: an own city's (<c>Cｼﾊｲﾄｼ</c>), else a city's next to ours (<c>Cﾃｷﾄｼ</c>).
    /// </summary>
    public static IReadOnlyList<SceneMenuItem> ProvinceMenuItems(GameState s, int province)
    {
        s.Prov_Cur = (Sys3Int)province;
        return s.Provs[province].Owner.Value == 1 ? OwnCityCommand.MenuItems(s) : EnemyCityCommand.MenuItems(s);
    }

    /// <summary>
    /// Two of the castle's buildings standing, as Construction (<c>Cﾃｲﾄ</c>) leaves them: the Military Academy and the
    /// Genius Hospital, which Leazas Castle's menu then offers as places (<c>Cｼﾊｲﾄｼ.ADV:97</c>, <c>:103</c>).
    /// </summary>
    public static void CastleBuildings(GameState s)
    {
        s.Bld_GirlsAcademy = 1;
        s.Bld_Hospital = 1;
    }

    /// <summary>
    /// An attack of <paramref name="nation"/> on our <paramref name="province"/> as <c>FIGT_B</c> announces it: the nation
    /// file's <c>Ev_PhaseNation</c> and its army's name in string 10 (<c>TCﾘｰｻﾞｽ.ADV:1-3</c> for the rebels, nation 2,
    /// <paramref name="army"/> read by the caller), the attacked province in <c>Prov_Cur</c> and its name in string 8
    /// (<c>FIGT_B.ADV:15</c>, <c>MM 8</c>).
    /// </summary>
    public static void EnemyAttack(GameState s, int province, int nation, string army)
    {
        s.Prov_Cur = (Sys3Int)province;
        s.Strings[8] = s.Strings[(s.Prov_StrBase + s.Prov_Cur - 1).Value];
        s.Ev_PhaseNation = (Sys3Int)nation;
        s.Strings[10] = army;
    }

    /// <summary>The approaching army's places, <c>Bt_EnemyParty1-4</c>, as the nation file's picker fills them (0: empty).</summary>
    public static void EnemyParty(GameState s, params int[] commanders)
    {
        int At(int i) => i < commanders.Length ? commanders[i] : 0;
        s.Bt_EnemyParty1 = (Sys3Int)At(0);
        s.Bt_EnemyParty2 = (Sys3Int)At(1);
        s.Bt_EnemyParty3 = (Sys3Int)At(2);
        s.Bt_EnemyParty4 = (Sys3Int)At(3);
    }

    /// <summary>The Monsters' Cave (dungeon 2), the floor ladder's dungeon: a pit of 14 floors.</summary>
    public const int Cave = 2;

    /// <summary>
    /// A search of <paramref name="dungeon"/> standing on <paramref name="floor"/> (from 1), as the search sets it
    /// (<c>Cﾒｲｷｭｳ.ADV:469-476</c> and its parameters): <c>Dgn_Cur</c>, the floor, the last floor, the mode; and the
    /// dungeon's picture, the default background 1032 of <c>:28</c>, which the cave has, shown by <c>WAO_SUB</c> 331
    /// (<c>CallArg</c>, which it keeps in <c>Ui_CenterCg</c> without overlays, <c>WAO_SUB.ADV:391-402</c>).
    /// </summary>
    public static void DungeonFloor(GameState s, int dungeon, int floor)
    {
        var (last, mode) = DungeonCommand.FloorsOf(dungeon)
            ?? throw new ArgumentException($"Dungeon {dungeon} has no search.", nameof(dungeon));
        s.Dgn_Cur = (Sys3Int)dungeon;
        s.DgnRun_MaxFloor = (Sys3Int)last;
        s.DgnRun_FloorMode = (Sys3Int)mode;
        s.DgnRun_Floor = (Sys3Int)Math.Clamp(floor, 1, last);
        s.DungeonBgCg = 1032;
        s.CallArg = 1032;
        s.Ui_CenterCg = 1032;
        s.Ui_CenterOverlay1 = 0;
        s.Ui_CenterOverlay2 = 0;
        s.Ui_CenterOverlay3 = 0;
        s.Ui_CenterOverlay4 = 0;
    }

    /// <summary>The donations <c>MAIN.ADV</c> prints at a week's start, by their lines, in units of 100K GOLD (<c>:185-211</c>).</summary>
    public static IReadOnlyDictionary<int, int> Donations { get; } = new Dictionary<int, int> { [194] = 20, [201] = 65, [208] = 45 };

    private static ScriptRef Main(int line) => new("MAIN.ADV", line);

    /// <summary>
    /// What our week report reads of a Leazas week's start, as <c>MAIN.ADV:96-212</c> prints it (<c>WeekLoop</c>): the
    /// banner, its date (the state's), the revenue and the troops replenished, the donations at
    /// <paramref name="donationLines"/> (<see cref="Donations"/>), the window closed. The figures are the caller's (the
    /// gallery's save is past them); <c>Gold</c> is moved as the file moves it, from before the revenue to where it is.
    /// Iterate it while the events are shown: the report reads <c>Gold</c> at the banner and at the close.
    /// </summary>
    public static IEnumerable<ScriptEvent> LeazasWeekStart(GameState s, int revenue, int troops, params int[] donationLines)
    {
        int after = s.Gold.Value;
        s.Gold = (Sys3Int)(after - revenue + donationLines.Sum(line => Donations[line]));
        yield return new WindowEvent(113, s.CallArg);
        yield return new MessageEvent(Main(98));
        yield return new ValueMessageEvent(Main(103), [s.DateYear, s.DateMonth, s.DateWeek]);
        s.Gold = (Sys3Int)(s.Gold.Value + revenue);
        yield return new ValueMessageEvent(Main(173), [(Sys3Int)revenue, (Sys3Int)troops]);
        if (donationLines.Length > 0)
            yield return new MessageEvent(Main(188));
        foreach (int line in donationLines)
        {
            s.Gold = (Sys3Int)(s.Gold.Value - Donations[line]);
            yield return new MessageEvent(Main(line));
        }
        yield return new WindowEvent(0, s.CallArg);
    }

    /// <summary>What our week report reads of the thieves' week's start (<c>MAIN.ADV:33-49</c>): the banner and its date alone.</summary>
    public static IEnumerable<ScriptEvent> ThievesWeekStart(GameState s)
    {
        yield return new WindowEvent(113, s.CallArg);
        yield return new MessageEvent(Main(35));
        yield return new ValueMessageEvent(Main(40), [s.DateYear, s.DateMonth, s.DateWeek]);
        yield return new WindowEvent(0, s.CallArg);
    }

    /// <summary>The boot's selection window (<c>ｽﾀｰﾄ.ADV:860</c>, <c>B1 1, 250, 50, 150, 100, 1</c>), where the scenes' menus open.</summary>
    public static ScriptEvent SelectionWindow { get; } =
        new EngineEvent("B1", [(Sys3Int)1, (Sys3Int)250, (Sys3Int)50, (Sys3Int)150, (Sys3Int)100, (Sys3Int)1]);

    /// <summary>A short question's answers for the scene menu's entry (the gallery's own).</summary>
    public static IReadOnlyList<string> MenuSample { get; } = ["Attack at dawn", "Wait for the scouts", "Ask Maris"];

    /// <summary>
    /// A message log's pages for its entry: every kind of page, a question answered among them, more than the paper
    /// holds so it opens scrolled to the end. The lines are the gallery's own, not the game's.
    /// </summary>
    public static IReadOnlyList<LogPage> LogSample { get; } =
    [
        new(LogKind.Place, "", ["Leazas Castle", "The Throne Room"]),
        new(LogKind.Narration, "", ["The morning sun rose over the castle walls."]),
        new(LogKind.Speech, "Maris", ["Good morning, King Rance.", "The ministers are waiting for you."]),
        new(LogKind.Speech, "Rance", ["Let them wait. Gahahaha!"]),
        new(LogKind.Thought, "Maris", ["(As always...)"]),
        new(LogKind.Speech, "Maris", ["There is news from the south.", "The rebels are gathering near Oaks."]),
        new(LogKind.Speech, "Rance", ["Then we strike before they are ready."]),
        new(LogKind.Choice, "", MenuSample, Chosen: 2),
        new(LogKind.Speech, "Rance", ["...No. We wait for the scouts first."]),
        new(LogKind.Speech, "Kanami", ["I will go at once."]),
        new(LogKind.Narration, "", ["Kanami vanished from the room without a sound."]),
        new(LogKind.Thought, "Rance", ["(Hmm... something is not right", "about this...)"]),
        new(LogKind.Narration, "", ["Three days later, the army", "reached the border."]),
        new(LogKind.Speech, "Haurein", ["The troops are in position.", "We await your order, King Rance."]),
        new(LogKind.Speech, "Rance", ["Good. Gather everyone!"]),
    ];
}
