using Kichikuou.Engine.Script;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// Our command panel's Construction (owner, 2026-10-25; docs/ui/command-screen.md, "The command panel"): it calls
/// <c>Cﾃｲﾄ.ADV</c>, whose first menu is Build / Don't (<c>L_00155</c>, <c>:60-71</c>). This answers that menu unseen:
/// Build the first time, so the command goes straight to Maris's refusal or the buildings' list; Don't when the menu
/// comes back (the list's Don't Build), so the player is back on the map. Interface only: the script runs as it is.
/// </summary>
public sealed class ConstructionShortcut
{
    private const string File = "Cﾃｲﾄ.ADV";
    private const int Build = 1, Dont = 2;

    private bool _running, _opened;

    /// <summary>Whether a Construction is under way: the menu is answered by this until it is left.</summary>
    public bool Running => _running;

    /// <summary>The panel answered: <paramref name="construction"/> when it chose an enabled Construction.</summary>
    public void Start(bool construction)
    {
        _running = construction;
        _opened = false;
    }

    /// <summary>The answer to <paramref name="items"/> when they are the Build / Don't menu and a Construction is under way; else null.</summary>
    public int? Choose(IReadOnlyList<SceneMenuItem> items)
    {
        if (!_running || !IsMenu(items))
            return null;
        if (!_opened)
        {
            _opened = true;
            return Build;
        }
        Start(false);
        return Dont;
    }

    /// <summary>Whether <paramref name="items"/> are <c>Cﾃｲﾄ.ADV</c>'s Build / Don't (<c>:62, 66</c>).</summary>
    public static bool IsMenu(IReadOnlyList<SceneMenuItem> items) =>
        items.Count == 2 && items.All(i => i.At.File == File) && items[0].At.Line == 62 && items[1].At.Line == 66;
}
