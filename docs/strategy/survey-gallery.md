# Survey — the gallery and the music room (`CG`, `MMODE`)

Survey behind milestone 7 (owner's decision 20 in [README.md](README.md)): the two title buttons
decision 19 put off — the CG gallery (`CG.ADV`, button 7) and the music room (`MMODE.ADV`, button 1),
both unlocked by a clear and called only from the title (`ｽﾀMENU.ADV:577-581`, `:272-274`; the dead
`TEST.ADV:94` calls `CG` too). Source: `scenario_named\` (assignments read `!Var : n!`, `\` is ≠). Read
on 2026-10-11 by three subagents (the gallery; its scene replays, every one traced; the music room
and the port's side); the load-bearing lines were re-read and confirmed: `CG.ADV:301-307, 829-846,
2322-2349`, `Gｴﾚﾅ.ADV:1-14, 1155-1163`, `Iﾄｳｿﾞｸ.ADV:1834-1843`, `Gﾏﾘｽ.ADV:2160-2166`, `MMODE.ADV:11-31,
80-98`, `SET.ADV:284-297`, xsystem35 `cg.c:262-277`, `cmdz.c:480-485`, `cmdg.c:38, 53`, `cmdf.c:77-92`.
**[I]** marks an inference.

## 1. Where the port stands

| File | Lines | In the port today |
|---|---|---|
| `CG.ADV` | 3977 | Not a scene file (`SceneRunner.IsSceneFile`): `%#CG.ADV` goes to the unported files' hooks (Godot: a notice; the title is drawn again, `TitleTests`). |
| `MMODE.ADV` | 560 | The same. |

Both read as programs under the strict parser and every text resolves (checked 2026-10-11 by adding
them to the scene sweep). `SceneRunner` runs every statement they use but two gaps (§4): the CG load
counter `ZG`, which the gallery is built on, is not modelled; `F1` past the end of its string table
throws, which the music room reaches.

## 2. `CG.ADV` — the gallery

### 2.1 Shape

No `Ev_Entry` dispatch: the file runs from its top. `:1-33` zero `Tmp0001-0019` (twice) and set page 1;
`SS 0` (`:34`, the gallery has no music); five 640×400 page sheets go off screen — page 1 CG 844 at
(1920, 400), 2 CG 845 at (640, 400), 3 CG 846 at (1280, 0), 4 CG 847 at (1280, 400), 5 CG 848 at
(1920, 0) — and the 104×58 lock tile CG 849 at (0, 400) (`:35-46`); `\L_03fd9` stamps the lock on every
entry not yet seen, once (`:47`, `:3002-3046`); `CC 1920, 400, 640, 400, 0, 0` shows page 1 (`:48`).
The loop `L_0010a` (`:49-208`) polls `IM`, hit-tests and keeps the hover (`L_00765`-`L_007c4`, a saved
rectangle at (0, 400) and a `CB` outline in colour 65); a release (`L_00884`, `:249`) dispatches: a
thumbnail to the page's dispatcher (`:359`, `:453`, `:547`, `:641`, `:735`) and one of 150 stubs
(`:829-2321`), a tab to `L_00965`-`L_00989`, Exit to `L_00992`, which waits for the release, sets
`Ev_Entry := 0` and returns (`:301-307`); the title then plays `SS 27` (`ｽﾀMENU.ADV:580`).

### 2.2 The model: 150 pictures and the engine's load counter

Five pages of 6 × 5 thumbnails (104×58 at x 3, 109, 215, 321, 427, 533 and y 40, 112, 184, 256, 328),
baked into the sheets; each entry is a CG number, looked up into `CallArg` (`L_040b7`, `:3047-3667`):
page 1 CGs 101-130, page 2 131-160, page 3 161-190, page 4 191-193 and 195-221 (no CG 194), page 5
222-240, 299, 300, 500-502, 504-506, 516, 517 and 862.

An entry is shown when `Glob_CgSeen[CallArg] ≠ 0` (`!Glob_CgCur : CallArg!` then `{Glob_CgSeen = 0:`,
`:234-235`, `:281-282`, `:3040-3041`): locked entries keep the lock tile, get no highlight and ignore
clicks. `Glob_CgSeen` is not written by the scenario but by the engine: `SET.ADV:297` `ZG Glob_CgSeen:`
binds it as the CG load counter (`cmdz.c:480-485`), and every CG loaded from the archive adds 1 to its
record — `cg_load(num - 1)` (`cmdg.c:38, 53`) → `loader(no)` → `(*(cg_loadCountVar + no + 1))++`
(`cg.c:276-277`), i.e. record `num` of the page-4 array at offset 80 (`SET.ADV:291`). Only a load from
file counts: a CG still in xsystem35's cache does not (`cg.c:268-271`) [I for the original engine].
The only reader is the gallery (and `ｽﾀMENU.ADV:84-85`, which marks CG 240 seen on every title pass);
it only tests ≠ 0. Like the fates, the counts live on page 4 and reach the slots through `ﾛｰﾄﾞｾｰﾌﾞ` 11
(STR-31).

### 2.3 The picture and the replay prompt (`L_033ac`, `:2322-2346`)

`CF` black over both halves, `J0 0, 400; G 806` (the backdrop), `J0 0, 400; G CallArg` (the event
picture, 640×294), `CC 0, 400, 640, 400, 0, 0` onto the screen, then `WAO_SUB` 131 (the full-screen
window: frame CG 815, `Ui_WinType := 31`) and "Right click (cancel key) to replay the scene." The
prompt waits for a button and its release: a left click (16) returns to the page (`{RetVal = 16: \0:}`
in each stub, `:833-835`) — without closing the window; a right click (or both buttons) plays the
scene; after it `WAO_SUB` 0 closes the window (`\L_03449`), and the page is drawn again (`L_00a37`:
`G 841`, `SS 0`, the page's sheet onto the screen).

### 2.4 The replays (EVT-07)

- 141 stubs call a scene file (82 files: 77 `G*`, 5 `I*`), each `{Ev_Entry := 300-304, CallArg := the
  CG, \L_033ac, %#file}`: 82 at entry 300, 41 at 301, 14 at 302, 3 at 303, 1 at 304; no pair twice. Only
  `Gﾄｳｹﾞﾝ`'s stub sets anything else, `MS 1, "Rance"` (`:2033-2034`).
- 8 stubs replay text kept in `CG.ADV` itself (CGs 133, 212, 213, 235-238, 240; 237's and 240's are only
  "............"), through `WAO_SUB` 1; CG 862's shows the picture only (`:2313-2321`).
- Every replay was traced (both branches of every condition not on `Ev_Entry`, subroutines followed;
  the tracer does catch writes on story entries): each pair has its own `{Ev_Entry = n: @label}` at the
  file's top, the label sits inside the scene past its set-up (`Gｴﾚﾅ`: `F_Elena_State := 8` at `:31`
  is skipped), and the scene ends at `{Ev_Entry = n: %0:}` with the same n, before the code after it
  (`Gｴﾚﾅ.ADV:1157-1159` before its `%#M.ADV`; `Iﾄｳｿﾞｸ.ADV:1836-1839` before `StoryStage := 99` and
  `F_Maris_RebelTalk := 1`). 140 replays write only scratch and window state; `Gﾏﾘｽ` 300 sets the
  cursor `Unit_Cur := 2` to choose a line by Sill (unit 2)'s status (`Gﾏﾘｽ.ADV:2163-2164`). None calls a
  file but `WAO_SUB`, opens a menu, fights, jumps (`&#`) or falls into story code; `Ev_Entry ≥ 200` is
  set nowhere else.
- So **EVT-07 does not hold**: a replay changes no story state, and the gallery leaves only `Ev_Entry =
  0`, cursors (`Unit_Cur`, `Glob_CgCur`), string 1 and window state for a game started next. The
  title-to-game carry-over EVT-07 suspected is real elsewhere: `Gｻｲﾄｳ`'s `F_Saitou_State := 1` (STR-36).

### 2.5 Inputs and writes

`IM` only (left 16, right 32; no `IK`, no menu, no keys in the file; `WAO_SUB` 1 waits for a key in the
inline texts). On the pages only a left click acts, on what the read before it lit (the HAPPY_C
pattern, STR-39's kind); a right click does nothing. Hit boxes (strict): thumbnail columns x 3-108,
109-214, 215-320, 321-426, 427-532, 535-636, rows y 40-98, 112-170, 184-242, 256-314, 328-386; tabs
1-5 at x 36-81, 91-136, 146-191, 201-246, 256-301, y 5-23 (the current page's not tested); Exit x
553-638, y 1-24. Writes: `Glob_CgCur` (a cursor), `Ev_Entry`, `CallArg`, `Ui_WaoOp` (ops 0, 1, 2, 131
live), string 1, `RetVal`; no `ﾛｰﾄﾞｾｰﾌﾞ` call — and every `G` it runs counts a load (§2.2).

### 2.6 Commands

`ZH` 162, `R` 90, `CC` 10, `G` 9, `J0` 9, `IM` 5, `CB` 5, `SS` 2 (`SS 0`), `CF` 2, `MS` 1; 141 `%#`
scene calls, 23 `%#WAO_SUB`, one `%0`. All are run by `SceneRunner` and drawn by `GamePresenter`.

### 2.7 Quirk candidates

| # | What | Lines | Kind |
|---|---|---|---|
| G1 | A left click at the replay prompt returns without closing the window (`WAO_SUB` 0 is only after a replay): `Ui_WinType` stays 31, back to the title [I] | `:833-835`, `:2344`, `:2347-2349` | bug |
| G2 | `WAO_SUB` 131's strip save (`WAO_SUB.ADV:235`, rows 300-403 to x 640-1279) overwrites the top rows of page 2's sheet at (640, 400): after a prompt page 2's top edge can show the strip [I] | `:2330`; `:37-38` | bug, screen only |
| G3 | The locks are stamped once, at entry: a CG loaded first during a replay becomes clickable but keeps its lock tile until the gallery is opened again [I] | `:47`, `:3002-3046` | bug, screen only |
| G4 | Column 6 starts at x 535, not 533: x 533-534 select nothing | `:74`, `:98`, `:122`, `:146`, `:170` | bug |
| G5 | The prompt inverts the buttons: left goes back, right (or both) replays; the pages ignore a right click | `:2333-2346`, `:51` | odd design |
| G6 | String 1 is set only for `Gﾄｳｹﾞﾝ`; `G1` and `Gﾊﾟﾊﾟｲｱ` replays print `X 1` with what it holds [I] | `:2033-2034` | bug [I] |
| G7 | Eight entries replay text kept in `CG.ADV` (two only "............"), CG 862 none; CG 240 is always unlocked (`ｽﾀMENU.ADV:84-85`) | `:1149-1157`, `:2313-2321`, `:2350-3001` | odd design |
| G8 | `Gﾏﾘｽ` 300's line depends on Sill (unit 2)'s status as the title left it (the boot's, or the last game's) [I] | `Gﾏﾘｽ.ADV:2163-2164` | odd design |
| G9 (STR-43) | "Seen" is the engine's load counter: only loads from file count (not cache hits), 16 bits wrapping; the gallery tests ≠ 0 | `SET.ADV:297`; `cg.c:268-277` | engine artefact |
| G10 | Dead: the error prints `:329-330`, `:356-357`, `:450`, `:544`, `:638`, `:732`, `:826` (their fallbacks jump to wrong pages), `:3898-3899`; the `WAO_SUB` wrappers `:3905-3952`, `:3957-3965`; `G 841` at `:334` [I]; `:1-19` repeated by `:20-33`; `Tmp0007-0010`, `Tmp0015-0019` never read | — | dead code |

## 3. `MMODE.ADV` — the music room

### 3.1 Shape

`US` saves `RetVal`, `CallArg`, `Tmp0001-0040` (`:1-4`; `UG` restores them at the exit, `:34-37`); `SS 0`
(`:5`); the screen (`L_000f5`, `:39-68`: the title saved at (0, 400), `G 693`, a clean copy at (640,
400), the 28 track names in a 3 × 10 grid at `T 328 + 108·col, 25 + 32·row`, `ZM 14`, "Stop" and
"End"); the loop `L_00064` (`:11-31`) gets an index from `L_00509` (`:208-259`: `IZ`, `IM` polls, the
time display refreshed every pass from `SC`, the hover underline `CF … 240`): 0-27 plays `SS index + 1`
(`:313`) and marks it (colour 255, its title and comment panels), 30 Stop (`:17-27`), 32 End. The exit
(`:32-38`): `PF1`, the saved title back, `PF0`, then `SS 0` (`:77-79`), the `UG`s and `%0` — the title
does not play its music again (STR-37).

### 3.2 Tracks, inputs, writes

`SS 1-28`, every MP3 the game has (index 26 is the title's 27, 22 the ending's 23); no `SL`, so a
track loops. `SC` is polled for the time display (`［－－］－－：－－：－－` while none plays). `IM`
only: the grid x 328-652, y 18-370 in 108 × 32 cells; a right click is Stop; every valid click plays
sound 108 (`PCMPLAY`). Nothing outlives the file: `UG` restores the scratch, no `Glob_*`, no `Ui_*`
(string 1 holds the last name read; the title sets it again).

### 3.3 Commands

`ZH` 12, `CC` 10, `ZC` 9, `T` 8, `ZM` 6, `X` 6, `US` 4, `UG` 4, `SS` 4, `H` 4, `F1` 4, `PF0`/`PF1` 2, `MS` 2,
`IM` 2, `CF` 2, `SC`, `R`, `IZ`, `G`, `F3` 1 each; a data area of 224 strings and a word table. All are
run by `SceneRunner` (`F1` with a gap, §4.2).

### 3.4 Quirk candidates

| # | What | Lines | Kind |
|---|---|---|---|
| R1 | Stop with nothing playing (a right click, or Stop twice: `:25` sets `Tmp0039 := 999`) draws "the track 999" plain: `F1, 1, 999` skips 999 strings of a 224-string table — xsystem35 reads the scenario's bytes after it (`cmdf.c:85-90`) — and prints them at (328, 10681), off the 800-row surface [I: nothing visible] | `:17-22`, `:80-98` | bug |
| R2 | Holding the button on Stop repeats Stop and sound 108 on every poll (no release wait on that path) | `:17-27`, `:253-257` | bug |
| R3 | The ending's and "Brutal"'s titles are flagged two-line but their second lines are blank, so they sit 8 dots high [I: the translation] | `:407`, `:409`, `:560` | odd design |
| R4 | The time display sets no colour of its own: it takes the last `ZC` (255 or 15) | `:260-284` | bug, screen only |
| R5 | Dead: the panels' colour-15 branches (`:152-154`, `:165-167`, `:189-191`), `Tmp0039 := RetVal` twice (`:295`, `:297`), a stray `R` (`:62`) | — | dead code |

## 4. The port's side

### 4.1 `ZG` — the load counter

The gallery needs `Glob_CgSeen` counted, and the port does not count it (`GameSetup.Set.cs:205`; the
golden comparison ignores the variable, `Golden/MemoryComparison.cs:46-48`). Without it every entry
but CG 240 stays locked. The engine counts a CG wherever the script loads one: the scenes' and the
frame's `G` (`SceneRunner`), the window helper's pictures (`WAO_SUB` — portraits, the centre picture
and its overlays, the frames; ported in `Script/ScriptContext.Wao.cs`, drawn by the presenter from
`GameState`), the map, the battle and the hand-ported command files and pickers. Which of these paths
draw the 150 gallery CGs is not traced [I: the scenes' window pictures above all]. The exact counts
cannot be the original's (they depend on xsystem35's cache, G9); only ≠ 0 is read.

### 4.2 `F1` past its table

`ReadDataString` throws ("string … is outside the string table", `ScriptContext.Statements.cs:49-50`)
where xsystem35 reads on into the scenario's bytes (R1). The decompile does not hold those bytes; the
string is drawn off the surface. Each of the music room's reads sets the table first (`#L_007b3, 1`,
`:88`), so the overrun does not carry to the next read.

### 4.3 Inputs, drawing, the mask

- Inputs: `IM`, `IZ` (Godot answers it, `godot/Strategy/GameScreen.cs:385-386`), `SC`
  (`ISceneInput.GetCdPositionAsync`; the tests' `EndInput` throws on it today), key waits (`WAO_SUB` 1).
- Drawing: every command has a case in `GamePresenter.Engine` (`CC`, `CB`, `CF`, `G`, `J0`, `PF0/1`, `T`,
  `ZC`, `ZM`); `T` at x 640 works (the stage writer does not clip). Pages 3-5 sit at x ≥ 1280, the
  map's tile area: `Drawn` marks it foreign so the map is not drawn over them, but a replayed scene
  that dirties the tiles (`WAO_MAP` 3) would let the next page copy repaint the map over them [I: the
  original would lose those sheets too] — to check when the gallery runs.
- `MaskEventCgs`: the full picture is shown and then window 131 opens (`Ui_WinType` 31), so the
  existing rule masks the prompt and the replays' full-screen windows — but the thumbnails (the sheets
  844-848, reduced event pictures [I: inferred from the layout; not viewed]) are shown with no window.
  A new rule is needed: mask the stage from the gallery's sheets (`G 844`-`848`) until the title's
  background (`G 301`), which covers the pages, the prompt and every replay.

## 5. Open questions — for the owner

1. **`ZG`**: (a) count every CG the port draws on every path the script loads one (scenes and frame,
   the window helper's pictures, the map, the battle, the ported command files) — the engine's model,
   with counts that cannot match xsystem35's cache (G9); (b) count only the scripts' `G` and the window
   helper's pictures, the paths the gallery's pictures are drawn on [I: to confirm]; (c) no counter:
   the gallery shows everything. The agent recommends (a), the faithful one; (b) is smaller if the
   trace confirms it.
2. **`F1` past its table** (R1): read an empty string (the agent's recommendation: nothing of it is
   visible in the original either), or something else.
3. EVT-07 is withdrawn by §2.4 (no question; in the docs step).

Answered by decision 21 (2026-10-11): 1 (a), every path; 2 an empty string.

## 6. Proposed order (after the owner's answers)

0. This survey ✓.
1. The `ZG` counter per question 1: tests that a scene's `G` and a window picture count, that the
   counts reach page 4 and the slots (`ﾛｰﾄﾞｾｰﾌﾞ` 11).
2. `CG` from the player's files (`IsSceneFile`), the gallery mask; tests: entries locked and unlocked
   by `Glob_CgSeen`, the tabs, a picture's prompt (left back with G1, right replays a scene from its
   entry and comes back), the exit with `Ev_Entry` 0 and `SS 27`; the presenter's.
3. `MMODE` from the player's files, `F1` per question 2, `SC` in the tests' input; tests: a track
   played, Stop, Stop with nothing (R1), End and the silent title (STR-37); the presenter's.
4. Docs: G1-G10 and R1-R5 registered, EVT-07 withdrawn, the README and CLAUDE.md.
