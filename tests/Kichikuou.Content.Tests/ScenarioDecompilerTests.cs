using System.Buffers.Binary;
using System.Text;
using Kichikuou.Content.Decompiler;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The decompiler on pages written here (docs/import.md, step 6; the whole game is compared with xsys35dc's own
/// decompile in the content tests, <c>DecompilerTests</c>).
/// </summary>
public class ScenarioDecompilerTests
{
    private static readonly byte[][] Variables = [.. Enumerable.Range(0, 3).Select(i => Encoding.ASCII.GetBytes($"VAR{i:D4}"))];

    /// <summary>An <c>S380</c> page: a 0x20-byte header naming it, then the code.</summary>
    private static ScenarioPage Page(string name, params byte[][] code)
    {
        byte[] body = [.. code.SelectMany(c => c)];
        var page = new byte[0x20 + body.Length];
        "S380"u8.CopyTo(page);
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(4), 0x20);
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(8), page.Length);
        BinaryPrimitives.WriteInt16LittleEndian(page.AsSpan(16), (short)name.Length);
        Encoding.ASCII.GetBytes(name).CopyTo(page, 18);
        body.CopyTo(page, 0x20);
        return new ScenarioPage(page, Encoding.ASCII.GetBytes(Path.ChangeExtension(name, ".SCO")));
    }

    private static byte[] Le32(int n) => BitConverter.GetBytes(n);

    private static string Decompile(ScenarioPage page, params string[] messages)
    {
        var files = ScenarioDecompiler.Decompile([page], [.. messages.Select(Encoding.ASCII.GetBytes)], Variables);
        Assert.EndsWith(".ADV", files[0].Name); // the header's name, not the archive's (.SCO)
        return Encoding.UTF8.GetString(files.Single().Text);
    }

    [Fact]
    public void Writes_expressions_conditions_and_texts()
    {
        var page = Page("A.ADV",
            // !VAR0001 : (VAR0002 + 3) * 4!
            [(byte)'!', 0x81, 0x82, 0x43, 0x79, 0x44, 0x77, 0x7f],
            // {VAR0001 = 1: … } around a text of the .ain and R
            [(byte)'{', 0x81, 0x41, 0x7b, 0x7f, .. Le32(0x38)],
            [0x2f, 0x7c, .. Le32(0)],
            [(byte)'R'],
            // 16383 + 100 is folded into one number
            [(byte)'!', 0x81, 0x3f, 0xff, 0x00, 0x64, 0x79, 0x7f],
            // a return: % 0
            [(byte)'%', 0x40, 0x7f]);

        Assert.Equal(
            "\t!VAR0001 : (VAR0002 + 3) * 4!\r\n" +
            "\t{VAR0001 = 1:\r\n" +
            "\t\t'He said \\\"hi\\\" \\<ok>'\r\n" +
            "\t\tR\r\n" +
            "\t}\r\n" +
            "\t!VAR0001 : 16483!\r\n" +
            "\t%0:\r\n",
            Decompile(page, "He said \"hi\" <ok>"));
    }

    [Fact]
    public void Finds_a_data_table_and_its_data_in_a_later_pass()
    {
        var page = Page("B.ADV",
            [(byte)'#', .. Le32(0x2c), 0x40, 0x7f], // 0x20: #L_0002c, 0:
            [(byte)'@', .. Le32(0x38)], //            0x27: @L_00038:
            Le32(0x30), //                            0x2c: the table
            [(byte)'a', (byte)'b', (byte)'c', 0, 2, 1, 4, 3], // 0x30: the data
            [(byte)'R']); //                          0x38

        Assert.Equal(
            "\t#L_0002c, 0:\r\n" +
            "\t@L_00038:\r\n" +
            "*L_0002c:\r\n" +
            "\t_L_00030:\r\n" +
            "*L_00030:\r\n" +
            "\t\"abc\"\r\n" +
            "\t[258, 772]\r\n" +
            "*L_00038:\r\n" +
            "\tR\r\n",
            Decompile(page));
    }

    [Fact]
    public void A_command_it_does_not_know_is_read_as_data()
    {
        var page = Page("C.ADV", [(byte)'C', (byte)'Q', 0, 0]);
        Assert.Equal("\t\"CQ\"\r\n\t\"\"\r\n", Decompile(page));
    }

    [Theory]
    [InlineData(new byte[] { (byte)'~', 1, 0, 0, 0, 0, 0 }, "C.SCO:20: function calls are not supported")]
    [InlineData(new byte[] { 0x2f, 0xff }, "C.SCO:20: Unsupported command 2f ff")]
    [InlineData(new byte[] { 0x2f, 0x7c, 5, 0, 0, 0 }, "C.SCO:20: invalid message id 5")]
    public void What_it_does_not_read_fails_with_the_file_and_the_address(byte[] code, string message)
    {
        var e = Assert.Throws<DecompileException>(() => Decompile(Page("C.ADV", code)));
        Assert.Equal(message, e.Message);
    }

    [Fact]
    public void Only_s380_pages_are_read()
    {
        var page = Page("D.ADV", [(byte)'R']);
        "S360"u8.CopyTo(page.Bytes);
        Assert.Throws<DecompileException>(() => Decompile(page));
    }
}
