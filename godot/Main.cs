using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Game.Strategy;

namespace Kichikuou.Game;

/// <summary>
/// The start of the program and the way back to the title: it opens the title — a
/// <see cref="GameScreen"/> running <c>ｽﾀMENU</c> from the player's files (owner's decision 19, in
/// place of decision 10's own title), a fresh boot each time, as a new launch of the original —
/// or, on the first start, what the development switches ask for. The battle-capture viewer is in
/// the game screen's Debug menu.
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

        Node? screen = null;
        if (!_commandLineRead)
        {
            _commandLineRead = true;
            screen = FromCommandLine();
        }
        screen ??= new GameScreen(new GameOptions { Start = GameStart.Title });
        Callable.From(() => Screens.Open(this, screen)).CallDeferred();     // the tree is busy during _Ready
    }

    /// <summary>
    /// Development switches after <c>--</c>. A game without the title: <c>--newgame</c> or <c>--load</c> starts it at
    /// once (<c>--ending N</c> shows ending N's <c>OVER</c> on the booted game, its slots in
    /// <c>user://saves-ending</c>, so the clear it records stays out of the player's); <c>--auto</c> plays it unattended (keys answered, Turn END every week, the battles
    /// fought with the first attack, from stage 1 a replenish and an attack on the rebels once a
    /// week, a strong party sent) up to week 30 or <c>--weeks N</c>, or to the command menu two weeks after story stage
    /// <c>--stage N</c> was reached, or earlier with <c>--until deployment|battle</c> (after the first
    /// deployment picker or the first battle: a look at one screen); <c>--seed N</c> fixes the random generator (once per launch: a
    /// later game goes on with it, as after xsystem35's Restart); <c>--rnglog</c> prints every
    /// draw to the console; <c>--msgskip</c> starts with MsgSkip
    /// on; <c>--interface new</c> uses our own screens for the launch (Option → Interface); <c>--saves DIR</c>
    /// keeps its slots there (<c>--auto</c> defaults to <c>user://saves-auto</c>); <c>--slots-from DIR</c>
    /// copies DIR's slots there first, so a kept save is loaded unchanged every time; <c>--turn-end</c>
    /// makes the unattended player end every week at once, <c>--party 1,10,11</c> send those units
    /// (to make a kept save). A battle capture:
    /// <c>--play FILE</c> / <c>--watch FILE</c>. Both: <c>--fast</c> drops the delays, <c>--quit</c>
    /// exits at the end (exit code 1 after an error), <c>--shots DIR</c> saves the screen at every
    /// key wait and menu (a game's event CGs masked, <c>GamePresenter.MaskEventCgs</c>).
    /// <c>--portraits</c> opens Debug → Portraits over the title (with <c>--shots DIR</c> it saves the
    /// units' list there; <c>--quit</c> then ends). Null without a switch: the title.
    /// </summary>
    private static Node? FromCommandLine()
    {
        var args = OS.GetCmdlineUserArgs();
        string? Value(string name) => Array.IndexOf(args, name) is int i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        bool fast = args.Contains("--fast"), quit = args.Contains("--quit");
        string? shots = Value("--shots");

        int? ending = int.TryParse(Value("--ending"), out int e) ? e : null;
        if (args.Contains("--newgame") || args.Contains("--load") || ending is not null)
        {
            bool auto = args.Contains("--auto");
            return new GameScreen(new GameOptions
            {
                Start = ending is not null ? GameStart.Ending : args.Contains("--load") ? GameStart.Load : GameStart.New,
                Ending = ending ?? 0,
                Auto = auto,
                Fast = fast,
                QuitWhenDone = quit,
                ScreenshotDirectory = shots,
                MsgSkip = args.Contains("--msgskip"),
                SaveDirectory = Value("--saves") ?? (ending is not null ? "user://saves-ending" : auto ? "user://saves-auto" : null),
                AutoWeeks = int.TryParse(Value("--weeks"), out int weeks) ? weeks : 30,
                AutoStage = int.TryParse(Value("--stage"), out int stage) ? stage : 0,
                AutoUntil = Value("--until") switch
                {
                    "deployment" => AutoUntil.Deployment,
                    "battle" => AutoUntil.Battle,
                    _ => AutoUntil.None,
                },
                SlotsFrom = Value("--slots-from"),
                AutoTurnEnd = args.Contains("--turn-end"),
                AutoParty = Value("--party") is { } party ? [.. party.Split(',').Select(int.Parse)] : null,
                AutoScout = args.Contains("--scout"),
            });
        }
        if (args.Contains("--portraits"))
            return new GameScreen(new GameOptions { Start = GameStart.Title, Portraits = true, ScreenshotDirectory = shots, QuitWhenDone = quit });
        if ((Value("--play") ?? Value("--watch")) is { } capture)
        {
            return new BattleScreen(capture, Value("--play") is not null ? BattleMode.Play : BattleMode.Watch)
            {
                Fast = fast,
                QuitWhenDone = quit,
                ScreenshotDirectory = shots,
            };
        }
        return null;
    }
}
