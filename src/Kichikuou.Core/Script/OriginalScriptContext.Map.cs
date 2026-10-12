using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

// M.ADV and the WAO_MAP.ADV operations it reaches: the strategic-map helpers, ported for their
// state effects only — the camera (MapCamX/Y, MapTargetX/Y), the scratch variables they leave
// behind (Tmp0021-0030, Tmp0039/0040, CallArg, RetVal from the Y 3 pauses) and the map marker
// list on page 10. Drawing (PC, G, J0, CC, CS, EC) is the MapEvent that MapCallAsync raises
// first; each G counts its CG (CountCgLoad). Found necessary by the first golden capture: the scratch values and RetVal that M.ADV
// op 13 leaves are what FIGT_A returns to its caller. Ported so far: the operations the battle
// reaches; the others (WAO_MAP op 1 drag-scroll, op 5, op 100) come with the parts that use them.
public sealed partial class OriginalScriptContext
{
    /// <summary>
    /// <c>!CallArg : mode! %#M.ADV</c>: a strategic-map call — the <see cref="MapEvent"/> for
    /// the presentation, then <c>M.ADV</c>'s state effects (<see cref="MapAsync"/>).
    /// </summary>
    public async Task MapCallAsync(Sys3Int mode)
    {
        State.CallArg = mode;
        await View.ShowAsync(new MapEvent(mode));
        await MapAsync();
    }

    /// <summary>
    /// <c>!CallArg : mode! %#WAO_MAP.ADV</c> called directly (e.g. op 3 by the new game,
    /// <c>ｽﾀMENU.ADV:283-284</c>): the <see cref="WaoMapEvent"/>, then the operation's state effects.
    /// </summary>
    public async Task WaoMapCallAsync(Sys3Int mode)
    {
        State.CallArg = mode;
        await View.ShowAsync(new WaoMapEvent(mode));
        if (State.CallArg == 1)
            await MapDragAsync();
        else
            MapWaoMap();
    }

    /// <summary>
    /// <c>WAO_MAP.ADV:5-81</c> (op 1, the map loop of <c>COMMAND.ADV:61</c>): while the left button
    /// is held the map moves with the pointer (a redraw and a
    /// <c>Y 3, Ui_ScrollWait</c> per step); after the release it coasts on, the step decaying
    /// to 5/10 then 8/10 of the last until it stops — a new press restarts the drag. Starts from
    /// the anchor <c>COMMAND</c> set (<c>Ui_DragPending</c>) or from a fresh <c>IM</c>, which
    /// returns at once unless the left button is down. The camera here is the view's corner.
    /// Leaves <c>Tmp0021/0022</c> at 0, <c>Tmp0027-0030</c> and the pointer in <c>Ui_Mouse*</c>.
    /// </summary>
    private async Task MapDragAsync()
    {
        var input = Input as ISceneInput
            ?? throw new InvalidOperationException("WAO_MAP op 1 reads the mouse: the context's input must be an ISceneInput.");
        while (true)
        {
            // L_0004f (:6-19)
            if (State.Ui_DragPending != 0)
                State.RetVal = 16;
            if (State.Ui_DragPending == 0)
            {
                var anchor = await input.GetMouseAsync();
                State.RetVal = anchor.Button;
                State.Ui_DragX = anchor.X;
                State.Ui_DragY = anchor.Y;
            }
            State.Ui_DragPending = 0;
            State.Tmp0029 = State.MapCamX;
            State.Tmp0030 = State.MapCamY;
            if (State.RetVal != 16)
                break;
            var shape = await input.SetCursorAsync(6);
            State.Tmp0027 = shape;
            while (true)
            {
                // L_0009d (:20-44): follow the pointer.
                await MapPointerAsync(input);
                if (State.RetVal == 0)
                    break;
                if (State.RetVal != 16)
                {
                    State.MapCamX = State.Tmp0029;          // another button: back where the drag began
                    State.MapCamY = State.Tmp0030;
                    break;
                }
                if (State.Ui_DragX != State.Ui_MouseX || State.Ui_DragY != State.Ui_MouseY)
                {
                    State.MapCamX = State.MapCamX + (State.Ui_DragX - State.Ui_MouseX) - (State.Ui_MouseX - State.Ui_DragX);
                    if (State.MapCamX > 1280)
                        State.MapCamX = 1280;
                    State.MapCamY = State.MapCamY + (State.Ui_DragY - State.Ui_MouseY) - (State.Ui_MouseY - State.Ui_DragY);
                    if (State.MapCamY > 400)
                        State.MapCamY = 400;
                    await MapScrollStepAsync();
                }
            }
            // L_0015b (:45-51): the last movement, for the coasting.
            shape = await input.SetCursorAsync(State.Tmp0027);
            State.RetVal = shape;
            State.Tmp0021 = State.Ui_DragX - State.Ui_MouseX;
            State.Tmp0022 = State.Ui_DragY - State.Ui_MouseY;
            State.Tmp0023 = State.Ui_MouseX - State.Ui_DragX;
            State.Tmp0024 = State.Ui_MouseY - State.Ui_DragY;
            State.Tmp0028 = 3;
            bool pressedAgain = false;
            while (true)
            {
                // L_00185 (:52-77)
                await MapPointerAsync(input);
                if (State.RetVal != 0)
                {
                    pressedAgain = true;
                    break;
                }
                if (State.Tmp0021 + State.Tmp0022 + State.Tmp0023 + State.Tmp0024 == 0)
                    break;
                State.MapCamX = State.MapCamX + State.Tmp0021 - State.Tmp0023;
                if (State.MapCamX > 1280)
                    State.MapCamX = 1280;
                State.MapCamY = State.MapCamY + State.Tmp0022 - State.Tmp0024;
                if (State.MapCamY > 400)
                    State.MapCamY = 400;
                await MapScrollStepAsync();
                State.Tmp0021 = State.Tmp0021 * (8 - State.Tmp0028) / 10;
                State.Tmp0022 = State.Tmp0022 * (8 - State.Tmp0028) / 10;
                State.Tmp0023 = State.Tmp0023 * (8 - State.Tmp0028) / 10;
                State.Tmp0024 = State.Tmp0024 * (8 - State.Tmp0028) / 10;
                State.Tmp0028 = State.Tmp0028 - 10;
            }
            if (!pressedAgain)
                break;
        }
        // L_00247 (:78-81)
        State.Tmp0021 = State.Tmp0029 - State.MapCamX + (State.MapCamX - State.Tmp0029);
        State.Tmp0022 = State.Tmp0030 - State.MapCamY + (State.MapCamY - State.Tmp0030);
    }

    /// <summary><c>IM Ui_MouseX, Ui_MouseY:</c> — the button into <c>RetVal</c>.</summary>
    private async Task MapPointerAsync(ISceneInput input)
    {
        var m = await input.GetMouseAsync();
        State.RetVal = m.Button;
        State.Ui_MouseX = m.X;
        State.Ui_MouseY = m.Y;
    }

    /// <summary>
    /// One scroll step of the drag (<c>WAO_MAP.ADV:39-42, 68-71</c>): the view redrawn at the
    /// camera, <c>Y 3, Ui_ScrollWait</c>, and the anchor moved to the pointer.
    /// </summary>
    private async Task MapScrollStepAsync()
    {
        await View.ShowAsync(new EngineEvent("CC", [State.MapCamX + 1280, State.MapCamY, (Sys3Int)640, (Sys3Int)400, Sys3Int.Zero, Sys3Int.Zero]));
        await PauseAsync(State.Ui_ScrollWait);
        State.Ui_DragX = State.Ui_MouseX;
        State.Ui_DragY = State.Ui_MouseY;
    }

    /// <summary><c>%#M.ADV</c> with the operation in <c>CallArg</c> (<c>M.ADV:1-242</c>).</summary>
    internal async Task MapAsync()
    {
        switch (State.CallArg.Value)
        {
            case 0:
                // :1-19 — redraw the map at the camera; clear the window pictures.
                State.CallArg = 0;
                MapViewCorner();
                CountCgLoad(State.Ui_MapCgTopL);    // :11-12 PC 6: G Ui_MapCgTopL (loaded, not drawn)
                MapWaoMap();                // CallArg 0: blit only
                State.Ui_PortraitL = 0;
                State.Ui_PortraitR = 0;
                State.Ui_CenterCg = 0;
                return;
            case 1:
                await MapScrollToTargetAsync();
                return;
            case 3:
                MapWaoMap();                // :114-117, CallArg 3: redraw the markers
                return;
            case 4:
                // :118-124 — marker CG = the owner flag passed in Tmp0021, at the target.
                State.Tmp0023 = State.Tmp0021 + 10000;
                State.Tmp0021 = State.MapTargetX;
                State.Tmp0022 = State.MapTargetY;
                MapWaoMap();                // CallArg 4: set the marker
                return;
            case 10:
                // :131-171 — blink the target mark, with a sound.
                MapTargetMarkCorner();
                State.CallArg = 0;
                State.RetVal = 109;
                await SoundAsync(0);        // %#PCMPLAY.ADV with CallArg 0 writes nothing else
                await MapBlinkAsync();
                return;
            case 11:
                // :172-176 — EC 0, Y 3, 30.
                await PauseAsync(30);
                return;
            case 12:
                // :177-190 — redraw at the camera without clearing the pictures, then pause.
                State.CallArg = 0;
                MapViewCorner();
                MapWaoMap();
                await PauseAsync(30);
                return;
            case 13:
                // :191-226 — blink the target mark without the sound.
                MapTargetMarkCorner();
                await MapBlinkAsync();
                return;
            default:
                throw new InvalidOperationException($"M.ADV operation {State.CallArg} is not ported.");
        }
    }

    /// <summary><c>M.ADV:3-10</c>: <c>Tmp0021/0022</c> := the view's top-left corner (camera − 320/200, capped).</summary>
    private void MapViewCorner()
    {
        State.Tmp0021 = State.MapCamX - 320;
        State.Tmp0022 = State.MapCamY - 200;
        if (State.Tmp0021 > 1280)
            State.Tmp0021 = 1280;
        if (State.Tmp0022 > 400)
            State.Tmp0022 = 400;
    }

    /// <summary>
    /// <c>M.ADV:20-113</c> (op 1): moves the camera to the target in steps of at most 1/10 of
    /// the distance (at least 1/15), redrawing and pausing 5 ticks per step; then the camera
    /// and the target both become the original (unclamped) target.
    /// </summary>
    private async Task MapScrollToTargetAsync()
    {
        State.Tmp0039 = State.MapTargetX;
        State.Tmp0040 = State.MapTargetY;
        if (State.MapTargetX < 320)
            State.MapTargetX = 320;
        if (State.MapTargetY < 200)
            State.MapTargetY = 200;
        if (State.MapCamX < 320)
            State.MapCamX = 320;
        if (State.MapCamY < 200)
            State.MapCamY = 200;
        if (State.MapTargetX > 1600)
            State.MapTargetX = 1600;
        if (State.MapTargetY > 600)
            State.MapTargetY = 600;
        if (State.MapCamX > 1600)
            State.MapCamX = 1600;
        if (State.MapCamY > 600)
            State.MapCamY = 600;
        State.Tmp0029 = State.MapCamX - State.MapTargetX + (State.MapTargetX - State.MapCamX);
        State.Tmp0030 = State.MapCamY - State.MapTargetY + (State.MapTargetY - State.MapCamY);
        if (State.Tmp0029 + State.Tmp0030 != 0)
        {
            // Step sizes: Tmp0023 horizontal, Tmp0024 vertical (:49-74).
            State.Tmp0021 = State.Tmp0029;
            if (State.Tmp0021 > 9)
            {
                State.Tmp0022 = State.Tmp0021 / 10;
                State.Tmp0023 = State.Tmp0022 > 15 ? State.Tmp0029 / 15 : State.Tmp0029 / State.Tmp0022;
            }
            else
            {
                State.Tmp0023 = 1;
            }
            State.Tmp0021 = State.Tmp0030;
            if (State.Tmp0021 > 19)
            {
                State.Tmp0022 = State.Tmp0021 / 10;
                State.Tmp0024 = State.Tmp0022 > 15 ? State.Tmp0030 / 15 : State.Tmp0030 / State.Tmp0022;
            }
            else
            {
                State.Tmp0024 = 1;
            }

            do
            {
                // L_001e7 (:75-86): horizontal step, or snap when closer than one step.
                State.Tmp0021 = State.MapCamX - State.MapTargetX;
                State.Tmp0022 = State.MapTargetX - State.MapCamX;
                if (State.Tmp0021 * Lt(State.Tmp0021, State.Tmp0023) + State.Tmp0022 * Lt(State.Tmp0022, State.Tmp0023) != 0)
                {
                    State.MapCamX = State.MapTargetX;
                }
                else
                {
                    if (State.Tmp0021 > 0)
                        State.MapCamX = State.MapCamX - State.Tmp0023;
                    if (State.Tmp0022 > 0)
                        State.MapCamX = State.MapCamX + State.Tmp0023;
                }
                // L_0023a (:87-98): vertical.
                State.Tmp0021 = State.MapCamY - State.MapTargetY;
                State.Tmp0022 = State.MapTargetY - State.MapCamY;
                if (State.Tmp0021 * Lt(State.Tmp0021, State.Tmp0024) + State.Tmp0022 * Lt(State.Tmp0022, State.Tmp0024) != 0)
                {
                    State.MapCamY = State.MapTargetY;
                }
                else
                {
                    if (State.Tmp0021 > 0)
                        State.MapCamY = State.MapCamY - State.Tmp0024;
                    if (State.Tmp0022 > 0)
                        State.MapCamY = State.MapCamY + State.Tmp0024;
                }
                // L_0028d (:99-113)
                MapViewCorner();
                State.CallArg = 0;
                MapWaoMap();
                await PauseAsync(5);
            }
            while (State.MapCamX != State.MapTargetX || State.MapCamY != State.MapTargetY);
        }
        // L_002e7 (:114-119)
        State.MapTargetX = State.Tmp0039;
        State.MapTargetY = State.Tmp0040;
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
    }

    /// <summary>
    /// <c>M.ADV:132-150</c> / <c>:192-210</c>: <c>Tmp0021/0022</c> := the camera (clamped to the
    /// view range), <c>Tmp0023/0024</c> := the target (capped to the map), <c>Tmp0025/0026</c> :=
    /// the target's position on screen, less 16.
    /// </summary>
    private void MapTargetMarkCorner()
    {
        State.Tmp0021 = State.MapCamX;
        State.Tmp0022 = State.MapCamY;
        if (State.Tmp0021 < 320)
            State.Tmp0021 = 320;
        if (State.Tmp0022 < 200)
            State.Tmp0022 = 200;
        if (State.Tmp0021 > 1600)
            State.Tmp0021 = 1600;
        if (State.Tmp0022 > 600)
            State.Tmp0022 = 600;
        State.Tmp0023 = State.MapTargetX;
        State.Tmp0024 = State.MapTargetY;
        if (State.Tmp0023 > 1920)
            State.Tmp0023 = 1920;
        if (State.Tmp0024 > 800)
            State.Tmp0024 = 800;
        State.Tmp0025 = (Sys3Int)320 + (State.Tmp0023 - State.Tmp0021) - (State.Tmp0021 - State.Tmp0023) - 16;
        State.Tmp0026 = (Sys3Int)200 + (State.Tmp0024 - State.Tmp0022) - (State.Tmp0022 - State.Tmp0024) - 16;
    }

    /// <summary>
    /// <c>M.ADV:156-169</c> / <c>:212-225</c>: four blinks of the target mark, each two 8-tick
    /// pauses; <c>Tmp0023</c> counts 3 → 0.
    /// </summary>
    private async Task MapBlinkAsync()
    {
        State.Tmp0023 = 3;
        CountCgLoad(814);           // :169-170 / :227-228 J0 640, 0: G 814 (the mark)
        while (true)
        {
            await PauseAsync(8);    // CS: mark drawn
            await PauseAsync(8);    // CC: mark erased
            if (State.Tmp0023 == 0)
                break;
            State.Tmp0023 = State.Tmp0023 - 1;
        }
    }

    /// <summary>
    /// <c>%#WAO_MAP.ADV</c> with the operation in <c>CallArg</c>, for the operations the battle
    /// reaches (<c>WAO_MAP.ADV:1-4</c>, <c>:81-117</c>, <c>:183-196</c>).
    /// </summary>
    private void MapWaoMap()
    {
        switch (State.CallArg.Value)
        {
            case 0:
                return;                     // CC: blit the map view
            case 3:
                // :81-109 — redraw the six map tiles, then every marker; flags above 10000
                // are drawn 18 px higher.
                CountCgLoad(State.Ui_MapCgTopL);
                CountCgLoad(State.Ui_MapCgTopM);
                CountCgLoad(State.Ui_MapCgTopR);
                CountCgLoad(State.Ui_MapCgBotL);
                CountCgLoad(State.Ui_MapCgBotM);
                CountCgLoad(State.Ui_MapCgBotR);
                State.Ui_MarkCur = 0;
                while (State.Ui_MarkCur < State.Ui_MarkCount)
                {
                    var mark = State.Marks[State.Ui_MarkCur];
                    State.Tmp0021 = mark.X + 1280;
                    State.Tmp0022 = mark.Y;
                    State.Tmp0023 = mark.Cg;
                    if (State.Tmp0023 > 10000)
                    {
                        State.Tmp0022 = State.Tmp0022 - 18;
                        State.Tmp0023 = State.Tmp0023 - 10000;
                    }
                    CountCgLoad(State.Tmp0023);     // G Tmp0023, 40
                    State.Ui_MarkCur = State.Ui_MarkCur + 1;
                }
                return;
            case 4:
                // :110-116 — overwrite the marker at (Tmp0021, Tmp0022), or append one.
                MapFindMarker();
                var at = State.Marks[State.Ui_MarkCur];
                at.X = State.Tmp0021;
                at.Y = State.Tmp0022;
                at.Cg = State.Tmp0023;
                State.Ui_MarkCount = State.Ui_MarkCount + State.RetVal;
                return;
            default:
                throw new InvalidOperationException($"WAO_MAP.ADV operation {State.CallArg} is not ported.");
        }
    }

    /// <summary>
    /// <c>L_004f5</c> (<c>WAO_MAP.ADV:183-196</c>): <c>Ui_MarkCur</c> := the marker at
    /// (<c>Tmp0021</c>, <c>Tmp0022</c>) with <c>RetVal</c> 0, or the count with <c>RetVal</c> 1.
    /// </summary>
    private void MapFindMarker()
    {
        State.Ui_MarkCur = 0;
        State.RetVal = 1;
        while (State.Ui_MarkCur < State.Ui_MarkCount)
        {
            var mark = State.Marks[State.Ui_MarkCur];
            if (mark.X == State.Tmp0021 && mark.Y == State.Tmp0022)
            {
                State.RetVal = 0;
                return;
            }
            State.Ui_MarkCur = State.Ui_MarkCur + 1;
        }
    }

    /// <summary>The script's <c>a &lt; b</c> as a value (1 or 0).</summary>
    private static Sys3Int Lt(Sys3Int a, Sys3Int b) => a < b ? (Sys3Int)1 : Sys3Int.Zero;
}
