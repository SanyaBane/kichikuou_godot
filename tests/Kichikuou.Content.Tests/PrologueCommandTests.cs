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
/// hideout, Turn END. The pickers run as ports and are answered with clicks on their grids and
/// bars; the battle is not ported into the router yet and answers through a recording hook.
/// Skipped without the decompile.
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
        unported.Effects["FIGT_A.ADV"] = s => s.Bt_Result = 2;
        int exp = state.Units[1].Exp.Value;
        int gold = state.Gold.Value;
        var rance = state.Units[1];
        int expenses = (rance.DeployCost + rance.Troops / 300).Value;
        state.Hist_Cur = 0;
        int spent = state.History[0].MilitaryExp.Value;

        input.Province(34);
        input.Pick(1);                                                  // Sﾌﾞﾀｲ: Rance,
        input.Button(1);                                                // Attack
        input.Menu(2);                                                  // Subordinates
        input.Pick(3);                                                  // Sﾌﾞｶ: Bound
        input.Menu(1);                                                  // Harem
        input.Pick(1);                                                  // Sﾊｰﾚﾑ: Sill
        input.Dungeon(1);
        input.Menu(5);                                                  // Turn END
        await script.CallFileAsync("COMMAND.ADV");

        output.WriteLine($"rules: {string.Join(", ", unported.Calls.Select(c => $"{c.File} {c.Entry}"))}");
        output.WriteLine($"draws: {string.Join(", ", random.Draws)}; inputs: {input.Reads} pointer reads; war expenses {expenses}");
        Assert.Equal(["FIGT_A.ADV"], unported.Calls.Select(c => c.File));
        Assert.Equal(0, input.Pending);
        Assert.Equal(1, state.Bt_PlayerParty1.Value);                   // Rance went alone,
        Assert.Equal(0, state.Bt_PlayerParty2.Value);
        Assert.Equal(2, state.Units[1].Status.Value);                   // spent for the week,
        Assert.Equal(spent + expenses, state.History[0].MilitaryExp.Value);
        Assert.Equal(1, state.F_Thieves_State.Value);                   // the thieves' first raid
        Assert.Equal(gold - expenses + 1, state.Gold.Value);            // paid for, and one gold won
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
    /// The new game enters the week loop (<c>&amp;#MAIN.ADV:</c>, routed by <see cref="StrategyRules"/>):
    /// week 9 opens with Sill's scene, week 10 with Soul's message, each week autosaves, refreshes
    /// the thieves and runs the player phase to Turn END.
    /// </summary>
    [Fact]
    public async Task The_first_prologue_weeks_run_through_the_week_loop()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var game = await NewGameAsync();
        int saves = 0;
        game.Unported.Effects["ﾛｰﾄﾞｾｰﾌﾞ.ADV"] = _ =>
        {
            if (++saves == 3)
                throw new WeekOverException();
        };
        var bound = game.State.Units[3];
        bound.Troops = bound.MaxTroops - 1;
        game.Input.Menu(5);                                             // week 9: Turn END
        game.Input.Menu(5);                                             // week 10: Turn END

        await Assert.ThrowsAsync<WeekOverException>(() => game.Script.CallFileAsync("MAIN.ADV"));

        Assert.Equal(["ﾛｰﾄﾞｾｰﾌﾞ.ADV", "ﾛｰﾄﾞｾｰﾌﾞ.ADV", "ﾛｰﾄﾞｾｰﾌﾞ.ADV"], game.Unported.Calls.Select(c => c.File));
        Assert.Equal(0, game.Input.Pending);
        Assert.Equal(11, game.State.Week.Value);
        Assert.Equal(0, game.State.StoryStage.Value);
        Assert.Equal(bound.MaxTroops.Value, bound.Troops.Value);        // +2 a week, capped
        Assert.Contains(game.View.Events, e => e is MessageEvent { Text.File: "Iｼﾚｲﾌﾞ.ADV" });
        Assert.Contains(game.View.Events, e => e is MessageEvent { Text.File: "Gｿｳﾙ.ADV" });
        Assert.Equal(2, game.View.Events.Count(e => e is MessageEvent { Text: { File: "MAIN.ADV", Line: 77 } }));
    }

    /// <summary>
    /// Turn END after week 22 ends the prologue: the thieves' last fight (<c>Iﾄｳｿﾞｸ</c> 2, the duel
    /// lost), the opening, <c>StoryStage 99 → 1</c> and <c>Iﾊﾝﾗﾝ</c> in the week loop; the next week
    /// is Leazas's, which is not ported yet.
    /// </summary>
    [Fact]
    public async Task Turn_END_in_week_23_ends_the_prologue_through_the_week_loop()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var game = await NewGameAsync();
        game.State.Week = 22;
        game.Unported.Effects["FIGT_A.ADV"] = s => s.Bt_Result = 1;     // the duel is lost
        game.Input.Menu(5);                                             // Turn END

        var e = await Assert.ThrowsAsync<NotSupportedException>(() => game.Script.CallFileAsync("MAIN.ADV"));

        output.WriteLine($"rules: {string.Join(", ", game.Unported.Calls.Select(c => $"{c.File} {c.Entry}"))}");
        Assert.StartsWith("MAIN.ADV:83", e.Message);
        Assert.Equal(["ﾛｰﾄﾞｾｰﾌﾞ.ADV", "FIGT_A.ADV"], game.Unported.Calls.Select(c => c.File));
        Assert.Equal(0, game.Input.Pending);
        Assert.Equal(1, game.State.StoryStage.Value);
        Assert.Equal(1, game.State.F_OpeningSeen.Value);
        Assert.Contains(game.View.Events, ev => ev is MessageEvent { Text.File: "Iﾊﾝﾗﾝ.ADV" });
        Assert.Equal(25, game.State.Week.Value);                        // 23, Iﾊﾝﾗﾝ's + 1, the next week's + 1
    }

    private sealed record Game(GameState State, ScriptContext Script, PlayerInput Input, RecordingView View, RecordingRules Unported);

    /// <summary>The boot and the new game with the rule files routed and the scenes run from the decompile.</summary>
    private static async Task<Game> NewGameAsync()
    {
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var view = new RecordingView();
        var input = new PlayerInput(state);
        var unported = new RecordingRules { State = state };
        var rules = new StrategyRules(scripts, unported);
        var runner = new SceneRunner(scripts, rules);
        var script = new ScriptContext(state, new QueuedRandom { DefaultToOne = true }, view, input, runner);
        runner.Bind(script);
        rules.Bind(script);
        var setup = new GameSetup(script, scripts);
        await setup.BootAsync();
        await setup.NewGameAsync();
        unported.Calls.Clear();
        return new Game(state, script, input, view, unported);
    }

    private sealed class WeekOverException : Exception;

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

        /// <summary>A click on unit or girl <paramref name="id"/> in a picker's grid.</summary>
        public void Pick(int id) => ScreenClick(PointerScript.PickerCell(id));

        /// <summary>A click on bar button <paramref name="button"/> (1-3) under a picker's grid.</summary>
        public void Button(int button) => ScreenClick(PointerScript.PickerButton(button));

        private void ScreenClick((int X, int Y) at) => _gestures.Enqueue(() =>
            [new((Sys3Int)16, (Sys3Int)at.X, (Sys3Int)at.Y), new(Sys3Int.Zero, (Sys3Int)at.X, (Sys3Int)at.Y)]);

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

        private int _timer;

        /// <summary><c>ZT 1</c>: the timer starts at the value (the opening times its animation).</summary>
        public Task ResetTimerAsync(Sys3Int value)
        {
            _timer = value.Value;
            return Task.CompletedTask;
        }

        /// <summary><c>ZT n</c>: 100 ticks pass on every read.</summary>
        public Task<Sys3Int> GetTimerAsync(int mode)
        {
            _timer += 100;
            return Task.FromResult((Sys3Int)(_timer & 0xffff));
        }

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() =>
            Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)999, (Sys3Int)999, (Sys3Int)999, (Sys3Int)999]);

        public Task<Sys3Int> GetMidiTrackAsync() => Task.FromResult(Sys3Int.Zero);
    }
}
