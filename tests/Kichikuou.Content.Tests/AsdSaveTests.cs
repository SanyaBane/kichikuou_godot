using System.Buffers.Binary;
using System.Text;
using Kichikuou.Content.Saves;

namespace Kichikuou.Content.Tests;

/// <summary>The original's save reader (docs/save-import.md, §1) on files written here in the layout of xsystem35's <c>save_saveAll</c>.</summary>
public sealed class AsdSaveTests
{
    /// <summary>A save file as xsystem35 writes one: the header, the stack, the strings, the pages, the footer.</summary>
    private sealed class AsdWriter
    {
        public string Signature { get; set; } = AsdSave.Signature;
        public int Version { get; set; } = AsdSave.DataVersion;
        public int ScoPage { get; set; } = 34;
        public int ScoIndex { get; set; } = 0xd0;
        public byte[] Stack { get; set; } = [];
        public List<byte[]> Strings { get; } = [];
        public SortedDictionary<int, ushort[]> Pages { get; } = new() { [0] = [0, 1, 2] };
        public bool Footer { get; set; } = true;
        public Func<int, int>? PageNumber { get; set; }

        public byte[] Write()
        {
            var body = new MemoryStream();
            var header = new byte[AsdSave.HeaderSize];
            Encoding.ASCII.GetBytes(Signature).CopyTo(header, 0);
            Put(header, 32, Version);
            Put(header, 80, ScoPage);
            Put(header, 84, ScoIndex);
            const int stackOffset = 96 + 8 * 256;

            int at = AsdSave.HeaderSize;
            Put(header, stackOffset, at);
            body.Write(Part(Stack.Length, 0));
            body.Write(Stack);
            at += 16 + Stack.Length;

            Put(header, stackOffset + 4, at);
            byte[] strings = [.. Strings.SelectMany(s => s.Append((byte)0))];
            body.Write(Part(strings.Length, Strings.Count));
            body.Write(strings);
            at += 16 + strings.Length;

            foreach (var (page, values) in Pages)
            {
                Put(header, stackOffset + 16 + 4 * page, at);
                body.Write(Part(values.Length * 2, PageNumber?.Invoke(page) ?? page));
                foreach (ushort v in values)
                    body.Write([(byte)v, (byte)(v >> 8)]);
                at += 16 + values.Length * 2;
            }
            if (Footer)
            {
                body.Write(Enumerable.Repeat((byte)0xff, 256).ToArray());
                body.Write("XS35"u8);
                body.Write(new byte[12]);
                body.Write("INFS"u8);
            }
            return [.. header, .. body.ToArray()];
        }

        private static byte[] Part(int size, int second)
        {
            var part = new byte[16];
            Put(part, 0, size);
            Put(part, 4, second);
            return part;
        }

        private static void Put(byte[] buffer, int at, int value) => BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(at), value);
    }

    /// <summary>A near call returning to 0xa1, then a far call returning to page 12 at 0x187c: an autosave's stack.</summary>
    private static readonly byte[] AutosaveStack = [0xa1, 0, 0, 0, 0xee, 13, 0, 0x7c, 0x18, 0, 0, 0xff];

    [Fact]
    public void Reads_the_resume_point_the_stack_the_strings_and_the_pages()
    {
        var writer = new AsdWriter { Stack = AutosaveStack };
        writer.Strings.AddRange([[], "Rance"u8.ToArray(), [0x83, 0x89, 0x83, 0x93, 0x83, 0x58], []]);   // ランス in Shift JIS
        writer.Pages[1] = [7, 8];
        writer.Pages[4] = [65535];

        var save = AsdSave.Read(writer.Write());

        Assert.Equal(33, save.ResumePage);
        Assert.Equal(0xd0, save.ResumeAddress);
        Assert.Equal<AsdFrame>([new AsdNearCall(0xa1), new AsdFarCall(12, 0x187c)], save.Stack);
        Assert.Equal(["", "Rance", "ランス", ""], save.Strings);
        Assert.Equal([0, 1, 4], save.Pages.Keys.Order());
        Assert.Equal([0, 1, 2], save.Pages[0]);
        Assert.Equal([7, 8], save.Pages[1]);
        Assert.Equal([65535], save.Pages[4]);
        Assert.True(save.WrittenByXSystem35);
    }

    [Fact]
    public void A_file_without_the_footer_is_System39s()
    {
        var save = AsdSave.Read(new AsdWriter { Footer = false }.Write());
        Assert.False(save.WrittenByXSystem35);
    }

    [Fact]
    public void Every_kind_of_stack_frame_is_read_bottom_first()
    {
        byte[] stack =
        [
            0x10, 0, 0, 0, 0xee,                                // near call returning to 0x10
            5, 0, 4, 0, 9, 0, 0xdd,                             // page 4 [5] pushed with 9
            1, 2, 0xcc,                                         // the text's colour
            1, 0, 0, 0, 0, 0xbb,                                // the text's size
            0, 0, 0, 0, 0, 0, 0, 0, 0xaa,                       // the text's place
            3, 0, 0x20, 0, 0, 0, 0xff,                          // far call returning to page 2 at 0x20
        ];
        var save = AsdSave.Read(new AsdWriter { Stack = stack }.Write());
        Assert.Equal<AsdFrame>(
            [new AsdNearCall(0x10), new AsdVariableFrame(4, 5, 9), new AsdTextFrame(0xcc), new AsdTextFrame(0xbb),
             new AsdTextFrame(0xaa), new AsdFarCall(2, 0x20)],
            save.Stack);
    }

    [Theory]
    [InlineData("System3.6 SaveData(c)ALICE-SOFT", "not a save file of the original game")]
    [InlineData("System3.5 SavaData(c)ALICE-SOFT", "older engine")]
    [InlineData("", "not a save file of the original game")]
    public void Another_signature_is_refused_in_words(string signature, string reason)
    {
        var e = Assert.Throws<AsdFormatException>(() => AsdSave.Read(new AsdWriter { Signature = signature }.Write()));
        Assert.Contains(reason, e.Message);
    }

    [Fact]
    public void A_damaged_file_is_refused_in_words()
    {
        Assert.Contains("too short", Assert.Throws<AsdFormatException>(() => AsdSave.Read(new byte[10])).Message);
        byte[] whole = new AsdWriter().Write();
        Assert.Contains("header is not whole", Assert.Throws<AsdFormatException>(() => AsdSave.Read(whole.AsSpan(0, 2000))).Message);
        Assert.Contains("unknown version", Assert.Throws<AsdFormatException>(() => AsdSave.Read(new AsdWriter { Version = 1 }.Write())).Message);
        Assert.Contains("cut short", Assert.Throws<AsdFormatException>(() =>
            AsdSave.Read(new AsdWriter { Footer = false }.Write().AsSpan()[..^2])).Message);
        Assert.Contains("unknown frame", Assert.Throws<AsdFormatException>(() =>
            AsdSave.Read(new AsdWriter { Stack = [1, 2, 3] }.Write())).Message);
        Assert.Contains("says it is page", Assert.Throws<AsdFormatException>(() =>
            AsdSave.Read(new AsdWriter { PageNumber = p => p + 1 }.Write())).Message);
    }

    [Fact]
    public void A_save_without_page_0_is_refused()
    {
        var writer = new AsdWriter();
        writer.Pages.Remove(0);
        writer.Pages[1] = [1];
        Assert.Contains("page 0", Assert.Throws<AsdFormatException>(() => AsdSave.Read(writer.Write())).Message);
    }
}
