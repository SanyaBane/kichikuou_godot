using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>
/// The armies' scene of our own battle screen (docs/ui/battle-screen.md, "The scene between
/// exchanges"): a 640×256 picture — the battlefield and two armies — that stays on top for the whole
/// battle. During an exchange it shows the engagement's pictures (<see cref="EngagementSceneEvent"/>)
/// with the exchange's numbers in the picture's geometry (what the original draws below row 255 moved
/// up); between exchanges the player unit and the enemy commander last in focus (the turn's actor and
/// its target, <see cref="Focus"/> for the card the pointer is on). Follows the battle's events with
/// the state as it is when each is shown (<see cref="Observe"/>); draws on its own <see cref="Surface"/>,
/// never on the stage. Engine-independent.
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

    private readonly CgLibrary _cgs;
    private Mode _mode;
    private int _background;
    private bool _overlay;
    private IReadOnlyList<int> _enemySprites = [];
    private IReadOnlyList<int> _playerSprites = [];
    private int _unit, _cmdr;                                   // the records in focus between exchanges
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
        Surface = new Surface(1280, 560);
    }

    /// <summary>The scene's surface: the picture at (0, 0)-(640, 256), the digit sheets right of it.</summary>
    public Surface Surface { get; }

    /// <summary>Whether the battle's field or an exchange is on: the split screen shows (not the pre-battle map, strategy or scout screens).</summary>
    public bool Active { get; private set; }

    /// <summary>Counts the scene's changes; a view uploads the picture when it moves.</summary>
    public int Version { get; private set; }

    /// <summary>The player unit and the enemy commander the picture shows between exchanges (0: none).</summary>
    public (int Unit, int Cmdr) InFocus => (_unit, _cmdr);

    /// <summary>Whether an exchange is on (from its engagement picture to the next redraw of the field).</summary>
    public bool InEngagement => _mode == Mode.Engagement;

    /// <summary>A battle begins: nothing shown until its field is (<see cref="Active"/>).</summary>
    public void Start(GameState state)
    {
        _state = state;
        Active = false;
        _mode = Mode.Between;
        _unit = _cmdr = 0;
        ClearNumbers();
    }

    /// <summary>The battle ends.</summary>
    public void Stop() => Active = false;

    /// <summary>
    /// A card the player points at (between exchanges): its army goes into the picture on its side.
    /// Ignored during an exchange.
    /// </summary>
    public void Focus(BattlePlace place)
    {
        if (_state is not { } s || _mode != Mode.Between)
            return;
        int record = BattleBoard.Record(s, place);
        if (record == 0)
            return;
        if (place.Side == BattleSide.Player ? _unit == record : _cmdr == record)
            return;
        if (place.Side == BattleSide.Player)
            _unit = record;
        else
            _cmdr = record;
        Render();
    }

    /// <summary>Follows one battle event (the state as it is when the event is shown).</summary>
    public void Observe(ScriptEvent scriptEvent, GameState state)
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
            case RoundHeaderEvent or BattlefieldBackgroundEvent:
                _mode = Mode.Between;
                KeepFocusInSlots(state);                                // a death compacts the slots between redraws
                Active = true;
                ClearNumbers();
                Render();
                break;
            case PanelHighlightEvent highlight when BattleBoard.PanelPlace(highlight.Panel.Value) is { } place:
                if (_mode == Mode.Between)
                    Focus(place);
                break;
            case EngagementSceneEvent e:
                Active = true;
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
        }
    }

    /// <summary>The records in focus, kept when they are still in a slot, else each side's first.</summary>
    private void KeepFocusInSlots(GameState s)
    {
        var players = BattleBoard.Cards(s, BattleSide.Player);
        var enemies = BattleBoard.Cards(s, BattleSide.Enemy);
        if (!players.Any(c => c?.Record == _unit))
            _unit = players.FirstOrDefault(c => c is not null)?.Record ?? 0;
        if (!enemies.Any(c => c?.Record == _cmdr))
            _cmdr = enemies.FirstOrDefault(c => c is not null)?.Record ?? 0;
    }

    private void ClearNumbers()
    {
        _counters = null;
        _shotsStep = null;
        _results = false;
        _marks = null;
        _dropCmdrMarks = false;
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>Draws the picture again from what the scene holds.</summary>
    private void Render()
    {
        if (_state is not { } s)
            return;
        if (_mode == Mode.Between)
            ComposeBetween(s);
        string picture = $"{_background} {_overlay} {string.Join(",", _enemySprites)} / {string.Join(",", _playerSprites)}";
        if (picture != _picture)
        {
            // The picture under the numbers, kept below the scene; the digit sheets load their palettes after it, as in FIGT.
            _picture = picture;
            Surface.Fill(0, BaseY, Width, Height, 0);
            G(_background, 0, BaseY, keyed: false, own: true);
            foreach (int cg in _enemySprites)
                G(cg, 0, BaseY, keyed: true, own: true);
            foreach (int cg in _playerSprites)
                G(cg, 0, BaseY, keyed: true, own: true);
            if (_overlay)
                G(_background + 1, 0, BaseY, keyed: true, own: true);
            G(888, 640, CounterSheetY, keyed: false, own: false);
            G(893, 640, AttackSheetY, keyed: false, own: false);
        }
        Surface.Copy(0, BaseY, Width, Height, 0, 0);
        if (_mode == Mode.Engagement)
            DrawNumbers(s);
        Version++;
    }

    /// <summary>Where the picture under the numbers is kept (below the scene, left of nothing).</summary>
    private const int BaseY = 290;

    private string? _picture;

    /// <summary>The pictures between exchanges: the engagement's rule (<c>FIGT.ADV:1628-1718</c>) for the two in focus, no specials.</summary>
    private void ComposeBetween(GameState s)
    {
        _background = s.Bt_BackgroundCg.Value;
        _overlay = s.Bt_EnemyParty1 != 1137;
        Sys3Int limit = s.Bt_TroopLimit;
        int Sprite(Sys3Int troops, Sys3Int full, Sys3Int partial, Sys3Int alone) =>
            (troops >= limit ? full : troops != 0 ? partial : alone).Value;
        if (_cmdr != 0)
        {
            var c = s.Cmdrs[_cmdr];
            _enemySprites = [Sprite(c.Troops, c.CgFull, c.CgPartial, c.CgAlone)];
        }
        else
        {
            _enemySprites = [];
        }
        if (_unit != 0)
        {
            var u = s.Units[_unit];
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
    /// as the presenter draws them on the stage but with the rows below 255 moved up (<see cref="Y"/>).
    /// </summary>
    private void DrawNumbers(GameState s)
    {
        if (_counters is null && _shotsStep is null && !_results && _marks is null)
            return;
        var unit = s.Units[s.Unit_Cur];
        var cmdr = s.Cmdrs[s.Cmdr_Cur];
        if (_results)
        {
            Counters(unit.Troops != 0 ? (unit.Troops - s.Bt_ETroopVsTroopDmg - s.Bt_ECmdrVsTroopDmg).Value : null,
                cmdr.Troops != 0 ? (cmdr.Troops - s.Bt_PTroopVsTroopDmg - s.Bt_PCmdrVsTroopDmg).Value : null,
                (unit.HP - s.Bt_ETroopVsCmdrDmg - s.Bt_ECmdrVsCmdrDmg).Value,
                (cmdr.HP - s.Bt_PTroopVsCmdrDmg - s.Bt_PCmdrVsCmdrDmg).Value, AttackSheetY);
            const int t = 360, d = t * 5 / 11;
            foreach (var (shots, damage, x, y) in Rows(s, t, d, damages: true))
            {
                if (shots != 0)
                    BattlePresenter.DrawNumber(Surface, damage, x, Y(y), 1, AttackSheetY);
            }
        }
        else if (_counters is { } c)
        {
            Counters(c.UnitTroops != 0 ? c.UnitTroops : null, c.CmdrTroops != 0 ? c.CmdrTroops : null, c.UnitHp, c.CmdrHp, CounterSheetY);
        }
        if (_shotsStep is int step)
        {
            int d = step * 5 / 11;
            foreach (var (shots, _, x, y) in Rows(s, step, d, damages: false))
            {
                if (shots != 0)
                    BattlePresenter.DrawNumber(Surface, shots, x, Y(y), 1, AttackSheetY);
            }
        }
        if (_marks is { } m)
        {
            if (m.PlayerTroops)
                G(889, 0, 0, keyed: true, own: true);
            if (m.EnemyTroops)
                G(890, 0, 0, keyed: true, own: true);
            if (m.PlayerCmdr)
                G(891, 0, CmdrMarkY - 256, keyed: true, own: true);
            if (m.EnemyCmdr)
                G(892, 0, CmdrMarkY - 256, keyed: true, own: true);
        }
    }

    /// <summary>The TROOPS labels and numbers (a side without troops shows none) and the HP labels and numbers, moved up to <see cref="HpY"/>.</summary>
    private void Counters(int? unitTroops, int? cmdrTroops, int unitHp, int cmdrHp, int sheetY)
    {
        if (unitTroops is int ut)
        {
            Surface.CopyKeyed(990, sheetY, 100, 32, 0, 0);
            BattlePresenter.DrawNumber(Surface, ut, 100, 0, 0, sheetY);
        }
        if (cmdrTroops is int ct)
        {
            Surface.CopyKeyed(990, sheetY, 100, 32, 392, 0);
            BattlePresenter.DrawNumber(Surface, ct, 480, 0, 0, sheetY);
        }
        Surface.CopyKeyed(990, sheetY + 64, 64, 32, 0, HpY);
        BattlePresenter.DrawNumber(Surface, unitHp, 0, HpY, 0, sheetY);
        Surface.CopyKeyed(990, sheetY + 64, 64, 32, 480, HpY);
        BattlePresenter.DrawNumber(Surface, cmdrHp, 480, HpY, 0, sheetY);
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
