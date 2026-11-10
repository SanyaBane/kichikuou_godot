using Kichikuou.Content.Audio;

namespace Kichikuou.Content.Tests;

/// <summary>The music's playlist (docs/assets.md §5) on folders written here: the copy's own, else the translation's.</summary>
public sealed class MusicTableTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("music-table-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void A_copy_without_the_playlist_plays_the_translations()
    {
        var music = new MusicTable(_dir);

        Assert.False(music.FromPlaylist);
        Assert.Equal(28, music.Count);
        Assert.Null(music.PathOf(0));
        Assert.Equal(Path.Combine(_dir, "bgm", "kichiku_2.mp3"), music.PathOf(1));
        Assert.Equal(Path.Combine(_dir, "bgm", "kichiku_29.mp3"), music.PathOf(28));
        Assert.Null(music.PathOf(29));
    }

    [Fact]
    public void A_copys_own_playlist_plays_as_it_lists()
    {
        // another case of the name, a trailing space and an empty line, as copies may have them
        File.WriteAllLines(Path.Combine(_dir, "_INMM.INI"), [@"music\a.mp3 ", "", @"music\c.mp3"]);

        var music = new MusicTable(_dir);

        Assert.True(music.FromPlaylist);
        Assert.Equal(3, music.Count);
        Assert.Equal(Path.Combine(_dir, "music", "a.mp3"), music.PathOf(1));
        Assert.Null(music.PathOf(2));
        Assert.Equal(Path.Combine(_dir, "music", "c.mp3"), music.PathOf(3));
        Assert.Null(music.PathOf(4));
    }

    [Fact]
    public void The_translations_playlist_is_not_checked_as_a_game_file()
    {
        Assert.DoesNotContain(GameFiles.EnglishTranslation10.Files,
            f => string.Equals(f.File, MusicTable.PlaylistName, StringComparison.OrdinalIgnoreCase));
    }
}
