using Godot;
using Kichikuou.Content;
using Kichikuou.Content.Battles;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Golden;
using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;
using SelButton = Kichikuou.Content.Battles.MouseButton;

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
/// The battle screen in the original's layout at twice its size (docs/battle/screen-layout.md):
/// <see cref="BattlePresenter"/> draws every core event on the emulated engine surface, this
/// node shows the surface's screen corner, plays the sounds and turns mouse and keys into the
/// battle's inputs. Backspace returns to the capture list.
/// </summary>
public partial class BattleScreen : Control, IScriptView, IBattleInput
{
    private const ushort KeyReturn = 16;   // xsystem35 SYS35KEY_RET (left click, Enter)
    private const ushort KeySpace = 32;    // SYS35KEY_SPC (right click, Space, Escape)

    /// <summary>How long a recording rests at a key wait before it goes on by itself.</summary>
    private const double WatchKeySeconds = 1.6;

    private readonly string _capturePath;
    private readonly BattleMode _mode;
    private BattlePresenter _presenter = null!;
    private GodotGlyphRenderer _glyphs = null!;
    private BattleAudio _audio = null!;
    private TextureRect _view = null!;
    private ImageTexture? _texture;
    private byte[]? _frame;
    private int _shownVersion = -1;
    private Label _overlay = null!;
    private TaskCompletionSource<ushort>? _key;
    private FigtSelScreen? _sel;
    private BattleMenuScreen? _menu;
    private TaskCompletionSource? _answered;
    private Vector2I _mouse;
    private bool _closing;
    private int _shot;

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

    /// <summary>Development: save the screen as a PNG into this folder at every key wait.</summary>
    public string? ScreenshotDirectory { get; init; }

    public override void _Ready()
    {
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
        _overlay = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize, 16);
        _overlay.AddThemeColorOverride("font_outline_color", Colors.Black);
        _overlay.AddThemeConstantOverride("outline_size", 6);
        AddChild(_overlay);

        _audio = new BattleAudio();
        AddChild(_audio);
        _glyphs = new GodotGlyphRenderer();
        _presenter = new BattlePresenter(new ScenarioText(GameData.ScenarioDirectory), new CgLibrary(GameData.CgDirectory),
            _glyphs, _audio);
        _ = RunAsync();
    }

    public override void _ExitTree()
    {
        _closing = true;
        _key?.TrySetCanceled();
        _answered?.TrySetCanceled();
        _glyphs?.Dispose();
    }

    public override void _Process(double delta) => Present();

    /// <summary>Uploads the surface's screen when it changed.</summary>
    private void Present()
    {
        var surface = _presenter.Surface;
        if (surface.ScreenVersion == _shownVersion)
            return;
        _shownVersion = surface.ScreenVersion;
        _frame = surface.ReadScreen(_frame);
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

    // ---------------------------------------------------------------- running

    private async Task RunAsync()
    {
        string end;
        try
        {
            var capture = await Task.Run(() => GoldenCapture.Load(_capturePath));
            if (_mode == BattleMode.Watch)
            {
                var report = await GoldenReplay.RunAsync(capture, view: new WatchView(this), started: _presenter.Start);
                end = report.Passed ? "End of the recording." : "End: " + report.Describe(10);
            }
            else
            {
                var (state, jump) = await BattleSandbox.RunLiveAsync(capture, this, this, (uint)System.Environment.TickCount,
                    file =>
                    {
                        GD.Print($"The story scene {file} cannot run yet and was skipped.");
                        return Task.CompletedTask;
                    },
                    started: _presenter.Start);
                end = jump is not null ? $"The battle ended with a jump to {jump} (game over)."
                    : state.Bt_Result == 2 ? "Victory." : "Defeat.";
            }
        }
        catch (TaskCanceledException) when (_closing)
        {
            return;
        }
        catch (Exception e)
        {
            GD.PushError(e.ToString());
            end = "Error: " + e.Message;
        }
        _presenter.Flush();
        Present();
        if (_presenter.Unresolved.Count > 0)
            GD.PushWarning("Texts not found: " + string.Join("; ", _presenter.Unresolved.Distinct()));
        GD.Print(end);
        _overlay.Text = end + "  (click to return)";
        if (QuitWhenDone)
        {
            GetTree().Quit();
            return;
        }
        try
        {
            await KeyAsync();
            Screens.Open(this, new CaptureList());
        }
        catch (TaskCanceledException)
        {
        }
    }

    // ---------------------------------------------------------------- view

    public Task ShowAsync(ScriptEvent scriptEvent)
    {
        _presenter.Show(scriptEvent);
        return Task.CompletedTask;
    }

    /// <summary>
    /// The view for a recording: the same drawing; pauses take their time, and where the
    /// original waits for a key the playback rests a moment (a click goes on at once).
    /// </summary>
    private sealed class WatchView(BattleScreen screen) : IScriptView
    {
        public async Task ShowAsync(ScriptEvent scriptEvent)
        {
            var presenter = screen._presenter;
            presenter.Show(scriptEvent);
            if (scriptEvent is PauseEvent p)
            {
                presenter.Flush();
                if (p.Ticks.Value >= 10)
                    screen.Screenshot();
                if (!screen.Fast)
                    await screen.DelayAsync(p.Ticks.Value * 0.016, skippable: false);
            }
            if (presenter.WaitsForKey)
            {
                presenter.Flush();
                screen.Screenshot();
                if (!screen.Fast)
                    await screen.DelayAsync(WatchKeySeconds, skippable: true);
                presenter.AfterKey();
            }
        }
    }

    private async Task DelayAsync(double seconds, bool skippable)
    {
        var timer = GetTree().CreateTimer(seconds);
        var key = skippable ? KeyAsync() : null;
        while (timer.TimeLeft > 0 && key is not { IsCompleted: true })
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _key = null;
    }

    private void Screenshot()
    {
        if (ScreenshotDirectory is null)
            return;
        System.IO.Directory.CreateDirectory(ScreenshotDirectory);
        var frame = _presenter.Surface.ReadScreen();
        var image = Image.CreateFromData(Surface.ScreenWidth * Surface.Scale, Surface.ScreenHeight * Surface.Scale,
            false, Image.Format.Rgba8, frame);
        image.SavePng(System.IO.Path.Combine(ScreenshotDirectory, $"{_shot++:D4}.png"));
    }

    // ---------------------------------------------------------------- input

    private Task<ushort> KeyAsync()
    {
        _key = new TaskCompletionSource<ushort>();
        return _key.Task;
    }

    /// <summary>The mouse position in script coordinates (640×400).</summary>
    private Vector2I ToScript(Vector2 local)
    {
        var size = _view.Size;
        float scale = Math.Min(size.X / (Surface.ScreenWidth * Surface.Scale), size.Y / (Surface.ScreenHeight * Surface.Scale));
        var drawn = new Vector2(Surface.ScreenWidth, Surface.ScreenHeight) * Surface.Scale * scale;
        var p = (local - _view.Position - (size - drawn) / 2) / (scale * Surface.Scale);
        return new Vector2I((int)Math.Floor(p.X), (int)Math.Floor(p.Y));
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseMotion motion:
                _mouse = ToScript(motion.Position);
                _sel?.MouseMove(_mouse.X, _mouse.Y);
                _menu?.MouseMove(_mouse.X, _mouse.Y);
                break;
            case InputEventMouseButton { ButtonIndex: Godot.MouseButton.Left or Godot.MouseButton.Right } button:
                _mouse = ToScript(button.Position);
                var which = button.ButtonIndex == Godot.MouseButton.Left ? SelButton.Left : SelButton.Right;
                if (_sel is not null || _menu is not null)
                {
                    if (button.Pressed)
                    {
                        _sel?.MouseDown(which, _mouse.X, _mouse.Y);
                        _menu?.MouseDown(which, _mouse.X, _mouse.Y);
                    }
                    else
                    {
                        _sel?.MouseUp(which, _mouse.X, _mouse.Y);
                        _menu?.MouseUp(which, _mouse.X, _mouse.Y);
                    }
                    CheckAnswered();
                }
                else if (button.Pressed)
                {
                    Key(which == SelButton.Left ? KeyReturn : KeySpace);
                }
                break;
        }
        AcceptEvent();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
            return;
        if (key.Keycode == Godot.Key.Backspace)
        {
            Screens.Open(this, new CaptureList());
            return;
        }
        if (_sel is not null || _menu is not null)
            return;
        if (key.Keycode is Godot.Key.Enter or Godot.Key.KpEnter)
            Key(KeyReturn);
        else if (key.Keycode is Godot.Key.Space or Godot.Key.Escape)
            Key(KeySpace);
    }

    private void Key(ushort code)
    {
        var key = _key;
        _key = null;
        key?.TrySetResult(code);
    }

    private void CheckAnswered()
    {
        if (_sel?.Result is not null || _menu?.IsDone == true)
            _answered?.TrySetResult();
    }

    public async Task<Sys3Int> WaitForKeyAsync()
    {
        _presenter.Flush();
        Screenshot();
        ushort key = await KeyAsync();
        _presenter.AfterKey();
        return key;
    }

    public async Task<Sys3Int> PauseAsync(Sys3Int ticks)
    {
        // Y 3: ticks × 16 ms, cut short by a key (which goes to RetVal).
        _presenter.Flush();
        var timer = GetTree().CreateTimer(ticks.Value * 0.016);
        var key = KeyAsync();
        while (!key.IsCompleted && timer.TimeLeft > 0)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _key = null;
        return key.IsCompletedSuccessfully ? key.Result : (Sys3Int)0;
    }

    public async Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
    {
        _presenter.Flush();
        _answered = new TaskCompletionSource();
        _sel = new FigtSelScreen(_presenter, menu);
        _sel.MouseMove(_mouse.X, _mouse.Y);
        await _answered.Task;
        var result = _sel.Result!.Value;
        _sel = null;
        return result;
    }

    public async Task<Sys3Int?> ChooseAsync(BattleMenu menu)
    {
        _presenter.Flush();
        _answered = new TaskCompletionSource();
        _menu = new BattleMenuScreen(_presenter, menu);
        _menu.MouseMove(_mouse.X, _mouse.Y);
        await _answered.Task;
        var result = _menu.Result;
        _menu = null;
        return result;
    }

    /// <summary><c>SR</c>: the CD track playing, which is <c>SS n</c>'s n + 1 (0 when nothing plays).</summary>
    public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult(_audio.Track == 0 ? Sys3Int.Zero : (Sys3Int)(ushort)(_audio.Track + 1));
}
