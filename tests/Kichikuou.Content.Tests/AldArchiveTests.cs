using System.Text;
using Kichikuou.Content.Archives;

namespace Kichikuou.Content.Tests;

/// <summary>The ALD reader on archives written here in the layout of xsys35c's <c>ald_write</c>.</summary>
public sealed class AldArchiveTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("ald-tests-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    /// <summary>A resource to write: its volume (0 for a gap), name and data.</summary>
    private sealed record Resource(int Volume, string Name = "", byte[]? Data = null);

    [Fact]
    public void Reads_two_volumes_through_the_first_volumes_link_table()
    {
        Resource[] resources =
        [
            new(1, "SET.SCO", [1, 2, 3]),
            new(0),
            new(2, "CG0003.PMS", [.. Enumerable.Range(0, 600).Select(i => (byte)i)]),
            new(1, "Fourth.dat", []),
        ];
        WriteSet("TESTA.ALD", resources, 1);
        WriteSet("TESTB.ALD", resources, 2);

        var archive = AldArchive.Open(_dir, "TEST");

        Assert.Equal(256 / 3, archive.Count); // one sector of link table
        Assert.Equal([0, 2, 3], archive.Entries.Select(e => e.Index));
        Assert.Null(archive[1]);
        Assert.Null(archive[4]);
        Assert.Null(archive[archive.Count]);
        Assert.Null(archive[-1]);
        Assert.Equal([1, 2, 3], archive[0]!.ReadAllBytes());
        Assert.Equal(2, archive[2]!.Volume);
        Assert.Equal(resources[2].Data, archive[2]!.ReadAllBytes());
        Assert.Empty(archive[3]!.ReadAllBytes());
        Assert.Equal("CG0003.PMS", Encoding.ASCII.GetString(archive[2]!.RawName.Span));
        Assert.Equal(new DateTime(2014, 9, 11, 0, 0, 0, DateTimeKind.Utc), archive[0]!.Written);
    }

    [Fact]
    public void Finds_ascii_names_without_case()
    {
        WriteSet("WA.ALD", [new(1, "R5FEF01.WAV", [9]), new(1, "R5FEF02.WAV", [8])], 1);

        var archive = AldArchive.Open(_dir, "w");

        Assert.Same(archive[1], archive.Find("r5fef02.wav"));
        Assert.Null(archive.Find("R5FEF03.WAV"));
        Assert.Null(archive.Find("R5FEF0.WAV"));
    }

    [Fact]
    public void A_volume_the_link_table_names_must_be_there()
    {
        Resource[] resources = [new(1, "A.DAT", [1]), new(2, "B.DAT", [2])];
        WriteSet("TESTA.ALD", resources, 1);

        var e = Assert.Throws<AldFormatException>(() => AldArchive.Open(_dir, "TEST"));
        Assert.Contains("volume B", e.Message);
    }

    [Fact]
    public void The_first_volume_must_be_there()
    {
        WriteSet("TESTB.ALD", [new(2, "B.DAT", [2])], 2);

        Assert.Throws<AldFormatException>(() => AldArchive.Open(_dir, "TEST"));
    }

    [Fact]
    public void A_file_that_is_no_archive_is_refused()
    {
        string sized = Path.Combine(_dir, "SIZEA.ALD");
        File.WriteAllBytes(sized, new byte[300]);
        string signed = Path.Combine(_dir, "SIGNA.ALD");
        File.WriteAllBytes(signed, new byte[256 + 16]);

        Assert.Contains("file size", Assert.Throws<AldFormatException>(() => AldArchive.Open(_dir, "SIZE")).Message);
        Assert.Contains("signature", Assert.Throws<AldFormatException>(() => AldArchive.Open(_dir, "SIGN")).Message);
    }

    /// <summary>Writes one volume of a set as <c>ald_write</c> does (xsys35c 1.13 <c>common/ald.c:68-113</c>).</summary>
    private void WriteSet(string fileName, IReadOnlyList<Resource> resources, int volume)
    {
        var mine = resources.Where(r => r.Volume == volume).ToList();
        using var ald = new MemoryStream();
        int sector = 0;
        void Pointer(int size)
        {
            sector += (size + 0xff) >> 8;
            ald.Write([(byte)sector, (byte)(sector >> 8), (byte)(sector >> 16)]);
        }
        void Pad()
        {
            while ((ald.Length & 0xff) != 0)
                ald.WriteByte(0);
        }

        Pointer((mine.Count + 2) * 3);
        Pointer(resources.Count * 3);
        foreach (var r in mine)
            Pointer(HeaderSize(r) + r.Data!.Length);
        Pad();

        var link = new int[256];
        foreach (var r in resources)
        {
            if (r.Volume != 0)
                link[r.Volume]++;
            ald.Write([(byte)r.Volume, (byte)link[r.Volume], (byte)(link[r.Volume] >> 8)]);
        }
        Pad();

        long fileTime = new DateTime(2014, 9, 11, 0, 0, 0, DateTimeKind.Utc).ToFileTimeUtc();
        foreach (var r in mine)
        {
            int header = HeaderSize(r);
            ald.Write(BitConverter.GetBytes(header));
            ald.Write(BitConverter.GetBytes(r.Data!.Length));
            ald.Write(BitConverter.GetBytes(fileTime));
            ald.Write(Encoding.ASCII.GetBytes(r.Name));
            ald.Write(new byte[header - 16 - r.Name.Length]);
            ald.Write(r.Data);
            Pad();
        }

        ald.Write(BitConverter.GetBytes(0x14c4e));
        ald.Write(BitConverter.GetBytes(0x10));
        ald.Write(BitConverter.GetBytes(mine.Count << 8 | volume));
        ald.Write(BitConverter.GetBytes(0));
        File.WriteAllBytes(Path.Combine(_dir, fileName), ald.ToArray());
    }

    private static int HeaderSize(Resource r) => (r.Name.Length + 1 + 31) & ~0xf;
}
