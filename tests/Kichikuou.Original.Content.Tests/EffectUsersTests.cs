using Xunit.Abstractions;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Original.Content.Tests;

/// <summary>Debug → Effects' lists of the units that play a plain attack's effect (<see cref="EffectUsers"/>).</summary>
public class EffectUsersTests(ITestOutputHelper output)
{
    /// <summary>
    /// In the booted game (<c>SET_CHR</c>) Maria (unit 16) and Isoroku (unit 27) shoot (Archers), Melfeis (unit 13) casts
    /// ice, Rance (unit 1) strikes; every named unit is in exactly one list.
    /// </summary>
    [ScenarioFact]
    public async Task Each_unit_is_listed_under_its_attack()
    {
        var s = (await EndGame.BootAsync()).State;
        var bow = EffectUsers.Of(s, ExchangeEffectKind.Bow).Select(u => u.Unit).ToList();
        Assert.Contains(16, bow);
        Assert.Contains(27, bow);
        Assert.Contains(13, EffectUsers.Of(s, ExchangeEffectKind.IceMagic).Select(u => u.Unit));
        Assert.Contains(1, EffectUsers.Of(s, ExchangeEffectKind.Melee).Select(u => u.Unit));
        var all = Enum.GetValues<ExchangeEffectKind>().SelectMany(k => EffectUsers.Of(s, k)).Select(u => u.Unit).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
        foreach (var kind in Enum.GetValues<ExchangeEffectKind>())
        {
            var users = EffectUsers.Of(s, kind);
            if (users.Count > 0)
                output.WriteLine($"{kind}: {string.Join("; ", users.Select(u => u.Label))}");
        }
    }
}
