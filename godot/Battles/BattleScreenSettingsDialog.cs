using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

/// <summary>
/// Option → Battle Screen… (owner, 2026-10-12): a slider per setting of our battle screen
/// (<see cref="BattleScreenSettings"/>), applied and saved as it moves, and a preview of the order strip
/// (Preview makes a unit jump ahead and back; pressed again while it moves, the move hurries). The game
/// goes on behind it, so a setting can be tried in a battle at once.
/// </summary>
internal sealed partial class BattleScreenSettingsDialog : AcceptDialog
{
    private readonly OrderStrip _preview = new();
    private readonly List<(BattleScreenSettings.Setting Setting, HSlider Slider)> _sliders = [];
    private bool _jumped;

    // The week-26 battle's first round: Barres, hit hard, jumps ahead of Leila.
    private static readonly OrderChip[] Before =
    [
        new(BattleSide.Enemy, 4, 2, "Melfeis", ChipState.Acting), new(BattleSide.Player, 5, 4, "Leila", ChipState.Waiting),
        new(BattleSide.Player, 10, 3, "Menad", ChipState.Waiting), new(BattleSide.Player, 1, 1, "Rance", ChipState.Waiting),
        new(BattleSide.Player, 6, 2, "Barres", ChipState.Waiting), new(BattleSide.Enemy, 5, 1, "Dodge", ChipState.Waiting),
    ];
    private static readonly OrderChip[] After = [Before[0], Before[4], Before[1], Before[2], Before[3], Before[5]];

    public BattleScreenSettingsDialog()
    {
        Title = "Battle Screen";
        OkButtonText = "Close";
        Exclusive = false;                                      // the game goes on behind it
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 12);
        foreach (var setting in BattleScreenSettings.All)
        {
            double value = BattleScreenSettings.Get(setting);
            grid.AddChild(new Label { Text = setting.Label, TooltipText = setting.Hint, MouseFilter = Control.MouseFilterEnum.Pass });
            var slider = new HSlider
            {
                MinValue = setting.Min, MaxValue = setting.Max, Step = setting.Step, Value = value,
                CustomMinimumSize = new Vector2(280, 0), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                TooltipText = setting.Hint,
            };
            var shown = new Label { Text = Format(setting, value), CustomMinimumSize = new Vector2(64, 0) };
            slider.ValueChanged += v =>
            {
                BattleScreenSettings.Set(setting, v);
                shown.Text = Format(setting, v);
            };
            grid.AddChild(slider);
            grid.AddChild(shown);
            _sliders.Add((setting, slider));
        }
        column.AddChild(grid);

        var colours = DeploymentPalette.Chosen;
        var back = new PanelContainer();
        back.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = colours.Back, ContentMarginTop = 6, ContentMarginBottom = 6 + 40,  // room for the jump's row
        });
        var area = new Control { CustomMinimumSize = new Vector2(640, 34) };
        _preview.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        area.AddChild(_preview);
        back.AddChild(area);
        column.AddChild(back);
        AddChild(column);
        _preview.Show(Before, instant: true);

        AddButton("Preview", right: false, action: "preview");
        AddButton("Defaults", right: false, action: "defaults");
        CustomAction += action =>
        {
            if (action == "preview")
            {
                _jumped = !_jumped;
                _preview.Show(_jumped ? After : Before, instant: false);
            }
            else if (action == "defaults")
            {
                foreach (var (setting, slider) in _sliders)
                    slider.Value = setting.Default;
            }
        };
    }

    private static string Format(BattleScreenSettings.Setting setting, double value) =>
        setting.Unit == "%" ? $"{value * 100:F0} %" : $"{value:F2} {setting.Unit}";
}
