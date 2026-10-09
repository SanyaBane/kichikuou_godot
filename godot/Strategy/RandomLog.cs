using Godot;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// <c>--rnglog</c>: every draw of the generator printed to the console (Rider's Console tab) as
/// <c>rnd k: Y 4, n = result</c>, k counting the draws of the launch — the same lines the patched
/// xsystem35 prints with <c>XSYS35_RNGLOG=1</c> (<c>tools/golden/capture.ps1 -RngLog</c>), there
/// followed by the script location, so a parallel run of both can be compared draw by draw.
/// <c>Y 4, 0</c> and <c>Y 4, 1</c> do not draw and are not printed.
/// </summary>
internal sealed class RandomLog(IScriptRandom inner) : IScriptRandom
{
    private long _draws;

    public Sys3Int Next(Sys3Int n)
    {
        var result = inner.Next(n);
        if (n.Value > 1)
            GD.Print($"rnd {++_draws}: Y 4, {n.Value} = {result.Value}");
        return result;
    }
}
