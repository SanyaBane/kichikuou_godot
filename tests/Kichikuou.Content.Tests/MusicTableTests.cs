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
    public void The_missing_tracks_are_the_listed_files_not_in_the_folder()
    {
        File.WriteAllLines(Path.Combine(_dir, "_inmm.ini"), [@"bgm\a.mp3", "", @"bgm\b.mp3", @"bgm\c.mp3"]);
        Directory.CreateDirectory(Path.Combine(_dir, "bgm"));
        File.WriteAllBytes(Path.Combine(_dir, "bgm", "b.mp3"), [0]);

        var music = new MusicTable(_dir);

        string a = Path.Combine("bgm", "a.mp3"), b = Path.Combine("bgm", "b.mp3"), c = Path.Combine("bgm", "c.mp3");
        Assert.Equal([a, b, c], music.Listed);
        Assert.Equal([a, c], music.Missing());
        Assert.Equal("2 of the 3 tracks are missing", MusicNote.Of(music)!.Title);
    }

    private static string Bgm(int track) => Path.Combine("bgm", $"kichiku_{track}.mp3");

    private static readonly string[] Translation = [.. Enumerable.Range(2, 28).Select(Bgm)];

    [Fact]
    public void No_note_when_every_track_is_there()
    {
        Assert.Null(MusicNote.Of(Translation, []));
    }

    [Fact]
    public void Every_track_missing_is_no_music_found()
    {
        var note = MusicNote.Of(Translation, Translation)!;

        Assert.Equal("No music found", note.Title);
        Assert.Equal($"No {Bgm(2)} … kichiku_29.mp3 in the folder: the game plays without music. "
            + "Copy the bgm folder of your copy here to hear it.", note.Body);
    }

    [Fact]
    public void Some_tracks_missing_are_counted_and_named()
    {
        var one = MusicNote.Of(Translation, [Bgm(7)])!;
        Assert.Equal("1 of the 28 tracks is missing", one.Title);
        Assert.Equal($"{Bgm(7)} is not in the folder: the game plays the others and is silent where it would play.", one.Body);

        var three = MusicNote.Of(Translation, [Bgm(7), Bgm(12), Bgm(20)])!;
        Assert.Equal("3 of the 28 tracks are missing", three.Title);
        Assert.Equal($"{Bgm(7)}, kichiku_12.mp3 and kichiku_20.mp3 are not in the folder: the game plays the others and is "
            + "silent where these would play.", three.Body);

        var five = MusicNote.Of(Translation, [Bgm(7), Bgm(12), Bgm(20), Bgm(21), Bgm(29)])!;
        Assert.Equal("5 of the 28 tracks are missing", five.Title);
        Assert.StartsWith($"{Bgm(7)}, kichiku_12.mp3, kichiku_20.mp3 and 2 more are not in the folder", five.Body);
    }

    [Fact]
    public void The_translations_playlist_is_not_checked_as_a_game_file()
    {
        Assert.DoesNotContain(GameFiles.EnglishTranslation10.Files,
            f => string.Equals(f.File, MusicTable.PlaylistName, StringComparison.OrdinalIgnoreCase));
    }
}
