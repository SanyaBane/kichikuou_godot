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
  pick on a second; the message log by a button at a key wait. One by one, after the battle and the map.

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

- **The target frame breathes on Android** (owner's choice A of three — A a slow breathing, B a lock-on once at each
  change, C a spark running round the brackets; 2026-10-24): the brackets 2-5 layout pixels out of the card and their
  glow from 0.6 to 1.3 of its usual strength and back, once every 1.2 s, from the near end at each new target
  (`BattleCommandLayer.DrawTouchTarget`). On PC, `--touch` included, the frame stands still as before; so the pulse is
  seen on the tablet only.

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

**The owner's choice (2026-10-24): C2 and P1.** Built, in the touch mode only (PC's panels as before):

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
- Looked at by the agent (2026-10-25) in the screen gallery's touch pass (`battle-turn`, `retreat`, `menu`,
  `exchange-counters`, the info boxes). Not yet on the tablet.

## Phones (put off)

Put off by the owner (2026-10-24). What is ready for them: the measurements above, mock-ups B1, B3 (the battle),
M1, M2 (the map). Open questions: B1 or B3; M1 or M2; the text size on a phone (a phone layout with larger text, or a zoom). Turn END stays in the command panel on a phone
too, asking nothing — no button on the map ends the week (owner, 2026-10-25; M1's button on the rail goes).
