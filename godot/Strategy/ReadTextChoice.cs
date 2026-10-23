using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Messages → "Already-read text" (docs/ui/read-text.md, after Rance X's row): the colour our message windows
/// draw a window's text in when the player read every message on it before — one of nine, or none ("Don't change").
/// </summary>
internal static class ReadTextChoice
{
    /// <summary>The nine colours, after Rance X's row, left to right.</summary>
    public static readonly (string Name, Color Colour)[] Colours =
    [
        ("Salmon", Color.Color8(240, 120, 120)), ("Rose", Color.Color8(240, 128, 184)), ("Magenta", Color.Color8(255, 128, 255)),
        ("Violet", Color.Color8(192, 128, 255)), ("Periwinkle", Color.Color8(128, 128, 255)), ("Sky", Color.Color8(128, 184, 255)),
        ("Cyan", Color.Color8(128, 255, 255)), ("Mint", Color.Color8(128, 255, 192)), ("Green", Color.Color8(128, 255, 128)),
    ];

    /// <summary>The owner's default (2026-10-23): the first colour, Salmon.</summary>
    public const int DefaultIndex = 0;

    /// <summary>"Don't change": no colour.</summary>
    public const int None = -1;

    /// <summary>A thought's read text: the colour as dim as <see cref="Lacquer.Quiet"/> is against <see cref="Lacquer.Text"/>.</summary>
    private const float ThoughtDim = 0.72f;

    private static int? _index;

    /// <summary>
    /// The chosen colour's index in <see cref="Colours"/>, or <see cref="None"/>: <c>[display] read_text_colour</c> in
    /// <c>user://settings.cfg</c>, else <see cref="DefaultIndex"/>. Setting it saves it for later launches.
    /// </summary>
    public static int Index
    {
        get
        {
            if (_index is { } chosen)
                return chosen;
            var config = new ConfigFile();
            int saved = config.Load(GameData.SettingsPath) == Error.Ok
                ? (int)config.GetValue("display", "read_text_colour", DefaultIndex) : DefaultIndex;
            return (_index = Valid(saved)).Value;
        }
        set
        {
            _index = Valid(value);
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("display", "read_text_colour", _index.Value);
            config.Save(GameData.SettingsPath);
        }
    }

    /// <summary>The chosen colour, or null for "Don't change".</summary>
    public static Color? Colour => Index == None ? null : Colours[Index].Colour;

    /// <summary>The colour a read window's text takes in place of <paramref name="plain"/>, its own; a thought's dimmed.</summary>
    public static Color Of(Color colour, Color plain) =>
        plain == Lacquer.Quiet ? new Color(colour.R * ThoughtDim, colour.G * ThoughtDim, colour.B * ThoughtDim) : colour;

    private static int Valid(int index) => index >= None && index < Colours.Length ? index : DefaultIndex;
}
