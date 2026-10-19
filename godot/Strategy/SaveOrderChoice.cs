using Godot;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The order of our Save/Load screen's manual saves (owner's request 2026-10-20, docs/ui/save-load.md, §7): the last one the
/// player chose, kept for later launches; by slot ascending, as the original lists them, until one is chosen.
/// </summary>
internal static class SaveOrderChoice
{
    private static SlotOrder? _order;

    /// <summary>
    /// <c>[display] save_order</c> ("slot" or "date") and <c>save_order_descending</c> in <c>user://settings.cfg</c>, else
    /// <see cref="SlotOrder.Default"/>. Setting it saves it.
    /// </summary>
    public static SlotOrder Order
    {
        get
        {
            if (_order is { } chosen)
                return chosen;
            var config = new ConfigFile();
            var saved = SlotOrder.Default;
            if (config.Load(GameData.SettingsPath) == Error.Ok)
            {
                var key = (string)config.GetValue("display", "save_order", "slot") == "date" ? SlotSortKey.Date : SlotSortKey.Slot;
                saved = new SlotOrder(key, (bool)config.GetValue("display", "save_order_descending", false));
            }
            return (_order = saved).Value;
        }
        set
        {
            _order = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("display", "save_order", value.Key == SlotSortKey.Date ? "date" : "slot");
            config.SetValue("display", "save_order_descending", value.Descending);
            config.Save(GameData.SettingsPath);
        }
    }
}
