using Kichikuou.Engine.Script;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>An item between Start Search and Return on <see cref="DungeonEntryMenu"/>: its 1-based place and its text.</summary>
public sealed record DungeonEntryItem(int Choice, string Text);

/// <summary>
/// The dungeon's own menu before a search (<c>Cﾒｲｷｭｳ.ADV:238-305</c>, <c>DungeonCommand.MainMenuAsync</c>) as the touch
/// mode's rail shows it (docs/ui/dungeon-menu.md, §6, the owner's B): Start Search first, then Bring or Leave Merim, Bring
/// Pink Cow and God's Door as the script offers them (<see cref="Others"/>), Return last. The script is answered with the
/// item's choice; a cancel (0) is Return, as in the original.
/// </summary>
public sealed record DungeonEntryMenu(int Start, string StartText, IReadOnlyList<DungeonEntryItem> Others, int Return, string ReturnText)
{
    private const string File = "Cﾒｲｷｭｳ.ADV";
    private const int StartLine = 267, ReturnLine = 295;

    /// <summary>The items registered between Start Search and Return: the Merims, the cow, God's Door (<c>:272-290</c>).</summary>
    private static readonly int[] OtherLines = [272, 278, 284, 290];

    /// <summary>
    /// The menu <paramref name="items"/> make, their <paramref name="texts"/> as shown, or null when they are not the
    /// dungeon's menu before a search: Start Search registered at <c>:267</c> first, Return at <c>:295</c> last, the
    /// others among <c>:272</c>, <c>:278</c>, <c>:284</c> and <c>:290</c>, each once.
    /// </summary>
    public static DungeonEntryMenu? For(IReadOnlyList<SceneMenuItem> items, IReadOnlyList<string> texts)
    {
        if (items.Count < 2 || items.Count != texts.Count || items.Any(i => i.At.File != File)
            || items[0].At.Line != StartLine || items[^1].At.Line != ReturnLine)
            return null;
        var middle = items.Skip(1).Take(items.Count - 2).Select(i => i.At.Line).ToList();
        if (middle.Any(line => !OtherLines.Contains(line)) || middle.Distinct().Count() != middle.Count)
            return null;
        var others = Enumerable.Range(1, items.Count - 2).Select(i => new DungeonEntryItem(i + 1, texts[i])).ToList();
        return new DungeonEntryMenu(1, texts[0], others, items.Count, texts[^1]);
    }
}
