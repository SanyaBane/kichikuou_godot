namespace Kichikuou.Core.Sys3;

/// <summary>
/// Exact port of the 1998 Mersenne Twister reference code <c>mt19937-1.c</c> used by
/// xsystem35: Knuth multiplicative seeding (<c>mt[i] = 69069 * mt[i-1]</c>) and reals on the
/// closed interval [0, 1].
/// </summary>
/// <remarks>
/// Not the same as later MT19937 versions (<c>init_genrand</c>, <c>genrand_real2</c>): seeding
/// and the real conversion differ, so sequences would not match xsystem35.
/// </remarks>
public sealed class Mt19937Legacy
{
    private const int N = 624;
    private const int M = 397;
    private const uint MatrixA = 0x9908b0df;
    private const uint UpperMask = 0x80000000;
    private const uint LowerMask = 0x7fffffff;

    /// <summary>1 / (2^32 - 1): maps a 32-bit output onto [0, 1] inclusive.</summary>
    private const double RealScale = 2.3283064370807974e-10;

    private readonly uint[] _mt = new uint[N];
    private int _mti;

    /// <summary>Number of words in the state vector (<c>N</c>).</summary>
    public const int StateSize = N;

    /// <summary>Creates a generator seeded like <c>sgenrand(seed)</c>.</summary>
    public Mt19937Legacy(uint seed) => Seed(seed);

    private Mt19937Legacy()
    {
    }

    /// <summary>
    /// Creates a generator in a recorded state: the state vector <c>mt[0..623]</c> and the index
    /// <c>mti</c> of <c>mt19937-1.c</c> (0..624, or 625 = never seeded).
    /// </summary>
    public static Mt19937Legacy FromState(ReadOnlySpan<uint> state, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(state.Length, N);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, N + 1);
        var generator = new Mt19937Legacy();
        state.CopyTo(generator._mt);
        generator._mti = index;
        return generator;
    }

    /// <summary>The index <c>mti</c> into the state vector.</summary>
    public int StateIndex => _mti;

    /// <summary>A copy of the state vector <c>mt[0..623]</c>.</summary>
    public uint[] CopyState() => (uint[])_mt.Clone();

    /// <summary>Reseeds, equivalent to <c>sgenrand(seed)</c>.</summary>
    public void Seed(uint seed)
    {
        _mt[0] = seed;
        for (_mti = 1; _mti < N; _mti++)
            _mt[_mti] = unchecked(69069 * _mt[_mti - 1]);
    }

    /// <summary>The next tempered 32-bit output.</summary>
    public uint NextUInt32()
    {
        if (_mti == N + 1)
            Seed(4357);   // genrand() before any sgenrand() uses the default seed
        if (_mti >= N)
            Twist();

        uint y = _mt[_mti++];
        y ^= y >> 11;
        y ^= (y << 7) & 0x9d2c5680;
        y ^= (y << 15) & 0xefc60000;
        y ^= y >> 18;
        return y;
    }

    /// <summary>The next real on [0, 1] inclusive, equivalent to <c>genrand()</c>.</summary>
    public double NextDouble() => NextUInt32() * RealScale;

    private void Twist()
    {
        int kk;
        for (kk = 0; kk < N - M; kk++)
            _mt[kk] = _mt[kk + M] ^ Mix(_mt[kk], _mt[kk + 1]);
        for (; kk < N - 1; kk++)
            _mt[kk] = _mt[kk + (M - N)] ^ Mix(_mt[kk], _mt[kk + 1]);
        _mt[N - 1] = _mt[M - 1] ^ Mix(_mt[N - 1], _mt[0]);
        _mti = 0;
    }

    private static uint Mix(uint current, uint next)
    {
        uint y = (current & UpperMask) | (next & LowerMask);
        return (y >> 1) ^ ((y & 1) != 0 ? MatrixA : 0u);
    }
}
