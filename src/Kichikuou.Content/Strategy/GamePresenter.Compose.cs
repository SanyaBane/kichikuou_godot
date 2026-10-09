using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.State;

namespace Kichikuou.Content.Strategy;

// The map on the stage and the screens drawn by hand over it.
public sealed partial class GamePresenter
{
    /// <summary>The left edge of the map's tiles off screen (<c>WAO_MAP.ADV:86-115</c>).</summary>
    private const int TileArea = 1280;

    /// <summary>What the off-screen tile area holds.</summary>
    private enum Tiles
    {
        /// <summary>The map as GameState has it.</summary>
        Valid,

        /// <summary>The map before a change of tiles or markers: drawn again before it is shown.</summary>
        Dirty,

        /// <summary>Something a scene drew there (the opening): copied as it is, drawn again before the map is shown.</summary>
        Foreign,
    }

    private enum MapAnimation { None, Scroll, Blink }

    private Tiles _tiles = Tiles.Dirty;
    private MapAnimation _mapAnimation;
    private int _blinkStep;
    private IReadOnlyList<string>? _menu;
    private (int X, int Y) _pointer;

    internal CgLibrary Cgs => _cgs;

    internal IGlyphRenderer Glyphs => _glyphs;

    /// <summary>The text of <c>MS n, "text"</c> at <paramref name="at"/> in the player's files, or null.</summary>
    internal string? Literal(ScriptRef at) =>
        (_scenario?.ReadStatement(at) as SetStringStatement)?.Text is { } text ? Hankaku.ToHalfWidth(text) : null;

    /// <summary>The strings of the table <c>#label, 1:</c> at <paramref name="at"/> in the player's files (empty without them).</summary>
    internal IReadOnlyList<string> StringTable(ScriptRef at) =>
        (_scenario?.ReadStatement(at) as SetStringTableStatement)?.Strings ?? [];

    // ------------------------------------------------------------------ the map

    /// <summary>
    /// A call of <c>M.ADV</c> (<see cref="MapEvent"/>): 0 and 12 show the map at the camera (0 with
    /// the map's palette), 1 starts a scroll and 10/13 a blink, whose steps are the pauses that
    /// follow; 3 and 4 change markers (drawn again before the next view); 11 blacks the screen.
    /// </summary>
    private void Map(int mode)
    {
        switch (mode)
        {
            case 0 or 12:
                if (mode == 0 && State is { } s && _cgs.Get(s.Ui_MapCgTopL.Value) is { } top)
                    Stage.Palette = top.PaletteOver(Stage.Palette);
                ShowMap();
                break;
            case 1:
                _mapAnimation = MapAnimation.Scroll;
                break;
            case 3 or 4:
                _tiles = Tiles.Dirty;
                break;
            case 10 or 13:
                if (_cgs.Get(814) is { } mark)
                    Stage.DrawCg(mark, 640, 0);
                _mapAnimation = MapAnimation.Blink;
                _blinkStep = 0;
                break;
            case 11:
                Stage.Fill(0, 0, Surface.ScreenWidth, Surface.ScreenHeight, 0);
                break;
        }
    }

    /// <summary>The pauses of a map animation: a scroll shows the map at the new camera, a blink alternates the mark (<c>M.ADV:172-175</c>).</summary>
    private void MapPause()
    {
        if (State is not { } s)
            return;
        switch (_mapAnimation)
        {
            case MapAnimation.Scroll:
                ShowMap();
                break;
            case MapAnimation.Blink:
                int cx = s.Tmp0021.Value, cy = s.Tmp0022.Value, mx = s.Tmp0025.Value, my = s.Tmp0026.Value;
                if (_blinkStep++ % 2 == 0)
                {
                    Stage.CopyKeyed(640, 0, 32, 32, mx, my);
                }
                else
                {
                    EnsureTiles();
                    Stage.Copy(mx + cx + TileArea - 320, my + cy - 200, 32, 32, mx, my);
                }
                break;
        }
    }

    /// <summary>The map's view at the camera onto the screen, its tiles drawn first when they are not the map's.</summary>
    private void ShowMap()
    {
        EnsureTiles();
        Battle.BlitMapView();
    }

    /// <summary>The six tiles and every marker off screen, from GameState (<c>WAO_MAP.ADV</c> op 3), unless they are there.</summary>
    private void EnsureTiles()
    {
        if (_tiles == Tiles.Valid || State is null)
            return;
        Battle.DrawMapTiles();
        _tiles = Tiles.Valid;
    }

    /// <summary>A copy is about to read the tile area: tiles the map changed since are drawn first (a scene's drawing there is copied as it is).</summary>
    private void PrepareCopy(int sx, int w)
    {
        if (sx + w > TileArea && _tiles == Tiles.Dirty)
            EnsureTiles();
    }

    /// <summary>A drawing command reached the tile area: what is there is no longer the map.</summary>
    private void Drawn(int x, int y, int w, int h)
    {
        if (x + w > TileArea && w > 0 && h > 0)
            _tiles = Tiles.Foreign;
    }

    // ------------------------------------------------------------------ menus and the pointer

    /// <summary>
    /// <c>]</c>: shows <paramref name="items"/> in the selection window last chosen (<c>B1</c>/<c>B2</c>),
    /// until <see cref="CloseMenu"/>.
    /// </summary>
    public void OpenMenu(IReadOnlyList<SceneMenuItem> items)
    {
        _menu = items.Select(item => string.Concat(item.Texts.Select(Read)).Replace("\n", "")).ToArray();
        Invalidate();
    }

    /// <summary>The menu's item texts while it is open.</summary>
    public IReadOnlyList<string>? MenuItems => _menu;

    /// <summary>The menu answered: it disappears.</summary>
    public void CloseMenu()
    {
        _menu = null;
        Invalidate();
    }

    /// <summary>The menu item (0-based) at a screen point, or −1.</summary>
    public int MenuHitTest(int x, int y) => MenuBox()?.HitTest(x, y) ?? -1;

    /// <summary>The pointer moved (screen point): the menu's highlight follows it.</summary>
    public void Pointer(int x, int y)
    {
        if (_pointer == (x, y))
            return;
        _pointer = (x, y);
        if (_menu is not null)
            Invalidate();
    }

    private SelectionBox? MenuBox()
    {
        if (_menu is null)
            return null;
        var (x, y, w) = _selectionWindows.TryGetValue(_selectionWindow, out var window) ? window : _selectionWindows[1];
        return new SelectionBox(Screen, _glyphs, x, y, w, _menu);
    }

    // ------------------------------------------------------------------ composing

    /// <summary>
    /// Composes <see cref="Screen"/> when anything changed: the stage's screen, then (outside a
    /// battle) the pictures, the open picker, the window's frame and text and the menu, then the
    /// fade. Returns whether the screen was drawn again.
    /// </summary>
    public bool Compose()
    {
        if (!_dirty && Stage.ScreenVersion == _stageVersion)
            return false;
        _dirty = false;
        _stageVersion = Stage.ScreenVersion;
        Screen.Palette = Stage.Palette;
        Screen.CopyFrom(Stage, 0, 0, Surface.ScreenWidth, Surface.ScreenHeight, 0, 0);
        if (!InBattle && State is { } s)
        {
            Pictures(s);
            Picker?.Draw(Screen, s);
            Frame(s);
            _window.Draw(Screen, _glyphs);
            if (MenuBox() is { } menu)
            {
                menu.Open();
                menu.Hover(_pointer.X, _pointer.Y);
            }
        }
        Screen.FadeScreen(FadeAmount, FadeToWhite);
        return true;
    }

    /// <summary><c>WAO_SUB</c>'s pictures from GameState: the portraits at (0, 0) and (448, 144), the centre picture at (176, 96) with its overlays.</summary>
    private void Pictures(GameState s)
    {
        Picture(s.Ui_PortraitL.Value, 0, 0, keyed: true);
        Picture(s.Ui_PortraitR.Value, 448, 144, keyed: true);
        if (s.Ui_CenterCg == 0)
            return;
        Picture(s.Ui_CenterCg.Value, 176, 96, keyed: false);
        Picture(s.Ui_CenterOverlay1.Value, 176, 96, keyed: true);
        Picture(s.Ui_CenterOverlay2.Value, 176, 96, keyed: true);
        Picture(s.Ui_CenterOverlay3.Value, 176, 96, keyed: true);
        Picture(s.Ui_CenterOverlay4.Value, 176, 96, keyed: true);
    }

    /// <summary>The open window's frame (<c>Ui_WinFrameCg</c>) at its own position; the full-screen window's is opaque under the keyed one.</summary>
    private void Frame(GameState s)
    {
        if (s.Ui_WinType == 0 || _cgs.Get(s.Ui_WinFrameCg.Value) is not { } frame)
            return;
        if (s.Ui_WinType.Value is >= 31 and <= 39)
            Screen.DrawCg(frame, frame.X, frame.Y);
        Screen.DrawCg(frame, frame.X, frame.Y, Surface.SpriteKey);
    }

    private void Picture(int number, int x, int y, bool keyed)
    {
        if (number != 0 && _cgs.Get(number) is { } cg)
            Screen.DrawCg(cg, x, y, keyed ? Surface.SpriteKey : null);
    }
}
