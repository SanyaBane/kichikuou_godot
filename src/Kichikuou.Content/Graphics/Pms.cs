using System.Buffers.Binary;

namespace Kichikuou.Content.Graphics;

/// <summary>
/// Decodes an 8-bit PMS file (<c>cg_pms\CGnnnn.PMS</c>, the format inside <c>KICHIKUGA/GB.ALD</c>):
/// the header of <c>pms.c:54-87</c> and the RLE of <c>extract_8bit</c> (<c>pms.c:95-128</c>).
/// Every CG of the game is in this format (docs/assets.md §2).
/// </summary>
public static class Pms
{
    /// <summary>Decodes a PMS file.</summary>
    public static CgImage Load(string path)
    {
        try
        {
            return Decode(File.ReadAllBytes(path));
        }
        catch (CgFormatException e)
        {
            throw new CgFormatException($"{path}: {e.Message}");
        }
    }

    /// <summary>Decodes PMS bytes.</summary>
    public static CgImage Decode(ReadOnlySpan<byte> pms)
    {
        if (pms.Length < 48 || pms[0] != 'P' || pms[1] != 'M')
            throw new CgFormatException("Not a PMS file.");
        if (pms[6] != 8)
            throw new CgFormatException($"Only 8-bit PMS files are supported (bpp {pms[6]}).");
        ushort mask = BinaryPrimitives.ReadUInt16LittleEndian(pms[10..]);
        int x = BinaryPrimitives.ReadInt32LittleEndian(pms[16..]);
        int y = BinaryPrimitives.ReadInt32LittleEndian(pms[20..]);
        int width = BinaryPrimitives.ReadInt32LittleEndian(pms[24..]);
        int height = BinaryPrimitives.ReadInt32LittleEndian(pms[28..]);
        int dataOffset = BinaryPrimitives.ReadInt32LittleEndian(pms[32..]);
        int paletteOffset = BinaryPrimitives.ReadInt32LittleEndian(pms[36..]);
        if (paletteOffset < 0 || paletteOffset + 768 > pms.Length || dataOffset < 0 || dataOffset > pms.Length)
            throw new CgFormatException("Bad data or palette offset.");

        var palette = pms.Slice(paletteOffset, 768).ToArray();
        var pixels = new byte[width * height];
        int b = dataOffset;
        try
        {
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width;)
                {
                    int loc = row * width + col;
                    int c0 = pms[b++];
                    int l;
                    if (c0 <= 0xF7)
                    {
                        pixels[loc] = (byte)c0;
                        col++;
                    }
                    else if (c0 == 0xFF || c0 == 0xFE)
                    {
                        // Copy l pixels from one (FF) or two (FE) lines above.
                        l = pms[b++] + 3;
                        int from = loc - width * (c0 == 0xFF ? 1 : 2);
                        for (int i = 0; i < l; i++)
                            pixels[loc + i] = pixels[from + i];
                        col += l;
                    }
                    else if (c0 == 0xFD)
                    {
                        l = pms[b++] + 4;
                        byte value = pms[b++];
                        pixels.AsSpan(loc, l).Fill(value);
                        col += l;
                    }
                    else if (c0 == 0xFC)
                    {
                        l = (pms[b++] + 3) * 2;
                        byte v0 = pms[b++], v1 = pms[b++];
                        for (int i = 0; i < l; i += 2)
                        {
                            pixels[loc + i] = v0;
                            pixels[loc + i + 1] = v1;
                        }
                        col += l;
                    }
                    else
                    {
                        // F8-FB: the next byte is a literal pixel.
                        pixels[loc] = pms[b++];
                        col++;
                    }
                }
            }
        }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentOutOfRangeException)
        {
            throw new CgFormatException("Pixel data is truncated or runs past the image.");
        }
        return new CgImage(width, height, pixels, palette, x, y, mask);
    }
}
