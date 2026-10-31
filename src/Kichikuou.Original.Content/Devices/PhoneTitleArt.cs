using Kichikuou.Content.Graphics;

namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The title's pictures for the phone held upright (docs/ui/phone.md, §44-§45: PE6), cut from the player's own CGs: the
/// buttons' characters without their words, the logo, and the stone texture without the logo. <c>ｽﾀMENU</c> draws CG 301
/// (the background: the texture, a frieze at its top and foot, the logo) and copies each button's box onto it from CG 303
/// (unlit) or CG 302 (lit, the pointer over it; <c>ｽﾀMENU.ADV:10-14</c>, <c>:62-75</c>, the boxes <c>:805</c>). A button's
/// word is drawn into the same box, but as a plate of its own beside the character: where 302 differs from 301 the word is
/// a group of pixels apart (Spy's touches her skirt by its tail alone, so her box is cut under the skirt first), so the
/// character is the rest, its holes filled (<see cref="Character"/>). The logo is drawn in palette entries the texture
/// never uses (<see cref="Logo"/>), so the texture is shown without it (<see cref="Texture"/>). Interface only.
/// Engine-independent; tested against the player's files.
/// </summary>
public static class PhoneTitleArt
{
    /// <summary>The title's background (<c>ｽﾀMENU.ADV:14</c>).</summary>
    public const int BackgroundCg = 301;

    /// <summary>The buttons lit (<c>ｽﾀMENU.ADV:10</c>): the characters in full colour.</summary>
    public const int LitCg = 302;

    /// <summary>An RGBA picture: its size and its bytes, row-major, 4 a pixel.</summary>
    public sealed record Picture(int Width, int Height, byte[] Rgba);

    /// <summary>The buttons' boxes at 640×400 (<c>ｽﾀMENU.ADV:805</c>: x, y, w, h), 0-7; 8-10 draw nothing.</summary>
    private static readonly (int X, int Y, int W, int H)[] Boxes =
        [(0, 0, 240, 154), (248, 0, 187, 127), (434, 2, 202, 100), (545, 105, 92, 122),
         (0, 272, 96, 117), (95, 270, 151, 130), (261, 263, 173, 137), (463, 238, 177, 162)];

    /// <summary>Each button's word plate (x0, y0, x1, y1, the right and foot outside): a group of pixels inside it is the word.</summary>
    private static readonly (int X0, int Y0, int X1, int Y1)[] Words =
        [(135, 21, 238, 51), (353, 60, 433, 89), (445, 4, 520, 32), (550, 107, 634, 136),
         (21, 358, 78, 386), (97, 275, 170, 303), (375, 315, 430, 343), (465, 331, 526, 354)];

    /// <summary>Spy (button 4): her plate's tail touches her skirt, which ends in the row over this one.</summary>
    private const int SpyButton = 4, SpyCut = 358;

    /// <summary>CG 301's stone texture: the nine palette entries it is drawn in; every other one is the logo's.</summary>
    private static readonly HashSet<byte> TextureEntries = [30, 31, 169, 170, 171, 172, 191, 198, 206];

    /// <summary>CG 301's rows between its friezes, and the rows the logo and its ink splashes take.</summary>
    private const int InnerTop = 16, InnerFoot = 380, LogoTop = 124, LogoFoot = 276;

    /// <summary>The logo's letters are five groups of 683-15955 pixels; smaller groups are the splashes' odd pixels.</summary>
    private const int LogoLeast = 300;

    /// <summary>The logo's dark edge: its width, its colour (where the texture's dark ink was round the letters).</summary>
    private const int EdgeWidth = 2;
    private static readonly (byte R, byte G, byte B) Edge = (18, 12, 10);

    /// <summary>
    /// Button <paramref name="button"/>'s character (0-7) cut from <paramref name="lit"/> (CG 302) against
    /// <paramref name="background"/> (CG 301): the pixels of its box that differ from the background, less its word's group,
    /// the holes filled; cropped to it.
    /// </summary>
    public static Picture Character(CgImage background, CgImage lit, int button)
    {
        var (bx, by, bw, bh) = Boxes[button];
        var back = background.OwnPalette;
        var front = lit.OwnPalette;
        var own = new bool[bw * bh];
        for (int y = 0; y < bh; y++)
            for (int x = 0; x < bw; x++)
                own[y * bw + x] = (button != SpyButton || by + y < SpyCut)
                    && front[lit.IndexAt(bx + x, by + y)] != back[background.IndexAt(bx + x, by + y)];
        var word = Words[button];
        var keep = new bool[bw * bh];
        foreach (var group in Groups(own, bw, bh))
        {
            var (x0, y0, x1, y1) = group.Bounds;
            if (bx + x0 >= word.X0 - 1 && bx + x1 <= word.X1 + 1 && by + y0 >= word.Y0 - 1 && by + y1 <= word.Y1 + 1)
                continue;
            FillHoles(group, bw, keep);
        }
        return Crop(keep, bw, bh, (x, y) => front[lit.IndexAt(bx + x, by + y)]);
    }

    /// <summary>The logo cut from <paramref name="background"/> (CG 301) by its colours, with a dark edge round its letters.</summary>
    public static Picture Logo(CgImage background)
    {
        int w = background.Width, h = LogoFoot - LogoTop;
        var mine = new bool[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                mine[y * w + x] = !TextureEntries.Contains(background.IndexAt(x, LogoTop + y));
        var letters = new bool[w * h];
        foreach (var group in Groups(mine, w, h).Where(g => g.Pixels.Count >= LogoLeast))
            foreach (int i in group.Pixels)
                letters[i] = true;
        var edge = Dilate(letters, w, h, EdgeWidth);
        var palette = background.OwnPalette;
        return Crop(edge, w, h, (x, y) => letters[y * w + x] ? palette[background.IndexAt(x, LogoTop + y)] : Edge, margin: 1);
    }

    /// <summary>
    /// CG 301's texture between its friezes (640×364), the logo's band filled from the texture over and under it — its top
    /// half from rows 40-115, its foot from rows 290-365.
    /// </summary>
    public static Picture Texture(CgImage background)
    {
        int middle = (LogoTop + LogoFoot) / 2;
        return Rows(background, InnerTop, InnerFoot, y => y < LogoTop || y >= LogoFoot ? y : y < middle ? y - LogoTop + 40 : y - middle + 290);
    }

    /// <summary>CG 301's frieze at its top (rows 0-15).</summary>
    public static Picture FriezeTop(CgImage background) => Rows(background, 0, InnerTop, y => y);

    /// <summary>CG 301's frieze at its foot (rows 380-399).</summary>
    public static Picture FriezeFoot(CgImage background) => Rows(background, InnerFoot, background.Height, y => y);

    private static Picture Rows(CgImage cg, int top, int foot, Func<int, int> from)
    {
        var palette = cg.OwnPalette;
        var rgba = new byte[cg.Width * (foot - top) * 4];
        for (int y = top; y < foot; y++)
            for (int x = 0; x < cg.Width; x++)
            {
                var (r, g, b) = palette[cg.IndexAt(x, from(y))];
                int i = ((y - top) * cg.Width + x) * 4;
                (rgba[i], rgba[i + 1], rgba[i + 2], rgba[i + 3]) = (r, g, b, 255);
            }
        return new Picture(cg.Width, foot - top, rgba);
    }

    private sealed record Group(List<int> Pixels, (int X0, int Y0, int X1, int Y1) Bounds);

    /// <summary>The groups of <paramref name="mask"/>'s pixels touching side or corner.</summary>
    private static IEnumerable<Group> Groups(bool[] mask, int w, int h)
    {
        var seen = new bool[mask.Length];
        var queue = new Queue<int>();
        for (int start = 0; start < mask.Length; start++)
        {
            if (!mask[start] || seen[start])
                continue;
            var pixels = new List<int>();
            int x0 = w, y0 = h, x1 = 0, y1 = 0;
            seen[start] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int i = queue.Dequeue(), x = i % w, y = i / w;
                pixels.Add(i);
                (x0, y0, x1, y1) = (Math.Min(x0, x), Math.Min(y0, y), Math.Max(x1, x + 1), Math.Max(y1, y + 1));
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h)
                            continue;
                        int n = ny * w + nx;
                        if (mask[n] && !seen[n])
                        {
                            seen[n] = true;
                            queue.Enqueue(n);
                        }
                    }
            }
            yield return new Group(pixels, (x0, y0, x1, y1));
        }
    }

    /// <summary>Sets <paramref name="group"/>'s pixels in <paramref name="keep"/> and every pixel it closes in.</summary>
    private static void FillHoles(Group group, int w, bool[] keep)
    {
        var (x0, y0, x1, y1) = group.Bounds;
        int gw = x1 - x0, gh = y1 - y0;
        var inside = new bool[gw * gh];
        foreach (int i in group.Pixels)
            inside[(i / w - y0) * gw + i % w - x0] = true;
        var outside = new bool[gw * gh];                    // the rest reached from the bounds' edge, side to side
        var queue = new Queue<int>();
        for (int y = 0; y < gh; y++)
            for (int x = 0; x < gw; x++)
                if ((x == 0 || y == 0 || x == gw - 1 || y == gh - 1) && !inside[y * gw + x])
                {
                    outside[y * gw + x] = true;
                    queue.Enqueue(y * gw + x);
                }
        while (queue.Count > 0)
        {
            int i = queue.Dequeue(), x = i % gw, y = i / gw;
            foreach (var (nx, ny) in (ReadOnlySpan<(int, int)>)[(x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)])
            {
                if (nx < 0 || ny < 0 || nx >= gw || ny >= gh)
                    continue;
                int n = ny * gw + nx;
                if (!inside[n] && !outside[n])
                {
                    outside[n] = true;
                    queue.Enqueue(n);
                }
            }
        }
        for (int y = 0; y < gh; y++)
            for (int x = 0; x < gw; x++)
                if (!outside[y * gw + x])
                    keep[(y0 + y) * w + x0 + x] = true;
    }

    /// <summary><paramref name="mask"/> grown by <paramref name="steps"/> pixels, side to side at each step.</summary>
    private static bool[] Dilate(bool[] mask, int w, int h, int steps)
    {
        var grown = (bool[])mask.Clone();
        for (int s = 0; s < steps; s++)
        {
            var next = (bool[])grown.Clone();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (!grown[y * w + x] && ((x > 0 && grown[y * w + x - 1]) || (x < w - 1 && grown[y * w + x + 1])
                                              || (y > 0 && grown[(y - 1) * w + x]) || (y < h - 1 && grown[(y + 1) * w + x])))
                        next[y * w + x] = true;
            grown = next;
        }
        return grown;
    }

    /// <summary><paramref name="mask"/>'s pixels coloured by <paramref name="colour"/>, the rest clear, cropped to them.</summary>
    private static Picture Crop(bool[] mask, int w, int h, Func<int, int, (byte R, byte G, byte B)> colour, int margin = 0)
    {
        int x0 = w, y0 = h, x1 = 0, y1 = 0;
        for (int i = 0; i < mask.Length; i++)
            if (mask[i])
                (x0, y0, x1, y1) = (Math.Min(x0, i % w), Math.Min(y0, i / w), Math.Max(x1, i % w + 1), Math.Max(y1, i / w + 1));
        (x0, y0, x1, y1) = (Math.Max(0, x0 - margin), Math.Max(0, y0 - margin), Math.Min(w, x1 + margin), Math.Min(h, y1 + margin));
        int cw = x1 - x0, ch = y1 - y0;
        var rgba = new byte[cw * ch * 4];
        for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
                if (mask[(y0 + y) * w + x0 + x])
                {
                    var (r, g, b) = colour(x0 + x, y0 + y);
                    int i = (y * cw + x) * 4;
                    (rgba[i], rgba[i + 1], rgba[i + 2], rgba[i + 3]) = (r, g, b, 255);
                }
        return new Picture(cw, ch, rgba);
    }
}
