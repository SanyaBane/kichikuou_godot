using Kichikuou.Content.Audio;
using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Audio;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>A fact that needs a local folder; skipped without it.</summary>
public sealed class LocalFolderFactAttribute : FactAttribute
{
    public LocalFolderFactAttribute(params string[] folders)
    {
        foreach (string folder in folders)
        {
            string path = folder switch
            {
                "cg" => LocalData.CgDirectory,
                "pms" => LocalData.PmsDirectory,
                "wav" => LocalData.WavDirectory,
                "scenario" => LocalData.ScenarioDirectory,
                "aindump" => LocalData.AinDumpDirectory,
                _ => LocalData.GameDirectory,
            };
            if (!Directory.Exists(path))
            {
                Skip = $"No local folder {path}.";
                return;
            }
        }
    }
}

/// <summary>The asset loaders against the player's own extracted data (docs/assets.md).</summary>
public class GameDataAssetTests(ITestOutputHelper output)
{
    [LocalFolderFact("pms")]
    public void Every_png_matches_its_pms()
    {
        int count = 0;
        foreach (string pmsPath in Directory.EnumerateFiles(LocalData.PmsDirectory, "*.PMS"))
        {
            var pms = Pms.Load(pmsPath);
            var png = IndexedPng.Load(Path.Combine(LocalData.CgDirectory, Path.GetFileNameWithoutExtension(pmsPath) + ".png"));
            string name = Path.GetFileName(pmsPath);
            Assert.True((pms.Width, pms.Height) == (png.Width, png.Height), name);
            Assert.True((pms.X, pms.Y) == (png.X, png.Y), $"{name}: position");
            Assert.True(pms.PaletteMask == png.PaletteMask, $"{name}: mask");
            Assert.True(pms.FilePalette.AsSpan().SequenceEqual(png.FilePalette), $"{name}: palette");
            Assert.True(pms.Indices.AsSpan().SequenceEqual(png.Indices), $"{name}: pixels");
            count++;
        }
        output.WriteLine($"{count} CGs compared");
        Assert.Equal(1222, count);
    }

    [LocalFolderFact("cg")]
    public void Library_resolves_numbers_and_gaps()
    {
        var library = new CgLibrary(LocalData.CgDirectory);

        var frame = library.Get(807);
        Assert.NotNull(frame);
        Assert.Equal((200, 3, 408, 122), (frame.X, frame.Y, frame.Width, frame.Height));
        Assert.Null(library.Get(0));
        Assert.Null(library.Get(194)); // a gap in the link table
        Assert.Same(frame, library.Get(807));
    }

    /// <summary>
    /// The palette entries the battle's text and frames use (docs/battle/screen-layout.md §7.2)
    /// have the same colour in every CG the battle draws, so text colours do not depend on which
    /// CG was drawn last. CG 806 and 862 (dungeon and gallery screens, never drawn by the battle)
    /// are the exceptions in the UI range.
    /// </summary>
    [LocalFolderFact("cg")]
    public void Battle_text_colours_agree_over_the_battle_cgs()
    {
        var expected = new Dictionary<int, string>
        {
            [0] = "000000", [11] = "d7ebd7", [15] = "879187", [18] = "bec3dc", [22] = "6e7382",
            [178] = "e0e0e0", [190] = "505050", [240] = "dcff91", [249] = "ff0000", [255] = "ffffff",
        };
        var library = new CgLibrary(LocalData.CgDirectory);
        IEnumerable<int> battle = [.. Enumerable.Range(1, 100), .. Enumerable.Range(801, 99), .. Enumerable.Range(887, 33),
            .. Enumerable.Range(1001, 64), .. Enumerable.Range(1100, 431)];
        var mismatches = new List<string>();
        int checkedCgs = 0;
        foreach (int n in battle)
        {
            if (n is 806 or 862 || library.Get(n) is not { PaletteMask: not 0 } cg)
                continue;
            checkedCgs++;
            var palette = cg.OwnPalette;
            foreach (var (index, hex) in expected)
            {
                var (r, g, b) = palette[index];
                string actual = $"{r:x2}{g:x2}{b:x2}";
                if (actual != hex)
                    mismatches.Add($"CG {n}: [{index}] = {actual}, expected {hex}");
            }
        }
        output.WriteLine($"{checkedCgs} CGs checked");
        foreach (string m in mismatches)
            output.WriteLine(m);
        Assert.Empty(mismatches);
    }

    [LocalFolderFact("wav")]
    public void Sound_table_matches_the_wav_folder()
    {
        var files = Directory.EnumerateFiles(LocalData.WavDirectory, "*.WAV").Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(51, SoundTable.Numbers.Count);
        Assert.Equal(files.Count, SoundTable.Numbers.Count);
        foreach (int n in SoundTable.Numbers)
            Assert.Contains(SoundTable.FileName(n), files);
        Assert.Equal("R5ESE059.WAV", SoundTable.FileName(109));
        Assert.True(SoundTable.IsSilent(5));
        Assert.True(SoundTable.IsSilent(3));
        Assert.False(SoundTable.IsSilent(6));
    }

    [LocalFolderFact("game")]
    public void Music_table_maps_SS_n_to_kichiku_n_plus_1()
    {
        var music = new MusicTable(LocalData.GameDirectory);

        Assert.True(music.FromPlaylist);
        Assert.Equal(28, music.Count);
        for (int n = 1; n <= 28; n++)       // the copy's playlist is the translation's, the list a copy without one plays
            Assert.Equal(MusicTable.TranslationPlaylist[n - 1].Replace('\\', Path.DirectorySeparatorChar),
                Path.GetRelativePath(LocalData.GameDirectory, music.PathOf(n)!));
        Assert.Null(music.PathOf(0));
        Assert.EndsWith("kichiku_2.mp3", music.PathOf(1));
        Assert.EndsWith("kichiku_25.mp3", music.PathOf(24));
        Assert.Null(music.PathOf(29));
        for (int n = 1; n <= 28; n++)
            Assert.True(File.Exists(music.PathOf(n)), $"SS {n}");
    }
}
