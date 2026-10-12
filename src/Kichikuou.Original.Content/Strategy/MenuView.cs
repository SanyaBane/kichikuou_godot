namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// An open <c>]</c> menu as data, for any interface to draw: its items in order (the script's
/// answer is the 1-based index) and the selection window it shows in (<c>B1</c>/<c>B2</c>), null
/// when none was set.
/// </summary>
public sealed record MenuView(IReadOnlyList<MenuEntry> Items, (int X, int Y, int W)? Window)
{
    /// <summary>
    /// The marks the command menus lead their items with (<c>COMMAND.ADV:188-284</c>): ○ for a command
    /// the player has, × for one not available (the item re-opens the menu), △ for a third state some
    /// commands show; what △ means is the command's own.
    /// </summary>
    public const string Marks = "○×△";

    /// <summary>The items' texts as printed, one line each, from <c>]</c>'s captured texts.</summary>
    public static MenuView FromTexts(IEnumerable<string> texts, (int X, int Y, int W)? window) =>
        new([.. texts.Select(MenuEntry.FromText)], window);
}

/// <summary>A menu item: its text without the leading mark, and the mark (<see cref="MenuView.Marks"/>) if it has one.</summary>
public sealed record MenuEntry(string Text, char? Mark)
{
    /// <summary>The item as the original prints it: the mark, then the text.</summary>
    public string Shown => Mark is { } mark ? mark + Text : Text;

    /// <summary>Splits a printed item into its mark and its text.</summary>
    public static MenuEntry FromText(string text) =>
        text.Length > 0 && MenuView.Marks.Contains(text[0]) ? new(text[1..], text[0]) : new(text, null);
}
