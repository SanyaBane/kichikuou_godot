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
- **Option → Hide Dimmed Map Names** (owner's request 2026-10-16, off by default, saved): the dimmed
  plates — the provinces neither ours nor next to ours, the dungeons not ours — are not shown
  (`godot/Strategy/MapNamesChoice`; docs/window-menu.md).
- The scenario's `’` is drawn wide by the Japanese font ("Monsters’  Cave"): the plates print `'`.

**Built (2026-10-16).** The model is `Original.Content/Strategy/MapMarkers` (the markers from the state,
the dungeons named through `DungeonCommand.NameAt` and their search from `DungeonCommand.SearchOf`;
`ClickPoint` checks with `GesturePoints.MapAt` that the script reads the click as the marker's — a
plate whose click the script would read otherwise gives none; `MapMarkersTests`) and
`MapCameraForm`, which follows the camera's form (the state does not record it: centred from the
map wait's start through the release wait, the view's corner while the loop waits, drags and coasts,
centred again at the menu's right click). The layer is `godot/Strategy/MapMarkersView` over the
picture, shown by `GameScreen.UpdateMapMarkers` while the map waits, not under a message window or a
menu; it reads the markers when it appears and follows the scroll each frame — at the view the screen
shows (`GamePresenter.MapViewShown`: the map shown, a drag's step), not at the camera, whose form
changes under an unchanged picture: a still click's release centres it a read before the wait ends,
and a click on no town starts the wait again with it centred until the release wait's read (placed
from the camera, the plates jumped for a frame before the province's menu, and hidden while the camera
was centred they blinked at a click on no town; both fixed 2026-10-16). A left click on a plate, while
the camera is the corner, queues the still click (`GameScreen.MarkerPressed`, its own
`PointerScript`); a right click goes on to the script (the menu); presses elsewhere stay the
script's. A press that starts on a plate does not drag the map. `--auto --interface new` in a window
dumps the first map wait's plates (`map-markers`, `map-markers-hover` with Leazas Castle (1),
`map-markers-active` without the dimmed ones) and
clicks the week's attack target through its plate as a player does (the system pointer onto it,
`map-marker-hover`, a press and a release through Godot's input), so the attack runs from the plate's
click.

## The province card (owner, 2026-10-16)

**The second small step: a card on hover**, the province's figures only, right of the hovered plate (left
of it where the map ends), over the plates; nothing for a dungeon. Each line is the original's figure and
what it does (the owner's format): "Economy: 20 (2000K Gold)" — the weekly income, in the 100K GOLD the
budget counts; "Defense: 10 (Walls +20%)" — two points off the attackers' hit chance a point, as the
battle screen's "Walls"; "Battle Area: 180 troops" — the most that fight at once in a city battle. A city
with Defense 0 is fought on the field (`Bt_WallBonus` 0, a limit of 5000), so its Battle Area does not
apply there; the card does not say so.

- **Who shows figures.** Ours always, as `Cｼﾊｲﾄｼ`'s window (`Cｼﾊｲﾄｼ.ADV:15-31`); the others "??", as
  `Cﾃｷﾄｼ`/`Cﾃｷﾄｼ2` print them for a city not scouted (`Cﾃｷﾄｼ.ADV:26-43`) — until the player has seen them.
- **What the original remembers: nothing.** A ninja's report (`Cﾆﾝｼﾞｬ`, and Kanami's sabotage) sets only
  `Ui_CityScouted`, which the city files reset on entering (`EnemyCityCommand.cs:45`,
  `DistantCityCommand.cs:16`). The port remembers it for the card (beyond the original, the owner's choice):
  `GameState.KnownProvinces`, marked where the ninja sets the flag (`NinjaCommand`, five places) and for every
  province of ours at each map wait (`CommandPhase.MapAsync`), so a province lost stays known. It is the
  port's own data, `ScriptState.Extras`, kept in the save slot (`SaveData` format 2; a slot of format 1 loads
  with nothing known) and read by no rule.
- **The fact, not the figures.** The card shows a known province's figures as the state has them now. No
  nation's turn and no capture changes them; only Bolster Defense (`Cｼﾊｲﾄｼ.ADV:1609`, ours), Kanami's
  sabotage (`Cﾆﾝｼﾞｬ.ADV:698`, scouted at once), a city defence (`FIGT_B.ADV:185`, ours, Battle Area +10 %,
  Q-3) and three story scenes — Papaia's ruin (`Gﾊﾟﾊﾟｲｱ.ADV:1800, 4753`: 0 / 0 / 2000), Miki's at Leazas
  Castle (1) (`Gﾐｷ.ADV:4645`: 1 / 1) and stage 4 (`Tﾙﾄﾞﾗ.ADV:66-68`: every province 0 / 0 / Battle Area × 2).
  So remembered figures could only go stale after those scenes; showing the state's never does.
- **Built (2026-10-16):** the lines are `MapMarkers.CardOf` (`MapMarker.Card`; `MapMarkersTests`), drawn by
  `MapMarkersView`'s `ProvinceCard` (label muted, the rest in the text colour, "??" muted). `--auto
  --interface new` in a window shows it in `map-markers-hover` (Leazas Castle (1)) and `map-marker-hover`
  (the attack's target).

## The province menu (owner, 2026-10-16)

**The third small step: a city's first menu as our panel.** Mock-ups first: an interactive page was
"a free-to-play browser game" to the owner (pill buttons, tags, capitals); four drawn styles followed
(lacquer and gold, the original's PC-98 windows, parchment, a Sengoku Rance-like list), then the list in
the colours of the first and of the third. The owner chose **4a**: the list at the map's top left in
lacquer and gold — a near-black panel with a double gold frame and gold corners, the city's picture
(`WAO_SUB` 331, `Prov.BgCg`) with the name in Shojumaru beside it, the army in Cinzel, the card's figures
(`MapMarkers.CardOf`), then the commands as rows, each with a coloured tag of ours (Levy, Build, Plot, Visit,
Search, Battle, Ninja), and Return. The mock-ups hold the player's CGs and stay outside the repository.

- **Which menus.** A city file's first menu, recognised by its items' places (`ProvinceMenu.For`):
  `Cｼﾊｲﾄｼ.ADV:32-507` (the commands at 41-59, × 24 below; the places at 91-283, × 207 below; the Ruins
  Search's ○ at 436 too, CMD-09; Return 502), `Cﾃｷﾄｼ.ADV` 52/58 (the AL Church ○/×), 64/70 (Attack /
  Pillage), 76 (Ninja Maneuver), 81 (Return), `Cﾃｷﾄｼ2.ADV` 50 (Ninja Maneuver), 55 (Return). The army's
  name is the line the files print (`Cｼﾊｲﾄｼ.ADV:21`, read from the `R` at `:20` where its run starts — read at
  `:21` it showed as "[Cｼﾊｲﾄｼ.ADV:21]", owner 2026-10-16; `Cﾃｷﾄｼ2.ADV:71 + 4 (owner − 2)`).
- **The answer is the original's.** A row's click answers the menu with its item's place; a × item (it
  only opens the menu again in the original) is a dimmed row, "used this week", not clickable; a right
  click or Esc cancels (0), as the original's menu. The files' later windows and menus stay the original's,
  but one: the dungeon's menu (below).
- **The dungeon's menu (owner, 2026-10-16).** After the place Dungeon in Leazas Castle (1) `Cｼﾊｲﾄｼ` clears the
  window and opens a second menu (`:732-763`: the prisoners' cells, Look Around, Return) over the city's picture
  and the emptied window; with our panel that showed the original's picture, empty window and menu after our
  panel. It is our panel too: the same header, the place's name ("Dungeon", the item at `:134`) as a caption,
  the cells (tag Cell) and Look Around (tag Look) as rows, Return. A cancel only opens it again in the
  original, so the panel ignores a right click and Esc there (`ProvinceMenu.CancelReopens`).
- **Built (2026-10-16):** `Original.Content/Strategy/ProvinceMenu` (`ProvinceMenuTests`); the presenter keeps
  the open menu's items (`GamePresenter.ProvinceMenu`) and, while `MenuCovered`, draws neither the
  city's picture nor the window and its text nor the menu; `godot/Strategy/ProvinceMenuView` draws the panel
  and answers `GameScreen.SelectAsync`. `--auto --interface new` in a window dumps it once (`province-menu`,
  the attack's target).
- **The lead-in is covered too (2026-10-16).** The files draw the city's picture (`WAO_SUB` 331) and the
  window 113 with "［name］" and the figures, then wait for the button's release (`IM`, frames pass) before
  the menu: that showed the original's window for a few frames before our panel. With our panel chosen
  (`GamePresenter.CoverCityLeadIn`) the presenter covers them from the picture of `Prov.BgCg` (not on a
  forced trip, `Ev_CommandMode` 1/2) to the menu; anything else the files show (another window op, a key
  wait, a pause, a screen event) ends it (`A_city_files_lead_in_is_covered_up_to_the_menu`). So is the
  lead-out: a cancel (right click, Esc) puts the pointer back (`IZ`, frames pass) before the files close the
  window (0) and remove the picture (332), and a frame of the original's showed; after our menu the cover
  stays through the text cleared, `IZ`, 0/10 and 332, and ends with the next thing shown (the map).
- **The owner's look (2026-10-16):** the panel sits in the screen's centre, as the original's menu (not at the
  map's top left); its lines are at least one window pixel wide (`ProvinceMenuView.Line`), since a 1-pixel line
  scaled down fell between the pixels' centres (the first row's top edge vanished).

## The command panel (owner, 2026-10-16)

**The fourth small step: the right click's menu as our panel, and a status strip.** Three drawn mock-ups in
the style of 4a (A: a column at the left edge, B: a bar along the bottom, C: the status along the top with
the commands at the right). The owner first chose A centred on the screen as the original's menu (built as
`aa6e054`, the status and the weekly uses as diamonds in the panel), then **C with the panel in the centre**:
the status on a strip along the top, the panel holding only the menu. The week's uses moved to the strip
(three mock-ups: tags in one row, a second row, struck-through words; the owner chose the second row), and
Subordinates is two of them, Troops and Talk (four groupings: two tags, one split tag, captions in front,
brackets with captions under; the owner chose **the brackets**).

- **Which menu.** `COMMAND.ADV`'s command menu (`CommandPhase.CommandMenuAsync`, `:183-312`), recognised by
  its items' places (`CommandMenu.For`, Turn END last at `:310`, the thieves' at `:218`).
- **What it shows.** Still opened by the right click, in the screen's centre: the commands as rows with a tag
  of ours (Build, Harem, Items, Trade, Units); a × item and Merchant's △ only re-open the menu in the original,
  so they are dimmed rows, "used this week", while Subordinates' △ still opens `Cﾌﾞｶ`; the other items
  (→ Leazas Castle, Load/Save, All Data, Think, TADA) as plain rows; Turn END as a large lacquer-red button.
- **The answer is the original's.** A row's click answers with its item's place; a right click or Esc
  cancels (0: back to the map). The status window and the menu are covered while it is open
  (`GamePresenter.MenuCovered`); after it, the way out is covered as after a province menu.
- **Built (2026-10-16):** `Original.Content/Strategy/CommandMenu` (`CommandMenuTests`),
  `GamePresenter.CommandMenu`, `godot/Strategy/CommandMenuView`; the province menu, the command panel and the
  strip share `godot/Strategy/Lacquer`. `--auto --interface new` in a window dumps it once (`command-menu`).

## The status strip (owner, 2026-10-16)

- **Where and when.** Above the map, as wide as it, not over it (owner, 2026-10-16: at first it lay over the
  map's top, 980 wide): from the game's first map wait on and outside a battle, the view gives up a band at
  its top (50 of the picture's 1280-wide pixels) and the screen's picture fits under it; every screen reckons
  from the view, so the pointer, the plates, the panels and the deployment table follow; a battle keeps the whole
  view (`GameScreen.UpdateStatusStrip`). It shows in the scenes and screens between map waits too. A right click
  on it opens the menu as anywhere (`StatusStripView.Forward`).
- **One row** (owner, 2026-10-16: at first two, the status above the week's uses). From the left: the
  nation's flag (CG 820; none for the thieves) without its name (the owner's, for room); the status window
  the right click prints, read now from the state: its two lines as the files print them (`:443` "Budget
  116400K GOLD", "Troops 6251"; the thieves' `:472` Money and Subordinates), the troops summed as the files sum
  them (units 1-50 with a status; the thieves' units 1, 3 and 4); the week's uses as tags, lit while free, dim
  when spent, in groups bracketed with their captions: Castle (Construction `Ui_CmdBuildAvail`, Harem
  `Ui_CmdHaremAvail`, Merchant `Ui_CmdMerchantAvail`), Subordinates (Troops `CmdAvail_SubTroops`, Talk
  `CmdAvail_SubTalk`, both dim while `Ui_CmdSubordAvail` is 0, the menu's △), Map (Own city
  `Ui_CmdOwnCityAvail`, Attack `Ui_CmdAttackAvail`, Dungeon `Ui_CmdDungeonAvail`); the thieves' week: Hideout
  (Harem, Subordinates as one), Map (Attack, Dungeon). At the right the date as "LP 3 · Month 5 · Week 2"
  (`SetDate`; LP, `DateYear`, is computed by the original but not shown: ours, question 4), large and in the
  names' gold (owner, 2026-10-16: the small dim one was lost on the lacquer), smaller only if the row leaves it
  no room (the widest figures leave it ~70 of the 1280 pixels).
- **Built (2026-10-16):** `Original.Content/Strategy/StatusStrip` (`StatusStripTests`, one test on the
  player's files), `GamePresenter.StatusStrip`, `godot/Strategy/StatusStripView`.

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
