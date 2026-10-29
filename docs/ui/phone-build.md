# The phone — the build plan (2026-10-28)

The phone's base versions are drawn and chosen in both orientations (docs/ui/phone.md, rounds P1-P19; the screen
gallery's tabs **Smartphone, upright** and **Smartphone, landscape**, docs/ui/gallery.md, §9). This document is the plan
for building them: what the code has today, the owner's answers that set the order, the foundation, and the steps. It is
the implementation's governing document; each step records here what was built and where it stands.

The rules stay the original's (docs/ui/style.md, §1): a phone layout moves and enlarges what our screens draw and where
the finger answers; every answer still reaches the script through the inputs it reads. **The PC and the tablet do not
change**: every phone step leaves the screen gallery's PC and touch passes "same".

## 1. The owner's answers (2026-10-28)

1. **Landscape first, then upright.** The landscape phone keeps the 1280×800 frame (LM1's principle), so the scenes and
   the engine's picture stay where they are and the whole game is playable on the phone soonest; the foundation is
   proven on that case. Until the upright phase the phone is held to landscape (either way round, by the sensor).
2. **The reference phone and longer screens.** The layouts are the Xiaomi Poco X6 Pro's (20:9, 1220 × 2712); a longer
   screen (21:9) gets wider black sides in landscape and more free height upright. 19.5:9 nearly fits (landscape sides
   13.4 mm against 14.5). 18:9 and shorter are put off (docs/backlog.md): their landscape sides are ~7.6 mm, too narrow
   for LM1's rails, and need mock-ups of their own.
3. **The tablet stays landscape.** The orientation choice in Option → Display (System, Upright, Landscape 1,
   Landscape 2; PO1o) is a phone's only; the tablet keeps its sensor landscape and its layout.
4. **The missing base versions are drawn before any build** (the owner's rule of 2026-10-27, "base versions first"):
   three screens of ours have no phone mock-up in either orientation — the **Menu sheet** (the menu bar's File, Screen,
   Option, MsgSkip as finger rows, which the Menu cell opens), the **message log** (`MessageLogView`), and **the
   comment's editing on Save/Load** with the on-screen keyboard up. Rounds P20 (upright) and P21 (landscape), step 0
   below.

## 2. What the code has today

Surveyed 2026-10-28 (references as of commit `199e397`):

- **The frame.** `GameScreen` lays a black `ColorRect` under `_content` (under the 24 px menu bar,
  `GameScreen.MenuBar.cs`), in which `_view`, a `TextureRect` with `KeepAspectCentered`, shows the engine's 1280×800
  picture (`GameScreen.cs:303-317`, fed at `:533-544`). The black sides are what `KeepAspectCentered` leaves; **nothing
  draws in them** — the tablet's drawer, Commands and the battle's rails all stand inside the frame
  (`SkipButtons`, `CommandsButton`, `BattleCommandLayer.Touch.cs:22`).
- **One frame, copied.** The picture's rectangle and its scale are computed in one way, but the formula is copied
  eleven times in `GameScreen` and its partial files (`PictureArea()`, `GameScreen.cs:2088-2094`, is the canonical one),
  and again in `BattleScreen`, `GalleryScreen` and `ImportScreen`.
- **The views are 1280×800 through and through.** Every view of ours is an unanchored `Control` placed each frame by
  `GameScreen._Process` with the frame's rectangle (`Place(Rect2)`, `k = area.Size.X / 1280`) or an origin and a scale,
  and draws in `_Draw` through `DrawSetTransform(k)` at layout-pixel rectangles hard-coded in the view or in its model
  (`SkipButtons`, `MessageWindow`'s panels, `DeploymentPanels.TableArea`); each hit-tests by dividing by its own `k`. No
  view lays itself out from its area's shape.
- **The touch mode** is the static `TouchMode` (`On`: Android or `--touch`; the pointer helpers), read 80 times in 30
  files, and **frozen at load in several views**: `BattleBoardView.Narrow` (`static readonly`), `CardPortrait`'s widths,
  the `readonly bool _touch` of `CommandMenuView`, `ProvinceMenuView`, `FloorLadderView`, `BattleDialogLayer`,
  `BattleCommandLayer.Touch`, and `LedgerTabsBar`. A phone that turns while the game runs needs them read live.
- **Orientation, safe area: none.** `project.godot` sets sensor landscape (`handheld/orientation=4`), the export
  immersive mode; nothing calls `ScreenSetOrientation`, `GetDisplaySafeArea` or the cutout functions; a resize is only
  followed because `_Process` places the views every frame.
- **The engine's picture can move.** `_view` is a plain `TextureRect`, and `ToScript`/`FromScript`/`PictureArea` read
  its place and size, so the picture can stand anywhere (upright's PS2 puts it in the screen's middle). The catch: every
  view takes the picture's rectangle as its own frame, so upright needs a layout area apart from the picture's.
- **Input.** The script's pointer is converted in one place (`GameScreen._GuiInput` → `ToScript`, or
  `BattleBoardView.ToScript` in a battle); taps arrive as emulated mouse events (`emulate_mouse_from_touch` on by
  default); our views hit-test themselves (about 20 `_GuiInput` overrides).
- **The screen gallery** dumps at a forced 1280×800 window (`GalleryScreen.cs:261`), cropped to `PictureArea()`
  (`:286-291`); its passes are argument lists in `tools/ui/gallery.ps1` (the touch pass is `--touch`); the phone tabs are
  `tools/ui/mock_phone.py`'s drawings, not dumps.
- **Settings** are static `*Choice` classes over `user://settings.cfg` (section `[display]`: `font`, `ui_palette`,
  `ledger_row_height_touch`, …); Option → Display is `OptionsView.DrawDisplay` (`OptionsView.cs:486-515`).

So the frame is decided in one place in practice, but the 1280×800 coordinate system is in every view: **each screen
needs a phone layout of its own** (as phone.md, §1 measured), chosen by one live switch and handed out from one place.

## 3. The foundation

Built in step F (landscape) and completed in step P0 (upright).

- **The device, decided live.** One `DeviceLayout` — `Pc`, `Tablet`, `PhoneLandscape`, `PhoneUpright` — decided from
  the touch mode, the screen's physical size and the window's shape, and **read live** wherever a view lays itself out
  (the frozen reads above become reads of it; `TouchMode.On` stays the input's switch: no hover, the long press). A
  phone is a touch screen whose short side is under ~90 mm (`ScreenGetSize` / `ScreenGetDpi`: the Poco 69.5 mm, the
  Galaxy Tab S7 FE 167 mm; robust to Android's density buckets). On the PC `--phone landscape` or `--phone upright`
  (implies `--touch`) opens a window of the reference phone's shape, fitted to the monitor, so every phone screen can be
  looked at and dumped without the device.
- **The phone's own unit: the reference phone's pixel.** The mock-ups and phone.md measure everything in the Poco's
  pixels (1220 × 2712, 17.6 a millimetre), so a phone layout is written in those: the figures of the documents carry over
  unchanged. Landscape scales by the screen's height (1220), upright by its width (1220); the other side flexes (the
  sides' width, the free height). A pure model in `Original.Content` — `PhoneFrame`, with tests — gives, for a screen
  size, an orientation and the safe insets: the frame's rectangle, the black sides' rectangles (landscape), the
  picture's and the layout area's rectangles (upright), and the factor from the reference pixel to the canvas.
- **One frame helper.** The eleven copies of the picture's fit become one (`PictureArea` and its scale), which the
  phone's frame then replaces on a phone; the views take what they take today (a rectangle, or an origin and a scale)
  on the PC and the tablet, and a phone view takes `PhoneFrame` too.
- **The safe area.** `DisplayServer.GetDisplaySafeArea()` on Android; the reference's values — the camera's 110 px
  (6.3 mm) kept free on **both** short sides in landscape (either may hold the hole, phone.md, §12) and at the top
  upright, the gesture bar's 50 px (3 mm) at the foot — as the fallback and under `--phone` on the PC.
- **The sides drawn.** On the phone the menu bar goes (the Menu cell opens the Menu sheet instead), `_content` takes the
  whole screen, and the black sides are ours to draw in (still black under our controls, as the frame's surround is
  today).
- **The orientation** (step P0): Option → Display's four ways on a phone (PO1o) through
  `DisplayServer.ScreenSetOrientation`; System is "as the phone is held, unless its own rotation lock holds it" —
  Android's `fullUser`, which Godot 4.7.2's Android library asks for on `ScreenOrientation.Sensor` (read in the export
  template's `GodotIO.setScreenOrientation`: `Landscape` → `landscape`, `Portrait` → `portrait`, `ReverseLandscape` →
  `reverseLandscape`, `SensorLandscape` → `userLandscape`, `Sensor` → `fullUser`). `handheld/orientation` stays sensor
  landscape (`userLandscape`), the tablet's, and a phone asks for its choice at the launch; a turn at any moment lays
  every open view out again (the game waits on nothing for it).
- **The screen gallery's phone passes.** `gallery.ps1` gains real passes `phone-land` and `phone` (`--phone …`, the
  window's size an argument instead of `GalleryScreen.cs:261`, the whole window dumped, not `PictureArea()`); dumps at
  the reference's pixels if Windows lets a window be that large, else at a fixed fraction (decided in step F). As each
  phone screen is built, its entry dumps on the pass and its mock-up leaves `mock_phone.py`'s `GALLERY_ENTRIES_LAND` /
  `GALLERY_ENTRIES` (gallery.md, §9); the tab shows dumps and the mock-ups not yet built, each marked.
- **The device.** The Poco by USB through `run-android.cmd` (`ANDROID_SERIAL` when the tablet is connected too;
  `-PushGame` the first time). **The speed** is measured on it in step F — the engine surface is drawn per pixel in C#
  (docs/backlog.md, "Android": not measured yet) —: frame times in a scene, on the map and in a battle, recorded here; a
  phone too slow to play is a task of its own before the screens.

## 4. How a step is made

Every step below, in the house's way (docs/ui/style.md, §7) with the mock-ups already chosen:

1. Read the screen's section of phone.md and look at its mock-up on the gallery's phone tab (`gallery.ps1 -PhoneOnly`).
2. Where the layout has logic of its own (the grid folded in two, which window goes above the picture, the ledger's
   columns at the phone's type), a model in `Original.Content` with tests first.
3. The view's phone layout: a branch of its own keyed by the live `DeviceLayout`, written in the reference phone's
   pixels; the PC's and the tablet's code paths left as they are.
4. The gallery: the entry dumps on its phone pass, its mock-up leaves the tab; the PC and touch passes stay "same".
5. Unattended play with `--phone …` where the step changes a flow the bot goes through (`--auto` from the kept saves,
   CLAUDE.md, "A look at one screen"); dumps through `--shots` keep the event CGs masked.
6. The owner's look on the Poco; this document says "awaiting the owner's look on the phone" until then.
7. A commit per step (docs and code together); the tests the change reaches (CLAUDE.md, "Commands": Godot-only code
   needs the build, the core tests and its own tests; a model in `Original.Content` its content tests, quick run).

## 5. The steps

### Step 0 — the missing base versions (rounds P20, P21)

Drawn by `mock_phone.py --round P20` (upright) and `--round P21` (landscape) over the gallery's dumps, the sheets sent
to the owner before the question (as every round), the owner's choices recorded in phone.md and added to the gallery's
phone tabs:

- **The Menu sheet** — the menu bar's commands (File, Screen, Option, MsgSkip; Debug in a development build) as finger
  rows (7.4 mm), opened by the Menu cell (upright the bar's left, PK1; landscape the left side's top, LM1/LS1).
- **The message log** — the paper chronicle (mock-up L3a, docs/ui/messages.md, §5 step 6) at the phone's type, opened by
  the Log cell, scrolled by a finger, closed by a tap off it (touch.md, "The Skip and Log buttons").
- **The comment's editing** on Save/Load (PL2, LL1: Edit) — where the field stands while Android's keyboard covers the
  screen's lower part.
- **The original's own screens**, shown as they are for the owner to confirm (no layout of ours): the Fortune Check
  (`HAPPY_C`), the CG gallery (`CG`), the music room (`MMODE`), an ending's Completion Report (`OVER`) — landscape in the
  frame as LS1, upright at the screen's width with the bar; a tap is the click, a long press the right click. They are
  drawn over new screen gallery entries of those screens, which `GalleryTitle` reaches by a click on their button (the
  backlog's "The title's other screens in the screen gallery", taken into this step, 2026-10-28; Alice's Mansion and
  Saitou are scenes, laid out as PS2 and LS1, and the debug starts' menu is a development screen).

**Where it stands (2026-10-28):** the gallery entries are built — `fortune-check`, `cg-gallery`, `music-room`
(docs/ui/gallery.md, §7 step 7); the ending's Completion Report is taken from a `--ending 11` run. Rounds P20 and P21
are drawn (docs/ui/phone.md, §25-§26) and chosen by the owner: the Menu sheet PN1 (PN1q) and LN3, the log PR2 and LR2,
the comment's editing PY2 and LY1, the original's own screens as they are (PX…, LX…) — all on the gallery's phone tabs.
**Step 0 is done**; every screen of ours has a chosen base version in both orientations. L1 builds the Menu sheet as
LN3 and the log as LR2; L7 the editing as LY1.

The development screens (the capture viewer, Portraits, Effects, the screen gallery, Debug's sliders) get no phone
layout: on a phone they show as on the tablet, in the frame (the agent's choice, below).

### Landscape (steps F, L1-L10)

| Step | What | Base versions (phone.md) |
|---|---|---|
| **F** | The foundation (§3): `DeviceLayout`, `PhoneFrame`, the one frame helper, the safe area, the sides, the menu bar gone, `--phone landscape`, the gallery's `phone-land` pass, the speed on the Poco. Done when the game runs on the Poco in the frame with today's touch layout inside and black sides, the PC and touch passes same. | §12 (the frame) |
| **L1** | The scenes and the left side: Menu on top of the left side, Skip and Log at the left thumb (the drawer out of the frame, so 131 no longer docks smaller), the `]` menu's rows at the frame's right foot; the Menu sheet and the message log (round P21); the week report's side; the title's description. | LS1, LS1b, LS1m (§19); LW1 (§14); LEd (§23) |
| **L2** | The map: the status stacked in the left side, the week's uses by group in the right, Commands at the right thumb; the command panel in the sides (two-line rows 14.5 × 7.0 mm, Turn END on the right's top, Close where Commands was); a province's card beside it at the phone's type, its commands in the right side; plates with 36 px text. The status no longer over the map, so the map's headroom (`MapHeadroom`) stays off on the phone. | LM1, LM1b, LM1c (§12) |
| **L3** | The battle's turn: the scene's picture at 70%, its sides blurred to the frame; the cards 25.5 × 19.0 mm, their text 40-45 px, the order toggles across the card's foot; the rails in the black sides (rows 14.5 × 7.0 mm, Melee 14.8 mm with the damage forecast at the thumb); Skip on the left rail's top. The retreat question beside Retreat; an exchange with its counters in the scene's blurred sides. | LB2 (§12), LB2r1, LB2x (§17) |
| **L4** | The battle's other states: the opening at the scene's place, the scout report and the strategy's cut-in across the width, the results plate (a level-up's page, a fall's box, our panel the same way), a fighter's line (the portrait in the blurred side, the balloon at 3.5 mm), a defence's ribbon and caption. | LB3o1, LB3s, LB3c, LB3r, LB3b (§17) |
| **L5** | The deployment table and Subordinates: the ledger in one-line rows at the phone's type (rows 112 px, every column of a tab in one line), wholly at the left on the deployment table; the party two cards a row at the right with their ✕, Stop \| Attack at the right thumb; Subordinates' Add New Troops in the left side, Talk, Replenish and End in the right, Replenish's question in a callout left of it; a unit's picture on a long press. | LD2, LU1, LU1q (§13) |
| **L6** | The enemy attack: the facts at the left, the answers in a column at the right thumb (Abandon City narrower, Intercept half as high), Ninja Scout's list in two columns (five ninjas in view). | LA2, LA2n (§14) |
| **L7** | Harem and Treasure with their grid folded after its ninth row (a model, tested), every cell in view, long item names shrunk; Save/Load as the PC's split, the comment's editing as round P21 chose. | LH2, LT2, LL1 (§15) |
| **L8** | The options window across the width, each tab in two columns; the dungeon's ladder and entry menu as the tablet's rail at the phone's sizes. | LO1, LF2, LF2e (§16) |
| **L9** | All Data (the graphs as cells in the left side, the scales as chips in the right, the bars' figures drawn again at 2.3 mm); the title as the PC's (Menu in the left side); the import screen on its paper sheet; the original's own screens as round P21 chose. | LG2m, LG2, LE1, LI, LIp, LIr (§23) |
| **L10** | The landscape phone whole: unattended play with `--phone landscape` from the kept saves through every exercise it has (the deployment, the battle, `--treasure`, `--saveload`, `--scout`, `--dungeon`, `--harem`), the owner's play on the Poco, the docs (phone.md, touch.md, style.md §6, backlog). | — |

**Step F, where it stands (2026-10-28):** done — built, run on the Poco and measured there (below, "The Poco").

- **The device**: `Original.Content/Devices/DeviceLayout` (the enum and `DeviceLayouts.IsPhone` / `Decide`, tested) and
  `godot/Strategy/Device`, which reads it live — the main window followed from `Main` (`Device.Follow`: the layout and
  the safe area read again on every resize or turn), `Device.Phone` from Android's screen size and dpi or `--phone`,
  `Device.TouchLayout` for the views that take the tablet's layout. `TouchMode.On` stays the input's switch and is on
  with `--phone` too. The frozen reads of §2 are live reads of `Device.TouchLayout` now: `BattleBoardView.Narrow` and
  the board's places that hang on it, `CardPortrait`'s widths, the `Touch` of `CommandMenuView`, `ProvinceMenuView`,
  `FloorLadderView`, `BattleDialogLayer` and `BattleCommandLayer`, `LedgerTabsBar.RowHeight`, `LedgerRowsChoice`.
- **The frame**: `Original.Content/Devices/PhoneFrame` (tested, `PhoneFrameTests`): for a screen held landscape and its
  safe insets, the frame (fitted to the height, centred; between the hole's strips on a screen too short for it), the
  black sides whole, the room in them our controls may take, `Unit` (the canvas's units a reference pixel) and `Px`.
  The insets are the system's when it reports them — the larger side's on both sides, so nothing moves when the phone
  is turned round, and at least the gesture zone at the foot, which Android's safe area does not count — else the
  reference's 110 / 50 px.
- **One frame helper**: `godot/Strategy/ScreenFrame` (`In` for the device, `Fit`, `Scale`, `Pixels`, `ToScript`,
  `FromScript`) in place of the eleven copies: `GameScreen.Frame()` (`PictureArea()` is its area) and its partial
  files, `BattleScreen.ToScript`, `GalleryScreen.PictureArea`, `ImportScreen.Layout`.
- **The phone's screen**: `GameScreen.UpdateDevice` (each frame): on a phone the menu bar is hidden and `_content`
  takes the whole screen; held landscape `_view` stands at the phone's frame, its black sides ours. Nothing is drawn in
  them yet (L1 on); the Menu sheet comes with L1, so until then the phone has no File/Option menu (Save and Load are
  in the command panel).
- **`--phone landscape`** (implies the touch mode): the window at the reference phone's shape, as large as the
  monitor's usable area allows, at most 2712 × 1220 (`Device.OpenPhoneWindow`; 2544 × 1144 on the owner's 2560 × 1440
  monitor). `--phone upright` is refused until P0.
- **The gallery's pass** (the agent's choice): a pass of its own, `phone-landscape` ("Smartphone, landscape";
  `gallery.ps1 -PhoneLand`), beside the mock-up tab `phone-land`, which stays as it is — the mock-up tab's folder keeps
  its name, and each screen's mock-up leaves `GALLERY_ENTRIES_LAND` as its phone layout is built. The pass dumps every
  entry with `--phone landscape --window 1808x813`: the whole window, its black sides with it, at **two thirds of the
  reference's pixels** (a window of 2712 × 1220 does not fit a common monitor; at two thirds the frame is 1301 × 813,
  about the PC's own size, and the dumps are the same on every machine). `GalleryScreen.WindowSize` (`--window WxH`)
  replaced the fixed 1280 × 800, which stays the default. At F the pass shows today's touch layout in the frame: the
  baseline the L steps change entry by entry. The Lacquer and Touch passes were "same" (110 and 111 entries).
- **The speed**: `godot/Strategy/FrameTimes`, on on Android and with `--frame-times`: every 10 s the console (logcat,
  tag `godot`) gets the frames of each kind shown — a scene, the map, a battle — their count, mean, 95th percentile and
  longest interval, and the slowest frame's processing in each second (Godot's `TimeProcess` is that, updated once a
  second), its median and longest; `Device` logs the screen's size and dpi, the window and the safe area (§7).

**The Poco** (the owner's play, 2026-10-28: the debug build, the week-18 save copied to it; Android 16, HyperOS):

- **The device**: the window 2712 × 1220, the screen at 480 dpi (Android's bucket; the short side reads 64.6 mm, a
  phone), `PhoneLandscape`. The game is drawn under the cutout: the safe area is (120, 0)-(2592, 1220), the camera's
  hole **120 px** on the left (the reference's 110 was the mock-ups' guess), 0 on the right and at the foot (Android's
  safe area does not count the gesture zone), so `PhoneFrame` keeps 120 px on both sides and the reference's 50 at the
  foot. The process held 1.1-1.7 GB (HyperOS's log).
- **The speed** — the screen runs at 120 Hz, a frame 8.3 ms:

  | Screen | Frame interval: mean / p95 / longest | Slowest processing a second: median / longest |
  |---|---|---|
  | Scenes (the title, the week's start, after the battle) | 8.4-9.8 / 8.3-11.1 / 16-150 ms | 16-45 / 51-313 ms |
  | The map, held and dragged | 8.8-9.8 / 10-19.4 / 58-108 ms | 9-36 / 52-118 ms |
  | The map's first seconds (the talks read ahead, 190 ms) | 11.8-18.2 / 23-110 / 133 ms | 158 ms |
  | The battle, the player's turn | 14.6 / 37.7 / 150 ms | 43 / 459 ms |
  | The battle, the exchanges | **47.4 / 138-143 / 150 ms** | **146-151 / 178 ms** |

  The scenes and the map run at the screen's rate, a long frame now and then (entering a screen, the read-ahead). **The
  battle's exchanges run at about 21 frames a second**, a frame of 140-150 ms every few: playable, but the animation
  stutters. The cause guessed here (the whole 1280×800 picture composed and uploaded each frame) was wrong; the cause
  measured and its fix are "The battle's speed" below: the exchanges now run at about 100 frames a second.
- **Found on the way**: on Android 16 a folder `adb push` creates in the app's external folder is the shell user's with
  no access for others, so the game could not read the pushed game folder (an older Android let it); the Android
  scripts now open what adb put there to the app (`Grant-AppAccess` in `tools/android-adb.ps1`, run by
  `export-android.ps1` and `copy-saves.ps1`). Xiaomi's phones also need Developer options → "Install via USB" before
  adb may install.

**The battle's speed** (2026-10-28, a task of its own before L3; the owner's play on the Poco, the week-18 save's attack
on Oaks (6), the debug build):

- **The cause**, measured with a probe of each frame's parts written to logcat (temporary, not committed). Not the whole
  picture: while our battle screen covers the view, `GameScreen` only takes the change, and the windows' layer was
  composed once in ten seconds. The time went to:
  - the scene's **front layer**, drawn again by `BattleScene` at every shots frame and counter of an exchange though
    without the numbers (our screen's) only its marks can change, then read and uploaded again by `BattleBoardView`:
    13.5 ms, 147 times in 7.6 s;
  - the **surface's reads and keyed copies** (`Surface`): the palette applied a byte at a time with a second pass for a
    layer's alpha, a `Buffer.BlockCopy` call per pixel in the keyed copies. These slowed the layers' reads (~13 ms
    each) and the engine's own drawing of an exchange (`BattlePresenter`: a shots frame 11.6 ms, the counters ~40 ms,
    up to 80);
  - **`BattleBoardView.Place` every frame**: 1.2 ms, its theme overrides set again (a theme change: the armies' names
    shaped and their lines sorted again) and every layer drawn again;
  - **all eight cards built again** when one changed: ~10 ms, then their layout;
  - and **our libraries compiled unoptimized** in Godot's export configurations: the .NET SDK optimizes only `Release`,
    so in `ExportDebug` (this build) and `ExportRelease` (the Windows release too) Mono and CoreCLR ran all our C# —
    the rules, the presenter, the surfaces — without the JIT's optimizations, which on the PC makes the same code
    5-10 times slower.
- **The fix** — nothing drawn differently: the PC and touch gallery passes same (110 and 111 entries):
  - `BattleScene` draws the front again only when what it shows changes: in an exchange with the numbers every time,
    without them only with its overlay or the marks;
  - `Surface` reads a row in one pass, a pixel a word, through a table of the palette for each alpha (stored, opaque,
    a layer's), copies keyed a word at a time into a buffer it keeps, and draws a CG's row from its span;
  - `BattleBoardView` lays the screen out only when its origin, scale or layout changed, and builds again only the
    cards whose contents changed;
  - `Directory.Build.props` sets `Optimize` for `ExportDebug` and `ExportRelease`; the editor's `Debug` stays as it is.

  The surface alone, a 640×256 layer on the PC (stored 1280×512):

  | | Unoptimized: before / after | Optimized: before / after |
  |---|---|---|
  | `ReadLayer` | 11.0 / 4.8 ms | 2.5 / 0.5 ms |
  | `ReadArea` | 10.6 / 5.3 ms | 2.0 / 0.6 ms |
  | `CopyKeyed` | 10.6 / 5.2 ms | 7.7 / 3.5 ms |
  | `DrawCg` | 4.2 / 2.2 ms | 0.4 / 0.5 ms |

- **On the Poco** — the screen at 120 Hz, a frame 8.3 ms:

  | The battle's exchanges | Frame interval: mean / p95 / longest | Slowest processing a second: median / longest |
  |---|---|---|
  | Before, MsgSkip on (without it not measured before) | 28.6-47.1 / 128-133 / 146-149 ms | 43-144 / 161-168 ms |
  | After, MsgSkip off | **9.4-10.2 / 11.9-15.0 / 91-142 ms** | 20-64 / 93-226 ms |
  | After, MsgSkip on | 15.8 / 64 / 111 ms | 20 / 106 ms |

  The exchanges run at about 100 frames a second, near the screen's rate; with MsgSkip, whose pauses take one frame
  each, every frame draws a step of the exchange. Still a long frame (40-140 ms) or two at an exchange's start:
  - the engine's own drawing of its start (`BattlePresenter`: the engagement picture, the slide and the counters,
    copies of 640×400 stored at 2×, ~13-45 ms on the phone, kept as the original draws them);
  - an effect's first play in a launch (its textures made per pixel in C#, `EffectTextures`: ~130 ms the first time,
    ~30 ms the second) — since 2026-10-28 their masks are drawn ahead, on a worker at the launch
    (`EffectTextures.Prepare`, from `Main`; its time in the log, ~850 ms on the PC for all 43), the textures still made
    from them at first use; the frames are the same (180 of three effects compared); on the Poco the masks take 1060 ms,
    done a second after the launch. The first play's frame times in a battle are the owner's play to tell.

  Both are in docs/backlog.md, "Android". Note on the figures: a frame's interval is Godot's `delta`, so the script's
  work at a frame's start (`process_frame`) is counted in the next frame's interval; a period's figures are the same.

**Step L1, where it stands (2026-10-28):** built, **awaiting the owner's look on the phone**.

- **The models** (`Original.Content/Devices`, tested in `PhoneScenesTests`): `PhoneScenes` — the left side's cells, the
  mock-ups' rail from the hole's strip + 6 to the frame − 10 (Menu 110 high on top, Skip and Log 118 high at the foot,
  Log lowest); a `]` menu's rows at the frame's right foot (123 high, 12 apart, at least 500 wide and wider for a long
  item up to half the frame, lower with their type when a long menu would not fit the height); the log's paper (LR2);
  the week report's scale (LW1's card 1320 wide unless it would then leave less than 60 over and under it). `PhoneMenu`
  — the Menu sheet's pages of tiles (the menu bar's commands; Debug's own page), the questions in the PC's dialogs'
  words, the panel, the tiles four a row, a question's callout above its tile or, near the top, under it.
  `PhoneFrame.Box.Contains`.
- **The views**, each keyed by `Device.Landscape(view)` (the phone's frame over the view's parent; null on any other
  device, whose code paths are as they were): `SkipDrawer` draws Skip and Log as cells in the left side (`SideCells`:
  a plate, the icon left, the word right; Skip lit gold reading "ON" while the skip runs); `MessageWindowView` no
  longer docks 131 there; `SceneMenuView` puts the rows at the frame's right foot (LS1m); `MessageLogView` lays the
  paper across the width with Close at the Menu cell's place (LR2; its figures a `Metrics` record whose PC values are
  the former constants); `WeekReportView` draws the card ×1.44 over the map dimmed (LW1).
- **The Menu** (LN3): `MenuSheetView` — the cell, and the sheet it opens over the whole screen, modal as the PC's
  boxes — and `GameScreen.Phone.cs`: the cell shows on the phone wherever a game is shown and no screen of ours takes
  the side or waits on its own (`MenuCellShown`); the tiles act as the menu bar's items (`PhoneMenuChosen`); the PC's
  confirmation boxes now take their words from `PhoneMenu.QuestionOf`. On the phone the drawer and Log also show at the
  week report (`PhoneWeekReport`), as LW1 has them.
- **The agent's choices**, for the owner to confirm: Debug's tiles on a page of their own, ‹ Menu first, the Debug tile
  in a development build only (`OS.IsDebugBuild`; the PC's bar keeps its Debug always); Battle Captures and Screen
  Gallery ask in the callout too, as the PC's boxes do; the Menu cell also on the map's wait, over the province menu and
  over All Data (the steps that lay those out decide their side), not over our battle and full screens, the command
  panel and the enemy attack; All Data's `]` menu takes LS1m's rows until L9 gives it LG2m's cells.
- **The gallery**: the PC and touch passes same (110 and 111 entries); the phone pass's L1 entries match their
  mock-ups, three entries of its own show the sheet (docs/ui/gallery.md, §9), and LS1, LS1b, LS1m, LW1, LEd, LN3 and
  LR2 left `GALLERY_ENTRIES_LAND`.
- **Unattended play**: `--phone landscape` headless from the week-18 save to its deployment picker runs as before. The
  touch exercise in a window (`--harem` from the week-18 save) now also taps the Menu cell, Sound twice, Restart and its
  Cancel, then Close (`AutoPhoneMenuAsync`); not run — it needs a window, where a hand on the mouse can break it.

**Step L2, where it stands (2026-10-28):** built, **awaiting the owner's look on the phone**.

- **The model** (`Original.Content/Devices/PhoneMap`, tested in `PhoneMapTests`): the map's places in the sides at the
  mock-ups' figures — the status under the Menu cell (from 34 under it, 246 high), the week's uses by group in the right
  side (a caption 36, tags 46 high 54 apart, 12 after a group), Commands at the right thumb (150 high over the gesture
  zone), Close and Return a row's height there; the command panel's header on the left's top, its caption ("This week")
  at 176, its commands from the foot up, Turn END on the right's top (140 high) and the items from the foot up over Close;
  a province's commands over Return, the dungeon's place's caption over them; the rows 123 high 12 apart, lower when a
  rail has too many for its height; a province's card (560 wide, 90 beside the province, left of it when the frame has
  room, its middle level with it, kept 20 inside the frame over the gesture zone; the army under a long name), the ring
  (74), the plates' type (36).
- **The views**, each keyed by `Device.Landscape(view)`: `StatusStripView` (`StatusStripView.Phone.cs`) over the whole
  screen on the phone, in a `SideMode` — the map's wait (the status under Menu, the tags in the right side), a province
  (the status alone), the command panel (the date and the budget on the left's top) — its building tooltip left of
  Construction's tag in the frame; `CommandsButton` in the right side, its icon over its word (`SideCells.DrawButton`),
  as Close a row's height; `CommandMenuView` (`CommandMenuView.Phone.cs`) dims the frame, its rows two lines, the tag
  over the name (`SideCells.DrawRow`), a tap anywhere but a row closes it; `ProvinceMenuView`
  (`ProvinceMenuView.Phone.cs`) rings the province, its card beside it at the phone's type (the figure right of its
  label), its rows in the right side, Return at the foot (`SideCells.DrawButton`); `MapMarkersView`'s plates at 2 mm
  with the mock-up's tighter plate (11 of its units high, 3.5 at the sides). `GameScreen` keeps the map's room under the
  strip off on the phone (`UpdateHeadroom`, `ScriptY`) and places the strip over the content (`UpdateStatusStrip`).
- **The agent's choices**, for the owner to confirm: while the command panel is open the Menu cell and the drawer are
  hidden (LM1b's left side holds the week's commands; L1 already hid them there), and over a province's menu Menu shows
  and the drawer does not (LM1c); the map under the panel dimmed (LM1b), not on the tablet; Commands 150 high but Close
  and Return a row's 123 (the mock-ups'); a long province name ("Leazas Castle", "Great Wall of Helman A") has its army
  on a line under it rather than its type below 2.3 mm; on the phone the building's tooltip comes by a long press on the
  right side's Construction tag, left of it.
- **The gallery**: the PC and touch passes same (110 and 111 entries); the phone pass's map entries match LM1a, LM1b and
  LM1c (docs/ui/gallery.md, §9), which left `GALLERY_ENTRIES_LAND`.
- **Unattended play**: `--phone landscape` headless from the week-18 save to its deployment picker runs as before (its
  map waits, the replenish through Subordinates, the attack on Oaks (6)); the touch exercise in a window not run.

**Step L3, where it stands (2026-10-28):** built, **awaiting the owner's look on the phone**.

- **The model** (`Original.Content/Devices/PhoneBattle`, tested in `PhoneBattleTests`): the frame's places in its layout
  pixels (the mock-ups' figures over the reference's 1.525) — the scene's band 438 high (70%), its own picture as much
  smaller in the middle, the names' line, the cards 447 × 334 from 50 under the scene, 12 apart, the gauge 90 wide between
  the front rows, a card's insides (the face 160, the figures' row 52 at 32, the name 45, the troops and HP 40, the quiet
  lines 36, the toggles 80 high across the foot), the counters' sizes (124, 70, 26) —; the sides' places in the canvas's
  units — the rails' panels the sides' height, the battle's Skip on the left's top (100 high), the acting unit's name at
  180 and its rule at 222, TARGET at 60, the target at 122 and the rule at 172, the commands from the foot up (123 high,
  12 apart, Retreat on top), the attacks from the foot up (260 high), lower when a rail has too many; the retreat question
  (660 × 350 at the frame's left + 44, level with Retreat, kept over the gesture zone) and its answers in a row.
  `PhoneMap.Stack` is shared with it (the rows' height and gap its parameters).
- **The views**, each keyed by the live device: `BattleBoardView` lays the board out at `PhoneBattle`'s places on the
  phone (the scene, its windows' layer and the exchange's effect at the scene's 70%, the strip and the limit at the
  scene's foot and top at the PC's size, the stones from the scene's foot, `BoardStones` now made for an area; the gauge
  1.2 times the PC's and as high as both rows; the cards' insides at the phone's type, the troop type and the troops'
  line shrunk to fit as the mock-up's; the toggles across the card's foot; the action's pill 1.6 times; the speaker's
  portrait in the scene's blurred side, LB3b's place); `CardPortrait` the PC's face again; `ExchangeCountersView` in the
  scene's blurred sides beside their army (LB2x; smaller together when a four-digit side would not fit its room);
  `BattleCommandLayer.Phone.cs` the rails in the sides (the tablet's buttons, the attack's name and forecast at the
  button's height); `BattleDialogLayer.Phone.cs` the retreat question (LB2r1); `TouchPill` the battle's Skip as the
  scenes' Skip cell on the left rail's top (`GameScreen.Skip.cs`).
- **The agent's choices**, for the owner to confirm: the enemy's cards keep their face at the right, as on every device
  (the mock-up drew every face at the left); the attack button's forecast is the tablet's two lines (the owner's D2) at the
  button's size, where the mock-up had both figures on one line; the order strip and the limit's plate keep the PC's size
  at the scene's foot and top (the mock-up showed them at the scene's 70%); while the retreat question asks, the left
  rail shows Retreat alone, lit (the other commands are gone once Retreat is chosen), the screen dimmed under the callout.
- **Left for L4** (done there): the cards' long-press menu at a finger's size, the balloon and the info boxes (LB3b,
  LB3r), the opening (LB3o1).
- **The gallery**: the PC and touch passes same (110 and 111 entries); the phone pass's battle entries match LB2, LB2r1
  and LB2x (docs/ui/gallery.md, §9), which left `GALLERY_ENTRIES_LAND`.
- **Unattended play**: `--phone landscape` headless from the week-18 save to round 2 of the battle on Oaks (6) runs as
  before.

**Step L4, where it stands (2026-10-28):** built, **awaiting the owner's look on the phone**.

- **The model** (`Original.Content/Devices/PhoneBattleStates`, tested in `PhoneBattleStatesTests`): the frame's places in
  its layout pixels at the mock-ups' figures — the opening's plate (940 × 196 from 18), the names' room beside it (30 in
  from the frame's edge and the plate, down to the advantage's line when the army has one), the advantage's and VS's
  middles; the results plate (1400 × 720, centred 20 over the frame's middle), a level-up's rows on it (76 high, 92 apart,
  closer when four would pass the gesture zone) with room under them for the click's triangle; our panel in the frame's
  middle at the plate's type (50); the balloon (1000 wide from 50 right of the scene's picture, its foot 486 down, growing
  up) and the tail's place; a defence's caption over the scene's foot above the order strip (2.4 mm); the cards' menu rows
  (104, lower when nine items would not fit) — and the scout report's places in the canvas's units across the room between
  the hole's strips (the ninja, her balloon, the cards 700 × 270, the rows' captions, Rance and his answer, Maris and her
  comment at the foot right).
- **The views**, each keyed by the live device: `BattleOpeningLayer` covers the lower scene band, its plate and type
  LB3o1's, the names centred beside the plate (`DrawPhoneText`); `ScoutReportView.Phone.cs` draws the report over the
  whole screen, the cards as the enemy attack's; `StrategyCutInView` lets the band run on across the black sides (strong
  on the strategist's side, its gold edges alone on the other); `BattleInfoLayer.Phone.cs` the results plate and the
  panel over the frame dimmed, the ribbon 1.3 times the PC's, the caption at the phone's type over the scene's foot;
  `SpeechBalloon` the balloon at 62 (smaller down to 46 to keep two lines), its tail at the portrait in the blurred side
  (`DrawBalloon` takes the tail's place); `BattleCommandLayer`'s card menu at the phone's rows (its sizes properties of
  the device).
- **The agent's choices**, for the owner to confirm: a level-up's rows stay on the results plate, under the EXP line (the
  plate grows; LB3r's note spoke of "a level-up's page after it", which would make the player tap twice for one key
  wait); our narration panel and a fall's box dim the scene and the board as the results plate does ("the same way"); the
  damages in the brush face as LB3r drew them (the PC's are Cinzel); Maris's comment, which LB3s did not draw, at the foot
  right beside Rance's answer, her portrait in the corner; the card menu's rows 5.9 mm (nine items fit the height), not the
  rails' 7.0.
- **The gallery**: the PC and touch passes same (110 and 111 entries); the phone pass's `opening`, `scout-report`,
  `strategy`, `strategy-enemy`, `info-results`, `info-level-up`, `info-panel`, `info-defence`, `battle-speech…` and `menu`,
  `menu-orders` match their mock-ups, and LB3o1, LB3s, LB3c, LB3r and LB3b left `GALLERY_ENTRIES_LAND`.
- **Unattended play**: `--phone landscape` headless from the week-18 save to round 2 of the battle on Oaks (6) runs as
  before.

**Step L5, where it stands (2026-10-28):** built, **awaiting the owner's look on the phone**.

- **The model** (`Original.Content/Devices/PhoneLedger`, tested in `PhoneLedgerTests`): the places at the mock-ups'
  figures (`ld2`, `lu1`, `land_ledger`, `party_card`, `lu_orders_side`) in the canvas's units — the ledger's paper (the
  deployment's from the hole's strip + 6 to 16 short of the right part, Subordinates' the frame less 12 a side, both from
  104 down to 12 over the screen's foot), the tabs over it (300 wide, 12 apart, 88 high, an unchosen one 10 lower); the
  deployment's right part (880 wide, ending 6 short of the hole's strip: a longer screen widens the ledger), its lines,
  the party's cards (434 × 214, 12 apart, the back row left), a card's insides and its ✕ (84), the totals under the
  cards, Stop (300) and the sends sharing the rest at the right thumb (150 high); Subordinates' left side (ORDERS, the
  unit, ADD NEW TROOPS, the steps 118 high from the foot up, lower when they would not fit), its right side (the title
  block, Talk 140, Replenish 150 between Talk and End, End 140 at the thumb), Replenish's question's room (620 wide, 40
  left of the button, on its foot); the picture a long press raises (in the frame's middle, the frame's height less 60
  over and under it). `PhoneMap.Left`/`Right` are shared with it.
- **The views**, each keyed by the live device: `DeploymentTableView.PlacePhone` lays the ledger out in one-line rows at
  the phone's type (rows 112, the text 40, the columns 18 apart; every column of a tab in one line, as the PC's columns
  fit their text) and, on the phone, raises `RowHeld` for a row held half a second (no tap then); `LedgerTabsBar.Phone`
  the tabs' row and sizes; `DeploymentPanels.Phone.cs` the screen over the whole screen (the backdrop too,
  `UiPalette.DrawWaveBackdrop`'s size), its cards with their ✕, a drag between them as on the PC, the bar with a
  defence's Plains and City and a search's note; `DeploymentPanels.PlaceTable` places the ledger for `GameScreen` and the
  screen gallery alike; `SubordinatesView.Phone.cs` the orders in the sides; `TreasureCallout.PlacePhone` lays the
  callout over the whole screen at the phone's sizes (its figures a `Metrics` record whose PC values are the former
  constants; `PlaceOnDevice`, `FromGlobal`): beside its anchor it stands on the room's foot, its tail to the anchor on
  either hand, the answers filling its width with Yes, red, at the right; `UnitPictureView` the picture over the screen
  dimmed, put away by a tap.
- **The agent's choices**, for the owner to confirm: the deployment's Refill question as on the PC, under or over its
  cell, at the phone's sizes (LD2 drew none); a step's question in a callout right of the step, on its foot (LU1q drew
  Replenish's only); Yes at the right in every phone callout (nearer the right thumb), the PC's order unchanged; the
  picture by a long press raised over the frame at the frame's height (PD2d's "2.2 ×" was upright's width); the party
  cards' names shrunk to the room between the badge and ✕ as LD2 drew them, the troops' bar across the card's text
  column (LD2's was short); the result plate after an order (R4/R5) stays the PC's in the frame's middle; on the phone
  the picture panel of the PC's top right is not drawn (the long press shows it).
- **The gallery**: the PC and touch passes same (110 and 111 entries); the phone pass's `deployment…` and
  `subordinates…` entries match LD2, LU1 and LU1q (docs/ui/gallery.md, §9), which left `GALLERY_ENTRIES_LAND`. A touch
  pass hung once in a window waiting for a frame (no CPU used) and ran through when started again.
- **Unattended play**: `--phone landscape` headless from the week-18 save to its deployment picker runs as before (the
  replenish through Subordinates, the party built in the ledger, the attack on Oaks (6)).

**Step L6, where it stands (2026-10-28):** built, **awaiting the owner's look on the phone**.

- **The model** (`Original.Content/Devices/PhoneAttack`, tested in `PhoneAttackTests`): the places at the mock-ups' figures
  (`la2_base`, `la2n`, `ys_panel`, `rebels_panel`, `attacker_card`, `maris`, `ninja_callout`) in the canvas's units — the
  province's panel (580 wide from the hole's strip + 6, from 16 down to 16 over the screen's foot), "VS" in the 100 after
  it, the army's panel to 16 short of the column, the column 804 wide ending 6 short of the right strip (a longer screen
  widens the army's panel); the province's picture (540 wide, the PC's 3:2) and its four figures' rows (88) centred in
  the panel, the army's cards (300 high, two a row, 16 and 14 apart, the front row left) with their captions, the pointer
  and the totals centred in theirs; the column's head (the flag, the title, the date, the news in two lines, the divider
  at 286); Intercept 130 high 10 over the gesture zone, Abandon City (90% of its half, 18 in from the row's top and foot) |
  Ninja Scout (180) 16 over it, Maris's panel (320) 16 over them; Ninja Scout's list 30 over its button, the column's
  width, its rows 128 high 12 apart, two a row when more than one ninja serves.
- **The views**, keyed by the live device: `EnemyAttackView.Phone.cs` draws the screen over the whole screen on the wave
  backdrop (the PC's blur under it hidden), the hit tests and the list's rows at `PhoneAttack`'s places; Abandon City's
  question in the callout above its button, at the phone's sizes (laid out again with the screen, so a question open when
  the layout changes follows its button). `PhoneCommanderCard` is the scouted commander's card the scout report (L4)
  drew, now shared by both (its name and troop type shrunk to the card's room, as the mock-up's narrower cards need).
  `TreasureCallout.PlacePhone` lays the callout over the screen the frame was laid out for and keeps a callout out of
  the hole's strips.
- **The agent's choices**, for the owner to confirm: the province's figures in the game's font as LA2 drew them (the PC's
  values are Cinzel), the rules between them the PC's seams; a place not scouted yet and an empty one drawn as the PC's
  (dashed, "?" / the number and "empty") at the phone's sizes, which LA2 did not draw; the list's spent ninjas "used this
  week" as on the PC (the mock-up's "spent"); Abandon City's question the PC's words in the callout above its button,
  as wide as its lines (it reaches over the army's panel), Abandon at the right.
- **The gallery**: the PC and touch passes same (110 and 111 entries); the phone pass's `enemy-attack…` entries match LA2
  and LA2n (docs/ui/gallery.md, §9), which left `GALLERY_ENTRIES_LAND`; `scout-report` and the L5 callouts same.
- **Unattended play**: `--phone landscape --turn-end --scout` headless from the week-18 save to its deployment picker runs
  as before (Ninja Scout on Ys (5), Back from the deployment screen, the interception).

**Step L7, where it stands (2026-10-28):** split in two, both built. **L7a (Harem and Treasure) and L7b (Save/Load as LL1,
the comment's editing as LY1) are awaiting the owner's look on the phone.** L7a:

- **The model** (`Original.Content/Devices/PhonePickers`, tested in `PhonePickersTests`): the places at the mock-ups'
  figures (`lh2`, `lt2`, `folded_grid`, `column_title`, `girl_details`, `item_details`, `harem_cell`, `treasure_cell_two`)
  in the canvas's units — the column at the left (1104 wide from the hole's strip + 6) with the title band (the divider at
  110), the picture at the column's width in the PC's shape (666 × 416; the mock's dump crop was 663 × 412), the details 12
  under it; the harem's End (320) | Summon 140 high 10 over the gesture zone; the treasure's Characters | Items and End
  (320) 130 high, its touch mode button (440 × 150) beside the details, a first pick's prompt and Back (220) in the tabs'
  place, a callout's room left of the button; the grid from 40 right of the column to the hole's strip over the whole
  height, folded after its ninth row (the halves 40 apart, the cells 8 and 6, the rows as high as nine fill the grid — the
  mock's whole-pixel rows left 7 at its foot), `CellAt`, the gold rule between the halves. A longer screen widens the grid.
- **The views**, keyed by the live device: `HaremView.Phone.cs` and `TreasureView.Phone.cs` (their `Place` switches to the
  phone's layout over the whole screen on the wave backdrop; the hit tests at `PhonePickers`' places), the parts they share
  in `PhonePickerParts` (the title band, an empty cell, the rule, a plain button); `TreasureView.ShowCallout` places the
  script's questions and refusals for the game and the screen gallery alike — on the phone in `TreasureCallout.PlacePhone`'s
  layout beside the button, standing on its foot.
- **The agent's choices**, for the owner to confirm: the girls' names in their cells in Cinzel as LH2 drew them (the PC's
  are the brush face), a visit's mark (NEW, ???) at her cell's top right; "This visit" as the details' third column as on
  the PC (LH2 drew two), the values shrunk so the three fit; the treasure's subtitle ("Equippable") after the name as on
  the PC (LT2 put it before the figure's label, "EQUIPPABLE · HELD BY"), a unit's three figures in columns, the values shrunk
  to the room; the button's word in the PC's Cinzel (LT2's brush), greyed with the refusal under it; a first pick's prompt
  (the unit's face, "Rance is equipping —" over "Which item?") and Back in the tabs' place while it waits (the tabs drop the
  pick as Back does), which LT2 did not draw; a callout over the picture's foot and the details, left of the button; a held
  item's name over its holder, an item in stock and a unit in one line (a unit's diamond, Chaos and Nikkou's padlock as on
  the PC); with nothing selected the PC's hint in the details' place.
- **The gallery**: the PC and touch passes same (110 and 111 entries); the phone pass's `harem` and `treasure…` entries
  match LH2 and LT2 (docs/ui/gallery.md, §9), which left `GALLERY_ENTRIES_LAND`.
- **Unattended play**: `--phone landscape` headless from the week-18 save to its deployment picker with `--treasure` (our
  Treasure screen clicked through) and with `--harem 6` runs as before.

L7b:

- **The model** (`Original.Content/Devices/PhoneSaves`, tested in `PhoneSavesTests`): the places at the mock-ups' figures
  (`ll1`, `land_save_list`, `save_row`, `land_save_page`, `load_save_chips`, `land_back`, `bin_button`, `ly1`) in the canvas's
  units — the list at the left (884 wide from the hole's strip + 6) with the title band (the divider at 110), Load | Save
  (200 × 80) at its right, the lists' tabs (96 high from 124), the heading with the sweep (440 × 64) and the rows (104 high,
  10 apart, from 320 to 10 over the gesture zone: seven in view, six under New save, 88 high, in Save), the scroll bar's
  groove 14 in from the list's right; the page from 40 right of the list to the hole's strip — its title band (100, Delete
  240 × 84 at its right), the map at its width cut at its foot for the figures (two rows of three, 92 apart) and the comment
  (its box 100 high, Edit 204 wide; an auto-save's two lines 40 apart under it, the map cut 94 lower for them), Back (360)
  | the red button 150 high 10 over the gesture zone; Delete's question (900 × 250) hanging under it, the sweep's (1000 ×
  250) right of it; the comment's card (360 high) 24 over the keyboard Android reports, else over the mock-up's 650 of the
  1220 — the field in one line (130 high), the count under its right, Cancel | Keep (330 × 130). A longer screen widens
  the page.
- **The views**, keyed by the live device: `SaveLoadView.Phone.cs` draws the screen over the whole screen on the wave
  backdrop (a layer behind the view, drawn once), the places a press and unattended play's points look at are
  `PhoneSaves`' there (`SaveLoadView`'s `…Box` members), a finger scrolls the rows at the phone's pitch
  (`SaveLoadView.Touch.cs`). Edit raises the card over the dimmed screen with the comment's field in it; Cancel, Keep, the
  keyboard's ✓ (Enter) and Back answer it, a tap elsewhere does nothing.
- **The keyboard** (§7): Godot's Android field asks for `IME_FLAG_NO_EXTRACT_UI | IME_ACTION_DONE` (its `GodotEditText`,
  read from the 4.7.2 export template's `godot-lib`), so Android shows no full-screen editor held landscape (LY2 does not
  happen) and the keyboard's ✓ is Done, which Godot sends as Enter. The card follows the height
  `DisplayServer.VirtualKeyboardGetHeight` reports (a PC playing a phone has none: the mock-up's place); the first height
  a launch sees is logged ("Android's keyboard is … px high of the window's …") — the Poco's figure is to be read from
  its log at the owner's look. Back with the keyboard up closes it (Android's own): the card takes a keyboard reported up
  and then down for 0.4 s, nothing pressed, for Back and leaves the comment as it was; Back over the card (the keyboard
  down) does the same (`GameScreen.BackAsRightClick`, on the tablet too, where Back left the save screen before).
- **The agent's choices**, for the owner to confirm: the rows' date and time in the game's font as LL1 drew them (the PC's
  date is Cinzel), the time quiet; the chosen row, list tab and chip in the PC's vermilion frame (LL1 drew the chosen row
  and tab gold, the chosen chip red); the groove stays when every save fits, as the PC's; New save over the rows in Save
  (LL1 drew Load only); an auto-save's kind on its tag left of the time, Auto and its kind after the page's title, its two
  lines on its load under the comment, the map cut lower by them (LL1 drew no auto-save); in Save what a save writes
  ("Saving: …") as a line under the red button's word (the PC has it beside the button), an auto-save's button greyed as
  on the PC; Delete's and the sweep's questions in the PC's words at the phone's sizes (LL1 drew neither); the card's
  title "No. 3 · LP 3 · Month 4 · Week 4" (an auto-save's "Turn Start auto-save · …"), a tap off the card doing nothing
  (the PC's click off the field keeps).
- **The gallery**: the PC and touch passes same (110 and 111 entries); the phone pass's `save-load…` entries match LL1 and
  LY1 (`save-load-comment`: the card where LY1 has it, no keyboard drawn), which left `GALLERY_ENTRIES_LAND`.
- **Unattended play**: `--phone landscape --saveload` headless from the week-18 save to its deployment picker clicks
  through the whole visit at the phone's places (the red button, the sweep, Edit, Delete, the auto-saves, the Load tab,
  Back) and plays on. Headless, `--phone landscape` now gives the root window half the reference's shape (1356 × 610, the
  canvas 1778 × 800 as the gallery's phone pass): before, the headless window was square and a phone layout's places fell
  off the canvas, which only clicks at a view's places (as `--saveload`'s) notice.

**Step L8, where it stands (2026-10-28):** built, **awaiting the owner's look on the phone**.

- **The models** (tested in `PhoneOptionsTests`): `Original.Content/Devices/PhoneOptions` — the places at the mock-up's
  figures (`lo1`, `messages_columns`, `paper_button`) in the canvas's units: the five tabs (330 × 110, 10 apart, from the
  hole's strip + 6; one not chosen 12 lower), the title's right at the sheet's less 4, the sheet from 126 to 10 over the
  gesture zone, its two columns 40 in from its sides and 30 either side of its middle from 36 under its top, the rule
  between them 40 in from its top and foot, Defaults (380) | Close (to the right column's end) 130 high 30 over the
  sheet's foot; the question's box the PC's at the parts' scale over the sheet's middle; `PartScale` (2.32 reference pixels
  a unit of the PC's parts: their names 17 → 40, their explanations 13 → 30, their boxes 20 → 46, PO1's type). A longer
  screen widens the sheet and its columns. `Original.Content/Devices/PhoneLadder` — the rail 356 wide 16 in from the
  frame's right, from 10 over the gesture zone up: Descend (150), the floor's block (110), Ascend, Retreat, 14 apart; before
  a search Start Search in Descend's place, the other items over it, Return on top; the panel 14 round them.
- **The views**, keyed by the live device: `OptionsView.Phone.cs` lays the window over the whole screen on the wave
  backdrop (a layer behind it), the PC's parts drawn at `PartScale` — their geometry by the drawing's scale, their text
  through `Text` at the canvas's own size, so it stays sharp (on the PC `Text` is `DrawString` as before) — at the
  columns' places; the tabs, the title, Defaults (a paper button, deaf on Game) and Close (red, the brush face) at
  `PhoneOptions`' places. The PC's tabs share their parts with the phone's (`SpeedSlider`, `MessageSkipColumn`,
  `FontSection`, `PaletteSection`, `ThousandsSection`, `RowsSection`, `EffectsSection`, `TimingRow`, `GameSection`,
  `ForecastBox`, `ReachRadio`, `NewScenesBox`); a check box's hint wraps into a column on the phone. `GameScreen` places
  the open window every frame, so a phone turned lays it out again. `FloorLadderView.Phone.cs` draws the rail at
  `PhoneLadder`'s places, the hit tests the same boxes.
- **The agent's choices**, for the owner to confirm: Messages' preview is the game's 121 at the reference's own pixels (its
  text at PO1's 2.7 mm), centred in the column (LO1 drew a box of the column's width, its text at the left); the read
  text's two explanations wrapped in the column as one paragraph (LO1 drew the first); the other tabs, which LO1 did not
  draw — Display: the font left, the palette right, the thousands and the unit tables' row heights across the sheet (their
  five tiles need its width); Battle: the effects across, the timings in the two columns (five left, four right, over
  Defaults); Game: the folder across; Tweaks: the line on what they are across, Battle left with "Chaos and Nikkou reach
  demons"' three choices one under another (a column has no room for them side by side), Scenes right; the rule between
  the columns only where a tab has two; the PC's parts' look kept (Cinzel tabs, the chosen one in bold, the slider and
  boxes as the PC's, lines at the parts' scale); a question (Forget, the game folder) the PC's box at the parts' scale
  over the sheet's middle, the screen dimmed. On the ladder the way back's arrow and sub-line quiet, the way deeper's
  gold, as on the PC and the tablet (LF2 drew both arrows gold); Start Search framed in gold as on the tablet; the
  dungeon's other items (Merim, God's Door) as rows the rungs' height with their words centred between Return and Start
  Search (LF2e drew none).
- **The gallery**: the PC and touch passes same; the phone pass's `options-…`, `floor-ladder…` and `dungeon-entry…`
  entries match LO1, LF2 and LF2e (docs/ui/gallery.md, §9), which left `GALLERY_ENTRIES_LAND`. The gallery's entries draw
  no Menu cell in the left side (the game's, LF2's "the left as LF1's").
- **Unattended play**: `--phone landscape --dungeon` headless from the week-18 save runs as before (the menu before the
  search, the battle on the Monsters' Cave's (dungeon 2) first floor); unattended play answers the dungeon's menus
  without the rail's places, so step L10's runs are its check.

**Step L9, where it stands (2026-10-28):** split in two, as L7 was, both built. **L9a (All Data, the title, the original's
own screens and a finger's press) and L9b (the import screen as LI, LIp, LIr) are awaiting the owner's look on the
phone.**

- **The model** (`Original.Content/Devices/PhoneData`, tested in `PhoneDataTests`): the places at the mock-ups' figures
  (`side_graphs`, `graph_cell`, `lg2`, `scale_chips`, `funds_relabelled`) in the canvas's units — the four graphs' cells
  (132 high, 14 apart, the last 10 over the gesture zone) on the left side's rail under GRAPHS, Back (150 high) on the right
  side's rail at the thumb, the eight chips two a row (112 high, 12 apart) 30 over it under SCALE; a bar's figure centred on
  its bar, its baseline 6 layout pixels over the bar's frame line, at 2.3 mm (40). The presenter keeps each bar's figure
  (`GamePresenter.DataFigures`, the bar's left and top and its number) and leaves them off the screen while
  `DataFiguresCovered`; it names the graphs, Return and the scales as the menus print them and gives the shown graph's
  scale (`DataGraphNames`, `DataReturnName`, `DataScaleNames`, `DataScale`; tested in
  `GamePresenterTests.All_data_keeps_the_figures_and_names_for_the_phone`).
- **The view** (`godot/Strategy/AllDataView`, wired in `GameScreen.Data.cs`), only on the phone held landscape: over the
  map's view the menu of graphs is ours — the PC's box not drawn, a cell answers it with its graph, Back with Return
  (LG2m); over a graph the cells (the shown one lit), the chips (the shown scale lit, vermilion) and Back, and the figures
  drawn again over the bars (LG2). While a graph waits for the click a tap of the sides is queued as the original's clicks
  (`_dataPointer`): Back the left click that leaves it, a chip the right click and the scale menu answered with it, another
  graph's cell the left click and the menu of graphs answered with it, unseen; a tap on the frame stays the original's
  (the view takes presses only on its parts then). The screen gallery shares the view (`GalleryEntries.Data.cs`).
- **The title and the original's own screens** (LE1, LXf, LXc, LXm, LXr): already in the frame as on the PC with the Menu
  cell on top of the left side (LEd, the title's description with Skip and Log, came with L1). On the phone a long press
  on them is the right click (`GameScreen.OriginalScreens.cs`, as a battle card's: the press held back, lifted sooner a
  tap) — the title's descriptions without Back.
- **A tap's move before its press** (§7), on the tablet too: a finger's press that lands elsewhere moves the pointer at
  once and reaches the script's reads only after a read has taken the move (`Engine/Script/TouchPress`, tested in
  `TouchPressTests`; `PlayerInput.Press`, used by `GameScreen.PointerButton` in the touch mode) — so Fortune Check and the
  CG gallery act on what the finger touched, not on what the read before lit. A press no read takes goes in after the
  frame after its own and ages there as any tap.
- **The agent's choices**, for the owner to confirm: the graphs' names in the game's font, split at the first space; the
  scale menu (a long press on a graph) shows as the graph with its chips, which answer it — Back cancels it (the graph
  left, as the menu's own cancel), the shown graph's cell keeps the graph at its scale, another graph's cell cancels it and
  opens that graph; a long press or Back over the menu of graphs is Return; a tap on the shown graph's cell or the lit chip
  does nothing; the figures in the mock-up's light grey; the long press on the original's screens on the phone alone (the
  tablet keeps Back as its right click there).
- **The gallery**: the PC and touch passes same; the phone pass's `all-data…` entries match LG2m and LG2, which left
  `GALLERY_ENTRIES_LAND` with LE1, LXf, LXc and LXm (LXr stays: the ending's report has no entry of the game's gallery).
- **Unattended play**: `--phone landscape` headless from the week-18 save to its deployment picker, with `--saveload` and
  `--treasure` (their taps through Godot's input, now through `TouchPress`), runs as before. The bot visits All Data only
  from the prologue (stage 1), through the script's own menus.

L9b, the import screen:

- **The model** (`Original.Content/Devices/PhoneImport`, tested in `PhoneImportTests`): the places at the mock-ups' figures
  (`import_land`, `sheet_head`, `step_mark`, `step_text`, `folder_field`, `hash_lines`, `stamp`) in the canvas's units — the
  lacquer panel 10 in from the frame's sides, from 16 under the screen's top to 4 over the gesture zone, the paper 26 in;
  the heading's middle 90 under the paper's top (66, the brush face), the version's 166 (28, Cinzel), the rule 206, the
  steps' first line 262; the columns 50 in from the paper's sides and 40 either side of the frame's middle, a step's seal 34
  in (34 round), its words 100 in (the title 48 with 62 between lines, its lines 38 with 50), 14 under a step; the field 150
  high; the bar 44, its share 92 under it, Play 200 under it; the hashes 30 with 40 between them; the stamp 260 in from the
  right column's left, 110 round; the red button 130 high 30 over the paper's foot at the right column's end (Import 520
  wide, Choose another folder 640), Back level with it — at the left column's foot ("Back to the title"), once refused from
  the right column's left to the red button ("Back"). A longer screen keeps the sheet in the frame. The words: a folder as
  its name over its place (`FolderLines`), a path as its folders joined by " › " with Android's own storage
  (`/storage/emulated/0`, `/sdcard`) named "Internal storage" (`Readable`, also in a refusal's message), the hashes in two
  lines (`HashLines`).
- **The view** (`godot/ImportScreen.Phone.cs`), keyed by the live device in the screen's own `Layout`: on the phone it lays
  itself over its parent's whole area (the gallery gives it the frame's picture) and draws in the frame's layout pixels
  (`PhoneUnits`); the hit tests read the same boxes (`Button`, `BackBox`, the field as drawn). Android's Back is the way back,
  as Esc is (on the tablet too). The screen gallery's folder on the phone pass is one in Android's storage
  (`GalleryEntries.SampleFolder`), as LI's.
- **The agent's choices**, for the owner to confirm: the steps' colours by the PC's roles (a step done quiet, one to come
  grey, a seal's ring vermilion, a step to come's grey), where LI drew the folder's title dark and every ring quiet; a step
  done in the right column (the scenario read) ticked at its column's right as the PC ticks it; the app's own folder
  (`…/Android/data/org.kichikuou.port/files/game`, where `-PushGame` copies it) shows as "game" over "Internal storage ›
  Android › data › org.kichikuou.port › files", cut at its start where it does not fit; on the PC playing a phone a path as
  "C: › Games"; the hashes in the PC's small grey, each line shrunk to the sheet's width; the red button's word in the brush
  face (LI; the PC's stays Cinzel), greyed with no folder chosen (a first launch, with no Back); refused at a first launch no
  Back either (LIr drew the way from Option → Game).
- **The gallery**: the PC and touch passes same; the phone pass's `import-…` entries match LI, LIp and LIr, which left
  `GALLERY_ENTRIES_LAND` (LXr stays). `--phone landscape --import-screen DIR --quit` headless imports as before (its dumps
  need a window).

**Step L10, where it stands (2026-10-28):** the agent's part done; **the landscape phone awaits the owner's play on the
Poco** (every step's look above, the agent's choices with them), and the tablet's, unchanged (§7).

- **Unattended play** headless as the phone held landscape (`--phone landscape`, the 1778 × 800 canvas) from the week-18
  save, each exercise in a working folder of its own (`--saves user://saves-look-phone-…`; CLAUDE.md, "A look at one
  screen"): every run ended as asked, exit 0, nothing unported met.

  | Switches | What it went through | Time |
  |---|---|---|
  | `--until battle-end` | Replenish of Haurein (unit 12) on our Subordinates screen; the attack on Oaks (6), the party built in our deployment table by drops, its first two places swapped and back; the battle to its end (lost, as with `--seed 2`) | 10 s |
  | `--treasure --until deployment` | our Treasure screen on Rance (unit 1) and the L. Holy Sword (item 6), then the attack's deployment | 5 s |
  | `--saveload --until deployment` | our Save/Load screen: a new manual save by the red button, its comment written, left by Back | 6 s |
  | `--turn-end --scout --until deployment` | the rebels' attack on Ys (5) on our enemy attack screen, Ninja Scout, the deployment left by Back and the attack screen back in 237 ms | 6 s |
  | `--refill --until deployment` | Replenish on the deployment table: Haurein (unit 12) asked and replenished | 5 s |
  | `--harem 6 --until deployment` | the harem's cell 6, Kanami's visit | 7 s |
  | `--dungeon --weeks 18` | the Monsters' Cave (dungeon 2) searched with Rance (unit 1) to floor 4, a battle fought, the floor menus answered to the way out; then the week's Replenish and the rebels' attack on Ys (5), won | 12 s |
  | `--weeks 21` | weeks 18-21 whole: Oaks (6) lost in week 18 and taken in week 19 (stage 2), the attacks on Ys (5) and, twice in week 20, on Leazas Castle (1), all won | 52 s |

  Unattended play answers the screens through their models and Godot's input, not the phone's places alone, so these
  runs check the flows on the phone's layouts; the places are the gallery's phone pass's (114 entries) and the owner's
  look. The windowed exercises (the battle's touch rails and their dumps, `--map-north`) wait for the owner's ask, as
  always.
- **The docs**: phone.md (§24), touch.md ("Phones (put off)": the Poco the reference, the landscape phone built), style.md
  (§6, the phone held landscape), the backlog ("Android").
- **Left open**: the Poco's keyboard height (step L7b; no Kichikuou keyboard line in its log at L9b's install); the
  battle's long frame or two at an exchange's start (backlog, "Android"); an effect's first play in a launch no longer
  draws its masks (step F, "The battle's speed"; 1060 ms on a worker at the Poco's launch).

### Upright (steps P0-P9)

| Step | What | Base versions (phone.md) |
|---|---|---|
| **P0** | The upright foundation: the orientation choice in Option → Display (§3), `--phone upright`, the gallery's `phone` pass; `PhoneFrame`'s upright rectangles — the engine's picture at the screen's width wherever a screen puts it, and the layout area apart from it. Until P8 a screen not built yet shows the tablet's layout fitted to the width. | PO1o (§21) |
| **P1** | The scenes: the picture in the screen's middle, the upper speaker's window above it and the lower one's under it (a thought by its thinker's portrait the same way; the narration and a speaker with no portrait under it), no name plate; the script's breaks kept (32 cells at 3.6 mm, 49 at 2.6 mm), four lines' height so the picture never moves; 131 under its CG with its plate; a `]` menu's rows at the thumb in the bar's place; the bar with Menu, Log and Skip at its left; the Menu sheet and the log (round P20); the week report's card; the title's description and the requests slip. Which window is "upper" is surveyed first (the windows' kinds and places, `WAO_SUB`). | PS2, PS2l, PS2b, PS2m (§18); PK1 (§21); PW1 (§8); PEd, PEr (§22) |
| **P2** | The map: the whole height at the width, the status on top, the bar with Commands at the right thumb; the command panel as a sheet over the map's foot; a province ringed, its card and commands as a sheet, the map scrolled up by the sheet's height. | PM1a, PM1b, PM1c (§3) |
| **P3** | The battle's turn: our army on top, the enemy's at the thumb, the gauge across between the front rows; Skip on its strip; the deck (the acting unit's commands from the foot up, the target's attacks at the right thumb); the retreat question over the deck's right column; an exchange. | PB3, PB3r, PB3x (§6) |
| **P4** | The battle's other states. | PB4o3 (§20), PB4s, PB4c, PB4r, PB4b (§11) |
| **P5** | The deployment table (the party on top, the ledger at the thumb in two-line rows scrolled sideways — № and name over the type fixed, each tab its own scroll) and Subordinates (the orders under the ledger, Replenish and Talk on one row, End at the thumb with the budget beside it, the question under its button); a unit's picture on a long press. | PD2, PD2d, PU1, PU1q (§7, §20) |
| **P6** | The enemy attack: a screen of its own, Ys over the rebels, the answers at the thumb; Ninja Scout's list above its button. | PA1, PA1n (§8) |
| **P7** | Harem and Treasure (the picture on top, the grid scrolled), Save/Load on one page (the page on the bar, the list above it), the comment's editing as round P20 chose. | PH2, PT2, PL2 (§9) |
| **P8** | The options window (the paper sheet, its tabs on top) and the dungeon's ladder at the right thumb; All Data (the miniature on top, the graphs as tabs, the scales as chips); the title with a row for each button; the import screen. | PO1, PF1, PF1e (§10); PG2m, PG2, PE2, PI, PIp, PIr (§22) |
| **P9** | The phone whole: unattended play with `--phone upright`, turning the phone in every screen (each laid out again, nothing lost: a drag, a question open, a scroll), the owner's play on the Poco, the docs. | — |

**Step P0, where it stands (2026-10-29):** built, awaiting the owner's look on the Poco (the orientation's four ways
and System's rotation lock).

- **The frame upright**: `PhoneFrame.Upright` (tested, `PhoneFrameTests`): the safe insets — the system's when it reports
  them, else the reference's hole of 110 px at the top and nothing at the sides — and at least the gesture zone's 50 px
  at the foot; the `Room` our layouts may take (the screen less the insets); `PictureAt(top)`, the engine's picture at
  the room's width wherever a screen puts it (0.953 reference pixels a layout pixel, 762.5 high); `Frame`, its default
  place in the room's middle; `Unit` the screen's width over 1220, so the free height flexes with a longer screen.
  `PhoneFrame.For` picks the way by the shape (a square one landscape). `Device.Frame` hands out either,
  `Device.Upright(view)` the upright one as `Device.Landscape(view)` the landscape one; `ScreenFrame.In` puts the
  picture at the upright `Frame`, and `GameScreen.UpdateDevice` the view there.
- **Until P8 a screen not built yet shows the tablet's layout fitted to the width**: every view whose phone branch is
  keyed by `PhoneLandscape` falls through to the tablet's path upright and lays itself over the picture, as on the
  tablet. Until P1 the upright phone has **no Menu** (the menu bar is gone on a phone and the bar with Menu, Log and Skip
  comes with P1): Option is reached held landscape (System turns the game with the phone).
- **The orientation** (§3; PO1o): `godot/Strategy/OrientationChoice` — System (the default), Upright, Landscape 1 (the
  phone's top at the left), Landscape 2 (at the right), kept in `[display] orientation`, applied on an Android phone at
  the launch and at each change (`DisplayServer.ScreenSetOrientation`: Sensor, Portrait, Landscape, ReverseLandscape —
  `fullUser`, `portrait`, `landscape`, `reverseLandscape` to Android). The tablet keeps the project's sensor landscape
  and is never offered it. On a PC playing a phone a choice turns the window (`Device.TurnWindow`), so a turn can be
  tried without the device; System leaves it. Option → Display shows the section on a phone only: held landscape (LO1)
  right of the palette, its four ways two a row, the chosen way's line under them; upright, in the tablet's layout until
  P8, beside the font. Defaults sets System.
- **`--phone upright`** on the PC: the window at the reference's upright shape fitted to the monitor's usable area
  (608 × 1352 on the owner's 2560 × 1440 monitor), headless the canvas at half the reference (610 × 1356). `Device`
  reads the root window's size (headless has no window to ask) and logs each change of the layout on the PC.
- **The gallery's pass** `phone-upright` ("Smartphone, upright"; `gallery.ps1 -PhoneUp`), beside the mock-up tab `phone`,
  which stays as it is: every entry dumped with `--phone upright --window 610x1356` — the whole window at **half the
  reference's pixels** (two thirds, 813 × 1808, would not fit a 1440-pixel-tall monitor). At P0 it shows the tablet's
  layout at the width in the room's middle (111 entries): the baseline P1-P8 change entry by entry. The Lacquer and
  Touch passes were "same" (110 and 111 entries), the landscape pass "same" but `options-display`, which shows the
  orientation (113 of 114).
- **Unattended play** headless with `--phone upright` from the week-18 save to its deployment picker
  (`--load --auto --fast --quit --seed 2 --saves user://saves-look-phone-up --slots-from user://saves-week18 --until
  deployment`): the replenish on our Subordinates screen, the attack on Oaks (6), the party built in our deployment table
  and its first two places swapped and back, exit 0 in 5 s.
- **On the Poco** (the install of 2026-10-29): `adb shell dumpsys activity activities` reads the game's activity as
  `requestedOrientation=SCREEN_ORIENTATION_FULL_USER` (from the manifest's `USER_LANDSCAPE`) with System chosen, and the
  game's log "the orientation System, the screen's Sensor".
- **Left open**: System's rotation lock on the Poco — the owner's turn of the phone with the lock on and off.

**Step P1, where it stands (2026-10-29):** built, **awaiting the owner's look on the Poco**.

- **Which window is upper — the survey** (`WAO_SUB`, `Original/Script/OriginalScriptContext.Wao.cs`; docs/ui/messages.md,
  §1). The script leaves no choice: every window is told by its op, and each op belongs to a portrait's place. The left
  portrait (`311`, `Ui_PortraitL`) stands at the picture's top left (384×512 at (0, 0)), the right one (`321`,
  `Ui_PortraitR`) at its foot right ((896, 288)); 111 (speech) and 112 (thought) are the left one's, their frames at the
  picture's top (807/808 at (400, 6)); 121 and 122 the right one's, at its foot (810/811 at (64, 518)); 113/123 (the
  narration, a place's title at `ZM 32`, the status windows) sit at the foot (809 at (256, 542)); 131 is the bar under
  the event CGs (815 at (0, 588)), which clears every picture. So the **upper speaker is 111/112 while the left portrait
  shows**; 111 opens far more often than `311` (12,664 against 1,982 scenes — mostly Rance's lines, who has no portrait),
  and such a window has no portrait to stand by: it goes under the picture with the rest, as the owner's rule for a
  speaker with no portrait says (phone.md, §18). A tail or a thought's bubbles point at a portrait only while it shows.
- **The frame** (`PhoneFrame`, tested): `Bar`, the bar at the room's foot (PK1: 148 high, 16 from the sides, 10 over the
  gesture zone; reference 16, 2504, 1188 × 148), and the picture's default place `Frame` now in the middle between the
  room's top and the bar (mock_phone.py's `SCENE_Y`, 925.75) — every upright screen not built yet moves up with it.
- **The model** (`PhoneScenes`, `PhoneMenu`, tested in `PhoneScenesUprightTests`): Menu, Log and Skip are the bar's cells
  at its left (170 wide, 14 apart); `Window` places a message window — across the screen (16 from the sides), 64 from the
  picture, 420 high (five of the script's lines at their type, so the picture never moves; 131 330), above the picture for
  the left portrait's while it shows, else under it; the glyphs at their cells from the text box's corner (the window's
  new `TextRect`) at a speech's 3.6 mm at most (131's 2.6), smaller when the text box would not fit (131: 2.5 mm), the
  rows 1.08 times the cells' pitch apart (the script's size + 2 read too close); `SceneMenu` upright — rows across the
  screen (130 high, 14 apart) from the bar's foot up under the lower window, lower for a long menu down to 0.6 of their
  height, then rising over the window's foot; `LogPaper` over the screen from the hole to the bar, `LogClose` in Log's
  place, `NamesOver` (each speaker's name on a line of its own over his lines, PR2); `ReportScale` at the bar's width;
  the Menu sheet's rows (`PhoneMenu.Tiles(f, tiles)`: 130 high, 12 apart, ending 40 over the bar, the first page from the
  thumb up — Options lowest, a rule between Save Screenshot and the switches, Debug on top —, Debug's page top down with
  ‹ Menu on top; the panel round them with "Menu" over them; a question's callout across the screen over its row).
- **The views**, keyed by the new `Device.Held(view)` (the phone's frame held either way; the landscape paths unchanged):
  `SkipDrawer` (the two cells together), `SideCells.Draw` (a tall cell: the icon over the word), `MenuSheetView` (rows:
  the word at the left, "…" after a command that asks, a switch's pill reading On or Off, a page's ›), `SceneMenuView`
  (upright over the whole screen, the rows in the bar's place), `MessageLogView` (PR2: the paper, the names over the
  lines, the narration at 2.5 mm, Close in Log's place), `WeekReportView` (PW1: the card at the bar's width in the
  frame's middle). `MessageWindowView` (keyed by `Device.Upright`) draws the window over or under the picture with its
  tail or bubbles at the portrait (`Portraits`, from `Ui_PortraitL`/`R`, set by `GameScreen`), 131's plate larger and the
  original's bar at the picture's foot hidden, a place's title scaled into the panel; Option → Messages's preview stays
  in its sheet (`Leaves = false`). `GameScreen.Phone.cs`: the Menu cell and the drawer show upright as held landscape
  (the cell hidden while a scene's menu takes the bar).
- **The title's description and the requests slip** (PEd, PEr) need nothing of their own: window 113 is the narration's
  (under the picture), the slip is the script's own picture in the frame.
- **The agent's choices**, for the owner to confirm: a left window with no left portrait goes under the picture; the
  rows of a long scene menu shrink to 0.6 of their height before they cover the question's window; the lines 1.08 times
  the script's pitch apart; 131's type 2.5 mm (its text box, 52 cells, at the width) instead of the mock-up's 2.6.
- **The gallery**: the PC, touch and landscape passes same (110, 111 and 114 entries); the upright pass 111 changed (the
  picture moved up to its place over the bar, the Menu cell in the bar) and 3 new (the Menu sheet's); the gallery's Menu
  cell hidden under an upright `]` menu as the game hides it (`scene-menu`, `all-data`).
- **Unattended play**: headless with `--phone upright` from the week-18 save (`--harem 6`, then the replenish, the
  attack on Oaks (6), the deployment table) to its deployment picker, exit 0 (its dumps are the engine's picture only;
  the layout was looked at in the gallery). No real scene with portraits is dumped upright — the gallery's samples show
  both portraits' places; the owner's look on the Poco will be the first.

## 6. The agent's own choices, for the owner to confirm

- A phone is told from a tablet by the screen's physical short side (under ~90 mm); no setting.
- On a phone the menu bar is gone; its commands are the Menu sheet's.
- The development screens get no phone layout (they show as on the tablet, in the frame).
- Until the upright phase the phone is held to landscape, both ways round by the sensor (since step P0 it turns as
  Option → Display says, System by default).
- In landscape the status lives in the sides, so the map's headroom under the strip stays off on the phone.
- The phone's layouts are written in the reference phone's pixels, so phone.md's figures are the code's.

## 7. Risks and checks

- **Speed** on the phone (the per-pixel engine surface; §3): measured in step F — the scenes and the map at the
  screen's rate, the battle's exchanges at ~21 frames a second (step F, "The Poco"), now ~100 (step F, "The battle's
  speed"); a long frame or two at an exchange's start left.
- **The safe area's values**: the Poco reports a hole of 120 px (not the reference's 110) and nothing at the foot; the
  frame reads Android's, the gesture zone's 50 px kept, and the fallback serves only the PC (step F, "The Poco").
- **The keyboard**: Android shows it for the comment's `LineEdit` (`SaveLoadView`); no full-screen editor (Godot's field
  asks for none), the card over the height Godot reports (step L7, "Where it stands", L7b); the Poco's height is read
  from the log line at the owner's look.
- **System's rotation lock** (§3): System asks Android for `fullUser` (step P0, read in Godot's Android library); the
  Poco's behaviour with the lock on and off is the owner's look.
- **A tap's move before its press** (found in round P20, phone.md §25): Fortune Check and the CG gallery act on a click
  on what the read before it lit, so a tap whose move and press reach the script in one poll would act on the cell lit
  before; the touch mode now holds such a press back until a read has taken its move (`TouchPress`; step L9a), on the
  tablet too.
- **The tablet unchanged**: the PC and touch gallery passes same after every step, and the tablet's play by the owner
  after F and after L10.
