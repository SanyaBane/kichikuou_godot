using System.Buffers.Binary;
using System.Text;
using Kichikuou.Content;
using Kichikuou.Content.Archives;
using Kichikuou.Content.Saves;
using Kichikuou.Content.Scripts;
using Kichikuou.Content.Text;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Saves;
using Kichikuou.Original.Script;
using Kichikuou.Original.Setup;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>A fact over the owner's saves of the original in the game folder (<c>KICHIKUSA.asd</c> …); skipped without them.</summary>
public sealed class OriginalSavesFactAttribute : FactAttribute
{
    public OriginalSavesFactAttribute()
    {
        if (OriginalSaveTests.OwnersSaves().Count == 0)
            Skip = $"No KICHIKUS?.asd in {LocalData.GameDirectory}.";
    }
}

/// <summary>An original save as a slot of the port (docs/save-import.md, §2-§3, §7; <see cref="OriginalSave"/>).</summary>
public sealed class OriginalSaveTests(ITestOutputHelper output)
{
    /// <summary>The owner's saves in the game folder, slot A first.</summary>
    internal static IReadOnlyList<string> OwnersSaves() => Directory.Exists(LocalData.GameDirectory)
        ? [.. Directory.EnumerateFiles(LocalData.GameDirectory, "KICHIKUS?.asd").Order(StringComparer.OrdinalIgnoreCase)]
        : [];

    /// <summary>A save file in the version's layout, made at a week's start (§2), in xsystem35's <c>save_saveAll</c> order.</summary>
    private sealed class Asd
    {
        public int ResumePage { get; set; } = OriginalSave.SaveScreenPage;
        public int ResumeAddress { get; set; } = OriginalSave.ResumeAddress;
        public int NearReturn { get; set; } = 0x1f0;
        public int FarPage { get; set; } = OriginalSave.MainPage;
        public byte[]? Stack { get; set; }
        public Dictionary<int, ushort[]> Pages { get; } = new()
        {
            [0] = new ushort[65537],
            [1] = new ushort[20001],
            [2] = new ushort[1801],
            [4] = new ushort[2101],
            [10] = new ushort[301],
        };
        public List<string> Strings { get; } = [.. Enumerable.Repeat("", 5001)];

        public AsdSave Read() => AsdSave.Read(Write());

        public byte[] Write()
        {
            var header = new byte[AsdSave.HeaderSize];
            Encoding.ASCII.GetBytes(AsdSave.Signature).CopyTo(header, 0);
            Put(header, 32, AsdSave.DataVersion);
            Put(header, 80, ResumePage + 1);
            Put(header, 84, ResumeAddress);
            var body = new MemoryStream();
            int at = AsdSave.HeaderSize;
            const int stackOffset = 96 + 8 * 256;
            byte[] stack = Stack ?? [.. Le(NearReturn), 0xee, (byte)(FarPage + 1), 0, .. Le(OriginalSave.FarCallReturn), 0xff];
            Put(header, stackOffset, at);
            at += Part(body, stack.Length, 0, stack);
            Put(header, stackOffset + 4, at);
            byte[] strings = [.. Strings.SelectMany(s => Encoding.ASCII.GetBytes(s).Append((byte)0))];
            at += Part(body, strings.Length, Strings.Count, strings);
            foreach (var (page, values) in Pages.OrderBy(p => p.Key))
            {
                Put(header, stackOffset + 16 + 4 * page, at);
                at += Part(body, values.Length * 2, page, [.. values.SelectMany(v => new[] { (byte)v, (byte)(v >> 8) })]);
            }
            return [.. header, .. body.ToArray()];
        }

        private static byte[] Le(int value) => BitConverter.GetBytes(value);

        private static int Part(MemoryStream body, int size, int second, byte[] data)
        {
            var head = new byte[16];
            Put(head, 0, size);
            Put(head, 4, second);
            body.Write(head);
            body.Write(data);
            return 16 + data.Length;
        }

        private static void Put(byte[] buffer, int at, int value) => BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(at), value);
    }

    [Fact]
    public void A_Leazas_weeks_autosave_becomes_a_slot_resuming_after_MAIN_83_with_the_mark()
    {
        var asd = new Asd();
        asd.Pages[0][517] = 18;                                         // Week
        asd.Pages[0][805] = 7;
        asd.Pages[1][20000] = 3;
        asd.Pages[4][79] = 1;
        asd.Strings[1] = "Rance";
        asd.Strings[5000] = "last";
        var written = new DateTime(2003, 4, 5, 6, 7, 8);

        var slot = OriginalSave.FromAsd(asd.Read(), written);

        Assert.Equal(new ScriptRef("MAIN.ADV", 83), slot.Resume);
        Assert.Equal(806, slot.Variables.Length);
        Assert.Equal(18, slot.Variables[517].Value);
        Assert.Equal(7, slot.Variables[805].Value);
        Assert.Equal([1, 2, 4, 10], slot.Pages.Keys.Order());
        Assert.Equal(3, slot.Pages[1][20000].Value);
        Assert.Equal(1, slot.Pages[4][79].Value);
        Assert.Equal(5001, slot.Strings.Length);
        Assert.Equal("Rance", slot.Strings[1]);
        Assert.Equal("last", slot.Strings[5000]);
        Assert.Equal(written, slot.Written);
        Assert.Equal("", slot.Comment);
        Assert.Equal([GameState.ImportedFromOriginalKey], slot.Extras.Keys);

        var state = new GameState();
        slot.Restore(state);
        Assert.True(state.ImportedFromOriginal);
        Assert.Equal(18, state.Week.Value);
    }

    [Fact]
    public void A_thieves_weeks_autosave_resumes_after_MAIN_20() =>
        Assert.Equal(new ScriptRef("MAIN.ADV", 20), OriginalSave.FromAsd(new Asd { NearReturn = 0xa1 }.Read(), null).Resume);

    [Fact]
    public void A_save_made_elsewhere_or_of_another_version_is_refused_in_words()
    {
        Assert.Contains("goes on at page 12, address 0x50", Refused(new Asd { ResumePage = 12, ResumeAddress = 0x50 }));
        Assert.Contains("not made at a week's start (a call returning to 0x1234, a call returning to page 12 at 0x187c)",
            Refused(new Asd { NearReturn = 0x1234 }));
        Assert.Contains("a call returning to page 13", Refused(new Asd { FarPage = 13 }));
        Assert.Contains("not made at a week's start (no calls)", Refused(new Asd { Stack = [] }));

        var past = new Asd();
        past.Pages[0][806] = 1;
        Assert.Contains("it uses variable 806, past the 806", Refused(past));

        var pages = new Asd();
        pages.Pages.Remove(10);
        pages.Pages[3] = new ushort[5];
        Assert.Contains("it holds the pages 1, 2, 3, 4, this version's saves 1, 2, 4, 10", Refused(pages));

        var size = new Asd();
        size.Pages[2] = new ushort[1802];
        Assert.Contains("its page 2 holds 1802 values, this version's 1801", Refused(size));

        var strings = new Asd();
        strings.Strings.Add("one too many");
        Assert.Contains("it uses string 5001", Refused(strings));
        Assert.Contains(GameFiles.EnglishTranslation10.Name, Refused(strings));
    }

    [Fact]
    public void A_file_that_is_no_save_is_refused_with_the_readers_words()
    {
        string path = Path.Combine(Directory.CreateTempSubdirectory("original-save-").FullName, "KICHIKUSA.asd");
        try
        {
            File.WriteAllBytes(path, Encoding.ASCII.GetBytes("Hello, this is not a save at all, not at all."));
            Assert.Contains("not a save file of the original game", Assert.Throws<OriginalSaveException>(() => OriginalSave.Read(path)).Message);
            Assert.Contains("cannot be read", Assert.Throws<OriginalSaveException>(() => OriginalSave.Read(path + ".missing")).Message);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    private static string Refused(Asd asd) => Assert.Throws<OriginalSaveException>(() => OriginalSave.FromAsd(asd.Read(), null)).Message;

    /// <summary>
    /// The owner's ten (slots A-J, weeks 15-22): each a Leazas week's autosave (§4), its strings and pages whole, six
    /// written by xsystem35 and four without its footer (likely <c>System39.exe</c>'s).
    /// </summary>
    [OriginalSavesFact]
    public void The_owners_saves_are_Leazas_weeks_autosaves()
    {
        int xsystem35 = 0;
        foreach (string path in OwnersSaves())
        {
            var asd = AsdSave.Read(path);
            xsystem35 += asd.WrittenByXSystem35 ? 1 : 0;
            Assert.Equal(65537, asd.Pages[0].Length);
            Assert.True(asd.Strings.Count <= 5001, $"{asd.Strings.Count} strings");

            var slot = OriginalSave.Read(path);
            var state = new GameState();
            slot.Restore(state);
            output.WriteLine($"{Path.GetFileName(path)}: {slot.Resume}, week {state.Week}, stage {state.StoryStage}, "
                + $"{asd.Strings.Count(s => s.Length > 0)} strings, xsystem35 {asd.WrittenByXSystem35}, {slot.Written}");
            Assert.Equal(new ScriptRef("MAIN.ADV", 83), slot.Resume);
            Assert.InRange(state.Week.Value, 15, 22);
            Assert.True(state.ImportedFromOriginal);
            Assert.Equal(File.GetLastWriteTime(path), slot.Written);
            Assert.Contains(state.Strings, s => s.Length > 0);
        }
        if (OwnersSaves().Count == 10)
            Assert.Equal(6, xsystem35);
    }

    /// <summary>
    /// The addresses <see cref="OriginalSave"/> knows are the scenario archive's (§2): page 12 is <c>MAIN.ADV</c> and
    /// page 33 <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c>; <c>QD 1</c> ends at the resume address, both <c>\L_01875:</c> calls at the near returns, and
    /// <c>%#ﾛｰﾄﾞｾｰﾌﾞ.ADV:</c> (page 33's call) at the far return.
    /// </summary>
    [LocalFolderFact]
    public void The_known_addresses_are_the_scenario_archives()
    {
        var archive = AldArchive.Open(LocalData.GameDirectory, "KICHIKUS");
        byte[] main = archive[OriginalSave.MainPage]!.ReadAllBytes(), screen = archive[OriginalSave.SaveScreenPage]!.ReadAllBytes();
        Assert.Equal("MAIN.ADV", SourceName(main));
        Assert.Equal("ﾛｰﾄﾞｾｰﾌﾞ.ADV", SourceName(screen));

        Assert.Equal("QD"u8.ToArray().Concat(new byte[] { 0x41, 0x7f }), screen[(OriginalSave.ResumeAddress - 4)..OriginalSave.ResumeAddress]);
        foreach (int back in OriginalSave.Autosaves.Keys)
            Assert.Equal(new byte[] { (byte)'\\', 0x75, 0x18, 0, 0 }, main[(back - 5)..back]);
        Assert.Equal(new byte[] { (byte)'%', 0x40 + OriginalSave.SaveScreenPage, 0x7f },
            main[(OriginalSave.FarCallReturn - 3)..OriginalSave.FarCallReturn]);
    }

    /// <summary>The pages' sizes <see cref="OriginalSave"/> checks are those the port's boot allocates (<c>SET.ADV</c>).</summary>
    [ScenarioFact]
    public async Task The_known_page_sizes_are_the_boots()
    {
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var stubs = new RecordingRules { State = state };
        var rules = new StrategyRules(scripts, stubs);
        var runner = new SceneRunner(scripts, rules);
        var script = new OriginalScriptContext(state, new QueuedRandom(), new RecordingView(), new EndInput(state), runner)
        {
            Saves = new MemorySaveStore(),
        };
        runner.Bind(script);
        rules.Bind(script);
        await new GameSetup(script, scripts).BootAsync();

        Assert.Equal(OriginalSave.PageSizes.Keys.Order(), state.Profile.SavedPages.Order());
        foreach (var (page, size) in OriginalSave.PageSizes)
            Assert.Equal(size, state.Memory.PageSize(page));
    }

    private static string SourceName(byte[] sco)
    {
        int length = sco[16] | sco[17] << 8;
        var name = sco.AsSpan(18, length);
        int nul = name.IndexOf((byte)0);
        return Sjis.Decode(nul >= 0 ? name[..nul] : name);
    }
}
