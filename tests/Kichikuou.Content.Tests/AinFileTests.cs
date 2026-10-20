using System.Text;
using Kichikuou.Content.Archives;

namespace Kichikuou.Content.Tests;

/// <summary>The <c>.ain</c> reader on files written here as <c>ain_read</c> reads them (xsys35c 1.13 <c>decompiler/ain.c</c>).</summary>
public class AinFileTests
{
    [Fact]
    public void Reads_the_variables_and_the_messages()
    {
        byte[] file = Ain("AINI", 1, Section("HEL0", 0), Section("FUNC", 0),
            Section("VARI", 2, "VAR0000", "VAR0001"), Section("MSGI", 3, "Rance", "", "\x82\x4F"));

        var ain = AinFile.Decode(file);

        Assert.Equal(1, ain.Version);
        Assert.Equal(["VAR0000", "VAR0001"], ain.Variables.Select(v => Encoding.Latin1.GetString(v)));
        Assert.Equal(["Rance", "", "\x82\x4F"], ain.Messages.Select(m => Encoding.Latin1.GetString(m)));
    }

    [Fact]
    public void Leaves_the_files_bytes_alone()
    {
        byte[] file = Ain("AIN2", 2, Section("MSGI", 1, "x"));
        byte[] copy = [.. file];

        Assert.Equal(2, AinFile.Decode(file).Version);
        Assert.Equal(copy, file);
    }

    [Fact]
    public void A_missing_section_is_empty()
    {
        var ain = AinFile.Decode(Ain("AINI", 1, Section("MSGI", 1, "x")));

        Assert.Empty(ain.Variables);
        Assert.Single(ain.Messages);
    }

    [Theory]
    [InlineData("not an AIN file")]
    [InlineData("unknown AIN version 3")]
    [InlineData("FUNC has 1 entries")]
    [InlineData("unknown ain section 'ABCD'")]
    [InlineData("MSGI entry 1 has no end")]
    public void Refuses_what_it_does_not_know(string message)
    {
        byte[] file = message switch
        {
            "not an AIN file" => Encoding.ASCII.GetBytes("AIN3\x01\x00\x00\x00"),
            "unknown AIN version 3" => Ain("AINI", 3),
            "FUNC has 1 entries" => Ain("AINI", 1, Section("FUNC", 1, "F")),
            "unknown ain section 'ABCD'" => Ain("AINI", 1, Section("ABCD", 0)),
            _ => Ain("AINI", 1, Section("MSGI", 2, "a")),
        };

        Assert.Contains(message, Assert.Throws<AinFormatException>(() => AinFile.Decode(file)).Message);
    }

    /// <summary>A section's plain bytes: its tag, 4 bytes, the count and the NUL-terminated strings (Latin-1 for raw bytes).</summary>
    private static byte[] Section(string tag, int count, params string[] strings) =>
        [.. Encoding.ASCII.GetBytes(tag), 0, 0, 0, 0, .. BitConverter.GetBytes(count),
            .. strings.SelectMany(s => (byte[])[.. Encoding.Latin1.GetBytes(s), 0])];

    /// <summary>A whole file: the magic, the version as stored, the sections rotated right by 2 bits.</summary>
    private static byte[] Ain(string magic, int version, params byte[][] sections)
    {
        byte[] body = [.. sections.SelectMany(s => s)];
        // The reader rotates the version's bytes too and skips them, so they are stored plain.
        return [.. Encoding.ASCII.GetBytes(magic), .. BitConverter.GetBytes(version), .. body.Select(b => (byte)(b >> 2 | b << 6))];
    }
}
