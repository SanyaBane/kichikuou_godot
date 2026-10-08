using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.Script;

/// <summary>
/// Answers the scene inputs: the pointer from a <see cref="PointerScript"/>, menus from a queue
/// (failing when it is empty), keys and pauses with 0, the cursor as the engine's static. Fails
/// once the pointer has been read at rest <see cref="RestBudget"/> times in a row, so a test whose
/// gestures run out stops instead of polling forever. As the game's input it also answers the
/// battle from its own queues, failing when they are empty.
/// </summary>
public sealed class ScriptedSceneInput : IGameInput
{
    private int _restReads;

    public PointerScript Pointer { get; } = new();

    /// <summary>Menu answers: 1-based item, 0 = cancel.</summary>
    public Queue<int> Choices { get; } = new();

    /// <summary>The menus shown, in order.</summary>
    public List<IReadOnlyList<SceneMenuItem>> Menus { get; } = [];

    /// <summary>Every <c>IC</c>: the shape set and the shape returned.</summary>
    public List<(int Shape, int Old)> Cursors { get; } = [];

    /// <summary>The cursor shape set last (the engine starts at 1).</summary>
    public int Cursor { get; private set; } = 1;

    /// <summary>Every <c>IM</c> answer, in order.</summary>
    public List<MouseState> MouseReads { get; } = [];

    public int RestBudget { get; init; } = 50;

    /// <summary>Runs before every <c>IM</c> answer (e.g. to note what the view has shown by then).</summary>
    public Action? OnRead { get; set; }

    public Task<MouseState> GetMouseAsync()
    {
        OnRead?.Invoke();
        var m = Pointer.Next(out bool read);
        _restReads = read ? 0 : _restReads + 1;
        if (_restReads > RestBudget)
            throw new InvalidOperationException("Unexpected wait for the pointer: the gestures ran out.");
        MouseReads.Add(m);
        return Task.FromResult(m);
    }

    public Task<Sys3Int> SetCursorAsync(Sys3Int shape)
    {
        int old = Cursor;
        Cursor = shape.Value;
        Cursors.Add((shape.Value, old));
        return Task.FromResult((Sys3Int)old);
    }

    public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
    {
        Menus.Add(items);
        Pointer.Interrupt();
        if (Choices.Count == 0)
            throw new InvalidOperationException("Unexpected menu.");
        return Task.FromResult(Choices.Dequeue());
    }

    /// <summary>Key answers (e.g. 32 for a right click); 0 once empty.</summary>
    public Queue<int> Keys { get; } = new();

    public Task<Sys3Int> WaitForKeyAsync()
    {
        Pointer.Interrupt();
        return Task.FromResult((Sys3Int)(Keys.Count > 0 ? Keys.Dequeue() : 0));
    }

    public Task<Sys3Int> PauseAsync(Sys3Int ticks)
    {
        Pointer.Interrupt();
        return Task.FromResult(Sys3Int.Zero);
    }

    public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult(Sys3Int.Zero);

    public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new InvalidOperationException("Unexpected ZT 0.");

    public Task ResetTimerAsync(Sys3Int value) => throw new InvalidOperationException("Unexpected ZT 1.");

    public Task<Sys3Int> GetTimerAsync(int mode) => throw new InvalidOperationException("Unexpected ZT n.");

    public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => throw new InvalidOperationException("Unexpected SC.");

    public Task<Sys3Int> GetMidiTrackAsync() => throw new InvalidOperationException("Unexpected SR 1.");

    /// <summary>The battle's FIGT_SEL answers.</summary>
    public Queue<FigtSelChoice> BattleCommands { get; } = new();

    /// <summary>The battle's menu answers: an option's code, <c>null</c> = cancel.</summary>
    public Queue<int?> BattleChoices { get; } = new();

    public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
    {
        if (BattleCommands.Count == 0)
            throw new InvalidOperationException("Unexpected FIGT_SEL.");
        return Task.FromResult(BattleCommands.Dequeue());
    }

    public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
    {
        if (BattleChoices.Count == 0)
            throw new InvalidOperationException($"Unexpected battle menu {menu.Kind}.");
        int? answer = BattleChoices.Dequeue();
        return Task.FromResult(answer is null ? (Sys3Int?)null : (Sys3Int)answer.Value);
    }
}
