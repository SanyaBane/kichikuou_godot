# The deployment table — the first screen of our own interface (plan, 2026-10-12)

Owner's decision 25 ([strategy/README.md](../strategy/README.md)): our own interface comes screen by
screen, over the original layout, chosen by a setting; the first screen is `Sﾌﾞﾀｲ`'s unit grid (the
deployment picker, `PickerKind.Deployment`, `Strategy/DeploymentScreen`) replaced by a table of the
player's units. The rest of the screen — the portrait and description, the party and expense panel on
the left, the bar buttons (Attack / Plains, City / Search; Stop) — stays the original's.

**Second part (owner's request 2026-10-12): the rest of the screen.** Everything but the portrait and
its description (the unit's picture, its name and the text beside it, all in the unit's CG — the
description exists nowhere else in the player's files) is covered by our panels in the same places:
the details of the hovered unit under the portrait, the party with a remove button per member and
its totals against the gold, and the bar's three buttons labelled from the player's files. The
owner's choices: the description stays the original's; a remove button (×) per member; the gold
shown next to the war expenses; a left click on our panels does nothing but their buttons, a right
click is the script's own (the last member out), the portrait keeps the original's clicks (a left
click there takes the last member out too, CMD-23).

**Third part (owner's request 2026-10-12): the party as a formation.** The owner looked at the
second part (× and the bar's buttons work with the mouse) and asked for the party panel in the
manner of Sengoku Rance's: who stands in the front row and who in the back. The battle's rows are
the party's places — 1-2 the front row, 3-4 the back row (docs/battle/01-flow.md §0.2); a melee
unit (class 0/10) in the back row cannot attack (docs/battle/02-resolution.md §2.2, §2.5). The
owner's choices (2026-10-12), from four mock-ups:
- **Variant B**: two columns, the back row on the left, the front row on the right (the enemy to
  the right), two member cards in each, the totals against the gold below.
- **Faces**: a member's face is its dialogue face (`Unit_FaceCg`, 192×256; scenes change it at
  run time — unit 48 is empty until `Gﾊﾆｰ` makes it the Hanny King, CG 495, or `Gｵｳｼﾞ` the Kaiju
  Prince, CG 499) cut to a square: 144 pixels of its middle from y 7, below the portrait's gold
  frame (first from y 0, moved by the owner the same day; the closer 104 showed the pixels, 128 and
  160 were the other choices), shrunk to the card. A face the square misses gets
  its own: Asuka's (unit 11) sits under Pigu's hat. `Original.Content/Strategy/UnitFaces`.
- **Debug → Portraits** first, so the owner can judge every face before it is used: the units of
  the running game and every face CG in the player's files, each cut as the panel will cut it.
- **No mark** for a melee unit in the back row.
- **Drag and drop** between places: the script only moves a clicked member to the end of the party
  (CMD-16), so a drop is answered with the clicks that give the order wanted. While they run
  the player's input stays out (as for × already: while the table's gestures are queued the script
  reads only them, and presses on our panels are dropped), no new drag starts, and the panel shows
  the order wanted at once instead of the orders on the way.

## How it works

- **The script decides everything.** `DeploymentScreen` (the port of `Sﾌﾞﾀｲ.ADV`) runs unchanged and
  hit-tests the pointer as always. The table answers with the gestures it expects
  (`PointerScript`, `GesturePoints.PickerCell`/`PickerButton`/`PickerOutside`):
  - the pointer over a row is a hover over that unit's cell: the script draws its details on the left
    (`ShowDetailsAsync`), as when the original's cell is hovered;
  - a click on a row is a click on its cell: the unit joins the party, or the script refuses it
    (deployed this week, party full) with its sound and message;
  - taking a unit out: the script takes out only the last member (a click left of the list or a right
    click); a click on a member moves it to the end (CMD-16). So a click on a member's row takes it
    out with two gestures — its cell, then left of the list; a right click on the table is the
    script's own (the last member out).
- **The state shown comes from GameState**: the listed units are the script's (`Units[n].Status != 0`),
  the party is `Bt_PlayerParty1-4`.
- **The original stays whole**: with the setting on Original, nothing changes. With it on New, the
  presenter leaves the grid undrawn while the table covers it, and the pointer over the table is the
  table's, not the mouse's (else the script would hit-test its own grid under the table): over a
  row it is on that unit's cell, elsewhere on the empty 51st cell, which the script never
  highlights. While a message window is up (a refusal) the table steps aside, so the window shows
  whole and a click anywhere answers it.

## The colours (the owner's choice, 2026-10-12)

The first build was dark blue, which sat badly with the portrait's gold frame. The owner looked at
four more palettes drawn from the original screen (its gold frames, grey tiles and sepia map) — gold on
warm charcoal, gold on grey steel, ink on parchment, gold on crimson — kept all five for the player
(Option → Palette, `godot/Strategy/DeploymentPalette`, saved in `user://settings.cfg`) and made steel
the default. Every colour of the table and the panels comes from the palette; a change recolours an
open screen at once.

## The table (the owner's choices)

Rows: the listed units. Columns, compact headers, each with its full name as a tooltip, in the
owner's order (2026-10-12), after a first column with the party place (header ●, "Place in the party").
The troop type (`Unit_StrBase + (n-1)·3 + 1`) was a column after Troops until the owner moved it into
the details panel (2026-10-12, after the look at step 8):

| Header | Full name | Value |
|---|---|---|
| № | Unit number | the record (Rance (unit 1), Sill (unit 2), Bound (unit 3) …) |
| Name | Name | the unit's name string (`Unit_StrBase + (n-1)·3`; scenes may rename it) |
| Troops | Troops | `Troops / MaxTroops` |
| T.Atk | Troop attack | `TroopAtk` |
| T.Def | Troop defense | `TroopDef` |
| Off.A, Off.D, Def.A, Def.D, Dng.A, Dng.D | Troop attack / defense on offense, on defense, in dungeons | the situational modifiers `OffAtkMod`, `OffDefMod`, `DefAtkMod`, `DefDefMod`, `DgnAtkMod`, `DgnDefMod` as the battle adds them (`DeploymentTable.Modifier`, `FIGT.ADV:2450-2483`: below 10 +n, above 10 −(n−10), 10 none; "0" for both 0 and 10, which the status page prints as "+0" and nothing); "—" for a unit without troops, whose status page shows none (`Cﾌﾞｶ.ADV:1168`); "0" and "—" in a faint ink (`Lacquer.InkFaint`), so +n and −n stand out. Added by the owner, 2026-10-23, here and on the Subordinates screen |
| Strat% | Strategy | `Strategy` (success chance) |
| Cost | War expense | `DeployCost + Troops / 300` (×100K GOLD), as the script sums it |
| HP | Commander HP | `HP / MaxHP` |
| Lvl | Level | `Level` |
| EXP | Experience | `Exp / (100 + Level·30)` (the next level) |
| C.Atk | Commander attack | `Atk` |
| C.Def | Commander defense | `Def` |
| Strikes | # of Attacks (the commander's own strikes per exchange) | `Attacks` |
| Mag | Magic | `Magic` |

- Sorting by any column (a header click: the values where more is better — Troops, T.Atk, T.Def, the six
  modifiers ("—" below every value), Strat%, HP, Lvl, EXP, C.Atk, C.Def, Strikes, Mag — descending, № and the text columns and the prices Cost and Refill ascending
  (the owner's choice, 2026-10-20); Rows by rank, descending first: Front, then "Front, Back", then Back (the
  owner's choice, 2026-10-20); again the other way); by № at first. The Subordinates screen's table
  sorts the same way. A sort key of
  "a / b" is a. Units deployed this week (status 2) are grey but sort like the rest — Rance stays first
  by number after he has fought this week. Members of the party are marked with their place (1-4).
- Scrolling both ways (51 rows at most; the columns may not fit the grid's width). The owner judges
  the look after the first build.
- Later, for touch: no hover, so a first tap shows the details and a second adds the unit.

## Steps

1. ✓ (2026-10-12) **The table's model** — `Original.Content/Strategy/DeploymentTable` (engine-independent, tested):
   the rows from GameState, the columns (header, full name, text, sort key), the party place, the grey
   rows, the sort. As done: `DeploymentTable.Rows` (units 1-50 at status ≠ 0, names and troop types
   from the string table through `Hankaku`), `DeploymentRow.Text`/`Key`, `DeploymentTable.Sort` (stable,
   ties by number), `DeploymentTableTests` on a hand-made state; the names on a booted game are
   checked with step 5.
2. ✓ (2026-10-12) **The screen of our own** — in `GameScreen`: while the deployment picker is open and the setting is
   New, the table shows over the grid area; the presenter skips drawing the grid
   (`PickerView`); the pointer over the table area is the table's gestures, elsewhere the mouse's.
   As done: `GameScreen.UpdateTable` (each frame and before each pointer read) shows, places and fills
   the table, not while `GamePresenter.MessageWindowShown`; `GamePresenter.DeploymentCovered`
   leaves out the grid's names and underline; over the table `GetMouseAsync` answers from a
   `PointerScript` of the table's (`DeploymentTable.PointerAt` at rest, `DeploymentTable.Press` /
   `RightPress` for the presses, engine-independent and tested).
3. ✓ (2026-10-12) **The Godot table** — a `Tree` with the columns, tooltips, sorting on a header click, both scroll
   bars, placed over the grid area and following the window's size. As done:
   `godot/Strategy/DeploymentTableView` — a first column with the party place (members' rows
   tinted), grey text for status 2, the sorted header marked small (▴/▾ in MS Gothic, ↑/↓ in BIZ UDGothic: the
   font's narrowest pair, its room kept in every column, so a sort never widens one), Troops on the
   left like the text columns (owner, 2026-10-12), columns as wide as their widest
   text, the text 10 script pixels high in the Option → Fonts font (regular), over (322, 4)-(638, 322).
4. ✓ (2026-10-12) **The setting** — Option → Interface (Original / New) in `user://settings.cfg`
   (`[display] interface`; `InterfaceChoice`, `--interface original|new` for one launch; docs/window-menu.md).
5. ✓ (2026-10-12) **Checks** — the model's tests; a content test running `Sﾌﾞﾀｲ` with the table's gestures (a party
   built and sent, a member removed); `--auto` through the table; the owner's look at it. As done:
   `DeploymentTableScriptTests` (the booted game's names; `Sﾌﾞﾀｲ` from the player's files: a hover
   shows the details, three presses build the party, a press on a member takes it out, Attack sends
   it); `--auto --interface new` builds every party through the table's presses
   (`DeploymentTableView.Press`); masked window dumps for the owner (`--shots` without `--headless`
   saves the window while the table shows). The owner looked and approved (2026-10-12).
6. ✓ (2026-10-12) **The rest of the screen** (the second part above) — the model:
   `DeploymentTable.Details` (what the script shows for the hovered unit, `Tmp0026`/`Tmp0033`: war
   expense, troops, rows, item) and `DeploymentTable.Party` (the places `Tmp0040` offers, the troops
   and expenses as `Sﾌﾞﾀｲ.ADV:269-366` sums them, the gold), tested; `PickerView.BarLabelTexts`; the
   presenter draws only the background and the portrait under it (`GamePresenter.DeploymentCovered`).
   Godot: `godot/Strategy/DeploymentPanels` (a backdrop over the right half and the left part below
   the portrait, the details and party panels, the bar's buttons; a × is `DeploymentTable.Press` on a
   member, a button a click on the script's own, `GameScreen.UpdateDeploymentScreen` places and fills
   it with the table). `--auto --interface new` hovers each row until the script shows its details,
   then presses it, and presses Go / Stop on our bar. Found on the way: a window dump's `Compose` took
   the frame's upload from `Present`, so the window kept the screen from before the picker — fixed.
7. ✓ (2026-10-12) **Debug → Portraits** (the third part above) — the model `UnitFaces` (the square of a
   face CG, `Default` and `Exceptions`; the units 1-50 with their names and faces from GameState),
   tested (`UnitFacesTests`: the squares, the booted game's faces, unit 48 empty); Godot:
   `godot/Strategy/PortraitViewer` over the game (which waits while it is open): the units of the
   running game or every 192×256 CG in the player's files, each square at the party panel's size in a
   1280×800 window (shrunk with premultiplied alpha, so the colour key's magenta does not bleed in), a
   click shows the whole face twice as large with its square framed. `--portraits` opens it over the
   title (`--shots DIR` saves the units' list — never the list of every face, some are adult pictures).
   The owner looked (2026-10-12): some faces need their own squares, and enemies will want faces too
   (the battle, later). So the viewer grew (2026-10-12): a third list, the commanders of the running
   game (`UnitFaces.Commanders`, `Cmdr_FaceCg`: 196 listed, nine without a face — Aladdin
   (commander 21), the three Hannies (commanders 83-85), PG-X mk2 (commander 138), the four Kaiju
   (commanders 187-190); commander 100 is the record scenes and the dungeon fill before their
   battles, empty at the boot; many generic commanders share one face, e.g. CG 482 for 24 of them),
   and the owner's edits: drag the square with the mouse, size it with the wheel, the square below as
   the panel will show it, Reset; each edit saved at once in `user://face-crops.txt`
   (`FaceCrops`, `godot/Strategy/FaceCropFile`, `FaceCropEditor`), which the agent then moves into
   `UnitFaces.Exceptions`. The owner set 58 faces' squares (2026-10-12: units' and commanders', most
   closer than 144, Asuka's at (26, 45)); they are `UnitFaces.Exceptions` now. The faceless
   commanders are monsters, which never speak in a window, so the original has no face for them
   and the battle's engagement scene draws none (`FIGT.ADV:1720-1723`); the owner's choice
   (2026-10-12): their battle sprite stands in (`RecordFace.StandIn`: `Cmdr_CgAlone`, the commander
   without troops, else `Cmdr_CgFull`; 320×256, the same colour key 40), its square set in the
   viewer like a face's (144 in the middle by default; the file keeps a sprite's size with its line).
   The owner set the sprites' squares (the Hannies', PG-X mk2's, the Kaiju's; Aladdin's stays in the
   middle) and eight more faces' — the Hanny King's and the Kaiju Prince's (unit 48) among them —
   the same day; they are `UnitFaces.Exceptions` too (73 in all).
8. ✓ (2026-10-12) **The party panel, variant B**, with the faces of step 7 — the model's
   `DeploymentMember` carries the troop type and the face (`Unit_FaceCg`); `DeploymentPanels.FillParty`:
   "Back row" (places 3-4) on the left, "Front row ▶" (places 1-2) on the right, a card per place — the
   face's square (45 script pixels, 90 in a 1280×800 window), the place and name, the troop type, the
   troops, × and the war expense; "empty" where no member stands, blank where the screen does not offer
   the place (`Tmp0040`); the totals below. The faces come through `godot/Strategy/FacePictures` (the
   viewer's loader, shared), their squares are `UnitFaces`' with the owner's edits read as the screen
   opens. The owner looked (2026-10-12) and asked for three changes, made the same day: a card's
   expense reads "Cost: 900K" (under the card's lines, so they keep its width); the troop type left
   the table for the details panel (`DeploymentDetails.TroopType`: name and type, expense and
   troops, rows and item); a member's card under the pointer is hovered as its row is
   (`DeploymentPanels.HoveredUnit`: the script's pointer on the member's cell, so the script shows its
   portrait and details). `--auto` hovers a row one read longer once its details show, so a window
   dump (the next frame) has them.
9. ✓ (2026-10-12) **Drag and drop** between the places, input held while the script's clicks run.
   The model: `DeploymentTable.Dropped` (the order a drop asks for: the two members swap places; onto
   an empty place the member goes to the end, as the party has no gaps), `Reordering` (the fewest
   clicks that give it: a click moves a member to the end, CMD-16, so the longest start of the wanted
   order that already stands in that order stays and the rest is clicked in turn) and `Drop` (a click
   on each of their cells), tested over every order of up to four members against a search of all
   click series, and against `Sﾌﾞﾀｲ` run from the player's files (`DeploymentTableScriptTests`: three
   drops, each giving the order asked for). `Party` and `Rows` take the wanted order. Godot: a left
   press on a member's card moved six screen pixels is a drag (`DeploymentPanels.Drag`: the card
   dimmed, its face under the pointer, the place under it framed); a release over another place is
   `GameScreen.PartyDropped`, which queues the clicks and keeps the wanted order (`_wantedOrder`) until
   the script reads the pointer again with nothing queued — meanwhile the table and the panels show
   that order, no drag starts and presses on our screen are dropped. `--auto --interface new` drops
   place 1 onto place 2 and back once the party is built, and fails unless the party comes out as it was.

## The lacquer look (survey, 2026-10-19)

Put off when our Subordinates screen took mock-up G3 (docs/backlog.md, "Interface"; subordinates.md, §5): the
deployment screen in the same lacquer and gold, its table a paper ledger. The layout, the gestures and the
script's answers stay as above; only the drawing changes. No rule moves.

**What it draws now** (our interface; screen pixels of a 1280×800 window):

- **The portrait and its description** (0,0)-(640,404): the script's own drawing (the unit's CG in the original's
  gold frame, its text in the CG). Stays.
- **The table** (`DeploymentTableView`, a `Tree`) over (644,8)-(1276,644): 15 columns (place, №, Name, Troops …
  Mag) at text 10, a horizontal scroll bar (they reach HP at this width), the party's rows tinted with their place
  in the first column, the deployed ones grey, the sorted header marked. Coloured from `DeploymentPalette`; its
  `ledger: true` (paper, ink, Cinzel headers, the troops' bars, a chooser's vermilion mark) is the Subordinates
  screen's and already built — the ledger has no mark for the party's places yet.
- **The panels** (`DeploymentPanels`, Godot controls): a backdrop over the right half and the left part below
  the portrait (`ColorRect`s), the details panel (12,408) 628×92 (a `RichTextLabel`: name, type, expense,
  troops, rows, item), the party panel (12,508) 628×284 ("Back row" / "Front row ▶" labels, a `PanelContainer`
  card per offered place — face 90×90, place and name, type, troops, "Cost: …K", a flat × — "n empty" for an
  empty place, the totals line), the bar (644,652) 632×140 (three Godot `Button`s labelled from the player's
  files, "---" disabled).
- **The drag**: the dragged card at 40 % alpha, its face (90×90, 85 %) under the pointer, the place under it
  framed in the palette's accent.
- **The palette** (Option → Palette: navy, gold, steel — the default —, parchment, crimson) colours all of it,
  `[display] palette` in `user://settings.cfg`, `--palette` for a launch. It also colours our battle screen and the
  map's name plates (`DeploymentPalette`); the Subordinates screen's ledger ignores it. (Since 2026-10-24 one palette, Lacquer or Steel,
  colours all our screens and the map's plates keep steel's colours, docs/ui/options.md, "The palette".)

**What changes:** the backdrop as the Subordinates screen's (dark lacquer, a faint gold seigaiha, a vignette);
the table in a lacquer panel with a title band ("Deployment", the mode and the places offered, the date) over
the ledger; the details and the party as lacquer panels drawn by hand (`Lacquer.DrawPanel`, Cinzel labels,
Shojumaru titles, figures in the game's font); the cards, the drop's highlight and the bar's buttons in one
of the variants below. Everything is drawn in Godot; `DeploymentTable`'s model stays as it is.

### Mock-ups (2026-10-19)

`tools/ui/mock_deployment.py` (its header gives the dump command and the call), drawn over a window dump of the
kept save `saves-week18` (`--until deployment --interface new`, the picker with Cordoba (unit 8) hovered); the
party Rance (unit 1), Haurein (unit 12) in the front row, Cordoba, Barres (unit 6) in the back row. The images
hold the player's CGs and stay outside the repository.

- **D1 — lacquer cards, three buttons.** The ledger (the party's rows gilt, their place a vermilion seal in the
  first column); a card a dark row box, the face on dark lacquer in a gold frame, the place on a gold diamond,
  the troops with their bar, the cost, × a small red box; the bar as now: Attack red lacquer (as Turn END),
  "---" a greyed plate, Stop a dark row box.
- **D2 — paper cards, Attack wide.** The cards as muster slips (paper, ink, the place a vermilion seal), an
  empty place dashed; Attack wide with the war expenses on it, Stop small, no plate where the bar has none.
- **D3 — a drag, the gold highlight** (on D1): the dragged card dimmed, the face (64) under the pointer, the
  place under it glowing gold, framed twice, a "⇄ Swap" tag on its top edge.
- **D4 — a drag, the vermilion highlight** (on D2): the place tinted and framed in seal red, a red tag.
- **D5 — the Subordinates screen's layout**, for comparison: the ledger across the top with every column (Type,
  Rows and Item too) and no scroll, the portrait panel below left as that screen cuts it, the party, its totals
  and the bar right, no details panel.

**The owner's look (2026-10-19): D5's layout** — the ledger across the top, the portrait below left, as the
Subordinates screen — and more mock-ups in it. A defence's bar has two buttons that send the party, Plains and City
(`Sﾌﾞﾀｲ.ADV:45-63`), where an attack has Attack and "---" (`:14-33`) and a dungeon search Search and "---" (`:75-93`);
Stop is the third in every mode.

**Round E** (the same script, round `e`): the ledger and the portrait as D5; the party panel at the bottom right
(574×416) varied.

- **E1 — 2×2 cards, an attack.** D5 tidied: lacquer cards two by two (back row left, front row right), the totals,
  Attack wide with the war expenses, Stop small.
- **E2 — 2×2 cards, a defence.** Plains and City side by side in red lacquer, Stop small.
- **E3a / E3b — four standing cards in a line**, an attack and a defence: back row 3, 4 | front row 1, 2 towards the
  enemy; each card a large face with the place's mark and × on it, the name, the type, the troops and the cost.
- **E4 — the bar a column** at the panel's right (Plains, City, Stop); the cards 2×2, narrower (a smaller face with
  the place's mark on it); a defence of three places, the fourth not offered (`Tmp0040`) a dark slot.
- **E5 — E3 with paper cards** (muster slips, vermilion seals) and a drag: Barres (unit 6) from place 4 onto place 1,
  the gold highlight.
- **E6 — the formation as a field**: the rows on a dark ground, the enemy's side a vermilion band with chevrons at
  the right, compact cards; the totals, Plains, City and Stop below.

**The owner's choice (2026-10-19): E1/E2** — the 2×2 lacquer cards, the gold highlight of D3, the bar as E1 in an
attack or a search (the sending button wide with the war expenses, Stop small) and as E2 in a defence (Plains and City
side by side). Option → Palette leaves the deployment screen; it stays for our battle screen and the map's name plates,
which it colours too (the survey above missed them).

### Built (2026-10-19)

- **The model:** `DeploymentTable.Columns` are E's (№, Name, Type, Troops, Cost, Rows, T.Atk, T.Def, Strat%, HP, Lvl, EXP,
  C.Atk, C.Def, Strikes, Mag, Item; since 2026-10-23 the six modifiers after T.Def, `DeploymentTable.ModifierColumns`): the details the original shows for the hovered unit are columns, so there is no details
  panel; `DeploymentColumn.Rows` (`DeploymentTable.RowsOf`, the class's rows as `Details` names them);
  `DeploymentMember.MaxTroops` for the card's strength bar.
- **The table** (`DeploymentTableView`) is always a ledger now (the palette's look and its `ledger` switch are gone): the
  party's rows gilt (`Lacquer.PartyRow`), their place a vermilion seal in the first column (`Lacquer.DrawSeal`), text
  8.3 script pixels (every column fits a 1280×800 window without a scroll bar).
  `GameScreen` places it in `DeploymentPanels.TableArea` (screen pixels (30, 66), 1220×282), as the Subordinates screen's.
- **The panels** (`DeploymentPanels`) are drawn by hand over the whole screen, as `SubordinatesView`: the wave backdrop
  (`Lacquer.DrawWaveBackdrop`, shared), the table's panel with its title band ("Deployment", the mode and the places
  offered, the date), the picture panel at (16, 368) 666×416 — the hovered unit's status CG in the screen's frame, cut as
  the Subordinates screen cuts it (`FacePictures.PicturePanel`, shared; the empty frame while no unit is hovered, as the
  script clears its details) — and the party's panel at (690, 368) 574×416: "Party" and the budget, "Back row" /
  "Front row ▶", the four cards (face in gold, the place on a gold diamond, `Lacquer.DrawBadge`, name, type, troops with
  their bar, cost, ×), the totals (war expenses in red beyond the budget), the bar (`Lacquer.DrawRedButton`, shared).
  The gestures are as before: × and the bar's buttons on release, a drag from six window pixels (the card dimmed, its face
  64 under the pointer, the place under it glowing gold with "Swap", or "Move" onto an empty place), a right click the
  script's own; the whole screen is the screen's pointer, so the script's pointer stays ours over it.

### The facing mark (mock-ups, 2026-10-19)

The owner looked at the built E1/E2 and asked for a triangle right of each card's face that shows which way the unit
faces (towards the enemy, on the right, as "Front row ▶"). Round F of `tools/ui/mock_deployment.py` (round `f`) draws
six marks on E1, each moving the card's text over by its room, and a sheet of the six party panels side by side:

- **T1** a solid gold arrowhead in the gap right of the face;
- **T2** a pointed tab out of the face's gold frame, as the message windows' tails;
- **T3** the face's plate a pentagon, its right side running out to a point, framed in gold;
- **T4** two vermilion chevrons;
- **T5** a narrow gold strip along the face's right edge with a pale arrowhead;
- **T6** T1 bright gold in the front row, dim gold in the back row.

The owner (2026-10-19): not a mark per face but one for the whole party, right of the cards, showing which way all the
units face. Round U (round `u`) draws six on E1; the cards give up a strip at the panel's right edge where the mark
needs one (U2's cards are narrow enough to drop the "Cost" label and put the place's mark on the face):

- **U1** one slim triangle as high as both rows, dark gold framed in gold;
- **U2** the enemy's side: a vermilion band with three chevrons, as E6's field;
- **U3** the rule under the cards drawn in gold, ending in an arrowhead at the right (the cards keep their width);
- **U4** three small arrowheads stacked, the middle one bright;
- **U5** one medium gold triangle at the rows' middle, a dim rule above and below it;
- **U6** a narrow lacquer tab, "ENEMY" written up it, an arrowhead under the word.

**The owner's choice (2026-10-19): U5 without its rules** — one gold triangle (a dark one inside it) right of the cards
at the two rows' middle; the cards give up a strip of 30 screen pixels and a gap of 6 for it
(`DeploymentPanels.DrawFacing`). Built the same day and looked at in a window (the defence of Ys (5) from `saves-week18`).

**Redrawn (owner, 2026-10-24):** the triangle did not say what it belonged to. Mock-up rounds F (F0-F7), B/T (more of the
bracket and the tab), X (more of the crossed swords) and D (those on dark lacquer) are `tools/ui/mock_facing.py`; **the
owner chose F5**, its ends kept off the cards: a gold bracket holding the front row's two cards from the right, its ends
in the gap 2 pixels short of the cards' edges, its middle drawn out into a light gold head pointing at the enemy.
The Attack button lost its line ("Go to war · cost", the war expenses being right above it) the same day.

### The cards' troops (owner, 2026-10-24)

The owner asked for the troops a line higher, as current/maximum, Cost staying at the card's foot on the right.
`tools/ui/mock_cards.py` draws four over the defence of Ys (5) from `saves-week18`: **A** the bar under the troops,
**B** the bar at the foot under Cost, **C** as A with the maximum quieter, **D** the bar beside the troops. **The owner
chose C**, built the same day (`DeploymentPanels.DrawCard`): "980/1,000" under the type, "/1,000" in `Lacquer.Quiet`, the
strength bar under it, Cost alone at the foot.

## Replenish on the deployment screen (owner's request, 2026-10-20)

The owner asked for Replenish (`Cﾌﾞｶ`'s troop menu; not Add New Troops) on the deployment screen. What the rules say:

- Replenish is the week's **one troop order for the whole army** (`CmdAvail_SubTroops`, shared with Add New Troops; set
  again at each `COMMAND` start, `CommandPhase.cs:127`), priced one per 50 men short, at least 1 (×100K;
  `SubordinatesCommand.Refill`, CMD-20), its gold counted as military spending (CMD-14).
- `Sﾌﾞﾀｲ` opens in the player's `COMMAND` (an attack, `Cﾃｷﾄｼ`, three calls; a dungeon search, `Cﾒｲｷｭｳ`) and in a
  defence (`FIGT_B`, in the nations' turns, after the week's `COMMAND`). In `COMMAND` a Replenish there is a shortcut the
  original has a way to: Stop leaves the week's attack unspent (and the dungeon's Return gives its week back), then
  Subordinates → Replenish, then the attack again. In a defence the original never offers it.
- **The owner's choice (2026-10-20): Replenish works in a defence too, while the week's troop order is unspent** — a
  rule change on purpose, as decision 26 was: decision 27 (docs/strategy/README.md).
- It is the Subordinates screen's order applied while `Sﾌﾞﾀｲ` waits (one rule method in `SubordinatesCommand`),
  then the party panel summed again (`Tmp0016`, else Go would charge the war expenses of the troops before it: Cost
  grows with the troops).

### Mock-ups (2026-10-20)

`tools/ui/mock_deployment_refill.py` (its header gives the dump command and the call), over a window dump of the kept
save `saves-week18` with the party built — Cordoba (unit 8), Barres (unit 6) in the back row, Rance (unit 1), Haurein
(unit 12) in the front row; Barres 980/1000, Replenish 100K. The question and the report are the Subordinates screen's
(C3, a callout at the clicked place with Yes / No; R4, a plate in the middle).

- **A1 — a Refill column** after Cost, as the Subordinates screen's, each price a button (a vermilion-ruled chip,
  filled when hovered), "—" when full; any listed unit can be replenished. The table no longer fits: it scrolls, Item
  cut. **A2**: the order spent — the prices grey, no buttons, the header's tooltip "Replenish: used this week".
- **B1 — a ribbon on a member's face**: only the party's members short of men, the price across the foot of the card's
  face, red lacquer when hovered; the callout beside it. The table stays as it is. **B2**: the order spent — no
  ribbons, "Replenish used this week" by the party panel's title.
- **R — the report** (either variant): R4's plate, then the screen with the troops, the card's cost and the war
  expenses summed again.

**The owner's choice (2026-10-20): A1/A2**, with the question at the cell (C3) and the plate (R); decision 27 in
docs/strategy/README.md.

### Built (2026-10-20)

- **The rule** (`Kichikuou.Original`): `SubordinatesCommand.TroopOrderOpen`, `CanReplenish` and `ReplenishNow` (what
  `Cﾌﾞｶ`'s Replenish does, without its windows); the request `OriginalScriptContext.ReplenishRequest`, which the port
  of `Sﾌﾞﾀｲ` answers at the next press with `DeploymentScreen.ReplenishCode` — the Replenish, then the party panel
  summed again (`Tmp0016`).
- **The model** (`Original.Content`): the Refill column after Cost in `DeploymentTable.Columns`;
  `DeploymentRow.CanRefill` and `RefillNote` (why a price is no button: "used this week", "not now (Subordinates
  first)", "deployed this week", "not enough gold"); `DeploymentTable.ReplenishQuestion`; `DeploymentReplenish` (the
  request and the click at the unit's cell, the plate's `SubordinateResult` after it).
- **Godot**: `DeploymentTableView` draws a Refill cell the unit can be replenished from as a chip (vermilion-ruled,
  filled while hovered or asked about) and raises `RefillPressed` for it — the rest of the row still joins the unit;
  `GameScreen.Replenish.cs` asks in a `TreasureCallout` at the cell, sets the request on Yes and shows the plate,
  `OrderResultPlate` — the Subordinates screen's R4/R5, now one control both screens use. The table's other presses
  wait while the question or the Replenish is under way.
- **Checks**: `SubordinatesFiguresTests` (the rule), `DeploymentReplenishTests` (the shortcut against `Cﾌﾞｶ`'s
  Replenish; `Sﾌﾞﾀｲ`'s port replenishing at the request and sending the party at its new expenses; no request once
  the order is spent); `--refill` (unattended play replenishes through the table instead of Subordinates; in a window
  the dumps `deployment-refill-question` and `deployment-refill-done`). The table no longer fits a 1280×800 window: it
  scrolls, as the Subordinates screen's since its Cost column. The owner looked at it and accepted it (2026-10-20).
