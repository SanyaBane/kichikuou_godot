using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Text Speed… (docs/ui/messages.md, §5 step 4; the owner's mock-up S2, 2026-10-18): a lacquer dialog over the
/// game's top, the picture dimmed — the title, a slider of <see cref="TextSpeedChoice.Steps"/> steps (Slow … Instant,
/// the step over the knob), Default, OK and Cancel — and as the preview the game's own 121 at its place and size, typing
/// a line of Maris's at the chosen speed again and again. Dragging or clicking the track, or Left and Right, choose the
/// step; Return is OK, Escape or a right click Cancel. Laid out in the 1280×800 screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class TextSpeedView : Control
{
    private static readonly Rect2 Box = new(330, 90, 620, 300);
    private static readonly string[] PreviewLines =
        ["I believe that we should first", "prepare our cities for their", "attacks, then defend them with", "urban warfare."];

    private const float Hold = 1.2f;                // seconds the whole preview stays before it types again

    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);
    private readonly FontFile _gothic;
    private readonly MessageWindowView _preview;
    private readonly Control _plate;
    private readonly MessageWindow _sample = MessageWindow.Sample(PreviewLines);
    private int _step;
    private bool _dragging;
    private int _hovered = -1, _pressed = -1;
    private double _held;
    private float _k = 1;                           // window pixels to a screen pixel

    /// <summary>OK with the chosen step.</summary>
    public event Action<int>? Chosen;

    /// <summary>The dialog closed, by OK or Cancel.</summary>
    public event Action? Closed;

    public TextSpeedView(GameFont font, int step)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        _step = step;
        MouseFilter = MouseFilterEnum.Stop;
        _preview = new MessageWindowView(font) { Tail = false };
        AddChild(_preview);
        _plate = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _plate.Draw += DrawPlate;
        AddChild(_plate);
        _preview.Show(_sample, true, null, null);
        SetStep(step);
    }

    /// <summary>Lays the dialog over the screen's picture (<paramref name="area"/>, in its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        float k = area.Size.X / 1280f;
        _preview.Place(new Rect2(Vector2.Zero, area.Size));
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f)
            return;
        Position = area.Position;
        Size = area.Size;
        _plate.Size = area.Size;
        _k = k;
        QueueRedraw();
        _plate.QueueRedraw();
    }

    private void SetStep(int step)
    {
        _step = Math.Clamp(step, 1, TextSpeedChoice.Steps);
        _preview.LettersPerSecond = TextSpeedChoice.LettersPerSecond(_step);
        _preview.Restart();
        _held = 0;
        QueueRedraw();
    }

    /// <summary>The preview types again a while after its text is whole; at Instant it stays.</summary>
    public override void _Process(double delta)
    {
        if (_preview.Typing || double.IsPositiveInfinity(_preview.LettersPerSecond))
            return;
        _held += delta;
        if (_held < Hold)
            return;
        _held = 0;
        _preview.Restart();
    }

    // ---------------------------------------------------------------- the layout (screen pixels, mock-up S2)

    private static float TrackLeft => Box.Position.X + 80;

    private static float TrackRight => Box.End.X - 80;

    private static float TrackY => Box.Position.Y + 150;

    private float KnobX => TrackLeft + (TrackRight - TrackLeft) * (_step - 1) / (TextSpeedChoice.Steps - 1);

    private static readonly (string Label, Rect2 Rect)[] Buttons =
    [
        ("Default", new Rect2(Box.Position.X + 40, Box.End.Y - 58, 120, 34)),
        ("OK", new Rect2(Box.End.X - 250, Box.End.Y - 58, 100, 34)),
        ("Cancel", new Rect2(Box.End.X - 140, Box.End.Y - 58, 100, 34)),
    ];

    private const int DefaultButton = 0, OkButton = 1, CancelButton = 2;

    private static int ButtonAt(Vector2 at)
    {
        for (int i = 0; i < Buttons.Length; i++)
            if (Buttons[i].Rect.HasPoint(at))
                return i;
        return -1;
    }

    private static bool OnTrack(Vector2 at) =>
        at.X >= TrackLeft - 18 && at.X <= TrackRight + 18 && Math.Abs(at.Y - TrackY) <= 22;

    private static int StepAt(float x) =>
        1 + (int)Math.Round(Math.Clamp((x - TrackLeft) / (TrackRight - TrackLeft), 0, 1) * (TextSpeedChoice.Steps - 1));

    // ---------------------------------------------------------------- input

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion:
            {
                var at = motion.Position / _k;
                if (_dragging)
                {
                    int step = StepAt(at.X);
                    if (step != _step)
                        SetStep(step);
                }
                int button = ButtonAt(at);
                if (button != _hovered)
                {
                    _hovered = button;
                    QueueRedraw();
                }
                MouseDefaultCursorShape = button >= 0 || OnTrack(at) ? CursorShape.PointingHand : CursorShape.Arrow;
                break;
            }
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false }:
                Close(null);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
            {
                var at = click.Position / _k;
                if (click.Pressed)
                {
                    _pressed = ButtonAt(at);
                    if (_pressed < 0 && OnTrack(at))
                    {
                        _dragging = true;
                        SetStep(StepAt(at.X));
                    }
                }
                else
                {
                    _dragging = false;
                    int released = ButtonAt(at);
                    if (released >= 0 && released == _pressed)
                        Press(released);
                    _pressed = -1;
                }
                break;
            }
        }
        AcceptEvent();
    }

    public override void _Input(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true } key)
            return;
        switch (key.Keycode)
        {
            case Key.Left:
                SetStep(_step - 1);
                break;
            case Key.Right:
                SetStep(_step + 1);
                break;
            case Key.Enter or Key.KpEnter:
                Press(OkButton);
                break;
            case Key.Escape:
                Press(CancelButton);
                break;
            default:
                return;
        }
        GetViewport().SetInputAsHandled();
    }

    private void Press(int button)
    {
        switch (button)
        {
            case DefaultButton:
                SetStep(TextSpeedChoice.DefaultStep);
                break;
            case OkButton:
                Close(_step);
                break;
            case CancelButton:
                Close(null);
                break;
        }
    }

    private void Close(int? chosen)
    {
        if (IsQueuedForDeletion())
            return;
        if (chosen is { } step)
            Chosen?.Invoke(step);
        Closed?.Invoke();
        QueueFree();
    }

    // ---------------------------------------------------------------- drawing

    private float Line(float width) => Lacquer.Line(width, _k);

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        DrawRect(new Rect2(0, 0, 1280, 800), new Color(0, 0, 0, 0.45f));
        Lacquer.DrawPanel(this, Box, _k);
        float cx = Box.GetCenter().X, y = Box.Position.Y;
        Centred(_brush, "Text Speed", 34, cx, y + 58, Lacquer.Name);
        Lacquer.DrawDivider(this, Box.Position.X + 60, Box.End.X - 60, y + 82, _k);
        DrawSlider();
        for (int i = 0; i < Buttons.Length; i++)
        {
            var (label, rect) = Buttons[i];
            bool lit = i == _hovered || i == OkButton && _hovered < 0;
            Lacquer.DrawRowBox(this, rect, lit, _k);
            Centred(_gothic, label, 24, rect.GetCenter().X, Lacquer.Baseline(rect, _gothic, 24), lit ? Lacquer.Name : Lacquer.Text);
        }
    }

    /// <summary>The track, filled up to the knob; a tick for each step, Instant's bright; the knob a gold diamond with its step over it.</summary>
    private void DrawSlider()
    {
        float x0 = TrackLeft, x1 = TrackRight, y = TrackY, knob = KnobX;
        var track = new StyleBoxFlat { BgColor = Lacquer.Row, BorderColor = Lacquer.DimGold, AntiAliasing = true };
        track.SetCornerRadiusAll(4);
        track.SetBorderWidthAll(1);
        DrawStyleBox(track, new Rect2(x0, y - 4, x1 - x0, 8));
        if (knob > x0)
        {
            var fill = new StyleBoxFlat { BgColor = Color.Color8(150, 118, 58), AntiAliasing = true };
            fill.SetCornerRadiusAll(4);
            DrawStyleBox(fill, new Rect2(x0, y - 4, knob - x0, 8));
        }
        for (int i = 0; i < TextSpeedChoice.Steps; i++)
        {
            float px = x0 + (x1 - x0) * i / (TextSpeedChoice.Steps - 1);
            bool last = i == TextSpeedChoice.Steps - 1;
            DrawLine(new Vector2(px, y + 12), new Vector2(px, y + 20), last ? Lacquer.Gold : Lacquer.DimGold, Line(last ? 2 : 1));
        }
        var diamond = new Vector2[] { new(knob, y - 16), new(knob + 14, y), new(knob, y + 16), new(knob - 14, y) };
        DrawColoredPolygon(diamond, Lacquer.Gold);
        DrawPolyline([.. diamond, diamond[0]], Lacquer.Name, Line(1), true);
        Centred(_cinzel, _step.ToString(), 22, knob, y - 26, Lacquer.Name);
        Centred(_cinzel, "SLOW", 18, x0, y + 46, Lacquer.Label);
        Centred(_cinzel, "INSTANT", 18, x1, y + 46, Lacquer.Name);
    }

    /// <summary>The preview's label: a small plate on the 121's top edge, over the panel.</summary>
    private void DrawPlate()
    {
        _plate.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        var panel = _sample.Panel;
        var plate = new Rect2(panel.X + 40, panel.Y - 15, 150, 30);
        _plate.DrawRect(plate, Lacquer.Panel with { A = 1 });
        _plate.DrawRect(plate, Lacquer.Gold, false, Line(2));
        string text = "PREVIEW";
        float tw = _cinzel.GetStringSize(text, fontSize: 18).X;
        _plate.DrawString(_cinzel, new Vector2(plate.GetCenter().X - tw / 2, Lacquer.Baseline(plate, _cinzel, 18)), text,
            fontSize: 18, modulate: Lacquer.Name);
    }

    private void Centred(Font font, string text, int size, float cx, float baseline, Color colour)
    {
        float tw = font.GetStringSize(text, fontSize: size).X;
        DrawString(font, new Vector2(cx - tw / 2, baseline), text, fontSize: size, modulate: colour);
    }
}
