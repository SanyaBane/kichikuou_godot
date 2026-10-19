"""Mock-ups of Replenish on our deployment screen (owner, 2026-10-20; docs/ui/deployment-table.md, "Replenish on the
deployment screen"): the week's troop order of the Subordinates screen offered where the party is built, in an attack,
a dungeon search and a defence alike, while the week's troop order is not spent.

Drawn over a window dump of our deployment screen from the kept save saves-week18, the party built (Cordoba (unit 8)
and Barres (unit 6) in the back row, Rance (unit 1) and Haurein (unit 12) in the front row):
  <Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look
      --slots-from user://saves-week18 --until deployment --interface new --shots <shots dir>
the last `picker` dump of the run is the base. Then, from the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_deployment_refill.py <picker dump> <out dir> < /dev/null
The dump holds the player's CGs (the faces): keep <out dir> outside the repository.
Everything is in the dump's pixels (1280x800 with the window's menu bar, plus a caption band below); drawn at 2x.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

DUMP, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H, CAPTION = 1280, 800, 64

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
SHOJUMARU = REPO + '/godot/fonts/Shojumaru-Regular.ttf'
_fonts = {}

# Lacquer (godot/Strategy/Lacquer.cs), as tools/ui/mock_subordinates_orders.py.
LQ_PANEL, LQ_GOLD, LQ_DIM = (22, 16, 12, 250), (196, 160, 86, 255), (110, 86, 46, 255)
LQ_NAME, LQ_SUB, LQ_LABEL = (232, 199, 122, 255), (160, 140, 104, 255), (150, 134, 108, 255)
LQ_VALUE, LQ_ROW = (236, 226, 204, 255), (34, 26, 20, 255)
LQ_HOVER, LQ_HOVER_LINE, LQ_TEXT = (64, 48, 28, 255), (222, 186, 104, 255), (244, 232, 206, 255)
LQ_GREEN, LQ_RED = (150, 196, 110, 255), (222, 110, 90, 255)
INK, INK_RED, INK_QUIET, HEAD_INK = (40, 28, 18, 255), (150, 52, 30, 255), (150, 136, 112, 255), (128, 52, 30, 255)
VERMILION = (178, 56, 36, 255)


def font(size, path=GOTHIC, weight=None):
    key = (size, path, weight)
    if key not in _fonts:
        f = ImageFont.truetype(path, size * S)
        if weight is not None:
            f.set_variation_by_axes([weight])
        _fonts[key] = f
    return _fonts[key]


def cinzel(size, weight=600):
    return font(size, CINZEL, weight)


def brush(size):
    return font(size, SHOJUMARU)


class Canvas:
    def __init__(self, base):
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (14, 10, 8, 255))
        self.im.paste(base, (0, 0))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill, outline=outline, width=width * S)

    def rounded(self, r, radius, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rounded_rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], radius * S, fill=fill, outline=outline,
                                 width=width * S)

    def overlay(self, r, fill):
        layer = Image.new('RGBA', self.im.size, (0, 0, 0, 0))
        x0, y0, x1, y1 = r
        ImageDraw.Draw(layer).rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill)
        self.im = Image.alpha_composite(self.im, layer)
        self.d = ImageDraw.Draw(self.im)

    def text(self, xy, s, f, fill=LQ_TEXT, anchor='la'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=LQ_DIM, width=1):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=width * S)

    def poly(self, pts, fill):
        self.d.polygon([(x * S, y * S) for x, y in pts], fill=fill)

    def paste(self, img, xy):
        self.im.alpha_composite(img, (round(xy[0] * S), round(xy[1] * S)))
        self.d = ImageDraw.Draw(self.im)

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=(168, 138, 74, 255))
        self.text((16, H + 10), caption, cinzel(20, 700), fill=(242, 199, 92, 255))
        self.text((16, H + 38), note, font(15), fill=(165, 167, 171, 255))
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def lq_panel(c, r, fill=LQ_PANEL):
    """Lacquer.DrawPanel: the shadow, the lacquer, the double gold frame and its corners."""
    x0, y0, x1, y1 = r
    for i in range(12, 0, -1):
        c.overlay((x0 - i + 3, y0 - i + 8, x1 + i + 3, y1 + i + 8), (0, 0, 0, 16))
    c.overlay(r, fill)
    c.rect((x0 + 1, y0 + 1, x1 - 1, y1 - 1), outline=LQ_GOLD, width=2)
    c.rect((x0 + 5, y0 + 5, x1 - 5, y1 - 5), outline=LQ_DIM, width=1)


def red_button(c, r, label, hover=False, f=None):
    x0, y0, x1, y1 = r
    top, bottom = ((150, 40, 30), (96, 20, 14)) if hover else ((128, 32, 24), (80, 16, 12))
    for i in range(y1 - y0):
        t = i / max(1, y1 - y0 - 1)
        c.line((x0, y0 + i), (x1, y0 + i), fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,))
    c.rect(r, outline=LQ_GOLD, width=2)
    c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(150, 96, 52, 255), width=1)
    c.text(((x0 + x1) / 2, (y0 + y1) / 2), label, f or cinzel(16, 700), fill=LQ_NAME, anchor='mm')


def dark_button(c, r, label, hover=False, f=None):
    c.rect(r, fill=LQ_HOVER if hover else LQ_ROW, outline=LQ_HOVER_LINE if hover else LQ_DIM, width=2 if hover else 1)
    c.text(((r[0] + r[2]) / 2, (r[1] + r[3]) / 2), label, f or cinzel(16, 700),
           fill=LQ_NAME if hover else LQ_TEXT, anchor='mm')


def yes_no(c, cx, y, w=120, h=36, gap=14, hover=0):
    """Yes (red lacquer) and No side by side, centred on cx; returns Yes's centre for the pointer."""
    x = cx - w - gap / 2
    red_button(c, (round(x), y, round(x + w), y + h), 'Yes', hover=hover == 0)
    dark_button(c, (round(x + w + gap), y, round(x + 2 * w + gap), y + h), 'No', hover=hover == 1)
    return x + w * 0.55, y + h * 0.6


def pointer(c, x, y):
    pts = [(0, 0), (0, 22), (5, 17), (9, 26), (13, 24), (9, 15), (16, 15)]
    c.poly([(x + px + 1, y + py + 1) for px, py in pts], fill=(0, 0, 0, 120))
    c.poly([(x + px, y + py) for px, py in pts], fill=(255, 255, 255, 255))
    c.d.line([((x + px) * S, (y + py) * S) for px, py in pts + [pts[0]]], fill=(0, 0, 0, 255), width=S)


def callout(c, target, lines, side='below', width=400):
    """The Treasure screen's W2 / the Subordinates screen's C3: a lacquer callout below or left of `target`, its tail
    on it, Yes / No inside."""
    tx, ty = (target[0] + target[2]) / 2, (target[1] + target[3]) / 2
    h = 26 + len(lines) * 26 + 62
    if side == 'below':
        x0, y0 = round(min(max(tx - width / 2, 40), W - 40 - width)), target[3] + 14
    else:
        x0, y0 = target[0] - 14 - width, round(min(ty - h / 2, H - 16 - h))
    lq_panel(c, (x0, y0, x0 + width, y0 + h), fill=(30, 22, 16, 252))
    if side == 'below':
        c.poly([(tx - 12, y0 + 2), (tx, y0 - 12), (tx + 12, y0 + 2)], fill=LQ_GOLD)
        c.poly([(tx - 9, y0 + 3), (tx, y0 - 7), (tx + 9, y0 + 3)], fill=(30, 22, 16, 255))
    else:
        x1 = x0 + width
        c.poly([(x1 - 2, ty - 12), (x1 + 12, ty), (x1 - 2, ty + 12)], fill=LQ_GOLD)
        c.poly([(x1 - 3, ty - 9), (x1 + 7, ty), (x1 - 3, ty + 9)], fill=(30, 22, 16, 255))
    for i, line in enumerate(lines):
        c.text((x0 + 22, y0 + 30 + i * 26), line, font(16), fill=LQ_TEXT, anchor='lm')
    return yes_no(c, x0 + 22 + 127, y0 + h - 52)


# ---------------------------------------------------------------- the dump and its places (the dump's pixels)
dump = Image.open(DUMP).convert('RGBA')
assert dump.size == (W, H), dump.size
base = dump.resize((W * S, H * S), Image.LANCZOS)

ROW_TOP, ROW_H = 114, 23                   # the ledger's first row and its height
UNITS = [1, 5, 6, 7, 8, 9, 10, 11, 12]     # the rows by number, as the dump lists them
REFILL = {6: 1, 7: 3, 9: 11, 10: 12}       # Replenish's price (x100K): one per 50 men short, at least 1
BARRES = UNITS.index(6)
GAP_X, GAP_W = 468, 64                     # the new column: after Cost, the rest moved right
TABLE_R, TABLE_B = 1230, 340
BARRES_CARD = (702, 560, 946, 660)
BARRES_FACE = (709, 568, 795, 653)


def row_box(i):
    return (50, ROW_TOP + i * ROW_H, TABLE_R, ROW_TOP + (i + 1) * ROW_H)


def with_refill_column():
    """The ledger with a Refill column after Cost: the columns from Rows on moved right, Item cut by the scroll."""
    im = base.copy()
    moved = base.crop((GAP_X * S, 88 * S, (TABLE_R - GAP_W) * S, TABLE_B * S))
    im.paste(moved, ((GAP_X + GAP_W) * S, 88 * S))
    blank = base.crop((532 * S, 88 * S, 533 * S, TABLE_B * S)).resize((GAP_W * S, (TABLE_B - 88) * S))
    im.paste(blank, (GAP_X * S, 88 * S))
    d = ImageDraw.Draw(im)
    # the scroll bar the wider table needs (as the Subordinates screen's since its Cost column)
    d.rectangle([50 * S, 352 * S, TABLE_R * S, 359 * S], fill=(176, 160, 130, 255))
    d.rectangle([50 * S, 352 * S, 1188 * S, 359 * S], fill=(120, 104, 84, 255))
    return im


def chip(c, i, price, state='normal'):
    """A Refill cell as a button: the price in a vermilion-ruled chip; hovered filled vermilion; spent greyed."""
    y0 = ROW_TOP + i * ROW_H + 3
    r = (GAP_X + 6, y0, GAP_X + GAP_W - 4, y0 + ROW_H - 6)
    label = f'{price * 100}K'
    if state == 'hover':
        c.rounded(r, 3, fill=VERMILION, outline=(120, 30, 18, 255))
        c.text((r[2] - 5, (r[1] + r[3]) / 2 + 1), label, font(14), fill=(250, 236, 214, 255), anchor='rm')
    elif state == 'spent':
        c.text((r[2] - 5, (r[1] + r[3]) / 2 + 1), label, font(14), fill=INK_QUIET, anchor='rm')
    else:
        c.rounded(r, 3, outline=(178, 56, 36, 160))
        c.text((r[2] - 5, (r[1] + r[3]) / 2 + 1), label, font(14), fill=INK_RED, anchor='rm')
    return r


def refill_column(c, hover=None, spent=False):
    c.text((GAP_X + GAP_W - 4, 101), 'REFILL', cinzel(11, 700), fill=HEAD_INK, anchor='rm')
    boxes = {}
    for i, unit in enumerate(UNITS):
        if unit in REFILL:
            boxes[unit] = chip(c, i, REFILL[unit], 'spent' if spent else 'hover' if unit == hover else 'normal')
        else:
            c.text((GAP_X + GAP_W - 9, ROW_TOP + i * ROW_H + ROW_H / 2 + 1), '—', font(14), fill=INK, anchor='rm')
    return boxes


QUESTION = ["Replenish Barres's unit?", '+20 troops (980 → 1000) for 100 K Gold.']


# A1 — the Refill column, its cells buttons
def a1():
    c = Canvas(with_refill_column())
    c.overlay(row_box(BARRES), (178, 56, 36, 20))
    boxes = refill_column(c, hover=6)
    yes = callout(c, boxes[6], QUESTION, width=410)
    pointer(c, *yes)
    c.save('A1-refill-column.png', 'A1 — a Refill column in the ledger, its cells buttons',
           "After Cost, as the Subordinates screen's: a price is a button (a click asks at the cell, C3), \"—\" when full. "
           'The table no longer fits: it scrolls, Item cut.')


# A2 — the week's troop order spent
def a2():
    c = Canvas(with_refill_column())
    refill_column(c, spent=True)
    tip = (GAP_X - 70, 112, GAP_X + 190, 140)
    c.rect(tip, fill=(30, 22, 16, 245), outline=LQ_GOLD)
    c.text((tip[0] + 10, (tip[1] + tip[3]) / 2), 'Replenish: used this week', font(14), fill=LQ_TEXT, anchor='lm')
    pointer(c, GAP_X + 30, 98)
    c.save('A2-refill-column-spent.png', "A2 — A1 after the week's troop order",
           'Spent (here, in Subordinates or in an earlier deployment this week): the prices grey, no buttons; the '
           "header's tooltip says why.")


def ribbon(c, face, price, state='normal'):
    """A Refill ribbon across the bottom of a card's face: the price; hovered red lacquer; none when full or spent."""
    x0, y0, x1, y1 = face
    r = (x0, y1 - 20, x1, y1)
    if state == 'hover':
        c.overlay(r, (150, 40, 30, 240))
        c.rect(r, outline=LQ_GOLD)
    else:
        c.overlay(r, (22, 16, 12, 215))
        c.line((x0, r[1]), (x1, r[1]), fill=LQ_GOLD)
    c.text(((x0 + x1) / 2, r[1] + 10), f'＋ {price * 100}K', font(13), fill=LQ_NAME, anchor='mm')
    return r


# B1 — a ribbon on the member's face
def b1():
    c = Canvas(base)
    c.rect(BARRES_CARD, outline=LQ_HOVER_LINE, width=2)
    r = ribbon(c, BARRES_FACE, REFILL[6], 'hover')
    yes = callout(c, r, QUESTION, side='left', width=410)
    pointer(c, *yes)
    c.save('B1-card-ribbon.png', "B1 — a Refill ribbon on a member's face",
           "Only the party's members short of men (here Barres): the price across the face's foot, a click asks beside "
           'it (C3). The table stays as it is.')


# B2 — the week's troop order spent
def b2():
    c = Canvas(base)
    c.text((838, 404), 'Replenish used this week', cinzel(11, 700), fill=LQ_SUB, anchor='lm')
    c.save('B2-card-ribbon-spent.png', "B2 — B1 after the week's troop order",
           "No ribbon on any card; a quiet line by the panel's title says the week's troop order is spent.")


def plate(c, face_box, title, who, troops, gain, cost, budget):
    """The Subordinates screen's R4: a lacquer plate in the middle — the face, the order, the troops, cost and budget."""
    h, x0, x1 = 150, 250, 1030
    y0 = round(400 - h / 2) - 10
    lq_panel(c, (x0, y0, x1, y0 + h), fill=(22, 16, 12, 248))
    size = h - 52
    fx0, fy0, fx1, fy1 = face_box
    img = base.crop((fx0 * S, fy0 * S, fx1 * S, fy1 * S)).resize((size * S, size * S), Image.LANCZOS)
    c.paste(img, (x0 + 26, y0 + 26))
    c.rect((x0 + 25, y0 + 25, x0 + 27 + size, y0 + 27 + size), outline=LQ_GOLD)
    tx = x0 + 26 + size + 24
    c.text((tx, y0 + 42), title, brush(28), fill=LQ_NAME, anchor='lm')
    c.text((tx + 2, y0 + 76), who, cinzel(15, 700), fill=LQ_SUB, anchor='lm')
    x = tx + 2
    for i, part in enumerate(troops.split('→')):           # the arrow in the gothic font (Cinzel has none)
        if i:
            c.text((x, y0 + 112), '→', font(20), fill=LQ_GOLD, anchor='lm')
            x += c.width('→', font(20))
        c.text((x, y0 + 112), part, cinzel(22, 700), fill=LQ_VALUE, anchor='lm')
        x += c.width(part, cinzel(22, 700))
    c.text((x + 14, y0 + 113), gain, cinzel(18, 700), fill=LQ_GREEN, anchor='lm')
    rx, my = x1 - 232, y0 + h / 2
    c.line((rx, my - 40), (rx, my + 40))
    c.text((rx + 20, my - 18), 'Cost', cinzel(13, 700), fill=LQ_LABEL, anchor='lm')
    c.text((x1 - 28, my - 18), cost, cinzel(18, 700), fill=LQ_RED, anchor='rm')
    c.text((rx + 20, my + 18), 'Budget', cinzel(13, 700), fill=LQ_LABEL, anchor='lm')
    c.text((x1 - 28, my + 18), budget, cinzel(18, 700), fill=LQ_NAME, anchor='rm')


# R — the report (either variant)
def r():
    c = Canvas(base)
    c.overlay((0, 24, W, H), (0, 0, 0, 70))
    plate(c, BARRES_FACE, 'Troops Replenished', 'Barres · Leazas Knights', '980 → 1000', '+20', '−100 K', '114,000 K')
    c.save('R-report.png', "R — the report, the Subordinates screen's R4 (either variant)",
           'The plate in the middle, gone after about two seconds or at a click; then the screen with the troops, the '
           "card's cost and the war expenses summed again.")


for f in (a1, a2, b1, b2, r):
    f()
print('done:', OUT)
