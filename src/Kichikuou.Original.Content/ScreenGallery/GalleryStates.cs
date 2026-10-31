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

    /// <summary>How many units <see cref="LongRoster"/> lists: more than any device's ledger shows at once (the PC's about 25 rows).</summary>
    public const int RosterLength = 33;

    /// <summary>
    /// The ledgers' roster made long enough to scroll on every device (owner, 2026-10-31): unit records the save does not
    /// list (status 0) and that hold troops switched on in their order until <see cref="RosterLength"/> are listed, each at
    /// its full troops so the save's own units stay the ones short of troops (<see cref="NeediestUnit"/>, Replenish's).
    /// Made up.
    /// </summary>
    public static void LongRoster(GameState s)
    {
        int listed = Enumerable.Range(1, 50).Count(n => s.Units[n].Status.Value != 0);
        for (int n = 1; n <= 50 && listed < RosterLength; n++)
        {
            if (s.Units[n].Status.Value != 0 || s.Units[n].MaxTroops.Value <= 0)        // Sill (unit 2) commands none
                continue;
            s.Units[n].Status = (Sys3Int)1;
            s.Units[n].Troops = s.Units[n].MaxTroops;
            listed++;
        }
    }

    /// <summary>The unit at status 1 with the fewest troops of its maximum, under it (0: none) — Replenish's likely one.</summary>
    public static int NeediestUnit(GameState s) =>
        Enumerable.Range(1, 50)
            .Where(n => s.Units[n].Status.Value == 1 && s.Units[n].Troops < s.Units[n].MaxTroops)
            .OrderBy(n => (double)s.Units[n].Troops.Value / s.Units[n].MaxTroops.Value)
            .FirstOrDefault();

    /// <summary>
    /// The map's camera at its top edge, over the Great Wall of Helman A (49) and the Tower of Evil (78): the view's corner
    /// (275, 0), as a drag there leaves it (<c>WAO_MAP.ADV:29-43</c>; the camera is the view's centre while the map is shown).
    /// </summary>
    public static void MapNorth(GameState s)
    {
        s.MapCamX = 595;
        s.MapCamY = 200;
    }

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
    /// The Military Academy under way with 5 of its 16 weeks left, as Construction (<c>Cﾃｲﾄ.ADV:372-373</c>) and the weeks
    /// since leave it: the building at 2, the timer counting down (<c>COMMAND.ADV:18-23</c>); the week's construction free
    /// again, as in any week after the order.
    /// </summary>
    public static void AcademyUnderWay(GameState s)
    {
        s.Bld_GirlsAcademy = 2;
        s.BuildWeeksLeft = 5;
        s.Ui_CmdBuildAvail = 1;
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
    /// The castle's cells' menu as Dungeon opens it from Leazas Castle (1) (<c>Cｼﾊｲﾄｼ.ADV:732-762</c>, <c>Prov_Cur</c> 1): the
    /// prisoners held, Look Around, Return.
    /// </summary>
    public static IReadOnlyList<SceneMenuItem> CellsMenuItems(GameState s)
    {
        s.Prov_Cur = 1;
        return [.. OwnCityCommand.CellItems(s), OwnCityCommand.CellsReturn];
    }

    /// <summary>
    /// The four prisoners in the castle's cells at once, each at a state its cell is offered in (<c>Cｼﾊｲﾄｼ.ADV:733-753</c>):
    /// Lelyukov 3, Ruberan 1, Papaya 6, the Lady (<c>F_Cafe_State</c>) 10. Made up: the story does not hold all four together.
    /// </summary>
    public static void PrisonersHeld(GameState s)
    {
        s.F_Lelyukov_State = 3;
        s.F_Ruberan_State = 1;
        s.F_Papaya_State = 6;
        s.F_Cafe_State = 10;
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
    /// Made up: every command Leazas Castle's menu can hold at once (<c>Cｼﾊｲﾄｼ.ADV:39-290</c>), 15 and Return — the levy
    /// and the defence, Babolat's pitfall and Galtia's poison asked for, the six buildings standing, Maria (unit 16) in
    /// the forces with her tulip unfinished, Aoi in her rehabilitation, Miki in the castle, a girl in the Special Room,
    /// Lady's caravan at the castle. A look at the menu's height (owner, 2026-10-30).
    /// </summary>
    public static void CastleEveryCommand(GameState s)
    {
        s.Ui_CmdOwnCityAvail = 1;
        s.Provs[1].Economy = s.Provs[1].Economy == 0 ? (Sys3Int)1 : s.Provs[1].Economy;
        s.F_Babolat_State = 2;
        s.F_Galtia_State = 3;
        s.Bld_MariaLab = 1;
        s.Bld_GirlsAcademy = 1;
        s.Bld_Hospital = 1;
        s.Bld_MagicLab = 1;
        s.Bld_Library = 1;
        s.Bld_SMTower = 1;
        s.Units[16].Status = 1;
        s.F_Maria_Tulip = 0;
        s.F_Aoi_State = 10;
        s.F_Miki_InCastle = 1;
        s.N_SpecialRoom_Girls = 1;
        s.LadyCaravan_Prov = 1;
        s.F_Cafe_State = 0;
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

    /// <summary>
    /// <c>Sﾌﾞﾀｲ</c> building a party: its places limit (<c>Tmp0040</c>, the caller's <c>Tmp0021</c>, <c>Sﾌﾞﾀｲ.ADV:5</c>: 4 for
    /// an attack or a defence, 1 for a search), the party picked so far (<c>Bt_PlayerParty1-4</c>, without gaps as the
    /// file keeps them) and the unit whose details the picker shows (<c>Tmp0026</c> set, <c>Tmp0033</c>; 0: none).
    /// </summary>
    public static void Deployment(GameState s, int limit, IReadOnlyList<int> party, int details)
    {
        int At(int i) => i < party.Count ? party[i] : 0;
        s.Tmp0040 = (Sys3Int)limit;
        s.Bt_PlayerParty1 = (Sys3Int)At(0);
        s.Bt_PlayerParty2 = (Sys3Int)At(1);
        s.Bt_PlayerParty3 = (Sys3Int)At(2);
        s.Bt_PlayerParty4 = (Sys3Int)At(3);
        s.Tmp0026 = (Sys3Int)(details == 0 ? 0 : 1);
        s.Tmp0033 = (Sys3Int)details;
    }

    /// <summary>
    /// The comments of <see cref="SampleLibrary"/>'s manual saves, newest first ("" none): twelve, more than any device's
    /// list shows at once, so it scrolls and its bar shows (owner, 2026-10-31).
    /// </summary>
    public static IReadOnlyList<string> SampleComments { get; } =
        ["before the attack on Oaks", "", "", "Lia's scene next week", "", "", "after the Monsters' Cave", "", "", "", "", ""];

    /// <summary>How many weeks before the newest <see cref="SampleLibrary"/>'s manual save <paramref name="place"/> (0 the newest) is: the four newest every other week, the older ones a week apart.</summary>
    public static int ManualWeeksBefore(int place) => place < 4 ? 2 * place : place + 3;

    /// <summary>The comment of <see cref="SampleLibrary"/>'s newest Turn END auto-save.</summary>
    public const string SampleEndComment = "before the rebels' turn";

    /// <summary>
    /// A library for the Save/Load screen, in memory (nothing is written to disk): <paramref name="data"/>'s game as the
    /// newest of the auto-saves and of the manual saves, the older ones the same game a week earlier each (made up: only
    /// the week and the date the lists read, <c>Tmp0021</c>, differ). The auto-saves are both rings full (owner's decision
    /// 31): the turn starts of ten weeks written <paramref name="newest"/> less a day each, the Turn ENDs of the ten weeks
    /// before the newest two hours after their start (the newest with <see cref="SampleEndComment"/>); the manual saves an
    /// hour after their week's start (the newest is the save written last), <see cref="ManualWeeksBefore"/> apart, with
    /// <see cref="SampleComments"/>.
    /// </summary>
    public static MemorySaveLibrary SampleLibrary(SaveData data, DateTime newest)
    {
        var library = new MemorySaveLibrary();
        SaveData WeeksBefore(int weeks, int hours)
        {
            var s = new GameState();
            data.Restore(s);
            s.Week = (Sys3Int)(s.Week.Value - weeks);
            s.Tmp0021 = (Sys3Int)(s.Tmp0021.Value - weeks);         // the week the lists date a save by (SaveSlots.Of)
            return SaveData.Capture(s, data.Resume).Stamped(newest.AddDays(-weeks).AddHours(hours));
        }
        for (int weeks = ISaveLibrary.AutoCount; weeks >= 0; weeks--)
        {
            if (weeks < ISaveLibrary.AutoCount)
                library.WriteAuto(WeeksBefore(weeks, 0));
            if (weeks > 0)
                library.WriteAuto(WeeksBefore(weeks, 2).Commented(weeks == 1 ? SampleEndComment : ""), AutoRing.TurnEnd);
        }
        for (int i = SampleComments.Count - 1; i >= 0; i--)
            library.WriteManual(WeeksBefore(ManualWeeksBefore(i), 1).Commented(SampleComments[i]));
        return library;
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

    private const string AllDataFile = "各種DATA.ADV";

    /// <summary>A menu item of <c>各種DATA</c> registered at <paramref name="line"/>, its text on the next line, as <c>AllDataCommand</c> registers it.</summary>
    private static SceneMenuItem AllDataItem(int line) =>
        new(new ScriptRef(AllDataFile, line), [new CapturedText(new ScriptRef(AllDataFile, line + 1), [], [])]);

    /// <summary>All Data's menu over the map in miniature (<c>各種DATA.ADV:28-47</c>): All Funds, Sundry Expenses, Military Spending, All Casualties, Return.</summary>
    public static IReadOnlyList<SceneMenuItem> AllDataMenu { get; } = [.. new[] { 28, 32, 36, 40, 44 }.Select(AllDataItem)];

    /// <summary>The scale menu a right click on a graph opens (<c>各種DATA.ADV:316-347</c>): 1x to 128x.</summary>
    public static IReadOnlyList<SceneMenuItem> AllDataScaleMenu { get; } = [.. Enumerable.Range(0, 8).Select(i => AllDataItem(316 + 4 * i))];

    /// <summary>
    /// All Data opened (<c>各種DATA.ADV:12-19</c>): the money graphs' and the casualties' scales at 16x, a divisor of
    /// 4095 / 300, unless a visit set them before.
    /// </summary>
    public static void AllDataOpened(GameState s)
    {
        if (s.Ui_MoneyGraphDiv == 0)
        {
            s.Ui_MoneyGraphDiv = (Sys3Int)4095 / 300;
            s.Ui_MoneyGraphScale = 16;
        }
        if (s.Ui_DamageGraphDiv == 0)
        {
            s.Ui_DamageGraphDiv = (Sys3Int)4095 / 300;
            s.Ui_DamageGraphScale = 16;
        }
    }

    /// <summary>
    /// A game cleared once: page 4's clear count (<c>Glob_Fate[79]</c>, which <c>OVER</c> records), so the title unlocks
    /// Fortune Check, the music room and the gallery and rolls for the requests slip and Saitou
    /// (<c>ｽﾀMENU.ADV:53-83</c>; docs/strategy/survey-w4.md, §3.2).
    /// </summary>
    public static void Cleared(GameState s) => s.Fates[79].Fate = 1;

    /// <summary>
    /// Made up: a game cleared once (<see cref="Cleared"/>) and three fates seen, so Fortune Check stamps its first page
    /// (<c>HAPPY_C.ADV:258-296</c>): Sill (fate 0) both her fortune and her misfortune, Menad (fate 7) her fortune,
    /// Chizuko (fate 32, the page's last cell) her misfortune.
    /// </summary>
    public static void FatesSeen(GameState s)
    {
        Cleared(s);
        s.Fates[0].Fate = 100;
        s.Fates[7].Fate = 1;
        s.Fates[32].Fate = 10;
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
        // the day before: the log runs to about twice the PC's paper, so its bar's thumb is short (owner, 2026-10-31)
        new(LogKind.Place, "", ["Leazas Castle", "The War Room"]),
        new(LogKind.Narration, "", ["The generals gathered around the map."]),
        new(LogKind.Speech, "Leila", ["The Royal Guards are ready to march."]),
        new(LogKind.Speech, "Rick", ["The knights too. Give the word."]),
        new(LogKind.Speech, "Rance", ["Not yet. Let them come to us."]),
        new(LogKind.Thought, "Leila", ["(He is planning something again...)"]),
        new(LogKind.Speech, "Menad", ["Sir, a messenger from the north!"]),
        new(LogKind.Narration, "", ["The messenger knelt and handed over", "a sealed letter."]),
        new(LogKind.Speech, "Rance", ["Boring. Read it later, Maris."]),
        new(LogKind.Speech, "Maris", ["As you wish, King Rance."]),
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
