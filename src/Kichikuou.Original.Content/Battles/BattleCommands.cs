using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;

namespace Kichikuou.Original.Content.Battles;

/// <summary>
/// One command our battle screen offers: its label (the script's own line, as <see cref="FigtSelScreen"/>
/// prints it, unless our screen names it itself) and the answer it gives <c>FIGT_SEL</c>.
/// </summary>
/// <param name="Label">Where the script's label is in the scenario.</param>
/// <param name="Choice">The pair returned to the core.</param>
/// <param name="Current">A standing order the unit has now (marked in its menu).</param>
/// <param name="Name">Our screen's own label, shown instead of the script's (null: the script's).</param>
public sealed record BattleCommand(ScriptRef Label, FigtSelChoice Choice, bool Current = false, string? Name = null);

/// <summary>
/// <c>FIGT_SEL</c> answered by our own battle screen (docs/ui/battle-screen.md, step 4, "Next — our own
/// command input"): the banners on an enemy's card — the attacks on it, the only way to attack — and the
/// cards' context menus. Built from the core's <see cref="FigtSelMenu"/> alone: every command is a pair the
/// core allows (<see cref="FigtSelMenu.Allows"/>), so nothing here decides a rule. Engine-independent.
/// </summary>
public sealed class BattleCommands(FigtSelMenu menu)
{
    private const string Sel = "FIGT_SEL.ADV";

    /// <summary>Our screen's name for the Direct attack (owner, 2026-10-13): the close-combat exchange, front row to front row, which the enemy answers.</summary>
    public const string MeleeName = "Melee";

    /// <summary>
    /// Our screen's name for a side's action in an exchange (owner, 2026-10-13; on the fighters' cards): the
    /// banners' names — Melee, Bow, Magic — and Special for any special (its own name is not known after the
    /// choice: <c>Bt_SpecialAtk</c> 3 is three of them), Defense; empty for none.
    /// </summary>
    public static string ActionName(ExchangeAction action) => action switch
    {
        ExchangeAction.Melee => MeleeName,
        ExchangeAction.Bow => "Bow",
        ExchangeAction.Magic => "Magic",
        ExchangeAction.Special => "Special",
        ExchangeAction.Defense => "Defense",
        _ => "",
    };

    /// <summary>
    /// The argument of the commands that take no target: no enemy button under the pointer, as a right
    /// click off the panels gives in the original (<c>Tmp0001</c> = 99; Q-18 then binds no commander).
    /// </summary>
    public static readonly Sys3Int NoButton = 99;

    /// <summary>The core's menu.</summary>
    public FigtSelMenu Menu => menu;

    /// <summary>The acting unit's place (<c>Bt_PlayerActSlot</c>).</summary>
    public BattlePlace Acting => new(BattleSide.Player, menu.ActingSlot.Value);

    /// <summary>
    /// The attacks on <paramref name="target"/>, one banner each: the unit's own attack type first
    /// (<c>AttackType + 1</c>: Bow, Magic or Direct), then Direct, then the specials (the debug Cheater Beam
    /// among them) — each only when its mode allows the target's button (4-7). Empty for an own card and
    /// for a target no attack reaches (a melee unit in the back row). Direct is named "Melee" here.
    /// </summary>
    public IReadOnlyList<BattleCommand> Banners(BattlePlace target)
    {
        if (target.Side != BattleSide.Enemy)
            return [];
        var button = (Sys3Int)(target.Slot + 3);
        var own = menu.AttackType + 1;
        var modes = new List<Sys3Int> { own };
        if (own != FigtSelCommand.Direct)
            modes.Add(FigtSelCommand.Direct);
        modes.AddRange(menu.Options.Select(o => o.Command).Where(c => c.Value is >= 10 and <= 21));
        return [.. modes
            .Where(m => menu.Find(m)?.Arguments.Contains(button) == true)
            .Select(m => new BattleCommand(ModeLabel(m.Value), new FigtSelChoice(m, button),
                Name: m == FigtSelCommand.Direct ? MeleeName : null))];
    }

    /// <summary>
    /// The context menu of <paramref name="place"/>'s card: the acting unit's Defend, Wait, the items without a
    /// target (the capsules, the Fog Card), Auto-Atk, Auto-Def and Retreat (Repel in debug mode); another own
    /// unit's standing orders Manual, Auto-Atk and Auto-Def (50-52, <paramref name="order"/> marked); an enemy's
    /// none. Only what the core allows. Info (53/54) is not offered (owner, 2026-10-15): every card shows its stats
    /// under the portrait (<see cref="BattleCardStats"/>).
    /// </summary>
    /// <param name="place">The card's place (it holds a unit).</param>
    /// <param name="order">The unit's standing order now (own units).</param>
    public IReadOnlyList<BattleCommand> ContextMenu(BattlePlace place, StandingOrder? order = null)
    {
        var slot = (Sys3Int)place.Slot;
        var items = new List<BattleCommand>();
        void Add(int line, ushort command, Sys3Int argument, bool current = false) =>
            items.Add(new BattleCommand(new ScriptRef(Sel, line), new FigtSelChoice(command, argument), current));

        if (place.Side == BattleSide.Enemy)
            return [];
        if (place == Acting)
        {
            Add(249, FigtSelCommand.Defend, NoButton);
            Add(253, FigtSelCommand.Wait, NoButton);
            foreach (var item in (ReadOnlySpan<ushort>)[FigtSelCommand.Amoeba, FigtSelCommand.JagiJagi, FigtSelCommand.FogCard])
                items.Add(new BattleCommand(ModeLabel(item), new FigtSelChoice(item, NoButton)));
            Add(257, FigtSelCommand.AutoAttack, NoButton);
            Add(261, FigtSelCommand.AutoDefend, NoButton);
            Add(271, FigtSelCommand.Retreat, NoButton);
            Add(276, FigtSelCommand.Repel, NoButton);
        }
        else
        {
            Add(323, FigtSelCommand.OrderManual, slot, order == StandingOrder.Manual);
            Add(327, FigtSelCommand.OrderAttack, slot, order == StandingOrder.AutoAttack);
            Add(331, FigtSelCommand.OrderDefend, slot, order == StandingOrder.AutoDefend);
        }
        return [.. items.Where(i => menu.Allows(i.Choice))];
    }

    /// <summary>A mode's or an item's label: the command popup's entry (<c>L_00bc0</c>), the Cheater Beam's its Cheat line.</summary>
    private static ScriptRef ModeLabel(int mode) =>
        new(Sel, mode == FigtSelCommand.CheaterBeam ? 244 : FigtSelScreen.ModeEntry(mode).Line);
}
