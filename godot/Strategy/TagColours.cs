using Godot;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The command tags' colours (ours: the original has no tags), the same in every palette: the status strip's tag of a
/// week's use, and every command that spends it in that use's colour — the command panel's (<see cref="CommandMenu.UseOf"/>)
/// and the province menu's (<see cref="ProvinceMenu.UseOf"/>), on the PC, the rails and the phone's sheets alike; a command
/// that spends none has a colour of its own (owner, 2026-10-31, mock-ups TC2 and TC3; docs/ui/command-screen.md, "The
/// tags' colours").
/// </summary>
public static class TagColours
{
    /// <summary>The colour of the week's use <paramref name="use"/>.</summary>
    public static Color Of(WeekAction use) => use switch
    {
        WeekAction.Build => Color.Color8(172, 84, 30),
        WeekAction.Harem => Color.Color8(128, 44, 70),
        WeekAction.Trade => Color.Color8(138, 104, 34),
        WeekAction.Units or WeekAction.Troops or WeekAction.Talk => Color.Color8(46, 62, 104),
        WeekAction.City => Color.Color8(70, 96, 52),
        WeekAction.Attack => Color.Color8(128, 30, 30),
        WeekAction.Dungeon => Color.Color8(36, 92, 96),
        _ => throw new ArgumentOutOfRangeException(nameof(use)),
    };

    /// <summary>Treasure's: it spends no use.</summary>
    public static Color Items { get; } = Color.Color8(104, 70, 34);

    /// <summary>Ninja Maneuver's: it spends each ninja's own week, not one of the strip's.</summary>
    public static Color Ninja { get; } = Color.Color8(74, 50, 96);

    /// <summary>The command panel's tag of <paramref name="action"/>.</summary>
    public static Color Of(CommandAction action) => CommandMenu.UseOf(action) is { } use ? Of(use) : Items;

    /// <summary>The province menu's tag of <paramref name="action"/>.</summary>
    public static Color Of(ProvinceAction action) => ProvinceMenu.UseOf(action) is { } use ? Of(use) : Ninja;
}
