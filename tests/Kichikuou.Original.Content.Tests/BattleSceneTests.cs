using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The armies' scene of our battle screen (docs/ui/battle-screen.md, "The scene between exchanges").</summary>
public class BattleSceneTests(ITestOutputHelper output)
{
    // One reader for every capture: loading the CGs again for each is what takes the time.
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    [Fact]
    public void The_commanders_rows_come_halfway_up_and_the_troops_row_stays()
    {
        Assert.Equal(80, BattleScene.Y(80));
        Assert.Equal(190, BattleScene.Y(300));
        Assert.Equal(171, BattleScene.Y(263));
        Assert.Equal(98, BattleScene.Y(117));
        Assert.True(BattleScene.Y(300) + 32 <= BattleScene.HpY);
    }

    /// <summary>
    /// In every captured battle the scene is on from the field's first drawing; when the original's
    /// engagement stands whole with its counters (the slide's last step), the scene's rows 0-223 are the
    /// original's screen there (the HP counters, moved to row 224, aside); between exchanges it shows a
    /// player unit and an enemy commander that stand in the slots; the picture kept under the numbers
    /// changes only with <see cref="BattleScene.PictureVersion"/> (what our screen's sides are blurred from).
    /// </summary>
    [CaptureTheory]
    [MemberData(nameof(Captures))]
    public async Task The_scene_shows_the_engagement_as_the_original_does(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var cgs = Cgs.Value;
        var presenter = new BattlePresenter(Texts.Value, cgs, new BoxGlyphs());
        var scene = new BattleScene(cgs);
        GameState? state = null;
        int compared = 0, between = 0;
        int lastOffset = -1;
        var wrong = new List<string>();
        byte[]? picture = null;
        int pictureVersion = -1, pictures = 0;
        var view = new ObservingView(e =>
        {
            presenter.Show(e);
            scene.Observe(e, state!);
            if (e is BattlefieldBackgroundEvent or CountersEvent)
            {
                // The picture under the numbers (what the screen's sides blur) changes only with its version.
                var now = scene.Surface.ReadArea(0, BattleScene.PictureY, BattleScene.Width, BattleScene.Height);
                bool same = picture is not null && now.AsSpan().SequenceEqual(picture);
                if (scene.PictureVersion == pictureVersion && !same && picture is not null)
                    wrong.Add($"the picture under the numbers changed without its version ({e.GetType().Name})");
                if (scene.PictureVersion != pictureVersion)
                    pictures++;
                picture = now;
                pictureVersion = scene.PictureVersion;
            }
            if (e is EngagementSlideEvent slide)
                lastOffset = slide.Offset.Value;
            if (e is CountersEvent && lastOffset == 0 && scene.InEngagement)
            {
                lastOffset = -1;
                compared++;
                int differ = 0;
                for (int y = 0; y < BattleScene.HpY; y++)
                {
                    for (int x = 0; x < BattleScene.Width; x++)
                    {
                        var (r, g, b, _) = presenter.Surface.PixelAt(x, y);
                        var (r2, g2, b2, _) = scene.Surface.PixelAt(x, y);
                        if ((r, g, b) != (r2, g2, b2))
                            differ++;
                    }
                }
                if (differ > 0)
                    wrong.Add($"engagement {compared}: {differ} pixels differ");
            }
            if (e is BattlefieldBackgroundEvent && state is { } s)
            {
                between++;
                if (!scene.Active)
                    wrong.Add("the field is drawn but the scene is off");
                var (unit, cmdr) = scene.InFocus;
                bool unitIn = BattleBoard.Cards(s, BattleSide.Player).Any(c => c?.Record == unit);
                bool cmdrIn = BattleBoard.Cards(s, BattleSide.Enemy).Any(c => c?.Record == cmdr);
                if ((unit != 0 && !unitIn) || (cmdr != 0 && !cmdrIn))
                    wrong.Add($"redraw {between}: unit {unit} / commander {cmdr} not in a slot");
            }
        });
        var report = await GoldenReplay.RunAsync(capture, view: view, started: s =>
        {
            state = s;
            presenter.Start(s);
            scene.Start(s);
        });
        output.WriteLine($"{file}: {compared} engagements compared, {between} redraws, {pictures} pictures; replay {(report.Passed ? "matches" : "diverges")}");
        Assert.Empty(wrong);
    }

    /// <summary>
    /// Every capture in the full run (<see cref="DifferentialRuns.Full"/>), every eighth in the quick
    /// one: the presenter and the scene draw each battle whole (minutes for all of them).
    /// </summary>
    public static IEnumerable<object[]> Captures =>
        ScenarioTextGoldenTests.Captures.Where((_, i) => DifferentialRuns.Full || i % DifferentialRuns.QuickShare == 0);

    private sealed class ObservingView(Action<ScriptEvent> show) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            show(scriptEvent);
            return Task.CompletedTask;
        }
    }
}
