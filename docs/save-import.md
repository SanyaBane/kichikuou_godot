# Importing the original's saves — survey and plan (2026-11-10)

The owner asked (2026-11-10) whether the original game's saves can be brought into the port, then for the import to
be planned with mock-ups. Until now the item stood in [backlog.md](backlog.md), "Saves" (put off 2026-10-11; decision
11 chose the port's own format). This file holds what was found, a proof of concept, the proposal, the mock-ups and
the questions for the owner.

## 1. What the original writes

`System39.exe` and xsystem35 write the same file, "System3.8 SaveData(c)ALICE-SOFT" (xsystem35 `savedata.c`,
`save_saveAll`; `SAVE_DATAVERSION` 0x350200), one file per slot, 26 slots:

| Part | Where | What |
|---|---|---|
| Header | 0-4095 | the signature; the selection and message windows' font sizes and colours; **the resume point** (`scoPage` = page + 1, `scoIndex`); the 128 selection and 128 message windows' rectangles; offsets of the stack, the strings and each of the 256 variable pages |
| Stack | `stackinfo` | xsystem35's own call stack, bytes: a near call is the return address and `0xEE`, a far call the page + 1, the address and `0xFF`; variables pushed `0xDD`, text states `0xCC`/`0xBB`/`0xAA` |
| Strings | `varStr` | a count, then that many zero-ended Shift JIS strings, numbered from 0 |
| Pages | `varSys[n]` | each page saved with the save flag: its size and number, then 16-bit values |
| Footer | the end | xsystem35 only: 256 × `0xFF`, `XS35`, a stack summary, `INFS` (`System39.exe` writes none) |

The files are named after the scenario archive: `KICHIKUSA.asd` for slot 1 to `KICHIKUSZ.asd` for slot 26, next to
`KICHIKUSA.ALD` (`System39.exe`; xsystem35 without a `.gr` file the same, its Android and web builds `KICHIKUsa.asd`;
very old xsystem35 `asleep.asd` to `zsleep.asd`; xsystem35's `-savedir` or a `.gr` file may put them elsewhere).
`ﾛｰﾄﾞｾｰﾌﾞ` uses slots 1-10: slot 1 the weekly autosave, 2-10 the player's (STR-22, docs/ui/save-load.md, §1).

## 2. Why every original save resumes where ours do

The scenario has one `QD`, `ﾛｰﾄﾞｾｰﾌﾞ.ADV:33` (at 0xcc, mode 3), reached only through `MAIN.ADV`'s `L_01875`, which two
near calls reach: `:20` (the thieves' week) and `:83` (the Leazas week). A manual save is a copy of slot 1 (`QC n, 1`).
So every original save holds the same resume point and one of two stacks:

- resume: page 33 (`ﾛｰﾄﾞｾｰﾌﾞ.ADV`), address 0xd0 (after `QD 1:`);
- stack, bottom first: a near call returning to `MAIN.ADV` at 0xa1 (`:21`, after `:20`) or 0x1f0 (`:84`, after
  `:83`), then a far call returning to page 12 (`MAIN.ADV`) at 0x187c (after `L_01875`'s `%#ﾛｰﾄﾞｾｰﾌﾞ.ADV:`).

The page numbers are the archive's link order (`xsys35dc.hed`); the addresses are the known version's (the English
translation v1.0, docs/import.md), read with `xsys35dc -a`. These are the port's own two autosaves
(`WeekLoop.ThievesAutosave`, `LeazasAutosave`; `WeekLoop.ResumeAsync`), so an original save maps onto a `SaveData`
resuming at `MAIN.ADV:20` or `:83` and loads as one of ours does — "Load Complete", then the week's start again.

## 3. What carries over

| The original's | The port's slot | Note |
|---|---|---|
| page 0, 65 537 values | `Variables`, the 806 of `VariableCatalog` | the rest are zero in every save looked at; a save with a value past 806 is refused |
| pages 1, 2, 4, 10 (20 001, 1 801, 2 101, 301) | `Pages`, the same sizes | `OriginalVariant.SavedPages` |
| strings 0-4 999 | `Strings[0..4999]` (5 001 places) | the same numbers, as the golden dumps have them; decoded with `Content/Text/Sjis` |
| the resume point and stack | `Resume`: `MAIN.ADV:20` or `:83` | anything else is refused |
| window rectangles, font sizes and colours | — | dropped, as in every save of ours (decision 11): the port's resume sets what it draws |
| the random generator | — | the original saves none either |
| the file's time | `Written` | what the original's own list shows (`LT`); ENG-07 makes it the last save's time for every slot |
| — | `Extras` | none: the talk marks (decision 30) see nothing read, so they say "New" until a scene is read again; the map's province cards fill at the first map wait (`KnownProvinces`) |
| page 4 (clears, fates, CGs seen, the title's buttons) | the persistent page (STR-31) | the library lays its own page 4 over any loaded save, so the original's only reaches the game if the import merges it in (§5, Q4) |

## 4. Proof of concept (2026-11-10)

A converter in Python (in the agent's scratchpad, not committed) read the owner's ten saves (all `System3.8
SaveData`; six with xsystem35's footer, four without, likely `System39.exe`'s) and wrote each as a slot of format 4:

| Slot | Week | Stage | Date | Resume |
|---|---|---|---|---|
| A | 22 | 2 | LP 3 · Month 6 · Week 1 | `MAIN.ADV:83` |
| B | 20 | 1 | LP 3 · Month 5 · Week 3 | `:83` |
| C | 15 | 1 | LP 3 · Month 4 · Week 2 | `:83` |
| D | 17 | 1 | LP 3 · Month 4 · Week 4 | `:83` |
| E | 18 | 1 | LP 3 · Month 5 · Week 1 | `:83` |
| F | 19 | 2 | LP 3 · Month 5 · Week 2 | `:83` |
| G | 22 | 2 | LP 3 · Month 6 · Week 1 | `:83` |
| H | 21 | 2 | LP 3 · Month 5 · Week 4 | `:83` |
| I | 16 | 1 | LP 3 · Month 4 · Week 3 | `:83` |
| J | 15 | 1 | LP 3 · Month 4 · Week 1 | `:83` |

All ten file times are the same second (ENG-07). Played headless with `--load --auto --fast --quit --seed 2`:
slot E (one of the four without the footer) from week 18 to week 20 — a replenish, the Monsters' Cave (dungeon 2),
the rebels' attacks on Ys (5) repelled, South (4) taken; slot A from week 22 (stage 2) to week 24. Both ended with
exit code 0, nothing unported met; the talk marks said "New" everywhere, as §3 expects.

## 5. The proposal (round 1, not taken: the owner chose §7)

**Reading** (no screen): `Content/Saves/AsdSave` reads the format whole (§1) with its own checks; `Original.Content`
maps it onto a `SaveData` for the original (§2-§3) and refuses, with the reason in words, a save of another version
or game, a save resuming elsewhere, a value past the 806 variables. The addresses are the known version's constants,
checked by a content test against the scenario archive's bytes (a `QD` at `ﾛｰﾄﾞｾｰﾌﾞ` 0xcc, near calls to 0x1875 at
`MAIN` 0x9c and 0x1eb). A save already imported (the same contents) is skipped, so a second import adds nothing.

**Where the saves come from**: the game folder the player imported (docs/import.md), its `KICHIKUS?.asd` in any case
and `?sleep.asd`; "Choose another folder…" for xsystem35's save folder.

**Where they go** — two ways, drawn as mock-up round 1 (§6):

- **Copied into the manual saves** (proposed): an Import in Option → Game (B) and, on the first launch, a step of the
  import screen when the folder holds saves (C). Each becomes a manual save numbered after the highest, its time the
  file's; they are numbered by week, the oldest first, slot A before the others of its week last, so the list (newest
  first: by time, then by number, `SaveSlots.NewestFirst`) shows them under the port's own newer saves, the newest week
  on top. The row and the page say where it came from: an "Original A" tag where an auto-save has "Turn Start", the
  slip's comment left to the player. After that they are saves like any other — loaded, commented, deleted, taken by
  the sweep ("Delete all without ★") when they have no comment; the files in the game folder are never touched, so an
  import can always be made again.
- **A third list on the Save/Load screen** (A): "Original · 10" beside the manual saves and the auto-saves, read from
  the folder each time the screen opens and loaded in place; no copy, no delete, no comment; in Save the red button
  is off, as on the auto-saves at first (G6).

**Page 4**: merged into the persistent page at the import (proposed) — a CG seen in either is seen, a fate seen in
either is seen (1 fortune and 10 misfortune make 100, both), the clears the larger count, the title's buttons the
larger state (`ｽﾀMENU` sets them again from the fates on its next pass). The original's slots all hold the same page 4
(STR-31's sync), so one file's is enough: the newest's.

**Not in this plan**: writing the port's saves back as `.asd` (only a week's-start auto-save could be expressed: the
original can resume nowhere else; it would need the windows' rectangles in the header); seeding the talk marks from
xsystem35's message-skip record (a Bloom filter over the messages' addresses, no list); a mod's saves (the Sparrow Mod:
its own addresses, decision 24's second variant).

### The build round 1 proposed

1. **The reader** (`Content/Saves/AsdSave`, tests on a file the test writes and, with `[LocalFolderFact]`, the owner's
   ten).
2. **The original's mapping** (`Original.Content/Saves/OriginalSaves`: the checks, `SaveData`, page 4's merge; a content
   test of the addresses against the archive, and one that loads an imported save through `WeekLoop.ResumeAsync`).
3. **The library**: a save's origin kept in the slot (format 5, `SaveData.Origin`: the original's slot letter), the
   copy into the manual saves (B/C), or the third list (A).
4. **The screens** as chosen, their entries in the screen gallery, and `--import-saves DIR` for unattended play (the
   saves imported, the newest loaded, a week played).
5. **Docs**: this file, docs/ui/save-load.md, CLAUDE.md, the backlog's "Saves" item removed.

## 6. Round 1: mock-ups (2026-11-10)

`tools/ui/mock_save_import.py`, over the screen gallery's dumps (`save-load-title`, `options-game`, `import-done`);
the saves are the owner's ten as §4 lists them, their figures and the maps made up (the dumps' week 18).

- **A1 — a third list** on the Save/Load screen opened from the title: "Manual · 12 | Auto · 20 | Original · 10"; a
  row is the slot's letter on an indigo seal, the date, the file and its time; the page: "Original · slot A", the
  figures, two lines on what a load does and that the file stays as it is; Load | Back, no Delete, no comment.
- **B1, B2 — Option → Game**: under the game folder, "Saves of the original game": the folder's ten in a small paper
  table (slot, date, file, what will happen), "Import 10 saves" and "Choose another folder…"; after it (B2) each row
  says what became of it ("Manual save 13"), one refused for another version as a sample, the button off.
- **C1 — the import screen's step**: on the first launch, after "The scenario is read", "Saves of the original game —
  10 found in this folder" with Import | Not now, then Play.
- **M1 — the imported saves in the manual list** (after B or C): the "Original A" tag at the row's foot where an
  auto-save has its kind, the page's tag "Original · slot A" after the title, the comment's slip empty with Edit, and
  what a load does.

### Questions for the owner

1. Copy into the manual saves (B and C, proposed) or a third list read in place (A)?
2. With a copy: both places — Option → Game and the first launch's step (proposed) — or one of them?
3. The mark: an "Original A" tag on the row and the page (proposed), or none (the comment left to the player)?
4. Page 4 merged into the port's (proposed), taken only when the port has none yet, or never?
5. The clears: the larger count (proposed: never counts a clear twice) or the two added?

Answered by §7: none of round 1.

## 7. The owner's choice (2026-11-10): File → Import Original Save…, loaded at once

The owner did not take round 1: no copy into the port's saves and no list of them. The import is an item of the menu
bar's File menu; the player points at a save file and the game loads it at once, as a load from the title would. The
import writes nothing — no save of ours, the `.asd` untouched; the player saves when they want, and the game's own
auto-saves go on as ever (at the next turn's start and at Turn END).

1. **File → Import Original Save…**, between Save Screenshot and Restart (docs/window-menu.md; ours, xsystem35 has no
   such item). Godot's native file dialog (`FileDialog`, `UseNativeDialog`, as the import screen's folder dialog),
   opened in the game folder, showing `*.asd`.
2. **The file is read and checked** (the checks of §5, "Reading": the format, the known version's page and addresses,
   the 806 variables). A refusal is a message box saying why in words: not a save of this game, a save of another
   version, a save that resumes somewhere the port does not know.
3. **A question, as Restart's** (the menu bar's `ConfirmationDialog`, `PhoneMenu.QuestionOf`): the save's date and
   file, and that the game under way is left without saving — Load | Cancel. Also on the title: it shows which save
   was picked.
4. **The game screen starts again** (as Restart) and, after the boot, loads the save: the memory restored, the port's
   persistent page laid over it as over any loaded save (the port's own clears and gallery stay, the original's page 4
   is not taken), `RetVal` 0, then what follows any load — "Load Complete" and the week's start
   (`GameSession.LoadAsync(SaveData)`, then `ResumeAsync` as after `LD`). What §3 says of `Extras` holds: the talk
   marks say "New" until a scene is read again, the province cards fill at the first map wait.

No mock-ups: the menu item, the file dialog, the message box and the question are the stock ones the menu bar's Restart
and the import screen already use.

**An imported game is marked, and decision 30 stays off in it** (owner, 2026-11-10). An original save has no record of
the lines read, so the talk marks and "New scenes first" would take every repeated line and variant for unread (§3).
The owner's answer:

- The load writes a mark into the game's `Extras` (the port's own data); every save of that game carries it on — manual
  saves, auto-saves, Turn END saves — and a load brings it back. A new game starts without it.
- In a marked game **"New scenes first" is off and cannot be turned on**: Option → Tweaks shows its check box greyed
  with a line saying why; the player's own setting stays as it was and holds again in a game without the mark. So
  everything decision 30 changes in play is the original's there — the draws at the original's odds, no Kanami's choice
  (Code's event 4 counts the flags as the original does: two or more lead to ending 3), no Kentarou's hint lines.
- **The marks New / ??? are not shown** in a marked game (the Subordinates screen's bubbles and Talk pill, the harem's
  slots, Summon pill and "This visit"); the read lines are still noted, as always (§9 of docs/ui/talk-marks.md).
- How the mark shows on the saves: mock-up round 2 below.

### Round 2: mock-ups (2026-11-10)

`tools/ui/mock_save_import.py`, over the gallery's `save-load-title`, `save-load-auto-end` and `options-tweaks`; in
each list the six newest rows are the imported game's saves, the rest an older game's (made up).

- **R1, R1a — a paper tag "Original"** (as round 1's M1) at the row's bottom right; on an auto-save left of its Turn
  Start / Turn End tag, a comment cut before both. The page: the tag after the title, and one quiet line: "A game
  imported from the original's save: "New scenes first" and the talk marks are off in it."
- **R2, R2a — an indigo seal**: the row as today, the number (or the auto-save's "A") on indigo instead of vermilion
  (gold); the page's tag indigo, the same line.
- **O1 — Option → Tweaks in a marked game**: "New scenes first" greyed and empty, its line under it, and in vermilion
  ink "Off in this game: it was imported from a save of the original, which keeps no record of the scenes read."

### Questions for the owner (round 2)

1. The mark on the saves: R1 (a tag) or R2 (the seal's colour; the agent's choice — the rows stay as they are, the
   page says it in words)?
2. The two lines' words (the page's and O1's) as drawn, or other words?
3. Kanami's choice goes with the option in a marked game, so Code's event 4 is the original's there (two flags or more:
   ending 3) — accepted?
4. Anywhere else in the game the mark should show (the agent proposes nowhere: the saves and the options window)?

### The owner's choice (2026-11-10): R1, "Original" at the right

**R1**, the paper tag "Original", on every save of a marked game, at the row's right edge: on an auto-save the kind's
tag (Turn Start / Turn End) moves left of it, a comment cut before both (**R1b**, drawn after the owner's note); on a
manual save the tag alone at the bottom right; on the page the tag after the title (after "Auto" and the kind's on an
auto-save) and the quiet line. The other questions were left as proposed: the two lines' words as drawn, Kanami's
choice gone with the option (accepted), the mark nowhere else.

### The build (after round 2)

1. **The reader**: `Content/Saves/AsdSave` reads the format (§1); tests on a file the test writes and, with
   `[LocalFolderFact]`, the owner's ten in the game folder.
2. **The original's mapping**: `Original.Content/Saves/OriginalSave` checks it against the known version (§2) and makes
   the `SaveData` (§3) with the mark in its `Extras`, or says why not; a content test checks the page numbers and
   addresses against the scenario archive's bytes.
3. **The mark's effects** (Original): `GameState.ImportedFromOriginal` (the `Extras` key); `OriginalScriptContext`'s
   "New scenes first" holds only when the option is on and the game is not marked (read at each use, so a load of
   another save sets it right); the scene hooks (Kanami's choice, Kentarou's line) follow it; the marks (`SceneMarks`,
   `PickerMarks`) shown nowhere in a marked game. Tests: a marked state draws as the original's odds, asks nothing in
   Code's event 4, and has no marks; the mark is carried by `SaveData.Capture` / `Restore`.
4. **The load**: `GameSession.LoadAsync(SaveData)` (the restore, the persistent page, `ResumeAsync`); a content test
   loads the owner's slot E and plays on to `COMMAND`'s first map wait.
5. **Godot**: File → Import Original Save… (the dialog, the refusal, the question, the game screen started again with
   the save); Option → Tweaks as O1; the Save/Load screen's tag as R1/R1b (`SaveSlots` reads the mark from each save's
   `Extras`); the screen gallery's entries (a marked sample in `GalleryStates`); `--load-original FILE` for unattended
   play (a headless run from slot E).
6. **Docs**: this file, docs/window-menu.md, docs/tweaks.md and docs/ui/talk-marks.md (the option off in a marked game),
   docs/ui/save-load.md (the tag), docs/ui/style.md (the "Original" tag among the colours left on purpose), CLAUDE.md,
   the backlog.

**Not now**: the phone has no menu bar (its Menu sheet, docs/ui/phone-build.md, step L1) and Android's file dialog goes
through its storage framework — a tile of the Menu sheet later ([backlog.md](backlog.md)). The tablet has the menu bar;
whether Godot's native dialog opens an `.asd` there is checked at the build.

### Built (2026-11-10)

Steps 1-6 are built:

1. `Content/Saves/AsdSave` reads the format whole (`System3.8 SaveData` only; xsystem35's older "System3.5 SavaData"
   and other files are refused in words), the stack's frames bottom first, the strings decoded with `Text/Sjis`.
2. `Original.Content/Saves/OriginalSave` maps it (`Read`, `FromAsd`) and refuses with the reason (`OriginalSaveException`);
   the slot carries `GameState.ImportedFromOriginal` (`Extras` key `imported-original`). `OriginalSaveTests` check the
   addresses against `KICHIKUSA.ALD`'s bytes, the pages' sizes against the boot, and the owner's ten.
3. `OriginalScriptContext.NewScenesOption` is the player's option, `NewScenesFirst` the rule (the option in an unmarked
   game); `SubordinatesScreen.TalkMarks`, `HaremRoster.Girls` and `PickerMarks` show and run no marks in a marked game.
4. `GameSession.LoadImportedAsync` (on `ScriptContext.LoadImported`, `LastImport`); a content test loads the owner's slot
   E and plays to `COMMAND`'s first map wait, nothing written.
5. Godot: `GameScreen.ImportSave.cs` (the dialog, the refusal, the question, `GameStart.Original`), Option → Tweaks as O1
   (`OptionsView.ImportedGame`), the tag as R1/R1b (`SaveSlot.Original`, `SaveLoadView.DrawOriginalTag`), the gallery's
   `options-tweaks-imported`, `save-load-original`, `save-load-original-auto` (`GalleryStates.SampleLibrary`'s
   `importedWeeks`), and `--load-original FILE`: slot E played headless from week 18 to 21 with `--auto --fast --quit
   --seed 2 --weeks 20`, exit 0, nothing unported.

**The agent's choices, for the owner to review:**

- "Load Complete" names the save **"Original save E"** — the slot's letter from the file's name (`KICHIKUSE.asd`;
  `OriginalSave.Label`), "Original save" for a file named otherwise — where it names ours "Auto-save" or "Manual save 3".
- The question: title "Load the original's save?", the lines "Original save E: LP 3 · Month 5 · Week 1",
  "KICHIKUSE.asd, saved 2026-10-10 21:40", then Restart's "Any unsaved progress will be lost."; buttons Load | Cancel. The
  same words on the title. A refusal: a message box titled "KICHIKUSE.asd cannot be imported." with the reader's words.
- With no persistent page of the port yet (a first launch), the save's own page 4 stays, as with any load; the game's
  first auto-save then makes it the port's (`ﾛｰﾄﾞｾｰﾌﾞ`'s page-4 sync).
- The tag's paper and ink are fixed colours (R1's), framed in the palette's gold, as the Turn Start / Turn End tags
  are; the Steel palette keeps them.
- Not built: the tag on the phone's save rows (both orientations; a marked game's saves reach the phone only when copied
  there) and a check of the tablet's native dialog with an `.asd` — both in [backlog.md](backlog.md), "Saves".

### The build as first planned (superseded by "The build (after round 2)" below)

1. **The reader**: `Content/Saves/AsdSave` reads the format (§1) — the header, the stack's frames, the strings
   (`Text/Sjis`), the pages; tests on a file the test writes and, with `[LocalFolderFact]`, the owner's ten in the
   game folder.
2. **The original's mapping**: `Original.Content/Saves/OriginalSave` checks an `AsdSave` against the known version (§2)
   and makes the `SaveData` (§3), or says why not; a content test checks its page numbers and addresses against the
   scenario archive's bytes.
3. **The load**: `GameSession.LoadAsync(SaveData)` (the restore, the persistent page, `ResumeAsync`); a content test
   loads the owner's slot E and plays on to `COMMAND`'s first map wait.
4. **Godot**: the menu item, the dialog, the refusal, the question, the game screen started again with the save
   (`GameOptions`); `--load-original FILE` for unattended play (a headless run from slot E as the proof of concept's).
5. **Docs**: this file, docs/window-menu.md, CLAUDE.md, the backlog (the phone's Menu sheet).

## 8. On Android (2026-11-10): survey and mock-up round 3

The owner asked for File → Import Original Save… on Android too. What stands in the way of the PC's way:

- **The phone has no menu bar**: its commands are the Menu sheet's tiles (`PhoneMenu`, `MenuSheetView`; held landscape
  LN3, upright PN1), on the title as in play. The tablet keeps the menu bar and its stock boxes (Restart's question).
- **Android's own file picker** (Godot's native `FileDialog`) answers with a `content://` URI. Since Godot 4.6 Godot's
  `FileAccess` reads such a URI (`System.IO` does not), so a file picked there could be read as bytes — not tried in
  this project. But since Android 11 the picker hides `Android/data`, and that is where the player's copy of the game
  folder lives (`GameData.AndroidGameFolder`, README "Installing") — and the original's saves with it, next to
  `KICHIKUSA.ALD` (on the owner's Poco `KICHIKUSA.asd` … `KICHIKUSJ.asd`). The picker could reach only a save kept
  elsewhere (Downloads).
- So on Android the game offers **the saves its game folder holds** itself: every `*.asd` there read when the list opens
  (`OriginalSave.Read`), each named by its slot's letter and dated by its week, one that cannot be imported greyed with
  its reason. The original's own list (`ﾛｰﾄﾞｾｰﾌﾞ`) writes slots 1-10, so at most ten (A-J) in practice. After the pick,
  everything is the PC's (§7): the same question, the game started again with the save, nothing written, the game marked.

`tools/ui/mock_save_import_android.py`, over the gallery's `menu-sheet` (both orientations), the phone's
`save-load-original` and the tablet's `map`; the saves are the owner's ten (§4):

- **N1, N1p — the Menu sheet's new tile**: "Import original save ›" between Screenshot and Restart, as on the PC's File
  menu; a page's tile (as Debug's). Held landscape the second row is Import original save, Restart, Exit (Debug after
  them in a development build); upright the row stands between Screenshot and Restart….
- **S1, S1p — its page "Original saves"** (the agent's choice): ‹ Menu first, then a tile (a row upright) per save —
  "Save E", "LP 3 · M5 · W1", "Stage 1". **S1q, S1qp**: a save's tap asks the PC's question in the sheet's callout
  (the save, its date, the file and its time, "Any unsaved progress will be lost."), Load | Cancel. **S1r**: a save
  that cannot be imported, greyed, its reason a word; a tap says why in the reader's words, OK. **S1n**: none found —
  where the original keeps them and where they go (`Android/data/org.kichikuou.port/files/game`).
- **S2 — the alternative: a screen of its own** in Save/Load's look (LL1): the list at the left (the letter on a seal,
  the date, the stage), the chosen save's page at the right with its map and figures (`SaveSlots.Of` reads them from the
  save), the file and what a load does where a comment stands, the red Load | Back. More to build (both orientations).
- **T1 — the tablet**: the menu bar's item opens a stock dialog listing the same saves (Load | Cancel), then the PC's
  question — stock as the bar's other boxes.

### Questions for the owner (round 3)

1. The phone: S1 (a page of the Menu sheet; the agent's choice) or S2 (a screen of its own)?
2. The tablet: T1 (a stock dialog with the list; the agent's choice), or our S1/S2 there too?
3. Where the saves are looked for: the game folder alone (the agent's choice — they come with it), or also the app's
   `files` folder beside it, for a player who copies the saves alone?
4. A tile "Another file…" opening Android's own picker, for a save kept outside `Android/data` (Downloads): not now (the
   agent's choice; untried, and the saves come with the folder), or with it?

### The owner's choice (2026-11-10): S1 on the phone, T1 on the tablet

S1, the Menu sheet's page, and T1, the tablet's stock list; the other questions as proposed: the game folder alone, no
"Another file…", the version left to the owner.

### Built (2026-11-10)

- `Original.Content/Saves/OriginalSaveFolder` reads a folder's `*.asd` (any case) by name into `OriginalSaveFile`s: the
  slot, its week (`Week`) and story stage, the file's time, or the refusal in the reader's words and in a word
  (`OriginalSaveException.Word`: `OriginalSave.NotASave`, `Unreadable`, `AnotherVersion`, `NotAtWeekStart`); the PC's
  question takes its lines from it (`QuestionLines`).
- `Devices/PhoneMenu`: the tile Import original save (`Command.ImportSave`, a page) between Screenshot and Restart; its
  page `PhoneMenu.Saves(SaveShelf)` — ‹ Menu, a `Kind.Save` tile per save (word, date, stage; a refused one greyed with
  "cannot be imported" and the word), or a note (`Kind.Note`) where they go; held landscape the grid two rows at least
  (‹ Menu the first page's size), the note across the rest; upright the note a row 560 high. `ImportQuestion` and
  `RefusalOf` are the PC's words; a callout grows by `LinePitch` a line (`QuestionAt(f, tile, lines)`) and stands over its
  tile unless only the room under it holds it, kept on the screen.
- `godot/Strategy/MenuSheetView`: the saves' page read as it opens (`GameScreen.OriginalSaveShelf`: `GameData.GameDirectory`,
  named without Android's `/storage/emulated/0/`), the question's lines wrapped to the callout, a refusal's OK alone,
  Load closing the sheet and handing the save over (`SaveChosen` → `GameScreen.LoadOriginal`, the PC's way on).
- `godot/Strategy/GameScreen.ImportSave.cs`: on Android and in the touch layout the menu bar's item opens T1 (a
  `ConfirmationDialog` with a `Tree`, the first row selected, Load or a double tap), none found a message box with the
  note; the PC keeps the native dialog.
- Unattended: `--menu-import <letter>` imports that slot's save at the first map wait — on a phone through the Menu
  sheet's taps (Godot's input), else through T1 — and plays on with the same options. From the week-18 save with the
  owner's slot E in the game folder, headless, `--auto --fast --quit --seed 2 --weeks 20`: `--phone landscape`,
  `--phone upright` and `--touch` each imported E and played weeks 18-20, exit 0.
- Tests: `OriginalSaveFolderTests` (the folder read, the page's tiles, the grid, the question's growth and words); the
  Menu sheet's tests updated for the new tile. The screen gallery's phone passes: `menu-sheet-saves`,
  `menu-sheet-save-question`, `menu-sheet-save-refused`, `menu-sheet-no-saves` (docs/ui/gallery.md).

Not yet: a run on the Poco or the tablet — with the version after 0.1, which this goes into (owner, 2026-11-10: not
0.1; the Poco keeps the release build 0.1 meanwhile) —, and T1 in the screen gallery (a stock dialog the tablet pass
does not dump).
