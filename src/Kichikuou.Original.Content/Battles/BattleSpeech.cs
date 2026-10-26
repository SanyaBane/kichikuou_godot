namespace Kichikuou.Original.Content.Battles;

/// <summary>
/// The line a speech bubble holds now (<c>WAO_SUB</c> 141/142, the player unit's, or 151/152, the enemy
/// commander's; 142/152 are the original's dark bubbles, thoughts), as our battle screen shows it in a
/// dialogue panel of its own (docs/ui/battle-screen.md, "The dialogue panel"). <see cref="Text"/> is what the
/// script printed into the bubble since it opened or its last key wait, its line breaks — made for the
/// original's 240-pixel bubble — flowed into one paragraph (<see cref="Flow"/>). <see cref="Read"/>: the player read every
/// message in it before, as a message window's text tells it (<see cref="Kichikuou.Content.Screen.TextBox.ReadBefore"/>), for the
/// already-read text's colour (docs/ui/read-text.md, §7).
/// </summary>
public sealed record BattleSpeech(BattleSide Side, bool Thought, string Text, bool Read = false)
{
    /// <summary>
    /// The printed lines as one paragraph: each line trimmed, the empty ones dropped, joined with a space
    /// between two Latin characters (the English script) and with nothing next to a Japanese one.
    /// </summary>
    public static string Flow(string printed)
    {
        var text = new System.Text.StringBuilder();
        foreach (var raw in printed.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0)
                continue;
            if (text.Length > 0 && !IsWide(text[^1]) && !IsWide(line[0]))
                text.Append(' ');
            text.Append(line);
        }
        return text.ToString();
    }

    /// <summary>A Japanese character (kana — half-width too —, kanji, full-width forms and punctuation): no spaces between words.</summary>
    private static bool IsWide(char c) => c >= '⺀';
}
