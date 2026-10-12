namespace Kichikuou.Core.Tests.Combat;

/// <summary>
/// Draw count of every unit and commander (spec 02 §5.5): RND(2) at :5, RND(3) at :25 or
/// :2825, then the branch draw that the "else" of §5.2 / §5.3 names, in a neutral state where
/// no guard fires (no special, <c>Cmdr_Cur</c>/<c>Unit_Cur</c> 0 on the other side, troops
/// present, story flags 0, <c>StoryStage</c> 1 unless stated).
/// </summary>
public class FlavourSweepTests
{
    /// <summary>Units with a final draw (§5.2 "else RND(n)") under physical attack (mode 3).</summary>
    private static readonly Dictionary<int, int> PlayerDraws = new()
    {
        [1] = 10, [2] = 5, [3] = 3, [4] = 3, [5] = 5, [6] = 5, [7] = 4, [8] = 4, [9] = 3, [10] = 3,
        [11] = 3, [12] = 3, [13] = 3, [14] = 3, [15] = 3, [16] = 3, [17] = 3, [18] = 3, [19] = 3,
        [20] = 3, [21] = 3, [22] = 3, [23] = 3, [24] = 3, [25] = 3, [26] = 3, [28] = 3, [29] = 3,
        [31] = 3, [32] = 3, [33] = 3, [34] = 3, [35] = 3, [36] = 3, [37] = 3, [38] = 3, [39] = 3,
        [40] = 3, [41] = 3, [43] = 3, [45] = 3, [46] = 3, [47] = 3, [49] = 3, [50] = 3,
    };

    /// <summary>Units whose "Mode=1" guard pre-empts the final draw under player magic.</summary>
    private static readonly int[] MagicLineUnits = [11, 13, 16, 17, 23, 37, 38, 39, 40, 46];

    /// <summary>Commanders with a final draw (§5.3) under physical attack (mode 4), stage 1.</summary>
    private static readonly Dictionary<int, int> EnemyDraws = new()
    {
        [1] = 3, [2] = 3, [8] = 3, [22] = 3, [31] = 3, [32] = 3, [39] = 3, [40] = 3, [41] = 3,
        [44] = 3, [45] = 3, [50] = 3, [51] = 3, [52] = 3, [54] = 3, [55] = 3, [56] = 3, [57] = 3,
        [62] = 3, [83] = 3, [84] = 3, [90] = 3, [91] = 3, [92] = 3, [93] = 3, [101] = 3, [104] = 3,
        [107] = 3, [109] = 3, [113] = 3, [117] = 3, [121] = 3, [125] = 3, [129] = 3, [130] = 3,
        [131] = 3, [132] = 3, [133] = 3, [134] = 3, [135] = 4, [156] = 3, [157] = 3,
    };

    /// <summary>Commanders whose "Mode=2" guard pre-empts the final draw under enemy magic.</summary>
    private static readonly int[] EnemyMagicLineCmdrs = [56, 57, 90, 101, 104, 107, 109, 113, 117, 121, 125, 130, 133];

    [Fact]
    public async Task Every_unit_under_physical_attack_draws_as_specified()
    {
        for (int unit = 0; unit <= 255; unit++)
        {
            var rig = Neutral(new FlavourRig(1, 3, 1).Player(unit, cmdr: 0, mode: 3));

            await rig.RunAsync();

            Assert.Equal(Expected(PlayerDraws.GetValueOrDefault(unit)), rig.Draws);
        }
    }

    [Fact]
    public async Task Every_unit_under_player_magic_draws_as_specified()
    {
        for (int unit = 0; unit <= 255; unit++)
        {
            // :25 rolls 3 so that Sill's two magic guards (RetVal = 1 / 2) stay false.
            var rig = Neutral(new FlavourRig(1, 3, 1).Player(unit, cmdr: 0, mode: 1));

            await rig.RunAsync();

            int n = MagicLineUnits.Contains(unit) ? 0 : PlayerDraws.GetValueOrDefault(unit);
            Assert.Equal(Expected(n), rig.Draws);
        }
    }

    [Fact]
    public async Task Every_commander_under_physical_attack_draws_as_specified()
    {
        for (int cmdr = 0; cmdr <= 255; cmdr++)
        {
            var rig = Neutral(new FlavourRig(1, 1, 1).Enemy(cmdr, unit: 0, mode: 4));

            await rig.RunAsync();

            Assert.Equal(Expected(EnemyDraws.GetValueOrDefault(cmdr)), rig.Draws);
        }
    }

    [Fact]
    public async Task Every_commander_under_enemy_magic_draws_as_specified()
    {
        for (int cmdr = 0; cmdr <= 255; cmdr++)
        {
            var rig = Neutral(new FlavourRig(1, 1, 1).Enemy(cmdr, unit: 0, mode: 2));

            await rig.RunAsync();

            int n = EnemyMagicLineCmdrs.Contains(cmdr) ? 0 : EnemyDraws.GetValueOrDefault(cmdr);
            Assert.Equal(Expected(n), rig.Draws);
        }
    }

    [Fact]
    public async Task Every_commander_at_story_stage_0_draws_as_specified()
    {
        for (int cmdr = 0; cmdr <= 255; cmdr++)
        {
            var rig = Neutral(new FlavourRig(1, 1, 1).Enemy(cmdr, unit: 0, mode: 4));
            rig.State.StoryStage = 0;

            await rig.RunAsync();

            // C31 has a fixed line at stage 0; C58 rolls RND(2) at stage 0 (and has a fixed line later).
            int n = cmdr switch { 31 => 0, 58 => 2, _ => EnemyDraws.GetValueOrDefault(cmdr) };
            Assert.Equal(Expected(n), rig.Draws);
        }
    }

    [Fact]
    public async Task Every_call_draws_two_or_three_times()
    {
        int[] modes = [1, 2, 3, 4];
        foreach (int mode in modes)
        {
            for (int id = 0; id <= 255; id++)
            {
                var rig = Neutral(new FlavourRig(1, 3, 1).Player(unit: id, cmdr: id, mode: mode));

                await rig.RunAsync();

                Assert.InRange(rig.Draws.Length, 2, 3);
                Assert.Equal(2, rig.Draws[0]);
                Assert.Equal(3, rig.Draws[1]);
            }
        }
    }

    private static int[] Expected(int branchDraw) => branchDraw == 0 ? [2, 3] : [2, 3, branchDraw];

    /// <summary>Troops on every unit (Rance's "no troops" guard), stage 1, everything else 0.</summary>
    private static FlavourRig Neutral(FlavourRig rig)
    {
        for (int unit = 0; unit < 52; unit++)
            rig.State.Units[unit].Troops = 100;
        rig.State.StoryStage = 1;
        return rig;
    }
}
