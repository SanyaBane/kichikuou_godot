using System.Globalization;

namespace Kichikuou.Original.Content;

/// <summary>What our interface writes between a number's thousands (Option → Display → Thousands).</summary>
public enum ThousandsSeparator
{
    /// <summary>"129,300" (the default).</summary>
    Comma,

    /// <summary>"129.300".</summary>
    Period,

    /// <summary>"129 300", a no-break space (none of the bundled fonts has a thin one).</summary>
    Space,

    /// <summary>"129'300".</summary>
    Apostrophe,

    /// <summary>"129300", as the original prints its numbers.</summary>
    None,
}

/// <summary>
/// How our own screens write gold, troops and damage, one standard for all (owner, 2026-10-23): the thousands
/// separated by <see cref="Separator"/>, a minus as U+2212, gold in K — "K GOLD" where the amount stands alone or in
/// a sentence (the budget, the week report, the questions), "K" in tables, buttons and cards — with no space
/// before the K; a large figure with a small "GOLD" after it takes the K in its own face (<see cref="GoldShort"/>).
/// The screens of the original engine keep its own way.
/// </summary>
public static class Figures
{
    /// <summary>The player's choice, set by the Godot game at the start and when Option changes it.</summary>
    public static ThousandsSeparator Separator { get; set; } = ThousandsSeparator.Comma;

    /// <summary>The minus our screens write.</summary>
    public const string Minus = "−";

    /// <summary>"129,300", "−50".</summary>
    public static string Number(int n) => Number(n, Separator);

    /// <summary><paramref name="n"/> with <paramref name="separator"/> between its thousands.</summary>
    public static string Number(int n, ThousandsSeparator separator)
    {
        string digits = Math.Abs((long)n).ToString("N0", CultureInfo.InvariantCulture);
        if (separator != ThousandsSeparator.Comma)
            digits = digits.Replace(",", separator switch
            {
                ThousandsSeparator.Period => ".",
                ThousandsSeparator.Space => " ",
                ThousandsSeparator.Apostrophe => "'",
                _ => "",
            });
        return n < 0 ? Minus + digits : digits;
    }

    /// <summary>"+1,191", "−50", "0".</summary>
    public static string Signed(int n) => n > 0 ? "+" + Number(n) : Number(n);

    /// <summary>An amount in K standing alone or in a sentence: "129,300K GOLD".</summary>
    public static string Gold(int k) => Number(k) + "K GOLD";

    /// <summary>An amount in K in a table, a button or a card: "2,300K".</summary>
    public static string GoldShort(int k) => Number(k) + "K";
}
