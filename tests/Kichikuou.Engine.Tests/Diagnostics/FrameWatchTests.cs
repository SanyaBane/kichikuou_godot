using Kichikuou.Engine.Diagnostics;

namespace Kichikuou.Engine.Tests.Diagnostics;

/// <summary>The frame watch (docs/hang-watch.md): a report when the frames stop, none when they stop on purpose.</summary>
public class FrameWatchTests
{
    private sealed class Rig
    {
        public double Now = 100;
        public readonly List<StringWriter> Reports = [];
        public IReadOnlyList<ThreadSample> Threads = [];
        public readonly FrameWatch Watch;

        public Rig()
        {
            Watch = new FrameWatch(() =>
            {
                var report = new StringWriter();
                Reports.Add(report);
                return report;
            }, () => Now)
            {
                Threads = () => Threads,
                Memory = () => "VmRSS: 10 kB",
            };
            Watch.Beat();
        }

        /// <summary><paramref name="seconds"/> pass, looked at twice a second, without a frame.</summary>
        public void Stand(double seconds)
        {
            for (double left = seconds; left > 0; left -= 0.5)
            {
                Now += Math.Min(left, 0.5);
                Watch.Check();
            }
        }

        public string Report => Assert.Single(Reports).ToString();
    }

    [Fact]
    public void Frames_coming_write_nothing()
    {
        var rig = new Rig();
        for (int i = 0; i < 100; i++)
        {
            rig.Stand(0.5);
            rig.Watch.Beat();
        }
        Assert.Empty(rig.Reports);
    }

    [Fact]
    public void Nothing_counts_before_the_first_frame()
    {
        double now = 0;
        var reports = 0;
        var watch = new FrameWatch(() =>
        {
            reports++;
            return new StringWriter();
        }, () => now)
        {
            Threads = () => [],
            Memory = () => "",
        };
        now = 60;                                                                // the boot, in the frame before the first
        watch.Check();
        Assert.Equal(0, reports);
        watch.Beat();
        now = 68;
        watch.Check();
        Assert.Equal(1, reports);
    }

    [Fact]
    public void No_frame_for_eight_seconds_opens_a_report_of_what_went_before()
    {
        var rig = new Rig { Threads = [new ThreadSample(7, "VkThread", 'R', 300, "", "running")] };
        rig.Watch.Describe = () => "week 34";
        rig.Watch.Beat();
        rig.Watch.At("draw");
        rig.Watch.Note(new DateOnly(2026, 11, 1));
        rig.Stand(7.5);
        Assert.Empty(rig.Reports);
        rig.Stand(0.5);
        string report = rig.Report;
        Assert.Contains("no frame for 8.0 s: frame 2", report);
        Assert.Contains("game: week 34", report);
        Assert.True(report.IndexOf("  frame", StringComparison.Ordinal) < report.IndexOf("  draw", StringComparison.Ordinal));
        Assert.Contains(new DateOnly(2026, 11, 1).ToString(), report);
        Assert.Contains("VkThread", report);
        Assert.Contains("VmRSS: 10 kB", report);
        Assert.Contains("collections:", report);
    }

    [Fact]
    public void A_frame_again_closes_the_report_and_the_next_stall_opens_another()
    {
        var rig = new Rig();
        rig.Stand(9);
        rig.Watch.Beat();
        rig.Stand(0.5);
        Assert.Contains("the main thread again after 9.5 s", rig.Report);       // as the watch saw it, at its next look
        rig.Stand(8);
        Assert.Equal(2, rig.Reports.Count);
        Assert.DoesNotContain("again", rig.Reports[1].ToString());
    }

    [Fact]
    public void The_later_samples_show_who_spent_the_time()
    {
        var rig = new Rig { Threads = [new ThreadSample(7, "VkThread", 'R', 300), new ThreadSample(8, "Sleeper", 'S', 50, "futex_wait_queue")] };
        rig.Stand(8);
        rig.Threads = [new ThreadSample(7, "VkThread", 'R', 500), new ThreadSample(8, "Sleeper", 'S', 50, "futex_wait_queue")];
        rig.Stand(2);
        string[] lines = rig.Report.Split('\n');
        string last = Assert.Single(lines, line => line.Contains("VkThread") && line.Contains("500"));
        Assert.Contains("+200", last);
        Assert.Contains(lines, line => line.Contains("Sleeper") && line.Contains("+0") && line.Contains("futex_wait_queue"));
        Assert.True(Array.IndexOf(lines, last) < Array.FindLastIndex(lines, line => line.Contains("Sleeper")), "who spent the most comes first");
    }

    [Fact]
    public void The_samples_stop_after_the_last()
    {
        var rig = new Rig();
        rig.Stand(1000);
        int samples = rig.Report.Split("threads ").Length - 1;
        Assert.Equal(5, samples);
    }

    [Fact]
    public void Frames_stopped_on_purpose_write_nothing_and_do_not_count_after_the_resume()
    {
        var rig = new Rig();
        rig.Watch.Pause();
        rig.Stand(100);
        Assert.Empty(rig.Reports);
        rig.Watch.Resume();
        rig.Stand(7.5);
        Assert.Empty(rig.Reports);
        rig.Stand(0.5);
        Assert.Single(rig.Reports);
    }

    [Fact]
    public void A_pause_in_a_stall_means_the_main_thread_answered()
    {
        var rig = new Rig();
        rig.Stand(9);
        rig.Watch.Pause();
        rig.Stand(0.5);
        Assert.Contains("the main thread again", rig.Report);
        rig.Stand(100);
        Assert.Single(rig.Reports);
    }

    [Fact]
    public void Only_the_last_places_are_kept_oldest_first()
    {
        var rig = new Rig();
        for (int i = 0; i < 100; i++)
            rig.Watch.At($"place-{i:D3}");
        rig.Stand(8);
        string report = rig.Report;
        Assert.DoesNotContain("place-051", report);
        Assert.True(report.IndexOf("place-052", StringComparison.Ordinal) < report.IndexOf("place-099", StringComparison.Ordinal));
    }

    [Fact]
    public void A_description_that_fails_does_not_stop_the_report()
    {
        var rig = new Rig();
        rig.Watch.Describe = () => throw new InvalidOperationException("collection changed");
        rig.Watch.Note(new Unprintable());
        rig.Stand(8);
        Assert.Contains("(InvalidOperationException: collection changed)", rig.Report);
        Assert.Contains("(NotSupportedException:", rig.Report);
        Assert.Contains("memory:", rig.Report);
    }

    private sealed class Unprintable
    {
        public override string ToString() => throw new NotSupportedException("no text");
    }

    [Theory]
    [InlineData("123 (VkThread) R 1 2 3 0 -1 4194560 10 0 0 0 110 25 0 0 20", "VkThread", 'R', 135)]
    [InlineData("9 (.NET TP Worker) S 1 2 3 0 -1 4194560 10 0 0 0 7 0 0 0 20", ".NET TP Worker", 'S', 7)]
    [InlineData("9 (a) b)) D 1 2 3 0 -1 4194560 10 0 0 0 1 2 0 0 20", "a) b)", 'D', 3)]
    public void A_stat_line_is_read(string line, string name, char state, long ticks)
    {
        var thread = ProcThreads.Parse(9, line);
        Assert.NotNull(thread);
        Assert.Equal(name, thread.Name);
        Assert.Equal(state, thread.State);
        Assert.Equal(ticks, thread.CpuTicks);
    }

    [Theory]
    [InlineData("")]
    [InlineData("9 no brackets")]
    [InlineData("9 (short) S 1 2")]
    public void Anything_else_is_no_thread(string line) => Assert.Null(ProcThreads.Parse(9, line));

    [Fact]
    public void The_threads_folder_is_read_and_a_thread_that_ended_left_out()
    {
        string tasks = Directory.CreateTempSubdirectory("kichikuou-frame-watch").FullName;
        try
        {
            string thread = Directory.CreateDirectory(Path.Combine(tasks, "42")).FullName;
            File.WriteAllText(Path.Combine(thread, "stat"), "42 (VkThread) S 1 2 3 0 -1 4194560 10 0 0 0 1 2 0 0 20\n");
            File.WriteAllText(Path.Combine(thread, "wchan"), "futex_wait_queue\n");
            Directory.CreateDirectory(Path.Combine(tasks, "43"));                  // no stat any more
            var read = ProcThreads.Read(tasks);
            var only = Assert.Single(read);
            Assert.Equal(42, only.Id);
            Assert.Equal("futex_wait_queue", only.Wait);
            Assert.Equal("", only.Syscall);
        }
        finally
        {
            Directory.Delete(tasks, recursive: true);
        }
    }

    [Fact]
    public void No_proc_folder_means_no_threads_and_no_memory()
    {
        Assert.Empty(ProcThreads.Read(Path.Combine(Path.GetTempPath(), "kichikuou-no-such-folder")));
        Assert.Equal("", ProcThreads.Memory(Path.Combine(Path.GetTempPath(), "kichikuou-no-such-file")));
    }

    [Fact]
    public void The_memory_line_keeps_what_tells_about_a_stall()
    {
        string status = Path.GetTempFileName();
        try
        {
            File.WriteAllText(status, "Name:\tgame\nVmRSS:\t    3968 kB\nVmSwap:\t       0 kB\nThreads:\t1\nSigQ:\t0/1\n");
            Assert.Equal("VmRSS: 3968 kB, VmSwap: 0 kB, Threads: 1", ProcThreads.Memory(status));
        }
        finally
        {
            File.Delete(status);
        }
    }
}
