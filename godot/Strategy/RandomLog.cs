using Godot;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// <c>--rnglog</c>: every draw of the generator printed to the console (Rider's Console tab) and
/// written to <c>user://rnglog.txt</c> (emptied at start) as <c>rnd k: Y 4, n = result</c>, k
/// counting the draws of the launch — the same lines the patched xsystem35 prints with
/// <c>tools/golden/capture.ps1 -RngLog</c> (there followed by the script location, into
/// <c>rnglog-original.txt</c> beside ours), so <c>tools/golden/rngwatch.py</c> can compare a
/// parallel run of both draw by draw. <c>Y 4, 0</c> and <c>Y 4, 1</c> do not draw and are not logged.
/// </summary>
internal sealed class RandomLog : IScriptRandom
{
    private readonly IScriptRandom _inner;
    private readonly System.IO.StreamWriter? _file;
    private long _draws;

    public RandomLog(IScriptRandom inner)
    {
        _inner = inner;
        string path = ProjectSettings.GlobalizePath("user://rnglog.txt");
        try
        {
            _file = new System.IO.StreamWriter(path, append: false) { AutoFlush = true, NewLine = "\n" };
            GD.Print("Kichikuou: random draws are logged to " + path);
        }
        catch (System.IO.IOException e)
        {
            GD.PushWarning($"Kichikuou: cannot write {path}: {e.Message}");
        }
    }

    public Sys3Int Next(Sys3Int n)
    {
        var result = _inner.Next(n);
        if (n.Value > 1)
        {
            string line = $"rnd {++_draws}: Y 4, {n.Value} = {result.Value}";
            GD.Print(line);
            _file?.WriteLine(line);
        }
        return result;
    }
}
