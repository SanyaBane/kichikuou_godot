# The message windows — our own look

The scenes' windows (`WAO_SUB`) are the last large part of the screen still drawn as the original draws
them; next to our lacquer panels they look out of place (owner, 2026-10-18). This survey says what they
are, and records the owner's choices and the mock-ups.

## 1. What the original shows

Every scene message has one shape (survey-events.md:71): `!CallArg : <portrait CG>! \L_portrait: \L_open:
ZH 1: 'text' R 'text' \L_wait: \L_close:` — the helper `WAO_SUB` sets the window type, its frame CG and the
pictures (`Original/Script/OriginalScriptContext.Wao.cs:20-178`); the presenter draws them
(`GamePresenter.Window`, `GamePresenter.cs:450-481`; the battle has its copy, `BattlePresenter.Window`,
`BattlePresenter.cs:497-565`). In the 1280×800 screen's pixels (geometry: battle/screen-layout.md:184-208):

| Op | What it is | Frame CG, place | Text | Opened in scenes |
|---|---|---|---|---|
| 111 | white speech box, tail at the left portrait | 807 at (400,6), 816×244 | (470,16), 5 lines of 31 cells | 12,664 |
| 112 | dark box: a thought, left | 808, same place | same | 901 |
| 121 | white speech box, tail at the right portrait | 810 at (64,518), 816×244 | (86,530), same | 13,046 |
| 122 | dark box: a thought, right | 811, same place | same | 320 |
| 113 / 123 | cream box: narration, places' titles (`ZM 32`), status windows | 809 at (256,542), 768×252 | (280,556), 5 lines of 31 cells | 6,015 + 1,476 |
| 131 | full-width grey bar under the event CGs (640×294 at 1×) | 815 at (0,588), 1280×212, opaque | (16,610), 4 lines of 52 cells | 296 |
| 141-153 | the battle's bubbles | 894-898 | — | battle only; ours since 2026-10-15 (`SpeechBalloon`) |

- **No speaker's name in the text.** The window's side says who speaks (111 the left, 121 the right), the
  portraits carry their names baked into the CG (384×512 at (0,0) and (896,288); the centre picture
  576×384 at (352,192)). 111 opens mostly without a left portrait (12,664 against 1,982 `311`s) — most
  likely Rance's lines; not checked. Window 131 carries the name in the text: `Sill「Ah... My clothes...」`.
- **The text** is the English translation, monospaced (MS Gothic or BIZ UDGothic, Option → Fonts), set at
  the script's own line breaks (`R`; it never wraps), all at once; no marker asks for the click; no log.
  Some windows hold tables aligned by spaces: `Cﾌﾞｶ`'s 121 reads `HP    4/    5`, `Troops   102/ 1000`.
- **`]` menus** are `SelectionBox` at a fixed spot (500,100), 308 wide, 36-px rows, lilac (#bec3dc), on top
  of everything (`Content/Screen/SelectionBox.cs:71-123`); 123 menus in 67 scene files plus the command
  files' menus not covered yet.
- **`TextBox`** (`Content/Screen/TextBox.cs`) knows only the rectangle and the characters it placed;
  the window's type, frame and pictures are `Ui_WinType`, `Ui_WinFrameCg`, `Ui_Portrait*` in the state.

## 2. Variants (2026-10-18)

- **A — the original's layout, our drawing.** The windows where they are, as lacquer panels with a tail at
  the portrait; thoughts, narration and titles each their own panel.
- **B — a bottom panel after Sengoku Rance** with a name tab; the speaker lit, the other dimmed.
- **C — B with the portraits recut** (`FacePictures`) in our frame and name plate.

## 3. The owner's choice (2026-10-18)

**Variant A**, with:

1. **The click's marker** — the week report's triangle (`WeekReportView.DrawTriangle`) at the panel's
   bottom right.
2. **The text's face and breaks stay as now** (owner, 2026-10-18): monospaced (the font of Option → Fonts),
   at the script's own line breaks and cells — not rewrapped; mock-up `A-point2-text`, way 1.
3. **Text appearing letter by letter**, its speed a slider with a live preview next to it (a speech panel
   typing a line at the chosen speed, again and again); the slider's far right is instant. A click while it
   types shows the whole text; the next answers the wait. The message skip passes it as before.
4. **A message log** — wanted, its design discussed separately; **the last step** (docs/backlog.md).
5. **The `]` menus** in our rows.
6. **Window 131** our own too (the name in `Name「…」` split off).
7. **The battle's windows** (111/112/121/122/113/131 that a battle opens, `BattlePresenter.Window`) too —
   the battle screen's step 6 put them off (backlog, Interface).

The rules do not change: the windows' key waits stay the script's own, only the drawing is ours
(the cover pattern of the week report and the province menu: `Compose` leaves out the frame and the text,
a Godot view draws the panel from a model).

## 4. Mock-ups (2026-10-18)

Drawn over the screen dumps of the kept save `saves-week26` (Lia's and Maris's scene at the week's start,
`Cﾌﾞｶ`'s menu), the dumps 640×400 scaled to 1280×800; the panels in `Lacquer`'s colours and parts. The
images hold the player's CGs, so they stay outside the repository.

- `A-1-speech-right` (121, Maris), `A-2-speech-left` (111, Rance): the panel over the old box with its
  gold frame broken by the tail, the text as now (MS Gothic), the marker.
- `A-3-thought` (112): rounded, a dashed gold frame, no corner diamonds, a trail of three bubbles to the
  thinker, the text dimmer.
- `A-4-narration` (113): no tail, a warmer brown lacquer, parchment-gold text.
- `A-5-place-title` (113 at `ZM 32`): the place in Cinzel, a divider, the room under it.
- `A-6-menu`: the `]` menu as lacquer rows (the hovered one lit, the ○ items a gold ring) over the 121
  that asks.
- `A-point2-text`: one window four ways — monospace at the script's breaks (now); a proportional face at
  the script's breaks (ragged); a proportional face with the lines joined and rewrapped (as the battle's
  balloon does, `BattleSpeech.Flow`); a table line kept as lines with each column at its cell.
- `A-text-speed`: Option → Text Speed…: the slider (Slow … Instant), the preview, Default / OK / Cancel.
- **Window 131** (2026-10-18), over a harem scene of the kept save `saves-week18` (CLAUDE.md, "Commands", `--harem`):
  `A-7-cg-bar-speech` — the bar a lacquer panel the screen's width, the speaker's name on a plate over its top edge, the
  text without the name and the brackets, moved left by the name's cells; `A-7b-cg-bar-speech-inline` — the name kept at
  its cells in gold instead; `A-8-cg-bar-thought` — `Name（…）`: the plate rounded with a dashed frame, the name and the
  text quieter; `A-9-cg-bar-narration` — no plate, the narration's text colour. **The owner chose A-7, A-8 and A-9**
  (2026-10-18).

**Making them again:** `tools/ui/mock_messages.py` (its header gives the dump command and the call); the
images go to a folder outside the repository. Its numbers are the build's starting point (screen pixels):

- **121** panel (58,512) 822×254, the tail out of its right side between y+150 and y+196, its point at
  (x+w+30, y+188), next to the right portrait (x 896). **111 / 112** panel (436,4) 834×254, the tail out
  of its left side between y+96 and y+142, its point at (x−30, y+104). **113** panel (248,536) 784×258. Each
  covers the original's box and tail whole.
- **Speech:** `Lacquer.DrawPanel` (the frame broken where the tail leaves it: lacquer over the frame, gold
  along the tail's two edges). **Thought:** fill (30,26,30), corners rounded 18, the frame gold dashes 14
  on 8, no corner diamonds, bubbles of radius 12, 8, 5 towards the thinker, the text `Lacquer.Quiet`.
  **Narration:** fill (52,38,24), the text (226,204,150). **A place's title:** Cinzel 600 at 50 in
  `Lacquer.Name`, `DrawDivider`, the room in Cinzel 500 at 34 in `Lacquer.Subtitle`.
- **Text:** the game's font at 48 px (half-width cells of 24, as the engine places them), bold as
  `GodotGlyphRenderer` makes it, lines 46 apart, from (x+30, y+22) in 121 and (x+34, y+22) in 111/113 —
  i.e. where `TextBox` puts the characters now; colour `Lacquer.Text`.
- **Marker:** `WeekReportView.DrawTriangle` at (x+w−30, y+h−28).
- **Menu:** a panel at (470,70) 380 wide, rows 36 high at a 44 pitch (`Lacquer.DrawRowBox`), the ○ a gold
  ring 14 across at the row's left; the mock-up's row text is a serif at 26 — the build's face is open (the
  province menu uses the game's font).
- **Text Speed…:** a panel 680×470 in the centre over the map dimmed by 45 %; the title in Shojumaru, a
  divider; ten steps, the last Instant, the step's number over the knob; the preview a speech panel typing
  a line again and again; Default, OK, Cancel.

## 5. Steps (proposed, 2026-10-18)

Each step behind our interface (`godot/Strategy/InterfaceChoice`); the original's layout stays
as it is. Where to start:

- **The cover pattern** the windows follow: `GamePresenter.CoverWeekReport` (the presenter's flag, which
  `Compose` reads to leave out the pictures, the frame and the text, `GamePresenter.Compose.cs:419`),
  `MenuCovered` (`Compose.cs:296-349`), and `GameScreen.WaitForKeyAsync` (`godot/Strategy/GameScreen.cs:999-1007`)
  that answers the original's waits. Here only the frame and the text are left out (the portraits and the
  centre picture stay the original's), and the waits stay the player's.
- **The text** is `GamePresenter._window` (`GamePresenter.cs:44`, `WindowText` `:224`, `MessageWindowShown`
  `:182`); the window's kind is `Ui_WinType` / `Ui_WinFrameCg` in the state (set by
  `OriginalScriptContext.Wao.cs:20-178`); the frame is drawn by `GamePresenter.Window` (`:450-481`).
- **The battle's own copy**: `BattlePresenter.Window` (`BattlePresenter.cs:497-565`), its bubbles already
  ours (`godot/Battles/SpeechBalloon`, `BattlePresenter.Speech` `:594-596`), the rest through
  `Battles/BattleWindows.cs` (`WindowMask`).
- **A look** in a window starts from the kept save `saves-week26` (its week starts with Lia's and Maris's
  scene: 111, 112, 113, 121, a place's title and `Cﾌﾞｶ`'s menu) with `--interface new` and `--shots`, the
  command of `tools/ui/mock_messages.py`'s header without `--headless`; `--auto` should dump the new windows
  once a launch, as it does the week report.
- **Tests:** the presenter's drawing is shared by the scaled tests (`GamePresenterTests.Audit`), so the
  full counts of the content tests are due once the step is settled (CLAUDE.md, "Commands").

1. **The windows** 111/112/121/122/113/123: a model in `Original.Content/Strategy` (the kind — speech left
   or right, thought left or right, narration, a place's title at `ZM 32` — the rectangle, the characters
   `TextBox` placed), `GamePresenter` leaving out the frame and the text while it is covered, a Godot view
   drawing the panel, the text at its cells in the game's font and the marker while the script waits.
   **Built (2026-10-18):** `Original.Content/Strategy/MessageWindow` (the kind from `Ui_WinFrameCg`, the panel, the
   glyphs at their cells in screen pixels, the lines with their spaces put back; a place's title is a 113 whose first
   line is at `ZM 32` with one line under it at most; text outside the panel leaves the window to the original),
   `GamePresenter.CoverMessages` / `MessageWindow` / `MenuArea`, `godot/Strategy/MessageWindowView` (the triangle
   while `GameScreen` waits for the player's key, a menu over the window cut from the screen's picture on top, the
   panels fading with the screen); `MessageWindowTests`. `--auto` in a window dumps each kind once a launch
   (`message-speech-left`, `-speech-right`, `-thought-left`, `-thought-right`, `-narration`, `-place-title`).
2. **Window 131** under the event CGs, the name of `Name「…」` split off.
   **Built (2026-10-18):** `MessageKind.CgBar` (frame 815; the panel (0,588) 1280×212). The text's own colour is 190; a
   speaker is a name at the first line's start before `「` (`｢` from a string variable) or `（` — words beginning with a
   capital, a digit or `?` (`Rance`, `Pink-Haired Girl`, `PG-7`, `???`; the scenario has 8,942 speeches and 346 thoughts so)
   — split off into `Speaker` / `Thinks`; the first line moves left by the name's cells, the lines under it by their
   indent up to that, the closing bracket at the end is dropped. A speech not closed at a window's end (4 in the scenario)
   is `Unclosed`; a next window without a name whose first line is indented to its text goes on with it
   (`Gﾗﾌｧﾘｱ.ADV:652-657`; the presenter keeps the 131 of the last key wait). 131's text may run off the screen's edge and
   foot as the engine's does (a 52-character line with a full-width `’`), so only where a character starts counts. The
   presenter leaves 131 under a picker to the original (`FIGT_B.ADV:77-80`, `Cﾃｷﾄｼ`, `Cﾌﾞｶ` open it for `Sﾌﾞﾀｲ`/`Sﾌﾞｶ`).
   After `WAO_SUB` 0 the original's window goes onto the stage as before (`LeaveWindowOnStage`, composed uncovered) and
   ours stays over it until something is drawn over its place or a window opens; the debug mask stays on the scene left
   on the stage until the whole screen is drawn over. `MessageWindowView` draws the bar, the name's plate (Cinzel 600 at
   30, capitals; a thought's dashed and quieter) and the text in `Lacquer.Text`, `Lacquer.Quiet` or the narration's colour.
   `--auto` in a window dumps `message-cg-bar-speech`, `-thought`, `-narration` once a launch; `--harem <cell>` on the
   week-18 save reaches a harem scene (CLAUDE.md, "Commands"). Tests: `MessageWindowTests`. Not seen in the game yet: a
   thought's plate (none of the four harem scenes the week-18 save reaches has one).
3. **The `]` menus** as lacquer rows (the items are data already, `MenuView`).
   **Mock-ups (2026-10-18):** `tools/ui/mock_messages.py`, round `menus`, over our 121 and `Cﾌﾞｶ`'s menus of a week-18
   run with `--harem 6`: M1 at the original box's place, M2 docked on the asking window's top edge, M3 wide banners
   over the dimmed scene, each with five items and with Do / Don't. **The owner chose M1.**
   **Built (2026-10-18):** `godot/Strategy/SceneMenuView` — `Lacquer.DrawPanel` from the box's corner
   (`GamePresenter.MenuBoxArea`, screen pixels) as wide as the box or the widest item, rows as the command panel's
   (`DrawRowBox`, the hovered one lit), ○ a gold ring, × a dim cross, △ a gold triangle, the text in the game's font
   (30 px at a 44 pitch; from six items 26 at the original's 36, so the save slots keep the box's room); a row's click
   answers its item, a right click 0. `GamePresenter.SceneMenuCovered` leaves out the selection box alone (not
   `MenuCovered`, which hides the pictures and the windows too), `MenuArea` is then null, so `MessageWindowView` no
   longer pastes the box over itself. Not over our battle screen (step 5). `--auto` in a window dumps it once a launch
   (`scene-menu`: with `--load`, the load list). Their rows scale with Debug → Menu Row Height…'s Dialogues slider (2026-10-25, docs/ui/command-screen.md, "The rows' height on a slider"). Not seen in the game yet: a short scene menu over a window (none that
   the kept saves reach with `--interface new`; `Cﾌﾞｶ`'s are our Subordinates screen's now).
4. **Letter by letter** and Option → Text Speed… (the slider, the preview; kept in `settings.cfg`): a click
   while it types shows the whole text and is not passed to the script's wait.
   **Mock-ups (2026-10-18):** `tools/ui/mock_messages.py`, round `speed`, over window dumps of our interface (a 121 key
   wait and the map's plates): the text while it types — T1 cut off at the next cell (T1b: three moments, the triangle
   only once the text is whole), T2 with a gold caret at the next cell, T3 the last six letters fading in; Option → Text
   Speed… — S1 one dialog in the middle with a small speech panel typing inside it, S2 a smaller dialog over the map's
   top and the game's own 121 at its place and size as the preview. Both sliders have ten steps, 8 to 150 letters a
   second and Instant (the numbers a proposal). **The owner chose T1 and S2** (2026-10-18), the default step 9 for now,
   and no speed caption under the slider.
   **Built (2026-10-18):** `MessageWindowView` types the glyphs at `LettersPerSecond`, cut off at the next one; text the
   script adds to the same window (its glyphs begin with those typed) goes on from there, a place's title shows whole; the
   triangle once the text is whole. `GameScreen.FinishTypingAsync` holds the player's key wait until then: a click or a key
   while it types shows it whole and is released before the wait begins; the message skip, unattended play and a menu
   opening over the window show it whole at once. Option → Text Speed… is `godot/Strategy/TextSpeedView` (S2: the dialog at
   (330,90) 620×300 over the picture dimmed by 45 %, the preview `MessageWindow.Sample` — Maris's line as `WAO_SUB` prints
   121 — in a `MessageWindowView` without the tail, typing again 1.2 s after it is whole); the game waits while it is open
   (as under the confirmation boxes). The step is `TextSpeedChoice` (`[display] text_speed` in `settings.cfg`, 1-10:
   8, 12, 18, 25, 35, 50, 70, 100, 150 letters a second and Instant; default 9). Tests: `MessageWindowTests`
   (`A_sample_is_a_121_printed_as_WAO_SUB_prints`).
5. **The battle's windows** (`BattlePresenter.Window`, through `WindowMask` on our battle screen).
   **Built in part (owner, 2026-10-18: the windows after the battle were still the original's):** the battle presenter
   prints into the surface, so it now keeps what its writer prints into the open window (`MessageWriter.Printed`,
   `TextBox.Put`) and hands 111/112/121/122/113/123 and 131 over as `BattlePresenter.MessageWindow`
   (`GamePresenter.MessageWindow` in a battle); the surface still draws the original's frame, which the presenter leaves
   out of the composed screen while ours shows it (`BattlePresenter.HideWindow`: the screen under it as the window saved
   it; owner, 2026-10-31 — held upright ours leaves the picture and the original's stayed in it), and a 131 left after op
   0 stays until the next drawing. So the windows a battle file opens off the field are ours: Rance's
   lines before `FIGT_B`'s battle, "[ Defeat ]" / "Ys was successfully defended." over the map, the captures and deaths.
   The battle's own info boxes 143/153 (frame 894) on our battle screen — "prepared for the enemy's attack", "fell in
   battle", "The battle dragged on…", the damages and EXP at the end — got their own design, below. Tests: `MessageWindowTests.The_battles_windows_are_handed_over`.
   **The info boxes 143/153 (survey, 2026-10-18).** Both ops run one routine (`WAO_SUB` `L_00c65`: frame 894, text box
   200,196 240×200); a box holds pages — text, op 11 (the key wait, the frame redrawn), more text — and op 10 closes it.
   44 call sites in `FIGT.ADV`, `FIGT作`, `FIGT表示`, `FIGTﾃｷDE`, `FIGTﾐｶDE` (`FIGT_M`'s entries are dead). Our battle
   screen has its own for the "vs" window, the order messages, Info and the strategy's announcement; the rest:
   - per round: the Land of Death's damage (`FIGT.ADV:270`, `Prov_Cur` 82);
   - per command: a unit's defence ("X prepared for the enemy's attack. Defense Increase: n", `:1397`, n 1-20), focus
     ("X is focusing.", `:1306`; Miki's "couldn't concentrate", `:1283`), the Fog Card (two pages, `:1239`), "We can't run
     from this battle." / "It's impossible to run in this narrow dungeon." (`:550`, `:560`);
   - per unit down, after its last words in a bubble: "X fell in battle." (`FIGTﾐｶDE.ADV:721`, `FIGTﾃｷDE.ADV:1804`) and
     the variants — wounded, injured and couldn't move, died, ran away, Ex's retreat, Rance's and Sill's own;
   - at the end, once: the result ("Our army was annihilated." / "retreated.", "The enemy was annihilated." / "has
     retreated.", `:496-583`), the time limit ("The battle dragged on and it became night." / "time ran out.", `:451`), the
     tally (`:2991`: each side's damages, the EXP), then one box per unit that levels up (two pages: "X leveled up.",
     the stat raised, `:3064`);
   - scripted: the kaiju's rampages in the dungeon (several pages between shouts), Maha's raid, Xacalite's contract
     (`FIGT表示.ADV`).
   **Mock-ups (2026-10-18):** `tools/ui/mock_messages.py`, round `boxes`, over window dumps of our battle screen (the
   week-18 save played to the week's end; the original's box painted out): K1 — our narration panel (113's lacquer) in the
   scene's middle, the box's narrow line breaks joined into sentences, the triangle (night, tally, defence); K2 — a band
   across the scene in the battle screen's steel, the sentence in Cinzel, the tally as two columns; K3 — the events where
   they happen: the defence a "DEF +2" badge on the unit's card with a small plate, a fall a red stamp across the
   commander's dimmed card, the end one results plate (the reason as its title, each side's damages, the EXP); K3 needs
   K1 or K2 for the kinds without a card. **The owner chose K3, the other kinds as K1's panel** (2026-10-18).
   **The owner's answers (2026-10-19)**, with two more mock-ups of the round: K3d — the level-ups as rows on the plate
   under the EXP ("LEVEL UP", the name, the gains); K3e — a "LEVEL UP" badge on the unit's card with the gains on a small
   plate. The defence's badge flashes and its wait is answered at once (as the order's label, docs/ui/battle-screen.md,
   "The order messages"); a fall's stamp waits for the player's click (since 2026-10-21 answered at once, below). The
   end: the reason's wait (the result or the time limit) is answered at once, so the plate shows the reason, the damages
   and the EXP together at the tally's wait, one click (a scene between them, `Gﾀﾐ`, plays before the plate); then
   **K3d**: each level-up adds its row to the plate, one click (its first page answered at once). Which units level up is
   a rule the presenter cannot know beforehand, so the plate grows.
   **Built (2026-10-19):** `Original.Content/Battles/BattleInfoBox` (the kinds `Panel`, `Defence`, `Fall`, `Passed`,
   `Results`; `BattleResults`, `BattleLevelUp`) from `BattlePresenter.InfoBox` (`BattlePresenter.InfoBoxes.cs`): the presenter
   keeps the script locations printed into an open 143/153 (frame 894; not the "vs" window, the order messages, the units'
   info, the strategy's or the scout's) and decides at its key wait — `FIGT.ADV:1399` a defence on the acting slot's card
   (the badge the printed increase, the plate the sentence without the name and the number); a text of `FIGTﾐｶDE` /
   `FIGTﾃｷDE` a fall on the card of `Unit_Cur` / `Cmdr_Cur`, its stamp by the line (FELL IN BATTLE, WOUNDED, DIED, RAN
   AWAY, CAN'T MOVE, RETREATED, FAINTED, COLLAPSED, DEFEATED: the 12 boxes of the defeat scenes); the reasons (`:453`,
   `:458`, `:465`, `:498`, `:518`, `:542`, `:585`) passed and kept at their close (a second, the enemy annihilated after the
   night, joins the first and gives the title: Night Falls, Time Runs Out, Victory, Defeat, Retreat; none, a duel's:
   `Bt_Result`); the tally (`:2994`/`:2999`, `:3003`) the plate (each army's name as the tally prints it, the damages, the
   EXP); a level-up (`:3065`) passed on its first page and a row on its second (the unit's name, "Leader Attacks +1" and
   the rest flowed: a modifier's "… UP", " UP" alone for BTX-09, Mill's beasts, the Popularity Staff); a fall or a defence
   without its card, and every other box, our panel with the page's lines flowed into sentences. The windows' layer leaves
   frame 894 out but on the strategy's screen. `godot/Battles/BattleInfoLayer` (on `BattleBoardView`, over the cards)
   draws them; `GameScreen.WaitForKeyAsync` answers a defence (its badge flashing 1.8 s), a fall (since 2026-10-21, below)
   and a passed box at once.
   `--auto` in a window dumps `info-defence`, `info-fall`, `info-results`, `info-level-up`, `info-panel` once a launch
   (`info-fall` gone since 2026-10-24, below: a fall is `info-panel`).
   Tests: `BattleInfoBoxTests` (the kinds, and over every capture: the badge is the slot's new state, the stamp's card is
   down, the plate holds the losses and the EXP and one row more per level-up, every tally a plate). Seen in a window from
   the week-18 save (the stamp, the badge, the plate); not seen yet in the game: our panel and a level-up's row.
   **The defence redrawn (owner, 2026-10-21):** the badge was hard to read and its plate above the card covered the card
   above it (or a speech). Mock-ups D1-D5 (`tools/ui/mock_defend.py`: a shield on the portrait, a ribbon across the card,
   the event over the army in the scene, a rising number, a caption at the scene's foot); **the owner chose D2 and D5
   together**, built: the card glows blue (the acting card's is gold), a steel ribbon with gold edges across its middle —
   its tails past the card's edges, a shield, "DEFEND", the printed increase ("DEF +5") in gold —, and the sentence with
   the unit's name as a lacquer caption at the scene's foot on the defender's side, above the order strip, left out while
   a speech bubble or its portrait is up (`BattleInfoLayer.Speaking`); the card's order line says "Defend" (it overran
   as "Defending"). Seen in a window from the week-18 save (Barres's (unit 6) defence; Cordoba's (unit 3) under Haurein's
   speech, the caption left out). **The ribbon's label (owner, 2026-10-21):** "DEF +n" read as a gain of the Defense
   stat, while it is n points off the enemy's hit chance against the unit till the round's end (docs/battle/01-flow.md
   §8.3, 02-resolution.md §6.10). Label rounds L1-L7, M1-M6 and N1-N6 (`tools/ui/mock_defend.py`, each N drawn at −2 %
   and −21 %, the increase's range); **the owner chose N5**, built: "DEFEND" at 19 px, "ENEMY HIT −n%" in gold at 16 px
   right-aligned (`BattlePresenter.DefenceMark`), the shield moved onto the ribbon's left tail to make room.
   **The ribbon lowered (owner, 2026-10-24):** across the card's middle it hid the troops and the commander's HP; it now
   lies across the card's foot (40 px high, over the portrait's stats, under the order line). While MsgSkip skips it
   holds 0.35 s instead of 1.8 s, and one still up when the skip begins fades out at once (`BattleInfoLayer.Hurry`).
   **The fall without its click (owner, 2026-10-21):** a defeat played two clicks — the fallen one's line in its bubble
   (`WAO_SUB` 151 or 141, then 11's wait), then the box 153 or 143 with its sentence ("… was injured and couldn't move.",
   `FIGTﾃｷDE.ADV:1811-1830`; "… fell in battle.", `FIGTﾐｶDE.ADV:721-727`) and its own wait, our stamp. Now the stamp's
   wait is answered at once, as the defence's: the stamp flashes on the fallen card (`BattleInfoLayer.Flash`: it rises,
   holds and fades as the ribbon, the card dimmed under it, in 1.1 s, `BattleInfoLayer.FallSeconds`), the battle held for
   the whole of it — a stamp still fading when it went on lay on the card that moved up into the fallen one's place.
   **The fall as the original's (owner, 2026-10-24):** the stamp and its 1.1 s hold are gone, and the kind `Fall` with
   them. The original has no mark on a fallen panel: after the box `L_03058` redraws the field with the fallen one's
   panel as any other (its slot is compacted only after `Y 3, 10`, 160 ms) and the panels move up at the next redraw
   (docs/battle/screen-layout.md, §5). The fall's box is our panel with its sentence ("Barres fell in battle.") and
   waits for the player's click, as the original's 143/153 does: a defeat is three clicks again — UNABLE TO FIGHT, the
   line (when the unit has one), the box. Showing the stamp at UNABLE TO FIGHT was weighed and put aside: its word comes
   from the defeat scene's branches (the record and story flags), known before the scene only by a dry run of it.
   Some units have no box at all (Bound (unit 3) and Soul (unit 4) in the prologue, Rafalia (unit 34), Arlcoate (unit
   35), Aviator (unit 36), Gandhi (unit 37), Chizuko (unit 38), Nagi (unit 40), unit 45, Magic (unit 39) on one branch):
   their fall is the line alone, as in the original.
   **The panel lower (owner, 2026-10-24):** in the scene's middle it stood too high over the field; its foot is now at
   y 384, 26 px over the scene's edge and level with the defence's caption's (`BattleInfoLayer.PanelFoot`), a longer
   panel growing up from it.
6. **The message log** — its design discussed first (backlog).
   **Survey (2026-10-19).** The original has none (xsystem35 neither); Sengoku Rance has no battle text log either
   (docs/sengoku/04-screen.md:636). What a log can hold, and where our presenters already have it:
   - **One entry is one key wait's page.** A scene's window keeps its text until the key (`GamePresenter.Window`, ops 1/5/11:
     `_afterKey` clears it), and one open window may hold several pages (`Gﾏﾘｽ.ADV`: Maris's 121 holds four pages
     between `\L_03c38` waits before it closes). At that wait `MessageWindow.Of` already gives the kind (speech left or right,
     thought, narration, a place's title, 131), the lines at the script's breaks and 131's speaker; so an entry needs no new
     reading of the text, only a copy taken at the wait. This works in both interfaces (the model does not depend on the cover).
   - **The speaker.** 131 names it in the text (`Speaker`, `Thinks`, `Unclosed` across windows). 111/112 speak for the left
     portrait, 121/122 for the right one (`Ui_PortraitL` / `Ui_PortraitR`, set by `WAO_SUB` 311/321 from `CallArg`): the
     scenes set a portrait 2,056 times on the left (162 CGs) and 3,971 on the right (230 CGs). A record's name
     (`Unit_FaceCg` / `Cmdr_FaceCg` in `SET_CHR.ADV`) names one face alone for 1,643 of the left settings and 1,606 of the
     right; Lia (CG 5, 276 right) and Maris (CG 6, 508 right) have no record, so names from the records miss half the right
     side. **Every portrait carries its name on a plate baked into its bottom right** (192×256 at 1×, the plate in about
     (48,220)-(190,254): `RANCE`, `LIA / PARAPARA LEAZAS`, `MARIS AMARYLLIS`, `LEAZAS GREEN KNIGHT`): the log can show that
     plate cut from the CG, which names every portrait without a table. A window without its portrait (0) has no speaker.
   - **Narration and places' titles** (113/123, `MessageKind.PlaceTitle`) are entries of their own; a title can mark where a
     scene starts. **Status windows** (`Cﾌﾞｶ`'s `HP    4/    5`) are 113/123 too — kept at their cells they stay tables.
   - **The `]` menus' answers:** the presenter opens a menu (`OpenMenu`) and closes it without the answer (`CloseMenu`), so
     logging a choice needs the answer passed in. Only the scenes' questions are dialogue; the command menus (`COMMAND`, the
     cities') would be noise (an open window does not tell them apart: see "Built" below).
   - **The battle:** its windows off the field (`BattlePresenter.MessageWindow`, the same model), the bubbles' lines
     (`BattlePresenter.Speech`: the side and the text; the speaker is the acting card's unit or commander), the info boxes
     (`BattlePresenter.InfoBox`: falls, the results plate).
   - **The message skip** still runs every window op (only the waits are answered), so skipped pages can be logged as read
     (visual novels do) or left out. **`--auto`** answers the same waits: the log fills the same way.
   - **How long:** the launch (memory, a cap of pages), or kept with the saves in `ScriptState.Extras` (as
     `KnownProvinces`; a slot format change, the autosave every week grows by the log).
   - **Rules:** none — the log is presentation; the waits stay the script's.
   **Mock-ups (2026-10-19):** `tools/ui/mock_log.py` over the week-26 save's dumps (Maris's scene, the page "I believe that we
   should first…" open; our 121 drawn under the log): **L1** — a lacquer "Message Log" panel over the dimmed screen, one run
   of a speaker's pages under the face (`UnitFaces`' square) and the portrait's own plate, the text at the script's breaks in
   the game's font, narration on a brown band, a place's title as a divider, the newest page marked by a gold bar, opened at
   the bottom; **L1b** — the same scrolled to the scene's start; **L2** — compact: our names in Cinzel in a column (the
   records' names, so a table for the rest), more pages a screen; **L3** — the chronicle: the ledger's paper (`Lacquer.Paper`,
   vermilion names, ruled lines); **L4** — no panel: the wheel turns the open window back page by page (a "LOG 4 / 14" plate,
   the speaker's plate on its edge, no tail — the portraits on the screen stay the present ones, so a tail would point at
   the wrong speaker). Ways to open it, for the owner to choose: the wheel up at a key wait (unused in the game now), a key,
   a small button on the window, a menu bar item.
   **The owner's answers (2026-10-19):** L3, its rules a little paler, in a few more mock-ups; opened by **the wheel up**;
   kept **for the launch** (memory, a cap of pages; the save format unchanged); **the skipped pages** and **the answers of
   the dialogues' questions** are logged (not the play's menus, below); the battle's lines and week markers are not.
   **Mock-ups of L3 (2026-10-19)**, the rules at alpha 45 (the ledger's 110 for comparison, `L3-chronicle-paper`): **L3a** —
   L3 as it was; **L3b** — a blank ruled line between speakers, narration indented by a cell, the newest page marked by a
   vermilion bar at the margin; **L3c** — L3b with the portrait's own name plate cut from its CG in the margin instead of our
   names (no name table: our names need one for the portraits without a record, Lia's and Maris's among them); **L3d** —
   L3b over `GAL.ADV`'s church scene (Maris's advice, the question): the choice under a "CHOICE" label, the chosen item
   `◆` in vermilion, the other `◇` greyed; **L3e** — L3b scrolled to the scene's start: a place's title as a heading line
   in `Lacquer.HeadInk`. **The owner chose L3a** (2026-10-19): the lines close, no blank line between speakers, no mark on
   the newest page; the speaker's name in vermilion at a run's first line (a choice and a place's title as in L3d/L3e,
   without the blank lines).
   **Built (2026-10-19):** `Original.Content/Strategy/MessageLog` (the pages `LogPage`: speech, thought, narration, a place,
   a choice; `PageOf` a window at its key wait; `Lines` the ruled lines of L3a; the last 500 pages) filled by
   `GamePresenter.Log` at every key wait of a window (ops 1/5/11, either interface, the message skip's
   included) and by `MenuAnswered` for a dialogue's question. The speaker of 111/112 is the left portrait's, of 121/122
   the right one's, named by `PortraitNames` — the plate's text of the 244 portraits that have one (read from the plates,
   2026-10-19; a two-line plate by its first line, `LIA`, but where the lines are one title: `CITY AUTHORITY`, `FREE CITY
   ARMY GENERAL`, `Demon Blood Soul`; `P G - 7` as `PG-7`; 401's record says "Civilian General", its plate `HELMAN GUARD`).
   `godot/Strategy/MessageLogView` draws L3a (paper in a lacquer frame (190,28) 900×744, rules at alpha 45, the game's font
   at 28, a line too long for the column — 131's run to 52 cells — drawn smaller to fit, names in Cinzel shrunk to the
   margin); `GameScreen` opens it on the wheel up while the player is asked for a key (`CanOpenLog`; not in unattended
   play), the game waiting as under Option → Text Speed…; the wheel scrolls three lines, a right click, Escape or the wheel
   down at the newest line closes it. The log lives in `EngineState.Log` for the launch. `--auto` in a window dumps it once
   (`message-log`, from 20 pages). Tests: `MessageLogTests`, `NewGameStateTests.Every_face_of_a_new_game_has_its_plates_name`.
   **The owner (2026-10-19, after two looks): the play's information and choices stay out of the log, a dialogue's stay in.**
   The first build logged a menu's answer whenever a window was open — `COMMAND`'s menu (its status window is open) and
   `ﾛｰﾄﾞｾｰﾌﾞ`'s slots went in — and every window's narration: `ﾛｰﾄﾞｾｰﾌﾞ`'s "< Load Complete >", `MAIN`'s week banner, revenue
   and expenses. The second logged only the event scenes' menus and lost `Cﾃｲﾄ`'s question (Maris's "You summoned me, King
   Rance?", then "NEW Library"). Now (`MessageLog.Keeps`, `MessageLog.IsEventScene`, `GamePresenter.LogPage`):
   - a speech or a thought is logged from any file (the play's files have dialogues: `Cﾃｲﾄ`, `Cﾃｷﾄｼ`'s negotiations);
   - narration and a place's title only when the window's first message is an event scene's, `G*` or `I*` (the file of its
     `MessageEvent`; a window of strings alone has none and is not logged);
   - a menu's answer when the menu is an event scene's, or when the last page logged was a speech or a thought and no window
     or picker opened since — the window may have closed: `Cﾃｲﾄ` and `GAL` close it (`WAO_SUB` 0) before the menu, while
     `COMMAND` opens its status window (`COMMAND.ADV:655-659`, 113) before its own, `ﾛｰﾄﾞｾｰﾌﾞ` its windows before the slots.
   **The owner (2026-10-19):** a choice shows **the chosen item alone** — one line, "CHOICE" in the margin, `◆` and the item
   in vermilion — not the menu's other items (the page keeps them all; `MessageLog.Lines` lays out the chosen one).
   **The owner (2026-10-23):** what our interface plays unseen stays out of the log — the briefings
   of an attack and a search (`GamePresenter.Briefing`, docs/ui/command-screen.md) and an order of our Subordinates screen: `Cﾌﾞｶ`'s dialogue on
   its way (Rance's "Replenish the troops.", Maris's "Understood.", Rance's thought before a talk), which `GameScreen`
   answers unseen (`GamePresenter.AnswersUnseen` while the `SubordinateOrder` runs; the talk scene itself is logged).
   The menus answered unseen never reach `MenuAnswered`.
   **The owner (2026-10-25, a bug found on the tablet):** the windows a battle file opens off the field are logged by the
   same rules. The log had missed every page while `FIGT_A`/`FIGT_B`/`FIGT`/`死亡捕獲` ran — their events go to the
   battle's presenter, so the captures and deaths and the scenes `死亡捕獲` calls (`Gｸﾘｰﾑ`, `Gﾒﾙﾌｪｲｽ`, Kayblis's
   `Gｹｲﾌﾞﾘｽ` → `Iｼﾚｲﾌﾞ`) and Rance's lines before `FIGT_B`'s battle never reached it. Now `GamePresenter.LogBattleWindow`
   logs `BattlePresenter.MessageWindow` at its key waits (not the bubbles, the info boxes 141-153, the order messages, the
   scout's and the strategy's windows, which it never gives); the window's file is noted as outside a battle, so `FIGT*`'s
   narration ("[ Defeat ]", "Ys was successfully defended.") stays out; in a battle a menu is logged only when it is an
   event scene's (`FIGT_B`'s Intercept / Ninja Scout / Abandon City after Rance's lines is the play's). The battle's
   writer now takes a scene's `ZM` (a place's title at 32 there too; the battle's own sizes are its port's). Test:
   `GamePresenterTests.A_scene_the_battle_called_is_logged_as_any_scene` (`Gｸﾘｰﾑ` 1 and `Gﾒﾙﾌｪｲｽ` 1 log the same pages
   inside `死亡捕獲`'s screen as on their own).
   **The owner (2026-10-26):** the log opens on the map too, not only from a dialogue. The wheel up opens it while
   `COMMAND`'s map loop waits for the player (`GamePresenter.MapWaiting`) with nothing over the map — no menu or window, no
   plate's click or Subordinates order under way, no button held (a drag's release under the log would be a click on
   the map; `GameScreen.MapLogWait`, `LogOpens`), in either interface; the map waits while it is open, as the scenes do.
   In the touch mode the scenes' Skip and Log drawer shows on the waiting map too (the owner's L1; docs/ui/touch.md,
   "The message log from the map, round L"). `--auto` in a window checks it once a launch at the first map wait with
   something logged (`GameScreen.AutoMapLogAsync`: the wheel up over the map's middle through Godot's input — in the
   touch mode a tap on the drawer's Log —, dump `message-log-map`, a right click or a tap off the frame closes it,
   nothing reaches the script; the week-26 save reaches it after Lia's scene). An agent's dump of the log is now the window's in either
   interface (`GameScreen.Shot`: over the original's map it had saved the engine's surface, the log left out).

## 6. Open questions

1. ~~Point 2: monospace as now, or a proportional face with the lines rewrapped?~~ Answered (2026-10-18):
   monospace at the script's breaks, §3 item 2.
