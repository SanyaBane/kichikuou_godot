using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Cﾌﾞｶ.ADV</c>, the Subordinates command (<c>docs/strategy/survey-commands.md</c> §3.5): the
/// picker <c>Sﾌﾞｶ</c> (a hook, called with its mode in <c>CallArg</c>; it answers the unit in
/// <c>CallArg</c> and <c>Unit_Cur</c>), the view-only loop once the week's actions are spent, the
/// unit's status page, and the prologue's talk (<c>:713-720</c>: no menu, the unit's talk scene at
/// once, the command spent). The troop menu of the later stages (<c>:721-1097</c>) is part P6 and
/// fails as not ported.
/// </summary>
public sealed class SubordinatesCommand(ScriptContext script) : CommandFile(script, "Cﾌﾞｶ.ADV")
{
    /// <summary>
    /// The talk scene of each unit (<c>:208-712</c>, one label per unit, all called with the
    /// <c>Ev_Entry</c> of 20 set at <c>:25</c>); Rance (1), Honey/Prince (48) and the mercenaries
    /// (49, 50) are handled apart.
    /// </summary>
    private static readonly Dictionary<int, string> TalkFiles = new()
    {
        [2] = "Gｼｨﾙ.ADV", [3] = "Gﾊﾞｳﾝﾄﾞ.ADV", [4] = "Gｿｳﾙ.ADV", [5] = "Gﾚｲﾗ.ADV",
        [6] = "Gﾊﾞﾚｽ.ADV", [7] = "Gﾘｯｸ.ADV", [8] = "Gｺﾙﾄﾞﾊﾞ.ADV", [9] = "Gｷﾝｹｰﾄﾞ.ADV",
        [10] = "Gﾒﾅﾄﾞ.ADV", [11] = "Gｶﾄﾞﾐｭｳﾑ.ADV", [12] = "Gﾊｳﾚｰﾝ.ADV", [13] = "Gﾒﾙﾌｪｲｽ.ADV",
        [14] = "Gｴｸｽ.ADV", [15] = "Gｼﾉﾀﾞ.ADV", [16] = "Gﾏﾘｱ.ADV", [17] = "Gｼｽﾞｶ.ADV",
        [18] = "Gﾊﾟｯﾄﾝ.ADV", [19] = "Gﾊﾝﾃｨ.ADV", [20] = "Gﾌﾘｰｸ.ADV", [21] = "Gﾋｭｰﾊﾞｰﾄ.ADV",
        [22] = "Gﾚﾘｭｰｺﾌ.ADV", [23] = "Gﾗﾝ.ADV", [24] = "Gﾐﾘ.ADV", [25] = "Gﾐﾙ.ADV",
        [26] = "Gｶｾｲ.ADV", [27] = "Gｲｿﾛｸ.ADV", [28] = "Gｸﾘｰﾑ.ADV", [29] = "Gｻﾃﾗ.ADV",
        [30] = "Gﾒｶﾞﾗｽ.ADV", [31] = "Gｹﾝﾀﾛｳ.ADV", [32] = "Gﾐｷ.ADV", [33] = "Gｼﾞｭﾘｱ.ADV",
        [34] = "Gﾗﾌｧﾘｱ.ADV", [35] = "Gｱｰﾙｺｰﾄ.ADV", [36] = "Gｱﾋﾞｧﾄｰﾙ.ADV", [37] = "Gｶﾞﾝｼﾞｰ.ADV",
        [38] = "Gﾁｽﾞｺ.ADV", [39] = "Gﾏｼﾞｯｸ.ADV", [40] = "Gﾅｷﾞ.ADV", [41] = "Gｶﾞﾙﾃｨｱ.ADV",
        [42] = "Gｻｲｾﾞﾙ.ADV", [43] = "Gｱﾘｽﾄﾚｽ.ADV", [44] = "GAL.ADV", [45] = "Gｶﾌｪ.ADV",
        [46] = "Gｿﾐｰﾀ.ADV", [47] = "Gﾛﾚｯｸｽ.ADV",
    };

    /// <summary><c>Cﾌﾞｶ.ADV:1-207</c>: the command, up to its return.</summary>
    public async Task RunAsync()
    {
        if (State.Ui_CmdSubordAvail == 0)                               // :1-20
        {
            await ViewOnlyAsync();
            return;
        }
        if (State.F_SillCow_State == 3)                                 // :21-24
            await Script.EventAsync("Gｼｨﾙ.ADV", 3);
        // QUIRK: EVT-06 — set once here; every talk scene below is called with it.
        State.Ev_Entry = 20;                                            // :25
        await PickerAsync(2);
        while (true)
        {
            // L_000ca (:28-43): pick a unit; one that cannot act shows its status page.
            State.CallArg = 1;
            State.RetVal = 1;
            await Script.CallFileAsync("Sﾌﾞｶ.ADV");
            if (State.CallArg == 0 || State.CallArg == 503)
                return;                                                 // L_00550
            UnitNames();
            if (Unit.Status != 1)
            {
                await StatusPageAsync();                                // \L_0179c
                await PickerAsync(5);
                continue;
            }
            State.Tmp0021 = State.CallArg;                              // :44-47
            await PickerAsync(3);
            State.CallArg = State.Tmp0021;

            int unit = State.CallArg.Value;                             // :48-197 the unit's label
            if (unit is < 1 or > 50)
            {
                await ValuesAsync(199, State.CallArg);                  // "部下番号エラー：n"
                await HitAnyKeyAsync();
                return;                                                 // L_00550
            }
            State.Unit_Cur = (Sys3Int)unit;
            await ActionAsync();                                        // \L_00f4b
            if (State.Tmp0001 == 1)
            {
                await PickerAsync(4);                                   // L_00553
                continue;
            }
            await TalkAsync(unit);
            return;
        }
    }

    /// <summary>
    /// <c>:1-20</c> — the week's actions are spent: pick a unit to see its status page, until
    /// the picker is cancelled.
    /// </summary>
    private async Task ViewOnlyAsync()
    {
        await PickerAsync(2);
        while (true)
        {
            // L_00032
            State.CallArg = 1;
            State.RetVal = 1;
            await Script.CallFileAsync("Sﾌﾞｶ.ADV");
            if (State.CallArg != 0 && State.CallArg != 503)
            {
                UnitNames();
                await StatusPageAsync();                                // \L_0179c
                await PickerAsync(5);
                continue;
            }
            await PickerAsync(3);
            return;
        }
    }

    /// <summary><c>!CallArg : mode! %#Sﾌﾞｶ.ADV:</c> — the picker in one of its modes (2 open, 3 close, 4/5 redraw).</summary>
    private Task PickerAsync(Sys3Int mode)
    {
        State.CallArg = mode;
        return Script.CallFileAsync("Sﾌﾞｶ.ADV");
    }

    /// <summary><c>:9-11</c>, <c>:35-37</c>: the unit's three name strings into strings 1-3.</summary>
    private void UnitNames()
    {
        CopyString(1, State.Unit_StrBase + (State.Unit_Cur - 1) * 3 + 0);
        CopyString(2, State.Unit_StrBase + (State.Unit_Cur - 1) * 3 + 1);
        CopyString(3, State.Unit_StrBase + (State.Unit_Cur - 1) * 3 + 2);
    }

    /// <summary>
    /// <c>L_00f4b</c> (<c>:713-720</c>): in the prologue there is no menu — the command is spent
    /// and the talk follows (<c>Tmp0001</c> 2). The troop menu of the later stages is part P6.
    /// </summary>
    private Task ActionAsync()
    {
        CopyString(1, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
        CopyString(2, State.Unit_StrBase + (State.Unit_Cur - 1) * 3 + 1);
        if (State.StoryStage == 0)
        {
            State.Ui_CmdSubordAvail = 0;
            State.Tmp0001 = 2;
            return Task.CompletedTask;
        }
        throw NotPorted(721, "the troop menu beyond the prologue", "P6");
    }

    /// <summary>The <c>Tmp0001 = 2</c> branch of the unit's label (<c>:208-712</c>): its talk.</summary>
    private async Task TalkAsync(int unit)
    {
        switch (unit)
        {
            case 1:
                if (State.Tmp0001 == 2)
                    await SelfTalkAsync();                              // \L_01cf3
                return;
            case 48:                                                    // :706-711
                if (State.Tmp0001 == 2 && State.Unit48_Identity == 1)
                    await Script.CallFileAsync("Gﾊﾆｰ.ADV");
                if (State.Tmp0001 == 2 && State.Unit48_Identity == 2)
                    await Script.CallFileAsync("Gｵｳｼﾞ.ADV");
                return;
            case 49:
            case 50:
                if (State.Tmp0001 == 2)
                    await Script.EventAsync("Gﾖｳﾍｲ.ADV", (Sys3Int)(unit == 49 ? 3 : 4));
                return;
            default:
                if (State.Tmp0001 == 2)
                    await Script.CallFileAsync(TalkFiles[unit]);
                return;
        }
    }

    /// <summary>
    /// <c>L_01cf3</c> (<c>:1338-1377</c>) — Rance talks to himself; the line depends on how often
    /// he has (<c>N_RanceSelfTalk</c>).
    /// </summary>
    private async Task SelfTalkAsync()
    {
        State.N_RanceSelfTalk = State.N_RanceSelfTalk + 1;
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_01e1e
        await WaoAsync(111);                                            // \L_01dca
        await TextAsync(1344);
        await WaoAsync(1);                                              // \L_01eef
        if (State.N_RanceSelfTalk < 10)
        {
            await TextAsync(1348);
            await WaoAsync(1);
            await WaoAsync(0);                                          // \L_01ee2
            return;
        }
        if (State.N_RanceSelfTalk < 20)
        {
            await TextAsync(1357);
            await WaoAsync(1);
            await TextAsync(1366);
            await WaoAsync(1);
            await WaoAsync(0);
            return;
        }
        await TextAsync(1374);
        await WaoAsync(1);
        await WaoAsync(0);
    }

    /// <summary>
    /// <c>L_0179c</c> (<c>:1113-1337</c>) — the unit's status, page by page: level, HP, attack,
    /// experience; strategy, magic, attacks; with troops, the situational modifiers and the troops;
    /// with an item, the item. Each page waits for a key (<c>WAO_SUB</c> op 5); a right click
    /// (<c>RetVal</c> 32) closes the window and leaves.
    /// </summary>
    private async Task StatusPageAsync()
    {
        await WaoAsync(113);                                            // \L_01e02
        await ValuesAsync(1116, Unit.Level, Unit.HP, Unit.MaxHP, Unit.Atk, Unit.Def, Unit.Exp, (Sys3Int)100 + Unit.Level * 30);
        if (await PageEndAsync())
            return;
        await ValuesAsync(1149, Unit.Strategy, Unit.Magic, Unit.Attacks);
        if (await PageEndAsync())
            return;
        if (Unit.MaxTroops != 0)
        {
            await TextAsync(1169);                                      // "Situational Command Ability / On Offense (Atk"
            await ModifierAsync(1174, 1179, Unit.OffAtkMod);
            await TextAsync(1183);                                      // "／Def"
            await ModifierAsync(1186, 1191, Unit.OffDefMod);
            await TextAsync(1195);                                      // ") / On Defense (Atk"
            await ModifierAsync(1200, 1205, Unit.DefAtkMod);
            await TextAsync(1209);
            await ModifierAsync(1212, 1217, Unit.DefDefMod);
            await TextAsync(1221);                                      // ") / In Dungeons (Atk"
            await ModifierAsync(1226, 1231, Unit.DgnAtkMod);
            await TextAsync(1235);
            await ModifierAsync(1238, 1243, Unit.DgnDefMod);
            await TextAsync(1247);                                      // ")"
            if (await PageEndAsync())
                return;
            await ValuesAsync(1254, Unit.Troops, Unit.MaxTroops, Unit.TroopAtk, Unit.TroopDef);
            if (await PageEndAsync())
                return;
        }
        if (Unit.Item != 0)
        {
            State.Item_Cur = Unit.Item;
            CopyString(7, State.Item_StrBase + State.Item_Cur - 1);
            await TextAsync(1280);                                      // "（item）"
            if (Item.Effect == 1)
                await ValuesAsync(1286, Item.Amount);
            if (Item.Effect == 2)
                await ValuesAsync(1293, Item.Amount);
            if (Item.Effect == 3)
                await ValuesAsync(1300, Item.Amount);
            if (Item.Effect == 4)
                await ValuesAsync(1307, Item.Amount);
            if (Item.Effect == 5)
                await ValuesAsync(1314, Item.Amount);
            if (Item.Effect == 6)
                await ValuesAsync(1321, Item.Amount);
            if (Item.Effect == 0)
                await TextAsync(1328);                                  // "Abnormal Data"
            if (await PageEndAsync())
                return;
        }
        await WaoAsync(0);                                              // \L_01ee2
    }

    /// <summary>
    /// A modifier of the status page: values under 10 are bonuses ("＋n"), over 10 penalties
    /// ("－(n−10)"), 10 shows nothing.
    /// </summary>
    private async Task ModifierAsync(int plusLine, int minusLine, Sys3Int modifier)
    {
        if (modifier < 10)
            await ValuesAsync(plusLine, modifier);
        if (modifier > 10)
            await ValuesAsync(minusLine, modifier - 10);
    }

    /// <summary>
    /// <c>\L_01f09: {RetVal = 32: \L_01ee2: \0:}</c> — the key wait at the end of a page; true
    /// (the window closed) when it was a right click.
    /// </summary>
    private async Task<bool> PageEndAsync()
    {
        await WaoAsync(5);
        if (State.RetVal != 32)
            return false;
        await WaoAsync(0);
        return true;
    }
}
