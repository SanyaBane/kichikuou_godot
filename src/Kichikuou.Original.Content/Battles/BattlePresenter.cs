using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using SystemText = Kichikuou.Original.Content.Strategy.SystemText;
using WindowModel = Kichikuou.Original.Content.Strategy.MessageWindow;

namespace Kichikuou.Original.Content.Battles;

/// <summary>Sound and music requests of the battle; implemented by the presentation engine.</summary>
public interface IBattleAudio
{
    /// <summary>
    /// <c>PCMPLAY.ADV</c> (<see cref="SoundEvent"/>): mode 0 and 1 <c>SP RetVal, 1</c>, mode 2
    /// <c>SP RetVal, 0</c> (endless), mode 3 <c>SP 5, 1</c>, mode 10 and any other nothing
    /// (<c>:1-19</c>, <c>:28-43</c>; any other mode prints an error box in the original, which no
    /// caller reaches).
    /// </summary>
    static void PcmPlay(IBattleAudio? audio, SoundEvent e)
    {
        switch (e.Mode.Value)
        {
            case 0 or 1: audio?.PlaySound(e.Sound.Value, 1); break;
            case 2: audio?.PlaySound(e.Sound.Value, 0); break;
            case 3: audio?.PlaySound(5, 1); break;
        }
    }

    /// <summary>
    /// <c>SP n, loops</c>: plays sound effect <paramref name="number"/> that many times, 0 endlessly,
    /// in place of the one playing; <c>SP 0</c> stops it (<c>cmds.c</c> <c>commandSP</c>).
    /// </summary>
    void PlaySound(int number, int loops = 1);

    /// <summary><c>SQ l, r, 1</c>: plays two sound effects, one on each channel.</summary>
    void PlaySoundPair(int left, int right);

    /// <summary>
    /// <c>SS n</c>: plays music track <paramref name="track"/> (0 stops) <paramref name="loops"/>
    /// times, 0 endlessly (the <c>SL</c> before it; <c>Mix_PlayMusic</c>).
    /// </summary>
    void PlayMusic(int track, int loops = 0);
}

/// <summary>
/// Draws the battle the way the original does (docs/battle/screen-layout.md): every core
/// presentation event becomes the script's drawing commands on a <see cref="Surface"/>, whose
/// visible corner is the screen. Engine-independent; the presentation engine shows
/// <see cref="Surface"/>, supplies the font and audio, and calls <see cref="AfterKey"/> when
/// the player answers a key wait.
/// </summary>
public sealed partial class BattlePresenter
{
    private const string Figt = "FIGT.ADV";

    private readonly ScenarioText _text;
    private readonly CgLibrary _cgs;
    private readonly IGlyphRenderer _glyphs;
    private readonly IBattleAudio? _audio;
    private Action? _afterKey;

    /// <summary>Creates a presenter with a fresh surface.</summary>
    public BattlePresenter(ScenarioText text, CgLibrary cgs, IGlyphRenderer glyphs, IBattleAudio? audio = null)
        : this(text, cgs, glyphs, audio, new Surface())
    {
    }

    /// <summary>Creates a presenter that draws on <paramref name="surface"/>, shared with the strategic screens.</summary>
    public BattlePresenter(ScenarioText text, CgLibrary cgs, IGlyphRenderer glyphs, IBattleAudio? audio, Surface surface)
    {
        _text = text;
        _cgs = cgs;
        _glyphs = glyphs;
        _audio = audio;
        Surface = surface;
        Writer = new MessageWriter(Surface, glyphs) { Printed = WindowPrinted };
    }

    /// <summary>The engine's surface; its (0, 0)-(640, 400) corner is the screen.</summary>
    public Surface Surface { get; }

    /// <summary>The message text engine.</summary>
    public MessageWriter Writer { get; }

    /// <summary>The battle state (set by <see cref="Start"/>), read while events are shown.</summary>
    public GameState? State { get; private set; }

    /// <summary>The font.</summary>
    public IGlyphRenderer Glyphs => _glyphs;

    /// <summary>The scenario texts.</summary>
    public ScenarioText Texts => _text;

    /// <summary>
    /// Whether the original waits for a key after the last event shown (message waits, the round
    /// header, the end of the exchange). The battle asks the input for the key in Play; a
    /// playback uses it to pause.
    /// </summary>
    public bool WaitsForKey { get; private set; }

    /// <summary>
    /// Takes the battle state and draws what was on screen before the battle: the strategic map
    /// (its tiles and markers off screen, the view at the camera on screen).
    /// </summary>
    public void Start(GameState state)
    {
        State = state;
        Strategy = null;
        StrategyBackdrop = null;
        _strategyEffect = false;
        _scoutReplies.Clear();
        _shoutedSpell = null;
        Attack = null;
        RoundStarts();
        DrawMapTiles();
        BlitMapView();
    }

    /// <summary>
    /// Takes the game's state for a battle of a whole game: the map tiles and markers are drawn
    /// off screen for the battle's backgrounds; the screen is left as the game drew it.
    /// </summary>
    public void Attach(GameState state)
    {
        State = state;
        Strategy = null;
        StrategyBackdrop = null;
        _strategyEffect = false;
        _scoutReplies.Clear();
        _shoutedSpell = null;
        Attack = null;
        RoundStarts();
        DrawMapTiles();
    }

    /// <summary>
    /// Finishes the drawing the script does after the last event without an event of its own
    /// (the full-screen copy that ends a battlefield redraw). Call before waiting for input.
    /// </summary>
    public void Flush() => FinishRedraw();

    /// <summary>
    /// The drawing the script does right after a key wait (e.g. <c>WAO_SUB</c> op 11 erases the
    /// window text, the exchange removes the "unable to fight" marks). Runs once; the next
    /// <see cref="Show"/> runs it too if nobody called it.
    /// </summary>
    public void AfterKey()
    {
        var action = _afterKey;
        _afterKey = null;
        ExchangeResultWaits = false;
        ExchangeResultWipedOut = false;
        action?.Invoke();
    }

    /// <summary>Draws one core event.</summary>
    public void Show(ScriptEvent scriptEvent)
    {
        AfterKey();
        WaitsForKey = false;
        if (scriptEvent is not SlotPanelEvent and not PanelHighlightEvent)
            FinishRedraw();
        // A map animation's steps are the pauses after it; sound draws nothing (M 10 plays its sound before the blink:
        // FIGT_B's enemy attack, FIGT_B.ADV's lost province).
        if (scriptEvent is not (PauseEvent or SoundEvent or SoundPairEvent or MusicEvent))
            _mapAnimation = MapAnimation.None;
        if (scriptEvent is not (PauseEvent or SoundEvent or SoundPairEvent or MusicEvent or KeyWaitEvent))
            _leftBar = null;                                    // something is drawn: the 131 left goes with it
        // The blow is over at the first event after the result's key wait, or at one FIGTATTK does not raise: the core
        // has written it.
        if (Strike is not null && (_blowWaited || scriptEvent is not (ShotsFrameEvent or PauseEvent or SoundPairEvent
                or ExchangeResultsEvent or WipedOutMarksEvent or KeyWaitEvent)))
            (Strike, ExchangeCounters, _blowWaited) = (null, null, false);
        if (Strike is not null && scriptEvent is KeyWaitEvent)
            _blowWaited = true;

        switch (scriptEvent)
        {
            case BattlefieldBackgroundEvent e:
                ForgetResults();
                NoteEnemyDefences();
                Background(e.Cg);
                break;
            case SlotPanelEvent e: SlotPanel(e.Panel, e.Record); break;
            case PanelHighlightEvent e:
                _turnsStarted = true;
                Highlight(e.Panel);
                break;
            case RoundHeaderEvent e:
                ForgetResults();
                RoundStarts();
                DrawRoundHeader(e);
                break;
            case WindowEvent e: Window(e.Op.Value, e.CallArg.Value); break;
            case MessageEvent e:
                if (e.Text == OrderMessage)
                    _orderMessage = true;
                if (State is { } ordered && (e.Text == OrderMessage || e.Text == OwnOrderAttack || e.Text == OwnOrderDefend))
                    OrderedUnit = e.Text == OrderMessage ? ordered.Tmp0002.Value : ordered.Unit_Cur.Value;
                if (e.Text == ScoutArmyLine && State is { } scouted)
                {
                    var report = ScoutReport.Of(scouted, _portraitR);      // the army as the report will list it
                    Scout = report with
                    {
                        Reply = Read(ScoutReport.RanceLine, []),
                        Comment = report.Commented ? Read(report.CommentLine, []) : "",
                    };
                    _scoutReplies.Clear();
                    _scoutReplies.AddRange([ScoutReport.RanceLine, report.CommentLine]);
                }
                if (_scoutReplies.Remove(e.Text))
                    ScoutReplied = true;
                ScoutCalling = e.Text.File == ScoutArmyLine.File && e.Text.Line >= ScoutCallFirst && e.Text.Line < ScoutArmyLine.Line;
                ObserveEnemyAttack(e.Text);
                if (MagicSpells.OfLine(e.Text) is { } spell)
                    _shoutedSpell = spell;
                string printed = Read(e.Text, [], e.StringVariables);
                StrategyText(e.Text, printed);
                BoxText(e.Text, []);
                Print(printed, ReadBefore?.Invoke(e.Text) == true
                    && !SystemText.Is(e.Text, _winFrame == WindowModel.NarrationCg));
                break;
            case ValueMessageEvent e:
                if (e.Text == ScoutTotalsLine && Scout is not null)
                    ScoutReported = true;
                BoxText(e.Text, e.Values);
                Print(Read(e.Text, e.Values, e.StringVariables), false);    // its numbers change: never read before
                break;
            case PrintStringEvent e: PrintString(e.Slot.Value); break;
            case FlowErrorEvent e: Error(e.Text); break;
            case UnitInfoEvent e: UnitInfo(e.Unit, e.Order); break;
            case EnemyInfoEvent e: EnemyInfo(e.Cmdr); break;
            case ScoutReportEvent e:
                ScoutReported = Scout is not null;
                ScoutReportLine(e.Troops);
                break;
            case StrategyScreenEvent e:
                StrategyScreen(e.Side.Value);
                StartStrategy(e);
                break;
            case EngagementSceneEvent e: EngagementScene(e); break;
            case ActionLabelsEvent e: ActionLabels(e); break;
            case EngagementSlideEvent e: Slide(e.Offset.Value); break;
            case CountersEvent e: Counters(e); break;
            case SoundPairEvent e: _audio?.PlaySoundPair(e.First.Value, e.Second.Value); break;
            case ShotsFrameEvent e: ShotsFrame(e.Step.Value); break;
            case ExchangeResultsEvent: ExchangeResults(); break;
            case WipedOutMarksEvent e: WipedOutMarks(e); break;
            case KeyWaitEvent: ExchangeKeyWait(); break;
            case MapEvent e:
                ObserveAttackMap(e.Mode.Value);
                Map(e.Mode.Value);
                break;
            case ClearScreenEvent:
                Surface.Fill(0, 0, 640, 400, 0);
                PicturesCovered();
                break;
            case PauseEvent: MapPause(); break;
            // a scene's text size (a place's title at ZM 32, Gﾒﾙﾌｪｲｽ.ADV); the battle's own sizes are its port's
            case EngineEvent { Command: "ZM", Args: [var size, ..] }: Writer.Size = size.Value; break;
            case SoundEvent e when !ReturnsToAttack: IBattleAudio.PcmPlay(_audio, e); break;   // not the blink's after Back
            case MusicEvent e: _audio?.PlayMusic(e.Track.Value); break;
        }
        FollowStrategy();
        _lastEvent = scriptEvent;
    }

    private ScriptEvent? _lastEvent;

    // ------------------------------------------------------------------ drawing helpers

    /// <summary><c>G n</c> / <c>G n, 40</c> at the CG's own position.</summary>
    internal void G(int number, bool keyed = false)
    {
        if (_cgs.Get(number) is { } cg)
            Surface.DrawCg(cg, cg.X, cg.Y, keyed ? Surface.SpriteKey : null);
    }

    /// <summary><c>J0 x, y</c> + <c>G n</c>: at an absolute position.</summary>
    internal void GAt(int number, int x, int y, bool keyed = false)
    {
        if (_cgs.Get(number) is { } cg)
            Surface.DrawCg(cg, x, y, keyed ? Surface.SpriteKey : null);
    }

    /// <summary><c>J1 dx, dy</c> + <c>G n</c>: relative to the CG's own position.</summary>
    internal void GRel(int number, int dx, int dy, bool keyed = false)
    {
        if (_cgs.Get(number) is { } cg)
            Surface.DrawCg(cg, cg.X + dx, cg.Y + dy, keyed ? Surface.SpriteKey : null);
    }

    /// <summary>The text of a script location, with numbers and string variables filled in.</summary>
    internal string Read(ScriptRef source, IReadOnlyList<Sys3Int> numbers, IReadOnlyList<Sys3Int>? stringVariables = null)
    {
        try
        {
            return _text.Read(source).Format(numbers, v => State is { } s && v >= 0 && v < s.Strings.Length ? s.Strings[v] : "", stringVariables);
        }
        catch (Exception e) when (e is ScenarioTextException or ArgumentException or IOException)
        {
            Unresolved.Add($"{source}: {e.Message}");
            return $"[{source}]";
        }
    }

    /// <summary>Script locations whose text could not be read (shown as the location instead).</summary>
    public List<string> Unresolved { get; } = [];

    internal string Read(int line, params Sys3Int[] numbers) => Read(new ScriptRef(Figt, line), numbers);

    /// <summary>The text of a script line without numbers (a label, <see cref="BattleCommand.Label"/>).</summary>
    public string Text(ScriptRef source) => Read(source, []);

    private string StringVar(int index) => State is { } s && index < s.Strings.Length ? s.Strings[index] : "";

    // ------------------------------------------------------------------ battlefield (§1)

    private bool _redrawing;

    /// <summary>
    /// The last panel event, drawn once the next event shows its colour: 240 when a highlight
    /// follows, else the colour in force. Its texts are taken when the event arrives, because
    /// the core rewrites strings 1/2 or 4/5 and the cursors for every panel.
    /// </summary>
    private (int Panel, string Name, string Kind, Sys3Int Troops)? _pendingPanel;

    /// <summary>The panel rectangles of <c>L_0512e</c> (<c>FIGT.ADV:3268</c>), panels 1-8.</summary>
    internal static readonly (int X, int Y, int W, int H)[] Panels =
    [
        (12, 8, 182, 84), (12, 108, 182, 84), (12, 208, 182, 84), (12, 308, 182, 84),
        (448, 8, 182, 84), (448, 108, 182, 84), (448, 208, 182, 84), (448, 308, 182, 84),
    ];

    /// <summary><c>L_0306c</c> (<c>FIGT.ADV:1992-2009</c>), the start of a redraw (§1.1-§1.2).</summary>
    private void Background(Sys3Int cg)
    {
        _redrawing = true;
        if (cg == 0)
        {
            Surface.Fill(0, 400, 640, 400, 0);
        }
        else
        {
            var (x, y) = MapViewCorner();
            Surface.Copy(x + 1280, y, 640, 400, 0, 400);
            GAt(cg.Value, 176, 496);
        }
        Writer.Color = 18; // FIGT.ADV:2011
    }

    /// <summary><c>L_03236</c> (§1.3): kept until the next event decides its colour.</summary>
    private void SlotPanel(Sys3Int panel, Sys3Int record)
    {
        FinishPendingPanel();
        if (State is not { } s || panel.Value is < 1 or > 8)
            return;
        bool player = panel.Value <= 4;
        Sys3Int troops = player ? s.Units[s.Unit_Cur].Troops : s.Cmdrs[s.Cmdr_Cur].Troops;
        _pendingPanel = (panel.Value, StringVar(player ? 1 : 4), StringVar(player ? 2 : 5), troops);
    }

    /// <summary>
    /// <c>L_0320f</c> (§1.4): the caller sets <c>ZC 1, 240</c>, the panel is drawn and copied to the
    /// screen alone, then <c>ZC 1, 22</c>. A redraw right before it is finished first.
    /// </summary>
    private void Highlight(Sys3Int panel)
    {
        var pending = _pendingPanel;
        _pendingPanel = null;
        FinishRedraw();
        Writer.Color = 240;
        if (pending is { } p)
            DrawPanel(p);
        if (panel.Value is >= 1 and <= 8)
        {
            var (x, y, w, h) = Panels[panel.Value - 1];
            Surface.Copy(x - 7, y + 393, w + 14, h + 14, x - 7, y - 7);
        }
        Writer.Color = 22;
    }

    private void FinishPendingPanel()
    {
        if (_pendingPanel is { } p)
            DrawPanel(p);
        _pendingPanel = null;
    }

    private void FinishRedraw()
    {
        FinishPendingPanel();
        if (!_redrawing)
            return;
        _redrawing = false;
        Writer.Color = 22; // FIGT.ADV:2052
        Surface.Copy(0, 400, 640, 400, 0, 0); // L_031f9
        PicturesCovered();
    }

    /// <summary>One panel into the back buffer in the current text colour.</summary>
    private void DrawPanel((int Panel, string Name, string Kind, Sys3Int Troops) panel)
    {
        var (x, y, w, h) = Panels[panel.Panel - 1];
        Writer.Open(x, y + 400, w, h);
        GAt(836, x - 7, y - 7 + 400, keyed: true);
        Writer.Print(Hankaku.ToHalfWidth("［" + TextRun.FormatString(panel.Name, 13) + "］") + "\n");
        if (panel.Troops != 0)
            Writer.Print(Hankaku.ToHalfWidth(" " + TextRun.FormatString(panel.Kind, 14)) + "\n " + panel.Troops.Value);
    }

    // ------------------------------------------------------------------ round header (§3)

    private void DrawRoundHeader(RoundHeaderEvent e)
    {
        Surface.Copy(0, 0, 640, 400, 640, 0);
        Writer.Open(138, 75, 360, 260);
        G(887, keyed: true);
        if (e.IsDuel == 1)
            Writer.Print(Read(118));
        else if (e.Kind == 1)
            Writer.Print(Read(126));
        else if (e.Kind == 2)
            Writer.Print(Read(133));
        else if (e.Kind == 3)
            Writer.Print(Read(140));
        Writer.NewLine();
        if (e.TroopLimit == 5000 && e.IsDuel == 0)
            Writer.Print(Read(147));
        if (e.IsDuel == 1)
            Writer.Print(Read(151));
        if (e.TroopLimit != 5000 && e.Kind != 3 && e.IsDuel == 0)
            Writer.Print(Read(155));
        if (e.TroopLimit != 5000 && e.Kind == 3 && e.IsDuel == 0)
            Writer.Print(Read(159));
        Writer.Print(Read(162, e.TroopLimit));
        Writer.Print(Read(169));
        Writer.Print(Read(200, e.PlayerAdvantage, e.EnemyAdvantage));
        if (e.IsDuel == 0)
            Writer.Print(Read(209, e.RoundsLeft));
        WaitsForKey = true;
        RoundHeader = e;
        _afterKey = () =>
        {
            Surface.Copy(640, 0, 640, 400, 0, 0);
            RoundHeader = null;
        };
    }

    /// <summary>
    /// The round header while it waits for its key (CG 887 until the key's restore), else null. Our battle screen
    /// does not show the original's box (it is not in <see cref="Windows"/>): the battle's first is our opening
    /// (<see cref="BattleOpening"/>), the later ones are answered at once (docs/ui/battle-screen.md, "The opening").
    /// </summary>
    public RoundHeaderEvent? RoundHeader { get; private set; }

    // ------------------------------------------------------------------ the windows' shapes

    /// <summary>
    /// The shapes of the screen that are windows now (<see cref="WindowShape"/>): the open window's frame
    /// (<c>Ui_WinFrameCg</c>; the full-screen 815 of window 131 whole), the portraits of ops 311/321 and the
    /// centre picture of op 331 — not the round header (<see cref="RoundHeader"/>), which our screen draws its own way.
    /// <see cref="FigtSelScreen.Windows"/> and <see cref="BattleMenuScreen.Windows"/> add the input screens'.
    /// </summary>
    public IReadOnlyList<WindowShape> Windows
    {
        get
        {
            var shapes = new List<WindowShape>();
            // Not the "vs" window (our cards show the actions), a speech bubble (our dialogue panel shows its line, Speech),
            // an order message (the card's label flashes, OrderedUnit) nor an info box (ours, InfoBox).
            if (_winType != 0 && !ActionLabelsOpen && !SpeechOpen && OrderedUnit is null && !InfoBoxOpen && Shape(_winFrame, opaque: _winType is >= 31 and <= 39) is { } frame)
                shapes.Add(frame);
            if (!_covered.HasFlag(Pictures.PortraitL) && Shape(_portraitL, (0, 0)) is { } left)
                shapes.Add(left);
            if (!_covered.HasFlag(Pictures.PortraitR) && Shape(_portraitR, (448, 144)) is { } right)
                shapes.Add(right);
            if (!_covered.HasFlag(Pictures.Center) && Shape(_centerCg, (176, 96), opaque: true) is { } centre)
                shapes.Add(centre);
            return shapes;
        }
    }

    /// <summary>The pictures of ops 311/321/331 the screen no longer shows (their variables still hold them).</summary>
    [Flags]
    private enum Pictures { None = 0, PortraitL = 1, PortraitR = 2, Center = 4 }

    private Pictures _covered;

    /// <summary>The whole screen was drawn over (a field redraw, an engagement frame, the map): the pictures are gone from it.</summary>
    private void PicturesCovered() => _covered = Pictures.PortraitL | Pictures.PortraitR | Pictures.Center;

    /// <summary>CG <paramref name="number"/>'s shape at <paramref name="at"/> or its own position (null for none).</summary>
    internal WindowShape? Shape(int number, (int X, int Y)? at = null, bool opaque = false)
    {
        if (number == 0 || _cgs.Get(number) is not { } cg)
            return null;
        var (x, y) = at ?? (cg.X, cg.Y);
        return opaque ? WindowShape.Box(x, y, cg.Width, cg.Height) : WindowShape.Keyed(cg, x, y);
    }

    // ------------------------------------------------------------------ windows (§2)

    private int _winType;
    private int _winFrame;
    private int _portraitL, _portraitR, _centerCg;
    private bool _infoBox;

    /// <summary>The first line of an order message (<c>L_0129e</c>, <c>FIGT.ADV:801</c>).</summary>
    private static readonly ScriptRef OrderMessage = new("FIGT.ADV", 801);
    private bool _orderMessage;
    private bool _blowWaited;                           // the result's key wait has come: the next event ends the blow

    /// <summary>The acting unit's own standing orders' messages (<c>L_01f08</c> / <c>L_01f42</c>, <c>FIGT.ADV:1319</c>, <c>:1333</c>).</summary>
    private static readonly ScriptRef OwnOrderAttack = new("FIGT.ADV", 1319), OwnOrderDefend = new("FIGT.ADV", 1333);

    /// <summary>
    /// The unit an order message names while window 143 shows it (<c>L_0129e</c>: another unit's order, the unit of
    /// <c>Tmp0002</c>; <c>L_01f08</c> / <c>L_01f42</c>: the acting unit's own, <c>Unit_Cur</c>), else null. Our battle
    /// screen does not show the message (it is not in <see cref="Windows"/>): the order's label on that unit's card
    /// flashes instead and the message's key wait is answered at once (docs/ui/battle-screen.md, "The order messages").
    /// </summary>
    public int? OrderedUnit { get; private set; }

    private void Window(int op, int callArg)
    {
        switch (op)
        {
            case 111: OpenWindow((0, 0, 640, 150, 640, 0), (235, 8, 378, 141), 807, 15, 11); break;
            case 112: OpenWindow((0, 0, 640, 150, 640, 0), (235, 8, 378, 141), 808, 15, 12); break;
            case 113 or 123: OpenWindow((0, 250, 640, 150, 640, 150), (140, 278, 376, 140), 809, 15, 43); break;
            case 121: OpenWindow((0, 250, 640, 150, 640, 150), (43, 265, 378, 141), 810, 15, 21); break;
            case 122: OpenWindow((0, 250, 640, 150, 640, 150), (43, 265, 378, 141), 811, 15, 22); break;
            case 131:
                Surface.Copy(0, 296, 640, 104, 640, 300);
                Writer.Open(8, 305, 626, 140);
                _windowText.Open((8, 305, 626, 140));
                _leftBar = null;
                G(815);
                G(815, keyed: true);
                Writer.Color = 190;
                Writer.Size = 24;
                _winFrame = 815;
                _winType = 31;
                _portraitL = _portraitR = _centerCg = 0;
                break;
            case 141: OpenWindow((160, 180, 320, 220, 0, 500), (200, 196, 240, 200), 895, null, 41); break;
            case 142: OpenWindow((160, 180, 320, 220, 0, 500), (200, 196, 240, 200), 896, 11, 42); break;
            case 143 or 153: OpenWindow((160, 180, 320, 220, 0, 500), (200, 196, 240, 200), 894, null, 43); break;
            case 151: OpenWindow((160, 180, 320, 220, 0, 500), (224, 196, 240, 200), 897, null, 51); break;
            case 152: OpenWindow((160, 180, 320, 220, 0, 500), (224, 196, 240, 200), 898, 11, 52); break;
            case 10:
                CloseInfoBox();
                Surface.Copy(0, 500, 320, 220, 160, 180);
                Writer.Color = 15;
                CloseWindowState();
                EndStrategy();
                break;
            case 0:
                CloseByType();
                break;
            case 1 or 5 or 11:
                if (_winType == 0)
                    break;
                WaitsForKey = true;
                NoteAdvisorPage();
                _afterKey = op == 1 ? ClearPage : EraseText;
                break;
            case 2:
                ClearPage();
                break;
            case 311:
                if (_portraitL != 0)
                    ErasePortrait(left: true);
                GAt(callArg, 0, 0, keyed: true);
                _portraitL = callArg;
                _covered &= ~Pictures.PortraitL;
                break;
            case 312: ErasePortrait(left: true); break;
            case 321:
                if (_portraitR != 0)
                    ErasePortrait(left: false);
                GAt(callArg, 448, 144, keyed: true);
                _portraitR = callArg;
                _covered &= ~Pictures.PortraitR;
                break;
            case 322: ErasePortrait(left: false); break;
            case 331:
                if (_centerCg != 0)
                    EraseCenter();
                GAt(callArg, 176, 96);
                _centerCg = callArg;
                _covered &= ~Pictures.Center;
                break;
            case 332: EraseCenter(); break;
        }
    }

    // ------------------------------------------------------------------ the window's text for our message windows

    /// <summary>The open window's text as placed (<see cref="MessageWindow"/>): what the writer printed into its rectangle.</summary>
    private readonly TextBox _windowText = new((0, 0, 0, 0));

    /// <summary>The 131 left on the screen after op 0 (<see cref="CloseByType"/>: its copy onto itself leaves it) until the next drawing.</summary>
    private WindowModel? _leftBar;

    /// <summary>
    /// Whether the player read a message before, asked as it is printed (<see cref="Strategy.GamePresenter.ReadBefore"/>):
    /// the window's text keeps it for the already-read text's colour (docs/ui/read-text.md).
    /// </summary>
    public Func<ScriptRef, bool>? ReadBefore { get; set; }

    /// <summary>Whether the message being printed was read before; null while the text comes from none.</summary>
    private bool? _printing;

    private void WindowPrinted(int x, int y, int size, int color, char c)
    {
        if (_winType != 0 && Writer.Window == _windowText.Rect)
            _windowText.Put(x, y, size, color, c, _printing);
    }

    /// <summary>
    /// The open message window as our interface draws it (docs/ui/messages.md; our interface): a speech, a
    /// thought, a narration or 131 the battle or a scene it calls opens — the results after the field, the captures, the
    /// deaths —, from its frame and the text printed into it (<see cref="WindowModel.Of"/>); null for the other
    /// windows (the bubbles, the order messages, the "vs" window, the scout's and the strategy's, which our battle screen
    /// shows its own way) and when none is open. Our panel covers the original's frame whole, which the surface still draws.
    /// </summary>
    public WindowModel? MessageWindow =>
        _winType == 0 ? _leftBar
        : ActionLabelsOpen || SpeechOpen || OrderedUnit is not null || Scout is not null || ScoutCalling || ScoutReplied
            || Attack is not null || HidesLine ? null
        : WindowModel.Of(_winFrame, _windowText, Surface.Color);

    /// <summary>
    /// Draws onto <paramref name="screen"/>, the stage as composed from its view, what the open window's frame hides while our
    /// interface draws the window itself (<see cref="MessageWindow"/>, not 131's, whose frame is the whole screen): the screen
    /// under the frame as the window saved it on opening — what op 0 puts back (<see cref="CloseByType"/>) — and over it the
    /// pictures of ops 311/321/331 shown now. On the PC our panel lies over the frame anyway; held upright it leaves the
    /// picture (docs/ui/phone.md, §18), where the original's window and its text would stay.
    /// </summary>
    public void HideWindow(Surface screen)
    {
        if (_winType is 0 or (>= 31 and <= 39) || MessageWindow is null || _cgs.Get(_winFrame) is not { } frame)
            return;
        var (sx, sy, sw, sh, dx, dy) = _winSave;
        int x0 = Math.Max(frame.X, sx), y0 = Math.Max(frame.Y, sy);
        int x1 = Math.Min(frame.X + frame.Width, sx + sw), y1 = Math.Min(frame.Y + frame.Height, sy + sh);
        if (x1 <= x0 || y1 <= y0)
            return;
        var (vx, vy) = Surface.View;
        screen.CopyFrom(Surface, dx + x0 - sx, dy + y0 - sy, x1 - x0, y1 - y0, x0 - vx, y0 - vy);
        void Over(int number, int px, int py, Pictures which, bool keyed)
        {
            if (number == 0 || _covered.HasFlag(which) || _cgs.Get(number) is not { } cg)
                return;
            int ax = Math.Max(px, x0), ay = Math.Max(py, y0), bx = Math.Min(px + cg.Width, x1), by = Math.Min(py + cg.Height, y1);
            if (bx > ax && by > ay)
                screen.DrawCgRegion(cg, ax - px, ay - py, bx - ax, by - ay, ax - vx, ay - vy, keyed ? Surface.SpriteKey : null);
        }
        Over(_centerCg, 176, 96, Pictures.Center, keyed: false);
        Over(_portraitL, 0, 0, Pictures.PortraitL, keyed: true);
        Over(_portraitR, 448, 144, Pictures.PortraitR, keyed: true);
    }

    /// <summary>Where the open window saved the screen under it (<see cref="OpenWindow"/>'s first copy).</summary>
    private (int X, int Y, int W, int H, int DX, int DY) _winSave;

    private void OpenWindow((int X, int Y, int W, int H, int DX, int DY) save, (int X, int Y, int W, int H) text,
        int frame, int? color, int type)
    {
        Surface.Copy(save.X, save.Y, save.W, save.H, save.DX, save.DY);
        _winSave = save;
        Writer.Open(text.X, text.Y, text.W, text.H);
        _windowText.Open(text);
        _boxTexts.Clear();
        _leftBar = null;
        G(frame, keyed: true);
        if (color is int c)
            Writer.Color = c;
        if (type < 40)
            Writer.Size = 24;
        _winFrame = frame;
        _winType = type;
        _speech.Clear();
    }

    // ------------------------------------------------------------------ the speech bubbles' line

    private readonly System.Text.StringBuilder _speech = new();

    /// <summary>Whether a speech bubble is open: 141/142 (types 41/42, the player unit's) or 151/152 (51/52, the enemy commander's).</summary>
    private bool SpeechOpen => _winType is 41 or 42 or 51 or 52;

    /// <summary>
    /// The line the open speech bubble holds (null when none is open or it holds nothing yet): what was printed
    /// into it since it opened or its last key wait, read before when the bubble's text box says so (the box and the line
    /// are cleared together). Our screen shows it in its own dialogue panel and leaves the bubble out of the windows'
    /// layer (<see cref="Windows"/>); the stage still draws the original's.
    /// </summary>
    public BattleSpeech? Speech => SpeechOpen && BattleSpeech.Flow(_speech.ToString()) is { Length: > 0 } text
        ? new BattleSpeech(_winType < 50 ? BattleSide.Player : BattleSide.Enemy, _winType is 42 or 52, text, _windowText.ReadBefore)
        : null;

    /// <summary>
    /// Text into the open window, kept as the speech bubble's line too while one is open; <paramref name="read"/>: whether
    /// its message was read before, null when it comes from none.
    /// </summary>
    private void Print(string text, bool? read = null)
    {
        _printing = read;
        Writer.Print(text);
        _printing = null;
        if (SpeechOpen)
            _speech.Append(text);
    }

    /// <summary>Op 0 (<c>L_005fc</c>): restore by window type.</summary>
    private void CloseByType()
    {
        switch (_winType)
        {
            case >= 11 and <= 19:
                Surface.Copy(640, 0, 640, 150, 0, 0);
                Writer.Color = 15;
                break;
            case >= 21 and <= 29:
                Surface.Copy(640, 150, 640, 150, 0, 250);
                Writer.Color = 15;
                break;
            case >= 31 and <= 39:
                Writer.Color = 15; // the copy onto itself leaves the window visible
                _leftBar = MessageWindow;   // and ours stays over it until the next drawing
                break;
            case >= 41 and <= 49:
                Surface.Copy(640, 150, 640, 150, 0, 250);
                break;
        }
        CloseWindowState();
    }

    private void CloseWindowState()
    {
        _winType = 0;
        _winFrame = 0;
        _windowText.Clear();
        _boxTexts.Clear();
        _speech.Clear();
        ActionLabelsOpen = false;
        OrderedUnit = null;
        Scout = null;
        ScoutReported = false;
        ScoutReplied = false;
        ScoutCalling = false;
        HidesLine = false;
        _advisorSpeaks = false;
        if (_infoBox)
        {
            // FIGT.ADV:1013-1014 / 1112-1113 after the info box closes.
            _infoBox = false;
            Writer.Color = 22;
            Writer.Size = 24;
        }
        if (_orderMessage)
        {
            // FIGT.ADV:827 after the order message closes.
            _orderMessage = false;
            Writer.Color = 22;
        }
    }

    /// <summary>Op 11's tail (<c>WAO_SUB.ADV:516-518</c>): <c>Y 1, 1</c> and the frame redrawn over the text.</summary>
    private void EraseText()
    {
        Writer.Home();
        _windowText.Clear();
        _speech.Clear();
        if (_winFrame != 0)
            G(_winFrame, keyed: true);
    }

    /// <summary>Op 2 (<c>L_0072b</c>): a full-screen window first gets its frame opaque.</summary>
    private void ClearPage()
    {
        Writer.Home();
        _windowText.Clear();
        _speech.Clear();
        if (_winType is >= 31 and <= 39)
            G(_winFrame);
        if (_winFrame != 0)
            G(_winFrame, keyed: true);
    }

    /// <summary>Ops 312 / 322 (<c>WAO_SUB.ADV:321-390</c>).</summary>
    private void ErasePortrait(bool left)
    {
        int sx = left ? 0 : 448, sy = left ? 0 : 144;
        var (cx, cy) = PortraitCamera();
        if (State?.Ui_FaceOverMap == 1)
            Surface.Copy(sx + cx - 320 + 1280, sy + cy - 200, 192, 256, sx + 640, sy);
        else
            Surface.Fill(sx + 640, sy, 192, 256, 0);
        if (_centerCg != 0)
            GAt(_centerCg, 176 + 640, 96); // the overlays (not used by the battle) go to the screen
        Surface.Copy(sx + 640, sy, 192, 256, sx, sy);
        if (left)
            _portraitL = 0;
        else
            _portraitR = 0;
    }

    /// <summary>Op 332 (<c>WAO_SUB.ADV:423-436</c>), with the script's swapped <c>CF</c> arguments.</summary>
    private void EraseCenter()
    {
        var (cx, cy) = PortraitCamera();
        if (State?.Ui_FaceOverMap == 1)
            Surface.Copy(cx - 320 + 176 + 1280, cy - 200 + 96, 288, 192, 176, 96);
        else
            Surface.Fill(288, 192, 176, 96, 0);
        _centerCg = 0;
    }

    /// <summary><c>L_00b0f</c>: the camera clamped into [320, 1600] × [200, 600].</summary>
    private (int X, int Y) PortraitCamera()
    {
        if (State is not { } s)
            return (320, 200);
        return (Math.Clamp((int)s.MapCamX.Value, 320, 1600), Math.Clamp((int)s.MapCamY.Value, 200, 600));
    }

    /// <summary><c>MP slot, width</c> of the death scenes: 8 cells for slot 1, 18 for slot 4 (§2.7).</summary>
    private void PrintString(int slot) =>
        Print(Hankaku.ToHalfWidth(TextRun.FormatString(StringVar(slot), slot == 1 ? 8 : 18)));

    /// <summary>
    /// The two error boxes: <c>FIGT.ADV:772</c> prints into the open window 141;
    /// <c>FIGT.ADV:1260</c> prints into a window off screen and waits with <c>A</c>.
    /// </summary>
    private void Error(ScriptRef text)
    {
        if (text.Line == 1260)
        {
            WaitsForKey = true;
            return;
        }
        Print(Read(text, []));
    }

    // ------------------------------------------------------------------ info boxes (§6.1)

    private void UnitInfo(Sys3Int unitNumber, Sys3Int order)
    {
        if (State is not { } s)
            return;
        var unit = s.Units[unitNumber];
        _infoBox = true;
        Writer.Print(Hankaku.ToHalfWidth("［" + s.Strings[(s.Unit_StrBase + (unitNumber - 1) * 3).Value] + "］"));
        Writer.NewLine();
        Writer.Size = 16;
        Writer.Print(Read(860));
        if (order == 0)
            Writer.Print(Read(863));
        if (order == 1)
            Writer.Print(Read(867));
        if (order == 2)
            Writer.Print(Read(871));
        Writer.Print(Read(874, unit.Level, unit.HP)); // Battle LV and HP, one run
        int kind = s.Bt_Kind.Value;
        if (unit.Troops == 0)
        {
            Writer.Print(Read(885, unit.Atk, unit.Def)); // two lines, each ending in R
        }
        else
        {
            Writer.Print(Read(896, unit.Troops, unit.TroopAtk));
            if (kind is >= 1 and <= 3)
                Modifier(kind switch { 1 => unit.OffAtkMod, 2 => unit.DefAtkMod, _ => unit.DgnAtkMod }, 905 + (kind - 1) * 14);
            Writer.Print(Read(945, unit.TroopDef));
            if (kind is >= 1 and <= 3)
                Modifier(kind switch { 1 => unit.OffDefMod, 2 => unit.DefDefMod, _ => unit.DgnDefMod }, 951 + (kind - 1) * 14);
        }
        // :992 — outside the troops branch: with no troops it leaves an empty line.
        Writer.Print(Read(992, unit.Magic, unit.Attacks));
        Sys3Int partner = unit.Partner;
        if (partner != 0 && (partner == s.Bt_PlayerSlot1 || partner == s.Bt_PlayerSlot2 || partner == s.Bt_PlayerSlot3 || partner == s.Bt_PlayerSlot4))
            Writer.Print(Read(1003));
    }

    /// <summary>
    /// The modifier after a troop value: > 10 prints <c>(-</c> mod − 10, &lt; 10 <c>(+</c> mod, and
    /// <c>)</c> always — so 10 prints a lone <c>)</c> (script quirk). <paramref name="minusLine"/>
    /// is the line of <c>'（－'</c>; <c>'（＋'</c> is 5 lines later and <c>'）'</c> 9.
    /// </summary>
    private void Modifier(Sys3Int m, int minusLine, bool close = true)
    {
        if (m > 10)
            Writer.Print(Read(minusLine, m - 10));
        if (m < 10)
            Writer.Print(Read(minusLine + 5, m));
        if (close)
            Writer.Print(Read(minusLine + 9));
    }

    private void EnemyInfo(Sys3Int cmdrNumber)
    {
        if (State is not { } s)
            return;
        var cmdr = s.Cmdrs[cmdrNumber];
        _infoBox = true;
        Writer.Print(Hankaku.ToHalfWidth("［" + s.Strings[(s.Cmdr_StrBase + (cmdrNumber - 1) * 3).Value] + "］"));
        Writer.NewLine();
        Writer.Size = 16;
        Writer.Print(Read(1043, cmdr.HP));
        if (cmdr.Troops == 0)
        {
            Writer.Print(Read(1049, cmdr.Atk, cmdr.Def)); // two lines, each ending in R
        }
        else
        {
            Writer.Print(Read(1060, cmdr.Troops, cmdr.TroopAtk));
            Modifier(cmdr.TroopAtkMod, 1068, close: false);
            Writer.Print(Read(1077, cmdr.TroopDef)); // ")", R and the Troop Defense line, one run
            Modifier(cmdr.TroopDefMod, 1083);
        }
        // :1095 — outside the troops branch: with no troops it leaves an empty line.
        Writer.Print(Read(1095, cmdr.Magic, cmdr.Attacks));
    }

    // ------------------------------------------------------------------ scout report (§6.3)

    private static readonly ScriptRef ScoutArmyLine = new("FIGT_B.ADV", 587);
    private static readonly ScriptRef ScoutTotalsLine = new("FIGT_B.ADV", 613);

    /// <summary>
    /// The ninja's report while window 113 lists it (docs/ui/battle-screen.md, "The scout report"): from "The
    /// Approaching Enemy" (<c>FIGT_B.ADV:587</c>) to the window's close after the totals; null otherwise. Taken from
    /// GameState when that line prints, so our overlay shows the whole army from the report's first key wait.
    /// </summary>
    public ScoutReport? Scout { get; private set; }

    /// <summary>
    /// Whether the report has gone past its first key wait: a commander's line or the totals are printed. Our
    /// overlay shows them all already, so the key waits from here to the window's close are answered at once.
    /// </summary>
    public bool ScoutReported { get; private set; }

    /// <summary>
    /// Whether Rance's answer to the report (<c>FIGT_B.ADV:629</c>, window 111) or Maris's comment (<c>:636-647</c>, window
    /// 121) is in its window, until it closes. Our overlay shows both with the report already (<see cref="ScoutReport.Reply"/>,
    /// <see cref="ScoutReport.Comment"/>), so their key waits are answered at once.
    /// </summary>
    public bool ScoutReplied { get; private set; }

    /// <summary>
    /// Whether a line between the ninja's choice and the report is in its window (owner, 2026-10-16): the ninja called,
    /// how they come, their answer, Rance's order, the wait and "… returned with information" (<c>FIGT_B.ADV:393-586</c>,
    /// only the scout's path reaches them), until the window closes or another line prints. Our overlay comes straight
    /// after the choice, so their key waits are answered at once.
    /// </summary>
    public bool ScoutCalling { get; private set; }

    /// <summary>The first line of the ninjas' calls (<c>L_008c0</c>, Kanami's).</summary>
    private const int ScoutCallFirst = 393;

    /// <summary>The lines said after the last report — Rance's, then Maris's — from the report until each prints.</summary>
    private readonly List<ScriptRef> _scoutReplies = [];

    private void ScoutReportLine(Sys3Int troops)
    {
        Writer.Print(Read(new ScriptRef("FIGT_B.ADV", 655), []));
        if (troops != 0)
            Writer.Print(Read(new ScriptRef("FIGT_B.ADV", 662), [troops]));
    }

    // ------------------------------------------------------------------ the strategy (§6.2)

    /// <summary>The announcements (<c>FIGT作.ADV:37</c> the player's, <c>:378</c> the enemy's) and the effects' first lines (<c>:321</c>, <c>:463</c>).</summary>
    private static readonly ScriptRef PlayerAnnouncement = new("FIGT作.ADV", 37), EnemyAnnouncement = new("FIGT作.ADV", 378);
    private static readonly ScriptRef PlayerEffect = new("FIGT作.ADV", 321), EnemyEffect = new("FIGT作.ADV", 463);

    /// <summary>
    /// A successful strategy from its screen (<see cref="StrategyScreenEvent"/>) to the close of its last box, the effect's
    /// (docs/ui/battle-screen.md, "The strategy's cut-in"), filled in as the script goes; null otherwise. Our cut-in shows it.
    /// </summary>
    public BattleStrategy? Strategy { get; private set; }

    /// <summary>
    /// Whether a key wait of the strategy's is one our cut-in answers at once: the announcement's and the effect's (window
    /// 143), which the cut-in's title and label show — the strategist's line (its bubble) stays the player's click.
    /// </summary>
    public bool StrategyAnswered => Strategy is not null && _winType == 43;

    /// <summary>
    /// The original's strategy screen before its first box opens and without the strategist's face — its field and the
    /// strategist's troops (RGBA, the screen's stored pixels, <see cref="Surface.ReadArea"/>) — held with
    /// <see cref="Strategy"/>: our cut-in's backdrop, which shows none of the boxes the script opens over it nor what an
    /// earlier dialogue left on the screen (owner, 2026-10-16), and only our portrait of the strategist.
    /// </summary>
    public byte[]? StrategyBackdrop { get; private set; }

    private bool _strategyEffect;                       // the effect's box is open: its close ends the strategy

    private void StartStrategy(StrategyScreenEvent e)
    {
        if (State is not { } s)
            return;
        bool player = e.Side == 1;
        int record = e.Strategist.Value;
        int face = (player ? s.Units[e.Strategist].FaceCg : s.Cmdrs[e.Strategist].FaceCg).Value;
        Strategy = new BattleStrategy(player ? BattleSide.Player : BattleSide.Enemy, record, face, "", s.Bt_StrategyResult.Value, null);
        StrategyBackdrop = _strategyField;              // StrategyScreen has just drawn it
        _strategyEffect = false;
    }

    private byte[]? _strategyField;

    /// <summary>A text of the strategy's boxes: the announcement is kept, the effect's first line marks the last box.</summary>
    private void StrategyText(ScriptRef text, string printed)
    {
        if (Strategy is not { } strategy)
            return;
        if (text == PlayerAnnouncement || text == EnemyAnnouncement)
            Strategy = strategy with { Announcement = BattleSpeech.Flow(printed) };
        if (text == PlayerEffect || text == EnemyEffect)
            _strategyEffect = true;
    }

    /// <summary>After each event: the result as the script has set it, the strategist's line while its bubble holds it.</summary>
    private void FollowStrategy()
    {
        if (Strategy is not { } strategy || State is not { } s)
            return;
        var line = Speech ?? strategy.Line;
        if (strategy.Result != s.Bt_StrategyResult.Value || line != strategy.Line)
            Strategy = strategy with { Result = s.Bt_StrategyResult.Value, Line = line };
    }

    /// <summary>A box closes (op 10): after the effect's, the strategy is over.</summary>
    private void EndStrategy()
    {
        if (!_strategyEffect)
            return;
        _strategyEffect = false;
        Strategy = null;
        StrategyBackdrop = null;
    }
}
