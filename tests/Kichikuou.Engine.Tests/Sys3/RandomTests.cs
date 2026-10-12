using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Tests.Sys3;

/// <summary>
/// Reference values were produced by compiling xsystem35's own <c>mt19937-1.c</c> with gcc
/// and printing <c>genrand()</c> and the <c>Y 4, n</c> formula from <c>cmdy.c</c>.
/// </summary>
public class RandomTests
{
    [Fact]
    public void Generator_matches_reference_code_for_seed_4357()
    {
        var mt = new Mt19937Legacy(4357);
        double[] expected =
        [
            0.81733006001853614, 0.99906089971751466, 0.51035437255873217,
            0.13153290984489324, 0.035416348379900757,
        ];
        foreach (double value in expected)
            Assert.Equal(value, mt.NextDouble());
    }

    [Fact]
    public void Generator_matches_reference_code_after_a_twist()
    {
        var mt = new Mt19937Legacy(4357);
        for (int i = 0; i < 999; i++)
            mt.NextDouble();
        Assert.Equal(0.30481705984678514, mt.NextDouble());
    }

    [Fact]
    public void Y4_matches_reference_code_for_seed_1234()
    {
        var random = new XSystem35Random(1234);
        int[] expected = [76, 75, 30, 30, 100, 22, 30, 57, 22, 56];
        foreach (int value in expected)
            Assert.Equal((Sys3Int)value, random.Next(100));
    }

    [Fact]
    public void Y4_with_n_of_0_or_1_returns_n_without_drawing()
    {
        var random = new XSystem35Random(1234);
        for (int i = 0; i < 10; i++)
            random.Next(100);

        // Reference: n = 2, 6, 65535, 0, 1 after ten draws gives 1, 5, 32299, 0, 1.
        Assert.Equal((Sys3Int)1, random.Next(2));
        Assert.Equal((Sys3Int)5, random.Next(6));
        Assert.Equal((Sys3Int)32299, random.Next(65535));
        Assert.Equal((Sys3Int)0, random.Next(0));
        Assert.Equal((Sys3Int)1, random.Next(1));

        // If n = 0 or 1 had consumed a draw, the next value would differ from the reference
        // sequence continued with a fresh generator.
        var reference = new XSystem35Random(1234);
        for (int i = 0; i < 13; i++)
            reference.Next(100);
        Assert.Equal(reference.Next(100), random.Next(100));
    }
}
