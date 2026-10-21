# Our Save/Load screen — survey and mock-ups

The owner asked (2026-10-20) for our own Save/Load screen in the lacquer of our other screens, after the command
panel's Load and Save rows (docs/ui/command-screen.md, "The command panel") and the save of the moment (owner's
decision 26, docs/strategy/README.md). This is the survey, the first round of mock-ups and the owner's choice, C,
which is built (§5, "What was built").

## 1. What the original does

`ﾛｰﾄﾞｾｰﾌﾞ.ADV` (`Strategy/LoadSaveScreen`) runs in six modes, the caller's `RetVal`. Only two show slot lists:

| Mode | Caller | What the player sees |
|---|---|---|
| 0 | the title's Load (`ｽﾀMENU.ADV:241`) | the load list at once; a right click goes back to the title |
| 2 | `COMMAND`'s Load/Save (our panel's Load and Save rows) | the Load / Save / Don't menu (answered unseen by `LoadSaveShortcut` in our interface), then a list; a right click on a list goes back to that menu, Don't to the map |
| 1 | nothing calls it | the save list at once |
| 3, 10, 11 | the week's autosave, the boot, an ending | no list: the "Auto-Save" notice (3), nothing (10, 11) |

- **The load list** (`L_0027f`, `:120-238`): window 113 with "＜ Data Load ＞" and three lines — "When you load, the
  auto-save data will be overwritten with the loaded data", which decision 26 made untrue for a save of the moment —
  and a `]` menu (`B1 2, 250, 50, …`, the selection box over the map's top right) of ten items: "Auto" and "1"-"9",
  each with its slot's date "［ＬＰ：y, Month m, Week w］" or dashes. The date is the slot's `Tmp0021` (the week its
  autosave was taken, or the week of a save of the moment), read with `LP`. An empty slot is an item too: choosing it
  lists the slots again (STR-23 counts it). A chosen slot is loaded (`LD`); "Load Complete" with its number and date
  follows in window 113.
- **The save list** (`L_006a2`, `:325-512`): window 113 with "＜ Saving Data ＞", the date being saved and, since
  decision 26, our "Saving the game as it is now."; a menu of nine items, slots 2-10 as "1"-"9". A choice saves at
  once — over a filled slot too, without a question — and lists the slots again with the new date; a right click
  leaves.
- **Notices**: "＜ Auto-Save ＞" at each turn's start (window 113 with the slot's number and date), "＜ Load
  Complete ＞" after a load, "＜ An Error Occurred ＞" when a write fails. In our interface these are already our
  message windows (docs/ui/messages.md); they stay.
- The labels are off by one: the list's "1" is slot 2, and "Load Complete" says "Data Number (2)" for it (the
  script's `Tmp0022`). Our screen can call it what the list calls it.

Dumps (week-18 save, 2026-10-19): the save list and the load list in `COMMAND`, the load list from the title.

## 2. What our screen can show

The script tells the player only the slot's date. Our screen reads the slots themselves (`ISaveStore`, the same
files), which is presentation only — no rule reads them:

- **the kind**: Auto (slot 1), a turn's start (resume point `MAIN.ADV:20` / `:83`) or a save of the moment
  (`:69` / `:213`, `WeekLoop.IsMomentSave`) — today the two look the same in the lists;
- the date (`Week`), the story stage (`StoryStage`: the prologue, stage 1-4), the budget (`Gold`), the realm (the
  provinces Leazas owns; the hideout in the prologue), the units and their troops;
- when the slot was written (the file's time, as `LT` reads it);
- **the map of that game**, as All Data draws it (`各種DATA.ADV:1-11`): the map's six tiles (`Ui_MapCg*`) at a third
  and the markers (`Marks[]`, page 10, saved) over them. In the owner's slots (weeks 9-18) the maps differ by a few
  flags only; later in a game they differ more.

## 3. How the screen answers the script

As our province menu and Subordinates screen do: the screen covers `ﾛｰﾄﾞｾｰﾌﾞ`'s window 113 and its slot menu and
answers the menu with the item the player means — a load list's item (`slots` order: Auto, 1-9), a save list's item
(1-9), or 0 for a right click. No rule moves into the UI.

- Empty slots in the load list take no click (the script would only list them again).
- **Load | Save tabs** (in `COMMAND` only; the title's Load has no Save): the other tab is a right click on the
  list, then the Load / Save / Don't menu answered with the other list (`LoadSaveShortcut` already answers that
  menu).
- **A question before saving over a game** (ours: the original overwrites at once): in a callout at the clicked
  slot, as the Treasure screen's (docs/ui/pickers.md, round W), or a second click in C.
- After a save the script lists the slots again; the screen stays open with the new slot filled.
- The "Auto-Save", "Load Complete" and error notices stay our message windows.

## 4. Round 1: three layouts (2026-10-20)

`tools/ui/mock_saves.py` over the map dump of the week-18 save, with the owner's own ten slots (Auto and nine, the
ninth a save of the moment); each layout in Load and Save:

- **A — a paper ledger** (A1, A2): one lacquer panel, the slots as rows of the Subordinates screen's paper ledger
  (No., Kind, Date, Stage, Budget, Realm, Troops, Written); under it the hovered slot's map and what a click does.
  Save: Auto greyed; saving over a game asks in a callout at its row.
- **B — cards with the map** (B1, B2): the slots as ten cards in two columns, each with that game's map, a seal with
  its number, the date, its kind, the stage and the figures. Save: Auto dim ("the game writes it each turn"),
  empty cards "Empty — save here", the question in a callout at the card.
- **C — a list and the slot's page** (C1, C2): the slots as a narrow list on the left (seal, date, kind, time); a
  click picks one and shows its page — the map large, the figures — with a red lacquer button "Load this game" /
  "Save over slot n". The button is the second click, so no question.

## 5. The owner's choice (2026-10-20): C

**C, a list and the slot's page**, as drawn in C1/C2. The owner answered the layout only; the rest is built as the
mock-ups show it, to be changed after the owner's look:

- no callout before saving over a game: the red button is the second click (question 2 is moot);
- the Load | Save tabs from `COMMAND` (question 3), none from the title;
- the page's figures as drawn (question 4): the date and kind, the stage, the budget, the realm, the units, the troops,
  the time written; the map large; Auto greyed in the Save list; empty slots "Empty" (no click in Load; in Save the page
  says what will be saved and the button is "Save here").

### The build (a plan for the next session)

1. **The model**, `Original.Content/Strategy/SaveSlots` (with tests): from the `ISaveStore` and the slot number a
   slot's summary — kind (`WeekLoop.IsMomentSave` on `SaveData.Resume`; slot 1 Auto), week and date, stage, gold,
   provinces owned (`Prov` owner 1, 1-99), units with a status and their troops, the file's time
   (`ISaveStore.Timestamp`), the map's tiles (`Ui_MapCg*`) and markers (`Marks[]`, `Ui_MarkCount`). Read the slot's
   memory through a `GameState` restored from it (`SaveData.Restore`) rather than offsets.
2. **Recognising the lists**: `ﾛｰﾄﾞｾｰﾌﾞ.ADV`'s load menu (items at `Ref(at + 7)` for a filled slot, `:319` for an
   empty one, `at = 122 + 11 (slot - 1)`) and save menu (`Ref(352 + 11 (slot - 2) + 3)`), as `CommandMenu.For`
   recognises COMMAND's; the item → slot mapping. Window 113's header and text are covered while it shows
   (`GamePresenter.SaveScreenOpen`; the notices "Auto-Save", "Load Complete" stay our message windows).
3. **The view**, `godot/Strategy/SaveLoadView` on `Lacquer` (the panel, rows, seals, the red button), opened from
   `GameScreen.SelectAsync` like `CommandMenuView`; a row's click picks, the button answers the menu with the
   slot's item; a right click or Esc answers 0. The map: the six tile CGs at a third with the markers (CG less
   10000, transparent index 40) as All Data draws them, as a texture from `CgLibrary` (as `FacePictures` does).
4. **The tabs**: the other tab answers 0 (back to the Load / Save / Don't menu), then that menu with the other list
   (`LoadSaveShortcut.Start` again, or a `Switch`); after a save the script lists the slots again — the screen stays.
5. **Unattended play** (`--interface new`, a window): from the week-18 save, open the screen from the command panel,
   dump `save-load`, `save-load-save`, switch tabs, save into an empty slot, leave (never from the prologue).
6. Docs: this file, CLAUDE.md's layout entry, the backlog's Interface item.

### What was built (2026-10-20)

The six steps above, with these details settled while building (to be changed after the owner's look):

- **The model** `Original.Content/Strategy/SaveSlots` (`SaveSlotsTests`): `SaveSlot` per slot — the kind, the date from the
  slot's `Tmp0021` as the lists date it, the stage, the budget, the realm, the units with a status and the troops as the
  status window sums them, the file's time, the six tiles and the markers already placed for the miniature (All Data's
  arithmetic); `SaveSlots.Now` is the game being played. `ListOf` recognises the two lists, `ItemOf` gives a slot's item,
  `Takes` what a click may choose, `FirstChoice` the slot chosen as a list opens: to load the one written last, to save the
  first empty one, else the one written longest ago.
- **The screen** `godot/Strategy/SaveLoadView`, opened from `GameScreen.SelectAsync` while `ﾛｰﾄﾞｾｰﾌﾞ`'s screen is open
  (`GamePresenter.SaveScreenOpen`); `GamePresenter.SaveScreenCovered` leaves the window 113, its text and the menu
  undrawn (only the stage under the screen, dimmed) until the save screen closes, so the notices after it ("Load
  Complete", "Auto-Save") are our message windows as before. Between two lists (a save listed again, a tab) the screen
  stays drawn and takes no clicks. The map is the six tiles at two thirds and the markers at twice their size (All
  Data's miniature at 2x), cached per tiles and markers.
- An empty slot to save into shows the game being saved under "Slot n · Empty"; a load's note says what becomes of Auto
  (a save of the moment leaves it, a turn's start becomes it, decision 26). The right click's note names where it goes:
  the map, the title, or the Load / Save / Don't menu when the interface was changed with that menu open.
- **The tabs**: `LoadSaveShortcut.Switch` — the list left with 0, the menu answered with the other list.
- **Unattended play**: `--saveload` (CLAUDE.md, "A look at one screen"); the title's load list is dumped too.
- The time written is the slot's own stamp (`SaveData.Written`), as `LT` reads it: set when its game is saved (`QD`,
  `QC`), kept when a save writes page 4 into every slot (`WritePersistentPage`, `:511`, a `QP`). The original reads the
  file's time, which that sync renews in every slot, so all showed the last save's time (ENG-07, fixed 2026-10-20). A
  slot of format 1-2 is dated by its file's time.

## 6. Questions for the owner (answered by the choice above)

1. Which layout: A, B, C, or a mix.
2. A question before saving over a filled slot — wanted (ours; the original has none)?
3. Load | Save tabs on the screen (from `COMMAND`), or only the list the panel's row opened?
4. Which figures a slot shows: the date and kind surely; the stage, budget, realm, troops, the time it was written?

## 7. Round 2: sorting the list (owner's request 2026-10-20)

The owner asked for the list in two orders: by number (as built) and by date, with Auto at the very top — in a panel
of its own if need be. `tools/ui/mock_saves.py ... sort` draws it over the owner's ten slots (their times made up:
ENG-07 had stamped them all alike; one slot emptied in the Save pictures):

- **D — the switch in the title band** (D1 by number, D2 by date, D3 Save by date): "Order: No. | Date ▾" left of the
  tabs; the list as built, Auto its first row in either order.
- **E — Auto in a panel of its own** (E1, E2, E3): "Auto-save" over the list in a gold frame, then "Your saves" with
  the switch beside them and the nine rows a little lower (57 px for 63).

In both, by date: Auto first, then the games newest first, the empty slots last in number order; the time written is
the row's bright line. A row's seal keeps its slot's number, so the button and the page still name "Slot n".

### The owner's choice (2026-10-20): E, drawn again as F1-F4

**E**, with the first button named "Slot" (not "No.") and the second heading "Manual saves" (not "Your saves"). Each
button has two directions: a click on the active one turns it; a click on the other opens it by slot ascending or by
date newest first. The default is the original's, by slot with 1 at the top; the order chosen is kept across launches.
F1-F4 (the same script) draw it: F1 by slot ascending, F2 descending, F3 by date newest first, F4 oldest first (Save).

### What was built (2026-10-20)

- **The model**: `SlotSortKey`, `SlotOrder` (`Default`, `Clicked`) and `SaveSlots.Order` — slots 2-10 by slot or by
  `Written` (none as the oldest, equal times by slot), the empty slots last by slot either way (`SaveSlotsTests`).
- **The setting**: `godot/Strategy/SaveOrderChoice`, `[display] save_order` / `save_order_descending` in
  `user://settings.cfg`, as the Option menu's choices.
- **The screen**: `SaveLoadView` lays the list out as E — "Auto-save" and its gold-framed row (96-156), a rule, "Manual
  saves" with Slot | Date right-aligned (177-203; the active one in the chosen row's colours with a triangle, down for
  descending), the nine rows 57 high every 62 from 212. Sorted by date, a row's time is the bright line. The chosen slot
  stays chosen when the order changes.
- **Realm** (owner's report 2026-10-20) counts the provinces Leazas holds on the map: Nothing (75), Leazas's record off
  the map, is left out (it showed 16 where the map has 15; its hidden income is STR-58).
- **The red button** (owner, 2026-10-20): one line, no second — "Load", "Save over Slot n", "Save" (for an empty slot;
  "Load this game" and "Save here" before).
- **Unattended play**: `--saveload` clicks through the four orders on the load list (dumps `save-load-date`,
  `save-load-date-oldest`, `save-load-slot-down`) and clicks the player's own order back.

## 8. Fewer labels (owner, 2026-10-20)

Since decision 26 every manual save is a save of the moment, so the owner had the "Mid-turn" and "Turn start" tags
taken off the rows and the page, and ", mid-turn" off the Save page's "Saving:" line; Auto keeps its gold tag in its
row. A slot written before decision 26 still loads as a turn's start (Auto becomes it); only the load's note above
the red button says so. The rows' time written is drawn about a fifth larger (14 px, 16 when sorted by date).
The panel's title is "Save" or "Load" alone: the line beside it ("the game as it is now", "choose a game to go on
with") is gone (owner, 2026-10-20).

## 9. Auto-saves and manual saves (owner's decision 29, 2026-10-21)

The owner asked (2026-10-21) for saves to be **deletable** from both lists, and for two lists in place of the ten
slots: **auto-saves**, ten at most (the eleventh pushes the oldest out), and **manual saves** without the nine-slot
limit. The owner's answers to the agent's questions:

1. An auto-save is written at each turn's start, as now — the list is the last ten weeks.
2. Page 4 goes to a file of its own (STR-31's fix, the third option of 2026-10-20, see ENG-07).
3. Loading a save never makes it the auto-save (the original's `QC 1, n`): not wanted.
4. Saves of either list can be deleted by the player. Auto-saves are not sortable — newest at the top, oldest at the
   bottom; manual saves are sortable.
5. The original interface's save lists need not be kept: the project aims at our interface alone.
6. No names; a **comment** can be added to (and changed on) a save already made.

### What the ten slots hold today

- `ﾛｰﾄﾞｾｰﾌﾞ.ADV` (`Strategy/LoadSaveScreen`) numbers everything: slot 1 is the auto-save (`QD 1`, mode 3), the save
  list writes slots 2-10, a load is `QC 1, n` then `LD n`; page 4 is written into slots 1-10 after every save
  (`L_00ad6`, `:529-544`) and read from them at the boot and before a load (`L_00a7b`, mode 10). `Iｱﾘｽ.ADV:19` reads
  `F_OpeningSeen` from slot 1 (`LP 1, VAR0514, 1`).
- The engine's `ISaveStore` (`Script/SaveData.cs`) is numbered slots 1-26 (xsystem35's `SAVE_MAXNUMBER`), files
  `slotNN.ksav`, about 60 KB each; it cannot delete.
- Our screen answers the script's two menus (`SaveSlots.ListOf`: ten items and nine) with an item number.

With many saves the page-4 sync would rewrite every file at every save and every week; that, and the gallery lost
with the last save deleted, are why page 4 moves out.

### Round 4: two layouts (2026-10-21)

`tools/ui/mock_saves.py ... library`, over the map dump of the week-18 save; the saves are made up from the owner's
slots (ten auto-saves of consecutive weeks, fourteen manual saves with a few comments). Both keep C's page (the map,
the figures) and E's order switch for the manual saves:

- **G — the two lists as a switch over one column** (G1-G6): "Manual saves · 14 | Auto-saves · 10" over the list.
  Manual saves (G1): the order switch No. | Date, rows with the save's number on its seal, the date, the time
  written and the comment as a quiet second line; the list scrolls. Auto-saves (G2): the ten, newest first, a gold
  "A" seal, "start of the week". Save (G3): "+ New save" heads the manual list (a new save never replaces one);
  choosing a manual save offers "Save over No. n". Delete (G4): a button left of the red one, asking in a callout at
  it ("It cannot be undone."; Delete | Keep), the same in Load and Save and for both lists. The comment (G5): a field
  under the figures with Edit; written in place, Enter keeps it, Esc leaves the old one, 60 letters at most. Save on
  the Auto-saves side (G6): the red button off, Delete still works.
- **H — the two lists stacked** (H1-H3): the ten auto-saves as short rows (date, time) over the column's top half,
  the manual saves under them, about four in view; one save chosen across both. The page is G's.

### The plan (after the owner's choice of layout)

1. **The library** (engine, `Script/`): the saves as two lists of files next to each other — auto-saves numbered as
   written, the ten newest kept; manual saves numbered as made (a new one after the highest), any number of them —
   plus `persistent`, page 4 and `VAR0194`; a delete; a slot's `Comment` in the slot format (format 4; changing it
   keeps `Written`). The first open of a folder of `slotNN.ksav` migrates it: slot 1 the newest auto-save, slots
   2-10 manual saves 1-9 (their list numbers) with their times, page 4 read from them by `L_00a7b`'s rule (the last
   one wins); the old files are moved aside, not deleted.
2. **`ﾛｰﾄﾞｾｰﾌﾞ` on the library** (ours, as decision 26's `SaveHere`: the port only, the differential tests keep the
   plain slots): mode 3 writes a new auto-save; the lists ask our screen for a save, not a menu item; a load is `LD`
   of that save, never `QC 1, n`; a save is `QD` into a new manual save or the one chosen; page 4 is read and written
   through `persistent` (modes 10 and 11, after a save); slot 1 for `Iｱﾘｽ` is the newest auto-save. The notices
   ("Auto-Save", "Load Complete") name the save in our words, not "Data Number (n)".
3. **The screen** (`godot/Strategy/SaveLoadView`, the model `Original.Content/Strategy/SaveSlots`): the chosen layout,
   the scrolling list, "New save", Delete with its callout, the comment's field; Auto's panel goes. The original
   interface uses it too.
4. **Tools**: `--load --auto` loads the newest auto-save; `--slots-from` copies a folder (migrated in the copy, the
   kept saves stay as they are); `--saveload` exercises New save, a comment, a delete and both lists.
5. **Tests**: the library (the ring, the numbering, delete, the migration, a comment keeping `Written`), the port
   (the ring through `WeekLoop`, a load leaving the auto-saves, page 4 through `persistent`), `SaveSlots`.
6. **Docs**: quirks (STR-31 fixed, STR-11 and ENG-07 noted), this file, CLAUDE.md.
