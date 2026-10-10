using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// The template every enemy nation's file repeats (part N1, <c>docs/strategy/survey-nations.md</c>
/// §2.3): the weekly turns (<c>T*.ADV</c>) and the defender pickers (<c>T*2.ADV</c>) each carry
/// their own copy of the same routines, which differ only in the filler's filter — a priority
/// list in <c>Tmp0001-0009</c>, the picker that fills <c>Bt_EnemyParty1-4</c> from it, the troop
/// sum into <c>CallArg</c>, and the walk over the target provinces that attacks the first one the
/// player holds through <c>FIGT_B</c>. A file overrides <see cref="SkipsCommander"/> with its filter.
/// The captures of non-player provinces differ in their text from file to file and are ported with
/// the files that have them (N2, N3).
/// </summary>
public abstract class NationFile(ScriptContext script, string file, IScenarioSource? scenario = null)
    : CommandFile(script, file, scenario)
{
    /// <summary>
    /// <c>!Tmp0001 : a! … !Tmp0009 : 0!</c> — the priority list of commanders, padded with 0.
    /// </summary>
    protected void SetPriority(params int[] commanders)
    {
        if (commanders.Length > 9)
            throw new ArgumentException("A priority list has at most nine commanders.", nameof(commanders));
        State.Tmp0001 = Entry(0);
        State.Tmp0002 = Entry(1);
        State.Tmp0003 = Entry(2);
        State.Tmp0004 = Entry(3);
        State.Tmp0005 = Entry(4);
        State.Tmp0006 = Entry(5);
        State.Tmp0007 = Entry(6);
        State.Tmp0008 = Entry(7);
        State.Tmp0009 = Entry(8);

        Sys3Int Entry(int i) => i < commanders.Length ? (Sys3Int)commanders[i] : Sys3Int.Zero;
    }

    /// <summary>
    /// The picker (<c>TCﾘｰｻﾞｽ.ADV:119-178</c>, <c>TCﾘｰｻﾞｽ2.ADV:85-144</c>, …): the party is
    /// emptied, then every commander of the priority list who is alive (<c>Cmdr_Status ≠ 0</c>) goes
    /// to the filler, in order; from the fourth entry on it stops as soon as slot 4 is taken.
    /// Leaves <c>Cmdr_Cur</c> at the last entry tested.
    /// </summary>
    protected void PickArmy()
    {
        State.Bt_EnemyParty1 = 0;
        State.Bt_EnemyParty2 = 0;
        State.Bt_EnemyParty3 = 0;
        State.Bt_EnemyParty4 = 0;
        for (int k = 1; k <= 9; k++)
        {
            // QUIRK: NAT-12 — the 0 padding the list names commander record 0.
            State.Cmdr_Cur = Priority(k);
            if (Cmdr.Status != 0)
                Fill();
            if (k >= 4 && State.Bt_EnemyParty4 != 0)
                return;
        }
    }

    /// <summary><c>Tmp000k</c>, entry <paramref name="k"/> (1-9) of the priority list.</summary>
    private Sys3Int Priority(int k) => k switch
    {
        1 => State.Tmp0001,
        2 => State.Tmp0002,
        3 => State.Tmp0003,
        4 => State.Tmp0004,
        5 => State.Tmp0005,
        6 => State.Tmp0006,
        7 => State.Tmp0007,
        8 => State.Tmp0008,
        _ => State.Tmp0009,
    };

    /// <summary>
    /// The filler (<c>TCﾘｰｻﾞｽ.ADV:179-199</c>, <c>TCﾘｰｻﾞｽ2.ADV:145-162</c>, …): <c>Cmdr_Cur</c> into
    /// the first empty party slot, unless the file's filter skips it.
    /// </summary>
    private void Fill()
    {
        if (SkipsCommander())
            return;
        if (State.Bt_EnemyParty1 == 0)
        {
            State.Bt_EnemyParty1 = State.Cmdr_Cur;
            return;
        }
        if (State.Bt_EnemyParty2 == 0)
        {
            State.Bt_EnemyParty2 = State.Cmdr_Cur;
            return;
        }
        if (State.Bt_EnemyParty3 == 0)
        {
            State.Bt_EnemyParty3 = State.Cmdr_Cur;
            return;
        }
        if (State.Bt_EnemyParty4 == 0)
            State.Bt_EnemyParty4 = State.Cmdr_Cur;
    }

    /// <summary>The filler's filter: whether <c>Cmdr_Cur</c> stays out of the party (none by default).</summary>
    protected virtual bool SkipsCommander() => false;

    /// <summary>
    /// The troop sum (<c>TCﾘｰｻﾞｽ.ADV:108-118</c>, <c>TCﾘｰｻﾞｽ2.ADV:74-84</c>, …): the troops of the
    /// four party slots into <c>CallArg</c>; leaves <c>Cmdr_Cur</c> at slot 4.
    /// </summary>
    protected void SumTroops()
    {
        State.CallArg = 0;
        // QUIRK: NAT-12 — an empty slot adds the troops of commander record 0.
        State.Cmdr_Cur = State.Bt_EnemyParty1;
        State.CallArg = State.CallArg + Cmdr.Troops;
        State.Cmdr_Cur = State.Bt_EnemyParty2;
        State.CallArg = State.CallArg + Cmdr.Troops;
        State.Cmdr_Cur = State.Bt_EnemyParty3;
        State.CallArg = State.CallArg + Cmdr.Troops;
        State.Cmdr_Cur = State.Bt_EnemyParty4;
        State.CallArg = State.CallArg + Cmdr.Troops;
    }

    /// <summary>
    /// The target walk (<c>!Prov_Cur : p! {Prov_Owner = 1: %#FIGT_B.ADV: @…:}</c> per province):
    /// the first of <paramref name="provinces"/> the player holds is attacked; true if one was.
    /// Leaves <c>Prov_Cur</c> at the attacked province, or at the last one tested.
    /// </summary>
    protected async Task<bool> AttackFirstAsync(params int[] provinces)
    {
        foreach (int province in provinces)
        {
            State.Prov_Cur = (Sys3Int)province;
            if (Prov.Owner == 1)
            {
                await Script.CallFileAsync("FIGT_B.ADV");
                return true;
            }
        }
        return false;
    }
}
