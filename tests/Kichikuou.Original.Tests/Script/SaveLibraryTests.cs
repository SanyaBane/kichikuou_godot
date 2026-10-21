using Kichikuou.Engine.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Tests.Script;

/// <summary>
/// The library of auto-saves and manual saves (owner's decision 29, <c>SaveLibrary.cs</c>) and the context's
/// commands on it (<c>ScriptContext.Library.cs</c>): the ten auto-saves, the manual saves' numbers, deleting, the
/// comment, the persistent page, and the engine's slots migrated.
/// </summary>
public class SaveLibraryTests
{
    private static readonly ScriptRef Call = new("MAIN.ADV", 20);
    private static readonly ScriptRef Here = new("MAIN.ADV", 69);

    private static SaveData Game(int week) => SaveData.Capture(new GameState { Week = (Kichikuou.Engine.Sys3.Sys3Int)week }, Call);

    private static int Week(SaveData? data) => data!.Variables[517].Value;

    [Fact]
    public void The_eleventh_auto_save_pushes_the_oldest_out()
    {
        var library = new MemorySaveLibrary();
        for (int week = 1; week <= 12; week++)
            library.WriteAuto(Game(week));

        Assert.Equal(Enumerable.Range(3, 10).Reverse().Select(n => new SaveId(SaveKind.Auto, n)), library.Autos);
        Assert.Equal(12, Week(library.Read(library.Autos[0])));
        Assert.Null(library.Read(new SaveId(SaveKind.Auto, 2)));
    }

    [Fact]
    public void A_manual_save_is_numbered_after_the_highest_and_keeps_its_comment_when_saved_over()
    {
        var clock = new DateTime(2026, 10, 21, 20, 0, 0);
        var library = new MemorySaveLibrary(() => clock = clock.AddMinutes(1));
        var first = library.WriteManual(Game(5))!.Value;
        var second = library.WriteManual(Game(6))!.Value;
        Assert.Equal([new SaveId(SaveKind.Manual, 1), new SaveId(SaveKind.Manual, 2)], library.Manuals);

        Assert.True(library.SetComment(first, "before Oaks"));
        Assert.Equal(new DateTime(2026, 10, 21, 20, 1, 0), library.Read(first)!.Written);   // the comment keeps the time

        Assert.Equal(first, library.WriteManual(Game(7), over: 1));
        Assert.Equal("before Oaks", library.Read(first)!.Comment);
        Assert.Equal(7, Week(library.Read(first)));
        Assert.Equal(new DateTime(2026, 10, 21, 20, 3, 0), library.Read(first)!.Written);

        Assert.True(library.Delete(second));
        Assert.False(library.Delete(second));
        Assert.Equal(new SaveId(SaveKind.Manual, 2), library.WriteManual(Game(8)));       // after the highest left
    }

    [Fact]
    public void The_directory_library_keeps_its_saves_and_the_comment_round_trips()
    {
        string directory = TempDirectory();
        try
        {
            var library = new DirectorySaveLibrary(directory);
            for (int week = 1; week <= 11; week++)
                library.WriteAuto(Game(week));
            var manual = library.WriteManual(Game(30))!.Value;
            Assert.True(library.SetComment(manual, "Sill's scene next week"));

            var again = new DirectorySaveLibrary(directory);
            Assert.Equal(10, again.Autos.Count);
            Assert.Equal(11, Week(again.Read(again.Autos[0])));
            Assert.False(File.Exists(Path.Combine(directory, "auto-0001.ksav")));
            Assert.Equal("Sill's scene next week", again.Read(manual)!.Comment);
            Assert.True(again.Delete(manual));
            Assert.Empty(again.Manuals);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void The_engine_slots_become_the_newest_auto_save_and_manual_saves_by_their_list_numbers()
    {
        string directory = TempDirectory();
        try
        {
            var slots = new DirectorySaveStore(directory);
            var written = new DateTime(2026, 10, 19, 22, 0, 0);
            slots.Write(1, Game(11).Stamped(written));
            slots.Write(3, Game(10).Stamped(written.AddHours(1)));
            slots.Write(10, Game(18).Stamped(written.AddHours(2)));
            File.WriteAllText(Path.Combine(directory, "msgskip.txt"), "kept");

            var library = new DirectorySaveLibrary(directory);

            Assert.Equal([new SaveId(SaveKind.Auto, 1)], library.Autos);
            Assert.Equal([new SaveId(SaveKind.Manual, 2), new SaveId(SaveKind.Manual, 9)], library.Manuals);
            Assert.Equal(18, Week(library.Read(new SaveId(SaveKind.Manual, 9))));
            Assert.Equal(written.AddHours(1), library.Read(new SaveId(SaveKind.Manual, 2))!.Written);
            Assert.False(File.Exists(Path.Combine(directory, "slot01.ksav")));
            Assert.True(File.Exists(Path.Combine(directory, "legacy", "slot10.ksav")));
            Assert.True(File.Exists(Path.Combine(directory, "msgskip.txt")));
            Assert.Equal(2, new DirectorySaveLibrary(directory).Manuals.Count);   // migrated once
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void The_context_saves_and_loads_through_the_library_and_slot_1_is_the_newest_auto_save()
    {
        var rig = new ScriptRig();
        var library = new MemorySaveLibrary();
        rig.Script.Library = library;
        rig.Script.SaveResume = Call;
        rig.State.Week = 12;
        rig.Script.SaveAuto();
        rig.State.Week = 13;
        rig.Script.SaveAuto();
        Assert.Equal(1, rig.State.RetVal.Value);
        Assert.Equal(13, Week(rig.Script.Saves.Read(1)));                 // Iｱﾘｽ's LP 1
        Assert.Null(rig.Script.Saves.Read(2));

        rig.State.Week = 14;
        rig.Script.SaveManual(null, Here);
        var manual = rig.Script.LastSave!.Value;
        Assert.Equal(new SaveId(SaveKind.Manual, 1), manual);
        rig.Script.SaveManual(null, null);                                // a copy of the newest auto-save
        Assert.Equal(13, Week(library.Read(new SaveId(SaveKind.Manual, 2))));

        rig.State.Week = 40;
        var loaded = Assert.Throws<GameLoadedException>(() => rig.Script.LoadSave(manual));
        Assert.Equal(Here, loaded.Resume);
        Assert.Equal(14, rig.State.Week.Value);
        Assert.Equal(2, library.Autos.Count);                             // a load leaves the auto-saves

        rig.Script.LoadSave(new SaveId(SaveKind.Manual, 9));
        Assert.Equal(255, rig.State.RetVal.Value);
    }

    [Fact]
    public void The_persistent_page_is_read_from_the_newest_save_until_the_library_has_one()
    {
        var clock = new DateTime(2026, 10, 21, 20, 0, 0);
        var library = new MemorySaveLibrary(() => clock = clock.AddMinutes(1));
        var older = new GameState();
        older.Fates[2].Fate = 10;
        library.WriteManual(SaveData.Capture(older, Call));
        var newer = new GameState();
        newer.Fates[2].Fate = 100;
        library.WriteAuto(SaveData.Capture(newer, Call));

        var rig = new ScriptRig();
        rig.Script.Library = library;
        rig.Script.LoadPersistent(195, 2100);
        Assert.Equal(0, rig.State.RetVal.Value);
        Assert.Equal(100, rig.State.Fates[2].Fate.Value);

        rig.State.Fates[2].Fate = 1;
        rig.State.Week = 30;
        rig.Script.SavePersistent(195, 2100);
        Assert.Equal(1, rig.State.RetVal.Value);
        Assert.Equal(1, library.ReadPersistent()!.Pages[4][2].Value);

        rig.State.Fates[2].Fate = 0;
        library.Delete(library.Autos[0]);
        library.Delete(library.Manuals[0]);
        rig.Script.LoadPersistent(195, 2100);
        Assert.Equal(1, rig.State.Fates[2].Fate.Value);                   // no save left, the page stays
    }

    private static string TempDirectory() => Path.Combine(Path.GetTempPath(), "kichikuou-library-" + Guid.NewGuid().ToString("N"));
}
