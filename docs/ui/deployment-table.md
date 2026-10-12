# The deployment table — the first screen of our own interface (plan, 2026-10-12)

Owner's decision 25 ([strategy/README.md](../strategy/README.md)): our own interface comes screen by
screen, over the original layout, chosen by a setting; the first screen is `Sﾌﾞﾀｲ`'s unit grid (the
deployment picker, `PickerKind.Deployment`, `Strategy/DeploymentScreen`) replaced by a table of the
player's units. The rest of the screen — the portrait and description, the party and expense panel on
the left, the bar buttons (Attack / Plains, City / Search; Stop) — stays the original's.

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
   the table, not while `GamePresenter.MessageWindowShown`; `GamePresenter.DeploymentGridHidden`
   leaves out the grid's names and underline; over the table `GetMouseAsync` answers from a
   `PointerScript` of the table's (`DeploymentTable.PointerAt` at rest, `DeploymentTable.Press` /
   `RightPress` for the presses, engine-independent and tested).
3. ✓ (2026-10-12) **The Godot table** — a `Tree` with the columns, tooltips, sorting on a header click, both scroll
   bars, placed over the grid area and following the window's size. As done:
   `godot/Strategy/DeploymentTableView` — a first column with the party place (members' rows
   tinted), grey text for status 2, the sorted header marked ▲/▼, columns as wide as their widest
   text, the text 10 script pixels high in the Option → Fonts font (regular), over (322, 4)-(638, 322).
4. ✓ (2026-10-12) **The setting** — Option → Interface (Original / New) in `user://settings.cfg`
   (`[display] interface`; `InterfaceChoice`, `--interface original|new` for one launch; docs/window-menu.md).
5. ✓ (2026-10-12) **Checks** — the model's tests; a content test running `Sﾌﾞﾀｲ` with the table's gestures (a party
   built and sent, a member removed); `--auto` through the table; the owner's look at it. As done:
   `DeploymentTableScriptTests` (the booted game's names; `Sﾌﾞﾀｲ` from the player's files: a hover
   shows the details, three presses build the party, a press on a member takes it out, Attack sends
   it); `--auto --interface new` builds every party through the table's presses
   (`DeploymentTableView.Press`); masked window dumps for the owner (`--shots` without `--headless`
   saves the window while the table shows). The owner's look is next.
