using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Cﾃｷﾄｼ.ADV:604-3648</c>, the negotiations with the independent cities (part P4, milestone 3
/// step 4; <c>docs/strategy/stage-2-sweep.md</c>, the per-city table): after the mayor is called
/// out, each city's own talk and Rance's answer. A city that gives in becomes Leazas's (owner 1),
/// spends the week's attack and loses its heroes (<c>L_05109</c>); the others send the player back
/// to the header, to the city's menu, or out of the file.
/// </summary>
public sealed partial class EnemyCityCommand
{
    /// <summary>
    /// <c>:604-633</c>: the city's talk by province; a province the list does not name falls into
    /// Mars's (<c>L_00e20</c>). Null sends the player back to the city's menu.
    /// </summary>
    private Task<Outcome?> TalkAsync() => State.Prov_Cur.Value switch
    {
        17 => RockearthAsync(),                                         // L_013dd
        18 => IceAsync(),                                               // L_01c32
        19 => DiphtheriaAsync(),                                        // L_01331
        20 => LazealAsync(),                                            // L_01e20
        23 => RedAsync(),                                               // L_02624
        24 => ZeoAsync(),                                               // L_032cd
        25 => MLandAsync(),                                             // L_034dd
        26 => HannaAsync(),                                             // L_03d9e
        28 => PortugalAsync(),                                          // L_02a0e
        _ => MarsAsync(),                                               // L_00e20, Mars (22) and the rest
    };

    /// <summary>
    /// A negotiation's question (<c>L_01124</c>, <c>L_0190a</c>, <c>L_02062</c>, <c>L_027de</c>,
    /// <c>L_02bfb</c>, <c>L_0386a</c>, <c>L_040ee</c>): the release wait, then the two answers
    /// registered at <paramref name="first"/> and <paramref name="second"/> with a line break between
    /// them; a cancel asks again. True for the first answer.
    /// </summary>
    private async Task<bool> AnswerAsync(int first, int second)
    {
        while (true)
        {
            await WaitForReleaseAsync();
            await LineBreakAsync();                                     // R between the answers
            int choice = await SelectAsync([MenuItem(first), MenuItem(second)]);
            if (choice != 0)
                return choice == 1;
        }
    }

    /// <summary><c>!Cmdr_Cur : n! \L_05109</c> (<c>:4343-4346</c>): the city's hero is gone — no status, no troops.</summary>
    private void RemoveCommander(int commander)
    {
        State.Cmdr_Cur = (Sys3Int)commander;
        Cmdr.Status = 0;
        Cmdr.Troops = 0;
    }

    /// <summary><c>!Tmp0021 : Prov_Owner + 819!</c> and <c>M.ADV</c> ops 4 and 3: the new owner's flag set on the city.</summary>
    private async Task OwnerFlagAsync()
    {
        State.Tmp0021 = Prov.Owner + 819;
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(4);
        await Script.MapCallAsync(3);
    }

    /// <summary>
    /// <c>L_00e20</c> (<c>:634-920</c>), Mars (22): the talk ends in a question —
    /// "Reject" (<c>L_01164</c>) goes back to the header; "Make a General" (<c>L_0120c</c>): Mars the
    /// Great (unit 26) joins, <c>N_Mars_Score := 2</c>, the city is taken and Mars the Great
    /// (commander 71) is gone. Unlike Use Force, no <c>Gｶｾｲ</c> 1.
    /// </summary>
    private async Task<Outcome?> MarsAsync()
    {
        await SpeakAsync(121, 637);                                     // \L_0530f
        State.CallArg = 414;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(111, 644);                                     // \L_0529f
        await WaoAsync(322);                                            // \L_05371
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 654);
        await SpeakAsync(123, 663);                                     // \L_05347
        await WaoAsync(322);
        State.CallArg = 414;
        await WaoAsync(321);
        await SpeakAsync(121, 671, 680);
        await SpeakAsync(123, 689);
        await SpeakAsync(121, 694);
        await SpeakAsync(111, 703, 706);
        await SpeakAsync(121, 711);
        await SpeakAsync(111, 716);
        await SpeakAsync(121, 721, 728);
        await SpeakAsync(111, 739);
        await SpeakAsync(121, 746);
        await SpeakAsync(123, 751);
        await SpeakAsync(121, 760, 763);
        await SpeakAsync(111, 768);
        await SpeakAsync(121, 773, 778, 785, 794);
        await SpeakAsync(111, 801);

        if (await AnswerAsync(810, 814))                                // L_01124: Reject / Make a General
        {
            await SpeakAsync(111, 822);                                 // L_01164
            await SpeakAsync(121, 829, 832, 835);
            await SpeakAsync(123, 842);
            await WaoAsync(322);
            await SpeakAsync(111, 852);
            return Outcome.Header;                                      // @L_00078
        }

        await SpeakAsync(111, 859);                                     // L_0120c
        await SpeakAsync(121, 870, 875, 882);
        await SpeakAsync(111, 889);
        await SpeakAsync(122, 894);                                     // \L_0532b
        State.Unit_Cur = 26;                                            // :897-903
        Unit.Status = 1;
        State.N_Mars_Score = 2;
        Prov.Owner = 1;
        State.Ui_CmdAttackAvail = 0;
        RemoveCommander(71);
        await SpeakAsync(113, 906);                                     // \L_052d7
        await Script.MapCallAsync(0);
        await OwnerFlagAsync();
        await Script.MapCallAsync(10);
        return Outcome.Done;
    }

    /// <summary><c>L_01331</c> (<c>:921-956</c>), Diphtheria (19): the mayor never comes out; back to the city's menu.</summary>
    private async Task<Outcome?> DiphtheriaAsync()
    {
        await SpeakAsync(113, 924);                                     // \L_052d7
        await SpeakAsync(111, 929);                                     // \L_0529f
        State.CallArg = 6;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 936);                                     // \L_0530f
        await SpeakAsync(113, 941);
        await SpeakAsync(111, 946);
        await SpeakAsync(121, 953);
        return null;                                                    // @L_00b35
    }

    /// <summary>
    /// <c>L_013dd</c> (<c>:957-1430</c>), Rockearth (17): the talk ends in a question —
    /// "I'll approve you" (<c>L_01a49</c>): <c>F_DX_State := 1</c>, the city is taken, Rockearth
    /// Hero (commanders 68, 69) gone, Gold + 500; "Disgusting, die" (<c>L_0194a</c>):
    /// <c>F_DX_State := 100</c> (no talks ever again, <c>:529-551</c>) and back to the header.
    /// </summary>
    private async Task<Outcome?> RockearthAsync()
    {
        await SpeakAsync(113, 960);                                     // \L_052d7
        State.CallArg = 443;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 969);                                     // \L_0530f
        await SpeakAsync(111, 978);                                     // \L_0529f
        await SpeakAsync(121, 987, 990, 995);
        await SpeakAsync(111, 1006);
        await WaoAsync(322);                                            // \L_05371
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 1014);
        await SpeakAsync(123, 1023);                                    // \L_05347
        await SpeakAsync(111, 1028);
        await SpeakAsync(121, 1035, 1044);
        await SpeakAsync(111, 1055);
        await SpeakAsync(121, 1062);
        await WaoAsync(322);
        State.CallArg = 443;
        await WaoAsync(321);
        await SpeakAsync(121, 1072);
        await SpeakAsync(111, 1077);
        await SpeakAsync(121, 1082);
        await SpeakAsync(112, 1087);                                    // \L_052bb
        await SpeakAsync(111, 1094);
        await SpeakAsync(121, 1103);
        await SpeakAsync(111, 1112);
        await SpeakAsync(121, 1119, 1122);
        await SpeakAsync(111, 1129);
        await SpeakAsync(121, 1134, 1143, 1150);
        await SpeakAsync(111, 1161);
        await SpeakAsync(121, 1166, 1169);
        await SpeakAsync(111, 1176);
        await SpeakAsync(121, 1181);
        await SpeakAsync(123, 1188);
        await SpeakAsync(121, 1197);
        await SpeakAsync(123, 1202);
        await SpeakAsync(111, 1211);
        await SpeakAsync(121, 1216, 1219, 1226, 1233, 1242);
        await SpeakAsync(111, 1249);

        if (!await AnswerAsync(1258, 1262))                             // L_0190a: I'll approve you / Disgusting, die
        {
            State.F_DX_State = 100;                                     // L_0194a
            await SpeakAsync(111, 1271);
            await SpeakAsync(121, 1280);
            await SpeakAsync(113, 1285);
            await SpeakAsync(121, 1290, 1293);
            await SpeakAsync(111, 1302);
            await WaoAsync(322);
            await SpeakAsync(123, 1310);
            await SpeakAsync(113, 1317);
            return Outcome.Header;                                      // @L_00078
        }

        State.F_DX_State = 1;                                           // L_01a49
        await SpeakAsync(111, 1327);
        await SpeakAsync(121, 1336, 1345);
        await SpeakAsync(111, 1352);
        await SpeakAsync(121, 1357, 1360);
        await SpeakAsync(111, 1367);
        await SpeakAsync(121, 1372);
        await SpeakAsync(111, 1381, 1384);
        await SpeakAsync(121, 1393);
        Prov.Owner = 1;                                                 // :1400-1405
        State.Ui_CmdAttackAvail = 0;
        RemoveCommander(68);
        RemoveCommander(69);
        await SpeakAsync(113, 1408, 1413);
        State.Gold = State.Gold + 500;                                  // :1418
        await Script.MapCallAsync(0);
        await OwnerFlagAsync();
        await Script.MapCallAsync(0);
        return Outcome.Done;
    }

    /// <summary><c>L_01c32</c> (<c>:1431-1535</c>), Ice (18): the city gives in at once; Ice Hero (commanders 66, 67) gone.</summary>
    private async Task<Outcome?> IceAsync()
    {
        await SpeakAsync(113, 1434);                                    // \L_052d7
        State.CallArg = 411;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 1443);                                    // \L_0530f
        await SpeakAsync(113, 1450);
        await SpeakAsync(111, 1459);                                    // \L_0529f
        await SpeakAsync(121, 1466);
        await SpeakAsync(111, 1471, 1474);
        await SpeakAsync(121, 1479, 1482);
        await SpeakAsync(111, 1489, 1492);
        await SpeakAsync(113, 1499);
        await SpeakAsync(121, 1506, 1509);
        await WaoAsync(322);                                            // \L_05371
        await WaoAsync(312);                                            // \L_05301
        await WaoAsync(332);                                            // \L_053a9
        Prov.Owner = 1;                                                 // :1515-1520
        State.Ui_CmdAttackAvail = 0;
        RemoveCommander(66);
        RemoveCommander(67);
        await SpeakAsync(113, 1523);
        await OwnerFlagAsync();
        await Script.MapCallAsync(10);
        return Outcome.Done;
    }

    /// <summary>
    /// <c>L_01e20</c> (<c>:1536-1981</c>), Lazeal (20): the first talk ends in a question — "No"
    /// (<c>L_020a2</c>) goes back to the header, "I'll wait" (<c>L_02103</c>) sets
    /// <c>DXTalk_RetryWeek := Week + 4</c> and leaves the file, the attack not spent. While that week
    /// has not passed the mayor holds Rance to it (<c>L_023b8</c>, the file left); once
    /// <c>DXTalk_RetryWeek &lt; Week</c>, <c>Gﾚｨﾘｨ</c> 1 and the city is taken (<c>L_025d1</c>),
    /// Lazeal Hero (commander 65) gone.
    /// </summary>
    private async Task<Outcome?> LazealAsync()
    {
        if (State.DXTalk_RetryWeek != 0 && State.DXTalk_RetryWeek < State.Week)  // :1537-1539
        {
            await Script.EventAsync("Gﾚｨﾘｨ.ADV", 1);                    // L_025d1
            Prov.Owner = 1;
            State.Ui_CmdAttackAvail = 0;
            RemoveCommander(65);
            await Script.MapCallAsync(0);
            await OwnerFlagAsync();
            await Script.MapCallAsync(10);
            return Outcome.Done;
        }
        if (State.DXTalk_RetryWeek != 0)                                // :1540-1542
            return await LazealWaitingAsync();                          // @L_023b8

        await SpeakAsync(113, 1545);                                    // \L_052d7
        State.CallArg = 411;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 1554);                                    // \L_0530f
        await SpeakAsync(111, 1563);                                    // \L_0529f
        await SpeakAsync(113, 1568);
        await WaoAsync(322);                                            // \L_05371
        State.CallArg = 65;
        await WaoAsync(321);
        await SpeakAsync(113, 1578);
        await SpeakAsync(112, 1587);                                    // \L_052bb
        await WaoAsync(322);
        State.CallArg = 411;
        await WaoAsync(321);
        await SpeakAsync(121, 1597);
        await SpeakAsync(111, 1602);
        await SpeakAsync(121, 1607);
        await SpeakAsync(111, 1614, 1617, 1622);
        await SpeakAsync(121, 1633, 1638);
        await SpeakAsync(111, 1645);
        await SpeakAsync(121, 1650);

        if (await AnswerAsync(1665, 1669))                              // L_02062: No / I'll wait
        {
            await SpeakAsync(111, 1677);                                // L_020a2
            await SpeakAsync(121, 1684);
            await WaoAsync(322);
            await WaoAsync(312);                                        // \L_05301
            await SpeakAsync(113, 1691);
            return Outcome.Header;                                      // @L_00078
        }

        State.DXTalk_RetryWeek = State.Week + 4;                        // L_02103
        await SpeakAsync(111, 1699);
        await SpeakAsync(121, 1706);
        await SpeakAsync(111, 1711);
        State.CallArg = 65;
        await WaoAsync(321);
        await SpeakAsync(113, 1724);
        await SpeakAsync(121, 1731);
        State.CallArg = 411;
        await WaoAsync(321);
        await SpeakAsync(121, 1738);
        await SpeakAsync(111, 1745);
        await SpeakAsync(121, 1754);
        State.CallArg = 65;
        await WaoAsync(321);
        await SpeakAsync(121, 1761);
        State.CallArg = 411;
        await WaoAsync(321);
        await SpeakAsync(121, 1770);
        await SpeakAsync(111, 1775);
        State.CallArg = 65;
        await WaoAsync(321);
        await SpeakAsync(111, 1786);
        await SpeakAsync(121, 1795);
        await WaoAsync(322);
        await SpeakAsync(123, 1803);                                    // \L_05347
        await SpeakAsync(111, 1812);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 1823);
        await SpeakAsync(111, 1830);
        await SpeakAsync(121, 1835);
        await WaoAsync(322);
        await WaoAsync(332);                                            // \L_053a9
        await WaoAsync(312);
        return Outcome.Done;
    }

    /// <summary><c>L_023b8</c> (<c>:1846-1962</c>): Lazeal (20) before its week — the mayor holds Rance to his promise to wait; the file is left.</summary>
    private async Task<Outcome?> LazealWaitingAsync()
    {
        await SpeakAsync(113, 1849);                                    // \L_052d7
        State.CallArg = 411;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 1858);                                    // \L_0530f
        await SpeakAsync(111, 1867);                                    // \L_0529f
        await SpeakAsync(121, 1872);
        await SpeakAsync(111, 1879, 1882);
        await SpeakAsync(121, 1891, 1896);
        await SpeakAsync(111, 1903);
        await SpeakAsync(121, 1908, 1911);
        await WaoAsync(322);                                            // \L_05371
        await SpeakAsync(123, 1917);                                    // \L_05347
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 1924);
        await SpeakAsync(111, 1935);
        await SpeakAsync(121, 1940);
        await SpeakAsync(111, 1945);
        await SpeakAsync(121, 1952);
        await WaoAsync(322);
        await WaoAsync(312);                                            // \L_05301
        await WaoAsync(332);                                            // \L_053a9
        return Outcome.Done;
    }

    /// <summary>
    /// <c>L_02624</c> (<c>:1982-2202</c>), Red (23): "I promise" (<c>L_0281e</c>) takes the city, Red
    /// Hero (commanders 63, 64) gone; "I can't promise" (<c>L_02908</c>) goes back to the header.
    /// </summary>
    private async Task<Outcome?> RedAsync()
    {
        await SpeakAsync(113, 1985);                                    // \L_052d7
        State.CallArg = 411;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 1994);                                    // \L_0530f
        await SpeakAsync(113, 2001);
        await SpeakAsync(111, 2008);                                    // \L_0529f
        await SpeakAsync(113, 2015);
        await SpeakAsync(111, 2022);
        await SpeakAsync(121, 2029);
        await SpeakAsync(111, 2034);
        await SpeakAsync(121, 2039);
        await SpeakAsync(111, 2048);
        await SpeakAsync(121, 2053, 2056);
        await SpeakAsync(111, 2065);
        await SpeakAsync(121, 2070);

        if (!await AnswerAsync(2085, 2089))                             // L_027de: I promise / I can't promise
        {
            await SpeakAsync(111, 2149);                                // L_02908
            await SpeakAsync(121, 2154);
            await SpeakAsync(111, 2159);
            await SpeakAsync(121, 2168);
            await SpeakAsync(111, 2175);
            await SpeakAsync(121, 2180);
            await WaoAsync(322);                                        // \L_05371
            await SpeakAsync(113, 2188);
            await SpeakAsync(111, 2195);
            await WaoAsync(312);                                        // \L_05301
            await WaoAsync(332);                                        // \L_053a9
            return Outcome.Header;                                      // @L_00078
        }

        await SpeakAsync(112, 2097);                                    // L_0281e, \L_052bb
        await SpeakAsync(111, 2106);
        await SpeakAsync(121, 2113);
        await WaoAsync(312);                                            // \L_05301
        await WaoAsync(322);                                            // \L_05371
        await WaoAsync(332);                                            // \L_053a9
        Prov.Owner = 1;                                                 // :2125-2130
        State.Ui_CmdAttackAvail = 0;
        RemoveCommander(63);
        RemoveCommander(64);
        await SpeakAsync(113, 2133);
        await OwnerFlagAsync();
        await Script.MapCallAsync(10);
        return Outcome.Done;
    }

    /// <summary>
    /// <c>L_02a0e</c> (<c>:2203-2430</c>), Portugal (28): the demand of 90 with the national budget
    /// shown; "Pay and Conquer" (<c>L_02cf3</c>) is refused below 90 gold (the answers again), else
    /// pays 90 (booked to <c>Hist_SundryExp</c>); "Don't Pay" (<c>L_02c3b</c>) ends the same way for
    /// free (CMD-03): the city is taken, Portugal Hero (commander 17) gone (<c>L_02dae</c>).
    /// </summary>
    private async Task<Outcome?> PortugalAsync()
    {
        await SpeakAsync(113, 2206);                                    // \L_052d7
        State.CallArg = 411;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 2215);                                    // \L_0530f
        await SpeakAsync(111, 2222);                                    // \L_0529f
        await SpeakAsync(121, 2229);
        await SpeakAsync(111, 2236);
        await SpeakAsync(121, 2243, 2246);
        await SpeakAsync(111, 2253);
        await SpeakAsync(121, 2258);
        await SpeakAsync(111, 2269, 2274);
        await SpeakAsync(113, 2283);
        await SpeakAsync(121, 2292, 2295);
        await WaoAsync(113);
        await ValuesAsync(2306, State.Gold);                            // the demand (90) and the budget (Gold)

        while (true)
        {
            if (await AnswerAsync(2319, 2323))                          // L_02bfb: Don't Pay / Pay and Conquer
            {
                // QUIRK: CMD-03 — not paying ends as paying does.
                await WaoAsync(0);                                      // L_02c3b
                await SpeakAsync(111, 2332);
                await SpeakAsync(121, 2339);
                await SpeakAsync(111, 2346);
                await SpeakAsync(121, 2355);
                await SpeakAsync(111, 2364);
                break;                                                  // @L_02dae
            }
            await WaoAsync(0);                                          // L_02cf3
            if (State.Gold < 90)
            {
                await SpeakAsync(112, 2375);                            // \L_052bb
                continue;                                               // @L_02bfb
            }
            State.Gold = State.Gold - 90;
            Hist.SundryExp = Hist.SundryExp + 90;                       // QUIRK: CMD-14
            await SpeakAsync(111, 2386, 2389);
            State.CallArg = 6;
            await WaoAsync(321);
            await SpeakAsync(121, 2396);
            State.CallArg = 411;
            await WaoAsync(321);
            await SpeakAsync(121, 2403);
            break;
        }

        await WaoAsync(312);                                            // L_02dae, \L_05301
        await WaoAsync(322);                                            // \L_05371
        await WaoAsync(332);                                            // \L_053a9
        Prov.Owner = 1;
        State.Ui_CmdAttackAvail = 0;
        RemoveCommander(17);
        await SpeakAsync(113, 2416);
        await OwnerFlagAsync();
        await Script.MapCallAsync(10);
        return Outcome.Done;
    }

    /// <summary><c>L_032cd</c> (<c>:2698-2810</c>), Zeo (24): a refusal; the file is left.</summary>
    private async Task<Outcome?> ZeoAsync()
    {
        await SpeakAsync(112, 2701);                                    // \L_052bb
        await SpeakAsync(113, 2706);                                    // \L_052d7
        await SpeakAsync(112, 2711);
        await SpeakAsync(113, 2720);
        State.CallArg = 6;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 2727);                                    // \L_0530f
        await SpeakAsync(111, 2732);                                    // \L_0529f
        await SpeakAsync(121, 2741);
        await SpeakAsync(111, 2748);
        await SpeakAsync(121, 2753);
        await SpeakAsync(113, 2760, 2763);
        await WaoAsync(322);                                            // \L_05371
        await SpeakAsync(111, 2771);
        await SpeakAsync(113, 2776);
        await SpeakAsync(112, 2783);
        await SpeakAsync(113, 2788);
        await SpeakAsync(111, 2795);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 2804);
        await WaoAsync(322);
        await WaoAsync(312);                                            // \L_05301
        await WaoAsync(332);                                            // \L_053a9
        return Outcome.Done;
    }

    /// <summary>
    /// <c>L_034dd</c> (<c>:2811-3310</c>), M Land (25): "No" (<c>L_038aa</c>)
    /// leaves the file; "Maintain the Park" (<c>L_03972</c>): <c>F_Sayori_State := 1</c>,
    /// <c>F_MLand_Funded := 1</c> (20 gold a week, <c>MAIN.ADV:190</c>), the city is taken, Sayori
    /// (commander 11) and M Land Hero (12) gone.
    /// </summary>
    private async Task<Outcome?> MLandAsync()
    {
        await SpeakAsync(111, 2814);                                    // \L_0529f
        await SpeakAsync(113, 2819);                                    // \L_052d7
        State.CallArg = 12;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(112, 2828);                                    // \L_052bb
        await SpeakAsync(121, 2833);                                    // \L_0530f
        await SpeakAsync(112, 2842);
        await SpeakAsync(111, 2849);
        await SpeakAsync(121, 2860);
        await SpeakAsync(111, 2871);
        await SpeakAsync(121, 2878, 2883, 2892, 2899, 2902, 2907);
        await SpeakAsync(111, 2916, 2919);
        await SpeakAsync(121, 2928);
        await SpeakAsync(111, 2937);
        await SpeakAsync(121, 2942, 2945);
        await SpeakAsync(111, 2954);
        await WaoAsync(322);                                            // \L_05371
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 2962);
        await SpeakAsync(123, 2967);                                    // \L_05347
        await SpeakAsync(121, 2972, 2981);
        await SpeakAsync(111, 2992);
        await WaoAsync(322);
        State.CallArg = 12;
        await WaoAsync(321);
        await SpeakAsync(121, 3006, 3009);

        if (await AnswerAsync(3022, 3026))                              // L_0386a: No / Maintain the Park
        {
            await SpeakAsync(111, 3034);                                // L_038aa
            await SpeakAsync(121, 3045, 3048);
            await SpeakAsync(111, 3055);
            await SpeakAsync(121, 3060);
            await SpeakAsync(123, 3069);
            await Script.MapCallAsync(0);
            return Outcome.Done;
        }

        State.F_Sayori_State = 1;                                       // L_03972
        State.F_MLand_Funded = 1;
        await SpeakAsync(111, 3082);
        await SpeakAsync(121, 3089);
        await SpeakAsync(111, 3094, 3097);
        await SpeakAsync(121, 3102);
        await SpeakAsync(111, 3107);
        await SpeakAsync(121, 3114);
        await SpeakAsync(111, 3119, 3124);
        await SpeakAsync(121, 3135);
        await SpeakAsync(123, 3140);
        await SpeakAsync(121, 3147);
        await SpeakAsync(112, 3158);
        await SpeakAsync(121, 3167);
        await SpeakAsync(111, 3174);
        await SpeakAsync(121, 3183);
        await WaoAsync(322);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 3191);
        await WaoAsync(322);
        State.CallArg = 12;
        await WaoAsync(321);
        await SpeakAsync(121, 3205);
        await SpeakAsync(111, 3210);
        await WaoAsync(322);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 3220);
        await WaoAsync(322);
        await SpeakAsync(111, 3230);
        State.CallArg = 12;
        await WaoAsync(321);
        await SpeakAsync(113, 3239);
        await SpeakAsync(121, 3246);
        await SpeakAsync(123, 3251);
        await SpeakAsync(112, 3260);
        await SpeakAsync(111, 3269);
        await SpeakAsync(113, 3276);
        await SpeakAsync(121, 3283);
        Prov.Owner = 1;                                                 // :3286-3291
        State.Ui_CmdAttackAvail = 0;
        RemoveCommander(11);
        RemoveCommander(12);
        await SpeakAsync(113, 3294);
        await Script.MapCallAsync(0);
        await OwnerFlagAsync();
        await Script.MapCallAsync(10);
        return Outcome.Done;
    }

    /// <summary>
    /// <c>L_03d9e</c> (<c>:3311-3648</c>), Hanna (26): the demand of 30 with the national budget
    /// shown; "Pay" (<c>L_0412e</c>) is refused below 29 gold (the answers again), else costs 30
    /// (CMD-04; booked to <c>Hist_SundryExp</c>) and takes the city — with Gengorou (commander 8)
    /// alive <c>F_Shinoda_State := 1</c> and his talk —, Gengorou and Hanna Hero (9, 10) gone;
    /// "Refuse" (<c>L_042f5</c>) leaves the file.
    /// </summary>
    private async Task<Outcome?> HannaAsync()
    {
        await SpeakAsync(112, 3314);                                    // \L_052bb
        await SpeakAsync(113, 3319);                                    // \L_052d7
        State.CallArg = 411;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 3328);                                    // \L_0530f
        await SpeakAsync(113, 3335);
        await SpeakAsync(122, 3344);                                    // \L_0532b
        await SpeakAsync(111, 3349);                                    // \L_0529f
        await SpeakAsync(121, 3354, 3357);
        await SpeakAsync(111, 3364);
        await SpeakAsync(121, 3369);
        await SpeakAsync(111, 3378);
        await SpeakAsync(113, 3383);
        await SpeakAsync(111, 3390);
        await SpeakAsync(121, 3399);
        await SpeakAsync(111, 3404);
        await SpeakAsync(122, 3415);
        await SpeakAsync(121, 3424);
        await SpeakAsync(111, 3433);
        await SpeakAsync(121, 3440);
        await SpeakAsync(113, 3449);
        await SpeakAsync(111, 3458);
        await SpeakAsync(121, 3467);
        await SpeakAsync(111, 3474);
        await SpeakAsync(121, 3479, 3482);
        await WaoAsync(113);
        await ValuesAsync(3491, State.Gold);                            // the demand (30) and the budget (Gold)

        while (true)
        {
            if (!await AnswerAsync(3504, 3508))                         // L_040ee: Pay / Refuse
            {
                await WaoAsync(0);                                      // L_042f5
                await SpeakAsync(111, 3611);
                await SpeakAsync(121, 3616, 3621);
                await SpeakAsync(113, 3630);
                await WaoAsync(322);                                    // \L_05371
                await SpeakAsync(112, 3638, 3641);
                await WaoAsync(312);                                    // \L_05301
                await WaoAsync(332);                                    // \L_053a9
                return Outcome.Done;
            }
            await WaoAsync(0);                                          // L_0412e
            // QUIRK: CMD-04 — 29 gold is enough for a price of 30.
            if (State.Gold < 29)
            {
                await SpeakAsync(112, 3518);
                continue;                                               // @L_040ee
            }
            break;
        }

        State.Gold = State.Gold - 30;
        Hist.SundryExp = Hist.SundryExp + 30;                           // QUIRK: CMD-14
        Prov.Owner = 1;
        State.Ui_CmdAttackAvail = 0;
        await SpeakAsync(111, 3529);
        await SpeakAsync(121, 3536);
        State.Cmdr_Cur = 8;                                             // :3539-3582 Gengorou alive
        if (Cmdr.Status != 0)
        {
            State.F_Shinoda_State = 1;
            await SpeakAsync(121, 3544);
            await SpeakAsync(111, 3551);
            await SpeakAsync(121, 3556, 3563);
            await SpeakAsync(111, 3572);
            await SpeakAsync(121, 3577);
        }
        RemoveCommander(8);
        RemoveCommander(9);
        RemoveCommander(10);
        await WaoAsync(312);                                            // \L_05301
        await WaoAsync(322);                                            // \L_05371
        await SpeakAsync(113, 3593);
        await WaoAsync(332);                                            // \L_053a9
        await OwnerFlagAsync();
        await Script.MapCallAsync(10);
        return Outcome.Done;
    }
}
