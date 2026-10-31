# The options window

Owner's request (2026-10-21): every item of the menu bar's **Option** menu moves into one modal window of our own.

## What it holds

The Option menu as it is (`godot/Strategy/GameScreen.MenuBar.cs`, docs/window-menu.md):

| Item | Kind | Kept where |
|---|---|---|
| Mouse Movement | switch, checked at start | `EngineState.MouseMovement`, for the launch |
| Skip Unseen Text | switch | `MessageSkip.SkipUnseen` |
| MsgSkip Speeds Up Fades | switch, on by default | `MessageSkip.FastFades`, for the launch |
| Forget Seen Messages… | an action after a confirmation | `MessageSkip.ForgetSeen` |
| Battle Screen… | twelve timing sliders and the Effects switch, an order strip as the preview, Defaults; non-modal today | `BattleScreenSettings`, saved |
| Text Speed… | a slider of ten steps, the game's 121 typing as the preview; OK / Cancel | `TextSpeedChoice`, saved |
| Game Folder… | the import screen after a confirmation; greyed out while the extract is the data | `game_dir`, saved |
| Fonts | MS Gothic / BIZ UDGothic | `GodotGlyphRenderer.Chosen`, saved |
| Palette | Lacquer / Steel (since 2026-10-24; Navy / Gold / Steel / Parchment / Crimson before, the battle screen's) | `UiPalettes.Chosen`, saved |

## Mock-ups, round 1

`tools/ui/mock_options.py` over the week-18 save's map (the images stay outside the repository). Shared by all: Option
on the bar opens the window at once (no drop-down), the game waits under it, each change applies at once, Close, Esc
or a right click shut it.

- **A** — a lacquer panel, the sections (Messages, Display, Battle, Game) as an index on its left: A1 Messages (the
  text speed with its preview, the skip switches, Forget, the mouse), A2 Battle (Effects, the timings in two columns,
  the order strip's preview); Defaults resets the page shown.
- **B** — one paper sheet in the import screen's look with every setting at once, in two columns: B1; the battle's
  timings on a second sheet behind "Timings…": B2.
- **C** — a wide lacquer panel, the same sections as tabs across its top: C1 Display (fonts with samples, palette
  swatches, the game folder), C2 Messages (the speed left, the switches right).

The owner chose **C in B's colours** (2026-10-21).

## Mock-ups, round 2

- **D** — C's tabs on B's paper: the paper sheet in the lacquer frame, the tabs paper too (the current one joined to
  the sheet, a vermilion edge on it, the others darker paper), "Options" in the brush right of them, Defaults (the tab
  shown) as a vermilion link at the sheet's foot, Close the red button. D1 Messages (the speed with its preview left,
  the switches and Forget right), D2 Display (the fonts with samples, the palettes as swatches), D3 Battle (Effects,
  the twelve timings in two columns, the order strip's preview), D4 Game (the game folder, Change…).

The owner's go-ahead: 2026-10-21.

## Built (2026-10-21)

`godot/Strategy/OptionsView` after D, drawn by hand in the 1280×800 screen's pixels over the picture (as the old Text
Speed dialog was); `GameScreen.OpenOptions` opens it from the bar's Option button (a plain button now, no drop-down) and
applies the font and the palette to the open screens (`ApplyFont`, `ApplyPalette`). The text speed now applies at once
(the old dialog had OK / Cancel); the battle's settings now wait with the game (the old dialog was not modal). Forget
and the folder's Change… ask in a lacquer box over the sheet (Esc or a right click take it away). The preview of the
text speed is the game's 121 scaled into its column; the order strip's is `OrderStrip` itself with the old dialog's
chips. `TextSpeedView` and `BattleScreenSettingsDialog` are gone. `--options [--shots DIR] [--quit]` opens it over the
title and dumps each tab in a window (`options-messages`, `-display`, `-battle`, `-game`).

## New scenes first (2026-10-22)

Owner's decision 30 (docs/ui/talk-marks.md §5, §8 step 7): the Game tab holds, under the game folder, a section
**Scenes** with the check box **New scenes first** ("Talks and harem visits play unread scenes first, with a few hints
and a choice of ours."), on by default — `NewScenesChoice`, `[game] new_scenes_first` in `settings.cfg`. `GameScreen`
copies it onto the game's context (`OriginalScriptContext.NewScenesFirst`) when the session starts, and the window's
`NewScenesFirstChanged` keeps that context current when the player switches it during a game. The Game tab now has
Defaults too: it turns New scenes first back on (the game folder has no default and stays as it is).

Since 2026-10-27 the check box is on the Tweaks tab ("Tweaks" below).

## Thousands (2026-10-23)

Owner's request: one standard for the figures of our own screens, the separator of their thousands a choice. The Display
tab holds, under the palettes, a section **Thousands in gold, troops and damage** with five radios, each its sample:
`129,300K GOLD` (Comma, the default), `129.300K GOLD` (Period), `129 300K GOLD` (Space: a no-break space, since none of
the bundled fonts has a thin one), `129'300K GOLD` (Apostrophe), `129300K GOLD` (None, as the original prints) —
`ThousandsChoice`, `[display] thousands` in `settings.cfg`; Defaults turns it back to the comma. The window's
`ThousandsChosen` redraws the open screens.

Every figure goes through `Original.Content/Figures` (the separator `Figures.Separator`, set from the choice at the
launch). Its rules, the owner's S3: gold in K — **`K GOLD`** where the amount stands alone or in a sentence (the budget
over the Subordinates and deployment screens and the week report, a save's page, the status strip, the province card,
the Refill question), **`K`** in tables, buttons and cards (Cost and Refill, the orders' buttons, the order's plate,
War expenses, War cost) —, no space before the K, a minus as U+2212. Troops, HP, EXP and damage take the separator
too; the exchange's rolling counters (`ExchangeCountersView`, one digit a cell after Sengoku Rance's) keep plain digits.
The screens of the original engine (`--interface original`, the merchant, construction) keep the original's numbers.
The scenario's own sums in our message windows and menus take the separator too, only at the places verified to be
money (docs/ui/gold-figures.md, 2026-10-24: the survey, the owner's answers, `ScenarioFigures`).

## The palette (2026-10-24)

Owner's requests of the day: the battle screen's palette split from the other screens' (the map's plates), the other one
dropped (the plates keep steel's colours as constants of their own, `MapMarkersView`), a Lacquer swatch for the battle
screen to try — and, the owner liking it, one palette for every screen of ours: the lacquer, with Steel to see a change.

- **The palette** is `godot/Strategy/UiPalette`, a record of colours by their role — the lacquer's (`Panel`, `Gold`,
  `Row`, `Text`, …), the ledger's paper and inks (`Paper`, `Ink`, …), the marks' — with the lacquer style's parts drawn in
  them (`UiPalette.Drawing.cs`: the panel, a row's box, the dividers, the backdrop, the seal, …). Two presets:
  **Lacquer**, the default, the colours our screens had as `Lacquer`'s constants, and **Steel**, the battle screen's
  steel of 2026-10-12 carried over every screen (cool greys, a cool paper with slate inks; the red, the bars and the marks
  unchanged). `godot/Strategy/Lacquer` keeps only what takes no colour (fonts, line widths, baselines, a diamond).
- **The parts.** Each screen draws with its part's palette, `UiPalettes.For(UiPart.…)`: `Battle` (the battle screen, the
  scout report, a strategy's cut-in, its info boxes, the touch rails and Skip), `Map` (the command panel, the status
  strip, the province menu, the Commands button), `Messages` (the message windows, the scenes' menus, the message log,
  the Skip and Log drawer), `Screens` (deployment, Subordinates, Harem, Treasure, Save/Load, an enemy attack, the week
  report), `Settings` (the options window, the import screen). Option → Display → **Palette** (a row of swatches) sets
  every part, `[display] ui_palette` in `settings.cfg`; a part's own palette, `[ui_palette] <part>`, is read but no option
  sets it yet — a mixed palette later is a choice per part (a part split by a new member of `UiPart`) or a palette
  `with` some roles of another. `--palette lacquer|steel` sets every part for a launch. Defaults turns it back to Lacquer.
  The old keys (`palette`, `palette_battle`, `palette_other`) are no longer read.
- **A change** recolours every open screen at once: `OptionsView.PaletteChosen` → `UiPalettes.Repaint` (every canvas
  item redrawn; what keeps colours outside its `_Draw` is an `IUiPaletteUser` and sets them again — the battle screen's
  theme and cards, the order strip's chips, the ledger's theme and rows, the comment field of Save/Load).
- **Left in their own colours** (on purpose): the command and province tags, the red buttons and their text, the
  battle's banners, the defence ribbon's blues, the read-text colours, the map's plates.

## The unit tables' row height (2026-10-26)

Owner's request: the height of the Subordinates and deployment ledgers' rows in Options, on the PC and on Android alike
(docs/ui/unit-ledger.md §7-8, mock-up O2). The Display tab holds, under Thousands, a section **Unit tables: row height**
with five tiles, 24, 30, 36, 42 and 48 pixels, each two ledger rows at its height as the tables draw them (Rance's gilt
row with his seal, Leila's plain), the chosen one framed in vermilion, the mode's default marked "— default" —
`LedgerRowsChoice`. The PC and the touch mode keep their own, `[display] ledger_row_height` (24 by default, the text's
own row) and `ledger_row_height_touch` (36) in `settings.cfg`; Defaults sets the mode's own. The tables read it each
frame, so a change shows at once on a table open under the window. A tile's target is its whole column, the tallest
tile's height, a finger's on the tablet.

## Tweaks (2026-10-27)

Owner's request: the damage forecast is a convenience, but it changes how the game plays, so its setting leaves the
Battle tab for a tab of its own, named for what it holds — the settings that change the original's play. The owner
chose the name **Tweaks** (of House Rules, Gameplay, Tweaks, Assists & Fixes, Modern) and "New scenes first" with it.
The next to come is an optional fix of a quirk: Rance's or Kentarou's troops doing no damage to a demon commander while
their commander is off the field (his troops over the battle's limit).

- **The tab** is the last, after Game (the existing ones keep their places). A line of ours heads it: "Each setting here
  changes how the original game plays. Everything else keeps the original's rules." Its sections: **Battle** with
  **Damage forecast** (as it was on the Battle tab, its hint unchanged) and **Scenes** with **New scenes first** (as it
  was on the Game tab).
- **Kept where they were**: `[battle] damage_forecast` (`BattleScreenSettings.Forecast`, `--damage-forecast 0|1`) and
  `[game] new_scenes_first` (`NewScenesChoice`) in `settings.cfg`, so the player's choices carry over.
- **Defaults**: on Tweaks it turns the forecast off and New scenes first on; on Battle it no longer touches the forecast;
  on Game, which holds only the game folder now, it is greyed out.
- **Chaos and Nikkou reach demons** (decision 32, docs/tweaks.md): under the forecast in Tweaks' Battle section, its
  name and hint in the check boxes' column, three radios — Off — as the original (the default), On the field, Out of
  reach — and a line telling the two apart; `[tweaks] demon_reach`, Defaults turns it off.
- **Ninja follow-up** (decision 33, 2026-10-29, docs/tweaks.md; mock-ups N1 and NP, `tools/ui/mock_ninja_chance.py`,
  Never left out after them): under "Chaos and Nikkou reach demons", its name and hint ("… Ninja never kills.", the
  owner's words), three radios in the reach radios' columns — 33% — as the original (the default), 67%, 100% — and no
  note; Scenes moves down. On the phone held landscape Battle's head runs across both columns and the row tops the right
  column, the radios' words short with "33% is the original's." under them, Scenes below it. `[tweaks] ninja_chance`,
  Defaults sets it back to 33%.
- The Battle tab's first section is "Effects" again; `--options` and the screen gallery dump the new tab as
  `options-tweaks`.
- The tweaks, each with its doc, and the survey of the next one (BRS-24, a demon's commander out of reach) are listed in
  docs/tweaks.md.

## Mouse movement (2026-10-27)

Owner's request: the Messages tab's **Mouse movement** (the script's `IZ` moving the system pointer, docs/window-menu.md)
says what it does, is kept between launches, and is gone from the touch mode.

- **Its hint** is "After a menu, the pointer glides back where it was." — every `IZ` of the scenario but the music
  room's puts the pointer back where `IM` read it before a menu (`COMMAND`, the command files, `ｽﾀMENU`); the old hint
  ("The pointer moves to the chosen item.") said otherwise.
- **Kept** in `settings.cfg` as `[input] mouse_movement`, on by default (`MouseMovementChoice`; `EngineState` held it
  for the launch before).
- **The touch mode** (Android or `--touch`) leaves the Mouse section out and has the option off whatever the PC's
  choice: a finger leaves no pointer to put back, and the script's position alone moves, as with the option off.
  Defaults on Messages leaves the PC's choice alone there.

## Screen margins (2026-10-31)

Owner's request: margins from the screen's edges on Android, upright and landscape each their own, for rounded corners
and a film's opaque edge (docs/ui/screen-margins.md; the owner chose C). Display holds them in the touch layout only:
upright a section under the orientation, held landscape a field at the left column's foot beside Defaults | Close, on
the tablet a section right of the palette. A tap opens the calibration over the screen the window was opened from, the
window hidden meanwhile and shown again after it. Defaults on Display resets both sets to none.

## The message skip's switches kept (2026-10-31)

Owner's request: **Skip unseen text** and **MsgSkip speeds up fades** on the Messages tab, the last two settings of the
window held for the launch alone, are kept between launches as the others are: `[msgskip] skip_unseen` and
`[msgskip] fast_fades` in `settings.cfg`, both on by default (`MessageSkipChoice`; `EngineState` gives each message skip
it opens the kept ones). Defaults on Messages turns both on and keeps that too.

## The Battle tab without the order strip (2026-10-27)

Owner's request: the order strip under the Battle tab's timings, whose Preview made Barres jump ahead of Leila, showed a
move the game never plays — the order changes during an exchange, while the strip is out of the exchange's way, and it
comes back with the new order in place (docs/ui/battle-screen.md, "Out of the exchange's way"). Unattended play on the
week-18 save (Oaks's battle, eight weeks of attacks and defences, a battle in the Monsters' Cave (dungeon 2)) changed
the order 21 times, every time during an exchange. So the preview is gone, and with it the move itself (`OrderStrip`
places a new order at once) and its three timings, "Order strip: move", "hurry" and "drop and rise" (`strip_seconds`,
`strip_hurry_seconds`, `strip_drop_share`, `--strip-seconds`); the tab keeps Effects and nine timings, the strip's fly-in
among them. Their lines left in a player's `settings.cfg` are not read any more.
