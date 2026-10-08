using Kichikuou.Core.Golden;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.Golden;

/// <summary>Skipped, with the roots listed, while captures wait for the port of their root.</summary>
public sealed class PendingCapturesFactAttribute : FactAttribute
{
    public PendingCapturesFactAttribute()
    {
        var pending = GoldenData.Pending.ToList();
        if (pending.Count > 0)
        {
            Skip = $"{pending.Count} captures not replayed: no replay entry point yet for "
                + string.Join(", ", pending.GroupBy(p => p.Header.RootName).OrderBy(g => g.Key, StringComparer.Ordinal)
                    .Select(g => $"{g.Key} ×{g.Count()}"))
                + " (docs/golden-tests.md, \"Roots\"). They are parsed by Capture_awaiting_its_port_is_well_formed.";
        }
    }
}

/// <summary>A theory over the captures that wait for a port; skipped when there are none.</summary>
public sealed class PendingTheoryAttribute : TheoryAttribute
{
    public PendingTheoryAttribute()
    {
        if (!GoldenData.Pending.Any())
            Skip = $"No captures of strategic roots in {GoldenData.Directory}.";
    }
}

/// <summary>A fact about the newest boot capture (root <c>SET.ADV</c>); skipped when there is none.</summary>
public sealed class BootCaptureFactAttribute : FactAttribute
{
    public BootCaptureFactAttribute()
    {
        if (GoldenPendingTests.NewestBootCapture is null)
            Skip = $"No boot capture (root SET.ADV) in {GoldenData.Directory} (start the game with tools/golden/capture.ps1).";
    }
}

/// <summary>Captures of roots the core cannot replay yet (the strategic layer, F2).</summary>
public class GoldenPendingTests
{
    public static IEnumerable<object[]> Captures => GoldenData.Pending.Select(p => new object[] { GoldenData.Relative(p.Path) });

    public static string? NewestBootCapture =>
        GoldenData.Pending.Where(p => p.Header.Root == "SET.ADV").Select(p => p.Path)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal).LastOrDefault();

    [PendingCapturesFact]
    public void Every_capture_has_a_replay_entry_point() => Assert.Empty(GoldenData.Pending);

    [PendingTheory]
    [MemberData(nameof(Captures))]
    public void Capture_awaiting_its_port_is_well_formed(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(GoldenData.Directory, file));

        Assert.Equal(GoldenCapture.FormatVersion, capture.Version);
        Assert.Equal(capture.Group, Path.GetFileName(Path.GetDirectoryName(Path.Combine(GoldenData.Directory, file))));
        Assert.Equal(capture.RootKind == GoldenRootKind.Label, capture.RootCall.File == capture.Root);
        var memory = new ScriptMemory();
        capture.Enter.ApplyTo(memory);
        capture.Exit.ApplyTo(memory);
    }

    /// <summary>
    /// <c>SET.ADV</c> at boot (survey-data §2-3): no draws and no input; the only hook is
    /// <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c> (mode 10, page 4 from the save files); the exit dump has the generated
    /// <see cref="GameState"/> layout.
    /// </summary>
    [BootCaptureFact]
    public void The_boot_capture_holds_SET_and_its_save_hook()
    {
        var capture = GoldenCapture.Load(NewestBootCapture!);

        Assert.Equal("SET.ADV", capture.Root);
        Assert.Equal("boot", capture.Group);
        Assert.Equal(GoldenRootKind.PageCall, capture.RootKind);
        Assert.Equal("ｽﾀｰﾄ.ADV", capture.RootCall.File);
        Assert.Null(capture.JumpTarget);
        var hook = Assert.IsType<HookRecord>(Assert.Single(capture.Records));
        Assert.Equal("ﾛｰﾄﾞｾｰﾌﾞ.ADV", hook.File);
        Assert.Equal(10, hook.Enter.Vars[0]);

        Assert.Empty(capture.Enter.Bindings);
        var expected = VariableCatalog.Bindings.Select(b => new DumpBinding(b.Var, b.Page, b.Offset, b.Cursor)).ToHashSet();
        Assert.True(expected.SetEquals(capture.Exit.Bindings));
        foreach (var (page, size) in VariableCatalog.Pages)
            Assert.Equal(size, capture.Exit.Pages[page].Length);
        Assert.Equal(capture.Enter.RngIndex, capture.Exit.RngIndex);
        Assert.Equal(capture.Enter.RngState, capture.Exit.RngState);

        // Boot values of survey-data §2.2-2.3.
        var state = new GameState();
        capture.Exit.ApplyTo(state.Memory);
        Assert.Equal(52, state.Unit_Count.Value);
        Assert.Equal(20, state.Unit_StrBase.Value);
        Assert.Equal([1, 1, 1, 1], Enumerable.Range(1, 4).Select(u => (int)state.Units[u].Status.Value));
    }
}
