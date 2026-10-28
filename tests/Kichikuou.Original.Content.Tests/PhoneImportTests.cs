using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneImport"/>: the import screen on the phone held landscape (docs/ui/phone-build.md, step L9), at the
/// mock-ups' figures on the reference phone (docs/ui/phone.md, §23: LI, LIp, LIr), and its folder's and hashes' words.
/// </summary>
public class PhoneImportTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Landscape(2712, 1220);

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    [Fact]
    public void The_sheet_fills_the_frame_the_sides_empty()
    {
        // import_land: sheet_head's box (FX0 + 10, 16, FX1 - 10, FOOT - 4), the paper 26 in; the head at p[1] + 90, + 166,
        // the rule at p[1] + 206 from p[0] + 40 to p[2] - 40, the steps from p[1] + 262.
        Equal(new(390, 16, 1932, 1150), PhoneImport.Panel(Reference));
        Equal(new(416, 42, 1880, 1098), PhoneImport.Paper(Reference));
        Assert.Equal(132, PhoneImport.HeadingMiddle(Reference), Close);
        Assert.Equal(208, PhoneImport.VersionMiddle(Reference), Close);
        Assert.Equal((456f, 2256f, 248f), PhoneImport.Rule(Reference));
        Assert.Equal(304, PhoneImport.StepsTop(Reference), Close);
        Assert.True(PhoneImport.Panel(Reference).X >= Reference.Frame.X);
        Assert.True(PhoneImport.Panel(Reference).Right <= Reference.Frame.Right);
    }

    [Fact]
    public void The_steps_stand_in_two_columns_either_side_of_the_frame_s_middle()
    {
        // import_land: cx = 1356; lx0, lx1 = p[0] + 50, cx - 40; rx0, rx1 = cx + 40, p[2] - 50; the seal at x0 + 34, the
        // words at x0 + 100; the field (lx0 + 100, ly, lx1, ly + 150).
        Assert.Equal((466f, 1316f), PhoneImport.Column(Reference, 0));
        Assert.Equal((1396f, 2246f), PhoneImport.Column(Reference, 1));
        Assert.Equal(500, PhoneImport.MarkX(Reference, 0), Close);
        Assert.Equal(1430, PhoneImport.MarkX(Reference, 1), Close);
        Assert.Equal(566, PhoneImport.TextLeft(Reference, 0), Close);
        Assert.Equal(1496, PhoneImport.TextLeft(Reference, 1), Close);
        Equal(new(566, 530, 750, 150), PhoneImport.Field(Reference, 530));
    }

    [Fact]
    public void Importing_ticks_the_folder_and_refused_stamps_beside_the_field()
    {
        // LIp: the tick at lx1 - 20, the folder's words to lx1 - 60; LIr: the stamp at rx0 + 260.
        Assert.Equal(1296, PhoneImport.TickX(Reference, 0), Close);
        Assert.Equal(1256, PhoneImport.TickedRight(Reference, 0), Close);
        Assert.Equal(1656, PhoneImport.StampX(Reference), Close);
        Assert.Equal(2226, PhoneImport.TickX(Reference, 1), Close);
    }

    [Fact]
    public void The_red_button_stands_at_the_right_thumb_Back_left_of_it()
    {
        // foot = p[3] - 30; Import (rx1 - 520, foot - 130, rx1, foot), Choose another folder (rx1 - 640, …); Back's arrow
        // at lx0 (a folder chosen) or rx0 (refused), its middle at foot - 65.
        Equal(new(1726, 980, 520, 130), PhoneImport.Button(Reference, refused: false));
        Equal(new(1606, 980, 640, 130), PhoneImport.Button(Reference, refused: true));
        Equal(new(466, 980, 640, 130), PhoneImport.Back(Reference, refused: false));
        Equal(new(1396, 980, 190, 130), PhoneImport.Back(Reference, refused: true));
        Assert.Equal(1045, PhoneImport.Back(Reference, refused: false).Y + PhoneImport.Back(Reference, refused: false).H / 2, Close);
        Assert.True(PhoneImport.Button(Reference, true).Bottom <= Reference.Screen.H - Reference.Safe.Bottom);
        Assert.Equal("Back to the title", PhoneImport.BackWord(refused: false));
        Assert.Equal("Back", PhoneImport.BackWord(refused: true));
    }

    [Fact]
    public void A_longer_screen_keeps_the_sheet_in_the_frame()
    {
        var longer = PhoneFrame.Landscape(2800, 1220);
        Assert.Equal(PhoneImport.Panel(Reference).W, PhoneImport.Panel(longer).W, Close);
        Assert.Equal(longer.Frame.X + 10, PhoneImport.Panel(longer).X, Close);
        Assert.Equal(longer.Frame.X + longer.Frame.W / 2 + 40, PhoneImport.Column(longer, 1).Left, Close);
    }

    [Fact]
    public void A_folder_shows_its_name_over_its_place_Android_s_storage_named()
    {
        Assert.Equal("Internal storage › Games › Kichikuou Rance", PhoneImport.Readable("/storage/emulated/0/Games/Kichikuou Rance"));
        Assert.Equal("Internal storage › Games", PhoneImport.Readable("/sdcard/Games/"));
        Assert.Equal("Internal storage", PhoneImport.Readable("/storage/emulated/0"));
        Assert.Equal("C: › Games › Kichikuou Rance", PhoneImport.Readable(@"C:\Games\Kichikuou Rance"));
        Assert.Equal(("Kichikuou Rance", "Internal storage › Games"), PhoneImport.FolderLines("/storage/emulated/0/Games/Kichikuou Rance"));
        Assert.Equal(("game", "Internal storage › Android › data › org.kichikuou.port › files"),
            PhoneImport.FolderLines("/storage/emulated/0/Android/data/org.kichikuou.port/files/game"));
        Assert.Equal(("Kichikuou Rance", "C: › Games"), PhoneImport.FolderLines(@"C:\Games\Kichikuou Rance"));
        Assert.Equal(("C:", ""), PhoneImport.FolderLines(@"C:\"));
        // "/storage/emulated/01" is not the storage itself
        Assert.Equal("storage › emulated › 01", PhoneImport.Readable("/storage/emulated/01"));
    }

    [Fact]
    public void A_refusal_names_the_folder_as_the_field_does()
    {
        const string folder = "/storage/emulated/0/Games/Kichikuou Rance";
        Assert.Equal("KICHIKUSA.ALD in Internal storage › Games › Kichikuou Rance is not the file.",
            PhoneImport.Readable($"KICHIKUSA.ALD in {folder} is not the file.", folder));
        Assert.Equal("Nothing here.", PhoneImport.Readable("Nothing here.", ""));
    }

    [Fact]
    public void The_hashes_go_in_two_lines()
    {
        // LIr: ('its SHA-256 is ' + EMPTY_SHA + ',', SA_SHA + ' is expected.'), as the screen keeps them (SplitHashes).
        Assert.Equal(["its SHA-256 is e3b0,", "699d is expected."], PhoneImport.HashLines("its SHA-256 is e3b0, 699d is expected"));
        Assert.Equal(["its SHA-256 is e3b0"], PhoneImport.HashLines("its SHA-256 is e3b0"));
        Assert.Empty(PhoneImport.HashLines(""));
    }
}
