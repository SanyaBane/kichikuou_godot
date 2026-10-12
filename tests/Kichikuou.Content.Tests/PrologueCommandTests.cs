using Kichikuou.Content.Scripts;
using Kichikuou.Core.Combat;
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
/// hideout, Turn END; the week loop around it; the prologue's end. The pickers run as ports and
/// are answered with clicks on their grids and bars. The battle runs in the router as well; the
/// tests about the commands and the story stub it (<see cref="StubbedFiles"/>) to choose its
/// outcome, and one fights it. Skipped without the decompile.
/// </summary>
public class PrologueCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task A_prologue_week_runs_through_the_ported_commands_and_the_players_scenes()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var game = await NewGameAsync(stubBattle: true);
        var (state, script, input, view, unported, random) = (game.State, game.Script, game.Input, game.View, game.Unported, game.Random);

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
    /// Pillage on province 34 fought for real: <c>Iﾄｳｿﾞｸ</c> calls <c>FIGT_A</c>, which the router runs
    /// as the battle on the game's context and input (owner's decision 7), so nothing reaches the
    /// unported hooks.
    /// </summary>
    [Fact]
    public async Task A_pillage_is_fought_as_a_battle_through_the_router()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var game = await NewGameAsync();
        var state = game.State;
        state.Week = 10;
        int gold = state.Gold.Value;
        int expenses = (state.Units[1].DeployCost + state.Units[1].Troops / 300).Value;
        game.Input.Province(34);
        game.Input.Pick(1);                                             // Sﾌﾞﾀｲ: Rance,
        game.Input.Button(1);                                           // Attack
        game.Input.Menu(5);                                             // Turn END

        await game.Script.CallFileAsync("COMMAND.ADV");

        output.WriteLine($"result {state.Bt_Result}, gold {gold} -> {state.Gold}, draws {game.Random.Draws.Count}, " +
                         $"commands {game.Input.BattleCommands}, Rance HP {state.Units[1].HP}/{state.Units[1].MaxHP}, troops {state.Units[1].Troops}");
        Assert.Empty(game.Unported.Calls);
        Assert.Equal(0, game.Input.Pending);
        Assert.True(game.Input.BattleCommands > 0);
        Assert.Contains(game.View.Events, e => e is RoundHeaderEvent);
        Assert.Equal(2, state.Bt_Result.Value);                         // with every draw 1, Rance wins
        Assert.Equal(gold - expenses + 1, state.Gold.Value);            // and the raid brings one gold
        Assert.Equal(0, state.Bt_Kind.Value);                           // FIGT_A's exit
        Assert.Equal(1, state.F_Thieves_State.Value);
        Assert.Equal(0, state.Ui_CmdAttackAvail.Value);
    }

    /// <summary>
    /// Story stage 1 (milestone 2, step 3): after the prologue's end (<c>Iﾊﾝﾗﾝ</c> gives the rebels
    /// provinces 3-7) and the rebels' first turn, Rance attacks province 3 alone. <c>Cﾃｷﾄｼ</c>'s attack
    /// calls the ported picker (answered with clicks), the rebels' defenders (<c>TCﾘｰｻﾞｽ2</c>) and the
    /// battle, all through the router: nothing reaches the unported hooks.
    /// </summary>
    [Fact]
    public async Task An_attack_on_the_rebels_is_fought_through_the_router()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var game = await NewGameAsync();
        var state = game.State;
        await game.Script.EventAsync("Iﾊﾝﾗﾝ.ADV", 1);
        state.F_LeazasRebels_State = 2;                                 // the rebels' first turn (TCﾘｰｻﾞｽ.ADV:5-8)
        game.Unported.Calls.Clear();
        Assert.Equal(2, state.Provs[3].Owner.Value);
        int gold = state.Gold.Value;
        int expenses = (state.Units[1].DeployCost + state.Units[1].Troops / 300).Value;
        game.Input.Province(3);
        game.Input.Pick(1);                                             // Sﾌﾞﾀｲ: Rance,
        game.Input.Button(1);                                           // Go
        game.Input.Menu(-1);                                            // Turn END

        await game.Script.CallFileAsync("COMMAND.ADV");

        output.WriteLine($"result {state.Bt_Result}, owner of 3: {state.Provs[3].Owner}, gold {gold} -> {state.Gold}, " +
                         $"enemy {state.Bt_EnemyParty1}/{state.Bt_EnemyParty2}/{state.Bt_EnemyParty3}/{state.Bt_EnemyParty4}, " +
                         $"commands {game.Input.BattleCommands}, draws {game.Random.Draws.Count}");
        Assert.Empty(game.Unported.Calls);
        Assert.Equal(0, game.Input.Pending);
        Assert.Equal(2, state.Ev_PhaseNation.Value);                    // TCﾘｰｻﾞｽ2 chose the defenders:
        Assert.Equal([3, 5, 4], new[] { state.Bt_EnemyParty1, state.Bt_EnemyParty2, state.Bt_EnemyParty3 }.Select(c => c.Value));
        Assert.True(game.Input.BattleCommands > 0);                     // army A, fought
        Assert.Contains(game.View.Events, e => e is RoundHeaderEvent);
        Assert.Equal(0, state.Bt_Kind.Value);
        Assert.Equal(1, state.Bt_PlayerParty1.Value);
        Assert.Equal(gold - expenses, state.Gold.Value);                // the war expenses
        Assert.Equal(state.Bt_Result == 2 ? 1 : 2, state.Provs[3].Owner.Value);
        Assert.Equal(state.Bt_Result == 2 ? 0 : 1, state.Ui_CmdAttackAvail.Value);
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
        game.Saves.Written = (slot, data) =>
        {
            if (slot == 1 && data.Variables[517] == 11)                 // week 11's autosave (QD 1)
                throw new WeekOverException();
        };
        var bound = game.State.Units[3];
        bound.Troops = bound.MaxTroops - 1;
        game.Input.Menu(5);                                             // week 9: Turn END
        game.Input.Menu(5);                                             // week 10: Turn END

        await Assert.ThrowsAsync<WeekOverException>(() => game.Script.CallFileAsync("MAIN.ADV"));

        Assert.Empty(game.Unported.Calls);                              // the autosave is ported (ﾛｰﾄﾞｾｰﾌﾞ 3)
        Assert.Equal(0, game.Input.Pending);
        Assert.Equal(11, game.State.Week.Value);
        var autosave = game.Saves.Read(1)!;                             // week 10's, at the start of its player phase
        Assert.Equal(new ScriptRef("MAIN.ADV", 20), autosave.Resume);
        Assert.Equal(10, autosave.Variables[517].Value);                // Week
        Assert.Equal([1], game.Saves.Slots);
        Assert.Equal(0, game.State.StoryStage.Value);
        Assert.Equal(bound.MaxTroops.Value, bound.Troops.Value);        // +2 a week, capped
        Assert.Contains(game.View.Events, e => e is MessageEvent { Text.File: "Iｼﾚｲﾌﾞ.ADV" });
        Assert.Contains(game.View.Events, e => e is MessageEvent { Text.File: "Gｿｳﾙ.ADV" });
        Assert.Equal(2, game.View.Events.Count(e => e is MessageEvent { Text: { File: "MAIN.ADV", Line: 77 } }));
    }

    /// <summary>
    /// Turn END after week 22 ends the prologue: the thieves' last fight (<c>Iﾄｳｿﾞｸ</c> 2, the duel
    /// lost), the opening, <c>StoryStage 99 → 1</c> and <c>Iﾊﾝﾗﾝ</c> in the week loop; the next week
    /// is Leazas's, stopped at its autosave (<c>MAIN.ADV:83</c>).
    /// </summary>
    [Fact]
    public async Task Turn_END_in_week_23_ends_the_prologue_through_the_week_loop()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var game = await NewGameAsync(stubBattle: true);
        game.State.Week = 22;
        game.Unported.Effects["FIGT_A.ADV"] = s => s.Bt_Result = 1;     // the duel is lost
        game.Input.Menu(5);                                             // Turn END
        game.Saves.Written = (_, data) =>
        {
            if (data.Resume == new ScriptRef("MAIN.ADV", 83))
                throw new WeekOverException();                          // the Leazas week's autosave
        };

        await Assert.ThrowsAsync<WeekOverException>(() => game.Script.CallFileAsync("MAIN.ADV"));

        output.WriteLine($"rules: {string.Join(", ", game.Unported.Calls.Select(c => $"{c.File} {c.Entry}"))}");
        Assert.Equal(["FIGT_A.ADV"], game.Unported.Calls.Select(c => c.File));
        Assert.Equal([1], game.Saves.Slots);
        Assert.Equal(0, game.Input.Pending);
        Assert.Equal(1, game.State.StoryStage.Value);
        Assert.Equal(1, game.State.F_OpeningSeen.Value);
        Assert.Contains(game.View.Events, ev => ev is MessageEvent { Text.File: "Iﾊﾝﾗﾝ.ADV" });
        Assert.Equal(25, game.State.Week.Value);                        // 23, Iﾊﾝﾗﾝ's + 1, the next week's + 1
    }

    /// <summary>
    /// A whole game through <see cref="GameSession"/>: the new game plays week 9 and stops as week
    /// 10 autosaves; a second game loads slot 1 ("Auto") from the title and goes on from week 9's
    /// autosave — "Load Complete", the week's date and banner, Sill's scene, the player phase — to
    /// week 10's autosave.
    /// </summary>
    [Fact]
    public async Task A_game_saved_at_the_start_of_a_week_loads_from_the_title_and_goes_on_from_there()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var saves = new WatchedSaves
        {
            Written = (slot, data) =>
            {
                if (slot == 1 && data.Variables[517] == 10)             // week 10's autosave (QD 1)
                    throw new WeekOverException();
            },
        };
        var first = await SessionAsync(saves);
        first.Input.Menu(5);                                            // week 9: Turn END
        await Assert.ThrowsAsync<WeekOverException>(() => first.Session.NewGameAsync());
        Assert.Equal(9, saves.Read(1)!.Variables[517].Value);

        var second = await SessionAsync(saves);
        second.Input.Menu(5);                                           // week 9 again: Turn END
        await Assert.ThrowsAsync<WeekOverException>(() => second.Session.LoadFromTitleAsync());

        var events = second.View.Events;
        int loaded = events.FindIndex(e => e is MessageEvent { Text: { File: "ﾛｰﾄﾞｾｰﾌﾞ.ADV", Line: 71 } });   // "Load Complete"
        int banner = events.FindIndex(e => e is MessageEvent { Text: { File: "MAIN.ADV", Line: 35 } });        // "Rance's Thieves Phase"
        int scene = events.FindIndex(e => e is MessageEvent { Text.File: "Iｼﾚｲﾌﾞ.ADV" });
        Assert.True(loaded >= 0 && loaded < banner && banner < scene, $"Load Complete {loaded}, banner {banner}, Sill {scene}");
        Assert.Equal(10, second.Session.State.Week.Value);
        Assert.Equal(0, second.Input.Pending);
    }

    /// <summary>
    /// Load/Save in the player phase, Load, "Auto": the session leaves the week for its start —
    /// "Load Complete", the week's banner again — and the replayed week goes on to Turn END.
    /// </summary>
    [Fact]
    public async Task Loading_from_the_command_menu_goes_back_to_the_start_of_the_week()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var saves = new WatchedSaves
        {
            Written = (slot, data) =>
            {
                if (slot == 1 && data.Variables[517] == 10)
                    throw new WeekOverException();
            },
        };
        var game = await SessionAsync(saves);
        game.Input.Menu(4);                                             // week 9: Load/Save → Load → "Auto"
        game.Input.Menu(5);                                             // week 9 again: Turn END

        await Assert.ThrowsAsync<WeekOverException>(() => game.Session.NewGameAsync());

        var events = game.View.Events;
        Assert.Single(events, e => e is MessageEvent { Text: { File: "ﾛｰﾄﾞｾｰﾌﾞ.ADV", Line: 71 } });
        Assert.Equal(2, events.Count(e => e is MessageEvent { Text: { File: "MAIN.ADV", Line: 35 } }));
        Assert.Equal(10, game.Session.State.Week.Value);
    }

    /// <summary>
    /// The end of a game through <see cref="GameSession"/> (decision 19): a jump to <c>OVER</c> —
    /// here thrown, with Maria's ending (5), where week 10 would autosave — runs the file from the
    /// player's files to its <c>ZZ0</c>, and the session returns the ending; the clear is counted on
    /// page 4 and kept in slot 1, which holds week 9's autosave.
    /// </summary>
    [Fact]
    public async Task A_game_that_reaches_an_ending_runs_OVER_and_keeps_the_clear_in_its_slots()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        Played? game = null;
        bool ended = false;
        var saves = new WatchedSaves
        {
            Written = (slot, data) =>
            {
                if (!ended && slot == 1 && data.Variables[517] == 10)    // week 10's autosave (QD 1)
                {
                    ended = true;
                    game!.Session.State.EndingId = 5;
                    throw new ScenarioJumpException("OVER.ADV");
                }
            },
        };
        game = await SessionAsync(saves);
        game.Input.Menu(5);                                             // week 9: Turn END

        var end = await game.Session.NewGameAsync();

        Assert.Equal(5, end.EndingId.Value);
        Assert.Equal(1, game.Session.State.Fates[79].Fate.Value);
        var slot = saves.Read(1)!;
        Assert.Equal(9, slot.Variables[517].Value);
        Assert.Equal(1, slot.Pages[4][79].Value);
        Assert.Contains(game.View.Events, e => e is MessageEvent { Text: { File: "OVER.ADV", Line: 201 } });   // "Completion Report"
    }

    [Fact]
    public async Task Backing_out_of_the_load_list_returns_to_the_title()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var game = await SessionAsync(new WatchedSaves());               // no slots: every item lists them again…
        game.Input.CancelMenus = true;                                   // …so the player cancels

        Assert.Null(await game.Session.LoadFromTitleAsync());
    }

    private sealed record Played(GameSession Session, PlayerInput Input, RecordingView View);

    /// <summary>A booted game session on the player's files (every draw 1; unported files recorded).</summary>
    private static async Task<Played> SessionAsync(WatchedSaves saves)
    {
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var view = new RecordingView();
        var input = new PlayerInput(state);
        var session = new GameSession(
            state, new QueuedRandom { DefaultToOne = true }, view, input, scripts, scripts, saves, new RecordingRules { State = state });
        await session.BootAsync();
        return new Played(session, input, view);
    }

    private sealed record Game(
        GameState State, OriginalScriptContext Script, PlayerInput Input, RecordingView View, RecordingRules Unported, QueuedRandom Random,
        WatchedSaves Saves);

    /// <summary>
    /// The boot and the new game with the rule files routed and the scenes run from the decompile;
    /// with <paramref name="stubBattle"/> <c>FIGT_A</c> goes to the recording hooks instead of the
    /// router (its outcome set by the test), every draw is 1.
    /// </summary>
    private static async Task<Game> NewGameAsync(bool stubBattle = false)
    {
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var random = new QueuedRandom { DefaultToOne = true };
        var view = new RecordingView();
        var input = new PlayerInput(state);
        var unported = new RecordingRules { State = state };
        var rules = new StrategyRules(scripts, unported);
        var runner = new SceneRunner(scripts, stubBattle ? new StubbedFiles(rules, unported, "FIGT_A.ADV") : rules);
        var saves = new WatchedSaves();
        var script = new OriginalScriptContext(state, random, view, input, runner) { Saves = saves };
        runner.Bind(script);
        rules.Bind(script);
        var setup = new GameSetup(script, scripts);
        await setup.BootAsync();
        await setup.NewGameAsync();
        unported.Calls.Clear();
        return new Game(state, script, input, view, unported, random, saves);
    }

    /// <summary>Save slots in memory that report every write.</summary>
    private sealed class WatchedSaves : ISaveStore
    {
        private readonly MemorySaveStore _slots = new();

        public Action<int, SaveData>? Written { get; set; }

        public IEnumerable<int> Slots => _slots.Slots;

        public SaveData? Read(int slot) => _slots.Read(slot);

        public bool Write(int slot, SaveData data)
        {
            Written?.Invoke(slot, data);
            return _slots.Write(slot, data);
        }

        public DateTime? Timestamp(int slot) => _slots.Timestamp(slot);
    }

    /// <summary>The router, but the given files go to the stubs.</summary>
    private sealed class StubbedFiles(IStoryHooks rules, IStoryHooks stubs, params string[] files) : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry) =>
            files.Contains(file) ? stubs.RunEventAsync(file, entry) : rules.RunEventAsync(file, entry);
    }

    private sealed class WeekOverException : Exception;

    /// <summary>
    /// The player as pointer gestures placed when they begin (the camera moves between them), each
    /// after three reads at rest since the last gesture or other input, and menu answers by file:
    /// the command menu from a queue, Attack or Pillage in <c>Cﾃｷﾄｼ</c>, the first item anywhere else.
    /// </summary>
    private sealed class PlayerInput(GameState state) : IGameInput
    {
        /// <summary>FIGT_SEL answers given: each unit attacks the first enemy it may, or defends.</summary>
        public int BattleCommands { get; private set; }

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
        {
            BattleCommands++;
            _rests = 0;
            var direct = menu.Find(FigtSelCommand.Direct);
            return Task.FromResult(direct is { Arguments.Count: > 0 }
                ? new FigtSelChoice(FigtSelCommand.Direct, direct.Arguments[0])
                : new FigtSelChoice(FigtSelCommand.Defend, menu.Find(FigtSelCommand.Defend)!.Arguments[0]));
        }

        /// <summary>A battle menu: its first option.</summary>
        public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
        {
            _rests = 0;
            return Task.FromResult<Sys3Int?>(menu.Options[0].Code);
        }

        private readonly Queue<Func<MouseState[]>> _gestures = new();
        private readonly Queue<int> _commands = new();
        private MouseState[]? _current;
        private int _index;
        private int _rests;
        private MouseState _rest;

        public int Reads { get; private set; }

        public int Pending => _gestures.Count + (_current is null ? 0 : 1);

        /// <summary>Every menu is cancelled (right click).</summary>
        public bool CancelMenus { get; set; }

        public void Province(int p) => Click(() => (state.Provs[p].MapX, state.Provs[p].MapY));

        public void Dungeon(int d) => Click(() => (state.Dgns[d].MapX, state.Dgns[d].MapY));

        /// <summary>A click on unit or girl <paramref name="id"/> in a picker's grid.</summary>
        public void Pick(int id) => ScreenClick(PointerScript.PickerCell(id));

        /// <summary>A click on bar button <paramref name="button"/> (1-3) under a picker's grid.</summary>
        public void Button(int button) => ScreenClick(PointerScript.PickerButton(button));

        private void ScreenClick((int X, int Y) at) => _gestures.Enqueue(() =>
            [new((Sys3Int)16, (Sys3Int)at.X, (Sys3Int)at.Y), new(Sys3Int.Zero, (Sys3Int)at.X, (Sys3Int)at.Y)]);

        /// <summary>A right click on the map, then the command menu's item (from the end if negative: -1 is the last).</summary>
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
            int choice = CancelMenus ? 0 : items[0].At.File switch
            {
                "COMMAND.ADV" => Command(items.Count),
                "Cﾃｷﾄｼ.ADV" => items.Select((item, i) => (item, i)).Single(x => x.item.At.Line is 64 or 70).i + 1,   // Attack, Pillage
                _ => 1,
            };
            return Task.FromResult(choice);
        }

        /// <summary>The next command menu item, counted from the end if negative.</summary>
        private int Command(int count)
        {
            int item = _commands.Dequeue();
            return item < 0 ? count + 1 + item : item;
        }

        public Task<Sys3Int> WaitForKeyAsync()
        {
            _rests = 0;
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
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
