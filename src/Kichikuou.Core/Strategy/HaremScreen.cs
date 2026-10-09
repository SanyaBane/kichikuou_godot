using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Sﾊｰﾚﾑ.ADV</c>, the harem picker of <c>Cﾊｰﾚﾑ</c> (<c>docs/strategy/survey-commands.md</c> §3.8,
/// §7): lists the girls <c>Cﾊｰﾚﾑ</c> made available (<c>HaremAvail_*</c>, copied into
/// <c>Buf_Val[1-50]</c>; their names from the file's string table, in the player's files) and
/// answers the one picked in <c>CallArg</c>, or 503 for "End", a right click or a click on the
/// bar. Hovering a girl shows her picture, her count and the date she was last summoned (which
/// writes <c>DateYear/Month/Week</c> and leaves <c>Hist_Cur</c> at her index until the exit
/// resets it to 0).
/// </summary>
public sealed class HaremScreen(ScriptContext script, IScenarioSource scenario) : PickerScreen(script, "Sﾊｰﾚﾑ.ADV", scenario)
{
    private IReadOnlyList<string>? _names;

    /// <inheritdoc />
    protected override Sys3Int Cursor
    {
        get => State.Buf_Cur;
        set => State.Buf_Cur = value;
    }

    /// <inheritdoc />
    protected override bool Listed => State.Buffer[State.Buf_Cur].Val != 0;

    /// <summary><c>Sﾊｰﾚﾑ.ADV:1-45</c>: the screen, the list, the pick.</summary>
    public async Task RunAsync()
    {
        await Script.ShowAsync(new PickerScreenEvent(PickerKind.Harem, Sys3Int.Zero, true));
        await EngineAsync("CC", 0, 0, 640, 400, 0, 400);
        if (State.StoryStage == 0)
            await EngineAsync("G", 694, 40);
        if (State.StoryStage != 0)
            await EngineAsync("G", 818, 40);
        await EngineAsync("ZM", 24);
        await BarLabelAsync(10, 550 + 12);                              // MS 1, "End"
        await EngineAsync("CC", 0, 0, 640, 400, 640, 400);
        await EngineAsync("ZC", 1, 255);
        State.Buf_Cur = 1;
        for (int i = 0; i < 100; i++)                                   // NC Buf_Val, 100
            State.Buffer[State.Buf_Cur.Value + i].Val = Sys3Int.Zero;
        for (int i = 0; i < 50; i++)                                    // NB Buf_Val, HaremAvail_Sill, 50
            State.Buffer[State.Buf_Cur.Value + i].Val = State.Memory.Var(249 + i);
        do                                                              // L_000d4
            await PickAsync();
        while (State.CallArg == 0 || State.CallArg == 501 || State.CallArg == 502 || State.CallArg == 998);
        if (State.CallArg == 999)
            State.CallArg = 503;
        State.Hist_Cur = 0;
        await EngineAsync("ZM", 24);
        await EngineAsync("ZC", 1, 15);
        await EngineAsync("CC", 0, 400, 640, 400, 0, 0);
        await Script.ShowAsync(new PickerScreenEvent(PickerKind.Harem, Sys3Int.Zero, false));
    }

    /// <summary>
    /// <c>L_00165</c> (<c>:46-91</c>): the list, then the pointer until it picks a listed girl or
    /// anything that is not a girl.
    /// </summary>
    private async Task PickAsync()
    {
        await DrawListAsync();
        while (true)                                                    // L_00222
        {
            await PointAsync();                                         // \L_00275
            if (State.CallArg == 0)
                continue;
            State.Buf_Cur = State.CallArg;
            if (State.CallArg < 500 && State.Buffer[State.Buf_Cur].Val == 0)
                continue;
            if (State.CallArg < 500)
                await PlaySoundAsync(108);                              // \L_00823
            return;
        }
    }

    /// <summary><c>:56-67</c>: the girl's name (<c>#L_00848, 1: F1, 1, Buf_Cur - 1</c>) into string 1, in white.</summary>
    protected override async Task DrawEntryAsync()
    {
        _names ??= (Literal(57) as SetStringTableStatement
            ?? throw new ScenarioFormatException($"{Ref(57)}: expected #label, 1: naming a string table.")).Strings;
        Script.SetStringTable(_names);
        Script.ReadDataString(Ref(58), 1, State.Buf_Cur - 1);
        await EngineAsync("ZC", 1, 0);
        await EngineAsync("T", State.Tmp0021, State.Tmp0022);
        await TextAsync(62);                                            // X 1
        await EngineAsync("ZC", 1, 255);
        await EngineAsync("T", State.Tmp0021 - 1, State.Tmp0022);
        await TextAsync(66);                                            // X 1
    }

    /// <summary>
    /// <c>L_00514</c> (<c>:178-207</c>): a grid cell; on the bar 503 for "End" (x &gt; 532) and
    /// 999 elsewhere (the column x = 320 keeps the last code); left of the list a left click is
    /// 998 (nothing), a right click 999, and the pointer at rest keeps the last code.
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
            // QUIRK: CMD-22 — x = 320 sets no code; CMD-23 — the rest of the bar but "End" ends the picker.
            if (State.Tmp0021 > 320)
                State.Tmp0025 = 999;
            if (State.Tmp0021 > 426)
                State.Tmp0025 = 999;
            if (State.Tmp0021 > 532)
                State.Tmp0025 = 503;
            return;
        }
        // QUIRK: CMD-23 — left of the list a left click is 998: nothing.
        if (State.RetVal == 16)
            State.Tmp0025 = 998;
        if (State.RetVal == 32)
            State.Tmp0025 = 999;
    }

    /// <summary><c>:112-117</c>: the picture panel's background of the stage.</summary>
    protected override async Task ClearDetailsAsync()
    {
        if (State.StoryStage == 0)
            await EngineAsync("G", 696);
        if (State.StoryStage != 0)
            await EngineAsync("G", 700);
    }

    /// <summary>
    /// <c>:131-148</c>, <c>L_005e7</c> (<c>:208-285</c>): the girl's picture (in the prologue only
    /// Sill, Soul and Anasel have one; grown-up Mill another), her count and the date she was
    /// last summoned, or dashes as wide as this week's date.
    /// </summary>
    protected override async Task ShowDetailsAsync()
    {
        if (State.StoryStage == 0)
        {
            if (State.Tmp0025 == 1)
                await EngineAsync("G", 699);
            if (State.Tmp0025 == 11)
                await EngineAsync("G", 698);
            if (State.Tmp0025 == 12)
                await EngineAsync("G", 697);
        }
        if (State.StoryStage != 0)
            await EngineAsync("G", State.Tmp0025 + 700);
        if (State.F_Mill_State > 1 && State.Tmp0025 == 18)
            await EngineAsync("G", 695);

        State.Tmp0005 = State.Week;                                     // L_005e7
        SetDate();                                                      // \L_007c3
        State.Tmp0006 = 1;                                              // the digits of this week's year and month
        if (State.Tmp0001 > 9)
            State.Tmp0006 = 2;
        if (State.Tmp0001 > 99)
            State.Tmp0006 = 3;
        if (State.Tmp0001 > 999)
            State.Tmp0006 = 4;
        if (State.Tmp0001 > 9999)
            State.Tmp0006 = 5;
        State.Tmp0007 = 1;
        if (State.Tmp0002 > 9)
            State.Tmp0007 = 2;
        State.Hist_Cur = State.Tmp0025;
        State.Tmp0005 = Hist.Harem_LastWeek;
        SetDate();
        await EngineAsync("B4", 9, 0, 0, 0, 1, 0);
        await EngineAsync("CC", 16 + 640, 208 + 400, 296, 37, 16, 208);
        await EngineAsync("ZM", 12);
        await ValuesAsync(235, Hist.Harem_SexCount);                    // "Sex Count: n"
        if (Hist.Harem_LastWeek != 0)
            await ValuesAsync(241, State.Tmp0001, State.Tmp0002, State.Tmp0003);   // "Last Summoned: LP y, Month m, Week w"
        if (Hist.Harem_LastWeek == 0)
        {
            await TextAsync(253);                                       // "Last Summoned: LP"
            int[] years = [256, 260, 264, 268, 272];
            for (int digits = 1; digits <= 5; digits++)
            {
                if (State.Tmp0006 == (Sys3Int)digits)
                    await TextAsync(years[digits - 1]);                 // "－…, Month"
            }
            if (State.Tmp0007 == 1)
                await TextAsync(276);                                   // "－, Week"
            if (State.Tmp0007 == 2)
                await TextAsync(280);                                   // "－－, Week"
            await TextAsync(283);                                       // "－"
        }
    }

    /// <summary>
    /// <c>L_007c3</c> (<c>:286-301</c>): the date of week <c>Tmp0005</c> (48 weeks a year from year 3,
    /// 4 weeks a month) into <c>Tmp0001-0003</c> and <c>DateYear/Month/Week</c>.
    /// </summary>
    private void SetDate()
    {
        State.Tmp0004 = (State.Tmp0005 - 1) / 48;
        State.Tmp0001 = (Sys3Int)3 + State.Tmp0004;
        if (State.Tmp0004 != 0)
            State.Tmp0005 = State.Tmp0005 - State.Tmp0004 * 48;
        State.Tmp0004 = (State.Tmp0005 - 1) / 4;
        State.Tmp0002 = State.Tmp0004 + 1;
        if (State.Tmp0004 != 0)
            State.Tmp0005 = State.Tmp0005 - State.Tmp0004 * 4;
        State.Tmp0003 = State.Tmp0005;
        State.DateYear = State.Tmp0001;
        State.DateMonth = State.Tmp0002;
        State.DateWeek = State.Tmp0003;
    }
}
