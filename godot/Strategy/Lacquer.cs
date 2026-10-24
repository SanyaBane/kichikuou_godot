using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The lacquer-and-gold style of our map panels (the owner's mock-up 4a, docs/ui/command-screen.md): the
/// colours, the fonts, and the parts both the province menu (<see cref="ProvinceMenuView"/>) and the command
/// panel (<see cref="CommandMenuView"/>) draw — the panel with its double gold frame and corners, a row's box,
/// a tag, a divider — and the ledger's paper and inks (<see cref="SubordinatesView"/>'s table). Sizes are in the 1280×800 screen's pixels; <c>k</c> is the window pixels to a screen pixel.
/// </summary>
internal static class Lacquer
{
    public static readonly Color Panel = new(22 / 255f, 16 / 255f, 12 / 255f, 0.96f);
    public static readonly Color Gold = Color.Color8(196, 160, 86);
    public static readonly Color DimGold = Color.Color8(110, 86, 46);
    public static readonly Color Name = Color.Color8(232, 199, 122);
    public static readonly Color Subtitle = Color.Color8(160, 140, 104);
    public static readonly Color Label = Color.Color8(150, 134, 108);
    public static readonly Color Value = Color.Color8(236, 226, 204);
    public static readonly Color Row = Color.Color8(34, 26, 20);
    public static readonly Color RowHover = Color.Color8(64, 48, 28);
    public static readonly Color HoverLine = Color.Color8(222, 186, 104);
    public static readonly Color Text = Color.Color8(244, 232, 206);
    public static readonly Color Quiet = Color.Color8(176, 160, 130);
    public static readonly Color OffText = Color.Color8(98, 88, 74);
    public static readonly Color OffTag = Color.Color8(52, 46, 40);
    public static readonly Color SpentPip = Color.Color8(60, 50, 38);
    public static readonly Color OffButton = Color.Color8(40, 35, 30);
    public static readonly Color OffLine = Color.Color8(66, 58, 48);

    // The ledger: our tables as paper in their lacquer frames (the owner's mock-ups G3, docs/ui/subordinates.md, and E1/E2,
    // docs/ui/deployment-table.md).
    public static readonly Color Paper = Color.Color8(224, 208, 172);
    public static readonly Color PaperHead = Color.Color8(196, 176, 136);
    public static readonly Color PaperRule = Color.Color8(150, 120, 80, 110);
    public static readonly Color Ink = Color.Color8(40, 28, 18);
    public static readonly Color InkQuiet = Color.Color8(100, 80, 56);
    public static readonly Color InkFaint = Color.Color8(130, 112, 86);     // fainter than quiet, still apart from the grey rows
    public static readonly Color InkGrey = Color.Color8(150, 132, 104);
    public static readonly Color VermilionInk = Color.Color8(150, 52, 30);
    public static readonly Color HeadInk = Color.Color8(120, 40, 26);
    public static readonly Color Vermilion = Color.Color8(170, 40, 28);
    public static readonly Color ChosenRow = Color.Color8(190, 60, 40, 60);
    public static readonly Color PartyRow = Color.Color8(200, 160, 80, 70);
    public static readonly Color SealText = Color.Color8(246, 230, 206);
    public static readonly Color BarGood = Color.Color8(70, 110, 50);
    public static readonly Color BarLow = Color.Color8(160, 110, 30);
    public static readonly Color BarBad = Color.Color8(170, 50, 36);
    public static readonly Color Plus = Color.Color8(150, 196, 110);
    public static readonly Color Minus = Color.Color8(222, 110, 90);

    // The marks on Talk and the harem (owner's decision 30, docs/ui/talk-marks.md §6: H and S): NEW green, ??? amber on the
    // lacquer, a pill's dark ink; on the ledger's paper green and vermilion ink, a read talk's faint outline.
    public static readonly Color MarkNew = Color.Color8(150, 196, 110);
    public static readonly Color MarkMaybe = Color.Color8(226, 168, 72);
    public static readonly Color PillInk = Color.Color8(24, 18, 12);
    public static readonly Color InkGreen = Color.Color8(52, 104, 40);
    public static readonly Color ReadOutline = Color.Color8(150, 132, 104, 160);

    public static FontFile LoadFont(string path) => new() { Data = Godot.FileAccess.GetFileAsBytes(path) };

    /// <summary>Shojumaru, the names' brush face.</summary>
    public static FontFile Brush() => LoadFont("res://fonts/Shojumaru-Regular.ttf");

    /// <summary>Cinzel at <paramref name="weight"/> (500 by default), the subtitles' face.</summary>
    public static FontVariation Cinzel(int weight = 500) => new()
    {
        BaseFont = LoadFont("res://fonts/Cinzel-VariableFont_wght.ttf"),
        VariationOpentype = new Godot.Collections.Dictionary
        {
            [TextServerManager.Singleton.GetPrimaryInterface().NameToTag("wght")] = weight,
        },
    };

    /// <summary>
    /// A line's width, at least one window pixel: a thinner one may fall between the pixels' centres when the
    /// screen is scaled down and vanish (the first row's top edge did, owner, 2026-10-16).
    /// </summary>
    public static float Line(float width, float k) => Math.Max(width, 1 / k);

    /// <summary>
    /// A filled polygon with smooth edges. Godot fills a coloured polygon without antialiasing, and a slanted edge shows
    /// its steps even under an antialiased outline (the deployment screen's facing head, owner, 2026-10-24): the shape is
    /// rasterized here at the window's own pixels, each pixel's cover sampled 4×4, and drawn as a texture, kept for the
    /// next frame that draws the same shape at the same size. <paramref name="k"/> is the draw transform's scale
    /// (<c>DrawSetTransform</c>), which the canvas item's own transform does not include.
    /// </summary>
    public static void DrawSmoothPolygon(CanvasItem c, Vector2[] points, Color colour, float k)
    {
        var toWindow = c.GetViewport().GetFinalTransform() * c.GetGlobalTransformWithCanvas();
        float scale = toWindow.X.Length() * k;                      // the window's own pixels to a unit of the points
        var min = points.Aggregate(points[0], (a, p) => a.Min(p)).Floor() - Vector2.One;
        var max = points.Aggregate(points[0], (a, p) => a.Max(p)).Ceil() + Vector2.One;
        int w = Math.Max(1, (int)Math.Ceiling((max.X - min.X) * scale)), h = Math.Max(1, (int)Math.Ceiling((max.Y - min.Y) * scale));
        var shape = points.Select(p => (p - min) * scale).ToArray();
        string key = $"{string.Join(";", shape.Select(p => $"{p.X:F2},{p.Y:F2}"))}|{colour.ToRgba32()}|{w}x{h}";
        if (!SmoothFills.TryGetValue(key, out var texture))
        {
            const int Samples = 4;
            var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int cover = 0;
                    for (int sy = 0; sy < Samples; sy++)
                        for (int sx = 0; sx < Samples; sx++)
                            if (Inside(shape, new Vector2(x + (sx + 0.5f) / Samples, y + (sy + 0.5f) / Samples)))
                                cover++;
                    image.SetPixel(x, y, colour with { A = colour.A * cover / (Samples * Samples) });
                }
            SmoothFills[key] = texture = ImageTexture.CreateFromImage(image);
        }
        c.DrawTextureRect(texture, new Rect2(min, new Vector2(w, h) / scale), false);
    }

    private static readonly Dictionary<string, ImageTexture> SmoothFills = [];

    /// <summary>Whether <paramref name="p"/> is inside <paramref name="polygon"/> (the crossings of a ray to the right).</summary>
    private static bool Inside(Vector2[] polygon, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i], b = polygon[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>The baseline that centres text of <paramref name="size"/> in <paramref name="rect"/>.</summary>
    public static float Baseline(Rect2 rect, Font font, int size) =>
        rect.Position.Y + rect.Size.Y / 2 + (font.GetAscent(size) - font.GetDescent(size)) / 2;

    /// <summary>The panel: its shadow, the lacquer (<see cref="Panel"/> unless <paramref name="fill"/>), the double gold frame and its corners.</summary>
    public static void DrawPanel(CanvasItem c, Rect2 box, float k, Color? fill = null)
    {
        float x = box.Position.X, y = box.Position.Y, w = box.Size.X, h = box.Size.Y;
        for (int i = 6; i >= 1; i--)
            c.DrawRect(new Rect2(x - i + 2, y - i + 6, w + 2 * i, h + 2 * i), new Color(0, 0, 0, 0.06f));
        c.DrawRect(box, fill ?? Panel);
        c.DrawRect(new Rect2(x + 1, y + 1, w - 2, h - 2), Gold, false, Line(2, k));
        c.DrawRect(new Rect2(x + 5.5f, y + 5.5f, w - 11, h - 11), DimGold, false, Line(1, k));
        foreach (var (cx, cy, sx, sy) in new[] { (x, y, 1, 1), (x + w, y, -1, 1), (x, y + h, 1, -1), (x + w, y + h, -1, -1) })
        {
            c.DrawPolyline([new(cx + sx * 9, cy + sy * 2), new(cx + sx * 9, cy + sy * 9), new(cx + sx * 2, cy + sy * 9)], Gold, Line(2, k));
            c.DrawColoredPolygon([new(cx + sx * 9, cy + sy * 14), new(cx + sx * 14, cy + sy * 9), new(cx + sx * 9, cy + sy * 4), new(cx + sx * 4, cy + sy * 9)], Gold);
        }
    }

    /// <summary>
    /// A whole screen's backdrop (our Subordinates and deployment screens): dark lacquer under a faint gold wave pattern
    /// (seigaiha), darkened towards the edges; drawn in the 1280×800 screen's pixels.
    /// </summary>
    public static void DrawWaveBackdrop(CanvasItem c, float k)
    {
        var lacquer = Color.Color8(14, 10, 8);
        c.DrawRect(new Rect2(0, 0, 1280, 800), lacquer);
        const float R = 34;
        var wave = Gold with { A = 0.05f };
        for (int row = -1; row * R / 2 < 800 + R; row++)
        {
            float y = row * R / 2, off = row % 2 == 0 ? 0 : R;
            for (float x = -2 * R + off; x < 1280 + 2 * R; x += 2 * R)
            {
                var at = new Vector2(x, y);
                c.DrawCircle(at, R, lacquer);
                foreach (float r in (float[])[R, R * 0.72f, R * 0.44f])
                    c.DrawArc(at, r, 0, Mathf.Tau, 40, wave, Line(1, k), true);
            }
        }
        const float Inset = 220;
        var edge = new Color(0, 0, 0, 0.7f);
        var clear = new Color(0, 0, 0, 0);
        Vector2[] outer = [new(0, 0), new(1280, 0), new(1280, 800), new(0, 800)];
        Vector2[] inner = [new(Inset, Inset * 0.62f), new(1280 - Inset, Inset * 0.62f), new(1280 - Inset, 800 - Inset * 0.62f), new(Inset, 800 - Inset * 0.62f)];
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            c.DrawPolygon([outer[i], outer[j], inner[j], inner[i]], [edge, edge, clear, clear]);
        }
    }

    /// <summary>A main button's face: red lacquer framed in gold, as Turn END on the command panel; lit when hovered, sunk when pressed.</summary>
    public static void DrawRedButton(CanvasItem c, Rect2 rect, bool hover, bool down, float k)
    {
        Color top = down ? Color.Color8(96, 22, 16) : hover ? Color.Color8(150, 40, 30) : Color.Color8(128, 32, 24);
        Color bottom = down ? Color.Color8(70, 14, 10) : hover ? Color.Color8(96, 20, 14) : Color.Color8(80, 16, 12);
        c.DrawPolygon([rect.Position, new(rect.End.X, rect.Position.Y), rect.End, new(rect.Position.X, rect.End.Y)], [top, top, bottom, bottom]);
        c.DrawRect(rect, hover ? HoverLine : Gold, false, Line(2, k));
        c.DrawRect(rect.Grow(-4), Color.Color8(150, 96, 52), false, Line(1, k));
    }

    /// <summary>A row's box: dark with a dim frame, or lit with a bright one when hovered.</summary>
    public static void DrawRowBox(CanvasItem c, Rect2 rect, bool hover, float k)
    {
        c.DrawRect(rect, hover ? RowHover : Row);
        c.DrawRect(rect, hover ? HoverLine : DimGold, false, Line(hover ? 2 : 1, k));
    }

    /// <summary>A command's tag: its colour framed in gold, or grey when the command is off.</summary>
    public static void DrawTag(CanvasItem c, Font font, Rect2 rect, string tag, Color colour, bool on, float k)
    {
        c.DrawRect(rect, on ? colour : OffTag);
        if (on)
            c.DrawRect(rect, Gold, false, Line(1, k));
        float tw = font.GetStringSize(tag, fontSize: 13).X;
        c.DrawString(font, new Vector2(rect.GetCenter().X - tw / 2, Baseline(rect, font, 13)), tag, fontSize: 13, modulate: on ? Text : OffText);
    }

    /// <summary>
    /// The click's triangle centred at <paramref name="at"/>, pointing down, bobbing and breathing (owner, 2026-10-18: instead
    /// of "Click to continue"); its caller draws again every frame while it shows.
    /// </summary>
    public static void DrawTriangle(CanvasItem c, Vector2 at, float k)
    {
        double t = Time.GetTicksMsec() / 1000.0;
        float bob = (float)(Math.Sin(t * Math.PI * 2 * 0.9) * 3);
        float alpha = (float)(0.65 + 0.35 * Math.Sin(t * Math.PI * 2 * 0.9 + 1));
        var p = at + new Vector2(0, bob);
        c.DrawColoredPolygon([p + new Vector2(-8, -6), p + new Vector2(8, -6), p + new Vector2(0, 7)], Gold with { A = alpha });
        c.DrawPolyline([p + new Vector2(-8, -6), p + new Vector2(8, -6), p + new Vector2(0, 7), p + new Vector2(-8, -6)],
            Name with { A = alpha }, Line(1, k));
    }

    /// <summary>A party's place as a vermilion seal (hanko): a red square, a paler rule inside, the numeral in paper colour.</summary>
    public static void DrawSeal(CanvasItem c, Font font, Rect2 rect, int place)
    {
        c.DrawRect(rect, Vermilion);
        c.DrawRect(rect.Grow(-Math.Max(1, rect.Size.X * 0.1f)), SealText with { A = 0.45f }, false, 1);
        int size = Math.Max(6, (int)Math.Round(rect.Size.Y * 0.7f));
        string text = place.ToString();
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(rect.GetCenter().X - w / 2, Baseline(rect, font, size)), text, fontSize: size, modulate: SealText);
    }

    /// <summary>A place's number on a gold diamond of radius <paramref name="r"/> (our deployment screen's cards).</summary>
    public static void DrawBadge(CanvasItem c, Font font, Vector2 at, float r, int place)
    {
        DrawDiamond(c, at, r, Gold);
        DrawDiamond(c, at, r - 2, Color.Color8(60, 44, 24));
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
    public static void DrawPill(CanvasItem c, Font font, float right, float mid, string label, Color fill, int size, float height)
    {
        float w = font.GetStringSize(label, fontSize: size).X + height * 0.75f;
        var rect = new Rect2(right - w, mid - height / 2, w, height);
        int r = (int)Math.Round(height / 2);
        c.DrawStyleBox(new StyleBoxFlat
        {
            BgColor = fill, CornerRadiusTopLeft = r, CornerRadiusTopRight = r, CornerRadiusBottomLeft = r, CornerRadiusBottomRight = r,
            AntiAliasing = true,
        }, rect);
        c.DrawString(font, new Vector2(rect.Position.X, Baseline(rect, font, size) + 0.5f), label, HorizontalAlignment.Center, w, size, PillInk);
    }

    /// <summary>A small diamond centred at <paramref name="at"/>.</summary>
    public static void DrawDiamond(CanvasItem c, Vector2 at, float r, Color colour) =>
        c.DrawColoredPolygon([at + new Vector2(0, -r), at + new Vector2(r, 0), at + new Vector2(0, r), at + new Vector2(-r, 0)], colour);

    /// <summary>A divider: two dim rules with a gold diamond between them.</summary>
    public static void DrawDivider(CanvasItem c, float x0, float x1, float y, float k)
    {
        float m = (x0 + x1) / 2;
        c.DrawLine(new Vector2(x0, y), new Vector2(m - 10, y), DimGold, Line(1, k));
        c.DrawLine(new Vector2(m + 10, y), new Vector2(x1, y), DimGold, Line(1, k));
        c.DrawColoredPolygon([new(m, y - 4), new(m + 4, y), new(m, y + 4), new(m - 4, y)], Gold);
    }
}
