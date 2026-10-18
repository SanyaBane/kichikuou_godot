# The Subordinates screen — our own (plan, 2026-10-18)

The owner asked (2026-10-18) for the Subordinates command (`Cﾌﾞｶ`) to be reworked in earnest: a unit
picker like our deployment screen, the troop orders on our own buttons, and no dialogue with Maris.
Like the other screens of our own interface it comes with Option → Interface → New
(`InterfaceChoice`); with Original nothing changes.

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
  Troops, Refill (Replenish's price, "—" when full), T.Atk, T.Def, Str%, HP, Lv, EXP, Atk, Def, Act, Mag,
  Item (the unit's item, the one value the status page has that the table had not). The units deployed
  this week grey, the chosen one highlighted. **The situational modifiers** (the status page's attack and
  defence on offense, on defense, in dungeons) **are not shown** for now (docs/backlog.md).
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
     and the screen with it, the troop buttons greyed and the plate up.
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
