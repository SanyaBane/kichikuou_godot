# KichikuouGodot

A faithful port of **Kichikuou Rance** (AliceSoft, 1996, System 3.x) onto our own engine with a
**Sengoku Rance**–style interface, built in **Godot 4 + C#**. The owner directs; the agent writes
almost all code.

**Read [docs/project-brief.md](docs/project-brief.md) first** — it holds the decisions, the
reasoning, rejected alternatives and unverified facts about the source game.

**Backlog: [docs/backlog.md](docs/backlog.md)** holds everything put off (release, saves, captures,
interface, the surveys' open questions, tooling). Whenever work is put off — by the owner or by
you — add it there with the date and a link to its details; remove an item when it is done or taken
into a milestone. Keep it in English, like everything in the repository.

## Status

Spike surface analysis done — see [docs/spike-findings.md](docs/spike-findings.md) for
measurements, data model, the local layout of the game and tools. Porting strategy decided (hybrid,
see "Porting strategy" in the brief). The variable dictionary [docs/variables.tsv](docs/variables.tsv)
is done; when reading the scenario, use the renamed copy (`tools/analysis/rename_vars.py`) and
keep the dictionary updated when a meaning is confirmed or corrected.

The **battle** (`FIGT*.ADV`) is ported to `src/Kichikuou.Original/Combat` following
[docs/porting-conventions.md](docs/porting-conventions.md) and the spec in
[docs/battle/](docs/battle/README.md); it has been reviewed against the script and has unit and
end-to-end tests. The golden-test harness is built ([docs/golden-tests.md](docs/golden-tests.md)):
a patched xsystem35 (the checkout `KICHIKUOU_XSYSTEM35_DIR`, branch `golden-harness`, built with
MSYS2 — see `docs/spike-findings.md`) records battles, `src/Kichikuou.Original/Golden` replays them.
37 captured battles match; the battle screen reproduces the original layout at 1280×800.

Now: the **strategic layer** (week loop, commands, nation turns, data, scenes). Surveys, parts
and the owner's decisions are in [docs/strategy/](docs/strategy/README.md); milestone 1 is the
whole prologue, playable. Record data and scenes are read at run time from the player's files.
F0 is done: the shared services (message windows, map, sound, scene calls) live in
`Script/` (the engine's runtime and the original's helper files). E1 and E2 are done: every scene file reads as a goto program
(`ScriptProgram`) and `SceneRunner` runs it as the context's story hooks. W1 is done: `COM.ADV`
(weekly upkeep and story triggers) is `Strategy/WeeklyEvents`. W3 is under way: `COMMAND.ADV`
(the player phase) is `Strategy/CommandPhase`, reading the pointer as the script does
(`ISceneInput`, `Script/PointerScript` for gesture-level UIs), and the prologue paths of the
command files `Cﾃｷﾄｼ`, `Cﾃｷﾄｼ2`, `Cﾌﾞｶ`, `Cﾊｰﾚﾑ`, `Cﾒｲｷｭｳ` are ported (their later paths throw
`NotSupportedException`; `Cﾃｷﾄｼ`'s attack, `Cﾌﾞｶ`'s troop menu and the rest of `Cﾊｰﾚﾑ` came with milestone 2), and so are the pickers `Sﾌﾞﾀｲ`, `Sﾌﾞｶ`, `Sﾊｰﾚﾑ`
(`Strategy/*Screen.cs` on `PickerScreen`), all routed by `Strategy/StrategyRules`. W2 is done:
`MAIN.ADV` (the week loop) is `Strategy/WeekLoop`, the thieves' week and the Leazas week with
every nation phase (the `T*` files are calls through the router). The battle
runs in the router too (`FIGT_A`/`FIGT_B`/`FIGT`/`死亡捕獲`), so the game's input is an
`Strategy/IGameInput` (battle + scenes; owner's decisions 6-7 in `docs/strategy/README.md`).
G0 (the Godot screens, owner's decisions 8-12) is done: part 1 ported `ﾛｰﾄﾞｾｰﾌﾞ`
(`Strategy/LoadSaveScreen`, the save commands in `Script/ScriptContext.Save.cs` on an
`ISaveStore`, `Script/SaveData.cs`); part 2 is the game driver `Strategy/GameSession` (boot,
new game, load from the title or `COMMAND`, resume after the autosave, `&OVER` as `GameEnd`)
and the view/input proxies; part 3 is the presenter `Kichikuou.Original.Content/Strategy/GamePresenter`
(decision 12: the scenes' drawing runs on the engine surface, map, windows, menus and pickers are
drawn by hand over it from the semantic `ScreenEvent`s and GameState); part 4 is the title and
`godot/Strategy/GameScreen` — the whole prologue is playable in Godot up to the Leazas week, which
is not ported. The owner's prologue capture run (2026-10-09, docs/golden-tests.md, "Findings so
far") matches the port everywhere, and so do two more runs: top-left map clicks (STR-12 confirmed) and
path A's week-23 duel (EVT-08 confirmed); README open items 1-2 are answered.
**Milestone 2** (owner's decision 13, 2026-10-10) is story stage 1 as a whole — the Leazas week,
the rebels, every command a stage-1 player has, the dungeon search — in the order of
docs/strategy/README.md, "Milestone 2" (with the sweep of what stage 1 reaches). Steps 0-3 are
done: every stage-1 file and every scene is a capture root, docs/golden-tests.md has the owner's
stage-1 checklist, the Leazas week is ported (the W2 row), and so are the nation template with
the rebels' turn `TCﾘｰｻﾞｽ` and their defender picker `TCﾘｰｻﾞｽ2` (the N1 row), and the attack in
`Cﾃｷﾄｼ` (`EnemyCityCommand`, the P3 row; the independent cities still throw). Step 4 is done:
`Cﾌﾞｶ`'s troop menu and the whole `Cﾊｰﾚﾑ` are ported (`SubordinatesCommand`, `HaremCommand`, both
files whole; the P4-P7 row). Step 5 is done: `Cｼｮｳﾆﾝ` and `Cｼﾊｲﾄｼ` are ported whole (`MerchantCommand`,
`OwnCityCommand`). Step 6 is done: `Cﾆﾝｼﾞｬ` and `Cﾃｲﾄ` are ported whole (`NinjaCommand`,
`ConstructionCommand`). Step 7 is done: `Cﾀｶﾗ`, `Sｱｲﾃﾑ` and `各種DATA` are ported whole
(`TreasureCommand`, `ItemScreen` on `PickerScreen`, `AllDataCommand`; semantic events
`PickerKind.Items`, `DataScreenEvent`) and their screens are drawn by hand (`PickerView`, the
presenter's All Data layer). Step 8 is done: `Cﾒｲｷｭｳ` is ported but God's Door (`DungeonCommand`, its
scenes in `DungeonCommand.Scenes.cs`; the boss and Hanny setters are record data run from the player's
files through `IScenarioSource.ReadBlock`; God's Door came with milestone 3 step 6). Step 9:
`--auto` plays the whole stage to stage 2 (`--seed 2`: Oaks in week 34, `Iﾏｼﾞﾝ` 1, stop in week 36,
nothing unported met). **Milestone 2 is closed** (2026-10-10): the owner's three stage-1 capture
runs, the end of the stage included, match the port whole (all 885 captures in the folder, none
reaching an unported path). **Milestone 3** (owner's decision 14, 2026-10-10) is story stage 2 as
a whole, up to the gate attack that starts stage 3: the sweep is docs/strategy/stage-2-sweep.md,
the order docs/strategy/README.md, "Milestone 3" (Helman, Zeth, the gate, the independent cities,
JAPAN, God's Door), the owner's capture checklist docs/golden-tests.md, "Capturing stage 2".
Step 1 (Helman) is done: `Tﾍﾙﾏﾝ2`, `Tﾍﾙﾏﾝ` and `Tﾊﾟｯﾄﾝ` are ported (`Strategy/HelmanDefenders`,
`Strategy/HelmanTurn`, `Strategy/PattonTurn`). Step 2 (Zeth) is done: `Tｾﾞｽ2` and `Tｾﾞｽ` are
ported (`Strategy/ZethDefenders`, `Strategy/ZethTurn`). Step 3 (the gate) is done: `Tｹｲﾌﾞﾘｽ2` is
ported (`Strategy/KayblisDefenders`). Step 4 (the independent cities, P4, `Cﾃｷﾄｼ.ADV:429-3828`) is
done, so `Cﾃｷﾄｼ` is ported whole: the cities' exits, the menu and Use Force are in
`Strategy/EnemyCityCommand.Cities.cs`, the negotiations (`:604-3648`) in `Strategy/EnemyCityCommand.Talks.cs`.
Step 5 (JAPAN) is done: `TJAPAN2` is ported (`Strategy/JapanDefenders`), and so is `TJAPAN` whole —
its rules in `Strategy/JapanTurn`, Gekkou's and Shinobu's attempts (`:295-2744`, ending 4) in
`Strategy/JapanTurn.Gekkou.cs`. Step 6 (God's Door, D2) is done: `Strategy/DungeonCommand.GodsDoor.cs`,
so `Cﾒｲｷｭｳ` is ported whole; `Iｶﾐ` runs from the player's files (not run end to end yet).
**Milestone 3 is closed** (2026-10-11, owner's decision 15): no `--auto` through stage 2, the stage-2
captures put off. `FIGTATTK` (the battle's animation, which the scene `FIGT表示` calls) is routed to the
battle's port. **Milestone 4** is story stage 3 as a whole: the sweep is docs/strategy/stage-3-sweep.md,
the order docs/strategy/README.md, "Milestone 4". Step 1 is done: `Tｹｲﾌﾞﾘｽ`'s frame (week S, the
recoveries, the event chain, the dispatch, the Grief Valley guards, the capture with Helman's and Zeth's
collapses) is `Strategy/KayblisTurn`. Step 2 is done: the generals' units (`:729-1328`: Babolat, Redeye,
Kite, Galtia, Kesselring) are `Strategy/KayblisTurn.Generals.cs`. Step 3 is done: Piar's unit
(`:670-728`) and Kayblis's army at war (`:1329-1508`) are `Strategy/KayblisTurn.Armies.cs`. Step 5 is
done: the Assault Leaders (`:326-624`) are `Strategy/KayblisTurn.Assault.cs`, so `Tｹｲﾌﾞﾘｽ` is ported
whole. Step 6 is done: `StageThreeEndTests` runs Kayblis's death (`死亡捕獲` → `Gｹｲﾌﾞﾘｽ` 10 →
`Iｼﾚｲﾌﾞ` 7 from the player's files) to stage 4 or ending 8. **Milestone 4 is closed** (2026-10-11,
owner's decision 16: no stage-3 capture, no `--auto` through stage 3). **Milestone 5** is story stage 4
(the Creator phase `Tﾙﾄﾞﾗ` and on to the game's end), a sweep first (docs/strategy/stage-4-sweep.md;
docs/strategy/README.md, "Milestone 5"). Step 1 is done: `Tﾙﾄﾞﾗ` is `Strategy/CreatorTurn` (on
`NationFile`, using none of the template), so N1 is done and nothing stage 4 reaches is left unported
but `OVER` (`GameEnd`). Owner's decision 17: `Iｶﾐ` gets a survey of its own before step 2's end tests,
and W4 (`OVER`, `ｽﾀMENU`) stays out of milestone 5 — every ending stays `GameEnd`. Step 2a is done
(docs/strategy/survey-ikami.md); step 2 is done: `IkamiEndTests` (content) runs `Iｶﾐ` end to
end from the player's files to endings 9, 10, 11 and 13, and `GodsDoorEndTests` the way there from
Kayblis's death — Kou at 8 when `Gﾙﾄﾞﾗ` plays, Ho Raga's Bonsai Statue, the four statues at God's Door,
`Iｶﾐ` to ending 11; Kou below 8: Ho Raga refuses and the door stays shut (`EndGameRig.cs` is their rig).
Step 3 (docs) is done: the quirks are registered (EVT-10 to EVT-20, NAT-31, DGN-19). **Milestone 5 is
closed** (2026-10-11, owner's decision 18: no stage-4 capture, no `--auto` through stage 4).
**Milestone 6** is W4 — `OVER` (the endings) and `ｽﾀMENU` (the start menu, with `HAPPY_C`), which
replace the minimal "THE END" of decision 10; a survey first (docs/strategy/README.md, "Milestone 6").
Step 0 is done (docs/strategy/survey-w4.md). Owner's decision 19: the three files run from the
player's files through `SceneRunner` (not hand-ported), after `ZZ0` back to the title, `ｽﾀMENU`
replaces our title; the gallery `CG` and the music room `MMODE` later. Step 1 is done: `OVER` runs
from the player's files (`OriginalVariant.IsSceneFile`; `ZZ0, 0` is `GameQuitException`, which
`GameSession` turns into `GameEnd`; Godot then goes back to the title), tested by `OverTests`.
Step 2 is done: `ｽﾀMENU` (with its logo `ﾌﾟﾛﾛｰｸﾞ`) is the title, run from the player's files by
`GameSession.TitleAsync` after the boot (which now keeps `ｽﾀｰﾄ.ADV:858`'s `Y 3, 10000`): New game,
Load, `Iｱﾘｽ`, `Gｻｲﾄｳ`, the requests slip and the debug starts through it, the gallery and the music
room unported (`TitleTests`); Godot's `Main` opens it, the capture viewer is in the Debug menu.
Step 3 is done: `HAPPY_C` (Fortune Check, the title's button 0) runs from the player's files
(`FortuneCheckTests`). Step 4 is done: the frame's quirks are registered (STR-25 to STR-42, STR-13).
**Milestone 6 is closed** (2026-10-11, owner's decision 20: no capture of the frame). **Milestone 7**
is the gallery `CG` and the music room `MMODE`, the last files the game reaches that do not run
(docs/strategy/README.md, "Milestone 7"). Step 0 is done (docs/strategy/survey-gallery.md): the
gallery reads the engine's CG load counter (`ZG Glob_CgSeen`), which the port does not count yet;
EVT-07 does not hold. Owner's decision 21: the port counts every CG it shows on every path into
`Glob_CgSeen`; `F1` past its string table reads an empty string. Step 1 is done: the boot's `ZG`
binds the counter and `ScriptContext.CountCgLoad` counts each load (STR-43) — every `G` of a scene or
a hand port through `ScriptContext.EngineAsync`, and where `WAO_SUB`, the map and the battle load
CGs they draw through their own events. Step 2 is done: the gallery `CG` runs from the player's files
(`OriginalVariant.IsSceneFile`, `GalleryTests`) and the agent's dumps mask it from its sheets to the
title's background (`GamePresenter.MaskEventCgs`). Step 3 is done: the music room `MMODE` runs from
the player's files (`MusicRoomTests`); `F1` past its string table reads an empty string (STR-44).
Step 4 is done: the gallery's and the music room's quirks are STR-43 to STR-57, EVT-07 is withdrawn.
Every file the game reaches now runs but the dead `XSPEC`. **Milestone 7 is closed** (2026-10-11,
owner's decision 22: no capture). Decision 22's next step is done: the absolute paths of the owner's
machine left the repository for `local.env`, which git ignores ("Local paths" below). Decision 23:
the project is GPL-2.0-or-later (`LICENSE`; README, "License").
Next (owner's requests 2026-10-11/12): an interface of our own next to the original layout, chosen
by the player, and an Android build later (docs/backlog.md). The game logic stays the original's.
The UI survey's parts 1-4 are docs/ui/survey.md. **The house style every screen of ours follows is
[docs/ui/style.md](docs/ui/style.md)** — read it before a new screen's survey and mock-ups, and keep it current; the screen gallery
(each screen of ours shown from the kept saves without playing to it, the battle board from a replayed capture; Debug →
Screen Gallery…, `godot/ScreenGallery`) is docs/ui/gallery.md, built (steps 1-5, 2026-10-25). Decision 25 (2026-10-12): our own screens come one by
one over the original layout, chosen by Option → Interface (since 2026-10-21 always ours, the original's
screens left to `--interface original`; BIZ UDGothic is the default font); the first, the deployment table over
`Sﾌﾞﾀｲ`'s grid, is built with the rest of its screen and its third part (the party as a formation with
the units' faces, drag and drop; Debug → Portraits) — docs/ui/deployment-table.md. The battle screen
followed (docs/ui/battle-screen.md); next (owner, 2026-10-16) the map and command screen, its survey
and mock-ups in docs/ui/command-screen.md; then (owner, 2026-10-18) the messages of the turn's start,
surveyed in docs/ui/turn-start.md — the week report is built, the nations' banners put off; then (owner,
2026-10-18) the message windows in our lacquer, variant A, the battle's included — docs/ui/messages.md; its step 1
(the windows 111/112/121/122/113/123) and step 2 (window 131, the speaker on a plate) and step 3 (the `]` menus, `Strategy/SceneMenuView`, mock-up M1) and step 4 (the text typed letter by letter, Option → Text Speed…, mock-ups T1 and S2) are built, and so is step 5 (the battle's windows: those it opens off the field, and its info boxes 143/153 as the owner's mock-ups K3 and K1 — a defence's badge (since 2026-10-21 a ribbon and a caption, D2 and D5), the results plate, our panel — a fall's box too, waiting for its click as the original's (owner, 2026-10-24; the stamp of 2026-10-21 gone); `Battles/BattleInfoBox`, `godot/Battles/BattleInfoLayer`), and so is step 6 (the message log, mock-up L3a: `Strategy/MessageLog` filled by `GamePresenter.Log`, the speakers by `Strategy/PortraitNames`, `godot/Strategy/MessageLogView` on the wheel up at a key wait and on `COMMAND`'s waiting map (owner, 2026-10-26), kept for the launch in `EngineState.Log`);
then (owner, 2026-10-18) our own Subordinates screen after mock-up F1 (no dialogue with Maris; the orders on our
buttons, answered through `SubordinateOrder`) — docs/ui/subordinates.md, built; its look (owner, 2026-10-18) is mock-up
G3, lacquer with the table a paper ledger, built, awaiting the owner's look in a window; then (owner, 2026-10-19) the
Harem and Treasure screens (`Sﾊｰﾚﾑ`, `Sｱｲﾃﾑ`) in the same lacquer — the survey and mock-ups are
docs/ui/pickers.md (`tools/ui/mock_pickers.py`): the harem is H2 with a Summon button, built (`godot/Strategy/HaremView`, `Original.Content/Strategy/HaremRoster`), looked at by the owner and changed after it; the treasure was S5 (Sengoku Rance's manner), built and not accepted (no item icons); after rounds T and U the owner chose V, `Sｱｲﾃﾑ`'s own screen in lacquer as the harem's, built in its place (`godot/Strategy/TreasureView`, `GameScreen.Treasure.cs`, the model `Original.Content/Strategy/TreasureRoster`: the picker's clicks); after the owner's look its refusals and questions are round W's W2, a callout at the clicked cell with the Yes/No inside (`godot/Strategy/TreasureCallout`), plus our own question before an item picked first replaces a character's (`TreasureRoster.ReplaceQuestion`), built and accepted by the owner (2026-10-19); then (owner, 2026-10-19) the command panel's Load/Save split into Load and Save rows, the file's Load / Save / Don't menu answered unseen (`Original.Content/Strategy/LoadSaveShortcut`; docs/ui/command-screen.md, "The command panel").
**Owner's decision 26** (2026-10-19), the port's first rule change on purpose: a manual save from `COMMAND` is the game as it
is now (`ScriptContext.SaveHere`, set by `WeekLoop` around its `COMMAND` call), resuming in that `COMMAND` after a load
(`CommandPhase.ResumeAfterSaveAsync`); loading it leaves Auto alone; the save window says so in our words (`PortTextEvent`);
STR-11 is fixed (docs/strategy/README.md, decision 26).
Then (owner, 2026-10-20) our own Save/Load screen in lacquer over `ﾛｰﾄﾞｾｰﾌﾞ`'s slot lists: the survey and mock-up round 1
are docs/ui/save-load.md (`tools/ui/mock_saves.py`); the owner chose C (a list and the slot's page, the map large, a red
button), built after the plan in its §5 (`Original.Content/Strategy/SaveSlots`, `godot/Strategy/SaveLoadView`), awaiting
the owner's look in a window. Then (owner, 2026-10-20) the Subordinates screen's troop orders ask first (mock-up C3, a callout
at the button, Replenish too) and report on a plate in the middle (R4/R5): docs/ui/subordinates.md, §6, built. Then (owner,
2026-10-20) each save slot keeps the time its game was saved (ENG-07), and the Save/Load list sorts its manual saves by
slot or by date with Auto in a panel of its own (mock-up rounds 2-3, the owner's E as F1-F4): docs/ui/save-load.md, §7, built.
Then (owner, 2026-10-20) Replenish on the deployment screen, in a defence too while the week's troop order is unspent —
**owner's decision 27**, the second rule change on purpose: the owner chose mock-up A1 (a Refill column of buttons, the
question at the cell, the Subordinates screen's plate), built (`SubordinatesCommand.CanReplenish`/`ReplenishNow`,
`OriginalScriptContext.ReplenishRequest` answered by `DeploymentScreen.ReplenishCode`, `Original.Content/Strategy/DeploymentReplenish`,
`godot/Strategy/GameScreen.Replenish.cs`, the plate now `godot/Strategy/OrderResultPlate`): docs/ui/deployment-table.md,
"Replenish on the deployment screen", looked at and accepted by the owner (2026-10-20).
Then (owner, 2026-10-20) a public release — **owner's decision 28**: the game imports the player's own folder
directly, xsys35dc translated to C# with its output byte-identical to `_extract\scenario` (every line reference
stays), the CGs and sounds read from the `.ALD` archives, the version checked by the files' hashes; the import is **done**
(docs/import.md, steps 1-9, 2026-10-20: `Content/Archives/AldArchive` reads the `.ALD` sets, `CgLibrary` the CGs and
`Content/Audio/SoundLibrary` the sounds from the archives or the extract alike, `Content/Archives/AinFile` the
`System39.ain`, `Content/Text/Sjis` decodes Shift JIS with xsys35c's own table; step 6 done: `Content/Decompiler`
is xsys35dc 1.13's decompiler translated, its 201 files byte-identical to the extract; step 7 done:
`Content/GameFiles` checks the folder's SHA-256 against the one known version; step 8 part 1 done:
`Content/GameImport` decompiles into a cache, Godot's `GameData` reads the import where no extract is set, `--import`;
part 2 done: `godot/ImportScreen`, the owner's mock-up B, at the start when there is no data; step 9: README "Installing"),
the release's other items
docs/backlog.md, "Release".
Then (owner, 2026-10-21) our interface goes from the province menu's Attack straight to the deployment screen: `Cﾃｷﾄｼ`'s
briefing (Rance's and Maris's lines, the same on every attack) is played unseen (`GamePresenter.CoverBriefings`;
docs/ui/command-screen.md, "The province menu"); so is (owner, 2026-10-24) `Cﾒｲｷｭｳ`'s before the searcher's picker
(Rance's "Alright, time to search." and Maris's line about the unit; the snow advice stays seen).
**Owner's decision 29** (2026-10-21): the ten slots give way to two lists — the last ten auto-saves (one each turn's
start, newest first) and as many manual saves as the player keeps, any of them deletable, a manual save with a comment;
a load never makes a save the auto-save; page 4 in a file of its own (STR-31). The plan and mock-up round 4 (G, H) are
docs/ui/save-load.md, §9; the owner chose G, built: the library `Engine/Script/SaveLibrary` (`DirectorySaveLibrary` migrates a
folder of slots the first time), `ﾛｰﾄﾞｾｰﾌﾞ`'s port on it while `ScriptContext.Library` is set (`ScriptContext.Library.cs`),
the screen in either interface.
**Owner's decision 30** (2026-10-22): marks on Talk and the harem telling whether a talk or a visit can bring anything
new, and, as an option on by default, what is unread comes first (harmless variants and the draws that change the
state given for certain once the usual scene is read), Kanami's choice in Code's event 4, Kentarou's hint lines —
surveyed in docs/ui/talk-marks.md, the marks' look chosen from its mock-ups (§6); **built** (2026-10-22, §8 steps 1-8,
the agent's decisions in §9): the read lines kept per save (`ScriptState.ReadLines.cs`), the engine's dry run
(`Script/ScenePreview`), the marks (`Strategy/SceneMarks`, `MarkTree`, `SceneMarkState`), the rule change on
`OriginalScriptContext.NewScenesFirst` (`Strategy/SceneChoices`; Kanami's menu and Kentarou's lines as scene hooks,
`Engine/Script/SceneHooks`, `Strategy/ScenePatches`), Option → Tweaks → "New scenes first" (`godot/Strategy/NewScenesChoice`),
the marks on our Harem and Subordinates screens (`Original.Content/Strategy/PickerMarks`).
Then (owner, 2026-10-23) the already-read text's colour after Rance X's: a message window whose every message the player
read before (the message skip's seen record, `msgskip.txt` in the save folder) shows its text in a colour chosen in
Option → Messages, Salmon by default — docs/ui/read-text.md (mock-up round R, 2: whole windows), built
(`TextBox.ReadBefore`, `GamePresenter.ReadBefore`, `MessageWindow.Read`, `godot/Strategy/ReadTextChoice`); system text —
the narration outside the event scenes but the story told there — never takes it (owner, 2026-10-26; its §6,
`Original.Content/Strategy/SystemText`); the battle's speech balloons take it too, darkened on the white balloon (owner,
2026-10-26, mock-up B2; its §7: `BattleSpeech.Read`, `SpeechBalloon.InkOf`, `ReadTextChoice.OnPaper`).
Then (owner, 2026-10-24) our own screen for an enemy attack (`FIGT_B`'s announcement and its Intercept / Ninja Scout /
Abandon City menus), the refusals and Ex's scenes kept seen: the survey and mock-up round 1 (A a card over the map, B a
screen of its own, C the original's scene with our menu) are docs/ui/enemy-attack.md (`tools/ui/mock_enemy_attack.py`);
the owner chose B with the Ninja Scout list (B4), the places filled in place after a scouting, Abandon City asked first,
Rance's repeated lines, the ninjas' lead-in and Abandon City's six lines unseen — built (its §6:
`Original.Content/Battles/EnemyAttack`, `BattlePresenter.Attack.cs`, `godot/Strategy/EnemyAttackView`,
`GameScreen.EnemyAttack.cs`).
Then (owner, 2026-10-24) a touch mode for the Android build: survey and mock-ups in docs/ui/touch.md
(`tools/ui/mock_touch.py`), phones put off, the tablet first. The owner chose B2 (the battle's rails in the scene's blurred
sides, the target preselected per unit) and M3 (a round Commands button on the map): built (docs/ui/touch.md, "The tablet":
`godot/Strategy/TouchMode` — Android or `--touch` —, `godot/Battles/BattleCommandLayer.Touch.cs`,
`Original.Content/Battles/TouchTargets`, `godot/Strategy/CommandsButton`); the target frame breathes on Android only
(the owner's A); the command panel on two rails at the thumbs, the other items left and the week's commands right, from
the foot up, Turn END on top, Close at the foot (the owner's A, 2026-10-25; the sides swapped by the owner, 2026-10-30, on
the phone too; docs/ui/touch.md, "The command panel within the thumbs' reach"); the province menu's card beside the tapped province, ringed in gold, its commands on the right rail
(the owner's B, 2026-10-25; "The province menu within the thumbs' reach"; `MapMarkers.PointOnView`).
Then (owner, 2026-10-25) the dungeon's floor menu, whose rows move between floors (Ascend / Descend / Retreat as offered):
the survey and mock-up rounds 1-2 (1 three fixed rows, the missing one dimmed; 3 a ladder right of the picture, in round 2
with the missing way hidden) are
docs/ui/dungeon-menu.md (`tools/ui/mock_dungeon_menu.py`); the owner chose round 2's ladder, built
(`Original.Content/Strategy/FloorLadder`, `godot/Strategy/FloorLadderView`; its §4); in the touch mode the ladder is a
rail in the lower right corner, Retreat on top and Descend in the corner (the owner's B, 2026-10-26; its §5), and so is
the menu before a search, Start Search in Descend's place (`Original.Content/Strategy/DungeonEntryMenu`; its §6).
Then (owner, 2026-10-26) an auto-save at Turn END too, the auto-saves telling a week's start from its Turn END: the
proposal and mock-up round J (J1 one list with a seal per kind, J2 a row a week, J3 two lists) are
docs/ui/save-load.md, §12 (`tools/ui/mock_autosaves.py`); the owner chose round K's K1 — **owner's decision 31**, built:
Turn END writes an auto-save of the moment (`CommandPhase`, `ScriptContext.SaveTurnEnd`), the library keeps ten of each
`AutoRing` (`end-NNNN.ksav` beside `auto-NNNN.ksav`), each auto row tagged "Turn Start" / "Turn End" under the time,
auto-saves take a comment and scroll as the manual saves do.
Then (owner, 2026-10-26) the deployment and Subordinates screens' ledger down the screen's left, a part of its columns
at a time in tabs (Overview, Troops, Commander), the picture and the party / orders stacked on the right: the proposal
and mock-up round A are docs/ui/unit-ledger.md (`tools/ui/mock_unit_ledger.py`); the owner took A1-A4 and round D's D2
(the order's gold seal; its §5), built (its §6: `Original.Content/Strategy/LedgerTabs`, `godot/Strategy/LedgerTabsBar`,
`LedgerTabChoice`), awaiting the owner's look in a window.
Then (owner, 2026-10-26) a damage forecast beside the battle's attack banners, an option: what the attack is likely to do
to both sides (troops lost, a fall's chance, the counter), computed on the real exchange run many times on a copy of the
state with a generator of its own; the survey and mock-up round A (A1 on each banner, A2 at the scene's foot where the
counters roll, A3 a tooltip, T1 the touch rail) are docs/ui/damage-forecast.md (`tools/ui/mock_damage_forecast.py`);
the owner chose A1 for the PC first, the damage dealt alone (the target's troops, its commander's HP where the attack
can reach him; no counter, no fall's chance), laid out as round B's B2 (two rows within the card); round C, the figures
in the cards' bars' red and green on blue banners, the owner's C1 (the player's blue, the figures lightened); round D,
the touch rail's attack buttons to match, the owner's D2 (gold, the damage on a dark strip); the banners blue always, the
option off by default, every question answered — built for both at once (its §8, 2026-10-27: the rule
`Combat/ExchangeForecast`, the attack setters, the target binding and the battle kind's modifiers split out of the battle;
the model `Original.Content/Battles/DamageForecast`, the forecasts of a `FIGT_SEL` run off the game's thread; Option →
Tweaks → "Damage forecast" (since 2026-10-27; Battle before), `BattleScreenSettings.Forecast`; `BattleCommandLayer` and its touch part); the PC's banners,
their type too small, laid out again after mock-up rounds E and F as the owner's F2 and accepted by the owner in a window
(2026-10-27); the touch rail awaiting the owner's look on the tablet.
Then (owner, 2026-10-27) the settings that change the original's play moved to a tab of their own, **Option → Tweaks**
(the owner's name; docs/ui/options.md, "Tweaks"): the damage forecast and "New scenes first", listed with their docs in
docs/tweaks.md. **Owner's decision 32**, the fourth rule change and the first fix of a registered quirk: BRS-24 (the
holder of Chaos or Nikkou, his troops over the battle's limit, cannot wound a demon commander) — Tweaks → "Chaos and
Nikkou reach demons", off by default, or the owner's A ("On the field": he fights in person, the demon's troops may shoot
at him) or B ("Out of reach": they never do), built (`Combat/DemonReach` on `OriginalScriptContext.DemonReach`,
`Battle.ComputeExchange`, the forecast's copy too; `godot/Battles/DemonReachChoice`; docs/tweaks.md).
Then (owner, 2026-10-27) the status strip always on `COMMAND`'s map, not only with the command panel, and the map's
room under it: near the map's top edge the picture is drawn lower by up to the strip's height, so the Great Wall of
Helman A (49) and the Tower of Evil (78) are no longer under it; the script's camera stays the original's (docs/ui/
command-screen.md, "The map's room under the strip"), built, awaiting the owner's look in a window.
Then (owner, 2026-10-27) the phone in both orientations, the Xiaomi Poco X6 Pro the reference, upright first: the survey
and mock-up round P1 (the scenes PS1-PS3, the map PM1-PM2, the battle PB1-PB2) are docs/ui/phone.md
(`tools/ui/mock_phone.py`); the owner chose PM1 and PB1 with our army on top (round P2's PB3), none of the scenes (left
open; then, round P14, PS2 changed — the picture in the screen's middle, the upper speaker's window above it and the
lower one's under it, no name plate — with PS2l, PS2b, PS2m, 2026-10-28). Base versions of every screen's mock-ups first,
nothing built meanwhile (owner, 2026-10-27). Rounds P3-P7 drew
our full screens upright; the owner chose PD2, PU1, PA1, PW1, PH2, PT2, PL2, PO1, PF1 (some changed) and the battle's
other states (docs/ui/phone.md, §5). The open questions upright (rounds P16-P17, 2026-10-28; its §20-§21): the opening
PB4o3 at the scene's place, the unit ledger in two-line rows scrolled sideways (PD2s2: № and name over the type fixed),
Subordinates' End at the thumb with the budget beside it (PU1e3), the bar's cells at the left (PK1), the orientation a
choice in Option → Display (System, Upright, Landscape 1, Landscape 2; PO1o), the script's line breaks kept. The landscape phone (round P8 on, the L… variants): the
frame keeps the layout, our controls go to the black sides; the owner chose the map LM1, the battle LB2, the deployment
table LD2 and Subordinates LU1 (both changed), the enemy attack LA2 (changed), the week report LW1, Harem LH2 and Treasure LT2
(the pickers' grid folded in two after its ninth row, every cell in view), Save/Load LL1 (the PC's split), the options
window LO1 and the dungeon's menus LF2 (the tablet's rail kept in the frame), and the battle's other states on LB2 (round
P13: the retreat question LB2r1 beside Retreat, the opening LB3o1 at the scene's place, the rest as drawn — the exchange's
counters and a speaker's portrait in the scene's blurred sides), and the scenes LS1 (round P15: the frame as on the PC,
Menu, Skip and Log in the left side, a dialogue menu's rows at the right thumb). All Data, the title and the import
screen, the last three, were drawn over their screen gallery entries (`all-data…`, `title…`, `import…`; docs/ui/gallery.md,
§7 step 7) as rounds P18 (upright) and P19 (landscape), docs/ui/phone.md, §22-§23; the owner chose All Data PG2 and LG2
(the graphs as tabs, the scales as chips), the title PE2 (the buttons' words in rows under the picture) and LE1 (the PC's
alone), the import screen PI and LI as drawn — so every screen of ours has a base version in both orientations, and
the owner confirmed every choice of the agent's in the rounds (its §5, questions 33 and 37). The build's plan is
docs/ui/phone-build.md (owner, 2026-10-28): the three missing base versions first (rounds P20-P21: the Menu sheet, the
message log, the comment's editing; drawn as docs/ui/phone.md §25-§26 with the original's own screens, over the new
gallery entries `fortune-check`, `cg-gallery`, `music-room`; the owner chose PN1 and LN3, PR2 and LR2, PY2 and LY1, the
original's screens as they are — step 0 done), then the landscape phone
(steps F, L1-L10), then upright (P0-P9); the reference
phone and longer screens; the tablet stays landscape. The gallery shows both orientations'
choices on tabs of their own (`gallery.ps1 -PhoneOnly`; the scenes' picture from the week-18 look's frames copied into
`screen-gallery/phone-look`, docs/ui/gallery.md, §9). Step F (the foundation) is built but the speed on the Poco
(phone-build.md, "Step F, where it stands"): `Original.Content/Devices` (`DeviceLayout`, `PhoneFrame`), read live by
`godot/Strategy/Device`; `godot/Strategy/ScreenFrame` is the one picture fit; on a phone the menu bar goes and the
picture stands in the phone's frame (`GameScreen.UpdateDevice`); `--phone landscape` plays one on the PC; the gallery's
real pass `-PhoneLand`; `FrameTimes` logs the frame times on Android. The battle's exchanges run at ~100 frames a second
on the Poco since 2026-10-28 (phone-build.md, step F, "The battle's speed"; the export builds compile our libraries
optimized, `Directory.Build.props`). Step L1 is built, awaiting the owner's look on the phone (phone-build.md, "Step L1,
where it stands"): the left side's Menu cell and the Menu sheet LN3 (`godot/Strategy/MenuSheetView`,
`GameScreen.Phone.cs`, on `Original.Content/Devices/PhoneMenu`), Skip and Log in the side, a `]` menu's rows at the
frame's right foot (since 2026-10-31 in the picture's middle over the asking window, both ways held — owner, so a tap at
the foot never answers one; phone-build.md, §5), the log LR2 and the week report LW1 (on `Original.Content/Devices/PhoneScenes`), each view's phone
branch keyed by `Device.Landscape(view)`. Step L2 is built, awaiting the owner's look on the phone (phone-build.md,
"Step L2, where it stands"): the map's status and week's tags in the sides, Commands at the right thumb, the command panel
and a province's commands as the sides' rails, its card beside it, 2 mm plates, no room under a strip
(`Original.Content/Devices/PhoneMap`; `StatusStripView.Phone.cs`, `CommandMenuView.Phone.cs`, `ProvinceMenuView.Phone.cs`).
Step L3 is built, awaiting the owner's look on the phone (phone-build.md, "Step L3, where it stands"): the battle's turn
— the scene at 70%, the taller cards, the rails in the sides, the battle's Skip on the left rail's top, the retreat
question beside Retreat, the exchange's counters in the scene's blurred sides (`Original.Content/Devices/PhoneBattle`;
`BattleBoardView`'s phone places, `BattleCommandLayer.Phone.cs`, `BattleDialogLayer.Phone.cs`, `TouchPill.Phone`).
Step L4 is built, awaiting the owner's look on the phone (phone-build.md, "Step L4, where it stands"): the battle's other
states — the opening at the lower scene band, the scout report across the width, the cut-in's band on across the sides,
the results plate and our panel in the frame's middle, the ribbon and caption, the balloon over the scene's foot, the
cards' menu at a finger's size (`Original.Content/Devices/PhoneBattleStates`; `ScoutReportView.Phone.cs`,
`BattleInfoLayer.Phone.cs`, the phone branches of `BattleOpeningLayer`, `StrategyCutInView`, `SpeechBalloon`).
Step L5 is built, awaiting the owner's look on the phone (phone-build.md, "Step L5, where it stands"): the deployment table
and Subordinates — the ledger in one-line rows at the phone's type (wholly at the left on the deployment table, the frame on
Subordinates; since 2026-10-31 a unit's face left of its name and bars of its troops (blue) and its commander's HP (green)
under it, upright under the type too, `DeploymentTableView.DrawNameCell`, phone.md §35-§38), the party two cards a row and Attack | Stop at the right, Add New Troops in the left side, Talk, Replenish
and End in the right, the questions in callouts at the phone's sizes, a unit's picture on a long press
(`Original.Content/Devices/PhoneLedger`; `DeploymentPanels.Phone.cs`, `SubordinatesView.Phone.cs`, `UnitPictureView`,
`DeploymentTableView.PlacePhone`, `LedgerTabsBar.Phone`, `TreasureCallout.PlacePhone`).
Step L6 is built, awaiting the owner's look on the phone (phone-build.md, "Step L6, where it stands"): the enemy attack
across the whole width — the province and the army at the left, the news, Maris's question and the answers in a column at
the right thumb, Ninja Scout's list above its button two ninjas a row, Abandon City's question above its
(`Original.Content/Devices/PhoneAttack`; `EnemyAttackView.Phone.cs`, `Battles/PhoneCommanderCard`, shared with the scout
report).
Step L7 is split: L7a is built, awaiting the owner's look on the phone (phone-build.md, "Step L7, where it stands"): Harem
and Treasure across the whole width — the title, the picture and the details at the left, the buttons at the left thumb,
the pickers' grid folded after its ninth row at the right thumb, every cell at its number's place, an item's name over its
holder (`Original.Content/Devices/PhonePickers`; `HaremView.Phone.cs`, `TreasureView.Phone.cs`, `PhonePickerParts`; the
callout's placement `TreasureView.ShowCallout`, the game's and the gallery's); L7b is built, awaiting the owner's look on
the phone: Save/Load as the PC's split across the whole width — the list at the left in one-line rows, the chosen save's
page at the right with the map cut lower, the red button | Back at its foot — and the comment's own card over Android's
keyboard (LY1; Godot's field asks for no full-screen editor, the card follows the height Godot reports, Back cancels)
(`Original.Content/Devices/PhoneSaves`; `SaveLoadView.Phone.cs`; headless `--phone landscape` now on a phone-shaped canvas).
Step L8 is built, awaiting the owner's look on the phone (phone-build.md, "Step L8, where it stands"): the options window
across the whole width as LO1 — its tabs on top, each tab in two columns, Defaults | Close at the right thumb, the PC's
parts drawn at PO1's type, their text at the canvas's size — and the dungeon's ladder and entry menu as the tablet's rail
in the frame at the phone's sizes (LF2, LF2e) (`Original.Content/Devices/PhoneOptions`, `PhoneLadder`;
`OptionsView.Phone.cs`, `FloorLadderView.Phone.cs`).
Step L9 is split: L9a is built, awaiting the owner's look on the phone (phone-build.md, "Step L9, where it stands"): All
Data with the graphs as cells in the left side, Back and the scale's chips in the right, a graph's figures drawn again
at the phone's type (`Original.Content/Devices/PhoneData`, `GamePresenter.DataFigures`; `godot/Strategy/AllDataView`,
`GameScreen.Data.cs`); the title and the original's own screens as on the PC, a long press their right click
(`GameScreen.OriginalScreens.cs`); a finger's press that lands elsewhere reaches the script after a read of its move, on
the tablet too (`Engine/Script/TouchPress`, `PlayerInput.Press`). L9b is built, awaiting the owner's look on the phone:
the import screen as LI, LIp, LIr — the paper sheet in the frame at the phone's type, the steps in two columns, the red
button at the right thumb, a folder its name over its place with Android's storage named
(`Original.Content/Devices/PhoneImport`; `godot/ImportScreen.Phone.cs`).
Step L10 is done but the owner's play (phone-build.md, "Step L10, where it stands"): unattended play headless with
`--phone landscape` from the week-18 save through every exercise (the deployment and the battle, `--treasure`,
`--saveload`, `--scout`, `--refill`, `--harem`, `--dungeon` to the cave's way out, weeks 18-21), all as before; the docs
(phone.md, touch.md, style.md §6, backlog). **The landscape phone is built, awaiting the owner's play on the Poco**;
upright (P0-P9) is next. Step P0 is built, awaiting the owner's look on the Poco (phone-build.md, "Step P0, where it
stands"): `PhoneFrame.Upright` (the picture at the width wherever a screen puts it, `PictureAt`; the room apart from it),
Option → Display's orientation on a phone (`godot/Strategy/OrientationChoice`: System — Android's `fullUser` —, Upright,
Landscape 1, Landscape 2), `--phone upright`, the gallery's `-PhoneUp` pass; a screen without an upright layout yet
shows the tablet's at the width between the hole and the bar. Step P1 is built, awaiting the owner's look on the Poco
(phone-build.md, "Step P1, where it stands"): the scenes upright — `PhoneFrame.Bar` at the foot with Menu, Log and Skip at
its left (PK1), the picture between the hole and it, the left portrait's window above the picture while it shows and every
other under it, the script's breaks at the phone's type (`PhoneScenes.Window`, `MessageWindow.TextRect`), a `]` menu's
rows in the bar's place (since 2026-10-31 across the picture's middle, Menu in the bar), the Menu sheet in rows from the thumb up (PN1, PN1q), the log with the names over the lines
(PR2), the week report at the width (PW1); the views keyed by `Device.Held(view)` (either way) or `Device.Upright(view)`;
a battle's windows off the field no longer leave the original's in the picture under ours (2026-10-31,
`BattlePresenter.HideWindow`; phone-build.md, "After the build: a battle's windows upright").
Step P2 is built, awaiting the owner's look on the Poco (phone-build.md, "Step P2, where it stands"): the map upright —
the status a band on top, the map's whole height at the width under it (`godot/Strategy/UprightMapView`, drawn from the
state's tiles and markers, scrolled sideways by our own camera), Commands at the bar's right, the command panel and a
province's card and commands as sheets over the map's foot, the map lifted under a province's sheet
(`Original.Content/Devices/PhoneMapUpright`; `GameScreen.UprightMap.cs`); a tap on a marker the script's view does not
show is the drag a player would make to bring it there, then the still click (`MapMarkers.PressAnywhere`).
Step P3 is built, awaiting the owner's look on the Poco (phone-build.md, "Step P3, where it stands"): the battle's turn
upright — the board 1280 layout pixels wide over the whole room, the scene at the width on top, our rows under it, the
gauge across between the front rows, the enemy's rows at the thumb, Skip on its strip, the deck at the foot (the commands
from the foot up, the attacks at the right thumb), the retreat question over the deck's right column
(`Original.Content/Devices/PhoneBattleUpright`; `BattleBoardView`'s upright places, `BattleCommandLayer.Upright.cs`,
`BattleDialogLayer.Phone.cs`).
Step P4 is built, awaiting the owner's look on the Poco (phone-build.md, "Step P4, where it stands"): the battle's other
states upright — the opening's plate on top of the scene and the names at its sides, the results plate over the dimmed
board at the width (growing up over Skip), our panel where it stands, the speaker's portrait at the scene's top corner and
our balloon beside it, a defence's ribbon and caption at held landscape's millimetres, the cards' menu at a finger's rows,
the scout report (since 2026-10-31 the ninja and the enemy one block in the middle over Rance's answer, the front row in
the bracket, the owner's PB4sf2m2, phone.md §34) and the strategy's cut-in as screens of their own (`Original.Content/Devices/PhoneBattleStatesUpright`;
the upright branches of `BattleOpeningLayer`, `SpeechBalloon`, `BattleCommandLayer`, `BattleInfoLayer.Upright.cs`,
`ScoutReportView.Upright.cs`, `StrategyCutInView.Upright.cs`).
Step P5 is built, awaiting the owner's look on the Poco (phone-build.md, "Step P5, where it stands"): the deployment table
and Subordinates upright — the title band, the party on top as our army in the battle (since 2026-10-31 its front row held
from below in the bracket, the owner's PD2f2, phone.md §32; `PhoneLedgerUpright.Bracket`, `Devices/PhoneBracket`,
`godot/Strategy/FacingBracket`, shared with the enemy attack's), the ledger at the thumb in two-line
rows (the seal, № and the name over the type fixed at the left, the tab's other columns scrolled sideways under them, each
tab its own scroll), the sends | Stop in the bar; Subordinates' orders under the ledger, Replenish and Talk on one row, the
budget at the bar's left and End at the right thumb, a question under its button; a unit's picture on a long press as a
sheet over the foot (`Original.Content/Devices/PhoneLedgerUpright`; `DeploymentTableView.Upright.cs`,
`DeploymentPanels.Upright.cs`, `SubordinatesView.Upright.cs`, `UnitPictureView`, `OrderResultPlate`); since 2026-10-31 the
plate after a troop order a card of its own in the middle over the screen dimmed 65% (the owner's PU1r1d2, phone.md §30;
`OrderResultPlate.Upright.cs`, `SubordinateResult.Bar`; the 65% on every device).
Step P6 is built, awaiting the owner's look on the Poco (phone-build.md, "Step P6, where it stands"): the enemy attack
upright — the PC's screen stacked, the head with the news, the province's picture with its figures beside it, "VS", the
army's cards two a row with the front row on top facing up at the province (since 2026-10-31 held from above in the PC's
bracket turned up, the owner's PA1f2a, phone.md §31; `PhoneAttackUpright.Bracket`), Maris's question, Abandon City | Ninja Scout
over Intercept in the bar; Ninja Scout's list and Abandon City's question above their buttons
(`Original.Content/Devices/PhoneAttackUpright`; `EnemyAttackView.Upright.cs`, the answers, the list, the cards and the
totals shared with `EnemyAttackView.Phone.cs`).
Step P7 is split as L7 was (phone-build.md, "Step P7, where it stands"): P7a is built, awaiting the owner's look on the
Poco — Harem and Treasure upright: the title band, the picture at the width, the details, (Treasure's tabs or a first
pick's prompt and Back,) the picker's grid in rows of 7.3 / 6.7 mm scrolled by a finger with the reference scroll bar
(since 2026-10-31 the owner's S2 from round P31, phone.md §40-§43: `Devices/PhoneScrollBar`, `UiPalette.DrawScrollBar`;
every phone scroll bar takes it since, on the paper in PB's vermilion; the PC's and the tablet's 8 layout px wide, round T's T2,
docs/ui/touch.md, `Devices/LayoutScrollBar`), the red button |
End in the bar, Treasure's questions across the room over it (`Original.Content/Devices/PhonePickersUpright` with its
`ScrolledGrid`; `HaremView.Upright.cs`, `TreasureView.Upright.cs`, `PickerGridScroll`); P7b is built, awaiting the
owner's look on the Poco — Save/Load upright as PL2: the title band with Load | Save, the tabs, the list in one-line rows
above the page (nine in view, a drag for the rest), the chosen save's page standing on the bar (the map cut at its foot,
the figures in two columns, the comment with Edit), the red button | Back in the bar; the comment's own card over
Android's keyboard as PY2, its words wrapped in a field two lines high and drawn by the view, the line edit taking the keys
unseen (`Original.Content/Devices/PhoneSavesUpright`; `SaveLoadView.Upright.cs`, held landscape's drawing shared through
its `Places`).
Step P8 is split as L9 was (phone-build.md, "Step P8, where it stands"): P8a is built, awaiting the owner's look on the
Poco — the options window upright as PO1 with PO1o: the title under the hole, the five paper tabs across the room, the
sheet down to the bar with each tab's settings in one column at PO1's type (Display's orientation on top as PO1o's
rows), scrolled by a finger with a thin bar, cut at the sheet's margins (the canvas item's clip, the frame drawn with it
ignored), Defaults | Close in the bar (`Original.Content/Devices/PhoneOptionsUpright`; `OptionsView.Upright.cs`). P8b is
built, awaiting the owner's look on the Poco — the dungeon's menus upright as PF1 and PF1e: the screen black, the floor's
picture cut from the engine's texture at the room's width, window 113's lines left of the rail (the view takes the window,
`FloorLadderView.TakeWindow`), the rail at the right thumb on the bar's foot (`Original.Content/Devices/PhoneLadderUpright`;
`FloorLadderView.Upright.cs`). P8c is built, awaiting the owner's look on the Poco — All Data upright as PG2m and PG2:
the title band, the map in miniature cut from the engine's texture at the room's width (the presenter leaves the graph
off it, `GamePresenter.DataGraphCovered`), a sheet with the graphs as finger rows or, over a graph, as tabs over the plot
laid out again from the presenter's bars (`GamePresenter.DataBars`) and the scale as eight chips in a row, Back in the bar
(`Original.Content/Devices/PhoneDataUpright`; `AllDataView.Upright.cs`). P8d is built, awaiting the owner's look on the
Poco — the title upright as PE2: the picture moved over a finger row for each button it draws (`PhoneFrame.WithPictureAt`,
`Device.PictureTop`; `Original.Content/Devices/PhoneTitleUpright`, `godot/Strategy/TitleRowsView`, `GameScreen.Title.cs`),
a row a click in its button's box; the import screen as PI, PIp, PIr, a card in the middle with the steps one under
another, the red button | Back in the bar (`Original.Content/Devices/PhoneImportUpright`; `godot/ImportScreen.Upright.cs`);
the original's own screens as they are, a long press the right click upright too.
Step P9 is done but the owner's play (phone-build.md, "Step P9, where it stands"): unattended play headless with
`--phone upright` from the week-18 save through every exercise, all as before, and again with the window turned every n
frames (`--turn-every n`); a turn keeps what the open screen showed — `Device.Turns` counts the layout's changes,
`FingerScroll` ends a press and a flick across one, the questions stand at their buttons again, the ledger keeps its
first row and the message log its reading place, the battle's toggles and portrait are laid out again; the rest is in
the backlog. **The phone is built in both orientations, awaiting the owner's play on the Poco.**
**Owner's decision 33** (2026-10-29), the fifth rule change: the ninja follow-up's chance (BRS-11, R14's `RND(3)` = 1) —
Tweaks → "Ninja follow-up", 33% (the original, the default), 67% or 100%, read off the same draw, built
(`Combat/NinjaChance` on `OriginalScriptContext.NinjaChance`, `Battle.NinjaRolled`; `godot/Battles/NinjaChanceChoice`;
docs/tweaks.md).
**The variant split is done** (decision 24, 2026-10-12): a shared engine and one rule set per game
variant, the original alone for now — the plan, its steps and what is deferred until a second variant
are docs/variants.md. The Sparrow Mod survey (docs/mods/sparrow-mod.md) is why; its port waits for its
author's permission.
`tools/analysis/port_draft.py` drafts text-heavy ranges for hand ports.
**Agent screen dumps mask the event CGs** (`GamePresenter.MaskEventCgs`, on with `--shots` and
`KICHIKUOU_SHOTS_DIR`); never look at unmasked ones.

## Layout

The code is a shared engine plus one rule set per game variant, the original alone for now
(docs/variants.md); the compiler keeps the layers: Engine ← Content, Engine ← Original,
Original + Content ← Original.Content ← Godot. All four libraries are `net10.0`, warnings are errors.

- `src/Kichikuou.Engine` — what works unchanged for any System 3.9 game, no rule of the original.
  `LocalPaths` reads the developer's local paths ("Local paths" below).
  `Sys3/` holds exact reproductions of System 3 / xsystem35 semantics: `Sys3Int` (16-bit
  saturating arithmetic — use it for every value that comes from the script),
  `Mt19937Legacy` + `XSystem35Random` (`Y 4,n`), `IScriptRandom` for injecting recorded draws,
  and `ScriptMemory` (variables, array pages, strings in the original engine's layout).
  `Script/` is the runtime: `ScriptState` (what the runtime sees: the memory, `RetVal`, the `DS`
  bindings, and `Extras`, the port's own data kept with the saves — no rule reads it), `VariantProfile` (the facts the runtime takes from a variant — variable count, scene
  rule, saved pages), `ScriptContext` (the engine's side: state, draws, pauses, scene calls, saves;
  helper calls go through the virtual `CallHelperAsync`), the shared events (`ScriptEvent`) and the
  view/input/hook interfaces, `SaveData` (the slot format, 4 since the player's `Comment`, 3 the slot's own time `Written`, ENG-07; formats 1-3 still load), `SaveLibrary` (ours, decision 29: `ISaveLibrary`, the auto-saves — ten of each `AutoRing`, a turn's start and its Turn END, decision 31 —, the manual saves, the persistent page; `LibrarySlots` gives the scenario's slot 1 the newest auto-save), `MessageSkip`, `PointerScript` (the
  gesture queue), `IScenarioSource`, plus
  `ScriptStatements` — scenario code read at run time: a goto program that mirrors the
  decompile line for line (`ScriptProgram`, one statement per engine command, jumps resolved,
  no idiom rewritten); `RunAsync` runs the straight-line `SET_*` data files and `SceneRunner`
  runs scene programs (`ISceneInput` adds menus, mouse and clock to the inputs).
  `Golden/` is the harness's capture format (`GoldenCapture`, `MemoryDump`).
- `src/Kichikuou.Original` — the original game's rules (references the engine only).
  `State/GameState` is the typed view over the memory: `GameState.g.cs` is **generated** by
  `python tools/codegen/gen_state.py` from `docs/variables.tsv` — never edit it by hand;
  re-run the generator after changing the dictionary. Rule code addresses records explicitly
  (`state.Units[state.Unit_Cur].Troops`) to keep the script's cursor quirks visible.
  `GameState` derives from the engine's `ScriptState` and carries the original's profile
  (`State/OriginalVariant`); `State/VariableCatalog.g.cs` (names, groups, page bindings) is
  generated with `GameState.g.cs`. `Script/` holds the original's `OriginalScriptContext` on the
  engine's context with the helper files `WAO_SUB` (message windows), `M`/`WAO_MAP` (map) and
  `PCMPLAY` (sound), their events (`HelperEvents`), the semantic screen events (`ScreenEvents`)
  and the screens' gesture geometry (`GesturePoints`). `Golden/` replays harness captures through
  the port (`GoldenReplay`, `MemoryComparison`). `Combat/` is the battle. `Setup/GameSetup` is
  the boot (`SET.ADV`) and the new game.
  `Strategy/` holds the strategic rule files: `WeekLoop` is `MAIN.ADV`, `WeeklyEvents` is `COM.ADV`, `CommandPhase` is
  `COMMAND.ADV`, `LoadSaveScreen` is `ﾛｰﾄﾞｾｰﾌﾞ.ADV`, the `*Command` classes (on `CommandFile`) are the `C*.ADV` command files, the
  `*Screen` classes (on `PickerScreen`) their `S*.ADV` pickers (`ItemScreen` is `Sｱｲﾃﾑ`), `AllDataCommand` is `各種DATA.ADV`, the nation files sit on `NationFile` (the
  template every `T*`/`T*2` repeats: priority list, picker, filler, troop sum, target walk; the
  army picker itself is on `CommandFile`, since `Cﾃｷﾄｼ` has a copy) as
  `*Turn` (`T*.ADV`, e.g. `RebelTurn`) and `*Defenders` (`T*2.ADV`), and `StrategyRules` routes calls of these files to their ports (the rule hooks of a `SceneRunner`).
- `src/Kichikuou.Content` — engine-independent access to the player's own game data (never
  part of the repository; references the engine only): `Archives/AldArchive` reads the game's `.ALD`
  archive sets and `Archives/AinFile` its `System39.ain`, `Text/Sjis` decodes their Shift JIS as xsys35c does
  (`SjisTable.g.cs` is **generated** by `python tools/codegen/gen_sjis_table.py <s2utbl.h>`; docs/import.md, steps 1, 4, 5);
  `Decompiler/ScenarioDecompiler` is xsys35dc 1.13 translated: the scenario archive and the `.ain` become the `.ADV`
  files of `_extract\scenario` byte for byte (step 6; `Cali` the expressions, `ScenarioDecompiler.Commands` the
  command table); `GameFiles` checks a game folder's files against the known version (SHA-256, step 7); `GameImport` imports a
  game folder: checked, its scenario decompiled into a cache (`ImportedGame`: the scenario folder, the CGs and
  sounds from the archives; step 8);
  `ScenarioText` reads message texts from the decompiled scenario by
  `ScriptRef`; `Scripts/ScenarioScripts` reads the data files' statements and whole files as programs
  (`ScenarioParser`; strictly: anything unexpected fails with its location); `Graphics/` decodes the CGs keeping palette indices (`IndexedPng`, `Pms`,
  `CgLibrary`, `Palette` with the engine's reserved entries; `ToRgba(40)` = `G n, 40`);
  `Audio/MusicTable` maps music numbers to files, `Audio/SoundLibrary` sound numbers to WAV bytes. `Screen/` emulates the engine's drawing
  surface (3200×800 at 2×, 8-bit indexed as the engine's: a palette load recolours what is
  drawn), its drawing commands (`EngineStage`: `G`/`CC`/`CS`/`CF`/`CM`, `WV`, the fades, the text
  state; its owner is told of the drawing through `IStageObserver`), message text (`TextBox`) and selection windows.
- `src/Kichikuou.Original.Content` — the original's presenters (references Original and Content):
  `Audio/SoundTable` maps the original's sound numbers to files; `Devices/` the device's layout (`DeviceLayout`) and
  the phone's frame (`PhoneFrame`, docs/ui/phone-build.md, §3);
  `Battles/BattlePresenter` draws every core event the way the original does
  ([docs/battle/screen-layout.md](docs/battle/screen-layout.md)), `FigtSelScreen` /
  `BattleMenuScreen` are the mouse-driven input screens. `Strategy/GamePresenter` draws a whole
  game: the stage (`Battle` shares it) and the hand-drawn layers (`TextBox`, `PickerView`, menus,
  All Data in `GamePresenter.Data.cs`) composed into `Screen`; a UI asks it what is open (`MapWaiting`, `Picker`, `Data`) and where a menu
  item is (`MenuHitTest`); a gesture UI turns clicks into the script's inputs with
  `PointerScript` (`Picker.HitTest`).
- `tests/` — xUnit, one project per library: `Kichikuou.Engine.Tests` (Sys3, local paths, the
  timer, the message skip and the pauses, on a `TestState` of plain variables),
  `Kichikuou.Content.Tests` (graphics, scenario text, the ALD and `.ain` readers, Shift JIS), `Kichikuou.Original.Tests` (the rules, the
  battle, the engine's runtime on the original's state; `Golden/GoldenBattleTests` replays the
  captures in `_extract\golden` (`KICHIKUOU_GOLDEN_DIR`) and is skipped when there are none) and
  `Kichikuou.Original.Content.Tests` (everything that reads the player's files: the differential
  tests, the scenes, the presenters; the golden part draws and plays every capture and resolves every
  text; its folders are the local paths, "Local paths" below). `Kichikuou.TestSupport` (on the
  engine alone) holds the doubles the tests share: `OneProgram`, `LoggingView`, `LoggingRandom`,
  `UnexpectedInput`, the `ScribblingHooks` frame, `RandomStates`, `Differential` (state copy, log
  and memory differences) and the like.
- `godot/` — Godot 4.7.2 .NET project (`Kichikuou.csproj`, namespace `Kichikuou.Game`, references
  the four libraries; its build does not make warnings errors, so check it at run time);
  presentation only: `GameData` is where the data comes from (the extract where one is set, else the import of the
  player's folder; docs/import.md, step 8), `ImportScreen` imports that folder when there is no data yet (mock-up B), or another one from Option → Game;
  `Main` opens the title (a `GameScreen` running `ｽﾀMENU`) or what the
  development switches ask for; `Strategy/GameScreen` runs one game from the boot (`GameSession` on a `GamePresenter`;
  mouse and keys are a state, `Strategy/PlayerInput`, that `IM`, `Y 3`, `IK 0` and the menus poll
  once a frame as xsystem35 does — the map and the pickers decide clicks and drags themselves;
  `PointerScript` gestures only for `--auto`; `GameScreen.MenuBar` is xsystem35's menu bar —
  Restart, MsgSkip on `Engine/Script/MessageSkip` (it skips the opening whole, `OriginalVariant.WholeScenes`, `Engine/Script/EngineTimer`),
  docs/window-menu.md, plus our Debug menu; `Strategy/EngineState` keeps
  the random generator and the message skip for the whole launch); `Battles/CaptureList` (Debug → Battle Captures) picks a capture, `Battles/BattleScreen` shows its battle;
  `Strategy/PortraitViewer` (Debug → Portraits: units', commanders' and every face as our screens cut them, `UnitFaces`, through `Strategy/FacePictures`, which the deployment screen's party cards share; the owner's edits of a square, `FaceCropEditor`, are kept in `user://face-crops.txt`, `FaceCropFile`; `--portraits` opens it);
  `Battles/Effects` (our exchange effects after Sengoku Rance's, drawn by code: `EffectTextures`, particles from a seed
  as `MeleeEffect`, `BowEffect`, `MagicEffect`, `IceEffect` (the ice magic, by the shouted spell or the caster's element: `Original.Content/Battles/MagicSpells`, docs/ui/mage-lines.md), `LightningEffect`, `DarkEffect`, `LightEffect` and `PoisonEffect` (the lightning, the dark, the light and the poison magic, the same way; the dark laid over dark, `EffectPlayer.Dark`), `RanceAttackEffect`, `KentarouSlashEffect` (Kentarou's Rance Attack), `WhiteBeamEffect` (the Black D. Beam: the same laid over dark, `EffectPlayer.Dark`; the beams each placed for its user: `ShizukaBeamEffect`, `MagicBeamEffect`, `EpoLightEffect`, `ChizukoBeamEffect`, `NagiBeamEffect`), `WhirlwindShotEffect`, `HighSpeedEffect` (By la Way: Rance Attack's in red, `RanceAttackEffect.BuildRed`), `KanamiEffect`, `FreyaEffect`, `KaoruEffect`, `WichitaEffect` (the ninjas' follow-ups, each her own and weak: a throw, the forked whip, the fists, the flaming sword; `NinjaEffect`, the Ninja troop's, unused), `VanishBombEffect`, `WarDanceEffect`, `ChuhaiEffect`, `GoliathEffect` (T2 Goliath's, 15 frames), `SoaringWaveEffect` (Rolex's T2 Soaring Wave, our own), `GuardEffect` (a defence's, after Sengoku Rance's guard effect 45080: the player's with the defence's badge, the enemy's at its redraw, `BattlePresenter.EnemyDefence`; `BattleBoardView.PlayGuard`, waited for by `GameScreen.GuardAsync`), drawn by `EffectCanvas` and laid over the scene by `EffectPlayer`; Debug → Effects, `EffectViewer` (a special over the sprite its user shows),
  `--effects` (`--effects white-beam-magic,black-beam-nagi --shots DIR` saves only those); played in the exchange from its shots by `BattleBoardView` (under MsgSkip stepped by the exchange's waits, each a tick at least), the choice `Original.Content/Battles/ExchangeEffect`, Option → Battle → "Exchange effects" — docs/ui/battle-screen.md, "The exchange's effects");
  `Strategy/DeploymentTableView` and `Strategy/DeploymentPanels` (our own deployment screen, our interface, `InterfaceChoice`;
  the owner's E1/E2 in lacquer, laid out again (2026-10-26, docs/ui/unit-ledger.md): the ledger down the left, its
  columns a tab's at a time (Overview, Troops, Commander; `Strategy/LedgerTabsBar`, the tab kept per screen by
  `LedgerTabChoice`), the hovered unit's picture panel (`FacePictures.PicturePanel`) top right, the party's 2×2 cards
  with one gold triangle right of them (the way the party faces), totals and bar below it — docs/ui/deployment-table.md,
  "The lacquer look"; `GameScreen` feeds the script its gestures, `Original.Content/Strategy/DeploymentTable` the rows);
  `Strategy/SubordinatesView` (our Subordinates screen over `Sﾌﾞｶ` and `Cﾌﾞｶ`'s troop menu, our interface,
  docs/ui/subordinates.md: drawn by hand in lacquer (mock-up G3) over a wave-patterned backdrop — the table down the
  left in the ledger's tabs, `DeploymentTableView` as a chooser and a paper ledger (`ledger: true`, not the palette's colours) with
  `Original.Content/Strategy/SubordinatesScreen.Columns` —, the status CG and the orders' panel on the right; a troop order asks first in
  a `TreasureCallout` at its button (`SubordinatesScreen.Question`, the owner's C3) and reports on a plate in the middle
  (`SubordinateResult`, R4/R5; docs/ui/subordinates.md, §6); an order is
  `Original.Content/Strategy/SubordinateOrder`, fed by `GameScreen.Subordinates.cs`: the cell's click, the menus and key
  waits answered unseen, the command opened again on COMMAND's map after a troop order; the figures are
  `SubordinatesCommand.Offer`/`AddMore`/`Refill`);
  `Strategy/MapMarkersView` (our name plates over the map in `COMMAND`'s map wait, our interface,
  docs/ui/command-screen.md, "The map's markers": the model `Original.Content/Strategy/MapMarkers`, a plate's click a
  still click at its marker's point, the camera's form `MapCameraForm`, placed at the view the screen shows,
  `GamePresenter.MapViewShown`; the dimmed ones hidden by Debug → Hide Dimmed Map Names (on by default), `MapNamesChoice`;
  a hovered province's card right of its plate, `MapMarkers.CardOf`: its figures for ours and the provinces the
  player has seen — `GameState.KnownProvinces`, in `Extras`, marked at each map wait and by the ninja's scouting —
  else "??");
  `Strategy/ProvinceMenuView` (our province menu over a city file's first menu, our interface, the owner's
  mock-up 4a in lacquer and gold: `Original.Content/Strategy/ProvinceMenu`, `GamePresenter.MenuCovered`,
  docs/ui/command-screen.md, "The province menu"; also the dungeon's menu after it; its and the command panel's rows 38 px on the PC unless Debug → Menu Row Height… says otherwise, `Strategy/MenuRowsChoice`, `--menu-rows n` for a launch; the `]` menus' rows its second slider, `--dialogue-rows n`);
  `Strategy/CommandMenuView` (our command panel over `COMMAND`'s right-click menu and its status window, Option →
  Interface → New, centred as the original's menu: `Original.Content/Strategy/CommandMenu`, Load/Save as two rows
  answered through `LoadSaveShortcut`, docs/ui/command-screen.md, "The command panel"); `Strategy/StatusStripView` (our status strip over the map's top whenever the screen
  shows `COMMAND`'s map (owner, 2026-10-27), near the map's top edge the picture drawn lower under it so no province
  stays hidden — `Original.Content/Strategy/MapHeadroom`, `GameScreen.Headroom.cs`, `MapRoomPicture`, the script's
  pointer turned back to the view's rows; "The map's room under the strip" —, in one row: the status window's lines and the week's uses in bracketed groups,
  `Original.Content/Strategy/StatusStrip`, "The status strip"; while a building is under way Construction's tag, there
  and in the panel, is half-lit with its weeks left, the strip's with a tooltip naming the building,
  `Original.Content/Strategy/BuildingUnderWay`, `UiPalette.DrawBusyTag`, "A building under way"); `Strategy/WeekReportView` (our week report over the
  week start's windows — the banner, the budget as an account, the troops replenished, a triangle asking for the click;
  our interface, docs/ui/turn-start.md: `Original.Content/Strategy/WeekReport` gathered by
  `GamePresenter.CoverWeekReport`, the windows' waits answered at once, the player's click at their close,
  `GameScreen.WeekReportAsync`); `Strategy/MessageWindowView` (our message windows 111/112/121/122/113/123 and 131 where the
  original has them — speech with a tail, thought, narration, a place's title, 131 with its speaker on a plate — the text at its cells in the game's font,
  typed letter by letter (its speed Option → Messages, the game's 121 typing as the preview, `Strategy/TextSpeedChoice`; a click while it types shows it whole, `GameScreen.FinishTypingAsync`),
  the triangle while the player's key is awaited, a menu over it shown on top; our interface, docs/ui/messages.md:
  `Original.Content/Strategy/MessageWindow` from `GamePresenter.MessageWindow` while `CoverMessages`, in a battle `BattlePresenter.MessageWindow`);
  `Strategy/SceneMenuView` (our `]` menus: lacquer rows at the selection box's place, `GamePresenter.MenuBoxArea`, while
  `SceneMenuCovered`; not over our battle screen; docs/ui/messages.md, §5 step 3); `Strategy/FloorLadderView` (the dungeon's
  floor menu as a ladder right of its picture, a way not offered left out: `Original.Content/Strategy/FloorLadder`,
  docs/ui/dungeon-menu.md);
  `Strategy/SaveLoadView` (our Save/Load screen over `ﾛｰﾄﾞｾｰﾌﾞ`'s lists on the save library, in either interface, the
  owner's layout G, docs/ui/save-load.md, §9 (decision 29): a switch between the manual saves — newest first, no order to
  choose (the owner, 2026-10-21; `SaveSlots.NewestFirst`), the number on the seal, scrolling, the comment a row's
  second line and a gold star on the seal, "+ New save" heading them in Save, "Delete all without ★ (n)" in their heading
  asking in a callout beside it (`SaveSlots.DeleteUncommented`; Sengoku Rance's notes, §10, the owner's N1) — and the
  auto-saves, newest first, scrolling, each tagged "Turn Start" or "Turn End" under the time (decision 31, §12, the
  owner's K1); the chosen save's page with its
  map as All Data draws it, its figures, the save's comment (Edit, written in place), Delete at the save's
  title (a danger button, its callout under it; the owner's B5, §11), a red button at the foot's left that
  answers the script's menu with the save's item and Back at its right (owner, 2026-10-30); Load | Save tabs from `COMMAND`,
  `LoadSaveShortcut.Switch`; the model `Original.Content/Strategy/SaveSlots` (`ReadLibrary`, `SaveShelf`) — each save's
  summary read from the `ISaveLibrary`, the menu's saves from `GamePresenter.SaveLibraryList` —, the window 113 under it
  not drawn, `GamePresenter.SaveScreenCovered`);
  `Strategy/MessageLogView` (the message log, a paper chronicle, mock-up L3a, in either interface: the wheel up at a key wait
  or on `COMMAND`'s waiting map opens it (`GameScreen.MapLogWait`), the game waits; `Original.Content/Strategy/MessageLog` from `GamePresenter.Log` — each key wait's page, a
  dialogue question's answer, `MenuAnswered` (an event scene's menu or one after a speech, never the play's; the play's narration left out, `MessageLog.Keeps`) — the speakers by the portraits' plates, `PortraitNames`; kept for the launch,
  `EngineState.Log`; docs/ui/messages.md, §5 step 6); our screens draw with `Strategy/UiPalette` (the style's colours by role and its parts, the ledger's, the click's triangle; `UiPalette.Drawing.cs`), each through its part's palette, `UiPalettes.For(UiPart.…)`, and `Strategy/Lacquer` (fonts, lines, baselines);
  `Battles/BattleBoardView` (our own battle screen, our interface, docs/ui/battle-screen.md:
  the cards on stones drawn by code in the palette's colours, `Battles/BoardStones` ("The board's backdrop"),
  `Original.Content/Battles/BattleScene` on top, the stage's windows over it through `WindowMask`, the
  cards and the round's order from `BattleBoard`/`RoundOrder`, the order strip `OrderStrip` (a new order at once; its chips fly in after the opening);
  the exchange's troops and HP counters at the scene's foot after Sengoku Rance's, `Battles/ExchangeCountersView` from
  `BattlePresenter.ExchangeCounters` (the cards run in their steps; Option → Battle → "Counters: run");
  `FIGT_SEL` answered by our own input, `BattleCommandLayer`: banners and context menus from
  `Original.Content/Battles/BattleCommands`; the retreat confirmation our dialog, `BattleDialogLayer`
  from `BattleDialog`; the speech bubbles' lines our balloon, `SpeechBalloon` from `BattlePresenter.Speech`; the info boxes 143/153 (a defence's ribbon on the card and its caption at the scene's foot, mock-ups D2 and D5, the results plate, our panel, a fall's box among the panels) `Battles/BattleInfoLayer` from `BattlePresenter.InfoBox` (docs/ui/messages.md, §5 step 5); the ninja's scout report, before the field, our overlay `Battles/ScoutReportView` from `BattlePresenter.Scout` (`Original.Content/Battles/ScoutReport`); a successful strategy, before the field, our cut-in `Battles/StrategyCutInView` from `BattlePresenter.Strategy` (`Original.Content/Battles/BattleStrategy`; one click, the strategist's line); the first round header our opening `Battles/BattleOpeningLayer` from `GamePresenter.Opening` (`Original.Content/Battles/BattleOpening`; the later headers answered at once; its fonts Shojumaru and Cinzel bundled in `godot/fonts`); the exchange's result held ("Results: hold") and answered by `GameScreen` unless a wiped-out mark is up; the order messages not shown, the card's order label flashing (`BattleBoardView.FlashOrder`, `BattlePresenter.OrderedUnit`); every card's stats always under its portrait, `Battles/CardPortrait` from `Original.Content/Battles/BattleCardStats` (no menu offers Info), the focus "◆ n" on it; the advantages as what they do ("Walls +8%", "Strategy +10%") and the battle's limit on a plate at the scene's top (`BattleOpening.Effects`, `LimitText`); its settings Option → Battle, `BattleScreenSettings`);
  the touch mode (`Strategy/TouchMode`: Android or `--touch`; docs/ui/touch.md, "The tablet"): our battle screen's rails
  (`Battles/BattleCommandLayer.Touch.cs`: the acting unit's commands left, the target's attacks right, the banners not
  shown; the target preselected by `Original.Content/Battles/TouchTargets` through `GameScreen.Touch.cs`, which also
  holds the long press on a card and Back as a right click; the other own cards' Auto-Atk / Auto-Def toggles at their foot,
  `BattleBoardView.OrderToggles`, the faces at the cards' top — docs/ui/touch.md, "The standing orders on the cards"; in lacquer and gold, the target's frame gold, the owner's A,
  docs/ui/touch.md, "The map's and the battle's buttons, round 1"), the map's `Strategy/CommandsButton` (a lacquer plate;
  a tap is the right click that opens the command panel, Close while it is open), the scenes' Skip and Log as two plates at the screen's left edge, on `COMMAND`'s waiting map too (the owner's L1, docs/ui/touch.md, round L) (`Strategy/SkipDrawer`,
  the owner's B1-B3b and G3; over 131 the window drawn smaller beside them, `MessageWindowView.Docks`) and the battle's Skip
  under its cards (`Strategy/TouchPill`), both in `GameScreen.Skip.cs`, their places `Original.Content/Strategy/SkipButtons`
  (Skip is MsgSkip's switch; the message log scrolled by a drag, closed by a tap off its frame; docs/ui/touch.md, "The Skip
  and Log buttons" and "The dialogue buttons, round 2"); the Subordinates and deployment tables scrolled both ways by a
  finger's drag with a flick, a row's press a tap at the release (`Strategy/DeploymentTableView.Touch.cs`), and so the
  Save/Load screen's manual saves, a row at a time (`Strategy/SaveLoadView.Touch.cs`), the finger's part
  `Strategy/FingerScroll` (docs/ui/touch.md, "The lists scrolled by a finger"); the Treasure screen's tap only selects
  and a red button under the picture acts, greyed with the picker's refusal (`TreasureRoster.Button`), the script's
  questions in a callout beside it (docs/ui/pickers.md, §6, the owner's round X);
  `Strategy/OptionsView` (Option: the options window over the game, the owner's mock-up D, docs/ui/options.md — a paper sheet with the tabs Messages, Display, Battle, Game and Tweaks — what changes the original's play, the damage forecast and "New scenes first" (owner, 2026-10-27); `--options` dumps them); Option → Display's palette (Lacquer, the default, or Steel; `UiPalettes.Chosen`, a part's own read but not offered yet; docs/ui/options.md, "The palette") colours all our screens, a change repainting them at once (`UiPalettes.Repaint`, `IUiPaletteUser`); the map's plates keep steel's colours as their own (`MapMarkersView`);
  `Battles/AdvantageTip` (the instant tooltips of the advantages next to the armies' names, `BattleAdvantage.Parts`); `GodotGlyphRenderer` (TextServer; MS Gothic or the bundled BIZ UDGothic in `godot/fonts`, Option →
  Display, `--font ms-gothic|biz-ud-gothic` for one launch), `BattleAudio`; `Screens` switches screens. Solution `Kichikuou.sln` at the root (`dotnet/project/solution_directory`).
- `tools/analysis/` — Python scripts for reading the decompiled scenario; `tools/codegen/` —
  generators for committed C# sources; `tools/golden/` — harness launcher and page classes;
  `tools/ui/` — mock-ups of our screens drawn over screen dumps (the images stay outside the repository).
- `docs/` — brief, spike findings, variable dictionary; `window-menu.md` records
  xsystem35's window menu bar (our reference; File/Screen/Option/MsgSkip) and what its commands do;
  `ui/survey.md` surveys the screens and input for our own interface; `sengoku/` surveys Sengoku
  Rance's battle from its code, the reference for our own battle screen (`tools/sengoku/split_jam.py`).

## Local paths

The paths of the owner's machine never enter the repository: they live in **`local.env`** at the
root, which git ignores (`KEY=value` lines; the committed template `local.env.example` lists every
key; an environment variable of the same name overrides a line). **Read `local.env` first** whenever
a command or a doc needs a folder or a tool: `KICHIKUOU_GAME_DIR` (the game), `KICHIKUOU_EXTRACT_DIR`
(the extract, `_extract` in the docs: `scenario`, `scenario_named`, `cg_png`, `cg_pms`, `wav`,
`golden`), `KICHIKUOU_GODOT_EXE` (the Godot console exe), `KICHIKUOU_XSYSTEM35_DIR`,
`KICHIKUOU_MSYS2_DIR`, `KICHIKUOU_ALICE_TOOLS_DIR` and the spike's tools (docs/spike-findings.md,
"Local layout"); `SENGOKU_RANCE_GAME_DIR` and `SENGOKU_RANCE_EXTRACT_DIR` (docs/sengoku/README.md). `Kichikuou.Engine.LocalPaths` reads it for the tests and the Godot game's default
folders, `tools/golden/capture.ps1` for the harness. Docs and code name a key, never a path; never
commit `local.env`.

## Commands

- Tests: `dotnet test tests/Kichikuou.Engine.Tests`, `tests/Kichikuou.Content.Tests`,
  `tests/Kichikuou.Original.Tests` (seconds; "the core tests" below are these three) and
  `tests/Kichikuou.Original.Content.Tests` ("the content tests").
  The content tests run in two sizes (`DifferentialRuns`). The quick run, the default, takes an eighth
  of two things: the random cases of the differential tests (a port against its file run from the
  decompile on random states — the command phase, the command slices, the nation turns, the week
  loop, the weekly events, the load/save screen — and the presenter's audit, `GamePresenterTests.Audit`)
  and the captured battles the battle-screen tests replay (`BattleSceneTests.Captures`: the scene,
  the opening, the result waits, the strikes, the windows, the exchange effects, the scout report).
  `KICHIKUOU_FULL_TESTS=1` runs them all (the whole project ~8 min, its long tests spread over classes
  that xUnit runs in parallel, `Shards.cs` in the content tests; only the full counts fail a test
  whose random states never reached a path it lists). **Run the full counts only where they can find
  what the quick run cannot** (owner, 2026-10-14): a change reaches the scaled tests whose code it
  changes — run those, filtered, with the full counts
  (`KICHIKUOU_FULL_TESTS=1 dotnet test tests/Kichikuou.Original.Content.Tests --filter "FullyQualifiedName~ExchangeEffectTests"`);
  the whole project only when the change reaches what many of them share — the rule code the
  differential tests compare, the script runtime, the content readers, the presenter's drawing. A
  change no scaled test reaches (docs, comments, Godot-only code, a class only unscaled tests read)
  needs the build, the core tests and its own tests only. Run a due full run once, when the change is
  settled, not while the owner may still ask for changes (owner's request 2026-10-11). Say in the
  reply which run a result comes from and why the full counts were or were not due.
- Build everything: `dotnet build Kichikuou.sln`
- Read the player's game folder as the release does, not the extract: add `--import [DIR]` to any game switches
  (DIR, else the saved game folder; imported into `user://import` first when its cache is not whole).
  The import screen: `<Godot console exe> --path godot -- --import-screen [DIR] [--shots <dir>] [--quit]` (with DIR
  unattended, into a fresh `user://import-look`; its dumps need a window).
- Build and play in a window without Rider or the editor: `run-game.cmd` at the root (a double-click), or
  `powershell -ExecutionPolicy Bypass -File tools
un-game.ps1 [-Console] [-NoBuild] [game switches]` — it builds
  `godot/Kichikuou.csproj` and starts `KICHIKUOU_GODOT_EXE` (the exe without `_console` unless `-Console`) on `godot`,
  passing the rest after `--`. `run-game-touch.cmd` does the same with `--touch`: the Android interface on PC, the mouse
  playing a finger (no hover; docs/ui/touch.md). `--phone landscape` (any launch; implies `--touch`) plays a phone held
  landscape: the window at the reference phone's shape fitted to the monitor, no menu bar, the picture in the phone's
  frame (docs/ui/phone-build.md, §3); `--phone upright` one held upright (Option → Display's orientation then turns the
  window); with either, `--turn-every n` turns the window every n frames, upright to landscape and back (unattended
  play through turns, step P9). `--frame-times` logs the frame times every 10 s, as the Android build always does.
- Export the Windows release build (the export templates 4.7.2 mono installed):
  `powershell -ExecutionPolicy Bypass -File tools/export-release.ps1 -Out <folder outside the repository>` — the
  preset `godot/export_presets.cfg`, the licenses copied next to the exe. The build keeps its data in
  `%APPDATA%\KichikuouGodot`, not the development runs' `app_userdata\Kichikuou`; deleting that folder makes the next
  launch a first one (the import screen with no folder chosen).
- Export the Android test build (JDK 17 and the Android SDK set in Godot's editor settings), install it on the USB
  device and start it: `run-android.cmd` at the root (a double-click), or
  `powershell -ExecutionPolicy Bypass -File tools/export-android.ps1 -Install -Launch [-Out <apk outside the repository>] [-PushGame] [-NoExport]`
  — the APK by default in `%LOCALAPPDATA%\KichikuouGodot\android`, the device checked before the export (several:
  `ANDROID_SERIAL`), the app's data kept, the game folder copied when the device has none (`-PushGame` always)
  (docs/backlog.md, "Android").
- Copy the saves between the PC and the Android device (the Android build keeps them in its external
  `…/files/saves`, `GameData.AndroidSaveFolder`): `copy-saves-to-tablet.cmd` / `copy-saves-from-tablet.cmd` at the
  root, or `powershell -ExecutionPolicy Bypass -File tools/copy-saves.ps1 -ToDevice|-FromDevice [-Release] [-PcSaves <dir>]`
  — the PC's side the development runs' `user://saves` unless `-Release` or `-PcSaves`; the source's saves replace
  the destination's, which move first into a backup on the destination, `save-backups/<time>` next to its saves
  folder (never pruned); adb in any USB mode.
- Run the game headless (smoke test): `<Godot console exe> --headless --path godot --quit-after 30`;
  `<Godot console exe>` is `KICHIKUOU_GODOT_EXE` in `local.env` (the editor's exe is next to it).
  Commit the `*.uid` files Godot creates: a new script's `.uid` is not made by the smoke run above nor by unattended
  play (2026-10-24: none appeared for `BoardStones.cs`); the import pass makes it at once —
  `<Godot console exe> --headless --path godot --import` (run after its `.cs` was committed). So after adding a script
  run the import pass, and after every Godot run check `git status` for new `*.uid`; commit them (with their script's
  change when it is still open, else in a commit of their own).
- Watch a capture headless and save the screen at every key wait and long pause:
  `<Godot console exe> --headless --path godot -- --watch <capture> --fast --quit --shots <dir>`.
- **Our interface is the default** (owner, 2026-10-21: Option → Interface is gone, the player always has ours):
  `--interface new` in the commands below changes nothing any more, `--interface original` shows the original's
  screens for the launch. So unattended play in a window runs our screens' exercises, which move the system pointer,
  unless `--interface original` is given.
- Play the game unattended headless (slots in `user://saves-auto`; `--load` resumes them):
  `<Godot console exe> --headless --path godot -- --newgame --auto --fast --quit [--seed n] [--shots <dir>] [--weeks n] [--stage n] [--interface new]`;
  one visit each to Harem, Subordinates and the hideout in the prologue and to Treasure and All Data
  at stage 1 (after one search of the Monsters' Cave with the weakest unit: down to a battle, Retreat,
  Finish); from stage 1 once a week a replenish (`Cﾌﾞｶ`) and an attack on the rebels (from their
  second week, while Rance has 3/4 of his troops), defences with the strongest; Turn END every week;
  ends at the first file or path not ported yet ("not ported yet"), at a game over, after week 30
  (`--weeks n`) or two weeks after story stage `--stage n` is reached — `--seed 2 --weeks 60 --stage 2`
  takes Oaks in week 34 and stops in week 36, ~15 min (exit code 0; 1 after an error). `--interface new`
  builds the parties in our own deployment table and swaps the first two places by drops and back,
  and in a window, once a launch, dumps the week report (`week-report`), each kind of our message windows
  (`message-speech-left`, `message-speech-right`, `message-thought-left`, `message-thought-right`, `message-narration`,
  `message-place-title`; the kept save `saves-week26` reaches all but the right thought; 131 as `message-cg-bar-speech`, `-thought`, `-narration`, with `--harem`), the message log (`message-log`, once 20 pages are logged; plain `--auto` in a window shows it too, without moving the pointer, and opens it by the wheel up over the first map wait with something logged — with `--touch` by a tap on the drawer's Log, `touch-map-drawer` —, `message-log-map` — the week-26 save reaches it; `--turn-end --weeks 26` keeps that run's pointer still), the battle's info boxes (`info-defence`, `info-results`, `info-level-up`, `info-panel`; the week-18 save played to the week's end, `--weeks 18`, reaches all but the level-up, a fall's box as `info-panel`), the first map wait's plates (`map-markers`, `map-markers-hover`, `map-markers-active`), the province menu (`province-menu`) and
  clicks the week's attack target through its plate (`map-marker-hover`; Godot's input, the system pointer
  moved), and answers the first turn through the pointer on our command input
  (the acting unit's menu, another own card's, closed by a click off it; Retreat, whose dialog it
  cancels; back at the turn, another own card given the order it has already, whose label flashes;
  each advantage's tooltip, each enemy card's banners, two sample menus' two and three banners never
  answered, then a click on the target's Melee banner; dumps `menu`, `menu-orders`, `retreat`,
  `advantage-tip-1`…, `banners-E1`…, `sample-2-banners`, `sample-3-banners`, with the damage forecast whatever the option
  `forecast-E1`…, `forecast-sample-3-banners` (with `--touch`: `touch-rails-forecast`), `banner-hover`, and without `--fast` `order-flash`, `slide`
(halfway through the first wait for the armies' slides; `--army-seconds 1` makes them long enough) and each exchange effect once, at its peak (`effect-melee-player`, …); it moves the system pointer,
  so leave the mouse alone); its dumps need a window (no `--headless`). With `--until battle` the exercise ends
  the run once its attack's exchange is over (owner, 2026-10-15: the rest of the battle only cost time), so the
  battle's result is not reached.
- **A look at one screen** (screen dumps for the owner) starts from a kept save, never from the
  prologue (owner's requests 2026-10-12): `user://saves-week18` holds the owner's own game at week 18
  (LP 3, month 5, week 2; stage 1, the Leazas week) — the one to use, chosen by the owner (2026-10-13)
  because no event plays at its turn's start — and
  `<Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look --slots-from user://saves-week18 --until battle [--interface new] --shots <dir>`
  copies it into a working folder (the kept one stays unchanged), plays the week's attack on Oaks (6)
  — its deployment picker and its battle: Rance (unit 1), Haurein (unit 12), Cordoba (unit 8) and
  Barres (unit 6) against Ex (commander 1), Sakanaku (commander 6), Dodge (commander 5) and Jibul
  (commander 7), lost with `--seed 2` — and stops (`--until deployment` stops after the picker; in a window with
  `--interface new`, after the battle screen's exercise and its attack). A check of a later turn of that battle stops
  with it, never with the week (owner, 2026-10-20): `--until battle-end` ends the run at the battle's end whatever the
  exercise, ~100 s in a window without `--fast` — Barres's (unit 6) Defend and Dodge's (commander 5) defence are in it,
  their Guard dumped as `effect-guard-player` and `effect-guard-enemy`; `--until round-N` ends it already when the
  battle's round N starts (owner, 2026-10-20: the rounds after the one checked only cost time) — Barres and another
  back-row unit defend from round 1 (`--until round-2`, ~45 s), Dodge first in round 3 (`--until round-4`, ~70 s); the
  log names the round of each Guard. A load
  replays the week from its autosave (a manual save is a copy of it, `LoadSaveScreen`): the load
  list, the banner, income and expenses, then the command menu. `--load --auto` always loads the load list's first item,
  the newest auto-save, so a kept folder holds one `slot01.ksav`: the owner's slot copied under that
  name, which the copy's library makes its auto-save (decision 29; the kept folder itself stays as it is). It cannot be made again by a command; when the save format or the boot changes, ask the owner
  for a new one. The older kept save `user://saves-week26` (the bot's own game, the second Leazas week:
  the attack on South (4)) plays the week's events (Lia's scene, Maris's schedule) first. Making it
  again: first week 25, the first Leazas week, ~2 min headless —
  `--newgame --auto --fast --quit --seed 2 --weeks 24 --saves user://saves-leazas`; then week 25
  ended at once and the first attack on it (on North (2)) repelled by Rance (unit 1), Menad (unit 10) and Asuka
  (unit 11) (the owner's choice; the second, on Wes (8), with the bot's own party), seconds —
  `--load --auto --fast --quit --seed 2 --turn-end --party 1,10,11 --weeks 25 --saves user://saves-week26 --slots-from user://saves-leazas`.
  The scout report (owner, 2026-10-14): `--turn-end --scout` on the week-18 save ends the week at once; the rebels
  attack Ys (5) the same week and `--scout` chooses Ninja Scout in `FIGT_B`'s menu at that first enemy attack (once
  a launch; the ninja menu takes its first item, the first free ninja: Kanami) — both menus clicked as a player clicks
  them (press, held six frames, release; the system pointer stays), and the key wait after each must find no click
  left, else the run fails (the bug of 2026-10-16, `Engine/Script/InputLatch`) — then it intercepts as usual —
  `--load --auto --fast --quit --seed 2 --saves user://saves-scout --slots-from user://saves-week18 --turn-end --scout --until deployment [--interface new] --shots <dir>`
  (Ex (commander 1), Sakanaku (commander 6), Dodge (commander 5), Jibul (commander 7); our overlay needs a window,
  and `--until deployment` stops before the battle, so nothing moves the system pointer). In our interface the attack
  is our own screen (docs/ui/enemy-attack.md, §6), answered without the pointer: in a window it dumps `enemy-attack`,
  `enemy-attack-ninjas` (the list open), `enemy-attack-scouted` (the places filled) and `enemy-attack-abandon` (the
  question, answered Keep), then intercepts, leaves the first deployment screen by Back (§7: the log says how soon
  the screen was back) and dumps `enemy-attack-back`, then intercepts again.
  A harem scene (window 131 under an event CG, 2026-10-18): `--harem <cell>` on the week-18 save makes the first
  map wait open the menu, choose Harem and pick the girl in that cell of its picker (left to right, top to bottom:
  Lia 2, Maris 3, Kanami 6, Haurein 9), before the replenish and the attack —
  `--load --auto --fast --quit --seed 2 --saves user://saves-look-131 --slots-from user://saves-week18 --harem 6 --until deployment [--interface new] --shots <dir>`.
  The Treasure screen (2026-10-19): `--treasure` on the week-18 save makes the first map wait open Treasure (the
  L. Holy Sword (item 6) on Rance (unit 1), By Lord (item 7) on Rick (unit 7)); with `--interface new` unattended play
  clicks through our screen — Rance picked and the pick dropped, his sword taken off (the script's question answered
  Yes) and given back (picked first and offered to Rick, whose cell asks our question, answered No, then to Rance),
  then given again (refused, "Already equipped.") — and in a window dumps `treasure`, `treasure-hover`,
  `treasure-which-item`, `treasure-items`, `treasure-unequipped`, `treasure-question` (the callout),
  `treasure-replace` (our question), `treasure-equipped`, `treasure-refusal`; else it plays the original picker (`AutoItemSteps`) —
  `--load --auto --fast --quit --seed 2 --saves user://saves-look-items --slots-from user://saves-week18 --treasure --until deployment [--interface new] --shots <dir>`.
  Add another kept save the same way when a screen needs another phase.
  Our Save/Load screen (2026-10-20): `--saveload` on the week-18 save makes the first map wait open the command panel's
  Save row; in a window it saves a new manual save by the red button, asks the sweep of the saves without a comment and
  keeps them, writes its comment through Edit, asks Delete and
  keeps it, shows the auto-saves, opens the Load tab, chooses the newest auto-save, writes its comment and leaves by Back, then plays on — dumps `save-load-title` (the
  title's load list at the `--load`), `save-load-save`, `save-load-button`, `save-load-saved`, `save-load-sweep`, `save-load-comment`,
  `save-load-delete`, `save-load-autos-save`, `save-load`, `save-load-auto` —
  `--load --auto --fast --quit --seed 2 --saves user://saves-look-saves --slots-from user://saves-week18 --saveload --until deployment --interface new --shots <dir>`.
  Replenish on our deployment screen (2026-10-20, decision 27): `--refill` keeps unattended play from replenishing
  through Subordinates; with `--interface new` it replenishes once a launch through the deployment table's Refill button
  instead (the unit with the fewest troops of its maximum: Haurein (unit 12) on the week-18 save), the question answered
  Yes — in a window dumps `deployment-refill-question` and `deployment-refill-done` —
  `--load --auto --fast --quit --seed 2 --saves user://saves-look-refill --slots-from user://saves-week18 --refill --until deployment --interface new --shots <dir>`.
  A dungeon battle (owner, 2026-10-21): `--dungeon` makes the first map wait search the Monsters' Cave (dungeon 2) at
  once with Rance (unit 1) — no replenish, no attack that week; on the week-18 save its first floor meets Sergeant
  Hanny, and the cards' fade-in is dumped as `opening-appearing` and `cards-sliding` (a window, without `--fast`) —
  `--load --auto --quit --seed 2 --saves user://saves-look-dungeon --slots-from user://saves-week18 --dungeon --until round-2 --interface new --shots <dir>`.
  The map's room under the strip (owner, 2026-10-27; docs/ui/command-screen.md, "The map's room under the strip"):
  `--map-north` in a window makes the first map wait drag the map through Godot's input as a player drags it (the
  system pointer stays) — strokes west to the Great Wall of Helman A (49), then one north to the map's top — and fails
  unless the pressed row stayed under the pointer and the Great Wall's plate lies below the strip; dumps `map-north`
  and ends the launch —
  `--load --auto --fast --quit --seed 2 --saves user://saves-look-north --slots-from user://saves-week18 --map-north --shots <dir>`.
  **Only when the owner asks for it, never in a general run or as a routine check** (owner, 2026-10-27): the window's
  real pointer reaches the same input, and the owner works on the machine meanwhile — a mouse moved over the window
  broke a run (the pressed row strayed −53 to 17 rows). No test, script or other switch runs it.
- Show an ending's `OVER` (its picture, staff roll, Completion Report) on a booted game:
  `<Godot console exe> --headless --path godot -- --ending <1-14> --auto --fast --quit [--shots <dir>]`
  (without `--auto` the keys wait for the player; slots in `user://saves-ending`).
- Look at the options window: `<Godot console exe> --path godot -- --options --shots <dir> --quit` opens it over the
  title and saves each tab (a window; nothing moves the pointer).
- Render our exchange effects' frames (Debug → Effects; docs/ui/battle-screen.md, "The exchange's effects"), in a
  window — the pointer is not moved: `<Godot console exe> --path godot -- --effects --shots <dir> --quit`. Sengoku
  Rance's reference frames come from `tools/sengoku/effect_frames.py`, into a folder outside the repository.
- **The screen gallery** (docs/ui/gallery.md): our screens in their states from the kept saves, no playing to them, the
  battle board from a golden capture replayed into a presenter and held at a moment (`ScreenGallery/GalleryBattle`), the
  title run from the player's files after a fresh boot and held the same way (`GalleryTitle`), the import screen held in each phase —
  `powershell -ExecutionPolicy Bypass -File tools/ui/gallery.ps1 [-Steel] [-Touch] [-PhoneLand] [-Phone] [-Vfx | -VfxOnly] [-Only id,…]`
  (`-Vfx`: the Combat VFX tab, each exchange effect as six frames, `--combat-vfx`; **only when the owner asks for it or the
  tab itself changes**, owner 2026-10-29; docs/ui/gallery.md, §11) builds, dumps every
  entry (`-Only`: those, into the passes as they are) into `screen-gallery/` at the root (git ignores it; `KICHIKUOU_UI_GALLERY_DIR` elsewhere) in a window, ~33 s a
  pass, nothing moves the pointer, and writes its `index.html`, every screen in its place, a changed one badged with a link to its former picture, a
  find, a filter of the changes and a viewer (before/now, the box round what changed; `-IndexOnly` writes the page alone; docs/ui/gallery.md, §5) (two dumps of
  the same build are identical: our views animate by `Strategy/UiClock`, which the dump steps a frame at a time); the game alone:
  `<Godot console exe> --path godot -- --screen-gallery [id,…] --shots <dir> [--previous <dir>] --quit`. After a change
  to a screen in it, run it and look at what changed; a screen's look starts there before a kept save's run. Its
  state comes from `user://saves-week18-command`, the week-18 save saved in `COMMAND` by `--save-command DIR` (any
  `--load --auto` run: at the first map wait in `COMMAND` it saves as a manual save there and quits; the command is in
  docs/ui/gallery.md, §2) — made again with the kept save it comes from. `-Phone` adds a tab of the phone's chosen
  mock-ups, not built (docs/ui/gallery.md, §9; `-PhoneOnly` draws that tab and the index alone, no build, no window;
  its Python `KICHIKUOU_PYTHON` in local.env). `-PhoneLand` is the real pass of the phone held landscape (`--phone
  landscape --window 1808x813`, the whole window dumped at two thirds of the reference's pixels; the dump's window is
  `--window WxH`, else 1280×800), `-PhoneUp` the phone held upright's (`--phone upright --window 610x1356`, half the
  reference's pixels). For the owner's website, `tools/ui/gallery-deploy.ps1` copies the current pictures as
  WebP with the page into a new `screen-gallery-deploy/<time>/` at the root (git ignores it), the gallery unchanged
  (docs/ui/gallery.md, §10).

## Rules

- **Faithful port first.** Reproduce the original's mechanics and balance one-to-one; modernise
  only the interface. Do not "improve" rules. A Sengoku-mechanics hybrid comes later, on a
  separate branch, only after the full port.
- **Register every quirk.** Bugs and oddities of the original are reproduced as is and each one is
  recorded in [docs/quirks.md](docs/quirks.md) (stable ID, cited as `// QUIRK: <ID>`), so some can
  be offered as optional fixes later.
- **Engine-independent core.** Game rules live in `Kichikuou.Original` (on the shared `Kichikuou.Engine`), .NET class libraries with no
  Godot references, covered by unit tests. The Godot project only renders state and forwards input.
  Never put rules in UI code.
- **Godot 4 only.** Use Godot 4 C# APIs; never Godot 3 idioms (`yield`, old method/signal names).
  When unsure about an API, check the docs for the installed version.
- **Never commit original game assets or extracted data** — the player supplies their own files.
- **License: GPL-2.0-or-later** ([LICENSE](LICENSE), decision 23). Code translated from a
  third-party source (xsystem35's C, `mt19937-1.c`, …) gets a header naming the file and its
  copyright holders, as `Sys3/Mt19937Legacy.cs` and `Graphics/Pms.cs` have; reproducing behaviour
  needs none. Never copy code whose license is not GPL-2.0-or-later compatible.
- **Verify before relying.** Facts marked "unverified" in the brief are hypotheses; confirm them
  and update the brief.
- The alice-tools source (`KICHIKUOU_ALICE_TOOLS_DIR`) is a read-only reference; do not modify it.
- **Name records.** The owner cannot tell provinces, units or commanders apart by number: in docs
  and replies never write a bare record ID — name first, ID after it: "Oaks (6)", "Oku (3), South
  (4) or Baranchi (7)", "Kinkaid (unit 9)", "Ruberan (commander 31)"; the same for dungeons ("the
  Monsters' Cave (dungeon 2)"). Provinces: the `MS 8` strings of `SET_MAP.ADV` (record `VAR0158`);
  units and commanders: `MS 1` / `MS 4` of `SET_CHR.ADV` (records `Unit_Cur` / `Cmdr_Cur`; some
  scenes rename a record at run time — `MM Unit_StrBase…` / `MM Cmdr_StrBase…` — so check the
  context); dungeons: docs/strategy/survey-dungeon.md §2.1. Items the same ("the Doping Drug
  (item 48)", "Chaos (item 1) or Nikkou (item 2)"): the `MS 7` strings of `SET_ITEM.ADV` (record
  `Item_Cur`).
- IDE is JetBrains Rider.
