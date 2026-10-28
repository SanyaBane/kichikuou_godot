using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using MouseButton = Godot.MouseButton;

namespace Kichikuou.Game.Strategy;

// Our status strip on COMMAND's map and the map's room under it (owner, 2026-10-27; docs/ui/command-screen.md, "The status
// strip" and "The map's room under the strip"): the strip lies over the map's top whenever the screen shows the map alone,
// and near the map's top edge the picture is drawn lower (MapHeadroom), so no province stays under the strip; the pointer
// the script reads is turned back to the view's rows, our plates and menus placed at the corner the screen shows.
public partial class GameScreen
{
    private readonly MapHeadroom _headroom = new();
    private ShaderMaterial _roomMaterial = null!;

    /// <summary>
    /// The screen shows <c>COMMAND</c>'s map alone, our interface on: its map wait, or our command panel or province menu
    /// over it (<c>GamePresenter.MenuCovered</c>, which holds through a city's lead-in and lead-out too, where only the
    /// map is seen), or the map still after its wait until the script shows anything else (<see cref="_mapLeft"/>). Our
    /// status strip lies over its top then.
    /// </summary>
    private bool _mapStrip;

    /// <summary>How much lower the picture is drawn (<see cref="MapHeadroom.Shift"/>), in script pixels; 0 off the map.</summary>
    private int _mapShift;

    /// <summary>
    /// The map wait is over and the script has shown nothing since but pauses and sounds: the map is still all the screen
    /// shows. The command menu first reads the pointer (<c>COMMAND.ADV:615-617</c>, a frame passing) and prints its status
    /// window in the frame its menu opens; without this the strip and the room were gone for that frame and the map jumped
    /// up (owner, 2026-10-27).
    /// </summary>
    private bool _mapLeft;

    private void ReadyHeadroom()
    {
        _roomMaterial = MapRoomPicture.Material();
        _view.Material = _roomMaterial;
    }

    /// <summary>A script event on its way to the presenter: the map wait's end, or anything shown after it (<see cref="_mapLeft"/>).</summary>
    private void ObserveMapLeft(ScriptEvent scriptEvent)
    {
        if (scriptEvent is MapScreenEvent { Waiting: false })
            _mapLeft = _mapStrip;
        else if (scriptEvent is not (PauseEvent or SoundEvent or SoundPairEvent or MusicEvent))
            _mapLeft = false;
    }

    /// <summary>
    /// Takes the map's room as the screen shows it now; returns whether it changed. The strip slides in or out and the room
    /// opens or closes with it over <see cref="SlideSeconds"/> (<see cref="AdvanceHeadroom"/>); off the map the script's
    /// reads take the picture as it is at once.
    /// </summary>
    private bool UpdateHeadroom()
    {
        bool strip = InterfaceChoice.New && _presenter.State is not null && !_presenter.InBattle && !SkippingScene
            && _presenter.MapViewShown is not null
            && (_presenter.MapWaiting && _mapCamera.Waiting || _mapLeft || _presenter.MenuCovered || _commandView.Visible);
        int shift = strip && !PhoneLandscape ? MapHeadroom.Shift(_presenter.MapViewShown!.Value.Y) : 0;  // the phone's status is in the sides
        if (strip == _mapStrip && shift == _mapShift)
            return false;
        if (strip)
            _roomBase = shift;                                              // off the map it closes from the last one
        (_mapStrip, _mapShift) = (strip, shift);
        if (!strip)
            _headroom.Release();
        ShowRoom();
        return true;
    }

    // ---------------------------------------------------------------- the strip coming and going (owner, 2026-10-27)

    /// <summary>
    /// How long the strip takes to slide into the picture when the screen comes to the map and out of it when it leaves —
    /// a dungeon, a dialogue over the map —, the room under it opening or closing with it, so the map no longer jumps into
    /// the strip's place or out of it (owner, 2026-10-27).
    /// </summary>
    private const double SlideSeconds = 0.3;

    private float _slide;                               // how far the strip is in, 0 (gone) to 1 (all in), before the easing
    private int _roomBase;                              // the room it opens to, or closes from once the screen left the map

    /// <summary>The picture's shift as drawn now, in script pixels: <see cref="_mapShift"/> on the map once the strip is in.</summary>
    private int _shownShift;

    /// <summary>How much of the strip is in the picture, 0 to 1, eased in and out.</summary>
    private float StripIn => Smooth(_slide);

    private static float Smooth(float x) => x * x * (3 - 2 * x);

    /// <summary>Once a frame: the strip slides on towards the map's state and the room follows it, its top row under the strip's foot.</summary>
    private void AdvanceHeadroom(double delta)
    {
        float target = _mapStrip ? 1 : 0, step = (float)(delta / SlideSeconds);
        if (_slide == target)
            return;
        _slide = target > _slide ? Math.Min(target, _slide + step) : Math.Max(target, _slide - step);
        ShowRoom();
    }

    /// <summary>The picture drawn lower by the room as the strip stands, to the script's pixels (our plates are placed by them).</summary>
    private void ShowRoom()
    {
        int shown = (int)MathF.Round(_roomBase * StripIn);
        if (shown == _shownShift)
            return;
        _shownShift = shown;
        MapRoomPicture.SetShift(_roomMaterial, shown);
    }

    /// <summary>The map's point at the screen's top-left corner as the picture is drawn, the room counted; null before the map.</summary>
    private (int X, int Y)? MapCornerDrawn => _presenter.MapViewShown is { } corner ? (corner.X, corner.Y - _shownShift) : null;

    /// <summary>
    /// The Y the script reads for the pointer at <paramref name="y"/> on the picture with <paramref name="buttons"/> held:
    /// on the map the view's row (<see cref="MapHeadroom.ScriptY"/>, a drag keeping its row under the pointer; while the
    /// strip slides in, by the picture as drawn), else as it is.
    /// </summary>
    private int ScriptY(int y, ushort buttons)
    {
        if (!_mapStrip || PhoneLandscape || _presenter.MapViewShown is not { } corner)
        {
            _headroom.Release();
            return y;
        }
        return _headroom.ScriptY(y, corner.Y, held: (buttons & KeyReturn) != 0, shift: _slide < 1 ? _shownShift : null);
    }

    /// <summary>The player's pointer as the script reads it: the buttons held, the place on the picture turned to the view's (<see cref="ScriptY"/>).</summary>
    private MouseState PlayerRead()
    {
        ushort buttons = _input.ReadMouse();
        return new MouseState((Sys3Int)buttons, (Sys3Int)_input.Position.X, (Sys3Int)ScriptY(_input.Position.Y, buttons));
    }

    // ---------------------------------------------------------------- unattended play's check: --map-north

    private enum NorthDrag { NotYet, Running, Done }

    private NorthDrag _northDrag;
    private Exception? _northResult;                    // how the drag ended: an AutoStopException, or the failure

    /// <summary>
    /// <see cref="GameOptions.AutoMapNorth"/> in unattended play: at the first map wait with the camera as the view's corner
    /// it starts <see cref="NorthDragAsync"/> and hands the script the pointer Godot's input holds until the drag is over,
    /// then ends the launch with the drag's report; null while it is not running.
    /// </summary>
    private async Task<MouseState?> NorthDragReadAsync()
    {
        if (!_options.AutoMapNorth || _northDrag == NorthDrag.NotYet && !(Windowed && UpdateMapMarkers() && _mapCamera.Corner))
            return null;
        if (_northDrag == NorthDrag.NotYet)
        {
            _northDrag = NorthDrag.Running;
            _ = NorthDragAsync();
        }
        if (_northDrag == NorthDrag.Done)
            throw _northResult!;
        await NextFrameAsync();
        return PlayerRead();
    }

    /// <summary>
    /// The drag: strokes to the right until the Great Wall of Helman A (49) is in the view, then a press on the map below
    /// the strip where no plate is and steps down three script pixels at a time, each followed by the script's frames,
    /// until the view's top reaches the map's top; the release, the coasting's end, the dump <c>map-north</c>. The pressed
    /// row must stay under the pointer (a row off where a step of the camera moves the picture by two) and the Great Wall's
    /// plate lie below the strip, else the run fails.
    /// </summary>
    private async Task NorthDragAsync()
    {
        try
        {
            _markers.SetHideDimmed(false);                                      // the Great Wall's plate shown whoever holds it
            for (int stroke = 0; stroke < 4 && _presenter.MapViewShown!.Value.X > WallLeft; stroke++)
            {
                var (sx, sy) = await FreeMapPointAsync([40, 80, 120, 160], [160, 200, 240, 280]);
                SendPointerButton(Global(sx, sy), MouseButton.Left, true);
                await FramesAsync(4);
                while (sx < 600 && _presenter.MapViewShown!.Value.X > WallLeft)
                {
                    sx += 6;
                    SendPointerDrag(Global(sx, sy));
                    await FramesAsync(3);
                }
                SendPointerButton(Global(sx, sy), MouseButton.Left, false);
                await CoastedAsync();
            }
            var (x, y) = await FreeMapPointAsync([320, 280, 360, 240, 400, 200, 440], [60, 80, 100, 120]);
            int top0 = _presenter.MapViewShown!.Value.Y, row = y + MapHeadroom.Shown(top0);
            SendPointerButton(Global(x, y), MouseButton.Left, true);
            await FramesAsync(4);
            int least = int.MaxValue, most = int.MinValue, top = top0;
            while (top > 0 && y < Surface.ScreenHeight - 4)
            {
                y += 3;
                SendPointerDrag(Global(x, y));
                await FramesAsync(4);
                top = _presenter.MapViewShown!.Value.Y;
                if (top == 0)
                    break;                                                      // the camera stopped: the pointer may run on
                int strayed = row - (y + MapHeadroom.Shown(top));
                (least, most) = (Math.Min(least, strayed), Math.Max(most, strayed));
            }
            SendPointerButton(Global(x, y), MouseButton.Left, false);
            await CoastedAsync();
            await AutoDumpAsync("map-north");
            top = _presenter.MapViewShown!.Value.Y;
            var picture = PictureArea();
            float stripFoot = picture.Position.Y + StatusStripView.Height * picture.Size.X / (Surface.ScreenWidth * Surface.Scale);
            var wall = _markers.PlateOf(MapHitKind.Province, GreatWallA);
            string report = $"--map-north dragged the view's top from {top0} to {top} (shift {_shownShift}): the pressed row "
                + $"{(least > most ? "never measured" : $"{least} to {most} rows above the pointer")}; the Great Wall of Helman A "
                + $"({GreatWallA})'s plate {(wall is { } w ? $"at {w.Centre.Y:0} px, the strip's foot at {stripFoot:0}" : "not shown")}.";
            bool held = least <= most && least >= 0 && most <= 1;
            bool seen = wall is { } plate && plate.Centre.Y > stripFoot;
            _northResult = top == 0 && held && seen ? new AutoStopException(report) : new InvalidOperationException(report);
        }
        catch (Exception e)
        {
            _northResult = e;
        }
        finally
        {
            _northDrag = NorthDrag.Done;
        }
    }

    /// <summary>The Great Wall of Helman A (49), the map's topmost province (595, 28), and a view's left that shows it.</summary>
    private const int GreatWallA = 49, WallLeft = 300;

    /// <summary>The camera at rest after a release (the script's coasting, <c>WAO_MAP.ADV:52-77</c>).</summary>
    private async Task CoastedAsync()
    {
        for (int still = 0; still < 10;)
        {
            var before = _presenter.MapViewShown;
            await NextFrameAsync();
            still = _presenter.MapViewShown == before ? still + 1 : 0;
        }
    }

    /// <summary>
    /// The first of the picture's points <paramref name="xs"/> × <paramref name="ys"/> below the strip that the screen
    /// itself takes (no plate over it), the pointer moved there.
    /// </summary>
    private async Task<(int X, int Y)> FreeMapPointAsync(int[] xs, int[] ys)
    {
        foreach (int y in ys)
            foreach (int x in xs)
            {
                SendPointerMotion(Global(x, y));
                await FramesAsync(2);
                if (GetViewport().GuiGetHoveredControl() == this)
                    return (x, y);
            }
        throw new InvalidOperationException("--map-north finds no point of the map free of plates.");
    }

    /// <summary>Script pixel (x, y) of the picture in the viewport's coordinates (where synthetic events are sent).</summary>
    private Vector2 Global(int x, int y) => GetGlobalTransformWithCanvas() * FromScript(new Vector2I(x, y));

    private async Task FramesAsync(int frames)
    {
        for (int i = 0; i < frames; i++)
            await NextFrameAsync();
    }
}
