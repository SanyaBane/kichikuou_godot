using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

// WAO_SUB.ADV, the message-window helper, ported for its state effects only: the window state
// (Ui_WinType, Ui_WinFrameCg), the pictures (Ui_PortraitL/R, Ui_CenterCg, Ui_CenterOverlay1-4,
// Ui_FaceOverMap), the scratch it leaves (Tmp0021/0022 from L_00b0f whenever a picture is
// removed) and the key waits (ops 1, 5, 11), which do nothing while no window is open. Drawing
// (CC, CF, B3/B4, G, J0, ZC, ZM, Y 1) is the WindowEvent raised first; each G counts its CG
// (CountCgLoad). Callers set CallArg for a picture before the call, as the script does.
public sealed partial class ScriptContext
{
    /// <summary>
    /// <c>!Ui_WaoOp : op! %#WAO_SUB.ADV:</c> — sets <c>Ui_WaoOp</c>, shows the
    /// <see cref="WindowEvent"/>, then applies the operation's state effects
    /// (<see cref="WaoSubAsync"/>). The key waits (ops 1, 5, 11) run <c>IK 0</c> through
    /// <see cref="IScriptInput.WaitForKeyAsync"/> unless no window is open.
    /// </summary>
    public async Task WaoAsync(Sys3Int op)
    {
        State.Ui_WaoOp = op;
        await View.ShowAsync(new WindowEvent(op, State.CallArg));
        await WaoSubAsync();
    }

    /// <summary>
    /// <c>Ui_WaoOp := 11; call WAO_SUB.ADV</c> (<c>WAO_SUB.ADV:510-519</c>): waits with
    /// <c>IK 0</c>, which leaves the key code in <c>RetVal</c> (nothing while no window is open).
    /// </summary>
    public Task WaoWaitKeyAsync() => WaoAsync(11);

    /// <summary>
    /// <c>Ui_WaoOp := 1; call WAO_SUB.ADV</c> (<c>WAO_SUB.ADV:286-294</c>): waits with
    /// <c>US RetVal; IK 0; UG RetVal</c>, so <c>RetVal</c> is preserved (nothing while no window is open).
    /// </summary>
    public Task WaoWaitKeyKeepRetValAsync() => WaoAsync(1);

    /// <summary><c>%#WAO_SUB.ADV</c> with the operation in <c>Ui_WaoOp</c> (<c>WAO_SUB.ADV:1-169</c>).</summary>
    internal async Task WaoSubAsync()
    {
        switch (State.Ui_WaoOp.Value)
        {
            case 0:
                // L_005fc (:253-277) — close the window.
                if (State.Ui_WinType == 0)
                    return;
                State.Ui_WinType = 0;
                State.Ui_WinFrameCg = 0;
                return;
            case 1:
                // L_006fe (:286-294) — US RetVal, 1; IK 0; UG RetVal, 1: RetVal survives the key.
                if (State.Ui_WinType == 0)
                    return;
                _ = await Input.WaitForKeyAsync();
                WaoClear();                 // falls into L_0072b
                return;
            case 2:
                if (State.Ui_WinType == 0)
                    return;
                WaoClear();                 // L_0072b
                return;
            case 3:
                State.Ui_FaceOverMap = 1;   // L_00773 (:307-309)
                return;
            case 4:
                State.Ui_FaceOverMap = 0;   // L_0077d (:310-312)
                return;
            case 5:
            case 11:
                // L_006d8 (:278-285) / L_00ce1 (:510-519) — IK 0 stores the key in RetVal.
                if (State.Ui_WinType == 0)
                    return;
                Sys3Int key = await Input.WaitForKeyAsync();
                State.RetVal = key;
                if (State.Ui_WaoOp == 5)
                    WaoClear();             // @L_0072b
                else
                    CountCgLoad(State.Ui_WinFrameCg);   // :517-518 Y 1, 1; G Ui_WinFrameCg, 40
                return;
            case 10:
                // L_00cb9 (:504-509) — close the dialog box.
                State.Ui_WinType = 0;
                State.Ui_WinFrameCg = 0;
                return;
            case 111: WaoOpen(807, 11); return;     // L_00408 (:170-178)
            case 112: WaoOpen(808, 12); return;     // L_0042e (:179-187)
            case 113:
            case 123: WaoOpen(809, 43); return;     // L_0059e (:239-247)
            case 121: WaoOpen(810, 21); return;     // L_00489 (:193-201)
            case 122: WaoOpen(811, 22); return;     // L_004af (:202-210)
            case 131:
                // L_0050c (:216-233) — the full-screen window clears every picture.
                State.Ui_WinFrameCg = 815;
                CountCgLoad(State.Ui_WinFrameCg);       // :219 G Ui_WinFrameCg (no sprite key)
                State.Ui_PortraitL = 0;
                State.Ui_PortraitR = 0;
                State.Ui_CenterCg = 0;
                State.Ui_CenterOverlay1 = 0;
                State.Ui_CenterOverlay2 = 0;
                State.Ui_CenterOverlay3 = 0;
                State.Ui_CenterOverlay4 = 0;
                State.Ui_WinType = 31;
                return;
            case 141: WaoOpen(895, 41); return;     // L_00b78 (:453-459)
            case 142: WaoOpen(896, 42); return;     // L_00b94 (:460-467)
            case 143:
            case 153: WaoOpen(894, 43); return;     // L_00c65 (:493-499)
            case 151: WaoOpen(897, 51); return;     // L_00bee (:473-479)
            case 152: WaoOpen(898, 52); return;     // L_00c0a (:480-487)
            // The second entry of each window (2xx) sets the type without drawing the frame.
            case 211: State.Ui_WinType = 11; return;
            case 212: State.Ui_WinType = 12; return;
            case 213:
            case 223:
            case 243:
            case 253: State.Ui_WinType = 43; return;
            case 221: State.Ui_WinType = 21; return;
            case 222: State.Ui_WinType = 22; return;
            case 231: State.Ui_WinType = 31; return;
            case 241: State.Ui_WinType = 41; return;
            case 242: State.Ui_WinType = 42; return;
            case 251: State.Ui_WinType = 51; return;
            case 252: State.Ui_WinType = 52; return;
            case 311:
                // L_00787 (:313-320) — left portrait CallArg, replacing the one shown.
                if (State.Ui_PortraitL != 0)
                    WaoRemovePortraitL();
                CountCgLoad(State.CallArg);             // :318 G CallArg, 40
                State.Ui_PortraitL = State.CallArg;
                return;
            case 312:
                WaoRemovePortraitL();
                return;
            case 321:
                // L_008b1 (:352-359) — right portrait.
                if (State.Ui_PortraitR != 0)
                    WaoRemovePortraitR();
                CountCgLoad(State.CallArg);             // :357 G CallArg, 40
                State.Ui_PortraitR = State.CallArg;
                return;
            case 322:
                WaoRemovePortraitR();
                return;
            case 331:
                // L_009e7 (:391-402) — centre picture CallArg without overlays.
                if (State.Ui_CenterCg != 0)
                    WaoRemoveCenter();
                CountCgLoad(State.CallArg);             // :396 G CallArg
                State.Ui_CenterCg = State.CallArg;
                State.Ui_CenterOverlay1 = 0;
                State.Ui_CenterOverlay2 = 0;
                State.Ui_CenterOverlay3 = 0;
                State.Ui_CenterOverlay4 = 0;
                return;
            case 332:
                WaoRemoveCenter();
                return;
            case 333:
                // L_00a25 (:403-422) — centre picture CallArg with overlays Tmp0001-0004.
                if (State.Ui_CenterCg != 0)
                    WaoRemoveCenter();
                CountCgLoad(State.CallArg);             // :408-416 G CallArg, then each overlay (G 0 loads nothing)
                CountCgLoad(State.Tmp0001);
                CountCgLoad(State.Tmp0002);
                CountCgLoad(State.Tmp0003);
                CountCgLoad(State.Tmp0004);
                State.Ui_CenterCg = State.CallArg;
                State.Ui_CenterOverlay1 = State.Tmp0001;
                State.Ui_CenterOverlay2 = State.Tmp0002;
                State.Ui_CenterOverlay3 = State.Tmp0003;
                State.Ui_CenterOverlay4 = State.Tmp0004;
                return;
            default:
                // :165-169 prints an error and waits (A); no ported file reaches it.
                throw new InvalidOperationException($"WAO_SUB.ADV operation {State.Ui_WaoOp} does not exist.");
        }
    }

    /// <summary>
    /// Opens a framed window: <c>Ui_WinFrameCg := frame</c> and its <c>G Ui_WinFrameCg, 40</c>, the
    /// box (display only), <c>Ui_WinType := type</c>.
    /// </summary>
    private void WaoOpen(Sys3Int frame, Sys3Int type)
    {
        State.Ui_WinFrameCg = frame;
        CountCgLoad(State.Ui_WinFrameCg);
        State.Ui_WinType = type;
    }

    /// <summary>
    /// <c>L_0072b</c> (<c>WAO_SUB.ADV:295-306</c>, a window open): <c>Y 1, 1</c> clears the text and
    /// the frame is drawn again — twice for the full-screen windows (types 31-39).
    /// </summary>
    private void WaoClear()
    {
        if (State.Ui_WinType > 30 && State.Ui_WinType < 40)
            CountCgLoad(State.Ui_WinFrameCg);   // :302-304 G Ui_WinFrameCg
        CountCgLoad(State.Ui_WinFrameCg);       // :305 G Ui_WinFrameCg, 40
    }

    /// <summary><c>L_007b1</c> (<c>WAO_SUB.ADV:321-351</c>): restores the screen under the left portrait.</summary>
    private void WaoRemovePortraitL()
    {
        WaoMapCorner();
        WaoRedrawCenter();
        State.Ui_PortraitL = 0;
    }

    /// <summary><c>L_008dd</c> (<c>WAO_SUB.ADV:360-390</c>): restores the screen under the right portrait.</summary>
    private void WaoRemovePortraitR()
    {
        WaoMapCorner();
        WaoRedrawCenter();
        State.Ui_PortraitR = 0;
    }

    /// <summary>
    /// <c>WAO_SUB.ADV:329-348</c> / <c>:368-387</c>: a portrait's removal draws the centre picture
    /// again when one is shown, with each overlay that is set.
    /// </summary>
    private void WaoRedrawCenter()
    {
        if (State.Ui_CenterCg == 0)
            return;
        CountCgLoad(State.Ui_CenterCg);
        if (State.Ui_CenterOverlay1 != 0)
            CountCgLoad(State.Ui_CenterOverlay1);
        if (State.Ui_CenterOverlay2 != 0)
            CountCgLoad(State.Ui_CenterOverlay2);
        if (State.Ui_CenterOverlay3 != 0)
            CountCgLoad(State.Ui_CenterOverlay3);
        if (State.Ui_CenterOverlay4 != 0)
            CountCgLoad(State.Ui_CenterOverlay4);
    }

    /// <summary><c>L_00a9b</c> (<c>WAO_SUB.ADV:423-436</c>): restores the screen under the centre picture.</summary>
    private void WaoRemoveCenter()
    {
        WaoMapCorner();
        State.Ui_CenterCg = 0;
        State.Ui_CenterOverlay1 = 0;
        State.Ui_CenterOverlay2 = 0;
        State.Ui_CenterOverlay3 = 0;
        State.Ui_CenterOverlay4 = 0;
    }

    /// <summary>
    /// <c>L_00b0f</c> (<c>WAO_SUB.ADV:437-452</c>): <c>Tmp0021/0022</c> := the camera, kept inside
    /// 320..1600 × 200..600. Called by every picture removal whatever <c>Ui_FaceOverMap</c> is.
    /// </summary>
    private void WaoMapCorner()
    {
        State.Tmp0021 = State.MapCamX;
        if (State.Tmp0021 > 1920 - 320)
            State.Tmp0021 = 1920 - 320;
        if (State.Tmp0021 < 320)
            State.Tmp0021 = 320;
        State.Tmp0022 = State.MapCamY;
        if (State.Tmp0022 > 800 - 200)
            State.Tmp0022 = 800 - 200;
        if (State.Tmp0022 < 200)
            State.Tmp0022 = 200;
    }
}
