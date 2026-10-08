using Kichikuou.Core.Combat;
using Kichikuou.Core.Golden;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Battles;

/// <summary>What a golden capture holds, for a battle picker.</summary>
/// <param name="Source">The capture's file name.</param>
/// <param name="Root">The battle file (<c>FIGT_A.ADV</c> attack, <c>FIGT_B.ADV</c> defence, <c>FIGT.ADV</c> dungeon).</param>
/// <param name="Week">The game week.</param>
/// <param name="Province">The province's name.</param>
/// <param name="PlayerUnits">The player's party at entry (unit names).</param>
/// <param name="EnemyCommanders">The enemy party at entry (commander names).</param>
/// <param name="Hooks">The story scenes the battle called, in order.</param>
/// <param name="Outcome"><c>Bt_Result</c> at exit (1 defeat, 2 victory), or <c>null</c> for a jump.</param>
/// <param name="JumpTarget">The file the battle jumped to (game over), or <c>null</c>.</param>
public sealed record CaptureSummary(
    string Source,
    string Root,
    int Week,
    string Province,
    IReadOnlyList<string> PlayerUnits,
    IReadOnlyList<string> EnemyCommanders,
    IReadOnlyList<string> Hooks,
    int? Outcome,
    string? JumpTarget);

/// <summary>
/// Battles started from golden captures (<c>docs/golden-tests.md</c>) while there is no strategic
/// layer: a capture's entry memory is a ready battle. <see cref="RunLiveAsync"/> lets the player
/// fight it; the replay mode is <see cref="GoldenReplay.RunAsync"/> with a view.
/// </summary>
public static class BattleSandbox
{
    /// <summary>Reads the battle a capture holds.</summary>
    public static CaptureSummary Summarize(GoldenCapture capture)
    {
        var enter = Load(capture.Enter);
        var exit = Load(capture.Exit);
        string province = enter.Strings[(enter.Prov_StrBase + enter.Prov_Cur - 1).Value];
        Sys3Int[] players = [enter.Bt_PlayerParty1, enter.Bt_PlayerParty2, enter.Bt_PlayerParty3, enter.Bt_PlayerParty4];
        Sys3Int[] enemies = [enter.Bt_EnemyParty1, enter.Bt_EnemyParty2, enter.Bt_EnemyParty3, enter.Bt_EnemyParty4];
        return new CaptureSummary(
            Path.GetFileName(capture.Source),
            capture.Root,
            enter.Week.Value,
            province,
            players.Where(u => u != 0).Select(u => enter.Strings[(enter.Unit_StrBase + (u - 1) * 3).Value]).ToList(),
            enemies.Where(c => c != 0).Select(c => enter.Strings[(enter.Cmdr_StrBase + (c - 1) * 3).Value]).ToList(),
            capture.Records.OfType<HookRecord>().Select(h => h.File).ToList(),
            capture.JumpTarget is null ? exit.Bt_Result.Value : null,
            capture.JumpTarget);
    }

    /// <summary>
    /// Plays the battle of <paramref name="capture"/> with the player's input from its entry
    /// memory. The random generator starts from <paramref name="seed"/>, or from the capture's own
    /// state when it is <c>null</c> (the same choices then give the original's battle). Story
    /// scenes cannot run yet: one called before the first round header (deployment
    /// <c>Sﾌﾞﾀｲ</c>, pre-battle scenes) is replaced by its recorded outcome when the capture
    /// called the same scene there; any other is skipped and reported to
    /// <paramref name="onSceneSkipped"/>. <paramref name="started"/> gets the state before the
    /// battle begins. Returns the final state and the jump target (game over), if any.
    /// </summary>
    public static async Task<(GameState State, string? JumpTarget)> RunLiveAsync(
        GoldenCapture capture,
        IBattleView view,
        IBattleInput input,
        uint? seed = null,
        Func<string, Task>? onSceneSkipped = null,
        Action<GameState>? started = null)
    {
        var state = Load(capture.Enter);
        started?.Invoke(state);
        IScriptRandom random = seed is uint s ? new XSystem35Random(s) : new XSystem35Random(capture.Enter.CreateGenerator());
        var tracking = new RoundTrackingView(view);
        var hooks = new RecordedSceneHooks(capture, state, tracking, onSceneSkipped);
        var battle = new Battle(state, random, tracking, input, hooks);
        try
        {
            await Entry(battle, capture.Root)();
            return (state, null);
        }
        catch (ScenarioJumpException jump)
        {
            return (state, jump.File);
        }
    }

    /// <summary>The battle's entry point for a root file.</summary>
    public static Func<Task> Entry(Battle battle, string root) => root switch
    {
        "FIGT_A.ADV" => battle.AttackProvinceAsync,
        "FIGT_B.ADV" => battle.DefendProvinceAsync,
        "FIGT.ADV" => battle.FightAsync,
        _ => throw new ArgumentException($"No battle entry point for {root}.", nameof(root)),
    };

    private static GameState Load(MemoryDump dump)
    {
        var state = new GameState();
        dump.ApplyTo(state.Memory);
        return state;
    }

    /// <summary>Notes when the first round header is shown: the battle proper has begun.</summary>
    private sealed class RoundTrackingView(IBattleView inner) : IBattleView
    {
        public bool RoundsStarted { get; private set; }

        public Task ShowAsync(BattleEvent battleEvent)
        {
            if (battleEvent is RoundHeaderEvent)
                RoundsStarted = true;
            return inner.ShowAsync(battleEvent);
        }
    }

    private sealed class RecordedSceneHooks(
        GoldenCapture capture,
        GameState state,
        RoundTrackingView tracking,
        Func<string, Task>? onSceneSkipped) : IStoryHooks
    {
        private readonly Queue<HookRecord> _recorded = new(capture.Records.OfType<HookRecord>());

        public async Task RunEventAsync(string file, Sys3Int entry)
        {
            if (!tracking.RoundsStarted && _recorded.TryPeek(out var next) && next.File == file)
            {
                _recorded.Dequeue();
                next.Exit.ApplyTo(state.Memory);   // the recorded outcome; the live generator goes on
                return;
            }
            if (onSceneSkipped is not null)
                await onSceneSkipped(file);
        }
    }
}
