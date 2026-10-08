using Kichikuou.Content.Scripts;
using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// A prologue week's player phase on the world the boot and the new game build, with the rule
/// files routed by <see cref="StrategyRules"/> and the scenes run from the player's decompile by
/// <see cref="SceneRunner"/>: Pillage on province 34, a talk with Bound, a visit to Sill, the
/// hideout, Turn END. The party picker, the battle and the list pickers are not ported yet and
/// answer through a recording hook. Skipped without the decompile.
/// </summary>
public class PrologueCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task A_prologue_week_runs_through_the_ported_commands_and_the_players_scenes()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var random = new QueuedRandom { DefaultToOne = true };
        var view = new RecordingView();
        var input = new PlayerInput(state);
        var unported = new RecordingRules { State = state };
        var rules = new StrategyRules(scripts, unported);
        var runner = new SceneRunner(scripts, rules);
        var script = new ScriptContext(state, random, view, input, runner);
        runner.Bind(script);
        rules.Bind(script);
        var setup = new GameSetup(script, scripts);
        await setup.BootAsync();
        await setup.NewGameAsync();
        unported.Calls.Clear();

        state.Week = 10;                                                // MAIN's week + 1 and more: COM plays Gｿｳﾙ 4
        unported.Effects["Sﾌﾞﾀｲ.ADV"] = s => s.Bt_PlayerParty1 = 1;
        unported.Effects["FIGT_A.ADV"] = s => s.Bt_Result = 2;
        unported.Effects["Sﾌﾞｶ.ADV"] = s =>
        {
            if (s.CallArg == 1)
                (s.CallArg, s.Unit_Cur) = ((Sys3Int)3, (Sys3Int)3);    // Bound
        };
        unported.Effects["Sﾊｰﾚﾑ.ADV"] = s => s.CallArg = 1;           // Sill
        int exp = state.Units[1].Exp.Value;
        int gold = state.Gold.Value;

        input.Province(34);
        input.Menu(2);                                                  // Subordinates
        input.Menu(1);                                                  // Harem
        input.Dungeon(1);
        input.Menu(5);                                                  // Turn END
        await script.CallFileAsync("COMMAND.ADV");

        output.WriteLine($"rules: {string.Join(", ", unported.Calls.Select(c => $"{c.File} {c.Entry}"))}");
        output.WriteLine($"draws: {string.Join(", ", random.Draws)}; inputs: {input.Reads} pointer reads");
        Assert.Equal(
            ["Sﾌﾞﾀｲ.ADV", "FIGT_A.ADV", "Sﾌﾞｶ.ADV", "Sﾌﾞｶ.ADV", "Sﾌﾞｶ.ADV", "Sﾊｰﾚﾑ.ADV"],
            unported.Calls.Select(c => c.File));
        Assert.Equal(0, input.Pending);
        Assert.Equal(1, state.F_Thieves_State.Value);                   // the thieves' first raid
        Assert.Equal(gold + 1, state.Gold.Value);
        Assert.Equal(0, state.Ui_CmdAttackAvail.Value);
        Assert.Equal(0, state.Ui_CmdSubordAvail.Value);                 // the week's talk
        Assert.Equal(0, state.Ui_CmdHaremAvail.Value);                  // the week's visit
        Assert.Equal(exp + 5, state.Units[1].Exp.Value);                // Sill's reward
        Assert.Equal(1, state.History[1].Harem_SexCount.Value);
        Assert.Equal(1, state.Ui_CmdDungeonAvail.Value);                // the hideout is free
        Assert.Equal(0, state.StoryStage.Value);
        // The raid's banter (Iﾄｳｿﾞｸ:342), Bound's talk (Gﾊﾞｳﾝﾄﾞ:9), the hideout's banter (Cﾒｲｷｭｳ:2804).
        Assert.Equal([4, 2, 3], random.Draws);
        Assert.Contains(view.Events, e => e is MessageEvent { Text.File: "Cﾒｲｷｭｳ.ADV" });
    }

    /// <summary>
    /// The player as pointer gestures placed when they begin (the camera moves between them), each
    /// after three reads at rest since the last gesture or other input, and menu answers by file:
    /// the command menu from a queue, Pillage in <c>Cﾃｷﾄｼ</c>, the first item anywhere else.
    /// </summary>
    private sealed class PlayerInput(GameState state) : ISceneInput
    {
        private readonly Queue<Func<MouseState[]>> _gestures = new();
        private readonly Queue<int> _commands = new();
        private MouseState[]? _current;
        private int _index;
        private int _rests;
        private MouseState _rest;

        public int Reads { get; private set; }

        public int Pending => _gestures.Count + (_current is null ? 0 : 1);

        public void Province(int p) => Click(() => (state.Provs[p].MapX, state.Provs[p].MapY));

        public void Dungeon(int d) => Click(() => (state.Dgns[d].MapX, state.Dgns[d].MapY));

        /// <summary>A right click on the map, then the command menu's item.</summary>
        public void Menu(int item)
        {
            _gestures.Enqueue(() => [new((Sys3Int)32, (Sys3Int)5, (Sys3Int)6), new(Sys3Int.Zero, (Sys3Int)5, (Sys3Int)6)]);
            _commands.Enqueue(item);
        }

        private void Click(Func<(Sys3Int X, Sys3Int Y)> at) => _gestures.Enqueue(() =>
        {
            // The gesture begins in the map's poll, where the camera is the view's corner.
            var (x, y) = at();
            int sx = x.Value - state.MapCamX.Value, sy = y.Value - state.MapCamY.Value;
            if (sx is < 0 or >= 640 || sy is < 0 or >= 400)
                throw new InvalidOperationException($"({x}, {y}) is not in view (corner {state.MapCamX}, {state.MapCamY}).");
            return [new((Sys3Int)16, (Sys3Int)sx, (Sys3Int)sy), new(Sys3Int.Zero, (Sys3Int)sx, (Sys3Int)sy)];
        });

        public Task<MouseState> GetMouseAsync()
        {
            if (++Reads > 10_000)
                throw new InvalidOperationException("The pointer was read 10000 times.");
            if (_current is null && _gestures.Count > 0 && _rests >= 3)
            {
                _current = _gestures.Dequeue()();
                _index = 0;
            }
            if (_current is not null)
            {
                var m = _current[_index++];
                if (_index == _current.Length)
                {
                    _current = null;
                    _rests = 0;
                }
                _rest = m with { Button = Sys3Int.Zero };
                return Task.FromResult(m);
            }
            if (_gestures.Count == 0 && _rests > 50)
                throw new InvalidOperationException("The gestures ran out.");
            _rests++;
            return Task.FromResult(_rest);
        }

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            _rests = 0;
            int choice = items[0].At.File switch
            {
                "COMMAND.ADV" => _commands.Dequeue(),
                "Cﾃｷﾄｼ.ADV" => items.Select((item, i) => (item, i)).Single(x => x.item.At.Line == 70).i + 1,
                _ => 1,
            };
            return Task.FromResult(choice);
        }

        public Task<Sys3Int> WaitForKeyAsync()
        {
            _rests = 0;
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks)
        {
            _rests = 0;
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => Task.FromResult((Sys3Int)1);

        public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult(Sys3Int.Zero);

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() =>
            Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)2026, (Sys3Int)10, (Sys3Int)8, (Sys3Int)12, Sys3Int.Zero, Sys3Int.Zero, (Sys3Int)5]);

        public Task ResetTimerAsync(Sys3Int value) => Task.CompletedTask;

        public Task<Sys3Int> GetTimerAsync(int mode) => Task.FromResult(Sys3Int.Zero);

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() =>
            Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)999, (Sys3Int)999, (Sys3Int)999, (Sys3Int)999]);

        public Task<Sys3Int> GetMidiTrackAsync() => Task.FromResult(Sys3Int.Zero);
    }
}
