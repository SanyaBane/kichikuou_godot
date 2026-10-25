# Our interface — the house style

The conventions every screen of our own follows, gathered from the screens' documents (2026-10-25). It records
what the owner has already chosen, not designs for screens to come: each rule names the screen where it was decided,
whose document holds the mock-ups and the reasons. Values (colours, sizes) live in the code — `godot/Strategy/UiPalette*.cs`,
`Lacquer.cs`, `MenuRowsChoice.cs` — and are named here, not copied. When a screen's document and this one disagree,
the later decision wins; update this file when a convention changes, and add one when a second screen repeats a choice.

Read this before a new screen's survey and mock-ups: the first round should not ask again what is settled here.

## 1. Principles

- **The rules stay the original's; only the interface is ours** (decisions 3, 4, 9, 25 — docs/ui/survey.md). Our screen
  draws what the core shows and answers what the core asks. It chooses only what the original's player could have
  chosen, through the inputs the script reads (`PointerScript`, `GesturePoints`); the script still hit-tests and
  decides. No figure of a rule is computed in a view: a model in `Original.Content` reads `GameState` and the rule
  methods (`SubordinatesCommand.Offer`), with tests. A rule change on purpose is a numbered owner's decision
  (26, 27, 30), never UI work.
- **An answer given for the player is the answer a click would give** — the same key in `RetVal` (quirks read it), held
  buttons waited out first (docs/ui/battle-screen.md).
- **Covering.** Our screen replaces the script's by leaving it undrawn (`DeploymentCovered`, `MenuCovered`,
  `SaveScreenCovered`, `SceneMenuCovered`, `CoverMessages`) — lead-ins and lead-outs too, so no frame of the original
  flashes — while the script runs as ever under it. The script's pointer rests on an empty cell, so it hovers nothing;
  the pointer over our screen is ours (docs/ui/deployment-table.md).
- **Played unseen:** what is the same every time or only asks again what a click already said — the attack and search
  briefings, Rance's repeated lines, the ninjas' lead-in, the file's Load / Save / Don't, Construction's Build / Don't
  (docs/ui/command-screen.md, "The province menu"; docs/ui/enemy-attack.md). Its key waits are answered at once and it
  stays out of the message log.
- **Kept seen:** anything that is not repetition ends an unseen stretch and is shown — refusals, warnings (Maris's snow
  advice), story scenes, battle results, the units that refuse to go. Fidelity wins when in doubt: a fall's box waits
  for its click as the original's (owner, 2026-10-24).
- **Collapse what another element already carries:** the order messages (the card's label flashes), the battle's info
  boxes (the stats are always on the cards), the later round headers (the gauge), several windows into one screen with
  one click (the week report, the scout report, the results plate).
- **Show more only by the owner's word, and only what the state already knows** — the province card on hover, the
  budget before and after, the dungeon floor's hints. No hint of what the original hides, no prediction of draws, no
  spoilers (docs/ui/talk-marks.md). The port's own data that no rule reads goes into `Extras` (`KnownProvinces`).
- **Keep the original's words**; our own lines are few, in the port's English, worded with the owner. Label things by
  what they do, not by a sum: "Walls +8%", "ENEMY HIT −8%", not "Advantage +n" or "DEF +n".
- **Sengoku Rance is a reference, not a template.** Its art never enters the repository; our effects are drawn by code
  in its manner. Where it fights the original's habits the owner chose closer to the original (the harem's numbered
  grid, the centred province menu — docs/ui/pickers.md, 2026-10-19).
- **Not "a browser game"**: flat grey boxes, thin borders, Godot's stock buttons, pills and capitals everywhere were
  rejected (docs/ui/command-screen.md, 2026-10-16; docs/ui/subordinates.md, 2026-10-18).
- `--interface original` keeps the original's screens for a launch; the player always has ours (2026-10-21).

## 2. Materials and colour

- **Lacquer and gold** is the house material (the owner's 4a, 2026-10-16): a near-black panel, a double gold frame,
  gold corner studs — `UiPalette.DrawPanel`. Full screens stand on the **wave backdrop**, a faint gold seigaiha with a
  vignette (G1/G3, 2026-10-18; `DrawWaveBackdrop`).
- **Paper ledger** for tables and long text (G3): paper, ink, vermilion Cinzel headers over a rule, quieter ink for
  secondary columns, grey for rows that cannot be used, the chosen row a vermilion bar; the party's rows gilt, a place
  a vermilion seal. The message log and the options window are paper in a lacquer frame.
- **Colours by role, never by value.** A view declares `Ui => UiPalettes.For(UiPart.…)` and draws with its roles
  (`Panel`, `Gold`, `Row…`, `Text`, `Paper`, `Ink…`, `Danger…`, the marks). The palettes are Lacquer (default) and
  Steel, one choice in Option → Display for every part (`Battle`, `Map`, `Messages`, `Screens`, `Settings`); a part's
  own palette is read but not offered. A change repaints open screens at once (`UiPalettes.Repaint`; a view that
  keeps colours outside `_Draw` is an `IUiPaletteUser`). docs/ui/options.md, "The palette".
- **Left in their own colours on purpose:** the command and province tags, the red buttons, the battle's banners, the
  defence ribbon's blues, the read-text colours, the map's plates (steel).
- **What the colours mean:**
  - red lacquer in gold — the primary, committing action (Turn END, Replenish, Load this game, Yes, Retreat);
  - dark red in vermilion with a bin — destroying something (Delete; docs/ui/save-load.md, the owner's B5);
  - vermilion — the asked or chosen item, seals, prices; gold — a highlight or a drop target; the accent — the active
    thing (the acting card, the speaker); fixed cyan — the target bracket, never the accent;
  - player blue, enemy red; up green, down red; quiet ink for "0", "—" and the maximum in "980/1,000";
  - a strength bar green from ¾, amber from ⅓, else red — a look only.
- **Overlays dim (and in the battle blur) what is under them**; a plate dims the screen only a little.
- Drawn by hand in the 1280×800 screen's pixels (`k` = window pixels per screen pixel); a line is at least one window
  pixel wide (`Lacquer.Line`).

## 3. Type

- **Shojumaru** (`Lacquer.Brush`) — titles and names: panel titles, unit and place names, army names, Turn END, the
  touch buttons, a strategy's title.
- **Cinzel** (`Lacquer.Cinzel(weight)`, 500-900) — subtitles, labels, ledger headers, plates, dates, the name plate
  over a window (600, capitals).
- **The game's font** (MS Gothic or BIZ UDGothic, the default; `GodotGlyphRenderer.Chosen`) — the script's text, at its
  own breaks and cells, never rewrapped (the battle's balloons the one exception), and our figures and ledgers.
- **Oswald** — the exchange's big rolling counters only.
- All bundled in `godot/fonts` under the SIL OFL with their licence files.
- There is no type scale: sizes are set per screen. The one derived scale is the menu rows (`MenuRows`): 38 px rows on
  the PC, text 19 (text = 8 + 0.29 × row), a tag's word 4 smaller; the scene menus 40 px; touch rows 46 px
  (docs/ui/command-screen.md, "The province menu"; docs/ui/touch.md).
- **Figures go through `Original.Content/Figures`**: the thousands separator is the player's choice (Comma by default),
  "K GOLD" alone or in a sentence, "K" in tables, buttons and cards, the minus U+2212; the scenario's sums in our windows
  get it only where they are verified money (`ScenarioFigures`; docs/ui/gold-figures.md).
- A label that overruns is shortened ("Trp.") or a name shrunk to fit its row, never wrapped into the next.
- The date reads "LP 3 · Month 5 · Week 2".

## 4. Parts

Shared parts, with their users; a new screen takes these before drawing its own.

| Part | What it is for | Code |
|---|---|---|
| Panel | every lacquer surface | `UiPalette.DrawPanel` |
| Title band | Shojumaru title, Cinzel subtitle, the date right, a divider | per view, `DrawDivider` |
| Row box | a menu row, a secondary button; lit frame on hover, grey when off | `DrawRowBox` |
| Tag | a command's coloured word | `DrawTag` |
| Red button | the primary action | `DrawRedButton` |
| Danger button | a deletion | `SaveLoadView.DrawDelete` (not shared yet) |
| Click's triangle | "a click goes on", bobbing, shown once the text is whole | `DrawTriangle` |
| Callout | a question or the script's refusal at the clicked place, Yes (red) / No inside | `TreasureCallout` |
| Result plate | an order's outcome in the middle, waits for a click | `OrderResultPlate` |
| Ledger | a sortable table on paper | `DeploymentTableView` |
| Seal, badge | a number on vermilion; a place on a gold diamond | `DrawSeal`, `DrawBadge` |
| Pill | NEW / ??? on an action button | `DrawPill` |
| Picture panel | the original's unit / girl picture with its own frame | `FacePictures.PicturePanel` |
| Face square | a unit's face cut for cards (owner's crops in Debug → Portraits) | `UnitFaces` |
| Dashed frame | an unknown or empty place | `Lacquer.DrawDashed` |
| Tooltip | instant, context-menu style, never over a dialog | `AdvantageTip`, `HeaderTip` (touch) |
| Touch controls | one lacquer look: pill, drawer, Commands | `TouchPill`, `SkipDrawer`, `CommandsButton` |

The battle has its own family (cards on stones, pennant banners, ribbon and caption, cut-in, speech balloon,
opening plate) in docs/ui/battle-screen.md and docs/ui/messages.md, §5 step 5; the message windows (speech, thought,
narration, place title, 131) in docs/ui/messages.md.

## 5. Interaction

- **Ask at the click, not in the middle**: a question is a callout at the button or cell clicked, so the eyes and the
  pointer stay there (W2 over W1, 2026-10-19; C3, 2026-10-20). It is short ("Are you sure?") when the button already
  shows the figures. It appears at its whole size, never growing in sight. Nothing reaches the script before Yes.
- **No / Keep** is also a right click or Esc; a question left open when its screen closes is a No.
- **Before destroying:** a callout saying "It cannot be undone." with Delete | Keep; a sweep says how many it takes
  ("Delete all without ★ (3)"), is off at 0, and veils the rows it would take while it asks. One question, never two.
- **A choice then a red button is the confirmation** — no question after it (Save/Load C, the harem's Summon).
- **Results** go on the plate in the middle and wait for the click (since 2026-10-24: "gone by itself, it could be
  missed"); no character reports them. Small events flash instead (a label goes saturated, holds, fades) and their
  wait is answered at once; a frequent battle result holds a moment (Option → Battle) unless it carries a wiped-out mark.
- **Back and cancel:** right click and Esc answer 0 everywhere, and there is a Back button too, so no command needs the
  right button. Where the script's cancel only reopens the same menu, our screen ignores it (`CancelReopens`). A left
  click on a panel does nothing but its buttons; a click off a target's banners does nothing.
- **Buttons act on release**; a battle target's banner at the press.
- **Hover changes colour only** — nothing grows or moves. A hover over a row or card is the original's hover of that
  cell, so the script shows its details. Only what can be done lights up.
- **Drag and drop:** starts after 6 window pixels; the card dims and its face follows; the target glows gold with
  "Swap" or "Move"; the wanted order shows at once while the script's clicks run.
- **Refusals by their kind** (owner, 2026-10-25): a refusal that is a plain condition the state shows — spent this
  week, not enough gold, already full — is greyed beforehand with its reason as the tooltip ("used this week", "not
  enough gold"), so the script's refusal is never reached (Subordinates). A refusal that is a character's line, or part
  of the original's flow a screen keeps (a picker played through, Treasure V), is the script's own, in the callout at
  the click with the click's triangle.
- **Dimmed or hidden by meaning** (owner, 2026-10-25): an action that exists but cannot be taken now and will be
  again (spent, too dear) is dimmed — the player should know it is there. One that does not exist here at all (up from
  the top floor) is hidden, keeping its room (the dungeon's ladder).
- **A stable layout:** a mark that goes, a way not offered or a sort arrow leaves its room, so nothing else moves
  (the dungeon's ladder, the ledger's columns).
- **Marks for new content** (decision 30): NEW green, ??? amber, nothing once read; a pill on the action, tooltips in
  words; shown only where they can be acted on; "nothing new" stays a quiet figure.
- **Sorting a ledger:** the first click sorts more-is-better columns descending, №, names and prices ascending; stable,
  ties by number, "—" last; a second click reverses. Lists of saves are newest first with no switch.
- **The first choice when a list opens** is the likely one: the newest save to load, New save to save, the girl chosen
  before.
- **Text types in** letter by letter (Option → Messages → Text Speed, default 9 of 10); a click while it types shows it
  whole and is not passed to the script.
- **Settings apply at once** and are saved to `settings.cfg`; the game waits under the options window.
- Under MsgSkip and `--fast` every animation is instant.

## 6. Touch

docs/ui/touch.md. The tablet first, phones put off (2026-10-24). Touch mode is Android or `--touch`; the PC's mouse
interface does not change.

- A finger needs 7-9 mm: rows 46 px with 8 between; secondary items get a row box too, since no hover shows they can
  be tapped.
- Select, then act: nothing irreversible on one tap. A long press (0.5 s) gives details; a finger that moves more than
  12 px is neither tap nor press.
- A tap off a panel is the right click's cancel; Commands becomes Close while the panel is open.
- Turn END is only in the command panel (Commands, then Turn END), on a phone too, and asks nothing; no button on the
  map ends the week (owner, 2026-10-25; the phone's mock-up M1 put it on the map's rail, asking first).
- Controls sit where the thumb learns them (the drawer at the left edge, Commands bottom right), never over a face, a
  name plate or text; they hide under menus, dialogs and our screens, and their taps never reach the script.

## 7. How a screen is made

1. **Survey** — what the script shows and asks, statically from the scenario, with open questions for the owner
   (answered and struck through in the document).
2. **Mock-ups** over window dumps of a kept save (`saves-week18`), drawn by `tools/ui/mock_*.py` (each script's header
   gives its dump command); lettered rounds, numbered variants, the agent's recommendation, made-up figures marked.
   The images hold the player's CGs and stay outside the repository. Mock-ups come before code even after a choice in
   words.
3. **The owner chooses** — often a mix ("D2 and D5 together") or a new round.
4. **Build**: the model in `Original.Content` with tests first, then the Godot view, then unattended play that dumps
   it (`--auto --shots`, with the screen's switch: `--treasure`, `--saveload`, `--refill`, `--scout`, `--touch`…).
5. **The owner's look** in a window; the document says where it stands ("awaiting the owner's look", "accepted",
   "not yet on the tablet") and lists the agent's own choices for the owner to confirm.

The screen gallery (docs/ui/gallery.md; step 1 built: Treasure, Harem, Subordinates, the message windows) shows our
screens in their states without playing to them: Debug → Screen Gallery…, and `tools/ui/gallery.ps1` dumps them
outside the repository with an index marking what changed since the last dump. A screen that is in it gets its new
states as entries when it changes.

Debug sliders (Menu Row Height…) are for trying sizes, not for the release. Never dump a list of every face.

## 8. Open questions

None for the owner: the refusals, dimmed or hidden and Turn END on a phone were answered on 2026-10-25 (§5, §6).

- **The danger button** is drawn privately by `SaveLoadView`; it becomes a `UiPalette` part when a second screen needs
  one.
