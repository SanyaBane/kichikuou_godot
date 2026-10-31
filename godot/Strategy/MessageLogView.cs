using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The message log (docs/ui/messages.md, §5 step 6; the owner's mock-up L3a): a paper chronicle in a lacquer frame over the
/// picture dimmed, the pages of the message windows as ruled lines — the lines close, the speaker's name in vermilion at a
/// run's first line, narration in a quieter ink, a place's title as a heading, a menu's answer under "CHOICE" — in the game's
/// font (Option → Display) at the script's own breaks. It opens at the newest line; the wheel scrolls, a right click, Escape or
/// the wheel down at the newest line closes it; in the touch mode a finger's drag scrolls and a tap off the frame closes it.
/// Laid out in the 1280×800 screen's pixels, scaled with the picture. On the phone held landscape (docs/ui/phone.md, §26,
/// the owner's LR2) the paper runs across the width at the scenes' type, from the frame's left edge to the right side's
/// room (<see cref="PhoneScenes.LogPaper"/>), over the whole screen dimmed, with Close in the Menu cell's place. Held upright
/// (§25, PR2) the paper covers the screen over the bar, each speaker's name on a line of its own over his lines
/// (<see cref="PhoneScenes.NamesOver"/>), the narration in smaller type, Close in Log's place in the bar.
/// </summary>
internal sealed partial class MessageLogView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Messages);

    private static readonly Color Dim = new(0, 0, 0, 0.55f);
    private static Color Rule => Ui.PaperRule with { A = 45 / 255f };   // paler than the ledger's (owner, 2026-10-19)
    private const int Step = 3;                                             // lines a wheel notch scrolls
    private const float PhoneBarIn = 12;                                    // the phone's scroll bar in from the paper's edge (reference pixels)
    private const float PcBarIn = 5;                                        // the PC's and the tablet's (layout pixels)

    /// <summary>
    /// The chronicle's figures in screen pixels: its lacquer frame (none on the phone: the paper alone) and paper, the
    /// head's height, the speakers' column's width, a line's pitch, and the type of the lines, the speakers, a choice's
    /// label, the head's title, its hint and a place's heading; <see cref="R"/> scales the PC's small offsets with them.
    /// </summary>
    private readonly record struct Metrics(Rect2 Frame, Rect2 Paper, float Head, float Margin, float Pitch, float Text,
        float Label, float Choice, float Title, float Hint, float Place, bool Framed, bool Stacked = false)
    {
        /// <summary>The PC's: the frame in the picture's middle.</summary>
        public static Metrics Pc { get; } = new(new Rect2(190, 28, 900, 744), new Rect2(208, 46, 864, 708), 58, 200, 34, 28, 18,
            15, 26, 14, 18, true);

        /// <summary>The phone's held landscape (LR2), in the reference phone's pixels made screen pixels.</summary>
        public static Metrics Phone(PhoneUnits u)
        {
            var paper = u.Rect(PhoneScenes.LogPaper(u.Frame));
            if (u.Frame.IsUpright)
                return new(paper, paper, u.Px(100), 0, u.Px(76), u.Px(58), u.Px(32), u.Px(30), u.Px(46), u.Px(24), u.Px(40), false, true);
            return new(paper, paper, u.Px(100), u.Px(420), u.Px(84), u.Px(58), u.Px(36), u.Px(30), u.Px(46), u.Px(24), u.Px(40), false);
        }

        /// <summary>The narration's type: upright smaller than a speech's (PR2: 2.5 mm against 3.3), else the same.</summary>
        public float Narration => Stacked ? Text * 44 / 58 : Text;

        /// <summary>The PC's small offsets scaled with the pitch.</summary>
        public float R => Pitch / 34;

        public float BodyTop => Paper.Position.Y + Head + 12 * R;

        /// <summary>The ruled lines the page holds.</summary>
        public int Rows => (int)((Paper.End.Y - BodyTop) / Pitch);
    }

    private readonly FontVariation _gothic;
    private readonly FontVariation _plain;
    private readonly FontVariation _head = Lacquer.Cinzel(700);
    private readonly FontVariation _label = Lacquer.Cinzel(700);
    private readonly FontVariation _hint = Lacquer.Cinzel(600);
    private readonly IReadOnlyList<LogLine> _all;
    private IReadOnlyList<LogLine> _lines;
    private Metrics _m = Metrics.Pc;
    private PhoneUnits? _phone;
    private int _top;
    private float _k = 1;
    private int _placedTurn = Device.Turns;

    /// <summary>The log closed.</summary>
    public event Action? Closed;

    public MessageLogView(GameFont font, MessageLog log)
    {
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 1000;                                      // over everything, the battle screen's layers (up to 20) included; Godot's limit is 4096
        var file = DeploymentTableView.LoadFont(font);
        _gothic = new FontVariation { BaseFont = file, VariationEmbolden = 0.6f };
        _plain = new FontVariation { BaseFont = file };
        _lines = _all = log.Lines();
        _top = Last;
    }

    private int Last => Math.Max(0, _lines.Count - _m.Rows);

    /// <summary>
    /// A point off the paper a tap closes the log at (global coordinates): left of the PC's frame, or in the phone's left
    /// side under its Close.
    /// </summary>
    public Vector2 OutsidePoint => GetGlobalTransformWithCanvas() * (_phone is { } u
        ? u.Frame.IsUpright ? u.Rect(PhoneScenes.LogClose(u.Frame)).GetCenter() * _k
            : new Vector2(u.Rect(u.Frame.LeftRoom).GetCenter().X, _m.Paper.GetCenter().Y) * _k
        : new Vector2(100, 400) * _k);

    /// <summary>
    /// Lays the log over the picture (<paramref name="area"/>, in its parent's coordinates); on the phone held landscape
    /// over the whole screen, its parent's area.
    /// </summary>
    public void Place(Rect2 area)
    {
        bool newest = _top >= Last;
        int first = ReferenceEquals(_lines, _all) ? _top : PhoneScenes.FromNamesOver(_all, _top);   // the reading place in the log's own lines
        if (_placedTurn != Device.Turns)
        {
            _placedTurn = Device.Turns;
            _finger = null;                                 // a turn: the finger's drag was the old layout's
        }
        if (Device.Held(this) is { } phone)
        {
            var u = new PhoneUnits(phone);
            _phone = u;
            _m = Metrics.Phone(u);
            _lines = _m.Stacked ? PhoneScenes.NamesOver(_all) : _all;
            Position = Vector2.Zero;
            Size = GetParentAreaSize();
            _k = u.K;
        }
        else
        {
            _phone = null;
            _m = Metrics.Pc;
            _lines = _all;
            Position = area.Position;
            Size = area.Size;
            _k = area.Size.X / 1280f;
        }
        _top = newest ? Last : Math.Min(ReferenceEquals(_lines, _all) ? first : PhoneScenes.ToNamesOver(_all, first), Last);
        QueueRedraw();
    }

    /// <summary>Escape: closes it.</summary>
    public void Close()
    {
        Closed?.Invoke();
        QueueFree();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (TouchMode.On && Touch(@event))
        {
            AcceptEvent();
            return;
        }
        if (@event is InputEventMouseButton { Pressed: true } button)
        {
            switch (button.ButtonIndex)
            {
                case MouseButton.WheelUp:
                    Scroll(-Step);
                    break;
                case MouseButton.WheelDown when _top >= Last:
                case MouseButton.Right:
                    Close();
                    break;
                case MouseButton.WheelDown:
                    Scroll(Step);
                    break;
            }
        }
        AcceptEvent();
    }

    private Vector2? _finger;           // the touch mode: where the finger came down, its drag's start
    private int _fingerTop;             // the first line shown then
    private bool _dragged;              // it moved further than a tap

    /// <summary>
    /// The touch mode (docs/ui/touch.md, "The Skip and Log buttons"): a finger dragged over the page scrolls it, a tap off
    /// the frame closes it (on the phone off the paper: its Close among them); the wheel and the right click (Android's
    /// Back) as before. Returns whether the event was taken.
    /// </summary>
    private bool Touch(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                _finger = press.Position;
                _fingerTop = _top;
                _dragged = false;
                return true;
            case InputEventMouseMotion motion when _finger is { } start:
                float dy = (start.Y - motion.Position.Y) / _k;
                _dragged |= Math.Abs(dy) > 12;
                if (_dragged)
                    Scroll(_fingerTop + (int)Math.Round(dy / _m.Pitch) - _top);
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release when _finger is not null:
                _finger = null;
                if (!_dragged && !_m.Frame.HasPoint(release.Position / _k))
                    Close();
                return true;
        }
        return false;
    }

    private void Scroll(int lines)
    {
        int top = Math.Clamp(_top + lines, 0, Last);
        if (top == _top)
            return;
        _top = top;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float k = _k;
        var m = _m;
        float r = m.R;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(k, k));
        DrawRect(new Rect2(Vector2.Zero, Size / k), Dim);
        if (m.Framed)
            Ui.DrawPanel(this, m.Frame, k);
        var paper = m.Paper;
        DrawRect(paper, Ui.Paper);
        DrawRect(new Rect2(paper.Position, paper.Size.X, m.Head), Ui.PaperHead);
        if (!m.Framed)
            DrawRect(paper, Ui.Gold, false, Lacquer.Line(2.5f * r, k));
        Text(_head, "CHRONICLE", m.Title, paper.Position.X + 24 * r, paper.Position.Y + m.Head * 39 / 58, Ui.HeadInk);
        string hint = _phone is not null ? "DRAG SCROLLS   ·   CLOSE SHUTS IT"
            : TouchMode.On ? "DRAG SCROLLS   ·   TAP OUTSIDE CLOSES" : "WHEEL SCROLLS   ·   RIGHT CLICK CLOSES";
        Text(_hint, hint, m.Hint, paper.End.X - 24 * r - Width(_hint, hint, m.Hint), paper.Position.Y + m.Head * 34 / 58, Ui.InkQuiet);

        float margin = paper.Position.X + m.Margin;
        if (!m.Stacked)
            DrawLine(new Vector2(margin, paper.Position.Y + m.Head), new Vector2(margin, paper.End.Y), Ui.VermilionInk with { A = 110 / 255f },
                Lacquer.Line(1, k));
        // The reference scroll bar (the owner's S2 in PB's vermilion, docs/ui/phone.md §43; on the PC and the tablet round
        // T's T2, docs/ui/touch.md), the rules ending its gap before it.
        var groove = _phone is { } p
            ? p.Rect(PhoneScrollBar.Groove(p.Frame, (paper.End.X - p.Px(PhoneBarIn)) * p.K, m.BodyTop * p.K,
                (paper.End.Y - 8 * r - m.BodyTop) * p.K))
            : R(LayoutScrollBar.Groove(paper.End.X - PcBarIn, m.BodyTop, paper.End.Y - 8 * r - m.BodyTop));
        float rulesEnd = groove.Position.X - (_phone is { } q ? q.Px(PhoneScrollBar.Gap) : LayoutScrollBar.Gap);
        for (float y = m.BodyTop + m.Pitch - 4 * r; y < paper.End.Y; y += m.Pitch)
            DrawLine(new Vector2(paper.Position.X + 8 * r, y), new Vector2(rulesEnd, y), Rule, Lacquer.Line(1, k));

        float column = margin + 18 * r, right = paper.End.X - 24 * r;
        for (int i = 0; i < m.Rows && _top + i < _lines.Count; i++)
        {
            var line = _lines[_top + i];
            float top = m.BodyTop + i * m.Pitch;
            if (line.Kind == LogKind.Place)
            {
                string place = line.Text.ToUpperInvariant();
                float size = Fit(_head, place, m.Place, right - column);
                Text(_head, place, size, (column + right - Width(_head, place, size)) / 2, top + 24 * r, Ui.HeadInk);
                continue;
            }
            if (line.Label.Length > 0)
            {
                bool choice = line.Kind == LogKind.Choice;
                string label = line.Label.ToUpperInvariant();
                var colour = choice ? Ui.InkQuiet
                    : line.Kind == LogKind.Thought ? Ui.VermilionInk with { A = 0.6f } : Ui.VermilionInk;
                if (m.Stacked)
                {
                    // Upright the name on a line of its own over the lines it heads (PR2), at their left edge.
                    string over = choice ? "YOUR CHOICE" : label;
                    float at = Fit(_label, over, choice ? m.Choice : m.Label, right - column);
                    Text(_label, over, at, column, top + m.Pitch * 0.62f, colour);
                    continue;
                }
                float size = Fit(_label, label, choice ? m.Choice : m.Label, m.Margin - 32 * r);
                Text(_label, label, size, margin - 16 * r - Width(_label, label, size), top + 26 * r, colour);
            }
            var (font, text, ink) = line.Kind switch
            {
                LogKind.Choice when line.Chosen => (_gothic, "◆ " + line.Text, Ui.VermilionInk),
                LogKind.Choice => (_plain, "◇ " + line.Text, Ui.InkGrey),
                LogKind.Narration => (_plain, line.Text, Ui.InkQuiet),
                LogKind.Thought => (_gothic, line.Text, Ui.InkQuiet),
                _ => (_gothic, line.Text, Ui.Ink),
            };
            float textSize = Fit(font, text, line.Kind == LogKind.Narration ? m.Narration : m.Text, right - column);   // 131's lines run up to 52 cells
            Text(font, text, textSize, column, top + 2 * r + font.GetAscent((int)textSize), ink);
        }

        // the scroll bar: the thumb the part shown
        Rect2? thumb = null;
        if (_lines.Count > m.Rows)
        {
            float shown = (float)m.Rows / _lines.Count, at = (float)_top / Last;
            thumb = _phone is { } phone
                ? phone.Rect(PhoneScrollBar.Thumb(phone.Frame, new PhoneFrame.Box(groove.Position.X * phone.K, groove.Position.Y * phone.K,
                    groove.Size.X * phone.K, groove.Size.Y * phone.K), shown, at))
                : R(LayoutScrollBar.Thumb(new PhoneFrame.Box(groove.Position.X, groove.Position.Y, groove.Size.X, groove.Size.Y), shown, at));
        }
        Ui.DrawScrollBar(this, groove, thumb, _phone is { } f ? f.Px(PhoneScrollBar.Rim) : Lacquer.WindowPixel(k), paper: true);
        if (_phone is { } u)
            SideCells.Draw(this, u, u.Rect(PhoneScenes.LogClose(u.Frame)), SideCells.Icon.Close, "Close", lit: true);

    }

    private static Rect2 R(PhoneFrame.Box b) => new(b.X, b.Y, b.W, b.H);

    private static float Width(Font font, string text, float size) =>
        font.GetStringSize(text, HorizontalAlignment.Left, -1, (int)Math.Round(size)).X;

    /// <summary>The size up to <paramref name="size"/> at which <paramref name="text"/> fits <paramref name="width"/> (screen pixels).</summary>
    private static float Fit(Font font, string text, float size, float width)
    {
        float w = Width(font, text, size);
        return w <= width ? size : Math.Max(8, size * width / w);
    }

    /// <summary>Draws <paramref name="text"/> with its baseline at (x, baseline), in screen pixels.</summary>
    private void Text(Font font, string text, float size, float x, float baseline, Color colour) =>
        DrawString(font, new Vector2(x, baseline), text, HorizontalAlignment.Left, -1, (int)Math.Round(size), colour);
}
