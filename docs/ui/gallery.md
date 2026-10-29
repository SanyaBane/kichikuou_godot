# The screen gallery — our screens without playing to them (plan and build, 2026-10-25; steps 1-5 and 7 built)

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
| All Data (`各種DATA`) | the presenter's own drawing (`DataScreenEvent`) and `SceneMenuView` | the state; the menus' items by the file's lines | ready (step 7) |
| `ImportScreen` | `ImportScreen.Held(font, folder, …)` | none: a phase, a folder, a refusal's message (`GameFiles.MissingFile`, `OtherFile`) | small (step 7) |
| the title (`ｽﾀMENU`) | the presenter's screen; `MessageWindowView` at a key wait | the boot and the title run from the player's files (`GalleryTitle`) | heavy (step 7) |

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

An entry is one screen in one state: an id, a section (Map, Messages, Screens, Battle, Settings, Title), the save, the
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
- `tools/ui/gallery.ps1 [-Steel] [-Touch] [-Vfx | -VfxOnly] [-Only id,…] [-Out dir] [-NoBuild] [-IndexOnly]` builds the game; for each pass
  (Lacquer; `-Steel`, `-Touch` add theirs; `-Vfx` the Combat VFX tab, only when asked for, §11) it moves `<dir>\<pass>\current` to `previous`, dumps into `current`
  compared with it, and writes `<dir>\index.html`: a tab for each pass dumped so far, one shown at a time (the owner,
  2026-10-25; `#steel` or an entry's `#steel-treasure` opens its pass), a pass not dumped this time marked old on its
  tab and above its pictures; in a pass the sections, each entry in its own place at full size whatever changed (the
  owner, 2026-10-26: a screen should be where it always is, not jump to the top after a commit) — a changed one carries
  a small badge with the pixels that differ, which opens the former picture, a new one a badge alone (until then the
  changed and new entries came first, each beside its former picture at half size). `<dir>` is
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
- **The page** (the owner, 2026-10-29: items 1-4 of the agent's proposals, the remarks left out). Its style and script
  are `tools/ui/gallery-index.css` and `gallery-index.js`, inlined into `index.html` (the page also works as a data:
  URL; nothing it needs lies beside it). The pictures load as they scroll into view, each `<img>` with its size from
  the PNG's header so the grid keeps their places (before: all ~490 pictures of every tab at once, the hidden tabs'
  too). A bar under the tabs: a find over the entries' ids, titles and notes (`/` focuses it, Esc clears it),
  "Changed and new" (offered when some tab has them), the shown tab's sections to jump to (a section the find leaves
  empty greyed), and the pictures' size (half to double, kept in the browser). A picture's click opens the viewer, a
  changed badge's click opens it on the former picture: ←/→ the tab's entries the find leaves, ↑/↓ or the tabs' buttons
  the same entry in the other tabs, Space the former picture and back (a BEFORE label on it), B the box round what
  changed (the manifest's `box`, dashed, the rest dimmed a little), Esc or a click beside the picture closes it at the
  entry last shown. The address follows the entry (`#touch-treasure-replace`); opened with one, the page scrolls to it
  under the bar. A click with a key held, or the middle button, still opens the picture alone.
  `gallery.ps1 -IndexOnly` writes the page alone from the pictures and manifests as they are (no build, no pass), the
  tabs' "old" marks as the last run that dumped left them (`stale-passes.txt` beside the index).
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
   Later (2026-10-27, the strip always on the map; docs/ui/command-screen.md, "The map's room under the strip"): the
   plates' and the province menus' entries show the status strip too, and `map-north` the room at the map's top — the
   camera at the Great Wall of Helman A (49) (`GalleryStates.MapNorth`), the backdrop drawn lower by the strip's height.
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
   replenished). Since the ledger's tabs (docs/ui/unit-ledger.md, §6) these and the Subordinates entries pin Overview,
   and `deployment-troops`, `deployment-commander` and `subordinates-commander` show the other tabs. The Save/Load screen reads a `MemorySaveLibrary` made of the kept save
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
   commands begun on its menu as `GameScreen` begins them; the damage forecast on, which the touch pass's rail shows —
   docs/ui/damage-forecast.md, §8; every other entry has it off, whatever the option), `menu` and `menu-orders` (a right click on the acting card
   and on the other, given to the commands as the pointer's), `order-toggles` (made up: the other unit set to Auto-Def;
   in the touch pass its Auto-Atk toggle tapped, the choice checked, and held — docs/ui/touch.md, "The standing orders on
   the cards"; in the PC pass the turn as `battle-turn` with that order), `banners-E1` (the pointer on the first enemy card,
   the damage forecast on, its figures waited for),
   `retreat` (made up: the dialog over that turn, its menu `Battle.RetreatMenu`, pulled out of `PlayerRetreatAsync`),
   `exchange-counters` (the first exchange's result held), `battle-speech` and `battle-speech-enemy` (the first line in
   our speech balloon of a player unit, of an enemy commander; 2026-10-26 — the capture has no thought) and their
   `-read` (made up: every message read before, docs/ui/read-text.md §7), `info-panel` (a fall), `info-results` and `info-level-up`
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
   - The drawer shows on the waiting map too (2026-10-26, the owner's L1; docs/ui/touch.md, round L): the Touch tab's
     `map-markers`, `-hover` and `-active` draw it beside the Commands button, Log lit as with something logged
     (`GalleryEntries.Markers`).
7. **All Data, the title and the import screen** (owner, 2026-10-28: the three screens with no entry, so that the
   phone's mock-ups can be drawn over them; docs/ui/phone.md, §19). **Built (2026-10-28)**, 18 entries in every pass
   (the Lacquer pass 107, the Touch pass 108); two dumps of the same build differ by 0 pixels. Not yet looked at by the
   owner.
   - **All Data** (`GalleryEntries.Data.cs`, section Screens, the week-18 `COMMAND` save): a presenter of the entry's
     own draws the screen by hand as in a game (`DataScreenEvent` over the map `COMMAND` left on the stage, `M` 3 and
     0 first), the scales as the file's first visit sets them (`GalleryStates.AllDataOpened`, `各種DATA.ADV:12-19`),
     our `]` menu at the boot's selection box. Entries `all-data` (the map in miniature and the menu of the four
     graphs), `all-data-funds`, `-sundry`, `-military`, `-casualties` (each graph at 16x) and `all-data-scale` (the
     scale menu a right click opens over All Funds). The menus' items are the file's registered at its lines
     (`GalleryStates.AllDataMenu`, `AllDataScaleMenu`; `ScreenGalleryTests` reads their texts), not pulled out of
     `AllDataCommand`, so no rule code changed. The save's history is the game's own: every week before this one holds
     last week's figures (STR-01), so those bars are level.
   - **The import screen** (`GalleryEntries.Title.cs`, section Title, no save): `ImportScreen.Held` draws one phase
     as an import draws it, in the gallery's font, with no import run, no input taken and nothing opened after it; the
     folder is the gallery's own (`C:\Games\Kichikuou Rance`), never the player's, and nothing reads a disk. Entries
     `import-none` (the first launch, no folder chosen), `import-choose` (a folder chosen; the unattended look's
     `--import-screen DIR` dumps the same name), `import-back` (from Option → Game, its way back), `import-progress`
     (made up: the bar held at 45% of the decompile), `import-done`, `import-refused` (the wrong folder) and
     `import-refused-version` (made up: an empty KICHIKUSA.ALD, its hashes on the small line). The refusals' wording is
     `GameFiles`' own, pulled out as `GameFiles.MissingFile` and `OtherFile` (the check calls them), so the entries
     show the check's messages for a folder that is not there. Found by `import-refused-version`: the hashes' line is
     not wrapped and runs past the paper and the frame (docs/backlog.md, "Interface").
   - **The title** (`GalleryTitle`, `GalleryEntries.Title.cs`, section Title, no save): a fresh launch's boot and
     `ｽﾀMENU` run from the player's files through `GameSession` into a presenter of the entry's own, as the game opens
     the title — the logo's pauses answered at once, the pointer a `PointerScript`, the draws given — and held where an
     input is never answered: at the title's mouse poll once the pointer has rested a few reads (its hover drawn), or at
     the key wait after a gesture. The presenter masks the event CGs as the agent's dumps do (nothing is masked on the
     title) and leaves the windows to ours; in the touch pass a key wait shows the Skip and Log drawer, Log off (nothing
     is logged at the title). Entries `title` (a first launch: Alice's Mansion, Load, Start), `title-hover` (Start
     hovered), `title-clears` (made up: a clear and both rolls at their top at once, every button drawn),
     `title-description` (a right click on Start: its description in our window 113) and `title-requests` (the
     requests slip after a clear, its second request of the first five, at its `IK 0`). The title's load list is
     `save-load-title` (step 3). Left out: Alice's Mansion (`Iｱﾘｽ`) and Saitou (`Gｻｲﾄｳ`), which are scenes, and the debug
     starts' menu (Alice's secret only).
   - **The title's own screens** (2026-10-28, the phone's step 0, docs/ui/phone-build.md §5; section Title, no save): the
     screens three of the title's buttons run from the player's files, each reached by a click on its button after a
     clear (`GalleryTitle.FortuneCheck`, `MusicRoom`, `CgGallery`) and held at its mouse poll, the pointer then moved
     off what the screen lights (a hover queued after the click: a `Move` would be overwritten by the click's reads).
     Entries `fortune-check` (made up: three fates seen, `GalleryStates.FatesSeen`; the first page with their stamps),
     `cg-gallery` (page 1; its sheets masked as every agent's dump masks them, so only the grey placeholder shows) and
     `music-room` (no track playing). The other entries stayed the same in both passes (now 110 and 111).

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

## 9. The phone's mock-ups (owner, 2026-10-27)

The owner's request: the phone's mock-ups already chosen (docs/ui/phone.md) in the gallery too, on a tab of their own,
marked as not built. `gallery.ps1 -Phone` adds the tab **Smartphone, upright (mock-ups)**; `-PhoneOnly` draws it and the
index alone, with no build and no game pass (the other tabs left as they were, not marked old). They are not dumps:
`tools/ui/mock_phone.py --gallery <gallery dir> <extract dir>` draws the owner's chosen base versions — the map PM1 (its
wait, the command panel, a province), the battle PB3 (the player's turn, the retreat question, an exchange, and round
P7's scout report, strategy, results plate and fighter's line, round P16's opening PB4o3) and rounds P3-P6's choices
(2026-10-27: the deployment table PD2, Subordinates PU1 — both since round P16 with its ledger and End —, the enemy
attack PA1, the week report PW1, Harem PH2, Treasure PT2, Save/Load PL2, the options PO1 with round P17's Display tab
PO1o, the dungeon's ladder PF1; round P18's All Data PG2, the title PE2 and the import screen PI, 2026-10-28; round P20's
Menu sheet PN1 with PN1q, the log PR2, the comment's editing PY2 and the original's own screens PXf, PXc, PXm, PXr) — from
the touch and lacquer passes' current pictures and the map's own pictures, without the mock-ups' pink notes, each in a
phone's body, into `phone/current` with a `manifest.json` as a pass writes it (`"mockup": true`). Each drawing is compared
with the former one as a dump is (`GalleryScreen.Compare`: a channel more than 8 apart, changed past 600 pixels), so a
mock-up that changes, or one whose source dumps changed, gets its badge. The tab's heading says **Not built yet**; its
grid is narrower for the phone's tall pictures. The Python is `KICHIKUOU_PYTHON` in local.env (one with Pillow and
numpy), else `python`. The scenes (upright PS2, landscape LS1; rounds P14-P15, 2026-10-28) stand on the week-18 look's
picture of Ex's scene, which no pass dumps: its key waits `00058-message-speech-left.png`, `00059-key.png` and
`00060-key.png` (the look in a window, our interface, without `--touch`, `--until battle`; CLAUDE.md, "A look at one
screen") are copied into `phone-look` beside the passes, and without them the run leaves PS2, PS2l and LS1 out (and
PN1, PN1q, PR2, LN3, LR2, drawn over them) and says so. PXr and LXr need `ending-report.png` there too: frame `00002` of
`<Godot console exe> --headless --path godot -- --ending 11 --auto --fast --quit --shots <dir>` (an ending's
Completion Report, 640×400). When the owner chooses another base version, its entry goes into the script's `GALLERY_ENTRIES`;
when a phone screen is built, its mock-up leaves the tab for a pass of its own.

The landscape phone's choices (from round P8, 2026-10-27: the map LM1, the battle LB2, the deployment table LD2,
Subordinates LU1; round P19's All Data LG2, the title LE1 and the import screen LI, 2026-10-28; round P21's Menu sheet
LN3, the log LR2, the comment's editing LY1 and the original's own screens LXf, LXc, LXm, LXr) are a second tab,
**Smartphone, landscape (mock-ups)**, drawn by the same run into `phone-land/current` (`GALLERY_ENTRIES_LAND`), its grid
wider for the wide pictures; `-Phone` and `-PhoneOnly` draw both tabs.

The phone's real pass (step F, 2026-10-28; docs/ui/phone-build.md, "Step F, where it stands"): `gallery.ps1 -PhoneLand`
dumps every entry as the phone held landscape (`--phone landscape --window 1808x813`) into `phone-landscape/current`,
the tab **Smartphone, landscape** — the whole window at two thirds of the reference phone's pixels, its black sides
with it, not cut to the picture. Until a screen's phone layout is built its entry shows the tablet's layout in the
phone's frame; the mock-up tabs stay as they are, and a built screen's mock-up leaves them. Step L1 (2026-10-28) built
the scenes' layouts: the entries of the message windows, the scene menu, the week report, the message log, the map's
wait, All Data and the title show the left side's Menu cell (`GalleryEntries.MenuCell`) and the drawer in it, and the
pass alone has three Messages entries of its own, the Menu sheet open (`menu-sheet`), Restart's question
(`menu-sheet-restart`) and Debug's page (`menu-sheet-debug`; `GalleryEntries.Phone.cs`); LS1, LS1b, LS1m, LW1, LEd,
LN3 and LR2 left the mock-up tab. Step L2 (2026-10-28) built the map's: on the phone pass the map's entries show the
status in the sides (`StatusStripView` in the mode of what is open: the map's wait, a province, the command panel) and no
room under a strip (`map-north` shows the map's top edge as it is), the province and cells entries the Menu cell, and
`command-menu-building-tip` shows the tooltip on the map's wait, where the right side holds the week's tags; LM1a, LM1b
and LM1c left the mock-up tab. Step L3 (2026-10-28) built the battle's turn: on the phone pass the battle's entries show
the scene at 70% with the taller cards, the rails in the sides (`battle-turn`, `menu`, `order-toggles`, `banners-E1`),
the battle's Skip on the left rail's top (the entries add it, as the game's `TouchPill`), the retreat question beside
Retreat (`retreat`) and the exchange's counters in the scene's blurred sides (`exchange-counters`); LB2, LB2r1 and LB2x
left the mock-up tab. Step L4 (2026-10-28) built the battle's other states: the opening at the lower scene band (`opening`),
the scout report across the whole width (`scout-report`), the cut-in's band on across the sides (`strategy`,
`strategy-enemy`), the results plate and our panel in the frame's middle over the dimmed board (`info-results`,
`info-level-up`, `info-panel`), the ribbon and the caption at the phone's type (`info-defence`), the balloon over the
scene's foot (`battle-speech…`) and the cards' menu at a finger's size (`menu`, `menu-orders`); LB3o1, LB3s, LB3c, LB3r
and LB3b left the mock-up tab. Step L5 (2026-10-28) built the deployment table and Subordinates: the ledger in one-line
rows at the phone's type, wholly at the left with the party two cards a row at the right (`deployment…`, the Refill
question `deployment-refill-question` at the phone's sizes), the orders in the sides with Replenish's question left of its
button (`subordinates…`, `subordinates-question`); LD2, LU1 and LU1q left the mock-up tab. Step L6 (2026-10-28) built
the enemy attack: across the whole width, the province and the army at the left, the column with the news, Maris's
question and the answers at the right thumb (`enemy-attack`, `enemy-attack-scouted`), Ninja Scout's list above its button
(`enemy-attack-ninjas`), Abandon City's question above its (`enemy-attack-abandon`); LA2 and LA2n left the mock-up tab.
Step L7a (2026-10-28) built Harem and Treasure: across the whole width, the title, the picture and the details at the left
with the buttons at the left thumb, the grid folded after its ninth row at the right thumb (`harem`, `treasure…`), the
treasure's questions and refusals in the callout left of its button (`treasure-question`, `treasure-replace`), a first
pick's prompt and Back in the tabs' place (`treasure-which-item`); LH2 and LT2 left the mock-up tab. Step L7b (2026-10-28)
built Save/Load: the list at the left in one-line rows, the chosen save's page at the right with the map cut lower, Back
| the red button at its foot (`save-load…`), Delete's and the sweep's questions at them (`save-load-delete`,
`save-load-sweep`), the comment's own card where LY1 has it over the keyboard (`save-load-comment`); LL1 and LY1 left the
mock-up tab. Step L8 (2026-10-28) built the options window and the dungeon's menus: the paper sheet across the whole width,
its tabs on top, each tab in two columns, Defaults | Close at the right column's foot (`options-…`); the tablet's rail
kept in the frame at the phone's sizes (`floor-ladder…`, `dungeon-entry…`); LO1, LF2 and LF2e left the mock-up tab. Step
L9a (2026-10-28) built All Data: the miniature uncovered, the four graphs as cells under Menu in the left side, Back in
the right (`all-data`); over a graph its figures drawn again at the phone's type and the scale as eight chips over Back,
the shown graph and scale lit (`all-data-funds`, …, the scale menu `all-data-scale` the same); the title (`title…`) and
the original's own screens (`fortune-check`, `cg-gallery`, `music-room`) were already as LE1 and LXf-LXm have them;
LG2m, LG2, LE1, LXf, LXc and LXm left the mock-up tab (LXr stays: the ending's report has no entry here). Step L9b
(2026-10-28) built the import screen: the paper sheet in the frame at the phone's type, the steps in two columns, the red
button at the right thumb, Back at the sheet's left foot or, refused, left of the red button, on a folder in Android's
storage (`import-…`); LI, LIp and LIr left the mock-up tab.

The phone held upright's real pass (step P0, 2026-10-29; docs/ui/phone-build.md, "Step P0, where it stands"):
`gallery.ps1 -PhoneUp` dumps every entry as the phone held upright (`--phone upright --window 610x1356`) into
`phone-upright/current`, the tab **Smartphone, upright** — the whole window at **half** the reference phone's pixels
(two thirds would be 1808 high, more than a 1440-pixel monitor holds), its grid as narrow as the upright mock-ups'. Until
a screen's upright layout is built (P1-P8) its entry shows the tablet's layout at the picture's width between the hole
and the bar; each built screen's mock-up then leaves `GALLERY_ENTRIES` as the landscape ones left `GALLERY_ENTRIES_LAND`.
Step P1 (2026-10-29) built the scenes: the message windows over or under the picture (`message-…`, the samples with both
portraits shown, so the left ones stand above it), `scene-menu`, `message-log`, `week-report`, `title-description`, and
the Menu cell in the bar on the entries that show it; the Menu sheet's three entries (`menu-sheet…`) are on this pass
too. PS2, PS2l, PS2b, PS2m, PW1, PEd, PEr, PN1, PN1q and PR2 left `GALLERY_ENTRIES`.

Step P2 (2026-10-29) built the map: on this pass the map's entries (`map-markers…`, `map-north`, `command-menu…`,
`province-menu…`) show the status band on top and the map's whole height under it (`UprightMapView`, centred on the
script's view as the game centres it), the command panel and a province's menu as sheets over its foot; `map` stays the
engine's picture alone, for mock-ups. PM1a, PM1b and PM1c left `GALLERY_ENTRIES`.

Step P3 (2026-10-29) built the battle's turn: on this pass the board's entries (`battle-turn`, `menu…`, `order-toggles`,
`banners-E1`, `retreat`, `exchange-counters`) show the scene at the width on top, our rows under it, the gauge across,
the enemy's rows at the thumb, Skip on its strip and the deck at the foot; the board's other states (`opening`,
`battle-speech…`, `info-…`) changed with it and stand there as the PC's at the board's scale until step P4 lays them out.
PB3, PB3r and PB3x left `GALLERY_ENTRIES`.

Step P4 (2026-10-29) built the battle's other states: on this pass `opening` shows the plate on top of the scene and the
armies' names at its sides, `scout-report` Kanami's report as a screen of its own, `strategy` and `strategy-enemy` the
cut-in stacked round the room's middle, `info-results` and `info-level-up` the results plate over the dimmed board at the
screen's width, `info-panel` our panel where the plate stands, `info-defence` the ribbon and the caption at held
landscape's millimetres, `battle-speech…` the portrait at the scene's top corner and our balloon beside it, and `menu`,
`menu-orders` the cards' menu at a finger's rows. PB4o3, PB4s, PB4c, PB4r and PB4b left `GALLERY_ENTRIES`.

Step P5 (2026-10-29) built the deployment table and Subordinates: on this pass `deployment…` show the title band, the
party on top (the back row, then the front row), the tabs and the ledger at the thumb in two-line rows (the seal, № and
the name over the type fixed at the left, the tab's other columns beside them), Stop | the sends in the bar (`-defence`
Back | Plains | City, `-search` one place), `-refill-question` the Refill question at the phone's sizes, `-refill-done` the
plate at the screen's width, `deployment-commander` the order's seal on the subtitle's line; `subordinates…` the ledger
over the orders (Replenish and Talk on one row, Add New Troops three a row), the budget at the bar's left and End at the
right thumb, `-question` Replenish's question under its button, `-result` the plate at the screen's width. PD2, PD2d, PU1
and PU1q left `GALLERY_ENTRIES`; the long press's picture (PD2d) has no entry, as held landscape's has none.

Step P6 (2026-10-29) built the enemy attack: on this pass `enemy-attack…` show the PC's screen stacked — the head with the
news, Ys's picture with its figures beside it, "VS", the rebels' cards two a row with the front row on top, Maris's
question, Abandon City | Ninja Scout over Intercept in the bar —, `-scouted` the places filled, `-ninjas` the list above
Ninja Scout, `-abandon` the question above Abandon City. PA1 and PA1n left `GALLERY_ENTRIES`.

Step P7a (2026-10-29) built Harem and Treasure: on this pass `harem` shows the title band, Anasel's picture at the width,
her details, the grid in rows of 7.3 mm (nine of 17 in view, a thin scroll bar at the right) and End | Summon in the bar;
`treasure…` the item's or the character's picture at the width, its details, Characters | Items (`-which-item` Rance's
prompt and Back in their place), the grid in rows of 6.7 mm, End | the red button (`-refusal` greyed with its reason),
`-question` and `-replace` the questions across the room over the button. PH2 and PT2 left `GALLERY_ENTRIES`.

Step P7b (2026-10-30) built Save/Load: on this pass `save-load…` show the title band with Load | Save at its right
(`-title` without them), the lists' tabs, the heading with the sweep, the list in one-line rows above the page (nine in
view, the scroll bar at the right), the chosen save's page standing on the bar (its title with Delete, the map cut at its
foot, the figures in two columns, the comment with Edit) and Back | the red button in the bar; `-auto` and `-auto-end` an
auto-save's two lines under the comment, the map cut lower; `-delete` the question under Delete, `-sweep` the sweep's
under the sweep; `-comment` the comment's own card where PY2 has it (the keyboard not drawn), its words wrapped in the
field two lines high. PL2 and PY2 left `GALLERY_ENTRIES`.

Step P8a (2026-10-30) built the options window: on this pass `options-…` show the title under the hole, the five paper
tabs across the room, the sheet down to the bar with the tab's settings in one column at PO1's type and Defaults | Close
in the bar; `-display` the screen's orientation on top as PO1o's rows, then the font, the palette, the thousands one under
another and the unit tables' row heights two a row, cut at the sheet's foot with the thin scroll bar at its right (the
column scrolls by a finger). PO1 and PO1o left `GALLERY_ENTRIES`.

## 10. The copy for a website (owner, 2026-10-29)

The owner's request: the gallery on the owner's website, its pictures in a light format and the current ones alone, the
gallery itself unchanged. `powershell -ExecutionPolicy Bypass -File tools\ui\gallery-deploy.ps1 [-From <gallery>] [-Out <folder>] [-Quality n]`
reads the gallery (found as `gallery.ps1` finds it) and never writes it: it makes a new folder named by the time,
`screen-gallery-deploy\yyyy-MM-dd_HH-mm-ss` at the root (git ignores it; `-Out` gives another parent), with the page and
`<pass>\<id>.webp` for every picture the gallery's `index.html` shows. Each picture is the smaller of lossy WebP at
quality 85 and lossless WebP (`tools/ui/gallery_deploy.py`, Pillow on `KICHIKUOU_PYTHON`). The owner chose WebP over
JPEG (artefacts round small type, larger than the PNG on the flat screens) and AVIF (small type softened at a low
quality, no Safari before 16); lossless wins on the screens drawn on the original's 256-colour pictures and the flat
ones, whose lossy copy came out larger than the PNG (the title, Fortune Check, the music room). Measured (2026-10-29):
458 pictures, 242 MB of PNG to 55 MB of WebP (138 lossless) in ~35 s.

The page is the gallery's own, written again: the pictures in WebP, loaded as they scroll into view (each `<img>` with
its size, so the grid keeps their places), and nothing of the former pictures — `previous` is not copied, the changed
and new badges, the tabs' counts, the passes' "old" marks and the viewer's data on what changed (`data-since`, `-box`)
are left out, so its viewer pages and finds but offers no former picture and its bar no filter of changes; a link to a
picture the script did not copy fails the run (the page's script holds no double quote, so the check never reads it).
A page older than the viewer (§5) is refused: `gallery.ps1 -IndexOnly` writes it again. The manifests and `phone-look` are not copied either. The pictures hold the original's CGs as the
gallery's do (no event CG, §3).

## 11. Combat VFX (owner, 2026-10-29)

A tab of its own, **Combat VFX**: each exchange effect play shows, as six of its frames in a 2×3 grid (640×256 a frame,
half our scene's 1024×410) under a strip naming the effect and its armies, the frame's number in each corner — the
owner's B of the sheets drawn that day (one middle frame, A, shows little of most: melee's slashes are gone by frame 15,
the Black D. Beam's orb is not there yet). The frames are 2, 6 … 22 of 30, the part before the tail that the message skip
shows too (docs/ui/battle-screen.md, "Under the message skip"); 1, 3 … 11 for the short Guard (14) and T2 Goliath (15).
Seed 1, the player's side; the unused Ninja troop left out: 28 entries, by group (Attacks, Magic, Specials, Ninjas,
Defence). `GalleryEntries.CombatVfx`, each frame an `EffectStage` (the viewer's), no kept save needed.

**Only when asked for** (owner, 2026-10-29: the effects hardly ever change): `gallery.ps1 -Vfx` adds the pass, `-VfxOnly`
dumps it alone (`--screen-gallery --combat-vfx` in the game); a run without it leaves the tab as it was, never marked
old. In a session with the agent that means only when the owner asks for it or the tab itself changes. The gallery
looked at (Debug → Screen Gallery…) lists both. Two dumps of the same build are the same (0 pixels, 2026-10-29).

The armies, the owner's table of 2026-10-29 (`EffectCatalog`, shared with Debug → Effects, which offers the others):
the enemy's is Ex's (commander 1) Leazas Knights (CG 1131) throughout but the poison's.

| Effect | Left (the player's side) | CG |
|---|---|---|
| Melee, Guard | Rance (unit 1), Leazas Knights, as the rebellion makes them (`Iﾊﾝﾗﾝ.ADV:27`) | 1100 |
| Bow | Isoroku (unit 27), JAPAN Archers | 1209 |
| Magic (fire) | Sill (unit 2), Leazas Mages | 1125 |
| Ice Magic | Melfeis (unit 13), Leazas Mages | 1125 |
| Lightning Magic | Somita (unit 46), Kalars (the viewer: Eleanor (unit 23), Hanty (unit 19)) | 1450 |
| Dark Magic | Satella (unit 29), Guardians (the viewer: Chizuko (unit 38), Miki (unit 32)) | 1316 |
| Light Magic | Seizel Sisters (unit 42), False Angels (the viewer: Hanny King (unit 48)) | 1514 |
| Poison Magic | Rance (unit 1), Leazas Knights; cast by Papaya (commander 104), Zeth Mages, CG 1343, mirrored — no unit of ours casts poison | 1100 |
| The specials | the sprite the user shows while using it (`FIGT.ADV:1660-1713`): Rance Attack Rance (unit 1), Kentarou-kun Slash Kentarou (unit 31), the White D. Beams Shizuka (unit 17) and Magic (unit 39), EPO Light Gandhi (unit 37), the Black D. Beams Chizuko (unit 38) and Nagi (unit 40), Whirlwind Shot Isoroku (unit 27), High Speed Meglass (unit 30), By la Way Rick (unit 7), Vanish Bomb Miki (unit 32), C. War Dance Patton (unit 18), Chuhai Asuka (unit 11), T2 Soaring Wave Rolex (unit 47); T2 Goliath none (no sprite, as in play) | 1520 … 1530, 1222, 1121, 1252 |
| The ninjas' follow-ups | Kanami, Freya, Kaoru, Wichita, each her own | 1164, 1176, 1182, 1185 |
