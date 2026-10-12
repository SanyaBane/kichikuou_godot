namespace Kichikuou.Engine.Sys3;

/// <summary>
/// A 16-bit unsigned value with System 3.x script arithmetic, as implemented by xsystem35
/// (<c>cali.c</c> for expressions, <c>cmd_check.c</c> for compound assignments): every
/// operation is evaluated on 16-bit values, <c>+</c> and <c>*</c> saturate at 65535,
/// <c>-</c> floors at 0, and <c>/</c> or <c>%</c> by zero give 0.
/// </summary>
/// <remarks>
/// Ported rules rely on these semantics (e.g. the script writes <c>|a-b|</c> as
/// <c>a-b + (b-a)</c>), so rule code keeps script values in this type instead of <c>int</c>.
/// There is deliberately no implicit conversion to <c>int</c>: mixing it with ordinary integer
/// arithmetic would silently change the results.
/// </remarks>
public readonly record struct Sys3Int(ushort Value) : IComparable<Sys3Int>
{
    /// <summary>The largest value, where addition and multiplication saturate.</summary>
    public const ushort MaxValue = ushort.MaxValue;

    /// <summary>Zero, the floor of subtraction.</summary>
    public static readonly Sys3Int Zero = new(0);

    /// <summary>Converts a non-negative 16-bit value; integer literals convert implicitly.</summary>
    public static implicit operator Sys3Int(ushort value) => new(value);

    /// <summary>Converts an <c>int</c>; throws when it is outside 0..65535.</summary>
    public static explicit operator Sys3Int(int value) =>
        value is >= 0 and <= MaxValue
            ? new Sys3Int((ushort)value)
            : throw new ArgumentOutOfRangeException(nameof(value), value, "System 3 values are 0..65535.");

    /// <summary>Widens to <c>int</c> for display or indexing.</summary>
    public static explicit operator int(Sys3Int value) => value.Value;

    /// <summary>Adds, saturating at 65535.</summary>
    public static Sys3Int operator +(Sys3Int a, Sys3Int b) => Saturate(a.Value + b.Value);

    /// <summary>Subtracts, flooring at 0.</summary>
    public static Sys3Int operator -(Sys3Int a, Sys3Int b) => a.Value > b.Value ? new((ushort)(a.Value - b.Value)) : Zero;

    /// <summary>Multiplies, saturating at 65535.</summary>
    public static Sys3Int operator *(Sys3Int a, Sys3Int b) => Saturate(a.Value * b.Value);

    /// <summary>Divides; division by zero gives 0.</summary>
    public static Sys3Int operator /(Sys3Int a, Sys3Int b) => b.Value == 0 ? Zero : new((ushort)(a.Value / b.Value));

    /// <summary>Remainder; by zero gives 0.</summary>
    public static Sys3Int operator %(Sys3Int a, Sys3Int b) => b.Value == 0 ? Zero : new((ushort)(a.Value % b.Value));

    /// <summary>Bitwise and.</summary>
    public static Sys3Int operator &(Sys3Int a, Sys3Int b) => new((ushort)(a.Value & b.Value));

    /// <summary>Bitwise or.</summary>
    public static Sys3Int operator |(Sys3Int a, Sys3Int b) => new((ushort)(a.Value | b.Value));

    /// <summary>Bitwise exclusive or.</summary>
    public static Sys3Int operator ^(Sys3Int a, Sys3Int b) => new((ushort)(a.Value ^ b.Value));

    /// <summary>Less than.</summary>
    public static bool operator <(Sys3Int a, Sys3Int b) => a.Value < b.Value;

    /// <summary>Greater than.</summary>
    public static bool operator >(Sys3Int a, Sys3Int b) => a.Value > b.Value;

    /// <summary>Less than or equal.</summary>
    public static bool operator <=(Sys3Int a, Sys3Int b) => a.Value <= b.Value;

    /// <summary>Greater than or equal.</summary>
    public static bool operator >=(Sys3Int a, Sys3Int b) => a.Value >= b.Value;

    /// <inheritdoc />
    public int CompareTo(Sys3Int other) => Value.CompareTo(other.Value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();

    private static Sys3Int Saturate(int result) => new((ushort)Math.Min(result, MaxValue));
}
