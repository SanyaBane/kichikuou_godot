using System.Buffers.Binary;
using System.Text;
using Kichikuou.Content.Archives;
using Kichikuou.Content.Audio;
using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Audio;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The ALD reader against the player's own game folder and the extract xsys35c 1.13 made from it
/// (docs/import.md, step 1): every resource where the extract says, with the same bytes.
/// </summary>
public class GameArchiveTests(ITestOutputHelper output)
{
    static GameArchiveTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>A name as xsys35c decodes it (Shift JIS); the decoder of our own comes with step 5.</summary>
    private static string Name(AldEntry entry) => Encoding.GetEncoding(932).GetString(entry.RawName.Span);

    /// <summary>
    /// <c>KICHIKUSA.ALD</c>: the scenario in the order of xsys35dc's <c>xsys35dc.hed</c>, each page an
    /// <c>S380</c> file whose header names its own page, which is its link-table index.
    /// </summary>
    [LocalFolderFact("game", "scenario")]
    public void Scenario_archive_holds_the_decompiled_files_in_order()
    {
        var archive = AldArchive.Open(LocalData.GameDirectory, "KICHIKUS");
        string[] hed = File.ReadAllLines(Path.Combine(LocalData.ScenarioDirectory, "xsys35dc.hed"), Encoding.UTF8);
        Assert.Equal("#SYSTEM35", hed[0]);
        string[] files = hed[1..];

        var entries = archive.Entries.ToList();
        Assert.Equal(201, entries.Count);
        Assert.Equal(files.Length, entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            Assert.Equal(files[i], Path.ChangeExtension(Name(entry), ".ADV"));
            Assert.True(File.Exists(Path.Combine(LocalData.ScenarioDirectory, files[i])), files[i]);
            byte[] sco = entry.ReadAllBytes();
            Assert.Equal("S380", Encoding.ASCII.GetString(sco, 0, 4));
            Assert.Equal(sco.Length, BinaryPrimitives.ReadInt32LittleEndian(sco.AsSpan(8)));
            Assert.Equal(entry.Index, BinaryPrimitives.ReadInt32LittleEndian(sco.AsSpan(12)));
        }
        output.WriteLine($"{entries.Count} pages, link table {archive.Count}, first {Name(entries[0])}");
    }

    /// <summary>
    /// <c>KICHIKUGA.ALD</c> + <c>KICHIKUGB.ALD</c>: the 1222 CGs equal the PMS files <c>ald</c> extracted, in
    /// the volume <c>cg_manifest.txt</c> gives; the CG n is the link-table index n − 1.
    /// </summary>
    [LocalFolderFact("game", "pms")]
    public void Cg_archive_matches_the_extracted_pms()
    {
        var archive = AldArchive.Open(LocalData.GameDirectory, "KICHIKUG");
        var manifest = File.ReadAllLines(Path.Combine(LocalData.PmsDirectory, "..", "cg_manifest.txt"))
            .Select(line => line.Split(','))
            .ToDictionary(fields => fields[2], fields => (Volume: int.Parse(fields[0]), Number: int.Parse(fields[1])));

        int count = 0;
        foreach (var entry in archive.Entries)
        {
            string name = Name(entry);
            Assert.True(manifest.TryGetValue(name, out var listed), name);
            Assert.Equal(listed.Volume, entry.Volume);
            Assert.Equal(listed.Number, entry.Index + 1);
            Assert.Equal($"CG{entry.Index + 1:D4}.PMS", name);
            Assert.True(File.ReadAllBytes(Path.Combine(LocalData.PmsDirectory, name)).AsSpan().SequenceEqual(entry.ReadAllBytes()), name);
            count++;
        }
        Assert.Equal(1222, count);
        Assert.Equal(manifest.Count, count);
        Assert.Null(archive[193]); // CG 194, a gap (GameDataAssetTests.Library_resolves_numbers_and_gaps)
        output.WriteLine($"{count} CGs, link table {archive.Count}, volume B {archive.Entries.Count(e => e.Volume == 2)}");
    }

    /// <summary>
    /// The CG library over the archive gives every CG the library over <c>cg_png</c> gives — size, position,
    /// palette mask, palette, indices — and the same gaps, numbers and header sizes (docs/import.md, step 2).
    /// </summary>
    [LocalFolderFact("game", "cg")]
    public void Every_cg_from_the_archive_equals_its_png()
    {
        var archive = AldArchive.Open(LocalData.GameDirectory, "KICHIKUG");
        var fromArchive = new CgLibrary(archive);
        var fromPng = new CgLibrary(LocalData.CgDirectory);

        Assert.Equal(fromPng.Numbers, fromArchive.Numbers);
        int count = 0;
        for (int n = -1; n <= archive.Count + 1; n++)
        {
            var png = fromPng.Get(n);
            var pms = fromArchive.Get(n);
            Assert.Equal(fromPng.SizeOf(n), fromArchive.SizeOf(n));
            if (png is null)
            {
                Assert.Null(pms);
                continue;
            }
            Assert.NotNull(pms);
            Assert.True((pms.Width, pms.Height) == (png.Width, png.Height), $"CG {n}: size");
            Assert.True((pms.X, pms.Y) == (png.X, png.Y), $"CG {n}: position");
            Assert.True(pms.PaletteMask == png.PaletteMask, $"CG {n}: mask");
            Assert.True(pms.FilePalette.AsSpan().SequenceEqual(png.FilePalette), $"CG {n}: palette");
            Assert.True(pms.Indices.AsSpan().SequenceEqual(png.Indices), $"CG {n}: pixels");
            Assert.Equal((pms.Width, pms.Height), fromArchive.SizeOf(n));
            count++;
        }
        Assert.Equal(1222, count);
    }

    /// <summary>
    /// <c>KICHIKUWA.ALD</c>: the 51 sounds equal the extracted WAVs, and the sound n is the link-table index
    /// n − 1 with the name the sound table gives it.
    /// </summary>
    [LocalFolderFact("game", "wav")]
    public void Sound_archive_matches_the_extracted_wavs()
    {
        var archive = AldArchive.Open(LocalData.GameDirectory, "KICHIKUW");

        int count = 0;
        foreach (var entry in archive.Entries)
        {
            string name = Name(entry);
            Assert.True(File.ReadAllBytes(Path.Combine(LocalData.WavDirectory, name)).AsSpan().SequenceEqual(entry.ReadAllBytes()), name);
            count++;
        }
        Assert.Equal(51, count);
        Assert.Equal(Directory.GetFiles(LocalData.WavDirectory, "*.WAV").Length, count);
        foreach (int number in SoundTable.Numbers)
        {
            Assert.Equal(SoundTable.FileName(number), archive[number - 1] is { } entry ? Name(entry) : null);
            Assert.Same(archive[number - 1], archive.Find(SoundTable.FileName(number)!.ToLowerInvariant()));
        }
        Assert.Equal(SoundTable.Numbers.Count, count);
    }

    /// <summary>
    /// <c>System39.ain</c>: the variables and the messages equal xsys35dc's JSON dump of it
    /// (<c>aindump\System39.ain.txt</c>; docs/import.md, step 4).
    /// </summary>
    [LocalFolderFact("game", "aindump")]
    public void Ain_matches_the_dump()
    {
        var ain = AinFile.Read(Path.Combine(LocalData.GameDirectory, "System39.ain"));
        using var dump = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(LocalData.AinDumpDirectory, "System39.ain.txt")));
        var root = dump.RootElement;
        Assert.Empty(root.GetProperty("HEL0").EnumerateObject());
        Assert.Empty(root.GetProperty("FUNC").EnumerateObject());

        string[] variables = [.. root.GetProperty("VARI").EnumerateArray().Select(v => v.GetString()!)];
        Assert.Equal(806, ain.Variables.Count);
        Assert.Equal(variables, ain.Variables.Select(v => Encoding.ASCII.GetString(v)));

        // The dump went through a UTF-8 reading of its Shift JIS bytes on its way to the file, which made every
        // byte sequence that is no UTF-8 a U+FFFD; the same reading of ours must give the same strings, up to how
        // many U+FFFD a run of such bytes makes (decoders differ in that, so a run counts as one).
        static string Lossy(string s) => System.Text.RegularExpressions.Regex.Replace(s, "�+", "�");
        string[] messages = [.. root.GetProperty("MSGI").EnumerateArray().Select(v => Lossy(v.GetString()!))];
        Assert.Equal(126306, ain.Messages.Count);
        Assert.Equal(messages.Length, ain.Messages.Count);
        var differing = Enumerable.Range(0, messages.Length).Where(i => Lossy(Encoding.UTF8.GetString(ain.Messages[i])) != messages[i]).ToList();
        int plain = ain.Messages.Count(m => m.All(b => b < 0x80));
        output.WriteLine($"{ain.Messages.Count} messages ({plain} ASCII), version {ain.Version}, {differing.Count} differ");
        foreach (int i in differing.Take(5))
            output.WriteLine($"  {i}: '{Encoding.UTF8.GetString(ain.Messages[i])}' vs '{messages[i]}'");
        Assert.Empty(differing);
        Assert.Equal("０１２３４５６７８９", Encoding.GetEncoding(932).GetString(ain.Messages[0]));
    }

    /// <summary>
    /// The sound library over the archive gives every sound number the library over <c>wav\</c> gives, with
    /// the same bytes, and nothing where the table links nothing (docs/import.md, step 3).
    /// </summary>
    [LocalFolderFact("game", "wav")]
    public void Every_sound_from_the_archive_equals_its_wav()
    {
        var archive = AldArchive.Open(LocalData.GameDirectory, "KICHIKUW");
        var fromArchive = new SoundLibrary(archive);
        var fromWav = new SoundLibrary(LocalData.WavDirectory, SoundTable.FileName);

        int count = 0;
        for (int n = -1; n <= archive.Count + 1; n++)
        {
            byte[]? wav = fromWav.Get(n);
            byte[]? ald = fromArchive.Get(n);
            Assert.Equal(SoundTable.FileName(n) is not null, wav is not null);
            if (wav is null)
            {
                Assert.Null(ald);
                continue;
            }
            Assert.NotNull(ald);
            Assert.True(wav.AsSpan().SequenceEqual(ald), $"sound {n}");
            count++;
        }
        Assert.Equal(51, count);
    }
}
