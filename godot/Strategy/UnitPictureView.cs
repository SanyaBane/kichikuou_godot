using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// A unit's picture panel raised over the phone's screen by a long press on its ledger row (docs/ui/phone.md, PD2d;
/// docs/ui/phone-build.md, step L5) — the original's picture and its words about the unit, which the PC shows while the
/// row is hovered: the screen dimmed, the panel in the frame's middle as high as the frame allows
/// (<see cref="PhoneLedger.Picture"/>); held upright (step P5) a sheet over the screen's foot with the unit's name, type and
/// troops over the panel at the screen's width (<see cref="PhoneLedgerPortrait.PictureSheet"/>). A tap anywhere puts it away.
/// Over the deployment table and the Subordinates screen.
/// </summary>
internal sealed partial class UnitPictureView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    private ImageTexture? _picture;
    private (string Name, string Detail)? _caption;
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);

    public UnitPictureView()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    /// <summary>Whether a picture is up.</summary>
    public bool Showing => Visible;

    /// <summary>
    /// Raises <paramref name="picture"/> (nothing without one), the unit's <paramref name="caption"/> over it held upright; it
    /// takes every press till a tap puts it away.
    /// </summary>
    public void Show(ImageTexture? picture, (string Name, string Detail)? caption = null)
    {
        if (picture is null)
            return;
        _picture = picture;
        _caption = caption;
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
        if (_picture is null || Device.Held(this) is not { } f)
            return;
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0, 0, 0, 0.6f));
        if (f.IsPortrait)
        {
            DrawPortrait(f);
            return;
        }
        var b = PhoneLedger.Picture(f);
        var area = new Rect2(b.X, b.Y, b.W, b.H);
        for (int i = 12; i >= 1; i--)
            DrawRect(area.Grow(i * f.Px(2)), new Color(0, 0, 0, 0.06f));
        PanelDescriptions.Draw(this, _picture, area);
        DrawRect(area, Ui.DimGold, false, Math.Max(1, f.Px(3)));
    }

    /// <summary>PD2d: the sheet over the screen's foot, the name in the brush and the type with the troops after it, the panel under them.</summary>
    private void DrawPortrait(PhoneFrame f)
    {
        float k = f.Scale;
        var sheet = PhoneLedgerPortrait.PictureSheet(f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(k, k));
        Ui.DrawPanel(this, new Rect2(sheet.X / k, sheet.Y / k, sheet.W / k, sheet.H / k), k);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        if (_caption is { } caption)
        {
            var brush = SideCells.Brush;
            int name = (int)Math.Round(f.Px(PhoneLedgerPortrait.SheetNameText)), detail = (int)Math.Round(f.Px(PhoneLedgerPortrait.SheetDetailText));
            float x = sheet.X + f.Px(PhoneLedgerPortrait.SheetNameX), mid = sheet.Y + f.Px(PhoneLedgerPortrait.SheetNameMiddle);
            DrawString(brush, new Vector2(x, mid + (brush.GetAscent(name) - brush.GetDescent(name)) / 2), caption.Name, fontSize: name, modulate: Ui.Name);
            float dx = x + brush.GetStringSize(caption.Name, fontSize: name).X + f.Px(PhoneLedgerPortrait.SheetDetailGap);
            float dmid = sheet.Y + f.Px(PhoneLedgerPortrait.SheetDetailMiddle);
            int fitted = SideCells.Fit(_cinzel, caption.Detail, detail, sheet.Right - f.Px(PhoneLedgerPortrait.SheetNameX) - dx);
            DrawString(_cinzel, new Vector2(dx, dmid + (_cinzel.GetAscent(fitted) - _cinzel.GetDescent(fitted)) / 2), caption.Detail,
                fontSize: fitted, modulate: Ui.Subtitle);
        }
        var b = PhoneLedgerPortrait.Picture(f);
        var area = new Rect2(b.X, b.Y, b.W, b.H);
        PanelDescriptions.Draw(this, _picture!, area);
        DrawRect(area, Ui.Gold, false, Math.Max(1, f.Px(3)));
    }
}
