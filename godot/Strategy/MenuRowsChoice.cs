using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Debug → Menu Row Height (owner, 2026-10-25, for trying sizes out, not for the release): the height of the province
/// menu's and the command panel's rows on the PC, 18 to 60 window pixels, 38 by default (the owner's A1 and M1,
/// docs/ui/command-screen.md, "Taller rows"); their text and tags follow it (<see cref="MenuRows"/>). The touch mode
/// keeps its own rows.
/// </summary>
internal static class MenuRowsChoice
{
    public const int Min = 18, Max = 60, Default = 38;

    private static int? _height;

    /// <summary>Raised when <see cref="Height"/> changes, so open menus lay themselves out again.</summary>
    public static event Action? Changed;

    /// <summary>
    /// The rows' height: <c>--menu-rows n</c> for the launch (development), else <c>[debug] menu_row_height</c> in
    /// <c>user://settings.cfg</c>, else <see cref="Default"/>. Setting it saves it for later launches.
    /// </summary>
    public static int Height
    {
        get
        {
            if (_height is { } chosen)
                return chosen;
            var args = OS.GetCmdlineUserArgs();
            int at = Array.IndexOf(args, "--menu-rows");
            int height = Default;
            if (at >= 0 && at + 1 < args.Length && int.TryParse(args[at + 1], out int given))
                height = given;
            else
            {
                var config = new ConfigFile();
                if (config.Load(GameData.SettingsPath) == Error.Ok)
                    height = (int)config.GetValue("debug", "menu_row_height", Default);
            }
            return (_height = Math.Clamp(height, Min, Max)).Value;
        }
        set
        {
            value = Math.Clamp(value, Min, Max);
            if (value == Height)
                return;
            _height = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("debug", "menu_row_height", value);
            config.Save(GameData.SettingsPath);
            Changed?.Invoke();
        }
    }

    public static MenuRows Rows => new(Height);
}

/// <summary>
/// The sizes of our menus' rows on the PC drawn from their height (<see cref="MenuRowsChoice"/>): at 38 they are the owner's
/// A1 and M1 (text 19, the tag 74×26 inset 6 with its word at 15, 7 between rows); the text grows slower than the row
/// (13 at 18, 25 at 60), and widths grow with the text.
/// </summary>
internal readonly record struct MenuRows(int Height)
{
    /// <summary>A command's text.</summary>
    public int Text => (int)MathF.Round(8 + 0.29f * Height);

    /// <summary>The word on a command's tag.</summary>
    public int TagText => Math.Max(10, Text - 4);

    /// <summary>"used this week".</summary>
    public int Small => Math.Max(10, (int)MathF.Round(Text * 0.68f));

    /// <summary>How much wider than at 38 things holding text are.</summary>
    public float Scale => Text / 19f;

    public float TagInset => MathF.Round(Height * 6 / 38f);

    public float TagWidth => MathF.Round(74 * Scale);

    /// <summary>Where a command's text starts, from its row's left edge.</summary>
    public float TextLeft => TagInset + TagWidth + 13;

    /// <summary>The space between two commands.</summary>
    public float Gap => Math.Clamp(MathF.Round(Height * 0.18f), 3, 7);

    public float Pitch => Height + Gap;
}
