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
/// (commander 6), Dodge (commander 5) and Jibul (commander 7) (CLAUDE.md, "Commands", <c>--scout</c>).
/// </summary>
internal static partial class GalleryEntries
{
    private const string Battle = "Battle";
    private const int Ys = 5, Rebels = 2;
    private static readonly int[] RebelParty = [1, 6, 5, 7];

    private const string TroopsNote = "The rebels on Ys (5) as the --scout run meets them; their troops are the COMMAND save's, "
        + "before the rebels' turn, so Ex (commander 1) has none.";

    private static IEnumerable<GalleryEntry> BattleScreens()
    {
        yield return new("enemy-attack", Battle, "Enemy attack", Week18, TroopsNote,
            g => Attack(g, ninjas: false, scouted: false));
        yield return new("enemy-attack-ninjas", Battle, "Enemy attack: the ninjas' list", Week18, TroopsNote,
            g => Attack(g, ninjas: true, scouted: false));
        yield return new("enemy-attack-scouted", Battle, "Enemy attack: the places scouted", Week18,
            "The first free ninja's report filled in. " + TroopsNote, g => Attack(g, ninjas: false, scouted: true));
        yield return new("scout-report", Battle, "Scout report", Week18, "The first free ninja's report. " + TroopsNote, g =>
        {
            if (AttackState(g) is not var (s, attack))
                return null;
            var view = g.Add(new ScoutReportView(g.Font, g.Faces) { Visible = true });
            view.LoadCrops();
            view.Show(Report(s, attack));
            return area => view.Place(area.Position, area.Size.X / 1280f);
        });
    }

    /// <summary>The save's state with the rebels' attack on Ys (5) announced, and the attack as our screen takes it.</summary>
    private static (GameState State, EnemyAttack Attack)? AttackState(GalleryStage g)
    {
        if (g.State(Week18) is not { } s)
            return null;
        GalleryStates.EnemyAttack(s, Ys, Rebels, GalleryStage.Literal(new ScriptRef("TCﾘｰｻﾞｽ.ADV", 3)));
        GalleryStates.EnemyParty(s, RebelParty);
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

    /// <summary>Our enemy attack screen over the map, its ninjas' list open or the scout report filled in.</summary>
    private static Action<Rect2>? Attack(GalleryStage g, bool ninjas, bool scouted)
    {
        if (AttackState(g) is not var (s, attack))
            return null;
        var backdrop = g.Backdrop(s);
        if (scouted)
            attack = attack with { Report = Report(s, attack) };
        var view = g.Add(new EnemyAttackView(g.Font, g.Faces));
        view.Show(attack);
        view.SetList(ninjas);
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            view.Place(area.Position, area.Size.X / 1280f);
        };
    }
}
