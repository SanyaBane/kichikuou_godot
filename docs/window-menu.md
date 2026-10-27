# The window menu bar

Our game screen gets a classic Windows-style menu bar (owner's request, 2026-10-09). The
reference is **xsystem35's** menu bar — xsystem35 is the engine the port follows (golden
harness, input, `docs/golden-tests.md`). This file records how it looks, from the owner's
screenshots of xsystem35 2.14.3 on Windows, so they need not be sent again, and what its
commands do in the source (the checkout `KICHIKUOU_XSYSTEM35_DIR`). The System 3.9 menu at the end is
kept for reference only; it is **not** what we follow.

## xsystem35 (the reference)

### Title bar

`XSystem35 Version 2.14.3: Kichikuou Rance`, with xsystem35's green icon on the left.

### Menu bar

Four top-level items, left to right: **File**, **Screen**, **Option**, **MsgSkip[off]**. No
item shows a keyboard shortcut and there are no separators (the source's Option menu has a
separator and "Sound Settings" after the two items below; 2.14.3 does not show them).

| File | Screen | Option |
|---|---|---|
| Save Screenshot | Window | ✓ Mouse Movement |
| Restart | Fullscreen | Auto Copy Text to Clipboard |
| Exit | Integer Scaling | |

- The Option items are toggles shown with a check mark; **Mouse Movement** is checked at start
  (`menu_init`), Auto Copy is not. Screen's Window / Fullscreen are plain commands, Integer
  Scaling a check toggle.
- **MsgSkip[off]** is a top-level item **without a submenu**: a click toggles message skip and
  the label becomes `MsgSkip[on]` (`menu_setSkipState` rewrites the `[...]` part). It is greyed
  out while skipping is not allowed (a message not seen before, see below). Japanese build:
  `文字送り[off]`.
- In full screen the menu bar is hidden and comes back when the pointer touches the top edge
  (`win_menu_onMouseMotion`).

### What the commands do

Sources: `src/win/resources.rc` (the menu), `src/win/menubar.c` (the handlers), `src/menu.c`.

- **Save Screenshot** — saves the screen to a file (`saveScreenshot`).
- **Restart** — a message box "Restart game?" / "Any unsaved progress will be lost." with
  Restart / Cancel (`confirm_lose_progress`), then `nact_quit(true)`: the main loop
  (`src/xsystem35.c`) runs `sys_reset` and starts the interpreter again in the same process —
  all variables, array pages and strings cleared (`v_reset`), the timers (`cmdz_reset`),
  graphics (`ags_reset`) and music (`mus_reset`) — and the scenario runs again from its first
  line (`ｽﾀｰﾄ.ADV`: the opening and the title menu). **Not** reset: the random generator
  (seeded once at start-up, `sgenrand` in `sys35_init`; a restart continues its sequence) and a
  few engine statics (`IC`'s previous shape, `Y 3, 10000/10001`'s flag, `IK`'s key repeat). The
  save files are untouched.
- **Exit** — the same message box with "Quit game?" / Quit, then the program ends.
- **Window / Fullscreen** — leave or enter full screen (the menu bar hides in full screen).
- **Integer Scaling** — scale the 640×400 screen by whole factors only.
- **Mouse Movement** — the engine may move the pointer itself (`ags.mouse_warp_enabled`, the
  script's pointer-placing commands).
- **Auto Copy Text to Clipboard** — every message text is copied to the clipboard (text hook).
- **MsgSkip** (`src/msgskip.c`, `src/input.c`, `src/selection.c`, `src/cmdi.c`): while on, the
  skippable waits return at once — the message waits (`A`, `IK 0`: `RetVal` 0) and the pauses
  (`Y 3`: 0) — so the script runs on as if nothing was pressed (`KEYWAIT_SKIPPABLE`). With the
  default flags it turns itself off on a click (`STOP_ON_CLICK`), when a selection menu opens
  (`STOP_ON_MENU`, `selection.c:419`; the script can change this flag, `cmdi.c:154-156`) and at a
  message not seen before (`STOP_ON_UNSEEN`, the menu item greyed out there): seen messages are
  kept in a Bloom filter in a file.
- **But the seen messages are kept only when there is such a file**: the web build makes one
  (`<game>.msgskip`, `src/gameresource.c:139-145`, `#ifdef __EMSCRIPTEN__`), the desktop builds
  only from a `MsgSkip <path>` line in a `.gr` game-resource file (`:223`). The owner's
  xsystem35 2.14.3 for Windows runs the game folder without a `.gr`, so `msgskip_onMessage`
  returns at once (`msgskip.c:133`): **it skips every message, seen or not**, stops only on a
  click or a menu, and the item is never greyed out. There is nothing to reset there; with a
  `.gr`, deleting its msgskip file resets it.

## Ours (`godot/Strategy/GameScreen.MenuBar.cs`)

Done 2026-10-09; the owner's decisions: Restart, the random generator and MsgSkip's stops as
xsystem35's; both skip modes, as the owner's desktop xsystem35 (everything) by default and as its
web build (seen only) by an option.

- A light bar above the game screen: **File** (Save Screenshot, Restart, Exit), **Screen**
  (Window, Fullscreen), **Options** (ours: no drop-down, the options window below; **Option** until 2026-10-24, the
  name the entries below keep), **Sound[on]** (ours, below), **MsgSkip[off]** (toggles, `[on]` while
  on, greyed out while not allowed), and ours for development at the end, **Debug** (Battle
  Captures...: asks first, then the battle-capture viewer, which our own title had until `ｽﾀMENU`
  replaced it, 2026-10-11; Portraits..., Effects..., Hide Dimmed Map Names). Not done: Integer Scaling, Auto Copy Text to Clipboard
  (no text hook), the bar hiding in full screen.
- **Option** (ours, owner's request 2026-10-21; docs/ui/options.md, mock-up D): the options window, `OptionsView`, over
  the game, which waits while it is open — a paper sheet in a lacquer frame with the tabs **Messages** (text speed with
  the game's 121 typing as its preview, Skip unseen text, MsgSkip speeds up fades, Forget them…, Mouse movement),
  **Display** (Font, Palette), **Battle** (our battle screen's Exchange effects and twelve timings, the order strip as
  their preview), **Game** (the game folder). Each change is kept at once; Defaults resets the tab shown; Close, Esc or
  a right click shut it; Forget and the folder's Change… ask first in a box over the sheet. Until 2026-10-21 these were
  the Option drop-down's items, with Text Speed… and Battle Screen… as dialogs of their own; the entries below keep
  their history. `--options [--shots DIR] [--quit]` opens it over the title and, in a window, dumps each tab
  (development).
- **Sound[on] / Sound[off]** (ours, owner's request 2026-10-24): a toggle after Options, the game's music and sound
  effects heard or not, saved in `user://settings.cfg`, `[audio] sound` (on by default; `SoundChoice`, applied at the
  launch by `Main`). Off mutes the Master bus: the players go on unheard, so the music's position `SC` reads and every
  wait are as with the sound on.
- **Option → Messages → Mouse movement** (checked by default; 2026-10-10): the script's
  `IZ x, y` (`COMMAND.ADV:618` and the command files put the pointer back where it was before a
  menu) glides the system pointer there as `ags_setCursorLocation` does — seven points on its
  ease-out curve 7 ms apart, then the target (`Content/Screen/PointerGlide`); unchecked, only the
  position the script reads moves and the script waits 50 ms. `--auto` never moves the pointer.
  Kept for later launches since 2026-10-27 (owner; for the launch before): `[input] mouse_movement`
  in `settings.cfg`, `MouseMovementChoice`. The touch mode does not offer it and has it off
  (docs/ui/options.md, "Mouse movement").
- **Debug → Hide Dimmed Map Names** (ours, owner's request 2026-10-16; in Option until 2026-10-21, when
  the owner moved it to Debug and made it on by default; checkable, saved in `user://settings.cfg`,
  `[display] hide_dimmed_map_names`): our map's plates
  (docs/ui/command-screen.md, "The map's markers") leave out the dimmed ones — the provinces neither
  ours nor next to ours and the dungeons not ours (`MapNamesChoice`). A click on such a town still
  reaches the script as the original's. A change shows at once.
- **Option → Display → Font: MS Gothic / BIZ UDGothic** (ours, owner's requests 2026-10-11; radio buttons, each with
  a sample; saved in `user://settings.cfg`, `[display] font`): the font
  of the game's text. MS
  Gothic is the original's (docs/assets.md §6), read from the Windows fonts and greyed out where
  it is not installed; BIZ UDGothic (Morisawa, SIL OFL 1.1) is bundled in `godot/fonts` with its
  license. By default BIZ UDGothic (owner, 2026-10-21; before, MS Gothic where installed). Every character takes a fixed
  cell, so the layout is the same in both. A change shows at once in the message windows, menus
  and pickers; text the script printed on the stage keeps its font until drawn again.
  `--font ms-gothic|biz-ud-gothic` picks one for the launch without saving it (development).
  The deployment table's text uses the same font, regular.
- **Option → Interface → Original / New** (ours, owner's decision 25, 2026-10-12; removed 2026-10-21):
  New showed our own screens over the original layout where there is one, Original the original's.
  Since 2026-10-21 (owner) the player always has ours: the submenu is gone and `[display] interface`
  in `user://settings.cfg` is no longer read (`InterfaceChoice`). `--interface original` shows the
  original's screens for one launch (development checks).
- **Option → Display → Palette: Lacquer / Steel** (ours, owner, 2026-10-24; swatches, Lacquer by default): the colours of
  all our screens (`UiPalette`, by the screens' parts, `UiPalettes`; docs/ui/options.md, "The palette"). Saved as
  `[display] ui_palette` in `user://settings.cfg`. A change recolours an open screen at once. `--palette <key>` picks it
  for the launch without saving it (development). Before 2026-10-24 the palettes (Navy / Gold / Steel / Parchment /
  Crimson, 2026-10-12) coloured the battle screen and the map's name plates only.
- **Option → Game → Change…** (ours, owner's request 2026-10-21): after the window's question ("Any unsaved progress
  will be lost"), the import screen (docs/import.md, step 8) with the saved folder in its field and "← Back to the
  title" (or Esc) under it; an import of another folder keeps it (`game_dir`) and opens a fresh title, the way back
  leaves the open import as it was. Greyed out while the developer's extract is the data (`GameData.UsesExtract`:
  a folder would change nothing); `--import` turns it on.
- Hot keys: **F1** MsgSkip and **F4** full screen (on release, as xsystem35's `event.c`), and
  **Alt+R** Restart (ours, after System 3.9's); **Ctrl** held skips messages (ours, see MsgSkip). Backspace still returns to the title at once,
  without asking (development).
- **Save Screenshot** saves the screen as shown (1280×800, event CGs not masked) as
  `user://screenshots/kichikuou-<date>-<time>.png` and names the file on screen.
- **Restart / Exit** ask as xsystem35 does ("Restart game?" / "Quit game?", "Any unsaved progress
  will be lost."); the game waits while the box is open. Restart returns to the title, a fresh
  boot as xsystem35's (`SET`, the logo and `ｽﾀMENU` from the player's files, decision 19; until
  2026-10-11 our own title, decision 10); the next game starts on a fresh
  `GameState`. The random generator lives for the whole launch (`EngineState`, seeded once with
  `--seed N` or the tick count) and so does the message skip.
- **MsgSkip**: `Engine/Script/MessageSkip` (the state and the seen set) and `GameScreen`'s waits.
  The skippable waits — `IK 0` / `A` and `Y 3` — return 0 after one frame while it is on; input
  held then turns it off and is returned (`STOP_ON_CLICK`); a `]` menu (`SelectAsync`, the
  battle's `ChooseAsync`, our enemy-attack screen over `FIGT_B`'s menu included) turns it off (`STOP_ON_MENU`); the
  menus our interface answers unseen (our panel's Load/Save and Construction rows, a Subordinates order) do not, and
  neither do the map's, the pickers' and `FIGT_SEL`'s pointer loops. **It stops at a battle's end** (ours, owner's
  request 2026-10-26): when the fight on the field is over — `FIGT.ADV` returns (`L_00dfd`, `:591`), the battle's
  `FightEndEvent`, in every battle: an attack, a defence, the dungeon, `Iｶﾐ`'s garrisons — the switch turns off, so the
  results, `死亡捕獲`'s captures and deaths and the scenes after a battle are never skipped (Ctrl held still skips them);
  every `MessageEvent` /
  `ValueMessageEvent` marks its `ScriptRef` seen. **Option → Messages → Skip unseen text** (checked by
  default, `MSGSKIP_SKIP_UNSEEN`, and by the tab's Defaults — which had unchecked it until 2026-10-26; for the launch,
  not saved): unseen messages are skipped too, as
  the owner's xsystem35 does; unchecked, an unseen message turns the skip off and greys it out
  (`STOP_ON_UNSEEN`) until a seen one, as the web build does. **Option → Messages → MsgSkip speeds up
  fades** (ours, owner's request 2026-10-10; checked by default, for the launch, not saved):
  while MsgSkip is on, a `PF`/`PW` fade lasts 150 ms at most (`MessageSkip.FadeDuration`);
  unchecked, fades keep xsystem35's length while skipping too (`ags_runEffect` waits without
  `KEYWAIT_SKIPPABLE`; `rate * 16 * 1000 / 60` ms). **MsgSkip skips the opening whole** (ours,
  owner's requests 2026-10-11): inside `ｵｰﾌﾟﾆﾝｸﾞ`, between the prologue and the rebellion
  (`OriginalVariant.WholeScenes`; `SceneRunner.RunningScenes` says which scenes run), the skippable
  waits and the fades take no time and nothing is shown — a frame passes only every 100 ms so the
  window stays responsive. Its stage commands (`G`, `CC`, `CE`, `CS`, `CF`, `CM`, `CB`, `CL`) are
  dropped: drawing its 3,400 of them on the emulated surface alone takes ~7 s — and so are the jumps
  `J0`/`J1` that place the next `G` (`GamePresenter.IsSkippedStageCommand`): its last `J0 0, 400`
  (`:1407`), kept while its `G` was dropped, put the rebellion's first picture (the wedding, `Iﾊﾝﾗﾝ.ADV:1146`)
  below the screen and its palette over the map (2026-10-21); the stage is cleared
  (`EC 0`) when the skip ends, before anything else is drawn. It goes in about a tenth of a second
  (it may first wait up to ~1/3 s for its music, `SC`); turned on mid-way, from there on. The
  rebellion `Iﾊﾝﾗﾝ` comes next with nothing shown in between: from its first scene (Northern Leazas
  City on the map, `:63-77`) MsgSkip fast-forwards as everywhere. Turned off mid-way (F1), the rest
  of the opening is drawn on the cleared stage, so pictures it drew before are missing until it
  draws new ones. The script runs every statement, so the state is as when the opening is watched
  (it has no menu). It paces its pictures on the engine timer
  (`ZT2`-`ZT5`, the only scene that reads it); while MsgSkip is on, the timer
  (`Engine/Script/EngineTimer`) moves one 1/100 s tick per read instead of following the clock, which
  ends each of its waits exactly on schedule, so it loads every CG a watched opening does
  (`Glob_CgSeen`; a jump of the timer would trip its `RetVal > 6900` cuts and lose some);
  `OpeningSkipTests`. xsystem35's MsgSkip fast-forwards every scene alike and leaves the timer
  alone. **Option → Messages → Forget
  them…** (asks first) makes every message unseen again. The seen messages are one
  `File:Line` per line in `msgskip.txt` next to the save slots (`%APPDATA%\Godot\app_userdata\Kichikuou\saves`,
  `saves-auto` for `--auto`; deleting it with the game closed resets them too), exact where
  xsystem35 uses a Bloom filter. `--msgskip` starts a game with it on (development). **Ctrl held**
  (ours, owner's request 2026-10-18; xsystem35 has no such key) skips as MsgSkip does while it is
  held (`MessageSkip.Held`, polled once a frame while the window has the focus), the switch and its
  label untouched: input does not stop it, a `]` menu waits for its choice and the skip goes on
  after it while Ctrl is still held; with Skip Unseen Text unchecked, an unseen message stops it
  until a seen one, as it stops MsgSkip.

## System 3.9 (the original; reference only)

From the owner's screenshots of the English release's `System39.exe`. Title bar
`Kichikuou Rance - System3.9 Ver.5.49`. Top level: **File**, **Options**, **Help**,
**Skip Text [off]** (no submenu; a click toggles the label to `Skip Text [on]`). `———` is a
separator, `›` a submenu.

- **File**: Full Screen (Alt+Enter); Take Screenshot... (Alt+P); ———; Restart System3.9...
  (Alt+R); ———; Close.
- **Options**: Mouse movement [on]; Use Joypad[off]; Joypad Settings...; ———; Full screen
  settings...; ———; Window Size ›; ———; Sound settings... (Alt+S); CD Playback device ›; MIDI
  Playback device ›; ———; Startup settings...
- **Help**: Visit AliceSoft's Website ›; ———; Game version...; About system 3.9...

What System39.exe's Restart and Skip Text do exactly is not known (closed source).
