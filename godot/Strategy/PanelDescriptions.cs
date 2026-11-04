using System.Runtime.CompilerServices;
using Godot;
using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The description panel of a picture panel set again in our lacquer (owner's mock-up B, docs/ui/unit-descriptions.md):
/// a unit's status CG, a harem girl's or an item's picture keeps its sprite and frame, and the panel at its right, whose
/// text the English patch drew in a pixel font, is covered and its text (<see cref="UnitDescription"/>) set in Oswald.
/// <see cref="FacePictures.PicturePanel(int, int, int)"/> registers the CG each panel texture shows; a screen draws the
/// texture with <see cref="Draw"/>. A panel the reading does not know (654's Japanese, a CG without a panel) stays the
/// original's.
/// </summary>
internal static class PanelDescriptions
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    private sealed record Shown(int Cg, CgImage Image, CgLibrary Cgs);

    private static readonly ConditionalWeakTable<Texture2D, Shown> Panels = new();
    private static readonly Dictionary<int, IReadOnlyList<string>?> Texts = [];
    private static readonly Dictionary<(int Cg, int Size, int Width), List<string?>> Wrapped = [];
    private static readonly Dictionary<(int Width, int Height), int> Sizes = [];

    /// <summary>The longest text of all (384 letters), whose fit sets the size: every panel on a screen reads alike.</summary>
    private const int LongestCg = 622;

    private static readonly FontVariation Oswald = new()
    {
        BaseFont = Lacquer.LoadFont("res://fonts/Oswald-VariableFont_wght.ttf"),
        VariationOpentype = new Godot.Collections.Dictionary
        {
            [TextServerManager.Singleton.GetPrimaryInterface().NameToTag("wght")] = 400,
        },
    };

    /// <summary>Notes that <paramref name="panel"/> shows CG <paramref name="cg"/> (0: none) at its place in the picker's cut.</summary>
    public static void Register(Texture2D panel, int cg, CgLibrary cgs)
    {
        if (cg != 0 && cgs.Get(cg) is { } image)
            Panels.AddOrUpdate(panel, new Shown(cg, image, cgs));
    }

    /// <summary>
    /// Draws <paramref name="panel"/> over <paramref name="area"/> (the picker's cut, <see cref="FacePictures.PictureCut"/>,
    /// scaled to it) and, when its CG's description reads, the panel at its right in lacquer with the text.
    /// </summary>
    public static void Draw(CanvasItem c, Texture2D panel, Rect2 area)
    {
        c.DrawTextureRect(panel, area, false);
        if (!Panels.TryGetValue(panel, out var shown) || UnitDescription.Rows(shown.Image) is not var (top, bottom)
            || TextOf(shown.Cg, shown.Image) is not { } paragraphs)
            return;
        var (cutX, cutY, cutW, cutH) = FacePictures.PictureCut;
        float sx = area.Size.X / cutW, sy = area.Size.Y / cutH;
        int left = shown.Image.X - cutX + UnitDescription.Left, y0 = shown.Image.Y - cutY;
        var inside = new Rect2(area.Position.X + left * sx, area.Position.Y + (y0 + top) * sy,
            (shown.Image.Width - UnitDescription.Left) * sx, (bottom - top) * sy);     // the panel's pattern runs to the CG's edge

        var fill = Ui.Panel;
        c.DrawRect(inside, new Color(fill.R, fill.G, fill.B));
        float inset = 2 * sx;
        c.DrawRect(inside.Grow(-inset), Ui.DimGold, false, Math.Max(1f, 0.5f * sx));

        float pad = 4 * sx;
        int width = (int)(inside.Size.X - 2 * pad - 2 * sx), height = (int)(inside.Size.Y - 2 * pad);
        int size = SizeFor(width, height, sx, shown.Cgs, paragraphs);
        float step = MathF.Round(size * 1.22f), gap = MathF.Round(size * 0.5f);
        float x = inside.Position.X + pad + sx, y = inside.Position.Y + pad + sx;
        bool quoted = false;
        foreach (var row in Wrap(shown.Cg, paragraphs, size, width))
        {
            if (row is null)
            {
                y += gap;
                quoted = false;
                continue;
            }
            quoted = DrawRow(c, new Vector2(x, y + Oswald.GetAscent(size)), row, size, quoted);
            y += step;
        }
    }

    /// <summary>
    /// The largest size the longest text of all fits in a panel of <paramref name="width"/>×<paramref name="height"/>, from
    /// ten of the CG's pixels down to five; without that text (not in the player's files), this one's own fit.
    /// </summary>
    private static int SizeFor(int width, int height, float scale, CgLibrary cgs, IReadOnlyList<string> own)
    {
        if (Sizes.TryGetValue((width, height), out int size))
            return size;
        var longest = cgs.Get(LongestCg) is { } image ? TextOf(LongestCg, image) : null;
        var text = longest ?? own;
        int cg = longest is null ? -1 : LongestCg;
        int bottom = Math.Max(6, (int)MathF.Round(5 * scale));
        for (size = Math.Max(bottom, (int)MathF.Round(10 * scale)); size > bottom; size--)
            if (Height(Wrap(cg, text, size, width), size) <= height)
                break;
        Sizes[(width, height)] = size;
        return size;
    }

    private static float Height(List<string?> rows, int size) =>
        rows.Sum(r => r is null ? MathF.Round(size * 0.5f) : MathF.Round(size * 1.22f));

    private static IReadOnlyList<string>? TextOf(int cg, CgImage image)
    {
        if (!Texts.TryGetValue(cg, out var text))
            Texts[cg] = text = UnitDescription.Read(image, cg);
        return text;
    }

    /// <summary>The paragraphs' words in rows at most <paramref name="width"/> wide, a null between two paragraphs.</summary>
    private static List<string?> Wrap(int cg, IReadOnlyList<string> paragraphs, int size, int width)
    {
        if (cg >= 0 && Wrapped.TryGetValue((cg, size, width), out var kept))
            return kept;
        var rows = new List<string?>();
        foreach (var paragraph in paragraphs)
        {
            if (rows.Count > 0)
                rows.Add(null);
            string line = "";
            foreach (var word in paragraph.Split(' '))
            {
                string trial = line.Length == 0 ? word : line + " " + word;
                if (line.Length == 0 || Oswald.GetStringSize(trial, HorizontalAlignment.Left, -1, size).X <= width)
                    line = trial;
                else
                {
                    rows.Add(line);
                    line = word;
                }
            }
            if (line.Length > 0)
                rows.Add(line);
        }
        if (cg >= 0)
            Wrapped[(cg, size, width)] = rows;
        return rows;
    }

    /// <summary>
    /// A row, a quoted name (a special attack's, a nickname) in the names' gold, its marks too; <paramref name="quoted"/>:
    /// the row starts inside a name the row before broke ("By / la Way"). Returns whether the row ends inside one.
    /// </summary>
    private static bool DrawRow(CanvasItem c, Vector2 at, string row, int size, bool quoted)
    {
        int from = 0;
        for (int i = 0; i < row.Length; i++)
        {
            if (row[i] != '"')
                continue;
            int end = quoted ? i + 1 : i;                           // the closing mark goes with the name, the opening one too
            at = Part(c, at, row[from..end], size, quoted ? Ui.Name : Ui.Text);
            from = end;
            quoted = !quoted;
        }
        Part(c, at, row[from..], size, quoted ? Ui.Name : Ui.Text);
        return quoted;
    }

    private static Vector2 Part(CanvasItem c, Vector2 at, string text, int size, Color colour)
    {
        if (text.Length == 0)
            return at;
        c.DrawString(Oswald, at, text, HorizontalAlignment.Left, -1, size, colour);
        return at + new Vector2(Oswald.GetStringSize(text, HorizontalAlignment.Left, -1, size).X, 0);
    }
}
