using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The screen margins' calibration (docs/ui/screen-margins.md, the owner's C), opened by Option → Display → Screen margins
/// over the whole screen, the options window hidden meanwhile: the screen it came from stays under it, dimmed, and lays
/// itself out inside the margins as they change (<see cref="Device.Margins"/>); the margins a gold frame with thick corner
/// marks, a tab hanging inward from each edge to drag it (the edge follows the finger's move), the panel in the frame's
/// middle with − / + for each edge (a held one steps on), Reset (every edge to none) | Done. The set is the one for the way
/// the device is held, kept at each change (<see cref="ScreenMarginsChoice"/>); a turn shows the other. Done, Back, Esc or
/// a right click close it. The layout is <see cref="MarginsCalibration"/>'s, sized by the millimetre.
/// </summary>
internal sealed partial class ScreenMarginsView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Settings);

    /// <summary>A held − or + steps again after this many seconds, then every <see cref="RepeatEvery"/>.</summary>
    private const double RepeatAfter = 0.45, RepeatEvery = 0.07;

    private static readonly Color TabFill = Color.Color8(64, 50, 26);

    private readonly FontVariation _cinzel = Lacquer.Cinzel(700);
    private readonly FontFile _gothic = DeploymentTableView.LoadFont(GodotGlyphRenderer.Chosen);
    private readonly StyleBoxFlat _tab = new() { BgColor = TabFill, BorderColor = Ui.Gold, AntiAliasing = true };
    private MarginsCalibration.Target _pressed, _hover;
    private Vector2 _dragFrom;
    private ScreenMargins _dragStart;
    private double _held;
    private DeviceLayout _layoutSeen = Device.Layout;

    /// <summary>The calibration closed (Done, Back, Esc, a right click).</summary>
    public event Action? Closed;

    public ScreenMarginsView()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 1100;                                      // over the options window (1000), which it stands in for
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    private static bool Portrait => Device.HeldPortrait;

    private MarginsCalibration Layout => MarginsCalibration.For(Size.X, Size.Y, Device.Margins, Device.PerMillimetre, Portrait);

    private static ScreenMargins Current => ScreenMarginsChoice.For(Portrait);

    /// <summary>Closes the calibration: the options window shows again.</summary>
    public void Close()
    {
        if (IsQueuedForDeletion())
            return;
        Closed?.Invoke();
        QueueFree();
    }

    public override void _Process(double delta)
    {
        if (_layoutSeen != Device.Layout)                   // turned: the other set; a press at the old layout let go
        {
            _layoutSeen = Device.Layout;
            _pressed = default;
        }
        if (_pressed.Kind is MarginsCalibration.Kind.Minus or MarginsCalibration.Kind.Plus)
        {
            double before = _held;
            _held += delta;
            if (_held >= RepeatAfter && Math.Floor((_held - RepeatAfter) / RepeatEvery) > Math.Floor((before - RepeatAfter) / RepeatEvery))
                Step(_pressed);
        }
        QueueRedraw();                                      // the frame follows the margins, the hover the pointer
    }

    private static void Step(MarginsCalibration.Target target) =>
        ScreenMarginsChoice.Set(Portrait, Current.Stepped(target.Edge, target.Kind == MarginsCalibration.Kind.Plus ? 1 : -1));

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                _pressed = Layout.Hit(press.Position.X, press.Position.Y);
                _held = 0;
                if (_pressed.Kind == MarginsCalibration.Kind.Tab)
                    (_dragFrom, _dragStart) = (press.Position, Current);
                else if (_pressed.Kind is MarginsCalibration.Kind.Minus or MarginsCalibration.Kind.Plus)
                    Step(_pressed);                         // a step at the press, more while it is held
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } release:
                var at = Layout.Hit(release.Position.X, release.Position.Y);
                if (_pressed.Kind == MarginsCalibration.Kind.Reset && at.Kind == MarginsCalibration.Kind.Reset)
                    ScreenMarginsChoice.Set(Portrait, ScreenMargins.None);
                else if (_pressed.Kind == MarginsCalibration.Kind.Done && at.Kind == MarginsCalibration.Kind.Done)
                    Close();
                _pressed = default;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                Close();
                break;
            case InputEventMouseMotion motion:
                if (_pressed.Kind == MarginsCalibration.Kind.Tab)
                {
                    var moved = motion.Position - _dragFrom;
                    ScreenMarginsChoice.Set(Portrait, MarginsCalibration.Dragged(_dragStart, _pressed.Side, moved.X, moved.Y, Device.PerMillimetre));
                }
                _hover = TouchMode.On ? default : Layout.Hit(motion.Position.X, motion.Position.Y);
                break;
            default:
                return;
        }
        AcceptEvent();
    }

    public override void _Input(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Keycode: Key.Escape })
            return;
        Close();
        GetViewport().SetInputAsHandled();
    }

    // ------------------------------------------------------------------ drawing

    private static Rect2 R(PhoneFrame.Box b) => new(b.X, b.Y, b.W, b.H);

    private static int Pt(MarginsCalibration c, float reference) => Math.Max(1, (int)Math.Round(c.Px(reference)));

    public override void _Draw()
    {
        var c = Layout;
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0, 0, 0, MarginsCalibration.Dim));
        var frame = R(c.Frame);
        float line = Math.Max(2, c.Px(MarginsCalibration.FrameLine));
        DrawRect(frame.Grow(-line / 2), Ui.Gold, false, line);
        CornerMarks(c, frame);
        foreach (var side in Enum.GetValues<MarginsCalibration.Side>())
            DrawTab(c, side);
        DrawPanel(c);
    }

    private void CornerMarks(MarginsCalibration c, Rect2 frame)
    {
        float w = Math.Max(3, c.Px(MarginsCalibration.MarkWidth)), arm = c.Px(MarginsCalibration.MarkArm), h = w / 2;
        float x0 = frame.Position.X + h, y0 = frame.Position.Y + h, x1 = frame.End.X - h, y1 = frame.End.Y - h;
        foreach (var (x, y, sx, sy) in new[] { (x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1) })
        {
            DrawLine(new Vector2(x, y - sy * h), new Vector2(x, y + sy * arm), Ui.Name, w);
            DrawLine(new Vector2(x - sx * h, y), new Vector2(x + sx * arm, y), Ui.Name, w);
        }
    }

    private void DrawTab(MarginsCalibration c, MarginsCalibration.Side side)
    {
        var box = R(c.Tab(side));
        bool lit = _pressed.Kind == MarginsCalibration.Kind.Tab && _pressed.Side == side
            || _hover.Kind == MarginsCalibration.Kind.Tab && _hover.Side == side;
        float thick = Math.Min(box.Size.X, box.Size.Y), border = Math.Max(2, c.Px(4));
        _tab.BgColor = lit ? Ui.RowHover : TabFill;
        _tab.BorderColor = lit ? Ui.Name : Ui.Gold;
        _tab.SetBorderWidthAll((int)Math.Ceiling(border));
        _tab.SetCornerRadiusAll((int)(thick / 3));
        DrawStyleBox(_tab, box);
        var (rx, ry) = c.Ridges(side);
        float ridge = thick / 3, w = Math.Max(2, c.Px(6)), gap = c.Px(26);
        bool across = side is MarginsCalibration.Side.Top or MarginsCalibration.Side.Bottom;
        for (int i = -1; i <= 1; i++)
        {
            var mid = new Vector2(rx, ry) + (across ? new Vector2(0, i * gap) : new Vector2(i * gap, 0));
            var half = across ? new Vector2(ridge, 0) : new Vector2(0, ridge);
            DrawLine(mid - half, mid + half, Ui.Name, w);
        }
    }

    private void DrawPanel(MarginsCalibration c)
    {
        Ui.DrawPanel(this, R(c.Panel), 1);
        int title = Pt(c, MarginsCalibration.TitleText), orientation = Pt(c, MarginsCalibration.OrientationText);
        var (tx, ty) = c.Title;
        var (ox, oy) = c.Orientation;
        if (c.IsPortrait)
        {
            Centred(_cinzel, "SCREEN MARGINS", tx, ty, title, Ui.Name);
            Centred(_gothic, c.OrientationName, ox, oy, orientation, Ui.Quiet);
        }
        else
        {
            DrawString(_cinzel, new Vector2(tx, PhonePickerParts.Middle(_cinzel, title, ty)), "SCREEN MARGINS", fontSize: title, modulate: Ui.Name);
            float w = _gothic.GetStringSize(c.OrientationName, fontSize: orientation).X;
            DrawString(_gothic, new Vector2(ox - w, PhonePickerParts.Middle(_gothic, orientation, oy)), c.OrientationName,
                fontSize: orientation, modulate: Ui.Quiet);
        }
        var margins = Current;
        int label = Pt(c, MarginsCalibration.LabelText), value = Pt(c, MarginsCalibration.ValueText);
        foreach (var edge in ScreenMargins.Edges)
        {
            var (lx, ly) = c.Label(edge);
            if (c.IsPortrait)
                DrawString(_gothic, new Vector2(lx, PhonePickerParts.Middle(_gothic, label, ly)), ScreenMargins.Name(edge),
                    fontSize: label, modulate: Ui.Text);
            else
                DrawString(_gothic, new Vector2(lx - 500, ly), ScreenMargins.Name(edge), HorizontalAlignment.Center, 1000, label, Ui.Text);
            StepButton(c, c.Minus(edge), new(MarginsCalibration.Kind.Minus, edge), plus: false);
            StepButton(c, c.Plus(edge), new(MarginsCalibration.Kind.Plus, edge), plus: true);
            var box = R(c.Value(edge));
            Centred(_gothic, ScreenMargins.Millimetres(margins[edge]), box.GetCenter().X, box.GetCenter().Y, value, Ui.Name);
        }
        var reset = R(c.Reset);
        var done = R(c.Done);
        // Reset | Done: Cinzel 700 at one size (docs/ui/style.md, "Buttons side by side"), shrunk together.
        int size = SideCells.FitAll(_cinzel, Pt(c, MarginsCalibration.ButtonText), ("Reset", reset.Size.X - c.Px(30)), ("Done", done.Size.X - c.Px(30)));
        bool resetLit = _hover.Kind == MarginsCalibration.Kind.Reset;
        Ui.DrawRowBox(this, reset, resetLit || _pressed.Kind == MarginsCalibration.Kind.Reset, 1);
        Centred(_cinzel, "Reset", reset.GetCenter().X, reset.GetCenter().Y, size, Ui.Text);
        bool doneLit = _hover.Kind == MarginsCalibration.Kind.Done;
        Ui.DrawRedButton(this, done, doneLit, _pressed.Kind == MarginsCalibration.Kind.Done, 1);
        Centred(_cinzel, "Done", done.GetCenter().X, done.GetCenter().Y, size, Ui.Name);
    }

    private void StepButton(MarginsCalibration c, PhoneFrame.Box b, MarginsCalibration.Target target, bool plus)
    {
        var box = R(b);
        bool lit = _pressed == target || _hover == target;
        DrawRect(box, lit ? Ui.RowHover : Ui.Row);
        DrawRect(box, lit ? Ui.Name : Ui.Gold, false, Math.Max(2, c.Px(3)));
        var mid = box.GetCenter();
        float arm = box.Size.X * 0.22f, bar = Math.Max(2, c.Px(8));
        DrawLine(mid - new Vector2(arm, 0), mid + new Vector2(arm, 0), Ui.Name, bar);
        if (plus)
            DrawLine(mid - new Vector2(0, arm), mid + new Vector2(0, arm), Ui.Name, bar);
    }

    private void Centred(Font font, string text, float x, float middle, int size, Color colour)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        DrawString(font, new Vector2(x - w / 2, PhonePickerParts.Middle(font, size, middle)), text, fontSize: size, modulate: colour);
    }
}
