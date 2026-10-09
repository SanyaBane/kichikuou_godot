using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Game.Strategy;

namespace Kichikuou.Game;

/// <summary>
/// The title (owner's decision 10, minimal for now): New game, Load, Quit, and the battle-capture
/// viewer for debugging. Every game runs on its own <see cref="GameScreen"/>, which comes back here.
/// </summary>
public partial class Main : Control
{
    /// <summary>The development switches are read once per launch, not again when a game returns here.</summary>
    private static bool _commandLineRead;

    public override void _Ready()
    {
        var background = new ColorRect { Color = Colors.Black };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var root = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 16);
        AddChild(root);

        root.AddChild(new Label
        {
            Text = "鬼畜王ランス — Kichikuou Rance",
            ThemeTypeVariation = "HeaderLarge",
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        root.AddChild(new Control { CustomMinimumSize = new Vector2(0, 32) });
        AddButton(root, "New game", () => StartGame(GameStart.New));
        AddButton(root, "Load", () => StartGame(GameStart.Load));
        AddButton(root, "Quit", () => GetTree().Quit());
        root.AddChild(new Control { CustomMinimumSize = new Vector2(0, 32) });
        AddButton(root, "Battle captures (debug)", () => Screens.Open(this, new CaptureList()));

        if (!_commandLineRead)
        {
            _commandLineRead = true;
            StartFromCommandLine();
        }
    }

    private static void AddButton(VBoxContainer root, string text, Action pressed)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(320, 48), SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        button.Pressed += pressed;
        root.AddChild(button);
    }

    private void StartGame(GameStart start) => Screens.Open(this, new GameScreen(new GameOptions { Start = start }));

    /// <summary>
    /// Development switches after <c>--</c>. A game: <c>--newgame</c> or <c>--load</c> starts it at
    /// once; <c>--auto</c> plays it unattended (keys answered, Turn END every week, the battles
    /// fought with the first attack); <c>--seed N</c> fixes the random generator (once per launch: a
    /// later game goes on with it, as after xsystem35's Restart); <c>--msgskip</c> starts with MsgSkip
    /// on; <c>--saves DIR</c>
    /// keeps its slots there (<c>--auto</c> defaults to <c>user://saves-auto</c>). A battle capture:
    /// <c>--play FILE</c> / <c>--watch FILE</c>. Both: <c>--fast</c> drops the delays, <c>--quit</c>
    /// exits at the end (exit code 1 after an error), <c>--shots DIR</c> saves the screen at every
    /// key wait and menu (a game's event CGs masked, <c>GamePresenter.MaskEventCgs</c>).
    /// </summary>
    private void StartFromCommandLine()
    {
        var args = OS.GetCmdlineUserArgs();
        string? Value(string name) => Array.IndexOf(args, name) is int i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        bool fast = args.Contains("--fast"), quit = args.Contains("--quit");
        string? shots = Value("--shots");

        Node? screen = null;
        if (args.Contains("--newgame") || args.Contains("--load"))
        {
            bool auto = args.Contains("--auto");
            screen = new GameScreen(new GameOptions
            {
                Start = args.Contains("--load") ? GameStart.Load : GameStart.New,
                Auto = auto,
                Fast = fast,
                QuitWhenDone = quit,
                ScreenshotDirectory = shots,
                MsgSkip = args.Contains("--msgskip"),
                SaveDirectory = Value("--saves") ?? (auto ? "user://saves-auto" : null),
            });
        }
        else if ((Value("--play") ?? Value("--watch")) is { } capture)
        {
            screen = new BattleScreen(capture, Value("--play") is not null ? BattleMode.Play : BattleMode.Watch)
            {
                Fast = fast,
                QuitWhenDone = quit,
                ScreenshotDirectory = shots,
            };
        }
        if (screen is not null)
            Callable.From(() => Screens.Open(this, screen)).CallDeferred();   // the tree is busy during _Ready
    }
}
