using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our message windows (docs/ui/messages.md, variant A, steps 1-2; our interface): the original's windows
/// 111/112/121/122/113/123 and 131 where they are, as lacquer panels of their kind — speech with a tail at its portrait, a
/// thought rounded with a dashed frame and a trail of bubbles, narration in a warmer lacquer, a place's title in Cinzel, 131
/// a bar under the event CG with its speaker on a plate — the text at the cells the engine placed it in (131's moved left
/// by the name split off), in the game's font (Option → Display), bold as the surface draws it, and the
/// click's triangle while the script waits for the key. A menu open over the window (<see cref="Kichikuou.Original.Content.Strategy.GamePresenter.MenuArea"/>)
/// is shown again on top, cut from the screen's picture. It takes no input: the screen's own key wait answers it.
/// The text types letter by letter at <see cref="LettersPerSecond"/> (step 4, the owner's mock-up T1: the letters
/// cut off at the next cell, the triangle once the text is whole); text the script adds to a window goes on from where it
/// was. Laid out in the 1280×800 screen's pixels, scaled with the picture. On the phone held upright (docs/ui/phone.md,
/// §18, PS2, PS2l, PS2b; docs/ui/phone-build.md, step P1) the window leaves the picture: across the screen above it (the
/// left portrait's speech or thought) or under it (every other), no name plate but 131's, the script's lines at their
/// breaks in larger type (<see cref="PhoneScenes.Window"/>), the tail or the bubbles at the portrait.
/// </summary>
internal sealed partial class MessageWindowView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Messages);

    private static Color SpeechFill => Ui.Panel with { A = 1 };
    private static Color ThoughtFill => Ui.ThoughtFill;
    internal static Color NarrationFill => Ui.NarrationFill;
    internal static Color NarrationText => Ui.NarrationText;
    private static Color DockBand => Ui.DockBand;

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
    private PhoneFrame? _portrait;                   // the phone held upright: the window over or under the picture
    private double _typed;                          // the glyphs typed so far, a fraction of the next one included

    public MessageWindowView(GameFont font)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        SetFont(font);
    }

    /// <summary>Option → Display changed the game's font.</summary>
    public void SetFont(GameFont font)
    {
        _gothic = new FontVariation { BaseFont = DeploymentTableView.LoadFont(font), VariationEmbolden = Embolden };
        QueueRedraw();
    }

    /// <summary>The window drawn now, or null.</summary>
    public MessageWindow? Window => _window;

    /// <summary>Whether a speech's tail is drawn (not in Option → Messages's preview, which has no portrait).</summary>
    public bool Tail { get; init; } = true;

    /// <summary>Whether held upright the window leaves the picture (not Option → Messages's preview, which stays in its sheet).</summary>
    public bool Leaves { get; init; } = true;

    /// <summary>
    /// Whether the left and the right portrait show (<c>Ui_PortraitL</c>, <c>Ui_PortraitR</c>): held upright the left one's
    /// window stands above the picture only while it shows, and a tail points only at a portrait that does.
    /// </summary>
    public (bool Left, bool Right) Portraits
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            QueueRedraw();
        }
    } = (true, true);

    /// <summary>
    /// The touch mode's Skip and Log at the screen's left edge (<see cref="SkipDrawer"/>): a window that would cover them
    /// (131) is drawn smaller right of them (the owner's B3b, docs/ui/touch.md). Not on the phone held landscape, where they
    /// stand in the black side (docs/ui/phone.md, §19, LS1b).
    /// </summary>
    public bool Docks { get; init; }

    /// <summary>How fast the text types (Option → Messages); infinity shows it whole at once.</summary>
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

    /// <summary>Types the text again from its start (the preview of Option → Messages).</summary>
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
        var portrait = Leaves ? Device.Portrait(this) : null;
        if (portrait is { } f)
            area = new Rect2(Vector2.Zero, GetParentAreaSize());        // over the whole screen: the window leaves the picture
        float k = portrait?.Scale ?? area.Size.X / 1280f;
        bool turned = portrait != _portrait;
        _portrait = portrait;
        if (!turned && Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f)
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
        if (_portrait is { } phone)
        {
            DrawPortrait(phone, w);
            return;
        }
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        var (x, y, width, height) = w.Panel;
        var box = new Rect2(x, y, width, height);
        float shrink = Docks && Device.Layout != DeviceLayout.PhoneLandscape ? SkipButtons.WindowScale(w.Panel) : 1;
        if (shrink < 1)
        {
            // The original's window stays drawn under ours (GamePresenter.LeaveWindowOnStage): a dark band hides it where
            // ours no longer is, Skip and Log stand on it, and the window is drawn about its top right corner, smaller.
            DrawRect(box, DockBand);
            var corner = new Vector2(box.End.X, box.Position.Y);
            DrawSetTransform(corner * (1 - shrink) * _k, 0, new Vector2(_k * shrink, _k * shrink));
        }
        Color text = Ui.Text;
        switch (w.Kind)
        {
            case MessageKind.SpeechLeft or MessageKind.SpeechRight:
                Ui.DrawPanel(this, box, _k, SpeechFill);
                if (Tail)
                    SpeechTail(box, w.AtLeft);
                break;
            case MessageKind.ThoughtLeft or MessageKind.ThoughtRight:
                ThoughtPanel(box, w.AtLeft);
                text = Ui.Quiet;
                break;
            case MessageKind.CgBar:
                Ui.DrawPanel(this, box, _k, SpeechFill);
                if (w.Speaker.Length > 0)
                    NameTab(new Vector2(x + 32, y - 26), w.Speaker, w.Thinks);
                text = w.Speaker.Length == 0 ? NarrationText : w.Thinks ? Ui.Quiet : Ui.Text;
                break;
            default:
                Ui.DrawPanel(this, box, _k, NarrationFill);
                text = NarrationText;
                break;
        }
        if (w.Read && ReadTextChoice.Colour is { } read)
            text = ReadTextChoice.Of(read, text);                       // the already-read text's colour (docs/ui/read-text.md)
        if (w.Kind == MessageKind.PlaceTitle)
            PlaceTitle(box, w.Title, w.Subtitle);
        else
            foreach (var g in w.Glyphs.Take((int)_typed))
                DrawString(_gothic, new Vector2(g.X, g.Y + _gothic.GetAscent(g.Size)), g.C.ToString(), fontSize: g.Size,
                    modulate: g.Colour is { } c ? Color.Color8(c.R, c.G, c.B) : text);
        if (_waiting && !Typing)
            Ui.DrawTriangle(this, new Vector2(box.End.X - 30, box.End.Y - 28), _k);
        if (_menu is { } m && _screen is not null)
        {
            DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));     // the menu at its own place, not the window's
            var area = new Rect2(m.X, m.Y, m.W, m.H);
            DrawTextureRectRegion(_screen, area, area);
        }
    }

    /// <summary>
    /// The window held upright (PS2, PS2l, PS2b): its panel across the screen over or under the picture, in the frame's
    /// layout pixels; the speech's tail or the thought's bubbles out of the edge nearest the picture, at the portrait; 131's
    /// speaker on its plate, larger; the glyphs at their cells from the text box's corner at the window's own type; the
    /// triangle; a menu over the picture at its place. 131's band at the picture's foot, which our panel no longer covers,
    /// is hidden.
    /// </summary>
    private void DrawPortrait(PhoneFrame f, MessageWindow w)
    {
        var u = new PhoneUnits(f);
        var place = PhoneScenes.Window(f, w.Kind, w.TextRect, Portraits);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        if (w.Kind == MessageKind.CgBar)
        {
            var (bx, by, bw, bh) = w.Panel;                             // the original's bar, left over the picture
            DrawRect(new Rect2(f.Frame.X / u.K + bx, f.Frame.Y / u.K + by, bw, bh), Colors.Black);
        }
        var box = u.Rect(place.Panel);
        float? pointer = Tail && place.PointerX is { } px ? px / u.K : null;
        Color text = Ui.Text;
        switch (w.Kind)
        {
            case MessageKind.SpeechLeft or MessageKind.SpeechRight:
                Ui.DrawPanel(this, box, u.K, SpeechFill);
                if (pointer is { } sx)
                    PortraitTail(u, box, sx, place.Above);
                break;
            case MessageKind.ThoughtLeft or MessageKind.ThoughtRight:
                ThoughtPanel(box, w.AtLeft, sideBubbles: false);
                if (pointer is { } tx)
                    PortraitBubbles(u, box, tx, place.Above);
                text = Ui.Quiet;
                break;
            case MessageKind.CgBar:
                Ui.DrawPanel(this, box, u.K, SpeechFill);
                text = w.Speaker.Length == 0 ? NarrationText : w.Thinks ? Ui.Quiet : Ui.Text;
                break;
            default:
                Ui.DrawPanel(this, box, u.K, NarrationFill);
                text = NarrationText;
                break;
        }
        if (w.Read && ReadTextChoice.Colour is { } read)
            text = ReadTextChoice.Of(read, text);
        if (w.Kind == MessageKind.CgBar && w.Speaker.Length > 0)
        {
            const float plate = 1.35f;                                  // the mock-up's plate (PS2b), larger than the PC's
            var at = new Vector2(box.Position.X + u.Px(40), box.Position.Y - u.Px(34));
            DrawSetTransform(at * u.K, 0, new Vector2(u.K * plate, u.K * plate));
            NameTab(Vector2.Zero, w.Speaker, w.Thinks);
        }
        if (w.Kind == MessageKind.PlaceTitle)
        {
            // The PC's title box (784 × 258) scaled into the panel, centred.
            float t = Math.Min(box.Size.X / 784, box.Size.Y / 258);
            var origin = box.GetCenter() - new Vector2(784, 258) * t / 2;
            DrawSetTransform(origin * u.K, 0, new Vector2(u.K * t, u.K * t));
            PlaceTitle(new Rect2(0, 0, 784, 258), w.Title, w.Subtitle);
        }
        else
        {
            // The glyphs at their cells, from the text box's corner, at the window's type: drawn at that size, unscaled.
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            var (rx, ry, _, _) = w.TextRect;
            foreach (var g in w.Glyphs.Take((int)_typed))
            {
                int size = Math.Max(1, (int)Math.Round(g.Size * place.Scale));
                var at = new Vector2(place.TextX + (g.X - rx) * place.Scale, place.TextY + (g.Y - ry) * place.RowScale);
                DrawString(_gothic, at + new Vector2(0, _gothic.GetAscent(size)), g.C.ToString(), fontSize: size,
                    modulate: g.Colour is { } c ? Color.Color8(c.R, c.G, c.B) : text);
            }
        }
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        if (_waiting && !Typing)
            Ui.DrawTriangle(this, new Vector2(box.End.X - u.Px(50), box.End.Y - u.Px(44)), u.K);
        if (_menu is { } m && _screen is not null)
        {
            DrawSetTransform(new Vector2(f.Frame.X, f.Frame.Y), 0, new Vector2(f.Scale, f.Scale));   // the menu at its place in the picture
            var area = new Rect2(m.X, m.Y, m.W, m.H);
            DrawTextureRectRegion(_screen, area, area);
        }
    }

    /// <summary>A speech's tail held upright: out of the panel's edge nearest the picture, at <paramref name="x"/> (layout pixels), down or up to the portrait.</summary>
    private void PortraitTail(PhoneUnits u, Rect2 box, float x, bool above)
    {
        float edge = above ? box.End.Y : box.Position.Y, way = above ? 1 : -1, inward = -way * u.Px(7);
        var tip = new Vector2(x - u.Px(10), edge + way * u.Px(44));
        float x0 = x - u.Px(30), x1 = x + u.Px(30);
        DrawColoredPolygon([new(x0 + 2, edge + inward), new(x1 - 2, edge + inward), tip], SpeechFill);
        DrawColoredPolygon([new(x0, edge), new(x1, edge), tip], SpeechFill);
        DrawLine(new Vector2(x0, edge), tip, Ui.Gold, Lacquer.Line(2, u.K), true);
        DrawLine(new Vector2(x1, edge), tip, Ui.Gold, Lacquer.Line(2, u.K), true);
    }

    /// <summary>A thought's bubbles held upright: out of the panel's edge nearest the picture, towards the thinker at <paramref name="x"/>.</summary>
    private void PortraitBubbles(PhoneUnits u, Rect2 box, float x, bool above)
    {
        float edge = above ? box.End.Y : box.Position.Y, way = above ? 1 : -1, line = Lacquer.Line(2, u.K);
        foreach (var (dx, dy, r) in new[] { (0f, 22f, 17f), (-16f, 54f, 11f), (-28f, 78f, 7f) })
        {
            var c = new Vector2(x + u.Px(dx), edge + way * u.Px(dy));
            DrawCircle(c, u.Px(r), ThoughtFill with { A = 240 / 255f });
            DrawArc(c, u.Px(r), 0, Mathf.Tau, 24, Ui.Gold, line, true);
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
        DrawLine(new Vector2(edge, y0), tip, Ui.Gold, Lacquer.Line(2, _k), true);
        DrawLine(new Vector2(edge, y1), tip, Ui.Gold, Lacquer.Line(2, _k), true);
    }

    /// <summary>
    /// A thought: corners rounded 18, a dimmer lacquer, the frame gold dashes 14 on 8 without the corners' diamonds, bubbles of
    /// radius 12, 8 and 5 towards the thinker (down to the left portrait from 112, up to the right one from 122).
    /// </summary>
    private void ThoughtPanel(Rect2 box, bool left, bool sideBubbles = true)
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
            DrawArc(new Vector2(cx, cy), r - 1, (float)from, (float)(from + Math.PI / 2), 12, Ui.Gold, line, true);
        if (!sideBubbles)
            return;
        var bubbles = left
            ? new[] { (x - 12, y + 128, 12f), (x - 34, y + 150, 8f), (x - 50, y + 166, 5f) }
            : new[] { (x + w + 12, y + 150, 12f), (x + w + 34, y + 128, 8f), (x + w + 50, y + 112, 5f) };

        foreach (var (bx, by, br) in bubbles)
        {
            DrawCircle(new Vector2(bx, by), br, ThoughtFill with { A = 240 / 255f });
            DrawArc(new Vector2(bx, by), br, 0, Mathf.Tau, 24, Ui.Gold, line, true);
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
                DrawArc(new Vector2(cx, cy), r - 1, (float)from, (float)(from + Math.PI / 2), 8, Ui.Gold, line, true);
        }
        else
        {
            Ui.DrawPanel(this, tab, _k, SpeechFill);
        }
        Centred(_title, shown, size, tab.GetCenter().X, Lacquer.Baseline(tab, _title, size), thought ? Ui.Quiet : Ui.Name);
    }

    private void Dashes(Vector2 a, Vector2 b, float width, float dash = 14, float gap = 8)
    {
        float length = a.DistanceTo(b);
        var step = (b - a) / length;
        for (float t = 0; t < length; t += dash + gap)
            DrawLine(a + step * t, a + step * Math.Min(t + dash, length), Ui.Gold, width);
    }

    /// <summary>A place's title (113 at <c>ZM 32</c>): the place in Cinzel 600 at 50, a divider and the room in Cinzel 500 at 34 under it.</summary>
    private void PlaceTitle(Rect2 box, string title, string room)
    {
        float cx = box.GetCenter().X, y = box.Position.Y;
        float titleBaseline = room.Length > 0 ? y + 92 : y + box.Size.Y / 2 + 18;
        Centred(_title, title.ToUpperInvariant(), 50, cx, titleBaseline, Ui.Name);
        if (room.Length == 0)
            return;
        Ui.DrawDivider(this, box.Position.X + 160, box.End.X - 160, y + 128, _k);
        Centred(_room, room, 34, cx, y + 186, Ui.Subtitle);
    }

    private void Centred(Font font, string text, int size, float cx, float baseline, Color colour)
    {
        float tw = font.GetStringSize(text, fontSize: size).X;
        DrawString(font, new Vector2(cx - tw / 2, baseline), text, fontSize: size, modulate: colour);
    }
}
