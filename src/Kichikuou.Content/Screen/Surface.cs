// Parts translated from xsystem35 (https://github.com/kichikuou/xsystem35-sdl2), ags.c
// (ags_check_param, as CheckParam): Copyright (C) 1997-1998 Masaki Chikama (Wren). GNU General Public
// License, version 2 or later, as KichikuouGodot (LICENSE).

using Kichikuou.Content.Graphics;

namespace Kichikuou.Content.Screen;

/// <summary>
/// The engine's drawing surface: the 3200×800 virtual screen of <c>WW 3200, 800, 8</c>
/// (<c>ｽﾀｰﾄ.ADV:852</c>) whose (0, 0)-(640, 400) corner is what the player sees, unless <c>WV</c>
/// moves the view (docs/battle/screen-layout.md §0.1). Coordinates are the script's; the pixels are
/// stored at <see cref="Scale"/>× so art can be drawn nearest-neighbour and text in high resolution.
/// <para>
/// An <b>indexed</b> surface (the engine's: the stage, the battle's) is 8-bit as xsystem35's at
/// depth 8: the CGs and fills store palette entries and show in the <see cref="Palette"/> of the
/// moment they are read, so a palette load recolours everything already drawn (<c>PC 1</c> draws a
/// CG without its palette, a later <c>PC 6</c> loads it — <c>ｵｰﾌﾟﾆﾝｸﾞ.ADV:5-41</c>). Text, debug
/// fills and fades store colours (<see cref="Direct"/> pixels) that no palette changes. A surface
/// that is not indexed (the hand-drawn <c>Screen</c>) stores every pixel's colour at the moment it
/// is drawn. Index 40, the colour key of <c>CS …, 40</c>, reads with alpha <see cref="KeyAlpha"/>
/// (in a surface that is not indexed it is remembered in the alpha channel);
/// <see cref="ReadScreen"/> returns opaque pixels.
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

    /// <summary>The index-plane mark of a pixel that stores its colour.</summary>
    private const short Direct = -1;

    private readonly byte[] _pixels;
    private readonly short[]? _index;
    private readonly int _stride;
    private readonly int _rowPixels;
    private Palette _palette = Palette.Initial;

    /// <summary>
    /// Creates a black surface of <paramref name="width"/> × <paramref name="height"/> script pixels;
    /// <paramref name="indexed"/> as the engine's (palette entry 0 everywhere).
    /// </summary>
    public Surface(int width = 3200, int height = 800, bool indexed = true)
    {
        Width = width;
        Height = height;
        _rowPixels = width * Scale;
        _stride = _rowPixels * 4;
        _pixels = new byte[_stride * height * Scale];
        for (int i = 3; i < _pixels.Length; i += 4)
            _pixels[i] = 255;
        if (indexed)
            _index = new short[_rowPixels * height * Scale];
    }

    /// <summary>Width in script pixels.</summary>
    public int Width { get; }

    /// <summary>Height in script pixels.</summary>
    public int Height { get; }

    /// <summary>Whether the surface stores palette entries (see the class).</summary>
    public bool Indexed => _index is not null;

    /// <summary>
    /// The screen palette: the last CG drawn with <c>PC</c> bit 4 loaded its banks. On an indexed
    /// surface a new palette recolours what is drawn (the screen changes).
    /// </summary>
    public Palette Palette
    {
        get => _palette;
        set
        {
            if (ReferenceEquals(_palette, value))
                return;
            _palette = value;
            if (_index is not null)
                ScreenVersion++;
        }
    }

    /// <summary>Counts the changes to the visible screen; a presenter uploads the screen when it moves.</summary>
    public int ScreenVersion { get; private set; }

    /// <summary>The colour of palette entry <paramref name="index"/> now.</summary>
    public (byte R, byte G, byte B) Color(int index) => _palette[index];

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
        for (int row = 0; row < h; row++)
        {
            if (_index is not null)
            {
                // Indexed (the engine's surfaces): each script pixel's entry into its block of stored pixels.
                int p = Pixel(dx, dy + row, 0);
                for (int col = 0; col < w; col++, p += Scale)
                {
                    byte index = cg.IndexAt(sx + col, sy + row);
                    if (index == key)
                        continue;
                    for (int r = 0, q = p; r < Scale; r++, q += _rowPixels)
                        for (int c = 0; c < Scale; c++)
                            _index[q + c] = index;
                }
                continue;
            }
            for (int col = 0; col < w; col++)
            {
                byte index = cg.IndexAt(sx + col, sy + row);
                if (index == key)
                    continue;
                PlotIndex(dx + col, dy + row, index);
            }
        }
        Touch(dx, dy, w, h);
    }

    /// <summary><c>CC sx, sy, w, h, dx, dy</c>: copies a rectangle (overlap-safe).</summary>
    public void Copy(int sx, int sy, int w, int h, int dx, int dy)
    {
        if (!Clip(ref sx, ref sy, ref w, ref h, ref dx, ref dy, Width, Height))
            return;
        int rows = h * Scale;
        bool upward = dy > sy;
        for (int i = 0; i < rows; i++)
        {
            int row = upward ? rows - 1 - i : i;
            CopyStored(Pixel(sx, sy, row), Pixel(dx, dy, row), w * Scale);
        }
        Touch(dx, dy, w, h);
    }

    /// <summary>
    /// <c>CM sx, sy, sw, sh, dx, dy, dw, dh, mirror</c> (xsystem35 <c>ags_scaledCopyArea</c>): each
    /// rectangle is clipped to the surface on its own (<c>ags_check_param</c>: nothing at all when it
    /// starts right of or below the surface), then the source is stretched over the destination
    /// nearest-neighbour as <c>SDL_SoftStretch</c> does (16.16 steps sampled at their middle) on
    /// script pixels, each moved as its block of stored pixels; <paramref name="mirror"/> bit 1
    /// flips the result vertically, bit 2 horizontally. The source is read before anything is
    /// written.
    /// </summary>
    public void ScaledCopy(int sx, int sy, int sw, int sh, int dx, int dy, int dw, int dh, int mirror) =>
        ScaledCopyFrom(this, sx, sy, sw, sh, dx, dy, dw, dh, mirror);

    /// <summary>
    /// <see cref="ScaledCopy"/> from a rectangle of another surface (same <see cref="Scale"/>): its
    /// pixels arrive as the colours they show now (in the source's palette), as <see cref="CopyFrom"/>.
    /// </summary>
    public void ScaledCopyFrom(Surface source, int sx, int sy, int sw, int sh, int dx, int dy, int dw, int dh, int mirror)
    {
        if (!source.CheckParam(ref sx, ref sy, ref sw, ref sh) || !CheckParam(ref dx, ref dy, ref dw, ref dh))
            return;
        bool same = ReferenceEquals(source, this);
        int sourceRow = sw * Scale;
        var colours = new byte[sourceRow * sh * Scale * 4];
        var indices = same && _index is not null ? new short[sourceRow * sh * Scale] : null;
        for (int row = 0; row < sh * Scale; row++)
        {
            int from = source.Pixel(sx, sy, row);
            if (!same)
            {
                source.ResolveRow(from, sourceRow, colours, row * sourceRow * 4, opaque: false);
                continue;
            }
            Buffer.BlockCopy(_pixels, from * 4, colours, row * sourceRow * 4, sourceRow * 4);
            if (indices is not null)
                Array.Copy(_index!, from, indices, row * sourceRow, sourceRow);
        }
        uint incX = (uint)(((long)sw << 16) / dw), incY = (uint)(((long)sh << 16) / dh);
        uint posY = incY / 2;
        for (int row = 0; row < dh; row++, posY += incY)
        {
            int fromRow = (int)(posY >> 16) * Scale;
            int toRow = dy + ((mirror & 1) != 0 ? dh - 1 - row : row);
            uint posX = incX / 2;
            for (int col = 0; col < dw; col++, posX += incX)
            {
                int fromCol = (int)(posX >> 16) * Scale;
                int toCol = dx + ((mirror & 2) != 0 ? dw - 1 - col : col);
                for (int line = 0; line < Scale; line++)
                {
                    int toLine = toRow * Scale + ((mirror & 1) != 0 ? Scale - 1 - line : line);
                    for (int p = 0; p < Scale; p++)
                    {
                        int at = (fromRow + line) * sourceRow + fromCol + ((mirror & 2) != 0 ? Scale - 1 - p : p);
                        int to = toLine * _rowPixels + toCol * Scale + p;
                        Buffer.BlockCopy(colours, at * 4, _pixels, to * 4, 4);
                        if (_index is not null)
                            _index[to] = indices?[at] ?? Direct;
                    }
                }
            }
        }
        Touch(dx, dy, dw, dh);
    }

    /// <summary>
    /// The visible area's top-left corner: <c>WV x, y, w, h</c> (xsystem35 <c>ags_setViewArea</c>)
    /// shows the surface from there; (0, 0) unless a scene moves it.
    /// </summary>
    public (int X, int Y) View { get; private set; }

    /// <summary><c>WV x, y, …</c>: moves the visible area (its size stays <see cref="ScreenWidth"/> × <see cref="ScreenHeight"/>; kept on the surface).</summary>
    public void SetView(int x, int y)
    {
        View = (Math.Clamp(x, 0, Width - ScreenWidth), Math.Clamp(y, 0, Height - ScreenHeight));
        ScreenVersion++;
    }

    /// <summary>
    /// Copies a rectangle of another surface (same <see cref="Scale"/>) onto this one, as the
    /// colours it shows now (in the source's palette).
    /// </summary>
    public void CopyFrom(Surface source, int sx, int sy, int w, int h, int dx, int dy)
    {
        if (!Clip(ref sx, ref sy, ref w, ref h, ref dx, ref dy, source.Width, source.Height))
            return;
        int count = w * Scale;
        for (int row = 0; row < h * Scale; row++)
        {
            int to = Pixel(dx, dy, row);
            source.ResolveRow(source.Pixel(sx, sy, row), count, _pixels, to * 4, opaque: false);
            if (_index is not null)
                Array.Fill(_index, Direct, to, count);
        }
        Touch(dx, dy, w, h);
    }

    /// <summary>
    /// Blends the visible screen towards black or white by <paramref name="amount"/> (0 leaves
    /// it, 255 is all black or white): the engine's palette fades (<c>PF</c>, <c>PW</c>).
    /// </summary>
    public void FadeScreen(byte amount, bool toWhite)
    {
        if (amount == 0)
            return;
        int target = toWhite ? 255 : 0;
        int count = Math.Min(ScreenWidth, Width) * Scale;
        var line = new byte[count * 4];
        for (int row = 0; row < ScreenHeight * Scale && row < Height * Scale; row++)
        {
            int p = Pixel(View.X, View.Y, row);
            ResolveRow(p, count, line, 0, opaque: false);
            for (int i = 0; i < line.Length; i += 4)
                for (int c = 0; c < 3; c++)
                    line[i + c] = (byte)((line[i + c] * (255 - amount) + target * amount + 127) / 255);
            Buffer.BlockCopy(line, 0, _pixels, p * 4, line.Length);
            if (_index is not null)
                Array.Fill(_index, Direct, p, count);
        }
        ScreenVersion++;
    }

    /// <summary><c>CS sx, sy, w, h, dx, dy, 40</c>: copies a rectangle skipping pixels of index 40.</summary>
    public void CopyKeyed(int sx, int sy, int w, int h, int dx, int dy)
    {
        if (!Clip(ref sx, ref sy, ref w, ref h, ref dx, ref dy, Width, Height))
            return;
        // Source and destination never overlap in the battle; copy through a buffer anyway.
        int count = w * Scale;
        var colours = new byte[count * h * Scale * 4];
        var indices = _index is null ? null : new short[count * h * Scale];
        for (int row = 0; row < h * Scale; row++)
        {
            int from = Pixel(sx, sy, row);
            Buffer.BlockCopy(_pixels, from * 4, colours, row * count * 4, count * 4);
            if (indices is not null)
                Array.Copy(_index!, from, indices, row * count, count);
        }
        for (int row = 0; row < h * Scale; row++)
        {
            int to = Pixel(dx, dy, row);
            for (int i = 0; i < count; i++)
            {
                int at = row * count + i;
                if (indices is not null ? indices[at] == SpriteKey : colours[at * 4 + 3] == KeyAlpha)
                    continue;
                Buffer.BlockCopy(colours, at * 4, _pixels, (to + i) * 4, 4);
                if (indices is not null)
                    _index![to + i] = indices[at];
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
        if (_index is not null)
        {
            for (int row = 0; row < h * Scale; row++)                          // indexed: whole stored rows at once
                _index.AsSpan(Pixel(x, y, row), w * Scale).Fill((short)index);
            Touch(x, y, w, h);
            return;
        }
        for (int row = 0; row < h; row++)
            for (int col = 0; col < w; col++)
                PlotIndex(x + col, y + row, index);
        Touch(x, y, w, h);
    }

    /// <summary>Fills a rectangle with a colour outside the palette (debug drawing, e.g. a masked picture).</summary>
    public void FillColor(int x, int y, int w, int h, (byte R, byte G, byte B) color)
    {
        int sx = x, sy = y;
        if (!Clip(ref sx, ref sy, ref w, ref h, ref x, ref y, Width, Height))
            return;
        for (int row = 0; row < h; row++)
            for (int col = 0; col < w; col++)
                Plot(x + col, y + row, color.R, color.G, color.B, 255);
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

    /// <summary>
    /// <c>CB x, y, w, h, c</c> (<c>ags_drawRectangle</c>): the rectangle clipped to the surface first
    /// (<c>ags_check_param</c>), then its one-pixel outline — a rectangle running past the edge
    /// gets its right or bottom side at the edge.
    /// </summary>
    public void DrawBox(int x, int y, int w, int h, int index)
    {
        if (CheckParam(ref x, ref y, ref w, ref h))
            DrawRectangle(x, y, w, h, index);
    }

    /// <summary>
    /// <c>CL x0, y0, x1, y1, c</c> (<c>ags_drawLine</c>): each end clamped at 0 (nothing when one lies
    /// right of or below the surface), then a line with both ends drawn (<c>SDL_RenderDrawLine</c>,
    /// Bresenham). The renderer draws the entry's colour, which the 8-bit surface stores as the
    /// first entry nearest to it (<c>SDL_FindColor</c>): white 255 may become entry 15.
    /// </summary>
    public void DrawLine(int x0, int y0, int x1, int y1, int index)
    {
        if (x0 >= Width || y0 >= Height || x1 >= Width || y1 >= Height)
            return;
        index = NearestEntry(_palette[index]);
        (x0, y0, x1, y1) = (Math.Max(x0, 0), Math.Max(y0, 0), Math.Max(x1, 0), Math.Max(y1, 0));
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;
        for (int x = x0, y = y0; ; )
        {
            PlotIndex(x, y, index);
            if (x == x1 && y == y1)
                break;
            int e2 = 2 * error;
            if (e2 >= dy) { error += dy; x += sx; }
            if (e2 <= dx) { error += dx; y += sy; }
        }
        Touch(Math.Min(x0, x1), Math.Min(y0, y1), Math.Abs(x1 - x0) + 1, Math.Abs(y1 - y0) + 1);
    }

    /// <summary><c>SDL_FindColor</c>: the first palette entry with the least squared distance to <paramref name="color"/>.</summary>
    private int NearestEntry((byte R, byte G, byte B) color)
    {
        int best = 0, smallest = int.MaxValue;
        for (int i = 0; i < 256; i++)
        {
            var (r, g, b) = _palette[i];
            int distance = (r - color.R) * (r - color.R) + (g - color.G) * (g - color.G) + (b - color.B) * (b - color.B);
            if (distance < smallest)
            {
                best = i;
                smallest = distance;
                if (distance == 0)
                    break;
            }
        }
        return best;
    }

    /// <summary><c>ags_saveRegion</c>: a copy of a rectangle, for <see cref="RestoreRegion"/>.</summary>
    public SavedRegion SaveRegion(int x, int y, int w, int h)
    {
        int sx = x, sy = y, dx = x, dy = y;
        if (!Clip(ref sx, ref sy, ref w, ref h, ref dx, ref dy, Width, Height))
            return new SavedRegion(0, 0, 0, 0, []);
        int count = w * Scale;
        var data = new byte[count * 4 * h * Scale];
        var indices = _index is null ? null : new short[count * h * Scale];
        for (int row = 0; row < h * Scale; row++)
        {
            int from = Pixel(sx, sy, row);
            Buffer.BlockCopy(_pixels, from * 4, data, row * count * 4, count * 4);
            if (indices is not null)
                Array.Copy(_index!, from, indices, row * count, count);
        }
        return new SavedRegion(sx, sy, w, h, data, indices);
    }

    /// <summary><c>ags_restoreRegion</c>: puts a saved rectangle back where it came from.</summary>
    public void RestoreRegion(SavedRegion region)
    {
        int count = region.W * Scale;
        for (int row = 0; row < region.H * Scale; row++)
        {
            int to = Pixel(region.X, region.Y, row);
            Buffer.BlockCopy(region.Data, row * count * 4, _pixels, to * 4, count * 4);
            if (_index is not null)
            {
                if (region.Indices is { } indices)
                    Array.Copy(indices, row * count, _index, to, count);
                else
                    Array.Fill(_index, Direct, to, count);
            }
        }
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
                int p = y * _rowPixels + x;
                var (r, g, b, _) = Resolve(p);
                int o = p * 4;
                _pixels[o] = (byte)((color.R * c + r * (255 - c) + 127) / 255);
                _pixels[o + 1] = (byte)((color.G * c + g * (255 - c) + 127) / 255);
                _pixels[o + 2] = (byte)((color.B * c + b * (255 - c) + 127) / 255);
                _pixels[o + 3] = 255;
                if (_index is not null)
                    _index[p] = Direct;
                touched = true;
            }
        }
        if (touched && px < (View.X + ScreenWidth) * Scale && py < (View.Y + ScreenHeight) * Scale)
            ScreenVersion++;
    }

    /// <summary>The colour at script pixel (x, y) now (top-left stored pixel), with its alpha marker.</summary>
    public (byte R, byte G, byte B, byte A) PixelAt(int x, int y) => StoredPixelAt(x * Scale, y * Scale);

    /// <summary>The colour at stored pixel (px, py) now.</summary>
    public (byte R, byte G, byte B, byte A) StoredPixelAt(int px, int py) => Resolve(py * _rowPixels + px);

    /// <summary>
    /// The visible screen (from <see cref="View"/>) as opaque RGBA rows (<see cref="ScreenWidth"/>·<see cref="Scale"/> ×
    /// <see cref="ScreenHeight"/>·<see cref="Scale"/>), into <paramref name="target"/> when given.
    /// </summary>
    public byte[] ReadScreen(byte[]? target = null) => ReadArea(View.X, View.Y, ScreenWidth, ScreenHeight, target);

    /// <summary>
    /// The area at script pixel (<paramref name="x"/>, <paramref name="y"/>) as opaque RGBA rows
    /// (<paramref name="width"/>·<see cref="Scale"/> × <paramref name="height"/>·<see cref="Scale"/>), into
    /// <paramref name="target"/> when given, from its byte <paramref name="at"/> on (rows of the same width).
    /// </summary>
    public byte[] ReadArea(int x, int y, int width, int height, byte[]? target = null, int at = 0)
    {
        int count = width * Scale;
        target ??= new byte[at + count * 4 * height * Scale];
        for (int row = 0; row < height * Scale; row++)
            ResolveRow(Pixel(x, y, row), count, target, at + row * count * 4, opaque: true);
        return target;
    }

    /// <summary>
    /// <see cref="ReadArea"/> for a layer drawn on the colour key: the pixels of <see cref="SpriteKey"/> (and
    /// the colours stored with its marker) come out transparent, every other one opaque.
    /// </summary>
    public byte[] ReadLayer(int x, int y, int width, int height, byte[]? target = null)
    {
        int count = width * Scale;
        target ??= new byte[count * 4 * height * Scale];
        for (int row = 0; row < height * Scale; row++)
            ResolveRow(Pixel(x, y, row), count, target, row * count * 4, opaque: false);
        for (int i = 3; i < target.Length; i += 4)
            target[i] = target[i] == KeyAlpha ? (byte)0 : (byte)255;
        return target;
    }

    /// <summary>The stored pixel number of row <paramref name="row"/> (stored) below script pixel (x, y).</summary>
    private int Pixel(int x, int y, int row) => (y * Scale + row) * _rowPixels + x * Scale;

    private (byte R, byte G, byte B, byte A) Resolve(int p)
    {
        if (_index is not null && _index[p] >= 0)
        {
            int i = _index[p];
            var (r, g, b) = _palette[i];
            return (r, g, b, i == SpriteKey ? KeyAlpha : (byte)255);
        }
        int o = p * 4;
        return (_pixels[o], _pixels[o + 1], _pixels[o + 2], _pixels[o + 3]);
    }

    /// <summary>Writes <paramref name="count"/> stored pixels from <paramref name="p"/> as RGBA into <paramref name="target"/>.</summary>
    private void ResolveRow(int p, int count, byte[] target, int at, bool opaque)
    {
        Buffer.BlockCopy(_pixels, p * 4, target, at, count * 4);
        if (_index is not null)
        {
            var rgb = _palette.Rgb;
            for (int i = 0; i < count; i++)
            {
                int index = _index[p + i];
                if (index < 0)
                    continue;
                int o = at + i * 4;
                target[o] = rgb[index * 3];
                target[o + 1] = rgb[index * 3 + 1];
                target[o + 2] = rgb[index * 3 + 2];
                target[o + 3] = index == SpriteKey ? KeyAlpha : (byte)255;
            }
        }
        if (opaque)
            for (int i = 3; i < count * 4; i += 4)
                target[at + i] = 255;
    }

    /// <summary>Moves <paramref name="count"/> stored pixels (colours and entries) within the surface.</summary>
    private void CopyStored(int from, int to, int count)
    {
        Buffer.BlockCopy(_pixels, from * 4, _pixels, to * 4, count * 4);
        if (_index is not null)
            Array.Copy(_index, from, _index, to, count);
    }

    /// <summary>Draws palette entry <paramref name="index"/> at script pixel (x, y): stored as the entry, or as its colour now.</summary>
    private void PlotIndex(int x, int y, int index)
    {
        if (_index is null)
        {
            var (r, g, b) = _palette[index];
            Plot(x, y, r, g, b, index == SpriteKey ? KeyAlpha : (byte)255);
            return;
        }
        int p = Pixel(x, y, 0);
        for (int dy = 0; dy < Scale; dy++, p += _rowPixels)
            for (int dx = 0; dx < Scale; dx++)
                _index[p + dx] = (short)index;
    }

    /// <summary>Draws a colour at script pixel (x, y) (a <see cref="Direct"/> pixel).</summary>
    private void Plot(int x, int y, byte r, byte g, byte b, byte a)
    {
        int p = Pixel(x, y, 0);
        for (int dy = 0; dy < Scale; dy++, p += _rowPixels)
        {
            for (int dx = 0; dx < Scale; dx++)
            {
                int o = (p + dx) * 4;
                _pixels[o] = r;
                _pixels[o + 1] = g;
                _pixels[o + 2] = b;
                _pixels[o + 3] = a;
                if (_index is not null)
                    _index[p + dx] = Direct;
            }
        }
    }

    private void Touch(int x, int y, int w, int h)
    {
        var (vx, vy) = View;
        if (x < vx + ScreenWidth && y < vy + ScreenHeight && x + w > vx && y + h > vy)
            ScreenVersion++;
    }

    /// <summary>xsystem35 <c>ags_check_param</c>: clips a rectangle to the surface; false when nothing is left or it starts beyond the right or bottom edge.</summary>
    private bool CheckParam(ref int x, ref int y, ref int w, ref int h)
    {
        if (x >= Width || y >= Height)
            return false;
        if (x < 0) { w += x; x = 0; }
        if (y < 0) { h += y; y = 0; }
        if (x + w > Width) w = Width - x;
        if (y + h > Height) h = Height - y;
        return w > 0 && h > 0;
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

/// <summary>
/// A rectangle saved by <see cref="Surface.SaveRegion"/> (script coordinates, stored pixels: the
/// colours, and the palette entries of an indexed surface).
/// </summary>
public sealed record SavedRegion(int X, int Y, int W, int H, byte[] Data, short[]? Indices = null);
