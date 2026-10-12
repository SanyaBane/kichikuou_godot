using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Tｹｲﾌﾞﾘｽ.ADV</c>, the Kayblis Empire's turn at story stage 3 (part N3, milestone 4; called by
/// <c>MAIN.ADV:594-629</c> while the Kayblis war is 0 or 2), ported one-to-one on the nation template
/// (<c>docs/strategy/survey-nations.md</c> §2.2-§2.4, <c>docs/strategy/stage-3-sweep.md</c>). The first
/// phase of stage 3 (week S) plays the war council <c>Gｹｲﾌﾞﾘｽ</c> 1 and goes straight to the dispatch.
/// Every later phase: the assault <c>Gｹｲﾌﾞﾘｽ</c> 2 when it is pending, the generals' weekly attack
/// marks reset, 22 commanders recovered (Babolat (129) three times, NAT-05), then the event chain —
/// Medusa, Camilla (91), Silky, Warg, Nagi, Kaybwan, Kaybnyan, Satella, Medusa again — whose first
/// scene ends it (<c>Gｼﾙｷｨ</c> 8 alone does not, NAT-26). The dispatch (<c>L_004d1</c>): the Grief Valley
/// (89) guards while the player holds it and Kayblis has not moved (they come back after every
/// battle, NAT-02), the generals introduced and their units, Piar's unit, Kayblis's army at war,
/// <c>Gｹｲﾌﾞﾘｽ</c> 3 once the player holds Silky's Castle (81), the Assault Leaders after week 100, and
/// Monster General (commander 86) given its name back. A province Kayblis's armies reach that is
/// neither his nor the player's is taken without a battle (<see cref="CaptureAsync"/>), with
/// Helman's and Zeth's collapses. The generals' units (<c>:729-1328</c>, step 2) are in
/// <c>KayblisTurn.Generals.cs</c>, Piar's unit and Kayblis's army (<c>:670-728</c>,
/// <c>:1329-1508</c>, step 3) in <c>KayblisTurn.Armies.cs</c>, the Assault Leaders (<c>:326-624</c>,
/// step 5) in <c>KayblisTurn.Assault.cs</c>. The army names (<c>MS 10</c>) and Monster General's name
/// (<c>MS 4</c>) are read from the player's files through <paramref name="scenario"/>.
/// </summary>
public sealed partial class KayblisTurn(OriginalScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "Tｹｲﾌﾞﾘｽ.ADV", scenario)
{
    /// <summary>
    /// The recoveries of <c>:17-62</c>, in order: Camilla (91), Medusa (134), PG-7 (136), PG-9 (137),
    /// PG-X mk2 (138), Babolat (129) three times, Truffle 77 (148), Moreinyan (149), Ouzawa (150),
    /// Redeye (130), Kite (132), Tobroiv (151), Deckmaster (152), Strength (153), Derandron (154),
    /// Nasport (155), Kesselring (133), Kayblis (135), Silky (156), Satella (157), Meglass (158),
    /// Galtia (131).
    /// </summary>
    private static readonly int[] Recovered =
        [91, 134, 136, 137, 138, 129, 129, 129, 148, 149, 150, 130, 132, 151, 152, 153, 154, 155, 133, 135, 156, 157, 158, 131];

    /// <summary>
    /// <c>:289-320</c>: past each week one Assault Leader marches, the 1st (191) on the north list,
    /// the 2nd (192) on the south list, and so on alternating.
    /// </summary>
    private static readonly (int Week, int Leader, bool North)[] AssaultLeaders =
        [(100, 191, true), (110, 192, false), (120, 193, true), (130, 194, false), (140, 195, true), (150, 196, false), (155, 197, true), (160, 198, false)];

    /// <summary><c>Tｹｲﾌﾞﾘｽ.ADV:1-325</c>, up to its <c>%0:</c>.</summary>
    public async Task RunAsync()
    {
        State.Ev_WeeklyEventDone = 0;                                   // :1-2
        State.Bt_EnemyRetreatMode = 2;
        if (State.F_DemonWar_Stage < 20)                                // :3-8 the war council (week S)
        {
            // QUIRK: NAT-04 — the reset below is jumped over; dead in play, since this is the first
            // Kayblis phase and nothing has written the attack marks yet.
            await WeeklyEventAsync("Gｹｲﾌﾞﾘｽ.ADV", 1);
            await DispatchAsync();                                      // @L_004d1
            return;
        }
        if (State.Ev_KayblisAssault == 1)                               // :9-12 the assault
            await Script.EventAsync("Gｹｲﾌﾞﾘｽ.ADV", 2);
        State.Ai_BabolatAtk = 0;                                        // :13-16
        State.Ai_KiteAtk = 0;
        State.Ai_RedeyeAtk = 0;
        State.Ai_GaltiaAtk = 0;
        // QUIRK: NAT-05 — Babolat (129) recovers three times.
        foreach (int commander in Recovered)                            // :17-62
        {
            State.Cmdr_Cur = (Sys3Int)commander;
            Recover();                                                  // \L_02662
        }
        await EventChainAsync();                                        // :63-187
        await DispatchAsync();                                          // L_004d1
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:63-187</c> — the week's event chain; the first scene that applies plays and the
    /// chain ends (<c>@L_004d1</c>), but for <c>Gｼﾙｷｨ</c> 8. Medusa's first scene (no test of her life);
    /// while Medusa (134) lives a roll in five moves her counter on, and her scenes 2, 3 and 4 play
    /// while it equals 2, 4 and 6; Camilla (91) alive (one draw in three either way): <c>Gｶﾐｰﾗ</c> 3
    /// below state 10, 4 and 5 at 10 and 11 on a 1; outside the war Silky's scenes (1, then 2-6 for
    /// each soul freed, 8 when twelve weeks have passed since her second absorption, 9 at state 6);
    /// Warg's scenes 3 and 4 by Stessel's state while she is not met; Nagi's 9 while Kesselring
    /// (133) lives; Kaybwan's and Kaybnyan's 3; Satella's 2 while Satella (157) lives; Medusa's 5 on a
    /// roll in five.
    /// </summary>
    private async Task EventChainAsync()
    {
        if (State.F_Medusa_State == 0 && State.Ev_WeeklyEventDone == 0)    // :63-67
        {
            await Script.EventAsync("Gﾒﾃﾞｨｳｻ.ADV", 1);
            return;
        }
        State.Cmdr_Cur = 134;                                           // :68-89 Medusa
        if (Cmdr.Status != 0)
        {
            Script.Rnd(5);
            if (State.RetVal == 1)
                State.N_Medusa_Events = State.N_Medusa_Events + 1;
            // QUIRK: NAT-25 — the scenes play while the counter equals 2, 4, 6: every week until the next roll.
            if (State.N_Medusa_Events == 2)
            {
                await Script.EventAsync("Gﾒﾃﾞｨｳｻ.ADV", 2);
                return;
            }
            if (State.N_Medusa_Events == 4)
            {
                await Script.EventAsync("Gﾒﾃﾞｨｳｻ.ADV", 3);
                return;
            }
            if (State.N_Medusa_Events == 6)
            {
                await Script.EventAsync("Gﾒﾃﾞｨｳｻ.ADV", 4);
                return;
            }
        }
        State.Cmdr_Cur = 91;                                            // :90-106 Camilla
        Script.Rnd(3);
        if (Cmdr.Status != 0 && State.F_Camilla_State < 10)
        {
            await Script.EventAsync("Gｶﾐｰﾗ.ADV", 3);
            return;
        }
        if (Cmdr.Status != 0 && State.F_Camilla_State == 10 && State.RetVal == 1)
        {
            await Script.EventAsync("Gｶﾐｰﾗ.ADV", 4);
            return;
        }
        if (Cmdr.Status != 0 && State.F_Camilla_State == 11 && State.RetVal == 1)
        {
            await Script.EventAsync("Gｶﾐｰﾗ.ADV", 5);
            return;
        }
        // QUIRK: NAT-16 — dead in play: no scene before it marks the week.
        if (State.Ev_WeeklyEventDone != 0)                              // :107-109
            return;
        if (State.F_Kayblis_War == 0 && State.F_Silky_State == 0)       // :110-148 Silky
        {
            await Script.EventAsync("Gｼﾙｷｨ.ADV", 1);
            return;
        }
        if (State.F_Kayblis_War == 0 && State.F_SoulRedeye == 1 && State.F_Silky_State < 6)
        {
            await Script.EventAsync("Gｼﾙｷｨ.ADV", 2);
            return;
        }
        if (State.F_Kayblis_War == 0 && State.F_SoulPiar == 1 && State.F_Silky_State < 6)
        {
            await Script.EventAsync("Gｼﾙｷｨ.ADV", 3);
            return;
        }
        if (State.F_Kayblis_War == 0 && State.F_SoulMedusa == 1 && State.F_Silky_State < 6)
        {
            await Script.EventAsync("Gｼﾙｷｨ.ADV", 4);
            return;
        }
        if (State.F_Kayblis_War == 0 && State.F_SoulKesselring == 1 && State.F_Silky_State < 6)
        {
            await Script.EventAsync("Gｼﾙｷｨ.ADV", 5);
            return;
        }
        if (State.F_Kayblis_War == 0 && State.F_SoulCamilla == 1 && State.F_Silky_State < 6)
        {
            await Script.EventAsync("Gｼﾙｷｨ.ADV", 6);
            return;
        }
        // QUIRK: NAT-26 — no jump after it: the chain goes on.
        if (State.F_Kayblis_War == 0 && State.F_Silky_State > 1 && State.F_Silky_State < 6 && State.Silky_NextEventWeek < State.Week)
            await Script.EventAsync("Gｼﾙｷｨ.ADV", 8);
        if (State.F_Kayblis_War == 0 && State.F_Silky_State == 6)
        {
            await Script.EventAsync("Gｼﾙｷｨ.ADV", 9);
            return;
        }
        if (State.N_Warg_Stage == 0 && State.F_Stessel_State == 4)      // :149-153
        {
            await Script.EventAsync("Gﾜｰｸﾞ.ADV", 3);
            return;
        }
        State.Cmdr_Cur = 133;                                           // :154-159 Kesselring
        if (Cmdr.Status != 0 && State.F_Nagi_State == 5)
        {
            await Script.EventAsync("Gﾅｷﾞ.ADV", 9);
            return;
        }
        if (State.F_Kaybwan_State == 2)                                 // :160-164
        {
            await Script.EventAsync("Gｹｲﾌﾞﾜﾝ.ADV", 3);
            return;
        }
        if (State.F_Kaybnyan_State == 2)                                // :165-169
        {
            await Script.EventAsync("Gｹｲﾌﾞﾆｬﾝ.ADV", 3);
            return;
        }
        if (State.N_Warg_Stage == 0 && State.F_Stessel_State == 5)      // :170-174
        {
            await Script.EventAsync("Gﾜｰｸﾞ.ADV", 4);
            return;
        }
        State.Cmdr_Cur = 157;                                           // :175-180 Satella
        if (Cmdr.Status != 0 && State.F_Satella_State == 11)
        {
            await Script.EventAsync("Gｻﾃﾗ.ADV", 2);
            return;
        }
        Script.Rnd(5);                                                  // :181-187 Medusa
        State.Cmdr_Cur = 134;
        if (Cmdr.Status != 0 && State.RetVal == 1)
            await Script.EventAsync("Gﾒﾃﾞｨｳｻ.ADV", 5);
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:188-325</c> (<c>L_004d1</c>) — the dispatch: the Grief Valley (89) guards while
    /// the player holds it and Kayblis has not moved; each general introduced once (Babolat in any
    /// case, the others only while the week is not marked) and his unit while he lives
    /// (Galtia's while he has not defected, Kesselring's once the player has taken Bone Forest
    /// (90) or the Red House (85)); <c>Gﾊﾟｲｱｰﾙ</c> 1 once three of Babolat (129), Redeye (130), Galtia
    /// (131), Kite (132) are dead, then Piar's unit while her state is 1-99; Kayblis's army at war; the
    /// army named "Kayblis Unit"; <c>Gｹｲﾌﾞﾘｽ</c> 3 once the player holds Silky's Castle (81); the
    /// Assault Leaders with Monster Generals (87, 88) after week 100; Monster General (86)'s name.
    /// </summary>
    private async Task DispatchAsync()
    {
        while (true)                                                    // :189-192
        {
            State.Prov_Cur = 89;
            if (!(Prov.Owner == 1 && State.F_Kayblis_Moved == 0))
                break;
            // QUIRK: NAT-02 — the guards come back after every battle while Grief Valley (89) holds.
            await GuardsAsync();                                        // @L_00fd0, back to L_004d1
        }
        if (State.F_Babolat_State == 0)                                 // :193-197 whether the week is marked or not
            await WeeklyEventAsync("Gﾊﾞﾎﾞﾗ.ADV", 1);
        State.Cmdr_Cur = 129;                                           // :198-201
        if (Cmdr.Status != 0 && State.F_Babolat_State != 0)
            await BabolatAsync();                                       // \L_0120e
        if (State.F_Redeye_State == 0 && State.Ev_WeeklyEventDone == 0)    // :202-206
            await WeeklyEventAsync("Gﾚｯﾄﾞｱｲ.ADV", 1);
        State.Cmdr_Cur = 130;                                           // :207-210
        if (Cmdr.Status != 0 && State.F_Redeye_State != 0)
            await RedeyeAsync();                                        // \L_01478
        if (State.F_Calory_State == 0 && State.Ev_WeeklyEventDone == 0)    // :211-215
            await WeeklyEventAsync("Gｶｲﾄ.ADV", 1);
        State.Cmdr_Cur = 132;                                           // :216-219
        if (Cmdr.Status != 0 && State.F_Calory_State != 0)
            await KiteAsync();                                          // \L_017e4
        if (State.F_Galtia_State == 0 && State.Ev_WeeklyEventDone == 0)    // :220-224
            await WeeklyEventAsync("Gｶﾞﾙﾃｨｱ.ADV", 1);
        State.Cmdr_Cur = 131;                                           // :225-228
        if (Cmdr.Status != 0 && State.F_Galtia_State != 0 && State.F_Galtia_DefectedToKayblis == 0)
            await GaltiaAsync();                                        // \L_01b5e
        State.Prov_Cur = 90;                                            // :229-234 Bone Forest
        if (Prov.Owner == 1 && State.F_Kesselring_State == 0)
            await WeeklyEventAsync("Gｹｯｾﾙ.ADV", 1);
        State.Prov_Cur = 85;                                            // :235-240 the Red House
        if (Prov.Owner == 1 && State.F_Kesselring_State == 0)
            await WeeklyEventAsync("Gｹｯｾﾙ.ADV", 1);
        State.Cmdr_Cur = 133;                                           // :241-244
        if (Cmdr.Status != 0 && State.F_Kesselring_State != 0)
            await KesselringAsync();                                    // \L_01db2
        State.Tmp0001 = 0;                                              // :245-261 the generals dead
        foreach (int general in new[] { 129, 130, 131, 132 })
        {
            State.Cmdr_Cur = (Sys3Int)general;
            if (Cmdr.Status == 0)
                State.Tmp0001 = State.Tmp0001 + 1;
        }
        if (State.Tmp0001 > 2 && State.F_Piar_State == 0)               // :262-266
            await WeeklyEventAsync("Gﾊﾟｲｱｰﾙ.ADV", 1);
        if (State.F_Piar_State != 0 && State.F_Piar_State != 100)       // :267-269
            await PiarAsync();                                          // \L_010af
        if (State.F_Kayblis_War == 2)                                   // :270-272
            await KayblisArmyAsync();                                   // \L_02038
        SetLiteral(274);                                                // :273-274 MS 10, "Kayblis Unit"
        State.Prov_Cur = 81;                                            // :275-279 Silky's Castle
        if (Prov.Owner == 1 && State.F_Kayblis_Moved == 0)
            await Script.EventAsync("Gｹｲﾌﾞﾘｽ.ADV", 3);
        SetPriority(0, 87, 88);                                         // :280-288
        foreach (var (week, leader, north) in AssaultLeaders)           // :289-320
        {
            if (State.Week > (Sys3Int)week)
            {
                State.Tmp0001 = (Sys3Int)leader;
                await AssaultLeaderAsync(north);                        // \L_00881 / \L_00c91
            }
        }
        State.Cmdr_Cur = 86;                                            // :321-324
        SetLiteral(323);                                                // MS 4, "Monster General"
        CopyString(State.Cmdr_StrBase + (State.Cmdr_Cur - 1) * 3 + 0, 4);
    }

    /// <summary>A scene that marks the week's event done before it plays.</summary>
    private Task WeeklyEventAsync(string file, Sys3Int entry)
    {
        State.Ev_WeeklyEventDone = 1;
        return Script.EventAsync(file, entry);
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:625-669</c> (<c>L_00fd0</c>) — the Grief Valley (89) guards: Lis Guard (186)
    /// alive at full HP with <c>20 + RND(100)</c> troops, retreat mode 1, Kayblis's order with his
    /// portrait (415, at war 466) and <c>FIGT_B</c>; then back to the dispatch.
    /// </summary>
    private async Task GuardsAsync()
    {
        State.Cmdr_Cur = 186;
        Cmdr.Status = 1;
        Cmdr.HP = Cmdr.MaxHP;
        Script.Rnd(100);
        Cmdr.Troops = 20 + State.RetVal;
        State.Bt_EnemyRetreatMode = 1;
        SetPriority(186);
        PickArmy();                                                     // \L_02464
        // QUIRK: NAT-16 — dead: Grief Valley (89) is the player's here.
        if (Prov.Owner != 1)                                            // :642-645
        {
            await CaptureAsync();                                       // \L_026bf, then @L_004d1
            return;
        }
        if (State.F_Kayblis_War == 0)                                   // :646-651
            State.CallArg = 415;
        if (State.F_Kayblis_War == 2)
            State.CallArg = 466;
        await WaoAsync(311);                                            // \L_02a99 Kayblis's portrait
        await WaoAsync(111);                                            // \L_02a45
        await TextAsync(655);                                           // "It's not gonna be good if we lose Grief Valley. …"
        await WaoAsync(1);                                              // \L_02b6a
        await TextAsync(662);                                           // "Go, my guards! …"
        await WaoAsync(1);
        await WaoAsync(0);                                              // \L_02b5d
        await WaoAsync(312);                                            // \L_02aa7
        await Script.CallFileAsync("FIGT_B.ADV");
    }

    /// <summary>The filler's filter (<c>Tｹｲﾌﾞﾘｽ.ADV:1580-1583</c>): under 99 troops and of a class under 5, once slot 1 is taken.</summary>
    protected override bool SkipsCommander() => Cmdr.Troops < 99 && Cmdr.Class < 5 && State.Bt_EnemyParty1 != 0;

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:1601-1614</c> (<c>L_02662</c>) — <c>Cmdr_Cur</c>, unless dead: status 1, 3 HP and
    /// three times its recovery in troops, each up to its maximum.
    /// </summary>
    private void Recover()
    {
        if (Cmdr.Status == 0)
            return;
        Cmdr.Status = 1;
        Cmdr.HP = Cmdr.HP + 3;
        if (Cmdr.HP > Cmdr.MaxHP)
            Cmdr.HP = Cmdr.MaxHP;
        Cmdr.Troops = Cmdr.Troops + Cmdr.Recovery * 3;
        if (Cmdr.Troops > Cmdr.MaxTroops)
            Cmdr.Troops = Cmdr.MaxTroops;
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:1615-1694</c> (<c>L_026bf</c>) — Kayblis takes <c>Prov_Cur</c> without a battle:
    /// the camera to it, owner 8, "The Kayblis Army appeared in &lt;province&gt; and took control of it
    /// in the blink of an eye." with its key wait, the flag redrawn. Lang Bau (45) while Helman stands
    /// brings Helman down; a Zeth tower — Chizuko's (68), Papaya's (70), Nagi's (67), Magic's (66) —
    /// kills its minister, Chizuko (101), Papaya (104), Nagi (107), Magic (110), and moves Zeth's story
    /// on (and with the four dead brings Zeth down); a Zeth general's city — Adam's Fortress (58),
    /// Tape (61), Amber Castle (65), Old Zeth (63) — kills Saias (113), Uspira (117), Kabachahn (121),
    /// Alex (125). Every army of the file captures here; internal for the differential tests.
    /// </summary>
    internal async Task CaptureAsync()
    {
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(1);
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
        await Script.MapCallAsync(10);
        Prov.Owner = 8;
        CopyString(8, State.Prov_StrBase + State.Prov_Cur - 1);         // MM 8, the province's name
        await WaoAsync(113);                                            // \L_02a7d
        await TextAsync(1628);                                          // "The Kayblis Army appeared in <province> and …"
        await WaoAsync(1);                                              // \L_02b6a
        await WaoAsync(0);                                              // \L_02b5d
        State.Tmp0021 = Prov.Owner + 819;
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(4);
        await Script.MapCallAsync(3);
        await Script.MapCallAsync(0);
        // QUIRK: NAT-27 — "≠ 2" also brings down a Helman that never went to war.
        if (State.F_Helman_War != 2 && State.Prov_Cur == 45)            // :1647-1649
            await HelmanFallsAsync();                                   // \L_0291b
        // QUIRK: NAT-01 — a collapse leaves Prov_Cur at Shangri-La (29) or Adam's Fortress (58),
        // and the tests below read it: Zeth's collapse goes on to kill Saias (113).
        await KillMinisterAsync(68, 101);                               // :1650-1669
        await KillMinisterAsync(70, 104);
        await KillMinisterAsync(67, 107);
        await KillMinisterAsync(66, 110);
        KillGeneral(58, 113);                                           // :1670-1693
        KillGeneral(61, 117);
        KillGeneral(65, 121);
        KillGeneral(63, 125);
    }

    /// <summary>
    /// <c>{Prov_Cur = tower: !Cmdr_Cur : minister! !Cmdr_Status : 0! \L_02897:}</c> — the tower's
    /// minister dies and Zeth's story moves on.
    /// </summary>
    private async Task KillMinisterAsync(int tower, int minister)
    {
        if (State.Prov_Cur != (Sys3Int)tower)
            return;
        State.Cmdr_Cur = (Sys3Int)minister;
        Cmdr.Status = 0;
        await MinisterKilledAsync();                                    // \L_02897
    }

    /// <summary><c>{Prov_Cur = city: !Cmdr_Cur : general! {Cmdr_Status \ 0: !Cmdr_Status : 0!}}</c> — the city's general dies.</summary>
    private void KillGeneral(int city, int general)
    {
        if (State.Prov_Cur != (Sys3Int)city)
            return;
        State.Cmdr_Cur = (Sys3Int)general;
        if (Cmdr.Status != 0)
            Cmdr.Status = 0;
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:1695-1715</c> (<c>L_02897</c>) — after a minister's death: below state 7 Zeth's
    /// story jumps to 11 (Ragarl's ritual to 6, Papaya to 3); the barrier is down; with Chizuko (101),
    /// Papaya (104), Nagi (107) and Magic (110) all dead and Zeth not fallen yet, Zeth falls.
    /// </summary>
    private async Task MinisterKilledAsync()
    {
        if (State.F_Zeth_State < 7)
        {
            State.F_Zeth_State = 11;
            State.F_RagarlRitual_State = 6;
            State.F_Papaya_State = 3;
        }
        State.F_ZethBarrier_State = 2;
        State.Cmdr_Cur = 101;
        if (Cmdr.Status != 0)
            return;
        State.Cmdr_Cur = 104;
        if (Cmdr.Status != 0)
            return;
        State.Cmdr_Cur = 107;
        if (Cmdr.Status != 0)
            return;
        State.Cmdr_Cur = 110;
        // QUIRK: NAT-27 — "≠ 2" also brings down a Zeth that never went to war.
        if (State.F_Zeth_War != 2 && Cmdr.Status == 0)
            await ZethFallsAsync();                                     // \L_029b2
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:1716-1747</c> (<c>L_0291b</c>) — Helman falls (<c>F_Helman_War := 2</c>, fallen to
    /// Kayblis), "The Helman Empire, a long prospering nation, has now collapsed."; its provinces were
    /// to go to Kayblis, but the loop stops after Shangri-La (29) (NAT-01).
    /// </summary>
    private async Task HelmanFallsAsync()
    {
        State.F_Helman_War = 2;
        State.F_Helman_FallCause = 1;
        await WaoAsync(113);                                            // \L_02a7d
        await TextAsync(1721);                                          // "The Helman Empire, a long prospering nation, …"
        await WaoAsync(1);                                              // \L_02b6a
        await WaoAsync(0);                                              // \L_02b5d
        State.Prov_Cur = 28;
        await HandOverAsync(4, 54);                                     // L_02950
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:1748-1778</c> (<c>L_029b2</c>) — Zeth falls (<c>F_Zeth_War := 2</c>), "The Kingdom
    /// of Zeth, a prosperous magic nation, has now collapsed."; its provinces were to go to Kayblis,
    /// but the loop stops after Adam's Fortress (58) (NAT-01).
    /// </summary>
    private async Task ZethFallsAsync()
    {
        State.F_Zeth_War = 2;
        await WaoAsync(113);                                            // \L_02a7d
        await TextAsync(1752);                                          // "The Kingdom of Zeth, a prosperous magic nation, …"
        await WaoAsync(1);                                              // \L_02b6a
        await WaoAsync(0);                                              // \L_02b5d
        State.Prov_Cur = 57;
        await HandOverAsync(7, 76);                                     // L_029e3
    }

    /// <summary>
    /// <c>L_02950</c> / <c>L_029e3</c> — the loop meant to give Kayblis every province of the fallen
    /// nation (owner <paramref name="nation"/>) after <c>Prov_Cur</c> up to <paramref name="last"/>,
    /// each flag redrawn, then the map (op 3). Its body ends with <c>\0:</c> before the jump back, so
    /// it returns after the first province.
    /// </summary>
    private async Task HandOverAsync(int nation, int last)
    {
        State.Prov_Cur = State.Prov_Cur + 1;
        if (State.Prov_Cur > (Sys3Int)last)
        {
            await Script.MapCallAsync(3);                               // @L_029a5 / @L_02a38
            return;
        }
        if (Prov.Owner == (Sys3Int)nation)
            Prov.Owner = 8;
        State.Tmp0021 = Prov.Owner + 819;
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(4);
        // QUIRK: NAT-01 — \0: before @L_02950: one province only.
    }
}
