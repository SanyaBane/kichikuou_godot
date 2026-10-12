using Godot;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Debug → Portraits' picture — a face twice as large, or a stand-in's battle sprite (320×256) as
/// large as fits the same width — with its square framed: the left button drags the square, the
/// wheel sizes it by 8 pixels about its centre (<see cref="FaceCrop.Clamped"/> keeps it inside the
/// picture). <see cref="Changed"/> follows every move, <see cref="Finished"/> the release.
/// </summary>
internal sealed partial class FaceCropEditor : Control
{
    private static readonly Vector2 Area = new Vector2(UnitFaces.Width, UnitFaces.Height) * 2;
    private Texture2D? _picture;
    private float _zoom = 2;
    private FaceCrop _crop;
    private Vector2? _dragFrom;
    private FaceCrop _dragStart;

    /// <summary>The square moved or sized.</summary>
    public event Action<FaceCrop>? Changed;

    /// <summary>A drag released or a wheel step done: the square as it stays.</summary>
    public event Action<FaceCrop>? Finished;

    public FaceCropEditor()
    {
        CustomMinimumSize = Area;
        TextureFilter = TextureFilterEnum.Nearest;
        MouseDefaultCursorShape = CursorShape.Move;
    }

    /// <summary>Shows <paramref name="picture"/> (null: nothing) with <paramref name="crop"/> framed.</summary>
    public void SetPicture(Texture2D? picture, FaceCrop crop)
    {
        _picture = picture;
        _zoom = picture is null ? 2 : Math.Min(2, Area.X / picture.GetWidth());
        _crop = crop;
        _dragFrom = null;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_picture is null)
            return;
        DrawTextureRect(_picture, new Rect2(Vector2.Zero, _picture.GetSize() * _zoom), false);
        var frame = new Rect2(new Vector2(_crop.X, _crop.Y) * _zoom, new Vector2(_crop.Size, _crop.Size) * _zoom);
        DrawRect(frame, Colors.Yellow, false, 2);
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_picture is null)
            return;
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                _dragFrom = press.Position;
                _dragStart = _crop;
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } when _dragFrom is not null:
                _dragFrom = null;
                Finished?.Invoke(_crop);
                AcceptEvent();
                break;
            case InputEventMouseMotion motion when _dragFrom is { } from:
                var delta = (motion.Position - from) / _zoom;
                Set(new FaceCrop(_dragStart.X + (int)MathF.Round(delta.X), _dragStart.Y + (int)MathF.Round(delta.Y), _dragStart.Size));
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown, Pressed: true } wheel
                when _dragFrom is null:
                int step = wheel.ButtonIndex == MouseButton.WheelUp ? -8 : 8;      // up: closer
                Set(new FaceCrop(_crop.X - step / 2, _crop.Y - step / 2, _crop.Size + step));
                Finished?.Invoke(_crop);
                AcceptEvent();
                break;
        }
    }

    private void Set(FaceCrop crop)
    {
        crop = crop.Clamped(_picture!.GetWidth(), _picture.GetHeight());
        if (crop == _crop)
            return;
        _crop = crop;
        QueueRedraw();
        Changed?.Invoke(crop);
    }
}
