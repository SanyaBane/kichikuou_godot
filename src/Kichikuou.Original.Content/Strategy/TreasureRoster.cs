using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>How a cell of our Treasure screen's grid is inked, after <c>Sｱｲﾃﾑ</c>'s colours (<c>:181-226</c>).</summary>
public enum TreasureInk
{
    /// <summary>White: a unit at status 1 or 2, an item in stock.</summary>
    Normal,

    /// <summary>The held items' colour (253).</summary>
    Held,

    /// <summary>Grey: a unit at another status (drawn in its shadow's colour), an item without effect (12).</summary>
    Off,
}

/// <summary>
/// A cell of the grid: the entry's number (its cell), its name, its holder's name (an item held by a unit, else ""),
/// its ink, whether it is (or holds) Chaos or Nikkou, and whether a unit holds an item.
/// </summary>
public sealed record TreasureEntry(int Number, string Name, string Holder, TreasureInk Ink, bool Fixed, bool HoldsItem);

/// <summary>A figure of the line under the picture: its label and value.</summary>
public sealed record TreasureFigure(string Label, string Value);

/// <summary>
/// The picture panel and its line for an entry: the CG drawn into the panel and the panel's empty picture (a unit's status
/// CG over 600, an item's 750 + n over 692), the name, a subtitle (the unit's class, or Equippable/Unequippable) and the
/// figures (<c>L_00eac</c>, <c>L_0101d</c>).
/// </summary>
public sealed record TreasureDetails(int PictureCg, int EmptyCg, string Name, string Subtitle, bool SubtitleOff, IReadOnlyList<TreasureFigure> Figures);

/// <summary>
/// The first pick's prompt (<c>Sｱｲﾃﾑ.ADV:96-122</c>): a unit picked ("X is equipping — Which item?") or an item picked
/// ("Equipping X — Which character?"). The original shrinks the picked entry's picture panel beside it, where its
/// description cannot be read (the owner, 2026-10-19): ours shows a unit's face (<c>Unit_FaceCg</c>, cut as
/// <see cref="UnitFaces"/>; 0 when it has none) and an item by its name alone (<paramref name="FaceCg"/> 0).
/// </summary>
public sealed record TreasurePrompt(bool UnitFirst, int Number, string Name, int FaceCg);

/// <summary>
/// Our own Treasure screen's model (docs/ui/pickers.md, round V: <c>Sｱｲﾃﾑ</c>'s own screen in lacquer): the view the
/// picker shows, its grid's entries at their numbers' cells, the picture panel and line of an entry, the first pick's
/// prompt — all read from the state as <c>Sｱｲﾃﾑ</c> shows them — and the gestures by which the screen answers the
/// picker. It decides nothing: the picker still hit-tests the clicks, refuses, asks and equips.
/// </summary>
public static class TreasureRoster
{
    /// <summary>The grid's cells: 3 columns of 17 (the 51st is no entry: the script's pointer rests there).</summary>
    public const int Cells = 51;

    /// <summary>The screen's background, which every picture panel is cut from (<c>Sｱｲﾃﾑ</c>'s first <c>G 819</c>).</summary>
    public const int ScreenCg = 819;

    /// <summary>The view's empty picture panels (<c>L_00042</c>): 600 for the characters, 692 for the items.</summary>
    public const int UnitEmptyCg = 600, ItemEmptyCg = 692;

    /// <summary>The unit classes' names (<c>L_00eac</c>).</summary>
    private static readonly Dictionary<int, string> Classes = new()
    {
        [0] = "Assault Troops", [1] = "Archers", [2] = "Mages", [3] = "Magic Warriors", [4] = "Phantom Beasts",
        [10] = "Demon Assault", [11] = "Demon Magic",
    };

    /// <summary>The items that never move (<c>:249-266</c>, <c>:290-308</c>): Chaos (1) and Nikkou (2).</summary>
    public static bool Fixed(int item) => item is 1 or 2;

    /// <summary>The view the picker shows (<c>Ui_ItemViewMode</c>, kept between visits): 0 the characters, 1 the items.</summary>
    public static int View(GameState s) => s.Ui_ItemViewMode.Value;

    /// <summary>Item <paramref name="item"/>'s name: its string (<c>Item_StrBase + n - 1</c>).</summary>
    public static string ItemName(GameState s, int item) =>
        item is >= 1 and <= 50 ? DeploymentTable.StringAt(s, (s.Item_StrBase + (Sys3Int)(item - 1)).Value) : "";

    /// <summary>
    /// The view's entries as <c>Sｱｲﾃﾑ</c> lists them (<c>:170-226</c>): in the characters' view the units with a status
    /// (grey but at 1 and 2), in the items' view the items owned (white in stock, held coloured, grey without effect).
    /// </summary>
    public static IReadOnlyList<TreasureEntry> Entries(GameState s)
    {
        var entries = new List<TreasureEntry>();
        int view = View(s);
        for (int n = 1; n <= 50; n++)
        {
            if (view == 0)
            {
                var u = s.Units[n];
                if (u.Status == 0)
                    continue;
                int item = u.Item.Value;
                entries.Add(new TreasureEntry(n, DeploymentTable.UnitName(s, n), "",
                    u.Status.Value is 1 or 2 ? TreasureInk.Normal : TreasureInk.Off, Fixed(item), item != 0));
            }
            else if (view == 1)
            {
                var it = s.Items[n];
                int holder = it.Holder.Value;
                if (holder == 0)
                    continue;
                var ink = it.Effect == 0 ? TreasureInk.Off : holder == 100 ? TreasureInk.Normal : TreasureInk.Held;
                entries.Add(new TreasureEntry(n, ItemName(s, n), holder == 100 ? "" : DeploymentTable.UnitName(s, holder), ink, Fixed(n), false));
            }
        }
        return entries;
    }

    /// <summary>
    /// The picture panel and line of entry <paramref name="number"/> in the view shown (<c>L_00eac</c>, <c>L_0101d</c>): a
    /// unit's status CG, war cost, troops, class and item; an item's CG, whether it can be equipped and who holds it.
    /// Null for a number that is no entry.
    /// </summary>
    public static TreasureDetails? Details(GameState s, int number)
    {
        if (number is < 1 or > 50 || Entries(s).All(e => e.Number != number))
            return null;
        if (View(s) == 0)
        {
            var u = s.Units[number];
            int item = u.Item.Value;
            return new TreasureDetails(u.PortraitCg.Value, UnitEmptyCg, DeploymentTable.UnitName(s, number),
                Classes.GetValueOrDefault(u.Class.Value, ""), false,
                [
                    new("War cost", $"{u.DeployCost.Value * 100:N0}K"),
                    new("Troops", u.Troops.Value.ToString()),
                    new("Equipping", item == 0 ? "—" : ItemName(s, item)),
                ]);
        }
        var it = s.Items[number];
        bool equippable = it.Effect != 0;
        int holder = it.Holder.Value;
        string heldBy = !equippable ? "—" : holder == 100 ? "Nobody (in stock)" : DeploymentTable.UnitName(s, holder);
        return new TreasureDetails(750 + number, ItemEmptyCg, ItemName(s, number), equippable ? "Equippable" : "Unequippable",
            !equippable, [new("Held by", heldBy)]);
    }

    /// <summary>
    /// The first pick's prompt, or null: a unit picked turns the view to the items (<c>Ui_ItemUnitSel</c>, "Which item?"),
    /// an item picked to the characters (<c>Ui_ItemSel</c>, "Which character?").
    /// </summary>
    public static TreasurePrompt? Prompt(GameState s)
    {
        int unit = s.Ui_ItemUnitSel.Value, item = s.Ui_ItemSel.Value;
        if (unit is >= 1 and <= 50 && item == 0)
            return new TreasurePrompt(true, unit, DeploymentTable.UnitName(s, unit), s.Units[unit].FaceCg.Value);
        if (item is >= 1 and <= 50 && unit == 0)
            return new TreasurePrompt(false, item, ItemName(s, item), 0);
        return null;
    }

    /// <summary>
    /// Our own question before an item picked first goes to unit <paramref name="unit"/>, who holds another one
    /// (docs/ui/pickers.md, round W; the owner, 2026-10-19): the picker equips at once and puts the unit's item back in
    /// stock without asking (<c>L_0043a</c>), so our screen asks first and a No never sends the click. Null where the
    /// script would not equip that way: no item picked first, a unit not at status 1 or 2, one holding nothing, the
    /// picked item itself, or Chaos or Nikkou (refused by the script).
    /// </summary>
    public static IReadOnlyList<string>? ReplaceQuestion(GameState s, int unit)
    {
        int item = s.Ui_ItemSel.Value;
        if (View(s) != 0 || s.Ui_ItemUnitSel.Value != 0 || item is < 1 or > 50 || Fixed(item) || unit is < 1 or > 50)
            return null;
        var u = s.Units[unit];
        int held = u.Item.Value;
        if (u.Status.Value is not (1 or 2) || held == 0 || held == item || Fixed(held))
            return null;
        return [$"{DeploymentTable.UnitName(s, unit)} already has {ItemName(s, held)} equipped.", $"Replace it with {ItemName(s, item)}?"];
    }

    /// <summary>Where the script's pointer rests before the first click: the grid's empty 51st cell, so the picker hovers nothing.</summary>
    public static (int X, int Y) Rest => GesturePoints.PickerCell(Cells);

    /// <summary>A pick: a click on entry <paramref name="number"/>'s cell, which the picker answers as its player's click.</summary>
    public static void Pick(int number, PointerScript pointer)
    {
        var (x, y) = GesturePoints.PickerCell(number);
        pointer.Click(x, y);
    }

    /// <summary>The bar's "Item/Character" (502): the view turned, a half-made pick dropped.</summary>
    public static void Turn(PointerScript pointer)
    {
        var (x, y) = GesturePoints.PickerButton(2);
        pointer.Click(x, y);
    }

    /// <summary>A right click (998): a half-made pick dropped (the view turned back), or the screen left when there is none.</summary>
    public static void Back(PointerScript pointer)
    {
        var (x, y) = Rest;
        pointer.RightClick(x, y);
    }

    /// <summary>End: a click on the bar's "End" (503).</summary>
    public static void End(PointerScript pointer)
    {
        var (x, y) = GesturePoints.PickerButton(3);
        pointer.Click(x, y);
    }
}
