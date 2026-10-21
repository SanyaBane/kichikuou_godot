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

The owner's choice: pending.
