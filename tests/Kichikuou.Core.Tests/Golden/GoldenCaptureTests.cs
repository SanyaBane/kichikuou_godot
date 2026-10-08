using Kichikuou.Core.Golden;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.Golden;

/// <summary>The capture format of <c>docs/golden-tests.md</c>, as the harness writes it.</summary>
public class GoldenCaptureTests
{
    private static string Dump(string tag, string body = "") =>
        $"mem {tag}\nrng 624\n" +
        string.Concat(Enumerable.Range(0, 39).Select(i => $"rngv {i * 16}" + string.Concat(Enumerable.Repeat(" 0000002a", 16)) + "\n")) +
        $"vars 65537\n{body}end-mem\n";

    private static GoldenCapture Parse(string text) => GoldenCapture.Parse(new StringReader(text), "test");

    [Fact]
    public void Parses_events_dumps_and_hooks_in_order()
    {
        string text =
            "golden 1\nseed 777\nroot FIGT_B.ADV GAL.ADV@b14\n" +
            Dump("enter", "v 0 5 0 0 0 0 0 0 0 0 0 0 0 0 0 0 9\npage 1 20000\np 1 32 0 0 0 4\n" +
                          "bind 74 1 0 154\nstr 8 Leazas\\x20Castle\nstr 9 a\\\\b\\x0a\nstr 10 ﾘｰｻﾞｽ\n") +
            "rnd 2 1 FIGT.ADV@6f\nkey 13 WAO_SUB.ADV@1fe\nwait 15 0 FIGT.ADV@a3c\n" +
            "menu 0 3 FIGT_B.ADV@105\nmusic 7 FIGT_B.ADV@3f0\npick 1 4 FIGT_SEL.ADV@ff\n" +
            "call Sﾌﾞﾀｲ.ADV FIGT_B.ADV@1a0\n" + Dump("hook-enter") + Dump("hook-exit", "v 528 3\n") +
            "ret Sﾌﾞﾀｲ.ADV Sﾌﾞﾀｲ.ADV@9c\n" +
            "jump OVER.ADV FIGT_B.ADV@2b0\n" + Dump("exit") + "end jump OVER.ADV\n";

        var capture = Parse(text);

        Assert.Equal(777u, capture.Seed);
        Assert.Equal("FIGT_B.ADV", capture.Root);
        Assert.Equal(new GoldenLocation("GAL.ADV", 0xb14), capture.RootCall);
        Assert.Equal("OVER.ADV", capture.JumpTarget);
        Assert.Equal(65537, capture.Enter.Vars.Length);
        Assert.Equal(5, capture.Enter.Vars[0]);
        Assert.Equal(9, capture.Enter.Vars[15]);
        Assert.Equal(4, capture.Enter.Pages[1][35]);
        Assert.Equal(new DumpBinding(74, 1, 0, 154), Assert.Single(capture.Enter.Bindings));
        Assert.Equal("Leazas Castle", capture.Enter.Strings[8]);
        Assert.Equal("a\\b\n", capture.Enter.Strings[9]);
        Assert.Equal("ﾘｰｻﾞｽ", capture.Enter.Strings[10]);
        Assert.Equal(624, capture.Enter.RngIndex);
        Assert.Equal(42u, capture.Enter.RngState[623]);

        Assert.Collection(capture.Records,
            r => Assert.Equal(new RndRecord(r.Line, new GoldenLocation("FIGT.ADV", 0x6f), 2, 1), r),
            r => Assert.Equal(13, Assert.IsType<KeyRecord>(r).Code),
            r => Assert.Equal(15, Assert.IsType<WaitRecord>(r).Ticks),
            r => Assert.Equal((0, 3), (Assert.IsType<MenuRecord>(r).Choice, ((MenuRecord)r).Count)),
            r => Assert.Equal(7, Assert.IsType<MusicRecord>(r).Track),
            r => Assert.Equal((1, 4), (Assert.IsType<PickRecord>(r).RetVal, ((PickRecord)r).Tmp0001)),
            r =>
            {
                var hook = Assert.IsType<HookRecord>(r);
                Assert.Equal("Sﾌﾞﾀｲ.ADV", hook.File);
                Assert.Equal(new GoldenLocation("FIGT_B.ADV", 0x1a0), hook.Where);
                Assert.Equal(0, hook.Enter.Vars[528]);
                Assert.Equal(3, hook.Exit.Vars[528]);
            },
            r => Assert.Equal("OVER.ADV", Assert.IsType<JumpRecord>(r).File));
    }

    [Fact]
    public void Parses_a_label_root_with_its_group_and_the_format_2_input_records()
    {
        string text =
            "golden 2\nseed 5\nroot MAIN.ADV MAIN.ADV@20 label\ngroup week\n" + Dump("enter") +
            "mouse 16 320 200 4711 COMMAND.ADV@171\ncursor 6 1 COMMAND.ADV@1c8\ncdpos 999 999 999 999 ｵｰﾌﾟﾆﾝｸﾞ.ADV@3a\n" +
            "date 2026 10 8 19 45 0 5 Gﾜｵ.ADV@1234\ntimer 5 12 XSPEC.ADV@162\n" +
            Dump("exit") + "end label\n";

        var capture = Parse(text);

        Assert.Equal(2, capture.Version);
        Assert.Equal("week", capture.Group);
        Assert.Equal(GoldenRootKind.Label, capture.RootKind);
        Assert.Equal("MAIN.ADV@20", capture.Header.RootName);
        Assert.True(capture.EndsAtLabel);
        Assert.Null(capture.JumpTarget);
        Assert.Collection(capture.Records,
            r => Assert.Equal(new MouseRecord(r.Line, new GoldenLocation("COMMAND.ADV", 0x171), 16, 320, 200, 4711), r),
            r => Assert.Equal((6, 1), (Assert.IsType<CursorRecord>(r).Cursor, ((CursorRecord)r).Old)),
            r => Assert.Equal(999, Assert.IsType<CdPosRecord>(r).Frame),
            r => Assert.Equal([2026, 10, 8, 19, 45, 0, 5], Assert.IsType<DateRecord>(r).Values),
            r => Assert.Equal((5, 12), (Assert.IsType<TimerRecord>(r).Command, ((TimerRecord)r).Value)));
    }

    [Fact]
    public void A_format_1_capture_is_a_battle_and_a_page_call()
    {
        var capture = Parse("golden 1\nseed 1\nroot FIGT.ADV Cﾒｲｷｭｳ.ADV@10\n" + Dump("enter") + Dump("exit") + "end return\n");

        Assert.Equal(1, capture.Version);
        Assert.Equal(GoldenCapture.BattleGroup, capture.Group);
        Assert.Equal(GoldenRootKind.PageCall, capture.RootKind);
        Assert.True(GoldenReplay.CanReplay(capture.Header));
    }

    [Fact]
    public void Only_a_label_root_ends_at_its_label()
    {
        string text = "golden 2\nseed 1\nroot COM.ADV COMMAND.ADV@10\ngroup com\n" + Dump("enter") + Dump("exit") + "end label\n";

        Assert.Throws<GoldenFormatException>(() => Parse(text));
    }

    [Fact]
    public void The_header_is_read_without_the_dumps()
    {
        string path = Path.Combine(Path.GetTempPath(), $"golden-header-{Guid.NewGuid():N}.golden");
        File.WriteAllText(path, "golden 2\nseed 9\nroot COMMAND.ADV MAIN.ADV@6a1\ngroup command\n" + Dump("enter") + "rnd 3 1 X.ADV@0\n");
        try
        {
            var header = GoldenCapture.ReadHeader(path);

            Assert.Equal(new GoldenHeader(2, "COMMAND.ADV", new GoldenLocation("MAIN.ADV", 0x6a1), GoldenRootKind.PageCall, "command"), header);
            Assert.False(GoldenReplay.CanReplay(header));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_newer_format_is_rejected()
    {
        var e = Assert.Throws<GoldenFormatException>(() => Parse("golden 3\nseed 1\n"));
        Assert.Contains("golden 1", e.Message);
    }

    [Fact]
    public void An_incomplete_capture_is_rejected()
    {
        string text = "golden 1\nseed 1\nroot FIGT.ADV Cﾒｲｷｭｳ.ADV@10\n" + Dump("enter") + "rnd 2 1 FIGT.ADV@6f\n";

        var e = Assert.Throws<GoldenFormatException>(() => Parse(text));
        Assert.Contains("incomplete", e.Message);
    }

    [Fact]
    public void An_aborted_capture_is_rejected()
    {
        string text = "golden 1\nseed 1\nroot FIGT.ADV Cﾒｲｷｭｳ.ADV@10\n" + Dump("enter") + "end abort load\n";

        Assert.Throws<GoldenFormatException>(() => Parse(text));
    }

    [Fact]
    public void A_dump_applies_to_the_core_memory_and_releases_missing_pages()
    {
        var capture = Parse("golden 1\nseed 1\nroot FIGT.ADV X.ADV@0\n" +
            Dump("enter", "v 800 0 0 0 0 0 1\npage 2 1800\np 2 16 7\nstr 3 x\n") +
            Dump("exit") + "end return\n");
        var memory = new ScriptMemory();
        memory.AllocatePage(1, 20000);
        memory.WritePage(1, 5, 9);
        memory.Strings[4] = "old";

        capture.Enter.ApplyTo(memory);

        Assert.Equal(1, memory.Var(805).Value);
        Assert.Equal(0, memory.PageSize(1));
        Assert.Equal(7, memory.ReadPage(2, 16).Value);
        Assert.Equal("x", memory.Strings[3]);
        Assert.Equal(string.Empty, memory.Strings[4]);
    }

    [Fact]
    public void A_variable_beyond_the_core_layout_cannot_be_applied()
    {
        var capture = Parse("golden 1\nseed 1\nroot FIGT.ADV X.ADV@0\n" +
            Dump("enter", "v 1008 1\n") + Dump("exit") + "end return\n");

        var e = Assert.Throws<GoldenFormatException>(() => capture.Enter.ApplyTo(new ScriptMemory()));
        Assert.Contains("VAR1008", e.Message);
    }
}
