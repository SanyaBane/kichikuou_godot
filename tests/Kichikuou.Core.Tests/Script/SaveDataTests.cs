using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.Script;

/// <summary>
/// The save slots and the engine's save-data commands (<c>ScriptContext.Save.cs</c>), against
/// xsystem35 <c>cmdq.c</c>, <c>cmdl.c</c>, <c>savedata.c</c>: what a slot holds, the result codes
/// in <c>RetVal</c>, partial loads and saves through bound variables, and the slot time.
/// </summary>
public class SaveDataTests
{
    private static readonly ScriptRef Call = new("MAIN.ADV", 20);

    private static ScriptRig Rig(ISaveStore? saves = null)
    {
        var rig = new ScriptRig();
        rig.Script.Saves = saves ?? new MemorySaveStore();
        rig.Script.SaveResume = Call;
        return rig;
    }

    [Fact]
    public void A_slot_holds_page_0_the_saved_pages_and_the_strings_but_not_page_3()
    {
        var rig = Rig();
        rig.State.Week = 42;
        rig.State.Units[3].Troops = 77;                                 // page 1
        rig.State.History[5].Funds = 900;                               // page 2
        rig.State.Buffer[7].Val = 5;                                    // page 3, scratch
        rig.State.Fates[2].Fate = 3;                                    // page 4
        rig.State.Strings[10] = "Kayblis Unit";

        rig.Script.SaveAll(1);

        Assert.Equal(1, rig.State.RetVal.Value);
        var slot = rig.Script.Saves.Read(1)!;
        Assert.Equal(Call, slot.Resume);
        Assert.Equal([1, 2, 4, 10], slot.Pages.Keys.Order());
        var restored = new GameState();
        slot.Restore(restored.Memory);
        Assert.Equal(42, restored.Week.Value);
        Assert.Equal(77, restored.Units[3].Troops.Value);
        Assert.Equal(900, restored.History[5].Funds.Value);
        Assert.Equal(0, restored.Buffer[7].Val.Value);
        Assert.Equal(3, restored.Fates[2].Fate.Value);
        Assert.Equal("Kayblis Unit", restored.Strings[10]);
    }

    [Fact]
    public void Loading_restores_the_slot_sets_RetVal_0_and_leaves_the_running_code()
    {
        var rig = Rig();
        rig.State.Week = 42;
        rig.State.Buffer[7].Val = 5;
        rig.Script.SaveAll(3);
        rig.State.Week = 50;
        rig.State.RetVal = 9;

        var e = Assert.Throws<GameLoadedException>(() => rig.Script.LoadAll(3));

        Assert.Equal(3, e.Slot);
        Assert.Equal(Call, e.Resume);
        Assert.Equal(42, rig.State.Week.Value);
        Assert.Equal(0, rig.State.RetVal.Value);
        Assert.Equal(5, rig.State.Buffer[7].Val.Value);                 // page 3 is not in the slot: kept
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]                                                     // empty
    [InlineData(27)]                                                    // past SAVE_MAXNUMBER
    public void Loading_an_empty_or_impossible_slot_only_sets_RetVal_255(int slot)
    {
        var rig = Rig();
        rig.State.Week = 42;

        rig.Script.LoadAll((Sys3Int)slot);

        Assert.Equal(255, rig.State.RetVal.Value);
        Assert.Equal(42, rig.State.Week.Value);
    }

    [Fact]
    public void Saving_without_the_autosave_call_set_fails_loudly()
    {
        var rig = Rig();
        rig.Script.SaveResume = null;

        Assert.Throws<InvalidOperationException>(() => rig.Script.SaveAll(1));
    }

    [Fact]
    public void A_slot_that_cannot_be_written_gives_255()
    {
        var rig = Rig(new RefusingStore());

        rig.Script.SaveAll(1);

        Assert.Equal(255, rig.State.RetVal.Value);
    }

    [Fact]
    public void A_partial_load_reads_through_the_binding_at_the_cursor_now()
    {
        var rig = Rig();
        rig.State.Tmp0021 = 33;
        rig.State.Glob_FateCur = 0;
        rig.State.Fates[0].Fate = 1;
        rig.State.Fates[1].Fate = 2;
        rig.Script.SaveAll(2);
        rig.State.Tmp0021 = 0;
        rig.State.Fates[0].Fate = 0;
        rig.State.Fates[1].Fate = 0;

        rig.Script.LoadPartial(2, 21, 1);                               // LP 2, Tmp0021, 1
        Assert.Equal(0, rig.State.RetVal.Value);
        Assert.Equal(33, rig.State.Tmp0021.Value);

        rig.State.Glob_FateCur = 1;                                     // Glob_Fate now points at slot 1 of page 4
        rig.Script.LoadPartial(2, 195, 1);                              // LP 2, Glob_Fate, 1
        Assert.Equal(0, rig.State.Fates[0].Fate.Value);
        Assert.Equal(2, rig.State.Fates[1].Fate.Value);
    }

    [Fact]
    public void A_partial_load_stops_at_the_end_of_the_page()
    {
        var rig = Rig();
        rig.State.Glob_FateCur = 0;
        rig.Script.SaveAll(2);

        rig.Script.LoadPartial(2, 195, 60000);                          // far past page 4's 2101 slots

        Assert.Equal(0, rig.State.RetVal.Value);
    }

    [Fact]
    public void A_partial_save_patches_one_slot_and_refuses_unsaved_pages_and_empty_slots()
    {
        var rig = Rig();
        rig.State.Tmp0022 = 1;
        rig.Script.SaveAll(2);
        rig.State.Tmp0022 = 2;

        rig.Script.SavePartial(2, 22, 1);                               // QP 2, Tmp0022, 1
        Assert.Equal(1, rig.State.RetVal.Value);
        Assert.Equal(2, rig.Script.Saves.Read(2)!.Variables[22].Value);

        rig.State.Buf_Cur = 0;
        rig.Script.SavePartial(2, 180, 1);                              // QP 2, Buf_Val, 1: page 3 has no save flag
        Assert.Equal(255, rig.State.RetVal.Value);

        rig.Script.SavePartial(5, 22, 1);                               // empty slot
        Assert.Equal(255, rig.State.RetVal.Value);
    }

    [Fact]
    public void Copying_needs_a_source_slot()
    {
        var rig = Rig();
        rig.State.Week = 12;
        rig.Script.SaveAll(1);

        rig.Script.CopySave(4, 1);
        Assert.Equal(1, rig.State.RetVal.Value);
        Assert.Equal(12, rig.Script.Saves.Read(4)!.Variables[517].Value);

        rig.Script.CopySave(5, 9);
        Assert.Equal(255, rig.State.RetVal.Value);
        Assert.Null(rig.Script.Saves.Read(5));
    }

    [Fact]
    public void The_slot_time_goes_into_six_variables()
    {
        var rig = Rig(new MemorySaveStore(() => new DateTime(2026, 10, 9, 21, 7, 45)));
        rig.Script.SaveAll(1);

        rig.Script.ReadSaveTime(1, 1);                                  // LT 1, Tmp0001
        Assert.Equal(0, rig.State.RetVal.Value);
        Assert.Equal(
            [2026, 10, 9, 21, 7, 45],
            new[] { rig.State.Tmp0001, rig.State.Tmp0002, rig.State.Tmp0003, rig.State.Tmp0004, rig.State.Tmp0005, rig.State.Tmp0006 }.Select(v => (int)v.Value));

        rig.Script.ReadSaveTime(2, 1);                                  // empty: zeros
        Assert.Equal(255, rig.State.RetVal.Value);
        Assert.Equal(0, rig.State.Tmp0006.Value);
    }

    [Fact]
    public void The_file_format_round_trips_and_rejects_foreign_data()
    {
        var state = new GameState();
        state.Week = 300;
        state.Units[1].HP = 65535;
        state.Strings[1] = "ランス";
        var data = SaveData.Capture(state, new ScriptRef("MAIN.ADV", 83));
        var stream = new MemoryStream();
        data.WriteTo(stream);

        stream.Position = 0;
        var read = SaveData.ReadFrom(stream);

        Assert.Equal(data.Resume, read.Resume);
        Assert.Equal(data.Variables, read.Variables);
        Assert.Equal(data.Pages.Keys.Order(), read.Pages.Keys.Order());
        foreach (var (page, values) in data.Pages)
            Assert.Equal(values, read.Pages[page]);
        Assert.Equal(data.Strings, read.Strings);
        Assert.Throws<InvalidDataException>(() => SaveData.ReadFrom(new MemoryStream([1, 2, 3])));
        Assert.Throws<InvalidDataException>(() => SaveData.ReadFrom(new MemoryStream(stream.ToArray()[..100])));
    }

    [Fact]
    public void Slots_on_disk_survive_a_new_store_and_unreadable_files_are_empty()
    {
        string directory = Path.Combine(Path.GetTempPath(), "kichikuou-saves-" + Guid.NewGuid().ToString("N"));
        try
        {
            var state = new GameState();
            state.Week = 77;
            Assert.True(new DirectorySaveStore(directory).Write(3, SaveData.Capture(state, Call)));

            var store = new DirectorySaveStore(directory);
            Assert.Equal(77, store.Read(3)!.Variables[517].Value);
            Assert.NotNull(store.Timestamp(3));
            Assert.Null(store.Read(4));
            Assert.Null(store.Timestamp(4));

            File.WriteAllBytes(Path.Combine(directory, "slot05.ksav"), [9, 9, 9]);
            Assert.Null(store.Read(5));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class RefusingStore : ISaveStore
    {
        public SaveData? Read(int slot) => null;

        public bool Write(int slot, SaveData data) => false;

        public DateTime? Timestamp(int slot) => null;
    }
}
