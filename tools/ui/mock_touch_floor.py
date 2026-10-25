"""Mock-ups of the touch mode's floor ladder within the right thumb's reach (owner, 2026-10-26: the ladder's ways at the
screen's lower right corner; docs/ui/dungeon-menu.md, "Touch: the ladder within the thumb's reach"), drawn over a window
dump of the ladder as it is built (right of the dungeon's picture, at the screen's middle height).

The dump: a dungeon look in the touch mode, in a window (CLAUDE.md, "A dungeon battle"):
  <Godot console exe> --path godot -- --load --auto --quit --seed 2 --saves user://saves-look-dungeon
      --slots-from user://saves-week18 --dungeon --until round-1 --touch --shots <dir>
its `floor-ladder` (the Monsters' Cave (dungeon 2), 1F; 1280x824 with the menu bar). Then, from the repository's root
(Pillow; godot/fonts; the parts are mock_touch_panel.py's):
  python -I tools/ui/mock_touch_floor.py <dump dir> <out dir> <floor-ladder dump's file name> < /dev/null
Keep <out dir> outside the repository.

On the real screen the dungeon's picture stands over black, the place's window 113 (the dungeon, the floor, the unit,
its troops) under it from x 248 to 1032; the corners right and left of the window are free. Three layouts, each on 7F
(both ways) and 1F (no Ascend: its place stays empty, as the owner's round 2): A the ladder as it is, moved down into
the corner, Retreat at its foot; B Retreat at the rail's top, the ways under it at the thumb; C the ways at the right
thumb, Retreat alone at the left thumb's corner.
"""
import sys, os
sys.dont_write_bytecode = True                  # no __pycache__ in the repository for the borrowed module
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw, ImageFont
from mock_touch_panel import (W, H, S, REPO, GOLD, DIM_GOLD, NAME, SUBTITLE, TEXT, QUIET, ROW, NOTE, GOTHIC, BRUSH,
                              CINZEL, Frame, sc, text, note, rect, shadow, panel, rule, reach, mm)

DUMPS, DUMP = sys.argv[1], sys.argv[3]
RETREAT, RETREAT_LINE, RETREAT_TEXT = (96, 30, 24), (214, 150, 90), (250, 226, 160)
BAR = 24                                        # the dump's menu bar


def screen():
    """The dump below the menu bar, with today's ladder taken away (black, as the screen round the picture)."""
    img = Image.open(os.path.join(DUMPS, DUMP)).convert('RGB').crop((0, BAR, W, BAR + H))
    ImageDraw.Draw(img).rectangle((936, 186, 1210, 533), fill=(0, 0, 0))
    ImageDraw.Draw(img).rectangle((1034, 533, 1210, 545), fill=(0, 0, 0))   # its foot, beside the window's corner
    return img


def floor_in_window(img, floor):
    """Rewrites the floor's line of window 113 ("1F" at its second line) in a narrowed BIZ UDGothic standing in for the
    game's font."""
    img = img.copy()
    fill = img.getpixel((700, 700 - BAR))
    ink = (226, 204, 150)
    ImageDraw.Draw(img).rectangle((300, 606, 420, 668), fill=fill)
    fnt = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf'), 50)
    probe = ImageDraw.Draw(Image.new('L', (1, 1)))
    l, t0, r, b = probe.textbbox((0, 0), floor, font=fnt)
    glyphs = Image.new('RGBA', (r - l + 4, b - t0 + 4), (0, 0, 0, 0))
    ImageDraw.Draw(glyphs).text((2 - l, 2 - t0), floor, font=fnt, fill=ink, stroke_width=1, stroke_fill=ink)
    glyphs = glyphs.resize((round(glyphs.width * 0.8), glyphs.height), Image.LANCZOS)
    img.paste(glyphs, (330, 614), glyphs)
    return img


def triangle(f, cx, cy, size, up, colour):
    lay, d = f.draw()
    h = size * 0.8
    if up:
        d.polygon(sc(cx - size / 2, cy + h / 2, cx + size / 2, cy + h / 2, cx, cy - h / 2), fill=colour)
    else:
        d.polygon(sc(cx - size / 2, cy - h / 2, cx + size / 2, cy - h / 2, cx, cy + h / 2), fill=colour)
    f.add(lay)


def way(f, r, up, label, to, deeper, arrow=30, size=24):
    """A way's button as FloorLadderView draws it: the arrow (gold the way deeper), the name, where it leads."""
    if to is None:
        return                                  # not offered: its place stays empty
    shadow(f, r, 3)
    rect(f, r, ROW, outline=DIM_GOLD)
    cy = (r[1] + r[3]) / 2
    triangle(f, r[0] + 36, cy, arrow, up, GOLD if deeper else QUIET)
    text(f, r[0] + 66, cy - 11, label, GOTHIC(size), TEXT, anchor='lm')
    text(f, r[0] + 67, cy + 17, f'{"deeper" if deeper else "back"} · to {to}F', GOTHIC(14), NAME if deeper else QUIET,
         anchor='lm')


def floor_plate(f, cx, y, floor):
    text(f, cx, y, 'FLOOR', CINZEL(12), SUBTITLE)
    text(f, cx, y + 30, f'{floor}F', BRUSH(34), NAME)


def retreat(f, r, size=21):
    shadow(f, r, 4)
    rect(f, r, RETREAT, outline=GOLD, w=2)
    rect(f, (r[0] + 4, r[1] + 4, r[2] - 4, r[3] - 4), outline=RETREAT_LINE)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2 + 1, 'Retreat', BRUSH(size), RETREAT_TEXT)


def finger(f, x, y):
    lay, d = f.draw()
    d.ellipse(sc(x - 15, y - 15, x + 15, y + 15), fill=NOTE + (90,), outline=NOTE + (230,), width=2 * S)
    f.add(lay)


def right_reach(f):
    reach(f)


def caption(f, lines):
    for i, t in enumerate(lines):
        note(f, 16, 14 + i * 23, t, size=17 if i == 0 else 14)


def floors(n):
    """The Monsters' Cave (dungeon 2, 14 floors): Ascend back, Descend deeper."""
    return (n - 1 if n > 1 else None), (n + 1 if n < 14 else None)


RAIL_X0, RAIL_X1 = 1040, 1276                   # right of window 113 (to x 1032), to the screen's edge
IN_X0, IN_X1 = RAIL_X0 + 12, RAIL_X1 - 12


def frame(n):
    return Frame(screen() if n == 1 else floor_in_window(screen(), f'{n}F'))


def layout_a(n):
    """A: the ladder as built (Ascend, the floor, Descend, Retreat), its foot at the screen's foot."""
    f = frame(n)
    up, down = floors(n)
    panel(f, (RAIL_X0, 424, RAIL_X1, 796))
    way(f, (IN_X0, 438, IN_X1, 516), True, 'Ascend', up, False)
    floor_plate(f, (RAIL_X0 + RAIL_X1) / 2, 536, n)
    way(f, (IN_X0, 594, IN_X1, 672), False, 'Descend', down, True)
    rule(f, IN_X0 + 8, IN_X1 - 8, 690)
    retreat(f, (IN_X0, 706, IN_X1, 784))
    if down:
        finger(f, 1244, 652)
    right_reach(f)
    caption(f, [f'A — the ladder as built, moved into the corner: the Monsters\' Cave, {n}F',
                f'the ways {IN_X1 - IN_X0}x78 = {mm(IN_X1 - IN_X0)} x {mm(78)}; Descend (deeper, the usual way) at the grip\'s '
                'height, where the thumb rests;',
                'Ascend (a floor back, with an encounter roll) the furthest; Retreat at the foot, where the map\'s Close and the',
                'province menu\'s Return stand; the PC\'s order kept, only placed lower; a way not offered leaves its place empty'])
    f.save(f'A-corner-{n}F.png')


def layout_b(n):
    """B: Retreat at the rail's top (as Turn END on the command panel's rail), the ways under it, Descend lowest."""
    f = frame(n)
    up, down = floors(n)
    panel(f, (RAIL_X0, 424, RAIL_X1, 796))
    retreat(f, (IN_X0, 438, IN_X1, 498), size=19)
    rule(f, IN_X0 + 8, IN_X1 - 8, 514)
    way(f, (IN_X0, 528, IN_X1, 606), True, 'Ascend', up, False)
    floor_plate(f, (RAIL_X0 + RAIL_X1) / 2, 624, n)
    way(f, (IN_X0, 680, IN_X1, 784), False, 'Descend', down, True, arrow=36, size=26)
    if down:
        finger(f, 1244, 750)
    right_reach(f)
    caption(f, [f'B — Retreat on the rail\'s top, the ways at the thumb: the Monsters\' Cave, {n}F',
                f'Descend {IN_X1 - IN_X0}x104 = {mm(IN_X1 - IN_X0)} x {mm(104)} in the corner, Ascend over the floor;',
                'Retreat furthest from the resting thumb, as Turn END on the command panel\'s rail (it only asks first);',
                'in a tower the way deeper is Ascend, in the rail\'s middle'])
    f.save(f'B-retreat-top-{n}F.png')


def layout_c(n):
    """C: the ways at the right thumb, Retreat alone at the left thumb's corner (left of window 113)."""
    f = frame(n)
    up, down = floors(n)
    panel(f, (RAIL_X0, 470, RAIL_X1, 796))
    way(f, (IN_X0, 484, IN_X1, 584), True, 'Ascend', up, False, arrow=34, size=25)
    floor_plate(f, (RAIL_X0 + RAIL_X1) / 2, 602, n)
    way(f, (IN_X0, 682, IN_X1, 782), False, 'Descend', down, True, arrow=34, size=25)
    panel(f, (4, 700, 240, 796))
    retreat(f, (16, 712, 228, 784))
    if down:
        finger(f, 1244, 750)
    right_reach(f)
    caption(f, [f'C — the ways at the right thumb, Retreat at the left: the Monsters\' Cave, {n}F',
                f'the ways {IN_X1 - IN_X0}x100 = {mm(IN_X1 - IN_X0)} x {mm(100)}, the floor between them;',
                'Retreat alone in the lower left corner, left of the place\'s window: leaving takes the other hand,',
                'so a thumb stepping down floor after floor never lands on it'])
    f.save(f'C-split-{n}F.png')


for n in (7, 1):
    layout_a(n)
    layout_b(n)
    layout_c(n)
