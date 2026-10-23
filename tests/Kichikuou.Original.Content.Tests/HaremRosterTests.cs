using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// Our own Harem screen (docs/ui/pickers.md, mock-up H2): <see cref="HaremRoster"/> on hand-made states, and its
/// gestures answering <c>Cﾊｰﾚﾑ</c> on the booted new game moved to stage 1 — the picker still hit-tests the click and
/// the command plays her scene. The script tests are skipped without the decompile.
/// </summary>
public class HaremRosterTests(ITestOutputHelper output)
{
    private static readonly string[] Names = ["Sill", "Lia", "Maris", "Suzume", "Wendy", "Kanami"];

    [Fact]
    public void The_girls_on_offer_come_in_their_numbers_order_with_their_counts()
    {
        var s = new GameState();
        s.StoryStage = 1;
        s.HaremAvail_Lia = 1;
        s.HaremAvail_Maris = 1;
        s.HaremAvail_Kanami = 1;
        s.History[3].Harem_SexCount = 4;
        s.History[3].Harem_LastWeek = 13;

        var girls = HaremRoster.Girls(s, Names);

        Assert.Equal([2, 3, 6], girls.Select(g => g.Number));
        Assert.Equal(new HaremGirl(3, "Maris", 4, 13, 703), girls[1]);
        Assert.Equal(new HaremGirl(6, "Kanami", 0, 0, 706), girls[2]);
    }

    [Fact]
    public void The_prologue_has_pictures_for_Sill_Soul_and_Anasel_only_and_grown_up_Mill_her_own()
    {
        var s = new GameState();
        Assert.Equal((699, 698, 697, 0), (HaremRoster.PictureOf(s, 1), HaremRoster.PictureOf(s, 11), HaremRoster.PictureOf(s, 12), HaremRoster.PictureOf(s, 2)));
        Assert.Equal((694, 696), HaremRoster.Background(s));
        s.StoryStage = 2;
        s.F_Mill_State = 2;
        Assert.Equal((695, 709), (HaremRoster.PictureOf(s, 18), HaremRoster.PictureOf(s, 9)));
        Assert.Equal((818, 700), HaremRoster.Background(s));
    }

    [Fact]
    public void The_first_choice_keeps_the_chosen_girl_then_the_last_summoned_then_the_first()
    {
        var s = new GameState();
        var girls = new[] { new HaremGirl(2, "Lia", 0, 0, 702), new HaremGirl(6, "Kanami", 0, 0, 706) };
        Assert.Equal(6, HaremRoster.FirstChoice(s, girls, 6));
        s.Harem_LastPick = 6;
        Assert.Equal(6, HaremRoster.FirstChoice(s, girls, 3));
        s.Harem_LastPick = 9;
        Assert.Equal(2, HaremRoster.FirstChoice(s, girls, 0));
        Assert.Equal(0, HaremRoster.FirstChoice(s, [], 0));
    }

    [Fact]
    public void A_date_reads_as_the_status_window_has_it_and_week_0_is_never()
    {
        Assert.Equal("LP 3 · Month 5 · Week 2", HaremRoster.DateText(18));
        Assert.Equal("LP 4 · Month 1 · Week 1", HaremRoster.DateText(49));
        Assert.Null(HaremRoster.DateText(0));
    }

    /// <summary>The booted game at stage 1, week 18, with the week's visit left; nobody met yet (no first meeting plays).</summary>
    private static async Task<EndGame> GameAsync()
    {
        var game = await EndGame.BootAsync();
        var s = game.State;
        s.StoryStage = 1;
        s.Week = 18;
        s.Ui_CmdHaremAvail = 1;
        return game;
    }

    [ScenarioFact]
    public async Task Summon_picks_her_cell_and_her_visit_is_recorded()
    {
        var game = await GameAsync();
        var s = game.State;
        bool clicked = false;
        game.Input.Idle = () =>
        {
            if (!clicked && s.HaremAvail_Maris != 0)
            {
                clicked = true;
                HaremRoster.Summon(3, game.Input.Pointer);           // Maris (3), always on offer after the prologue
            }
        };
        for (int i = 0; i < 16; i++)
            game.Input.Menu.Enqueue(_ => 0);

        await game.Script.CallFileAsync("Cﾊｰﾚﾑ.ADV");

        output.WriteLine($"menus {game.Input.Menus.Count}; visits {s.History[3].Harem_SexCount.Value}");
        Assert.True(clicked);
        Assert.Equal(3, s.Harem_LastPick.Value);
        Assert.Equal(18, s.History[3].Harem_LastWeek.Value);
        Assert.Equal(0, s.Ui_CmdHaremAvail.Value);
    }

    [ScenarioFact]
    public async Task End_leaves_the_picker_and_gives_the_visit_back()
    {
        var game = await GameAsync();
        var s = game.State;
        bool clicked = false;
        game.Input.Idle = () =>
        {
            if (!clicked && s.HaremAvail_Lia != 0)
            {
                clicked = true;
                HaremRoster.End(game.Input.Pointer);
            }
        };

        await game.Script.CallFileAsync("Cﾊｰﾚﾑ.ADV");

        Assert.True(clicked);
        Assert.Equal(1, s.Ui_CmdHaremAvail.Value);
        Assert.Equal(0, s.Harem_LastPick.Value);
        Assert.Empty(game.Input.Menus);
    }
}
