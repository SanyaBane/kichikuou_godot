using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The way out of story stage 4 (milestone 5 step 2; docs/strategy/stage-4-sweep.md, "God's Door and
/// `Iｶﾐ`"), run from Kayblis's death with the rule files routed and the scenes from the player's
/// files. S4: Kayblis dies (<c>死亡捕獲</c>), the story moves to stage 4 and the first Creator phase
/// (<c>Tﾙﾄﾞﾗ</c>) plays <c>Gﾙﾄﾞﾗ</c> 1, which decides Kou's fate. S4+1: <c>COM</c> plays <c>Gｺｳﾋﾒ</c> 6
/// (Kou at 10 → 11), and Rance searches Ho Raga's Tower (dungeon 5) in Siberia (38) through
/// <c>Cﾒｲｷｭｳ</c>, where Ho Raga (<c>Gﾎﾗｶﾞ</c> 1) hands over the Bonsai Statue (item 18) — or refuses
/// it. S4+2: the Ancient Ruins (dungeon 30), entered from the city of Ancient Ruins (35) with Ruins
/// Search (<c>Cｼﾊｲﾄｼ</c>): God's Door found, the four statues placed, the door open, Enter →
/// <c>Iｶﾐ</c> → an ending (<c>OVER</c>, <c>GameEnd</c>).
/// The quest so far is preset at 2 (Ho Raga's and British's scenes with Chaos), Rance holds Chaos
/// (item 1), and the Gourd (item 15), Monkey (item 16) and Flower (item 17) Statues are in stock.
/// Skipped without the decompile.
/// </summary>
public class GodsDoorEndTests(ITestOutputHelper output)
{
    [Fact]
    public async Task With_Kou_at_8_Ho_Raga_s_Bonsai_Statue_opens_God_s_Door_into_Iｶﾐ()
    {
        var game = await WithTheBonsaiStatueAsync();
        if (game is null)
            return;
        var s = game.State;
        await CreatorPhaseAsync(game, 3);                               // S4+1's phase 2: the ruins still spared
        await NewWeekAsync(game);                                       // S4+2
        int atIkami = 0;
        game.OnCall = file =>
        {
            if (file != "Iｶﾐ.ADV")
                return;
            atIkami = s.Prov_Cur.Value;
            game.Input.Pick(EndGame.Garrisons);
        };
        RuinsSearch(game);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 290);                    // God's Door
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3890);                   // the Gourd Statue,
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3897);                   // the Monkey,
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3904);                   // the Flower,
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3911);                   // the Bonsai: the door opens
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3923);                   // Enter
        game.Input.Menu.Enqueue("Iｶﾐ.ADV", 1259);                      // Think,
        game.Input.Menu.Enqueue("Iｶﾐ.ADV", 1259);                      // Think again (Warg at stage 2),
        game.Input.Menu.Enqueue("Iｶﾐ.ADV", 1270);                      // Leave it to Warg

        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => game.Script.CallFileAsync("Cｼﾊｲﾄｼ.ADV"));

        Log(game);
        Assert.Equal("OVER.ADV", jump.File);
        Assert.Equal(1, s.F_GodsDoor_Found.Value);                      // Cﾒｲｷｭｳ.ADV:233-236
        Assert.Equal(6, s.F_GodsDoor_Quest.Value);                      // :4052
        Assert.Equal([1, 1, 1, 1], new[] { s.F_Statue_Gourd, s.F_Statue_Monkey, s.F_Statue_Sunflower, s.F_Statue_Bonsai }
            .Select(f => f.Value));
        Assert.All([15, 16, 17, 18], item => Assert.Equal(0, s.Items[item].Holder.Value));
        Assert.Equal(35, atIkami);                                      // survey-ikami §10 question 2, on this path
        Assert.Equal(11, s.EndingId.Value);                             // every garrison held, Sill (unit 2) alive
        Assert.Equal(1, s.F_Sill_Fate.Value);
        Assert.Equal(20, game.Fights);
    }

    /// <summary>
    /// Kou below 8 when <c>Gﾙﾄﾞﾗ</c> plays (7: she kills herself; 0: she never came): Ho Raga refuses
    /// the statue twice (<c>Gﾎﾗｶﾞ:293-294</c> → <c>L_0096e</c>, quest 2 → 3; then <c>:296-357</c>),
    /// and at the door no hint comes (<c>Cﾒｲｷｭｳ:3874-3876</c> needs Kou past 9): three statues placed,
    /// the door stays shut and Enter is never offered.
    /// </summary>
    [Theory]
    [InlineData(7, 2)]
    [InlineData(0, 0)]
    public async Task With_Kou_below_8_Ho_Raga_keeps_the_statue_and_God_s_Door_stays_shut(int kou, int dead)
    {
        var game = await StageFourAsync(kou);
        if (game is null)
            return;
        var s = game.State;
        Assert.Equal(0, s.F_Kou_State.Value);                           // Gﾙﾄﾞﾗ.ADV:468-470, or never set
        Assert.Equal(dead, s.F_Kouhime_Dead.Value);
        await NewWeekAsync(game);                                       // S4+1
        Assert.DoesNotContain("Gｺｳﾋﾒ.ADV 6", game.Calls);
        HoRagasTower(game);
        game.Input.Menu.Enqueue("Gﾎﾗｶﾞ.ADV", 279);                     // Get Gold Statue: refused, quest 3
        game.Input.Menu.Enqueue("Gﾎﾗｶﾞ.ADV", 279);                     // again: refused
        game.Input.Menu.Enqueue("Gﾎﾗｶﾞ.ADV", 284);                     // Leave

        await game.Script.CallFileAsync("Cﾒｲｷｭｳ.ADV");

        Assert.Equal(3, s.F_GodsDoor_Quest.Value);                      // Gﾎﾗｶﾞ.ADV:511
        Assert.Equal(0, s.Items[18].Holder.Value);
        Assert.Equal(0, s.F_Statue_Bonsai.Value);

        await CreatorPhaseAsync(game, 3);
        await NewWeekAsync(game);                                       // S4+2
        RuinsSearch(game);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 290);                    // God's Door
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3890);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3897);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3904);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3928);                   // Leave,
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 295);                    // Return

        await game.Script.CallFileAsync("Cｼﾊｲﾄｼ.ADV");

        Log(game);
        Assert.Equal(3, s.F_GodsDoor_Quest.Value);                      // no hint: still 3
        Assert.Equal([1, 1, 1, 0], new[] { s.F_Statue_Gourd, s.F_Statue_Monkey, s.F_Statue_Sunflower, s.F_Statue_Bonsai }
            .Select(f => f.Value));
        var door = game.Input.Menus.Where(m => m.Any(at => at == new ScriptRef("Cﾒｲｷｭｳ.ADV", 3928))).ToList();
        Assert.Equal(4, door.Count);
        Assert.DoesNotContain(door, m => m.Contains(new ScriptRef("Cﾒｲｷｭｳ.ADV", 3923)));
        Assert.Equal([new("Cﾒｲｷｭｳ.ADV", 3917), new("Cﾒｲｷｭｳ.ADV", 3928)], door[^1]);    // Rance's talk, Leave
        Assert.DoesNotContain(game.Calls, c => c.StartsWith("Iｶﾐ.ADV", StringComparison.Ordinal));
    }

    /// <summary>
    /// Stage-4-sweep's quirk candidate: placing the Bonsai Statue clears its holder
    /// (<c>Cﾒｲｷｭｳ:4008-4009</c>), so at stage 4 with Kou past 9 Ho Raga offers and gives it again
    /// (<c>Gﾎﾗｶﾞ:276-291, 358-359</c>): the quest falls from 6 back to 5, and Enter
    /// (<c>Cﾒｲｷｭｳ:3921-3925</c>) hides until the statue is placed again.
    /// </summary>
    [Fact]
    public async Task Ho_Raga_gives_the_Bonsai_Statue_again_after_it_is_placed()
    {
        var game = await WithTheBonsaiStatueAsync();
        if (game is null)
            return;
        var s = game.State;
        await CreatorPhaseAsync(game, 3);
        await NewWeekAsync(game);                                       // S4+2: the door opened, then left
        RuinsSearch(game);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 290);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3890);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3897);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3904);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3911);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3928);                   // Leave,
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 295);                    // Return: the week's search given back (L_008bf)
        await game.Script.CallFileAsync("Cｼﾊｲﾄｼ.ADV");
        Assert.Equal(6, s.F_GodsDoor_Quest.Value);
        Assert.Equal(1, s.Ui_CmdDungeonAvail.Value);

        HoRagasTower(game);                                             // the same week
        game.Input.Menu.Enqueue("Gﾎﾗｶﾞ.ADV", 279);                     // Get Gold Statue, offered again
        game.Input.Menu.Enqueue("Gﾎﾗｶﾞ.ADV", 284);
        await game.Script.CallFileAsync("Cﾒｲｷｭｳ.ADV");

        // QUIRK candidate (stage-4-sweep, "Quirk candidates"): the open door's quest goes back to 5.
        Assert.Equal(5, s.F_GodsDoor_Quest.Value);
        Assert.Equal(100, s.Items[18].Holder.Value);
        Assert.Equal(1, s.F_Statue_Bonsai.Value);

        await CreatorPhaseAsync(game, 4);
        await NewWeekAsync(game);                                       // S4+3
        int menus = game.Input.Menus.Count;
        RuinsSearch(game);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 290);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3911);                   // the Bonsai placed again: open
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3928);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 295);
        await game.Script.CallFileAsync("Cｼﾊｲﾄｼ.ADV");

        Log(game);
        var door = game.Input.Menus.Skip(menus).Where(m => m.Contains(new ScriptRef("Cﾒｲｷｭｳ.ADV", 3928))).ToList();
        Assert.Equal([new("Cﾒｲｷｭｳ.ADV", 3911), new("Cﾒｲｷｭｳ.ADV", 3917), new("Cﾒｲｷｭｳ.ADV", 3928)], door[0]);
        Assert.Equal([new("Cﾒｲｷｭｳ.ADV", 3923), new("Cﾒｲｷｭｳ.ADV", 3928)], door[1]);    // Enter, Leave
        Assert.Equal(6, s.F_GodsDoor_Quest.Value);
    }

    /// <summary>
    /// Kou's hint at the shut door (<c>Cﾒｲｷｭｳ:3874-3876</c> → <c>L_05ea4</c>, quest 3 → 4 with Kou past
    /// 9; the sweep's quest table): with Ho Raga's refusal behind him (quest 3), Rance goes to the
    /// door first, hears Kouhime name the Bonsai Statue still missing, and Ho Raga then hands it over.
    /// </summary>
    [Fact]
    public async Task Kou_s_hint_at_the_door_sets_quest_4_and_Ho_Raga_then_gives_the_statue()
    {
        var game = await StageFourAsync(kou: 8, quest: 3);
        if (game is null)
            return;
        var s = game.State;
        await NewWeekAsync(game);                                       // S4+1
        Assert.Equal(11, s.F_Kou_State.Value);
        RuinsSearch(game);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 290);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 3928);
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 295);
        await game.Script.CallFileAsync("Cｼﾊｲﾄｼ.ADV");
        Assert.Equal(4, s.F_GodsDoor_Quest.Value);                      // Cﾒｲｷｭｳ.ADV:4412

        HoRagasTower(game);
        game.Input.Menu.Enqueue("Gﾎﾗｶﾞ.ADV", 279);
        game.Input.Menu.Enqueue("Gﾎﾗｶﾞ.ADV", 284);
        await game.Script.CallFileAsync("Cﾒｲｷｭｳ.ADV");

        Log(game);
        Assert.Equal(5, s.F_GodsDoor_Quest.Value);                      // Gﾎﾗｶﾞ.ADV:359
        Assert.Equal(100, s.Items[18].Holder.Value);
    }

    /// <summary>
    /// "Get Gold Statue" is offered only with Rance (unit 1) leading (<c>Gﾎﾗｶﾞ:276-282</c>): Bound
    /// (unit 3) opens the gate (<c>:4-6</c>) but meets Ask Question and Leave only.
    /// </summary>
    [Fact]
    public async Task Ho_Raga_offers_the_statue_only_to_Rance()
    {
        var game = await StageFourAsync(kou: 8);
        if (game is null)
            return;
        await NewWeekAsync(game);
        HoRagasTower(game, searcher: 3);
        game.Input.Menu.Enqueue("Gﾎﾗｶﾞ.ADV", 284);

        await game.Script.CallFileAsync("Cﾒｲｷｭｳ.ADV");

        Assert.Equal([new("Gﾎﾗｶﾞ.ADV", 273), new("Gﾎﾗｶﾞ.ADV", 284)], game.Input.Menus[^1]);
        Assert.Equal(2, game.State.F_GodsDoor_Quest.Value);
        Assert.Equal(0, game.State.Items[18].Holder.Value);
    }

    /// <summary>
    /// S4 with Kou at 8, then S4+1: <c>COM</c> (<c>Gｺｳﾋﾒ</c> 6), Ho Raga's Tower with Rance — Get
    /// Gold Statue, Leave. Asserts the way there.
    /// </summary>
    private static async Task<EndGame?> WithTheBonsaiStatueAsync()
    {
        var game = await StageFourAsync(kou: 8);
        if (game is null)
            return null;
        var s = game.State;
        Assert.Equal(10, s.F_Kou_State.Value);                          // Gﾙﾄﾞﾗ.ADV:548
        await NewWeekAsync(game);                                       // S4+1
        Assert.Contains("Gｺｳﾋﾒ.ADV 6", game.Calls);                    // COM.ADV:268-272
        Assert.Equal(11, s.F_Kou_State.Value);                          // Gｺｳﾋﾒ.ADV:29
        HoRagasTower(game);
        game.Input.Menu.Enqueue("Gﾎﾗｶﾞ.ADV", 279);                     // Get Gold Statue
        game.Input.Menu.Enqueue("Gﾎﾗｶﾞ.ADV", 284);                     // Leave

        await game.Script.CallFileAsync("Cﾒｲｷｭｳ.ADV");

        Assert.Contains("Gﾎﾗｶﾞ.ADV 1", game.Calls);                     // Cﾒｲｷｭｳ.ADV:434
        Assert.Equal(5, s.F_GodsDoor_Quest.Value);                      // Gﾎﾗｶﾞ.ADV:359
        Assert.Equal(100, s.Items[18].Holder.Value);                    // :507-508, in stock
        return game;
    }

    /// <summary>
    /// The booted new game (<see cref="EndGame"/>) at the end of stage 3 — the whole map Leazas's
    /// but Shangri-La (29) and Nothing (75), Kayblis (commander 135) dead in the last battle's enemy
    /// party — with Kou at <paramref name="kou"/>, JAPAN fallen, the quest at <paramref name="quest"/>, Rance
    /// holding Chaos (item 1), Ho Raga's Tower (dungeon 5) visited (state 2: <c>Gﾎﾗｶﾞ:3236</c>), three
    /// statues in stock, Bound (unit 3), Soul (unit 4), Leila (unit 5) and Rick (unit 7) in service,
    /// Warg at stage 2, the debug commands on (Repel) and the Gold for the pickers; then S4 —
    /// <c>死亡捕獲</c> (stage 4) and the first Creator phase (<c>Gﾙﾄﾞﾗ</c> 1).
    /// </summary>
    private static async Task<EndGame?> StageFourAsync(int kou, int quest = 2)
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return null;
        var s = game.State;
        s.StoryStage = 3;
        for (int p = 1; p <= 91; p++)
            s.Provs[p].Owner = (Sys3Int)(p is 29 or 75 ? 3 : 1);
        s.Bt_EnemyParty1 = 135;
        s.Bt_EnemyParty2 = 0;
        s.Bt_EnemyParty3 = 0;
        s.Bt_EnemyParty4 = 0;
        s.Cmdrs[135].Status = 0;
        s.F_Kou_State = (Sys3Int)kou;
        s.F_Japan_State = 10;                                           // fallen (IJAPAN.ADV:6198), where Kou's arc begins
        s.F_GodsDoor_Quest = (Sys3Int)quest;                            // Gﾎﾗｶﾞ.ADV:1492 (1), Gﾌﾞﾘﾃｨ.ADV:1014 (2), Gﾎﾗｶﾞ.ADV:511 (3)
        s.Units[1].Item = 1;
        s.Items[1].Holder = 1;
        s.Dgns[5].State = 2;
        int[] statues = [15, 16, 17];                                   // Gﾌﾞﾘﾃｨ.ADV:1977, Gｶﾌｪ.ADV:546, Cﾒｲｷｭｳ.ADV:2745
        foreach (int item in statues)
            s.Items[item].Holder = 100;
        foreach (int unit in EndGame.Garrisons)
            s.Units[unit].Status = 1;
        s.N_Warg_Stage = 2;
        s.DebugMode = 1;
        s.Gold = 30000;

        await game.Script.CallFileAsync("死亡捕獲.ADV");                // S4, the attack's battle
        Assert.Equal(4, s.StoryStage.Value);
        await CreatorPhaseAsync(game, 2);
        Assert.Contains("Gﾙﾄﾞﾗ.ADV 1", game.Calls);
        return game;
    }

    /// <summary>
    /// The week's Creator phase (<c>MAIN.ADV:816-846</c>), counting <paramref name="turns"/>: up to
    /// 4 every draw 1 is Leazas Castle (1), spared, so each search gives up and no battle is fought.
    /// </summary>
    private static async Task CreatorPhaseAsync(EndGame game, int turns)
    {
        await game.Script.CallFileAsync("Tﾙﾄﾞﾗ.ADV");
        Assert.Equal(turns, game.State.N_Rudrasaum_Turns.Value);
        Assert.Equal(0, game.Fights);
    }

    /// <summary>
    /// A new week up to the player's commands, as far as these tests need it: every unit in service
    /// back to status 1 (<c>MAIN.ADV:140-151</c>; the rest of the Leazas phase — recovery, income —
    /// left out), <c>COM</c> (<c>COMMAND.ADV:3</c>) and the week's commands (<c>:38-44</c>). On the
    /// new game's other flags <c>COM</c>'s second tier may play an early scene (<c>Gｺﾙﾄﾞﾊﾞ</c> 1,
    /// <c>Gｶﾞﾝｼﾞｰ</c> 1; text) when nothing else marked the week.
    /// </summary>
    private static async Task NewWeekAsync(EndGame game)
    {
        var s = game.State;
        for (int unit = 1; unit <= 50; unit++)
        {
            if (s.Units[unit].Status != 0)
                s.Units[unit].Status = 1;
        }
        await game.Script.CallFileAsync("COM.ADV");
        s.Ui_CmdOwnCityAvail = 1;
        s.Ui_CmdDungeonAvail = 1;
    }

    /// <summary>
    /// Ho Raga's Tower (dungeon 5) clicked on the map (<c>COMMAND.ADV:149-151</c>: Siberia (38) under
    /// the cursor): Start Search, <paramref name="searcher"/> picked to search (Rance by default), the
    /// gate's Enter (open to units 1, 3, 7, 14, 21 and 31, <c>Gﾎﾗｶﾞ:4-6</c>); Ho Raga's own answers follow.
    /// </summary>
    private static void HoRagasTower(EndGame game, int searcher = 1)
    {
        game.State.Dgn_Cur = 5;
        game.State.Prov_Cur = game.State.Dgns[5].Prov;
        game.Input.Menu.Enqueue("Cﾒｲｷｭｳ.ADV", 267);                    // Start Search
        game.Input.Pick(searcher);                                      // the searcher, Search
        game.Input.Menu.Enqueue("Gﾎﾗｶﾞ.ADV", 138);                     // Enter
    }

    /// <summary>
    /// The city of Ancient Ruins (35) clicked (<c>Cｼﾊｲﾄｼ</c> on its city): Ruins Search
    /// (<c>:227-232</c> → <c>L_00c9e</c>, dungeon 30, then <c>Cﾒｲｷｭｳ</c>); the dungeon's answers follow.
    /// </summary>
    private static void RuinsSearch(EndGame game)
    {
        game.State.Prov_Cur = 35;
        game.Input.Menu.Enqueue("Cｼﾊｲﾄｼ.ADV", 229);
    }

    private void Log(EndGame game)
    {
        output.WriteLine(string.Join(", ", game.Calls.Where(c => !c.StartsWith("FIGT.ADV", StringComparison.Ordinal))));
        foreach (var menu in game.Input.Menus)
            output.WriteLine(string.Join(" | ", menu));
    }
}
