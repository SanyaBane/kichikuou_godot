namespace Kichikuou.Engine.Script;

public abstract partial class ScriptState
{
    /// <summary>
    /// Makes <paramref name="target"/>, a state of the same variant, a copy of this one: the memory
    /// (<see cref="Sys3.ScriptMemory.CopyTo"/>: the variables with <see cref="RetVal"/>, every array page at its
    /// size, the strings) and the port's own data (<see cref="Extras"/>, each array copied). That is the whole
    /// state: the <c>DS</c> bindings are the variant's (<see cref="BindingOf"/>, fixed by its state type) and the
    /// profile is shared; what a run also depends on — the random generator, the files' calls, the engine's
    /// counters — is the context's. Copying into one target again reuses its pages (docs/ui/talk-marks.md §8, the dry run).
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="target"/> is a state of another type.</exception>
    public void CopyTo(ScriptState target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.GetType() != GetType())
            throw new ArgumentException($"A {GetType().Name} cannot be copied into a {target.GetType().Name}.", nameof(target));
        if (ReferenceEquals(target, this))
            return;
        Memory.CopyTo(target.Memory);
        target.Extras.Clear();
        foreach (var (key, values) in Extras)
            target.Extras[key] = (int[])values.Clone();
    }

    /// <summary>A new state that is a copy of <paramref name="source"/> (<see cref="CopyTo"/>).</summary>
    public static T Copy<T>(T source) where T : ScriptState, new()
    {
        ArgumentNullException.ThrowIfNull(source);
        var copy = new T();
        source.CopyTo(copy);
        return copy;
    }
}
