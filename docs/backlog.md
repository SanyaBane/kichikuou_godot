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

The owner aims at a public release (decision 28, 2026-10-20).

- **The import's open ends** — *agent* ([import.md](import.md) is done, 2026-10-20): the start with no
  extract set has not been run end to end (the owner's `user://settings.cfg` sets the extract; a release
  build has none); the content tests still read `_extract` and could move to the import; the music still
  goes through `_inmm.ini` (it is hashed with the rest).
- **Hide the development parts** in a release build — *agent*: the Debug menu, the capture viewer,
  the switches (`--auto`, `--watch`, `--ending`, …).
- **Windows export** — *agent*: the preset `godot/export_presets.cfg` ("Windows Desktop") and
  `tools/export-release.ps1` export a release build outside the repository (2026-10-21; templates 4.7.2 mono
  installed on the owner's machine). The build keeps its data in `%APPDATA%\KichikuouGodot` (project.godot's
  `.template` overrides), apart from the development runs, so it starts as a new player's would; the fonts'
  OFL texts go into the pack (the include filter) and next to the exe (`licenses\`) with `LICENSE`. Still open:
  an icon and version info (`application/modify_resources` is off), the archive a player downloads, and the
  owner's look at a first launch.
- **Android** — *agent* (the owner wants it, 2026-10-11): our own `.apk`, not Google Play. Godot 4.7
  exports C# to Android as experimental and needs .NET 9 or later (the project is on `net10.0`). It
  needs the import (done, [import.md](import.md); the player copies the game's files to the device; how a
  Godot 4.7 app reaches them under Android's scoped storage, and whether the native folder dialog does, is to
  be checked) and a touch interface (the font is
  done: BIZ UDGothic is bundled, docs/window-menu.md, Option → Fonts; `OFL.txt` ships as for
  Windows): the original is played with a mouse — the right button cancels (menus, the map, the empty
  dungeon menu, All Data's scale menu), the pickers' highlight and info panel follow the hover, keys
  drive the message skip and xsystem35's menu, and a menu line is 18 of 400 pixels (~3 mm on a 6.5"
  phone; every dependency in [ui/survey.md](ui/survey.md) §3). The touch interface belongs with our
  own interface (Interface, below). Unknown until
  measured: the speed of the engine surface's per-pixel C# drawing on a phone. A cheap spike first:
  the current build on a device, a tap as a left click. Its setup is made (2026-10-24): the preset
  "Android" (arm64, debug-signed, package `org.kichikuou.port`) and `tools/export-android.ps1` (`-Install`,
  `-PushGame`); the build finds the game folder copied into the app's own external folder
  (`GameData.AndroidGameFolder`, no permission, no folder dialog) and imports it at its first start. The
  libraries and the Godot project build for `net9.0` when Godot exports for Android (`GodotTargetPlatform`),
  since Godot 4.7's Android templates take .NET 9 only; everything else stays on `net10.0`. First run on a
  Galaxy Tab S7 FE (2026-10-24): the import takes ~5 s, the opening plays full screen (Vulkan, Adreno 619).
  Android's Back (button or gesture) is a right click at the last touch (`GameScreen.Touch.cs`,
  `quit_on_go_back=false`; the owner checked it on the tablet, 2026-10-24). The build starts full screen (bars
  hidden); Screen → Window shows the navigation bar with its Back, and the choice is kept for later launches
  (`ScreenModeChoice`). The screen turns with the device in both landscapes (owner, 2026-10-24:
  `display/window/handheld/orientation` sensor landscape, Android's `sensorLandscape`; not yet seen on the tablet).
  Still to measure: the speed in
  play, and the hover a touch screen does not have. A long press on a battle card opens its menu (2026-10-24).
  The touch mode for phones and the tablet (owner, 2026-10-24): survey and mock-ups in
  [ui/touch.md](ui/touch.md) (the battle's rails, the map); the tablet's B2 and M3 are built (2026-10-24), the target
  frame breathing on Android; still to try on the tablet. **Phones put off** (owner, 2026-10-24): the tablet first,
  phones later — their mock-ups B1, B3, M1, M2 and the open questions are in ui/touch.md, "Phones (put off)".
- **CI** — *owner* (a remote first), then *agent*: build, core tests, export. Without the game data
  the content tests no longer fail (2026-10-23): a test that reads the scenario, the CGs or a capture group
  is `[ScenarioFact]`/`[ScenarioTheory]`, `[LocalFolderFact]` or `[CaptureGroupFact]`/`[CaptureGroupTheory]`
  (`ScenarioTextGoldenTests.cs`), skipped with its reason; the helpers that boot a game (`EndGameRig.BootAsync`
  and those on it) no longer return null without the scenario. Without the data 304 pass, 336 are skipped.
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
  layout, the deployment table first — built (2026-10-12, our interface, its second part
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
  Then (owner, 2026-10-18): the messages between Turn END and the next menu, surveyed in
  [ui/turn-start.md](ui/turn-start.md); the week report is built. Put off (owner, 2026-10-18): the nations'
  phase banners ("Helman Phase", …) and their news stay the original's windows for now (turn-start.md, §3 and
  the questions).
  Then (owner, 2026-10-18): the message windows in our lacquer, variant A — the survey, the mock-ups and the
  owner's choices are [ui/messages.md](ui/messages.md); the battle's script windows put off above come with it.
  Steps 1-5 (the windows, 131, the `]` menus, letter by letter, the battle's windows and info boxes) are built (2026-10-18/19); step 6, the log, remains (§5).
  Put off (owner, 2026-10-18): **a message log** — wanted, its design discussed separately, the last step of
  the message windows (ui/messages.md, §3 item 4). Surveyed with mock-ups L1-L4 (2026-10-19, ui/messages.md, §5
  step 6); the owner chose L3 (the paper chronicle), the wheel up, the launch, skipped pages and choices logged;
  then its variant L3a (2026-10-19); built (2026-10-19), so the message windows' steps are all built.
  Then (owner, 2026-10-18): our own Subordinates screen (`Cﾌﾞｶ`/`Sﾌﾞｶ`) after mock-up F1 —
  [ui/subordinates.md](ui/subordinates.md).
  Its look is mock-up G3 (owner, 2026-10-18: lacquer panels, the table as a paper ledger; ui/subordinates.md, §5).
  Then (owner, 2026-10-18): **the deployment screen in the same look** — surveyed with mock-ups D1-D5 and E1-E6
  (2026-10-19, [ui/deployment-table.md](ui/deployment-table.md), "The lacquer look"); the owner chose E1/E2 (the
  Subordinates screen's layout, 2×2 lacquer cards, the gold drop highlight) and took Option → Palette off it; built
  (2026-10-19).
  Then (owner, 2026-10-19): **the Harem and Treasure screens** (`Sﾊｰﾚﾑ`, `Sｱｲﾃﾑ`) in the same lacquer — surveyed with
  mock-ups ([ui/pickers.md](ui/pickers.md)): the harem H2 chosen and built; the treasure S5 chosen and built
  (2026-10-19), then not accepted by the owner (no item icons for Sengoku Rance's layout); after mock-up rounds T and U,
  round V (the original's screen in lacquer) chosen and built in S5's place (2026-10-19); after the owner's look its
  refusals and questions became round W's W2, a callout at the clicked cell (built 2026-10-19), accepted by the owner
  the same day.
  Then (owner, 2026-10-19): the command panel's Load/Save as two rows, Load and Save, without the original's Load /
  Save / Don't menu between — built ([ui/command-screen.md](ui/command-screen.md), "The command panel").
  Then (owner, 2026-10-20): **our own Save/Load screen** in the same lacquer — surveyed with mock-up round 1 (A: a
  paper ledger, B: cards with each game's map, C: a list and the slot's page), [ui/save-load.md](ui/save-load.md);
  the owner chose C (2026-10-20), built after §5's plan; after the owner's look, each slot's own save time and the
  manual saves sorted by slot or by date with Auto in a panel of its own (mock-up rounds 2-3, the owner's E as F1-F4;
  [ui/save-load.md](ui/save-load.md), §7), built (2026-10-20).
  Groundwork (survey §4.8): menus as data and the map helper are done (2026-10-12); the shared engine
  host (§4.8, "Items 2 and 4 are one step") is not needed for screens over the original layout.
- **Quality-of-life features**, the README's second aim — *owner* (which), then *agent*; a survey of
  candidates first. Done so far: MsgSkip skips the opening whole (2026-10-11,
  [window-menu.md](window-menu.md), MsgSkip); a manual save is the game as it is now, not the turn's start
  (2026-10-19, owner's decision 26 in [strategy/README.md](strategy/README.md); STR-11 fixed). Our Save/Load screen
  tells them apart on a load (its note says what becomes of Auto; [ui/save-load.md](ui/save-load.md), §5, §8); the
  original's lists still cannot.
  Replenish from the deployment screen's Refill column, in a defence too while the week's troop order is unspent
  (owner's decision 27 in [strategy/README.md](strategy/README.md); [ui/deployment-table.md](ui/deployment-table.md),
  "Replenish on the deployment screen"), built and accepted by the owner (2026-10-20).
  Under way (owner, 2026-10-22): marks on Talk and the harem telling whether a talk or a visit can bring something
  new — the condition is surveyed in [ui/talk-marks.md](ui/talk-marks.md) (the rule, the random draws, the variables
  that count, decision 30 agreed as an option: an unread harmless variant comes for certain, Kanami chooses in
  Code's event 4); the look chosen from mock-up round 1 (§6). Built (2026-10-22, §8-§9); left: the owner's review of
  the decisions in §9 and a look at the ??? and read marks in a window (the kept saves predate the read-line record,
  so every talk and visit shows New there).
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

The 2026-10-11 list was answered on 2026-10-23 (each answer in place in its doc; new quirks BFL-26,
CMD-26, CMD-27, EVT-21 to EVT-24). Left from it:

- stage-2-sweep.md, open question 3: can `Gﾅｷﾞ` 3 (`:3424-3427`, Nagi and Ragarl back at status 1) follow
  Nagi's death? Not traced.
- A battle with no enemy (CMD-26's Use Force) has never been run in the port or our screens — *agent*, a
  test if it matters.
- Three variables stay below C ([spike-findings.md](spike-findings.md), open questions): `Cmdr_Item`,
  `VAR0333`, `F_Isoroku_Flag` — nothing reads them that could tell more.

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
- **Unverified facts in the brief** — *agent*: Godot 4.7's video playback (Ogg Theora only?) and
  AliceSoft's freeware release of the game ([project-brief.md](project-brief.md), "What is known").

## Later, after the whole port

- **The hybrid** on a separate branch: the original's content with Sengoku Rance's mechanics (brief,
  scope decision 2).
- **A remake** (brief, scope decision 3).
- **Extending the game** (2026-10-21) — *owner*: new story, provinces, heroes, nations, map and battle AI.
  Survey done ([mods/extensibility.md](mods/extensibility.md)): mods on the original, an extended fork, or
  a rule set of our own; its §7 asks the owner which wishes and which way.
- **The Sparrow Mod port** (2026-10-12) — *owner*: survey done ([mods/sparrow-mod.md](mods/sparrow-mod.md));
  decision 24: a second variant on the shared engine, after the variant split
  ([variants.md](variants.md)), with the split's deferred parts (variants.md, "Deferred"). Waits for
  the mod author's permission (the owner wrote to them 2026-10-12; a refusal is expected).
