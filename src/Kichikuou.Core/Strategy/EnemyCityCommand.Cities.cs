using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Cﾃｷﾄｼ.ADV:429-3828</c>, the independent cities (owner 3; part P4, milestone 3 step 4;
/// <c>docs/strategy/stage-2-sweep.md</c>, "The player's commands"): the cities' own exits — Pluepet
/// at Portugal (28), Shangri-La (29), Custom (21), Toushin City (27) —, Maris's question, the menu
/// (Negotiate, Use Force, Stop), the way to the negotiation and Use Force. The negotiations with the
/// mayors (<c>:604-3648</c>) are in <c>EnemyCityCommand.Talks.cs</c>.
/// </summary>
public sealed partial class EnemyCityCommand
{
    private enum CityChoice
    {
        Negotiate,      // L_00bfb
        Force,          // L_043b1
        Stop,           // L_00b9a
    }

    /// <summary>
    /// <c>L_00a07</c> (<c>:429-506</c>): the cities' own exits, Rance's "Time to conquer…", Custom
    /// joining (<c>Iｼﾚｲﾌﾞ</c> 3) and Toushin City's battle inside <c>Gｻｰﾅｷｱ</c> 1, Maris's question,
    /// then the menu until a choice leaves.
    /// </summary>
    private async Task<Outcome> IndependentCityAsync()
    {
        if (State.Prov_Cur == 28 && State.F_Merchant_State == 1)       // :430-433
        {
            await PluepetAsync();                                       // \L_02e1e
            return Outcome.Top;                                         // @L_00025
        }
        if (State.Prov_Cur == 29 && State.F_ShangriLa_State == 0)      // :434-438
        {
            await Script.EventAsync("Iｼｬﾝｸﾞﾘﾗ.ADV", 1);
            return Outcome.Header;                                      // @L_00078
        }
        if (State.Prov_Cur == 29)                                       // :439-443
        {
            await Script.EventAsync("Iｼｬﾝｸﾞﾘﾗ.ADV", 2);
            await LeaveAsync();                                         // @L_00205
            return Outcome.Done;
        }
        State.CallArg = 1;                                              // :444-455 "Time to conquer …"
        await WaoAsync(311);                                            // \L_052f3
        await SpeakAsync(111, 448);                                     // \L_0529f
        if (State.Prov_Cur == 21)                                       // :456-460 Custom joins
        {
            await Script.EventAsync("Iｼﾚｲﾌﾞ.ADV", 3);
            return Outcome.Done;
        }
        if (State.Prov_Cur == 27)                                       // :461-471 Toushin City
        {
            State.Tmp0021 = 4;
            State.Tmp0022 = 1;
            await Script.CallFileAsync("Sﾌﾞﾀｲ.ADV");
            if (State.Bt_PlayerParty1 == 0)
                return Outcome.Header;
            await Script.EventAsync("Gｻｰﾅｷｱ.ADV", 1);
            return Outcome.Done;
        }
        State.CallArg = 6;                                              // :472-485 Maris: "How would you like …"
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(121, 476);                                     // \L_0530f
        await SpeakAsync(112, 483);                                     // \L_052bb

        while (true)
        {
            switch (await CityMenuAsync())
            {
                case CityChoice.Negotiate:
                    if (await NegotiateAsync() is { } outcome)
                        return outcome;
                    break;                                              // @L_00b35
                case CityChoice.Force:
                    return await UseForceAsync();
                default:
                    await StopAsync();
                    return Outcome.Menu;                                // @L_0010d
            }
        }
    }

    /// <summary>
    /// <c>L_00b35</c> (<c>:486-506</c>): the release wait and the menu — Negotiate (not at Temple AL
    /// HQ (77) or Shangri-La (29)), Use Force, Stop; a cancel starts again from the wait.
    /// </summary>
    private async Task<CityChoice> CityMenuAsync()
    {
        while (true)
        {
            await WaitForReleaseAsync();
            var items = new List<(SceneMenuItem Item, CityChoice Choice)>();
            if (State.Prov_Cur != 77 && State.Prov_Cur != 29)
                items.Add((MenuItem(493), CityChoice.Negotiate));
            items.Add((MenuItem(498), CityChoice.Force));
            await LineBreakAsync();                                     // :501 R
            items.Add((MenuItem(502), CityChoice.Stop));
            int choice = await SelectAsync(items.Select(i => i.Item).ToArray());
            if (choice != 0)
                return items[choice - 1].Choice;
        }
    }

    /// <summary><c>L_00b9a</c> (<c>:507-527</c>): "Nevermind." — "Oh, are you sure?" — "Whatever."; the portraits closed.</summary>
    private async Task StopAsync()
    {
        await SpeakAsync(111, 510);                                     // \L_0529f
        await SpeakAsync(121, 515);                                     // \L_0530f
        await SpeakAsync(111, 520);
        await WaoAsync(312);                                            // \L_05301
        await WaoAsync(322);                                            // \L_05371
    }

    /// <summary>
    /// <c>L_00bfb</c> (<c>:528-633</c>): at Rockearth (17) after the DX president was killed
    /// (<c>F_DX_State = 100</c>) Maris refuses and the menu comes back (null); else Rance's
    /// "as a gentleman", the army surrounds the city and the mayor is called out, then the city's own
    /// negotiation (<see cref="TalkAsync"/>).
    /// </summary>
    private async Task<Outcome?> NegotiateAsync()
    {
        if (State.Prov_Cur == 17 && State.F_DX_State == 100)           // :529-551
        {
            await SpeakAsync(121, 532);
            await SpeakAsync(111, 543);
            return null;
        }
        await SpeakAsync(111, 554, 559);                                // :552-565
        await WaoAsync(312);                                            // \L_05301
        await WaoAsync(322);                                            // \L_05371
        await WaoAsync(332);                                            // \L_053a9
        await SpeakAsync(113, 571, 575, 579, 583);                      // \L_052d7: "The Leazas Army surrounded …"
        State.CallArg = 1045;                                           // :589-603
        await WaoAsync(331);                                            // \L_0539b
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_052f3
        await SpeakAsync(113, 595);                                     // "… ordered the mayor … to come out."
        return await TalkAsync();                                       // :604-633, EnemyCityCommand.Talks.cs
    }

    /// <summary>
    /// <c>L_043b1</c> (<c>:3649-3828</c>): "Let's take them by force", the party picked in
    /// <c>Sﾌﾞﾀｲ</c> (four units), the city's army by province (its name <c>MS 10</c> from the player's
    /// files; picked through <c>L_04f2a</c>, retreat mode 1) and <c>FIGT_A</c>; Portugal (28) taken
    /// while Pluepet waits (<c>F_Merchant_State = 2</c>) plays <c>Gﾚﾍﾞｯｶ</c> 2, Mars (22) taken
    /// <c>Gｶｾｲ</c> 1. A party left empty goes back to the header.
    /// </summary>
    private async Task<Outcome> UseForceAsync()
    {
        await SpeakAsync(111, 3652);                                    // \L_0529f
        await WaoAsync(322);                                            // \L_05371
        State.Tmp0021 = 4;                                              // :3660-3667
        State.Tmp0022 = 1;
        // QUIRK: CMD-01 — no unit refuses to go here.
        await Script.CallFileAsync("Sﾌﾞﾀｲ.ADV");
        await Script.MapCallAsync(0);
        if (State.Bt_PlayerParty1 == 0)
            return Outcome.Header;
        State.Bt_EnemyRetreatMode = 1;                                  // :3668
        if (State.Prov_Cur == 17)                                       // :3669-3812 the city's army
            CityArmy(3671, 68, 69);                                     // Rockearth Hero (68, 69)
        if (State.Prov_Cur == 18)
            CityArmy(3684, 66, 67);                                     // Ice Hero (66, 67)
        if (State.Prov_Cur == 19)
            CityArmy(3697, 70);                                         // Diphtheria Hero (70)
        if (State.Prov_Cur == 20)
        {
            SetLiteral(3710);                                           // MS 10, "Lazeal Army"
            State.DXTalk_RetryWeek = 0;
            SetPriority(65);                                            // Lazeal Hero (65)
        }
        if (State.Prov_Cur == 22)
            CityArmy(3724, 71);                                         // Mars the Great (71)
        if (State.Prov_Cur == 23)
            CityArmy(3737, 63, 64);                                     // Red Hero (63, 64)
        if (State.Prov_Cur == 24)
            CityArmy(3750, 13, 14, 15, 16);                             // Zeo Hero (13-16)
        if (State.Prov_Cur == 25)
            CityArmy(3763, 11, 12);                                     // Sayori (11), M Land Hero (12)
        if (State.Prov_Cur == 26)
            CityArmy(3776, 9, 10, 8);                                   // Hanna Hero (9, 10), Gengorou (8)
        if (State.Prov_Cur == 28)
            CityArmy(3789, 17);                                         // Portugal Hero (17)
        if (State.Prov_Cur == 29)
            CityArmy(3802, 18, 19, 20);                                 // the Desert Guides (18-20)
        PickArmy();                                                     // \L_04f2a
        // QUIRK: BFL-15 — the wall bonus is the last battle's.
        State.Bt_Kind = 1;                                              // :3814-3816
        await Script.CallFileAsync("FIGT_A.ADV");
        State.Bt_Kind = 0;
        if (State.Prov_Cur == 28 && Prov.Owner == 1 && State.F_Merchant_State == 2)   // :3817-3821
        {
            State.F_Merchant_State = 3;
            await Script.EventAsync("Gﾚﾍﾞｯｶ.ADV", 2);
        }
        if (State.Prov_Cur == 22 && Prov.Owner == 1)                    // :3822-3825
            await Script.EventAsync("Gｶｾｲ.ADV", 1);
        await Script.MapCallAsync(0);                                   // :3826-3828
        return Outcome.Done;
    }

    /// <summary>A city's army (<c>:3669-3812</c>): its name (<c>MS 10</c> at <paramref name="line"/>) and its list.</summary>
    private void CityArmy(int line, params int[] commanders)
    {
        SetLiteral(line);
        SetPriority(commanders);
    }

    /// <summary>
    /// <c>L_02e1e</c> (<c>:2431-2697</c>): at Portugal (28) while the merchant's state is 1 — the
    /// city's picture, Pluepet's visit and her talk with Rance and Maris; the state moves to 2 and
    /// 100 gold is paid.
    /// </summary>
    private async Task PluepetAsync()
    {
        State.F_Merchant_State = 2;
        await Script.MapCallAsync(0);
        State.CallArg = 1046;
        await WaoAsync(331);                                            // \L_0539b
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_052f3
        await WaoAsync(113);                                            // \L_052d7
        await EngineAsync("ZM", 32);
        await TextAsync(2442);
        await EngineAsync("ZM", 24);
        await TextAsync(2446);
        await WaoAsync(1);
        await TextAsync(2450);
        await WaoAsync(1);
        await WaoAsync(0);
        await SpeakAsync(111, 2461);
        await PortraitSpeechAsync(5, 2470);
        await PortraitSpeechAsync(6, 2482, 2489);
        await PortraitSpeechAsync(5, 2501);
        await SpeakAsync(121, 2509);
        State.CallArg = 470;
        await WaoAsync(321);                                            // \L_05363
        await SpeakAsync(111, 2516);
        await SpeakAsync(123, 2521);
        await SpeakAsync(123, 2530);
        await SpeakAsync(121, 2537);
        await SpeakAsync(111, 2542);
        await SpeakAsync(121, 2551, 2556);
        await SpeakAsync(111, 2563);
        await SpeakAsync(121, 2568, 2571, 2574, 2581, 2588, 2593);
        await SpeakAsync(111, 2600);
        await SpeakAsync(121, 2607, 2612);
        await SpeakAsync(123, 2621);
        await SpeakAsync(121, 2630);
        await SpeakAsync(111, 2637);
        await SpeakAsync(121, 2648, 2653);
        await WaoAsync(322);                                            // \L_05371
        await SpeakAsync(123, 2661);
        await SpeakAsync(111, 2668, 2671);
        await PortraitSpeechAsync(5, 2682, close: false);
        await SpeakAsync(111, 2689);
        State.Gold = State.Gold + 100;                                  // :2694
        await Script.MapCallAsync(0);
    }

    /// <summary>
    /// <c>!CallArg : portrait!</c>, the portrait on the other side (op 321), the speech in its
    /// window (op 121) and, unless <paramref name="close"/> is false, the portrait closed (op 322).
    /// </summary>
    private async Task PortraitSpeechAsync(int portrait, int run, int? more = null, bool close = true)
    {
        State.CallArg = (Sys3Int)portrait;
        await WaoAsync(321);                                            // \L_05363
        if (more is { } second)
            await SpeakAsync(121, run, second);                         // \L_0530f
        else
            await SpeakAsync(121, run);
        if (close)
            await WaoAsync(322);                                        // \L_05371
    }
}
