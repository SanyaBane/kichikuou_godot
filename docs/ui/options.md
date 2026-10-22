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
| Palette | Navy / Gold / Steel / Parchment / Crimson | `DeploymentPalette.Chosen`, saved |

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
