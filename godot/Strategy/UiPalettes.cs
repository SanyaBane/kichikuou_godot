using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The parts of our interface that can take a palette of their own. Each screen draws with its part's palette
/// (<see cref="UiPalettes.For"/>); for now the player chooses one palette for all of them, but a part's own choice is
/// already kept and read (a mixed palette, owner, 2026-10-24). A part is split by giving a screen a new member here.
/// </summary>
internal enum UiPart
{
    /// <summary>The battle screen and what comes before its field (a strategy's cut-in), its info boxes, the touch mode's rails and Skip.</summary>
    Battle,
    /// <summary>The map's command panel, its status strip, the province menu, the touch mode's Commands button.</summary>
    Map,
    /// <summary>The message windows, the scenes' menus, the message log, the touch mode's Skip and Log drawer.</summary>
    Messages,
    /// <summary>Our full screens: deployment, Subordinates, Harem, Treasure, Save/Load, an enemy attack, the week report.</summary>
    Screens,
    /// <summary>The options window and the import screen.</summary>
    Settings,
}

/// <summary>
/// The palettes chosen (Option → Display → Palette): one for every part, <c>[display] ui_palette</c> in
/// <c>user://settings.cfg</c>, and a part's own where <c>[ui_palette] &lt;part&gt;</c> names one (no option sets it yet).
/// <c>--palette &lt;key&gt;</c> sets every part for the launch (development). A change is kept at once; the screens redraw
/// through <see cref="IUiPaletteUser"/> and their <c>_Draw</c>.
/// </summary>
internal static class UiPalettes
{
    private const string Section = "display", Key = "ui_palette", PartsSection = "ui_palette";
    private static UiPalette? _all;
    private static Dictionary<UiPart, UiPalette>? _parts;

    /// <summary>The palette of every part without one of its own.</summary>
    public static UiPalette Chosen
    {
        get
        {
            if (_all is null)
                Load();
            return _all!;
        }
        set
        {
            _all = value;
            _parts?.Clear();                // one choice for all: the parts' own ones go
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue(Section, Key, value.Key);
            if (config.HasSection(PartsSection))
                config.EraseSection(PartsSection);
            config.Save(GameData.SettingsPath);
        }
    }

    /// <summary>
    /// Every part in <paramref name="palette"/> for this launch, the player's choice in <c>settings.cfg</c> left alone: the
    /// screen gallery's switch (docs/ui/gallery.md), as <c>--palette</c>.
    /// </summary>
    public static void Use(UiPalette palette)
    {
        if (_all is null)
            Load();
        _all = palette;
        _parts!.Clear();
    }

    /// <summary><paramref name="part"/>'s palette: its own, else <see cref="Chosen"/>.</summary>
    public static UiPalette For(UiPart part)
    {
        if (_all is null)
            Load();
        return _parts!.TryGetValue(part, out var own) ? own : _all!;
    }

    /// <summary>
    /// Every node under <paramref name="root"/> drawn again in the palettes as they are now: <see cref="IUiPaletteUser"/>s
    /// set what they keep (theme colours, style boxes) again, and every canvas item redraws.
    /// </summary>
    public static void Repaint(Node root)
    {
        if (root is IUiPaletteUser user)
            user.ApplyPalette();
        if (root is CanvasItem item)
            item.QueueRedraw();
        foreach (var child in root.GetChildren())
            Repaint(child);
    }

    private static void Load()
    {
        _parts = [];
        var args = OS.GetCmdlineUserArgs();
        int at = Array.IndexOf(args, "--palette");
        if (at >= 0 && at + 1 < args.Length && UiPalette.Named(args[at + 1]) is { } given)
        {
            _all = given;
            return;
        }
        var config = new ConfigFile();
        if (config.Load(GameData.SettingsPath) != Error.Ok)
        {
            _all = UiPalette.Lacquer;
            return;
        }
        _all = UiPalette.Named((string)config.GetValue(Section, Key, "")) ?? UiPalette.Lacquer;
        foreach (var part in Enum.GetValues<UiPart>())
            if (UiPalette.Named((string)config.GetValue(PartsSection, part.ToString().ToLowerInvariant(), "")) is { } own)
                _parts[part] = own;
    }
}

/// <summary>A node that keeps colours of its part's palette outside its <c>_Draw</c> (theme colours, style boxes, labels).</summary>
internal interface IUiPaletteUser
{
    /// <summary>Sets what it keeps again from the palettes as they are now.</summary>
    void ApplyPalette();
}
