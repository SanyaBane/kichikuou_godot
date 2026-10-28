using Godot;
using Kichikuou.Content;
using Kichikuou.Game.Battles;
using Kichikuou.Game.Battles.Effects;
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
        Device.Follow(GetTree().Root);
        if (!_commandLineRead && !Device.OpenPhoneWindow())
        {
            GetTree().Quit(1);
            return;
        }
        ThousandsChoice.Apply();
        ScreenModeChoice.Apply(this);
        SoundChoice.Apply();
        var background = new ColorRect { Color = Colors.Black };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        Node? screen = null;
        if (!_commandLineRead)
        {
            _commandLineRead = true;
            EffectTextures.Prepare();               // the exchange effects' masks, ahead of a battle (docs/backlog.md, "Android")
            if (TouchMode.NoHover)
                NoHoverFilter.Install(GetTree().Root);
            ImportFromCommandLine();
            if (ImportScreenFromCommandLine() is { } look)
            {
                screen = look;
            }
            else if (!GameData.UsesExtract && GameData.Imported is null && !GameData.OpenImport(out string? problem))
            {
                // No data yet: the import screen first, then what was asked for (docs/import.md, step 8).
                GD.Print($"Import screen: {problem}");
                string folder = GameData.SavedGameDirectory;
                screen = new ImportScreen(folder, start: folder.Length > 0, next: () => FromCommandLine() ?? Title());
            }
            else
            {
                screen = FromCommandLine();
            }
        }
        screen ??= Title();
        Callable.From(() => Screens.Open(this, screen)).CallDeferred();     // the tree is busy during _Ready
    }

    /// <summary>
    /// <c>--import [DIR]</c>: the launch reads the import of the game folder DIR (else the saved one) even where an
    /// extract is set, importing it first when its cache is not whole (docs/import.md, step 8; the cache in
    /// <c>user://import</c>). A folder that is no known version or a scenario that does not decompile is an error
    /// (with <c>--quit</c>, exit code 1).
    /// </summary>
    private void ImportFromCommandLine()
    {
        var args = OS.GetCmdlineUserArgs();
        int at = Array.IndexOf(args, "--import");
        if (at < 0)
            return;
        GameData.ForceImport = true;
        string folder = at + 1 < args.Length && !args[at + 1].StartsWith("--", StringComparison.Ordinal) ? args[at + 1] : GameData.GameDirectory;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        string? stage = null;
        try
        {
            GameData.Imported = GameImport.Import(folder, GameData.ImportCache, new Progress(p =>
            {
                if (p.Stage != stage)
                    GD.Print($"Import: {stage = p.Stage} ({p.Fraction:P0})");
            }));
            GD.Print($"Import: {GameData.Imported.Folder.Version.Name} from {folder}, scenario in {GameData.Imported.ScenarioDirectory}, {watch.ElapsedMilliseconds} ms");
        }
        catch (Exception e) when (e is GameFolderException or Kichikuou.Content.Decompiler.DecompileException)
        {
            GD.PrintErr($"Import failed: {e.Message}");
            if (OS.GetCmdlineUserArgs().Contains("--quit"))
                GetTree().Quit(1);
        }
    }

    internal static Node Title() => new GameScreen(new GameOptions { Start = GameStart.Title });

    /// <summary>
    /// <c>--import-screen [DIR]</c>: the import screen whatever data there is (the launch reads the import). With DIR it
    /// imports DIR without the dialog into a fresh cache of its own (<c>user://import-look</c>), saving the screen at each
    /// phase into <c>--shots DIR</c> (a window is needed), and with <c>--quit</c> ends there; else the title follows.
    /// </summary>
    private static ImportScreen? ImportScreenFromCommandLine()
    {
        var args = OS.GetCmdlineUserArgs();
        int at = Array.IndexOf(args, "--import-screen");
        if (at < 0)
            return null;
        GameData.ForceImport = true;
        string? folder = at + 1 < args.Length && !args[at + 1].StartsWith("--", StringComparison.Ordinal) ? args[at + 1] : null;
        int shots = Array.IndexOf(args, "--shots");
        return new ImportScreen(GameData.SavedGameDirectory, start: false, next: Title, unattended: folder,
            shots: shots >= 0 && shots + 1 < args.Length ? args[shots + 1] : null, quit: args.Contains("--quit"));
    }

    /// <summary>A progress report on the reporting thread.</summary>
    private sealed class Progress(Action<ImportProgress> report) : IProgress<ImportProgress>
    {
        public void Report(ImportProgress value) => report(value);
    }

    /// <summary>
    /// Development switches after <c>--</c>. A game without the title: <c>--newgame</c> or <c>--load</c> starts it at
    /// once (<c>--ending N</c> shows ending N's <c>OVER</c> on the booted game, its slots in
    /// <c>user://saves-ending</c>, so the clear it records stays out of the player's); <c>--auto</c> plays it unattended (keys answered, Turn END every week, the battles
    /// fought with the first attack, from stage 1 a replenish and an attack on the rebels once a
    /// week, a strong party sent) up to week 30 or <c>--weeks N</c>, or to the command menu two weeks after story stage
    /// <c>--stage N</c> was reached, or earlier with <c>--until deployment|battle|battle-end</c> (after the first
    /// deployment picker or the first battle: a look at one screen; <c>battle</c> after our battle screen's exercise and
    /// its attack where it runs, <c>battle-end</c> at the battle's end always; <c>round-N</c> already when a battle's round N
    /// starts); <c>--seed N</c> fixes the random generator (once per launch: a
    /// later game goes on with it, as after xsystem35's Restart); <c>--rnglog</c> prints every
    /// draw to the console; <c>--msgskip</c> starts with MsgSkip
    /// on; <c>--interface original</c> uses the original's screens for the launch instead of ours; <c>--saves DIR</c>
    /// keeps its slots there (<c>--auto</c> defaults to <c>user://saves-auto</c>); <c>--slots-from DIR</c>
    /// copies DIR's slots there first, so a kept save is loaded unchanged every time; <c>--turn-end</c>
    /// makes the unattended player end every week at once, <c>--party 1,10,11</c> send those units
    /// (to make a kept save). A battle capture:
    /// <c>--play FILE</c> / <c>--watch FILE</c>. Both: <c>--fast</c> drops the delays, <c>--quit</c>
    /// exits at the end (exit code 1 after an error), <c>--shots DIR</c> saves the screen at every
    /// key wait and menu (a game's event CGs masked, <c>GamePresenter.MaskEventCgs</c>).
    /// <c>--portraits</c> opens Debug → Portraits over the title (with <c>--shots DIR</c> it saves the
    /// units' list there; <c>--quit</c> then ends). <c>--effects</c> opens Debug → Effects the same way (with
    /// <c>--shots DIR</c> it saves the effects' frames there; needs a window; <c>--effects white-beam-magic,black-beam-nagi</c> saves
    /// only the effects with those prefixes). <c>--options</c> opens the options window over the title (with <c>--shots DIR</c>
    /// in a window it saves each tab there; <c>--quit</c> then ends). <c>--screen-gallery [id,…]</c> opens the screen gallery
    /// (docs/ui/gallery.md; with <c>--shots DIR</c> in a window it dumps the entries there, compared with <c>--previous DIR</c>'s,
    /// the window at <c>--window WxH</c>, else 1280×800). With any of them <c>--phone landscape</c> plays a phone held
    /// landscape (docs/ui/phone-build.md, §3: the touch mode, the window at the reference phone's shape, <see cref="Device"/>).
    /// Null without a switch: the title.
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
                    "battle-end" => AutoUntil.BattleEnd,
                    { } until when until.StartsWith("round-") => AutoUntil.Round,
                    _ => AutoUntil.None,
                },
                AutoUntilRound = Value("--until") is { } round && round.StartsWith("round-") && int.TryParse(round[6..], out int r) ? r : 0,
                SlotsFrom = Value("--slots-from"),
                AutoTurnEnd = args.Contains("--turn-end"),
                AutoParty = Value("--party") is { } party ? [.. party.Split(',').Select(int.Parse)] : null,
                AutoScout = args.Contains("--scout"),
                AutoHarem = int.TryParse(Value("--harem"), out int harem) ? harem : 0,
                AutoTreasure = args.Contains("--treasure"),
                AutoSaveLoad = args.Contains("--saveload"),
                AutoRefill = args.Contains("--refill"),
                AutoDungeon = args.Contains("--dungeon"),
                AutoMapNorth = args.Contains("--map-north"),
                SaveCommandTo = Value("--save-command"),
            });
        }
        if (args.Contains("--screen-gallery"))
        {
            string[]? only = Value("--screen-gallery") is { } ids && !ids.StartsWith("--") ? ids.Split(',') : null;
            return new ScreenGallery.GalleryScreen
            {
                Only = only, ScreenshotDirectory = shots, PreviousDirectory = Value("--previous"), QuitWhenDone = quit,
                WindowSize = Value("--window")?.Split('x') is [var w, var h] ? new Vector2I(int.Parse(w), int.Parse(h)) : new Vector2I(1280, 800),
            };
        }
        if (args.Contains("--portraits"))
            return new GameScreen(new GameOptions { Start = GameStart.Title, Portraits = true, ScreenshotDirectory = shots, QuitWhenDone = quit });
        if (args.Contains("--options"))
            return new GameScreen(new GameOptions { Start = GameStart.Title, Options = true, ScreenshotDirectory = shots, QuitWhenDone = quit });
        if (args.Contains("--effects"))
        {
            string[]? only = Value("--effects") is { } names && !names.StartsWith("--") ? names.Split(',') : null;
            return new GameScreen(new GameOptions
            {
                Start = GameStart.Title, Effects = true, EffectsOnly = only, ScreenshotDirectory = shots, QuitWhenDone = quit,
            });
        }
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
