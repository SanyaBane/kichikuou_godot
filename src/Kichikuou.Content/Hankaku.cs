// Parts translated from xsystem35 (https://github.com/kichikuou/xsystem35-sdl2), hankaku.c (the
// table of zen2han_utf8): Copyright (C) 1997-1998 Masaki Chikama (Wren). GNU General Public License,
// version 2 or later, as KichikuouGodot (LICENSE).

using System.Text;

namespace Kichikuou.Content;

/// <summary>
/// The engine's full-width → half-width conversion of message text (<c>ZH 1</c>): xsystem35
/// <c>zen2han_utf8</c> (<c>hankaku.c:167-343</c>), which mirrors the Shift-JIS table the game
/// runs with. Full-width ASCII, the ideographic space, the curly apostrophe and quote, the
/// Japanese punctuation and the katakana become their half-width forms; anything else is kept.
/// </summary>
public static class Hankaku
{
    private static readonly Dictionary<char, string> Map = Build();

    /// <summary>Applies <c>zen2han</c> to <paramref name="text"/>.</summary>
    public static string ToHalfWidth(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
            sb.Append(Map.TryGetValue(c, out var half) ? half : c.ToString());
        return sb.ToString();
    }

    private static Dictionary<char, string> Build()
    {
        var map = new Dictionary<char, string>
        {
            ['’'] = "'",   // ’
            ['”'] = "\"",  // ”
            ['　'] = " ",
            ['、'] = "､", ['。'] = "｡", ['「'] = "｢", ['」'] = "｣", ['゛'] = "ﾞ", ['゜'] = "ﾟ",
            ['・'] = "･", ['ー'] = "ｰ",
            ['￣'] = "~", ['￥'] = "\\",
        };
        const string kana = "ァアィイゥウェエォオカキクケコサシスセソタチッツテトナニヌネノハヒフヘホマミムメモャヤュユョヨラリルレロワヲン";
        const string halfKana = "ｧｱｨｲｩｳｪｴｫｵｶｷｸｹｺｻｼｽｾｿﾀﾁｯﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓｬﾔｭﾕｮﾖﾗﾘﾙﾚﾛﾜｦﾝ";
        for (int i = 0; i < kana.Length; i++)
            map[kana[i]] = halfKana[i].ToString();
        // ！..｝ except the ones the table leaves out: ＂ ＇ (no case in zen2han_utf8) and ～.
        foreach (char ascii in "!#$%&()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}")
            map[(char)(ascii - 0x21 + 0xFF01)] = ascii.ToString();
        return map;
    }
}
