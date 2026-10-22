using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Tests.Script;

/// <summary>
/// The state's copy (docs/ui/talk-marks.md §8, step 2): the variables, every array page at its size, the strings
/// and the port's own data, the copy independent of its source.
/// </summary>
public class StateCopyTests
{
    /// <summary>The test variant's state as another type: a copy between the two is refused.</summary>
    private sealed class OtherState() : ScriptState(TestState.Variant)
    {
        protected override VariableBinding? BindingOf(int number) => null;
    }

    /// <summary>A state with something in every part: variables, two pages of different sizes, strings, two keys of Extras.</summary>
    private static TestState Filled()
    {
        var state = new TestState();
        state.RetVal = (Sys3Int)7;
        state.Memory.Var(42) = (Sys3Int)4242;
        state.Memory.Var(99) = (Sys3Int)65535;
        state.Memory.AllocatePage(1, 300);
        state.Memory.AllocatePage(5, 12);
        state.Memory.WritePage(1, 0, (Sys3Int)1);
        state.Memory.WritePage(1, 299, (Sys3Int)299);
        state.Memory.WritePage(5, 11, (Sys3Int)511);
        state.Strings[1] = "Rance";
        state.Strings[ScriptMemory.DefaultStringCount] = "last";
        state.Extras["known-provinces"] = [2, 6];
        state.MarkLineRead(new ScriptRef("GTEST.ADV", 12));
        return state;
    }

    private static void AssertSame(ScriptState expected, ScriptState actual)
    {
        for (int v = 0; v < expected.Memory.VariableCount; v++)
            Assert.Equal(expected.Memory.Var(v), actual.Memory.Var(v));
        Assert.Equal(expected.Memory.AllocatedPages, actual.Memory.AllocatedPages);
        foreach (int page in expected.Memory.AllocatedPages)
        {
            Assert.Equal(expected.Memory.PageSize(page), actual.Memory.PageSize(page));
            for (int i = 0; i < expected.Memory.PageSize(page); i++)
                Assert.Equal(expected.Memory.ReadPage(page, i), actual.Memory.ReadPage(page, i));
        }
        Assert.Equal(expected.Strings, actual.Strings);
        Assert.Equal(expected.Extras.Keys.Order(), actual.Extras.Keys.Order());
        foreach (var (key, values) in expected.Extras)
            Assert.Equal(values, actual.Extras[key]);
    }

    [Fact]
    public void A_copy_is_its_source_whole()
    {
        var source = Filled();

        var copy = ScriptState.Copy(source);

        AssertSame(source, copy);
        Assert.Equal((Sys3Int)7, copy.RetVal);
        Assert.Equal(300, copy.Memory.PageSize(1));
        Assert.Equal(12, copy.Memory.PageSize(5));
        Assert.True(copy.IsLineRead(new ScriptRef("GTEST.ADV", 12)));
    }

    [Fact]
    public void A_copy_shares_nothing_with_its_source()
    {
        var source = Filled();
        var copy = ScriptState.Copy(source);

        copy.Memory.Var(42) = (Sys3Int)1;
        copy.Memory.WritePage(1, 299, (Sys3Int)2);
        copy.Strings[1] = "Sill";
        copy.Extras["known-provinces"][0] = 9;
        copy.MarkLineRead(new ScriptRef("GTEST.ADV", 13));
        copy.Extras["new"] = [1];

        AssertSame(Filled(), source);
        Assert.All(source.Extras.Keys, key => Assert.NotSame(source.Extras[key], copy.Extras[key]));
    }

    [Fact]
    public void A_copy_into_a_used_state_leaves_nothing_of_it()
    {
        var source = Filled();
        var target = new TestState();
        target.Memory.AllocatePage(1, 20);      // another size
        target.Memory.AllocatePage(7, 5);       // a page the source has not
        target.Memory.WritePage(7, 0, (Sys3Int)3);
        target.Memory.Var(3) = (Sys3Int)3;
        target.Strings[2] = "Maris";
        target.Extras["stale"] = [1, 2, 3];

        source.CopyTo(target);

        AssertSame(source, target);
        Assert.Equal(0, target.Memory.PageSize(7));
        Assert.False(target.Extras.ContainsKey("stale"));
    }

    [Fact]
    public void A_copy_into_the_same_target_again_allocates_no_page()
    {
        var source = new TestState();
        source.Memory.AllocatePage(1, 20000);
        var target = new TestState();
        source.CopyTo(target);

        source.Memory.WritePage(1, 19999, (Sys3Int)5);
        long before = GC.GetAllocatedBytesForCurrentThread();
        source.CopyTo(target);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal((Sys3Int)5, target.Memory.ReadPage(1, 19999));
        Assert.True(allocated < 20000 * sizeof(ushort), $"{allocated} bytes allocated");
    }

    [Fact]
    public void A_copy_into_itself_changes_nothing()
    {
        var state = Filled();

        state.CopyTo(state);

        AssertSame(Filled(), state);
    }

    /// <summary>The memory's fingerprint (the dry run's repeated decisions): equal for a copy, other for any part changed.</summary>
    [Fact]
    public void A_copys_memory_has_the_same_fingerprint_and_a_change_another()
    {
        var source = Filled();
        ulong print = source.Memory.Fingerprint();

        Assert.Equal(print, ScriptState.Copy(source).Memory.Fingerprint());
        source.Extras["known-provinces"] = [3];                         // the port's own data is no memory
        Assert.Equal(print, source.Memory.Fingerprint());
        var changes = new Action<TestState>[]
        {
            s => s.Memory.Var(42) = (Sys3Int)4243,
            s => s.Memory.WritePage(5, 0, (Sys3Int)1),
            s => s.Memory.AllocatePage(7, 1),
            s => s.Strings[2] = "Sill",
        };
        foreach (var change in changes)
        {
            var copy = ScriptState.Copy(Filled());
            change(copy);
            Assert.NotEqual(print, copy.Memory.Fingerprint());
        }
    }

    [Fact]
    public void A_copy_needs_a_state_of_the_same_type()
    {
        Assert.Throws<ArgumentException>(() => Filled().CopyTo(new OtherState()));
        Assert.Throws<ArgumentException>(() => new ScriptMemory(100).CopyTo(new ScriptMemory(806)));
    }
}
