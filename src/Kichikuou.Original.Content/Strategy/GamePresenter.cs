using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// Draws a whole game in the original's layout (part G0, owner's decisions 3 and 9) on two layers.
/// <list type="bullet">
/// <item>The <b>stage</b> (<see cref="Stage"/>): the engine's 3200×800 surface. What the scenes draw
/// themselves runs there as in the engine (<see cref="EngineStage"/>: <c>G</c> with
/// <c>J0</c>/<c>J1</c>/<c>PC</c>, <c>CC</c>, <c>CE</c>, <c>CS</c>, <c>CF</c>, <c>EC</c>; the opening,
/// the CG scenes, the logo, the title and the ending); the map is drawn there from
/// GameState (tiles, markers, camera); the battle draws there through <see cref="Battle"/>, which
/// gets every event between a battle's <see cref="BattleScreenEvent"/>s — except while a picker the
/// battle opened is up (<c>FIGT_B</c>'s <c>Sﾌﾞﾀｲ</c>): that one is drawn by hand over the battle's
/// screen like any other. The scenes a battle file calls draw their windows through the battle's
/// presenter and their own drawing on the stage.</item>
/// <item>The <b>screens drawn by hand</b> over it, from the events' meaning and GameState: the
/// message window (frame, text), the portraits and the centre picture, the menus, the pickers
/// (<see cref="PickerView"/>). They are composed onto <see cref="Screen"/>, the 640×400 picture
/// the player sees, by <see cref="Compose"/>; fades apply last.</item>
/// </list>
/// Engine-independent: the presentation engine shows <see cref="Screen"/>, supplies the font and
/// audio, animates <see cref="TakeFade"/> and turns its input into the script's inputs (it asks
/// <see cref="MapWaiting"/>, <see cref="Picker"/> and <see cref="MenuHitTest"/> what a click means).
/// </summary>
public sealed partial class GamePresenter : IStageObserver
{
    private readonly ScenarioText _text;
    private readonly CgLibrary _cgs;
    private readonly IGlyphRenderer _glyphs;
    private readonly IBattleAudio? _audio;
    private readonly IScenarioSource? _scenario;
    private readonly MessageWriter _stageWriter;
    private readonly EngineStage _engine;

    /// <summary>The message window's text (<c>WAO_SUB</c> windows, and text printed with none open).</summary>
    private readonly TextBox _window = new((140, 278, 376, 140));

    private int _battleDepth;
    private TextTarget _target = TextTarget.Window;
    private int _lastCg;
    private int _endingCg;                      // OVER's ending picture while on the stage (MaskEventCgs)
    private bool _gallery;                      // from the gallery's sheets to the title's background (MaskEventCgs)
    private Action? _afterKey;
    private bool _dirty = true;
    private int _stageVersion = -1;

    /// <summary>Where message text goes now.</summary>
    private enum TextTarget
    {
        /// <summary>The message window (<see cref="_window"/>).</summary>
        Window,

        /// <summary>A panel of the open picker (its <c>B4</c> window).</summary>
        Panel,

        /// <summary>The stage at the position of the last <c>T</c> (outside the screens drawn by hand).</summary>
        Stage,

        /// <summary>Nowhere: text the hand-drawn picker shows by itself (its grid, its bar).</summary>
        None,
    }

    /// <summary>Creates a presenter with a black stage.</summary>
    /// <param name="text">The scenario texts.</param>
    /// <param name="cgs">The CGs.</param>
    /// <param name="glyphs">The font.</param>
    /// <param name="scenario">The player's scenario statements, for the literals the pickers show (bar labels, the harem's names).</param>
    /// <param name="audio">Sound and music, or null.</param>
    public GamePresenter(ScenarioText text, CgLibrary cgs, IGlyphRenderer glyphs, IScenarioSource? scenario = null, IBattleAudio? audio = null)
    {
        _text = text;
        _cgs = cgs;
        _glyphs = glyphs;
        _audio = audio;
        _scenario = scenario;
        Stage = new Surface();
        Screen = new Surface(Surface.ScreenWidth, Surface.ScreenHeight, indexed: false);
        Battle = new BattlePresenter(text, cgs, glyphs, audio, Stage);
        Scene = new BattleScene(cgs);
        _stageWriter = new MessageWriter(Stage, glyphs);
        _engine = new EngineStage(Stage, cgs, this);
        _engine.SetState("B1", [(Sys3Int)1, (Sys3Int)250, (Sys3Int)50, (Sys3Int)150]);   // ｽﾀｰﾄ.ADV:860, the boot's window 1
    }

    /// <summary>The engine's surface: what the scenes, the map and the battle draw.</summary>
    public Surface Stage { get; }

    /// <summary>The picture the player sees: the stage's screen with the hand-drawn screens over it.</summary>
    public Surface Screen { get; }

    /// <summary>The battle's presenter, drawing on <see cref="Stage"/>; its input screens take it.</summary>
    public BattlePresenter Battle { get; }

    /// <summary>
    /// Our battle screen's armies' scene (docs/ui/battle-screen.md, "The scene between exchanges"): it
    /// follows every event the battle's presenter draws, whichever interface shows.
    /// </summary>
    public BattleScene Scene { get; }

    /// <summary>Our battle screen's round order and rounds (<see cref="RoundOrder"/>), following the same events.</summary>
    public RoundOrder Order { get; } = new();

    /// <summary>Whether our battle screen can stand now: the battle draws and its field or an exchange is on (<see cref="BattleScene.Active"/>).</summary>
    public bool BattleSceneShows => BattleShows && Scene.Active;

    /// <summary>
    /// The battle's opening while its first round header waits for its key (<see cref="BattleOpening"/>; docs/ui/battle-screen.md,
    /// "The opening"), else null.
    /// </summary>
    public BattleOpening? Opening =>
        BattleShows && Battle.RoundHeader is { } header && Order.Round == 1 && State is { } s ? BattleOpening.Of(header, s) : null;

    /// <summary>Whether a later round's header waits for its key: our screen does not show it and answers it at once.</summary>
    public bool LaterRoundHeader => BattleShows && Battle.RoundHeader is not null && Order.Round > 1;

    /// <summary>
    /// The windows of the battle's screen now (<see cref="BattlePresenter.Windows"/>, plus those of the
    /// input screen open, <paramref name="extra"/>, and the menu a scene the battle called opened) as a
    /// mask over <see cref="Screen"/>; our battle screen shows the screen only there (<see cref="WindowMask.Compose"/>).
    /// </summary>
    public WindowMask BattleWindows(IEnumerable<WindowShape>? extra = null) =>
        WindowMask.Of(BattleShows ? Battle.Windows.Concat(extra ?? []).Concat(MenuWindow()) : []);

    private IEnumerable<WindowShape> MenuWindow()
    {
        if (MenuBox() is { Box: var (x, y, w, h) })
            yield return WindowShape.Box(x, y, w, h);
    }

    /// <summary>The game state the screens are drawn from (set by <see cref="Bind"/>).</summary>
    public GameState? State { get; private set; }

    /// <summary>Whether a battle runs (its events draw the screen, unless it opened a picker).</summary>
    public bool InBattle => _battleDepth > 0;

    /// <summary>Whether the battle's presenter draws the events now: in a battle, outside a picker it opened.</summary>
    private bool BattleShows => InBattle && Picker is null;

    /// <summary>Whether <c>COMMAND</c>'s map loop waits for the player: its pointer reads are the map's.</summary>
    public bool MapWaiting { get; private set; }

    /// <summary>The open picker, or null.</summary>
    public PickerView? Picker { get; private set; }

    /// <summary>Whether the save screen's lists or menu are open.</summary>
    public bool SaveScreenOpen { get; private set; }

    /// <summary>
    /// Our own deployment screen covers <c>Sﾌﾞﾀｲ</c>'s (owner's decision 25,
    /// docs/ui/deployment-table.md): the deployment picker is drawn with its background and the
    /// hovered unit's portrait only — without the grid's names, the underlines, the bar's labels
    /// and the text of the details and party panels, which our panels show instead.
    /// </summary>
    public bool DeploymentCovered
    {
        get;
        set
        {
            if (field != value)
                Invalidate();
            field = value;
        }
    }

    /// <summary>Whether <c>WAO_SUB</c>'s message window is up over a screen drawn by hand (its frame is drawn: <c>Ui_WinType</c> not 0).</summary>
    public bool MessageWindowShown => !BattleShows && State is { } s && s.Ui_WinType != 0;

    /// <summary>How far the screen is faded (0 not at all, 255 all black or white).</summary>
    public byte FadeAmount
    {
        get => _engine.FadeAmount;
        set => _engine.FadeAmount = value;
    }

    /// <summary>Whether the fade goes to white (<c>PW</c>) rather than black (<c>PF</c>).</summary>
    public bool FadeToWhite => _engine.FadeToWhite;

    /// <summary>Script locations whose text could not be read (shown as the location instead).</summary>
    public List<string> Unresolved { get; } = [];

    /// <summary>Whether the battle waits for a key after the last event (see <see cref="BattlePresenter.WaitsForKey"/>).</summary>
    public bool WaitsForKey => BattleShows && Battle.WaitsForKey;

    /// <summary>
    /// Debug only, off for play: while the full-screen window (<c>WAO_SUB</c> 131/231) is open, the
    /// stage under it is drawn as a grey placeholder with the number of the CG last drawn there —
    /// the event CGs stay out of the screen dumps the agent's tooling makes. So is the stage from
    /// <c>OVER</c>'s ending picture (<see cref="FirstEndingCg"/>-<see cref="LastEndingCg"/>, under the
    /// staff roll too) until the report's background (<see cref="ReportCg"/>) replaces it, and the
    /// gallery from its sheets of reduced event pictures (<see cref="FirstGallerySheet"/>-<see cref="LastGallerySheet"/>)
    /// until the title's background (<see cref="TitleCg"/>): its pages, its pictures and the scenes it replays.
    /// </summary>
    public bool MaskEventCgs { get; set; }

    /// <summary>The endings' pictures, <c>G 269 + EndingId</c> (<c>OVER.ADV:33</c>).</summary>
    public const int FirstEndingCg = 270, LastEndingCg = 283;

    /// <summary>The gallery's five page sheets, loaded off screen first (<c>CG.ADV:35-44</c>).</summary>
    public const int FirstGallerySheet = 844, LastGallerySheet = 848;

    /// <summary>The title's background (<c>ｽﾀMENU.ADV</c>), drawn again when the gallery returns.</summary>
    public const int TitleCg = 301;

    /// <summary>The Completion Report's background (<c>OVER.ADV:194</c>).</summary>
    public const int ReportCg = 840;

    /// <summary>The message window's text (for tests and other UIs).</summary>
    public TextBox WindowText => _window;

    /// <summary>Takes the game's state: the screens are drawn from it.</summary>
    public void Bind(GameState state)
    {
        State = state;
        Battle.Attach(state);
        _tiles = Tiles.Valid;
        Invalidate();
    }

    /// <summary>Something under the screens drawn by hand changed (the pointer, a battle input screen): compose again.</summary>
    public void Invalidate() => _dirty = true;

    /// <summary>The fade the last event started, once (the engine animates it; the amount is already the end).</summary>
    public FadeRequest? TakeFade() => _engine.TakeFade();

    /// <summary>Finishes the drawing the script does without an event of its own; call before waiting for input.</summary>
    public void Flush()
    {
        if (BattleShows)
            Battle.Flush();
    }

    /// <summary>The drawing the script does right after a key wait (the window's text cleared, the battle's).</summary>
    public void AfterKey()
    {
        if (BattleShows)
        {
            Battle.AfterKey();
            return;
        }
        var action = _afterKey;
        _afterKey = null;
        action?.Invoke();
        Invalidate();
    }

    /// <summary>Shows one event.</summary>
    public void Show(ScriptEvent scriptEvent)
    {
        Invalidate();
        if (scriptEvent is BattleScreenEvent battle)
        {
            BattleScreen(battle.Open);
            return;
        }
        if (BattleShows)
        {
            // A picker the battle opens (FIGT_B's Sﾌﾞﾀｲ) is drawn by hand over the battle's screen,
            // and its file's events go to it until it closes.
            if (scriptEvent is PickerScreenEvent { Open: true } picker)
                PickerScreen(picker);
            else if (scriptEvent is not ScreenEvent)
                Battle.Show(scriptEvent);
            // The scenes a battle file calls (死亡捕獲's deaths, Gｼｨﾙ 28 …) draw on the same surface:
            // their G, fades and copies run on the stage after the battle's drawing they follow.
            if (scriptEvent is EngineEvent engine)
                Engine(engine.Command, engine.Args);
            if (State is { } s && Picker is null)
            {
                Order.Observe(scriptEvent, s);
                Scene.Observe(scriptEvent, s, Order.Acting);                // the turn's actor first: the scene shows it
            }
            return;
        }
        // A map animation's steps are the pauses after it; sound draws nothing (M 10 plays its sound before the blink).
        if (scriptEvent is not (PauseEvent or SoundEvent or SoundPairEvent or MusicEvent))
            _mapAnimation = MapAnimation.None;
        if (_afterKey is { } after)
        {
            _afterKey = null;
            after();
        }

        switch (scriptEvent)
        {
            case MapScreenEvent e:
                MapWaiting = e.Waiting;
                if (e.Waiting && _tiles == Tiles.Foreign)
                    EnsureTiles();                                      // the drag copies the view from the tiles
                break;
            case PickerScreenEvent e: PickerScreen(e); break;
            case DataScreenEvent e: DataScreen(e.View); break;
            case SaveScreenEvent e: SaveScreenOpen = e.Open; break;
            case WindowEvent e: Window(e.Op.Value); break;
            case MessageEvent e: Print(Read(e.Text, [], e.StringVariables)); break;
            case ValueMessageEvent e: Print(Read(e.Text, e.Values, e.StringVariables)); break;
            case PrintStringEvent e: Print(Hankaku.ToHalfWidth(StringVar(e.Slot.Value))); break;
            case LineBreakEvent: NewLine(); break;
            case MessageClearEvent: ClearText(); break;
            case MessageResetEvent e:
                if (e.Mode == 0)
                    ClearText();
                else
                    HomeText();
                break;
            case MapEvent e: Map(e.Mode.Value); break;
            case WaoMapEvent e:
                if (e.Mode == 3)
                    _tiles = Tiles.Dirty;
                break;
            case PauseEvent: MapPause(); break;
            case ClearScreenEvent:
                Stage.Fill(0, 0, Surface.ScreenWidth, Surface.ScreenHeight, 0);
                CoverScreen();
                break;
            case SoundEvent e: IBattleAudio.PcmPlay(_audio, e); break;
            case SoundPairEvent e: _audio?.PlaySoundPair(e.First.Value, e.Second.Value); break;
            case MusicEvent e:
                _audio?.PlayMusic(e.Track.Value, _engine.TakeMusicLoops());
                break;
            case EngineEvent e: Engine(e.Command, e.Args); break;
        }
    }

    // ------------------------------------------------------------------ battle

    private void BattleScreen(bool open)
    {
        if (open)
        {
            if (_battleDepth++ == 0 && State is { } s)
            {
                Battle.Attach(s);
                Scene.Start(s);
                Order.Reset();
                _tiles = Tiles.Valid;
                _window.Clear();
                _afterKey = null;
                MapWaiting = false;
            }
            return;
        }
        if (_battleDepth > 0 && --_battleDepth == 0)
        {
            Battle.Flush();
            Scene.Stop();
            _tiles = Tiles.Dirty;
            _target = TextTarget.Window;
        }
    }

    // ------------------------------------------------------------------ text

    /// <summary>The text of a script location, with numbers and string variables filled in.</summary>
    private string Read(ScriptRef source, IReadOnlyList<Sys3Int> numbers, IReadOnlyList<Sys3Int>? stringVariables)
    {
        try
        {
            return _text.Read(source).Format(numbers, StringVar, stringVariables);
        }
        catch (Exception e) when (e is ScenarioTextException or ArgumentException or IOException)
        {
            Unresolved.Add($"{source}: {e.Message}");
            return $"[{source}]";
        }
    }

    /// <summary>The text at a script location with its numbers (the screens drawn by hand print the script's lines themselves).</summary>
    internal string ReadText(ScriptRef source, params Sys3Int[] numbers) => Read(source, numbers, null);

    /// <summary>A menu item's text as it was printed into the item.</summary>
    internal string Read(CapturedText captured)
    {
        try
        {
            return _text.Read(captured.Text).FormatCaptured(captured.Numbers, captured.Strings);
        }
        catch (Exception e) when (e is ScenarioTextException or ArgumentException or IOException)
        {
            Unresolved.Add($"{captured.Text}: {e.Message}");
            return $"[{captured.Text}]";
        }
    }

    private string StringVar(int index) => State is { } s && index >= 0 && index < s.Strings.Length ? s.Strings[index] : "";

    private TextBox? TargetBox => _target switch
    {
        TextTarget.Window => _window,
        TextTarget.Panel => Picker?.Panel,
        _ => null,
    };

    private void Print(string text)
    {
        if (_target == TextTarget.Stage)
        {
            _stageWriter.Size = _engine.TextSize;
            _stageWriter.Color = _engine.TextColor;
            _stageWriter.Print(text);
            return;
        }
        TargetBox?.Print(text, _engine.TextSize, _engine.TextColor);
    }

    private void NewLine()
    {
        if (_target == TextTarget.Stage)
            _stageWriter.NewLine();
        else
            TargetBox?.NewLine(_engine.TextSize);
    }

    private void ClearText() => TargetBox?.Clear();

    private void HomeText()
    {
        if (_target == TextTarget.Stage)
            _stageWriter.Home();
        else
            TargetBox?.Home();
    }

    // ------------------------------------------------------------------ the message window (WAO_SUB)

    /// <summary>
    /// A <c>WAO_SUB.ADV</c> call (<see cref="WindowEvent"/>, raised before its state effects): the
    /// windows that open move the text box (the rectangles, colours and sizes of
    /// docs/battle/screen-layout.md §2); the key waits clear the text after the key; the frame and
    /// the pictures are drawn from GameState (<c>Ui_WinType</c>, <c>Ui_WinFrameCg</c>,
    /// <c>Ui_Portrait*</c>, <c>Ui_Center*</c>) when the screen is composed.
    /// </summary>
    private void Window(int op)
    {
        if (op is (>= 111 and <= 153) or 0 or 10)
            _covered &= ~Pictures.Frame;                                // the frame drawn again, or the window closed
        switch (op)
        {
            case 111 or 112: OpenWindow((235, 8, 378, 141), 15, 24); break;
            case 113 or 123: OpenWindow((140, 278, 376, 140), 15, null); break;
            case 121 or 122: OpenWindow((43, 265, 378, 141), 15, 24); break;
            case 131: OpenWindow((8, 305, 626, 140), 190, 24); break;
            case 141 or 143 or 153: OpenWindow((200, 196, 240, 200), null, null); break;
            case 142: OpenWindow((200, 196, 240, 200), 11, null); break;
            case 151: OpenWindow((224, 196, 240, 200), null, null); break;
            case 152: OpenWindow((224, 196, 240, 200), 11, null); break;
            case >= 211 and <= 253: _target = TextTarget.Window; break;
            case 0 or 10:
                if (op == 0 && State is { Ui_WinType.Value: >= 31 and <= 39 } shown)
                    LeaveWindowOnStage(shown);
                if (op == 10 || State?.Ui_WinType.Value is >= 11 and <= 29)
                    _engine.TextColor = 15;
                _window.Clear();
                if (_target == TextTarget.Window && Picker is not null)
                    _target = TextTarget.None;
                break;
            case 1 or 5 or 11:
                if (State?.Ui_WinType != 0)
                    _afterKey = _window.Clear;
                break;
            case 2: _window.Clear(); break;
            case >= 311 and <= 333: Uncover(op); break;
        }
    }

    private void OpenWindow((int X, int Y, int W, int H) text, int? color, int? size)
    {
        _window.Open(text);
        if (color is int c)
            _engine.TextColor = c;
        if (size is int s)
            _engine.TextSize = s;
        _target = TextTarget.Window;
    }

    // ------------------------------------------------------------------ pickers and menus

    /// <summary>A screen drawn by hand is open: a picker or All Data.</summary>
    private bool DrawnByHand => Picker is not null || Data != DataView.Closed;

    private void PickerScreen(PickerScreenEvent e)
    {
        if (e.Open)
        {
            if (Picker?.Kind != e.Kind)
                Picker = new PickerView(e.Kind, e.Mode, this);
            _target = TextTarget.None;
            return;
        }
        Picker = null;
        _target = TextTarget.Window;
    }

    // ------------------------------------------------------------------ engine commands

    /// <summary>
    /// A presentation-only engine command. The text commands (<c>ZM</c>, <c>ZC 1</c>, the message
    /// and selection windows <c>B1</c>-<c>B4</c>, <c>T</c>) steer the text; the drawing commands
    /// run on the stage as in the engine (<see cref="EngineStage"/>), except while a picker or All
    /// Data is open (they are drawn by hand: the text placed with <c>T</c> goes nowhere, they print
    /// it themselves).
    /// </summary>
    private void Engine(string command, IReadOnlyList<Sys3Int> args)
    {
        if (_engine.SetState(command, args))
            return;
        int A(int i) => i < args.Count ? args[i].Value : 0;
        switch (command)
        {
            case "B4":
                if (Picker is { } picker)
                {
                    picker.SelectPanel(A(0), _engine.MessageWindow(A(0)));
                    _target = TextTarget.Panel;
                }
                else if (_engine.MessageWindow(A(0)) is { } rect)
                {
                    _window.Open(rect);
                    _target = TextTarget.Window;
                }
                return;
            case "T":
                if (DrawnByHand)
                {
                    _target = TextTarget.None;
                    return;
                }
                _stageWriter.Open(A(0), A(1), Surface.ScreenWidth, Surface.ScreenHeight);
                _target = TextTarget.Stage;
                return;
        }
        if (!DrawnByHand)
            _engine.Draw(command, args);
    }

    void IStageObserver.BeforeCopy(int sourceX, int width) => PrepareCopy(sourceX, width);

    void IStageObserver.Drawn(int x, int y, int width, int height, bool opaque) => Drawn(x, y, width, height, opaque);

    /// <summary>The CG last loaded, and whether the stage shows an ending's picture or the gallery (<see cref="MaskEventCgs"/>).</summary>
    void IStageObserver.CgLoaded(int number)
    {
        _lastCg = number;
        if (number is >= FirstEndingCg and <= LastEndingCg)
            _endingCg = number;
        else if (number == ReportCg)
            _endingCg = 0;
        if (number is >= FirstGallerySheet and <= LastGallerySheet)
            _gallery = true;
        else if (number == TitleCg)
            _gallery = false;
    }
}
