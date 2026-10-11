using Kichikuou.Core.Script;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The opening (<c>ｵｰﾌﾟﾆﾝｸﾞ</c>, at the prologue's end) under the message skip, which skips it whole
/// (the owner's request 2026-10-11, <see cref="MessageSkip.WholeScenes"/>, <see cref="EngineTimer"/>):
/// run from the player's files on a booted game, once watched (a timer read every 1/60 s) and once
/// skipped (one tick a read, no frame), it loads the same CGs — so the gallery's locks come out the
/// same — and makes the same calls. Skipped without the decompile.
/// </summary>
public class OpeningSkipTests(ITestOutputHelper output)
{
    private sealed record Run(EndGame Game, int Frames);

    private static async Task<Run?> RunOpeningAsync(bool skipping)
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return null;
        game.Input.Timer = new EngineTimer();
        game.Input.Skipping = skipping;
        await game.Script.CallFileAsync("ｵｰﾌﾟﾆﾝｸﾞ.ADV");
        return new Run(game, game.Input.TimerFrames);
    }

    /// <summary>How many times each CG was loaded by a <c>G</c>, by CG.</summary>
    private static Dictionary<int, int> GLoads(EndGame game) =>
        game.View.Events.OfType<EngineEvent>()
            .Where(e => e.Command == "G" && e.Args[0].Value != 0)
            .GroupBy(e => (int)e.Args[0].Value)
            .ToDictionary(g => g.Key, g => g.Count());

    [Fact]
    public async Task The_skipped_opening_loads_what_a_watched_one_does()
    {
        var watched = await RunOpeningAsync(skipping: false);
        if (watched is null)
            return;
        var skipped = await RunOpeningAsync(skipping: true);
        Assert.NotNull(skipped);
        output.WriteLine($"frames: watched {watched.Frames}, skipped {skipped.Frames}");

        Assert.Equal(GLoads(watched.Game).OrderBy(p => p.Key), GLoads(skipped.Game).OrderBy(p => p.Key));
        Assert.All(Enumerable.Range(0, 2000), cg =>
            Assert.Equal(watched.Game.State.CgSeen[cg].CgSeen.Value, skipped.Game.State.CgSeen[cg].CgSeen.Value));
        Assert.Equal(1, skipped.Game.State.F_OpeningSeen.Value);
        Assert.Equal(watched.Game.Calls, skipped.Game.Calls);

        Assert.InRange(watched.Frames, 60 * 125, 60 * 135);           // about 130 s watched
        Assert.Equal(0, skipped.Frames);
    }
}
