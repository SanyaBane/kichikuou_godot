using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own Harem screen (docs/ui/pickers.md, the owner's mock-up H2): over the whole screen while <c>Sﾊｰﾚﾑ</c> is open —
/// the wave backdrop; a title band (the screen's name, the date); the chosen girl's picture large at
/// the left, in the original screen's gold frame, with her name, her visits and the date she was last summoned under it
/// and "Summon" in red lacquer; at the right the picker's grid, each girl at her own number's cell and the others' cells empty
/// (the owner, 2026-10-19: as the original), End under it. A click on a tablet only chooses her (the owner, 2026-10-19);
/// Summon summons. The marks (owner's decision 30, docs/ui/talk-marks.md §6, H): a tablet's right slot NEW in green or
/// ??? in amber, nothing when her visit is read (her visits stay in the panel); the panel's "This visit" in words; Summon's
/// pill at its top right, none when read. Drawn by hand in the 1280×800 screen's pixels (<see cref="Lacquer"/>). It decides nothing: the
/// presses go to <see cref="GameScreen"/>, which clicks the picker's cell or its End (<see cref="HaremRoster"/>). On the phone
/// held landscape the screen takes the whole screen, its grid folded after its ninth row (docs/ui/phone-build.md, step L7, LH2);
/// held upright her picture stands at the width over the grid scrolled by a finger (step P7, PH2).
/// </summary>
internal sealed partial class HaremView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    // In the 1280×800 screen's pixels, after the mock-up.
    private static readonly Rect2 TitlePanel = new(16, 16, 1248, 56);
    private static readonly Rect2 PictureArea = new(16, 88, 666, 416);
    private static readonly Rect2 InfoPanel = new(16, 516, 666, 268);
    private static readonly Rect2 SummonArea = new(34, 668, 630, 98);
    private static readonly Rect2 GridPanel = new(690, 88, 574, 696);
    private static readonly Rect2 EndArea = new(1106, 724, 140, 42);
    private const float TabletW = 176, TabletH = 32, TabletGap = 4, GridX = 708, GridY = 106;
    private const int Columns = 3, Cells = 50;          // every girl at her own number's cell, as Sﾊｰﾚﾑ's grid; the rest empty
    private static readonly Color Plum = Color.Color8(128, 44, 70), PlumDim = Color.Color8(90, 60, 66);

    /// <summary>Every button's word on the PC and the tablet, Summon's and End's: one size (docs/ui/style.md, "Buttons side by side").</summary>
    private const int ButtonText = 17;
    private const int NoButton = -1, SummonButton = -2, EndButton = -3;    // a tablet is its girl's number

    private readonly FacePictures _faces;
    private readonly Layer _backdrop, _face;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(700);
    private FontFile _gothic;
    private Vector2 _origin;
    private float _k = 1;
    private IReadOnlyList<HaremGirl> _girls = [];
    private string _key = "", _date = "";
    private int _chosen, _pictureKey = -1, _pictureCg;

    /// <summary>
    /// Her picture greyed out with its CG's number, as the agent's dumps mask an event CG (<c>GamePresenter.MaskEventCgs</c>;
    /// owner, 2026-11-04: the harem's pictures are as explicit): on in a screenshot run and in the screen gallery.
    /// </summary>
    public bool MaskPicture { get; set; }
    private ImageTexture? _picture;
    private int _hovered = NoButton, _pressed = NoButton;

    /// <summary>Summon pressed for the girl chosen.</summary>
    public event Action<int>? SummonPressed;

    /// <summary>End pressed, or a right press on the screen.</summary>
    public event Action? EndPressed;

    public HaremView(GameFont font, FacePictures faces)
    {
        _faces = faces;
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        _backdrop = new Layer(c =>
        {
            c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
            Ui.DrawWaveBackdrop(c, _k, _phone is null ? null : c.Size / _k);      // the phone's: the whole screen
        }) { ClipContents = true };
        AddChild(_backdrop);
        _face = new Layer(DrawScreen);
        AddChild(_face);
    }

    /// <summary>The girl chosen (her number), 0 with nobody on offer.</summary>
    public int Chosen => _chosen;

    /// <summary>Whether the buttons take presses (not while a summon's click is on its way).</summary>
    public bool Busy { get; set; }

    /// <summary>Option → Display.</summary>
    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        _face.QueueRedraw();
    }

    /// <summary>
    /// Covers the screen whose script pixel (0, 0) is at <paramref name="origin"/>, <paramref name="pixelsPerScript"/> screen
    /// pixels to a script pixel; on the phone, held either way, the whole screen in its frame's layout pixels (LH2, PH2).
    /// </summary>
    public void Place(Vector2 origin, float pixelsPerScript)
    {
        Position = Vector2.Zero;
        Size = GetParentAreaSize();
        var phone = Device.Held(this);
        float k = phone?.Scale ?? pixelsPerScript / 2;
        if (phone is not null)
            origin = Vector2.Zero;
        var area = phone is not null ? Size : new Vector2(1280, 800) * k;
        if (origin == _origin && Math.Abs(k - _k) < 0.0001f && _backdrop.Size == area && phone == _phone)
            return;
        _origin = origin;
        _k = k;
        _phone = phone;
        foreach (var layer in new[] { _backdrop, _face })
        {
            layer.Position = origin;
            layer.Size = area;
            layer.QueueRedraw();
        }
    }

    /// <summary>The girls and the chosen one's picture from <paramref name="state"/>; the choice kept while she is on offer (<see cref="HaremRoster.FirstChoice"/>).</summary>
    public void Refresh(GameState state, IReadOnlyList<HaremGirl> girls)
    {
        _chosen = HaremRoster.FirstChoice(state, girls, _chosen);
        _date = HaremRoster.DateText(state.Week.Value) ?? "";
        var (screen, empty) = HaremRoster.Background(state);
        var girl = girls.FirstOrDefault(g => g.Number == _chosen);
        int picture = girl?.PictureCg ?? 0;
        _pictureCg = picture;
        if (picture * 10000 + screen != _pictureKey)
        {
            _pictureKey = picture * 10000 + screen;
            _picture = _faces.PicturePanel(picture, screen, empty);
        }
        string key = $"{string.Join(",", girls)}|{_chosen}|{_date}|{Busy}";
        if (key == _key)
            return;
        _key = key;
        _girls = girls;
        _face.QueueRedraw();
    }

    /// <summary>Summon as the mouse presses it (unattended play goes through here).</summary>
    public void Summon(int girl)
    {
        _chosen = girl;
        SummonPressed?.Invoke(girl);
    }

    // ---------------------------------------------------------------- the buttons

    /// <summary>Girl <paramref name="number"/>'s cell: the picker's grid, left to right, top to bottom.</summary>
    private static Rect2 TabletRect(int number) =>
        new(GridX + (number - 1) % Columns * (TabletW + TabletGap), GridY + (number - 1) / Columns * (TabletH + TabletGap), TabletW, TabletH);

    private int ButtonAt(Vector2 local)
    {
        if (_phone is { } phone)
            return phone.IsPortrait ? PortraitButtonAt(phone, local) : PhoneButtonAt(phone, local);
        var at = (local - _origin) / _k;
        if (SummonArea.HasPoint(at))
            return SummonButton;
        if (EndArea.HasPoint(at))
            return EndButton;
        foreach (var girl in _girls)
            if (TabletRect(girl.Number).HasPoint(at))
                return girl.Number;
        return NoButton;
    }

    private bool Enabled(int button) => !Busy && (button != SummonButton || _chosen != 0);

    public override void _GuiInput(InputEvent e)
    {
        if (_phone is { IsPortrait: true } && GridTouch(e))
        {
            AcceptEvent();
            return;
        }
        switch (e)
        {
            case InputEventMouseMotion motion:
                Hover(ButtonAt(motion.Position));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                if (!Busy)
                    EndPressed?.Invoke();
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
                int button = ButtonAt(click.Position);
                if (click.Pressed)
                    _pressed = button != NoButton && Enabled(button) ? button : NoButton;
                else
                {
                    if (button == _pressed && button != NoButton && Enabled(button))
                        Click(button);
                    _pressed = NoButton;
                }
                _face.QueueRedraw();
                AcceptEvent();
                break;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit)
            Hover(NoButton);
    }

    private void Hover(int button)
    {
        if (button == _hovered)
            return;
        _hovered = button;
        _face.QueueRedraw();
    }

    private void Click(int button)
    {
        if (button == EndButton)
            EndPressed?.Invoke();
        else if (button == SummonButton)
            SummonPressed?.Invoke(_chosen);
        else
        {
            _chosen = button;                               // a tablet only chooses her
            _key = "";
        }
    }

    // ---------------------------------------------------------------- drawing

    private float Line(float width) => Lacquer.Line(width, _k);

    private void DrawScreen(Control c)
    {
        c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        if (_phone is { } phone)
        {
            if (phone.IsPortrait)
                DrawPortrait(c, new PhoneUnits(phone));
            else
                DrawPhone(c, new PhoneUnits(phone));
            return;
        }
        DrawTitle(c);
        DrawPicture(c, PictureArea);
        DrawInfo(c);
        DrawGrid(c);
    }

    private void DrawTitle(Control c)
    {
        Ui.DrawPanel(c, TitlePanel, _k);
        var band = new Rect2(TitlePanel.Position.X + 22, TitlePanel.Position.Y + 11, TitlePanel.Size.X - 44, 34);
        const string title = "Harem";
        c.DrawString(_brush, new Vector2(band.Position.X, Lacquer.Baseline(band, _brush, 24)), title, fontSize: 24, modulate: Ui.Name);
        float dw = _cinzel.GetStringSize(_date, fontSize: 16).X;
        c.DrawString(_cinzel, new Vector2(band.End.X - dw, Lacquer.Baseline(band, _cinzel, 16) + 1), _date, fontSize: 16, modulate: Ui.Name);
    }

    /// <summary>Her picture as the picker's panel shows it (the gold frame is the original's), with a shadow.</summary>
    private void DrawPicture(Control c, Rect2 p)
    {
        for (int i = 10; i >= 1; i--)
            c.DrawRect(new Rect2(p.Position.X - i + 3, p.Position.Y - i + 8, p.Size.X + 2 * i, p.Size.Y + 2 * i), new Color(0, 0, 0, 0.06f));
        if (MaskPicture && _pictureCg != 0)
        {
            c.DrawRect(p, Color.Color8(96, 96, 96));                    // the presenter's mask: grey, the CG's number
            int size = Math.Max(10, (int)(p.Size.Y / 14));
            c.DrawString(_gothic, new Vector2(p.Position.X + size, p.Position.Y + size + _gothic.GetAscent(size)), $"CG {_pictureCg} (masked)",
                fontSize: size, modulate: Colors.White);
        }
        else if (_picture is not null)
            PanelDescriptions.Draw(c, _picture, p);
        else
            c.DrawRect(p, Colors.Black);
    }

    /// <summary>Her name, visits, the last date and what this visit may bring (decision 30); Summon with her mark's pill.</summary>
    private void DrawInfo(Control c)
    {
        Ui.DrawPanel(c, InfoPanel, _k);
        if (_girls.FirstOrDefault(g => g.Number == _chosen) is not { } girl)
            return;
        float x = InfoPanel.Position.X + 26, y = InfoPanel.Position.Y + 40;
        DrawHeart(c, new Vector2(x + 2, y), 11, Plum);
        c.DrawString(_brush, new Vector2(x + 22, y + 12), girl.Name, fontSize: 32, modulate: Ui.Name);
        // The columns one after another at their widest values, so they never overlap and stay put from girl to girl.
        float last = x + StatWidth("Visits", "None yet") + StatGap, visitX = last + StatWidth("Last summoned", "LP 9 · Month 12 · Week 4") + StatGap;
        DrawStat(c, x, InfoPanel.Position.Y + 88, "Visits", girl.Visits == 0 ? "None yet" : girl.Visits.ToString());
        DrawStat(c, last, InfoPanel.Position.Y + 88, "Last summoned", HaremRoster.DateText(girl.LastWeek) ?? "Never");
        if (HaremRoster.VisitText(girl.Mark) is { } visit)
            DrawStat(c, visitX, InfoPanel.Position.Y + 88, "This visit", visit, MarkColour(girl.Mark) ?? Ui.Quiet);
        bool on = Enabled(SummonButton), hover = on && _hovered == SummonButton;
        Ui.DrawRedButton(c, SummonArea, hover, hover && _pressed == SummonButton, _k);
        CentredString(c, _cinzel, $"Summon {girl.Name}", ButtonText, Color.Color8(250, 226, 160), SummonArea.GetCenter().X, Lacquer.Baseline(SummonArea, _cinzel, ButtonText));
        if (PickerMarks.Label(girl.Mark) is { } pill)
            Ui.DrawPill(c, _cinzel, SummonArea.End.X - 12, SummonArea.Position.Y + 23, pill, MarkColour(girl.Mark)!.Value, 14, 22);
    }

    /// <summary>A mark's colour: NEW green, ??? amber; null when read or unknown.</summary>
    private static Color? MarkColour(SceneMark? mark) => mark switch
    {
        SceneMark.New => Ui.MarkNew,
        SceneMark.Maybe => Ui.MarkMaybe,
        _ => null,
    };

    private const float StatGap = 32;

    /// <summary>A stat's column in the font chosen: its label or its widest value.</summary>
    private float StatWidth(string label, string widest) =>
        Math.Max(_cinzel.GetStringSize(label, fontSize: 11).X, _gothic.GetStringSize(widest, fontSize: 20).X);

    private void DrawStat(Control c, float x, float y, string label, string value, Color? colour = null)
    {
        c.DrawString(_cinzel, new Vector2(x, y), label, fontSize: 11, modulate: Ui.Label);
        c.DrawString(_gothic, new Vector2(x, y + 26), value, fontSize: 20, modulate: colour ?? Ui.Value);
    }

    /// <summary>
    /// The picker's grid: every girl at her own number's cell, three to a row (<c>Sﾊｰﾚﾑ</c>'s 3×17, its last cell unused), the
    /// cells of the girls not on offer empty; a girl's tablet a heart, her name and her visit's mark (decision 30: NEW or
    /// ???, nothing when read), the chosen one lit in gold; End.
    /// </summary>
    private void DrawGrid(Control c)
    {
        Ui.DrawPanel(c, GridPanel, _k);
        var offered = _girls.ToDictionary(g => g.Number);
        for (int n = 1; n <= Cells; n++)
        {
            var r = TabletRect(n);
            if (!offered.TryGetValue(n, out var girl))
            {
                c.DrawRect(r, new Color(Ui.OffButton, 0.55f));
                c.DrawRect(r, new Color(Ui.OffLine, 0.6f), false, Line(1));
                continue;
            }
            bool chosen = n == _chosen, hover = n == _hovered;
            c.DrawRect(r, chosen || hover ? Ui.RowHover : Ui.Row);
            c.DrawRect(r, chosen ? Ui.HoverLine : hover ? Ui.Gold : Ui.DimGold, false, Line(chosen ? 2 : 1));
            float mid = r.Position.Y + r.Size.Y / 2;
            DrawHeart(c, new Vector2(r.Position.X + 14, mid - 1), 6, girl.Visits > 0 ? Plum : PlumDim);
            c.DrawString(_brush, new Vector2(r.Position.X + 26, mid + 6), girl.Name, fontSize: 15,
                modulate: chosen || hover ? Ui.Name : Ui.Text);
            if (PickerMarks.Label(girl.Mark) is { } mark)
            {
                float mw = _cinzel.GetStringSize(mark, fontSize: 12).X;
                c.DrawString(_cinzel, new Vector2(r.End.X - 10 - mw, mid + 5), mark, fontSize: 12, modulate: MarkColour(girl.Mark)!.Value);
            }
        }
        bool hoverEnd = !Busy && _hovered == EndButton;
        c.DrawRect(EndArea, hoverEnd ? Ui.RowHover : Ui.Row);
        c.DrawRect(EndArea, hoverEnd ? Ui.HoverLine : Ui.DimGold, false, Line(hoverEnd ? 2 : 1));
        CentredString(c, _cinzel, "End", ButtonText, hoverEnd ? Ui.Name : Ui.Text, EndArea.GetCenter().X, Lacquer.Baseline(EndArea, _cinzel, ButtonText));
    }

    /// <summary>A small heart, drawn: two circles over a point.</summary>
    private static void DrawHeart(Control c, Vector2 at, float r, Color colour)
    {
        c.DrawCircle(at + new Vector2(-r / 2, -r * 0.1f), r / 2, colour);
        c.DrawCircle(at + new Vector2(r / 2, -r * 0.1f), r / 2, colour);
        c.DrawColoredPolygon([at + new Vector2(-r * 0.97f, 0), at + new Vector2(r * 0.97f, 0), at + new Vector2(0, r * 1.05f)], colour);
    }

    private static void CentredString(Control c, Font font, string text, int size, Color colour, float centre, float baseline)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(centre - w / 2, baseline), text, fontSize: size, modulate: colour);
    }

    /// <summary>A layer of the screen drawn by its view.</summary>
    private sealed partial class Layer : Control
    {
        private readonly Action<Control> _paint;

        public Layer(Action<Control> paint)
        {
            _paint = paint;
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Draw() => _paint(this);
    }
}
