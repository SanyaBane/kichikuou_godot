# The turn's start — what the player is shown

The owner's next step (2026-10-18): our own screens for the messages the player gets between Turn END
and the next command menu. This file surveys them (a static read of the scenario, `scenario_named`; no
run but the week-18 dumps below) and holds the mock-ups of the first of them, **the week report**.

## 1. In order, from Turn END to the next menu

`MAIN.ADV` (survey-main.md §2.1) runs, in this order:

1. **The scenes after the command phase** (`:216-228`: `Gﾌｳｶ` 4, `Gｶﾄﾞﾐｭｳﾑ` 1, `Gｸﾘｰﾑ` 3) — story scenes.
2. **The nations' phases** (`:229-883`), each behind its gate: Mars the Great, AL Church, Leazas Rebel
   Army, JAPAN, Helman, Patton Army, Zeth, Kayblis Empire, Camilla Unit, Creator, La Vaswald. Each opens
   with a **banner** — the map panned to the nation's capital, its music, a window "<Nation> Phase" and
   "LP y, Month m, Week w", one click — then the nation's turn (§3).
3. **The demon war's scenes** (`:884-1035`: `Iﾏｼﾞﾝ`, `Gｼｨﾙ` 21-27, `Gﾏｼﾞｯｸ` 5, `Iﾏｼﾞﾝ2`) — story scenes.
4. **"Then some time passed."** (`:1041`) between fades, one click.
5. The next week: the autosave, then **the week report** (§2), then `COMMAND.ADV`: **the week's
   events** (`COM.ADV`, §4) and **a finished building's scene** (§4), and then the map.

## 2. The week report (`MAIN.ADV:83-212`)

All three windows are the cream box of `WAO_SUB` 113 at the screen's foot, over the map panned to Leazas
Castle (1); each waits for a click (`\L_01a0b`), the last closes (`\L_019fe`).

| Window | Text | Values | When |
|---|---|---|---|
| Banner | "Leazas Army Phase" / "LP 3, Month 5, Week 2" | `Tmp0001-0003` from `Week` (`L_01881`; 48 weeks a year from LP 3, 4 a month) | every Leazas week |
| Revenue | "Revenue & Troop Replenishment" / "Revenue: n00K GOLD" / "Replenished Troops: n" | `Tmp0022` = the sum of `Prov_Economy` over owner 1's provinces (added to `Gold`); `Tmp0024` = the troops the units 1-48 gained (`RefillUnits`, random) | every Leazas week |
| Special expenses | "Special Expenses (Donations)" / up to three lines | "M Land: -2000K GOLD" (`F_MLand_Funded = 1`), "AL Church: -6500K GOLD" (`N_ALChurch_Stage < 19`), "Galtia's Food: -4500K GOLD" (Galtia (unit 41) alive) | when any applies |

- **What the report leaves out**: the budget before or after, how many provinces pay (`Tmp0021` is counted
  and never shown), which units gained troops, the units whose item changes the refill (the Doping Drug (item 48) loses
  troops, E. Nurse (item 50) gains more) — the state holds them, the windows do not.
- **The thieves' week** (`StoryStage = 0`, `:19-82`) has only the banner, "Rance's Thieves Phase": no
  revenue, no expenses (Rance (unit 1), Sill (unit 2), Bound (unit 3) and Soul (unit 4) heal and gain 2
  troops in silence), then `Gｿｳﾙ` 4, Soul's three-window line, every week but week 9.
- **The port** (`Strategy/WeekLoop.LeazasWeekAfterAutosaveAsync`, `BannerAsync`, `ExpensesAsync`) shows
  them as plain text in `WAO_SUB`'s windows; no semantic event names them yet.
- **Week 18** (the owner's save `user://saves-week18`): budget 102700K before; Revenue 20200K GOLD,
  Replenished Troops 677; AL Church -6500K GOLD; budget 116400K after.

## 3. The nations' turns

Only five files print text of their own; `TCﾘｰｻﾞｽ`, `Tﾊﾟｯﾄﾝ` and `Tｾﾞｽ` only name their army (`MS 10`)
and attack through `FIGT_B`. Every line below is one window and one click.

- **Moves that do not touch the player.** JAPAN takes a city-state (`TJAPAN.ADV:148-202`: "The <army>
  appeared around <province>.", "<army> ＶＳ <province>", "In Battle", "<province> was occupied by the
  <army>.", over the province's picture); Helman takes a Kayblis province (`Tﾍﾙﾏﾝ.ADV:676-718`: "［province］",
  "<army> VS Monster Army", "The <army> took control of the city."); Kayblis takes a province not ours
  (`Tｹｲﾌﾞﾘｽ.ADV:1615-1646`, "…took control of it in the blink of an eye."); Kesselring's unit flies off
  (`:1287-1328`); a nation collapses (`:1721` Helman when Lang Bau (45) falls, `:1752` Zeth).
- **Losses without a battle.** Gekkou steals 50 % or 25 % of the funds (`TJAPAN.ADV:399`, `:435`), or is
  foiled (`:1956`); the Creator's earthquake ruins a province of ours (`Tﾙﾄﾞﾗ.ADV:41-82`: economy and
  defence to 0).
- **An attack on the player** (`FIGT_B.ADV`): "The <army> appeared around <province>.", Maris: "King Rance,
  if we don't act, <province> will be occupied.", "What should we do?", and the menu **Intercept / Ninja
  Scout / Abandon City**. Intercept → Rance's line → the deployment picker (`Sﾌﾞﾀｲ`, at most 4 units;
  Plains / City / Stop; Gandhi (unit 37), Satella (unit 29), Meglass (unit 30) may refuse) → the battle.
  Ninja Scout → a free ninja → the scout report (our overlay already) → the menu again. Abandon → the
  province is lost (at Leazas Castle (1) after week 20 with `F_Ex_State = 4`, Ex holds it alone, `Gｴｸｽ` 5).
- **The results.** "<province> was successfully defended." (`FIGT_B:247-267`); "Our army was defeated...",
  "<province> was occupied by the <army>." (`:830-882`); Leazas Castle (1) lost: "The castle has fallen...
  Leazas is no more." and ending 2. The battle's own lines (units fallen or wounded, commanders captured)
  come before; `死亡捕獲` after a battle plays only story scenes.
- **Camilla's unit** (`MAIN.ADV:631-815`) prints nothing of its own: its scenes and up to four attacks
  through `FIGT_B`.

## 4. The week's events (`COM.ADV`) and buildings

`COM` runs once a week before the player gets the map (151 calls, 149 scenes; survey-main.md §2.3). Every
call is a scene of portraits and speech (a location card, faces, the cream box), 15 with a full-screen CG;
none is a bare notice. Roughly: ~103 long story scenes, ~38 short ones (≤ 20 windows; ~24 report-like:
someone joins, leaves, dies or comes back, a letter, a rumour, an idol chart, Zeth's surrender; ~14
slice-of-life), 7 with a choice (`IJAPAN` 1, `Iﾏｼﾞﾝ` 2, `GAL` 2, `Gｲｿﾛｸ` 2, `Gﾊﾟﾊﾟｲｱ` 5, `Gｶﾞﾝｼﾞｰ` 5, `Gﾕｳﾚｲ` 6),
3 that show nothing (`Gｼｨﾙ` 4, `Gﾓｺﾓｺ` 3; `GAL` 5 is the AL Church's attack through `FIGT_B`). Upkeep with no
window: the Lady Caravan's walk, the kaiju counters, the ninjas freed. A finished building
(`COMMAND.ADV:18-37`, `:578-614`) is Maris's report in a scene (`Gﾏﾘｽ` 4-8, `Gｱｰﾔ` 2). These stay scenes.

## 5. The week report's mock-ups (2026-10-18)

Drawn over the week-18 map (`saves-week18`, the dumps 640×400 at 2×) in the lacquer and gold of our map
panels (`godot/Strategy/Lacquer`), with the week's real figures. The images hold the player's CGs (the
map, Leazas's flag CG 820), so they stay outside the repository. In all three the three windows are one
screen and one click (or Enter / Space / Esc); the rules run as before.

- **A — the week card.** A panel in the screen's centre, as our menus: Leazas's flag, "Leazas Army
  Phase" in Shojumaru, the date in Cinzel; then the original's two sections as ledger rows — Revenue
  +20,200K GOLD, Replenished Troops +677; Special Expenses (Donations): AL Church −6,500K GOLD. Only
  what the original's windows say.
- **B — the turn banner.** A band across the screen (Sengoku Rance's turn banner): the title and the
  date large at the left, behind a gold divider the week's figures as three columns (Revenue, Troops,
  Donations). Short and wide; the map stays visible above and below.
- **C — the ledger with the balance.** A's card with what the state holds beyond the windows (ours):
  the budget before (102,700K) and after (116,400K) and the week's net (+13,700K), laid out as an
  account: the budget, plus the revenue, minus each donation, the new budget.

## 6. The week report (owner, 2026-10-18)

**The owner's choice:** C without the week's net and without "Click to continue": an animated triangle at
the card's bottom right asks for the click; shown at the week's start, where the original's windows are;
the thieves' week the same card with the banner alone; the nations' banners put off (docs/backlog.md).

- **What it shows.** Leazas's flag (CG 820; none for the thieves), the banner's title and date as the files
  print them (`MAIN.ADV:35`/`:40`, `:98`/`:103`); TREASURY (ours): Budget "last week" (`Gold` at the banner),
  + the revenue (the label and value of `:173`), − each expense (`:194`, `:201`, `:208`: the label and amount
  the files print), the new Budget (`Gold` when the window closes) in a gold box; TROOPS: "Replenished Troops"
  and its value (`:173`). Amounts in K GOLD with thousands' commas; the sub-labels ("last week", "provinces",
  "special expense") and the captions are ours.
- **The answer is the original's.** The windows' key waits (`\L_01a0b`: two or three in a Leazas week, one in
  the thieves') are answered at once, as by a click; when the window closes (`\L_019fe`, `WAO_SUB` 0) the
  report shows and waits for the player's click or key, released before the script goes on (to `COMMAND`,
  `COM`'s events). The rules run as before; nothing of the original's window is drawn meanwhile. The message
  skip passes it, as it passes the windows.
- **Built (2026-10-18):** `Original.Content/Strategy/WeekReport` (gathered from the printed runs;
  `WeekReportTests` run `WeekLoop` to `COMMAND` with the player's files), `GamePresenter.CoverWeekReport` /
  `WeekReport` / `EndWeekReport` (the window not drawn while it is gathered), `godot/Strategy/WeekReportView`,
  `GameScreen.WeekReportAsync`. Our interface. `--auto` in a window dumps it once (`week-report`).
