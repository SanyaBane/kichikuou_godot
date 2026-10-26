using Godot;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The unit ledger's tab each screen opens on (the owner, 2026-10-26; docs/ui/unit-ledger.md): the one the player left
/// there, each screen its own — <c>[display] ledger_tab_&lt;screen&gt;</c> in <c>user://settings.cfg</c>, else Overview.
/// </summary>
internal static class LedgerTabChoice
{
    private static readonly Dictionary<string, LedgerTab> Chosen = [];

    /// <summary>The tab <paramref name="screen"/> ("deployment", "subordinates") opens on.</summary>
    public static LedgerTab Get(string screen)
    {
        if (Chosen.TryGetValue(screen, out var tab))
            return tab;
        var config = new ConfigFile();
        string? saved = config.Load(GameData.SettingsPath) == Error.Ok ? (string)config.GetValue("display", Key(screen), "") : null;
        return Chosen[screen] = LedgerTabs.Parse(saved);
    }

    /// <summary>Remembers <paramref name="tab"/> for <paramref name="screen"/>, for this launch and the later ones.</summary>
    public static void Set(string screen, LedgerTab tab)
    {
        if (Chosen.TryGetValue(screen, out var was) && was == tab)
            return;
        Chosen[screen] = tab;
        var config = new ConfigFile();
        config.Load(GameData.SettingsPath);
        config.SetValue("display", Key(screen), tab.ToString());
        config.Save(GameData.SettingsPath);
    }

    private static string Key(string screen) => $"ledger_tab_{screen}";
}
