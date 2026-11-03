using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Display → "Frame rate" (owner, 2026-11-03; docs/ui/options.md, "Frame rate"): how often Godot draws at most,
/// for the phone's battery — 30, 60 or the screen's own rate (<see cref="Screen"/>: no limit, vsync alone; 120 Hz on the
/// reference phone, a monitor's rate on a PC). Kept in <c>[display] frame_rate</c> of <c>user://settings.cfg</c> (30, 60 or
/// 0), else <see cref="Default"/>: 30 on Android, 60 elsewhere — each device its own, since each keeps its own settings.
/// It is <see cref="Godot.Engine.MaxFps"/>: on Android Godot hands it to the system's frame pacer (Swappy), so 30 and 60 come
/// evenly every fourth or second refresh of a 120 Hz screen; on a PC it limits on top of vsync. Not applied headless nor
/// with <c>--fast</c>, whose unattended runs go as fast as the machine allows (<see cref="Apply"/>).
/// </summary>
internal static class FrameRateChoice
{
    /// <summary>The screen's own rate: no limit.</summary>
    public const int Screen = 0;

    /// <summary>The rates Option offers, in its order.</summary>
    public static readonly int[] Rates = [30, 60, Screen];

    /// <summary>This device's default: 30 on Android, 60 elsewhere (a PC playing a phone too).</summary>
    public static int Default => OS.GetName() == "Android" ? 30 : 60;

    private static int? _chosen;

    /// <summary>The chosen rate (<see cref="Screen"/> for none); setting it saves it for later launches and applies it at once.</summary>
    public static int Chosen
    {
        get
        {
            if (_chosen is { } chosen)
                return chosen;
            var config = new ConfigFile();
            int saved = config.Load(GameData.SettingsPath) == Error.Ok ? (int)config.GetValue("display", "frame_rate", Default) : Default;
            return (_chosen = Rates.Contains(saved) ? saved : Default).Value;
        }
        set
        {
            if (!Rates.Contains(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Not a rate Option offers.");
            _chosen = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("display", "frame_rate", value);
            config.Save(GameData.SettingsPath);
            Apply();
        }
    }

    /// <summary>The screen's own rate in whole hertz, or null when the display does not tell it.</summary>
    public static int? ScreenHz => DisplayServer.ScreenGetRefreshRate() is var hz and > 0 ? (int)Math.Round(hz) : null;

    /// <summary>A rate's name in Option: "30 fps", "60 fps", "Screen: 120 Hz" (or "Screen's own"), the default marked.</summary>
    public static string Name(int rate)
    {
        string name = rate == Screen ? ScreenHz is { } hz ? $"Screen: {hz} Hz" : "Screen's own" : $"{rate} fps";
        return rate == Default ? name + " — default" : name;
    }

    /// <summary>The line under a rate's name: the least power first, the screen's the most.</summary>
    public static string Hint(int rate) => rate switch
    {
        30 => OS.GetName() == "Android" ? "Saves the battery." : "Draws half as often: the least power.",
        60 => "Smoother effects and scrolling.",
        _ => "As fast as the screen: the most power.",
    };

    /// <summary>
    /// The chosen rate on the engine (at the launch, and after each change); headless and with <c>--fast</c> none, the
    /// unattended runs' speed left to the machine.
    /// </summary>
    public static void Apply()
    {
        bool development = DisplayServer.GetName() == "headless" || OS.GetCmdlineUserArgs().Contains("--fast");
        Godot.Engine.MaxFps = development ? 0 : Chosen;
        GD.Print($"Kichikuou: the frame rate {(Godot.Engine.MaxFps == 0 ? "the screen's" : Godot.Engine.MaxFps + " fps")}"
            + (development ? " (headless or --fast: no limit)" : "") + ".");
    }
}
