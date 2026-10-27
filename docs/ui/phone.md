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
own pictures; the images stay outside the repository. Their variants are named **P…** (portrait), apart from
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
7. **What comes next** (round P2, after the choices here): our full screens upright — the deployment table and
   Subordinates (the ledger as a list), Harem, Treasure, Save/Load, the week report, the enemy attack, All Data, the
   options window, the title and the import screen —, then the landscape phone.

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
