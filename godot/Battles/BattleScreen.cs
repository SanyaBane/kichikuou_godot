using Godot;
using Kichikuou.Content;
using Kichikuou.Content.Battles;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Golden;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Game.Battles;

/// <summary>How a battle screen runs its capture.</summary>
public enum BattleMode
{
    /// <summary>The player fights the battle from the capture's entry state.</summary>
    Play,

    /// <summary>The recorded battle plays back as it happened in the original.</summary>
    Watch,
}

/// <summary>
/// Draft battle screen: a message log, both sides' slots read from <see cref="GameState"/> and
/// buttons for every input the battle asks for. It implements the core's view and input
/// interfaces; the original-layout screen (docs/battle/screen-layout.md) replaces the drawing,
/// the plumbing stays.
/// </summary>
public partial class BattleScreen : Control, IBattleView, IBattleInput
{
    private const ushort KeyReturn = 16;   // xsystem35 SYS35KEY_RET (left click, Enter)
    private const ushort KeySpace = 32;    // SYS35KEY_SPC (right click, Space)

    private readonly string _capturePath;
    private readonly BattleMode _mode;
    private GameState? _state;
    private ScenarioText _text = null!;
    private TaskCompletionSource<ushort>? _click;
    private bool _closing;

    private Label _title = null!;
    private RichTextLabel _log = null!;
    private readonly Label[] _playerSlots = new Label[4];
    private readonly Label[] _enemySlots = new Label[4];
    private HFlowContainer _commands = null!;
    private Label _prompt = null!;

    /// <summary>For the engine; screens are created with the other constructor.</summary>
    public BattleScreen() : this("", BattleMode.Play)
    {
    }

    public BattleScreen(string capturePath, BattleMode mode)
    {
        _capturePath = capturePath;
        _mode = mode;
    }

    /// <summary>Development: no watch delays.</summary>
    public bool Fast { get; init; }

    /// <summary>Development: quit the program when the battle ends.</summary>
    public bool QuitWhenDone { get; init; }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        BuildLayout();
        _text = new ScenarioText(GameData.ScenarioDirectory);
        _ = RunAsync();
    }

    public override void _ExitTree()
    {
        _closing = true;
        _click?.TrySetCanceled();
    }

    // ---------------------------------------------------------------- layout

    private void BuildLayout()
    {
        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize, 12);
        AddChild(root);

        var top = new HBoxContainer();
        var back = new Button { Text = "◀ Back" };
        back.Pressed += () => GetTree().ChangeSceneToFile("res://Main.tscn");
        top.AddChild(back);
        _title = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        top.AddChild(_title);
        root.AddChild(top);

        var middle = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        middle.AddChild(SlotColumn(_playerSlots));
        _log = new RichTextLabel
        {
            ScrollFollowing = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SelectionEnabled = true,
        };
        _log.AddThemeFontSizeOverride("normal_font_size", 18);
        middle.AddChild(_log);
        middle.AddChild(SlotColumn(_enemySlots));
        root.AddChild(middle);

        _prompt = new Label();
        root.AddChild(_prompt);
        _commands = new HFlowContainer();
        root.AddChild(_commands);
    }

    private static VBoxContainer SlotColumn(Label[] labels)
    {
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(280, 0) };
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i] = new Label { CustomMinimumSize = new Vector2(280, 110), AutowrapMode = TextServer.AutowrapMode.Off };
            column.AddChild(labels[i]);
        }
        return column;
    }

    private void RefreshSlots()
    {
        if (_state is not { } s)
            return;
        Sys3Int[] players = [s.Bt_PlayerSlot1, s.Bt_PlayerSlot2, s.Bt_PlayerSlot3, s.Bt_PlayerSlot4];
        Sys3Int[] focus = [s.Bt_PSlot1Focus, s.Bt_PSlot2Focus, s.Bt_PSlot3Focus, s.Bt_PSlot4Focus];
        Sys3Int[] orders = [s.Bt_PlayerOrder1, s.Bt_PlayerOrder2, s.Bt_PlayerOrder3, s.Bt_PlayerOrder4];
        for (int i = 0; i < 4; i++)
        {
            Sys3Int u = players[i];
            if (u == 0)
            {
                _playerSlots[i].Text = $"P{i + 1}  —";
                continue;
            }
            var unit = s.Units[u];
            string order = orders[i].Value switch { 1 => "  auto-attack", 2 => "  auto-defend", _ => "" };
            _playerSlots[i].Text = $"P{i + 1}  {UnitName(s, u)}\n  troops {unit.Troops}/{unit.MaxTroops}\n  HP {unit.HP}/{unit.MaxHP}"
                + $"\n  focus {focus[i]}{order}";
        }
        Sys3Int[] enemies = [s.Bt_EnemySlot1, s.Bt_EnemySlot2, s.Bt_EnemySlot3, s.Bt_EnemySlot4];
        for (int i = 0; i < 4; i++)
        {
            Sys3Int c = enemies[i];
            if (c == 0)
            {
                _enemySlots[i].Text = $"E{i + 1}  —";
                continue;
            }
            var cmdr = s.Cmdrs[c];
            _enemySlots[i].Text = $"E{i + 1}  {CmdrName(s, c)}\n  troops {cmdr.Troops}/{cmdr.MaxTroops}\n  HP {cmdr.HP}/{cmdr.MaxHP}";
        }
        _title.Text = $"{(_mode == BattleMode.Watch ? "Recording" : "Battle")} at "
            + $"{s.Strings[(s.Prov_StrBase + s.Prov_Cur - 1).Value]} — rounds left {s.Bt_RoundsLeft}, troop limit {s.Bt_TroopLimit}";
    }

    private static string UnitName(GameState s, Sys3Int unit) => s.Strings[(s.Unit_StrBase + (unit - 1) * 3).Value];

    private static string CmdrName(GameState s, Sys3Int cmdr) => s.Strings[(s.Cmdr_StrBase + (cmdr - 1) * 3).Value];

    private void Log(string text, string? color = null)
    {
        _log.AppendText(color is null ? EscapeBbcode(text) + "\n" : $"[color={color}]{EscapeBbcode(text)}[/color]\n");
    }

    private static string EscapeBbcode(string text) => text.Replace("[", "[lb]");

    // ---------------------------------------------------------------- running

    private async Task RunAsync()
    {
        try
        {
            var capture = await Task.Run(() => GoldenCapture.Load(_capturePath));
            if (_mode == BattleMode.Watch)
            {
                var report = await GoldenReplay.RunAsync(capture, view: new WatchView(this), started: s => _state = s);
                Log(report.Passed ? "— End of the recording (the port matched the original)." : "— End: " + report.Describe(10), "gray");
            }
            else
            {
                var (state, jump) = await BattleSandbox.RunLiveAsync(capture, this, this, (uint)System.Environment.TickCount,
                    file =>
                    {
                        Log($"(The story scene {file} cannot run yet and was skipped.)", "orange");
                        return Task.CompletedTask;
                    },
                    started: s => _state = s);
                RefreshSlots();
                Log(jump is not null ? $"— The battle ended with a jump to {jump} (game over)."
                    : state.Bt_Result == 2 ? "— Victory." : "— Defeat.", "yellow");
            }
        }
        catch (TaskCanceledException) when (_closing)
        {
        }
        catch (Exception e)
        {
            Log(e.ToString(), "red");
            GD.PushError(e.ToString());
        }
        ClearCommands();
        _prompt.Text = "The battle is over.";
        if (QuitWhenDone)
        {
            GD.Print(_log.GetParsedText());
            GetTree().Quit();
        }
    }

    // ---------------------------------------------------------------- view

    public async Task ShowAsync(BattleEvent battleEvent)
    {
        RefreshSlots();
        switch (battleEvent)
        {
            case MessageEvent m:
                Log(Resolve(m.Text, [], m.StringVariables));
                break;
            case FlowValueMessageEvent v:
                Log(Resolve(v.Text, v.Values, null));
                break;
            case FlowErrorEvent e:
                Log(Resolve(e.Text, [], null), "red");
                break;
            case ActionLabelsEvent a:
                if (a.PlayerLabel is { } p)
                    Log(Resolve(p, [], null), "lightblue");
                if (a.EnemyLabel is { } en)
                    Log(Resolve(en, [], null), "salmon");
                break;
            case RoundHeaderEvent h:
                Log($"=== Round — {h.RoundsLeft} left, limit {h.TroopLimit}, advantage {h.PlayerAdvantage} : {h.EnemyAdvantage} ===", "yellow");
                break;
            case ExchangeResultsEvent when _state is { } s:
                Log($"  dealt: troops {s.Bt_PTroopVsTroopDmg + s.Bt_PCmdrVsTroopDmg}, HP {s.Bt_PTroopVsCmdrDmg + s.Bt_PCmdrVsCmdrDmg}"
                    + $"   taken: troops {s.Bt_ETroopVsTroopDmg + s.Bt_ECmdrVsTroopDmg}, HP {s.Bt_ETroopVsCmdrDmg + s.Bt_ECmdrVsCmdrDmg}", "gray");
                break;
            case WaoCallEvent { Op.Value: 10 } or FlavourWindowCloseEvent or FlowWindowEvent { Op.Value: 0 or 10 }:
                Log("");
                break;
            case UnitInfoEvent u when _state is { } s:
                Log($"[info] {UnitName(s, u.Unit)}: atk {s.Units[u.Unit].Atk}, def {s.Units[u.Unit].Def}, level {s.Units[u.Unit].Level}", "gray");
                break;
            case EnemyInfoEvent c when _state is { } s:
                Log($"[info] {CmdrName(s, c.Cmdr)}: troops {s.Cmdrs[c.Cmdr].Troops}, HP {s.Cmdrs[c.Cmdr].HP}", "gray");
                break;
        }
    }

    private string Resolve(ScriptRef source, IReadOnlyList<Sys3Int> numbers, IReadOnlyList<Sys3Int>? stringVariables)
    {
        try
        {
            return _text.Read(source).Format(numbers, v => _state?.Strings[v] ?? "", stringVariables);
        }
        catch (Exception e) when (e is ScenarioTextException or ArgumentException)
        {
            return $"[{source}: {e.Message}]";
        }
    }

    // ---------------------------------------------------------------- input

    private Task<ushort> WaitClickAsync(string prompt)
    {
        _prompt.Text = prompt;
        _click = new TaskCompletionSource<ushort>();
        return _click.Task;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_click is null)
            return;
        ushort? key = @event switch
        {
            InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } => KeyReturn,
            InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } => KeySpace,
            InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter } => KeyReturn,
            InputEventKey { Pressed: true, Keycode: Key.Space or Key.Escape } => KeySpace,
            _ => null,
        };
        if (key is ushort k)
        {
            var click = _click;
            _click = null;
            _prompt.Text = "";
            click.TrySetResult(k);
            GetViewport().SetInputAsHandled();
        }
    }

    public async Task<Sys3Int> WaitForKeyAsync()
    {
        ushort key = await WaitClickAsync("▶ Click or press Enter to continue");
        return key;
    }

    public async Task<Sys3Int> PauseAsync(Sys3Int ticks)
    {
        // Y 3: ticks × 16 ms, cut short by a key (which goes to RetVal).
        var timer = GetTree().CreateTimer(ticks.Value * 0.016);
        var click = WaitClickAsync("");
        while (!click.IsCompleted && timer.TimeLeft > 0)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (click.IsCompleted)
            return click.Result;
        _click = null;
        _prompt.Text = "";
        return 0;
    }

    public async Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
    {
        var chosen = new TaskCompletionSource<FigtSelChoice>();
        ClearCommands();
        _prompt.Text = $"Slot P{menu.ActingSlot}: choose a command";
        foreach (var option in menu.Options)
        {
            foreach (var argument in TargetsOf(option))
            {
                var button = new Button { Text = CommandLabel(option.Command, argument) };
                var choice = new FigtSelChoice(option.Command, argument);
                button.Pressed += () => chosen.TrySetResult(choice);
                _commands.AddChild(button);
            }
        }
        using var registration = CancelOnClose(chosen);
        var result = await chosen.Task;
        ClearCommands();
        _prompt.Text = "";
        return result;
    }

    /// <summary>The arguments worth a button: every target of a targeted command, else the first.</summary>
    private static IEnumerable<Sys3Int> TargetsOf(FigtSelOption option)
    {
        bool targeted = option.Command.Value is (>= 1 and <= 3) or (>= 10 and <= 21) or (>= 50 and <= 54);
        return targeted ? option.Arguments : option.Arguments.Take(1);
    }

    private string CommandLabel(Sys3Int command, Sys3Int argument)
    {
        string name = command.Value switch
        {
            1 => "Attack", 2 => "Bow", 3 => "Magic", 4 => "Defend", 5 => "Wait (focus)", 6 => "Retreat",
            7 => "Repel (debug)", 8 => "Auto-attack", 9 => "Auto-defend", 10 => "Cheater Beam",
            11 => "Rance Attack", 12 => "By la Way", 13 => "White Destruction Beam", 14 => "High Speed",
            15 => "Vanish Bomb", 16 => "War Dance", 17 => "Black Destruction Beam", 18 => "Whirlwind Shot",
            19 => "Chuhai", 20 => "Soaring Wave", 21 => "EPO Light", 30 => "Capsule: Amoeba",
            31 => "Capsule: Jagi Jagi", 32 => "Fog Card", 50 => "Order: manual", 51 => "Order: auto-attack",
            52 => "Order: auto-defend", 53 => "Unit info", 54 => "Enemy info",
            _ => $"Command {command}",
        };
        if (_state is not { } s)
            return name;
        if (command.Value is (>= 1 and <= 3) or (>= 10 and <= 21) && argument.Value is >= 4 and <= 7)
            return $"{name} → {EnemyInSlot(s, argument.Value - 3)}";
        if (command.Value is >= 50 and <= 53 && argument.Value is >= 1 and <= 4)
            return $"{name}: P{argument}";
        if (command.Value == 54 && argument.Value is >= 1 and <= 4)
            return $"{name}: {EnemyInSlot(s, argument.Value)}";
        return name;
    }

    private static string EnemyInSlot(GameState s, int slot)
    {
        Sys3Int c = slot switch { 1 => s.Bt_EnemySlot1, 2 => s.Bt_EnemySlot2, 3 => s.Bt_EnemySlot3, _ => s.Bt_EnemySlot4 };
        return c == 0 ? $"E{slot}" : CmdrName(s, c);
    }

    public async Task<Sys3Int?> ChooseAsync(BattleMenu menu)
    {
        var chosen = new TaskCompletionSource<Sys3Int?>();
        ClearCommands();
        _prompt.Text = $"{menu.Kind}:";
        foreach (var option in menu.Options)
        {
            var button = new Button { Text = Resolve(option.Label, [], null).Replace("\n", " ") };
            var code = option.Code;
            button.Pressed += () => chosen.TrySetResult(code);
            _commands.AddChild(button);
        }
        var cancel = new Button { Text = "(cancel)" };
        cancel.Pressed += () => chosen.TrySetResult(null);
        _commands.AddChild(cancel);
        using var registration = CancelOnClose(chosen);
        var result = await chosen.Task;
        ClearCommands();
        _prompt.Text = "";
        return result;
    }

    public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult((Sys3Int)0);

    private void ClearCommands()
    {
        foreach (var child in _commands.GetChildren())
            child.QueueFree();
    }

    private IDisposable CancelOnClose<T>(TaskCompletionSource<T> source)
    {
        void OnExit() => source.TrySetCanceled();
        TreeExiting += OnExit;
        return new Unsubscribe(() => TreeExiting -= OnExit);
    }

    private sealed class Unsubscribe(Action action) : IDisposable
    {
        public void Dispose() => action();
    }

    /// <summary>
    /// The view for a recording: shows the same events, paced for watching — a message stays a
    /// moment (a click skips it), pauses take their time. Inputs come from the recording.
    /// </summary>
    private sealed class WatchView(BattleScreen screen) : IBattleView
    {
        public async Task ShowAsync(BattleEvent battleEvent)
        {
            await screen.ShowAsync(battleEvent);
            double seconds = battleEvent switch
            {
                MessageEvent or FlowValueMessageEvent => 0.9,
                PauseEvent p => p.Ticks.Value * 0.016,
                ExchangeResultsEvent => 0.6,
                _ => 0,
            };
            if (seconds > 0 && !screen.Fast)
                await screen.WatchDelayAsync(seconds);
        }
    }

    private async Task WatchDelayAsync(double seconds)
    {
        var timer = GetTree().CreateTimer(seconds);
        var click = WaitClickAsync("(click to skip)");
        while (!click.IsCompleted && timer.TimeLeft > 0)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _click = null;
        _prompt.Text = "";
    }
}
