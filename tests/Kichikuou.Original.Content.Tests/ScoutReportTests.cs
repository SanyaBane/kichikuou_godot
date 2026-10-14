using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The ninja's scout report as our own overlay shows it (docs/ui/battle-screen.md, "The scout report").</summary>
public class ScoutReportTests(ITestOutputHelper output)
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    /// <summary>
    /// The week-18 rebels: Sakanaku (commander 6) in the party's first place, Dodge (commander 5) without troops in
    /// the third, Jibul (commander 7, no face: his sprite) in the fourth; the second place empty.
    /// </summary>
    private static GameState Rebels()
    {
        var s = new GameState { Cmdr_StrBase = 200 };
        s.Strings[10] = " Leazas Rebel Army ";
        void Cmdr(int n, string name, string type, int troops, int face, int alone = 0)
        {
            s.Strings[200 + (n - 1) * 3] = name;
            s.Strings[201 + (n - 1) * 3] = type;
            var c = s.Cmdrs[n];
            (c.Troops, c.MaxTroops, c.HP, c.FaceCg, c.CgAlone) = ((Sys3Int)troops, (Sys3Int)1000, (Sys3Int)6, (Sys3Int)face, (Sys3Int)alone);
        }
        Cmdr(6, "Sakanaku", "Leazas Soldiers", 970, 1106);
        Cmdr(5, "Dodge", "Leazas Soldiers", 0, 1105);
        Cmdr(7, "Jibul", "Leazas Soldiers", 400, 0, alone: 1207);
        (s.Bt_EnemyParty1, s.Bt_EnemyParty3, s.Bt_EnemyParty4) = (6, 5, 7);
        return s;
    }

    [Fact]
    public void A_report_lists_the_partys_commanders_in_their_places_with_only_what_the_ninja_tells()
    {
        var report = ScoutReport.Of(Rebels(), ninjaCg: 9);

        Assert.Equal((9, "Leazas Rebel Army"), (report.NinjaCg, report.Army));
        Assert.Equal(4, report.Places.Count);
        Assert.Equal(new ScoutedCommander(1, 6, "Sakanaku", "Leazas Soldiers", 1106, 970), report.Places[0]);
        Assert.Null(report.Places[1]);
        Assert.Equal(new ScoutedCommander(3, 5, "Dodge", "", 1105, 0), report.Places[2]);     // no troops: no troop type
        Assert.Equal(new ScoutedCommander(4, 7, "Jibul", "Leazas Soldiers", 1207, 400), report.Places[3]);
        Assert.Equal((3, 1370), (report.Commanders, report.Troops));                            // Tmp0002, Tmp0001
    }

    [Fact]
    public void The_totals_saturate_as_the_scripts_sum()
    {
        var s = Rebels();
        s.Cmdrs[6].Troops = 65000;
        s.Cmdrs[7].Troops = 65000;
        Assert.Equal(65535, ScoutReport.Of(s, 9).Troops);
    }

    /// <summary>
    /// The presenter holds the report from "The Approaching Enemy" (<c>FIGT_B.ADV:587</c>) to the window's close; it is
    /// reported (its key waits answered at once on our screen) from the first commander's line on — the first key
    /// wait, after line 587, stays the player's.
    /// </summary>
    [Fact]
    public void The_presenter_holds_the_report_while_window_113_lists_it()
    {
        var s = Rebels();
        var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
        presenter.Attach(s);
        presenter.Show(new WindowEvent((Sys3Int)321, (Sys3Int)9));
        presenter.Show(new WindowEvent((Sys3Int)113, Sys3Int.Zero));
        presenter.Show(new MessageEvent(new ScriptRef("FIGT_B.ADV", 557)));
        Assert.Null(presenter.Scout);

        presenter.Show(new MessageEvent(new ScriptRef("FIGT_B.ADV", 587)));
        Assert.Equal(ScoutReport.Of(s, 9), presenter.Scout);
        Assert.False(presenter.ScoutReported);

        presenter.Show(new ScoutReportEvent((Sys3Int)6, (Sys3Int)970));
        Assert.True(presenter.ScoutReported);
        presenter.Show(new ValueMessageEvent(new ScriptRef("FIGT_B.ADV", 613), [(Sys3Int)3, (Sys3Int)1370]));
        Assert.True(presenter.ScoutReported);

        presenter.Show(new WindowEvent(Sys3Int.Zero, Sys3Int.Zero));
        Assert.Null(presenter.Scout);
        Assert.False(presenter.ScoutReported);
    }

    /// <summary>
    /// In every captured battle with a scout report, the report our overlay shows is what the script prints: its
    /// commanders' lines (record, name, troop type, troops) in order, its totals (<c>Tmp0002</c>, <c>Tmp0001</c>), and
    /// the places the battle then gives them (<c>Bt_EnemySlot1-4</c>).
    /// </summary>
    [CaptureTheory]
    [MemberData(nameof(Defences))]
    public async Task A_report_is_what_the_script_prints_and_the_battle_places(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
        GameState? state = null;
        ScoutReport? report = null, placed = null;
        var lines = new List<ScoutedCommander>();
        int reports = 0;
        var wrong = new List<string>();
        var view = new ObservingView(e =>
        {
            presenter.Show(e);
            if (state is not { } s)
                return;
            if (presenter.Scout is { } shown && report is null)
            {
                report = shown;
                reports++;
                lines.Clear();
            }
            switch (e)
            {
                case ScoutReportEvent line when report is not null:
                    int record = line.Cmdr.Value;
                    lines.Add(new ScoutedCommander(0, record, s.Strings[4].Trim(), line.Troops == 0 ? "" : s.Strings[5].Trim(),
                        0, line.Troops.Value));
                    break;
                case ValueMessageEvent { Text: { File: "FIGT_B.ADV", Line: 613 } } totals when report is not null:
                    var listed = report.Places.OfType<ScoutedCommander>().Select(c => c with { Slot = 0, Picture = 0 });
                    if (!listed.SequenceEqual(lines))
                        wrong.Add($"report {reports}: shown {string.Join(", ", listed)}; printed {string.Join(", ", lines)}");
                    if ((report.Commanders, report.Troops) != (totals.Values[0].Value, totals.Values[1].Value)
                        || (report.Commanders, report.Troops) != (s.Tmp0002.Value, s.Tmp0001.Value))
                        wrong.Add($"report {reports}: totals {report.Commanders}/{report.Troops}, printed {totals.Values[0]}/{totals.Values[1]}");
                    if (!presenter.ScoutReported)
                        wrong.Add($"report {reports}: not reported at its totals");
                    placed = report;
                    break;
                case SlotPanelEvent when placed is not null:
                    // The battle's first field: the commanders stand where the report put them.
                    for (int slot = 1; slot <= 4; slot++)
                    {
                        int there = BattleBoard.Record(s, new BattlePlace(BattleSide.Enemy, slot));
                        if (there != (placed.Places[slot - 1]?.Record ?? 0))
                            wrong.Add($"report {reports}: place {slot} holds {there}");
                    }
                    placed = null;
                    break;
            }
            if (report is not null && presenter.Scout is null)
                report = null;                                          // the window closed
        });
        await GoldenReplay.RunAsync(capture, view: view, started: s =>
        {
            state = s;
            presenter.Start(s);
        });
        output.WriteLine($"{file}: {reports} scout reports");
        Assert.Empty(wrong);
    }

    /// <summary>The captures of a defence (<c>FIGT_B</c>, the only file with a scout report): every eighth in the quick run.</summary>
    public static IEnumerable<object[]> Defences =>
        ScenarioTextGoldenTests.Captures.Where(c => ((string)c[0]).EndsWith("-FIGT_B.golden", StringComparison.Ordinal))
            .Where((_, i) => DifferentialRuns.Full || i % DifferentialRuns.QuickShare == 0);

    private sealed class ObservingView(Action<ScriptEvent> show) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            show(scriptEvent);
            return Task.CompletedTask;
        }
    }
}
