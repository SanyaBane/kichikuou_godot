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
time, as `Cﾃｷﾄｼ`'s briefing, which our interface already plays unseen: `GamePresenter.CoverBriefings`); the
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
- **Built (2026-10-24):** `Original.Content/Battles/EnemyAttack` (`EnemyAttackTests`), `BattlePresenter.Attack.cs`
  (the ninjas taken again with the report, which spent one), `godot/Strategy/EnemyAttackView` (the report's cards
  drawn by `ScoutReportView.DrawCard`; since §8 our party's cards), `godot/Strategy/GameScreen.EnemyAttack.cs`. The `--scout` look on the week-18
  save (CLAUDE.md, "Commands") goes through all of it in a window: the screen, Kanami's list, the places filled
  (Kanami spent: "0 of 1 free"), Abandon City's question answered Keep, Intercept and the deployment table, with no
  key wait of the hidden lines left for the player. Not yet looked at in a run: a refusal (Gandhi, Satella, Meglass),
  Abandon City answered Abandon, Maris's comment past 5000 troops, several ninjas.
- **MsgSkip** (owner, 2026-10-26): the screen stops the message skip as `FIGT_B`'s menu does (`STOP_ON_MENU`;
  `GameScreen.ChooseAsync` calls `MessageSkip.OnMenu` before the screen answers). Built first it let the skip run on
  through the choice into the deployment and the battle, where the original interface stops it.

## 7. Back from the deployment screen (owner, 2026-10-24)

The deployment picker's Stop (503) already goes back to the announcement (`:118-119`, `@L_0003d`), not to a defeat; but
the script announces again — the map, its scroll to the province and four blinks with a sound (`:7-14`) — before our
screen comes back, which looked like leaving the defence. The owner's request: in a defence the button reads **Back** and
the player is on our attack screen at once; giving the city up stays Abandon City's.

- **Back**: `godot/Strategy/DeploymentPanels` labels the bar's third button Back in a defence (picker mode 2).
- **At once**: the presenter keeps the attack it closed at Intercept; at the map redrawn after the picker (`:44`) with
  `Tmp0621 = 503` our screen is up again (`BattlePresenter.ReturnsToAttack`) until the announcement's `:20`: meanwhile
  `GameScreen.PauseAsync` takes no time and the blink's sound is not played. The rules run as before.
- **The report kept**: the screen rebuilt at `:20` keeps the army a ninja reported (before, the places went back to
  "?" although the ninja was spent), after Back and after a refusal too. A refusal still shows its lines and the
  announcement's map as before.
- **Checked**: `EnemyAttackTests.Back_from_the_deployment_screen_shows_the_attack_again_at_once_with_its_report`; the
  `--scout` look leaves the first deployment screen by Back and dumps `enemy-attack-back` (the report kept, Kanami
  spent), back ~0.4 s after the press, the click's own frames included.

## 8. The army's cards (owner, 2026-10-24)

The owner: the army's cards — the scout report's, grey and 282×150 — sat badly on the lacquer and were too large; they
should look like our party's cards on the deployment screen, with a bracket and its arrowhead left of them. Mock-up
round E is `tools/ui/mock_enemy_cards.py` (over the `enemy-attack-back` dump): **E1** our cards as they are, the bracket
in gold; **E2** E1 in vermilion; **E3** E1 mirrored, the faces on the cards' right; **E4** E1 taller (130); and E1
before a scouting and with a place empty. **The owner chose E3**, built the same day (`EnemyAttackView.DrawArmy`):
cards of the deployment screen's size (252×104) in `Lacquer.Row`, the face on the right in gold, the place's badge,
name, troop type, "Troops" and the figure; the front row (1, 2) on the left, held by the deployment screen's bracket
mirrored, its head pointing at the "VS"; a place not scouted dashed with a "?" where the face goes and "Unknown"; an
empty place as our empty place; Commanders and Total troops under them, then whose report it is. The block is centred
in the panel. Option → Display's palette colours it with the other screens (`UiPart.Screens`, docs/ui/options.md,
"The palette", the same day). Looked at in a window (`--scout`: the
places unknown, then Kanami's report); an empty place not yet (the week-18 save's rebels come four).

## 9. A more compact screen (owner, 2026-10-24)

With the army as our cards (§8) its panel was mostly empty. The owner asked for smaller panels, the cards as they are,
the army's panel and the province's (its picture) above all. Mock-up round L is `tools/ui/mock_attack_layout.py` (whole
screens from the map dump and the scouted attack dump): **A** both panels 404 tall instead of 496, the province's
picture 300×200 instead of 428×285, the title band, the panels and the foot one block 20 apart centred on the screen;
**B** as A, the army's panel narrowed to its cards (600) and the province's widened (520), 380 tall; **C** the title
band and the foot where they were, the shorter panels centred between them. **The owner chose A**, built the same day:
`EnemyAttackView`'s rectangles hang off `TitleTop`, `PanelsTop` and `FootTop`; the ninjas' list and Abandon City's
callout follow their buttons. Looked at in a window (`--scout`: the screen, the list, the report, the question).

## 10. The buttons' sizes and colours (owner, 2026-10-26)

The owner asked for Ninja Scout as tall as Intercept, built the same day (`EnemyAttackView.ScoutButton`: 190×92 at
the foot's `FootTop + 28`, its list's tip following), and found Abandon City's neutral row box wrong for giving a city
up: red, but then Intercept wants another colour. Mock-up round P is `tools/ui/mock_attack_buttons.py`, over the
screen gallery's `enemy-attack` dump with the taller Ninja Scout, each full screen and a sheet of the foot as it is and
with Abandon City hovered: **P1** Abandon City as the danger button (Delete's look on the Save/Load screen: dark red
lacquer in vermilion, a white flag before its label instead of the bin), Intercept red as it is — the style's two reds,
the committing action and the loss; **P2** P1 with Abandon City as tall as the others, "no battle, the city is lost"
under its name; **P3** Abandon City red, Intercept gold leaf with dark ink; **P4** Abandon City red, Intercept in the
player's blue (style §2: player blue, enemy red). The agent recommends P1: the style's meanings stay as they are
(red lacquer is the likely, committing choice everywhere; P3 and P4 would make the city's loss the brightest red on
the screen), and the small button keeps Abandon City the unlikely answer. **The owner chose P1**, built the same day:
the danger button is now a shared part (`UiPalette.DrawDangerButton`, Delete on the Save/Load screen drawn with it
unchanged), `EnemyAttackView` draws Abandon City with it at its former size, a white flag before the label, lit while
hovered or while its question is open. Looked at in the screen gallery (Lacquer, Steel, touch).

## 11. Maris as her face square (owner, 2026-10-27)

The owner: the foot's Maris is the top of her 192×256 portrait (CG 6, script pixels 192×190 squeezed into 122×120,
its gold frame open at the foot) — she should be her miniature, the face square our cards show (`UnitFaces`,
Debug → Portraits; hers the built-in square, 144 from y 7), mock-ups first in case the square looks bad filling the
portrait's place. The portrait's checker also reads purple now: `DrawTextureRectRegion` filters the colour key's
magenta into it, which the squares avoid (`FacePictures.Shrink`). Mock-up round M is `tools/ui/mock_attack_advisor.py`,
over the screen gallery's `enemy-attack` dump, each full screen and a sheet of the foot's left part at 1280×800 and at
2560×1600: **M1** the square in the portrait's place, 120×120 in gold as the army's cards' faces, the name and the line
as they are; **M2** the square at the cards' face size, 88×88, the line 32 further left; **M3** a 100 square with her
name on a speaker's plate under it (message window 131's), the line given the foot's height; **M4** M2 speaking, the
line in a box whose tail points at her and the name on a plate across its top. The square is 144 of the CG's pixels, so
120 on the layout is a reduction, and enlarged only on windows past ~1.2× the layout (240 at 2560×1600, still clean).
The agent recommended M1: nothing else on the foot moves, and the face is as large as before. **The owner chose M2**
with the square raised, its top at the top of the name's capitals; built the same day (`EnemyAttackView.AdvisorFace`,
88×88 at `FootTop + 21`; the name and the line at x 156, the line up to 20 short of Intercept), the square drawn as the
army's cards draw theirs (`EnemyAttackView.DrawFace`, shared). Looked at in the screen gallery (Lacquer, Steel, touch).

## 12. The answers mirrored for a finger (owner, 2026-10-27)

The owner: on Android, Intercept and Abandon City change places. Built the same day without a mock-up: in the touch mode
(`TouchMode.On`) the row of answers (548-1226) is mirrored, each button at its size — Abandon City at 548 (196×52,
the row's middle height), Ninja Scout at 762, Intercept at 974 by the foot's right edge (`EnemyAttackView.Mirrored`); the
ninjas' list and Abandon City's callout follow their buttons as before. The PC's row is unchanged. Looked at in the
screen gallery's touch pass (the screen, the list, the question); not yet on the tablet.
