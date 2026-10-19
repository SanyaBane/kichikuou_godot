using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="SaveSlots"/>: the slots' summaries for our Save/Load screen (docs/ui/save-load.md, §5) and
/// <c>ﾛｰﾄﾞｾｰﾌﾞ</c>'s two slot lists recognised as the script builds them (that part skipped without the decompile).
/// </summary>
public class SaveSlotsTests
{
    private static readonly ScriptRef Autosave = new("MAIN.ADV", 83);
    private static readonly ScriptRef Moment = new("MAIN.ADV", 213);

    private static GameState Game(int week)
    {
        var s = new GameState
        {
            Week = (Sys3Int)week,
            Tmp0021 = (Sys3Int)week,
            StoryStage = 1,
            Gold = 1164,
            Ui_MapCgTopL = 11, Ui_MapCgTopM = 12, Ui_MapCgTopR = 13,
            Ui_MapCgBotL = 14, Ui_MapCgBotM = 15, Ui_MapCgBotR = 16,
            Ui_MarkCount = 3,
        };
        s.Units[1].Status = 1;
        s.Units[1].Troops = 1200;
        s.Units[6].Status = 2;
        s.Units[6].Troops = 800;
        s.Units[7].Troops = 500;                                        // not in the army: not counted
        s.Provs[1].Owner = 1;
        s.Provs[6].Owner = 1;
        s.Provs[4].Owner = 2;
        s.Provs[75].Owner = 1;                                          // Nothing (75): Leazas's, off the map, not counted
        s.Marks[(Sys3Int)0].Cg = 10200;
        s.Marks[(Sys3Int)0].X = 300;
        s.Marks[(Sys3Int)0].Y = 120;
        s.Marks[(Sys3Int)1].Cg = 200;                                   // under 10000: All Data leaves it out
        s.Marks[(Sys3Int)2].Cg = 10201;
        s.Marks[(Sys3Int)2].X = 12;                                     // 12 / 3 - 8 saturates at 0
        s.Marks[(Sys3Int)2].Y = 90;
        return s;
    }

    [Fact]
    public void A_slot_reads_its_game()
    {
        var saves = new MemorySaveStore(() => new DateTime(2026, 10, 20, 0, 14, 5));
        saves.Write(1, SaveData.Capture(Game(18), Autosave));
        saves.Write(9, SaveData.Capture(Game(18), Moment));
        saves.Write(3, SaveData.Capture(Game(11), Autosave));

        var slots = SaveSlots.ReadAll(saves);

        Assert.Equal(10, slots.Count);
        Assert.Null(slots[1]);
        var auto = slots[0]!;
        Assert.Equal(SlotKind.Auto, auto.Kind);
        Assert.Equal("Auto", auto.Label);
        Assert.Equal("A", auto.Seal);
        Assert.Equal(SlotKind.TurnStart, slots[2]!.Kind);
        Assert.Equal("Slot 2", slots[2]!.Label);
        Assert.Equal("LP 3 · Month 3 · Week 3", slots[2]!.DateText);
        var moment = slots[8]!;
        Assert.Equal(SlotKind.MidTurn, moment.Kind);
        Assert.Equal("8", moment.Seal);
        Assert.Equal("LP 3 · M5 · W2", moment.ShortDate);
        Assert.Equal("Mid-turn", moment.KindText);
        Assert.Equal("Stage 1 · Leazas civil war", moment.StageText);
        Assert.Equal("116400K GOLD", moment.BudgetText);
        Assert.Equal("2 provinces", moment.RealmText);
        Assert.Equal(2, moment.Units);
        Assert.Equal(2000, moment.Troops);
        Assert.Equal("20 Oct, 00:14", moment.WrittenShort);
        Assert.Equal("20 Oct 2026, 00:14", moment.WrittenText);
        Assert.Equal([11, 12, 13, 14, 15, 16], moment.Tiles);
        Assert.Equal([new SlotMark(200, 92, 32), new SlotMark(201, 0, 22)], moment.Marks);
    }

    [Fact]
    public void The_game_being_played_is_dated_by_its_week()
    {
        var s = Game(18);
        s.Tmp0021 = 0;                                                  // the script's scratch, whatever it holds now
        s.StoryStage = 0;
        s.Units[3].Troops = 40;                                         // the thieves' window sums units 1, 3 and 4

        var now = SaveSlots.Now(s);

        Assert.Equal(SlotKind.MidTurn, now.Kind);
        Assert.Equal("LP 3 · Month 5 · Week 2", now.DateText);
        Assert.Equal("the hideout", now.RealmText);
        Assert.Equal(1240, now.Troops);
        Assert.Equal("now", now.WrittenText);
    }

    [Fact]
    public void The_first_choice_is_the_last_game_to_load_and_an_empty_slot_to_save()
    {
        var clock = new DateTime(2026, 10, 20, 0, 0, 0);
        var saves = new MemorySaveStore(() => clock = clock.AddMinutes(1));
        saves.Write(1, SaveData.Capture(Game(18), Autosave));
        saves.Write(4, SaveData.Capture(Game(18), Moment));
        saves.Write(2, SaveData.Capture(Game(11), Autosave));
        var slots = SaveSlots.ReadAll(saves);

        Assert.Equal(2, SaveSlots.FirstChoice(SaveList.Load, slots));
        Assert.Equal(3, SaveSlots.FirstChoice(SaveList.Save, slots));
        Assert.False(SaveSlots.Takes(SaveList.Load, 3, slots[2]));     // an empty slot loads nothing
        Assert.False(SaveSlots.Takes(SaveList.Save, 1, slots[0]));     // Auto is the game's to write

        foreach (int n in (int[])[3, 5, 6, 7, 8, 9, 10])
            saves.Write(n, SaveData.Capture(Game(12), Autosave));
        Assert.Equal(4, SaveSlots.FirstChoice(SaveList.Save, SaveSlots.ReadAll(saves)));   // every slot full: the oldest
        Assert.Null(SaveSlots.FirstChoice(SaveList.Load, SaveSlots.ReadAll(new MemorySaveStore())));
    }

    /// <summary>The manual saves by slot or by date, each both ways; the empty slots last either way.</summary>
    [Fact]
    public void The_manual_saves_sort_by_slot_or_by_date()
    {
        var clock = new DateTime(2026, 10, 20, 0, 0, 0);
        var saves = new MemorySaveStore(() => clock = clock.AddMinutes(1));
        foreach (int n in (int[])[1, 5, 2, 9, 4])                     // written in this order
            saves.Write(n, SaveData.Capture(Game(12), Autosave));
        saves.Write(7, SaveData.Capture(Game(12), Autosave).Stamped(new DateTime(2026, 10, 20, 0, 3, 0)));   // as slot 2
        var slots = SaveSlots.ReadAll(saves);

        Assert.Equal([2, 3, 4, 5, 6, 7, 8, 9, 10], SaveSlots.Order(slots, SlotOrder.Default));
        Assert.Equal([10, 9, 8, 7, 6, 5, 4, 3, 2], SaveSlots.Order(slots, new SlotOrder(SlotSortKey.Slot, true)));
        Assert.Equal([4, 9, 2, 7, 5, 3, 6, 8, 10], SaveSlots.Order(slots, new SlotOrder(SlotSortKey.Date, true)));
        Assert.Equal([5, 2, 7, 9, 4, 3, 6, 8, 10], SaveSlots.Order(slots, new SlotOrder(SlotSortKey.Date, false)));
    }

    [Fact]
    public void A_click_turns_the_active_order_and_opens_the_other()
    {
        var bySlot = SlotOrder.Default;
        Assert.Equal(new SlotOrder(SlotSortKey.Slot, true), bySlot.Clicked(SlotSortKey.Slot));
        Assert.Equal(new SlotOrder(SlotSortKey.Date, true), bySlot.Clicked(SlotSortKey.Date));          // newest first
        Assert.Equal(new SlotOrder(SlotSortKey.Date, false), bySlot.Clicked(SlotSortKey.Date).Clicked(SlotSortKey.Date));
        Assert.Equal(SlotOrder.Default, new SlotOrder(SlotSortKey.Date, false).Clicked(SlotSortKey.Slot));
    }

    [Fact]
    public void An_item_answers_its_slot()
    {
        Assert.Equal(1, SaveSlots.ItemOf(SaveList.Load, 1));
        Assert.Equal(9, SaveSlots.ItemOf(SaveList.Load, 9));
        Assert.Equal(1, SaveSlots.ItemOf(SaveList.Save, 2));
        Assert.Equal(9, SaveSlots.ItemOf(SaveList.Save, 10));
    }

    [Fact]
    public void Other_menus_are_not_slot_lists()
    {
        static IReadOnlyList<SceneMenuItem> Items(string file, IEnumerable<int> lines) =>
            [.. lines.Select(line => new SceneMenuItem(new ScriptRef(file, line), []))];

        Assert.Null(SaveSlots.ListOf(Items("ﾛｰﾄﾞｾｰﾌﾞ.ADV", [91, 95, 99])));
        Assert.Null(SaveSlots.ListOf(Items("COMMAND.ADV", Enumerable.Range(0, 9).Select(k => 355 + 11 * k))));
        Assert.Null(SaveSlots.ListOf(Items("ﾛｰﾄﾞｾｰﾌﾞ.ADV", Enumerable.Range(0, 8).Select(k => 355 + 11 * k))));
        Assert.Equal(SaveList.Save, SaveSlots.ListOf(Items("ﾛｰﾄﾞｾｰﾌﾞ.ADV", Enumerable.Range(0, 9).Select(k => 355 + 11 * k))));
    }

    /// <summary>The lists as <see cref="LoadSaveScreen"/> builds them from the player's files: a save, then the load list.</summary>
    [Fact]
    public async Task The_scripts_lists_are_recognised()
    {
        if (!System.IO.File.Exists(Path.Combine(LocalData.ScenarioDirectory, "ﾛｰﾄﾞｾｰﾌﾞ.ADV")))
            return;
        var state = new GameState { Week = 18 };
        var input = new ScriptedSceneInput();
        var saves = new MemorySaveStore();
        var script = new OriginalScriptContext(state, new QueuedRandom(), new RecordingView(), input, new RecordingRules { State = state })
        {
            Saves = saves,
            SaveResume = Autosave,
        };
        state.Ui_WinType = 43;
        var screen = new LoadSaveScreen(script, new ScenarioScripts(LocalData.ScenarioDirectory));
        state.RetVal = 3;
        await screen.RunAsync();                                        // the autosave: slot 1

        state.RetVal = 2;
        input.Choices.Enqueue(2);                                       // Save
        input.Choices.Enqueue(SaveSlots.ItemOf(SaveList.Save, 4));      // "3", slot 4
        input.Choices.Enqueue(0);                                       // leave the list
        input.Choices.Enqueue(1);                                       // Load
        input.Choices.Enqueue(0);                                       // leave the list
        input.Choices.Enqueue(3);                                       // Don't
        await screen.RunAsync();

        Assert.Equal([null, SaveList.Save, SaveList.Save, null, SaveList.Load, null], input.Menus.Select(SaveSlots.ListOf));
        Assert.NotNull(saves.Read(4));
        var load = input.Menus[4];
        Assert.Equal([true, false, false, true, false, false, false, false, false, false],
            load.Select(item => item.At.Line != 319));                  // the filled slots' items, Auto and slot 4
    }
}
