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
**Smartphone, upright (mock-ups)**, marked as not built (`tools/ui/gallery.ps1 -PhoneOnly`; docs/ui/gallery.md, §9). Their variants are named **P…** (portrait),
and from round P8 (§12) the landscape phone's **L…**, apart from touch.md's landscape sketches B1, M1 and the rest.

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

**The owner (2026-10-28): PS2 as the base, changed** — the picture and its portraits in the screen's middle; the upper
portrait's speech in a window above the picture, the lower one's under it, with no name plate (the name is on the
portrait); PS2b and PS2m as drawn, but with the picture (and the window) in the middle too. Redrawn as round P14 (§18).

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
   no rule; it changes how a page looks. PS2 (round P14) keeps them; for the owner to confirm (round P17, §21).
2. ~~**The scenes:** PS1, PS2 or PS3.~~ None (owner, 2026-10-27); then PS2, changed (owner, 2026-10-28): round P14,
   §18.
3. ~~**The map:** PM1 or PM2.~~ PM1.
4. ~~**The battle:** PB1 or PB2.~~ PB1, changed: round P2 (§6).
5. **The bar's cells** (Menu, Log, Skip): at the left, as the tablet's drawer, or at the right under the right thumb?
   Drawn as round P17 (§21): PK1 or PK2.
6. **Turning the phone:** the game turns with the phone at any moment (the sensor), or the player chooses upright,
   landscape or "turns with the phone" in Option → Display? Sketched as round P17's PO1o (§21).
7. **What comes next**: our full screens upright — the deployment table and Subordinates (round P3, §7), the enemy
   attack and the week report, Harem, Treasure, Save/Load, All Data, the options window, the title and the import
   screen, the dungeon's ladder and entry menu, the battle's other states —, then the landscape phone.
8. ~~**The deployment table** (round P3): PD1, PD2 or PD3; the Subordinates screen: PU1 or PU2.~~ PD2 and PU1,
   PU1 changed (§7).
9. ~~**The enemy attack** (round P4): PA1 or PA2; the week report: PW1 or PW2.~~ PA1, changed, and PW1 (§8).
10. ~~**Harem, Treasure, Save/Load** (round P5): PH1 or PH2, PT1 or PT2, PL1 or PL2.~~ PH2, PT2 and PL2, PL2 changed
    (§9).
11. ~~**The options window and the dungeon's menus** (round P6): PO1 or PO2; PF1 or PF2.~~ PO1 and PF1.
12. **The battle's opening** (round P7): PB4o1 or PB4o2 — not answered yet (2026-10-27). The other states (PB4s, PB4c,
    PB4r, PB4b) as drawn. Round P16 (§20) adds PB4o3, PB4o1's place laid out again as landscape's LB3o1.
13. **The unit ledger's rows upright** (the owner, 2026-10-27, thinking): PD2's layout holds, but the ledger is still
    to be thought over — most likely it will scroll sideways too. Drawn as round P16 (§20): PD2s1 or PD2s2.
14. **Subordinates' End** (the owner, 2026-10-27, thinking): where End goes now that Talk has left the bar. Drawn as
    round P16 (§20): PU1e1, PU1e2 or PU1e3.
15. ~~**The landscape map** (round P8, §12): LM1 or LM2.~~ LM1.
16. ~~**The landscape battle** (round P8, §12): LB1 or LB2.~~ LB2.
17. ~~**The landscape deployment table** (round P9, §13): LD1 or LD2.~~ LD2, changed.
18. ~~**The landscape Subordinates screen** (round P9, §13): LU1 or LU2.~~ LU1, changed.
19. ~~**The landscape enemy attack** (round P10, §14): LA1 or LA2.~~ LA2, changed.
20. ~~**The landscape week report** (round P10, §14): LW1 or LW2.~~ LW1.
21. ~~**The landscape Harem** (round P11, §15): LH1 or LH2.~~ LH2.
22. ~~**The landscape Treasure** (round P11, §15): LT1 or LT2.~~ LT2.
23. ~~**The landscape Save/Load** (round P11, §15): LL1 or LL2.~~ LL1.
24. ~~**The landscape options window** (round P12, §16): LO1 or LO2.~~ LO1.
25. ~~**The landscape dungeon's menus** (round P12, §16): LF1 or LF2.~~ LF2.
26. ~~**The landscape retreat question** (round P13, §17): LB2r1 or LB2r2.~~ LB2r1.
27. ~~**The landscape opening** (round P13, §17): LB3o1 or LB3o2.~~ LB3o1 (upright's question 12 is still open).
28. ~~**The landscape battle's other states** (round P13, §17): LB2x, LB3s, LB3c, LB3r and LB3b as drawn?~~ As drawn.
29. ~~**What comes next** (the owner, 2026-10-28).~~ The scenes (§2), upright first: round P14 (§18).
30. ~~**The scenes upright** (round P14, §18): PS2, PS2l, PS2b and PS2m as drawn?~~ As drawn, the agent's choices with
    them.
31. ~~**The scenes landscape** (round P15, §19): LS1 or LS2.~~ LS1, with LS1b and LS1m.
32. ~~**What comes next** (the owner, 2026-10-28): the open questions upright — 12 (the opening), 13 (the ledger's
    rows), 14 (Subordinates' End), 5 (the bar's cells) and 6 (turning the phone); 1 (the line breaks) looks answered by
    PS2, which keeps the script's breaks, for the owner to confirm.~~ Drawn as rounds P16 (§20: 12, 13, 14) and P17
    (§21: 5, 6, 1).
33. **The agent's own choices in rounds P16-P17**, for the owner to confirm: each ledger tab keeps its own sideways
    scroll, which glides (the columns are not stepped); the phone's Back gesture ends Subordinates wherever End stands.

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

**The owner's choice (2026-10-27): PD2 and PU1.** PD2's layout as drawn, its ledger still to be thought over (most
likely it scrolls sideways too; §5, question 13). PU1 changed: **Replenish and Talk on one row**, Replenish left and
Talk (with the unit's face) right, where Replenish alone stood (each 33 × 7.7 mm); End alone in the bar for now — where
it goes is the owner's open question (§5, 14). Redrawn as PU1 and PU1q.

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

**The owner's choice (2026-10-27): PA1, changed, and PW1.** PA1's Abandon City is **10% narrower** (30 mm instead of
33), the room it gives up widening the gap to Ninja Scout (4.3 mm instead of 0.9); Ninja Scout and Intercept as drawn.
Redrawn as PA1 and PA1n (PA2 shares the row).

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

**The owner's choice (2026-10-27): PH2, PT2 and PL2, changed.** PL2 left about 35 mm free under the save's page; now **the
page stands on the bar and the list takes the room above it**: nine one-line rows in view instead of four, the rest a
drag away (a scroll bar at the right). Drawn with twelve manual saves: the gallery's four, renumbered 12-9, and eight
older ones made up.

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

**The owner's choice (2026-10-27): PO1 and PF1.**

## 11. Round P7: the battle's other states (2026-10-27)

Drawn by `mock_phone.py --round P7` on PB3's board, over the gallery's `opening`, `scout-report`, `strategy`,
`battle-speech` and the battle's figures (the capture's battle on Oaks (6)). The board's layout is chosen, so each
state is one base version, the opening two.

- **The opening.** **PB4o1**: as on the PC, at the scene's place (0.95 ×): the armies' names 3.2 mm, but the plate's
  place and limit 0.9 mm. **PB4o2**: turned upright over the scene and the board as PB3 stands — ours blue on top, the
  enemy red at the foot, the plate between (the place 2.6 mm, the limit 2 mm), VS at the gauge's place; the cards fly in
  under it as it fades. **Recommendation: PB4o2** — its type is readable and it shows the armies where the board puts
  them.
- **PB4s — the ninja's scout report**, before the field, as a screen of its own: Kanami and her report on top, the
  rebels' cards as the enemy attack's (PA1), front row first, Rance's answer at the foot; one tap goes on.
- **PB4c — the strategy's cut-in**: the result and its word on top, the strategist on the band with what it does
  (4 mm), his line under it (2.6 mm); one tap. Barres (unit 6), his line the gallery's; the enemy's the same in red.
- **PB4r — the results plate** over the dimmed board at the screen's width (the title 5 mm, the damages 6 mm); a
  level-up's page, a fall's box and our narration panel the same way.
- **PB4b — a fighter's line**: the portrait at the scene's left as on the PC, our balloon over the scene's foot at
  3.5 mm (rewrapped, as the balloons ever are); a defence's caption the same way, at 2.4 mm.

With this round every screen of ours has an upright base version but All Data, the title and the import screen, which
wait for screen gallery entries to draw over (§9, §10), and the scenes (§2, open).

**The owner (2026-10-27):** PB4s, PB4c, PB4r and PB4b as drawn; the opening (PB4o1 or PB4o2) not answered yet.

The chosen base versions of rounds P3-P7 are on the gallery's phone tab (docs/ui/gallery.md, §9): PD2 (and PD2d), PU1
(and PU1q), PA1 (and PA1n), PW1, PH2, PT2, PL2, PO1, PF1 (and PF1e), and PB4s, PB4c, PB4r, PB4b with the battle.

## 12. Round P8: the landscape phone — the frame, the map, the battle (2026-10-27)

The phone held landscape, 2712 × 1220 (154.5 × 69.5 mm). The game's 1280 × 800 layout fitted to the height is 1952 ×
1220 (111 × 69.5 mm), a layout pixel 1.525 phone pixels (0.087 mm), with black sides of 380 px (21.6 mm) on either hand.
Drawn by `mock_phone.py --round P8` over the gallery's touch dumps and the map's own pictures; the variants are named
**L…** (LM the map, LB the battle; touch.md's B1, M1 were this device's first sketches).

The device's edges in landscape: the camera's hole sits at a side's middle — the left or the right, as the phone is
turned —, so **110 px (6.3 mm) is kept free on both sides** (drawn on the left); the gesture zone is at the foot (50
px, 3 mm). Held in both hands at the sides, the thumbs (~70 mm round a grip low on each side, as on the tablet) reach
all but the top's middle: the height is no problem here, the sizes are.

**What of the tablet's touch layout works at this size** (sheets L0b, L0m — today's layout fitted to the height):

| Part | On the landscape phone | |
|---|---|---|
| The scenes' own text (48 px em), the map's picture (3.05 phone px a map pixel, a flag 4.2 mm) | 4.2 mm | works |
| The drawer's cells 50 × 84 | 4.3 × 7.3 mm | high enough, narrow |
| The map's Commands 220 × 72 | 19 × 6.3 mm | nearly |
| A battle card 223 × 150, its text (17 px) | 19.4 × 13 mm, 1.5 mm | the text too small |
| A battle rail button 116 × 54; Melee 116 × 76 | 10.1 × 4.7 mm; 6.6 mm high | too low |
| The cards' order toggles 66 × 32 | 5.7 × 2.8 mm | too small |
| A map plate ~30 px, its text; the status strip's text (18 px) | 2.6 mm, 1.4 mm; 1.6 mm | too small |
| The command panel's and the province menu's rows (46 px) | 4.0 mm | too low |

So the scenes and the map's picture stay as they are, and **every screen of ours needs larger type and targets** (2-3.5
mm, 7 mm), as upright; but here the height is short, not the width, and the black sides are free room. The rails can
leave the frame for the sides (14.5 mm wide between the hole's strip and the frame, `RAIL_L`/`RAIL_R`), which gives the
frame's own width back to the board and the map.

The map (`LM…`): plates with 2 mm text (36 px) over the map at the PC's scale (3.05) in both.

- **LM1 — the frame kept, the black sides as rails.** The map as on the tablet in the frame (the PC's view, 640 × 400
  map pixels), nothing over it. Left side: Menu (the menu bar's commands) on top, the status stacked (the date, the
  budget, the troops), the drawer's Skip and Log at the thumb (6.7 mm high). Right side: the week's uses as the strip's
  tags by group, Commands at the thumb. **LM1b**: the command panel in the sides — the week's commands left, Turn END on
  the right's top and the other items from the foot up, Close where Commands was; the rows 14.5 × 7.0 mm, the tag over
  the name in two lines (a side has no room for both in one). **LM1c**: Oaks (6) ringed, its card beside it as on the
  tablet (the owner's B), its type 2-2.5 mm; its commands on the right side, Return at the foot.
- **LM2 — the map across the whole screen** (as PM1 upright). The map at the same scale over the whole phone: 889 × 400
  map pixels in view, two fifths more than the PC's; the status strip over its top in two rows (2-2.2 mm: the status
  window's lines, then the week's uses by group), Menu at its right; the drawer at the left thumb and Commands at the
  right one, over the map as on the tablet. **LM2b**: the tablet's rails as lacquer over the map's sides, 29 mm wide,
  the commands in one line with their tags (Construction's weeks on its chip), the strip still seen. **LM2c**: Oaks's
  card beside it, its commands on a rail only as wide as the black side (two-line rows as LM1's): Oaks lies near the
  map's east edge, which the view cannot pass, so a wider rail would cover it. The map left of map x 1280 shows the
  game's first owners (made up).

**Recommendation: LM2** — the owner's PM1 turned (the map fills the screen, the panels over it), two fifths more of the
map, and a strip that reads in two rows; LM1 keeps the map uncovered, but its sides are only 14.5 mm wide, so the tags
stack and the command rows take two lines.

The battle (`LB…`), the player's turn as the tablet's (touch.md, "The battle within the thumbs' reach"), its rails moved
into the black sides — the acting unit's commands left from the foot up (Retreat on top), the target's attacks right
(Melee 14.8 mm with the damage forecast at the thumb), rows 14.5 × 7.0 mm; the battle's Skip on the left rail's top, where
it stays while the exchanges play and the rails are gone:

- **LB1 — the PC's board under the whole scene.** The scene whole, as on every device (111 × 35.6 mm); with the rails
  out of the frame the board has the PC's width again: cards 25.5 × 13.0 mm, the text 1.9-2.2 mm (34-38 px), a unit's
  state at its HP line's end. The order toggles do not fit a finger at this height (2.8 mm): the other own cards' orders
  go back to the long press.
- **LB2 — the scene lower, the cards taller.** The scene's own picture 24.9 mm high (70%; as large as upright's PB3,
  27.8 mm), its sides blurred out to the frame; the cards 25.5 × 19.0 mm, the text 2.3-2.5 mm (40-45 px); Rick's order
  toggles across the card's foot, 4.6 mm high (the tablet's 6.5).

**Recommendation: LB2** — every card's text above the 2 mm floor and the owner's toggles kept on the cards, while the
scene stays as large as the one the owner chose upright; LB1 keeps the scene whole at the cost of the smallest type
and the toggles.

The agent's own choices, for the owner to confirm: the hole's 6.3 mm kept free on both sides (either may hold it); Menu
as a cell on the map (the menu bar is 2 mm high here); the battle's Skip on the left rail's top.

**The owner's choice (2026-10-27): LM1 and LB2.** So in landscape the frame keeps the layout's 16:10 and our controls
live in the black sides wherever they fit (the map), and the battle trades some of the scene's height for the cards.

## 13. Round P9: the deployment table and Subordinates, landscape (2026-10-27)

Drawn by `mock_phone.py --round P9` on LM1's principle — the frame holds the screen, the black sides the title, the
figures and the buttons at the thumbs — over the same data as round P3 (the week-18 save's nine units, the party sent
against Oaks (6): Rance (unit 1), Haurein (unit 12), Cordoba (unit 8), Barres (unit 6)).

In landscape the frame is wide enough for the PC's ledger in **one-line rows at the phone's type**: rows 6.4 mm (112
px), names 2.3 mm, figures 2.2 mm, and every column of a tab in one line — Overview's №, name, type, rows, troops with
their bar, Refill and cost; Troops' ten figures under their groups — so nothing scrolls sideways (upright's open question
13 does not arise here). The tabs and sorting as on the PC; a long press on a row shows its picture (PD2d).

The deployment table (`LD…`):

- **LD1 — the party across the top, the ledger under it.** The party as the battle lays our army out, the back row left
  and the front row right (cards 26 × 11.4 mm with the face, the place's badge, the troops and the cost, ✕ 4.8 mm), the
  ledger under it with seven rows in view; the title, the date and the budget over Stop in the left side, the troops
  and the war expenses over Attack in the right.
- **LD2 — the ledger fills the frame, the party in the right side.** All nine rows in view; the party in the right side
  as the battle's rows read, the back row over the front, each card 14.5 × 11.2 mm (the face, the place, the cost, the
  name, the troops) — no ✕ at that width: a unit leaves the party by a tap on its row, as it joined. Attack at the right
  thumb, Stop at the left; the title, the date, the budget, the troops and the war expenses in the left side.

**Recommendation: LD2** — every unit in view and the party at the thumb that sends it; LD1 shows the party larger, with
its ✕, but the ledger then scrolls.

The Subordinates screen (`LU…`), shown with the Troops tab:

- **LU1 — the ledger fills the frame, the orders in the sides.** The right side holds the chosen unit's orders from the
  foot up: Replenish lowest (greyed and "full" for Rance (unit 1)), the six Add New Troops over it, the dearest on top
  (greyed above the budget); the left side the title, the date and the budget, then Talk (the unit's face) over End at
  the left thumb, 8 mm each. **LU1q**: Haurein chosen, Replenish +1,191 troops for 2,300K asks first in a callout left
  of its button (the house's C3), No | Yes 7.4 mm high, Yes nearer the thumb.
- **LU2 — the ledger over the orders, as PU1.** The owner's upright choice laid across: the ledger on top (five rows in
  view), under it the orders' deck — Replenish and Talk on one row (the owner's change), Add New Troops in two rows of
  three; End alone in the right side at the thumb.

**Recommendation: LU1** — all nine units in view while the orders stay at both thumbs, the same arrangement as LD2's;
LU2 keeps upright's order of things, at the cost of four rows.

**The owner's choice (2026-10-27): LD2 and LU1, both changed**, redrawn in their places:

- **LD2: the party two cards a row, as the battle shows our army** (the back row left, the front row right; cards 24.7
  × 12.2 mm with their ✕ 4.8 mm). That widens the right part, so **the ledger stands wholly at the left** (from the
  hole's strip to x 1700, its columns drawn closer, the type the same: all nine rows in view) and **everything else goes
  right**: the title, the date and the budget over the party, the troops and the war expenses under it, Stop | Attack at
  the right thumb.
- **LU1: the sides swapped, Replenish kept right.** The left side holds the chosen unit's six Add New Troops from the
  foot up (the dearest on top); the right side the title, the date and the budget, then Talk, **Replenish between Talk
  and End**, End at the thumb. LU1q's callout stays left of Replenish.

Both are on the gallery's landscape tab with LM1 and LB2.

## 14. Round P10: the enemy attack and the week report, landscape (2026-10-27)

Drawn by `mock_phone.py --round P10` over the same data as round P4 (§8): the gallery's `enemy-attack-scouted` (the
rebels on Ys (5) after Kanami's scouting: Ex (commander 1), Sakanaku (commander 6), Dodge (commander 5), Jibul
(commander 7)), `week-report` and LM1's map, with Maris's and Kanami's own face CGs (6, 9).

The enemy attack (`LA…`; upright PA1 as the owner changed it):

- **LA1 — the PC's screen in the frame, the answers in the sides** (LM1's principle). The frame holds the PC's B at the
  phone's type: the news across the top, Ys (its picture and four figures, 2.2 mm) left, the rebels' cards right as on
  the PC — the front row left, nearer Ys and facing it — (30.4 × 12.0 mm, names 2.4 mm), Maris's question across the
  foot in her own breaks (2.5 mm). The answers in the sides: **Intercept** at the right thumb (22.8 mm high), **Ninja
  Scout** at the left thumb, **Abandon City** (the danger button, its white flag) at the left side's top, out of the
  thumbs' way; the date at the right side's top, as the status stands in LM1. **LA1n**: Ninja Scout's list (B4) opens
  right of its button over Maris's question, a row per ninja (8.5 mm); Abandon City's question the same way.
- **LA2 — across the whole width, the answers in a column at the right thumb** (the screen's own split, as LD2's). The
  facts at the left over the whole height — Ys (its picture 30.8 mm wide), the rebels' cards taller (24.9 × 17.1 mm) —;
  the right column is upright's PA1 foot under the news: the title, the date and the news, Maris's question, Abandon
  City (10% narrower, the owner's change) | Ninja Scout, Intercept at the thumb (14.8 × 45.8 mm). **LA2n**: the list
  above Ninja Scout's button, over Maris's question, as upright's PA1n.

**Recommendation: LA2** — the question stands right over its answers, as on the PC and upright, and one thumb answers
it; the rebels' cards get the height. LA1 keeps the frame whole, but its answers are split between the thumbs and the
sides' 14.5 mm make their labels smaller.

The week report (`LW…`), before `COMMAND`'s map wait, so the map has no plates (as on the PC):

- **LW1 — the report in the middle over the dimmed map.** The PC's card (and upright's PW1) in the frame, 75.2 × 53.5
  mm: the banner 3.5 mm, the treasury as an account 2.5 mm, the budget 3.5 mm, the troops replenished, the triangle; a
  tap anywhere goes on; the left side as in the scenes (Menu on top, Skip and Log at the thumb).
- **LW2 — the report in the sides, the map clear.** The frame shows the map undimmed as the week starts; the left side
  holds Menu, the banner and the date where LM1's status will stand, Skip and Log at the thumb; the right side the
  treasury stacked as an account (the figures 2.5 mm, the budget 2.8 mm), the troops replenished, the triangle at the
  right thumb, where Commands comes next.

The thieves' week: the banner alone, on LW1's card or in LW2's left side.

**Recommendation: LW1** — the owner's PW1 and the PC's card at the largest type, the account read as one piece; LW2
keeps the map in view, but its account is narrow and its labels small.

**The owner's choice (2026-10-27): LA2, changed, and LW1.** LA2 redrawn in its place:

- **The left and the middle panels' content in their middle**: Ys's picture and figures, the rebels' cards and their
  totals centred in the panels' height; the captions stay at the panels' top.
- **Intercept half as high** (7.4 mm instead of 14.8; its two lines closer), still at the thumb; **Abandon City 10%
  lower** (8.2 mm instead of 9.1), Ninja Scout as drawn (10.3 mm).
- **Five ninjas** — all the game has (Kanami, Freya, Kaoru, Wichita, Saitou; the portraits of `FIGT_B`'s scout menu) —
  **must fit, or the list scrolls**: LA2n now draws all five (made up: the week-18 save has Kanami alone, two of them
  spent), in the scout menu's order in two columns of the right column's width under the news, rows 7.3 mm — every one
  in view, no scroll bar needed.

LA2, LA2n and LW1 are on the gallery's landscape tab.

## 15. Round P11: Harem, Treasure and Save/Load, landscape (2026-10-28)

Drawn by `mock_phone.py --round P11` over the same data as round P5 (§9): the gallery's `harem` (Sill, Soul and
Anasel on offer, Anasel chosen), `treasure-items` (the L. Holy Sword (item 6) on Rance (unit 1), By Lord (item 7) on
Rick (unit 7)) and `save-load`, with PL2's twelve manual saves (the gallery's four renumbered 12-9, eight older ones made
up). The PC's own screens are already laid across, so every variant uses the whole width (as LD2 and LA2), and LL2 also
LM1's principle.

The pickers' grid is 3 × 17 cells, each girl or item at her number's cell (the owner's grid, docs/ui/pickers.md). On the
landscape phone the height is what is short: 17 rows of finger-sized cells (6.7 mm) do not fit it. Two answers:

- **LH1 / LT1 — the PC's split, the grid scrolled.** The picture at the left, 70.6 mm wide (its words 2.5 mm, as
  upright's PH2 and PT2), the details and the red button under it in a panel — Summon Anasel across it (7.7 mm), Take Off
  at its right as the tablet's (round X), with the PC's hint "Tap a character, then the item —"; the title, the date and
  the grid at the right, its cells 6.7 mm (the names 2 mm): **seven of its 17 rows in view**, the rest a drag away (its
  scroll bar). End at the right thumb at the grid's foot, the treasure's Characters | Items beside it (7.4 mm), as on
  the PC.
- **LH2 / LT2 — the grid folded in two, every cell in view.** The grid cut after its ninth row — rows 1-9 at the left,
  10-17 at the right, a gold rule between them — over the whole height at the right thumb: cells 6.9 × 12.0 mm, each
  girl or item still at her number's cell, the order read down the left half, then the right (the harem's 50 cells, the
  51st being the script's resting place; the treasure's 51). The picture 62.9 mm wide (its words 2.2 mm), the title
  over it, the details under it; End | Summon Anasel at the left thumb (8 mm); Take Off beside the item's details,
  Characters | Items and End under them. The cells being narrow, an item's name stands over its holder, and a long name
  shrinks: the L. Holy Sword 1.6 mm, the others 1.9 mm — and the game has many such (Hawaiian Sword, Happiness Ring,
  Ninja Birdlime: 13-14 letters).

**Recommendation: LH2 for the harem, LT1 for Treasure** — the girls' names are short, so the folded grid shows the
whole harem at the thumb with the picture nearly as large; the items' names are long, and folding would shrink them
below 2 mm, so the treasure keeps the PC's split and its scroll. (Upright the owner chose the picture over the whole
grid, PH2 and PT2; LH2 keeps both.)

Save/Load (`LL…`; upright PL2 as the owner changed it: one-line rows 5.9 mm, the comment only on the page):

- **LL1 — the PC's split across the whole width.** At the left the title, Load | Save (from `COMMAND`), the two lists'
  tabs, the heading with the sweep "Delete all without ★ (9)" and the list: **seven of the twelve in view**, the rest a
  drag away (the scroll bar). At the right the chosen save's page: its title with Delete (the owner's B5), the map at
  the page's width (88.6 mm), cut lower to 30.3 mm for the figures in three columns and the comment with Edit; Back |
  **Load** at its foot as on the PC, Load at the right thumb.
- **LL2 — the frame holds the list and the page, the sides the rest** (LM1's principle). The left side: the title,
  Load | Save stacked, the two lists as two cells (Manual · 12 saves, Auto · 20 saves), Back at the left thumb. The
  frame: the list with **nine of the twelve in view**, the page beside it — the map whole (70 × 27.8 mm), the figures in
  two columns, the comment and Edit. The right side: Delete (the danger button, its bin over the word) at the top,
  beside the save's title and out of the thumbs' way; **Load** at the right thumb (14.8 mm high).

**Recommendation: LL1** — the PC's order kept: Back and Load together at the right thumb (as the owner's LD2 put Stop
and Attack), Delete at the save's title, the map the largest; LL2 shows two rows more, as many as the owner's PL2, at
the cost of a smaller map and the buttons split between the thumbs.

**The owner's choice (2026-10-28): LH2, LT2 and LL1**, as drawn — the treasure folded as the harem, so the two screens
keep one layout, its long item names shrunk in their narrow cells. LH2, LT2 and LL1 are on the gallery's landscape tab.

## 16. Round P12: the options window and the dungeon's menus, landscape (2026-10-28)

Drawn by `mock_phone.py --round P12` over the same data as round P6 (§10): the gallery's `options-messages`,
`floor-ladder-middle` (the Monsters' Cave (dungeon 2) on floor 7) and `dungeon-entry`, the latter's map under the
tablet's rail taken from the gallery's `map` (the same camera).

The options window (`LO…`; upright PO1, the owner's choice: the paper sheet, its tabs on top). Upright the Messages tab
is one column 1,360 px high; landscape it needs **two columns** — the text speed (its preview the game's window at
2.7 mm) and the read text's colours at the left, the message skip at the right —, then the whole tab is in view. The
type as PO1's: the names 2.3 mm, the explanations 1.7 mm, the boxes 2.6 mm (the row's whole height taps).

- **LO1 — the sheet across the whole width, its tabs on top** (PO1 laid across): the five paper tabs on top as on the PC
  (6.3 mm), the title at their right; Defaults | **Close** at the right column's foot, at the right thumb (7.4 mm). The
  other tabs split the same way (Tweaks: Battle left, Scenes right).
- **LO2 — the sheet in the frame, the tabs in the left side** (LM1's principle): the five tabs as a rail of lacquer
  cells under the title (7.3 mm each), the chosen one lit, Defaults at the left thumb; **Close** in the right side at
  the right thumb; the sheet's two columns as LO1's, each narrower.

**Recommendation: LO1** — the owner's PO1 as it is, the tabs where the PC and upright have them; LO2 brings them nearer
the left thumb, which the owner did not want upright (PO2).

The dungeon's menus (`LF…`; upright PF1, the owner's choice: the ladder as the tablet's rail, Retreat on top, Descend in
the corner, a way not offered hidden in its room):

- **LF1 — the frame kept, the ladder in the right side** (LM1's principle). The floor's picture and the script's window
  at their places in the frame (the window's text 4.2 mm); the tablet's rail leaves the frame for the right side, from
  the foot up — Descend in the corner, the floor, Ascend, Retreat on top —, rungs 8.5 × 14.5 mm, where LM1 has Commands
  and a province's commands. The left side as LM1c's while a menu is up: Menu and the status, the drawer hidden.
  **LF1e**: before a search, Return over Start Search, its two words in two lines (11.4 mm high); with four items
  (Merim, God's Door) the rail grows upwards.
- **LF2 — the tablet's layout kept, the ladder in the frame**: the rail at the frame's right foot as on the tablet, at
  the phone's sizes — rungs 8.5 × 20.3 mm, the words 2.6 mm in one line, the sub-lines 1.6 mm —, its panel growing
  upwards beside the window; the right side empty. **LF2e**: before a search, Return over Start Search, both in one
  line.

**Recommendation: LF1** — the thumb finds the ladder where it finds every menu of the map in landscape (LM1's right
side); LF2's wider rungs read in one line, but they stand a side's width further in and leave the side empty.

**The owner's choice (2026-10-28): LO1 and LF2**, as drawn — the dungeon keeps the tablet's rail in the frame, its rungs
wide enough for every word in one line. LO1, LF2 and LF2e are on the gallery's landscape tab.

## 17. Round P13: the battle's other states, landscape (2026-10-28)

Drawn by `mock_phone.py --round P13` on LB2's board (the scene's own picture 70% high, its sides blurred out to the
frame; the cards 25.5 × 19.0 mm; the rails in the black sides), over the gallery's touch dumps `battle-turn`,
`exchange-counters`, `opening`, `scout-report`, `strategy`, `info-results` and `battle-speech` (the capture's battle on
Oaks (6)). Where the rails are gone — the exchanges, the opening, the results, the lines — the battle's Skip stays on the
left rail's top, as LB2 put it. The board's layout is chosen, so each state is one base version; the retreat question's
place and the opening are two. The variants of the player's turn keep LB2's name (`LB2r…`, `LB2x`, as upright's PB3r and
PB3x), the other states are `LB3…` (upright's PB4…).

The retreat question (`LB2r…`; upright's PB3r: the script's question at Retreat, Cease Retreat nearer the button,
Retreat red and last):

- **LB2r1 — in the frame, beside Retreat.** The house's callout at the click (the tablet's A2), 37.6 mm wide, its tip at
  the rail's Retreat, which is lit; the question 2.5 mm, Cease Retreat | **Retreat** in one row, 7.9 mm high, as on the
  PC and upright. It lies over our back row and part of the front row while it asks; the rails' other rows dimmed.
- **LB2r2 — over the left rail.** The question takes the rail's rows under its heading (RETREAT): its words wrapped in
  the rail's 14.5 mm (2.1 mm), then Cease Retreat (in two lines) and **Retreat** red at the thumb, 8.5 mm high each; the
  board stays whole in view, the target's rail dimmed.

**Recommendation: LB2r1** — the question at the button as on every screen of ours (docs/ui/style.md, §5), its answers in
one row as the PC's, the tablet's and upright's; LB2r2 covers nothing of the board, but its words wrap into short lines
and its answers stack in the narrow side.

- **LB2x — an exchange** (Ex (commander 1) on Rance (unit 1), both by Melee): the fighters' cards lit with their
  action's pill, the others dimmed; the rails gone. **The counters leave the picture for its blurred sides**, beside
  their army (ours left, the enemy's right, the PC's order of TROOPS and HP): the figures 5.9 mm high, where in the
  picture at its 70% they would be 2.7 mm.

The opening (`LB3o…`; upright's question 12, PB4o1 or PB4o2, is still open):

- **LB3o1 — at the scene's place**, as on the PC and the tablet, laid out again at the frame's width: the scene in the
  opening's blue and red, the plate on top (the place 2.8 mm, the limit 2.2 mm, the turns 2.1 mm), the armies' names
  5.9 mm at its sides, VS under it; the board under it as LB2's with the opening's figures, the rails not up yet.
- **LB3o2 — over the scene and the board** as LB2 stands: ours blue at the left, the enemy red at the right, the names
  8.5 mm, the plate over the scene, VS at the gauge's place; the cards fly in on the board under it as it fades.

**Recommendation: LB3o1** — in landscape the scene's band is 111 mm wide, so the PC's opening reads at the phone's type
where it stands on the PC, and the board under it stays in view; LB3o2 is larger, as upright's PB4o2.

- **LB3s — the ninja's scout report**, before the field, across the whole width as on the PC: Kanami and her report on
  top (3.1 mm), the rebels' cards under it as the enemy attack's (LA2; the front row left), Rance's answer at the foot
  (3.4 mm); one tap goes on. With no button the sides hold nothing of their own.
- **LB3c — the strategy's cut-in**: the PC's cut-in fitted to the frame as it is — every line is already 2 mm or more
  (the caption 2.3 mm, "Success" 5.6 mm, the strategist's line 2.6 mm, what it does 3.3 mm) —, the band running on
  across the black sides to the screen's edges. Barres (unit 6); the enemy's the same in red.
- **LB3r — the results plate** in the frame's middle over the dimmed scene and board (the title 5.7 mm, the damages
  6.8 mm, the line under the title 2.1 mm); a tap goes on. A level-up's page, a fall's box and our narration panel the
  same way. The board as the gallery's `info-results` has it: night fell with Dodge alone left.
- **LB3b — a fighter's line**: the speaker's portrait in the scene's blurred left side (on the PC it covers the
  picture's left edge), so every sprite stays seen — an enemy's in the right side —; our balloon over the scene's foot
  at 3.5 mm, rewrapped as ever, two lines at most. A defence's ribbon and caption the same way, the caption 2.4 mm.

The agent's own choices, for the owner to confirm: the exchange's counters and a speaker's portrait in the scene's
blurred sides; the rails' other rows dimmed while the retreat question asks; the scout report and the cut-in across the
whole width, with nothing in the black sides.

With this round every screen of ours has a landscape base version but All Data, the title and the import screen, which
wait for screen gallery entries to draw over, and the scenes (§2, open upright too).

**The owner's choice (2026-10-28): LB2r1 and LB3o1, and LB2x, LB3s, LB3c, LB3r and LB3b as drawn** — the agent's own
choices above with them (the counters and a speaker's portrait in the scene's blurred sides). They are on the gallery's
landscape tab with LB2. Next, the owner's word: the scenes (§2), upright and landscape.

## 18. Round P14: the scenes upright, PS2 changed (2026-10-28)

The owner took PS2 as the base (§2) with changes. Redrawn in place by `mock_phone.py --round P14` over the week-18
look's key waits (Ex's scene before the attack on Oaks (6); the look dir as in round P1), PS2l added:

- **PS2 — the upper speaker's window above the picture.** The picture and its portraits in the screen's middle, between
  the camera's strip and the bar (from 52.7 mm to 96.1 mm of the screen's 154.5); Ex, the upper portrait, speaks in a
  window above it, its tail down at him; no name plate — his name is on his portrait. The speech's lines (32 cells) at
  3.6 mm, the script's breaks kept; the window keeps four lines' height (23.9 mm), so the picture never moves.
- **PS2l — the lower speaker's window under the picture**: the Leazas White Knight, the lower portrait, in a window under
  it, its tail up at him; the upper window's room empty meanwhile.
- **PS2b — window 131 under its CG**, the CG where PS2's picture stands; 131's lines at 2.6 mm, its speaker on a plate
  (no portrait shows the name there).
- **PS2m — a dialogue menu**: the picture and the question's window where PS2 has them, the rows at the thumb in the
  bar's place (7.4 mm).

The agent's own choices, for the owner to confirm: a thought goes by its thinker's portrait the same way (above or
under); the narration and a speaker with no portrait in the picture go under it; Menu, Log and Skip stay in the bar.

**The owner's choice (2026-10-28): PS2, PS2l, PS2b and PS2m as drawn**, with the agent's choices above. They are on the
gallery's upright tab (its scenes need the look's frames beside the passes: docs/ui/gallery.md, §9). Next: the scenes
landscape.

## 19. Round P15: the scenes, landscape (2026-10-28)

Drawn by `mock_phone.py --round P15` over the week-18 look's key waits (Ex's scene; in the gallery's `phone-look`) and
the lacquer pass's `message-cg-bar-speech` and `scene-menu`. In landscape the frame already reads the scenes' own text at
4.2 mm (§12), so the question is whether the windows stay over the picture or leave it, as upright's PS2 makes them.

- **LS1 — the frame as on the PC, the left side as LW1's** (LM1's principle): the scene and our windows at their places
  over the picture, the script's text 4.2 mm (upright's PS2: 3.6 mm); Menu on top of the left side, Skip and Log at the
  left thumb, the right side empty; a tap anywhere goes on. (The look's window is in the read text's colour: it had
  been read.) **LS1b**: window 131 over its CG's lower quarter as on the PC, its speaker on a plate, its lines at 2.8 mm.
  **LS1m**: a `]` menu — the question's window at its place, our rows at the frame's right foot, at the thumb (as LF2's
  ladder): rows 7.0 mm, the items 2.7 mm, in the script's order; on the PC the rows stand at the selection box's place,
  3.5 mm here, too low for a finger. The drawer hides under the menu; Menu stays.
- **LS2 — upright's PS2 laid sideways**: the picture uncovered in the middle at 66% (45.8 mm high), the left portrait's
  speech (Ex) in the left column at his height, its tail at him; the right portrait's in the right column; the
  narration under the speaker's window. With the script's breaks kept, the speech's 32 cells are 2.1 mm in the column's
  31.4 mm — half LS1's.

**Recommendation: LS1** — in landscape the picture fills the frame and the text is the largest of any device; LS2 keeps
upright's rule that a window never covers the picture, at the cost of a third of the picture and half the type.

**The owner's choice (2026-10-28): LS1**, with LS1b and LS1m — on the gallery's landscape tab. So every screen of ours
has a base version in both orientations but All Data, the title and the import screen, which wait for screen gallery
entries to draw over.

## 20. Round P16: the opening, the ledger's rows and Subordinates' End upright (2026-10-28)

The owner's next step (§5, question 32): the open questions upright. Drawn by `mock_phone.py --round P16` over the same
data as rounds P3 and P7 (the week-18 save's nine units, the party sent against Oaks (6): Rance (unit 1), Haurein
(unit 12), Cordoba (unit 8), Barres (unit 6); the capture's battle on Oaks).

The opening (question 12; round P7's two redrawn with it). Landscape took LB3o1, the opening at the scene's place laid
out again at the frame's width (§17); its upright counterpart is new here:

- **PB4o1** — as on the PC at the scene's place (0.95 ×): the names 3.2 mm, the plate's place and limit 0.9 mm.
- **PB4o2** — turned upright over the scene and the board: ours blue on top, the enemy red at the foot, the plate
  between (the place 2.6 mm, the limit 2 mm), VS at the gauge's place.
- **PB4o3 — PB4o1's place laid out again**, as LB3o1: the scene in the opening's blue and red, the plate on top (the place
  2.6 mm, the limit 2.1 mm, the turns 1.9 mm), the armies' names at its sides (4.1 mm), VS between them; the board under
  it as PB3's, the deck empty; a tap goes on.

**Recommendation: PB4o3** — the owner's landscape choice at the phone's type, every line at 1.9 mm or more, the board in
view under it; PB4o2 is larger, but covers the board the cards fly onto.

The unit ledger's rows (question 13): **the ledger scrolled sideways** — the seal (or the chosen row's diamond), № and
name fixed at the left (to x 420, 23 mm), the tab's other columns scrolled under a finger (the tablet's drag, both
ways; touch.md, "The lists scrolled by a finger"); a shadow and ▶ at the view's right edge while columns lie beyond it,
a shadow at the fixed part's edge while some have passed under it, a scroll bar under the rows (and one at the right
when the rows do not fit). The tabs stay as on the PC. Drawn on PD2 (the deployment table) and PU1 (Subordinates), the
Overview tab:

- **PD2s1 — one-line rows**, the PC's: rows 8.0 mm, the names 2.6 mm, the figures 2.3 mm, the headings 1.6 mm. At rest
  Type, Rows and Troops are in view, Refill and Cost a drag away. All nine units in view.
- **PD2s1x — a late game's roster**, made up as docs/ui/unit-ledger.md's (the week-18 units and twenty more, their boot
  records' names, types and rows, the troops made up): 29 units, "some thirty" as a late game has; Overview scrolled to
  its end and down — Refill and Cost in view, 9.8 of the 29 rows, the party's seals staying with their rows.
- **PD2s2 — two-line rows**: the fixed part a name tag, № and name (2.8 mm) over the unit's type (1.9 mm); the tab's
  other columns beside it in one line, larger (2.6 mm); rows 8.5 mm. With the type in the tag, Overview scrolls only
  as far as Cost. All nine units in view.
- **PU1s1, PU1s2** — PU1 with each ledger, the chosen unit's row vermilion with its diamond; Refill a price here, not a
  button, as on the PC. All nine units in view over the orders.

**Recommendation: PD2s1** — the same row as the PC's, the tablet's and the landscape phone's (LD2, LU1), so a player
moving between devices reads one table, and every figure above the 2 mm floor; PD2s2's type is larger and the type never
scrolls away, at the cost of taller rows and a row unlike the other devices'.

Subordinates' End (question 14; PU1 as the owner changed it, Replenish and Talk on one row):

- **PU1e1 — End at the title's right**, where a phone's own screens put close (17 × 7.1 mm); the date follows the
  subtitle; the bar gone, its room given to the orders, which reach down to the gesture zone. Out of the thumb's reach,
  but the phone's Back gesture ends the screen too (a right click).
- **PU1e2 — End across the bar**, its whole width: the thumb finds it anywhere along the foot, where every screen of ours
  has its exit; nothing else moves.
- **PU1e3 — End where it is, the budget beside it**: End at the right thumb as now; the budget leaves the orders'
  heading for the bar's left, as text with no frame (not a button).

**Recommendation: PU1e2** — End stays where the thumb learned the screen's exit (Stop, Defaults, Back stand in the bar on
our other screens), and alone in the bar it takes the bar's width rather than leave half of it empty.

## 21. Round P17: the bar's cells, turning the phone and the line breaks upright (2026-10-28)

Drawn by `mock_phone.py --round P17` over the week-18 look's key waits (Ex's scene, as PS2) and PM1's map.

The bar's cells (question 5) — Menu, Log and Skip, each 9.7 × 8.4 mm — drawn on PS2 and PM1's map wait:

- **PK1 — at the bar's left** (PK1s, PK1m: as every upright mock-up has them so far), as the tablet's drawer stands at
  the screen's left edge: the same place on every device; on the map Commands alone at the right thumb. Menu at the left
  edge lies on the thumb's arc, beyond its comfortable reach (one hand).
- **PK2 — at the bar's right, under the right thumb** (PK2s, PK2m): Skip nearest the corner (held through read text),
  Log, Menu the farthest; on the map the cells beside Commands, which keeps the corner — four cells from the corner:
  Commands, Skip, Log, Menu.

**Recommendation: PK2** — upright the phone is mostly held in one hand, and the three cells then lie within the thumb's
reach on every screen; PK1 keeps the tablet's side, which on the tablet two hands reach.

Turning the phone (question 6) — a words question; **PO1o** sketches the choice on top of Option → Display (PO1's
sheet): **Turns with the phone** (the default: the game takes the phone's orientation at any moment — PS2 ↔ LS1, PB3 ↔
LB2 —, unless the phone's own rotation lock holds it), **Upright** or **Landscape** (always, however the phone is held:
reading lying down); the tab's other settings as on the PC. The rules never see it: a turn only lays the screen out
again.

**Recommendation: the choice in Option → Display**, turning with the phone by default — the phone's own lock already
covers most needs, and the two fixed choices cover a player who reads lying down with the lock off.

The line breaks (question 1): PS2 (round P14, §18) keeps the script's breaks — the speech and thought windows' 32 cells
at 3.6 mm, 131's and the narration's 49 at 2.6 mm (PS2b) — for the owner to confirm; rewrapping at one size stays the
alternative.
