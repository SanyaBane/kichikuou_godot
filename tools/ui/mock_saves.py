"""Mock-ups of our Save/Load screen over ﾛｰﾄﾞｾｰﾌﾞ's slot lists, in the lacquer look of our other screens
(docs/ui/save-load.md; docs/ui/subordinates.md, §5; docs/ui/pickers.md).

The slots are a folder of the game's own (user://saves holds the owner's game, ten slots, the ninth a save of the
moment, owner's decision 26); the background is a dump of the map in COMMAND's map wait from the week-18 save:
  <Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look-save
      --slots-from user://saves-week18 --until deployment --interface new --shots <dir>
(its "map-markers" dump). Then, from the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_saves.py <slots dir> <cg_png dir> <map dump> <out dir> [summary] < /dev/null
"summary" prints what each slot holds and draws nothing. Round 1: A1/A2 (a ledger), B1/B2 (cards with the slot's
map), C1/C2 (a list and the hovered slot's page), each in Load and Save.
The images hold the player's CGs: keep <out dir> outside the repository. Everything is in the 1280x800 screen's pixels
(plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os, re, struct, datetime
from PIL import Image, ImageDraw, ImageFont

SLOTS, CGS, MAP_DUMP, OUT = sys.argv[1:5]
SUMMARY = len(sys.argv) > 5 and sys.argv[5] == 'summary'
sys.stdout.reconfigure(encoding='utf-8')
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
S = 2
W, H, CAPTION = 1280, 800, 64

# Lacquer.cs
PANEL, GOLD, DIM = (22, 16, 12, 245), (196, 160, 86, 255), (110, 86, 46, 255)
NAME, SUB, LABEL = (232, 199, 122, 255), (160, 140, 104, 255), (150, 134, 108, 255)
VALUE, ROW, HOVER = (236, 226, 204, 255), (34, 26, 20, 255), (64, 48, 28, 255)
HOVER_LINE, TEXT, QUIET = (222, 186, 104, 255), (244, 232, 206, 255), (176, 160, 130, 255)
OFF_TEXT, OFF, OFF_LINE = (98, 88, 74, 255), (40, 35, 30, 255), (66, 58, 48, 255)
PAPER, PAPER_HEAD, PAPER_RULE = (224, 208, 172, 255), (196, 176, 136, 255), (150, 120, 80, 110)
INK, INK_QUIET, INK_GREY = (40, 28, 18, 255), (100, 80, 56, 255), (150, 132, 104, 255)
VERMILION_INK, HEAD_INK, VERMILION = (150, 52, 30, 255), (120, 40, 26, 255), (170, 40, 28, 255)
TEAL = (36, 92, 96, 255)                       # the command panel's Trade colour: a save of the moment

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


def cinzel(size, weight=600):
    return font(size, CINZEL, weight)


def brush(size):
    return font(size, SHOJUMARU)


# ---------------------------------------------------------------- the slots (Script/SaveData's slot format)

GEN = open(os.path.join(REPO, 'src/Kichikuou.Original/State/GameState.g.cs'), encoding='utf-8').read()


def record(name):
    body = GEN[GEN.index('public sealed class ' + name):]
    body = body[:body.index('\n}\n')]
    return {m.group(1): (int(m.group(2)), int(m.group(3))) for m in re.finditer(
        r'public Sys3Int (\w+)\s*\{\s*get => memory\.ReadPage\((\d+), (\d+) \+ index\)', body)}


UNIT, PROV, MARK = record('UnitRecord'), record('ProvRecord'), record('MarkRecord')


def read_slot(path):
    data = open(path, 'rb').read()
    pos = [0]

    def u7():
        v = s = 0
        while True:
            b = data[pos[0]]
            pos[0] += 1
            v |= (b & 0x7f) << s
            s += 7
            if not b & 0x80:
                return v

    def rstr():
        n = u7()
        s = data[pos[0]:pos[0] + n].decode('utf-8')
        pos[0] += n
        return s

    def i32():
        v = struct.unpack_from('<i', data, pos[0])[0]
        pos[0] += 4
        return v

    def vals():
        n = i32()
        v = list(struct.unpack_from('<%dH' % n, data, pos[0]))
        pos[0] += 2 * n
        return v

    assert rstr() == 'KKSAVE'
    i32()
    resume = (rstr(), i32())
    variables = vals()
    pages = {}
    for _ in range(i32()):
        p = i32()
        pages[p] = vals()
    strings = [rstr() for _ in range(i32())]

    def field(rec, name, idx):
        p, o = rec[name]
        return pages[p][o + idx] if p in pages and o + idx < len(pages[p]) else 0

    units = [n for n in range(1, 51) if field(UNIT, 'Status', n)]
    troops = sum(field(UNIT, 'Troops', n) for n in units)
    provs = [n for n in range(1, 100) if field(PROV, 'Owner', n) == 1]
    marks = [(field(MARK, 'Cg', i), field(MARK, 'X', i), field(MARK, 'Y', i)) for i in range(variables[212])]
    v = variables
    return dict(resume=resume, week=v[517], stage=v[518], gold=v[519], units=len(units), troops=troops,
                provs=len(provs), rance=field(UNIT, 'Level', 1), tiles=[v[219], v[221], v[223], v[220], v[222], v[224]],
                marks=marks, time=datetime.datetime.fromtimestamp(os.path.getmtime(path)))


def kind(n, slot):
    if n == 1:
        return 'Auto'
    return 'Now' if slot['resume'] in (('MAIN.ADV', 69), ('MAIN.ADV', 213)) else 'Turn start'


SLOT = {n: read_slot(os.path.join(SLOTS, 'slot%02d.ksav' % n)) for n in range(1, 11)
        if os.path.exists(os.path.join(SLOTS, 'slot%02d.ksav' % n))}

if SUMMARY:
    for n, s in SLOT.items():
        print(n, kind(n, s), s['resume'], 'week', s['week'], 'stage', s['stage'], 'gold', s['gold'], 'provs', s['provs'],
              'units', s['units'], 'troops', s['troops'], 'Rance L', s['rance'], 'tiles', s['tiles'], 'marks',
              len(s['marks']), s['time'].strftime('%Y-%m-%d %H:%M'))
    sys.exit(0)

os.makedirs(OUT, exist_ok=True)
NOW = SLOT[9] if 9 in SLOT else SLOT[1]          # the game being played: the owner's save of the moment
STAGES = {0: 'Prologue · the thieves', 1: 'Stage 1 · Leazas civil war', 2: 'Stage 2 · wars of humanity',
          3: 'Stage 3 · the Kayblis war', 4: 'Stage 4 · the Creator', 99: 'Prologue · its end'}


def date(week):
    y, w = divmod(week - 1, 48)
    m, w = divmod(w, 4)
    return 3 + y, m + 1, w + 1


def date_text(week, short=False):
    y, m, w = date(week)
    return f'LP {y} · M{m} · W{w}' if short else f'LP {y} · Month {m} · Week {w}'


def gold_text(s):
    return f"{s['gold']}00K GOLD"


def realm_text(s):
    return 'the hideout' if s['stage'] == 0 else f"{s['provs']} provinces"


def when(s):
    return s['time'].strftime('%d %b %Y, %H:%M')


# ---------------------------------------------------------------- drawing

class Canvas:
    def __init__(self):
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (14, 10, 8, 255))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1, radius=0):
        x0, y0, x1, y1 = r
        box = [x0 * S, y0 * S, x1 * S - 1, y1 * S - 1]
        if radius:
            self.d.rounded_rectangle(box, radius * S, fill=fill, outline=outline, width=round(width * S))
        else:
            self.d.rectangle(box, fill=fill, outline=outline, width=round(width * S))

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
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=round(width * S))

    def paste(self, img, xy):
        self.im.alpha_composite(img, (round(xy[0] * S), round(xy[1] * S)))
        self.d = ImageDraw.Draw(self.im)

    def poly(self, pts, fill=None, outline=None):
        self.d.polygon([(x * S, y * S) for x, y in pts], fill=fill, outline=outline)

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=DIM)
        self.text((16, H + 10), caption, cinzel(20, 700), fill=NAME)
        self.text((16, H + 38), note, font(15), fill=QUIET)
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
        print(name)


_cgs = {}


def cg(n):
    """A CG as the engine draws it with G n, 40: palette index 40 transparent."""
    if n not in _cgs:
        raw = Image.open(os.path.join(CGS, 'CG%04d.png' % n))
        img = raw.convert('RGBA')
        if raw.mode == 'P':
            key = Image.new('L', raw.size)
            key.putdata([0 if i == 40 else 255 for i in raw.getdata()])
            img.putalpha(key)
        _cgs[n] = img
    return _cgs[n]


_maps = {}


def map_image(s):
    """The map's six tiles (Ui_MapCg*, 640x400 each) and its markers as All Data draws them (各種DATA.ADV:1-11):
    the marker CG less 10000 at a third of its place, on a third of the map."""
    key = (tuple(s['tiles']), tuple(s['marks']))
    if key in _maps:
        return _maps[key]
    full = Image.new('RGBA', (1920, 800))
    for i, n in enumerate(s['tiles']):
        tile = Image.open(os.path.join(CGS, 'CG%04d.png' % n)).convert('RGBA')
        full.paste(tile, ((i % 3) * 640, (i // 3) * 400))
    third = full.resize((640, 266), Image.LANCZOS)
    for c_, x, y in s['marks']:
        if c_ > 10000 and os.path.exists(os.path.join(CGS, 'CG%04d.png' % (c_ - 10000))):
            third.alpha_composite(cg(c_ - 10000), (max(0, x // 3 - 8), max(0, y // 3 - 8)))
    _maps[key] = third
    return third


def put_map(c, s, r, dim=False):
    x0, y0, x1, y1 = r
    img = map_image(s).resize((round((x1 - x0) * S), round((y1 - y0) * S)), Image.LANCZOS)
    if dim:
        img = Image.blend(img, Image.new('RGBA', img.size, (30, 24, 18, 255)), 0.55)
    c.paste(img, (x0, y0))
    c.rect(r, outline=DIM, width=1)


def shadow(c, r, strength=14):
    x0, y0, x1, y1 = r
    for i in range(10, 0, -1):
        c.overlay((x0 - i + 3, y0 - i + 8, x1 + i + 3, y1 + i + 8), (0, 0, 0, strength))


def diamond(c, x, y, r, fill):
    c.poly([(x, y - r), (x + r, y), (x, y + r), (x - r, y)], fill=fill)


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


def title_band(c, r, title, subtitle):
    x0, y0, x1, _ = r
    c.text((x0 + 22, y0 + 25), title, brush(24), fill=NAME, anchor='lm')
    c.text((x0 + 22 + c.width(title, brush(24)) + 14, y0 + 27), subtitle, cinzel(13, 600), fill=SUB, anchor='lm')
    divider(c, x0 + 16, x1 - 16, y0 + 44)


def row_box(c, r, state='normal'):
    if state == 'off':
        c.rect(r, fill=OFF, outline=OFF_LINE, width=1)
    elif state == 'chosen':
        c.rect(r, fill=(60, 30, 22, 255), outline=VERMILION, width=2)
    else:
        hover = state == 'hover'
        c.rect(r, fill=HOVER if hover else ROW, outline=HOVER_LINE if hover else DIM, width=2 if hover else 1)


def red_button(c, r, title, sub=None, hover=False):
    """Lacquer.DrawRedButton: red lacquer framed in gold (as Turn END)."""
    x0, y0, x1, y1 = r
    top, bottom = ((150, 40, 30), (96, 20, 14)) if hover else ((128, 32, 24), (80, 16, 12))
    for i in range(round(y1 - y0)):
        t = i / max(1, y1 - y0 - 1)
        c.line((x0, y0 + i), (x1, y0 + i), fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,))
    c.rect(r, outline=GOLD, width=2)
    c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(150, 96, 52, 255), width=1)
    cx = (x0 + x1) / 2
    if sub:
        c.text((cx, y0 + (y1 - y0) * 0.40), title, brush(22), fill=NAME, anchor='mm')
        c.text((cx, y0 + (y1 - y0) * 0.76), sub, font(13), fill=(236, 206, 160, 255), anchor='mm')
    else:
        c.text((cx, (y0 + y1) / 2), title, brush(18), fill=NAME, anchor='mm')


def small_button(c, r, label, state='normal', f=None):
    row_box(c, r, state)
    x0, y0, x1, y1 = r
    c.text(((x0 + x1) / 2, (y0 + y1) / 2), label, f or cinzel(16, 700),
           fill=OFF_TEXT if state == 'off' else (NAME if state == 'hover' else TEXT), anchor='mm')


def seal(c, x, y, s, size=20, fill=VERMILION):
    c.rect((x, y, x + size, y + size), fill=fill, radius=3)
    c.rect((x + 2, y + 2, x + size - 2, y + size - 2), outline=(236, 196, 170, 140), radius=2)
    c.text((x + size / 2, y + size / 2 + 1), s, cinzel(round(size * 0.58), 700), fill=(246, 230, 206, 255), anchor='mm')


def kind_tag(c, x, y, k, off=False):
    """The slot's kind: Auto (gold), a turn's start (quiet), a save of the moment (teal) — ours, decision 26."""
    colours = {'Auto': (138, 104, 34, 255), 'Turn start': (86, 70, 50, 255), 'Now': TEAL}
    label = {'Auto': 'Auto', 'Turn start': 'Turn start', 'Now': 'Mid-turn'}[k]
    f = font(12)
    w = c.width(label, f) + 14
    c.rect((x, y, x + w, y + 20), fill=OFF if off else colours[k], radius=3)
    c.text((x + w / 2, y + 10), label, f, fill=OFF_TEXT if off else (246, 232, 206, 255), anchor='mm')
    return w


def pointer(c, x, y):
    p = [(x, y), (x, y + 22), (x + 6, y + 17), (x + 10, y + 26), (x + 14, y + 24), (x + 10, y + 15), (x + 17, y + 15)]
    c.poly(p, fill=(255, 255, 255, 255), outline=(0, 0, 0, 255))


def tabs(c, x, y, active):
    """Load | Save, from COMMAND only: the other list is the script's menu answered for the player."""
    for i, label in enumerate(('Load', 'Save')):
        r = (x + i * 96, y, x + i * 96 + 90, y + 30)
        if label == active:
            c.rect(r, fill=(60, 30, 22, 255), outline=VERMILION, width=2)
            c.text(((r[0] + r[2]) / 2, y + 15), label, cinzel(15, 700), fill=NAME, anchor='mm')
        else:
            c.rect(r, fill=ROW, outline=DIM, width=1)
            c.text(((r[0] + r[2]) / 2, y + 15), label, cinzel(15, 700), fill=QUIET, anchor='mm')


def background():
    c = Canvas()
    dump = Image.open(MAP_DUMP).convert('RGBA')
    if dump.size[1] > 800:
        dump = dump.crop((0, dump.size[1] - 800, 1280, dump.size[1]))
    c.paste(dump.resize((W * S, H * S), Image.LANCZOS), (0, 0))
    c.overlay((0, 0, W, H), (0, 0, 0, 120))
    return c


def callout(c, cell, lines, answers=('Yes', 'No')):
    """Our question at the clicked cell, as the Treasure screen's (round W, W2)."""
    x0, y0, x1, y1 = cell
    w, h = 420, 40 + 24 * len(lines) + 44
    cx = min(max((x0 + x1) / 2 - w / 2, 70), W - 70 - w)
    top = y1 + 14
    if top + h > H - 30:
        top = y0 - 14 - h
    r = (cx, top, cx + w, top + h)
    lq_panel(c, r)
    tip = (x0 + x1) / 2
    if top > y1:
        c.poly([(tip - 10, top + 2), (tip + 10, top + 2), (tip, top - 12)], fill=GOLD)
    else:
        c.poly([(tip - 10, r[3] - 2), (tip + 10, r[3] - 2), (tip, r[3] + 12)], fill=GOLD)
    for i, line in enumerate(lines):
        c.text((cx + 22, top + 24 + 24 * i), line, font(16), fill=TEXT)
    by = top + 30 + 24 * len(lines)
    red_button(c, (cx + w - 222, by, cx + w - 122, by + 30), answers[0])
    small_button(c, (cx + w - 112, by, cx + w - 22, by + 30), answers[1])


def label_of(n):
    return 'Auto' if n == 1 else 'Slot %d' % (n - 1)


def question(n):
    """Our question before a save over a game: which game the slot holds."""
    s = SLOT[n]
    k = {'Turn start': "a turn's start", 'Now': 'mid-turn', 'Auto': 'Auto'}[kind(n, s)]
    return [label_of(n) + ' holds ' + date_text(s['week']), '(' + k + '). Save over it?']


# ---------------------------------------------------------------- A: the ledger

COLUMNS = [('No.', 56), ('Kind', 104), ('Date', 200), ('Stage', 222), ('Budget', 126), ('Realm', 112), ('Troops', 74),
           ('Written', 132)]


def ledger_row(c, x, y, n, s, state, row_h, save_mode):
    if s is None:
        cols = ['Auto' if n == 1 else str(n - 1), None, '— empty —', '', '', '', '', '']
    else:
        cols = ['Auto' if n == 1 else str(n - 1), kind(n, s), date_text(s['week']), STAGES.get(s['stage'], ''),
                gold_text(s), realm_text(s), str(s['troops']), s['time'].strftime('%d %b, %H:%M')]
    off = save_mode and n == 1
    if state == 'hover':
        c.rect((x, y, x + sum(w for _, w in COLUMNS), y + row_h), fill=(240, 222, 178, 255), outline=VERMILION, width=2)
    ink = INK_GREY if off or s is None else INK
    cx = x
    for (head, w), value in zip(COLUMNS, cols):
        if head == 'Kind':
            if value:
                kind_tag(c, cx + 6, y + row_h / 2 - 10, value, off=off)
        elif value:
            f = cinzel(14, 700) if head == 'No.' else font(14)
            c.text((cx + 10, y + row_h / 2), value, f, fill=VERMILION_INK if head == 'No.' and not off else ink, anchor='lm')
        cx += w
    c.line((x, y + row_h), (cx, y + row_h), fill=PAPER_RULE)


def ledger(c, r, hover, save_mode):
    x0, y0, x1, y1 = r
    c.rect(r, fill=PAPER)
    c.rect((x0, y0, x1, y0 + 30), fill=PAPER_HEAD)
    cx = x0
    for head, w in COLUMNS:
        c.text((cx + 10, y0 + 15), head, cinzel(13, 700), fill=HEAD_INK, anchor='lm')
        cx += w
    row_h = (y1 - y0 - 30) / 10
    for i, n in enumerate(range(1, 11)):
        ledger_row(c, x0, y0 + 30 + i * row_h, n, SLOT.get(n), 'hover' if n == hover else 'normal', row_h, save_mode)
    return row_h


def a_screen(save_mode):
    c = background()
    r = (90, 40, 1190, 760)
    lq_panel(c, r)
    title_band(c, r, 'Save' if save_mode else 'Load', 'the game as it is now' if save_mode else 'choose a game to go on with')
    tabs(c, r[2] - 220, r[1] + 11, 'Save' if save_mode else 'Load')
    hover = 5 if save_mode else 9
    lx0 = r[0] + 22
    lw = sum(w for _, w in COLUMNS)
    top = r[1] + 62
    row_h = ledger(c, (lx0, top, lx0 + lw, top + 30 + 10 * 40), hover, save_mode)
    sy = top + 30 + 10 * 40 + 18
    s = SLOT.get(hover)
    put_map(c, s, (lx0, sy, lx0 + 330, sy + 137))
    tx = lx0 + 350
    c.text((tx, sy + 12), label_of(hover) + ' · ' + date_text(s['week']), cinzel(19, 700), fill=NAME)
    kind_tag(c, tx + c.width(label_of(hover) + ' · ' + date_text(s['week']), cinzel(19, 700)) + 14, sy + 14, kind(hover, s))
    if save_mode:
        c.text((tx, sy + 48), 'A click saves the game being played here — ' + date_text(NOW['week']) + ', mid-turn —', font(15), fill=TEXT)
        c.text((tx, sy + 70), 'in place of the game it holds. It asks first.', font(15), fill=TEXT)
    else:
        c.text((tx, sy + 48), 'Saved in the middle of its week: a click goes on from that', font(15), fill=TEXT)
        c.text((tx, sy + 70), 'week\'s map, as it was. Auto stays as it is.', font(15), fill=TEXT)
    c.text((tx, sy + 110), 'Right click: back to the map', font(13), fill=QUIET)
    cell_y = top + 30 + (hover - 1) * row_h
    if save_mode:
        callout(c, (lx0 + 160, cell_y, lx0 + 560, cell_y + row_h), question(hover))
        pointer(c, lx0 + 420, cell_y + 18)
    else:
        pointer(c, lx0 + 420, cell_y + 20)
    n = '2' if save_mode else '1'
    c.save(('A2-ledger-save.png' if save_mode else 'A1-ledger-load.png'),
           'A' + n + ' — the slots as a paper ledger (' + ('Save' if save_mode else 'Load') + ')',
           'Each slot one row, its kind a tag; under it the hovered slot\'s map and what a click does. '
           + ('Saving over a game asks first, at its row.' if save_mode else 'Empty rows are grey and take no click.'))


# ---------------------------------------------------------------- B: cards with the slot's map

def dashed(c, r):
    x0, y0, x1, y1 = r
    for x in range(round(x0), round(x1), 12):
        c.line((x, y0), (min(x + 6, x1), y0), fill=DIM)
        c.line((x, y1), (min(x + 6, x1), y1), fill=DIM)
    for y in range(round(y0), round(y1), 12):
        c.line((x0, y), (x0, min(y + 6, y1)), fill=DIM)
        c.line((x1, y), (x1, min(y + 6, y1)), fill=DIM)


def card(c, r, n, s, state, save_mode):
    x0, y0, x1, y1 = r
    off = save_mode and n == 1
    if s is None:
        c.rect(r, fill=(26, 20, 16, 230))
        dashed(c, r)
        seal(c, x0 + 14, y0 + 14, str(n - 1), 26, fill=OFF_LINE)
        c.text(((x0 + x1) / 2, (y0 + y1) / 2), 'Empty' + (' — save here' if save_mode else ''), cinzel(16, 600),
               fill=QUIET if save_mode else OFF_TEXT, anchor='mm')
        return
    hover = state == 'hover'
    c.rect(r, fill=HOVER if hover else (34, 26, 20, 250), outline=HOVER_LINE if hover else DIM, width=2 if hover else 1)
    put_map(c, s, (x0 + 10, y0 + 10, x0 + 10 + 240, y0 + 10 + 100), dim=off)
    tx = x0 + 264
    seal(c, tx, y0 + 12, 'A' if n == 1 else str(n - 1), 26, fill=OFF_LINE if off else ((138, 104, 34, 255) if n == 1 else VERMILION))
    c.text((tx + 36, y0 + 25), date_text(s['week']), cinzel(17, 700), fill=OFF_TEXT if off else NAME, anchor='lm')
    w = kind_tag(c, tx, y0 + 47, kind(n, s), off=off)
    c.text((tx + w + 10, y0 + 57), STAGES.get(s['stage'], ''), font(13), fill=OFF_TEXT if off else QUIET, anchor='lm')
    c.text((tx, y0 + 86), f"{gold_text(s)} · {realm_text(s)} · {s['troops']} troops", font(13),
           fill=OFF_TEXT if off else TEXT, anchor='lm')
    if off:
        c.text((x1 - 12, y1 - 10), 'the game writes it each turn', font(12), fill=QUIET, anchor='rb')
    else:
        c.text((x1 - 12, y1 - 10), s['time'].strftime('%d %b, %H:%M'), font(12), fill=LABEL, anchor='rb')


def b_screen(save_mode):
    c = background()
    r = (30, 24, 1250, 776)
    lq_panel(c, r)
    title_band(c, r, 'Save' if save_mode else 'Load', 'the game as it is now' if save_mode else 'choose a game to go on with')
    tabs(c, r[2] - 220, r[1] + 11, 'Save' if save_mode else 'Load')
    hover = 7 if save_mode else 9
    cw, ch, gx, gy = 582, 120, 12, 11
    cells = {}
    for i, n in enumerate(range(1, 11)):
        col, row = i % 2, i // 2
        x = r[0] + 22 + col * (cw + gx)
        y = r[1] + 60 + row * (ch + gy)
        cells[n] = (x, y, x + cw, y + ch)
        card(c, cells[n], n, SLOT.get(n), 'hover' if n == hover else 'normal', save_mode)
    c.text((r[0] + 26, r[3] - 18), 'Right click: back to the map', font(13), fill=QUIET, anchor='lm')
    x0, y0, x1, y1 = cells[hover]
    if save_mode:
        callout(c, (x0 + 260, y0, x1, y1), question(hover))
        pointer(c, x0 + 380, y0 + 40)
    else:
        pointer(c, x0 + 380, y0 + 70)
    n = '2' if save_mode else '1'
    c.save(('B2-cards-save.png' if save_mode else 'B1-cards-load.png'),
           'B' + n + ' — the slots as cards with their map (' + ('Save' if save_mode else 'Load') + ')',
           'Each slot a card: that game\'s map as All Data draws it, its date, kind and figures. '
           + ('Auto is dim (the game writes it); saving over a game asks first.' if save_mode else 'A click on a card loads it.'))


# ---------------------------------------------------------------- C: a list and the slot's page

def list_row(c, r, n, s, state, save_mode):
    x0, y0, x1, y1 = r
    off = save_mode and n == 1
    row_box(c, r, 'chosen' if state == 'chosen' else ('hover' if state == 'hover' else ('off' if off else 'normal')))
    seal(c, x0 + 10, y0 + (y1 - y0) / 2 - 12, 'A' if n == 1 else str(n - 1), 24,
         fill=OFF_LINE if s is None or off else ((138, 104, 34, 255) if n == 1 else VERMILION))
    if s is None:
        c.text((x0 + 46, (y0 + y1) / 2), 'Empty', cinzel(15, 600), fill=OFF_TEXT, anchor='lm')
        return
    c.text((x0 + 46, y0 + 17), date_text(s['week'], short=True), cinzel(15, 700), fill=OFF_TEXT if off else NAME, anchor='lm')
    kind_tag(c, x0 + 46, y0 + 30, kind(n, s), off=off)
    c.text((x1 - 10, y0 + 40), s['time'].strftime('%d %b, %H:%M'), font(12), fill=OFF_TEXT if off else LABEL, anchor='rm')


def stat(c, x, y, label, value):
    c.text((x, y), label, font(13), fill=LABEL)
    c.text((x, y + 18), value, font(17), fill=VALUE)


def c_screen(save_mode):
    c = background()
    r = (30, 24, 1250, 776)
    lq_panel(c, r)
    title_band(c, r, 'Save' if save_mode else 'Load', 'the game as it is now' if save_mode else 'choose a game to go on with')
    tabs(c, r[2] - 220, r[1] + 11, 'Save' if save_mode else 'Load')
    chosen = 6 if save_mode else 9
    lx0, ly0, rh = r[0] + 22, r[1] + 60, 63
    for i, n in enumerate(range(1, 11)):
        list_row(c, (lx0, ly0 + i * (rh + 5), lx0 + 330, ly0 + i * (rh + 5) + rh), n, SLOT.get(n),
                 'chosen' if n == chosen else ('hover' if n == 3 else 'normal'), save_mode)
    px0, py0, px1 = lx0 + 354, ly0, r[2] - 24
    s = SLOT[chosen]
    title = label_of(chosen) + ' · ' + date_text(s['week'])
    c.text((px0, py0 + 14), title, cinzel(22, 700), fill=NAME, anchor='lm')
    kind_tag(c, px0 + c.width(title, cinzel(22, 700)) + 16, py0 + 4, kind(chosen, s))
    mh = round((px1 - px0) * 266 / 640)
    put_map(c, s, (px0, py0 + 34, px1, py0 + 34 + mh))
    gy = py0 + 34 + mh + 16
    cols = [('Stage', STAGES.get(s['stage'], '')), ('Budget', gold_text(s)), ('Realm', realm_text(s)),
            ('Units', str(s['units'])), ('Troops', str(s['troops'])), ('Written', when(s))]
    for i, (label, value) in enumerate(cols):
        stat(c, px0 + (i % 3) * 280, gy + (i // 3) * 50, label, value)
    by = r[3] - 82
    if save_mode:
        red_button(c, (px1 - 330, by, px1, by + 58), 'Save over ' + label_of(chosen), 'it holds ' + date_text(s['week']), hover=True)
        c.text((px0, by + 16), 'Saving: ' + date_text(NOW['week']) + ', mid-turn', font(15), fill=TEXT, anchor='lm')
        c.text((px0, by + 40), f"{gold_text(NOW)} · {realm_text(NOW)} · {NOW['troops']} troops", font(14), fill=QUIET, anchor='lm')
    else:
        red_button(c, (px1 - 330, by, px1, by + 58), 'Load this game', 'from the middle of its week', hover=True)
        c.text((px0, by + 16), 'Auto stays the start of week ' + str(SLOT[1]['week']) + '.', font(15), fill=TEXT, anchor='lm')
        c.text((px0, by + 40), 'Right click: back to the map', font(13), fill=QUIET, anchor='lm')
    pointer(c, px1 - 26, by + 34)
    n = '2' if save_mode else '1'
    c.save(('C2-page-save.png' if save_mode else 'C1-page-load.png'),
           'C' + n + ' — a list and the chosen slot\'s page (' + ('Save' if save_mode else 'Load') + ')',
           'A click picks a slot and shows its page: the map large, the figures; the red button '
           + ('saves — the second click is the question.' if save_mode else 'loads it (a second click).'))


for save_mode in (False, True):
    a_screen(save_mode)
    b_screen(save_mode)
    c_screen(save_mode)
