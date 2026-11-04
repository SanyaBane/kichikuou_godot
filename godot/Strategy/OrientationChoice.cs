using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Display → "Screen orientation", a phone's only (docs/ui/phone-build.md, §3 and step P0; the owner's PO1o,
/// docs/ui/phone.md, §21): <see cref="Way.System"/> (the default: as the phone is held, unless its own rotation lock holds
/// it), <see cref="Way.Portrait"/>, <see cref="Way.Landscape1"/> (the phone's top at the left) or
/// <see cref="Way.Landscape2"/> (at the right), kept in <c>[display] orientation</c> of <c>user://settings.cfg</c>. On an
/// Android phone it is the activity's requested orientation, through <see cref="DisplayServer.ScreenSetOrientation"/>:
/// Godot 4.7.2's Android library asks Android for <c>fullUser</c> on <c>Sensor</c> (the sensor's four ways unless the
/// rotation lock is on, then the way the phone was), <c>portrait</c> on <c>Portrait</c>, <c>landscape</c> (the top at the
/// left) on <c>Landscape</c> and <c>reverseLandscape</c> on <c>ReverseLandscape</c>. A tablet keeps the project's sensor
/// landscape (<c>userLandscape</c>) and is never offered the choice. On a PC playing a phone (<c>--phone</c>) a choice
/// turns the window, as the phone would turn (System leaves it as it is).
/// </summary>
internal static class OrientationChoice
{
    public enum Way { System, Portrait, Landscape1, Landscape2 }

    /// <summary>The ways in the order Option lists them, each with its name and the line under it (PO1o).</summary>
    public static readonly (Way Way, string Name, string Hint)[] All =
    [
        (Way.System, "System", "As the phone is held, unless its rotation lock holds it."),
        (Way.Portrait, "Portrait", "Always, however the phone is held."),
        (Way.Landscape1, "Landscape 1", "Always, the phone's top at the left."),
        (Way.Landscape2, "Landscape 2", "Always, the phone's top at the right."),
    ];

    private static Way? _chosen;

    /// <summary>
    /// The chosen way: <c>[display] orientation</c> in <c>user://settings.cfg</c>, else System. Setting it saves it for
    /// later launches and applies it at once.
    /// </summary>
    public static Way Chosen
    {
        get
        {
            if (_chosen is { } chosen)
                return chosen;
            var config = new ConfigFile();
            int saved = config.Load(GameData.SettingsPath) == Error.Ok ? (int)config.GetValue("display", "orientation", (int)Way.System) : 0;
            return (_chosen = Enum.IsDefined((Way)saved) ? (Way)saved : Way.System).Value;
        }
        set
        {
            _chosen = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("display", "orientation", (int)value);
            config.Save(GameData.SettingsPath);
            Apply();
            if (Device.PlaysPhone && value != Way.System)
                Device.TurnWindow(value == Way.Portrait);
        }
    }

    /// <summary>The chosen way on an Android phone (at the launch, and after each change); nothing elsewhere.</summary>
    public static void Apply()
    {
        if (OS.GetName() != "Android" || !Device.Phone)
            return;
        DisplayServer.ScreenSetOrientation(Chosen switch
        {
            Way.Portrait => DisplayServer.ScreenOrientation.Portrait,
            Way.Landscape1 => DisplayServer.ScreenOrientation.Landscape,
            Way.Landscape2 => DisplayServer.ScreenOrientation.ReverseLandscape,
            _ => DisplayServer.ScreenOrientation.Sensor,
        });
        GD.Print($"Kichikuou: the orientation {Chosen}, the screen's {DisplayServer.ScreenGetOrientation()}.");
    }
}
