using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Kichikuou.TestSupport;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="WeekReport"/>: the week's start gathered from <see cref="WeekLoop"/>'s windows through the presenter
/// (docs/ui/turn-start.md, "The week report"), the loop run up to <c>COMMAND</c> with the player's files' texts.
/// Skipped without them.
/// </summary>
public class WeekReportTests
{
    /// <summary>
    /// Week 18: two provinces of ours worth 150 and 52 (20200K) and one not ours, the AL Church still paid,
    /// M Land not, Galtia (unit 41) absent; 102700K before. The banner's, the revenue's and the expenses' key waits
    /// all come before the window closes, which completes the report with 116400K.
    /// </summary>
    [Fact]
    public async Task A_Leazas_week_gathers_the_banner_the_revenue_and_the_expenses()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "MAIN.ADV")))
            return;
        var state = new GameState { Week = 17, StoryStage = 1, Gold = 1027, Prov_Count = 3, N_ALChurch_Stage = 0 };
        state.Provs[1].Owner = 1;
        state.Provs[1].Economy = 150;
        state.Provs[2].Owner = 1;
        state.Provs[2].Economy = 52;
        state.Provs[3].Owner = 2;
        state.Provs[3].Economy = 99;
        state.Units[1].Status = 1;
        state.Units[1].MaxTroops = 2000;
        state.Units[1].Troops = 1000;
        state.Units[1].Recovery = 40;

        var (presenter, report, waits) = await RunToCommandAsync(state);

        Assert.Empty(presenter.Unresolved);
        Assert.Equal(("Leazas Army Phase", false, "LP 3 · Month 5 · Week 2"), (report.Title, report.Thieves, report.Date));
        Assert.Equal("Revenue & Troop Replenishment", report.IncomeCaption);
        Assert.Equal(new ReportLine("Revenue", 20200), report.Revenue);
        Assert.Equal(new ReportLine("Replenished Troops", state.Tmp0024.Value), report.Troops);
        Assert.True(report.Troops!.Amount > 0);
        Assert.Equal("Special Expenses (Donations)", report.ExpensesCaption);
        Assert.Equal([new ReportLine("AL Church", -6500)], report.Expenses);
        Assert.Equal((102700, 116400), (report.BudgetBefore, report.BudgetAfter));
        Assert.Equal(3, waits);
    }

    /// <summary>All three expenses, and none: the caption only when one applies; the revenue's wait is then the last.</summary>
    [Fact]
    public async Task The_expenses_are_the_ones_printed()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "MAIN.ADV")))
            return;
        var all = new GameState { Week = 30, StoryStage = 1, Gold = 2000, Prov_Count = 1, N_ALChurch_Stage = 0, F_MLand_Funded = 1 };
        all.Units[41].Status = 1;
        var (_, report, _) = await RunToCommandAsync(all);
        Assert.Equal([new ReportLine("AL Church", -6500), new ReportLine("Galtia's Food", -4500), new ReportLine("M Land", -2000)],
            report.Expenses.OrderBy(e => e.Label, StringComparer.Ordinal));
        Assert.Equal(200000 - 13000, report.BudgetAfter);

        var none = new GameState { Week = 30, StoryStage = 1, Gold = 2000, Prov_Count = 1, N_ALChurch_Stage = 19 };
        var (_, quiet, waits) = await RunToCommandAsync(none);
        Assert.Null(quiet.ExpensesCaption);
        Assert.Empty(quiet.Expenses);
        Assert.Equal(2, waits);
    }

    /// <summary>The thieves' week: the banner alone, its one key wait, then the window closes.</summary>
    [Fact]
    public async Task A_thieves_week_has_the_banner_alone()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "MAIN.ADV")))
            return;
        var (_, report, waits) = await RunToCommandAsync(new GameState { Week = 0, StoryStage = 0, Gold = 3 });
        Assert.Equal(("Rance's Thieves Phase", true, "LP 3 · Month 1 · Week 1"), (report.Title, report.Thieves, report.Date));
        Assert.Null(report.Revenue);
        Assert.Empty(report.Expenses);
        Assert.Equal(1, waits);
    }

    /// <summary>
    /// Runs <see cref="WeekLoop"/> from the week's start to its call of <c>COMMAND</c> with the presenter as the view
    /// (covering the week report); every key wait must come while the report is still being gathered, and the
    /// report must be complete before <c>COMMAND</c>. Returns the report and the number of key waits.
    /// </summary>
    private static async Task<(GamePresenter Presenter, WeekReport Report, int Waits)> RunToCommandAsync(GameState state)
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(LocalData.ScenarioDirectory), new CgLibrary(missing), new BoxGlyphs());
        presenter.Bind(state);
        presenter.CoverWeekReport = true;
        WeekReport? complete = null;
        var view = new PresenterView(presenter, () => complete ??= presenter.WeekReport is { Complete: true } r ? r : null);
        var input = new KeyInput(presenter);
        var script = new OriginalScriptContext(state, new LoggingRandom(7, []), view, input, new StopAtCommand());
        await Assert.ThrowsAsync<StopException>(() => new WeekLoop(script).RunAsync());
        Assert.NotNull(complete);
        return (presenter, complete, input.Waits);
    }

    private sealed class StopException() : Exception("COMMAND reached.");

    /// <summary>The autosave does nothing; <c>COMMAND</c> ends the run.</summary>
    private sealed class StopAtCommand : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry) =>
            file == "COMMAND.ADV" ? throw new StopException() : Task.CompletedTask;
    }

    private sealed class PresenterView(GamePresenter presenter, Action shown) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            presenter.Show(scriptEvent);
            shown();
            return Task.CompletedTask;
        }
    }

    /// <summary>Key waits answered with 0, each while the report is still being gathered; pauses with 0.</summary>
    private sealed class KeyInput(GamePresenter presenter) : UnexpectedInput
    {
        public int Waits { get; private set; }

        public override Task<Sys3Int> WaitForKeyAsync()
        {
            Assert.True(presenter.WeekReport is { Complete: false }, "a key wait outside the report");
            Waits++;
            return Task.FromResult(Sys3Int.Zero);
        }

        public override Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => Task.FromResult(Sys3Int.Zero);
    }
}
