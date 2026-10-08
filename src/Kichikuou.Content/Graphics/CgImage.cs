namespace Kichikuou.Content.Graphics;

/// <summary>
/// One CG as the engine keeps it: 8-bit palette indices, the CG's own palette and its default
/// display position (PMS header x0/y0, the PNG's <c>oFFs</c> chunk). The colours on screen come
/// from the screen <see cref="Palette"/>, which drawing the CG updates (docs/assets.md §3).
/// </summary>
public sealed class CgImage
{
    /// <summary>Creates a CG from decoded data.</summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="indices">Row-major palette indices, <paramref name="width"/> × <paramref name="height"/>.</param>
    /// <param name="palette">The CG's palette: 256 RGB triples (768 bytes), as stored in the file.</param>
    /// <param name="x">Default display x.</param>
    /// <param name="y">Default display y.</param>
    /// <param name="paletteMask">The PMS palette bank mask (which entries a draw loads into the screen palette).</param>
    public CgImage(int width, int height, byte[] indices, byte[] palette, int x = 0, int y = 0, ushort paletteMask = 0xFFFF)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), $"Bad CG size {width}×{height}.");
        if (indices.Length != width * height)
            throw new ArgumentException($"Expected {width * height} indices, got {indices.Length}.", nameof(indices));
        if (palette.Length != 768)
            throw new ArgumentException($"Expected a 768-byte palette, got {palette.Length}.", nameof(palette));
        Width = width;
        Height = height;
        Indices = indices;
        FilePalette = palette;
        X = x;
        Y = y;
        PaletteMask = paletteMask;
    }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>Row-major palette indices.</summary>
    public byte[] Indices { get; }

    /// <summary>The palette stored in the file (256 RGB triples), reserved entries included.</summary>
    public byte[] FilePalette { get; }

    /// <summary>Default display x (<c>G n</c> without a <c>J</c> override, <c>cg.c:203-223</c>).</summary>
    public int X { get; }

    /// <summary>Default display y.</summary>
    public int Y { get; }

    /// <summary>
    /// PMS palette bank mask: bit 0 = entries 10-15, bit 15 = 240-245, bit k = 16k..16k+15
    /// (<c>cg.c:415-425</c>). <c>0xFFFF</c> for all CGs but 396 and 815, which load no palette.
    /// </summary>
    public ushort PaletteMask { get; }

    /// <summary>The palette index at (x, y).</summary>
    public byte IndexAt(int x, int y) => Indices[y * Width + x];

    /// <summary>The screen palette after this CG is drawn with <c>PC 7</c> over <paramref name="screen"/>.</summary>
    public Palette PaletteOver(Palette screen) => screen.Load(this);

    /// <summary>The palette this CG shows with when drawn over the initial screen palette.</summary>
    public Palette OwnPalette => Palette.Initial.Load(this);

    /// <summary>
    /// The CG as RGBA bytes (row-major, 4 bytes per pixel) coloured with <paramref name="palette"/>
    /// (default: <see cref="OwnPalette"/>). Pixels whose index is <paramref name="transparentIndex"/>
    /// get alpha 0 — the colour key of <c>G n, 40</c> is an index, not a colour
    /// (<c>gfx_draw.c:224</c>); everything else is opaque.
    /// </summary>
    public byte[] ToRgba(int? transparentIndex = null, Palette? palette = null)
    {
        palette ??= OwnPalette;
        var rgba = new byte[Indices.Length * 4];
        for (int i = 0; i < Indices.Length; i++)
        {
            byte index = Indices[i];
            var (r, g, b) = palette[index];
            int o = i * 4;
            rgba[o] = r;
            rgba[o + 1] = g;
            rgba[o + 2] = b;
            rgba[o + 3] = index == transparentIndex ? (byte)0 : (byte)255;
        }
        return rgba;
    }
}
