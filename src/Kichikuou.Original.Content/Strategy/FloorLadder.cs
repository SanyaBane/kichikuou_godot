using Kichikuou.Engine.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// One way off the floor on <see cref="FloorLadder"/>: <paramref name="Choice"/> answers the script's menu (the item's
/// 1-based place), <paramref name="Text"/> is the item's text (Ascend or Descend), <paramref name="To"/> the floor it
/// leads to, <paramref name="Deeper"/> whether it goes on into the dungeon (else back towards its entrance).
/// </summary>
public sealed record FloorWay(int Choice, string Text, int To, bool Deeper);

/// <summary>
/// The dungeon's floor menu (<c>Cﾒｲｷｭｳ.ADV:858-893</c>, <c>DungeonCommand.FloorMenuAsync</c>) as our ladder shows it
/// (docs/ui/dungeon-menu.md, the owner's 3e-3g): the script offers Ascend, Descend and Retreat only where they apply,
/// so its rows move between floors; the ladder keeps Ascend above the floor, Descend below it and Retreat at its foot,
/// a way the script does not offer left out (<see cref="Up"/> or <see cref="Down"/> null). In a cave (<c>DgnRun_FloorMode</c>
/// 1) Descend goes deeper, in a tower (2) Ascend. The script is answered as the original's menu is: the item's choice,
/// or 0 for a cancel, which only opens the menu again.
/// </summary>
public sealed record FloorLadder(int Floor, FloorWay? Up, FloorWay? Down, int Retreat, string RetreatText)
{
    private const string File = "Cﾒｲｷｭｳ.ADV";
    private const int RetreatLine = 889;

    /// <summary>
    /// The ladder <paramref name="items"/> make, their <paramref name="texts"/> as shown, or null when they are not the
    /// floor menu: its items registered at <c>:866</c> (Ascend, back), <c>:872</c> (Ascend, deeper), <c>:878</c>
    /// (Descend, back), <c>:884</c> (Descend, deeper) and Retreat at <c>:889</c>, last.
    /// </summary>
    public static FloorLadder? For(GameState state, IReadOnlyList<SceneMenuItem> items, IReadOnlyList<string> texts)
    {
        if (items.Count == 0 || items.Count != texts.Count || items.Any(i => i.At.File != File) || items[^1].At.Line != RetreatLine)
            return null;
        int floor = state.DgnRun_Floor.Value;
        FloorWay? up = null, down = null;
        for (int i = 0; i < items.Count - 1; i++)
        {
            bool deeper;
            switch (items[i].At.Line)
            {
                case 866 or 872 when up is null:
                    deeper = items[i].At.Line == 872;
                    up = new FloorWay(i + 1, texts[i], deeper ? floor + 1 : floor - 1, deeper);
                    break;
                case 878 or 884 when down is null:
                    deeper = items[i].At.Line == 884;
                    down = new FloorWay(i + 1, texts[i], deeper ? floor + 1 : floor - 1, deeper);
                    break;
                default:
                    return null;
            }
        }
        return new FloorLadder(floor, up, down, items.Count, texts[^1]);
    }
}
