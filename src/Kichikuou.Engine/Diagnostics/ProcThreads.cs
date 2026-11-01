namespace Kichikuou.Engine.Diagnostics;

/// <summary>
/// One thread of this process as Linux's <c>/proc</c> shows it: <paramref name="State"/> R is running, S and D are asleep;
/// <paramref name="CpuTicks"/> its user and system time so far (100 a second); <paramref name="Wait"/> the kernel function
/// it sleeps in (<c>wchan</c>) and <paramref name="Syscall"/> the call it is in, with its stack and program counter.
/// </summary>
public sealed record ThreadSample(int Id, string Name, char State, long CpuTicks, string Wait = "", string Syscall = "");

/// <summary>What the process can read about its own threads and memory, on Android too; nothing where there is no <c>/proc</c>.</summary>
public static class ProcThreads
{
    private const string Tasks = "/proc/self/task", Status = "/proc/self/status";

    /// <summary>The process's threads now; one that ended meanwhile is left out.</summary>
    public static IReadOnlyList<ThreadSample> Read() => Read(Tasks);

    /// <summary>The threads in the folder <paramref name="tasks"/>, laid out as <c>/proc/self/task</c>; none where it is missing.</summary>
    public static IReadOnlyList<ThreadSample> Read(string tasks)
    {
        var threads = new List<ThreadSample>();
        if (!Directory.Exists(tasks))
            return threads;
        foreach (string folder in Directory.EnumerateDirectories(tasks))
        {
            if (int.TryParse(Path.GetFileName(folder), out int id)
                && ReadFile(Path.Combine(folder, "stat")) is { } stat && Parse(id, stat) is { } thread)
            {
                threads.Add(thread with
                {
                    Wait = ReadFile(Path.Combine(folder, "wchan"))?.Trim() ?? "",
                    Syscall = ReadFile(Path.Combine(folder, "syscall"))?.Trim() ?? "",
                });
            }
        }
        return threads;
    }

    /// <summary>
    /// A line of <c>/proc/…/stat</c>: <c>id (name) state …</c> with the user and system time the 14th and 15th fields. The
    /// name may hold spaces and brackets, so it ends at the last closing bracket. Null when the line is not one.
    /// </summary>
    public static ThreadSample? Parse(int id, string stat)
    {
        int open = stat.IndexOf('('), close = stat.LastIndexOf(')');
        if (open < 0 || close < open)
            return null;
        string[] fields = stat[(close + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        const int State = 0, UserTime = 11, SystemTime = 12;       // the line's fields 3, 14 and 15
        if (fields.Length <= SystemTime || fields[State].Length != 1
            || !long.TryParse(fields[UserTime], out long user) || !long.TryParse(fields[SystemTime], out long system))
            return null;
        return new ThreadSample(id, stat[(open + 1)..close], fields[State][0], user + system);
    }

    /// <summary>The process's memory and its threads' count, as one line (<c>VmRSS</c>, <c>VmSwap</c>, <c>Threads</c>).</summary>
    public static string Memory() => Memory(Status);

    /// <summary>The same from the file <paramref name="status"/>, laid out as <c>/proc/self/status</c>; empty where it is missing.</summary>
    public static string Memory(string status)
    {
        if (ReadFile(status) is not { } text)
            return "";
        var kept = text.Split('\n').Where(line => line.StartsWith("VmRSS:", StringComparison.Ordinal)
            || line.StartsWith("VmSwap:", StringComparison.Ordinal) || line.StartsWith("Threads:", StringComparison.Ordinal));
        return string.Join(", ", kept.Select(line => string.Join(' ', line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))));
    }

    /// <summary>A file of <c>/proc</c>, or null when it cannot be read: the thread has ended, or the system does not allow it.</summary>
    private static string? ReadFile(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
