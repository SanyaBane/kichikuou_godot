# Sengoku Rance's battle — a reference survey (2026-10-12)

The owner chose to build our own battle screen for the whole battle "in the manner of Sengoku
Rance" (AliceSoft, 2006, System 4) and asked for its battle system understood fully, from the
game's code. This folder is that survey. It is **reference material for our interface only**: the
port's battle keeps Kichikuou's rules one to one (docs/battle/); nothing here is a rule of ours.

## The parts

| Part | Covers |
|---|---|
| [01-flow.md](01-flow.md) | the conventions of the four parts; set-up and placement, the turn queue and the name strip, action points, the time gauge, the 戦果 (advantage) bar, the end of a battle and what follows, the enemy AI |
| [02-resolution.md](02-resolution.md) | what a unit is, the 22 troop types, the damage formulas (physical, magic, troop scaling, counterattack), guard, battle-wide modifiers, healing, buffs, the commanders after a battle |
| [03-skills.md](03-skills.md) | the four command slots and the passive, the action table's fields, how an action executes, states and buffs, the per-target command menu, every action and passive |
| [04-screen.md](04-screen.md) | the screen (800×600): regions and layers, the troops on stage, the top bar, the name strip, the damage counters, the unit cards, the time gauge, the command menu on the hovered card, the input, each kind of action step by step |

Each part ends with its open questions and the oddities of Sengoku Rance's code it met (not part
of [../quirks.md](../quirks.md), which registers the original Kichikuou's).

## Sources and how to make them again

The MangaGamer English release (`SENGOKU_RANCE_GAME_DIR` in `local.env`) keeps its code in
`Rance7.ain`. alice-tools 0.13.0 (`KICHIKUOU_ALICE_TOOLS_RELEASE_DIR`) has no decompiler, so the
survey reads its assembly dump, one file per original source, under `SENGOKU_RANCE_EXTRACT_DIR`:

- `alice ain dump -c -o ain\code.jam Rance7.ain` — the code; `-t` → `ain\text.txt` (strings and
  messages by function), `-S` → `structures.txt`, `-g` → `globals.txt`, `-f` → `functions.txt`;
- `python -I tools/sengoku/split_jam.py ain\code.jam ain\files` — one `.jam` per source
  (`あべ/T_BATTLE.JAF.jam` …), the files the parts cite with their line numbers;
- `python -I tools/sengoku/effect_frames.py <alice.exe> <Rance7GA.ald> <out> 40000 40260 40360` — the battle
  effects' frames and contact sheets (04-screen.md §2.4.1), into a folder outside the repository.

The battle is mostly Abe's (`あべ/`): `C_合戦`, `C_合戦敵` (the AI), `C_順番` (the queue), `T_BAT*`
(time, bar, selection, numbers, cards …), `T_UNI*`; the data and the action table are Tada's
(`多田/▲技能データ`, `▲武将データ`, `関数／本編`). Nothing of the game — code, text, pictures — enters
the repository.

## The battle in brief

- **Sides and places.** Up to six units a side, two columns of three: places 1-3 the front row
  (the column nearer the centre), 4-6 the back row. The player places the commanders before the
  battle. When a side's front row is all destroyed, its back row moves forward. A unit is a
  commander with troops; the troops are its HP. (01-flow.md §1, 02-resolution.md §1)
- **Turns.** No rounds and no alternating sides: a charge-time queue. After acting a unit waits a
  delay (its troop type's base, its speed, the action's own weight); the name strip above the cards
  is that queue, the next actor first. The player never picks who acts, only what the unit whose
  turn it is does — and command and target in one click, on the target's card. (01-flow.md §2)
- **Action points** — the squares under a card's troops: up to six, refilled only at the start of
  a battle; an action costs 1-3, a special all that remain; a unit at 0 leaves the queue but stays
  a target. (01-flow.md §2.5)
- **Time and advantage.** The vertical gauge in the centre is the battle's time: 24 steps, one per
  action (Wait too). The horizontal bar on top is 戦果: each side's share of the casualties
  inflicted plus bonuses (facilities defended, units destroyed, some skills). Destroying the other
  side wins at once; when time runs out (or nobody has action points left) the larger 戦果 wins,
  a tie goes to the defender. (01-flow.md §3-4)
- **Damage** has no random spread and no criticals: the troop type's attack and the target's
  vulnerability, attack ×10 against defence ×8 (intelligence for magic), scaled by the attacker's
  troops (full up to 500, half to 2000, a quarter beyond), the action's percentage, the battlefield
  bonus, halved by a guard. Melee needs both units in their front rows; bows, cavalry and some
  magic reach anyone. A counterattack from the target's troops comes first, except against ranged
  and support types. (02-resolution.md)
- **Commands.** Four slots and a passive per unit: slot 2 is always Wait, slot 3 the troop type's
  basic attack, the others the commander's skills (217 actions in all; "Miko Storm 2" is another
  action, not a level). Four buffs (attack, defence, intelligence, speed) last the whole battle.
  (03-skills.md)
- **The screen.** A scene on top with at most two units (the actor and its target), soldiers drawn
  as a fixed formation per troop type; the name strip; the cards (face, name, troops "now/max",
  the six squares, the troop-type icon, buffs, frames for hovered, acting and destroyed); the time
  gauge between the sides. Hovering a card shows the commands that can reach it; hovering a
  command shows its cost on the squares, where the actor goes in the queue and whom a multi-target
  action hits. Nothing waits for a click during a battle but the choice of command.
  (04-screen.md)

## Against Kichikuou's battle

What our screen has to show is Kichikuou's battle (docs/battle/), which differs:

| | Sengoku Rance | Kichikuou |
|---|---|---|
| Units a side | 6 (two rows of three) | 4 (places 1-2 front, 3-4 back) |
| Who acts | a charge-time queue by delays | every unit once a round, fewest troops first, re-evaluated after each action (docs/battle/01-flow.md §6) |
| Battle length | 24 actions on the time gauge | 4-5 rounds (3 against an enemy with retreat mode 6); time out: the attacker loses |
| Points | action points (squares), spent per action | none; Focus (Wait) builds up for the specials |
| Winner at time out | the larger 戦果 | the defender |
| HP | troops are the HP | troops and the commander's HP are separate |
| Pre-battle | placement | the strategy roll (±10 / ±50 % troop damage, docs/battle/01-flow.md §4) |
| Waits for a click | only the command | the round header, the "vs" window (BSC-07), the flavour lines, the results |
