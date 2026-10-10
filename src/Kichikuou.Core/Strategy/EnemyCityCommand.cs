using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Cﾃｷﾄｼ.ADV</c>, the enemy city next to the player's land (<c>docs/strategy/survey-commands.md</c>
/// §2-§3.1): the header, the menu (AL Church, Attack/Pillage, Ninja Maneuver, Return), the
/// prologue's Pillage (<c>Iﾄｳｿﾞｸ</c> 1, then <c>ｵｰﾌﾟﾆﾝｸﾞ</c> if the story stage moved) and the attack
/// (<c>:99-425</c>): Maris's refusals, the party picker <c>Sﾌﾞﾀｲ</c>, the units' refusals, the
/// defenders chosen by the owner's <c>T*2</c> file (calls through the hooks; Patton's list inline),
/// the exits without a battle and <c>FIGT_A</c>. The independent cities (owner 3, <c>:429-3828</c>,
/// part P4) are in <c>EnemyCityCommand.Cities.cs</c>; their negotiations fail as not ported yet.
/// Called by <c>COMMAND.ADV</c> with the clicked province in <c>Prov_Cur</c>. The JAPAN "Fleeing
/// Soldier" names (<c>MS 4/5/10</c>) and the cities' army names (<c>MS 10</c>) are read from the
/// player's files through <paramref name="scenario"/>.
/// </summary>
public sealed partial class EnemyCityCommand(ScriptContext script, IScenarioSource? scenario = null)
    : CommandFile(script, "Cﾃｷﾄｼ.ADV", scenario)
{
    private const int VarTmp0021 = 21;

    private enum Choice
    {
        ALChurch,       // L_0021f
        Unavailable,    // L_0010d: the menu again
        Attack,         // L_00230
        Ninja,          // L_009fe
        Return,         // L_00205
    }

    /// <summary>Where an attack leaves the file.</summary>
    private enum Outcome
    {
        Done,           // %0: or @L_00205 — the file returns
        Header,         // @L_00078 — the header and the menu again
        Menu,           // @L_0010d — the menu again, without the header
        Top,            // @L_00025 — the city's picture, the header and the menu again
    }

    /// <summary><c>Cﾃｷﾄｼ.ADV:1-108</c>: the city's menu until a command leaves the file.</summary>
    public async Task RunAsync()
    {
        State.Ui_CityScouted = 0;                                       // :1
        while (true)
        {
            // L_00025 (:2-16) — Ninja Maneuver comes back here, keeping what the ninja scouted.
            State.Bt_TargetProv = State.Prov_Cur;
            CopyString(8, State.Prov_StrBase + State.Prov_Cur - 1);
            State.CallArg = Prov.BgCg;
            await WaoAsync(331);                                        // \L_0539b: the city's picture
            if (State.Ev_CommandMode == 1)                              // :7-16 a forced trip takes over
            {
                await Script.EventAsync("Gｶﾌｪ.ADV", 31);
                return;
            }
            if (State.Ev_CommandMode == 2)
            {
                await Script.EventAsync("Gﾏﾘｱ.ADV", 5);
                return;
            }

            bool header = true;
            while (true)
            {
                // L_00078 (:17-43) — "［name］ (owner)" and the scouted figures; an attack the
                // player called off comes back here.
                if (header)
                {
                    await WaoAsync(113);                                // \L_052d7
                    await TextAsync(20);
                    await OwnerAsync();                                 // \L_05185
                    if (State.Ui_CityScouted != 1)
                        await TextAsync(28);                            // "Economy: ??  Defense: ??"
                    if (State.Ui_CityScouted == 1)
                        await ValuesAsync(34, Prov.Economy, Prov.Defense, Prov.BattleArea);
                }
                header = true;

                var choice = await MenuAsync();
                switch (choice)
                {
                    case Choice.ALChurch:                               // L_0021f (:94-98)
                        State.Ui_CmdOwnCityAvail = 0;
                        await Script.EventAsync("GAL.ADV", 7);
                        return;
                    case Choice.Attack:
                        var outcome = await AttackAsync();
                        if (outcome == Outcome.Header)
                            continue;
                        if (outcome == Outcome.Menu)                    // the independent cities' Stop
                        {
                            header = false;
                            continue;
                        }
                        if (outcome == Outcome.Top)                     // after Pluepet's scene
                            break;
                        return;
                    case Choice.Ninja:                                  // L_009fe
                        await Script.CallFileAsync("Cﾆﾝｼﾞｬ.ADV");
                        break;
                    default:
                        await LeaveAsync();
                        return;
                }
                break;                                                  // @L_00025
            }
        }
    }

    /// <summary>
    /// <c>L_0010d</c> (<c>:44-86</c>): the release wait, the pointer saved, the menu; a "×" item
    /// starts again from the wait, a cancel puts the pointer back and returns.
    /// </summary>
    private async Task<Choice> MenuAsync()
    {
        while (true)
        {
            await WaitForReleaseAsync();
            await SavePointerAsync();                                   // \L_05289
            var items = new List<(SceneMenuItem Item, Choice Choice)>();
            bool church = State.N_ALChurch_Stage > 3 && State.N_ALChurch_Stage < 20 && State.Prov_Cur == 77;
            if (church && State.Ui_CmdOwnCityAvail != 0)
                items.Add((MenuItem(52), Choice.ALChurch));             // ○Go to AL Church
            if (church && State.Ui_CmdOwnCityAvail == 0)
                items.Add((MenuItem(58), Choice.Unavailable));          // ×Go to AL Church
            if (State.StoryStage != 0)
                items.Add((MenuItem(64), Choice.Attack));               // Attack
            if (State.StoryStage == 0)
                items.Add((MenuItem(70), Choice.Attack));               // Pillage
            if (State.StoryStage != 0)
                items.Add((MenuItem(76), Choice.Ninja));                // Ninja Maneuver
            items.Add((MenuItem(81), Choice.Return));

            int choice = await SelectAsync(items.Select(i => i.Item).ToArray());
            if (choice == 0)
            {
                await RestorePointerAsync();                            // \L_05294, then @L_00205
                return Choice.Return;
            }
            if (items[choice - 1].Choice != Choice.Unavailable)
                return items[choice - 1].Choice;
        }
    }

    /// <summary><c>L_00205</c> (<c>:87-93</c>): no target, the window and the picture closed, the map redrawn.</summary>
    private async Task LeaveAsync()
    {
        State.Bt_TargetProv = 0;
        await WaoAsync(0);                                              // \L_053b7
        await WaoAsync(332);                                            // \L_053a9
        await Script.MapCallAsync(0);
    }

    // ---------------------------------------------------------------------------------------
    // The attack (L_00230)
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_00230</c> (<c>:99-307</c>): in the prologue Pillage — <c>Iﾄｳｿﾞｸ</c> 1, and the opening if
    /// the scene moved the story on. Later the attack: Maris's refusals, Patton's scenes, the
    /// briefing, the party picked in <c>Sﾌﾞﾀｲ</c> (which charges the war expenses) and the units
    /// that refuse to go; then the defenders and the battle (<see cref="DeployAsync"/>).
    /// </summary>
    private async Task<Outcome> AttackAsync()
    {
        await WaoAsync(0);                                              // \L_053b7
        if (State.StoryStage == 0)
        {
            await Script.EventAsync("Iﾄｳｿﾞｸ.ADV", 1);
            if (State.StoryStage != 0)
                await Script.CallFileAsync("ｵｰﾌﾟﾆﾝｸﾞ.ADV");
            return Outcome.Done;
        }
        if (State.StoryStage == 4)                                      // :109 @L_04b8a
        {
            await ChaosRefusalAsync();
            return Outcome.Done;
        }
        if (State.Ui_CmdAttackAvail == 0)                               // :112 @L_04aaa
        {
            await GarrisonRefusalAsync();
            return Outcome.Done;
        }
        if (State.Prov_Cur == 82)                                       // :115 \L_0499c
            await LandOfDeathAsync();
        if (State.StoryStage == 1 && Prov.Owner != 2)                   // :118 @L_0481f
        {
            await CivilWarRefusalAsync();
            return Outcome.Done;
        }
        if (Prov.Owner == 3)                                            // :121 @L_00a07
            return await IndependentCityAsync();
        if (Prov.Owner == 5 && State.F_Patton_State < 10 && State.F_Helman_War == 1)   // :124-130
        {
            await Script.EventAsync("Gﾊﾟｯﾄﾝ.ADV", 3);
            if (State.F_Patton_State == 20)
                return Outcome.Done;
        }
        if (Prov.Owner == 5 && State.F_Patton_State == 10 && State.F_Helman_War == 1)  // :131-134
            await Script.EventAsync("Gﾊﾟｯﾄﾝ.ADV", 4);

        State.CallArg = 1;                                              // :135-143 Rance: "Let's begin the battle"
        await WaoAsync(311);                                            // \L_052f3
        await SpeakAsync(111, 139);                                     // \L_0529f

        if (Prov.Owner == 2 && State.F_LeazasRebels_State == 1)        // :144 @L_04786
        {
            await DisruptedRefusalAsync();
            return Outcome.Done;
        }
        if ((State.Prov_Cur == 78 || State.Prov_Cur == 89 || State.Prov_Cur == 90 || State.Prov_Cur == 91)
            && State.StoryStage == 2)                                   // :147-157
        {
            if (State.F_Zeth_War != 2 || State.F_Helman_War != 2 || State.F_DemonWar_Stage < 10)
            {
                await Script.EventAsync("Iｼﾚｲﾌﾞ.ADV", 5);
                await WaoAsync(312);                                    // \L_05301
                await WaoAsync(322);                                    // \L_05371
                return Outcome.Header;
            }
            await Script.EventAsync("Iｼﾚｲﾌﾞ.ADV", 6);
        }

        State.CallArg = 6;                                              // :158-171 Maris: "select the units"
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 162, 165);                                // \L_0530f

        State.Tmp0021 = 4;                                              // :172-199 the party's size
        State.Tmp0022 = 1;
        if (State.Prov_Cur == 32)
        {
            State.Prov_Cur = 33;
            if (Prov.Owner != 1)
            {
                State.Prov_Cur = 34;
                if (Prov.Owner != 1)
                {
                    State.Prov_Cur = 31;
                    if (Prov.Owner != 1)
                        State.Tmp0021 = 1;
                }
            }
            State.Prov_Cur = 32;
        }
        if (State.Prov_Cur == 31)
        {
            State.Prov_Cur = 36;
            if (Prov.Owner != 1)
            {
                State.Prov_Cur = 32;
                if (Prov.Owner != 1)
                {
                    State.Prov_Cur = 30;
                    if (Prov.Owner != 1)
                        State.Tmp0021 = 1;
                }
            }
            State.Prov_Cur = 31;
        }
        State.Buf_Cur = 0;                                              // :200-201 NB Buf_Val, Tmp0021, 20
        for (int i = 0; i < 20; i++)
            State.Buffer[State.Buf_Cur.Value + i].Val = State.Memory.Var(VarTmp0021 + i);
        if (State.Tmp0021 == 1)                                         // :202-214 "only one unit can attack"
            await SpeakAsync(121, 205);
        await WaoAsync(322);                                            // :215 \L_05371
        State.Buf_Cur = 0;                                              // :216-217 NB Tmp0021, Buf_Val, 20
        for (int i = 0; i < 20; i++)
            State.Memory.Var(VarTmp0021 + i) = State.Buffer[State.Buf_Cur.Value + i].Val;

        // QUIRK: BFL-01 — Sﾌﾞﾀｲ charges the war expenses on Go; nothing below gives them back.
        await Script.CallFileAsync("Sﾌﾞﾀｲ.ADV");                        // :218
        await Script.MapCallAsync(0);                                   // :219-220
        State.CallArg = Prov.BgCg;                                      // :221-222
        await WaoAsync(331);                                            // \L_0539b
        if (State.Bt_PlayerParty1 == 0)                                 // :223-225 the picker was left
            return Outcome.Header;

        // :226-307 — the units that will not go. QUIRK: CMD-01 — province 56 at war with JAPAN
        // skips Satella's and Meglass's refusals.
        if (InParty(44) && Prov.Owner == 11)                            // :226-237
        {
            await RefuseAsync(463, 231);
            return Outcome.Header;
        }
        if (InParty(37) && Prov.Owner == 7 && State.F_Zeth_State < 8)  // :238-262 Gandhi
        {
            await RefuseAsync(42, 244, 255);
            return Outcome.Header;
        }
        if (!(State.Prov_Cur == 56 && State.F_Japan_State == 1))        // :263-265 @L_00745
        {
            if (InParty(29) && Prov.Owner != 8 && Prov.Owner != 10)     // :266-288 Satella
            {
                await RefuseAsync(75, 272, 281);
                return Outcome.Header;
            }
            if (InParty(30) && Prov.Owner != 8 && Prov.Owner != 10)     // :289-307 Meglass
            {
                await RefuseAsync(447, 295, 300);
                return Outcome.Header;
            }
        }
        await DeployAsync();
        return Outcome.Done;
    }

    /// <summary>Whether unit <paramref name="unit"/> is in one of the four party slots.</summary>
    private bool InParty(Sys3Int unit) =>
        State.Bt_PlayerParty1 == unit || State.Bt_PlayerParty2 == unit
        || State.Bt_PlayerParty3 == unit || State.Bt_PlayerParty4 == unit;

    /// <summary>
    /// A unit refuses (<c>:226-307</c>): its portrait and words, the narration "… refused to fight"
    /// (none for unit 44), the portrait removed, the party back to status 1 (<c>\L_05118</c>), the
    /// header again.
    /// </summary>
    private async Task RefuseAsync(int portrait, int words, int narration = 0)
    {
        State.CallArg = (Sys3Int)portrait;
        await WaoAsync(311);                                            // \L_052f3
        await SpeakAsync(111, words);                                   // \L_0529f
        if (narration != 0)
            await SpeakAsync(113, narration);                           // \L_052d7
        await WaoAsync(312);                                            // \L_05301
        Undeploy();                                                     // \L_05118
    }

    /// <summary>
    /// <c>L_05118</c> (<c>:4347-4364</c>): every unit of the party back to status 1. The war
    /// expenses <c>Sﾌﾞﾀｲ</c> charged stay paid (BFL-01).
    /// </summary>
    private void Undeploy()
    {
        if (State.Bt_PlayerParty1 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerParty1;
            Unit.Status = 1;
        }
        if (State.Bt_PlayerParty2 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerParty2;
            Unit.Status = 1;
        }
        if (State.Bt_PlayerParty3 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerParty3;
            Unit.Status = 1;
        }
        if (State.Bt_PlayerParty4 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerParty4;
            Unit.Status = 1;
        }
    }

    /// <summary>
    /// <c>L_00745</c> (<c>:308-425</c>): "Our army advanced to …", the defenders chosen by the
    /// owner (each test reads the owner again: a setup file may move the cursor), the exits without
    /// a battle — Warg, the Zeth barrier, Papaya, a city nobody defends — and <c>FIGT_A</c>.
    /// </summary>
    private async Task DeployAsync()
    {
        await SpeakAsync(113, 311);                                     // \L_052d7
        State.Bt_WallBonus = 0;                                         // :317
        if (Prov.Owner == 2)                                            // :318-320
            await Script.CallFileAsync("TCﾘｰｻﾞｽ2.ADV");
        if (Prov.Owner == 6 && State.F_Japan_State == 10)               // :321-340
        {
            await FleeingSoldierAsync();
            await BattleAsync();                                        // @L_009e6
            return;
        }
        if (Prov.Owner == 6)                                            // :341-343
            await Script.CallFileAsync("TJAPAN2.ADV");
        if (Prov.Owner == 7)                                            // :344-346
            await Script.CallFileAsync("Tｾﾞｽ2.ADV");
        if (Prov.Owner == 4)                                            // :347-349
            await Script.CallFileAsync("Tﾍﾙﾏﾝ2.ADV");
        if (Prov.Owner == 11)                                           // :350-353
            await Script.EventAsync("GAL.ADV", 9);
        if (Prov.Owner == 8)                                            // :354-360
        {
            await Script.CallFileAsync("Tｹｲﾌﾞﾘｽ2.ADV");
            if (State.F_Warg_State == 1)
            {
                State.F_Warg_State = 0;
                return;
            }
        }
        if (Prov.Owner == 5)                                            // :361-376 Patton, inline
        {
            // QUIRK: BFL-14 — neither Ev_PhaseNation nor the army name (string 10) is set.
            State.Bt_EnemyRetreatMode = 1;
            SetPriority(54, 55, 56, 57);
            if (State.F_Aristoles_Helman2ndState > 9)
                State.Tmp0005 = 32;
            PickArmy();                                                 // \L_04f2a
        }
        // QUIRK: CMD-19 — owners 9 and 10 have no setup: the party of the last battle defends.

        if (State.Bt_PlayerParty1 == 0 && State.F_Warg_State == 1)     // :377-380
        {
            State.F_Warg_State = 0;
            return;
        }
        if (State.Bt_PlayerParty1 == 0 && State.F_ZethBarrier_State == 1)   // :381-383 @L_04c8d
        {
            await BarrierAsync();
            return;
        }
        if (State.Bt_PlayerParty1 == 0 && State.F_ZethBarrier_State == 0)   // :384-387 @L_04d24
        {
            State.F_ZethBarrier_State = 1;
            await BarrierFirstMetAsync();
            return;
        }
        if (State.F_Papaya_State == 2)                                  // :388-394
        {
            State.Ev_Entry = 2;
            State.F_Papaya_State = 3;
            State.Ui_CmdAttackAvail = 0;
            await Script.CallFileAsync("Gﾊﾟﾊﾟｲｱ.ADV");
            return;
        }
        if (State.Bt_EnemyParty1 == 0)                                  // :395-418 nobody defends
        {
            State.Ui_CmdAttackAvail = 0;
            await SpeakAsync(113, 399);                                 // "Our army occupied … with no difficulties."
            Prov.Owner = 1;
            State.Tmp0021 = Prov.Owner + 819;
            State.MapTargetX = Prov.MapX;
            State.MapTargetY = Prov.MapY;
            await Script.MapCallAsync(4);
            await Script.MapCallAsync(3);
            await Script.MapCallAsync(0);
            return;
        }
        await BattleAsync();
    }

    /// <summary>
    /// <c>:321-339</c>, JAPAN after its fall (<c>F_Japan_State = 10</c>): commander 73 alone, refilled,
    /// as the "Fleeing Soldier" (the army name and his two name strings, from the player's files).
    /// </summary>
    private async Task FleeingSoldierAsync()
    {
        // QUIRK: CMD-02 — straight to battle, and commander 73 keeps the name for good.
        State.Bt_EnemyRetreatMode = 1;
        SetLiteral(324);                                                // MS 10, "Fleeing Soldier"
        State.Bt_EnemyParty1 = 73;
        State.Bt_EnemyParty2 = 0;
        State.Bt_EnemyParty3 = 0;
        State.Bt_EnemyParty4 = 0;
        State.Cmdr_Cur = 73;
        Cmdr.Status = 1;
        Cmdr.HP = Cmdr.MaxHP;
        Cmdr.Troops = Cmdr.MaxTroops;
        SetLiteral(334);                                                // MS 4, "Fleeing Soldier"
        await LineBreakAsync();                                         // :335 R
        SetLiteral(336);                                                // MS 5, "Foot Soldiers"
        CopyString(State.Cmdr_StrBase + (State.Cmdr_Cur - 1) * 3 + 0, 4);
        CopyString(State.Cmdr_StrBase + (State.Cmdr_Cur - 1) * 3 + 1, 5);
    }

    /// <summary><c>L_009e6</c> (<c>:419-425</c>): the attack battle, then the map redrawn.</summary>
    private async Task BattleAsync()
    {
        State.Bt_Kind = 1;
        await Script.CallFileAsync("FIGT_A.ADV");
        State.Bt_Kind = 0;
        await Script.MapCallAsync(0);
    }

    // ---------------------------------------------------------------------------------------
    // The refusals and the barrier (:3829-4264)
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_04786</c> (<c>:3829-3862</c>): the rebels' first week (<c>F_LeazasRebels_State = 1</c>) —
    /// "our army's organization has been disrupted"; then <c>@L_00205</c>.
    /// </summary>
    private async Task DisruptedRefusalAsync()
    {
        State.CallArg = 6;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 3834);
        await SpeakAsync(111, 3841);
        await SpeakAsync(121, 3846, 3853);
        await SpeakAsync(111, close: false, 3858);
        await LeaveAsync();
    }

    /// <summary>
    /// <c>L_0481f</c> (<c>:3863-3947</c>): story stage 1 — Maris and Sill stop a war on anyone but
    /// the rebels; then <c>@L_00205</c>.
    /// </summary>
    private async Task CivilWarRefusalAsync()
    {
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_052f3
        await SpeakAsync(111, 3868);
        State.CallArg = 6;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 3879);
        await SpeakAsync(113, 3884);                                    // "Maris stopped Rance."
        await SpeakAsync(111, 3889);
        await SpeakAsync(121, 3896, 3905);
        await WaoAsync(322);                                            // \L_05371
        State.CallArg = 5;
        await WaoAsync(321);
        await SpeakAsync(121, 3919);
        await WaoAsync(322);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(112, 3931);                                    // \L_052bb
        await SpeakAsync(111, 3938);
        await SpeakAsync(121, close: false, 3945);
        await LeaveAsync();
    }

    /// <summary>
    /// <c>L_0499c</c> (<c>:3948-4008</c>), province 82: the warning about the Land of Death, then the
    /// attack goes on.
    /// </summary>
    private async Task LandOfDeathAsync()
    {
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_052f3
        await SpeakAsync(111, 3953);
        State.CallArg = 6;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 3966);
        await SpeakAsync(111, 3971);
        await SpeakAsync(121, 3976, 3983);
        await SpeakAsync(111, 3994);
        await SpeakAsync(121, 4003);
        await WaoAsync(312);                                            // \L_05301
        await WaoAsync(322);                                            // \L_05371
    }

    /// <summary>
    /// <c>L_04aaa</c> (<c>:4009-4058</c>): the week's conquest is made (<c>Ui_CmdAttackAvail = 0</c>)
    /// — the army must hold the occupied land; then <c>@L_00205</c>.
    /// </summary>
    private async Task GarrisonRefusalAsync()
    {
        // QUIRK: BFL-02 — only a conquest spends the week's attack.
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_052f3
        await SpeakAsync(111, 4014);
        State.CallArg = 6;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 4023);
        await SpeakAsync(111, 4028);
        await SpeakAsync(121, 4037, 4040, 4049);
        await SpeakAsync(111, close: false, 4056);
        await LeaveAsync();
    }

    /// <summary>
    /// <c>L_04b8a</c> (<c>:4059-4115</c>): story stage 4 — chaos all over the country, no attacks;
    /// then <c>@L_00205</c>.
    /// </summary>
    private async Task ChaosRefusalAsync()
    {
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_052f3
        await SpeakAsync(111, 4064);
        State.CallArg = 6;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 4075);
        await SpeakAsync(111, 4080);
        await SpeakAsync(121, 4085, 4092);
        await SpeakAsync(111, 4101);
        await SpeakAsync(121, 4108);
        await SpeakAsync(111, close: false, 4113);
        await LeaveAsync();
    }

    /// <summary>
    /// <c>L_04c8d</c> (<c>:4116-4149</c>): <c>Tｾﾞｽ2</c> sent the army back (an empty party) once the
    /// barrier is known — the short report, the map redrawn.
    /// </summary>
    private async Task BarrierAsync()
    {
        // QUIRK: BFL-01 — the units stay deployed and the expenses paid.
        if (State.F_Papaya_State == 2)
            State.F_Papaya_State = 1;
        await SpeakAsync(113, 4122, 4128, 4135, 4138);                  // \L_052d7
        await Script.MapCallAsync(0);
    }

    /// <summary>
    /// <c>L_04d24</c> (<c>:4150-4264</c>): the first time — the report, the camera to Leazas Castle
    /// and the talk about the magic barrier in the audience room.
    /// </summary>
    private async Task BarrierFirstMetAsync()
    {
        // QUIRK: BFL-01 — the units stay deployed and the expenses paid.
        if (State.F_Papaya_State == 2)
            State.F_Papaya_State = 1;
        await SpeakAsync(113, 4156, 4162, 4169);                        // \L_052d7
        State.Prov_Cur = 1;                                             // :4172-4180
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(1);
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
        State.CallArg = 1046;
        await WaoAsync(331);                                            // \L_0539b
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_052f3
        await WaoAsync(113);                                            // \L_052d7: "Leazas Castle / Audience Room"
        await EngineAsync("ZM", 32);
        await TextAsync(4186);
        await EngineAsync("ZM", 24);
        await TextAsync(4190);
        await WaoAsync(1);
        await WaoAsync(0);
        await SpeakAsync(111, 4196, 4201);
        State.CallArg = 6;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 4208, 4215);
        await SpeakAsync(111, 4226);
        await SpeakAsync(121, 4235);
        await SpeakAsync(111, 4244);
        await SpeakAsync(121, 4251);
        await SpeakAsync(112, 4256);                                    // \L_052bb
        await WaoAsync(312);                                            // \L_05301
        await WaoAsync(322);                                            // \L_05371
        await WaoAsync(332);                                            // \L_053a9
    }

    /// <summary><c>L_05185</c> (<c>:4365-4408</c>): the owner's name after the city's, then a line break.</summary>
    private async Task OwnerAsync()
    {
        int owner = Prov.Owner.Value;
        if (owner is >= 2 and <= 11)
            await TextAsync(4368 + (owner - 2) * 4);
        await LineBreakAsync();
    }
}
