using Godot;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// <c>COMMAND</c>'s map on the phone held upright (docs/ui/phone-build.md, step P2; docs/ui/phone.md, §3, PM1): the map's
/// whole height at the screen's width under the status band, down to the bar (<see cref="PhoneMapUpright.MapBox"/>), drawn
/// by us from the state's six tiles and its markers as the script's tile area holds them (<c>WAO_MAP.ADV</c> op 3,
/// <c>BattlePresenter.DrawMapTiles</c>), so it is what the script's view shows, only taller. A finger's drag scrolls it
/// sideways: our own camera (<see cref="View"/>), the script's staying its own — the view follows the script's only when
/// the script moves it (<see cref="Follow"/>; our own tap's drag excepted, <see cref="Expect"/>). A tap is
/// <see cref="Tapped"/>, the screen finds the marker it means; a province's sheet draws the map higher
/// (<see cref="LiftFor"/>). Over its parent's whole area in the canvas's units. Interface only: no rule reads it.
/// </summary>
internal sealed partial class UprightMapView : Control
{
    private readonly FacePictures _pictures;
    private ImageTexture? _texture;
    private int _key;
    private PhoneFrame? _phone;
    private PhoneFrame.Box _band;
    private float _left, _lift;
    private bool _placed;
    private (int X, int Y)? _corner;                    // the script's view as last followed
    private (int X, int Y)? _centreOn;                  // a view to centre on once the map's box is known
    private (int X, int Y)? _expected;                  // the view our own tap's drag moves the script's to
    private bool _down, _dragging;
    private Vector2 _press;
    private float _pressLeft;

    /// <summary>How far a finger moves before its press is a drag, not a tap (1.4 mm).</summary>
    private const float Slop = 24;

    public UprightMapView(FacePictures pictures)
    {
        _pictures = pictures;
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    /// <summary>A tap on the map (its release where it came down), at its point in the canvas's units.</summary>
    public event Action<Vector2>? Tapped;

    /// <summary>An event but a left button's: the screen's own (Back's right click opens the command panel).</summary>
    public event Action<InputEvent>? Forwarded;

    /// <summary>The map's view as drawn: its box, our camera's column, the lift.</summary>
    public PhoneMapUpright.MapView View => new(_phone is { } f ? PhoneMapUpright.MapBox(f, _band) : default, _left, _lift);

    /// <summary>The script's view corner our plates are placed from: the column at the box's left and the row at its top.</summary>
    public (int X, int Y) Corner => ((int)_left, View.LiftRows);

    /// <summary>The phone's frame it was placed in.</summary>
    public PhoneFrame? Phone => _phone;

    /// <summary>
    /// Shows the map of <paramref name="state"/> (drawn again when its tiles or markers changed), following the script's
    /// view, whose corner is <paramref name="corner"/> (<see cref="Follow"/>).
    /// </summary>
    public void Show(GameState state, (int X, int Y) corner)
    {
        Visible = true;
        Load(state);
        Follow(corner);
    }

    /// <summary>The map is gone: the next one centres on the script's view again.</summary>
    public void Close()
    {
        Visible = false;
        (_corner, _centreOn, _expected, _lift) = (null, null, null, 0);
        (_down, _dragging) = (false, false);
    }

    /// <summary>
    /// The script's view corner as the screen shows it: when it moved and not to where our own tap's drag sent it, our view
    /// centres on it (the map wait's start, the script's own scrolls); else ours stays where the finger left it.
    /// </summary>
    public void Follow((int X, int Y) corner)
    {
        if (_corner == corner)
            return;
        bool ours = _expected == corner;
        _corner = corner;
        _expected = null;
        if (!ours)
            _centreOn = corner;
        Centre();
    }

    /// <summary>Our tap's drag will move the script's view to <paramref name="corner"/>: ours does not follow it there.</summary>
    public void Expect((int X, int Y) corner) => _expected = corner == _corner ? null : corner;

    /// <summary>Lays the view over its parent's whole area, the map under <paramref name="band"/>, the status band; false off an upright phone.</summary>
    public bool Place(PhoneFrame.Box band)
    {
        if (Device.Upright(this) is not { } f)
            return false;
        var size = GetParentAreaSize();
        if (f == _phone && band == _band && Position == Vector2.Zero && Size == size)
            return true;
        (_phone, _band, Position, Size) = (f, band, Vector2.Zero, size);
        _placed = true;
        if (_centreOn is null)
            _left = View.At(_left).Left;                // a turn's or a resize's narrower box
        Centre();
        QueueRedraw();
        return true;
    }

    /// <summary>The map drawn higher so map point (<paramref name="mx"/>, <paramref name="my"/>) stands over a sheet whose top is <paramref name="sheetTop"/>.</summary>
    public void LiftFor(float mx, float my, float sheetTop)
    {
        if (_phone is not { } f)
            return;
        var lifted = PhoneMapUpright.Lift(f, View with { Lift = 0 }, mx, my, sheetTop);
        if (lifted.Left == _left && lifted.Lift == _lift)
            return;
        (_left, _lift) = (lifted.Left, lifted.Lift);
        QueueRedraw();
    }

    /// <summary>The map at its place again (a sheet gone).</summary>
    public void Unlift()
    {
        if (_lift == 0)
            return;
        _lift = 0;
        QueueRedraw();
    }

    private void Centre()
    {
        if (!_placed || _centreOn is not { } corner)
            return;
        _centreOn = null;
        _left = View.Centred(corner).Left;
        QueueRedraw();
    }

    public override bool _HasPoint(Vector2 point) => _phone is { } f && PhoneMapUpright.MapBox(f, _band).Contains(point.X, point.Y);

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                AcceptEvent();
                if (button.Pressed)
                {
                    (_down, _dragging, _press, _pressLeft) = (true, false, button.Position, _left);
                }
                else if (_down)
                {
                    _down = false;
                    if (!_dragging)
                        Tapped?.Invoke(button.Position);
                    _dragging = false;
                }
                break;
            case InputEventMouseMotion motion when _down:
                AcceptEvent();
                if (!_dragging && _phone is { } f && motion.Position.DistanceTo(_press) > f.Px(Slop))
                    _dragging = true;
                if (_dragging)
                {
                    float left = View.At(_pressLeft - (motion.Position.X - _press.X) / View.Scale).Left;
                    if (left != _left)
                    {
                        _left = left;
                        QueueRedraw();
                    }
                }
                break;
            case InputEventMouseMotion:
                break;
            default:
                Forwarded?.Invoke(e);
                break;
        }
    }

    public override void _Draw()
    {
        if (_phone is null)
            return;
        var view = View;
        var box = new Rect2(view.Box.X, view.Box.Y, view.Box.W, view.Box.H);
        DrawRect(box, Colors.Black);
        if (_texture is null || view.Scale <= 0)
            return;
        int rows = view.LiftRows;
        var source = new Rect2(view.Left, rows, view.Width, PhoneMapUpright.MapHeight - rows);
        DrawTextureRectRegion(_texture, new Rect2(box.Position, box.Size.X, box.Size.Y - rows * view.Scale), source);
    }

    /// <summary>
    /// The map's picture for <paramref name="state"/>: the six tiles at their places and every marker over them as
    /// <c>WAO_MAP.ADV</c> op 3 draws them (a CG over 10000 is that CG less 10000, 18 pixels higher), palette index 40 clear;
    /// made again only when the tiles or the markers changed, into the same texture (docs/memory.md, cut 10: a new one
    /// each time held both until the collector came).
    /// </summary>
    private void Load(GameState state)
    {
        int[] tiles = [state.Ui_MapCgTopL.Value, state.Ui_MapCgTopM.Value, state.Ui_MapCgTopR.Value, state.Ui_MapCgBotL.Value,
            state.Ui_MapCgBotM.Value, state.Ui_MapCgBotR.Value];
        var marks = new List<(int Cg, int X, int Y)>();
        for (int i = 0; i < state.Ui_MarkCount.Value && i < state.Marks.Count; i++)
        {
            var mark = state.Marks[(Sys3Int)i];
            Sys3Int y = mark.Y;
            int cg = mark.Cg.Value;
            if (cg > 10000)
            {
                y -= (Sys3Int)18;
                cg -= 10000;
            }
            marks.Add((cg, mark.X.Value, y.Value));
        }
        var hash = new HashCode();
        foreach (int tile in tiles)
            hash.Add(tile);
        foreach (var mark in marks)
            hash.Add(mark);
        int key = hash.ToHashCode();
        if (_texture is not null && key == _key)
            return;
        _key = key;
        using var full = Image.CreateEmpty(PhoneMapUpright.MapWidth, PhoneMapUpright.MapHeight, false, Image.Format.Rgba8);
        full.Fill(Colors.Black);
        for (int i = 0; i < tiles.Length; i++)
            if (_pictures.Picture(tiles[i]) is { } tile)
                full.BlitRect(tile, new Rect2I(0, 0, Math.Min(640, tile.GetWidth()), Math.Min(400, tile.GetHeight())),
                    new Vector2I(i % 3 * 640, i / 3 * 400));
        foreach (var (cg, x, y) in marks)
            if (_pictures.Picture(cg) is { } picture)
                full.BlendRect(picture, new Rect2I(Vector2I.Zero, picture.GetSize()), new Vector2I(x, y));
        if (_texture is null)
            _texture = ImageTexture.CreateFromImage(full);
        else
            _texture.Update(full);
        QueueRedraw();
    }
}
