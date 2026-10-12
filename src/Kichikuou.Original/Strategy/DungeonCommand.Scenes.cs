using Kichikuou.Engine.Sys3;

namespace Kichikuou.Original.Strategy;

// Cﾒｲｷｭｳ.ADV's scenes with state (survey-dungeon.md §8, part D2 without God's Door): the treasure
// chests, Orochi, the Uncurse Spring, the Holy Gal's floor, KD and his quiz, Merim's inscriptions,
// joining and leaving, the refusals, the snow advice, the Pink Cow, the week's refusal and the
// Thieves' Hideout.
public sealed partial class DungeonCommand
{
    /// <summary>
    /// The treasure chests <c>L_02528</c>…<c>L_02da1</c> (<c>:1445-1932</c>): item
    /// <paramref name="item"/> to Leazas's stock, the searcher's portrait, three runs (found, what,
    /// added) with the fanfare.
    /// </summary>
    private async Task ChestAsync(int item, bool resetsCursor, int found, int what, int added)
    {
        // QUIRK: DGN-18 — L_02abb…L_02da1 keep whatever unit the cursor is on.
        if (resetsCursor)
            State.Unit_Cur = State.Bt_PlayerParty1;
        State.Item_Cur = (Sys3Int)item;
        Item.Holder = 100;
        if (item == 28)
            State.FogCard_Price = 120;                                  // L_02d13
        await WaoAsync(1);
        await WaoAsync(0);
        State.CallArg = Unit.FaceCg;
        await WaoAsync(311);
        await WaoAsync(113);
        await TextAsync(found);                                         // "… found a treasure chest in the dungeon."
        await WaoAsync(1);
        await FanfareAsync();                                           // \L_07514
        await TextAsync(what);
        await WaoAsync(1);
        await TextAsync(added);
        await WaoAsync(1);
        await FanfareEndAsync();                                        // \L_0753a
        await WaoAsync(0);
        await WaoAsync(312);
    }

    /// <summary>
    /// <c>L_023da</c> (<c>:1376-1444</c>): Orochi at the bottom of its pit — the tremor, its speech,
    /// the duel straight into the battle (no monster roll), then <c>Gﾌｳｶ</c> 5 (Rance) or 6.
    /// </summary>
    private async Task OrochiAsync()
    {
        await WaoAsync(1);
        await WaoAsync(0);
        await SpeakAsync(113, 1381);
        State.CallArg = Unit.FaceCg;
        await WaoAsync(311);
        await SpeakAsync(111, 1392);
        await SpeakAsync(113, 1397, 1400, 1403);
        await SpeakAsync(121, 1408, 1415, 1418, 1425);
        await WaoAsync(312);
        State.Bt_EnemyParty1 = 100;
        // QUIRK: DGN-04 — no flag: only the Fuuka arc gates it.
        await RunBlockAsync(2903);                                      // \L_03fc8 Orochi
        await FightAsync();                                             // \L_01f47
        if (Unit.Status != 0)
        {
            if (State.Unit_Cur == 1)
                await Script.EventAsync("Gﾌｳｶ.ADV", 5);
            if (State.Unit_Cur != 1)
                await Script.EventAsync("Gﾌｳｶ.ADV", 6);
        }
    }

    /// <summary><c>L_02bce</c> (<c>:1806-1815</c>): the Holy Gal's 33rd floor — <c>Gﾐﾘ</c> 3 or <c>G2</c> 1.</summary>
    private async Task HolyGalAsync()
    {
        await WaoAsync(0);
        if (State.VAR0427 == 3 && State.F_Wenlina_CureMilli == 1)
        {
            await Script.EventAsync("Gﾐﾘ.ADV", 3);
            return;
        }
        await Script.EventAsync("G2.ADV", 1);
    }

    /// <summary>
    /// <c>L_02e29</c> (<c>:1933-2008</c>): the Uncurse Spring — Enter or Don't (a cancel asks
    /// again); Melfeis's and the Cafe trip's scenes, otherwise nothing happens.
    /// </summary>
    private async Task UncurseSpringAsync()
    {
        await WaoAsync(1);
        await WaoAsync(0);
        State.CallArg = Unit.FaceCg;
        await WaoAsync(311);
        await SpeakAsync(113, 1940);
        while (true)
        {
            await WaitForReleaseAsync();                                // L_02e6d
            var enter = MenuItem(1954);
            await LineBreakAsync();
            int choice = await SelectAsync([enter, MenuItem(1958)]);
            if (choice == 1)
                break;
            if (choice == 2)
            {
                await SpeakAsync(113, 1966);                            // L_02eab
                await WaoAsync(312);
                return;
            }
        }
        State.Unit_Cur = State.Bt_PlayerParty1;                         // L_02edd
        if (State.Unit_Cur == 13 && State.N_Melfeis_Stage < 10)
        {
            await Script.EventAsync("Gﾒﾙﾌｪｲｽ.ADV", 3);
            return;
        }
        if (State.Unit_Cur == 1 && State.Ev_CommandMode == 1)
        {
            await Script.EventAsync("Gｶﾌｪ.ADV", 35);
            State.Unit_Cur = 1;
            return;
        }
        await SpeakAsync(113, 1989);
        await SpeakAsync(111, 1999);
        await SpeakAsync(113, 2004);                                    // "But nothing happened."
        await WaoAsync(312);
    }

    /// <summary>
    /// <c>L_02f97</c> (<c>:2009-2118</c>): KD at the Rising Dragon Mountain's summit. Another unit
    /// is sent away; Rance meets the cat, and once the quest needs the statue and item 17 is not
    /// owned, the statue and the quiz (<c>L_0319e</c>).
    /// </summary>
    private async Task SummitAsync()
    {
        await WaoAsync(1);
        await WaoAsync(0);
        State.CallArg = Unit.FaceCg;
        await WaoAsync(311);
        await WaoAsync(113);
        await TitleAsync(2017);                                         // "Rising Dragon Mountain"
        await TextAsync(2021);                                          // "Summit"
        await WaoAsync(1);
        await TextAsync(2025);
        await WaoAsync(1);
        await WaoAsync(0);
        State.CallArg = 490;
        await WaoAsync(321);
        await SpeakAsync(121, 2036, 2041);
        if (State.Unit_Cur != 1)
        {
            await SpeakAsync(113, 2051);
            await SpeakAsync(121, 2056, 2061, 2068);
            await WaoAsync(312);
            await WaoAsync(322);
            return;
        }
        State.Item_Cur = 17;
        if (State.F_GodsDoor_Quest > 1 && Item.Holder == 0)
        {
            await StatueAsync();                                        // @L_0319e
            return;
        }
        await SpeakAsync(111, 2081, 2084);
        await SpeakAsync(121, 2089);
        await SpeakAsync(111, 2094, 2097);
        await SpeakAsync(121, 2102);
        await SpeakAsync(112, 2109);
        await WaoAsync(312);
        await WaoAsync(322);
    }

    /// <summary>
    /// <c>L_0319e</c> (<c>:2119-2421</c>): KD's gold collection (Chaos points out the sunflower when
    /// Rance holds him), then the quiz or "take it by force", which blows the unit down to floor 20.
    /// </summary>
    private async Task StatueAsync()
    {
        await SpeakAsync(112, 2122, 2127, 2134, 2139);
        await SpeakAsync(111, 2148);
        await SpeakAsync(121, 2155);
        await SpeakAsync(123, 2162);
        await SpeakAsync(121, 2169);
        await SpeakAsync(123, 2180);
        await SpeakAsync(121, 2187);
        await SpeakAsync(111, 2198);
        if (Unit.Item == 1)
        {
            await WaoAsync(322);
            State.CallArg = 80;
            await WaoAsync(321);
            await SpeakAsync(121, 2209);
            await SpeakAsync(111, 2214);
            await SpeakAsync(121, 2219);
            await SpeakAsync(123, 2224);
            await SpeakAsync(111, 2231);
            await SpeakAsync(121, 2238, 2245);
            await SpeakAsync(111, 2256);
            await WaoAsync(322);
            State.CallArg = 490;
            await WaoAsync(321);
        }
        if (Unit.Item != 1)
        {
            await SpeakAsync(112, 2266, 2269);
            await SpeakAsync(121, 2278, 2287, 2292);
            await SpeakAsync(113, 2297);
            await SpeakAsync(121, 2304, 2313);
            await SpeakAsync(111, 2322);
        }
        await SpeakAsync(121, 2328);
        await SpeakAsync(111, 2335);
        await SpeakAsync(121, 2342, 2347, 2350);
        while (true)
        {
            await WaitForReleaseAsync();                                // L_035d0
            var quiz = MenuItem(2363);
            await LineBreakAsync();
            int choice = await SelectAsync([quiz, MenuItem(2367)]);
            if (choice == 1)
                break;
            if (choice == 2)
            {
                // QUIRK: DGN-12 — down to floor 20; the quiz waits for the next visit.
                await SpeakAsync(111, 2375);                            // L_0360e
                await SpeakAsync(121, 2384, 2389);
                await SpeakAsync(123, 2394);
                await SpeakAsync(111, 2399);
                await SpeakAsync(123, 2404);
                await SpeakAsync(111, 2413);
                await WaoAsync(312);
                await WaoAsync(322);
                State.DgnRun_Floor = 20;
                return;
            }
        }
        await SpeakAsync(111, 2425);                                    // L_036f1
        await SpeakAsync(121, 2434);
        State.Tmp0621 = 0;
        while (true)
        {
            State.Tmp0621 = State.Tmp0621 + 1;                          // L_03744
            if (State.Tmp0621 == 4)
            {
                await QuizWonAsync();
                return;
            }
            await WaoAsync(121);
            await ValuesAsync(2449, State.Tmp0621);                     // "Now, Question n."
            await WaoAsync(1);
            await WaoAsync(0);
            Script.Rnd(5);
            State.Tmp0690 = State.RetVal;
            if (!await QuestionAsync())
            {
                await QuizLostAsync();
                return;
            }
            State.RetVal = 89;                                          // L_03a4b
            await Script.SoundAsync(0);
            await SpeakAsync(121, close: false, 2620);                  // "Correct, meow."
            await Script.SoundAsync(3);
            await WaoAsync(0);
        }
    }

    /// <summary>
    /// KD's questions (<c>:2461-2613</c>) — <c>Tmp0690</c> picks one: its speech, then three answers
    /// (a cancel asks again). True for the right answer.
    /// </summary>
    private async Task<bool> QuestionAsync()
    {
        (int[] Speech, int[] Answers, int Right)? question = State.Tmp0690.Value switch
        {
            1 => ([2464, 2467], [2476, 2480, 2484], 1),                 // pink unyuun
            2 => ([2493, 2496], [2509, 2513, 2517], 2),                 // transforms under the moon
            3 => ([2526, 2529], [2540, 2544, 2548], 3),                 // KD's first appearance
            4 => ([2557, 2560], [2569, 2573, 2577], 1),                 // aliens
            5 => ([2586, 2589], [2600, 2604, 2608], 1),                 // Ho Raga's doggie
            _ => null,
        };
        // QUIRK: DGN-17 — a draw of 6 (ENG-02) asks nothing and counts as right.
        if (question is not { } q)
            return true;
        await SpeakAsync(121, q.Speech);
        while (true)
        {
            await WaitForReleaseAsync();
            var first = MenuItem(q.Answers[0]);
            await LineBreakAsync();
            var second = MenuItem(q.Answers[1]);
            await LineBreakAsync();
            int choice = await SelectAsync([first, second, MenuItem(q.Answers[2])]);
            if (choice != 0)
                return choice == q.Right;
        }
    }

    /// <summary><c>L_03a87</c> (<c>:2628-2695</c>): a wrong answer — KD blows the unit down to floor 20.</summary>
    private async Task QuizLostAsync()
    {
        State.RetVal = 90;
        await Script.SoundAsync(0);
        await SpeakAsync(121, close: false, 2634, 2639);                // "Wrong, meow."
        await Script.SoundAsync(3);
        await WaoAsync(0);
        await SpeakAsync(111, 2646);
        await SpeakAsync(113, 2655);
        await SpeakAsync(121, 2660);
        await SpeakAsync(123, 2665);
        await SpeakAsync(111, 2670);
        await SpeakAsync(121, 2675);
        await SpeakAsync(111, 2680);
        await SpeakAsync(113, 2685);
        await WaoAsync(312);
        await WaoAsync(322);
        // QUIRK: DGN-12 — down to floor 20; the quiz can be taken again on the next visit.
        State.DgnRun_Floor = 20;
    }

    /// <summary><c>L_03bc5</c> (<c>:2696-2749</c>): three right answers — the Golden Sunflower Statue (item 17) to stock.</summary>
    private async Task QuizWonAsync()
    {
        await SpeakAsync(121, 2699);
        await SpeakAsync(111, 2706);
        await SpeakAsync(121, 2711);
        await SpeakAsync(111, 2722);
        await SpeakAsync(113, 2727);
        await SpeakAsync(121, 2734);
        await WaoAsync(322);
        await SpeakAsync(123, 2740);
        State.Item_Cur = 17;
        Item.Holder = 100;
        await WaoAsync(312);
        await WaoAsync(322);
    }

    /// <summary>
    /// <c>L_06153</c> (<c>:4557-4594</c>): Merim's next inscription by her count — the HP medicine
    /// (0, 2, 5, 8, 9), the magic light (1, 4, 7), the Super Egg (11); at any other count nothing.
    /// </summary>
    private async Task InscriptionAsync()
    {
        int count = State.N_Merim_Inscriptions.Value;
        Func<Task>? find = count switch
        {
            0 or 2 or 5 or 8 or 9 => HpMedicineAsync,
            1 or 4 or 7 => MagicLightAsync,
            11 => SuperEggAsync,
            _ => null,
        };
        if (find is null)
            return;
        State.N_Merim_Inscriptions = (Sys3Int)(count + 1);
        await find();
    }

    /// <summary><c>L_06239</c> (<c>:4595-4730</c>): the legendary HP medicine — Max HP + 2 for the unit under the cursor.</summary>
    private async Task HpMedicineAsync()
    {
        State.CallArg = 63;
        await WaoAsync(311);
        await SpeakAsync(111, 4600);
        await SpeakAsync(113, 4607);
        await SpeakAsync(111, 4616);
        await SpeakAsync(123, 4623);
        await SpeakAsync(111, 4630, 4633, 4636);
        await SpeakAsync(113, 4643, 4652, 4655);
        await SpeakAsync(111, 4664);
        await SpeakAsync(113, 4672, 4682);
        // QUIRK: DGN-18 — whatever unit the cursor is on.
        Unit.MaxHP = Unit.MaxHP + 2;
        await SpeakAsync(113, 4691, 4694);
        await SpeakAsync(111, 4704);
        await SpeakAsync(113, 4711);
        await SpeakAsync(111, 4720);
        await SpeakAsync(113, 4727);
    }

    /// <summary><c>L_064b1</c> (<c>:4731-4850</c>): the black light — magic + 1.</summary>
    private async Task MagicLightAsync()
    {
        State.CallArg = 63;
        await WaoAsync(311);
        await SpeakAsync(111, 4736);
        await SpeakAsync(113, 4741);
        await SpeakAsync(111, 4748);
        await SpeakAsync(113, 4753);
        await SpeakAsync(111, 4760);
        await SpeakAsync(113, 4767);
        await SpeakAsync(111, 4776);
        await SpeakAsync(113, 4781, 4784, 4789, 4792, 4797);
        await SpeakAsync(111, 4805);
        await SpeakAsync(113, 4816);
        await SpeakAsync(111, 4826, 4831, 4838);
        await SpeakAsync(113, 4843);
        // QUIRK: DGN-18 — whatever unit the cursor is on.
        Unit.Magic = Unit.Magic + 1;
        await WaoAsync(0);
    }

    /// <summary><c>L_066db</c> (<c>:4851-5056</c>): the Super Egg — Max HP + 10, attack and defence + 5, magic + 3.</summary>
    private async Task SuperEggAsync()
    {
        State.CallArg = 63;
        await WaoAsync(311);
        await SpeakAsync(111, 4856);
        await SpeakAsync(113, 4861);
        await SpeakAsync(111, 4872);
        await SpeakAsync(113, 4877);
        await SpeakAsync(111, 4882);
        await SpeakAsync(113, 4887);
        await SpeakAsync(111, 4896, 4899, 4902);
        await SpeakAsync(113, 4907);
        await SpeakAsync(111, 4914);
        await SpeakAsync(113, 4925);
        await SpeakAsync(111, 4935, 4940, 4949);
        await SpeakAsync(113, 4958);
        await SpeakAsync(111, 4963);
        await SpeakAsync(113, 4968);
        await SpeakAsync(111, 4979);
        await SpeakAsync(113, 4986);
        await SpeakAsync(111, 4999);
        await SpeakAsync(113, 5008);
        await SpeakAsync(111, 5016);
        await SpeakAsync(113, 5023);
        // QUIRK: DGN-18 — whatever unit the cursor is on.
        Unit.MaxHP = Unit.MaxHP + 10;
        Unit.Atk = Unit.Atk + 5;
        Unit.Def = Unit.Def + 5;
        Unit.Magic = Unit.Magic + 3;
        await SpeakAsync(113, 5037, 5040, 5043, 5046);
        await SpeakAsync(111, 5051);
    }

    /// <summary><c>L_06aa0</c> (<c>:5057-5131</c>): Galtia (41) goes plant-hunting with Martina instead.</summary>
    private async Task GaltiaRefusesAsync()
    {
        await RefusalPortraitsAsync(452);
        await SpeakAsync(121, 5064);
        await SpeakAsync(111, 5071);
        await SpeakAsync(121, 5076);
        await SpeakAsync(111, 5083);
        await SpeakAsync(121, 5088);
        await SpeakAsync(111, 5099);
        await SpeakAsync(121, 5104);
        await SpeakAsync(111, 5115);
        await SpeakAsync(121, 5124);
        await WaoAsync(312);
        await WaoAsync(322);
    }

    /// <summary><c>L_06bf2</c> (<c>:5132-5210</c>): Hawzel and Lucie (42) would rather bake a cake.</summary>
    private async Task HawzelRefusesAsync()
    {
        await RefusalPortraitsAsync(429);
        await SpeakAsync(121, 5139);
        await SpeakAsync(111, 5144);
        await SpeakAsync(121, 5151);
        await SpeakAsync(111, 5158);
        await WaoAsync(322);
        State.CallArg = 428;
        await WaoAsync(321);
        await SpeakAsync(121, 5166);
        await SpeakAsync(111, 5173, 5176);
        await SpeakAsync(121, 5183);
        await SpeakAsync(111, 5188);
        await SpeakAsync(121, 5193);
        await WaoAsync(322);
        State.CallArg = 429;
        await WaoAsync(321);
        await SpeakAsync(121, 5205);
        await WaoAsync(312);
        await WaoAsync(322);
    }

    /// <summary><c>L_06d69</c> (<c>:5211-5252</c>): Miki (32) is scared of dungeons.</summary>
    private async Task MikiRefusesAsync()
    {
        await RefusalPortraitsAsync(54);
        await SpeakAsync(121, 5218);
        await SpeakAsync(111, 5223);
        await SpeakAsync(121, 5228);
        await SpeakAsync(111, 5235);
        await SpeakAsync(121, 5244, 5247);
        await WaoAsync(312);
        await WaoAsync(322);
    }

    /// <summary><c>L_06e2e</c> (<c>:5253-5307</c>): Meglass (30) disappears.</summary>
    private async Task MeglassRefusesAsync()
    {
        await RefusalPortraitsAsync(447);
        await SpeakAsync(121, 5260);
        await SpeakAsync(111, 5265);
        await SpeakAsync(121, 5270);
        await WaoAsync(322);
        await SpeakAsync(113, 5278);
        await SpeakAsync(111, 5283);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 5290);
        await SpeakAsync(111, 5297);
        await SpeakAsync(121, 5302);
        await WaoAsync(312);
        await WaoAsync(322);
    }

    /// <summary><c>L_06f37</c> (<c>:5308-5344</c>): Satella (29) has no time for dungeons.</summary>
    private async Task SatellaRefusesAsync()
    {
        await RefusalPortraitsAsync(75);
        await SpeakAsync(121, 5315);
        await SpeakAsync(111, 5320);
        await SpeakAsync(121, 5325);
        await WaoAsync(322);
        State.CallArg = 444;
        await WaoAsync(321);
        await SpeakAsync(121, 5339);
        await WaoAsync(312);
        await WaoAsync(322);
    }

    /// <summary>Rance's portrait (1) and the refusing unit's (<paramref name="face"/>).</summary>
    private async Task RefusalPortraitsAsync(int face)
    {
        State.CallArg = 1;
        await WaoAsync(311);
        State.CallArg = (Sys3Int)face;
        await WaoAsync(321);
    }

    /// <summary>
    /// <c>L_06fe4</c> (<c>:5345-5416</c>): Maris on the snow — with the Snow Charm in stock it is
    /// passable, without it troops will be lost (an equipped charm gets neither line).
    /// </summary>
    private async Task SnowAdviceAsync()
    {
        State.Item_Cur = 11;
        if (Item.Holder == 100)
        {
            await SpeakAsync(121, 5350, 5359);
            await SpeakAsync(111, 5370);
        }
        if (Item.Holder == 0)
        {
            await SpeakAsync(121, 5377);
            await SpeakAsync(111, 5384);
            await SpeakAsync(121, 5389, 5394);
            await SpeakAsync(111, 5405);
            await SpeakAsync(121, 5412);
        }
    }

    /// <summary><c>L_0712e</c> (<c>:5417-5501</c>): Rance thinks of bathing Sill in the spring, and drops it.</summary>
    private async Task PinkCowAsync()
    {
        await WaoAsync(0);
        State.CallArg = 1;
        await WaoAsync(311);
        await SpeakAsync(112, 5423);
        await SpeakAsync(111, 5432);
        State.CallArg = 471;
        await WaoAsync(321);
        await SpeakAsync(121, 5439);
        await SpeakAsync(111, 5444);
        await SpeakAsync(112, 5449);
        await SpeakAsync(121, 5460);
        await SpeakAsync(111, 5465);
        await SpeakAsync(121, 5472);
        await WaoAsync(322);
        await SpeakAsync(112, 5478, 5483);
        if (State.F_GodsDoor_Quest != 0)
            await SpeakAsync(112, 5493);
        await Script.MapCallAsync(0);
    }

    /// <summary><c>L_072b8</c> (<c>:5502-5532</c>): Merim joins the search party.</summary>
    private async Task MerimJoinsAsync()
    {
        State.DgnRun_MerimInParty = 1;
        await WaoAsync(0);
        State.CallArg = 1;
        await WaoAsync(311);
        await SpeakAsync(111, 5509);
        State.CallArg = 63;
        await WaoAsync(321);
        await SpeakAsync(121, 5520);
        await SpeakAsync(113, 5527);
        await Script.MapCallAsync(0);
    }

    /// <summary><c>L_07344</c> (<c>:5533-5578</c>): Merim is left behind.</summary>
    private async Task MerimLeavesAsync()
    {
        State.DgnRun_MerimInParty = 0;
        await WaoAsync(0);
        State.CallArg = 1;
        await WaoAsync(311);
        await SpeakAsync(111, 5540);
        State.CallArg = 63;
        await WaoAsync(321);
        await SpeakAsync(121, 5547);
        await SpeakAsync(111, 5556);
        await SpeakAsync(122, 5563, 5568);
        await SpeakAsync(113, 5573);
        await Script.MapCallAsync(0);
    }

    /// <summary><c>L_07418</c> (<c>:5579-5619</c>): a dungeon was searched already this week.</summary>
    private async Task AlreadySearchedAsync()
    {
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 5584);
        State.CallArg = 1;
        await WaoAsync(311);
        await SpeakAsync(111, 5597);
        await SpeakAsync(121, 5602);
        await SpeakAsync(111, 5607);
        await SpeakAsync(113, 5612);
        await Script.MapCallAsync(0);
    }

    /// <summary>
    /// <c>L_03cbd</c> (<c>:2750-2871</c>) — the Thieves' Hideout: Bound's scene once Soul's story
    /// reaches it; after the prologue Rance only looks round and leaves; in the prologue one of
    /// three bits of banter (<c>Y 4, 3</c>).
    /// </summary>
    private async Task HideoutAsync()
    {
        State.Unit_Cur = 4;
        if (Unit.Status != 0 && State.N_Soul_Stage == 5)
        {
            await Script.EventAsync("Gﾊﾞｳﾝﾄﾞ.ADV", 1);
            return;
        }
        if (State.StoryStage != 0)                                      // :2757-2787
        {
            State.CallArg = 1008;
            await WaoAsync(331);                                        // \L_0766d
            await WaoAsync(113);                                        // \L_075a9
            await TitleAsync(2763);
            await WaoAsync(1);                                          // \L_076a4
            await WaoAsync(0);                                          // \L_07697
            State.CallArg = 1;
            await WaoAsync(311);                                        // \L_075c5
            await WaoAsync(111);                                        // \L_07571
            await TextAsync(2771);
            await WaoAsync(1);
            await TextAsync(2776);
            await WaoAsync(1);
            await TextAsync(2779);
            await WaoAsync(1);
            await WaoAsync(0);
            await WaoAsync(312);                                        // \L_075d3
            await WaoAsync(332);                                        // \L_0767b
            return;
        }

        State.CallArg = 1008;                                           // :2788-2803
        await WaoAsync(331);
        State.CallArg = 1;
        await WaoAsync(311);
        await WaoAsync(113);
        await TitleAsync(2795);
        await WaoAsync(1);
        await WaoAsync(0);
        await WaoAsync(111);
        await TextAsync(2801);                                          // "This is my hideout."
        await WaoAsync(1);
        await WaoAsync(0);
        Script.Rnd(3);                                                  // :2804
        State.Tmp0690 = State.RetVal;
        if (State.Tmp0690 == 1)                                         // Sill
        {
            State.CallArg = 2;
            await WaoAsync(321);                                        // \L_07635
            await WaoAsync(121);                                        // \L_075e1
            await TextAsync(2811);
            await WaoAsync(1);
            await WaoAsync(0);
            await WaoAsync(111);
            await TextAsync(2818);
            await WaoAsync(1);
            await WaoAsync(0);
        }
        if (State.Tmp0690 == 2)                                         // Bound
        {
            State.CallArg = 3;
            await WaoAsync(321);
            await WaoAsync(121);
            await TextAsync(2827);
            await WaoAsync(1);
            await TextAsync(2830);
            await WaoAsync(1);
            await WaoAsync(0);
            await WaoAsync(111);
            await TextAsync(2839);
            await WaoAsync(1);
            await WaoAsync(0);
        }
        if (State.Tmp0690 == 3)                                         // Soul
        {
            State.CallArg = 4;
            await WaoAsync(321);
            await WaoAsync(121);
            await TextAsync(2852);
            await WaoAsync(1);
            await TextAsync(2857);
            await WaoAsync(1);
            await WaoAsync(0);
            await WaoAsync(112);                                        // \L_0758d
            await TextAsync(2864);
            await WaoAsync(1);
            await WaoAsync(0);
        }
        await WaoAsync(312);                                            // \L_075d3
        await WaoAsync(322);                                            // \L_07643
        await WaoAsync(332);                                            // \L_0767b
    }
}
