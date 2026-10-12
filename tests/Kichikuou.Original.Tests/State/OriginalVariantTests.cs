using Kichikuou.Engine.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Tests.State;

/// <summary>The facts the engine takes from the original (<see cref="OriginalVariant"/>).</summary>
public class OriginalVariantTests
{
    /// <summary>The skip skips the opening whole, also when another scene runs inside it, and only while it is on; the rebellion after it is fast-forwarded as usual.</summary>
    [Fact]
    public void Only_the_opening_is_skipped_whole()
    {
        var skip = new MessageSkip([]);
        Assert.False(skip.SkipsScene(["ｵｰﾌﾟﾆﾝｸﾞ.ADV"], OriginalVariant.WholeScenes));                     // not skipping
        skip.Toggle();
        Assert.True(skip.SkipsScene(["ｵｰﾌﾟﾆﾝｸﾞ.ADV"], OriginalVariant.WholeScenes));
        Assert.True(skip.SkipsScene(["Iｱﾘｽ.ADV", "ｵｰﾌﾟﾆﾝｸﾞ.ADV"], OriginalVariant.WholeScenes));
        Assert.False(skip.SkipsScene(["Iﾊﾝﾗﾝ.ADV"], OriginalVariant.WholeScenes));
        Assert.False(skip.SkipsScene(["Iﾄｳｿﾞｸ.ADV"], OriginalVariant.WholeScenes));
        Assert.False(skip.SkipsScene(["Iｼﾚｲﾌﾞ.ADV"], OriginalVariant.WholeScenes));
        Assert.False(skip.SkipsScene([], OriginalVariant.WholeScenes));
    }
}
