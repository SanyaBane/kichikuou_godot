"""Mock-ups of the unit ledger on the left with column presets (owner, 2026-10-26; docs/ui/unit-ledger.md): our
deployment screen (Sﾌﾞﾀｲ) and Subordinates screen (Sﾌﾞｶ) with the ledger down the screen's left side, showing a part
of its columns at a time, switched by tabs; the picture panel and the party (or the orders) stacked on the right.

The pictures come from the screen gallery's dumps (docs/ui/gallery.md; tools/ui/gallery.ps1, the Lacquer pass):
deployment.png and subordinates.png, whose picture panel (16,368)-(682,784) and party / orders panel
(690,368)-(1264,784) are cut out and set on the right. The ledger is drawn here. Its roster is made up so that it is
long: the week-18 save's nine units with the figures the dumps show, and twenty more from SET_CHR.ADV's records (their
boot figures, the troops made up); SET_CHR.ADV and SET_ITEM.ADV are read from the extract's scenario_named.
    python -I tools/ui/mock_unit_ledger.py <extract dir> <gallery dump dir> <out dir> [round] < /dev/null
Round a (the default): A1-A5; round b: B1-B5, the owner's A1-A4 with the first tab named Overview. The images hold the player's CGs: keep <out dir> outside the repository.
Everything is in the 1280x800 screen's pixels (plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os, re
from PIL import Image, ImageDraw, ImageFont

EXTRACT, DUMPS, OUT = sys.argv[1:4]
ROUND = sys.argv[4] if len(sys.argv) > 4 else 'a'
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H, CAPTION = 1280, 800, 64

# UiPalette.Lacquer (godot/Strategy/UiPalette.cs)
PANEL, GOLD, DIM = (22, 16, 12, 245), (196, 160, 86, 255), (110, 86, 46, 255)
NAME, SUB, QUIET, TEXT = (232, 199, 122, 255), (160, 140, 104, 255), (176, 160, 130, 255), (244, 232, 206, 255)
ROW, HOVER_LINE, CHOSEN_FILL = (34, 26, 20, 255), (222, 186, 104, 255), (60, 30, 22, 255)
PAPER, PAPER_HEAD, PAPER_RULE = (224, 208, 172, 255), (196, 176, 136, 255), (150, 120, 80, 110)
INK, INK_QUIET, INK_FAINT, INK_GREY = (40, 28, 18, 255), (100, 80, 56, 255), (130, 112, 86, 255), (150, 132, 104, 255)
VERMILION_INK, HEAD_INK, VERMILION = (150, 52, 30, 255), (120, 40, 26, 255), (170, 40, 28, 255)
CHOSEN_ROW, PARTY_ROW, HOVER_ROW = (190, 60, 40, 60), (200, 160, 80, 70), (170, 40, 28, 26)
BAR_GOOD, BAR_LOW, BAR_BAD = (70, 110, 50, 255), (160, 110, 30, 255), (170, 50, 36, 255)

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
SHOJUMARU = REPO + '/godot/fonts/Shojumaru-Regular.ttf'
_fonts = {}


def font(size, path=GOTHIC, weight=None):
    key = (size, path, weight)
    if key not in _fonts:
        f = ImageFont.truetype(path, round(size * S))
        if weight is not None:
            f.set_variation_by_axes([weight])
        _fonts[key] = f
    return _fonts[key]


def cinzel(size, weight=700):
    return font(size, CINZEL, weight)


def brush(size):
    return font(size, SHOJUMARU)


# ---------------------------------------------------------------- the roster

def read_records():
    """SET_CHR.ADV's unit records (name, type and figures by Unit_Cur) and SET_ITEM.ADV's item names."""
    scen = os.path.join(EXTRACT, 'scenario_named')
    units, cur, strings = {}, None, []
    for line in open(os.path.join(scen, 'SET_CHR.ADV'), encoding='utf-8'):
        s = line.strip()
        m = re.match(r'MS \d+, "(.*)":', s)
        if m:
            strings.append(m.group(1))
            continue
        m = re.match(r'!Unit_Cur : (\d+)!', s)
        if m:
            cur = int(m.group(1))
            units[cur] = {'n': cur, 'name': strings[-2], 'type': strings[-1]}
            strings = []
            continue
        if s.startswith('!Cmdr_Cur'):
            cur, strings = None, []
            continue
        m = re.match(r'!Unit_(\w+) : (-?\d+)!', s)
        if m and cur is not None:
            units[cur][m.group(1)] = int(m.group(2))
    items, cur = {}, None
    for line in open(os.path.join(scen, 'SET_ITEM.ADV'), encoding='utf-8'):
        s = line.strip()
        m = re.match(r'!Item_Cur : (\d+)!', s)
        if m:
            cur = int(m.group(1))
            continue
        m = re.match(r'MS 7, "(.*)":', s)
        if m and cur is not None:
            items[cur] = m.group(1)
    return units, items


def modifier(raw):
    """DeploymentTable.Modifier: below 10 the value, above 10 minus the excess, 10 none."""
    return raw if raw < 10 else 10 - raw if raw > 10 else 0


ROWS_BY_CLASS = {0: 'Front', 4: 'Front', 10: 'Front', 1: 'Back', 2: 'Back', 11: 'Back', 3: 'Front, Back'}
# The dumps' figures (the week-18 save after Haurein's replenish is undone in the gallery): type, troops / maximum,
# T.Atk, T.Def and the six modifiers as the ledger shows them.
SHOWN = {
    1: ('Leazas Knights', 1000, 1000, 10, 6, (0, 0, 0, 1, 1, 0)),
    5: ('Royal Guards', 800, 800, 9, 5, (1, 1, 1, 1, 1, 1)),
    6: ('Leazas Knights', 980, 1000, 10, 6, (2, 1, 2, 1, -1, 0)),
    7: ('Leazas Knights', 612, 800, 10, 6, (3, 0, 0, -2, -2, -2)),
    8: ('Leazas Knights', 1000, 1000, 10, 6, (-1, -1, 0, 2, -2, -2)),
    9: ('Leazas Soldiers', 901, 1500, 8, 4, (0, 0, 0, 0, 0, -2)),
    10: ('Leazas Soldiers', 649, 1250, 8, 4, (0, 0, 0, 0, -2, 0)),
    11: ('Leazas Mages', 100, 100, 3, 3, (0, 0, 0, 0, 0, 0)),
    12: ('Leazas Soldiers', 209, 1400, 8, 4, (0, 0, 1, 0, 0, 0)),
}
COSTS = {1: 3, 5: 12, 6: 11, 7: 10, 8: 11, 9: 9, 10: 8, 11: 1, 12: 6}         # x100K, as the dumps show them
MORE = [13, 15, 16, 17, 18, 19, 21, 23, 24, 26, 27, 28, 29, 30, 31, 33, 38, 40, 42, 47]
GREY = {15, 18}                                     # made up: deployed this week
PARTY = [1, 12, 8, 6]                               # places 1-4 as the deployment dump has them
ITEMS_HELD = {1: 6}                                 # the L. Holy Sword (item 6) on Rance (unit 1); By Lord on Rick from SET_CHR


def roster():
    units, items = read_records()
    rows = []
    for i, n in enumerate(sorted(list(SHOWN) + MORE)):
        u = units[n]
        if n in SHOWN:
            kind, troops, most, tatk, tdef, mods = SHOWN[n]
            cost = COSTS[n]
        else:
            kind, most, tatk, tdef = u['type'], u['MaxTroops'], u['TroopAtk'], u['TroopDef']
            troops = most if i % 3 == 0 else round(most * (0.45 + 0.17 * (i % 4)))
            mods = tuple(modifier(u[k]) for k in ('OffAtkMod', 'OffDefMod', 'DefAtkMod', 'DefDefMod', 'DgnAtkMod', 'DgnDefMod'))
            cost = u['DeployCost'] + troops // 300
        item = ITEMS_HELD.get(n, u['Item'])
        level = u['Level'] or (n * 7) % 9 + 1                # made up where the boot has none
        rows.append({
            'n': n, 'name': u['name'], 'type': kind, 'rows': ROWS_BY_CLASS.get(u['Class'], ''),
            'troops': troops, 'most': most, 'cost': cost,
            'refill': 0 if troops == most else max(1, (most - troops) // 50),
            'tatk': tatk, 'tdef': tdef, 'mods': mods,
            'strat': u['Strategy'], 'hp': u['MaxHP'] + level * 2, 'maxhp': u['MaxHP'] + level * 2, 'level': level,
            'exp': (n * 37) % (100 + level * 30), 'next': 100 + level * 30, 'atk': u['Atk'], 'def': u['Def'],
            'attacks': u['Attacks'], 'magic': u['Magic'], 'item': items.get(item, '') if item else '',
            'grey': n in GREY, 'place': PARTY.index(n) + 1 if n in PARTY else 0,
        })
    return rows


ROSTER = roster()


def number(v):
    return f'{v:,}'


def gold(k100):
    return f'{k100 * 100:,}K'


def signed(m):
    return f'+{m}' if m > 0 else f'−{-m}' if m < 0 else '0'


# ---------------------------------------------------------------- the presets

# A column: header, full name, kind ('text', 'quiet', 'num', 'troops', 'refill', 'mod'), the cell's text, its sort key.
def col(header, kind, text, key=None, high=True):
    return {'header': header, 'kind': kind, 'text': text, 'key': key or text, 'high': high}


NUMBER = col('No.', 'num', lambda r: str(r['n']), lambda r: r['n'], high=False)
NAME_COL = col('Name', 'text', lambda r: r['name'], high=False)
TYPE = col('Type', 'quiet', lambda r: r['type'], high=False)
ROWS = col('Rows', 'quiet', lambda r: r['rows'] or '—', high=False)
TROOPS = col('Troops', 'troops', lambda r: f"{number(r['troops'])}/{number(r['most'])}", lambda r: r['troops'])
REFILL = col('Refill', 'refill', lambda r: gold(r['refill']) if r['refill'] else '—', lambda r: r['refill'], high=False)
COST = col('Cost', 'num', lambda r: gold(r['cost']), lambda r: r['cost'], high=False)
TATK = col('T.Atk', 'num', lambda r: str(r['tatk']), lambda r: r['tatk'])
TDEF = col('T.Def', 'num', lambda r: str(r['tdef']), lambda r: r['tdef'])
STRIKES = col('Strikes', 'num', lambda r: str(r['attacks']), lambda r: r['attacks'])
MAG = col('Mag', 'num', lambda r: str(r['magic']), lambda r: r['magic'])
MODS = [col(h, 'mod', (lambda i: lambda r: signed(r['mods'][i]))(i), (lambda i: lambda r: r['mods'][i])(i))
        for i, h in enumerate(('Off.A', 'Off.D', 'Def.A', 'Def.D', 'Dng.A', 'Dng.D'))]
STRAT = col('Strat%', 'num', lambda r: f"{r['strat']}%", lambda r: r['strat'])
HP = col('HP', 'num', lambda r: f"{r['hp']}/{r['maxhp']}", lambda r: r['hp'])
LVL = col('Lvl', 'num', lambda r: str(r['level']), lambda r: r['level'])
EXP = col('EXP', 'num', lambda r: f"{r['exp']}/{r['next']}", lambda r: r['exp'])
CATK = col('C.Atk', 'num', lambda r: str(r['atk']), lambda r: r['atk'])
CDEF = col('C.Def', 'num', lambda r: str(r['def']), lambda r: r['def'])
ITEM = col('Item', 'quiet', lambda r: r['item'] or '—', high=False)

# The owner's three presets; the deployment's General adds Rows (the details the original shows for a hovered unit).
GENERAL_DEPLOY = [NUMBER, NAME_COL, TYPE, ROWS, TROOPS, REFILL, COST]
GENERAL_SUB = [NUMBER, NAME_COL, TYPE, TROOPS, REFILL, COST]
TROOPS_TAB = [NUMBER, NAME_COL, TATK, TDEF, STRIKES, MAG] + MODS
COMMANDER = [NUMBER, NAME_COL, STRAT, HP, LVL, EXP, CATK, CDEF, ITEM]
TABS = ['General', 'Troops', 'Commander']          # round b: the first is Overview (the owner, 2026-10-26)


# ---------------------------------------------------------------- drawing

class Canvas:
    def __init__(self):
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (14, 10, 8, 255))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1, radius=0):
        x0, y0, x1, y1 = r
        box = [x0 * S, y0 * S, x1 * S - 1, y1 * S - 1]
        if radius:
            self.d.rounded_rectangle(box, radius * S, fill=fill, outline=outline, width=width * S)
        else:
            self.d.rectangle(box, fill=fill, outline=outline, width=width * S)

    def overlay(self, r, fill):
        layer = Image.new('RGBA', self.im.size, (0, 0, 0, 0))
        x0, y0, x1, y1 = r
        ImageDraw.Draw(layer).rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill)
        self.im = Image.alpha_composite(self.im, layer)
        self.d = ImageDraw.Draw(self.im)

    def text(self, xy, s, f, fill=TEXT, anchor='la'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=DIM, width=1):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=width * S)

    def polygon(self, points, fill):
        self.d.polygon([(x * S, y * S) for x, y in points], fill=fill)

    def paste(self, img, r):
        x0, y0, x1, y1 = r
        self.im.alpha_composite(img.resize(((x1 - x0) * S, (y1 - y0) * S), Image.LANCZOS), (x0 * S, y0 * S))
        self.d = ImageDraw.Draw(self.im)

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=DIM)
        self.text((16, H + 10), caption, cinzel(20), fill=NAME)
        self.text((16, H + 38), note, font(15), fill=QUIET)
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def diamond(c, x, y, r, fill):
    c.polygon([(x, y - r), (x + r, y), (x, y + r), (x - r, y)], fill)


def backdrop(c):
    """Lacquer.DrawWaveBackdrop: a faint gold seigaiha on dark lacquer, darkened towards the edges."""
    layer = Image.new('RGBA', c.im.size, (0, 0, 0, 0))
    dd = ImageDraw.Draw(layer)
    step = 34 * S
    for row in range(-1, H * S // (step // 2) + 3):
        y = row * step // 2
        off = 0 if row % 2 == 0 else step
        for x in range(-2 * step + off, W * S + 2 * step, 2 * step):
            for kk in (1.0, 0.72, 0.44):
                rr = step * kk
                dd.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(14, 10, 8, 255) if kk == 1.0 else None,
                           outline=(196, 160, 86, 13), width=S)
    c.im = Image.alpha_composite(c.im, layer)
    mask = Image.new('L', (W, H + CAPTION), 0)
    md = ImageDraw.Draw(mask)
    for i in range(60):
        md.rectangle([i * 6, i * 4, W - 1 - i * 6, H - 1 - i * 4], outline=int(200 * (1 - i / 60) ** 2), width=6)
    dark = Image.new('RGBA', (W, H + CAPTION), (0, 0, 0, 255))
    dark.putalpha(mask)
    c.im = Image.alpha_composite(c.im, dark.resize(c.im.size, Image.LANCZOS))
    c.d = ImageDraw.Draw(c.im)


def panel(c, r):
    """UiPalette.DrawPanel: the shadow, the lacquer, the double gold frame and its corners."""
    x0, y0, x1, y1 = r
    for i in range(10, 0, -1):
        c.overlay((x0 - i + 3, y0 - i + 8, x1 + i + 3, y1 + i + 8), (0, 0, 0, 14))
    c.overlay(r, PANEL)
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


def title_band(c, r, title, subtitle, date=None):
    x0, y0, x1, _ = r
    c.text((x0 + 22, y0 + 25), title, brush(24), fill=NAME, anchor='lm')
    if subtitle:
        c.text((x0 + 22 + c.width(title, brush(24)) + 14, y0 + 27), subtitle, cinzel(13, 600), fill=SUB, anchor='lm')
    if date:
        c.text((x1 - 22, y0 + 27), date, cinzel(16), fill=NAME, anchor='rm')
    divider(c, x0 + 16, x1 - 16, y0 + 44)


def seal(c, x, y, n, size=18):
    c.rect((x, y, x + size, y + size), fill=VERMILION, radius=3)
    c.rect((x + 2, y + 2, x + size - 2, y + size - 2), outline=(236, 196, 170, 140), radius=2)
    c.text((x + size / 2, y + size / 2 + 1), str(n), cinzel(round(size * 0.7)), fill=(246, 230, 206, 255), anchor='mm')


def troop_bar(c, x, y, w, troops, most):
    f = troops / most
    c.rect((x, y, x + w, y + 3), fill=(120, 100, 70, 90))
    c.rect((x, y, x + max(1, round(w * f)), y + 3), fill=BAR_GOOD if f >= 0.75 else BAR_LOW if f >= 0.35 else BAR_BAD)


SIZE, ROW_H, HEAD_H, PAD = 16, 24, 28, 10
FIRST = 30                                          # the first column: the party's seal, the chosen row's diamond


def label_of(column, grouped):
    """The header's text: a modifier under a group's name is only its ATK or DEF."""
    if grouped and column['kind'] == 'mod':
        return 'ATK' if column['header'].endswith('A') else 'DEF'
    return column['header'].upper()


def widths(c, columns, size=SIZE, grouped=False):
    out = []
    for column in columns:
        f = font(size if column['kind'] in ('text', 'num', 'troops', 'refill', 'mod') else size - 2)
        cells = max(c.width(column['text'](r), f) for r in ROSTER)
        head = c.width(label_of(column, grouped), cinzel(11)) + 2
        out.append(round(max(cells + (10 if column['kind'] == 'refill' else 0), head) + PAD))
    return out


def ledger(c, r, columns, rows, sort=None, hover=None, chosen=None, members=True, groups=None, first_row=0):
    """The ledger on paper (DeploymentTableView, ledger): Cinzel headers in vermilion ink over a rule, the rows' rules,
    the party's rows gilt with their place's seal, the chosen row a vermilion bar and diamond, the hovered row faint
    vermilion, the deployed rows grey; a vertical scroll bar when the rows run past the paper. The columns are laid out
    from the left at their widths; the last one takes what is left."""
    x0, y0, x1, y1 = r
    c.rect(r, fill=PAPER, outline=DIM)
    ws = widths(c, columns, grouped=bool(groups))
    spare = (x1 - x0 - 12) - FIRST - sum(ws)
    if spare < 0:
        print(f'  {len(columns)} columns run {-spare} px past the paper')
    if spare > 0:                                   # the spare room spread over the columns, as the Tree's expand
        ws = [w + spare // len(ws) for w in ws]
    head = HEAD_H + (16 if groups else 0)
    c.rect((x0, y0, x1, y0 + head), fill=PAPER_HEAD)
    c.line((x0, y0 + head), (x1, y0 + head), fill=HEAD_INK)
    x = x0 + 4 + FIRST
    xs = []
    for column, w in zip(columns, ws):
        xs.append(x)
        x += w
    if groups:                                      # A6: a group's name over its columns
        for label, first, last in groups:
            gx0, gx1 = xs[first] + 4, xs[last] + ws[last] - 4
            c.text(((gx0 + gx1) / 2, y0 + 10), label.upper(), cinzel(10), fill=INK_QUIET, anchor='mm')
            c.line((gx0, y0 + 18), (gx1, y0 + 18), fill=PAPER_RULE)
    hy = y0 + head - HEAD_H / 2
    for column, w, cx in zip(columns, ws, xs):
        label = label_of(column, bool(groups))
        mark = '↓' if sort and sort[0] is column and sort[1] else '↑' if sort and sort[0] is column else ''
        if column['kind'] in ('text', 'quiet'):
            c.text((cx + 6, hy), label, cinzel(11), fill=HEAD_INK, anchor='lm')
            if mark:
                c.text((cx + 8 + c.width(label, cinzel(11)), hy), mark, font(11), fill=HEAD_INK, anchor='lm')
        else:
            right = cx + w - 7 - (11 if mark else 0)
            c.text((right, hy), label, cinzel(11), fill=HEAD_INK, anchor='rm')
            if mark:
                c.text((cx + w - 5, hy), mark, font(11), fill=HEAD_INK, anchor='rm')
    y = y0 + head + 1
    shown = 0
    for row in rows[first_row:]:
        if y + ROW_H > y1 - 2:
            break
        shown += 1
        mid = y + ROW_H / 2
        if members and row['place']:
            c.overlay((x0 + 1, y, x1 - 1, y + ROW_H), PARTY_ROW)
        if row['n'] == chosen:
            c.overlay((x0 + 1, y, x1 - 1, y + ROW_H), CHOSEN_ROW)
            c.rect((x0 + 1, y + 1, x0 + 4, y + ROW_H - 1), fill=VERMILION)
            diamond(c, x0 + 4 + FIRST / 2, mid, 4, VERMILION)
        if row['n'] == hover:
            c.overlay((x0 + 1, y, x1 - 1, y + ROW_H), HOVER_ROW)
        c.line((x0 + 6, y + ROW_H), (x1 - 6, y + ROW_H), fill=PAPER_RULE)
        if members and row['place']:
            seal(c, x0 + 4 + (FIRST - 18) / 2, mid - 9, row['place'])
        for column, w, cx in zip(columns, ws, xs):
            text = column['text'](row)
            kind = column['kind']
            ink = INK_GREY if row['grey'] else INK
            if kind == 'text':
                c.text((cx + 6, mid), text, font(SIZE), fill=ink, anchor='lm')
            elif kind == 'quiet':
                c.text((cx + 6, mid), text, font(SIZE - 2), fill=INK_GREY if row['grey'] or text == '—' else INK_QUIET,
                       anchor='lm')
            elif kind == 'troops':
                c.text((cx + 6, mid - 3), text, font(SIZE), fill=ink, anchor='lm')
                troop_bar(c, cx + 6, y + ROW_H - 6, w - 14, row['troops'], row['most'])
            elif kind == 'refill':
                if row['refill'] and not row['grey'] and members:      # the deployment's Refill buttons
                    chip = (cx + 4, y + 3, cx + w - 4, y + ROW_H - 3)
                    c.rect(chip, outline=(150, 52, 30, 150), radius=3)
                    c.text((cx + w - 9, mid), text, font(SIZE - 1), fill=VERMILION_INK, anchor='rm')
                else:
                    c.text((cx + w - 9, mid), text, font(SIZE - 1),
                           fill=INK_FAINT if text == '—' else INK_GREY if row['grey'] else VERMILION_INK, anchor='rm')
            else:
                faint = text in ('0', '—')
                c.text((cx + w - 7, mid), text, font(SIZE), fill=INK_GREY if row['grey'] else INK_FAINT if faint else ink,
                       anchor='rm')
        y += ROW_H
    if shown < len(rows):                           # the vertical scroll bar: paper and ink
        top, bottom = y0 + head + 2, y1 - 3
        c.rect((x1 - 9, top, x1 - 3, bottom), fill=PAPER_HEAD, radius=2)
        span = bottom - top
        t0 = top + span * first_row / len(rows)
        c.rect((x1 - 9, t0, x1 - 3, t0 + span * shown / len(rows)), fill=INK_QUIET, radius=2)
    return shown


def paper_tabs(c, x, top, foot, chosen, names=TABS):
    """Options' paper tabs (OptionsView.DrawTabs): the chosen one paper with a vermilion top, joined to the sheet."""
    rects = []
    for name in names:
        w = c.width(name.upper(), cinzel(16)) + 40
        on = name == chosen
        rect = (x, top if on else top + 5, x + w, foot)
        c.rect(rect, fill=PAPER if on else PAPER_HEAD, outline=PAPER_HEAD if on else INK_GREY)
        c.text((x + w / 2, (rect[1] + foot) / 2 + 1), name.upper(), cinzel(16, 700 if on else 600),
               fill=HEAD_INK if on else INK_QUIET, anchor='mm')
        rects.append((name, rect))
        x += w + 5
    for name, rect in rects:
        if name == chosen:
            c.rect((rect[0] + 1, foot - 2, rect[2] - 1, foot + 3), fill=PAPER)
            c.rect((rect[0], rect[1], rect[2], rect[1] + 4), fill=VERMILION)
    return x


def lacquer_switch(c, x, y, h, chosen, names=TABS):
    """Save/Load's switch (SaveLoadView): row boxes, the chosen one framed in vermilion on a dark red fill."""
    for name in names:
        w = c.width(name.upper(), cinzel(14)) + 36
        on = name == chosen
        c.rect((x, y, x + w, y + h), fill=CHOSEN_FILL if on else ROW, outline=VERMILION if on else DIM, width=2 if on else 1)
        c.text((x + w / 2, y + h / 2 + 1), name.upper(), cinzel(14, 700 if on else 600), fill=NAME if on else QUIET,
               anchor='mm')
        x += w + 4
    return x


# ---------------------------------------------------------------- the screens

LEDGER_W = 692                                      # the ledger's panel; the right column takes the rest
RIGHT = (16 + LEDGER_W + 8, 1264)
DATE = 'LP 3 · Month 5 · Week 2'


def dump(name):
    return Image.open(os.path.join(DUMPS, name)).convert('RGBA')


def right_column(c, source, panel_box=(690, 368, 1264, 784)):
    """The dump's picture panel and party (orders) panel, set one over the other in the right column."""
    x0, x1 = RIGHT
    w = x1 - x0
    ph = round(w * 416 / 666)
    c.paste(source.crop((16, 368, 682, 784)), (x0, 16, x1, 16 + ph))
    c.paste(source.crop(panel_box), (x0, 784 - 416, x1, 784))
    return ph


def sort_rows(column, high):
    rows = sorted(ROSTER, key=lambda r: r['n'])
    return sorted(rows, key=column['key'], reverse=high) if column is not NUMBER else rows


def screen(kind, tab, switch='tabs', sort=None, hover=None, chosen=None, groups=False, first_row=0):
    c = Canvas()
    backdrop(c)
    deploy = kind == 'deployment'
    source = dump('deployment.png' if deploy else 'subordinates.png')
    right_column(c, source)
    r = (16, 16, 16 + LEDGER_W, 784)
    panel(c, r)
    title_band(c, r, 'Deployment' if deploy else 'Subordinates',
               'Attack · up to 4 units' if deploy else f'{sum(not x["grey"] for x in ROSTER)} units ready', DATE)
    columns = [GENERAL_DEPLOY if deploy else GENERAL_SUB, TROOPS_TAB, COMMANDER][TABS.index(tab)]
    sort = sort or (NUMBER, False)
    rows = sort_rows(*sort)
    paper = (30, 100, r[2] - 14, r[3] - 14)
    if switch == 'tabs':
        end = paper_tabs(c, paper[0] + 8, 66, paper[1], tab)
    else:
        end = lacquer_switch(c, paper[0], 64, 28, tab)
    if sort[0] not in columns:                      # the order kept from another tab: said beside the tabs
        arrow = '↓' if sort[1] else '↑'
        c.text((paper[2] - 4, 84), f'Sorted by {sort[0]["header"]} {arrow}', font(13), fill=SUB, anchor='rm')
    grouped = [('Offense', 6, 7), ('Defense', 8, 9), ('Dungeon', 10, 11)] if groups or tab == 'Troops' else None
    ledger(c, paper, columns, rows, sort=sort if sort[0] in columns else None, hover=hover, chosen=chosen,
           members=deploy, groups=grouped, first_row=first_row)
    return c


def widest(c):
    for name, columns, grouped in (('General (deployment)', GENERAL_DEPLOY, False), ('General (Subordinates)', GENERAL_SUB, False),
                                   ('Troops', TROOPS_TAB, False), ('Troops grouped', TROOPS_TAB, True),
                                   ('Commander', COMMANDER, False)):
        print(f'{name}: {FIRST + sum(widths(c, columns, grouped=grouped)) + 16} px for {len(columns)} columns')


def round_a():
    c = Canvas()
    widest(c)
    roster_note = f'Made-up roster: the week-18 units and {len(MORE)} more (their boot records, the troops made up).'
    screen('deployment', 'General', hover=1).save(
        'A1.png', 'A1 — Deployment, General (paper tabs on the ledger)',
        'No., Name, Type, Rows, Troops, Refill, Cost; Rows added for deployment. ' + roster_note)
    screen('deployment', 'Troops', sort=(TATK, True), hover=1).save(
        'A2.png', 'A2 — Deployment, Troops, sorted by T.Atk',
        "No., Name, T.Atk, T.Def, Strikes, Mag, the six modifiers under Offense / Defense / Dungeon (as Off.A ... Dng.D "
        "they run 32 px past the paper).")
    screen('deployment', 'Commander', sort=(TATK, True), hover=1).save(
        'A3.png', 'A3 — Deployment, Commander, the order from A2 kept',
        'No., Name, Strat%, HP, Lvl, EXP, C.Atk, C.Def, Item; the sorted column not in the tab: "Sorted by T.Atk" beside the tabs.')
    screen('subordinates', 'General', chosen=1).save(
        'A4.png', 'A4 — Subordinates, General, the orders under the picture',
        'No., Name, Type, Troops, Refill, Cost; Rance (unit 1) chosen. The orders panel as built, set on the right.')
    screen('deployment', 'General', switch='lacquer', hover=1).save(
        'A5.png', 'A5 — the switch in lacquer (Save/Load\'s manner) instead of paper tabs',
        'Three row boxes under the title band, the chosen one framed in vermilion; the ledger as A1.')


# ---------------------------------------------------------------- round B: A1-A4 as the owner took them (2026-10-26)
# The owner took A1-A4 (paper tabs, Rows in the deployment's first tab, the modifiers grouped), the first tab named
# Overview (General read as the rank beside Commander), the order kept across tabs (A3), each screen its own tab.
#   python -I tools/ui/mock_unit_ledger.py <extract dir> <gallery dump dir> <out dir> b < /dev/null

def round_b():
    TABS[0] = 'Overview'
    roster_note = f'Made-up roster: the week-18 units and {len(MORE)} more (their boot records, the troops made up).'
    screen('deployment', 'Overview', hover=1).save(
        'B1.png', 'B1 — Deployment, Overview',
        'No., Name, Type, Rows, Troops, Refill, Cost; Rance (unit 1) hovered. ' + roster_note)
    screen('deployment', 'Troops', sort=(TATK, True), hover=1).save(
        'B2.png', 'B2 — Deployment, Troops, sorted by T.Atk',
        'No., Name, T.Atk, T.Def, Strikes, Mag, the six modifiers under Offense / Defense / Dungeon.')
    screen('deployment', 'Commander', sort=(TATK, True), hover=1).save(
        'B3.png', 'B3 — Deployment, Commander, the order from B2 kept',
        'No., Name, Strat%, HP, Lvl, EXP, C.Atk, C.Def, Item; T.Atk is not in the tab: "Sorted by T.Atk" beside the tabs.')
    screen('subordinates', 'Overview', chosen=1).save(
        'B4.png', 'B4 — Subordinates, Overview',
        'No., Name, Type, Troops, Refill, Cost; Rance (unit 1) chosen. The orders panel as built, set on the right.')
    screen('subordinates', 'Commander', sort=(LVL, True), chosen=1).save(
        'B5.png', 'B5 — Subordinates, Commander, sorted by Lvl',
        "The screen's own tab, remembered apart from the deployment's; the sorted column in the tab carries the arrow.")


if ROUND == 'a':
    round_a()
elif ROUND == 'b':
    round_b()
