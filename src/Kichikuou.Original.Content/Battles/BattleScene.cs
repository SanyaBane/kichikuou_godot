using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>What one side does in an exchange, as our screen names it (<see cref="BattleScene.Actions"/>).</summary>
public enum ExchangeAction
{
    /// <summary>Not known, or none (between exchanges; the enemy's side of a player's special).</summary>
    None,
    /// <summary>The Direct attack (action 0).</summary>
    Melee,
    /// <summary>The Indirect attack (action 1).</summary>
    Bow,
    /// <summary>The Magic attack (action 2).</summary>
    Magic,
    /// <summary>A special attack of the player's unit (<c>Bt_SpecialAtk</c> not 0).</summary>
    Special,
    /// <summary>Defending (action 99).</summary>
    Defense,
}

/// <summary>
/// The armies' scene of our own battle screen (docs/ui/battle-screen.md, "The scene between
/// exchanges"): a 640×256 picture — the battlefield and two armies — that stays on top for the whole
/// battle. During an exchange it shows the engagement's pictures (<see cref="EngagementSceneEvent"/>)
/// with the exchange's numbers in the picture's geometry (what the original draws below row 255 moved
/// up); between exchanges the turn's actor and its target (<see cref="Observe"/>), while the player
/// chooses the acting unit and the enemy whose card the pointer is on (<see cref="Hover"/>). Follows the battle's events with
/// the state as it is when each is shown (<see cref="Observe"/>); draws on its own <see cref="Surface"/>,
/// never on the stage. Besides the whole picture it keeps its layers, so a view can slide the armies in and
/// out (<see cref="FieldY"/>, <see cref="PlayerArmy"/>). Engine-independent.
/// </summary>
public sealed class BattleScene
{
    /// <summary>The picture's size in script pixels (its top-left corner of <see cref="Surface"/>).</summary>
    public const int Width = 640, Height = 256;

    /// <summary>Where the digit sheets go, right of the picture: <c>FIGT</c>'s (CG 888) and <c>FIGTATTK</c>'s (CG 893).</summary>
    private const int CounterSheetY = 0, AttackSheetY = 150;

    /// <summary>The y the original's HP counters (y 368) move up to.</summary>
    public const int HpY = 224;

    /// <summary>The y the "unable to fight" marks (CG 891/892, y 256) move up to.</summary>
    public const int CmdrMarkY = 112;

    /// <summary>
    /// Without <see cref="Numbers"/> (our screen, whose counters stand at the scene's foot, owner 2026-10-16) the marks
    /// go higher: the commanders' to this y (their text, rows 71-124 of CG 891/892, ends above the counters at row 180)…
    /// </summary>
    public const int OwnCmdrMarkY = 56;

    /// <summary>…and the troops' (CG 889/890, text in rows 66-184) this many rows up, so that both fit one above the other.</summary>
    public const int OwnTroopMarkRise = 60;

    /// <summary>
    /// The picture's layers on <see cref="Surface"/>, each <see cref="Width"/> × <see cref="Height"/>: the
    /// battlefield alone at (0, <see cref="FieldY"/>); the player's army at (0, <see cref="ArmyY"/>) and the
    /// enemy's at (<see cref="EnemyArmyX"/>, <see cref="ArmyY"/>), on the colour key
    /// (<see cref="Surface.SpriteKey"/>) where they draw nothing; the front — the battlefield's overlay and the
    /// exchange's numbers, on the key — at (0, <see cref="FrontY"/>). The battlefield, the enemy's army, the
    /// player's over it (the picture draws them so) and the front make the picture.
    /// </summary>
    public const int FieldY = 560, ArmyY = 820, EnemyArmyX = 640, FrontY = 1080;

    /// <summary>Where the overlay alone is kept (right of the battlefield), copied under the front's numbers at every change.</summary>
    internal const int OverlayX = 640;

    /// <summary>
    /// A side's army in the picture: its record (the unit or the commander; 0: none drawn), the x span its
    /// sprites cover, and its arrival — a number that changes whenever the side's army changes, so a view
    /// that sees only the last of several changes (the armies part after an exchange and the same unit acts
    /// next) still knows the army left and came back.
    /// </summary>
    public readonly record struct Army(int Record, int Left, int Right, int Arrival);

    private readonly CgLibrary _cgs;
    private Mode _mode;
    private int _background;
    private bool _overlay;
    private IReadOnlyList<int> _enemySprites = [];
    private IReadOnlyList<int> _playerSprites = [];
    private int _unit, _cmdr;                                   // the records in focus between exchanges
    private bool _engaged;                                      // the turn has had its exchange
    private int _drawnUnit, _drawnCmdr;                         // the records the picture was last drawn with
    private int _playerArrival, _enemyArrival;
    private bool _choosing;
    private int _hovered;                                       // the enemy whose card the pointer is on while choosing
    private (int UnitTroops, int CmdrTroops, int UnitHp, int CmdrHp)? _counters;
    private int? _shotsStep;
    private bool _results;
    private WipedOutMarksEvent? _marks;
    private bool _dropCmdrMarks;
    private GameState? _state;

    private enum Mode { Between, Engagement }

    /// <summary>Creates a scene drawing CGs from <paramref name="cgs"/>.</summary>
    public BattleScene(CgLibrary cgs)
    {
        _cgs = cgs;
        Surface = new Surface(1280, FrontY + Height);
    }

    /// <summary>
    /// The scene's surface: the layers (<see cref="FieldY"/>), the digit sheets right of (0, 0), and there the
    /// whole picture when <see cref="ComposeWhole"/> puts it together.
    /// </summary>
    public Surface Surface { get; }

    /// <summary>The player's army in the picture's layer (changes with <see cref="PictureVersion"/>).</summary>
    public Army PlayerArmy { get; private set; }

    /// <summary>The enemy's army in the picture's layer (changes with <see cref="PictureVersion"/>).</summary>
    public Army EnemyArmy { get; private set; }

    /// <summary>
    /// Whether the battle's field or an exchange is on: the split screen shows (not the pre-battle map,
    /// strategy or scout screens, nor while a scene the battle calls draws on the stage).
    /// </summary>
    public bool Active { get; private set; }

    /// <summary>Counts the scene's changes; a view uploads the picture when it moves.</summary>
    public int Version { get; private set; }

    /// <summary>
    /// Counts the changes of the picture under the numbers (the battlefield and the armies' layers), not
    /// every number of an exchange; each layer has its own count besides (<see cref="FieldVersion"/>).
    /// </summary>
    public int PictureVersion { get; private set; }

    /// <summary>Where on <see cref="Surface"/> <see cref="ComposeWhole"/> puts the picture under the numbers (<see cref="Width"/> × <see cref="Height"/>).</summary>
    public const int PictureY = BaseY;

    /// <summary>
    /// Whether the front shows the exchange's numbers — the TROOPS and HP counters, the shots flying across and the damages
    /// (on by default, as the original draws them). Our battle screen turns them off (owner, 2026-10-15): its own
    /// counters at the scene's foot and its cards show the troops and HP and run them to the result (2026-10-16). The
    /// wiped-out marks stay, moved up above those counters (<see cref="OwnCmdrMarkY"/>, <see cref="OwnTroopMarkRise"/>).
    /// </summary>
    public bool Numbers
    {
        get => _numbers;
        set
        {
            if (value == _numbers)
                return;
            _numbers = value;
            _frontNumbers = true;                               // the front drawn again without (or with) them
            Render();
        }
    }

    private bool _numbers = true;

    /// <summary>The player unit and the enemy commander the picture shows between exchanges (0: none).</summary>
    public (int Unit, int Cmdr) InFocus => _choosing && _mode == Mode.Between ? (_unit, _hovered) : (_unit, _cmdr);

    /// <summary>
    /// Whether the player is choosing the acting unit's command (<c>FIGT_SEL</c>): the picture shows the
    /// acting unit and, on the right, only the enemy whose card the pointer is on (<see cref="Hover"/>).
    /// When the choice ends that enemy stays — an attack's target is the card its banner was on, and the
    /// target's highlight follows — so a view does not move its army out and in again.
    /// </summary>
    public bool Choosing
    {
        get => _choosing;
        set
        {
            if (_choosing == value)
                return;
            _choosing = value;
            if (!value && _mode == Mode.Between)
                _cmdr = _hovered;
            _hovered = 0;
            if (_mode == Mode.Between)
                Render();
        }
    }

    /// <summary>Whether an exchange is on (from its engagement picture to the next redraw of the field).</summary>
    public bool InEngagement => _mode == Mode.Engagement;

    /// <summary>
    /// What each side does in the exchange on (from its action labels, <c>FIGT.ADV:1739-1800</c>, to the
    /// field's redraw; <see cref="ExchangeAction.None"/> before them, between exchanges, and for the enemy's
    /// side of a player's special, whose label line is blank).
    /// </summary>
    public (ExchangeAction Player, ExchangeAction Enemy) Actions { get; private set; }

    /// <summary>A side's action in <c>Bt_PlayerAction</c> / <c>Bt_EnemyAction</c> (0 direct, 1 indirect, 2 magic, 99 defend).</summary>
    private static ExchangeAction Action(Sys3Int value) => value.Value switch
    {
        0 => ExchangeAction.Melee,
        1 => ExchangeAction.Bow,
        2 => ExchangeAction.Magic,
        99 => ExchangeAction.Defense,
        _ => ExchangeAction.None,
    };

    /// <summary>A battle begins: nothing shown until its field is (<see cref="Active"/>).</summary>
    public void Start(GameState state)
    {
        _state = state;
        Active = false;
        _mode = Mode.Between;
        _unit = _cmdr = _hovered = _drawnUnit = _drawnCmdr = 0;
        _engaged = _choosing = false;
        _fieldKey = _enemyKey = _playerKey = null;                       // the new battle's picture drawn whole
        ClearNumbers();
    }

    /// <summary>The battle ends.</summary>
    public void Stop() => Active = false;

    /// <summary>
    /// The card the pointer is on while the player chooses (<see cref="Choosing"/>; null: none): an
    /// enemy's army goes into the picture on the right until the pointer leaves its card; the player's
    /// own cards change nothing (the acting unit stays). Ignored at any other time.
    /// </summary>
    public void Hover(BattlePlace? place)
    {
        if (_state is not { } s || !_choosing || _mode != Mode.Between)
            return;
        int record = place is { Side: BattleSide.Enemy } enemy ? BattleBoard.Record(s, enemy) : 0;
        if (record == _hovered)
            return;
        _hovered = record;
        Render();
    }

    /// <summary>
    /// Follows one battle event (the state as it is when the event is shown; <paramref name="acting"/>:
    /// the turn's actor, <see cref="RoundOrder.Acting"/>, after the same event). Between exchanges the
    /// picture shows (owner, 2026-10-13): nothing at a round's start; from a turn's first highlight its
    /// actor alone, then the target the next highlight names (the enemy's pick, the player's attack);
    /// after an exchange nothing from the field's redraw on — the armies part, even when the same unit acts
    /// next (owner, 2026-10-13); after a turn without one (a defence) its actor alone.
    /// </summary>
    public void Observe(ScriptEvent scriptEvent, GameState state, BattlePlace? acting = null)
    {
        _state = state;
        if (_dropCmdrMarks && _marks is { } shown)
        {
            // FIGTATTK.ADV:248-253: after the key the commanders' marks are removed, the troops' stay.
            _marks = shown with { PlayerCmdr = false, EnemyCmdr = false };
            _dropCmdrMarks = false;
            Render();
        }
        switch (scriptEvent)
        {
            case ActionLabelsEvent labels when _mode == Mode.Engagement:
                bool special = state.Bt_SpecialAtk != 0;
                Actions = (labels.PlayerLabel is null ? ExchangeAction.None : special ? ExchangeAction.Special : Action(state.Bt_PlayerAction),
                    labels.EnemyLabel is null || special ? ExchangeAction.None : Action(state.Bt_EnemyAction));
                break;
            case RoundHeaderEvent:
                _mode = Mode.Between;
                _unit = _cmdr = 0;                                      // an empty field until the round's first turn
                _engaged = false;
                Active = true;
                ClearNumbers();
                Render();
                break;
            case BattlefieldBackgroundEvent:
                _mode = Mode.Between;
                if (_engaged)
                    _unit = _cmdr = 0;                                  // after an exchange the armies part (owner, 2026-10-13)
                else
                    KeepActor(state, acting);
                Active = true;
                ClearNumbers();
                Render();
                break;
            case PanelHighlightEvent highlight when BattleBoard.PanelPlace(highlight.Panel.Value) is { } place:
                Highlight(state, place, actor: place == acting);
                break;
            case EngagementSceneEvent e:
                Active = true;
                _engaged = true;
                _mode = Mode.Engagement;
                _background = e.Background.Value;
                _overlay = e.Overlay;
                _enemySprites = [.. e.EnemySprites.Select(c => c.Value)];
                _playerSprites = [.. e.PlayerSprites.Select(c => c.Value)];
                _unit = state.Unit_Cur.Value;
                _cmdr = state.Cmdr_Cur.Value;
                ClearNumbers();
                Render();
                break;
            case CountersEvent c when _mode == Mode.Engagement:
                _counters = (c.UnitTroops.Value, c.CmdrTroops.Value, c.UnitHP.Value, c.CmdrHP.Value);
                Render();
                break;
            case ShotsFrameEvent shots when _mode == Mode.Engagement:
                _shotsStep = shots.Step.Value;
                Render();
                break;
            case ExchangeResultsEvent when _mode == Mode.Engagement:
                _shotsStep = null;
                _results = true;
                Render();
                break;
            case WipedOutMarksEvent marks when _mode == Mode.Engagement:
                _marks = marks;
                Render();
                break;
            case KeyWaitEvent when _marks is { PlayerCmdr: true } or { EnemyCmdr: true }:
                _dropCmdrMarks = true;
                break;
            case MapEvent { Mode.Value: 0 or 11 or 12 } or StrategyScreenEvent or ClearScreenEvent:
                Active = false;                                         // the original's own full screens
                break;
            case EngineEvent { Command: "G" or "CC" or "CE" or "CS" or "CF" or "CM" or "CB" or "CL" } or WindowEvent { Op.Value: 131 }:
                // A scene the battle calls (死亡捕獲's deaths, Gｼｨﾙ 28 …) draws on the stage itself: its
                // screen is shown whole until the battle's field or exchange is drawn again.
                Active = false;
                break;
        }
    }

    /// <summary>
    /// A panel highlighted (<c>L_0320f</c>): the turn's <paramref name="actor"/> alone on its side, or the
    /// target it names (the enemy's pick, <c>FIGT.ADV:1424</c>; the player's attack, <c>:1592</c>) added on its own.
    /// </summary>
    private void Highlight(GameState s, BattlePlace place, bool actor)
    {
        int record = BattleBoard.Record(s, place);
        if (record == 0)
            return;
        if (actor)
        {
            _engaged = false;
            _hovered = 0;
            (_unit, _cmdr) = place.Side == BattleSide.Player ? (record, 0) : (0, record);
        }
        else if (place.Side == BattleSide.Player)
        {
            _unit = record;
        }
        else
        {
            _cmdr = record;
        }
        if (_mode == Mode.Between)
            Render();
    }

    /// <summary>The armies that are no longer in a slot (fallen) leave the picture.</summary>
    private void DropFallen(GameState s)
    {
        if (!BattleBoard.Cards(s, BattleSide.Player).Any(c => c?.Record == _unit))
            _unit = 0;
        if (!BattleBoard.Cards(s, BattleSide.Enemy).Any(c => c?.Record == _cmdr))
            _cmdr = 0;
    }

    /// <summary>A redraw in a turn without an exchange (a defence, a round's start): only the actor stays, if any.</summary>
    private void KeepActor(GameState s, BattlePlace? acting)
    {
        if (acting?.Side != BattleSide.Player)
            _unit = 0;
        if (acting?.Side != BattleSide.Enemy)
            _cmdr = 0;
        DropFallen(s);
    }

    /// <summary>A new exchange or none: its numbers and its sides' actions are gone.</summary>
    private void ClearNumbers()
    {
        Actions = default;
        _counters = null;
        _shotsStep = null;
        _results = false;
        _marks = null;
        _dropCmdrMarks = false;
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>
    /// Draws again what changed of the picture (owner, 2026-10-13: a pointer moving over the enemies' cards
    /// must not cost frames): the palette as the original's drawing loads it, then only the layers whose CGs
    /// changed (<see cref="FieldY"/>; a hovered enemy redraws the enemy's army alone), and the front when
    /// it shows numbers or its overlay changed.
    /// </summary>
    private void Render()
    {
        if (_state is not { } s)
            return;
        if (_mode == Mode.Between)
            ComposeBetween(s);
        var (unit, cmdr) = _mode == Mode.Between ? InFocus : (_unit, _cmdr);
        if (unit != _drawnUnit)
            (_drawnUnit, _playerArrival) = (unit, _playerArrival + 1);
        if (cmdr != _drawnCmdr)
            (_drawnCmdr, _enemyArrival) = (cmdr, _enemyArrival + 1);
        if (!_sheetsDrawn)
        {
            _sheetsDrawn = true;
            Sheet(888, CounterSheetY);
            Sheet(893, AttackSheetY);
        }
        string field = $"{_background} {_overlay}";
        string enemy = $"{string.Join(",", _enemySprites)} {cmdr} {_enemyArrival}";
        string player = $"{string.Join(",", _playerSprites)} {unit} {_playerArrival}";
        bool fieldChanged = field != _fieldKey;
        if (fieldChanged || enemy != _enemyKey || player != _playerKey)
        {
            // The palette as FIGT leaves it: the battlefield, the sprites, the overlay, then the digit sheets.
            var palette = Surface.Palette;
            int[] loads = [_background, .. _enemySprites, .. _playerSprites, _overlay ? _background + 1 : 0, 888, 893];
            foreach (int number in loads)
            {
                if (number != 0 && _cgs.Get(number) is { } cg)
                    palette = cg.PaletteOver(palette);
            }
            Surface.Palette = palette;
            if (fieldChanged)
            {
                _fieldKey = field;
                Surface.Fill(0, FieldY, Width, Height, 0);
                Put(_background, 0, FieldY, keyed: false);
                Surface.Fill(OverlayX, FieldY, Width, Height, Surface.SpriteKey);
                if (_overlay)
                    Put(_background + 1, OverlayX, FieldY, keyed: true);
                FieldVersion++;
            }
            if (enemy != _enemyKey)
            {
                _enemyKey = enemy;
                EnemyArmy = ArmyLayer(_enemySprites, cmdr, EnemyArmyX, _enemyArrival);
                EnemyLayerVersion++;
            }
            if (player != _playerKey)
            {
                _playerKey = player;
                PlayerArmy = ArmyLayer(_playerSprites, unit, 0, _playerArrival);
                PlayerLayerVersion++;
            }
            PictureVersion++;
        }
        if (fieldChanged || _mode == Mode.Engagement || _frontNumbers)
        {
            Surface.Copy(OverlayX, FieldY, Width, Height, 0, FrontY);
            _frontNumbers = _mode == Mode.Engagement;
            if (_frontNumbers)
                DrawNumbers(s, FrontY);
            FrontVersion++;
        }
        if (!Surface.Palette.SameAs(_shownPalette))
        {
            _shownPalette = Surface.Palette;
            PaletteVersion++;
        }
        Version++;
    }

    private string? _fieldKey, _enemyKey, _playerKey;
    private bool _sheetsDrawn, _frontNumbers;
    private Palette? _shownPalette;

    /// <summary>Counts the redraws of the battlefield's layer (and the overlay kept beside it).</summary>
    public int FieldVersion { get; private set; }

    /// <summary>Counts the redraws of the enemy's army's layer (<see cref="EnemyArmy"/>).</summary>
    public int EnemyLayerVersion { get; private set; }

    /// <summary>Counts the redraws of the player's army's layer (<see cref="PlayerArmy"/>).</summary>
    public int PlayerLayerVersion { get; private set; }

    /// <summary>Counts the redraws of the front (the overlay and the exchange's numbers).</summary>
    public int FrontVersion { get; private set; }

    /// <summary>Counts the palette's changes: every layer shows in the new one (a view reads them all again).</summary>
    public int PaletteVersion { get; private set; }

    /// <summary>
    /// The whole picture from the layers, as the original draws it: the picture under the numbers at
    /// (0, <see cref="PictureY"/>) — the battlefield, the enemy's army, the player's, the overlay — and with
    /// the front's numbers at (0, 0). Not kept up to date by the scene itself (no view needs it); for the tests.
    /// </summary>
    public void ComposeWhole()
    {
        Surface.Copy(0, FieldY, Width, Height, 0, BaseY);
        Surface.CopyKeyed(EnemyArmyX, ArmyY, Width, Height, 0, BaseY);
        Surface.CopyKeyed(0, ArmyY, Width, Height, 0, BaseY);
        Surface.CopyKeyed(OverlayX, FieldY, Width, Height, 0, BaseY);
        Surface.Copy(0, BaseY, Width, Height, 0, 0);
        Surface.CopyKeyed(0, FrontY, Width, Height, 0, 0);
    }

    private Army ArmyLayer(IReadOnlyList<int> sprites, int record, int x, int arrival)
    {
        Surface.Fill(x, ArmyY, Width, Height, Surface.SpriteKey);
        int left = Width, right = 0;
        foreach (int number in sprites)
        {
            if (Put(number, x, ArmyY, keyed: true) is not { } cg)
                continue;
            left = Math.Min(left, Math.Max(0, cg.X));
            right = Math.Max(right, Math.Min(Width, cg.X + cg.Width));
        }
        return right > left ? new Army(record, left, right, arrival) : new Army(0, 0, 0, arrival);
    }

    /// <summary>
    /// A CG at its own position in the layer at (x, y), clipped to the layer (<see cref="Width"/> × <see cref="Height"/>,
    /// as the picture clips it), in the palette of the moment (no palette load).
    /// </summary>
    private CgImage? Put(int number, int x, int y, bool keyed)
    {
        if (number == 0 || _cgs.Get(number) is not { } cg)
            return null;
        int sx = Math.Max(0, -cg.X), sy = Math.Max(0, -cg.Y);
        int w = Math.Min(cg.Width, Width - cg.X) - sx, h = Math.Min(cg.Height, Height - cg.Y) - sy;
        if (w > 0 && h > 0)
            Surface.DrawCgRegion(cg, sx, sy, w, h, x + cg.X + sx, y + cg.Y + sy, keyed ? Surface.SpriteKey : null);
        return cg;
    }

    /// <summary>A digit sheet at x 640, y <paramref name="y"/> (its palette is loaded with the picture's).</summary>
    private void Sheet(int number, int y)
    {
        if (_cgs.Get(number) is { } cg)
            Surface.DrawCgRegion(cg, 0, 0, cg.Width, cg.Height, 640, y);
    }

    /// <summary>Where the picture under the numbers is composed for the tests (<see cref="ComposeWhole"/>).</summary>
    private const int BaseY = 290;

    /// <summary>The pictures between exchanges: the engagement's rule (<c>FIGT.ADV:1628-1718</c>) for the two in focus, no specials.</summary>
    private void ComposeBetween(GameState s)
    {
        _background = s.Bt_BackgroundCg.Value;
        _overlay = s.Bt_EnemyParty1 != 1137;
        Sys3Int limit = s.Bt_TroopLimit;
        int Sprite(Sys3Int troops, Sys3Int full, Sys3Int partial, Sys3Int alone) =>
            (troops >= limit ? full : troops != 0 ? partial : alone).Value;
        var (unit, cmdr) = InFocus;
        if (cmdr != 0)
        {
            var c = s.Cmdrs[cmdr];
            _enemySprites = [Sprite(c.Troops, c.CgFull, c.CgPartial, c.CgAlone)];
        }
        else
        {
            _enemySprites = [];
        }
        if (unit != 0)
        {
            var u = s.Units[unit];
            _playerSprites = [Sprite(u.Troops, u.CgFull, u.CgPartial, u.CgAlone)];
        }
        else
        {
            _playerSprites = [];
        }
    }

    /// <summary>
    /// The exchange's numbers in the picture: the counters (<c>L_02c00</c>), the shots of the frame
    /// (<c>FIGTATTK.ADV:34-126</c>), the results (<c>:134-233</c>) and the marks (<c>:235-246</c>),
    /// as the presenter draws them on the stage but with the rows below 255 moved up (<see cref="Y"/>),
    /// on the picture at y <paramref name="oy"/> (the whole one, or the front layer); without <see cref="Numbers"/>
    /// the marks alone.
    /// </summary>
    private void DrawNumbers(GameState s, int oy)
    {
        if (_counters is null && _shotsStep is null && !_results && _marks is null)
            return;
        var unit = s.Units[s.Unit_Cur];
        var cmdr = s.Cmdrs[s.Cmdr_Cur];
        if (_numbers && _results)
        {
            Counters(unit.Troops != 0 ? (unit.Troops - s.Bt_ETroopVsTroopDmg - s.Bt_ECmdrVsTroopDmg).Value : null,
                cmdr.Troops != 0 ? (cmdr.Troops - s.Bt_PTroopVsTroopDmg - s.Bt_PCmdrVsTroopDmg).Value : null,
                (unit.HP - s.Bt_ETroopVsCmdrDmg - s.Bt_ECmdrVsCmdrDmg).Value,
                (cmdr.HP - s.Bt_PTroopVsCmdrDmg - s.Bt_PCmdrVsCmdrDmg).Value, AttackSheetY, oy);
            const int t = 360, d = t * 5 / 11;
            foreach (var (shots, damage, x, y) in Rows(s, t, d, damages: true))
            {
                if (shots != 0)
                    BattlePresenter.DrawNumber(Surface, damage, x, oy + Y(y), 1, AttackSheetY);
            }
        }
        else if (_numbers && _counters is { } c)
        {
            Counters(c.UnitTroops != 0 ? c.UnitTroops : null, c.CmdrTroops != 0 ? c.CmdrTroops : null, c.UnitHp, c.CmdrHp, CounterSheetY, oy);
        }
        if (_shotsStep is int step && _numbers)
        {
            int d = step * 5 / 11;
            foreach (var (shots, _, x, y) in Rows(s, step, d, damages: false))
            {
                if (shots != 0)
                    BattlePresenter.DrawNumber(Surface, shots, x, oy + Y(y), 1, AttackSheetY);
            }
        }
        if (_marks is { } m)
        {
            int troopY = _numbers ? oy : oy - OwnTroopMarkRise;         // the CGs' rows above their text are the key
            int cmdrY = oy + (_numbers ? CmdrMarkY : OwnCmdrMarkY) - 256;
            if (m.PlayerTroops)
                G(889, 0, troopY, keyed: true, own: true);
            if (m.EnemyTroops)
                G(890, 0, troopY, keyed: true, own: true);
            if (m.PlayerCmdr)
                G(891, 0, cmdrY, keyed: true, own: true);
            if (m.EnemyCmdr)
                G(892, 0, cmdrY, keyed: true, own: true);
        }
    }

    /// <summary>The TROOPS labels and numbers (a side without troops shows none) and the HP labels and numbers, moved up to <see cref="HpY"/>.</summary>
    private void Counters(int? unitTroops, int? cmdrTroops, int unitHp, int cmdrHp, int sheetY, int oy)
    {
        if (unitTroops is int ut)
        {
            Surface.CopyKeyed(990, sheetY, 100, 32, 0, oy);
            BattlePresenter.DrawNumber(Surface, ut, 100, oy, 0, sheetY);
        }
        if (cmdrTroops is int ct)
        {
            Surface.CopyKeyed(990, sheetY, 100, 32, 392, oy);
            BattlePresenter.DrawNumber(Surface, ct, 480, oy, 0, sheetY);
        }
        Surface.CopyKeyed(990, sheetY + 64, 64, 32, 0, oy + HpY);
        BattlePresenter.DrawNumber(Surface, unitHp, 0, oy + HpY, 0, sheetY);
        Surface.CopyKeyed(990, sheetY + 64, 64, 32, 480, oy + HpY);
        BattlePresenter.DrawNumber(Surface, cmdrHp, 480, oy + HpY, 0, sheetY);
    }

    /// <summary>The eight shot rows at step <paramref name="t"/>: their counts (or damages) and positions on the original's screen.</summary>
    private static IEnumerable<(int Shots, int Damage, int X, int Y)> Rows(GameState s, int t, int d, bool damages)
    {
        int px = 48 + t, ex = 416 - t;
        yield return (s.Bt_PTroopVsTroopShots.Value, s.Bt_PTroopVsTroopDmg.Value, px, 80);
        yield return (s.Bt_PTroopVsCmdrShots.Value, s.Bt_PTroopVsCmdrDmg.Value, px, damages ? 263 : 100 + d);
        yield return (s.Bt_PCmdrVsTroopShots.Value, s.Bt_PCmdrVsTroopDmg.Value, px, damages ? 117 : 280 - d);
        yield return (s.Bt_PCmdrVsCmdrShots.Value, s.Bt_PCmdrVsCmdrDmg.Value, px, 300);
        yield return (s.Bt_ETroopVsTroopShots.Value, s.Bt_ETroopVsTroopDmg.Value, ex, 80);
        yield return (s.Bt_ETroopVsCmdrShots.Value, s.Bt_ETroopVsCmdrDmg.Value, ex, damages ? 263 : 100 + d);
        yield return (s.Bt_ECmdrVsTroopShots.Value, s.Bt_ECmdrVsTroopDmg.Value, ex, damages ? 117 : 280 - d);
        yield return (s.Bt_ECmdrVsCmdrShots.Value, s.Bt_ECmdrVsCmdrDmg.Value, ex, 300);
    }

    /// <summary>
    /// A shot row's y in the picture: the troops' row (y 80) stays, the commanders' (y 300) comes
    /// halfway up and the crossing rows with them — y' = 80 + (y − 80) / 2 below row 80.
    /// </summary>
    public static int Y(int y) => y <= 80 ? y : 80 + (y - 80) / 2;

    /// <summary>A CG at (x, y), or at its own position moved by (x, y) (<paramref name="own"/>).</summary>
    private void G(int number, int x, int y, bool keyed, bool own)
    {
        if (number == 0 || _cgs.Get(number) is not { } cg)
            return;
        Surface.DrawCg(cg, own ? cg.X + x : x, own ? cg.Y + y : y, keyed ? Surface.SpriteKey : null);
    }
}
