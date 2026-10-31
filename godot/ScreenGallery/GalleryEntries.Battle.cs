using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Battles;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.State;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// The battle's screens before the field (docs/ui/gallery.md, step 3): the enemy attack and the ninja's scout report, on the
/// attack the week-18 save meets when its week ends at once — the rebels on Ys (5) with Ex (commander 1), Sakanaku
/// (commander 6), Dodge (commander 5) and Jibul (commander 7) (CLAUDE.md, "Commands", <c>--scout</c>). Maris comments on a
/// report only past 5000 troops (<c>FIGT_B.ADV:636-647</c>), which no rebel army reaches, so her line is shown on the
/// Helman 3rd Army's attack on Micrograd (44) (<c>Tﾍﾙﾏﾝ.ADV:293-313</c>).
/// </summary>
internal static partial class GalleryEntries
{
    private const string Battle = "Battle";
    private const int Ys = 5, Rebels = 2, Micrograd = 44, Helman = 4;
    private static readonly int[] RebelParty = [1, 6, 5, 7];
    private static readonly int[] HelmanThirdParty = [42, 43, 39, 40];

    private const string TroopsNote = "The rebels on Ys (5) as the --scout run meets them; their troops are the COMMAND save's, "
        + "before the rebels' turn, so Ex (commander 1) has none.";

    private const string StrongNote = "The Helman 3rd Army on Micrograd (44) (Tﾍﾙﾏﾝ.ADV:293-313: Serges (commander 42), Booch "
        + "(commander 43), Mineva (commander 39) and Tami (commander 40)) with the COMMAND save's troops, past 5000, so Maris "
        + "comments (FIGT_B.ADV:637).";

    private static IEnumerable<GalleryEntry> BattleScreens()
    {
        yield return new("enemy-attack", Battle, "Enemy attack", Week18, TroopsNote,
            g => Attack(g, ninjas: false, scouted: false));
        yield return new("enemy-attack-abandon", Battle, "Enemy attack: Abandon City asks first", Week18, TroopsNote,
            g => Attack(g, ninjas: false, scouted: false, abandon: true));
        yield return new("enemy-attack-ninjas", Battle, "Enemy attack: the ninjas' list", Week18, TroopsNote,
            g => Attack(g, ninjas: true, scouted: false));
        yield return new("enemy-attack-scouted", Battle, "Enemy attack: the places scouted", Week18,
            "The first free ninja's report filled in. " + TroopsNote, g => Attack(g, ninjas: false, scouted: true));
        yield return new("scout-report", Battle, "Scout report", Week18, "The first free ninja's report. " + TroopsNote,
            g => Scout(g, strong: false));
        yield return new("scout-report-comment", Battle, "Scout report: Maris comments", Week18,
            "The first free ninja's report. " + StrongNote, g => Scout(g, strong: true));
        yield return new("strategy", Battle, "Strategy cut-in: ours", Week18,
            "Barres (unit 6) succeeds, his line FIGT作.ADV:80's; no field under it (the battle's is drawn by the strategy screen).",
            g => CutIn(g, BattleSide.Player, 6, result: 1, line: 80));
        yield return new("strategy-enemy", Battle, "Strategy cut-in: the enemy's, a great success", Week18,
            "Ex (commander 1) succeeds greatly, his line FIGT作.ADV:392's; no field under it.",
            g => CutIn(g, BattleSide.Enemy, 1, result: 12, line: 392));
    }

    /// <summary>
    /// Our cut-in of a successful strategy as the presenter has it once the strategist's bubble printed: the side's
    /// announcement (<c>FIGT作.ADV:37</c>, <c>:378</c>), the strategist's face, the result, the line at <paramref name="line"/>.
    /// </summary>
    private static Action<Rect2>? CutIn(GalleryStage g, BattleSide side, int record, int result, int line)
    {
        if (g.State(Week18) is not { } s)
            return null;
        bool player = side == BattleSide.Player;
        string Flowed(int at) => BattleSpeech.Flow(GalleryStage.Text(new ScriptRef("FIGT作.ADV", at), s));
        var strategy = new BattleStrategy(side, record, (player ? s.Units[record].FaceCg : s.Cmdrs[record].FaceCg).Value,
            Flowed(player ? 37 : 378), result, new BattleSpeech(side, false, Flowed(line)));
        var view = g.Add(new StrategyCutInView(g.Font, g.Faces) { Visible = true });
        view.Show(strategy, null, 0, 0, instant: true);
        return area => view.Place(area.Position, area.Size.X / 1280f);
    }

    /// <summary>Our scout report over the screen: the rebels' on Ys (5), or the Helman 3rd Army's, whose troops Maris comments on.</summary>
    private static Action<Rect2>? Scout(GalleryStage g, bool strong)
    {
        if (AttackState(g, strong) is not var (s, attack))
            return null;
        var view = g.Add(new ScoutReportView(g.Font, g.Faces) { Visible = true });
        view.LoadCrops();
        view.Show(Report(s, attack));
        return area => view.Place(area.Position, area.Size.X / 1280f);
    }

    /// <summary>
    /// The save's state with an attack announced, and the attack as our screen takes it: the rebels' on Ys (5), or with
    /// <paramref name="strong"/> the Helman 3rd Army's on Micrograd (44) as <c>Tﾍﾙﾏﾝ.ADV:293-313</c> picks it while every
    /// one of them has troops.
    /// </summary>
    private static (GameState State, EnemyAttack Attack)? AttackState(GalleryStage g, bool strong = false)
    {
        if (g.State(Week18) is not { } s)
            return null;
        if (strong)
        {
            GalleryStates.EnemyAttack(s, Micrograd, Helman, GalleryStage.Literal(new ScriptRef("Tﾍﾙﾏﾝ.ADV", 276)));
            GalleryStates.EnemyParty(s, HelmanThirdParty);
        }
        else
        {
            GalleryStates.EnemyAttack(s, Ys, Rebels, GalleryStage.Literal(new ScriptRef("TCﾘｰｻﾞｽ.ADV", 3)));
            GalleryStates.EnemyParty(s, RebelParty);
        }
        // Maris's pages (FIGT_B.ADV:35-43), as her window prints them.
        string[] lines = [.. GalleryStage.Text(new ScriptRef("FIGT_B.ADV", 35), s).Split('\n'), GalleryStage.Text(new ScriptRef("FIGT_B.ADV", 43), s)];
        var attack = EnemyAttack.Of(s, at => GalleryStage.Text(at, s)) with { AdvisorLines = [.. lines.Select(l => l.Trim()).Where(l => l.Length > 0)] };
        return (s, attack);
    }

    /// <summary>The first free ninja's report, Rance's answer and Maris's comment read as the presenter reads them.</summary>
    private static ScoutReport Report(GameState s, EnemyAttack attack)
    {
        var ninja = attack.Ninjas.FirstOrDefault(n => n.Free) ?? throw new InvalidOperationException("No ninja is free.");
        var report = ScoutReport.Of(s, ninja.Portrait);
        return report with
        {
            Reply = GalleryStage.Text(ScoutReport.RanceLine, s).Trim(),
            Comment = report.Commented ? GalleryStage.Text(report.CommentLine, s).Trim() : "",
        };
    }

    /// <summary>Our enemy attack screen over the map, its ninjas' list open, the scout report filled in or Abandon City asked.</summary>
    private static Action<Rect2>? Attack(GalleryStage g, bool ninjas, bool scouted, bool abandon = false)
    {
        if (AttackState(g) is not var (s, attack))
            return null;
        var backdrop = g.Backdrop(s);
        if (scouted)
            attack = attack with { Report = Report(s, attack) };
        var view = g.Add(new EnemyAttackView(g.Font, g.Faces));
        view.Show(attack);
        view.SetList(ninjas);
        if (abandon)
        {
            _ = view.ChooseAsync();
            view.Choose(3);                                     // the button's question, as its click asks it
        }
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            view.Place(area.Position, area.Size.X / 1280f);
        };
    }
}
