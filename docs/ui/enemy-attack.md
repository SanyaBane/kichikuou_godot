# An enemy attack — our own screen

The owner's request (2026-10-24): an enemy attack on a province of ours (`FIGT_B.ADV`) still plays the original's
screen and dialogue in our interface — the province's picture, Rance's and Maris's portraits, their lines and the
selection box Intercept / Ninja Scout / Abandon City. This file surveys what it shows (a static read of
`scenario_named`, the port `Combat/Battle.Flow.Defence.cs`) and holds the first round of mock-ups.

## 1. What the original shows

`DefendProvinceAsync` runs the file's labels; every nation phase that attacks us calls it (survey of the turn's
start, docs/ui/turn-start.md §3). In order:

| Step | Lines | What the player sees | How often |
|---|---|---|---|
| Ex's first attack | `:1-4` | `Gｴｸｽ` 1 (a story scene) when Ex (commander 1) leads and `F_Ex_State = 0` | once |
| The announcement | `:5-45` | the map panned to the province; its picture (`WAO_SUB` 331, `Prov_BgCg`); window 113 "The <army, string 10> appeared around <province>."; Rance's and Maris's portraits (311, 321); window 121 Maris: "King Rance, if we don't act, <province> will be occupied." / "What should we do?" | every attack, and again after a refusal or a stopped picker |
| The menu | `:46-64` | the selection box: Intercept (1), Ninja Scout (2), Abandon City (3); a right click shows it again | every attack |
| Intercept | `:65-80` | Rance: "Truly they're fools for approaching my country." / "Kill them all, obviously."; then the deployment picker `Sﾌﾞﾀｲ` (ours already: docs/ui/deployment-table.md), Plains (501) / City (502) / Stop (503, back to the announcement) | every intercept |
| A refusal | `:84-149` | Gandhi (unit 37, against Zeth before `F_Zeth_State` 8), Satella (unit 29) or Meglass (unit 30) (not against Kayblis or the Creator): the unit's portrait, its line, "<name> refused to fight." in 113; the deployment undone (the war expenses not refunded, BFL-01), back to the announcement | when one of them was picked |
| Plains | `:164-180` | Rance: "Alright, move out!!" / "Don't let them near <province>!" | every plains battle |
| City | `:181-216` | Rance: "...Yeah, the citizens'll be harmed, but it'll be more convenient for us." / "Maris, we're fighting inside <province>. Alright?"; Maris: "Understood." | every city battle |
| Before the battle | `:217-242` | Kayblis's war: `Gｹｲﾌﾞﾘｽ` 5, 6, 7 when Satella, Meglass or Galtia (unit 41) fights Kayblis's army (story scenes; Galtia's leaves the party) | rarely |
| Ninja Scout | `:311-648` | Rance: "Yeah, this is a good time to use a ninja." (112); none free: Maris's apology and Rance's grumble (`:322`, `:331`), back to the menu; else the ninja menu (the free ninjas, None: `:384`, `:389`, back to the menu); the ninja's call and report — the report is ours already, the lines before it are answered unseen (docs/ui/battle-screen.md, "The scout report") | every scouting |
| Abandon City | `:714-829` | Leazas Castle (1) after week 20 with `F_Ex_State = 4`: `Gｴｸｽ` 5 and Ex's last stand (Ex's unit, unit 14, alone); else Rance and Maris, six lines ("King Rance..." / "Just leave it. I don't care." / "But then..." / "Shut up, ..."), the province's flag changes, "<province> was occupied by the <army>." (113); Leazas Castle: "The castle has fallen... Leazas is no more." and ending 2 | every abandon |
| The result | `:244-310`, `:830-883` | "<province> was successfully defended." or "Our army was defeated..." / "... was occupied by ..."; `死亡捕獲`; `Gｴｸｽ` 6 after Ex's stand, `Gｴﾚﾅ` 1-3, 7 after some wins in the north | every battle |

**Ours already**: the message windows (111/112/113/121/122, docs/ui/messages.md), the deployment table, the scout
report and the way to it, the battle screen. **Still the original's**: the province's picture and the two portraits,
the selection boxes (`BattleMenuKind.EnemyAttackResponse`, `BattleMenuKind.NinjaScout`, drawn by `BattleMenuScreen`).

## 2. What stays seen (owner, 2026-10-24)

- **The refusals** of Gandhi, Satella and Meglass (`:84-149`): their lines and "<name> refused to fight." in our
  message windows, as now; then back to our screen (the script announces again).
- **Ex's scenes** (`Gｴｸｽ` 1, 5, 6) as now — and, the same way, every story scene `FIGT_B` calls (`Gｹｲﾌﾞﾘｽ` 5-7,
  `Gｴﾚﾅ`).

What else is played unseen is the owner's to decide with the variant (§4). The mock-ups assume the announcement
(`:5-45`) is the screen itself; candidates for the rest: Rance's lines at Intercept, Plains and City (the same every
time, as `Cﾃｷﾄｼ`'s briefing, which our interface already plays unseen: `GamePresenter.CoverAttackBriefing`); the
ninja menu's lead-in and the "no ninja" lines (the ninjas on our screen instead); the six lines of Abandon City (the
result "<province> was occupied..." stays seen).

## 3. Mock-ups, round 1 (2026-10-24)

Drawn by `tools/ui/mock_enemy_attack.py` (its docstring has the commands) over the dumps of the rebels' attack on Ys
(5) that the week-18 save meets after Turn END (`--turn-end --scout`, a window, without `--fast`), with the save's
and the report's real figures: Ys has Defense 2 (Walls +4%), Battle Area 330, Economy 8 (800K GOLD a week); the
Leazas Rebel Army is Ex (commander 1, 240 Leazas Knights), Sakanaku (commander 6, 970), Dodge (commander 5, 486)
and Jibul (commander 7, 400), 2,096 in all; Kanami is the only ninja in service and free. The images hold the
player's CGs, so they stay outside the repository. In all three the rules run as before: our screen answers the
script's menus with their codes (1-3; then the ninja's code), and a right click is the original's cancel.

- **A — a card over the map.** The map panned to Ys as the script pans it, Ys ringed in red with its plate; a lacquer
  card (the province menu's look) points at it: the attacker's flag, "Enemy Attack", the date; the announcement in one
  line; Ys's picture and what it holds (Walls, the city's and the plains' battle limits, its revenue); the enemy's four
  places, unknown; Maris's line small with her face; the answers — Intercept (red, to the deployment table), Ninja
  Scout as the free ninjas' chips, Abandon City. **A1** before the scouting; **A2** back from the scout report (ours
  already): the card keeps what the report said, Kanami's chip spent; **A3** Abandon City asks first in a callout at
  its button (ours: the original abandons at once; Keep is the menu's cancel).
- **B — a screen of its own.** The map blurred and dimmed; a title band; Ys left (a large picture and its figures),
  "VS", the army right as the scout report's cards in their 2×2 formation (front row, back row), unknown until
  scouted; Maris's portrait and line and the answers at the foot. **B1** before the scouting; **B2** after it, the
  places showing the report's cards — the report's overlay could then be left out and the cards filled in place.
- **C — the original's scene, our menu.** The province's picture and the two portraits as now, Maris's lines in our
  windows as now; only the selection boxes become ours (`SceneMenuView`'s lacquer rows at the box's place):
  **C1** Intercept / Ninja Scout / Abandon City; **C2** the ninja menu, the free ninjas with their faces and None.
  The cheapest; every attack still takes the announcement's three clicks.

## 4. For the owner

1. A, B or C (or a mix). **B** (owner, 2026-10-24), with room for more ninjas than one (§5).
2. Which lines besides the refusals and the story scenes are played unseen (§2).
3. A and B: whether Abandon City asks first (A3), and whether the scouting shows the report's overlay and then comes
   back to the screen (A2) or fills the screen's places at once (B2).

## 5. Mock-ups, round 2: more ninjas (2026-10-24)

The owner likes B and asks for the case of four ninjas. `FIGT_B`'s ninja menu lists only the free ones of the five
(Kanami, Freya, Kaoru, Wichita, Saitou: codes 1-5, `:345-380`; a flag of 1 is spent this week, 99 not in service,
reset weekly by `COM`). The week-18 save has Kanami alone, so the case is made up: four in service, Freya spent this
week, the other three free.

- **B3 — their faces at the foot.** Every ninja in service as her portrait's face with her name under it, in the
  menu's order, between Intercept and Abandon City, "3 of 4 free" over them; a click on a free face sends her (our
  screen answers the menu with Ninja Scout, then the ninja menu with her code); a tooltip names what the click does.
  **B3b**: a spent one is greyed and struck through, her tooltip "used this week / free again next week" (the original
  leaves her out). **B3c**: back from Kanami's report, the places filled and Kanami greyed too, two still free.
- **B4 — one button and a list.** "Ninja Scout · 3 of 4 free" as B1's button; a click opens a list above it, a row
  per ninja in service (face, name, free or used this week), the spent ones dimmed; a click on a free row sends her.
  One click more than B3, the foot as in B1.
- **B5 — the most there can be.** Five faces (Saitou in service too) still fit B3's row.

Either way, with none free (all spent) nothing on the screen sends one, so the original's "no ninjas" lines
(`:322`, `:331`) are never reached; with none in service the section says so and is quiet.

## 6. The build (owner, 2026-10-24)

**The owner's choice:** B with the Ninja Scout button and its list (B4); after a scouting the army's places fill in
place (B2: no report overlay on this screen); Abandon City asks first (A3's callout); Rance's repeated lines, the
ninjas' lead-in and the six lines of Abandon City are played unseen. The refusals, the story scenes and the results stay
seen. Our interface only; the rules run as before.

- **What is unseen.** The announcement (`:20`, `:35`, `:43`) is the screen itself. Rance at Intercept (`:68`, `:73`),
  Plains (`:171`, `:174`) and City (`:196`, `:203`; Maris's "Understood.", `:214`), the ninjas' lead-in (`:314`), the
  ninja menu's None (`:384`, `:389`; our list never answers it), the way to the report and the report itself
  (`:393-648`; the places fill instead, Maris's comment past 5000 troops, `:637`, becomes her line at the foot) and the
  six lines of Abandon City (`:737-766`) are answered at once and drawn by none of our windows. The "no ninjas" lines
  (`:322`, `:331`) are never reached: with none free the button is off.
- **The model** is `Original.Content/Battles/EnemyAttack`, taken from GameState when the announcement prints (`:20`):
  the army (string 10), the province (`Prov_Cur`, its name, `Prov_BgCg`, `Prov_Defense`, `Prov_BattleArea`,
  `Prov_Economy`), the attacker's flag (`Ev_PhaseNation + 819`, as `:773` marks the map), the date (`Week`, 48 weeks a
  year from LP 3, four a month, `MAIN.ADV`'s `L_01881`), the ninjas in service (a weekly flag other than 99) and free
  (0), Maris's line as her window prints it, and the scout report once one is made (`ScoutReport`).
- **The presenter** (`BattlePresenter`, which draws all of `FIGT_B` before the field) holds it while `CoverEnemyAttack`
  (set by `GameScreen` from the interface): `EnemyAttack` from `:20` to the menu's answer Intercept or Abandon City
  (`EnemyAttackAnswered`); while it is up every key wait is answered at once and no message window of ours shows;
  `HidesLine` marks the unseen lines after it.
- **Godot**: `godot/Strategy/EnemyAttackView` (the screen, its list and its callout) and `GameScreen.EnemyAttack.cs`: the
  menu `EnemyAttackResponse` answered by the screen (1, 2, 3), the ninja menu that follows a scouting answered with the
  ninja the list chose; `--auto` in a window dumps `enemy-attack`, with `--scout` `enemy-attack-ninjas`,
  `enemy-attack-scouted` and `enemy-attack-abandon` (the question answered Keep) before it intercepts.
