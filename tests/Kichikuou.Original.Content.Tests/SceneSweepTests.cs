using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.Setup;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// Every entry of every scene file runs from the new game's world under several answer policies — the menus'
/// first item, their last, random items and draws, and random story flags — with the rule files stubbed, and
/// nothing may fail but a scene that loops on its input (the pointer, or a draw the fixed policies never change).
/// A hunt for what no capture reaches (Shangri-La's end met an unported map operation on the owner's phone,
/// 2026-10-30; its menu's first item reaches it); a failure under scrambled flags may be a state the original never
/// reaches, so each is checked against the script. Quick: 2 seeds a random policy (~5k runs, seconds); full: 16.
/// </summary>
public class SceneSweepTests(ITestOutputHelper output)
{
    private enum Policy { First, Last, Random, Scrambled }

    [ScenarioFact]
    public async Task Every_scene_entry_runs_under_every_policy()
    {
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var booted = await BootAsync(scripts);
        var files = Directory.EnumerateFiles(LocalData.ScenarioDirectory, "*.ADV")
            .Select(Path.GetFileName)
            .Where(f => f![0] is 'G' or 'I')
            .Append("ｵｰﾌﾟﾆﾝｸﾞ.ADV")
            .Order(StringComparer.Ordinal)
            .ToList();
        int seeds = DifferentialRuns.Cases(16);
        var runs = new List<(string File, int Entry, Policy Policy, int Seed)>();
        foreach (string file in files!)
        {
            var entries = scripts.ReadProgram(file!).FindEntries(OriginalVariant.Profile.EventEntryVariable)
                .Select(e => (int)e.Entry.Value).Distinct().DefaultIfEmpty(0);
            foreach (int entry in entries)
            {
                runs.Add((file!, entry, Policy.First, 0));
                runs.Add((file!, entry, Policy.Last, 0));
                for (int seed = 1; seed <= seeds; seed++)
                {
                    runs.Add((file!, entry, Policy.Random, seed));
                    runs.Add((file!, entry, Policy.Scrambled, seed));
                }
            }
        }

        var failures = new System.Collections.Concurrent.ConcurrentBag<(string Key, string Run)>();
        int loops = 0;
        await Parallel.ForEachAsync(runs, async (run, _) =>
        {
            string outcome = await RunAsync(scripts, booted, run.File, run.Entry, run.Policy, run.Seed);
            if (outcome == "loops")
                Interlocked.Increment(ref loops);
            else if (outcome.Length > 0)
                failures.Add((outcome, $"{run.File} {run.Entry} {run.Policy} {run.Seed}"));
        });

        var groups = failures.GroupBy(f => f.Key).OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
        output.WriteLine($"{runs.Count} runs over {files.Count} files; {loops} waited on the pointer; {failures.Count} failed in {groups.Count} ways.");
        foreach (var g in groups)
            output.WriteLine($"{g.Count(),5} × {g.Key} — e.g. {string.Join("; ", g.Select(f => f.Run).Order(StringComparer.Ordinal).Take(3))}");
        Assert.Empty(groups);
    }

    private static async Task<GameState> BootAsync(ScenarioScripts scripts)
    {
        var rig = Rig(scripts, new GameState(), new PolicyRandom(Policy.First, 0), new PolicyInput(Policy.First, 0));
        var setup = new GameSetup(rig.Script, scripts);
        await setup.BootAsync();
        await setup.NewGameAsync();
        return rig.State;
    }

    private static async Task<string> RunAsync(ScenarioScripts scripts, GameState booted, string file, int entry, Policy policy, int seed)
    {
        var state = new GameState();
        booted.CopyTo(state);
        if (policy == Policy.Scrambled)
            Scramble(state, new Random(seed * 7919 + entry));
        var rig = Rig(scripts, state, new PolicyRandom(policy, seed), new PolicyInput(policy, seed));
        try
        {
            await rig.Script.EventAsync(file, (Sys3Int)entry);
            return "";
        }
        catch (Exception e) when (e is ScenarioJumpException or GameQuitException)
        {
            return "";
        }
        catch (Exception e) when (e.Message.Contains("loops on its input", StringComparison.Ordinal))
        {
            return "loops";
        }
        catch (Exception e)
        {
            return $"{e.GetType().Name}: {e.Message}";
        }
    }

    /// <summary>Every story variable at a random value from 0 to 8.</summary>
    private static void Scramble(GameState state, Random random)
    {
        for (int n = 0; n < VariableCatalog.Variables.Count; n++)
        {
            if (VariableCatalog.Variables[n].Group == "story")
                state.Memory.Var(n) = (Sys3Int)random.Next(0, 9);
        }
    }

    private static (GameState State, OriginalScriptContext Script) Rig(ScenarioScripts scripts, GameState state, IScriptRandom random, ISceneInput input)
    {
        var runner = new SceneRunner(scripts, new StubRules());
        var script = new OriginalScriptContext(state, random, new NullView(), input, runner);
        runner.Bind(script);
        return (state, script);
    }

    private const string Loops = "the scene loops on its input.";

    /// <summary>Shows nothing; a scene that loops without input still shows something each round, so it is stopped here.</summary>
    private sealed class NullView : IScriptView
    {
        private int _events;

        public Task ShowAsync(ScriptEvent scriptEvent) =>
            ++_events > 200_000 ? throw new InvalidOperationException($"More than 200000 events: {Loops}") : Task.CompletedTask;
    }

    /// <summary>The rule files do nothing; a scene that calls one again and again waiting for its effect is stopped.</summary>
    private sealed class StubRules : IStoryHooks
    {
        private int _calls;

        public Task RunEventAsync(string file, Sys3Int entry) =>
            ++_calls > 2_000 ? throw new InvalidOperationException($"More than 2000 rule calls: {Loops}") : Task.CompletedTask;
    }

    /// <summary>Draws by the policy; a scene that draws again until it gets another value loops under a fixed one.</summary>
    private sealed class PolicyRandom(Policy policy, int seed) : IScriptRandom
    {
        private readonly Random _random = new(seed);
        private int _draws;

        public Sys3Int Next(Sys3Int n) =>
            n.Value <= 1 ? n
            : ++_draws > 100_000 ? throw new InvalidOperationException($"More than 100000 draws: {Loops}")
            : policy switch
            {
                Policy.First => (Sys3Int)1,
                Policy.Last => n,
                _ => (Sys3Int)_random.Next(1, n.Value + 1),
            };
    }

    /// <summary>Menus answered by the policy (a random one cancels now and then), keys 0 or a random button.</summary>
    private sealed class PolicyInput(Policy policy, int seed) : ISceneInput
    {
        private readonly Random _random = new(seed + 1);
        private int _answers;
        private int _timer;

        private PointerScript Pointer { get; } = new();

        public Task<Sys3Int> WaitForKeyAsync() => Answer(policy is Policy.First or Policy.Last ? Sys3Int.Zero : (Sys3Int)(_random.Next(3) * 16));

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => Answer(Sys3Int.Zero);

        public Task<Sys3Int> GetPlayingTrackAsync() => Answer(Sys3Int.Zero);

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items) => Answer(policy switch
        {
            Policy.First => 1,
            Policy.Last => items.Count,
            _ => _random.Next(10) == 0 ? 0 : _random.Next(1, items.Count + 1),
        });

        public Task<MouseState> GetMouseAsync() => Answer(Pointer.Next(out _));

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => Answer((Sys3Int)1);

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() =>
            Answer<IReadOnlyList<Sys3Int>>([(Sys3Int)2026, (Sys3Int)10, (Sys3Int)30, (Sys3Int)12, Sys3Int.Zero, Sys3Int.Zero, (Sys3Int)5]);

        public Task ResetTimerAsync(Sys3Int value)
        {
            _timer = value.Value;
            return Answer(0);
        }

        public Task<Sys3Int> GetTimerAsync(int mode)
        {
            _timer += 100;
            return Answer((Sys3Int)(_timer & 0xffff));
        }

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() =>
            Answer<IReadOnlyList<Sys3Int>>([(Sys3Int)999, (Sys3Int)999, (Sys3Int)999, (Sys3Int)999]);

        public Task<Sys3Int> GetMidiTrackAsync() => Answer(Sys3Int.Zero);

        private Task<T> Answer<T>(T value)
        {
            if (++_answers > 20_000)
                throw new InvalidOperationException("More than 20000 inputs: the scene loops on its input.");
            return Task.FromResult(value);
        }
    }
}
