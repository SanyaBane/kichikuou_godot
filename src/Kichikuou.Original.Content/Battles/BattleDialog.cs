using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;

namespace Kichikuou.Original.Content.Battles;

/// <summary>One item of a dialog on our battle screen: the script's label, the code the menu returns, and whether it is shown as a warning.</summary>
public sealed record BattleDialogItem(ScriptRef Label, Sys3Int Code, bool Warning);

/// <summary>
/// A script menu of the battle as our own screen asks it (docs/ui/battle-screen.md, step 4): a question of our
/// own over the script's items. Cancelling returns no code, as a right click on the script's menu does.
/// Engine-independent.
/// </summary>
/// <param name="Question">Our question (owner, 2026-10-13).</param>
/// <param name="Note">A line under it, muted.</param>
/// <param name="Items">The menu's items, in its order.</param>
public sealed record BattleDialog(string Question, string Note, IReadOnlyList<BattleDialogItem> Items)
{
    /// <summary>
    /// The dialog for <paramref name="menu"/> on our battle screen, or null when the original's menu stays: only the
    /// retreat confirmation opens over the battle (<c>FIGT.ADV:571-580</c>; Retreat, the whole army's, as a
    /// warning); <c>FIGT_B</c>'s menus come before its field is drawn, on the original's screen.
    /// </summary>
    public static BattleDialog? For(BattleMenu menu) => menu.Kind switch
    {
        BattleMenuKind.RetreatConfirm => new BattleDialog("Retreat the whole army?", "The battle is lost.",
            [.. menu.Options.Select(o => new BattleDialogItem(o.Label, o.Code, o.Code == 1))]),
        _ => null,
    };
}
