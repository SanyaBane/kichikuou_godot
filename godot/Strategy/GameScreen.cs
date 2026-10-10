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

    /// <summary>
    /// Development: plays unattended — keys answered, Turn END every week, the battles fought with
    /// the first attack, a defence with the first unit free.
    /// </summary>
    public bool Auto { get; init; }

    /// <summary>Development: unattended play stops at the first command menu after this week.</summary>
    public int AutoWeeks { get; init; } = 30;

    /// <summary>Development: no delays (pauses and fades take one frame, the clock runs 20 times faster).</summary>
    public bool Fast { get; init; }

    /// <summary>Development: quit the program when the game ends (exit code 1 after an error).</summary>
    public bool QuitWhenDone { get; init; }

    /// <summary>Development: save the screen (event CGs masked) as a PNG at every key wait and menu.</summary>
    public string? ScreenshotDirectory { get; init; }

    /// <summary>Development: MsgSkip is on from the start.</summary>
    public bool MsgSkip { get; init; }

    /// <summary>Where the save slots are (default <c>user://saves</c>); a Godot path or a folder.</summary>
    public string? SaveDirectory { get; init; }
}

/// <summary>
/// One game in the original's layout (part G0, owner's decisions 4, 8, 10): a
/// <see cref="GameSession"/> whose view and input are this node behind the proxies, drawn by a
/// <see cref="GamePresenter"/> (its <see cref="GamePresenter.Screen"/> is shown, fades animated) and
/// played with the mouse and keys the way xsystem35 reads them: the input is a state
/// (<see cref="PlayerInput"/>) that <c>IM</c>, <c>Y 3</c>, <c>IK 0</c> and the menus poll once a
/// frame, so the map's clicks and drags and the pickers' hit tests and highlight are the
/// script's own. The battle's input screens work as in <see cref="BattleScreen"/>, fed only the
/// presses that begin while they are open. Over it, xsystem35's menu bar (File, Screen,
/// MsgSkip; docs/window-menu.md) with its hot keys F1 (MsgSkip) and F4 (full screen), and Alt+R for
/// Restart; Backspace returns to the title at once (development).
/// </summary>
public partial class GameScreen : Control, IScriptView, IGameInput
{
    private const ushort KeyReturn = PlayerInput.Return;
    private const ushort KeySpace = PlayerInput.Space;

    private readonly GameOptions _options;
    private readonly PlayerInput _input = new();
    private readonly PointerScript _pointer = new();   // --auto only: the unattended player's gestures
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private SceneTree _tree = null!;
    private GamePresenter _presenter = null!;
    private GodotGlyphRenderer _glyphs = null!;
    private BattleAudio _audio = null!;
    private Control _content = null!;
    private TextureRect _view = null!;
    private ImageTexture? _texture;
    private byte[]? _frame;
    private Label _overlay = null!;
    private Label _notice = null!;
    private double _noticeLeft;
    private bool _closing;
    private bool _ended;
    private int _shot;

    private FigtSelScreen? _sel;
    private BattleMenuScreen? _battleMenu;
    private TaskCompletionSource? _answered;
    private ushort _battleButtons;              // the buttons pressed since the battle's input screen opened

    private MessageSkip _skip = null!;
    private bool _modal;                        // a confirmation box is open: the game waits

    private ushort _ikKey;                      // cmdi.c ik_key, repeating: IK 0's key repeat
    private int _repeating;

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
        _content = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _content.OffsetTop = MenuBarHeight;
        AddChild(_content);
        _view = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _view.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _content.AddChild(_view);
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
        _skip = EngineState.Skip(SaveDirectory);
        if (_options.MsgSkip)
            _skip.Activate(true);
        BuildMenuBar();
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
        _content.AddChild(label);
        return label;
    }

    public override void _ExitTree()
    {
        _closing = true;
        _answered?.TrySetCanceled();
        if (_skip is not null)
            _skip.Changed -= UpdateSkipItem;
        _glyphs?.Dispose();
    }

    public override void _Process(double delta)
    {
        if (_noticeLeft > 0 && (_noticeLeft -= delta) <= 0)
            _notice.Text = "";
        Present();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMWindowFocusOut)
            _input.Clear();
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
            _presenter.Bind(state);
            var saves = new DirectorySaveStore(SaveDirectory);
            var session = new GameSession(state, EngineState.Random, new ViewProxy(this), new GameInputProxy(this),
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
        catch (AutoStopException e)
        {
            GD.Print("Kichikuou: " + e.Message);
            message = e.Message;
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
            await KeyWaitAsync(double.PositiveInfinity, cancelable: true);
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

    /// <summary>The save slots' folder (also of the seen messages, <see cref="MessageSkip"/>).</summary>
    private string SaveDirectory => ProjectSettings.GlobalizePath(_options.SaveDirectory ?? "user://saves");

    /// <summary>Waits for the next frame, and while a confirmation box is open; the game stops here once the screen is closed.</summary>
    private async Task NextFrameAsync()
    {
        do
        {
            await ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            if (_closing)
                throw new OperationCanceledException();
        }
        while (_modal);
    }

    /// <summary>
    /// The files the port does not run yet: skipped, with a note — or, in unattended play, the end
    /// of the run (as a path not ported yet), so <c>--auto</c> plays exactly as far as the port reaches.
    /// </summary>
    private sealed class UnportedFiles(GameScreen screen) : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry)
        {
            if (screen._options.Auto)
                throw new NotSupportedException($"{file} (entry {entry.Value}) is not ported yet.");
            GD.Print($"Kichikuou: {file} (entry {entry.Value}) is not ported yet and was skipped.");
            screen.Notice($"{file} is not ported yet and was skipped.");
            return Task.CompletedTask;
        }
    }

    // ---------------------------------------------------------------- view

    public async Task ShowAsync(ScriptEvent scriptEvent)
    {
        _presenter.Show(scriptEvent);
        if (scriptEvent is MessageEvent message)
            _skip.OnMessage(message.Text);
        else if (scriptEvent is ValueMessageEvent values)
            _skip.OnMessage(values.Text);
        if (scriptEvent is MapScreenEvent { Waiting: true })
        {
            _autoMapWait = true;
            if (_autoDungeon == AutoDungeon.Searching && _presenter.State is { } s)
            {
                _autoDungeon = AutoDungeon.Done;                        // back on the map: the search is over
                GD.Print($"Kichikuou: --auto left the cave: floor {s.DgnRun_Floor.Value} reached, "
                    + $"{(_autoDungeonBattle ? "a battle fought" : "no battle")}, the searcher's status {s.Units[s.Bt_PlayerParty1].Status.Value}.");
            }
        }
        if (scriptEvent is BattleScreenEvent { Open: true } && _autoDungeon == AutoDungeon.Searching)
            _autoDungeonBattle = true;
        if (scriptEvent is EngineEvent { Command: "IZ", Args: [var x, var y] })
            await WarpPointerAsync(x.Value, y.Value);
        if (_presenter.TakeFade() is { } fade)
            await FadeAsync(fade);
    }

    /// <summary>
    /// Animates a palette fade over its <see cref="FadeRequest.Duration"/>, never skipped, unless
    /// Option → MsgSkip Speeds Up Fades cuts it short while skipping (<see cref="MessageSkip.FadeDuration"/>);
    /// the presenter already holds its end.
    /// </summary>
    private async Task FadeAsync(FadeRequest fade)
    {
        double seconds = _skip.FadeDuration(fade.Duration).TotalSeconds;
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
        var p = (local - (_view.GlobalPosition - GlobalPosition) - (size - drawn) / 2) / (scale * Surface.Scale);
        return new Vector2I(Math.Clamp((int)Math.Floor(p.X), 0, Surface.ScreenWidth - 1),
            Math.Clamp((int)Math.Floor(p.Y), 0, Surface.ScreenHeight - 1));
    }

    /// <summary>The centre of script pixel <paramref name="script"/> in this control's coordinates (the inverse of <see cref="ToScript"/>).</summary>
    private Vector2 FromScript(Vector2I script)
    {
        var size = _view.Size;
        float scale = Math.Min(size.X / (Surface.ScreenWidth * Surface.Scale), size.Y / (Surface.ScreenHeight * Surface.Scale));
        var drawn = new Vector2(Surface.ScreenWidth, Surface.ScreenHeight) * Surface.Scale * scale;
        return (_view.GlobalPosition - GlobalPosition) + (size - drawn) / 2 + (new Vector2(script.X, script.Y) + new Vector2(0.5f, 0.5f)) * scale * Surface.Scale;
    }

    /// <summary>
    /// <c>IZ x, y</c> (<c>cmdi.c</c> → <c>ags_setCursorLocation</c>): nothing when the point lies right
    /// of or below the 3200×800 surface, else it is clamped at 0 and taken relative to the <c>WV</c>
    /// view. With Option → Mouse Movement (on, as <c>menu_init</c>) the pointer glides there in
    /// seven steps 7 ms apart on the engine's ease-out curve and lands; off, only the position the
    /// script reads moves and the script waits 50 ms. <c>COMMAND.ADV:618</c> puts the pointer back
    /// where it was before a menu.
    /// </summary>
    private async Task WarpPointerAsync(int x, int y)
    {
        if (x >= 3200 || y >= 800)
            return;
        x = Math.Max(x, 0) - _presenter.Stage.View.X;
        y = Math.Max(y, 0) - _presenter.Stage.View.Y;
        if (!EngineState.MouseMovement || _options.Auto)
        {
            _input.Position = ClampToScreen(x, y);
            await DelayAsync(_options.Fast ? 0 : 0.05);
            return;
        }
        var path = PointerGlide.Path((_input.Position.X, _input.Position.Y), (x, y));
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < path.Count; i++)
        {
            WarpTo(path[i].X, path[i].Y);
            while (i < path.Count - 1 && watch.Elapsed.TotalMilliseconds < (i + 1) * PointerGlide.StepMilliseconds)
                await NextFrameAsync();
        }
    }

    private static Vector2I ClampToScreen(int x, int y) =>
        new(Math.Clamp(x, 0, Surface.ScreenWidth - 1), Math.Clamp(y, 0, Surface.ScreenHeight - 1));

    /// <summary>Moves the system pointer onto script pixel (x, y) and the position the script reads with it.</summary>
    private void WarpTo(int x, int y)
    {
        _input.Position = ClampToScreen(x, y);
        WarpMouse(FromScript(_input.Position));
        PointerMoved();
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseMotion motion:
                _input.Position = ToScript(motion.Position);
                PointerMoved();
                break;
            case InputEventMouseButton { ButtonIndex: Godot.MouseButton.Left or Godot.MouseButton.Right } button:
                _input.Position = ToScript(button.Position);
                ushort code = button.ButtonIndex == Godot.MouseButton.Left ? KeyReturn : KeySpace;
                _input.Button(code, button.Pressed);
                BattleButton(code, button.Pressed);
                break;
        }
        AcceptEvent();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Echo: false } key)
            return;
        if (key.Pressed && key.Keycode == Godot.Key.Backspace)
        {
            Screens.ToTitle(this);
            return;
        }
        if (MenuHotKey(key))
            return;
        _input.Key(key.Keycode, key.Pressed);
    }

    /// <summary>The menus' highlight and the battle's input screens follow the pointer.</summary>
    private void PointerMoved()
    {
        var (x, y) = (_input.Position.X, _input.Position.Y);
        _presenter.Pointer(x, y);
        if (_sel is null && _battleMenu is null)
            return;
        _sel?.MouseMove(x, y);
        _battleMenu?.MouseMove(x, y);
        _presenter.Invalidate();
    }

    /// <summary>
    /// A press or release for the battle's input screens, which are driven by the buttons' changes:
    /// only the presses that began while the screen is open, and their releases.
    /// </summary>
    private void BattleButton(ushort code, bool pressed)
    {
        if (_sel is null && _battleMenu is null)
            return;
        if (pressed)
            _battleButtons |= code;
        else if ((_battleButtons & code) == 0)
            return;
        else
            _battleButtons &= (ushort)~code;
        var (x, y) = (_input.Position.X, _input.Position.Y);
        var which = code == KeyReturn ? SelButton.Left : SelButton.Right;
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
    }

    /// <summary>
    /// xsystem35 <c>sys_keywait</c>: polls the input once a frame until <paramref name="seconds"/>
    /// passed (at least once); <paramref name="cancelable"/> ends it at the first poll that finds
    /// anything held. Returns the last poll. A <paramref name="skippable"/> wait ends as soon as
    /// the message skip is on: while it is, it takes one frame and returns what is held, which
    /// turns the skip off (<c>STOP_ON_CLICK</c>).
    /// </summary>
    private async Task<ushort> KeyWaitAsync(double seconds, bool cancelable, bool skippable = false)
    {
        ushort key = 0;
        if (skippable && _skip.IsSkipping)
        {
            await NextFrameAsync();
            key = _input.ReadInput();
            if (key != 0)
                _skip.Activate(false);
            return key;
        }
        var watch = Stopwatch.StartNew();
        do
        {
            await NextFrameAsync();
            if (skippable && _skip.IsSkipping)
                break;
            key = _input.ReadInput();
            if (cancelable && key != 0)
                break;
        }
        while (watch.Elapsed.TotalSeconds < seconds);
        return key;
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
            key = await HitKeyAsync();
        }
        _presenter.AfterKey();
        return key;
    }

    /// <summary>
    /// <c>IK 0</c> (<c>cmdi.c</c>): waits for anything held and returns it, after the release
    /// for Return and the left button (<c>sys_key_releasewait</c> with the release mask 16). A key
    /// still held from the last <c>IK 0</c> repeats: the second time after 600 ms, then at once.
    /// <c>A</c> (<c>sys_hit_any_key</c>) comes here too: the same wait, without the 600 ms.
    /// </summary>
    private async Task<ushort> HitKeyAsync()
    {
        if (_input.PeekInput() != _ikKey)
            _repeating = 0;
        ushort key = await KeyWaitAsync(double.PositiveInfinity, cancelable: true, skippable: true);
        if (_skip.IsSkipping)
            return 0;
        if (_repeating == 1)
            await KeyWaitAsync(_options.Fast ? 0 : 0.6, cancelable: false);
        if (_repeating < 2)
            _repeating++;
        for (ushort held = key; (held & KeyReturn) != 0;)
            held = await KeyWaitAsync(0.05, cancelable: false);
        _ikKey = key;
        return key;
    }

    public async Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
    {
        // Y 3 (cmdy.c): 0 reads the input once; the core keeps Y3waitFlags (Y 3, 10000 / 10001
        // never come here) and passes it as cancelable.
        _pointer.Interrupt();
        _presenter.Flush();
        double seconds = _options.Fast ? 0 : ticks.Value * 0.016;
        if (_options.Auto)
        {
            await DelayAsync(seconds);
            return Sys3Int.Zero;
        }
        if (ticks.Value == 0)
        {
            await NextFrameAsync();
            return _input.ReadInput();
        }
        // ticks × 16 ms, polled once a frame; when cancelable, anything held ends it at once; the last poll is returned
        // (nothing is consumed: a button held over several pauses ends each of them).
        return await KeyWaitAsync(seconds, cancelable, skippable: true);
    }

    /// <summary>At least one frame, then until <paramref name="seconds"/> passed.</summary>
    private async Task DelayAsync(double seconds)
    {
        var watch = Stopwatch.StartNew();
        do
            await NextFrameAsync();
        while (watch.Elapsed.TotalSeconds < seconds);
    }

    public async Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
    {
        _pointer.Interrupt();
        _skip.OnMenu();
        _presenter.OpenMenu(items);
        _presenter.Pointer(_input.Position.X, _input.Position.Y);
        Shot("menu");
        try
        {
            if (_options.Auto)
            {
                await DelayAsync(_options.Fast ? 0 : 0.5);
                return AutoChoice(items);
            }
            return await MenuAsync();
        }
        finally
        {
            _presenter.CloseMenu();
        }
    }

    /// <summary>
    /// The menu's input as xsystem35's <c>sel_select</c> (<c>selection.c</c>) reads it: first until
    /// nothing is held, then once a frame — Space alone (the right button) cancels at once, the
    /// release of Return alone (the left button) over an item chooses it. The arrow keys are not
    /// supported (the highlight follows the pointer only).
    /// </summary>
    private async Task<int> MenuAsync()
    {
        for (ushort held = KeyReturn; held != 0;)
            held = await KeyWaitAsync(0.05, cancelable: false);
        ushort previous = 0;
        int item = -1;
        while (true)
        {
            await NextFrameAsync();
            ushort key = _input.ReadInput();
            if (key == KeySpace)
                return 0;
            if (key == 0 && previous == KeyReturn && item >= 0)
                return item + 1;
            previous = key;
            if ((key & 0xf) == 0)
                item = _presenter.MenuHitTest(_input.Position.X, _input.Position.Y);
        }
    }

    // ---------------------------------------------------------------- input: the pointer

    public async Task<MouseState> GetMouseAsync()
    {
        _presenter.Flush();
        if (_options.Auto)
        {
            AutoPointer();
            var queued = _pointer.Next(out bool read);
            if (!read)
                await NextFrameAsync();                 // the scripts poll the pointer: one read per frame
            return queued;
        }
        await NextFrameAsync();
        return new MouseState((Sys3Int)_input.ReadMouse(), (Sys3Int)_input.Position.X, (Sys3Int)_input.Position.Y);
    }

    /// <summary>
    /// <c>IC</c>: returns the shape set before and shows the new one as a system cursor
    /// (<c>cursor.c</c>: 6 is the four-way arrow of the map's drag); a shape the engine has no
    /// cursor for leaves the cursor as it is.
    /// </summary>
    public Task<Sys3Int> SetCursorAsync(Sys3Int shape)
    {
        var before = (Sys3Int)_cursor;
        _cursor = shape.Value;
        CursorShape? cursor = shape.Value switch
        {
            1 or 4 or 12 => CursorShape.Arrow,
            2 => CursorShape.Cross,
            3 => CursorShape.Ibeam,
            5 => CursorShape.Forbidden,
            6 or 7 => CursorShape.Move,
            8 => CursorShape.Bdiagsize,
            9 => CursorShape.Vsize,
            10 => CursorShape.Fdiagsize,
            11 => CursorShape.Hsize,
            13 => CursorShape.Wait,
            _ => null,
        };
        if (cursor is { } c && !_options.Auto)
            MouseDefaultCursorShape = c;
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
        _battleButtons = 0;
        _sel = new FigtSelScreen(_presenter.Battle, menu);
        _sel.MouseMove(_input.Position.X, _input.Position.Y);
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
        _battleButtons = 0;
        _skip.OnMenu();
        _battleMenu = new BattleMenuScreen(_presenter.Battle, menu);
        _battleMenu.MouseMove(_input.Position.X, _input.Position.Y);
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

    /// <summary>
    /// <c>SC</c> (<c>cmds.c</c>, <c>cdrom.mp3.c</c>): the track <c>SS</c> started and the time since,
    /// as minutes, seconds and 1/75 s frames, while it plays; 999 four times when nothing does.
    /// The opening waits on it for its first 25 frames (<c>ｵｰﾌﾟﾆﾝｸﾞ.ADV:17-31</c>) and times every
    /// picture from there, so the music and the pictures keep step. Read once per frame (the
    /// script polls it in a loop).
    /// </summary>
    public async Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync()
    {
        await NextFrameAsync();
        if (_audio.MusicPosition is not { } position)
            return [(Sys3Int)999, (Sys3Int)999, (Sys3Int)999, (Sys3Int)999];
        long ms = (long)position.TotalMilliseconds;
        return [(Sys3Int)_audio.Track, (Sys3Int)(int)(ms / 60000), (Sys3Int)(int)(ms / 1000 % 60), (Sys3Int)(int)(ms % 1000 * 75 / 1000)];
    }

    /// <summary><c>SR 1</c>: no MIDI plays.</summary>
    public Task<Sys3Int> GetMidiTrackAsync() => Task.FromResult(Sys3Int.Zero);

    // ---------------------------------------------------------------- unattended play (--auto)

    /// <summary>
    /// The command menu's items the unattended player opens once each, one a week, before it ends the
    /// weeks: the prologue's three, then Treasure and All Data once the Leazas menu offers them.
    /// </summary>
    private static readonly string[] AutoVisits = ["Harem", "Subordinates", "Thieves' Hideout", "Treasure", "All Data"];

    private int _autoVisit;
    private int _autoWeek = -1;
    private bool _autoMapWait;

    /// <summary>All Data's graphs the unattended player has opened this visit (1-4, then Return).</summary>
    private int _autoGraph;

    /// <summary>Whether the unattended player asked All Data for a scale once (a right click on the first graph).</summary>
    private bool _autoScaled;

    /// <summary>The deployment picker the unattended player sent its party from (a second visit means Go was refused).</summary>
    private PickerView? _autoSentFrom;

    /// <summary>The unattended player stopped a deployment picker: the next menu abandons the city.</summary>
    private bool _autoAbandon;

    /// <summary>The item screen the unattended player works in, and its steps left (<see cref="AutoItemSteps"/>).</summary>
    private PickerView? _autoItemsFrom;

    private Queue<Action> _autoItemSteps = new();

    /// <summary>The dungeon the unattended player searches once at stage 1: the Monsters' Cave.</summary>
    private const int AutoCave = 2;

    /// <summary>Where the unattended player's one dungeon search stands.</summary>
    private enum AutoDungeon { NotYet, Searching, Done }

    private AutoDungeon _autoDungeon;

    /// <summary>Whether the search has fought a battle.</summary>
    private bool _autoDungeonBattle;

    /// <summary>Whether the search's Start Search was chosen (the dungeon's menu again means the picker came back empty).</summary>
    private bool _autoDungeonStarted;

    /// <summary>The end of unattended play (<see cref="GameOptions.AutoWeeks"/>).</summary>
    private sealed class AutoStopException(string message) : Exception(message);

    /// <summary>
    /// The map opens the command menu (one right click each time the map loop starts waiting), or once
    /// the Monsters' Cave with a left click on it (<see cref="AutoCaveOnScreen"/>); the
    /// deployment picker takes the first unit free (status 1) and sends it with the first button,
    /// or stops (third button) when nobody is free or the sending was refused; the item screen
    /// takes <see cref="AutoItemSteps"/>, then ends (third button); any other picker is left
    /// with its third button (End, Stop). All Data's first graph gets a right
    /// click (the scale menu), every graph then a left one (back to the menu).
    /// </summary>
    private void AutoPointer()
    {
        if (_pointer.Pending > 0)
            return;
        if (_presenter.Data is not (DataView.Closed or DataView.Map))
        {
            Shot("data");
            if (_autoScaled)
            {
                _pointer.Click(320, 200);
            }
            else
            {
                _autoScaled = true;
                _pointer.RightClick(320, 200);
            }
            return;
        }
        if (_presenter.Picker is { Kind: PickerKind.Items } items && _presenter.State is { } state)
        {
            Shot("picker");
            if (_autoItemsFrom != items)
            {
                _autoItemsFrom = items;
                _autoItemSteps = AutoItemSteps(state);
            }
            if (_autoItemSteps.TryDequeue(out var step))
            {
                step();
            }
            else
            {
                var end = PointerScript.PickerButton(3);
                _pointer.Click(end.X, end.Y);
            }
            return;
        }
        if (_presenter.Picker is { } picker)
        {
            Shot("picker");
            var (x, y) = PointerScript.PickerButton(3);
            if (picker.Kind == PickerKind.Deployment && _presenter.State is { } s)
            {
                int free = Enumerable.Range(1, 50).FirstOrDefault(u => s.Units[u].Status == 1);
                if (_autoSentFrom == picker || (s.Bt_PlayerParty1 == 0 && free == 0))
                {
                    _autoSentFrom = null;
                    _autoAbandon = true;
                }
                else if (s.Bt_PlayerParty1 != 0)
                {
                    (x, y) = PointerScript.PickerButton(1);
                    _autoSentFrom = picker;
                }
                else
                {
                    (x, y) = PointerScript.PickerCell(free);
                }
            }
            _pointer.Click(x, y);
        }
        else if (_presenter.MapWaiting && _autoMapWait)
        {
            _autoMapWait = false;
            if (AutoCaveOnScreen() is var (x, y))
            {
                _autoDungeon = AutoDungeon.Searching;
                _pointer.Click(x, y);
            }
            else
            {
                _pointer.RightClick(5, 6);
            }
        }
    }

    /// <summary>
    /// Where the Monsters' Cave is on the screen when the unattended player is to search it: once, at
    /// the first map wait of stage 1 that finds the week's dungeon action free and the cave's province
    /// the player's (<c>COMMAND.ADV:145-158</c>), before the menu's visits of the stage (the castle may
    /// fall in a few weeks); null otherwise or out of view. The map wait's first read still has the
    /// camera as the centre (<c>COMMAND.ADV:50-58</c>), on Leazas Castle (<c>:4-13</c>).
    /// </summary>
    private (int X, int Y)? AutoCaveOnScreen()
    {
        if (_autoDungeon != AutoDungeon.NotYet || _presenter.State is not { } s || s.StoryStage != 1
            || s.Ui_CmdDungeonAvail != 1 || s.Provs[s.Dgns[AutoCave].Prov].Owner != 1)
            return null;
        if (PointerScript.OnScreen(s, s.Dgns[AutoCave].MapX, s.Dgns[AutoCave].MapY) is not { } at)
            return null;
        GD.Print($"Kichikuou: --auto searches the Monsters' Cave (week {s.Week.Value}).");
        return at;
    }

    /// <summary>
    /// What the unattended player does on the item screen, one step each time the pointer has
    /// nothing queued (a step without a gesture lets the screen be drawn and dumped): an entry the
    /// screen takes without a refusal or a question (<c>Sｱｲﾃﾑ.ADV:240-375</c>) is hovered and
    /// picked — in the unit view a unit at status 1 or 2 not holding item 1 or 2, in the item view
    /// an item past 2 in stock with an effect, else the view is turned to the units first and back
    /// after — its prompt is looked at, the pick is dropped with a right click (998, which turns the
    /// view back), then End. Nothing is equipped.
    /// </summary>
    private Queue<Action> AutoItemSteps(GameState s)
    {
        int unit = Enumerable.Range(1, 50).FirstOrDefault(id => s.Units[id].Status.Value is 1 or 2 && s.Units[id].Item.Value is not (1 or 2));
        int item = Enumerable.Range(3, 48).FirstOrDefault(id => s.Items[id].Holder == 100 && s.Items[id].Effect != 0);
        var steps = new Queue<Action>();
        void Click((int X, int Y) at) => _pointer.Click(at.X, at.Y);
        void Pick(int id)
        {
            var at = PointerScript.PickerCell(id);
            steps.Enqueue(() =>
            {
                _pointer.Move(at.X, at.Y);                              // its details,
                _pointer.Click(at.X, at.Y);                             // the pick
            });
            steps.Enqueue(() => { });                                   // its prompt drawn
            steps.Enqueue(() => _pointer.RightClick(at.X, at.Y));      // dropped
            steps.Enqueue(() => { });
        }
        if (s.Ui_ItemViewMode == 1 && item != 0)
        {
            Pick(item);
        }
        else if (s.Ui_ItemViewMode.Value is 0 or 1 && unit != 0)
        {
            bool turn = s.Ui_ItemViewMode == 1;
            if (turn)
                steps.Enqueue(() => Click(PointerScript.PickerButton(2)));
            Pick(unit);
            if (turn)
                steps.Enqueue(() => Click(PointerScript.PickerButton(2)));
        }
        return steps;
    }

    /// <summary>
    /// The command menu's next item of <see cref="AutoVisits"/> once a week, else its Turn END
    /// (past <see cref="GameOptions.AutoWeeks"/> the run stops there); a defence's menu after a
    /// stopped deployment picker abandons the city; All Data's menu each graph in turn, then Return;
    /// the dungeon search's menus a few floors down with a battle, then out; elsewhere the first item.
    /// </summary>
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
        {
            if (_autoAbandon && Find("Abandon City") is var abandon and > 0)
            {
                _autoAbandon = false;
                return abandon;
            }
            if (Find("All Funds") > 0 && Find("Return") is var back and > 0)
            {
                // All Data's menu: each graph once, then Return.
                if (++_autoGraph <= 4)
                    return _autoGraph;
                _autoGraph = 0;
                return back;
            }
            if (_autoDungeon == AutoDungeon.Searching && _presenter.State is { } d)
            {
                // The search: Start Search once (the dungeon's menu again means nobody went: Return);
                // down until a battle was fought on floor 3 or deeper — between floors 3 and 4 until
                // one is, Marder waits on 5 — then Retreat and Finish.
                if (Find("Start Search") is var start and > 0)
                {
                    bool again = _autoDungeonStarted;
                    _autoDungeonStarted = true;
                    return again ? Find("Return") : start;
                }
                if (Find("Retreat") is var retreat and > 0)
                {
                    if (_autoDungeonBattle && d.DgnRun_Floor >= 3)
                        return retreat;
                    return d.DgnRun_Floor < 4 ? Find("Descend") : Find("Ascend");
                }
                if (Find("Finish") is var finish and > 0)
                    return finish;
            }
            return items.Count > 0 ? 1 : 0;
        }
        int week = _presenter.State?.Week.Value ?? 0;
        if (week > _options.AutoWeeks)
            throw new AutoStopException($"--auto stopped after week {_options.AutoWeeks} (week {week}).");
        if (week != _autoWeek && _autoVisit < AutoVisits.Length && Find(AutoVisits[_autoVisit]) is var visit and > 0)
        {
            _autoWeek = week;
            _autoVisit++;
            return visit;
        }
        return turnEnd;
    }
}
