namespace Kichikuou.Core.Combat;

// Part R1: the defeat scenes FIGTﾐｶDE.ADV and FIGTﾃｷDE.ADV (02-resolution.md §7.4, 01 §10).
// Their control flow is translated one-to-one from the decompile: every window operation
// (WAO_SUB.ADV call), every run of text (MessageEvent with the line of its first text line), the
// string prints and the state writes. They draw nothing from the RNG.
public sealed partial class Battle
{
    /// <summary>Shows the run of text that starts at <paramref name="line"/> of <paramref name="file"/>.</summary>
    private Task MessageAsync(string file, int line) => View.ShowAsync(new MessageEvent(new ScriptRef(file, line)));

    /// <summary>
    /// <c>FIGTﾐｶDE.ADV</c> (<c>FIGTﾐｶDE.ADV:1-728</c>) — the defeat scene when the player unit <c>Unit_Cur</c> fell: dialogue keyed by the record and story state. 02 §7.4.
    /// </summary>
    private async Task PlayerUnitDefeatedSceneAsync()
    {
        if (State.Unit_Cur == 1 && State.StoryStage == 0)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 4);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            await WaoAsync(143); // L_00e4e
            await MessageAsync("FIGTﾐｶDE.ADV", 13);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:20
        }
        if (State.Unit_Cur == 1)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 25);
            await WaoAsync(11); // L_00ecb
            await MessageAsync("FIGTﾐｶDE.ADV", 28);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            await WaoAsync(143); // L_00e4e
            await MessageAsync("FIGTﾐｶDE.ADV", 35);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            State.EndingId = 1; // FIGTﾐｶDE.ADV:38
            throw new ScenarioJumpException("OVER.ADV"); // FIGTﾐｶDE.ADV:39
        }
        if (State.Unit_Cur == 2 && State.StoryStage == 0)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 45);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            if (State.Bt_EnemyParty1 == 31)
            {
                await WaoAsync(142); // L_00e32
                await MessageAsync("FIGTﾐｶDE.ADV", 51);
                await WaoAsync(11); // L_00ecb
                await WaoAsync(10); // L_00ebe
                await WaoAsync(143); // L_00e4e
                await MessageAsync("FIGTﾐｶDE.ADV", 62);
                await WaoAsync(11); // L_00ecb
                await WaoAsync(10); // L_00ebe
                return; // FIGTﾐｶDE.ADV:65
            }
            await WaoAsync(142); // L_00e32
            await MessageAsync("FIGTﾐｶDE.ADV", 69);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            await WaoAsync(143); // L_00e4e
            await MessageAsync("FIGTﾐｶDE.ADV", 80);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:87
        }
        if (State.Unit_Cur == 2)
        {
            State.F_Sill_Died = 1; // FIGTﾐｶDE.ADV:90
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 93);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            await WaoAsync(143); // L_00e4e
            await MessageAsync("FIGTﾐｶDE.ADV", 98);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:105
        }
        if (State.Unit_Cur == 3 && State.StoryStage == 0)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 110);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:117
        }
        if (State.Unit_Cur == 3)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 122);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 4 && State.StoryStage == 0)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 133);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:140
        }
        if (State.Unit_Cur == 4)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 145);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 5)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 158);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 6)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 171);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 7)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 188);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 8)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 199);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 9)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 212);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 10)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 219);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 11)
        {
            State.F_Asuka_State = 0; // FIGTﾐｶDE.ADV:228
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 231);
            await WaoAsync(11); // L_00ecb
            await MessageAsync("FIGTﾐｶDE.ADV", 238);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 12)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 249);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 13)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 264);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 14)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 275);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 15)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 288);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 16)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 295);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 17)
        {
            if (State.Bt_IsDuel == 1)
            {
                await WaoAsync(141); // L_00e16
                await MessageAsync("FIGTﾐｶDE.ADV", 309);
                await WaoAsync(11); // L_00ecb
                await WaoAsync(10); // L_00ebe
                await PlayerUnitDefeatedScene_L_00decAsync(); // FIGTﾐｶDE.ADV:316
                return;
            }
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 320);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 18)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 333);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 19)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 340);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 20)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 351);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 21)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 360);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 22)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 375);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 23)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 390);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 24)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 401);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 25)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 412);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 26)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 423);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 27)
        {
            if (State.N_Isoroku_Stage == 4)
            {
                await WaoAsync(141); // L_00e16
                await MessageAsync("FIGTﾐｶDE.ADV", 437);
                await WaoAsync(11); // L_00ecb
                await WaoAsync(10); // L_00ebe
            }
            if (State.N_Isoroku_Stage > 4)
            {
                await WaoAsync(141); // L_00e16
                await MessageAsync("FIGTﾐｶDE.ADV", 450);
                await WaoAsync(11); // L_00ecb
                await WaoAsync(10); // L_00ebe
            }
            if (State.N_Isoroku_Stage < 4)
            {
                await WaoAsync(141); // L_00e16
                await MessageAsync("FIGTﾐｶDE.ADV", 461);
                await WaoAsync(11); // L_00ecb
                await WaoAsync(10); // L_00ebe
            }
        }
        if (State.Unit_Cur == 28)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 479);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 29)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 490);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 30)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 501);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 31)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 508);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 32)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 523);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 33)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 534);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 34)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 545);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:552
        }
        if (State.Unit_Cur == 35)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 557);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:568
        }
        if (State.Unit_Cur == 36)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 573);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:578
        }
        if (State.Unit_Cur == 37)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 583);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:592
        }
        if (State.Unit_Cur == 38)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 597);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:604
        }
        if (State.Unit_Cur == 39)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 609);
            if (State.F_Magic_State == 6)
            {
                await MessageAsync("FIGTﾐｶDE.ADV", 612);
            }
            await MessageAsync("FIGTﾐｶDE.ADV", 615);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:622
        }
        if (State.Unit_Cur == 40)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 627);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:630
        }
        if (State.Unit_Cur == 41)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 635);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 42)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 644);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 43)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 655);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 44)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 662);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 45)
        {
            if (State.F_Cafe_JoinMode == 2)
            {
                await WaoAsync(141); // L_00e16
                await MessageAsync("FIGTﾐｶDE.ADV", 670);
                await WaoAsync(11); // L_00ecb
                await WaoAsync(10); // L_00ebe
                return; // FIGTﾐｶDE.ADV:675
            }
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 679);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
            return; // FIGTﾐｶDE.ADV:688
        }
        if (State.Unit_Cur == 46)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 693);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 49)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 704);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        if (State.Unit_Cur == 50)
        {
            await WaoAsync(141); // L_00e16
            await MessageAsync("FIGTﾐｶDE.ADV", 715);
            await WaoAsync(11); // L_00ecb
            await WaoAsync(10); // L_00ebe
        }
        await WaoAsync(143); // L_00e4e
        await View.ShowAsync(new PrintStringEvent(1));
        await MessageAsync("FIGTﾐｶDE.ADV", 725);
        await WaoAsync(11); // L_00ecb
        await WaoAsync(10); // L_00ebe
        return; // FIGTﾐｶDE.ADV:728
    }

    /// <summary><c>L_00dec</c> (<c>FIGTﾐｶDE.ADV:729-739</c>) — shared closing lines of <see cref="PlayerUnitDefeatedSceneAsync"/>.</summary>
    private async Task PlayerUnitDefeatedScene_L_00decAsync()
    {
        await WaoAsync(143); // L_00e4e
        await View.ShowAsync(new PrintStringEvent(1));
        await MessageAsync("FIGTﾐｶDE.ADV", 734);
        await WaoAsync(11); // L_00ecb
        await WaoAsync(10); // L_00ebe
        return; // FIGTﾐｶDE.ADV:739
    }

    /// <summary>
    /// <c>FIGTﾃｷDE.ADV</c> (<c>FIGTﾃｷDE.ADV:1-1810</c>) — the defeat scene when the enemy commander <c>Cmdr_Cur</c> fell: dialogue keyed by the record and story state. 02 §7.4.
    /// </summary>
    private async Task EnemyCommanderDefeatedSceneAsync()
    {
        if (State.Cmdr_Cur == 1 && State.Prov_Cur != 6)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 4);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
            await WaoAsync(153); // L_0262f
            await MessageAsync("FIGTﾃｷDE.ADV", 13);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0259bAsync(); // FIGTﾃｷDE.ADV:21
            return;
        }
        if (State.Cmdr_Cur == 1 && State.Prov_Cur == 6)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 26);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:32
            return;
        }
        if (State.Cmdr_Cur == 2)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 37);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:43
            return;
        }
        if (State.Cmdr_Cur == 3)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 48);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0254aAsync(); // FIGTﾃｷDE.ADV:56
            return;
        }
        if (State.Cmdr_Cur == 4)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 61);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:67
            return;
        }
        if (State.Cmdr_Cur == 5)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 72);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 6)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 81);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 7)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 92);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 8)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 103);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 9)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 114);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 10)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 121);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 11)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 134);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 12)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 143);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 13)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 152);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 14)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 159);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 15)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 170);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 16)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 179);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 17)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 186);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 18)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 193);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 19)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 202);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 20)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 209);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 21)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 218);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 22)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 227);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:234
            return;
        }
        if (State.Cmdr_Cur == 23)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 239);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 24)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 246);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 25)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 255);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 26)
        {
            await WaoAsync(152); // L_02613
            await MessageAsync("FIGTﾃｷDE.ADV", 262);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 27)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 277);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 28)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 284);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 29)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 297);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 30)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 304);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 31)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 313);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:317
            return;
        }
        if (State.Cmdr_Cur == 32)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 322);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 33)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 337);
            await WaoAsync(11); // L_02658
            await MessageAsync("FIGTﾃｷDE.ADV", 342);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:344
            return;
        }
        if (State.Cmdr_Cur == 34)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 349);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 35)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 358);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 36)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 367);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 37)
        {
            await WaoAsync(152); // L_02613
            await MessageAsync("FIGTﾃｷDE.ADV", 374);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 38)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 383);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 39)
        {
            if (State.Bt_IsDuel == 1)
            {
                await WaoAsync(151); // L_025f7
                await MessageAsync("FIGTﾃｷDE.ADV", 391);
                await WaoAsync(11); // L_02658
                await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:399
                return;
            }
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 403);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:409
            return;
        }
        if (State.Cmdr_Cur == 40)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 414);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 41)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 421);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 42)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 432);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 43)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 441);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 44)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 448);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 45)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 459);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:463
            return;
        }
        if (State.Cmdr_Cur == 46)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 468);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 47)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 475);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 48)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 486);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 49)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 497);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 50)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 504);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 51)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 513);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:519
            return;
        }
        if (State.Cmdr_Cur == 52)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 524);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:528
            return;
        }
        if (State.Cmdr_Cur == 53)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 533);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 54)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 544);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 55)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 559);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:565
            return;
        }
        if (State.Cmdr_Cur == 56)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 570);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:574
            return;
        }
        if (State.Cmdr_Cur == 57)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 579);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:583
            return;
        }
        if (State.Cmdr_Cur == 58)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 588);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 59)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 597);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 60)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 610);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:614
            return;
        }
        if (State.Cmdr_Cur == 62)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 619);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:625
            return;
        }
        if (State.Cmdr_Cur == 63)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 630);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 64)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 641);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 65)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 650);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 66)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 659);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 67)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 672);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 68)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 683);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 69)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 692);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 70)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 703);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 71)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 712);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 72 && State.Prov_Cur != 56)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 721);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
            await EnemyCommanderDefeatedScene_L_0259bAsync(); // FIGTﾃｷDE.ADV:728
            return;
        }
        if (State.Cmdr_Cur == 72 && State.Prov_Cur == 56)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 733);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0254aAsync(); // FIGTﾃｷDE.ADV:741
            return;
        }
        if (State.Cmdr_Cur == 73)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 746);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 74)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 753);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 75)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 762);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 76)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 777);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 77)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 784);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 78)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 793);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 79)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 800);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 80)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 809);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 81)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 826);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 82)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 843);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:847
            return;
        }
        if (State.Cmdr_Cur == 83)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 852);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 84)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 859);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 85)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 868);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 86)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 877);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 87)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 884);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 88)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 891);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 89)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 900);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 91)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 907);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:915
            return;
        }
        if (State.Cmdr_Cur == 92)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 920);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:928
            return;
        }
        if (State.Cmdr_Cur == 93)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 933);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 94)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 944);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 95)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 951);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 96)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 958);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 97)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 969);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:975
            return;
        }
        if (State.Cmdr_Cur == 98)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 980);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 100)
        {
            if (Cmdr.CgFull == 1414)
            {
                await WaoAsync(151); // L_025f7
                await MessageAsync("FIGTﾃｷDE.ADV", 988);
                await WaoAsync(11); // L_02658
                await WaoAsync(10); // L_0264b
                await EnemyCommanderDefeatedScene_L_0254aAsync(); // FIGTﾃｷDE.ADV:991
                return;
            }
            if (Cmdr.FaceCg == 75)
            {
                await WaoAsync(151); // L_025f7
                await MessageAsync("FIGTﾃｷDE.ADV", 996);
                await WaoAsync(11); // L_02658
                await WaoAsync(10); // L_0264b
                await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1003
                return;
            }
            if (Cmdr.FaceCg == 484)
            {
                await WaoAsync(151); // L_025f7
                await MessageAsync("FIGTﾃｷDE.ADV", 1008);
                await WaoAsync(11); // L_02658
                await WaoAsync(10); // L_0264b
                await EnemyCommanderDefeatedScene_L_0254aAsync(); // FIGTﾃｷDE.ADV:1011
                return;
            }
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1015);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 101)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1022);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1028
            return;
        }
        if (State.Cmdr_Cur == 102)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1033);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 103)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1042);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 104)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1051);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1057
            return;
        }
        if (State.Cmdr_Cur == 105)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1062);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 106)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1071);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 107)
        {
            if (State.F_RagarlRitual_State != 7)
            {
                await WaoAsync(151); // L_025f7
                await MessageAsync("FIGTﾃｷDE.ADV", 1083);
                await WaoAsync(11); // L_02658
                await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1089
                return;
            }
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1093);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1099
            return;
        }
        if (State.Cmdr_Cur == 108)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1104);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 109)
        {
            if (State.Unit_Cur == 1)
            {
                if (State.Bt_IsDuel == 1)
                {
                    await WaoAsync(151); // L_025f7
                    await MessageAsync("FIGTﾃｷDE.ADV", 1117);
                    await WaoAsync(11); // L_02658
                    await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:1121
                    return;
                }
            }
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1126);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:1132
            return;
        }
        if (State.Cmdr_Cur == 110)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1137);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1139
            return;
        }
        if (State.Cmdr_Cur == 111)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1144);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 112)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1151);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 113)
        {
            if (State.Prov_Cur == 61 && State.Bt_IsDuel == 1)
            {
                await WaoAsync(151); // L_025f7
                await MessageAsync("FIGTﾃｷDE.ADV", 1163);
                await WaoAsync(11); // L_02658
                await WaoAsync(10); // L_0264b
                await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1170
                return;
            }
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1174);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:1178
            return;
        }
        if (State.Cmdr_Cur == 114)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1183);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 115)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1190);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 116)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1199);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 117)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1206);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1212
            return;
        }
        if (State.Cmdr_Cur == 118)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1217);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 119)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1226);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 120)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1233);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 121)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1244);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:1248
            return;
        }
        if (State.Cmdr_Cur == 122)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1253);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 123)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1262);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 124)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1269);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 125)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1278);
            await WaoAsync(11); // L_02658
            if (State.Prov_Cur == 66)
            {
                await EnemyCommanderDefeatedScene_L_0254aAsync(); // FIGTﾃｷDE.ADV:1283
                return;
            }
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:1285
            return;
        }
        if (State.Cmdr_Cur == 126)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1290);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 127)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1299);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 128)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1310);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 136)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1319);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1327
            return;
        }
        if (State.Cmdr_Cur == 137)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1332);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1340
            return;
        }
        if (State.Cmdr_Cur == 138)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1345);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 139)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1356);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 140)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1365);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 141)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1374);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 142)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1387);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 143)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1400);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 144)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1409);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 145)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1420);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 146)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1427);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 147)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1436);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 148)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1445);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 149)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1456);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 150)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1463);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 151)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1470);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 152)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1477);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 153)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1484);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 154)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1493);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 155)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1502);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 157)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1509);
            await WaoAsync(11); // L_02658
            if (State.Bt_IsDuel == 1)
            {
                await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1512
                return;
            }
        }
        if (State.Cmdr_Cur == 159)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1518);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1524
            return;
        }
        if (State.Cmdr_Cur == 160)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1529);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 161)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1538);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 162)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1545);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0251eAsync(); // FIGTﾃｷDE.ADV:1551
            return;
        }
        if (State.Cmdr_Cur == 163)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1556);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 164)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1565);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 165)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1572);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 166)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1583);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 167)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1590);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 168)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1599);
            await WaoAsync(11); // L_02658
            await EnemyCommanderDefeatedScene_L_0256fAsync(); // FIGTﾃｷDE.ADV:1601
            return;
        }
        if (State.Cmdr_Cur == 169)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1606);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 170)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1617);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 171)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1626);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 172)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1633);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 173)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1642);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 174)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1649);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 175)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1658);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 176)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1665);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 177)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1674);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 178)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1683);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 179)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1692);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 180)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1699);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 181)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1708);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 182)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1721);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 183)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1730);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 184)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1737);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 185)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1746);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 186)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1753);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 187)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1762);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 188)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1769);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 189)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1776);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur == 190)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1783);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        if (State.Cmdr_Cur > 190 && State.Cmdr_Cur < 200)
        {
            await WaoAsync(151); // L_025f7
            await MessageAsync("FIGTﾃｷDE.ADV", 1790);
            await WaoAsync(11); // L_02658
            await WaoAsync(10); // L_0264b
        }
        await WaoAsync(143); // L_025db
        await View.ShowAsync(new PrintStringEvent(4));
        await MessageAsync("FIGTﾃｷDE.ADV", 1808);
        await WaoAsync(11); // L_02658
        await EnemyCommanderDefeatedScene_L_0259bAsync(); // FIGTﾃｷDE.ADV:1810
        return;
    }

    /// <summary><c>L_0251e</c> (<c>FIGTﾃｷDE.ADV:1811-1821</c>) — shared closing lines of <see cref="EnemyCommanderDefeatedSceneAsync"/>.</summary>
    private async Task EnemyCommanderDefeatedScene_L_0251eAsync()
    {
        await WaoAsync(10); // L_0264b
        await WaoAsync(153); // L_0262f
        await View.ShowAsync(new PrintStringEvent(4));
        await MessageAsync("FIGTﾃｷDE.ADV", 1817);
        await WaoAsync(11); // L_02658
        await EnemyCommanderDefeatedScene_L_0259bAsync(); // FIGTﾃｷDE.ADV:1821
        return;
    }

    /// <summary><c>L_0254a</c> (<c>FIGTﾃｷDE.ADV:1822-1830</c>) — shared closing lines of <see cref="EnemyCommanderDefeatedSceneAsync"/>.</summary>
    private async Task EnemyCommanderDefeatedScene_L_0254aAsync()
    {
        await WaoAsync(10); // L_0264b
        await WaoAsync(153); // L_0262f
        await View.ShowAsync(new PrintStringEvent(4));
        await MessageAsync("FIGTﾃｷDE.ADV", 1828);
        await WaoAsync(11); // L_02658
        await EnemyCommanderDefeatedScene_L_0259bAsync(); // FIGTﾃｷDE.ADV:1830
        return;
    }

    /// <summary><c>L_0256f</c> (<c>FIGTﾃｷDE.ADV:1831-1841</c>) — shared closing lines of <see cref="EnemyCommanderDefeatedSceneAsync"/>.</summary>
    private async Task EnemyCommanderDefeatedScene_L_0256fAsync()
    {
        await WaoAsync(10); // L_0264b
        await WaoAsync(153); // L_0262f
        await View.ShowAsync(new PrintStringEvent(4));
        await MessageAsync("FIGTﾃｷDE.ADV", 1837);
        await WaoAsync(11); // L_02658
        await EnemyCommanderDefeatedScene_L_0259bAsync(); // FIGTﾃｷDE.ADV:1841
        return;
    }

    /// <summary><c>L_0259b</c> (<c>FIGTﾃｷDE.ADV:1842-1856</c>) — shared closing lines of <see cref="EnemyCommanderDefeatedSceneAsync"/>.</summary>
    private async Task EnemyCommanderDefeatedScene_L_0259bAsync()
    {
        await WaoAsync(10); // L_0264b
        return; // FIGTﾃｷDE.ADV:1844
    }
}
