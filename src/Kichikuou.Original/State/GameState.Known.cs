namespace Kichikuou.Original.State;

public sealed partial class GameState
{
    /// <summary>The key of <see cref="KnownProvinces"/> in <see cref="Engine.Script.ScriptState.Extras"/>.</summary>
    public const string KnownProvincesKey = "known-provinces";

    /// <summary>
    /// The provinces whose figures (Economy, Defense, Battle Area) the player has seen, ascending: ours at a
    /// map wait, or scouted by a ninja (<c>Ui_CityScouted</c>, which the original forgets once the city's
    /// file is left). Ours, kept with the saves, read only by our map's cards (docs/ui/command-screen.md,
    /// "The province card"); no rule reads it.
    /// </summary>
    public IReadOnlyList<int> KnownProvinces => Extras.GetValueOrDefault(KnownProvincesKey) ?? [];

    /// <summary>Whether the player has seen <paramref name="province"/>'s figures (<see cref="KnownProvinces"/>).</summary>
    public bool IsProvinceKnown(int province) =>
        Extras.TryGetValue(KnownProvincesKey, out var known) && Array.BinarySearch(known, province) >= 0;

    /// <summary>Adds <paramref name="province"/> to <see cref="KnownProvinces"/>.</summary>
    public void MarkProvinceKnown(int province)
    {
        var known = Extras.GetValueOrDefault(KnownProvincesKey) ?? [];
        int at = Array.BinarySearch(known, province);
        if (at >= 0)
            return;
        at = ~at;
        Extras[KnownProvincesKey] = [.. known[..at], province, .. known[at..]];
    }

    /// <summary>Adds every province of ours (<c>Owner</c> 1) to <see cref="KnownProvinces"/>.</summary>
    public void MarkOwnProvincesKnown()
    {
        for (int p = 1; p <= Prov_Count.Value; p++)
            if (Provs[p].Owner == 1)
                MarkProvinceKnown(p);
    }
}
