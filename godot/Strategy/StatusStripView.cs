using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our status strip above the map, as wide as it (docs/ui/command-screen.md, "The status strip"; the owner's
/// mock-up C with groups d, 2026-10-16), over the map's top while the screen shows <c>COMMAND</c>'s map (owner, 2026-10-27;
/// before, while our command panel was open, 2026-10-17), in one row (owner, 2026-10-16):
/// the status window <c>COMMAND.ADV</c> prints at a right click — the nation's flag (not its name), its money and
/// troops; the week's commands as tags, lit while free, grouped under brackets with their captions (Castle,
/// Subordinates, Map); the date with LP at the right, large and bright.
/// Laid out in a 1280-wide strip of the screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class StatusStripView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Map);

    private const float Width = 1280;

    /// <summary>The strip's height in the screen's pixels (1280 wide): the map's room under it is as high (<see cref="MapHeadroom.Strip"/>).</summary>
    public const float Height = MapHeadroom.Strip * 2;

    private const int CaptionSize = 10, DateSize = 18, LabelSize = 14, ValueSize = 17;

    /// <summary>The flags' CGs: <c>Prov.Owner + 819</c>, Leazas 820 (the map's markers).</summary>
    private const int LeazasFlag = 820;

    private readonly FacePictures _pictures;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel();
    private readonly FontVariation _date = Lacquer.Cinzel(700);
    private FontFile _gothic;
    private StatusStrip? _strip;
    private ImageTexture? _flag;
    private float _k = 1;                           // window pixels to a screen pixel
    private (Rect2 Tag, BuildingUnderWay Building)? _site;  // Construction's tag while a building is under way, as last drawn
    private bool _tip;                              // its tooltip is shown
    private bool _tipDown;                          // the touch mode: a press on the tag, not released or moved away yet
    private Vector2 _tipPress;                      // where it came down
    private int _tipPresses;                        // the presses counted: a timer acts only for its own

    private const int TipNameSize = 15, TipWeeksSize = 13;

    /// <summary>The touch mode's long press (the battle cards' and the table headers').</summary>
    private const double LongPressSeconds = 0.5;

    private const float LongPressSlop = 12;

    public StatusStripView(GameFont font, FacePictures pictures)
    {
        _pictures = pictures;
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        QueueRedraw();
    }

    /// <summary>Shows <paramref name="strip"/>, drawn again only when what it shows changed.</summary>
    public void Show(StatusStrip strip)
    {
        Visible = true;
        if (_strip?.Key == strip.Key)
            return;
        _strip = strip;
        _flag ??= _pictures.Picture(LeazasFlag) is { } image ? ImageTexture.CreateFromImage(image) : null;
        QueueRedraw();
    }

    public void Close()
    {
        Visible = false;
        _strip = null;
        _out = 0;
        _site = null;
        (_tip, _tipDown) = (false, false);
    }

    /// <summary>Where the events on the strip go but a left click's: the screen's input (a right click opens the menu as anywhere).</summary>
    public Action<InputEvent>? Forward { get; set; }

    /// <summary>
    /// How far the strip has slid up out of the picture as it goes (owner, 2026-10-27), 0 to 1 of its height; its parent
    /// clips it at the picture's top. While it slides it takes no input.
    /// </summary>
    public float Out
    {
        get => _out;
        set
        {
            if (Math.Abs(value - _out) < 0.0001f)
                return;
            _out = value;
            QueueRedraw();
        }
    }

    private float _out;

    public override bool _HasPoint(Vector2 point) => Visible && _out == 0 && new Rect2(0, 0, Width, Height).HasPoint(point / _k);

    /// <summary>A left click on the strip reaches no province or plate under it; the rest is the screen's.</summary>
    public override void _GuiInput(InputEvent e)
    {
        TipInput(e);
        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left })
            Forward?.Invoke(e);
        AcceptEvent();
    }

    /// <summary>Lays the strip in its band above the picture (<paramref name="area"/>, in its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        float k = area.Size.X / Width;
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_strip is not { } s)
            return;
        DrawSetTransform(new Vector2(0, -Height * _out * _k), 0, new Vector2(_k, _k));
        Ui.DrawPanel(this, new Rect2(0, 0, Width, Height), _k);
        // One row (owner, 2026-10-16): the flag, the status window's figures, the week's commands, the date.
        var row = new Rect2(0, 10, Width, 22);
        float cx = 18;
        if (_flag is not null && !s.Thieves)
        {
            DrawTextureRect(_flag, new Rect2(cx, row.GetCenter().Y - 9.5f, 24, 19), false);
            cx += 34;
        }
        foreach (var (label, value) in s.Status)
        {
            DrawString(_gothic, new Vector2(cx, Lacquer.Baseline(row, _gothic, LabelSize)), label, fontSize: LabelSize, modulate: Ui.Label);
            cx += _gothic.GetStringSize(label, fontSize: LabelSize).X + 8;
            DrawString(_gothic, new Vector2(cx, Lacquer.Baseline(row, _gothic, ValueSize)), value, fontSize: ValueSize, modulate: Ui.Value);
            cx += _gothic.GetStringSize(value, fontSize: ValueSize).X + 20;
        }
        // The week's commands as tags, grouped under brackets with their captions.
        cx += 6;
        _site = null;
        foreach (var group in s.Groups)
        {
            float start = cx;
            foreach (var use in group.Uses)
            {
                float cw = _gothic.GetStringSize(use.Text, fontSize: 13).X + 16;
                if (use.UnderWay is { } site)
                {
                    // A building under way (the owner's A, 2026-10-25): half-lit, the weeks left on a chip.
                    cw += UiPalette.ChipWidth(_gothic, site.WeeksLeft);
                    _site = (new Rect2(cx, row.Position.Y, cw, row.Size.Y), site);
                    Ui.DrawBusyTag(this, _gothic, _site.Value.Tag, use.Text, ColourOf(use.Action), _k, weeks: site.WeeksLeft);
                }
                else
                {
                    Ui.DrawTag(this, _gothic, new Rect2(cx, row.Position.Y, cw, row.Size.Y), use.Text, ColourOf(use.Action), use.Free, _k);
                }
                cx += cw + 4;
            }
            float end = cx - 4, by = row.End.Y + 6, mid = (start + end) / 2;
            DrawPolyline([new(start, by - 3), new(start, by), new(end, by), new(end, by - 3)], Ui.DimGold, Lacquer.Line(1, _k));
            float tw = _cinzel.GetStringSize(group.Caption, fontSize: CaptionSize).X;
            DrawRect(new Rect2(mid - tw / 2 - 5, by - 2, tw + 10, 4), Ui.Panel with { A = 1 });
            DrawString(_cinzel, new Vector2(mid - tw / 2, by + (_cinzel.GetAscent(CaptionSize) - _cinzel.GetDescent(CaptionSize)) / 2),
                group.Caption, fontSize: CaptionSize, modulate: Ui.Subtitle);
            cx += 22;
        }
        // The date at the right, large and bright (owner, 2026-10-16: the small dim one was lost on the lacquer);
        // smaller only when the row would not leave it room.
        int size = DateSize;
        float dw = _date.GetStringSize(s.Date, fontSize: size).X;
        while (size > 12 && Width - 20 - dw < cx)
            dw = _date.GetStringSize(s.Date, fontSize: --size).X;
        DrawString(_date, new Vector2(Width - 20 - dw, Lacquer.Baseline(new Rect2(0, 0, Width, Height), _date, size)), s.Date,
            fontSize: size, modulate: Ui.Name);
        if (_tip && _site is var (tag, building))
            DrawBuildingTip(tag, building);
    }

    /// <summary>
    /// The building under way's tooltip under its tag (the owner's A, 2026-10-25): its name and the weeks left on a lacquer
    /// plate in a gold frame with a dim gold one inside; no "n of m built" (owner). Drawn below the strip, over the map.
    /// </summary>
    private void DrawBuildingTip(Rect2 tag, BuildingUnderWay building)
    {
        string? name = building.Name;
        string weeks = building.WeeksText;
        float w = Math.Max(name is null ? 0 : _gothic.GetStringSize(name, fontSize: TipNameSize).X,
            _gothic.GetStringSize(weeks, fontSize: TipWeeksSize).X) + 24;
        float h = name is null ? 32 : 52;
        float x = Math.Min(tag.Position.X, Width - 8 - w), y = tag.End.Y + 22;
        var box = new Rect2(x, y, w, h);
        DrawRect(new Rect2(box.Position + new Vector2(3, 4), box.Size), new Color(0, 0, 0, 0.3f));
        DrawRect(box, Ui.CalloutFill);
        DrawRect(box, Ui.Gold, false, Lacquer.Line(1, _k));
        DrawRect(box.Grow(-3), Ui.DimGold, false, Lacquer.Line(1, _k));
        float top = y;
        if (name is not null)
        {
            DrawString(_gothic, new Vector2(x + 12, Lacquer.Baseline(new Rect2(x, y + 7, w, 20), _gothic, TipNameSize)), name,
                fontSize: TipNameSize, modulate: Ui.Name);
            top += 20;
        }
        DrawString(_gothic, new Vector2(x + 12, Lacquer.Baseline(new Rect2(x, top + 8, w, 16), _gothic, TipWeeksSize)), weeks,
            fontSize: TipWeeksSize, modulate: Ui.Quiet);
    }

    /// <summary>Shows or hides the building's tooltip (drawn only while a building is under way).</summary>
    private void SetTip(bool on)
    {
        if (on == _tip)
            return;
        _tip = on;
        QueueRedraw();
    }

    /// <summary>The building's tooltip while it is shown, else null (the screen gallery, unattended play).</summary>
    public string? BuildingTipShown => _tip && _site is var (_, building) ? $"{building.Name}|{building.WeeksText}" : null;

    /// <summary>Shows the building's tooltip as a hover would (the screen gallery: nothing moves the pointer).</summary>
    public void ShowBuildingTip() => SetTip(true);

    /// <summary>
    /// The pointer over Construction's tag while a building is under way shows its tooltip, at once, and leaving the tag
    /// hides it. The touch mode has no hover: a long press on the tag shows it, the next press anywhere hides it, as a
    /// table header's tip (<see cref="HeaderTip"/>).
    /// </summary>
    private void TipInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion when TouchMode.On:
                if (_tipDown && motion.Position.DistanceTo(_tipPress) > LongPressSlop)
                    _tipDown = false;
                break;
            case InputEventMouseMotion motion:
                SetTip(OnTag(motion.Position));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } button when TouchMode.On && OnTag(button.Position):
                (_tipDown, _tipPress) = (true, button.Position);
                int press = ++_tipPresses;
                GetTree().CreateTimer(LongPressSeconds).Timeout += () =>
                {
                    if (press == _tipPresses && _tipDown && IsVisibleInTree())
                        SetTip(true);                                   // no vibration (owner, 2026-10-24)
                };
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                _tipDown = false;
                break;
        }
    }

    private bool OnTag(Vector2 window) => _site is var (tag, _) && tag.HasPoint(window / _k);

    // The touch mode's tip goes at the next press anywhere (HeaderTip's).
    public override void _Input(InputEvent e)
    {
        if (_tip && TouchMode.On && e is InputEventMouseButton { Pressed: true })
        {
            _tipPresses++;
            SetTip(false);
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit && !TouchMode.On)
            SetTip(false);
    }

    /// <summary>A command's tag colour (ours; the same as the command panel's and the province menu's tags).</summary>
    private static Color ColourOf(WeekAction action) => action switch
    {
        WeekAction.Build => Color.Color8(46, 62, 104),
        WeekAction.Harem => Color.Color8(128, 44, 70),
        WeekAction.Trade => Color.Color8(36, 92, 96),
        WeekAction.City => Color.Color8(150, 52, 36),
        WeekAction.Attack => Color.Color8(128, 30, 30),
        WeekAction.Dungeon => Color.Color8(36, 92, 96),
        _ => Color.Color8(70, 96, 52),                                  // Subordinates, Troops, Talk
    };
}
