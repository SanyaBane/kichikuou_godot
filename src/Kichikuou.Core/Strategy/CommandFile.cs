using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// What the hand-ported command files (<c>C*.ADV</c>) share: the context, the record cursors, and
/// the engine commands each file repeats in its own label stubs — the release wait before a menu
/// (<c>IM Ui_MouseX, Ui_MouseY</c> until no button), the pointer saved for a cancel (<c>IM
/// Tmp0019, Tmp0020</c>) and put back (<c>IZ</c>), the menu (<c>]</c>) and the message runs. Every
/// event is the one <see cref="SceneRunner"/> raises for the same statement, so a port and its
/// file run from the decompile can be compared event by event. A file whose literals the port
/// must not embed (<c>MS n, "text"</c>, data tables) reads them from the player's files through
/// the scenario source.
/// </summary>
public abstract class CommandFile(ScriptContext script, string file, IScenarioSource? scenario = null)
{
    /// <summary>The shared services the file runs on.</summary>
    public ScriptContext Script { get; } = script;

    /// <summary>The scenario file this class ports, e.g. <c>Cﾃｷﾄｼ.ADV</c>.</summary>
    protected string File { get; } = file;

    /// <summary>The game state.</summary>
    protected GameState State => Script.State;

    /// <summary>The context's input, which must answer the scene inputs (menus, the mouse).</summary>
    protected ISceneInput Input =>
        Script.Input as ISceneInput
        ?? throw new InvalidOperationException($"{File} reads menus and the mouse: the context's input must be an ISceneInput.");

    /// <summary>The player-unit record selected by <c>Unit_Cur</c>, re-read on every access.</summary>
    protected UnitRecord Unit => State.Units[State.Unit_Cur];

    /// <summary>The commander record selected by <c>Cmdr_Cur</c>, re-read on every access.</summary>
    protected CmdrRecord Cmdr => State.Cmdrs[State.Cmdr_Cur];

    /// <summary>The province record selected by <c>Prov_Cur</c>, re-read on every access.</summary>
    protected ProvRecord Prov => State.Provs[State.Prov_Cur];

    /// <summary>The dungeon record selected by <c>Dgn_Cur</c>, re-read on every access.</summary>
    protected DgnRecord Dgn => State.Dgns[State.Dgn_Cur];

    /// <summary>The item record selected by <c>Item_Cur</c>, re-read on every access.</summary>
    protected ItemRecord Item => State.Items[State.Item_Cur];

    /// <summary>The history record selected by <c>Hist_Cur</c>, re-read on every access.</summary>
    protected HistRecord Hist => State.History[State.Hist_Cur];

    /// <summary>Line <paramref name="line"/> of this file.</summary>
    protected ScriptRef Ref(int line) => new(File, line);

    /// <summary><c>!Ui_WaoOp : op! %#WAO_SUB.ADV:</c> — one of the file's window stubs.</summary>
    protected Task WaoAsync(Sys3Int op) => Script.WaoAsync(op);

    /// <summary>A text run of this file without numbers, starting at <paramref name="line"/>.</summary>
    protected Task TextAsync(int line) => Script.TextAsync(File, line);

    /// <summary>A text run whose <c>X expr</c> commands print computed string variables, evaluated now.</summary>
    protected Task StringsAsync(int line, params Sys3Int[] strings) =>
        Script.ShowAsync(new MessageEvent(Ref(line), strings.Length > 0 ? strings : null));

    /// <summary>A presentation-only engine command (<c>G</c>, <c>CC</c>, <c>T</c>, <c>ZC</c>, …) with its evaluated arguments.</summary>
    protected Task EngineAsync(string command, params Sys3Int[] args) => Script.ShowAsync(new EngineEvent(command, args));

    /// <summary>The statement at <paramref name="line"/> of this file, read from the player's files.</summary>
    protected ScriptStatement Literal(int line) =>
        (scenario ?? throw new InvalidOperationException($"{File} reads its literals from the player's files: no scenario source given."))
        .ReadStatement(Ref(line));

    /// <summary><c>MS n, "text":</c> at <paramref name="line"/>, the text from the player's files.</summary>
    protected void SetLiteral(int line)
    {
        var statement = Literal(line) as SetStringStatement
            ?? throw new ScenarioFormatException($"{Ref(line)}: expected MS n, \"text\".");
        Script.SetString(statement.Slot, statement.Text);
    }

    /// <summary>A text run with its <c>H</c> numbers, evaluated now.</summary>
    protected Task ValuesAsync(int line, params Sys3Int[] values) =>
        Script.ShowAsync(new ValueMessageEvent(Ref(line), values));

    /// <summary>A run of <c>R</c> only: one line break.</summary>
    protected Task LineBreakAsync() => Script.ShowAsync(new LineBreakEvent());

    /// <summary><c>MM target, source</c>.</summary>
    protected void CopyString(Sys3Int target, Sys3Int source) => Script.CopyString(target, source);

    /// <summary><c>IM x, y:</c> — the button into <c>RetVal</c>; the caller stores the position.</summary>
    protected async Task<MouseState> MouseAsync()
    {
        var m = await Input.GetMouseAsync();
        State.RetVal = m.Button;
        return m;
    }

    /// <summary><c>IM Ui_MouseX, Ui_MouseY:</c> repeated while a button is down — the wait before every menu.</summary>
    protected async Task WaitForReleaseAsync()
    {
        do
        {
            var m = await MouseAsync();
            State.Ui_MouseX = m.X;
            State.Ui_MouseY = m.Y;
        }
        while (State.RetVal != 0);
    }

    /// <summary><c>IM Tmp0019, Tmp0020:</c> — the pointer, where a cancelled menu puts it back.</summary>
    protected async Task SavePointerAsync()
    {
        var m = await MouseAsync();
        State.Tmp0019 = m.X;
        State.Tmp0020 = m.Y;
    }

    /// <summary><c>IZ Tmp0019, Tmp0020:</c> — the pointer back where it was before the menu.</summary>
    protected Task RestorePointerAsync() =>
        Script.ShowAsync(new EngineEvent("IZ", [State.Tmp0019, State.Tmp0020]));

    /// <summary>A menu item <c>$label$ 'text' $</c> registered at <paramref name="line"/>, its text on the next line.</summary>
    protected SceneMenuItem MenuItem(int line) =>
        new(Ref(line), [new CapturedText(Ref(line + 1), [], [])]);

    /// <summary>
    /// <c>]</c>: the player picks one of <paramref name="items"/> (1-based) or cancels (0); the
    /// message window is cleared either way.
    /// </summary>
    protected async Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
    {
        int choice = await Input.SelectAsync(items);
        if (choice < 0 || choice > items.Count)
            throw new InvalidOperationException($"{File}: choice {choice} of a menu with {items.Count} items.");
        await Script.ShowAsync(new MessageClearEvent());
        return choice;
    }

    /// <summary><c>A</c>: waits for a key, then clears the message.</summary>
    protected async Task HitAnyKeyAsync()
    {
        await Script.ShowAsync(new KeyWaitEvent());
        _ = await Input.WaitForKeyAsync();
        await Script.ShowAsync(new MessageClearEvent());
    }

    /// <summary>A path of the file that a later part ports; reaching it fails.</summary>
    protected NotSupportedException NotPorted(int line, string what, string part) =>
        new($"{File}:{line}: {what} is not ported yet (part {part}).");
}
