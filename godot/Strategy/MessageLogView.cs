using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The message log (docs/ui/messages.md, §5 step 6; the owner's mock-up L3a): a paper chronicle in a lacquer frame over the
/// picture dimmed, the pages of the message windows as ruled lines — the lines close, the speaker's name in vermilion at a
/// run's first line, narration in a quieter ink, a place's title as a heading, a menu's answer under "CHOICE" — in the game's
/// font (Option → Display) at the script's own breaks. It opens at the newest line; the wheel scrolls, a right click, Escape or
/// the wheel down at the newest line closes it; in the touch mode a finger's drag scrolls and a tap off the frame closes it.
/// Laid out in the 1280×800 screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class MessageLogView : Control
{
    private static readonly Rect2 Frame = new(190, 28, 900, 744);
    private static readonly Color Dim = new(0, 0, 0, 0.55f);
    private static readonly Color Rule = Color.Color8(150, 120, 80, 45);   // paler than the ledger's (owner, 2026-10-19)
    private const float Pitch = 34, TextSize = 28, MarginX = 200, HeadHeight = 58, Inset = 18;
    private const int Step = 3;                                             // lines a wheel notch scrolls

    private readonly FontVariation _gothic;
    private readonly FontVariation _plain;
    private readonly FontVariation _head = Lacquer.Cinzel(700);
    private readonly FontVariation _label = Lacquer.Cinzel(700);
    private readonly FontVariation _hint = Lacquer.Cinzel(600);
    private readonly IReadOnlyList<LogLine> _lines;
    private int _top;
    private float _k = 1;

    /// <summary>The log closed.</summary>
    public event Action? Closed;

    public MessageLogView(GameFont font, MessageLog log)
    {
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 1000;                                      // over everything, the battle screen's layers (up to 20) included; Godot's limit is 4096
        var file = DeploymentTableView.LoadFont(font);
        _gothic = new FontVariation { BaseFont = file, VariationEmbolden = 0.6f };
        _plain = new FontVariation { BaseFont = file };
        _lines = log.Lines();
        _top = Math.Max(0, _lines.Count - Rows);
    }

    private static Rect2 Paper => new(Frame.Position + new Vector2(Inset, Inset), Frame.Size - new Vector2(2 * Inset, 2 * Inset));

    private static float BodyTop => Paper.Position.Y + HeadHeight + 12;

    /// <summary>The ruled lines the page holds.</summary>
    private static int Rows => (int)((Paper.End.Y - BodyTop) / Pitch);

    private int Last => Math.Max(0, _lines.Count - Rows);

    /// <summary>Lays the log over the picture (<paramref name="area"/>, in its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        Position = area.Position;
        Size = area.Size;
        _k = area.Size.X / 1280f;
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
    /// the frame closes it; the wheel and the right click (Android's Back) as before. Returns whether the event was taken.
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
                    Scroll(_fingerTop + (int)Math.Round(dy / Pitch) - _top);
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release when _finger is not null:
                _finger = null;
                if (!_dragged && !Frame.HasPoint(release.Position / _k))
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
        DrawSetTransform(Vector2.Zero, 0, new Vector2(k, k));
        DrawRect(new Rect2(Vector2.Zero, Size / k), Dim);
        Lacquer.DrawPanel(this, Frame, k);
        var paper = Paper;
        DrawRect(paper, Lacquer.Paper);
        DrawRect(new Rect2(paper.Position, paper.Size.X, HeadHeight), Lacquer.PaperHead);
        Text(_head, "CHRONICLE", 26, paper.Position.X + 24, paper.Position.Y + 39, Lacquer.HeadInk);
        string hint = TouchMode.On ? "DRAG SCROLLS   ·   TAP OUTSIDE CLOSES" : "WHEEL SCROLLS   ·   RIGHT CLICK CLOSES";
        Text(_hint, hint, 14, paper.End.X - 24 - Width(_hint, hint, 14), paper.Position.Y + 34, Lacquer.InkQuiet);

        float margin = paper.Position.X + MarginX;
        DrawLine(new Vector2(margin, paper.Position.Y + HeadHeight), new Vector2(margin, paper.End.Y), Lacquer.VermilionInk with { A = 110 / 255f },
            Lacquer.Line(1, k));
        for (float y = BodyTop + Pitch - 4; y < paper.End.Y; y += Pitch)
            DrawLine(new Vector2(paper.Position.X + 8, y), new Vector2(paper.End.X - 8, y), Rule, Lacquer.Line(1, k));

        float column = margin + 18, right = paper.End.X - 24;
        for (int i = 0; i < Rows && _top + i < _lines.Count; i++)
        {
            var line = _lines[_top + i];
            float top = BodyTop + i * Pitch;
            if (line.Kind == LogKind.Place)
            {
                string place = line.Text.ToUpperInvariant();
                float size = Fit(_head, place, 18, right - column);
                Text(_head, place, size, (column + right - Width(_head, place, size)) / 2, top + 24, Lacquer.HeadInk);
                continue;
            }
            if (line.Label.Length > 0)
            {
                bool choice = line.Kind == LogKind.Choice;
                string label = line.Label.ToUpperInvariant();
                float size = Fit(_label, label, choice ? 15 : 18, MarginX - 32);
                var colour = choice ? Lacquer.InkQuiet
                    : line.Kind == LogKind.Thought ? Lacquer.VermilionInk with { A = 0.6f } : Lacquer.VermilionInk;
                Text(_label, label, size, margin - 16 - Width(_label, label, size), top + 26, colour);
            }
            var (font, text, ink) = line.Kind switch
            {
                LogKind.Choice when line.Chosen => (_gothic, "◆ " + line.Text, Lacquer.VermilionInk),
                LogKind.Choice => (_plain, "◇ " + line.Text, Lacquer.InkGrey),
                LogKind.Narration => (_plain, line.Text, Lacquer.InkQuiet),
                LogKind.Thought => (_gothic, line.Text, Lacquer.InkQuiet),
                _ => (_gothic, line.Text, Lacquer.Ink),
            };
            float textSize = Fit(font, text, TextSize, right - column);            // 131's lines run up to 52 cells
            Text(font, text, textSize, column, top + 2 + font.GetAscent((int)textSize), ink);
        }

        // the scroll bar: the track at the paper's right edge, the thumb the part shown
        var track = new Rect2(paper.End.X - 10, BodyTop, 4, paper.End.Y - 8 - BodyTop);
        DrawRect(track, Lacquer.PaperHead);
        if (_lines.Count > Rows)
        {
            float h = Math.Max(24, track.Size.Y * Rows / _lines.Count);
            float y = track.Position.Y + (track.Size.Y - h) * _top / Last;
            DrawRect(new Rect2(track.Position.X - 1, y, 6, h), Lacquer.VermilionInk with { A = 0.7f });
        }
    }

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
