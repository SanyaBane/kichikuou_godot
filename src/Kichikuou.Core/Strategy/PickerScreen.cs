using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// The list widget the original's picker screens share (<c>Sﾌﾞﾀｲ</c>, <c>Sﾌﾞｶ</c>, <c>Sﾊｰﾚﾑ</c>;
/// <c>docs/strategy/survey-commands.md</c> §7): a 3×17 grid of names on the right half of the
/// screen (ids 1-51, cells 108×18 from (328, 18), <see cref="PointerScript.PickerCell"/>), a bar of
/// buttons below it, a highlight under the pointer and a details panel for the entry it is on,
/// all read with <c>IM</c> polled as the script polls it. Each file has its own copy of the code;
/// the copies differ in what a cell shows, which entries are listed, the details panel and the
/// codes of the hit test (the subclasses), and are the same code otherwise (here). Every event is
/// the one <see cref="SceneRunner"/> raises for the same statement.
/// </summary>
public abstract class PickerScreen(ScriptContext script, string file, IScenarioSource scenario)
    : CommandFile(script, file, scenario)
{
    /// <summary>The cursor the grid walks and the pointer sets (<c>Unit_Cur</c>, <c>Buf_Cur</c>).</summary>
    protected abstract Sys3Int Cursor { get; set; }

    /// <summary>The entry under <see cref="Cursor"/> is on the list: drawn, and highlighted under the pointer.</summary>
    protected abstract bool Listed { get; }

    /// <summary>The cell of the entry under <see cref="Cursor"/>, at (<c>Tmp0021</c>, <c>Tmp0022</c>).</summary>
    protected abstract Task DrawEntryAsync();

    /// <summary>
    /// The file's hit test: <c>IM Tmp0021, Tmp0022</c>, then the code under the pointer into
    /// <c>Tmp0025</c> (a grid id, a bar code, or the file's codes for the rest of the screen) and
    /// the corner of what is under it into <c>Tmp0023</c>/<c>Tmp0024</c>.
    /// </summary>
    protected abstract Task HitTestAsync();

    /// <summary>The details panel's background, drawn back when the highlight leaves an entry (after its <c>CC</c>).</summary>
    protected abstract Task ClearDetailsAsync();

    /// <summary>The picture and the details of the entry under the pointer (after its highlight's <c>CF</c>).</summary>
    protected abstract Task ShowDetailsAsync();

    /// <summary>
    /// The grid (<c>Sﾌﾞﾀｲ.ADV:381-418</c>, <c>Sﾌﾞｶ.ADV:58-92</c>, <c>Sﾊｰﾚﾑ.ADV:47-78</c>): every
    /// listed entry 1-50 in its cell, row by row; leaves <see cref="Cursor"/> and <c>Tmp0032</c> at
    /// 51, the end of the hoverable ids.
    /// </summary>
    protected async Task DrawListAsync()
    {
        await EngineAsync("ZM", 12);
        State.Tmp0030 = 108;
        State.Tmp0031 = 18;
        Cursor = 1;
        State.Tmp0034 = 328;
        State.Tmp0035 = State.Tmp0031 + 0;
        State.Tmp0021 = State.Tmp0034;
        State.Tmp0022 = State.Tmp0035;
        do
        {
            if (Listed)
                await DrawEntryAsync();
            State.Tmp0021 = State.Tmp0021 + State.Tmp0030;
            if (State.Tmp0021 > 600)
            {
                State.Tmp0021 = State.Tmp0034;
                State.Tmp0022 = State.Tmp0022 + State.Tmp0031;
            }
            Cursor = Cursor + 1;
        }
        while (Cursor < 51);
        await EngineAsync("ZC", 1, 255);
        State.Tmp0032 = Cursor;
    }

    /// <summary>
    /// The pointer loop (<c>Sﾌﾞﾀｲ.ADV:433-497</c>, <c>Sﾌﾞｶ.ADV:110-174</c>, <c>Sﾊｰﾚﾑ.ADV:92-177</c>):
    /// waits for the buttons to be released, then follows the pointer — the highlight and the
    /// details move with the entry or bar button under it — until a button is pressed; a right
    /// click is 999. The code under the pointer at the press goes to <c>CallArg</c>; then it waits
    /// for the release.
    /// </summary>
    protected async Task PointAsync()
    {
        State.Tmp0023 = 999;
        State.Tmp0025 = 999;
        State.Tmp0026 = 0;
        State.Tmp0027 = State.Tmp0023;
        State.Tmp0028 = State.Tmp0024;
        State.Tmp0029 = 0;
        State.Tmp0033 = 0;
        await EngineAsync("B3", 9, 16, 208, 296, 37, 1);
        await ReleaseAsync();
        do
        {
            await HitTestAsync();
            Cursor = State.Tmp0025;
            if (State.Tmp0025 != State.Tmp0033 && State.Tmp0033 != 998 || State.Tmp0025 != State.Tmp0029 && State.Tmp0029 != 0)
            {
                // The highlight leaves what it was on.
                if (State.Tmp0026 != 0)
                {
                    await EngineAsync("CC", 16 + 640, 208 + 400, 296, 37, 16, 208);
                    await ClearDetailsAsync();
                    State.Tmp0026 = 0;
                }
                if (State.Tmp0033 != 998)
                {
                    await EngineAsync("CC", State.Tmp0027 + 640, State.Tmp0028 + State.Tmp0031 + 14 + 2 + 400, 90, 2,
                        State.Tmp0027, State.Tmp0028 + State.Tmp0031 + 14 + 2);
                    State.Tmp0033 = 998;
                }
                if (State.Tmp0029 != 0)
                {
                    await EngineAsync("CC", State.Tmp0027 + 640 + 10, State.Tmp0028 + 400, 70, 2, State.Tmp0027 + 10, State.Tmp0028);
                    State.Tmp0029 = 0;
                }
            }
            if (State.Tmp0025 != State.Tmp0033 && State.Tmp0025 < State.Tmp0032 && Listed)
            {
                // A listed entry: underline it, show its details.
                await EngineAsync("CF", State.Tmp0023, State.Tmp0024 + State.Tmp0031 + 14 + 2, 90, 2, 240);
                await ShowDetailsAsync();
                State.Tmp0026 = 1;
                State.Tmp0033 = State.Tmp0025;
                State.Tmp0027 = State.Tmp0023;
                State.Tmp0028 = State.Tmp0024;
            }
            if (State.Tmp0025 > 500 && State.Tmp0025 < 600 && State.Tmp0025 != State.Tmp0029)
            {
                // A bar button: underline it.
                State.Tmp0029 = State.Tmp0025;
                await EngineAsync("CF", State.Tmp0023 + 10, State.Tmp0024, 70, 2, 240);
                State.Tmp0027 = State.Tmp0023;
                State.Tmp0028 = State.Tmp0024;
            }
        }
        while (State.RetVal == 0);
        if (State.RetVal == 32)
            State.Tmp0025 = 999;
        State.CallArg = State.Tmp0025;
        if (State.Tmp0029 > 500 && State.Tmp0029 < 600)
            await EngineAsync("CC", State.Tmp0027 + 640 + 10, State.Tmp0028 + 400, 70, 2, State.Tmp0027 + 10, State.Tmp0028);
        else
            await EngineAsync("CC", State.Tmp0027 + 640, State.Tmp0028 + State.Tmp0031 + 14 + 2 + 400, 90, 2,
                State.Tmp0027, State.Tmp0028 + State.Tmp0031 + 14 + 2);
        await ReleaseAsync();
    }

    /// <summary><c>IM Tmp0021, Tmp0022:</c> — the pointer, the button into <c>RetVal</c>.</summary>
    protected async Task ReadPointerAsync()
    {
        var m = await MouseAsync();
        State.Tmp0021 = m.X;
        State.Tmp0022 = m.Y;
    }

    /// <summary><c>IM Tmp0021, Tmp0022:</c> until no button is down.</summary>
    private async Task ReleaseAsync()
    {
        do
            await ReadPointerAsync();
        while (State.RetVal != 0);
    }

    /// <summary>The grid's part of every hit test: the cell's corner into <c>Tmp0023</c>/<c>Tmp0024</c>, its id (1-51) into <c>Tmp0025</c>.</summary>
    protected void HitCell()
    {
        State.Tmp0023 = (State.Tmp0021 - 328) / State.Tmp0030 * State.Tmp0030 + 328;
        State.Tmp0024 = (State.Tmp0022 - State.Tmp0031) / State.Tmp0031 * State.Tmp0031;
        State.Tmp0025 = State.Tmp0024 / State.Tmp0031 * 3 + (State.Tmp0023 - 320) / State.Tmp0030 + 1;
    }

    /// <summary>The bar's part of every hit test: the button's corner into <c>Tmp0023</c>/<c>Tmp0024</c>.</summary>
    protected void HitBar()
    {
        State.Tmp0023 = (State.Tmp0021 - 320) / 106 * 106 + 328;
        State.Tmp0024 = 360;
    }

    /// <summary>
    /// A bar button's label (<c>MS 1, "text"</c> at <paramref name="line"/>, from the player's
    /// files): string 1 printed at (x + 1, 333) in black, then at (x, 333) in white.
    /// </summary>
    protected async Task BarLabelAsync(int line, Sys3Int x)
    {
        SetLiteral(line);
        await EngineAsync("T", x + 1, 333);
        await EngineAsync("ZC", 1, 0);
        await TextAsync(line + 4);                                      // X 1
        await EngineAsync("T", x, 333);
        await EngineAsync("ZC", 1, 255);
        await TextAsync(line + 8);                                      // X 1
    }

    /// <summary>
    /// The file's sound stub (<c>PCMPLAY</c> mode 0 with sound <paramref name="sound"/>: 108 a
    /// pick, 90 a refusal), keeping <c>RetVal</c> and <c>CallArg</c> through <c>Tmp0001</c>/<c>Tmp0002</c>.
    /// </summary>
    protected async Task PlaySoundAsync(Sys3Int sound)
    {
        State.Tmp0001 = State.RetVal;
        State.Tmp0002 = State.CallArg;
        State.RetVal = sound;
        await Script.SoundAsync(0);
        State.RetVal = State.Tmp0001;
        State.CallArg = State.Tmp0002;
    }
}
