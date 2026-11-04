using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneBattlePortrait"/>: our battle screen on the phone held upright (docs/ui/phone-build.md, step P3), at the
/// mock-ups' figures on the reference phone (docs/ui/phone.md, §6, PB3, PB3r, PB3x).
/// </summary>
public class PhoneBattlePortraitTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Portrait(1220, 2712);

    /// <summary>The scene's foot on the reference phone: the 1024 × 410 scene at its width.</summary>
    private const float Scene = 410f * 1220 / 1024;

    private static float Height => PhoneBattlePortrait.Height(Reference);

    /// <summary>A box of the mock-ups' reference pixels from the room's top (the mock-ups' y less their TOP, 110) as the board's layout pixels.</summary>
    private static PhoneFrame.Box Board(float x, float y, float w, float h)
    {
        const float k = 1280f / 1220;
        return new(x * k, y * k, w * k, h * k);
    }

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    [Fact]
    public void TheBoardSpansTheRoomAtThePcsWidth()
    {
        Assert.Equal(1220f / 1280, PhoneBattlePortrait.Scale(Reference), 0.0001f);
        Assert.Equal(2712 - 110 - 50, Height * PhoneBattlePortrait.Scale(Reference), Close);
        Equal(new(0, 0, 1280, 512.5f), PhoneBattlePortrait.Scene);
        Equal(new(16, 110 + Scene + 1138, 300, 120), PhoneBattlePortrait.OnScreen(Reference, PhoneBattlePortrait.Skip));
    }

    [Fact]
    public void OurRowsAreOnTopTheEnemysAtTheThumbTheGaugeBetweenTheFrontRows()
    {
        Equal(Board(16, Scene + 52, 586, 230), PhoneBattlePortrait.Card(0, 0));
        Equal(Board(618, Scene + 52, 586, 230), PhoneBattlePortrait.Card(0, 1));
        Equal(Board(16, Scene + 292, 586, 230), PhoneBattlePortrait.Card(1, 0));
        Equal(Board(16, Scene + 532, 1188, 72), PhoneBattlePortrait.Gauge);
        Equal(Board(16, Scene + 614, 586, 230), PhoneBattlePortrait.Card(2, 0));
        Equal(Board(618, Scene + 854, 586, 230), PhoneBattlePortrait.Card(3, 1));
        Equal(Board(24, Scene + 6, 1172, 40), PhoneBattlePortrait.Names(true));
        Equal(Board(24, Scene + 1092, 1172, 40), PhoneBattlePortrait.Names(false));
        Assert.Equal(PhoneBattlePortrait.Layout(230), 2 * PhoneBattlePortrait.Layout(PhoneBattlePortrait.CardPad)
            + PhoneBattlePortrait.Layout(PhoneBattlePortrait.Face) + PhoneBattlePortrait.Layout(PhoneBattlePortrait.StatsRow), Close);
    }

    [Fact]
    public void SkipHasAStripOfItsOwnOverTheDeck()
    {
        Equal(Board(16, Scene + 1138, 300, 120), PhoneBattlePortrait.Skip);
        Assert.Equal(Board(0, Scene + 1270, 0, 0).Y, PhoneBattlePortrait.DeckTopY, Close);
        var deck = PhoneBattlePortrait.Deck(Height);
        Assert.Equal(PhoneBattlePortrait.DeckTopY, deck.Y, Close);
        Assert.Equal(Height, deck.Bottom, Close);
    }

    [Fact]
    public void TheCommandsRiseFromTheFootTheAttacksStandAtTheRightThumb()
    {
        var rows = PhoneBattlePortrait.CommandRows(Height, 5);
        float h = (2552 - 20 - (Scene + 1270 + 110) - 4 * 12) / 5;              // the mock-up's 123 on the reference phone
        Equal(Board(16, 2532 - h, 586, h), rows[0]);
        Equal(Board(16, Scene + 1270 + 110, 586, h), rows[4]);
        Equal(Board(618, 2532 - 250, 586, 250), PhoneBattlePortrait.AttackRows(Height, 1)[0]);
        var attacks = PhoneBattlePortrait.AttackRows(Height, 3);
        Assert.True(attacks[2].Y >= PhoneBattlePortrait.DeckTopY + PhoneBattlePortrait.Layout(110) - Close);
        Assert.Equal(attacks[0].Y - attacks[1].Bottom, PhoneBattlePortrait.Layout(12), Close);
        Assert.Equal(PhoneBattlePortrait.Layout(132), PhoneBattlePortrait.CommandRows(Height, 2)[0].H, Close);
        Assert.Empty(PhoneBattlePortrait.AttackRows(Height, 0));
    }

    [Fact]
    public void TheRetreatQuestionStandsInTheScreensMiddle()
    {
        var question = PhoneBattlePortrait.RetreatQuestion(-100, Height + 300);
        Assert.Equal((Height + 200) / 2, question.Y + question.H / 2, Close);          // the screen's middle, not the board's
        Equal(Board(40, 0, 1140, 330) with { Y = question.Y }, question);
        var answers = PhoneBattlePortrait.RetreatAnswers(question, 2);
        float top = question.Y + PhoneBattlePortrait.Layout(168);
        Equal(Board(60, 0, 544, 138) with { Y = top }, answers[0]);
        Equal(Board(616, 0, 544, 138) with { Y = top }, answers[1]);
    }
}
