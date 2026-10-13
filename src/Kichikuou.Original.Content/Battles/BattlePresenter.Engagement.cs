using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;

namespace Kichikuou.Original.Content.Battles;

// The engagement scene, the exchange animation (docs/battle/screen-layout.md §5), the strategy
// screen (§6.2) and the strategic map (§6.4).
public sealed partial class BattlePresenter
{
    // ------------------------------------------------------------------ engagement (§5.1-§5.3)

    /// <summary><c>L_02609</c> (<c>FIGT.ADV:1624-1725</c>): the scene composed at (640, 0), rows 112-511 shown.</summary>
    private void EngagementScene(EngagementSceneEvent e)
    {
        if (State?.Bt_Kind == 3)
            Surface.Fill(0, 0, 640, 400, 0); // EC 0 (:1626)
        GRel(899, 640, 256);
        GRel(e.Background.Value, 640, 0);
        foreach (var cg in e.EnemySprites)
            GRel(cg.Value, 640, 0, keyed: true);
        foreach (var cg in e.PlayerSprites)
            GRel(cg.Value, 640, 0, keyed: true);
        if (e.Overlay)
            GRel(e.Background.Value + 1, 640, 0, keyed: true);
        if (e.PlayerFace != 0)
            GAt(e.PlayerFace.Value, 640, 256, keyed: true);
        if (e.EnemyFace != 0)
            GAt(e.EnemyFace.Value, 1088, 256, keyed: true);
        Surface.Copy(640, 112, 640, 400, 0, 0);
        PicturesCovered();
    }

    /// <summary>
    /// Whether the "vs" window (window 143 with the two sides' action labels) is up, until it closes. Our screen
    /// shows the actions on the fighters' cards instead (owner, 2026-10-13): it leaves this window out of the
    /// windows' layer (<see cref="Windows"/>) and answers its click wait itself (BSC-07).
    /// </summary>
    public bool ActionLabelsOpen { get; private set; }

    /// <summary><c>FIGT.ADV:1739-1800</c>: the two names and action labels, in window 143.</summary>
    private void ActionLabels(ActionLabelsEvent e)
    {
        ActionLabelsOpen = true;
        Writer.Print(Read(1741));
        if (e.PlayerLabel is { } player)
            Writer.Print(Read(player, []));
        Writer.Print(Read(1769));
        if (e.EnemyLabel is { } enemy)
            Writer.Print(Read(enemy, []));
    }

    private int _slideOffset = -1;

    /// <summary><c>FIGT.ADV:1807-1827</c>: one step of the scene sliding up; its counters follow.</summary>
    private void Slide(int offset)
    {
        if (offset == 112)
            GRel(888, 640, 400); // the digit sheet at (640, 512)
        Surface.Copy(640, offset, 640, 400, 0, 400);
        _slideOffset = offset;
    }

    /// <summary>
    /// After a slide step: <c>L_02c00</c> into the back buffer, then shown. Otherwise the first
    /// block of <c>FIGTATTK.ADV</c> (<c>:2-31</c>): the numbers straight onto the screen.
    /// </summary>
    private void Counters(CountersEvent e)
    {
        if (_lastEvent is EngagementSlideEvent)
        {
            const int sheetY = 512;
            if (e.UnitTroops != 0)
            {
                Surface.CopyKeyed(990, sheetY, 100, 32, 0, 400);
                Number(e.UnitTroops.Value, 100, 400, 0, sheetY);
            }
            if (e.CmdrTroops != 0)
            {
                Surface.CopyKeyed(990, sheetY, 100, 32, 392, 400);
                Number(e.CmdrTroops.Value, 480, 400, 0, sheetY);
            }
            Surface.CopyKeyed(990, sheetY + 64, 64, 32, 0, 768);
            Number(e.UnitHP.Value, 0, 768, 0, sheetY);
            Surface.CopyKeyed(990, sheetY + 64, 64, 32, 480, 768);
            Number(e.CmdrHP.Value, 480, 768, 0, sheetY);
            Surface.Copy(0, 400, 640, 400, 0, 0);
            PicturesCovered();
            if (_slideOffset == 0)
                Surface.Copy(640, 0, 640, 400, 0, 400); // :1828, the clean final frame
            return;
        }

        GAt(893, 640, 400);
        if (e.UnitTroops != 0)
            Number(e.UnitTroops.Value, 100, 0, 0, AttackSheetY);
        if (e.CmdrTroops != 0)
            Number(e.CmdrTroops.Value, 480, 0, 0, AttackSheetY);
        Number(e.UnitHP.Value, 0, 368, 0, AttackSheetY);
        Number(e.CmdrHP.Value, 480, 368, 0, AttackSheetY);
    }

    /// <summary>The digit sheet of <c>FIGTATTK.ADV</c>: CG 893 at (640, 400).</summary>
    private const int AttackSheetY = 400;

    /// <summary>
    /// <c>L_04ed0</c> / <c>FIGTATTK</c> <c>L_009e4</c> (<c>FIGT.ADV:3175-3217</c>): a 5-cell
    /// number of 32×32 digits copied with key 40 from the sheet row of <paramref name="style"/>;
    /// leading zeros stay blank, the last digit is always drawn.
    /// </summary>
    private void Number(int value, int x, int y, int style, int sheetY) => DrawNumber(Surface, value, x, y, style, sheetY);

    /// <summary><see cref="Number"/> on <paramref name="surface"/>, whose digit sheet is at x 640, y <paramref name="sheetY"/>.</summary>
    internal static void DrawNumber(Kichikuou.Content.Screen.Surface surface, int value, int x, int y, int style, int sheetY)
    {
        int sum = 0;
        for (int divisor = 10000; divisor >= 1; divisor /= 10)
        {
            int digit = value / divisor;
            value -= digit * divisor;
            sum += digit;
            if (sum + digit != 0 || divisor == 1)
                surface.CopyKeyed(digit * 32 + 640, style * 64 + sheetY, 32, 32, x, y);
            x += 32;
        }
    }

    // ------------------------------------------------------------------ exchange (§5.5)

    /// <summary>One frame of the shot counters (<c>FIGTATTK.ADV:34-126</c>).</summary>
    private void ShotsFrame(int t)
    {
        if (State is not { } s)
            return;
        Surface.Copy(0, 400, 640, 400, 640, 0);
        int d = t * 5 / 11;
        int px = 48 + t, ex = 416 - t;
        var shots = new (Sys3Int Shots, int X, int Y, int RectX, int RectY)[]
        {
            (s.Bt_PTroopVsTroopShots, px, 80, px - 40, 80),
            (s.Bt_PTroopVsCmdrShots, px, 100 + d, px - 40, 80 + d),
            (s.Bt_PCmdrVsTroopShots, px, 280 - d, px - 40, 280 - d),
            (s.Bt_PCmdrVsCmdrShots, px, 300, px - 40, 300),
            (s.Bt_ETroopVsTroopShots, ex, 80, ex, 80),
            (s.Bt_ETroopVsCmdrShots, ex, 100 + d, ex, 80 + d),
            (s.Bt_ECmdrVsTroopShots, ex, 280 - d, ex, 280 - d),
            (s.Bt_ECmdrVsCmdrShots, ex, 300, ex, 300),
        };
        foreach (var shot in shots)
        {
            if (shot.Shots != 0)
                Number(shot.Shots.Value, 640 + shot.X, shot.Y, 1, AttackSheetY);
        }
        foreach (var shot in shots)
        {
            if (shot.Shots != 0)
                Surface.Copy(640 + shot.RectX, shot.RectY, 200, 64, shot.RectX, shot.RectY);
        }
    }

    /// <summary><c>FIGTATTK.ADV:134-233</c>: predicted values and damages, shown at once.</summary>
    private void ExchangeResults()
    {
        if (State is not { } s)
            return;
        var unit = s.Units[s.Unit_Cur];
        var cmdr = s.Cmdrs[s.Cmdr_Cur];
        Surface.Copy(0, 400, 640, 400, 640, 0);
        if (unit.Troops != 0)
        {
            Surface.CopyKeyed(990, AttackSheetY, 100, 32, 640, 0);
            Number((unit.Troops - s.Bt_ETroopVsTroopDmg - s.Bt_ECmdrVsTroopDmg).Value, 740, 0, 0, AttackSheetY);
        }
        if (cmdr.Troops != 0)
        {
            Surface.CopyKeyed(990, AttackSheetY, 100, 32, 392 + 640, 0);
            Number((cmdr.Troops - s.Bt_PTroopVsTroopDmg - s.Bt_PCmdrVsTroopDmg).Value, 1120, 0, 0, AttackSheetY);
        }
        Surface.CopyKeyed(990, AttackSheetY + 64, 64, 32, 640, 368);
        Number((unit.HP - s.Bt_ETroopVsCmdrDmg - s.Bt_ECmdrVsCmdrDmg).Value, 640, 368, 0, AttackSheetY);
        Surface.CopyKeyed(990, AttackSheetY + 64, 64, 32, 1120, 368);
        Number((cmdr.HP - s.Bt_PTroopVsCmdrDmg - s.Bt_PCmdrVsCmdrDmg).Value, 1120, 368, 0, AttackSheetY);

        const int t = 360, d = t * 5 / 11;
        var damages = new (Sys3Int Shots, Sys3Int Damage, int X, int Y)[]
        {
            (s.Bt_PTroopVsTroopShots, s.Bt_PTroopVsTroopDmg, 48 + t, 80),
            (s.Bt_PTroopVsCmdrShots, s.Bt_PTroopVsCmdrDmg, 48 + t, 100 + d),
            (s.Bt_PCmdrVsTroopShots, s.Bt_PCmdrVsTroopDmg, 48 + t, 280 - d),
            (s.Bt_PCmdrVsCmdrShots, s.Bt_PCmdrVsCmdrDmg, 48 + t, 300),
            (s.Bt_ETroopVsTroopShots, s.Bt_ETroopVsTroopDmg, 416 - t, 80),
            (s.Bt_ETroopVsCmdrShots, s.Bt_ETroopVsCmdrDmg, 416 - t, 100 + d),
            (s.Bt_ECmdrVsTroopShots, s.Bt_ECmdrVsTroopDmg, 416 - t, 280 - d),
            (s.Bt_ECmdrVsCmdrShots, s.Bt_ECmdrVsCmdrDmg, 416 - t, 300),
        };
        foreach (var damage in damages)
        {
            if (damage.Shots != 0)
                Number(damage.Damage.Value, 640 + damage.X, damage.Y, 1, AttackSheetY);
        }
        Surface.Copy(640, 0, 640, 400, 0, 0);
        PicturesCovered();
    }

    private WipedOutMarksEvent? _marks;

    /// <summary><c>FIGTATTK.ADV:235-246</c>: CG 889-892 at their own positions.</summary>
    private void WipedOutMarks(WipedOutMarksEvent e)
    {
        _marks = e;
        if (e.PlayerTroops)
            G(889, keyed: true);
        if (e.EnemyTroops)
            G(890, keyed: true);
        if (e.PlayerCmdr)
            G(891, keyed: true);
        if (e.EnemyCmdr)
            G(892, keyed: true);
    }

    /// <summary><c>IK 0</c> (<c>:247</c>), then the commander marks are removed (<c>:248-253</c>).</summary>
    private void ExchangeKeyWait()
    {
        WaitsForKey = true;
        var marks = _marks;
        _marks = null;
        _afterKey = () =>
        {
            if (marks?.PlayerCmdr == true)
                Surface.Copy(640, 300, 320, 100, 0, 300);
            if (marks?.EnemyCmdr == true)
                Surface.Copy(960, 300, 320, 100, 320, 300);
        };
    }

    // ------------------------------------------------------------------ strategy screen (§6.2)

    /// <summary><c>FIGT作.ADV:15-33</c> (side 1) / <c>:357-375</c> (side 2).</summary>
    private void StrategyScreen(int side)
    {
        if (State is not { } s)
            return;
        Writer.Color = 22; // FIGT.ADV:35, battle setup; nothing has been redrawn yet
        GRel(899, 0, 400);
        GRel(side == 1 ? s.Bt_BackgroundCg.Value : 900, 0, 400);
        Sys3Int troops, full, partial, alone, face;
        if (side == 1)
        {
            var unit = s.Units[s.Unit_Cur];
            (troops, full, partial, alone, face) = (unit.Troops, unit.CgFull, unit.CgPartial, unit.CgAlone, unit.FaceCg);
        }
        else
        {
            var cmdr = s.Cmdrs[s.Cmdr_Cur];
            (troops, full, partial, alone, face) = (cmdr.Troops, cmdr.CgFull, cmdr.CgPartial, cmdr.CgAlone, cmdr.FaceCg);
        }
        if (troops >= s.Bt_TroopLimit)
            GRel(full.Value, 0, 400, keyed: true);
        if (troops < s.Bt_TroopLimit && troops != 0)
            GRel(partial.Value, 0, 400, keyed: true);
        if (troops == 0)
            GRel(alone.Value, 0, 400, keyed: true);
        GAt(face.Value, side == 1 ? 0 : 448, 544, keyed: true);
        Surface.Copy(0, 400, 640, 400, 0, 0);
        PicturesCovered();
    }

    // ------------------------------------------------------------------ strategic map (§6.4)

    private enum MapAnimation { None, Scroll, Blink }

    private MapAnimation _mapAnimation;
    private int _blinkStep;

    private void Map(int mode)
    {
        switch (mode)
        {
            case 0 or 12:
                if (mode == 0 && State is { } s && _cgs.Get(s.Ui_MapCgTopL.Value) is { } top)
                    Surface.Palette = top.PaletteOver(Surface.Palette); // PC 6: G Ui_MapCgTopL
                BlitMapView();
                if (mode == 0)
                    _portraitL = _portraitR = _centerCg = 0;
                break;
            case 1:
                _mapAnimation = MapAnimation.Scroll;
                break;
            case 3:
                DrawMapTiles();
                break;
            case 10 or 13:
                GAt(814, 640, 0);
                _mapAnimation = MapAnimation.Blink;
                _blinkStep = 0;
                break;
            case 11:
                Surface.Fill(0, 0, 640, 400, 0);
                PicturesCovered();
                break;
        }
    }

    /// <summary>The pauses of a map animation: a scroll redraws at the new camera, a blink alternates the mark.</summary>
    private void MapPause()
    {
        if (State is not { } s)
            return;
        switch (_mapAnimation)
        {
            case MapAnimation.Scroll:
                BlitMapView();
                break;
            case MapAnimation.Blink:
                // M.ADV:172-175: CS the mark, Y 3, 8, CC the map back, Y 3, 8.
                int cx = s.Tmp0021.Value, cy = s.Tmp0022.Value, mx = s.Tmp0025.Value, my = s.Tmp0026.Value;
                if (_blinkStep++ % 2 == 0)
                    Surface.CopyKeyed(640, 0, 32, 32, mx, my);
                else
                    Surface.Copy(mx + cx + 1280 - 320, my + cy - 200, 32, 32, mx, my);
                break;
        }
    }

    /// <summary><c>M.ADV:3-10</c>: the view's top-left corner on the map.</summary>
    internal (int X, int Y) MapViewCorner()
    {
        if (State is not { } s)
            return (0, 0);
        Sys3Int x = s.MapCamX - 320, y = s.MapCamY - 200;
        return (Math.Min((int)x.Value, 1280), Math.Min((int)y.Value, 400));
    }

    /// <summary><c>WAO_MAP.ADV</c> op 0: the map view onto the screen.</summary>
    internal void BlitMapView()
    {
        var (x, y) = MapViewCorner();
        Surface.Copy(x + 1280, y, 640, 400, 0, 0);
        PicturesCovered();
    }

    /// <summary><c>WAO_MAP.ADV</c> op 3 (<c>:86-115</c>): the six tiles and every marker, off screen.</summary>
    internal void DrawMapTiles()
    {
        if (State is not { } s)
            return;
        GAt(s.Ui_MapCgTopL.Value, 1280, 0);
        GAt(s.Ui_MapCgTopM.Value, 1920, 0);
        GAt(s.Ui_MapCgTopR.Value, 2560, 0);
        GAt(s.Ui_MapCgBotL.Value, 1280, 400);
        GAt(s.Ui_MapCgBotM.Value, 1920, 400);
        GAt(s.Ui_MapCgBotR.Value, 2560, 400);
        for (int i = 0; i < s.Ui_MarkCount.Value; i++)
        {
            var mark = s.Marks[(Sys3Int)i];
            int x = mark.X.Value + 1280, y = mark.Y.Value, cg = mark.Cg.Value;
            if (cg > 10000)
            {
                y -= 18;
                cg -= 10000;
            }
            GAt(cg, x, y, keyed: true);
        }
    }
}
