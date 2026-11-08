# Backlog

Everything put off, in one place: what it is, where its details live, what it waits for. Details
stay in the linked docs; this list only points at them.

- **Add** an item whenever work is put off (by the owner or by the agent), with the date and a link.
- **Remove** it when it is done or taken into a milestone ([strategy/README.md](strategy/README.md)
  holds the milestones and the owner's numbered decisions); git keeps the history.
- **Waits for:** *owner* — a decision or a play session of the owner's; *capture* — a run of the
  original game through the harness ([golden-tests.md](golden-tests.md)); *agent* — work the agent
  can do on its own.

Last swept: 2026-10-27.

## Release

The model is decided (brief, "Risks"; decision 23): only our engine is distributed, under
GPL-2.0-or-later, and the player brings their own copy of the game. What a release still needs
(discussed with the owner 2026-10-11):

The owner aims at a public release (decision 28, 2026-10-20).

- **The import's open ends** — *agent* ([import.md](import.md) is done, 2026-10-20): the start with no
  extract set has run on Android only (the first start imports the copied folder, 2026-10-24; "Android" below); on
  Windows it has not been run end to end (import.md, "Not run": the owner's `user://settings.cfg` sets the extract) —
  a release build's first launch is that start (Windows export, below); the content tests still read `_extract` and
  could move to the import; the music still goes through `_inmm.ini` (it is hashed with the rest).
- **Windows export** — *agent*: the preset `godot/export_presets.cfg` ("Windows Desktop") and
  `tools/export-release.ps1` export a release build outside the repository (2026-10-21; templates 4.7.2 mono
  installed on the owner's machine). The build keeps its data in `%APPDATA%\KichikuouGodot` (project.godot's
  `.template` overrides), apart from the development runs, so it starts as a new player's would; the fonts'
  OFL texts go into the pack (the include filter) and next to the exe (`licenses\`) with `LICENSE`. The version
  info is in the exe since 2026-11-08 (`0.1.0.0` from project.godot's version; [release.md](release.md),
  "Versions"). Still open: an icon, the exe's company and copyright fields (empty), the archive a player
  downloads, and the owner's look at a first launch.
- **The Android release APK** — *owner* (2026-11-08, [release.md](release.md)): the owner's release key is made and
  `tools/export-android.ps1 -Release` / `release.cmd` sign with it (tried with a throwaway key, not yet with the
  owner's). The owner's devices have the debug build: the release one installs only after it is uninstalled, which
  deletes the saves and the game folder in the app's folder — copy the saves off first (`copy-saves-from-tablet.cmd`),
  or give the development builds a package of their own (`GameData.AndroidPackage` names one today).
- **The Android player's way in** — *agent*: a player without adb copies the game folder into
  `Android/data/org.kichikuou.port/files/game` (a PC over USB; Android 11 and later keep a phone's own file
  managers out of `Android/data`); the README says nothing of it yet. A folder picked through Android's own
  dialog (a `content://` tree, a Java plugin) would spare that, later.
- **Android** — *agent* (the owner wants it, 2026-10-11): our own `.apk`, not Google Play; the tablet first,
  phones put off (owner, 2026-10-24: their mock-ups B1, B3, M1, M2 and the open questions are in
  [ui/touch.md](ui/touch.md), "Phones (put off)") and taken up again (owner, 2026-10-27): both orientations, the
  Xiaomi Poco X6 Pro the reference — survey and mock-up rounds in [ui/phone.md](ui/phone.md): every screen's base
  version chosen in both orientations (rounds P1-P19, 2026-10-27/28); the build's plan is
  [ui/phone-build.md](ui/phone-build.md) (owner, 2026-10-28: the three missing base versions first, rounds P20-P21;
  then the landscape phone, then upright; the tablet stays landscape) — the landscape phone built (2026-10-28, steps F
  and L1-L10) and upright (2026-10-30, steps P0-P9), both installed on the Poco, awaiting the owner's look and play there;
  the Poco's keyboard height (steps L7b, P7b) read from its log once the owner has tried Edit on Save/Load. Built and played on a Galaxy Tab S7 FE since 2026-10-24: the
  preset "Android" and `tools/export-android.ps1` (`run-android.cmd`); the game folder copied into the app's own
  external folder and imported at the first start (`GameData.AndroidGameFolder`: no permission, where the native
  folder dialog would give a `content://` URI that `System.IO` cannot open); the saves beside it
  (`GameData.AndroidSaveFolder`, `tools/copy-saves.ps1`), the save files one format on both (the PC's loaded on the
  tablet, 2026-10-25; uninstalling the app deletes them, their backups and the game folder); the build full screen,
  its bars hidden, Screen → Window showing the navigation bar (`ScreenModeChoice`); Android's Back (button or
  gesture) a right click at the last touch (`GameScreen.Touch.cs`, `quit_on_go_back=false`; checked on the tablet,
  2026-10-24); the libraries and the Godot project built for `net9.0` for the export (`GodotTargetPlatform`: Godot
  4.7's Android templates take .NET 9 only); the touch mode ([ui/touch.md](ui/touch.md)), played by the owner on the
  tablet since 2026-10-25. The speed in play, measured on the Poco (2026-10-28, ui/phone-build.md, step F, "The Poco"
  and "The battle's speed"): the scenes and the map at the screen's 120 Hz, the battle's exchanges at ~100 frames a
  second (~21 before: the scene's front layer read and uploaded again every shots frame, the surface's slow reads, the
  board laid out and the cards built again needlessly, and the libraries compiled unoptimized in the export builds).
  Still open: a long frame or two (40-140 ms) at an exchange's start on the phone — the engine's own drawing of its
  start, copies of 640×400 stored at 2× (a surface that stores at 1× what is not drawn at 2× would quarter them) —
  *agent*, put off 2026-10-28 (an effect's first play in a launch no longer draws its textures' masks: since 2026-10-28
  `EffectTextures.Prepare` draws all 43 on a worker at the launch, ~850 ms on the PC, 1060 ms on the Poco, a second after the
  launch, its time in the log);
  the hover a touch screen lacks, where the touch mode has not replaced it yet (ui/touch.md, "One by one, after the
  battle and the map"); the screen turning with the device (sensor landscape, 2026-10-24), not yet seen on the
  tablet; the touch mode's later parts, each marked "Not yet on the tablet" in ui/touch.md, and the damage
  forecast's touch rail ([ui/damage-forecast.md](ui/damage-forecast.md)), awaiting the owner's look there.
  A freeze on the Poco in a battle, seen once (owner, 2026-11-01), cause open: the main thread never left its frame; the
  build now reports the next one — [hang-watch.md](hang-watch.md), *agent* when a report comes.
  Memory on the Poco (owner, 2026-11-01/02): 1.9 GB after a battle, 2.9 GB in a long session, an empty Godot project 355 MB;
  measured, no leak, mostly ours: the findings and the ranked reduction plan are [memory.md](memory.md). Cuts 1–4 and 10 are
  built (~950 MB after a battle through the title, ~1.0 GB in a long session: the GPU's 256 MB block no longer taken), the
  backdrop looked at by the owner on the Poco, no seam; the stones kept at the layout's pixels (cut 10) await the owner's
  look on the Poco; *owner* picks from cuts 5, 6, 8 and 9, then *agent*. Put off 2026-11-03 with cut 10 (memory.md, "Cut 10",
  "What is left to the edge"): the launch lays every screen out landscape before it turns upright, whose font sizes and
  stones stay; `FacePictures` keeps every unit's and girl's picture panel (~1 MB on the GPU each) for the launch; the upload
  ring's cap stays at 16 MB. The probe's patches and the adb scripts stay outside the repository (its "How it was measured").
  Energy (owner, 2026-11-03): the frame-rate limit is built (docs/ui/options.md, "Frame rate"; 30 by default on Android);
  **not yet measured on the Poco** — *agent*: frame rate and the process's CPU time a second on a still map and in the held
  battle, before (3fe8803) and after, recorded in docs/memory.md as an energy section. Next candidates (owner's list):
  drawing nothing while nothing changes (Godot's low-processor mode; our endless animations — the waiting triangle, the
  pulsing gauge, the breathing target frame — stopped after a few idle seconds), and a garbage collection at quiet moments
  (the title after a battle 1042 → 857 MB after a forced one).
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
  window settings, which a survey must find. The port keeps its own saves in its own library (decision 29), so
  reading them would be an import into it.

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

- **Our own interface** — *owner* (which screen next), then *agent*: screen by screen over the original layout
  (decision 25, 2026-10-12), each surveyed and drawn as mock-ups first, in the house style
  ([ui/style.md](ui/style.md)); what is built is in CLAUDE.md's status and each screen's doc under docs/ui/.
  Decisions 3-4 (the player picks the UI) are superseded: since 2026-10-21 the player always has ours, and
  `--interface original` shows the original's screens for one launch ([window-menu.md](window-menu.md), Option →
  Interface). Put off (owner, 2026-10-18): the nations' phase banners ("Helman Phase", …) and their news stay the
  original's windows ([ui/turn-start.md](ui/turn-start.md), §3 and the questions). Still the original engine's: the
  merchant's and construction's screens ([ui/options.md](ui/options.md), "Thousands").
- **Quality-of-life features**, the README's second aim — *owner* (which), then *agent*; a survey of
  candidates first. What is built is in [strategy/README.md](strategy/README.md) (the owner's decisions 26-32) and
  [tweaks.md](tweaks.md) (the options that change the original's play). Open: the talk marks (decision 30, built
  2026-10-22, [ui/talk-marks.md](ui/talk-marks.md)) — the owner's review of the agent's decisions in §9, and a look
  at the ??? and read marks in a window (the kept saves predate the read-line record, so every talk and visit shows
  New there).
- **A mixed palette** — *owner* (2026-10-24, put off), then *agent*: one palette (Lacquer or Steel) colours all our
  screens since 2026-10-24 ([ui/options.md](ui/options.md), "The palette"); a palette per part (`UiPart`) is kept and
  read but no option offers it, nor colours chosen role by role.
- **The menus' row height** — *owner* (2026-10-25): Debug → Menu Row Height… sets the province menu's and the command
  panel's rows (38 px by default) for trying sizes out; once the owner settles on a height it becomes the default and
  the slider may go. Its second slider, the scenes' `]` menus (`SceneMenuView`), is settled at 40 px by default
  (owner, 2026-10-25) but still there. Our other questions (the callouts, the battle's dialog) do not follow them
  ([ui/command-screen.md](ui/command-screen.md), "The province menu", "The rows' height on a slider").
- **The import screen's hash line** — *owner* (how), then *agent* (found 2026-10-28 by the screen gallery's
  `import-refused-version`, [ui/gallery.md](ui/gallery.md), §7 step 7): a refusal of another version's file shows both
  SHA-256s on one small line under the message, not wrapped, so it runs past the paper and the frame
  (`ImportScreen.DrawRefused`). Wrapping it pushes the folder field down; shortening the hashes loses what a player
  would report.
- **A turn of the phone, the rest** — *agent* (put off 2026-10-30, [ui/phone-build.md](ui/phone-build.md), "Step P9,
  where it stands"): what a turn still loses, each small or brief — the upright map's own sideways scroll (centred on the
  script's view again after a trip to landscape: the script's camera is the one kept), the battle's order strip's
  fly-in (its chips snap to their places), a map plate pressed across a turn (its release still clicks its marker), a
  press on an original screen across a turn (the script reads its press and release at two points), the spotlight's
  0.15 s fade-out at the old places.
- **Phones shorter than 19.5:9** — *owner* (mock-ups), then *agent* (put off 2026-10-28,
  [ui/phone-build.md](ui/phone-build.md), §1): the phone's layouts are the reference's (20:9) and fit longer screens;
  on an 18:9 phone held landscape the black sides are ~7.6 mm, too narrow for LM1's rails, and upright 272 px less
  height is left for the scenes' windows and the bars — both need mock-ups of their own.
- **Optional fixes** — *owner* (which), then *agent*: the quirks register ([quirks.md](quirks.md),
  decision 5) lists every reproduced bug; some can become options. The first is BRS-24 (decision 32, Option →
  Tweaks → "Chaos and Nikkou reach demons"; [tweaks.md](tweaks.md)).

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
  quirks or code but not marked in place (line numbers as of 2026-10-27) — battle/README.md Q5 (`:29`; and its
  copies 01-flow `:1362`, 02-resolution `:1156`; 02-resolution's O1/O2 are resolved in battle/README.md only),
  spike-findings.md `:196`, survey-commands.md `:379`, survey-data.md `:286`, survey-dungeon.md `:354`,
  survey-events.md `:388` and `:416`, survey-main.md `:334`, `:346`, `:348`, survey-nations.md `:338` and `:339`.

## Repository and tooling

- **Old absolute paths in the git history** — *owner*: rewriting the history is destructive, only on
  the owner's explicit request (decision 22).
- **README** — *owner*: About, Installing (2026-10-20: what the player needs, building from source, the first
  start's import), Credits and License; a release download comes with a release.
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
