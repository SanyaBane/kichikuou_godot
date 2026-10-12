using Kichikuou.Engine;

namespace Kichikuou.Core.Tests;

public class LocalPathsTests
{
    [Fact]
    public void Parse_reads_keys_and_values_as_written()
    {
        var values = LocalPaths.Parse(
        [
            "# a comment",
            "",
            @"KICHIKUOU_GAME_DIR=E:\Some games\The game",
            "  KICHIKUOU_EXTRACT_DIR =  D:/data/_extract  ",
            "#KICHIKUOU_GOLDEN_DIR=commented out",
        ]);

        Assert.Equal(2, values.Count);
        Assert.Equal(@"E:\Some games\The game", values["KICHIKUOU_GAME_DIR"]);
        Assert.Equal("D:/data/_extract", values["KICHIKUOU_EXTRACT_DIR"]);
    }

    [Fact]
    public void Parse_drops_the_quotes_of_a_copied_path_and_keeps_empty_values()
    {
        var values = LocalPaths.Parse(
        [
            @"KICHIKUOU_GAME_DIR=""C:\Program Files\Game""",
            "KICHIKUOU_GODOT_EXE=",
            "=no key",
            "no equals sign",
        ]);

        Assert.Equal(2, values.Count);
        Assert.Equal(@"C:\Program Files\Game", values["KICHIKUOU_GAME_DIR"]);
        Assert.Equal("", values["KICHIKUOU_GODOT_EXE"]);
    }

    [Fact]
    public void Parse_lets_a_later_line_win()
    {
        var values = LocalPaths.Parse(["KEY=first", "KEY=second"]);

        Assert.Equal("second", values["KEY"]);
    }

    [Fact]
    public void Get_reads_the_environment()
    {
        const string key = "KICHIKUOU_LOCAL_PATHS_TEST_KEY";
        Assert.Null(LocalPaths.Get(key));
        Environment.SetEnvironmentVariable(key, @"X:\from the environment");
        try
        {
            Assert.Equal(@"X:\from the environment", LocalPaths.Get(key));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }
}
