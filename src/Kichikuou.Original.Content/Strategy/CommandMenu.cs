using Kichikuou.Engine.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>What a command of the command menu opens, for its tag on our panel (ours: the original has no such tags).</summary>
public enum CommandAction
{
    /// <summary>Construction (<c>Cﾃｲﾄ</c>).</summary>
    Build,

    /// <summary>Harem (<c>Cﾊｰﾚﾑ</c>).</summary>
    Harem,

    /// <summary>Treasure (<c>Cﾀｶﾗ</c>).</summary>
    Items,

    /// <summary>Merchant (<c>Cｼｮｳﾆﾝ</c>).</summary>
    Trade,

    /// <summary>Subordinates (<c>Cﾌﾞｶ</c>).</summary>
    Units,
}

/// <summary>
/// One command of the command menu: <paramref name="Choice"/> answers the menu (its 1-based place),
/// <paramref name="Text"/> is the item's text without its mark; <paramref name="Left"/> of
/// <paramref name="Total"/> uses are left this week (the marks ○ ◎ × △; Treasure has none). A × item and
/// Merchant's △ only open the menu again in the original (<paramref name="Enabled"/> false); Subordinates'
/// △ still opens <c>Cﾌﾞｶ</c>.
/// </summary>
public sealed record CommandMenuItem(int Choice, string Text, CommandAction Action, bool Enabled, int Left, int Total);

/// <summary>One of the menu's other items (→ Leazas Castle, Load/Save, All Data, Think, TADA).</summary>
public sealed record CommandMenuLink(int Choice, string Text);

/// <summary>
/// <c>COMMAND.ADV</c>'s command menu (the right click on the map, <c>CommandPhase.CommandMenuAsync</c>,
/// <c>:183-312</c>) as our panel shows it (docs/ui/command-screen.md, "The command panel"): the status
/// window's lines (the nation, its money and troops as the files print them, the date with LP, which the
/// original computes but does not show), the commands with their weekly uses, the other items, and Turn END
/// (<see cref="TurnEnd"/>). The script is answered as the original's menu is: the item's choice, or 0 for a
/// cancel (back to the map).
/// </summary>
public sealed record CommandMenu(
    string Nation,
    string Date,
    IReadOnlyList<(string Label, string Value)> Status,
    IReadOnlyList<CommandMenuItem> Commands,
    IReadOnlyList<CommandMenuLink> Links,
    int TurnEnd,
    string TurnEndText,
    bool Thieves)
{
    private const string File = "COMMAND.ADV";

    /// <summary>The status window's text runs: Leazas (<c>:443</c>) and the thieves (<c>:472</c>).</summary>
    public static bool IsStatus(ScriptRef text) => text.File == File && text.Line is 443 or 472;

    /// <summary>
    /// The command menu <paramref name="items"/> make, their <paramref name="texts"/> as shown (mark and
    /// text), under the status window's text <paramref name="status"/> as printed; or null when they are not
    /// <c>COMMAND.ADV</c>'s command menu (Turn END last: <c>:310</c>, the thieves' <c>:218</c>) or no status
    /// was printed.
    /// </summary>
    public static CommandMenu? For(GameState state, IReadOnlyList<SceneMenuItem> items, IReadOnlyList<string> texts, string? status)
    {
        if (status is null || items.Count == 0 || items.Count != texts.Count || items.Any(i => i.At.File != File))
            return null;
        int last = items[^1].At.Line;
        if (last is not (310 or 218))
            return null;
        var commands = new List<CommandMenuItem>();
        var links = new List<CommandMenuLink>();
        for (int i = 0; i < items.Count - 1; i++)
        {
            string text = Unmarked(texts[i]);
            int line = items[i].At.Line;
            if (CommandOf(line) is var (action, enabled, left, total))
                commands.Add(new CommandMenuItem(i + 1, text, action, enabled, left, total));
            else if (line is 288 or 292 or 296 or 300 or 305 or 210 or 214)
                links.Add(new CommandMenuLink(i + 1, text));
            else
                return null;
        }
        // The status window: the nation, two "label: value" lines, "Month m, Week w".
        string[] lines = [.. status.Split('\n').Select(l => l.Trim(' ', '　'))];
        if (lines.Length < 4)
            return null;
        var pairs = lines[1..3].Select(l => l.IndexOfAny([':', '：']) is var at and >= 0 ? (l[..at].Trim(), l[(at + 1)..].Trim()) : ("", l)).ToArray();
        string date = $"LP {state.DateYear.Value} · {lines[3].Replace(", ", " · ").Replace(",", " ·")}";
        return new CommandMenu(lines[0].Replace('’', '\''), date, pairs, commands, links, items.Count, Unmarked(texts[^1]), last == 218);
    }

    /// <summary>
    /// A command's item at <paramref name="line"/>: Leazas' Construction ○229/×235, Harem ○241/×247, Treasure
    /// 252, Merchant ○257/△263, Subordinates ◎270/○276/△283; the thieves' Harem ○187/×193, Subordinates
    /// ○199/△205.
    /// </summary>
    private static (CommandAction, bool, int, int)? CommandOf(int line) => line switch
    {
        229 => (CommandAction.Build, true, 1, 1),
        235 => (CommandAction.Build, false, 0, 1),
        241 or 187 => (CommandAction.Harem, true, 1, 1),
        247 or 193 => (CommandAction.Harem, false, 0, 1),
        252 => (CommandAction.Items, true, 0, 0),
        257 => (CommandAction.Trade, true, 1, 1),
        263 => (CommandAction.Trade, false, 0, 1),
        270 => (CommandAction.Units, true, 2, 2),
        276 => (CommandAction.Units, true, 1, 2),
        283 => (CommandAction.Units, true, 0, 2),
        199 => (CommandAction.Units, true, 1, 1),
        205 => (CommandAction.Units, true, 0, 1),
        _ => null,
    };

    /// <summary>An item's text without the mark the file puts first.</summary>
    private static string Unmarked(string shown) => shown.TrimStart('○', '×', '△', '◎', ' ', '　').Replace('’', '\'');
}
