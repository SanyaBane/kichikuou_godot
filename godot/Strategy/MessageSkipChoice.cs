using Godot;
using Kichikuou.Engine.Script;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Messages → Skip unseen text and MsgSkip speeds up fades (docs/window-menu.md): the message skip's two
/// switches, both on by default, kept for later launches (owner, 2026-10-31; for the launch before). <see cref="EngineState"/>
/// gives each skip it opens the kept ones; the options window sets them here and on its skip.
/// </summary>
internal static class MessageSkipChoice
{
    private const string Section = "msgskip";
    private static bool? _skipUnseen, _fastFades;

    /// <summary>Whether unseen messages are skipped too: <c>[msgskip] skip_unseen</c> in <c>user://settings.cfg</c>, else they are. Setting it saves it for later launches.</summary>
    public static bool SkipUnseen
    {
        get => _skipUnseen ??= Read("skip_unseen");
        set => Write("skip_unseen", (_skipUnseen = value).Value);
    }

    /// <summary>Whether fades run fast while skipping: <c>[msgskip] fast_fades</c> in <c>user://settings.cfg</c>, else they do. Setting it saves it for later launches.</summary>
    public static bool FastFades
    {
        get => _fastFades ??= Read("fast_fades");
        set => Write("fast_fades", (_fastFades = value).Value);
    }

    /// <summary>Gives <paramref name="skip"/> the kept switches.</summary>
    public static void Apply(MessageSkip skip)
    {
        skip.SkipUnseen = SkipUnseen;
        skip.FastFades = FastFades;
    }

    private static bool Read(string key)
    {
        var config = new ConfigFile();
        return config.Load(GameData.SettingsPath) != Error.Ok || (bool)config.GetValue(Section, key, true);
    }

    private static void Write(string key, bool value)
    {
        var config = new ConfigFile();
        config.Load(GameData.SettingsPath);
        config.SetValue(Section, key, value);
        config.Save(GameData.SettingsPath);
    }
}
