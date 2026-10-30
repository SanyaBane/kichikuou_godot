using Godot;
using Parts = Kichikuou.Game.Strategy.Lacquer;    // UiPalette.Lacquer is the palette

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The lacquer style's parts in a palette's colours: the panel with its double gold frame and corners, a row's box, a tag,
/// a divider, the screens' backdrop, the main button, the seal and the badge. Sizes are in the 1280×800 screen's pixels;
/// <c>k</c> is the window pixels to a screen pixel. The parts without colours of their own are <see cref="Parts"/>'s.
/// </summary>
internal sealed partial record UiPalette
{
    /// <summary>The panel: its shadow, the lacquer (<see cref="Panel"/> unless <paramref name="fill"/>), the double gold frame and its corners.</summary>
    public void DrawPanel(CanvasItem c, Rect2 box, float k, Color? fill = null)
    {
        float x = box.Position.X, y = box.Position.Y, w = box.Size.X, h = box.Size.Y;
        for (int i = 6; i >= 1; i--)
            c.DrawRect(new Rect2(x - i + 2, y - i + 6, w + 2 * i, h + 2 * i), new Color(0, 0, 0, 0.06f));
        c.DrawRect(box, fill ?? Panel);
        c.DrawRect(new Rect2(x + 1, y + 1, w - 2, h - 2), Gold, false, Parts.Line(2, k));
        c.DrawRect(new Rect2(x + 5.5f, y + 5.5f, w - 11, h - 11), DimGold, false, Parts.Line(1, k));
        foreach (var (cx, cy, sx, sy) in new[] { (x, y, 1, 1), (x + w, y, -1, 1), (x, y + h, 1, -1), (x + w, y + h, -1, -1) })
        {
            c.DrawPolyline([new(cx + sx * 9, cy + sy * 2), new(cx + sx * 9, cy + sy * 9), new(cx + sx * 2, cy + sy * 9)], Gold, Parts.Line(2, k));
            c.DrawColoredPolygon([new(cx + sx * 9, cy + sy * 14), new(cx + sx * 14, cy + sy * 9), new(cx + sx * 9, cy + sy * 4), new(cx + sx * 4, cy + sy * 9)], Gold);
        }
    }

    /// <summary>
    /// A whole screen's backdrop (our Subordinates and deployment screens): dark lacquer under a faint gold wave pattern
    /// (seigaiha), darkened towards the edges; drawn in the 1280×800 screen's pixels, or over <paramref name="size"/> of
    /// them (the phone's whole screen, docs/ui/phone-build.md, step L5).
    /// </summary>
    public void DrawWaveBackdrop(CanvasItem c, float k, Vector2? size = null)
    {
        var lacquer = Backdrop;
        float width = size?.X ?? 1280, height = size?.Y ?? 800;
        c.DrawRect(new Rect2(0, 0, width, height), lacquer);
        const float R = 34;
        var wave = Gold with { A = 0.05f };
        for (int row = -1; row * R / 2 < height + R; row++)
        {
            float y = row * R / 2, off = row % 2 == 0 ? 0 : R;
            for (float x = -2 * R + off; x < width + 2 * R; x += 2 * R)
            {
                var at = new Vector2(x, y);
                c.DrawCircle(at, R, lacquer);
                foreach (float r in (float[])[R, R * 0.72f, R * 0.44f])
                    c.DrawArc(at, r, 0, Mathf.Tau, 40, wave, Parts.Line(1, k), true);
            }
        }
        const float Inset = 220;
        var edge = new Color(0, 0, 0, 0.7f);
        var clear = new Color(0, 0, 0, 0);
        Vector2[] outer = [new(0, 0), new(width, 0), new(width, height), new(0, height)];
        Vector2[] inner = [new(Inset, Inset * 0.62f), new(width - Inset, Inset * 0.62f), new(width - Inset, height - Inset * 0.62f), new(Inset, height - Inset * 0.62f)];
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            c.DrawPolygon([outer[i], outer[j], inner[j], inner[i]], [edge, edge, clear, clear]);
        }
    }

    /// <summary>A main button's face: red lacquer framed in gold, as Turn END on the command panel; lit when hovered, sunk when pressed.</summary>
    public void DrawRedButton(CanvasItem c, Rect2 rect, bool hover, bool down, float k)
    {
        Color top = down ? Color.Color8(96, 22, 16) : hover ? Color.Color8(150, 40, 30) : Color.Color8(128, 32, 24);
        Color bottom = down ? Color.Color8(70, 14, 10) : hover ? Color.Color8(96, 20, 14) : Color.Color8(80, 16, 12);
        c.DrawPolygon([rect.Position, new(rect.End.X, rect.Position.Y), rect.End, new(rect.Position.X, rect.End.Y)], [top, top, bottom, bottom]);
        c.DrawRect(rect, hover ? HoverLine : Gold, false, Parts.Frame(2, k));
        c.DrawRect(rect.Grow(-4), Color.Color8(150, 96, 52), false, Parts.Line(1, k));
    }

    /// <summary>
    /// A button that loses something (Delete, Abandon City): dark red lacquer in a vermilion frame, lit when hovered; its
    /// label <see cref="DangerText"/>, <see cref="Name"/> when hovered, after an icon of its own.
    /// </summary>
    public void DrawDangerButton(CanvasItem c, Rect2 rect, bool hover, float k)
    {
        c.DrawRect(rect, hover ? DangerHover : DangerFill);
        c.DrawRect(rect, Vermilion, false, Parts.Frame(2, k));
        c.DrawRect(rect.Grow(-4), DangerLine, false, Parts.Line(1, k));
    }

    /// <summary>A row's box: dark with a dim frame, or lit with a bright one when hovered.</summary>
    public void DrawRowBox(CanvasItem c, Rect2 rect, bool hover, float k)
    {
        c.DrawRect(rect, hover ? RowHover : Row);
        c.DrawRect(rect, hover ? HoverLine : DimGold, false, Parts.Frame(hover ? 2 : 1, k));
    }

    /// <summary>A command's tag: its colour framed in gold, or grey when the command is off.</summary>
    public void DrawTag(CanvasItem c, Font font, Rect2 rect, string tag, Color colour, bool on, float k, int size = 13)
    {
        c.DrawRect(rect, on ? colour : OffTag);
        if (on)
            c.DrawRect(rect, Gold, false, Parts.Line(1, k));
        float tw = font.GetStringSize(tag, fontSize: size).X;
        c.DrawString(font, new Vector2(rect.GetCenter().X - tw / 2, Parts.Baseline(rect, font, size)), tag, fontSize: size, modulate: on ? Text : OffText);
    }

    /// <summary>
    /// A command's tag while its work is under way (Construction's building, the owner's A, 2026-10-25): its colour halfway
    /// to the spent grey in a dim gold frame, the word quiet; with <paramref name="weeks"/> a chip at its right end, the
    /// weeks left in a gold frame (<see cref="ChipWidth"/> wider than the plain tag; its word centred in the rest).
    /// </summary>
    public void DrawBusyTag(CanvasItem c, Font font, Rect2 rect, string tag, Color colour, float k, int size = 13, int? weeks = null)
    {
        c.DrawRect(rect, colour.Lerp(OffTag, 0.55f));
        c.DrawRect(rect, DimGold, false, Parts.Line(1, k));
        float chip = weeks is { } w ? ChipWidth(font, w, size) : 0;
        float tw = font.GetStringSize(tag, fontSize: size).X;
        c.DrawString(font, new Vector2(rect.Position.X + (rect.Size.X - chip - tw) / 2, Parts.Baseline(rect, font, size)), tag, fontSize: size,
            modulate: Quiet);
        if (weeks is not { } left)
            return;
        string n = left.ToString();
        float nw = font.GetStringSize(n, fontSize: size - 1).X;
        var box = new Rect2(rect.End.X - (nw + 8) - 3, rect.Position.Y + 3, nw + 8, rect.Size.Y - 6);
        c.DrawRect(box, BadgeFill);
        c.DrawRect(box, Gold, false, Parts.Line(1, k));
        c.DrawString(font, new Vector2(box.GetCenter().X - nw / 2, Parts.Baseline(box, font, size - 1)), n, fontSize: size - 1, modulate: Name);
    }

    /// <summary>How much wider <see cref="DrawBusyTag"/>'s chip of <paramref name="weeks"/> makes a tag of <paramref name="size"/>.</summary>
    public static float ChipWidth(Font font, int weeks, int size = 13) => font.GetStringSize(weeks.ToString(), fontSize: size - 1).X + 8 + 2;

    /// <summary>
    /// The click's triangle centred at <paramref name="at"/>, pointing down, bobbing and breathing (owner, 2026-10-18: instead
    /// of "Click to continue"); its caller draws again every frame while it shows.
    /// </summary>
    public void DrawTriangle(CanvasItem c, Vector2 at, float k)
    {
        double t = UiClock.Msec / 1000.0;
        float bob = (float)(Math.Sin(t * Math.PI * 2 * 0.9) * 3);
        float alpha = (float)(0.65 + 0.35 * Math.Sin(t * Math.PI * 2 * 0.9 + 1));
        var p = at + new Vector2(0, bob);
        c.DrawColoredPolygon([p + new Vector2(-8, -6), p + new Vector2(8, -6), p + new Vector2(0, 7)], Gold with { A = alpha });
        c.DrawPolyline([p + new Vector2(-8, -6), p + new Vector2(8, -6), p + new Vector2(0, 7), p + new Vector2(-8, -6)],
            Name with { A = alpha }, Parts.Line(1, k));
    }

    /// <summary>A party's place as a vermilion seal (hanko): a red square, a paler rule inside, the numeral in paper colour.</summary>
    public void DrawSeal(CanvasItem c, Font font, Rect2 rect, int place)
    {
        c.DrawRect(rect, Vermilion);
        c.DrawRect(rect.Grow(-Math.Max(1, rect.Size.X * 0.1f)), SealText with { A = 0.45f }, false, 1);
        int size = Math.Max(6, (int)Math.Round(rect.Size.Y * 0.7f));
        string text = place.ToString();
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(rect.GetCenter().X - w / 2, Parts.Baseline(rect, font, size)), text, fontSize: size, modulate: SealText);
    }

    /// <summary>A place's number on a gold diamond of radius <paramref name="r"/> (our deployment screen's cards).</summary>
    public void DrawBadge(CanvasItem c, Font font, Vector2 at, float r, int place)
    {
        Parts.DrawDiamond(c, at, r, Gold);
        Parts.DrawDiamond(c, at, r - 2, BadgeFill);
        int size = Math.Max(6, (int)Math.Round(r * 1.1f));
        string text = place.ToString();
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(at.X - w / 2, at.Y + (font.GetAscent(size) - font.GetDescent(size)) / 2), text, fontSize: size, modulate: Name);
    }

    /// <summary>
    /// A mark's pill (decision 30, mock-ups H and S): a filled capsule <paramref name="height"/> tall ending at
    /// <paramref name="right"/>, centred on <paramref name="mid"/>, its <paramref name="label"/> in the pill's ink — Talk's
    /// (10 in 14) and Summon's (14 in 22).
    /// </summary>
    public void DrawPill(CanvasItem c, Font font, float right, float mid, string label, Color fill, int size, float height)
    {
        float w = font.GetStringSize(label, fontSize: size).X + height * 0.75f;
        var rect = new Rect2(right - w, mid - height / 2, w, height);
        int r = (int)Math.Round(height / 2);
        c.DrawStyleBox(new StyleBoxFlat
        {
            BgColor = fill, CornerRadiusTopLeft = r, CornerRadiusTopRight = r, CornerRadiusBottomLeft = r, CornerRadiusBottomRight = r,
            AntiAliasing = true,
        }, rect);
        c.DrawString(font, new Vector2(rect.Position.X, Parts.Baseline(rect, font, size) + 0.5f), label, HorizontalAlignment.Center, w, size, PillInk);
    }

    /// <summary>A divider: two dim rules with a gold diamond between them.</summary>
    public void DrawDivider(CanvasItem c, float x0, float x1, float y, float k)
    {
        float m = (x0 + x1) / 2;
        c.DrawLine(new Vector2(x0, y), new Vector2(m - 10, y), DimGold, Parts.Line(1, k));
        c.DrawLine(new Vector2(m + 10, y), new Vector2(x1, y), DimGold, Parts.Line(1, k));
        c.DrawColoredPolygon([new(m, y - 4), new(m + 4, y), new(m, y + 4), new(m - 4, y)], Gold);
    }

    /// <summary>The same divider standing up, between two columns (the touch mode's panels).</summary>
    public void DrawVerticalDivider(CanvasItem c, float x, float y0, float y1, float k)
    {
        float m = (y0 + y1) / 2;
        c.DrawLine(new Vector2(x, y0), new Vector2(x, m - 10), DimGold, Parts.Line(1, k));
        c.DrawLine(new Vector2(x, m + 10), new Vector2(x, y1), DimGold, Parts.Line(1, k));
        c.DrawColoredPolygon([new(x, m - 4), new(x + 4, m), new(x, m + 4), new(x - 4, m)], Gold);
    }
}
