using Kichikuou.Engine.Script;

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
/// <paramref name="Text"/> is the item's text without its mark. A × item and Merchant's △ only open the menu
/// again in the original (<paramref name="Enabled"/> false); Subordinates' △ still opens <c>Cﾌﾞｶ</c>. The
/// week's uses are on the status strip (<see cref="StatusStrip"/>).
/// </summary>
public sealed record CommandMenuItem(int Choice, string Text, CommandAction Action, bool Enabled);

/// <summary>One of the menu's other items (→ Leazas Castle, Load/Save, All Data, Think, TADA).</summary>
public sealed record CommandMenuLink(int Choice, string Text);

/// <summary>
/// <c>COMMAND.ADV</c>'s command menu (the right click on the map, <c>CommandPhase.CommandMenuAsync</c>,
/// <c>:183-312</c>) as our panel shows it (docs/ui/command-screen.md, "The command panel"): the commands, the
/// other items, and Turn END (<see cref="TurnEnd"/>); the status window it opens with is on our status strip
/// (<see cref="StatusStrip"/>). The script is answered as the original's menu is: the item's choice, or 0 for a
/// cancel (back to the map).
/// </summary>
public sealed record CommandMenu(
    IReadOnlyList<CommandMenuItem> Commands,
    IReadOnlyList<CommandMenuLink> Links,
    int TurnEnd,
    string TurnEndText)
{
    private const string File = "COMMAND.ADV";

    /// <summary>
    /// The command menu <paramref name="items"/> make, their <paramref name="texts"/> as shown (mark and
    /// text); or null when they are not <c>COMMAND.ADV</c>'s command menu (Turn END last: <c>:310</c>, the
    /// thieves' <c>:218</c>).
    /// </summary>
    public static CommandMenu? For(IReadOnlyList<SceneMenuItem> items, IReadOnlyList<string> texts)
    {
        if (items.Count == 0 || items.Count != texts.Count || items.Any(i => i.At.File != File))
            return null;
        if (items[^1].At.Line is not (310 or 218))
            return null;
        var commands = new List<CommandMenuItem>();
        var links = new List<CommandMenuLink>();
        for (int i = 0; i < items.Count - 1; i++)
        {
            string text = Unmarked(texts[i]);
            int line = items[i].At.Line;
            if (CommandOf(line) is var (action, enabled))
                commands.Add(new CommandMenuItem(i + 1, text, action, enabled));
            else if (line is 288 or 292 or 296 or 300 or 305 or 210 or 214)
                links.Add(new CommandMenuLink(i + 1, text));
            else
                return null;
        }
        return new CommandMenu(commands, links, items.Count, Unmarked(texts[^1]));
    }

    /// <summary>
    /// A command's item at <paramref name="line"/>: Leazas' Construction ○229/×235, Harem ○241/×247, Treasure
    /// 252, Merchant ○257/△263, Subordinates ◎270/○276/△283; the thieves' Harem ○187/×193, Subordinates
    /// ○199/△205.
    /// </summary>
    private static (CommandAction, bool)? CommandOf(int line) => line switch
    {
        229 => (CommandAction.Build, true),
        235 => (CommandAction.Build, false),
        241 or 187 => (CommandAction.Harem, true),
        247 or 193 => (CommandAction.Harem, false),
        252 => (CommandAction.Items, true),
        257 => (CommandAction.Trade, true),
        263 => (CommandAction.Trade, false),
        270 or 276 or 283 or 199 or 205 => (CommandAction.Units, true),
        _ => null,
    };

    /// <summary>An item's text without the mark the file puts first.</summary>
    private static string Unmarked(string shown) => shown.TrimStart('○', '×', '△', '◎', ' ', '　').Replace('’', '\'');
}
