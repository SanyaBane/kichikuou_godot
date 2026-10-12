using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Cｼｮｳﾆﾝ.ADV</c>, the Merchant command (<c>docs/strategy/survey-commands.md</c> §3.4), ported
/// whole: Pluepet is summoned (the first meeting, the new merchant after his "death", or the
/// greeting), then the menu loops until Send Away, which alone spends the week's merchant —
/// Buy Item (the shop's eight items, each sold once), Fuck Rebecca (once a visit) and Buy Rebecca
/// while <c>F_Merchant_State</c> is under 3, and the two mercenary units while they are free.
/// Every purchase is booked to <c>Hist_SundryExp</c> of whatever record <c>Hist_Cur</c> names.
/// </summary>
public sealed class MerchantCommand(OriginalScriptContext script) : CommandFile(script, "Cｼｮｳﾆﾝ.ADV")
{
    /// <summary><c>Cｼｮｳﾆﾝ.ADV:1-153</c>: the command, up to its return.</summary>
    public async Task RunAsync()
    {
        State.Tmp0001 = 13;                                             // the merchant's picture
        if (State.F_Rebecca_State > 1)
            State.Tmp0001 = 470;
        State.Tmp0002 = 0;
        State.Tmp0003 = 0;
        State.CallArg = 1001;
        await WaoAsync(331);                                            // \L_01fb5
        // QUIRK: CMD-08 — Sill's two scenes leave without spending the week's merchant.
        if (State.F_SillCow_State == 6)                                 // :9-18
        {
            await Script.EventAsync("Gｼｨﾙ.ADV", 6);
            return;
        }
        if (State.F_SillCow_State == 2)
        {
            await Script.EventAsync("Gｼｨﾙ.ADV", 2);
            return;
        }
        if (State.F_Merchant_State == 0)                                // :19-24
            await FirstMeetingAsync();
        else if (State.F_Merchant_State == 3)
            await NewMerchantAsync();
        else if (State.F_Merchant_State > 3)                            // :25-56
        {
            State.CallArg = 1;
            await WaoAsync(311);                                        // \L_01f0d
            await SpeakAsync(111, 30);                                  // "I want to shop. Call Pluepet."
            await SpeakAsync(113, 37);
            State.Tmp0001 = 470;
            State.CallArg = State.Tmp0001;
            await WaoAsync(321);                                        // \L_01f7d
            await SpeakAsync(121, 47);
            await SpeakAsync(123, 52);
        }
        else                                                            // :57-88
        {
            State.CallArg = 1;
            await WaoAsync(311);
            await SpeakAsync(111, 61);
            await SpeakAsync(113, 66);
            State.CallArg = State.Tmp0001;
            await WaoAsync(321);
            await SpeakAsync(121, 75);
            await SpeakAsync(123, 84);
        }

        while (await MenuAsync())
        {
        }
    }

    /// <summary>
    /// <c>L_001dc</c> (<c>:89-129</c>): the menu, a cancel shows it again; false once Pluepet is
    /// sent away (<c>L_002b1</c>, <c>:130-153</c>: the week's merchant spent, the map back).
    /// </summary>
    private async Task<bool> MenuAsync()
    {
        await WaitForReleaseAsync();
        var items = new List<SceneMenuItem> { MenuItem(95) };          // Buy Item
        if (State.F_Merchant_State < 3 && State.Tmp0002 == 0)
            items.Add(MenuItem(100));                                   // Fuck Rebecca
        if (State.F_Merchant_State < 3)
            items.Add(MenuItem(106));                                   // Buy Rebecca
        State.Unit_Cur = 49;
        if (State.F_MercA_Lost == 0 && Unit.Status == 0)
            items.Add(MenuItem(113));                                   // Hire Mercenary A
        State.Unit_Cur = 50;
        if (State.F_MercB_Lost == 0 && Unit.Status == 0)
            items.Add(MenuItem(120));                                   // Hire Mercenary B
        items.Add(MenuItem(125));                                       // Send Away
        int choice = await SelectAsync(items);
        if (choice == 0)
            return true;                                                // :129 @L_001dc
        switch (items[choice - 1].At.Line)
        {
            case 95:
                await ShopAsync();
                return true;
            case 100:                                                   // L_010c2
                State.Tmp0002 = 1;
                await Script.EventAsync("Gﾚﾍﾞｯｶ.ADV", 1);
                return true;
            case 106:
                await BuyRebeccaAsync();
                return true;
            case 113:
                await HireLouisAsync();
                return true;
            case 120:
                await HireCecilAsync();
                return true;
        }
        State.Ui_CmdMerchantAvail = 0;                                  // L_002b1
        await SpeakAsync(111, 134);                                     // "Enough. Go home."
        await SpeakAsync(121, 141);
        await SpeakAsync(113, 148);
        await Script.MapCallAsync(0);
        return false;
    }

    // -----------------------------------------------------------------------------------------
    // The mercenaries
    // -----------------------------------------------------------------------------------------

    /// <summary><c>L_003eb</c> (<c>:205-302</c>): Louis (unit 49), 200 men for 40 or 1000 for 80.</summary>
    private async Task HireLouisAsync()
    {
        await SpeakAsync(121, 208, 217, 222, 227, 232, 239);
        await WaoAsync(113);                                            // \L_01ef1
        await ValuesAsync(250, State.Gold);                             // "[Louis Unit] … (Budget: n00KG)"
        int line = await ContractAsync(266, 270, 274);                  // L_004d4
        if (line == 274)
        {
            await NoContractAsync();
            return;
        }
        await WaoAsync(0);                                              // \L_01fd1
        // QUIRK: CMD-06 — the test is one gold under the price, which floors Gold at 0.
        if (line == 266 ? State.Gold < 39 : State.Gold < 79)            // L_0051f, L_00563
        {
            await NoMoneyAsync();
            return;
        }
        var price = (Sys3Int)(line == 266 ? 40 : 80);
        State.Gold = State.Gold - price;
        Hist.SundryExp = Hist.SundryExp + price;                        // QUIRK: CMD-14 — whatever history slot Hist_Cur names
        State.Unit_Cur = 49;
        Unit.Status = 1;
        Unit.Troops = (Sys3Int)(line == 266 ? 200 : 1000);
        Unit.MaxTroops = (Sys3Int)(line == 266 ? 200 : 1000);

        if (State.F_Louis_HiredBefore == 1)                             // L_005aa
        {
            await SpeakAsync(111, 307);
            await SpeakAsync(121, 314, 321, 326, 329);
            await SpeakAsync(113, 334);
            await ShowAsync(90);
            await SpeakAsync(121, 342);
            await SpeakAsync(111, 349);
            await SpeakAsync(121, 354);
            await SpeakAsync(123, 363);
            await ShowAsync(State.Tmp0001);
            await SpeakAsync(111, 373);
            return;
        }
        State.F_Louis_HiredBefore = 1;
        await SpeakAsync(121, 381, 386);
        await SpeakAsync(113, 393);
        await ShowAsync(90);
        await SpeakAsync(121, 403);
        await SpeakAsync(112, 414);
        await SpeakAsync(121, 419);
        await SpeakAsync(113, 430);
        await SpeakAsync(121, 435);
        await SpeakAsync(113, 440);
        await SpeakAsync(121, 447);
        await SpeakAsync(123, 452);
        await SpeakAsync(111, 461);
        await SpeakAsync(121, 472);
        await SpeakAsync(111, 479);
        await SpeakAsync(121, 490);
        await SpeakAsync(123, 501);
        await ShowAsync(State.Tmp0001);
        await SpeakAsync(111, 511);
        await SpeakAsync(121, 522);
    }

    /// <summary><c>L_009a8</c> (<c>:528-639</c>): Cecil (unit 50), 1000 men for 30 or 5000 for 110.</summary>
    private async Task HireCecilAsync()
    {
        await SpeakAsync(121, 531, 540, 547, 554, 559, 564, 569, 576);
        await WaoAsync(113);
        await ValuesAsync(587, State.Gold);                             // "[Cecil Unit] … (Budget: n00KG)"
        int line = await ContractAsync(603, 607, 611);                  // L_00acb
        if (line == 611)
        {
            await NoContractAsync();
            return;
        }
        await WaoAsync(0);
        // QUIRK: CMD-06 — the test is one gold under the price, which floors Gold at 0.
        if (line == 603 ? State.Gold < 29 : State.Gold < 109)           // L_00b16, L_00b5a
        {
            await NoMoneyAsync();
            return;
        }
        var price = (Sys3Int)(line == 603 ? 30 : 110);
        State.Gold = State.Gold - price;
        Hist.SundryExp = Hist.SundryExp + price;                        // QUIRK: CMD-14
        State.Unit_Cur = 50;
        Unit.Status = 1;
        Unit.Troops = (Sys3Int)(line == 603 ? 1000 : 5000);
        Unit.MaxTroops = (Sys3Int)(line == 603 ? 1000 : 5000);

        if (State.F_Cecil_HiredBefore == 1)                             // L_00ba1
        {
            await SpeakAsync(111, 644);
            await SpeakAsync(121, 653, 660, 665, 672);
            await ShowAsync(89);
            await SpeakAsync(121, 682);
            await SpeakAsync(111, 687);
            await SpeakAsync(121, 694);
            await WaoAsync(322);                                        // \L_01f8b
            await SpeakAsync(113, 700);
            await SpeakAsync(111, 707);
            State.CallArg = State.Tmp0001;
            await WaoAsync(321);
            await SpeakAsync(121, 718);
            return;
        }
        State.F_Cecil_HiredBefore = 1;
        await SpeakAsync(121, 728, 733);
        await ShowAsync(89);
        await SpeakAsync(121, 745);
        await SpeakAsync(111, 750);
        await SpeakAsync(121, 755);
        await SpeakAsync(111, 762);
        await SpeakAsync(123, 773, 780);
        await SpeakAsync(111, 785);
        await SpeakAsync(113, 790);
        await SpeakAsync(111, 795);
        await SpeakAsync(121, 800);
        await SpeakAsync(111, 809);
        await SpeakAsync(121, 816);
        await SpeakAsync(111, 827, 830);
        await SpeakAsync(121, 835);
        await SpeakAsync(111, 840);
        await SpeakAsync(121, 845, 852);
        await ShowAsync(State.Tmp0001);
        await SpeakAsync(121, 864);
        await SpeakAsync(111, 869);
        await ShowAsync(89);
        await SpeakAsync(121, 881);
        await WaoAsync(322);
        await SpeakAsync(113, 891);
        await ShowAsync(State.Tmp0001);
        await SpeakAsync(121, 901);
        await SpeakAsync(111, 906);
        await SpeakAsync(121, 911);
        await SpeakAsync(111, 922);
    }

    /// <summary>
    /// The contract menu (<c>L_004d4</c>, <c>L_00acb</c>): two contracts and No Contract, a line break
    /// between items; a cancel shows it again. Returns the chosen item's line.
    /// </summary>
    private async Task<int> ContractAsync(int first, int second, int none)
    {
        while (true)
        {
            await WaitForReleaseAsync();
            var items = new List<SceneMenuItem> { MenuItem(first) };
            await LineBreakAsync();
            items.Add(MenuItem(second));
            await LineBreakAsync();
            items.Add(MenuItem(none));
            int choice = await SelectAsync(items);
            if (choice != 0)
                return items[choice - 1].At.Line;
        }
    }

    /// <summary><c>L_0031a</c> (<c>:154-172</c>): no contract.</summary>
    private async Task NoContractAsync()
    {
        await WaoAsync(0);
        await SpeakAsync(121, 158, 165);
    }

    /// <summary><c>L_00368</c> (<c>:173-204</c>): not enough money for the contract.</summary>
    private Task NoMoneyAsync() => SpeakAsync(121, 176, 183, 190, 195);

    /// <summary><c>\L_01f8b</c>, <c>CallArg</c>, <c>\L_01f7d</c>: the other side's picture replaced by <paramref name="picture"/>.</summary>
    private async Task ShowAsync(Sys3Int picture)
    {
        await WaoAsync(322);
        State.CallArg = picture;
        await WaoAsync(321);
    }

    // -----------------------------------------------------------------------------------------
    // The shop
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_01316</c> (<c>:1059-1400</c>): the budget and the items not owned yet (<c>Item_Holder</c>
    /// 0; the birdlime while <c>N_Freya_State</c> is 3, the clothes while Sill is present and has
    /// none, the lemon while unit 32 is present, the fog card while it has a price); an item's page
    /// with its price, Buy or Don't Buy; Don't Buy in the list goes back to the menu.
    /// </summary>
    private async Task ShopAsync()
    {
        await SpeakAsync(111, 1062);
        await SpeakAsync(121, 1067);
        while (true)
        {
            await WaoAsync(113);                                        // L_0134f
            await ValuesAsync(1075, State.Gold);                        // "National Budget n00K GOLD"
            int pick = await ItemListAsync();
            if (pick == 1144)
            {
                await WaoAsync(0);                                      // L_014e7
                return;
            }
            (Sys3Int Price, int Item, int Page) offer = pick switch
            {
                1124 => (1, 19, 1299),                         // L_01782 Clothes
                1132 => (500, 43, 1311),                                // L_017b1 Hirami Lemon
                1139 => (State.FogCard_Price, 28, 1323),                // L_017e1 Fog Card
                1088 => (500, 3, 1338),                                 // L_01825 Popularity Staff
                1116 => (1000, 8, 1352),                                // L_0185c Substitute Ninja
                1095 => (800, 4, 1366),                                 // L_01893 Napoleon's Writings
                1102 => (800, 5, 1380),                                 // L_018ca Shingen's Writings
                _ => (30, 44, 1394),                                    // L_01901 Ninja Birdlime
            };
            State.Tmp0003 = offer.Price;
            State.Tmp0004 = (Sys3Int)offer.Item;
            await WaoAsync(0);
            await WaoAsync(123);                                        // \L_01f61
            if (offer.Page == 1323)
                await ValuesAsync(offer.Page, State.FogCard_Price);
            else
                await TextAsync(offer.Page);

            // QUIRK: CMD-06 — one gold under the price buys it; the Clothes are free at 0.
            if (State.Gold < State.Tmp0003 - 1)                         // L_014f1
            {
                await WaoAsync(1);
                await WaoAsync(0);
                await SpeakAsync(111, 1158);
                await SpeakAsync(121, 1163, 1168);
                continue;
            }
            if (await BuyAsync())                                       // L_01567
            {
                State.Gold = State.Gold - State.Tmp0003;                // L_015a5
                Hist.SundryExp = Hist.SundryExp + State.Tmp0003;        // QUIRK: CMD-14
                State.Item_Cur = State.Tmp0004;
                Item.Holder = 100;
                if (State.Item_Cur == 19)
                    State.F_SillClothes_State = 1;
                await WaoAsync(0);
                await SpeakAsync(111, 1203);
                await SpeakAsync(121, 1208, 1211);
                await SpeakAsync(123, 1216);
                await SpeakAsync(121, 1225);
                await SpeakAsync(111, 1234);
                await SpeakAsync(121, 1241, 1244);
                await SpeakAsync(123, 1251);
                await SpeakAsync(121, 1258);
            }
            else
            {
                await WaoAsync(0);                                      // L_016fc
                await SpeakAsync(111, 1268);
                await SpeakAsync(121, 1273, 1276, 1279, 1282, 1289);
            }
        }
    }

    /// <summary><c>L_01373</c> (<c>:1080-1148</c>): the items on sale; a cancel shows the list again. Returns the item's line.</summary>
    private async Task<int> ItemListAsync()
    {
        while (true)
        {
            await WaitForReleaseAsync();
            var items = new List<SceneMenuItem>();
            State.Item_Cur = 3;
            if (Item.Holder == 0)
                items.Add(MenuItem(1088));                              // Popularity Staff
            State.Item_Cur = 4;
            if (Item.Holder == 0)
                items.Add(MenuItem(1095));                              // Napoleon's Writings
            State.Item_Cur = 5;
            if (Item.Holder == 0)
                items.Add(MenuItem(1102));                              // Shingen's Writings
            State.Item_Cur = 44;
            if (Item.Holder == 0 && State.N_Freya_State == 3)
                items.Add(MenuItem(1109));                              // Ninja Birdlime
            State.Item_Cur = 8;
            if (Item.Holder == 0)
                items.Add(MenuItem(1116));                              // Substitute Ninja
            State.Item_Cur = 19;
            State.Unit_Cur = 2;
            if (Unit.Status != 0 && State.F_SillClothes_State == 0 && Item.Holder == 0)
                items.Add(MenuItem(1124));                              // Clothes
            State.Item_Cur = 43;
            State.Unit_Cur = 32;
            if (Unit.Status != 0 && Item.Holder == 0)
                items.Add(MenuItem(1132));                              // Hirami Lemon
            State.Item_Cur = 28;
            if (Item.Holder == 0 && State.FogCard_Price != 0)
                items.Add(MenuItem(1139));                              // Fog Card
            items.Add(MenuItem(1144));                                  // Don't Buy
            int choice = await SelectAsync(items);
            if (choice != 0)
                return items[choice - 1].At.Line;
        }
    }

    /// <summary><c>L_01567</c> (<c>:1177-1191</c>): Buy or Don't Buy; a cancel shows it again.</summary>
    private async Task<bool> BuyAsync()
    {
        while (true)
        {
            await WaitForReleaseAsync();
            var items = new List<SceneMenuItem> { MenuItem(1183) };
            await LineBreakAsync();
            items.Add(MenuItem(1187));
            int choice = await SelectAsync(items);
            if (choice != 0)
                return choice == 1;
        }
    }

    // -----------------------------------------------------------------------------------------
    // Rebecca and the merchant's arrivals
    // -----------------------------------------------------------------------------------------

    /// <summary><c>L_010d4</c> (<c>:931-1058</c>): Rance tries to buy Rebecca; nothing changes.</summary>
    private async Task BuyRebeccaAsync()
    {
        await SpeakAsync(111, 934);
        await SpeakAsync(121, 939);
        await SpeakAsync(111, 944);
        await SpeakAsync(121, 951);
        await SpeakAsync(111, 962);
        await SpeakAsync(121, 969);
        await SpeakAsync(111, 976);
        await SpeakAsync(121, 983);
        await SpeakAsync(111, 988);
        await SpeakAsync(113, 997);
        await SpeakAsync(121, 1006);
        await SpeakAsync(111, 1013);
        await SpeakAsync(121, 1018);
        await SpeakAsync(111, 1025);
        await SpeakAsync(121, 1030, 1037);
        await SpeakAsync(111, 1048);
        await SpeakAsync(113, 1053);
    }

    /// <summary><c>L_01931</c> (<c>:1401-1593</c>): the first summons; Pluepet and Rebecca, then Lia.</summary>
    private async Task FirstMeetingAsync()
    {
        State.F_Merchant_State = 1;
        State.CallArg = 1;
        await WaoAsync(311);
        await SpeakAsync(112, 1407);
        await SpeakAsync(113, 1414, 1419);
        State.CallArg = State.Tmp0001;
        await WaoAsync(321);
        await SpeakAsync(113, 1426);
        await SpeakAsync(111, 1433, 1438);
        await SpeakAsync(121, 1447);
        await SpeakAsync(111, 1454);
        await SpeakAsync(121, 1459);
        await SpeakAsync(111, 1466);
        await SpeakAsync(121, 1471, 1476);
        await SpeakAsync(123, 1485);
        await SpeakAsync(111, 1492);
        await SpeakAsync(121, 1499, 1502);
        await SpeakAsync(111, 1509);
        await SpeakAsync(121, 1514);
        await SpeakAsync(111, 1521);
        await SpeakAsync(121, 1526, 1531, 1538);
        await SpeakAsync(123, 1549);
        await SpeakAsync(121, 1554, 1563);
        State.CallArg = 5;
        await WaoAsync(321);
        await SpeakAsync(121, 1574, 1577, 1582);
        State.CallArg = State.Tmp0001;
        await WaoAsync(321);
    }

    /// <summary><c>L_01c97</c> (<c>:1594-1714</c>): Pluepet back after his "death" (state 3 → 4).</summary>
    private async Task NewMerchantAsync()
    {
        State.F_Merchant_State = 4;
        State.CallArg = 1;
        await WaoAsync(311);
        await SpeakAsync(112, 1600, 1603, 1606, 1613, 1620);
        await SpeakAsync(111, 1629);
        await SpeakAsync(113, 1634);
        await SpeakAsync(121, 1641);
        await SpeakAsync(112, 1646);
        State.CallArg = State.Tmp0001;
        await WaoAsync(321);
        await SpeakAsync(121, 1653);
        await SpeakAsync(111, 1658);
        await SpeakAsync(121, 1663);
        await SpeakAsync(111, 1670);
        await SpeakAsync(121, 1679, 1686, 1693);
        await SpeakAsync(111, 1704);
        await SpeakAsync(121, 1711);
    }
}
