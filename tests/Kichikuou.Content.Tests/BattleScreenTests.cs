using Kichikuou.Content.Battles;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Golden;
using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>Box glyphs: every visible character is a solid rectangle inside its cell.</summary>
internal sealed class BoxGlyphs : IGlyphRenderer
{
    public int Rendered { get; private set; }

    public Glyph? Render(char c, int pixelSize)
    {
        if (c == ' ')
            return null;
        Rendered++;
        int w = MessageWriter.CellWidth(c, pixelSize) - 2, h = pixelSize - 4;
        return new Glyph(w, h, Enumerable.Repeat((byte)255, w * h).ToArray(), 1, 2);
    }
}

public class SurfaceTests
{
    private static CgImage Cg(int w, int h, params byte[] indices)
    {
        var palette = new byte[768];
        for (int i = 0; i < 256; i++)
            palette[i * 3] = (byte)i; // red channel = index
        return new CgImage(w, h, indices, palette);
    }

    [Fact]
    public void Keyed_draw_skips_index_40_and_plain_draw_marks_it()
    {
        var s = new Surface(8, 4);
        s.DrawCg(Cg(2, 1, 40, 50), 0, 0, key: 40);
        Assert.Equal((0, 0, 0, 255), Rgba(s.PixelAt(0, 0)));
        Assert.Equal(50, s.PixelAt(1, 0).R);

        s.DrawCg(Cg(2, 1, 40, 50), 0, 1);
        Assert.Equal(Surface.KeyAlpha, s.PixelAt(0, 1).A);
    }

    [Fact]
    public void Pixels_are_stored_at_scale_and_read_back_opaque()
    {
        var s = new Surface(700, 500);
        s.DrawCg(Cg(1, 1, 40), 3, 2);
        for (int dy = 0; dy < Surface.Scale; dy++)
            for (int dx = 0; dx < Surface.Scale; dx++)
                Assert.Equal(40, s.StoredPixelAt(3 * Surface.Scale + dx, 2 * Surface.Scale + dy).R);
        var screen = s.ReadScreen();
        Assert.Equal(Surface.ScreenWidth * Surface.ScreenHeight * Surface.Scale * Surface.Scale * 4, screen.Length);
        Assert.All(Enumerable.Range(0, screen.Length / 4), i => Assert.Equal(255, screen[i * 4 + 3]));
    }

    [Fact]
    public void Keyed_copy_skips_pixels_drawn_from_index_40()
    {
        var s = new Surface(8, 4);
        s.DrawCg(Cg(2, 1, 40, 60), 0, 0);
        s.DrawCg(Cg(2, 1, 70, 70), 0, 2);
        s.CopyKeyed(0, 0, 2, 1, 0, 2);
        Assert.Equal(70, s.PixelAt(0, 2).R);
        Assert.Equal(60, s.PixelAt(1, 2).R);
    }

    [Fact]
    public void Copy_handles_overlap_and_clips()
    {
        var s = new Surface(8, 8);
        s.DrawCg(Cg(1, 3, 10, 20, 30), 0, 0);
        s.Copy(0, 0, 1, 3, 0, 1);
        Assert.Equal([10, 10, 20, 30], Enumerable.Range(0, 4).Select(y => (int)s.PixelAt(0, y).R));
        s.Copy(0, 0, 4, 4, 6, 6); // clipped to 2×2
        Assert.Equal(10, s.PixelAt(6, 6).R);
        Assert.Equal(10, s.PixelAt(6, 7).R);
    }

    /// <summary>
    /// <c>CM</c> (<c>ags_scaledCopyArea</c> → <c>SDL_SoftStretch</c>): nearest-neighbour with 16.16
    /// steps sampled at their middle, so a 2:1 shrink takes the odd pixels; mirror bit 2 flips
    /// horizontally; a source starting beyond the surface draws nothing.
    /// </summary>
    [Fact]
    public void Scaled_copy_stretches_as_sdl_soft_stretch()
    {
        var s = new Surface(16, 4);
        s.DrawCg(Cg(4, 1, 10, 20, 30, 40), 0, 0);

        s.ScaledCopy(0, 0, 2, 1, 0, 1, 4, 1, 0);                      // grow 2 → 4
        Assert.Equal([10, 10, 20, 20], Enumerable.Range(0, 4).Select(x => (int)s.PixelAt(x, 1).R));
        Assert.Equal(10, s.StoredPixelAt(1, 3).R);                    // whole stored blocks

        s.ScaledCopy(0, 0, 4, 1, 4, 1, 2, 1, 0);                      // shrink 4 → 2
        Assert.Equal([20, 40], Enumerable.Range(4, 2).Select(x => (int)s.PixelAt(x, 1).R));

        s.ScaledCopy(0, 0, 4, 1, 8, 2, 4, 1, 2);                      // mirrored
        Assert.Equal([40, 30, 20, 10], Enumerable.Range(8, 4).Select(x => (int)s.PixelAt(x, 2).R));

        s.ScaledCopy(16, 0, 4, 1, 0, 3, 4, 1, 0);                     // starts beyond: nothing
        Assert.Equal(0, s.PixelAt(0, 3).R);
    }

    /// <summary>
    /// The engine's surface is 8-bit: a palette load recolours what is drawn (and copied) from
    /// palette entries, but not text (direct colours); a surface that is not indexed keeps the
    /// colour of the moment.
    /// </summary>
    [Fact]
    public void An_indexed_surface_shows_its_pixels_in_the_current_palette()
    {
        var first = Cg(1, 1, 50);                                          // entry 50: red 50
        var second = new CgImage(1, 1, [50], Enumerable.Range(0, 768).Select(i => (byte)(i == 150 ? 200 : 0)).ToArray());
        var s = new Surface(8, 4);
        s.DrawCg(first, 0, 0);
        s.Copy(0, 0, 1, 1, 1, 0);
        s.Blend([255, 255, 255, 255], 2, 2, 4, 0, (9, 9, 9));             // text at script pixel (2, 0)
        Assert.Equal(50, s.PixelAt(1, 0).R);
        int version = s.ScreenVersion;

        s.Palette = second.PaletteOver(s.Palette);
        Assert.NotEqual(version, s.ScreenVersion);
        Assert.Equal(200, s.PixelAt(0, 0).R);
        Assert.Equal(200, s.PixelAt(1, 0).R);
        Assert.Equal(9, s.PixelAt(2, 0).R);

        var plain = new Surface(8, 4, indexed: false);
        plain.DrawCg(first, 0, 0);
        plain.Palette = second.PaletteOver(plain.Palette);
        Assert.Equal(50, plain.PixelAt(0, 0).R);
    }

    /// <summary><c>WV x, y, …</c> (<c>ags_setViewArea</c>): the screen is read from the view, and drawing inside it counts as a change.</summary>
    [Fact]
    public void The_view_moves_the_visible_screen()
    {
        var s = new Surface(640, 800);
        s.DrawCg(Cg(1, 1, 90), 5, 500);
        s.SetView(0, 400);
        var screen = s.ReadScreen();
        int at = (100 * Surface.Scale * Surface.ScreenWidth + 5) * Surface.Scale * 4;
        Assert.Equal(90, screen[at]);

        int version = s.ScreenVersion;
        s.Fill(0, 700, 4, 4, 1);
        Assert.NotEqual(version, s.ScreenVersion);
        version = s.ScreenVersion;
        s.Fill(0, 0, 4, 4, 1);                                        // above the view now
        Assert.Equal(version, s.ScreenVersion);
    }

    [Fact]
    public void Save_and_restore_a_region()
    {
        var s = new Surface(8, 8);
        var saved = s.SaveRegion(1, 1, 2, 2);
        s.Fill(0, 0, 8, 8, 255);
        s.RestoreRegion(saved);
        Assert.Equal((0, 0, 0, 255), Rgba(s.PixelAt(1, 1)));
        Assert.Equal(255, s.PixelAt(0, 0).R);
    }

    private static (int, int, int, int) Rgba((byte R, byte G, byte B, byte A) p) => (p.R, p.G, p.B, p.A);
}

public class MessageWriterTests
{
    [Fact]
    public void Cells_lines_and_home_follow_the_engine()
    {
        var s = new Surface(640, 400);
        var w = new MessageWriter(s, new BoxGlyphs());
        w.Open(200, 196, 240, 200);
        Assert.Equal((200, 198), w.Cursor);
        w.Print("ab");
        Assert.Equal((224, 198), w.Cursor);
        w.Print("\n");
        Assert.Equal((200, 224), w.Cursor);
        w.Size = 16;
        w.Print("x\n");
        Assert.Equal((200, 242), w.Cursor);
        w.Print("あ");
        Assert.Equal((216, 242), w.Cursor);
        w.Home();
        Assert.Equal((200, 198), w.Cursor);
    }

    /// <summary>A cell is half-width only for a single Shift-JIS byte: the scenario's <c>×</c> (the used-up Harem) is full-width.</summary>
    [Theory]
    [InlineData('H', 12)]
    [InlineData('ｱ', 12)]
    [InlineData('○', 24)]
    [InlineData('×', 24)]
    [InlineData('÷', 24)]
    [InlineData('§', 24)]
    public void Cell_widths_follow_shift_jis(char c, int width) => Assert.Equal(width, MessageWriter.CellWidth(c, 24));

    [Fact]
    public void Text_is_drawn_in_the_palette_colour_at_high_resolution()
    {
        var s = new Surface(640, 400);
        var w = new MessageWriter(s, new BoxGlyphs()) { Color = 249 };
        w.Open(10, 10, 100, 100);
        w.Print("A");
        // Box glyph: from (cell x·2 + 1, cell top·2 + 2), red (initPal 249).
        Assert.Equal((255, 0, 0), Rgb(s.StoredPixelAt(21, 26)));
        Assert.Equal((0, 0, 0), Rgb(s.StoredPixelAt(20, 26)));
    }

    private static (int, int, int) Rgb((byte R, byte G, byte B, byte A) p) => (p.R, p.G, p.B);
}

public class SelectionBoxTests
{
    [Fact]
    public void Box_geometry_hit_test_and_restore()
    {
        var s = new Surface(640, 400);
        var box = new SelectionBox(s, new BoxGlyphs(), 100, 50, 68, ["Manual", "Auto-Atk", "Auto-Def", "Info"]);
        Assert.Equal((100, 50, 72, 74), box.Box);
        Assert.Equal(-1, box.HitTest(99, 60));
        Assert.Equal(0, box.HitTest(100, 50));
        Assert.Equal(1, box.HitTest(169, 68));
        Assert.Equal(-1, box.HitTest(170, 68));
        Assert.Equal(3, box.HitTest(120, 121));

        box.Open();
        Assert.Equal(255, s.PixelAt(92, 42).R); // the outermost frame line (initPal 255)
        box.Hover(120, 70);
        Assert.Equal(1, box.Hovered);
        box.Close();
        Assert.Equal(0, s.PixelAt(92, 42).R);
        Assert.Equal(0, s.PixelAt(100, 68).R);
    }
}

/// <summary>The presenter and FIGT_SEL over the player's data (CGs, scenario, captures).</summary>
public class BattlePresenterGameDataTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Captures => LocalData.Captures.Select(f => new object[] { Path.GetFileName(f) });

    private static BattlePresenter NewPresenter(BoxGlyphs? glyphs = null) =>
        new(new ScenarioText(LocalData.ScenarioDirectory), new CgLibrary(LocalData.CgDirectory), glyphs ?? new BoxGlyphs());

    /// <summary>Every recorded battle draws without errors, and every text it draws is found.</summary>
    [CaptureTheory]
    [MemberData(nameof(Captures))]
    public async Task Every_capture_draws(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var glyphs = new BoxGlyphs();
        var presenter = NewPresenter(glyphs);
        int events = 0, waits = 0;
        var names = new List<string>();
        var view = new DelegateView(e =>
        {
            presenter.Show(e);
            events++;
            names.Add(e.ToString()!);
            if (presenter.WaitsForKey)
                waits++;
        });

        var report = await GoldenReplay.RunAsync(capture, view: view, started: presenter.Start);

        output.WriteLine($"{events} events, {waits} key waits, {glyphs.Rendered} glyphs");
        if (waits == 0)
            output.WriteLine(string.Join("\n", names));
        Assert.True(report.Passed, report.Describe(5));
        Assert.True(presenter.Unresolved.Count == 0, string.Join("\n", presenter.Unresolved.Distinct()));
        Assert.True(glyphs.Rendered > 0);
        Assert.True(waits > 0);
    }

    [CaptureFact]
    public async Task A_redraw_shows_the_panel_frames_and_the_highlight_copies_one_panel()
    {
        var capture = GoldenCapture.Load(LocalData.Captures.First());
        var presenter = NewPresenter();
        bool checkedRedraw = false;
        var view = new DelegateView(e =>
        {
            presenter.Show(e);
            if (checkedRedraw || e is not PanelHighlightEvent h)
                return;
            // The redraw before the highlight copied the back buffer up: the frame CG 836 of
            // panel 1 sits at (5, 1), the same pixels as in the back buffer.
            Assert.Equal(presenter.Surface.PixelAt(10, 404), presenter.Surface.PixelAt(10, 4));
            // The highlighted panel's first character ("[", a box glyph) is in colour 240 on screen.
            var (x, y, _, _) = BattlePresenter.Panels[h.Panel.Value - 1];
            var p = presenter.Surface.StoredPixelAt(x * Surface.Scale + 1, (y + 2) * Surface.Scale + 2);
            Assert.Equal((0xdc, 0xff, 0x91), ((int)p.R, (int)p.G, (int)p.B));
            checkedRedraw = true;
        });
        await GoldenReplay.RunAsync(capture, view: view, started: presenter.Start);
        Assert.True(checkedRedraw);
    }

    [CaptureFact]
    public async Task Closing_an_order_message_restores_colour_22_and_other_windows_leave_15()
    {
        var presenter = NewPresenter();
        await GoldenReplay.RunAsync(GoldenCapture.Load(LocalData.Captures.First()), view: new DelegateView(presenter.Show), started: presenter.Start);
        var name = presenter.State!.Unit_StrBase;

        // L_0129e: window 143, the order message, op 10 (ZC 1, 15), then FIGT.ADV:827 ZC 1, 22.
        presenter.Show(new WindowEvent(143, 0));
        presenter.Show(new MessageEvent(new ScriptRef("FIGT.ADV", 801), [name]));
        presenter.Show(new WindowEvent(10, 0));
        Assert.Equal(22, presenter.Writer.Color);

        presenter.Show(new WindowEvent(143, 0));
        presenter.Show(new MessageEvent(new ScriptRef("FIGT.ADV", 805)));
        presenter.Show(new WindowEvent(10, 0));
        Assert.Equal(15, presenter.Writer.Color);
    }

    [CaptureFact]
    public void FigtSel_targets_frames_and_popups()
    {
        var presenter = NewPresenter();
        var hovered = new List<Sys3Int> { 99, 4, 5 };
        var menu = new FigtSelMenu(1, 0, 1, 2, 2,
        [
            new FigtSelOption(FigtSelCommand.Direct, [4, 5]),
            new FigtSelOption(FigtSelCommand.Defend, hovered),
            new FigtSelOption(FigtSelCommand.Wait, hovered),
            new FigtSelOption(FigtSelCommand.AutoAttack, hovered),
            new FigtSelOption(FigtSelCommand.AutoDefend, hovered),
            new FigtSelOption(FigtSelCommand.EnemyInfo, [1, 2]),
            new FigtSelOption(FigtSelCommand.Retreat, hovered),
            new FigtSelOption(FigtSelCommand.UnitInfo, [1, 2]),
            new FigtSelOption(FigtSelCommand.OrderManual, [1, 2]),
        ], [FigtSelCommand.Direct]);
        var screen = new FigtSelScreen(presenter, menu);
        var surface = presenter.Surface;

        // Hover enemy slot 1 (panel 5 at 448, 8): a white 4-px frame inside the panel.
        screen.MouseMove(500, 50);
        Assert.Equal((255, 255, 255), Rgb(surface.PixelAt(449, 9)));
        // Moving away restores the panel from the back buffer.
        screen.MouseMove(300, 50);
        Assert.Equal(surface.PixelAt(449, 409), surface.PixelAt(449, 9));

        // Right press over enemy 2 → the command popup opens on release; "Defend" answers (4, 5).
        screen.MouseDown(MouseButton.Right, 500, 150);
        Assert.Null(screen.Result);
        screen.MouseUp(MouseButton.Right, 500, 150);
        // Items: Defend, Wait, Auto-Atk, Auto-Def, Info, Retreat (Direct is the current mode).
        int boxX = 500 - 16, boxY = 150 - 10;
        screen.MouseMove(boxX + 10, boxY + 5);
        screen.MouseDown(MouseButton.Left, boxX + 10, boxY + 5);
        screen.MouseUp(MouseButton.Left, boxX + 10, boxY + 5);
        Assert.Equal(new FigtSelChoice(FigtSelCommand.Defend, 5), screen.Result);

        // Left click on a valid target.
        var again = new FigtSelScreen(presenter, menu);
        again.MouseMove(500, 50);
        again.MouseDown(MouseButton.Left, 500, 50);
        Assert.Equal(new FigtSelChoice(FigtSelCommand.Direct, 4), again.Result);
    }

    /// <summary>
    /// Live battles fought through the screens with the mouse: the first valid enemy panel is
    /// clicked, or else "Defend" is chosen from the command popup; script menus take their first
    /// item. Checks that the FIGT_SEL screen answers every real menu the core asks.
    /// </summary>
    [CaptureTheory]
    [MemberData(nameof(Captures))]
    public async Task Live_battles_can_be_fought_with_the_mouse(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var presenter = NewPresenter();
        var input = new MouseInput(presenter);
        var view = new DelegateView(presenter.Show);

        var (state, jump) = await BattleSandbox.RunLiveAsync(capture, view, input, seed: 1, started: presenter.Start);

        output.WriteLine($"{input.Selections} selections ({input.Popups} by popup), result {state.Bt_Result}, jump {jump}");
        Assert.True(presenter.Unresolved.Count == 0, string.Join("\n", presenter.Unresolved.Distinct()));
    }

    private sealed class MouseInput(BattlePresenter presenter) : IBattleInput
    {
        public int Selections { get; private set; }
        public int Popups { get; private set; }

        public Task<Sys3Int> WaitForKeyAsync()
        {
            presenter.Flush();
            presenter.AfterKey();
            return Task.FromResult((Sys3Int)16);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => Task.FromResult(Sys3Int.Zero);

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
        {
            if (++Selections > 300)
                throw new InvalidOperationException("The battle does not end.");
            presenter.Flush();
            var screen = new FigtSelScreen(presenter, menu);
            foreach (var target in menu.Find(menu.InitialMode)?.Arguments ?? [])
            {
                var (x, y, w, h) = BattlePresenter.Panels[target.Value];
                Click(screen, MouseButton.Left, x + w / 2, y + h / 2);
                if (screen.Result is { } chosen)
                    return Task.FromResult(chosen);
            }
            Popups++;
            Click(screen, MouseButton.Right, 320, 380);
            var popup = screen.Popup ?? throw new InvalidOperationException("No popup.");
            int item = popup.Items.ToList().FindIndex(i => i.Trim() == "Defend");
            Assert.True(item >= 0, string.Join("|", popup.Items));
            Click(screen, MouseButton.Left, popup.X + 10, popup.Y + item * 18 + 5);
            return Task.FromResult(screen.Result ?? throw new InvalidOperationException("Defend did not answer."));
        }

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
        {
            presenter.Flush();
            var screen = new BattleMenuScreen(presenter, menu);
            screen.MouseMove(260, 55);
            screen.MouseDown(MouseButton.Left, 260, 55);
            screen.MouseUp(MouseButton.Left, 260, 55);
            Assert.True(screen.IsDone);
            return Task.FromResult(screen.Result);
        }

        public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult(Sys3Int.Zero);

        private static void Click(FigtSelScreen screen, MouseButton button, int x, int y)
        {
            screen.MouseMove(x, y);
            screen.MouseDown(button, x, y);
            screen.MouseUp(button, x, y);
        }
    }

    private static (int, int, int) Rgb((byte R, byte G, byte B, byte A) p) => (p.R, p.G, p.B);

    private sealed class DelegateView(Action<ScriptEvent> show) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            show(scriptEvent);
            return Task.CompletedTask;
        }
    }
}

/// <summary>A fact over the golden captures; skipped without captures or scenario.</summary>
public sealed class CaptureFactAttribute : FactAttribute
{
    public CaptureFactAttribute()
    {
        if (!Directory.Exists(LocalData.ScenarioDirectory))
            Skip = $"No decompiled scenario in {LocalData.ScenarioDirectory}.";
        else if (!LocalData.Captures.Any())
            Skip = $"No golden captures in {LocalData.GoldenDirectory}.";
    }
}
