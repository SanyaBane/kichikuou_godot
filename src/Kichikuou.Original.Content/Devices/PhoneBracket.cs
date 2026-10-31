namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The bracket that holds an army's front row on the phone held upright (docs/ui/phone.md, §31-§32): the PC's
/// (<c>EnemyAttackView.DrawArmyBracket</c>, docs/ui/enemy-attack.md, §8) turned to face up or down and scaled as the
/// cards are (573 / 252). Its line runs across the row's outer side from the row's one end to the other, its ends turn to
/// the cards, its arms bend to a head in the middle that points the way the army faces: up at the province on the enemy
/// attack (the owner's PA1f2a, <see cref="PhoneAttackUpright.Bracket"/>), down as our army in the battle on the deployment
/// table (PD2f2, <see cref="PhoneLedgerUpright.Bracket"/>). Its figures in the reference phone's pixels, from its line.
/// Interface only. Engine-independent.
/// </summary>
public static class PhoneBracket
{
    /// <summary>
    /// The line's width; its ends' stop short of the cards; each arm's end beside the middle, bending to this far beside it
    /// and this far off the line; the head's base and tip off the line, its half width.
    /// </summary>
    public const float Width = 5.7f, EndsShort = 4.5f, ArmEnd = 36, BendIn = 14, BendOut = 23, HeadBase = 14, HeadTip = 75,
        HeadHalf = 32;
}
