# Survey — our own interface (parts 1-4)

The owner's request (2026-10-12): an interface of our own next to the original layout, chosen by
the player, on Windows and later on Android with touch (docs/backlog.md, "Android" and "Interface"),
while the game logic stays the original's. This survey covers what the port has today and what
such an interface would need — the screens (§1), the classes of work (§2), input on mouse and
touch (§3) and how a second interface plugs in (§4). **The design itself — what the screens look
like, what comes from Sengoku Rance, what extra information a screen may show — is the owner's and
is not here** (part 5, later). Proposals below are marked as such; nothing in them is decided.

Read on 2026-10-12 by four subagents (drawing, input, the screens run from the player's files, the
architecture and the Godot side). Re-read and confirmed: the map wait's right click and click test
(`CommandPhase.cs:182-236`), the picker loop's hover and press (`PickerScreen.cs:128-168`), the
presenter's routing during a battle (`GamePresenter.cs:220-240`, `BattlePresenter.cs:141-185`), the
proxies' wiring (`godot/Strategy/GameScreen.cs:266-274`), `Ui_FaceOverMap` (read by the map drawing,
`BattlePresenter.cs:516, 533`). The rest is as the subagents read it. **[I]** marks an inference.
Coordinates and sizes are script pixels on the 640×400 screen (the window shows it at 1280×800).

The owner's decisions this rests on (docs/strategy/README.md): **3** — the original layout first, a
Sengoku Rance–style redesign later with the owner, the look need not be pixel-exact; **4** — the UI
is swappable, picked at the start of a game and ideally during play, at a safe point (a key wait or
the command loop) through a view/input proxy, missing meaning derived in the presentation layer or
added as semantic events, never rules in the UI; **9** — screens drawn from the events' meaning and
`GameState`, a UI turns its clicks into the pointer gestures the script reads (`PointerScript`);
**12** — the scenes' drawing runs on the engine surface, everything with a meaning is drawn by hand
over it, "a redesigned UI replaces the layers and may show or hide the stage". The brief: only the
interface is modernised (scope 1); the presentation layer can be replaced without touching the rules.

## 0. In short

- **The rules need no change.** The core talks to a UI through two objects — a view
  (`IScriptView.ShowAsync(ScriptEvent)`) and an input (`IGameInput`) — behind proxies made for
  swapping (§4.1-4.2).
- **What is already semantic:** every `]` menu (the UI returns an index), the whole battle input
  (`FigtSelChoice`, battle menu codes), the scenes' windows, portraits, centre pictures and text
  (`WindowEvent` + `Ui_*` fields + message events), the screens' open/close (map wait, pickers, All
  Data, save screen, battle).
- **What reads the pointer raw:** the strategic map, the four pickers, All Data's waits, and four
  screens run from the player's files — the title `ｽﾀMENU`, Fortune Check `HAPPY_C`, the gallery
  `CG`, the music room `MMODE`. A UI of our own drives these with synthesized gestures
  (`PointerScript`, as `--auto` already does) or — for the four frame screens — a hand port.
- **Touch blockers in the original's input:** the right button is the only way to the map's command
  menu (every command and Turn END), the battle's command popup, out of the save list, out of the
  dungeon's empty menu, into All Data's scale menu; the pickers' details and the battle's target
  check follow the hover; a map click must not move a single pixel between press and release.
  An interface of our own that synthesizes gestures avoids all of them (§3.7).
- **Wiring still missing for a switch:** `GameScreen` does not keep its proxies, and part of what
  the presenter draws comes from the event history, not from `GameState` (§4.2-4.3).

## 1. The screens

### 1.1 How a frame is drawn today

`Kichikuou.Original.Content/Strategy/GamePresenter` composes the 640×400 `Screen` whenever anything changed
(`GamePresenter.Compose.cs:242-273`):

1. **the stage** — the emulated engine surface (3200×800, 8-bit indexed as the engine's; the battle's
   presenter shares it, `GamePresenter.cs:106-108`), where the scripts' own drawing commands run
   (`G`, `CC`, `CE`, `CS`, `CF`, `CM`, `CB`, `CL`, `WV`, `EC`, text after `T x,y`), and where the
   map and the battle are drawn;
2. over its visible view, **the hand-drawn layers**: the portraits and the centre picture, an open
   picker, the window's frame, the window's text — or All Data instead of those four;
3. the menu on top, then the fade.

| Layer | Drawn by | From |
|---|---|---|
| Message window text | `TextBox` (`Strategy/TextBox.cs`), routed by `Print`/`NewLine` (`GamePresenter.cs:356-384`) | `MessageEvent`, `ValueMessageEvent`, `PrintStringEvent`, `LineBreakEvent`, `MessageClearEvent`, `MessageResetEvent`; the rectangle from `WindowEvent` 111-153 or `B3`/`B4` (`:395-426`, `:477-488`); size and colour from `ZM`/`ZC` |
| Window frame (`WAO_SUB`) | `Frame()` (`Compose.cs:338-345`); opaque for the full-screen types 31-39 | `Ui_WinType`, `Ui_WinFrameCg` |
| Portraits left/right, centre picture + 4 overlays | `DrawPictures()` (`Compose.cs:307-326`) at fixed spots | `Ui_PortraitL/R`, `Ui_CenterCg`, `Ui_CenterOverlay1-4`; hidden when a stage drawing covers them whole (`Cover`, `:169-186`) |
| Menus (`]`) | `SelectionBox` (`Screen/SelectionBox.cs:74-88`) via `MenuBox()` (`Compose.cs:226-232`) | no event: the input side opens it (`OpenMenu`/`CloseMenu`, `godot/Strategy/GameScreen.cs:757-777`); position from `B1`/`B2` |
| Pickers | `PickerView.Draw` (`Strategy/PickerView.cs:95-139`): frame, 3×17 name grid, highlight, details picture, bar labels, panels 8/9, item prompt | `PickerScreenEvent`; `Units[].Status`, `Buffer[].Val`, `Items[].Holder`, `Bt_PlayerParty1-4`, `Tmp0026-0029`, `Tmp0033`, `Ui_Item*`, `StoryStage`; panel text by `B4 n` |
| All Data | `DrawData`/`DrawGraph` (`GamePresenter.Data.cs:39-155`) | `DataScreenEvent`; `History[0-15]`, `Ui_MoneyGraph*`, `Ui_DamageGraph*`, `Marks[]` |
| Fades | `Screen.FadeScreen` (`Compose.cs:271`), animated by Godot | `PF`/`PW` engine events (`GamePresenter.cs:545-548`) |

While a battle shows (`BattleShows`: inside a battle file and no picker open, `GamePresenter.cs:128`)
every event but the screen events goes to `BattlePresenter.Show` and no hand-drawn layer is
composed; a picker the battle opens (`FIGT_B`'s `Sﾌﾞﾀｲ`) is drawn by hand over it. While a picker or
All Data is open (`DrawnByHand`, `:441`) the script's own drawing commands and `T` text are dropped:
the hand-drawn screen replaces them.

### 1.2 The events

- **Screen events** (`Original/Script/ScreenEvents.cs`; they draw nothing, they say what is open):
  `MapScreenEvent(Waiting)` (`CommandPhase.cs:185`, closed at `:214, :260, :277`),
  `PickerScreenEvent(Kind, Mode, Open)` (`DeploymentScreen`, `SubordinateScreen`, `HaremScreen`,
  `ItemScreen`), `DataScreenEvent(View)` (`AllDataCommand.cs:34, 102, 260, 272`),
  `SaveScreenEvent(Mode, Open)` (`LoadSaveScreen.cs:82, 96, 152`), `BattleScreenEvent(File, Open)`
  (`StrategyRules.cs:105, 107`, nesting).
- **Script events** (`Engine/Script/ScriptEvents.cs`): messages and line breaks, `MessageClearEvent`
  (after `A` and every menu choice), `MessageResetEvent` (`Y 1`), `PrintStringEvent` (`MP`),
  `KeyWaitEvent` (a bare `IK 0`), `PauseEvent` (`Y 3`), `WindowEvent(Op, CallArg)` (every `WAO_SUB`
  call), `MapEvent`/`WaoMapEvent` (`M.ADV`/`WAO_MAP` calls), `ClearScreenEvent`, sound and music,
  and `EngineEvent(Command, Args)` — any raw engine command.
- **Battle events** (`Original/Combat/BattleEvents.*.cs`): `RoundHeader`, `StrategyScreen`, `UnitInfo`,
  `EnemyInfo`, `FlowError`, `ScoutReport`, `PanelHighlight`, `BattlefieldBackground`, `SlotPanel`,
  `EngagementScene`, `ActionLabels`, `EngagementSlide`, `Counters`, `ShotsFrame`, `ExchangeResults`,
  `WipedOutMarks`. Several carry little and the presenter reads `GameState` while showing them
  (`ExchangeResultsEvent` is empty).

### 1.3 Screen by screen

| Screen | What the player sees | Drawn by | Input it reads | Semantic description today |
|---|---|---|---|---|
| Logo `ﾌﾟﾛﾛｰｸﾞ` (187 lines) | timed slideshow, CGs 1700-1720 | stage (`G`, `CE`, `CF`, `PC`) | cancelable `Y 3` (any input skips) | none |
| Title `ｽﾀMENU` (871) | one picture with the buttons baked in; hover lights a button; right click shows its description; the requests slip; the debug list | stage (`CC` copies from lit sheets, `T`/`X`), description in window 113 | raw `IM`: 11 regions (box table `:805`, hit test `L_00e91` `:692-714`), 92×122 up to 470×100 plus strips 144×30 and 174×20; left launches, right describes | none; unlocked buttons are `Glob_MenuItem[0..10]` (page 4) |
| Fortune Check `HAPPY_C` (1451) | two pages of 33 stamped entries; detail view with portrait and six strings | stage (sheets CG 837/838, stamps from CG 841, `CB` hover, `T`/`X`) | raw `IM`: 33 cells (~150×32) per page + page flip + Back; left acts on what the previous read lit; right anywhere = back | none; stamps from `Glob_Fate[0..65]` |
| Gallery `CG` (3977) | pages of 30 thumbnails, 4 tabs, Exit; the picture prompt; scene replays | stage (sheets CG 844-848, lock tile CG 849, `CB` hover), `WAO_SUB` 131 for the prompt | raw `IM`: 35 regions per page (thumbnails 106×60, tabs 46×20, Exit 86×24), acts on release; prompt: left back, right replays (STR-49) | none (the `_gallery` flag only masks); seen = `Glob_CgSeen[CG]` |
| Music room `MMODE` (560) | 28 track names, Stop, End; title and comment panels; play time | stage (CG 693, `T` text `ZM 14`, `CF` underline) | raw `IM`, arithmetic grid: 30 cells of 108×32; right = Stop; uses `IZ` | `MusicEvent` for the playback only |
| Opening `ｵｰﾌﾟﾆﾝｸﾞ` (1513) | a film synced to the music | stage only (168 `G`, 60 `CC`, 58 `CS`, 60 `CF`, 11 `CM`, 66 `PC`; `SC` and the engine timer) | a final `IK 0` | none; MsgSkip skips it whole |
| Story scenes `G*`/`I*` (136 files, ~423k lines) | windows with text, portraits left/right, centre picture, the map, menus | windows/portraits/pictures by hand from `WAO_SUB` (`ScriptContext.Wao.cs:39-177`); rare raw drawing (617 `G` in 92 files, 27 `CC`, 7 `CS`, 5 `CB`, 1 `CF`) | key waits, 123 `]` menus in 67 files, 115 `IM` that only wait for a release | yes — `WindowEvent`, `Ui_*`, message events, menus |
| Strategic map (`COMMAND`'s wait) | the map with markers, scrolling, a status window | stage, from `GameState` (`BattlePresenter.DrawMapTiles`, `Engagement.cs:318-346`; tiles `Ui_MapCg*`, markers `Marks[]`, camera `MapCamX/Y`); the status window is message text | raw `IM`: left click (no movement), drag-scroll with coasting, right = command menu (§3.2) | `MapScreenEvent(Waiting)`; no "province under the pointer" query (the hit test is in `CommandPhase.cs:254-307`); no status-window event |
| Command menu and the command files' menus (`C*`) | `]` menus; ○/×/△ marks | `SelectionBox` | index; right cancels | the items' texts only: no id, no enabled flag (○/× are part of the text, a × item is its own item that re-opens the menu, `CommandPhase.cs:434-437, 506`), no position |
| Pickers `Sﾌﾞﾀｲ`, `Sﾌﾞｶ`, `Sﾊｰﾚﾑ`, `Sｱｲﾃﾑ` | grid of names, details of the hovered one, bar buttons, panels | hand-drawn (`PickerView`) | raw `IM`: hover moves highlight and details, press acts, right = cancel code (999, 998 in `Sｱｲﾃﾑ`); `Sｱｲﾃﾑ`'s Yes/No menu opens at the pointer | `PickerScreenEvent` + state; geometry in `PickerView.HitTest` |
| All Data | mini-map, four graphs, scale menu | hand-drawn (`GamePresenter.Data.cs`) | menu index; waits raw `IM` (left leaves, right opens the scale menu, both = CMD-25) | `DataScreenEvent` + state |
| Save/Load (`ﾛｰﾄﾞｾｰﾌﾞ`) | window 113 and slot-list menus | generic window + `SelectionBox` | menu indices | `SaveScreenEvent` (emitted, read by nothing); slots are menu texts |
| Dungeon search (`Cﾒｲｷｭｳ`) | `EC 0`, background picture, messages, menus, battles | window + centre picture (`WAO_SUB` 331) + menus | menu indices; the empty menu only a right click leaves (DGN-07) | none; progress in `DgnRun_*` |
| Week loop, nation turns | map animations, windows, fades | stage + layers | key waits | none ("nation X's turn" is not an event) |
| Battle | panels, engagement scenes, counters, popups | stage (`BattlePresenter`, a reproduction of `FIGT*`'s drawing); input screens `FigtSelScreen`/`BattleMenuScreen` draw on the stage too | semantic result; the input screens translate the mouse (hover frames a panel, left attacks or shows info, right opens popups) | battle events + `GameState`; input typed (`FigtSelMenu`, `BattleMenu`) |
| Endings `OVER` (2210) | ending picture, staff roll, Completion Report (66 entries: portrait, text, fortune marker) | stage (`G`, `CS` scroll of CG 850-857, CG 840), report text in window 113 | key waits; roll waits cancelable from the second clear | none; at each entry's key wait the state says which entry (`Glob_FateCur`, `Tmp0001`, `CallArg`) |
| Menu bar (ours) | File, Screen, Option, MsgSkip, Debug | Godot controls (`GameScreen.MenuBar.cs`) | Godot | — |

## 2. Classes of work

- **A — semantic already**: a UI of our own draws it from events and `GameState` and answers with
  indices; nothing in the core changes.
- **B — semantic in part**: it needs a new event, a query or a geometry helper in `Original.Content`/`Original`
  (presentation only, no rule), and its input is given as synthesized gestures.
- **C — the script draws and hit-tests**: keep the original layout scaled (input as pointer), or
  port the file by hand into a semantic form.

Effort: S — a day or less, M — a few days, L — more. **[I]** for every estimate.

| Screen | Class | What a UI of our own needs | Effort |
|---|---|---|---|
| Story scenes (windows, portraits, centre picture, text) | A | restyle the layers once — all 136 files follow; raw full-screen pictures stay on the stage | M (part of the first step) |
| `]` menus | A/B | the index answer is enough; for a good look an event with the items as data (text, the ○/×/△ mark split off, the `]`'s window) instead of `OpenMenu` from the input side | S |
| Battle input | A | our own controls returning `FigtSelChoice` / a menu code (the popup's argument can be 99) | M |
| Battle drawing | A/B | our own battle screen from the battle events + `GameState`; or keep `BattlePresenter` on the stage | M-L (own) / 0 (keep) |
| All Data | A | draw from `DataScreenEvent` + history; its waits by gestures | S-M |
| Pickers | B | draw from `PickerScreenEvent` + state (as `PickerView` does); input `PointerScript.PickerCell/PickerButton`; details from state without hover | M |
| Strategic map | B | draw from `GameState` (tiles, markers, camera) or keep the stage's map; a "province/dungeon at a point" helper from `Prov.MapX/MapY` and the hit radii; input `Click`/`Drag`/`RightClick` gestures | M |
| Status window, save screen, dungeon, nation turns | B | small events or state reads (`SaveScreenEvent` exists, unused; `DgnRun_*`; the status values are in `GameState`) | S each |
| `OVER` | B | a report view keyed on the state at each entry's key wait; picture and roll stay on the stage | S-M |
| Title `ｽﾀMENU` | C | (b) own buttons from `Glob_MenuItem`, clicking the box centres; or (c) hand port (New game and Load are ported already; the debug presets are the bulk) | S-M / M |
| Fortune Check `HAPPY_C` | C | hand port: a read-only viewer over `Glob_Fate`, the 66 entries' texts extracted by script | M |
| Gallery `CG` | C | hand port from its two tables (entry → CG, stub → scene and `Ev_Entry` 300-304); replays run unchanged through `SceneRunner` | M |
| Music room `MMODE` | C | hand port: `SS 1-28` and the name table | S |
| Logo, opening | — | keep (the opening is a film; replacing it is art, not porting) | 0 |

## 3. Input on mouse and touch

### 3.1 How input reaches the logic today

- **`ISceneInput`** (`Engine/Script/SceneInput.cs:11-53`): `SelectAsync(items)` — a `]` menu, answered
  with the item (1-based) or 0 for a cancel; `GetMouseAsync()` — `IM`, raw: buttons 0/16/32/48 and
  the point. **`IScriptInput`** (`ScriptInterfaces.cs:32-54`): `WaitForKeyAsync()` (`IK 0`, `A`; the
  key code goes to `RetVal`), `PauseAsync(ticks, cancelable)` (`Y 3`). **`IBattleInput`**
  (`Combat/BattleInput.cs:13-28`): `SelectCommandAsync(FigtSelMenu)`, `ChooseAsync(BattleMenu)` —
  semantic, the core rejects an answer the menu does not allow.
- **Godot** (`godot/Strategy/PlayerInput.cs`): a polled state as xsystem35's. Left button and Return
  are 16, right button and Space 32, arrows 1/2/4/8, Esc 64, Tab 128. `IM` reads the mouse buttons
  only; `Y 3`, `IK 0` and the menus read buttons and keys. A menu waits until nothing is held,
  cancels on Space alone, chooses on the release of Return alone over an item (press, slide,
  release works); no arrow keys. `IZ` moves the read position (gliding with Option → Mouse
  Movement).
- `project.godot` has no touch settings: Godot's default mouse emulation turns a touch into a left
  button — no right button, no hover without a press.

### 3.2 Per screen

| Screen | Left | Right | Drag | Hover | Keys |
|---|---|---|---|---|---|
| Map wait (`CommandPhase.cs:182-288`, `ScriptContext.Map.cs:48-120`) | click = press + release **with no pixel moved**; hit-tested against provinces, then dungeons | **opens the command menu** (the only way) | any move while held scrolls; coasts after release; another button during a drag snaps back | — | — |
| Command menu (`CommandPhase.cs:487-496`) | chooses | back to the map (+ `IZ`) | — | highlight | Return/Space |
| Pickers (`PickerScreen.cs:99-167`) | **acts on the press** with the code under the pointer | cancel code | — | **moves highlight and the details panel** | — |
| `Sｱｲﾃﾑ` Yes/No (`ItemScreen.cs:385-408`) | menu **opened at the pointer** | cancel | — | — | — |
| All Data (`AllDataCommand.cs:57-83, 281-328`) | leaves a graph | **opens the scale menu**; after a menu cancel, right = Return | — | — | — |
| Dungeon (`DungeonCommand.cs:256-275, 690-692, 796-803`) | menus | the empty menu **only right leaves** | — | — | — |
| Save/Load (`LoadSaveScreen.cs:180-199, 241-249, 324-334`) | menus | after a save the list comes back: **only right leaves** | — | — | — |
| Battle `FIGT_SEL` (`FigtSelScreen.cs:104-253, 272-309`) | enemy: attack; own panel: unit info | **command popup** (modes, Defend, Wait, auto orders, Retreat, Enemy Info while an enemy is hovered); own slot at x < 199: **orders popup**; inside a popup: cancel | — | **frames a panel; colour = valid target** | — |
| Battle menus (`BattleMenuScreen.cs:42-69`) | release chooses | cancel | — | highlight | — |
| Message waits (`ScriptContext.Wao.cs:50-79`) | any | any | — | — | any |
| Title | launches | **shows the description** | — | lights a button | — |
| `HAPPY_C` | acts on what the previous read lit | back | — | **needed** (previous read) | — |
| Gallery | acts on release | at the prompt: **replays** | — | **needed** (previous read) | — |
| Music room | plays | **Stop** (also a cell) | — | underline | — |

### 3.3 The right button

**No left-click way to:** (1) the map's command menu — every command and Turn END
(`CommandPhase.cs:202`); (2) the battle's command popup and the slot orders popup
(`FigtSelScreen.cs:245-252`); (3) out of the save list, and out of the load list opened from a
menu; (4) out of the dungeon's empty menu (`DungeonCommand.cs:802`); (5) All Data's scale menu
(`AllDataCommand.cs:288`); (6) the title's descriptions, the gallery's replay.
**With a left alternative:** every `]` cancel that also has a Return/Don't item, the battle menus'
cancel, the pickers' cancel (the grid's left side, the bar buttons, `Sｱｲﾃﾑ`'s 502 and End), the
music room's Stop.

### 3.4 The hover

1. The pickers' details (portrait, troops, war expense) follow the hover
   (`PickerScreen.cs:135-147`); a press acts at once, so with touch the details cannot be seen
   before picking — a tap shows the details of the entry it picks.
2. In the battle the target check (the frame's colour) shows only on hover, and Enemy Info and the
   popup commands' argument depend on the button hovered at the right press.
3. `HAPPY_C` and the gallery act on what the read before the click lit (`PointerScript.Hover` exists
   for it).
4. The menus' highlight and the title's and music room's underlines are cosmetic.

### 3.5 Keys

No script tests a specific key code: keys count as "any input" (`IK 0`, `A`, a cancelable `Y 3`,
`OVER`'s skip), but for `FIGT_M`, where the key that ended a wait picks flavour lines (BTX-03).
Return and Space act only in `]` menus. Keys never reach `IM`, so the keyboard cannot drive the map,
the pickers, All Data's waits or the title, gallery and music room loops. Ours: F1 MsgSkip, F4 full
screen, Alt+R Restart, Backspace to the title (development).

### 3.6 Target sizes

| Target | Size (script px) | Smaller side, mm on a 6.5" phone in landscape |
|---|---|---|
| Province click box | 23×23 (large provinces 59-71) | 3.9 |
| Dungeon click box | 19×19 | 3.2 |
| Menu item | 18 high, box width + 2 (default 152) | 3.1 |
| Picker cell | 108×18 | 3.1 |
| Picker bar button | 106×76 | 13 |
| Battle panel | 182×84 | 14 |
| Battle popup item | 18 high | 3.1 |
| Gallery thumbnail / tab / Exit | 104×58 / 45×18 / 85×23 | 9.9 / 3.1 / 3.9 |
| Music room cell | 108×32 | 5.4 |

(A 6.5" 20:9 screen is ~68 mm high; the 640×400 screen fitted to that height is ~0.17 mm per script
pixel. A finger needs ~7-9 mm.)

### 3.7 What follows for an interface of our own (proposals)

- **Synthesized gestures remove the touch blockers.** Our widgets know what the player means
  ("Kinkaid (unit 9)", "Oaks (6)", "commands", "back") and queue the exact raw input the script expects
  through `PointerScript` (`Click`, `RightClick`, `Drag`, `PickerCell`, `PickerButton`, `Hover`) —
  a still click (no jitter), a right click from a "Commands"/"Back" button or Android's back
  gesture, a hover before a click where a screen needs it. `--auto` proves the path on every screen
  it plays (`GameScreen.cs:1069-1349`); no rule is decided by the UI: the script still hit-tests the
  point.
- **Details without hover:** our UI can show a picker entry's details (or a battle target's
  validity) from `GameState` directly, then send the click on confirmation — the same information
  the original shows on hover, in another gesture. Whether a screen may show *more* than the
  original is a design question for part 5.
- **Sizes:** our own widgets choose their own sizes; on a phone the original layout's 18-pixel rows
  are too small for a finger, so the original layout on Android would need at least a zoom or larger
  menus **[I]**.
- **The original layout on touch** (if offered at all): a tap as a left click, a long press or an
  on-screen button as the right click, the back gesture as a cancel; hover-only information would
  stay unreachable before a press.

## 4. How a second interface plugs in

### 4.1 What a UI implements

`GameSession` (`Original/Strategy/GameSession.cs:35-45`) takes `GameState`, the random generator, an
`IScriptView`, an `IGameInput`, the scenario and scene sources, an `ISaveStore` and the hooks for
unported files.

- **`IScriptView`** — `Task ShowAsync(ScriptEvent)`; the script waits for the task, so a UI can
  animate inside it (Godot does for fades and the pointer glide).
- **`IGameInput`** = `IBattleInput` + `ISceneInput` (`Strategy/IGameInput.cs:13-15`; decision 7: only
  what drives a whole game implements it, `StrategyRules.Bind` checks it). Besides menus, mouse, key
  waits and pauses it carries the clock (`ZT0-5`), the CD position (`SC`), the MIDI track, the cursor
  shape (`IC`) and the display query.
- The presenters also take an `IGlyphRenderer` and an `IBattleAudio`.

### 4.2 Swapping

`GameInputProxy` and `ViewProxy` (`Original/Strategy/GameInputProxy.cs:13-69`) each have a settable
`Target`; a switch must happen while no request is pending (a pending one stays with the old UI).
The battle checks that its input is the context's own (`Battle.cs:19-20`); the router passes the
proxy itself (`StrategyRules.cs:58`), so a proxy switch passes it. **But** `GameScreen` creates both
proxies inline and keeps no reference, and the session is a local of `RunAsync`
(`godot/Strategy/GameScreen.cs:270`): nothing can switch them today.

### 4.3 State a new UI would not have

Decision 4 wants a UI that takes over mid-game to draw from `GameState`. Part of what
`GamePresenter` draws comes from the event history instead: `MapWaiting`, the open picker, the
battle's depth, the window's text, the open menu (`GamePresenter.cs:221-288`). A UI switched in
mid-game would miss it. **Proposal:** a shared screen-state tracker in `Original.Content`, fed every event
whichever UI is active, that both UIs read (what is open, the window and its text, the menu, the
battle depth).

### 4.4 The Godot side today

- `Main` opens a `GameScreen` on the title or what the switches ask for; `Screens` switches screens.
- `GameScreen._Ready` builds a `TextureRect` (keep aspect, centred) and a `GamePresenter`, then starts
  the game task on the main thread; it yields once a frame (`NextFrameAsync`), and the rules and the
  drawing run between the yields.
- `ShowAsync` passes each event to the presenter, keeps MsgSkip, runs the `--auto` hooks, glides
  the pointer for `IZ`, animates fades (`GameScreen.cs:376-445`).
- `_Process` → `Present`: when `Compose()` reports a change, `Screen.ReadScreen` → `Image` (1280×800
  RGBA) → `ImageTexture.Update` (`:212-250`).
- Input: `_GuiInput` converts to script coordinates and updates `PlayerInput`; the script polls it
  once a frame; the battle's input screens get press/release edges (`:522-595`); the clock and the
  devices at `:914-976`; `--auto` is ~480 lines (`:978-1459`).
- **Cost per changed frame:** the stage is 6400×1600 stored pixels (~41 MB RGBA + ~20 MB palette
  indices, `Surface.cs:29-64`); each changed frame copies the visible 1280×800 with a palette lookup
  per pixel, draws the layers, fades per pixel, copies 4 MB out and uploads 4 MB to the GPU. It
  happens only on change — but on every event shown, every pointer move while a menu or a battle
  input screen is open, and every fade frame. This is what may be slow on a phone (backlog,
  "Android": measure first).

### 4.5 What an interface of our own reuses and replaces

**Reuses unchanged:** the whole core (`GameSession`, `StrategyRules`, the battle, `SceneRunner`,
saves, proxies, `PointerScript`); `ScenarioText` (messages and menu items are text references),
`CgLibrary`/`Palette`, the audio, the glyph renderer, `EngineState`, `MessageSkip`, `EngineTimer`;
the presenter's **stage** path for whatever stays the script's drawing (story pictures, the opening,
the endings' pictures and roll, the frame screens kept as they are) — decision 12 lets a redesign
show or hide it.

**Replaces:** the hand-drawn layers (window, pictures, pickers, menus, All Data); the battle's input
screens (by controls returning `FigtSelChoice` or a code); optionally `BattlePresenter`; the map and
picker interaction (by widgets queuing gestures); `GameScreen`'s view/input implementation and its
texture path, as far as our screens are not the stage.

**Must stay identical in both:** the engine's timing as the scripts see it — `IK 0`'s release wait
and key repeat, `Y 3`, the clock and timer, MsgSkip's stops. **Proposal:** move these out of
`GameScreen` into a shared base both UIs use, so a second UI cannot drift from xsystem35's
semantics.

### 4.6 How to draw it (options)

1. **Godot controls over the stage** — the stage stays a `TextureRect` shown when a scene draws on it
   (or hidden), our screens are Godot `Control`s: native widgets, any font and size, layouts that
   scale from a desktop window to a phone, touch handled by Godot. The text still comes from
   `ScenarioText`; pictures from `CgLibrary` as textures.
2. **Hand-drawn on the 640×400 surface**, as the original layout is today — one pipeline, but fixed
   pixels, the font at the original's size, and no touch-sized layout.

**Recommendation: 1.** It is the natural fit for Android and for "any font we like"; the stage path
remains for what the scripts draw themselves.

### 4.7 Choosing the interface at run time

Per decision 4: a setting (Option menu and `user://settings.cfg`, as the font), applied at a safe
point — a key wait or the command loop — by switching the proxies' targets; Android defaults to our
own interface. The original layout stays the reference and the default on Windows **[I]** (the
owner's call).

### 4.8 Groundwork that does not wait for the design (proposals)

None of these changes a rule or a look; each is small and testable on its own:

1. keep the session and the proxies in fields; a switch at a safe point;
2. the shared screen-state tracker (§4.3);
3. menus as data: an event with the items (text, ○/×/△ split off, window), emitted where `]` asks;
4. the engine-timing base shared by both UIs (§4.5);
5. a map geometry helper in `Original.Content`: the province or dungeon at a point and the point of a province
   (from `Prov.MapX/MapY` and the hit radii), for our map to send exact clicks.

## 5. Found while surveying

- **Scenes inside a battle file.** While a battle shows, every event goes to `BattlePresenter.Show`,
  which has no case for `EngineEvent` (`BattlePresenter.cs:141-185`) and the hand-drawn layers are
  not composed (`GamePresenter.Compose.cs:250`). Scenes the battle files call (`Gｴｸｽ` 1-3, 5, 6 from
  `Battle.Flow.Attack.cs:36-38` and `Battle.Flow.Defence.cs:66, 324, 576`, and the other
  `EventAsync` calls in `Combat`) would lose any raw drawing, and their portraits and centre
  pictures would show only as far as `BattlePresenter.Window` draws them. `Gｴｸｽ` has no raw drawing
  command; whether its `WAO_SUB` pictures show was not checked.
  **Fixed (2026-10-12):** of the ~55 scene calls in `Combat`, 23 reach raw drawing (`G`, the fades,
  `EC` — `死亡捕獲`'s death scenes above all, `Gｼｨﾙ` 28, `Gﾏｼﾞｯｸ` 25, `Iｾﾞｽ` 50); none places text
  (`T`, `B1`-`B4`, `ZC`, `ZS`). Their raw drawing now runs on the stage the battle draws on, after
  the battle's own drawing; their windows and portraits (`Gｴｸｽ`'s included) are
  `BattlePresenter.Window`'s, which draws every `WAO_SUB` op they use (3 and 4 through
  `Ui_FaceOverMap`); the value messages read their computed string variables there too; the debug mask covers their
  full-screen window there too (`GamePresenterTests.A_death_scene_draws_its_pictures_over_the_battle`).
- `SaveScreenEvent` is emitted and read by nothing; `KeyWaitEvent` is ignored outside battles.
- The menus always use the battle's colours and size (16 / 18 / 255 / 22): the scenes' `ZC 2-4`,
  `ZS` and `B1`'s extra arguments are not tracked (visual only).
