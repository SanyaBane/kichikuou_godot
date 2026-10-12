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

## The table (the owner's choices)

Rows: the listed units. Columns, compact headers, each with its full name as a tooltip, in the
owner's order (2026-10-12), after a first column with the party place (header ●, "Place in the party"):

| Header | Full name | Value |
|---|---|---|
| № | Unit number | the record (Rance (unit 1), Sill (unit 2), Bound (unit 3) …) |
| Name | Name | the unit's name string (`Unit_StrBase + (n-1)·3`; scenes may rename it) |
| Troops | Troops | `Troops / MaxTroops` |
| Type | Troop type | the unit's troop-type string (`Unit_StrBase + (n-1)·3 + 1`) |
| T.Atk | Troop attack | `TroopAtk` |
| T.Def | Troop defense | `TroopDef` |
| Str% | Strategy | `Strategy` (success chance) |
| Cost | War expense | `DeployCost + Troops / 300` (×100K GOLD), as the script sums it |
| HP | Commander HP | `HP / MaxHP` |
| Lv | Level | `Level` |
| EXP | Experience | `Exp / (100 + Level·30)` (the next level) |
| Atk | Commander attack | `Atk` |
| Def | Commander defense | `Def` |
| Act | Attacks | `Attacks` |
| Mag | Magic | `Magic` |

- Sorting by any column (a header click: ascending, again descending); by № at first. A sort key of
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
   `UnitFaces.Exceptions`. Waits for the owner's edits.
8. **The party panel, variant B**, with the faces of step 7.
9. **Drag and drop** between the places, input held while the script's clicks run.
