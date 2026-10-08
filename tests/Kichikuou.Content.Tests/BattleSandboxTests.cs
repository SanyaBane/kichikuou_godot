using Kichikuou.Content.Battles;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Golden;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>The battle sandbox on the local golden captures.</summary>
public class BattleSandboxTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Captures => ScenarioTextGoldenTests.Captures;

    private static GoldenCapture Load(string file) => GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));

    [CaptureTheory]
    [MemberData(nameof(Captures))]
    public void A_capture_is_summarised(string file)
    {
        var summary = BattleSandbox.Summarize(Load(file));

        output.WriteLine(summary.ToString());
        Assert.False(string.IsNullOrWhiteSpace(summary.Province));
        Assert.All(summary.PlayerUnits.Concat(summary.EnemyCommanders), n => Assert.False(string.IsNullOrWhiteSpace(n)));
        Assert.NotEmpty(summary.EnemyCommanders);
        Assert.True(summary.Outcome is 1 or 2 || summary.JumpTarget is not null);
    }

    [CaptureTheory]
    [MemberData(nameof(Captures))]
    public async Task A_live_battle_from_a_capture_runs_to_its_end(string file)
    {
        var capture = Load(file);
        var skipped = new List<string>();
        var input = new AutoPlayer();

        var (state, jump) = await BattleSandbox.RunLiveAsync(capture, new NullView(), input, seed: 20261008,
            onSceneSkipped: f => { skipped.Add(f); return Task.CompletedTask; });

        output.WriteLine($"{input.Inputs} inputs, result {state.Bt_Result}, jump {jump ?? "-"}, skipped [{string.Join(", ", skipped)}]");
        Assert.True(jump is not null || state.Bt_Result == 1 || state.Bt_Result == 2);
    }

    [CaptureTheory]
    [MemberData(nameof(Captures))]
    public async Task With_the_recorded_generator_and_choices_the_live_battle_is_the_original(string file)
    {
        var capture = Load(file);
        if (capture.Records.OfType<HookRecord>().Any())
            return;   // scenes after the first round are skipped in live mode, so the battles differ

        var replay = new RecordedPlayer(capture);
        var (state, jump) = await BattleSandbox.RunLiveAsync(capture, new NullView(), replay);

        Assert.Equal(capture.JumpTarget, jump);
        var differences = MemoryComparison.Compare(capture.Exit, state.Memory, null, GoldenFilter.Default)
            .Where(d => !d.Ignored).ToList();
        Assert.True(differences.Count == 0, string.Join("\n", differences.Take(20)));
    }

    private sealed class NullView : IBattleView
    {
        public Task ShowAsync(BattleEvent battleEvent) => Task.CompletedTask;
    }

    /// <summary>Attacks the first target it may, else defends; first menu item; keys 0.</summary>
    private sealed class AutoPlayer : IBattleInput
    {
        public int Inputs { get; private set; }

        private void Count()
        {
            if (++Inputs > 20000)
                throw new InvalidOperationException("The battle does not end.");
        }

        public Task<Sys3Int> WaitForKeyAsync()
        {
            Count();
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks) => Task.FromResult(Sys3Int.Zero);

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
        {
            Count();
            var attack = menu.Options.FirstOrDefault(o => o.Command.Value is >= 1 and <= 3 && o.Arguments.Count > 0)
                ?? menu.Find(FigtSelCommand.Defend)
                ?? menu.Options[0];
            return Task.FromResult(new FigtSelChoice(attack.Command, attack.Arguments[0]));
        }

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
        {
            Count();
            return Task.FromResult<Sys3Int?>(menu.Options[0].Code);
        }

        public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult((Sys3Int)1);
    }

    /// <summary>Answers from the capture's input records, in order (draws come from the generator).</summary>
    private sealed class RecordedPlayer(GoldenCapture capture) : IBattleInput
    {
        private readonly Queue<GoldenRecord> _inputs = new(capture.Records.Where(r => r is not RndRecord));

        private T Next<T>() where T : GoldenRecord =>
            _inputs.Dequeue() as T ?? throw new InvalidOperationException($"Expected a {typeof(T).Name}.");

        public Task<Sys3Int> WaitForKeyAsync() => Task.FromResult((Sys3Int)Next<KeyRecord>().Code);

        public Task<Sys3Int> PauseAsync(Sys3Int ticks) => Task.FromResult((Sys3Int)Next<WaitRecord>().Code);

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
        {
            var pick = Next<PickRecord>();
            return Task.FromResult(new FigtSelChoice((Sys3Int)pick.RetVal, (Sys3Int)pick.Tmp0001));
        }

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
        {
            var record = Next<MenuRecord>();
            return Task.FromResult<Sys3Int?>(record.Choice == 0 ? null : menu.Options[record.Choice - 1].Code);
        }

        public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult((Sys3Int)Next<MusicRecord>().Track);
    }
}
