using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>A side of the battle.</summary>
public enum BattleSide
{
    /// <summary>The player's units (<c>Bt_PlayerSlot1-4</c>).</summary>
    Player,

    /// <summary>The enemy's commanders (<c>Bt_EnemySlot1-4</c>).</summary>
    Enemy,
}

/// <summary>A player slot's standing order (<c>Bt_PlayerOrder1-4</c>).</summary>
public enum StandingOrder
{
    /// <summary>0: the player is asked each turn.</summary>
    Manual = 0,

    /// <summary>1: the unit keeps attacking.</summary>
    AutoAttack = 1,

    /// <summary>2: the unit keeps defending.</summary>
    AutoDefend = 2,
}

/// <summary>A place on one side: slot 1-4 (1-2 the front row, 3-4 the back row).</summary>
public readonly record struct BattlePlace(BattleSide Side, int Slot)
{
    /// <inheritdoc/>
    public override string ToString() => (Side == BattleSide.Player ? "P" : "E") + Slot;
}

/// <summary>The eight slots' records and round states at one moment (player 1-4, then enemy 1-4).</summary>
public sealed class SlotSnapshot
{
    private readonly int[] _records = new int[8];
    private readonly int[] _states = new int[8];

    /// <summary>The eight places in the script's scan order.</summary>
    public static IReadOnlyList<BattlePlace> Places { get; } =
        [.. new[] { BattleSide.Player, BattleSide.Enemy }.SelectMany(side => Enumerable.Range(1, 4).Select(slot => new BattlePlace(side, slot)))];

    /// <summary>The slots as <paramref name="s"/> has them now.</summary>
    public static SlotSnapshot Of(GameState s)
    {
        var snapshot = new SlotSnapshot();
        for (int i = 0; i < 8; i++)
        {
            snapshot._records[i] = BattleBoard.Record(s, Places[i]);
            snapshot._states[i] = BattleBoard.StateOf(s, Places[i]);
        }
        return snapshot;
    }

    /// <summary>The record in a place.</summary>
    public int Record(BattlePlace place) => _records[Index(place)];

    /// <summary>A place's round state (<c>Bt_PSlot1-4State</c> / <c>Bt_ESlot1-4State</c>).</summary>
    public int State(BattlePlace place) => _states[Index(place)];

    private static int Index(BattlePlace p) => (p.Side == BattleSide.Player ? 0 : 4) + p.Slot - 1;
}

/// <summary>
/// One unit's card on our battle screen (docs/ui/battle-screen.md, "The layout"): the record in a
/// slot as GameState has it now — a player unit (<c>Unit_*</c>) or an enemy commander (<c>Cmdr_*</c>).
/// </summary>
/// <param name="Place">The side and slot.</param>
/// <param name="Record">The unit's or commander's record number.</param>
/// <param name="Name">The record's first string (scenes may rename it).</param>
/// <param name="TroopType">The record's second string (the troop type's name).</param>
/// <param name="FaceCg">The face CG (<c>Unit_FaceCg</c> / <c>Cmdr_FaceCg</c>; 0 none).</param>
/// <param name="StandIn">For a commander without a face, its battle sprite (<see cref="UnitFaces.Commanders"/>); else 0.</param>
/// <param name="Troops">The troops now.</param>
/// <param name="MaxTroops">The troops at full strength.</param>
/// <param name="Hp">The commander's own HP now.</param>
/// <param name="MaxHp">Its HP at full health.</param>
/// <param name="Order">The standing order (player units only).</param>
/// <param name="Focus">The focus a Wait has built (player units only, <c>Bt_PSlot1-4Focus</c>).</param>
/// <param name="Acted">Whether the unit has acted this round (its slot state is not 0).</param>
/// <param name="Defending">Whether it defends this round (player state 2-21, enemy state 20).</param>
public sealed record BattleCard(
    BattlePlace Place, int Record, string Name, string TroopType, int FaceCg, int StandIn,
    int Troops, int MaxTroops, int Hp, int MaxHp, StandingOrder? Order, int Focus, bool Acted, bool Defending)
{
    /// <summary>The CG our screens cut for the card: the face, else the stand-in (0: none).</summary>
    public int Picture => FaceCg != 0 ? FaceCg : StandIn;
}

/// <summary>
/// The battle as our own screen shows it, read from GameState: the cards (<see cref="Cards"/>) and who
/// acts next this round (<see cref="Remaining"/>, Kichikuou's rule). Engine-independent; reads only.
/// </summary>
public static class BattleBoard
{
    /// <summary>The slot state of an empty slot at the round's start (<c>FIGT.ADV:300-313</c>).</summary>
    private const int EmptyState = 100;

    /// <summary>The record in a slot (0: empty).</summary>
    public static int Record(GameState s, BattlePlace place) => Slot(s, place).Value;

    /// <summary>A place's round state (0 to act, 1 acted, 2-21 / 20 defending, 100 empty at the round's start).</summary>
    public static int StateOf(GameState s, BattlePlace place) => SlotState(s, place).Value;

    /// <summary>The four places of <paramref name="side"/>, slot 1 first; an empty slot is null.</summary>
    public static IReadOnlyList<BattleCard?> Cards(GameState s, BattleSide side) =>
        [.. Enumerable.Range(1, 4).Select(slot => Card(s, new BattlePlace(side, slot)))];

    /// <summary>The card of a place, or null when the slot is empty.</summary>
    public static BattleCard? Card(GameState s, BattlePlace place)
    {
        int record = Record(s, place);
        if (record == 0)
            return null;
        int state = SlotState(s, place).Value;
        bool acted = state != 0 && state != EmptyState;
        if (place.Side == BattleSide.Player)
        {
            var u = s.Units[record];
            int strings = (s.Unit_StrBase + (Sys3Int)((record - 1) * 3)).Value;
            return new BattleCard(place, record, DeploymentTable.StringAt(s, strings).Trim(), DeploymentTable.StringAt(s, strings + 1).Trim(),
                u.FaceCg.Value, 0, u.Troops.Value, u.MaxTroops.Value, u.HP.Value, u.MaxHP.Value,
                (StandingOrder)Math.Min((int)Order(s, place.Slot).Value, 2), Focus(s, place.Slot).Value,
                acted, state is >= 2 and <= 21);
        }
        var c = s.Cmdrs[record];
        int cs = (s.Cmdr_StrBase + (Sys3Int)((record - 1) * 3)).Value;
        int face = c.FaceCg.Value;
        int standIn = face != 0 ? 0 : c.CgAlone != 0 ? c.CgAlone.Value : c.CgFull.Value;
        return new BattleCard(place, record, DeploymentTable.StringAt(s, cs).Trim(), DeploymentTable.StringAt(s, cs + 1).Trim(),
            face, standIn, c.Troops.Value, c.MaxTroops.Value, c.HP.Value, c.MaxHP.Value, null, 0, acted, state == 20);
    }

    /// <summary>
    /// The places still to act this round, in the order <c>L_007ca</c> (<c>FIGT.ADV:320-446</c>, 01-flow.md
    /// §6) would pick them now: fewest troops first, ties to the lower slot and the player. The order is
    /// re-evaluated before every action, so damage changes it; a unit that has acted (or defends) is
    /// not listed. Slot 1 is taken without the comparison against the 65535 sentinel (Q-17) — the same
    /// order but for a unit of exactly 65535 troops elsewhere, which is never picked.
    /// </summary>
    public static IReadOnlyList<BattlePlace> Remaining(GameState s)
    {
        var waiting = new List<(BattlePlace Place, int Troops)>();
        foreach (var side in (ReadOnlySpan<BattleSide>)[BattleSide.Player, BattleSide.Enemy])
        {
            for (int slot = 1; slot <= 4; slot++)
            {
                var place = new BattlePlace(side, slot);
                int record = Record(s, place);
                if (record == 0 || SlotState(s, place) != 0)
                    continue;
                int troops = side == BattleSide.Player ? s.Units[record].Troops.Value : s.Cmdrs[record].Troops.Value;
                bool sentinel = troops == 65535 && !(side == BattleSide.Player && slot == 1);
                if (!sentinel)
                    waiting.Add((place, troops));
            }
        }
        // A stable sort keeps the script's scan order (player 1-4, enemy 1-4) among equal troops.
        return [.. waiting.OrderBy(w => w.Troops).Select(w => w.Place)];
    }

    /// <summary>
    /// The place whose turn it is, from GameState: during a player turn its slot (<c>Bt_PlayerActSlot</c>),
    /// else the enemy slot that acted last (<c>Bt_EnemyActSlot</c>, whose state says it has acted) — or
    /// null between rounds. <see cref="RoundOrder"/> follows the turns from the events instead.
    /// </summary>
    public static BattlePlace? Acting(GameState s)
    {
        int player = s.Bt_PlayerActSlot.Value;
        if (player is >= 1 and <= 4 && Record(s, new BattlePlace(BattleSide.Player, player)) != 0)
            return new BattlePlace(BattleSide.Player, player);
        int enemy = s.Bt_EnemyActSlot.Value;
        var place = new BattlePlace(BattleSide.Enemy, enemy);
        return enemy is >= 1 and <= 4 && Record(s, place) != 0 && SlotState(s, place) is { Value: not 0 and not EmptyState } ? place : null;
    }

    /// <summary>The place a slot panel event names: panels 1-4 are the player's slots, 5-8 the enemy's.</summary>
    public static BattlePlace? PanelPlace(int panel) => panel switch
    {
        >= 1 and <= 4 => new BattlePlace(BattleSide.Player, panel),
        >= 5 and <= 8 => new BattlePlace(BattleSide.Enemy, panel - 4),
        _ => null,
    };

    /// <summary>A place's panel (1-8).</summary>
    public static int Panel(BattlePlace place) => place.Side == BattleSide.Player ? place.Slot : place.Slot + 4;

    private static Sys3Int Slot(GameState s, BattlePlace p) => (p.Side, p.Slot) switch
    {
        (BattleSide.Player, 1) => s.Bt_PlayerSlot1,
        (BattleSide.Player, 2) => s.Bt_PlayerSlot2,
        (BattleSide.Player, 3) => s.Bt_PlayerSlot3,
        (BattleSide.Player, 4) => s.Bt_PlayerSlot4,
        (BattleSide.Enemy, 1) => s.Bt_EnemySlot1,
        (BattleSide.Enemy, 2) => s.Bt_EnemySlot2,
        (BattleSide.Enemy, 3) => s.Bt_EnemySlot3,
        (BattleSide.Enemy, 4) => s.Bt_EnemySlot4,
        _ => Sys3Int.Zero,
    };

    private static Sys3Int SlotState(GameState s, BattlePlace p) => (p.Side, p.Slot) switch
    {
        (BattleSide.Player, 1) => s.Bt_PSlot1State,
        (BattleSide.Player, 2) => s.Bt_PSlot2State,
        (BattleSide.Player, 3) => s.Bt_PSlot3State,
        (BattleSide.Player, 4) => s.Bt_PSlot4State,
        (BattleSide.Enemy, 1) => s.Bt_ESlot1State,
        (BattleSide.Enemy, 2) => s.Bt_ESlot2State,
        (BattleSide.Enemy, 3) => s.Bt_ESlot3State,
        (BattleSide.Enemy, 4) => s.Bt_ESlot4State,
        _ => Sys3Int.Zero,
    };

    private static Sys3Int Order(GameState s, int slot) => slot switch
    {
        1 => s.Bt_PlayerOrder1,
        2 => s.Bt_PlayerOrder2,
        3 => s.Bt_PlayerOrder3,
        _ => s.Bt_PlayerOrder4,
    };

    private static Sys3Int Focus(GameState s, int slot) => slot switch
    {
        1 => s.Bt_PSlot1Focus,
        2 => s.Bt_PSlot2Focus,
        3 => s.Bt_PSlot3Focus,
        _ => s.Bt_PSlot4Focus,
    };
}

/// <summary>The state of a chip in the round's order strip.</summary>
public enum ChipState
{
    /// <summary>Has acted this round.</summary>
    Acted,

    /// <summary>Acts now.</summary>
    Acting,

    /// <summary>Still to act, in the order shown.</summary>
    Waiting,
}

/// <summary>A chip of the round's order: who, where now (0 when no longer in a slot) and whether it has acted.</summary>
public sealed record OrderChip(BattleSide Side, int Record, int Slot, string Name, ChipState State);

/// <summary>
/// The round's order as our screen shows it (docs/ui/battle-screen.md, "The layout"): the units that
/// have acted in the order they did, the one acting, then the rest in Kichikuou's order
/// (<see cref="BattleBoard.Remaining"/>). The turns are followed from the battle's events
/// (<see cref="Observe"/>): a round header starts a round, a slot whose state the core's choice set
/// (<see cref="TurnStarted"/>) names the turn's actor. Also keeps the battle's rounds for the gauge.
/// </summary>
public sealed class RoundOrder
{
    private readonly List<(BattleSide Side, int Record)> _acted = [];
    private SlotSnapshot? _slots;

    /// <summary>The place acting now, or null (before the round's first turn).</summary>
    public BattlePlace? Acting { get; private set; }

    /// <summary>The record acting now (0: nobody).</summary>
    public int ActingRecord { get; private set; }

    /// <summary>The rounds the battle had at its first round header (<c>Bt_RoundsLeft</c> then; 0 before).</summary>
    public int Rounds { get; private set; }

    /// <summary>The round now, 1 at the battle's first round header (0 before).</summary>
    public int Round { get; private set; }

    /// <summary>Forgets the battle (a new one begins).</summary>
    public void Reset()
    {
        _acted.Clear();
        Acting = null;
        ActingRecord = 0;
        Rounds = 0;
        Round = 0;
        _slots = null;
    }

    /// <summary>
    /// Follows one battle event, with the state as it is when the event is shown: a
    /// <see cref="RoundHeaderEvent"/> starts a round; a turn starts where the core's choice
    /// (<c>L_007ca</c>) has marked a slot as acted since the last event (<see cref="TurnStarted"/>).
    /// No event comes between the choice and the turn's first drawing, so every turn is seen.
    /// </summary>
    public void Observe(ScriptEvent scriptEvent, GameState s)
    {
        var now = SlotSnapshot.Of(s);
        if (scriptEvent is RoundHeaderEvent header)
        {
            if (Rounds == 0)
                Rounds = header.RoundsLeft.Value;
            Round++;
            _acted.Clear();
            Acting = null;
            ActingRecord = 0;
        }
        if (_slots is { } before && TurnStarted(before, now) is { } place)
        {
            if (Acting is { } acting && ActingRecord != 0)
                _acted.Add((acting.Side, ActingRecord));
            Acting = place;
            ActingRecord = now.Record(place);
        }
        _slots = now;
    }

    /// <summary>
    /// The place the core chose between two looks at the state: its slot state went from 0 (to act)
    /// to 1 with the same record in it (the choice sets 1, <c>FIGT.ADV:392</c> / <c>:424</c>). A slot
    /// compaction moves another record in, a round's start sets 0: neither is a choice.
    /// </summary>
    public static BattlePlace? TurnStarted(SlotSnapshot before, SlotSnapshot now)
    {
        foreach (var place in SlotSnapshot.Places)
        {
            int record = now.Record(place);
            if (record != 0 && record == before.Record(place) && before.State(place) == 0 && now.State(place) == 1)
                return place;
        }
        return null;
    }

    /// <summary>The strip: the acted (in turn order), the acting, the waiting (Kichikuou's order now).</summary>
    public IReadOnlyList<OrderChip> Chips(GameState s)
    {
        var chips = new List<OrderChip>();
        foreach (var (side, record) in _acted)
            chips.Add(Chip(s, side, record, ChipState.Acted));
        if (Acting is { } acting && ActingRecord != 0)
            chips.Add(Chip(s, acting.Side, ActingRecord, ChipState.Acting));
        foreach (var place in BattleBoard.Remaining(s))
        {
            int record = BattleBoard.Record(s, place);
            if (Acting is { } a && a.Side == place.Side && record == ActingRecord)
                continue;                                               // its state not set yet (the turn's start)
            chips.Add(Chip(s, place.Side, record, ChipState.Waiting));
        }
        return chips;
    }

    private static OrderChip Chip(GameState s, BattleSide side, int record, ChipState state)
    {
        int slot = 0;
        for (int i = 1; i <= 4 && slot == 0; i++)
        {
            if (BattleBoard.Record(s, new BattlePlace(side, i)) == record)
                slot = i;
        }
        var strings = side == BattleSide.Player
            ? s.Unit_StrBase + (Sys3Int)((record - 1) * 3)
            : s.Cmdr_StrBase + (Sys3Int)((record - 1) * 3);
        return new OrderChip(side, record, slot, DeploymentTable.StringAt(s, strings.Value).Trim(), state);
    }
}
