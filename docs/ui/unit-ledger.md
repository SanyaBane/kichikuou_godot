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
- **The first column** carries the party's place (a vermilion seal; deployment), the chosen row's diamond and the
  talks' marks (Subordinates, docs/ui/subordinates.md §7); the party's rows are gilt, the deployed ones grey.

## 2. The proposal

- **The layout:** the ledger's panel down the left, (16,16) 692×768, its title band as now ("Deployment", the mode and
  the places offered, the date; "Subordinates", the units ready, the date); the right column 548 wide — the picture
  panel above it (548×342: the status CG at 1.75×, 82 % of the present size) and the party panel or the orders panel
  under it at its present height (416), 26 pixels narrower.
- **The rows:** 26 at once at 24 pixels (now about ten); a vertical scroll bar of paper and ink past that.
- **The tabs:** General, Troops, Commander, over the paper. The first column, No. and Name in every tab; the full
  names stay the headers' tooltips.
  - **General** — the owner's first preset. The deployment's adds **Rows** after Type (the agent's proposal): the rows
    a unit fights from are what the original shows for a hovered unit on this screen (`Sﾌﾞﾀｲ.ADV:524-569`), and a
    melee unit cannot attack from the back row. Refill is a button here as now (owner's decision 27).
  - **Troops** — the owner's second preset. Its twelve columns do not fit the paper with the headers as they are
    (Off.A … Dng.D: 684 pixels for 652); the six modifiers go under three group headers, **Offense**, **Defense**,
    **Dungeon**, each over ATK and DEF (613 pixels). Strikes and Mag are the commander's in the battle (`Unit_Attacks`,
    the level-up's "Leader Attacks +1"; `Unit_Magic`, the magic attack and defence; docs/variables.tsv) — they stay
    here as the owner placed them, and Commander could not take them: its paper would overflow too.
  - **Commander** — the owner's third preset.
- **The sort** works as now: a header's click sorts by it, again the other way. A tab's change keeps the order; when
  the sorted column is not in the tab shown, "Sorted by T.Atk ↓" stands beside the tabs (A3), until a header of the
  tab sorts again.
- **The tab is remembered** (the agent's proposal): one choice for both screens, kept in `settings.cfg` as the options
  are, so the screen opens on the tab the player left.
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

1. The layout (A1): the ledger down the left, the picture 82 % of its present size above the party / orders panel?
2. Paper tabs (A1) or the lacquer switch (A5)?
3. Rows in the deployment's General (A1)?
4. The modifiers under group headers in Troops (A2), so the twelve columns fit?
5. The order kept across tabs, with "Sorted by …" beside them (A3)?
6. The tab remembered once for both screens — or each screen its own, or always General on opening?
7. The tabs' names: General, Troops, Commander?

Awaiting the owner's choice.
