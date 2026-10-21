using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our message windows (docs/ui/messages.md, variant A, steps 1-2; our interface): the original's windows
/// 111/112/121/122/113/123 and 131 where they are, as lacquer panels of their kind — speech with a tail at its portrait, a
/// thought rounded with a dashed frame and a trail of bubbles, narration in a warmer lacquer, a place's title in Cinzel, 131
/// a bar under the event CG with its speaker on a plate — the text at the cells the engine placed it in (131's moved left
/// by the name split off), in the game's font (Option → Fonts), bold as the surface draws it, and the
/// click's triangle while the script waits for the key. A menu open over the window (<see cref="Kichikuou.Original.Content.Strategy.GamePresenter.MenuArea"/>)
/// is shown again on top, cut from the screen's picture. It takes no input: the screen's own key wait answers it.
/// The text types letter by letter at <see cref="LettersPerSecond"/> (step 4, the owner's mock-up T1: the letters
/// cut off at the next cell, the triangle once the text is whole); text the script adds to a window goes on from where it
/// was. Laid out in the 1280×800 screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class MessageWindowView : Control
{
    private static readonly Color SpeechFill = Lacquer.Panel with { A = 1 };
    private static readonly Color ThoughtFill = Color.Color8(30, 26, 30);
    internal static readonly Color NarrationFill = Color.Color8(52, 38, 24);
    internal static readonly Color NarrationText = Color.Color8(226, 204, 150);

    /// <summary>Synthetic bold, as <see cref="GodotGlyphRenderer"/> makes the surface's text.</summary>
    private const float Embolden = 0.6f;

    private readonly FontVariation _title = Lacquer.Cinzel(600);
    private readonly FontVariation _room = Lacquer.Cinzel(500);
    private FontVariation _gothic = null!;
    private MessageWindow? _window;
    private bool _waiting;
    private Texture2D? _screen;
    private (int X, int Y, int W, int H)? _menu;
    private float _k = 1;                           // window pixels to a screen pixel
    private double _typed;                          // the glyphs typed so far, a fraction of the next one included

    public MessageWindowView(GameFont font)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        SetFont(font);
    }

    /// <summary>Option → Fonts changed the game's font.</summary>
    public void SetFont(GameFont font)
    {
        _gothic = new FontVariation { BaseFont = DeploymentTableView.LoadFont(font), VariationEmbolden = Embolden };
        QueueRedraw();
    }

    /// <summary>The window drawn now, or null.</summary>
    public MessageWindow? Window => _window;

    /// <summary>Whether a speech's tail is drawn (not in Option → Text Speed…'s preview, which has no portrait).</summary>
    public bool Tail { get; init; } = true;

    /// <summary>How fast the text types (Option → Text Speed…); infinity shows it whole at once.</summary>
    public double LettersPerSecond { get; set; } = double.PositiveInfinity;

    /// <summary>Whether the window's text is still typing.</summary>
    public bool Typing => _window is { } w && _typed < w.Glyphs.Count;

    /// <summary>Shows the whole text at once (a click while it types, the message skip, a menu over it).</summary>
    public void Complete()
    {
        if (!Typing)
            return;
        _typed = _window!.Glyphs.Count;
        QueueRedraw();
    }

    /// <summary>Types the text again from its start (the preview of Option → Text Speed…).</summary>
    public void Restart()
    {
        _typed = 0;
        QueueRedraw();
    }

    /// <summary>
    /// Shows <paramref name="window"/> (hidden when null), the triangle while <paramref name="waiting"/>, and the menu at
    /// <paramref name="menu"/> (screen pixels) cut from <paramref name="screen"/>, the screen's picture, over it.
    /// </summary>
    public void Show(MessageWindow? window, bool waiting, Texture2D? screen, (int X, int Y, int W, int H)? menu)
    {
        Visible = window is not null;
        if (window is null)
        {
            _window = null;
            return;
        }
        if (window.SameAs(_window) && waiting == _waiting && menu == _menu && screen == _screen)
            return;
        if (!window.SameAs(_window))
            _typed = window.Kind == MessageKind.PlaceTitle || double.IsPositiveInfinity(LettersPerSecond)
                ? window.Glyphs.Count
                : Continued(_window, window, _typed);
        _window = window;
        _waiting = waiting;
        _screen = screen;
        _menu = menu;
        QueueRedraw();
    }

    /// <summary>The screen's fade (0 none, 255 whole): the panels fade with it.</summary>
    public void Fade(byte amount) => Modulate = new Color(1, 1, 1, 1 - amount / 255f);

    /// <summary>Lays the window over the picture (<paramref name="area"/>, in its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        float k = area.Size.X / 1280f;
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        QueueRedraw();
    }

    /// <summary>
    /// Where <paramref name="next"/> goes on typing: from the glyphs <paramref name="shown"/> typed of <paramref name="before"/>
    /// when it is the same window with text added (its glyphs begin with those), else from its start.
    /// </summary>
    private static double Continued(MessageWindow? before, MessageWindow next, double shown)
    {
        if (before is null || before.Kind != next.Kind || before.Panel != next.Panel || before.Speaker != next.Speaker)
            return 0;
        int kept = Math.Min((int)shown, before.Glyphs.Count);
        if (kept > next.Glyphs.Count)
            return 0;
        for (int i = 0; i < kept; i++)
            if (before.Glyphs[i] != next.Glyphs[i])
                return 0;
        return kept;
    }

    /// <summary>The text types on; the triangle and the menu's hover move: drawn again every frame while any of them shows.</summary>
    public override void _Process(double delta)
    {
        if (!Visible)
            return;
        if (Typing)
        {
            int count = _window!.Glyphs.Count;
            _typed = double.IsPositiveInfinity(LettersPerSecond) ? count : Math.Min(count, _typed + delta * LettersPerSecond);
            QueueRedraw();
        }
        else if (_waiting || _menu is not null)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_window is not { } w)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        var (x, y, width, height) = w.Panel;
        var box = new Rect2(x, y, width, height);
        Color text = Lacquer.Text;
        switch (w.Kind)
        {
            case MessageKind.SpeechLeft or MessageKind.SpeechRight:
                Lacquer.DrawPanel(this, box, _k, SpeechFill);
                if (Tail)
                    SpeechTail(box, w.AtLeft);
                break;
            case MessageKind.ThoughtLeft or MessageKind.ThoughtRight:
                ThoughtPanel(box, w.AtLeft);
                text = Lacquer.Quiet;
                break;
            case MessageKind.CgBar:
                Lacquer.DrawPanel(this, box, _k, SpeechFill);
                if (w.Speaker.Length > 0)
                    NameTab(new Vector2(x + 32, y - 26), w.Speaker, w.Thinks);
                text = w.Speaker.Length == 0 ? NarrationText : w.Thinks ? Lacquer.Quiet : Lacquer.Text;
                break;
            default:
                Lacquer.DrawPanel(this, box, _k, NarrationFill);
                text = NarrationText;
                break;
        }
        if (w.Kind == MessageKind.PlaceTitle)
            PlaceTitle(box, w.Title, w.Subtitle);
        else
            foreach (var g in w.Glyphs.Take((int)_typed))
                DrawString(_gothic, new Vector2(g.X, g.Y + _gothic.GetAscent(g.Size)), g.C.ToString(), fontSize: g.Size,
                    modulate: g.Colour is { } c ? Color.Color8(c.R, c.G, c.B) : text);
        if (_waiting && !Typing)
            Lacquer.DrawTriangle(this, new Vector2(box.End.X - 30, box.End.Y - 28), _k);
        if (_menu is { } m && _screen is not null)
        {
            var area = new Rect2(m.X, m.Y, m.W, m.H);
            DrawTextureRectRegion(_screen, area, area);
        }
    }

    /// <summary>
    /// A speech's tail out of the panel's side towards its portrait: lacquer over the frame where it leaves the panel, gold
    /// along its two edges (the mock-ups' numbers: 111 between y+96 and y+142, its point at (x−30, y+104); 121 between y+150
    /// and y+196, its point at (x+w+30, y+188)).
    /// </summary>
    private void SpeechTail(Rect2 box, bool left)
    {
        float edge = left ? box.Position.X : box.End.X, inward = left ? 7 : -7, y = box.Position.Y;
        var (y0, y1, tip) = left
            ? (y + 96, y + 142, new Vector2(edge - 30, y + 104))
            : (y + 150, y + 196, new Vector2(edge + 30, y + 188));
        DrawColoredPolygon([new(edge + inward, y0 + 2), new(edge + inward, y1 - 2), tip], SpeechFill);
        DrawColoredPolygon([new(edge, y0), new(edge, y1), tip], SpeechFill);
        DrawLine(new Vector2(edge, y0), tip, Lacquer.Gold, Lacquer.Line(2, _k), true);
        DrawLine(new Vector2(edge, y1), tip, Lacquer.Gold, Lacquer.Line(2, _k), true);
    }

    /// <summary>
    /// A thought: corners rounded 18, a dimmer lacquer, the frame gold dashes 14 on 8 without the corners' diamonds, bubbles of
    /// radius 12, 8 and 5 towards the thinker (down to the left portrait from 112, up to the right one from 122).
    /// </summary>
    private void ThoughtPanel(Rect2 box, bool left)
    {
        const float r = 18;
        var style = new StyleBoxFlat
        {
            BgColor = ThoughtFill,
            ShadowColor = new Color(0, 0, 0, 0.3f),
            ShadowSize = 6,
            ShadowOffset = new Vector2(2, 6),
            AntiAliasing = true,
        };
        style.SetCornerRadiusAll((int)r);
        DrawStyleBox(style, box);
        float x = box.Position.X, y = box.Position.Y, w = box.Size.X, h = box.Size.Y, line = Lacquer.Line(2, _k);
        Dashes(new(x + r, y + 1), new(x + w - r, y + 1), line);
        Dashes(new(x + r, y + h - 1), new(x + w - r, y + h - 1), line);
        Dashes(new(x + 1, y + r), new(x + 1, y + h - r), line);
        Dashes(new(x + w - 1, y + r), new(x + w - 1, y + h - r), line);
        foreach (var (cx, cy, from) in new[] { (x + r, y + r, Math.PI), (x + w - r, y + r, Math.PI * 1.5), (x + w - r, y + h - r, 0.0), (x + r, y + h - r, Math.PI * 0.5) })
            DrawArc(new Vector2(cx, cy), r - 1, (float)from, (float)(from + Math.PI / 2), 12, Lacquer.Gold, line, true);
        var bubbles = left
            ? new[] { (x - 12, y + 128, 12f), (x - 34, y + 150, 8f), (x - 50, y + 166, 5f) }
            : new[] { (x + w + 12, y + 150, 12f), (x + w + 34, y + 128, 8f), (x + w + 50, y + 112, 5f) };
        foreach (var (bx, by, br) in bubbles)
        {
            DrawCircle(new Vector2(bx, by), br, ThoughtFill with { A = 240 / 255f });
            DrawArc(new Vector2(bx, by), br, 0, Mathf.Tau, 24, Lacquer.Gold, line, true);
        }
    }

    /// <summary>
    /// 131's speaker on a plate over the bar's top edge, half over the picture's foot (mock-ups A-7, A-8): the name in Cinzel
    /// 600 at 30, capitals; a speech's plate a lacquer panel, a thought's rounded with a dashed frame and its name quieter.
    /// </summary>
    private void NameTab(Vector2 at, string name, bool thought)
    {
        const int size = 30;
        string shown = name.ToUpperInvariant();
        float textWidth = _title.GetStringSize(shown, fontSize: size).X;
        var tab = new Rect2(at, textWidth + 64, 46);
        if (thought)
        {
            const float r = 12;
            var style = new StyleBoxFlat { BgColor = ThoughtFill, AntiAliasing = true };
            style.SetCornerRadiusAll((int)r);
            DrawStyleBox(style, tab);
            float x = tab.Position.X, y = tab.Position.Y, w = tab.Size.X, h = tab.Size.Y, line = Lacquer.Line(2, _k);
            Dashes(new(x + r, y + 1), new(x + w - r, y + 1), line, 10, 6);
            Dashes(new(x + r, y + h - 1), new(x + w - r, y + h - 1), line, 10, 6);
            foreach (var (cx, cy, from) in new[] { (x + r, y + r, Math.PI), (x + w - r, y + r, Math.PI * 1.5), (x + w - r, y + h - r, 0.0), (x + r, y + h - r, Math.PI * 0.5) })
                DrawArc(new Vector2(cx, cy), r - 1, (float)from, (float)(from + Math.PI / 2), 8, Lacquer.Gold, line, true);
        }
        else
        {
            Lacquer.DrawPanel(this, tab, _k, SpeechFill);
        }
        Centred(_title, shown, size, tab.GetCenter().X, Lacquer.Baseline(tab, _title, size), thought ? Lacquer.Quiet : Lacquer.Name);
    }

    private void Dashes(Vector2 a, Vector2 b, float width, float dash = 14, float gap = 8)
    {
        float length = a.DistanceTo(b);
        var step = (b - a) / length;
        for (float t = 0; t < length; t += dash + gap)
            DrawLine(a + step * t, a + step * Math.Min(t + dash, length), Lacquer.Gold, width);
    }

    /// <summary>A place's title (113 at <c>ZM 32</c>): the place in Cinzel 600 at 50, a divider and the room in Cinzel 500 at 34 under it.</summary>
    private void PlaceTitle(Rect2 box, string title, string room)
    {
        float cx = box.GetCenter().X, y = box.Position.Y;
        float titleBaseline = room.Length > 0 ? y + 92 : y + box.Size.Y / 2 + 18;
        Centred(_title, title.ToUpperInvariant(), 50, cx, titleBaseline, Lacquer.Name);
        if (room.Length == 0)
            return;
        Lacquer.DrawDivider(this, box.Position.X + 160, box.End.X - 160, y + 128, _k);
        Centred(_room, room, 34, cx, y + 186, Lacquer.Subtitle);
    }

    private void Centred(Font font, string text, int size, float cx, float baseline, Color colour)
    {
        float tw = font.GetStringSize(text, fontSize: size).X;
        DrawString(font, new Vector2(cx - tw / 2, baseline), text, fontSize: size, modulate: colour);
    }
}
