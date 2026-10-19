"""Mock-ups, round 5 of our Subordinates screen (owner, 2026-10-20): a Yes/No question before Add New Troops and the
order's report as a panel in the middle of the screen instead of the plate beside Talk (docs/ui/subordinates.md, §6).

Drawn over two window dumps of our screen from the kept save saves-week18 (`--interface new --shots`): `subordinates`
(Rance (unit 1) chosen, nothing ordered yet) and `subordinates-done` (Haurein (unit 12) replenished, the plate up):
  python -I tools/ui/mock_subordinates_orders.py <subordinates dump> <done dump> <out dir> < /dev/null
The dumps hold the player's CGs (the status CG, the faces): keep <out dir> outside the repository.
Everything is in the 1280x800 screen's pixels (plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

BEFORE, DONE, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H, CAPTION = 1280, 800, 64

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
SHOJUMARU = REPO + '/godot/fonts/Shojumaru-Regular.ttf'
_fonts = {}

# Lacquer (godot/Strategy/Lacquer.cs), as round 4 of tools/ui/mock_subordinates.py.
LQ_PANEL, LQ_GOLD, LQ_DIM = (22, 16, 12, 250), (196, 160, 86, 255), (110, 86, 46, 255)
LQ_NAME, LQ_SUB, LQ_LABEL = (232, 199, 122, 255), (160, 140, 104, 255), (150, 134, 108, 255)
LQ_VALUE, LQ_ROW = (236, 226, 204, 255), (34, 26, 20, 255)
LQ_HOVER, LQ_HOVER_LINE, LQ_TEXT = (64, 48, 28, 255), (222, 186, 104, 255), (244, 232, 206, 255)
LQ_QUIET, LQ_OFF_TEXT = (176, 160, 130, 255), (98, 88, 74, 255)
LQ_GREEN, LQ_RED, LQ_AMBER = (150, 196, 110, 255), (222, 110, 90, 255), (214, 170, 80, 255)
INK, INK_RED = (40, 28, 18, 255), (150, 52, 30, 255)


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


def game_area(path):
    """The game's 640x400 area of a window dump (under the 24-px menu bar, centred), at 1280x800 (x2)."""
    im = Image.open(path).convert('RGBA')
    k = (im.height - 24) / 400
    left = (im.width - 640 * k) / 2
    return im.crop((round(left), 24, round(left + 640 * k), im.height)).resize((W * S, H * S), Image.LANCZOS)


class Canvas:
    def __init__(self, base):
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (14, 10, 8, 255))
        self.im.paste(base, (0, 0))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill, outline=outline, width=width * S)

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

    def restore(self, base, r):
        """Paste a piece of the undimmed screen back over the dim (what the question is about stays lit)."""
        x0, y0, x1, y1 = r
        self.im.paste(base.crop((x0 * S, y0 * S, x1 * S, y1 * S)), (x0 * S, y0 * S))
        self.d = ImageDraw.Draw(self.im)

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=(168, 138, 74, 255))
        self.text((16, H + 10), caption, cinzel(20, 700), fill=(242, 199, 92, 255))
        self.text((16, H + 38), note, font(15), fill=(165, 167, 171, 255))
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def lq_panel(c, r, fill=LQ_PANEL, shadow=True):
    """Lacquer.DrawPanel: the shadow, the lacquer, the double gold frame and its corners."""
    x0, y0, x1, y1 = r
    if shadow:
        for i in range(12, 0, -1):
            c.overlay((x0 - i + 3, y0 - i + 8, x1 + i + 3, y1 + i + 8), (0, 0, 0, 16))
    c.overlay(r, fill)
    c.rect((x0 + 1, y0 + 1, x1 - 1, y1 - 1), outline=LQ_GOLD, width=2)
    c.rect((x0 + 5, y0 + 5, x1 - 5, y1 - 5), outline=LQ_DIM, width=1)
    for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        c.d.line([((cx + sx * 9) * S, (cy + sy * 2) * S), ((cx + sx * 9) * S, (cy + sy * 9) * S),
                  ((cx + sx * 2) * S, (cy + sy * 9) * S)], fill=LQ_GOLD, width=2 * S)
        c.poly([(cx + sx * 9, cy + sy * 14), (cx + sx * 14, cy + sy * 9), (cx + sx * 9, cy + sy * 4),
                (cx + sx * 4, cy + sy * 9)], fill=LQ_GOLD)


def lq_divider(c, x0, x1, y):
    m = (x0 + x1) / 2
    c.line((x0, y), (m - 10, y))
    c.line((m + 10, y), (x1, y))
    c.poly([(m, y - 4), (m + 4, y), (m, y + 4), (m - 4, y)], fill=LQ_GOLD)


def dotted(c, x0, x1, y):
    for x in range(x0, x1, 6):
        c.line((x, y), (x + 2, y), fill=(110, 86, 46, 200))


def red_button(c, r, label, hover=False, f=None):
    x0, y0, x1, y1 = r
    top, bottom = ((150, 40, 30), (96, 20, 14)) if hover else ((128, 32, 24), (80, 16, 12))
    for i in range(y1 - y0):
        t = i / max(1, y1 - y0 - 1)
        c.line((x0, y0 + i), (x1, y0 + i), fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,))
    c.rect(r, outline=LQ_GOLD, width=2)
    c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(150, 96, 52, 255), width=1)
    c.text(((x0 + x1) / 2, (y0 + y1) / 2), label, f or cinzel(17, 700), fill=LQ_NAME, anchor='mm')


def dark_button(c, r, label, hover=False, f=None):
    c.rect(r, fill=LQ_HOVER if hover else LQ_ROW, outline=LQ_HOVER_LINE if hover else LQ_DIM, width=2 if hover else 1)
    c.text(((r[0] + r[2]) / 2, (r[1] + r[3]) / 2), label, f or cinzel(17, 700),
           fill=LQ_NAME if hover else LQ_TEXT, anchor='mm')


def yes_no(c, cx, y, w=136, h=40, gap=16, hover=0):
    """Yes (red lacquer) and No side by side, centred on cx; returns Yes's centre for the pointer."""
    x = cx - w - gap / 2
    red_button(c, (round(x), y, round(x + w), y + h), 'Yes', hover=hover == 0)
    dark_button(c, (round(x + w + gap), y, round(x + 2 * w + gap), y + h), 'No', hover=hover == 1)
    return x + w * 0.55, y + h * 0.6


def click_triangle(c, x, y):
    c.poly([(x - 8, y - 6), (x + 8, y - 6), (x, y + 6)], fill=LQ_GOLD)


def pointer(c, x, y):
    pts = [(0, 0), (0, 22), (5, 17), (9, 26), (13, 24), (9, 15), (16, 15)]
    c.poly([(x + px + 1, y + py + 1) for px, py in pts], fill=(0, 0, 0, 120))
    c.poly([(x + px, y + py) for px, py in pts], fill=(255, 255, 255, 255))
    c.d.line([((x + px) * S, (y + py) * S) for px, py in pts + [pts[0]]], fill=(0, 0, 0, 255), width=S)


def face(c, base, box, x, y, size):
    """A unit's face cut from the Talk button of a dump, in a gold frame."""
    x0, y0, x1, y1 = box
    img = base.crop((x0 * S, y0 * S, x1 * S, y1 * S)).resize((size * S, size * S), Image.LANCZOS)
    c.paste(img, (x, y))
    c.rect((x - 1, y - 1, x + size + 1, y + size + 1), outline=LQ_GOLD, width=1)


def troop_bar(c, x, y, w, before, after, top_before, top_after):
    """The troops before and after the order on one bar: the old part in its colour, the gain bright green."""
    c.rect((x, y, x + w, y + 6), fill=(0, 0, 0, 140))
    scale = w / max(top_before, top_after)
    f = before / top_before
    colour = LQ_GREEN if f >= 0.75 else (LQ_AMBER if f >= 0.35 else LQ_RED)
    c.rect((x, y, x + round(before * scale), y + 6), fill=colour)
    c.rect((x + round(before * scale), y, x + round(after * scale), y + 6), fill=(186, 232, 132, 255))
    if top_after > top_before:
        c.line((x + round(top_before * scale), y - 3), (x + round(top_before * scale), y + 9), fill=LQ_TEXT)


def figures(c, x1, y, s, f, fill, anchor='rm'):
    """A figure right- or left-aligned at x1; its arrows in the gothic font (Cinzel has none). Returns its width."""
    parts = s.split('→')
    arrow = font(round(f.size / S * 0.9))
    w = sum(c.width(p, f) for p in parts) + (len(parts) - 1) * c.width('→', arrow)
    x = x1 - w if anchor == 'rm' else x1
    for i, p in enumerate(parts):
        if i:
            c.text((x, y), '→', arrow, fill=LQ_GOLD, anchor='lm')
            x += c.width('→', arrow)
        c.text((x, y), p, f, fill=fill, anchor='lm')
        x += c.width(p, f)
    return w


def kv(c, x0, x1, y, label, value, value_fill=LQ_VALUE, sub=None, unit='K GOLD', vf=None):
    """One row of the week report's account: the label in Cinzel, the value right with its unit."""
    c.text((x0, y), label, cinzel(15, 600), fill=LQ_TEXT, anchor='lm')
    if sub:
        c.text((x0 + c.width(label, cinzel(15, 600)) + 8, y + 1), sub, cinzel(11, 600), fill=LQ_SUB, anchor='lm')
    if unit:
        c.text((x1, y + 1), unit, cinzel(11, 600), fill=LQ_SUB, anchor='rm')
        x1 -= c.width(unit, cinzel(11, 600)) + 6
    figures(c, x1, y, value, vf or cinzel(18, 700), value_fill)


# ---------------------------------------------------------------- the two screens
before = game_area(BEFORE)
done = game_area(DONE)
# The plate beside Talk (round 4) goes: the panel's own lacquer over it.
_fill = done.getpixel((1000 * S, 700 * S))
ImageDraw.Draw(done).rectangle([928 * S, 592 * S, 1256 * S, 690 * S], fill=_fill)

TALK_FACE = (708, 604, 780, 676)          # the face on the Talk button (game pixels)
STEP_50 = (792, 512, 882, 570)            # Rance's +50 step
GOLD_BEFORE, PRICE_50 = 116400, 34200     # Rance (unit 1): +25 for 11400K, Add More once: +50 for 34200K


def dimmed(base, alpha=140):
    c = Canvas(base)
    c.overlay((0, 0, W, H), (0, 0, 0, alpha))
    return c


def add_question_lines():
    return ['Are you sure you want to add 50 new troops', "to Rance's unit for 34,200 K Gold?"]


# C1 — the question alone, in the middle
def c1():
    c = dimmed(before)
    c.restore(before, STEP_50)
    c.rect(STEP_50, outline=LQ_HOVER_LINE, width=2)
    r = (380, 292, 900, 516)
    lq_panel(c, r)
    c.text((640, 330), 'Add New Troops', brush(22), fill=LQ_NAME, anchor='mm')
    lq_divider(c, 410, 870, 354)
    for i, line in enumerate(add_question_lines()):
        c.text((640, 388 + i * 28), line, font(18), fill=LQ_TEXT, anchor='mm')
    yes = yes_no(c, 640, 450)
    pointer(c, *yes)
    c.save('C1-question.png', 'C1 — the question alone, in the middle',
           'The screen dimmed, the clicked step left lit; the men, the unit and the price; Yes in red lacquer, '
           'No or a right click closes it.')


# C2 — the question with the order's account (the week report's manner)
def account_panel(c, base, title, who, rows, r=(380, 168, 900, 632)):
    lq_panel(c, r)
    x0, y0, x1, y1 = r
    face(c, base, TALK_FACE, x0 + 30, y0 + 28, 72)
    c.text((x0 + 122, y0 + 54), title, brush(26), fill=LQ_NAME, anchor='lm')
    c.text((x0 + 124, y0 + 86), who, cinzel(15, 700), fill=LQ_SUB, anchor='lm')
    lq_divider(c, x0 + 30, x1 - 30, y0 + 122)
    y = y0 + 148
    for row in rows:
        kind = row[0]
        if kind == 'section':
            c.text((x0 + 30, y), row[1].upper(), cinzel(12, 700), fill=LQ_LABEL, anchor='lm')
            y += 30
        elif kind == 'bar':
            troop_bar(c, x0 + 52, y - 10, x1 - x0 - 104, *row[1:])
            y += 22
        elif kind == 'total':
            c.rect((x0 + 30, y - 22, x1 - 30, y + 26), outline=LQ_GOLD, width=1)
            kv(c, x0 + 46, x1 - 46, y + 2, row[1], row[2], LQ_NAME, vf=cinzel(24, 700))
            y += 60
        else:
            _, label, value, fill, sub, unit = row
            kv(c, x0 + 52, x1 - 52, y, label, value, fill, sub, unit)
            dotted(c, x0 + 52, x1 - 52, y + 20)
            y += 40
    return r


def c2():
    c = dimmed(before)
    c.restore(before, STEP_50)
    c.rect(STEP_50, outline=LQ_HOVER_LINE, width=2)
    r = account_panel(c, before, 'Add New Troops', "Rance · Leazas Knights", [
        ('section', 'Troops'),
        ('row', 'Troops', '1000/1000  →  1050/1050', LQ_VALUE, None, None),
        ('bar', 1000, 1050, 1000, 1050),
        ('row', 'Weekly recovery', '+1', LQ_GREEN, 'troops regained each week', None),
        ('section', 'Treasury'),
        ('row', 'Cost', '−34,200', LQ_RED, '+50 men', 'K GOLD'),
        ('total', 'Budget after', '82,200'),
    ], r=(380, 150, 900, 652))
    c.text((640, r[3] - 92), 'Are you sure?', font(18), fill=LQ_TEXT, anchor='mm')
    yes = yes_no(c, 640, r[3] - 72)
    pointer(c, *yes)
    c.save('C2-question-with-account.png', "C2 — the question with the order's account",
           "As the week report: the face, what the order does (the weekly recovery's +1 the original never shows), "
           'the budget after it; Yes / No.')


# C3 — a callout at the clicked step (the Treasure screen's W2)
def c3():
    c = Canvas(before)
    c.rect(STEP_50, fill=None, outline=(190, 60, 40, 255), width=2)
    width, lines = 470, add_question_lines()
    x0, y0 = 716, STEP_50[3] + 16
    h = 30 + len(lines) * 28 + 70
    lq_panel(c, (x0, y0, x0 + width, y0 + h), fill=(30, 22, 16, 252))
    tx = (STEP_50[0] + STEP_50[2]) / 2
    c.poly([(tx - 13, y0 + 2), (tx, y0 - 13), (tx + 13, y0 + 2)], fill=LQ_GOLD)
    c.poly([(tx - 10, y0 + 3), (tx, y0 - 8), (tx + 10, y0 + 3)], fill=(30, 22, 16, 255))
    for i, line in enumerate(lines):
        c.text((x0 + 26, y0 + 32 + i * 28), line, font(18), fill=LQ_TEXT, anchor='lm')
    yes = yes_no(c, x0 + 26 + 144, y0 + h - 58)
    pointer(c, *yes)
    c.save('C3-callout.png', "C3 — a callout at the clicked step (the Treasure screen's manner)",
           'No dimming: the step marked in vermilion, the question and Yes / No in a lacquer callout under it, its tail on '
           'the step. The pointer barely moves.')


# R1 — the report in the middle, the week report's manner (after a replenish)
def r1():
    c = dimmed(done, 130)
    r = account_panel(c, done, 'Troops Replenished', "Haurein · Leazas Soldiers", [
        ('section', 'Troops'),
        ('row', 'Troops', '209  →  1400', LQ_VALUE, None, None),
        ('bar', 209, 1400, 1400, 1400),
        ('row', 'Replenished', '+1191', LQ_GREEN, None, 'MEN'),
        ('section', 'Treasury'),
        ('row', 'Cost', '−2,300', LQ_RED, None, 'K GOLD'),
        ('total', 'Budget', '114,100'),
    ], r=(380, 176, 900, 600))
    click_triangle(c, r[2] - 30, r[3] - 26)
    c.save('R1-report.png', "R1 — the report in the middle, as the week report",
           "The face, the troops before and after on one bar, the cost, the budget left; a click anywhere closes it "
           '(the triangle).')


# R2 — a band across the middle
def r2():
    c = dimmed(done, 70)
    y0, y1 = 318, 478
    c.overlay((0, y0, W, y1), (18, 13, 10, 240))
    c.line((0, y0 + 2), (W, y0 + 2), fill=LQ_GOLD, width=2)
    c.line((0, y0 + 7), (W, y0 + 7), fill=LQ_DIM)
    c.line((0, y1 - 3), (W, y1 - 3), fill=LQ_GOLD, width=2)
    c.line((0, y1 - 8), (W, y1 - 8), fill=LQ_DIM)
    face(c, done, TALK_FACE, 300, y0 + 30, 100)
    c.text((430, y0 + 50), 'Troops Replenished', brush(30), fill=LQ_NAME, anchor='lm')
    c.text((432, y0 + 88), "Haurein · Leazas Soldiers", cinzel(15, 700), fill=LQ_SUB, anchor='lm')
    w = figures(c, 432, y0 + 122, '209  →  1400', cinzel(22, 700), LQ_VALUE, anchor='lm')
    c.text((432 + w + 14, y0 + 123), '+1191', cinzel(18, 700), fill=LQ_GREEN, anchor='lm')
    c.text((990, y0 + 88), 'Cost', cinzel(13, 700), fill=LQ_LABEL, anchor='rm')
    c.text((1000, y0 + 88), '−2,300 K', cinzel(18, 700), fill=LQ_RED, anchor='lm')
    c.text((990, y0 + 122), 'Budget', cinzel(13, 700), fill=LQ_LABEL, anchor='rm')
    c.text((1000, y0 + 122), '114,100 K', cinzel(18, 700), fill=LQ_NAME, anchor='lm')
    c.save('R2-band.png', 'R2 — a band across the middle',
           "A lacquer band over the whole width, as a battle's cut-in: the face, the troops, cost and budget. "
           'Gone by itself after about two seconds, or at a click.')


def after_add():
    """The `subordinates` dump as it is after Rance's +50: his troops, the budget, the troop orders greyed."""
    base = before.copy()
    d = ImageDraw.Draw(base)
    # The table and the budget as they are after the order: Rance's troops, the budget, the troop orders greyed.
    row = base.getpixel((445 * S, 98 * S))
    d.rectangle([336 * S, 94 * S, 422 * S, 114 * S], fill=row)
    d.text((338 * S, 104 * S), '1050/1050', font=font(15), fill=INK, anchor='lm')
    d.rectangle([338 * S, 113 * S, 418 * S, 116 * S], fill=(70, 110, 50, 255))
    d.rectangle([1120 * S, 380 * S, 1200 * S, 404 * S], fill=base.getpixel((1100 * S, 400 * S)))
    d.text((1196 * S, 392 * S), '82,200', font=cinzel(20, 700), fill=LQ_NAME, anchor='rm')
    return base


# R3 — R1 after Add New Troops
def r3():
    base = after_add()
    c = Canvas(base)
    c.overlay((704, 418, 1250, 576), (20, 14, 10, 170))
    c.overlay((0, 0, W, H), (0, 0, 0, 130))
    r = account_panel(c, base, 'New Troops', "Rance · Leazas Knights", [
        ('section', 'Troops'),
        ('row', 'Troops', '1000/1000  →  1050/1050', LQ_VALUE, None, None),
        ('bar', 1000, 1050, 1000, 1050),
        ('row', 'Weekly recovery', '+1', LQ_GREEN, None, None),
        ('section', 'Treasury'),
        ('row', 'Cost', '−34,200', LQ_RED, '+50 men', 'K GOLD'),
        ('total', 'Budget', '82,200'),
    ], r=(380, 176, 900, 600))
    click_triangle(c, r[2] - 30, r[3] - 26)
    c.save('R3-report-add.png', 'R3 — R1 after Add New Troops',
           'The same panel for new troops: the troops and their maximum raised (the bar\'s tick is the old maximum), the '
           'weekly recovery +1, the cost and the budget left.')


# ---------------------------------------------------------------- round 6: R2's look as a plate (owner, 2026-10-20)
# The owner chose C3 (asked before Replenish too) and R2's look, but as a plate in the middle, not over the width.

def plate(c, base, title, who, troops, gain, cost, budget, extra=None):
    """R2's contents in a lacquer plate in the middle: the face, the order, the troops; cost and budget on the right."""
    h = 178 if extra else 150
    x0, x1 = 250, 1030
    y0 = round(400 - h / 2) - 10
    lq_panel(c, (x0, y0, x1, y0 + h), fill=(22, 16, 12, 248))
    size = h - 52
    face(c, base, TALK_FACE, x0 + 26, y0 + 26, size)
    tx = x0 + 26 + size + 24
    c.text((tx, y0 + 42), title, brush(28), fill=LQ_NAME, anchor='lm')
    c.text((tx + 2, y0 + 76), who, cinzel(15, 700), fill=LQ_SUB, anchor='lm')
    w = figures(c, tx + 2, y0 + 112, troops, cinzel(22, 700), LQ_VALUE, anchor='lm')
    c.text((tx + 2 + w + 14, y0 + 113), gain, cinzel(18, 700), fill=LQ_GREEN, anchor='lm')
    if extra:
        c.text((tx + 2, y0 + 146), extra[0], cinzel(13, 700), fill=LQ_LABEL, anchor='lm')
        c.text((tx + 2 + c.width(extra[0], cinzel(13, 700)) + 10, y0 + 146), extra[1], cinzel(16, 700), fill=LQ_GREEN, anchor='lm')
    rx, my = x1 - 232, y0 + h / 2
    c.line((rx, my - 40), (rx, my + 40), fill=LQ_DIM)
    c.text((rx + 20, my - 18), 'Cost', cinzel(13, 700), fill=LQ_LABEL, anchor='lm')
    c.text((x1 - 28, my - 18), cost, cinzel(18, 700), fill=LQ_RED, anchor='rm')
    c.text((rx + 20, my + 18), 'Budget', cinzel(13, 700), fill=LQ_LABEL, anchor='lm')
    c.text((x1 - 28, my + 18), budget, cinzel(18, 700), fill=LQ_NAME, anchor='rm')


# R4 — the plate after a replenish
def r4():
    c = dimmed(done, 70)
    plate(c, done, 'Troops Replenished', 'Haurein · Leazas Soldiers', '209  →  1400', '+1191', '−2,300 K', '114,100 K')
    c.save('R4-plate.png', 'R4 — R2 as a plate in the middle (after Replenish)',
           "A lacquer plate in the middle, R2's contents: the face, the troops, cost and budget. Gone by itself after "
           'about two seconds, or at a click.')


# R5 — the plate after Add New Troops
def r5():
    base = after_add()
    c = Canvas(base)
    c.overlay((704, 418, 1250, 576), (20, 14, 10, 170))
    c.overlay((0, 0, W, H), (0, 0, 0, 70))
    plate(c, base, 'New Troops', 'Rance · Leazas Knights', '1000/1000  →  1050/1050', '+50', '−34,200 K', '82,200 K')
    c.save('R5-plate-add.png', 'R5 — the plate after Add New Troops',
           'The same plate: the troops and their maximum raised, cost and budget (no weekly recovery: owner, 2026-10-20).')


for f in (c1, c2, c3, r1, r2, r3, r4, r5):
    f()
print('done:', OUT)
