using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Cﾒｲｷｭｳ.ADV</c>, a dungeon clicked on the map (<c>docs/strategy/survey-dungeon.md</c>), so far
/// its entry and the Thieves' Hideout: the entry checks and resets, the camera, the week's dungeon
/// action spent, the dungeon's name (string 8, from the player's files) and background, the Cafe
/// trip's takeover (<c>:1-166</c>); then the hideout's banter (<c>L_03cbd</c>, with the prologue's
/// <c>Y 4, 3</c>), or, in the prologue, nothing more. The search (part D1) and the dispatch of
/// the later stages fail as not ported.
/// </summary>
public sealed class DungeonCommand(ScriptContext script, IScenarioSource scenario) : CommandFile(script, "Cﾒｲｷｭｳ.ADV")
{
    /// <summary>
    /// <c>:29-150</c>: each dungeon's <c>MS 8, "name"</c> line (<c>MS 9</c> for 9, never printed —
    /// DGN-15) and its background if it has its own (0: the default 1032 of <c>:28</c>).
    /// </summary>
    private static readonly (int Dungeon, int NameLine, int Background)[] Names =
    [
        (1, 31, 1008), (2, 36, 0), (3, 40, 0), (4, 44, 0), (5, 48, 1017), (6, 57, 0), (7, 61, 1029),
        (8, 66, 0), (9, 70, 1037), (10, 75, 0), (11, 79, 1025), (12, 84, 0), (13, 88, 0), (14, 92, 0),
        (15, 96, 0), (16, 100, 1016), (17, 105, 0), (18, 109, 0), (19, 113, 0), (20, 117, 0),
        (21, 121, 0), (22, 125, 0), (23, 129, 0), (24, 133, 0), (25, 137, 0), (26, 145, 0), (30, 149, 0),
    ];

    /// <summary><c>Cﾒｲｷｭｳ.ADV:1-237</c> as far as it is ported: the entry, then the dispatch.</summary>
    public async Task RunAsync()
    {
        if (State.Ev_CommandMode == 2)                                  // :1-3 the forced Maria attack
            return;
        if (State.Ui_CmdDungeonAvail == 0 && State.StoryStage != 0)
            throw NotPorted(5, "a dungeon already searched this week (L_07418)", "D1");
        State.DgnRun_MerimInParty = 0;                                  // :7-16
        if (State.F_Merim_MillArc == 3)
            State.F_Merim_MillArc = 4;
        // QUIRK: DGN-01 — whatever commander record the cursor was left on.
        if (Cmdr.CgFull == 1137)
            State.F_Gunagan_State = 2;
        if (State.Dgn_Cur == 9 && State.F_PG9_State != 2)
            return;
        State.MapTargetX = Dgn.MapX;                                    // :17-24 the camera on the dungeon
        State.MapTargetY = Dgn.MapY;
        await Script.MapCallAsync(1);
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
        await Script.MapCallAsync(10);
        // QUIRK: DGN-14 — the week's dungeon action is spent before anything is known.
        State.Ui_CmdDungeonAvail = 0;                                   // :25
        SetLiteral(27);                                                 // MS 10, "Monsters"
        State.DungeonBgCg = 1032;
        foreach (var (dungeon, nameLine, background) in Names)          // :29-150, each test on the cursor as it is then
        {
            if (State.Dgn_Cur != (Sys3Int)dungeon)
                continue;
            SetLiteral(nameLine);
            if (background != 0)
                State.DungeonBgCg = (Sys3Int)background;
            if (dungeon == 5 && Dgn.State == 0)
                await Script.EventAsync("Gﾎﾗｶﾞ.ADV", 2);
            if (dungeon == 25 && State.F_HannyKing_State > 1)
                SetLiteral(140);                                        // "Haniwa Temple"
        }
        if (State.Ev_CommandMode == 1 && State.Dgn_Cur == 10)          // :151-161 the Cafe trip's search
        {
            await Script.EventAsync("Gｶﾌｪ.ADV", 32);
            State.Unit_Cur = 1;
            CopyString(1, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
            State.Bt_PlayerParty1 = 1;
            State.Bt_PlayerParty2 = 0;
            State.Bt_PlayerParty3 = 0;
            State.Bt_PlayerParty4 = 0;
            throw NotPorted(160, "the dungeon search (L_00a2c)", "D1");
        }
        if (State.Ev_CommandMode == 1)                                  // :162-166
        {
            await Script.EventAsync("Gｶﾌｪ.ADV", 31);
            return;
        }

        // L_005b6 (:167-174)
        if (State.Dgn_Cur == 1)
        {
            State.Ui_CmdDungeonAvail = 1;                               // the hideout costs nothing
            await HideoutAsync();
            return;
        }
        if (State.StoryStage == 0)
            return;
        throw NotPorted(175, "the dungeon dispatch beyond the prologue", "D1");
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

    /// <summary><c>ZM 32: 'title' ZM 24:</c> — the place's name in the large font.</summary>
    private async Task TitleAsync(int line)
    {
        await Script.ShowAsync(new EngineEvent("ZM", [(Sys3Int)32]));
        await TextAsync(line);
        await Script.ShowAsync(new EngineEvent("ZM", [(Sys3Int)24]));
    }

    /// <summary><c>MS n, "text":</c> at <paramref name="line"/> of the file, with the text from the player's files.</summary>
    private void SetLiteral(int line)
    {
        var at = Ref(line);
        var statement = scenario.ReadStatement(at) as SetStringStatement
            ?? throw new ScenarioFormatException($"{at}: expected MS n, \"text\".");
        Script.SetString(statement.Slot, statement.Text);
    }
}
