using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

// Part R1: drawing routines of FIGT.ADV that also change state (01-flow.md §9.4, Q-20, Q-28).
// They are ported for their state writes (cursors, Unit_CurAtkMod/CurDefMod, Tmp0021-0025,
// Ui_MouseX/Y, RetVal, CallArg, string variables) and emit an event for the visual part.
public sealed partial class Battle
{
    /// <summary>
    /// <c>L_03058</c> (<c>FIGT.ADV:1987-1991</c>) — redraws the battlefield: background
    /// (<c>L_0306c</c>), every occupied slot panel (<c>L_030f0</c>) and the screen copy
    /// (<c>L_031f9</c>). Leaves <c>Unit_Cur</c>/<c>Cmdr_Cur</c> on the last occupied player/enemy
    /// slot and rewrites <c>Unit_CurAtkMod/CurDefMod</c> of every drawn player unit.
    /// 01 §9.4, Q-20, Q-27, Q-28.
    /// </summary>
    internal async Task RedrawAsync()
    {
        await DrawBattlefieldBackgroundAsync();
        await DrawSlotPanelsAsync();
        // L_031f9 (2054-2056): CC — presentation only.
    }

    /// <summary>
    /// <c>L_0306c</c> (<c>FIGT.ADV:1992-2009</c>) — the battlefield background; outside dungeons
    /// sets <c>CallArg = 0</c> and <c>Tmp0021/Tmp0022</c> to the map camera's corner (capped).
    /// 01 §9.4.
    /// </summary>
    private async Task DrawBattlefieldBackgroundAsync()
    {
        if (State.Bt_Kind == 3)
        {
            await View.ShowAsync(new BattlefieldBackgroundEvent(0)); // CF fill
            return;
        }

        State.CallArg = 0;
        State.Tmp0021 = State.MapCamX - 320;
        State.Tmp0022 = State.MapCamY - 200;
        if (State.Tmp0021 > 1280)
            State.Tmp0021 = 1280;
        if (State.Tmp0022 > 400)
            State.Tmp0022 = 400;
        CountCgLoad(Prov.BgCg);                                    // :2008 G Prov_BgCg
        await View.ShowAsync(new BattlefieldBackgroundEvent(Prov.BgCg));
    }

    /// <summary>
    /// <c>L_030f0</c> (<c>FIGT.ADV:2010-2053</c>) — selects each occupied slot's record
    /// (<c>Unit_Cur</c> / <c>Cmdr_Cur</c>), sets <c>CallArg</c> to its panel 1-8 and draws it.
    /// 01 §9.4.
    /// </summary>
    private async Task DrawSlotPanelsAsync()
    {
        if (State.Bt_PlayerSlot1 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerSlot1;
            State.CallArg = 1;
            await DrawSlotPanelAsync();
        }

        if (State.Bt_PlayerSlot2 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerSlot2;
            State.CallArg = 2;
            await DrawSlotPanelAsync();
        }

        if (State.Bt_PlayerSlot3 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerSlot3;
            State.CallArg = 3;
            await DrawSlotPanelAsync();
        }

        if (State.Bt_PlayerSlot4 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerSlot4;
            State.CallArg = 4;
            await DrawSlotPanelAsync();
        }

        if (State.Bt_EnemySlot1 != 0)
        {
            State.Cmdr_Cur = State.Bt_EnemySlot1;
            State.CallArg = 5;
            await DrawSlotPanelAsync();
        }

        if (State.Bt_EnemySlot2 != 0)
        {
            State.Cmdr_Cur = State.Bt_EnemySlot2;
            State.CallArg = 6;
            await DrawSlotPanelAsync();
        }

        if (State.Bt_EnemySlot3 != 0)
        {
            State.Cmdr_Cur = State.Bt_EnemySlot3;
            State.CallArg = 7;
            await DrawSlotPanelAsync();
        }

        if (State.Bt_EnemySlot4 != 0)
        {
            State.Cmdr_Cur = State.Bt_EnemySlot4;
            State.CallArg = 8;
            await DrawSlotPanelAsync();
        }
    }

    /// <summary>
    /// <c>L_0320f</c> (<c>FIGT.ADV:2057-2060</c>) — draws panel <c>CallArg</c> (1-8) through
    /// <c>L_03236</c> and copies just that panel to the screen (the highlight is the text colour
    /// the caller set). Same state effects as <c>L_03236</c>. 01 §9.4.
    /// </summary>
    internal async Task HighlightAsync()
    {
        await DrawSlotPanelAsync();
        await View.ShowAsync(new PanelHighlightEvent(State.CallArg));
    }

    /// <summary>
    /// <c>L_03236</c> (<c>FIGT.ADV:2061-2136</c>) — one slot panel <c>CallArg</c> (1-4 player with
    /// <c>Unit_Cur</c>, 5-8 enemy with <c>Cmdr_Cur</c>); returns at once when that slot is empty.
    /// Writes <c>RetVal = (CallArg - 1) * 4</c>, <c>Tmp0021-0024</c> = the panel rectangle,
    /// strings 1/2 (player) or 4/5 (enemy), and for a player panel copies the <c>Bt_Kind</c>
    /// modifiers into <c>Unit_CurAtkMod/CurDefMod</c>. 01 §9.4, Q-20.
    /// </summary>
    private async Task DrawSlotPanelAsync()
    {
        if ((State.CallArg == 1 && State.Bt_PlayerSlot1 == 0) || (State.CallArg == 2 && State.Bt_PlayerSlot2 == 0))
            return;
        if ((State.CallArg == 3 && State.Bt_PlayerSlot3 == 0) || (State.CallArg == 4 && State.Bt_PlayerSlot4 == 0))
            return;
        if ((State.CallArg == 5 && State.Bt_EnemySlot1 == 0) || (State.CallArg == 6 && State.Bt_EnemySlot2 == 0))
            return;
        if ((State.CallArg == 7 && State.Bt_EnemySlot3 == 0) || (State.CallArg == 8 && State.Bt_EnemySlot4 == 0))
            return;

        // #L_05126, 1 selects the table L_0512e; F3 reads its words 0-based.
        State.RetVal = (State.CallArg - 1) * 4;
        State.Tmp0021 = ReadPanelTable(State.RetVal + 0);
        State.Tmp0022 = ReadPanelTable(State.RetVal + 1);
        State.Tmp0023 = ReadPanelTable(State.RetVal + 2);
        State.Tmp0024 = ReadPanelTable(State.RetVal + 3);
        CountCgLoad(836);                                          // :2083 G 836, 40 (the panel)
        if (State.CallArg < 5)
        {
            CopyString(1, UnitString(0));
            CopyString(2, UnitString(1));
            await View.ShowAsync(new SlotPanelEvent(State.CallArg, State.Unit_Cur));
            // QUIRK: 01 Q-20 — drawing a player panel rewrites the unit's active modifiers.
            if (State.Bt_Kind == 1)
            {
                Unit.CurAtkMod = Unit.OffAtkMod;
                Unit.CurDefMod = Unit.OffDefMod;
            }

            if (State.Bt_Kind == 2)
            {
                Unit.CurAtkMod = Unit.DefAtkMod;
                Unit.CurDefMod = Unit.DefDefMod;
            }

            if (State.Bt_Kind == 3)
            {
                Unit.CurAtkMod = Unit.DgnAtkMod;
                Unit.CurDefMod = Unit.DgnDefMod;
            }
        }

        if (State.CallArg > 4)
        {
            CopyString(4, CmdrString(0));
            CopyString(5, CmdrString(1));
            await View.ShowAsync(new SlotPanelEvent(State.CallArg, State.Cmdr_Cur));
        }
    }

    /// <summary>
    /// The inline data at <c>L_0512e</c> (<c>FIGT.ADV:3268</c>): x, y, w, h of the eight battlefield
    /// panels (1-4 player, 5-8 enemy), followed in memory by <c>L_0516e</c> (<c>3270</c>).
    /// </summary>
    private static readonly ushort[] PanelTable =
    [
        12, 8, 182, 84, 12, 108, 182, 84, 12, 208, 182, 84, 12, 308, 182, 84,
        448, 8, 182, 84, 448, 108, 182, 84, 448, 208, 182, 84, 448, 308, 182, 84,
        8, 8, 182, 82, 8, 108, 182, 82, 8, 208, 182, 82, 8, 308, 182, 82,
        440, 8, 182, 82, 440, 108, 182, 82, 440, 208, 182, 82, 440, 308, 182, 82,
    ];

    /// <summary><c>F3, var, index</c> on the panel table selected by <c>#L_05126, 1</c>.</summary>
    private static Sys3Int ReadPanelTable(Sys3Int index) =>
        (int)index < PanelTable.Length
            ? PanelTable[(int)index]
            : throw new InvalidOperationException($"F3 index {index} reads past the panel tables (FIGT.ADV:3268-3270).");

    /// <summary>
    /// <c>L_02c00</c> (<c>FIGT.ADV:1830-1863</c>) — the troop (when non-zero) and HP counters of the
    /// duel window; each number goes through <see cref="DrawCounter"/> (style 0). 02 §4.
    /// </summary>
    private async Task ShowCountersAsync()
    {
        if (Unit.Troops != 0)
            DrawCounter(100, (Sys3Int)0 + 400, 0, Unit.Troops);
        if (Cmdr.Troops != 0)
            DrawCounter(480, (Sys3Int)0 + 400, 0, Cmdr.Troops);
        DrawCounter(0, (Sys3Int)368 + 400, 0, Unit.HP);
        DrawCounter(480, (Sys3Int)368 + 400, 0, Cmdr.HP);
        await View.ShowAsync(new CountersEvent(Unit.Troops, Cmdr.Troops, Unit.HP, Cmdr.HP));
    }

    /// <summary>
    /// <c>L_04ed0</c> (<c>FIGT.ADV:3175-3217</c>) and its copy <c>L_009e4</c>
    /// (<c>FIGTATTK.ADV:353-395</c>) — draws <c>Tmp0023</c> with <c>Tmp0021</c> digits at
    /// <c>Ui_MouseX/Y</c>. State effects: <c>Tmp0023</c> ends 0, <c>Tmp0024</c> = last digit,
    /// <c>Tmp0025</c> = digit sum, <c>Ui_MouseX</c> += 32 per leading digit position.
    /// </summary>
    private void DrawNumber()
    {
        State.Tmp0025 = 0;
        if (State.Tmp0021 > 4)
            NextDigit(10000);
        if (State.Tmp0021 > 3)
            NextDigit(1000);
        if (State.Tmp0021 > 2)
            NextDigit(100);
        if (State.Tmp0021 > 1)
            NextDigit(10);
        State.Tmp0024 = State.Tmp0023 / 1;
        State.Tmp0023 = State.Tmp0023 - State.Tmp0024 * 1;
        State.Tmp0025 = State.Tmp0025 + State.Tmp0024;
    }

    /// <summary>One leading-digit block of <c>L_04ed0</c> (e.g. <c>FIGT.ADV:3177-3185</c>); the <c>CS</c> is presentation.</summary>
    private void NextDigit(Sys3Int unit)
    {
        State.Tmp0024 = State.Tmp0023 / unit;
        State.Tmp0023 = State.Tmp0023 - State.Tmp0024 * unit;
        State.Tmp0025 = State.Tmp0025 + State.Tmp0024;
        State.Ui_MouseX = State.Ui_MouseX + 32;
    }
}
