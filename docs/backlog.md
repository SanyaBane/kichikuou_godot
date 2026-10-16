# Backlog

Everything put off, in one place: what it is, where its details live, what it waits for. Details
stay in the linked docs; this list only points at them.

- **Add** an item whenever work is put off (by the owner or by the agent), with the date and a link.
- **Remove** it when it is done or taken into a milestone ([strategy/README.md](strategy/README.md)
  holds the milestones and the owner's numbered decisions); git keeps the history.
- **Waits for:** *owner* — a decision or a play session of the owner's; *capture* — a run of the
  original game through the harness ([golden-tests.md](golden-tests.md)); *agent* — work the agent
  can do on its own.

Last swept: 2026-10-11.

## Release

The model is decided (brief, "Risks"; decision 23): only our engine is distributed, under
GPL-2.0-or-later, and the player brings their own copy of the game. What a release still needs
(discussed with the owner 2026-10-11):

- **Read the player's game folder directly** (decision 1 foresaw it) — *agent*. Today the port needs
  the developer's extract (`_extract`: the decompile, CGs as PNG, the sounds). An import on the
  first launch into `user://`: the `.ALD` archives read directly (the PMS decoder exists,
  `Content/Graphics/Pms.cs`), the scenario decompiled by a bundled `xsys35dc` pinned to 1.13 (the
  parser knows only its output; GPL-2, shipped with its source) or by our own loader of the `.SCO`
  bytecode. Android (below) cannot run a bundled native tool, so the import has to be C# whole: our
  own `.SCO` loader, or `xsys35dc` translated to C# (GPL-2, a header naming its origin).
- **Check the game's version** — *agent*: the port reads only the English translation v1.0; check the
  archives' hashes and refuse another version clearly instead of failing somewhere in the parser.
- **A folder picker** for the game folder — *agent*. Today only `user://settings.cfg` or the field in
  Debug → Battle Captures; `local.env` is for development only and is not read outside the
  repository.
- **Hide the development parts** in a release build — *agent*: the Debug menu, the capture viewer,
  the switches (`--auto`, `--watch`, `--ending`, …).
- **Windows export** — *agent*: commit `export_presets.cfg` (git ignores it today); export templates
  4.7.2 mono. The bundled fonts' licenses `godot/fonts/OFL.txt`, `Shojumaru-OFL.txt`, `Cinzel-OFL.txt` and `Oswald-OFL.txt`
  must ship with it (non-resource files: the preset's include filter); the fonts themselves are exported as
  they are (`importer="keep"`).
- **Android** — *agent* (the owner wants it, 2026-10-11): our own `.apk`, not Google Play. Godot 4.7
  exports C# to Android as experimental and needs .NET 9 or later (the project is on `net10.0`). It
  needs the C# import above (the player copies the game's files to the device; how a Godot 4.7 app
  reaches them under Android's scoped storage is to be checked) and a touch interface (the font is
  done: BIZ UDGothic is bundled, docs/window-menu.md, Option → Fonts; `OFL.txt` ships as for
  Windows): the original is played with a mouse — the right button cancels (menus, the map, the empty
  dungeon menu, All Data's scale menu), the pickers' highlight and info panel follow the hover, keys
  drive the message skip and xsystem35's menu, and a menu line is 18 of 400 pixels (~3 mm on a 6.5"
  phone; every dependency in [ui/survey.md](ui/survey.md) §3). The touch interface belongs with our
  own interface (Interface, below). Unknown until
  measured: the speed of the engine surface's per-pixel C# drawing on a phone. A cheap spike first:
  the current build on a device with the extract copied by hand, a tap as a left click.
- **CI** — *owner* (a remote first), then *agent*: build, core tests, export. Without the game data
  some content tests fail instead of skipping; they must skip.
- **Rights** — *owner*: the license covers only our own work; the rule code ports AliceSoft's
  scenario logic, the risk the brief accepts for personal use. Any public release is the owner's call.

## Saves

- **The original's `.asd` saves** — *owner* put it off (2026-10-11); decision 11 chose our own format.
  Found while looking (2026-10-11): `System39.exe` and xsystem35 both write "System3.8 SaveData"
  (`savedata.c`): a 4 KB header (the resume point as page and address, window and font settings,
  offsets), the call stack, 5000 strings, pages 0, 1, 2, 4, 10 — what `Script/SaveData` holds. The
  scenario's only `QD` is `ﾛｰﾄﾞｾｰﾌﾞ.ADV:33`, called only from `MAIN.ADV`'s `L_01875` at `:20` and
  `:83`, so every original save resumes at one of our two autosave points (the near call's return
  address tells which). The owner's ten slots are all Leazas-week autosaves; four lack xsystem35's
  `XS35` mark, so `System39.exe` likely wrote them. Reading them is cheap; writing them needs the
  window settings, which a survey must find. Open: where the port keeps saves (the game folder, its
  own, an import).

## Verification against the original

The stages after stage 1 and the game frame are covered by the port's own tests only.

- **Stage-2 captures** — *owner, capture* (decision 15): the checklist is
  [golden-tests.md](golden-tests.md), "Capturing stage 2".
- **Stage-3 captures** — *owner, capture* (decision 16).
- **Stage-4 captures** — *owner, capture* (decision 18); they start from the owner's save at the end
  of stage 3.
- **The frame** (the endings `OVER`, the title `ｽﾀMENU`, Fortune Check `HAPPY_C`) — *owner, capture*
  (decision 20); **the gallery and the music room** — *owner, capture* (decision 22).
- **`Iｶﾐ` end to end against a capture** — *capture* (survey-ikami.md; no capture reaches it).
- **`--auto` plays stages 0-1 only** — *agent* if wanted (decision 15).

## Interface

- **Our own interface** next to the original layout — *owner*: decisions 3-4 (the player picks the UI,
  ideally also during play), 9 and 12 (what a redesign replaces). The survey's parts 1-4 are done
  (2026-10-12, [ui/survey.md](ui/survey.md): the screens, the classes of work, input on mouse and
  touch, how a second UI plugs in). Decision 25 (2026-10-12): screen by screen over the original
  layout, the deployment table first — built (2026-10-12, Option → Interface → New, its second part
  too, and the third, the party as a formation with faces and drag and drop): [ui/deployment-table.md](ui/deployment-table.md).
  Next (owner, 2026-10-12): our own battle screen, the whole battle in the manner of Sengoku Rance;
  its battle is surveyed from its code in [sengoku/README.md](sengoku/README.md); the plan is
  [ui/battle-screen.md](ui/battle-screen.md).
  Later (owner, 2026-10-12): our own battle screen may want the enemies' faces too — the commanders'
  faces are in Debug → Portraits (`UnitFaces.Commanders`); nine commanders have none (and commander
  100 has one only in some scenes' battles); their battle sprite stands in (owner, 2026-10-12;
  `RecordFace.StandIn`, its square set in the viewer) — [ui/deployment-table.md](ui/deployment-table.md), step 7.
  Under way (owner, 2026-10-14): the exchange's effects on our battle screen, our own as like Sengoku Rance's as
  can be (melee, bow, magic: done), their wiring into the exchange (done), Rance Attack's after Sengoku Rance's
  (done 2026-10-14, the owner took v1 as it is), the white and the black beam after 40460 (done 2026-10-15, the owner took v1 as it is;
  no captured battle has a beam), Isoroku's Whirlwind Shot after 40660 (done 2026-10-15, the owner took v1 as it is),
  Meglass's High Speed after 41400 and Rick's By la Way as Rance Attack's in red (done 2026-10-15, the owner took v1 as it is; the
  owner's new choices), the ninja's follow-up after 40100 (done 2026-10-15, the owner took v1 as it is), Miki's Vanish Bomb
  after 40560 (done 2026-10-15, the owner took v1 as it is; no captured battle has one), Patton's C. War Dance after
  47400 (done 2026-10-15, the owner took v1 as it is; no captured battle has one), Asuka's Chuhai after 47060 (done
  2026-10-15, the owner took v1 as it is; no captured battle has one), T2 Goliath's after 46820 (done 2026-10-15, the owner
  took v1 as it is; 15 frames as the reference; no captured battle has one), Kentarou's Rance Attack after his own
  Kentarou-kun Slash, 40800 (done 2026-10-15, the owner took it as it is; in his sprite's mint, starting where his
  sprite strikes, nothing behind him), Rolex's T2 Soaring Wave, our own (done 2026-10-15, the owner took v2 as it is; the owner's design: his
  sprite's four arcs running at the enemy and bursting on it; no captured battle has one); v2 of four after the owner's notes (2026-10-15,
  the owner took them as they are): High Speed in Meglass's blues, By la Way redder, Vanish Bomb far larger, War Dance a
  third fewer crescents —
  then the owner's review of them all (2026-10-15, taken as it is): the attacker's melee dust, no level arrows,
  each beam its user's own effect placed for its sprite (Shizuka's, Magic's, Gandhi's EPO Light, Chizuko's, Nagi's),
  the beams and Whirlwind Shot stretched through the enemy's army, the specials moved onto their users' sprites,
  By la Way's hits on the enemy —
  [ui/battle-screen.md](ui/battle-screen.md), "The exchange's effects". Every special has its effect now; the
  Cheater Beam stays melee.
  Put off (owner, 2026-10-15, step 6): on our battle screen the other script windows a battle opens
  (111/112/121/122/113/131: narration and dialogue, e.g. in the story battles) stay the original's for now —
  [ui/battle-screen.md](ui/battle-screen.md), question 3 and step 6 (the strategy screen is our cut-in since
  2026-10-15, "The strategy's cut-in").
  Next (owner, 2026-10-16): our own map and command screen (`COMMAND`'s map wait and command menu) —
  the survey, Sengoku Rance's map screen and three mock-ups are [ui/command-screen.md](ui/command-screen.md);
  the owner goes in small steps, first the map's markers (chosen 2026-10-16: name plates over the original's
  flags, the dungeons named with their kind and search; ui/command-screen.md, "The map's markers").
  Groundwork (survey §4.8): menus as data and the map helper are done (2026-10-12); the shared engine
  host (§4.8, "Items 2 and 4 are one step") is not needed for screens over the original layout.
- **Quality-of-life features**, the README's second aim — *owner* (which), then *agent*; a survey of
  candidates first. Done so far: MsgSkip skips the opening whole (2026-10-11,
  [window-menu.md](window-menu.md), MsgSkip).
- **Optional fixes** — *owner* (which), then *agent*: the quirks register ([quirks.md](quirks.md),
  decision 5) lists every reproduced bug; some can become options.

## Open questions from the surveys

Swept 2026-10-11: of ~120 questions in the docs' "Open questions" sections, these are still open
(line numbers as of the sweep). A copy in another file is noted once.

Need a run or capture of the original:

- [battle/README.md](battle/README.md) Q4 (`:16`, = 02-resolution O4): can `System39.exe`'s `Y 4`
  return n+1 like xsystem35's? (ENG-02)
- battle/README.md Q7 (`:19`, = 01-flow §14): who restores Rance's `Unit_Status` after a prologue
  defeat? (BFL-25)
- [strategy/survey-main.md](strategy/survey-main.md) `:332`: does `System39.exe` do the overlapping
  `NB` as a forward copy? (STR-01)
- [strategy/stage-2-sweep.md](strategy/stage-2-sweep.md) `:281` (AL Church attacked: a stale enemy
  party, or the city taken unopposed?), `:285` (Shangri-La taken anyway after a lost duel?).
- [strategy/stage-3-sweep.md](strategy/stage-3-sweep.md) `:334` (= survey-nations `:325`: does the
  Grief Valley guards' loop end only by losing? NAT-02), `:339` (Medusa's replays, NAT-25), `:340`
  (is La Vaswald reached at stage 3 in practice?).
- [strategy/stage-4-sweep.md](strategy/stage-4-sweep.md) `:320`: is Kou at 8 the only winning route?
- [strategy/survey-commands.md](strategy/survey-commands.md) `:380`: attacking a province of owner 9
  or 10 (the Angels): `Cﾃｷﾄｼ` has no defender setup for them (CMD-19, unverified).
- [strategy/survey-dungeon.md](strategy/survey-dungeon.md) `:348` (can a click hit the empty dungeon
  record 0? STR-12), `:351` (`RetVal` after `L_0755b` on Return).
- [strategy/survey-ikami.md](strategy/survey-ikami.md) `:360`: the summon slot (unit 51) in a party,
  not run (EVT-18).
- [strategy/survey-nations.md](strategy/survey-nations.md) `:322` (NAT-01, confirmed by reading, not
  by a capture), `:330` (what the empty commander record 0 holds, NAT-12; existing dumps may answer).

Research the agent can do:

- battle/README.md Q3 (`:15`): what the summon slot (unit 51) holds before any summon — probably
  closable (survey-data.md `:66-68`: never initialised; BRS-13). Q6 (`:18`): can `Bt_MsgMode` be
  outside 1..4 at a Goliath strike? (BTX-02)
- [battle/screen-layout.md](battle/screen-layout.md) `:727`: does `#L_05126, 1` select `L_0512e`?
  (`Battle.Display.cs:141` assumes it; the battle captures may prove it.)
- [spike-findings.md](spike-findings.md) `:186`: confirm the variables still at I or G confidence in
  [variables.tsv](variables.tsv) (136 at I, 8 at G).
- stage-2-sweep.md `:287` (does taking a Zeth tower kill or capture its minister?), `:289` (`RetVal`
  after the Camilla Unit's `FIGT_B` while she lives; STR-02), `:290` (is `Iﾏｼﾞﾝ` 1 `:1569-1719`
  reachable; endings 5 and 12 at stage 2?), `:292` (Use Force with every listed commander dead).
- stage-3-sweep.md `:336` (Kesselring's Castle won with Kesselring alive?), `:338` (Kayblis killed
  without `Gｼﾙｷｨ` 9?).
- stage-4-sweep.md `:324` (Siberia and the Ancient Ruins lost for good to the Angels?), `:329` (is
  path 2 reachable? NAT-31).
- survey-commands.md `:381` (= survey-main `:340`: does anything read the stale cursors? single cases
  in ENG-05, EVT-08/17/20, NAT-28), `:383` (`Gｼｨﾙ` #2/#5/#6 command flags), `:384` (`Gﾊﾟｯﾄﾝ` 3 then 4
  in one pass?), `:385` (where Mars the Great's troops come from), `:386` (the Mineva
  `TroopAtk = 20` gate, `OwnCityCommand.cs:428`), `:387` (where Rance levels up from harem EXP),
  `:388` (is the item record 100 live when `Cﾆﾝｼﾞｬ` zeroes it? EVT-04).
- [strategy/survey-data.md](strategy/survey-data.md) `:288`: the third unit string slot's meaning.
- survey-dungeon.md `:344` (who sets `F_Merim_MillArc = 3`? DGN-11), `:353` (`VAR0427` unnamed).
- [strategy/survey-events.md](strategy/survey-events.md) `:393` (the owner of the empty province
  record 0 for `Gｻｲｾﾞﾙ`, EVT-02; dumps may answer), `:394` (who calls `Gｶﾌｪ` with the Lady-unit
  entry?), `:395` (Shangri-La's date calculation vs `L_01881`; low priority), `:398` (Lelyukov's
  `{Prov_Owner \ 0: %0:}`), `:400` (`FIGT表示` has no survey; strategy/README.md `:23`).
- survey-ikami.md `:370` (is `Bt_IsDuel` ever stale at entry?), `:375` (the empty unit record 0's
  status, EVT-17), `:376` (the `N_Warg_Stage` 2 path, for telling the owner how a player gets there).

Other:

- battle/screen-layout.md `:731`: the `ZC 1` text colour is not traced per message — visual only;
  decision 3 probably closes it.
- spike-findings.md `:187`: did the fan translation change any logic? Parked by design until a
  discrepancy shows up.
- **Mark the answered questions answered** — *agent*, housekeeping: answered by later decisions,
  quirks or code but not marked in place — battle/README.md Q5 (and its copies 01-flow `:1362`,
  02-resolution `:1155`; 02-resolution's O1/O2 are resolved in battle/README.md only),
  spike-findings.md `:188`, survey-commands.md `:379` and `:382`, survey-data.md `:286`,
  survey-dungeon.md `:345`, survey-events.md `:388` and `:396`, survey-main.md `:334`, `:342`, `:344`,
  survey-nations.md `:338` and `:339`.

## Repository and tooling

- **`tools/sengoku/effect_frames.py` is one frame off** — *agent* (found 2026-10-15): in `Rance7GA.ald` an
  archive index is the CG number less 1 (`alice ar list`: `40000: cg40001.webp`), but the script passes the CG number
  to `ar extract -i`, so every reference sheet began at the effect's second frame and may end on a foreign one; the
  frame numbers in docs/sengoku/04-screen.md §2.4.1 may be shifted too. The owner took our effects as they are; fix
  the script and check §2.4.1.

- **Old absolute paths in the git history** — *owner*: rewriting the history is destructive, only on
  the owner's explicit request (decision 22).
- **README** — *owner*: About and Credits only so far (2026-10-11); installing, building and playing
  come with a release.
- **`--msgskip` cannot test the skip in `--auto` runs** — *agent*, low priority: MsgSkip stops at the
  first menu (`STOP_ON_MENU`), so an unattended run reaches the opening with it off; checking the
  opening's skip (2026-10-11) took a temporary line.
- **"2 ObjectDB instances were leaked at exit"** on every headless run — *agent*, low priority:
  harmless so far, not looked into.
- **Unverified facts in the brief** — *agent*: Godot 4.7's video playback (Ogg Theora only?) and
  AliceSoft's freeware release of the game ([project-brief.md](project-brief.md), "What is known").

## Later, after the whole port

- **The hybrid** on a separate branch: the original's content with Sengoku Rance's mechanics (brief,
  scope decision 2).
- **A remake** (brief, scope decision 3).
- **The Sparrow Mod port** (2026-10-12) — *owner*: survey done ([mods/sparrow-mod.md](mods/sparrow-mod.md));
  decision 24: a second variant on the shared engine, after the variant split
  ([variants.md](variants.md)), with the split's deferred parts (variants.md, "Deferred"). Waits for
  the mod author's permission (the owner wrote to them 2026-10-12; a refusal is expected).
