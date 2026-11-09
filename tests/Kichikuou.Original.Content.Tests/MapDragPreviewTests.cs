using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.TestSupport;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The map's drag drawn every frame (docs/ui/command-screen.md, "The drag drawn every frame").</summary>
public class MapDragPreviewTests(ITestOutputHelper output)
{
    /// <summary>
    /// <c>WAO_MAP</c> op 1 run by the port on random cameras and pointers — presses, moves, releases, other buttons, presses
    /// while the view coasts. Wherever the script waits (at a read, in a step's pause), the corner the preview draws for the
    /// read that comes next (coasting, with the whole step's time gone) is the view the script shows once it has taken that
    /// read; for a release, once it has taken the coasting's first read too. After another button has put the camera back
    /// the run checks nothing more (the original's picture stays where the drag was, its camera does not).
    /// </summary>
    [Fact]
    public async Task Each_step_lands_where_the_preview_drew_it()
    {
        int checkedDrags = 0, checkedCoasts = 0, steps = 0;
        for (int seed = 1; seed <= 500; seed++)
        {
            var rng = new Random(seed);
            var state = new GameState();
            state.MapCamX = (Sys3Int)rng.Next(0, 1281);
            state.MapCamY = (Sys3Int)rng.Next(0, 401);
            state.Ui_DragPending = (Sys3Int)rng.Next(0, 2);
            state.Ui_DragX = (Sys3Int)rng.Next(0, 640);
            state.Ui_DragY = (Sys3Int)rng.Next(0, 400);
            state.Ui_ScrollWait = (Sys3Int)rng.Next(0, 3);
            var rig = new Rig(state, seed);
            var script = new OriginalScriptContext(state, new NoRandom(), rig, rig, new NoHooks());
            try
            {
                await script.WaoMapCallAsync(1);
            }
            catch (InputBudgetException)
            {
            }
            rig.Settle();
            checkedDrags += rig.CheckedDrags;
            checkedCoasts += rig.CheckedCoasts;
            steps += rig.Steps;
        }
        output.WriteLine($"{steps} steps; {checkedDrags} drag previews and {checkedCoasts} coasting previews checked");
        Assert.True(checkedDrags > 2000);
        Assert.True(checkedCoasts > 500);
    }

    [Fact]
    public void While_the_view_coasts_the_picture_goes_on_towards_the_next_step()
    {
        var s = new GameState();
        s.MapCamX = (Sys3Int)500;
        s.MapCamY = (Sys3Int)200;
        s.Ui_DragPending = (Sys3Int)1;
        var preview = new MapDragPreview();
        preview.Observe(new WaoMapEvent((Sys3Int)1), s, 0);
        preview.Read(0, s);                                         // released: the coasting comes
        Assert.True(preview.Ahead);
        (s.Tmp0021, s.Tmp0022, s.Tmp0023, s.Tmp0024, s.Tmp0028) = ((Sys3Int)40, Sys3Int.Zero, Sys3Int.Zero, (Sys3Int)20, (Sys3Int)3);
        preview.Reading();
        Assert.Equal((540, 180), preview.Corner(s, (0, 0), 0, now: 0.5, stepSeconds: 0.05));     // the first step: the whole way
        preview.Read(0, s);
        (s.MapCamX, s.MapCamY) = ((Sys3Int)540, (Sys3Int)180);                                   // its step
        preview.Observe(new EngineEvent("CC", [(Sys3Int)1820, (Sys3Int)180, (Sys3Int)640, (Sys3Int)400, Sys3Int.Zero, Sys3Int.Zero]), s, 1.0);
        preview.Observe(new PauseEvent((Sys3Int)2), s, 1.0);
        // In the pause the differences are still the step's; the next one is half of it (+20, −10).
        Assert.Equal((540, 180), preview.Corner(s, (0, 0), 0, now: 1.0, stepSeconds: 0.05));
        Assert.Equal((550, 175), preview.Corner(s, (0, 0), 0, now: 1.025, stepSeconds: 0.05));
        Assert.Equal((560, 170), preview.Corner(s, (0, 0), 0, now: 1.05, stepSeconds: 0.05));
        Assert.Equal((560, 170), preview.Corner(s, (0, 0), 0, now: 3.0, stepSeconds: 0.05));     // never past it
        Assert.Equal((540, 180), preview.Corner(s, (0, 0), 16, now: 1.025, stepSeconds: 0.05));  // a press: the step's view
    }

    [Fact]
    public void Outside_the_drag_it_draws_nothing()
    {
        var s = new GameState();
        s.MapCamX = (Sys3Int)300;
        var preview = new MapDragPreview();
        Assert.Null(preview.Corner(s, (10, 10), 16, 0, 0));
        preview.Observe(new WaoMapEvent((Sys3Int)1), s, 0);         // no anchor from COMMAND: the first read is it
        Assert.Null(preview.Corner(s, (10, 10), 16, 0, 0));
        preview.Read(0, s);                                         // nothing held: the call is over
        Assert.Null(preview.Corner(s, (10, 10), 16, 0, 0));
        s.Ui_DragPending = (Sys3Int)1;
        preview.Observe(new WaoMapEvent((Sys3Int)1), s, 0);
        Assert.NotNull(preview.Corner(s, (10, 10), 16, 0, 0));
        preview.Observe(new MapScreenEvent(false), s, 0);           // anything but its steps ends it
        Assert.Null(preview.Corner(s, (10, 10), 16, 0, 0));
    }

    /// <summary>The script's view and input: the pointer's reads drawn ahead, so the preview sees the read that comes next.</summary>
    private sealed class Rig(GameState state, int seed) : UnexpectedInput, IScriptView
    {
        private readonly MapDragPreview _preview = new();
        private readonly Random _rng = new(seed * 31 + 7);
        private readonly List<MouseState> _reads = [];
        private readonly List<Expected> _expected = [];
        private int _taken, _x = 320, _y = 200, _button = 16, _cursor = 1;
        private (int X, int Y) _view = (state.MapCamX.Value, state.MapCamY.Value);
        private bool _snapped;

        public int CheckedDrags { get; private set; }
        public int CheckedCoasts { get; private set; }
        public int Steps { get; private set; }

        private sealed record Expected(int Read, bool Release, bool Drag, (int X, int Y) Corner);

        public Task ShowAsync(ScriptEvent e)
        {
            _preview.Observe(e, state, 0);
            if (e is EngineEvent { Command: "CC" } copy && MapDragPreview.IsStep(copy.Args))
            {
                _view = (copy.Args[0].Value - 1280, copy.Args[1].Value);
                Steps++;
            }
            return Task.CompletedTask;
        }

        public override Task<MouseState> GetMouseAsync()
        {
            if (_taken > 200)
                throw new InputBudgetException();
            _preview.Reading();
            Settle();
            Predict();
            var read = Upcoming();
            bool dragging = !_preview.Ahead && _preview.Corner(state, (0, 0), 16, 0, 0) is not null;
            if (dragging && read.Button != 0 && read.Button != 16)
                _snapped = true;                                    // another button puts the camera back, not the picture
            _taken++;
            _preview.Read(read.Button.Value, state);
            return Task.FromResult(read);
        }

        public override Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            Predict();
            return Task.FromResult(Sys3Int.Zero);
        }

        public override Task<Sys3Int> SetCursorAsync(Sys3Int shape)
        {
            int old = _cursor;
            _cursor = shape.Value;
            return Task.FromResult((Sys3Int)old);
        }

        /// <summary>The checks whose reads the script has taken: the view it shows now is theirs.</summary>
        public void Settle()
        {
            foreach (var e in _expected.Where(e => e.Read < _taken).ToList())
            {
                _expected.Remove(e);
                if (_snapped || e.Release && _reads[e.Read].Button != 0)
                    continue;                                       // a press at once after the release: no coasting
                Assert.True(e.Corner == _view, $"seed {seed}: the preview drew {e.Corner}, the script shows {_view} after read {e.Read}");
                if (e.Drag)
                    CheckedDrags++;
                else
                    CheckedCoasts++;
            }
        }

        private void Predict()
        {
            var next = Upcoming();
            int buttons = next.Button.Value;
            if (_preview.Corner(state, (next.X.Value, next.Y.Value), buttons, 0, 0) is not { } corner)
                return;
            bool drag = !_preview.Ahead;
            bool release = drag && buttons == 0;                    // the coasting's first step comes with the read after it
            _expected.Add(new Expected(_taken + (release ? 1 : 0), release, drag, corner));
        }

        private MouseState Upcoming()
        {
            while (_reads.Count <= _taken + 1)
            {
                int r = _rng.Next(100);
                _button = r < 75 ? _button : r < 88 ? 0 : r < 96 ? 16 : _rng.Next(2) == 0 ? 32 : 48;
                if (_rng.Next(3) != 0)
                {
                    _x = Math.Clamp(_x + _rng.Next(-80, 81), 0, 639);
                    _y = Math.Clamp(_y + _rng.Next(-50, 51), 0, 399);
                }
                _reads.Add(new MouseState((Sys3Int)_button, (Sys3Int)_x, (Sys3Int)_y));
            }
            return _reads[_taken];
        }
    }
}
