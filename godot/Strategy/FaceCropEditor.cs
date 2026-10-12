using Godot;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Debug → Portraits' face, twice as large, with its square framed: the left button drags the
/// square, the wheel sizes it by 8 pixels about its centre (<see cref="FaceCrop.Clamped"/> keeps it
/// inside the face). <see cref="Changed"/> follows every move, <see cref="Finished"/> the release.
/// </summary>
internal sealed partial class FaceCropEditor : Control
{
    private const float Zoom = 2;
    private Texture2D? _face;
    private FaceCrop _crop;
    private Vector2? _dragFrom;
    private FaceCrop _dragStart;

    /// <summary>The square moved or sized.</summary>
    public event Action<FaceCrop>? Changed;

    /// <summary>A drag released or a wheel step done: the square as it stays.</summary>
    public event Action<FaceCrop>? Finished;

    public FaceCropEditor()
    {
        CustomMinimumSize = new Vector2(UnitFaces.Width, UnitFaces.Height) * Zoom;
        TextureFilter = TextureFilterEnum.Nearest;
        MouseDefaultCursorShape = CursorShape.Move;
    }

    /// <summary>Shows <paramref name="face"/> (null: nothing) with <paramref name="crop"/> framed.</summary>
    public void SetFace(Texture2D? face, FaceCrop crop)
    {
        _face = face;
        _crop = crop;
        _dragFrom = null;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_face is null)
            return;
        DrawTextureRect(_face, new Rect2(Vector2.Zero, CustomMinimumSize), false);
        var frame = new Rect2(new Vector2(_crop.X, _crop.Y) * Zoom, new Vector2(_crop.Size, _crop.Size) * Zoom);
        DrawRect(frame, Colors.Yellow, false, 2);
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_face is null)
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
                var delta = (motion.Position - from) / Zoom;
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
        crop = crop.Clamped();
        if (crop == _crop)
            return;
        _crop = crop;
        QueueRedraw();
        Changed?.Invoke(crop);
    }
}
