using Kichikuou.Core.Script;

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

    [Fact]
    public void An_unseen_message_stops_skipping_and_disables_it_until_a_seen_one()
    {
        var recorded = new List<string>();
        var skip = new MessageSkip([MessageSkip.Key(Seen)], recorded.Add);
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
            second.Activate(true);
            second.OnMessage(New);
            Assert.True(second.IsSkipping);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
