# The damage forecast at the attack banners — survey and mock-ups (2026-10-26)

The owner's request (2026-10-26): an optional setting that shows, beside a target's attack banners (the gallery's
`banners-E1`), what the attack is likely to do, as some turn-based games do (Heroes of Might and Magic V). This document
is the survey and mock-up rounds A and B; nothing is built. The owner chose A1 for the PC, the damage dealt alone (§10).
**Status: round B drawn, awaiting the owner's choice of its layout** (§10).

## 1. What an attack does

The rules are docs/battle/02-resolution.md, §6-7; in short:

- A banner's attack is one **exchange**: each side runs up to four streams — troops → troops, troops → commander,
  commander → troops, commander → commander — computed at once (`ComputeExchange`), then applied together
  (`ApplyExchange`). A loss is counted in **hits**: one hit kills one soldier or takes 1 HP.
- **Who answers** (§2.4): Melee (Direct) and T2 Soaring Wave are countered by the target's troops and commander in the
  same exchange; Bow, Magic and every other special are not.
- **A fall:** a commander at HP 0 is out with all his troops (`Cmdr_Status = 0`); one of ours at HP 0 plays
  `FIGTﾐｶDE` — for Rance (unit 1) after the prologue a game over (docs/battle/01-flow.md, §10).
- **The hit chance** of a stream: `(atk − def) × 8` when the attack is higher, else `8 + atk − def`; plus our unit's
  level (minus it for the enemy's streams), minus the defender's slot state (1 acted, 20 the AI defending, 2-21 ours
  defending), minus `2 × wall bonus` for troops on troops at a city; 1-100 % (§6.1, §6.10).
- **Where the dice are:** the troops' hits are the expected count spread uniformly over `[E − 2k + 1, E + k]`, `k = E / 10`
  (±1 below 11; Q5); 80 shots or fewer roll d100 each (binomial); a commander's shots on troops are themselves random
  (`Attacks / 2 + 1` to `Attacks × 1.5`), on a commander 1-6; a partner on the field adds +0/+1/+2 to the troops' attack
  (or defence) at random; C. War Dance and Chuhai roll their size. The strategy's ±10 % / ±50 % and the clamps (no more
  dead than engaged) come after.

So a forecast needs the whole exchange, not one formula: both sides, every stream, the clamps, the demon shields, the
specials — exactly the code that runs it.

## 2. Against the house style

docs/ui/style.md, §1: "Show more only by the owner's word, and only what the state already knows … no prediction of
draws". The forecast is the owner's word (this request), and it shows **the spread of the possible results**, never the
draw that will come: it runs on its own generator, never the game's (xsystem35's MT19937 is deterministic: from its state
the forecast could tell the exact result, a cheat, not a hint). What it reads is on the screen already — each card's
attack, defence, magic, modifiers and level (`BattleCardStats`), the slot states (Acted, the defence's ribbon), the walls
(`Walls +8%`), the strategy's cut-in — or the player's own (the items, the partners). So the sentence of §1 stays true
if it reads "no prediction of a draw's outcome"; to be worded with the owner when a variant is accepted (§9, question 6).

## 3. How it is computed

**On the real exchange, not a copy of its formula.** A method of the rules (`Kichikuou.Original/Combat`, say
`ExchangeForecast`) takes the state at `FIGT_SEL` — the actor, its slot and Focus are set by then (`Battle.Flow.cs:472`) —
copies it (`ScriptState.CopyTo`), sets the banner's command as its setter does (`PlayerAttackAsync`'s switch, split out so
both use it; on the copy the Focus it spends is harmless), binds the target (`Bt_EnemyActSlot`, `Cmdr_Cur`) and runs
`ComputeExchange` N times on its own `XSystem35Random` with a fixed seed: the numbers never flicker, every quirk, shield,
clamp and special is in them, and no second formula can drift from the rules. Enumerating the paths as the talk marks'
dry run does (`ScenePreview`) is no way here: a stream rolls up to 80 d100.

**The probe** (2026-10-26, a throwaway test, not committed): the capture the gallery's battle shows,
`20261010-213758-114-FIGT_A` (an attack on Oaks (6) of story stage 1), its first four `FIGT_SEL`, every offered attack on
every enemy card, 4,000 runs each — replay and all under a second. What it gives (the 5th-95th percentiles; the whole
range in brackets where it differs):

| Moment | Attack | Target's troops lost | Target falls | Counter: our troops lost | Our unit falls |
|---|---|---|---|---|---|
| Rance (unit 1), 451 troops | Melee on Ex (commander 1): 92 troops, HP 1, acted | 29-37 | 78 % | 30-38 (30-39) | 0 % |
| the same, made up: Rance with Focus | Rance Attack on Ex | 0 | 99.7 % | no counter | — |
| the same | Melee on Sakanaku (commander 6): 60 troops, HP 4 | all 60 | 30 % | 8-12 (8-13) | 0 % |
| Rick (unit 7), 773 troops | Melee on Ex | 74-92 (73-92) | 96.5 % | 30-38 (30-39) | 0 % |
| Rance, 312 troops | Melee on Ex, **defending** (state 20) | 1-3 | 41 % | 30-38 (30-39) | 0 % |
| Rick, 772 troops | Melee on Melfeis (commander 4): 188 troops | 134-177 (132-179) | 0 % | 6-9 (6-11) | 0 % |

(Our hit on Ex's troops is 15 %: the troops' attack 10 against 6 + 1, the walls −8, Ex having acted −1; theirs back
39 %. Against Ex defending the same Melee takes 1-3: the forecast shows the AI's defence plainly.)

**When:** once a turn, when our commands begin on `FIGT_SEL`'s menu — every banner of every enemy card, at most 3 × 4.
The probe's cost says N = 2,000-4,000 is cheap; the build measures it.

**What it gives:** per side the troops lost (percentiles, mean, whole range), the HP lost, the chance of a fall; for A3
the streams' hit chances. A model in `Original.Content/Battles` turns it into a banner's figures (`Figures`, the minus
U+2212).

**Tests:** the game's state and generator untouched (`Differential`); the same numbers twice; and against the rules
themselves — every exchange of the golden captures falls inside its forecast's whole range (the count of exceptions
checked, the binomial tails being possible).

## 4. What it does not see

Agent's pick: none of these is forecast; the banner shows the plain attack.

- **Miki (unit 32)**: each manual attack turns into Vanish Bomb with 1 chance in 10, again and again (Q7).
- **A ninja's follow-up** after the exchange (§7.3): Kanami, Freya, Kaoru or Wichita for Menad (unit 10), Hubert
  (unit 21), Gandhi (unit 37) or Magic (unit 39), 1 chance in 3 — a little damage more.
- **The Sieg event** (`FIGT_M.ADV:10090`): Rance against Sieg (commander 93) while `F_Sieg_State` is 2 or 3 gets Chaos
  (item 1) before the exchange; once a game.
- The enemy's turns after it: a forecast is one exchange.

## 5. What it shows

- The target's troops lost as a range; the target's **fall** as a chance (the most telling figure: a fall takes the
  whole unit); its HP lost where there is room (A2, A3).
- The **counter**: our troops lost, our unit's fall — red, and for Rance after the prologue a game over; or "No counter".
- A range is the usual 90 % of the results (5th-95th percentile) — agent's pick: the whole range of a commander's
  stream (0-30 for one with 20 attacks) says little. For the troops' streams, uniform, the two are nearly the same.
- A chance in whole percents, "<1%" and ">99%" at the ends, nothing at 0.
- Colours by the style: a loss is a fall, red, on either side; a fall's chance on a vermilion chip.

## 6. Mock-ups, round A

`tools/ui/mock_damage_forecast.py` over the gallery's `banners-E1` and `battle-turn` (lacquer and touch), the probe's
first row; made up: a Rance Attack banner beside Melee.

- **A1 — on each banner**: a second row under the label — the target's troops lost, its fall on a chip, `←` the counter
  on ours. Every attack at once, compared at a glance; but small, and the banners grow to 58 px: three stand 184 px, 38
  over the card's 146.
- **A2 — at the scene's foot, where the counters roll the result**: the hovered banner's forecast in the exchange's
  counters' places (`ExchangeCountersView`), quieter — the troops as they stand, the likely loss beside them, the HP, a
  fall on its chip — and "MELEE · LIKELY" at the foot's centre. Large, two-sided, covers no card, any number of banners;
  the place the player then watches the result roll.
- **A3 — a tooltip under the hovered banner** (as `AdvantageTip`): in words, both sides, the figures before and after,
  and the hit chances that make them ("Hit 15% (Walls −8%) · Ex's troops hit 39%"). The most explicit; covers the cards
  below while the pointer rests.
- **T1 — the touch mode**: no hover on a tablet, so each attack button of the right rail carries its own forecast under
  its label (the target chosen first, `TouchTargets`; the button tapped knowing it). 13 px is small on the tablet
  (2.6 mm); the buttons could grow.

**Agent's recommendation:** A2 on the PC (it reads at a glance, hides nothing and sits where the result comes), with
A3's hit-chance line as its tooltip if the owner wants the why; T1 on the tablet.

## 7. The setting

Option → Battle → "Damage forecast" (`BattleScreenSettings`), our interface only; applied at once. Off or on by default:
the owner's choice (§9).

## 8. The build, after the choice

1. The rules: `ExchangeForecast` (the setters split out of `PlayerAttackAsync`), with the tests of §3.
2. The model in `Original.Content/Battles` (the figures, the chances' wording), with tests.
3. Godot: the chosen view, the option, the gallery's entries (`banners-E1` with the forecast, the touch pass's turn) and
   the `--auto --interface new` exercise's dumps.
4. The owner's look in a window.

## 9. Questions for the owner

1. ~~Which: A1, A2, A3, a mix (A2 with A3 as its tooltip?) — and T1 for the tablet?~~ The PC first: A1, the damage
   dealt alone (the owner, 2026-10-26; §10). The tablet later.
2. The range: the usual 90 % (agent's pick) or the whole?
3. ~~The hit chances: shown (A3's line) or not?~~ Not shown: the damage alone (§10).
4. The option on or off by default?
5. Leave out what §4 lists (agent's pick), or mark Miki's banner ("10% Vanish Bomb")?
6. docs/ui/style.md, §1: "no prediction of draws" to read "no prediction of a draw's outcome" (§2)?
7. Melee and Bow now read the same on a banner (their streams differ only by the counter, §1): leave it, or mark the
   banners the target answers (a mark, no figure)?

## 10. The owner's choice and round B (2026-10-26)

**The owner:** the PC first. A1 — the forecast on each banner — but without the enemy's counter and without the chance
of a fall: only the damage the attack deals, to the target's troops, and separately to its commander where the attack
can reach him.

**The agent's choices** (for the owner to confirm):

- The figures are the usual 90 % of the runs (the 5th-95th percentile, §5), no minus: the banner is the attack, its
  figures the damage it deals. A range of one value is one figure ("60", "1").
- The words are the cards' own: "Trp." the troops lost, "HP" the commander's.
- The HP is never more than the commander has: Ex (commander 1) at HP 1 reads "HP 0–1", though 78 % of the runs take
  that HP (the fall's chance is not shown).
- "Where the attack can reach him": a stream that hits the commander has shots and no demon shield zeroes its damage
  (docs/battle/02-resolution.md, §6.2) — decided from the exchange, not from the runs (a rare hit is still a possible one). Else the HP's place stays
  empty, so the troops' figure never moves (style.md, §5, "A stable layout"). The troops' place likewise: Whirlwind Shot
  reads "HP 1" alone.
- Examples from the probe (§3): Rance (unit 1)'s Melee on Ex — "Trp. 29–37 · HP 0–1"; on Sakanaku (commander 6, 60 troops,
  HP 4) — "Trp. 60 · HP 1–4"; his next turn's Melee on Melfeis (commander 4), whose 300 troops are more than the battle's
  limit of 232, so no troop shoots at her — "Trp. 91–118" alone.

**Round B** (`tools/ui/mock_damage_forecast.py`; Ex with a bowman's three banners, Bow, Melee and Whirlwind Shot — made up
but Melee, their figures the real exchange's on Rance's state — and Sakanaku with his one Melee banner):

- **B1 — two rows**: the label as today (20), the damage under it (15); banners 52 px, three stand 166 px, 20 over the
  card's 146.
- **B2 — two rows within the card**: the label 17, the damage 14; banners 46 px, three stand 148 px, the card's height.
- **B3 — one row**: the label left (16), the damage right (13); banners 40 px as today. The longest names are tight:
  "T2 Soaring Wave" with both figures does not fit.

**Agent's recommendation:** B2 — the stack keeps to the card whatever the number of banners, the damage reads at a
glance under its attack, and the label is only 3 px smaller. B1 if the label must stay as it is.

What §4 lists is still not forecast (Miki's Vanish Bomb, a ninja's follow-up, Sieg's event).
