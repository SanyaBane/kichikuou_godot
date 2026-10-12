using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The windows over our battle screen's scene (docs/ui/battle-screen.md, "The scene between exchanges").</summary>
public class BattleWindowsTests(ITestOutputHelper output)
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    /// <summary>The window ops that open a window, with their frame CGs (docs/battle/screen-layout.md §2.2).</summary>
    private static readonly Dictionary<int, int> Frames = new()
    {
        [111] = 807, [112] = 808, [113] = 809, [123] = 809, [121] = 810, [122] = 811,
        [141] = 895, [142] = 896, [143] = 894, [153] = 894, [151] = 897, [152] = 898,
    };

    [Fact]
    public void A_mask_is_the_union_of_its_shapes_and_the_screen_shows_only_there()
    {
        var palette = new byte[768];
        palette[3 * 7] = 200;
        // A 3×2 sprite: index 40 (the key) at its top-left corner.
        var cg = new CgImage(3, 2, [40, 7, 7, 7, 7, 7], palette);
        var mask = WindowMask.Of([WindowShape.Keyed(cg, 10, 20), WindowShape.Box(11, 20, 2, 1), WindowShape.Box(638, 398, 10, 10)]);

        Assert.False(mask.Contains(10, 20));
        Assert.True(mask.Contains(11, 20));
        Assert.True(mask.Contains(10, 21));
        Assert.False(mask.Contains(13, 20));
        Assert.True(mask.Contains(639, 399));
        Assert.Equal(5 + 4, mask.Count);                                // the boxes clipped to the screen

        var screen = new Surface(Surface.ScreenWidth, Surface.ScreenHeight, indexed: false);
        screen.FillColor(0, 0, Surface.ScreenWidth, Surface.ScreenHeight, (1, 2, 3));
        var rgba = mask.Compose(screen);
        int row = Surface.ScreenWidth * Surface.Scale;
        byte Alpha(int x, int y) => rgba[((y * Surface.Scale + 1) * row + x * Surface.Scale + 1) * 4 + 3];
        Assert.Equal(255, Alpha(11, 21));
        Assert.Equal(0, Alpha(10, 20));
        Assert.Equal(0, Alpha(300, 200));
        Assert.Equal(1, rgba[((21 * Surface.Scale) * row + 11 * Surface.Scale) * 4]);
    }

    /// <summary>
    /// A mask reads only the rows its windows span (<see cref="Surface.ReadArea"/>, the same pixels as
    /// <see cref="Surface.ReadScreen"/>); the rest is transparent, also in a target an earlier compose filled,
    /// and an empty mask leaves the whole screen transparent.
    /// </summary>
    [Fact]
    public void A_mask_reads_only_the_rows_of_its_windows()
    {
        var screen = new Surface(Surface.ScreenWidth, Surface.ScreenHeight, indexed: false);
        screen.FillColor(0, 0, Surface.ScreenWidth, Surface.ScreenHeight, (1, 2, 3));
        screen.FillColor(100, 50, 20, 10, (9, 8, 7));
        var whole = screen.ReadScreen();
        int rowBytes = Surface.ScreenWidth * Surface.Scale * 4;
        var area = screen.ReadArea(0, 50, Surface.ScreenWidth, 10);
        Assert.Equal(whole.AsSpan(50 * Surface.Scale * rowBytes, area.Length).ToArray(), area);

        var target = WindowMask.Of([WindowShape.Box(0, 0, Surface.ScreenWidth, Surface.ScreenHeight)]).Compose(screen);
        WindowMask.Of([WindowShape.Box(100, 50, 20, 10)]).Compose(screen, target);
        byte Alpha(int x, int y) => target[(y * Surface.Scale * rowBytes) + x * Surface.Scale * 4 + 3];
        Assert.Equal(255, Alpha(105, 55));
        Assert.Equal(9, target[55 * Surface.Scale * rowBytes + 105 * Surface.Scale * 4]);
        Assert.Equal(0, Alpha(105, 49));
        Assert.Equal(0, Alpha(105, 60));
        Assert.Equal(0, Alpha(99, 55));

        var empty = WindowMask.Of([]);
        Assert.True(empty.IsEmpty);
        empty.Compose(screen, target);
        Assert.True(Enumerable.Range(0, target.Length / 4).All(i => target[i * 4 + 3] == 0), "an empty mask shows nothing");
    }

    [Fact]
    public void The_input_screens_add_the_prompt_and_their_boxes_until_they_answer()
    {
        var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
        var hovered = new List<Sys3Int> { 99, 4 };
        var menu = new FigtSelMenu(1, 0, 1, 1, 1,
        [
            new FigtSelOption(FigtSelCommand.Direct, [4]),
            new FigtSelOption(FigtSelCommand.Defend, hovered),
            new FigtSelOption(FigtSelCommand.Wait, hovered),
            new FigtSelOption(FigtSelCommand.AutoAttack, hovered),
            new FigtSelOption(FigtSelCommand.AutoDefend, hovered),
            new FigtSelOption(FigtSelCommand.EnemyInfo, [1]),
            new FigtSelOption(FigtSelCommand.Retreat, hovered),
            new FigtSelOption(FigtSelCommand.UnitInfo, [1]),
        ], [FigtSelCommand.Direct]);
        var sel = new FigtSelScreen(presenter, menu);
        var prompt = Cgs.Value.Get(861)!;

        var mask = WindowMask.Of(sel.Windows);
        Assert.True(mask.Contains(prompt.X + prompt.Width / 2, prompt.Y + prompt.Height / 2));
        Assert.False(mask.Contains(300, 380));

        sel.MouseDown(MouseButton.Right, 300, 380);
        sel.MouseUp(MouseButton.Right, 300, 380);
        var box = sel.Popup ?? throw new InvalidOperationException("No popup.");
        mask = WindowMask.Of(sel.Windows);
        var (x, y, w, h) = box.Box;
        Assert.True(mask.Contains(x - 8, y - 8));
        Assert.True(mask.Contains(x + w + 7, y + h + 7));
        Assert.False(mask.Contains(x + w + 8, y));

        sel.MouseMove(x + 10, y + 5);
        sel.MouseDown(MouseButton.Left, x + 10, y + 5);
        sel.MouseUp(MouseButton.Left, x + 10, y + 5);
        Assert.Equal(FigtSelCommand.Defend, sel.Result?.Command);
        Assert.Empty(sel.Windows);

        var script = new BattleMenuScreen(presenter, new BattleMenu(BattleMenuKind.RetreatConfirm,
            [new BattleMenuOption(1, new ScriptRef("FIGT.ADV", 573)), new BattleMenuOption(2, new ScriptRef("FIGT.ADV", 575))]));
        Assert.True(WindowMask.Of(script.Windows).Contains(250 - 8, 50 - 8));
        script.MouseDown(MouseButton.Right, 0, 0);
        Assert.Empty(script.Windows);
    }

    [Fact]
    public void A_scene_the_battle_calls_turns_the_split_off_until_the_field_is_drawn_again()
    {
        var state = new GameState();
        var scene = new BattleScene(Cgs.Value);
        scene.Start(state);
        scene.Observe(new BattlefieldBackgroundEvent(0), state);
        Assert.True(scene.Active);
        scene.Observe(new WindowEvent(143, 0), state);
        Assert.True(scene.Active);
        scene.Observe(new EngineEvent("G", [(Sys3Int)101]), state);
        Assert.False(scene.Active);
        scene.Observe(new WindowEvent(141, 0), state);
        Assert.False(scene.Active);
        scene.Observe(new BattlefieldBackgroundEvent(0), state);
        Assert.True(scene.Active);
        scene.Observe(new WindowEvent(131, 0), state);
        Assert.False(scene.Active);
    }

    /// <summary>
    /// In every captured battle the mask covers each window while it is open — the round header and the
    /// window frames and the portraits are on the screen at every pixel of their shapes when they open —
    /// and is empty whenever the field has been drawn again.
    /// </summary>
    [CaptureTheory]
    [MemberData(nameof(BattleSceneTests.Captures), MemberType = typeof(BattleSceneTests))]
    public async Task The_mask_covers_each_window_while_it_is_open(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var cgs = Cgs.Value;
        var presenter = new BattlePresenter(Texts.Value, cgs, new BoxGlyphs());
        int opened = 0, redraws = 0;
        var wrong = new List<string>();
        bool redrawn = false;
        var view = new ObservingView(e =>
        {
            if (redrawn && e is not SlotPanelEvent and not PanelHighlightEvent)
            {
                // The field's redraw is finished (its copy up) before the next event draws anything.
                redrawn = false;
                presenter.Flush();
                if (presenter.Windows.Count > 0)
                    wrong.Add($"redraw {redraws}: {presenter.Windows.Count} window(s) open");
            }
            presenter.Show(e);
            switch (e)
            {
                case RoundHeaderEvent:
                    // The header's text is printed with it: its frame is checked outside the text rectangle.
                    Check(887, "round header", (138, 75, 360, 260), null);
                    break;
                case WindowEvent w when Frames.TryGetValue(w.Op.Value, out int frame):
                    Check(frame, $"window {w.Op.Value}", null, null);
                    break;
                case WindowEvent { Op.Value: 311 or 321 } w:
                    Check(w.CallArg.Value, $"portrait {w.CallArg.Value}", null, w.Op == 311 ? (0, 0) : (448, 144));
                    break;
                case WindowEvent { Op.Value: 10 } when presenter.Windows.Count > 0:
                    wrong.Add($"op 10 left {presenter.Windows.Count} window(s)");
                    break;
                case BattlefieldBackgroundEvent:
                    redraws++;
                    redrawn = true;
                    break;
            }
        });
        await GoldenReplay.RunAsync(capture, view: view, started: presenter.Start);
        output.WriteLine($"{file}: {opened} windows opened, {redraws} redraws");
        Assert.True(opened > 0);
        Assert.Empty(wrong.Distinct());

        void Check(int number, string what, (int X, int Y, int W, int H)? text, (int X, int Y)? at)
        {
            opened++;
            var cg = cgs.Get(number)!;
            var mask = WindowMask.Of(presenter.Windows);
            var surface = presenter.Surface;
            int missing = 0, differ = 0;
            for (int y = 0; y < cg.Height; y++)
            {
                for (int x = 0; x < cg.Width; x++)
                {
                    int index = cg.IndexAt(x, y);
                    if (index == Surface.SpriteKey)
                        continue;
                    int sx = (at?.X ?? cg.X) + x, sy = (at?.Y ?? cg.Y) + y;
                    if (!mask.Contains(sx, sy))
                        missing++;
                    if (text is var (tx, ty, tw, th) && sx >= tx && sy >= ty && sx < tx + tw && sy < ty + th)
                        continue;
                    var (r, g, b, _) = surface.PixelAt(sx, sy);
                    if ((r, g, b) != surface.Color(index))
                        differ++;
                }
            }
            if (missing > 0 || differ > 0)
                wrong.Add($"{what}: {missing} frame pixels outside the mask, {differ} not on the screen");
        }
    }

    private sealed class ObservingView(Action<ScriptEvent> show) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            show(scriptEvent);
            return Task.CompletedTask;
        }
    }
}
