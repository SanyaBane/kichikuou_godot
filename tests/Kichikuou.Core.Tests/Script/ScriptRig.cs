using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Tests.Combat;

namespace Kichikuou.Core.Tests.Script;

/// <summary>A script context over a fresh state with scripted draws, input and story.</summary>
public sealed class ScriptRig
{
    public ScriptRig(params int[] draws)
    {
        Random = new ScriptedRandom(draws);
        Script = new OriginalScriptContext(State, Random, View, Input, Story);
    }

    public GameState State { get; } = new();
    public ScriptedRandom Random { get; }
    public RecordingView View { get; } = new();
    public ScriptedInput Input { get; } = new();
    public ScriptedStory Story { get; } = new();
    public OriginalScriptContext Script { get; }
}
