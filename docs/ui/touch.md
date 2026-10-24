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
- **The battle's rails** (`godot/Battles/BattleCommandLayer.Touch.cs`): in the scene's blurred sides (layout x 0-128
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

- **The target frame breathes on Android** (owner's choice A of three — A a slow breathing, B a lock-on once at each
  change, C a spark running round the brackets; 2026-10-24): the brackets 2-5 layout pixels out of the card and their
  glow from 0.6 to 1.3 of its usual strength and back, once every 1.2 s, from the near end at each new target
  (`BattleCommandLayer.DrawTouchTarget`). On PC, `--touch` included, the frame stands still as before; so the pulse is
  seen on the tablet only.

## Phones (put off)

Put off by the owner (2026-10-24). What is ready for them: the measurements above, mock-ups B1, B3 (the battle),
M1, M2 (the map). Open questions: B1 or B3; M1 or M2; Turn END on the map's rail (asking first) or only in the
command panel; the text size on a phone (a phone layout with larger text, or a zoom).
