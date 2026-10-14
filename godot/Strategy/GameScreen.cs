// Parts translated from xsystem35 (https://github.com/kichikuou/xsystem35-sdl2): cmdi.c (IK 0),
// input.c (sys_keywait) and selection.c (the selection loop), Copyright (C) 1997-1998 Masaki Chikama
// (Wren); cursor.c (the cursor shapes), Copyright (C) 2000- TAJIRI Yasuhiro. GNU General Public
// License, version 2 or later, as KichikuouGodot (LICENSE).

using System.Diagnostics;
using Godot;
using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using SelButton = Kichikuou.Original.Content.Battles.MouseButton;

namespace Kichikuou.Game.Strategy;

/// <summary>How a game starts.</summary>
public enum GameStart
{
    /// <summary>The title (<c>ｽﾀMENU</c> from the player's files, owner's decision 19), then the game started there.</summary>
    Title,

    /// <summary>Development: the new game at once, then the week loop.</summary>
    New,

    /// <summary>Development: the save screen's load list at once (backing out returns to the title).</summary>
    Load,

    /// <summary>Development: an ending's <c>OVER</c> on the booted game (<see cref="GameOptions.Ending"/>).</summary>
    Ending,
}

/// <summary>Where unattended play stops early (<c>--until</c>).</summary>
public enum AutoUntil
{
    /// <summary>Not early: at the weeks or the stage.</summary>
    None,

    /// <summary>When the first deployment picker closes.</summary>
    Deployment,

    /// <summary>When the first battle ends.</summary>
    Battle,
}

/// <summary>What a <see cref="GameScreen"/> plays and how (the development switches of <see cref="Main"/>).</summary>
public sealed record GameOptions
{
    /// <summary>The title, or (development) a new game, a load or an ending at once.</summary>
    public GameStart Start { get; init; }

    /// <summary>Development: the ending <see cref="GameStart.Ending"/> shows (1-14).</summary>
    public int Ending { get; init; }

    /// <summary>
    /// Development: plays unattended — keys answered, Turn END every week, the battles fought with
    /// the first attack, from stage 1 a replenish and an attack on the rebels once a week, a strong
    /// party sent.
    /// </summary>
    public bool Auto { get; init; }

    /// <summary>Development: unattended play stops at the first command menu after this week.</summary>
    public int AutoWeeks { get; init; } = 30;

    /// <summary>
    /// Development: unattended play stops at the command menu two weeks after the week this story
    /// stage was reached, so the stage's first whole week is played (0: never).
    /// </summary>
    public int AutoStage { get; init; }

    /// <summary>Development: the unattended player ends every week at once — no visits, replenishes, attacks or searches.</summary>
    public bool AutoTurnEnd { get; init; }

    /// <summary>
    /// Development: the units the unattended player sends from a deployment picker, those of them
    /// free (status 1), up to the places it offers; its own choice when none of them is free.
    /// </summary>
    public IReadOnlyList<int>? AutoParty { get; init; }

    /// <summary>Development: unattended play stops after the first deployment picker or the first battle (a look at one screen).</summary>
    public AutoUntil AutoUntil { get; init; }

    /// <summary>
    /// Development: a folder whose save slots are copied into <see cref="SaveDirectory"/> before the
    /// game starts, so a kept save (e.g. the first Leazas week) is loaded again and again unchanged.
    /// </summary>
    public string? SlotsFrom { get; init; }

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

    /// <summary>
    /// Development: Debug → Portraits opens once the game has booted; with <see cref="ScreenshotDirectory"/>
    /// it saves the units' list there (<see cref="PortraitViewer.SaveUnitsAsync"/>), then the program ends with <see cref="QuitWhenDone"/>.
    /// </summary>
    public bool Portraits { get; init; }
}

/// <summary>
/// One game in the original's layout (part G0, owner's decisions 4, 8, 10): a
/// <see cref="GameSession"/> whose view and input are this node behind the proxies, drawn by a
/// <see cref="GamePresenter"/> (its <see cref="GamePresenter.Screen"/> is shown, fades animated) and
/// played with the mouse and keys the way xsystem35 reads them: the input is a state
/// (<see cref="PlayerInput"/>) that <c>IM</c>, <c>Y 3</c>, <c>IK 0</c> and the menus poll once a
/// frame, so the map's clicks and drags and the pickers' hit tests and highlight are the
/// script's own. The battle's input screens work as in <see cref="BattleScreen"/>, fed only the
/// presses that begin while they are open. With Option → Interface on New our own deployment screen
/// covers <c>Sﾌﾞﾀｲ</c>'s but the portrait (<see cref="UpdateDeploymentScreen"/>, docs/ui/deployment-table.md) and
/// answers the script with its gestures. Over it, xsystem35's menu bar (File, Screen,
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

    /// <summary>The screen changed while our battle screen hid the view (<see cref="Present"/>).</summary>
    private bool _viewStale;
    private Label _overlay = null!;
    private Label _notice = null!;
    private double _noticeLeft;
    private bool _closing;
    private int _shot;

    private FigtSelScreen? _sel;
    private BattleMenuScreen? _battleMenu;
    private TaskCompletionSource? _answered;
    private ushort _battleButtons;              // the buttons pressed since the battle's input screen opened

    private MessageSkip _skip = null!;
    private bool _modal;                        // a confirmation box is open: the game waits
    private bool _portraitsOpened;              // GameOptions.Portraits: Debug → Portraits was opened

    private ushort _ikKey;                      // cmdi.c ik_key, repeating: IK 0's key repeat
    private int _repeating;

    private int _cursor = 1;
    private readonly EngineTimer _timer;

    private SceneRunner? _scenes;                       // the session's, for the scenes running now

    private DeploymentTableView _table = null!;         // our own screen over Sﾌﾞﾀｲ's grid (Option → Interface: New)
    private BattleBoardView _battleView = null!;        // our own battle screen over the stage (Option → Interface: New)
    private DeploymentPanels _panels = null!;           // and over the rest of it but the portrait
    private PointerScript _tablePointer = new();        // the script's pointer while the player's is over the table
    private IReadOnlyList<int>? _wantedOrder;           // the party a drop asked for, while its clicks run
    private readonly Stopwatch _skipYield = Stopwatch.StartNew();
    private Stopwatch? _sceneSkip;                      // since the message skip began to skip a scene whole
    private bool _stageSkipped;                         // a skipped scene's stage commands were dropped: clear the stage after it

    /// <summary>For the engine; screens are created with the other constructor.</summary>
    public GameScreen() : this(new GameOptions())
    {
    }

    public GameScreen(GameOptions options)
    {
        _options = options;
        _timer = new EngineTimer { Speed = options.Fast ? 20 : 1 };
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
        var faces = new FacePictures(new CgLibrary(GameData.CgDirectory));
        _battleView = new BattleBoardView(GodotGlyphRenderer.Chosen, faces);
        _content.AddChild(_battleView);
        _panels = new DeploymentPanels(GodotGlyphRenderer.Chosen, faces);
        _panels.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _panels.RemovePressed += TableRowPressed;
        _panels.ButtonPressed += TableButtonPressed;
        _panels.RightPressed += TableRightPressed;
        _panels.Dropped += PartyDropped;
        _content.AddChild(_panels);
        _table = new DeploymentTableView(GodotGlyphRenderer.Chosen);
        _table.RowPressed += TableRowPressed;
        _table.RightPressed += TableRightPressed;
        _content.AddChild(_table);
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
        UpdateDeploymentScreen();
        UpdateBattleScreen(Present());
        if (_options.Portraits && !_portraitsOpened && _presenter.State is not null && !_modal)
        {
            _portraitsOpened = true;
            OpenPortraits();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMWindowFocusOut)
            _input.Clear();
    }

    /// <summary>
    /// Composes the screen and uploads it when it changed; nothing while a scene is skipped whole
    /// (<see cref="SkippingScene"/>); neither while our battle screen covers the view
    /// (<see cref="BattleScreenShown"/>: it composes the screen for its windows' layer itself), the
    /// change uploaded when the view shows again. Returns whether the screen changed.
    /// </summary>
    private bool Present()
    {
        if (SkippingScene)
            return false;
        if (_sceneSkip is { } skip)
        {
            GD.Print($"Kichikuou: MsgSkip skipped the scene in {skip.ElapsedMilliseconds} ms.");
            _sceneSkip = null;
        }
        ClearSkippedStage();
        if (BattleScreenShown)
        {
            bool covered = _presenter.TakeChange();                            // our battle screen covers the view: it composes for its windows,
            _viewStale |= covered;                                             // the view is uploaded when it shows again
            return covered;
        }
        bool changed = _presenter.Compose();
        if (!changed && !_viewStale)
            return false;
        _viewStale = false;
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
        return true;
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
            if (_options.SlotsFrom is { } from)
                CopySlots(ProjectSettings.GlobalizePath(from), SaveDirectory);
            var state = new GameState();
            _presenter.Bind(state);
            var saves = new DirectorySaveStore(SaveDirectory);
            var session = new GameSession(state, EngineState.Random, new ViewProxy(this), new GameInputProxy(this),
                scripts, scripts, saves, new UnportedFiles(this));
            _scenes = session.Scenes;
            await session.BootAsync();
            var end = _options.Start switch
            {
                GameStart.Title => await session.TitleAsync(),
                GameStart.New => await session.NewGameAsync(),
                GameStart.Load => await session.LoadFromTitleAsync(),
                _ => await session.EndingAsync((Sys3Int)_options.Ending),
            };
            if (end is not null)
            {
                // OVER.ADV ran to its ZZ0: back to the title (decision 19; the original quits, STR-10).
                GD.Print($"Kichikuou: the game ended, ending {end.EndingId.Value}.");
                Shot("end");
            }
            WarnUnresolved();
            Leave(failed: false);
            return;
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
        WarnUnresolved();
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

    private void WarnUnresolved()
    {
        if (_presenter.Unresolved.Count + _presenter.Battle.Unresolved.Count > 0)
            GD.PushWarning("Texts not found: " + string.Join("; ", _presenter.Unresolved.Concat(_presenter.Battle.Unresolved).Distinct()));
    }

    private void Leave(bool failed)
    {
        if (_options.QuitWhenDone)
            _tree.Quit(failed ? 1 : 0);
        else
            Screens.ToTitle(this);
    }

    /// <summary>
    /// <see cref="GameOptions.SlotsFrom"/>: the save slots of <paramref name="from"/> replace those in
    /// <paramref name="to"/> (the slots there without a copy are removed); none there is an error.
    /// </summary>
    private static void CopySlots(string from, string to)
    {
        var slots = System.IO.Directory.Exists(from) ? System.IO.Directory.GetFiles(from, "slot*.ksav") : [];
        if (slots.Length == 0)
            throw new InvalidOperationException($"No save slots to copy in {from}.");
        if (System.IO.Path.GetFullPath(from) == System.IO.Path.GetFullPath(to))
            throw new InvalidOperationException("--slots-from names the run's own save folder.");
        System.IO.Directory.CreateDirectory(to);
        foreach (string old in System.IO.Directory.GetFiles(to, "slot*.ksav"))
            System.IO.File.Delete(old);
        foreach (string slot in slots)
            System.IO.File.Copy(slot, System.IO.Path.Combine(to, System.IO.Path.GetFileName(slot)));
        GD.Print($"Kichikuou: {slots.Length} save slot(s) copied from {from}.");
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
        if (SkippingScene)
        {
            // A scene skipped whole is not drawn: its stage commands (seconds of work for the opening) are dropped.
            if (scriptEvent is EngineEvent { Command: "G" or "CC" or "CE" or "CS" or "CF" or "CM" or "CB" or "CL" })
            {
                _stageSkipped = true;
                return;
            }
        }
        else
        {
            ClearSkippedStage();
        }
        _presenter.Show(scriptEvent);
        if (scriptEvent is EngagementSceneEvent)
            await WaitForArmiesAsync();
        if (scriptEvent is MessageEvent message)
            _skip.OnMessage(message.Text);
        else if (scriptEvent is ValueMessageEvent values)
            _skip.OnMessage(values.Text);
        if (scriptEvent is MapScreenEvent { Waiting: true })
        {
            _autoMapWait = true;
            _autoDragged = false;
            if (_autoAttack != AutoAttack.None && _presenter.State is { } a)
            {
                _autoAttack = AutoAttack.None;                          // back on the map: the attack is over
                var p = a.Provs[_autoAttackProv];
                GD.Print($"Kichikuou: --auto's attack on province {_autoAttackProv}: "
                    + $"{(p.Owner == 1 ? "taken" : $"still owner {p.Owner.Value}'s")}, stage {a.StoryStage.Value}, gold {a.Gold.Value}.");
            }
            if (_autoDungeon == AutoDungeon.Searching && _presenter.State is { } s)
            {
                _autoDungeon = AutoDungeon.Done;                        // back on the map: the search is over
                GD.Print($"Kichikuou: --auto left the cave: floor {s.DgnRun_Floor.Value} reached, "
                    + $"{(_autoDungeonBattle ? "a battle fought" : "no battle")}, the searcher's status {s.Units[s.Bt_PlayerParty1].Status.Value}.");
            }
        }
        if (scriptEvent is BattleScreenEvent { Open: true } && _autoDungeon == AutoDungeon.Searching)
            _autoDungeonBattle = true;
        if (scriptEvent is BattleScreenEvent { File: "FIGT_A.ADV" or "FIGT_B.ADV" } battle && _options.Auto)
            AutoBattleLog(battle);
        if (_options.Auto)
            AutoStopEarly(scriptEvent);
        if (scriptEvent is EngineEvent { Command: "IZ", Args: [var x, var y] })
            await WarpPointerAsync(x.Value, y.Value);
        if (_presenter.TakeFade() is { } fade)
            await FadeAsync(fade);
    }

    /// <summary>
    /// An exchange begins (its engagement picture, numbers not yet drawn): on our battle screen it waits
    /// until the armies have slid into place (<see cref="BattleBoardView.ArmiesSettled"/>) — the ones the
    /// player's choice or the turn brought in, or the exchange's own (owner, 2026-10-13: the choice itself
    /// never waits for a slide, the fighting does).
    /// </summary>
    private async Task WaitForArmiesAsync()
    {
        var watch = Stopwatch.StartNew();
        while (BattleScreenShown && !_battleView.ArmiesSettled(_presenter.Scene))
        {
            if (_options.Auto && !_slideShot && watch.Elapsed.TotalSeconds >= BattleScreenSettings.Get(BattleScreenSettings.ArmySeconds) / 2)
            {
                _slideShot = true;                                          // unattended play's dump: once a launch, halfway
                Shot("slide");
            }
            await NextFrameAsync();
        }
    }

    private bool _slideShot;

    /// <summary>
    /// Animates a palette fade over its <see cref="FadeRequest.Duration"/>, never skipped, unless
    /// Option → MsgSkip Speeds Up Fades cuts it short while skipping (<see cref="MessageSkip.FadeDuration"/>);
    /// the presenter already holds its end.
    /// </summary>
    private async Task FadeAsync(FadeRequest fade)
    {
        double seconds = SkippingScene ? 0 : _skip.FadeDuration(fade.Duration).TotalSeconds;
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
        if ((_table.Visible || BattleScreenShown) && DisplayServer.GetName() != "headless")
        {
            // Our deployment and battle screens are drawn by Godot over the screen: the window's next frame (headless
            // draws none). No Compose here: the frame's Present must find the change to upload it.
            _ = WindowShotAsync(System.IO.Path.Combine(dir, $"{_shot++:D5}-{what}.png"));
            return;
        }
        _viewStale |= _presenter.Compose();                                     // the change it takes is the window's too
        var image = Image.CreateFromData(Surface.ScreenWidth * Surface.Scale, Surface.ScreenHeight * Surface.Scale,
            false, Image.Format.Rgba8, _presenter.Screen.ReadScreen());
        image.Resize(Surface.ScreenWidth, Surface.ScreenHeight, Image.Interpolation.Bilinear);
        image.SavePng(System.IO.Path.Combine(dir, $"{_shot++:D5}-{what}.png"));
    }

    /// <summary>The window as the next frame draws it, saved at <paramref name="path"/> (the screen is composed in the frame's process step).</summary>
    private async Task WindowShotAsync(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (!_closing)
            GetViewport().GetTexture().GetImage().SavePng(path);
    }

    // ---------------------------------------------------------------- mouse and keys

    /// <summary>The mouse position in script coordinates (640×400), clamped to the screen.</summary>
    private Vector2I ToScript(Vector2 local)
    {
        if (_battleView.Visible && _presenter.State is { } s)
            return _battleView.ToScript(local - _content.Position, s);  // a card's panel, or the windows' layer
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
        if (_battleView.Visible)
            return _content.Position + _battleView.FromScript(script);
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
                _battlePointer = motion.Position;
                _input.Position = ToScript(motion.Position);
                PointerMoved();
                break;
            case InputEventMouseButton { ButtonIndex: Godot.MouseButton.Left or Godot.MouseButton.Right } button:
                _battlePointer = button.Position;
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
        if (key.Pressed && key.Keycode == Godot.Key.Escape && _battleView.Dialog.Escape())
        {
            _answered?.TrySetResult();                                      // cancels our battle screen's dialog
            return;
        }
        if (key.Pressed && key.Keycode == Godot.Key.Escape && _battleView.Commands.Escape())
            return;                                                         // closes our battle screen's context menu
        if (MenuHotKey(key))
            return;
        _input.Key(key.Keycode, key.Pressed);
    }

    /// <summary>The menus' highlight and the battle's input screens follow the pointer.</summary>
    private void PointerMoved()
    {
        var (x, y) = (_input.Position.X, _input.Position.Y);
        _presenter.Pointer(x, y);
        if (_battleView.Commands.Active)
            _battleView.Commands.Move(_battleView.ToLayout(_battlePointer - _content.Position));
        if (_battleView.Dialog.Active)
            _battleView.Dialog.Move(_battleView.ToLayout(_battlePointer - _content.Position));
        HoverScene();
        if (_sel is null && _battleMenu is null)
            return;
        _sel?.MouseMove(x, y);
        _battleMenu?.MouseMove(x, y);
        _presenter.Invalidate();
    }

    /// <summary>
    /// While the player chooses (<c>FIGT_SEL</c>), the enemy card under the pointer (or whose banners it is on)
    /// puts its army into the scene (<see cref="BattleScene.Hover"/>).
    /// </summary>
    private void HoverScene()
    {
        if (_presenter.State is not { } s || !_presenter.Scene.Choosing)
            return;
        if (_battleView.Commands.Active)
            _presenter.Scene.Hover(_battleView.Commands.Hovered);
        else
            _presenter.Scene.Hover(_battleView.Visible ? _battleView.PlaceAt(GetLocalMousePosition() - _content.Position, s) : null);
    }

    /// <summary>
    /// A press or release for the battle's input screens, which are driven by the buttons' changes:
    /// only the presses that began while the screen is open, and their releases.
    /// </summary>
    private void BattleButton(ushort code, bool pressed)
    {
        var dialog = _battleView.Dialog;
        if (dialog.Active)
        {
            var at = _battleView.ToLayout(_battlePointer - _content.Position);
            if (pressed)
                dialog.Press(code == KeySpace, at);
            else
                dialog.Release(code == KeySpace, at);
            if (dialog.Done)
                _answered?.TrySetResult();
            return;
        }
        var commands = _battleView.Commands;
        if (commands.Active)
        {
            var at = _battleView.ToLayout(_battlePointer - _content.Position);
            if (pressed)
                commands.Press(code == KeySpace, at);
            else
                commands.Release(code == KeySpace, at);
            HoverScene();
            if (commands.Result is not null)
                _answered?.TrySetResult();
            return;
        }
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
        if (skippable && SkippingScene)
        {
            await SkipYieldAsync();
            return 0;
        }
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
        ushort key;
        if (SkippingScene)
        {
            await SkipYieldAsync();
            key = 0;
        }
        else if (BattleScreenShown && _presenter.Battle.ActionLabelsOpen)
        {
            // QUIRK: BSC-07 — our battle screen shows the actions on the fighters' cards, not the "vs"
            // window (owner, 2026-10-13): its click wait is answered at once, as by a click. A target's banner
            // is chosen at the press, so the button may still be held: wait for its release, as IK 0 does,
            // or the next key wait (the flavour line's) would take it.
            key = KeyReturn;
            await ReleaseWaitAsync(_input.PeekInput());
            _ikKey = key;
        }
        else if (_options.Auto)
        {
            Shot("key");
            await DelayAsync(_options.Fast ? 0 : 0.8);
            key = KeyReturn;
        }
        else
        {
            Shot("key");
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
        await ReleaseWaitAsync(key);
        _ikKey = key;
        return key;
    }

    /// <summary><c>sys_key_releasewait</c> with the release mask 16: while Return or the left button is held, polls until it is released.</summary>
    private async Task ReleaseWaitAsync(ushort held)
    {
        while ((held & KeyReturn) != 0)
            held = await KeyWaitAsync(0.05, cancelable: false);
    }

    public async Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
    {
        // Y 3 (cmdy.c): 0 reads the input once; the core keeps Y3waitFlags (Y 3, 10000 / 10001
        // never come here) and passes it as cancelable.
        _pointer.Interrupt();
        _presenter.Flush();
        if (SkippingScene)
        {
            await SkipYieldAsync();
            return Sys3Int.Zero;
        }
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

    /// <summary>
    /// The message skip is on inside a scene it skips whole (<see cref="VariantProfile.WholeScenes"/>,
    /// the opening between the prologue and the rebellion): its waits and fades take no time and none
    /// of it is shown (<see cref="Present"/>).
    /// </summary>
    private bool SkippingScene =>
        _scenes is not null && _skip.SkipsScene(_scenes.RunningScenes, _scenes.Script.State.Profile.WholeScenes);

    /// <summary>
    /// After a scene skipped whole, whose stage commands were dropped (<see cref="ShowAsync"/>): the
    /// stage is cleared, as <c>EC 0</c>, before anything else is drawn or shown on it — whatever the
    /// scene would have left there is not there to show.
    /// </summary>
    private void ClearSkippedStage()
    {
        if (!_stageSkipped)
            return;
        _stageSkipped = false;
        _presenter.Show(new EngineEvent("EC", [Sys3Int.Zero]));
    }

    /// <summary>
    /// Instead of a wait while the skip runs a scene through: a frame only every 100 ms, so the window
    /// stays responsive (and F1 can still turn the skip off); nothing of the scene is drawn meanwhile.
    /// </summary>
    private async Task SkipYieldAsync()
    {
        if (_sceneSkip is null && SkippingScene)
        {
            _sceneSkip = Stopwatch.StartNew();
            GD.Print($"Kichikuou: MsgSkip skips {_scenes!.RunningScenes[^1]}.");
        }
        if (_skipYield.ElapsedMilliseconds < 100)
            return;
        await NextFrameAsync();
        _skipYield.Restart();
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
            if (_pointer.Pending == 0)
                _wantedOrder = null;                    // the script asks again with nothing queued: a drop's clicks are done
            UpdateDeploymentScreen();
            AutoPointer();
            var queued = _pointer.Next(out bool read);
            if (!read)
                await NextFrameAsync();                 // the scripts poll the pointer: one read per frame
            return queued;
        }
        await NextFrameAsync();
        if (_tablePointer.Pending == 0)
            _wantedOrder = null;                        // the script asks again with nothing queued: a drop's clicks are done
        if (UpdateDeploymentScreen() && (_tablePointer.Pending > 0 || _table.HasPointer || _panels.HasPointer))
        {
            // Over our screen the script's pointer is ours: on the hovered row's or member card's cell, pressed by its gestures.
            if (_tablePointer.Pending == 0)
            {
                var (x, y) = DeploymentTable.PointerAt(HoveredUnit);
                _tablePointer.Move(x, y);
            }
            return _tablePointer.Next(out _);
        }
        return new MouseState((Sys3Int)_input.ReadMouse(), (Sys3Int)_input.Position.X, (Sys3Int)_input.Position.Y);
    }

    // ---------------------------------------------------------------- the deployment table (owner's decision 25)

    /// <summary>The grid area the table covers, in script pixels: right of the portrait and above the bar (<c>Sﾌﾞﾀｲ.ADV:498-523</c>).</summary>
    private static readonly Rect2 TableArea = new(322, 4, 316, 318);

    /// <summary>
    /// Shows our deployment screen — the table over <c>Sﾌﾞﾀｲ</c>'s grid and the panels over the rest
    /// but the portrait — while the deployment picker is open with Option → Interface on New (the
    /// presenter draws only the background and the portrait under it), but not while a message
    /// window is up over the screen; places it over the picture as the window is sized and fills it
    /// from GameState. Returns whether it shows. A screen shown or hidden starts with no gestures queued.
    /// </summary>
    private bool UpdateDeploymentScreen()
    {
        bool ours = InterfaceChoice.New && _presenter.Picker is { Kind: PickerKind.Deployment };
        _presenter.DeploymentCovered = ours;
        bool show = ours && !_presenter.MessageWindowShown && !SkippingScene && _presenter.State is not null;
        if (show != _table.Visible)
        {
            _table.Visible = _panels.Visible = show;
            _tablePointer = new PointerScript();
            _wantedOrder = null;
            if (show)
                _panels.LoadCrops();                            // the owner's squares from Debug → Portraits
        }
        if (!show)
            return false;
        var size = _view.Size;
        float scale = Math.Min(size.X / (Surface.ScreenWidth * Surface.Scale), size.Y / (Surface.ScreenHeight * Surface.Scale));
        var drawn = new Vector2(Surface.ScreenWidth, Surface.ScreenHeight) * Surface.Scale * scale;
        float pixels = scale * Surface.Scale;
        var origin = _view.Position + (size - drawn) / 2;
        _panels.Busy = (_options.Auto ? _pointer : _tablePointer).Pending > 0 || _wantedOrder is not null;
        _panels.Place(origin, pixels);
        _panels.Refresh(_presenter.State!, _presenter.Picker!, _wantedOrder);
        _table.Place(new Rect2(origin + TableArea.Position * pixels, TableArea.Size * pixels), pixels);
        _table.Refresh(_presenter.State!, _wantedOrder);
        return true;
    }

    // ---------------------------------------------------------------- our battle screen (docs/ui/battle-screen.md)

    /// <summary>
    /// Shows our battle screen over the stage while Option → Interface is on New and the battle's field
    /// or an exchange is on (<see cref="GamePresenter.BattleSceneShows"/>; the pre-battle map, the
    /// strategy and scout screens and the scenes a battle calls stay the original's); places it over the
    /// picture as the window is sized and fills it (<paramref name="screenChanged"/>: the windows' layer
    /// is read again).
    /// </summary>
    private bool BattleScreenShown => InterfaceChoice.New && _presenter.BattleSceneShows && !SkippingScene && _presenter.State is not null;

    private void UpdateBattleScreen(bool screenChanged)
    {
        bool show = BattleScreenShown;
        if (show != _battleView.Visible)
        {
            _battleView.Visible = show;
            _view.Visible = !show;
            screenChanged = true;
            if (show)
                _battleView.LoadCrops();
        }
        if (!show)
        {
            if (_battleView.Commands.Active || _battleView.Dialog.Active)
                _answered?.TrySetResult();                                  // the interface changed: the original's FIGT_SEL or menu takes over
            return;
        }
        var size = _view.Size;
        float scale = Math.Min(size.X / (Surface.ScreenWidth * Surface.Scale), size.Y / (Surface.ScreenHeight * Surface.Scale));
        var drawn = new Vector2(Surface.ScreenWidth, Surface.ScreenHeight) * Surface.Scale * scale;
        _battleView.Place(_view.Position + (size - drawn) / 2, scale);
        IEnumerable<WindowShape> input = _sel?.Windows ?? _battleMenu?.Windows ?? [];
        var hovered = _battleView.Commands.Active ? _battleView.Commands.Lit   // a card lights up only while the player chooses
            : _sel is null ? null : _battleView.PlaceAt(GetLocalMousePosition() - _content.Position, _presenter.State!);
        _battleView.Refresh(_presenter, input, screenChanged, hovered,
            instant: _skip.IsSkipping,                                         // MsgSkip: the order strip's chips jump to their places
            slidesInstant: _skip.IsSkipping || _options.Fast);                 // and the armies (--fast: no slides)
    }

    /// <summary>
    /// A row pressed (by the mouse, or by unattended play): the gestures the script expects
    /// (<see cref="DeploymentTable.Press"/>), queued for the table's pointer — or unattended play's —
    /// unless the last press's are still under way.
    /// </summary>
    private void TableRowPressed(int unit)
    {
        var pointer = _options.Auto ? _pointer : _tablePointer;
        if (pointer.Pending > 0 || _wantedOrder is not null || _presenter.State is not { } s)
            return;
        DeploymentTable.Press(s, unit, pointer);
    }

    /// <summary>A bar button pressed (by the mouse, or by unattended play): a click on the script's button there.</summary>
    private void TableButtonPressed(int button)
    {
        var pointer = _options.Auto ? _pointer : _tablePointer;
        if (pointer.Pending > 0 || _wantedOrder is not null)
            return;
        var (x, y) = GesturePoints.PickerButton(button);
        pointer.Click(x, y);
    }

    /// <summary>
    /// A member's card dropped on another place (by the mouse, or by unattended play): the clicks that
    /// give the order the drop asks for (<see cref="DeploymentTable.Drop"/>), queued as a row's press
    /// is; until the script has run them our screen shows that order and starts no other drag.
    /// </summary>
    private void PartyDropped(int from, int to)
    {
        var pointer = _options.Auto ? _pointer : _tablePointer;
        if (pointer.Pending > 0 || _wantedOrder is not null || _presenter.State is not { } s)
            return;
        _wantedOrder = DeploymentTable.Drop(s, from, to, pointer);
    }

    /// <summary>A right press on our screen: a right click where the script's pointer is, which takes the last member out.</summary>
    private void TableRightPressed()
    {
        if (_options.Auto || _tablePointer.Pending > 0 || _wantedOrder is not null)
            return;
        DeploymentTable.RightPress(HoveredUnit, _tablePointer);
    }

    /// <summary>The unit hovered on our deployment screen: its row in the table or its card in the party panel, else 0.</summary>
    private int HoveredUnit => _table.HoveredUnit != 0 ? _table.HoveredUnit : _panels.HoveredUnit;

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

    /// <summary>
    /// <c>FIGT_SEL</c>: on our battle screen (Option → Interface → New) our own command input answers it
    /// (<see cref="BattleCommandLayer"/>: the banners on the enemy cards, the cards' context menus); else, or
    /// once the interface changes while the player chooses, the original's prompt and popups (<see cref="FigtSelScreen"/>).
    /// </summary>
    public async Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
    {
        _pointer.Interrupt();
        _presenter.Flush();
        if (_options.Auto)
        {
            await DelayAsync(_options.Fast ? 0 : 0.5);
            var direct = menu.Find(FigtSelCommand.Direct);
            if (_autoCommandsPhase < 3 && BattleScreenShown && DisplayServer.GetName() != "headless" && direct is { Arguments.Count: > 0 })
                return await AutoCommandsAsync(menu, direct.Arguments[0]);
            return direct is { Arguments.Count: > 0 }
                ? new FigtSelChoice(FigtSelCommand.Direct, direct.Arguments[0])
                : new FigtSelChoice(FigtSelCommand.Defend, menu.Find(FigtSelCommand.Defend)!.Arguments[0]);
        }
        _battleButtons = 0;
        _presenter.Scene.Choosing = true;
        var commands = _battleView.Commands;
        try
        {
            if (BattleScreenShown)
            {
                _answered = new TaskCompletionSource();
                commands.Begin(new BattleCommands(menu), _presenter.State!, _presenter.Battle.Text);
                PointerMoved();
                await _answered.Task;
                if (commands.Result is { } choice)
                    return choice;
                commands.End();
            }
            _answered = new TaskCompletionSource();
            _sel = new FigtSelScreen(_presenter.Battle, menu);
            _sel.MouseMove(_input.Position.X, _input.Position.Y);
            HoverScene();
            _presenter.Invalidate();
            await _answered.Task;
            return _sel.Result!.Value;
        }
        finally
        {
            commands.End();
            _sel = null;
            _presenter.Scene.Choosing = false;
            _presenter.Invalidate();
            _input.ReadMouse();                                             // the script's own IM polls took the answering click
        }
    }

    /// <summary>
    /// The battle's release loops (<c>IM</c> while a button is down, <see cref="IBattleInput.WaitForReleaseAsync"/>):
    /// the engine's mouse reads, once a frame; unattended play holds no button.
    /// </summary>
    public async Task WaitForReleaseAsync()
    {
        if (_options.Auto)
            return;
        while ((await GetMouseAsync()).Button != 0)
        {
        }
    }

    /// <summary>
    /// Unattended play on our battle screen, once a launch (in a window): the first turn goes through the
    /// pointer as a player's would. First a right click on the acting unit's card opens its menu (screen
    /// dump "menu", the pointer on its first item), one on another own card opens that one's
    /// ("menu-orders"), a left click off it closes it, and Retreat from the acting unit's menu asks the
    /// retreat dialog, which <see cref="AutoRetreatDialogAsync"/> cancels. Back at the same turn: the
    /// pointer on each enemy card shows its banners (dumps "banners-E1"…); two sample menus, never
    /// answered, show two and three banners on the target ("sample-2-banners", "sample-3-banners": Rance
    /// Attack added; Bow and Whirlwind Shot added); then the pointer on the target's Melee banner
    /// ("banner-hover"), and a click there attacks. Anything else is an error.
    /// </summary>
    private async Task<FigtSelChoice> AutoCommandsAsync(FigtSelMenu menu, Sys3Int button)
    {
        var s = _presenter.State!;
        var model = new BattleCommands(menu);
        var commands = _battleView.Commands;
        _battleButtons = 0;
        _answered = new TaskCompletionSource();
        _presenter.Scene.Choosing = true;
        commands.Begin(model, s, _presenter.Battle.Text);
        try
        {
            if (_autoCommandsPhase == 0)
            {
                GD.Print($"Kichikuou: --auto opens the cards' menus and asks the retreat dialog (week {s.Week.Value}).");
                await AutoClickAsync(Centre(model.Acting), KeySpace);
                if (!commands.MenuOpen || commands.ItemCentre(0) is not { } first)
                    throw new InvalidOperationException("--auto's right click on the acting unit's card opened no menu.");
                await AutoMoveAsync(first);
                await AutoDumpAsync("menu");
                var other = Enumerable.Range(1, menu.PlayerSlotsUsed.Value).Select(n => new BattlePlace(BattleSide.Player, n)).FirstOrDefault(p => p != model.Acting);
                if (other.Slot != 0)
                {
                    await AutoClickAsync(Centre(other), KeySpace);             // another card: its menu at once
                    if (commands.ItemCentre(1) is { } second)
                        await AutoMoveAsync(second);
                    await AutoDumpAsync("menu-orders");
                }
                await AutoClickAsync(new Vector2(640, 300), KeyReturn);        // in the scene, off the menu
                if (commands.MenuOpen || commands.Result is not null)
                    throw new InvalidOperationException("--auto's left click off the menu did not close it.");
                var retreat = new FigtSelChoice(FigtSelCommand.Retreat, BattleCommands.NoButton);
                await AutoClickAsync(Centre(model.Acting), KeySpace);
                int item = commands.MenuItems.ToList().IndexOf(retreat);
                if (item >= 0 && commands.ItemCentre(item) is { } at)
                {
                    await AutoClickAsync(at, KeyReturn);
                    if (commands.Result != retreat)
                        throw new InvalidOperationException($"--auto's click on Retreat answered {commands.Result}, not {retreat}.");
                    _autoCommandsPhase = 1;                                    // the retreat dialog comes next
                    return retreat;
                }
                await AutoClickAsync(new Vector2(640, 300), KeyReturn);
                _autoCommandsPhase = 2;                                        // no Retreat in the menu: on to the banners
            }

            GD.Print($"Kichikuou: --auto shows the banners and attacks by a banner (week {s.Week.Value}).");
            _autoCommandsPhase = 3;
            var target = new BattlePlace(BattleSide.Enemy, button.Value - 3);
            var expected = new FigtSelChoice(FigtSelCommand.Direct, button);
            for (int slot = 1; slot <= menu.EnemySlotsUsed.Value; slot++)
            {
                await AutoMoveAsync(Face(new BattlePlace(BattleSide.Enemy, slot)));
                await AutoDumpAsync($"banners-E{slot}");
            }
            // Samples for the banners' look, never answered: the menus a unit with a special and focus would get.
            var button4 = new List<Sys3Int> { button };
            foreach (var (name, sample) in new[]
            {
                ("sample-2", menu with { Options = [.. menu.Options, new FigtSelOption(FigtSelCommand.RanceAttack, button4)] }),
                ("sample-3", menu with { AttackType = 1, Options = [.. menu.Options, new FigtSelOption(FigtSelCommand.Bow, button4), new FigtSelOption(FigtSelCommand.WhirlwindShot, button4)] }),
            })
            {
                commands.Begin(new BattleCommands(sample), s, _presenter.Battle.Text);
                await AutoMoveAsync(Face(target));
                await AutoDumpAsync($"{name}-banners");
            }
            commands.Begin(model, s, _presenter.Battle.Text);
            await AutoMoveAsync(Face(target));
            if (commands.BannerCentre(expected) is not { } banner)
                throw new InvalidOperationException($"--auto found no Melee banner on {target} (banners {string.Join(", ", commands.Banners)}).");
            await AutoMoveAsync(banner);
            await AutoDumpAsync("banner-hover");
            await AutoClickAsync(banner, KeyReturn);
            if (commands.Result != expected)
                throw new InvalidOperationException($"--auto's click on {target}'s Melee banner answered {commands.Result}, not {expected}.");
            return expected;
        }
        finally
        {
            commands.End();
            _presenter.Scene.Choosing = false;
            _presenter.Invalidate();
            _input.ReadMouse();
        }

        static Vector2 Centre(BattlePlace place) => BattleBoardView.CardArea(place).GetCenter();
        static Vector2 Face(BattlePlace place) => BattleBoardView.CardArea(place).Position + new Vector2(220, 75);   // an enemy's face, off its banners
    }

    /// <summary>
    /// Unattended play, after <see cref="AutoCommandsAsync"/> chose Retreat: the retreat dialog through the
    /// pointer (dump "retreat", the pointer on Retreat), cancelled by a left click off it.
    /// </summary>
    private async Task<Sys3Int?> AutoRetreatDialogAsync(BattleDialog dialog)
    {
        _autoCommandsPhase = 2;
        GD.Print("Kichikuou: --auto shows the retreat dialog and cancels it.");
        var layer = _battleView.Dialog;
        _battleButtons = 0;
        _answered = new TaskCompletionSource();
        layer.Begin(dialog, _presenter.Battle.Text);
        try
        {
            await AutoMoveAsync(layer.ItemCentre(0));
            await AutoDumpAsync("retreat");
            await AutoClickAsync(new Vector2(100, 100), KeyReturn);           // in the scene, off the box
            if (!layer.Done || layer.Result is not null)
                throw new InvalidOperationException($"--auto's click off the retreat dialog did not cancel it (done {layer.Done}, {layer.Result}).");
            return null;
        }
        finally
        {
            layer.End();
            _input.ReadMouse();
        }
    }

    /// <summary>Unattended play: the system pointer and the script's onto layout point <paramref name="layout"/> of our battle screen.</summary>
    private async Task AutoMoveAsync(Vector2 layout)
    {
        var at = _content.Position + _battleView.FromLayout(layout);
        WarpMouse(at);
        _battlePointer = at;
        _input.Position = ToScript(at);
        PointerMoved();
        await NextFrameAsync();
        await NextFrameAsync();
    }

    /// <summary>Unattended play: a click (press and release) at layout point <paramref name="layout"/>.</summary>
    private async Task AutoClickAsync(Vector2 layout, ushort code)
    {
        await AutoMoveAsync(layout);
        _input.Button(code, true);
        BattleButton(code, true);
        await NextFrameAsync();
        _input.Button(code, false);
        BattleButton(code, false);
        await NextFrameAsync();
        await NextFrameAsync();
    }

    /// <summary>Unattended play: a screen dump of the next frame, then two frames for it to be taken.</summary>
    private async Task AutoDumpAsync(string what)
    {
        Shot(what);
        await NextFrameAsync();
        await NextFrameAsync();
    }

    /// <summary>
    /// A script menu of the battle: on our battle screen the retreat confirmation is our own dialog
    /// (<see cref="BattleDialogLayer"/>); the rest, or once the interface changes while it is open, the
    /// script's menu (<see cref="BattleMenuScreen"/>).
    /// </summary>
    public async Task<Sys3Int?> ChooseAsync(BattleMenu menu)
    {
        _pointer.Interrupt();
        _presenter.Flush();
        if (_options.Auto)
        {
            await DelayAsync(_options.Fast ? 0 : 0.5);
            if (_autoCommandsPhase == 1 && BattleScreenShown && BattleDialog.For(menu) is { } auto)
                return await AutoRetreatDialogAsync(auto);
            if (menu.Kind == BattleMenuKind.EnemyAttackResponse && _autoAbandon)
            {
                // FIGT_B's menu after a stopped deployment picker: Abandon City (code 3).
                _autoAbandon = false;
                return menu.Options.First(o => o.Code == 3).Code;
            }
            return menu.Options[0].Code;
        }
        _battleButtons = 0;
        _skip.OnMenu();
        if (BattleScreenShown && BattleDialog.For(menu) is { } dialog)
        {
            var layer = _battleView.Dialog;
            _answered = new TaskCompletionSource();
            layer.Begin(dialog, _presenter.Battle.Text);
            PointerMoved();
            try
            {
                await _answered.Task;
                if (layer.Done)
                    return layer.Result;
            }
            finally
            {
                layer.End();
                _input.ReadMouse();
            }
        }
        _answered = new TaskCompletionSource();
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
        _timer.Restart(value.Value, _clock.Elapsed);
        return Task.CompletedTask;
    }

    /// <summary>
    /// <c>ZT2</c>-<c>ZT5</c>: the timer in 1/10, 1/30, 1/60 or 1/100 s; read once per frame (the scripts
    /// wait on it in loops). While the message skip is on, one tick a read and no frame (but
    /// <see cref="SkipYieldAsync"/>'s): the opening runs through at once (<see cref="EngineTimer"/>).
    /// </summary>
    public async Task<Sys3Int> GetTimerAsync(int mode)
    {
        bool skipping = _skip.IsSkipping;
        if (!skipping)
            await NextFrameAsync();
        long ms = _timer.Read(_clock.Elapsed, skipping);
        if (skipping)
            await SkipYieldAsync();
        return EngineTimer.InUnits(ms, mode);
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

    /// <summary><c>ZZ3</c> (<c>cmdz.c</c>): the size of the screen the window is on, at 24 bits per pixel (SDL's XRGB8888 desktop).</summary>
    public Task<IReadOnlyList<Sys3Int>> GetDisplayAsync()
    {
        var size = DisplayServer.ScreenGetSize();
        return Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)size.X, (Sys3Int)size.Y, (Sys3Int)24]);
    }

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

    /// <summary>The deployment picker the unattended player builds <see cref="_autoParty"/> in.</summary>
    private PickerView? _autoPartyFrom;

    /// <summary>The picker the unattended player drops on (our party panel) and how many drops it has made there (3: done).</summary>
    private PickerView? _autoDropPicker;
    private int _autoDrops;
    private bool _autoDropsUsed;

    /// <summary>
    /// How far the unattended player has gone through our battle screen's input by the pointer (once a launch,
    /// <see cref="AutoCommandsAsync"/>): 0 not yet, 1 Retreat chosen, 2 its dialog cancelled, 3 done.
    /// </summary>
    private int _autoCommandsPhase;

    /// <summary>The pointer as the last mouse event (or unattended play) put it, in this control's coordinates.</summary>
    private Vector2 _battlePointer;

    private List<int> _autoParty = [];

    /// <summary>The reads the unattended player has hovered a row of our table for, waiting for its details.</summary>
    private int _autoHovers;

    /// <summary>Whether the unattended player has hovered one read more since the details showed (a window dump saves the next frame).</summary>
    private bool _autoHoverHeld;

    /// <summary>Whether the unattended player has pressed a row of our own deployment table (logged once).</summary>
    private bool _autoTableUsed;

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

    /// <summary>
    /// The rebel provinces the unattended player attacks at stage 1, the first one next to an own
    /// province: the shortest way to province 6, whose conquest ends the stage
    /// (<c>FIGT_A.ADV:192-195</c>), then the others next to the player's land.
    /// </summary>
    private static readonly int[] AutoTargets = [4, 5, 6, 7, 3];

    /// <summary>Where the unattended player's attack of the week stands, from the click on the province to the map again.</summary>
    private enum AutoAttack { None, Clicked, Chosen }

    private AutoAttack _autoAttack;
    private int _autoAttackWeek = -1;
    private int _autoAttackProv;

    /// <summary>The unattended player dragged the map: the map loop holds the camera as the view's corner.</summary>
    private bool _autoDragged;

    /// <summary>The week the unattended player first saw <see cref="GameOptions.AutoStage"/> reached at the command menu.</summary>
    private int _autoStageWeek = -1;

    /// <summary>
    /// Where the unattended player's replenish of the week stands: wanted at the map, Subordinates
    /// chosen, the unit picked, Replenish chosen (then Do).
    /// </summary>
    private enum AutoReplenish { None, Wanted, Chosen, Picked, Asked }

    private AutoReplenish _autoReplenish;
    private int _autoReplenishUnit;
    private int _autoReplenishWeek = -1;

    /// <summary>The end of unattended play (<see cref="GameOptions.AutoWeeks"/>).</summary>
    private sealed class AutoStopException(string message) : Exception(message);

    /// <summary>
    /// The map opens the command menu (one right click each time the map loop starts waiting) — once a
    /// week first for a replenish (<see cref="AutoReplenishUnit"/>) —, or once a week at stage 1 a rebel province with a left click on it (<see cref="AutoAttackTarget"/>;
    /// dragged into view first when it is out of it), or once the Monsters' Cave
    /// (<see cref="AutoCaveOnScreen"/>); the deployment picker takes <see cref="AutoDeploy"/>; the
    /// item screen takes <see cref="AutoItemSteps"/>, then ends (third button); the subordinates
    /// picker the unit to replenish; any other picker
    /// is left with its third button (End, Stop). All Data's first graph gets a right
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
                var end = GesturePoints.PickerButton(3);
                _pointer.Click(end.X, end.Y);
            }
            return;
        }
        if (_presenter.Picker is { Kind: PickerKind.Deployment } deployment && _presenter.State is { } d)
        {
            Shot("picker");
            if (AutoDrop(deployment, d))
                return;
            var (step, value) = AutoDeploy(deployment, d);
            if (step == AutoDeployStep.Button && _panels.Visible)
            {
                _panels.PressButton(value);                             // our own bar
                return;
            }
            bool detailsShown = DeploymentTable.Details(d)?.Unit == value;
            if (step == AutoDeployStep.Join && _table.Visible && (!detailsShown || !_autoHoverHeld) && _autoHovers++ < 10)
            {
                _autoHoverHeld = detailsShown;
                var (hx, hy) = DeploymentTable.PointerAt(value);
                _pointer.Move(hx, hy);                                  // the row hovered first: the script shows its details
                return;
            }
            _autoHovers = 0;
            _autoHoverHeld = false;
            if (step != AutoDeployStep.Button && _table.Visible)
            {
                if (!_autoTableUsed)
                    GD.Print($"Kichikuou: --auto builds its party in the deployment table (week {d.Week.Value}).");
                _autoTableUsed = true;
                _table.Press(value);                                    // our own table: a press on the unit's row
                return;
            }
            var (dx, dy) = step switch
            {
                AutoDeployStep.Join => GesturePoints.PickerCell(value),
                AutoDeployStep.TakeOut => GesturePoints.PickerOutside,
                _ => GesturePoints.PickerButton(value),
            };
            _pointer.Click(dx, dy);
        }
        else if (_presenter.Picker is { } picker)
        {
            Shot("picker");
            var (x, y) = GesturePoints.PickerButton(3);
            if (picker.Kind == PickerKind.Subordinates && _autoReplenish == AutoReplenish.Chosen)
            {
                _autoReplenish = AutoReplenish.Picked;
                (x, y) = GesturePoints.PickerCell(_autoReplenishUnit);
            }
            _pointer.Click(x, y);
        }
        else if (_presenter.MapWaiting && (_autoMapWait || _autoDragged) && _presenter.State is { } s)
        {
            // The map wait's first read still has the camera as the centre (COMMAND.ADV:50-58), on
            // Leazas Castle at the start of the week (:4-13); after a drag the loop holds it as the corner.
            bool centred = !_autoDragged;
            _autoMapWait = _autoDragged = false;
            if (centred && AutoReplenishUnit(s) is { } unit)
            {
                GD.Print($"Kichikuou: --auto replenishes unit {unit} ({s.Units[unit].Troops.Value} of {s.Units[unit].MaxTroops.Value} troops, week {s.Week.Value}).");
                _autoReplenish = AutoReplenish.Wanted;
                _autoReplenishUnit = unit;
                _autoReplenishWeek = s.Week.Value;
                _pointer.RightClick(5, 6);                              // the menu: Subordinates
                return;
            }
            if (AutoAttackTarget(s) is { } target)
            {
                var p = s.Provs[target];
                if (GesturePoints.OnScreen(s, p.MapX, p.MapY, centred) is var (x, y))
                {
                    GD.Print($"Kichikuou: --auto attacks province {target} (week {s.Week.Value}, gold {s.Gold.Value}).");
                    _autoAttack = AutoAttack.Clicked;
                    _autoAttackWeek = s.Week.Value;
                    _autoAttackProv = target;
                    _pointer.Click(x, y);
                    return;
                }
                if (GesturePoints.DragToward(s, p.MapX, p.MapY, centred) is var (fromX, fromY, toX, toY))
                {
                    _autoDragged = true;
                    _pointer.Drag(fromX, fromY, toX, toY);
                    return;
                }
                _autoAttackWeek = s.Week.Value;                         // out of reach: no attack this week
            }
            if (centred && AutoCaveOnScreen(s) is var (cx, cy))
            {
                _autoDungeon = AutoDungeon.Searching;
                _pointer.Click(cx, cy);
            }
            else
            {
                _pointer.RightClick(5, 6);
            }
        }
    }

    /// <summary>
    /// A line for each war battle of unattended play: when it starts, both sides' commanders with
    /// their troops (<c>Bt_PlayerParty1-4</c>, <c>Bt_EnemyParty1-4</c>), and when it returns, the result
    /// (<c>Bt_Result</c>: 2 the player won).
    /// </summary>
    private void AutoBattleLog(BattleScreenEvent battle)
    {
        if (_presenter.State is not { } s)
            return;
        string Side(IEnumerable<Sys3Int> ids, Func<int, int> troops) =>
            string.Join(" ", ids.Select(id => id.Value).Where(id => id != 0).Select(id => $"{id}:{troops(id)}"));
        if (battle.Open)
        {
            // FIGT_B picks the player's party itself: only the attackers are known here.
            _autoBattleProv = s.Prov_Cur.Value;
            _autoBattleEnemy = Side([s.Bt_EnemyParty1, s.Bt_EnemyParty2, s.Bt_EnemyParty3, s.Bt_EnemyParty4], id => s.Cmdrs[id].Troops.Value);
            return;
        }
        string ours = Side([s.Bt_PlayerParty1, s.Bt_PlayerParty2, s.Bt_PlayerParty3, s.Bt_PlayerParty4], id => s.Units[id].Troops.Value);
        GD.Print($"Kichikuou: {battle.File[..^4]} at province {_autoBattleProv} (week {s.Week.Value}): "
            + $"commanders {_autoBattleEnemy} against units {(ours == "" ? "none" : ours)} (troops left): "
            + $"{(s.Bt_Result == 2 ? "won" : $"lost (result {s.Bt_Result.Value})")}.");
    }

    private int _autoBattleProv;
    private string _autoBattleEnemy = "";

    /// <summary>
    /// <see cref="GameOptions.AutoUntil"/>: the end of unattended play when the first deployment
    /// picker closes, or when the first battle (<c>FIGT_A</c>, <c>FIGT_B</c>, <c>FIGT</c>) ends.
    /// </summary>
    private void AutoStopEarly(ScriptEvent scriptEvent)
    {
        int week = _presenter.State?.Week.Value ?? 0;
        switch (_options.AutoUntil, scriptEvent)
        {
            case (AutoUntil.Deployment, PickerScreenEvent { Kind: PickerKind.Deployment, Open: false }):
                throw new AutoStopException($"--auto stopped after the first deployment picker (week {week}).");
            case (AutoUntil.Battle, BattleScreenEvent { File: "FIGT_A.ADV" or "FIGT_B.ADV" or "FIGT.ADV", Open: false } battle):
                throw new AutoStopException($"--auto stopped after the first battle, {battle.File} (week {week}).");
        }
    }

    /// <summary>
    /// The rebel province the unattended player attacks this week, if any: at stage 1, from the second
    /// Leazas week on (the first refuses it, "disrupted": <c>Cﾃｷﾄｼ.ADV:144</c>), once a week while the
    /// week's attack is free and Rance, who leads it, is free with at least three quarters of his
    /// troops (thinner, he tends to die), the first of <see cref="AutoTargets"/> the rebels hold next to
    /// an own province (a click on it opens <c>Cﾃｷﾄｼ</c>, <c>COMMAND.ADV:531-574</c>).
    /// </summary>
    private int? AutoAttackTarget(GameState s)
    {
        if (_options.AutoTurnEnd)
            return null;
        if (s.StoryStage != 1 || s.F_LeazasRebels_State == 1 || s.Ui_CmdAttackAvail != 1 || s.Week.Value == _autoAttackWeek)
            return null;
        if (s.Units[1].Status != 1 || s.Units[1].Troops.Value * 4 < s.Units[1].MaxTroops.Value * 3)
            return null;
        foreach (int target in AutoTargets)
        {
            var p = s.Provs[target];
            if (p.Owner == 2 && new[] { p.Adj1, p.Adj2, p.Adj3, p.Adj4, p.Adj5 }.Any(adj => adj != 0 && s.Provs[adj].Owner == 1))
                return target;
        }
        return null;
    }

    /// <summary>
    /// The unit the unattended player replenishes this week, if any (<c>Cﾌﾞｶ</c>'s Replenish Troops:
    /// the troops back to the maximum, once a week, <c>CmdAvail_SubTroops</c>; only a unit at status 1,
    /// so before the week's attack): from stage 1 on, of the units at status 1 short of troops the one
    /// whose replenish restores the most strength, the missing troops times troop attack.
    /// </summary>
    private int? AutoReplenishUnit(GameState s)
    {
        if (_options.AutoTurnEnd)
            return null;
        if (s.StoryStage.Value is < 1 or >= 99 || s.Ui_CmdSubordAvail != 1 || s.CmdAvail_SubTroops != 1
            || s.Week.Value == _autoReplenishWeek)
            return null;
        int Gain(int u) => (s.Units[u].MaxTroops.Value - s.Units[u].Troops.Value) * s.Units[u].TroopAtk.Value;
        int unit = Enumerable.Range(1, 50).Where(u => s.Units[u].Status == 1 && s.Units[u].Troops < s.Units[u].MaxTroops)
            .OrderByDescending(Gain).ThenBy(u => u).FirstOrDefault();
        return unit == 0 ? null : unit;
    }

    /// <summary>What the unattended player does next in the deployment picker.</summary>
    private enum AutoDeployStep
    {
        /// <summary>The unit joins the party (a click on its cell, or a press on its row).</summary>
        Join,

        /// <summary>The last member leaves it (a click left of the list, or a press on its row).</summary>
        TakeOut,

        /// <summary>A bar button.</summary>
        Button,
    }

    /// <summary>
    /// With our screen (Option → Interface: New), once the party is built: place 1's member dropped
    /// onto place 2 and back, as the player's drags make them (<see cref="PartyDropped"/>); the party
    /// must come out as it was. Returns whether a drop was made.
    /// </summary>
    private bool AutoDrop(PickerView picker, GameState s)
    {
        if (_autoDropPicker != picker)
        {
            _autoDropPicker = picker;
            _autoDrops = 0;
        }
        if (!_panels.Visible || _autoPartyFrom != picker || _autoDrops == 3)
            return false;
        var order = DeploymentTable.PartyOrder(s);
        if (_autoDrops == 0 && (order.Count < 2 || !order.SequenceEqual(_autoParty)))
            return false;
        if (_autoDrops == 2)
        {
            _autoDrops = 3;
            if (!order.SequenceEqual(_autoParty))
                throw new InvalidOperationException($"--auto's drops left the party {string.Join(" ", order)}, not {string.Join(" ", _autoParty)}.");
            return false;
        }
        if (!_autoDropsUsed)
            GD.Print($"Kichikuou: --auto swaps the first two places of its party by drops and back (week {s.Week.Value}).");
        _autoDropsUsed = true;
        _autoDrops++;
        PartyDropped(1, 2);
        return true;
    }

    /// <summary>
    /// The deployment picker's next step: the party of <see cref="AutoParty"/> joined, the last one
    /// taken out while the war expenses the screen shows (<c>Tmp0016</c>) top the gold, then Go (the
    /// first button) — or Stop (the third) with nobody to send or when Go was refused, which in a
    /// defence leads to abandoning the city.
    /// </summary>
    private (AutoDeployStep Step, int Value) AutoDeploy(PickerView picker, GameState s)
    {
        if (_autoPartyFrom != picker)
        {
            _autoPartyFrom = picker;
            _autoParty = ChosenParty(s) ?? AutoParty(picker.Mode.Value, s);
        }
        int[] party = [.. new[] { s.Bt_PlayerParty1, s.Bt_PlayerParty2, s.Bt_PlayerParty3, s.Bt_PlayerParty4 }
            .Select(u => u.Value).Where(u => u != 0)];
        if (_autoSentFrom != picker)
        {
            if (party.Length < _autoParty.Count && party.SequenceEqual(_autoParty.Take(party.Length)))
                return (AutoDeployStep.Join, _autoParty[party.Length]);
            if (party.Length > 0 && s.Tmp0016 > s.Gold)
            {
                _autoParty = [.. party[..^1]];
                return (AutoDeployStep.TakeOut, party[^1]);
            }
            if (party.Length > 0)
            {
                _autoSentFrom = picker;
                return (AutoDeployStep.Button, 1);
            }
        }
        _autoSentFrom = null;
        if (picker.Mode == 2)
            _autoAbandon = true;
        return (AutoDeployStep.Button, 3);
    }

    /// <summary>
    /// The units the unattended player sends, of those free (status 1), the strongest first (the most
    /// troops times troop attack), up to the places the screen offers (<c>Tmp0040</c>): an attack
    /// (mode 1) takes Rance and the strongest others, leaving two free for the rebels' turn — all it
    /// may take against Oaks (province 6: walls, a narrow field, defenders who never retreat, and a
    /// battle the attacker loses when its rounds run out); a defence (mode 2) the strongest, without
    /// Rance when he has less than a quarter of his troops (his death ends the game) unless he is the
    /// only one; a search (mode 3) the weakest, so the strong stay for the rebels' turn.
    /// </summary>
    /// <summary><see cref="GameOptions.AutoParty"/>: those of the units given that are free, up to the places; null when none is.</summary>
    private List<int>? ChosenParty(GameState s)
    {
        if (_options.AutoParty is not { } chosen)
            return null;
        List<int> free = [.. chosen.Where(u => u is >= 1 and <= 50 && s.Units[u].Status == 1).Take(Math.Max((int)s.Tmp0040.Value, 1))];
        return free.Count > 0 ? free : null;
    }

    private static List<int> AutoParty(int mode, GameState s)
    {
        var free = Enumerable.Range(1, 50).Where(u => s.Units[u].Status == 1).ToList();
        var strongest = free.OrderByDescending(u => s.Units[u].Troops.Value * s.Units[u].TroopAtk.Value).ThenBy(u => u).ToList();
        if (mode is not (1 or 2))
            return [.. strongest.TakeLast(1)];
        int places = Math.Max((int)s.Tmp0040.Value, 1);
        if (mode == 1)
        {
            var order = free.Contains(1) ? strongest.Where(u => u != 1).Prepend(1) : strongest;
            return [.. order.Take(s.Bt_TargetProv == 6 ? places : Math.Min(places, Math.Max(free.Count - 2, 1)))];
        }
        if (s.Units[1].Troops.Value * 4 < s.Units[1].MaxTroops.Value && strongest.Count > 1)
            strongest.Remove(1);
        return [.. strongest.Take(places)];
    }

    /// <summary>
    /// Where the Monsters' Cave is on the screen when the unattended player is to search it: once, at
    /// the first map wait of stage 1 that finds the week's dungeon action free and the cave's province
    /// the player's (<c>COMMAND.ADV:145-158</c>), before the menu's visits of the stage (the castle may
    /// fall in a few weeks); null otherwise or out of view (the camera centred, the map wait's first read).
    /// </summary>
    private (int X, int Y)? AutoCaveOnScreen(GameState s)
    {
        if (_options.AutoTurnEnd)
            return null;
        if (_autoDungeon != AutoDungeon.NotYet || s.StoryStage != 1 || s.Ui_CmdDungeonAvail != 1
            || s.Provs[s.Dgns[AutoCave].Prov].Owner != 1)
            return null;
        if (GesturePoints.OnScreen(s, s.Dgns[AutoCave].MapX, s.Dgns[AutoCave].MapY) is not { } at)
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
            var at = GesturePoints.PickerCell(id);
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
                steps.Enqueue(() => Click(GesturePoints.PickerButton(2)));
            Pick(unit);
            if (turn)
                steps.Enqueue(() => Click(GesturePoints.PickerButton(2)));
        }
        return steps;
    }

    /// <summary>
    /// The command menu's next item of <see cref="AutoVisits"/> once a week, else its Turn END
    /// (past <see cref="GameOptions.AutoWeeks"/>, or two weeks after <see cref="GameOptions.AutoStage"/>
    /// was reached, the run stops there); the attacked city's menu Attack, then Return; All Data's menu each graph in turn, then Return;
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
        int FindExact(string text)
        {
            for (int i = 0; i < texts.Count; i++)
                if (texts[i].Trim() == text)
                    return i + 1;
            return 0;
        }
        int turnEnd = Find("Turn END");
        if (turnEnd == 0)
        {
            if (_autoReplenish != AutoReplenish.None && Find("Do Nothing") > 0 && Find("Info") > 0)
            {
                // Cﾌﾞｶ's troop menu: Replenish Troops for the unit picked, else (anything unexpected) Do Nothing.
                if (_autoReplenish == AutoReplenish.Picked && Find("○Replenish Troops") is var replenish and > 0)
                {
                    _autoReplenish = AutoReplenish.Asked;
                    return replenish;
                }
                _autoReplenish = AutoReplenish.None;
                return Find("Do Nothing");
            }
            if (_autoReplenish == AutoReplenish.Asked && FindExact("Do") is var yes and > 0 && Find("Don") > 0)
            {
                _autoReplenish = AutoReplenish.None;
                return yes;
            }
            if (_autoAttack != AutoAttack.None && Find("Ninja Maneuver") > 0 && Find("Return") is var leave and > 0)
            {
                // Cﾃｷﾄｼ's menu: the attack once; again (the picker stopped: back to the header), Return.
                if (_autoAttack == AutoAttack.Clicked && Find("Attack") is var attack and > 0)
                {
                    _autoAttack = AutoAttack.Chosen;
                    return attack;
                }
                return leave;
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
        if (_options.AutoStage > 0 && _autoStageWeek < 0 && _presenter.State?.StoryStage.Value == _options.AutoStage)
        {
            _autoStageWeek = week;
            GD.Print($"Kichikuou: --auto reached stage {_options.AutoStage} in week {week}.");
        }
        if (_autoStageWeek >= 0 && week >= _autoStageWeek + 2)
            throw new AutoStopException($"--auto stopped in week {week}, two weeks after stage {_options.AutoStage} was reached (week {_autoStageWeek}).");
        if (_autoReplenish == AutoReplenish.Wanted && Find("Subordinates") is var subordinates and > 0)
        {
            _autoReplenish = AutoReplenish.Chosen;
            return subordinates;
        }
        if (!_options.AutoTurnEnd && week != _autoWeek && _autoVisit < AutoVisits.Length && Find(AutoVisits[_autoVisit]) is var visit and > 0)
        {
            _autoWeek = week;
            _autoVisit++;
            return visit;
        }
        return turnEnd;
    }
}
