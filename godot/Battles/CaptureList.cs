using Godot;
using Kichikuou.Content.Battles;
using Kichikuou.Core.Golden;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The battle-capture viewer, a debug entry of the title: picks a golden capture and starts its
/// battle live ("Play") or plays the recorded battle back ("Watch").
/// </summary>
public partial class CaptureList : Control
{
    private readonly List<(string Path, CaptureSummary Summary)> _captures = [];
    private LineEdit _dataPath = null!;
    private ItemList _list = null!;
    private Label _status = null!;
    private Button _play = null!;
    private Button _watch = null!;

    public override void _Ready()
    {
        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize, 24);
        root.AddThemeConstantOverride("separation", 12);
        AddChild(root);

        var header = new HBoxContainer();
        var back = new Button { Text = "← Title" };
        back.Pressed += () => Screens.ToTitle(this);
        header.AddChild(back);
        header.AddChild(new Label { Text = "Battle captures (debug)", ThemeTypeVariation = "HeaderLarge" });
        root.AddChild(header);

        var pathRow = new HBoxContainer();
        pathRow.AddChild(new Label { Text = "Extracted game data:" });
        _dataPath = new LineEdit { Text = GameData.ExtractDirectory, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        pathRow.AddChild(_dataPath);
        var apply = new Button { Text = "Reload" };
        apply.Pressed += () =>
        {
            GameData.ExtractDirectory = _dataPath.Text;
            _ = LoadCapturesAsync();
        };
        pathRow.AddChild(apply);
        root.AddChild(pathRow);

        _list = new ItemList { SizeFlagsVertical = SizeFlags.ExpandFill };
        _list.ItemSelected += _ => UpdateButtons();
        _list.ItemActivated += index => Start((int)index, BattleMode.Play);
        root.AddChild(_list);

        var buttons = new HBoxContainer();
        _play = new Button { Text = "Play", CustomMinimumSize = new Vector2(160, 40) };
        _play.Pressed += () => StartSelected(BattleMode.Play);
        _watch = new Button { Text = "Watch the recording", CustomMinimumSize = new Vector2(220, 40) };
        _watch.Pressed += () => StartSelected(BattleMode.Watch);
        buttons.AddChild(_play);
        buttons.AddChild(_watch);
        _status = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.AddChild(_status);
        root.AddChild(buttons);

        UpdateButtons();
        _ = LoadCapturesAsync();
    }

    private async Task LoadCapturesAsync()
    {
        _captures.Clear();
        _list.Clear();
        UpdateButtons();
        string dir = GameData.GoldenDirectory;
        if (!System.IO.Directory.Exists(dir))
        {
            _status.Text = $"No captures folder: {dir}";
            return;
        }
        _status.Text = "Reading captures…";
        var loaded = await Task.Run(() => System.IO.Directory.EnumerateFiles(dir, "*.golden")
            .Order(StringComparer.Ordinal)
            .Select(path => (path, BattleSandbox.Summarize(GoldenCapture.Load(path))))
            .ToList());
        foreach (var (path, summary) in loaded)
        {
            _captures.Add((path, summary));
            _list.AddItem(Describe(summary));
        }
        _status.Text = $"{_captures.Count} captures";
        UpdateButtons();
    }

    private static string Describe(CaptureSummary s)
    {
        string kind = s.Root switch
        {
            "FIGT_A.ADV" => "Attack",
            "FIGT_B.ADV" => "Defence",
            "FIGT.ADV" => "Dungeon",
            _ => s.Root,
        };
        string outcome = s.JumpTarget is not null ? "game over" : s.Outcome == 2 ? "won" : "lost";
        string hooks = s.Hooks.Count > 0 ? $"  [scenes: {string.Join(", ", s.Hooks)}]" : "";
        return $"Week {s.Week,2}  {kind,-7}  {s.Province,-14}  {string.Join(", ", s.PlayerUnits)}  vs  "
            + $"{string.Join(", ", s.EnemyCommanders)}  — {outcome}{hooks}";
    }

    private void UpdateButtons()
    {
        bool selected = _list.GetSelectedItems().Length > 0;
        _play.Disabled = !selected;
        _watch.Disabled = !selected;
    }

    private void StartSelected(BattleMode mode)
    {
        var selected = _list.GetSelectedItems();
        if (selected.Length > 0)
            Start(selected[0], mode);
    }

    private void Start(int index, BattleMode mode)
    {
        Screens.Open(this, new BattleScreen(_captures[index].Path, mode));
    }
}
