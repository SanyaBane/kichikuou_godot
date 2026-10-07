using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.Sys3;

public class Sys3IntTests
{
    private static Sys3Int V(int value) => (Sys3Int)value;

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(65000, 535, 65535)]
    [InlineData(65000, 1000, 65535)]
    [InlineData(65535, 65535, 65535)]
    public void Addition_saturates_at_65535(int a, int b, int expected) =>
        Assert.Equal(V(expected), V(a) + V(b));

    [Theory]
    [InlineData(5, 3, 2)]
    [InlineData(3, 3, 0)]
    [InlineData(3, 5, 0)]
    [InlineData(0, 65535, 0)]
    public void Subtraction_floors_at_zero(int a, int b, int expected) =>
        Assert.Equal(V(expected), V(a) - V(b));

    [Theory]
    [InlineData(255, 257, 65535)]
    [InlineData(256, 256, 65535)]
    [InlineData(300, 200, 60000)]
    [InlineData(300, 300, 65535)]
    [InlineData(100, 8, 800)]
    public void Multiplication_saturates_at_65535(int a, int b, int expected) =>
        Assert.Equal(V(expected), V(a) * V(b));

    [Theory]
    [InlineData(7, 2, 3, 1)]
    [InlineData(7, 0, 0, 0)]
    [InlineData(0, 5, 0, 0)]
    public void Division_and_remainder_by_zero_give_zero(int a, int b, int quotient, int remainder)
    {
        Assert.Equal(V(quotient), V(a) / V(b));
        Assert.Equal(V(remainder), V(a) % V(b));
    }

    [Fact]
    public void Every_operation_saturates_separately()
    {
        // The script computes |a-b| as a-b + (b-a), relying on the floor at zero (M.ADV:47).
        Assert.Equal(V(4), V(3) - V(7) + (V(7) - V(3)));
        Assert.Equal(V(4), V(7) - V(3) + (V(3) - V(7)));
        // (a*b)/c saturates before dividing: 300*300 caps at 65535, then /100.
        Assert.Equal(V(655), V(300) * V(300) / V(100));
    }

    [Fact]
    public void Bitwise_operators()
    {
        Assert.Equal(V(0b1000), V(0b1100) & V(0b1010));
        Assert.Equal(V(0b1110), V(0b1100) | V(0b1010));
        Assert.Equal(V(0b0110), V(0b1100) ^ V(0b1010));
    }

    [Fact]
    public void Integer_literals_convert_implicitly()
    {
        Sys3Int troops = 1500;
        Assert.True(troops > 1000);
        Assert.Equal(V(1450), troops - 50);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Explicit_conversion_rejects_out_of_range_ints(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => (Sys3Int)value);
}
