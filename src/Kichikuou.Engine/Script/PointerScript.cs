using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

/// <summary>
/// The pointer as a queue of gestures, read back one <c>IM</c> at a time
/// (<see cref="ISceneInput.GetMouseAsync"/>). The map and the pickers poll the mouse the way the
/// script does; a UI that lets the player pick a province or a menu instead of moving a pointer
/// (and the tests) queues the gestures the script expects — <see cref="Click"/>,
/// <see cref="RightClick"/>, <see cref="Drag"/> — at the screen points the variant's screens have
/// (the original's <c>GesturePoints</c>). No rule is decided here: the script still hit-tests
/// the point.
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

    /// <summary>
    /// The pointer moved to a screen point without a button, as a gesture in turn: one read there,
    /// and the reads at rest answer it afterwards. Fortune Check tests the button before the
    /// pointer's place, so a click acts on what the last read highlighted (<c>HAPPY_C.ADV:50-72</c>);
    /// a hover before the click puts the highlight under it, as the pointer's way there does in real time.
    /// </summary>
    public void Hover(int x, int y) => Add([Read(0, x, y)]);

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

    private static MouseState Read(int button, int x, int y) => new((Sys3Int)button, (Sys3Int)x, (Sys3Int)y);
}
