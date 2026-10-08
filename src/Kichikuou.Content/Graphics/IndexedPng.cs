using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Kichikuou.Content.Graphics;

/// <summary>Thrown for a PNG file this decoder cannot read.</summary>
public sealed class CgFormatException(string message) : Exception(message);

/// <summary>
/// Decodes the extracted CGs (<c>cg_png\CGnnnn.png</c>, written by xsys35c <c>pms.exe</c>) keeping
/// the palette indices, which general-purpose loaders expand to RGB. Reads the 8-bit indexed,
/// non-interlaced PNGs that tool writes: <c>IHDR</c>, <c>PLTE</c>, <c>IDAT</c>, the <c>oFFs</c>
/// default position and the private <c>pmSk</c> palette mask (docs/assets.md §2).
/// </summary>
public static class IndexedPng
{
    private static ReadOnlySpan<byte> Signature => [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Decodes a PNG file.</summary>
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

    /// <summary>Decodes PNG bytes.</summary>
    public static CgImage Decode(ReadOnlySpan<byte> png)
    {
        if (png.Length < 8 || !png[..8].SequenceEqual(Signature))
            throw new CgFormatException("Not a PNG file.");

        int width = 0, height = 0;
        byte[]? palette = null;
        int x = 0, y = 0;
        ushort mask = 0xFFFF;
        var idat = new MemoryStream();
        bool header = false;

        int pos = 8;
        while (true)
        {
            if (pos + 12 > png.Length)
                throw new CgFormatException("Truncated chunk.");
            int length = BinaryPrimitives.ReadInt32BigEndian(png[pos..]);
            if (length < 0 || pos + 12 + length > png.Length)
                throw new CgFormatException("Bad chunk length.");
            string type = Encoding.ASCII.GetString(png.Slice(pos + 4, 4));
            var data = png.Slice(pos + 8, length);
            pos += 12 + length;

            switch (type)
            {
                case "IHDR":
                    width = BinaryPrimitives.ReadInt32BigEndian(data);
                    height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
                    byte depth = data[8], colorType = data[9], interlace = data[12];
                    if (depth != 8 || colorType != 3)
                        throw new CgFormatException($"Only 8-bit indexed PNGs are supported (bit depth {depth}, colour type {colorType}).");
                    if (data[10] != 0 || data[11] != 0 || interlace != 0)
                        throw new CgFormatException("Interlaced or unknown compression/filter method.");
                    header = true;
                    break;
                case "PLTE":
                    if (length % 3 != 0 || length > 768)
                        throw new CgFormatException("Bad PLTE chunk.");
                    palette = new byte[768];
                    data.CopyTo(palette);
                    break;
                case "IDAT":
                    idat.Write(data);
                    break;
                case "oFFs":
                    // int32 x, int32 y, unit (0 = pixels).
                    if (length == 9 && data[8] == 0)
                    {
                        x = BinaryPrimitives.ReadInt32BigEndian(data);
                        y = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
                    }
                    break;
                case "pmSk":
                    // pms.exe's private chunk: the PMS palette bank mask, written only when it is not
                    // 0xFFFF. Both CGs that carry it have 0, so the byte order is untested; read as the
                    // PMS header stores it (little-endian).
                    if (length == 2)
                        mask = BinaryPrimitives.ReadUInt16LittleEndian(data);
                    break;
                case "IEND":
                    if (!header || palette is null)
                        throw new CgFormatException("Missing IHDR or PLTE.");
                    return new CgImage(width, height, Unfilter(idat, width, height), palette, x, y, mask);
            }
        }
    }

    private static byte[] Unfilter(MemoryStream idat, int width, int height)
    {
        idat.Position = 0;
        var raw = new byte[(width + 1) * height];
        using (var z = new ZLibStream(idat, CompressionMode.Decompress))
        {
            int read = 0;
            while (read < raw.Length)
            {
                int n = z.Read(raw, read, raw.Length - read);
                if (n == 0)
                    throw new CgFormatException("Pixel data ends early.");
                read += n;
            }
        }

        // One byte per pixel, so the filter's "bytes per pixel" is 1.
        var pixels = new byte[width * height];
        for (int row = 0; row < height; row++)
        {
            byte filter = raw[row * (width + 1)];
            var src = raw.AsSpan(row * (width + 1) + 1, width);
            var cur = pixels.AsSpan(row * width, width);
            var prev = row > 0 ? pixels.AsSpan((row - 1) * width, width) : default;
            for (int i = 0; i < width; i++)
            {
                int a = i > 0 ? cur[i - 1] : 0;
                int b = row > 0 ? prev[i] : 0;
                int c = i > 0 && row > 0 ? prev[i - 1] : 0;
                int predictor = filter switch
                {
                    0 => 0,
                    1 => a,
                    2 => b,
                    3 => (a + b) >> 1,
                    4 => Paeth(a, b, c),
                    _ => throw new CgFormatException($"Unknown filter type {filter}."),
                };
                cur[i] = (byte)(src[i] + predictor);
            }
        }
        return pixels;
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }
}
