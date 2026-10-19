using System.Globalization;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>The orders our Subordinates screen gives (docs/ui/subordinates.md).</summary>
public enum SubordinateOrderKind
{
    /// <summary>Replenish Troops: the troops back to the maximum.</summary>
    Replenish,

    /// <summary>Add New Troops at one of Add More's steps.</summary>
    AddTroops,

    /// <summary>Talk: the unit's talk scene.</summary>
    Talk,
}

/// <summary>
/// A button of the orders' panel: whether it can be clicked, the men and the price (×100K) it shows,
/// and, greyed, why ("used this week", "deployed this week", "full", "over the budget", …).
/// </summary>
public sealed record SubordinateButton(bool Enabled, int Men, int Price, string Note);

/// <summary>
/// The orders' panel for the chosen unit: its name, face and status CG, the budget (×100K), Replenish,
/// whether new troops are offered and Add More's steps (the first is the offer, each next one twice the
/// men for three times the price while the price is under 10000), and Talk.
/// </summary>
public sealed record SubordinatePanel(
    int Unit, string Name, int FaceCg, int PortraitCg, int Gold,
    SubordinateButton Replenish, bool AddOffered, IReadOnlyList<SubordinateButton> Steps, SubordinateButton Talk);

/// <summary>
/// What a troop order did, for the plate in the middle of the screen (docs/ui/subordinates.md, §6, R4/R5): the unit
/// (name, troop type, face), its troops and maximum before and after, the cost and the budget left (both ×100K).
/// </summary>
public sealed record SubordinateResult(
    SubordinateOrderKind Kind, string Name, string TroopType, int FaceCg,
    int TroopsBefore, int Troops, int MaxBefore, int MaxTroops, int Cost, int Gold)
{
    /// <summary>The plate's title.</summary>
    public string Title => Kind == SubordinateOrderKind.Replenish ? "Troops Replenished" : "New Troops";

    /// <summary>"Haurein · Leazas Soldiers".</summary>
    public string Who => TroopType.Length == 0 ? Name : $"{Name} · {TroopType}";

    /// <summary>The troops before and after: "209 → 1400" (Replenish), "1000/1000 → 1050/1050" (new troops, the maximum raised).</summary>
    public string TroopsLine => Kind == SubordinateOrderKind.Replenish
        ? $"{TroopsBefore} → {Troops}"
        : $"{TroopsBefore}/{MaxBefore} → {Troops}/{MaxTroops}";

    /// <summary>The men gained: "+1191" (a unit above its maximum loses some, CMD-20: "−50").</summary>
    public string Gain => Signed(Troops - TroopsBefore);

    /// <summary>"−2,300 K".</summary>
    public string CostLine => $"−{Thousands(Cost)} K";

    /// <summary>"114,100 K".</summary>
    public string BudgetLine => $"{Thousands(Gold)} K";

    private static string Thousands(int hundreds) => (hundreds * 100).ToString("N0", CultureInfo.InvariantCulture);

    private static string Signed(int n) => n < 0 ? $"−{-n}" : $"+{n}";
}

/// <summary>
/// Our own Subordinates screen over <c>Sﾌﾞｶ</c> and <c>Cﾌﾞｶ</c>'s troop menu (docs/ui/subordinates.md): the
/// table's rows and columns and the orders' panel, from GameState and <see cref="SubordinatesCommand"/>'s
/// figures. It decides nothing: an order is a <see cref="SubordinateOrder"/>, the inputs the script expects.
/// </summary>
public static class SubordinatesScreen
{
    /// <summary>
    /// The table's columns (the owner's choice, F1): every value of the status page but the modifiers, and the
    /// war expense of sending the unit now after Replenish's price (the owner's choice, 2026-10-20).
    /// </summary>
    public static IReadOnlyList<DeploymentColumnInfo> Columns { get; } =
    [
        new(DeploymentColumn.Number, "№", "Unit number"),
        new(DeploymentColumn.Name, "Name", "Name", IsText: true),
        new(DeploymentColumn.TroopType, "Type", "Troop type", IsText: true),
        new(DeploymentColumn.Troops, "Troops", "Troops / maximum"),
        new(DeploymentColumn.Refill, "Refill", "Replenish's price (GOLD)"),
        new(DeploymentColumn.Cost, "Cost", "War expense (GOLD)"),
        new(DeploymentColumn.TroopAtk, "T.Atk", "Troop attack"),
        new(DeploymentColumn.TroopDef, "T.Def", "Troop defense"),
        new(DeploymentColumn.Strategy, "Str%", "Strategy success chance"),
        new(DeploymentColumn.Hp, "HP", "Commander HP / maximum"),
        new(DeploymentColumn.Level, "Lv", "Level"),
        new(DeploymentColumn.Exp, "EXP", "Experience / next level"),
        new(DeploymentColumn.Atk, "Atk", "Commander attack"),
        new(DeploymentColumn.Def, "Def", "Commander defense"),
        new(DeploymentColumn.Attacks, "Act", "Attacks"),
        new(DeploymentColumn.Magic, "Mag", "Magic"),
        new(DeploymentColumn.Item, "Item", "Item", IsText: true),
    ];

    /// <summary>The listed units (status not 0), as <c>Sﾌﾞｶ</c> lists them.</summary>
    public static IReadOnlyList<DeploymentRow> Rows(GameState state) => DeploymentTable.Rows(state);

    /// <summary>The row chosen when the screen opens: the first unit that can act (status 1), else the first listed, else 0.</summary>
    public static int FirstChoice(GameState state)
    {
        var rows = Rows(state);
        return rows.FirstOrDefault(r => state.Units[r.Unit].Status == 1)?.Unit ?? rows.FirstOrDefault()?.Unit ?? 0;
    }

    /// <summary>
    /// The orders' panel for <paramref name="unit"/>: what <c>Cﾌﾞｶ</c> lets it do now and at what price.
    /// Greyed: everything when the week's actions are spent (<c>Ui_CmdSubordAvail</c> 0: the view-only
    /// picker) or the unit cannot act (status not 1: its pick opens the status page); the troop orders in
    /// the prologue (no troop menu) and once the week's troop order is spent; Replenish when the unit is
    /// full (refused) or the budget short; a step dearer than the budget; Talk once spent.
    /// </summary>
    public static SubordinatePanel Panel(GameState state, int unit)
    {
        var u = state.Units[unit];
        int gold = state.Gold.Value;
        string? all = state.Ui_CmdSubordAvail == 0 ? "used this week"
            : u.Status == 2 ? "deployed this week"
            : u.Status != 1 ? "cannot act"
            : null;
        string? troops = all ?? (state.StoryStage == 0 ? "after the prologue"
            : state.CmdAvail_SubTroops != 1 ? "used this week"
            : null);

        var (deficiency, refill) = SubordinatesCommand.Refill(u);
        string? replenishOff = troops ?? (u.Troops == u.MaxTroops ? "full"
            : state.Gold < refill ? "over the budget"
            : null);
        var replenish = new SubordinateButton(replenishOff is null, deficiency.Value, refill.Value, replenishOff ?? "");

        var steps = new List<SubordinateButton>();
        var (price, men) = SubordinatesCommand.Offer(state, (Sys3Int)unit);
        bool offered = price != 0;
        while (offered)
        {
            string? off = troops ?? (state.Gold < price ? "over the budget" : null);
            steps.Add(new SubordinateButton(off is null, men.Value, price.Value, off ?? ""));
            if (!SubordinatesCommand.CanAddMore(price))
                break;
            (price, men) = SubordinatesCommand.AddMore(price, men);
        }

        string? talkOff = all ?? (state.StoryStage != 0 && state.CmdAvail_SubTalk != 1 ? "used this week" : null);
        var talk = new SubordinateButton(talkOff is null, 0, 0, talkOff ?? "");
        return new SubordinatePanel(unit, DeploymentTable.UnitName(state, unit), u.FaceCg.Value, u.PortraitCg.Value, gold,
            replenish, offered, steps, talk);
    }

    /// <summary>
    /// Our question before a troop order, Replenish or a step of Add New Troops (the owner's C3, docs/ui/subordinates.md,
    /// §6; the button it asks about shows the men and the price). It stands for the original's Do / Don't, which the
    /// order then answers with Do.
    /// </summary>
    public static IReadOnlyList<string> Question { get; } = ["Are you sure?"];
}

/// <summary>
/// One order of our Subordinates screen as the inputs <c>Cﾌﾞｶ</c> expects from its player
/// (docs/ui/subordinates.md, §3): a click on the unit's cell, the troop menu's item, Do (Replenish) or Add
/// More as many times as the step asks and Do It (Add New Troops); every key wait on the way answered at
/// once. A troop order ends the command; on the map that follows, a right click and COMMAND's Subordinates
/// open it again, and the order is over when <c>Sﾌﾞｶ</c> opens. Talk is over after Rance's thought (the
/// talk scene plays as any scene); in the prologue as soon as the picker closes (the pick plays the scene).
/// </summary>
public sealed class SubordinateOrder
{
    /// <summary>Where the order is.</summary>
    private enum Phase
    {
        Picking,        // the cell's click on its way; then the troop menu
        Confirming,     // the troop menu answered: Replenish's Do / Don't, Add's Do It / Add More / Don't
        Finishing,      // the order done: the command's windows, up to COMMAND's map wait
        Reopening,      // the map's right click on its way: COMMAND's menu, then Sﾌﾞｶ
        Thought,        // Talk: Rance's thought, the one key wait before the scene
        Done,
    }

    /// <summary><c>Cﾌﾞｶ.ADV</c>'s troop menu: Info, ○Replenish Troops, ○Add New Troops, ○Talk, Do Nothing.</summary>
    private const int MenuReplenish = 812, MenuAdd = 817, MenuTalk = 836, MenuNothing = 847;

    /// <summary>Replenish's Do (<c>:1026</c>); Add's Do It and Add More (<c>:904</c>, <c>:909</c>).</summary>
    private const int ReplenishDo = 1026, AddDoIt = 904, AddMore = 909;

    /// <summary>COMMAND's Subordinates item: ◎/○/△ (Leazas), ○/△ (the thieves) — <c>CommandPhase.cs:442-478</c>.</summary>
    private static readonly int[] CommandSubordinates = [270, 276, 283, 199, 205];

    private Phase _phase = Phase.Picking;
    private int _addMoreLeft;
    private readonly int _troops, _maxTroops, _gold;

    private SubordinateOrder(SubordinateOrderKind kind, int unit, int step, GameState state)
    {
        Kind = kind;
        Unit = unit;
        Step = step;
        _addMoreLeft = step;
        var u = state.Units[unit];
        (_troops, _maxTroops, _gold) = (u.Troops.Value, u.MaxTroops.Value, state.Gold.Value);
        Prologue = state.StoryStage == 0;
    }

    /// <summary>The order given.</summary>
    public SubordinateOrderKind Kind { get; }

    /// <summary>The unit it is given to.</summary>
    public int Unit { get; }

    /// <summary>Add New Troops' step (0 = the offer, n = Add More n times).</summary>
    public int Step { get; }

    /// <summary>Whether it was given in the prologue (stage 0: the pick is the talk).</summary>
    public bool Prologue { get; }

    /// <summary>Whether the order still runs: our screen covers the script's windows and answers them.</summary>
    public bool Running => _phase != Phase.Done;

    /// <summary>Whether the troop order went through (the command's result is to be shown).</summary>
    public bool Succeeded { get; private set; }

    /// <summary>
    /// Starts <paramref name="kind"/> for <paramref name="unit"/> (at Add More's <paramref name="step"/>):
    /// queues the click on its cell for the script's pointer.
    /// </summary>
    public static SubordinateOrder Start(GameState state, SubordinateOrderKind kind, int unit, int step, PointerScript pointer)
    {
        var (x, y) = GesturePoints.PickerCell(unit);
        pointer.Click(x, y);
        return new SubordinateOrder(kind, unit, kind == SubordinateOrderKind.AddTroops ? step : 0, state);
    }

    /// <summary>
    /// The choice for a menu the script opens while the order runs (1-based), or null when it is not one
    /// the order answers (the screen then lets the menu be the player's).
    /// </summary>
    public int? Choose(IReadOnlyList<SceneMenuItem> items)
    {
        int At(string file, params int[] lines)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].At.File == file && lines.Contains(items[i].At.Line))
                    return i + 1;
            }
            return 0;
        }
        switch (_phase)
        {
            case Phase.Picking when At("Cﾌﾞｶ.ADV", MenuNothing) > 0:
                int line = Kind switch
                {
                    SubordinateOrderKind.Replenish => MenuReplenish,
                    SubordinateOrderKind.AddTroops => MenuAdd,
                    _ => MenuTalk,
                };
                int choice = At("Cﾌﾞｶ.ADV", line);
                if (choice == 0)
                {
                    _phase = Phase.Done;                        // not offered after all: Do Nothing, back to the picker
                    return At("Cﾌﾞｶ.ADV", MenuNothing);
                }
                _phase = Kind == SubordinateOrderKind.Talk ? Phase.Thought : Phase.Confirming;
                return choice;
            case Phase.Confirming when Kind == SubordinateOrderKind.Replenish && At("Cﾌﾞｶ.ADV", ReplenishDo) > 0:
                _phase = Phase.Finishing;
                Succeeded = true;
                return At("Cﾌﾞｶ.ADV", ReplenishDo);
            case Phase.Confirming when Kind == SubordinateOrderKind.AddTroops && At("Cﾌﾞｶ.ADV", AddDoIt) > 0:
                if (_addMoreLeft > 0 && At("Cﾌﾞｶ.ADV", AddMore) is > 0 and var more)
                {
                    _addMoreLeft--;
                    return more;
                }
                _phase = Phase.Finishing;
                Succeeded = true;
                return At("Cﾌﾞｶ.ADV", AddDoIt);
            case Phase.Confirming or Phase.Finishing when At("Cﾌﾞｶ.ADV", MenuNothing) > 0:
                _phase = Phase.Done;                            // refused (the budget): the troop menu again, Do Nothing
                Succeeded = false;
                return At("Cﾌﾞｶ.ADV", MenuNothing);
            case Phase.Reopening:
                int item = At("COMMAND.ADV", CommandSubordinates);
                if (item == 0)
                    _phase = Phase.Done;                        // no Subordinates in the menu (STR-20): back to the map
                return item;                                    // else over when Sﾌﾞｶ opens
            default:
                return null;
        }
    }

    /// <summary>Whether a key wait now is the order's, answered at once; Rance's thought ends a talk.</summary>
    public bool AnswerKey()
    {
        if (_phase == Phase.Done)
            return false;
        if (_phase == Phase.Thought)
            _phase = Phase.Done;
        return true;
    }

    /// <summary>
    /// A picker opened or closed: <c>Sﾌﾞｶ</c> opening again ends the order (re-opened, or back after a
    /// refusal); in the prologue the pick's close is the talk's start.
    /// </summary>
    public void OnPicker(PickerScreenEvent picker)
    {
        if (picker.Kind != PickerKind.Subordinates)
            return;
        if (picker.Open && _phase is Phase.Reopening or Phase.Finishing or Phase.Confirming)
            _phase = Phase.Done;
        else if (!picker.Open && Prologue && _phase == Phase.Picking)
            _phase = Phase.Done;
    }

    /// <summary>
    /// COMMAND waits on the map: after a troop order the command is opened again — true when the right
    /// click is to be queued. Sill's scene at the command's start (<c>F_SillCow_State</c> 3,
    /// <c>Cﾌﾞｶ.ADV:21-24</c>) cannot play under the reopened screen: the state is 3 only after the
    /// merchant's scene (<c>Gｼｨﾙ</c> 2), and the entry that led to the order already turned it to 4.
    /// </summary>
    public bool OnMapWait()
    {
        if (_phase is Phase.Done or Phase.Reopening or Phase.Thought)
            return false;
        if (_phase != Phase.Finishing || !Succeeded)
        {
            _phase = Phase.Done;
            return false;
        }
        _phase = Phase.Reopening;
        return true;
    }

    /// <summary>What the order did, for the plate (null for a talk or an order that did not go through).</summary>
    public SubordinateResult? Result(GameState state)
    {
        if (!Succeeded || Kind == SubordinateOrderKind.Talk)
            return null;
        var u = state.Units[Unit];
        return new SubordinateResult(Kind, DeploymentTable.UnitName(state, Unit), DeploymentTable.TroopType(state, Unit),
            u.FaceCg.Value, _troops, u.Troops.Value, _maxTroops, u.MaxTroops.Value, _gold - state.Gold.Value, state.Gold.Value);
    }

    /// <summary>The map's point COMMAND's right click is made at: the top left corner, clear of the map's plates.</summary>
    public static (int X, int Y) RightClickAt => (5, 6);
}
