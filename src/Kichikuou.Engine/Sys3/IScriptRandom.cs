namespace Kichikuou.Engine.Sys3;

/// <summary>
/// Source of the script's random numbers: the <c>Y 4, n:</c> command. Rule code draws through
/// this interface so that golden tests can replay draws recorded from the original game; the
/// order and number of draws must match the script, including draws whose result is discarded.
/// </summary>
public interface IScriptRandom
{
    /// <summary>
    /// <c>Y 4, n:</c> — a random value in 1..n; for n of 0 or 1 it returns n without drawing.
    /// </summary>
    Sys3Int Next(Sys3Int n);
}
