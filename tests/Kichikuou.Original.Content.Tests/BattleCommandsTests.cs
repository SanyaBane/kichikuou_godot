using Kichikuou.Content;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Kichikuou.Original.State;
using Kichikuou.TestSupport;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// Our own command input on the battle screen (docs/ui/battle-screen.md, step 4, "Next — our own command
/// input"): the banners on an enemy's card and the cards' context menus, from the core's own menu.
/// </summary>
public class BattleCommandsTests(ITestOutputHelper output)
{
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    private static BattlePlace Enemy(int slot) => new(BattleSide.Enemy, slot);
    private static BattlePlace Own(int slot) => new(BattleSide.Player, slot);

    /// <summary>
    /// The core's <c>FIGT_SEL</c> menu for <paramref name="unit"/> of class <paramref name="cls"/> acting from
    /// <paramref name="slot"/>, with two own units in front (and the actor), and <paramref name="enemies"/> enemies.
    /// </summary>
    private static FigtSelMenu Menu(int unit, int cls, int slot = 1, int enemies = 4, int focus = 0, int item = 0, Action<GameState>? set = null)
    {
        var s = new GameState();
        var battle = new Battle(s, new NoRandom(), new NullView(), new NoBattleInput(), new NoHooks());
        int used = Math.Max(slot, 2);
        for (int i = 1; i <= used; i++)
            Slot(s, BattleSide.Player, i, i == slot ? unit : 40 + i);
        for (int i = 1; i <= enemies; i++)
            Slot(s, BattleSide.Enemy, i, 100 + i);
        (s.Bt_PlayerSlotsUsed, s.Bt_EnemySlotsUsed) = ((Sys3Int)used, (Sys3Int)enemies);
        s.Units[unit].Class = (Sys3Int)cls;
        s.Units[unit].Item = (Sys3Int)item;
        s.Unit_Cur = (Sys3Int)unit;
        s.Bt_PlayerActSlot = (Sys3Int)slot;
        s.Bt_PlayerAtkType = cls == 1 ? (Sys3Int)1 : cls is 2 or 3 or 11 ? (Sys3Int)2 : Sys3Int.Zero;
        s.Bt_ActFocus = (Sys3Int)focus;
        set?.Invoke(s);
        return battle.BuildFigtSelMenu();
    }

    private static void Slot(GameState s, BattleSide side, int slot, int record)
    {
        var v = (Sys3Int)record;
        switch (side, slot)
        {
            case (BattleSide.Player, 1): s.Bt_PlayerSlot1 = v; break;
            case (BattleSide.Player, 2): s.Bt_PlayerSlot2 = v; break;
            case (BattleSide.Player, 3): s.Bt_PlayerSlot3 = v; break;
            case (BattleSide.Player, 4): s.Bt_PlayerSlot4 = v; break;
            case (BattleSide.Enemy, 1): s.Bt_EnemySlot1 = v; break;
            case (BattleSide.Enemy, 2): s.Bt_EnemySlot2 = v; break;
            case (BattleSide.Enemy, 3): s.Bt_EnemySlot3 = v; break;
            default: s.Bt_EnemySlot4 = v; break;
        }
    }

    private static string[] Codes(IEnumerable<BattleCommand> commands) =>
        [.. commands.Select(c => $"{c.Choice.Command.Value}:{c.Choice.Argument.Value}{(c.Current ? "*" : "")}")];

    private static string[] Labels(IEnumerable<BattleCommand> commands) =>
        [.. commands.Select(c => c.Name ?? Texts.Value.Read(c.Label).Format([], _ => ""))];

    [Fact]
    public void A_melee_unit_in_front_attacks_the_enemy_front_row_directly_and_nothing_from_the_back()
    {
        var front = new BattleCommands(Menu(5, 0, slot: 1));
        Assert.Equal(["1:4"], Codes(front.Banners(Enemy(1))));
        Assert.Equal(["1:5"], Codes(front.Banners(Enemy(2))));
        Assert.Empty(front.Banners(Enemy(3)));                              // the enemy's back row: out of reach
        Assert.Empty(front.Banners(Own(2)));

        var back = new BattleCommands(Menu(5, 0, slot: 3));
        Assert.Empty(back.Banners(Enemy(1)));
    }

    [Fact]
    public void A_bowman_s_own_attack_comes_first_then_direct_where_it_reaches()
    {
        var bow = new BattleCommands(Menu(5, 1, slot: 2));
        Assert.Equal(["2:4", "1:4"], Codes(bow.Banners(Enemy(1))));
        Assert.Equal(["2:7"], Codes(bow.Banners(Enemy(4))));
        Assert.Equal(["Bow", "Melee"], Labels(bow.Banners(Enemy(1))));

        var mage = new BattleCommands(Menu(17, 2, slot: 1, focus: 1));     // White Destruction Beam with focus
        Assert.Equal(["3:5", "1:5", "13:5"], Codes(mage.Banners(Enemy(2))));
        Assert.Equal(["Magic", "Melee", "WD Beam"], Labels(mage.Banners(Enemy(2))));
        Assert.Equal(["3:6", "13:6"], Codes(mage.Banners(Enemy(3))));
    }

    [Fact]
    public void Rance_Attack_comes_with_focus_only()
    {
        Assert.Equal(["1:4"], Codes(new BattleCommands(Menu(1, 0)).Banners(Enemy(1))));
        var focused = new BattleCommands(Menu(1, 0, focus: 1));
        Assert.Equal(["1:4", "11:4"], Codes(focused.Banners(Enemy(1))));
        Assert.Equal(["Melee", "Rance Attack"], Labels(focused.Banners(Enemy(1))));
    }

    [Fact]
    public void The_acting_unit_s_menu_holds_the_commands_without_a_target()
    {
        var commands = new BattleCommands(Menu(5, 0, slot: 2, item: 28));
        var menu = commands.ContextMenu(Own(2));
        Assert.Equal(["4:99", "5:99", "32:99", "8:99", "9:99", "6:99"], Codes(menu));
        Assert.Equal(["Defend", "Wait", "Fog Card", "Auto-Atk", "Auto-Def", "Retreat"], Labels(menu));

        var capsule = new BattleCommands(Menu(5, 0, item: 27)).ContextMenu(Own(1));
        Assert.Equal(["4:99", "5:99", "30:99", "8:99", "9:99", "6:99"], Codes(capsule));
        var duel = new BattleCommands(Menu(5, 0, item: 28, set: s => s.Bt_IsDuel = 1)).ContextMenu(Own(1));
        Assert.DoesNotContain(duel, c => c.Choice.Command == FigtSelCommand.FogCard);   // the core leaves the Fog Card out of duels
        var debug = new BattleCommands(Menu(5, 0, set: s => s.DebugMode = 1)).ContextMenu(Own(1));
        Assert.Equal("7:99", Codes(debug)[^1]);
    }

    [Fact]
    public void Another_own_card_gives_its_standing_orders_with_the_current_marked()
    {
        var commands = new BattleCommands(Menu(5, 0, slot: 2));
        var menu = commands.ContextMenu(Own(1), StandingOrder.AutoAttack);
        Assert.Equal(["50:1", "51:1*", "52:1"], Codes(menu));
        Assert.Equal(["Manual", "Auto-Atk", "Auto-Def"], Labels(menu));
        Assert.Empty(commands.ContextMenu(Own(4)));                         // no unit there: the core allows nothing
    }

    /// <summary>Every card shows its stats (owner, 2026-10-15): no menu offers Info, an enemy's card has none.</summary>
    [Fact]
    public void No_card_offers_info_and_an_enemy_card_has_no_menu()
    {
        var commands = new BattleCommands(Menu(5, 0, enemies: 3));
        Assert.Empty(commands.ContextMenu(Enemy(3)));
        foreach (var place in new[] { Own(1), Own(2) })
            Assert.DoesNotContain(commands.ContextMenu(place, StandingOrder.Manual),
                c => c.Choice.Command == FigtSelCommand.UnitInfo || c.Choice.Command == FigtSelCommand.EnemyInfo);
    }

    /// <summary>
    /// Every <c>FIGT_SEL</c> answer of the captured battles can be given on our screen: an attack by a banner
    /// on its target's card, any other command by an item of a card's menu (the commands without a target with
    /// 99 in ours, whatever button the original's pointer was on) — but Info, whose stats every card shows.
    /// </summary>
    [Theory]
    [MemberData(nameof(Captures))]
    public async Task Every_captured_answer_is_offered_on_a_card(string file)
    {
        if (file == "")
            return;
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var missing = new List<string>();
        int answers = 0;
        void Check(FigtSelMenu menu, FigtSelChoice choice)
        {
            answers++;
            var commands = new BattleCommands(menu);
            var offered = new List<BattleCommand>();
            for (int slot = 1; slot <= 4; slot++)
            {
                offered.AddRange(commands.Banners(Enemy(slot)));
                offered.AddRange(commands.ContextMenu(Enemy(slot)));
                offered.AddRange(commands.ContextMenu(Own(slot), StandingOrder.Manual));
            }
            foreach (var c in offered)
                Assert.True(menu.Allows(c.Choice), $"{c.Choice} is not allowed by the core");
            int code = choice.Command.Value;
            if (code is 53 or 54)
                return;                                                     // Info: every card shows its stats (BattleCardStats)
            if (code is >= 50 and <= 52 && choice.Argument == menu.ActingSlot)
            {
                // The slot popup on the acting unit: Auto-Atk / Auto-Def become 8 / 9 in the core (OrderOtherAsync),
                // which its menu offers; Manual leaves it as it is (a message, the turn kept), which ours leaves out.
                if (code == 50)
                    return;
                choice = new FigtSelChoice((ushort)(code - 43), BattleCommands.NoButton);
                code = choice.Command.Value;
            }
            bool attack = code is (>= 1 and <= 3) or (>= 10 and <= 21);
            bool targeted = attack || code is >= 50 and <= 52;
            if (!offered.Any(c => c.Choice.Command == choice.Command && (!targeted || c.Choice.Argument == choice.Argument)))
                missing.Add($"({code}, {choice.Argument.Value})");
        }
        var report = await GoldenReplay.RunAsync(capture, commands: Check);
        output.WriteLine($"{file}: {answers} answers; replay {(report.Passed ? "matches" : "diverges")}");
        Assert.Empty(missing.Distinct().Take(10));
    }

    /// <summary>The retreat confirmation the core asks (cancelled here) opens our dialog: its question, Retreat as a warning, Cease Retreat.</summary>
    [Fact]
    public async Task The_retreat_confirmation_is_our_dialog_and_the_enemy_s_attack_menus_are_not()
    {
        var input = new MenuInput();
        var battle = new Battle(new GameState(), new NoRandom(), new NullView(), input, new NoHooks());
        await battle.PlayerRetreatAsync();
        var dialog = BattleDialog.For(Assert.Single(input.Menus))!;
        Assert.Equal(("Retreat the whole army?", "The battle is lost."), (dialog.Question, dialog.Note));
        Assert.Equal(["1:Retreat:True", "2:Cease Retreat:False"],
            dialog.Items.Select(i => $"{i.Code.Value}:{Texts.Value.Read(i.Label).Format([], _ => "")}:{i.Warning}"));

        Assert.Null(BattleDialog.For(new BattleMenu(BattleMenuKind.EnemyAttackResponse, [])));
        Assert.Null(BattleDialog.For(new BattleMenu(BattleMenuKind.NinjaScout, [])));
    }

    public static IEnumerable<object[]> Captures =>
        LocalData.Captures.Any() ? LocalData.Captures.Select(f => new object[] { Path.GetFileName(f) }) : [[""]];

    private sealed class NoBattleInput : UnexpectedInput, IBattleInput
    {
        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu) => throw new InvalidOperationException("Unexpected FIGT_SEL");
        public Task<Sys3Int?> ChooseAsync(BattleMenu menu) => throw new InvalidOperationException("Unexpected menu");
    }

    /// <summary>Cancels every script menu and keeps it.</summary>
    private sealed class MenuInput : UnexpectedInput, IBattleInput
    {
        public List<BattleMenu> Menus { get; } = [];
        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu) => throw new InvalidOperationException("Unexpected FIGT_SEL");

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
        {
            Menus.Add(menu);
            return Task.FromResult<Sys3Int?>(null);
        }
    }
}
