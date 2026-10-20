using System.Text;
using Kichikuou.Content.Text;

namespace Kichikuou.Content.Tests;

/// <summary>The Shift JIS decoder after xsys35c 1.13 <c>common/sjisutf.c</c> and its table.</summary>
public class SjisTests
{
    static SjisTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [Fact]
    public void Decodes_ascii_half_width_and_pairs()
    {
        Assert.Equal("Rance ｽﾀｰﾄ０一", Sjis.Decode([.. "Rance "u8, 0xbd, 0xc0, 0xb0, 0xc4, 0x82, 0x4f, 0x88, 0xea]));
        Assert.Equal("｠", Sjis.Decode([0xa0]));
        Assert.Equal("", Sjis.Decode([]));
    }

    [Theory]
    [InlineData(new byte[] { 0x80 })]
    [InlineData(new byte[] { 0x82 })]
    [InlineData(new byte[] { 0x82, 0x7f })]
    [InlineData(new byte[] { 0xfd, 0x40 })]
    [InlineData(new byte[] { 0x85, 0x40 })] // a lead and trail byte the table maps to nothing
    public void Refuses_what_the_table_does_not_map(byte[] sjis) =>
        Assert.Throws<SjisFormatException>(() => Sjis.Decode(sjis));

    [Fact]
    public void Expands_the_compacted_bytes()
    {
        Assert.Equal(0x8140, Sjis.Expand((byte)' '));
        Assert.Equal(0x8142, Sjis.Expand(0xa1)); // 。
        Assert.Equal(0x82f1, Sjis.Expand(0xdd)); // ん
        Assert.Equal(0, Sjis.Expand(0xde));
        Assert.Equal(0, Sjis.Expand((byte)'A'));
    }

    [Fact]
    public void A_pair_is_unicode_safe_unless_private_or_ambiguous()
    {
        Assert.True(Sjis.IsUnicodeSafe(0x82, 0x4f));
        Assert.False(Sjis.IsUnicodeSafe(0x87, 0x54)); // Ⅰ, which 0xfa4a maps to as well
        Assert.False(Sjis.IsUnicodeSafe(0xf0, 0x40)); // a gaiji (U+E000)
        Assert.False(Sjis.IsUnicodeSafe(0x85, 0x40)); // nothing
        Assert.False(Sjis.IsUnicodeSafe(0x41, 0x41));
    }

    /// <summary>
    /// xsys35c's table against .NET's code page 932 (measured 2026-10-20): wherever both map a pair they agree,
    /// and the table maps 587 pairs more that .NET reads as "?" — ゔ (0x82f2), a few NEC symbols in row 0x87,
    /// the user-defined rows 0xeb-0xee and 0xfa — which is why the import does not use the platform's decoder.
    /// </summary>
    [Fact]
    public void The_table_agrees_with_code_page_932_and_maps_more()
    {
        var cp932 = Encoding.GetEncoding(932, EncoderFallback.ExceptionFallback, new DecoderReplacementFallback("�"));
        var conflicts = new List<string>();
        int onlyOurs = 0;
        for (int lead = 0x81; lead <= 0xfc; lead++)
        {
            for (int trail = 0x40; trail <= 0xfc; trail++)
            {
                if (!Sjis.IsValid((byte)lead, (byte)trail))
                    continue;
                string ours = Sjis.Decode([(byte)lead, (byte)trail]);
                string theirs = cp932.GetString([(byte)lead, (byte)trail]);
                if (theirs.Contains('�'))
                    onlyOurs++;
                else if (ours != theirs)
                    conflicts.Add($"{lead:X2}{trail:X2}: U+{(int)ours[0]:X4} vs U+{(int)theirs[0]:X4}");
            }
        }
        Assert.Empty(conflicts);
        Assert.Equal(587, onlyOurs);
    }
}
