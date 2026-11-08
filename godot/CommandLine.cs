using Godot;

namespace Kichikuou.Game;

/// <summary>
/// The development switches, the arguments after <c>--</c> on the command line (CLAUDE.md, "Commands"): Godot's user
/// arguments in a development build — the editor's runs and the debug exports —, none in a release export, which plays
/// as a player's whatever it is started with (owner, 2026-11-08; docs/release.md). Every switch is read through here,
/// never through <see cref="OS.GetCmdlineUserArgs"/>.
/// </summary>
public static class CommandLine
{
    private static string[]? _args;

    public static string[] Args => _args ??= OS.IsDebugBuild() ? OS.GetCmdlineUserArgs() : [];
}
