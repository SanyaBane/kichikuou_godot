namespace Kichikuou.Engine.Sys3;

/// <summary>
/// <c>Y 4, n:</c> exactly as xsystem35 implements it (<c>cmdy.c</c>):
/// <c>(int)(genrand() * n) + 1</c> stored into a 16-bit variable.
/// </summary>
/// <remarks>
/// Because <c>genrand()</c> can return exactly 1.0, the result is n + 1 in rare cases, and
/// for n = 65535 that wraps to 0. Both quirks are reproduced on purpose.
/// </remarks>
public sealed class XSystem35Random(Mt19937Legacy generator) : IScriptRandom
{
    /// <summary>Creates a generator seeded like xsystem35's <c>sgenrand(seed)</c>.</summary>
    public XSystem35Random(uint seed) : this(new Mt19937Legacy(seed))
    {
    }

    /// <inheritdoc />
    public Sys3Int Next(Sys3Int n)
    {
        if (n.Value <= 1)
            return n;
        return new Sys3Int(unchecked((ushort)((int)(generator.NextDouble() * n.Value) + 1)));
    }
}
