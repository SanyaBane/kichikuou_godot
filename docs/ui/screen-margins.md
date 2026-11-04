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

1. ~~Which way to set them: A, B or C?~~ **C** (the owner, 2026-10-31).
2. The model of §2 as proposed — Sides one value for both, the tablet with the landscape set, a margin within the
   camera's strip moving nothing, 0-8 mm by 0.1 mm? Built so; among the agent's choices of §5 for the owner to confirm.

## 5. Built (2026-10-31)

C, after the model of §2. Awaiting the owner's look on the Poco and on the tablet.

- **The model** (`Original.Content/Devices`, `ScreenMarginsTests`): `ScreenMargins` — a set in tenths of a millimetre
  (Top, Bottom, Sides; 0-80), its words for Option ("2.8 mm all round", "none", or each edge) and its insets at a
  millimetre's units. `PhoneFrame` takes the margins (`Landscape`, `Portrait`, `For`): the size it is given is the room
  inside them, the system's insets are still the whole screen's, and each edge keeps free only what its margin has not
  already (`PhoneFrame.Within`); the reference's camera strip and gesture zone are measured on the whole screen.
  `MarginsCalibration` lays out C over the whole screen: the frame at the margins, the four tabs (9 mm in, 7 mm across;
  the edge follows the finger's move, `Dragged`), the panel in the frame's middle with its steppers, Reset | Done, and
  what a press lands on (`Hit`). Sized by the millimetre (the figures are the reference phone's pixels at 17.6 a
  millimetre), so it is the same size on a phone and on the tablet.
- **The setting** (`godot/Strategy/ScreenMarginsChoice`): `[display] margins_upright` and `margins_landscape` in
  `user://settings.cfg` as `Vector3i(top, bottom, sides)`; kept at each change; its `Override` holds a set for the
  screen gallery and the `--options` look, never saved. `Device` turns the set for the way the device is held into the
  canvas's units (`Device.Margins`, `Device.PerMillimetre`: on Android by the reported density, on a PC with `--phone`
  the reference phone's, on a PC in the touch mode the reference tablet's 167 mm short side), none on the PC with the
  mouse; a change lays every screen out again as a turn does (`Device.Turns`). The default clear colour is black.
- **Every screen inside them**: `GameScreen.UpdateDevice` insets its room (`_content`, and the menu bar on the tablet) by
  the margins and clips it then (our wave backdrop's rings reach past its rect), the black background round it; the
  views inside lay themselves out from their parent's size as before, and the phone's frame from `Device.Frame` keeps
  free what the margins have not. The import screen is laid out in `Device.RoomIn` (`ImportScreen.Layout`,
  `PlacePhone`).
- **Option → Display** (`OptionsView.MarginsField`), in the touch layout only: upright a section under the orientation
  with a row as its rows (the way it is held now, the other under it, Adjust…); held landscape a field at the left
  column's foot beside Defaults | Close ("Screen margins", both sets under it); on the tablet a section right of the
  palette (landscape's set alone). A tap anywhere on it opens C. **Defaults resets both sets to none** with the rest of
  the tab.
- **C** (`godot/Strategy/ScreenMarginsView`): over the whole screen, `GameScreen.OpenMargins` hides the options window
  meanwhile; the screen under it dimmed 55% and laid out live inside the margins; a press on − or + steps 0.1 mm, held it
  steps on (after 0.45 s, every 0.07 s); Reset sets the way held now to none; Done, Android's Back, Esc or a right click
  close it and show the options window again, laid out in the new room.
- **Looked at**: the screen gallery's `screen-margins` (C at 2.8 mm all round over the map's plates laid out inside
  them) and `map-margins` (the same without C), and `options-display`, in every pass (the PC's shows no margins);
  `--options` in the touch layout also dumps `options-margins` (C over the title) and `options-margins-display` (the
  window again inside them), the margins held at 2.8 mm for the look.

The agent's own choices, for the owner to confirm:

- The model of §2 as it stands (open question 2).
- Defaults on Display resets the margins too, both ways.
- Upright the side tabs stand a quarter of the way down, not in the middle as C drew them: in the middle a margin past
  ~6 mm puts them under the panel. Landscape both side tabs at a third, off the hole either side may hold; the landscape
  panel 600 reference pixels high, not C's 640, so the top and bottom tabs clear it at 8 mm.
- No line of explanation on C's panel, as drawn (B had one).
- A PC in the touch mode plays the reference tablet when it turns millimetres into the canvas's units, so the tablet
  pass shows C at the tablet's size.
