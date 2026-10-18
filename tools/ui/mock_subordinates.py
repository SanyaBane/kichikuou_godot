"""Mock-ups of our own Subordinates screen (Cﾌﾞｶ / Sﾌﾞｶ): ten layouts after the deployment screen, in its steel
palette (godot/Strategy/DeploymentPalette.cs), with the troop actions on our own buttons instead of Maris's dialogue.

The units are those of the kept save saves-week18 (week 18), read from its slot by a script over the .ksav into a
JSON file ({"units": [records of page 1 with Index], "strings": [...]}); the CGs are the player's own:
  python -I tools/ui/mock_subordinates.py <units.json> <cg_png dir> <out dir> < /dev/null
The images hold the player's CGs: keep <out dir> outside the repository.
Everything is in the 1280x800 screen's pixels (plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os, json
from PIL import Image, ImageDraw, ImageFont

UNITS, CGS, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H, CAPTION = 1280, 800, 64


def hx(s, a=255):
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4)) + (a,)


# The steel palette of DeploymentPalette.Steel.
BACK, CARD, BORDER = hx('2b2d31'), hx('3b3e44'), hx('a88a4a')
TEXT, MUTED, WARN, ACCENT = hx('f0f0ee'), hx('a5a7ab'), hx('e2725b'), hx('f2c75c')
BUTTON, BUTTON_HOVER, BUTTON_OFF = hx('45484f'), hx('575b63'), hx('2f3135')
BUTTON_BORDER, BUTTON_TEXT, TITLE, SELECTED, GREY = hx('c9a85a'), hx('f4e6c0'), hx('3d4046'), hx('7a6430', 160), hx('7d7f84')
GOOD = hx('8fc27a')

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
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


data = json.load(open(UNITS, encoding='utf-8'))
STR = data['strings']
WEEK, GOLD = 18, 1141          # the week and the budget (x100K) the deployment screen showed for this save
for u in data['units']:
    base = 20 + (u['Index'] - 1) * 3
    u['Name'], u['Type'] = STR[base], STR[base + 1]

# UnitFaces: the owner's squares by face CG (24, 7, 144 otherwise).
CROPS = {1: (33, 7, 128), 7: (28, 7, 136), 8: (31, 7, 128), 10: (32, 7, 128), 402: (15, 9, 136), 403: (44, 7, 136),
         404: (28, 7, 128), 406: (38, 7, 112), 430: (26, 45, 144), 6: (24, 7, 144)}


def unit(n):
    return next(u for u in data['units'] if u['Index'] == n)


def refill(u):
    """Cﾌﾞｶ.ADV:1005-1009: the deficiency and its price, one per 50 men, at least 1 (None when full)."""
    d = u['MaxTroops'] - u['Troops']
    return None if d == 0 else (d, max(1, d // 50))


def offer(u):
    """Cﾌﾞｶ.ADV:757-799: the new troops' men and price, with a quarter of the week; None when none is offered."""
    n, price, men = u['Index'], 0, 25
    if u['Class'] == 0 and u['TroopAtk'] == 10: price = 110
    if u['Class'] == 0 and u['TroopAtk'] == 11: price = 300
    if u['Class'] == 0 and u['TroopAtk'] == 8: price = 50
    if u['Class'] == 2 and u['TroopAtk'] == 3: price = 100
    if n in (3, 4): price, men = 30, 50
    if n in (5, 33, 36, 18): price = 200
    if n == 16: price = 120
    if u['Class'] == 3: price = 150
    if price == 0: price, men = 320, 10
    if n > 47 or n in (25, 41): return None
    return men, price + WEEK // 4


def tiers(u, count=4):
    """Add More: the men doubled, the price tripled, while the price is under 10000."""
    o = offer(u)
    out = []
    while o and len(out) < count:
        out.append(o)
        if o[1] >= 10000: break
        o = (o[0] * 2, o[1] * 3)
    return out


def gold(v):
    return f'{v * 100}K'


class Canvas:
    def __init__(self):
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), BACK)
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
        if radius:
            dd.rounded_rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], radius * S, fill=fill)
        else:
            dd.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill)
        self.im = Image.alpha_composite(self.im, layer)
        self.d = ImageDraw.Draw(self.im)

    def text(self, xy, s, f, fill=TEXT, anchor='la'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=BORDER, width=1):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=width * S)

    def paste(self, img, xy):
        self.im.alpha_composite(img, (xy[0] * S, xy[1] * S))
        self.d = ImageDraw.Draw(self.im)

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=hx('17181b'))
        self.line((0, H), (W, H), fill=BORDER)
        self.text((16, H + 10), caption, cinzel(20, 700), fill=ACCENT)
        self.text((16, H + 38), note, font(15), fill=MUTED)
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def cg(n):
    return Image.open(os.path.join(CGS, 'CG%04d.png' % n)).convert('RGBA')


def portrait(c, r, u):
    """The unit's status-screen CG (600 + n, 304x186, its description in it) framed in gold."""
    x0, y0, x1, y1 = r
    c.rect(r, fill=hx('000000'), outline=BORDER, width=2)
    img = cg(u['PortraitCg']).resize(((x1 - x0 - 8) * S, (y1 - y0 - 8) * S), Image.LANCZOS)
    c.paste(img, (x0 + 4, y0 + 4))
    c.rect((x0 + 2, y0 + 2, x1 - 2, y1 - 2), outline=hx('d8b25e'), width=1)


def face(c, x, y, size, n, dim=False):
    """A unit's dialogue face cut to its square (UnitFaces)."""
    cx, cy, cs = CROPS.get(n, (24, 7, 144))
    img = cg(n).crop((cx, cy, cx + cs, cy + cs)).resize((size * S, size * S), Image.LANCZOS)
    if dim:
        grey = img.convert('LA').convert('RGBA')
        img = Image.blend(grey, Image.new('RGBA', grey.size, (40, 40, 44, 255)), 0.35)
    c.paste(img, (x, y))
    c.rect((x, y, x + size, y + size), outline=BORDER, width=1)


def panel(c, r, title=None):
    c.rect(r, fill=BACK, outline=BORDER, width=1)
    if title:
        c.text((r[0] + 12, r[1] + 8), title, cinzel(15, 700), fill=ACCENT)


def button(c, r, label, sub=None, state='normal', big=20, align='center'):
    fill = {'normal': BUTTON, 'hover': BUTTON_HOVER, 'off': BUTTON_OFF, 'pressed': hx('34363b')}[state]
    edge = BUTTON_BORDER if state != 'off' else hx('55575c')
    tc = BUTTON_TEXT if state != 'off' else GREY
    c.rect(r, fill=fill, outline=edge, width=2 if state == 'hover' else 1)
    x0, y0, x1, y1 = r
    cx = (x0 + x1) / 2 if align == 'center' else x0 + 14
    anchor = 'mm' if align == 'center' else 'lm'
    if sub:
        c.text((cx, y0 + (y1 - y0) * 0.38), label, font(big), fill=tc, anchor=anchor)
        c.text((cx, y0 + (y1 - y0) * 0.72), sub, font(14), fill=MUTED if state != 'off' else GREY, anchor=anchor)
    else:
        c.text((cx, (y0 + y1) / 2), label, font(big), fill=tc, anchor=anchor)


def chip(c, x, y, label, on=True, w=None):
    f = font(13)
    w = w or c.width(label, f) + 16
    c.rect((x, y, x + w, y + 22), fill=hx('4a4231') if on else BUTTON_OFF, outline=ACCENT if on else hx('55575c'), radius=4)
    c.text((x + w / 2, y + 11), label, f, fill=ACCENT if on else GREY, anchor='mm')
    return w


def pips(c, x, y, label, left):
    """A charge: its name and one pip, lit while it is left this week."""
    c.text((x, y), label, font(14), fill=MUTED)
    px = x + c.width(label, font(14)) + 8
    c.d.ellipse([px * S, (y + 2) * S, (px + 12) * S, (y + 14) * S], fill=ACCENT if left else None,
                outline=ACCENT if left else GREY, width=S)
    c.text((px + 18, y), 'left this week' if left else 'used this week', font(14), fill=TEXT if left else GREY)


COLUMNS = [('', 20), ('№', 32), ('Name', 92), ('Troops', 104), ('Refill', 64), ('T.Atk', 52), ('T.Def', 52),
           ('Str%', 48), ('HP', 44), ('Lv', 30), ('EXP', 66)]
WIDE = [('', 22), ('№', 38), ('Name', 100), ('Type', 150), ('Troops', 112), ('Refill', 76), ('T.Atk', 60),
        ('T.Def', 60), ('Str%', 56), ('HP', 56), ('Lv', 36), ('EXP', 80), ('Atk', 48), ('Def', 48), ('Act', 48),
        ('Mag', 48), ('Item', 150)]


def cells(u):
    r = refill(u)
    return {'': '', '№': str(u['Index']), 'Name': u['Name'], 'Type': u['Type'],
            'Troops': f"{u['Troops']}/{u['MaxTroops']}", 'Refill': gold(r[1]) if r else '—',
            'T.Atk': str(u['TroopAtk']), 'T.Def': str(u['TroopDef']), 'Str%': f"{u['Strategy']}%",
            'HP': f"{u['HP']}/{u['MaxHP']}", 'Lv': str(u['Level']), 'EXP': f"{u['Exp']}/{100 + u['Level'] * 30}",
            'Atk': str(u['Atk']), 'Def': str(u['Def']), 'Act': str(u['Attacks']), 'Mag': str(u['Magic']),
            'Item': STR[936 + u['Item'] - 1] if u['Item'] else '—'}


def table(c, r, selected=12, hover=None, columns=COLUMNS, row_h=30, drawer=None, scroll=True):
    x0, y0, x1, y1 = r
    c.rect(r, fill=BACK, outline=BORDER, width=1)
    c.rect((x0 + 1, y0 + 1, x1 - 1, y0 + 32), fill=TITLE)
    x = x0 + 4
    for name, w in columns:
        right = name not in ('Name', 'Type', 'Item', '')
        mark = ' ↑' if name == '№' else ''
        if right:
            c.text((x + w - 4, y0 + 17), name + mark, font(15), fill=TEXT, anchor='rm')
        else:
            c.text((x + (16 if name == 'Item' else 2), y0 + 17), name, font(15), fill=TEXT, anchor='lm')
        x += w
    y = y0 + 34
    for u in data['units']:
        h = row_h + (drawer[1] if drawer and u['Index'] == selected else 0)
        if u['Index'] == selected:
            c.overlay((x0 + 1, y, x1 - 1, y + h), SELECTED)
            c.rect((x0 + 1, y, x1 - 1, y + h), outline=ACCENT, width=1)
        elif u['Index'] == hover:
            c.overlay((x0 + 1, y, x1 - 1, y + row_h), hx('ffffff', 22))
        tc = GREY if u['Status'] != 1 else TEXT
        x = x0 + 4
        values = cells(u)
        for name, w in columns:
            v = values[name]
            if name == '':
                if u['Index'] == selected:
                    c.text((x + w / 2, y + row_h / 2), '▶', font(13), fill=ACCENT, anchor='mm')
                elif u['Status'] != 1:
                    c.text((x + w / 2, y + row_h / 2), '·', font(15), fill=GREY, anchor='mm')
            elif name == 'Refill' and v != '—' and u['Status'] == 1:
                c.text((x + w - 4, y + row_h / 2), v, font(16), fill=ACCENT, anchor='rm')
            elif name in ('Name', 'Type', 'Item'):
                c.text((x + (16 if name == 'Item' else 2), y + row_h / 2), v, font(17 if name == 'Name' else 15), fill=tc if name == 'Name' else MUTED, anchor='lm')
            else:
                c.text((x + w - 4, y + row_h / 2), v, font(16), fill=tc, anchor='rm')
            x += w
        if drawer and u['Index'] == selected:
            drawer[0](c, (x0 + 8, y + row_h, x1 - 8, y + h - 6))
        y += h
    if not scroll:
        return
    # the horizontal scrollbar of the deployment table: the columns go on to Atk, Def, Act, Mag
    c.rect((x0 + 4, y1 - 12, x1 - 4, y1 - 4), fill=TITLE)
    c.rect((x0 + 4, y1 - 12, x0 + 4 + (x1 - x0 - 8) * 0.62, y1 - 4), fill=hx('8a7440'))


def details(c, r, u, rows=3):
    x0, y0, x1, y1 = r
    panel(c, r)
    f, m = font(17), font(17)
    def kv(x, y, k, v, vc=TEXT):
        c.text((x, y), k, m, fill=MUTED)
        c.text((x + (c.width(k, m) + 8 if k else 0), y), v, f, fill=vc)
    kv(x0 + 10, y0 + 10, '', u['Name'])
    kv(x0 + 120, y0 + 10, 'Type', u['Type'])
    kv(x0 + 10, y0 + 34, 'Troops', f"{u['Troops']}/{u['MaxTroops']}", WARN if u['Troops'] * 4 < u['MaxTroops'] else TEXT)
    kv(x0 + 230, y0 + 34, 'T.Atk', str(u['TroopAtk']))
    kv(x0 + 340, y0 + 34, 'T.Def', str(u['TroopDef']))
    if rows >= 3:
        kv(x0 + 10, y0 + 58, 'HP', f"{u['HP']}/{u['MaxHP']}")
        kv(x0 + 120, y0 + 58, 'Lv', str(u['Level']))
        kv(x0 + 190, y0 + 58, 'EXP', f"{u['Exp']}/{100 + u['Level'] * 30}")
        kv(x0 + 340, y0 + 58, 'Item', '—' if not u['Item'] else 'yes')


def end_button(c, r):
    button(c, r, 'End', big=34)


def maris(c, r, lines, title='Report from Maris'):
    """Maris's report: our own plate with her face (CG 6), not WAO_SUB's windows."""
    x0, y0, x1, y1 = r
    c.rect(r, fill=hx('24262a'), outline=ACCENT, width=1)
    size = y1 - y0 - 16
    face(c, x0 + 8, y0 + 8, size, 6)
    tx = x0 + size + 20
    c.text((tx, y0 + 8), title, cinzel(14, 700), fill=ACCENT)
    for i, s in enumerate(lines):
        c.text((tx, y0 + 30 + i * 21), s, font(15), fill=TEXT if i == 0 else MUTED)


def action_lines(u):
    r, t = refill(u), tiers(u)
    return (f"+{r[0]} troops · {gold(r[1])}" if r else 'full',
            f"+{t[0][0]} troops · {gold(t[0][1])}" if t else 'none offered')


HAUREIN = unit(12)
REPORT = [f"Haurein's Leazas Soldiers replenished by 1350 to 1400.", f"Budget {gold(GOLD)} → {gold(GOLD - 27)}"]


# ---------------------------------------------------------------- the ten layouts

def a_twin():
    """The deployment screen's own frame: the party panel's place holds three buttons."""
    c = Canvas(); u = HAUREIN
    portrait(c, (16, 16, 636, 396), u)
    details(c, (16, 404, 636, 492), u)
    panel(c, (16, 500, 636, 784), 'Orders')
    pips(c, 160, 509, 'Troops', True)
    pips(c, 420, 509, 'Talk', True)
    rl, al = action_lines(u)
    button(c, (28, 540, 224, 680), 'Replenish', rl)
    button(c, (232, 540, 428, 680), 'Add Troops', al)
    button(c, (436, 540, 624, 680), 'Talk', 'with Haurein')
    c.text((28, 694), 'Replenish and Add Troops share the week\'s troop order; Talk is its own.', font(14), fill=MUTED)
    c.text((28, 716), 'A click does it at once: no Do / Don\'t, no dialogue.', font(14), fill=MUTED)
    table(c, (644, 16, 1264, 668))
    end_button(c, (644, 676, 1264, 784))
    c.save('A-twin.png', 'A — Twin of the deployment screen',
           'Portrait, details and the table where the deployment screen has them; three equal buttons in the party panel\'s place.')


def b_charges():
    """Two groups, one per weekly charge; shown after a replenish: the troop group spent, Maris's report on top."""
    c = Canvas(); HAUREIN['Troops'] = 1400; u = HAUREIN
    portrait(c, (16, 16, 636, 396), u)
    details(c, (16, 404, 636, 470), u, rows=2)
    x0, y0 = 16, 478
    panel(c, (x0, y0, 420, 784), 'Troops order')
    c.text((410, y0 + 10), 'used this week', font(14), fill=GREY, anchor='ra')
    button(c, (28, 512, 408, 572), 'Replenish', 'full: 1400 troops', state='off')
    c.rect((28, 580, 408, 676), fill=BUTTON_OFF, outline=hx('55575c'))
    c.text((218, 600), 'Add New Troops', font(20), fill=GREY, anchor='mm')
    x = 44
    for i, (men, price) in enumerate(tiers(u, 3)):
        x += chip(c, x, 630, f'+{men} · {gold(price)}', on=False) + 8
    panel(c, (428, y0, 636, 784), 'Talk')
    c.text((626, y0 + 10), '1 left', font(14), fill=ACCENT, anchor='ra')
    face(c, 472, 514, 120, u['FaceCg'])
    button(c, (440, 644, 624, 700), 'Talk', 'with Haurein')
    maris(c, (28, 690, 420, 776), ['Replenished by 1350 to 1400.', f'Budget {gold(GOLD)} → {gold(GOLD - 27)}'])
    table(c, (644, 16, 1264, 668), selected=12)
    end_button(c, (644, 676, 1264, 784))
    HAUREIN['Troops'] = 50
    c.save('B-charges.png', 'B — One group per weekly charge (after a replenish)',
           'Troops (Replenish, Add New with its Add More steps as chips) and Talk with the unit\'s face; a spent group greys out, Maris reports in a plate.')


def c_rail():
    """A vertical rail of buttons at the left edge; the status page beside it, always open."""
    c = Canvas(); u = HAUREIN
    portrait(c, (16, 16, 636, 396), u)
    rl, al = action_lines(u)
    button(c, (16, 404, 216, 494), 'Replenish', rl, big=19)
    button(c, (16, 502, 216, 592), 'Add Troops', al, big=19, state='hover')
    button(c, (16, 600, 216, 690), 'Talk', 'with Haurein', big=19)
    c.rect((16, 698, 216, 784), outline=hx('55575c'))
    pips(c, 26, 712, 'Troops', True)
    pips(c, 26, 740, 'Talk', True)
    panel(c, (224, 404, 636, 784), 'Haurein · Leazas Soldiers')
    rows = [('Level', '0'), ('HP', '5/5'), ('Attack / Defense', '13 / 9'), ('Experience', '0/100'),
            ('Strategy', '5%'), ('Magic', '0'), ('Attacks', '6'), ('Troops', '50/1400'), ('Troop Atk / Def', '8 / 4'),
            ('On offense', '+0 / +0'), ('On defense', '+1 / +0'), ('In dungeons', '+0 / +0'), ('Item', '—')]
    for i, (k, v) in enumerate(rows):
        y = 436 + i * 26
        c.text((240, y), k, font(16), fill=MUTED)
        c.text((620, y), v, font(16), fill=WARN if k == 'Troops' else TEXT, anchor='ra')
        c.line((240, y + 22), (620, y + 22), fill=hx('3a3c41'))
    table(c, (644, 16, 1264, 668))
    end_button(c, (644, 676, 1264, 784))
    # the hovered button's tip: Add More's steps
    c.rect((222, 520, 636, 592), fill=hx('1f2023'), outline=ACCENT)
    c.text((234, 528), 'Add New Troops — a step\'s click buys it', font(14), fill=ACCENT)
    x = 234
    for i, (men, price) in enumerate(tiers(u, 3)):
        x += chip(c, x, 556, f'+{men} · {gold(price)}', on=i == 0 or price <= GOLD) + 6
    c.save('C-rail.png', 'C — Rail of buttons and the status page',
           'The buttons in a column at the left edge; beside them the status page (Info) always open; Add Troops opens its Add More steps beside it, a step\'s click buys it.')


def d_sheet():
    """A smaller portrait, the whole status page under it, the buttons as a strip along its foot; after a replenish."""
    c = Canvas(); HAUREIN['Troops'] = 1400; u = HAUREIN
    portrait(c, (16, 16, 420, 264), u)
    panel(c, (428, 16, 636, 264))
    c.text((440, 28), 'Haurein', cinzel(24, 700), fill=TEXT)
    c.text((440, 64), 'Leazas Soldiers', font(16), fill=MUTED)
    c.text((440, 120), 'This week', cinzel(14, 700), fill=ACCENT)
    pips(c, 440, 146, 'Troops', False)
    pips(c, 440, 174, 'Talk', True)
    panel(c, (16, 272, 636, 700), 'Status')
    cols = [[('Level', '0'), ('HP', '5/5'), ('Attack', '13'), ('Defense', '9'), ('EXP', '0/100'), ('Item', '—')],
            [('Strategy', '5%'), ('Magic', '0'), ('Attacks', '6'), ('Troops', '1400/1400'), ('Troop Atk', '8'),
             ('Troop Def', '4')],
            [('Offense Atk', '+0'), ('Offense Def', '+0'), ('Defense Atk', '+1'), ('Defense Def', '+0'),
             ('Dungeon Atk', '+0'), ('Dungeon Def', '+0')]]
    for ci, col in enumerate(cols):
        x = 28 + ci * 204
        for i, (k, v) in enumerate(col):
            y = 306 + i * 34
            c.text((x, y), k, font(16), fill=MUTED)
            c.text((x + 190, y), v, font(16), fill=GOOD if k == 'Troops' else TEXT, anchor='ra')
            c.line((x, y + 26), (x + 190, y + 26), fill=hx('3a3c41'))
    maris(c, (28, 524, 624, 610), ['Haurein\'s unit replenished by 1350 to 1400.', f'Budget {gold(GOLD)} → {gold(GOLD - 27)}'])
    c.text((28, 630), 'The status page (Info) is always open, so the menu has no Info.', font(14), fill=GREY)
    button(c, (16, 708, 216, 784), 'Replenish', 'troop order used', state='off')
    button(c, (224, 708, 428, 784), 'Add Troops', 'troop order used', state='off')
    button(c, (436, 708, 636, 784), 'Talk', '1 left this week')
    table(c, (644, 16, 1264, 668))
    end_button(c, (644, 676, 1264, 784))
    HAUREIN['Troops'] = 50
    c.save('D-sheet.png', 'D — Small portrait, full status page, strip of buttons (after a replenish)',
           'The portrait at two thirds, the week\'s charges beside it; the status page under it; the buttons a strip along the foot.')


def e_gauge():
    """The table across the whole top with every column; below it the portrait, the troop gauge and Maris's plate."""
    c = Canvas(); u = HAUREIN
    table(c, (16, 16, 1264, 360), columns=WIDE, scroll=False)
    portrait(c, (16, 368, 436, 625), u)
    panel(c, (16, 633, 436, 784))
    c.text((28, 643), 'Haurein', cinzel(22, 700), fill=TEXT)
    c.text((28, 678), 'HP 5/5   Lv 0   EXP 0/100', font(16), fill=MUTED)
    c.text((28, 704), 'Atk 13   Def 9   Act 6   Str 5%', font(16), fill=MUTED)
    c.text((28, 730), 'Item —', font(16), fill=MUTED)
    x0, x1 = 444, 1000
    panel(c, (x0, 368, x1, 784), 'Troops')
    pips(c, x0 + 110, 377, 'Order', True)
    bx0, bx1, by = x0 + 16, x1 - 16, 432
    total = u['MaxTroops'] + 100
    def px(v): return bx0 + (bx1 - bx0) * v / total
    c.rect((bx0, by, bx1, by + 34), fill=hx('1f2023'), outline=hx('55575c'))
    c.rect((bx0, by, px(u['Troops']), by + 34), fill=GOOD)
    c.overlay((px(u['Troops']), by, px(u['MaxTroops']), by + 34), hx('f2c75c', 70))
    c.rect((px(u['Troops']), by, px(u['MaxTroops']), by + 34), outline=ACCENT)
    c.rect((px(u['MaxTroops']), by, px(u['MaxTroops'] + 25), by + 34), fill=hx('6d8fc7'))
    c.text((bx0, by - 22), '50 now', font(14), fill=GOOD)
    c.text((px(u['MaxTroops']) - 4, by + 40), '1400 max', font(14), fill=TEXT, anchor='ra')
    c.text((bx1, by - 22), '+25 new', font(14), fill=hx('9fb8e0'), anchor='ra')
    button(c, (bx0, 500, bx0 + 250, 580), 'Replenish', '+1350 · 2700K', state='hover')
    c.text((bx0 + 270, 502), 'Add New Troops', font(17), fill=TEXT)
    x = bx0 + 270
    for i, (men, price) in enumerate(tiers(u, 3)):
        x += chip(c, x, 530, f'+{men}', on=i == 0, w=48) + 6
    c.text((bx0 + 270, 560), f'{gold(54)} for 25 men', font(14), fill=MUTED)
    button(c, (bx1 - 90, 520, bx1, 580), 'Add')
    face(c, bx0, 600, 96, u['FaceCg'])
    button(c, (bx0 + 110, 616, bx1, 680), 'Talk with Haurein', '1 left this week')
    c.text((bx0, 712), 'Replenish and Add share the troop order: one of them a week.', font(14), fill=GREY)
    c.rect((1008, 368, 1264, 668), fill=hx('24262a'), outline=ACCENT)
    face(c, 1072, 380, 128, 6)
    c.text((1136, 520), 'Maris', cinzel(16, 700), fill=ACCENT, anchor='ma')
    c.text((1022, 552), 'No orders yet', font(15), fill=MUTED)
    c.text((1022, 576), 'this week. Her reports', font(15), fill=MUTED)
    c.text((1022, 600), 'come here.', font(15), fill=MUTED)
    end_button(c, (1008, 676, 1264, 784))
    c.save('E-gauge.png', 'E — Table across the top, the troop gauge below',
           'Every column without a scrollbar; one bar shows troops (green), what Replenish fills (gold), what Add adds (blue); Maris\'s plate by End.')



def f_cards():
    """The table becomes a grid of unit cards; the left column keeps the portrait and the orders."""
    c = Canvas(); u = HAUREIN
    portrait(c, (16, 16, 636, 396), u)
    details(c, (16, 404, 636, 492), u)
    panel(c, (16, 500, 636, 784), 'Orders')
    rl, al = action_lines(u)
    button(c, (28, 534, 624, 594), 'Replenish', rl, align='left')
    button(c, (28, 602, 624, 662), 'Add New Troops', al, align='left')
    button(c, (28, 670, 624, 730), 'Talk', 'with Haurein', align='left')
    pips(c, 28, 744, 'Troops', True); pips(c, 330, 744, 'Talk', True)
    cw, ch = 200, 208
    for i, v in enumerate(data['units']):
        x, y = 648 + (i % 3) * (cw + 8), 16 + (i // 3) * (ch + 8)
        sel = v['Index'] == 12
        c.rect((x, y, x + cw, y + ch), fill=CARD, outline=ACCENT if sel else BORDER, width=2 if sel else 1)
        face(c, x + 8, y + 8, 96, v['FaceCg'], dim=v['Status'] != 1)
        tc = TEXT if v['Status'] == 1 else GREY
        c.text((x + 112, y + 12), v['Name'], font(18), fill=tc)
        c.text((x + 112, y + 38), f"Lv {v['Level']}", font(14), fill=MUTED)
        c.text((x + 112, y + 58), f"HP {v['HP']}/{v['MaxHP']}", font(14), fill=MUTED)
        if v['Status'] != 1:
            c.text((x + 112, y + 80), 'fought', font(14), fill=GREY)
        c.text((x + 10, y + 114), v['Type'], font(14), fill=MUTED)
        frac = v['Troops'] / v['MaxTroops']
        c.rect((x + 10, y + 140, x + cw - 10, y + 156), fill=hx('1f2023'))
        c.rect((x + 10, y + 140, x + 10 + (cw - 20) * frac, y + 156), fill=GOOD if v['Status'] == 1 else GREY)
        c.text((x + 10, y + 164), f"{v['Troops']}/{v['MaxTroops']}", font(15), fill=tc)
        r = refill(v)
        if r and v['Status'] == 1:
            c.text((x + cw - 10, y + 164), gold(r[1]), font(15), fill=ACCENT, anchor='ra')
    end_button(c, (644, 676, 1264, 784))
    c.save('F-cards.png', 'F — Cards instead of the table',
           'The units as cards with faces and a troop bar (refill price in gold); the left column as the deployment screen; buttons as wide rows.')


def g_face():
    """No status CG: the dialogue face and a dossier; the table taller; Maris's report under the orders."""
    c = Canvas(); u = HAUREIN
    panel(c, (16, 16, 636, 360))
    face(c, 28, 28, 220, u['FaceCg'])
    c.text((264, 32), 'Haurein', cinzel(28, 700), fill=TEXT)
    c.text((264, 72), 'Leazas Soldiers', font(17), fill=MUTED)
    rows = [('Troops', '50 / 1400'), ('Troop Atk / Def', '8 / 4'), ('HP', '5 / 5'), ('Level · EXP', '0 · 0/100'),
            ('Attack / Defense', '13 / 9'), ('Strategy', '5%'), ('Item', '—')]
    for i, (k, v) in enumerate(rows):
        y = 108 + i * 32
        c.text((264, y), k, font(16), fill=MUTED)
        c.text((620, y), v, font(17), fill=WARN if k == 'Troops' else TEXT, anchor='ra')
    c.text((28, 262), 'No description:', font(14), fill=GREY)
    c.text((28, 284), 'it exists only', font(14), fill=GREY)
    c.text((28, 306), 'in the status CG.', font(14), fill=GREY)
    rl, al = action_lines(u)
    button(c, (16, 368, 324, 448), 'Replenish', rl)
    button(c, (332, 368, 636, 448), 'Add New Troops', al)
    button(c, (16, 456, 636, 516), 'Talk with Haurein', '1 left this week')
    maris(c, (16, 524, 636, 610), ['Haurein\'s unit is short of 1350 men.', 'Replenishing costs 2700K; we have ' + gold(GOLD) + '.'],
          title='Maris advises')
    table(c, (644, 16, 1264, 784), row_h=40)
    end_button(c, (16, 618, 636, 784))
    c.save('G-face.png', 'G — Dossier with the dialogue face, the table full height',
           'The unit\'s face and stats instead of the status CG; the buttons under it, Maris advises (or reports) in our plate; End moves left.')


def h_inline():
    """The orders open as a drawer in the chosen row; the bottom left keeps the week's log."""
    c = Canvas(); u = HAUREIN
    kinkaid = unit(9); kept = kinkaid['Troops']; kinkaid['Troops'] = 1500
    portrait(c, (16, 16, 636, 396), u)
    details(c, (16, 404, 636, 492), u)
    def drawer(c, r):
        x0, y0, x1, y1 = r
        button(c, (x0, y0 + 4, x0 + 196, y1), 'Replenish', '+1350 · 2700K', big=17, state='off')
        button(c, (x0 + 204, y0 + 4, x0 + 400, y1), 'Add Troops', '+25 · 5400K', big=17, state='off')
        button(c, (x0 + 408, y0 + 4, x1, y1), 'Talk', '1 left', big=17)
    table(c, (644, 16, 1264, 668), drawer=(drawer, 66))
    panel(c, (16, 500, 636, 784), 'This week')
    pips(c, 140, 509, 'Troops', False)
    pips(c, 400, 509, 'Talk', True)
    maris(c, (28, 540, 624, 626), ['Kinkaid\'s Leazas Soldiers replenished by 714 to 1500.',
                                   f'Budget {gold(GOLD)} → {gold(GOLD - 14)}'])
    c.text((28, 640), 'The week\'s reports stack here, the newest on top.', font(14), fill=GREY)
    c.text((28, 662), 'The table has no Talk or Troops column: the drawer says what is left.', font(14), fill=GREY)
    end_button(c, (644, 676, 1264, 784))
    kinkaid['Troops'] = kept
    c.save('H-inline.png', 'H — Orders inside the chosen row (troop order already used)',
           'A click on a row opens its buttons under it in the table; the bottom left shows the week\'s charges and Maris\'s reports as a log.')


def i_ledger():
    """The orders as the lines of an account, the price and the budget after it on each line."""
    c = Canvas(); u = HAUREIN
    portrait(c, (16, 16, 636, 396), u)
    details(c, (16, 404, 636, 470), u, rows=2)
    panel(c, (16, 478, 636, 784), 'Orders for Haurein')
    c.text((620, 487), f'Budget {gold(GOLD)}', font(15), fill=TEXT, anchor='ra')
    y = 518
    def line(label, sub, price, btn, state='normal'):
        nonlocal y
        c.text((32, y + 8), label, font(18), fill=TEXT if state != 'off' else GREY)
        c.text((32, y + 34), sub, font(14), fill=MUTED)
        if price is not None:
            c.text((440, y + 8), gold(price), font(18), fill=ACCENT, anchor='ra')
            c.text((440, y + 34), '→ ' + gold(GOLD - price), font(14), fill=MUTED, anchor='ra')
        button(c, (460, y + 4, 620, y + 52), btn, state=state, big=18)
        c.line((28, y + 62), (624, y + 62), fill=hx('3a3c41'))
        y += 68
    line('Replenish troops', '1350 short of 1400', 27, 'Replenish')
    line('New troops: +25 men', 'Add More: +50 for 16200K', 54, 'Add')
    chip(c, 250, y - 62, '−  ×1  +')
    line('Talk', 'Haurein\'s talk scene', None, 'Talk')
    c.text((32, y + 6), 'Replenish and New troops share one troop order a week.', font(14), fill=GREY)
    table(c, (644, 16, 1264, 668))
    end_button(c, (644, 676, 1264, 784))
    c.save('I-ledger.png', 'I — The orders as an account',
           'Each order a line: what it does, its price, the budget after it, its button; the Add More step as a small −/+ on its line.')


def j_tabs():
    """A tabbed panel over the whole left column: Orders / Info, the portrait smaller beside the tabs."""
    c = Canvas(); u = HAUREIN
    portrait(c, (16, 16, 636, 396), u)
    tabs = [('Troops', True), ('Talk', False), ('Info', False)]
    x = 16
    for name, on in tabs:
        w = 130
        c.rect((x, 404, x + w, 440), fill=BACK if on else BUTTON_OFF, outline=BORDER)
        c.text((x + w / 2, 422), name, font(17), fill=ACCENT if on else MUTED, anchor='mm')
        x += w + 4
    c.rect((16, 439, 636, 784), fill=BACK, outline=BORDER)
    c.rect((17, 438, 145, 441), fill=BACK)
    pips(c, 420, 414, 'Order', True)
    for i, (title, big, lines, st) in enumerate([
            ('Replenish', '+1350', ['to 1400 troops', 'cost 2700K'], 'normal'),
            ('Add New Troops', '+25', ['to 1425 max', 'cost 5400K'], 'hover')]):
        x0 = 32 + i * 304
        c.rect((x0, 456, x0 + 288, 700), fill=CARD, outline=ACCENT if st == 'hover' else BORDER, width=2 if st == 'hover' else 1)
        c.text((x0 + 16, 468), title, cinzel(18, 700), fill=TEXT)
        c.text((x0 + 16, 500), big, font(44), fill=GOOD if i == 0 else hx('9fb8e0'))
        for k, s in enumerate(lines):
            c.text((x0 + 16, 566 + k * 24), s, font(16), fill=MUTED)
        if i == 1:
            xx = x0 + 16
            for (men, price) in tiers(u, 3):
                xx += chip(c, xx, 622, f'+{men}', on=men == 25, w=56) + 6
        button(c, (x0 + 16, 650, x0 + 272, 690), 'Replenish' if i == 0 else 'Add', big=17)
    c.text((32, 716), 'Talk: its own tab — the unit\'s face, the line Rance thinks, and the button.', font(14), fill=GREY)
    c.text((32, 738), 'Info: the status page (level, attacks, modifiers, item).', font(14), fill=GREY)
    table(c, (644, 16, 1264, 668))
    end_button(c, (644, 676, 1264, 784))
    c.save('J-tabs.png', 'J — Tabs: Troops / Talk / Info',
           'The left column\'s lower half as tabs; Troops shows Replenish and Add New as two cards with their effect in large figures.')



# ---------------------------------------------------------------- round 2: from layout E (owner, 2026-10-18)
# The table across the top with every column (the item too); no details under the portrait, no Maris, no gauge,
# Talk not full width, no hint that the troop order is shared.

TOP = (16, 16, 1264, 360)


def budget(c, xy, anchor='la', spent=0):
    c.text(xy, f'Budget {gold(GOLD - spent)}', font(16), fill=MUTED, anchor=anchor)


def steps(c, x, y, u, chosen=0, w=None, on=True):
    """Add More's steps: +men, the price under; a step dearer than the budget is greyed."""
    for i, (men, price) in enumerate(tiers(u)):
        ok = on and price <= GOLD
        sel = on and i == chosen
        bw = w or 120
        c.rect((x, y, x + bw, y + 48), fill=hx('4a4231') if sel else (BUTTON if ok else BUTTON_OFF),
               outline=ACCENT if sel else (BUTTON_BORDER if ok else hx('55575c')), width=2 if sel else 1)
        c.text((x + bw / 2, y + 15), f'+{men}', font(17), fill=ACCENT if sel else (TEXT if ok else GREY), anchor='mm')
        c.text((x + bw / 2, y + 35), gold(price), font(13), fill=MUTED if ok else GREY, anchor='mm')
        x += bw + 6


def toast(c, r, lines):
    """What was done: a plate that fades after a few seconds (not a message window)."""
    c.overlay(r, hx('17181b', 235), radius=6)
    c.rect(r, outline=GOOD, width=1, radius=6)
    for i, s in enumerate(lines):
        c.text((r[0] + 16, r[1] + 12 + i * 24), s, font(17 if i == 0 else 15), fill=TEXT if i == 0 else MUTED)


def talk_button(c, r, u, state='normal', sub=None):
    x0, y0, x1, y1 = r
    fill = {'normal': BUTTON, 'hover': BUTTON_HOVER, 'off': BUTTON_OFF}[state]
    c.rect(r, fill=fill, outline=BUTTON_BORDER if state != 'off' else hx('55575c'), width=1)
    size = y1 - y0 - 12
    face(c, x0 + 6, y0 + 6, size, u['FaceCg'], dim=state == 'off')
    tc = BUTTON_TEXT if state != 'off' else GREY
    tx = (x0 + size + 12 + x1) / 2
    c.text((tx, y0 + (y1 - y0) * (0.4 if sub else 0.5)), 'Talk', font(20), fill=tc, anchor='mm')
    if sub:
        c.text((tx, y0 + (y1 - y0) * 0.72), sub, font(14), fill=MUTED if state != 'off' else GREY, anchor='mm')


def e1_quad():
    """Big portrait left; Replenish and Add side by side, Add More's steps under Add, Talk compact, End below."""
    c = Canvas(); u = HAUREIN
    table(c, TOP, columns=WIDE, scroll=False)
    portrait(c, (16, 368, 696, 784), u)
    panel(c, (704, 368, 1264, 676))
    budget(c, (1250, 380), 'ra')
    button(c, (716, 404, 980, 504), 'Replenish', '+1350 troops · 2700K', state='hover')
    button(c, (988, 404, 1252, 504), 'Add New Troops', '+25 troops · 5400K')
    steps(c, 988, 512, u, w=62)
    talk_button(c, (716, 584, 900, 664), u)
    end_button(c, (704, 684, 1264, 784))
    c.save('E1-quad.png', 'E1 — Replenish and Add side by side, Talk compact',
           'The portrait as large as the space allows; Add More\'s steps as small buttons under Add (a step dearer than the budget greyed).')


def e2_tiles():
    """Three equal tiles in a row right of the portrait, each its effect in large figures; End below them."""
    c = Canvas(); u = HAUREIN
    table(c, TOP, columns=WIDE, scroll=False)
    portrait(c, (16, 368, 696, 784), u)
    budget(c, (1264, 370), 'ra')
    tiles = [('Replenish', '+1350', 'to 1400 · 2700K', GOOD), ('Add Troops', '+25', 'max 1425 · 5400K', hx('9fb8e0'))]
    for i, (title, big, sub, col) in enumerate(tiles):
        x0 = 704 + i * 190
        st = 'hover' if i == 0 else 'normal'
        c.rect((x0, 396, x0 + 182, 640), fill=BUTTON_HOVER if st == 'hover' else BUTTON, outline=BUTTON_BORDER,
               width=2 if st == 'hover' else 1)
        c.text((x0 + 91, 424), title, font(19), fill=BUTTON_TEXT, anchor='mm')
        c.text((x0 + 91, 490), big, font(44), fill=col, anchor='mm')
        c.text((x0 + 91, 540), sub, font(13), fill=MUTED, anchor='mm')
        if i == 1:
            for k, (men, price) in enumerate(tiers(u, 3)):
                chip(c, x0 + 8 + k * 57, 572, f'+{men}', on=k == 0, w=52)
    x0 = 704 + 2 * 190
    c.rect((x0, 396, 1264, 640), fill=BUTTON, outline=BUTTON_BORDER)
    face(c, x0 + 26, 410, 128, u['FaceCg'])
    c.text((x0 + 90, 580), 'Talk', font(22), fill=BUTTON_TEXT, anchor='mm')
    end_button(c, (704, 648, 1264, 784))
    c.save('E2-tiles.png', 'E2 — Three tiles',
           'Replenish, Add Troops and Talk as three equal tiles; the figures say what a click gives; Add More\'s steps as chips in its tile.')


def e3_column():
    """The portrait at its own size, a column of orders beside it, End a tall button at the right."""
    c = Canvas(); u = HAUREIN
    table(c, TOP, columns=WIDE, scroll=False)
    portrait(c, (16, 376, 632, 756), u)
    panel(c, (640, 368, 1056, 784))
    budget(c, (652, 380))
    button(c, (652, 410, 1044, 486), 'Replenish', '+1350 troops · 2700K', align='left')
    button(c, (652, 494, 1044, 570), 'Add New Troops', '+25 troops · 5400K', align='left', state='hover')
    steps(c, 652, 578, u, w=93)
    talk_button(c, (652, 688, 900, 772), u)
    button(c, (1064, 368, 1264, 784), 'End', big=34)
    c.save('E3-column.png', 'E3 — A column of orders, End tall at the right',
           'The portrait at the original\'s size; the orders as wide rows with their effect, Add More\'s steps a row of their own; Talk half width.')


def e4_segmented():
    """Add More as a segmented choice that sets what one Add button buys."""
    c = Canvas(); u = HAUREIN
    table(c, TOP, columns=WIDE, scroll=False)
    portrait(c, (16, 368, 696, 784), u)
    panel(c, (704, 368, 1264, 676))
    budget(c, (1250, 380), 'ra')
    c.text((716, 380), 'Troops', cinzel(15, 700), fill=ACCENT)
    button(c, (716, 408, 1252, 468), 'Replenish to 1400', '+1350 troops · 2700K')
    x = 716
    for i, (men, price) in enumerate(tiers(u)):
        ok, sel = price <= GOLD, i == 1
        c.rect((x, 486, x + 100, 520), fill=hx('4a4231') if sel else (BUTTON if ok else BUTTON_OFF),
               outline=ACCENT if sel else hx('55575c'))
        c.text((x + 50, 503), f'+{men}', font(16), fill=ACCENT if sel else (TEXT if ok else GREY), anchor='mm')
        x += 100
    button(c, (1132, 478, 1252, 528), 'Add', big=19)
    c.text((716, 530), 'Add 50 new troops for 16200K', font(15), fill=MUTED)
    talk_button(c, (716, 576, 940, 664), u)
    end_button(c, (704, 684, 1264, 784))
    c.save('E4-segmented.png', 'E4 — Add More as a segmented choice',
           'Replenish a wide button; Add\'s size chosen in a segmented bar (+25 / +50 / +100 / +200), one Add button buys it; Talk compact.')


def e5_left():
    """The orders on the left (the owner's corner), the portrait on the right."""
    c = Canvas(); u = HAUREIN
    table(c, TOP, columns=WIDE, scroll=False)
    panel(c, (16, 368, 576, 676))
    budget(c, (562, 380), 'ra')
    button(c, (28, 404, 292, 504), 'Replenish', '+1350 troops · 2700K')
    button(c, (300, 404, 564, 504), 'Add New Troops', '+25 troops · 5400K', state='hover')
    steps(c, 300, 512, u, w=62)
    talk_button(c, (28, 584, 212, 664), u)
    end_button(c, (16, 684, 576, 784))
    portrait(c, (584, 368, 1264, 784), u)
    c.save('E5-left.png', 'E5 — Orders bottom left, portrait bottom right',
           'E1 mirrored: the buttons in the bottom left corner, End under them, the portrait to the right.')


def e6_used():
    """E1 after a replenish: the troop orders greyed, a plate says what was done."""
    c = Canvas(); HAUREIN['Troops'] = 1400; u = HAUREIN
    table(c, TOP, columns=WIDE, scroll=False)
    portrait(c, (16, 368, 696, 784), u)
    panel(c, (704, 368, 1264, 676))
    budget(c, (1250, 380), 'ra', spent=27)
    button(c, (716, 404, 980, 504), 'Replenish', 'used this week', state='off')
    button(c, (988, 404, 1252, 504), 'Add New Troops', 'used this week', state='off')
    steps(c, 988, 512, u, w=62, on=False)
    talk_button(c, (716, 584, 900, 664), u, state='hover')
    end_button(c, (704, 684, 1264, 784))
    toast(c, (908, 584, 1252, 664), ['Replenished: +1350 → 1400', 'Budget −2700K'])
    HAUREIN['Troops'] = 50
    c.save('E6-used.png', 'E6 — E1 after a replenish (a state, not a layout)',
           'Both troop buttons greyed for the week ("used this week"); what was done on a plate beside Talk, gone after a few seconds.')


def e7_fought():
    """E3 with a unit that fought this week: Cﾌﾞｶ lets it only be looked at."""
    c = Canvas(); u = unit(7)
    table(c, TOP, selected=7, columns=WIDE, scroll=False)
    portrait(c, (16, 376, 632, 756), u)
    panel(c, (640, 368, 1056, 784))
    budget(c, (652, 380))
    button(c, (652, 410, 1044, 486), 'Replenish', 'deployed this week', align='left', state='off')
    button(c, (652, 494, 1044, 570), 'Add New Troops', 'deployed this week', align='left', state='off')
    steps(c, 652, 578, u, w=93, on=False)
    talk_button(c, (652, 688, 900, 772), u, state='off', sub='deployed this week')
    button(c, (1064, 368, 1264, 784), 'End', big=34)
    c.save('E7-fought.png', 'E7 — E3 with Rick (unit 7), deployed this week (a state)',
           'In the original such a unit only shows its status page: here every order is greyed and says why.')




# ---------------------------------------------------------------- round 3: from E1 (owner, 2026-10-18)
# "Add New Troops" is a label, its steps are the buttons; End belongs to the orders' panel, small, bottom right,
# on a row of its own under Talk.

PANEL = (704, 368, 1264, 784)


def step_buttons(c, x, y, u, w, h=56, gap=8, per_row=4, state=None, hover=None):
    """The Add More steps as the buttons themselves: +men, the price under; dearer than the budget greyed."""
    for i, (men, price) in enumerate(tiers(u)):
        st = state or ('off' if price > GOLD else ('hover' if i == hover else 'normal'))
        bx, by = x + (i % per_row) * (w + gap), y + (i // per_row) * (h + gap)
        button(c, (bx, by, bx + w, by + h), f'+{men}', gold(price), state=st, big=19)


def add_label(c, x, y, u, used=False):
    c.text((x, y), 'Add New Troops', font(17), fill=GREY if used else TEXT)


def small_end(c, r):
    button(c, r, 'End', big=20)


def f1_rows(used=False):
    """Rows: Replenish; the Add label over its four step buttons; Talk; End small at the bottom right."""
    c = Canvas()
    if used:
        HAUREIN['Troops'] = 1400
    u = HAUREIN
    table(c, TOP, columns=WIDE, scroll=False)
    portrait(c, (16, 368, 696, 784), u)
    panel(c, PANEL)
    budget(c, (1250, 380), 'ra', spent=27 if used else 0)
    if used:
        button(c, (716, 404, 1252, 470), 'Replenish', 'used this week', state='off')
    else:
        button(c, (716, 404, 1252, 470), 'Replenish', '+1350 troops · 2700K')
    add_label(c, 716, 486, u, used)
    step_buttons(c, 716, 514, u, w=128, state='off' if used else None, hover=None if used else 0)
    talk_button(c, (716, 596, 936, 680), u)
    if used:
        toast(c, (948, 596, 1252, 680), ['Replenished: +1350 → 1400', 'Budget −2700K'])
    small_end(c, (1112, 720, 1252, 772))
    if used:
        HAUREIN['Troops'] = 50
        c.save('F4-rows-used.png', 'F4 — F1 after a replenish (a state)',
               'The troop orders greyed for the week; what was done on a plate beside Talk, gone after a few seconds.')
    else:
        c.save('F1-rows.png', 'F1 — Rows: Replenish, Add New Troops\' steps, Talk, End',
               '"Add New Troops" a label; its steps +25 / +50 / +100 / +200 the buttons (the price under each); End small at the bottom right.')


def f2_columns():
    """Two columns: Replenish over the steps (two by two) on the left, Talk on the right; End under Talk."""
    c = Canvas(); u = HAUREIN
    table(c, TOP, columns=WIDE, scroll=False)
    portrait(c, (16, 368, 696, 784), u)
    panel(c, PANEL)
    budget(c, (716, 380))
    button(c, (716, 410, 1000, 490), 'Replenish', '+1350 troops · 2700K')
    add_label(c, 716, 508, u)
    step_buttons(c, 716, 536, u, w=138, h=60, per_row=2, hover=1)
    talk_button(c, (1016, 410, 1252, 490), u)
    small_end(c, (1112, 720, 1252, 772))
    c.save('F2-columns.png', 'F2 — Two columns, the steps two by two',
           'Replenish and Add New Troops\' steps on the left, Talk at the top right; End small at the bottom right, under Talk.')


def f3_line():
    """Each order on one line: its name at the left, its buttons at the right; Talk and End on the last lines."""
    c = Canvas(); u = HAUREIN
    table(c, TOP, columns=WIDE, scroll=False)
    portrait(c, (16, 368, 696, 784), u)
    panel(c, PANEL)
    budget(c, (1250, 380), 'ra')
    c.text((716, 424), 'Replenish', font(17), fill=TEXT)
    c.text((716, 448), '1350 short of 1400', font(14), fill=MUTED)
    button(c, (956, 410, 1252, 470), '+1350', '2700K', big=19)
    c.line((716, 484), (1252, 484), fill=hx('3a3c41'))
    add_label(c, 716, 498, u)
    step_buttons(c, 716, 526, u, w=128, hover=0)
    c.line((716, 596), (1252, 596), fill=hx('3a3c41'))
    talk_button(c, (716, 610, 936, 690), u)
    small_end(c, (1112, 720, 1252, 772))
    c.save('F3-line.png', 'F3 — Replenish as a figure too',
           'Replenish\'s button is its figure (+1350 · 2700K) like the steps, its name a label; the orders on lines, Talk and End below.')


# ---------------------------------------------------------------- round 4: F1 as a game's screen (2026-10-18)
# The layout the owner approved stays; the look moves from the steel boxes to the lacquer and gold of our map
# panels (godot/Strategy/Lacquer.cs). Drawn over the real screen's dumps: the picture panel (the status CG in the
# original's gold frame) is cut from a dump of our screen, the map from a dump of the map wait.
#   python -I tools/ui/mock_subordinates.py <units.json> <cg_png> <out> g <subordinates dump> <done dump> <map dump>
# The dumps are the Godot window (1280x800 with the menu bar); the game's area is scaled back to 1280x800.

LQ_PANEL, LQ_GOLD, LQ_DIM = (22, 16, 12, 245), (196, 160, 86, 255), (110, 86, 46, 255)
LQ_NAME, LQ_SUB, LQ_LABEL = (232, 199, 122, 255), (160, 140, 104, 255), (150, 134, 108, 255)
LQ_VALUE, LQ_ROW, LQ_ROW2 = (236, 226, 204, 255), (34, 26, 20, 255), (28, 21, 16, 255)
LQ_HOVER, LQ_HOVER_LINE, LQ_TEXT = (64, 48, 28, 255), (222, 186, 104, 255), (244, 232, 206, 255)
LQ_QUIET, LQ_OFF_TEXT, LQ_OFF = (176, 160, 130, 255), (98, 88, 74, 255), (40, 35, 30, 255)
LQ_GREEN, LQ_RED = (150, 196, 110, 255), (222, 110, 90, 255)
SHOJUMARU = REPO + '/godot/fonts/Shojumaru-Regular.ttf'
G_TABLE, G_PORTRAIT, G_PANEL = (16, 16, 1264, 360), (16, 368, 682, 784), (690, 368, 1264, 784)


def brush(size):
    return font(size, SHOJUMARU)


def game_area(path):
    """The game's 640x400 area of a window dump (under the 24-px menu bar, centred), at 1280x800."""
    im = Image.open(path).convert('RGBA')
    k = (im.height - 24) / 400
    left = (im.width - 640 * k) / 2
    return im.crop((round(left), 24, round(left + 640 * k), im.height)).resize((W * S, H * S), Image.LANCZOS)


def g_setup():
    """The table as the week-18 save shows it on our screen; Barres (unit 6) and Rick (unit 7) deployed for the mock."""
    troops = {1: 1000, 5: 800, 6: 980, 7: 612, 8: 1000, 9: 901, 10: 649, 11: 100, 12: 209}
    for u in data['units']:
        u['Troops'] = troops[u['Index']]
        u['Status'] = 2 if u['Index'] in (6, 7) else 1
    global GOLD
    GOLD = 1164


def lq_panel(c, r, fill=LQ_PANEL, shadow=True):
    """Lacquer.DrawPanel: the shadow, the lacquer, the double gold frame and its corners."""
    x0, y0, x1, y1 = r
    if shadow:
        for i in range(10, 0, -1):
            c.overlay((x0 - i + 3, y0 - i + 8, x1 + i + 3, y1 + i + 8), (0, 0, 0, 14))
    c.overlay(r, fill)
    c.rect((x0 + 1, y0 + 1, x1 - 1, y1 - 1), outline=LQ_GOLD, width=2)
    c.rect((x0 + 5, y0 + 5, x1 - 5, y1 - 5), outline=LQ_DIM, width=1)
    for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        c.d.line([((cx + sx * 9) * S, (cy + sy * 2) * S), ((cx + sx * 9) * S, (cy + sy * 9) * S),
                  ((cx + sx * 2) * S, (cy + sy * 9) * S)], fill=LQ_GOLD, width=2 * S)
        c.d.polygon([((cx + sx * 9) * S, (cy + sy * 14) * S), ((cx + sx * 14) * S, (cy + sy * 9) * S),
                     ((cx + sx * 9) * S, (cy + sy * 4) * S), ((cx + sx * 4) * S, (cy + sy * 9) * S)], fill=LQ_GOLD)


def lq_divider(c, x0, x1, y):
    m = (x0 + x1) / 2
    c.line((x0, y), (m - 10, y), fill=LQ_DIM)
    c.line((m + 10, y), (x1, y), fill=LQ_DIM)
    c.d.polygon([(m * S, (y - 4) * S), ((m + 4) * S, y * S), (m * S, (y + 4) * S), ((m - 4) * S, y * S)], fill=LQ_GOLD)


def lq_diamond(c, x, y, r, fill):
    c.d.polygon([(x * S, (y - r) * S), ((x + r) * S, y * S), (x * S, (y + r) * S), ((x - r) * S, y * S)], fill=fill)


def seigaiha(c, colour=(196, 160, 86, 13), radius=34):
    """The backdrop's wave pattern, faint gold on the dark lacquer."""
    layer = Image.new('RGBA', c.im.size, (0, 0, 0, 0))
    dd = ImageDraw.Draw(layer)
    rs, step = radius * S, radius * S
    for row in range(-1, H * S // (step // 2) + 3):
        y = row * step // 2
        off = 0 if row % 2 == 0 else step
        for x in range(-2 * step + off, W * S + 2 * step, 2 * step):
            for k in (1.0, 0.72, 0.44):
                rr = rs * k
                dd.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(14, 10, 8, 255) if k == 1.0 else None,
                           outline=colour, width=S)
    c.im = Image.alpha_composite(c.im, layer)
    c.d = ImageDraw.Draw(c.im)


def vignette(c, strength=170):
    mask = Image.new('L', (W, H), 0)
    md = ImageDraw.Draw(mask)
    for i in range(60):
        a = int(strength * (1 - i / 60) ** 2)
        md.rectangle([i * 6, i * 4, W - 1 - i * 6, H - 1 - i * 4], outline=a, width=6)
    dark = Image.new('RGBA', (W, H), (0, 0, 0, 255))
    dark.putalpha(mask)
    c.im = Image.alpha_composite(c.im, dark.resize(c.im.size, Image.LANCZOS))
    c.d = ImageDraw.Draw(c.im)


def backdrop(c, kind, map_dump):
    if kind == 'map':
        from PIL import ImageFilter, ImageEnhance
        m = game_area(map_dump).filter(ImageFilter.GaussianBlur(5 * S))
        m = ImageEnhance.Brightness(ImageEnhance.Color(m).enhance(0.6)).enhance(0.5)
        c.im.paste(m, (0, 0))
        c.d = ImageDraw.Draw(c.im)
        vignette(c, 150)
    else:
        c.rect((0, 0, W, H), fill=(14, 10, 8, 255))
        seigaiha(c)
        vignette(c, 200)


def picture(c, dump):
    """The picture panel cut from our screen's dump: the CG in the original's gold frame, with a shadow under it."""
    x0, y0, x1, y1 = G_PORTRAIT
    for i in range(10, 0, -1):
        c.overlay((x0 - i + 3, y0 - i + 8, x1 + i + 3, y1 + i + 8), (0, 0, 0, 16))
    area = game_area(dump)
    c.im.paste(area.crop((x0 * S, y0 * S, x1 * S, y1 * S)), (x0 * S, y0 * S))
    c.d = ImageDraw.Draw(c.im)


G_COLUMNS = [('', 22), ('No.', 38), ('Name', 100), ('Type', 150), ('Troops', 112), ('Refill', 76), ('T.Atk', 60),
             ('T.Def', 60), ('Str%', 56), ('HP', 56), ('Lv', 36), ('EXP', 80), ('Atk', 48), ('Def', 48), ('Act', 48),
             ('Mag', 48), ('Item', 150)]


def troop_bar(c, x, y, w, u, ink=False):
    f = u['Troops'] / u['MaxTroops']
    colour = LQ_GREEN if f >= 0.75 else ((214, 170, 80, 255) if f >= 0.35 else LQ_RED)
    if ink:
        colour = (70, 110, 50, 255) if f >= 0.75 else ((160, 110, 30, 255) if f >= 0.35 else (170, 50, 36, 255))
    c.rect((x, y, x + w, y + 3), fill=(0, 0, 0, 90) if not ink else (120, 100, 70, 90))
    c.rect((x, y, x + max(1, round(w * f)), y + 3), fill=colour)


def g_table(c, selected, style='lacquer'):
    """The table in a lacquer panel: a title band, the headers in Cinzel, banded rows, the chosen one lit."""
    x0, y0, x1, y1 = G_TABLE
    lq_panel(c, G_TABLE)
    ledger = style == 'ledger'
    if style == 'vermilion':
        red_band(c, (x0 + 6, y0 + 6, x1 - 6, y0 + 44))
    c.text((x0 + 22, y0 + 25), 'Subordinates', brush(24), fill=LQ_NAME, anchor='lm')
    c.text((x0 + 22 + c.width('Subordinates', brush(24)) + 16, y0 + 27), f'{sum(u["Status"] == 1 for u in data["units"])} units ready',
           cinzel(13, 600), fill=LQ_SUB, anchor='lm')
    c.text((x1 - 22, y0 + 27), 'LP 3 · Month 5 · Week 2', cinzel(15, 700), fill=LQ_NAME, anchor='rm')
    lq_divider(c, x0 + 16, x1 - 16, y0 + 44)
    ix0, ix1 = x0 + 14, x1 - 14
    top = y0 + 50
    if ledger:
        c.rect((ix0, top, ix1, y1 - 12), fill=(224, 208, 172, 255), outline=LQ_DIM)
    head_fill = (44, 33, 24, 255) if not ledger else (196, 176, 136, 255)
    if style == 'vermilion':
        head_fill = (58, 22, 16, 255)
    c.rect((ix0, top, ix1, top + 26), fill=head_fill)
    head_ink = LQ_LABEL if not ledger else (120, 40, 26, 255)
    x = ix0 + 4
    for name, w in G_COLUMNS:
        right = name not in ('Name', 'Type', 'Item', '')
        if right:
            c.text((x + w - 6, top + 13), name, cinzel(13, 700), fill=head_ink, anchor='rm')
        elif name:
            c.text((x + (16 if name == 'Item' else 2), top + 13), name, cinzel(13, 700), fill=head_ink, anchor='lm')
        x += w
    c.line((ix0, top + 26), (ix1, top + 26), fill=LQ_GOLD if not ledger else (120, 40, 26, 255))
    y, row_h = top + 28, 28
    for i, u in enumerate(data['units']):
        ready = u['Status'] == 1
        chosen = u['Index'] == selected
        if ledger:
            if chosen:
                c.overlay((ix0 + 1, y, ix1 - 1, y + row_h), (190, 60, 40, 60))
                c.rect((ix0 + 1, y, ix0 + 5, y + row_h), fill=(170, 40, 28, 255))
            c.line((ix0 + 6, y + row_h), (ix1 - 6, y + row_h), fill=(150, 120, 80, 110))
        else:
            c.rect((ix0, y, ix1, y + row_h), fill=LQ_HOVER if chosen else (LQ_ROW if i % 2 == 0 else LQ_ROW2))
            if chosen:
                c.line((ix0, y), (ix1, y), fill=LQ_HOVER_LINE, width=1)
                c.line((ix0, y + row_h - 1), (ix1, y + row_h - 1), fill=LQ_HOVER_LINE, width=1)
        if ledger:
            ink, quiet, num = ((40, 28, 18, 255), (100, 80, 56, 255), (40, 28, 18, 255)) if ready else \
                ((150, 132, 104, 255),) * 3
        else:
            ink, quiet, num = (LQ_TEXT, LQ_QUIET, LQ_VALUE) if ready else (LQ_OFF_TEXT,) * 3
        if chosen and not ledger:
            ink = LQ_NAME
        x = ix0 + 4
        values = cells(u)
        values['No.'] = values['№']
        mid = y + row_h / 2
        for name, w in G_COLUMNS:
            v = values[name]
            if name == '':
                if chosen:
                    lq_diamond(c, x + w / 2 + 2, mid, 5, LQ_GOLD if not ledger else (170, 40, 28, 255))
                elif not ready:
                    c.text((x + w / 2 + 2, mid), '—', font(12), fill=quiet, anchor='mm')
            elif name == 'Troops':
                c.text((x + w - 6, mid - 4), v, font(15), fill=num, anchor='rm')
                troop_bar(c, x + 10, y + row_h - 6, w - 16, u, ledger)
            elif name == 'Refill' and v != '—' and ready:
                c.text((x + w - 6, mid), v, font(15), fill=LQ_NAME if not ledger else (150, 52, 30, 255), anchor='rm')
            elif name in ('Name', 'Type', 'Item'):
                c.text((x + (16 if name == 'Item' else 2), mid), v, font(16 if name == 'Name' else 14),
                       fill=ink if name == 'Name' else quiet, anchor='lm')
            else:
                c.text((x + w - 6, mid), v, font(15), fill=num, anchor='rm')
            x += w
        y += row_h


def lq_button(c, r, title, sub=None, kind='normal', title_font=None, sub_font=None):
    """Our buttons: 'primary' red lacquer (as Turn END), 'normal' a dark row box, 'hover' lit, 'off' greyed."""
    x0, y0, x1, y1 = r
    if kind == 'primary' or kind == 'primary-hover':
        top, bottom = ((150, 40, 30), (96, 20, 14)) if kind == 'primary-hover' else ((128, 32, 24), (80, 16, 12))
        for i in range(y1 - y0):
            t = i / max(1, y1 - y0 - 1)
            c.line((x0, y0 + i), (x1, y0 + i), fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,))
        c.rect(r, outline=LQ_GOLD, width=2)
        c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(150, 96, 52, 255), width=1)
        tc, sc = LQ_NAME, (236, 206, 160, 255)
    elif kind == 'off':
        c.rect(r, fill=LQ_OFF, outline=(66, 58, 48, 255), width=1)
        tc, sc = LQ_OFF_TEXT, LQ_OFF_TEXT
    else:
        hover = kind == 'hover'
        c.rect(r, fill=LQ_HOVER if hover else LQ_ROW, outline=LQ_HOVER_LINE if hover else LQ_DIM, width=2 if hover else 1)
        tc, sc = (LQ_NAME if hover else LQ_TEXT), LQ_QUIET
    cx = (x0 + x1) / 2
    tf, sf = title_font or brush(20), sub_font or font(14)
    if sub:
        c.text((cx, y0 + (y1 - y0) * 0.40), title, tf, fill=tc, anchor='mm')
        c.text((cx, y0 + (y1 - y0) * 0.76), sub, sf, fill=sc, anchor='mm')
    else:
        c.text((cx, (y0 + y1) / 2), title, tf, fill=tc, anchor='mm')


def g_budget(c, spent=0):
    x1, y = G_PANEL[2] - 18, G_PANEL[1] + 24
    f = cinzel(20, 700)
    c.text((x1, y), 'K GOLD', cinzel(11, 600), fill=LQ_SUB, anchor='rm')
    w = c.width('K GOLD', cinzel(11, 600)) + 6
    c.text((x1 - w, y), f'{(GOLD - spent) * 100:,}', f, fill=LQ_NAME, anchor='rm')
    c.text((G_PANEL[0] + 18, y), 'Orders', brush(18), fill=LQ_NAME, anchor='lm')
    c.text((x1 - w - c.width(f'{(GOLD - spent) * 100:,}', f) - 10, y + 1), 'Budget', cinzel(12, 600), fill=LQ_LABEL, anchor='rm')


def g_talk(c, r, u, kind='normal'):
    x0, y0, x1, y1 = r
    if kind == 'off':
        c.rect(r, fill=LQ_OFF, outline=(66, 58, 48, 255))
    else:
        hover = kind == 'hover'
        c.rect(r, fill=LQ_HOVER if hover else LQ_ROW, outline=LQ_HOVER_LINE if hover else LQ_DIM, width=2 if hover else 1)
    size = y1 - y0 - 12
    face(c, x0 + 6, y0 + 6, size, u['FaceCg'], dim=kind == 'off')
    c.rect((x0 + 6, y0 + 6, x0 + 6 + size, y0 + 6 + size), outline=LQ_GOLD if kind != 'off' else (80, 70, 58, 255), width=1)
    tx = (x0 + size + 12 + x1) / 2
    c.text((tx, y0 + (y1 - y0) * 0.42), 'Talk', brush(20), fill=LQ_TEXT if kind != 'off' else LQ_OFF_TEXT, anchor='mm')
    c.text((tx, y0 + (y1 - y0) * 0.74), f"with {u['Name']}", cinzel(11, 600), fill=LQ_SUB if kind != 'off' else LQ_OFF_TEXT, anchor='mm')


def g_result(c, r):
    x0, y0, x1, y1 = r
    c.rect(r, fill=(18, 26, 14, 240), outline=(120, 160, 84, 255), width=1)
    c.rect((x0 + 3, y0 + 3, x1 - 3, y1 - 3), outline=(60, 84, 44, 255), width=1)
    c.text((x0 + 14, y0 + 22), 'Replenished', cinzel(13, 700), fill=LQ_GREEN, anchor='lm')
    c.text((x1 - 14, y0 + 22), '+1191 → 1400', font(16), fill=LQ_TEXT, anchor='rm')
    c.text((x0 + 14, y0 + 56), 'Budget', cinzel(12, 600), fill=LQ_LABEL, anchor='lm')
    c.text((x1 - 14, y0 + 56), '−2,300 K GOLD', font(15), fill=LQ_RED, anchor='rm')


def red_band(c, r):
    """A title band in red lacquer (the vermilion variant): a gradient with a gold rule under it."""
    x0, y0, x1, y1 = r
    for i in range(y1 - y0):
        t = i / max(1, y1 - y0 - 1)
        c.line((x0, y0 + i), (x1, y0 + i), fill=(round(120 - 50 * t), round(28 - 12 * t), round(20 - 8 * t), 255))
    c.line((x0, y1), (x1, y1), fill=LQ_GOLD, width=2)


def g_orders(c, u, used=False, hover=None, style='lacquer'):
    x0, y0, x1, y1 = G_PANEL
    lq_panel(c, G_PANEL)
    if style == 'vermilion':
        red_band(c, (x0 + 6, y0 + 6, x1 - 6, y0 + 44))
    g_budget(c, spent=23 if used else 0)
    if style != 'vermilion':
        lq_divider(c, x0 + 14, x1 - 14, y0 + 44)
    r = refill(u)
    if used:
        lq_button(c, (702, 408, 1252, 470), 'Replenish', 'used this week', 'off')
    else:
        lq_button(c, (702, 408, 1252, 470), 'Replenish', f'+{r[0]} troops  ·  {gold(r[1])}',
                  'primary-hover' if hover == 'replenish' else 'primary', title_font=brush(22), sub_font=font(15))
    c.text((704, 488), 'Add New Troops', cinzel(14, 700), fill=LQ_OFF_TEXT if used else LQ_LABEL, anchor='lm')
    c.line((704 + c.width('Add New Troops', cinzel(14, 700)) + 10, 488), (1250, 488), fill=LQ_DIM)
    ts = tiers(u, 6)
    n = len(ts)
    w = (550 - 6 * (n - 1)) / n
    for i, (men, price) in enumerate(ts):
        bx = 702 + i * (w + 6)
        kind = 'off' if used or price > GOLD else ('hover' if hover == i else 'normal')
        lq_button(c, (round(bx), 502, round(bx + w), 560), f'+{men}', gold(price), kind, title_font=brush(17), sub_font=font(12))
    c.line((704, 584), (1250, 584), fill=LQ_DIM)
    g_talk(c, (702, 598, 922, 682), u)
    if used:
        g_result(c, (934, 598, 1252, 682))
    lq_button(c, (1112, 718, 1252, 770), 'End', kind='normal', title_font=cinzel(17, 700))


def g_round(kind, style, name, caption, note, used=False, hover=None):
    c = Canvas()
    backdrop(c, kind, sys.argv[7])
    u = HAUREIN
    if used:
        u['Troops'] = 1400
    g_table(c, 12, style)
    picture(c, sys.argv[6])
    g_orders(c, u, used, hover, style)
    if used:
        u['Troops'] = 209
    c.save(name, caption, note)


def g_variants():
    g_setup()
    g_round('lacquer', 'lacquer', 'G1-lacquer-hall.png', 'G1 — Lacquer hall',
            'A dark lacquer backdrop with a faint gold wave pattern; table and orders as our map panels; Replenish red lacquer as Turn END.',
            hover=0)
    g_round('map', 'vermilion', 'G2-vermilion-over-the-map.png', 'G2 — Vermilion bands over the map',
            "The map stays behind, dimmed and blurred; the panels' title bands and the table's header in red lacquer, after Sengoku Rance's red, black and gold.",
            hover='replenish')
    g_round('lacquer', 'ledger', 'G3-ledger.png', 'G3 — The ledger',
            'G1 with the table as a paper ledger in its lacquer frame (ink, a vermilion mark for the chosen unit), after Sengoku Rance\'s lists.')
    g_round('lacquer', 'lacquer', 'G4-lacquer-used.png', 'G1 after a replenish (a state)',
            'The troop orders greyed for the week, the plate beside Talk says what was done; End and Talk still lit.', used=True)


if len(sys.argv) > 4 and sys.argv[4] == 'g':
    g_variants()
    print('done:', OUT)
    sys.exit(0)
if len(sys.argv) > 4 and sys.argv[4] == 'f':
    f1_rows(); f2_columns(); f3_line(); f1_rows(used=True)
    print('done:', OUT)
    sys.exit(0)
if len(sys.argv) > 4 and sys.argv[4] == 'e':
    for f in (e1_quad, e2_tiles, e3_column, e4_segmented, e5_left, e6_used, e7_fought):
        f()
else:
    for f in (a_twin, b_charges, c_rail, d_sheet, e_gauge, f_cards, g_face, h_inline, i_ledger, j_tabs):
        f()
print('done:', OUT)
