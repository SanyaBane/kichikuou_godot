using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Sﾌﾞﾀｲ.ADV</c>, the deployment screen (<c>docs/strategy/survey-commands.md</c> §2.3, §7):
/// called with the most units in <c>Tmp0021</c> and the mode in <c>Tmp0022</c> (1 attack, 2 a
/// defence — plains or city, 3 a dungeon search). The player builds the party in
/// <c>Bt_PlayerParty1-4</c> by clicking units at status 1 (a click on a unit in the party moves it
/// to the end — CMD-16; a click left of the list or a right click takes the last one out) and
/// sends it with the first bar button (501; in a defence also the second, 502) or stops (503,
/// the party emptied). Sending it needs a party and the war expenses — Σ
/// <c>Unit_DeployCost + Unit_Troops / 300</c>, computed in <c>Tmp0016</c> — which are paid at once
/// with every member set to status 2 (BFL-01). The code is answered in <c>CallArg</c>.
/// </summary>
public sealed class DeploymentScreen(OriginalScriptContext script, IScenarioSource scenario) : PickerScreen(script, "Sﾌﾞﾀｲ.ADV", scenario)
{
    /// <summary>The bar labels of each mode: the <c>MS 1</c> lines of its three buttons (<c>:12-102</c>).</summary>
    private static readonly int[][] BarLabels = [[14, 24, 33], [45, 54, 63], [75, 84, 93]];

    /// <summary>The x of the three bar labels (each printed one pixel right first).</summary>
    private static readonly int[] BarX = [340, 445, 550];

    /// <inheritdoc />
    protected override Sys3Int Cursor
    {
        get => State.Unit_Cur;
        set => State.Unit_Cur = value;
    }

    /// <inheritdoc />
    protected override bool Listed => Unit.Status != 0;

    /// <summary><c>Sﾌﾞﾀｲ.ADV:1-158</c>: the screen, then the party editing until it is sent or stopped.</summary>
    public async Task RunAsync()
    {
        await Script.ShowAsync(new PickerScreenEvent(PickerKind.Deployment, State.Tmp0022, true));
        State.Bt_PlayerParty1 = 0;
        State.Bt_PlayerParty2 = 0;
        State.Bt_PlayerParty3 = 0;
        State.Bt_PlayerParty4 = 0;
        State.Tmp0040 = State.Tmp0021;
        await EngineAsync("B3", 8, 8, 274, 296, 120, 0);
        await EngineAsync("CC", 0, 0, 640, 400, 0, 400);
        await EngineAsync("G", 817, 40);
        await EngineAsync("G", 600);
        await EngineAsync("CC", 0, 0, 640, 400, 640, 400);
        await EngineAsync("ZM", 24);
        for (int mode = 1; mode <= 3; mode++)                           // :12-102 the bar: Attack / Plains, City / Search; Stop
        {
            if (State.Tmp0022 != (Sys3Int)mode)
                continue;
            for (int i = 0; i < 3; i++)
                await BarLabelAsync(BarLabels[mode - 1][i], (Sys3Int)BarX[i]);
        }
        await EngineAsync("ZC", 1, 255);
        await PartyAsync();                                             // \L_006bc
        State.Ui_SquadMode = State.Tmp0022;
        while (true)                                                    // L_002ea
        {
            await PickAsync();                                          // \L_00910
            if (State.CallArg == 0)
                continue;
            if (State.CallArg == 501 || State.CallArg == 502)
            {
                if (State.CallArg == 502 && State.Ui_SquadMode != 2)
                    continue;
                if (await GoAsync())
                {
                    await Script.ShowAsync(new PickerScreenEvent(PickerKind.Deployment, State.Ui_SquadMode, false));
                    return;
                }
                continue;
            }
            if (State.CallArg == 503)
            {
                await StopAsync();
                await Script.ShowAsync(new PickerScreenEvent(PickerKind.Deployment, State.Ui_SquadMode, false));
                return;
            }
            if (State.CallArg == 999)
            {
                // :123-145 the last unit out of the party.
                if (State.Bt_PlayerParty4 != 0)
                    State.Bt_PlayerParty4 = 0;
                else if (State.Bt_PlayerParty3 != 0)
                    State.Bt_PlayerParty3 = 0;
                else if (State.Bt_PlayerParty2 != 0)
                    State.Bt_PlayerParty2 = 0;
                else if (State.Bt_PlayerParty1 != 0)
                    State.Bt_PlayerParty1 = 0;
                else
                    continue;
                await PartyAsync();
                continue;
            }
            State.Tmp0001 = State.CallArg;
            await ToggleAsync();                                        // \L_00557
            await PartyAsync();
        }
    }

    /// <summary><c>L_003f7</c> (<c>:150-158</c>) — Stop: the party emptied.</summary>
    private async Task StopAsync()
    {
        await EngineAsync("ZM", 24);
        await EngineAsync("ZC", 1, 15);
        State.Bt_PlayerParty1 = 0;
        State.Bt_PlayerParty2 = 0;
        State.Bt_PlayerParty3 = 0;
        State.Bt_PlayerParty4 = 0;
        await EngineAsync("CC", 0, 400, 640, 400, 0, 0);
    }

    /// <summary>
    /// <c>L_00429</c> (<c>:159-210</c>) — Go: refused without a party or without the war
    /// expenses (false: back to the list); otherwise the party is set to status 2, the expenses
    /// are paid and booked as military spending in the history record <c>Hist_Cur</c> selects,
    /// and the button's code is answered (true).
    /// </summary>
    private async Task<bool> GoAsync()
    {
        State.Ui_SquadChoice = State.CallArg;
        await EngineAsync("ZM", 24);
        await EngineAsync("ZC", 1, 15);
        if (State.Bt_PlayerParty1 == 0)
        {
            await RefuseAsync(167);                                     // "You can't deploy without selecting units."
            return false;
        }
        if (State.Gold < State.Tmp0016)
        {
            await RefuseAsync(179);                                     // "Funds are unsufficient. ..."
            return false;
        }
        await PlaySoundAsync(108);                                      // \L_00e6a
        // QUIRK: BFL-01 — the expenses are paid and the units spent here; a later refusal refunds nothing.
        if (State.Bt_PlayerParty1 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerParty1;
            Unit.Status = 2;
        }
        if (State.Bt_PlayerParty2 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerParty2;
            Unit.Status = 2;
        }
        if (State.Bt_PlayerParty3 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerParty3;
            Unit.Status = 2;
        }
        if (State.Bt_PlayerParty4 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerParty4;
            Unit.Status = 2;
        }
        State.Gold = State.Gold - State.Tmp0016;
        Hist.MilitaryExp = Hist.MilitaryExp + State.Tmp0016;
        await EngineAsync("CC", 0, 400, 640, 400, 0, 0);
        State.CallArg = State.Ui_SquadChoice;
        return true;
    }

    /// <summary><c>:164-173</c>, <c>:176-187</c>: a refusal in the centre window, with sound 90.</summary>
    private async Task RefuseAsync(int line)
    {
        await WaoAsync(113);                                            // \L_00eec
        await PlaySoundAsync(90);                                       // \L_00e8f
        await TextAsync(line);
        await WaoAsync(1);                                              // \L_00fd9
        await WaoAsync(0);                                              // \L_00fcc
        await EngineAsync("ZC", 1, 255);
    }

    /// <summary>
    /// <c>L_00557</c> (<c>:211-268</c>): the clicked unit (<c>Tmp0001</c>) leaves the party if it is
    /// in it, the members after it moving up, and joins at the first free place within the limit
    /// <c>Tmp0040</c>; a full party is refused with the limit's message.
    /// </summary>
    private async Task ToggleAsync()
    {
        if (State.Bt_PlayerParty1 == State.Tmp0001)
        {
            State.Bt_PlayerParty1 = State.Bt_PlayerParty2;
            State.Bt_PlayerParty2 = State.Bt_PlayerParty3;
            State.Bt_PlayerParty3 = State.Bt_PlayerParty4;
            State.Bt_PlayerParty4 = 0;
        }
        if (State.Bt_PlayerParty2 == State.Tmp0001)
        {
            State.Bt_PlayerParty2 = State.Bt_PlayerParty3;
            State.Bt_PlayerParty3 = State.Bt_PlayerParty4;
            State.Bt_PlayerParty4 = 0;
        }
        if (State.Bt_PlayerParty3 == State.Tmp0001)
        {
            State.Bt_PlayerParty3 = State.Bt_PlayerParty4;
            State.Bt_PlayerParty4 = 0;
        }
        if (State.Bt_PlayerParty4 == State.Tmp0001)
            State.Bt_PlayerParty4 = 0;
        // QUIRK: CMD-16 — the unit just taken out joins again at the end: a click never deselects.
        if (State.Bt_PlayerParty1 == 0 && State.Tmp0040 > 0)
        {
            State.Bt_PlayerParty1 = State.Tmp0001;
            await PlaySoundAsync(108);
            return;
        }
        if (State.Bt_PlayerParty2 == 0 && State.Tmp0040 > 1)
        {
            State.Bt_PlayerParty2 = State.Tmp0001;
            await PlaySoundAsync(108);
            return;
        }
        if (State.Bt_PlayerParty3 == 0 && State.Tmp0040 > 2)
        {
            State.Bt_PlayerParty3 = State.Tmp0001;
            await PlaySoundAsync(108);
            return;
        }
        if (State.Bt_PlayerParty4 == 0 && State.Tmp0040 > 3)
        {
            State.Bt_PlayerParty4 = State.Tmp0001;
            await PlaySoundAsync(108);
            return;
        }
        await EngineAsync("ZM", 24);
        await EngineAsync("ZC", 1, 15);
        await WaoAsync(113);
        await PlaySoundAsync(90);
        await ValuesAsync(255, State.Tmp0040);                          // "Up to n unit"
        if (State.Tmp0040 != 1)
            await TextAsync(261);                                       // "s"
        await TextAsync(264);                                           // " can deploy."
        await WaoAsync(1);
        await WaoAsync(0);
        await EngineAsync("ZC", 1, 255);
    }

    /// <summary>
    /// <c>L_006bc</c> (<c>:269-343</c>): the party panel — a line per place up to the limit (the
    /// member's name, war expense and troops, or dashes), then the troop total (<c>Tmp0015</c>)
    /// and the war expenses (<c>Tmp0016</c>), both summed here.
    /// </summary>
    private async Task PartyAsync()
    {
        State.Tmp0016 = 0;
        State.Tmp0015 = 0;
        await EngineAsync("B4", 8, 0, 0, 0, 1, 0);
        await EngineAsync("CC", 14 + 640, 274 + 400, 296, 120, 14, 274);
        await EngineAsync("ZM", 12);
        for (int place = 1; place <= 4; place++)
        {
            State.Tmp0575 = 0;
            Sys3Int member = Member(place);
            if (member != 0)
            {
                State.Unit_Cur = member;
                await MemberLineAsync();                                // \L_00849
                State.Tmp0575 = 1;
            }
            await EmptyLineAsync();                                     // \L_008d5
            if (State.Tmp0040 == (Sys3Int)place)
            {
                for (int i = place; i < 4; i++)
                    await LineBreakAsync();                             // the panel's height kept
                break;
            }
        }
        await LineBreakAsync();                                         // L_007f0
        await EngineAsync("ZM", 16);
        await ValuesAsync(329, State.Tmp0015);                          // "Troop Total: n"
        await EngineAsync("ZM", 16);
        await ValuesAsync(335, State.Tmp0016);                          // "War Expenses: n00K"
        await EngineAsync("ZM", 8);
        await TextAsync(341);                                           // " GOLD"
        await EngineAsync("ZM", 12);
    }

    private Sys3Int Member(int place) => place switch
    {
        1 => State.Bt_PlayerParty1,
        2 => State.Bt_PlayerParty2,
        3 => State.Bt_PlayerParty3,
        _ => State.Bt_PlayerParty4,
    };

    /// <summary><c>L_00849</c> (<c>:344-366</c>): the member's line; its troops and expense added to the totals.</summary>
    private async Task MemberLineAsync()
    {
        CopyString(1, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
        State.Tmp0015 = State.Tmp0015 + Unit.Troops;
        State.Tmp0016 = State.Tmp0016 + Unit.DeployCost + Unit.Troops / 300;
        await ValuesAsync(349, Unit.DeployCost + Unit.Troops / 300);    // " <name>（n00K"
        await EngineAsync("ZM", 8);
        await TextAsync(358);                                           // " GOLD"
        await EngineAsync("ZM", 12);
        await ValuesAsync(361, Unit.Troops);                            // ", n）"
    }

    /// <summary><c>L_008d5</c> (<c>:367-379</c>): dashes for a free place (<c>Tmp0575</c> 0).</summary>
    private async Task EmptyLineAsync()
    {
        if (State.Tmp0575 != 0)
            return;
        await TextAsync(370);
        await EngineAsync("ZM", 8);
        await TextAsync(373);
        await EngineAsync("ZM", 12);
        await TextAsync(376);
    }

    /// <summary>
    /// <c>L_00910</c> (<c>:380-432</c>): the list, then the pointer until it picks a unit at
    /// status 1 (another listed unit is refused with sound 90) or anything that is not a unit.
    /// </summary>
    private async Task PickAsync()
    {
        await DrawListAsync();
        while (true)                                                    // L_00a18
        {
            await PointAsync();                                         // \L_00a75
            if (State.CallArg == 0)
                continue;
            // QUIRK: CMD-24 — the empty 51st cell answers unit record 51, taken at status 1.
            State.Unit_Cur = State.CallArg;
            if (State.CallArg < 500 && Unit.Status == 0)
                continue;
            if (State.CallArg < 500 && Unit.Status != 1)
            {
                await PlaySoundAsync(90);                               // \L_00e8f
                continue;
            }
            return;
        }
    }

    /// <summary><c>:390-407</c>: the unit's name, white at status 1, coloured 15 at status 2, 13 in the party.</summary>
    protected override async Task DrawEntryAsync()
    {
        await EngineAsync("ZC", 1, 0);
        await EngineAsync("T", State.Tmp0021, State.Tmp0022);
        await StringsAsync(394, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
        if (Unit.Status == 1)
            await EngineAsync("ZC", 1, 255);
        if (Unit.Status == 2)
            await EngineAsync("ZC", 1, 15);
        if (State.Unit_Cur == State.Bt_PlayerParty1 || State.Unit_Cur == State.Bt_PlayerParty2
            || State.Unit_Cur == State.Bt_PlayerParty3 || State.Unit_Cur == State.Bt_PlayerParty4)
            await EngineAsync("ZC", 1, 13);
        await EngineAsync("T", State.Tmp0021 - 1, State.Tmp0022);
        await StringsAsync(406, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
    }

    /// <summary>
    /// <c>L_00c80</c> (<c>:498-523</c>): a grid cell; on the bar the buttons 501 (x 321-426), 502
    /// (427-532) and 503 (533 on) — the column x = 320 keeps the code of the last hit test;
    /// left of the list 999, so a left click there takes the last unit out like a right click.
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
            // QUIRK: CMD-22 — x = 320 sets no code: the last hit test's stays.
            if (State.Tmp0021 > 320)
                State.Tmp0025 = 501;
            if (State.Tmp0021 > 426)
                State.Tmp0025 = 502;
            if (State.Tmp0021 > 532)
                State.Tmp0025 = 503;
            return;
        }
        // QUIRK: CMD-23 — left of the list a left click is 999 too: the last unit out.
        State.Tmp0025 = 999;
    }

    /// <inheritdoc />
    protected override Task ClearDetailsAsync() => EngineAsync("G", 600);

    /// <summary>
    /// <c>:467-468</c>, <c>L_00d37</c> (<c>:524-569</c>): the portrait, the unit's war expense,
    /// troops, rows and item.
    /// </summary>
    protected override async Task ShowDetailsAsync()
    {
        await EngineAsync("G", Unit.PortraitCg);
        await EngineAsync("B4", 9, 0, 0, 0, 1, 0);
        await EngineAsync("CC", 16 + 640, 208 + 400, 296, 37, 16, 208);
        State.Unit_Cur = State.Tmp0025;
        await EngineAsync("ZM", 12);
        await ValuesAsync(530, Unit.DeployCost + Unit.Troops / 300);    // " n00K"
        await EngineAsync("ZM", 8);
        await TextAsync(536);                                           // " GOLD"
        await EngineAsync("ZM", 12);
        await ValuesAsync(539, Unit.Troops, Unit.MaxTroops);            // ", n/m"
        await EngineAsync("ZM", 12);
        await TextAsync(548);
        if (Unit.Class == 0 || Unit.Class == 4 || Unit.Class == 10)
            await TextAsync(551);                                       // "［Front/－－］"
        if (Unit.Class == 1 || Unit.Class == 2 || Unit.Class == 11)
            await TextAsync(555);                                       // "［－－/Back］"
        if (Unit.Class == 3)
            await TextAsync(559);                                       // "［Front/Back］"
        if (Unit.Item != 0)
            await StringsAsync(563, State.Item_StrBase + Unit.Item - 1); // "<item> equipped"
        await LineBreakAsync();
        await EngineAsync("ZM", 12);
    }
}
