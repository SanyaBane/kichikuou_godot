# Our own battle screen — the plan (2026-10-12)

Owner's choice (2026-10-12): after the deployment table ([deployment-table.md](deployment-table.md)),
our own screen for **the whole battle**, in the manner of Sengoku Rance's — surveyed from its code in
[../sengoku/README.md](../sengoku/README.md). Chosen by the same setting as the deployment table
(Option → Interface → New), coloured by Option → Palette. **The battle's rules stay Kichikuou's**
(docs/battle/): the core (`Original/Combat`) runs unchanged and decides everything; the screen
draws what the core shows and answers what the core asks.

## What the battle gives a screen

- **State** — `GameState`: the slots (`Bt_PlayerSlot1-4`, `Bt_EnemySlot1-4`), who has acted this
  round (`Bt_PSlot1-4State`, `Bt_ESlot1-4State`), the standing orders (`Bt_PlayerOrder1-4`: Manual,
  Auto-Atk, Auto-Def), the focus a Wait builds (`Bt_PSlot1-4Focus`), rounds left
  (`Bt_RoundsLeft`), the strategy result, the units' and commanders' troops, HP, faces and sprites
  (docs/battle/01-flow.md §0.2).
- **Events** — `BattleEvents*.cs`, each drawn today by `BattlePresenter` the way `FIGT*` draws it
  (docs/battle/screen-layout.md): the round header, the strategy screen, unit and enemy info, the
  scout report, the redraw (background and slot panels) and the acting unit's highlight, the
  engagement scene, the action labels ("vs"), the slide and the counters, the exchange animation
  (`ShotsFrameEvent`, `ExchangeResultsEvent`, `WipedOutMarksEvent`), the message windows
  (`WindowEvent`: flavour lines, results), the map views. **`Redraw` and `Highlight` have a state
  side effect in the core** (01-flow.md §9.4): the screen never skips the core's work, it only
  draws differently.
- **Input** — already semantic (`Combat/BattleInput.cs`): `SelectCommandAsync(FigtSelMenu)` lists
  every command the acting unit may give **with its valid targets** (`FigtSelMenu.Options`, checked by
  `Allows`) and is answered with a `FigtSelChoice`; `ChooseAsync(BattleMenu)` for the script menus
  (retreat confirmation, the enemy's attack: intercept / ninja scout / abandon, the ninja); the key
  waits (`IK 0`) of the windows. No pointer translation is needed, unlike the deployment table:
  our controls answer with the codes `FigtSelScreen` derives from the mouse today.

## Sengoku Rance's screen on Kichikuou's battle

| Sengoku Rance (docs/sengoku/04-screen.md) | Ours, from Kichikuou's battle |
|---|---|
| Scene on top: the actor and its target, soldiers as a formation per troop type, the battlefield | The engagement's pictures: the battlefield (`Bt_BackgroundCg`, 900-918, and its overlay), the player unit's and the commander's sprites by troops (`Unit_Cg*`, `Cmdr_Cg*`, 320×256 halves), the specials' pictures; the exchange's shots and counters drawn there |
| Six cards a side, 2 columns × 3 rows, the front column at the centre | **Four** a side, 2 × 2: places 1-2 front (at the centre), 3-4 back — as our deployment panel shows the party |
| Card: face, name, troops now/max, six action-point squares, troop-type icon, buffs, frames | Face (`UnitFaces`; the commanders' faces and the monsters' stand-ins are ready), name, troops now/max, **HP now/max** (the commander's own), troop type, standing order, focus (squares?), "acted this round", the acting frame |
| Name strip: the charge-time queue | **The round's order**: every unit acts once a round, fewest troops first, re-evaluated after each action, ties by slot, player before enemy (01-flow.md §6) — computed from the state, those who acted greyed or dropped |
| Central gauge: battle time, 24 actions | **Rounds left** (4-5, or 3; the time-out loses for the attacker, wins for a defending player) |
| 戦果 bar on top | No such thing in Kichikuou: the place could show the two sides' troops, the round header's advantages or the strategy result (owner) |
| Commands on the hovered target's card; cost on the squares, the queue arrow | The acting unit's commands for that target from `FigtSelMenu.Options` (Direct / Bow / Magic, the specials, the capsules, Fog Card); commands without a target (Defend, Wait, Auto-Atk / Auto-Def, Retreat) on the acting card or a bar; own cards: Info and the standing orders (the slot popup) |
| Nothing waits for a click but the command | The script waits for keys: the round header, the "vs" window (BSC-07), the flavour lines, the results, the info boxes. Answering a key wait changes nothing in the rules — but its key code lands in `RetVal` (ENG-04), which one flavour quirk reads (BTX-03): an automatic answer must give the key the player's click would |

## Questions for the owner

1. **Layout** at 1280×800 (the game's window): Sengoku's proportions (scene on top, name strip,
   cards below with the gauge between the sides) or another arrangement — mock-ups first (step 1).
2. **The scene on top**: the engagement's pictures as they are (the battlefield 640×256, the two
   sprites) scaled into the top area; the faces then live on the cards, not under the scene.
3. **Click waits**: which of the original's waits stay (round header, "vs", flavour lines,
   results, info boxes) and which become a timed caption or a log line.
4. **The 戦果 place**: troops of the two sides, advantages, strategy — or nothing.
5. **Focus**: as squares on the card (Sengoku's action points' place) or a number.
6. **Pre-battle**: the strategy screen and the scout report — our own panels or the original's
   pictures in the scene.
7. **Script menus** (retreat, intercept / ninja scout / abandon, the ninja): a dialog of ours.
8. **Dungeon battles** (black field, no strategy): the scene's background.

## The layout (owner, 2026-10-12, from the mock-ups)

The owner goes one thing at a time; first: **the scene and the cards on screen at once**, as
Sengoku Rance's (today the engagement scene and the field with the slot panels replace each other),
and **the cards like Sengoku's, with portraits**. The questions above wait. Mock-up A (made from
window dumps of the week-26 battle, steel palette) was approved with one addition, the speaker's
portrait (mock-up B):

- **Window 1280×800** (script pixels ×2). Top, y 0-410: **the scene** — what the original fills the
  whole screen with during an exchange (the battlefield with the sprites, the TROOPS counters, the
  shots, the marks such as ANNIHILATED, the speech bubble), its picture rows 0-255 scaled ×1.6 to
  1024×410 in the middle, the sides filled with the same picture scaled to the width, blurred and
  darkened. The two big portraits under the original's scene are not drawn there (the cards carry
  the faces) — except while someone speaks (below).
- **The speaker's whole portrait** while a flavour line or any window with a speaker is up: the
  192×256 face (`Unit_FaceCg` / `Cmdr_FaceCg`) scaled ×1.56 to 300×400 on the speaker's side of the
  scene (the player's left, the enemy's right: the bubble's tail), framed in the accent colour, the
  name below it — so it is clear who speaks, as the original's faces beside the bubble show.
- **The round's order**, y 412-446: a strip of name chips, the acted grey, the acting framed, the
  rest blue (player) or red (enemy), in Kichikuou's order (fewest troops first, re-evaluated after
  each action; ties by slot, player first; 01-flow.md §6).
- **The cards**, y ~470-785: two columns of two cards a side, 282×150; the player's back row
  (places 3-4) on the far left, its front row (1-2) next to the centre, the enemy's front row next to
  the centre, its back row on the far right; between them **the rounds gauge** (one segment per
  round of the battle, the spent ones dark, "n left" below). A card: the face (`UnitFaces` square,
  118 px; the enemy's on the card's right), place and name, troop type, troops now/max with a bar,
  HP now/max with a bar, the standing order (Manual / Auto-Atk / Auto-Def; the player's), "acted"
  (the face dimmed); the acting unit's card framed in the accent colour; an empty place "n empty".
  "Rance Army" / the enemy army's name above the two sides.
- The windows that wait for a click (round header, "vs", results, info) stay as they are, over the
  scene, for now.

### The scene between exchanges (owner, 2026-10-12)

**During the whole battle the cards are at the bottom and the armies' scene on top** — while a
command is chosen as well as during an exchange; the original's field (the map, the province's
picture, the slot panels) is not shown. How it is built:

- **The scene** is a picture of our own, 640×256 shown ×1.6: the battlefield (`Bt_BackgroundCg` and
  its overlay `+1`, which `Bt_EnemyParty1 = 1137` leaves out) with two armies. During an exchange
  (from `EngagementSceneEvent` to the next redraw) exactly the engagement's pictures (the sprites by
  troops, the specials); between exchanges the player unit and the enemy commander last in focus —
  the acting unit, the target the enemy picked, the card the pointer is on — each by the engagement's
  rule (`Cg*Full` at the troop limit, `Cg*Partial` below it, `Cg*Alone` without troops). The exchange's
  numbers are drawn in the picture: the TROOPS counters, the flying shots and the damages; what the
  original draws below row 255 moves up — the HP counters to y 224, the commanders' rows halfway
  (y' = 80 + (y − 80) / 2), "UNABLE TO FIGHT" to y 112. The slide (the picture scrolling 112 rows
  down) is not shown: the picture stands whole from the start.
- **The windows over it** (round header, "vs", the flavour lines, the results, the info boxes, the
  prompt, the popups, the script menus) are the stage's screen as the original draws it, shown only
  inside the windows' own shapes (their frame CGs, the selection boxes): the 640×400 screen fitted to
  the scene's height (×1.025, centred). The stage keeps drawing the original's screen unchanged —
  Option → Interface → Original shows it as today.
- **The input** stays the script's: a click on a card is the click on its panel (`FigtSelScreen`), a
  popup opens over the scene where the panel would be; the command menu on the target's card comes
  later (step 4, the owner first).

## Steps

1. **Mock-ups** of the layout — the first part's done (2026-10-12, "The layout" above); the
   questions above come one at a time, later.
2. **The model** (`Original.Content`, engine-independent, tested): the cards from GameState (slot,
   record, face, troops, HP, order, focus, acted), the round's order (Kichikuou's rule, tested
   against the core's choice on recorded battles), the commands per target from `FigtSelMenu`
   (tested: every captured `FIGT_SEL` answer of the golden captures is offered on its target).
   ✓ 2026-10-12 the cards and the order: `Battles/BattleBoard.cs` — `BattleBoard.Cards` / `Card`,
   `BattleBoard.Remaining` (who acts next, `L_007ca`'s rule), `RoundOrder` (the strip: acted, acting,
   waiting; a turn is the slot whose state the core's choice set; the rounds for the gauge);
   `BattleBoardTests` checks every turn of every golden capture against the core's choice. The
   commands per target wait for step 4.
   Fixed 2026-10-12 (the owner saw Melfeis missing from the strip): after the round header and its
   redraw the core resets the slots and makes the round's first choice with no event between
   (`FIGT.ADV:296-330`), so a first actor that had acted the round (or battle) before went 1 → 0 → 1
   unseen and was on the strip nowhere. `RoundOrder.FirstTurn` now names it (the slots changed since the
   header, all 0 but one 1), and until then the strip shows the whole round as the reset will leave it;
   the golden test now also requires every unit in a slot to be on the strip (92 of 110 captures failed
   it before the fix).
   ✓ 2026-10-12 the scene's picture: `Battles/BattleScene.cs` (on its own surface; the picture under
   the numbers is kept and drawn again only when the armies change); `BattleSceneTests` — in the
   captured battles the scene's rows 0-223 equal the original's screen when its engagement stands whole
   with the counters, and between exchanges the two in focus stand in the slots (every eighth capture
   in the quick run, all in the full one).
   ✓ 2026-10-12 the windows' layer: `Battles/BattleWindows.cs` — `WindowShape` (a keyed CG's non-key
   pixels, or a rectangle) and `WindowMask` (their union, and the stage's screen with alpha 0 outside
   it); `BattlePresenter.Windows` (the round header until its key, the open window's frame — 815 of
   window 131 whole —, the portraits and the centre picture of ops 311/321/331 until a full-screen copy
   covers them), `FigtSelScreen.Windows` (the prompt, CG 861, and the open popup with its 8-px frame),
   `BattleMenuScreen.Windows` (the menu's box); `GamePresenter.BattleWindows` joins them,
   `GamePresenter.Scene` / `Order` follow every battle event. **The scenes a battle calls**
   (`死亡捕獲`'s deaths, any `G`/`CC`/… on the stage or window 131) turn the split off until the field
   or an exchange is drawn again: their screen is the original's, whole. `BattleWindowsTests`: in the
   captured battles every window frame, the round header and the portraits are on the screen at every
   pixel of their shapes when they open, and the mask is empty once the field has been drawn again.
3. **The frame** (Godot): while a battle shows and the interface is New, our screen covers the
   stage; the presenter keeps drawing the stage (the core's events still run) but it is not shown
   (as `DeploymentCovered`); our view follows the events it needs.
   ✓ 2026-10-12 `godot/Battles/BattleBoardView`, shown by `GameScreen.UpdateBattleScreen` while
   `GamePresenter.BattleSceneShows` (the field or an exchange; not the pre-battle map, the strategy and
   scout screens or a scene the battle calls): the scene ×1.6 at 1024×410 centred, its sides the same
   picture blurred and darkened; the windows' layer (640×400 at 656×410, centred); the speaker's whole
   face on its side while a bubble 141/142 (the player unit, left) or 151/152 (the enemy commander,
   right) is up (`BattleBoard.Speaker`), framed in the accent colour with the name; the round's order
   strip; the cards 282×150 (faces through `FacePictures`, the acting one framed and not dimmed, the
   acted dimmed); the rounds gauge; the armies' names (`BattleBoard.Armies`). Input as before: the
   pointer over a card is the script's pointer at its panel's centre (`BattleBoard.PanelCentre`) and
   focuses the scene on it, elsewhere the point of the windows' layer under it. From a round header
   until the round's first turn the cards, as the strip, show the states the round's reset will leave
   (`RoundOrder.States`; GameState still holds the last round's).
   ✓ 2026-10-12 the strip moves (owner): `godot/Battles/OrderStrip`, on its own while the game goes on.
   When damage re-sorts the waiting units, the chip that jumps ahead (out of the longest run kept in
   order; among runs as long, the one that moved left) drops a row, slides to its place while the chips
   it passes make room, and rises into it — 1 s (`OrderStrip.Seconds`, to be configurable). A new order
   or turn while chips move makes them finish within 0.15 s, then the next move starts; under MsgSkip
   the chips stand at their places at once, and so when a new round (or battle) begins (owner: the
   units that acted go back into the order without jumps).
4. **Input**: our `IBattleInput` answers — the target cards' commands, the acting card's, the
   own cards' orders and info; the script menus as dialogs.
5. **The scene and the exchange**: the engagement's pictures, the shots and counters, the action
   labels, the wiped-out marks.
6. **Windows**: the round header, the flavour lines, the results, the info boxes, the strategy
   screen, the scout report — as the owner decides in question 3.
7. **Checks**: the model's tests, the golden captures replayed through our input (the same
   choices give the same memory), `--auto --interface new` through the battles, the owner's look
   from a kept save (`--until battle`).
