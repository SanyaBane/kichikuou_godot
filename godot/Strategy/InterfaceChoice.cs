using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own screens over the original layout (owner's decision 25), or the original's. Since 2026-10-21 (owner) the
/// player always has ours: Option → Interface is gone and the setting it saved is not read; the original's screens
/// are left for development checks (<c>--interface original</c>).
/// </summary>
internal static class InterfaceChoice
{
    private static bool? _new;

    /// <summary>Whether our own screens are used: <c>--interface original|new</c> for the launch (development), else ours.</summary>
    public static bool New
    {
        get
        {
            if (_new is { } chosen)
                return chosen;
            var args = OS.GetCmdlineUserArgs();
            int at = Array.IndexOf(args, "--interface");
            bool given = at < 0 || at + 1 >= args.Length || Parse(args[at + 1]) is not { } named || named;
            return (_new = given).Value;
        }
    }

    private static bool? Parse(string name) => name switch
    {
        "original" => false,
        "new" => true,
        _ => null,
    };
}
