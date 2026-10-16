# Our own map and command screen — the plan (2026-10-16)

Owner's choice (2026-10-16): after the battle screen ([battle-screen.md](battle-screen.md)), our own
screen for **the strategic map and the command menu** — `COMMAND.ADV`'s player phase
(`Strategy/CommandPhase`) — first a survey and two or three mock-ups. Chosen by the same setting as
our other screens (Option → Interface → New), coloured by Option → Palette. **The rules stay the
original's**: `CommandPhase` and the command files run unchanged and decide everything; the screen
draws what they show and answers what they ask with the gestures they expect (`PointerScript`), as
the deployment table does.

## The original screen (survey)

Line numbers are `CommandPhase.cs`'s unless named; the decompile is `COMMAND.ADV` (its line numbers
are the `Ref(n)` calls).

### The map wait

- **Only the map is on screen.** `MapAsync` (`:183-289`) raises `MapScreenEvent(true)` and polls the
  mouse; no window, no text, no menu. **Nothing follows the hover** (the cursor shape changes only
  while a button is held, `IC 6`).
- **The map** is 1920×800 script pixels, six tiles of 640×400 (`Ui_MapCgTopL/TopM/TopR/BotL/BotM/BotR`,
  CGs 812, 802, 804, 801, 803, 805; `GameSetup.Set.cs:235-240`); the view is 640×400 around
  `MapCamX/Y` (its centre, 320-1600 × 200-600; `:188-193`). `BattlePresenter.DrawMapTiles`
  (`BattlePresenter.Engagement.cs:397-418`) draws tiles and markers off screen, `BlitMapView` copies
  the view.
- **The markers** are the owners' flags (`Marks[]`, `Ui_MarkCount`): CG `Prov.Owner + 819` (24×19)
  at the province's `MapX`, `MapY` − 18, set by `M` op 4 when a city changes hands. The town pictures
  are part of the tiles. There are no army markers.
- **Scrolling:** any movement while the left button is held drags the map, which coasts after the
  release (`WAO_MAP` op 1, `OriginalScriptContext.Map.cs:49-134`); another button during a drag snaps
  back.

### A click on the map

- A click is a press and release **with no pixel moved** (`:233-241`). The hit test (`:254-288`)
  walks provinces 0..`Prov_Count` (record 0 first, STR-12), box half-size `HitRadius` (`:292-299`: 12,
  larger for the big castles), then dungeons (half-size 10; only in an own province or none, STR-19).
  `GesturePoints.MapAt` (`Original/Script/GesturePoints.cs:103-122`) mirrors it.
- **Dispatch** (`ProvinceClickedAsync`, `:346-382`): at stage 0 Vladivostok (33) or Golak (34) →
  `Cﾃｷﾄｼ`, anything else → `Cﾃｷﾄｼ2`; later an own province → `Cｼﾊｲﾄｼ` (`OwnCityCommand`), a
  province next to an own one (Adj1-5) → `Cﾃｷﾄｼ` (`EnemyCityCommand`: attack, negotiations, ninja),
  any other → `Cﾃｷﾄｼ2` (`DistantCityCommand`); a dungeon → `Cﾒｲｷｭｳ`. The weekly gates
  (`Ui_CmdOwnCityAvail`, `Ui_CmdAttackAvail`, `Ui_CmdDungeonAvail`) are checked inside the files.
- What a city's file shows first: its picture (`WAO_SUB` 331, `Prov.BgCg`), window 113 with
  "［name］ (owner)" and either "Economy: ?? Defense: ??" or the three values once a ninja has scouted
  it (`Ui_CityScouted`, `EnemyCityCommand.cs:69-77`), then a `]` menu (on Oaks (6) at week 18:
  Attack / Ninja Maneuver / Return).

### The command menu (right click)

- **The right button is the only way** to every command and Turn END (`:203-217`; docs/ui/survey.md
  §3.3). The menu step (`CommandMenuAsync`, `:419-501`) first prints **the status window** (window
  113): Leazas — "Budget: n00K GOLD", "Troops" (the sum of the present units' troops), "Month m, Week
  w" (`LeazasStatusAsync`, `:657-673`); the thieves' week shows "Rance's Thieves", money and the
  subordinates' troops (`:639-651`). LP (`DateYear`) is computed but not shown.
- **The items** (stage ≥ 1, `COMMAND.ADV:229-310`): ○/× Construction (`Ui_CmdBuildAvail`), ○/×
  Harem (`Ui_CmdHaremAvail`), ○ Treasure, ○/△ Merchant (`Ui_CmdMerchantAvail`), ◎/○/△ Subordinates
  (`Ui_CmdSubordAvail`, `CmdAvail_SubTroops` + `CmdAvail_SubTalk`; STR-20), → Leazas Castle (a pan),
  Load/Save, All Data, Think (`Iｶﾝｶﾞｴ`), TADA (debug only), Turn END. Stage 0: ○/× Harem, ○/△
  Subordinates, → Thieves' Hideout, Load/Save, Turn END. The marks are part of the texts; a × item is
  an item of its own that re-opens the menu (`:509`). Every flag is reset to 1 at the week's start
  (`:104-112`). The ninja is not a menu item (it is in `Cﾃｷﾄｼ`/`Cﾃｷﾄｼ2`).
- A right click in the menu goes back to the map and warps the pointer back (`IZ`).

### What the state already gives a screen

Provinces (`Provs`): `Owner` (1 Leazas … 11 ALism, docs/variables.tsv `VAR0130`), `MapX/MapY`,
`Adj1-5`, `BgCg`, `Economy`/`Defense`/`BattleArea`/`Girls`; names at `Prov_StrBase + p − 1`. Dungeons
(`Dgns`): `MapX/MapY`, `Prov`, `State`. The date (`Week`, `DateMonth`, `DateWeek`, `DateYear`),
`Gold`, the units' troops, every weekly flag above, the ninjas' flags. No province record holds its
garrison (the enemy forces are commanders without a province link).

## Sengoku Rance's map screen (reference)

Surveyed from its code (`SENGOKU_RANCE_EXTRACT_DIR`, `ain\files\あべ\`; `tagMapControl@run(2)` in
`C_MAPCTL.JAF:155-189` and the parts `T_MAP`, `T_MAPKUN`, `T_MAPDAT`, `T_CHIIKI`, `T_COMMND`,
`T_MAPINF`, `T_SHISET`, `T_SYSBAR`). At 800×600:

- **The map fills the screen**; each province is tinted by its relation to the player — cyan ours,
  red at war, green a client state, yellow bordering — with a legend (`T_MAPKUN:507-610`,
  `T_MAP:169-306`), and carries its nation's flag. A province is found from a hidden colour-keyed copy
  of the map (`T_MAP:428-555`).
- **The commands live on the provinces.** Hovering a province frames it (cyan corners), switches the
  info card and previews its command list under a frosted veil; a click locks it (it blinks white) and
  the list becomes clickable (`T_CHIIKI:1332-1471`, `T_MAP:784-850`). The list, top left (426×278),
  holds that province's verbs with a coloured tag each (Battle, Build, Search, Neg., with the
  difficulty left; `T_COMMND:105-160, 868-1014`). Right click or Esc unlocks.
- **The turn's limit**: "Actions" pips under the list (gold left, grey spent), End Turn beside them
  (`T_CHIIKI:34-294`, `T_MAPCTL:1662-1671`).
- **The province card**, bottom right (284×139): "[name] n National Power", "Controlled by …", the
  facilities' icons; blue for own, red for others (`T_MAPINF:16-311`).
- **A thin bar** along the bottom: the sub-screens' buttons (System, Items, Forces, Prison) and the
  resources (cost/power, satisfaction, gold, turn) (`T_SYSBAR:25-247, 1027-1228`).

## How our screen answers the script

- **A command button** is the right click that opens the menu (`PointerScript.RightClick`) and the
  answer to it: the item whose text is the command (`SelectAsync`). The status window and the menu are
  then not drawn: our screen shows the status always and the commands as buttons. A × command is a
  disabled button (the script would only re-open the menu); △ and ◎ are states of the button.
- **A province click** is a still click at the province's point (`PointerScript.Click`). The script
  hit-tests it against its own camera (`MapCamX/Y`): where our view differs from the script's 640×400
  (a smaller zoom, a side panel), the click needs the script's view moved first — a drag
  (`GesturePoints.DragToward`, as `--auto` does) — or the script's camera kept equal to ours where the
  scale is the original's.
- **A province's own menu** (the city files' first `]` menu, their window 113 and picture) is drawn as
  ours: the card shows the picture and the values (or "??") the window prints, the menu's items become
  the province's verbs. The files' later windows and menus (negotiations, the ninja, the attack's
  deployment picker — which is ours already) stay as they are for now.
- **Back** (a right click: out of the province's menu, out of the command menu) is a button and Esc as
  well, so no command needs the right button (survey §3.7).
- What our screen shows **beyond the original** — the province's name and card on hover before a
  click, the names on the map, the status always on screen, the nations' list — reads only the state;
  whether to show it is the owner's choice (question 4).

## The mock-ups (2026-10-16)

Made from the owner's week-18 save (`user://saves-week18`: LP 3, month 5, week 2; Leazas, stage 1),
the steel palette, the hovered province Oaks (6) (Rebel Army, next to Ys (5)), each in two states: the
map wait with Oaks hovered, and Oaks clicked with its menu (Attack / Ninja Maneuver / Return) as ours.
Rings on the map mark own provinces (blue) and the provinces next to them (red: a click opens
`Cﾃｷﾄｼ`); the original's flags stay on the towns. The images hold the player's CGs, so they live
outside the repository; the script that draws them took the state from the save.

- **A — Sengoku Rance's arrangement.** The map at the original's scale (2×, the script's 640×400 view
  filling 1280×800) under everything. Top left a command panel: the menu's commands as rows with a tag
  (ONCE / FREE / 2 LEFT for the weekly flags), the system items below (→ Leazas Castle, All Data,
  Think, Load/Save); a click on a province turns the panel into that province's verbs (tags BATTLE,
  NINJA). Under it "This week" (Attack, Own city, Dungeon: lit while their flag is 1) and Turn END. The
  province card bottom right (picture, name, owner's flag, the three values or "??"), a legend at the
  right, a thin status bar along the bottom (Leazas, Budget, Troops, LP · month · week).
- **B — a side panel.** The map left (880 px, at 1.5×: the whole of Leazas in view, names under own
  and neighbouring towns, → Leazas Castle as a map button); a panel right: the nation, the date,
  Budget and Troops, the province card with its picture, the commands as a grid of buttons with a
  badge for the uses left this week, "This week", a large Turn END. A clicked province's verbs replace
  the command grid.
- **C — the whole map at once.** The whole continent across the top (1280×533, the map at a third of
  its 2× size), each province a dot in its owner's colour, own and neighbouring ones ringed, the
  neighbours named; the nations with their province counts top left; wheel to zoom, drag to scroll. A
  dashboard below: the status and "This week" left, the province card in the middle (its verbs on it
  once clicked), the commands and Turn END right (greyed while a province's menu is open, as the
  original cannot reach them then).

## The map's markers (owner, 2026-10-16)

The owner goes in small steps; the first is **interactive markers on the map**, at the original's
scale. Interactive mock-ups (a local page over the week-18 map, outside the repository) went through
five shapes (a ring, a pin, a name plate, corner brackets, a shield) and then the plate's details.

- **The original's markers are invisible squares**: a click counts when it lies strictly inside the
  square of half-size r around (`MapX`, `MapY`), the middle of the town's picture (`COMMAND.ADV:138`,
  `:148`): (2r − 1)² script pixels — 23×23 for a province (r 12), 39×39 for Toushin City (27), 59×59
  for Leazas Castle (1), Osaka (56), Zeth Palace (69), Tower of Evil (78) and Demon Lord's Castle (79),
  63×63 for Lang Bau (45), Silky's Castle (81), Kayblis's Castle (84), Kesselring's Castle (87) and
  Camilla's Castle (88), 71×71 for M Land (25), 19×19 for a dungeon. Provinces are tested first, from
  record 0 (STR-12); the only overlap is Leazas Castle (1) with the Thieves' Cave (dungeon 3), a few
  pixels the castle wins.
- **Ours are drawn and hit-tested by our layer**, any shape: a click on a marker is answered with a
  still click at the province's (`MapX`, `MapY`), which lies inside its own square, so the script finds
  that province; a dungeon in a province not ours opens nothing (STR-19), as in the original.
- **The plate** (the owner's choice): a dark plate under the town with its name — 32 px high on the
  1280×800 screen (15 px text), no colour stripe; the provinces neither ours nor next to ours at 70 %
  opacity. **The hover** changes the colour only — the frame gold (2 px; 2 px transparent at rest, so
  nothing moves), a gold glow, the plate lighter, a light spot under the town; no growth, no second
  line.
- **The original's flags stay** (CG `owner + 819` at (`MapX`, `MapY` − 18), 2×): they show who owns
  what; the plates carry no colour of their own.
- **The dungeons** (variant D3): the dungeon's name from the player's files (`Cﾒｲｷｭｳ.ADV:29-150`,
  `MS 8, "…"`; `MS 9` for Piar's Ship (dungeon 9), DGN-15; Titi Lake (dungeon 25) reads Haniwa Temple
  once `F_HannyKing_State > 1`, so the name follows the state), on a plate of their own — rounder,
  dark teal, an icon of the kind in a teal disc: a cave for a search, a tower for the towers (Rising
  Dragon Mountain (11), Hyper Building (26)), a star for an event place (no search). A badge after the
  name shows the search: `B<floor>/<last floor>` while `Dgn_State` is 1 (`Dgn_SavedFloor`; the last
  floors are `DungeonCommand.Parameters`), ✓ at state 2, none before the first search or at an event
  place — more than the original shows on the map. A dungeon not ours: the plate dimmed grey, no lock,
  a muted hover and the "not allowed" cursor.
- The scenario's `’` is drawn wide by the Japanese font ("Monsters’  Cave"): the plates print `'`.

**Built (2026-10-16).** The model is `Original.Content/Strategy/MapMarkers` (the markers from the state,
the dungeons named through `DungeonCommand.NameAt` and their search from `DungeonCommand.SearchOf`;
`ClickPoint` checks with `GesturePoints.MapAt` that the script reads the click as the marker's — a
plate whose click the script would read otherwise gives none; `MapMarkersTests`) and
`MapCameraForm`, which follows the camera's form (the state does not record it: centred from the
map wait's start through the release wait, the view's corner while the loop waits, drags and coasts,
centred again at the menu's right click). The layer is `godot/Strategy/MapMarkersView` over the
picture, shown by `GameScreen.UpdateMapMarkers` while the map waits with the camera as the corner,
not under a message window or a menu; it reads the markers when it appears and follows the scroll
each frame — at the view the screen shows (`GamePresenter.MapViewShown`: the map shown, a drag's
step), not at the camera, which a still click's release centres a read before the wait ends with the
picture unchanged (the plates jumped for a frame before the province's menu, fixed 2026-10-16). A left click on a plate queues the still click (`GameScreen.MarkerPressed`, its own
`PointerScript`); a right click goes on to the script (the menu); presses elsewhere stay the
script's. A press that starts on a plate does not drag the map. `--auto --interface new` in a window
dumps the first map wait's plates (`map-markers`, `map-markers-hover` with Leazas Castle (1)) and
clicks the week's attack target through its plate as a player does (the system pointer onto it,
`map-marker-hover`, a press and a release through Godot's input), so the attack runs from the plate's
click.

## Questions for the owner

1. **Layout**: A, B or C (or a mix: e.g. A's map with C's zoom-out).
2. **Province commands**: a click opens the province's verbs in our panel (as Sengoku Rance's lock),
   or the script's menu stays the original's over our screen.
3. **The weekly limits**: as tags on the commands and "This week" pips (the mock-ups), or only as
   enabled / disabled buttons.
4. **What may be shown beyond the original**: the province card on hover (name, owner, picture, "??"
   values as the original's window), names on the map, the nations' counts, the status always.
5. **Markers**: the original's flags on the towns with our rings, or our own markers (dots or tinted
   chips) instead.
