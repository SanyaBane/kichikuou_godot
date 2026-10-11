using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The music room, <c>MMODE</c> (milestone 7 step 3; docs/strategy/survey-gallery.md §3), run from
/// the player's files on the booted title with a scripted mouse: a click on one of the 28 titles
/// plays its track (<c>SS index + 1</c>), Stop (or a right click) silences it, End leaves — silent,
/// the scratch the file saved restored (<c>US</c>/<c>UG</c>). The time display polls <c>SC</c>
/// (answered "nothing playing"). Skipped without the decompile.
/// </summary>
public class MusicRoomTests
{
    private const string MusicRoom = "MMODE.ADV";

    /// <summary>The middle of cell <paramref name="index"/> of the 3 × 11 grid (<c>MMODE.ADV:216-230</c>): 0-27 the tracks, 30 Stop, 32 End.</summary>
    private static (int X, int Y) Cell(int index) => (328 + 108 * (index % 3) + 54, 18 + 32 * (index / 3) + 16);

    private static void Click(EndGame game, int index)
    {
        var (x, y) = Cell(index);
        game.Input.Pointer.Click(x, y);
    }

    private static List<int> Music(EndGame game) =>
        [.. game.View.Events.OfType<MusicEvent>().Select(m => (int)m.Track.Value)];

    /// <summary>
    /// The room opens silent (<c>:5</c>); the first title plays track 1, Stop silences it, End puts
    /// the title back and leaves with <c>SS 0</c> (<c>:32-33</c>, <c>:69-79</c>). Each click plays sound
    /// 108 (<c>:248-252</c>); the time
    /// display read <c>SC</c>; <c>RetVal</c>, <c>CallArg</c> and <c>Tmp0001-0040</c> come back as they were.
    /// </summary>
    [Fact]
    public async Task A_title_plays_its_track_Stop_silences_it_and_End_leaves_the_scratch_as_it_was()
    {
        var game = await EndGame.BootAsync(newGame: false);
        if (game is null)
            return;
        int musicBefore = Music(game).Count;
        var s = game.State;
        (s.RetVal, s.CallArg, s.Tmp0001, s.Tmp0020, s.Tmp0021, s.Tmp0040) =
            ((Sys3Int)7, (Sys3Int)9, (Sys3Int)11, (Sys3Int)20, (Sys3Int)21, (Sys3Int)40);
        Click(game, 0);
        Click(game, 30);
        Click(game, 32);

        await game.Script.CallFileAsync(MusicRoom);

        Assert.Equal([0, 1, 0, 0], Music(game).Skip(musicBefore));
        Assert.Equal(3, game.View.Events.Count(e => e is SoundEvent { Sound.Value: 108 }));
        Assert.True(game.Input.CdReads > 0);
        Assert.Equal((7, 9, 11, 20, 21, 40), ((int)s.RetVal.Value, (int)s.CallArg.Value, (int)s.Tmp0001.Value,
            (int)s.Tmp0020.Value, (int)s.Tmp0021.Value, (int)s.Tmp0040.Value));
        Assert.Equal(1, game.Input.Pointer.Pending);                    // End acts on the press: the release is the caller's
    }

    /// <summary>
    /// Stop with nothing playing — a right click first — draws "track 999": <c>F1</c> reads past the
    /// end of the 224 names, an empty string in the port (STR-44), and the room goes on.
    /// </summary>
    [Fact]
    public async Task Stop_with_nothing_playing_reads_past_the_names_and_goes_on()
    {
        var game = await EndGame.BootAsync(newGame: false);
        if (game is null)
            return;
        int musicBefore = Music(game).Count;
        game.Input.Pointer.RightClick(500, 200);
        Click(game, 25);
        Click(game, 32);

        await game.Script.CallFileAsync(MusicRoom);

        Assert.Equal([0, 0, 26, 0], Music(game).Skip(musicBefore));
        Assert.Equal(3, game.View.Events.Count(e => e is SoundEvent { Sound.Value: 108 }));
    }
}
