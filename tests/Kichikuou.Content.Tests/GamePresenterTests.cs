using Kichikuou.Content.Graphics;
using Kichikuou.Content.Scripts;
using Kichikuou.Content.Strategy;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The strategic presenter (<see cref="GamePresenter"/>) on whole runs of the player's game,
/// headless: every event is shown and the screen composed at every input, the way a UI does, and
/// the player's choices go through the presenter's own hit tests (the picker grid, the menu).
/// Skipped without the decompile.
/// </summary>
public class GamePresenterTests(ITestOutputHelper output)
{
    /// <summary>
    /// Week 9 of a new game played through <see cref="GameSession"/>: Sill's CG scene, Pillage on
    /// province 34 with the deployment picker and the battle, a talk picked in the subordinate
    /// picker, a harem visit, the hideout, a save into slot 2 on the save screen, Turn END; week 10:
    /// the subordinate picker left with End (its file leaves it open, the map covers it), Turn END.
    /// Every text resolves and every screen is drawn; no picker outlives the map.
    /// </summary>
    [Fact]
    public async Task A_prologue_week_is_drawn_through_the_presenter()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var run = await RunAsync(stopAtWeek: 11);
        var player = run.Player;
        player.Map.Enqueue(Gesture.Province(34));                           // Pillage → Sﾌﾞﾀｲ
        player.Pickers.Enqueue((PickerKind.Deployment, PickerArea.Cell, 1)); // Rance,
        player.Pickers.Enqueue((PickerKind.Deployment, PickerArea.Button, 1)); // Attack
        player.Map.Enqueue(Gesture.Menu(2));                                // Subordinates → Sﾌﾞｶ
        player.Pickers.Enqueue((PickerKind.Subordinates, PickerArea.Cell, 3)); // Bound
        player.Map.Enqueue(Gesture.Menu(1));                                // Harem → Sﾊｰﾚﾑ
        player.Pickers.Enqueue((PickerKind.Harem, PickerArea.Cell, 1));     // Sill
        player.Map.Enqueue(Gesture.Dungeon(1));                             // the hideout
        player.Map.Enqueue(Gesture.Menu(4));                                // Load/Save:
        player.Answers["ﾛｰﾄﾞｾｰﾌﾞ.ADV"] = new([2, 1, 0, 3]);                 // Save, slot "1", back, Don't
        player.Map.Enqueue(Gesture.Menu(5));                                // Turn END
        player.Map.Enqueue(Gesture.Menu(2));                                // week 10: Subordinates,
        player.Pickers.Enqueue((PickerKind.Subordinates, PickerArea.Button, 3)); // End (Sﾌﾞｶ stays open, the map covers it)
        player.Map.Enqueue(Gesture.Menu(5));                                // Turn END

        await Assert.ThrowsAsync<WeekOverException>(() => run.Session.NewGameAsync());

        var p = run.Presenter;
        output.WriteLine($"composed {player.Composed}, pointer reads {player.Reads}, menus {player.Menus}, pickers {string.Join(",", player.PickersSeen)}");
        Assert.Empty(p.Unresolved.Concat(p.Battle.Unresolved).Distinct());
        Assert.Empty(player.Map);
        Assert.Empty(player.Pickers);
        Assert.Equal(0, player.Pending);
        Assert.Contains(2, run.Saves.Slots);                                // the save screen saved
        Assert.Equal([PickerKind.Deployment, PickerKind.Subordinates, PickerKind.Harem], player.PickersSeen.Distinct());
        Assert.True(player.BattleFrames > 0, "the battle was drawn");
        Assert.True(player.WindowTexts > 10, "message windows were drawn with text");
        Assert.All(player.MenuTexts, t => Assert.NotEmpty(t.Trim()));         // every menu item has its text
        Assert.Equal(11, run.Session.State.Week.Value);
    }

    /// <summary>
    /// <c>ｵｰﾌﾟﾆﾝｸﾞ</c> run on the booted game: its drawing commands run on the stage (CGs off screen,
    /// copies onto the screen, fills), its texts resolve, and the screen shows pictures.
    /// </summary>
    [Fact]
    public async Task The_opening_draws_on_the_stage()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var run = await RunAsync(stopAtWeek: 0);

        await run.Session.Script.CallFileAsync("ｵｰﾌﾟﾆﾝｸﾞ.ADV");

        var p = run.Presenter;
        output.WriteLine($"composed {run.Player.Composed}, lit frames {run.Player.LitFrames}");
        Assert.Empty(p.Unresolved);
        Assert.True(run.Player.LitFrames > 10, "the opening's pictures reached the screen");
    }

    /// <summary>
    /// <see cref="GamePresenter.MaskEventCgs"/>: while the full-screen window is open the stage is a
    /// grey placeholder; off (the default) and with any other window the stage shows as drawn.
    /// </summary>
    [Fact]
    public void The_debug_mask_hides_the_stage_under_the_full_screen_window()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        var state = new GameState();
        presenter.Bind(state);
        presenter.Show(new EngineEvent("CF", [(Sys3Int)0, (Sys3Int)0, (Sys3Int)640, (Sys3Int)400, (Sys3Int)15]));
        var drawn = presenter.Stage.PixelAt(320, 100);

        state.Ui_WinType = 31;
        presenter.Invalidate();
        presenter.Compose();
        Assert.Equal(drawn, presenter.Screen.PixelAt(320, 100));                // off by default

        presenter.MaskEventCgs = true;
        presenter.Invalidate();
        presenter.Compose();
        Assert.Equal(((byte)96, (byte)96, (byte)96, (byte)255), presenter.Screen.PixelAt(320, 100));

        state.Ui_WinType = 11;
        presenter.Invalidate();
        presenter.Compose();
        Assert.Equal(drawn, presenter.Screen.PixelAt(320, 100));
    }

    /// <summary>
    /// <c>PF</c>/<c>PW</c> last <c>rate * 16 * 1000 / 60</c> ms as xsystem35's <c>ags_fadeIn</c>/<c>ags_fadeOut</c>
    /// (<c>ags.c</c>), not <c>rate</c> × 16 ms: the scenes' <c>PF1 FadeSpeed</c> (4) takes 1066 ms.
    /// </summary>
    [Fact]
    public void A_fade_lasts_as_long_as_the_engines()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        presenter.Bind(new GameState());

        presenter.Show(new EngineEvent("PF1", [(Sys3Int)4]));
        var fadeOut = presenter.TakeFade();
        Assert.Equal(new FadeRequest(0, 255, false, 4), fadeOut);
        Assert.Equal(TimeSpan.FromMilliseconds(1066), fadeOut!.Duration);

        presenter.Show(new EngineEvent("PW0", [(Sys3Int)1]));
        Assert.Equal(TimeSpan.FromMilliseconds(266), presenter.TakeFade()!.Duration);
    }

    /// <summary>
    /// <c>Iｼﾚｲﾌﾞ.ADV:78-89</c>: <c>M.ADV</c> 1 scrolls the map with full-screen copies of the view,
    /// which wipe <c>WAO_SUB</c>'s pictures although their variables still hold them; only the
    /// right portrait is drawn again (<c>321</c>), so Rance and the centre picture stay gone.
    /// Skipped without the player's CGs.
    /// </summary>
    [Fact]
    public void A_map_scroll_wipes_the_pictures_until_they_are_drawn_again()
    {
        if (!File.Exists(Path.Combine(LocalData.CgDirectory, "CG1008.png")))
            return;
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(LocalData.CgDirectory), new BoxGlyphs());
        var state = new GameState { Ui_PortraitL = 1, Ui_PortraitR = 4, Ui_CenterCg = 1008 };
        presenter.Bind(state);
        bool Shown(int x, int y, int w, int h)
        {
            presenter.Compose();
            for (int py = y; py < y + h; py += 2)
                for (int px = x; px < x + w; px += 2)
                    if (presenter.Screen.PixelAt(px, py) != presenter.Stage.PixelAt(px, py))
                        return true;
            return false;
        }
        bool Left() => Shown(0, 0, 176, 256);
        bool Right() => Shown(448, 144, 192, 256);
        bool Centre() => Shown(200, 120, 64, 64);
        Assert.True(Left() && Right() && Centre());

        presenter.Show(new MapEvent(1));
        presenter.Show(new PauseEvent((Sys3Int)5));                      // a scroll step: the view over the whole screen
        Assert.False(Left() || Right() || Centre());

        presenter.Show(new WindowEvent((Sys3Int)321, (Sys3Int)4));       // Soul drawn again
        Assert.True(Right());
        Assert.False(Left() || Centre());

        presenter.Show(new WindowEvent((Sys3Int)311, (Sys3Int)1));       // a later 311 draws Rance again
        Assert.True(Left());
    }

    /// <summary>
    /// <c>M.ADV</c> 10 blinks the target mark after its selection sound (<c>PCMPLAY</c>): the sound
    /// does not end the animation, and the first pause draws the mark at (<c>Tmp0025</c>, <c>Tmp0026</c>).
    /// </summary>
    [Fact]
    public void The_map_mark_blinks_after_the_selection_sound()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        var state = new GameState { Tmp0025 = 100, Tmp0026 = 120 };
        presenter.Bind(state);
        // The mark's cell off screen (CG 814 draws there; the player's files are not needed here).
        presenter.Show(new EngineEvent("CF", [(Sys3Int)640, Sys3Int.Zero, (Sys3Int)32, (Sys3Int)32, (Sys3Int)15]));
        var mark = presenter.Stage.PixelAt(640, 0);
        Assert.NotEqual(mark, presenter.Stage.PixelAt(110, 130));

        presenter.Show(new MapEvent(10));
        presenter.Show(new SoundEvent(Sys3Int.Zero, (Sys3Int)109));
        presenter.Show(new PauseEvent((Sys3Int)8));

        Assert.Equal(mark, presenter.Stage.PixelAt(110, 130));
    }

    private sealed record Run(GameSession Session, GamePresenter Presenter, Player Player, StoppingSaves Saves);

    /// <summary>A booted session whose view is a presenter and whose input plays through it.</summary>
    private static async Task<Run> RunAsync(int stopAtWeek)
    {
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var presenter = new GamePresenter(new ScenarioText(LocalData.ScenarioDirectory), new CgLibrary(LocalData.CgDirectory),
            new BoxGlyphs(), scripts) { MaskEventCgs = Environment.GetEnvironmentVariable("KICHIKUOU_SHOTS_DIR") is not null };
        var player = new Player(state, presenter);
        var saves = new StoppingSaves(stopAtWeek);
        var session = new GameSession(state, new QueuedRandom { DefaultToOne = true }, player, player, scripts, scripts, saves,
            new RecordingRules { State = state });
        presenter.Bind(state);
        await session.BootAsync();
        return new Run(session, presenter, player, saves);
    }

    private sealed class WeekOverException : Exception;

    /// <summary>Save slots in memory; the autosave of week <c>stopAtWeek</c> ends the run.</summary>
    private sealed class StoppingSaves(int stopAtWeek) : ISaveStore
    {
        private readonly MemorySaveStore _slots = new();

        public IEnumerable<int> Slots => _slots.Slots;

        public SaveData? Read(int slot) => _slots.Read(slot);

        public bool Write(int slot, SaveData data)
        {
            if (slot == 1 && data.Variables[517].Value == stopAtWeek)            // Week
                throw new WeekOverException();
            return _slots.Write(slot, data);
        }

        public DateTime? Timestamp(int slot) => _slots.Timestamp(slot);
    }

    /// <summary>A map gesture: a province or a dungeon clicked, or the command menu's item.</summary>
    private sealed record Gesture(int ProvinceId, int DungeonId, int MenuItem)
    {
        public static Gesture Province(int p) => new(p, 0, 0);

        public static Gesture Dungeon(int d) => new(0, d, 0);

        public static Gesture Menu(int item) => new(0, 0, item);
    }

    /// <summary>
    /// The player as a UI on the presenter: the view shows every event and the screen is composed
    /// at every input; map gestures are queued as pointer gestures when the map loop starts
    /// waiting (<see cref="MapScreenEvent"/>), picker choices go through
    /// <see cref="PickerView.HitTest"/>, menus are shown and answered by file.
    /// </summary>
    private sealed class Player(GameState state, GamePresenter presenter) : IGameInput, IScriptView
    {
        private readonly PointerScript _pointer = new();
        private readonly Queue<int> _commands = new();
        private int _timer;

        public Queue<Gesture> Map { get; } = new();

        public Queue<(PickerKind Kind, PickerArea Area, int Number)> Pickers { get; } = new();

        public Dictionary<string, Queue<int>> Answers { get; } = new(StringComparer.Ordinal);

        public List<PickerKind> PickersSeen { get; } = [];

        public List<string> MenuTexts { get; } = [];

        public int Composed { get; private set; }

        public int Reads { get; private set; }

        public int Menus { get; private set; }

        public int BattleFrames { get; private set; }

        public int WindowTexts { get; private set; }

        public int LitFrames { get; private set; }

        public int Pending => _pointer.Pending;

        public Task ShowAsync(ScriptEvent e)
        {
            presenter.Show(e);
            if (e is MapScreenEvent { Waiting: true })
                Assert.Null(presenter.Picker);                                  // the map is drawn over every picker
            if (e is MapScreenEvent { Waiting: true } && _pointer.Pending == 0 && Map.TryDequeue(out var g))
            {
                // The camera is still the centre here (COMMAND.ADV:51-58 turns it into the corner).
                if (g.MenuItem != 0)
                {
                    _pointer.RightClick(5, 6);
                    _commands.Enqueue(g.MenuItem);
                }
                else
                {
                    var (x, y) = g.ProvinceId != 0 ? (state.Provs[g.ProvinceId].MapX, state.Provs[g.ProvinceId].MapY)
                        : (state.Dgns[g.DungeonId].MapX, state.Dgns[g.DungeonId].MapY);
                    var at = PointerScript.OnScreen(state, x, y) ?? throw new InvalidOperationException($"({x}, {y}) is not in view.");
                    _pointer.Click(at.X, at.Y);
                }
            }
            if (e is PickerScreenEvent { Open: true } open)
                PickersSeen.Add(open.Kind);
            return Task.CompletedTask;
        }

        private static readonly string? Shots = Environment.GetEnvironmentVariable("KICHIKUOU_SHOTS_DIR");
        private int _shot;

        /// <summary>With <c>KICHIKUOU_SHOTS_DIR</c> set, the screen as a PPM file there (for looking at the drawing).</summary>
        private void Shot(string what)
        {
            if (Shots is null)
                return;
            Directory.CreateDirectory(Shots);
            var rgba = presenter.Screen.ReadScreen();
            using var file = File.Create(Path.Combine(Shots, $"{_shot++:D4}-{what}.ppm"));
            var header = System.Text.Encoding.ASCII.GetBytes("P6 640 400 255 ");
            file.Write(header);
            for (int y = 0; y < 400; y++)
                for (int x = 0; x < 640; x++)
                {
                    int o = (y * 2 * 1280 + x * 2) * 4;
                    file.Write(rgba, o, 3);
                }
        }

        /// <summary>What a UI does before it waits: finish the drawing and compose the screen.</summary>
        private void Frame()
        {
            presenter.Flush();
            if (!presenter.Compose())
                return;
            Composed++;
            if (presenter.InBattle)
                BattleFrames++;
            if (!presenter.WindowText.IsEmpty)
                WindowTexts++;
            var (r, g, b, _) = presenter.Screen.PixelAt(320, 200);
            if (r + g + b > 0)
                LitFrames++;
        }

        public Task<MouseState> GetMouseAsync()
        {
            if (++Reads > 20_000)
                throw new InvalidOperationException("The pointer was read 20000 times.");
            Frame();
            if (presenter.Picker is { } picker && _pointer.Pending == 0 && Pickers.TryPeek(out var pick) && pick.Kind == picker.Kind)
            {
                Pickers.Dequeue();
                var at = pick.Area == PickerArea.Cell ? PointerScript.PickerCell(pick.Number) : PointerScript.PickerButton(pick.Number);
                Assert.Equal((pick.Area, pick.Number), picker.HitTest(at.X, at.Y));   // the drawn screen agrees with the script's
                _pointer.Click(at.X, at.Y);
                Shot("picker");
            }
            return Task.FromResult(_pointer.Next(out _));
        }

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            _pointer.Interrupt();
            Menus++;
            presenter.OpenMenu(items);
            Frame();
            MenuTexts.AddRange(presenter.MenuItems!);
            Shot("menu");
            string file = items[0].At.File;
            int choice = file switch
            {
                "COMMAND.ADV" => _commands.Dequeue(),
                "Cﾃｷﾄｼ.ADV" => items.Select((item, i) => (item, i)).Single(x => x.item.At.Line == 70).i + 1,
                _ when Answers.TryGetValue(file, out var answers) && answers.Count > 0 => answers.Dequeue(),
                _ => 1,
            };
            presenter.CloseMenu();
            return Task.FromResult(choice);
        }

        public Task<Sys3Int> WaitForKeyAsync()
        {
            _pointer.Interrupt();
            Frame();
            Shot("key");
            presenter.AfterKey();
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            _pointer.Interrupt();
            Frame();
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
        {
            _pointer.Interrupt();
            Frame();
            var direct = menu.Find(FigtSelCommand.Direct);
            return Task.FromResult(direct is { Arguments.Count: > 0 }
                ? new FigtSelChoice(FigtSelCommand.Direct, direct.Arguments[0])
                : new FigtSelChoice(FigtSelCommand.Defend, menu.Find(FigtSelCommand.Defend)!.Arguments[0]));
        }

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
        {
            _pointer.Interrupt();
            Frame();
            return Task.FromResult<Sys3Int?>(menu.Options[0].Code);
        }

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => Task.FromResult((Sys3Int)1);

        public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult(Sys3Int.Zero);

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() =>
            Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)2026, (Sys3Int)10, (Sys3Int)9, (Sys3Int)12, Sys3Int.Zero, Sys3Int.Zero, (Sys3Int)6]);

        public Task ResetTimerAsync(Sys3Int value)
        {
            _timer = value.Value;
            return Task.CompletedTask;
        }

        /// <summary><c>ZT n</c>: 100 ticks pass on every read (the opening times its pictures).</summary>
        public Task<Sys3Int> GetTimerAsync(int mode)
        {
            _timer += 100;
            Frame();
            return Task.FromResult((Sys3Int)(_timer & 0xffff));
        }

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() =>
            Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)999, (Sys3Int)999, (Sys3Int)999, (Sys3Int)999]);

        public Task<Sys3Int> GetMidiTrackAsync() => Task.FromResult(Sys3Int.Zero);
    }
}
