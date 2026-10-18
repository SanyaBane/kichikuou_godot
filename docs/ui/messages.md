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

Each step behind Option → Interface → New (`godot/Strategy/InterfaceChoice`); the original's layout stays
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
   (`scene-menu`: with `--load`, the load list). Not seen in the game yet: a short scene menu over a window (none that
   the kept saves reach with `--interface new`; `Cﾌﾞｶ`'s are our Subordinates screen's now).
4. **Letter by letter** and Option → Text Speed… (the slider, the preview; kept in `settings.cfg`): a click
   while it types shows the whole text and is not passed to the script's wait.
5. **The battle's windows** (`BattlePresenter.Window`, through `WindowMask` on our battle screen).
6. **The message log** — its design discussed first (backlog).

## 6. Open questions

1. ~~Point 2: monospace as now, or a proportional face with the lines rewrapped?~~ Answered (2026-10-18):
   monospace at the script's breaks, §3 item 2.
