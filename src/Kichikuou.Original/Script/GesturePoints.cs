using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Script;

/// <summary>
/// Where the original's screens take the gestures a <see cref="PointerScript"/> queues: the
/// pickers' grid and bar, and a map point on the screen (a point out of view brought in first with
/// <see cref="DragToward"/>).
/// </summary>
public static class GesturePoints
{
    /// <summary>
    /// The middle of cell <paramref name="id"/> (1-51, row by row, three to a row) of the picker
    /// screens' grid (<c>Sﾌﾞﾀｲ</c>, <c>Sﾌﾞｶ</c>, <c>Sﾊｰﾚﾑ</c>): unit or girl <paramref name="id"/>.
    /// The cells are 108×18 from (328, 18) (<c>Sﾌﾞﾀｲ.ADV:500-505</c>).
    /// </summary>
    public static (int X, int Y) PickerCell(int id)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(id, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(id, 51);
        return (328 + (id - 1) % 3 * 108 + 40, 18 + (id - 1) / 3 * 18 + 9);
    }

    /// <summary>
    /// The middle of bar button <paramref name="button"/> (1-3, left to right) under the pickers'
    /// grid: in <c>Sﾌﾞﾀｲ</c> 501 (Attack, Plains, Search), 502 (City) and 503 (Stop); in
    /// <c>Sﾌﾞｶ</c> and <c>Sﾊｰﾚﾑ</c> only the third, "End" (<c>Sﾌﾞﾀｲ.ADV:507-517</c>).
    /// </summary>
    public static (int X, int Y) PickerButton(int button)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(button, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(button, 3);
        return (320 + (button - 1) * 106 + 53, 360);
    }

    /// <summary>
    /// A point left of the pickers' grid: a left click there takes the last unit out of the
    /// party in <c>Sﾌﾞﾀｲ</c>, ends <c>Sﾌﾞｶ</c> and does nothing in <c>Sﾊｰﾚﾑ</c>.
    /// </summary>
    public static (int X, int Y) PickerOutside => (160, 200);

    /// <summary>
    /// Where a map point is on the 640×400 screen; null when the point is not in view. The camera
    /// (<c>MapCamX</c>, <c>MapCamY</c>) is <paramref name="centred"/> on the view when the map wait
    /// begins and is turned into the view's corner by the map loop (<c>COMMAND.ADV:51-58</c>), which
    /// keeps it so while it waits — after a drag it is the corner (<see cref="ViewCorner"/>).
    /// </summary>
    public static (int X, int Y)? OnScreen(GameState state, Sys3Int mapX, Sys3Int mapY, bool centred = true)
    {
        var (left, top) = ViewCorner(state, centred);
        int x = mapX.Value - left;
        int y = mapY.Value - top;
        return x is >= 0 and < 640 && y is >= 0 and < 400 ? (x, y) : null;
    }

    /// <summary>
    /// The view's top-left corner on the map: the camera <paramref name="centred"/> on the view as
    /// the map loop turns it into the corner (<c>COMMAND.ADV:51-58</c>, saturating at 0 and held
    /// within 1280 × 400), or the camera itself while the loop waits after a drag.
    /// </summary>
    public static (int X, int Y) ViewCorner(GameState state, bool centred)
    {
        if (!centred)
            return (state.MapCamX.Value, state.MapCamY.Value);
        Sys3Int left = state.MapCamX - 320;
        Sys3Int top = state.MapCamY - 200;
        if (left > 1280)
            left = 1280;
        if (top > 400)
            top = 400;
        return (left.Value, top.Value);
    }

    /// <summary>
    /// A <see cref="PointerScript.Drag"/> that brings a map point toward the middle of the view: the drag-scroll
    /// moves the view's corner by the pointer's way back (<c>WAO_MAP.ADV:20-51</c>; the corner stays
    /// within 0-1280 × 0-400), at most 600 × 360 at a time, so a far point may need a second one;
    /// null when the view cannot move toward it. The camera is <paramref name="centred"/> as in
    /// <see cref="OnScreen"/>.
    /// </summary>
    public static (int FromX, int FromY, int ToX, int ToY)? DragToward(GameState state, Sys3Int mapX, Sys3Int mapY, bool centred = true)
    {
        var (left, top) = ViewCorner(state, centred);
        int dx = Math.Clamp(Math.Clamp(mapX.Value - 320, 0, 1280) - left, -600, 600);
        int dy = Math.Clamp(Math.Clamp(mapY.Value - 200, 0, 400) - top, -360, 360);
        if (dx == 0 && dy == 0)
            return null;
        int fromX = 320 + dx / 2, fromY = 200 + dy / 2;
        return (fromX, fromY, fromX - dx, fromY - dy);
    }
}
