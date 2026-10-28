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
using Kichikuou.Game.Battles.Effects;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;
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

    /// <summary>When the first battle ends — with our battle screen's exercise, when its attack's exchange is over.</summary>
    Battle,

    /// <summary>When the first battle ends, whatever our battle screen's exercise: a check of a later turn (a defence, a level-up).</summary>
    BattleEnd,

    /// <summary>When a battle's round <see cref="GameOptions.AutoUntilRound"/> starts (its header), or with the first battle's end.</summary>
    Round,
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

    /// <summary>
    /// Development: at the first enemy attack on a province of the player's, the unattended player sends the
    /// first free ninja to scout (Ninja Scout, <c>FIGT_B.ADV:57</c>) before it intercepts.
    /// </summary>
    public bool AutoScout { get; init; }

    /// <summary>
    /// Development: at the first map wait the unattended player visits the harem once and picks the girl in
    /// this cell of its picker (1-51, left to right, top to bottom; 0: no visit), before its replenish and attack.
    /// </summary>
    public int AutoHarem { get; init; }

    /// <summary>
    /// Development: at the first map wait the unattended player opens Treasure once, before its replenish and attack
    /// (with our interface it clicks through our own screen, <c>GameScreen.AutoTreasureAsync</c>).
    /// </summary>
    public bool AutoTreasure { get; init; }

    /// <summary>
    /// Development (owner's decision 27): the unattended player does not replenish through Subordinates; with Option →
    /// Interface on New it replenishes once a launch through the deployment table's Refill button instead
    /// (<c>GameScreen.AutoRefill</c>).
    /// </summary>
    public bool AutoRefill { get; init; }

    /// <summary>
    /// Development: at the first map wait the unattended player opens the command panel's Save row once, before its
    /// replenish and attack; with our interface it clicks through our Save/Load screen
    /// (<c>GameScreen.AutoSaveLoadAsync</c>).
    /// </summary>
    public bool AutoSaveLoad { get; init; }

    /// <summary>
    /// Development (owner, 2026-10-21): at the first map wait the unattended player searches the Monsters' Cave at once
    /// with Rance (unit 1), before any replenish, and attacks nothing that week (a look at a dungeon battle).
    /// </summary>
    public bool AutoDungeon { get; init; }

    /// <summary>
    /// Development (owner, 2026-10-27; in a window): at the first map wait the unattended player drags the map to its top
    /// through Godot's input as a player drags it, checks that the pressed row stays under the pointer in the map's room
    /// under the strip, dumps <c>map-north</c> and ends the launch (<c>GameScreen.NorthDragAsync</c>).
    /// </summary>
    public bool AutoMapNorth { get; init; }

    /// <summary>
    /// Development (docs/ui/gallery.md, §2): at the first map wait in <c>COMMAND</c> the game is saved into this folder's
    /// <c>slot01.ksav</c> as a manual save there saves it (owner's decision 26: the game as it is now, resuming in
    /// <c>COMMAND</c>), then the launch ends — the screen gallery's kept save of a week's commands.
    /// </summary>
    public string? SaveCommandTo { get; init; }

    /// <summary>Development: unattended play stops after the first deployment picker or the first battle (a look at one screen).</summary>
    public AutoUntil AutoUntil { get; init; }

    /// <summary>Development: the round whose start ends unattended play (<see cref="AutoUntil.Round"/>, <c>--until round-N</c>).</summary>
    public int AutoUntilRound { get; init; }

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

    /// <summary>Where the save slots are (default <see cref="GameData.DefaultSaveDirectory"/>); a Godot path or a folder.</summary>
    public string? SaveDirectory { get; init; }

    /// <summary>
    /// Development: Debug → Portraits opens once the game has booted; with <see cref="ScreenshotDirectory"/>
    /// it saves the units' list there (<see cref="PortraitViewer.SaveUnitsAsync"/>), then the program ends with <see cref="QuitWhenDone"/>.
    /// </summary>
    public bool Portraits { get; init; }

    /// <summary>
    /// Development: Debug → Effects opens once the game has booted; with <see cref="ScreenshotDirectory"/> it saves
    /// the effects' frames there (<see cref="Battles.Effects.EffectViewer.SaveFramesAsync"/>; needs a window),
    /// then the program ends with <see cref="QuitWhenDone"/>.
    /// </summary>
    public bool Effects { get; init; }

    /// <summary>
    /// Development: the options window opens once the game has booted; with <see cref="ScreenshotDirectory"/> in a window it
    /// saves each tab there (<c>options-messages</c>, …), then the program ends with <see cref="QuitWhenDone"/>.
    /// </summary>
    public bool Options { get; init; }

    /// <summary>With <see cref="Effects"/>: the prefixes of the effects whose frames are saved (<c>white-beam-magic</c>, …); null for all.</summary>
    public IReadOnlyCollection<string>? EffectsOnly { get; init; }
}

/// <summary>
/// One game in the original's layout (part G0, owner's decisions 4, 8, 10): a
/// <see cref="GameSession"/> whose view and input are this node behind the proxies, drawn by a
/// <see cref="GamePresenter"/> (its <see cref="GamePresenter.Screen"/> is shown, fades animated) and
/// played with the mouse and keys the way xsystem35 reads them: the input is a state
/// (<see cref="PlayerInput"/>) that <c>IM</c>, <c>Y 3</c>, <c>IK 0</c> and the menus poll once a
/// frame, so the map's clicks and drags and the pickers' hit tests and highlight are the
/// script's own. The battle's input screens work as in <see cref="BattleScreen"/>, fed only the
/// presses that begin while they are open. With our interface, our own deployment screen
/// covers <c>Sﾌﾞﾀｲ</c>'s (<see cref="UpdateDeploymentScreen"/>, docs/ui/deployment-table.md) and
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
    private bool _optionsOpened;                // GameOptions.Options: the options window was opened
    private bool _effectsOpened;                // GameOptions.Effects: Debug → Effects was opened

    private ushort _ikKey;                      // cmdi.c ik_key, repeating: IK 0's key repeat
    private int _repeating;

    private int _cursor = 1;
    private readonly EngineTimer _timer;

    private SceneRunner? _scenes;                       // the session's, for the scenes running now

    private DeploymentTableView _table = null!;         // our own deployment screen's ledger (our interface)
    private BattleBoardView _battleView = null!;        // our own battle screen over the stage (our interface)
    private ScoutReportView _scoutView = null!;         // our own scout report over the original's screen (our interface)
    private StrategyCutInView _strategyView = null!;    // our own cut-in of a successful strategy over the original's screen (New)
    private DeploymentPanels _panels = null!;           // and the rest of it, over the whole of Sﾌﾞﾀｲ's screen
    private UnitPictureView _tablePicture = null!;      // a unit's picture raised by the phone's long press on its row
    private PointerScript _tablePointer = new();        // the script's pointer while the player's is over the table
    private bool _tableActive;                          // our deployment screen takes the pointer: no message window over it
    private MapMarkersView _markers = null!;            // our name plates over the map in COMMAND's map wait (New)
    private CommandsButton _commandsButton = null!;     // the touch mode's Commands button with them
    private ProvinceMenuView _provinceView = null!;     // our province menu over a city file's first menu (New)
    private CommandMenuView _commandView = null!;       // our command panel over COMMAND's command menu (New)
    private readonly LoadSaveShortcut _saveShortcut = new(); // its Load and Save rows: ﾛｰﾄﾞｾｰﾌﾞ's menu answered unseen
    private readonly ConstructionShortcut _buildShortcut = new(); // its Construction: Cﾃｲﾄ's Build / Don't answered unseen
    private StatusStripView _strip = null!;             // our status strip over the map's top on COMMAND's map (New)
    private Control _stripClip = null!;                 // the picture's area, clipping the strip as it slides out
    private WeekReportView _weekView = null!;           // our week report over the week start's windows (New)
    private MessageWindowView _messageView = null!;     // our message windows over WAO_SUB's (New)
    private SceneMenuView _sceneMenuView = null!;       // our ] menus over the selection box (New)
    private FloorLadderView _ladderView = null!;        // our ladder over the dungeon's floor menu (New)
    private SaveLoadView _saveView = null!;             // our Save/Load screen over ﾛｰﾄﾞｾｰﾌﾞ's slot lists (New)
    private ISaveLibrary? _library;                     // the session's saves (decision 29), which our Save/Load screen reads
    private bool _keyWaiting;                           // the script waits for the player's key: our windows show the triangle
    private readonly HashSet<string> _messageDumps = [];        // the kinds of window unattended play has dumped this launch
    private PointerScript _markerPointer = new();       // a plate's still click on its way to the script
    private readonly MapCameraForm _mapCamera = new();  // whether the map loop holds the camera as the view's corner
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
        ReadyHeadroom();
        _markers = new MapMarkersView(GodotGlyphRenderer.Chosen);
        _markers.Pressed += MarkerPressed;
        _content.AddChild(_markers);
        _commandsButton = new CommandsButton();
        _commandsButton.Tapped += CommandsTapped;
        _commandsButton.Forwarded += StripInput;
        var faces = new FacePictures(GameData.Cgs());
        _provinceView = new ProvinceMenuView(GodotGlyphRenderer.Chosen, faces);
        _content.AddChild(_provinceView);
        _commandView = new CommandMenuView(GodotGlyphRenderer.Chosen);
        _content.AddChild(_commandView);
        _content.AddChild(_commandsButton);                 // over the command panel: its Close while the panel is open
        _strip = new StatusStripView(GodotGlyphRenderer.Chosen, faces) { Forward = StripInput };
        _stripClip = new Control { ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };  // the picture: the strip slides out at its top
        _content.AddChild(_stripClip);
        _stripClip.AddChild(_strip);
        _weekView = new WeekReportView(faces);
        _content.AddChild(_weekView);
        _battleView = new BattleBoardView(GodotGlyphRenderer.Chosen, faces);
        _content.AddChild(_battleView);
        _scoutView = new ScoutReportView(GodotGlyphRenderer.Chosen, faces);
        _scoutView.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _content.AddChild(_scoutView);
        ReadyEnemyAttack(faces);
        _strategyView = new StrategyCutInView(GodotGlyphRenderer.Chosen, faces);
        _strategyView.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _content.AddChild(_strategyView);
        _panels =new DeploymentPanels(GodotGlyphRenderer.Chosen, faces);
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
        _panels.Table = _table;                                 // the ledger under the panels' tabs
        _tablePicture = new UnitPictureView();                  // the phone's long press on a row: over the ledger
        _table.RowHeld += unit =>
        {
            if (_presenter.State is { } state)
                _tablePicture.Show(_panels.Picture(state.Units[unit].PortraitCg.Value));
        };
        _content.AddChild(_tablePicture);
        ReadyDeploymentReplenish(faces);                        // over the table: its question and its plate
        _messageView = new MessageWindowView(GodotGlyphRenderer.Chosen)
        {
            LettersPerSecond = TextSpeedChoice.LettersPerSecond(TextSpeedChoice.Step),
            Docks = TouchMode.On,
        };
        _content.AddChild(_messageView);
        _sceneMenuView = new SceneMenuView(GodotGlyphRenderer.Chosen);
        _content.AddChild(_sceneMenuView);                      // over the message windows: the menu asks below them
        _ladderView = new FloorLadderView(GodotGlyphRenderer.Chosen);
        _content.AddChild(_ladderView);
        _saveView = new SaveLoadView(GodotGlyphRenderer.Chosen, faces);
        _content.AddChild(_saveView);                           // over the message windows: they are not drawn under it
        ReadySubordinates(faces);
        ReadyHarem(faces);
        ReadyTreasure(faces);
        ReadySkipButtons();
        ReadyPhone();
        _overlay =OutlinedLabel(VerticalAlignment.Center, 32);
        _notice = OutlinedLabel(VerticalAlignment.Top, 18);

        _audio = new BattleAudio();
        AddChild(_audio);
        _glyphs = new GodotGlyphRenderer();
        var scripts = new ScenarioScripts(GameData.ScenarioDirectory);
        _presenter = new GamePresenter(new ScenarioText(GameData.ScenarioDirectory), GameData.Cgs(),
            _glyphs, scripts, _audio)
        {
            MaskEventCgs = _options.ScreenshotDirectory is not null,
            Log = EngineState.Log,
            AnswersUnseen = () => OrderRunning,                         // an order's windows: answered unseen, not logged
        };
        _skip = EngineState.Skip(SaveDirectory);
        _presenter.ReadBefore = _skip.HasSeen;                          // asked before OnMessage notes it (docs/ui/read-text.md)
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
        UpdateDevice();
        if (FrameTimes.On)
            FrameTimes.Record(BattleScreenShown || _presenter.BattleSceneShows ? "battle" : _presenter.MapWaiting ? "map" : "scene", delta);
        // Ctrl held skips as MsgSkip does (ours); polled, so a release no control passes on is still seen.
        if (_skip is not null)
            _skip.Held = Input.IsKeyPressed(Godot.Key.Ctrl) && GetWindow().HasFocus();
        if (_skip?.IsSkipping == true && _battleView?.Info.Flashing == true)
            _battleView.Info.Hurry();                                   // a defence's ribbon goes at once while skipping
        if (_noticeLeft > 0 && (_noticeLeft -= delta) <= 0)
            _notice.Text = "";
        UpdateDeploymentScreen();
        UpdateSubordinatesScreen(delta);
        UpdateHaremScreen();
        WarmMarks();
        UpdateTreasureScreen();
        AdvanceHeadroom(delta);
        UpdateHeadroom();
        UpdateStatusStrip();
        UpdateMapMarkers();
        if (_commandView.Visible)
            _commandView.Place(PictureArea());
        if (_sceneMenuView.Visible)
            _sceneMenuView.Place(PictureArea());
        if (_ladderView.Visible)
            _ladderView.Place(PictureArea());
        if (_provinceView.Visible)
            _provinceView.Place(PictureArea());
        if (_weekView.Visible)
            _weekView.Place(PictureArea());
        if (_saveView.Visible)
        {
            if (_presenter.SaveScreenOpen)
                _saveView.Place(PictureArea());
            else
                _saveView.Close();                              // the save screen left without its event reaching us
        }
        _optionsView?.Place(PictureArea());
        _logView?.Place(PictureArea());
        _presenter.CoverMessages = InterfaceChoice.New;                  // also when the interface changed with a window open
        UpdateBattleScreen(Present());
        if (UpdateHeadroom())
        {
            UpdateStatusStrip();                                        // the map composed now: its room in this frame
            UpdateMapMarkers();
        }
        UpdateMessageWindow();
        UpdateSkipButtons();
        UpdatePhone();
        UpdateScoutReport();
        UpdateEnemyAttack();
        UpdateStrategyCutIn();
        if (_options.Portraits && !_portraitsOpened && _presenter.State is not null && !_modal)
        {
            _portraitsOpened = true;
            OpenPortraits();
        }
        if (_options.Effects && !_effectsOpened && _presenter.State is not null && !_modal)
        {
            _effectsOpened = true;
            OpenEffects();
        }
        if (_options.Options && !_optionsOpened && _presenter.State is not null && !_modal)
        {
            _optionsOpened = true;
            _ = OptionsLookAsync();
        }
        // A tap the script's reads did not take by now is dropped — but kept while MsgSkip runs, so a click during a
        // fade still stops it at the next wait (STOP_ON_CLICK).
        if (_skip?.IsSkipping != true)
            _input.EndFrame();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMWindowFocusOut)
            _input.Clear();
        else if (what == NotificationWMGoBackRequest)
            BackAsRightClick();
    }

    /// <summary>
    /// Composes the screen and uploads it when it changed; nothing while a scene is skipped whole
    /// (<see cref="SkippingScene"/>); neither while our battle screen covers the view
    /// (<see cref="BattleScreenShown"/>: it composes the screen for its windows' layer itself) or another of our
    /// screens hides it (<see cref="ViewHidden"/>), the change uploaded when the view shows again. Returns whether
    /// the screen changed.
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
        if (ViewHidden)
        {
            _viewStale |= _presenter.TakeChange();                             // composed and uploaded when the view shows again
            return false;
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

    /// <summary>
    /// Whether one of our screens that lie over the whole picture on an opaque backdrop shows (the deployment,
    /// Subordinates, Harem and Treasure screens): the view under it is not seen, so the script's every redraw
    /// under it (a hovered row's details: ~35 ms composed, ~5 ms uploaded) is not composed (owner, 2026-10-24).
    /// </summary>
    private bool ViewHidden => _table.Visible || _subordinates.Visible || _harem.Visible || _treasure.Visible;

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
            var library = new DirectorySaveLibrary(SaveDirectory);  // the ten slots migrated the first time (decision 29)
            _library = library;
            var session = new GameSession(state, EngineState.Random, new ViewProxy(this), new GameInputProxy(this),
                scripts, scripts, new LibrarySlots(library), new UnportedFiles(this));
            session.Script.Library = library;
            session.Script.NewScenesFirst = NewScenesChoice.On;     // Option → Tweaks (decision 30); the options window keeps it current
            session.Script.DemonReach = DemonReachChoice.Chosen;    // Option → Tweaks (decision 32), the same way
            _scenes = session.Scenes;
            _script = session.Script;
            _pickerMarks = new PickerMarks(session.Marks);          // our screens' marks (decision 30)
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
    /// The files of a save folder: the engine's slots (a kept folder) and the save library's (decision 29), the Turn END
    /// auto-saves included (decision 31: left behind, the newest of them was what <c>--load</c> loaded).
    /// </summary>
    private static readonly string[] SaveFilePatterns = ["slot*.ksav", "auto-*.ksav", "end-*.ksav", "save-*.ksav", "persistent.ksav"];

    /// <summary>
    /// <see cref="GameOptions.SlotsFrom"/>: the saves of <paramref name="from"/> — a kept folder's slots, which the library
    /// migrates as it opens the copy, or a library's saves — replace those in <paramref name="to"/> (the saves there
    /// without a copy are removed); none there is an error.
    /// </summary>
    private static void CopySlots(string from, string to)
    {
        string[] slots = System.IO.Directory.Exists(from) ? [.. SaveFilePatterns.SelectMany(p => System.IO.Directory.GetFiles(from, p))] : [];
        if (slots.Length == 0)
            throw new InvalidOperationException($"No save slots to copy in {from}.");
        if (System.IO.Path.GetFullPath(from) == System.IO.Path.GetFullPath(to))
            throw new InvalidOperationException("--slots-from names the run's own save folder.");
        System.IO.Directory.CreateDirectory(to);
        foreach (string old in SaveFilePatterns.SelectMany(p => System.IO.Directory.GetFiles(to, p)))
            System.IO.File.Delete(old);
        foreach (string slot in slots)
            System.IO.File.Copy(slot, System.IO.Path.Combine(to, System.IO.Path.GetFileName(slot)));
        GD.Print($"Kichikuou: {slots.Length} save slot(s) copied from {from}.");
    }

    /// <summary>The save slots' folder (also of the seen messages, <see cref="MessageSkip"/>).</summary>
    private string SaveDirectory =>
        _options.SaveDirectory is { } saves ? ProjectSettings.GlobalizePath(saves) : GameData.DefaultSaveDirectory;

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
        ObserveMapLeft(scriptEvent);
        if (SkippingScene)
        {
            // A scene skipped whole is not drawn: its stage commands (seconds of work for the opening) are dropped.
            if (GamePresenter.IsSkippedStageCommand(scriptEvent))
            {
                _stageSkipped = true;
                return;
            }
        }
        else
        {
            ClearSkippedStage();
        }
        _presenter.CoverCityLeadIn = InterfaceChoice.New;                // our province menu: the city's lead-in hidden too
        _presenter.CoverWeekReport = InterfaceChoice.New;                // our week report over the week start's windows
        _presenter.CoverBriefings = InterfaceChoice.New;                 // an attack or a search goes straight to its picker
        _presenter.Battle.CoverEnemyAttack = InterfaceChoice.New;        // our screen for an enemy attack (FIGT_B)
        _presenter.CoverMessages = InterfaceChoice.New;                  // our message windows (docs/ui/messages.md)
        _presenter.Show(scriptEvent);
        MarkPicker(scriptEvent);                                         // the state as the picker opens (decision 30)
        if (_presenter.WeekReport is { Complete: true } report)
            await WeekReportAsync(report);
        if (scriptEvent is EngagementSceneEvent)
            await WaitForArmiesAsync();
        if (scriptEvent is BattlefieldBackgroundEvent && _presenter.Battle.EnemyDefences != _enemyDefencesSeen)
        {
            _enemyDefencesSeen = _presenter.Battle.EnemyDefences;
            if (BattleScreenShown)
                await GuardAsync(BattleSide.Enemy);                     // the enemy's defence prints nothing: its redraw shows it
        }
        if (scriptEvent is ShotsFrameEvent { Step.Value: 0 } && _options.Auto && !_options.Fast && BattleScreenShown
            && _presenter.Battle.Effect is { } effect && _effectShots.Add(effect))
            _ = EffectShotAsync($"effect-{effect.Kind}-{effect.Side}".ToLowerInvariant());
        if (scriptEvent is MessageEvent message)
            _skip.OnMessage(message.Text);
        else if (scriptEvent is ValueMessageEvent values)
            _skip.OnMessage(values.Text);
        if (scriptEvent is FightEndEvent && _skip.Activated)
        {
            _skip.Activate(false);                                       // ours: the fight is over, what follows is not skipped (owner, 2026-10-26)
            GD.Print("Kichikuou: MsgSkip turned off at the fight's end.");
        }
        if (scriptEvent is MapScreenEvent map)
            _mapCamera.OnMapWait(map.Waiting);
        if (scriptEvent is MapScreenEvent { Waiting: true } && _options.SaveCommandTo is { } saveTo && SaveCommand(saveTo))
            return;
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
        await SubordinatesEventAsync(scriptEvent);
        if (scriptEvent is SaveScreenEvent { Open: false })
        {
            _saveShortcut.Stop();                               // the save screen left, a load's included
            _saveView.Close();
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
    /// Unattended play's dump of an exchange's effect (in a window, without <c>--fast</c>): once a launch for each
    /// effect and side, at its peak (frame 3, 150 ms after the shots' start), <c>effect-melee-player</c> and the like; a
    /// defence's Guard (<see cref="GuardAsync"/>) after <paramref name="after"/>.
    /// </summary>
    private async Task EffectShotAsync(string what, double after = 0.15)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed.TotalSeconds < after && !_closing)
            await NextFrameAsync();
        Shot(what);
    }

    private readonly HashSet<ExchangeEffect> _effectShots = [];

    /// <summary>
    /// A defence's Guard effect on our battle screen (docs/ui/battle-screen.md, "The exchange's effects"), waited for
    /// before the battle goes on (owner, 2026-10-20: with Auto-Def the next turn took the defender off the scene at once);
    /// nothing when it does not play. Unattended play dumps it once a side, at its wall's height
    /// (<c>effect-guard-player</c>, <c>effect-guard-enemy</c>).
    /// </summary>
    private async Task GuardAsync(BattleSide side)
    {
        if (!_battleView.PlayGuard(side))
            return;
        if (_options.Auto)
            GD.Print($"Kichikuou: --auto saw the {side.ToString().ToLowerInvariant()}'s Guard in round {_autoRound}.");
        if (_options.Auto && _guardShots.Add(side))
            _ = EffectShotAsync($"effect-guard-{side}".ToLowerInvariant(), 0.4);
        await DelayAsync(GuardEffect.Seconds);
    }

    private readonly HashSet<BattleSide> _guardShots = [];
    private int _enemyDefencesSeen;

    /// <summary>
    /// Animates a palette fade over its <see cref="FadeRequest.Duration"/>, never skipped, unless
    /// Option → Messages → MsgSkip speeds up fades cuts it short while skipping (<see cref="MessageSkip.FadeDuration"/>);
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
        if ((_optionsView is not null || _logView is not null || _drawer.Visible || _table.Visible || _subordinates.Visible || _harem.Visible || _treasure.Visible || _markers.Visible || _provinceView.Visible || _commandView.Visible || _sceneMenuView.Visible || _ladderView.Visible || _saveView.Visible || _strip.Visible || _weekView.Visible || _messageView.Visible || BattleScreenShown || ScoutReportShown || StrategyCutInShown || _attackView.Visible) && DisplayServer.GetName() != "headless")
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
        var p = Frame().ToScript(local - _content.Position);
        return new Vector2I(Math.Clamp((int)Math.Floor(p.X), 0, Surface.ScreenWidth - 1),
            Math.Clamp((int)Math.Floor(p.Y), 0, Surface.ScreenHeight - 1));
    }

    /// <summary>The centre of script pixel <paramref name="script"/> in this control's coordinates (the inverse of <see cref="ToScript"/>).</summary>
    private Vector2 FromScript(Vector2I script)
    {
        if (_battleView.Visible)
            return _content.Position + _battleView.FromScript(script);
        return _content.Position + Frame().FromScript(script);
    }

    /// <summary>
    /// <c>IZ x, y</c> (<c>cmdi.c</c> → <c>ags_setCursorLocation</c>): nothing when the point lies right
    /// of or below the 3200×800 surface, else it is clamped at 0 and taken relative to the <c>WV</c>
    /// view. With Option → Messages → Mouse movement (<see cref="MouseMovementChoice"/>: on by default as
    /// <c>menu_init</c>, off in the touch mode) the pointer glides there in seven steps 7 ms apart on the
    /// engine's ease-out curve and lands; off, only the position the script reads moves and the script
    /// waits 50 ms. <c>COMMAND.ADV:618</c> puts the pointer back where it was before a menu.
    /// </summary>
    private async Task WarpPointerAsync(int x, int y)
    {
        if (x >= 3200 || y >= 800)
            return;
        x = Math.Max(x, 0) - _presenter.Stage.View.X;
        y = Math.Max(y, 0) - _presenter.Stage.View.Y + _shownShift;    // on the map, where the picture shows that row
        if (!MouseMovementChoice.On || _options.Auto || OrderRunning)  // an order's way puts back a pointer that was never the player's
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
                TouchMoved(motion);
                _battlePointer = motion.Position;
                _input.Position = ToScript(motion.Position);
                PointerMoved();
                break;
            case InputEventMouseButton { ButtonIndex: Godot.MouseButton.Left or Godot.MouseButton.Right } button:
                if (!CardTouch(button))
                    PointerButton(button.Position, button.ButtonIndex == Godot.MouseButton.Left ? KeyReturn : KeySpace, button.Pressed);
                break;
            case InputEventMouseButton { ButtonIndex: Godot.MouseButton.WheelUp, Pressed: true } when CanOpenLog:
                OpenLog();
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
        if (_logView is not null)
        {
            if (key.Pressed && key.Keycode == Godot.Key.Escape)
                _logView.Close();                                           // closes the message log
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

    /// <summary>A mouse button (<see cref="KeyReturn"/> or <see cref="KeySpace"/>) went down or up at <paramref name="at"/>.</summary>
    private void PointerButton(Vector2 at, ushort code, bool pressed)
    {
        _battlePointer = at;
        _input.Position = ToScript(at);
        _input.Button(code, pressed);
        BattleButton(code, pressed);
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
            _presenter.Scene.Hover(_battleView.Visible ? _battleView.PlaceAt(TouchMode.LocalPointer(this) - _content.Position, s) : null);
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
        InterruptPointers();
        _presenter.Flush();
        _presenter.EndCityLeadIn();                                     // whatever the script drew is shown at a key wait
        CheckNoClickLeft();
        ushort key;
        if (SkippingScene)
        {
            await SkipYieldAsync();
            key = 0;
        }
        else if (SubordinatesAnswersKey())
        {
            // An order of our Subordinates screen (docs/ui/subordinates.md): the command's windows on its way are
            // answered at once, as by a click, under the screen — Maris's lines, the result, Rance's thought.
            key = KeyReturn;
            await ReleaseWaitAsync(_input.PeekInput());
            _ikKey = key;
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
        else if (BattleScreenShown && _presenter.Battle.OrderedUnit is { } ordered)
        {
            // Our battle screen does not show the order messages (owner, 2026-10-15): the order's label on the unit's card
            // flashes and the message's click wait is answered at once, as by a click (the key lands in RetVal), after the
            // release of a button still held.
            _battleView.FlashOrder(ordered);
            if (_options.Auto && !_options.Fast)
            {
                await DelayAsync(0.25);                                         // the flash at its height
                await AutoDumpAsync("order-flash");
            }
            key = KeyReturn;
            await ReleaseWaitAsync(_input.PeekInput());
            _ikKey = key;
        }
        else if (BattleScreenShown && _presenter.Battle.InfoBox is { AnsweredAtOnce: true } passed)
        {
            // Our battle screen shows these info boxes elsewhere (docs/ui/messages.md, §5 step 5; owner, 2026-10-19): a defence's
            // ribbon flashes on the unit's card, the end's reason waits for the results plate, a level-up's first page for its
            // row. The wait is answered at once, as by a click (the key lands in RetVal), after the release of a button still held.
            if (passed.Kind == InfoBoxKind.Defence)
            {
                _battleView.Info.Flash(passed, passed.Place is { } at && BattleBoard.Card(_presenter.State!, at) is { } card ? card.Name : "",
                    quick: _skip.IsSkipping);
                await GuardAsync(BattleSide.Player);
                if (_options.Auto && Windowed && _messageDumps.Add("info-defence"))
                {
                    await DelayAsync(0.35);                                     // the badge at its height
                    await AutoDumpAsync("info-defence");
                }
            }
            key = KeyReturn;
            await ReleaseWaitAsync(_input.PeekInput());
            _ikKey = key;
        }
        else if (BattleScreenShown && _presenter.LaterRoundHeader)
        {
            // Our battle screen shows the round header once, as the battle's opening (owner, 2026-10-14): a later round's
            // differs only in the rounds left, which the gauge shows, so its key wait is answered at once, as by a click —
            // the key lands in RetVal (FIGT.ADV:214), which a flavour line reads (BTX-03).
            key = KeyReturn;
            await ReleaseWaitAsync(_input.PeekInput());
            _ikKey = key;
        }
        else if (EnemyAttackAnswersKey)
        {
            // Our enemy attack's screen (docs/ui/enemy-attack.md, §6): the announcement, the ninjas' lead-in, the report
            // and Rance's repeated lines are answered at once, as by a click (RetVal is kept).
            key = KeyReturn;
            await ReleaseWaitAsync(_input.PeekInput());
            _ikKey = key;
        }
        else if (ScoutReportShown && _presenter.Battle.ScoutReported)
        {
            // Our scout report shows the whole army from its first key wait (owner, 2026-10-14): the waits of its
            // commanders' lines and of its totals are answered at once, as by a click (RetVal is kept, FIGT_B.ADV:649-672),
            // after the release of a button still held from the click that answered the first.
            key = KeyReturn;
            await ReleaseWaitAsync(_input.PeekInput());
            _ikKey = key;
        }
        else if (InterfaceChoice.New && (_presenter.Battle.ScoutReplied || _presenter.Battle.ScoutCalling) && _presenter.State is not null)
        {
            // Our scout report comes straight after the ninja's choice and shows Rance's answer and Maris's comment at its
            // foot (owner, 2026-10-16): the windows between the choice and the report (FIGT_B.ADV:393-586) and after it
            // (629, 636-647) are answered at once, as by a click (RetVal is kept), before a frame shows them.
            key = KeyReturn;
            await ReleaseWaitAsync(_input.PeekInput());
            _ikKey = key;
        }
        else if (StrategyCutInShown && _presenter.Battle.StrategyAnswered)
        {
            // Our strategy's cut-in shows the announcement as its title and the effect as its label (owner, 2026-10-15): the
            // key waits of their boxes (window 143) are answered at once, as by a click, after the release of a button still
            // held from the click on the strategist's line — the one wait the player answers.
            key = KeyReturn;
            await ReleaseWaitAsync(_input.PeekInput());
            _ikKey = key;
        }
        else if (BattleScreenShown && _presenter.Battle.ExchangeResultWaits && !_presenter.Battle.ExchangeResultWipedOut)
            key = await ExchangeResultHoldAsync();
        else if (_presenter.WeekReport is { Complete: false })
        {
            // Our week report shows the week's start as one screen (owner, 2026-10-18; docs/ui/turn-start.md): the key waits
            // of the banner's, the revenue's and the expenses' windows are answered at once, as by a click; the player
            // answers the report itself when the window closes (WeekReportAsync).
            key = KeyReturn;
            await ReleaseWaitAsync(_input.PeekInput());
            _ikKey = key;
        }
        else if (_presenter.Briefing)
        {
            // Our interface goes from the province menu's Attack straight to the deployment screen (owner, 2026-10-21) and from
            // a dungeon's Start Search straight to the searcher's picker (owner, 2026-10-24): the briefing's key waits — Rance's
            // "Let's begin the battle" and Maris's two lines (Cﾃｷﾄｼ.ADV:135-171), Rance's "Alright, time to search." and Maris's
            // "…select the unit" (Cﾒｲｷｭｳ.ADV:310-330), the same every time — are answered at once, as by a click (WAO_SUB 1
            // keeps RetVal), after the release of the menu's click.
            key = KeyReturn;
            await ReleaseWaitAsync(_input.PeekInput());
            _ikKey = key;
        }
        else if (_options.Auto)
        {
            if (StrategyCutInShown && !_options.Fast)
                await DelayAsync(0.5);                                          // the cut-in in place
            _keyWaiting = true;
            UpdateMessageWindow();
            UpdateInfoBox();
            if (TouchMode.On && Windowed && !_autoSkipTouched && _messageView.Window is not null && !BattleScreenShown
                && _presenter.Log.Pages.Count > 0)
                await AutoSkipTouchAsync();                                    // the touch mode's Skip and Log, once a launch
            if (Windowed && _presenter.Log.Pages.Count >= 20 && _messageDumps.Add("message-log"))
                await LogDumpAsync();                                           // the message log once a launch
            if (_treasureCallout.Visible && Windowed && _messageDumps.Add("treasure-refusal"))
                await AutoDumpAsync("treasure-refusal");                        // our Treasure screen's callout
            else if (_messageView.Window is { } window && Windowed && _messageDumps.Add(MessageDumpName(window)))
                await AutoDumpAsync(MessageDumpName(window));                   // each kind of our windows once a launch
            else if (BattleScreenShown && _presenter.Battle.InfoBox is { } box && Windowed && _messageDumps.Add(InfoDumpName(box)))
                await AutoDumpAsync(InfoDumpName(box));                         // and of the battle's info boxes
            else
                Shot(StrategyCutInShown ? "strategy" : "key");
            if (!_options.Fast && BattleScreenShown && _presenter.Opening is not null)
                await OpeningAppearShotsAsync();
            await DelayAsync(_options.Fast ? 0 : AutoKeySeconds());
            key = KeyReturn;
        }
        else
        {
            _keyWaiting = true;
            UpdateMessageWindow();
            UpdateInfoBox();
            await FinishTypingAsync();
            Shot("key");
            key = await HitKeyAsync();
        }
        _keyWaiting = false;
        bool opening = BattleScreenShown && _presenter.Opening is not null;
        _presenter.AfterKey();
        if (opening && _options.Auto && !_options.Fast && !_openingShot)
            await OpeningShotAsync();
        if (opening)
            await OpeningLeaveAsync();
        return key;
    }

    /// <summary>
    /// After the opening's click the battle goes on only once the opening has left and the order strip's flags have flown
    /// in (owner, 2026-10-21): else an enemy that acts first starts its turn while they still move, and the player's
    /// banners come up over them. At once while skipping and with <c>--fast</c> (both put the opening and the flags away at once).
    /// </summary>
    private async Task OpeningLeaveAsync()
    {
        do
            await NextFrameAsync();                                             // the view sees the click first
        while (!_battleView.OpeningSettled && BattleScreenShown && !_skip.IsSkipping && !SkippingScene);
    }

    /// <summary>
    /// Our week report (docs/ui/turn-start.md, "The week report"), complete when the week start's window closed: shown
    /// over the map until the player clicks or presses a key — the click the original's windows asked for, which the
    /// script no longer reads (their waits were answered) — and released before the script goes on, so the next wait
    /// finds nothing held. The message skip passes it, as it passes the windows; unattended play dumps it once in a window.
    /// </summary>
    private async Task WeekReportAsync(WeekReport report)
    {
        try
        {
            _weekView.Open(report);
            _weekView.Place(PictureArea());
            if (_skip.IsSkipping || SkippingScene)
                return;
            if (_options.Auto)
            {
                await DelayAsync(_options.Fast ? 0 : 0.5);
                if (!_weekReportDumped && DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null)
                {
                    _weekReportDumped = true;
                    await AutoDumpAsync("week-report");
                }
                return;
            }
            Shot("week-report");
            for (ushort held = await KeyWaitAsync(double.PositiveInfinity, cancelable: true, skippable: true); held != 0;)
                held = await KeyWaitAsync(0.05, cancelable: false);
        }
        finally
        {
            _weekView.Close();
            _presenter.EndWeekReport();
        }
    }

    private bool _weekReportDumped;

    /// <summary>
    /// Our window's text typing at a key wait (docs/ui/messages.md, §5 step 4): the wait begins once it is whole. A click or
    /// a key while it types shows it whole and is not passed to the script — the next one answers the wait, so the
    /// press is released first; the message skip shows it whole at once.
    /// </summary>
    private async Task FinishTypingAsync()
    {
        while (_messageView.Visible && _messageView.Typing)
        {
            if (_skip.IsSkipping)
            {
                _messageView.Complete();
                return;
            }
            await NextFrameAsync();
            if (_input.ReadInput() is var held and not 0)
            {
                _messageView.Complete();
                while (held != 0)
                    held = await KeyWaitAsync(0.05, cancelable: false);
                return;
            }
        }
    }

    /// <summary>
    /// Our message windows (docs/ui/messages.md, step 1; our interface) over the picture: the window the presenter
    /// leaves out (<see cref="GamePresenter.MessageWindow"/>), the triangle while the player's key is awaited, a menu open
    /// over it shown again on top; in a battle its own windows (the results after the field, the captures); hidden while a scene is skipped whole.
    /// </summary>
    private void UpdateMessageWindow()
    {
        var window = InterfaceChoice.New && !SkippingScene ? _presenter.MessageWindow : null;
        if (UpdateTreasureCallout(window))
            window = null;                              // at the clicked cell over our Treasure screen instead
        _messageView.Show(window, _keyWaiting, _texture, window is null ? null : _presenter.MenuArea);
        if (window is null)
            return;
        if (_skip.IsSkipping || _options.Auto)
            _messageView.Complete();                    // the message skip and unattended play do not wait for the typing
        _messageView.Place(PictureArea());
        _messageView.Fade(_presenter.FadeAmount);
    }

    /// <summary>
    /// <see cref="GameOptions.SaveCommandTo"/>: in <c>COMMAND</c> (<see cref="ScriptContext.SaveHere"/> set), the game saved as
    /// <c>LoadSaveScreen</c>'s manual save there saves it — the week in <c>Tmp0021</c>, resuming at <c>SaveHere</c> — into
    /// the folder's slot 1, then the launch ends. False outside <c>COMMAND</c>.
    /// </summary>
    private bool SaveCommand(string folder)
    {
        if (_script?.SaveHere is not { } here || _presenter.State is not { } s)
            return false;
        s.Tmp0021 = s.Week;
        var data = SaveData.Capture(s, here).Commented("COMMAND (--save-command)");
        string path = ProjectSettings.GlobalizePath(folder);
        System.IO.Directory.CreateDirectory(path);
        bool written = new DirectorySaveStore(path).Write(1, data);
        GD.Print($"Kichikuou: --save-command {(written ? "saved" : "could not save")} week {s.Week.Value} "
            + $"(gold {s.Gold.Value}) in COMMAND into {path}.");
        _tree.Quit(written ? 0 : 1);
        return true;
    }

    /// <summary>Whether unattended play can dump our screens: in a window, with a folder for the shots.</summary>
    private bool Windowed => DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null;

    /// <summary>The battle's info box on our battle screen (docs/ui/messages.md, §5 step 5), the triangle while the player's key is awaited.</summary>
    private void UpdateInfoBox() => _battleView.Info.Show(BattleScreenShown ? _presenter.Battle.InfoBox : null, _keyWaiting);

    /// <summary>Unattended play's dump of a kind of the battle's info boxes: <c>info-panel</c>, <c>info-results</c>, <c>info-level-up</c>.</summary>
    private static string InfoDumpName(BattleInfoBox box) => "info-" + box switch
    {
        { Kind: InfoBoxKind.Results, Results.LevelUps.Count: > 0 } => "level-up",
        { Kind: InfoBoxKind.Results } => "results",
        _ => "panel",
    };

    /// <summary>
    /// Unattended play's dump of a kind of our windows: <c>message-speech-left</c> and the like; 131 as
    /// <c>message-cg-bar-speech</c>, <c>-thought</c> or <c>-narration</c>.
    /// </summary>
    private static string MessageDumpName(MessageWindow window) => "message-" + window.Kind switch
    {
        MessageKind.SpeechLeft => "speech-left",
        MessageKind.SpeechRight => "speech-right",
        MessageKind.ThoughtLeft => "thought-left",
        MessageKind.ThoughtRight => "thought-right",
        MessageKind.PlaceTitle => "place-title",
        MessageKind.CgBar => window.Speaker.Length == 0 ? "cg-bar-narration" : window.Thinks ? "cg-bar-thought" : "cg-bar-speech",
        _ => "narration",
    };

    /// <summary>
    /// How long unattended play without <c>--fast</c> holds a key wait: over a battle's field or exchange 0.8 s, so they can be
    /// watched (a strategy's cut-in too); the characters' lines — every other message (the turn's events, the scenes a battle
    /// plays before its field) and a battle's speech — only long enough to be seen (owner, 2026-10-16).
    /// </summary>
    private double AutoKeySeconds() =>
        StrategyCutInShown || _presenter.BattleSceneShows && _presenter.Battle.Speech is null ? 0.8 : 0.15;

    /// <summary>
    /// The exchange's result screen without a wiped-out mark (owner, 2026-10-14): our screen shows the result in the scene
    /// and runs the cards' counters from it, so instead of the click (<c>IK 0</c>, <c>FIGTATTK.ADV:247</c>) it holds for
    /// Option → Battle → "Results: hold" and goes on as by a click — the key lands in RetVal (BTX-03). A key or a
    /// click ends the hold sooner and is the key, as <c>IK 0</c> returns it. At once with <c>--fast</c>; under the message
    /// skip at once with 0, as <c>IK 0</c>.
    /// </summary>
    private async Task<ushort> ExchangeResultHoldAsync()
    {
        Shot("key");
        double seconds = _options.Fast ? 0 : BattleScreenSettings.Get(BattleScreenSettings.ResultSeconds);
        ushort key = await KeyWaitAsync(seconds, cancelable: !_options.Auto, skippable: true);
        if (_skip.IsSkipping)
            return 0;
        if (key == 0 || _options.Auto)
            key = KeyReturn;
        await ReleaseWaitAsync(_options.Auto ? (ushort)0 : key);
        _ikKey = key;
        return key;
    }

    /// <summary>
    /// Unattended play's dumps of the battle's opening fading in (in a window, once a launch): halfway and whole; the
    /// key wait's own dump, just before, shows the field alone.
    /// </summary>
    private async Task OpeningAppearShotsAsync()
    {
        if (_openingShot)
            return;
        double seconds = BattleScreenSettings.Get(BattleScreenSettings.OpeningAppearSeconds);
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed.TotalSeconds < seconds / 2 && !_closing)
            await NextFrameAsync();
        Shot("opening-appearing");
        while (_battleView.OpeningShown < 1 && BattleScreenShown && !_closing)
            await NextFrameAsync();
        Shot("opening");
        double cards = BattleScreenSettings.Get(BattleScreenSettings.CardsSlideSeconds) * 0.8;   // the cards on their way in
        while (watch.Elapsed.TotalSeconds < cards && !_closing)
            await NextFrameAsync();
        Shot("cards-sliding");
    }

    /// <summary>
    /// Unattended play's dumps of the battle's opening leaving: once a launch, halfway (in a window), and the order strip's
    /// flags flying in, the fourth on its way.
    /// </summary>
    private async Task OpeningShotAsync()
    {
        _openingShot = true;
        var watch = Stopwatch.StartNew();
        double half = BattleScreenSettings.Get(BattleScreenSettings.OpeningSeconds) / 2;
        while (watch.Elapsed.TotalSeconds < half && !_closing)
            await NextFrameAsync();
        Shot("opening-leaving");
        double flags = BattleScreenSettings.Get(BattleScreenSettings.StripFlySeconds) * 3.5;
        while (watch.Elapsed.TotalSeconds < flags && !_closing)
            await NextFrameAsync();
        Shot("strip-flying");
    }

    private bool _openingShot;

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
        InterruptPointers();
        _presenter.Flush();
        _presenter.EndCityLeadIn();                                     // and at a pause
        if (SkippingScene)
        {
            await SkipYieldAsync();
            return Sys3Int.Zero;
        }
        if (InterfaceChoice.New && _presenter.Battle.ReturnsToAttack)
            return Sys3Int.Zero;                                        // the map's scroll and blink under our attack screen after Back
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
        InterruptPointers();
        if (SubordinatesChoice(items) is { } ordered)
            return ordered;                                     // an order of our Subordinates screen answers it, unseen
        if (_saveShortcut.Choose(items) is { } list)
            return list;                                        // our panel's Load or Save row answers Load / Save / Don't, unseen
        if (_buildShortcut.Choose(items) is { } build)
            return build;                                       // our panel's Construction answers Build / Don't, unseen
        _skip.OnMenu();
        _messageView.Complete();                                // the question is whole under its menu
        _presenter.OpenMenu(items);
        _presenter.Pointer(_input.Position.X, _input.Position.Y);
        // Our Save/Load screen in either interface (decision 29: the original's slot lists are not kept).
        var saveList = _presenter.SaveScreenOpen && _presenter.SaveLibraryList is { } shelf ? shelf.Save ? SaveList.Save : SaveList.Load : (SaveList?)null;
        var province = InterfaceChoice.New && saveList is null ? _presenter.ProvinceMenu : null;
        var command = InterfaceChoice.New && province is null && saveList is null ? _presenter.CommandMenu : null;
        var ladder = InterfaceChoice.New && province is null && command is null && saveList is null ? _presenter.FloorLadder : null;
        // The dungeon's menu before a search on the touch rail (docs/ui/dungeon-menu.md, §6); the PC keeps our ] menu.
        var entry = InterfaceChoice.New && TouchMode.On && province is null && command is null && saveList is null && ladder is null
            ? _presenter.DungeonEntry : null;
        if (saveList is { } shownList)
        {
            _presenter.SaveScreenCovered = true;                // our Save/Load screen (docs/ui/save-load.md, §5)
            OpenSaveView(shownList);
        }
        else if (province is not null)
        {
            _presenter.MenuCovered = true;                      // our panel instead (docs/ui/command-screen.md)
            _provinceView.Open(province, _presenter.State is { } state
                ? ProvinceMenuView.PointOf(state, province.Province, MapCornerDrawn) : null);
            _provinceView.Place(PictureArea());
        }
        else if (command is not null)
        {
            _presenter.MenuCovered = true;                      // our command panel, the status window with it
            _commandView.Open(command);
            _commandView.Place(PictureArea());
            UpdateStatusStrip();
        }
        else if (ladder is not null)
        {
            _presenter.SceneMenuCovered = true;                 // our ladder right of the picture (docs/ui/dungeon-menu.md)
            _presenter.Invalidate();
            _ladderView.Open(ladder);
            _ladderView.Place(PictureArea());
        }
        else if (entry is not null)
        {
            _presenter.SceneMenuCovered = true;                 // the touch rail in the lower right corner
            _presenter.Invalidate();
            _ladderView.OpenEntry(entry);
            _ladderView.Place(PictureArea());
        }
        // Our ] menu at the selection box's place (docs/ui/messages.md, §5 step 3); a battle's menus stay the original's
        // with the battle's windows (step 5).
        bool scene = province is null && command is null && ladder is null && entry is null && saveList is null && InterfaceChoice.New && !BattleScreenShown
            && _presenter.Menu is not null && _presenter.MenuBoxArea is not null;
        // Over our Treasure screen the question's window is a callout at the clicked cell, its Yes/No inside it
        // (docs/ui/pickers.md, round W): opened with its buttons at once.
        bool callout = scene && TreasureCalloutFor(InterfaceChoice.New && !SkippingScene ? _presenter.MessageWindow : null);
        if (scene)
        {
            _presenter.SceneMenuCovered = true;
            _presenter.Invalidate();
            if (callout)
            {
                _treasureCallout.OpenMenu(_presenter.Menu!.Items.Select(i => i.Text).ToList());
                UpdateMessageWindow();
            }
            else
            {
                _sceneMenuView.Open(_presenter.Menu!, _presenter.MenuBoxArea!.Value);
                _sceneMenuView.Place(PictureArea());
            }
        }
        Shot("menu");
        try
        {
            if (_options.Auto)
            {
                await DelayAsync(_options.Fast ? 0 : 0.5);
                if (province is not null && !_provinceDumped && DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null)
                {
                    _provinceDumped = true;
                    await AutoDumpAsync("province-menu");
                }
                if (command is not null && !_commandDumped && DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null)
                {
                    _commandDumped = true;
                    await AutoDumpAsync("command-menu");
                }
                if (ladder is not null && Windowed && _messageDumps.Add("floor-ladder"))
                    await AutoDumpAsync("floor-ladder");
                if (entry is not null && Windowed && _messageDumps.Add("dungeon-entry"))
                    await AutoDumpAsync("dungeon-entry");
                if (callout && Windowed && _messageDumps.Add("treasure-question"))
                    await AutoDumpAsync("treasure-question");
                else if (scene && !callout && !_sceneMenuDumped && DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null)
                {
                    _sceneMenuDumped = true;
                    await AutoDumpAsync("scene-menu");
                }
                if (saveList is { } autoList)
                    return Answered(await AutoSaveLoadAsync(autoList, items));
                if (TouchMode.On && Windowed
                    && (command is not null && !_autoCommandCloses || province is { CancelReopens: false } && !_autoProvinceOutside))
                    await AutoTouchDismissAsync(command is not null);
                return Answered(AutoChoice(items));
            }
            return Answered(saveList is not null ? await SaveAnswerAsync()
                : province is not null ? await _provinceView.ChooseAsync()
                : command is not null ? await CommandAnswerAsync(command)
                : ladder is not null || entry is not null ? await _ladderView.ChooseAsync()
                : callout ? await _treasureCallout.ChooseAsync()
                : scene ? await _sceneMenuView.ChooseAsync()
                : await MenuAsync());
        }
        finally
        {
            _provinceView.Close();
            _commandView.Close();
            _saveView.Release();                                // drawn on until the next list or the screen's end
            _sceneMenuView.Close();
            _ladderView.Close();
            _treasureCallout.CloseMenu();
            _presenter.CloseMenu();
        }
    }

    /// <summary>
    /// Our command panel's answer; a Load or Save row starts its list (<see cref="LoadSaveShortcut"/>), Construction its
    /// Build (<see cref="ConstructionShortcut"/>).
    /// </summary>
    private async Task<int> CommandAnswerAsync(CommandMenu command)
    {
        var (choice, opens) = await _commandView.ChooseAsync();
        _saveShortcut.Start(opens);
        _buildShortcut.Start(command.Commands.Any(c => c.Choice == choice && c is { Action: CommandAction.Build, Enabled: true }));
        return choice;
    }

    /// <summary>
    /// Our Save/Load screen over <paramref name="list"/>: the library's saves read again (after a save the script lists them
    /// again, the one just written chosen), the game now for a save; the tabs while our panel's row runs the screen
    /// (<see cref="LoadSaveShortcut"/>).
    /// </summary>
    private void OpenSaveView(SaveList list)
    {
        var library = _library ?? new MemorySaveLibrary();
        var now = list == SaveList.Save && _presenter.State is { } s ? SaveSlots.Now(s) : null;
        _saveView.Open(list, _presenter.SaveLibraryList?.Items ?? [], library, now, _script?.LastSave, _saveShortcut.Running);
        _saveView.Place(PictureArea());
    }

    /// <summary>Our Save/Load screen's answer; a tab leaves the list for the other (<see cref="LoadSaveShortcut.Switch"/>).</summary>
    private async Task<int> SaveAnswerAsync()
    {
        var (choice, other) = await _saveView.ChooseAsync();
        if (other is { } list)
            _saveShortcut.Switch(list);
        return choice;
    }

    /// <summary>The menu's answer, into the message log when it asked over a window (docs/ui/messages.md, §5 step 6).</summary>
    private int Answered(int answer)
    {
        _presenter.MenuAnswered(answer);
        return answer;
    }

    private MessageLogView? _logView;
    private bool _autoMapLogDone;                       // unattended play's wheel over the map done (once a launch)
    private bool _autoMapWheel;                         // that wheel on its way

    /// <summary>
    /// The wheel up opens the message log (docs/ui/messages.md, §5 step 6; owner, 2026-10-19) while the player is asked for a
    /// key and on <c>COMMAND</c>'s map while it waits (owner, 2026-10-26), in either interface; not in unattended play (but its
    /// own wheel over the map, <see cref="AutoMapLogAsync"/>), nor over a dialog or the log itself.
    /// </summary>
    private bool CanOpenLog => (!_options.Auto || _autoMapWheel) && LogOpens;

    /// <summary>
    /// Whether the message log opens now: <see cref="LogReachable"/>, and on the map nothing held — a drag's release under
    /// the log would be a click on the map.
    /// </summary>
    private bool LogOpens => LogReachable && (_keyWaiting || _input.PeekInput() == 0);

    /// <summary>
    /// Whether the message log is there to open (the touch mode's Log lit): the player's key is awaited or the map waits
    /// (<see cref="MapLogWait"/>), and on the phone held landscape the week report (<see cref="PhoneWeekReport"/>, LW1),
    /// something logged.
    /// </summary>
    private bool LogReachable => (_keyWaiting || MapLogWait || PhoneWeekReport) && !_modal && _logView is null && _presenter.Log.Pages.Count > 0;

    /// <summary><c>COMMAND</c>'s map loop waits for the player with nothing over the map: no menu, window, plate's click or troop order under way.</summary>
    private bool MapLogWait => _presenter.MapWaiting && _presenter.Menu is null && !_presenter.MessageWindowShown
        && _markerPointer.Pending == 0 && !OrderRunning;

    /// <summary>The message log over the game, which waits while it is open (as under Option → Messages).</summary>
    private void OpenLog()
    {
        _modal = true;
        _input.Clear();
        var view = new MessageLogView(GodotGlyphRenderer.Chosen, _presenter.Log);
        view.Closed += () =>
        {
            _logView = null;
            _modal = false;
            _input.Clear();
        };
        _logView = view;
        _content.AddChild(view);
        view.Place(PictureArea());
        Shot("message-log");
    }

    /// <summary>Development (<see cref="GameOptions.Options"/>): the options window, each tab dumped in a window; then the end with <c>--quit</c>.</summary>
    private async Task OptionsLookAsync()
    {
        if (OpenOptions() is not { } view)
            return;
        if (DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null)
        {
            foreach (var tab in Enum.GetValues<OptionsView.Tab>())
            {
                view.ShowTab(tab);
                await FramesAsync(tab == OptionsView.Tab.Messages ? 1.0 : 0.3);   // the preview types a while
                Shot("options-" + tab.ToString().ToLowerInvariant());
                await FramesAsync(0.1);
            }
            view.Close();
        }
        if (_options.QuitWhenDone)
            _tree.Quit();

        // Not NextFrameAsync: it waits while the window, a modal one, is open.
        async Task FramesAsync(double seconds)
        {
            var watch = Stopwatch.StartNew();
            do
                await ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            while (watch.Elapsed.TotalSeconds < seconds);
        }
    }

    /// <summary>Unattended play in a window: the message log opened at a key wait, dumped and closed again.</summary>
    private async Task LogDumpAsync()
    {
        var view = new MessageLogView(GodotGlyphRenderer.Chosen, _presenter.Log);
        _content.AddChild(view);
        view.Place(PictureArea());
        await DelayAsync(0.3);
        await AutoDumpAsync("message-log");
        view.Close();
    }

    /// <summary>
    /// Unattended play in a window, once a launch, at the first map wait with something logged (the week-26 save's, after
    /// Lia's scene), through Godot's input: the wheel up over the map's middle opens the message log, a right click closes
    /// it — in the touch mode the drawer at the map's left edge (dump "touch-map-drawer"), a tap on its Log, a tap off the
    /// log's frame; the log dumped ("message-log-map"), the map still waiting and nothing reached the script's input.
    /// Anything else is an error.
    /// </summary>
    private async Task AutoMapLogAsync()
    {
        _autoMapLogDone = true;
        var area = PictureArea();
        var middle = _content.GlobalPosition + area.GetCenter();
        string way = TouchMode.On ? "tap on the drawer's Log" : "wheel up";
        if (TouchMode.On)
        {
            UpdateSkipButtons();
            await FrameAsync();
            if (!_drawer.Visible || _drawer.LogDisabled)
                throw new InvalidOperationException("--auto found the touch mode's Skip and Log drawer hidden or Log greyed on the waiting map.");
            Shot("touch-map-drawer");
            await FrameAsync();
            await ClickAsync(_drawer.LogCentre, Godot.MouseButton.Left);
        }
        else
        {
            _autoMapWheel = true;
            try
            {
                await ClickAsync(middle, Godot.MouseButton.WheelUp);
            }
            finally
            {
                _autoMapWheel = false;
            }
        }
        if (_logView is null)
            throw new InvalidOperationException($"--auto's {way} over the waiting map opened no message log.");
        for (int i = 0; i < 20; i++)
            await FrameAsync();                                         // the log in place
        Shot("message-log-map");
        await FrameAsync();
        if (TouchMode.On)
            await ClickAsync(_logView!.OutsidePoint, Godot.MouseButton.Left);
        else
            await ClickAsync(middle, Godot.MouseButton.Right);
        if (_logView is not null || _modal)
            throw new InvalidOperationException("--auto did not close the message log over the map.");
        if (!_presenter.MapWaiting || _input.PeekInput() != 0)
            throw new InvalidOperationException($"--auto's {way} and the log's close over the map reached the script's input.");
        GD.Print($"Kichikuou: --auto opens the message log on the map by the {way}.");

        // Frames that pass while the log, a modal view, is open (NextFrameAsync waits for its close).
        async Task FrameAsync() => await ToSignal(_tree, SceneTree.SignalName.ProcessFrame);

        async Task ClickAsync(Vector2 at, Godot.MouseButton button)
        {
            SendPointerMotion(at);
            await FrameAsync();
            foreach (bool pressed in (bool[])[true, false])
            {
                SendPointerButton(at, button, pressed);
                await FrameAsync();
            }
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

    /// <summary>
    /// Another input came between the pointer reads (a key wait, a pause, a menu): every queue of gestures
    /// settles again — unattended play's and our Subordinates screen's alike. A queue left unsettled starts its
    /// next gesture too early: the map's right click that opens the command again after a troop order was read
    /// by <c>WAO_MAP</c>'s own read instead of the map's wait, and the order never ended (2026-10-18).
    /// </summary>
    private void InterruptPointers()
    {
        _pointer.Interrupt();
        _subPointer.Interrupt();
        _treasurePointer.Interrupt();
    }

    public async Task<MouseState> GetMouseAsync()
    {
        _presenter.Flush();
        if (_options.Auto)
        {
            if (_pointer.Pending == 0)
                _wantedOrder = null;                    // the script asks again with nothing queued: a drop's clicks are done
            UpdateDeploymentScreen();
            if (await NorthDragReadAsync() is { } dragged)
                return Read(dragged);                   // --map-north: the drag's pointer is Godot's
            if (UpdateMapMarkers() && !_markersDumped && DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null)
                await DumpMarkersAsync();
            if (!_autoMapLogDone && Windowed && _pointer.Pending == 0 && MapLogWait && LogOpens)
                await AutoMapLogAsync();                // the message log on the map, once a launch
            if (_autoPlateTarget != 0 && UpdateMapMarkers() && _mapCamera.Corner)
                await AutoPlateClickAsync();
            if (_autoCommandsTap && UpdateMapMarkers() && _commandsButton.Visible)
                await AutoCommandsTapAsync();
            if (_markerPointer.Pending > 0)
            {
                var click = _markerPointer.Next(out bool clickRead);
                if (!clickRead)
                    await NextFrameAsync();
                return Read(click);
            }
            if (!await AutoTreasureAsync())
                AutoPointer();                          // over our Treasure screen it clicks through the screen
            var queued = _pointer.Next(out bool read);
            if (!read)
                await NextFrameAsync();                 // the scripts poll the pointer: one read per frame
            return Read(queued);
        }
        await NextFrameAsync();
        if (UpdateSubordinatesScreen())
            return Read(SubordinatesRead());            // over our Subordinates screen the script's pointer is the screen's
        if (UpdateHaremScreen())
            return Read(HaremRead());                   // likewise over our Harem screen
        if (UpdateTreasureScreen())
            return Read(TreasureRead());                // and over our Treasure screen
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
        if (UpdateMapMarkers() && _markerPointer.Pending > 0)
            return Read(_markerPointer.Next(out _));    // a plate's still click
        return Read(PlayerRead());
    }

    /// <summary>A mouse read handed to the script, told to <see cref="MapCameraForm"/>.</summary>
    private MouseState Read(MouseState read)
    {
        _mapCamera.OnMouseRead(read.Button.Value);
        return read;
    }

    // ---------------------------------------------------------------- our map's markers (docs/ui/command-screen.md)

    /// <summary>
    /// Shows our name plates over the map while <c>COMMAND</c> waits on it with our interface on,
    /// but not under a message window or a menu; places them over the picture as the window is sized and
    /// the map scrolls — at the view the screen shows (<see cref="GamePresenter.MapViewShown"/>), not at
    /// the camera, whose form changes under an unchanged picture: a still click centres it, and a click
    /// on no town starts the wait again with it centred until the release wait's read. The markers are
    /// read from the state when they appear (owners change only off the map). Returns whether they show.
    /// Shown or hidden, no plate's click stays queued.
    /// </summary>
    /// <summary>
    /// The screen's picture in <c>_content</c>'s coordinates: where <c>_view</c> shows it, the 1280×800 screen scaled to
    /// fit the view and centred (on a phone held landscape the view is the phone's frame, <see cref="UpdateDevice"/>).
    /// </summary>
    private ScreenFrame Frame() => ScreenFrame.Fit(new Rect2(_view.Position, _view.Size));

    /// <summary>The screen's picture in <c>_content</c>'s coordinates (<see cref="Frame"/>).</summary>
    private Rect2 PictureArea() => Frame().Area;

    private bool _provinceDumped, _commandDumped, _sceneMenuDumped;

    /// <summary>
    /// Our status strip (docs/ui/command-screen.md, "The status strip") lies over the picture's top, as wide as
    /// it, while the screen shows <c>COMMAND</c>'s map (owner, 2026-10-27; before, only with our command panel, where
    /// the original prints its status window; 2026-10-17: a band of its own above the map lived apart from the map),
    /// the map's room under it (<see cref="UpdateHeadroom"/>); it slides into the picture as the screen comes to the map and
    /// out of it as it leaves (<see cref="StripIn"/>).
    /// </summary>
    private void UpdateStatusStrip()
    {
        if (PhoneLandscape)
        {
            // The phone (docs/ui/phone-build.md, step L2): the status in the black sides with the map, no slide; what the
            // sides hold follows what is open over the map (PhoneMap).
            if (_mapStrip && _presenter.StatusStrip is { } shown)
            {
                _stripClip.Position = Vector2.Zero;
                _stripClip.Size = _content.Size;
                _strip.Show(shown);
                _strip.Out = 0;
                _strip.Mode = _commandView.Visible ? StatusStripView.SideMode.Commands
                    : _provinceView.Visible ? StatusStripView.SideMode.Province : StatusStripView.SideMode.Map;
                _strip.Place(new Rect2(Vector2.Zero, _content.Size));          // its parent's whole area
            }
            else
            {
                _strip.Close();
            }
            return;
        }
        if (_slide > 0 && _presenter.StatusStrip is { } strip)
        {
            var picture = PictureArea();
            _stripClip.Position = picture.Position;
            _stripClip.Size = picture.Size;
            _strip.Show(strip);
            _strip.Out = 1 - StripIn;
            _strip.Place(new Rect2(Vector2.Zero, new Vector2(picture.Size.X, StatusStripView.Height * picture.Size.X / Surface.ScreenWidth)));
        }
        else
        {
            _strip.Close();
        }
    }

    /// <summary>The strip's events but a left click's, as the screen's own (a right click opens the menu).</summary>
    private void StripInput(InputEvent e)
    {
        if (e.Duplicate() is InputEventMouse mouse)
        {
            mouse.Position = GetGlobalTransformWithCanvas().AffineInverse() * mouse.GlobalPosition;
            _GuiInput(mouse);
        }
    }

    private bool UpdateMapMarkers()
    {
        bool show = InterfaceChoice.New && _presenter.MapWaiting && _mapCamera.Waiting
            && !_presenter.MessageWindowShown && _presenter.Menu is null && !SkippingScene && _presenter.State is not null
            && _presenter.MapViewShown is not null;
        if (show != _markers.Visible)
        {
            _markers.Visible = show;
            _markerPointer = new PointerScript();
            if (show)
                _markers.Load(_presenter.MapMarkers());
        }
        // The touch mode's button: Commands on the map, Close while our command panel is open (owner, 2026-10-24).
        _commandsButton.Visible = TouchMode.On && (show || _commandView.Visible);
        _commandsButton.Closes = _commandView.Visible;
        var area = PictureArea();
        if (_commandsButton.Visible)
            _commandsButton.Place(area);
        if (!show)
            return false;
        _markers.SetCoveredTop(_strip.Visible && !PhoneLandscape ? MapHeadroom.Strip : 0);  // the phone's status is in the sides
        _markers.Place(area, area.Size.X / Surface.ScreenWidth, MapCornerDrawn!.Value);
        return true;
    }

    /// <summary>
    /// A plate clicked: the still click the original expects at its marker's point, unless one is on its way
    /// or the map loop does not hold the camera as the view's corner (<see cref="MapCameraForm"/>), which
    /// the click's point is reckoned from.
    /// </summary>
    private void MarkerPressed(MapMarker marker)
    {
        if ((!_options.Auto || _autoPlateClicking) && _markerPointer.Pending == 0 && _markers.Visible && _mapCamera.Corner
            && _presenter.State is { } s)
            MapMarkers.Press(s, marker, _markerPointer);
    }

    private bool _markersDumped;
    private int _autoPlateTarget;                       // the province --auto attacks through its plate, once a launch
    private bool _autoPlateDone, _autoPlateClicking;

    /// <summary>
    /// Unattended play with <c>--interface new</c> in a window, once a launch: the week's attack target clicked
    /// through its plate as a player clicks it — the system pointer onto the plate (dump
    /// <c>map-marker-hover</c>), a press and a release through Godot's input — so the plate queues its
    /// still click, which the script reads as the original's click on the province.
    /// </summary>
    private async Task AutoPlateClickAsync()
    {
        int target = _autoPlateTarget;
        _autoPlateTarget = 0;
        _autoPlateDone = true;
        if (_markers.PlateOf(MapHitKind.Province, target) is not var (marker, at))
            throw new InvalidOperationException($"--auto finds no plate of province {target} on the map.");
        GetViewport().WarpMouse(at);
        SendPointerMotion(at);                                                                  // the warp alone moves no hover
        await NextFrameAsync();
        await NextFrameAsync();
        if (TouchMode.NoHover)
        {
            // --touch on PC: a finger hovers nothing (NoHoverFilter), the tap alone reaches the plate.
            if (_markers.Hovered is not null)
                throw new InvalidOperationException($"--touch's pointer, moved with no button down, hovers the plate of {_markers.Hovered.Name}.");
        }
        else
        {
            if (_markers.Hovered != marker)
                throw new InvalidOperationException($"--auto's pointer on the plate of {marker.Name} does not hover it.");
            await AutoDumpAsync("map-marker-hover");
        }
        _autoPlateClicking = true;
        try
        {
            foreach (bool pressed in (bool[])[true, false])
            {
                SendPointerButton(at, Godot.MouseButton.Left, pressed);
                await NextFrameAsync();
            }
        }
        finally
        {
            _autoPlateClicking = false;
        }
        if (_markerPointer.Pending == 0)
            throw new InvalidOperationException($"--auto's click on the plate of {marker.Name} queued no click for the script.");
        GD.Print($"Kichikuou: --auto clicks the plate of {marker.Name} (province {target}).");
    }

    /// <summary>
    /// Unattended play in a window with <c>--shots</c>: once a launch, the first map wait's plates
    /// (<c>map-markers</c>), then the same with Leazas Castle (1) shown hovered (<c>map-markers-hover</c>),
    /// then without the dimmed plates (<c>map-markers-active</c>, Debug → Hide Dimmed Map Names).
    /// </summary>
    private async Task DumpMarkersAsync()
    {
        _markersDumped = true;
        await AutoDumpAsync("map-markers");
        _markers.ShowHover(_presenter.MapMarkers().FirstOrDefault(m => m is { Kind: MapHitKind.Province, Id: 1 }));
        await AutoDumpAsync("map-markers-hover");
        _markers.ShowHover(null);
        _markers.SetHideDimmed(true);
        await AutoDumpAsync("map-markers-active");
        _markers.SetHideDimmed(MapNamesChoice.HideDimmed);
    }

    // ---------------------------------------------------------------- the deployment table (owner's decision 25)

    /// <summary>
    /// Shows our deployment screen — the panels over the whole of <c>Sﾌﾞﾀｲ</c>'s screen and the table across their
    /// top (<see cref="DeploymentPanels.TableArea"/>; the owner's E1/E2) — while the deployment picker is open with
    /// our interface on; places it over the picture as the window is sized and fills it from GameState. While a message
    /// window is up over it (a refusal's key wait) it stays in sight under our window, but its clicks pass through it to
    /// the window. Returns whether it takes the pointer: it shows and no message window is up. A screen shown, hidden,
    /// or taking the pointer again starts with no gestures queued.
    /// </summary>
    private bool UpdateDeploymentScreen()
    {
        bool ours = InterfaceChoice.New && _presenter.Picker is { Kind: PickerKind.Deployment };
        _presenter.DeploymentCovered = ours;
        bool show = ours && !SkippingScene && _presenter.State is not null;
        bool active = show && !_presenter.MessageWindowShown;
        if (show != _table.Visible)
        {
            _table.Visible = _panels.Visible = show;
            if (show)
                _panels.LoadCrops();                            // the owner's squares from Debug → Portraits
        }
        if (active != _tableActive)
        {
            _tableActive = active;
            _tablePointer = new PointerScript();
            _wantedOrder = null;
            _table.MouseFilter = _panels.MouseFilter = active ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        }
        if (!show)
        {
            UpdateDeploymentReplenish(false, default);
            _tablePicture.Hide();
            return false;
        }
        var frame = Frame();
        float pixels = frame.Pixels;
        var origin = frame.Origin;
        if (active)
            UpdateDeploymentReplenish(true, new Rect2(origin, new Vector2(1280, 800) * (pixels / 2)));
        else
            UpdateDeploymentReplenish(false, default);
        _panels.Busy = !active || (_options.Auto ? _pointer : _tablePointer).Pending > 0 || _wantedOrder is not null || RefillBusy;
        _panels.Place(origin, pixels);
        _panels.Refresh(_presenter.State!, _presenter.Picker!, _wantedOrder);
        DeploymentPanels.PlaceTable(_table, origin, pixels);
        _table.Refresh(_presenter.State!, _wantedOrder);
        var units = _presenter.State!.Units;
        _panels.WarmPictures(_table.Units.Select(u => (int)units[u].PortraitCg.Value));   // the rows' pictures, one a frame
        return active;
    }

    // ---------------------------------------------------------------- our battle screen (docs/ui/battle-screen.md)

    /// <summary>
    /// Shows our battle screen over the stage while our interface is on and the battle's field
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
        var frame = Frame();
        _battleView.Place(frame.Origin, frame.Scale);
        IEnumerable<WindowShape> input = _sel?.Windows ?? _battleMenu?.Windows ?? [];
        var hovered = _battleView.Commands.Active ? _battleView.Commands.Lit   // a card lights up only while the player chooses
            : _sel is null ? null : _battleView.PlaceAt(TouchMode.LocalPointer(this) - _content.Position, _presenter.State!);
        _battleView.Refresh(_presenter, input, screenChanged, hovered,
            instant: _skip.IsSkipping,                                         // MsgSkip: no flash on the rounds gauge
            slidesInstant: _skip.IsSkipping || _options.Fast);                 // the armies and the counters at once (--fast: no runs)
        UpdateInfoBox();
    }

    /// <summary>
    /// Our scout report (docs/ui/battle-screen.md, "The scout report") over the original's screen while Option →
    /// Interface is on New and a ninja's report lists the approaching army (<see cref="BattlePresenter.Scout"/>).
    /// </summary>
    private bool ScoutReportShown => InterfaceChoice.New && _presenter.Battle.Scout is not null && _presenter.Battle.Attack is null
        && !SkippingScene && _presenter.State is not null;

    private void UpdateScoutReport()
    {
        bool show = ScoutReportShown;
        if (show != _scoutView.Visible)
        {
            _scoutView.Visible = show;
            if (show)
                _scoutView.LoadCrops();
        }
        if (!show)
            return;
        var frame = Frame();
        _scoutView.Place(frame.Origin, frame.Scale);
        _scoutView.Show(_presenter.Battle.Scout!);
    }

    /// <summary>
    /// Our strategy's cut-in (docs/ui/battle-screen.md, "The strategy's cut-in") over the original's strategy screen while
    /// our interface is on and a successful strategy is under way (<see cref="BattlePresenter.Strategy"/>).
    /// </summary>
    private bool StrategyCutInShown => InterfaceChoice.New && _presenter.Battle.Strategy is not null && !SkippingScene && _presenter.State is not null;

    private void UpdateStrategyCutIn()
    {
        bool show = StrategyCutInShown;
        _strategyView.Visible = show;
        if (!show)
        {
            _strategyView.Clear();
            return;
        }
        var frame = Frame();
        _strategyView.Place(frame.Origin, frame.Scale);
        _strategyView.Show(_presenter.Battle.Strategy!, _presenter.Battle.StrategyBackdrop,
            Surface.ScreenWidth * Surface.Scale, Surface.ScreenHeight * Surface.Scale, instant: _skip.IsSkipping || _options.Fast);
    }

    /// <summary>
    /// A row pressed (by the mouse, or by unattended play): the gestures the script expects
    /// (<see cref="DeploymentTable.Press"/>), queued for the table's pointer — or unattended play's —
    /// unless the last press's are still under way.
    /// </summary>
    private void TableRowPressed(int unit)
    {
        var pointer = _options.Auto ? _pointer : _tablePointer;
        if (pointer.Pending > 0 || _wantedOrder is not null || RefillBusy || _presenter.State is not { } s)
            return;
        DeploymentTable.Press(s, unit, pointer);
    }

    /// <summary>A bar button pressed (by the mouse, or by unattended play): a click on the script's button there.</summary>
    private void TableButtonPressed(int button)
    {
        var pointer = _options.Auto ? _pointer : _tablePointer;
        if (pointer.Pending > 0 || _wantedOrder is not null || RefillBusy)
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
        if (pointer.Pending > 0 || _wantedOrder is not null || RefillBusy || _presenter.State is not { } s)
            return;
        _wantedOrder = DeploymentTable.Drop(s, from, to, pointer);
    }

    /// <summary>A right press on our screen: a right click where the script's pointer is, which takes the last member out.</summary>
    private void TableRightPressed()
    {
        if (_options.Auto || _tablePointer.Pending > 0 || _wantedOrder is not null || RefillBusy)
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
    /// <c>FIGT_SEL</c>: on our battle screen (our interface) our own command input answers it
    /// (<see cref="BattleCommandLayer"/>: the banners on the enemy cards, the cards' context menus); else, or
    /// once the interface changes while the player chooses, the original's prompt and popups (<see cref="FigtSelScreen"/>).
    /// </summary>
    public async Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
    {
        InterruptPointers();
        _presenter.Flush();
        if (_options.Auto)
        {
            await DelayAsync(_options.Fast ? 0 : 0.5);
            var direct = menu.Find(FigtSelCommand.Direct);
            if (TouchMode.On && !_autoTouched && BattleScreenShown && DisplayServer.GetName() != "headless" && direct is { Arguments.Count: > 0 })
                return await AutoTouchCommandsAsync(menu, direct.Arguments);
            if (!TouchMode.On && _autoCommandsPhase < 4 && BattleScreenShown && DisplayServer.GetName() != "headless" && direct is { Arguments.Count: > 0 })
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
                var model = new BattleCommands(menu);
                commands.Begin(model, _presenter.State!, _presenter.Battle.Text, TouchMode.On ? TouchTarget(model) : null);
                PointerMoved();
                await _answered.Task;
                if (commands.Result is { } choice)
                {
                    if (TouchMode.On)
                        TouchAttacked(model, choice);
                    return choice;
                }
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
    /// retreat dialog, which <see cref="AutoRetreatDialogAsync"/> cancels. Back at the same turn, another own
    /// card is given the order it has already (no rule changes: the core's message, our label's flash, dump
    /// "order-flash" without <c>--fast</c>). Back at the same turn again: the pointer on each advantage next to the armies'
    /// names shows its tooltip (dumps "advantage-tip-1"…), on each enemy card its banners (dumps "banners-E1"…); two sample menus, never
    /// answered, show two and three banners on the target ("sample-2-banners", "sample-3-banners": Rance
    /// Attack added; Bow and Whirlwind Shot added); the same with the damage forecast, whatever the option
    /// ("forecast-E1"…, "forecast-sample-3-banners"); then the pointer on the target's Melee banner
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
                _autoCommandsPhase = 2;                                        // no Retreat in the menu: on to the order
            }

            if (_autoCommandsPhase == 2)
            {
                _autoCommandsPhase = 3;                                        // the banners next
                var other = Enumerable.Range(1, menu.PlayerSlotsUsed.Value).Select(n => new BattlePlace(BattleSide.Player, n)).FirstOrDefault(p => p != model.Acting);
                if (other.Slot != 0)
                {
                    // The order the unit has already (no rule changes): the core's message, our label's flash.
                    var order = new FigtSelChoice((ushort)(FigtSelCommand.OrderManual + PlayerOrder(s, other.Slot)), (Sys3Int)other.Slot);
                    GD.Print($"Kichikuou: --auto gives {other} its own order again (week {s.Week.Value}).");
                    await AutoClickAsync(Centre(other), KeySpace);
                    int item = commands.MenuItems.ToList().IndexOf(order);
                    if (item < 0 || commands.ItemCentre(item) is not { } at)
                        throw new InvalidOperationException($"--auto found no {order} in {other}'s menu ({string.Join(", ", commands.MenuItems)}).");
                    await AutoClickAsync(at, KeyReturn);
                    if (commands.Result != order)
                        throw new InvalidOperationException($"--auto's click on {other}'s order answered {commands.Result}, not {order}.");
                    return order;
                }
            }

            GD.Print($"Kichikuou: --auto shows the banners and attacks by a banner (week {s.Week.Value}).");
            _autoCommandsPhase = 4;
            var target = new BattlePlace(BattleSide.Enemy, button.Value - 3);
            var expected = new FigtSelChoice(FigtSelCommand.Direct, button);
            int tips = 0;
            foreach (var advantage in _battleView.AdvantageCentres())           // each advantage's tooltip
            {
                await AutoMoveAsync(advantage);
                if (!_battleView.AdvantageTipShown)
                    throw new InvalidOperationException("--auto's pointer on an advantage showed no tooltip.");
                await AutoDumpAsync($"advantage-tip-{++tips}");
            }
            for (int slot = 1; slot <= menu.EnemySlotsUsed.Value; slot++)
            {
                await AutoMoveAsync(Face(new BattlePlace(BattleSide.Enemy, slot)));
                await AutoDumpAsync($"banners-E{slot}");
            }
            // Samples for the banners' look, never answered: the menus a unit with a special and focus would get.
            var button4 = new List<Sys3Int> { button };
            var sample3 = menu with { AttackType = 1, Options = [.. menu.Options, new FigtSelOption(FigtSelCommand.Bow, button4), new FigtSelOption(FigtSelCommand.WhirlwindShot, button4)] };
            foreach (var (name, sample) in new[]
            {
                ("sample-2", menu with { Options = [.. menu.Options, new FigtSelOption(FigtSelCommand.RanceAttack, button4)] }),
                ("sample-3", sample3),
            })
            {
                commands.Begin(new BattleCommands(sample), s, _presenter.Battle.Text);
                await AutoMoveAsync(Face(target));
                await AutoDumpAsync($"{name}-banners");
            }
            // The same with the damage forecast (docs/ui/damage-forecast.md), the option's or not; its figures waited for.
            commands.Forecast = true;
            try
            {
                commands.Begin(model, s, _presenter.Battle.Text);
                await commands.ForecastsDone!;
                for (int slot = 1; slot <= menu.EnemySlotsUsed.Value; slot++)
                {
                    await AutoMoveAsync(Face(new BattlePlace(BattleSide.Enemy, slot)));
                    await AutoDumpAsync($"forecast-E{slot}");
                }
                commands.Begin(new BattleCommands(sample3), s, _presenter.Battle.Text);
                await commands.ForecastsDone!;
                await AutoMoveAsync(Face(target));
                await AutoDumpAsync("forecast-sample-3-banners");
            }
            finally
            {
                commands.Forecast = null;
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
        static int PlayerOrder(GameState s, int slot) => slot switch
        {
            1 => s.Bt_PlayerOrder1.Value,
            2 => s.Bt_PlayerOrder2.Value,
            3 => s.Bt_PlayerOrder3.Value,
            _ => s.Bt_PlayerOrder4.Value,
        };
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

    /// <summary>
    /// Unattended play with <c>--scout</c>: a click on the item of <paramref name="code"/> of the battle menu
    /// <see cref="ChooseAsync"/> opens next, as a player's — the press, held six frames, the release that chooses
    /// (<see cref="BattleButton"/>, as <see cref="_GuiInput"/> passes it) — without moving the system pointer. The next key
    /// wait then finds nothing pressed (<see cref="CheckNoClickLeft"/>): the bug of 2026-10-16, where the press stayed
    /// latched and Rance's next line was answered at once.
    /// </summary>
    private async Task AutoMenuClickAsync(Sys3Int code)
    {
        while (_battleMenu is null)
            await NextFrameAsync();
        var menu = _battleMenu;
        var (x, y) = menu.ItemCentre(code);
        _input.Position = ClampToScreen(x, y);
        PointerMoved();
        await NextFrameAsync();
        _input.Button(KeyReturn, true);
        BattleButton(KeyReturn, true);
        for (int frame = 0; frame < 6; frame++)
            await NextFrameAsync();
        _autoMenuClicked = true;
        _input.Button(KeyReturn, false);
        BattleButton(KeyReturn, false);
        if (!menu.IsDone || menu.Result != code)
            throw new InvalidOperationException($"--scout's click on the battle menu's item {code} answered {menu.Result}.");
    }

    /// <summary>After <see cref="AutoMenuClickAsync"/>: the key wait that follows must find no click left.</summary>
    private void CheckNoClickLeft()
    {
        if (!_autoMenuClicked)
            return;
        _autoMenuClicked = false;
        if (_input.PeekInput() != 0)
            throw new InvalidOperationException(
                $"The click that answered a battle menu was left for the next key wait ({_input.PeekInput()}), which would take it at once.");
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
        InterruptPointers();
        _presenter.Flush();
        if (EnemyAttackAnswers(menu))
        {
            _skip.OnMenu();                                             // our screen is FIGT_B's menu: it stops the skip as the menu does
            return await EnemyAttackChooseAsync(menu);
        }
        Task? clicking = null;
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
            if (menu.Kind == BattleMenuKind.EnemyAttackResponse && _options.AutoScout && !_autoScouted)
            {
                // Ninja Scout (code 2) once a launch; the ninja menu then takes its first item, the first free ninja.
                // Both are clicked as a player clicks, through the menu below (AutoMenuClickAsync).
                _autoScouted = true;
                _autoScoutClicks = 2;
            }
            if (_autoScoutClicks > 0 && menu.Kind is BattleMenuKind.EnemyAttackResponse or BattleMenuKind.NinjaScout)
            {
                _autoScoutClicks--;
                clicking = AutoMenuClickAsync(menu.Kind == BattleMenuKind.EnemyAttackResponse ? (Sys3Int)2 : menu.Options[0].Code);
            }
            else
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
            if (clicking is not null && await Task.WhenAny(_answered.Task, clicking) == clicking)
                await clicking;                                             // its error, if the click did not answer
            await _answered.Task;
            return _battleMenu.Result;
        }
        finally
        {
            _battleMenu = null;
            _presenter.Invalidate();
            _input.ReadInput();                                             // the menu took the answering click
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

    /// <summary>The unattended player has sent a ninja to scout (<see cref="GameOptions.AutoScout"/>).</summary>
    private bool _autoScouted;

    /// <summary>The battle menus left that the unattended scout answers by clicks (<see cref="AutoMenuClickAsync"/>).</summary>
    private int _autoScoutClicks;

    /// <summary>A battle menu was answered by an unattended click; the next key wait checks it left none (<see cref="CheckNoClickLeft"/>).</summary>
    private bool _autoMenuClicked;

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

    /// <summary>Where the unattended harem visit stands (<see cref="GameOptions.AutoHarem"/>): wanted at the map, Harem chosen, the girl picked.</summary>
    private enum AutoHarem { None, Wanted, Chosen, Picked }

    private AutoHarem _autoHarem;

    /// <summary>The unattended treasure visit (<see cref="GameOptions.AutoTreasure"/>): null not yet, true wanted at the menu, false chosen.</summary>
    private bool? _autoTreasureWanted;

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
        if (_pointer.Pending > 0 || OrderRunning || _subordinates.Asking)
            return;                                     // an order of our Subordinates screen has the pointer, or asks
        if (_presenter.Picker is { Kind: PickerKind.Subordinates } && _autoReplenish == AutoReplenish.Chosen && UpdateSubordinatesScreen()
            && _presenter.State is { } ordering)
        {
            _autoReplenish = AutoReplenish.None;        // our own screen: its Replenish button, its question answered Yes
            GD.Print($"Kichikuou: --auto replenishes through our Subordinates screen (week {ordering.Week.Value}).");
            AutoReplenishThroughScreen(_autoReplenishUnit);
            return;
        }
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
            if (AutoRefill(d))
                return;
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
            else if (picker.Kind == PickerKind.Harem && _autoHarem == AutoHarem.Chosen)
            {
                _autoHarem = AutoHarem.Picked;
                (x, y) = GesturePoints.PickerCell(_options.AutoHarem);
            }
            _pointer.Click(x, y);
        }
        else if (_presenter.MapWaiting && (_autoMapWait || _autoDragged) && _presenter.State is { } s)
        {
            // The map wait's first read still has the camera as the centre (COMMAND.ADV:50-58), on
            // Leazas Castle at the start of the week (:4-13); after a drag the loop holds it as the corner.
            bool centred = !_autoDragged;
            _autoMapWait = _autoDragged = false;
            if (centred && _options.AutoHarem > 0 && _autoHarem == AutoHarem.None)
            {
                GD.Print($"Kichikuou: --auto visits the harem (week {s.Week.Value}).");
                _autoHarem = AutoHarem.Wanted;
                _pointer.RightClick(5, 6);                              // the menu: Harem
                return;
            }
            if (centred && _options.AutoTreasure && !_autoTreasureWanted.HasValue)
            {
                GD.Print($"Kichikuou: --auto visits the treasure (week {s.Week.Value}).");
                _autoTreasureWanted = true;
                _pointer.RightClick(5, 6);                              // the menu: Treasure
                return;
            }
            if (centred && _options.AutoSaveLoad && !_autoSaveLoadWanted.HasValue)
            {
                GD.Print($"Kichikuou: --auto opens Load/Save (week {s.Week.Value}).");
                _autoSaveLoadWanted = true;
                _pointer.RightClick(5, 6);                              // the menu: Load/Save
                return;
            }
            if (centred && _options.AutoDungeon && _autoDungeon == AutoDungeon.NotYet)
            {
                if (AutoCaveOnScreen(s) is var (sx, sy))
                {
                    _autoDungeon = AutoDungeon.Searching;              // at once, with Rance (AutoParty)
                    _pointer.Click(sx, sy);
                    return;
                }
                throw new AutoStopException($"--dungeon: the Monsters' Cave cannot be searched now (week {s.Week.Value}, stage {s.StoryStage.Value}, "
                    + $"dungeon command {s.Ui_CmdDungeonAvail.Value}, the cave's province owner {s.Provs[s.Dgns[AutoCave].Prov].Owner.Value}).");
            }
            if (centred && !_options.AutoRefill && AutoReplenishUnit(s) is { } unit)
            {
                GD.Print($"Kichikuou: --auto replenishes unit {unit} ({s.Units[unit].Troops.Value} of {s.Units[unit].MaxTroops.Value} troops, week {s.Week.Value}).");
                _autoReplenish = AutoReplenish.Wanted;
                _autoReplenishUnit = unit;
                _autoReplenishWeek = s.Week.Value;
                if (TouchMode.On && Windowed && !_autoCommandsTapped)
                    _autoCommandsTap = _autoCommandsTapped = true;      // the menu by the Commands button (AutoCommandsTapAsync)
                else
                    _pointer.RightClick(5, 6);                          // the menu: Subordinates
                return;
            }
            if (!_options.AutoDungeon && AutoAttackTarget(s) is { } target)
            {
                var p = s.Provs[target];
                if (GesturePoints.OnScreen(s, p.MapX, p.MapY, centred) is var (x, y))
                {
                    GD.Print($"Kichikuou: --auto attacks province {target} (week {s.Week.Value}, gold {s.Gold.Value}).");
                    _autoAttack = AutoAttack.Clicked;
                    _autoAttackWeek = s.Week.Value;
                    _autoAttackProv = target;
                    if (InterfaceChoice.New && !_autoPlateDone && DisplayServer.GetName() != "headless")
                        _autoPlateTarget = target;              // through its plate, as a player clicks it (AutoPlateClickAsync)
                    else
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

    /// <summary>The round of the battle under way (its headers counted), for unattended play's stop and log.</summary>
    private int _autoRound;
    private string _autoBattleEnemy = "";

    /// <summary>
    /// <see cref="GameOptions.AutoUntil"/>: the end of unattended play when the first deployment
    /// picker closes, or when the first battle (<c>FIGT_A</c>, <c>FIGT_B</c>, <c>FIGT</c>) ends — or, once
    /// our battle screen's exercise (<see cref="AutoCommandsAsync"/>) has run, when its attack's exchange is over; with
    /// <see cref="AutoUntil.BattleEnd"/> when that battle ends whatever the exercise (owner, 2026-10-20: a check inside a
    /// battle stops with it, not with the week); with <see cref="AutoUntil.Round"/> already when a battle's round
    /// <see cref="GameOptions.AutoUntilRound"/> starts (owner, 2026-10-20: the rounds after the one checked only cost time).
    /// </summary>
    private void AutoStopEarly(ScriptEvent scriptEvent)
    {
        int week = _presenter.State?.Week.Value ?? 0;
        if (scriptEvent is BattleScreenEvent { File: "FIGT_A.ADV" or "FIGT_B.ADV" or "FIGT.ADV", Open: true })
            _autoRound = 0;
        if (scriptEvent is RoundHeaderEvent)
            _autoRound++;
        switch (_options.AutoUntil, scriptEvent)
        {
            case (AutoUntil.Round, RoundHeaderEvent) when _autoRound >= _options.AutoUntilRound:
                throw new AutoStopException($"--auto stopped at the start of round {_autoRound} of a battle (week {week}).");
            case (AutoUntil.Deployment, PickerScreenEvent { Kind: PickerKind.Deployment, Open: false })
                when _autoBackAt is null || _attackBackDumped:                  // not the picker --scout leaves by Back
                throw new AutoStopException($"--auto stopped after the first deployment picker (week {week}).");
            case (AutoUntil.Battle or AutoUntil.BattleEnd or AutoUntil.Round, BattleScreenEvent { File: "FIGT_A.ADV" or "FIGT_B.ADV" or "FIGT.ADV", Open: false } battle):
                throw new AutoStopException($"--auto stopped after the first battle, {battle.File} (week {week}).");
            // Our battle screen's exercise done (owner, 2026-10-15: the rest of the battle only costs time): the exchange
            // its banner started has run, and the field's redraw after it ends unattended play.
            case (AutoUntil.Battle, ExchangeResultsEvent) when _autoCommandsPhase == 4:
                _autoCommandsPhase = 5;
                break;
            case (AutoUntil.Battle, BattlefieldBackgroundEvent) when _autoCommandsPhase == 5:
                throw new AutoStopException($"--auto stopped after our battle screen's exercise and its attack (week {week}).");
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
    /// With our screen (our interface), once the party is built: place 1's member dropped
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
        if (picker.Mode == 2 && _options.AutoScout && InterfaceChoice.New && _panels.Visible && _autoBackAt is null)
        {
            _autoBackAt = Time.GetTicksMsec();                          // Back once, to our attack screen (GameScreen.EnemyAttack.cs)
            return (AutoDeployStep.Button, 3);
        }
        if (_autoPartyFrom != picker)
        {
            _autoPartyFrom = picker;
            _autoParty = ChosenParty(s)
                ?? (picker.Mode == 3 && _options.AutoDungeon && s.Units[1].Status == 1 ? [1] : AutoParty(picker.Mode.Value, s));
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
    /// picked — in the unit view a unit at status 1 or 2 not holding Chaos (item 1) or Nikkou (item 2), in the item view
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
        if (_autoHarem == AutoHarem.Wanted && Find("Harem") is var harem and > 0)
        {
            _autoHarem = AutoHarem.Chosen;
            return harem;
        }
        if (_autoSaveLoadWanted == true && Find("Load") is var loadSave and > 0)
        {
            _autoSaveLoadWanted = false;
            _saveShortcut.Start(SaveList.Save);                         // the panel's Save row
            return loadSave;
        }
        if (_autoTreasureWanted == true && Find("Treasure") is var treasure and > 0)
        {
            _autoTreasureWanted = false;
            return treasure;
        }
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
