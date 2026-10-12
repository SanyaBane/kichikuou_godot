using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.TestSupport;

/// <summary>A code path that draws nothing: a draw is a divergence.</summary>
public sealed class NoRandom : IScriptRandom
{
    public Sys3Int Next(Sys3Int n) => throw new InvalidOperationException($"Unexpected draw Y 4, {n}.");
}

/// <summary>
/// Draws from a generator of <paramref name="generatorSeed"/>, logged with their range:
/// <c>Y 4, n</c> with n of 0 or 1 gives n without drawing, as the engine's.
/// </summary>
public sealed class LoggingRandom(int generatorSeed, List<string> log) : IScriptRandom
{
    private readonly Random _rng = new(generatorSeed);

    public Sys3Int Next(Sys3Int n)
    {
        var value = n.Value <= 1 ? n : (Sys3Int)_rng.Next(1, n.Value + 1);
        log.Add($"rnd {n} {value}");
        return value;
    }
}

/// <summary>The building blocks of a differential test's random states, drawn in a fixed order.</summary>
public static class RandomStates
{
    /// <summary>
    /// Every variable not bound to a page: 0 with probability <paramref name="zero"/>, otherwise
    /// mostly a small number, now and then up to 3000.
    /// </summary>
    public static void Variables(ScriptState state, Random rng, double zero)
    {
        for (int v = 0; v < state.Profile.VariableCount; v++)
            if (!state.IsBound(v))
                state.Memory.Var(v) = (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(5) == 0 ? rng.Next(0, 3000) : rng.Next(0, 12));
    }

    /// <summary>Every string variable named after its number (<c>s1</c>, <c>s2</c> …), so a log shows which one a message used.</summary>
    public static void NameStrings(ScriptState state)
    {
        for (int s = 0; s < state.Strings.Length; s++)
            state.Strings[s] = $"s{s}";
    }
}
