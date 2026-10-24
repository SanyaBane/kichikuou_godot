using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Debug → Menu Row Height (owner, 2026-10-25): a small window with two sliders, 18 to 60 — the province menu's and the
/// command panel's rows (<see cref="MenuRowsChoice.Height"/>) and the scenes' <c>]</c> menus' (<see cref="MenuRowsChoice.DialogueHeight"/>);
/// the game goes on under it, and an open menu follows a slider at once.
/// </summary>
internal sealed partial class MenuRowsWindow : Window
{
    public MenuRowsWindow()
    {
        Title = "Menu Row Height (debug)";
        Size = new Vector2I(520, 150);
        Transient = true;
        Exclusive = false;
        CloseRequested += QueueFree;
    }

    public override void _Ready()
    {
        var background = new Panel();
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(background);
        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.KeepSize, 12);
        root.AddThemeConstantOverride("separation", 10);
        AddChild(root);

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 8);
        root.AddChild(grid);
        var menus = Slider(grid, "Menus", MenuRowsChoice.Height, v => MenuRowsChoice.Height = v,
            v => $"{v} px · text {new MenuRows(v).Text}");
        var dialogues = Slider(grid, "Dialogues", MenuRowsChoice.DialogueHeight, v => MenuRowsChoice.DialogueHeight = v,
            v => $"{v} px · text {MathF.Round(30 * v / 36f)}");

        var reset = new Button
        {
            Text = $"Reset to {MenuRowsChoice.Default} and {MenuRowsChoice.DialogueDefault}",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
        };
        reset.Pressed += () =>
        {
            menus.Value = MenuRowsChoice.Default;
            dialogues.Value = MenuRowsChoice.DialogueDefault;
        };
        root.AddChild(reset);
    }

    /// <summary>A row of the grid: its name, the slider, its value shown by <paramref name="describe"/>.</summary>
    private static HSlider Slider(GridContainer grid, string name, int value, Action<int> set, Func<int, string> describe)
    {
        grid.AddChild(new Label { Text = name });
        var slider = new HSlider
        {
            MinValue = MenuRowsChoice.Min, MaxValue = MenuRowsChoice.Max, Step = 1, Value = value,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        grid.AddChild(slider);
        var shown = new Label { Text = describe(value), CustomMinimumSize = new Vector2(130, 0) };
        grid.AddChild(shown);
        slider.ValueChanged += v =>
        {
            set((int)v);
            shown.Text = describe((int)v);
        };
        return slider;
    }
}
