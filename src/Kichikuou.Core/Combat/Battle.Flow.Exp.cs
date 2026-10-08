using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

// EXP and level-up at the end of every battle, FIGT.ADV:2977-3160 (docs/battle/01-flow.md §11.1-§11.2).
public sealed partial class Battle
{
    /// <summary>
    /// <c>L_04a2c</c>, <c>FIGT.ADV:2977-3044</c> (01 §11.1): EXP <c>5 + RND(15)</c> [R15]
    /// (<c>RND(50)</c> [R16] with Rebecca's bonus; 1 in dungeons; a third on a loss) in
    /// <c>Tmp0001</c>, the garrison hit counter, a level-up check for every occupied slot and the
    /// casualty history.
    /// </summary>
    internal async Task AwardExpAsync()
    {
        Rnd(15);
        State.Tmp0001 = 5 + State.RetVal;
        if (State.Bt_ExpBonusNext != 0)
        {
            Rnd(50);
            State.Tmp0001 = State.RetVal;
            State.Bt_ExpBonusNext = 0;
        }
        // QUIRK: Q-21 — the draws above are made even when these override them.
        if (State.Bt_Kind == 3)
            State.Tmp0001 = 1;
        if (State.Bt_Result == 1)
            State.Tmp0001 = State.Tmp0001 / 3;
        await FlowWindowAsync(143);
        // :2992-3001 picks the heading line, then one text run with the numbers continues at :3003.
        await FlowTextAsync(FigtFile, State.StoryStage != 0 ? 2994 : 2999);
        await FlowValueTextAsync(FigtFile, 3003, State.Bt_PlayerLosses, State.Bt_EnemyLosses, State.Tmp0001);
        await FlowWaitKeyAsync();
        await FlowWindowAsync(10);
        if (State.Bt_GarrisonProv == State.Prov_Cur && State.Bt_EnemyLosses > 200)
            State.Bt_GarrisonHits = State.Bt_GarrisonHits + 1;
        for (int i = 1; i <= 4; i++)
        {
            if (PSlot(i) != 0)
            {
                State.Unit_Cur = PSlot(i);
                await LevelUpAsync();
            }
        }
        State.Hist_Cur = 0;
        State.History[State.Hist_Cur].Casualties = State.History[State.Hist_Cur].Casualties + State.Bt_PlayerLosses;
    }

    /// <summary>
    /// <c>L_04bb7</c>, <c>FIGT.ADV:3045-3160</c> (01 §11.2): <c>Unit_Cur</c> gains <c>Tmp0001</c>
    /// EXP (double with the Happiness Ring); at <c>100 + 30 × Level</c> it levels up once (EXP
    /// reset, +1 attack, <c>RND(5)</c> [R17] recovery), then Mill gains 50 troops, a Popularity
    /// Staff holder gains troops, everyone else rolls <c>RND(12 + Level)</c> [R18] for a modifier
    /// raised on <c>RND(3)</c> [R19] above its value.
    /// </summary>
    internal async Task LevelUpAsync()
    {
        Unit.Exp = Unit.Exp + State.Tmp0001;
        if (Unit.Item == 29)
            Unit.Exp = Unit.Exp + State.Tmp0001;
        if (Unit.Exp < 100 + Unit.Level * 30)
            return;
        // QUIRK: Q-21 — surplus EXP is discarded; at most one level per battle.
        Unit.Exp = 0;
        Unit.Attacks = Unit.Attacks + 1;
        Unit.Level = Unit.Level + 1;
        if (Unit.Recovery != 0)
        {
            Rnd(5);
            Unit.Recovery = Unit.Recovery + State.RetVal;
        }
        FlowCopyString(1, UnitNameString(State.Unit_Cur));
        State.CallArg = 0;
        State.RetVal = 104;
        await FlowSoundAsync(0);
        await FlowWindowAsync(143);
        await FlowTextAsync(FigtFile, 3065);
        await FlowWaitKeyAsync();
        await FlowTextAsync(FigtFile, 3071);
        if (State.Unit_Cur == 25)
        {
            Unit.MaxTroops = Unit.MaxTroops + 50;
            Unit.Recovery = Unit.MaxTroops;
            await FlowTextAsync(FigtFile, 3077);
            await FlowWaitKeyAsync();
            await FlowWindowAsync(10);
            return;
        }
        if (Unit.Item == 3 && Unit.MaxTroops != 0)
        {
            State.Tmp0005 = Unit.MaxTroops / (10 + Unit.Level * 2);
            Unit.MaxTroops = Unit.MaxTroops + State.Tmp0005;
            await FlowValueTextAsync(FigtFile, 3088, State.Tmp0005);
            await FlowWaitKeyAsync();
            await FlowWindowAsync(10);
            return;
        }
        Rnd(12 + Unit.Level);
        // Each case draws RND(3) and jumps to L_04e6c, so the chain is an if-else on R18.
        if (State.RetVal == 1)
            await RaiseModifierAsync(() => Unit.OffAtkMod, v => Unit.OffAtkMod = v, 3106);
        else if (State.RetVal == 2)
            await RaiseModifierAsync(() => Unit.DefAtkMod, v => Unit.DefAtkMod = v, 3115);
        else if (State.RetVal == 3)
            await RaiseModifierAsync(() => Unit.DgnAtkMod, v => Unit.DgnAtkMod = v, 3124);
        else if (State.RetVal == 4)
            await RaiseModifierAsync(() => Unit.OffDefMod, v => Unit.OffDefMod = v, 3133);
        else if (State.RetVal == 5)
            await RaiseModifierAsync(() => Unit.DefDefMod, v => Unit.DefDefMod = v, 3142);
        else if (State.RetVal == 6)
            await RaiseModifierAsync(() => Unit.DgnDefMod, v => Unit.DgnDefMod = v, 3151);
        // L_04e6c (:3155-3160). QUIRK: Q-13 — " UP" is shown even when nothing rose.
        await FlowTextAsync(FigtFile, 3157);
        await FlowWaitKeyAsync();
        await FlowWindowAsync(10);
    }

    /// <summary>
    /// One case of the modifier roll (<c>FIGT.ADV:3101-3154</c>): <c>RND(3)</c> [R19]; when
    /// <c>RetVal - mod ≠ 0</c> (saturating: <c>RetVal &gt; mod</c>) the modifier rises by 1.
    /// The modifier is read and written through the current <c>Unit_Cur</c>.
    /// </summary>
    private async Task RaiseModifierAsync(Func<Sys3Int> get, Action<Sys3Int> set, int line)
    {
        Rnd(3);
        if (State.RetVal - get() != 0)
        {
            set(get() + 1);
            await FlowTextAsync(FigtFile, line);
        }
    }
}
