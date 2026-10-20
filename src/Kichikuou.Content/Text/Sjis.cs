// Parts translated from xsys35c 1.13.0 (https://github.com/kichikuou/xsys35c), common/sjisutf.c (sjis2utf_sub,
// is_valid_sjis, is_unicode_safe and its ambigious_unicodes, expand_sjis and its kanatbl) and common/common.h
// (the byte classes): Copyright (C) 2020 <KichikuouChrome@gmail.com>. GNU General Public License, version 2 or
// later, as KichikuouGodot (LICENSE).

using System.Text;

namespace Kichikuou.Content.Text;

/// <summary>A byte sequence that is no Shift JIS xsys35c's table knows.</summary>
public sealed class SjisFormatException(string message) : Exception(message);

/// <summary>
/// The game's Shift JIS, decoded as xsys35c 1.13 decodes it (docs/import.md, step 5): with xsys35c's own table
/// (<c>common/s2utbl.h</c>, generated into <c>SjisTable.g.cs</c> by <c>tools/codegen/gen_sjis_table.py</c>),
/// not a platform's code page 932, so the import writes the text xsys35dc writes on any platform. Also the
/// byte classes and the checks the decompiler uses: the half-width forms "compacted" from full-width ones,
/// and the pairs it must write as <c>&lt;0xXXXX&gt;</c> instead of their character.
/// </summary>
public static partial class Sjis
{
    /// <summary>A lead byte of a two-byte character (<c>is_sjis_byte1</c>).</summary>
    public static bool IsLeadByte(byte c) => c is (>= 0x81 and <= 0x9f) or (>= 0xe0 and <= 0xfc);

    /// <summary>A trail byte of a two-byte character (<c>is_sjis_byte2</c>).</summary>
    public static bool IsTrailByte(byte c) => c is >= 0x40 and <= 0xfc and not 0x7f;

    /// <summary>A half-width katakana or punctuation byte (<c>is_sjis_half_kana</c>).</summary>
    public static bool IsHalfKana(byte c) => c is >= 0xa1 and <= 0xdf;

    /// <summary>A byte the System 3 compiler made of a full-width character (<c>is_compacted_sjis</c>): the space and 0xa1-0xdd.</summary>
    public static bool IsCompacted(byte c) => c == ' ' || c is >= 0xa1 and <= 0xdd;

    /// <summary>The full-width pair a compacted byte stands for (<c>expand_sjis</c>); 0 for any other byte.</summary>
    public static ushort Expand(byte c) =>
        !IsCompacted(c) ? (ushort)0 : c == ' ' ? (ushort)0x8140 : KanaTable[c - 0xa0];

    /// <summary>Whether the table maps the pair to a character (<c>is_valid_sjis</c>).</summary>
    public static bool IsValid(byte lead, byte trail) => IsLeadByte(lead) && IsTrailByte(trail) && CodePoint(lead, trail) != 0;

    /// <summary>
    /// Whether the pair can be written as its character and read back as the same pair (<c>is_unicode_safe</c>):
    /// valid, no private-use character (a gaiji, U+E000-U+EFFF), and no character that several pairs map to.
    /// </summary>
    public static bool IsUnicodeSafe(byte lead, byte trail)
    {
        if (!IsLeadByte(lead) || !IsTrailByte(trail))
            return false;
        int cp = CodePoint(lead, trail);
        return cp != 0 && (cp & 0xf000) != 0xe000 && AmbiguousUnicodes.BinarySearch((ushort)cp) < 0;
    }

    /// <summary>
    /// Decodes Shift JIS as <c>sjis2utf</c> does: ASCII as is, 0xa0-0xdf as U+FF60-U+FF9F, a valid pair through
    /// the table; anything else is an error (no NUL is expected: the caller passes the bytes before it).
    /// </summary>
    public static string Decode(ReadOnlySpan<byte> sjis)
    {
        var text = new StringBuilder(sjis.Length);
        for (int i = 0; i < sjis.Length;)
        {
            byte c = sjis[i];
            if (c <= 0x7f)
            {
                text.Append((char)c);
                i++;
            }
            else if (c is >= 0xa0 and <= 0xdf)
            {
                text.Append((char)(0xff60 + c - 0xa0));
                i++;
            }
            else if (i + 1 < sjis.Length && IsValid(c, sjis[i + 1]))
            {
                text.Append((char)CodePoint(c, sjis[i + 1]));
                i += 2;
            }
            else
            {
                throw new SjisFormatException(i + 1 < sjis.Length
                    ? $"Invalid SJIS byte sequence {c:x2} {sjis[i + 1]:x2} at {i}."
                    : $"Invalid SJIS byte {c:x2} at the end.");
            }
        }
        return text.ToString();
    }

    private static int CodePoint(byte lead, byte trail) => Table[(lead - 0x80) * 192 + trail - 0x40];

    /// <summary><c>kanatbl</c>: the full-width pairs of the compacted bytes 0xa0-0xdf.</summary>
    private static ReadOnlySpan<ushort> KanaTable =>
    [
        0x8140, 0x8142, 0x8175, 0x8176, 0x8141, 0x8145, 0x82f0, 0x829f,
        0x82a1, 0x82a3, 0x82a5, 0x82a7, 0x82e1, 0x82e3, 0x82e5, 0x82c1,
        0x815b, 0x82a0, 0x82a2, 0x82a4, 0x82a6, 0x82a8, 0x82a9, 0x82ab,
        0x82ad, 0x82af, 0x82b1, 0x82b3, 0x82b5, 0x82b7, 0x82b9, 0x82bb,
        0x82bd, 0x82bf, 0x82c2, 0x82c4, 0x82c6, 0x82c8, 0x82c9, 0x82ca,
        0x82cb, 0x82cc, 0x82cd, 0x82d0, 0x82d3, 0x82d6, 0x82d9, 0x82dc,
        0x82dd, 0x82de, 0x82df, 0x82e0, 0x82e2, 0x82e4, 0x82e6, 0x82e7,
        0x82e8, 0x82e9, 0x82ea, 0x82eb, 0x82ed, 0x82f1, 0x814a, 0x814b,
    ];

    /// <summary><c>ambigious_unicodes</c>: the code points several pairs map to, sorted.</summary>
    private static ReadOnlySpan<ushort> AmbiguousUnicodes =>
    [
        0x2116, 0x2121, 0x2160, 0x2161, 0x2162, 0x2163, 0x2164, 0x2165,
        0x2166, 0x2167, 0x2168, 0x2169, 0x2170, 0x2171, 0x2172, 0x2173,
        0x2174, 0x2175, 0x2176, 0x2177, 0x2178, 0x2179, 0x221A, 0x2220,
        0x2229, 0x222A, 0x222B, 0x2235, 0x2252, 0x2261, 0x22A5, 0x3231,
        0x5022, 0x5040, 0x5042, 0x5046, 0x5070, 0x5094, 0x5164, 0x5215,
        0x52AF, 0x5300, 0x5307, 0x5324, 0x5372, 0x5393, 0x5586, 0x5759,
        0x5765, 0x5953, 0x5963, 0x6085, 0x6111, 0x6120, 0x6130, 0x6137,
        0x6198, 0x6213, 0x6460, 0x6600, 0x6609, 0x6615, 0x6624, 0x6631,
        0x6657, 0x6659, 0x6665, 0x6673, 0x6699, 0x6766, 0x6801, 0x6844,
        0x6852, 0x6968, 0x6998, 0x6D96, 0x6DAC, 0x6DCF, 0x6DF2, 0x6DF8,
        0x6DFC, 0x6E27, 0x6E39, 0x6E3C, 0x6E5C, 0x6EBF, 0x6F88, 0x6FB5,
        0x6FF5, 0x7005, 0x7007, 0x7028, 0x7085, 0x70AB, 0x7104, 0x710F,
        0x7146, 0x7147, 0x715C, 0x71C1, 0x71FE, 0x72B1, 0x72BE, 0x7324,
        0x7377, 0x73BD, 0x73C9, 0x73D2, 0x73D6, 0x73E3, 0x73F5, 0x7407,
        0x7426, 0x7429, 0x742A, 0x742E, 0x7462, 0x7489, 0x749F, 0x7501,
        0x756F, 0x7682, 0x769B, 0x769C, 0x769E, 0x76A6, 0x7746, 0x7821,
        0x784E, 0x7864, 0x787A, 0x7930, 0x7994, 0x799B, 0x7AD1, 0x7AE7,
        0x7AEB, 0x7B9E, 0x7D48, 0x7D5C, 0x7DA0, 0x7DB7, 0x7DD6, 0x7E52,
        0x7F47, 0x7FA1, 0x8301, 0x8362, 0x837F, 0x83C7, 0x83F6, 0x8448,
        0x84B4, 0x8553, 0x8559, 0x856B, 0x85B0, 0x8807, 0x88F5, 0x8A12,
        0x8A37, 0x8A79, 0x8AA7, 0x8ABE, 0x8ADF, 0x8AF6, 0x8B53, 0x8B7F,
        0x8CF0, 0x8CF4, 0x8D12, 0x8D76, 0x8ECF, 0x9067, 0x90DE, 0x9115,
        0x9127, 0x91D7, 0x91DA, 0x91DE, 0x91E4, 0x91E5, 0x91ED, 0x91EE,
        0x9206, 0x920A, 0x9210, 0x9239, 0x923A, 0x923C, 0x9240, 0x924E,
        0x9251, 0x9259, 0x9267, 0x9277, 0x9278, 0x9288, 0x92A7, 0x92D0,
        0x92D3, 0x92D5, 0x92D7, 0x92D9, 0x92E0, 0x92E7, 0x92FB, 0x92FF,
        0x9302, 0x931D, 0x931E, 0x9321, 0x9325, 0x9348, 0x9357, 0x9370,
        0x93A4, 0x93C6, 0x93DE, 0x93F8, 0x9431, 0x9445, 0x9448, 0x9592,
        0x969D, 0x96AF, 0x9733, 0x973B, 0x9743, 0x974D, 0x974F, 0x9751,
        0x9755, 0x9857, 0x9865, 0x9927, 0x999E, 0x9A4E, 0x9AD9, 0x9ADC,
        0x9B72, 0x9B75, 0x9B8F, 0x9BB1, 0x9BBB, 0x9C00, 0x9D6B, 0x9D70,
        0x9E19, 0x9ED1, 0xF9DC, 0xFA15, 0xFA16, 0xFA17, 0xFA18, 0xFA19,
        0xFA1A, 0xFA1B, 0xFA1C, 0xFA1D, 0xFA1E, 0xFA1F, 0xFA20, 0xFA21,
        0xFA22, 0xFA23, 0xFA24, 0xFA25, 0xFA26, 0xFA27, 0xFA28, 0xFA29,
        0xFA2A, 0xFA2B, 0xFA2C, 0xFA2D, 0xFFE2,
    ];
}
