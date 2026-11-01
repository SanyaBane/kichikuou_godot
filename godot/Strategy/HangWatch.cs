using System.Diagnostics;
using Godot;
using Kichikuou.Engine.Diagnostics;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The frame watch of this launch (<see cref="FrameWatch"/>, docs/hang-watch.md): on Android and with <c>--hang-watch</c>,
/// nothing else. A report goes into the app's external folder on Android (<see cref="GameData.AndroidHangReports"/>; a PC
/// reaches it over USB, <c>tools/android-logs.ps1</c>), else into <c>user://hang-reports</c>. <c>--hang-test N</c> and
/// <c>--hang-spin N</c> stop the main thread N seconds once, two seconds into the launch, asleep or busy, to see a report;
/// the same words in a file <c>test.txt</c> in the reports' folder do it once, where a launch's parameters do not reach the
/// game (Android: the Poco's launch intent arrives without them).
/// </summary>
internal static class HangWatch
{
    private const int TestFrame = 120;

    private static FrameWatch? _watch;
    private static (double Seconds, bool Spin)? _test;
    private static int _frames;

    /// <summary>Starts the watch, once a launch (the screens come and go, the watch's nodes stay on the root).</summary>
    public static void Start(SceneTree tree)
    {
        var args = OS.GetCmdlineUserArgs();
        if (_watch is not null || (OS.GetName() != "Android" && !args.Contains("--hang-watch")))
            return;
        string folder = OS.GetName() == "Android" ? GameData.AndroidHangReports : ProjectSettings.GlobalizePath("user://hang-reports");
        var clock = Stopwatch.StartNew();
        var watch = new FrameWatch(() => OpenReport(folder), () => clock.Elapsed.TotalSeconds);
        _watch = watch;
        Directory.CreateDirectory(folder);                              // the app's own, so that it can remove a test.txt put there
        _test = TestOf(args, Path.Combine(folder, "test.txt"));
        var first = new FrameBeat { Watch = watch, First = true, ProcessPriority = int.MinValue, ProcessMode = Node.ProcessModeEnum.Always };
        var last = new FrameBeat { Watch = watch, ProcessPriority = int.MaxValue, ProcessMode = Node.ProcessModeEnum.Always };
        tree.Root.CallDeferred(Node.MethodName.AddChild, first);       // the tree is busy during _Ready
        tree.Root.CallDeferred(Node.MethodName.AddChild, last);
        RenderingServer.FramePreDraw += () => watch.At("draw");
        RenderingServer.FramePostDraw += () => watch.At("drawn");
        watch.Start(e => GD.PushError($"The frame watch could not write its report: {e.Message}"));
        GD.Print($"Kichikuou: the frame watch is on, its reports in {folder}.");
    }

    /// <summary>The main thread is at <paramref name="place"/> in its frame.</summary>
    public static void At(string place) => _watch?.At(place);

    /// <summary>What the game has just done, kept for a report.</summary>
    public static void Note(object note) => _watch?.Note(note);

    /// <summary>The game's state for a report, read from the watch's thread.</summary>
    public static void Describe(Func<string>? describe)
    {
        if (_watch is { } watch)
            watch.Describe = describe;
    }

    /// <summary>A frame has begun: <c>--hang-test</c> and <c>--hang-spin</c> act at their frame.</summary>
    public static void TestWhenDue()
    {
        if (_test is not { } test || ++_frames != TestFrame)
            return;
        GD.Print($"Kichikuou: the main thread stops {test.Seconds} s ({(test.Spin ? "busy" : "asleep")}), for the frame watch.");
        if (test.Spin)
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed.TotalSeconds < test.Seconds)
            {
            }
        }
        else
        {
            Thread.Sleep(TimeSpan.FromSeconds(test.Seconds));
        }
    }

    private static (double Seconds, bool Spin)? TestOf(string[] args, string file)
    {
        if (File.Exists(file))
        {
            args = File.ReadAllText(file).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            File.Delete(file);
        }
        double? Seconds(string name) =>
            Array.IndexOf(args, name) is int at and >= 0 && at + 1 < args.Length && double.TryParse(args[at + 1], out double seconds) ? seconds : null;
        return Seconds("--hang-spin") is { } spin ? (spin, true) : Seconds("--hang-test") is { } sleep ? (sleep, false) : null;
    }

    private static StreamWriter OpenReport(string folder)
    {
        return new StreamWriter(Path.Combine(folder, $"hang-{DateTime.Now:yyyyMMdd-HHmmss}.txt"));
    }
}
