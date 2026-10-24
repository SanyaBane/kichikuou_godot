using Godot;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The touch mode's Skip and Log in the scenes (owner's B1-B3b, 2026-10-24; docs/ui/touch.md, "The dialogue buttons,
/// round 2"): a lacquer drawer glued to the screen's left edge, Skip over Log, an icon over a small label in each cell
/// (<see cref="SkipButtons"/>). Over 131 its cells stand on the dock our message window draws beside the bar
/// (<see cref="Inset"/>). Skip is lit gold, "ON", while the skip runs; a greyed cell is deaf. A tap on a cell (its
/// release over the cell pressed) is <see cref="SkipTapped"/> or <see cref="LogTapped"/>; a tap elsewhere on the drawer
/// is its own and does nothing; its other events go on to the screen through <see cref="Forwarded"/>.
/// </summary>
internal sealed partial class SkipDrawer : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Messages);

    private static readonly Color LitTop = Color.Color8(255, 214, 110), LitFoot = Color.Color8(214, 140, 40),
        LitInk = Color.Color8(40, 24, 8), LitGlow = new(1f, 0.74f, 0.35f, 0.75f), Held = new(1, 1, 1, 0.08f);

    private const float Icon = 24, Label = 13;

    private SkipButtons.Box _plate = SkipButtons.Drawer;
    private float _k = 1;
    private bool _skipOn, _skipDisabled, _logDisabled, _inset;
    private int _held = -1;                              // the cell pressed (0 Skip, 1 Log), -1 none

    public SkipDrawer()
    {
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        MouseExited += () => SetHeld(-1);
    }

    /// <summary>A tap on Skip, unless it is greyed.</summary>
    public event Action? SkipTapped;

    /// <summary>A tap on Log, unless it is greyed.</summary>
    public event Action? LogTapped;

    /// <summary>An event but a left button's: the screen's own.</summary>
    public event Action<InputEvent>? Forwarded;

    /// <summary>Skip lit: the message skip runs.</summary>
    public bool SkipOn { get => _skipOn; set => Set(ref _skipOn, value); }

    /// <summary>Skip greyed: the skip is not allowed now.</summary>
    public bool SkipDisabled { get => _skipDisabled; set => Set(ref _skipDisabled, value); }

    /// <summary>Log greyed: the message log does not open now.</summary>
    public bool LogDisabled { get => _logDisabled; set => Set(ref _logDisabled, value); }

    /// <summary>The Skip cell's centre (global coordinates).</summary>
    public Vector2 SkipCentre => CellCentre(SkipButtons.SkipCell);

    /// <summary>The Log cell's centre (global coordinates).</summary>
    public Vector2 LogCentre => CellCentre(SkipButtons.LogCell);

    /// <summary>
    /// Places the drawer on the picture <paramref name="area"/> (its parent's coordinates) over <paramref name="plate"/>
    /// (screen pixels): its own plate, or the dock it stands on when <paramref name="inset"/>.
    /// </summary>
    public void Place(Rect2 area, SkipButtons.Box plate, bool inset)
    {
        float k = area.Size.X / 1280;
        var position = area.Position + new Vector2(plate.X, plate.Y) * k;
        if (plate == _plate && inset == _inset && Math.Abs(k - _k) < 0.001f && Position == position)
            return;
        (_plate, _inset, _k) = (plate, inset, k);
        Position = position;
        Size = new Vector2(plate.W, plate.H) * k;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            Forwarded?.Invoke(e);
            return;
        }
        AcceptEvent();
        int cell = CellAt(button.Position);
        if (button.Pressed)
        {
            SetHeld(cell);
            return;
        }
        int held = _held;
        SetHeld(-1);
        if (held < 0 || cell != held)
            return;
        if (held == 0 && !_skipDisabled)
            SkipTapped?.Invoke();
        else if (held == 1 && !_logDisabled)
            LogTapped?.Invoke();
    }

    /// <summary>The cell at <paramref name="local"/> (the control's coordinates): 0 Skip, 1 Log, -1 neither.</summary>
    private int CellAt(Vector2 local)
    {
        var p = local / _k + new Vector2(_plate.X, _plate.Y);
        return Contains(SkipButtons.SkipCell, p) ? 0 : Contains(SkipButtons.LogCell, p) ? 1 : -1;

        static bool Contains(SkipButtons.Box b, Vector2 p) => p.X >= b.X && p.X < b.X + b.W && p.Y >= b.Y && p.Y < b.Y + b.H;
    }

    private Vector2 CellCentre(SkipButtons.Box cell) =>
        GlobalPosition + (new Vector2(cell.X + cell.W / 2f, cell.Y + cell.H / 2f) - new Vector2(_plate.X, _plate.Y)) * _k;

    private void SetHeld(int cell)
    {
        if (cell == _held)
            return;
        _held = cell;
        QueueRedraw();
    }

    private void Set(ref bool field, bool value)
    {
        if (field == value)
            return;
        field = value;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // In the screen's pixels, the plate's corner at the control's origin.
        DrawSetTransform(-new Vector2(_plate.X, _plate.Y) * _k, 0, new Vector2(_k, _k));
        if (!_inset)
            DrawPlate(this, new Rect2(_plate.X, _plate.Y, _plate.W, _plate.H), _k);
        var skip = Rect(SkipButtons.SkipCell);
        var log = Rect(SkipButtons.LogCell);
        if (_held >= 0 && !(_held == 0 ? _skipDisabled : _logDisabled))
            DrawRect(_held == 0 ? skip : log, Held);
        if (_skipOn)
            LitCell(skip.Grow(-3));
        // The divider between the cells.
        float y = log.Position.Y, mid = skip.GetCenter().X;
        DrawLine(new Vector2(8, y), new Vector2(skip.End.X - 8, y), Ui.DimGold, Lacquer.Line(1, _k));
        Lacquer.DrawDiamond(this, new Vector2(mid, y), 3, Ui.Gold);
        var font = GetThemeDefaultFont();
        Color Ink(bool disabled, Color on) => disabled ? Ui.OffText : on;
        var skipInk = _skipOn ? LitInk : Ink(_skipDisabled, Ui.Gold);
        Arrows(new Vector2(mid + 2, skip.Position.Y + 32), skipInk);
        Lines(new Vector2(mid, log.Position.Y + 32), Ink(_logDisabled, Ui.Gold));
        if (font is null)
            return;
        CellLabel(font, skip, _skipOn ? "ON" : "Skip", _skipOn ? LitInk : Ink(_skipDisabled, Ui.Quiet));
        CellLabel(font, log, "Log", Ink(_logDisabled, Ui.Quiet));
    }

    /// <summary>
    /// The drawer's plate (and the dock over 131, <see cref="MessageWindowView"/>): lacquer glued to the screen's left edge,
    /// gold along its other three sides, the right corners rounded, a dim inner line (screen pixels).
    /// </summary>
    public static void DrawPlate(CanvasItem c, Rect2 box, float k)
    {
        var plate = new StyleBoxFlat
        {
            BgColor = Ui.Panel,
            BorderColor = Ui.Gold,
            AntiAliasing = true,
            ShadowColor = new Color(0, 0, 0, 0.35f),
            ShadowSize = 6,
            ShadowOffset = new Vector2(2, 4),
        };
        plate.SetBorderWidthAll((int)Math.Ceiling(Lacquer.Line(2, k)));
        plate.BorderWidthLeft = 0;
        plate.CornerRadiusTopRight = plate.CornerRadiusBottomRight = 10;
        c.DrawStyleBox(plate, box);
        var inner = new StyleBoxFlat { DrawCenter = false, BorderColor = Ui.DimGold, AntiAliasing = true };
        inner.SetBorderWidthAll((int)Math.Ceiling(Lacquer.Line(1, k)));
        inner.BorderWidthLeft = 0;
        inner.CornerRadiusTopRight = inner.CornerRadiusBottomRight = 6;
        c.DrawStyleBox(inner, new Rect2(box.Position.X, box.Position.Y + 4.5f, box.Size.X - 4.5f, box.Size.Y - 9));
    }

    private static Rect2 Rect(SkipButtons.Box b) => new(b.X, b.Y, b.W, b.H);

    /// <summary>The lit Skip: a gold cell with a white edge and a glow.</summary>
    private void LitCell(Rect2 r)
    {
        var cell = new StyleBoxFlat
        {
            BgColor = LitFoot,
            BorderColor = Colors.White,
            AntiAliasing = true,
            ShadowColor = LitGlow,
            ShadowSize = 10,
        };
        cell.SetBorderWidthAll((int)Math.Ceiling(Lacquer.Line(2, _k)));
        cell.SetCornerRadiusAll(8);
        DrawStyleBox(cell, r);
        // The gold's light from the top: the cell's upper half a lighter gold.
        var top = new StyleBoxFlat { BgColor = LitTop with { A = 0.55f }, AntiAliasing = true };
        top.CornerRadiusTopLeft = top.CornerRadiusTopRight = 7;
        DrawStyleBox(top, new Rect2(r.Position + new Vector2(2, 2), new Vector2(r.Size.X - 4, r.Size.Y / 2 - 2)));
    }

    private void CellLabel(Font font, Rect2 cell, string text, Color colour)
    {
        int size = (int)Label;
        float baseline = cell.Position.Y + 64 + (font.GetAscent(size) - font.GetDescent(size)) / 2;
        DrawString(font, new Vector2(cell.Position.X, baseline), text, HorizontalAlignment.Center, cell.Size.X, size, colour);
    }

    /// <summary>The skip's icon: two triangles pointing right, centred at <paramref name="c"/>.</summary>
    private void Arrows(Vector2 c, Color colour)
    {
        float h = Icon / 2;
        foreach (float dx in (float[])[-Icon * 0.55f, Icon * 0.05f])
            DrawColoredPolygon([new(c.X + dx, c.Y - h), new(c.X + dx + Icon * 0.6f, c.Y), new(c.X + dx, c.Y + h)], colour);
    }

    /// <summary>The log's icon: three lines of text, the last shorter.</summary>
    private void Lines(Vector2 c, Color colour)
    {
        float thick = Icon * 0.16f;
        for (int i = 0; i < 3; i++)
        {
            float y = c.Y - Icon * 0.4f + i * Icon * 0.4f, w = i == 2 ? Icon * 0.6f : Icon;
            var line = new StyleBoxFlat { BgColor = colour, AntiAliasing = true };
            line.SetCornerRadiusAll((int)Math.Ceiling(thick / 2));
            DrawStyleBox(line, new Rect2(c.X - Icon / 2, y - thick / 2, w, thick));
        }
    }
}
