namespace Kichikuou.Engine;

/// <summary>
/// The local paths of the developer's machine: the game folder, the extracted data, the tools. A key
/// is read from the environment variable of the same name, else from its line in <c>local.env</c>
/// at the repository root, which git ignores (<c>local.env.example</c> is the template with every
/// key). Outside the repository (an exported game) only the environment counts. Nothing here is a
/// game rule; the tests, the Godot development defaults and <c>tools/golden/capture.ps1</c> read it.
/// </summary>
public static class LocalPaths
{
    /// <summary>The local file's name, next to <c>Kichikuou.sln</c>.</summary>
    public const string FileName = "local.env";

    /// <summary>The player's game folder (<c>KICHIKUOU_GAME_DIR</c>).</summary>
    public static string? GameDirectory => Get("KICHIKUOU_GAME_DIR");

    /// <summary>The folder of extracted data, <c>_extract</c> in the docs (<c>KICHIKUOU_EXTRACT_DIR</c>).</summary>
    public static string? ExtractDirectory => Get("KICHIKUOU_EXTRACT_DIR");

    /// <summary>The raw decompiled scenario: <c>KICHIKUOU_SCENARIO_DIR</c>, else <c>scenario</c> in the extract.</summary>
    public static string? ScenarioDirectory => Get("KICHIKUOU_SCENARIO_DIR") ?? InExtract("scenario");

    /// <summary>The golden captures: <c>KICHIKUOU_GOLDEN_DIR</c>, else <c>golden</c> in the extract.</summary>
    public static string? GoldenDirectory => Get("KICHIKUOU_GOLDEN_DIR") ?? InExtract("golden");

    /// <summary>A key's value: its environment variable, else its line in the local file; null when neither has one.</summary>
    public static string? Get(string key) =>
        Environment.GetEnvironmentVariable(key) is { Length: > 0 } value ? value
        : LocalFile.Value.TryGetValue(key, out string? line) && line.Length > 0 ? line
        : null;

    /// <summary>
    /// Reads <c>KEY=value</c> lines: blank lines and lines starting with <c>#</c> are skipped, the
    /// value is the rest of the line trimmed (spaces inside kept), and quotes around it are dropped
    /// (Explorer's "Copy as path" adds them). A later line of the same key wins.
    /// </summary>
    public static Dictionary<string, string> Parse(IEnumerable<string> lines)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            int eq = line.IndexOf('=');
            if (line.Length == 0 || line[0] == '#' || eq <= 0)
                continue;
            string value = line[(eq + 1)..].Trim();
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                value = value[1..^1];
            values[line[..eq].Trim()] = value;
        }
        return values;
    }

    private static string? InExtract(string folder) =>
        ExtractDirectory is { } extract ? Path.Combine(extract, folder) : null;

    private static readonly Lazy<Dictionary<string, string>> LocalFile = new(() =>
        FindFile() is { } path ? Parse(File.ReadAllLines(path)) : []);

    /// <summary>
    /// The local file of the repository the process runs in: the first folder that holds
    /// <c>Kichikuou.sln</c>, up from the assemblies (tests) or the working directory (Godot runs in
    /// its project folder).
    /// </summary>
    private static string? FindFile()
    {
        foreach (string start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Kichikuou.sln")))
                {
                    string path = Path.Combine(dir.FullName, FileName);
                    if (File.Exists(path))
                        return path;
                    break;
                }
            }
        }
        return null;
    }
}
