using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using static Kichikuou.Original.Tests.Combat.Sys3Assert;

namespace Kichikuou.Original.Tests.Combat;

/// <summary>
/// The ninja follow-up's chance (owner's decision 33, docs/tweaks.md; <see cref="NinjaChance"/>): the player's choice is
/// read off R14, the <c>RND(3)</c> of <c>FIGT.ADV:1915</c>, which is drawn whatever the choice; the follow-up's other
/// conditions (02 §7.1) stay the original's. One <c>ApplyExchange</c> of no damage: Menad (unit 10) against commander 6
/// (HP 5), a player's exchange; the draws are FIGTATTK's <c>RND(2)</c> and R14, and a ninja who comes stops the rig at the
/// nested exchange's first draw.
/// </summary>
public class NinjaChanceTests
{
    private const int Menad = 10;

    private static FlowRig Rig(NinjaChance chance, int roll, int unit = Menad, int cmdrHp = 5, int msgMode = 3)
    {
        var rig = new FlowRig(1, roll);
        var s = rig.State;
        rig.SetPlayerSlot(1, unit);
        rig.SetEnemySlot(1, 6);
        var u = s.Units[unit];
        u.HP = 50;
        u.MaxHP = 50;
        u.Status = 2;
        var c = s.Cmdrs[6];
        c.HP = (Sys3Int)cmdrHp;
        c.MaxHP = 5;
        c.Status = 1;
        s.Unit_Cur = (Sys3Int)unit;
        s.Cmdr_Cur = 6;
        s.Bt_PlayerActSlot = 1;
        s.Bt_EnemyActSlot = 1;
        s.Bt_Kind = 1;
        s.Bt_TroopLimit = 5000;
        s.Bt_MsgMode = (Sys3Int)msgMode;
        rig.Battle.Script.NinjaChance = chance;
        return rig;
    }

    /// <summary>Runs the exchange; true when Kanami came (the rig stopped at her exchange's draw).</summary>
    private static async Task<bool> KanamiComes(FlowRig rig)
    {
        bool stopped = await FlowRig.UntilStub(rig.Battle.ApplyExchangeAsync);
        Assert.Equal([2, 3], rig.Random.Draws.Take(2));     // FIGTATTK.ADV:234, R14 — the same draws whatever the choice
        if (!stopped)
        {
            Assert.Equal(2, rig.Random.Draws.Count);
            Is(Menad, rig.State.Unit_Cur);
            return false;
        }
        Is(51, rig.State.Unit_Cur);
        Is(1164, rig.State.Units[51].CgFull);
        return true;
    }

    [Fact]
    public void A_context_built_by_hand_keeps_the_original()
    {
        Assert.Equal(NinjaChance.Original, new FlowRig().Battle.Script.NinjaChance);
    }

    [Theory]
    [InlineData(NinjaChance.Original, 1, true)]
    [InlineData(NinjaChance.Original, 2, false)]
    [InlineData(NinjaChance.Original, 3, false)]
    [InlineData(NinjaChance.Original, 4, false)]       // genrand() = 1.0 (ENG-02)
    [InlineData(NinjaChance.TwoInThree, 1, true)]
    [InlineData(NinjaChance.TwoInThree, 2, true)]
    [InlineData(NinjaChance.TwoInThree, 3, false)]
    [InlineData(NinjaChance.TwoInThree, 4, false)]
    [InlineData(NinjaChance.Always, 1, true)]
    [InlineData(NinjaChance.Always, 2, true)]
    [InlineData(NinjaChance.Always, 3, true)]
    [InlineData(NinjaChance.Always, 4, true)]
    public async Task The_choice_reads_R14(NinjaChance chance, int roll, bool comes)
    {
        var rig = Rig(chance, roll);

        Assert.Equal(comes, await KanamiComes(rig));
        if (!comes)
            Is(roll, rig.State.RetVal);     // R14's value left as the original leaves it
    }

    [Theory]
    [InlineData(44, 5, 3, 0)]          // a unit without a ninja
    [InlineData(Menad, 1, 3, 0)]       // the commander at 1 HP: the ninja never kills (02 Q14)
    [InlineData(Menad, 5, 4, 0)]       // an enemy's exchange
    [InlineData(Menad, 5, 2, 0)]       // the player's magic met by the enemy's
    [InlineData(Menad, 5, 3, 1)]       // Kanami spent this week
    public async Task Always_keeps_the_other_conditions(int unit, int cmdrHp, int msgMode, int used)
    {
        var rig = Rig(NinjaChance.Always, 3, unit, cmdrHp, msgMode);
        rig.State.F_Kanami_NinjaUsed = (Sys3Int)used;

        bool stopped = await FlowRig.UntilStub(rig.Battle.ApplyExchangeAsync);

        Assert.False(stopped);
        Assert.Equal([2, 3], rig.Random.Draws);
        Is(unit, rig.State.Unit_Cur);
    }
}
