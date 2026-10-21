# The mages' battle lines

Every unit and commander whose troops fight with magic, and every line `FIGT_M.ADV` gives them when
they attack. Purpose (owner, 2026-10-21): decide whether Sengoku Rance's Fire Blast, which our
exchange plays for every magic attack today (docs/ui/battle-screen.md, "The exchange's effects"), is
enough for all of them, or which mages need effects of their own.

Source: the decompile (`scenario_named/FIGT_M.ADV`, the classes from `SET_CHR.ADV`), extracted by a
script and checked by hand; the dispatch is docs/battle/02-resolution.md §5.

## The plan: magic effects by element (owner, 2026-10-21)

The owner's decision: a magic attack plays the effect of the **spell its caster shouts**; when the
line names no spell (a generic line, a line for the target, a chant, no line at all), the effect of
the **caster's element**. Interface only — no rule, draw or line of the original changes.

### The elements

Six effects, one per element; Fire is the Fire Blast we have (Sengoku Rance's Mage, 40360), the other
five are new, drawn by code like the rest (docs/ui/battle-screen.md, "The exchange's effects"), each
after a Sengoku Rance counterpart the owner chooses (step 1 below).

| Element | Spells that pick it (by the line shown) | Callers of the element when no spell is named |
|---|---|---|
| **Fire** (Fire Blast, ours) | Flame Arrow, Fire Laser, Super Royal Fire Laser | Sill (unit 2), Shizuka (unit 17), Gandhi (unit 37), Magic (unit 39), Nagi (unit 40); Nagi (commander 107), Saias (commander 113), Annis (commander 168) — and everyone without an element of their own (below) |
| **Ice** | Ice Arrow, White Chill, Snow Laser, "There, freeze!" | Asuka (unit 11), Melfeis (unit 13); La Seizel (commander 90), Uspira (commander 117) |
| **Lightning** | Lightning, Lightning Laser | Hanty (unit 19; her spell as an enemy), Eleanor (unit 23), Somita (unit 46); Melfeis (commander 4), Hanty (commander 57), Magic (commander 110), Kabachahn (commander 121) |
| **Dark** | Darkness Beam, Black Wave | Chizuko (unit 38); Chizuko (commander 101); the demons without a spell — Satella (unit 29, and as commander 100), Miki (unit 32), La Vaswald (commander 98) |
| **Light** | Friendship Beam, Light Bomb, Miracle Straight Flash | Seizel Sisters (unit 42); Alex (commander 125), Redeye (commander 130); Hanny King (unit 48) |
| **Poison** | nerve gas, Poison Gauntlet | Papaya (commander 104), Ragarl (commander 109) |

The element of a caster is kept per record and side: Melfeis casts Ice Arrow as our unit (13) but
Lightning as the enemy's commander (4), Magic Fire Laser as unit 39 but Lightning Laser as commander
110. *Without an element of their own* (Fire Blast, as today): Freak (unit 20, commander 56), Cafe
(unit 45), the Monster Generals (commanders 88, 89), Marder (commander 100), Zeth's chanting rank and
file (Mage Leader (commander 96) and the others in "Zeth's rank-and-file mages"), Ouzawa (commander
150), Deckmaster (commander 152), Strength (commander 153). Sill shouts fire and ice alike; her
generic lines take Fire.

One effect per element, not per spell's shape (an arrow, a laser, a beam, a burst): the shapes can be
variants later, if the owner wants them.

### How the choice works

- **The line.** The flavour lines reach the presenter as `MessageEvent`s with their `ScriptRef`
  (`FIGT_M` and the line of the run's first text, `Battle.Flavour.cs`, `ShowTextAsync`; e.g. Sill's
  Flame Arrow `:8893`, Ice Arrow `:8901`). A table in `Original.Content/Battles` (`MagicSpells`) maps the
  spell lines to their element; `BattlePresenter` remembers the element of the last spell line it was
  shown since the engagement started, and forgets it when the exchange's shots start.
- **The caster.** The same table maps the casters to their element (a Demon Magic caster (class 11) not in it is
  Dark: Satella, Miki, La Vaswald, and commander 100 when it is Satella — Marder in the same slot is a Mage): a player's unit by `Unit_Cur`, an
  enemy's commander by `Cmdr_Cur`, read at the shots (where stage 4's substitute commander 200,
  BTX-08, has been put back already).
- **The effect.** `ExchangeEffect.Of` takes the element heard: when the firing side's action is magic
  (2) and no special applies, the spell's element, else the caster's, else Fire. New kinds
  `IceMagic`, `LightningMagic`, `DarkMagic`, `LightMagic`, `PoisonMagic`; `Magic` stays the Fire Blast.
  Melee, bow and the specials are unchanged; a mage's Direct attack stays melee (action 0).
- **Under message skip or our battle screen** the lines are still raised (only not shown), so the
  choice does not depend on what is drawn.

### Steps

1. **The references** — Sengoku Rance's magic effects for ice, lightning, dark, light and poison,
   their frames looked at and offered to the owner (docs/sengoku/04-screen.md §2.4.1 gets the
   survey — done 2026-10-21, docs/sengoku/04-screen.md §2.4.1, "The magic's elements"); the owner chooses one per element. The owner settled the casters above (2026-10-21: Sill fire, Hanty
   lightning, the demons without a spell dark, Hanny King light, Annis fire).
2. **The choice** — `MagicSpells` (the spell lines and the casters), the presenter's memory,
   `ExchangeEffect.Of` and the new kinds; until an element's effect exists it plays the Fire Blast. **Done**
   (2026-10-21): `Original.Content/Battles/MagicSpells` (29 spell lines, 13 units, 14 commanders, class 11 dark),
   `BattlePresenter` keeps the last spell line until the shots (`_shoutedSpell`), `godot/Battles/BattleBoardView` plays
   the Fire Blast for every element so far.
   Tests (`MagicSpellsTests`): on given states (a spell line, a generic line, a line for the target,
   no line, the enemy's side, Melfeis and Magic on both sides, a special, a Direct attack); a content
   test that each spell line of the table reads its spell's name in the player's scenario and that
   every unit and commander of a magic class in `SET_CHR.ADV` is in the casters' table or the list
   without an element.
3. **Ice**, 4. **Lightning**, 5. **Dark**, 6. **Light**, 7. **Poison** — one effect each, in this
   order (the first two are the player's units' too), as the others were made: drawn by code after
   its reference, in Debug → Effects and `--effects` (`ice-magic`, `lightning-magic`, `dark-magic`,
   `light-magic`, `poison-magic`), its frames next to the reference's for the owner's look, then wired
   in place of the Fire Blast. The references (owner, 2026-10-21): ice **40730** recoloured white-blue,
   lightning **40630**, dark **47030**, light **40430**, poison **41300**.
   - **Ice** — built (2026-10-21), accepted by the owner (2026-10-21): `godot/Battles/Effects/IceEffect` (a new
     texture, `EffectTextures.Flake`, the snowflake), wired for `IceMagic`. Miko Storm's flurry in white and
     ice blue: blowing in at the bottom right (2-5), a dense whirl of ice shards (diamonds, `Kite`),
     snowflakes, frosty billows, cold glows, thin wind arcs and a few curls turning about a centre that
     climbs to (635, 165) by frame 12, fullest at 12-18, thinning to frame ~29; Miko Storm's petals became
     shards and flakes, its mint white-blue.
   - **Lightning** — built (2026-10-21), accepted by the owner (2026-10-21): `godot/Battles/Effects/LightningEffect`,
     wired for `LightningMagic`. Thunderstrike in blue-white (the owner's colours; its own is green-white): a
     bolt down on the target at (605, 235) with jagged branches over the sky above it, struck anew each frame
     and thinning from frame 7 (0-9); a white flash at its foot with rays fanning out (1-5); a bright filled
     ellipse on the ground (5-9) spreading as a ring (to ~17) while thin strands linger; star sparkles and soft
     specks over the target's half and thinly beyond it, gone by frame ~27.
   - **Dark** — built (2026-10-21), accepted by the owner (2026-10-21): `godot/Battles/Effects/DarkEffect`, wired
     for `DarkMagic`, laid over dark as the Black D. Beam (`EffectPlayer.Dark`; the owner found it too bright
     as light, 2026-10-21): its bright parts black the scene out, its edges glow blue-violet. 47030's shapes: a small magenta orb among faint violet specks (0) swelling into a
     sparkling white ball (2-3), pink-violet specks bursting out round a violet rim (3-7), a white blast of soft
     glows with a darker mottled heart swelling over the half toward the actor (7-15) and turning magenta in a
     magenta afterglow (10-18), a dark violet sphere with a bright rim (15-23), then violet glows, glints and small
     cyan curls fading by frame ~29.
   - **Light** — built (2026-10-21), awaiting the owner's look: `godot/Battles/Effects/LightEffect`, wired for
     `LightMagic`. Angel Cutter in its own colours, thrown from the actor's side as 40430 is: a warm orb at
     (305, 170) swelling into a white sun in an orange-gold halo with rays (0-3), gold glints gathering round it,
     blades of light sweeping right across the field (3-27) — v2 after the owner's look (v1's many flat crescents
     "too simple", 2026-10-21): 68 instead of 130 (64 small, as the owner doubled them), four big ones crossing slowly, each a pale blue-white body fading
     toward its hollow (`EffectTextures.BladeBody`) under a gold-white cutting edge in a halo (`BladeEdge`), tilting
     and growing as it flies with a blur of afterimages; six small faint feathers (`Feather`, half of v2's size, at 45%: the owner's)
     appearing low already, sinking and drifting on toward the target, some off the screen —, a faint glassy sphere where the orb was (6-16), warm glints lingering on the actor's
     side and yellow glints and curls among the blades on the target's (11-26).
8. **Docs** — docs/ui/battle-screen.md, "The exchange's effects", says how a magic attack picks its
   effect; CLAUDE.md's effect list gets the new effects.

## How a line is chosen

- **Magic classes** (`Unit_Class` / `Cmdr_Class`, docs/variables.tsv): 2 Mages, 3 Magic Warriors,
  11 Demon Magic. On the player's side all three attack with Magic (`Bt_PlayerAtkType = 2`); on the
  enemy's side classes 2 and 11 always attack with magic, class 3 never acts (02 §2.3).
- **The player's attack** (the player's turn): the speaker is the attacking unit. `Bt_MsgMode` is 1
  for **Magic** (the command or Auto-Atk) and 3 for **Direct** (a mage in the front row may choose
  it). The guards are tested in order, the first true one shows its line(s): first specials and
  target-specific lines, then the **spell line** (`Mode=1`), then a random generic line. So a mage
  with a spell line shouts it on every magic attack except against the targets listed above it; a
  mage without one shows a generic line on a magic attack too.
- **The enemy's attack**: the speaker is the attacking commander. An enemy mage's attack is always
  magic, so `Bt_MsgMode` is 2: the target-specific lines first, then the `Mode=2` line. The lines
  after `Mode=2` (marked *non-magic* below) are reached only when the mode is stale (BTX-01: Goliath
  and the ninja do not set it) — practically never for a mage.
- In story stage 4 every enemy speaks commander 200's lines, which are none (BTX-08).
- `P` = the player's unit speaks, `E` = the enemy commander; "random 1/3" = one of three lines.

## Summary — what the magic attack is called

| Who | Class | Spell named on a magic attack | Element |
|---|---|---|---|
| **Player units** | | | |
| Sill (unit 2) | Mage | Flame Arrow / Ice Arrow (1/3 each, else a generic line) | fire / ice |
| Asuka (unit 11) | Mage | White Chill; special: Chuhai | ice |
| Melfeis (unit 13) | Mage | Ice Arrow | ice |
| Shizuka (unit 17) | Mage | Fire Laser; special: White Destruction Beam | fire |
| Hanty (unit 19) | Mage | — (generic lines) | — |
| Freak (unit 20) | Mage | — (generic lines) | — |
| Eleanor (unit 23) | Magic Warrior | Lightning | lightning |
| Satella (unit 29) | Demon Magic | — (generic lines) | — |
| Miki (unit 32) | Demon Magic | — (generic lines); special: Vanish Bomb | — |
| Gandhi (unit 37) | Magic Warrior | Super Royal Fire Laser; special: Evil-Purging Overlord's Light | fire |
| Chizuko (unit 38) | Mage | Darkness Beam; special: Black Destruction Beam | dark |
| Magic (unit 39) | Mage | Fire Laser; special: White Destruction Beam | fire |
| Nagi (unit 40) | Mage | Fire Laser; special: Black Destruction Beam | fire |
| Seizel Sisters (unit 42) | Demon Magic | Friendship Beam | light |
| Cafe (unit 45; "？？？？？？？" until `Gｶﾌｪ` names her) | Mage | — (generic lines) | — |
| Somita (unit 46) | Magic Warrior | Lightning | lightning |
| Hanny King (unit 48, class 3 once `Gﾊﾆｰ` sets it) | Magic Warrior | — ("Aiyaaa") | — |
| **Enemy commanders** | | | |
| Melfeis (commander 4) | Mage | Lightning | lightning |
| Freak (commander 56) | Mage | — ("Hiyaaaah!!") | — |
| Hanty (commander 57) | Mage | Lightning Laser | lightning |
| Monster General (commanders 88, 89) | Mage | — | — |
| La Seizel (commander 90) | Demon Magic | Snow Laser / "freeze" | ice |
| Mage Leader (commander 96) | Mage | "Namu namu namu..." (a chant) | — |
| La Vaswald (commander 98) | Demon Magic | — | — |
| Marder (commander 100, a dungeon boss) | Mage | — (".........") | — |
| Satella (commander 100, as an enemy) | Demon Magic | — (".........") | — |
| Chizuko (commander 101) | Mage | Black Wave | dark |
| Noplans (commander 103) | Mage | chant | — |
| Papaya (commander 104) | Mage | nerve gas | poison |
| Casmint (commander 106) | Mage | chant | — |
| Nagi (commander 107) | Mage | Fire Laser | fire |
| Ragarl (commander 109) | Mage | Poison Gauntlet | poison |
| Magic (commander 110) | Mage | Lightning Laser | lightning |
| Nei (commander 112) | Mage | chant | — |
| Saias (commander 113) | Mage | Fire Laser | fire |
| Reshie (commander 114) | Mage | chant | — |
| Uspira (commander 117) | Mage | Snow Laser | ice |
| Starlighter (commander 118) | Mage | chant | — |
| Kabachahn (commander 121) | Mage | Lightning Laser | lightning |
| Kyouko (commander 122) | Mage | chant | — |
| Alex (commander 125) | Mage | Light Bomb | light |
| Sanneri (commander 126) | Mage | chant | — |
| Redeye (commander 130) | Demon Magic | Miracle Straight Flash | light |
| Ouzawa (commander 150) | Mage | — (a roar) | — |
| Deckmaster (commander 152) | Mage | — (a roar) | — |
| Strength (commander 153) | Mage | — | — |
| Renko (commander 167) | Mage | chant | — |
| Annis (commander 168) | Mage | — | — |
| Jessica (171), Am (172), Mayumi (175), Hiroko (176), Suppli (179), Rue (180) (commanders) | Mage | chant | — |

Not mages but magic-sounding: the dungeon's dragons in commander slot 100 (class 0, melee) shout
"Yellow Breath!", "I'll burn you to a crisp!!", "Black!! Breeeeath!" (`FIGT_M:5440`, CGs 1493-1497).

### By element

| Element | Spells | Who |
|---|---|---|
| Fire | Flame Arrow, Fire Laser, Super Royal Fire Laser | Sill, Shizuka, Gandhi, Magic, Nagi (units); Nagi, Saias (commanders) |
| Ice / snow | Ice Arrow, White Chill, Snow Laser, "freeze" | Sill, Asuka, Melfeis (units); La Seizel, Uspira (commanders) |
| Lightning | Lightning, Lightning Laser | Eleanor, Somita (units); Melfeis, Hanty, Magic, Kabachahn (commanders) |
| Dark | Darkness Beam, Black Wave | Chizuko (unit); Chizuko (commander) |
| Light | Friendship Beam, Light Bomb, Miracle Straight Flash | Seizel Sisters (unit); Alex, Redeye (commanders) |
| Poison | nerve gas, Poison Gauntlet | Papaya, Ragarl (commanders) |
| Unnamed | a chant ("Namu namu namu...") or no spell at all | Hanty, Freak, Satella, Miki, Cafe, Hanny King (units); the Zeth rank and file, Freak, the monsters, Vaswald, Marder, Annis (commanders) |

## Player units

### Sill (unit 2) — Mage (`FIGT_M:8889`)
The draw `R25 = RND(3)` at `:25` picks:
- 1 and Magic → **P:** Flame Arrow!!
- 2 and Magic → **P:** Ice Arrow!!
- otherwise, random 1/5:
  - **P:** I'll do my best, Master Rance.
  - **P:** If I don't do my best, I'll get scolded later... I'm sorry!
  - **P:** Taaake that! Please go awaaay!
  - **P:** Take that!
  - **P** (dark window): Sob... I'm scared...

### Asuka (unit 11) — Mage (`FIGT_M:8314`)
- Special (Chuhai) → **P:** Chuhai, go, go. Ooh ooh.
- Mill (unit 25) in her party, once (`F_Asuka_State` 0 → 1) → a short scene with Chaka about Mill's phantom
  beasts, ending "Ah～～..." (BTX-04).
- Magic → one window: a line for the target, then "White Chiiill!":
  - Ex (commander 1): "Ah, Mr. Ex. Let's go～."
  - Haurein (commander 2): "Haurein? Is this okay...? She never spanked me."
  - Pegasus (commander 3): "Mr. Pega. Hello～."
  - Melfeis (commander 4): "Melfy... Why? ...Uu～, whatever. Let's go～."
  - Dodge, Sakanaku or Jibul (commanders 5-7): "Oooh? Mister... ...Uu, here I go."
  - anyone else: only "White Chiiill!"
- otherwise, random 1/3:
  - **P:** This'll hurt. Go away.
  - **P:** Aah～～... Take this!
  - **P:** Puipui! Puu～～!!

### Melfeis (unit 13) — Mage (`FIGT_M:151`)
- Magic → one window: a line for the target, then "Ice Arrow!":
  - Ex (commander 1): "Sir Ex... I'm sorry."
  - Haurein (commander 2): "Haurein... This feels strange..."
  - Pegasus (commander 3): "Sir Pegasus... I..." — **E:** Now, come at me! Melfeis! — **P:** Ice Arrow!
  - anyone else: only "Ice Arrow!"
- otherwise, random 1/3:
  - **P:** Kh... I'm no good on the front lines...
  - **P:** Everyone... You must be careful...
  - **P:** You don't need to pick fights you can't win! Do what you can while staying alive!

### Shizuka (unit 17) — Mage (`FIGT_M:381`)
- against Nagi (commander 107): in a duel **P:** You're in the way... Move it!!; else **P:** As you wish... I'll settle this!
- against Ragarl (commander 109): **P:** Ragaaaarl!! I'll kill you...!
- Special 2 → **P:** White Destruction Beam!!
- Magic → **P:** Fire Laser!
- otherwise, random 1/3:
  - **P** (dark window): Stupid Rance! Don't make me fight directly!
  - **P:** Running's the best option... I'll open an escape route!
  - **P:** I'm not gonna die for Rance!

### Hanty (unit 19) — Mage (`FIGT_M:585`)
- against Lelyukov (commander 22): **P:** ...Sorry. General Lelyukov...
- against Aristoles (commander 32): **P:** Aristoles... This is hard for Patton too...
- against Mineva (commander 39): **P:** ......... I won't go easy.
- otherwise, random 1/3 (also on Magic):
  - **P:** Now then... Should I do it?
  - **P:** Hmph... War's been going on forever... I wonder when it'll end...
  - **P:** Alright, let's go.

### Freak (unit 20) — Mage (`FIGT_M:653`)
- against Lelyukov (commander 22): **P:** Ah, man... What are we geezers doing...?
- against Aristoles (commander 32): **P:** As I always say, yesterday's enemy is today's friend...
- against Mineva (commander 39): **P:** You gave us a lot of trouble, too...
- against Redeye (commander 130): a scene (`L_0bddd`, long the first time, short after).
- otherwise, random 1/3 (also on Magic):
  - **P:** Hey, don't gang up on the elderly!
  - **P:** Hyah! Let's go.
  - **P:** Mgh～～... Mmmmgh... ...Haah... Charge!

### Eleanor (unit 23) — Magic Warrior (`FIGT_M:942`)
- Magic → **P:** Lightning!!
- otherwise, random 1/3:
  - **P:** ...I'm sorry...
  - **P** (dark window): ...Aah... Why this...?
  - **P:** Um... Everyone... Move out... Without getting too out of order...

### Satella (unit 29) — Demon Magic (`FIGT_M:1254`)
- before stage 3, once each: against Camilla (commander 91) and Lei (commander 92) a scene (`L_0ba24`, `L_0bb6d`).
- against Nobunaga Oda (commander 72): **P:** A demon...? There's something strange about him, though...
- against La Seizel (commander 90): **P:** Stupid Seizel. You won't even think about Hawzel's feelings...
- against Camilla (commander 91): **P:** Camilla! You should just lie down and stay down!
- against Lei (commander 92): **P** (dark window): Lei, I thought you hated Kayblis... Why are you on his side?
- against Sieg (commander 93): **P:** Out of the way, Sieg!
- against La Vaswald (commander 98): **P:** Something strange happened somewhere in the world... How could you have shown up?!
- against Babolat (commander 129): **P:** Out of the way, you resilient moron!
- against Redeye (commander 130): **P:** I hate you, stupid!
- against Kayblis (commander 135): **P:** ...Die!!
- otherwise, random 1/3 (also on Magic):
  - **P:** Let's go!! Everyone in front of me's my enemy! Slaughter them all!
  - **P:** You're in the way! Hurry up and die!
  - **P:** I won't go down that easily. Prepare yourself!

### Miki (unit 32) — Demon Magic (`FIGT_M:1493`)
- Special (Vanish Bomb) → **P:** No moooore!!
- against Kayblis (commander 135): **P:** Hii...!! No, nooo!
- otherwise, random 1/3 (also on Magic):
  - **P:** No... Stay away... I'm scared... Stay awaaay...!
  - **P:** Nooo!! Mooooom!!
  - **P:** Hick... Sob.. I don't like this...

### Gandhi (unit 37) — Magic Warrior (`FIGT_M:1701`)
- Special → **P:** Evil-Purging Overlord's Light!!
- against Chizuko (commander 101): **P:** Chizuko... Die defending Zeth! — **E:** Uu... No... King Gandhi...!
- against Papaya (commander 104): **P:** You fool! You will never be forgiven for the Flash!
- against Nagi (commander 107) or Ragarl (commander 109): **P:** This is your punishment... Perish, Zeth!
- against Magic (commander 110): **E:** Dad... Are you attacking me!? — **P:** Don't despise me, Magic! Your father's decision won't be swayed by blood!
- against Saias (commander 113) or Uspira (commander 117): **P:** This is not only about Zeth, but all of mankind... Forgive me!
- against Kabachahn (commander 121): **P:** Sorry, old man... You'll have to suffer to the end.
- against Alex (commander 125): **P:** If you truly want Magic, you'll have to defeat this traitorous king!
- against Annis (commander 168): **P:** Mgh... Annis. It's been a while, but I won't go easy! — **E:** Huh? Who are you, mister...?
- Magic → **P:** Super Royal Fire Laser!!
- otherwise, random 1/3:
  - **P:** When there's pleasure, pain comes again! That is the cycle of humanity...
  - **P:** Look at the big picture! King Rance is clearly our hero and messiah!
  - **P:** Forgive me! This battle, too, is for the sake of humanity!

### Chizuko (unit 38) — Mage (`FIGT_M:1902`)
- Special → **P:** Hoooohohohoh! Black Destruction Beam!!
- against Papaya (commander 104): **P:** I thought about it after King Gandhi scolded me, and... Yeah, the Flash was bad. — **E:** Ah, how could yooou?! That's terrible, Chizukooo!
- against Magic (commander 110): **P:** When will you stop being stubborn!? Hurry up and surrender!
- against Saias (commander 113) or Alex (commander 125): **P:** Hmph, you look so unconcerned... I could never stand that. This is my chance!
- against any other Zeth commander (96-128): Magic → **P:** I'm fighting for King Rance... Don't think poorly of me. Darkness Beam!!; else **P:** King Rance... I won't betray you, even for my homeland!
- Magic → **P:** Hohohohohoh, Darkness Beam!!
- otherwise, random 1/3:
  - **P** (dark window): I'm trying hard... Please don't abandon me... King Rance...
  - **P:** Kh... I won't step aside. I wouldn't be able to face King Rance if I did!
  - **P:** Hooohohohohoh! Go, for King Rance!

### Magic (unit 39) — Mage (`FIGT_M:2056`)
- Special → **P:** White Destruction Beam!
- against Chizuko (commander 101): **P:** Sorry, Chizuko. I've got orders, so let me attack you.
- against Papaya (commander 104): **P:** Papaya, let's end this futile fighting... Stop your mad research and surrender!
- against Nagi (commander 107): **P:** Nagi... I've fought with you before. You don't scare me.
- against Ragarl (commander 109): **P:** I'm different from my dad... I know your existence is worthless to Zeth, Ragarl.
- against Saias (113), Uspira (117) or Kabachahn (121) (commanders): **P:** Sorry... Forgive me, everyone...
- against any other Zeth commander (96-128): Magic → **P:** Everyone... Surrender now! Now isn't the time to defy him...!; else **P:** Intentionally having me face Zeth... Are you testing me, King Rance...?!
- Magic → **P:** Uuuum... Fire Laser.
- otherwise, random 1/3:
  - **P:** Let's end this fast and go home. I'm gonna complain to King Rance for this order!
  - **P:** Don't be reckless... You're a complete fool if you die in war.
  - **P:** Well, let's do this without dying.

### Nagi (unit 40) — Mage (`FIGT_M:2225`)
- Special → **P:** Black Destruction Beam.
- Magic → **P:** Fire Laser.
- otherwise, random 1/3: **P:** "........." / "...Go." / "Everyone, attack."

### Seizel Sisters (unit 42) — Demon Magic (`FIGT_M:2337`)
- Magic → **P:** Friendship Beam!!
- otherwise → **P:** I'm sorry... to the people that will fall in this battle... Forgive me...

### Cafe (unit 45) — Mage (`FIGT_M:2465`)
- once she has joined as the caravan's lady (`F_Cafe_JoinMode = 2`): **P:** Goooo!! For Lady...
- against the demons (La Seizel, Camilla, Lei, Sieg, La Vaswald, Babolat, Redeye and the others of commanders 90-93, 98, 129-134): **P:** A demon... Don't hold back! Take them!
- against Kayblis (commander 135): **P:** You must be the leader of the demons... Hmph... I hate you!
- otherwise, random 1/3 (also on Magic):
  - **P:** Charge!! You're good girls, so don't get injured!
  - **P:** Keep focused! Don't be startled!
  - **P:** Let's go! Follow me!

### Somita (unit 46) — Magic Warrior (`FIGT_M:2537`)
- Magic → **P:** Hyaaaaah!! Lightning!
- otherwise, random 1/3:
  - **P:** Sorry! It'll hurt, but please bear with it!
  - **P:** Hyaaah! Huhu.
  - **P:** Yeah! I'll do my best!

### Hanny King (unit 48) — Magic Warrior once `Gﾊﾆｰ` makes him (`FIGT_M:2673`)
- only as the Hanny King (`Unit48_Identity = 1`): **P:** Aiyaaa. Aiyaaa.

## Enemy commanders

Only the lines an enemy mage's (magic) attack reaches are listed first; *non-magic* marks the
practically unreachable rest (see "How a line is chosen").

### Melfeis (commander 4) — Mage (`FIGT_M:3034`)
- **E:** Lightning!! Match the timing on the chanting!
- *non-magic:* Please, step back! I don't want to kill you!

### Freak (commander 56) — Mage (`FIGT_M:4575`)
- **E:** Hiyaaaah!!
- *non-magic, random 1/3:* "Ah, man. Gotta fight even at my age." / "Guess I've gotta do a little fighting." / "Hmhm... Should we do it? Hey, let's go."

### Hanty (commander 57) — Mage (`FIGT_M:4621`)
- **E:** Haaaah... Lightning Laser!!
- *non-magic, random 1/3:* "Prepare yourself! I won't go easy!" / "Aah, how annoying. One after another..." / "...Do your best, Patton."

### Monster General (commanders 88, 89) — Mage
- 88: **E:** I'll show you the power of monsters.
- 89: **E:** Still resisting? A futile effort...

### La Seizel (commander 90) — Demon Magic (`FIGT_M:5133`)
- target's magic below 4 → **E:** Don't have much magic resistance, do they? Maybe. There, freeze!
- else → **E:** Taaake that!! Snow Laser.
- *non-magic, random 1/3:* "Hmph. Don't waste my time with weaklings." / "Do you seriously think you can win? Are you stupid?" / "Hahaha!! Freeze!"

### La Vaswald (commander 98) — Demon Magic
- **E:** Are you the one that made me remember my own existence...?

### Commander 100 — Marder (a dungeon boss, Mage) and Satella as an enemy (Demon Magic) (`FIGT_M:5440`)
- both → **E:** .........
- The slot's other occupants have lines of their own (the angels "For God, we will kill the evil god,
  Rance!", the dragons' breaths, two more), but none of them is a mage.

### Chizuko (commander 101) — Mage (`FIGT_M:5536`)
- against Gandhi (unit 37): **E:** Ah... Ah... King Gandhi, what should I do...?! — **P** (dark window): Forgive me, Chizuko... I've made things difficult for you...
- against Magic (unit 39): **E:** Waiiit a second!! What are you doing switching to Leazas's side!? — **P:** Shut up, it's not like I like being on my dad's side or anything!!
- against Nagi (unit 40): **E:** You've betrayed us, Nagi...! — **P:** ...I guess.
- **E:** Learn the power of the magical country of Zeth! Black Wave!!
- *non-magic, random 1/3:* "Kick around those filthy followers of the Brutal King!" / "Even if I die I won't let you bring ruin to Zeth!" / "King Gandhi... Please protect me!"

### Papaya (commander 104) — Mage (`FIGT_M:5689`)
- against Gandhi (unit 37): **E:** It's not the Flash's fault...! It's because it's an amazing magic!
- against Chizuko (unit 38): **E:** Chizukooo, without you they aren't gonna let me continue my research～. — **P:** Th-Then don't attack me! Your reasoning doesn't make any sense...!
- against Magic (unit 39): **E:** Hee... Heeheehee... Look, Nomicon. She looks just like Magic～. Should we kill her for dissection～? — **P:** Wait... I'm the real Magic!
- against Nagi (unit 40): **E:** Oh, Nagi. I'm happy to see you. I wanted to try fighting a modified human at least once～. — **P:** ...Hmph...
- **E:** Um... Maaaybe I should use nerve gas. Hee, heeheehee...
- *non-magic, random 1/3:* "Heeey, look, Nomicon, there's sooo many test subjects." / "Hee, Heeheehee... Can I huuurt you?" / "What should we do, Nomicon? ...Huh, attack? Then leeet's go, everyone!"

### Nagi (commander 107) — Mage (`FIGT_M:5870`)
- against Shizuka (unit 17): **E:** Daughter of Masou... I will defeat you.
- **E:** Fire Laser.
- *non-magic, random 1/3:* "........." / "Kill them." / "Go, annihilate them."

### Ragarl (commander 109) — Mage (`FIGT_M:5928`)
- against Rance (unit 1) in a duel: **E:** Not retreating, youngster?!
- against Shizuka (unit 17): **E:** Hah... Huhahahahahahaha! I'll take everything from you again, Masou! — **P:** Shut up... Don't laugh!
- against Gandhi (unit 37): **E:** If it isn't Your Majesty...? Glad to see you.
- against Magic (unit 39): **E:** Oh, the King's daughter... She could be interesting as a test subject.
- **E:** Watch closely, Nagi. Poison Gauntlet!!
- *non-magic, random 1/3:* "Hmph... A foolish battle." / "Ch... Is there anyone here to act as a shield?!" / "Ridiculous. Let's end this fast and get back to training."

### Magic (commander 110) — Mage (`FIGT_M:6043`)
- against Gandhi (unit 37): **E:** I'll protect this country! If you won't come to your senses, die! — **P:** As you wish... Come, Magic!
- against Chizuko (unit 38): **E:** Chizukoooo! Why did you change sides?! Don't be misled by my dad! Tasteless moron!! — **P:** As insulting as ever...!
- against Nagi (unit 40): **E:** Nagi... I knew you didn't care what happens to Zeth. — **P:** ...You're right.
- in the Zeth war (`F_Zeth_State = 9`): **E:** Leazas... Further aggression won't be tolerated!! Lightning Laser!!
  (*non-magic, random 1/3:* "Kh... Did we falter?!" / "I won't lose...! I'll never lose!!" / "This is our country... Get out!")
- else: **E:** Haah... This is annoying. Lightning Laser!!
- *non-magic:* I have to get my homework done...

### Saias (commander 113) — Mage (`FIGT_M:6208`)
- against Gandhi (unit 37): **E:** Why are you with Leazas, King?!
- against Chizuko (unit 38): **E:** Lord Chizuko... Allow me to return my many years of debt.
- against Magic (unit 39): **E:** Magic...! Have you lost your mind?!
- against Nagi (unit 40): **E:** Lord Nagi... I heard you'd disappeared, but...!
- **E:** Ngh... Fire Laser!!
- *non-magic, random 1/3:* "Attack... Watch the enemy's movements!" / "Ah, man... The front line? Things have gotten tough." / "Uspira... I fight for you."

### Uspira (commander 117) — Mage (`FIGT_M:6350`)
- against Gandhi (unit 37): **E** (dark window): Why...? Why am I fighting you...?
- against Chizuko (unit 38): **E** (dark window): I see... You decided to go to your loved one...
- against Magic (unit 39): **E:** I suppose you have some idea, Magic. But I will fight...
- against Nagi (unit 40): **E:** I'm shocked... Lord Nagi has joined Leazas.
- **E:** ... Snow Laser.
- *non-magic, random 1/3:* "I mustn't run away..." / "Let's go. There's no need to be afraid..." / "...Let's repel them."

### Kabachahn (commander 121) — Mage (`FIGT_M:6480`)
- against Gandhi (unit 37): **E:** It's been a while since we've had a bout... King Gandhi...
- against Chizuko (unit 38): **E:** Lord Chizuko, forgive me!
- against Magic (unit 39): **E:** Dear, I even have to fight you, Miss...? A soldier's life is tough.
- against Nagi (unit 40): **E:** Lord Nagi...? This is complicated.
- **E:** Let's see... How about we scatter them? Lightning Laser.
- *non-magic, random 1/3:* "A slight disadvantage... But I have to do it." / "We do not fear swords." / "We're protecting the country in the King's absence. Let's go, everyone."

### Alex (commander 125) — Mage (`FIGT_M:6626`)
- against Gandhi (unit 37): **E:** Your Majesty, please return to your country! I don't wish to fight you...!
- against Chizuko (unit 38): **E:** Lord Chizuko... Even you've turned your back on Zeth?!
- against Nagi (unit 40): **E:** Lord Nagi...? What are you doing?!
- **E:** Sorry, but... We have something to protect, too! Light Bomb!
- *non-magic, random 1/3:* "Kh... We won't hand this city over! Hang in there, everyone!!" / "More than magic, the sword...! That's what His Majesty would always say...!" / "Your Majesty... I won't give up on Magic...!"

### Redeye (commander 130) — Demon Magic (`FIGT_M:6836`)
- **E:** Mm～... Miracle Straight Flaaaaaaaaash!!
- *non-magic, random 1/3:* "KILL YOU～! Whatever you do, ME NO HELP! KILL YOU～!" / "MAAAAAAKE DRAAAAAMA!!" / "This called, KILL YOU～!"

### Zeth's rank-and-file mages — Mage (the chant)
Each attacks with **E:** Namu namu namu... (*non-magic:* the line after it):

| Commander | *non-magic* line |
|---|---|
| Mage Leader (96) | I'll protect Zeth while King Gandhi's away! |
| Noplans (103) | I won't allow you to dirty Zeth's land. |
| Casmint (106) | Aah, I'm no good at physical labor... |
| Nei (112) | Leazas was supposed to be an inferior country! |
| Reshie (114) | I'll show you how well I can do, General Saias! |
| Starlighter (118) | Everyone, charge! |
| Kyouko (122) | General Kabachahn, we're going! |
| Sanneri (126) | We won't yield to violence!! |
| Renko (167) | Savage nation. Lay yourselves down!! |
| Jessica (171) | Where are you going...? Wheeeeere?! |
| Am (172) | Hmph... Your country's magic is pathetic!! |
| Mayumi (175) | Aah... It's times like this when I wish King Gandhi were here...! |
| Hiroko (176) | Uu, this looks bad... But I won't lose! |
| Suppli (179) | Countries where most people can't use magic are so uncool!! |
| Rue (180) | Zeth won't lose! |

### The others with one line (no `Mode=2` guard, so it is their attack line)
- Ouzawa (commander 150): **E:** Gogyaaaah!! Guh... Gugaaah!!
- Deckmaster (commander 152): **E:** Guoooo!!
- Strength (commander 153): **E:** Haaaah...! Give me your life!
- Annis (commander 168): **E:** Take this, Leazas Army!!
