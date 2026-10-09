using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Battles;

/// <summary>Sound and music requests of the battle; implemented by the presentation engine.</summary>
public interface IBattleAudio
{
    /// <summary><c>SP n, 1</c>: plays sound effect <paramref name="number"/> once.</summary>
    void PlaySound(int number);

    /// <summary><c>SQ l, r, 1</c>: plays two sound effects, one on each channel.</summary>
    void PlaySoundPair(int left, int right);

    /// <summary><c>SS n</c>: plays music track <paramref name="track"/> (0 stops).</summary>
    void PlayMusic(int track);
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
        Writer = new MessageWriter(Surface, glyphs);
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
        action?.Invoke();
    }

    /// <summary>Draws one core event.</summary>
    public void Show(ScriptEvent scriptEvent)
    {
        AfterKey();
        WaitsForKey = false;
        if (scriptEvent is not SlotPanelEvent and not PanelHighlightEvent)
            FinishRedraw();
        if (scriptEvent is not PauseEvent)
            _mapAnimation = MapAnimation.None;

        switch (scriptEvent)
        {
            case BattlefieldBackgroundEvent e: Background(e.Cg); break;
            case SlotPanelEvent e: SlotPanel(e.Panel, e.Record); break;
            case PanelHighlightEvent e: Highlight(e.Panel); break;
            case RoundHeaderEvent e: RoundHeader(e); break;
            case WindowEvent e: Window(e.Op.Value, e.CallArg.Value); break;
            case MessageEvent e:
                if (e.Text == OrderMessage)
                    _orderMessage = true;
                Writer.Print(Read(e.Text, [], e.StringVariables));
                break;
            case ValueMessageEvent e: Writer.Print(Read(e.Text, e.Values, null)); break;
            case PrintStringEvent e: PrintString(e.Slot.Value); break;
            case FlowErrorEvent e: Error(e.Text); break;
            case UnitInfoEvent e: UnitInfo(e.Unit, e.Order); break;
            case EnemyInfoEvent e: EnemyInfo(e.Cmdr); break;
            case ScoutReportEvent e: ScoutReport(e.Troops); break;
            case StrategyScreenEvent e: StrategyScreen(e.Side.Value); break;
            case EngagementSceneEvent e: EngagementScene(e); break;
            case ActionLabelsEvent e: ActionLabels(e); break;
            case EngagementSlideEvent e: Slide(e.Offset.Value); break;
            case CountersEvent e: Counters(e); break;
            case SoundPairEvent e: _audio?.PlaySoundPair(e.First.Value, e.Second.Value); break;
            case ShotsFrameEvent e: ShotsFrame(e.Step.Value); break;
            case ExchangeResultsEvent: ExchangeResults(); break;
            case WipedOutMarksEvent e: WipedOutMarks(e); break;
            case KeyWaitEvent: ExchangeKeyWait(); break;
            case MapEvent e: Map(e.Mode.Value); break;
            case ClearScreenEvent: Surface.Fill(0, 0, 640, 400, 0); break;
            case PauseEvent: MapPause(); break;
            case SoundEvent e when e.Mode != 3: _audio?.PlaySound(e.Sound.Value); break;
            case MusicEvent e: _audio?.PlayMusic(e.Track.Value); break;
        }
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

    private void RoundHeader(RoundHeaderEvent e)
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
        _afterKey = () => Surface.Copy(640, 0, 640, 400, 0, 0);
    }

    // ------------------------------------------------------------------ windows (§2)

    private int _winType;
    private int _winFrame;
    private int _portraitL, _portraitR, _centerCg;
    private bool _infoBox;

    /// <summary>The first line of an order message (<c>L_0129e</c>, <c>FIGT.ADV:801</c>).</summary>
    private static readonly ScriptRef OrderMessage = new("FIGT.ADV", 801);
    private bool _orderMessage;

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
                Surface.Copy(0, 500, 320, 220, 160, 180);
                Writer.Color = 15;
                CloseWindowState();
                break;
            case 0:
                CloseByType();
                break;
            case 1 or 5 or 11:
                if (_winType == 0)
                    break;
                WaitsForKey = true;
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
                break;
            case 312: ErasePortrait(left: true); break;
            case 321:
                if (_portraitR != 0)
                    ErasePortrait(left: false);
                GAt(callArg, 448, 144, keyed: true);
                _portraitR = callArg;
                break;
            case 322: ErasePortrait(left: false); break;
            case 331:
                if (_centerCg != 0)
                    EraseCenter();
                GAt(callArg, 176, 96);
                _centerCg = callArg;
                break;
            case 332: EraseCenter(); break;
        }
    }

    private void OpenWindow((int X, int Y, int W, int H, int DX, int DY) save, (int X, int Y, int W, int H) text,
        int frame, int? color, int type)
    {
        Surface.Copy(save.X, save.Y, save.W, save.H, save.DX, save.DY);
        Writer.Open(text.X, text.Y, text.W, text.H);
        G(frame, keyed: true);
        if (color is int c)
            Writer.Color = c;
        if (type < 40)
            Writer.Size = 24;
        _winFrame = frame;
        _winType = type;
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
        if (_winFrame != 0)
            G(_winFrame, keyed: true);
    }

    /// <summary>Op 2 (<c>L_0072b</c>): a full-screen window first gets its frame opaque.</summary>
    private void ClearPage()
    {
        Writer.Home();
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
        Writer.Print(Hankaku.ToHalfWidth(TextRun.FormatString(StringVar(slot), slot == 1 ? 8 : 18)));

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
        Writer.Print(Read(text, []));
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

    private void ScoutReport(Sys3Int troops)
    {
        Writer.Print(Read(new ScriptRef("FIGT_B.ADV", 655), []));
        if (troops != 0)
            Writer.Print(Read(new ScriptRef("FIGT_B.ADV", 662), [troops]));
    }
}
