using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Saves;
using Xunit;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The game folder's saves of the original as Android offers them (docs/save-import.md, §8, the owner's S1 and T1).</summary>
public sealed class OriginalSaveFolderTests : IDisposable
{
    private static readonly PhoneFrame Landscape = PhoneFrame.Landscape(2712, 1220);
    private const float Close = 0.01f;

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "kichikuou-asd-" + Guid.NewGuid().ToString("N"));

    public OriginalSaveFolderTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    /// <summary>A save of the version made at the Leazas week's start of week 18 (LP 3 · Month 5 · Week 2), in the folder.</summary>
    private string WriteSave(string name)
    {
        var asd = new OriginalSaveTests.Asd();
        asd.Pages[0][517] = 18;                                                     // Week (VAR0517)
        string path = Path.Combine(_folder, name);
        File.WriteAllBytes(path, asd.Write());
        return path;
    }

    [Fact]
    public void A_missing_or_empty_folder_holds_none()
    {
        Assert.Empty(OriginalSaveFolder.Read(_folder));
        Assert.Empty(OriginalSaveFolder.Read(Path.Combine(_folder, "none")));
        Assert.Empty(OriginalSaveFolder.Read(""));
    }

    [Fact]
    public void Every_asd_is_read_by_name_a_refused_one_with_its_reason()
    {
        WriteSave("KICHIKUSB.asd");
        File.WriteAllText(Path.Combine(_folder, "KICHIKUSa.ASD"), "not a save");
        File.WriteAllText(Path.Combine(_folder, "notes.txt"), "");
        var saves = OriginalSaveFolder.Read(_folder);
        Assert.Equal(["KICHIKUSa.ASD", "KICHIKUSB.asd"], saves.Select(s => s.File));

        var a = saves[0];
        Assert.True(a.Refused);
        Assert.Equal(OriginalSave.NotASave, a.RefusalWord);
        Assert.Null(a.Save);
        Assert.Equal("Save A", a.Word);

        var b = saves[1];
        Assert.False(b.Refused);
        Assert.NotNull(b.Save);
        Assert.Equal(("B", "Save B", "Original save B"), (b.Letter, b.Word, b.Label));
        Assert.Equal(("LP 3 · M5 · W2", "LP 3 · Month 5 · Week 2", "Prologue"), (b.ShortDate, b.DateText, b.StageText));
        Assert.Equal("Original save B: LP 3 · Month 5 · Week 2", b.QuestionLines[0]);
        Assert.StartsWith("KICHIKUSB.asd, saved ", b.QuestionLines[1]);
    }

    [Fact]
    public void A_file_named_otherwise_is_worded_by_its_name()
    {
        var save = OriginalSaveFolder.ReadFile(WriteSave("rance.asd"));
        Assert.Equal(("", "rance", "Original save"), (save.Letter, save.Word, save.Label));
    }

    [Fact]
    public void The_menus_import_tile_stands_between_screenshot_and_restart()
    {
        var main = PhoneMenu.Main(new PhoneMenu.Settings(false, true, true, false, true));
        Assert.Equal([PhoneMenu.Command.Options, PhoneMenu.Command.MessageSkip, PhoneMenu.Command.Sound, PhoneMenu.Command.Screenshot,
            PhoneMenu.Command.ImportSave, PhoneMenu.Command.Restart, PhoneMenu.Command.Exit], main.Select(t => t.Command));
        Assert.Equal(PhoneMenu.Kind.Page, main[4].Kind);
        Assert.Equal("›", main[4].Sub);
    }

    [Fact]
    public void The_page_lists_the_saves_under_back_or_says_where_they_go()
    {
        WriteSave("KICHIKUSB.asd");
        File.WriteAllText(Path.Combine(_folder, "KICHIKUSA.asd"), "");
        var page = PhoneMenu.Saves(new(OriginalSaveFolder.Read(_folder), "games/kichikuou"));
        Assert.Equal(PhoneMenu.Command.Back, page[0].Command);
        Assert.Equal(new PhoneMenu.Tile(PhoneMenu.Command.OriginalSave, "Save A", PhoneMenu.Kind.Save, Index: 0, Date: "cannot be imported",
            Stage: OriginalSave.NotASave, Refused: true), page[1]);
        Assert.Equal(new PhoneMenu.Tile(PhoneMenu.Command.OriginalSave, "Save B", PhoneMenu.Kind.Save, Index: 1, Date: "LP 3 · M5 · W2",
            Stage: "Prologue"), page[2]);

        var none = PhoneMenu.Saves(new([], "Android/data/org.kichikuou.port/files/game"));
        Assert.Equal([PhoneMenu.Command.Back, PhoneMenu.Command.Note], none.Select(t => t.Command));
        Assert.EndsWith("\nAndroid/data/org.kichikuou.port/files/game", none[1].Label);
    }

    [Fact]
    public void Ten_saves_lie_in_three_rows_a_note_beside_back()
    {
        var page = PhoneMenu.Saves(new([.. "ABCDEFGHIJ".Select(c => new OriginalSaveFile($"KICHIKUS{c}.asd", null, 18, 1, null))], ""));
        var boxes = PhoneMenu.Tiles(Landscape, page);
        float h = (960 - 170 - 32) / 3f;
        Assert.Equal(11, boxes.Count);
        Assert.Equal(h, boxes[0].H, Close);
        Assert.Equal(290 + 2 * (h + 16), boxes[10].Y, Close);                      // J: the third row's third

        var none = PhoneMenu.Saves(new([], "games"));
        var two = PhoneMenu.Tiles(Landscape, none);
        var grid = PhoneMenu.Tiles(Landscape, 8);
        Assert.Equal(grid[0], two[0]);                                              // ‹ Menu the first page's size
        Assert.Equal(new PhoneFrame.Box(grid[1].X, grid[1].Y, grid[3].Right - grid[1].X, grid[7].Bottom - grid[1].Y), two[1]);
        Assert.Equal(1, PhoneMenu.TileAt(Landscape, none, grid[5].X + 10, grid[5].Y + 10));   // the note takes its taps
    }

    [Fact]
    public void A_question_grows_with_its_lines()
    {
        var tiles = PhoneMenu.Tiles(Landscape, 11);
        var one = PhoneMenu.QuestionAt(Landscape, tiles[5]);
        var four = PhoneMenu.QuestionAt(Landscape, tiles[5], 4);
        Assert.Equal(one.Box.H + 3 * PhoneMenu.LinePitch, four.Box.H, Close);
        var three = PhoneMenu.QuestionAt(Landscape, tiles[5], 3);                   // the load's question: no room under the
        Assert.False(three.Below);                                                  // second row, so over it,
        Assert.True(three.Box.Bottom <= tiles[5].Y);                                // kept on the screen, clear of its tile
        Assert.True(PhoneMenu.QuestionAt(Landscape, tiles[1], 4).Below);            // the first row: under it
        Assert.Equal(1100 - 2 * PhoneMenu.LineMargin, PhoneMenu.QuestionTextWidth(Landscape), Close);

        var save = new OriginalSaveFile("KICHIKUSE.asd", null, 18, 1, new DateTime(2026, 10, 10, 21, 40, 0));
        var question = PhoneMenu.ImportQuestion(save);
        Assert.Equal(("Load the original's save?", "Load"), (question.Title, question.Confirm));
        Assert.Equal("Original save E: LP 3 · Month 5 · Week 2\nKICHIKUSE.asd, saved 2026-10-10 21:40\n" + PhoneMenu.LoseProgress, question.Line);
        var refused = PhoneMenu.RefusalOf(save with { Refusal = "It is not a save." });
        Assert.Equal(("KICHIKUSE.asd cannot be imported.", "It is not a save.", ""), (refused.Title, refused.Line, refused.Confirm));
    }
}
