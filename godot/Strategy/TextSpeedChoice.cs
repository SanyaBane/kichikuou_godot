using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Text Speed… (docs/ui/messages.md, §5 step 4): how fast our message windows type their text, a step of ten —
/// 1 to 9 letters a second from <see cref="LettersPerSecond"/>, 10 the whole text at once.
/// </summary>
internal static class TextSpeedChoice
{
    public const int Steps = 10;

    /// <summary>The owner's default for now (2026-10-18).</summary>
    public const int DefaultStep = 9;

    private static readonly double[] Speeds = [8, 12, 18, 25, 35, 50, 70, 100, 150, double.PositiveInfinity];

    private static int? _step;

    /// <summary>The letters a second of <paramref name="step"/> (1-10); infinity at 10, the whole text at once.</summary>
    public static double LettersPerSecond(int step) => Speeds[Math.Clamp(step, 1, Steps) - 1];

    /// <summary>The chosen step: <c>[display] text_speed</c> in <c>user://settings.cfg</c>, else <see cref="DefaultStep"/>. Setting it saves it for later launches.</summary>
    public static int Step
    {
        get
        {
            if (_step is { } chosen)
                return chosen;
            var config = new ConfigFile();
            int saved = config.Load(GameData.SettingsPath) == Error.Ok ? (int)config.GetValue("display", "text_speed", DefaultStep) : DefaultStep;
            return (_step = Math.Clamp(saved, 1, Steps)).Value;
        }
        set
        {
            _step = Math.Clamp(value, 1, Steps);
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("display", "text_speed", _step.Value);
            config.Save(GameData.SettingsPath);
        }
    }
}
