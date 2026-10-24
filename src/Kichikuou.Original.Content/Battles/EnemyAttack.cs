using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.State;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Battles;

/// <summary>A ninja <c>FIGT_B</c>'s scout menu can send: its code, name and portrait, free this week or spent.</summary>
public sealed record AttackNinja(int Code, string Name, int Portrait, bool Free);

/// <summary>
/// An enemy attack on a province of ours as our screen shows it (docs/ui/enemy-attack.md, §6): what <c>FIGT_B.ADV</c>'s
/// announcement says (<c>:5-45</c>) and what GameState holds that the player decides on — the province's battles, the
/// ninjas, the scout report once one is made. Taken from GameState when the announcement prints (<c>:20</c>).
/// </summary>
/// <param name="Army">The attacking army, string 10.</param>
/// <param name="Province">The attacked province's name (<c>Prov_Cur</c>, string 8 the announcement prints).</param>
/// <param name="Record">The attacked province, <c>Prov_Cur</c>.</param>
/// <param name="FlagCg">The attacker's flag, <c>Ev_PhaseNation + 819</c>, as <c>FIGT_B.ADV:773</c> marks the map with it.</param>
/// <param name="PictureCg">The province's picture, <c>Prov_BgCg</c> (op 331).</param>
/// <param name="Walls">The percentage points the walls take from the attacker's hit chance in a city battle (<c>Prov_Defense * 2</c>).</param>
/// <param name="CityLimit">The city battle's limit, <c>Prov_BattleArea</c> (<c>FIGT_B.ADV:182</c>).</param>
/// <param name="Revenue">The province's share of the week's revenue in K GOLD (<c>Prov_Economy</c>, hundreds).</param>
/// <param name="Date">"LP 3, Month 5, Week 2" from <c>Week</c>.</param>
/// <param name="Ninjas">The ninjas in service (a weekly flag other than 99), in the menu's order.</param>
public sealed record EnemyAttack(string Army, string Province, int Record, int FlagCg, int PictureCg, int Walls,
    int CityLimit, int Revenue, string Date, IReadOnlyList<AttackNinja> Ninjas)
{
    /// <summary>The plains battle's limit (<c>FIGT_B.ADV:166</c>), where no walls count.</summary>
    public const int PlainsLimit = 5000;

    /// <summary>The adviser who speaks the announcement, Maris (CG 6, <c>FIGT_B.ADV:33</c>).</summary>
    public const int AdvisorCg = ScoutReport.AdvisorCg;

    /// <summary>Maris's line as her window prints it (<c>:35-45</c>), one entry per printed line; empty until it prints.</summary>
    public IReadOnlyList<string> AdvisorLines { get; init; } = [];

    /// <summary>The ninja's report once one is made (<c>:587-648</c>); our screen fills the army's places from it.</summary>
    public ScoutReport? Report { get; init; }

    /// <summary>Whether any ninja can be sent: one is in service and free.</summary>
    public bool CanScout => Ninjas.Any(n => n.Free);

    /// <summary>What Maris says at the foot: her comment on the report past 5000 troops (<c>:637</c>), else her line.</summary>
    public IReadOnlyList<string> AdvisorSays =>
        Report is { Commented: true } report && report.Comment.Length > 0 ? report.Comment.Split('\n') : AdvisorLines;

    /// <summary>The attack as GameState holds it now; <paramref name="ninjaName"/> reads a ninja's name from the menu's line.</summary>
    public static EnemyAttack Of(GameState s, Func<ScriptRef, string> ninjaName)
    {
        var prov = s.Provs[s.Prov_Cur];
        var ninjas = new List<AttackNinja>();
        foreach (var (code, line, portrait) in Battle.ScoutNinjas)
        {
            int flag = NinjaFlag(s, code).Value;
            if (flag != 99)
                ninjas.Add(new AttackNinja(code, ninjaName(new ScriptRef("FIGT_B.ADV", line)).Trim(), portrait, flag == 0));
        }
        return new EnemyAttack(DeploymentTable.StringAt(s, 10).Trim(), ProvinceName(s), s.Prov_Cur.Value,
            (s.Ev_PhaseNation + 819).Value, prov.BgCg.Value, (prov.Defense * 2).Value, prov.BattleArea.Value,
            prov.Economy.Value * 100, DateOf(s.Week.Value), ninjas);
    }

    /// <summary>
    /// "LP y, Month m, Week w" as the week's banner prints it (<c>MAIN.ADV</c>'s <c>L_01881</c>): 48 weeks a year from LP 3,
    /// four a month.
    /// </summary>
    public static string DateOf(int week)
    {
        int w = Math.Max(0, week - 1);
        return $"LP {3 + w / 48}, Month {w % 48 / 4 + 1}, Week {w % 4 + 1}";
    }

    /// <inheritdoc/>
    public bool Equals(EnemyAttack? other) =>
        other is not null && (Army, Province, Record, FlagCg, PictureCg, Walls, CityLimit, Revenue, Date)
            == (other.Army, other.Province, other.Record, other.FlagCg, other.PictureCg, other.Walls, other.CityLimit,
                other.Revenue, other.Date)
        && Ninjas.SequenceEqual(other.Ninjas) && AdvisorLines.SequenceEqual(other.AdvisorLines) && Equals(Report, other.Report);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Army, Province, Record, Date, Ninjas.Count);

    /// <summary>The weekly flag of ninja <paramref name="code"/>: 0 free, 1 spent, 99 not in service.</summary>
    public static Sys3Int NinjaFlag(GameState s, int code) => code switch
    {
        1 => s.F_Kanami_NinjaUsed,
        2 => s.F_Freya_NinjaUsed,
        3 => s.F_Kaoru_NinjaUsed,
        4 => s.F_Wichita_NinjaUsed,
        5 => s.F_Saitou_NinjaUsed,
        _ => throw new ArgumentOutOfRangeException(nameof(code)),
    };

    /// <summary>The province's name, its <c>MS 8</c> string (<c>Prov_StrBase + Prov_Cur - 1</c>, which <c>:15</c> copies into string 8).</summary>
    private static string ProvinceName(GameState s) =>
        DeploymentTable.StringAt(s, (s.Prov_StrBase + s.Prov_Cur - 1).Value).Trim();
}
