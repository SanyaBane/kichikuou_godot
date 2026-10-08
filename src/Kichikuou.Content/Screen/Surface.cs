using Kichikuou.Content.Graphics;

namespace Kichikuou.Content.Screen;

/// <summary>
/// The engine's drawing surface: the 3200×800 virtual screen of <c>WW 3200, 800, 8</c>
/// (<c>ｽﾀｰﾄ.ADV:852</c>) whose (0, 0)-(640, 400) corner is what the player sees
/// (docs/battle/screen-layout.md §0.1). Coordinates are the script's; the pixels are stored at
/// <see cref="Scale"/>× in RGBA so art can be drawn nearest-neighbour and text in high resolution.
/// <para>
/// The original surface holds palette indices. Here a pixel keeps its colour from the moment it
/// was drawn, which is the same picture as long as later palette loads agree on the indices
/// already on screen — true for the battle's CGs (docs/assets.md §3). The one index that matters
/// after drawing, the colour key 40 of <c>CS …, 40</c>, is remembered in the alpha channel
/// (<see cref="KeyAlpha"/>); <see cref="ReadScreen"/> returns opaque pixels.
/// </para>
/// </summary>
public sealed class Surface
{
    /// <summary>Stored pixels per script pixel, in each direction.</summary>
    public const int Scale = 2;

    /// <summary>The colour key index of every keyed draw and copy in the battle.</summary>
    public const int SpriteKey = 40;

    /// <summary>Alpha of a pixel drawn from palette index <see cref="SpriteKey"/> (all others are 255).</summary>
    public const byte KeyAlpha = 254;

    /// <summary>The visible screen.</summary>
    public const int ScreenWidth = 640, ScreenHeight = 400;

    private readonly byte[] _pixels;
    private readonly int _stride;

    /// <summary>Creates a black surface of <paramref name="width"/> × <paramref name="height"/> script pixels.</summary>
    public Surface(int width = 3200, int height = 800)
    {
        Width = width;
        Height = height;
        _stride = width * Scale * 4;
        _pixels = new byte[_stride * height * Scale];
        for (int i = 3; i < _pixels.Length; i += 4)
            _pixels[i] = 255;
    }

    /// <summary>Width in script pixels.</summary>
    public int Width { get; }

    /// <summary>Height in script pixels.</summary>
    public int Height { get; }

    /// <summary>The screen palette: the last CG drawn loaded its banks (<c>PC 7</c>).</summary>
    public Palette Palette { get; set; } = Palette.Initial;

    /// <summary>Counts the changes to the visible screen; a presenter uploads the screen when it moves.</summary>
    public int ScreenVersion { get; private set; }

    /// <summary>The colour of palette entry <paramref name="index"/> now.</summary>
    public (byte R, byte G, byte B) Color(int index) => Palette[index];

    /// <summary>
    /// <c>G n</c> / <c>G n, 40</c>: draws <paramref name="cg"/> with its top-left at (x, y), loading
    /// its palette first; with <paramref name="key"/> the pixels of that index are skipped.
    /// </summary>
    public void DrawCg(CgImage cg, int x, int y, int? key = null)
    {
        Palette = cg.PaletteOver(Palette);
        DrawCgRegion(cg, 0, 0, cg.Width, cg.Height, x, y, key);
    }

    /// <summary>Draws a part of <paramref name="cg"/> with the current palette (no palette load).</summary>
    public void DrawCgRegion(CgImage cg, int sx, int sy, int w, int h, int dx, int dy, int? key = null)
    {
        if (!Clip(ref sx, ref sy, ref w, ref h, ref dx, ref dy, cg.Width, cg.Height))
            return;
        var palette = Palette;
        for (int row = 0; row < h; row++)
        {
            for (int col = 0; col < w; col++)
            {
                byte index = cg.IndexAt(sx + col, sy + row);
                if (index == key)
                    continue;
                var (r, g, b) = palette[index];
                Plot(dx + col, dy + row, r, g, b, index == SpriteKey ? KeyAlpha : (byte)255);
            }
        }
        Touch(dx, dy, w, h);
    }

    /// <summary><c>CC sx, sy, w, h, dx, dy</c>: copies a rectangle (overlap-safe).</summary>
    public void Copy(int sx, int sy, int w, int h, int dx, int dy)
    {
        if (!Clip(ref sx, ref sy, ref w, ref h, ref dx, ref dy, Width, Height))
            return;
        int bytes = w * Scale * 4;
        int rows = h * Scale;
        bool upward = dy > sy;
        for (int i = 0; i < rows; i++)
        {
            int row = upward ? rows - 1 - i : i;
            int from = (sy * Scale + row) * _stride + sx * Scale * 4;
            int to = (dy * Scale + row) * _stride + dx * Scale * 4;
            Buffer.BlockCopy(_pixels, from, _pixels, to, bytes);
        }
        Touch(dx, dy, w, h);
    }

    /// <summary><c>CS sx, sy, w, h, dx, dy, 40</c>: copies a rectangle skipping pixels of index 40.</summary>
    public void CopyKeyed(int sx, int sy, int w, int h, int dx, int dy)
    {
        if (!Clip(ref sx, ref sy, ref w, ref h, ref dx, ref dy, Width, Height))
            return;
        // Source and destination never overlap in the battle; copy through a buffer anyway.
        var source = new byte[w * Scale * h * Scale * 4];
        int bytes = w * Scale * 4;
        for (int row = 0; row < h * Scale; row++)
            Buffer.BlockCopy(_pixels, (sy * Scale + row) * _stride + sx * Scale * 4, source, row * bytes, bytes);
        for (int row = 0; row < h * Scale; row++)
        {
            int to = (dy * Scale + row) * _stride + dx * Scale * 4;
            for (int i = 0; i < bytes; i += 4)
            {
                if (source[row * bytes + i + 3] == KeyAlpha)
                    continue;
                Buffer.BlockCopy(source, row * bytes + i, _pixels, to + i, 4);
            }
        }
        Touch(dx, dy, w, h);
    }

    /// <summary><c>CF x, y, w, h, c</c> / <c>EC</c>: fills a rectangle with palette entry <paramref name="index"/>.</summary>
    public void Fill(int x, int y, int w, int h, int index)
    {
        int sx = x, sy = y;
        if (!Clip(ref sx, ref sy, ref w, ref h, ref x, ref y, Width, Height))
            return;
        var (r, g, b) = Palette[index];
        byte a = index == SpriteKey ? KeyAlpha : (byte)255;
        for (int row = 0; row < h; row++)
            for (int col = 0; col < w; col++)
                Plot(x + col, y + row, r, g, b, a);
        Touch(x, y, w, h);
    }

    /// <summary><c>ags_drawRectangle</c>: a one-pixel outline of the rectangle in palette entry <paramref name="index"/>.</summary>
    public void DrawRectangle(int x, int y, int w, int h, int index)
    {
        Fill(x, y, w, 1, index);
        Fill(x, y + h - 1, w, 1, index);
        Fill(x, y, 1, h, index);
        Fill(x + w - 1, y, 1, h, index);
    }

    /// <summary><c>ags_saveRegion</c>: a copy of a rectangle, for <see cref="RestoreRegion"/>.</summary>
    public SavedRegion SaveRegion(int x, int y, int w, int h)
    {
        int sx = x, sy = y, dx = x, dy = y;
        if (!Clip(ref sx, ref sy, ref w, ref h, ref dx, ref dy, Width, Height))
            return new SavedRegion(0, 0, 0, 0, []);
        int bytes = w * Scale * 4;
        var data = new byte[bytes * h * Scale];
        for (int row = 0; row < h * Scale; row++)
            Buffer.BlockCopy(_pixels, (sy * Scale + row) * _stride + sx * Scale * 4, data, row * bytes, bytes);
        return new SavedRegion(sx, sy, w, h, data);
    }

    /// <summary><c>ags_restoreRegion</c>: puts a saved rectangle back where it came from.</summary>
    public void RestoreRegion(SavedRegion region)
    {
        int bytes = region.W * Scale * 4;
        for (int row = 0; row < region.H * Scale; row++)
            Buffer.BlockCopy(region.Data, row * bytes, _pixels, (region.Y * Scale + row) * _stride + region.X * Scale * 4, bytes);
        Touch(region.X, region.Y, region.W, region.H);
    }

    /// <summary>
    /// Blends a coverage bitmap (0-255 per stored pixel) in <paramref name="color"/> with its top-left
    /// at stored-pixel position (<paramref name="px"/>, <paramref name="py"/>) — text in high resolution.
    /// Pixels outside <paramref name="clip"/> (script pixels) are left alone.
    /// </summary>
    public void Blend(byte[] coverage, int width, int height, int px, int py, (byte R, byte G, byte B) color,
        (int X, int Y, int W, int H)? clip = null)
    {
        var (cx, cy, cw, ch) = clip ?? (0, 0, Width, Height);
        int minX = Math.Max(cx * Scale, 0), minY = Math.Max(cy * Scale, 0);
        int maxX = Math.Min((cx + cw) * Scale, Width * Scale), maxY = Math.Min((cy + ch) * Scale, Height * Scale);
        bool touched = false;
        for (int row = 0; row < height; row++)
        {
            int y = py + row;
            if (y < minY || y >= maxY)
                continue;
            for (int col = 0; col < width; col++)
            {
                int x = px + col;
                int c = coverage[row * width + col];
                if (c == 0 || x < minX || x >= maxX)
                    continue;
                int o = y * _stride + x * 4;
                _pixels[o] = (byte)((color.R * c + _pixels[o] * (255 - c) + 127) / 255);
                _pixels[o + 1] = (byte)((color.G * c + _pixels[o + 1] * (255 - c) + 127) / 255);
                _pixels[o + 2] = (byte)((color.B * c + _pixels[o + 2] * (255 - c) + 127) / 255);
                _pixels[o + 3] = 255;
                touched = true;
            }
        }
        if (touched && px < ScreenWidth * Scale && py < ScreenHeight * Scale)
            ScreenVersion++;
    }

    /// <summary>The stored colour at script pixel (x, y) (top-left stored pixel), with its alpha marker.</summary>
    public (byte R, byte G, byte B, byte A) PixelAt(int x, int y) => StoredPixelAt(x * Scale, y * Scale);

    /// <summary>The colour at stored pixel (px, py).</summary>
    public (byte R, byte G, byte B, byte A) StoredPixelAt(int px, int py)
    {
        int o = py * _stride + px * 4;
        return (_pixels[o], _pixels[o + 1], _pixels[o + 2], _pixels[o + 3]);
    }

    /// <summary>
    /// The visible screen as opaque RGBA rows (<see cref="ScreenWidth"/>·<see cref="Scale"/> ×
    /// <see cref="ScreenHeight"/>·<see cref="Scale"/>), into <paramref name="target"/> when given.
    /// </summary>
    public byte[] ReadScreen(byte[]? target = null)
    {
        int bytes = ScreenWidth * Scale * 4;
        target ??= new byte[bytes * ScreenHeight * Scale];
        for (int row = 0; row < ScreenHeight * Scale; row++)
            Buffer.BlockCopy(_pixels, row * _stride, target, row * bytes, bytes);
        for (int i = 3; i < target.Length; i += 4)
            target[i] = 255;
        return target;
    }

    private void Plot(int x, int y, byte r, byte g, byte b, byte a)
    {
        int o = y * Scale * _stride + x * Scale * 4;
        for (int dy = 0; dy < Scale; dy++)
        {
            int p = o + dy * _stride;
            for (int dx = 0; dx < Scale; dx++, p += 4)
            {
                _pixels[p] = r;
                _pixels[p + 1] = g;
                _pixels[p + 2] = b;
                _pixels[p + 3] = a;
            }
        }
    }

    private void Touch(int x, int y, int w, int h)
    {
        if (x < ScreenWidth && y < ScreenHeight && x + w > 0 && y + h > 0)
            ScreenVersion++;
    }

    /// <summary>Clips a source rectangle (inside a sourceW × sourceH image) and its destination to the surface.</summary>
    private bool Clip(ref int sx, ref int sy, ref int w, ref int h, ref int dx, ref int dy, int sourceW, int sourceH)
    {
        if (sx < 0) { w += sx; dx -= sx; sx = 0; }
        if (sy < 0) { h += sy; dy -= sy; sy = 0; }
        if (dx < 0) { w += dx; sx -= dx; dx = 0; }
        if (dy < 0) { h += dy; sy -= dy; dy = 0; }
        w = Math.Min(w, Math.Min(sourceW - sx, Width - dx));
        h = Math.Min(h, Math.Min(sourceH - sy, Height - dy));
        return w > 0 && h > 0;
    }
}

/// <summary>A rectangle saved by <see cref="Surface.SaveRegion"/> (script coordinates, stored pixels).</summary>
public sealed record SavedRegion(int X, int Y, int W, int H, byte[] Data);
