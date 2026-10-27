# Tweaks — what changes the original's play

Option → Tweaks (docs/ui/options.md, "Tweaks"; owner, 2026-10-27) holds the settings that make the game play otherwise
than the original. Everything else keeps the original's rules. A tweak that fixes a quirk is off by default
([quirks.md](quirks.md)); its code keeps the original's path and takes the other only while the switch is on.

| Tweak | Section | Default | Its doc |
|---|---|---|---|
| Damage forecast | Battle | off | [ui/damage-forecast.md](ui/damage-forecast.md) |
| New scenes first | Scenes | on | [ui/talk-marks.md](ui/talk-marks.md) (owner's decision 30) |
| Chaos and Nikkou reach demons | Battle | off | below (BRS-24, owner's decision 32) |

## Chaos and Nikkou reach demons (BRS-24) — the survey, 2026-10-27

Owner's request: Rance's or Kentarou's troops do no damage to a demon ("Fiend") commander while their commander is off
the field, his troops over the battle's limit; an optional fix in Tweaks.

### What the original does

- **The engagement** (BRS-19, `FIGT.ADV:2246-2293`, 02 §6.2): a side engages its troops up to the battle's limit
  `Bt_TroopLimit`; its commander fights in person only when all his troops fit (`Bt_PlayerAllEngaged`), and in person he
  hits the enemy commander's HP only when the enemy's engaged troops are under 10 — else their troops (`:2263-2269`).
- **The demon shield** (`FIGT.ADV:2414-2448`): an enemy commander of class 10 or 11 takes no HP damage from troops, and
  from a commander only when that unit is a demon itself (`Unit_Class` > 9) or holds Chaos (item 1) or Nikkou (item 2).
- **Together** (BRS-24): a unit that can wound a demon, its troops over the limit, cannot wound a demon commander who is
  alone or down to fewer than 10 troops — its troops are shielded, its commander stays behind them. Only specials reach
  him (they call the commander's stream whatever the engagement, 02 §3.3): Rance Attack (Rance, unit 1; Kentarou, unit 31,
  once `F_Kentarou_State` > 7) at full damage, Miki's (unit 32) Vanish Bomb for half his HP, Meglass's (unit 30) High
  Speed for 1 HP. The losses lower the unit's troops through the battle (`FIGT.ADV:1878-1879`): once they fall to the
  limit, the commander fights in person.

### Where it matters

- **Demons who fight alone** (0 troops): Kayblis (commander 135), Lei (commander 92), La Vaswald (commander 98), Babolat
  (commander 129), Silky (commander 156). Any other once his troops fall under 10: Nobunaga Oda (commander 72), La Seizel
  (commander 90), Camilla (commander 91), Sieg (commander 93), Redeye (commander 130), Galtia (commander 131), Kite
  (commander 132), Kesselring (commander 133), Medusa (commander 134), Satella (commander 157), Meglass (commander 158),
  Satella's copy before her duel (commander 100, `Gｻﾃﾗ:3393-3426`). No dungeon has a demon.
- **The limits**: the attack on the Demon Lord's Castle (79), Kayblis's, 500 (its area, `Tｹｲﾌﾞﾘｽ2:130`); Kayblis's
  Castle (84), Medusa's, 300; Osaka (56), Nobunaga's, 400; a defence in a city its area (Leazas Castle (1) 180, North (2)
  220, Maunes (13) 210, Pawn (40) 200, Grief Valley (89) 700, Laurengrad (48) 1420 at boot, each defence adding 10 %,
  BFL-03); the plains and the duels 5000 (Lei's, Kesselring's and Satella's duels fit).
- **The troops**: Rance (unit 1) has 1000 at most from the boot, raised only by purchases (10 men a time, `Cﾌﾞｶ`);
  Kentarou (unit 31) 450 (25 a time). Rance is over every city limit above and under 5000; Kentarou fits 500, not 400,
  300 or 180. The player's own demons: Satella (unit 29) 200, Meglass (unit 30) 600, Miki (unit 32) 8, Galtia (unit 41)
  0, Seizel Sisters (unit 42) 500.
- **The typical case**: Rance with 1000 troops attacks the Demon Lord's Castle (79): limit 500, Kayblis alone. His 500
  engaged shoot at Kayblis and the shield zeroes them; Rance stays behind; only Rance Attack wounds Kayblis.

### Options

- **A — on the field.** Against a demon commander, a unit that can wound him counts as all engaged even when its troops
  exceed the limit (`Bt_PlayerAllEngaged` 1, its engaged troops still the limit): its commander fights in person (his
  troops first while the demon's engaged troops are 10 or more, as the original), and the demon's troops may shoot at him
  as at any commander on the field (step d: `engaged enemy / engaged player × 2` shots, none against a lone demon). The
  scene shows him with his troops (`Unit_CgPartial`, not `CgFull`). One switch in `ComputeExchange`; nothing else reads
  the flag.
- **B — in person, out of reach.** Step b alone: he strikes, but the demon's troops never shoot at him. Against a lone
  demon the same as A; against one with troops (Medusa's 4500, 300 engaged at Kayblis's Castle (84): two shots an
  exchange) kinder to the player.
- **C — the blade's troops.** The shield lets the troops of such a unit wound the demon. Far stronger: 500 engaged troops
  shooting at Kayblis's 40 HP.
- **Who**: Chaos and Nikkou's holders only (Rance, Kentarou — the request's words), or every unit the script lets wound a
  demon (`Bt_EnemyCmdrShield` 2): the holders and the player's demons.
- **Not proposed**: the mirror for the enemy (an enemy demon over the limit fighting in person against the player's
  demons): it would make the game harder, and no one asked.
- The damage forecast runs the real exchange on a copy of the state (`ExchangeForecast`): the switch goes on the copy too.

### The owner's answers and the build (2026-10-27, decision 32)

The owner took **both A and B as the player's choice**, with the original's rule kept as a third: Option → Tweaks,
section Battle, **"Chaos and Nikkou reach demons"** — radios **Off — as the original** (the default), **On the field**
(A) and **Out of reach** (B). **Only Chaos's and Nikkou's holders** (not the player's demons). Built:

- **The rule**: `Combat/DemonReach` (`Original`, `OnField`, `OutOfReach`) on the game's context,
  `OriginalScriptContext.DemonReach`, the original's on a context built by hand. `Battle.ComputeExchange`
  (`Battle.Resolution.cs`, marker `BRS-24`): while the unit's troops exceed the limit and `Battle.DemonSlayerInPerson`
  holds (the choice is not `Original`, `Cmdr_Class` > 9, `Unit_Item` 1 or 2, tested as the shields test them), step b
  runs (the commander in person: on the demon's troops while 10 or more are engaged, else on the demon), and with
  `OnField` step d too (the demon's troops on him). `Bt_PlayerAllEngaged` keeps the original's 0; the shields are
  unchanged, so the holder's troops still never wound the demon. The scene (`ShowEngagementSceneAsync`) shows him with
  his troops, `Unit_CgPartial`, not `CgFull`.
- **The forecast**: `ExchangeForecast.Of(…, reach)` and `DamageForecasts.Start(…, reach)` run the copy's battle with
  the game's choice.
- **The option**: `godot/Battles/DemonReachChoice` — `[tweaks] demon_reach` in `settings.cfg` (`original`,
  `on_field`, `out_of_reach`), `--demon-reach original|on-field|out-of-reach` for a launch; `GameScreen` copies it onto
  the context at the session's start, `OptionsView.DemonReachChosen` at once; Defaults on Tweaks turns it off.
- **Tests**: `Kichikuou.Original.Tests/Combat/DemonReachTests` (the streams each choice runs, only the two swords
  against a demon, Medusa's troops shooting only on the field, the scene's sprite, the forecast).
