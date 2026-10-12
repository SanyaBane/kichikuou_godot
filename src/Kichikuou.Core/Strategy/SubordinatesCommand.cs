using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Cﾌﾞｶ.ADV</c>, the Subordinates command (<c>docs/strategy/survey-commands.md</c> §3.5), ported
/// whole: the picker <c>Sﾌﾞｶ</c> (called with its mode in <c>CallArg</c>; it answers the unit in
/// <c>CallArg</c> and <c>Unit_Cur</c>), the view-only loop once the week's actions are spent, the
/// unit's status page, the prologue's talk (<c>:713-720</c>: no menu, the unit's talk scene at once,
/// the command spent) and the troop menu of the later stages (<c>:721-1112</c>: Info, Replenish
/// Troops, Add New Troops, Talk, Do Nothing).
/// </summary>
public sealed class SubordinatesCommand(OriginalScriptContext script) : CommandFile(script, "Cﾌﾞｶ.ADV")
{
    /// <summary>
    /// The talk scene of each unit (<c>:208-712</c>, one label per unit, all called with the
    /// <c>Ev_Entry</c> of 20 set at <c>:25</c>); Rance (1), Honey/Prince (48) and the mercenaries
    /// (49, 50) are handled apart.
    /// </summary>
    private static readonly Dictionary<int, string> TalkFiles = new()
    {
        [2] = "Gｼｨﾙ.ADV", [3] = "Gﾊﾞｳﾝﾄﾞ.ADV", [4] = "Gｿｳﾙ.ADV", [5] = "Gﾚｲﾗ.ADV",
        [6] = "Gﾊﾞﾚｽ.ADV", [7] = "Gﾘｯｸ.ADV", [8] = "Gｺﾙﾄﾞﾊﾞ.ADV", [9] = "Gｷﾝｹｰﾄﾞ.ADV",
        [10] = "Gﾒﾅﾄﾞ.ADV", [11] = "Gｶﾄﾞﾐｭｳﾑ.ADV", [12] = "Gﾊｳﾚｰﾝ.ADV", [13] = "Gﾒﾙﾌｪｲｽ.ADV",
        [14] = "Gｴｸｽ.ADV", [15] = "Gｼﾉﾀﾞ.ADV", [16] = "Gﾏﾘｱ.ADV", [17] = "Gｼｽﾞｶ.ADV",
        [18] = "Gﾊﾟｯﾄﾝ.ADV", [19] = "Gﾊﾝﾃｨ.ADV", [20] = "Gﾌﾘｰｸ.ADV", [21] = "Gﾋｭｰﾊﾞｰﾄ.ADV",
        [22] = "Gﾚﾘｭｰｺﾌ.ADV", [23] = "Gﾗﾝ.ADV", [24] = "Gﾐﾘ.ADV", [25] = "Gﾐﾙ.ADV",
        [26] = "Gｶｾｲ.ADV", [27] = "Gｲｿﾛｸ.ADV", [28] = "Gｸﾘｰﾑ.ADV", [29] = "Gｻﾃﾗ.ADV",
        [30] = "Gﾒｶﾞﾗｽ.ADV", [31] = "Gｹﾝﾀﾛｳ.ADV", [32] = "Gﾐｷ.ADV", [33] = "Gｼﾞｭﾘｱ.ADV",
        [34] = "Gﾗﾌｧﾘｱ.ADV", [35] = "Gｱｰﾙｺｰﾄ.ADV", [36] = "Gｱﾋﾞｧﾄｰﾙ.ADV", [37] = "Gｶﾞﾝｼﾞｰ.ADV",
        [38] = "Gﾁｽﾞｺ.ADV", [39] = "Gﾏｼﾞｯｸ.ADV", [40] = "Gﾅｷﾞ.ADV", [41] = "Gｶﾞﾙﾃｨｱ.ADV",
        [42] = "Gｻｲｾﾞﾙ.ADV", [43] = "Gｱﾘｽﾄﾚｽ.ADV", [44] = "GAL.ADV", [45] = "Gｶﾌｪ.ADV",
        [46] = "Gｿﾐｰﾀ.ADV", [47] = "Gﾛﾚｯｸｽ.ADV",
    };

    /// <summary><c>Cﾌﾞｶ.ADV:1-207</c>: the command, up to its return.</summary>
    public async Task RunAsync()
    {
        if (State.Ui_CmdSubordAvail == 0)                               // :1-20
        {
            await ViewOnlyAsync();
            return;
        }
        if (State.F_SillCow_State == 3)                                 // :21-24
            await Script.EventAsync("Gｼｨﾙ.ADV", 3);
        // QUIRK: EVT-06 — set once here; every talk scene below is called with it.
        State.Ev_Entry = 20;                                            // :25
        await PickerAsync(2);
        while (true)
        {
            // L_000ca (:28-43): pick a unit; one that cannot act shows its status page.
            State.CallArg = 1;
            State.RetVal = 1;
            await Script.CallFileAsync("Sﾌﾞｶ.ADV");
            if (State.CallArg == 0 || State.CallArg == 503)
                return;                                                 // L_00550
            UnitNames();
            if (Unit.Status != 1)
            {
                await StatusPageAsync();                                // \L_0179c
                await PickerAsync(5);
                continue;
            }
            State.Tmp0021 = State.CallArg;                              // :44-47
            await PickerAsync(3);
            State.CallArg = State.Tmp0021;

            int unit = State.CallArg.Value;                             // :48-197 the unit's label
            if (unit is < 1 or > 50)
            {
                await ValuesAsync(199, State.CallArg);                  // "部下番号エラー：n"
                await HitAnyKeyAsync();
                return;                                                 // L_00550
            }
            State.Unit_Cur = (Sys3Int)unit;
            await ActionAsync();                                        // \L_00f4b
            if (State.Tmp0001 == 1)
            {
                await PickerAsync(4);                                   // L_00553
                continue;
            }
            await TalkAsync(unit);
            return;
        }
    }

    /// <summary>
    /// <c>:1-20</c> — the week's actions are spent: pick a unit to see its status page, until
    /// the picker is cancelled.
    /// </summary>
    private async Task ViewOnlyAsync()
    {
        await PickerAsync(2);
        while (true)
        {
            // L_00032
            State.CallArg = 1;
            State.RetVal = 1;
            await Script.CallFileAsync("Sﾌﾞｶ.ADV");
            if (State.CallArg != 0 && State.CallArg != 503)
            {
                UnitNames();
                await StatusPageAsync();                                // \L_0179c
                await PickerAsync(5);
                continue;
            }
            await PickerAsync(3);
            return;
        }
    }

    /// <summary><c>!CallArg : mode! %#Sﾌﾞｶ.ADV:</c> — the picker in one of its modes (2 open, 3 close, 4/5 redraw).</summary>
    private Task PickerAsync(Sys3Int mode)
    {
        State.CallArg = mode;
        return Script.CallFileAsync("Sﾌﾞｶ.ADV");
    }

    /// <summary><c>:9-11</c>, <c>:35-37</c>: the unit's three name strings into strings 1-3.</summary>
    private void UnitNames()
    {
        CopyString(1, State.Unit_StrBase + (State.Unit_Cur - 1) * 3 + 0);
        CopyString(2, State.Unit_StrBase + (State.Unit_Cur - 1) * 3 + 1);
        CopyString(3, State.Unit_StrBase + (State.Unit_Cur - 1) * 3 + 2);
    }

    /// <summary>Where a troop-menu action goes on.</summary>
    private enum Next
    {
        Header,         // @L_00f9b — the unit's header, the offer and the menu again
        Menu,           // @L_0117d — the menu again
        Done,           // \0: — back to the unit's label with Tmp0001 0 (done), 1 (pick again) or 2 (talk)
    }

    /// <summary>The troop menu's items (<c>:805-850</c>).</summary>
    private enum TroopChoice
    {
        Info,           // L_0129a
        Replenish,      // L_014d9
        AddTroops,      // L_012b5
        Talk,           // L_014b2
        Unavailable,    // L_0117d: a "×" item, the menu again
        Nothing,        // L_0127d, also a cancel
    }

    /// <summary>
    /// <c>L_00f4b</c> (<c>:713-1112</c>): in the prologue there is no menu — the command is spent
    /// and the talk follows (<c>Tmp0001</c> 2). Later the unit's troop menu (survey-commands §3.5):
    /// Info (the status page), Replenish Troops, Add New Troops (the offer by class and unit, dearer
    /// every four weeks; Add More doubles the men for three times the price), Talk, Do Nothing.
    /// Replenishing or adding spends <c>CmdAvail_SubTroops</c>, talking <c>CmdAvail_SubTalk</c>;
    /// the command is spent when both are. Leaves <c>Tmp0001</c> for the unit's label: 0 done,
    /// 1 pick another unit, 2 talk.
    /// </summary>
    private async Task ActionAsync()
    {
        CopyString(1, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
        CopyString(2, State.Unit_StrBase + (State.Unit_Cur - 1) * 3 + 1);
        if (State.StoryStage == 0)
        {
            State.Ui_CmdSubordAvail = 0;
            State.Tmp0001 = 2;
            return;
        }
        State.CallArg = Unit.FaceCg;                                    // :721-724
        await WaoAsync(311);                                            // \L_01e1e
        State.CallArg = 6;
        await WaoAsync(321);                                            // \L_01e8e
        var next = Next.Header;
        while (true)
        {
            if (next == Next.Header)
                await OfferAsync();                                     // L_00f9b
            next = await TroopMenuAsync() switch                        // L_0117d
            {
                TroopChoice.Info => await InfoAsync(),
                TroopChoice.Replenish => await ReplenishAsync(),
                TroopChoice.AddTroops => await AddTroopsAsync(),
                TroopChoice.Talk => await TalkChoiceAsync(),
                _ => await NothingAsync(),
            };
            if (next == Next.Done)
                return;
        }
    }

    /// <summary>
    /// <c>L_00f9b</c> (<c>:725-799</c>): "X's unit. What will you do?" with its HP and troops, and
    /// the offer of new troops — <c>Tmp0003</c> men for <c>Tmp0001</c> (hundreds of gold): by class
    /// and troop attack, a few units of their own, 320 for 10 men otherwise, none for units 25, 41
    /// and 48-50, and a quarter of the week added to any price.
    /// </summary>
    private async Task OfferAsync()
    {
        if (State.Unit_Cur != 1)
        {
            await WaoAsync(121);                                        // \L_01e3a
            await TextAsync(729);                                       // "X's unit. What will you do?"
        }
        if (State.Unit_Cur == 1)
        {
            await WaoAsync(121);
            await TextAsync(738);                                       // "The King's personal unit."
        }
        await ValuesAsync(744, Unit.HP, Unit.MaxHP, Unit.Troops, Unit.MaxTroops);
        State.Tmp0001 = 0;                                              // :757-799 the offer
        State.Tmp0003 = 25;
        if (Unit.Class == 0 && Unit.TroopAtk == 10)
            State.Tmp0001 = 110;
        if (Unit.Class == 0 && Unit.TroopAtk == 11)
            State.Tmp0001 = 300;
        if (Unit.Class == 0 && Unit.TroopAtk == 8)
            State.Tmp0001 = 50;
        if (Unit.Class == 2 && Unit.TroopAtk == 3)
            State.Tmp0001 = 100;
        if (State.Unit_Cur == 3 || State.Unit_Cur == 4)
        {
            State.Tmp0001 = 30;
            State.Tmp0003 = 50;
        }
        if (State.Unit_Cur == 5 || State.Unit_Cur == 33 || State.Unit_Cur == 36 || State.Unit_Cur == 18)
            State.Tmp0001 = 200;
        if (State.Unit_Cur == 16)
            State.Tmp0001 = 120;
        if (Unit.Class == 3)
            State.Tmp0001 = 150;
        if (State.Tmp0001 == 0)
        {
            State.Tmp0001 = 320;
            State.Tmp0003 = 10;
        }
        if (State.Unit_Cur > 47)
            State.Tmp0001 = 0;
        if (State.Unit_Cur == 25)
            State.Tmp0001 = 0;
        if (State.Unit_Cur == 41)
            State.Tmp0001 = 0;
        if (State.Tmp0001 != 0)
            State.Tmp0001 = State.Tmp0001 + State.Week / 4;
    }

    /// <summary>
    /// <c>L_0117d</c> (<c>:800-851</c>): the release wait, the pointer saved, the menu (Add New
    /// Troops only with an offer); a "×" item starts again from the wait, a cancel is Do Nothing.
    /// </summary>
    private async Task<TroopChoice> TroopMenuAsync()
    {
        while (true)
        {
            await WaitForReleaseAsync();
            await SavePointerAsync();                                   // \L_01db4
            var items = new List<(SceneMenuItem Item, TroopChoice Choice)> { (MenuItem(807), TroopChoice.Info) };
            if (State.CmdAvail_SubTroops == 1)
            {
                items.Add((MenuItem(812), TroopChoice.Replenish));      // ○Replenish Troops
                if (State.Tmp0001 != 0)
                    items.Add((MenuItem(817), TroopChoice.AddTroops));  // ○Add New Troops
            }
            if (State.CmdAvail_SubTroops == 0)
            {
                items.Add((MenuItem(824), TroopChoice.Unavailable));    // ×Replenish Troops
                if (State.Tmp0001 != 0)
                    items.Add((MenuItem(829), TroopChoice.Unavailable));
            }
            if (State.CmdAvail_SubTalk == 1)
                items.Add((MenuItem(836), TroopChoice.Talk));           // ○Talk
            if (State.CmdAvail_SubTalk == 0)
                items.Add((MenuItem(842), TroopChoice.Unavailable));    // ×Talk
            items.Add((MenuItem(847), TroopChoice.Nothing));            // Do Nothing

            int choice = await SelectAsync(items.Select(i => i.Item).ToArray());
            if (choice == 0)
                return TroopChoice.Nothing;                             // :851 @L_0127d
            if (items[choice - 1].Choice != TroopChoice.Unavailable)
                return items[choice - 1].Choice;
        }
    }

    /// <summary><c>L_0127d</c> (<c>:852-858</c>): the windows and portraits closed, the pointer back, pick again.</summary>
    private async Task<Next> NothingAsync()
    {
        await WaoAsync(0);                                              // \L_01ee2
        await WaoAsync(312);                                            // \L_01e2c
        await WaoAsync(322);                                            // \L_01e9c
        State.Tmp0001 = 1;
        await RestorePointerAsync();                                    // \L_01dbf
        return Next.Done;
    }

    /// <summary><c>L_0129a</c> (<c>:859-862</c>): the unit's status page, then the header again.</summary>
    private async Task<Next> InfoAsync()
    {
        await WaoAsync(0);
        await StatusPageAsync();                                        // \L_0179c
        return Next.Header;
    }

    /// <summary>
    /// <c>L_012b5</c> (<c>:863-965</c>): the offer and its price against the budget; Add More
    /// (<c>L_012a9</c>, while the price is under 10000) doubles the men and triples the price; Do It
    /// pays and raises the unit's troops, maximum and recovery.
    /// </summary>
    private async Task<Next> AddTroopsAsync()
    {
        while (true)
        {
            await WaoAsync(0);                                          // \L_01ee2
            await WaoAsync(121);                                        // \L_01e3a
            await ValuesAsync(870, State.Tmp0003, State.Tmp0001);       // "Giving n new troops will cost m00K GOLD."
            await WaoAsync(1);                                          // \L_01eef
            await WaoAsync(0);
            await WaoAsync(123);                                        // \L_01e72
            await ValuesAsync(883, State.Tmp0001, State.Tmp0003, State.Gold);
            await WaitForReleaseAsync();                                // L_01358
            var items = new List<SceneMenuItem> { MenuItem(904) };      // Do It
            if (State.Tmp0001 < 10000)
                items.Add(MenuItem(909));                               // Add More
            items.Add(MenuItem(914));                                   // Don't
            int choice = await SelectAsync(items);
            if (choice == 0)
                return Next.Menu;                                       // :918 @L_0117d — QUIRK: CMD-12, the multiplied offer stays
            if (items[choice - 1].At.Line == 909)
            {
                State.Tmp0003 = State.Tmp0003 * 2;                      // L_012a9
                State.Tmp0001 = State.Tmp0001 * 3;
                continue;
            }
            if (items[choice - 1].At.Line == 914)
            {
                await WaoAsync(0);                                      // L_013b8
                return Next.Header;
            }
            break;
        }

        await WaoAsync(0);                                              // L_013c2
        if (State.Gold < State.Tmp0001)
        {
            await WaoAsync(121);
            await TextAsync(927);                                       // "we don't have the budget"
            await WaoAsync(1);
            await WaoAsync(0);
            return Next.Header;
        }
        State.CmdAvail_SubTroops = 0;
        if (State.CmdAvail_SubTalk == 0)
            State.Ui_CmdSubordAvail = 0;
        State.Gold = State.Gold - State.Tmp0001;
        Hist.MilitaryExp = Hist.MilitaryExp + State.Tmp0001;            // QUIRK: CMD-14 — whatever history slot Hist_Cur names
        Unit.Troops = Unit.Troops + State.Tmp0003;
        Unit.MaxTroops = Unit.MaxTroops + State.Tmp0003;
        Unit.Recovery = Unit.Recovery + 1;
        await WaoAsync(113);                                            // \L_01e02
        await ValuesAsync(945, State.Tmp0003, Unit.Troops, Unit.MaxTroops);
        await WaoAsync(1);
        await WaoAsync(0);
        await Script.MapCallAsync(0);
        State.Tmp0001 = 0;
        return Next.Done;
    }

    /// <summary>
    /// <c>L_014b2</c> (<c>:966-973</c>) and <c>L_01757</c> (<c>:1098-1112</c>): the talk is spent;
    /// Rance thinks of meeting the unit; the unit's label plays its talk (<c>Tmp0001</c> 2).
    /// </summary>
    private async Task<Next> TalkChoiceAsync()
    {
        await WaoAsync(0);
        State.Tmp0001 = 2;
        State.CmdAvail_SubTalk = 0;
        if (State.CmdAvail_SubTroops == 0)
            State.Ui_CmdSubordAvail = 0;
        await WaoAsync(312);                                            // L_01757: \L_01e2c
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_01e1e
        await WaoAsync(112);                                            // \L_01de6
        await TextAsync(1104);                                          // "Maybe I'll meet with X."
        await WaoAsync(1);
        await WaoAsync(0);
        await WaoAsync(312);
        State.Tmp0001 = 2;
        return Next.Done;
    }

    /// <summary>
    /// <c>L_014d9</c> (<c>:974-1097</c>): a full unit cannot be replenished; otherwise the deficiency
    /// and its price (one per 50 men, at least 1), Do / Don't, and the troops back to the maximum.
    /// </summary>
    private async Task<Next> ReplenishAsync()
    {
        await WaoAsync(0);                                              // \L_01ee2
        if (Unit.Troops == Unit.MaxTroops)                              // :976-1004
        {
            await WaoAsync(121);                                        // \L_01e3a
            await ValuesAsync(979, Unit.MaxTroops);                     // "already has n troops"
            await WaoAsync(1);
            await WaoAsync(0);
            await WaoAsync(312);                                        // \L_01e2c
            State.CallArg = 1;
            await WaoAsync(311);                                        // \L_01e1e
            await WaoAsync(112);                                        // \L_01de6
            await TextAsync(995);                                       // "What? Damn it."
            await WaoAsync(1);
            await WaoAsync(0);
            await WaoAsync(312);
            State.CallArg = Unit.FaceCg;
            await WaoAsync(311);
            return Next.Header;
        }
        // QUIRK: CMD-20 — above the maximum the deficiency is 0, priced 1, and Do lowers the troops.
        State.Tmp0621 = Unit.MaxTroops - Unit.Troops;                   // :1005-1009
        State.Tmp0226 = State.Tmp0621 / 50;
        if (State.Tmp0226 == 0)
            State.Tmp0226 = 1;
        await WaoAsync(121);                                            // \L_01e3a
        await ValuesAsync(1012, State.Tmp0621, State.Tmp0226);          // "Replenish X's unit? …"
        var items = new List<SceneMenuItem> { MenuItem(1026) };         // Do
        await LineBreakAsync();                                         // :1029 R
        items.Add(MenuItem(1030));                                      // Don't
        int choice = await SelectAsync(items);
        if (choice == 0)
            return Next.Menu;                                           // :1034 @L_0117d
        if (choice == 2)
        {
            await WaoAsync(0);                                          // L_0160a
            await WaoAsync(312);                                        // \L_01e2c
            await WaoAsync(322);                                        // \L_01e9c
            State.Tmp0001 = 1;
            return Next.Done;
        }

        await WaoAsync(0);                                              // L_01622
        if (State.Gold < State.Tmp0226)
        {
            await WaoAsync(121);
            await TextAsync(1046);                                      // "we don't have the budget"
            await WaoAsync(1);
            await WaoAsync(0);
            await WaoAsync(111);                                        // \L_01dca
            await TextAsync(1053);                                      // "Ch..."
            await WaoAsync(1);
            await WaoAsync(0);
            return Next.Header;
        }
        State.CmdAvail_SubTroops = 0;
        if (State.CmdAvail_SubTalk == 0)
            State.Ui_CmdSubordAvail = 0;
        State.Gold = State.Gold - State.Tmp0226;
        Hist.MilitaryExp = Hist.MilitaryExp + State.Tmp0226;            // QUIRK: CMD-14
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_01e1e
        await WaoAsync(111);                                            // \L_01dca
        await TextAsync(1068);                                          // "Replenish the troops."
        await WaoAsync(1);
        await WaoAsync(0);
        await WaoAsync(121);                                            // \L_01e3a
        await TextAsync(1073);                                          // "Understood."
        await WaoAsync(1);
        await WaoAsync(0);
        State.Tmp0001 = Unit.MaxTroops - Unit.Troops;
        Unit.Troops = Unit.MaxTroops;
        await WaoAsync(113);                                            // \L_01e02
        await ValuesAsync(1080, State.Tmp0001, Unit.Troops);            // "X's Y replenished by n to m."
        await WaoAsync(1);
        await WaoAsync(0);
        await WaoAsync(322);                                            // \L_01e9c
        await Script.MapCallAsync(0);
        State.Tmp0001 = 0;
        return Next.Done;
    }

    /// <summary>The <c>Tmp0001 = 2</c> branch of the unit's label (<c>:208-712</c>): its talk.</summary>
    private async Task TalkAsync(int unit)
    {
        switch (unit)
        {
            case 1:
                if (State.Tmp0001 == 2)
                    await SelfTalkAsync();                              // \L_01cf3
                return;
            case 48:                                                    // :706-711
                if (State.Tmp0001 == 2 && State.Unit48_Identity == 1)
                    await Script.CallFileAsync("Gﾊﾆｰ.ADV");
                if (State.Tmp0001 == 2 && State.Unit48_Identity == 2)
                    await Script.CallFileAsync("Gｵｳｼﾞ.ADV");
                return;
            case 49:
            case 50:
                if (State.Tmp0001 == 2)
                    await Script.EventAsync("Gﾖｳﾍｲ.ADV", (Sys3Int)(unit == 49 ? 3 : 4));
                return;
            default:
                if (State.Tmp0001 == 2)
                    await Script.CallFileAsync(TalkFiles[unit]);
                return;
        }
    }

    /// <summary>
    /// <c>L_01cf3</c> (<c>:1338-1377</c>) — Rance talks to himself; the line depends on how often
    /// he has (<c>N_RanceSelfTalk</c>).
    /// </summary>
    private async Task SelfTalkAsync()
    {
        State.N_RanceSelfTalk = State.N_RanceSelfTalk + 1;
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_01e1e
        await WaoAsync(111);                                            // \L_01dca
        await TextAsync(1344);
        await WaoAsync(1);                                              // \L_01eef
        if (State.N_RanceSelfTalk < 10)
        {
            await TextAsync(1348);
            await WaoAsync(1);
            await WaoAsync(0);                                          // \L_01ee2
            return;
        }
        if (State.N_RanceSelfTalk < 20)
        {
            await TextAsync(1357);
            await WaoAsync(1);
            await TextAsync(1366);
            await WaoAsync(1);
            await WaoAsync(0);
            return;
        }
        await TextAsync(1374);
        await WaoAsync(1);
        await WaoAsync(0);
    }

    /// <summary>
    /// <c>L_0179c</c> (<c>:1113-1337</c>) — the unit's status, page by page: level, HP, attack,
    /// experience; strategy, magic, attacks; with troops, the situational modifiers and the troops;
    /// with an item, the item. Each page waits for a key (<c>WAO_SUB</c> op 5); a right click
    /// (<c>RetVal</c> 32) closes the window and leaves.
    /// </summary>
    private async Task StatusPageAsync()
    {
        await WaoAsync(113);                                            // \L_01e02
        await ValuesAsync(1116, Unit.Level, Unit.HP, Unit.MaxHP, Unit.Atk, Unit.Def, Unit.Exp, (Sys3Int)100 + Unit.Level * 30);
        if (await PageEndAsync())
            return;
        await ValuesAsync(1149, Unit.Strategy, Unit.Magic, Unit.Attacks);
        if (await PageEndAsync())
            return;
        if (Unit.MaxTroops != 0)
        {
            await TextAsync(1169);                                      // "Situational Command Ability / On Offense (Atk"
            await ModifierAsync(1174, 1179, Unit.OffAtkMod);
            await TextAsync(1183);                                      // "／Def"
            await ModifierAsync(1186, 1191, Unit.OffDefMod);
            await TextAsync(1195);                                      // ") / On Defense (Atk"
            await ModifierAsync(1200, 1205, Unit.DefAtkMod);
            await TextAsync(1209);
            await ModifierAsync(1212, 1217, Unit.DefDefMod);
            await TextAsync(1221);                                      // ") / In Dungeons (Atk"
            await ModifierAsync(1226, 1231, Unit.DgnAtkMod);
            await TextAsync(1235);
            await ModifierAsync(1238, 1243, Unit.DgnDefMod);
            await TextAsync(1247);                                      // ")"
            if (await PageEndAsync())
                return;
            await ValuesAsync(1254, Unit.Troops, Unit.MaxTroops, Unit.TroopAtk, Unit.TroopDef);
            if (await PageEndAsync())
                return;
        }
        if (Unit.Item != 0)
        {
            State.Item_Cur = Unit.Item;
            CopyString(7, State.Item_StrBase + State.Item_Cur - 1);
            await TextAsync(1280);                                      // "（item）"
            if (Item.Effect == 1)
                await ValuesAsync(1286, Item.Amount);
            if (Item.Effect == 2)
                await ValuesAsync(1293, Item.Amount);
            if (Item.Effect == 3)
                await ValuesAsync(1300, Item.Amount);
            if (Item.Effect == 4)
                await ValuesAsync(1307, Item.Amount);
            if (Item.Effect == 5)
                await ValuesAsync(1314, Item.Amount);
            if (Item.Effect == 6)
                await ValuesAsync(1321, Item.Amount);
            if (Item.Effect == 0)
                await TextAsync(1328);                                  // "Abnormal Data"
            if (await PageEndAsync())
                return;
        }
        await WaoAsync(0);                                              // \L_01ee2
    }

    /// <summary>
    /// A modifier of the status page: values under 10 are bonuses ("＋n"), over 10 penalties
    /// ("－(n−10)"), 10 shows nothing.
    /// </summary>
    private async Task ModifierAsync(int plusLine, int minusLine, Sys3Int modifier)
    {
        if (modifier < 10)
            await ValuesAsync(plusLine, modifier);
        if (modifier > 10)
            await ValuesAsync(minusLine, modifier - 10);
    }

    /// <summary>
    /// <c>\L_01f09: {RetVal = 32: \L_01ee2: \0:}</c> — the key wait at the end of a page; true
    /// (the window closed) when it was a right click.
    /// </summary>
    private async Task<bool> PageEndAsync()
    {
        await WaoAsync(5);
        if (State.RetVal != 32)
            return false;
        await WaoAsync(0);
        return true;
    }
}
