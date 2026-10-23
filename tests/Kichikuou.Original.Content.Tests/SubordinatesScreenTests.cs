using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// Our own Subordinates screen's model (docs/ui/subordinates.md): the orders' panel from GameState, and
/// <see cref="SubordinateOrder"/>'s answers to <c>Cﾌﾞｶ</c>'s and COMMAND's menus on a hand-made way.
/// </summary>
public class SubordinatesScreenTests
{
    /// <summary>
    /// Stage 1, week 18, 1141 (×100K) in the budget, both orders left: Rance (unit 1) deployed this week,
    /// Haurein (unit 12) a soldiers' unit at 50 of 1400, Asuka (unit 11) full, Mill (unit 25) offered nothing.
    /// </summary>
    private static GameState World()
    {
        var s = new GameState { Unit_StrBase = 20, StoryStage = 1, Week = 18, Gold = 1141 };
        (s.Ui_CmdSubordAvail, s.CmdAvail_SubTroops, s.CmdAvail_SubTalk) = (1, 1, 1);
        void Unit(int n, string name, int status, int troops, int max, int cls, int troopAtk)
        {
            s.Strings[20 + (n - 1) * 3] = name;
            var u = s.Units[n];
            (u.Status, u.Troops, u.MaxTroops, u.Class, u.TroopAtk) =
                ((Sys3Int)status, (Sys3Int)troops, (Sys3Int)max, (Sys3Int)cls, (Sys3Int)troopAtk);
            u.FaceCg = (Sys3Int)n;
            u.PortraitCg = (Sys3Int)(600 + n);
        }
        Unit(1, "Rance", 2, 949, 1000, 0, 10);
        Unit(11, "Asuka", 1, 100, 100, 2, 3);
        Unit(12, "Haurein", 1, 50, 1400, 0, 8);
        Unit(25, "Mill", 1, 10, 100, 0, 8);
        return s;
    }

    private static SceneMenuItem Item(string file, int line) => new(new ScriptRef(file, line), []);

    /// <summary><c>Cﾌﾞｶ.ADV</c>'s troop menu with both orders left: Info, ○Replenish, ○Add, ○Talk, Do Nothing.</summary>
    private static readonly SceneMenuItem[] TroopMenu =
        [.. new[] { 807, 812, 817, 836, 847 }.Select(l => Item("Cﾌﾞｶ.ADV", l))];

    /// <summary>COMMAND's menu of a Leazas week with one Subordinates order left (○, <c>:276</c>) among others.</summary>
    private static readonly SceneMenuItem[] CommandMenu =
        [Item("COMMAND.ADV", 233), Item("COMMAND.ADV", 250), Item("COMMAND.ADV", 276), Item("COMMAND.ADV", 310)];

    [Fact]
    public void The_panel_offers_replenish_and_add_mores_steps_with_the_dearer_ones_greyed()
    {
        var panel = SubordinatesScreen.Panel(World(), 12);

        Assert.Equal(("Haurein", 12, 612, 1141), (panel.Name, panel.FaceCg, panel.PortraitCg, panel.Gold));
        Assert.Equal(new SubordinateButton(true, 1350, 27, ""), panel.Replenish);
        Assert.True(panel.AddOffered);
        Assert.Equal([(25, 54, true), (50, 162, true), (100, 486, true), (200, 1458, false), (400, 4374, false), (800, 13122, false)],
            panel.Steps.Select(b => (b.Men, b.Price, b.Enabled)));
        Assert.All(panel.Steps.Where(b => !b.Enabled), b => Assert.Equal("over the budget", b.Note));
        Assert.True(panel.Talk.Enabled);
    }

    [Fact]
    public void Greyed_orders_say_why()
    {
        var s = World();
        Assert.Equal("deployed this week", SubordinatesScreen.Panel(s, 1).Replenish.Note);   // Rance's pick opens the status page
        Assert.False(SubordinatesScreen.Panel(s, 1).Talk.Enabled);
        Assert.Equal("full", SubordinatesScreen.Panel(s, 11).Replenish.Note);                // refused by the script
        Assert.False(SubordinatesScreen.Panel(s, 25).AddOffered);                            // Mill: no new troops
        Assert.Empty(SubordinatesScreen.Panel(s, 25).Steps);

        s.CmdAvail_SubTroops = 0;
        var used = SubordinatesScreen.Panel(s, 12);
        Assert.Equal("used this week", used.Replenish.Note);
        Assert.All(used.Steps, b => Assert.Equal((false, "used this week"), (b.Enabled, b.Note)));
        Assert.True(used.Talk.Enabled);

        s.StoryStage = 0;                                                                   // the prologue: only Talk
        s.CmdAvail_SubTroops = 1;
        Assert.Equal("after the prologue", SubordinatesScreen.Panel(s, 12).Replenish.Note);
        Assert.True(SubordinatesScreen.Panel(s, 12).Talk.Enabled);

        s.Ui_CmdSubordAvail = 0;                                                            // the view-only picker
        Assert.False(SubordinatesScreen.Panel(s, 12).Talk.Enabled);
    }

    [Fact]
    public void The_first_choice_is_the_first_unit_that_can_act()
    {
        Assert.Equal(11, SubordinatesScreen.FirstChoice(World()));                          // Rance (unit 1) fought this week
    }

    [Fact]
    public void Replenish_clicks_the_cell_answers_the_menus_and_opens_the_command_again()
    {
        var s = World();
        var pointer = new PointerScript();
        var order = SubordinateOrder.Start(s, SubordinateOrderKind.Replenish, 12, 0, pointer);

        Assert.Equal(1, pointer.Pending);                                                   // the click on Haurein's cell
        Assert.Equal(2, order.Choose(TroopMenu));                                           // ○Replenish Troops
        Assert.True(order.AnswerKey());                                                     // the offer's windows
        Assert.Equal(1, order.Choose([Item("Cﾌﾞｶ.ADV", 1026), Item("Cﾌﾞｶ.ADV", 1030)]));    // Do
        Assert.True(order.AnswerKey());                                                     // "Replenish the troops.", the result
        s.Units[12].Troops = 1400;
        s.Gold = 1114;
        s.CmdAvail_SubTroops = 0;

        Assert.True(order.OnMapWait());                                                    // COMMAND's map: the right click
        Assert.Equal(3, order.Choose(CommandMenu));                                         // ○Subordinates
        Assert.True(order.Running);
        order.OnPicker(new PickerScreenEvent(PickerKind.Subordinates, 2, true));            // Sﾌﾞｶ again

        Assert.False(order.Running);
        Assert.False(order.AnswerKey());
        var result = order.Result(s)!;
        Assert.Equal(new SubordinateResult(SubordinateOrderKind.Replenish, "Haurein", "", 12, 50, 1400, 1400, 1400, 27, 1114), result);
        Assert.Equal(("Troops Replenished", "Haurein", "50 → 1,400", "+1,350", "−2,700K", "111,400K"),
            (result.Title, result.Who, result.TroopsLine, result.Gain, result.CostLine, result.BudgetLine));
    }

    [Fact]
    public void Add_troops_asks_for_add_more_as_many_times_as_its_step_then_does_it()
    {
        var s = World();
        var order = SubordinateOrder.Start(s, SubordinateOrderKind.AddTroops, 12, 2, new PointerScript());
        SceneMenuItem[] offer = [Item("Cﾌﾞｶ.ADV", 904), Item("Cﾌﾞｶ.ADV", 909), Item("Cﾌﾞｶ.ADV", 914)];

        Assert.Equal(3, order.Choose(TroopMenu));                                           // ○Add New Troops
        Assert.Equal(2, order.Choose(offer));                                               // Add More
        Assert.Equal(2, order.Choose(offer));                                               // Add More
        Assert.Equal(1, order.Choose(offer));                                               // Do It
        s.Units[12].Troops = 150;
        s.Units[12].MaxTroops = 1500;
        s.Gold = 1141 - 486;

        Assert.True(order.OnMapWait());
        var result = order.Result(s)!;
        Assert.Equal(new SubordinateResult(SubordinateOrderKind.AddTroops, "Haurein", "", 12, 50, 150, 1400, 1500, 486, 655), result);
        Assert.Equal(("New Troops", "50/1,400 → 150/1,500", "+100", "−48,600K", "65,500K"),
            (result.Title, result.TroopsLine, result.Gain, result.CostLine, result.BudgetLine));
    }

    [Fact]
    public void Talk_lets_go_after_rances_thought_and_does_not_open_the_command_again()
    {
        var s = World();
        var order = SubordinateOrder.Start(s, SubordinateOrderKind.Talk, 12, 0, new PointerScript());

        Assert.Equal(4, order.Choose(TroopMenu));                                           // ○Talk
        Assert.True(order.AnswerKey());                                                     // "Hm... Maybe I'll meet with Haurein."
        Assert.False(order.Running);                                                        // the talk scene is the player's
        Assert.False(order.AnswerKey());
        Assert.Null(order.Result(s));
        Assert.False(order.OnMapWait());
    }

    [Fact]
    public void A_prologue_talk_is_over_when_the_picker_closes()
    {
        var s = World();
        s.StoryStage = 0;
        var order = SubordinateOrder.Start(s, SubordinateOrderKind.Talk, 12, 0, new PointerScript());

        order.OnPicker(new PickerScreenEvent(PickerKind.Subordinates, 3, false));

        Assert.False(order.Running);
    }

    [Fact]
    public void An_order_the_menu_does_not_offer_is_answered_with_do_nothing()
    {
        var s = World();
        var order = SubordinateOrder.Start(s, SubordinateOrderKind.Replenish, 12, 0, new PointerScript());
        SceneMenuItem[] spent = [.. new[] { 807, 824, 829, 836, 847 }.Select(l => Item("Cﾌﾞｶ.ADV", l))];   // ×Replenish, ×Add

        Assert.Equal(5, order.Choose(spent));                                               // Do Nothing: back to the picker
        Assert.False(order.Running);
        Assert.Null(order.Result(s));
    }

    [Fact]
    public void A_budget_refusal_goes_back_to_the_picker()
    {
        var s = World();
        var order = SubordinateOrder.Start(s, SubordinateOrderKind.Replenish, 12, 0, new PointerScript());
        order.Choose(TroopMenu);
        order.Choose([Item("Cﾌﾞｶ.ADV", 1026), Item("Cﾌﾞｶ.ADV", 1030)]);

        Assert.Equal(5, order.Choose(TroopMenu));                                           // "we don't have the budget": the menu again
        Assert.False(order.Running);
        Assert.Null(order.Result(s));
    }
}
