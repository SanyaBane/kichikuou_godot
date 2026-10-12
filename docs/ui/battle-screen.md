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

## Steps

1. **Mock-ups** of the layout (owner's choice, as A-D for the deployment panel), with the answers
   to the questions above.
2. **The model** (`Original.Content`, engine-independent, tested): the cards from GameState (slot,
   record, face, troops, HP, order, focus, acted), the round's order (Kichikuou's rule, tested
   against the core's choice on recorded battles), the commands per target from `FigtSelMenu`
   (tested: every captured `FIGT_SEL` answer of the golden captures is offered on its target).
3. **The frame** (Godot): while a battle shows and the interface is New, our screen covers the
   stage; the presenter keeps drawing the stage (the core's events still run) but it is not shown
   (as `DeploymentCovered`); our view follows the events it needs.
4. **Input**: our `IBattleInput` answers — the target cards' commands, the acting card's, the
   own cards' orders and info; the script menus as dialogs.
5. **The scene and the exchange**: the engagement's pictures, the shots and counters, the action
   labels, the wiped-out marks.
6. **Windows**: the round header, the flavour lines, the results, the info boxes, the strategy
   screen, the scout report — as the owner decides in question 3.
7. **Checks**: the model's tests, the golden captures replayed through our input (the same
   choices give the same memory), `--auto --interface new` through the battles, the owner's look
   from a kept save (`--until battle`).
