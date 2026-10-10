using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

/// <summary>
/// The pointer as a queue of gestures, read back one <c>IM</c> at a time
/// (<see cref="ISceneInput.GetMouseAsync"/>). The map and the pickers poll the mouse the way the
/// script does; a UI that lets the player pick a province or a menu instead of moving a pointer
/// (and the tests) queues the gestures the script expects — <see cref="Click"/>,
/// <see cref="RightClick"/>, <see cref="Drag"/> — at the screen points <see cref="OnScreen"/>
/// gives (a point out of view brought in first with <see cref="DragToward"/>). No rule is decided here: the script still hit-tests the point.
/// </summary>
/// <remarks>
/// In real time the script reads the pointer thousands of times while it waits, so a gesture
/// always begins in its waiting loop, never in the release waits around it. A queue has no
/// time, so a gesture is held back until the pointer has been read at rest
/// <see cref="SettleReads"/> times in a row since the last gesture or the last other input
/// (<see cref="Interrupt"/>): on the map, the release wait (<c>COMMAND.ADV:50</c>), the
/// drag-scroll's own read (<c>WAO_MAP.ADV:11</c>) and one poll of the wait (<c>:63</c>).
/// </remarks>
public sealed class PointerScript
{
    private readonly Queue<MouseState[]> _gestures = new();
    private MouseState[]? _current;
    private int _index;
    private int _rests;
    private MouseState _rest;

    /// <summary>Reads at rest before a queued gesture begins.</summary>
    public int SettleReads { get; init; } = 3;

    /// <summary>The gestures not yet finished.</summary>
    public int Pending => _gestures.Count + (_current is null ? 0 : 1);

    /// <summary>
    /// The next <c>IM</c>: the next read of the gesture under way or of the next one once the
    /// pointer has settled (<paramref name="read"/> true), else the pointer at rest — no button,
    /// at the last position.
    /// </summary>
    public MouseState Next(out bool read)
    {
        if (_current is null && _gestures.Count > 0 && _rests >= SettleReads)
        {
            _current = _gestures.Dequeue();
            _index = 0;
        }
        if (_current is not null)
        {
            var m = _current[_index++];
            if (_index == _current.Length)
            {
                _current = null;
                _rests = 0;
            }
            _rest = new MouseState(Sys3Int.Zero, m.X, m.Y);
            read = true;
            return m;
        }
        _rests++;
        read = false;
        return _rest;
    }

    /// <summary>
    /// The pointer moved to a screen point without a button: the reads at rest answer it from now
    /// on (the pickers' highlight and details follow it). A gesture under way is not affected.
    /// </summary>
    public void Move(int x, int y) => _rest = new MouseState(Sys3Int.Zero, (Sys3Int)x, (Sys3Int)y);

    /// <summary>Another input came between the pointer reads (a menu, a key): the pointer settles again.</summary>
    public void Interrupt() => _rests = 0;

    /// <summary>
    /// A left click at a screen point: the button down, then the release there.
    /// <c>COMMAND.ADV:63-98</c> takes the pressed point when the release is at the same place.
    /// </summary>
    public void Click(int x, int y) => Add([Read(16, x, y), Read(0, x, y)]);

    /// <summary>A right click (button 32): on the map it opens the command menu (<c>COMMAND.ADV:67-72</c>).</summary>
    public void RightClick(int x, int y) => Add([Read(32, x, y), Read(0, x, y)]);

    /// <summary>
    /// A drag with the left button from one screen point to another, held there for two reads
    /// (the map loop sees the move, then the drag-scroll follows it) and released: on the map the
    /// view moves with the pointer, without coasting (<c>COMMAND.ADV:76-88</c>, <c>WAO_MAP.ADV:20-51</c>).
    /// </summary>
    public void Drag(int fromX, int fromY, int toX, int toY) =>
        Add([Read(16, fromX, fromY), Read(16, toX, toY), Read(16, toX, toY), Read(0, toX, toY)]);

    private void Add(MouseState[] gesture) => _gestures.Enqueue(gesture);

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

    private static MouseState Read(int button, int x, int y) => new((Sys3Int)button, (Sys3Int)x, (Sys3Int)y);

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
    /// A <see cref="Drag"/> that brings a map point toward the middle of the view: the drag-scroll
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
