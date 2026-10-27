using System.Collections.Concurrent;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Combat;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>
/// An attack banner's damage (the owner's A1 laid out as B2, docs/ui/damage-forecast.md, §8, §10): the figures of the
/// target's troops lost and its commander's HP lost, the usual 90 % of the runs (<see cref="ExchangeForecast"/>), each
/// null where the attack cannot reach it — the view leaves its place empty. No minus: the banner is the attack, its
/// figures the damage it deals; a range of one value is one figure ("60"), else "29–37".
/// </summary>
/// <param name="Troops">The troops' figure, or null.</param>
/// <param name="Hp">The HP's figure, or null.</param>
public sealed record BannerDamage(string? Troops, string? Hp)
{
    /// <summary>The troops' word before their figure, the cards' own.</summary>
    public const string TroopsWord = "Trp.";

    /// <summary>The HP's word before their figure.</summary>
    public const string HpWord = "HP";

    /// <summary>The figures of <paramref name="forecast"/>.</summary>
    public static BannerDamage Of(ExchangeForecast forecast) =>
        new(forecast.Troops is { } t ? Range(t) : null, forecast.Hp is { } h ? Range(h) : null);

    /// <summary>"29–37", "60", "1,250–2,400" (<see cref="Figures"/>, an en dash between).</summary>
    public static string Range(DamageSpread spread) => spread.Low == spread.High
        ? Figures.Number(spread.Low)
        : Figures.Number(spread.Low) + "–" + Figures.Number(spread.High);
}

/// <summary>
/// The damage forecasts of one <c>FIGT_SEL</c> (docs/ui/damage-forecast.md, §3, "When"): every banner of every enemy
/// card, each an <see cref="ExchangeForecast"/>. A forecast takes tens of milliseconds, so they run off the game's thread,
/// side by side, on a snapshot of the state taken at the start (the game goes on after the answer, the snapshot does
/// not): <see cref="For"/> is null until a banner's is done. The first target's banners come first (the touch mode's
/// preselected one). Option → Tweaks → "Damage forecast" (<c>BattleScreenSettings</c>), our interface only.
/// </summary>
public sealed class DamageForecasts
{
    private readonly ConcurrentDictionary<FigtSelChoice, BannerDamage> _done = new();

    private DamageForecasts(GameState snapshot, IReadOnlyList<FigtSelChoice> attacks, DemonReach reach)
    {
        Attacks = attacks;
        Done = Task.Run(() => Parallel.ForEach(
            Partitioner.Create(attacks, EnumerablePartitionerOptions.NoBuffering),
            choice => _done[choice] = BannerDamage.Of(ExchangeForecast.Of(snapshot, choice, reach))));
    }

    /// <summary>
    /// Starts the forecasts of <paramref name="commands"/>' banners in <paramref name="state"/>, the state while
    /// <c>FIGT_SEL</c> asks (copied here, on the caller's thread); <paramref name="first"/>'s banners first;
    /// <paramref name="reach"/> is the game's choice of BRS-24's fix (decision 32).
    /// </summary>
    public static DamageForecasts Start(GameState state, BattleCommands commands, BattlePlace? first = null,
        DemonReach reach = DemonReach.Original)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(commands);
        var targets = Enumerable.Range(1, 4).Select(slot => new BattlePlace(BattleSide.Enemy, slot))
            .OrderBy(place => place == first ? 0 : 1);
        var attacks = targets.SelectMany(commands.Banners).Select(b => b.Choice)
            .Where(c => ExchangeForecast.IsAttack(c.Command)).Distinct().ToList();
        return new DamageForecasts(ScriptState.Copy(state), attacks, reach);
    }

    /// <summary>The attacks forecast, in the order they are taken.</summary>
    public IReadOnlyList<FigtSelChoice> Attacks { get; }

    /// <summary>Done when every forecast is (faulted if one failed).</summary>
    public Task Done { get; }

    /// <summary>How many forecasts are done: a view redraws when it grows.</summary>
    public int Count => _done.Count;

    /// <summary>The figures of <paramref name="choice"/>'s banner, or null while its forecast runs (or for no attack).</summary>
    public BannerDamage? For(FigtSelChoice choice) => _done.TryGetValue(choice, out var damage) ? damage : null;
}
