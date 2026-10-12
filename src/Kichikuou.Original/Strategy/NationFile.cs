using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// The template every enemy nation's file repeats (part N1, <c>docs/strategy/survey-nations.md</c>
/// §2.3): the weekly turns (<c>T*.ADV</c>) and the defender pickers (<c>T*2.ADV</c>) each carry
/// their own copy of the same routines, which differ only in the filler's filter — a priority
/// list in <c>Tmp0001-0009</c>, the picker that fills <c>Bt_EnemyParty1-4</c> from it (on
/// <see cref="CommandFile"/>, since <c>Cﾃｷﾄｼ</c> has a copy too), the troop sum into
/// <c>CallArg</c>, and the walk over the target provinces that attacks the first one the player
/// holds through <c>FIGT_B</c>. A file overrides <see cref="CommandFile.SkipsCommander"/> with its
/// filter. The captures of non-player provinces differ in their text from file to file and are
/// ported with the files that have them (N2, N3).
/// </summary>
public abstract class NationFile(OriginalScriptContext script, string file, IScenarioSource? scenario = null)
    : CommandFile(script, file, scenario)
{
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
