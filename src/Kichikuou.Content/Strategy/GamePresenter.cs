using Kichikuou.Content.Battles;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Strategy;

/// <summary>A palette fade (<c>PF</c>, <c>PW</c>) for the presentation engine to animate: from one amount to another at the command's <paramref name="Rate"/>.</summary>
public sealed record FadeRequest(byte From, byte To, bool ToWhite, int Rate)
{
    /// <summary>
    /// xsystem35's <c>ags_fadeIn</c>/<c>ags_fadeOut</c>: <c>rate * 16 * 1000 / 60</c> ms (≈267 ms a
    /// unit; the scenes' <c>FadeSpeed</c> 4 is 1066 ms), not the 16 ms tick of <c>Y 3</c>. The
    /// message skip does not shorten it (<c>ags_runEffect</c> waits without <c>KEYWAIT_SKIPPABLE</c>).
    /// </summary>
    public TimeSpan Duration => TimeSpan.FromMilliseconds(Rate * 16 * 1000 / 60);
}

/// <summary>
/// Draws a whole game in the original's layout (part G0, owner's decisions 3 and 9) on two layers.
/// <list type="bullet">
/// <item>The <b>stage</b> (<see cref="Stage"/>): the engine's 3200×800 surface. What the scenes draw
/// themselves runs there as in the engine (<c>G</c> with <c>J0</c>/<c>J1</c>/<c>PC</c>, <c>CC</c>,
/// <c>CS</c>, <c>CF</c>, <c>EC</c>; the opening and the CG scenes); the map is drawn there from
/// GameState (tiles, markers, camera); the battle draws there through <see cref="Battle"/>, which
/// gets every event between a battle's <see cref="BattleScreenEvent"/>s.</item>
/// <item>The <b>screens drawn by hand</b> over it, from the events' meaning and GameState: the
/// message window (frame, text), the portraits and the centre picture, the menus, the pickers
/// (<see cref="PickerView"/>). They are composed onto <see cref="Screen"/>, the 640×400 picture
/// the player sees, by <see cref="Compose"/>; fades apply last.</item>
/// </list>
/// Engine-independent: the presentation engine shows <see cref="Screen"/>, supplies the font and
/// audio, animates <see cref="TakeFade"/> and turns its input into the script's inputs (it asks
/// <see cref="MapWaiting"/>, <see cref="Picker"/> and <see cref="MenuHitTest"/> what a click means).
/// </summary>
public sealed partial class GamePresenter
{
    private readonly ScenarioText _text;
    private readonly CgLibrary _cgs;
    private readonly IGlyphRenderer _glyphs;
    private readonly IBattleAudio? _audio;
    private readonly IScenarioSource? _scenario;
    private readonly MessageWriter _stageWriter;

    /// <summary>The message window's text (<c>WAO_SUB</c> windows, and text printed with none open).</summary>
    private readonly TextBox _window = new((140, 278, 376, 140));

    /// <summary>Message windows <c>B3 n, x, y, w, h</c> by number.</summary>
    private readonly Dictionary<int, (int X, int Y, int W, int H)> _messageWindows = [];

    /// <summary>Selection windows <c>B1 n, x, y, w</c> by number; window 1 as <c>ｽﾀｰﾄ.ADV:860</c> sets it.</summary>
    private readonly Dictionary<int, (int X, int Y, int W)> _selectionWindows = new() { [1] = (250, 50, 150) };

    private int _selectionWindow = 1;
    private int _battleDepth;
    private int _textSize = 24;
    private int _textColor = 15;
    private TextTarget _target = TextTarget.Window;
    private (int X, int Y)? _jump;
    private (int X, int Y)? _jumpRelative;
    private int _cgFlags = 7;
    private int _lastCg;
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
        Screen = new Surface(Surface.ScreenWidth, Surface.ScreenHeight);
        Battle = new BattlePresenter(text, cgs, glyphs, audio, Stage);
        _stageWriter = new MessageWriter(Stage, glyphs);
    }

    /// <summary>The engine's surface: what the scenes, the map and the battle draw.</summary>
    public Surface Stage { get; }

    /// <summary>The picture the player sees: the stage's screen with the hand-drawn screens over it.</summary>
    public Surface Screen { get; }

    /// <summary>The battle's presenter, drawing on <see cref="Stage"/>; its input screens take it.</summary>
    public BattlePresenter Battle { get; }

    /// <summary>The game state the screens are drawn from (set by <see cref="Bind"/>).</summary>
    public GameState? State { get; private set; }

    /// <summary>Whether a battle runs (its events draw the screen).</summary>
    public bool InBattle => _battleDepth > 0;

    /// <summary>Whether <c>COMMAND</c>'s map loop waits for the player: its pointer reads are the map's.</summary>
    public bool MapWaiting { get; private set; }

    /// <summary>The open picker, or null.</summary>
    public PickerView? Picker { get; private set; }

    /// <summary>Whether the save screen's lists or menu are open.</summary>
    public bool SaveScreenOpen { get; private set; }

    /// <summary>How far the screen is faded (0 not at all, 255 all black or white).</summary>
    public byte FadeAmount { get; set; }

    /// <summary>Whether the fade goes to white (<c>PW</c>) rather than black (<c>PF</c>).</summary>
    public bool FadeToWhite { get; private set; }

    /// <summary>Script locations whose text could not be read (shown as the location instead).</summary>
    public List<string> Unresolved { get; } = [];

    /// <summary>Whether the battle waits for a key after the last event (see <see cref="BattlePresenter.WaitsForKey"/>).</summary>
    public bool WaitsForKey => InBattle && Battle.WaitsForKey;

    /// <summary>
    /// Debug only, off for play: while the full-screen window (<c>WAO_SUB</c> 131/231) is open, the
    /// stage under it is drawn as a grey placeholder with the number of the CG last drawn there —
    /// the event CGs stay out of the screen dumps the agent's tooling makes.
    /// </summary>
    public bool MaskEventCgs { get; set; }

    /// <summary>The message window's text (for tests and other UIs).</summary>
    public TextBox WindowText => _window;

    private FadeRequest? _fade;

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
    public FadeRequest? TakeFade()
    {
        var fade = _fade;
        _fade = null;
        return fade;
    }

    /// <summary>Finishes the drawing the script does without an event of its own; call before waiting for input.</summary>
    public void Flush()
    {
        if (InBattle)
            Battle.Flush();
    }

    /// <summary>The drawing the script does right after a key wait (the window's text cleared, the battle's).</summary>
    public void AfterKey()
    {
        if (InBattle)
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
        if (InBattle)
        {
            if (scriptEvent is not ScreenEvent)
                Battle.Show(scriptEvent);
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
            case SoundEvent e when e.Mode != 3: _audio?.PlaySound(e.Sound.Value); break;
            case SoundPairEvent e: _audio?.PlaySoundPair(e.First.Value, e.Second.Value); break;
            case MusicEvent e: _audio?.PlayMusic(e.Track.Value); break;
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
            _stageWriter.Size = _textSize;
            _stageWriter.Color = _textColor;
            _stageWriter.Print(text);
            return;
        }
        TargetBox?.Print(text, _textSize, _textColor);
    }

    private void NewLine()
    {
        if (_target == TextTarget.Stage)
            _stageWriter.NewLine();
        else
            TargetBox?.NewLine(_textSize);
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
                if (op == 10 || State?.Ui_WinType.Value is >= 11 and <= 29)
                    _textColor = 15;
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
            _textColor = c;
        if (size is int s)
            _textSize = s;
        _target = TextTarget.Window;
    }

    // ------------------------------------------------------------------ pickers and menus

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
    /// run on the stage as in the engine, except while a picker is open (it is drawn by hand).
    /// </summary>
    private void Engine(string command, IReadOnlyList<Sys3Int> args)
    {
        int A(int i) => i < args.Count ? args[i].Value : 0;
        switch (command)
        {
            case "ZM": _textSize = A(0); return;
            case "ZC":
                if (A(0) == 1)
                    _textColor = A(1);
                return;
            case "B1": _selectionWindows[A(0)] = (A(1), A(2), A(3)); return;
            case "B2": _selectionWindow = A(0); return;
            case "B3": _messageWindows[A(0)] = (A(1), A(2), A(3), A(4)); return;
            case "B4":
                if (Picker is { } picker)
                {
                    picker.SelectPanel(A(0), _messageWindows.TryGetValue(A(0), out var r) ? r : null);
                    _target = TextTarget.Panel;
                }
                else if (_messageWindows.TryGetValue(A(0), out var rect))
                {
                    _window.Open(rect);
                    _target = TextTarget.Window;
                }
                return;
            case "T":
                if (Picker is not null)
                {
                    _target = TextTarget.None;
                    return;
                }
                _stageWriter.Open(A(0), A(1), Surface.ScreenWidth, Surface.ScreenHeight);
                _target = TextTarget.Stage;
                return;
        }
        if (Picker is not null)
            return;
        switch (command)
        {
            case "J0": _jump = (A(0), A(1)); break;
            case "J1": _jumpRelative = (A(0), A(1)); break;
            case "PC": _cgFlags = A(0); break;
            case "G": DrawCg(A(0), args.Count > 1 ? A(1) : null); break;
            case "CC":
                PrepareCopy(A(0), A(2));
                Stage.Copy(A(0), A(1), A(2), A(3), A(4), A(5));
                Drawn(A(4), A(5), A(2), A(3));
                break;
            case "CS":
                PrepareCopy(A(0), A(2));
                Stage.CopyKeyed(A(0), A(1), A(2), A(3), A(4), A(5));
                Drawn(A(4), A(5), A(2), A(3));
                break;
            case "CF":
                Stage.Fill(A(0), A(1), A(2), A(3), A(4));
                Drawn(A(0), A(1), A(2), A(3));
                break;
            case "EC":
                if (A(0) == 0)
                {
                    Stage.Fill(0, 0, Surface.ScreenWidth, Surface.ScreenHeight, 0);
                    CoverScreen();
                }
                break;
            case "PF0" or "PF2": Fade(0, toWhite: false, A(0)); break;
            case "PF1" or "PF3": Fade(255, toWhite: false, A(0)); break;
            case "PW0" or "PW2": Fade(0, toWhite: true, A(0)); break;
            case "PW1" or "PW3": Fade(255, toWhite: true, A(0)); break;
        }
    }

    /// <summary>
    /// <c>G n</c> / <c>G n, key</c>: at the <c>J0</c> position, or the CG's own moved by <c>J1</c>,
    /// or its own; <c>PC</c> bit 1 loads the CG's palette, bit 4 draws it (<c>cmdp.c</c> <c>cg_fflg</c>).
    /// </summary>
    private void DrawCg(int number, int? key)
    {
        var jump = _jump;
        var relative = _jumpRelative;
        _jump = null;
        _jumpRelative = null;
        if (_cgs.Get(number) is not { } cg)
            return;
        _lastCg = number;
        var (x, y) = jump ?? (relative is var (dx, dy) ? (cg.X + dx, cg.Y + dy) : (cg.X, cg.Y));
        if ((_cgFlags & 1) != 0)
            Stage.Palette = cg.PaletteOver(Stage.Palette);
        if ((_cgFlags & 4) == 0)
            return;
        Stage.DrawCgRegion(cg, 0, 0, cg.Width, cg.Height, x, y, key);
        Drawn(x, y, cg.Width, cg.Height);
    }

    private void Fade(byte to, bool toWhite, int ticks)
    {
        FadeToWhite = toWhite;
        _fade = new FadeRequest(FadeAmount, to, FadeToWhite, ticks);
        FadeAmount = to;
    }
}
