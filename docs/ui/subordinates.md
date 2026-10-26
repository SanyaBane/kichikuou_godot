# The Subordinates screen — our own (plan, 2026-10-18)

The owner asked (2026-10-18) for the Subordinates command (`Cﾌﾞｶ`) to be reworked in earnest: a unit
picker like our deployment screen, the troop orders on our own buttons, and no dialogue with Maris.
Like the other screens of our own interface it comes over the original layout
(`InterfaceChoice`); with `--interface original` nothing changes.

## 1. What the original does (`Cﾌﾞｶ.ADV`, `Sﾌﾞｶ.ADV`)

- **The picker `Sﾌﾞｶ`** (`Strategy/SubordinateScreen`): the units at status 1 or 2 in a three-column grid
  (names only), the hovered unit's status CG (600 + n, 304×186, its description in the CG) and a line
  of HP and troops in the frame at the top left, "End" on the bar. A left click on a unit picks it; End, a
  right click or a left click left of the list ends the command.
- **A unit at status 2** (deployed this week) or the week's actions spent (`Ui_CmdSubordAvail` 0): the
  pick shows the unit's status page, page by page (`L_0179c`), and the picker comes back.
- **Story stage 0 (the prologue):** a pick spends the command and plays the unit's talk scene at once
  (`:713-720`); there is no troop menu.
- **Later: the troop menu** (`:721-1112`). The unit's face on the left (311), Maris on the right (321, CG 6),
  her line in window 121 — "X's unit. What will you do?" with its HP and troops — then the menu: Info,
  ○/× Replenish Troops, ○/× Add New Troops (only with an offer), ○/× Talk, Do Nothing.
  - **Replenish** (`L_014d9`): a full unit is refused ("already has n troops", Rance's "What? Damn it.");
    otherwise the deficiency and its price (one per 50 men, at least 1), Do / Don't, the budget checked,
    Rance's "Replenish the troops.", Maris's "Understood.", the result in window 113.
  - **Add New Troops** (`L_012b5`): the offer (men and price by class, troop attack and a few units of
    their own, a quarter of the week added to the price; none for Mill (unit 25), Galtia (unit 41) and
    units 48-50), Do It / Add More (the men doubled, the price tripled, while the price is under 10000) /
    Don't, the budget checked, the troops and the maximum raised and the recovery by 1.
  - **Talk** (`L_014b2`, `L_01757`): Rance's thought "Hm... Maybe I'll meet with X.", then the unit's talk
    scene (`G*.ADV`; Rance (unit 1) talks to himself, `L_01cf3`).
  - Replenish and Add New Troops spend one order a week (`CmdAvail_SubTroops`), Talk another
    (`CmdAvail_SubTalk`); the command is spent when both are (`Ui_CmdSubordAvail` 0).
- **After any order** the command ends: `Cﾌﾞｶ` returns, `COMMAND` redraws the map and waits on it
  (`CommandPhase.cs:538-542`); only Do Nothing and Don't go back to the picker.
- Maris's lines are the same all game (checked 2026-10-18): only the names, figures, the offer and the
  menu's ○/× change.

## 2. The owner's choices (2026-10-18)

Three rounds of mock-ups (`tools/ui/mock_subordinates.py`, over the units of the kept save
`saves-week18`): ten layouts first (A-J), then seven from E (the table across the top), then four from
E1. The owner chose **F1**:

- **The table across the whole top**, every column without a scroll bar: №, Name, Type (the troop type),
  Troops, Refill (Replenish's price, "—" when full), Cost (the war expense of sending the unit now, as the
  deployment table has it; added by the owner, 2026-10-20 — it grows with the troops, so after a Replenish;
  with it the table is a little wider than its panel and scrolls, which the owner accepted), T.Atk, T.Def,
  the six situational modifiers (the status page's troop attack and defence on offense, on defense, in dungeons:
  Off.A, Off.D, Def.A, Def.D, Dng.A, Dng.D; put off on 2026-10-18, added by the owner on 2026-10-23, as the
  deployment table shows them — docs/ui/deployment-table.md, "The table"),
  Strat%, HP, Lvl, EXP, C.Atk, C.Def, Strikes, Mag, Item (the unit's item, the one value the status page has that the table had not). The units deployed
  this week grey, the chosen one highlighted.
- **The status CG** (600 + n) at the bottom left, whole with the gold frame it has on the original's screen,
  with no frame of ours (owner, 2026-10-18, after the first look: the CG alone, stretched over the space,
  read as cut off). The frame is the screen's background, not the CG's: the view draws `G 870, 40` and
  `G 600` (`Sﾌﾞｶ.ADV:5-13`), the CG at its own place (`:144`), and cuts the panel out as `Sｱｲﾃﾑ`'s prompt
  does — script pixels (6, 6), 314×196 — at the table's left edge, scaled to the orders' panel's height
  (333×208); the orders' panel takes the rest of the row (287 wide). Nothing under it — the table holds the details.
- **The orders' panel** at the bottom right:
  - the budget at its top right;
  - **Replenish**, a wide button with its effect and price ("+1350 troops · 2700K");
  - **"Add New Troops", a label, not a button**; under it the **steps as the buttons**: +25, +50, +100,
    +200 … (Add More's men, the price under each), a step dearer than the budget greyed;
  - **Talk**, a compact button with the unit's face, on a row of its own;
  - **End**, small, at the bottom right, on a row of its own under Talk.
- **No dialogue with Maris** and no report from her (round 1's plate was turned down); no hint that
  Replenish and Add share the week's order — the player learns it from the greyed buttons.
- **A click does the order at once** (no Do / Don't). After a troop order both troop buttons are greyed
  for the week ("used this week") and a plate beside Talk says what was done ("Replenished: +1350 → 1400",
  "Budget −2700K"), gone after a few seconds.
- **Talk plays the unit's talk scene** as it is.

## 3. How the screen answers the script

The script decides everything, as with the deployment table (deployment-table.md, "How it works"): the
screen only chooses what the original's player would have chosen, through the inputs the script reads.

- **When it shows.** While `Sﾌﾞｶ` is the open picker (`PickerKind.Subordinates`) and while one of our orders
  runs (below). The presenter leaves the picker's grid, details and bar undrawn while it covers them, as
  for `Sﾌﾞﾀｲ` (`DeploymentCovered`). The table's chosen row is the screen's own: the script's pointer stays
  on the empty 51st cell, so the script hovers nothing and draws no details.
- **An order is a short plan of inputs** (`Original.Content/Strategy/SubordinateOrders`), answered while it
  runs; the screen stays drawn over everything, the player's input is kept out, every key wait is answered
  at once (Return), and the presenter draws no message window:
  1. a click on the unit's cell (`GesturePoints.PickerCell`): the script picks it;
  2. the troop menu: the item by its line — ○Replenish Troops (`:812`), ○Add New Troops (`:817`) or
     ○Talk (`:836`);
  3. Replenish: Do (`:1026`). Add New Troops: Add More (`:909`) as many times as the step asks, then Do It
     (`:904`);
  4. the command ends and `COMMAND` waits on the map: a right click opens its menu, and the menu is
     answered with Subordinates (`COMMAND.ADV:270/276/283`, the thieves' `:199/205`), so `Cﾌﾞｶ` opens again
     and the screen with it, the troop buttons greyed and the plate up. The screen's pointer settles again at
     every key wait, pause and menu, as unattended play's does (`GameScreen.InterruptPointers`): unsettled, the
     right click was read by `WAO_MAP`'s own read instead of the map's wait, and the order never ended (the
     owner's first replenish by hand, 2026-10-18: every button grey, no way out).
- **Talk** runs steps 1-2, answers Rance's thought (one key wait, so Maris's portrait, still up from the
  menu, is not seen), and lets go: the talk scene plays with our message windows as any scene. Afterwards
  the map, as in the original (no re-opening).
- **The prologue** (stage 0): only Talk; its step 1 is the whole order (the pick plays the scene).
- **A unit at status 2, or the week's actions spent:** every order greyed with its reason ("deployed this
  week", "used this week"). The status page is never opened (a pick of such a unit would open it): the
  table shows the same.
- **Never reached through the screen:** a full unit's refusal (Replenish greyed when the troops equal the
  maximum), the budget's refusals (a dearer step greyed, Replenish greyed beyond the budget), the troop
  menu's cancel and Don't — so CMD-12 (the multiplied offer kept after a cancel) cannot happen here.
  CMD-20 still can: a unit above its maximum is replenished down to it for 100K, as in the original.
- **End** clicks the bar's End (`GesturePoints.PickerButton(3)`), a right click on the screen likewise.

The agent's own choices in this plan, kept by the owner (2026-10-18): Rance's thought before a talk is
hidden; after a talk the map comes back (after a troop order the screen re-opens); the first row chosen
is the first unit at status 1.

## 4. Steps

1. **The rules' figures in one place.** `SubordinatesCommand.Offer` and `.Refill` (the offer of
   `:757-799`, the deficiency and price of `:1005-1009`, Add More's step), used by the port and by the
   screen, so no rule lives in the UI. Tests in `Kichikuou.Original.Tests`.
2. **The model** `Original.Content/Strategy/SubordinateOrders`: the rows (the deployment table's, with the
   type, the refill and the item), the panel (the buttons, their figures, greyed and why), the plan of an
   order and its answers, the plate. Tests on hand-made states, and a script test that runs `Cﾌﾞｶ` from
   the player's files and does each order through the plan.
3. **The screen in Godot** `godot/Strategy/SubordinatesView`: the table (`DeploymentTableView` with the
   columns given), the CG, the panel, the plate; `GameScreen` feeds the plan to the script (pointer,
   menus, key waits, the map's right click).
4. **A look** from `saves-week18` with `--interface new`: the screen, a replenish, a step of Add, a talk;
   dumps for the owner.

**Built (2026-10-18), steps 1-3 and the look's replenish:** `SubordinatesCommand.Offer`/`AddMore`/`Refill`
(`SubordinatesFiguresTests`); `Original.Content/Strategy/SubordinateOrders` (`SubordinatesScreen`,
`SubordinateOrder`; `SubordinatesScreenTests`, and `SubordinatesScriptTests` running `Cﾌﾞｶ` from the player's
files for Replenish, Add at its second step and Talk); the deployment table's rows carry the troop type, the
refill and the item (`DeploymentColumn.TroopType`/`Refill`/`Item`); `godot/Strategy/SubordinatesView`
(`DeploymentTableView` as a chooser with the screen's columns, text 9) and `GameScreen.Subordinates.cs`.
Unattended play with `--interface new` replenishes through the screen and, in a window with `--shots`,
dumps it as it opens (`subordinates`) and after the order (`subordinates-done`):
`--load --auto --fast --quit --seed 2 --saves user://saves-look-sub --slots-from user://saves-week18 --until deployment --interface new --shots <dir>`
(Haurein (unit 12): +1191 → 1400 for 2300K, the command opened again, then End). Not yet looked at by the
owner; Add New Troops and Talk are covered by the tests, not by a look.

## 5. The look (owner, 2026-10-18)

The owner approved the layout but not the look: the first build (flat steel boxes of `DeploymentPalette`, thin
borders, Godot's own buttons on a grey backdrop) read as a browser game, not a game's screen. Round 4 of the mock-ups
(`tools/ui/mock_subordinates.py`, round `g`, drawn over the dumps of our screen and the map) kept the layout and moved
it to the lacquer and gold of our map panels (command-screen.md, the province menu, the command panel; the week
report): G1 a dark lacquer backdrop with a faint gold wave pattern, G2 the map blurred behind with red-lacquer title
bands, G3 G1 with the table as a paper ledger, G4 G1 after a replenish. The owner chose **G3**:

- **The backdrop:** dark lacquer under a faint gold seigaiha (wave) pattern, darkened towards the edges.
- **The table's panel:** `Lacquer.DrawPanel` with a title band — "Subordinates" in Shojumaru, the units ready this
  week in Cinzel, the date ("LP 3 · Month 5 · Week 2") at the right — and a divider over the ledger.
- **The ledger** (`DeploymentTableView` with `ledger: true`): paper with ink text, the headers in Cinzel in
  vermilion ink over a rule, the rows' rules, the type and the item in a quieter ink, the refill's price in
  vermilion, the deployed units in grey ink; under the troops a thin bar of their strength (green from three
  quarters, amber from a third, else red — a look only); the chosen row tinted with a vermilion bar and diamond in
  the first column; the last column reaches the paper's edge. Option → Palette does not touch it.
- **The picture panel** as before (the original's gold frame), with a shadow.
- **The orders' panel:** `Lacquer.DrawPanel`; its header "Orders" in Shojumaru with the budget in Cinzel ("Budget
  116,400 K GOLD"); Replenish in red lacquer framed in gold, as Turn END; Add New Troops' label in Cinzel with a rule,
  its steps, Talk (the face in a gold frame, "with <name>" or why it is greyed) and End as dark row boxes — lit with
  a bright frame when hovered, grey when they cannot be clicked; a greyed step's and Talk's reason as a tooltip; the
  plate after an order in green: its label and figure, the budget's cost in red.
- Everything is drawn by hand in the 1280×800 screen's pixels (`SubordinatesView`, its layers: the backdrop, the
  panels; the ledger a `Tree` over them), as the other lacquer views.

Built (2026-10-18) and looked at from `saves-week18` (the dumps `subordinates`, `subordinates-done`). Put off: the
deployment screen in the same look (docs/backlog.md).

## 6. A question before Add New Troops, the report in the middle (owner, 2026-10-20)

The owner asked (2026-10-20) for two changes: a click on an Add New Troops step asks first ("Are you sure you want
to …", Yes / No), and the report of a troop order — the plate beside Talk (§5) — becomes a panel in the middle of the
screen. The original asks before both troop orders: Add New Troops' offer ("Giving n new troops will cost …", the
cost and the budget; Do It / Add More / Don't, `Cﾌﾞｶ.ADV:870-916`) and Replenish's ("Replenish X's unit?", the
deficiency and the cost; Do / Don't, `:1012-1031`); it reports in window 113 ("X got n additional troops. t/m is now
their troop count.", `:946-959`; "X's … replenished by n to t.", `:1081-1091`). Our question stays the screen's own:
nothing reaches the script before Yes, so No needs no Don't.

Round 5 of the mock-ups (`tools/ui/mock_subordinates_orders.py`, over the dumps `subordinates` and
`subordinates-done` of the week-18 look in §4; Rance (unit 1) adds +50 for 34,200K, Haurein (unit 12) replenishes
+1191 for 2,300K):

- **C1 — the question alone, in the middle:** the screen dimmed, the clicked step left lit; a lacquer dialog with
  "Add New Troops", the question with the men, the unit and the price, Yes in red lacquer and No.
- **C2 — the question with the order's account:** the week report's manner — the face, the troops before and after
  on one bar, the weekly recovery's +1 (`Unit_Recovery`, which the original never shows), the cost and the budget
  after it, then "Are you sure?" Yes / No. Its panel is R1's, so Yes can turn it into the report in place.
- **C3 — a callout at the clicked step:** the Treasure screen's W2 (pickers.md): no dimming, the step marked in
  vermilion, the question and Yes / No under it.
- **R1 — the report in the middle, as the week report:** the face, "Troops Replenished", the troops before and after
  on one bar, the men, the cost, the budget left; a click closes it (the triangle).
- **R2 — a band across the middle:** a lacquer band over the whole width, as a cut-in; gone after about two seconds
  or at a click.
- **R3 — R1 after Add New Troops:** "New Troops", the troops and their maximum raised (the bar's tick at the old
  maximum), the weekly recovery +1, the cost, the budget left.

**The owner's choices (2026-10-20):** **C3** — the question in a callout at the clicked button — and **Replenish asks
too**, in the same callout under its button. The report is **R2's look as a plate in the middle**, not over the whole width: round 6 of the same script,
**R4** (after Replenish: the face, "Troops Replenished", the unit, the troops before and after with the men gained;
cost and budget right of a rule) and **R5** (after Add New Troops: "New Troops", the troops and their maximum; the
weekly recovery's +1 left out at the owner's word). The screen dimmed a little under it; it goes by itself after about two
seconds or at a click. These replace §2's "a click does the order at once" and its plate beside Talk. After the first
build (owner, 2026-10-20) the question is only "Are you sure?" for both orders: the button it points at shows the men and
the price.

**Built (2026-10-20):**

- `SubordinatesScreen.Question` is the question ("Are you sure?"); `SubordinateResult` carries the plate's figures — the unit's name, troop type and face, the
  troops and maximum before and after, the cost and the budget left — and its lines (`Title`, `Who`, `TroopsLine`,
  `Gain`, `CostLine`, `BudgetLine`); `DeploymentTable.TroopType` names a unit's troop type.
- `SubordinatesView` asks in the Treasure screen's `TreasureCallout` at the clicked button (Replenish or a step), the
  button marked (a step in vermilion, Replenish lit); Yes gives the order (`OrderPressed`, the script then answered with
  Do / Do It as before), No or a right click leaves it, and a question left open when the screen closes is a No. The
  plate (780×150 in the middle) lies on a layer over the ledger that takes the mouse while it shows: any click closes
  it, else it goes after 2.5 seconds. Since 2026-10-20 the plate is its own control, `OrderResultPlate`, which the
  deployment screen's Replenish shows too (decision 27, docs/ui/deployment-table.md). Since 2026-10-24 (owner: gone by
  itself, it could be missed) it waits for the click, with the click's triangle at its lower right corner, on both
  screens; unattended play closes it after its dump.
- Unattended play answers the question Yes (`GameScreen.AutoReplenishThroughScreen`) and in a window dumps it as
  `subordinates-question`, between `subordinates` and `subordinates-done` (the §4 command). Looked at from
  `saves-week18`: Haurein (unit 12)'s replenish, +1191 → 1400 for 2,300K. Add New Troops' question and plate R5 are
  covered by the tests (`SubordinatesScreenTests`), not by a look in a window.

## 7. The talks' marks (owner's decision 30, built 2026-10-22)

The owner's S (docs/ui/talk-marks.md §6): a speech bubble left of No. in the row of every unit that can talk (status 1)
— filled green with "!" (new), vermilion with "?" (a draw decides), an empty outline (nothing new; its tooltip says
which) — and the chosen unit's NEW or ??? pill at Talk's top right, none when read. Neither shows while the week's talk
is spent (`CmdAvail_SubTalk` 0) nor in the view-only picker (the owner, 2026-10-22): `SubordinatesScreen.TalkOpen`,
`SubordinatesScreen.TalkMarks`, `SubordinatePanel.TalkMark`. The marks are computed once each time `Sﾌﾞｶ` opens
(`Original.Content/Strategy/PickerMarks`; not at all while the talk is spent); the bubbles sit in the ledger's first
column, widened for them, after the chosen row's bar and diamond (`DeploymentTableView.TalkMarks`, a chooser only); the
pill is `Lacquer.DrawPill`. Our interface only. Looked at from `saves-week18`: every unit NEW (the save predates the read
lines).

## 8. The ledger down the left (owner's request, 2026-10-26)

The deployment screen's request applies here too: the ledger down the screen's left, a part of its columns at a time
in tabs, the picture and the orders panel stacked on the right — docs/ui/unit-ledger.md; the owner took A1-A4 and round
D's D2 for the order's seal (its §5), built (its §6).
