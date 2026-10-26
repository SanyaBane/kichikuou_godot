# The screen gallery — our screens without playing to them (plan and build, 2026-10-25; steps 1-5 built)

Our Storybook: a development mode that shows each of our screens on its own, in its states, fed from the kept saves,
and dumps them all as pictures in seconds. In code and commands it is the *screen* gallery (`ScreenGallery`,
`--screen-gallery`, Debug → Screen Gallery…), apart from the game's own CG gallery `CG` (`GalleryTests`). The owner pages through it in a window (switching palette, font, touch
mode, row heights live); the agent dumps it after a change and sees which screens changed. The pictures are never committed
(they hold the player's CGs): they go to `screen-gallery/` at the root, which git ignores (the owner, 2026-10-25: the
app's file pane opens its `index.html`), or to `KICHIKUOU_UI_GALLERY_DIR` in `local.env`.

**Why.** A look at a screen today plays to it: a kept save copied and loaded, the boot, the load list, the week's
banner, the menus, one launch for each family of screens (`--treasure`, `--saveload`, `--scout`, …), some moving the
system pointer — and some states no kept save reaches (§4). The gallery shows every screen in one launch, in states
of its choosing, without the pointer, the same pictures every time, so a change shows as a difference. Measured
(step 1): the whole gallery, 20 entries, dumps in 7-9 s with the launch; the `--treasure` look alone runs 6 s headless
(more in a window with its dumps). The gain is the one launch, the states and the diff, not one screen's speed; the
battle's looks (45-100 s) gain more. It is the same Godot project, so it builds as fast as the game.

**What it does not check.** The flow around a screen — when it opens, what is played unseen, what answer reaches the
script — stays with the tests and the looks from the kept saves (CLAUDE.md, "Commands"). The gallery checks the
drawing; fewer looks are needed, not none.

## 1. The survey: how each view is fed

Every view of ours is a plain Control built once by `GameScreen` (`GameScreen.cs:303-354`, `Ready*`), with
`GodotGlyphRenderer.Chosen` as its font and `new FacePictures(GameData.Cgs())` for pictures, and fed by a method that
takes a model from `Original.Content`. `UiPalettes` and `Lacquer` need no setting up. Every `Update*` in `GameScreen`
fits the 640×400 picture into the view's size the same way before it feeds — one helper (`PictureFit`) to pull out.

Verdicts: **ready** — a model built from a `GameState`, the view fed directly; **small** — a factory or helper to
pull out first (named); **heavy** — needs a battle replayed (§6).

| View | Feed | Model from | Verdict |
|---|---|---|---|
| `TreasureView` | `Refresh(state)` | the state; `Ui_ItemViewMode`, the prompt are the picker's variables, set on a copy | ready |
| `TreasureCallout` | `Show(lines, cell, waiting)`, `OpenMenu` | `TreasureRoster.ReplaceQuestion(state, unit)`; a refusal is the script's lines (read from the file) | ready |
| `HaremView` | `Refresh(state, girls)` | `presenter.HaremGirls()` (the names table is the scenario's) | ready |
| `SubordinatesView` | `Refresh(state, delta, talks)`, `ShowResult` | the state; talk marks optional (null) | ready; the plate small |
| `OrderResultPlate` | `Show(SubordinateResult)` | today only after an order ran (`SubordinateOrder.Result`) | small: `SubordinateResult.Preview(state, kind, unit, step)` |
| `MessageWindowView` | `Show(window, waiting, null, menu)` | today the presenter's `TextBox` from script events; `MessageWindow.Sample` gives a right speech only | small: `MessageWindow.SampleOf(kind, lines, speaker)` (the frame CGs are private) |
| `SceneMenuView` | `Open(MenuView, box)` | `MenuView.FromTexts(strings)` | ready |
| `MessageLogView` | the constructor's `MessageLog` | `new MessageLog()` + `Add(LogPage…)` (the log is not saved) | ready |
| `WeekReportView` | `Open(WeekReport)` | built by `WeekReport.Start`/`Observe` from `MAIN.ADV`'s events | small: a feeder of the week's events (or `WeekReport.Sample`) |
| `CommandMenuView` | `Open(CommandMenu)` | `CommandPhase.MenuItems(state)` opened in the bound presenter, `presenter.CommandMenu` | ready (built in step 2) |
| `StatusStripView` | `Show(StatusStrip)` | `presenter.StatusStrip` (a bound presenter) | ready |
| `ProvinceMenuView` | `Open(ProvinceMenu)` | the city file's items for the state (`OwnCityCommand.MenuItems`, `EnemyCityCommand.MenuItems`) opened in the bound presenter, `presenter.ProvinceMenu` | ready (built in step 2) |
| `MapMarkersView` | `Load(markers)`, `Place(…, corner)` | `presenter.MapMarkers()`; needs the map under it | small: the map backdrop (§2) |
| `FloorLadderView` | `Open(FloorLadder)` | `DungeonCommand.FloorMenuItems(state)` opened in the bound presenter, `presenter.FloorLadder` | ready (built in step 2) |
| `SkipDrawer`, `CommandsButton`, `TouchPill` | properties, `Place` | none (`SkipButtons`) | ready |
| `OptionsView` | `ShowTab` | none — but it writes the real `settings.cfg` | ready, its writes kept off |
| `DeploymentTableView` | `Refresh(state, order)` | `DeploymentTable.Rows(state)` | ready |
| `DeploymentPanels` | `Refresh(state, picker, order)` | reads only the picker's bar labels and mode; `PickerView`'s constructor is internal | small: `Refresh(state, labels, mode)` |
| `SaveLoadView` | `Open(list, items, library, now, last, tabs)` | the library and the state; **it deletes and writes comments** | small: `LoadSaveScreen.Items(list, library)` and a read-only library |
| `EnemyAttackView` | `Show(EnemyAttack)` | `EnemyAttack.Of(state, ninjaName)` on `Prov_Cur`, `Ev_PhaseNation`, string 10 set on a copy | ready |
| `ScoutReportView` | `Show(ScoutReport)` | `ScoutReport.Of(state, ninjaCg)` on `Bt_EnemyParty1-4` set on a copy | ready |
| `StrategyCutInView` | `Show(strategy, backdrop, …)` | the record by hand; a null backdrop allowed | ready |
| `ExchangeCountersView` | `Show(counters, started, instant)` | `ExchangeCounters.Of(state, strike)` or the record | ready |
| `BattleInfoLayer` | `Show(box, waiting)` | the record by hand; real tallies only in a battle | small / heavy |
| `BattleDialogLayer`, `BattleCommandLayer` | `Begin(…)` | records by hand (`BattleDialog`, `FigtSelMenu`); input forwarded by `GameScreen` | small: forward the input |
| `BattleOpeningLayer` | `Refresh(opening, instant)` | `BattleOpening.Of(header, state)` on the `Bt_*` variables | small |
| `BattleBoardView` | `Refresh(presenter, …)` | the presenter's `Scene`, `Order`, `Battle` — event-driven | heavy |

## 2. The data

- **A kept save, read only.** `new DirectorySaveStore(dir).Read(1)` → `data.Restore(new GameState())`, as
  `SaveSlots.Read` does (`SaveSlots.cs:150-157`). Never `DirectorySaveLibrary` on a kept folder: its constructor moves
  `slotNN.ksav` into `legacy/` (`SaveLibrary.cs:146-150`). The records' names are string variables, in the save.
  The saves: `user://saves-week18-command` (below; the default) and, when an entry needs another phase, a kept save
  made the same way from `user://saves-week26`.
- **The gallery's save is taken in `COMMAND`** (found in step 1, the owner's choice the same day): a kept save is the
  turn's start, an auto-save made before the week's banner, upkeep and `COMMAND`, so it still holds last week's uses
  (Replenish showed "used this week"), the budget before the week's income and the troops before the week's
  recovery. `user://saves-week18-command` is the week-18 save played through the week's start and saved on the
  map as a manual save there saves it (decision 26: the game as it is now, resuming in `COMMAND`; it loads and plays
  on) — made, and made again whenever the save format or the boot changes, by
  `<Godot console exe> --headless --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look-command --slots-from user://saves-week18 --save-command user://saves-week18-command`
  (seconds; `GameScreen.SaveCommandTo`). Its figures are the game's own: Haurein (unit 12) at 209/1,400, the budget
  116,400K.
- **A spare presenter** for what reads the scenario's text (harem names, dungeon names, the status strip's lines,
  the battle's text): `new GamePresenter(new ScenarioText(GameData.ScenarioDirectory), GameData.Cgs(), glyphs,
  scripts, audio)` and `Bind(state)`, as `GameScreen.cs:361-369` builds it. No script runs.
- **The map backdrop** (for the plates, the province menu, the status strip, the week report): `presenter.Show(new
  MapEvent(3))` then `MapEvent(0)` — `Bind` marks the tiles valid, so the first forces them to be drawn — then
  `Compose()` and `Screen.ReadScreen()` into a texture (`Compose.cs:76-95`) — works as it is (step 2,
  `GalleryStage.Backdrop`). A picture of `WAO_SUB` 331 over it (the dungeon's) needs the helper's state, `Ui_CenterCg`,
  as well as its `WindowEvent`: the port's `WAO_SUB` only records the picture, the presenter draws it from the state.
- **No event CG in any entry.** Picture panels and portraits are what the looks already show; a 131 window stands on
  a plain backdrop, never over its scene's CG.

## 3. Entries

An entry is one screen in one state: an id, a section (Map, Messages, Screens, Battle, Settings), the save, the
state variation (§4) and how its view is fed. Ids follow the looks' dump names where they exist (`treasure-question`,
`message-thought-right`, `week-report`), so the gallery's picture and the look's compare by name.

- **The part outside Godot** — the variations and the models built from them — goes into
  `src/Kichikuou.Original.Content/ScreenGallery/` (`GalleryStates`, one method a variation), with content tests that each
  variation's state builds its model (the save from `LocalPaths`, skipped when there is none, as the golden tests).
- **The Godot part** — `godot/ScreenGallery/GalleryScreen` (a full-screen Control opened by `Screens.Open`, as
  `CaptureList`) and `GalleryEntries` (a file a section once it grows): the entries' list on the left, the screen on the
  right at 1280×800 fitted, a bar with the palette, the font, touch mode and row heights (the options' own
  choices, applied live), a "made up" tag on an entry whose state the game may not reach as shown.
- **Opening it:** Debug → Screen Gallery…, and `--screen-gallery [id,…]` (`Main.FromCommandLine`, beside `--effects`);
  "← Title" goes back.

## 4. State variations

A variation edits a copy of the save's state; the rule code never sees it. Each says where the game reaches that
state, or is tagged "made up". The first ones are the states the docs record as never looked at:

- `message-thought-right` (no kept save reaches it; messages.md) and a 131 thought's name plate (messages.md, §5 step 2);
- the thieves' week's report (story stage 0; turn-start.md);
- an empty place and several ninjas on the enemy attack (enemy-attack.md, §6); the "no ninja free" state;
- the Treasure screen's stock of many items and the replace question (pickers.md);
- the harem with many girls and its marks (pickers.md, talk-marks.md);
- a short scene menu over a window (messages.md, §5 step 3);
- the battle's level-up row and our panel (messages.md, §5 step 5);
- the province menu's own five items and the dungeon's menu (touch.md);
- Subordinates' refusals greyed (subordinates.md, §2).

## 5. Dump and diff

- `<Godot exe> --path godot -- --screen-gallery [id,…] --shots <dir> [--previous <dir>] --quit` dumps every entry (or
  the listed ones, written into the manifest already in `<dir>`) as `<id>.png` at 1280×800, plus `manifest.json` (each entry's id, section, title, save, note, hash,
  error; the palette, the font, touch). A window is needed (views draw nothing headless); nothing moves the system
  pointer. With `--previous` each picture is compared with the former one of its name: `since` is new, changed or
  same, `differing` the pixels that differ by more than 8 in a channel — changed above 600 —, `box` the rectangle round
  them. While it dumps, our views animate by a clock of frames (`Strategy/UiClock`: it stands at the same time as each
  entry is shown and goes a sixtieth of a second a frame), so the bobbing triangles, the battle's pulsing gauge and
  blinks are caught at the same point: two dumps of the same build differ by 0 pixels (step 4). An entry whose view
  rises into place waits `Settle` seconds of frames first (the exchange's counters). While it dumps, the window takes
  no input (`GuiDisableInput`; the entries' clicks go to the views directly): the real pointer over it dropped the
  hover of `treasure-hover` and `treasure-items` once (2026-10-25).
- `tools/ui/gallery.ps1 [-Steel] [-Touch] [-Only id,…] [-Out dir] [-NoBuild]` builds the game; for each pass
  (Lacquer; `-Steel`, `-Touch` add theirs) it moves `<dir>\<pass>\current` to `previous`, dumps into `current`
  compared with it, and writes `<dir>\index.html`: a tab for each pass dumped so far, one shown at a time (the owner,
  2026-10-25; `#steel` or an entry's `#steel-treasure` opens its pass), a pass not dumped this time marked old on its
  tab and above its pictures; in a pass the changed and new entries first with the former picture beside them, then
  the sections, each once. `<dir>` is
  `-Out`, else `KICHIKUOU_UI_GALLERY_DIR` (`local.env`), else `screen-gallery/` at the root; a folder inside the
  repository that git does not ignore is refused (`git check-ignore`). `-Only` keeps each pass whole (the owner,
  2026-10-25): only the chosen entries' former pictures are copied to `previous`, their new ones replace them in
  `current`, and the game writes them into the pass's manifest (`GalleryScreen.Merged`), the other entries kept as
  they were — so a narrow change dumps its own entries alone and the tab stays complete. The marks add up until a full
  run: an entry an earlier partial run found changed or new keeps its mark and is compared with the picture before
  that change (the script copies no newer one over it), so five fixes dumped one by one show all five, each beside its
  former picture. A change in a
  shared place (a palette, the message windows, the drawing every screen uses) still takes a full run: only it shows
  what changed where it was not meant to.
- After a change to a screen: update its entries, run the script, look at what changed — what was meant and what was
  not.

## 6. The battle

The battle board is drawn from a presenter fed by the battle's events; a state alone cannot draw it. The route is a
golden capture (`KICHIKUOU_GOLDEN_DIR`) replayed by `GoldenReplay.RunAsync` into the spare presenter, held at the
chosen round header or exchange, as `BattleSceneTests` does — deterministic, the original script run headless on the
capture's inputs. This is the last step: the board's looks from the week-18 save (`--until round-N`) do until then.

Built (step 4, `GalleryBattle`): the replay is synchronous (the capture's inputs are answered at once), so one replay
looks at the presenter at each moment — after an event, at a key wait (`ReplayAnswers.Key`), at `FIGT_SEL` (the
`commands` callback) — and at the one an entry picks never shows nor answers the next event: the presenter stays as
it was then. Each entry replays into a presenter of its own (`GalleryStage.Fresh`): the board's pointer moves the
scene's focus, so a shared one would show what the entry before did. A replay takes about a second.

## 7. Steps

1. **The frame and four screens.** The data (§2: the kept save, the spare presenter), `GalleryScreen` with its list
   and bar, `--screen-gallery`, the dump with its manifest, `tools/ui/gallery.ps1` with the index and the diff, `PictureFit`.
   The screens, chosen to try each kind of feed:
   - **Treasure** — the state alone plus a variation (`Ui_ItemViewMode`), the callout's question (`ReplaceQuestion`)
     and a refusal (the script's lines);
   - **Harem** — the spare presenter's names;
   - **Subordinates** with its plate — `SubordinateResult.Preview` pulled out;
   - **the message windows**, every kind, `message-thought-right` among them — `MessageWindow.SampleOf` pulled out.
   Done when the gallery opens from Debug, `gallery.ps1` dumps the four screens' entries and marks a changed one, and
   the time is measured against the `--treasure` look.

   **Built (2026-10-25).** `godot/ScreenGallery/` — `GalleryScreen` (the list, the Palette and Font switches for the
   gallery alone, `UiPalettes.Use`; the dump and the comparison), `GalleryStage` (the kept save read only, the bound
   presenter), `GalleryEntries` (20 entries); `Original.Content/ScreenGallery/GalleryStates` (the variations and the
   message samples, `ScreenGalleryTests`); pulled out: `MessageWindow.SampleOf(kind, lines)` (every kind where
   `WAO_SUB` prints it, `WAO_SUB.ADV:170-250`) and `SubordinateResult.ReplenishPreview`; `tools/ui/gallery.ps1`. The
   entries: Treasure ×6 (`treasure`, `-hover`, `-items`, `-which-item`, `-replace` with our question, `-refusal` with
   `Sｱｲﾃﾑ.ADV:311`'s line), `harem`, Subordinates ×3 (`subordinates`, `-question`, `-result`), the message windows ×10
   (every kind, `message-thought-right` and a read window among them). Two dumps of the same build: all same (198
   pixels at most); Steel against Lacquer: all changed. Not yet looked at by the owner. Not as planned: the bar
   switches the palette and the font only (touch mode is the launch's `--touch`; row heights come with the menus in
   step 2), no "made up" tag yet (no entry needs one: the notes say what is the gallery's own), the tests build their
   states by hand rather than from the kept save, and `PictureFit` stays in `GameScreen` (the gallery fits the picture
   itself).
2. **Map and messages:** the map backdrop, the plates, the command panel, the status strip, the province menu
   (`ProvinceMenu.Of`), the week report (its feeder), the floor ladder, the scene menu, the log, the touch controls,
   the options (writes kept off).
   **Built so far (2026-10-25): the map backdrop and the owner's three screens.** The menus are the ones the files
   register for the save's state: the item lists were pulled out of the rule files as static methods —
   `CommandPhase.MenuItems`, `OwnCityCommand.MenuItems`, `EnemyCityCommand.MenuItems`, `DungeonCommand.FloorMenuItems`
   (and `DungeonCommand.FloorsOf`) — which their files now call (the full differential runs of `COMMAND`, the command
   slices and the dungeon replays pass); the gallery opens them in the bound presenter, which makes our menu of them
   (`presenter.CommandMenu`, `ProvinceMenu`, `FloorLadder`) as in a game, so no `ProvinceMenu.Of` was needed.
   `GalleryEntries.Map.cs`, 10 entries over the map (`GalleryStage.Backdrop`): `map-markers` (the dimmed plates shown),
   `map-markers-hover` (Leazas Castle (1) hovered with its card), `map-markers-active` (the dimmed hidden),
   `command-menu` with the status strip, `command-menu-spent` (made up: the four commands spent at once,
   `GalleryStates.CommandsSpent`), `province-menu` (Oaks (6), the rebels': Attack, Ninja Maneuver; its figures "??",
   unscouted), `province-menu-own` (Leazas Castle (1): Levy, Bolster, Dungeon — the save's castle has no building yet, so
   not the five items touch.md asks about), `floor-ladder`, `-middle`, `-last` (the Monsters' Cave (dungeon 2) on
   floors 1, 7 and 14, its picture 1032 in `WAO_SUB` 331 on black, `EC 0`, and window 113 under it with the gallery's
   own header lines, as the game shows the floor menu (2026-10-26); `GalleryStates.DungeonFloor`), and `dungeon-entry`
   (owner, 2026-10-26: the dungeon's menu before a search, `Cﾒｲｷｭｳ.ADV:238-305` — the picture over the map, window 113
   with "[Monsters' Cave]" and "(Unsearched)", our `]` menu with Start Search and Return at the boot's selection box,
   in the touch pass the rail of docs/ui/dungeon-menu.md, §6) and `dungeon-entry-more` (made up: the Ancient Ruins
   (dungeon 30) with Bring Merim and God's Door).
   Not yet looked at by the owner.
   **Then the scenes' screens** (`GalleryEntries.Scenes.cs`): `week-report` — the presenter gathers it from a Leazas
   week's start fed as `MAIN.ADV:96-212` prints it (`GalleryStates.LeazasWeekStart`: the banner, the save's date, the
   revenue and troops, a donation, the window closed, `Gold` moved as the file moves it); the figures are made up (the
   COMMAND save is past its week's start, and `Tmp0022` is the pointer's by then) — and `week-report-thieves` (the banner
   alone, `ThievesWeekStart`); `scene-menu` (a 121 speech and our menu at the boot's selection window, `B1` of
   `ｽﾀｰﾄ.ADV:860`, which the gallery's presenter is given since it never booted; `scene-menu-top`, the same over a 111
   speech covering its question, was removed: the game never shows it, §8); `message-log` (`GalleryStates.LogSample`: every kind of page, a choice,
   more lines than the paper's 18 rows).
   **Then the touch controls and the options** (`GalleryEntries.Controls.cs`; the touch controls later moved onto
   their screens' entries in the touch pass, step 6): `touch-skip`, `touch-skip-on` (the drawer
   at a 121 key wait), `touch-skip-docked` (beside a 131 bar, the window drawn smaller), `touch-commands` and
   `touch-commands-close` (the map's button, and its Close over the command panel), `options-messages`, `-display`,
   `-battle`, `-game` (with a `MessageSkip` of its own that records nothing; the window writes `settings.cfg` only when
   a control is used). And `province-menu-own-five` (made up: two buildings standing, `GalleryStates.CastleBuildings`,
   so Leazas Castle (1) has the five commands touch.md asks about). **Step 2 is done** (2026-10-25): 45 entries, the
   Lacquer and Touch passes dump them all, two dumps of the same build all "same". Left out: the dungeon's own menu of
   the castle's cells (`Cｼﾊｲﾄｼ.ADV:732-763`, its items built in `CellsAsync`), for a later round. Not yet looked at by the
   owner. Later (2026-10-25): `command-menu-building` (made up: the Military Academy under way, 5 weeks left,
   `GalleryStates.AcademyUnderWay`; docs/ui/command-screen.md, "A building under way") and `command-menu-building-tip`
   (the same with the building's tooltip shown, `StatusStripView.ShowBuildingTip`). And `map`, the map's backdrop alone
   (no plates, nothing of ours), for mock-ups that draw a screen over the map (`tools/ui/mock_touch_panel.py`).
3. **Screens and the battle's overlays:** deployment (the panels' overload), Save/Load (`LoadSaveScreen.Items`, a
   read-only library), the enemy attack, the scout report, the cut-in, the counters, the info boxes, the dialog and
   the commands (input forwarded).
   **Built so far (2026-10-25): the enemy attack and the scout report** (`GalleryEntries.Battle.cs`) on the attack the
   `--scout` run meets: the rebels (nation 2) on Ys (5) with Ex (commander 1), Sakanaku (commander 6), Dodge (commander 5)
   and Jibul (commander 7) — `GalleryStates.EnemyAttack` (`Prov_Cur`, string 8, `Ev_PhaseNation`, string 10 read from
   `TCﾘｰｻﾞｽ.ADV:3`) and `EnemyParty`; Maris's lines, Rance's reply and her comment read from `FIGT_B.ADV` as the presenter
   reads them (`GalleryStage.Text`, `Literal`). Entries `enemy-attack`, `-ninjas` (the list open), `-scouted` (the report
   filled in), `scout-report`. The troops are the COMMAND save's, before the rebels' turn: Ex has none. Left for step 3:
   the Abandon City question (asked by the view's own button, no way in from outside yet), deployment, Save/Load, the
   cut-in, the counters, the info boxes, the dialog and the commands.
   **Then deployment and Save/Load** (`GalleryEntries.Screens.cs`). The deployment picker is opened in the bound presenter
   (`PickerScreenEvent`) for its bar's labels and mode, as `Sﾌﾞﾀｲ` opens it, so `DeploymentPanels` needed no overload;
   `GalleryStates.Deployment` sets the places limit, the party and the details' unit. Entries `deployment` (the week-18
   look's party against Oaks (6), Rance's details), `-picking` (two picked, no details), `-defence` (mode 2, Plains /
   City), `-search` (mode 3, one unit), `-refill-question` (the callout at Haurein's (unit 12) Refill cell, laid after the
   table's layout, `GalleryStage.AfterLayout`) and `-refill-done` (the plate, `ReplenishPreview`; the table under it not
   replenished). The Save/Load screen reads a `MemorySaveLibrary` made of the kept save
   (`GalleryStates.SampleLibrary`: ten Turn Start and ten Turn End auto-saves (decision 31, the newest Turn End with a
   comment) and four manual saves, the older a week earlier each — `Week` and
   `Tmp0021`, the date the lists read —, two with comments, the manual saves an hour after their week's auto-save), so
   its deletes and comments reach no disk and no read-only library was needed; its items are `LoadSaveScreen.Items(library,
   save)`, pulled out of the file (the full load/save differential runs pass). Its clicks are given to it as input
   (`GalleryStage.Click`: the view's `_GuiInput`, the system pointer not moved). Entries `save-load-save`, `save-load`
   (the save written last), `-title` (no tabs), `-auto`, `-auto-end` (the newest Turn End), `-delete`, `-sweep`, `-comment` (Edit on the uncommented No. 3,
   its caret not blinking).
   **Then the cut-in, Abandon City and the cells.** `strategy` (Barres (unit 6), `FIGT作.ADV:37`'s announcement and his
   line `:80`) and `strategy-enemy` (Ex (commander 1), a great success, `:378` and `:392`), the record built as the
   presenter builds it, no field under it; `enemy-attack-abandon` (the view's own `Choose(3)` asks it, so no way in was
   needed); `province-menu-cells` and `-cells-full` (made up: the four prisoners held at once, `GalleryStates.PrisonersHeld`),
   the items `OwnCityCommand.CellItems` and `CellsReturn`, pulled out of `CellsAsync` (the full command-slice and
   command-phase differential runs pass). **Moved to step 4:** the exchange's counters, the info boxes, the retreat dialog
   and the battle's commands are layers of `BattleBoardView` (`Info`, `Dialog`, `Commands`, the counters inside it) fed by
   the presenter: alone over a black stage they would show nothing of their place, so they come as states of the board.
4. **The battle board** from a replayed capture (§6).
   **Built (2026-10-25).** `GalleryEntries.Board.cs` on `20261010-213758-114-FIGT_A` (an attack on Oaks (6) of story
   stage 1, four rounds, Rance (unit 1) and Rick (unit 7) against Ex (commander 1), Sakanaku (commander 6), Melfeis (commander 4)
   and Dodge (commander 5)): `opening` (the first header's key wait), `battle-turn` (round 1's first `FIGT_SEL`, our
   commands begun on its menu as `GameScreen` begins them), `menu` and `menu-orders` (a right click on the acting card
   and on the other, given to the commands as the pointer's), `order-toggles` (made up: the other unit set to Auto-Def;
   in the touch pass its Auto-Atk toggle tapped, the choice checked, and held — docs/ui/touch.md, "The standing orders on
   the cards"; in the PC pass the turn as `battle-turn` with that order), `banners-E1` (the pointer on the first enemy card),
   `retreat` (made up: the dialog over that turn, its menu `Battle.RetreatMenu`, pulled out of `PlayerRetreatAsync`),
   `exchange-counters` (the first exchange's result held), `info-panel` (a fall), `info-results` and `info-level-up`
   (the tally and Rance's level-up page); `info-defence` on `20261010-213435-25-FIGT_B`, a defence with ten (the attack
   has none). The board is followed every frame as `GameScreen.UpdateBattleScreen` follows it, slides instant. Found on
   the way: `ExchangeCountersView.Layout` sized its digits' picture before their port, so a single layout left the
   digits 102 pixels too tall (the game lays the board out every frame and hid it); fixed. The `UiClock` (§5) made every
   dump repeatable. A full Lacquer pass, 78 entries, takes ~33 s. Not yet looked at by the owner.
5. **The house style:** docs/ui/style.md, §7 gains "a changed screen updates its entries and the gallery is rebuilt";
   CLAUDE.md's commands list the gallery.
   **Built (2026-10-25):** style.md §7's step 4 makes a screen's entries part of its build and says that a changed
   screen updates its entries and the gallery is dumped again; CLAUDE.md's commands list the gallery with the board.
6. **The owner's look** (2026-10-25), entry by entry:
   - `enemy-attack` (and its `-ninjas`, `-scouted`, `-abandon`): the date in the title band was too small — Cinzel 600
     at 13 in the subtitle's colour; now Cinzel 700 at 15 in the name's colour, as the title bands of Subordinates,
     Harem, Treasure and deployment have it (`EnemyAttackView.DrawTitle`). The other dates differ on purpose and stay
     as they are (the owner): the status strip's (18, the map's top), the week report's (17, under its banner's title) and the
     Save/Load screen's (the chosen save's title; "Saving:" in the foot's Gothic).
   - `info-defence` showed no defence at all: the gallery gave the box to the info layer's `Show`, which draws only the
     results and our panel, while the game flashes a defence (`Flash`). Now the board flashes it as `GameScreen` does and
     the entry settles 0.35 s, the ribbon at its height. Found by the owner's remark on the touch mode's ribbon, whose text
     ran together on the narrower card (docs/ui/touch.md, "The battle within the thumbs' reach").
   - The Touch tab's `map-markers` (and `-hover`, `-active`), `command-menu` and `-spent` lacked the Commands button the
     touch mode shows on the map and, as Close, over the command panel: only the Touch section's own entries drew it. Now
     the map's entries draw it in the touch pass as `GameScreen.UpdateMapMarkers` does (`GalleryEntries.TouchCommandsButton`).
   - The Touch section is gone (the owner: a Touch section in the Touch tab has no logic). It came from step 2, before the
     passes were tabs, to show the touch controls in a Lacquer dump; with the tabs it showed the PC controls it never has
     and, in the Touch tab, what belongs on the screens. Now each pass is what its device shows: the Lacquer tab the PC
     (72 entries), the Touch tab the tablet (73) — the Commands button on the map's entries (above), the Skip and Log drawer
     on every message window's entry at its key wait, docked beside 131, as `GameScreen.UpdateSkipButtons` shows it
     (`GalleryEntries.MessageWindowOf`), and `touch-skip-on`, the drawer with MsgSkip on, a Messages entry of the touch
     pass alone. `touch-skip`, `touch-skip-docked`, `touch-commands` and `-close` were dropped as repeats.
   - Skip and Log in the Touch tab sat too close together: the owner chose G3 of the mock-ups, two plates 16 px apart
     (docs/ui/touch.md, "Round G"); every message window's entry and `touch-skip-on` show them, 131's without its dock.

## 8. Questions for the owner

- ~~Dump both palettes and the touch mode by default, or Lacquer alone with the others on request?~~ Lacquer alone,
  `-Steel` and `-Touch` on request (owner, 2026-10-25; as built).
- ~~The gallery in the release build's Debug menu too, or development runs only?~~ Wherever the Debug menu is, the
  release included (owner, 2026-10-25; as built).
- ~~The scene menu over a 111 speech covering its question (`scene-menu-top`)?~~ The entry removed (owner,
  2026-10-25). **Never reached** (checked 2026-10-25,
  a walk of every `]` menu of the decompile, 188): no menu follows text in 111 or 112. A menu comes over text only
  in 113 (narration, at the screen's foot: the merchant, the own city, construction, `Cﾃｷﾄｼ`, the dungeon, three
  scenes, `ﾛｰﾄﾞｾｰﾌﾞ`; 26), 123 (3), 121 (`Cﾌﾞｶ`, 2) and the battle's 143 (1); in the scenes a question is
  mostly asked, its key awaited and its window closed before the menu (152). So the entry shows a state the game
  never shows.
- ~~A kept save taken in `COMMAND` for the gallery (§2)?~~ Yes (owner, 2026-10-25): `saves-week18-command`.
