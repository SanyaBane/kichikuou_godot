namespace Kichikuou.Original.Content.Battles;

/// <summary>What our battle screen makes of one of the battle's info boxes (<see cref="BattleInfoBox"/>).</summary>
public enum InfoBoxKind
{
    /// <summary>Our narration panel in the scene's middle, the box's sentences in it (mock-up K1): every box not below.</summary>
    Panel,

    /// <summary>A unit's defence (<c>FIGT.ADV:1399</c>): an "ENEMY HIT −n%" ribbon flashing on its card, the wait answered at once.</summary>
    Defence,

    /// <summary>A unit or a commander down (<c>FIGTﾐｶDE.ADV</c>, <c>FIGTﾃｷDE.ADV</c>): a stamp flashing across its dimmed card, the wait answered at once (owner, 2026-10-21).</summary>
    Fall,

    /// <summary>A box our screen shows elsewhere: the battle's end reason (on the results plate) and a level-up's first page.</summary>
    Passed,

    /// <summary>The end's tally and each level-up's second page: the results plate (mock-ups K3c, K3d), the player's click.</summary>
    Results,
}

/// <summary>
/// One of the battle's own info boxes (<c>WAO_SUB</c> 143/153, frame 894) at its key wait as our battle screen shows it
/// (docs/ui/messages.md, §5 step 5; the owner's choice K3 with K1, 2026-10-18/19): the events on the cards — a defence as
/// a badge, a fall as a stamp —, the end as one results plate, every other box as our narration panel. Its kind comes from
/// the script locations of the text printed into it; the waits stay the script's.
/// </summary>
/// <param name="Kind">How the box shows.</param>
/// <param name="Sentences">The page's text: the box's narrow line breaks flowed into sentences, one per paragraph (an empty line parts them).</param>
/// <param name="Place">The card a <see cref="InfoBoxKind.Defence"/> or a <see cref="InfoBoxKind.Fall"/> lands on.</param>
/// <param name="Mark">A defence's ribbon's text ("ENEMY HIT −7%", <see cref="BattlePresenter.DefenceMark"/>) or the stamp's ("FELL IN BATTLE").</param>
/// <param name="Results">The results plate as it stands now.</param>
public sealed record BattleInfoBox(InfoBoxKind Kind, IReadOnlyList<string> Sentences, BattlePlace? Place = null, string Mark = "",
    BattleResults? Results = null)
{
    /// <summary>Whether its key wait is answered at once, as by a click: a defence (its ribbon flashes), a fall (its stamp flashes) and a passed box.</summary>
    public bool AnsweredAtOnce => Kind is InfoBoxKind.Defence or InfoBoxKind.Fall or InfoBoxKind.Passed;

    /// <summary>
    /// A page's lines as sentences: the lines trimmed and flowed into one paragraph (<see cref="BattleSpeech.Flow"/>) up to an
    /// empty line, which starts the next.
    /// </summary>
    public static IReadOnlyList<string> SentencesOf(IEnumerable<string> lines)
    {
        var sentences = new List<string>();
        var paragraph = new List<string>();
        foreach (var line in lines.Append(""))
        {
            if (line.Trim().Length > 0)
            {
                paragraph.Add(line);
                continue;
            }
            if (paragraph.Count > 0)
                sentences.Add(BattleSpeech.Flow(string.Join('\n', paragraph)));
            paragraph.Clear();
        }
        return sentences;
    }
}

/// <summary>
/// The battle's end as one plate (mock-ups K3c and K3d; owner, 2026-10-19): the reason as its title and sentence, each
/// side's damages, the EXP, then a row for each unit that levelled up, added as the script reaches it.
/// </summary>
/// <param name="Title">The outcome: "Night Falls", "Victory", "Defeat"….</param>
/// <param name="Reason">The reason's sentence ("The battle dragged on and it became night."), "" when the script gave none (a duel).</param>
/// <param name="PlayerArmy">The player's army as the tally names it ("Leazas", "Thieves").</param>
/// <param name="PlayerDamages">The player's losses (<c>Bt_PlayerLosses</c>).</param>
/// <param name="EnemyArmy">The enemy's army as the tally names it (string 10).</param>
/// <param name="EnemyDamages">The enemy's losses (<c>Bt_EnemyLosses</c>).</param>
/// <param name="Exp">The EXP each unit gained (<c>Tmp0001</c>).</param>
/// <param name="LevelUps">The level-ups so far.</param>
public sealed record BattleResults(string Title, string Reason, string PlayerArmy, int PlayerDamages, string EnemyArmy, int EnemyDamages,
    int Exp, IReadOnlyList<BattleLevelUp> LevelUps);

/// <summary>A unit's level-up (<c>FIGT.ADV:3045-3160</c>): its name and the second page's gains ("Leader Attacks +1", "Def on Defense UP").</summary>
public sealed record BattleLevelUp(string Unit, IReadOnlyList<string> Gains);
