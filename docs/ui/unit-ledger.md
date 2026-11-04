# The unit ledger down the left, its columns in tabs (survey and mock-ups, 2026-10-26)

The owner's request (2026-10-26): with many units the ledger across the top of the deployment screen (`Sﾌﾞﾀｲ`,
docs/ui/deployment-table.md) and the Subordinates screen (`Sﾌﾞｶ`, docs/ui/subordinates.md) shows too few of them at
once to be comfortable. The owner's idea: the ledger in the screen's left part, showing a part of its columns at a
time, the part chosen by buttons — by default the general figures, another the troops', another the commander's. The
owner's presets:

1. No., Name, Type, Troops, Refill, Cost
2. No., Name, T.Atk, T.Def, Strikes, Mag, Off.A, Off.D, Def.A, Def.D, Dng.A, Dng.D
3. No., Name, Strat%, HP, Lvl, EXP, C.Atk, C.Def, Item

Only the drawing changes: the rows, the columns' figures, the sort and the gestures stay the model's
(`Original.Content/Strategy/DeploymentTable`, `SubordinatesScreen`); no rule moves.

## 1. What there is now

- **Both screens** (1280×800 screen pixels): the ledger's panel across the top, (16,16)-(1264,360), its paper at
  (30,66) 1220×282 — a header and about ten rows of 24 pixels; every column in one row (deployment 19, Subordinates 18),
  wider than the paper since Refill and Cost, so a horizontal scroll bar (`DeploymentTableView`, a `Tree`).
- Under it the picture panel at (16,368) 666×416 (the hovered or chosen unit's status CG, 314×196 script pixels at
  2.12×; `FacePictures.PicturePanel`) and the party panel (deployment) or the orders panel (Subordinates) at
  (690,368) 574×416.
- **How many rows:** the units at status 1 or 2, at most 50 (`Sﾌﾞﾀｲ`'s grid, `DeploymentTable.LastUnit`); the
  week-18 save has 9, a late game some thirty. Past ten rows the ledger scrolls, and the eye loses the row it compared.
- **The first column** carries the party's place (a vermilion seal; deployment), the chosen row's diamond (§9: its bar alone since 2026-10-31) and the
  talks' marks (Subordinates, docs/ui/subordinates.md §7); the party's rows are gilt, the deployed ones grey.

## 2. The proposal

- **The layout:** the ledger's panel down the left, (16,16) 692×768, its title band as now ("Deployment", the mode and
  the places offered, the date; "Subordinates", the units ready, the date); the right column 548 wide — the picture
  panel above it (548×342: the status CG at 1.75×, 82 % of the present size) and the party panel or the orders panel
  under it at its present height (416), 26 pixels narrower.
- **The rows:** 26 at once at 24 pixels (now about ten); a vertical scroll bar of paper and ink past that.
- **The tabs:** Overview (General in round A), Troops, Commander, over the paper. The first column, No. and Name in
  every tab; the full names stay the headers' tooltips.
  - **Overview** — the owner's first preset. The deployment's adds **Rows** after Type (the agent's proposal): the rows
    a unit fights from are what the original shows for a hovered unit on this screen (`Sﾌﾞﾀｲ.ADV:524-569`), and a
    melee unit cannot attack from the back row. Refill is a button here as now (owner's decision 27).
    **Item** after Cost (owner, 2026-11-04), as in Commander too; on the PC the deployment's Overview scrolls sideways
    to reach it.
  - **Troops** — the owner's second preset. Its twelve columns do not fit the paper with the headers as they are
    (Off.A … Dng.D: 684 pixels for 652); the six modifiers go under three group headers, **Offense**, **Defense**,
    **Dungeon**, each over ATK and DEF (613 pixels). Strikes and Mag are the commander's in the battle (`Unit_Attacks`,
    the level-up's "Leader Attacks +1"; `Unit_Magic`, the magic attack and defence; docs/variables.tsv) — they stay
    here as the owner placed them, and Commander could not take them: its paper would overflow too.
  - **Commander** — the owner's third preset.
- **The sort** works as now: a header's click sorts by it, again the other way. A tab's change keeps the order; when
  the sorted column is not in the tab shown, "Sorted by T.Atk ↓" stands beside the tabs (A3), until a header of the
  tab sorts again.
- **The tab is remembered**, each screen its own (the owner, 2026-10-26), kept in `settings.cfg` as the options are,
  so a screen opens on the tab the player left there. The order is kept for the launch, as now: each screen's table
  keeps the order the player left between openings, by No. at the launch (the owner, 2026-10-26).
- **Touch** (not drawn): the tabs as tall as a touch row (46 pixels), the paper shorter by the difference.

## 3. Mock-ups (round A)

`tools/ui/mock_unit_ledger.py` (its header gives the call), over the screen gallery's dumps `deployment` and
`subordinates` (docs/ui/gallery.md): the picture panel and the party / orders panel are cut from the dumps and set in
the right column (the party and orders panels squeezed to 548 wide; built, they would be laid out again); the ledger
is drawn. **The roster is made up** so that it is long: the week-18 save's nine units with the figures the dumps show,
twenty more from `SET_CHR.ADV`'s records (their boot figures, the troops and levels made up), Gengorou (unit 15) and
Patton (unit 18) deployed this week. The party is the dump's: Rance (unit 1), Haurein (unit 12) in the front row,
Cordoba (unit 8), Barres (unit 6) in the back row. The images hold the player's CGs and stay outside the repository.

- **A1 — deployment, General**, paper tabs on the ledger as the options window's (the chosen tab paper with a
  vermilion top, joined to the sheet; the others darker paper); Rance hovered, his picture on the right.
- **A2 — deployment, Troops**, sorted by T.Atk: the modifiers under their group headers.
- **A3 — deployment, Commander**, the order from A2 kept: "Sorted by T.Atk ↓" beside the tabs.
- **A4 — Subordinates, General**, Rance chosen; the orders panel under the picture.
- **A5 — the switch in lacquer** instead of paper tabs: three row boxes under the title band, the chosen one framed
  in vermilion on dark red, as the Save/Load screen's switch between the manual saves and the auto-saves.

**The agent's recommendation:** A1's paper tabs (the ledger is paper, and tabs joined to a sheet are the options
window's already; A5's buttons sit apart from the sheet they change), with A2-A4 as drawn.

## 4. Questions for the owner

1. ~~The layout (A1): the ledger down the left, the picture 82 % of its present size above the party / orders panel?~~
   Yes (A1-A4).
2. ~~Paper tabs (A1) or the lacquer switch (A5)?~~ Paper tabs.
3. ~~Rows in the deployment's General (A1)?~~ Yes.
4. ~~The modifiers under group headers in Troops (A2), so the twelve columns fit?~~ Yes.
5. ~~The order kept across tabs, with "Sorted by …" beside them (A3)?~~ Yes, to be tried (of: kept, reset to No., each
   tab its own order).
6. ~~The tab remembered once for both screens — or each screen its own, or always General on opening?~~ Remembered,
   each screen its own.
7. ~~The tabs' names: General, Troops, Commander?~~ **Overview**, Troops, Commander: "General" beside "Commander" read
   as the rank, not as "general figures".

## 5. The owner's choice (2026-10-26)

**A1-A4**: the ledger down the left with paper tabs, Rows in the deployment's first tab, the modifiers under group
headers, the order kept across tabs (A3); the first tab named **Overview**; the tab remembered, each screen its own.

**Round B** (`tools/ui/mock_unit_ledger.py`, round `b`) draws them so, over the same dumps and made-up roster:

- **B1 — deployment, Overview**, Rance hovered;
- **B2 — deployment, Troops**, sorted by T.Atk;
- **B3 — deployment, Commander**, the order from B2 kept, "Sorted by T.Atk ↓" beside the tabs;
- **B4 — Subordinates, Overview**, Rance chosen;
- **B5 — Subordinates, Commander**, sorted by Lvl: the screen's own tab, the sorted column in it carrying the arrow.

**The owner's look at round B (2026-10-26):** all of it as drawn, but "Sorted by T.Atk ↓" beside the tabs (B3) is not
noticed; it needs to stand out.

**Round C** (round `c`): three looks for it on B3, full screens, and `C-sheet` with the ledger's top of each beside B3's.

- **C1 — a paper tag** right of the tabs, framed in vermilion, "SORTED BY T.ATK ↓" in Cinzel in the headers' ink.
- **C2 — a vermilion seal**: the tag filled vermilion with a fine inner rule, its words in paper colour — the colour of
  the party's seals and of what is chosen (docs/ui/style.md, §2).
- **C3 — a lit row box** on the lacquer (gold frame, gold words) with a × after a rule: a click on it sorts by No. again.

**The agent's recommendation:** C2 — the strongest of the three without being a button's red lacquer, and not taken
for a fourth tab as C1's paper may be. C3's × (a click sorts by No. again) could go on any of them.

**The owner (2026-10-26): C2 in C3's colours**, no ×. Read two ways, round D (round `d`, `D-sheet` beside C2):
**D1** C2's seal on C3's lit lacquer, a gold frame and gold letters, a dim inner rule; **D2** C2's seal filled gold,
the letters lacquer-dark. **The owner chose D2** (2026-10-26), the gold seal (`LedgerTabsBar.DrawOrder`).

## 6. Built (2026-10-26)

- **The model:** `Original.Content/Strategy/LedgerTabs` — `LedgerTab` (Overview, Troops, Commander) and
  `LedgerTabs.Of(columns, tab)`: a tab's columns picked from the screen's (No. and Name in each, the deployment's Overview
  with Rows), and the Troops tab's groups (Offense, Defense, Dungeon over their ATK and DEF); `LedgerTabsTests`.
- **The ledger:** `godot/Strategy/DeploymentTableView` takes a `Tab`: the tree's columns rebuilt, the rows and the order
  kept (`SortShown`, `SortHeader`, `SortMark` say the order when its column is not in the tab); the groups' names in a
  strip at the header's top, drawn by a child `Overlay` (a `Tree`'s own `_Draw` lands under its header); the chosen tab
  joined to the paper (`OpenTop`). An empty filler column after the last takes the spare width, the last keeping its
  width and alignment (the owner, 2026-10-26): spread over the columns as round B drew it, the spare width set No. far
  from the first column; taken by the last column, it left Cost alone at the paper's far edge; the last column
  left-aligned (the owner's first word) glued the Troops tab's Dungeon DEF to its ATK ("−2−2"). A tab's titles are drawn by the overlay
  too (`DrawTitles`), centred between the groups' rule and the header's foot: the tree centres its own over the whole
  header, the groups' strip included, where the rule crossed them (the owner, 2026-10-26).
- **The tabs:** `godot/Strategy/LedgerTabsBar` — paper tabs as the options window's, lit while hovered, a tab shown at
  the release; the order's gold seal right of them (D2, `DrawOrder`); 34 pixels tall, 46 in the touch mode (the paper
  shorter by the difference). Each screen's tab is remembered: `LedgerTabChoice` (`[display] ledger_tab_deployment`,
  `ledger_tab_subordinates` in `settings.cfg`).
- **The layout:** `DeploymentPanels` and `SubordinatesView` — the ledger's panel (16,16) 692×768, the tabs from y 66,
  the paper (`TableArea`) at x 30, 664 wide, under the tabs; the picture (716,16) 548×342; the party or orders panel
  (716,368) 548×416.
- **The screen gallery:** `deployment-troops` (sorted by T.Atk), `deployment-commander` (that order kept, the seal) and
  `subordinates-commander` (sorted by Lvl); the other entries pin Overview (`ShowTab(…, remember: false)`).
- **Unattended play:** the touch mode's Subordinates exercise taps the Troops tab first (T.Atk is its) and the tab
  shown before at the end; `--refill` shows Overview, not remembered, when the tab shown has no Refill buttons.
- **The agent's choices**, for the owner to confirm: the headers 0.72 of the text in every tab (round B's Cinzel 11 to
  16; the Troops tab's twelve columns fit with it) and the columns' padding 3 pixels instead of 6 while tabbed; the
  Subordinates ledger's text 8.3 script pixels, the deployment's (was 9); the groups' names Cinzel 10 as in B2; in the
  touch mode the tabs 46 pixels tall, the rows as on the PC; a header's long-press tip shows over the tabs.
- **The order between openings:** each screen's table lives for the launch and keeps its order, as it did before the
  tabs. §2 said at first that it starts by No. on every opening; the owner kept the launch's order (2026-10-26).
- **Checked:** the screen gallery's Lacquer, Steel and touch passes of the twelve deployment and Subordinates entries;
  the week-18 look in a window with our interface (Subordinates' Replenish, the deployment table's presses and drops),
  with `--refill`, and in the touch mode (the tab taps, a header's tip and sort, the finger's scroll). Awaiting the
  owner's look in a window.

## 7. Taller rows for a finger (round E, 2026-10-26)

The owner (2026-10-26): on the tablet the rows of both ledgers are too small to choose with a finger. Built, a row is
24 layout pixels, 4.9 mm on the owner's tablet (0.203 mm a pixel, docs/ui/touch.md); a finger needs 7-9 mm.

**Round E** (`tools/ui/mock_unit_ledger.py`, round `e`, over the touch pass's dumps `deployment` and `subordinates`,
`tools/ui/gallery.ps1 -Tablet`): the ledger's paper drawn again as built — the game's font at 17, the headers, the
seals, Refill's chips, the 8-pixel scroll bar — with a made-up roster of 33 units, so that the rows reach the paper's
foot and the scroll bar shows; the text as built. A grey disc 9 mm across stands for a fingertip. `E-sheet` sets the
ten ledgers side by side.

| Variant | Row | On the tablet | Whole rows in view |
|---|---|---|---|
| E0 | 24 px (as built) | 4.9 mm | 25 |
| E1 | 32 px (+33 %) | 6.5 mm | 19 |
| E2 | 36 px (+50 %) | 7.3 mm | 17 |
| E3 | 42 px (+75 %) | 8.5 mm | 14 |
| E4 | 48 px (+100 %) | 9.7 mm | 12 |

- **The touch mode only:** the PC's rows stay 24 pixels (a pointer needs no more).
- **The text stays 17:** grown with the rows, the deployment's Overview (seven columns) would run past the paper.
- **The roster** leaves out Mars the Great (unit 26) and Seizel Sisters (unit 42), whose names would crowd the
  columns; by the mock-up's widths they, with Eleanor's (unit 23) "Front, Back", would push the deployment's Overview
  some 10-50 pixels past the paper as built (not checked in the game).

**The agent's recommendation:** E3 — 8.5 mm, the middle of a finger's 7-9 mm with no gap between the rows, and 14 rows
in view, scrolled by the finger's drag; E2 if fourteen rows feel too few.

**The owner chose E2** (2026-10-26), built: in the touch mode both ledgers' rows are 36 pixels
(each item's minimum height in `DeploymentTableView`; the tree adds its separation twice a row; since §8 Option's), the text,
the header and the PC's 24-pixel rows as before (the screen gallery's Lacquer pass identical to the build before).
The seals, the chosen row's diamond, the talk bubbles and the troops' figures with their bar keep the text's size,
centred in the row (`Content`); the chosen row's bar runs along the whole row and Refill's chip grows with it (a
finger's button). Checked: the screen gallery's touch pass of the twelve deployment and Subordinates entries (36-pixel
rules), and the week-18 look with `--touch --interface new --until deployment` (the header's tip and sort, the drag, a
row's tap choosing Rick (unit 7), the party built in the deployment table). Not yet on the tablet.

## 8. The row height as an option (round O, 2026-10-26)

The owner (2026-10-26): the ledgers' row height in Options, on the PC and on Android alike. The proposal: a section
**Unit tables: row height** on Option → Display, under Thousands, for both ledgers (Subordinates and deployment), kept
in `settings.cfg` as the other options — so each device keeps its own: 24 pixels by default on the PC, 36 in the touch
mode (E2, §7); Defaults sets the device's own; the tables change at once. The text stays 17 in every height (§7).

Mock-ups (`tools/ui/mock_row_height_option.py`, over the screen gallery's `options-display` of the Lacquer and touch
passes), each on the PC (24 chosen) and on the tablet (36):

- **O1 — a slider** from 24 to 48 pixels in steps of 2, as Text speed's, with a preview of two ledger rows at the
  height chosen;
- **O2 — five tiles** of 24, 30, 36, 42 and 48 pixels, each two ledger rows at its height, the chosen one framed in
  vermilion as the palette's swatches, the device's default marked.

**The agent's recommendation:** O2 — each height is seen before it is chosen, and a tile (176 pixels wide) is a
finger's target on the tablet where a slider's knob is 18 pixels (3.7 mm); five heights are enough, the slider's
steps of 2 pixels too fine to tell apart.

**The owner chose O2** (2026-10-26), built: Option → Display → **Unit tables: row height**, five tiles
(`OptionsView.RowTile`), the choice `godot/Strategy/LedgerRowsChoice` — the PC's `[display] ledger_row_height` (24 by
default) and the touch mode's `ledger_row_height_touch` (36), Defaults the mode's own; docs/ui/options.md, "The unit
tables' row height". `DeploymentTableView.Place` reads it each frame, so a table open under the window takes a change
at once; above the text's own 24 its rows are taller as in §7, at 24 they are the text's own (the PC's look unchanged).
Checked: the screen gallery's `options-display` in the Lacquer and touch passes (24 and 36 chosen, as O2 drew them), the
ledgers at the defaults (the PC's dumps identical to the build before), and with 42 saved for the PC (written into
`settings.cfg` for the run and put back): the PC's rows 42, the touch pass's still 36. Not tried by hand in a window
(a tile's click, the change under the window), nor on the tablet.

## 9. № as narrow as its figures, no diamond (owner, 2026-10-31)

Looking at the gallery's phone passes, the owner asked for № no wider than its figures (fewer than a hundred units: two
at most) and no diamond on Subordinates' chosen row, its tint and bar telling it; then the same on the PC and the
tablet. Built on every device (`DeploymentTableView.Compact`; the phone's upright places docs/ui/phone-build.md, §5,
"After the build: № as narrow as its figures"): with a tab given (our screens always give one) № is as wide as its
widest figures, its title "№" with the sort's mark — wider than two figures in Cinzel, and the tree widened the column
to its own title — drawn by `DrawTitles` ending at the column's right and reaching left past it; a chooser's first column
holds the chosen row's bar alone, and the talks' bubbles while any unit has one. Checked in the screen gallery's
Lacquer, Steel, touch and phone passes (`deployment…`, `subordinates…`).

## 10. The Name · Type cell (owner, 2026-11-04)

The deployment table held upright took the owner's mock-up A2 (`tools/ui/mock_rows_under_type.py`; docs/ui/phone-build.md,
"After the build: the rows under the type upright"): the Rows column under the troop type in the fixed part, the name
smaller, the face from the name's top to the HP bar's foot. The owner asked for the same design in the other modes; the
mock-ups `tools/ui/mock_name_cell.py` (PC, tablet, phone held landscape) were built as drawn:

- **One cell** in place of Name, Type and Rows, headed "Name · Type" (`Original.Content/Strategy/LedgerNameCell`:
  `Merge` takes the type and the rows out of a tab's columns; `For` gives A2's places scaled so the ledger's text stands
  for upright's, 46): the face (`DrawFace`), the name, the type and the rows in the quiet ink from one left edge, the
  troops' and the HP bars under them (`DeploymentTableView.DrawStackedCell`); № at the name's line (`DrawNumberCell`).
  Every tab has it, as upright.
- **The rows**: 3.6 times the text (61 px on the PC and the tablet, ~10 rows in view for ~27 and ~17; 96 held
  landscape for 74), more if Option's row height asks for more; what the cell frees in width brings Troops to Item into
  view on the PC and the tablet.
- **Refill's button** half the row, as upright (it grew with the row, 5/6 of it).
- **Subordinates** (owner, the same day): the same cell without the rows' line, upright too
  (`PhoneLedgerUpright.SubordinatesRows`: the row 132 for 150 upright, 49 px on the PC, the name 12 % smaller, the face
  from the name's top to the HP bar's foot — 102, a little smaller than its 108 before, the line under it gone). Sorting
  by the type or the rows goes with their columns.
- Checked in the screen gallery's four passes (`deployment…`); the build, the core tests, `LedgerNameCellTests` and the
  ledger and phone layouts' tests.
