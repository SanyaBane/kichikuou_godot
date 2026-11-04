using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Display → "Screen margins" (owner, 2026-10-31; docs/ui/screen-margins.md, the owner's C): the player's margins
/// from the screen's edges, upright and landscape each their own (<see cref="ScreenMargins"/>, in tenths of a millimetre),
/// kept in <c>[display] margins_upright</c> and <c>margins_landscape</c> of <c>user://settings.cfg</c> as
/// <c>Vector3i(top, bottom, sides)</c>, none by default. Offered and applied in the touch layout only (a phone, the
/// tablet, a PC with <c>--touch</c> or <c>--phone</c>); <see cref="Device"/> turns them into the canvas's units and every
/// screen is laid out inside them. The screen gallery holds them at <see cref="Override"/> for its dumps.
/// </summary>
internal static class ScreenMarginsChoice
{
    private static ScreenMargins? _portrait, _landscape;

    /// <summary>
    /// What the screen gallery shows instead of the player's set, both ways alike, never saved (null: the player's): none
    /// for every entry, the margins' own entries theirs.
    /// </summary>
    public static ScreenMargins? Override { get; set; }

    /// <summary>A set changed (the calibration, Option's Defaults): <see cref="Device"/> lays the screens out again.</summary>
    public static event Action? Changed;

    /// <summary>The set for the device held <paramref name="portrait"/> or landscape.</summary>
    public static ScreenMargins For(bool portrait)
    {
        if (Override is { } shown)
            return shown;
        ref var kept = ref portrait ? ref _portrait : ref _landscape;
        if (kept is null)
        {
            var config = new ConfigFile();
            var saved = config.Load(GameData.SettingsPath) == Error.Ok ? (Vector3I)config.GetValue("display", Key(portrait), Vector3I.Zero) : Vector3I.Zero;
            kept = new ScreenMargins(saved.X, saved.Y, saved.Z).Clamped();
        }
        return kept.Value;
    }

    /// <summary>Keeps <paramref name="margins"/> for the device held <paramref name="portrait"/> or landscape, at once.</summary>
    public static void Set(bool portrait, ScreenMargins margins)
    {
        margins = margins.Clamped();
        if (Override is null && For(portrait) == margins)
            return;
        if (portrait)
            _portrait = margins;
        else
            _landscape = margins;
        var config = new ConfigFile();
        config.Load(GameData.SettingsPath);
        config.SetValue("display", Key(portrait), new Vector3I(margins.Top, margins.Bottom, margins.Sides));
        config.Save(GameData.SettingsPath);
        Changed?.Invoke();
    }

    /// <summary>Holds the screen gallery's set (null: the player's again) and lays the screens out for it.</summary>
    public static void Show(ScreenMargins? shown)
    {
        if (Override == shown)
            return;
        Override = shown;
        Changed?.Invoke();
    }

    private static string Key(bool portrait) => portrait ? "margins_upright" : "margins_landscape";
}
