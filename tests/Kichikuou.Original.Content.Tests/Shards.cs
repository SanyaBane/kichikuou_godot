using System.Reflection;
using Xunit.Sdk;

namespace Kichikuou.Original.Content.Tests;

// xUnit runs the tests of one class one after another and the classes in parallel, so a theory over many cases (a
// capture each) is spread over several classes: an abstract class holds the theory with ShardedData, and each shard
// is a class derived from it with its Shard attribute (a fact that loops over its runs reads its Shard itself). A
// theory with no case fails, so a theory has no more shards than the quick run's cases. A test class of facts that
// each take minutes in the full run puts each fact in a nested class of its own. Whatever the shards share must be
// safe on several threads.

/// <summary>The shard a test class runs of its theories' <see cref="ShardedDataAttribute"/> cases.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ShardAttribute(int index) : Attribute
{
    public int Index { get; } = index;
}

/// <summary>
/// The cases of the static property <paramref name="member"/> of <paramref name="type"/> in the shard of the test's
/// class (its <see cref="ShardAttribute"/>): every <paramref name="shards"/>-th case, from the shard's index on.
/// </summary>
public sealed class ShardedDataAttribute(string member, Type type, int shards) : DataAttribute
{
    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var shard = testMethod.ReflectedType?.GetCustomAttribute<ShardAttribute>()
                    ?? throw new InvalidOperationException($"{testMethod.ReflectedType?.Name} has no [Shard].");
        var cases = (IEnumerable<object[]>)(type.GetProperty(member, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException($"{type.Name}.{member} is no static property.")).GetValue(null)!;
        return cases.Where((_, i) => i % shards == shard.Index);
    }
}
