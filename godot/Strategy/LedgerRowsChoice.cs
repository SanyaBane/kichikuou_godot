using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Display → "Unit tables: row height" (the owner, 2026-10-26; docs/ui/unit-ledger.md §8, the owner's O2): the
/// height of the Subordinates and deployment ledgers' rows, in the 1280×800 screen's pixels, one of <see cref="Heights"/>.
/// The PC and the touch mode keep their own — <c>[display] ledger_row_height</c> and <c>ledger_row_height_touch</c> in
/// <c>user://settings.cfg</c>, else 24 on the PC (the text's own row) and 36 in the touch mode (the owner's E2, §7).
/// </summary>
internal static class LedgerRowsChoice
{
    /// <summary>The heights Option offers, in its order.</summary>
    public static readonly int[] Heights = [24, 30, 36, 42, 48];

    /// <summary>The text's own row: at it the rows are as tall as their text makes them.</summary>
    public const int TextRow = 24;

    /// <summary>This mode's default: the PC's 24, the touch mode's 36.</summary>
    public static int Default => TouchMode.On ? 36 : TextRow;

    private static string Key => TouchMode.On ? "ledger_row_height_touch" : "ledger_row_height";

    private static int? _chosen;

    /// <summary>The chosen height; setting it saves it for later launches (the tables follow at their next frame).</summary>
    public static int Chosen
    {
        get
        {
            if (_chosen is { } chosen)
                return chosen;
            var config = new ConfigFile();
            int saved = config.Load(GameData.SettingsPath) == Error.Ok ? (int)config.GetValue("display", Key, Default) : Default;
            return (_chosen = Heights.Contains(saved) ? saved : Default).Value;
        }
        set
        {
            if (!Heights.Contains(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Not a height Option offers.");
            _chosen = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("display", Key, value);
            config.Save(GameData.SettingsPath);
        }
    }
}
