namespace Kichikuou.Original.Content.Battles;

/// <summary>
/// A successful pre-battle strategy (<c>FIGT作.ADV</c>, docs/battle/01-flow.md §4) as our cut-in shows it
/// (docs/ui/battle-screen.md, "The strategy's cut-in"; owner, 2026-10-15, variant A): the side whose strategist
/// succeeded, the strategist's whole portrait, the script's announcement (window 143's first box), the strategist's own
/// line (the speech bubble 141/142/151/152, word for word) and the result as its label (no sentence of what it does:
/// owner, 2026-10-15 — the label's tooltip on the board tells it). The script fills it in as it goes:
/// <see cref="Result"/> is set after the announcement's key wait, <see cref="Line"/> when the bubble prints.
/// </summary>
/// <param name="Side">The strategist's side (<c>FIGT作</c>'s <c>CallArg</c> 1 the player, 2 the enemy).</param>
/// <param name="Record">The strategist's record: a unit (<c>Bt_PlayerStrategist</c>) or a commander (<c>Bt_EnemyStrategist</c>).</param>
/// <param name="Portrait">Its face CG, the framed 192×256 picture the original's strategy screen shows (0: none).</param>
/// <param name="Announcement">The script's first box flowed into one line ("Ally army's strategy was successful."), "" before it prints.</param>
/// <param name="Result"><c>Bt_StrategyResult</c>: 1 / 2 the player's success / great success, 11 / 12 the enemy's, 0 before it is set.</param>
/// <param name="Line">The strategist's line, null before the bubble prints.</param>
public sealed record BattleStrategy(BattleSide Side, int Record, int Portrait, string Announcement, int Result, BattleSpeech? Line)
{
    /// <summary>The troop damage the result adds to its side and takes from the other's, in percent (0 before it is set).</summary>
    public int Percent => Result switch
    {
        1 or 11 => 10,
        2 or 12 => 50,
        _ => 0,
    };

    /// <summary>A great success (the roll's margin above 50): +50%.</summary>
    public bool Great => Result is 2 or 12;

    /// <summary>The cut-in's title under the announcement: "Success", +50% "Great Success" (owner, 2026-10-16: no "Strategy" in it).</summary>
    public string Title => Great ? "Great Success" : "Success";

    /// <summary>The advantage as the opening and the armies' labels go on to show it ("Strategy +10%"); "" before the result.</summary>
    public string Label => Percent == 0 ? "" : new BattleAdvantage(0, Percent).Text;
}
