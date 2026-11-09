using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.TestSupport;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The camera's scroll to a target drawn every frame (docs/ui/command-screen.md, "The scroll drawn every frame").</summary>
public class MapScrollGlideTests(ITestOutputHelper output)
{
    /// <summary>
    /// <c>M.ADV</c> op 1 run by the port on random cameras and targets (outside the scroll's range too, short ways, none),
    /// the screen's frames played through each step's pause at 30, 60 and 144 a second: every pause is a step of the glide;
    /// each frame draws a view on the way from the view before the step (the camera's before the first) to the step's,
    /// never past it and never back; the pause's first frame still shows the view before, the frame it ends at shows the
    /// step's; once the pause is over and when the scroll is, the screen shows the script's own view.
    /// </summary>
    [Fact]
    public async Task Each_step_glides_from_the_view_before_to_its_own()
    {
        int scrolls = 0, steps = 0, frames = 0;
        for (int seed = 1; seed <= 400; seed++)
        {
            var rng = new Random(seed);
            int Near(int at, int max) => Math.Clamp(at + rng.Next(-30, 31), 0, max);
            var state = new GameState { CallArg = 1 };
            state.MapCamX = (Sys3Int)rng.Next(0, 2001);
            state.MapCamY = (Sys3Int)rng.Next(0, 901);
            state.MapTargetX = (Sys3Int)(rng.Next(3) == 0 ? Near(state.MapCamX.Value, 2000) : rng.Next(0, 2001));
            state.MapTargetY = (Sys3Int)(rng.Next(3) == 0 ? Near(state.MapCamY.Value, 900) : rng.Next(0, 901));
            double frame = new[] { 1.0 / 30, 1.0 / 60, 1.0 / 144 }[rng.Next(3)];
            var rig = new Rig(state, frame, seed);
            var script = new OriginalScriptContext(state, new NoRandom(), rig, rig, new NoHooks());

            await script.MapCallAsync(1);
            await rig.ShowAsync(new MapEvent((Sys3Int)10));             // what comes next ends it

            Assert.Equal(rig.Pauses, rig.Glided);
            Assert.Null(rig.Glide.Corner(rig.Clock, frame, 0.016));
            if (rig.Pauses > 0)
            {
                Assert.Equal(rig.ViewShown, Corner(state.MapCamX, state.MapCamY));    // the last step's is the script's final view
                scrolls++;
            }
            steps += rig.Pauses;
            frames += rig.Frames;
        }
        output.WriteLine($"{scrolls} scrolls, {steps} steps, {frames} frames checked");
        Assert.True(scrolls > 300);
    }

    [Fact]
    public void A_step_s_pause_glides_a_frame_at_a_time()
    {
        // From (640, 200) to (100, 200): the view's corner from 320 by steps of 21; the first step's is 299.
        var glide = Started(640, 200, 100, 200);
        var state = new GameState { MapCamX = 619, MapCamY = 200 };
        glide.Observe(new PauseEvent((Sys3Int)5), state, 10.0);
        Assert.True(glide.Gliding);
        double frame = 1.0 / 60;                                        // 80 ms: five frames
        int?[] xs = [.. Enumerable.Range(0, 6).Select(j => glide.Corner(10.0 + j * frame, frame, 0.016)?.X)];
        Assert.Equal([320, 316, 312, 307, 303, null], xs);              // null: the step's own view, 299
        Assert.Equal((316, 0), glide.Corner(10.0 + frame, frame, 0.016));
        Assert.Null(glide.Corner(10.0 + frame, frame, 0));              // pauses that take no time: nothing to glide
        glide.Waited();
        Assert.Null(glide.Corner(10.0 + frame, frame, 0.016));          // the pause is over: the step's view stands
    }

    [Fact]
    public void Only_the_scroll_s_own_steps_glide()
    {
        var glide = new MapScrollGlide();
        var state = new GameState { MapCamX = 619, MapCamY = 200 };
        glide.Observe(new PauseEvent((Sys3Int)5), state, 0);            // no scroll
        Assert.False(glide.Gliding);

        glide = Started(640, 200, 100, 200);
        state.MapCamX = 600;                                            // not the first step's camera (619)
        glide.Observe(new PauseEvent((Sys3Int)5), state, 0);
        Assert.False(glide.Gliding);
        state.MapCamX = 619;
        glide.Observe(new PauseEvent((Sys3Int)5), state, 0);
        Assert.False(glide.Gliding);                                    // that ended it

        glide = Started(640, 200, 100, 200);
        glide.Observe(new SoundEvent((Sys3Int)0, (Sys3Int)109), state, 0);
        glide.Observe(new PauseEvent((Sys3Int)5), state, 0);
        Assert.False(glide.Gliding);                                    // anything else ends it

        glide = Started(800, 300, 800, 300);                            // no way to go: no step
        state.MapCamX = 800;
        state.MapCamY = 300;
        glide.Observe(new PauseEvent((Sys3Int)5), state, 0);
        Assert.False(glide.Gliding);
    }

    [Fact]
    public void A_pause_takes_the_frames_the_screen_waits_it()
    {
        Assert.Equal(5.0 / 60, MapScrollGlide.PauseSeconds(0.08, 1.0 / 60), 9);
        Assert.Equal(2.0 / 30, MapScrollGlide.PauseSeconds(0.08, 1.0 / 30), 9);
        Assert.Equal(12.0 / 144, MapScrollGlide.PauseSeconds(0.08, 1.0 / 144), 9);
        Assert.Equal(1.0 / 20, MapScrollGlide.PauseSeconds(0.016, 1.0 / 20), 9);   // one frame at least
        Assert.Equal(0, MapScrollGlide.PauseSeconds(0, 1.0 / 60));
    }

    /// <summary>
    /// The presenter draws the glide's view in the screen it composes, outside a battle and in one, while a scroll waits
    /// in a step's pause: the stage keeps the step's view, as the original's; outside a battle
    /// <see cref="GamePresenter.MapViewShown"/> follows the picture; the next event ends it, and outside a scroll nothing
    /// is drawn.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_screen_shows_the_glide_s_view_and_the_stage_the_step_s(bool inBattle)
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        var state = new GameState { MapCamX = 700, MapCamY = 300, MapTargetX = 900, MapTargetY = 300 };
        presenter.Bind(state);
        Tiles(presenter);
        if (inBattle)
            presenter.Show(new BattleScreenEvent("FIGT_A.ADV", true));
        void ShownIs((int X, int Y) corner)
        {
            if (!inBattle)                                              // the battle's map is its own presenter's
                Assert.Equal(corner, presenter.MapViewShown);
        }
        presenter.ShowScrollView((385, 100));
        presenter.Compose();
        Assert.NotEqual((385, 100), presenter.MapViewShown);           // no scroll: nothing drawn

        presenter.Show(new MapEvent((Sys3Int)1));
        state.MapCamX = 713;                                            // its first step: 200 in fifteenths
        presenter.Show(new PauseEvent((Sys3Int)5));
        presenter.Compose();
        AssertShows(presenter, (393, 100));
        ShownIs((393, 100));

        presenter.ShowScrollView((385, 100));
        presenter.Compose();
        AssertShows(presenter, (385, 100));
        ShownIs((385, 100));
        AssertStage(presenter, (393, 100));                             // the stage as the original's

        presenter.ShowScrollView(null);
        presenter.Compose();
        AssertShows(presenter, (393, 100));

        presenter.ShowScrollView((390, 100));
        presenter.Show(new MapEvent((Sys3Int)10));                      // the script goes on
        presenter.Compose();
        AssertShows(presenter, (393, 100));
        presenter.ShowScrollView((390, 100));                           // the scroll is over: ignored
        presenter.Compose();
        AssertShows(presenter, (393, 100));
        ShownIs((393, 100));
    }

    private static MapScrollGlide Started(int camX, int camY, int targetX, int targetY)
    {
        var glide = new MapScrollGlide();
        var state = new GameState { MapCamX = (Sys3Int)camX, MapCamY = (Sys3Int)camY, MapTargetX = (Sys3Int)targetX, MapTargetY = (Sys3Int)targetY };
        glide.Observe(new MapEvent((Sys3Int)1), state, 0);
        return glide;
    }

    private static (int X, int Y) Corner(Sys3Int camX, Sys3Int camY)
    {
        var (x, y) = OriginalScriptContext.MapViewCornerOf(camX, camY);
        return (x.Value, y.Value);
    }

    /// <summary>The tile area filled with 16-pixel blocks of the initial palette's distinct colours, in a pattern that does not repeat nearby.</summary>
    private static void Tiles(GamePresenter presenter)
    {
        int[] colours = [1, 2, 3, 4, 5, 6, 7, 8, 9, 246, 247, 248, 249, 250, 251, 252, 253, 254];
        for (int bx = 0; bx < 120; bx++)
            for (int by = 0; by < 50; by++)
                presenter.Show(new EngineEvent("CF", [(Sys3Int)(1280 + bx * 16), (Sys3Int)(by * 16), (Sys3Int)16, (Sys3Int)16,
                    (Sys3Int)colours[(bx * 7 + by * 11 + bx * by) % colours.Length]]));
    }

    private static readonly (int X, int Y)[] Samples = [(0, 0), (5, 3), (101, 57), (333, 211), (600, 380), (639, 399), (17, 390), (620, 9)];

    private static void AssertShows(GamePresenter presenter, (int X, int Y) corner)
    {
        foreach (var (x, y) in Samples)
            Assert.Equal(presenter.Stage.PixelAt(corner.X + 1280 + x, corner.Y + y), presenter.Screen.PixelAt(x, y));
    }

    private static void AssertStage(GamePresenter presenter, (int X, int Y) corner)
    {
        foreach (var (x, y) in Samples)
            Assert.Equal(presenter.Stage.PixelAt(corner.X + 1280 + x, corner.Y + y), presenter.Stage.PixelAt(x, y));
    }

    /// <summary>The script's view and pauses: each pause is played as the screen's frames, the glide checked at each.</summary>
    private sealed class Rig(GameState state, double frame, int seed) : UnexpectedInput, IScriptView
    {
        private const double Tick = 0.016;
        private (int X, int Y) _before;

        public MapScrollGlide Glide { get; } = new();
        public double Clock { get; private set; } = 100;
        public int Pauses { get; private set; }
        public int Glided { get; private set; }
        public int Frames { get; private set; }

        /// <summary>The view the script shows: the corner of its camera at its last step, before the scroll the camera's.</summary>
        public (int X, int Y) ViewShown { get; private set; }

        public Task ShowAsync(ScriptEvent e)
        {
            if (e is MapEvent { Mode.Value: 1 })
                _before = ViewShown = Corner(state.MapCamX, state.MapCamY);
            Glide.Observe(e, state, Clock);
            if (e is PauseEvent)
            {
                ViewShown = Corner(state.MapCamX, state.MapCamY);        // the presenter draws the step at the pause
                Pauses++;
                if (Glide.Gliding)
                    Glided++;
            }
            return Task.CompletedTask;
        }

        public override Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            double seconds = MapScrollGlide.PauseSeconds(ticks.Value * Tick, frame);
            int n = (int)Math.Round(seconds / frame);
            var (from, to) = (_before, ViewShown);
            int left = int.MaxValue;
            for (int j = 0; j <= n; j++)
            {
                var drawn = Glide.Corner(Clock + j * frame, frame, Tick) ?? ViewShown;
                Assert.True(Within(drawn.X, from.X, to.X) && Within(drawn.Y, from.Y, to.Y),
                    $"seed {seed}: frame {j} of {n} drew {drawn}, not between {from} and {to}");
                int way = Math.Abs(to.X - drawn.X) + Math.Abs(to.Y - drawn.Y);
                Assert.True(way <= left, $"seed {seed}: frame {j} went back to {drawn}");
                left = way;
                if (j == 0)
                    Assert.Equal(from, drawn);
                Frames++;
            }
            Assert.Equal(to, Glide.Corner(Clock + n * frame, frame, Tick) ?? ViewShown);   // it arrives as the pause ends
            Clock += seconds;
            Glide.Waited();
            Assert.Null(Glide.Corner(Clock, frame, Tick));
            _before = ViewShown;
            return Task.FromResult(Sys3Int.Zero);
        }

        private static bool Within(int v, int a, int b) => v >= Math.Min(a, b) && v <= Math.Max(a, b);
    }
}
