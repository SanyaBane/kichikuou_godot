using System.Globalization;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>What a slot holds (ours: the original's lists show the date only).</summary>
public enum SlotKind
{
    /// <summary>Slot 1, the game's own save at each turn's start (<c>ﾛｰﾄﾞｾｰﾌﾞ.ADV:30-33</c>); an auto-save of the library's turn starts.</summary>
    Auto,

    /// <summary>An auto-save written as Turn END was chosen (ours, owner's decision 31: <see cref="AutoRing.TurnEnd"/>).</summary>
    TurnEnd,

    /// <summary>A copy of the autosave: a turn's start (<c>MAIN.ADV:20</c> / <c>:83</c>).</summary>
    TurnStart,

    /// <summary>A save of the moment, made in <c>COMMAND</c> (owner's decision 26, <see cref="WeekLoop.IsMomentSave"/>).</summary>
    MidTurn,
}

/// <summary>The library's saves as our Save/Load screen lists them: the auto-saves newest first, the manual saves by number.</summary>
public sealed record SaveShelf(IReadOnlyList<SaveSlot> Autos, IReadOnlyList<SaveSlot> Manuals);

/// <summary>A marker on a slot's miniature map: its CG, its corner in the miniature's pixels (640×266).</summary>
public sealed record SlotMark(int Cg, int X, int Y);

/// <summary>
/// One save slot as our Save/Load screen shows it (docs/ui/save-load.md, §2): the game it holds read from the slot's
/// memory. <paramref name="Number"/> is the slot (1 Auto, 2-10 the lists' "1"-"9"), 0 for the game being played;
/// <paramref name="Week"/> the week the lists date it by; <paramref name="Troops"/> the status window's sum;
/// <paramref name="Tiles"/> the map's six tile CGs, top row then bottom, left to right; <paramref name="Marks"/> its markers
/// as All Data draws them; <paramref name="Id"/> the save of the library it is (owner's decision 29), with the player's
/// <paramref name="Comment"/>. Presentation only: no rule reads it.
/// </summary>
public sealed record SaveSlot(
    int Number,
    SlotKind Kind,
    int Week,
    int Stage,
    int Gold,
    int Provinces,
    int Units,
    int Troops,
    DateTime? Written,
    IReadOnlyList<int> Tiles,
    IReadOnlyList<SlotMark> Marks,
    SaveId? Id = null,
    string Comment = "")
{
    /// <summary>"Auto", or "Slot n" as the lists number it (slot 2 is "1"); a library's save "Auto-save" or "No. n".</summary>
    public string Label => Id is { } id ? (id.Kind == SaveKind.Auto ? "Auto-save" : $"No. {id.Number}") : SaveSlots.LabelOf(Number);

    /// <summary>Whether the player wrote a comment: the star on the seal, kept by the sweep (docs/ui/save-load.md, §10).</summary>
    public bool Commented => !string.IsNullOrWhiteSpace(Comment);

    /// <summary>Whether it is an auto-save, of either ring.</summary>
    public bool IsAuto => Kind is SlotKind.Auto or SlotKind.TurnEnd;

    /// <summary>
    /// Ours (owner, 2026-11-10; docs/save-import.md, §7, mock-ups R1/R1b): a save of a game imported from the original's save
    /// (<see cref="GameState.ImportedFromOriginal"/>): the "Original" tag at the row's right edge and after the page's
    /// title, with <see cref="SaveSlots.OriginalNote"/> on the page.
    /// </summary>
    public bool Original { get; init; }

    /// <summary>An auto-save's tag (docs/ui/save-load.md, §12): "Turn Start" or "Turn End"; empty for any other save.</summary>
    public string RingText => Kind switch { SlotKind.Auto => "Turn Start", SlotKind.TurnEnd => "Turn End", _ => "" };

    /// <summary>The seal's text: "A", or the list's number; a manual save's number.</summary>
    public string Seal => Id is { Kind: SaveKind.Manual } manual ? manual.Number.ToString(CultureInfo.InvariantCulture)
        : Number == 1 || Id is not null ? "A" : (Number - 1).ToString(CultureInfo.InvariantCulture);

    /// <summary>"LP 3 · Month 5 · Week 2".</summary>
    public string DateText
    {
        get
        {
            var (year, month, week) = StatusStrip.DateOf(Week);
            return $"LP {year} · Month {month} · Week {week}";
        }
    }

    /// <summary>"LP 3 · M5 · W2", the list's.</summary>
    public string ShortDate
    {
        get
        {
            var (year, month, week) = StatusStrip.DateOf(Week);
            return $"LP {year} · M{month} · W{week}";
        }
    }

    /// <summary>The story stage (<c>StoryStage</c>, docs/variables.tsv).</summary>
    public string StageText => Stage switch
    {
        0 => "Prologue · the thieves",
        1 => "Stage 1 · Leazas civil war",
        2 => "Stage 2 · wars of humanity",
        3 => "Stage 3 · the Kayblis war",
        4 => "Stage 4 · the Creator",
        99 => "Prologue · its end",
        _ => $"Stage {Stage}",
    };

    /// <summary>The budget: "116,400K GOLD".</summary>
    public string BudgetText => Figures.Gold(Gold * 100);

    /// <summary>The provinces Leazas holds on the map (Nothing (75) left out), or the hideout in the prologue.</summary>
    public string RealmText => Stage == 0 ? "the hideout" : Provinces == 1 ? "1 province" : $"{Provinces} provinces";

    /// <summary>When the slot was written, "20 Oct, 00:14"; empty for the game being played.</summary>
    public string WrittenShort => Written?.ToString("dd MMM, HH:mm", CultureInfo.InvariantCulture) ?? "";

    /// <summary>When the slot was written, "20 Oct 2026, 00:14"; "now" for the game being played.</summary>
    public string WrittenText => Written?.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture) ?? "now";
}

/// <summary>
/// The save slots for our Save/Load screen (docs/ui/save-load.md, §5): each slot's summary read from the
/// <see cref="ISaveStore"/> — the same files <c>ﾛｰﾄﾞｾｰﾌﾞ</c> lists —, and <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c>'s two slot lists
/// recognised, with the item that answers each for a slot. The screen answers the script's menu as the player's
/// choice would; the script runs as it is.
/// </summary>
public static class SaveSlots
{
    /// <summary>The slots the lists show: Auto and nine.</summary>
    public const int Count = 10;

    private const string File = "ﾛｰﾄﾞｾｰﾌﾞ.ADV";

    /// <summary>"Auto", or "Slot n" as the lists number it (slot 2 is "1").</summary>
    public static string LabelOf(int slot) => slot == 1 ? "Auto" : $"Slot {slot - 1}";

    /// <summary>Slot <paramref name="slot"/>'s summary, or null when it is empty or cannot be read.</summary>
    public static SaveSlot? Read(ISaveStore store, int slot) =>
        store.Read(slot) is { } data ? Of(data, slot, data.Written) : null;

    /// <summary>Slots 1-10 in order, null for an empty one.</summary>
    public static IReadOnlyList<SaveSlot?> ReadAll(ISaveStore store) => [.. Enumerable.Range(1, Count).Select(n => Read(store, n))];

    /// <summary>
    /// A slot's summary from its contents: the memory restored into a state of its own (<see cref="SaveData.Restore(ScriptState)"/>),
    /// dated by its <c>Tmp0021</c> as the lists read it (<c>LP n, Tmp0021, 1</c>, <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV:122</c>).
    /// </summary>
    public static SaveSlot Of(SaveData data, int slot, DateTime? written)
    {
        var state = new GameState();
        data.Restore(state);
        var kind = slot == 1 ? SlotKind.Auto : WeekLoop.IsMomentSave(data.Resume) ? SlotKind.MidTurn : SlotKind.TurnStart;
        return Of(state, slot, kind, state.Tmp0021.Value, written);
    }

    /// <summary>
    /// The library's saves (owner's decision 29): the auto-saves newest first and the manual saves by number, those that
    /// cannot be read left out.
    /// </summary>
    public static SaveShelf ReadLibrary(ISaveLibrary library) => new(
        [.. library.Autos.Select(id => Read(library, id)).OfType<SaveSlot>()],
        [.. library.Manuals.Select(id => Read(library, id)).OfType<SaveSlot>()]);

    /// <summary>One save of the library, or null when it is gone or cannot be read.</summary>
    public static SaveSlot? Read(ISaveLibrary library, SaveId id)
    {
        if (library.Read(id) is not { } data)
            return null;
        var state = new GameState();
        if (data.Variables.Length != state.Memory.VariableCount)
            return null;
        data.Restore(state);
        var kind = id.Kind == SaveKind.Auto ? (library.RingOf(id) == AutoRing.TurnEnd ? SlotKind.TurnEnd : SlotKind.Auto)
            : WeekLoop.IsMomentSave(data.Resume) ? SlotKind.MidTurn : SlotKind.TurnStart;
        // A Turn END save is taken outside ﾛｰﾄﾞｾｰﾌﾞ, which alone sets Tmp0021 to the week: it is dated by Week itself.
        int week = kind == SlotKind.TurnEnd ? state.Week.Value : state.Tmp0021.Value;
        return Of(state, 0, kind, week, data.Written) with { Id = id, Comment = data.Comment };
    }

    /// <summary>
    /// The manual saves as the screen lists them (owner, 2026-10-21: by date alone, no order to choose): the one written
    /// last at the top; none written as the oldest, equal times the higher number first.
    /// </summary>
    public static IReadOnlyList<SaveSlot> NewestFirst(IReadOnlyList<SaveSlot> manuals) =>
        [.. manuals.OrderByDescending(s => s.Written ?? DateTime.MinValue).ThenByDescending(s => s.Id?.Number ?? 0)];

    /// <summary>The manual saves the sweep would delete: those without a comment (Sengoku Rance's, docs/ui/save-load.md, §10).</summary>
    public static IReadOnlyList<SaveSlot> Uncommented(IReadOnlyList<SaveSlot> manuals) => [.. manuals.Where(s => !s.Commented)];

    /// <summary>
    /// The sweep (owner, 2026-10-22, after Sengoku Rance's <c>deleteNoMark</c>): every manual save of
    /// <paramref name="library"/> without a comment deleted, the comment read from the save itself (one that cannot be read
    /// is left); auto-saves are left, with a comment or without (they leave their rings by age). The number deleted.
    /// </summary>
    public static int DeleteUncommented(ISaveLibrary library)
    {
        int deleted = 0;
        foreach (var id in library.Manuals)
            if (library.Read(id) is { } data && string.IsNullOrWhiteSpace(data.Comment) && library.Delete(id))
                deleted++;
        return deleted;
    }

    /// <summary>The tag on a save of a game imported from the original's save (<see cref="SaveSlot.Original"/>).</summary>
    public const string OriginalTag = "Original";

    /// <summary>The page's quiet line on such a save (mock-up R1).</summary>
    public const string OriginalNote = "A game imported from the original's save: \"New scenes first\" and the talk marks are off in it.";

    /// <summary>The game being played, as a save from <c>COMMAND</c> would store it (owner's decision 26).</summary>
    public static SaveSlot Now(GameState state) => Of(state, 0, SlotKind.MidTurn, state.Week.Value, null);

    private static SaveSlot Of(GameState s, int slot, SlotKind kind, int week, DateTime? written)
    {
        var troops = Sys3Int.Zero;                                       // the status window's sum (StatusStrip.For)
        if (s.StoryStage == 0)
        {
            foreach (int unit in (int[])[1, 3, 4])
                troops += s.Units[unit].Troops;
        }
        int units = 0;
        for (int unit = 1; unit <= 50; unit++)
        {
            if (s.Units[unit].Status == 0)
                continue;
            units++;
            if (s.StoryStage != 0)
                troops += s.Units[unit].Troops;
        }
        int provinces = 0;                                               // not Nothing (75): Leazas's, off the map (STR-58)
        for (int prov = 1; prov < s.Provs.Count; prov++)
            if (s.Provs[prov].Owner == 1 && prov != 75)
                provinces++;
        int[] tiles = [s.Ui_MapCgTopL.Value, s.Ui_MapCgTopM.Value, s.Ui_MapCgTopR.Value, s.Ui_MapCgBotL.Value, s.Ui_MapCgBotM.Value, s.Ui_MapCgBotR.Value];
        return new SaveSlot(slot, kind, week, s.StoryStage.Value, s.Gold.Value, provinces, units, troops.Value, written, tiles, MarksOf(s))
        {
            Original = s.ImportedFromOriginal,
        };
    }

    /// <summary>
    /// The markers as All Data draws them (<c>各種DATA.ADV:1-11</c>, <c>GamePresenter.DrawData</c>): those of <c>Marks</c>
    /// with a CG over 10000, as that CG less 10000 at a third of their place, 8 pixels up and left (the script's
    /// arithmetic, saturating at 0).
    /// </summary>
    private static IReadOnlyList<SlotMark> MarksOf(GameState s)
    {
        var marks = new List<SlotMark>();
        for (int i = 0; i < s.Ui_MarkCount.Value && i < s.Marks.Count; i++)
        {
            var mark = s.Marks[(Sys3Int)i];
            var cg = mark.Cg - (Sys3Int)10000;
            if (cg != 0)
                marks.Add(new SlotMark(cg.Value, (mark.X / (Sys3Int)3 - (Sys3Int)8).Value, (mark.Y / (Sys3Int)3 - (Sys3Int)8).Value));
        }
        return marks;
    }

    /// <summary>
    /// Which of <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c>'s slot lists <paramref name="items"/> are, or null: the load list (<c>L_0027f</c>), ten
    /// items, slot n's at <c>122 + 11 (n - 1) + 7</c> when it holds a game and at <c>:319</c> when it is empty; the save
    /// list (<c>L_00739</c>), nine, slot n's at <c>352 + 11 (n - 2) + 3</c>.
    /// </summary>
    public static SaveList? ListOf(IReadOnlyList<SceneMenuItem> items)
    {
        if (items.Any(i => i.At.File != File))
            return null;
        if (items.Count == Count && items.Select((item, k) => item.At.Line == 122 + 11 * k + 7 || item.At.Line == 319).All(b => b))
            return SaveList.Load;
        if (items.Count == Count - 1 && items.Select((item, k) => item.At.Line == 352 + 11 * k + 3).All(b => b))
            return SaveList.Save;
        return null;
    }

    /// <summary>The menu's answer for <paramref name="slot"/>: the load list's items are slots 1-10, the save list's 2-10.</summary>
    public static int ItemOf(SaveList list, int slot) => list == SaveList.Load ? slot : slot - 1;

    /// <summary>Whether <paramref name="list"/> takes a click on <paramref name="slot"/>: a filled one to load, any but Auto to save.</summary>
    public static bool Takes(SaveList list, int slot, SaveSlot? held) => list == SaveList.Load ? held is not null : slot >= 2;

    /// <summary>
    /// The slot chosen when the list opens: to load, the one written last; to save, the first empty one, else the one
    /// written longest ago (ours). Null when the list takes no slot.
    /// </summary>
    public static int? FirstChoice(SaveList list, IReadOnlyList<SaveSlot?> slots)
    {
        var taken = Enumerable.Range(1, slots.Count).Where(n => Takes(list, n, slots[n - 1])).ToList();
        if (taken.Count == 0)
            return null;
        if (list == SaveList.Save && taken.FirstOrDefault(n => slots[n - 1] is null) is var empty and > 0)
            return empty;
        var written = taken.Where(n => slots[n - 1] is not null).ToList();
        if (written.Count == 0)
            return taken[0];
        return list == SaveList.Load
            ? written.MaxBy(n => slots[n - 1]!.Written ?? DateTime.MinValue)
            : written.MinBy(n => slots[n - 1]!.Written ?? DateTime.MinValue);
    }
}
