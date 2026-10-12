using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using static Kichikuou.Original.Tests.Combat.Sys3Assert;

namespace Kichikuou.Original.Tests.Script;

/// <summary><c>Y 3, n</c> (<see cref="ScriptContext.PauseAsync"/>) as xsystem35 <c>cmdy.c</c> runs it.</summary>
public class PauseTests
{
    [Fact]
    public async Task A_pause_shows_its_event_and_stores_the_key_that_ended_it()
    {
        var rig = new ScriptRig();
        rig.Input.PauseKeys.Enqueue(32);

        await rig.Script.PauseAsync(40);

        Assert.Equal([new PauseEvent(40)], rig.View.Events);
        Assert.Equal([40], rig.Input.Pauses);
        Assert.Equal([true], rig.Input.PauseCancelables); // Y3waitFlags starts as KEYWAIT_CANCELABLE
        Is(32, rig.State.RetVal);
    }

    [Theory]
    [InlineData(10000, false)]
    [InlineData(10001, true)]
    public async Task Y3_10000_and_10001_set_the_flag_without_pausing_or_touching_RetVal(int ticks, bool cancelable)
    {
        var rig = new ScriptRig();
        await rig.Script.PauseAsync(10000); // so that 10001 changes the flag too
        rig.State.RetVal = 9;

        await rig.Script.PauseAsync((Sys3Int)ticks);

        Assert.Equal(cancelable, rig.Script.PauseCancelable);
        Assert.Empty(rig.View.Events);
        Assert.Empty(rig.Input.Pauses);
        Is(9, rig.State.RetVal);
    }

    [Fact]
    public async Task Later_pauses_carry_the_flag_to_the_input()
    {
        var rig = new ScriptRig();

        await rig.Script.PauseAsync(10000);
        await rig.Script.PauseAsync(5);
        await rig.Script.PauseAsync(6);
        await rig.Script.PauseAsync(10001);
        await rig.Script.PauseAsync(7);

        Assert.Equal([5, 6, 7], rig.Input.Pauses);
        Assert.Equal([false, false, true], rig.Input.PauseCancelables);
        Assert.Equal([new PauseEvent(5), new PauseEvent(6), new PauseEvent(7)], rig.View.Events);
    }
}
