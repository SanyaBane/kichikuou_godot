using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Kichikuou.Content.Graphics;

namespace Kichikuou.Content.Tests;

public class PaletteTests
{
    [Fact]
    public void Initial_palette_has_the_reserved_colours_of_initPal()
    {
        Assert.Equal(((byte)0, (byte)0, (byte)0), Palette.Initial[0]);
        Assert.Equal(((byte)192, (byte)192, (byte)192), Palette.Initial[7]);
        Assert.Equal(((byte)255, (byte)251, (byte)240), Palette.Initial[246]);
        Assert.Equal(((byte)255, (byte)0, (byte)0), Palette.Initial[249]);
        Assert.Equal(((byte)255, (byte)255, (byte)255), Palette.Initial[255]);
    }

    [Fact]
    public void A_full_mask_loads_entries_10_to_245_only()
    {
        var cg = new CgImage(1, 1, [0], FilledPalette(7));

        var palette = Palette.Initial.Load(cg);

        for (int i = 0; i < 256; i++)
            Assert.Equal(Palette.IsReserved(i) ? Palette.Initial[i] : ((byte)7, (byte)7, (byte)7), palette[i]);
    }

    [Fact]
    public void Mask_bits_select_banks()
    {
        // bit 0 = 10-15, bit 2 = 32-47, bit 15 = 240-245.
        var cg = new CgImage(1, 1, [0], FilledPalette(7), paletteMask: 1 | 4 | 0x8000);

        var palette = Palette.Initial.Load(cg);

        Assert.Equal(((byte)7, (byte)7, (byte)7), palette[10]);
        Assert.Equal(((byte)7, (byte)7, (byte)7), palette[15]);
        Assert.Equal(Palette.Initial[16], palette[16]);
        Assert.Equal(((byte)7, (byte)7, (byte)7), palette[32]);
        Assert.Equal(((byte)7, (byte)7, (byte)7), palette[47]);
        Assert.Equal(Palette.Initial[48], palette[48]);
        Assert.Equal(((byte)7, (byte)7, (byte)7), palette[245]);
        Assert.Equal(Palette.Initial[246], palette[246]);
    }

    [Fact]
    public void An_empty_mask_keeps_the_screen_palette()
    {
        var screen = Palette.Initial.Load(new CgImage(1, 1, [0], FilledPalette(7)));
        var cg = new CgImage(1, 1, [0], FilledPalette(9), paletteMask: 0);

        Assert.Equal(((byte)7, (byte)7, (byte)7), cg.PaletteOver(screen)[100]);
    }

    [Fact]
    public void ToRgba_keys_by_index_not_by_colour()
    {
        var palette = FilledPalette(7); // index 40 and 41 have the same colour
        var cg = new CgImage(2, 1, [40, 41], palette);

        var rgba = cg.ToRgba(transparentIndex: 40);

        Assert.Equal(new byte[] { 7, 7, 7, 0, 7, 7, 7, 255 }, rgba);
        Assert.Equal(255, cg.ToRgba()[3]);
    }

    [Fact]
    public void ToRgba_takes_reserved_colours_from_initPal()
    {
        var cg = new CgImage(1, 1, [246], FilledPalette(7));

        Assert.Equal(new byte[] { 255, 251, 240, 255 }, cg.ToRgba());
    }

    internal static byte[] FilledPalette(byte value) => Enumerable.Repeat(value, 768).ToArray();
}

public class IndexedPngTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Every_filter_type_decodes_to_the_same_indices(byte filter)
    {
        byte[] indices = [1, 5, 9, 200, 3, 3, 250, 0, 40, 7, 128, 255];
        var png = TestPng.Encode(4, 3, indices, filter, offset: (200, 3));

        var cg = IndexedPng.Decode(png);

        Assert.Equal(4, cg.Width);
        Assert.Equal(3, cg.Height);
        Assert.Equal(indices, cg.Indices);
        Assert.Equal((200, 3), (cg.X, cg.Y));
        Assert.Equal(0xFFFF, cg.PaletteMask);
        Assert.Equal(((byte)4, (byte)5, (byte)6), (cg.FilePalette[3], cg.FilePalette[4], cg.FilePalette[5]));
    }

    [Fact]
    public void Missing_oFFs_means_position_zero_and_pmSk_sets_the_mask()
    {
        var png = TestPng.Encode(1, 1, [0], 0, offset: null, mask: 0);

        var cg = IndexedPng.Decode(png);

        Assert.Equal((0, 0), (cg.X, cg.Y));
        Assert.Equal(0, cg.PaletteMask);
    }

    [Fact]
    public void Rejects_non_indexed_images()
    {
        var png = TestPng.Encode(1, 1, [0], 0, offset: null, colorType: 2);

        Assert.Throws<CgFormatException>(() => IndexedPng.Decode(png));
    }
}

public class PmsTests
{
    [Fact]
    public void Decodes_every_RLE_code()
    {
        byte[] rle =
        [
            1, 2, 3, 4, 0xFD, 0, 9,          // literals, FD: 4 × 9
            0xFC, 0, 5, 6, 0xF8, 0xF8, 7,    // FC: 3 × (5 6), F8: escaped literal F8, literal 7
            0xFE, 0, 0xFF, 2,                // FE: 3 from two rows up, FF: 5 from one row up
        ];
        var pms = TestPms.Build(8, 3, rle, x: 10, y: 20);

        var cg = Pms.Decode(pms);

        Assert.Equal(new byte[]
        {
            1, 2, 3, 4, 9, 9, 9, 9,
            5, 6, 5, 6, 5, 6, 0xF8, 7,
            1, 2, 3, 6, 5, 6, 0xF8, 7,
        }, cg.Indices);
        Assert.Equal((10, 20), (cg.X, cg.Y));
        Assert.Equal(0xFFFF, cg.PaletteMask);
    }

    [Fact]
    public void Rejects_truncated_data()
    {
        var pms = TestPms.Build(8, 1, [1, 2, 3], x: 0, y: 0);

        Assert.Throws<CgFormatException>(() => Pms.Decode(pms));
    }
}

/// <summary>Writes small indexed PNGs the way xsys35c <c>pms.exe</c> does (plus test variations).</summary>
internal static class TestPng
{
    public static byte[] Encode(int width, int height, byte[] indices, byte filter, (int X, int Y)? offset,
        ushort? mask = null, byte colorType = 3)
    {
        var output = new MemoryStream();
        output.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);

        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
        ihdr[8] = 8;
        ihdr[9] = colorType;
        Chunk(output, "IHDR", ihdr);

        var plte = new byte[768];
        for (int i = 0; i < plte.Length; i++)
            plte[i] = (byte)(i + 1);
        Chunk(output, "PLTE", plte);

        if (offset is var (x, y))
        {
            var offs = new byte[9];
            BinaryPrimitives.WriteInt32BigEndian(offs, x);
            BinaryPrimitives.WriteInt32BigEndian(offs.AsSpan(4), y);
            Chunk(output, "oFFs", offs);
        }
        if (mask is ushort m)
        {
            var pmsk = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(pmsk, m);
            Chunk(output, "pmSk", pmsk);
        }

        var raw = new MemoryStream();
        for (int row = 0; row < height; row++)
        {
            raw.WriteByte(filter);
            for (int i = 0; i < width; i++)
            {
                int a = i > 0 ? indices[row * width + i - 1] : 0;
                int b = row > 0 ? indices[(row - 1) * width + i] : 0;
                int c = i > 0 && row > 0 ? indices[(row - 1) * width + i - 1] : 0;
                int predictor = filter switch
                {
                    0 => 0,
                    1 => a,
                    2 => b,
                    3 => (a + b) >> 1,
                    _ => Paeth(a, b, c),
                };
                raw.WriteByte((byte)(indices[row * width + i] - predictor));
            }
        }
        var compressed = new MemoryStream();
        using (var z = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            z.Write(raw.ToArray());
        // Split the data over two IDAT chunks, as encoders may.
        var data = compressed.ToArray();
        Chunk(output, "IDAT", data[..(data.Length / 2)]);
        Chunk(output, "IDAT", data[(data.Length / 2)..]);
        Chunk(output, "IEND", []);
        return output.ToArray();
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    private static void Chunk(Stream output, string type, byte[] data)
    {
        var length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);
        output.Write(Encoding.ASCII.GetBytes(type));
        output.Write(data);
        output.Write(new byte[4]); // CRC, not checked by the decoder
    }
}

/// <summary>Builds PMS files with the header layout of <c>pms.c:54-87</c>.</summary>
internal static class TestPms
{
    public static byte[] Build(int width, int height, byte[] rle, int x, int y, ushort mask = 0xFFFF)
    {
        var pms = new byte[48 + 768 + rle.Length];
        pms[0] = (byte)'P';
        pms[1] = (byte)'M';
        pms[2] = 1;
        pms[4] = 48;
        pms[6] = 8;
        BinaryPrimitives.WriteUInt16LittleEndian(pms.AsSpan(10), mask);
        BinaryPrimitives.WriteInt32LittleEndian(pms.AsSpan(16), x);
        BinaryPrimitives.WriteInt32LittleEndian(pms.AsSpan(20), y);
        BinaryPrimitives.WriteInt32LittleEndian(pms.AsSpan(24), width);
        BinaryPrimitives.WriteInt32LittleEndian(pms.AsSpan(28), height);
        BinaryPrimitives.WriteInt32LittleEndian(pms.AsSpan(32), 48 + 768);
        BinaryPrimitives.WriteInt32LittleEndian(pms.AsSpan(36), 48);
        rle.CopyTo(pms, 48 + 768);
        return pms;
    }
}

