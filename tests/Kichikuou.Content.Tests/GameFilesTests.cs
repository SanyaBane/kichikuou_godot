using System.Text;

namespace Kichikuou.Content.Tests;

/// <summary>The version check (docs/import.md, step 7) on folders written here, against a table of their own.</summary>
public sealed class GameFilesTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("game-files-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Write(string name, string text)
    {
        string path = Path.Combine(_dir, name);
        File.WriteAllText(path, text, Encoding.ASCII);
        return path;
    }

    private GameVersion Version(params (string Name, string Text)[] files) =>
        new("Test Game v1", [.. files.Select(f => (f.Name, Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(Encoding.ASCII.GetBytes(f.Text)))))]);

    [Fact]
    public void A_folder_of_the_version_is_its_files()
    {
        var version = Version(("GAMESA.ALD", "scenario"), ("System39.ain", "texts"));
        string scenario = Write("gamesa.ald", "scenario"); // another case, as a copy may have it
        Write("System39.ain", "texts");
        Write("other.txt", "anything");

        var folder = GameFiles.Check(_dir, [version]);
        Assert.Same(version, folder.Version);
        Assert.Equal(scenario, folder.Paths["GAMESA.ALD"]);
        Assert.Equal(16, folder.Key.Length);
        Assert.Equal(folder.Key, GameFiles.Check(_dir, [version]).Key);
    }

    [Fact]
    public void A_missing_file_is_named()
    {
        Write("GAMESA.ALD", "scenario");
        var e = Assert.Throws<GameFolderException>(() =>
            GameFiles.Check(_dir, [Version(("GAMESA.ALD", "scenario"), ("System39.ain", "texts"))]));
        Assert.Contains("has no System39.ain", e.Message);
        Assert.Contains("Test Game v1", e.Message);
    }

    [Fact]
    public void Another_file_is_named_with_both_hashes()
    {
        Write("GAMESA.ALD", "another scenario");
        var version = Version(("GAMESA.ALD", "scenario"));
        var e = Assert.Throws<GameFolderException>(() => GameFiles.Check(_dir, [version]));
        Assert.Contains("GAMESA.ALD", e.Message);
        Assert.Contains(version.Files[0].Sha256, e.Message);
        Assert.Contains(GameFiles.Sha256(Path.Combine(_dir, "GAMESA.ALD")), e.Message);
    }

    [Fact]
    public void A_folder_that_is_not_there_is_refused() =>
        Assert.Throws<GameFolderException>(() => GameFiles.Check(Path.Combine(_dir, "none")));
}
