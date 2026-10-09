# The window menu bar

Our game screen gets a classic Windows-style menu bar (owner's request, 2026-10-09). The
reference is **xsystem35's** menu bar — xsystem35 is the engine the port follows (golden
harness, input, `docs/golden-tests.md`). This file records how it looks, from the owner's
screenshots of xsystem35 2.14.3 on Windows, so they need not be sent again, and what its
commands do in the source (`F:\sources\Rance\xsystem35-sdl2`). The System 3.9 menu at the end is
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

## Ours (`godot/Strategy/GameScreen.MenuBar.cs`)

Done 2026-10-09; the owner's decisions: Restart, the random generator and MsgSkip's stops as
xsystem35's, skipping only seen messages if we know enough to do it (we do).

- A light bar above the game screen: **File** (Save Screenshot, Restart, Exit), **Screen**
  (Window, Fullscreen), **MsgSkip[off]** (toggles, `[on]` while on, greyed out while not
  allowed). Not done: Integer Scaling, the Option menu (no pointer warping, no text hook), the
  bar hiding in full screen.
- Hot keys: **F1** MsgSkip and **F4** full screen (on release, as xsystem35's `event.c`), and
  **Alt+R** Restart (ours, after System 3.9's). Backspace still returns to the title at once,
  without asking (development).
- **Save Screenshot** saves the screen as shown (1280×800, event CGs not masked) as
  `user://screenshots/kichikuou-<date>-<time>.png` and names the file on screen.
- **Restart / Exit** ask as xsystem35 does ("Restart game?" / "Quit game?", "Any unsaved progress
  will be lost."); the game waits while the box is open. Restart returns to our title (the
  opening and `ｽﾀMENU` are not ported, decision 10); the next game starts on a fresh
  `GameState`. The random generator lives for the whole launch (`EngineState`, seeded once with
  `--seed N` or the tick count) and so does the message skip.
- **MsgSkip**: `Core/Script/MessageSkip` (the state and the seen set) and `GameScreen`'s waits.
  The skippable waits — `IK 0` / `A` and `Y 3` — return 0 after one frame while it is on; input
  held then turns it off and is returned (`STOP_ON_CLICK`); a `]` menu (`SelectAsync`, the
  battle's `ChooseAsync`) turns it off (`STOP_ON_MENU`); every `MessageEvent` /
  `ValueMessageEvent` marks its `ScriptRef` seen, and an unseen one turns it off and greys it out
  (`STOP_ON_UNSEEN`). The seen messages are one `File:Line` per line in `msgskip.txt` next to the
  save slots (`user://saves`, `user://saves-auto` for `--auto`), exact where xsystem35 uses a
  Bloom filter. `--msgskip` starts a game with it on (development).

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
