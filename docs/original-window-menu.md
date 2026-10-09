# The original's window menu (System 3.9)

The original game runs in a classic Windows window with a menu bar. This file records how it
looks (from the owner's screenshots of the English release, 2026-10-09), so they need not be
sent again, and what xsystem35 says the commands do. Our own menu bar follows it loosely.

## Title bar

`Kichikuou Rance - System3.9 Ver.5.49`, with the game's icon on the left.

## Menu bar

Four top-level items, left to right: **File**, **Options**, **Help**, **Skip Text [off]**.
`›` marks an item with a submenu; `———` is a separator line. Shortcuts are shown right-aligned.

### File

| Item | Shortcut |
|---|---|
| Full Screen | Alt+Enter |
| Take Screenshot... | Alt+P |
| ——— | |
| Restart System3.9... | Alt+R |
| ——— | |
| Close | |

### Options

| Item | Shortcut |
|---|---|
| Mouse movement [on] | |
| Use Joypad[off] | |
| Joypad Settings... | |
| ——— | |
| Full screen settings... | |
| ——— | |
| Window Size › | |
| ——— | |
| Sound settings... | Alt+S |
| CD Playback device › | |
| MIDI Playback device › | |
| ——— | |
| Startup settings... | |

The `[on]` / `[off]` items are toggles that show their state in the label (no check marks).

### Help

| Item | Shortcut |
|---|---|
| Visit AliceSoft's Website › | |
| ——— | |
| Game version... | |
| About system 3.9... | |

### Skip Text [off]

A top-level item **without a submenu**: a click toggles it, and the label changes between
`Skip Text [off]` and `Skip Text [on]`.

## What the commands do (xsystem35, unverified for System39.exe)

System39.exe is closed; xsystem35 (`F:\sources\Rance\xsystem35-sdl2`) reimplements the same menu
(`src/win/resources.rc`: File — Save Screenshot / Restart / Exit; a top-level `MsgSkip[off]`,
Japanese `文字送り[off]`). Its behaviour is our reference; the original's is not verified.

- **Restart** (`src/menu.c` `menu_resetmenu_open`: "Restart game?" confirmation →
  `nact_quit(true)`; `src/xsystem35.c` main loop → `sys_reset`): the interpreter starts over in
  the same process — all variables, array pages and strings cleared (`v_reset`), timers
  (`cmdz_reset`), graphics (`ags_reset`), music (`mus_reset`) — and the scenario runs again from
  its first line (`ｽﾀｰﾄ.ADV`: the opening and the title menu). **Not** reset: the random
  generator (seeded once at start-up, `sgenrand` in `sys35_init`; a restart continues its
  sequence) and a few engine statics (`IC`'s previous shape, `Y 3, 10000/10001`'s flag, `IK`'s
  key repeat). The game's own save files are untouched.
- **MsgSkip / Skip Text** (`src/msgskip.c`, `src/input.c`): while on, the message waits (`A`,
  `IK 0`) and the pauses (`Y 3`) that are skippable return 0 at once (`KEYWAIT_SKIPPABLE`), so
  the script runs on as if nothing was pressed. xsystem35's default flags turn it off by itself
  on a click (`STOP_ON_CLICK`), at a selection menu (`STOP_ON_MENU`) and at a message not seen
  before (`STOP_ON_UNSEEN`: seen messages are kept in a Bloom filter on disk). Whether
  System39.exe skips only seen text is unknown.
