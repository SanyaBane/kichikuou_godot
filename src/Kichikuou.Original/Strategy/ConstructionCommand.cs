using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Cﾃｲﾄ.ADV</c>, the Construction command (<c>docs/strategy/survey-commands.md</c> §3.7), ported
/// whole: Build or Don't; while a building is under way (<c>BuildWeeksLeft ≠ 0</c>) Maris refuses,
/// naming every building at state 2 and the weeks left; otherwise the buildings not started yet
/// (Maria's institute only while she is in the army and her tulip is past 1), each with Rance's
/// proposal, its terms and the budget, and Construct or Something Else (the Magic Research Building
/// also Don't Construct, which leaves). Construct pays the price if the gold covers it — booked to
/// <c>Hist_SundryExp</c> of whatever record <c>Hist_Cur</c> names (CMD-14) — starts the timer, sets
/// the building to 2 and spends the week's construction; <c>COMMAND.ADV:18-36, 578-614</c> counts the
/// weeks down and plays the completion scene. The "all built" opening (<c>:1-59</c>) needs the pool,
/// which nothing builds, and the pool's own offer (<c>:588-657</c>) has no way in (CMD-20).
/// </summary>
public sealed class ConstructionCommand(OriginalScriptContext script) : CommandFile(script, "Cﾃｲﾄ.ADV")
{
    /// <summary><c>Cﾃｲﾄ.ADV:1-286</c>: the command, up to its return.</summary>
    public async Task RunAsync()
    {
        // QUIRK: CMD-20 — F_Pool_Built is never 2 (the pool's offer is dead), so this never holds.
        if (State.Bld_GirlsAcademy == 2 && State.Bld_Library == 2 && State.Bld_Hospital == 2 && State.F_Pool_Built == 2
            && State.Bld_SMTower == 2 && State.Bld_MariaLab == 2 && State.Bld_MagicLab == 2)   // :1-59
        {
            await RanceAsync();
            await SpeakAsync(111, 8);                                   // "Let's have a new building made."
            await MarisAsync();
            await SpeakAsync(121, 21);
            await SpeakAsync(111, 28);
            await SpeakAsync(111, 33);
            await SpeakAsync(121, 38);
            await WaoAsync(322);                                        // \L_01246
            await SpeakAsync(113, 46);                                  // "Maris left the room."
            await SpeakAsync(112, 51);                                  // "There's nothing left to build"
            await Script.MapCallAsync(0);
            return;
        }

        while (true)
        {
            // L_00155 (:60-71): no release wait before this menu; a cancel falls through to Don't.
            var items = new List<SceneMenuItem> { MenuItem(62) };       // Build
            await LineBreakAsync();
            items.Add(MenuItem(66));                                    // Don't
            if (await SelectAsync(items) != 1)
                return;                                                 // L_00175
            if (State.BuildWeeksLeft != 0)                              // L_00178 (:73-220)
            {
                await BusyAsync();
                return;
            }
            await RanceAsync();                                         // :221-238
            await SpeakAsync(111, 227);                                 // "Let's construct a new building."
            await MarisAsync();
            await SpeakAsync(121, 236);

            while (true)
            {
                int line = await ListAsync();
                if (line == 282)
                    break;                                              // Don't Build: L_00155
                bool again = line switch
                {
                    246 => await AcademyAsync(),
                    252 => await LibraryAsync(),
                    258 => await HospitalAsync(),
                    264 => await TowerAsync(),
                    271 => await MariaLabAsync(),
                    _ => await MagicLabAsync(),
                };
                if (!again)
                    return;
            }
        }
    }

    /// <summary><c>!CallArg : 1001! \L_01270: !CallArg : 1! \L_011c8:</c> — the backdrop and Rance's picture.</summary>
    private async Task RanceAsync()
    {
        State.CallArg = 1001;
        await WaoAsync(331);
        State.CallArg = 1;
        await WaoAsync(311);
    }

    /// <summary><c>!CallArg : 6! \L_01238:</c> — Maris's picture.</summary>
    private Task MarisAsync()
    {
        State.CallArg = 6;
        return WaoAsync(321);
    }

    /// <summary>
    /// <c>:74-219</c>: Maris refuses while a building is under way — each building at state 2 named,
    /// the weeks left ("another week" at 1) — Rance sulks; the map.
    /// </summary>
    private async Task BusyAsync()
    {
        await RanceAsync();
        await SpeakAsync(111, 80);
        await MarisAsync();
        await SpeakAsync(121, 93);                                      // "That's impossible."
        await SpeakAsync(111, 98);
        await WaoAsync(121);
        await TextAsync(103);                                           // "We have our hands full with"
        if (State.Bld_GirlsAcademy == 2)
            await TextAsync(107);
        if (State.Bld_Library == 2)
            await TextAsync(111);
        if (State.Bld_Hospital == 2)
            await TextAsync(115);
        if (State.Bld_SMTower == 2)
            await TextAsync(119);
        if (State.Bld_MariaLab == 2)
            await TextAsync(123);
        if (State.Bld_MagicLab == 2)
            await TextAsync(127);
        await TextAsync(130);                                           // ". It'll take another"
        if (State.BuildWeeksLeft > 1)
            await ValuesAsync(135, State.BuildWeeksLeft);
        await TextAsync(139);                                           // " week"
        if (State.BuildWeeksLeft > 1)
            await TextAsync(142);
        await TextAsync(145);
        await WaoAsync(1);
        await TextAsync(148);
        await WaoAsync(1);
        await WaoAsync(0);
        await SpeakAsync(111, 153, 160);
        await SpeakAsync(121, 171);
        await SpeakAsync(111, 182, 185);
        await SpeakAsync(121, 190);
        await SpeakAsync(111, 197);
        await SpeakAsync(121, 204);
        await SpeakAsync(111, 209, 214);
        await Script.MapCallAsync(0);
    }

    /// <summary>
    /// <c>L_004b2</c> (<c>:239-286</c>): the buildings not started (state 0) and Don't Build; a cancel
    /// shows the list again. Leaves <c>Unit_Cur</c> at 16. Returns the chosen item's line.
    /// </summary>
    private async Task<int> ListAsync()
    {
        while (true)
        {
            await WaitForReleaseAsync();
            var items = new List<SceneMenuItem>();
            if (State.Bld_GirlsAcademy == 0)
                items.Add(MenuItem(246));                               // Military Academy
            if (State.Bld_Library == 0)
                items.Add(MenuItem(252));                               // NEW Library
            if (State.Bld_Hospital == 0)
                items.Add(MenuItem(258));                               // General Hospital
            if (State.Bld_SMTower == 0)
                items.Add(MenuItem(264));                               // S&M Tower
            State.Unit_Cur = 16;                                        // QUIRK: CMD-20 — left behind (ENG-05)
            if (State.Bld_MariaLab == 0 && Unit.Status != 0 && State.F_Maria_Tulip > 1)
                items.Add(MenuItem(271));                               // Maria Research
            if (State.Bld_MagicLab == 0)
                items.Add(MenuItem(277));                               // Magic Research
            items.Add(MenuItem(282));                                   // Don't Build
            int choice = await SelectAsync(items);
            if (choice != 0)
                return items[choice - 1].At.Line;
        }
    }

    // -----------------------------------------------------------------------------------------
    // The buildings
    // -----------------------------------------------------------------------------------------

    /// <summary><c>L_005b6</c> (<c>:290-395</c>): the Girls' Military Academy, 600 for 16 weeks.</summary>
    private async Task<bool> AcademyAsync()
    {
        await SpeakAsync(111, 293, 298, 305, 314, 323);
        await SpeakAsync(121, 330);
        if (await OfferAsync(339, 355, 600) is bool again)
            return again;
        State.BuildWeeksLeft = 16;
        State.Bld_GirlsAcademy = 2;
        State.Ui_CmdBuildAvail = 0;
        await SpeakAsync(111, 377);
        await SpeakAsync(121, 386);
        return false;
    }

    /// <summary><c>L_00783</c> (<c>:396-476</c>): the Library, 400 for 8 weeks.</summary>
    private async Task<bool> LibraryAsync()
    {
        await SpeakAsync(111, 399, 406);
        await SpeakAsync(121, 417);
        if (await OfferAsync(426, 442, 400) is bool again)
            return again;
        State.BuildWeeksLeft = 8;
        State.Bld_Library = 2;
        State.Ui_CmdBuildAvail = 0;
        await SpeakAsync(111, 464);
        await SpeakAsync(121, 469);
        return false;
    }

    /// <summary><c>L_008eb</c> (<c>:477-587</c>): the Hospital, 400 for 12 weeks.</summary>
    private async Task<bool> HospitalAsync()
    {
        await SpeakAsync(111, 480);
        await SpeakAsync(121, 489);
        await SpeakAsync(111, 498, 507, 512);
        await SpeakAsync(121, 519);
        if (await OfferAsync(528, 544, 400) is bool again)
            return again;
        State.BuildWeeksLeft = 12;
        State.Bld_Hospital = 2;
        State.Ui_CmdBuildAvail = 0;
        await SpeakAsync(111, 566, 569);
        await SpeakAsync(121, 580);
        return false;
    }

    /// <summary><c>L_00bf9</c> (<c>:658-761</c>): the S&amp;M Tower, 800 for 12 weeks.</summary>
    private async Task<bool> TowerAsync()
    {
        await SpeakAsync(111, 661);
        await SpeakAsync(113, 668);
        await SpeakAsync(121, 677);
        await SpeakAsync(111, 684, 691);
        await SpeakAsync(121, 702);
        if (await OfferAsync(709, 725, 800) is bool again)
            return again;
        State.BuildWeeksLeft = 12;
        State.Bld_SMTower = 2;
        State.Ui_CmdBuildAvail = 0;
        await SpeakAsync(111, 747);
        await SpeakAsync(121, 756);
        return false;
    }

    /// <summary><c>L_00dc8</c> (<c>:762-871</c>): the Maria Research Institute, 980 for 8 weeks.</summary>
    private async Task<bool> MariaLabAsync()
    {
        await SpeakAsync(111, 765, 774, 781, 786, 789, 798);
        await SpeakAsync(121, 803);
        if (await OfferAsync(814, 830, 980) is bool again)
            return again;
        State.BuildWeeksLeft = 8;
        State.Bld_MariaLab = 2;
        State.Ui_CmdBuildAvail = 0;
        await SpeakAsync(111, 852, 857);
        await SpeakAsync(121, 868);
        return false;
    }

    /// <summary><c>L_00fac</c> (<c>:872-962</c>): the Magic Research Building, 900 for 12 weeks.</summary>
    private async Task<bool> MagicLabAsync()
    {
        await SpeakAsync(111, 875, 884);
        await SpeakAsync(121, 893);
        if (await OfferAsync(904, 920, 900, dontConstruct: true) is bool again)
            return again;
        State.BuildWeeksLeft = 12;
        State.Bld_MagicLab = 2;
        State.Ui_CmdBuildAvail = 0;
        await SpeakAsync(111, 946);
        await SpeakAsync(121, 955);
        return false;
    }

    /// <summary>
    /// A building's terms and the budget in the narration (<paramref name="terms"/>), the confirmation
    /// (<c>L_006b0</c>, …: Construct at <paramref name="construct"/>, Something Else four lines below,
    /// Don't Construct four more; a cancel shows it again), the price into <c>Tmp0001</c> and the funds
    /// check. Null when paid — the caller starts the building — else whether the list shows again:
    /// true after Something Else (<c>L_005ac</c>) or too little gold (<c>L_01136</c>), false after
    /// Don't Construct (<c>L_00175</c>, which leaves the windows as they are).
    /// </summary>
    private async Task<bool?> OfferAsync(int terms, int construct, int price, bool dontConstruct = false)
    {
        await WaoAsync(113);
        await ValuesAsync(terms, State.Gold);                           // "［name］ … （Budget: n00KG）"
        while (true)
        {
            await WaitForReleaseAsync();
            var items = new List<SceneMenuItem> { MenuItem(construct) };
            await LineBreakAsync();
            items.Add(MenuItem(construct + 4));                         // Something Else
            if (dontConstruct)
            {
                await LineBreakAsync();
                items.Add(MenuItem(construct + 8));                     // Don't Construct
            }
            int choice = await SelectAsync(items);
            if (choice == 1)
                break;
            if (choice == 2)
            {
                await WaoAsync(0);                                      // L_005ac
                return true;
            }
            if (choice == 3)
                return false;
        }
        State.Tmp0001 = (Sys3Int)price;
        await WaoAsync(0);
        if (State.Gold < State.Tmp0001)
        {
            await SpeakAsync(121, 966);                                 // L_01136 "lacks the necessary funds"
            await SpeakAsync(111, 973);
            return true;
        }
        State.Gold = State.Gold - State.Tmp0001;
        Hist.SundryExp = Hist.SundryExp + State.Tmp0001;                // QUIRK: CMD-14 — whatever history slot Hist_Cur names
        return null;
    }
}
