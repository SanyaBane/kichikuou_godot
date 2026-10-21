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
| Card: face, name, troops now/max, six action-point squares, troop-type icon, buffs, frames | Face (`UnitFaces`; the commanders' faces and the monsters' stand-ins are ready), name, troops now/max, **HP now/max** (the commander's own), troop type, standing order, focus ("◆ n" on the portrait), "acted this round", the acting frame |
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
   results, info boxes) and which become a timed caption or a log line. The round header is answered
   (owner, 2026-10-14: "The opening" below), and so is the exchange's result (owner, 2026-10-14: a hold, the
   click only with a wiped-out mark; "The exchange's result" below); the rest waits.
   Owner, 2026-10-15 (step 6): the order messages are not shown, their click answered at once, the order's label
   on the card flashes instead ("The order messages" below); the info boxes are not needed: every card shows the
   values under its portrait ("The cards' stats" below);
   the other script windows a battle opens (111/112/121/122/113/131: narration and dialogue, e.g. in the story
   battles) stay the original's for now (docs/backlog.md, "Interface").
4. **The 戦果 place**: troops of the two sides, advantages, strategy — or nothing. Owner, 2026-10-15 (mock-ups from the
   week-18 battle): no bar — it would mean nothing in Kichikuou, where nothing judges a battle by a score; instead the
   **battle's limit** gets a plate at the scene's top (its foot until 2026-10-16) and the **advantages** show what they do, in percent ("The
   limit and the advantages" below).
5. **Focus**: as squares on the card (Sengoku's action points' place) or a number. Owner, 2026-10-15 (mock-up 5B of
   three): "◆ n" on the portrait's top-left corner ("The focus" below).
6. **Pre-battle**: the strategy screen and the scout report — our own panels or the original's
   pictures in the scene. The scout report is answered (owner, 2026-10-14: our own overlay, "The scout
   report" below); the strategy screen (`FIGT作`, docs/battle/screen-layout.md §6.2) stayed the original's at
   first (owner, 2026-10-15), then became our cut-in (owner, 2026-10-15, mock-up A of three: "The strategy's
   cut-in" below).
7. **Script menus** (retreat, intercept / ninja scout / abandon, the ninja): a dialog of ours.
8. **Dungeon battles** (black field, no strategy): the scene's background. Owner, 2026-10-15 (mock-ups from a battle
   in the Monsters' Cave (dungeon 2) on 3F): as it is — the dungeon's battlefield (`Bt_BackgroundCg` 910 / 912 / 914,
   916 for God's dungeon) like any other, the sides its blurred copy; no black sides, no dungeon plate.

## The layout (owner, 2026-10-12, from the mock-ups)

The owner goes one thing at a time; first: **the scene and the cards on screen at once**, as
Sengoku Rance's (today the engagement scene and the field with the slot panels replace each other),
and **the cards like Sengoku's, with portraits**. The questions above wait. Mock-up A (made from
window dumps of the week-26 battle, steel palette) was approved with one addition, the speaker's
portrait (mock-up B):

- **Window 1280×800** (script pixels ×2). Top, y 0-410: **the scene** — what the original fills the
  whole screen with during an exchange (the battlefield with the sprites, the TROOPS counters, the
  shots, the marks such as ANNIHILATED, the speech bubble), its picture rows 0-255 scaled ×1.6 to
  1024×410 in the middle, the sides filled with its battlefield (without the armies, since 2026-10-13)
  scaled to the width, blurred and darkened. The two big portraits under the original's scene are not drawn there (the cards carry
  the faces) — except while someone speaks (below).
- **The speaker's whole portrait** while a flavour line or any window with a speaker is up: the
  192×256 face (`Unit_FaceCg` / `Cmdr_FaceCg`) scaled ×1.56 to 300×400 on the speaker's side of the
  scene (the player's left, the enemy's right: the bubble's tail), framed in the accent colour, the
  name below it — so it is clear who speaks, as the original's faces beside the bubble show.
- **The round's order**, y 374-406 (over the scene's foot, where the limit's plate stood; y 412-446 under the scene
  until 2026-10-16, owner): a strip of name chips, the acted grey, the acting framed, the
  rest blue (player) or red (enemy), in Kichikuou's order (fewest troops first, re-evaluated after
  each action; ties by slot, player first; 01-flow.md §6).
- **The separator** (owner, 2026-10-16): a bar across the screen at the scene's foot, y 408-414, in the frames'
  colour lit from above, an accent line along its top brightest in the middle, a shadow cast on the panel
  (`BattleBoardView.DrawSeparator`); over the strip and the spotlight.
- **The cards**, y 450-758 (474-782 until 2026-10-16: the owner raised them, with the armies' names at y 424 and the gauge, to just under the separator, the free room below them): two columns of two cards a side, 282×150; the player's back row
  (places 3-4) on the far left, its front row (1-2) next to the centre, the enemy's front row next to
  the centre, its back row on the far right; between them **the rounds gauge** (one segment per
  round of the battle, the spent ones dark, "n left" below; since 2026-10-15 the owner's mock-up A of three, after
  Sengoku Rance's time gauge, docs/sengoku/04-screen.md §7: a recessed dark tube 48×238 in the frame's colour, a
  glowing pill per round left in the accent colour, lit from the bottom, the spent ones dark slots from the top; the
  round under way pulses slowly (2 s, owner), a round that goes out flashes white and fades (0.4 s; not while
  skipping); the number left below in Cinzel Bold and "TURNS LEFT", on the last round the warning colour and "LAST
  TURN" — `BattleBoardView.DrawGauge`). A card: the face (`UnitFaces` square,
  118 px — since 2026-10-15 112 at the top with the unit's stats under it, "The cards' stats"; the enemy's on the card's right), place and name, troop type, troops now/max with a bar,
  HP now/max with a bar, the standing order (Manual / Auto-Atk / Auto-Def; the player's), "acted"
  (a word in the state line; the face is not dimmed, owner's request 2026-10-13); the acting unit's card as Sengoku Rance's
  (owner, 2026-10-13; docs/sengoku/04-screen.md §6.2, §6.5): its fill the card's colour 30 % toward the
  accent, its edge black / accent / accent / black, and every 500 ms a frame shown or hidden over it one
  unit further out, accent / white / white / accent (a unit is a quarter of the card's padding); an empty place "n empty".
  "Rance Army" / the enemy army's name above the two sides.
- The windows that wait for a click ("vs", results, info) stay as they are, over the scene, for now;
  the round header became the battle's opening ("The opening" below).

### The scene between exchanges (owner, 2026-10-12)

**During the whole battle the cards are at the bottom and the armies' scene on top** — while a
command is chosen as well as during an exchange; the original's field (the map, the province's
picture, the slot panels) is not shown. How it is built:

- **The scene** is a picture of our own, 640×256 shown ×1.6: the battlefield (`Bt_BackgroundCg` and
  its overlay `+1`, which `Bt_EnemyParty1 = 1137` leaves out) with two armies. During an exchange
  (from `EngagementSceneEvent` to the next redraw) exactly the engagement's pictures (the sprites by
  troops, the specials); between exchanges (owner, 2026-10-13) an empty field at a round's start; in a
  turn its actor alone from the highlight of its panel, then the target the next highlight names (the
  enemy's pick; the player's attack) — a defence leaves the actor alone; while the player chooses
  (`FIGT_SEL`) the acting unit and, on the right, only the enemy whose card the pointer is on (gone when
  the pointer leaves it; own cards change nothing); after an exchange nothing from the field's redraw
  on — the armies part (owner, 2026-10-13; until then its two armies stayed until the next turn), so the
  next actor comes in anew even when it fought in that exchange — each by the engagement's
  rule (`Cg*Full` at the troop limit, `Cg*Partial` below it, `Cg*Alone` without troops). The exchange's
  numbers are drawn in the picture: the TROOPS counters, the flying shots and the damages; what the
  original draws below row 255 moves up — the HP counters to y 224, the commanders' rows halfway
  (y' = 80 + (y − 80) / 2), "UNABLE TO FIGHT" to y 112. The slide (the picture scrolling 112 rows
  down) is not shown: the picture stands whole from the start. **Since 2026-10-15 our screen draws none of the
  original's numbers** (owner): no shots flying across, no damages, and the TROOPS and HP counters are our own since
  2026-10-16 ("The exchange's counters" below); the wiped-out marks stay (`BattleScene.Numbers`, off in
  `BattleBoardView`; on by default, so the tests still compare the scene with the original's screen;
  `BattleSceneTests.Without_numbers_the_front_is_the_overlay_and_the_marks`). The script's waits for them are unchanged.
- **The armies slide** (owner, 2026-10-13; Sengoku Rance's, docs/sengoku/04-screen.md §2.3): an army
  that comes into the picture slides in from outside the scene on its side (the player's from the left,
  the enemy's from the right), one that leaves slides out the same way; horizontally, evenly, in
  Option → Battle Screen… → "Armies: slide" (0.2 s by default; `--army-seconds x` for a launch), at once
  while skipping and with `--fast`. A new army on a side waits for the one shown there to leave — a new
  arrival (`BattleScene.Army.Arrival`), so the same unit that parted after an exchange and acts next
  slides out and in again; the same arrival with other sprites changes in place. The player's choice never waits for a slide (the hovered
  enemy slides in and out while the pointer moves); the exchange does: its engagement picture waits until
  every army stands in place, then the numbers run. When the choice ends the hovered enemy stays in the
  picture (the attack's target), so it does not slide out and in again. The scene keeps the picture's
  layers for it (`BattleScene.FieldY`: the battlefield, each army on the key, the front — the overlay and
  the numbers); our screen stacks them (`BattleBoardView`'s `ArmySlide`), `GameScreen` waits for them at
  an `EngagementSceneEvent`.
- **The spotlight** (owner, 2026-10-13; Sengoku Rance's, docs/sengoku/04-screen.md §6.5): while an
  exchange is on (`BattleScene.InEngagement`, from its engagement picture to the field's redraw) the whole
  panel below the scene — the order strip, the armies' names, the gauge, the cards — is darkened (black at
  Option → Battle Screen… → "Spotlight: darkness", 50 % by default), but the two fighters' cards (`Unit_Cur`, `Cmdr_Cur`), so who fights shows at a glance. It fades in
  and out in 0.15 s (at once while skipping and with `--fast`); the acting card's frame stays over it.
- **Out of the exchange's way** (owner, 2026-10-16, as Sengoku Rance's): on the spotlight's signal the limit's plate
  slides up out over the screen's top and the order strip down behind the separator (clipped at its top, so it
  never shows over the panel); when the exchange is over both come back (`BattleBoardView.StepAway`, eased, 0.3 s;
  at once while skipping and with `--fast`), the strip with the order's change that came meanwhile already in place:
  no move (owner, 2026-10-21 — played once the strip was back, it came while the next turn had begun).
- **The actions on the fighters' cards** (owner, 2026-10-13, mock-up D): the "vs" window (window 143 with
  the two sides' action labels, `FIGT.ADV:1739-1802`) is not shown — `BattlePresenter.ActionLabelsOpen`
  leaves it out of the windows' layer and `GameScreen` answers its click wait at once, as a click
  (BSC-07) — after the release of a button still held, as `IK 0` does (`sys_key_releasewait`): a
  target's banner is chosen at the press, and without the release wait the flavour line's key wait that
  follows took the held button and closed at once (fixed 2026-10-13, `GameScreen.WaitForKeyAsync`). Instead each fighter's card shows a pill in the accent colour under its state line for the
  whole exchange: an icon and our name (`BattleCommands.ActionName`: Melee, Bow, Magic, Special — a
  special's own name is not known after the choice —, Defense; `BattleScene.Actions`, from the labels'
  event to the field's redraw). A player's special leaves the enemy's line blank: no pill there.
- **The dialogue panel** (owner, 2026-10-13, mock-up A of three, from the owner's Sengoku Rance example;
  docs/sengoku/04-screen.md §10): the speech bubbles (`WAO_SUB` 141/142, the player unit's, and 151/152, the
  enemy commander's — the flavour lines of `FIGT_M.ADV` and `FIGT.ADV:772`'s error line) are not shown;
  their line is our balloon (`godot/Battles/SpeechBalloon`). The speaker's portrait stays the original's
  framed picture with its name (the owner: the big portraits carry their own frame and name, and the
  squares cut for the cards are too small to enlarge, so no Sengoku face box with a name band). The balloon
  is 580 layout pixels wide beside the portrait (the player's at x 330, the enemy's ending 330 from the right),
  its bottom at y 436 — across the scene's lower edge, over the order strip, as Sengoku's crosses into the
  cards —, as tall as its text; rounded, a soft shadow, a tail towards the portrait 44 below its top; the
  text bold, 30, in the screen's font. Talk (141/151) is white with dark text, a thought (142/152, the
  original's dark bubbles) dark with white text. A marker in the accent colour bobs in the corner while the
  line waits for the click. The text is what the script printed into the bubble since it opened or its last
  key wait (`BattlePresenter.Speech`, `BattleSpeech`), its line breaks — made for the original's 240-pixel
  bubble — flowed into one paragraph (a space between Latin letters, none next to Japanese); the key waits
  and the erase stay the script's. `BattlePresenter.Windows` leaves the bubble's frame out of the windows'
  layer; the stage still draws it, so Original is unchanged. `BattleWindowsTests`: in the captured battles no
  bubble is in the layer and every bubble's key wait has a line.
- **The order messages** (owner, 2026-10-15): an order given from a card's menu is announced by the script in
  window 143 — another unit's (`L_0129e`, `FIGT.ADV:776-833`: "<unit> received orders to wait for instructions." /
  "to keep attacking." / "to keep defending.") and the acting unit's own Auto-Atk / Auto-Def (`L_01f08` / `L_01f42`,
  `:1315-1342`, "received orders to continually attack." / "defend."; its name is string 1, BTX-10). On our screen the
  message is not shown (`BattlePresenter.OrderedUnit` names the unit while it is open and keeps the window out of the
  windows' layer); `GameScreen` answers its key wait at once, as a click (the key lands in RetVal), after the release
  of a button still held, as the "vs" window's (the script's own release loop, `:828-832`, follows the window's close;
  the menu item was chosen at the release anyway). Instead the order's label on that unit's card flashes
  (`BattleBoardView.FlashOrder`): the muted colour turns saturated in 0.12 s (its hue at full saturation, as bright
  as the text on a dark palette; a grey takes the accent's hue), holds 0.35 s and fades back in 0.35 s — the card
  built again meanwhile (the own order changes only after its message) keeps the flash. Nothing in the rules changes.
  `BattleScreenTests` checks the presenter's side; `--auto --interface new` gives another own card its own order again
  (dump `order-flash` without `--fast`).
- **The cards' stats** (owner, 2026-10-15, after a Sengoku Rance fan mod's cards — the stats as numbers under the
  portrait, the level by the face — and four rounds of mock-ups: the original's box as our panel or as an expanded card,
  then a window at the top of the scene for the card under the pointer, with every line or four, or the four over the
  portrait on hover; all set aside): **every card shows the info box's values always**, so no Info is needed. The
  portrait shrinks to 112 (from 118, in the same 118-wide column) and moves to the top; under it a row of four numbers,
  each centred in a quarter of the face's width, in the cards' font with a dark shadow: troop attack, troop defense, magic,
  attacks — what the info box prints (docs/battle/screen-layout.md §6.1) but HP and troops (on the card already) and the
  standing order. A troop modifier of the battle's kind tints its number (green up, red down; the box's decoding: 10, its
  lone ")" (BSC-04), is none, and the enemy's are decoded as the player's though the battle adds them raw, BRS-08); without
  troops the numbers are the commander's own attack and defense (`:883-899`), in light blue. The player's Battle LV is
  "Lv n" in the portrait's bottom-right corner on a see-through dark plate (owner: the text alone was lost on the face);
  an enemy commander has none. "Surging with power" (the unit's partner deployed, `:1001-1004`) is a badge with an up arrow
  on the portrait's top-right corner, in the accent colour — grey while the unit has no troops, since the partner's
  +2/+1/+0 raises the troops' attack and defense rolls only (`FIGT.ADV:2457-2482`), though the box still prints the line.
  The model is `Original.Content/Battles/BattleCardStats` (`BattleCard.Stats`, from GameState; `BattleCardStatsTests`), the
  portrait `godot/Battles/CardPortrait`. The cards' menus no longer offer Info (53/54) — an enemy's card has no menu now —
  so the original's info box (window 143, its click) does not come up on our screen; Option → Interface → Original keeps it.
- **The limit and the advantages** (owner, 2026-10-15, question 4): Sengoku Rance's 戦果 bar has no counterpart — nothing
  judges a Kichikuou battle by a score (a time-out loses for the attacker) — so its place stays empty. The round header's
  "Advantage +n" (`FIGT.ADV:177-204`) adds up two things a player cannot tell apart, so our screen shows them as what they
  do (`BattleOpening.Effects`, `BattleAdvantage`): **"Walls +n%"** for the side that holds the city in an offensive or a
  defensive battle — the resolution takes `Bt_WallBonus * 2` from the other side's troops' hit chance against its troops
  (`FIGT.ADV:2548`, `:2741`; Oaks (6): +4, so "Walls +8%"; the header also adds the walls to the enemy's sum in a dungeon,
  where nothing applies them, and a dungeon's are 0) — and **"Strategy +10%" / "+50%"** for the side whose strategy
  succeeded: its troops deal a tenth or a half more troop damage and take as much less (`:2294-2317`); both
  "Walls +8%  ·  Strategy +10%", nothing without either. The same texts replace "Advantage +n" in the opening.
  Each has a **tooltip** (owner, 2026-10-15; the texts agreed with the owner, `BattleAdvantage.Parts`): up at once, no
  delay, while the pointer is on its label (`godot/Battles/AdvantageTip`, in the context menus' style), not while the
  opening or a dialog is up. Walls: "The city's walls: troops attacking this army's troops hit 8 points less often (a
  hit chance of 60% becomes 52%). Melee, bow and magic of troops only; commanders' attacks and specials ignore the
  walls." Strategy: "The strategy succeeded: for the whole battle this army's troops kill 10% more enemy troops and lose
  10% fewer to theirs (rounded down). Troops against troops only; commanders' attacks and specials are not affected."
  The label "Walls +8%" stays although it is points of hit chance (owner). **The
  battle's limit** — the most troops of one unit that fight in an exchange (docs/battle/02-resolution.md, `:624`), which
  the header prints as `<City: Battle Limit>` / `Up to 232` (`:145-164`) — is a plate centred at the scene's top
  (owner, 2026-10-16; near its foot, between an exchange's HP counters, until then), in the owner's words: "City · Battle Limit: up to 232" (Plains / Duel / City /
  Dungeon as the header's conditions; `BattleOpening.LimitText`, the opening's plate says the same); the cards' colours
  at 80 %, the frame's colour, its top 8 below the screen's edge, 28 high (`BattleBoardView.DrawLimit`), under the script's windows.
  `BattleOpeningTests` checks the texts.
- **The focus** (owner, 2026-10-15, question 5, mock-up 5B of three: squares at the card's foot, the badge, a number at
  the name line's end): the focus a Wait builds (`Bt_PSlot1-4Focus`: +1 a Wait, reset only at the battle's start, spent
  by the specials — 1, the beams 2, Vanish Bomb 3; docs/battle/01-flow.md §8.3) is "◆ n" on the portrait's top-left
  corner, on the Lv plate's see-through dark, the diamond drawn and the number in the accent colour
  (`godot/Battles/CardPortrait`); the state line no longer says "Focus n".
- **The cards' counters** (owner, 2026-10-14): a card's troops and HP — the number and its bar together —
  run to a new value instead of jumping to it, eased out, in Option → Battle Screen… → "Cards: counters"
  (0.6 s by default; `--meter-seconds x` for a launch), from the moment the blow lands (owner): the core writes
  the exchange's damage only after its result screen's key wait (`ApplyExchangeAsync`), but `FIGT` computes it
  before it calls `FIGTATTK` (`FIGT.ADV:2512-2847`), so from the shots' first frame (`ShotsFrameEvent` step 0) to the
  next event after the result's key wait the two fighters' cards show what the result screen shows — the current
  values less the two damages (`BattlePresenter.Strike`, `BattleStrike`; `BattleStrikeTests`: in the captured battles
  these are what the core then writes). Since 2026-10-16 (owner) the exchange's blow runs on the cards with the
  scene's counters ("The exchange's counters" below): in their steps, so both show the same numbers.
  Nothing waits for the run.
  A loss leaves a damage trail (owner, 2026-10-14): the lost part of the bar turns light (the bar's colour 70 %
  toward white) and stays while the counter runs, 0.2 s more, then drains in 0.35 s. At once while skipping
  (MsgSkip) and with `--fast`, when another unit takes the place, and when our screen is shown again
  (`BattleBoardView.Meters.cs`: the shown values outlive the cards' controls, built again on every change).
- **The exchange's counters** (owner, 2026-10-16, variant A of four mock-ups — B the original's places, C plates over
  the armies, D versus bars —, after Sengoku Rance's: docs/sengoku/04-screen.md §5, §9.5): each side's TROOPS and HP
  are big counters at the scene's foot, the player's at the left, the enemy's at the right (`godot/Battles/ExchangeCountersView`;
  the values `Original.Content/Battles/ExchangeCounters`, `BattlePresenter.ExchangeCounters`: before the blow and after it, from
  the shots' first frame). A side without troops has no TROOPS counter, as the original's. The troops in as many
  digits as before the blow (a leading one falls away too), Oswald Bold (owner, 2026-10-16, of sixteen fonts on a sheet)
  in a white-to-gold gradient with a dark
  outline, "TROOPS" over them in gold (Oswald SemiBold); the HP in smaller digits in a mint gradient beside them (towards the scene's middle), "HP" before it; straight on the
  field, with no band behind them (owner, 2026-10-16), their baselines 36 + 10 over the scene's foot, above the order
  strip (owner, 2026-10-16: the strip moved there). They rise from below the scene's foot in 0.25 s as the shots start, count as
  Sengoku Rance's do — every 0.2 s the value due 0.2 s later, from 0.1 s after the start, over Option → Battle Screen…
  → "Counters: run" (1.3 s by default; `--count-seconds x` for a launch) — and every digit that changes falls away:
  the old one drops 1.4 digit heights, drifts 10-24 pixels outward, turns half a turn and fades in 0.2 s. They hold
  the result while its screen is up and sink in 0.25 s with the next event after its key wait. The wiped-out marks
  (ANNIHILATED, UNABLE TO FIGHT) are moved up above them on our screen (`BattleScene.Numbers` off: the troops' marks 60
  picture rows up, the commanders' to y 56). At once while skipping (MsgSkip) and with `--fast` (the result's values,
  no rise, no falling digits).
- **The scout report** (owner, 2026-10-14, mock-up C of three, "all at once"): when the player sends a ninja to
  scout an attacking army (`FIGT_B.ADV:506-648`, `NinjaScoutReportAsync`; the original prints it into window 113,
  docs/battle/screen-layout.md §6.3), the report is our overlay over the original's screen (the map, before the
  field is drawn), shown only in New (`godot/Battles/ScoutReportView`): the screen dimmed and blurred; the ninja's
  whole portrait on the left (the original's framed 192×256 picture of op 321, Kanami's CG 9, 300×400 at
  (40, 40), its top with the balloon's so the tail points at it — owner, 2026-10-14); our balloon (the dialogue panel's, `SpeechBalloon.DrawBalloon`) to its right at the top: "The
  Approaching Enemy  [<army, string 10>]" bold, then "Commanders n      Total troops N"; the commanders as cards
  in a 2×2 formation — the front row (places 1-2) in the left column, the back row (3-4) in the right one, "front
  row" / "back row" below — in our battle cards' style (282×150, the face square with the crops, place and name,
  troop type muted, "Troops" muted and the number large), an empty place "n  empty". **Only what the ninja
  reports**: name, troop type, troops — no maximum, no HP, no bars; a commander without troops has no troop type
  and troops "none" (the report leaves the line out). The report lists `Bt_EnemyParty1-4` in order and the battle's
  `InitBattle` puts member i into slot i, so the places are the battle's. The model is
  `Original.Content/Battles/ScoutReport` (taken from GameState by `BattlePresenter.Scout` when "The Approaching
  Enemy", `:587`, prints — the parties' commanders, names and troop types through `Cmdr_StrBase`, the totals as the
  script's `Tmp0002` / `Tmp0001` — and held until the window closes after the totals). **All at once**: the overlay
  shows the whole army from the report's first key wait (after `:587`); the key waits of the commanders' lines and
  of the totals are answered at once by `GameScreen` (`BattlePresenter.ScoutReported`, after the release wait, as
  the "vs" window's), so the player clicks once; RetVal is kept (`FlowWaitKeyKeepRetValAsync`). **Rance's answer and
  Maris's comment** (owner, 2026-10-16: Maris's first, variant A of four — B her portrait in the column right of the
  cards, C a strip in the cards' style with her face, D her portrait under Kanami's; then Rance's, variant R2 of three —
  R1 the two balloons side by side, R3 one panel with both faces): the overlay shows at its foot from the start what the
  script has them say after the report's window, in its order — Rance's portrait (CG 1, 154×205 at (40, 572)) in the
  bottom left corner, his balloon "Mhm... I see." (`:629`) high beside it with its tail at him; Maris's portrait (CG 6,
  at (1086, 572)) in the bottom right corner, her balloon lower, from x 516 to her, with its tail at her: "Quite a
  strong force. What shall we do?" (`:637`) past 5000 troops (`ScoutReport.Reply`, `Comment`, `CommentLine`). Up to
  5000 she says only "............" (`:644`), which means nothing on our screen: her portrait and balloon are left out.
  Their windows after the report (`:629`, `:636-647`) are answered at once before a frame shows them
  (`BattlePresenter.ScoutReplied`); her portrait stays drawn on the screen behind as the original leaves it. **The way
  to the report** (owner, 2026-10-16): the overlay comes straight after the ninja's choice — the lines between
  (`:393-586`: Rance calls the ninja, how they come, their answer, Rance's order, the wait, "… returned with
  information") are answered at once before a frame shows them (`BattlePresenter.ScoutCalling`); Rance's "Yeah, this
  is a good time to use a ninja." before the ninja menu (`:314`) stays the original's window.
  `ScoutReportTests`: the model on a GameState, the presenter's hold and the replies at 5000 and 5001 troops, and in
  the captured defences (three with a scout report) the overlay's commanders, totals, places, answer and comment are
  what the script prints and the battle then places. `--scout` (with `--auto`) sends the first free ninja to scout at the
  first enemy attack, once a launch, clicking both menus as a player does and failing if the click is left for the
  next key wait (CLAUDE.md, "Commands").
- **The strategy's cut-in** (owner, 2026-10-15, mock-up A of three — B put it into the opening, C into the dialogue
  panel on the board): a successful pre-battle strategy (`FIGT作.ADV`, docs/battle/01-flow.md §4; the original shows
  its own screen, docs/battle/screen-layout.md §6.2, and three boxes: the announcement, the strategist's line, the
  effect) is our cut-in over the original's screen, shown only in New (`godot/Battles/StrategyCutInView`): its backdrop
  the original's strategy screen as drawn before the face and the first box (`BattlePresenter.StrategyBackdrop`: the
  field and the strategist's troops, none of the boxes nor an earlier dialogue's leftovers — owner, 2026-10-16), dimmed
  and blurred; a slanted band in the side's colour (the opening's blue or red), strong at the strategist's side
  and fading across, its edges in the accent colour; the strategist's whole portrait (the framed 192×256 face CG,
  300×400) sliding in from that side (the player's left, the enemy's right), its top at the first line's, the whole
  centred on the screen (owner, 2026-10-16); the script's announcement in Cinzel Bold
  ("ALLY ARMY'S STRATEGY WAS SUCCESSFUL.") over a large title in Shojumaru — "Success", +50% "Great Success" in the
  accent colour (owner, 2026-10-16: no "Strategy" in it); the strategist's line in our balloon (the
  dialogue panel's, dark for the original's dark bubbles 142/152), **word for word**, its tail at the portrait; below
  it the advantage as the opening and the armies' labels show it ("Strategy +10%", larger for +50%). No sentence of
  what it does (owner, 2026-10-15): the label's tooltip on the board tells it. The model is
  `Original.Content/Battles/BattleStrategy`, held by `BattlePresenter.Strategy` from the `StrategyScreenEvent` to the
  close of the effect's box and filled in as the script goes (the announcement as printed, `Bt_StrategyResult`, the
  bubble's line). **One click**: the announcement's and the effect's key waits (window 143,
  `BattlePresenter.StrategyAnswered`) are answered at once by `GameScreen`, as by a click; the line's wait is the
  player's. `BattleStrategyTests`: the model, the presenter's hold, and in the captured battles the cut-in at the line's
  wait is the script's strategist, face, result and line, two waits answered for each strategy; `BattleInputReplayTests`
  replays them answered so. `--auto` in a window dumps `strategy` at the line's wait.
- **The opening** (owner, 2026-10-14, variant C of three after Sengoku Rance's opening; mock-ups and a font sheet
  from the week-18 attack on Oaks (6)): the round header (`FIGT.ADV:112-216`) is shown once a battle, at its first
  round, as our own opening over the scene (`godot/Battles/BattleOpeningLayer`), and the later rounds' headers not
  at all — they differ only in the rounds left, which the gauge shows (the kind, the place, the limit and the
  advantages are set before the first header: the walls' bonus before the battle, the strategy's result before the
  first round). The opening: the battle's own field (the scene's pictures, the blurred sides too) cut in two along a
  slanted seam (x 680 at the top, 600 at the bottom), the player's half tinted blue, the enemy's red (the cards'
  colours, 47 %); in each its army's name (string 7 / string 10) in **Shojumaru**, white with a dark outline, its
  last line's baseline at y 330, kept 90 px off the seam — the fewest lines up to three that fit, balanced (the
  widest line the narrowest), at the largest size from 64 down; "Leazas Rebel Army" takes three lines, and so does
  the longest name of the scenario, "A Certain Country’s People" (`BattleOpening.Wrap`) — and its advantage below
  it as what it does ("Walls +8%", nothing without one; "The limit and the advantages" below); "VS" between them; on top a plate (the cards' colours, the accent frame) in **Cinzel** Bold: "Oaks —
  Offensive Battle" (or "Duel", "Dungeon Battle"), "City · Battle Limit: up to 232" (Plains / Duel / City /
  Dungeon, as the header's conditions), "5 turns" (not in a duel). When our battle screen comes up with the first
  header, the scene shows the field alone and the opening — the halves' tints, the names, "VS", the plate — fades in
  over it from transparent (owner, 2026-10-14; Option → Battle Screen… → "Opening: appear", 0.35 s by default,
  `--opening-appear-seconds x` for a launch; at once while skipping and with `--fast`; `--auto` in a window without
  `--fast` dumps it halfway, `opening-appearing`, and whole, `opening`). The header's key wait stays the player's click;
  then the tints, the seam and "VS" fade where they are, the names (with their advantages) part to the sides and
  the plate rises (eased in; owner, 2026-10-15 — until then the halves parted with copies of the scene; Option →
  Battle Screen… → "Opening: leave", 0.5 s by default, `--opening-seconds x` for a launch; at once while skipping
  and with `--fast`) while the battle goes on; the limit's plate at the scene's top shows as the opening goes.
  **The cards slide in** under the opening as it comes up, after Sengoku Rance's (owner, 2026-10-15; docs/sengoku/04-screen.md
  §6.5): each from beyond its own side (the player's from the left, the enemy's from the right), linear, clipped to
  the panel; the front row 640 layout pixels in Option → Battle Screen… → "Cards: slide in" (1 s by default, Sengoku
  Rance's 400 px in 1 s ×1.6; `--cards-slide-seconds x` for a launch), each card a random 0-160 pixels further and
  the back row 160 more, so the front row stands at 1-1.25 s and the back row at 1.25-1.5 s (our own random draw,
  never the script's). In a dungeon (`Bt_Kind` 3) no armies collide: the cards stand at their places and come out of
  the dark instead (owner, 2026-10-21), each growing opaque in the time its slide would take
  (`BattleBoardView.StartCardsSlide`; a look: `--dungeon` on the week-18 save). **The order strip is held back** while the opening stands; at its click its flags fly in from
  the screen's right edge one after another, the first first (Sengoku Rance's, 04-screen.md §4): Option → Battle
  Screen… → "Strip: fly in" apart (0.25 s by default, `--strip-fly-seconds x`), at 960 layout pixels a second at that
  (Sengoku Rance's 0.6 px/ms ×1.6), as much slower or faster as the setting is; at once while skipping and with
  `--fast` (`OrderStrip.Hold`). The opening's key wait returns only once the opening has left and the last flag stands
  (owner, 2026-10-21, `GameScreen.OpeningLeaveAsync`): neither side acts while they move — before, an enemy that acted
  first began its turn under them. A later round's header is answered at once by `GameScreen`, as a click (its
  key lands in RetVal, `FIGT.ADV:214`, which BTX-03 reads), after the release wait. The original's box is no longer
  in the windows' layer. **The advantages** stay on the screen all battle long, over the cards next to the armies'
  names, in the accent colour — since 2026-10-15 as what they do, and the battle's limit at the scene's top ("The
  limit and the advantages" below). The fonts are
  bundled in `godot/fonts` under the SIL OFL 1.1 with their licenses (`Shojumaru-OFL.txt`, `Cinzel-OFL.txt`). The
  model is `Original.Content/Battles/BattleOpening` (`GamePresenter.Opening` while the first header waits,
  `GamePresenter.LaterRoundHeader`, `BattlePresenter.RoundHeader`); `BattleOpeningTests`: the texts, the advantages,
  the wrap, and in the captured battles every header waits with the scene up, the first is the opening, the later
  ones are not, and the advantages are the header's. `--auto --interface new` without `--fast` in a window dumps
  the cards on their way in (`cards-sliding`), the opening halfway out (`opening-leaving`) and the flags flying in
  (`strip-flying`).
- **The exchange's result** (owner, 2026-10-14, after Sengoku Rance's, which never stops after an attack:
  docs/sengoku/04-screen.md §9.11): the result screen's click (`FIGTATTK.ADV:247`, `IK 0`) is the battle's most
  frequent, and our screen already runs the cards' counters from the result ("The cards' counters" above; the scene
  showed the result's numbers too until 2026-10-15). So when no wiped-out mark is on it, `GameScreen` holds it for Option → Battle Screen… →
  "Results: hold" (1.2 s by default — the counters' run and the damage trail; `--result-seconds x` for a launch) and
  goes on as by a click: the key lands in RetVal (BTX-03) as `KeyReturn`, after the release wait; a key or a click
  ends the hold sooner and is the key, as `IK 0` returns it; at once with `--fast`, and under the message skip at
  once with 0, as `IK 0`. With ANNIHILATED or UNABLE TO FIGHT (`:235-246`) the player's click stays (owner). The
  presenter tells them apart (`BattlePresenter.ExchangeResultWaits`, `ExchangeResultWipedOut`); `BattleResultWaitTests`:
  in the captured battles only the result screen's key wait is one, wiped out exactly when a mark is shown. Kichikuou
  has no troops' animation and no effects but the shots' numbers crossing the scene: the exchange's effects are next
  ("The exchange's effects").
- **The exchange's effects** (owner, 2026-10-14; under way): Sengoku Rance's attack effects on our scene, **made by
  us** — the owner turned down both a hit of our own design (the struck army shaking, the damages popping in) and
  Sengoku Rance's own pictures, which are the game's art: they may never enter the repository, and reading them from
  the player's copy of Sengoku Rance would tie the screen to owning it. Ours are Godot particles and shaders with
  textures drawn by code, **as like Sengoku Rance's as we can make them** (owner) — the composition, the colours, the
  rhythm and the length — but **not traced frame by frame** (owner). The references are docs/sengoku/04-screen.md
  §2.4.1 (the table, the 800×314 frames added over the scene, actor left and target right, mirrored for an enemy
  actor, 30 frames of 50 ms) and the frames themselves, extracted into the scratchpad with
  `tools/sengoku/effect_frames.py` (never committed). Kichikuou's attack types (`Bt_PlayerAtkType`, from the unit's
  class: 0 melee, 1 bow, 2 magic; docs/battle/01-flow.md §8.1) take Sengoku Rance's **Samurai (40000)**
  for melee, **Archer (40260)** for the bow and **Mage (40360, Fire Blast)** for magic — every magic class alike at
  first (confirmed by the owner, 2026-10-14). **Since 2026-10-21 a magic attack plays its element's effect** (owner;
  the plan, the spells and the casters in docs/ui/mage-lines.md): when the firing side's action is magic (2) and no
  special applies, the element of the **spell its caster shouts** — the last spell line the presenter was shown since
  the engagement started (`MagicSpells`, `BattlePresenter`), whether or not the line was drawn —, else the **caster's
  element** (kept per record and side: a unit by `Unit_Cur`, a commander by `Cmdr_Cur`; a Demon Magic caster (class 11)
  not in the table is dark), else **fire** (`ExchangeEffect.Of`, the kinds `IceMagic`, `LightningMagic`, `DarkMagic`,
  `LightMagic`, `PoisonMagic`; `Magic` stays fire). The effects, each ours after the reference the owner chose: fire
  the Fire Blast (`MagicEffect`, 40360), ice `IceEffect` (40730 recoloured white-blue), lightning `LightningEffect`
  (40630 in blue-white), dark `DarkEffect` (47030, **laid over dark** as the black beams, `EffectPlayer.Dark`: its light
  blacks the scene out), light `LightEffect` (40430, Angel Cutter), poison `PoisonEffect` (41300, Death by Poison:
  marbled gas clumps, `EffectTextures.Gas`). A mage's Direct attack stays melee. The effects are drawn under the exchange's numbers (the counters, the shots,
  the damages), at their full brightness as Sengoku Rance's — the fireball whites out the target's half for ~0.4 s
  (owner, 2026-10-14). The bow and the magic come first, then the wiring of all three into the exchange (owner,
  2026-10-14); open for the wiring: Kichikuou's exchange has an action on each side (`Bt_PlayerAction`,
  `Bt_EnemyAction`), where Sengoku Rance's blow has one actor and one effect — both effects at once, or the turn's
  actor's alone. Our scene is 640×256 shown ×1.6 (1024×410): a frame scales ×1.28 to 1024×402. Steps: (1) the
  references — done 2026-10-14; (2) the melee effect alone, as a Godot scene with Debug → Effects to play it over a
  battlefield, frames rendered next to the reference's for the owner — done 2026-10-14, the owner took its first look
  ("v1") as it is; (3) the bow and the magic — done 2026-10-14, the owner took their first look ("v1") as it is; (4)
  the wiring into the exchange, decided by the owner (2026-10-14) — done 2026-10-14 (below, "Wired in"):
  - **When**: from the shots' first frame (`ShotsFrameEvent` step 0, `FIGTATTK.ADV:33-131`). The shots take ~165 ms
    and the pause after them (`:133`, `Y 3, 40`) 640 ms, so the effect's peak falls on the numbers' flight and its last
    ~0.7 s on the result screen and the cards' counters, as Sengoku Rance's runs with its counters; nothing waits for
    it and the exchange is no longer.
  - **Whose**: an exchange is one-sided but for direct against direct (the defending side's action is 99 and it
    fires nothing: the player's bow and magic, the enemy's back-row bow and magic, every special but Soaring Wave;
    `Battle.Resolution.cs`, `Battle.Flow.Enemy.cs`). The effect is the firing side's, by its action (0 melee, 1 bow,
    2 magic); in a two-sided exchange only the turn's actor's, as Sengoku Rance's blow has one actor and one effect.
    Mirrored when the enemy fires.
  - **Specials**: the player's specials get Sengoku Rance's own counterparts, ours as like them as we can make them —
    Rance Attack first (Sengoku Rance has one: find its effect through the action table, `あべ/T_EFFDAT.JAF.jam`
    `add1`, and make ours); the others by their action (melee or magic) until a counterpart is found for each (owner).
    **Rance Attack** (owner, 2026-10-14): Sengoku Rance's is effect 40500 for the general's figure alone, then the
    troop's charge with the Samurai effect (04-screen.md §2.4.1); Kichikuou's is the commander's blow alone (the troops'
    shots zeroed, no counter, the unit's own sprite; docs/battle/02-resolution.md §3.3), so it plays **one effect**, ours
    after 40500, for the command whoever uses it — Rance (unit 1) or Kentarou (unit 31; his own counterpart since
    2026-10-15, below); By la Way, Rick's (unit 7)
    special 1, stayed melee (now the same in red, below). No spotlight: the owner saw none in Sengoku Rance's Rance Attack either. The choice reads
    special 1 with the actor (`Bt_SpecialAtk`, `Unit_Cur`), since the shots' sound selection has reset `Bt_SkillSfx`.
    One captured battle has one (`20261008-111109-01-FIGT_A`, in the full run's captures only); `ExchangeEffectTests`
    checks the rule on given states and that a Rance Attack's troops fire nothing. The owner saw it in a battle and
    took it (2026-10-14).
    **The other specials** (owner, 2026-10-14, from the survey in 04-screen.md §2.4.1, "Counterparts for Kichikuou's
    other specials"; each told at the shots by `Bt_SpecialAtk` and `Unit_Cur`): White D. Beam and EPO Light after
    Sengoku Rance's White Destruction Beam (40460), Black D. Beam the same recoloured dark; Whirlwind Shot after
    Piercing Tempest (40660); High Speed after Commander Charge (41400; owner, 2026-10-15, in place of Sonic Shuriken); the ninja's follow-up after the Ninja troop's
    effect (40100); By la Way after Rance Attack (40500) recoloured red, as Rick's sprite (owner, 2026-10-15); Vanish Bomb after Begone Bomb (40560); C. War Dance after
    Ieyasu's Special Move (47400); Chuhai after Shogun Whirlwind (47060); T2 Goliath after Gigai Punch (46820, 15
    frames); Kentarou's Rance Attack after his own Kentarou-kun Slash (40800); T2 Soaring Wave our own (no counterpart; the
    owner's design, 2026-10-15). **Kentarou-kun Slash** (2026-10-15, the owner took it as it is):
    special 1 by Kentarou (unit 31) — the same Rance Attack command as Rance's (`FIGT_SEL.ADV:579-588`), its rule
    unchanged — plays ours after his own move in Sengoku Rance (`ExchangeEffectKind.KentarouSlash`; the row `(1, 31)`,
    Rance's `(1, 1)` keeps Rance Attack's). **The beams** (2026-10-15, the owner took their v1 as it is): special 2 (`Bt_SpecialAtk == 2`) by Shizuka (unit 17), Magic (unit 39) or
    Gandhi (unit 37) plays the white beam, each user's own since 2026-10-15 (owner: three effects, placed for each
    sprite, to be told apart in the debug panel and tuned apart later; `ExchangeEffectKind.ShizukaBeam`, `MagicBeam`,
    `EpoLight`, the rows `(2, 17)`, `(2, 39)`, `(2, 37)`), by Chizuko (unit 38) or Nagi (unit 40) the dark one, each
    user's own as well (owner, 2026-10-15; `ChizukoBeam`, `NagiBeam`, the rows `(2, 38)`, `(2, 40)`); special 2 by anyone else plays by its action. No captured battle has
    a beam: `ExchangeEffectTests` checks the rule on given states, and in the captures that a beam's troops fire nothing
    (nor its commander at the commander, 02-resolution.md §3.3). **Whirlwind Shot** (2026-10-15, the owner took its
    v1 as it is): special 3 (`Bt_SpecialAtk == 3`, `FIGT.ADV:1542-1550`) by Isoroku (unit 27) plays ours after
    Piercing Tempest (`ExchangeEffectKind.WhirlwindShot`). **High Speed** (2026-10-15, the owner took its v1 as it is):
    the same setter by Meglass (unit 30) plays ours after Commander Charge (`ExchangeEffectKind.HighSpeed`). **The
    ninja's follow-up** (2026-10-15, the owner took its v1 as it is): the follow-up sets special 3 too
    (`FIGT.ADV:1980`) with the ninja — Kanami, Freya, Kaoru or Wichita alike — as record 51 (`Unit_Cur` 51,
    `:1922-1968`) and plays ours after the Ninja troop's 40100 (`ExchangeEffectKind.NinjaFollowUp`); one captured battle
    has one (`20261010-213655-86-FIGT_A`, in the full run's captures only). **Vanish Bomb** (2026-10-15, the owner took its v1
    as it is): special 4 (`FIGT.ADV:1560-1570`) is Miki's (unit 32) alone — the menu offers it to her only
    (`FIGT_SEL.ADV:644`) and her involuntary bomb is hers too (`FIGT.ADV:1588-1590`) — and plays ours after Begone Bomb
    (40560, `ExchangeEffectKind.VanishBomb`); no captured battle has one. **C. War Dance** (2026-10-15, the owner took its
    v1 as it is): special 5 (`FIGT.ADV:1571-1577`) is Patton's (unit 18) alone — its menu item (`FIGT_SEL.ADV:449`) is
    offered under `Unit_Cur = 18` only (`:662-664`) — and plays ours after Ieyasu's Special Move (47400,
    `ExchangeEffectKind.WarDance`); no captured battle has one. **Chuhai** (2026-10-15, the owner took its v1 as it is):
    special 6 (`FIGT.ADV:1551-1559`) is Asuka's (unit 11) alone — its menu item (`FIGT_SEL.ADV:464`) is offered under
    `Unit_Cur = 11` only (`:671-673`) — and plays ours after Shogun Whirlwind (47060, `ExchangeEffectKind.Chuhai`); no
    captured battle has one. **T2 Goliath** (2026-10-15, the owner took its v1 as it is): special 10 is set by Goliath's strike alone (`L_01abd`,
    `FIGT.ADV:1120-1148`: automatic, once a week when `Goliath_State == 1 and Goliath_Prov == Prov_Cur`; record 51
    rebuilt as "T2 Goliath", the player's action 0 and the enemy's 99, so the effect is the player's whatever the stale
    `Bt_MsgMode`) and plays ours after Gigai Punch (46820, `ExchangeEffectKind.Goliath`; `Unit_Cur` 51 with special 3 is
    the ninja's follow-up, so the row is `(10, 51)`); Goliath has no special sprite (`CgFull` 0); no captured battle has
    one. **T2 Soaring Wave** (2026-10-15, our own, the owner's design; the owner took its v2 as it is): special 7 (`FIGT.ADV:1525-1531`) is Rolex's
    (unit 47) alone (the menu's item 20, `FIGT_SEL.ADV:689`) and the only special the enemy answers: both actions 0, a
    two-sided exchange whose actor is the player's, as `L_02528` sets `Bt_MsgMode` 3 before the shots; it plays ours
    (`ExchangeEffectKind.SoaringWave`, the row `(7, 47)`); no captured battle has one. **By la
    Way** (2026-10-15, the owner took its v1 as it is): special 1 by Rick (unit 7) plays Rance Attack's effect
    recoloured red (`ExchangeEffectKind.ByLaWay`). One captured battle has a By la Way (`20261008-111109-01-FIGT_A`,
    the Rance Attack's, in the full run's captures only), none a Whirlwind Shot, a High Speed, a Vanish Bomb, a War Dance, a Chuhai, a Goliath or a Soaring Wave: `ExchangeEffectTests` checks
    the rules on given states (one table, `ExchangeEffect.SpecialOf`), and in the captures that a Whirlwind Shot, a
    High Speed or a ninja's follow-up fires only the commander's one shot at the commander, a By la Way's or a Vanish Bomb's troops fire nothing
    and a War Dance's, a Chuhai's or a Goliath's troops fire nothing, nor its commander at the commander, and a Soaring
    Wave's troops fire nothing and its commander two shots at the commander (02-resolution.md §3.3). The Cheater Beam
    stays melee.
  - **Defend** (owner, 2026-10-20): a unit's Defend plays Sengoku Rance's guard actions' counterpart, ours after
    **45080** (docs/sengoku/04-screen.md §2.4.1, "Guard"), though no shot is fired: the command (`L_02078`,
    `FIGT.ADV:1382-1410`; also an Auto-Def order and an Auto-Atk falling back to defend, 01-flow.md §8.4) prints its
    "Defense Increase" into box 143 (`:1399`), which our screen shows as the badge on the unit's card
    (`InfoBoxKind.Defence`, docs/ui/messages.md, §5 step 5); the effect starts with the badge's flash
    (`BattleBoardView.PlayGuard`, from `GameScreen` where the badge's wait is answered), over the defender, who stands
    alone on the player's half then ("The scene between exchanges"). The enemy's defence too (owner, 2026-10-20),
    mirrored over the defending commander, the player's unit it picked as its target on the other half: it prints
    nothing (`EState := 20`, `:1451-1464`, then the redraw, `L_0232f`), so the presenter tells it at the redraw's start
    where a commander's state has become 20 since the last one (`BattlePresenter.EnemyDefence`, counted by
    `EnemyDefences`; by the record, as a defender keeps its state when it moves up) — only once the round's turns have
    started (its first panel highlight): the redraws before (the round's header, its redraw, the Land of Death's, then
    the states' reset, `FIGT.ADV:105-313`; and those before a battle's first round) still show the last round's
    states, at a battle's start an earlier battle's. **The battle waits for it** (owner, 2026-10-20: with Auto-Def the
    next turn took the defender off the scene at once): `GameScreen.GuardAsync` holds the badge's wait, or the enemy's
    redraw, for the effect's 0.7 s (`GuardEffect.Seconds`); nothing when it does not play. `ExchangeEffectTests` checks
    in every captured battle that each enemy turn ending in a redraw without an exchange with its actor's state 20 is
    one defence at its place and no other redraw notes one (46 in 24 captures); `--auto` in a window without `--fast`
    dumps each side's once a launch, at the wall's height (`effect-guard-player`, `effect-guard-enemy`, 400 ms after its
    start).
  - **A setting**: Option → Battle Screen… → "Effects: on/off", on by default (Sengoku Rance's Combat Effect Display);
    none while skipping (MsgSkip) or with `--fast`.
  **Wired in**: the presenter takes the effect at the shots' first frame (`ExchangeEffect.Of` on the state,
  `BattlePresenter.Effect`, counted by `EffectsStarted`); `BattleBoardView` plays each start once in its scene, an
  `EffectPlayer` between the armies' layers and the front (the overlay and the numbers), at the scene's foot, scaled
  with the screen, hidden when done; the setting is `BattleScreenSettings.Effects` (a check box). `ExchangeEffectTests`:
  the rule on given states, and in every captured battle each exchange's shots start an effect whose side fires (its
  action is not 99 and it has a shot), a two-sided exchange being direct against direct with the actor in
  `Bt_MsgMode` 3 or 4. `--auto` in a window without `--fast` dumps each effect and side once a launch, at its peak
  (`effect-melee-player`, `effect-magic-enemy`, …).
  **How it is built** (`godot/Battles/Effects`, presentation only): the pictures
  are drawn by code (`EffectTextures`: a noisy puff, a crescent's body and its inner edge, an urchin, a four-point
  sparkle, a ring, a streak, a dot; for the bow an arrow, a release burst, a bow's arc, a hit's "crown", a bokeh disc;
  for the magic a glow, a flare's rays, a shockwave's lens), white with the shape in the alpha and tinted by each draw.
  An effect is a list of particles (`Particle`: texture, birth and life in frames, position and drift, turn and spin,
  size and growth, colour, an alpha curve over its life; optionally a colour by age and an eased path) made from a seed
  (`MeleeEffect.Build`, `BowEffect.Build`, `MagicEffect.Build`), drawn at a time in frames by `EffectCanvas`
  (each particle added to what is under it) in Sengoku Rance's 800×314 design space. `EffectPlayer` draws the canvas on
  black in a viewport of its own, scaled ×1.28 to 1024×402, and lays that picture over the scene with a shader:
  **screen** (1 − (1 − scene)(1 − effect), Sengoku Rance's draw method 1 as we read it) or add; mirrored when the enemy
  acts. The melee effect: on the actor's half the dust the armies raise as they clash (owner, 2026-10-15: the
  reference's bright speckled cloud read as magic) — 60 dim sand billows rising from the ground, first at the clash
  (x ~430, from frame 0) and back over the actor's ranks to x ~10 (by frame ~6), and up to ~90 % of the height (y 30-314;
  owner, 2026-10-15), the higher ones up to 3 frames later, swelling, drifting up and
  back and darkening, thinning out over the second half of their 13-20 frames, with 70 clods of dirt thrown up and
  falling back and 24 jets of dust kicked up and back (frames 0-10); on the target's half slashes (five a frame at the start down to two by frame 21, small
  violet ones to 24; each lives 1.4-2.4 frames, turning a little), urchins (swelling and fading over 5-9 frames, to
  frame 16, five large deep blue ones), sparkles (nine a frame to frame 12, then four, to 22), rings and streaks.
  The bow effect: on the actor's half a volley of white arrows leaving up and to the right from release bursts
  along a line running down to the left (frames 0-6; the bursts linger 6-11 frames) and bows' arcs standing and fading
  (from frame 3, 9-16 frames); on the target's half cream arrows falling down to the right (5-26, denser to 14; no arrow
  flies level: the reference's level second volley and gold arrows are left out, owner, 2026-10-15), gold crowns and flat teal streaks
  where they hit (6-21) and 90 bokeh discs fading to frame ~27. The magic effect, all on the target's half: an orb
  (a white disc, a warm halo, rays; frames 0-2), a white-hot ellipse with flames licking up (2-5), 130 billows thrown
  out from the centre and slowing (born 2-10, white turning yellow, orange and smoke with age, the late and far ones
  sooner) with 45 smaller flames on the rim, a pale lens of a shockwave running left off the fireball (3-16), then a
  white core (from 12.5 to the end), gold sparkles, ember trails and embers. Over a bright battlefield, screen blending
  leaves the late sparkles and the core faint. The Rance Attack effect (`RanceAttackEffect`, 2026-10-14, the owner took its v1 as it is; textures
  of its own: a spike, a flame's tongue, a billow without the puff's grain): 140 spikes fanning up from a white-hot
  foot in the middle (frames 0-6, white turning ice and blue) over a flat ground glow and five glassy rings spreading
  and drifting left, darkening to blue over a dark blue disc (to ~18); a white mass of billows spreading right behind
  a pale shock arc that throws sparks (front at x 545 at frame 5.5, 55 a frame), each billow dying as the mass draws
  back to the right edge (the middle at ~10.5, the edge at ~22; timed on the reference's white coverage by band), its
  trailing side burning in rows of blue tongues, each with two fainter copies trailing down-left (12-24); a browner
  dust cloud over the target's half (13-26; the reference's sand dust on the actor's half is left out, owner,
  2026-10-15); a cluster of pale blue motes in
  the middle (from ~7, gone by ~28). Rance's is drawn 100 units right of that (owner, 2026-10-15; the positions here
  are before the shift); By la Way stays where it was laid out.
  The White Destruction Beam effect (`WhiteBeamEffect`, 2026-10-15, the owner took its v1 as it is; textures of its own: a sphere of light, a swirl,
  a streak, a haze — a far flatter glow): a white disc in a haze of soft cyan-grey rays filling the frame (frames 0-3),
  breaking up in a violet halo with pink-white lumps and glints on its rim (3-5); a white disc at ~5 swelling into a
  mottled sphere of light whose clouds spread with it, fading by ~17; a needle shooting right from the flash (2-4),
  then the beam with a glow at its root and a teal cone widening over the target's half (5-14) under a teal haze that
  lingers (to ~27), swirls and white stars; then 640 horizontal streaks rushing right from the mass's back, which draws
  back from x 230 at frame 11 to 500 at 22, white to ~19, then cyan, blue and violet, with white knots, the last violet
  ones to ~27; teal sparkles in glows round the disc's rim from frame 4, scattering over the actor's half and
  twinkling to ~26; a grey glow lingering in the middle (10-27). Timed on the reference's near-white share and mean
  brightness in eight column bands. The **Black D. Beam** is the same effect laid over dark (`EffectPlayer.Dark`, a
  shader mode): the effect's light read as shadow — its bright parts black the scene out (by 92 %), its fainter parts
  and edges glow blue-violet (peaking at half light) — after Kichikuou's own dark sprites (CG 1525, 1526).
  Each white beam's user has its own effect (owner, 2026-10-15), the beam above placed for its sprite by
  `WhiteBeamEffect.Placed` (turned about the flash, then moved; every particle's position, drift, turn and path):
  Shizuka's (`ShizukaBeamEffect`) moved 76 units left, Gandhi's EPO Light (`EpoLightEffect`) 150 left and 40 up,
  Magic's (`MagicBeamEffect`) 140 left and 120 up — the flash on the orb over her head — and turned down 0.28 rad
  (~16°) so that it runs into the enemy's army, toward its middle at about (640, 185), not over its heads. Magic's and
  Gandhi's are stretched right past the sphere so that they run on through the army instead of dying in its middle
  (owner, 2026-10-15): `ParticleLayout.StretchedRight` from x 450 (the flash's 300 + 150) by 1.5 and 1.45 — every
  particle right of it drawn that much further from it, its drift and path along x too, and but for the round ones
  (the sphere's discs, sparkles, specks, rings, swirls) that much wider. The black
  beams too (owner, 2026-10-15): Chizuko's (`ChizukoBeamEffect`) moved 27 units left and 20 up; Nagi's
  (`NagiBeamEffect`) without its opening orb — `WhiteBeamEffect.Build(seed, orb: false)` leaves out the flash (its
  disc, glows, haze, rays and halo, frames 0-5), its break-up lumps, the sphere of light it turns into with its clouds
  (5-17) and the sparkles round its rim, the rest drawn the same — so that it starts with the needle; moved 150 units
  left and stretched right 1.45× from x 450, as Gandhi's, so that it runs on through the enemy's army (owner,
  2026-10-15).
  The Whirlwind Shot effect (`WhirlwindShotEffect`, 2026-10-15, the owner took its v1 as it is; after Sengoku Rance's Piercing Tempest, 40660 — 29
  frames in the archive, the 30th absent; a texture of its own, `Lens`: a tall arc with a bright rim on its outer side and
  a glassy fill): a whirlwind in the middle (its eye at ~(414, 168)) — six arms of beads placed by the frame, their
  heads winding in from a ring of radius ~210 (frame 0) to the eye (~11) as the arms turn, white glows with a soft haze
  at the heads, teal ones with four-point glints down the tails, which grow out over the first seven frames, and six
  all-teal strands just outside them from frame ~2; the whole draws in about the eye after frame 12 and fades by ~17;
  a shot of white blobs streaks right from the eye (12-13), then a white burst there with 40 cream rays fanning up and
  down (13-17) and a slim stream of white billows to the right edge (from 13; 16-28 high, flat and unturned —
  owner, 2026-10-15: v1's 45-75 high beam read as a blast, not an arrow's shot), led by a white arrow of light flying
  from the burst to the edge (12.8-14.4) over a narrow glow, dying from its root (~18) to the edge
  (~23); the burst closes into a tall white oval (16-19) that stands as the lens's arc, its rim to the left, glass
  turning teal as it fades (18-27); a thin white needle and cyan streaks rush right along the beam's line (18-25) over
  300 teal stars in clumps, twinkling (17-27). Timed on the reference's near-white share and mean brightness in eight
  column bands. The positions here are as laid out; its shot (the particles born from frame ~11.5) is stretched right
  1.75× from x 474 (the eye's 414 + 60) so that it runs on through the enemy's army, the whirlwind keeping its shape,
  and the whole drawn 244 units left and 20 up, onto Isoroku's sprite (owner, 2026-10-15).
  The High Speed effect (`HighSpeedEffect`, 2026-10-15, the owner took its v1 as it is; v2 recoloured to Meglass's blues
  after the owner's note, which the owner took as it is; after Sengoku Rance's Commander Charge, 41400 — 29 frames in
  the archive; textures of its own: `Bar`, a band even along its length; `Petal`, a soft filled ellipse; `Moon`, a
  half-moon): a white burst at (312, 162) on a thick white bar across the whole frame in a royal-blue haze turning sky
  blue (frames 0-3), the bar thinning to a thread in eight overlapping steps (gone by ~19); the reference's petals
  bursting out of the burst are left out (owner, 2026-10-15); a
  white disc with a bright rim swelling from the burst (2.5-7) into a blue-grey halo of two rings (6-16, radius 290 by
  16); then 110 blue, ice and lavender half-moons, each with two fainter copies trailing its turn, spinning in from x
  ~450 (frame 9) and drifting right at 16-30 a frame, the front moving right with them (9-24), and 240 pale blue
  four-point sparkles among them, half with a glow (10-28). Timed on the reference's bands as the others. Its colours
  are Meglass's (her sprite, CG 1527: royal blue, sky blue, ice, lavender; owner, 2026-10-15: v1's amber and gold did
  not match the blue strike on her sprite). Its opening is moved onto her sprite (CG 1527; owner, 2026-10-15): the burst, the
  disc and its halo 256 units left and 46 up, onto her afterimage on the left (the scene's x 10-80) on
  the line the sprite draws (its y 99: 116 here); the bar along that line, from x -40 to 640 — from left of her
  afterimage to where the line meets her figure on the right (the scene's x ~500) — at the same height; the
  half-moons and sparkles over the enemy stay. The **By la Way** effect is Rance Attack's recoloured red
  (`RanceAttackEffect.BuildRed`, after Rick's red sprite, CG 1521, red 245, 40, 35; v2 redder after the owner's note,
  2026-10-15: v1 read orange; the owner took v2 as it is): every colour where blue leads or that is white turned red — white to a pinkish red
  (salmon, then white, only where the light piles up), ice to red, the blues deep red — and the sand dust reddened to
  a rust brown. The dust on the actor's half, which reddened piled up into a lone glow behind Rick, is out of both
  (owner, 2026-10-15), and the white mass over the target's half (its billows right of x 450) is a deep, denser red
  (0.72, 0.06, 0.04 at 0.75) in place of the pinkish red, which read see-through there (owner). Its blows land on the enemy's ranks (owner,
  2026-10-15: to show that they are hurt; `RanceAttackEffect.Hits`, By la Way's alone): 22 hits over x 530-780,
  each as the mass's front reaches it or up to six frames after (frames ~6-16) — a white flash and a four-point star,
  a gold burst of spikes (gold, to stand out of the red mass), a red slash with a white edge across it and red and
  orange shards flying out. Over a sand battlefield its peak's core still reads pale, its red shows in the mass's fringe, the
  rings, the flames and the motes.
  The ninja's follow-up effect (`NinjaEffect`, 2026-10-15, the owner took its v1 as it is; after Sengoku Rance's Ninja troop effect, 40100;
  textures of its own: `Shuriken`, a four-point star with concave sides and a round hole; `Wing`, a crescent bulging
  left, its outer edge bitten into three points, in a soft glow): shuriken thrown from the actor's half — a first
  volley in flight at frame 0, then chains of three or four to frame ~12 — flying right at 75-95 a frame, spinning,
  some tumbling (flattened), white on the left, cream past x 200, gold past 300, the last gone by ~22; at the throws
  olive sprays with small white ticks, and tall pale "(" arcs standing in rows and fading (1-19); over the target's
  half a mesh of faint thin needles with bright heads, most running down to the right, spreading left from x 440 to
  280 (3-27); gold crescents in a glow, small first, larger later (9-27), and teal four-point sparkles in a glow with
  small white ones (10-27). Timed on the reference's mean brightness in four column quarters. Over the battlefield
  the shuriken and the crescents read, the needles hardly.
  The Vanish Bomb effect (`VanishBombEffect`, 2026-10-15, the owner took its v1 as it is; v2 larger after the owner's
  note, which the owner took as it is; after Sengoku Rance's Begone Bomb, 40560 — 29 frames in
  the archive; a texture of its own, `Rim`: a ring with a sharp inner edge and a frayed outer one): a wall of white
  light — lumps and four-point sparkles, pink past x 430, fewer toward the right edge — lit from the frame's edges
  inward as the hole in it closes on (295, 185) (its half-width 400 at frame 0, 200 at 4, 0 at 8), each lump dimming
  after it lights. Then, larger than Sengoku Rance's (owner, 2026-10-15: v1 looked weaker than Rance Attack, while
  Miki's sprite, CG 1529, is a huge bomb, white and a little blue — by the lore the mightiest blow of all): a white flash there
  swelling over the whole frame (8.4-13.8) and a white-out of the whole frame (9.3-11.7, after a brief violet haze);
  an ice-white sphere with a bright rim and a white core swelling from it past the frame's height (radius 230 at 9.5,
  330 at 10.5, 400 at 13.5; 8.5-15.5), throwing 130 long, wide ice spikes off its edge (10-15.5), and the flash
  flattening into a white ellipse (12-16); the blast's blue glow over the whole frame (10-21); then a thick flat ice-white
  rim spreading from it far past the frame's edges (half-width 290 at 14, 680 at 20, 880 at 24; nearly half as high),
  the blue shock wave just behind it in two rings (14-23), blurring into a broad soft grey band as it fades (16-24),
  a grey glow inside it, long rays from the centre (up to 560) and a star there (12.5-27), and inside it cyan, gold and
  white star bursts, orange motes and steel-blue flecks drifting out with it (13.5-25). The gathering light was timed on
  the reference's mean brightness in four column quarters; the blast is ours. No green (owner, 2026-10-15: the bomb on
  Miki's sprite is white and a little blue; v2's glow, shock wave and rim were green and mint, its flecks teal). The
  positions here are as laid out; it is drawn 70 units left and 70 up of them, onto her bomb (owner).
  The C. War Dance effect (`WarDanceEffect`, 2026-10-15, the owner took its v1 as it is, then asked for a third fewer
  crescents and wisps — v2, which the owner took as it is; after Sengoku Rance's Ieyasu's
  Special Move, 47400 — 29 frames of it in the archive, the 30th a stray picture of something else; a texture of its
  own, `Sickle`: a crescent filled evenly with sharp edges, its hollow facing left): rows of flat crescents bulging
  right, each lit for one frame and a new set every frame (as the reference's, none lasting into the next), added one
  over another so two make the next shade and three white — salmon to x ~430, grey by ~540, then grey turning violet
  (frames 5-9) and plum late (14-22); larger and more often wide toward the right; their front running right from the
  middle to the edge (x 390 at frame 2, 560 at 3, 830 by 8), most at the peak (frames 8-9, 48 a frame), crowding left
  early and evening out late, their left edge drawing back to the right from frame 18 (x 300) to 25 (700); behind them
  faint streaky wisps (the puff stretched flat) drifting right, a few frames each, salmon and then ash (from 19), the
  last fading by ~27. Timed on the reference's mean brightness in four column quarters. The positions here are as laid
  out; it is drawn 150 units right of them (owner, 2026-10-15).
  The Chuhai effect (`ChuhaiEffect`, 2026-10-15, the owner took its v1 as it is; after Sengoku Rance's Shogun Whirlwind,
  47060 — 29 frames in the archive; textures of its own: `Hook`, a thin arc thickening to a bright head, and `Kite`, a
  soft diamond): a whirlwind, a wheel of blue blades turning clockwise — swelling from a dome at the frame's foot in the
  middle (radius 40 at frame 0, 210 at 4, 245 at 8) while its centre climbs and drifts right, (390, 340) at 0, (410,
  120) at 8, (590, 30) at 12, off the top right by the end (radius 300); over the wheel thin hooks with bright heads,
  their chords rising to the right, and soft blue kites, each lit for a frame or two and moving with the turn, the eye
  hardly emptier; a white sector of the rim, hooks piled on a mist, faint at first and full from frame 6, sweeping from
  the right (to 6) down round the bottom (12) to the lower left (from 16), about ±90° wide at frame 8 and ±60° by 16;
  grey-teal haze just outside the rim; the whole dying from frame 20, gone by ~27. Timed on the reference's mean
  brightness in four column quarters. Over the sand battlefield the white sector reads strongly, the blue blades faintly.
  The T2 Goliath effect (`GoliathEffect`, 2026-10-15, the owner took its v1 as it is; after Sengoku Rance's Gigai Punch, 46820 — 14 frames in the
  archive, 46834 absent, and 46839 on another effect's; **15 frames as the reference**, the owner's choice, the rest of
  ours empty; its size the reference's; a texture of its own, `Claw`, a solid blade from a thin root to a sharp point,
  widest three quarters along, bowed a little): four white claws raking down and right over the enemy's half, from
  roots along the top (x 410-490) to points at the foot (x 570-730), each lit for a frame and longer to frame 3 (35 % of
  its length at 0, 60 % at 1, 85 % at 2), thin at first; their roots warming to short orange fingers from frame 2; a
  white flash where they cross (555, 185): a starburst at frame 1, a white mass to 4, a pale yellow core at 5; the
  claws turning yellow at 4-5 and softening into strokes (soft ellipses along them) from 5, orange to red by 7, dark red
  by 9, crossed from frame 4 by a long slash rising to the right (and a shorter one right of it) a step redder; a fan of
  thin gold rays thrown from the flash up and to the left (frames 3-9, between 188° and 288°), cooling to embers, with
  four-point sparkles among them, and short orange embers strewn left of the flash (6-12); on the left a beige dust
  cloud rising from the bottom (x 100-220) and drifting up and left, swelling, gone by 12, with a fine grain at the
  bottom left. Timed on the reference's mean brightness in four column quarters. Over the sand battlefield the white
  claws read strongly, the orange strokes faintly.
  The Kentarou-kun Slash effect (`KentarouSlashEffect`, 2026-10-15, the owner took it as it is after four rounds of notes; after Sengoku Rance's own move of Kentarou's,
  40800 — 29 frames, 40829 on another effect's; in the white and mint of Kentarou's sprite, CG 1222's 210/255/245,
  160/215/205 and 100/175/165, where the reference is blue): a blast thrown right across the field from a white-hot
  core at (410, 160) — where Kentarou's sprite strikes the ground (CG 1222: his figure at x 229-299 of the scene, the
  eruption at 300-400; owner, 2026-10-15: the reference's core, at 265, stood behind his back), the reference's right
  half compressed to fit between it and the edge, and the whole laid out so moved 20 units right (owner; the
  positions here are before that shift); the core's glow and face a white mass to a bowed edge at x ~470 (0-4); a domed
  shock front (the `Shock` arc) bursting from it at frame 3, a grey needle ahead of it, and running right (x 600 at 4,
  670 at 6, 735 at 8, 800 at 10), filled white behind while it leaves the core and throwing sparks; a white mass
  filling behind it and dying by place — the middle by frame ~8-9, the actor's half by ~11-13, the right edge last,
  by ~15; pale teal crescents (the `Moon`, its hollow facing left, owner, 2026-10-15; the large ones alone, 110-150 high, the
  small ones left out, owner) flying right through it (frames
  5-17); a column of soft teal bars standing just right of the core (x 455-545, frames 7-16) with a band of fine mint
  specks drifting right through it; sand-coloured dust over the target's half (9-26). Nothing is drawn behind
  Kentarou (owner, 2026-10-15): the reference's spikes fanning back to the left from the core (frames 0-9), its
  cluster of motes and its dust on the actor's half are left out, and the core's billows start right of his figure
  (x 380). Timed on the reference's mean brightness in four column quarters before the core was moved and the spikes
  left out. Over the battlefield it whites out the scene for
  frames ~4-11, as the reference does on black.
  The T2 Soaring Wave effect (`SoaringWaveEffect`, 2026-10-15, the owner took its v2 as it is; ours, no counterpart in Sengoku Rance: the owner's design —
  the four arcs Rolex's sprite shows on the ground growing a little and running at the enemy, hurting it as they hit;
  in the blues of his sprite, CG 1252's 215/250/255, 195/240/255, 175/205/235, 115/165/195 and 85/140/175, its dust
  215/190/195; two textures of its own, `SurgeBody`, a quarter arc rising from its foot at the right to a thin tip at
  the top left, its hollow facing left, thickest near the foot and faintly streaked, and `SurgeEdge`, its bright
  crest): the four waves where CG 1252 shows them (its pixel bounds: middles (360, 194), (400, 175), (444, 225),
  (507, 206), 75-88 wide and 112-150 high; Rolex's figure stands at x 5-175 of the scene, left of them all), drawn
  1.3× as wide with their feet where the sprite's are and their bodies twice over, light blue with less red and the
  sprite's mid blue, so that over the sand they read nearly solid and blue (owner, 2026-10-15: less see-through,
  a little wider), light up with a glow (frames 0-2), then run right at ~30 units a frame, each a little faster or slower, growing to 1.3× with
  their feet on the ground, two fainter copies trailing them; droplets of spray thrown up and back from their feet and
  falling, pink spikes of dust kicked up behind them with a low haze of it; as each one's front runs through the
  enemy's ranks (x 535-780) a burst every 55-80 units, two or three up its height: a white glow, a `Burst`, a small
  shock front pushed right, white shards and droplets thrown forward and up; past the ranks they break up into a blue
  mist (from ~10), gone by ~20. Nothing is drawn behind Rolex. Over the sand battlefield the bursts read faintly over
  the white armour of Ex's army.
  The Guard effect (`GuardEffect`, 2026-10-20, a Defend's; after Sengoku Rance's guard actions' 45080 — 14 frames in
  the archive, the 15th absent; **14 frames as the reference**, the rest of ours empty; a texture of its own, `Tile`, a
  flat rectangle with barely softened edges), all on the actor's half: a curtain of 90 rows of horizontal blue-violet
  streaks (`Streak`, 0.36/0.36/0.86, a fifth of them with a lavender-white core), the full height of the frame,
  sweeping in from the left edge (its front at x 150, 240, 330 at frames 0-2) and stopping at the middle, x 400 (from
  frame 3), its tail trailing back 190-280 units and dimming once it has stopped; there its front stands as a wall of
  light (3-10) — a soft lavender band widening from 16 to 70 units, blocks stacked down the face from frame 5, lavender
  turning pinkish white toward the foot, a faint straight edge at the face —; the wall then breaks up pixelated
  (10-13): a grid of blocks from the face back to the left (its left end at x 310, 200, 70, 0, each row's a little
  uneven; the blocks 26, 40, 54, 66 units, some wider), brighter toward the face and the foot, blue-lavender, then blue,
  then dark navy, gone by 14; nothing passes the face. Timed on the reference's mean brightness in twenty-unit column
  bands. Its multi-target variant (49060, 152×78, a card's size) is not used: a Defend is one unit's. Over the sand
  battlefield the curtain runs through the defender's ranks and the wall stands at their front, faint under screen
  blending but for the wall's peak (frames 9-11).
  **Debug → Effects** (`EffectViewer`) plays them over our scene's battlefield (CG 900 and its overlay, with Ex's
  (commander 1) army once a game has booted) or black: the effect and its user, Play, Loop, the enemy acting, the
  blend, the speed, the seed, a frame slider and an offset (owner, 2026-10-15): X and Y move the effect from its own
  place, in the design space (800 × 314, +x toward the target, +y down; `EffectPlayer.Offset`), to try another place
  before it is built in — Reset puts it back; the effect stands at the scene's foot (y 8). On the player's side
  stands Rance's (unit 1) army for melee and magic and Isoroku's (unit 27) archers for the bow (owner, 2026-10-15;
  once a game has booted) and for a special the sprite its user
  shows while using it (owner, 2026-10-15; `FIGT.ADV:1660-1713`, docs/battle/02-resolution.md §4): the user list holds
  the special's users — Rance Attack Rance (unit 1, CG 1520), Kentarou-kun Slash Kentarou (unit 31, 1222), the white beams one each, Shizuka's
  (unit 17, 1522), Magic's (unit 39, 1523) and Gandhi's EPO Light (unit 37, 1524), the dark ones Chizuko's
  (unit 38, 1526) and Nagi's (unit 40, 1525), Whirlwind Shot Isoroku (unit 27, 1528), High Speed Meglass (unit 30, 1527), By la Way Rick (unit 7,
  1521), the ninja's follow-up Kanami, Freya, Kaoru or Wichita (their own sprites, 1164, 1176, 1182, 1185), Vanish Bomb
  Miki (unit 32, 1529), C. War Dance Patton (unit 18, 1530), Chuhai Asuka (unit 11, 1121), T2 Goliath none (record 51,
  no sprite: the battlefield bare on the player's side, as in the battle), T2 Soaring Wave Rolex (unit 47, 1252);
  Guard has Rance's army, as melee.
  `--effects --shots DIR --quit` (in a window; `--effects white-beam-magic,black-beam-nagi` for those alone) writes every frame of each with seed 1: the effect alone on black
  (`melee-NN.png`, `bow-NN.png`, `magic-NN.png`, `ice-magic-NN.png`, `lightning-magic-NN.png`, `light-magic-NN.png`,
  `poison-magic-NN.png`, `rance-attack-NN.png`, `kentarou-slash-NN.png`, `white-beam-shizuka-NN.png`, `white-beam-magic-NN.png`, `epo-light-NN.png`, `whirlwind-shot-NN.png`, `high-speed-NN.png`, `by-la-way-NN.png`, `ninja-NN.png`, `vanish-bomb-NN.png`, `war-dance-NN.png`, `chuhai-NN.png`, `goliath-NN.png`, `soaring-wave-NN.png`, `guard-NN.png`; the black beams' (`black-beam-chizuko`,
  `black-beam-nagi`) and the dark magic's (`dark-magic`) picture is its light, so they are saved over the battlefield only) and over the battlefield with the effect's first user, the
  player acting and the enemy (`melee-scene-NN.png`, `melee-enemy-scene-NN.png`, …) — for a sheet next to the reference's frames, made outside the
  repository.
- **The windows over it** ("vs", the results, the info boxes, the
  prompt, the popups, the script menus) are the stage's screen as the original draws it, shown only
  inside the windows' own shapes (their frame CGs, the selection boxes): the 640×400 screen fitted to
  the scene's height (×1.025, centred). The stage keeps drawing the original's screen unchanged —
  Option → Interface → Original shows it as today.
- **The input**: `FIGT_SEL` is answered by our own command input on the cards (step 4, "Next — our own
  command input", built 2026-10-13); the script menus and the key waits stay the script's.

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
   strip; the cards 282×150 (faces through `FacePictures`, the acting one framed — since 2026-10-13 with Sengoku Rance's fill and blinking
   frame; the acted ones' faces
   were dimmed until the owner's request of 2026-10-13); the rounds gauge; the armies' names (`BattleBoard.Armies`). Input as before: the
   pointer over a card is the script's pointer at its panel's centre (`BattleBoard.PanelCentre`) and
   focuses the scene on it, elsewhere the point of the windows' layer under it. From a round header
   until the round's first turn the cards, as the strip, show the states the round's reset will leave
   (`RoundOrder.States`; GameState still holds the last round's).
   ✓ 2026-10-12 the strip moves (owner): `godot/Battles/OrderStrip`, on its own while the game goes on.
   When damage re-sorts the waiting units, the chip that jumps ahead (out of the longest run kept in
   order; among runs as long, the one that moved left) drops a row, slides to its place while the chips
   it passes make room, and rises into it — 1 s by default. Since 2026-10-16 (owner; the strip over the scene's
   foot) it rises a row into the scene instead, and drops into its place. A new order
   or turn while chips move makes them finish within 0.15 s, then the next move starts; under MsgSkip
   the chips stand at their places at once, and so when a new round (or battle) begins (owner: the
   units that acted go back into the order without jumps).
   ✓ 2026-10-12 Option → Battle Screen… (owner: a window of sliders), `godot/Battles/BattleScreenSettingsDialog`:
   the strip's move (0-3 s, 0 at once), its hurry (0-1 s) and the share of the drop and the rise
   (5-45 %), applied and saved (`[battle_screen]` in `user://settings.cfg`, `BattleScreenSettings`) as a
   slider moves, with a preview of the strip; the game goes on behind it. `--strip-seconds x` for a
   launch.
   ✓ 2026-10-13 the frame time (owner: the exchange's flying numbers, 10 frames each followed by
   `Y 3, 1`, ran several times slower in New than in Original). A frame whose screen changed cost
   ~53 ms in New against ~16.5 in Original (the week-18 look, window, `--fast`); now ~11 ms (median;
   the 10 % slowest, ~40 ms, when the field's picture changes or a window is open). The view under our
   screen is neither composed nor uploaded (`GamePresenter.TakeChange`; `BattleBoardView` composes for
   its windows' layer only while a window is open, the layer hidden when none is); the scene reads only
   its 640×256 (`Surface.ReadArea`); the sides are blurred only when the picture under the numbers
   changes (`BattleScene.PictureVersion`, blurred from that picture, so without the numbers); the
   windows' layer reads only the rows its windows span.
   Fixed 2026-10-13 (the owner saw the banners come late when the pointer moved from one enemy card to
   another): a hover cost ~145 ms in a debug build — the scene drew its whole picture again (~58 ms),
   the view read and uploaded every layer and blurred the sides (~78 ms), the eight cards were built
   again (~8 ms). Now the scene draws only the layer that changed (a hovered enemy: its army's layer;
   the palette chained as the original loads it, the digit sheets drawn once, the whole picture put
   together only for the tests, `BattleScene.ComposeWhole`), the view reads only the layers drawn again
   (all of them when the palette changes; `BattleScene.FieldVersion` and the like) and blurs the sides
   from the battlefield alone, and a hover changes only the cards' frames: ~2 ms + ~14 ms.
4. **Input**: our `IBattleInput` answers — the target cards' commands, the acting card's, the
   own cards' orders and info; the script menus as dialogs.
   ✓ 2026-10-13 `FIGT_SEL`'s popups over the cards (owner: they opened where the original's panels
   are): the script still opens the slot popup and the command popup at its pointer (on a card, the
   centre of the original's panel); `BattleBoardView` shows the popup's box, read from the stage's
   screen as drawn (its hover too), where the pointer was when it opened (the box at the pointer less
   (16, 10), as the original, kept on the screen) and leaves it out of the windows' layer; while it is
   open, a pointer on it is the popup's script point, off it a corner no popup covers (so a card's
   panel centre under the original's box cannot pick an item). `--auto --interface new` in a window
   opens the command popup on the target's card once, hovers its first item, cancels it and attacks
   by a click (dumps `popup`, `popup-hover`). Superseded the same day by our own command input (below):
   in New the original's popups are not shown.
   **Next — our own command input (owner, 2026-10-13; agreed on mock-ups, built the same day).** In New,
   `FIGT_SEL` is answered without `FigtSelScreen` and without the original's popups: our own screen
   builds its choices from the `FigtSelMenu` the core passes (`Options` with each command's allowed
   arguments; `Allows`) and returns a `FigtSelChoice` — the rules stay the core's. The prompt box
   ("Attack who? [Direct Attack]", CG 861) is not shown at all.
   - **Attacks only through banners on the enemy's card**, shown only on the card under the pointer
     while the player chooses (never on several cards). One banner per attack the acting unit may make
     on that slot (`Find(mode).Arguments` holds its button 4-7): 1 to 3 — the unit's own attack type
     first (Bow for bowmen, Magic for mages, Direct for melee: `AttackType + 1`), then Direct, then its
     special (one at most per unit: Rance Attack, WD Beam …, only with focus). None: no banners (a melee
     unit in the back row; the card stays grey). A click on a banner attacks; a click on the card off the
     banners does nothing (no default attack). Labels: the script's own lines (`FigtSelScreen.ModeEntry`).
   - **The banners' look (as Sengoku Rance's):** a pennant — the left end cut aslant, the right end an
     arrow point towards the target's face; 200×40 layout pixels, 5 apart, all in one column (no
     stagger), centred on the card's height, 22 px out of the card's left edge (never over the rounds
     gauge); a vertical gradient fill about 80 % opaque (the card shows faintly through), a white
     outline, a soft shadow, white text with a dark outline. All yellow-orange (top (255, 228, 90),
     bottom (226, 128, 20)); the one under the pointer orange-red (top (255, 170, 90), bottom
     (196, 70, 30)) with a light glow.
   - **The target's frame (owner, 2026-10-16; as Sengoku Rance's):** the enemy card whose banners show has a cyan
     bracket (70, 205, 255) on each of its corners, never a whole ring (owner), under the banners: an L bent round a
     corner of 7, its arms 60 layout pixels along the card and 46 down it, 3 out of the card's edge, 4 wide, a white
     line inside and a cyan glow (`BattleCommandLayer.DrawTargetFrame`). Fixed colours, never the palette's
     accent, which marks the acting card. An enemy card without banners gets none.
   - **Context menus of our own** (styled like the cards, the palette's colours and font), opened by a
     right click on a card at the pointer, kept on the screen: on an enemy card "Info" (54); on the
     acting unit's card Defend (4), Wait (5), the items without a target (Capsule 30/31, Fog Card 32)
     when offered, Auto-Atk (8), Auto-Def (9), Info (53), Retreat (6, the whole army; its "Retreat /
     Cease Retreat" confirmation stays the original's window for now); on another own card Manual (50),
     Auto-Atk (51), Auto-Def (52) — the current order marked — and Info (53). A left click on an item
     chooses it; a left click outside or Esc closes the menu (back to choosing); a right click on another
     card closes it and opens that card's menu at once; a right click elsewhere closes it. Only items the
     core allows (`Allows`) are listed. The Info screens stay the original's windows for now. (Since 2026-10-15 no
     menu offers Info and an enemy's card has none: every card shows its stats, "The cards' stats" above.)
   - `--auto --interface new` in a window exercises it once a launch (replacing today's popup run):
     hover the target's card (banners), click a banner; a right click on an own card (menu dump), a
     left click outside to close it.

   ✓ 2026-10-13 built as above. The model is `Original.Content/Battles/BattleCommands`
   (`Banners(target)`, `ContextMenu(place, order)`; labels are the script's lines): the commands without a
   target (Defend, Wait, the items, Auto-Atk, Auto-Def, Retreat) answer 99, as a right click off the
   panels does in the original (Q-18 then binds no commander). The original's slot popup on the acting
   unit itself is not offered as such: its Auto-Atk / Auto-Def become 8 / 9 in the core (`OrderOtherAsync`),
   which the acting unit's menu gives, and its Manual only shows a message. `BattleCommandsTests` builds the
   core's own menus and checks that every `FIGT_SEL` answer of the golden captures is offered on a card
   (`GoldenReplay.RunAsync`'s `commands` observer). Godot: `godot/Battles/BattleCommandLayer` (drawn over the
   cards, `BattleBoardView.Commands`) — the banners on the enemy card under the pointer (or whose banners it
   is on; the scene shows that enemy), lit cards only where something can be done (an enemy card without
   banners stays grey), the menus at the pointer; `GameScreen.SelectCommandAsync` answers with it while our
   screen shows, and falls back to `FigtSelScreen` when the interface changes to Original while the player
   chooses (switching to New while the original's prompt is open leaves that choice the original's). The
   exercise also dumps another own card's menu (`menu-orders`, its order marked) and, since the week-18
   party is all melee without focus, two sample menus never answered (`sample-2-banners`: Rance Attack
   added; `sample-3-banners`: Bow and Whirlwind Shot added) for the banners' look.
   Fixed 2026-10-13 (owner): the banners keep their places whatever their number — the first at the top
   of a column of three centred on the card. A menu item is chosen at the left button's release on it, as
   the original's popups; a banner at the press, as the original's click on a target. The Info boxes
   closed at once in both interfaces: the core had left out `FIGT.ADV`'s release loops (`IM` while a
   button is down, before and after the box's key wait; also `FIGT_B`'s before its menus and after an
   order's message), so the key wait took the click that chose Info. They are
   `IBattleInput.WaitForReleaseAsync` now (the game reads the mouse as `IM`; replays and tests find no
   button down), and the answering click is read off the input when `FIGT_SEL` returns, as the script's
   own `IM` polls take it.
   Renamed 2026-10-13 (owner): on our screen the Direct banner reads "Melee" (`BattleCommands.MeleeName`) —
   the close-combat exchange, front row to front row, which the enemy answers; the script's "Direct" pairs
   with "Indirect" (the bow). The original's windows (the prompt, the exchange's "Direct Attack") keep the
   script's text.

   ✓ 2026-10-13 the script menus (owner, mock-up B): only the retreat confirmation (`FIGT.ADV:571-580`)
   opens over our screen — `FIGT_B`'s Intercept / Ninja Scout / Abandon City and the ninja come before the
   field is drawn, on the original's screen, and stay the original's (the strategic map's interface,
   later). On our screen it is our dialog (`Original.Content/Battles/BattleDialog`, `godot/Battles/BattleDialogLayer`):
   the screen dimmed, a box centred over the scene in the context menus' style, our question "Retreat the
   whole army?" and "The battle is lost." (muted) over the script's items, Retreat in the palette's warning
   colour. A left release on an item chooses it; Esc, a right press or a left press off the box cancels
   (no code: Cease Retreat, as a right click on the script's menu). The other menus, and a dialog open when
   the interface changes, are the script's (`BattleMenuScreen`). `--auto --interface new` in a window
   chooses Retreat from the acting unit's menu on its first turn, cancels the dialog (dump `retreat`) and
   goes on to the banners in the same turn.
5. **The scene and the exchange**: the engagement's pictures, the shots and counters, the action
   labels, the wiped-out marks.
6. **Windows**: the round header, the flavour lines, the results, the info boxes, the strategy
   screen, the scout report — as the owner decides in question 3. ✓ 2026-10-14 the scout report
   ("The scout report" above).
   The owner's answers (2026-10-15): (1) the **info boxes** (Info on an own or an enemy card: `UnitInfoEvent` /
   `EnemyInfoEvent`, window 143, `FIGT.ADV:834-1119`, docs/battle/screen-layout.md §6.1) become ours — mock-ups
   first, 2-3 at 1280×800 over a real battle screen with the info's real values (both sides: a unit with troops,
   one without, an enemy commander); candidates: our panel over the scene in the cards' / scout report's style,
   closed by a click (the script's one key wait), an expanded card beside the unit's card as Sengoku Rance's
   details, or the original's window as now — then (owner, the same day) none of them: the stats on every card, always,
   under the portrait ("The cards' stats" above; ✓ 2026-10-15); (2) the **order messages** (`L_0129e`, `FIGT.ADV:776-833`) are not
   shown, the order's label on the card flashes instead ("The order messages" above; ✓ 2026-10-15); (3) the **strategy screen**
   stays the original's for now — later the same day our cut-in ("The strategy's cut-in" above; ✓ 2026-10-15); (4) the
   **other script windows** during a battle (111/112/121/122/113/131) stay the original's for now — in docs/backlog.md,
   "Interface".
7. **Checks**: the model's tests, the golden captures replayed through our input (the same
   choices give the same memory), `--auto --interface new` through the battles, the owner's look
   from a kept save (`--until battle`).

   ✓ 2026-10-15: `BattleInputReplayTests` replays every captured battle (`BattleSceneTests.Captures`) with the answers
   our screen gives (`GoldenReplay.RunAsync`'s `ReplayAnswers`): each `FIGT_SEL` answer is the banner's or the card menu
   item's choice from `BattleCommands` (Auto-Atk / Auto-Def on the acting unit's slot as its menu's 8 / 9, the commands
   without a target with 99), the retreat confirmation our dialog's, and the key waits our screen answers itself (the
   "vs" window, an order's message, a later round's header, the scout report's lines, a result without a wiped-out mark)
   a click's key, 16 (0 under the message skip). In the full run all 110 captures match the original: 277 answers, 77
   of them given otherwise by ours, 464 key waits ours (10 where the original's player pressed another key). Kept as
   captured, since our screen cannot give them: Info (53/54, every card shows the stats), Manual on the acting unit (50),
   and a command without a target given with the pointer on an enemy's panel — the original then rebinds `Cmdr_Cur`
   (BFL-19), which ours never does, as the original's with the pointer elsewhere; one captured retreat differs so at the
   exit. `--newgame --auto --fast --quit --seed 2 --weeks 60 --stage 2 --interface new` (headless) plays stage 1 through
   19 battles to Oaks (6) in week 34 and stops in week 36, as on the original's screen. The look: the week-18 attack on
   Oaks (6) in a window (`--until battle --interface new`).
