using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// JAPAN's files (milestone 3 step 5): the defender picker <c>TJAPAN2</c>
/// (<see cref="JapanDefenders"/>). Also compared with the decompile on random states (Content
/// tests, <c>NationTurnDifferentialTests</c>).
/// </summary>
public class JapanTests
{
    /// <summary>JAPAN's commanders, Nobunaga Oda (72) to Isoroku Yamamoto (82), alive with 100 troops each; a stale retreat mode 50.</summary>
    private sealed class JapanRig
    {
        public JapanRig()
        {
            Script = new ScriptContext(State, new ScriptedRandom(), View, Input, Story);
            for (int c = 72; c <= 82; c++)
            {
                State.Cmdrs[c].Status = 1;
                State.Cmdrs[c].Troops = 100;
            }
            State.Bt_EnemyRetreatMode = 50;
        }

        public GameState State { get; } = new();
        public RecordingView View { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public ScriptedStory Story { get; } = new();
        public ScriptContext Script { get; }

        public int[] Party =>
            [State.Bt_EnemyParty1.Value, State.Bt_EnemyParty2.Value, State.Bt_EnemyParty3.Value, State.Bt_EnemyParty4.Value];

        /// <summary>An attack on <paramref name="province"/>, with walls of 7 and a battle area of 900.</summary>
        public Task DefendersAsync(int province)
        {
            State.Prov_Cur = (Sys3Int)province;
            State.Provs[province].Defense = 7;
            State.Provs[province].BattleArea = 900;
            return new JapanDefenders(Script, new Literals()).RunAsync();
        }
    }

    /// <summary>The army name (<c>MS 10, "…"</c>) as "file:line", standing in for the player's files.</summary>
    private sealed class Literals : Kichikuou.Core.Setup.IScenarioSource
    {
        public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

        public ScriptStatement ReadStatement(ScriptRef at) => new SetStringStatement(at, 10, at.ToString());
    }

    private static void Is(int expected, Sys3Int actual) => Assert.Equal(expected, actual.Value);

    [Fact]
    public async Task Nagasaki_fields_Katsuie_to_Denden_Mori_and_starts_the_war_even_over_an_alliance()
    {
        var rig = new JapanRig();
        rig.State.F_Japan_State = 3;

        await rig.DefendersAsync(55);

        Is(6, rig.State.Ev_PhaseNation);
        Assert.Equal("TJAPAN2.ADV:3", rig.State.Strings[10]);
        Is(1, rig.State.F_Japan_State);
        Assert.Equal([75, 76, 77, 78], rig.Party);
        Is(7, rig.State.Bt_WallBonus);
        Is(900, rig.State.Bt_TroopLimit);
        Is(1, rig.State.Bt_EnemyRetreatMode);
        Assert.Empty(rig.Story.Calls);
    }

    [Fact]
    public async Task Nagasaki_under_10_troops_and_elsewhere_under_500_keep_the_last_retreat_mode()
    {
        var rig = new JapanRig();
        for (int c = 75; c <= 78; c++)
            rig.State.Cmdrs[c].Troops = 2;                          // 8 in all
        await rig.DefendersAsync(55);
        Is(8, rig.State.CallArg);
        Is(50, rig.State.Bt_EnemyRetreatMode);                      // BFL-13

        rig = new JapanRig();                                       // 400 at most anywhere else
        await rig.DefendersAsync(57);
        Is(400, rig.State.CallArg);
        Is(50, rig.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public async Task Osaka_fields_the_whole_army_with_Nobunaga_first()
    {
        var rig = new JapanRig();
        rig.State.Cmdrs[73].Status = 0;                             // Kenichi Oda fallen

        await rig.DefendersAsync(56);

        Assert.Equal([72, 74, 78, 77], rig.Party);
        Is(1, rig.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public async Task Elsewhere_the_strongest_list_defends_and_the_earlier_wins_a_tie()
    {
        var rig = new JapanRig();
        rig.State.Cmdrs[80].Troops = 300;                           // the last four: 600
        await rig.DefendersAsync(57);
        Is(3, rig.State.Tmp0022);
        Assert.Equal([79, 80, 81, 82], rig.Party);
        Is(600, rig.State.CallArg);
        Is(4, rig.State.Bt_EnemyRetreatMode);

        rig = new JapanRig();
        rig.State.Cmdrs[76].Troops = 300;                           // the first four: 600, the whole army 400
        await rig.DefendersAsync(57);
        Is(2, rig.State.Tmp0022);
        Assert.Equal([75, 76, 77, 78], rig.Party);

        rig = new JapanRig();
        rig.State.Cmdrs[76].Troops = 300;
        rig.State.Cmdrs[72].Troops = 300;                           // the whole army: 600 too, and first
        await rig.DefendersAsync(57);
        Is(1, rig.State.Tmp0022);
        Assert.Equal([72, 73, 74, 78], rig.Party);
    }

    [Fact]
    public void TJAPAN2_is_routed() => Assert.Contains("TJAPAN2.ADV", StrategyRules.PortedFiles);
}
