using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

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

    /// <summary><c>WAO_SUB</c>'s pictures and the window's frame (<see cref="Cover"/>).</summary>
    [Flags]
    private enum Pictures { None = 0, PortraitL = 1, PortraitR = 2, Center = 4, Frame = 8 }

    private static readonly (int X, int Y) PortraitLAt = (0, 0);
    private static readonly (int X, int Y) PortraitRAt = (448, 144);
    private static readonly (int X, int Y) CenterAt = (176, 96);

    private Tiles _tiles = Tiles.Dirty;
    private Pictures _covered;
    private MapAnimation _mapAnimation;
    private int _blinkStep;
    private (int X, int Y) _pointer;

    internal CgLibrary Cgs => _cgs;

    internal IGlyphRenderer Glyphs => _glyphs;

    /// <summary>The text of <c>MS n, "text"</c> at <paramref name="at"/> in the player's files, or null.</summary>
    internal string? Literal(ScriptRef at) =>
        (_scenario?.ReadStatement(at) as SetStringStatement)?.Text is { } text ? Hankaku.ToHalfWidth(text) : null;

    /// <summary>Our map's markers as the state has them now (<see cref="Strategy.MapMarkers.All"/>), the dungeons named from the player's files.</summary>
    public IReadOnlyList<MapMarker> MapMarkers() => State is { } s ? Strategy.MapMarkers.All(s, Literal) : [];

    /// <summary>The strings of the table <c>#label, 1:</c> at <paramref name="at"/> in the player's files (empty without them).</summary>
    internal IReadOnlyList<string> StringTable(ScriptRef at) =>
        (_scenario?.ReadStatement(at) as SetStringTableStatement)?.Strings ?? [];

    // ------------------------------------------------------------------ the map

    /// <summary>
    /// A call of <c>M.ADV</c> (<see cref="MapEvent"/>): 0 and 12 show the map at the camera (0 with
    /// the map's palette) over the whole screen — a screen drawn by hand that its file left open is gone (<c>Cﾌﾞｶ</c>
    /// returns from "End" without closing <c>Sﾌﾞｶ</c>, and <c>COMMAND.ADV:358-359</c> shows the map), 1 starts a scroll and 10/13 a blink, whose steps are the pauses that
    /// follow; 3 and 4 change markers (drawn again before the next view); 11 blacks the screen.
    /// </summary>
    private void Map(int mode)
    {
        switch (mode)
        {
            case 0 or 12:
                if (DrawnByHand)
                {
                    Picker = null;
                    Data = DataView.Closed;
                    _target = TextTarget.Window;
                }
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
                CoverScreen();
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
        MapViewShown = Battle.MapViewCorner();
        CoverScreen();
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

    /// <summary>
    /// A drawing command reached the tile area: what is there is no longer the map; on screen it
    /// may draw over <c>WAO_SUB</c>'s pictures, and an <paramref name="opaque"/> one (<c>CC</c>,
    /// <c>CF</c>, <c>CM</c>, <c>G</c> without a key) over the window's frame and text (<see cref="Cover"/>).
    /// </summary>
    private void Drawn(int x, int y, int w, int h, bool opaque = false)
    {
        if (x + w > TileArea && w > 0 && h > 0)
            _tiles = Tiles.Foreign;
        Cover(x, y, w, h, opaque);
    }

    /// <summary>
    /// The engine draws <c>WAO_SUB</c>'s pictures and the window's frame and text into the screen
    /// once, so a drawing over the whole of one (the map's view during an <c>M.ADV</c> scroll,
    /// <c>WAO_MAP.ADV:2</c>) wipes it while its variable still holds it; it stays hidden until
    /// <c>WAO_SUB</c> draws or removes it again. The frame and the text are wiped only by an
    /// <paramref name="opaque"/> drawing command — <c>OVER</c>'s ending picture over the window it
    /// has just opened (<c>OVER.ADV:29-34</c>), the frame until a window is opened or closed; its
    /// report's text area restored after each entry (<c>:2208</c>), the characters under it.
    /// </summary>
    private void Cover(int x, int y, int w, int h, bool opaque = false)
    {
        if (State is not { } s || InBattle)
            return;
        if (opaque)
            _window.Wipe(x, y, w, h);
        bool Covers((int X, int Y)? at, int number) =>
            number != 0 && _cgs.Get(number) is { } cg && (at ?? (cg.X, cg.Y)) is var (ax, ay)
            && x <= ax && y <= ay && x + w >= ax + cg.Width && y + h >= ay + cg.Height;
        if (Covers(PortraitLAt, s.Ui_PortraitL.Value))
            _covered |= Pictures.PortraitL;
        if (Covers(PortraitRAt, s.Ui_PortraitR.Value))
            _covered |= Pictures.PortraitR;
        if (Covers(CenterAt, s.Ui_CenterCg.Value))
            _covered |= Pictures.Center;
        if (opaque && s.Ui_WinType != 0 && Covers(null, s.Ui_WinFrameCg.Value))
            _covered |= Pictures.Frame;
    }

    /// <summary>The whole screen was drawn over.</summary>
    private void CoverScreen() => Cover(0, 0, Surface.ScreenWidth, Surface.ScreenHeight);

    // ------------------------------------------------------------------ menus and the pointer

    /// <summary>
    /// <c>]</c>: shows <paramref name="items"/> in the selection window last chosen (<c>B1</c>/<c>B2</c>),
    /// until <see cref="CloseMenu"/>.
    /// </summary>
    public void OpenMenu(IReadOnlyList<SceneMenuItem> items)
    {
        _menuItems = items;
        SetLeadIn(false);                                               // the UI covers a city's first menu again
        Menu = MenuView.FromTexts(items.Select(item => string.Concat(item.Texts.Select(Read)).Replace("\n", "")),
            _engine.SelectionWindow);
        Invalidate();
    }

    /// <summary>The open menu as data (<see cref="MenuView"/>), or null.</summary>
    public MenuView? Menu { get; private set; }

    /// <summary>The menu's item texts as printed (mark and text) while it is open.</summary>
    public IReadOnlyList<string>? MenuItems => Menu?.Items.Select(item => item.Shown).ToArray();

    /// <summary>The menu answered: it disappears.</summary>
    public void CloseMenu()
    {
        Menu = null;
        _menuItems = null;
        if (_provinceCovered)
            _cityLeadIn = true;                                         // the files' way out: IZ (frames pass), then 0 and 332
        MenuCovered = false;
        Invalidate();
    }

    private IReadOnlyList<SceneMenuItem>? _menuItems;

    /// <summary>The open menu as a city's first menu (<see cref="Strategy.ProvinceMenu"/>), or null.</summary>
    public ProvinceMenu? ProvinceMenu =>
        _menuItems is { } items && MenuItems is { } texts && State is { } s && !BattleShows
            ? Strategy.ProvinceMenu.For(s, items, texts, ReadFixedText)
            : null;

    /// <summary>The open menu as <c>COMMAND.ADV</c>'s command menu (<see cref="Strategy.CommandMenu"/>), or null.</summary>
    public CommandMenu? CommandMenu =>
        _menuItems is { } items && MenuItems is { } texts && State is not null && !BattleShows
            ? Strategy.CommandMenu.For(items, texts)
            : null;

    /// <summary>The status strip for the state now (<see cref="Strategy.StatusStrip"/>), or null.</summary>
    public StatusStrip? StatusStrip => State is { } s ? Strategy.StatusStrip.For(s, (at, numbers) => Read(at, numbers, null)) : null;

    /// <summary>A text run with zeros for its numbers and computed strings (for its fixed lines), or null when it does not resolve.</summary>
    private string? ReadFixedText(ScriptRef at)
    {
        try
        {
            var run = _text.Read(at);
            return run.Format(new Sys3Int[run.NumberCount], StringVar, new Sys3Int[run.ComputedStringCount]);
        }
        catch (Exception e) when (e is ScenarioTextException or ArgumentException or IOException)
        {
            Unresolved.Add($"{at}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Our province menu covers the city's (docs/ui/command-screen.md, "The province menu"): while the menu
    /// is open, the city's picture (<c>WAO_SUB</c> 331), the window and its text and the menu are not drawn,
    /// only the map under them. <see cref="CloseMenu"/> ends it. Also true through a city's lead-in and
    /// lead-out (<see cref="CoverCityLeadIn"/>).
    /// </summary>
    public bool MenuCovered
    {
        get => _provinceCovered || _cityLeadIn;
        set
        {
            if (_provinceCovered != value)
                Invalidate();
            _provinceCovered = value;
        }
    }

    private bool _provinceCovered;

    /// <summary>
    /// Whether a UI covers the city files' first menus (<see cref="MenuCovered"/>): then the
    /// files' lead-in is not drawn either — the city's picture (<c>WAO_SUB</c> 331 of <c>Prov.BgCg</c>),
    /// the window 113 and its "[name]" lines, which the files draw before the release wait (<c>IM</c>,
    /// frames pass) that comes before the menu. It ends at the menu (<see cref="OpenMenu"/>: the UI covers
    /// it again if it is a city's first menu) or at anything else the files would show (<see cref="LeadInGoesOn"/>).
    /// Nor the lead-out: after our menu closes, the files' way back to the map (a cancel puts the pointer back
    /// with <c>IZ</c> first, frames pass) until the window and the picture are gone, or a command shows more.
    /// </summary>
    public bool CoverCityLeadIn { get; set; }

    private bool _cityLeadIn;

    /// <summary>A city's lead-in starts or ends with this event (before it is shown).</summary>
    private void ObserveLeadIn(ScriptEvent scriptEvent)
    {
        if (scriptEvent is WindowEvent { Op.Value: 331 } picture && CoverCityLeadIn && State is { } s
            && s.Prov_Cur.Value >= 1 && s.Prov_Cur.Value < s.Provs.Count && picture.CallArg == s.Provs[s.Prov_Cur].BgCg
            && s.Ev_CommandMode.Value is not (1 or 2) && Picker is null && Data == DataView.Closed)
            SetLeadIn(true);                                            // Cｼﾊｲﾄｼ/Cﾃｷﾄｼ/Cﾃｷﾄｼ2: the city's picture
        else if (_cityLeadIn && !LeadInGoesOn(scriptEvent))
            SetLeadIn(false);
    }

    /// <summary>
    /// What the city files show between the picture and the menu — the window 113 and its text — and on the
    /// way out after it: the text cleared, the pointer put back, the window closed (0, 10), the picture removed (332).
    /// </summary>
    private static bool LeadInGoesOn(ScriptEvent scriptEvent) => scriptEvent is WindowEvent { Op.Value: 113 or 0 or 10 or 332 }
        or MessageEvent or ValueMessageEvent or PrintStringEvent or LineBreakEvent or MessageClearEvent
        or EngineEvent { Command: "IZ" } or SoundEvent or SoundPairEvent or MusicEvent;

    /// <summary>The script waits for a key or a pause: whatever it drew is shown.</summary>
    public void EndCityLeadIn() => SetLeadIn(false);

    private void SetLeadIn(bool on)
    {
        if (_cityLeadIn != on)
            Invalidate();
        _cityLeadIn = on;
    }

    /// <summary>The menu item (0-based) at a screen point, or −1.</summary>
    public int MenuHitTest(int x, int y) => MenuBox()?.HitTest(x, y) ?? -1;

    /// <summary>The pointer moved (screen point): the menu's highlight follows it.</summary>
    public void Pointer(int x, int y)
    {
        if (_pointer == (x, y))
            return;
        _pointer = (x, y);
        if (Menu is not null)
            Invalidate();
    }

    private SelectionBox? MenuBox()
    {
        if (Menu is not { } menu)
            return null;
        var (x, y, w) = menu.Window ?? throw new InvalidOperationException("No selection window is set (B1).");
        return new SelectionBox(Screen, _glyphs, x, y, w, [.. menu.Items.Select(item => item.Shown)]);
    }

    // ------------------------------------------------------------------ composing

    /// <summary>
    /// Whether anything changed since the last call or <see cref="Compose"/>, without composing: a view
    /// that does not show <see cref="Screen"/> now (our battle screen with no window open) asks this
    /// instead; the next <see cref="Compose"/> draws the change.
    /// </summary>
    public bool TakeChange()
    {
        if (!_dirty && Stage.ScreenVersion == _stageVersion)
            return false;
        _dirty = false;
        _stageVersion = Stage.ScreenVersion;
        _composeOwed = true;
        return true;
    }

    /// <summary>A change <see cref="TakeChange"/> took that <see cref="Screen"/> does not show yet.</summary>
    private bool _composeOwed;

    /// <summary>
    /// Composes <see cref="Screen"/> when anything changed: the stage's screen, then (outside a
    /// battle, or over a picker the battle opened) the pictures, the open picker, the window's frame
    /// and text — or All Data's screen instead —, the menu (in a battle too), then the fade. Returns whether the
    /// screen was drawn again.
    /// </summary>
    public bool Compose()
    {
        if (!_dirty && Stage.ScreenVersion == _stageVersion && !_composeOwed)
            return false;
        _dirty = false;
        _composeOwed = false;
        _stageVersion = Stage.ScreenVersion;
        Screen.Palette = Stage.Palette;
        Screen.CopyFrom(Stage, Stage.View.X, Stage.View.Y, Surface.ScreenWidth, Surface.ScreenHeight, 0, 0);
        if (BattleShows && MaskEventCgs && State is { Ui_WinType.Value: 31 })
            MaskStage();                                                // a scene the battle called; its window is on the stage too
        if (!BattleShows && State is { } s)
        {
            if (Data != DataView.Closed)
            {
                DrawData(s);                                            // over the whole screen (EC 0): no window, no pictures
            }
            else
            {
                if (MaskEventCgs && (s.Ui_WinType == 31 || _endingCg != 0 || _gallery))
                    MaskStage();
                if (!MenuCovered)
                {
                    DrawPictures(s);
                    Picker?.Draw(Screen, s);
                    Frame(s);
                    _window.Draw(Screen, _glyphs);
                }
            }
        }
        if (State is not null && !MenuCovered && MenuBox() is { } menu)                 // also over a battle: a scene it called (Gﾒﾙﾌｪｲｽ from 死亡捕獲)
        {
            menu.Open();
            menu.Hover(_pointer.X, _pointer.Y);
        }
        Screen.FadeScreen(FadeAmount, FadeToWhite);
        return true;
    }

    /// <summary>
    /// <c>WAO_SUB</c> 0 on the full-screen window (types 31-39) restores nothing: its
    /// <c>CC 0, 0, 640, 400, 0, 0</c> copies the screen onto itself (<c>WAO_SUB.ADV:267-270</c>), so
    /// the frame, the text and the pictures stay on the screen until something draws over them
    /// (the last line of <c>Iﾄｳｿﾞｸ.ADV</c>'s escape, <c>:1839-1843</c>, while <c>ｵｰﾌﾟﾆﾝｸﾞ</c> waits for
    /// its music). The engine drew them into the screen; here the screen as shown (without the fade)
    /// goes onto the stage under the frame, in its colours.
    /// </summary>
    private void LeaveWindowOnStage(GameState s)
    {
        if (_cgs.Get(s.Ui_WinFrameCg.Value) is not { } frame)
            return;
        byte fade = FadeAmount;
        FadeAmount = 0;
        Invalidate();
        Compose();
        FadeAmount = fade;
        Invalidate();
        Stage.CopyFrom(Screen, frame.X, frame.Y, frame.Width, frame.Height, Stage.View.X + frame.X, Stage.View.Y + frame.Y);
    }

    /// <summary>The debug placeholder of <see cref="MaskEventCgs"/>: the stage's picture greyed out, with the CG's number.</summary>
    private void MaskStage()
    {
        Screen.FillColor(0, 0, Surface.ScreenWidth, Surface.ScreenHeight, (96, 96, 96));
        MessageWriter.DrawString(Screen, _glyphs, $"CG {(_endingCg != 0 ? _endingCg : _lastCg)} (masked)", 16, 16, 24, (255, 255, 255));
    }

    /// <summary>
    /// <c>WAO_SUB</c>'s pictures from GameState: the portraits at (0, 0) and (448, 144), the centre
    /// picture at (176, 96) with its overlays — those not drawn over since (<see cref="Cover"/>).
    /// </summary>
    private void DrawPictures(GameState s)
    {
        if (s.Ui_PortraitL == 0)
            _covered &= ~Pictures.PortraitL;
        if (s.Ui_PortraitR == 0)
            _covered &= ~Pictures.PortraitR;
        if (s.Ui_CenterCg == 0)
            _covered &= ~Pictures.Center;
        if ((_covered & Pictures.PortraitL) == 0)
            Picture(s.Ui_PortraitL.Value, PortraitLAt, keyed: true);
        if ((_covered & Pictures.PortraitR) == 0)
            Picture(s.Ui_PortraitR.Value, PortraitRAt, keyed: true);
        if (s.Ui_CenterCg == 0 || (_covered & Pictures.Center) != 0)
            return;
        Picture(s.Ui_CenterCg.Value, CenterAt, keyed: false);
        Picture(s.Ui_CenterOverlay1.Value, CenterAt, keyed: true);
        Picture(s.Ui_CenterOverlay2.Value, CenterAt, keyed: true);
        Picture(s.Ui_CenterOverlay3.Value, CenterAt, keyed: true);
        Picture(s.Ui_CenterOverlay4.Value, CenterAt, keyed: true);
    }

    /// <summary><c>WAO_SUB</c> drew a picture or removed it (<c>311</c>/<c>312</c>, <c>321</c>/<c>322</c>, <c>331</c>-<c>333</c>): it is no longer drawn over.</summary>
    private void Uncover(int op) => _covered &= op switch
    {
        311 or 312 => ~Pictures.PortraitL,
        321 or 322 => ~Pictures.PortraitR,
        331 or 332 or 333 => ~Pictures.Center,
        _ => ~Pictures.None,
    };

    /// <summary>The open window's frame (<c>Ui_WinFrameCg</c>) at its own position; the full-screen window's is opaque under the keyed one.</summary>
    private void Frame(GameState s)
    {
        if (s.Ui_WinType == 0 || (_covered & Pictures.Frame) != 0 || _cgs.Get(s.Ui_WinFrameCg.Value) is not { } frame)
            return;
        if (s.Ui_WinType.Value is >= 31 and <= 39)
            Screen.DrawCg(frame, frame.X, frame.Y);
        Screen.DrawCg(frame, frame.X, frame.Y, Surface.SpriteKey);
    }

    private void Picture(int number, (int X, int Y) at, bool keyed)
    {
        if (number != 0 && _cgs.Get(number) is { } cg)
            Screen.DrawCg(cg, at.X, at.Y, keyed ? Surface.SpriteKey : null);
    }
}
