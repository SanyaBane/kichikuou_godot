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
  (Window, Fullscreen), **Option** (ours, see MsgSkip and Font), **MsgSkip[off]** (toggles, `[on]` while
  on, greyed out while not allowed), and ours for development at the end, **Debug** (Battle
  Captures...: asks first, then the battle-capture viewer, which our own title had until `ｽﾀMENU`
  replaced it, 2026-10-11). Not done: Integer Scaling, Auto Copy Text to Clipboard
  (no text hook), the bar hiding in full screen.
- **Option → Mouse Movement** (checked at start, for the launch; 2026-10-10): the script's
  `IZ x, y` (`COMMAND.ADV:618` and the command files put the pointer back where it was before a
  menu) glides the system pointer there as `ags_setCursorLocation` does — seven points on its
  ease-out curve 7 ms apart, then the target (`Content/Screen/PointerGlide`); unchecked, only the
  position the script reads moves and the script waits 50 ms. `--auto` never moves the pointer.
- **Option → Hide Dimmed Map Names** (ours, owner's request 2026-10-16; checkable, saved in
  `user://settings.cfg`, `[display] hide_dimmed_map_names`, off by default): our map's plates
  (docs/ui/command-screen.md, "The map's markers") leave out the dimmed ones — the provinces neither
  ours nor next to ours and the dungeons not ours (`MapNamesChoice`). A click on such a town still
  reaches the script as the original's. A change shows at once.
- **Option → Fonts → MS Gothic / BIZ UDGothic** (ours, owner's requests 2026-10-11; a submenu, only
  the chosen font is marked, with a dot; saved in `user://settings.cfg`, `[display] font`): the font
  of the game's text. MS
  Gothic is the original's (docs/assets.md §6), read from the Windows fonts and greyed out where
  it is not installed; BIZ UDGothic (Morisawa, SIL OFL 1.1) is bundled in `godot/fonts` with its
  license. By default MS Gothic where installed, else BIZ UDGothic. Every character takes a fixed
  cell, so the layout is the same in both. A change shows at once in the message windows, menus
  and pickers; text the script printed on the stage keeps its font until drawn again.
  `--font ms-gothic|biz-ud-gothic` picks one for the launch without saving it (development).
  The deployment table's text uses the same font, regular.
- **Option → Interface → Original / New** (ours, owner's decision 25, 2026-10-12; a submenu like
  Fonts; saved in `user://settings.cfg`, `[display] interface`, Original by default): New shows our
  own screens over the original layout where there is one — so far the deployment table over
  `Sﾌﾞﾀｲ`'s unit grid (docs/ui/deployment-table.md). A change shows at once.
  `--interface original|new` picks one for the launch without saving it (development).
- **Option → Palette → Navy / Gold / Steel / Parchment / Crimson** (ours, owner's choice 2026-10-12;
  a submenu like Fonts; saved in `user://settings.cfg`, `[display] palette`, Steel by default): the
  colours of our battle screen and the map's name plates (`DeploymentPalette`; the deployment screen is lacquer
  since 2026-10-19, docs/ui/deployment-table.md). A change recolours an open screen at once.
  `--palette <name>` picks one for the launch without saving it (development).
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
  battle's `ChooseAsync`) turns it off (`STOP_ON_MENU`); every `MessageEvent` /
  `ValueMessageEvent` marks its `ScriptRef` seen. **Option → Skip Unseen Text** (checked by
  default, `MSGSKIP_SKIP_UNSEEN`; for the launch, not saved): unseen messages are skipped too, as
  the owner's xsystem35 does; unchecked, an unseen message turns the skip off and greys it out
  (`STOP_ON_UNSEEN`) until a seen one, as the web build does. **Option → MsgSkip Speeds Up
  Fades** (ours, owner's request 2026-10-10; checked by default, for the launch, not saved):
  while MsgSkip is on, a `PF`/`PW` fade lasts 150 ms at most (`MessageSkip.FadeDuration`);
  unchecked, fades keep xsystem35's length while skipping too (`ags_runEffect` waits without
  `KEYWAIT_SKIPPABLE`; `rate * 16 * 1000 / 60` ms). **MsgSkip skips the opening whole** (ours,
  owner's requests 2026-10-11): inside `ｵｰﾌﾟﾆﾝｸﾞ`, between the prologue and the rebellion
  (`OriginalVariant.WholeScenes`; `SceneRunner.RunningScenes` says which scenes run), the skippable
  waits and the fades take no time and nothing is shown — a frame passes only every 100 ms so the
  window stays responsive. Its stage commands (`G`, `CC`, `CE`, `CS`, `CF`, `CM`, `CB`, `CL`) are
  dropped: drawing its 3,400 of them on the emulated surface alone takes ~7 s; the stage is cleared
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
  alone. **Option → Forget Seen
  Messages...** (asks first) makes every message unseen again. The seen messages are one
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
