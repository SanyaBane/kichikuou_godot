namespace Kichikuou.Original.Combat;

/// <summary>
/// Ours (owner's decision 32, the fourth rule change on purpose; docs/tweaks.md): BRS-24's optional fix, "Chaos and
/// Nikkou reach demons", the player's choice in Option → Tweaks. Against a demon commander (class over 9) the holder of
/// Chaos (item 1) or Nikkou (item 2), whose troops exceed the battle's limit, stays behind them in the original and so
/// cannot wound him; with the fix he fights in person all the same (<see cref="Battle.ComputeExchange"/>), shown with
/// his troops.
/// </summary>
public enum DemonReach
{
    /// <summary>The original's rule (BRS-19, BRS-24): the holder fights in person only while his troops fit the limit.</summary>
    Original,

    /// <summary>The owner's A: the holder counts as on the field — he fights in person, and the demon's troops may shoot at him.</summary>
    OnField,

    /// <summary>The owner's B: the holder fights in person, but the demon's troops never shoot at him.</summary>
    OutOfReach,
}
