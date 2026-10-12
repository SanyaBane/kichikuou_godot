using Kichikuou.Engine.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Core.Tests.Script;

public class MessageSkipTests
{
    private static readonly ScriptRef Seen = new("Iｼﾚｲﾌﾞ.ADV", 75);
    private static readonly ScriptRef New = new("Iｼﾚｲﾌﾞ.ADV", 94);

    [Fact]
    public void Skipping_runs_through_seen_messages()
    {
        var skip = new MessageSkip([MessageSkip.Key(Seen)]);
        Assert.False(skip.IsSkipping);
        skip.Toggle();
        Assert.True(skip.IsSkipping);
        skip.OnMessage(Seen);
        Assert.True(skip.IsSkipping);
        skip.Toggle();
        Assert.False(skip.IsSkipping);
    }

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

    /// <summary>Option → MsgSkip Speeds Up Fades (ours, on by default): only while skipping, and only shortening.</summary>
    [Fact]
    public void Fades_are_cut_short_only_while_skipping_with_the_option()
    {
        var skip = new MessageSkip([]);
        var fade = TimeSpan.FromMilliseconds(1066);
        Assert.True(skip.FastFades);
        Assert.Equal(fade, skip.FadeDuration(fade));                      // not skipping: the engine's

        skip.Activate(true);
        Assert.Equal(MessageSkip.SkippedFade, skip.FadeDuration(fade));
        Assert.Equal(TimeSpan.FromMilliseconds(16), skip.FadeDuration(TimeSpan.FromMilliseconds(16)));

        skip.FastFades = false;
        Assert.Equal(fade, skip.FadeDuration(fade));                      // as xsystem35
    }

    [Fact]
    public void An_unseen_message_stops_skipping_and_disables_it_until_a_seen_one()
    {
        var recorded = new List<string>();
        var skip = new MessageSkip([MessageSkip.Key(Seen)], recorded.Add) { SkipUnseen = false };
        int changes = 0;
        skip.Changed += () => changes++;
        skip.Activate(true);

        skip.OnMessage(New);
        Assert.False(skip.Activated);
        Assert.False(skip.Enabled);
        Assert.Equal([MessageSkip.Key(New)], recorded);

        skip.Activate(true);                    // the hot key still switches it on, but it does not skip
        Assert.False(skip.IsSkipping);
        skip.OnMessage(New);                    // seen now
        Assert.True(skip.Enabled);
        Assert.True(skip.IsSkipping);
        Assert.Single(recorded);
        Assert.Equal(5, changes);               // on, off, disabled, on, enabled
    }

    [Fact]
    public void By_default_unseen_messages_are_skipped_too_and_recorded()
    {
        var recorded = new List<string>();
        var skip = new MessageSkip([], recorded.Add);
        skip.Activate(true);
        skip.OnMessage(New);
        Assert.True(skip.IsSkipping);
        Assert.Equal([MessageSkip.Key(New)], recorded);
    }

    [Fact]
    public void Allowing_unseen_messages_enables_skipping_again()
    {
        var skip = new MessageSkip([]) { SkipUnseen = false };
        skip.OnMessage(New);
        Assert.False(skip.Enabled);
        skip.SkipUnseen = true;
        Assert.True(skip.Enabled);
    }

    [Fact]
    public void A_menu_stops_skipping_unless_told_otherwise()
    {
        var skip = new MessageSkip([]);
        skip.Activate(true);
        skip.OnMenu();
        Assert.False(skip.Activated);

        skip.StopOnMenu = false;
        skip.Activate(true);
        skip.OnMenu();
        Assert.True(skip.Activated);
    }

    [Fact]
    public void The_seen_messages_are_kept_in_a_file()
    {
        string dir = Path.Combine(Path.GetTempPath(), "kichikuou-msgskip-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(dir, "msgskip.txt");
        try
        {
            var first = MessageSkip.Open(path);
            first.OnMessage(Seen);
            first.OnMessage(Seen);
            first.OnMessage(New);

            var second = MessageSkip.Open(path);
            Assert.Equal(2, second.SeenCount);
            second.SkipUnseen = false;
            second.Activate(true);
            second.OnMessage(New);
            Assert.True(second.IsSkipping);

            second.ForgetSeen();
            Assert.Equal(0, second.SeenCount);
            Assert.False(File.Exists(path));
            second.OnMessage(New);
            Assert.False(second.IsSkipping);            // unseen again
            Assert.Equal(1, MessageSkip.Open(path).SeenCount);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
