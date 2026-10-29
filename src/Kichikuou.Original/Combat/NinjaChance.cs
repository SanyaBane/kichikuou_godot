namespace Kichikuou.Original.Combat;

/// <summary>
/// Ours (owner's decision 33, the fifth rule change on purpose; docs/tweaks.md): the chance of the ninja follow-up, the
/// player's choice in Option → Tweaks. After an exchange of Menad's (unit 10), Hubert's (unit 21), Gandhi's (unit 37) or
/// Magic's (unit 39) their ninja strikes the enemy commander for 1 HP when R14, the <c>RND(3)</c> of
/// <c>FIGT.ADV:1915</c>, is 1 (BRS-11); the choice reads the same draw otherwise (<see cref="Battle.NinjaRolled"/>).
/// </summary>
public enum NinjaChance
{
    /// <summary>The original's 1 in 3: R14 = 1.</summary>
    Original,

    /// <summary>2 in 3: R14 = 1 or 2.</summary>
    TwoInThree,

    /// <summary>Always: every exchange that meets the other conditions.</summary>
    Always,
}
