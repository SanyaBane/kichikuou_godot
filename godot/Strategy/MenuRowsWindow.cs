using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Debug → Menu Row Height (owner, 2026-10-25): a small window with a slider for <see cref="MenuRowsChoice.Height"/>, 18 to
/// 60; the game goes on under it, and an open province menu or command panel follows the slider at once.
/// </summary>
internal sealed partial class MenuRowsWindow : Window
{
    private Label _value = null!;
    private HSlider _slider = null!;

    public MenuRowsWindow()
    {
        Title = "Menu Row Height (debug)";
        Size = new Vector2I(420, 120);
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

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        _slider = new HSlider
        {
            MinValue = MenuRowsChoice.Min, MaxValue = MenuRowsChoice.Max, Step = 1, Value = MenuRowsChoice.Height,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        _slider.ValueChanged += value =>
        {
            MenuRowsChoice.Height = (int)value;
            ShowValue(MenuRowsChoice.Height);
        };
        row.AddChild(_slider);
        _value = new Label { CustomMinimumSize = new Vector2(130, 0) };
        row.AddChild(_value);
        root.AddChild(row);

        var reset = new Button { Text = $"Reset to {MenuRowsChoice.Default}", SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin };
        reset.Pressed += () => _slider.Value = MenuRowsChoice.Default;
        root.AddChild(reset);
        ShowValue(MenuRowsChoice.Height);
    }

    private void ShowValue(int height) => _value.Text = $"{height} px · text {new MenuRows(height).Text}";
}
