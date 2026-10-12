using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;
using Kichikuou.Original.Strategy;
using Kichikuou.TestSupport;

namespace Kichikuou.Original.Tests.Strategy;

/// <summary><see cref="InterfaceSwitch"/>: a requested UI takes over at a key wait or when the map loop starts waiting, never in between.</summary>
public class InterfaceSwitchTests
{
    /// <summary>A UI that logs what reaches it under its name.</summary>
    private sealed class Ui(string name, List<string> log) : UnexpectedInput, IGameInput, IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            log.Add($"{name} {scriptEvent.GetType().Name}");
            return Task.CompletedTask;
        }

        public override Task<Sys3Int> WaitForKeyAsync()
        {
            log.Add($"{name} key");
            return Task.FromResult(Sys3Int.Zero);
        }

        public override Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            log.Add($"{name} menu");
            return Task.FromResult(1);
        }

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu) => throw new InvalidOperationException("FIGT_SEL");

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu) => throw new InvalidOperationException("battle menu");
    }

    private static readonly MessageEvent Message = new(new ScriptRef("COMMAND.ADV", 188));

    [Fact]
    public async Task A_switch_waits_for_the_next_key_wait()
    {
        var log = new List<string>();
        var ui = new InterfaceSwitch(new Ui("A", log), new Ui("A", log));
        await ui.View.ShowAsync(Message);
        ui.Request(new Ui("B", log), new Ui("B", log));
        await ui.View.ShowAsync(Message);                   // the message the key wait is for
        await ui.Input.SelectAsync([]);
        Assert.True(ui.Pending);
        await ui.Input.WaitForKeyAsync();
        await ui.View.ShowAsync(Message);

        Assert.Equal(["A MessageEvent", "A MessageEvent", "A menu", "B key", "B MessageEvent"], log);
        Assert.False(ui.Pending);
    }

    [Fact]
    public async Task A_switch_happens_when_the_map_loop_starts_waiting()
    {
        var log = new List<string>();
        var ui = new InterfaceSwitch(new Ui("A", log), new Ui("A", log));
        ui.Request(new Ui("B", log), new Ui("B", log));
        await ui.View.ShowAsync(new MapScreenEvent(false));
        await ui.View.ShowAsync(new MapScreenEvent(true));

        Assert.Equal(["A MapScreenEvent", "B MapScreenEvent"], log);
    }
}
