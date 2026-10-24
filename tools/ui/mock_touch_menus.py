"""Mock-ups of the command panel and the province menu for the touch mode (owner, 2026-10-24: on Android their rows are
too low for a finger, and the panel's two lists could stand side by side, left and right; docs/ui/touch.md, "The command
panel and the province menu"), drawn over window dumps of the week-18 save on the tablet's frame (1280x824, the menu bar
included).

The dumps (a window; the run moves the system pointer):
  <Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look-menus
      --slots-from user://saves-week18 --until deployment --interface new --touch --shots <dump dir>
(00007-touch-commands.png: the map wait with the Commands button; 00009-command-menu.png: the command panel today;
00018-province-menu.png: Oaks's menu today, whose picture the mock-ups reuse). Then, from the repository's root
(Pillow; godot/fonts):
  python -I tools/ui/mock_touch_menus.py <dump dir> <out dir> < /dev/null
Keep <out dir> outside the repository.

The panels are drawn here after godot/Strategy/Lacquer (the panel, the row box, the tag) in the dump's pixels: the
1280x800 screen starts at y 24, under the menu bar.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

DUMPS, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H, BAR = 1280, 824, 24
TABLET_MM = 0.203

# Lacquer's colours (godot/Strategy/Lacquer.cs).
PANEL = (22, 16, 12, 245)
GOLD, DIM_GOLD = (196, 160, 86), (110, 86, 46)
NAME, SUBTITLE, LABEL, VALUE = (232, 199, 122), (160, 140, 104), (150, 134, 108), (236, 226, 204)
ROW, TEXT, QUIET, OFF_TEXT, OFF_TAG = (34, 26, 20), (244, 232, 206), (176, 160, 130), (98, 88, 74), (52, 46, 40)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)

COMMAND_TAGS = {'Build': (46, 62, 104), 'Harem': (128, 44, 70), 'Items': (138, 104, 34), 'Trade': (36, 92, 96),
                'Units': (70, 96, 52)}
PROVINCE_TAGS = {'Levy': (150, 52, 36), 'Build': (46, 62, 104), 'Plot': (104, 70, 34), 'Visit': (70, 96, 52),
                 'Search': (36, 92, 96), 'Battle': (128, 30, 30), 'Ninja': (74, 50, 96)}

# The touch rows: 46 layout pixels = 9.3 mm on the tablet (a finger's 7-9 mm), 8 apart.
ROW_H, GAP = 46, 8


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), int(size * S))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)
CINZEL = lambda size, w=500: font('Cinzel-VariableFont_wght.ttf', size, w)


def mm(px):
    return f'{px * TABLET_MM:.1f} mm'


class Frame:
    def __init__(self, name):
        base = Image.open(os.path.join(DUMPS, name)).convert('RGB')
        self.img = base.resize((W * S, H * S), Image.LANCZOS).convert('RGBA')

    def draw(self):
        lay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay):
        self.img = Image.alpha_composite(self.img, lay)

    def paste(self, picture, box):
        x0, y0, x1, y1 = box
        self.img.paste(picture.resize(((x1 - x0) * S, (y1 - y0) * S), Image.LANCZOS).convert('RGBA'), (x0 * S, y0 * S))

    def save(self, name):
        self.img.resize((W, H), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def sc(*v):
    return [round(a * S) for a in v]


def text(f, x, y, t, fnt, colour, anchor='lm'):
    lay, d = f.draw()
    d.text((x * S, y * S), t, font=fnt, fill=colour, anchor=anchor)
    f.add(lay)


def box(f, r, fill=None, outline=None, width=1):
    lay, d = f.draw()
    d.rectangle(sc(*r), fill=fill, outline=outline, width=round(width * S))
    f.add(lay)


def panel(f, x, y, w, h):
    """Lacquer.DrawPanel: the shadow, the lacquer, the double gold frame and its corners."""
    lay, d = f.draw()
    for i in range(6, 0, -1):
        d.rectangle(sc(x - i + 2, y - i + 6, x + w + i + 2, y + h + i + 6), fill=(0, 0, 0, 15))
    d.rectangle(sc(x, y, x + w, y + h), fill=PANEL)
    d.rectangle(sc(x + 1, y + 1, x + w - 1, y + h - 1), outline=GOLD, width=2 * S)
    d.rectangle(sc(x + 5.5, y + 5.5, x + w - 5.5, y + h - 5.5), outline=DIM_GOLD, width=S)
    for cx, cy, sx, sy in ((x, y, 1, 1), (x + w, y, -1, 1), (x, y + h, 1, -1), (x + w, y + h, -1, -1)):
        d.line(sc(cx + sx * 9, cy + sy * 2, cx + sx * 9, cy + sy * 9, cx + sx * 2, cy + sy * 9), fill=GOLD, width=2 * S)
        d.polygon(sc(cx + sx * 9, cy + sy * 14, cx + sx * 14, cy + sy * 9, cx + sx * 9, cy + sy * 4, cx + sx * 4, cy + sy * 9),
                  fill=GOLD)
    f.add(lay)


def divider(f, x0, x1, y):
    lay, d = f.draw()
    m = (x0 + x1) / 2
    d.line(sc(x0, y, m - 10, y), fill=DIM_GOLD, width=S)
    d.line(sc(m + 10, y, x1, y), fill=DIM_GOLD, width=S)
    d.polygon(sc(m, y - 4, m + 4, y, m, y + 4, m - 4, y), fill=GOLD)
    f.add(lay)


def vertical_rule(f, x, y0, y1):
    lay, d = f.draw()
    m = (y0 + y1) / 2
    d.line(sc(x, y0, x, m - 10), fill=DIM_GOLD, width=S)
    d.line(sc(x, m + 10, x, y1), fill=DIM_GOLD, width=S)
    d.polygon(sc(x, m - 4, x + 4, m, x, m + 4, x - 4, m), fill=GOLD)
    f.add(lay)


def row(f, r, label, tag=None, colour=None, on=True, quiet=False):
    """A row box (Lacquer.DrawRowBox), its tag (Lacquer.DrawTag) and text, at the touch size."""
    x0, y0, x1, y1 = r
    box(f, r, fill=ROW, outline=DIM_GOLD)
    tx = x0 + 14
    if tag is not None:
        t = (x0 + 7, y0 + 8, x0 + 79, y1 - 8)
        box(f, t, fill=colour if on else OFF_TAG, outline=GOLD if on else None)
        text(f, (t[0] + t[2]) / 2, (t[1] + t[3]) / 2, tag, GOTHIC(14), TEXT if on else OFF_TEXT, 'mm')
        tx = x0 + 94
    text(f, tx, (y0 + y1) / 2, label, GOTHIC(19 if not quiet else 18),
         (QUIET if quiet else TEXT) if on else OFF_TEXT)
    if not on:
        text(f, x1 - 10, (y0 + y1) / 2, 'used this week', GOTHIC(12), OFF_TEXT, 'rm')


def turn_end(f, r):
    x0, y0, x1, y1 = r
    box(f, r, fill=(118, 30, 24), outline=GOLD, width=2)
    box(f, (x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(214, 150, 90))
    text(f, (x0 + x1) / 2, (y0 + y1) / 2 + 1, 'Turn END', BRUSH(24), (250, 226, 160), 'mm')


def return_button(f, r):
    x0, y0, x1, y1 = r
    box(f, r, fill=ROW, outline=DIM_GOLD)
    text(f, (x0 + x1) / 2, (y0 + y1) / 2, 'Return', GOTHIC(18), QUIET, 'mm')


def note(f, x, y, t, anchor='la', size=15):
    """A note on a dark plate, so it reads over the map."""
    lay, d = f.draw()
    fnt = GOTHIC(size)
    l, t0, r, b = d.textbbox((x * S, y * S), t, font=fnt, anchor=anchor)
    d.rectangle((l - 5 * S, t0 - 3 * S, r + 5 * S, b + 3 * S), fill=(0, 0, 0, 200))
    d.text((x * S, y * S), t, font=fnt, fill=NOTE, anchor=anchor)
    f.add(lay)


def measure(f, x, y0, y1, label, left=False):
    """A pink bracket at x from y0 to y1 with its height in mm, the label right of it (or left)."""
    lay, d = f.draw()
    d.line(sc(x, y0, x, y1), fill=NOTE, width=2 * S)
    d.line(sc(x - 4, y0, x + 4, y0), fill=NOTE, width=2 * S)
    d.line(sc(x - 4, y1, x + 4, y1), fill=NOTE, width=2 * S)
    f.add(lay)
    if left:
        note(f, x - 10, (y0 + y1) / 2, label, anchor='rm', size=14)
    else:
        note(f, x + 10, (y0 + y1) / 2, label, anchor='lm', size=14)


# ------------------------------------------------------------------------------------------------- the command panel
COMMANDS = [('Build', 'Construction'), ('Harem', 'Harem'), ('Items', 'Treasure'), ('Trade', 'Merchant'),
            ('Units', 'Subordinates')]
LINKS = ['→Leazas Castle', 'Load', 'Save', 'All Data', 'Think']


def command_panel(variant):
    """C1: the commands left, the other items right, Turn END across the foot; C2: Turn END at the right column's
    foot, under the other items."""
    f = Frame('00007-touch-commands.png')
    col, pad, mid = 300, 18, 24
    pitch = ROW_H + GAP
    left_h = len(COMMANDS) * pitch - GAP
    if variant == 1:
        end_h = 60
        w = pad + col + mid + col + pad
        h = pad + left_h + 18 + end_h + pad
    else:
        end_h = 64
        w = pad + col + mid + col + pad
        right_h = len(LINKS) * pitch - GAP + 14 + end_h
        h = pad + max(left_h, right_h) + pad
    x = round((W - w) / 2)
    y = BAR + round((800 - h) / 2)
    panel(f, x, y, w, h)
    lx, rx, top = x + pad, x + pad + col + mid, y + pad
    for i, (tag, label) in enumerate(COMMANDS):
        row(f, (lx, top + i * pitch, lx + col, top + i * pitch + ROW_H), label, tag, COMMAND_TAGS[tag],
            on=label != 'Merchant' or variant != 2)
    for i, label in enumerate(LINKS):
        row(f, (rx, top + i * pitch, rx + col, top + i * pitch + ROW_H), label, quiet=True)
    if variant == 1:
        vertical_rule(f, x + pad + col + mid / 2, top + 4, top + left_h - 4)
        divider(f, x + 16, x + w - 16, top + left_h + 9)
        end = (lx, top + left_h + 18, x + w - pad, top + left_h + 18 + end_h)
    else:
        vertical_rule(f, x + pad + col + mid / 2, top + 4, y + h - pad - 4)
        ey = top + len(LINKS) * pitch - GAP + 14
        end = (rx, ey, rx + col, ey + end_h)
    turn_end(f, end)
    measure(f, x - 14, top, top + ROW_H, mm(ROW_H), left=True)
    measure(f, x + w + 8, end[1], end[3], mm(end_h))
    if variant == 1:
        note(f, 16, 40, 'C1 — two lists side by side, Turn END across the foot (touch mode only)', size=17)
        note(f, 16, 64, f'rows {ROW_H} px = {mm(ROW_H)} (now 30 px = {mm(30)}); the panel {w}x{h} (now 320x430)')
        note(f, 16, 86, 'the other items as boxes too: no hover on a tablet to show they can be tapped')
        f.save('C1-command-panel-two-lists.png')
    else:
        note(f, 16, 40, 'C2 — Turn END under the other items, at the right thumb (touch mode only)', size=17)
        note(f, 16, 64, f'rows {ROW_H} px = {mm(ROW_H)}; the panel {w}x{h}; Merchant spent this week, as an example')
        f.save('C2-command-panel-end-right.png')


# ------------------------------------------------------------------------------------------------- the province menu
OAKS = dict(name='Oaks', army='Rebel Army', card=[('Economy', '??'), ('Defense', '??'), ('Battle Area', '??')],
            items=[('Battle', 'Attack'), ('Ninja', 'Ninja Maneuver')])
CASTLE = dict(name='Leazas Castle', army='Rance Army',
              card=[('Economy', '20 (2,000K GOLD)'), ('Defense', '10 (Walls +20%)'), ('Battle Area', '180 troops')],
              items=[('Levy', 'Temporary Levy'), ('Build', 'Bolster Defense'), ('Plot', 'Dig Giant Hole'),
                     ('Visit', 'Tulip Research'), ('Search', 'Ruins Search')])


def oaks_picture():
    # ProvinceMenuView's picture in today's dump: the panel 470x308 centred, the picture 225x150 at its (18, 18).
    return Image.open(os.path.join(DUMPS, '00018-province-menu.png')).convert('RGB').crop((423, 288, 648, 438))


def figures(f, tx, ty, card, step=36):
    for label, value in card:
        text(f, tx, ty, label, GOTHIC(14), LABEL, 'la')
        text(f, tx, ty + 16, value, GOTHIC(16), LABEL if value == '??' else VALUE, 'la')
        ty += step


def province_left(p, tag):
    """P1: the province on the left (its picture, name, army, figures), its commands on the right, Return under them."""
    f = Frame('00007-touch-commands.png')
    pad, pic_w, pic_h, mid, col = 18, 270, 180, 24, 300
    pitch = ROW_H + GAP
    n = len(p['items'])
    right_h = n * pitch + 6 + ROW_H
    left_h = pic_h + 14 + 30 + 22 + 10 + 3 * 36
    w = pad + pic_w + mid + col + pad
    h = pad + max(left_h, right_h) + pad
    x = round((W - w) / 2)
    y = BAR + round((800 - h) / 2)
    panel(f, x, y, w, h)
    pic = (x + pad, y + pad, x + pad + pic_w, y + pad + pic_h)
    f.paste(oaks_picture(), pic)
    box(f, (pic[0] - 1, pic[1] - 1, pic[2] + 1, pic[3] + 1), outline=DIM_GOLD)
    ty = pic[3] + 14
    text(f, pic[0], ty, p['name'], BRUSH(22), NAME, 'la')
    text(f, pic[0] + 1, ty + 32, p['army'], CINZEL(14), SUBTITLE, 'la')
    figures(f, pic[0], ty + 62, p['card'])
    rx, top = pic[2] + mid, y + pad
    vertical_rule(f, pic[2] + mid / 2, top + 4, y + h - pad - 4)
    for i, (t, label) in enumerate(p['items']):
        row(f, (rx, top + i * pitch, rx + col, top + i * pitch + ROW_H), label, t, PROVINCE_TAGS[t])
    ry = y + h - pad - ROW_H
    return_button(f, (rx + col - 150, ry, rx + col, ry + ROW_H))
    measure(f, x + w + 8, top, top + ROW_H, mm(ROW_H))
    note(f, 16, 40, f'P1 — the province left, its commands right, Return at their foot (touch mode only){tag}', size=17)
    note(f, 16, 64, f'rows {ROW_H} px = {mm(ROW_H)} (now 29 px = {mm(29)}); the panel {w}x{h} (now 470x308); '
                    'the picture 270x180 (now 225x150)')
    return f, w, h


def province_two_columns(p, tag):
    """P2: the header as now (picture, name, army, figures), the commands below in two columns, Return under the right."""
    f = Frame('00007-touch-commands.png')
    pad, pic_w, pic_h, mid, col = 18, 240, 160, 20, 260
    pitch = ROW_H + GAP
    n = len(p['items'])
    rows = (n + 1) // 2
    w = pad + col + mid + col + pad
    h = pad + pic_h + pad + rows * pitch + (ROW_H if n % 2 == 0 else 0) + (GAP if n % 2 == 0 else 0) + pad - GAP
    x = round((W - w) / 2)
    y = BAR + round((800 - h) / 2)
    panel(f, x, y, w, h)
    pic = (x + pad, y + pad, x + pad + pic_w, y + pad + pic_h)
    f.paste(oaks_picture(), pic)
    box(f, (pic[0] - 1, pic[1] - 1, pic[2] + 1, pic[3] + 1), outline=DIM_GOLD)
    tx = pic[2] + pad
    text(f, tx, y + 16, p['name'], BRUSH(22), NAME, 'la')
    text(f, tx + 1, y + 48, p['army'], CINZEL(14), SUBTITLE, 'la')
    figures(f, tx, y + 74, p['card'], step=34)
    top = pic[3] + pad
    lx, rx = x + pad, x + pad + col + mid
    # Down the left column, then down the right (the script's order read as two lists).
    for i, (t, label) in enumerate(p['items']):
        cx = lx if i < rows else rx
        cy = top + (i if i < rows else i - rows) * pitch
        row(f, (cx, cy, cx + col, cy + ROW_H), label, t, PROVINCE_TAGS[t])
    ry = y + h - pad - ROW_H
    return_button(f, (rx + col - 150, ry, rx + col, ry + ROW_H))
    measure(f, x - 14, top, top + ROW_H, mm(ROW_H), left=True)
    note(f, 16, 40, f'P2 — the header as now, the commands in two columns under it (touch mode only){tag}', size=17)
    note(f, 16, 64, f'rows {ROW_H} px = {mm(ROW_H)}; the panel {w}x{h} (now 470x308); the left column first, then the right')
    return f, w, h


command_panel(1)
command_panel(2)
province_left(OAKS, '')[0].save('P1-province-oaks.png')
f, _, _ = province_left(CASTLE, '')
note(f, 16, 86, 'five items, an own province (Leazas Castle\'s items and figures; Oaks\'s picture standing in)')
f.save('P1b-province-five-items.png')
province_two_columns(OAKS, '')[0].save('P2-province-oaks.png')
f, _, _ = province_two_columns(CASTLE, '')
note(f, 16, 86, 'five items, an own province (Leazas Castle\'s items and figures; Oaks\'s picture standing in)')
f.save('P2b-province-five-items.png')
