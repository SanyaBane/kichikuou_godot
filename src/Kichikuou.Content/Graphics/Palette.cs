namespace Kichikuou.Content.Graphics;

/// <summary>
/// The engine's 256-entry screen palette. Entries 0-9 and 246-255 are the "Windows reserved"
/// colours of <c>initPal</c> (<c>ags.c:58-87</c>), which no CG ever changes; a CG drawn with
/// <c>PC 7</c> loads the banks its mask names, i.e. entries 10-245 for almost every CG
/// (<c>cg.c:415-425</c>, docs/assets.md §3).
/// </summary>
public sealed class Palette
{
    private readonly byte[] _rgb;

    private Palette(byte[] rgb)
    {
        _rgb = rgb;
    }

    /// <summary>The palette after <c>initPal</c>: the reserved entries set, everything else black.</summary>
    public static Palette Initial { get; } = new(InitialRgb());

    /// <summary>The colour of entry <paramref name="index"/>.</summary>
    public (byte R, byte G, byte B) this[int index] => (_rgb[index * 3], _rgb[index * 3 + 1], _rgb[index * 3 + 2]);

    /// <summary>Whether entry <paramref name="index"/> is reserved (0-9, 246-255): CGs never load it.</summary>
    public static bool IsReserved(int index) => index is < 10 or > 245;

    /// <summary>The palette after drawing <paramref name="cg"/> with palette loading on (<c>PC</c> bit 4).</summary>
    public Palette Load(CgImage cg)
    {
        var rgb = (byte[])_rgb.Clone();
        ushort mask = cg.PaletteMask;
        if ((mask & 1) != 0)
            Copy(cg.FilePalette, rgb, 10, 6);
        if ((mask & (1 << 15)) != 0)
            Copy(cg.FilePalette, rgb, 240, 6);
        for (int bank = 1; bank < 15; bank++)
        {
            if ((mask & (1 << bank)) != 0)
                Copy(cg.FilePalette, rgb, bank * 16, 16);
        }
        return new Palette(rgb);
    }

    private static void Copy(byte[] from, byte[] to, int first, int count) =>
        Array.Copy(from, first * 3, to, first * 3, count * 3);

    private static byte[] InitialRgb()
    {
        var rgb = new byte[768];
        void Set(int i, byte r, byte g, byte b)
        {
            rgb[i * 3] = r;
            rgb[i * 3 + 1] = g;
            rgb[i * 3 + 2] = b;
        }
        Set(0, 0, 0, 0);
        Set(1, 128, 0, 0);
        Set(2, 0, 128, 0);
        Set(3, 128, 128, 0);
        Set(4, 0, 0, 128);
        Set(5, 128, 0, 128);
        Set(6, 0, 128, 128);
        Set(7, 192, 192, 192);
        Set(8, 192, 220, 192);
        Set(9, 166, 202, 240);
        // initPal also sets entry 15 to white, but it is not reserved: every CG overwrites it.
        Set(15, 255, 255, 255);
        Set(246, 255, 251, 240);
        Set(247, 160, 160, 164);
        Set(248, 128, 128, 128);
        Set(249, 255, 0, 0);
        Set(250, 0, 255, 0);
        Set(251, 255, 255, 0);
        Set(252, 0, 0, 255);
        Set(253, 255, 0, 255);
        Set(254, 0, 255, 255);
        Set(255, 255, 255, 255);
        return rgb;
    }
}
