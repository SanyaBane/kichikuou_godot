using Kichikuou.Engine.Script;
using Kichikuou.Original.Combat;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Tests.Combat;

/// <summary>A battle over a fresh state with scripted draws, for the resolution tests (part R1).</summary>
internal sealed class ResolutionFixture
{
    private ResolutionFixture(int[] draws)
    {
        Random = new ScriptedRandom(draws);
        Battle = new Battle(State, Random, View, Input, Story);
    }

    public GameState State { get; } = new();

    public ScriptedInput Input { get; } = new();

    public ScriptedRandom Random { get; }

    public RecordingView View { get; } = new();

    public RecordingStory Story { get; } = new();

    public Battle Battle { get; }

    /// <summary>Creates a battle whose <c>Y 4</c> draws return <paramref name="draws"/> in order.</summary>
    public static ResolutionFixture Create(params int[] draws) => new(draws);

    public UnitRecord Unit(int id) => State.Units[id];

    public CmdrRecord Cmdr(int id) => State.Cmdrs[id];

    /// <summary>The events of type <typeparamref name="T"/>, in order.</summary>
    public List<T> Events<T>() where T : ScriptEvent => View.Events.OfType<T>().ToList();

    /// <summary>
    /// Asserts that the draws begin with <paramref name="expected"/>. Used where a test runs into
    /// the flavour text (part R2): before the merge its stub throws, after the merge FIGT_M's first
    /// draw exhausts the scripted values and throws — either way the draws up to the call are fixed.
    /// </summary>
    public void AssertDrawsStartWith(params int[] expected) =>
        Assert.Equal(expected, Random.Draws.Take(expected.Length));

    /// <summary>
    /// Runs <paramref name="action"/> into the flavour text (part R2) and asserts that it stopped
    /// there: the stub throws <see cref="NotImplementedException"/> before the merge; after it,
    /// FIGT_M's first draw finds no scripted value.
    /// </summary>
    public async Task RunIntoFlavourAsync(Func<Task> action)
    {
        var e = await Assert.ThrowsAnyAsync<Exception>(action);
        Assert.True(
            e is NotImplementedException
                || (e is InvalidOperationException && e.Message.StartsWith("Unexpected draw", StringComparison.Ordinal)),
            $"Stopped by {e.GetType().Name}: {e.Message}");
    }
}

