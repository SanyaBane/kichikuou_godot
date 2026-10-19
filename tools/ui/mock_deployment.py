"""Mock-ups of our deployment screen (Sﾌﾞﾀｲ) in the lacquer look of our Subordinates screen (docs/ui/subordinates.md,
§5, mock-up G3): the table as a paper ledger, the panels in lacquer and gold (godot/Strategy/Lacquer.cs), the party's
cards, the drop's highlight and the bar's buttons in variants (docs/ui/deployment-table.md, "The lacquer look").

The dumps come from the kept save saves-week18 in a window (our views need one; the run moves the system pointer):
  <Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look
      --slots-from user://saves-week18 --until deployment --interface new --shots <shots dir>
The picker dump with Cordoba (unit 8) hovered (his portrait and description at the top left) is the base; the units
are those of the save (a JSON of its page-1 records, as tools/ui/mock_subordinates.py reads), the figures those the
dump shows. Then, from the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_deployment.py <units.json> <cg_png dir> <picker dump> <out dir> [e] < /dev/null
Round d (no fifth argument): D1-D5 over the original layout; round e: E1-E6 in the Subordinates screen's layout.
The images hold the player's CGs: keep <out dir> outside the repository.
Everything is in the 1280x800 screen's pixels (plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os, json
from PIL import Image, ImageDraw, ImageFont

UNITS, CGS, DUMP, OUT = sys.argv[1:5]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H, CAPTION = 1280, 800, 64

# Lacquer.cs
PANEL, GOLD, DIM = (22, 16, 12, 245), (196, 160, 86, 255), (110, 86, 46, 255)
NAME, SUB, LABEL = (232, 199, 122, 255), (160, 140, 104, 255), (150, 134, 108, 255)
VALUE, ROW, HOVER = (236, 226, 204, 255), (34, 26, 20, 255), (64, 48, 28, 255)
HOVER_LINE, TEXT, QUIET = (222, 186, 104, 255), (244, 232, 206, 255), (176, 160, 130, 255)
OFF_TEXT, OFF, OFF_LINE = (98, 88, 74, 255), (40, 35, 30, 255), (66, 58, 48, 255)
GREEN, RED = (150, 196, 110, 255), (222, 110, 90, 255)
PAPER, PAPER_HEAD, PAPER_RULE = (224, 208, 172, 255), (196, 176, 136, 255), (150, 120, 80, 110)
INK, INK_QUIET, INK_GREY = (40, 28, 18, 255), (100, 80, 56, 255), (150, 132, 104, 255)
VERMILION_INK, HEAD_INK, VERMILION = (150, 52, 30, 255), (120, 40, 26, 255), (170, 40, 28, 255)
BAR_GOOD, BAR_LOW, BAR_BAD = (70, 110, 50, 255), (160, 110, 30, 255), (170, 50, 36, 255)

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
SHOJUMARU = REPO + '/godot/fonts/Shojumaru-Regular.ttf'
_fonts = {}


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


data = json.load(open(UNITS, encoding='utf-8'))
STR = data['strings']
UNITS_BY = {}
for u in data['units']:
    base = 20 + (u['Index'] - 1) * 3
    u['Name'], u['Type'] = STR[base], STR[base + 1]
    UNITS_BY[u['Index']] = u
# The figures the picker dump shows (after the week's replenish of Haurein): troops and war expense (x100K).
SHOWN = {1: (1000, 3), 5: (800, 12), 6: (980, 11), 7: (612, 10), 8: (1000, 11), 9: (901, 9), 10: (649, 8),
         11: (100, 1), 12: (1400, 10)}
for n, (troops, cost) in SHOWN.items():
    UNITS_BY[n].update(Troops=troops, Cost=cost, Status=1, HP=UNITS_BY[n]['MaxHP'])
GOLD_K = 1141
PARTY = [1, 12, 8, 6]          # places 1-4: Rance, Haurein (front row); Cordoba, Barres (back row)
HOVERED = 8                    # Cordoba: the dump's portrait and description

# UnitFaces: the owner's squares by face CG (x, y, size; 24, 7, 144 otherwise).
CROPS = {1: (33, 7, 128), 7: (28, 7, 136), 8: (31, 7, 128), 10: (32, 7, 128), 402: (15, 9, 136), 403: (44, 7, 136),
         404: (28, 7, 128), 406: (38, 7, 112), 430: (26, 45, 144)}


def k(v):
    return f'{v * 100:,}K'


class Canvas:
    def __init__(self, clear=False):
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (0, 0, 0, 0) if clear else (14, 10, 8, 255))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1, radius=0):
        x0, y0, x1, y1 = r
        box = [x0 * S, y0 * S, x1 * S - 1, y1 * S - 1]
        if radius:
            self.d.rounded_rectangle(box, radius * S, fill=fill, outline=outline, width=width * S)
        else:
            self.d.rectangle(box, fill=fill, outline=outline, width=width * S)

    def overlay(self, r, fill, radius=0):
        layer = Image.new('RGBA', self.im.size, (0, 0, 0, 0))
        dd = ImageDraw.Draw(layer)
        x0, y0, x1, y1 = r
        box = [x0 * S, y0 * S, x1 * S - 1, y1 * S - 1]
        if radius:
            dd.rounded_rectangle(box, radius * S, fill=fill)
        else:
            dd.rectangle(box, fill=fill)
        self.im = Image.alpha_composite(self.im, layer)
        self.d = ImageDraw.Draw(self.im)

    def text(self, xy, s, f, fill=TEXT, anchor='la'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=DIM, width=1):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=width * S)

    def paste(self, img, xy):
        self.im.alpha_composite(img, (round(xy[0] * S), round(xy[1] * S)))
        self.d = ImageDraw.Draw(self.im)

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=DIM)
        self.text((16, H + 10), caption, cinzel(20, 700), fill=NAME)
        self.text((16, H + 38), note, font(15), fill=QUIET)
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def game_area(path):
    """The game's 640x400 area of a window dump (under the 24-px menu bar, centred), at 1280x800."""
    im = Image.open(path).convert('RGBA')
    kk = (im.height - 24) / 400
    left = (im.width - 640 * kk) / 2
    return im.crop((round(left), 24, round(left + 640 * kk), im.height)).resize((W * S, H * S), Image.LANCZOS)


AREA = game_area(DUMP)


def cg(n):
    return Image.open(os.path.join(CGS, 'CG%04d.png' % n)).convert('RGBA')


def face_image(n, size, alpha=1.0, back=(52, 40, 28)):
    """A face's square (UnitFaces) as G n, 40 draws it: palette index 40 transparent, shown here on the card's backing
    colour (filled in before the shrink, so the key's magenta does not bleed into the edges)."""
    cx, cy, cs = CROPS.get(n, (24, 7, 144))
    raw = Image.open(os.path.join(CGS, 'CG%04d.png' % n))
    img = raw.convert('RGBA')
    key = Image.new('L', raw.size)
    key.putdata([255 if i == 40 else 0 for i in raw.getdata()])
    img.paste(Image.new('RGBA', raw.size, back + (255,)), (0, 0), key)
    img = img.crop((cx, cy, cx + cs, cy + cs)).resize((round(size * S), round(size * S)), Image.LANCZOS)
    if alpha < 1:
        img.putalpha(int(255 * alpha))
    return img


# ---------------------------------------------------------------- the lacquer parts

def shadow(c, r, strength=14):
    x0, y0, x1, y1 = r
    for i in range(10, 0, -1):
        c.overlay((x0 - i + 3, y0 - i + 8, x1 + i + 3, y1 + i + 8), (0, 0, 0, strength))


def lq_panel(c, r, fill=PANEL):
    """Lacquer.DrawPanel: the shadow, the lacquer, the double gold frame and its corners."""
    x0, y0, x1, y1 = r
    shadow(c, r)
    c.overlay(r, fill)
    c.rect((x0 + 1, y0 + 1, x1 - 1, y1 - 1), outline=GOLD, width=2)
    c.rect((x0 + 5, y0 + 5, x1 - 5, y1 - 5), outline=DIM, width=1)
    for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        c.d.line([((cx + sx * 9) * S, (cy + sy * 2) * S), ((cx + sx * 9) * S, (cy + sy * 9) * S),
                  ((cx + sx * 2) * S, (cy + sy * 9) * S)], fill=GOLD, width=2 * S)
        diamond(c, cx + sx * 9, cy + sy * 9, 5, GOLD)


def divider(c, x0, x1, y):
    m = (x0 + x1) / 2
    c.line((x0, y), (m - 10, y))
    c.line((m + 10, y), (x1, y))
    diamond(c, m, y, 4, GOLD)


def diamond(c, x, y, r, fill):
    c.d.polygon([(x * S, (y - r) * S), ((x + r) * S, y * S), (x * S, (y + r) * S), ((x - r) * S, y * S)], fill=fill)


def seigaiha(c, colour=(196, 160, 86, 13), radius=34):
    layer = Image.new('RGBA', c.im.size, (0, 0, 0, 0))
    dd = ImageDraw.Draw(layer)
    step = radius * S
    for row in range(-1, H * S // (step // 2) + 3):
        y = row * step // 2
        off = 0 if row % 2 == 0 else step
        for x in range(-2 * step + off, W * S + 2 * step, 2 * step):
            for kk in (1.0, 0.72, 0.44):
                rr = step * kk
                dd.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(14, 10, 8, 255) if kk == 1.0 else None,
                           outline=colour, width=S)
    c.im = Image.alpha_composite(c.im, layer)
    c.d = ImageDraw.Draw(c.im)


def vignette(c, strength=200):
    mask = Image.new('L', (W, H), 0)
    md = ImageDraw.Draw(mask)
    for i in range(60):
        md.rectangle([i * 6, i * 4, W - 1 - i * 6, H - 1 - i * 4], outline=int(strength * (1 - i / 60) ** 2), width=6)
    dark = Image.new('RGBA', (W, H + CAPTION), (0, 0, 0, 255))
    full = Image.new('L', (W, H + CAPTION), 0)
    full.paste(mask, (0, 0))
    dark.putalpha(full)
    c.im = Image.alpha_composite(c.im, dark.resize(c.im.size, Image.LANCZOS))
    c.d = ImageDraw.Draw(c.im)


def backdrop(c):
    seigaiha(c)
    vignette(c)


def portrait(c, r, at=None):
    """The portrait and its description as the script draws them (the dump's top left), at r or moved to at."""
    x0, y0, x1, y1 = r
    tx, ty = at or (x0, y0)
    shadow(c, (tx, ty, tx + x1 - x0, ty + y1 - y0), 16)
    c.paste(AREA.crop((x0 * S, y0 * S, x1 * S, y1 * S)), (tx, ty))


def row_box(c, r, hover=False):
    c.rect(r, fill=HOVER if hover else ROW, outline=HOVER_LINE if hover else DIM, width=2 if hover else 1)


def button(c, r, title, sub=None, kind='normal', tf=None, sf=None):
    """'primary' red lacquer (as Turn END), 'normal' a dark row box, 'hover' lit, 'off' greyed."""
    x0, y0, x1, y1 = r
    if kind.startswith('primary'):
        top, bottom = ((150, 40, 30), (96, 20, 14)) if kind == 'primary-hover' else ((128, 32, 24), (80, 16, 12))
        for i in range(y1 - y0):
            t = i / max(1, y1 - y0 - 1)
            c.line((x0, y0 + i), (x1, y0 + i), fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,))
        c.rect(r, outline=GOLD, width=2)
        c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(150, 96, 52, 255), width=1)
        tc, sc = NAME, (236, 206, 160, 255)
    elif kind == 'off':
        c.rect(r, fill=OFF, outline=OFF_LINE, width=1)
        tc, sc = OFF_TEXT, OFF_TEXT
    else:
        row_box(c, r, kind == 'hover')
        tc, sc = (NAME if kind == 'hover' else TEXT), QUIET
    cx = (x0 + x1) / 2
    tf, sf = tf or brush(22), sf or cinzel(12, 600)
    if sub:
        c.text((cx, y0 + (y1 - y0) * 0.42), title, tf, fill=tc, anchor='mm')
        c.text((cx, y0 + (y1 - y0) * 0.76), sub, sf, fill=sc, anchor='mm')
    else:
        c.text((cx, (y0 + y1) / 2), title, tf, fill=tc, anchor='mm')


def troop_bar(c, x, y, w, troops, most, ink=True, h=3):
    f = troops / most
    if ink:
        colour = BAR_GOOD if f >= 0.75 else (BAR_LOW if f >= 0.35 else BAR_BAD)
        track = (120, 100, 70, 90)
    else:
        colour = GREEN if f >= 0.75 else ((214, 170, 80, 255) if f >= 0.35 else RED)
        track = (0, 0, 0, 120)
    c.rect((x, y, x + w, y + h), fill=track)
    c.rect((x, y, x + max(1, round(w * f)), y + h), fill=colour)


def seal(c, x, y, n, size=20):
    """A place's number as a vermilion seal (hanko): a red square, the numeral in paper colour."""
    c.rect((x, y, x + size, y + size), fill=VERMILION, radius=3)
    c.rect((x + 2, y + 2, x + size - 2, y + size - 2), outline=(236, 196, 170, 140), radius=2)
    c.text((x + size / 2, y + size / 2 + 1), str(n), cinzel(round(size * 0.7), 700), fill=(246, 230, 206, 255), anchor='mm')


def badge(c, x, y, n, r=12):
    """A place's number on a gold diamond."""
    diamond(c, x, y, r, GOLD)
    diamond(c, x, y, r - 2, (60, 44, 24, 255))
    c.text((x, y + 1), str(n), cinzel(round(r * 1.1), 700), fill=NAME, anchor='mm')


# ---------------------------------------------------------------- the ledger

COLUMNS = [('', 30, None), ('No.', 40, 'n'), ('Name', 92, 'name'), ('Troops', 112, 'troops'), ('T.Atk', 58, 'TroopAtk'),
           ('T.Def', 58, 'TroopDef'), ('Str%', 54, 'str'), ('Cost', 70, 'cost'), ('HP', 52, 'hp'), ('Lv', 34, 'Level'),
           ('EXP', 80, 'exp'), ('Atk', 46, 'Atk'), ('Def', 46, 'Def'), ('Act', 46, 'Attacks'), ('Mag', 46, 'Magic')]
WIDE = [('', 26, None), ('No.', 40, 'n'), ('Name', 92, 'name'), ('Type', 146, 'type'), ('Troops', 112, 'troops'),
        ('Cost', 68, 'cost'), ('Rows', 96, 'rows'), ('T.Atk', 56, 'TroopAtk'), ('T.Def', 56, 'TroopDef'),
        ('Str%', 52, 'str'), ('HP', 50, 'hp'), ('Lv', 32, 'Level'), ('EXP', 76, 'exp'), ('Atk', 44, 'Atk'),
        ('Def', 44, 'Def'), ('Act', 44, 'Attacks'), ('Mag', 44, 'Magic'), ('Item', 120, 'item')]
ITEMS = {1: 'L. Holy Sword', 7: 'By Lord'}
ROWS_BY_CLASS = {0: 'Front', 4: 'Front', 10: 'Front', 1: 'Back', 2: 'Back', 11: 'Back', 3: 'Front, Back'}   # DeploymentTable.Details
ROWS = {u['Index']: ROWS_BY_CLASS.get(u['Class'], '') for u in data['units']}


def value(u, key):
    n = u['Index']
    return {
        'n': str(n), 'name': u['Name'], 'type': u['Type'], 'troops': f"{u['Troops']}/{u['MaxTroops']}",
        'str': f"{u['Strategy']}%", 'cost': f"{u['Cost'] * 100}K", 'hp': f"{u['HP']}/{u['MaxHP']}",
        'exp': f"{u['Exp']}/{100 + u['Level'] * 30}", 'rows': ROWS.get(n, 'Front'), 'item': ITEMS.get(n, '—'),
    }.get(key, str(u.get(key, '')))


def ledger(c, r, columns, size=16, row_h=27, hover=HOVERED, scroll=None, sort="No.", members=PARTY):
    """The table as a paper ledger: headers in Cinzel in vermilion ink over a rule, the rows' rules, the party's places
    as vermilion seals in the first column with the rows faintly gilt, the hovered row faintly vermilion."""
    x0, y0, x1, y1 = r
    t = Canvas(clear=True)
    t.rect(r, fill=PAPER, outline=DIM)
    head = row_h - 1
    t.rect((x0, y0, x1, y0 + head), fill=PAPER_HEAD)
    t.line((x0, y0 + head), (x1, y0 + head), fill=HEAD_INK)
    x = x0 + 4
    for name, w, key in columns:
        label = name
        if key in ('name', 'type', 'item', 'rows'):
            t.text((x + 4, y0 + head / 2), label, cinzel(13, 700), fill=HEAD_INK, anchor='lm')
        elif name:
            mark = 14 if name == sort else 0
            t.text((x + w - 6 - mark, y0 + head / 2), label, cinzel(13, 700), fill=HEAD_INK, anchor='rm')
            if mark:                                    # the sort's mark in the text's face (Cinzel has no arrows)
                t.text((x + w - 4, y0 + head / 2), '↑', font(12), fill=HEAD_INK, anchor='rm')
        x += w
    y = y0 + head + 2
    for u in data['units']:
        n = u['Index']
        place = members.index(n) + 1 if n in members else 0
        mid = y + row_h / 2
        if place:
            t.overlay((x0 + 1, y, x1 - 1, y + row_h), (200, 160, 80, 70))
        if n == hover:
            t.overlay((x0 + 1, y, x1 - 1, y + row_h), (170, 40, 28, 26))
        t.line((x0 + 6, y + row_h), (x1 - 6, y + row_h), fill=PAPER_RULE)
        x = x0 + 4
        for name, w, key in columns:
            if key is None:
                if place:
                    seal(t, x + (w - 18) / 2, mid - 9, place, 18)
            elif key == 'troops':
                t.text((x + 4, mid - 3), value(u, key), font(size), fill=INK, anchor='lm')
                troop_bar(t, x + 4, y + row_h - 6, w - 14, u['Troops'], u['MaxTroops'])
            elif key in ('name', 'type', 'item', 'rows'):
                t.text((x + 4, mid), value(u, key), font(size if key == 'name' else size - 2),
                       fill=INK if key == 'name' else INK_QUIET, anchor='lm')
            else:
                t.text((x + w - 6, mid), value(u, key), font(size), fill=INK, anchor='rm')
            x += w
        y += row_h
    if scroll:                                              # the columns run past the paper: a scroll bar of paper and ink
        sy = y1 - 10
        t.rect((x0 + 2, sy, x1 - 2, y1 - 2), fill=PAPER_HEAD)
        t.rect((x0 + 2, sy, x0 + 2 + (x1 - x0 - 4) * scroll, y1 - 2), fill=INK_QUIET)
    region = t.im.crop((x0 * S, y0 * S, x1 * S, y1 * S))
    c.paste(region, (x0, y0))


def title_band(c, r, title, subtitle, right=None):
    x0, y0, x1, _ = r
    c.text((x0 + 22, y0 + 25), title, brush(24), fill=NAME, anchor='lm')
    c.text((x0 + 22 + c.width(title, brush(24)) + 14, y0 + 27), subtitle, cinzel(13, 600), fill=SUB, anchor='lm')
    if right:
        c.text((x1 - 22, y0 + 27), right, cinzel(15, 700), fill=NAME, anchor='rm')
    divider(c, x0 + 16, x1 - 16, y0 + 44)


# ---------------------------------------------------------------- the panels

def details(c, r, u):
    """The hovered unit's details: the name in Cinzel, the values as label and figure, the troops with their bar."""
    x0, y0, x1, y1 = r
    lq_panel(c, r)
    c.text((x0 + 20, y0 + 24), u['Name'], brush(20), fill=NAME, anchor='lm')
    c.text((x0 + 22 + c.width(u['Name'], brush(20)) + 12, y0 + 26), u['Type'], cinzel(14, 700), fill=SUB, anchor='lm')
    c.line((x0 + 16, y0 + 44), (x1 - 16, y0 + 44))
    cols = [('Expense', f"{u['Cost'] * 100:,}K"), ('Troops', f"{u['Troops']}/{u['MaxTroops']}"),
            ('Rows', ROWS[u['Index']]), ('Item', ITEMS.get(u['Index'], '—'))]
    cw = (x1 - x0 - 40) / len(cols)
    for i, (label, v) in enumerate(cols):
        cx = x0 + 20 + i * cw
        c.text((cx, y0 + 58), label, cinzel(11, 700), fill=LABEL, anchor='lm')
        c.text((cx, y0 + 76), v, font(17), fill=VALUE, anchor='lm')
        if label == 'Troops':
            troop_bar(c, cx, y0 + 86, cw - 30, u['Troops'], u['MaxTroops'], ink=False)


def card(c, r, place, n, style, state='normal'):
    """A place's card: 'lacquer' (a dark row box, the face in gold, the place on a gold diamond) or 'paper' (a muster
    slip: paper and ink, the place a vermilion seal). state: normal, hover, dragged (dimmed), empty, drop-gold,
    drop-vermilion."""
    x0, y0, x1, y1 = r
    paper = style == 'paper'
    if n is None:
        dash = INK_GREY if paper else DIM
        if paper:
            c.overlay(r, (224, 208, 172, 70))
        else:
            c.overlay(r, (34, 26, 20, 150))
        for x in range(x0, x1, 14):
            c.line((x, y0), (min(x + 8, x1), y0), fill=dash)
            c.line((x, y1 - 1), (min(x + 8, x1), y1 - 1), fill=dash)
        for y in range(y0, y1, 14):
            c.line((x0, y), (x0, min(y + 8, y1)), fill=dash)
            c.line((x1 - 1, y), (x1 - 1, min(y + 8, y1)), fill=dash)
        c.text(((x0 + x1) / 2, (y0 + y1) / 2 - 8), str(place), cinzel(22, 700), fill=dash, anchor='mm')
        c.text(((x0 + x1) / 2, (y0 + y1) / 2 + 18), 'empty', cinzel(12, 600), fill=dash, anchor='mm')
        if state.startswith('drop'):
            drop_frame(c, r, state)
        return
    u = UNITS_BY[n]
    layer = Canvas(clear=True)                          # drawn apart, so a dragged card can be dimmed whole
    if paper:
        layer.rect(r, fill=PAPER, outline=DIM if state != 'hover' else VERMILION_INK, width=1 if state != 'hover' else 2)
    else:
        row_box(layer, r, state == 'hover')
    narrow = x1 - x0 < 250                             # a narrow card: a smaller face with the place's mark on it
    fs = min(y1 - y0 - 16, round((x1 - x0) * 0.4)) if narrow else y1 - y0 - 16
    if FACING == 'T3':
        facing_plate(layer, x0 + 8, y0 + 8, fs)
    layer.rect((x0 + 8, y0 + 8, x0 + 8 + fs, y0 + 8 + fs), fill=(200, 184, 148, 255) if paper else (52, 40, 28, 255))
    layer.paste(face_image(u['FaceCg'], fs, back=(200, 184, 148) if paper else (52, 40, 28)), (x0 + 8, y0 + 8))
    if FACING != 'T3':
        layer.rect((x0 + 8, y0 + 8, x0 + 8 + fs, y0 + 8 + fs), outline=INK_QUIET if paper else GOLD, width=1)
    tx = x0 + fs + (14 if narrow else 20)
    if FACING:
        tx += facing_mark(layer, x0 + 8, y0 + 8, fs, place)
    ink, quiet = (INK, INK_QUIET) if paper else (NAME, QUIET)
    mx, my = (x0 + 10, y0 + 10) if narrow else (tx, y0 + 9)
    if paper:
        seal(layer, mx, my, place, 20)
    else:
        badge(layer, mx + 10, my + 10, place, 11)
    layer.text((tx + (0 if narrow else 28), y0 + 19), u['Name'], font(18), fill=ink, anchor='lm')
    if x1 - x0 >= 210:
        layer.text((tx, y0 + 42), u['Type'], font(14 if not narrow else 13), fill=quiet, anchor='lm')
    line = y1 - 24
    troops = f"{u['Troops']} troops" if x1 - x0 > 280 else str(u['Troops'])
    layer.text((tx, line), troops, font(15), fill=INK if paper else TEXT, anchor='lm')
    cost = f"{u['Cost'] * 100:,}K"
    layer.text((x1 - 12, line), cost, font(15), fill=VERMILION_INK if paper else TEXT, anchor='rm')
    if not narrow:
        layer.text((x1 - 12 - layer.width(cost, font(15)) - 6, line + 1), 'Cost', cinzel(11, 700), fill=quiet, anchor='rm')
    troop_bar(layer, tx, line + 11, x1 - tx - 12, u['Troops'], u['MaxTroops'], ink=paper)
    # the remove button: a small × box at the top right
    bx = x1 - 26
    layer.rect((bx, y0 + 6, bx + 20, y0 + 26), fill=(0, 0, 0, 0) if paper else (40, 22, 16, 255),
               outline=VERMILION_INK if paper else (150, 70, 52, 255))
    layer.text((bx + 10, y0 + 16), '×', font(16), fill=VERMILION if paper else RED, anchor='mm')
    if state == 'dragged':
        a = layer.im.getchannel('A').point(lambda v: int(v * 0.35))
        layer.im.putalpha(a)
    c.im = Image.alpha_composite(c.im, layer.im)
    c.d = ImageDraw.Draw(c.im)
    if state.startswith('drop'):
        drop_frame(c, r, state)


def drop_frame(c, r, state):
    x0, y0, x1, y1 = r
    if state == 'drop-gold':                            # a gold glow and a bright double frame, a swap mark
        for i in range(8, 0, -1):
            c.overlay((x0 - i, y0 - i, x1 + i, y1 + i), (232, 199, 122, 9))
        c.overlay(r, (232, 199, 122, 30))
        c.rect(r, outline=HOVER_LINE, width=3)
        c.rect((x0 + 5, y0 + 5, x1 - 5, y1 - 5), outline=GOLD, width=1)
        cx, cy = (x0 + x1) / 2, y0
        c.rect((cx - 34, cy - 12, cx + 34, cy + 12), fill=(60, 44, 24, 255), outline=GOLD)
        c.text((cx, cy + 1), '⇄ Swap', font(14), fill=NAME, anchor='mm')
    else:                                               # vermilion: the card tinted, a seal-red frame, a tag
        c.overlay(r, (170, 40, 28, 46))
        c.rect(r, outline=VERMILION, width=3)
        c.rect((x0 - 4, y0 - 4, x1 + 4, y1 + 4), outline=(170, 40, 28, 120), width=1)
        cx, cy = (x0 + x1) / 2, y0
        c.rect((cx - 36, cy - 12, cx + 36, cy + 12), fill=VERMILION, radius=3)
        c.text((cx, cy + 1), '⇄ Swap', font(14), fill=(246, 230, 206, 255), anchor='mm')


def ghost(c, x, y, n, style):
    """The dragged member's face under the pointer, framed, with the pointer's arrow."""
    size = 64
    x0, y0 = x - size / 2, y - size / 2
    shadow(c, (x0, y0, x0 + size, y0 + size), 20)
    c.paste(face_image(UNITS_BY[n]['FaceCg'], size, 0.9), (x0, y0))
    c.rect((x0, y0, x0 + size, y0 + size), outline=VERMILION if style == 'paper' else HOVER_LINE, width=2)
    p = [(x, y), (x, y + 22), (x + 6, y + 17), (x + 10, y + 26), (x + 14, y + 24), (x + 10, y + 15), (x + 17, y + 15)]
    c.d.polygon([(a * S, b * S) for a, b in p], fill=(255, 255, 255, 255), outline=(0, 0, 0, 255))


def front_label(c, x, y):
    """"Front row ▶": the arrow towards the enemy in the text's face (Cinzel has none)."""
    w = c.width('Front row', cinzel(13, 700))
    c.text((x - 8, y), 'Front row', cinzel(13, 700), fill=LABEL, anchor='mm')
    c.text((x - 8 + w / 2 + 6, y), '▶', font(11), fill=LABEL, anchor='lm')


def party(c, r, style, members=PARTY, states=None, header=True):
    """The party panel, variant B: the back row (places 3-4) on the left, the front row (1-2) on the right; the totals
    against the budget at its foot."""
    x0, y0, x1, y1 = r
    lq_panel(c, r)
    states = states or {}
    top = y0 + 14
    if header:
        c.text((x0 + 20, y0 + 24), 'Party', brush(20), fill=NAME, anchor='lm')
        top = y0 + 44
    mid = (x0 + x1) / 2
    c.text(((x0 + 14 + mid - 3) / 2, top + 10), 'Back row', cinzel(13, 700), fill=LABEL, anchor='mm')
    front_label(c, (mid + 3 + x1 - 14) / 2, top + 10)
    foot = 40
    gy0, gy1 = top + 22, y1 - foot - 6
    ch = (gy1 - gy0 - 8) / 2
    places = {3: (x0 + 14, gy0), 4: (x0 + 14, gy0 + ch + 8), 1: (mid + 3, gy0), 2: (mid + 3, gy0 + ch + 8)}
    for place, (cx, cy) in places.items():
        n = members[place - 1] if place <= len(members) else None
        rect = (round(cx), round(cy), round(cx + (mid - 3 - x0 - 14)), round(cy + ch))
        card(c, rect, place, n, style, states.get(place, 'empty' if n is None else 'normal'))
    troops = sum(UNITS_BY[n]['Troops'] for n in members)
    cost = sum(UNITS_BY[n]['Cost'] for n in members)
    fy = y1 - foot / 2 - 4
    c.line((x0 + 16, y1 - foot - 2), (x1 - 16, y1 - foot - 2))
    c.text((x0 + 20, fy), 'Troops', cinzel(12, 700), fill=LABEL, anchor='lm')
    c.text((x0 + 20 + c.width('Troops', cinzel(12, 700)) + 8, fy), f'{troops:,}', font(17), fill=VALUE, anchor='lm')
    c.text((x0 + 200, fy), 'War expenses', cinzel(12, 700), fill=LABEL, anchor='lm')
    c.text((x0 + 200 + c.width('War expenses', cinzel(12, 700)) + 8, fy), k(cost), font(17),
           fill=VALUE if cost <= GOLD_K else RED, anchor='lm')
    budget = f'{GOLD_K * 100:,}'
    c.text((x1 - 20, fy), 'K GOLD', cinzel(11, 600), fill=SUB, anchor='rm')
    bw = c.width('K GOLD', cinzel(11, 600)) + 6
    c.text((x1 - 20 - bw, fy), budget, cinzel(18, 700), fill=NAME, anchor='rm')
    c.text((x1 - 20 - bw - c.width(budget, cinzel(18, 700)) - 8, fy + 1), 'of', cinzel(12, 600), fill=LABEL, anchor='rm')


def bar(c, r, kind, cost=35):
    """The bar's buttons (labels from the player's files: Attack / Plains, City / Search; Stop)."""
    x0, y0, x1, y1 = r
    if kind == 'three':                                 # three equal places, as now: "---" a greyed plate
        lq_panel(c, r)
        w = (x1 - x0 - 28 - 16) / 3
        xs = [x0 + 14 + i * (w + 8) for i in range(3)]
        button(c, (round(xs[0]), y0 + 14, round(xs[0] + w), y1 - 14), 'Attack', 'Go to war', 'primary', tf=brush(26))
        button(c, (round(xs[1]), y0 + 14, round(xs[1] + w), y1 - 14), '—', None, 'off', tf=cinzel(18, 700))
        button(c, (round(xs[2]), y0 + 14, round(xs[2] + w), y1 - 14), 'Stop', 'Back to the map', 'normal', tf=brush(22))
    else:                                               # 'wide': Attack wide, Stop small; no plate where the bar has none
        lq_panel(c, r)
        sw = 150
        button(c, (x0 + 14, y0 + 14, x1 - 14 - sw - 10, y1 - 14), 'Attack', f'Go to war  ·  {k(cost)}', 'primary-hover',
               tf=brush(28), sf=font(14))
        button(c, (x1 - 14 - sw, y0 + 14, x1 - 14, y1 - 14), 'Stop', None, 'normal', tf=cinzel(18, 700))


PORTRAIT = (0, 0, 640, 404)
TABLE = (652, 8, 1272, 644)
BAR = (652, 652, 1272, 792)
DETAILS = (8, 412, 632, 504)
PARTY_R = (8, 512, 632, 792)


def screen_a(card_style, bar_kind, states=None, members=PARTY, drag=None):
    c = Canvas()
    backdrop(c)
    portrait(c, PORTRAIT)
    lq_panel(c, TABLE)
    title_band(c, TABLE, 'Deployment', 'Attack · up to 4 units', 'LP 3 · Month 5 · Week 2')
    ledger(c, (TABLE[0] + 14, TABLE[1] + 52, TABLE[2] - 14, TABLE[3] - 14), COLUMNS, scroll=0.62, members=members)
    details(c, DETAILS, UNITS_BY[HOVERED])
    party(c, PARTY_R, card_style, members, states, header=False)
    bar(c, BAR, bar_kind, sum(UNITS_BY[n]['Cost'] for n in members))
    if drag:
        ghost(c, *drag)
    return c


def screen_b():
    """The Subordinates screen's layout: the ledger across the top with every column, the portrait at the bottom left,
    the party, its totals and the bar at the bottom right; no details panel (the ledger holds them)."""
    c = Canvas()
    backdrop(c)
    t = (16, 16, 1264, 360)
    lq_panel(c, t)
    title_band(c, t, 'Deployment', 'Attack · up to 4 units', 'LP 3 · Month 5 · Week 2')
    ledger(c, (t[0] + 14, t[1] + 50, t[2] - 14, t[3] - 12), WIDE, size=15, row_h=28)
    # The portrait panel as the Subordinates screen cuts it: script pixels (6, 6), 314x196, scaled to 333x208.
    shadow(c, (16, 368, 682, 784), 16)
    pic = AREA.crop((12 * S, 12 * S, 640 * S, 404 * S)).resize((666 * S, 416 * S), Image.LANCZOS)
    c.paste(pic, (16, 368))
    p = (690, 368, 1264, 784)
    lq_panel(c, p)
    x0, y0, x1, y1 = p
    c.text((x0 + 20, y0 + 24), 'Party', brush(20), fill=NAME, anchor='lm')
    budget = f'{GOLD_K * 100:,}'
    c.text((x1 - 20, y0 + 25), 'K GOLD', cinzel(11, 600), fill=SUB, anchor='rm')
    bw = c.width('K GOLD', cinzel(11, 600)) + 6
    c.text((x1 - 20 - bw, y0 + 24), budget, cinzel(20, 700), fill=NAME, anchor='rm')
    c.text((x1 - 20 - bw - c.width(budget, cinzel(20, 700)) - 10, y0 + 25), 'Budget', cinzel(12, 600), fill=LABEL, anchor='rm')
    divider(c, x0 + 14, x1 - 14, y0 + 44)
    mid = (x0 + x1) / 2
    c.text(((x0 + 14 + mid - 3) / 2, y0 + 60), 'Back row', cinzel(13, 700), fill=LABEL, anchor='mm')
    front_label(c, (mid + 3 + x1 - 14) / 2, y0 + 60)
    ch = 104
    for place, (cx, cy) in {3: (x0 + 14, y0 + 72), 4: (x0 + 14, y0 + 72 + ch + 8), 1: (mid + 3, y0 + 72),
                            2: (mid + 3, y0 + 72 + ch + 8)}.items():
        card(c, (round(cx), cy, round(cx + mid - 3 - x0 - 14), cy + ch), place, PARTY[place - 1], 'lacquer',
             'hover' if PARTY[place - 1] == HOVERED else 'normal')
    fy = y0 + 72 + 2 * ch + 8 + 22
    troops = sum(UNITS_BY[n]['Troops'] for n in PARTY)
    cost = sum(UNITS_BY[n]['Cost'] for n in PARTY)
    c.text((x0 + 20, fy), 'Troops', cinzel(12, 700), fill=LABEL, anchor='lm')
    c.text((x0 + 86, fy), f'{troops:,}', font(17), fill=VALUE, anchor='lm')
    c.text((x0 + 200, fy), 'War expenses', cinzel(12, 700), fill=LABEL, anchor='lm')
    c.text((x0 + 312, fy), k(cost), font(17), fill=VALUE, anchor='lm')
    c.line((x0 + 16, fy + 18), (x1 - 16, fy + 18))
    sw = 140
    button(c, (x0 + 14, y1 - 74, x1 - 14 - sw - 10, y1 - 16), 'Attack', 'Go to war  ·  3,500K', 'primary', tf=brush(21),
           sf=font(14))
    button(c, (x1 - 14 - sw, y1 - 74, x1 - 14, y1 - 16), 'Stop', None, 'normal', tf=cinzel(18, 700))
    return c



# ---------------------------------------------------------------- round E: the Subordinates screen's layout (2026-10-19)
# The owner looked at D1-D5 and took D5's layout: the ledger across the top, the portrait below left. Round E varies the
# party panel at the bottom right — the cards' shape, the buttons' place — in an attack and in a defence, whose bar has
# two buttons that send the party (Plains, City; Sﾌﾞﾀｲ.ADV:45-63) where an attack has one (Attack, ---; :14-33).
#   python -I tools/ui/mock_deployment.py <units.json> <cg_png> <picker dump> <out> e < /dev/null

TOP = (16, 16, 1264, 360)
PICTURE = (16, 368, 682, 784)
P = (690, 368, 1264, 784)
SUBTITLE = {'attack': 'Attack · up to 4 units', 'defence': 'Defence · up to 4 units', 'search': 'Dungeon search · up to 4 units'}
SENDS = {'attack': [('Attack', 'Go to war')], 'defence': [('Plains', 'Fight on the plains'), ('City', 'Fight in the city')],
         'search': [('Search', 'Into the dungeon')]}


def e_base(mode, members, hover=HOVERED):
    c = Canvas()
    backdrop(c)
    lq_panel(c, TOP)
    title_band(c, TOP, 'Deployment', SUBTITLE[mode], 'LP 3 · Month 5 · Week 2')
    ledger(c, (TOP[0] + 14, TOP[1] + 50, TOP[2] - 14, TOP[3] - 12), WIDE, size=15, row_h=28, members=members, hover=hover)
    shadow(c, PICTURE, 16)
    c.paste(AREA.crop((12 * S, 12 * S, 640 * S, 404 * S)).resize((666 * S, 416 * S), Image.LANCZOS), PICTURE[:2])
    lq_panel(c, P)
    return c


def e_header(c, title='Party'):
    x0, y0, x1, _ = P
    c.text((x0 + 20, y0 + 24), title, brush(20), fill=NAME, anchor='lm')
    budget = f'{GOLD_K * 100:,}'
    c.text((x1 - 20, y0 + 25), 'K GOLD', cinzel(11, 600), fill=SUB, anchor='rm')
    bw = c.width('K GOLD', cinzel(11, 600)) + 6
    c.text((x1 - 20 - bw, y0 + 24), budget, cinzel(20, 700), fill=NAME, anchor='rm')
    c.text((x1 - 20 - bw - c.width(budget, cinzel(20, 700)) - 10, y0 + 25), 'Budget', cinzel(12, 600), fill=LABEL, anchor='rm')
    divider(c, x0 + 14, x1 - 14, y0 + 44)


def e_totals(c, y, members, x0=None, x1=None):
    x0 = x0 or P[0] + 20
    troops = sum(UNITS_BY[n]['Troops'] for n in members)
    cost = sum(UNITS_BY[n]['Cost'] for n in members)
    c.text((x0, y), 'Troops', cinzel(12, 700), fill=LABEL, anchor='lm')
    c.text((x0 + 62, y), f'{troops:,}', font(17), fill=VALUE, anchor='lm')
    c.text((x0 + 170, y), 'War expenses', cinzel(12, 700), fill=LABEL, anchor='lm')
    c.text((x0 + 280, y), k(cost), font(17), fill=VALUE if cost <= GOLD_K else RED, anchor='lm')


def e_buttons(c, r, mode, members, hover=None, stop_w=130):
    """The bar in a row: the buttons that send the party in red lacquer (one in an attack or a search, Plains and City
    side by side in a defence), Stop small at the right."""
    x0, y0, x1, y1 = r
    cost = k(sum(UNITS_BY[n]['Cost'] for n in members))
    sends = SENDS[mode]
    gap = 8
    w = (x1 - x0 - stop_w - gap - gap * (len(sends) - 1)) / len(sends)
    for i, (title, sub) in enumerate(sends):
        bx = x0 + i * (w + gap)
        kind = 'primary-hover' if hover == title else 'primary'
        button(c, (round(bx), y0, round(bx + w), y1), title, f'{sub}  ·  {cost}' if len(sends) == 1 else sub, kind,
               tf=brush(21 if len(sends) == 1 else 19), sf=font(13) if len(sends) == 1 else cinzel(11, 600))
    button(c, (x1 - stop_w, y0, x1, y1), 'Stop', None, 'normal', tf=cinzel(18, 700))


def e_column_buttons(c, r, mode, hover=None):
    """The bar as a column at the panel's right: the sending buttons stacked, Stop at the foot."""
    x0, y0, x1, y1 = r
    sends = SENDS[mode]
    h = 62
    for i, (title, sub) in enumerate(sends):
        by = y0 + i * (h + 8)
        button(c, (x0, by, x1, by + h), title, sub.replace('Fight ', '').replace('Go to ', ''),
               'primary-hover' if hover == title else 'primary', tf=brush(19), sf=cinzel(10, 600))
    button(c, (x0, y1 - 46, x1, y1), 'Stop', None, 'normal', tf=cinzel(16, 700))


def rows_labels(c, xl, xr, y, mid):
    c.text(((xl + mid - 3) / 2, y), 'Back row', cinzel(13, 700), fill=LABEL, anchor='mm')
    front_label(c, (mid + 3 + xr) / 2, y)


def e_grid(c, members, style, states, x0, x1, y0, ch, gap=8):
    """2x2: the back row (3-4) left, the front row (1-2) right."""
    mid = (x0 + x1) / 2
    rows_labels(c, x0, x1, y0, mid)
    top = y0 + 12
    for place, (cx, cy) in {3: (x0, top), 4: (x0, top + ch + gap), 1: (mid + 3, top), 2: (mid + 3, top + ch + gap)}.items():
        n = members[place - 1] if place <= len(members) else None
        st = states.get(place, 'empty' if n is None else ('hover' if n == HOVERED else 'normal'))
        if st == 'locked':
            locked(c, (round(cx), cy, round(cx + mid - 3 - x0), cy + ch))
        else:
            card(c, (round(cx), cy, round(cx + mid - 3 - x0), cy + ch), place, n, style, st)
    return top + 2 * ch + gap


def locked(c, r):
    """A place the screen does not offer (Tmp0040 below 4): a dark slot with a dim lock line."""
    x0, y0, x1, y1 = r
    c.overlay(r, (10, 8, 6, 160))
    c.rect(r, outline=(52, 44, 34, 255))
    c.text(((x0 + x1) / 2, (y0 + y1) / 2), 'not offered', cinzel(11, 600), fill=OFF_TEXT, anchor='mm')


def card_tall(c, r, place, n, style, state='normal'):
    """A standing card: the face on top, the place's mark on it, the name, the type, the troops and their bar, the cost."""
    x0, y0, x1, y1 = r
    paper = style == 'paper'
    if n is None:
        card(c, r, place, None, style, state)
        return
    u = UNITS_BY[n]
    layer = Canvas(clear=True)
    if paper:
        layer.rect(r, fill=PAPER, outline=DIM if state != 'hover' else VERMILION_INK, width=1 if state != 'hover' else 2)
    else:
        row_box(layer, r, state == 'hover')
    fs = x1 - x0 - 12
    layer.rect((x0 + 6, y0 + 6, x0 + 6 + fs, y0 + 6 + fs), fill=(200, 184, 148, 255) if paper else (52, 40, 28, 255))
    layer.paste(face_image(u['FaceCg'], fs, back=(200, 184, 148) if paper else (52, 40, 28)), (x0 + 6, y0 + 6))
    layer.rect((x0 + 6, y0 + 6, x0 + 6 + fs, y0 + 6 + fs), outline=INK_QUIET if paper else GOLD, width=1)
    if paper:
        seal(layer, x0 + 10, y0 + 10, place, 20)
    else:
        badge(layer, x0 + 20, y0 + 20, place, 11)
    bx = x1 - 30
    layer.rect((bx, y0 + 10, bx + 20, y0 + 30), fill=(40, 22, 16, 230) if not paper else (224, 208, 172, 230),
               outline=VERMILION_INK if paper else (150, 70, 52, 255))
    layer.text((bx + 10, y0 + 20), '×', font(16), fill=VERMILION if paper else RED, anchor='mm')
    ink, quiet = (INK, INK_QUIET) if paper else (NAME, QUIET)
    ty = y0 + fs + 20
    layer.text(((x0 + x1) / 2, ty), u['Name'], font(17), fill=ink, anchor='mm')
    layer.text(((x0 + x1) / 2, ty + 21), u['Type'], font(12), fill=quiet, anchor='mm')
    layer.text((x0 + 8, ty + 44), str(u['Troops']), font(15), fill=INK if paper else TEXT, anchor='lm')
    cost = f"{u['Cost'] * 100:,}K"
    layer.text((x1 - 8, ty + 44), cost, font(15), fill=VERMILION_INK if paper else TEXT, anchor='rm')
    troop_bar(layer, x0 + 8, ty + 55, x1 - x0 - 16, u['Troops'], u['MaxTroops'], ink=paper)
    if state == 'dragged':
        layer.im.putalpha(layer.im.getchannel('A').point(lambda v: int(v * 0.35)))
    c.im = Image.alpha_composite(c.im, layer.im)
    c.d = ImageDraw.Draw(c.im)
    if state.startswith('drop'):
        drop_frame(c, r, state)


def e_line(c, members, style, states, y0, h):
    """Four standing cards in a line: back row 3, 4 | front row 1, 2 ▶ (towards the enemy)."""
    x0, x1 = P[0] + 14, P[2] - 14
    gap, mid_gap = 6, 16
    w = (x1 - x0 - 2 * gap - mid_gap) / 4
    xs = [x0, x0 + w + gap, x0 + 2 * w + gap + mid_gap, x0 + 3 * w + 2 * gap + mid_gap]
    c.text(((xs[0] + xs[1] + w) / 2, y0), 'Back row', cinzel(13, 700), fill=LABEL, anchor='mm')
    front_label(c, (xs[2] + xs[3] + w) / 2, y0)
    m = (xs[1] + w + xs[2]) / 2
    c.line((m, y0 + 14), (m, y0 + 14 + h), fill=DIM)
    for x, place in zip(xs, (3, 4, 1, 2)):
        n = members[place - 1] if place <= len(members) else None
        st = states.get(place, 'empty' if n is None else ('hover' if n == HOVERED else 'normal'))
        card_tall(c, (round(x), y0 + 14, round(x + w), y0 + 14 + h), place, n, style, st)
    return y0 + 14 + h


def e1():
    """E1: D5 tidied — 2x2 lacquer cards, totals, Attack wide and Stop small (an attack)."""
    c = e_base('attack', PARTY)
    e_header(c)
    end = e_grid(c, PARTY, 'lacquer', {}, P[0] + 14, P[2] - 14, P[1] + 62, 104)
    c.line((P[0] + 16, end + 10), (P[2] - 16, end + 10))
    e_totals(c, end + 30, PARTY)
    e_buttons(c, (P[0] + 14, P[3] - 72, P[2] - 14, P[3] - 14), 'attack', PARTY)
    return c


def e2():
    """E2: E1 in a defence — Plains and City side by side, Stop small."""
    c = e_base('defence', PARTY)
    e_header(c)
    end = e_grid(c, PARTY, 'lacquer', {}, P[0] + 14, P[2] - 14, P[1] + 62, 104)
    c.line((P[0] + 16, end + 10), (P[2] - 16, end + 10))
    e_totals(c, end + 30, PARTY)
    e_buttons(c, (P[0] + 14, P[3] - 72, P[2] - 14, P[3] - 14), 'defence', PARTY, hover='City')
    return c


def e3(mode):
    """E3: four standing cards in a line, the back row left of a rule, the front row right (an attack and a defence)."""
    c = e_base(mode, PARTY)
    e_header(c)
    end = e_line(c, PARTY, 'lacquer', {}, P[1] + 62, 214)
    c.line((P[0] + 16, end + 12), (P[2] - 16, end + 12))
    e_totals(c, end + 32, PARTY)
    e_buttons(c, (P[0] + 14, P[3] - 66, P[2] - 14, P[3] - 14), mode, PARTY)
    return c


def e4():
    """E4: the buttons a column at the right; the 2x2 cards narrower, the totals under them (a defence, three places
    offered: the fourth a dark slot)."""
    c = e_base('defence', PARTY[:3])
    e_header(c)
    bw = 128
    gx1 = P[2] - 14 - bw - 12
    end = e_grid(c, PARTY[:3], 'lacquer', {4: 'locked'}, P[0] + 14, gx1, P[1] + 62, 128)
    c.line((gx1 + 6, P[1] + 54), (gx1 + 6, P[3] - 16))
    e_column_buttons(c, (P[2] - 14 - bw, P[1] + 58, P[2] - 14, P[3] - 16), 'defence', hover='Plains')
    c.line((P[0] + 16, end + 12), (gx1 - 4, end + 12))
    e_totals(c, end + 34, PARTY[:3])
    return c


def e5():
    """E5: E3 with paper cards (muster slips) and a drag under way: Barres from place 4 onto place 1, the gold highlight."""
    c = e_base('attack', PARTY)
    e_header(c)
    end = e_line(c, PARTY, 'paper', {4: 'dragged', 1: 'drop-gold'}, P[1] + 62, 214)
    c.line((P[0] + 16, end + 12), (P[2] - 16, end + 12))
    e_totals(c, end + 32, PARTY)
    e_buttons(c, (P[0] + 14, P[3] - 66, P[2] - 14, P[3] - 14), 'attack', PARTY)
    ghost(c, 1010, 560, 6, 'paper')
    return c


def e6():
    """E6: the party as a field: the two rows facing right over a dark ground, the enemy's side marked; the cards
    compact (face, name, troops), the totals and the bar under it (a defence)."""
    c = e_base('defence', PARTY)
    e_header(c, 'Formation')
    x0, x1 = P[0] + 14, P[2] - 14
    fy0, fy1 = P[1] + 56, P[1] + 290
    c.rect((x0, fy0, x1, fy1), fill=(30, 24, 16, 255), outline=DIM)
    for i in range(1, 12):                                  # the ground's faint furrows
        c.line((x0 + 4, fy0 + i * 20), (x1 - 4, fy0 + i * 20), fill=(46, 36, 24, 255))
    mid = (x0 + x1) / 2 - 24
    c.line((mid, fy0 + 8), (mid, fy1 - 8), fill=DIM)
    c.text(((x0 + mid) / 2, fy0 + 14), 'Back row', cinzel(12, 700), fill=LABEL, anchor='mm')
    c.text(((mid + x1 - 50) / 2, fy0 + 14), 'Front row', cinzel(12, 700), fill=LABEL, anchor='mm')
    # the enemy's side: a vermilion band with a chevron
    c.rect((x1 - 40, fy0 + 1, x1 - 1, fy1 - 1), fill=(90, 24, 16, 255))
    for j in range(3):
        cy = (fy0 + fy1) / 2 + (j - 1) * 26
        c.d.polygon([((x1 - 30) * S, (cy - 10) * S), ((x1 - 14) * S, cy * S), ((x1 - 30) * S, (cy + 10) * S)],
                    fill=(222, 120, 90, 255))
    cw, chh = (mid - x0 - 24) / 1, 98
    spots = {3: (x0 + 12, fy0 + 30), 4: (x0 + 12, fy0 + 30 + chh + 6), 1: (mid + 12, fy0 + 30), 2: (mid + 12, fy0 + 30 + chh + 6)}
    for place, (cx, cy) in spots.items():
        n = PARTY[place - 1]
        card(c, (round(cx), cy, round(cx + (x1 - 52 - mid - 12) if place in (1, 2) else cx + cw), cy + chh), place, n,
             'lacquer', 'hover' if n == HOVERED else 'normal')
    e_totals(c, fy1 + 26, PARTY)
    c.line((P[0] + 16, fy1 + 46), (P[2] - 16, fy1 + 46))
    e_buttons(c, (P[0] + 14, P[3] - 72, P[2] - 14, P[3] - 14), 'defence', PARTY)
    return c


def e_round():
    e1().save('E1-grid-attack.png', 'E1 — 2x2 cards, an attack',
              'D5 tidied: lacquer cards two by two (back row left, front row right), the totals, Attack wide with the expenses, Stop small.')
    e2().save('E2-grid-defence.png', 'E2 — 2x2 cards, a defence',
              'The same in a defence: Plains and City side by side in red lacquer (City hovered), Stop small.')
    e3('attack').save('E3a-line-attack.png', 'E3 — four standing cards in a line, an attack',
                      'Back row 3, 4 | front row 1, 2 towards the enemy; each card a large face, the name, the type, troops and cost; Attack wide.')
    e3('defence').save('E3b-line-defence.png', 'E3 — four standing cards in a line, a defence',
                       'The same with Plains and City.')
    e4().save('E4-column-buttons.png', 'E4 — the buttons in a column, a defence of three places',
              'The cards 2x2 narrower and taller, the bar a column at the right (Plains, City, Stop); a place not offered a dark slot.')
    e5().save('E5-line-paper-drag.png', 'E5 — E3 with paper cards, a drag',
              'Muster slips with vermilion seals; Barres dragged from place 4 onto place 1 (dimmed, his face under the pointer; the gold highlight).')
    e6().save('E6-field.png', 'E6 — the formation as a field, a defence',
              'The rows on a dark ground with the enemy\'s side in vermilion at the right; compact cards; totals, Plains, City, Stop below.')


# ---------------------------------------------------------------- round F: the facing mark (2026-10-19)
# The owner looked at the built E1/E2 and asked for a triangle right of each card's face that shows which way the unit
# faces (towards the enemy, on the right, as "Front row ▶"). Six marks on E1; a sheet puts the six party panels side by
# side. The text right of the face moves over by the mark's room.
#   python -I tools/ui/mock_deployment.py <units.json> <cg_png> <picker dump> <out> f < /dev/null

FACING = None


def facing_mark(c, fx, fy, fs, place):
    """The mark right of a face (fx, fy, fs: the face's square); returns the room it takes before the text."""
    x1, cy = fx + fs, fy + fs / 2

    def poly(points, fill=None, outline=None, width=1):
        pts = [(a * S, b * S) for a, b in points]
        if fill:
            c.d.polygon(pts, fill=fill)
        if outline:
            c.d.line(pts + ([pts[0]] if fill else []), fill=outline, width=width * S)

    if FACING in ('T1', 'T6'):                          # a solid arrowhead in the gap
        colour = GOLD
        if FACING == 'T6':
            colour = HOVER_LINE if place in (1, 2) else DIM
        poly([(x1 + 4, cy - 9), (x1 + 13, cy), (x1 + 4, cy + 9)], fill=colour)
        return 10
    if FACING == 'T2':                                  # a tab out of the face's frame, as a speech tail
        poly([(x1 - 1, cy - 11), (x1 + 11, cy), (x1 - 1, cy + 11)], fill=(52, 40, 28, 255))
        poly([(x1, cy - 11), (x1 + 11, cy), (x1, cy + 11)], outline=GOLD)
        c.line((x1 - 1, cy - 10), (x1 - 1, cy + 10), fill=(52, 40, 28, 255), width=2)
        return 9
    if FACING == 'T3':                                  # the face's plate pointed to the right (drawn under the face)
        return 14
    if FACING == 'T4':                                  # two vermilion chevrons
        for dx in (0, 7):
            poly([(x1 + 5 + dx, cy - 8), (x1 + 11 + dx, cy), (x1 + 5 + dx, cy + 8)], outline=(214, 80, 56, 255), width=2)
        return 14
    if FACING == 'T5':                                  # a gold strip along the face's right edge with a pale arrowhead
        c.rect((x1, fy, x1 + 9, fy + fs), fill=(70, 52, 28, 255), outline=GOLD)
        poly([(x1 + 2, cy - 7), (x1 + 8, cy), (x1 + 2, cy + 7)], fill=NAME)
        return 8
    return 0


def facing_plate(c, fx, fy, fs):
    """T3: the face's plate a pentagon pointing right, framed in gold (the square face laid over its left part)."""
    x1, y1, cy = fx + fs, fy + fs, fy + fs / 2
    pts = [(fx, fy), (x1, fy), (x1 + 14, cy), (x1, y1), (fx, y1)]
    c.d.polygon([(a * S, b * S) for a, b in pts], fill=(52, 40, 28, 255))
    c.d.line([(a * S, b * S) for a, b in pts + [pts[0]]], fill=GOLD, width=S)


FACINGS = [('T1', 'T1 — a gold arrowhead', 'A solid gold triangle in the gap right of the face, the text moved over by its room.'),
           ('T2', 'T2 — a tab out of the frame', "The face's gold frame breaks into a pointed tab on its right side, as the message windows' tails."),
           ('T3', "T3 — the face's plate pointed", 'The plate under the face a pentagon: its right side runs out to a point, framed in gold.'),
           ('T4', 'T4 — vermilion chevrons', 'Two vermilion chevrons right of the face, as a marching sign.'),
           ('T5', 'T5 — a gold strip', "A narrow gold strip along the face's right edge with a pale arrowhead in its middle."),
           ('T6', 'T6 — T1 by row', 'T1, bright gold in the front row, dim gold in the back row (the front closer to the enemy).')]


def f_round():
    global FACING
    panels = []
    for name, caption, note in FACINGS:
        FACING = name
        c = e1()
        c.save(f'{name}-facing.png', caption, note)
        panels.append((name, c.im.crop((P[0] * S, P[1] * S, P[2] * S, P[3] * S)).resize((P[2] - P[0], P[3] - P[1]), Image.LANCZOS)))
    FACING = None
    pw, ph = P[2] - P[0], P[3] - P[1]
    sheet = Image.new('RGB', (pw * 3 + 40, (ph + 34) * 2 + 20), (14, 10, 8))
    d = ImageDraw.Draw(sheet)
    label = ImageFont.truetype(CINZEL, 20)
    label.set_variation_by_axes([700])
    for i, (name, im) in enumerate(panels):
        x, y = 10 + (i % 3) * (pw + 10), 10 + (i // 3) * (ph + 44)
        d.text((x + 4, y), name, font=label, fill=NAME)
        sheet.paste(im.convert('RGB'), (x, y + 30))
    sheet.save(os.path.join(OUT, 'T-sheet.png'))


# ---------------------------------------------------------------- round U: one facing mark for the formation (2026-10-19)
# Not a mark per face (round F): one mark right of the cards, showing which way the whole party faces (towards the
# enemy, on the right). The cards give up a strip at the panel's right edge (U3 keeps them whole: the rule under them is the arrow).
#   python -I tools/ui/mock_deployment.py <units.json> <cg_png> <picker dump> <out> u < /dev/null


def u_screen(kind):
    c = e_base('attack', PARTY)
    e_header(c)
    strip = {'U1': 34, 'U2': 40, 'U3': 0, 'U4': 26, 'U5': 30, 'U6': 34}[kind]
    gx0, gx1 = P[0] + 14, P[2] - 14 - strip - (6 if strip else 0)
    top = P[1] + 62
    end = e_grid(c, PARTY, 'lacquer', {}, gx0, gx1, top, 104)
    y0, y1 = top + 12, end                               # the cards' top and bottom
    sx0, sx1 = gx1 + 6, P[2] - 14                        # the strip
    cy = (y0 + y1) / 2

    def poly(points, fill=None, outline=None, width=1):
        pts = [(a * S, b * S) for a, b in points]
        if fill:
            c.d.polygon(pts, fill=fill)
        if outline:
            c.d.line(pts + [pts[0]], fill=outline, width=width * S)

    if kind == 'U1':                                     # a tall slim triangle as high as the two rows
        poly([(sx0 + 2, y0), (sx1, cy), (sx0 + 2, y1)], fill=(70, 52, 28, 255), outline=GOLD, width=2)
        poly([(sx0 + 8, y0 + 26), (sx1 - 9, cy), (sx0 + 8, y1 - 26)], outline=DIM)
    elif kind == 'U2':                                   # the enemy's side: a vermilion band with chevrons
        c.rect((sx0, y0, sx1, y1), fill=(90, 24, 16, 255), outline=(150, 70, 52, 255))
        for j in (-1, 0, 1):
            yy = cy + j * 30
            poly([(sx0 + 12, yy - 11), (sx1 - 10, yy), (sx0 + 12, yy + 11)], fill=(222, 120, 90, 255))
    elif kind == 'U3':                                   # the rule under the cards an arrow: the line runs to a gold arrowhead
        ry = end + 10
        c.line((P[0] + 16, ry), (P[2] - 40, ry), fill=GOLD, width=2)
        poly([(P[2] - 42, ry - 9), (P[2] - 16, ry), (P[2] - 42, ry + 9)], fill=GOLD)
    elif kind == 'U4':                                   # three small gold arrowheads stacked
        for j in (-1, 0, 1):
            yy = cy + j * 34
            poly([(sx0 + 4, yy - 10), (sx1, yy), (sx0 + 4, yy + 10)], fill=GOLD if j == 0 else DIM)
    elif kind == 'U5':                                   # one medium gold triangle at the rows' middle, a rule from it to the edges
        c.line((sx0 + 12, y0 + 4), (sx0 + 12, cy - 22), fill=DIM)
        c.line((sx0 + 12, cy + 22), (sx0 + 12, y1 - 4), fill=DIM)
        poly([(sx0 + 2, cy - 18), (sx1, cy), (sx0 + 2, cy + 18)], fill=GOLD)
        poly([(sx0 + 6, cy - 10), (sx1 - 7, cy), (sx0 + 6, cy + 10)], fill=(90, 66, 34, 255))
    elif kind == 'U6':                                   # a lacquer tab framed in gold, "Enemy" written up it, the arrowhead under
        c.rect((sx0, y0, sx1, y1), fill=(34, 26, 20, 255), outline=DIM)
        layer = Image.new('RGBA', (200 * S, 40 * S), (0, 0, 0, 0))
        ImageDraw.Draw(layer).text((100 * S, 20 * S), 'ENEMY', font=cinzel(13, 700), fill=LABEL, anchor='mm')
        layer = layer.rotate(90, expand=True)
        c.im.alpha_composite(layer, (round(((sx0 + sx1) / 2 - 20) * S), round((cy - 100 - 20) * S)))
        c.d = ImageDraw.Draw(c.im)
        poly([(sx0 + 7, cy + 24), (sx1 - 6, cy + 40), (sx0 + 7, cy + 56)], fill=GOLD)
    e_totals(c, end + 30, PARTY)
    if kind != 'U3':
        c.line((P[0] + 16, end + 10), (P[2] - 16, end + 10))
    e_buttons(c, (P[0] + 14, P[3] - 72, P[2] - 14, P[3] - 14), 'attack', PARTY)
    return c


UMARKS = [('U1', 'U1 — one tall triangle', 'A slim triangle as high as both rows at the panel\'s right edge, dark gold framed in gold; the cards a strip narrower.'),
          ('U2', "U2 — the enemy's side", 'A vermilion band with three chevrons right of the front row, as the field of E6.'),
          ('U3', 'U3 — the rule an arrow', 'The rule under the cards drawn in gold and ending in an arrowhead at the right; the cards keep their width.'),
          ('U4', 'U4 — three arrowheads', 'Three small arrowheads stacked right of the front row, the middle one bright.'),
          ('U5', 'U5 — one gold triangle', 'One medium gold triangle at the rows\' middle, a dim rule above and below it.'),
          ('U6', 'U6 — a tab: "Enemy"', 'A narrow lacquer tab right of the front row, "ENEMY" written up it, an arrowhead under the word.')]


def u_round():
    panels = []
    for name, caption, note in UMARKS:
        c = u_screen(name)
        c.save(f'{name}-formation.png', caption, note)
        panels.append((name, c.im.crop((P[0] * S, P[1] * S, P[2] * S, P[3] * S)).resize((P[2] - P[0], P[3] - P[1]), Image.LANCZOS)))
    pw, ph = P[2] - P[0], P[3] - P[1]
    sheet = Image.new('RGB', (pw * 3 + 40, (ph + 34) * 2 + 20), (14, 10, 8))
    d = ImageDraw.Draw(sheet)
    label = ImageFont.truetype(CINZEL, 20)
    label.set_variation_by_axes([700])
    for i, (name, im) in enumerate(panels):
        x, y = 10 + (i % 3) * (pw + 10), 10 + (i // 3) * (ph + 44)
        d.text((x + 4, y), name, font=label, fill=NAME)
        sheet.paste(im.convert('RGB'), (x, y + 30))
    sheet.save(os.path.join(OUT, 'U-sheet.png'))


ROUND = sys.argv[5] if len(sys.argv) > 5 else 'd'
if ROUND == 'u':
    u_round()
    print('done:', OUT)
    sys.exit(0)
if ROUND == 'f':
    f_round()
    print('done:', OUT)
    sys.exit(0)
if ROUND == 'e':
    e_round()
    print('done:', OUT)
    sys.exit(0)


screen_a('lacquer', 'three', states={3: 'hover'}).save(
    'D1-lacquer-cards.png', 'D1 — Lacquer cards, three buttons',
    'The ledger (members gilt with their place\'s seal); details and party in lacquer; a card a dark box, the face in gold, '
    'the place on a gold diamond; the bar as now.')
screen_a('paper', 'wide', members=[1, 12]).save(
    'D2-paper-cards.png', 'D2 — Paper cards, Attack wide',
    'The cards as muster slips (paper, ink, the place a vermilion seal), an empty place dashed; Attack wide with the '
    'expense, Stop small, no plate for "---".')
screen_a('lacquer', 'three', states={1: 'dragged', 2: 'drop-gold'}, drag=(560, 712, 1, 'lacquer')).save(
    'D3-drag-gold.png', 'D3 — A drag, the gold highlight (on D1)',
    'Rance dragged from place 1 (his card dimmed, his face under the pointer) onto place 2: the place glows gold, '
    'framed twice, "⇄ Swap".')
screen_a('paper', 'wide', states={3: 'dragged', 1: 'drop-vermilion'}, drag=(574, 620, 8, 'paper')).save(
    'D4-drag-vermilion.png', 'D4 — A drag, the vermilion highlight (on D2)',
    'Cordoba dragged from place 3 onto place 1: the place tinted and framed in seal red with a red "⇄ Swap" tag. '
    'Either highlight goes with either card.')
screen_b().save(
    'D5-subordinates-layout.png', 'D5 — The Subordinates screen\'s layout (for comparison)',
    'The ledger across the top with every column (Type, Rows, Item too), no scroll; the portrait below left; the party '
    'and the bar right; no details panel.')
print('done:', OUT)
