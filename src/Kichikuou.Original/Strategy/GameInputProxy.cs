using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// The game's input as a proxy for the UI that answers it now (owner's decision 4: the UI is
/// swappable). A UI is switched by setting <see cref="Target"/> at a safe point — while no request
/// is pending, e.g. at a key wait or in the command loop; a pending request stays with the UI that
/// received it.
/// </summary>
public sealed class GameInputProxy(IGameInput target) : IGameInput
{
    /// <summary>The UI that answers the next request.</summary>
    public IGameInput Target { get; set; } = target;

    /// <summary>Runs before a key wait reaches <see cref="Target"/> (<see cref="InterfaceSwitch"/>'s safe point).</summary>
    internal Action? BeforeKeyWait { get; init; }

    /// <inheritdoc />
    public Task<Sys3Int> WaitForKeyAsync()
    {
        BeforeKeyWait?.Invoke();
        return Target.WaitForKeyAsync();
    }

    /// <inheritdoc />
    public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => Target.PauseAsync(ticks, cancelable);

    /// <inheritdoc />
    public Task<Sys3Int> GetPlayingTrackAsync() => Target.GetPlayingTrackAsync();

    /// <inheritdoc />
    public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu) => Target.SelectCommandAsync(menu);

    /// <inheritdoc />
    public Task<Sys3Int?> ChooseAsync(BattleMenu menu) => Target.ChooseAsync(menu);

    /// <inheritdoc />
    public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items) => Target.SelectAsync(items);

    /// <inheritdoc />
    public Task<MouseState> GetMouseAsync() => Target.GetMouseAsync();

    /// <inheritdoc />
    public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => Target.SetCursorAsync(shape);

    /// <inheritdoc />
    public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => Target.GetDateAsync();

    /// <inheritdoc />
    public Task ResetTimerAsync(Sys3Int value) => Target.ResetTimerAsync(value);

    /// <inheritdoc />
    public Task<Sys3Int> GetTimerAsync(int mode) => Target.GetTimerAsync(mode);

    /// <inheritdoc />
    public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => Target.GetCdPositionAsync();

    /// <inheritdoc />
    public Task<Sys3Int> GetMidiTrackAsync() => Target.GetMidiTrackAsync();

    /// <inheritdoc />
    public Task<IReadOnlyList<Sys3Int>> GetDisplayAsync() => Target.GetDisplayAsync();
}

/// <summary>The game's view as a proxy for the UI that shows it now (decision 4); switched with the input, at a safe point.</summary>
public sealed class ViewProxy(IScriptView target) : IScriptView
{
    /// <summary>The UI that shows the next event.</summary>
    public IScriptView Target { get; set; } = target;

    /// <summary>Runs before an event reaches <see cref="Target"/> (<see cref="InterfaceSwitch"/>'s safe point).</summary>
    internal Action<ScriptEvent>? BeforeShow { get; init; }

    /// <inheritdoc />
    public Task ShowAsync(ScriptEvent scriptEvent)
    {
        BeforeShow?.Invoke(scriptEvent);
        return Target.ShowAsync(scriptEvent);
    }
}

/// <summary>
/// The interface the game talks to, switched at a safe point (decision 4; docs/ui/survey.md §4.2,
/// §4.7): <see cref="Request"/> names the next UI, and the switch happens when the script next waits
/// for a key or the map loop starts waiting (<see cref="MapScreenEvent"/>) — before that request
/// reaches a UI, so the new one gets it whole and no request is left half with each. The game's
/// view and input are <see cref="View"/> and <see cref="Input"/>.
/// </summary>
public sealed class InterfaceSwitch
{
    private (IScriptView View, IGameInput Input)? _pending;

    /// <summary>A switch whose proxies go to <paramref name="view"/> and <paramref name="input"/> first.</summary>
    public InterfaceSwitch(IScriptView view, IGameInput input)
    {
        View = new ViewProxy(view)
        {
            BeforeShow = e =>
            {
                if (e is MapScreenEvent { Waiting: true })
                    Apply();
            },
        };
        Input = new GameInputProxy(input) { BeforeKeyWait = Apply };
    }

    /// <summary>The game's view: the UI shown now.</summary>
    public ViewProxy View { get; }

    /// <summary>The game's input: the UI answering now.</summary>
    public GameInputProxy Input { get; }

    /// <summary>Whether a switch waits for its safe point.</summary>
    public bool Pending => _pending is not null;

    /// <summary>Switches to <paramref name="view"/> and <paramref name="input"/> at the next safe point (a later request replaces this one).</summary>
    public void Request(IScriptView view, IGameInput input) => _pending = (view, input);

    private void Apply()
    {
        if (_pending is not { } next)
            return;
        View.Target = next.View;
        Input.Target = next.Input;
        _pending = null;
    }
}
