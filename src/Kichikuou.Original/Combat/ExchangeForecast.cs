using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Combat;

/// <summary>
/// One loss over a forecast's runs: the whole range (<see cref="Min"/>-<see cref="Max"/>) and the usual 90 % of it, the
/// 5th-95th percentile (<see cref="Low"/>-<see cref="High"/>).
/// </summary>
public readonly record struct DamageSpread(int Min, int Low, int High, int Max)
{
    /// <summary>The spread of <paramref name="values"/>, sorted in place (nearest rank).</summary>
    internal static DamageSpread Of(int[] values)
    {
        Array.Sort(values);
        int Rank(double p) => values[Math.Max(0, (int)Math.Ceiling(p * values.Length) - 1)];
        return new DamageSpread(values[0], Rank(0.05), Rank(0.95), values[^1]);
    }

    /// <summary>Whether <paramref name="value"/> lies in the whole range.</summary>
    public bool Holds(int value) => value >= Min && value <= Max;
}

/// <summary>
/// What a player's attack is likely to do to its target (ours, the owner's request of 2026-10-26; docs/ui/damage-forecast.md,
/// §3, §8, §10): the target's troops lost and its commander's HP lost, each over <see cref="Runs"/> runs of the real
/// exchange (<see cref="Battle.ComputeExchange"/>) on a copy of the state at <c>FIGT_SEL</c>, drawn from a generator of
/// the forecast's own with a fixed seed — the game's state and generator are never touched, every quirk, shield, clamp
/// and special is the exchange's own, and the same state gives the same numbers. A loss is the one
/// <c>ApplyExchange</c> makes (<c>FIGT.ADV:1873-1971</c>): the damage, no more than the troops or the HP the commander has.
/// A loss is null where the attack cannot reach it: in reach is a stream that shoots and that no demon shield zeroes,
/// decided from the exchange (<see cref="ReachesTroops"/>, <see cref="ReachesCommander"/>), not from the runs — a rare
/// hit is still a possible one —, and a loss some run took (a phantom hit of no shots, 02 Q3).
/// </summary>
/// <remarks>
/// Not forecast (docs/ui/damage-forecast.md, §4): Miki's (unit 32) turn into Vanish Bomb (<c>ManualAttackPrelude</c>'s
/// roll, 02 Q7), a ninja's follow-up after the exchange (02 §7.3), the Sieg event's Chaos (item 1) before it
/// (<c>FIGT_M.ADV:10090</c>).
/// </remarks>
/// <param name="Troops">The target's troops lost, or null where the attack cannot reach them.</param>
/// <param name="Hp">The target commander's HP lost, or null where the attack cannot reach him.</param>
public sealed record ExchangeForecast(DamageSpread? Troops, DamageSpread? Hp)
{
    /// <summary>The runs of one forecast.</summary>
    public const int Runs = 2000;

    /// <summary>The forecast generator's seed, the same for every forecast: the numbers never flicker.</summary>
    public const uint Seed = 20261026;

    /// <summary>Whether <paramref name="command"/> is an attack (<c>FIGT.ADV:645-707</c>: 1-3, 10-21), which a forecast is for.</summary>
    public static bool IsAttack(Sys3Int command) => command.Value is >= 1 and <= 3 or >= 10 and <= 21;

    /// <summary>
    /// The forecast of <paramref name="choice"/>, an attack <c>FIGT_SEL</c> offers in <paramref name="state"/> (the state
    /// while it asks): on a copy, the answer applied and its target bound as <c>FIGT_SEL</c> and <c>L_00ee3</c> do
    /// (<see cref="Battle.ApplyFigtSelChoice"/>, <see cref="Battle.BindTarget"/>), the command's setter run
    /// (<see cref="Battle.SetAttackCommand"/>; the Focus it spends is the copy's), the battle kind's modifiers copied as
    /// <c>PresentEngagement</c> copies them (<see cref="Battle.CopyBattleKindMods"/>), then <see cref="Runs"/> exchanges.
    /// The exchange modifies no record (02 §6.2), so the runs are independent draws on the one copy. The copy's battle runs
    /// with the player's <paramref name="reach"/> (decision 32, BRS-24's fix), as the game's does.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="choice"/> is not an attack on an enemy slot.</exception>
    public static ExchangeForecast Of(GameState state, FigtSelChoice choice, DemonReach reach = DemonReach.Original, int runs = Runs)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!IsAttack(choice.Command) || choice.Argument.Value is < 4 or > 7)
            throw new ArgumentException($"({choice.Command}, {choice.Argument}) is not an attack on an enemy slot.", nameof(choice));
        ArgumentOutOfRangeException.ThrowIfLessThan(runs, 1);

        var copy = ScriptState.Copy(state);
        var battle = new Battle(copy, new XSystem35Random(Seed), Unseen.Instance, Unseen.Instance, Unseen.Instance);
        battle.Script.DemonReach = reach;
        battle.ApplyFigtSelChoice(choice);
        battle.BindTarget();
        battle.SetAttackCommand();
        battle.CopyBattleKindMods();

        var target = copy.Cmdrs[copy.Cmdr_Cur];
        int troops = target.Troops.Value, hp = target.HP.Value;
        var troopsLost = new int[runs];
        var hpLost = new int[runs];
        bool reachesTroops = false, reachesCommander = false;
        for (int i = 0; i < runs; i++)
        {
            battle.ComputeExchange();
            troopsLost[i] = Math.Min(troops, copy.Bt_PTroopVsTroopDmg.Value + copy.Bt_PCmdrVsTroopDmg.Value);
            hpLost[i] = Math.Min(hp, copy.Bt_PTroopVsCmdrDmg.Value + copy.Bt_PCmdrVsCmdrDmg.Value);
            // QUIRK: 02 Q3 — a stream of no shots still rolls once (troops on troops spread E = 0 to 1): a loss taken
            // in a run is in reach whatever the shots.
            reachesTroops |= ReachesTroops(copy) || troopsLost[i] > 0;
            reachesCommander |= ReachesCommander(copy) || hpLost[i] > 0;
        }

        return new ExchangeForecast(
            reachesTroops && troops > 0 ? DamageSpread.Of(troopsLost) : null,
            reachesCommander ? DamageSpread.Of(hpLost) : null);
    }

    /// <summary>
    /// After an exchange (<see cref="Battle.ComputeExchange"/>): whether one of the player's streams on the enemy's troops
    /// shoots (troops on troops, the commander on troops).
    /// </summary>
    private static bool ReachesTroops(GameState state) =>
        state.Bt_PTroopVsTroopShots != 0 || state.Bt_PCmdrVsTroopShots != 0;

    /// <summary>
    /// After an exchange: whether one of the player's streams on the enemy commander shoots and no demon shield zeroes
    /// its damage (<c>FIGT.ADV:2414-2448</c>: shield 1 takes both streams', shield 2 the troops' alone; 02 §6.2).
    /// </summary>
    private static bool ReachesCommander(GameState state) =>
        (state.Bt_PTroopVsCmdrShots != 0 && state.Bt_EnemyCmdrShield == 0)
        || (state.Bt_PCmdrVsCmdrShots != 0 && state.Bt_EnemyCmdrShield != 1);

    /// <summary>The forecast's view, input and hooks: the exchange shows nothing and asks nothing, so none is ever called.</summary>
    private sealed class Unseen : IScriptView, IBattleInput, IStoryHooks
    {
        public static readonly Unseen Instance = new();

        private static InvalidOperationException Called(string what) =>
            new($"The damage forecast's exchange reached {what}, which it never runs.");

        public Task ShowAsync(ScriptEvent scriptEvent) => throw Called(scriptEvent.GetType().Name);

        public Task<Sys3Int> WaitForKeyAsync() => throw Called("a key wait");

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => throw Called("a pause");

        public Task<Sys3Int> GetPlayingTrackAsync() => throw Called("the music");

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu) => throw Called("FIGT_SEL");

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu) => throw Called("a menu");

        public Task RunEventAsync(string file, Sys3Int entry) => throw Called(file);
    }
}
