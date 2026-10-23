using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Sｱｲﾃﾑ.ADV</c>, the item screen of the Treasure command (<c>Cﾀｶﾗ</c>;
/// <c>docs/strategy/survey-commands.md</c> §3.9, §7): the grid lists either the units with a
/// status (view 0, <c>Ui_ItemViewMode</c>, kept between visits) or the items owned (view 1:
/// <c>Item_Holder</c> 100 in stock, 1-50 the unit equipping it). A pick in one view selects
/// (<c>Ui_ItemUnitSel</c> / <c>Ui_ItemSel</c>) and turns to the other; the second pick equips the
/// item on the unit, the unit's own item going back to stock (<c>L_0043a</c>). An item another unit
/// holds is first taken off it after a Yes (<c>L_00b39</c>); in the item view without a unit
/// picked, a held item can only be taken off. Chaos and Nikkou (items 1 and 2) never move, and neither does what a
/// unit holding one of them equips; an item without effect cannot be equipped. The bar: its first
/// place (997, no label) is taken for a pick (CMD-17), "Item/Character" turns the view (502), "End"
/// (503) leaves; a right click (998) drops a half-made pick, or leaves when there is none.
/// </summary>
public sealed class ItemScreen(OriginalScriptContext script, IScenarioSource scenario) : PickerScreen(script, "Sｱｲﾃﾑ.ADV", scenario)
{
    /// <summary>The cursor of the view: <c>Unit_Cur</c> in view 0, <c>Item_Cur</c> otherwise.</summary>
    protected override Sys3Int Cursor
    {
        get => State.Ui_ItemViewMode == 0 ? State.Unit_Cur : State.Item_Cur;
        set
        {
            if (State.Ui_ItemViewMode == 0)
                State.Unit_Cur = value;
            else
                State.Item_Cur = value;
        }
    }

    /// <summary>Units with a status in view 0, owned items in view 1, nothing in any other view.</summary>
    protected override bool Listed =>
        State.Ui_ItemViewMode == 0 ? Unit.Status != 0 : State.Ui_ItemViewMode == 1 && Item.Holder != 0;

    /// <inheritdoc />
    protected override bool HasList => State.Ui_ItemViewMode == 0 || State.Ui_ItemViewMode == 1;

    /// <inheritdoc />
    protected override bool MarksUnlisted => true;

    /// <inheritdoc />
    protected override Sys3Int RightClickCode => 998;

    /// <summary><c>:444-445</c>: both cursors follow the pointer.</summary>
    protected override void PointAt(Sys3Int code)
    {
        State.Unit_Cur = code;
        State.Item_Cur = code;
    }

    /// <summary><c>Sｱｲﾃﾑ.ADV:1-169</c>: the screen, the picks until "End" or a right click, the map back.</summary>
    public async Task RunAsync()
    {
        await Script.ShowAsync(new PickerScreenEvent(PickerKind.Items, Sys3Int.Zero, true));
        await EngineAsync("G", 819, 40);
        await EngineAsync("CC", 0, 0, 640, 400, 0, 400);
        State.Ui_ItemSel = 0;
        State.Ui_ItemUnitSel = 0;
        do
            await HeaderAsync();                                        // L_00042
        while (await ChooseAsync());

        // L_004bf
        await EngineAsync("B2", 1, 1, 0, 0, 0, 0);
        await EngineAsync("ZM", 24);
        await EngineAsync("ZC", 1, 15);
        State.Unit_Cur = State.Ui_ItemUnitSel;
        State.Item_Cur = State.Ui_ItemSel;
        await Script.ShowAsync(new PickerScreenEvent(PickerKind.Items, Sys3Int.Zero, false));
        await Script.MapCallAsync(0);
    }

    /// <summary><c>L_00042</c> (<c>:5-46</c>): the view's background and the bar's labels, kept at (640, 400).</summary>
    private async Task HeaderAsync()
    {
        await EngineAsync("J1", 0, 400);
        if (State.Ui_ItemViewMode == 0)
            await EngineAsync("G", 600);
        if (State.Ui_ItemViewMode == 1)
            await EngineAsync("G", 692);
        await EngineAsync("CC", 0, 400, 640, 400, 0, 0);
        await EngineAsync("ZM", 12);
        await BarLabelAsync(16, 452 - 8, 332);                          // MS 1, "Item"
        await BarLabelAsync(25, 475 - 8, 346);                          // MS 1, "/Character"
        await EngineAsync("ZM", 24);
        await BarLabelAsync(36, 550 + 12);                              // MS 1, "End"
        await EngineAsync("CC", 0, 0, 640, 400, 640, 400);
        await EngineAsync("ZC", 1, 255);
    }

    /// <summary>
    /// <c>L_00170</c> (<c>:47-138</c>): picks until the screen is drawn again (true, <c>@L_00042</c>:
    /// the view turned, an item equipped or taken off, a half-made pick dropped) or left (false,
    /// <c>@L_004bf</c>). A first pick turns the view and shows what is picked; the second equips.
    /// </summary>
    private async Task<bool> ChooseAsync()
    {
        while (true)                                                    // L_00170
        {
            await PickAsync();                                          // \L_004ee
            if (State.CallArg == 0)
                continue;
            if (State.CallArg == 502)
            {
                State.Ui_ItemSel = 0;
                State.Ui_ItemUnitSel = 0;
                await PlaySoundAsync(108);                              // \L_010d2
                if (State.Ui_ItemViewMode == 0)
                {
                    State.Ui_ItemViewMode = 1;
                    return true;
                }
                if (State.Ui_ItemViewMode == 1)
                {
                    State.Ui_ItemViewMode = 0;
                    return true;
                }
            }
            if (State.CallArg == 503)
                return false;
            if (State.CallArg == 504 && (State.Ui_ItemUnitSel == 0 || State.Ui_ItemSel == 0))
            {
                // The Yes to taking a held item off (item view, no unit picked): both picks are 0,
                // so L_00b39 takes off the item and the holder the question left in the cursors.
                State.Tmp0021 = State.Ui_ItemSel;
                State.Tmp0022 = State.Ui_ItemUnitSel;
                await PlaySoundAsync(110);                              // \L_01141
                Unequip();                                              // \L_00b39
                return true;
            }
            if (State.CallArg == 998)
            {
                if (State.Ui_ItemSel + State.Ui_ItemUnitSel != 0)
                {
                    State.Ui_ItemSel = 0;
                    State.Ui_ItemUnitSel = 0;
                    if (State.Ui_ItemViewMode == 0)
                    {
                        State.Ui_ItemViewMode = 1;
                        return true;
                    }
                    if (State.Ui_ItemViewMode == 1)
                    {
                        State.Ui_ItemViewMode = 0;
                        return true;
                    }
                }
                State.CallArg = 503;
                return false;
            }
            if (State.CallArg == 999)
            {
                State.CallArg = 503;
                return false;
            }
            if (State.Ui_ItemViewMode == 0)
            {
                // QUIRK: CMD-17 — the bar's first place (997) is taken for a unit.
                State.Ui_ItemUnitSel = State.CallArg;
                State.Ui_ItemViewMode = 1;
                if (State.Ui_ItemSel == 0)
                {
                    await PromptAsync();
                    await StringsAsync(104, State.Unit_StrBase + (State.Ui_ItemUnitSel - 1) * 3);   // "<unit>"
                    await EngineAsync("T", 640 + 11 + 202, 400 + 273 + 80);
                    await TextAsync(107);                               // " is equipping"
                    await EngineAsync("T", 640 + 11 + 202, 400 + 273 + 96);
                    await TextAsync(110);                               // "Which item?"
                }
            }
            else if (State.Ui_ItemViewMode == 1)
            {
                // QUIRK: CMD-17 — the bar's first place (997) is taken for an item.
                State.Ui_ItemSel = State.CallArg;
                State.Ui_ItemViewMode = 0;
                if (State.Ui_ItemUnitSel == 0)
                {
                    await PromptAsync();
                    await TextAsync(121);                               // "Equipping"
                    await EngineAsync("T", 640 + 11 + 202, 400 + 273 + 80);
                    await StringsAsync(125, State.Item_StrBase + State.Ui_ItemSel - 1);              // "<item>"
                    await EngineAsync("T", 640 + 11 + 202, 400 + 273 + 96);
                    await TextAsync(128);                               // "Which character?"
                }
            }

            // L_00405
            if (State.Ui_ItemSel != 0 && State.Ui_ItemUnitSel != 0)
            {
                await EquipAsync();                                     // L_0043a
                return true;
            }
            await PlaySoundAsync(108);                                  // \L_010d2
            await EngineAsync("CC", 640, 400, 640, 400, 0, 0);
        }
    }

    /// <summary>
    /// <c>:101-102</c>, <c>:118-119</c>: the first pick's prompt is drawn on the kept screen — the
    /// details panel shrunk to (11, 273) and the text right of it — which the screen gets back at
    /// <c>L_00405</c>.
    /// </summary>
    private async Task PromptAsync()
    {
        await EngineAsync("CM", 6, 6, 314, 196, 640 + 11, 400 + 273, 186, 116, 0);
        await EngineAsync("T", 640 + 11 + 202, 400 + 273 + 64);
    }

    /// <summary>
    /// <c>L_0043a</c> (<c>:139-160</c>): the unit's own item back to stock, the item taken from
    /// its holder (always the stock by now, so the write lands on <c>Unit_Item[100]</c>), then
    /// the item on the unit; both picks cleared.
    /// </summary>
    private async Task EquipAsync()
    {
        State.Unit_Cur = State.Ui_ItemUnitSel;
        State.Item_Cur = State.Ui_ItemSel;
        State.Tmp0023 = Unit.Item;
        State.Tmp0024 = Item.Holder;
        if (State.Tmp0023 != 0)
        {
            State.Item_Cur = State.Tmp0023;
            Item.Holder = 100;
        }
        if (State.Tmp0024 != 0)
        {
            // QUIRK: CMD-17 — holder 100 (the stock) is unit record 100: Unit_DgnAtkMod[48] := 0.
            State.Unit_Cur = State.Tmp0024;
            Unit.Item = 0;
        }
        await PlaySoundAsync(110);                                      // \L_01141
        State.Unit_Cur = State.Ui_ItemUnitSel;
        Unit.Item = State.Ui_ItemSel;
        State.Item_Cur = State.Ui_ItemSel;
        Item.Holder = State.Ui_ItemUnitSel;
        await EngineAsync("CC", 640, 400, 640, 400, 0, 0);
        State.Ui_ItemUnitSel = 0;
        State.Ui_ItemSel = 0;
    }

    /// <summary>
    /// <c>L_00b39</c> (<c>:416-427</c>): takes an item off its holder into stock — the item
    /// <c>Tmp0021</c> (with its holder) or the item of unit <c>Tmp0022</c>, or with both 0 the
    /// item and unit the cursors are on.
    /// </summary>
    private void Unequip()
    {
        if (State.Tmp0021 != 0)
        {
            State.Item_Cur = State.Tmp0021;
            State.Unit_Cur = Item.Holder;
        }
        if (State.Tmp0022 != 0)
        {
            State.Unit_Cur = State.Tmp0022;
            State.Item_Cur = Unit.Item;
        }
        Item.Holder = 100;
        Unit.Item = 0;
    }

    /// <summary>
    /// <c>L_004ee</c> (<c>:170-375</c>): the grid of the view, then the pointer until a pick the
    /// view accepts (a unit at status 1 or 2 not holding Chaos (item 1) or Nikkou (item 2); an owned item that can move
    /// to the unit picked) or anything that is not a grid entry. A held item asked about is taken
    /// off after a Yes: 504 without a unit picked, the item picked otherwise.
    /// </summary>
    private async Task PickAsync()
    {
        await DrawListAsync();
        while (true)                                                    // L_006b1
        {
            await PointAsync();                                         // \L_00b7b
            if (State.CallArg == 0)
                continue;
            if (State.Ui_ItemViewMode == 0)
            {
                State.Unit_Cur = State.CallArg;
                if (State.CallArg < 500 && Unit.Status != 1 && Unit.Status != 2)
                    continue;
                if (State.CallArg < 500)
                {
                    if (Unit.Item == 1 || Unit.Item == 2)
                    {
                        await RefuseAsync(254, State.Unit_StrBase + (State.Unit_Cur - 1) * 3, State.Item_StrBase + Unit.Item - 1);
                        continue;                                       // "<unit>'s equipment, <item>, can't be unequipped."
                    }
                    // QUIRK: CMD-20 — dead: a pick of Chaos (item 1) or Nikkou (item 2) is refused when it is made.
                    if (State.Ui_ItemSel == 1 || State.Ui_ItemSel == 2)
                    {
                        await RefuseAsync(268, State.Unit_StrBase + (State.Unit_Cur - 1) * 3, State.Item_StrBase + State.Ui_ItemSel - 1);
                        continue;
                    }
                }
            }
            if (State.Ui_ItemViewMode == 1)
            {
                State.Item_Cur = State.CallArg;
                if (State.CallArg < 500 && Item.Holder == 0)
                    continue;
                if (State.CallArg < 500)
                {
                    State.Item_Cur = State.CallArg;
                    State.Unit_Cur = State.Ui_ItemUnitSel;
                    // QUIRK: CMD-17 — without a unit picked this reads Unit_Item[0].
                    if (Unit.Item == 1 || Unit.Item == 2)
                    {
                        await RefuseAsync(291, State.Item_StrBase + Unit.Item - 1);         // "<item> can't be unequipped."
                        continue;
                    }
                    if (State.Item_Cur == 1 || State.Item_Cur == 2)
                    {
                        await RefuseAsync(301, State.Item_StrBase + State.Item_Cur - 1);
                        continue;
                    }
                    if (Item.Holder == State.Ui_ItemUnitSel && State.Ui_ItemUnitSel != 0)
                    {
                        await RefuseAsync(311);                         // "Already equipped."
                        continue;
                    }
                    if (Item.Holder != 100)
                    {
                        if (State.Ui_ItemUnitSel == 0)
                        {
                            State.Unit_Cur = Item.Holder;
                            await AskAsync(322, State.Item_StrBase + Unit.Item - 1, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
                            if (State.Tmp0021 == 0)                     // "<item> is equipped to <unit>, but would you like to unequip it?"
                                continue;
                            State.CallArg = 504;
                            return;
                        }
                        if (State.Ui_ItemUnitSel != 0)
                        {
                            await AskAsync(341, State.Item_StrBase + State.Item_Cur - 1,
                                State.Unit_StrBase + (Item.Holder - 1) * 3, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
                            if (State.Tmp0021 == 0)                     // "… would you like to remove it and equip <unit> instead?"
                                continue;
                            State.Tmp0021 = State.Item_Cur;
                            State.Tmp0022 = Item.Holder;
                            Unequip();                                  // \L_00b39
                            State.Ui_ItemSel = State.CallArg;
                            return;
                        }
                    }
                    if (Item.Effect == 0)
                    {
                        await RefuseAsync(367);                         // "This item can't be equipped."
                        continue;
                    }
                }
            }
            return;
        }
    }

    /// <summary>A refusal in the narration window (op 113) with sound 90, closed after its key wait.</summary>
    private async Task RefuseAsync(int line, params Sys3Int[] strings)
    {
        await WaoAsync(113);                                            // \L_0119e
        await PlaySoundAsync(90);                                       // \L_010f7
        await StringsAsync(line, strings);
        await WaoAsync(1);                                              // \L_0128b
        await WaoAsync(0);                                              // \L_0127e
    }

    /// <summary>A question in the narration window (op 113) with sound 111 and Yes/No (<c>Tmp0021</c> 1 for Yes), then the window closed.</summary>
    private async Task AskAsync(int line, params Sys3Int[] strings)
    {
        await WaoAsync(113);                                            // \L_0119e
        await PlaySoundAsync(111);                                      // \L_0111c
        await StringsAsync(line, strings);
        await YesNoAsync();                                             // \L_00a3f
        await WaoAsync(0);                                              // \L_0127e
    }

    /// <summary>
    /// <c>L_00a3f</c> (<c>:376-415</c>): Yes/No in selection window 2 at the pointer (kept 32 px off
    /// the left and right edges, 28 off the top, 8 off the bottom); <c>Tmp0021</c> 1 for Yes, 0
    /// for No or a cancel.
    /// </summary>
    private async Task YesNoAsync()
    {
        var m = await MouseAsync();
        State.Ui_MouseX = m.X;
        State.Ui_MouseY = m.Y;
        if (State.Ui_MouseX < 24 + 8)
            State.Ui_MouseX = 24 + 8;
        if (State.Ui_MouseY < 20 + 8)
            State.Ui_MouseY = 20 + 8;
        if (State.Ui_MouseX > 640 - 24 - 8)
            State.Ui_MouseX = 640 - 24 - 8;
        if (State.Ui_MouseY > 400 - 8)
            State.Ui_MouseY = 400 - 8;
        await EngineAsync("B1", 2, State.Ui_MouseX - 24, State.Ui_MouseY - 20, 16 * 3, 20 * 2, 1);
        await EngineAsync("B2", 2, 1, 0, 0, 0, 0);
        await WaitForReleaseAsync();                                    // L_00ad5
        State.Tmp0021 = 0;
        var items = new List<SceneMenuItem> { MenuItem(399) };          // Yes
        await LineBreakAsync();
        items.Add(MenuItem(403));                                       // No
        if (await SelectAsync(items) == 1)
            State.Tmp0021 = 1;                                          // L_00b17
        await WaitForReleaseAsync();                                    // L_00b1b
    }

    /// <summary><c>:181-195</c>, <c>:211-226</c>: a unit's name (white at status 1 and 2), or an item's (white in stock, coloured 253 when held, 12 without effect).</summary>
    protected override async Task DrawEntryAsync()
    {
        if (State.Ui_ItemViewMode == 0)
        {
            await EngineAsync("ZC", 1, 0);
            await EngineAsync("T", State.Tmp0021, State.Tmp0022);
            await StringsAsync(185, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
            if (Unit.Status == 1)
                await EngineAsync("ZC", 1, 255);
            if (Unit.Status == 2)
                await EngineAsync("ZC", 1, 255);
            await EngineAsync("T", State.Tmp0021 - 1, State.Tmp0022);
            await StringsAsync(194, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
            return;
        }
        await EngineAsync("ZC", 1, 0);
        await EngineAsync("T", State.Tmp0021, State.Tmp0022);
        await StringsAsync(215, State.Item_StrBase + State.Item_Cur - 1);
        await EngineAsync("ZC", 1, 253);
        if (Item.Holder == 100)
            await EngineAsync("ZC", 1, 255);
        if (Item.Effect == 0)
            await EngineAsync("ZC", 1, 12);
        await EngineAsync("T", State.Tmp0021 - 1, State.Tmp0022);
        await StringsAsync(225, State.Item_StrBase + State.Item_Cur - 1);
    }

    /// <summary>
    /// <c>L_00df5</c> (<c>:507-532</c>): a grid cell; on the bar 997 for its first place (no label;
    /// the column x = 320 keeps the last code, CMD-22), 502 for "Item/Character", 503 for "End";
    /// left of the list 999.
    /// </summary>
    protected override async Task HitTestAsync()
    {
        await ReadPointerAsync();
        if (State.Tmp0021 > 319)
        {
            if (State.Tmp0022 < 324)
            {
                HitCell();
                return;
            }
            HitBar();
            // QUIRK: CMD-22 — x = 320 sets no code; CMD-17 — the first place answers 997.
            if (State.Tmp0021 > 320)
                State.Tmp0025 = 997;
            if (State.Tmp0021 > 426)
                State.Tmp0025 = 502;
            if (State.Tmp0021 > 532)
                State.Tmp0025 = 503;
            return;
        }
        State.Tmp0025 = 999;
    }

    /// <summary><c>:449-454</c>: the view's background.</summary>
    protected override async Task ClearDetailsAsync()
    {
        if (State.Ui_ItemViewMode == 0)
            await EngineAsync("G", 600);
        if (State.Ui_ItemViewMode == 1)
            await EngineAsync("G", 692);
    }

    /// <summary>
    /// <c>:469-470</c>, <c>L_00eac</c> (<c>:533-591</c>): a unit's portrait, war cost, troops, class
    /// and item; <c>:475-476</c>, <c>L_0101d</c> (<c>:592-617</c>): an item's picture, whether it can
    /// be equipped and who holds it.
    /// </summary>
    protected override async Task ShowDetailsAsync()
    {
        if (State.Ui_ItemViewMode == 0)
        {
            await EngineAsync("G", Unit.PortraitCg);
            await EngineAsync("B4", 9, 0, 0, 0, 1, 0);                 // L_00eac
            await EngineAsync("CC", 16 + 640, 208 + 400, 296, 37, 16, 208);
            await EngineAsync("ZC", 1, 255);
            State.Unit_Cur = State.Tmp0025;
            await EngineAsync("ZM", 12);
            await ValuesAsync(540, Unit.DeployCost);                    // "　　n00K"
            await EngineAsync("ZM", 8);
            await TextAsync(546);                                       // " GOLD"
            await EngineAsync("ZM", 12);
            await ValuesAsync(549, Unit.Troops);                        // ", n Troops"
            await EngineAsync("ZM", 12);
            await TextAsync(556);
            int[] classes = [0, 1, 2, 3, 4, 10, 11];
            int[] lines = [559, 563, 567, 571, 575, 579, 583];
            for (int i = 0; i < classes.Length; i++)
            {
                if (Unit.Class == (Sys3Int)classes[i])
                    await TextAsync(lines[i]);                          // "［Assault Troops］" …
            }
            if (Unit.Item != 0)
                await StringsAsync(587, State.Item_StrBase + Unit.Item - 1);    // " Equipping <item>"
            await EngineAsync("ZM", 12);
            return;
        }
        await EngineAsync("G", 750 + State.Tmp0025);
        await EngineAsync("B4", 9, 0, 0, 0, 1, 0);                     // L_0101d
        await EngineAsync("CC", 16 + 640, 208 + 400, 296, 37, 16, 208);
        State.Item_Cur = State.Tmp0025;
        await EngineAsync("ZC", 1, 255);
        await EngineAsync("ZM", 12);
        if (Item.Effect == 0)
            await TextAsync(600);                                       // "［Unequippable］"
        if (Item.Effect != 0)
        {
            await TextAsync(604);                                       // "［Equippable］ "
            if (Item.Holder != 100)
                await StringsAsync(607, State.Unit_StrBase + (Item.Holder - 1) * 3);    // "<unit> is equipped"
            if (Item.Holder == 100)
                await TextAsync(612);                                   // "Nobody's equipping this."
            await LineBreakAsync();
        }
    }
}
