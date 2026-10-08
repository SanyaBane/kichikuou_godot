using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Setup;

// SET.ADV, the boot procedure, ported line by line (docs/strategy/survey-data.md §2.2). The DC /
// DS lines are the memory layout, which GameState generates (tools/codegen/gen_state.py); they are
// left out, with the RetVal / Tmp0001 arithmetic between them kept because its last values are
// what the script leaves. Assignments to record fields before their DS write the plain variable
// slots (QUIRK: DAT-01). The three record files are read from the player's decompile.
public sealed partial class GameSetup
{
    /// <summary><c>%#SET.ADV:</c> (<c>SET.ADV:1-348</c>): the initial world.</summary>
    public async Task SetAsync()
    {
        State.MapCamX = (Sys3Int)0; // :1
        State.MapCamY = (Sys3Int)0; // :2
        State.MapTargetX = (Sys3Int)0; // :3
        State.MapTargetY = (Sys3Int)0; // :4
        State.Bt_PSlot1State = (Sys3Int)0; // :5
        State.Bt_PSlot2State = (Sys3Int)0; // :6
        State.Bt_PSlot3State = (Sys3Int)0; // :7
        State.Bt_PSlot4State = (Sys3Int)0; // :8
        State.Bt_ESlot1State = (Sys3Int)0; // :9
        State.Bt_ESlot2State = (Sys3Int)0; // :10
        State.Bt_ESlot3State = (Sys3Int)0; // :11
        State.Bt_ESlot4State = (Sys3Int)0; // :12
        State.VAR0055 = (Sys3Int)100; // :13
        State.FadeSpeed = (Sys3Int)4; // :14
        State.DebugMode = (Sys3Int)0; // :15
        State.N_Kanami_State = (Sys3Int)1; // :16
        State.F_Helman_War = (Sys3Int)0; // :17
        State.F_Zeth_War = (Sys3Int)0; // :18
        State.N_Warg_Stage = (Sys3Int)0; // :19
        State.N_Miri_DeadlineWeek = (Sys3Int)0; // :20
        State.VAR0063 = (Sys3Int)0; // :21
        State.LadyCaravan_Prov = (Sys3Int)40; // :22
        State.N_Amiran_Stage = (Sys3Int)0; // :23
        State.N_Cell_Stage = (Sys3Int)0; // :24
        State.N_Shizuka_Stage = (Sys3Int)0; // :25
        State.VAR0068 = (Sys3Int)0; // :26
        State.VAR0069 = (Sys3Int)0; // :27
        State.Bt_BackgroundCg = (Sys3Int)900; // :28
        State.BgmEnabled = (Sys3Int)1; // :29
        State.Ui_ItemViewMode = (Sys3Int)1; // :30
        State.VAR0073 = (Sys3Int)0; // :31
        SetLiteral(33); // :33 MS 1 (placeholder name)
        SetLiteral(35); // :35 MS 2
        State.Memory.Var(74) = (Sys3Int)0; // :36 Unit_Troops (plain slot, not bound yet)
        State.Memory.Var(75) = (Sys3Int)0; // :37 Unit_MaxTroops (plain slot, not bound yet)
        State.Memory.Var(76) = (Sys3Int)0; // :38 Unit_TroopAtk (plain slot, not bound yet)
        State.Memory.Var(77) = (Sys3Int)0; // :39 Unit_TroopDef (plain slot, not bound yet)
        State.Memory.Var(78) = (Sys3Int)0; // :40 Unit_OffAtkMod (plain slot, not bound yet)
        State.Memory.Var(79) = (Sys3Int)0; // :41 Unit_OffDefMod (plain slot, not bound yet)
        State.Memory.Var(80) = (Sys3Int)0; // :42 Unit_DefAtkMod (plain slot, not bound yet)
        State.Memory.Var(81) = (Sys3Int)0; // :43 Unit_DefDefMod (plain slot, not bound yet)
        State.Memory.Var(82) = (Sys3Int)0; // :44 Unit_DgnAtkMod (plain slot, not bound yet)
        State.Memory.Var(83) = (Sys3Int)0; // :45 Unit_DgnDefMod (plain slot, not bound yet)
        State.Memory.Var(84) = (Sys3Int)0; // :46 Unit_CurAtkMod (plain slot, not bound yet)
        State.Memory.Var(85) = (Sys3Int)0; // :47 Unit_CurDefMod (plain slot, not bound yet)
        State.Memory.Var(86) = (Sys3Int)0; // :48 Unit_Class (plain slot, not bound yet)
        State.Memory.Var(87) = (Sys3Int)0; // :49 Unit_Atk (plain slot, not bound yet)
        State.Memory.Var(88) = (Sys3Int)0; // :50 Unit_Def (plain slot, not bound yet)
        State.Memory.Var(89) = (Sys3Int)0; // :51 Unit_Attacks (plain slot, not bound yet)
        State.Memory.Var(90) = (Sys3Int)0; // :52 Unit_HP (plain slot, not bound yet)
        State.Memory.Var(91) = (Sys3Int)0; // :53 Unit_MaxHP (plain slot, not bound yet)
        State.Memory.Var(92) = (Sys3Int)0; // :54 Unit_Strategy (plain slot, not bound yet)
        State.Memory.Var(93) = (Sys3Int)0; // :55 Unit_Magic (plain slot, not bound yet)
        State.Memory.Var(94) = (Sys3Int)0; // :56 Unit_CgFull (plain slot, not bound yet)
        State.Memory.Var(95) = (Sys3Int)0; // :57 Unit_CgPartial (plain slot, not bound yet)
        State.Memory.Var(96) = (Sys3Int)0; // :58 Unit_CgAlone (plain slot, not bound yet)
        State.Memory.Var(97) = (Sys3Int)0; // :59 Unit_FaceCg (plain slot, not bound yet)
        State.Memory.Var(98) = (Sys3Int)0; // :60 Unit_PortraitCg (plain slot, not bound yet)
        State.Memory.Var(99) = (Sys3Int)0; // :61 Unit_Recovery (plain slot, not bound yet)
        State.Memory.Var(100) = (Sys3Int)0; // :62 Unit_DeployCost (plain slot, not bound yet)
        State.Memory.Var(101) = (Sys3Int)0; // :63 Unit_Item (plain slot, not bound yet)
        State.Memory.Var(102) = (Sys3Int)0; // :64 Unit_Status (plain slot, not bound yet)
        State.Memory.Var(103) = (Sys3Int)0; // :65 Unit_Partner (plain slot, not bound yet)
        State.Memory.Var(104) = (Sys3Int)0; // :66 Unit_Exp (plain slot, not bound yet)
        State.Memory.Var(105) = (Sys3Int)0; // :67 Unit_Level (plain slot, not bound yet)
        SetLiteral(69); // :69 MS 4
        SetLiteral(71); // :71 MS 5
        SetLiteral(73); // :73 MS 6
        State.Memory.Var(106) = (Sys3Int)0; // :74 Cmdr_Troops (plain slot, not bound yet)
        State.Memory.Var(107) = (Sys3Int)0; // :75 Cmdr_MaxTroops (plain slot, not bound yet)
        State.Memory.Var(108) = (Sys3Int)0; // :76 Cmdr_TroopAtk (plain slot, not bound yet)
        State.Memory.Var(109) = (Sys3Int)0; // :77 Cmdr_TroopDef (plain slot, not bound yet)
        State.Memory.Var(110) = (Sys3Int)0; // :78 Cmdr_TroopAtkMod (plain slot, not bound yet)
        State.Memory.Var(111) = (Sys3Int)0; // :79 Cmdr_TroopDefMod (plain slot, not bound yet)
        State.Memory.Var(112) = (Sys3Int)0; // :80 Cmdr_Class (plain slot, not bound yet)
        State.Memory.Var(113) = (Sys3Int)0; // :81 Cmdr_Atk (plain slot, not bound yet)
        State.Memory.Var(114) = (Sys3Int)0; // :82 Cmdr_Def (plain slot, not bound yet)
        State.Memory.Var(115) = (Sys3Int)0; // :83 Cmdr_Attacks (plain slot, not bound yet)
        State.Memory.Var(116) = (Sys3Int)0; // :84 Cmdr_HP (plain slot, not bound yet)
        State.Memory.Var(117) = (Sys3Int)0; // :85 Cmdr_MaxHP (plain slot, not bound yet)
        State.Memory.Var(118) = (Sys3Int)0; // :86 Cmdr_Strategy (plain slot, not bound yet)
        State.Memory.Var(119) = (Sys3Int)0; // :87 Cmdr_Magic (plain slot, not bound yet)
        State.Memory.Var(120) = (Sys3Int)0; // :88 Cmdr_CgFull (plain slot, not bound yet)
        State.Memory.Var(121) = (Sys3Int)0; // :89 Cmdr_CgPartial (plain slot, not bound yet)
        State.Memory.Var(122) = (Sys3Int)0; // :90 Cmdr_CgAlone (plain slot, not bound yet)
        State.Memory.Var(123) = (Sys3Int)0; // :91 Cmdr_FaceCg (plain slot, not bound yet)
        State.Memory.Var(124) = (Sys3Int)0; // :92 Cmdr_Recovery (plain slot, not bound yet)
        State.Memory.Var(125) = (Sys3Int)0; // :93 Cmdr_Item (plain slot, not bound yet)
        State.Memory.Var(126) = (Sys3Int)0; // :94 VAR0126 (plain slot, not bound yet)
        State.Memory.Var(127) = (Sys3Int)0; // :95 VAR0127 (plain slot, not bound yet)
        State.Memory.Var(128) = (Sys3Int)0; // :96 Cmdr_Status (plain slot, not bound yet)
        State.Memory.Var(129) = (Sys3Int)0; // :97 VAR0129 (plain slot, not bound yet)
        SetLiteral(99); // :99 MS 8
        State.Memory.Var(130) = (Sys3Int)0; // :100 Prov_Owner (plain slot, not bound yet)
        State.Memory.Var(131) = (Sys3Int)0; // :101 Prov_Girls (plain slot, not bound yet)
        State.Memory.Var(132) = (Sys3Int)0; // :102 Prov_Economy (plain slot, not bound yet)
        State.Memory.Var(133) = (Sys3Int)0; // :103 Prov_Defense (plain slot, not bound yet)
        State.Memory.Var(134) = (Sys3Int)500; // :104 Prov_BattleArea (plain slot, not bound yet)  QUIRK: DAT-01
        State.Memory.Var(135) = (Sys3Int)0; // :105 Prov_BgCg (plain slot, not bound yet)
        State.Memory.Var(136) = (Sys3Int)0; // :106 VAR0136 (plain slot, not bound yet)
        State.Memory.Var(137) = (Sys3Int)0; // :107 Prov_Adj1 (plain slot, not bound yet)
        State.Memory.Var(138) = (Sys3Int)0; // :108 Prov_Adj2 (plain slot, not bound yet)
        State.Memory.Var(139) = (Sys3Int)0; // :109 Prov_Adj3 (plain slot, not bound yet)
        State.Memory.Var(140) = (Sys3Int)0; // :110 Prov_Adj4 (plain slot, not bound yet)
        State.Memory.Var(141) = (Sys3Int)0; // :111 Prov_Adj5 (plain slot, not bound yet)
        State.Memory.Var(142) = (Sys3Int)0; // :112 Prov_MapX (plain slot, not bound yet)
        State.Memory.Var(143) = (Sys3Int)0; // :113 Prov_MapY (plain slot, not bound yet)
        State.Memory.Var(144) = (Sys3Int)0; // :114 VAR0144 (plain slot, not bound yet)
        State.Memory.Var(145) = (Sys3Int)0; // :115 Dgn_MapX (plain slot, not bound yet)
        State.Memory.Var(146) = (Sys3Int)0; // :116 Dgn_MapY (plain slot, not bound yet)
        State.Memory.Var(147) = (Sys3Int)0; // :117 Dgn_State (plain slot, not bound yet)
        State.Memory.Var(148) = (Sys3Int)0; // :118 Dgn_SavedFloor (plain slot, not bound yet)
        State.Memory.Var(149) = (Sys3Int)0; // :119 Dgn_Prov (plain slot, not bound yet)
        SetLiteral(121); // :121 MS 7
        State.Memory.Var(150) = (Sys3Int)0; // :122 Item_Holder (plain slot, not bound yet)
        State.Memory.Var(151) = (Sys3Int)0; // :123 Item_Amount (plain slot, not bound yet)
        State.Memory.Var(152) = (Sys3Int)0; // :124 Item_Effect (plain slot, not bound yet)
        State.Unit_Count = (Sys3Int)52; // :125
        State.Unit_Cur = (Sys3Int)0; // :126
        State.Cmdr_Count = (Sys3Int)220; // :127
        State.Cmdr_Cur = (Sys3Int)0; // :128
        State.Prov_Count = (Sys3Int)100; // :129
        State.Prov_Cur = (Sys3Int)0; // :130
        State.Item_Count = (Sys3Int)50; // :131
        State.Item_Cur = (Sys3Int)0; // :132
        State.Dgn_Count = (Sys3Int)30; // :133
        State.Dgn_Cur = (Sys3Int)0; // :134
        // :135 DC 1, 10000 * 2, 1: — the layout (GameState.AllocatePages); fresh pages are zero
        State.RetVal = (Sys3Int)0; // :136
        State.Tmp0001 = State.Unit_Count; // :137
        State.RetVal = State.RetVal + State.Tmp0001 * (Sys3Int)32; // :170
        State.Tmp0001 = State.Cmdr_Count; // :171
        State.RetVal = State.RetVal + State.Tmp0001 * (Sys3Int)24; // :196
        State.Tmp0001 = State.Prov_Count; // :197
        State.RetVal = State.RetVal + State.Tmp0001 * (Sys3Int)15; // :213
        State.Tmp0001 = State.Item_Count; // :214
        State.RetVal = State.RetVal + State.Tmp0001 * (Sys3Int)3; // :218
        State.Tmp0001 = State.Dgn_Count; // :219
        // :225 DF Unit_Troops, 10000 * 2, 0: — the layout (GameState.AllocatePages); fresh pages are zero
        State.Hist_Count = (Sys3Int)300; // :226
        State.Hist_Cur = (Sys3Int)0; // :227
        State.Memory.Var(165) = (Sys3Int)0; // :228 Hist_Funds (plain slot, not bound yet)
        State.Memory.Var(166) = (Sys3Int)0; // :229 Hist_SundryExp (plain slot, not bound yet)
        State.Memory.Var(167) = (Sys3Int)0; // :230 Hist_MilitaryExp (plain slot, not bound yet)
        State.Memory.Var(168) = (Sys3Int)0; // :231 Hist_Casualties (plain slot, not bound yet)
        State.Memory.Var(169) = (Sys3Int)0; // :232 Harem_SexCount (plain slot, not bound yet)
        State.Memory.Var(170) = (Sys3Int)0; // :233 Harem_LastWeek (plain slot, not bound yet)
        State.Hist_PrevCur = (Sys3Int)0; // :234
        State.Memory.Var(172) = (Sys3Int)0; // :235 Hist_PrevFunds (plain slot, not bound yet)
        State.Memory.Var(173) = (Sys3Int)0; // :236 Hist_PrevSundryExp (plain slot, not bound yet)
        State.Memory.Var(174) = (Sys3Int)0; // :237 Hist_PrevMilitaryExp (plain slot, not bound yet)
        State.Memory.Var(175) = (Sys3Int)0; // :238 Hist_PrevCasualties (plain slot, not bound yet)
        State.Memory.Var(176) = (Sys3Int)0; // :239 VAR0176 (plain slot, not bound yet)
        State.Memory.Var(177) = (Sys3Int)0; // :240 VAR0177 (plain slot, not bound yet)
        // :241 DC 2, Hist_Count * 6, 1: — the layout (GameState.AllocatePages); fresh pages are zero
        State.RetVal = (Sys3Int)0; // :242
        State.Tmp0001 = State.Hist_Count; // :243
        State.Buf_Count = (Sys3Int)1000; // :256
        State.Buf_Cur = (Sys3Int)0; // :257
        State.Memory.Var(180) = (Sys3Int)0; // :258 Buf_Val (plain slot, not bound yet)
        State.VAR0181 = (Sys3Int)0; // :259
        State.Memory.Var(182) = (Sys3Int)0; // :260 VAR0182 (plain slot, not bound yet)
        State.Ui_BtnCount = (Sys3Int)1000; // :261
        State.Ui_BtnCur = (Sys3Int)0; // :262
        State.Memory.Var(185) = (Sys3Int)0; // :263 Ui_BtnX1 (plain slot, not bound yet)
        State.Memory.Var(186) = (Sys3Int)0; // :264 Ui_BtnY1 (plain slot, not bound yet)
        State.Memory.Var(187) = (Sys3Int)0; // :265 Ui_BtnX2 (plain slot, not bound yet)
        State.Memory.Var(188) = (Sys3Int)0; // :266 Ui_BtnY2 (plain slot, not bound yet)
        State.Memory.Var(189) = (Sys3Int)0; // :267 Ui_BtnW (plain slot, not bound yet)
        State.Memory.Var(190) = (Sys3Int)0; // :268 Ui_BtnH (plain slot, not bound yet)
        // :269 DC 3, 7000, 0: — the layout (GameState.AllocatePages); fresh pages are zero
        State.RetVal = (Sys3Int)0; // :270
        State.Tmp0001 = State.Buf_Count; // :271
        State.RetVal = State.RetVal + State.Tmp0001 * (Sys3Int)1; // :274
        State.Tmp0001 = State.Ui_BtnCount; // :275
        // :282 DC 4, 80 + 2000 + 20, 1: — the layout (GameState.AllocatePages); fresh pages are zero
        State.Glob_FateCur = (Sys3Int)0; // :283
        State.Glob_CgCur = (Sys3Int)0; // :284
        State.Glob_MenuCur = (Sys3Int)0; // :285
        State.VAR0194 = (Sys3Int)0; // :286
        State.Memory.Var(195) = (Sys3Int)0; // :287 Glob_Fate (plain slot, not bound yet)
        State.Memory.Var(196) = (Sys3Int)0; // :288 Glob_CgSeen (plain slot, not bound yet)
        State.Memory.Var(197) = (Sys3Int)0; // :289 Glob_MenuItem (plain slot, not bound yet)
        State.Glob_FateCur = (Sys3Int)0; // :293
        State.Glob_CgCur = (Sys3Int)0; // :294
        State.Glob_MenuCur = (Sys3Int)0; // :295
        // :296 DF Glob_Fate, 2100, 0: — the layout (GameState.AllocatePages); fresh pages are zero
        // :297 ZG Glob_CgSeen: — the engine counts CG loads in Glob_CgSeen (not modelled; ignored by the golden comparison)
        State.Unit_StrBase = (Sys3Int)20; // :298
        State.Cmdr_StrBase = (Sys3Int)20 + State.Unit_Count * (Sys3Int)3; // :299
        State.Prov_StrBase = (Sys3Int)20 + State.Unit_Count * (Sys3Int)3 + State.Cmdr_Count * (Sys3Int)3; // :300
        State.Item_StrBase = (Sys3Int)20 + State.Unit_Count * (Sys3Int)3 + State.Cmdr_Count * (Sys3Int)3 + State.Prov_Count; // :301
        State.Ui_MouseX = (Sys3Int)0; // :302
        State.Ui_MouseY = (Sys3Int)0; // :303
        State.Ui_DragX = (Sys3Int)0; // :304
        State.Ui_DragY = (Sys3Int)0; // :305
        State.Ui_DragPending = (Sys3Int)0; // :306
        State.VAR0207 = (Sys3Int)0; // :307
        State.VAR0208 = (Sys3Int)0; // :308
        State.Ui_FaceOverMap = (Sys3Int)1; // :309
        State.Ui_MarkCur = (Sys3Int)0; // :310
        State.Memory.Var(211) = (Sys3Int)0; // :311 Ui_MarkCg (plain slot, not bound yet)
        State.Ui_MarkCount = (Sys3Int)0; // :312
        State.Memory.Var(213) = (Sys3Int)0; // :313 Ui_MarkX (plain slot, not bound yet)
        State.Memory.Var(214) = (Sys3Int)0; // :314 Ui_MarkY (plain slot, not bound yet)
        State.Ui_MarkNextCur = (Sys3Int)0; // :315
        State.Memory.Var(216) = (Sys3Int)0; // :316 Ui_MarkNextCg (plain slot, not bound yet)
        State.Memory.Var(217) = (Sys3Int)0; // :317 Ui_MarkNextX (plain slot, not bound yet)
        State.Memory.Var(218) = (Sys3Int)0; // :318 Ui_MarkNextY (plain slot, not bound yet)
        State.RetVal = (Sys3Int)100; // :319
        // :320 DC 10, RetVal * 3, 1: — the layout (GameState.AllocatePages); fresh pages are zero
        // :327 DF Ui_MarkCg, RetVal * 3, 0: — the layout (GameState.AllocatePages); fresh pages are zero
        await Script.RunAsync(Scenario.ReadFile("SET_MAP.ADV")); // :328
        await Script.RunAsync(Scenario.ReadFile("SET_CHR.ADV")); // :329
        await Script.RunAsync(Scenario.ReadFile("SET_ITEM.ADV")); // :330
        // :331 %#ﾓｼﾞｷｬｯｼｭ.ADV: — warms the glyph cache, display only (no state)
        // :332 ZD 0, 1: — debug-message switch, no state
        State.Ui_MapCgTopL = (Sys3Int)812; // :333
        State.Ui_MapCgBotL = (Sys3Int)801; // :334
        State.Ui_MapCgTopM = (Sys3Int)802; // :335
        State.Ui_MapCgBotM = (Sys3Int)803; // :336
        State.Ui_MapCgTopR = (Sys3Int)804; // :337
        State.Ui_MapCgBotR = (Sys3Int)805; // :338
        State.RetVal = (Sys3Int)10; // :339
        await Script.CallFileAsync("ﾛｰﾄﾞｾｰﾌﾞ.ADV"); // :340 mode 10 (RetVal): page 4 from every save slot
        State.Ui_ScrollWait = (Sys3Int)2; // :341
        if (State.SpecGraphics / State.SpecBaseline > (Sys3Int)2) // :342
            State.Ui_ScrollWait = (Sys3Int)1; // :343  QUIRK: DAT-04
        if (State.SpecGraphics / State.SpecBaseline > (Sys3Int)3) // :345
            State.Ui_ScrollWait = (Sys3Int)0; // :346
    }

    /// <summary><c>MS n, "text":</c> at <c>SET.ADV:line</c>, with the text from the player's files.</summary>
    private void SetLiteral(int line)
    {
        var at = new ScriptRef(SetFile, line);
        var statement = Scenario.ReadStatement(at) as SetStringStatement
            ?? throw new ScenarioFormatException($"{at}: expected MS n, \"text\".");
        Script.SetString(statement.Slot, statement.Text);
    }
}
