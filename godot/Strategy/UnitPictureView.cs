using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// A unit's picture panel raised over the phone's screen by a long press on its ledger row (docs/ui/phone.md, PD2d;
/// docs/ui/phone-build.md, step L5) — the original's picture and its words about the unit, which the PC shows while the
/// row is hovered: the screen dimmed, the panel in the frame's middle as high as the frame allows
/// (<see cref="PhoneLedger.Picture"/>); a tap anywhere puts it away. Over the deployment table and the Subordinates screen.
/// </summary>
internal sealed partial class UnitPictureView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    private ImageTexture? _picture;

    public UnitPictureView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    /// <summary>Whether a picture is up.</summary>
    public bool Showing => Visible;

    /// <summary>Raises <paramref name="picture"/> (nothing without one); it takes every press till a tap puts it away.</summary>
    public void Show(ImageTexture? picture)
    {
        if (picture is null)
            return;
        _picture = picture;
        Visible = true;
        MouseFilter = MouseFilterEnum.Stop;
        QueueRedraw();
    }

    /// <summary>Puts the picture away.</summary>
    public new void Hide()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: false })
            Hide();
        AcceptEvent();
    }

    public override void _Draw()
    {
        if (_picture is null || Device.Landscape(this) is not { } f)
            return;
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0, 0, 0, 0.6f));
        var b = PhoneLedger.Picture(f);
        var area = new Rect2(b.X, b.Y, b.W, b.H);
        for (int i = 12; i >= 1; i--)
            DrawRect(area.Grow(i * f.Px(2)), new Color(0, 0, 0, 0.06f));
        DrawTextureRect(_picture, area, false);
        DrawRect(area, Ui.DimGold, false, Math.Max(1, f.Px(3)));
    }
}
