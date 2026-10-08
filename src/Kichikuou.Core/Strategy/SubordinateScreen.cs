using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Sﾌﾞｶ.ADV</c>, the subordinate picker of <c>Cﾌﾞｶ</c> (<c>docs/strategy/survey-commands.md</c>
/// §3.5, §7). <c>CallArg</c> gives the mode: 2 opens the screen (grid background, the "End"
/// button), 3 closes it, 4 and 5 restore parts of the screen, and 1 (or any other mode, which
/// also opens the screen first) lists the units with a status and lets the player pick one. The
/// answer is in <c>CallArg</c> and <c>Unit_Cur</c>: a unit, 503 for "End" or a right click, 997
/// for the bar's column 320; a left click left of the list is 999, which also becomes 503.
/// With <c>RetVal</c> 0 at the call only units at status 1 can be picked; the callers pass 1, so
/// any listed unit can.
/// </summary>
public sealed class SubordinateScreen(ScriptContext script, IScenarioSource scenario) : PickerScreen(script, "Sﾌﾞｶ.ADV", scenario)
{
    /// <inheritdoc />
    protected override Sys3Int Cursor
    {
        get => State.Unit_Cur;
        set => State.Unit_Cur = value;
    }

    /// <inheritdoc />
    protected override bool Listed => Unit.Status != 0;

    /// <summary><c>Sﾌﾞｶ.ADV:1-56</c>: the picker in the mode <c>CallArg</c> gives.</summary>
    public async Task RunAsync()
    {
        State.Ui_SubordMode = State.CallArg;
        if (State.Ui_SubordMode == 3)
        {
            await EngineAsync("CC", 0, 400, 640, 400, 0, 0);
            return;
        }
        if (State.Ui_SubordMode == 4)
        {
            await EngineAsync("CC", 640, 400, 640, 400, 0, 0);
            return;
        }
        if (State.Ui_SubordMode == 5)
        {
            await EngineAsync("CC", 640 + 0, 400 + 200, 320, 50, 0, 200);
            return;
        }
        State.Tmp0040 = State.RetVal;
        if (State.Ui_SubordMode != 1)                                   // :15-34 the screen
        {
            await EngineAsync("CC", 0, 0, 640, 400, 0, 400);
            await EngineAsync("G", 870, 40);
            await EngineAsync("G", 600);
            await EngineAsync("ZM", 24);
            await BarLabelAsync(21, 550 + 12);                          // MS 1, "End"
            await EngineAsync("CC", 0, 0, 640, 400, 640, 400);
            if (State.Ui_SubordMode == 2)
                return;
        }
        await EngineAsync("ZC", 1, 255);
        do                                                              // L_00138
            await PickAsync();
        while (State.CallArg == 0 || State.CallArg == 501 || State.CallArg == 502);
        if (State.CallArg == 999)
            State.CallArg = 503;
        await EngineAsync("ZM", 24);
        await EngineAsync("ZC", 1, 15);
        if (State.Ui_SubordMode == 0)
            await EngineAsync("CC", 0, 400, 640, 400, 0, 0);
        State.Unit_Cur = State.CallArg;
    }

    /// <summary>
    /// <c>L_001c4</c> (<c>:57-109</c>): the list, then the pointer until it picks a listed unit
    /// (with <c>Tmp0040</c> 0, one at status 1; another one is refused with sound 90) or
    /// anything that is not a unit.
    /// </summary>
    private async Task PickAsync()
    {
        await DrawListAsync();
        while (true)                                                    // L_002a4
        {
            await PointAsync();                                         // \L_0031a
            if (State.CallArg == 0)
                continue;
            // QUIRK: CMD-24 — the empty 51st cell answers unit record 51.
            State.Unit_Cur = State.CallArg;
            if (State.CallArg < 500 && Unit.Status == 0)
                continue;
            if (State.CallArg < 500 && Unit.Status != 1 && State.Tmp0040 == 0)
            {
                await PlaySoundAsync(90);                               // \L_006ac
                continue;
            }
            if (State.CallArg < 500)
                await PlaySoundAsync(108);                              // \L_00687
            return;
        }
    }

    /// <summary><c>:67-81</c>: the unit's name, white at status 1, coloured 15 at status 2 (3 and more keep the black).</summary>
    protected override async Task DrawEntryAsync()
    {
        await EngineAsync("ZC", 1, 0);
        await EngineAsync("T", State.Tmp0021, State.Tmp0022);
        await StringsAsync(71, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
        if (Unit.Status == 1)
            await EngineAsync("ZC", 1, 255);
        if (Unit.Status == 2)
            await EngineAsync("ZC", 1, 15);
        await EngineAsync("T", State.Tmp0021 - 1, State.Tmp0022);
        await StringsAsync(80, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
    }

    /// <summary>
    /// <c>L_00525</c> (<c>:175-201</c>): a grid cell; on the bar "End" (x &gt; 532, 503), the column
    /// x = 320 (997), and between them nothing — the press is dropped (<c>RetVal</c> := 0); left of
    /// the list 999, so a left click there ends the picker like a right click.
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
            // QUIRK: CMD-22 — x = 320 answers 997; CMD-23 — the rest of the bar but "End" drops the press.
            State.Tmp0025 = 997;
            if (State.Tmp0021 > 532)
            {
                State.Tmp0025 = 503;
                return;
            }
            if (State.Tmp0021 > 320)
                State.RetVal = 0;
            if (State.Tmp0021 > 426)
                State.RetVal = 0;
            return;
        }
        // QUIRK: CMD-23 — left of the list a left click is 999 too: the picker ends.
        State.Tmp0025 = 999;
    }

    /// <inheritdoc />
    protected override Task ClearDetailsAsync() => EngineAsync("G", 600);

    /// <summary><c>:144-145</c>, <c>L_005df</c> (<c>:202-224</c>): the portrait, the unit's name and troop type, HP and troops.</summary>
    protected override async Task ShowDetailsAsync()
    {
        await EngineAsync("G", Unit.PortraitCg);
        await EngineAsync("B4", 9, 0, 0, 0, 1, 0);
        await EngineAsync("CC", 16 + 640, 208 + 400, 296, 37, 16, 208);
        await EngineAsync("ZM", 12);
        State.Unit_Cur = State.Tmp0025;
        await StringsAsync(208,
            State.Unit_StrBase + (State.Unit_Cur - 1) * 3,
            State.Unit_StrBase + (State.Unit_Cur - 1) * 3 + 1);
        await Script.ShowAsync(new MessageResetEvent(1));               // Y 1, 1
        await ValuesAsync(213, Unit.HP, Unit.MaxHP, Unit.Troops, Unit.MaxTroops);
    }
}
