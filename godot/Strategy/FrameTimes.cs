using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The game's speed as the phone sees it (docs/ui/phone-build.md, §3, "The speed"; the engine surface is drawn per pixel
/// in C#): every <see cref="Period"/> seconds the console — Android's logcat, tag <c>godot</c> — gets the frames of each
/// kind of screen shown meanwhile (a scene, the map, a battle): how many, the mean, the 95th percentile and the longest
/// frame interval, and the processing of the slowest frame in each second (Godot's <c>TimeProcess</c>, which it updates
/// once a second: our code and the scripts' drawing, without the GPU), its median and its longest. On on Android and
/// with <c>--frame-times</c>; nothing is recorded else.
/// </summary>
internal static class FrameTimes
{
    private const double Period = 10;

    private static bool? _on;
    private static readonly Dictionary<string, (List<double> Frames, List<double> Process)> Kinds = [];
    private static double _since;

    /// <summary>Whether frame times are recorded.</summary>
    public static bool On => _on ??= OS.GetName() == "Android" || CommandLine.Args.Contains("--frame-times");

    /// <summary>A frame of <paramref name="delta"/> seconds while the screen showed <paramref name="kind"/>.</summary>
    public static void Record(string kind, double delta)
    {
        if (!On)
            return;
        if (!Kinds.TryGetValue(kind, out var times))
            Kinds[kind] = times = ([], []);
        times.Frames.Add(delta);
        times.Process.Add(Performance.GetMonitor(Performance.Monitor.TimeProcess));
        if ((_since += delta) < Period)
            return;
        var lines = Kinds.Select(k =>
        {
            var frames = k.Value.Frames.Order().ToList();
            var process = k.Value.Process.Order().ToList();
            double Ms(double seconds) => Math.Round(seconds * 1000, 1);
            return $"{k.Key} {frames.Count} frames, mean {Ms(frames.Average())} ms, p95 {Ms(frames[(int)(frames.Count * 0.95)])} ms, "
                + $"max {Ms(frames[^1])} ms, slowest processing a second: median {Ms(process[process.Count / 2])} ms, max {Ms(process[^1])} ms";
        });
        GD.Print($"Kichikuou: frame times over {_since:0.0} s: {string.Join("; ", lines)}.");
        Kinds.Clear();
        _since = 0;
    }
}
