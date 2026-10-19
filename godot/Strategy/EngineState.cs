using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// What the engine keeps for the whole run of the program: xsystem35's Restart (<c>sys_reset</c>)
/// clears the interpreter but not the random generator (seeded once at start-up) nor the message
/// skip (docs/window-menu.md). Our Restart goes back to the title and the next game starts on a
/// fresh <see cref="Original.State.GameState"/>, with these.
/// </summary>
internal static class EngineState
{
    private static IScriptRandom? _random;

    /// <summary>
    /// Option → Mouse Movement (<c>ags.mouse_warp_enabled</c>, on at start as <c>menu_init</c>): the
    /// script's <c>IZ</c> moves the system pointer; off, only the position the script reads.
    /// </summary>
    public static bool MouseMovement { get; set; } = true;
    private static readonly Dictionary<string, MessageSkip> Skips = [];

    /// <summary>The message log, kept for the launch (docs/ui/messages.md, §5 step 6; owner, 2026-10-19): not saved.</summary>
    public static Original.Content.Strategy.MessageLog Log { get; } = new();

    /// <summary>
    /// The random generator, seeded on first use with <c>--seed N</c> or the tick count; with
    /// <c>--rnglog</c> every draw is printed (<see cref="RandomLog"/>).
    /// </summary>
    public static IScriptRandom Random
    {
        get
        {
            if (_random is null)
            {
                var args = OS.GetCmdlineUserArgs();
                int at = Array.IndexOf(args, "--seed");
                uint seed = at >= 0 && at + 1 < args.Length && uint.TryParse(args[at + 1], out uint s)
                    ? s : (uint)System.Environment.TickCount;
                GD.Print($"Kichikuou: random seed {seed}");
                _random = new XSystem35Random(seed);
                if (args.Contains("--rnglog"))
                    _random = new RandomLog(_random);
            }
            return _random;
        }
    }

    /// <summary>The message skip whose seen messages are kept with the save slots in <paramref name="saveDirectory"/>.</summary>
    public static MessageSkip Skip(string saveDirectory)
    {
        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(saveDirectory, "msgskip.txt"));
        if (!Skips.TryGetValue(path, out var skip))
            Skips[path] = skip = MessageSkip.Open(path);
        return skip;
    }
}
