using Kichikuou.Core.Script;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>TJAPAN.ADV:295-2744</c> — Gekkou's attempts on Leazas Castle (1), one a week while
/// <c>N_Gekkou_Attempts</c> is 1-4 (<c>IJAPAN</c> sets 1): Gekkou's raid, a theft or an attempt on
/// one of six units, or Shinobu's seduction of Rance, the only kind that counts — a game over
/// (ending 4) when Rance was deployed this week, the finale on the fifth count (NAT-14).
/// </summary>
public sealed partial class JapanTurn
{
    /// <summary>The runs of <c>L_02851</c>'s scene in window 131, each with its key wait (<c>:2215-2367</c>).</summary>
    private static readonly int[] ShinobuSceneRuns =
    [
        2215, 2222, 2225, 2232, 2235, 2242, 2247, 2254, 2259, 2264, 2269, 2274, 2281, 2286, 2291, 2296,
        2299, 2304, 2307, 2314, 2317, 2324, 2329, 2332, 2335, 2338, 2343, 2350, 2353, 2358, 2361, 2366,
    ];

    /// <summary>
    /// <c>TJAPAN.ADV:295-487</c> (<c>L_006ac</c>) — a draw in two, or in three when Rance (unit 1)
    /// was deployed this week (<c>Unit_Status = 2</c>); 2 is Shinobu's attempt. Else Gekkou slips
    /// into the castle: on a draw in two an attempt on a unit (<c>L_00a2b</c>), else a draw in
    /// three — half the gold stolen, a quarter, or nothing.
    /// </summary>
    private async Task GekkouAttemptAsync()
    {
        Script.Rnd(2);                                                  // :296-303
        State.Unit_Cur = 1;
        if (Unit.Status == 2)
            Script.Rnd(3);
        if (State.RetVal == 2)
        {
            await ShinobuAttemptAsync();                                // @L_024b0
            return;
        }
        await LeazasCastleAsync(304);
        await SpeakAsync(113, 328, 333, 338, 343);
        State.CallArg = 421;                                            // Gekkou
        await WaoAsync(311);                                            // \L_0334a
        await SpeakAsync(111, 354);                                     // \L_032f6
        Script.Rnd(2);                                                  // :357-360
        if (State.RetVal == 1)
        {
            await AttemptOnUnitAsync();                                 // @L_00a2b
            return;
        }
        await SpeakAsync(113, 363);                                     // \L_0332e
        Script.Rnd(3);                                                  // :370-371
        State.Tmp0690 = State.RetVal;
        await WaoAsync(111);
        if (State.Tmp0690 == 1)                                         // :373-406 half the gold
        {
            await TextAsync(375);
            await WaoAsync(1);                                          // \L_0341b
            await WaoAsync(0);                                          // \L_0340e
            await SpeakAsync(113, 384);
            await SpeakAsync(111, 393);
            await WaoAsync(312);                                        // \L_03358
            await SpeakAsync(113, close: false, 399);
            State.Gold = State.Gold / 2;
        }
        if (State.Tmp0690 == 2)                                         // :407-442 a quarter
        {
            await TextAsync(409);
            await WaoAsync(1);
            await WaoAsync(0);
            await SpeakAsync(113, 418);
            await SpeakAsync(111, 427);
            await WaoAsync(312);
            await SpeakAsync(113, close: false, 435);
            State.Gold = State.Gold / 4 * 3;
        }
        if (State.Tmp0690 == 3)                                         // :443-484 nothing
        {
            await TextAsync(445);
            await WaoAsync(1);
            await WaoAsync(0);
            await SpeakAsync(113, 452, 457);
            await SpeakAsync(111, 468);
            await WaoAsync(312);
            await SpeakAsync(113, close: false, 478);
        }
        await WaoAsync(0);
        await WaoAsync(332);                                            // \L_03400
    }

    /// <summary>
    /// The camera to Leazas Castle (1), its picture (1001) and the header "Leazas Castle / Before
    /// the Castle Wall" (<c>TJAPAN.ADV:304-325</c>, <c>:1962-1983</c>; <paramref name="line"/> is
    /// the first).
    /// </summary>
    private async Task LeazasCastleAsync(int line)
    {
        State.Prov_Cur = 1;
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(1);
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
        await Script.MapCallAsync(10);
        State.CallArg = 1001;
        await WaoAsync(331);                                            // \L_033f2
        await WaoAsync(113);                                            // \L_0332e
        await EngineAsync("ZM", 32);
        await TextAsync(line + 14);
        await EngineAsync("ZM", 24);
        await TextAsync(line + 18);
        await WaoAsync(1);                                              // \L_0341b
        await WaoAsync(0);                                              // \L_0340e
    }

    /// <summary>
    /// <c>TJAPAN.ADV:488-550</c> (<c>L_00a2b</c>) — a draw in six picks Barres (unit 6), Rick (7),
    /// Cordoba (8), Ex (14), Eleanor (23) or Gengorou (15) by the cursor, each only if alive
    /// (<c>Unit_Status ≠ 0</c>); a dead target (or a 7, ENG-02) and Gekkou finds nobody.
    /// </summary>
    private async Task AttemptOnUnitAsync()
    {
        await WaoAsync(312);                                            // \L_03358
        Script.Rnd(6);                                                  // :490-514
        State.Unit_Cur = 6;
        if (State.RetVal == 1 && Unit.Status != 0)
        {
            await BarresAsync();                                        // @L_00b86
            return;
        }
        State.Unit_Cur = 7;
        if (State.RetVal == 2 && Unit.Status != 0)
        {
            await RickAsync();                                          // @L_00f8b
            return;
        }
        State.Unit_Cur = 8;
        if (State.RetVal == 3 && Unit.Status != 0)
        {
            await CordobaAsync();                                       // @L_012f5
            return;
        }
        State.Unit_Cur = 14;
        if (State.RetVal == 4 && Unit.Status != 0)
        {
            await ExAsync();                                            // @L_016ab
            return;
        }
        State.Unit_Cur = 23;
        if (State.RetVal == 5 && Unit.Status != 0)
        {
            await EleanorAsync();                                       // @L_01b07
            return;
        }
        State.Unit_Cur = 15;
        if (State.RetVal == 6 && Unit.Status != 0)
        {
            await GengorouAsync();                                      // @L_01e9c
            return;
        }
        await WaoAsync(0);
        await SpeakAsync(113, 518);
        State.CallArg = 421;
        await WaoAsync(311);
        await SpeakAsync(111, 527, 536);
        await WaoAsync(312);
        await SpeakAsync(113, 542);
        await WaoAsync(332);
    }

    /// <summary>The target's whereabouts: its header (<paramref name="line"/> and the run 4 lines on, at 32 and 24 dots).</summary>
    private async Task PlaceAsync(int line, bool close = true)
    {
        await WaoAsync(113);
        await EngineAsync("ZM", 32);
        await TextAsync(line);
        await EngineAsync("ZM", 24);
        await TextAsync(line + 4);
        await WaoAsync(1);
        if (close)
            await WaoAsync(0);
    }

    /// <summary>
    /// <c>TJAPAN.ADV:551-774</c> (<c>L_00b86</c>) — Barres (unit 6, the cursor): killed if he
    /// was deployed this week (QUIRK: NAT-14), else he fends Gekkou off.
    /// </summary>
    private async Task BarresAsync()
    {
        await PlaceAsync(555);
        State.CallArg = 402;
        await WaoAsync(311);
        if (Unit.Status == 2)                                           // :565-675
        {
            Unit.Status = 0;
            await SpeakAsync(111, 569, 576);
            await SpeakAsync(121, 583);                                 // \L_03366
            await SpeakAsync(111, 592);
            State.CallArg = 421;
            await WaoAsync(321);                                        // \L_033ba
            await SpeakAsync(123, 599);                                 // \L_0339e
            await SpeakAsync(111, 606);
            await SpeakAsync(123, 611);
            await SpeakAsync(111, 618);
            await SpeakAsync(113, 623, 628);
            await SpeakAsync(112, 635);                                 // \L_03312
            await WaoAsync(312);
            await SpeakAsync(113, 641);
            await SpeakAsync(121, 648);
            await WaoAsync(312);
            await SpeakAsync(113, 654, 657, 661, 665, 669);
            return;
        }
        await SpeakAsync(111, 678, 681);
        State.CallArg = 421;
        await WaoAsync(321);
        await SpeakAsync(111, 694);
        await SpeakAsync(113, 699);
        await SpeakAsync(121, 706);
        await SpeakAsync(123, 711);
        await SpeakAsync(113, 716, 719);
        await SpeakAsync(122, 726);                                     // \L_03382
        await SpeakAsync(113, 733);
        await SpeakAsync(111, 744);
        await SpeakAsync(113, 749);
        await WaoAsync(322);                                            // \L_033c8
        await SpeakAsync(113, 755, 758);
        await SpeakAsync(111, 765);
    }

    /// <summary>
    /// <c>TJAPAN.ADV:775-966</c> (<c>L_00f8b</c>) — Rick (unit 7, the cursor): killed if he was deployed
    /// this week (QUIRK: NAT-14; tested after the first exchange), else he wins the duel.
    /// </summary>
    private async Task RickAsync()
    {
        await PlaceAsync(779);
        State.CallArg = 403;
        await WaoAsync(311);
        await SpeakAsync(111, 791);
        State.CallArg = 421;
        await WaoAsync(321);
        await SpeakAsync(121, 804);
        await SpeakAsync(111, 809);
        await SpeakAsync(121, 816);
        await SpeakAsync(113, 821);
        await SpeakAsync(111, 830);
        await SpeakAsync(113, 835);
        if (Unit.Status == 2)                                           // :840-919
        {
            Unit.Status = 0;
            await SpeakAsync(111, 844);
            await SpeakAsync(113, 849);
            await SpeakAsync(121, 860);
            await WaoAsync(322);
            await SpeakAsync(111, 870, 873);
            await SpeakAsync(113, 878);
            await SpeakAsync(111, 883, 886, 889);
            await WaoAsync(312);
            await SpeakAsync(113, 897, 901, 905, 909);
            return;
        }
        await SpeakAsync(121, 922);
        await SpeakAsync(113, 927);
        await SpeakAsync(111, 934);
        await SpeakAsync(113, 939);
        await WaoAsync(322);
        await SpeakAsync(111, 947, 952);
        await SpeakAsync(113, 959);
    }

    /// <summary>
    /// <c>TJAPAN.ADV:967-1175</c> (<c>L_012f5</c>) — Cordoba (unit 8, the cursor): killed if he
    /// was deployed this week (QUIRK: NAT-14), else he survives.
    /// </summary>
    private async Task CordobaAsync()
    {
        await PlaceAsync(971);
        State.CallArg = 404;
        await WaoAsync(311);
        await SpeakAsync(111, 983);
        await SpeakAsync(113, 988);
        await SpeakAsync(111, 999, 1006, 1009);
        await SpeakAsync(113, 1014);
        State.CallArg = 421;
        await WaoAsync(321);
        await SpeakAsync(121, 1023);
        await SpeakAsync(111, 1028);
        await SpeakAsync(113, close: false, 1037, 1040);
        if (Unit.Status == 2)                                           // :1044-1105
        {
            Unit.Status = 0;
            await TextAsync(1047);
            await WaoAsync(1);
            await TextAsync(1050);
            await WaoAsync(1);
            await WaoAsync(0);
            await SpeakAsync(111, 1061, 1064);
            await SpeakAsync(113, 1069);
            await SpeakAsync(111, 1076);
            await WaoAsync(312);
            await SpeakAsync(121, 1082);
            await WaoAsync(322);
            await SpeakAsync(113, 1088, 1097);
            return;
        }
        await TextAsync(1107);
        await WaoAsync(1);
        await TextAsync(1110);
        await WaoAsync(1);
        await WaoAsync(0);
        await SpeakAsync(111, 1117);
        await SpeakAsync(121, 1128);
        await WaoAsync(322);
        await SpeakAsync(113, 1134);
        await SpeakAsync(111, 1141, 1148, 1151);
        await SpeakAsync(113, 1156);
        await SpeakAsync(111, 1163, 1172);
    }

    /// <summary>
    /// <c>TJAPAN.ADV:1176-1416</c> (<c>L_016ab</c>) — Ex (unit 14, the cursor): killed if he was deployed
    /// this week (QUIRK: NAT-14), else he walks the hallway with a staff officer (portrait 479).
    /// </summary>
    private async Task ExAsync()
    {
        await PlaceAsync(1180, close: false);
        if (Unit.Status == 2)                                           // :1187-1267
        {
            Unit.Status = 0;
            await TextAsync(1190);
            await WaoAsync(1);
            await WaoAsync(0);
            State.CallArg = 405;
            await WaoAsync(311);
            await SpeakAsync(111, 1203);
            State.CallArg = 421;
            await WaoAsync(321);
            await SpeakAsync(111, 1214);
            await SpeakAsync(113, 1219, 1224, 1227);
            await SpeakAsync(111, 1232);
            await SpeakAsync(112, 1237);
            await WaoAsync(312);
            await SpeakAsync(113, 1249);
            await WaoAsync(322);
            await SpeakAsync(113, 1259);
            return;
        }
        await TextAsync(1269);
        await WaoAsync(1);
        await WaoAsync(0);
        State.CallArg = 405;
        await WaoAsync(311);
        State.CallArg = 479;
        await WaoAsync(321);
        await SpeakAsync(121, 1282);
        await SpeakAsync(111, 1293);
        await SpeakAsync(121, 1304);
        await WaoAsync(322);
        State.CallArg = 421;
        await WaoAsync(321);
        await SpeakAsync(121, 1312);
        await SpeakAsync(111, 1319);
        await SpeakAsync(113, 1324);
        await SpeakAsync(112, 1333);
        await WaoAsync(322);
        State.CallArg = 479;
        await WaoAsync(321);
        await SpeakAsync(121, 1341);
        await SpeakAsync(113, 1346);
        await SpeakAsync(121, 1351);
        await SpeakAsync(111, 1356);
        await SpeakAsync(113, 1361);
        await WaoAsync(322);
        State.CallArg = 421;
        await WaoAsync(321);
        await SpeakAsync(121, 1371);
        await WaoAsync(322);
        await SpeakAsync(123, 1377);
        State.CallArg = 479;
        await WaoAsync(321);
        await SpeakAsync(121, 1386);
        await SpeakAsync(111, 1391);
        await SpeakAsync(121, 1400);
        await WaoAsync(322);
        await SpeakAsync(111, 1406);
        await SpeakAsync(113, 1411);
    }

    /// <summary>
    /// <c>TJAPAN.ADV:1417-1617</c> (<c>L_01b07</c>) — Eleanor (unit 23, the cursor): killed if she
    /// was deployed this week (QUIRK: NAT-14), else she survives.
    /// </summary>
    private async Task EleanorAsync()
    {
        await PlaceAsync(1421);
        State.CallArg = 66;
        await WaoAsync(311);
        if (Unit.Status == 2)                                           // :1431-1498
        {
            Unit.Status = 0;
            await SpeakAsync(111, 1435);
            await SpeakAsync(113, 1444, 1453);
            await SpeakAsync(111, 1460);
            await SpeakAsync(121, 1471);
            await SpeakAsync(111, 1476);
            State.CallArg = 421;
            await WaoAsync(321);
            await SpeakAsync(113, 1483);
            await WaoAsync(312);
            await WaoAsync(322);
            await SpeakAsync(113, 1490);
            return;
        }
        await SpeakAsync(111, 1501);
        await SpeakAsync(113, 1506);
        await SpeakAsync(111, 1511, 1518, 1527, 1534, 1537);
        await SpeakAsync(113, 1542);
        await SpeakAsync(111, 1549);
        await SpeakAsync(113, 1554);
        await SpeakAsync(111, 1561);
        await SpeakAsync(113, 1566);
        State.CallArg = 421;
        await WaoAsync(321);
        await SpeakAsync(111, 1577);
        await SpeakAsync(113, 1582);
        await WaoAsync(322);
        await SpeakAsync(111, 1592, 1595);
        await SpeakAsync(113, 1600);
        await SpeakAsync(111, 1609, 1614);
    }

    /// <summary>
    /// <c>TJAPAN.ADV:1618-1959</c> (<c>L_01e9c</c>) — Gengorou (unit 15, the cursor): killed if he
    /// was deployed this week (QUIRK: NAT-14; tested before the header), else he survives.
    /// </summary>
    private async Task GengorouAsync()
    {
        if (Unit.Status == 2)                                           // :1619-1764
        {
            Unit.Status = 0;
            await PlaceAsync(1624);
            State.CallArg = 413;
            await WaoAsync(311);
            State.CallArg = 12;
            await WaoAsync(321);
            await SpeakAsync(111, 1638);
            await SpeakAsync(121, 1643);
            await SpeakAsync(111, 1650);
            await SpeakAsync(113, 1661);
            await SpeakAsync(111, 1672);
            await SpeakAsync(121, 1679);
            await SpeakAsync(111, 1684);
            await SpeakAsync(121, 1691);
            await SpeakAsync(113, 1696);
            await SpeakAsync(111, 1703, 1708);
            await SpeakAsync(121, 1713);
            await SpeakAsync(111, 1718);
            await SpeakAsync(113, 1723);
            await SpeakAsync(121, 1730);
            await WaoAsync(312);
            State.CallArg = 421;
            await WaoAsync(311);
            await SpeakAsync(113, 1738);
            await WaoAsync(312);
            await SpeakAsync(113, 1750, 1753, 1758);
            return;
        }
        await PlaceAsync(1768);
        State.CallArg = 413;
        await WaoAsync(311);
        await SpeakAsync(111, 1780);
        await SpeakAsync(113, 1785, 1792);
        await SpeakAsync(111, 1801, 1806, 1811, 1814);
        await SpeakAsync(113, 1819);
        State.CallArg = 421;
        await WaoAsync(321);
        await SpeakAsync(123, 1828, 1833, 1838);
        await WaoAsync(322);
        State.CallArg = 5;
        await WaoAsync(321);
        await SpeakAsync(121, 1846);
        await SpeakAsync(113, 1853);
        await SpeakAsync(121, 1860);
        await SpeakAsync(111, 1869, 1874);
        await SpeakAsync(121, 1881);
        await SpeakAsync(111, 1888);
        await SpeakAsync(121, 1897, 1904, 1911);
        await SpeakAsync(113, 1918, 1921, 1924);
        await SpeakAsync(111, 1931);
        await SpeakAsync(121, 1940);
        await SpeakAsync(111, 1949);
        await SpeakAsync(113, 1954);
    }

    /// <summary>
    /// <c>TJAPAN.ADV:1960-2161</c> (<c>L_024b0</c>) — Shinobu's attempt, the one that counts
    /// (<c>N_Gekkou_Attempts + 1</c>): her seduction of Rance (unit 1) works if he was deployed this week
    /// (QUIRK: NAT-14 — ending 4, <c>L_02851</c>), else he runs off; on the fifth count she is
    /// caught (<c>L_02c04</c>).
    /// </summary>
    private async Task ShinobuAttemptAsync()
    {
        State.N_Gekkou_Attempts = State.N_Gekkou_Attempts + 1;
        await LeazasCastleAsync(1962);
        State.CallArg = 422;                                            // Shinobu
        await WaoAsync(311);
        await SpeakAsync(111, 1988);
        await SpeakAsync(113, 1993);
        await SpeakAsync(111, 2002, 2005);
        State.CallArg = 1;                                              // Rance
        await WaoAsync(321);
        await SpeakAsync(121, 2012);
        await SpeakAsync(113, 2017);
        await SpeakAsync(112, 2026);
        await SpeakAsync(113, 2033);
        await SpeakAsync(121, 2042);
        await SpeakAsync(111, 2049);
        await SpeakAsync(113, 2054, 2059);
        await SpeakAsync(121, 2068);
        await SpeakAsync(113, 2075, 2082);
        await SpeakAsync(121, 2089);
        await SpeakAsync(112, 2096);
        await SpeakAsync(113, 2101);
        await SpeakAsync(121, 2108);
        State.Unit_Cur = 1;                                             // :2111-2114
        if (Unit.Status == 2)
            await ShinobuSucceedsAsync();                               // @L_02851
        await WaoAsync(322);
        await SpeakAsync(113, 2118, 2125);
        await SpeakAsync(111, 2132, 2135);
        await SpeakAsync(113, 2142, 2151);
        if (State.N_Gekkou_Attempts == 5)                               // :2156-2158
        {
            await ShinobuCaughtAsync();                                 // @L_02c04
            return;
        }
        await WaoAsync(312);
        await WaoAsync(332);
    }

    /// <summary>
    /// <c>TJAPAN.ADV:2162-2375</c> (<c>L_02851</c>) — Rance gives in to Shinobu: the scene (CG
    /// 806 / 133 between fades), the map reset and the game's end, ending 4 (<c>&amp;#OVER</c>).
    /// </summary>
    private async Task ShinobuSucceedsAsync()
    {
        await SpeakAsync(113, 2165);
        await SpeakAsync(121, 2174);
        await SpeakAsync(113, 2185);
        await SpeakAsync(121, 2192);
        await SpeakAsync(113, 2199);
        await EngineAsync("PF1", State.FadeSpeed);
        await EngineAsync("G", 806);
        await EngineAsync("G", 133);
        await WaoAsync(131);                                            // \L_033d6
        await TextAsync(2211);
        await EngineAsync("PF0", State.FadeSpeed);
        await WaoAsync(1);
        foreach (int line in ShinobuSceneRuns)
        {
            await TextAsync(line);
            await WaoAsync(1);
        }
        await EngineAsync("PF1", State.FadeSpeed);
        await WaoAsync(0);
        await Script.MapCallAsync(0);
        await EngineAsync("PF0", State.FadeSpeed);
        State.EndingId = 4;
        ScriptContext.JumpTo("OVER.ADV");
    }

    /// <summary>
    /// <c>TJAPAN.ADV:2376-2744</c> (<c>L_02c04</c>, <c>L_02ddd</c>, <c>L_03268</c>) — Shinobu caught
    /// on the fifth count: Maris (portrait 6) speaks if Leila (unit 5) is dead, else Leila (7), and
    /// Gekkou takes Shinobu away. Nothing but the windows changes.
    /// </summary>
    private async Task ShinobuCaughtAsync()
    {
        State.Unit_Cur = 5;                                             // :2377-2432
        if (Unit.Status == 0)
        {
            await WaoAsync(0);
            State.CallArg = 6;
            await WaoAsync(321);
            await SpeakAsync(121, 2384);
            await SpeakAsync(111, 2391);
            await SpeakAsync(113, 2396);
            await SpeakAsync(121, 2407, 2416);
            await SpeakAsync(113, close: false, 2425);
        }
        else
        {
            await WaoAsync(0);
            State.CallArg = 7;
            await WaoAsync(321);
            await SpeakAsync(121, 2438);
            await SpeakAsync(111, 2445);
            await SpeakAsync(113, 2450, 2455);
            await SpeakAsync(121, 2462, 2469);
            await SpeakAsync(113, close: false, 2478);
        }
        await WaoAsync(0);                                              // :2482 L_02ddd
        await WaoAsync(312);
        await WaoAsync(322);
        await SpeakAsync(113, 2488, 2493);
        await SpeakAsync(111, 2500);
        await SpeakAsync(113, 2505);
        State.CallArg = 421;
        await WaoAsync(311);
        await SpeakAsync(113, 2516);
        State.CallArg = 422;
        await WaoAsync(321);
        await SpeakAsync(121, close: false, 2527);
        State.Unit_Cur = 5;                                             // :2529-2551
        if (Unit.Status == 0)
        {
            // QUIRK: NAT-24 — Maris's order has no jump past Leila's: a dead Leila orders too.
            await WaoAsync(0);
            State.CallArg = 6;
            await WaoAsync(321);
            await SpeakAsync(121, 2536);
            await SpeakAsync(113, close: false, 2543);
        }
        await WaoAsync(0);
        State.CallArg = 7;
        await WaoAsync(321);
        await SpeakAsync(121, 2557);
        await SpeakAsync(113, 2562);
        State.CallArg = 422;
        await WaoAsync(321);
        await SpeakAsync(111, 2575);
        await SpeakAsync(121, 2580);
        await SpeakAsync(113, 2585);
        await SpeakAsync(111, 2594);
        await SpeakAsync(113, 2599, 2604);
        await WaoAsync(312);
        await WaoAsync(322);
        State.Unit_Cur = 5;                                             // :2613-2678
        if (Unit.Status == 0)
        {
            State.CallArg = 6;
            await WaoAsync(321);
            await SpeakAsync(121, 2619, 2622);
            await WaoAsync(322);
            await SpeakAsync(113, 2628, 2637, 2640, 2647);
            State.CallArg = 6;
            await WaoAsync(321);
            await SpeakAsync(121, 2660);
            await SpeakAsync(113, close: false, 2671);
        }
        else
        {
            State.CallArg = 7;
            await WaoAsync(321);
            await SpeakAsync(121, 2683);
            await WaoAsync(322);
            await SpeakAsync(113, 2689, 2698, 2701, 2708);
            State.CallArg = 7;
            await WaoAsync(321);
            await SpeakAsync(122, 2721);
            await SpeakAsync(113, close: false, 2732);
        }
        await WaoAsync(0);                                              // :2740 L_03268
        await WaoAsync(322);
        await WaoAsync(332);
    }
}
