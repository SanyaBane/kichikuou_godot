# The phone — survey and mock-ups (2026-10-27)

The owner's request (2026-10-27): the Android build on a **phone, played in both orientations**, landscape and upright;
the reference phone is the **Xiaomi Poco X6 Pro**. **Upright first** — this document's round P1. The landscape phone was
surveyed with the tablet (docs/ui/touch.md, "The devices", its mock-ups B1, B3, M1, M2 and "Phones (put off)"); it comes
after the upright.

The rules stay the original's, as in the touch mode: every choice an upright screen offers is one the script's menus
offer, answered through the inputs the script reads (docs/ui/style.md, §1). Only the layout changes; the touch mode's
habits (docs/ui/style.md, §6: select then act, a long press for details, the thumbs' reach, Turn END only in the command
panel) hold upright too.

**Base versions first** (owner, 2026-10-27): the phone's mock-ups are drawn screen by screen as base versions; nothing
is built while they are being drawn.

The mock-ups are `tools/ui/mock_phone.py` (the command and its inputs in its docstring), drawn at the phone's own pixels
over the screen gallery's dumps, the week-18 look's key waits (Ex's scene before the attack on Oaks (6)) and the map's
own pictures; the images stay outside the repository. The chosen base versions are also on the screen gallery's tab
**Smartphone, upright (mock-ups)**, marked as not built (`tools/ui/gallery.ps1 -PhoneOnly`; docs/ui/gallery.md, §9). Their variants are named **P…** (portrait), apart from
touch.md's landscape B1, M1 and the rest.

## 1. The device

| | |
|---|---|
| Screen | 6.67", 1220 × 2712, ~446 ppi, 20:9 (the maker's and GSMArena's specifications) |
| Size | 69.5 × 154.5 mm upright; 17.6 phone pixels a millimetre |
| Camera | a hole at the top's middle: nothing to read or tap in the top 110 px (6.3 mm), the status bar's room |
| Gestures | the gesture bar at the foot: no button in the bottom ~50 px (3 mm), where a swipe up is the system's |

The game's 1280 × 800 layout (`GameScreen` keeps its aspect) in the two orientations:

| | Landscape (2712 × 1220) | Upright (1220 × 2712) |
|---|---|---|
| The layout fitted | to the height: 1952 × 1220 (111 × 69.5 mm), black sides of 21.6 mm | to the width: 1220 × 762 (69.5 × 43.4 mm), 111 mm free over and under it |
| A layout pixel | 1.525 phone px = 0.087 mm | 0.953 phone px = 0.054 mm |
| The scenes' own text (48 px em) | 4.2 mm | 2.6 mm |
| Our cards' text (17 px) | 1.5 mm | 0.9 mm |
| A battle rail button (116 × 54) | 10.1 × 4.7 mm | 6.3 × 2.9 mm |
| The map | 3.05 phone px a map pixel | 1.91 |

A finger needs 7-9 mm (123-158 phone px); Android's smallest common text, 12 sp, is ~1.9 mm. So upright the engine's
own letters — the scripts' text at its cells — stay readable at the picture's width, but **every screen of ours needs a
layout of its own**: text 2-3.5 mm (35-60 phone px), targets of 7 mm and more — two to three times today's layout
pixels. **P0** shows today's touch layout turned upright: 49 mm unused over the picture, 52 mm under it, the cards'
text 0.9 mm.

The thumb: a phone upright is mostly held in one hand. The mock-ups draw the right thumb's comfortable reach as a pink
arc, ~70 mm round a grip low on the right side: the lower two thirds of the screen, the top left out of it.

## 2. The scenes (dialogue, narration, event CGs)

The original's windows lie over its picture (speech 121/122, thought, narration, the place's title, 131 under an event
CG). Upright the picture stands under the camera at the screen's width; the rest is the question.

- **PS1 — the picture as on the PC, a bar under it.** The picture and our windows as today, centred in the screen, the
  text 2.6 mm. A tap anywhere goes on. A bar at the foot holds what the PC has elsewhere: **Menu** (the menu bar's
  commands — File, Screen, Option, MsgSkip — as finger rows), **Log** and **Skip** (the tablet's drawer). No layout
  work, but half the screen is empty and a window still covers the picture.
- **PS2 — the window under the picture.** The picture whole (the portraits uncovered); our window under it at the
  screen's width, its speaker on a plate (`PortraitNames`, as 131's), the speech's tail pointing up at its portrait;
  the bar at the foot. The script's line breaks are kept, so each window kind's type is as large as its longest line
  allows: the speech and thought windows' lines (32 cells) at **3.6 mm**, 131's and the narration's longer lines (49
  cells) at 2.6 mm. The window keeps four lines' height, so nothing under it moves. **PS2b**: window 131 under its CG —
  the CG whole (on the PC the window covers its lower quarter). **PS2m**: a `]` menu as finger rows (7.7 mm) at the
  thumb, in the bar's place, in the script's order; the question stays readable above (made up: the gallery's scene
  menu).
- **PS3 — the chronicle under the picture.** The scene's pages run on under the picture as a chronicle: the page
  waiting for the tap at the foot, lit and framed, the earlier ones above it quieter, each with its speaker (a thought
  marked by a dashed rule, the narration in its own ink); a drag down reads further back — the message log merged into
  the scene. One size for every window kind, 2.6 mm, so 131's lines fit with their breaks.

**Recommendation: PS2** — the reading stays the original's (one window at a time, as on the PC and the tablet), the
type is the largest, the picture is whole; the room under the window stays free for the menus (PS2m) and the touch
controls. PS3 uses that room for what was just read, at the cost of the smaller type and a look further from the
original's windows.

**The owner (2026-10-27): none of the three.** The scenes stay open; the other screens go on without them.

## 3. The map (`COMMAND`'s map wait, the command panel, a province)

The map is six pictures, 1920 × 800 map pixels; the PC shows 640 × 400 of it. Upright at the picture's width the
whole **height** of the map fits the screen: it would scroll sideways only.

- **PM1 — the map fills the screen; sheets rise over it.** The status on top (the date, the budget, the troops, the
  week's uses as the strip's tags), the map under it at its whole height (2.64 phone px a map pixel: a flag 3.6 mm
  wide), our plates with 2 mm text, the bar at the foot with **Commands** at the right thumb. **PM1b**: Commands opens
  the command panel as a sheet over the map's foot — the tablet's two rails side by side, filled from the foot up,
  Turn END at the top right, Close where Commands was (rows 7.1 mm). **PM1c**: a tap on Oaks (6) rings it; its card
  and commands rise as a sheet, Return at the right thumb; since the sheet covers the map's south, the map then
  scrolls up by the sheet's height to keep the province in view.
- **PM2 — the map and the deck.** Only the date and the budget on top; the map's whole height at the picture's width
  (1.91: the PC's view with all its rows; a flag 2.6 mm wide, plates with 1.7 mm text); under it a fixed **deck**:
  in the map wait the status, large — the troops and the week's uses in their three groups —, the bar with Commands;
  **PM2b** the command panel in the deck (Turn END at the top right, Close at the right's foot; rows 6.7 mm, all the
  deck's height holds under the whole map); **PM2c** the province's card and commands in the deck. Nothing ever covers
  the map.

The map's flags outside the gallery dump's view (map x 1280-1920, y 208-608) are the game's first owners, made up for
the mock-ups; the dungeons are the four the dump shows.

**Recommendation: PM2** — the map is never covered, so a province's card and the map show together; the deck is under
the thumb and the status gets room to be read; the map is the PC's view, only taller. PM1's map is larger (its flags
and plates easier to hit), but every sheet hides the south and needs the map to scroll.

**The owner's choice (2026-10-27): PM1.**

## 4. The battle (the player's turn)

The battle scene (1024 × 410) at the screen's width (1.19 ×) under the camera; the cards and the choices under it.

- **PB1 — the armies stacked.** The enemy's two rows over ours, each army's front row facing the other's across the
  round's gauge, turned across; cards two to a row, 33 × 14 mm, laid out as today's (the face left with its four
  figures, the text right), the text 2.0-2.3 mm; our cards near the thumb (their Auto-Atk / Auto-Def toggles, the long
  press). Under them the **deck**: the acting unit's commands in a column from the foot up (Defend lowest, Retreat red
  at the top, as the tablet's rail), the target's attacks at the right thumb (Melee gold with the damage forecast's
  strip), the battle's Skip a pill at the deck's top right, TARGET and its name in the deck's heading. A tap on an
  enemy card moves the target, as on the tablet.
- **PB2 — the armies side by side**, ours left and theirs right as in the scene: four columns of tall cards, 16 × 25
  mm, the face over the name, the text 1.7-2.2 mm; the same deck under them.

**Recommendation: PB1** — the larger cards and type, today's card layout, our own cards at the thumb. PB2 keeps the
scene's sides but its cards are half as wide.

**The owner's choice (2026-10-27): PB1 as the base, the armies swapped — ours on top, the enemy's at the foot — and
the battle's Skip between the cards and the deck.** Drawn as round P2 (§6).

## 5. Open questions

1. **The line breaks upright.** Kept (each window kind's type as large as its longest line: 3.6 mm and 2.6 mm), or
   rewrapped at one size for every window (e.g. 3.2 mm), as the battle's balloons already are? Rewrapping touches
   no rule; it changes how a page looks.
2. ~~**The scenes:** PS1, PS2 or PS3.~~ None (owner, 2026-10-27); open, put off.
3. ~~**The map:** PM1 or PM2.~~ PM1.
4. ~~**The battle:** PB1 or PB2.~~ PB1, changed: round P2 (§6).
5. **The bar's cells** (Menu, Log, Skip): at the left, as the tablet's drawer, or at the right under the right thumb?
6. **Turning the phone:** the game turns with the phone at any moment (the sensor), or the player chooses upright,
   landscape or "turns with the phone" in Option → Display?
7. **What comes next**: our full screens upright — the deployment table and Subordinates (round P3, §7), the enemy
   attack and the week report, Harem, Treasure, Save/Load, All Data, the options window, the title and the import
   screen, the dungeon's ladder and entry menu, the battle's other states —, then the landscape phone.
8. **The deployment table** (round P3): PD1, PD2 or PD3; the Subordinates screen: PU1 or PU2.
9. **The enemy attack** (round P4): PA1 or PA2; the week report: PW1 or PW2.
10. **Harem, Treasure, Save/Load** (round P5): PH1 or PH2, PT1 or PT2, PL1 or PL2.
11. **The options window and the dungeon's menus** (round P6): PO1 or PO2; PF1 or PF2.

The agent's own choices in the mock-ups, for the owner to confirm: Menu, Log and Skip in one bar at the foot; a `]`
menu taking the bar's place; the map's plates drawn larger than the map's scale (their text 1.7-2 mm whatever the
zoom).

## 6. Round P2: the battle on PB1 (owner, 2026-10-27)

PB1 with the owner's changes, drawn by the same script in the battle's three states; the scene, the cards and the deck
are PB1's.

- **PB3 — the player's turn.** From the top: the scene; "Rance Army" and our two rows (the back row first, then the
  front row); the round's gauge, turned across, where the two front rows face each other; the enemy's front row, then
  its back row, "Leazas Rebel Army" and its advantage; **Skip** on a strip of its own; the deck (the acting unit's
  commands from the foot up, Retreat at the top, the target's attacks at the right thumb). The cards 33.4 × 13.1 mm
  (230 phone px high, text 2.0-2.2 mm), the deck's rows 7.0 mm. The enemy's cards are now within the thumb's reach, so
  a tap moves the target there; our own cards (their Auto-Atk / Auto-Def toggles, the long press) went up.
- **PB3r — the retreat question**: at Retreat, over the deck's right column (the tablet's A2): the script's question,
  Cease Retreat nearer the button, Retreat red and last, the answers 7.9 mm high.
- **PB3x — an exchange** (the gallery's: Ex (commander 1) on Rance (unit 1), both by Melee): the counters at the scene's
  foot as today (their figures 4 mm), the fighters' cards lit with their action's pill, the other cards dimmed; the deck
  empty while the exchange plays, as the tablet's rails are; Skip in its place.

The agent's choice, for the owner to confirm: Skip at its strip's left, the tablet's corner (the strip has room for
more beside it).

## 7. Round P3: the deployment table and Subordinates (2026-10-27)

The base versions of our full screens upright begin with the two that share the unit ledger (docs/ui/unit-ledger.md):
the deployment table (`Sﾌﾞﾀｲ`) and the Subordinates screen (`Sﾌﾞｶ` and `Cﾌﾞｶ`'s troop menu). Drawn by
`mock_phone.py --round P3` over the gallery's dumps (the week-18 save's nine units, the party sent against Oaks (6):
Rance (unit 1), Haurein (unit 12), Cordoba (unit 8), Barres (unit 6)); the faces are the units' own (`Unit_FaceCg`,
cut as `UnitFaces` cuts them). From this round the notes stand in a column beside the phone, the screens being full.

On the PC the ledger's rows are one line of 17 layout pixels' text: upright that is 0.9 mm. Two answers are drawn:

- **Two-line rows** (PD1, PD2, PU1, PU2): each unit's row holds the unit — its seal, №, name, and on Overview its type
  and rows — over the chosen tab's figures; **the tabs change only the second line**. Overview's second line is the
  troops with their bar, Refill (a button on the deployment table, a price on Subordinates, as on the PC) and the cost;
  Troops' its ten figures under their groups; Commander's Strat%, HP, Lvl, EXP, C.Atk, C.Def and the item. Rows
  8-8.5 mm, the names 2.5 mm, the figures 2.2 mm; nine units fit with no scrolling, a longer list scrolls under the
  finger (touch.md, "The lists scrolled by a finger").
- **One-line rows** (PD3): the PC's table with its rows 6.4 mm and its text 2.3 mm; the seal, № and name stay while the
  rest scrolls sideways under a finger, as the tablet's table does — 13 rows in view, but Overview's Refill and Cost
  are a drag away.

The deployment table (`PD…`):

- **PD1 — the ledger on top, the party at the thumb.** The PC's arrangement turned: the ledger, then the party
  panel — its 2×2 formation as on the PC (the back row left, the front row right, facing right; cards 32 × 13 mm with
  the face, the place's badge, the troops and the cost, their ✕ 4.8 mm, a drag swaps places), the budget, the troops
  and the war expenses — and Stop | **Attack** in the bar, Attack at the right thumb. A tap on a row picks the unit or
  puts it back.
- **PD2 — the party on top, the ledger at the thumb.** The party laid out as the battle shows our army (PB3): the back
  row, then the front row, facing down; the ledger under it, nearest the thumb, as PB3 puts the enemy's cards — what is
  tapped most is lowest. Shown with the Troops tab. **PD2d**: a long press on a row raises the unit's picture panel —
  the original's picture and its words about the unit, which the PC shows while the row is hovered — at the screen's
  width (2.2 ×, its words 1.9 mm); a tap anywhere puts it away. PD1 and PD3 take it the same way.
- **PD3 — the table in one-line rows**, scrolled sideways (above); the party as a strip of four at the thumb (the back
  row, then the front row, facing right).

**Recommendation: PD2** — picking units is the screen's main work, so the ledger sits within the thumb's reach and
the party reads as it will in the battle; the party's ✕ and drags (rarer) went up. PD1 keeps the PC's order and
formation; PD3 shows more rows at the cost of the smaller type and the sideways drag.

The Subordinates screen (`PU…`):

- **PU1 — the ledger on top, the orders at the thumb.** The ledger as the deployment table's, the chosen unit's row
  vermilion; under it the orders for that unit: Replenish (7.7 mm; greyed and "full" when the unit is, as Rance (unit 1)
  is), Add New Troops as six buttons in two rows (7.3 mm; greyed above the budget), and in the bar Talk (with the
  unit's face) and End at the right thumb. **PU1q**: a troop order asks first in a callout under its button (the
  house's C3, as on the PC), the chosen row still in view; Yes / No 8 mm high. Haurein (unit 12): Replenish +1,191
  troops for 2,300K, the gallery's; her Add New Troops prices +400 and +800 are the gallery's, the cheaper ones derived
  (each step three times the one before, as Rance's).
- **PU2 — the ledger whole, a unit's sheet.** The ledger fills the screen (shown: the Commander tab), End in the bar; a
  tap on a row raises the unit's sheet over the ledger's foot, as a province's on the map (PM1c) — its face, figures
  and orders —, Return where End was. More of the ledger while no sheet is up, one tap more per unit.

**Recommendation: PU1** — the orders always show for the chosen unit, one tap fewer, and the two screens share their
layout's order (the ledger, then what acts on it at the thumb) — with PD2 the deployment's ledger is the lower part
instead, since there the ledger is what is tapped.

The agent's own choices, for the owner to confirm: our full screens upright have no Menu / Log / Skip bar (their own
buttons take the foot; the map's bar is back when they close); the picture panel only on a long press.

## 8. Round P4: the enemy attack and the week report (2026-10-27)

Drawn by `mock_phone.py --round P4` over the gallery's `enemy-attack-scouted` (the rebels on Ys (5) after Kanami's
scouting: Ex (commander 1), Sakanaku (commander 6), Dodge (commander 5), Jibul (commander 7)) and `week-report`, with
Maris's and Kanami's own face CGs (6, 9).

The enemy attack (`PA…`; docs/ui/enemy-attack.md, the owner's B with B4's list, P1's Abandon City, the touch mode's
mirrored answers):

- **PA1 — a screen of its own, Ys over the rebels.** The PC's screen stacked: the news ("The Leazas Rebel Army appeared
  around Ys.") and the date; Ys's picture with its four figures; VS; the rebels' cards (35 × 15 mm, names 2.4 mm), their
  front row first and facing up at Ys — as PB3's enemy faces our army —, the commanders and troops, the report's
  source; Maris's question in her own breaks (2.4 mm). The answers in two rows: **Abandon City** (the danger button,
  its white flag) top left, farthest from the thumb, **Ninja Scout** beside it (7.3 mm); **Intercept** the bar's width
  at the thumb (8.4 mm). **PA1n**: Ninja Scout's list above its button (B4): a row per ninja in service (8.5 mm), the
  spent dimmed, a tap on a free one sends her; Abandon City's question is a callout above its button the same way.
- **PA2 — over the map.** The map at PM1's scale with Ys ringed, under the news; the attack rises as a sheet as a
  province's does (PM1c): Ys's figures in two columns, the rebels as rows (face, place, name, type, row, troops; 6.7 mm),
  Maris's question, the answers as PA1's. The map says where at a glance; the type is smaller (the figures 1.8 mm).

**Recommendation: PA1** — the screen the owner chose on the PC, with nothing smaller than 2 mm; where the attack falls
is in the news and Ys's picture, and the map is back right after.

The week report (`PW…`; docs/ui/turn-start.md):

- **PW1 — over the map, in the middle.** The PC's card at the screen's width over the dimmed map: the banner (3.5 mm),
  the treasury as an account (2.5 mm), the budget (3.5 mm), the troops replenished, the triangle; a tap anywhere goes
  on; the scenes' bar (Menu, Log, Skip) at the foot. The thieves' week: the banner alone on the same card.
- **PW2 — a sheet at the thumb.** The same card over the map's foot, where the command panel rises next (PM1b); the
  triangle at the right thumb.

**Recommendation: PW2** — the eye stays low from the report to the commands, and the map above it shows the week's
start.

## 9. Round P5: Harem, Treasure and Save/Load (2026-10-27)

Drawn by `mock_phone.py --round P5` over the gallery's `harem` (Anasel chosen), `treasure-items` (the L. Holy Sword
(item 6) on Rance (unit 1), By Lord (item 7) on Rick (unit 7)) and `save-load` (four manual saves, the newest chosen).
All Data is left for a later round: the screen gallery has no entry of it to draw over.

The pickers keep the original's grid of 3 × 17 cells (the owner kept the numbered grid, docs/ui/pickers.md) — three
cells to a row fit the phone's width. Two ways:

- **PH1 / PT1 — the grid whole, the choice at the thumb.** The whole grid under the title (the harem's cells 5.3 mm,
  the treasure's 4.8 mm under its Characters | Items tabs), the chosen girl's or item's panel at the thumb — a small
  picture, the name, the visits and last summons or the holder — and the bar: End | **Summon Anasel** / **Take Off**,
  the red button at the right thumb (the touch mode's round X: a tap chooses, the red button acts, greyed with the
  refusal's reason). The picture's own words are 1 mm there: a long press shows it whole (as PD2d).
- **PH2 / PT2 — the picture on top, the grid scrolled.** The picture at the screen's width (its words 2.4 mm — for an
  item they say what it does), the details under it, the grid in taller cells (7.3 / 6.7 mm), seven or eight rows in
  view, the rest a drag away.

**Recommendation: PH1 for the harem, PT2 for Treasure** — the harem's picture is the girl and her line, the grid is
what is chosen from; an item's picture carries what the item does, which the player reads before equipping it.

Save/Load (`PL…`; docs/ui/save-load.md, the owner's G with decisions 29 and 31):

- **PL1 — the list, then the save's page.** Two pages. **PL1**: Load | Save at the title (from `COMMAND`), the manual
  saves and the auto-saves as two tabs, newest first, "Delete all without ★ (2)"; rows 9.8 mm — the number on its seal
  (a gold star with a comment), the date, the time written, the comment as the second line; in Save "+ New save" heads
  the list. A tap opens **PL1p**: the save's title and Delete (the danger button) on top, its map at the screen's width,
  its figures in two columns, the comment and Edit; Back | **Load** in the bar.
- **PL2 — one page.** The list in one-line rows (5.9 mm, the comments only on the page) scrolling in its room, the
  chosen save's page under it, its map cut lower; one tap fewer.

**Recommendation: PL1** — rows and type a finger can take, and the map at its full width; the second page is the
phone's usual way down a list.

## 10. Round P6: the options window and the dungeon's menus (2026-10-27)

Drawn by `mock_phone.py --round P6` over the gallery's `options-messages`, `options-tweaks`, `floor-ladder-middle` (the
Monsters' Cave (dungeon 2) on floor 7) and `dungeon-entry`. The title (`ｽﾀMENU`, the original's own screen) and the
import screen have no entry in the screen gallery to draw over; they wait for one.

The options window (`PO…`; docs/ui/options.md, mock-up D):

- **PO1 — the paper sheet, its tabs on top.** The window as a full paper sheet, its five tabs on top as on the PC
  (6.3 mm); one column of settings, each a row with its control at the left (the row's whole height taps), its name
  2.3 mm, its explanation 1.7 mm; the text speed's preview the game's window at 2.7 mm; the read-text colours in two
  rows. Defaults | **Close** in the bar.
- **PO2 — the tabs at the thumb.** The same sheet, the tabs as a bar of lacquer cells over Defaults | Close (7.4 mm),
  the chosen tab's name at the title's right. Shown: Tweaks, its radio choices as finger rows (7 mm).

**Recommendation: PO2** — the tabs are what is tapped most here, and a phone's tab bar is at its foot; the sheet
above is read, not tapped.

The dungeon's menus (`PF…`; docs/ui/dungeon-menu.md, the touch mode's rail), over the map as on the PC:

- **PF1 — the ladder as the tablet's rail.** The floor's picture at the screen's width (2.1 ×) above its menu; the
  script's window at its own breaks (3.3 mm) at the left; the ladder at the right thumb — Retreat on top, Ascend, the
  floor, Descend in the corner, rungs 8.5 mm, a way not offered hidden in its room. **PF1e**: before a search, Return on
  top and Start Search in Descend's corner (with four items the rail grows upwards).
- **PF2 — the ladder laid across the foot.** The script's window under the picture at the screen's width (3.5 mm);
  Retreat across the screen, Ascend | the floor | Descend under it — wider rungs, but Ascend on the left thumb's side.

**Recommendation: PF1** — the tablet's rail as the owner chose it (touch.md's B), the same place for the thumb on both
devices.
