# The touch mode — survey and mock-ups (2026-10-24)

Owner's choice (2026-10-24): the Android build is for **phones too**, not the tablet alone, and its interface
may change more widely than the single fix first asked for (the battle's attack banners show only on a hover,
which a touch screen does not have; docs/backlog.md, "Android"). First a survey and mock-ups of a **touch mode**:
the battle with an action bar, then the map. **The rules stay the original's**: every choice the touch mode
offers is one the core's menu offers (`Original.Content/Battles/BattleCommands`, the command files' menus), as
on PC. PC's mouse interface stays as it is.

**The tablet first** (owner, 2026-10-24, after the mock-ups below): the touch mode is built for the tablet now;
phones come later ("Phones (put off)" at the end, docs/backlog.md, "Android").

The mock-ups are `tools/ui/mock_touch.py`, drawn over window dumps of the week-18 save (the command in its
docstring); the images stay outside the repository.

## The devices

| | Screen | The game's 1280×824 | A layout pixel | Free on each side |
|---|---|---|---|---|
| Phone (the reference: 6.5", 20:9, landscape) | 150.5 × 67.7 mm | fitted to the height | 0.082 mm | 275 px = 22.6 mm |
| Phone 18:9 | | | ~0.09 mm | ~184 px |
| Galaxy Tab S7 FE (the owner's, 12.4", 16:10) | 267 × 167 mm | fitted to the height | 0.203 mm | none |

`GameScreen`'s view keeps the aspect (`KeepAspectCentered`), so on every phone the game's 1280×800 stands in the
middle with black bars left and right. A finger needs a target of **7-9 mm** (85-110 layout pixels on the phone,
35-45 on the tablet).

What our screens give a finger today (mock-up **B0**, **M0**):

| Target | Layout px | Phone | Tablet |
|---|---|---|---|
| Battle card | 282 × 150 | 23 × 12.3 mm | 57 × 30 mm |
| Attack banner | 200 × 40 | 3.3 mm high | 8.1 mm |
| Context menu row (battle, `]` menus) | 30 | 2.5 mm | 6.1 mm |
| Map name plate | ~50 × 30 | 2.5 mm high | 6.1 mm |
| Province menu row | 30 | 2.5 mm | 6.1 mm |
| Menu bar (File, Screen, Option, MsgSkip) | 24 | 2.0 mm | 4.9 mm |
| Card text (17-20 px) | | 1.4-1.6 mm | 3.5-4 mm |

So on the tablet the screens are usable as they are once nothing needs a hover or a right button; on a phone
every small target needs a larger one, and the text is at the edge of legibility (Android's smallest common
text, 12 sp, is ~1.9 mm). The height is the binding side: on a phone the layout cannot grow, only the black
sides can be used.

## How mobile turn-based battles do it (the owner's question, 2026-10-24)

- **No hover: select, then act.** A first tap selects and shows (the target, the details), a second tap or a
  button acts. Nothing irreversible happens on one tap.
- **The acting unit's actions in a bar at the thumbs** (Honkai: Star Rail, Epic Seven, Summoners War, Darkest
  Dungeon on mobile): big buttons at the bottom or the sides, the target **preselected**, so most turns are
  one tap; a tap on an enemy moves the selection.
- **Or the action first, then the target** (Into the Breach, XCOM on mobile): the action lights the targets it
  reaches, a tap on one acts.
- **A long press for details**, rarely for actions; the hover's tooltips open on a tap.
- **On-screen Back / Skip / Menu buttons**: Android's Back is a gesture on many phones, not a button.

## The battle

The acting unit's choices (`BattleCommands`): the attacks on a target (`Banners(target)`: its own attack type,
Melee, the specials — each only where it reaches), the commands without a target (Defend, Wait, the capsules,
the Fog Card, Auto-Atk, Auto-Def, Retreat: `ContextMenu(acting)`), and the other own units' standing orders
(`ContextMenu(own)`, now on a long press).

- **B1 — the target first, two rails (phone).** The black sides become rails in the panel's colours. Left
  (the left thumb): the acting unit's face and name, its commands without a target (Defend, Wait with its
  focus, Auto-Atk, Auto-Def, Retreat in red; the capsules and the Fog Card when it has them), the menu button
  at the top. Right (the right thumb): **TARGET** and its name, the attacks on it as the banners' gold buttons
  (96 px = 7.9 mm). The target is **preselected** (the last one attacked, else the first in the order that an
  attack reaches) and framed by the cyan brackets; a tap on another enemy card moves it; a card no attack
  reaches is dimmed. One tap on an attack acts. The banners on the card are not shown in the touch mode.
- **B2 — the same on the tablet.** No black sides on 16:10: the rails stand in the scene's blurred sides
  (128 px each, the scene's own picture is 1024 wide), a button 112×56 = 23×11 mm.
- **B3 — the action first, one rail (phone).** The right rail lists every command, the attacks first; a tap on
  an attack lights the cards it reaches ("tap a target"), a tap on one of them acts, Cancel on the left rail
  goes back. Fewer buttons on screen (Auto-Atk, Auto-Def and Retreat behind "Orders…"), but always two taps,
  and the player cannot see which attacks reach a given enemy before choosing one.

**Recommendation: B1 on phones, B2 on the tablet** — one design (the rails take the black sides where there are
any, else the scene's sides). It is the usual mobile pattern, one tap for a usual turn, the target's choice
always visible, and nothing chosen by a tap that also changed what the screen shows. The long press keeps
opening the own units' standing orders.

## The map

The map wait has two touch problems: the command panel opens only with the right button (on Android, Back,
which a gesture phone hides), and the plates are 2.5 mm high on a phone. A click on a plate already opens the
province menu with its figures, so the hover's card is not missed: the menu is the "select" step.

- **M1 — rails (phone).** Left: the menu button, the status strip's lines stacked (the date, the budget, the
  troops), **Commands** (the command panel, as the right click opens it) and **Turn END** (red; on touch it asks
  first, being one tap from the map). Right: a tap on a province (its hit area a finger's 8 mm round its marker,
  not the plate alone) selects it — its plate lit — and the rail shows its card: the picture, the name, the
  owner, the figures (or "??" as now) and its actions as big buttons (Attack, Ninja Maneuver, …: the province
  menu's items). The province menu's popup is not shown in the touch mode.
- **M2 — floating buttons (phone).** The map as now, the menu button in a corner, one round **Commands** button
  at the right thumb; a tap opens the province menu as now, drawn half as large again — its rows are still only
  3.7 mm, and the black sides stay unused.

**Recommendation: M1** — the same rails as the battle's, the province's figures before any action, every target
a finger's size. The command panel itself (rows of 30 px) and the other popups need the same enlargement on a
phone: next, after the owner's choice here.

## The rest (not drawn yet)

What else a touch screen does not reach, for the next rounds:

- **The menu bar** (File, Screen, Option, MsgSkip, Debug) is 2 mm high on a phone: the menu button of the
  mock-ups opens the same commands as a list of finger-sized rows.
- **The message log** opens with the mouse wheel up: a button (or a swipe down) at a key wait.
- **The message skip** (F1) is a key: a button, as most mobile visual novels have (Skip, Auto, Log).
- **Hover-only details**: the advantages' tooltips on the battle screen (`AdvantageTip`), the map's province
  card (`MapMarkersView`; M1 replaces it), the pickers' details (Harem, Treasure, Subordinates, the deployment
  table: a first tap shows, a second picks; deployment-table.md already noted it).
- **Small rows**: the `]` menus, the command panel, the save list, the options window — on a phone each needs
  rows of ~90 px, which the 800-pixel height holds only for short lists.
- **Text size on phones** (1.4-1.6 mm on the cards): whether our screens get a phone layout with larger text
  (fewer things at once), or the player zooms. A question for the owner, after the rails.

## The tablet (now)

On the tablet the sizes hold (a banner 8 mm, a row or a plate 6 mm, a card 57 × 30 mm); what it lacks is the
hover and the right button. So the tablet keeps our layout and gets only what touch needs:

- **The battle**, one of two:
  - **B2** — the rails in the scene's blurred sides (above): the target preselected, the attacks as buttons on the
    right, the commands without a target on the left; the banners not shown.
  - **B4** — the banners as now, chosen by tap (no new look, so no mock-up; the dump `00083-banners-E1` is what it
    shows): a tap on an enemy card makes it the target (brackets and banners, kept until another tap); a tap on a
    banner chooses at its release, and only a touch that began after the banners appeared; a tap elsewhere drops
    the target. The commands without a target stay on the acting card's long press.
- **The map — M3** (the tablet's map): the map and the menu bar as now (the bar's items are ~5 mm high, its rows
  ~5 mm: usable), one round **Commands** button at the bottom right — the command panel, as the right click opens
  it —, a tap on a plate opening the province menu as now (its figures are the hover card's). Turn END stays in the
  command panel.
- **The hover's other places**: the advantages' tooltips open on a tap; the pickers' details on a first tap, the
  pick on a second; the message log by a button at a key wait. One by one, after the battle and the map. (The Treasure
  screen, 2026-10-27: a tap selects and a button under the picture acts — docs/ui/pickers.md, §6.)

**The owner's choice (2026-10-24): B2 and M3**, with the target preselected per unit. Built:

- **The touch mode** (`godot/Strategy/TouchMode`): on on Android, and for a launch with `--touch` (a look in a
  window, the mouse standing in for the finger). Without it PC is as before.
- **No hover with `--touch` on PC** (owner, 2026-10-24: to play the Android interface on PC as a finger plays it):
  `NoHoverFilter` takes the main window's input callback and drops the mouse's moves with no button down, so nothing
  lights up, opens or shows a tooltip under a pointer no finger touches; a press or a drag moves the pointer, which
  stays where it was left, as Android's mouse from touch does. A node's `_Input` was too late for it: the viewport
  updates the controls' hover (`mouse_entered`) first. Where our code reads the pointer's place itself (the battle's
  hovered card and advantage tips, the deployment table and party panels, Back as a right click) it asks
  `TouchMode.Pointer`/`GlobalPointer`/`LocalPointer`, the last touch's place under `--touch` on PC. On Android nothing
  changes. Unattended play with `--touch` checks it: its pointer moved onto the week's target plate must hover nothing
  before its tap (`AutoPlateClickAsync`). Still different from the tablet: the frame's pulse, full screen, a real finger.
- **The battle's rails** (`godot/Battles/BattleCommandLayer.Touch.cs`; since 2026-10-25 beside the cards, "The battle
  within the thumbs' reach" below): in the scene's blurred sides (layout x 0-128
  and 1152-1280, y 0-410). Left: the acting unit's name and `BattleCommands.ContextMenu(acting)` as buttons (Defend,
  Wait, the items, Auto-Atk, Auto-Def, Retreat in red), 112×56, smaller when more do not fit. Right: TARGET, the
  target's name and `BattleCommands.Banners(target)` as the banners' gold buttons, 112×64. A rail button chooses at
  its release over it. The banners on the card are not shown; the target's card has the cyan brackets and the scene
  shows that enemy. A tap on an enemy card makes it the target — also one no attack reaches (the agent's choice: its
  brackets faded, the rail saying "No attack reaches it"), so the player can see why. The long press keeps opening
  the own cards' menus.
- **The preselected target** (`Original.Content/Battles/TouchTargets`, `GameScreen.TouchTarget`/`TouchAttacked`):
  each unit's last attacked enemy by its record (the cards close up when one falls), else the last enemy any unit
  attacked, else the first enemy card; forgotten when a new battle opens (`GamePresenter.Battles`). **An agent's
  choice to confirm**: an enemy none of the acting unit's attacks reaches is passed over for the next in that chain.
- **The map's Commands button** (`godot/Strategy/CommandsButton`): round, 190×84 layout pixels, 16 in from the
  picture's bottom right corner, shown with the name plates in `COMMAND`'s map wait; a tap is the right click that
  opens the command panel, queued for the script as a plate's click is. A plate's tap opens the province menu as before.
- **Unattended play** with `--touch` in a window, once a launch: the bot's first right click for the command panel is
  a tap on the Commands button (`GameScreen.AutoCommandsTapAsync`, dump `touch-commands`); in the battle
  (`GameScreen.AutoTouchCommandsAsync`) it dumps `touch-rails`, taps another enemy card that an attack reaches
  (`touch-target`), then the Melee rail button. The mouse exercise of our battle screen is not run in the touch mode.
  Looked at by the agent (2026-10-24): rails, target change and the button as the mock-ups. A look:
  `--load --auto --fast --quit --seed 2 --saves user://saves-look-touch --slots-from user://saves-week18 --until round-2 --touch --shots <dir>`
  (it moves the system pointer).
- **The tables' header tips** (owner, 2026-10-24; `godot/Strategy/DeploymentTableView.Touch.cs`, `HeaderTip`): on the
  Subordinates and deployment tables a long press (0.5 s, as a battle card's) on a column's header shows its full name
  on a lacquer plate above the header, centred on the finger; the next press anywhere hides it. A tap sorts as a click
  does, a finger that wanders more than 12 pixels does neither; Godot's hover tooltips of the headers are off in the
  touch mode. Unattended play with `--touch` in a window, once a launch, on the Subordinates screen
  (`GameScreen.AutoHeaderTouchAsync`): a long press on T.Atk's header (dump `touch-header-tip`), a tap on it
  (`touch-header-sorted`), a tap on №'s. Looked at by the agent (2026-10-24) on the Subordinates screen, the
  look's command with `--until deployment --interface new` (the deployment table shares the code, not run).
- **The lists scrolled by a finger** (owner, 2026-10-26: the tables' scroll bars are too thin to hit on the tablet, the
  manual saves scroll by the wheel alone; the finger's part shared, `godot/Strategy/FingerScroll.cs`): a finger dragged
  more than 12 pixels scrolls, the list following it from where it passed them; risen at 150 pixels a second or more
  (measured over its last 0.1 s) the scroll goes on and slows (e^-4 of its speed kept after a second), stopping at the
  list's end; a press while it goes on stops it and taps nothing.
  - **The Subordinates and deployment tables** (`godot/Strategy/DeploymentTableView.Touch.cs`): a drag anywhere over
  the table (the header too) scrolls it both ways, the rows under the finger, each axis stopping at its bar's end. So a row's and a
  Refill button's press come at the release, when the finger did not wander (the Refill button lit while the finger is
  down on it); the header's tap and long press as before. After a scroll the hovered unit (the deployment screen's
  picture panel, the script's pointer) stays the row the finger came down on, not what scrolled under the place it rose
  from. The bars and the wheel work as before; the mouse on PC without `--touch` is unchanged. Unattended play with
  `--touch` in a window, once a launch, after the header's exercise (`GameScreen.AutoTableDragAsync`): a drag up held
  still before it rises (dump `touch-table-dragged`: the rows down, no flick, the chosen unit kept), a quick drag left
  (`touch-table-flicked`, once its flick stopped: the columns right, the flick on to the bar's end), a tap on another
  row choosing it; a bar the table does not show is left out — the kept saves list 9-10 units, which fit, so only the
  columns scroll there (the rows were checked once with the table made shorter for the run). Not yet on the tablet.
  - **The Save/Load screen's manual saves** (`godot/Strategy/SaveLoadView.Touch.cs`): a drag starting over the list's
  column (New save and the rows) scrolls the rows a row for each row's height (57 layout pixels) the finger moves — the
  list scrolls by whole rows, as the wheel does, so the rows step rather than glide; the flick goes on the same way.
  The auto-saves the same since they are twenty (decision 31, docs/ui/save-load.md, §12); not while a question is
  open. A row's press stays a tap at the release, as before; a
  drag lights no row. Unattended play with `--touch` and `--saveload`, in the load list after its dump `save-load`
  (`GameScreen.AutoSaveListDragAsync`): a drag up held still before it rises (dump `touch-saves-dragged`: the rows
  down, no flick, the chosen save kept), a row's quick drag up (`touch-saves-flicked`, once the flick stopped: the
  flick on to the list's end), a tap on another row choosing it; with every manual save in view it says so and drags
  nothing. The kept saves have no manual save, so it needs a folder with more of them than the list shows (ten in
  Load): `user://saves-manuals15`, made from the week-18 save — its `slot01.ksav` copied in as `auto-0001.ksav` and as
  `save-0001.ksav` to `save-0014.ksav` (`--slots-from` copies a library's saves too) —
  `--load --auto --fast --quit --seed 2 --saves user://saves-look-touch-saves --slots-from user://saves-manuals15 --saveload --until deployment --interface new --touch --shots <dir>`.
  Its synthetic drags move a frame and at least 1/60 s apart (`GameScreen.AutoDragAsync`): a window drawing hundreds of
  frames a second made a flick of three moves too short to measure. Looked at by the agent (2026-10-26); not yet on the
  tablet.
- **The building's tip on the status strip** (2026-10-25; docs/ui/command-screen.md, "A building under way"): the same
  long press on Construction's tag while a building is under way shows the tooltip a hover shows on the PC (the
  building, its weeks left); the next press anywhere hides it. Not tried on the tablet, nor by unattended play.

- **The target frame breathes on Android** (owner's choice A of three — A a slow breathing, B a lock-on once at each
  change, C a spark running round the brackets; 2026-10-24): the brackets 2-5 layout pixels out of the card and their
  glow from 0.6 to 1.3 of its usual strength and back, once every 1.2 s, from the near end at each new target
  (`BattleCommandLayer.DrawTouchTarget`). On PC, `--touch` included, the frame stands still as before; so the pulse is
  seen on the tablet only.
- **No Mouse movement** (owner, 2026-10-27): the options window's Messages tab leaves it out and the script's `IZ` never
  moves the system pointer, only the position the script reads (docs/ui/options.md, "Mouse movement").

## The Skip and Log buttons (owner, 2026-10-24)

The owner's request: the menu bar's MsgSkip is awkward to reach on the tablet; a large skip button at the bottom left
for the dialogues and scenes, as the map's Commands button is at the bottom right. Round 1 drew a hold (K1, the same as
Ctrl held, `MessageSkip.Held`), a tap or hold (K2) and the hold with a Log button (K3). **The owner's choice: no hold —
a tap turns the skip on and off, and a Log button beside it**; and the battle needs another place than round 1's
(the left blurred side), since the rails stand there in the player's turn.

What the buttons do (touch mode only; PC keeps F1, Ctrl and the wheel):

- **Skip** is the menu bar's MsgSkip (`MessageSkip.Toggle`, as F1): a tap turns it on — the pill lit gold, "Skip: ON" —
  and stops as xsystem35's switch stops: a tap on it again, a tap on the screen during a skipped wait, a menu, an unseen
  text when Option → Messages does not skip unseen ones. Greyed while the skip is not allowed (`MessageSkip.Enabled`).
  A tap on the button is the button's, never a click that answers the wait (nor one that stops the skip it started).
- **Log** opens the message log (`MessageLogView`, now the wheel up at a key wait); greyed where the log does not open.

The mock-ups are `tools/ui/mock_skip.py`, over window dumps of the week-18 save on the tablet's frame (1280×824, the
menu bar included; the player's turn from the touch look, `00074-touch-rails`); the images stay outside the repository:

- **S1 — the scenes**: steel pills as Commands, Skip 190×84 (39 × 17 mm) and Log 150×84 (30 × 17 mm), 16 in from the
  picture's bottom left, at every key wait of a scene.
- **S2** — the skip on: Skip lit gold. Where a window leaves no room beside it (the narration window starts at x 250),
  Log stands above Skip.
- **S3** — window 131 spans the bottom: the pair rides above it and its speaker's plate.
- **B1 — the battle, under the cards**: the strip under the cards (y 782-824 of the dump, 42 px) is free on every
  battle screen, the player's turn included; the pair at its left, Skip 150×34 and Log 110×34 (6.9 mm high) — the same
  corner as in the scenes. Shown through the whole battle, the turn included (the skip can be turned on before the
  exchange), Log greyed but at a key wait.
- **B2 — the battle, in the armies' heading**: the pair between "Rance Army" and the right side's label, over the turn
  counter's column (140×34 and 130×34).

**Recommendation: B1** — the same corner as the scenes, nothing else ever drawn there.

**The owner's choice (2026-10-24): S1-S3 in the scenes, B1 in the battle without Log** (the battle needs none). Built:

- **The places** (`Original.Content/Strategy/SkipButtons`, `SkipButtonsTests`): side by side at the bottom left; Log above
  Skip beside the narration window; above the window, side by side, where it covers the corner — 131 and the right
  speech (121/122, whose pair then stands over the left portrait's foot); in the battle Skip alone, 150×34, at
  (12, 762) of the screen.
- **The buttons** (`godot/Strategy/TouchPill`, the Commands button's look, lit gold while on, greyed and deaf while
  disabled; `godot/Strategy/GameScreen.Skip.cs`): shown in the touch mode at a key wait or while our message window is
  up — Skip also while MsgSkip is on, anywhere, to turn it off again —, never under a menu, a modal view or our own
  screens (the deployment table, Subordinates, Harem, Treasure, the command panel, the province menu, Save/Load, the
  enemy attack); on our battle screen Skip the whole battle. Skip is `MessageSkip.Toggle` (greyed while not
  `Enabled`); Log opens the message log at a key wait with something logged, else greyed.
- **The message log on touch** (`MessageLogView`): a finger's drag scrolls it, a tap off its frame closes it (Back, a
  right click, as before); its hint says so.
- **Unattended play** with `--touch` in a window, once a launch, at the first key wait of a scene with our window and
  something logged (`GameScreen.AutoSkipTouchAsync`): dump `touch-skip`, a tap on Log (`touch-log`), a tap off the
  log's frame closes it, a tap on Skip (`touch-skip-on`), another turns it off; the taps must not reach the script's
  input. The week-18 battle look reaches no logged page; the harem look does —
  `--load --auto --fast --quit --seed 2 --saves user://saves-look-131 --slots-from user://saves-week18 --harem 6 --until deployment --touch --shots <dir>`.
  The battle's Skip shows in the touch look's `touch-rails`. Looked at by the agent (2026-10-24); not yet on the tablet.

## The command panel and the province menu (owner, 2026-10-24)

The owner's request: on Android the command panel's and the province menu's rows are too low for a finger (30 and 29
layout pixels, 6.1 and 5.9 mm on the tablet), and the command panel's two lists — the commands and the other items
(→ Leazas Castle, Load, Save, All Data, Think) — could stand side by side, left and right; the same for the province
menu. Touch mode only: PC keeps today's panels.

The mock-ups are `tools/ui/mock_touch_menus.py`, over window dumps of the week-18 save on the tablet's frame (the
command in its docstring); the images stay outside the repository. Every row is 46 layout pixels (9.3 mm), 8 apart;
the other items get a row box too, since no hover shows that they can be tapped.

- **C1 — the command panel, two lists and Turn END across the foot**: the commands left, the other items right, a
  gold rule between them, Turn END (60 px, 12.2 mm) under both. 660×376 (now 320×430).
- **C2 — Turn END under the other items**: the same two lists, Turn END (64 px) at the foot of the right column, at the
  right thumb; the left column ends higher. The same size.
- **P1 — the province left, its commands right**: the picture larger (270×180, now 225×150), the name, the army and
  the figures under it; the commands down the right column, Return at its foot. 630×400 with five items (P1b: an own
  province's five — Leazas Castle's items and figures, Oaks's picture standing in).
- **P2 — the header as now, the commands in two columns under it**: down the left column, then the right; Return in
  the free cell (an odd count) or a row of its own. 576×314 with two items, 576×368 with five (P2b).

**Recommendation: C1 and P1** — Turn END apart from every other button (one tap ends the week, nothing asks first),
and in P1 the commands always one column at the right thumb, in the script's order, wherever the picture leaves room.

**The owner's choice (2026-10-24): C2 and P1.** Built, in the touch mode only (PC's panels as before; the command panel
since 2026-10-25 on two rails, "The command panel within the thumbs' reach"):

- **The command panel** (`godot/Strategy/CommandMenuView`): two columns of 300 with a gold rule between them
  (`Lacquer.DrawVerticalDivider`), rows 46 high and 54 apart — the commands left (tag 72 wide, text 19), the other items
  right in row boxes (text 18), Turn END 64 high, 14 under them; the panel as high as the longer column, centred.
- **The province menu** (`godot/Strategy/ProvinceMenuView`): the left column the picture (270×180), the name, the army
  and the figures under it; the right column the commands as 46-high rows, Return (150×46) at the panel's foot. The
  dungeon's caption (its place) stands over the right column's rows.
- Looked at by the agent (2026-10-24) in a window, the look's command with `--until deployment --interface new --touch`
  (dumps `command-menu`, `province-menu`); not yet on the tablet. The dungeon's menu and an own province's five items
  were not reached by the look.
- **Leaving them without Back** (owner, 2026-10-24: Android's Back is a gesture the player may not find): a tap outside
  the panel cancels it, as a right click does — the command panel back to the map, the province menu too, but not the
  dungeon's menu, whose cancel only opens it again; and while the command panel is open the map's Commands button reads
  **Close** (drawn over the panel, `CommandsButton.Closes`), its tap the same cancel (`CommandMenuView.Cancel`). A tap
  is a press and a release both outside the panel. Unattended play with `--touch` in a window, at the first command
  panel and the first province menu (`GameScreen.AutoTouchDismissAsync`): a tap on Close (dump `touch-close`), one
  outside the panel (`touch-outside`), one outside the province menu (`touch-province-outside`), each of which our view
  must answer with the cancel; the script then gets the bot's own choice, so the play goes on as before.

## The touch buttons' redesign (owner, 2026-10-24)

The owner's request: a new look (and, where it helps, a new place) for the touch mode's own buttons — the map's
Commands/Close button, the battle's rails and target frame, the scenes' Skip and Log. **The battle's Skip stays as it
is**; the rest changes. The rules and what each button does stay. **The dialogue buttons first**, their place and look.

### The dialogue buttons, round 2

What today's pair does wrong (the owner's dumps, and `touch-skip` of the harem look): the steel pills (190×84 and
150×84) are out of our message windows' lacquer, and their place jumps with the window — over the left portrait and
its name plate above the right speech, over the map beside the narration and the place's title, above 131.

The mock-ups are `tools/ui/mock_dialogue_buttons.py`, over window dumps of the harem look without `--touch` (the
command in its docstring); the images stay outside the repository. Every variant is drawn in our windows' lacquer
(`Lacquer`: the lacquer, the gold frame, the dim inner line), the skip on as gold:

- **A — on the window**: the pair rides on the window's edge at its right end, as 131's speaker plate does at its left
  (A1 the right speech: the top edge, Skip 140×46 and Log 112×46, 9.3 mm high); a window at the screen's top gets them
  hanging from its bottom edge (A2, the skip on); 131 on its top edge at the right (A3). Never over a portrait or the
  map, but the pair moves with every window, so while the skip runs Skip is a moving target.
- **B — the left edge**: a lacquer drawer glued to the screen's left edge, Skip over Log, icons over small labels, two
  cells 52×84 (10.6 × 17.1 mm) under the left thumb at y 590-762 of the screen — between the edge and every window
  (the right speech starts at x 58), under the left portrait (B1, B4). The skip on: the Skip cell lit gold, "ON", in the
  same place, so one tap stops it whatever flashes by; Log greyed where no key wait is (B2). Over 131 the drawer rises
  above the window on the same edge, clear of the speaker's plate (B3), its one other place.
- **C — one seal**: a round lacquer seal on the left edge (54×60, 11 × 12 mm), the quick menu: a tap opens its rows
  (Skip, Log, 46 high; room for more later) above it, a tap off them closes it (C1, C2); the skip on, the seal itself
  gold with the skip's arrows, one tap stops it (C3). The smallest footprint, but Skip and Log take two taps.

**Recommendation: B** — one place for every window (the thumb learns it), never over a portrait, a name plate or a
window's text, and the skip's stop always under the same finger.

**The owner (2026-10-24): B1 and B2, and over 131 the drawer at its usual place too**, 131 made a little smaller for
it. Our 131 covers the original's window, which the engine still draws under it (`GamePresenter.LeaveWindowOnStage`),
so whatever ours leaves free shows the original's frame and text. Two ways, drawn by the same script:

- **B3a — the drawer inside 131's frame**: the frame as now (so it still covers the original's whole), a gold rule at
  x 60, the drawer's cells left of it at their usual place (y 590-762, inside 131's 588-800); the text and the
  speaker's plate 95.7% as large right of the rule. The engine places the letters, so the line breaks stay; the
  narration's full lines still fit (B3a with the skip on, `B3a-edge-131-narration`).
- **B3b — the whole window 95.3% as large** right of the drawer (frame, plate, text); the strips it frees (60 px at
  the left, 10 at the foot) under an opaque lacquer dock of their own, the drawer on it.

**Recommendation: B3a** — nothing of the original's window to hide, one frame instead of a dock beside a frame.

**The owner's choice (2026-10-24): B1, B2 and B3b**, the dock's foot on the screen (the first B3b ran it off the
screen's bottom). Built, in the touch mode only (PC has no drawer, and 131 stays whole there):

- **The places** (`Original.Content/Strategy/SkipButtons`, `SkipButtonsTests`): the drawer (0, 590)-(52, 762) of the
  screen, its cells Skip (0, 592, 50×84) over Log (0, 676, 50×84). A window whose panel covers the drawer — 131 alone —
  is drawn at `WindowScale` (1220/1280) about its top right corner, so it starts at x 60, and the cells stand on its
  dock (0, 588, 56×202), as high as the window drawn smaller.
- **The drawer** (`godot/Strategy/SkipDrawer`): the plate glued to the left edge (`DrawPlate`: lacquer, gold on three
  sides, the right corners rounded), an icon over a small label in each cell, a gold divider between them; Skip lit
  gold, "ON", while MsgSkip is on; a greyed cell deaf; a cell chooses at its release over it, a tap elsewhere on the
  drawer does nothing. Shown and greyed as the pair before it (`GameScreen.Skip.cs`); the battle's Skip stays
  `TouchPill`.
- **131** (`MessageWindowView.Docks`): a dark band over the original's window, which the engine still draws under ours,
  the dock on it, the window — frame, speaker's plate, text, triangle — drawn smaller over the rest; a menu over it at
  its own place. The line breaks are the engine's, so they stay.
- Looked at by the agent (2026-10-24) in a window: the harem look with `--touch` (`touch-skip`, `touch-log`,
  `touch-skip-on`, `message-cg-bar-speech`, `message-cg-bar-narration`) and the battle look (`touch-rails`: the battle's
  Skip as before); every unattended `--touch` check passes. Not yet on the tablet.

**Round G (owner, 2026-10-25): more room between Skip and Log**, which sat too close together in the gallery's Touch
tab. Mock-ups `tools/ui/mock_drawer_gap.py` over the gallery's dumps, Skip kept where it was and the drawer grown
downwards in each: G1 one drawer with 16 px between the cells, G2 one drawer with 28 px and cells 80 high, G3 two plates
16 px apart. **The owner chose G3**, built: Skip's plate (0, 590, 52×88) round its cell as before, Log's (0, 694, 52×88)
16 px under it (3.2 mm on the tablet), the cell Log (0, 696, 50×84); `SkipButtons.Drawer` is both plates and the room
between them, (0, 590)-(52, 782), what the windows keep free. Over 131 the plates stand the same on the dark band, the
window drawn smaller right of them as before; the dock is gone (`SkipButtons.Dock`, `MessageWindowView`'s plate). A tap
between the plates is the drawer's own and does nothing, so a near miss never advances the text. Looked at in the
gallery's Touch tab (every message window, `touch-skip-on`); the unattended `--touch` harem look not run again.

### The map's and the battle's buttons, round 1

The owner's request (2026-10-24), after the dialogue's drawer: the map's Commands/Close button and the battle's rails
and target frame next. Today: the Commands button a steel pill 190×84, the rails steel columns with steel and gold
buttons, the target cyan brackets (breathing on Android).

The mock-ups are `tools/ui/mock_touch_buttons.py`, over window dumps of the week-18 save (the commands in its
docstring; Rance's turn pieced together from three dumps, so nothing lies over the cards); the images stay outside
the repository. Three looks, each drawn on the map (1 Commands, 2 Close over the command panel) and in Rance's turn
(3: the rails, the target Sakanaku (commander 6)); the battle's Skip as it is in all of them:

- **A — lacquer and gold**, our menus' and the dialogue drawer's style: the map's button a lacquer plate 220×72
  (44.7 × 14.6 mm) with the double gold frame, its studs, an icon and the brush font of Turn END; the rails lacquer
  columns in the same frame, the acting unit's name in the brush font, the rows as the command panel's (104×56,
  21 × 11 mm), Retreat vermilion as Turn END, the attacks lit gold with the brush font (108×66); the target framed by
  gold brackets with a gold diamond on top.
- **B — the battle screen's steel, crisper**: the map's button a rounded steel plate 220×72 with a sheen and an icon;
  the rails steel flush with the scene, the buttons as the cards' colours with an icon each (112×62), Retreat red; the
  attacks as the banners' gold chevrons pointing into the scene (116×66); the cyan brackets with a TARGET tab.
- **C — round seals, icons first**: the map's button a lacquer seal 92 across (18.7 mm), the icon over a small label;
  the commands seals 54 across (11 mm) two to a row with labels under them, Retreat a larger red seal; the attacks large
  gold seals 96 across; a round reticle over the target's portrait, the card outlined in gold.

**Recommendation: A** — one look for every touch control (the command panel, the province menu, the dialogue's drawer
are lacquer already), and the target in gold reads against the steel cards as well as the cyan did. B keeps the
battle in its own steel; C is the most "mobile", but its labels are small and the rails hold fewer seals when a unit
has items.

**The owner's choice (2026-10-24): A, on the map and in the battle.** Built, in the touch mode only (PC's banners and
cyan frame as before); the rules, the places and what each button does as before:

- **The map's button** (`godot/Strategy/CommandsButton`): 220×72, 16 in from the picture's bottom right, a lacquer
  panel (`Lacquer.DrawPanel`), three gold bars (Commands) or a gold cross (Close), the label in Shojumaru 19; pressed,
  its lacquer lighter. No palette any more (Option → Display's palette was its steel).
- **The rails** (`godot/Battles/BattleCommandLayer.Touch.cs`): each side a lacquer panel 4 in from the blurred side.
  Left: the acting unit's name in Shojumaru 19, a dim rule, the commands as the command panel's row boxes 104×56 from
  y 64 (`Lacquer.DrawRowBox`, held: lit), Retreat as Turn END (`Lacquer.DrawRedButton`, its name in the brush face).
  Right: TARGET in Cinzel, the target's name in Shojumaru 17, a rule, the attacks lit gold 108×66 from y 80 with their
  name in the brush face (held: brighter, a glow). Names shrink to fit their row; more rows than fit get lower as before.
- **The target frame** (`DrawTouchTarget`): the brackets in gold with a warm glow and a gold diamond on the card's top
  edge, faded where no attack reaches; it breathes on Android as before.
- Looked at by the agent (2026-10-24) in a window with the battle look (`touch-commands`, `touch-close`, `touch-rails`,
  `touch-target`); every unattended `--touch` check passes. Not yet on the tablet.

## The battle within the thumbs' reach (owner, 2026-10-25)

The owner's request: the tablet is held in landscape with both hands, so every control of the battle screen should be in
the screen's lower half, where the thumbs are. What is up there today (the screen gallery's touch pass, `battle-turn`,
`retreat`): the two rails (layout y 0-410, in the scene's blurred sides) and the retreat question (rows of 30 in the
scene's middle). Already low: the cards (a tap picks the target, a long press opens an own unit's orders, its menu at the
card), the advantages' tips at the board's top right, the battle's Skip (`SkipButtons.Battle`, the bottom left).

The lower half alone is not quite the measure: on the 12.4" tablet a thumb reaches about **70 mm (345 layout pixels)**
round the point its hand holds the side at (about y 640), so the reachable places are the two lower corners and the
sides; the middle of the screen's foot (x ~400-880) needs a hand let go. The mock-ups draw that reach as pink arcs.

The mock-ups are `tools/ui/mock_battle_low.py`, over the screen gallery's dumps (`gallery.ps1 -Touch`; the command in
its docstring); the images stay outside the repository. Round 1:

- **A — the rails moved down beside the cards.** The scene as now, full width, nothing over it. The rails stand at the
  screen's sides in the lower half (x 4-136 and 1144-1276, y 418-796), in round 1's lacquer: left the acting unit's
  name and its commands 116×54 (24 × 11 mm), right TARGET, its name and the attacks 116×76 stacked from the rail's
  foot up, so Melee is under the thumb. The board between them is 1000 wide instead of 1280 — drawn scaled in the
  mock-up; built, the cards would keep their text and give up portrait width. **A2**: the retreat question beside the
  rail's Retreat, its answers 144×80 (29 × 16 mm), Cease Retreat nearer the rail so that a double tap does not confirm.
- **B — a bar along the foot.** The scene 80% as large, the cards as now raised under it, a lacquer bar at y 692-796:
  the commands at the left thumb in 2 rows of 3 (118×42), the attacks at the right thumb (Melee 204×84, more lining up
  leftward), the bar's middle only telling ("Rance → Ex"; no thumb reaches it). The third column of commands and a
  second special already lie at the reach's edge.
- **C — the halves swapped.** The cards on top, the scene with today's rails at the thumbs below; nothing new to draw.
  But a tap on an enemy card (another target) or a long press on an own card now reaches up — rarely, since the target
  is preselected.

Everywhere: the battle's Skip (bottom left) would stand on the left rail's (A, C) or the bar's (B) place; it shows during
the exchanges, when the rails are not up, so it can take the rail's foot then.

**Recommendation: A** — every button inside the arcs, the scene whole, the cards where they are (a target's tap stays
low); its cost is the board's width (the cards' portraits narrower). B keeps the cards whole but shrinks the scene and
pushes buttons to the reach's edge; C is the cheapest but sends the card taps up.

**The owner's choice (2026-10-25): A and A2, the left rail filled from the foot up as well** (Defend lowest, Retreat at
its top). Built, in the touch mode only (PC's board, its cards and its dialog as before; the gallery's Lacquer pass
unchanged):

- **The rails** (`godot/Battles/BattleCommandLayer.Touch.cs`): x 0-140 and 1140-1280, y 414-800, the panels 4 in. Left:
  the acting unit's name, a rule, its commands 116×54 from the rail's foot up (Defend, Wait, the items, Auto-Atk,
  Auto-Def, Retreat); right: TARGET, the target's name, a rule, its attacks 116×76 from the foot up. The scene's blurred
  sides are bare again.
- **The board between them** (`BattleBoardView.Narrow`): the cards 223×150 in the columns 146, 377, 680, 911, the round's
  gauge 56 wide between, the armies' names over their columns; the card's text keeps its size and the portrait gives way
  (`CardPortrait`: 70 wide, the face 66, its four numbers at 14 across the whole width).
- **The retreat question** (`BattleDialogLayer`, A2): a lacquer box at (146, 470) 330 wide, beside the left rail's top
  where Retreat stands; the question, a rule, the items side by side as buttons 80 high, a warning item (Retreat) as
  Turn END and last, so Cease Retreat is nearer the rail.
- **The battle's Skip** (`SkipButtons.Battle`) right of the left rail, (146, 762).
- **A defence's ribbon** on the narrower card (owner, 2026-10-25: its text ran together): "DEFEND" and the mark at their
  size when they fit, else the mark's first word dropped ("HIT −10%"; the caption over the field says the rest), then
  both smaller, down to four fifths (`BattleInfoLayer.RibbonText`); PC's ribbon as before.
- **The target's name** under TARGET (owner, 2026-10-27: the widest enemy name, "2nd Assault Leader" (commander 192),
  ran past the rail even at 11): one line at 17, down to 14 when it must; a name wider still is broken at the space
  that balances its two lines best, both at up to 15 ("2nd Assault / Leader" at 14), between TARGET and the rule
  (`BattleCommandLayer.TargetLabel`). Of the 202 names the scenario gives commanders, 39 take two lines; a name of one
  word only shrinks, as before.
- Looked at by the agent (2026-10-25) in the screen gallery's touch pass (`battle-turn`, `retreat`, `menu`,
  `exchange-counters`, the info boxes), and in the unattended `--touch` battle look ("The tablet (now)"): its taps on
  the rails, another target and Melee pass to round 2, a party of four fits (`touch-rails`, `touch-target`). The retreat
  question is not answered by that run (the gallery's `retreat` only). Not yet on the tablet.

## The command panel within the thumbs' reach (owner, 2026-10-25)

The owner's request, after the battle's: the map's command panel easier to choose from at the screen's lower left and
right, as the battle's rails. Today (C2, "The command panel and the province menu") it stands in the screen's middle,
which no thumb reaches; only the Commands/Close button is at the right thumb. The reach is the battle's: about 345
layout pixels (70 mm) round the point each hand holds its side at (y ~640).

The mock-ups are `tools/ui/mock_touch_panel.py`, over the screen gallery's touch dumps (`map`, the backdrop alone, added
for them, and `command-menu-building`'s status strip: the Military Academy under way, 5 weeks left; the command in its
docstring); the images stay outside the repository. Round 1:

- **A — two rails in the sides' lower halves, as the battle's.** Left (x 4-238, y 466-796): THIS WEEK, the week's
  commands as rows 214×46 (43 × 9 mm) from the foot up, Construction lowest. Right (x 1042-1276, y 352-796): Turn END at
  the top (214×62), furthest from the resting thumb as the battle's Retreat; the other items from the foot up
  (→ Leazas Castle lowest); Close at the foot, where the Commands button stands. The map's middle stays open; a tap on it
  closes as now. A building's weeks go on the tag's chip (the rail has no room for "5 weeks left"). The thieves' week:
  two and three rows.
- **B — a bar along the foot** (y 680-796): the week's commands and → Leazas Castle at the left thumb, the other items
  and Close at the right, cells 164×46 in two rows of three; Turn END in the middle, where no thumb reaches — a hand let
  go to end the week, on purpose. The third columns lie at the reach's edge or past it, and the bar covers the map's foot.
- **C — a fan at each lower corner**: the rows' inner ends along the thumb's arc (rows 44 high, 39-67 mm long), on a
  see-through lacquer quarter; Turn END the right fan's top, Close its hub in the corner. The rows shorten as they rise,
  so the longest names have the least room.

**Recommendation: A** — the battle's language (rails, from the foot up, the risky button furthest), every row inside
the arcs, the map's middle open; B puts its outer columns past the reach, C is the most playful but its rows' lengths
depend on their height. The province menu (P1, its commands in the middle too) could follow the same way, the province's
card left and its commands on the right rail: a question for the owner.

**The owner's choice (2026-10-25): A.** Built, in the touch mode only (PC's centred panel as before; the province menu
stays P1 for now):

- **The rails** (`godot/Strategy/CommandMenuView`): 244 wide at x 4 and 1032, down to the picture's foot, rows 220×46
  54 apart. Left: THIS WEEK (Cinzel 13), a divider, the commands from the foot up (the lowest's top at 732), the rail as
  high as they need. Right: Turn END 220×62 at the top, a divider, the other items from the foot up (the lowest's top at
  656), and at the foot the map's button reading Close (`CommandsButton`, 220×72 at 1044, 712, unmoved). The thieves'
  week has shorter rails (two commands, three items).
- **The agent's choices:** a spent command's row in the rail is only dimmed — the rail has no room for "used this week",
  and the strip's dim tag says it too; a building's weeks go on the tag's chip (the mock-up). A tap off both rails
  cancels (the map's middle included); `OutsidePoint`, where unattended play taps it, is the map's middle.
- Looked at by the agent (2026-10-25) in the screen gallery's touch pass (`command-menu`, `-spent`, `-building`,
  `-building-tip`) and in the unattended `--touch` look from the week-18 save (`--until deployment`): its taps on Close
  and off the rails cancel the panel. Not yet on the tablet.

## The province menu within the thumbs' reach (owner, 2026-10-25)

After the command panel's rails, the owner asked for the province menu's mock-ups too, doubting it: the tap on a province
already takes the hand out of the thumb's comfort. Today (P1, "The command panel and the province menu") it stands in the
screen's middle, the province's card left and its commands right.

The mock-ups are `tools/ui/mock_touch_province.py` (its parts `mock_touch_panel.py`'s), over the screen gallery's touch
dumps (`map`, `province-menu` and `province-menu-own-five` for the pictures; the command in its docstring); the images
stay outside the repository. Each shows the tap and the tapped province ringed in gold (ours: today nothing marks it).
Round 1:

- **A — the command panel's rails**: the province's card on the left rail (the picture 220×144, the name, the army, the
  figures), its commands on the right from the foot up, Return at the foot where the command panel's Close stands.
  Leazas Castle (1) with two buildings, five commands.
- **B — the card at the province, the commands on the right rail**: the card beside the tapped province on the side with
  room, a triangle pointing at it, where the eyes are; the commands and Return as A's.
- **C — one callout at the province**: the card (compact: the picture 190×124, the figures in one line each) and the
  commands together, beside the finger, on the side with room, kept on the screen. Leazas Castle (1): the first command
  81 mm from the tap, A's and B's on the rail 103 mm; Oaks (6), the rebels' (two commands, "??"): 55 mm.

**Recommendation: C** — the owner's doubt holds: after the tap the finger is at the province, not at a thumb, so the
next tap is shortest beside it (the house style's "ask at the click", docs/ui/style.md §5), and a province near a side
puts the callout in that thumb's reach anyway. The command panel keeps its rails: it belongs to no place. A is the most
consistent with the panel but sends the finger across the screen and back; B splits the eyes (the card) from the finger
(the rail).

**The owner's choice (2026-10-25): B.** Built, in the touch mode only (PC's centred menu as before):

- **The card** (`godot/Strategy/ProvinceMenuView`): 300 wide, the picture 272×180, the name, the army, the figures as
  P1's; 60 left of the tapped province when there is room, else right of it short of the rail, centred on it and kept on
  the screen; a gold triangle from its edge to the province, the province ringed in gold. Its point is the province's
  map point on the view the screen shows (`MapMarkers.PointOnView`, `ProvinceMenuView.PointOf`, `GamePresenter.MapViewShown`).
- **The rail**: the command panel's right one — 244 wide at x 1032 down to the foot, the commands 220×46 from the foot up
  (the lowest's top at 664), Return 220×60 at the foot where the map's button stands. The rows' text one size for all,
  19 or smaller as the longest name needs ("Military Academy": 16); the tag 62 wide.
- **The dungeon's menu** (the castle's cells, its place as a caption at the rail's top) stands over the city's picture,
  so its card points at nothing and sits in the map's middle left of the rail; its cancel only opens it again, as before.
- **The agent's choices:** a × row is only dimmed (no room for "used this week"), as the command panel's rail; a tap off
  the card and the rail cancels.
- Looked at by the agent (2026-10-25) in the screen gallery's touch pass (`province-menu`, `-own`, `-own-five`, `-cells`,
  `-cells-full`) and in the unattended `--touch` look from the week-18 save: Oaks (6)'s card left of it, its tap off the
  menu cancels. Not yet on the tablet.

## The standing orders on the cards (owner, 2026-10-25)

The owner's request: during the player's turn every card of the player's army offers Auto-Atk or Auto-Def in the room at
its foot. In the touch layout (cards 223×150, the face 66 with its stats under it, `CardPortrait`) the text column ends
with the state line, ~40 px above the card's foot; only the exchange's action pill and a defence's ribbon ever show there,
and neither during `FIGT_SEL`. Today the orders are a long press away (`BattleCommands.ContextMenu(own)`: Manual,
Auto-Atk, Auto-Def, `FIGT_SEL` 50-52); the strip sends the same choices, so no rule changes.

Mock-ups (`tools/ui/mock_card_orders.py`, over the gallery's touch `battle-turn`), each with Rick (unit 7)'s card in its
three states enlarged:

- **A — three segments** `Manual | Atk | Def` under the state line, 137×32 across the text column (Manual 55, Atk and Def
  41 wide: 11 / 8 × 6 mm), the order lit in gold; one tap gives the order.
- **B — two toggles** `Auto-Atk | Auto-Def`, 66×32 each (13 × 6 mm), the order's lit, Manual when neither is; a tap on the
  lit one gives Manual back.

Both drop the order's word from the state line (the strip shows it; `FlashOrder`'s flash would go to the strip) and show no
strip on the acting card: the left rail's Auto-Atk and Auto-Def hand its turn to the automaton, a different thing. The
long press stays.

**The owner's choice (2026-10-25): B**, and the touch mode's portrait at the card's top, level with the name (centred it
floated). Built, in the touch mode only:

- **The toggles** (`BattleBoardView.OrderToggles`, `OrderToggleArea`, drawn by `OrderStrip` on the card's panel, so they
  slide and fade with it): 66×32 side by side across the text column, 8 above the card's foot; a row's box, the order's
  lit in the accent as the action's pill, the text 14 or smaller. On every own card but the acting one, always — deaf
  outside `FIGT_SEL`, so the order stays readable on the enemy's turns (an agent's choice: the mock-up's strips only while
  choosing would have left the order nowhere then). Hidden on a fighter's card while the exchange's spotlight shows: the
  action's pill stands there.
- **The input** (`BattleCommandLayer.Touch.cs`, `OrderToggleChoices`): each toggle's choice from the card's context menu
  (`ContextMenu(own)`): its order, or Manual when that is the unit's; chosen at the release over it, as a rail button, a
  glow round it while held. A long press on it opens the card's menu instead.
- **The state line** of the touch mode's own cards has no order word; an order given flashes round its lit toggle (both for
  Manual) instead of the word (`FlashOrder`).
- **The portrait** (`CardPortrait`): `ShrinkBegin` in the touch mode, `ShrinkCenter` on the PC as before.
- Looked at by the agent (2026-10-25) in the gallery's touch pass: `battle-turn` (Rick (unit 7)'s toggles, Manual),
  `order-toggles` (made up: Auto-Def lit, Auto-Atk held; the tap's choice checked), `exchange-counters` (Rance's card
  with the pill, no toggles). Not yet on the tablet, nor in unattended play.

## The dungeon's floor ladder within the thumb's reach (owner, 2026-10-26)

The ladder (docs/ui/dungeon-menu.md) stood right of the dungeon's picture at the screen's middle height. Mock-up round
T1 and the owner's choice are docs/ui/dungeon-menu.md, §5: B, built — in the touch mode a rail in the lower right
corner, right of window 113, Retreat at its top, then Ascend, the floor and a larger Descend in the corner. The menu
before a search went the same way (§6 there, the owner's B): Start Search in Descend's place, Return on top.

## The enemy attack's answers (owner, 2026-10-27)

In the touch mode the enemy attack's row of answers is mirrored: Abandon City first, Ninja Scout, Intercept last at the
foot's right, each at its size (docs/ui/enemy-attack.md, §12).

## The message log from the map, round L (owner, 2026-10-26)

On the PC the wheel up now opens the message log on `COMMAND`'s waiting map too (docs/ui/messages.md, §5 step 6). A
finger has no wheel, and the Log button is the scenes' drawer's, which the map shows only while MsgSkip is on
(`GameScreen.UpdateSkipButtons`), so on the tablet the log is still reached only from a dialogue. What would open it — the
same conditions as the wheel's (`GameScreen.MapLogWait`) — is settled; where its button stands is the owner's choice.

Mock-ups (`tools/ui/mock_map_log.py`, over the gallery's touch `map-markers` and `command-menu`):

- **L1 — the scenes' drawer on the map**: Skip over Log at the left edge, in their scene places (cells 50×84, 10.2 ×
  17.1 mm), so the thumb finds them where it does in the scenes; Skip turns MsgSkip on before Turn END, as the menu bar's
  MsgSkip does on the PC. The drawer covers the map's left edge there (Rockearth's plate); a drag moves the map from under it.
- **L2 — Log alone at the left edge**, in its scene place; Skip's place left to the map; the drawer whole again in the scenes.
- **L3 — a Log button beside Commands**, the same lacquer and gold, 132×72 (26.8 × 14.6 mm), 16 px left of it: both under
  the right thumb, nothing at the left edge. Its icon a speech balloon — the drawer's three lines would repeat Commands'
  bars beside them.
- **L4 — no button on the map**: a Log row in the command panel's right rail after All Data (the rail one row taller
  upwards, as each link makes it). Two taps (Commands, Log); the log over the panel, the panel again when it closes.

**The owner's choice (2026-10-26): L1**, built:

- **The drawer** (`SkipDrawer`, unchanged) shows on `COMMAND`'s waiting map while nothing is over it
  (`GameScreen.MapLogWait`: no menu or window, no plate's click or Subordinates order under way; `UpdateSkipButtons`),
  hidden under the command panel and the province menu as before. Log is lit when something is logged
  (`LogReachable`); a tap opens the log only while no button is held (`LogOpens`, the wheel's rule), so Log does not grey
  while a finger drags the map. Skip turns MsgSkip on and off there as in the scenes.
- **Unattended play** in the touch mode (`--touch`, a window) checks it once a launch at the first map wait with something
  logged (`GameScreen.AutoMapLogAsync`): the drawer shown and Log lit (dump `touch-map-drawer`), a tap on Log opens the
  log (`message-log-map`), a tap off its frame closes it, nothing reaches the script. The week-26 save reaches it after
  Lia's scene; `--turn-end --weeks 26 --until deployment` keeps the pointer still.
- **The gallery**: the Touch tab's map entries draw the drawer (docs/ui/gallery.md, §7).
- Looked at by the agent (2026-10-26) in a window and in the gallery. Not yet on the tablet.

## Phones (put off)

Put off by the owner (2026-10-24). What is ready for them: the measurements above, mock-ups B1, B3 (the battle),
M1, M2 (the map). Open questions: B1 or B3; M1 or M2; the text size on a phone (a phone layout with larger text, or a zoom). Turn END stays in the command panel on a phone
too, asking nothing — no button on the map ends the week (owner, 2026-10-25; M1's button on the rail goes).

Taken up again (owner, 2026-10-27): the phone in both orientations, the Xiaomi Poco X6 Pro the reference, upright first —
docs/ui/phone.md (round P1); the landscape phone after it. The figures above are the first survey's (a 6.5" phone);
the reference since is the Poco (154.5 × 69.5 mm held landscape, black sides of 21.6 mm; docs/ui/phone.md, §12).

The landscape phone is built (2026-10-28; docs/ui/phone-build.md, steps F and L1-L10): every screen of ours as its
landscape base version (docs/ui/phone.md), the touch mode's input with a long press as the right click on the original's
screens, awaiting the owner's look on the Poco; upright next (steps P0-P9).

The phone held upright is built (2026-10-30; docs/ui/phone-build.md, steps P0-P9): every screen of ours as its upright
base version, the bar at the foot with Menu, Log and Skip at its left and the act at the right thumb, the orientation
chosen in Option → Display and a turn of the phone laying the open screen out again (a finger's press or flick across a
turn ends, `FingerScroll`); the whole phone awaits the owner's look and play on the Poco.
