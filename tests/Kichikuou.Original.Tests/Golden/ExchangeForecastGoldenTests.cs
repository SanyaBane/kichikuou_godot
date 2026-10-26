using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Golden;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Tests.Golden;

/// <summary>
/// The damage forecast (<see cref="ExchangeForecast"/>; docs/ui/damage-forecast.md, §3) against the original's own
/// exchanges: at every attack of every capture the forecast is made on the replay's state while <c>FIGT_SEL</c> asks,
/// and the exchange that follows must lie inside its whole range; the replay still matches the original (the game's
/// state and generator untouched).
/// </summary>
public class ExchangeForecastGoldenTests(ITestOutputHelper output)
{
    /// <summary>The gallery's battle (docs/ui/gallery.md): an attack on Oaks (6) of story stage 1.</summary>
    private const string GalleryCapture = "20261010-213758-114-FIGT_A.golden";

    /// <summary>
    /// The first <c>FIGT_SEL</c> of the gallery's battle, Rance (unit 1) acting: Melee on Ex (commander 1, 92 troops, HP 1)
    /// takes 29-37 troops and 0-1 HP, on Sakanaku (commander 6, 60 troops, HP 4) all 60 troops and 1-4 HP — the probe's
    /// figures (docs/ui/damage-forecast.md, §3, §10).
    /// </summary>
    [GoldenFact(GalleryCapture)]
    public async Task The_gallery_battles_first_turn_reads_as_the_probe()
    {
        var capture = GoldenCapture.Load(Path.Combine(GoldenData.Directory, GalleryCapture));
        GameState? state = null;
        Dictionary<int, ExchangeForecast>? first = null;
        await GoldenReplay.RunAsync(capture, started: s => state = s, commands: (menu, _) =>
        {
            if (first is not null)
                return;
            Assert.Equal(1, state!.Unit_Cur.Value);
            first = [];
            foreach (var slot in Enumerable.Range(1, 4))
            {
                int record = EnemySlot(state, slot);
                if (record != 0 && menu.Allows(new FigtSelChoice(FigtSelCommand.Direct, (Sys3Int)(slot + 3))))
                    first[record] = ExchangeForecast.Of(state, new FigtSelChoice(FigtSelCommand.Direct, (Sys3Int)(slot + 3)));
            }
        });

        Assert.NotNull(first);
        var ex = first[1];
        var sakanaku = first[6];
        Assert.Equal((29, 37), (ex.Troops!.Value.Low, ex.Troops.Value.High));
        Assert.Equal((0, 1), (ex.Hp!.Value.Low, ex.Hp.Value.High));
        Assert.Equal((60, 60), (sakanaku.Troops!.Value.Low, sakanaku.Troops.Value.High));
        Assert.Equal((1, 4), (sakanaku.Hp!.Value.Low, sakanaku.Hp.Value.High));
    }

    /// <summary>
    /// Every attack of every capture: what the exchange took from the target lies in its forecast's whole range, and a
    /// loss the forecast says the attack cannot reach is none. The runs' range is no bound of the rules (a binomial
    /// tail may lie past it), so a few exceptions may stand; they are listed. What the forecast does not see
    /// (docs/ui/damage-forecast.md, §4: Miki's Vanish Bomb, the Sieg event) is counted apart.
    /// </summary>
    [GoldenFact]
    public async Task Every_captured_exchange_lies_in_its_forecast()
    {
        int exchanges = 0, unseen = 0;
        var clock = new System.Diagnostics.Stopwatch();
        var exceptions = new List<string>();
        foreach (var path in GoldenData.Replayable)
        {
            var capture = GoldenCapture.Load(path);
            GameState? state = null;
            Pending? pending = null;
            var view = new ShotsView(() =>
            {
                if (pending is not { } p)
                    return;
                pending = null;
                var s = state!;
                if (s.Unit_Cur != p.Unit || s.Cmdr_Cur != p.Cmdr || s.Units[s.Unit_Cur].Item != p.Item || s.F_Sieg_State != p.Sieg
                    || (p.Command <= 3 && s.Bt_SpecialAtk != 0))
                {
                    unseen++;
                    return;
                }
                exchanges++;
                var target = s.Cmdrs[s.Cmdr_Cur];
                int troops = Math.Min(target.Troops.Value, s.Bt_PTroopVsTroopDmg.Value + s.Bt_PCmdrVsTroopDmg.Value);
                int hp = Math.Min(target.HP.Value, s.Bt_PTroopVsCmdrDmg.Value + s.Bt_PCmdrVsCmdrDmg.Value);
                string where = $"{GoldenData.Relative(path)}, unit {p.Unit} on commander {p.Cmdr} ({p.Command})";
                Check(where, "troops", troops, p.Forecast.Troops, exceptions);
                Check(where, "HP", hp, p.Forecast.Hp, exceptions);
            });
            var report = await GoldenReplay.RunAsync(capture, view: view, started: s => state = s, commands: (_, choice) =>
            {
                if (!ExchangeForecast.IsAttack(choice.Command))
                    return;
                var s = state!;
                int slot = choice.Argument.Value - 3;
                clock.Start();
                var forecast = ExchangeForecast.Of(s, choice);
                clock.Stop();
                pending = new Pending(forecast, choice.Command.Value, s.Unit_Cur, (Sys3Int)EnemySlot(s, slot),
                    s.Units[s.Unit_Cur].Item, s.F_Sieg_State);
            });
            Assert.True(report.Passed, $"{GoldenData.Relative(path)}: {report.Describe()}");
        }

        output.WriteLine($"{exchanges} exchanges forecast ({clock.Elapsed.TotalMilliseconds / Math.Max(1, exchanges + unseen):F1} ms a forecast), {unseen} not forecast (§4), {exceptions.Count} outside the range:");
        foreach (var e in exceptions)
            output.WriteLine("  " + e);
        Assert.True(exchanges > 0, "No attack in the captures.");
        Assert.True(exceptions.Count <= Math.Max(2, exchanges / 200), string.Join("\n", exceptions));
    }

    private static void Check(string where, string what, int lost, DamageSpread? forecast, List<string> exceptions)
    {
        if (forecast is { } f ? !f.Holds(lost) : lost != 0)
            exceptions.Add($"{where}: {what} lost {lost}, forecast {(forecast is { } g ? $"{g.Min}-{g.Max}" : "out of reach")}");
    }

    private static int EnemySlot(GameState s, int slot) => slot switch
    {
        1 => s.Bt_EnemySlot1.Value,
        2 => s.Bt_EnemySlot2.Value,
        3 => s.Bt_EnemySlot3.Value,
        4 => s.Bt_EnemySlot4.Value,
        _ => 0,
    };

    /// <summary>A forecast made at <c>FIGT_SEL</c>, waiting for its exchange, with the inputs it assumed.</summary>
    private readonly record struct Pending(ExchangeForecast Forecast, int Command, Sys3Int Unit, Sys3Int Cmdr, Sys3Int Item, Sys3Int Sieg);

    /// <summary>The replay's view: each frame of an exchange's shot counters (<c>FIGTATTK</c>: computed, not applied yet).</summary>
    private sealed class ShotsView(Action shots) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            if (scriptEvent is ShotsFrameEvent)
                shots();
            return Task.CompletedTask;
        }
    }
}

/// <summary>A fact over the golden captures (one of them, when named); skipped when they are not there.</summary>
public sealed class GoldenFactAttribute : FactAttribute
{
    public GoldenFactAttribute(string? capture = null)
    {
        if (capture is null ? !GoldenData.Replayable.Any() : !File.Exists(Path.Combine(GoldenData.Directory, capture)))
            Skip = $"No {(capture ?? "replayable captures")} in {GoldenData.Directory} (play with tools/golden/capture.ps1).";
    }
}
