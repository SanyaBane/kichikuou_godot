# The gallery — our screens without playing to them (plan, 2026-10-25)

Our Storybook: a development mode that shows each of our screens on its own, in its states, fed from the kept saves,
and dumps them all as pictures in seconds. The owner pages through it in a window (switching palette, font, touch
mode, row heights live); the agent dumps it after a change and sees which screens changed. The pictures stay outside
the repository (they hold the player's CGs): `KICHIKUOU_UI_GALLERY_DIR` in `local.env`.

**Why.** A look at a screen today plays to it: a kept save loaded, the boot, the load list, the week's banner, the
menus — tens of seconds to minutes a screen, moving the system pointer — and some states no kept save reaches (§4).
The gallery skips the way there, not the build: it is the same Godot project, so it builds as fast as the game.

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
| `CommandMenuView` | `Open(CommandMenu)` | the record, or `CommandMenu.For` on `COMMAND.ADV`'s lines | ready |
| `StatusStripView` | `Show(StatusStrip)` | `presenter.StatusStrip` (a bound presenter) | ready |
| `ProvinceMenuView` | `Open(ProvinceMenu)` | the state and the menu's items; the army line is the presenter's private text | small: `ProvinceMenu.Of(state, province, items)` |
| `MapMarkersView` | `Load(markers)`, `Place(…, corner)` | `presenter.MapMarkers()`; needs the map under it | small: the map backdrop (§2) |
| `FloorLadderView` | `Open(FloorLadder)` | the record (`DgnRun_Floor`) | ready |
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
  The saves: `user://saves-week18` (the default) and `user://saves-week26`.
- **A spare presenter** for what reads the scenario's text (harem names, dungeon names, the status strip's lines,
  the battle's text): `new GamePresenter(new ScenarioText(GameData.ScenarioDirectory), GameData.Cgs(), glyphs,
  scripts, audio)` and `Bind(state)`, as `GameScreen.cs:361-369` builds it. No script runs.
- **The map backdrop** (for the plates, the province menu, the status strip, the week report): `presenter.Show(new
  MapEvent(3))` then `MapEvent(0)` — `Bind` marks the tiles valid, so the first forces them to be drawn — then
  `Compose()` and `Screen.ReadScreen()` into a texture (`Compose.cs:76-95`). Not run yet: step 1 checks it, else a
  `ShowMapNow()` on the presenter.
- **No event CG in any entry.** Picture panels and portraits are what the looks already show; a 131 window stands on
  a plain backdrop, never over its scene's CG.

## 3. Entries

An entry is one screen in one state: an id, a section (Map, Messages, Screens, Battle, Touch, Settings), the save, the
state variation (§4) and how its view is fed. Ids follow the looks' dump names where they exist (`treasure-question`,
`message-thought-right`, `week-report`), so the gallery's picture and the look's compare by name.

- **The part outside Godot** — the variations and the models built from them — goes into
  `src/Kichikuou.Original.Content/Gallery/` (`GalleryStates`, one method a variation), with content tests that each
  variation's state builds its model (the save from `LocalPaths`, skipped when there is none, as the golden tests).
- **The Godot part** — `godot/Gallery/GalleryScreen` (a full-screen Control opened by `Screens.Open`, as
  `CaptureList`) and `GalleryEntries*.cs` (one file a section): the entries' list on the left, the screen on the
  right at 1280×800 fitted, a bar with the palette, the font, touch mode and row heights (the options' own
  choices, applied live), a "made up" tag on an entry whose state the game may not reach as shown.
- **Opening it:** Debug → Gallery, and `--gallery [id,…]` (`Main.FromCommandLine`, beside `--effects`).

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

- `<Godot exe> --path godot -- --gallery --shots <dir> --quit` dumps every entry (or the listed ones) as
  `<id>.png`, plus `manifest.json` (id, section, save, variation, made up, palette, font, touch, hash). A window is
  needed (views draw nothing headless); nothing moves the system pointer.
- `tools/ui/gallery.ps1` builds the game, moves `KICHIKUOU_UI_GALLERY_DIR\current` to `previous`, dumps into
  `current` (Lacquer; `-Steel`, `-Touch` add their passes) and writes `index.html`: the sections, each entry's
  picture with its caption, and the entries changed since `previous` marked (by hash, then a pixel count), with
  before / after side by side.
- After a change to a screen: update its entries, run the script, look at what changed — what was meant and what was
  not.

## 6. The battle

The battle board is drawn from a presenter fed by the battle's events; a state alone cannot draw it. The route is a
golden capture (`KICHIKUOU_GOLDEN_DIR`) replayed by `GoldenReplay.RunAsync` into the spare presenter, held at the
chosen round header or exchange, as `BattleSceneTests` does — deterministic, the original script run headless on the
capture's inputs. This is the last step: the board's looks from the week-18 save (`--until round-N`) do until then.

## 7. Steps

1. **The frame and four screens.** The data (§2: the kept save, the spare presenter), `GalleryScreen` with its list
   and bar, `--gallery`, the dump with its manifest, `tools/ui/gallery.ps1` with the index and the diff, `PictureFit`.
   The screens, chosen to try each kind of feed:
   - **Treasure** — the state alone plus a variation (`Ui_ItemViewMode`), the callout's question (`ReplaceQuestion`)
     and a refusal (the script's lines);
   - **Harem** — the spare presenter's names;
   - **Subordinates** with its plate — `SubordinateResult.Preview` pulled out;
   - **the message windows**, every kind, `message-thought-right` among them — `MessageWindow.SampleOf` pulled out.
   Done when the gallery opens from Debug, `gallery.ps1` dumps the four screens' entries and marks a changed one, and
   the time is measured against the `--treasure` look.
2. **Map and messages:** the map backdrop, the plates, the command panel, the status strip, the province menu
   (`ProvinceMenu.Of`), the week report (its feeder), the floor ladder, the scene menu, the log, the touch controls,
   the options (writes kept off).
3. **Screens and the battle's overlays:** deployment (the panels' overload), Save/Load (`LoadSaveScreen.Items`, a
   read-only library), the enemy attack, the scout report, the cut-in, the counters, the info boxes, the dialog and
   the commands (input forwarded).
4. **The battle board** from a replayed capture (§6).
5. **The house style:** docs/ui/style.md, §7 gains "a changed screen updates its entries and the gallery is rebuilt";
   CLAUDE.md's commands list the gallery.

## 8. Questions for the owner

- Dump both palettes and the touch mode by default, or Lacquer alone with the others on request?
- The gallery in the release build's Debug menu too, or development runs only?
