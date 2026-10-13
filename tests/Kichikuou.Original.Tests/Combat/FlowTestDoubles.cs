using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Tests.Combat;

/// <summary>Answers the battle's input requests from queues and records what was asked.</summary>
public sealed class ScriptedInput : IBattleInput
{
    /// <summary>Key codes for <c>IK 0</c>; 0 once empty.</summary>
    public Queue<int> Keys { get; } = new();

    /// <summary>Answers to FIGT_SEL.</summary>
    public Queue<FigtSelChoice> Commands { get; } = new();

    /// <summary>Answers to script menus (<c>null</c> = cancel).</summary>
    public Queue<int?> MenuAnswers { get; } = new();

    /// <summary>The FIGT_SEL menus offered, in order.</summary>
    public List<FigtSelMenu> CommandMenus { get; } = [];

    /// <summary>The script menus offered, in order.</summary>
    public List<BattleMenu> Menus { get; } = [];

    /// <summary>Number of key waits.</summary>
    public int KeyWaits { get; private set; }

    /// <summary>What <c>SR 0</c> reports.</summary>
    public int Track { get; set; } = 7;

    /// <summary>The key waits ("key") and the release loops ("release"), in order.</summary>
    public List<string> Waits { get; } = [];

    public Task<Sys3Int> WaitForKeyAsync()
    {
        KeyWaits++;
        Waits.Add("key");
        return Task.FromResult(Keys.Count > 0 ? (Sys3Int)Keys.Dequeue() : Sys3Int.Zero);
    }

    /// <summary>Keys that cut <c>Y 3</c> pauses short; 0 (ran out) once empty.</summary>
    public Queue<int> PauseKeys { get; } = new();

    /// <summary>The ticks of every <c>Y 3</c> pause, in order.</summary>
    public List<int> Pauses { get; } = [];

    /// <summary>Whether a key may cut each <c>Y 3</c> pause short (the engine's <c>Y3waitFlags</c>), in order.</summary>
    public List<bool> PauseCancelables { get; } = [];

    public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
    {
        Pauses.Add(ticks.Value);
        PauseCancelables.Add(cancelable);
        return Task.FromResult(PauseKeys.Count > 0 ? (Sys3Int)PauseKeys.Dequeue() : Sys3Int.Zero);
    }

    public Task WaitForReleaseAsync()
    {
        Waits.Add("release");
        return Task.CompletedTask;
    }

    public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
    {
        CommandMenus.Add(menu);
        if (Commands.Count == 0)
            throw new InvalidOperationException("Unexpected FIGT_SEL.");
        return Task.FromResult(Commands.Dequeue());
    }

    public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
    {
        Menus.Add(menu);
        if (MenuAnswers.Count == 0)
            throw new InvalidOperationException($"Unexpected menu {menu.Kind}.");
        int? answer = MenuAnswers.Dequeue();
        return Task.FromResult(answer is null ? (Sys3Int?)null : (Sys3Int)answer.Value);
    }

    public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult((Sys3Int)Track);
}

/// <summary>Records story calls and runs an optional action for each (to simulate a scene's effects).</summary>
public sealed class ScriptedStory : IStoryHooks
{
    public List<(string File, int Entry)> Calls { get; } = [];

    /// <summary>Runs on every call with (file, entry).</summary>
    public Action<string, int>? OnCall { get; set; }

    public Task RunEventAsync(string file, Sys3Int entry)
    {
        Calls.Add((file, entry.Value));
        OnCall?.Invoke(file, entry.Value);
        return Task.CompletedTask;
    }
}

/// <summary>A battle over a fresh state with scripted draws, input and story.</summary>
public sealed class FlowRig
{
    public FlowRig(params int[] draws)
    {
        Random = new ScriptedRandom(draws);
        Battle = new Battle(State, Random, View, Input, Story);
    }

    public GameState State { get; } = new();
    public ScriptedRandom Random { get; }
    public RecordingView View { get; } = new();
    public ScriptedInput Input { get; } = new();
    public ScriptedStory Story { get; } = new();
    public Battle Battle { get; }

    /// <summary>The lines of the MessageEvents and ValueMessageEvents shown, in order.</summary>
    public List<int> Lines => View.Events
        .Select(e => e switch
        {
            MessageEvent m => m.Text.Line,
            ValueMessageEvent v => v.Text.Line,
            _ => 0,
        })
        .Where(l => l != 0)
        .ToList();

    /// <summary>
    /// Runs until the battle asks for a random draw or an input the test did not script (the
    /// test doubles throw "Unexpected …"). Returns true when it stopped there, false when the
    /// action completed. Tests use it to assert the state reached up to that frontier.
    /// </summary>
    public static async Task<bool> UntilStub(Func<Task> action)
    {
        try
        {
            await action();
            return false;
        }
        catch (InvalidOperationException e) when (e.Message.StartsWith("Unexpected", StringComparison.Ordinal))
        {
            return true;
        }
    }

    /// <summary>Places player units in slots 1.. and gives each the troops listed.</summary>
    public void PlayerSlots(params (int Unit, int Troops)[] units)
    {
        for (int i = 0; i < units.Length; i++)
        {
            SetPlayerSlot(i + 1, units[i].Unit);
            State.Units[units[i].Unit].Troops = (Sys3Int)units[i].Troops;
        }
    }

    /// <summary>Places enemy commanders in slots 1.. and gives each the troops listed.</summary>
    public void EnemySlots(params (int Cmdr, int Troops)[] cmdrs)
    {
        for (int i = 0; i < cmdrs.Length; i++)
        {
            SetEnemySlot(i + 1, cmdrs[i].Cmdr);
            State.Cmdrs[cmdrs[i].Cmdr].Troops = (Sys3Int)cmdrs[i].Troops;
        }
    }

    public void SetPlayerSlot(int slot, int unit)
    {
        var v = (Sys3Int)unit;
        switch (slot)
        {
            case 1: State.Bt_PlayerSlot1 = v; break;
            case 2: State.Bt_PlayerSlot2 = v; break;
            case 3: State.Bt_PlayerSlot3 = v; break;
            default: State.Bt_PlayerSlot4 = v; break;
        }
    }

    public void SetEnemySlot(int slot, int cmdr)
    {
        var v = (Sys3Int)cmdr;
        switch (slot)
        {
            case 1: State.Bt_EnemySlot1 = v; break;
            case 2: State.Bt_EnemySlot2 = v; break;
            case 3: State.Bt_EnemySlot3 = v; break;
            default: State.Bt_EnemySlot4 = v; break;
        }
    }

    public void PlayerParty(params int[] units)
    {
        State.Bt_PlayerParty1 = units.Length > 0 ? (Sys3Int)units[0] : Sys3Int.Zero;
        State.Bt_PlayerParty2 = units.Length > 1 ? (Sys3Int)units[1] : Sys3Int.Zero;
        State.Bt_PlayerParty3 = units.Length > 2 ? (Sys3Int)units[2] : Sys3Int.Zero;
        State.Bt_PlayerParty4 = units.Length > 3 ? (Sys3Int)units[3] : Sys3Int.Zero;
    }

    public void EnemyParty(params int[] cmdrs)
    {
        State.Bt_EnemyParty1 = cmdrs.Length > 0 ? (Sys3Int)cmdrs[0] : Sys3Int.Zero;
        State.Bt_EnemyParty2 = cmdrs.Length > 1 ? (Sys3Int)cmdrs[1] : Sys3Int.Zero;
        State.Bt_EnemyParty3 = cmdrs.Length > 2 ? (Sys3Int)cmdrs[2] : Sys3Int.Zero;
        State.Bt_EnemyParty4 = cmdrs.Length > 3 ? (Sys3Int)cmdrs[3] : Sys3Int.Zero;
    }
}

internal static class Sys3Assert
{
    /// <summary>Asserts a script value.</summary>
    public static void Is(int expected, Sys3Int actual) => Assert.Equal(expected, (int)actual);
}
