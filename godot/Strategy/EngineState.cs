using Godot;
using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// What the engine keeps for the whole run of the program: xsystem35's Restart (<c>sys_reset</c>)
/// clears the interpreter but not the random generator (seeded once at start-up) nor the message
/// skip (docs/window-menu.md). Our Restart goes back to the title and the next game starts on a
/// fresh <see cref="Core.State.GameState"/>, with these.
/// </summary>
internal static class EngineState
{
    private static IScriptRandom? _random;
    private static readonly Dictionary<string, MessageSkip> Skips = [];

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
