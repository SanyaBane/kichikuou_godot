using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Cﾆﾝｼﾞｬ.ADV</c>, Ninja Maneuver (<c>docs/strategy/survey-commands.md</c> §3.6), ported whole:
/// called from an enemy city's menu (<c>Cﾃｷﾄｼ</c>, <c>Cﾃｷﾄｼ2</c>) with the city in <c>Prov_Cur</c>.
/// Rance picks one of the ninjas not used this week (<c>F_*_NinjaUsed</c> 0; 99 is one not
/// recruited), who is spent at once (CMD-10) — Kanami may interrupt with <c>Gｶﾅﾐ</c> 12 instead,
/// which gives her back — and sends her on a mission: Collect Info (the city's figures; captured on
/// 1 of 100), Sabotage (not against Helman, JAPAN or Zeth before their war; captured over 90 of 100,
/// of 300 against owner 8; then success on 1 of 2, each ninja her own way: Kanami lowers the city's
/// walls, Freya, Kaoru and Wichita hit a commander of the owner's range, CMD-11; Saitou never
/// succeeds), Kanami's burger (<c>Gｶﾅﾐ</c> 11, offered on a draw made for every mission menu, STR-14)
/// or No Mission. A Substitute Ninja in stock (item 8) is destroyed instead of a capture. Leaves the
/// ninja in <c>Bt_ScoutNinja</c>; draws in the order of survey §6.
/// </summary>
public sealed class NinjaCommand(OriginalScriptContext script) : CommandFile(script, "Cﾆﾝｼﾞｬ.ADV")
{
    /// <summary>The ninjas' pictures by <c>Bt_ScoutNinja</c> 1-5: Kanami, Freya, Kaoru, Wichita, Saitou.</summary>
    private static readonly int[] Pictures = [9, 449, 72, 71, 94];

    /// <summary><c>Cﾆﾝｼﾞｬ.ADV:1-330</c>: the command, up to its return.</summary>
    public async Task RunAsync()
    {
        await WaoAsync(0);                                              // \L_0181f
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_0175b: Rance's picture
        await SpeakAsync(111, 6);                                       // "Who should I use?"
        if (State.F_Kanami_NinjaUsed != 0 && State.F_Freya_NinjaUsed != 0 && State.F_Kaoru_NinjaUsed != 0
            && State.F_Wichita_NinjaUsed != 0 && State.F_Saitou_NinjaUsed != 0)   // :11-43
        {
            State.CallArg = 6;
            await WaoAsync(321);                                        // \L_017cb: Maris
            await SpeakAsync(121, 16);                                  // "no ninjas capable of acting"
            await SpeakAsync(111, 25);
            await SpeakAsync(121, 30);
            await SpeakAsync(111, 35);
            await WaoAsync(312);                                        // \L_01769
            await WaoAsync(322);                                        // \L_017d9
            return;
        }

        switch (await PickAsync())
        {
            case 51:
                if (!await KanamiAsync())
                    return;
                break;
            case 57:
                await FreyaAsync();
                break;
            case 63:
                await KaoruAsync();
                break;
            case 69:
                await WichitaAsync();
                break;
            case 75:
                await SaitouAsync();
                break;
            default:                                                    // L_001c9 Stop
                await SpeakAsync(111, 87);                              // "...Nevermind."
                await WaoAsync(312);
                return;
        }
        await MissionAsync();
    }

    /// <summary>
    /// <c>L_00102</c> (<c>:44-83</c>): the ninjas not used this week and Stop; a cancel falls through
    /// to Stop. Returns the chosen item's line.
    /// </summary>
    private async Task<int> PickAsync()
    {
        await WaitForReleaseAsync();
        var items = new List<SceneMenuItem>();
        if (State.F_Kanami_NinjaUsed == 0)
            items.Add(MenuItem(51));
        if (State.F_Freya_NinjaUsed == 0)
            items.Add(MenuItem(57));
        if (State.F_Kaoru_NinjaUsed == 0)
            items.Add(MenuItem(63));
        if (State.F_Wichita_NinjaUsed == 0)
            items.Add(MenuItem(69));
        if (State.F_Saitou_NinjaUsed == 0)
            items.Add(MenuItem(75));
        items.Add(MenuItem(80));                                        // Stop
        int choice = await SelectAsync(items);
        return choice == 0 ? 80 : items[choice - 1].At.Line;
    }

    // -----------------------------------------------------------------------------------------
    // The ninjas
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_001ea</c> (<c>:92-126</c>): Kanami; false when she interrupts with <c>Gｶﾅﾐ</c> 12 (one draw
    /// of 5 while <c>F_Kanami_Scene4</c> is 0), which gives her back and leaves the file.
    /// </summary>
    private async Task<bool> KanamiAsync()
    {
        State.Bt_ScoutNinja = 1;
        State.F_Kanami_NinjaUsed = 1;                                   // QUIRK: CMD-10 — spent on the pick
        Script.Rnd(5);                                                  // :95
        if (State.RetVal == 1 && State.F_Kanami_Scene4 == 0)
        {
            await Script.EventAsync("Gｶﾅﾐ.ADV", 12);
            State.F_Kanami_NinjaUsed = 0;                               // QUIRK: CMD-10 — given back
            State.Bt_ScoutNinja = 0;
            return false;
        }
        await SpeakAsync(111, 105);                                     // "Hey, Kanami."
        await NinjaPictureAsync(9);
        await SpeakAsync(113, 112);
        await SpeakAsync(122, 119);
        return true;
    }

    /// <summary><c>L_00291</c> (<c>:127-156</c>): Freya.</summary>
    private async Task FreyaAsync()
    {
        State.Bt_ScoutNinja = 2;
        State.F_Freya_NinjaUsed = 1;                                    // QUIRK: CMD-10
        await SpeakAsync(111, 132);
        await NinjaPictureAsync(449);
        await SpeakAsync(113, 139);
        await SpeakAsync(122, 146, 151);
    }

    /// <summary><c>L_0031a</c> (<c>:157-190</c>): Kaoru.</summary>
    private async Task KaoruAsync()
    {
        State.Bt_ScoutNinja = 3;
        State.F_Kaoru_NinjaUsed = 1;                                    // QUIRK: CMD-10
        await SpeakAsync(111, 162);
        await NinjaPictureAsync(72);
        await SpeakAsync(121, 169);
        await SpeakAsync(113, 176);
        await SpeakAsync(121, 185);
    }

    /// <summary><c>L_003b4</c> (<c>:191-220</c>): Wichita.</summary>
    private async Task WichitaAsync()
    {
        State.Bt_ScoutNinja = 4;
        State.F_Wichita_NinjaUsed = 1;                                  // QUIRK: CMD-10
        await SpeakAsync(111, 196);
        await NinjaPictureAsync(71);
        await SpeakAsync(121, 203);
        await SpeakAsync(113, 208);
        await SpeakAsync(121, 215);
    }

    /// <summary><c>L_00440</c> (<c>:221-259</c>): Saitou.</summary>
    private async Task SaitouAsync()
    {
        State.Bt_ScoutNinja = 5;
        State.F_Saitou_NinjaUsed = 1;                                   // QUIRK: CMD-10
        await SpeakAsync(111, 226);
        await NinjaPictureAsync(94);
        await SpeakAsync(121, 235);
        await SpeakAsync(113, 240);
        await SpeakAsync(111, 247);
        await SpeakAsync(121, 256);
    }

    /// <summary><c>!CallArg : n! \L_017cb:</c> — the ninja's picture on the other side.</summary>
    private Task NinjaPictureAsync(Sys3Int picture)
    {
        State.CallArg = picture;
        return WaoAsync(321);
    }

    // -----------------------------------------------------------------------------------------
    // The missions
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_004f3</c>, <c>L_00556</c> (<c>:260-330</c>): whether Sabotage is offered (<c>Tmp0003</c>),
    /// the mission menu with its burger draw, the mission; a cancel falls through to No Mission.
    /// </summary>
    private async Task MissionAsync()
    {
        State.Tmp0003 = 1;
        if (Prov.Owner == 6 && State.F_Japan_State == 0)
            State.Tmp0003 = 0;
        if (Prov.Owner == 4 && State.F_Helman_War == 0)
            State.Tmp0003 = 0;
        if (Prov.Owner == 7 && State.F_Zeth_War == 0)
            State.Tmp0003 = 0;
        await SpeakAsync(111, 273);                                     // "Here's your mission..."

        await WaitForReleaseAsync();                                    // L_00556
        Script.Rnd(5);                                                  // :281 QUIRK: STR-14 — for every ninja
        var items = new List<SceneMenuItem> { MenuItem(283) };          // Collect Info
        if (State.Tmp0003 == 1)
            items.Add(MenuItem(288));                                   // Sabotage
        if (State.Bt_ScoutNinja == 1 && State.RetVal == 1)
            items.Add(MenuItem(294));                                   // Get Me A Burger
        items.Add(MenuItem(299));                                       // No Mission
        int choice = await SelectAsync(items);
        switch (choice == 0 ? 299 : items[choice - 1].At.Line)
        {
            case 283:
                await CollectInfoAsync();
                return;
            case 288:
                await SabotageAsync();
                return;
            case 294:                                                   // L_00bb1
                await Script.EventAsync("Gｶﾅﾐ.ADV", 11);
                return;
        }
        await SpeakAsync(111, 306);                                     // L_005d8 "Nothing!"
        await SpeakAsync(121, 311);
        await SpeakAsync(111, 316);
        await WaoAsync(322);
        await SpeakAsync(112, 322);                                     // "Boring girl."
        await WaoAsync(312);
    }

    /// <summary>
    /// <c>L_00657</c> (<c>:331-436</c>): Collect Info — Code's scenes on Kanami's missions into Zeth,
    /// the capture draw (1 of 100), then the city's figures and <c>Ui_CityScouted</c>.
    /// </summary>
    private async Task CollectInfoAsync()
    {
        await SpeakAsync(111, 334);
        await SpeakAsync(121, 341);
        await WaoAsync(322);
        await SpeakAsync(113, 347);                                     // "........."
        if (State.Bt_ScoutNinja == 1 && State.F_Code_State == 0 && Prov.Owner == 7)
            await Script.EventAsync("Gｺｰﾄﾞ.ADV", 1);
        if (State.Bt_ScoutNinja == 1 && State.F_Code_State == 2 && Prov.Owner == 7)
            await Script.EventAsync("Gｺｰﾄﾞ.ADV", 3);
        Script.Rnd(100);                                                // :359
        State.Item_Cur = 8;
        if (Item.Holder == 100 && State.RetVal == 1)
        {
            await SubstituteAsync();
            State.RetVal = 2;
        }
        if (State.RetVal == 1)
        {
            await CapturedAsync();
            return;
        }
        int[] lines = [373, 383, 393, 403, 413];                        // "<ninja> came back with information."
        await ReturnedAsync(lines);
        await ValuesAsync(419, Prov.Economy, Prov.Defense, Prov.BattleArea);
        await WaoAsync(1);
        State.Ui_CityScouted = 1;
        State.MarkProvinceKnown(State.Prov_Cur.Value);                  // ours: the card remembers it (docs/ui/command-screen.md)
        await WaoAsync(0);
        await WaoAsync(322);
        await WaoAsync(312);
    }

    /// <summary>
    /// <c>L_00897</c> (<c>:437-577</c>): Sabotage — the orders, the capture draw (and the redraw against
    /// owner 8), the success draw into <c>Tmp0001</c>, Code's scenes on Kanami's missions into Zeth,
    /// each ninja's report, the map.
    /// </summary>
    private async Task SabotageAsync()
    {
        int[] orders = [441, 450, 459, 468, 477];
        if (Ninja() is int n)
            await SpeakAsync(111, orders[n - 1]);
        await WaoAsync(121);
        int[] answers = [484, 489, 496, 503, 508];
        if (Ninja() is int m)
        {
            await TextAsync(answers[m - 1]);
            await WaoAsync(1);
        }
        await WaoAsync(0);
        await WaoAsync(322);
        await SpeakAsync(113, 517, 521, 525);                           // "........." "......" "..."

        Script.Rnd(100);                                                // :529
        if (Prov.Owner == 8)
            Script.Rnd(300);                                            // :531 QUIRK: STR-14 — the first draw is dropped
        State.Item_Cur = 8;
        if (Item.Holder == 100 && State.RetVal > 90)
        {
            await SubstituteAsync();
            State.RetVal = 1;
        }
        if (State.RetVal > 90)
        {
            await CapturedAsync();
            return;
        }
        Script.Rnd(2);                                                  // :541
        // QUIRK: ENG-02 — a 3 leaves Tmp0001 as it was.
        if (State.RetVal == 1)
            State.Tmp0001 = 1;
        if (State.RetVal == 2)
            State.Tmp0001 = 2;
        if (State.Bt_ScoutNinja == 1 && State.F_Code_State == 0 && Prov.Owner == 7)
        {
            await Script.EventAsync("Gｺｰﾄﾞ.ADV", 1);
            State.CallArg = 1;
            await WaoAsync(311);
        }
        if (State.Bt_ScoutNinja == 1 && State.F_Code_State == 2 && Prov.Owner == 7)
        {
            await Script.EventAsync("Gｺｰﾄﾞ.ADV", 3);
            State.CallArg = 1;
            await WaoAsync(311);
        }
        if (State.Bt_ScoutNinja == 1)
            await KanamiReportAsync();
        if (State.Bt_ScoutNinja == 2)
            await FreyaReportAsync();
        if (State.Bt_ScoutNinja == 3)
            await KaoruReportAsync();
        if (State.Bt_ScoutNinja == 4)
            await WichitaReportAsync();
        if (State.Bt_ScoutNinja == 5)
            await SaitouReportAsync();
        await Script.MapCallAsync(0);
    }

    /// <summary><c>Bt_ScoutNinja</c> when it names a ninja (1-5), else null.</summary>
    private int? Ninja() => State.Bt_ScoutNinja.Value is >= 1 and <= 5 ? State.Bt_ScoutNinja.Value : null;

    /// <summary>
    /// The ninja back (<c>:368-417</c>): her picture and, in the narration, the line of
    /// <paramref name="lines"/> for her, its key wait included; nothing for another <c>Bt_ScoutNinja</c>.
    /// </summary>
    private async Task ReturnedAsync(int[] lines)
    {
        if (Ninja() is not int n)
            return;
        await NinjaPictureAsync((Sys3Int)Pictures[n - 1]);
        await WaoAsync(113);
        await TextAsync(lines[n - 1]);
        await WaoAsync(1);
    }

    /// <summary>
    /// <c>L_00bbd</c> (<c>:582-600</c>): the Substitute Ninja (<c>Item_Cur</c> 8) is destroyed in the
    /// ninja's place.
    /// </summary>
    private async Task SubstituteAsync()
    {
        Item.Holder = 0;
        await SpeakAsync(113, 586, 595);
    }

    /// <summary>
    /// <c>L_00c0b</c> (<c>:601-643</c>): the ninja is captured — Kanami's scene (<c>Gｶﾅﾐ</c> 100), Freya's
    /// (<c>Gﾌﾚｲｱ</c> 100 when caught by Helman at war), the others lost; the map.
    /// </summary>
    private async Task CapturedAsync()
    {
        await WaoAsync(113);
        await TextAsync(604);                                           // "But they never returned."
        await WaoAsync(1);
        if (State.Bt_ScoutNinja == 1)
        {
            await TextAsync(608);
            await WaoAsync(1);
            await Script.EventAsync("Gｶﾅﾐ.ADV", 100);
        }
        if (State.Bt_ScoutNinja == 2)
        {
            await TextAsync(615);
            await WaoAsync(1);
            State.N_Freya_State = 10;
            if (Prov.Owner == 4 && State.F_Helman_War == 1)
                await Script.EventAsync("Gﾌﾚｲｱ.ADV", 100);
        }
        if (State.Bt_ScoutNinja == 3)
        {
            await TextAsync(625);
            await WaoAsync(1);
            State.N_Kaoru_State = 0;
        }
        if (State.Bt_ScoutNinja == 4)
        {
            await TextAsync(631);
            await WaoAsync(1);
            State.N_Wichita_State = 0;
        }
        if (State.Bt_ScoutNinja == 5)
        {
            await TextAsync(637);
            await WaoAsync(1);
            State.F_Saitou_State = 0;
        }
        await Script.MapCallAsync(0);
    }

    // -----------------------------------------------------------------------------------------
    // The reports of a sabotage
    // -----------------------------------------------------------------------------------------

    /// <summary><c>L_00cfb</c> (<c>:644-747</c>): Kanami — on success the city's walls lose one.</summary>
    private async Task KanamiReportAsync()
    {
        await NinjaPictureAsync(9);
        await SpeakAsync(113, 649);                                     // "Kanami returned."
        if (State.Tmp0001 == 1)
        {
            State.Ui_CityScouted = 1;
            State.MarkProvinceKnown(State.Prov_Cur.Value);                  // ours: the card remembers it (docs/ui/command-screen.md)
            await SpeakAsync(111, 656);
            await SpeakAsync(121, 661);
            await SpeakAsync(111, 666);
            await SpeakAsync(121, 675);
            await SpeakAsync(111, 684);
            await SpeakAsync(121, 695);
            Prov.Defense = Prov.Defense - 1;
            return;
        }
        await SpeakAsync(121, 703);
        await SpeakAsync(111, 708);
        await SpeakAsync(121, 715, 718, 721);
        await SpeakAsync(111, 730);
        await SpeakAsync(122, 735);
        await SpeakAsync(113, 742);
    }

    /// <summary>
    /// <c>L_00ee1</c> (<c>:748-852</c>): Freya — on success a commander of the owner's range loses
    /// up to 200 troops (a second draw within his troops when he has fewer than the first).
    /// </summary>
    private async Task FreyaReportAsync()
    {
        if (State.Tmp0001 == 1)
        {
            PickTarget();
            if (Cmdr.Status == 0 || Cmdr.Troops == 0)
                State.Tmp0001 = 0;
            if (Cmdr.Status != 0 && State.Tmp0001 == 1)
            {
                Script.Rnd(200);                                        // :755
                if (Cmdr.Troops < State.RetVal)
                    Script.Rnd(Cmdr.Troops);                            // :757
                State.Tmp0002 = State.RetVal;
                Cmdr.Troops = Cmdr.Troops - State.RetVal;
            }
        }
        await NinjaPictureAsync(449);
        await SpeakAsync(113, 767);                                     // "Freya returned."
        if (State.Tmp0001 == 1)
        {
            State.Ui_CityScouted = 1;
            State.MarkProvinceKnown(State.Prov_Cur.Value);                  // ours: the card remembers it (docs/ui/command-screen.md)
            await SpeakAsync(111, 774);
            await WaoAsync(121);
            await TextAsync(779);
            await WaoAsync(1);
            await TextAsync(782);
            await WaoAsync(1);
            await ValuesAsync(787, State.Tmp0002);                      // "n of <commander>'s troops …"
            await WaoAsync(1);
            await WaoAsync(0);
            await SpeakAsync(111, 800);
            return;
        }
        await SpeakAsync(121, 807);
        await SpeakAsync(111, 812);
        await SpeakAsync(121, 819);
        await SpeakAsync(111, 824);
        await SpeakAsync(121, 829);
        await SpeakAsync(111, 838);
        await SpeakAsync(121, 843);
    }

    /// <summary>
    /// <c>L_010f9</c> (<c>:853-969</c>): Kaoru — on success a commander of the owner's range loses one
    /// troop attack or one troop defence (a draw of 2).
    /// </summary>
    private async Task KaoruReportAsync()
    {
        if (State.Tmp0001 == 1)
        {
            PickTarget();
            if (Cmdr.Status == 0)
                State.Tmp0001 = 0;
            if (Cmdr.Status != 0)
            {
                Script.Rnd(2);                                          // :860
                if (State.RetVal == 1)
                    Cmdr.TroopAtk = Cmdr.TroopAtk - 1;
                if (State.RetVal == 2)
                    Cmdr.TroopDef = Cmdr.TroopDef - 1;
            }
        }
        await NinjaPictureAsync(72);
        await SpeakAsync(113, 873);                                     // "Kaoru returned."
        if (State.Tmp0001 == 1)
        {
            State.Ui_CityScouted = 1;
            State.MarkProvinceKnown(State.Prov_Cur.Value);                  // ours: the card remembers it (docs/ui/command-screen.md)
            await SpeakAsync(111, 880);
            await SpeakAsync(121, 887, 892, 903);
            await SpeakAsync(111, 914);
            return;
        }
        await SpeakAsync(121, 923);
        await SpeakAsync(111, 928);
        await SpeakAsync(121, 933);
        await SpeakAsync(113, 940);
        await SpeakAsync(111, 947);
        await SpeakAsync(121, 954);
        await SpeakAsync(111, 961);
        await SpeakAsync(113, 966);
    }

    /// <summary>
    /// <c>L_0133a</c> (<c>:970-1090</c>): Wichita — on success a commander of the owner's range
    /// recovers half as fast.
    /// </summary>
    private async Task WichitaReportAsync()
    {
        if (State.Tmp0001 == 1)
        {
            PickTarget();
            if (Cmdr.Status == 0)
                State.Tmp0001 = 0;
            if (Cmdr.Status != 0)
                Cmdr.Recovery = Cmdr.Recovery / 2;
        }
        await NinjaPictureAsync(71);
        await SpeakAsync(113, 984);                                     // "Wichita returned."
        if (State.Tmp0001 == 1)
        {
            State.Ui_CityScouted = 1;
            State.MarkProvinceKnown(State.Prov_Cur.Value);                  // ours: the card remembers it (docs/ui/command-screen.md)
            await SpeakAsync(111, 991);
            await SpeakAsync(121, 996, 1007);
            await SpeakAsync(111, 1020);
            return;
        }
        await SpeakAsync(121, 1027);
        await SpeakAsync(111, 1032);
        await SpeakAsync(121, 1039);
        await SpeakAsync(113, 1050);
        await SpeakAsync(111, 1059);
        await SpeakAsync(121, 1064);
        await SpeakAsync(111, 1073);
        await SpeakAsync(121, 1082);
        await SpeakAsync(113, 1087);
    }

    /// <summary><c>L_0157f</c> (<c>:1091-1148</c>): Saitou — never anything, whatever the draw (CMD-11).</summary>
    private async Task SaitouReportAsync()
    {
        await NinjaPictureAsync(94);
        await SpeakAsync(113, 1096);                                    // "Saitou the Spy returned."
        await SpeakAsync(111, 1101);
        await SpeakAsync(121, 1106);
        await SpeakAsync(111, 1111);
        await SpeakAsync(121, 1116);
        await SpeakAsync(111, 1123);
        await SpeakAsync(121, 1132);
        await WaoAsync(322);
        await SpeakAsync(113, 1138);
        await SpeakAsync(111, 1143);
    }

    /// <summary>
    /// <c>L_01691</c> (<c>:1149-1166</c>): the sabotage's target — commander record 100 zeroed (EVT-04),
    /// then a random commander of the city owner's range: Helman 22-51, JAPAN 71-82, Zeth 101-128;
    /// any other owner leaves record 100 (CMD-11). His name into string 4.
    /// </summary>
    private void PickTarget()
    {
        // QUIRK: EVT-04 — the shared scratch record is zeroed on every call.
        State.Cmdr_Cur = 100;
        Cmdr.Status = 0;
        Cmdr.Troops = 0;
        // QUIRK: CMD-11 — only Helman, JAPAN and Zeth have a range; the draw names any commander of it.
        if (Prov.Owner == 4)
        {
            Script.Rnd(30);                                             // :1154
            State.Cmdr_Cur = State.RetVal + 21;
        }
        if (Prov.Owner == 6)
        {
            Script.Rnd(12);                                             // :1158
            State.Cmdr_Cur = State.RetVal + 70;
        }
        if (Prov.Owner == 7)
        {
            Script.Rnd(28);                                             // :1162
            State.Cmdr_Cur = State.RetVal + 100;
        }
        CopyString(4, State.Cmdr_StrBase + (State.Cmdr_Cur - 1) * 3);
    }
}
