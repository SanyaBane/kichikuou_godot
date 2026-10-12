using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.TestSupport;

/// <summary>
/// A scene input that answers nothing: every command throws, naming itself. A test's input derives
/// from it and answers the commands its file reads.
/// </summary>
public class UnexpectedInput : ISceneInput
{
    public virtual Task<Sys3Int> WaitForKeyAsync() => throw new InvalidOperationException("IK 0");

    public virtual Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => throw new InvalidOperationException("Y 3");

    public virtual Task<Sys3Int> GetPlayingTrackAsync() => throw new InvalidOperationException("SR 0");

    public virtual Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items) => throw new InvalidOperationException("]");

    public virtual Task<MouseState> GetMouseAsync() => throw new InvalidOperationException("IM");

    public virtual Task<Sys3Int> SetCursorAsync(Sys3Int shape) => throw new InvalidOperationException("IC");

    public virtual Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new InvalidOperationException("ZT 0");

    public virtual Task ResetTimerAsync(Sys3Int value) => throw new InvalidOperationException("ZT 1");

    public virtual Task<Sys3Int> GetTimerAsync(int mode) => throw new InvalidOperationException("ZT n");

    public virtual Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => throw new InvalidOperationException("SC");

    public virtual Task<Sys3Int> GetMidiTrackAsync() => throw new InvalidOperationException("SR 1");
}

/// <summary>A random input ran out of answers: the run did not end, a loop does not stop.</summary>
public sealed class InputBudgetException() : Exception("The input budget ran out.");
