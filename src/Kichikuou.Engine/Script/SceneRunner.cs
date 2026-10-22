using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

/// <summary>
/// The scene runtime (part E2): runs scenario files read as <see cref="ScriptProgram"/>s from the
/// player's decompile — the story scenes (<c>G*</c>, <c>I*</c>) and the opening and battle inserts
/// — statement by statement on a <see cref="ScriptContext"/>, the way xsystem35 runs the
/// bytecode. As the context's <see cref="IStoryHooks"/> it receives every page call the rules
/// and the scenes make: scene files run here, every other file goes to the hooks given at
/// construction (the ported rules: the battle, the death handler, the party picker), so a scene
/// may start a battle that plays another scene. Which files are scenes is the variant's rule
/// (<see cref="VariantProfile.IsSceneFile"/>) unless a test gives its own. The helper files a
/// variant ports (the original's <c>WAO_SUB</c>, <c>M</c>, <c>WAO_MAP</c>, <c>PCMPLAY</c>) run in
/// its context (<see cref="ScriptContext.CallAsync"/>).
/// </summary>
/// <remarks>
/// Call stack as in <c>scenario.c</c>: a label call pushes its return; <c>US</c> pushes variables;
/// <c>\0:</c> returns to the innermost label call and <c>%0:</c> leaves the page, both restoring
/// the variables pushed above their target (<c>pop_state</c>). A page call is a C# call, so each
/// page keeps its own part of the stack. The menu being built (<c>$label$</c> … <c>]</c>) is
/// engine state shared by all pages, as <c>nact-&gt;sel</c> is. The save-data commands (<c>LD</c>,
/// <c>QD</c>, <c>LP</c>, <c>QP</c>, <c>QC</c>, <c>LT</c>; only the save screen uses them) run on
/// the context's slots; quitting (<c>ZZ0, 0</c>, the end of <c>OVER</c>) leaves by
/// <see cref="GameQuitException"/>.
/// </remarks>
public sealed class SceneRunner(ISceneSource scenes, IStoryHooks rules, Func<string, bool>? isScene = null) : IStoryHooks
{
    private readonly Dictionary<string, ScriptProgram> _programs = new(StringComparer.Ordinal);
    private readonly List<SceneMenuItem> _menu = [];
    private readonly List<(ScriptProgram Program, int Target)> _menuTargets = [];
    private MenuItemBuilder? _item;
    private ScriptContext? _script;

    /// <summary>The context the scenes run on; set once with <see cref="Bind"/>.</summary>
    public ScriptContext Script => _script ?? throw new InvalidOperationException("The scene runner is not bound to a context.");

    private ScriptState State => Script.State;

    private IScriptView View => Script.View;

    private ISceneInput Input =>
        Script.Input as ISceneInput
        ?? throw new InvalidOperationException("The context's input does not answer the scene inputs (ISceneInput).");

    /// <summary>
    /// Binds the runner to the context it serves as story hooks (the context is built with the
    /// runner, so it is bound afterwards). A context of the same game built elsewhere (e.g. by a
    /// <c>Battle</c>) shares the state, the generator, the view and the input, so scenes it calls
    /// behave the same.
    /// </summary>
    public void Bind(ScriptContext script)
    {
        if (_script is not null && !ReferenceEquals(_script, script))
            throw new InvalidOperationException("The scene runner is already bound to another context.");
        _script = script;
    }

    /// <inheritdoc />
    public Task RunEventAsync(string file, Sys3Int entry) =>
        (isScene ?? State.Profile.IsSceneFile)(file) ? RunFileAsync(file) : rules.RunEventAsync(file, entry);

    private readonly List<string> _running = [];

    /// <summary>The scene files running now, outermost first (a scene that calls another, e.g. a battle's insert, is below it).</summary>
    public IReadOnlyList<string> RunningScenes => _running;

    /// <summary>Runs <paramref name="file"/> from its first statement up to its page return (<c>Ev_Entry</c> as the caller left it).</summary>
    public async Task RunFileAsync(string file)
    {
        _running.Add(file);
        try
        {
            await RunProgramAsync(file);
        }
        finally
        {
            _running.RemoveAt(_running.Count - 1);
        }
    }

    private async Task RunProgramAsync(string file)
    {
        var program = Program(file);
        var stack = new List<StackEntry>();
        var statements = program.Statements;
        int pc = 0;
        while (true)
        {
            if (pc >= statements.Count)
                throw new ScenarioFormatException($"{file}: the script ran past the end of the file.");
            var statement = statements[pc++];
            switch (statement)
            {
                // --- Data and strings
                case AssignStatement s:
                    State.WriteVariable(s.Var, Eval(s.Value));
                    break;
                case SetStringStatement s:
                    Script.SetString(s.Slot, s.Text);
                    break;
                case CopyStringStatement s:
                    Script.CopyString(Eval(s.Target), Eval(s.Source));
                    break;
                case AppendStringStatement s:
                    Script.AppendString(Eval(s.Target), Eval(s.Source));
                    break;
                case NumberToStringStatement s:
                    Script.NumberToString(Eval(s.Slot), Eval(s.Width), Eval(s.Value));
                    break;
                case ClearVariablesStatement s:
                    {
                        int count = Eval(s.Count).Value;
                        for (int i = 0; i < count; i++)
                            State.WriteRun(s.Var, i, Sys3Int.Zero);
                        break;
                    }
                case CopyVariablesStatement s:
                    {
                        int count = Eval(s.Count).Value;
                        for (int i = 0; i < count; i++)
                            State.WriteRun(s.Target, i, State.ReadRun(s.Source, i));
                        break;
                    }
                case SetDataTableStatement s:
                    Script.SetDataTable(s.Words);
                    break;
                case ReadDataStatement s:
                    State.WriteVariable(s.Var, Script.ReadDataWord(s.At, Eval(s.Index)));
                    break;
                case SetStringTableStatement s:
                    Script.SetStringTable(s.Strings);
                    break;
                case ReadStringStatement s:
                    Script.ReadDataString(s.At, Eval(s.Slot), Eval(s.Skip));
                    break;
                case CompareStringsStatement s:
                    State.RetVal = Script.CompareStrings(Eval(s.Left), Eval(s.Right));
                    break;
                case LocalVariablesStatement { Push: true } s:
                    {
                        int count = Eval(s.Count).Value;
                        for (int i = 0; i < count; i++)
                            stack.Add(new SavedVariable(s.Var + i, Slot(s, s.Var, i)));
                        break;
                    }
                case LocalVariablesStatement s:
                    {
                        // sl_popVar: the values go back from the last variable down, whatever
                        // variable they were pushed from.
                        int count = Eval(s.Count).Value;
                        for (int i = count - 1; i >= 0; i--)
                        {
                            if (stack.Count == 0 || stack[^1] is not SavedVariable saved)
                                throw new ScenarioFormatException($"{s.At}: UG with no pushed variable on the stack.");
                            stack.RemoveAt(stack.Count - 1);
                            Slot(s, s.Var, i) = saved.Value;
                        }
                        break;
                    }
                case RandomStatement s:
                    Script.Rnd(Eval(s.Max));
                    break;

                // --- Control flow
                case GotoStatement s:
                    pc = s.Target;
                    break;
                case IfStatement s:
                    if (Eval(s.Condition) == Sys3Int.Zero)
                        pc = s.EndTarget;
                    break;
                case CallLabelStatement s:
                    stack.Add(new LabelCall(pc));
                    pc = s.Target;
                    break;
                case ReturnLabelStatement s:
                    pc = ReturnFromLabel(stack, s);
                    break;
                case CallFileStatement s:
                    await Script.CallAsync(s.File);
                    break;
                case ReturnStatement:
                    Unwind(stack, untilLabelCall: false);
                    return;
                case JumpFileStatement s:
                    ScriptContext.JumpTo(s.File);
                    break;

                // --- Messages and menus
                case MessageStatement s:
                    if (_item is not null)
                        _item.Texts.Add(Capture(s.Text));
                    else
                        await View.ShowAsync(Message(s.Text));
                    break;
                case LineBreakStatement:
                    await View.ShowAsync(new LineBreakEvent());
                    break;
                case MenuItemStatement s:
                    if (_item is not null)
                        throw new ScenarioFormatException($"{s.At}: a menu item starts while the one at {_item.At} is open.");
                    _item = new MenuItemBuilder(s.At, program, s.Target);
                    break;
                case MenuItemEndStatement s:
                    if (_item is null)
                        throw new ScenarioFormatException($"{s.At}: '$' with no menu item open.");
                    _menu.Add(new SceneMenuItem(_item.At, _item.Texts));
                    _menuTargets.Add((_item.Program, _item.Target));
                    _item = null;
                    break;
                case MenuStatement s:
                    pc = await MenuAsync(s, program, pc);
                    break;
                case MessageResetStatement s:
                    await View.ShowAsync(new MessageResetEvent(Eval(s.Mode)));
                    break;

                // --- Input
                case HitAnyKeyStatement:
                    await View.ShowAsync(new KeyWaitEvent());
                    _ = await Input.WaitForKeyAsync();
                    await View.ShowAsync(new MessageClearEvent());
                    break;
                case KeyWaitStatement:
                    {
                        await View.ShowAsync(new KeyWaitEvent());
                        Sys3Int key = await Input.WaitForKeyAsync();
                        State.RetVal = key;
                        break;
                    }
                case MouseStatement s:
                    {
                        var mouse = await Input.GetMouseAsync();
                        State.RetVal = mouse.Button;
                        State.WriteVariable(s.XVar, mouse.X);
                        State.WriteVariable(s.YVar, mouse.Y);
                        break;
                    }
                case CursorStatement s:
                    State.WriteVariable(s.Var, await Input.SetCursorAsync(Eval(s.Shape)));
                    break;
                case PauseStatement s:
                    await Script.PauseAsync(Eval(s.Ticks));
                    break;

                // --- Sound, clock, devices
                case MusicStatement s:
                    await Script.MusicAsync(Eval(s.Track));
                    break;
                case MusicInfoStatement s:
                    State.WriteVariable(s.Var, s.Device == 0 ? await Script.GetPlayingTrackAsync() : await Input.GetMidiTrackAsync());
                    break;
                case CdPositionStatement s:
                    WriteSlots(s, s.Var, await Input.GetCdPositionAsync(), 4);
                    break;
                case DateStatement s:
                    WriteSlots(s, s.Var, await Input.GetDateAsync(), 7);
                    break;
                case TimerResetStatement s:
                    await Input.ResetTimerAsync(Eval(s.Value));
                    break;
                case TimerStatement s:
                    State.WriteVariable(s.Var, await Input.GetTimerAsync(s.Mode));
                    break;
                case EngineStatement s:
                    await Script.EngineAsync(s.Command, s.Args.Select(Eval).ToArray());
                    break;
                case EffectCopyStatement s:
                    await EffectCopyAsync(s);
                    break;
                case DisplayInfoStatement s:
                    WriteSlots(s, s.Var, await Input.GetDisplayAsync(), 3);
                    break;
                case SaveDataStatement s:
                    RunSaveCommand(s);
                    break;
                case QuitStatement s:
                    // cmdz.c commandZZ0: 0 quits with RetVal as the exit code (the end of OVER); 1, waiting
                    // for the window to close, is used nowhere.
                    if (Eval(s.Mode) != Sys3Int.Zero)
                        throw new NotSupportedException($"{s.At}: ZZ0 {Eval(s.Mode).Value} is not ported.");
                    throw new GameQuitException(State.RetVal);
                default:
                    throw new ScenarioFormatException($"{statement.At}: {statement.GetType().Name} cannot run in a scene.");
            }
        }
    }

    /// <summary>The save-data commands on the context's slots; a successful <c>LD</c> leaves by <see cref="GameLoadedException"/>.</summary>
    private void RunSaveCommand(SaveDataStatement s)
    {
        int Var() => s.Var ?? throw new ScenarioFormatException($"{s.At}: {s.Command} without its variable.");
        Sys3Int Count() => Eval(s.Count ?? throw new ScenarioFormatException($"{s.At}: {s.Command} without its count."));
        switch (s.Command)
        {
            case "QD":
                Script.SaveAll(Eval(s.Slot));
                break;
            case "LD":
                Script.LoadAll(Eval(s.Slot));
                break;
            case "LP":
                Script.LoadPartial(Eval(s.Slot), Var(), Count());
                break;
            case "QP":
                Script.SavePartial(Eval(s.Slot), Var(), Count());
                break;
            case "QC":
                Script.CopySave(Eval(s.Slot), Eval(s.Other ?? throw new ScenarioFormatException($"{s.At}: QC without its source slot.")));
                break;
            case "LT":
                Script.ReadSaveTime(Eval(s.Slot), Var());
                break;
            default:
                throw new ScenarioFormatException($"{s.At}: unknown save-data command {s.Command}.");
        }
    }

    /// <summary>
    /// <c>CE</c>: the copy goes to the view (an <see cref="EngineEvent"/> <c>CE</c>, drawn at once),
    /// then the effect's time passes as a pause (<c>ags_runEffect</c>, 16 ms steps): with the wait
    /// flag set a key cuts it short, and with the flag 1 that key, or 0, lands in <c>RetVal</c>
    /// (<c>cmdc.c:142-144</c>). One difference: the engine's effects do not end on the message skip,
    /// the port's pause does (the logo, its only user, is over the sooner).
    /// </summary>
    private async Task EffectCopyAsync(EffectCopyStatement s)
    {
        var args = s.Args.Select(Eval).ToArray();
        await View.ShowAsync(new EngineEvent("CE", args));
        int effect = args[6].Value, option = args[7].Value, width = args[2].Value, wait = args[8].Value;
        // ecopy.c duration(): only the effect the scenario uses — 4, NACT_EFFECT_SKIP_LINE_LR_RL,
        // (option or 3) × half the width in ms (ﾌﾟﾛﾛｰｸﾞ.ADV:158: 3 × 320 = 960 ms).
        int milliseconds = effect == 4
            ? (option != 0 ? option : 3) * (width / 2)
            : throw new NotSupportedException($"{s.At}: CE effect {effect} is not ported.");
        var key = await Input.PauseAsync((Sys3Int)((milliseconds + 15) / 16), cancelable: wait != 0);
        if (wait == 1)
            State.RetVal = key;
    }

    private ScriptProgram Program(string file)
    {
        if (!_programs.TryGetValue(file, out var program))
        {
            program = scenes.ReadProgram(file);
            _programs[file] = program;
        }
        return program;
    }

    private Sys3Int Eval(ScriptExpr expr) => Script.Evaluate(expr);

    /// <summary>
    /// <c>]</c> (<c>sel_select</c>): the player picks one of the registered items, which are then
    /// forgotten; the message window is cleared; a choice continues at the item's label, a cancel
    /// after the <c>]</c>.
    /// </summary>
    private async Task<int> MenuAsync(MenuStatement menu, ScriptProgram program, int next)
    {
        if (_item is not null)
            throw new ScenarioFormatException($"{menu.At}: the menu opens while the item at {_item.At} is still being registered.");
        var items = _menu.ToArray();
        var targets = _menuTargets.ToArray();
        _menu.Clear();
        _menuTargets.Clear();
        int choice = await Script.SelectAsync(items);
        if (choice < 0 || choice > items.Length)
            throw new InvalidOperationException($"{menu.At}: choice {choice} of a menu with {items.Length} items.");
        await View.ShowAsync(new MessageClearEvent());
        if (choice == 0)
            return next;
        var (owner, target) = targets[choice - 1];
        // sl_jmpNear: the item's address is taken in the page that shows the menu.
        if (!ReferenceEquals(owner, program))
            throw new NotSupportedException($"{menu.At}: the chosen item was registered in {owner.File}.");
        return target;
    }

    /// <summary>A message for the window: numbers and computed string variables evaluated now.</summary>
    private ScriptEvent Message(ScriptText text)
    {
        var computed = text.Strings.Where(e => e is not ConstExpr).Select(Eval).ToArray();
        var strings = computed.Length > 0 ? computed : null;
        return text.Numbers.Count > 0
            ? new ValueMessageEvent(text.Start, text.Numbers.Select(Eval).ToArray(), strings)
            : new MessageEvent(text.Start, strings);
    }

    /// <summary>A text printed into a menu item: its values and string contents as they are now.</summary>
    private CapturedText Capture(ScriptText text)
    {
        var strings = State.Strings;
        return new CapturedText(
            text.Start,
            text.Numbers.Select(Eval).ToArray(),
            text.Strings.Select(e => Eval(e).Value is var n && n < strings.Length ? strings[n] : string.Empty).ToArray());
    }

    /// <summary>
    /// Variable <paramref name="var"/> + <paramref name="offset"/> as the commands that address a
    /// run of variables see it: the plain slots after a plain variable.
    /// </summary>
    private ref Sys3Int Slot(ScriptStatement at, int var, int offset)
    {
        if (State.IsBound(var))
            throw new NotSupportedException($"{at.At}: a run of variables starting at the bound VAR{var:D4}.");
        return ref State.Memory.Var(var + offset);
    }

    private void WriteSlots(ScriptStatement at, int var, IReadOnlyList<Sys3Int> values, int count)
    {
        if (values.Count != count)
            throw new InvalidOperationException($"{at.At}: the input gave {values.Count} values, the command writes {count}.");
        for (int i = 0; i < count; i++)
            Slot(at, var, i) = values[i];
    }

    /// <summary><c>\0:</c> (<c>sl_retNear</c>): back to the innermost label call, restoring the variables pushed since.</summary>
    private int ReturnFromLabel(List<StackEntry> stack, ReturnLabelStatement at)
    {
        if (!stack.Any(e => e is LabelCall))
            throw new ScenarioFormatException($"{at.At}: label return with no label call (a return from the page call).");
        return Unwind(stack, untilLabelCall: true);
    }

    /// <summary>
    /// Pops the page's stack: up to and including the innermost label call (whose return it
    /// gives), or everything (<c>%0:</c>, <c>sl_retFar</c>, which drops the label calls). Pushed
    /// variables get their values back on the way (<c>pop_state</c>).
    /// </summary>
    private int Unwind(List<StackEntry> stack, bool untilLabelCall)
    {
        while (stack.Count > 0)
        {
            var entry = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            switch (entry)
            {
                case SavedVariable v:
                    State.Memory.Var(v.Var) = v.Value;
                    break;
                case LabelCall call when untilLabelCall:
                    return call.ReturnPc;
            }
        }
        return -1;
    }

    private abstract record StackEntry;

    /// <summary>A pending label call and the statement to return to.</summary>
    private sealed record LabelCall(int ReturnPc) : StackEntry;

    /// <summary>A variable pushed by <c>US</c> with its value.</summary>
    private sealed record SavedVariable(int Var, Sys3Int Value) : StackEntry;

    private sealed class MenuItemBuilder(ScriptRef at, ScriptProgram program, int target)
    {
        public ScriptRef At { get; } = at;

        public ScriptProgram Program { get; } = program;

        public int Target { get; } = target;

        public List<CapturedText> Texts { get; } = [];
    }
}
