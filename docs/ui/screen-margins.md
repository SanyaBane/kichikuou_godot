# Screen margins on Android

The owner's request (2026-10-31): on Android the player sets margins from the screen's edges, upright and landscape
each its own. Phones round their screens' corners more or less, and a protective film may be clear in the middle but
opaque at its edges; either hides a strip of what our screens draw there. This file records what the code does today,
the model proposed and the first round of mock-ups (`tools/ui/mock_screen_margins.py`).

## 1. What the code does today

- **The phone** (`Original.Content/Devices/PhoneFrame`, docs/ui/phone-build.md, §3): the frame keeps safe insets free
  — Android's safe area (`DisplayServer.GetDisplaySafeArea`, read by `godot/Strategy/Device`), which reports the
  camera's cutout and nothing else (the Poco X6 Pro: a hole of 120 px, nothing at the foot), plus the gesture zone's
  50 px at the foot. Landscape the camera's strip is kept on both short sides (the phone may be turned round), the
  engine's picture fills the height and the safe insets only keep our controls in the sides off the edges; upright the
  room under the camera's strip and over the gesture zone is the layout's, the picture at its width.
- **Neither rounded corners nor a film are reported.** Android 12 has `WindowInsets.getRoundedCorner`, which Godot 4.7.2
  does not expose; a film is unknown to the system altogether.
- **The tablet** (the touch mode's layout, `ScreenFrame.Fit`): the picture fitted to the whole screen (the Galaxy Tab S7
  FE's 16:10 is the picture's own shape), the rails, Skip and Log, Commands in it, up to the screen's corners.

So on a phone today the status band's corners, the map's sides and the bar's outer cells (Menu, Commands) reach the
screen's edges upright; held landscape the picture's top and bottom rows and the sides' top and bottom cells do
(mock-ups P-up, P-land).

## 2. The model proposed

- **A margin is measured from the screen's edge**, in millimetres: what a film covers or a corner cuts. Each way the
  phone is held keeps its own set: **Top, Bottom, Sides** — left and right one value, as the camera's strip is kept on
  both sides landscape (the phone may be turned round) and a phone is alike left and right upright. 0 by default (the
  screen as today), 0-8 mm, by 0.1 mm.
- **Everything lays itself out inside the margins as if the screen were that much smaller**: the frame is the screen
  less the margins, the reference phone's unit follows its short side (a margin on the short side's edges makes the
  whole layout a little smaller — landscape the top and bottom, upright the sides), the long side flexes as on a longer
  or shorter phone, and outside the margins the screen is black. No screen needs a layout of its own for it.
- **The system's insets stay where they are**: each edge keeps free the larger of its margin and what it keeps today
  (the camera's strip, the gesture zone), so a margin within the camera's strip moves nothing.
- **The tablet takes the landscape set**: its picture is fitted inside the margins (only landscape is offered there, as
  the orientation is not).
- **Where**: Option → Display, on a phone and the tablet, and on a PC playing one (`--touch`, `--phone`), so the look
  can be dumped; never on the PC with the mouse. Kept in `user://settings.cfg` (`[display]`), so each device has its own.
- **Rounded corners are met by the edges' margins**: our layouts are rectangles, so a corner rounded more than the
  margins clear makes the player widen one of them. On the simulated phone of the mock-ups the film's edge alone needs
  1.5 mm, the corners 2.8 mm. A corner radius of its own (the room's corners kept free for our controls) would need every
  layout to know it; it is left out.

## 3. Mock-ups, round 1

Drawn at the reference phone's pixels over the screen gallery's phone passes (map-markers, options-display), on a
simulated phone: the corners rounded at 5.5 mm, the film opaque 1.5 mm from the edge (made up); the margins 2.8 mm on
every edge. P and R show what the margins are for, A-C how the player sets them.

- **P** (P-up, P-land) — today on that phone: the edges' controls partly hidden.
- **R** (R-up, R-land) — the same screens with the margins: laid out inside them (approximate: the dump scaled into the
  margins).
- **A** (A-up, A-land) — in Option → Display itself: a section under Screen orientation with three − / + steppers for
  the way the phone is held now (landscape in a row at Thousands' place, the tab then scrolling), a dashed line round the
  whole screen at the margins while the tab is open; the sheet lays itself out again at each step. Defaults resets them
  with the tab. The simplest to build; but a thin line is little to judge by, and nothing else of the game is in view.
- **E** (E-up, E-land) — Display's row for B and C: both sets in words and **Adjust…**.
- **B** (B-up, B-land) — Adjust… opens a screen of its own, black: the margins a gold frame with thick corner marks, a
  tab on each edge hanging 9 mm inward (the finger takes it out of Android's back gesture at the sides and its home
  gesture at the foot; the edge follows the finger's move; the camera's edge's tab at a third, off the hole), the
  steppers in a panel in the middle, Reset | Done. The player drags or steps each edge in until the frame and every
  corner mark are wholly in sight; turning the phone shows the other set. Hatched: what the system keeps free anyway.
- **C** (C-up, C-land) — B's frame and tabs over the screen the player opened Option from, dimmed and laid out live
  inside the margins as they change, the panel smaller: the player sees the screen's own buttons come into view. Done
  goes back to the options window.

**The agent's recommendation: C** — the frame shows where the edge is, the game's own buttons under it show what the
margins do to them, and it costs little more than B (every view already lays itself out again on a turn).

## 4. Open questions

1. Which way to set them: A, B or C?
2. The model of §2 as proposed — Sides one value for both, the tablet with the landscape set, a margin within the
   camera's strip moving nothing, 0-8 mm by 0.1 mm?
