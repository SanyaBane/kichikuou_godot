using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

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

    /// <inheritdoc />
    public Task<Sys3Int> WaitForKeyAsync() => Target.WaitForKeyAsync();

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
}

/// <summary>The game's view as a proxy for the UI that shows it now (decision 4); switched with the input, at a safe point.</summary>
public sealed class ViewProxy(IScriptView target) : IScriptView
{
    /// <summary>The UI that shows the next event.</summary>
    public IScriptView Target { get; set; } = target;

    /// <inheritdoc />
    public Task ShowAsync(ScriptEvent scriptEvent) => Target.ShowAsync(scriptEvent);
}
