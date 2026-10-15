using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>
/// The battle's opening on our own battle screen (docs/ui/battle-screen.md, "The opening"; owner, 2026-10-14,
/// variant C after Sengoku Rance's): what the first round header (<c>FIGT.ADV:112-216</c>) tells, shown once a
/// battle — the later rounds' headers differ only in the rounds left, which the rounds gauge shows. The texts
/// follow the header's lines and conditions: the battle's kind and place, its troop limit, the rounds left (not
/// in a duel), the two armies and their advantages.
/// </summary>
/// <param name="Title">"Oaks — Offensive Battle", "Duel", "Dungeon Battle" …</param>
/// <param name="Limit">"City · Battle Limit: up to 232" and the like (<see cref="LimitText(int, bool, int)"/>).</param>
/// <param name="Turns">The rounds left (null in a duel, whose header leaves the line out).</param>
/// <param name="PlayerArmy">String 7 (" Rance Army").</param>
/// <param name="EnemyArmy">String 10, the enemy army's name.</param>
/// <param name="PlayerAdvantage">The player's advantage (the header's <c>Tmp0001</c>).</param>
/// <param name="EnemyAdvantage">The enemy's (<c>Tmp0002</c>).</param>
/// <param name="PlayerEffects">The player's advantage as what it does (<see cref="Effects"/>), which our screen shows.</param>
/// <param name="EnemyEffects">The enemy's.</param>
public sealed record BattleOpening(string Title, string Limit, int? Turns, string PlayerArmy, string EnemyArmy,
    int PlayerAdvantage, int EnemyAdvantage, BattleAdvantage PlayerEffects, BattleAdvantage EnemyEffects)
{
    /// <summary>The opening of the header <paramref name="header"/> shows, with the strings GameState holds then.</summary>
    public static BattleOpening Of(RoundHeaderEvent header, GameState s)
    {
        bool duel = header.IsDuel == 1;
        int kind = header.Kind.Value, limit = header.TroopLimit.Value;
        string province = DeploymentTable.StringAt(s, 8).Trim();
        // :116-142 — the first line.
        string title = duel ? "Duel" : kind switch
        {
            1 => $"{province} — Offensive Battle",
            2 => $"{province} — Defensive Battle",
            3 => "Dungeon Battle",
            _ => "",
        };
        var (player, enemy) = BattleBoard.Armies(s);
        var (playerEffects, enemyEffects) = Effects(s);
        return new BattleOpening(title, LimitText(kind, duel, limit), duel ? null : header.RoundsLeft.Value,
            player, enemy, header.PlayerAdvantage.Value, header.EnemyAdvantage.Value, playerEffects, enemyEffects);
    }

    /// <summary>
    /// The header's second and third lines (<c>FIGT.ADV:145-164</c>) as one, in the owner's form (2026-10-15): "City · Battle
    /// Limit: up to 232" — the most troops of one unit that fight in an exchange (docs/battle/02-resolution.md, <c>:624</c>).
    /// A dungeon at the plains' limit reads "Plains", as the script's conditions have it.
    /// </summary>
    public static string LimitText(int kind, bool duel, int limit)
    {
        string field = limit == 5000 && !duel ? "Plains"
            : duel ? "Duel"
            : kind != 3 ? "City"
            : "Dungeon";
        return $"{field} · Battle Limit: up to {limit}";
    }

    /// <summary>The limit of the battle GameState holds (<see cref="LimitText(int, bool, int)"/>), which our screen shows at the scene's foot.</summary>
    public static string LimitText(GameState s) => LimitText(s.Bt_Kind.Value, s.Bt_IsDuel == 1, s.Bt_TroopLimit.Value);

    /// <summary>
    /// The two sides' advantages as the round header sums them (<c>FIGT.ADV:179-198</c>, <c>RoundHeaderAsync</c>):
    /// the walls' bonus for the side that holds the city (the enemy in an offensive battle and in a dungeon, the
    /// player in a defence), +10 / +50 for a successful strategy. Our screen shows them over the cards all battle long.
    /// </summary>
    public static (int Player, int Enemy) Advantages(GameState s)
    {
        Sys3Int player = 0, enemy = 0;
        if (s.Bt_Kind == 1 || s.Bt_Kind == 3)
            enemy += s.Bt_WallBonus;
        if (s.Bt_Kind == 2)
            player += s.Bt_WallBonus;
        if (s.Bt_StrategyResult == 1)
            player += 10;
        if (s.Bt_StrategyResult == 2)
            player += 50;
        if (s.Bt_StrategyResult == 11)
            enemy += 10;
        if (s.Bt_StrategyResult == 12)
            enemy += 50;
        return (player.Value, enemy.Value);
    }

    /// <summary>
    /// The two sides' advantages as what they do (owner, 2026-10-15: the header's sum, <see cref="Advantages"/>, adds up
    /// two different things): the walls count for the side that holds the city in an offensive or a defensive battle, where
    /// the resolution takes <c>Bt_WallBonus * 2</c> from the other side's troops' hit chance against its troops
    /// (<c>FIGT.ADV:2548</c>, <c>:2741</c>; the header also adds them to the enemy's in a dungeon, where nothing applies
    /// them); a successful strategy raises its side's troop damage and lowers the other's by a tenth or a half
    /// (<c>:2294-2317</c>).
    /// </summary>
    public static (BattleAdvantage Player, BattleAdvantage Enemy) Effects(GameState s)
    {
        int walls = (s.Bt_WallBonus * 2).Value;
        int strategy = s.Bt_StrategyResult.Value;
        return (new BattleAdvantage(s.Bt_Kind == 2 ? walls : 0, strategy switch { 1 => 10, 2 => 50, _ => 0 }),
            new BattleAdvantage(s.Bt_Kind == 1 ? walls : 0, strategy switch { 11 => 10, 12 => 50, _ => 0 }));
    }

    /// <summary>
    /// An army's name in at most <paramref name="maxLines"/> lines no wider than <paramref name="room"/> (by
    /// <paramref name="measure"/>): the fewest lines that fit, and among those the split whose widest line is the
    /// narrowest; words are never broken. Null when no split fits (the caller then tries a smaller size).
    /// </summary>
    public static IReadOnlyList<string>? Wrap(string name, int maxLines, Func<string, float> measure, float room)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return [];
        for (int lines = 1; lines <= Math.Min(maxLines, words.Length); lines++)
        {
            IReadOnlyList<string>? best = null;
            float widest = float.MaxValue;
            foreach (var cuts in Cuts(words.Length, lines - 1, 1))
            {
                var split = Split(words, cuts);
                float w = split.Max(measure);
                if (w <= room && w < widest)
                    (best, widest) = (split, w);
            }
            if (best is not null)
                return best;
        }
        return null;
    }

    /// <summary>Every way to cut <paramref name="count"/> words in <paramref name="cuts"/> places (each cut before a word index ≥ <paramref name="from"/>).</summary>
    private static IEnumerable<int[]> Cuts(int count, int cuts, int from)
    {
        if (cuts == 0)
        {
            yield return [];
            yield break;
        }
        for (int at = from; at <= count - cuts; at++)
            foreach (var rest in Cuts(count, cuts - 1, at + 1))
                yield return [at, .. rest];
    }

    private static string[] Split(string[] words, int[] cuts)
    {
        var bounds = new[] { 0 }.Concat(cuts).Append(words.Length).ToArray();
        return [.. Enumerable.Range(0, bounds.Length - 1).Select(i => string.Join(' ', words[bounds[i]..bounds[i + 1]]))];
    }
}

/// <summary>
/// A side's advantage as what it does (<see cref="BattleOpening.Effects"/>): <paramref name="Walls"/> percentage points
/// off the other side's troops' hit chance against its troops, <paramref name="Strategy"/> percent more troop damage
/// dealt (and as much less taken).
/// </summary>
public sealed record BattleAdvantage(int Walls, int Strategy)
{
    /// <summary>No advantage.</summary>
    public static readonly BattleAdvantage None = new(0, 0);

    /// <summary>"Walls +8%", "Strategy +10%", both "Walls +8%  ·  Strategy +10%"; "" without either.</summary>
    public string Text => string.Join("  ·  ", Parts());

    private IEnumerable<string> Parts()
    {
        if (Walls != 0)
            yield return $"Walls {Walls:+0;-0}%";
        if (Strategy != 0)
            yield return $"Strategy +{Strategy}%";
    }
}
