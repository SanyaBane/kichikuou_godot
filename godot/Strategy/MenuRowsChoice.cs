using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Debug → Menu Row Height (owner, 2026-10-25, for trying sizes out, not for the release): the height of the province
/// menu's and the command panel's rows on the PC, 18 to 60 window pixels, 38 by default (the owner's A1 and M1,
/// docs/ui/command-screen.md, "Taller rows"), their text and tags following it (<see cref="MenuRows"/>); the touch mode
/// keeps its own rows. A second slider sets the scenes' <c>]</c> menus' rows (<see cref="SceneMenuView"/>), 36 by default as
/// they were, in every mode.
/// </summary>
internal static class MenuRowsChoice
{
    public const int Min = 18, Max = 60, Default = 38, DialogueDefault = 36;

    private static int? _height, _dialogueHeight;

    /// <summary>Raised when either height changes, so open menus lay themselves out again.</summary>
    public static event Action? Changed;

    /// <summary>
    /// The menus' rows: <c>--menu-rows n</c> for the launch (development), else <c>[debug] menu_row_height</c> in
    /// <c>user://settings.cfg</c>, else <see cref="Default"/>. Setting it saves it for later launches.
    /// </summary>
    public static int Height
    {
        get => _height ??= Load("--menu-rows", "menu_row_height", Default);
        set => Set(ref _height, value, "menu_row_height");
    }

    /// <summary>The <c>]</c> menus' rows: <c>--dialogue-rows n</c>, else <c>[debug] dialogue_row_height</c>, else <see cref="DialogueDefault"/>.</summary>
    public static int DialogueHeight
    {
        get => _dialogueHeight ??= Load("--dialogue-rows", "dialogue_row_height", DialogueDefault);
        set => Set(ref _dialogueHeight, value, "dialogue_row_height");
    }

    public static MenuRows Rows => new(Height);

    private static int Load(string switchName, string key, int fallback)
    {
        var args = OS.GetCmdlineUserArgs();
        int at = Array.IndexOf(args, switchName);
        int height = fallback;
        if (at >= 0 && at + 1 < args.Length && int.TryParse(args[at + 1], out int given))
            height = given;
        else
        {
            var config = new ConfigFile();
            if (config.Load(GameData.SettingsPath) == Error.Ok)
                height = (int)config.GetValue("debug", key, fallback);
        }
        return Math.Clamp(height, Min, Max);
    }

    private static void Set(ref int? cache, int value, string key)
    {
        value = Math.Clamp(value, Min, Max);
        if (cache == value)
            return;
        cache = value;
        var config = new ConfigFile();
        config.Load(GameData.SettingsPath);
        config.SetValue("debug", key, value);
        config.Save(GameData.SettingsPath);
        Changed?.Invoke();
    }
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
