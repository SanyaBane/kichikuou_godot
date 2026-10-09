using System.Diagnostics;
using Godot;
using Kichikuou.Content;
using Kichikuou.Content.Battles;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Content.Scripts;
using Kichikuou.Content.Strategy;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Game.Battles;
using SelButton = Kichikuou.Content.Battles.MouseButton;

namespace Kichikuou.Game.Strategy;

/// <summary>How a game starts from the title.</summary>
public enum GameStart
{
    /// <summary>"New game": the new game, then the week loop.</summary>
    New,

    /// <summary>"Load": the save screen's load list (backing out returns to the title).</summary>
    Load,
}

/// <summary>What a <see cref="GameScreen"/> plays and how (the development switches of <see cref="Main"/>).</summary>
public sealed record GameOptions
{
    /// <summary>New game or load.</summary>
    public GameStart Start { get; init; }

    /// <summary>Development: plays unattended — keys answered, Turn END every week, the battles fought with the first attack.</summary>
    public bool Auto { get; init; }

    /// <summary>Development: no delays (pauses and fades take one frame, the clock runs 20 times faster).</summary>
    public bool Fast { get; init; }

    /// <summary>Development: quit the program when the game ends (exit code 1 after an error).</summary>
    public bool QuitWhenDone { get; init; }

    /// <summary>Development: save the screen (event CGs masked) as a PNG at every key wait and menu.</summary>
    public string? ScreenshotDirectory { get; init; }

    /// <summary>The random generator's seed (default: the tick count).</summary>
    public uint? Seed { get; init; }

    /// <summary>Where the save slots are (default <c>user://saves</c>); a Godot path or a folder.</summary>
    public string? SaveDirectory { get; init; }
}

/// <summary>
/// One game in the original's layout (part G0, owner's decisions 4, 8, 10): a
/// <see cref="GameSession"/> whose view and input are this node behind the proxies, drawn by a
/// <see cref="GamePresenter"/> (its <see cref="GamePresenter.Screen"/> is shown, fades animated) and
/// played with the mouse: key waits take a click, Enter or Space; menus a click on an item (right
/// click cancels); the map and the pickers turn the clicks into the pointer gestures the script
/// reads (<see cref="PointerScript"/>); the battle's input screens work as in <see cref="BattleScreen"/>.
/// Backspace returns to the title.
/// </summary>
public partial class GameScreen : Control, IScriptView, IGameInput
{
    private const ushort KeyReturn = 16;   // xsystem35 SYS35KEY_RET (left click, Enter)
    private const ushort KeySpace = 32;    // SYS35KEY_SPC (right click, Space, Escape)

    /// <summary>How far the left button must move on the map before a press is a drag (script pixels).</summary>
    private const int DragThreshold = 4;

    private readonly GameOptions _options;
    private readonly PointerScript _pointer = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private SceneTree _tree = null!;
    private GamePresenter _presenter = null!;
    private GodotGlyphRenderer _glyphs = null!;
    private BattleAudio _audio = null!;
    private TextureRect _view = null!;
    private ImageTexture? _texture;
    private byte[]? _frame;
    private Label _overlay = null!;
    private Label _notice = null!;
    private double _noticeLeft;
    private bool _closing;
    private bool _ended;
    private int _shot;

    private TaskCompletionSource<ushort>? _key;
    private TaskCompletionSource<int>? _menuAnswer;
    private FigtSelScreen? _sel;
    private BattleMenuScreen? _battleMenu;
    private TaskCompletionSource? _answered;

    private Vector2I _mouse;
    private bool _leftHeld;
    private bool _dragging;
    private Vector2I _pressAt;
    private Vector2I _dragFrom;

    private int _cursor = 1;
    private long _timerOffset;

    /// <summary>For the engine; screens are created with the other constructor.</summary>
    public GameScreen() : this(new GameOptions())
    {
    }

    public GameScreen(GameOptions options)
    {
        _options = options;
    }

    public override void _Ready()
    {
        _tree = GetTree();
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var background = new ColorRect { Color = Colors.Black, MouseFilter = MouseFilterEnum.Ignore };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);
        _view = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _view.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_view);
        _overlay = OutlinedLabel(VerticalAlignment.Center, 32);
        _notice = OutlinedLabel(VerticalAlignment.Top, 18);

        _audio = new BattleAudio();
        AddChild(_audio);
        _glyphs = new GodotGlyphRenderer();
        var scripts = new ScenarioScripts(GameData.ScenarioDirectory);
        _presenter = new GamePresenter(new ScenarioText(GameData.ScenarioDirectory), new CgLibrary(GameData.CgDirectory),
            _glyphs, scripts, _audio)
        {
            MaskEventCgs = _options.ScreenshotDirectory is not null,
        };
        _ = RunAsync(scripts);
    }

    private Label OutlinedLabel(VerticalAlignment vertical, int size)
    {
        var label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = vertical,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize, 24);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 8);
        AddChild(label);
        return label;
    }

    public override void _ExitTree()
    {
        _closing = true;
        _key?.TrySetCanceled();
        _menuAnswer?.TrySetCanceled();
        _answered?.TrySetCanceled();
        _glyphs?.Dispose();
    }

    public override void _Process(double delta)
    {
        if (_noticeLeft > 0 && (_noticeLeft -= delta) <= 0)
            _notice.Text = "";
        if (_leftHeld && _dragging)
            QueueDrag();
        Present();
    }

    /// <summary>Composes the screen and uploads it when it changed.</summary>
    private void Present()
    {
        if (_ended || !_presenter.Compose())
            return;
        _frame = _presenter.Screen.ReadScreen(_frame);
        var image = Image.CreateFromData(Surface.ScreenWidth * Surface.Scale, Surface.ScreenHeight * Surface.Scale,
            false, Image.Format.Rgba8, _frame);
        if (_texture is null)
        {
            _texture = ImageTexture.CreateFromImage(image);
            _view.Texture = _texture;
        }
        else
        {
            _texture.Update(image);
        }
    }

    /// <summary>A line at the top of the screen for a few seconds (files the port skips).</summary>
    private void Notice(string text)
    {
        _notice.Text = text;
        _noticeLeft = 4;
    }

    // ---------------------------------------------------------------- running

    private async Task RunAsync(ScenarioScripts scripts)
    {
        string message;
        bool failed = false;
        try
        {
            var state = new GameState();
            uint seed = _options.Seed ?? (uint)System.Environment.TickCount;
            GD.Print($"Kichikuou: random seed {seed}");
            _presenter.Bind(state);
            var saves = new DirectorySaveStore(ProjectSettings.GlobalizePath(_options.SaveDirectory ?? "user://saves"));
            var session = new GameSession(state, new XSystem35Random(seed), new ViewProxy(this), new GameInputProxy(this),
                scripts, scripts, saves, new UnportedFiles(this));
            await session.BootAsync();
            var end = _options.Start == GameStart.New ? await session.NewGameAsync() : await session.LoadFromTitleAsync();
            if (end is null)
            {
                Leave(failed: false);
                return;
            }
            GD.Print($"Kichikuou: the game ended, ending {end.EndingId.Value}.");
            _ended = true;
            _view.Visible = false;
            message = $"THE END — ending {end.EndingId.Value}";
        }
        catch (OperationCanceledException) when (_closing)
        {
            return;
        }
        catch (NotSupportedException e)
        {
            GD.Print("Kichikuou: not ported yet: " + e.Message);
            message = "This part of the game is not ported yet.\n" + e.Message;
        }
        catch (Exception e)
        {
            GD.PushError(e.ToString());
            message = "Error: " + e.Message;
            failed = true;
        }
        if (_presenter.Unresolved.Count + _presenter.Battle.Unresolved.Count > 0)
            GD.PushWarning("Texts not found: " + string.Join("; ", _presenter.Unresolved.Concat(_presenter.Battle.Unresolved).Distinct()));
        _overlay.Text = message + "\n\n(click to return to the title)";
        Shot("end");
        if (_options.QuitWhenDone)
        {
            _tree.Quit(failed ? 1 : 0);
            return;
        }
        try
        {
            await KeyAsync();
            Leave(failed);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Leave(bool failed)
    {
        if (_options.QuitWhenDone)
            _tree.Quit(failed ? 1 : 0);
        else
            Screens.ToTitle(this);
    }

    /// <summary>Waits for the next frame; the game stops here once the screen is closed.</summary>
    private async Task NextFrameAsync()
    {
        await ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
        if (_closing)
            throw new OperationCanceledException();
    }

    /// <summary>The files the port does not run yet: skipped, with a note.</summary>
    private sealed class UnportedFiles(GameScreen screen) : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry)
        {
            GD.Print($"Kichikuou: {file} (entry {entry.Value}) is not ported yet and was skipped.");
            screen.Notice($"{file} is not ported yet and was skipped.");
            return Task.CompletedTask;
        }
    }

    // ---------------------------------------------------------------- view

    public async Task ShowAsync(ScriptEvent scriptEvent)
    {
        _presenter.Show(scriptEvent);
        if (scriptEvent is MapScreenEvent { Waiting: true })
            _autoMapWait = true;
        if (_presenter.TakeFade() is { } fade)
            await FadeAsync(fade);
    }

    /// <summary>Animates a palette fade over its ticks × 16 ms (the presenter already holds its end).</summary>
    private async Task FadeAsync(FadeRequest fade)
    {
        double seconds = fade.Ticks * 0.016;
        if (!_options.Fast && seconds > 0 && fade.From != fade.To)
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed.TotalSeconds < seconds)
            {
                double t = watch.Elapsed.TotalSeconds / seconds;
                _presenter.FadeAmount = (byte)Math.Round(fade.From + (fade.To - fade.From) * t);
                _presenter.Invalidate();
                await NextFrameAsync();
            }
        }
        _presenter.FadeAmount = fade.To;
        _presenter.Invalidate();
    }

    private void Shot(string what)
    {
        if (_options.ScreenshotDirectory is not { } dir)
            return;
        System.IO.Directory.CreateDirectory(dir);
        _presenter.Compose();
        var image = Image.CreateFromData(Surface.ScreenWidth * Surface.Scale, Surface.ScreenHeight * Surface.Scale,
            false, Image.Format.Rgba8, _presenter.Screen.ReadScreen());
        image.Resize(Surface.ScreenWidth, Surface.ScreenHeight, Image.Interpolation.Bilinear);
        image.SavePng(System.IO.Path.Combine(dir, $"{_shot++:D5}-{what}.png"));
    }

    // ---------------------------------------------------------------- mouse and keys

    /// <summary>The mouse position in script coordinates (640×400), clamped to the screen.</summary>
    private Vector2I ToScript(Vector2 local)
    {
        var size = _view.Size;
        float scale = Math.Min(size.X / (Surface.ScreenWidth * Surface.Scale), size.Y / (Surface.ScreenHeight * Surface.Scale));
        var drawn = new Vector2(Surface.ScreenWidth, Surface.ScreenHeight) * Surface.Scale * scale;
        var p = (local - _view.Position - (size - drawn) / 2) / (scale * Surface.Scale);
        return new Vector2I(Math.Clamp((int)Math.Floor(p.X), 0, Surface.ScreenWidth - 1),
            Math.Clamp((int)Math.Floor(p.Y), 0, Surface.ScreenHeight - 1));
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseMotion motion:
                _mouse = ToScript(motion.Position);
                MouseMoved();
                break;
            case InputEventMouseButton { ButtonIndex: Godot.MouseButton.Left or Godot.MouseButton.Right } button:
                _mouse = ToScript(button.Position);
                MouseButton(button.ButtonIndex == Godot.MouseButton.Left, button.Pressed);
                break;
        }
        AcceptEvent();
    }

    private void MouseMoved()
    {
        var (x, y) = (_mouse.X, _mouse.Y);
        _presenter.Pointer(x, y);
        if (_sel is not null || _battleMenu is not null)
        {
            _sel?.MouseMove(x, y);
            _battleMenu?.MouseMove(x, y);
            _presenter.Invalidate();
            return;
        }
        if (_presenter.Picker is not null)
            _pointer.Move(x, y);
        if (_leftHeld && _presenter.MapWaiting)
        {
            if (!_dragging && (_mouse - _pressAt).LengthSquared() >= DragThreshold * DragThreshold)
                _dragging = true;
            if (_dragging)
                QueueDrag();
        }
    }

    private void MouseButton(bool left, bool pressed)
    {
        var (x, y) = (_mouse.X, _mouse.Y);
        if (_sel is not null || _battleMenu is not null)
        {
            var which = left ? SelButton.Left : SelButton.Right;
            if (pressed)
            {
                _sel?.MouseDown(which, x, y);
                _battleMenu?.MouseDown(which, x, y);
            }
            else
            {
                _sel?.MouseUp(which, x, y);
                _battleMenu?.MouseUp(which, x, y);
            }
            _presenter.Invalidate();
            if (_sel?.Result is not null || _battleMenu?.IsDone == true)
                _answered?.TrySetResult();
            return;
        }
        if ((!left && !pressed) || _options.Auto)
            return;
        if (_menuAnswer is { } menu)
        {
            if (!pressed)
                return;
            int item = left ? _presenter.MenuHitTest(x, y) + 1 : 0;
            if (!left || item > 0)
                menu.TrySetResult(item);
            return;
        }
        if (_key is not null)
        {
            if (pressed)
                Key(left ? KeyReturn : KeySpace);
            return;
        }
        if (_presenter.Picker is { } picker)
        {
            if (!pressed)
                return;
            if (!left)
            {
                _pointer.RightClick(x, y);
                return;
            }
            var (area, number) = picker.HitTest(x, y);
            var at = area switch
            {
                PickerArea.Cell => PointerScript.PickerCell(number),
                PickerArea.Button => PointerScript.PickerButton(number),
                _ => PointerScript.PickerOutside,
            };
            _pointer.Click(at.X, at.Y);
            return;
        }
        if (_presenter.MapWaiting)
        {
            if (!left)
            {
                _pointer.RightClick(x, y);
                return;
            }
            if (pressed)
            {
                _leftHeld = true;
                _dragging = false;
                _pressAt = _dragFrom = _mouse;
                return;
            }
            if (!_leftHeld)
                return;
            _leftHeld = false;
            if (!_dragging)
                _pointer.Click(_pressAt.X, _pressAt.Y);
            else if (_mouse != _dragFrom)
                _pointer.Drag(_dragFrom.X, _dragFrom.Y, _mouse.X, _mouse.Y);
        }
    }

    /// <summary>While the left button is held on the map, the view follows the pointer one drag at a time.</summary>
    private void QueueDrag()
    {
        if (_pointer.Pending > 0 || _mouse == _dragFrom || !_presenter.MapWaiting)
            return;
        _pointer.Drag(_dragFrom.X, _dragFrom.Y, _mouse.X, _mouse.Y);
        _dragFrom = _mouse;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
            return;
        if (key.Keycode == Godot.Key.Backspace)
        {
            Screens.ToTitle(this);
            return;
        }
        if (_sel is not null || _battleMenu is not null || _options.Auto)
            return;
        bool enter = key.Keycode is Godot.Key.Enter or Godot.Key.KpEnter;
        bool cancel = key.Keycode is Godot.Key.Space or Godot.Key.Escape;
        if (_menuAnswer is { } menu)
        {
            if (cancel)
                menu.TrySetResult(0);
            return;
        }
        if (enter)
            Key(KeyReturn);
        else if (cancel)
            Key(KeySpace);
    }

    private Task<ushort> KeyAsync()
    {
        _key = new TaskCompletionSource<ushort>();
        return _key.Task;
    }

    private void Key(ushort code)
    {
        var key = _key;
        _key = null;
        key?.TrySetResult(code);
    }

    // ---------------------------------------------------------------- input: keys, pauses, menus

    public async Task<Sys3Int> WaitForKeyAsync()
    {
        _pointer.Interrupt();
        _presenter.Flush();
        Shot("key");
        ushort key;
        if (_options.Auto)
        {
            await DelayAsync(_options.Fast ? 0 : 0.8);
            key = KeyReturn;
        }
        else
        {
            key = await KeyAsync();
        }
        _presenter.AfterKey();
        return key;
    }

    public async Task<Sys3Int> PauseAsync(Sys3Int ticks)
    {
        // Y 3: ticks × 16 ms, cut short by a key (which goes to RetVal).
        _pointer.Interrupt();
        _presenter.Flush();
        var key = KeyAsync();
        await DelayAsync(_options.Fast ? 0 : ticks.Value * 0.016, key);
        _key = null;
        return key.IsCompletedSuccessfully ? key.Result : Sys3Int.Zero;
    }

    /// <summary>At least one frame, then until <paramref name="seconds"/> passed or <paramref name="until"/> completed.</summary>
    private async Task DelayAsync(double seconds, Task? until = null)
    {
        var watch = Stopwatch.StartNew();
        do
            await NextFrameAsync();
        while (watch.Elapsed.TotalSeconds < seconds && until is not { IsCompleted: true });
    }

    public async Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
    {
        _pointer.Interrupt();
        _presenter.OpenMenu(items);
        _presenter.Pointer(_mouse.X, _mouse.Y);
        Shot("menu");
        int choice;
        try
        {
            if (_options.Auto)
            {
                await DelayAsync(_options.Fast ? 0 : 0.5);
                choice = AutoChoice(items);
            }
            else
            {
                _menuAnswer = new TaskCompletionSource<int>();
                choice = await _menuAnswer.Task;
            }
        }
        finally
        {
            _menuAnswer = null;
            _presenter.CloseMenu();
        }
        return choice;
    }

    // ---------------------------------------------------------------- input: the pointer

    public async Task<MouseState> GetMouseAsync()
    {
        _presenter.Flush();
        if (_options.Auto)
            AutoPointer();
        var mouse = _pointer.Next(out bool read);
        if (!read)
            await NextFrameAsync();                     // the scripts poll the pointer: one read per frame
        return mouse;
    }

    public Task<Sys3Int> SetCursorAsync(Sys3Int shape)
    {
        var before = (Sys3Int)_cursor;
        _cursor = shape.Value;
        return Task.FromResult(before);
    }

    // ---------------------------------------------------------------- input: the battle

    public async Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
    {
        _pointer.Interrupt();
        _presenter.Flush();
        if (_options.Auto)
        {
            await DelayAsync(_options.Fast ? 0 : 0.5);
            var direct = menu.Find(FigtSelCommand.Direct);
            return direct is { Arguments.Count: > 0 }
                ? new FigtSelChoice(FigtSelCommand.Direct, direct.Arguments[0])
                : new FigtSelChoice(FigtSelCommand.Defend, menu.Find(FigtSelCommand.Defend)!.Arguments[0]);
        }
        _answered = new TaskCompletionSource();
        _sel = new FigtSelScreen(_presenter.Battle, menu);
        _sel.MouseMove(_mouse.X, _mouse.Y);
        _presenter.Invalidate();
        try
        {
            await _answered.Task;
            return _sel.Result!.Value;
        }
        finally
        {
            _sel = null;
            _presenter.Invalidate();
        }
    }

    public async Task<Sys3Int?> ChooseAsync(BattleMenu menu)
    {
        _pointer.Interrupt();
        _presenter.Flush();
        if (_options.Auto)
        {
            await DelayAsync(_options.Fast ? 0 : 0.5);
            return menu.Options[0].Code;
        }
        _answered = new TaskCompletionSource();
        _battleMenu = new BattleMenuScreen(_presenter.Battle, menu);
        _battleMenu.MouseMove(_mouse.X, _mouse.Y);
        _presenter.Invalidate();
        try
        {
            await _answered.Task;
            return _battleMenu.Result;
        }
        finally
        {
            _battleMenu = null;
            _presenter.Invalidate();
        }
    }

    // ---------------------------------------------------------------- input: clock and devices

    /// <summary><c>SR</c>: the CD track playing, which is <c>SS n</c>'s n + 1 (0 when nothing plays).</summary>
    public Task<Sys3Int> GetPlayingTrackAsync() =>
        Task.FromResult(_audio.Track == 0 ? Sys3Int.Zero : (Sys3Int)(ushort)(_audio.Track + 1));

    public Task<IReadOnlyList<Sys3Int>> GetDateAsync()
    {
        var now = DateTime.Now;
        return Task.FromResult<IReadOnlyList<Sys3Int>>([
            (Sys3Int)now.Year, (Sys3Int)now.Month, (Sys3Int)now.Day, (Sys3Int)now.Hour, (Sys3Int)now.Minute,
            (Sys3Int)now.Second, (Sys3Int)((int)now.DayOfWeek + 1),
        ]);
    }

    /// <summary><c>ZT1 n</c>: the timer restarts at n milliseconds (<c>cmdz.c</c> <c>reset_counter(0, 1, n)</c>).</summary>
    public Task ResetTimerAsync(Sys3Int value)
    {
        _timerOffset = value.Value;
        _clock.Restart();
        return Task.CompletedTask;
    }

    /// <summary><c>ZT2</c>-<c>ZT5</c>: the timer in 1/10, 1/30, 1/60 or 1/100 s; read once per frame (the scripts wait on it in loops).</summary>
    public async Task<Sys3Int> GetTimerAsync(int mode)
    {
        await NextFrameAsync();
        long ms = (long)(_clock.Elapsed.TotalMilliseconds * (_options.Fast ? 20 : 1)) + _timerOffset;
        long value = mode switch
        {
            2 => ms / 100,
            3 => ms * 3 / 100,
            4 => ms * 3 / 50,
            _ => ms / 10,
        };
        return (Sys3Int)(ushort)(value & 0xffff);
    }

    /// <summary><c>SC</c>: the CD position; the music is not a CD here, so "nothing plays".</summary>
    public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() =>
        Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)999, (Sys3Int)999, (Sys3Int)999, (Sys3Int)999]);

    /// <summary><c>SR 1</c>: no MIDI plays.</summary>
    public Task<Sys3Int> GetMidiTrackAsync() => Task.FromResult(Sys3Int.Zero);

    // ---------------------------------------------------------------- unattended play (--auto)

    /// <summary>The command menu's items the unattended player opens once each, one a week, before it ends the weeks.</summary>
    private static readonly string[] AutoVisits = ["Harem", "Subordinates", "Thieves' Hideout"];

    private int _autoVisit;
    private int _autoWeek = -1;
    private bool _autoMapWait;

    /// <summary>
    /// The map opens the command menu (one right click each time the map loop starts waiting); a
    /// picker is left with its third button (End, Stop).
    /// </summary>
    private void AutoPointer()
    {
        if (_pointer.Pending > 0)
            return;
        if (_presenter.Picker is not null)
        {
            Shot("picker");
            var (x, y) = PointerScript.PickerButton(3);
            _pointer.Click(x, y);
        }
        else if (_presenter.MapWaiting && _autoMapWait)
        {
            _autoMapWait = false;
            _pointer.RightClick(5, 6);
        }
    }

    /// <summary>The command menu's next item of <see cref="AutoVisits"/> once a week, else its Turn END; elsewhere the first item.</summary>
    private int AutoChoice(IReadOnlyList<SceneMenuItem> items)
    {
        var texts = _presenter.MenuItems ?? [];
        int Find(string text)
        {
            for (int i = 0; i < texts.Count; i++)
                if (texts[i].Contains(text, StringComparison.Ordinal))
                    return i + 1;
            return 0;
        }
        int turnEnd = Find("Turn END");
        if (turnEnd == 0)
            return items.Count > 0 ? 1 : 0;
        int week = _presenter.State?.Week.Value ?? 0;
        if (week != _autoWeek && _autoVisit < AutoVisits.Length && Find(AutoVisits[_autoVisit]) is var visit and > 0)
        {
            _autoWeek = week;
            _autoVisit++;
            return visit;
        }
        return turnEnd;
    }
}
