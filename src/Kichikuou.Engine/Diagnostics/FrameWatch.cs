namespace Kichikuou.Engine.Diagnostics;

/// <summary>
/// Watches from a thread of its own that the main thread still comes round to a new frame (docs/hang-watch.md). The main
/// thread only writes: <see cref="Beat"/> each frame, <see cref="At"/> where the frame is, <see cref="Note"/> what the game
/// did last, all without a lock and for next to nothing. When no frame came for 8 s the watch opens a report and writes
/// there what those last said and what the process's threads are doing, again at 10, 15, 60 and 300 s, each flushed at once:
/// a frozen game is closed by its player, and the file must be whole by then. A frame coming again closes the report with
/// how long it took. <see cref="Pause"/> marks the frames stopping on purpose, the application in the background; nothing
/// counts before the first frame, since the boot takes its time in the frame before it (~6 s on the Poco).
/// A watch that writes nothing while the game is frozen says something too: every managed thread is stopped, as by the
/// garbage collector.
/// </summary>
public sealed class FrameWatch
{
    private const int PlaceCount = 48, NoteCount = 64, CheckMilliseconds = 500;

    /// <summary>The samples of a report, in seconds after the last frame; the first opens it.</summary>
    private static readonly double[] SampleSeconds = [8, 10, 15, 60, 300];

    private readonly Func<TextWriter> _openReport;
    private readonly Func<double> _clock;
    private readonly double _started;
    private readonly Trail _places = new(PlaceCount), _notes = new(NoteCount);
    private double _lastBeat;
    private long _beats;
    private bool _paused;

    // The watch's own thread alone reads and writes these.
    private TextWriter? _report;
    private long _reportBeats;
    private double _reportFrom;
    private int _nextSample;
    private IReadOnlyList<ThreadSample> _lastThreads = [];

    /// <param name="openReport">A new report's writer, each stall its own.</param>
    /// <param name="clock">The time in seconds, from any start.</param>
    public FrameWatch(Func<TextWriter> openReport, Func<double> clock)
    {
        _openReport = openReport;
        _clock = clock;
        _started = _lastBeat = clock();
    }

    /// <summary>The game's state in a line, read from the watch's thread while the main thread stands.</summary>
    public Func<string>? Describe { get; set; }

    /// <summary>The threads now (<see cref="ProcThreads.Read()"/>; the tests' own).</summary>
    public Func<IReadOnlyList<ThreadSample>> Threads { get; init; } = ProcThreads.Read;

    /// <summary>The process's memory now (<see cref="ProcThreads.Memory()"/>; the tests' own).</summary>
    public Func<string> Memory { get; init; } = ProcThreads.Memory;

    /// <summary>A new frame has begun (the main thread).</summary>
    public void Beat()
    {
        double now = _clock();
        Volatile.Write(ref _lastBeat, now);
        Volatile.Write(ref _beats, _beats + 1);
        _places.Add("frame", now);
    }

    /// <summary>The main thread is at <paramref name="place"/> in its frame.</summary>
    public void At(string place) => _places.Add(place, _clock());

    /// <summary>What the game has just done: kept as it is and turned into text only for a report, so a record costs nothing.</summary>
    public void Note(object note) => _notes.Add(note, _clock());

    /// <summary>The frames stop on purpose until <see cref="Resume"/> (the main thread, which is alive to say so).</summary>
    public void Pause() => Volatile.Write(ref _paused, true);

    /// <summary>The frames go on after <see cref="Pause"/>.</summary>
    public void Resume()
    {
        Volatile.Write(ref _lastBeat, _clock());
        Volatile.Write(ref _paused, false);
    }

    /// <summary>Starts the watch's thread, which <see cref="Check"/>s twice a second; <paramref name="failed"/> hears of a report it could not write.</summary>
    public void Start(Action<Exception>? failed = null)
    {
        var thread = new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(CheckMilliseconds);
                try
                {
                    Check();
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    failed?.Invoke(e);
                }
            }
        })
        {
            IsBackground = true,
            Name = "Frame watch",
        };
        thread.Start();
    }

    /// <summary>One look at the frames (the watch's thread; the tests call it themselves).</summary>
    public void Check()
    {
        long beats = Volatile.Read(ref _beats);
        double now = _clock(), lastBeat = Volatile.Read(ref _lastBeat);
        bool paused = Volatile.Read(ref _paused);
        if (_report is not null && (beats != _reportBeats || paused))
        {
            _report.WriteLine(FormattableString.Invariant($"the main thread again after {now - _reportFrom:F1} s"));
            _report.Dispose();
            _report = null;
        }
        double since = now - lastBeat;
        if (_report is null)
        {
            if (paused || beats == 0 || since < SampleSeconds[0])
                return;
            Open(beats, now, lastBeat);
        }
        if (_nextSample < SampleSeconds.Length && since >= SampleSeconds[_nextSample])
        {
            Sample(since);
            _nextSample++;
        }
    }

    private void Open(long beats, double now, double lastBeat)
    {
        _report = _openReport();
        (_reportBeats, _reportFrom, _nextSample, _lastThreads) = (beats, lastBeat, 0, []);
        _report.WriteLine(FormattableString.Invariant(
            $"no frame for {now - lastBeat:F1} s: frame {beats}, {now - _started:F1} s after the start, {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
        _report.Flush();
        _report.WriteLine("game: " + Text(() => Describe?.Invoke() ?? ""));
        WriteTrail("where the frames were, the seconds before now:", _places, now);
        WriteTrail("what the game did last:", _notes, now);
        _report.Flush();
    }

    private void WriteTrail(string title, Trail trail, double now)
    {
        _report!.WriteLine(title);
        foreach (var (time, item) in trail.Items())
            _report.WriteLine(FormattableString.Invariant($"  {time - now,9:F3}  {Text(() => item.ToString() ?? "")}"));
    }

    private void Sample(double since)
    {
        var report = _report!;
        var threads = Threads();
        report.WriteLine(FormattableString.Invariant(
            $"threads {since:F1} s after the last frame (id, name, state, CPU ticks at 100 a second, those since the last sample, wait, call):"));
        foreach (var (thread, spent) in threads.Select(t => (t, Spent(t))).OrderByDescending(t => t.Item2).ThenBy(t => t.t.Id))
            report.WriteLine(FormattableString.Invariant($"  {thread.Id,6} {thread.Name,-16} {thread.State} {thread.CpuTicks,7} +{spent,-5} {thread.Wait} [{thread.Syscall}]"));
        _lastThreads = threads;
        report.WriteLine("memory: " + Memory());
        report.Flush();
        // Last and flushed apart: where the collector holds its lock, this may be as far as the report gets.
        report.WriteLine(FormattableString.Invariant(
            $"collections: {GC.CollectionCount(0)}, {GC.CollectionCount(1)}, {GC.CollectionCount(2)}; managed memory {GC.GetTotalMemory(false) / (1024 * 1024)} MB"));
        report.Flush();
    }

    /// <summary>The ticks <paramref name="thread"/> spent since the last sample, all of them in a report's first.</summary>
    private long Spent(ThreadSample thread) =>
        thread.CpuTicks - (_lastThreads.FirstOrDefault(t => t.Id == thread.Id)?.CpuTicks ?? 0);

    /// <summary>What <paramref name="make"/> says, or its failure: the state it reads may be changing under it, and the report goes on.</summary>
    private static string Text(Func<string> make)
    {
        try
        {
            return make();
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            return $"({e.GetType().Name}: {e.Message})";
        }
    }

    /// <summary>The last things one thread wrote and when, for another to read when that one stands: no lock, torn reads harmless.</summary>
    private sealed class Trail(int size)
    {
        private readonly object?[] _items = new object?[size];
        private readonly double[] _times = new double[size];
        private int _next;

        public void Add(object item, double time)
        {
            int at = _next;
            _items[at] = item;
            _times[at] = time;
            Volatile.Write(ref _next, (at + 1) % size);
        }

        /// <summary>Oldest first.</summary>
        public List<(double Time, object Item)> Items()
        {
            int next = Volatile.Read(ref _next);
            var items = new List<(double, object)>(size);
            for (int i = 0; i < size; i++)
            {
                int at = (next + i) % size;
                if (_items[at] is { } item)
                    items.Add((_times[at], item));
            }
            return items;
        }
    }
}
