"""Mock-ups of the province menu on the PC (owner, 2026-10-25: its rows and their text a little taller; other looks
welcome; docs/ui/command-screen.md, "The province menu"), drawn over a window dump of the week-18 save (1280x824, the
menu bar included).

The dump (a window; the run moves the system pointer):
  <Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look-province
      --slots-from user://saves-week18 --until deployment --interface new --shots <dump dir>
(00015-province-menu.png: Oaks's menu today, the map under it). Leazas Castle's picture is CG 1001 of the extract
(Prov.BgCg). Then, from the repository's root (Pillow; godot/fonts):
  python -I tools/ui/mock_province_menu.py <dump dir> <extract cg_png dir> <out dir> < /dev/null
Keep <out dir> outside the repository: the mock-ups hold the player's CGs.

The panels are drawn after godot/Strategy/UiPalette's lacquer (the panel, the row box, the tag) and
godot/Strategy/ProvinceMenuView (today's sizes, A0) in the dump's pixels: the 1280x800 screen starts at y 24.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

DUMPS, CGS, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H, BAR = 1280, 824, 24

# The lacquer palette (godot/Strategy/UiPalette.cs).
PANEL = (22, 16, 12, 255)            # opaque here (245 in the game), so the dump's own panel does not show through
GOLD, DIM_GOLD, HOVER_LINE = (196, 160, 86), (110, 86, 46), (222, 186, 104)
NAME, SUBTITLE, LABEL, VALUE = (232, 199, 122), (160, 140, 104), (150, 134, 108), (236, 226, 204)
ROW, ROW_HOVER = (34, 26, 20), (64, 48, 28)
TEXT, QUIET, OFF_TEXT, OFF_TAG = (244, 232, 206), (176, 160, 130), (98, 88, 74), (52, 46, 40)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)

TAGS = {'Levy': (150, 52, 36), 'Build': (46, 62, 104), 'Plot': (104, 70, 34), 'Visit': (70, 96, 52),
        'Search': (36, 92, 96), 'Battle': (128, 30, 30), 'Ninja': (74, 50, 96)}

CASTLE = dict(name='Leazas Castle', army='Leazas Army',
              card=[('Economy', '20 (2,000K GOLD)'), ('Defense', '10 (Walls +20%)'), ('Battle Area', '180 troops')],
              items=[('Levy', 'Temporary Levy', True), ('Build', 'Bolster Defense', True), ('Visit', 'Dungeon', True)])
# Five rows with one spent, to see the tallest panel; sample items, not one city's.
FIVE = dict(CASTLE, items=[('Levy', 'Temporary Levy', True), ('Build', 'Bolster Defense', False),
                           ('Plot', 'Dig Giant Hole', True), ('Visit', 'Tulip Research', True),
                           ('Search', 'Ruins Search', True)])


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), int(size * S))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
GOTHIC_BOLD = lambda size: font('BIZUDGothic-Bold.ttf', size) if os.path.exists(
    os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Bold.ttf')) else GOTHIC(size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)
CINZEL = lambda size, w=500: font('Cinzel-VariableFont_wght.ttf', size, w)


class Frame:
    def __init__(self, dump='00015-province-menu.png'):
        base = Image.open(os.path.join(DUMPS, dump)).convert('RGB')
        self.img = base.resize((W * S, H * S), Image.LANCZOS).convert('RGBA')

    def draw(self):
        lay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay):
        self.img = Image.alpha_composite(self.img, lay)

    def paste(self, picture, box):
        x0, y0, x1, y1 = box
        self.img.paste(picture.resize((round((x1 - x0) * S), round((y1 - y0) * S)), Image.LANCZOS).convert('RGBA'),
                       (round(x0 * S), round(y0 * S)))

    def save(self, name):
        self.img.resize((W, H), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def sc(*v):
    return [round(a * S) for a in v]


def text(f, x, y, t, fnt, colour, anchor='lm'):
    lay, d = f.draw()
    d.text((x * S, y * S), t, font=fnt, fill=colour, anchor=anchor)
    f.add(lay)


def width(t, fnt):
    return fnt.getlength(t) / S


def box(f, r, fill=None, outline=None, line=1):
    lay, d = f.draw()
    d.rectangle(sc(*r), fill=fill, outline=outline, width=round(line * S))
    f.add(lay)


def panel(f, x, y, w, h):
    """UiPalette.DrawPanel: the shadow, the lacquer, the double gold frame and its corners."""
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


def vertical_rule(f, x, y0, y1):
    lay, d = f.draw()
    m = (y0 + y1) / 2
    d.line(sc(x, y0, x, m - 10), fill=DIM_GOLD, width=S)
    d.line(sc(x, m + 10, x, y1), fill=DIM_GOLD, width=S)
    d.polygon(sc(x, m - 4, x + 4, m, x, m + 4, x - 4, m), fill=GOLD)
    f.add(lay)


def row_box(f, r, hover):
    """UiPalette.DrawRowBox."""
    box(f, r, fill=ROW_HOVER if hover else ROW, outline=HOVER_LINE if hover else DIM_GOLD, line=2 if hover else 1)


def tag_box(f, r, tag, on, size):
    """UiPalette.DrawTag."""
    box(f, r, fill=TAGS[tag] if on else OFF_TAG, outline=GOLD if on else None)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2, tag, GOTHIC(size), TEXT if on else OFF_TEXT, 'mm')


def spent(f, x1, cy, size):
    text(f, x1 - 10, cy, 'used this week', GOTHIC(size), OFF_TEXT, 'rm')


def return_button(f, r, size, hover=False):
    row_box(f, r, hover)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2, 'Return', GOTHIC(size), TEXT if hover else QUIET, 'mm')


def note(f, x, y, t, anchor='la', size=15):
    """A note on a dark plate, so it reads over the map."""
    lay, d = f.draw()
    fnt = GOTHIC(size)
    l, t0, r, b = d.textbbox((x * S, y * S), t, font=fnt, anchor=anchor)
    d.rectangle((l - 5 * S, t0 - 3 * S, r + 5 * S, b + 3 * S), fill=(0, 0, 0, 200))
    d.text((x * S, y * S), t, font=fnt, fill=NOTE, anchor=anchor)
    f.add(lay)


def measure(f, x, y0, y1, label):
    lay, d = f.draw()
    d.line(sc(x, y0, x, y1), fill=NOTE, width=2 * S)
    d.line(sc(x - 4, y0, x + 4, y0), fill=NOTE, width=2 * S)
    d.line(sc(x - 4, y1, x + 4, y1), fill=NOTE, width=2 * S)
    f.add(lay)
    note(f, x + 10, (y0 + y1) / 2, label, anchor='lm', size=14)


def picture():
    return Image.open(os.path.join(CGS, 'CG1001.png')).convert('RGB')


def name_fitting(t, size, room):
    while size > 11 and width(t, BRUSH(size)) > room:
        size -= 1
    return size


def figures(f, tx, ty, card, label, value, step):
    for l, v in card:
        text(f, tx, ty, l, GOTHIC(label), LABEL, 'la')
        text(f, tx, ty + label + 2, v, GOTHIC(value), LABEL if v == '??' else VALUE, 'la')
        ty += step


# ------------------------------------------------------------------------------------- A: today's layout, scaled rows
def layout_a(p, s, hover=0):
    """Today's panel (picture and figures over the rows) with the sizes in s; A0 is today's ProvinceMenuView."""
    f = Frame()
    pad, n = 18, len(p['items'])
    top = pad + s['pic'][1] + pad
    h = top + n * s['pitch'] + 4 + s['ret'][1] + pad - (s['pitch'] - s['row'])
    w = s['w']
    x = round((W - w) / 2)
    y = BAR + round((800 - h) / 2)
    panel(f, x, y, w, h)
    pic = (x + pad, y + pad, x + pad + s['pic'][0], y + pad + s['pic'][1])
    f.paste(picture(), pic)
    box(f, (pic[0] - 1, pic[1] - 1, pic[2] + 1, pic[3] + 1), outline=DIM_GOLD)
    tx = pic[2] + pad
    size = name_fitting(p['name'], s['name'], x + w - 16 - tx)
    text(f, tx, y + 16, p['name'], BRUSH(size), NAME, 'la')
    text(f, tx + 1, y + 16 + s['army_at'], p['army'], CINZEL(s['army']), SUBTITLE, 'la')
    figures(f, tx, y + 16 + s['figures_at'], p['card'], *s['figures'])
    for i, (tag, label, on) in enumerate(p['items']):
        r = (x + 16, y + top + i * s['pitch'], x + w - 16, y + top + i * s['pitch'] + s['row'])
        row_box(f, r, i == hover and on)
        ti, tw = s['tag']
        tag_box(f, (r[0] + ti, r[1] + ti, r[0] + ti + tw, r[3] - ti), tag, on, s['tag_text'])
        text(f, r[0] + ti + tw + 13, (r[1] + r[3]) / 2, label, GOTHIC(s['text']), TEXT if on else OFF_TEXT)
        if not on:
            spent(f, r[2], (r[1] + r[3]) / 2, s['spent'])
    rw, rh = s['ret']
    ry = y + top + n * s['pitch'] + 4 - (s['pitch'] - s['row'])
    ret = (x + w - 16 - rw, ry + 4, x + w - 16, ry + 4 + rh)
    return_button(f, ret, s['ret_text'])
    r0 = y + top
    measure(f, x + w + 10, r0, r0 + s['row'], f"{s['row']} px")
    return f, w, h


NOW = dict(w=470, pic=(225, 150), row=29, pitch=36, tag=(5, 62), tag_text=13, text=16, spent=12, ret=(102, 28),
           ret_text=15, name=19, army=13, army_at=30, figures_at=54, figures=(13, 15, 34))
A1 = dict(w=500, pic=(234, 156), row=38, pitch=45, tag=(6, 74), tag_text=15, text=19, spent=13, ret=(120, 36),
          ret_text=17, name=21, army=14, army_at=32, figures_at=58, figures=(14, 17, 36))
A2 = dict(w=540, pic=(252, 168), row=44, pitch=52, tag=(7, 84), tag_text=16, text=21, spent=14, ret=(132, 40),
          ret_text=18, name=23, army=15, army_at=34, figures_at=62, figures=(15, 18, 39))


def save_a(name, s, title, line):
    for p, suffix in ((CASTLE, ''), (FIVE, '-five')):
        f, w, h = layout_a(p, s)
        note(f, 16, 40, title, size=17)
        note(f, 16, 64, line.format(w=w, h=h))
        if p is FIVE:
            note(f, 16, 86, 'five rows, one spent this week (sample items, not one city\'s); the first row hovered')
        else:
            note(f, 16, 86, 'the first row hovered')
        f.save(f'{name}{suffix}.png')


# ------------------------------------------------------------------------------- B: the province left, commands right
def layout_b(p, hover=0):
    """The touch mode's P1 at the PC's sizes: the province in a left column, the commands in a right one."""
    f = Frame()
    pad, pic_w, pic_h, mid, col, row_h, pitch, ret_h = 18, 260, 173, 24, 320, 40, 48, 36
    n = len(p['items'])
    left_h = pic_h + 14 + 30 + 22 + 10 + 3 * 36
    right_h = n * pitch + 10 + ret_h
    w = pad + pic_w + mid + col + pad
    h = pad + max(left_h, right_h) + pad
    x = round((W - w) / 2)
    y = BAR + round((800 - h) / 2)
    panel(f, x, y, w, h)
    pic = (x + pad, y + pad, x + pad + pic_w, y + pad + pic_h)
    f.paste(picture(), pic)
    box(f, (pic[0] - 1, pic[1] - 1, pic[2] + 1, pic[3] + 1), outline=DIM_GOLD)
    ty = pic[3] + 14
    text(f, pic[0], ty, p['name'], BRUSH(name_fitting(p['name'], 22, pic_w)), NAME, 'la')
    text(f, pic[0] + 1, ty + 32, p['army'], CINZEL(14), SUBTITLE, 'la')
    figures(f, pic[0], ty + 62, p['card'], 14, 16, 36)
    rx, top = pic[2] + mid, y + pad
    vertical_rule(f, pic[2] + mid / 2, top + 4, y + h - pad - 4)
    for i, (tag, label, on) in enumerate(p['items']):
        r = (rx, top + i * pitch, rx + col, top + i * pitch + row_h)
        row_box(f, r, i == hover and on)
        tag_box(f, (r[0] + 6, r[1] + 7, r[0] + 76, r[3] - 7), tag, on, 15)
        text(f, r[0] + 90, (r[1] + r[3]) / 2, label, GOTHIC(19), TEXT if on else OFF_TEXT)
        if not on:
            spent(f, r[2], (r[1] + r[3]) / 2, 13)
    ry = y + h - pad - ret_h
    return_button(f, (rx + col - 130, ry, rx + col, ry + ret_h), 17)
    measure(f, x + w + 10, top, top + row_h, f'{row_h} px')
    return f, w, h


# ------------------------------------------------------------- C: the picture as a banner, the figures in a strip
def layout_c(p, hover=0):
    """The city's picture across the panel's top with its name on it, the figures as three cells under it, then the
    commands as tall rows: a colour bar and the text, the tag's word at the row's right in its colour."""
    f = Frame()
    pad, w, banner_h, strip_h, row_h, pitch, ret_h = 18, 520, 170, 54, 42, 50, 38
    n = len(p['items'])
    inner = w - 2 * pad
    top = pad + banner_h + 10 + strip_h + 16
    h = top + n * pitch + ret_h + pad
    x = round((W - w) / 2)
    y = BAR + round((800 - h) / 2)
    panel(f, x, y, w, h)
    # The banner: the picture scaled to the panel's width, its middle band, darkened at the foot for the name.
    pic = picture()
    scaled_h = round(pic.height * inner / pic.width)
    band = pic.resize((inner * 4, scaled_h * 4), Image.LANCZOS)
    off = round((scaled_h - banner_h) * 0.45) * 4
    band = band.crop((0, off, inner * 4, off + banner_h * 4))
    b = (x + pad, y + pad, x + pad + inner, y + pad + banner_h)
    f.paste(band, b)
    lay, d = f.draw()
    for i in range(110):
        a = round(240 * (i / 109) ** 1.2)
        yy = b[3] - 110 + i
        d.rectangle(sc(b[0], yy, b[2], yy + 1), fill=(14, 10, 8, a))
    f.add(lay)
    box(f, (b[0] - 1, b[1] - 1, b[2] + 1, b[3] + 1), outline=DIM_GOLD)
    for dx, dy in ((2, 2), (1, 1)):
        text(f, b[0] + 14 + dx, b[3] - 36 + dy, p['name'], BRUSH(26), (0, 0, 0, 200), 'ls')
    text(f, b[0] + 14, b[3] - 36, p['name'], BRUSH(26), NAME, 'ls')
    text(f, b[0] + 15, b[3] - 12, p['army'], CINZEL(15, 600), (200, 180, 140), 'ls')
    # The figures: three cells.
    sy = b[3] + 10
    cell = (inner - 2 * 8) / 3
    for i, (l, v) in enumerate(p['card']):
        cx = b[0] + i * (cell + 8)
        box(f, (cx, sy, cx + cell, sy + strip_h), fill=(30, 23, 18), outline=DIM_GOLD)
        text(f, cx + cell / 2, sy + 15, l, GOTHIC(13), LABEL, 'mm')
        text(f, cx + cell / 2, sy + 36, v, GOTHIC(16), LABEL if v == '??' else VALUE, 'mm')
    # The commands.
    for i, (tag, label, on) in enumerate(p['items']):
        r = (b[0], y + top + i * pitch, b[2], y + top + i * pitch + row_h)
        hov = i == hover and on
        row_box(f, r, hov)
        box(f, (r[0] + 2, r[1] + 2, r[0] + 10, r[3] - 2), fill=TAGS[tag] if on else OFF_TAG)
        text(f, r[0] + 26, (r[1] + r[3]) / 2, label, GOTHIC(20), TEXT if on else OFF_TEXT)
        if on:
            c = TAGS[tag]
            light = tuple(min(255, round(v * 1.6 + 40)) for v in c)
            text(f, r[2] - 14, (r[1] + r[3]) / 2 + 1, tag.upper(), CINZEL(13, 700), light, 'rm')
        else:
            spent(f, r[2], (r[1] + r[3]) / 2, 13)
    ry = y + h - pad - ret_h
    return_button(f, (b[2] - 140, ry, b[2], ry + ret_h), 18)
    measure(f, x + w + 10, y + top, y + top + row_h, f'{row_h} px')
    return f, w, h


# ---------------------------------------------------------------------------------- D: the commands as big tiles
def layout_d(p, hover=0):
    """Today's header, the commands as tiles side by side (Sengoku Rance's manner): the tag's colour as the tile's
    top band with its word, the command's text in large type under it; Return a row across the foot."""
    f = Frame()
    pad, w, pic_w, pic_h, tile_h, gap, ret_h = 18, 560, 234, 156, 86, 10, 36
    n = len(p['items'])
    per_row = 3 if n <= 3 else 3
    rows = (n + per_row - 1) // per_row
    inner = w - 2 * pad
    tile_w = (inner - (per_row - 1) * gap) / per_row
    top = pad + pic_h + pad
    h = top + rows * (tile_h + gap) + ret_h + pad
    x = round((W - w) / 2)
    y = BAR + round((800 - h) / 2)
    panel(f, x, y, w, h)
    pic = (x + pad, y + pad, x + pad + pic_w, y + pad + pic_h)
    f.paste(picture(), pic)
    box(f, (pic[0] - 1, pic[1] - 1, pic[2] + 1, pic[3] + 1), outline=DIM_GOLD)
    tx = pic[2] + pad
    text(f, tx, y + 16, p['name'], BRUSH(name_fitting(p['name'], 22, x + w - 16 - tx)), NAME, 'la')
    text(f, tx + 1, y + 50, p['army'], CINZEL(14), SUBTITLE, 'la')
    figures(f, tx, y + 72, p['card'], 14, 17, 36)
    for i, (tag, label, on) in enumerate(p['items']):
        cx = x + pad + (i % per_row) * (tile_w + gap)
        cy = y + top + (i // per_row) * (tile_h + gap)
        r = (cx, cy, cx + tile_w, cy + tile_h)
        row_box(f, r, i == hover and on)
        band = (r[0] + 3, r[1] + 3, r[2] - 3, r[1] + 27)
        box(f, band, fill=TAGS[tag] if on else OFF_TAG)
        text(f, (band[0] + band[2]) / 2, (band[1] + band[3]) / 2, tag, GOTHIC(15), TEXT if on else OFF_TEXT, 'mm')
        words = label.split(' ')
        lines = [label] if width(label, GOTHIC(19)) < tile_w - 16 else [' '.join(words[:-1]), words[-1]]
        mid = (band[3] + r[3]) / 2
        for k, line in enumerate(lines):
            dy = (k - (len(lines) - 1) / 2) * 23
            text(f, (r[0] + r[2]) / 2, mid + dy, line, GOTHIC(19), TEXT if on else OFF_TEXT, 'mm')
        if not on:
            text(f, (r[0] + r[2]) / 2, r[3] - 9, 'used this week', GOTHIC(12), OFF_TEXT, 'mm')
    ry = y + h - pad - ret_h
    return_button(f, (x + w - pad - 140, ry, x + w - pad, ry + ret_h), 17)
    measure(f, x + w + 10, y + top, y + top + tile_h, f'{tile_h} px')
    return f, w, h


save_a('A0-now', NOW, 'A0 — today: rows 29 px, text 16', 'the panel {w}x{h}; the tag 62x19, its word 13; Return 102x28')
save_a('A1-taller-rows', A1, 'A1 — today\'s layout, rows 38 px (+31%), text 19',
       'the panel {w}x{h} (now 470); the tag 74x26, its word 15; Return 120x36; the figures 14/17')
save_a('A2-tallest-rows', A2, 'A2 — today\'s layout, rows 44 px (+52%), text 21, the picture larger',
       'the panel {w}x{h}; the picture 252x168 (now 225x150); the tag 84x30, its word 16; Return 132x40')
for p, suffix in ((CASTLE, ''), (FIVE, '-five')):
    f, w, h = layout_b(p)
    note(f, 16, 40, 'B — the province left, its commands right (the touch mode\'s P1 at the PC\'s sizes)', size=17)
    note(f, 16, 64, f'rows 40 px, text 19; the panel {w}x{h}; the picture 260x173')
    f.save(f'B-two-columns{suffix}.png')
    f, w, h = layout_c(p)
    note(f, 16, 40, 'C — the picture as a banner with the name on it, the figures in three cells, tall rows', size=17)
    note(f, 16, 64, f'rows 42 px, text 20; the tag as a colour bar, its word at the right; the panel {w}x{h}')
    f.save(f'C-banner{suffix}.png')
    f, w, h = layout_d(p)
    note(f, 16, 40, 'D — today\'s header, the commands as tiles in a row (three per row)', size=17)
    note(f, 16, 64, f'tiles 86 px, text 19, the tag a band across the tile\'s top; the panel {w}x{h}')
    f.save(f'D-tiles{suffix}.png')


# ------------------------------------------------------- the command panel (owner, 2026-10-25: its rows as A1's)
COMMANDS = [('Build', 'Construction', True), ('Harem', 'Harem', True), ('Items', 'Treasure', True),
            ('Trade', 'Merchant', False), ('Units', 'Subordinates', True)]
COMMAND_TAGS = {'Build': (46, 62, 104), 'Harem': (128, 44, 70), 'Items': (138, 104, 34), 'Trade': (36, 92, 96),
                'Units': (70, 96, 52)}
LINKS = ['→Leazas Castle', 'Load', 'Save', 'All Data', 'Think']


def divider(f, x0, x1, y):
    lay, d = f.draw()
    m = (x0 + x1) / 2
    d.line(sc(x0, y, m - 10, y), fill=DIM_GOLD, width=S)
    d.line(sc(m + 10, y, x1, y), fill=DIM_GOLD, width=S)
    d.polygon(sc(m, y - 4, m + 4, y, m, y + 4, m - 4, y), fill=GOLD)
    f.add(lay)


def turn_end(f, r, size):
    box(f, r, fill=(118, 30, 24), outline=GOLD, line=2)
    box(f, (r[0] + 4, r[1] + 4, r[2] - 4, r[3] - 4), outline=(214, 150, 90))
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2 + 1, 'Turn END', BRUSH(size), (250, 226, 160), 'mm')


def command_panel(s):
    """godot/Strategy/CommandMenuView at the sizes in s: the commands as rows with tags, a divider, the other items as
    plain rows (a box when hovered), Turn END."""
    f = Frame('00008-command-menu.png')
    TAGS.update(COMMAND_TAGS)
    w, pad = s['w'], 18
    links_top = pad + len(COMMANDS) * s['pitch'] + 14
    end_top = links_top + len(LINKS) * s['link_pitch'] + 10
    h = end_top + s['end'] + 16
    x = round((W - w) / 2)
    y = BAR + round((800 - h) / 2)
    panel(f, x, y, w, h)
    for i, (tag, label, on) in enumerate(COMMANDS):
        r = (x + 14, y + pad + i * s['pitch'], x + w - 14, y + pad + i * s['pitch'] + s['row'])
        row_box(f, r, i == 0)
        ti, tw = s['tag']
        tag_box(f, (r[0] + ti, r[1] + ti, r[0] + ti + tw, r[3] - ti), tag, on, s['tag_text'])
        text(f, r[0] + ti + tw + 13, (r[1] + r[3]) / 2, label, GOTHIC(s['text']), TEXT if on else OFF_TEXT)
        if not on:
            spent(f, r[2], (r[1] + r[3]) / 2, s['spent'])
    divider(f, x + 16, x + w - 16, y + links_top - 7)
    for i, label in enumerate(LINKS):
        r = (x + 14, y + links_top + i * s['link_pitch'], x + w - 14, y + links_top + i * s['link_pitch'] + s['link'])
        hover = i == 1
        if hover:
            row_box(f, r, True)
        text(f, r[0] + 10, (r[1] + r[3]) / 2, label, GOTHIC(s['link_text']), TEXT if hover else QUIET)
    end = (x + 14, y + end_top, x + w - 14, y + end_top + s['end'])
    turn_end(f, end, s['end_text'])
    measure(f, x + w + 10, y + pad, y + pad + s['row'], f"{s['row']} px")
    r1 = y + links_top + s['link_pitch']
    measure(f, x + w + 10, r1, r1 + s['link'], f"{s['link']} px")
    return f, w, h


COMMAND_NOW = dict(w=320, row=30, pitch=37, tag=(5, 60), tag_text=13, text=16, spent=12, link=26, link_pitch=28,
                   link_text=15, end=48, end_text=21)
COMMAND_M1 = dict(w=360, row=38, pitch=45, tag=(6, 74), tag_text=15, text=19, spent=13, link=38, link_pitch=42,
                  link_text=18, end=56, end_text=24)
COMMAND_M2 = dict(w=360, row=38, pitch=45, tag=(6, 74), tag_text=15, text=19, spent=13, link=32, link_pitch=34,
                  link_text=17, end=52, end_text=23)
for name, s, title, line in (
        ('M0-command-now', COMMAND_NOW, 'M0 — the command panel today: commands 30 px, the other items 26 px', None),
        ('M1-command-rows-38', COMMAND_M1, 'M1 — every row 38 px as A1: commands text 19, the other items 18',
         'Turn END 56 px; the panel {w}x{h} (now 320x431)'),
        ('M2-command-rows-38-32', COMMAND_M2, 'M2 — commands 38 px as A1, the other items 32 px, text 17',
         'Turn END 52 px; the panel {w}x{h} (now 320x431)')):
    f, w, h = command_panel(s)
    note(f, 16, 84, title, size=17)
    note(f, 16, 108, (line or 'the panel {w}x{h}').format(w=w, h=h))
    note(f, 16, 130, 'the first command and Load hovered; Merchant spent this week, as an example')
    f.save(f'{name}.png')
