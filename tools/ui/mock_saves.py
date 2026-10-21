"""Mock-ups of our Save/Load screen over ﾛｰﾄﾞｾｰﾌﾞ's slot lists, in the lacquer look of our other screens
(docs/ui/save-load.md; docs/ui/subordinates.md, §5; docs/ui/pickers.md).

The slots are a folder of the game's own (user://saves holds the owner's game, ten slots, the ninth a save of the
moment, owner's decision 26); the background is a dump of the map in COMMAND's map wait from the week-18 save:
  <Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look-save
      --slots-from user://saves-week18 --until deployment --interface new --shots <dir>
(its "map-markers" dump). Then, from the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_saves.py <slots dir> <cg_png dir> <map dump> <out dir> [summary|sort] < /dev/null
"summary" prints what each slot holds and draws nothing. Round 1: A1/A2 (a ledger), B1/B2 (cards with the slot's
map), C1/C2 (a list and the hovered slot's page), each in Load and Save. "sort" draws round 2 instead, the list of
C sorted by number or by the time written: D (the switch in the title band) and E (Auto in a panel of its own, the
switch over the other nine); its times are made up (the owner's slots were all stamped alike by ENG-07).
"library" draws round 4 (owner's decision 29): ten auto-saves newest first and as many manual saves as the player
keeps, each deletable, a manual save with a comment — G (the two lists as tabs over one column), H (the two stacked),
the delete question and the comment being written; its saves are made up from the owner's slots.
"notes" draws round 5 (docs/ui/save-load.md, §10) over the screen as built: a star on the seal of a save with a
comment and a button deleting every manual save without one (Sengoku Rance's), in the list's heading (N1) or under the
list (N2), asking (N3, N4), after it (N5) and in Save (N6).
The images hold the player's CGs: keep <out dir> outside the repository. Everything is in the 1280x800 screen's pixels
(plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os, re, math, struct, datetime
from PIL import Image, ImageDraw, ImageFont

SLOTS, CGS, MAP_DUMP, OUT = sys.argv[1:5]
ROUND = sys.argv[5] if len(sys.argv) > 5 else ''
SUMMARY = ROUND == 'summary'
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


# ---------------------------------------------------------------- round 2: sorting the list (D, E)

AUTO_GOLD, CHOSEN = (138, 104, 34, 255), (60, 30, 22, 255)
SORT_TIMES = {1: (20, 1, 32), 2: (18, 21, 5), 3: (18, 21, 40), 4: (19, 22, 10), 5: (18, 23, 2), 6: (19, 20, 47),
              7: (19, 23, 55), 8: (20, 0, 58), 9: (20, 1, 21), 10: (20, 0, 14)}


def sort_order(order, slots, desc=None):
    """By number: 1-10 (desc: Auto, then 9 down to 1). By date: Auto first, then the games newest first (desc, the
    default) or oldest first; the empty slots last by number either way."""
    if order in ('No.', 'Slot'):
        return [1] + (list(range(10, 1, -1)) if desc else list(range(2, 11)))
    filled = sorted((n for n in range(2, 11) if slots.get(n)), key=lambda n: slots[n]['time'], reverse=desc is not False)
    return [1] + filled + [n for n in range(2, 11) if not slots.get(n)]


def sort_switch(c, right, y, order, label=True, first='No.', desc=None):
    """Two segments, the slot's number and Date, as the tabs: the active one in the chosen row's colours. Round 2: Date
    always newest first (a triangle down). Round 3 (desc given): the active one shows its direction, a second click
    turns it (up: 1 at the top, the oldest at the top)."""
    f = cinzel(13, 700)
    segs = [(first, 66 if desc is not None else 58), ('Date', 74)]
    x = right - sum(w for _, w in segs) - 4
    if label:
        c.text((x - 10, y + 13), 'Order', cinzel(12, 600), fill=SUB, anchor='rm')
    for name, w in segs:
        r = (x, y, x + w, y + 26)
        active = name == order
        if active:
            c.rect(r, fill=CHOSEN, outline=VERMILION, width=2)
            colour = NAME
        else:
            c.rect(r, fill=ROW, outline=DIM, width=1)
            colour = QUIET
        down = (desc is None and name == 'Date') or (desc is not None and active and desc)
        up = desc is not None and active and not desc
        if down or up:
            tx = x + w / 2 - 6
            c.text((tx, y + 13), name, f, fill=colour, anchor='mm')
            ax = tx + c.width(name, f) / 2 + 9
            if down:
                c.poly([(ax - 4, y + 11), (ax + 4, y + 11), (ax, y + 16)], fill=colour)
            else:
                c.poly([(ax - 4, y + 16), (ax + 4, y + 16), (ax, y + 11)], fill=colour)
        else:
            c.text((x + w / 2, y + 13), name, f, fill=colour, anchor='mm')
        x += w + 4


def sort_row(c, r, n, s, state, save_mode, order, auto_panel=False):
    """C's row; by date the time written is the bright line (the key)."""
    x0, y0, x1, y1 = r
    h = y1 - y0
    off = save_mode and n == 1
    row_box(c, r, 'chosen' if state == 'chosen' else ('off' if off else 'normal'))
    if auto_panel and n == 1 and not off and state != 'chosen':
        c.rect(r, outline=AUTO_GOLD, width=2)
    seal(c, x0 + 10, y0 + h / 2 - 12, 'A' if n == 1 else str(n - 1), 24,
         fill=OFF_LINE if s is None or off else (AUTO_GOLD if n == 1 else VERMILION))
    if s is None:
        c.text((x0 + 46, (y0 + y1) / 2), 'Empty', cinzel(15, 600), fill=OFF_TEXT, anchor='lm')
        return
    top = 17 if h >= 60 else 15
    c.text((x0 + 46, y0 + top), date_text(s['week'], short=True), cinzel(15, 700), fill=OFF_TEXT if off else NAME, anchor='lm')
    kind_tag(c, x0 + 46, y0 + top + 13, kind(n, s), off=off)
    key = order == 'Date' and not off
    c.text((x1 - 10, y0 + top + 23), s['time'].strftime('%d %b, %H:%M'), font(13 if key else 12),
           fill=OFF_TEXT if off else (VALUE if key else LABEL), anchor='rm')


def sort_page(c, r, px0, py0, px1, chosen, slots, save_mode):
    """C's page: the chosen slot (an empty one to save into shows the game being saved), its figures and the button."""
    s = slots.get(chosen)
    shown = s or NOW
    if s:
        title = label_of(chosen) + ' · ' + date_text(s['week'])
        c.text((px0, py0 + 14), title, cinzel(22, 700), fill=NAME, anchor='lm')
        kind_tag(c, px0 + c.width(title, cinzel(22, 700)) + 16, py0 + 4, kind(chosen, s))
    else:
        title = label_of(chosen) + ' · Empty'
        c.text((px0, py0 + 14), title, cinzel(22, 700), fill=NAME, anchor='lm')
        c.text((px0 + c.width(title, cinzel(22, 700)) + 16, py0 + 15), 'the game below will be saved here', cinzel(13, 600),
               fill=SUB, anchor='lm')
    mh = round((px1 - px0) * 266 / 640)
    put_map(c, shown, (px0, py0 + 34, px1, py0 + 34 + mh))
    gy = py0 + 34 + mh + 16
    cols = [('Stage', STAGES.get(shown['stage'], '')), ('Budget', gold_text(shown)), ('Realm', realm_text(shown)),
            ('Units', str(shown['units'])), ('Troops', str(shown['troops'])), ('Written', when(shown) if s else 'now')]
    for i, (label, value) in enumerate(cols):
        stat(c, px0 + (i % 3) * 280, gy + (i // 3) * 50, label, value)
    by = r[3] - 82
    if save_mode:
        red_button(c, (px1 - 330, by, px1, by + 58), 'Save here' if s is None else 'Save over ' + label_of(chosen),
                   'into ' + label_of(chosen) if s is None else 'it holds ' + date_text(s['week']))
        c.text((px0, by + 16), 'Saving: ' + date_text(NOW['week']) + ', mid-turn', font(15), fill=TEXT, anchor='lm')
        c.text((px0, by + 40), f"{gold_text(NOW)} · {realm_text(NOW)} · {NOW['troops']} troops", font(14), fill=QUIET, anchor='lm')
    else:
        red_button(c, (px1 - 330, by, px1, by + 58), 'Load this game', 'from the middle of its week')
        c.text((px0, by + 16), 'Auto stays as it is: ' + date_text(slots[1]['week']) + '.', font(15), fill=TEXT, anchor='lm')
        c.text((px0, by + 40), 'Right click: back to the map', font(13), fill=QUIET, anchor='lm')


def sort_frame(save_mode):
    c = background()
    r = (30, 24, 1250, 776)
    lq_panel(c, r)
    title_band(c, r, 'Save' if save_mode else 'Load', 'the game as it is now' if save_mode else 'choose a game to go on with')
    tabs(c, r[2] - 220, r[1] + 11, 'Save' if save_mode else 'Load')
    return c, r


def d_screen(name, order, save_mode, slots, chosen, caption, note):
    """D: the list as built, the switch in the title band left of the tabs; Auto stays the first row either way."""
    c, r = sort_frame(save_mode)
    sw_right = r[2] - 220 - 24
    sort_switch(c, sw_right, r[1] + 13, order)
    lx0, ly0, rh = r[0] + 22, r[1] + 60, 63
    for i, n in enumerate(sort_order(order, slots)):
        sort_row(c, (lx0, ly0 + i * (rh + 5), lx0 + 330, ly0 + i * (rh + 5) + rh), n, slots.get(n),
                 'chosen' if n == chosen else 'normal', save_mode, order)
    sort_page(c, r, lx0 + 354, ly0, r[2] - 24, chosen, slots, save_mode)
    if order == 'Date':
        pointer(c, sw_right - 40, r[1] + 28)
    c.save(name, caption, note)


def e_screen(name, order, save_mode, slots, chosen, caption, note, heading='YOUR SAVES', first='No.', desc=None):
    """E: Auto in a panel of its own over the list; the other nine under a strip with the switch, rows a little lower."""
    c, r = sort_frame(save_mode)
    lx0, lx1, ly0 = r[0] + 22, r[0] + 352, r[1] + 60
    c.text((lx0, ly0 + 2), 'AUTO-SAVE', cinzel(11, 700), fill=SUB, anchor='lm')
    c.text((lx1, ly0 + 2), "the game's own, each turn", font(11), fill=LABEL, anchor='rm')
    auto = (lx0, ly0 + 12, lx1, ly0 + 72)
    sort_row(c, auto, 1, slots.get(1), 'chosen' if chosen == 1 else 'normal', save_mode, order, auto_panel=True)
    sy = auto[3] + 12
    c.line((lx0, sy), (lx1, sy), fill=DIM)
    c.text((lx0, sy + 22), heading, cinzel(11, 700), fill=SUB, anchor='lm')
    sort_switch(c, lx1 + 4, sy + 9, order, label=False, first=first, desc=desc)
    y, rh, pitch = sy + 44, 57, 62
    for n in sort_order(order, slots, desc)[1:]:
        sort_row(c, (lx0, y, lx1, y + rh), n, slots.get(n), 'chosen' if n == chosen else 'normal', save_mode, order)
        y += pitch
    sort_page(c, r, lx0 + 354, ly0, r[2] - 24, chosen, slots, save_mode)
    if order == 'Date' or desc:
        pointer(c, lx1 - (40 if order == 'Date' else 110), sy + 26)
    c.save(name, caption, note)


# ---------------------------------------------------------------- round 4: auto-saves and manual saves (G, H)

COMMENTS = {3: 'before the attack on Oaks', 5: 'Haurein joined', 8: 'safe point, all troops full', 11: '',
            12: 'try the Monsters\' Cave with Rance alone', 14: 'Sill\'s scene next week'}


def library():
    """Made-up saves from the owner's slots: ten auto-saves of consecutive weeks (newest first) and fourteen manual
    saves numbered as they were made, each with a time written and a comment."""
    autos = []
    base = SLOT[9]
    for i in range(10):
        s = dict(SLOT[[9, 8, 10, 7, 6, 4, 9, 8, 10, 7][i]])
        s['week'] = base['week'] - i
        s['time'] = datetime.datetime(2026, 10, 21, 23, 40) - datetime.timedelta(minutes=17 * i + (i * i) % 7)
        autos.append(s)
    manual = {}
    sources = [3, 5, 2, 4, 6, 7, 10, 8, 9, 3, 6, 10, 8, 9]
    for no, src in enumerate(sources, start=1):
        s = dict(SLOT[src])
        s['time'] = datetime.datetime(2026, 10, 19, 20, 5) + datetime.timedelta(hours=3 * no + no % 4, minutes=11 * no % 60)
        s['comment'] = COMMENTS.get(no, '')
        s['no'] = no
        manual[no] = s
    return autos, manual


def lib_row(c, r, seal_text, s, state, gold=False, order='Date', compact=False, dim=False):
    """A row of either list: the seal, the date, the time written right; under them the comment, quiet."""
    x0, y0, x1, y1 = r
    h = y1 - y0
    row_box(c, r, state if state in ('chosen', 'hover') else 'normal')
    if gold and state != 'chosen':
        c.rect(r, outline=AUTO_GOLD, width=1)
    size = 20 if compact else 24
    seal(c, x0 + 8, y0 + h / 2 - size / 2, seal_text, size, fill=AUTO_GOLD if gold else VERMILION)
    tx = x0 + 18 + size
    key = order == 'Date'
    if compact:
        c.text((tx, y0 + h / 2), date_text(s['week'], short=True), cinzel(14, 700), fill=NAME, anchor='lm')
        c.text((x1 - 10, y0 + h / 2), s['time'].strftime('%d %b, %H:%M'), font(13), fill=VALUE, anchor='rm')
        return
    c.text((tx, y0 + 16), date_text(s['week'], short=True), cinzel(15, 700), fill=OFF_TEXT if dim else NAME, anchor='lm')
    c.text((x1 - 10, y0 + 16), s['time'].strftime('%d %b, %H:%M'), font(13 if key else 12),
           fill=OFF_TEXT if dim else (VALUE if key else LABEL), anchor='rm')
    comment = s.get('comment', '')
    if comment:
        f = font(12)
        while c.width('“' + comment + '”', f) > x1 - tx - 12:
            comment = comment[:-2] + '…'
        c.text((tx, y0 + 37), '“' + comment.rstrip('…') + ('…' if comment.endswith('…') else '') + '”', f,
               fill=OFF_TEXT if dim else QUIET, anchor='lm')
    elif gold:
        c.text((tx, y0 + 37), 'start of the week', font(12), fill=OFF_TEXT if dim else LABEL, anchor='lm')


def new_row(c, r, state):
    """The Save list's first row: a new save, numbered after the last one."""
    x0, y0, x1, y1 = r
    if state == 'chosen':
        row_box(c, r, 'chosen')
    else:
        c.rect(r, fill=(26, 20, 16, 230))
        dashed(c, r)
    c.text((x0 + 18, (y0 + y1) / 2), '+', cinzel(22, 700), fill=NAME, anchor='lm')
    c.text((x0 + 44, (y0 + y1) / 2), 'New save', cinzel(15, 700), fill=NAME if state == 'chosen' else QUIET, anchor='lm')


def scrollbar(c, x, top, bottom, first, shown, total):
    c.rect((x, top, x + 5, bottom), fill=(40, 32, 24, 255), radius=2)
    span = bottom - top
    a = top + span * first / total
    b = top + span * min(total, first + shown) / total
    c.rect((x, a, x + 5, b), fill=GOLD, radius=2)


def segment(c, r, labels, active):
    """Two halves of one switch: the active one in the chosen row's colours."""
    x0, y0, x1, y1 = r
    w = (x1 - x0 - 4) / len(labels)
    for i, label in enumerate(labels):
        rr = (x0 + i * (w + 4), y0, x0 + i * (w + 4) + w, y1)
        on = i == active
        c.rect(rr, fill=CHOSEN if on else ROW, outline=VERMILION if on else DIM, width=2 if on else 1)
        c.text(((rr[0] + rr[2]) / 2, (y0 + y1) / 2), label, cinzel(14, 700), fill=NAME if on else QUIET, anchor='mm')


def comment_box(c, x0, x1, y, s, editing=False, text=None):
    """The chosen manual save's comment: a paper slip with Edit; written in place (Enter keeps it, Esc leaves it)."""
    c.text((x0, y), 'Comment', font(13), fill=LABEL)
    r = (x0, y + 20, x1 - (0 if editing else 104), y + 54)
    c.rect(r, fill=PAPER if editing else (34, 26, 20, 255), outline=VERMILION if editing else DIM, width=2 if editing else 1)
    shown = text if text is not None else s.get('comment', '')
    if editing:
        c.text((r[0] + 10, (r[1] + r[3]) / 2), shown, font(16), fill=INK, anchor='lm')
        cx = r[0] + 12 + c.width(shown, font(16))
        c.line((cx, r[1] + 8), (cx, r[3] - 8), fill=INK, width=2)
        c.text((r[2], r[3] + 14), 'Enter: keep it  ·  Esc: leave it as it was  ·  60 letters at most', font(12),
               fill=QUIET, anchor='rm')
    else:
        c.text((r[0] + 10, (r[1] + r[3]) / 2), shown or 'none — Edit writes one', font(16),
               fill=VALUE if shown else LABEL, anchor='lm')
        small_button(c, (x1 - 94, r[1], x1, r[3]), 'Edit', f=cinzel(14, 700))


def lib_page(c, r, px0, py0, px1, title, s, save_mode, kind_, editing=None, delete_q=False, new=False):
    """The chosen save's page: its map and figures, the comment (manual saves), Delete and the red button."""
    c.text((px0, py0 + 14), title, cinzel(22, 700), fill=NAME, anchor='lm')
    if kind_ == 'auto':
        w = c.width(title, cinzel(22, 700))
        c.rect((px0 + w + 16, py0 + 4, px0 + w + 16 + 52, py0 + 24), fill=AUTO_GOLD, radius=3)
        c.text((px0 + w + 42, py0 + 14), 'Auto', font(12), fill=(246, 232, 206, 255), anchor='mm')
    mh = round((px1 - px0) * 266 / 640)
    put_map(c, s, (px0, py0 + 34, px1, py0 + 34 + mh))
    stats = [('Stage', STAGES.get(s['stage'], '')), ('Budget', gold_text(s)), ('Realm', realm_text(s)),
             ('Units', str(s['units'])), ('Troops', str(s['troops'])), ('Written', 'now' if new else when(s))]
    sy = py0 + 34 + mh + 14
    for i, (label, value) in enumerate(stats):
        stat(c, px0 + (i % 3) * 280, sy + (i // 3) * 46, label, value)
    gy = sy + 100
    if kind_ == 'manual' and not new:
        comment_box(c, px0, px1, gy, s, editing is not None, editing)
    elif kind_ == 'auto':
        c.text((px0, gy + 2), 'The game writes one at the start of every week and keeps the last ten;', font(15), fill=TEXT)
        c.text((px0, gy + 24), 'the eleventh pushes the oldest out. Loading one leaves the list as it is.', font(15), fill=TEXT)
    else:
        c.text((px0, gy + 2), 'A new manual save, No. 15. Its comment can be written once it is saved.', font(15), fill=TEXT)
    by = r[3] - 82
    button = (px1 - 330, by, px1, by + 58)
    if save_mode:
        if kind_ == 'auto':
            red_button(c, button, 'Save', 'auto-saves are the game\'s own')
            c.overlay(button, (20, 16, 12, 150))
        else:
            red_button(c, button, 'Save' if new else 'Save over No. ' + str(s['no']), None if new else 'it holds ' + date_text(s['week']))
        c.text((px0, by + 16), 'Saving: ' + date_text(NOW['week']), font(15), fill=TEXT, anchor='lm')
        c.text((px0, by + 40), f"{gold_text(NOW)} · {realm_text(NOW)} · {NOW['troops']} troops", font(14), fill=QUIET, anchor='lm')
    else:
        red_button(c, button, 'Load')
        c.text((px0, by + 40), 'Right click: back to the map', font(13), fill=QUIET, anchor='lm')
    delete = (px1 - 330 - 132, by + 14, px1 - 342, by + 44)
    if not new:
        small_button(c, delete, 'Delete', 'hover' if delete_q else 'normal', f=cinzel(14, 700))
    return button, delete


def lib_frame(save_mode):
    """The panel as built (docs/ui/save-load.md, §8): the title alone, the tabs."""
    c = background()
    r = (30, 24, 1250, 776)
    lq_panel(c, r)
    title_band(c, r, 'Save' if save_mode else 'Load', '')
    tabs(c, r[2] - 220, r[1] + 11, 'Save' if save_mode else 'Load')
    return c, r


def g_screen(name, save_mode, tab, chosen, caption, note, order='Date', desc=True, editing=None, delete_q=False):
    """G: one column, the two lists as a switch over it — Manual saves (sortable, scrolling) or Auto-saves (ten,
    newest first, no order); the chosen save's page right of it."""
    autos, manual = library()
    c, r = lib_frame(save_mode)
    lx0, lx1, ly0 = r[0] + 22, r[0] + 352, r[1] + 60
    segment(c, (lx0, ly0, lx1, ly0 + 30), ('Manual saves · ' + str(len(manual)), 'Auto-saves · 10'), 0 if tab == 'manual' else 1)
    hy = ly0 + 44
    top, bottom = hy + 34, r[3] - 20
    rw = lx1 - 12
    if tab == 'manual':
        c.text((lx0, hy + 13), 'ORDER', cinzel(11, 700), fill=SUB, anchor='lm')
        sort_switch(c, lx1 + 4, hy, 'Date' if order == 'Date' else 'No.', label=False, first='No.', desc=desc)
        nos = sorted(manual, key=lambda n: manual[n]['time'] if order == 'Date' else n, reverse=desc)
        y = top
        if save_mode:
            new_row(c, (lx0, y, rw, y + 44), 'chosen' if chosen == 'new' else 'normal')
            y += 50
        for n in nos:
            if y + 52 > bottom:
                break
            lib_row(c, (lx0, y, rw, y + 52), str(n), manual[n], 'chosen' if n == chosen else 'normal', order=order)
            y += 57
        scrollbar(c, lx1 - 6, top, bottom, 0, 10 if not save_mode else 9, len(manual))
    else:
        c.text((lx0, hy + 13), 'NEWEST FIRST', cinzel(11, 700), fill=SUB, anchor='lm')
        c.text((lx1, hy + 13), 'the game keeps the last ten', font(11), fill=LABEL, anchor='rm')
        for i, s in enumerate(autos):
            y = top + i * 60
            lib_row(c, (lx0, y, lx1, y + 55), 'A', s, 'chosen' if i == chosen else 'normal', gold=True, order='Date')
    px0, py0, px1 = lx0 + 374, ly0, r[2] - 24
    if chosen == 'new':
        lib_page(c, r, px0, py0, px1, 'New save · ' + date_text(NOW['week']), NOW, save_mode, 'manual', new=True)
    elif tab == 'manual':
        s = manual[chosen]
        _, delete = lib_page(c, r, px0, py0, px1, 'No. ' + str(chosen) + ' · ' + date_text(s['week']), s, save_mode,
                             'manual', editing=editing, delete_q=delete_q)
        if delete_q:
            callout(c, delete, ['Delete No. ' + str(chosen) + ', ' + date_text(s['week']) + '?', 'It cannot be undone.'],
                    answers=('Delete', 'Keep'))
            pointer(c, delete[0] + 60, delete[1] + 18)
    else:
        s = autos[chosen]
        lib_page(c, r, px0, py0, px1, date_text(s['week']), s, save_mode, 'auto')
    c.save(name, caption, note)


def h_screen(name, save_mode, chosen, caption, note):
    """H: the two lists stacked — the ten auto-saves as short rows at the top (all of them shown), the manual saves
    under them, scrolling; one save chosen in either."""
    autos, manual = library()
    c, r = lib_frame(save_mode)
    lx0, lx1, ly0 = r[0] + 22, r[0] + 352, r[1] + 60
    c.text((lx0, ly0 + 2), 'AUTO-SAVES', cinzel(11, 700), fill=SUB, anchor='lm')
    c.text((lx1, ly0 + 2), 'newest first · the last ten', font(11), fill=LABEL, anchor='rm')
    for i, s in enumerate(autos):
        y = ly0 + 14 + i * 31
        lib_row(c, (lx0, y, lx1, y + 28), 'A', s, 'chosen' if chosen == ('auto', i) else 'normal', gold=True, compact=True)
    sy = ly0 + 14 + 10 * 31 + 8
    c.line((lx0, sy), (lx1, sy), fill=DIM)
    c.text((lx0, sy + 22), 'MANUAL SAVES · ' + str(len(manual)), cinzel(11, 700), fill=SUB, anchor='lm')
    sort_switch(c, lx1 + 4, sy + 9, 'Date', label=False, first='No.', desc=True)
    top, bottom = sy + 44, r[3] - 20
    y = top
    if save_mode:
        new_row(c, (lx0, y, lx1 - 12, y + 40), 'chosen' if chosen == 'new' else 'normal')
        y += 46
    nos = sorted(manual, key=lambda n: manual[n]['time'], reverse=True)
    for n in nos:
        if y + 52 > bottom:
            break
        lib_row(c, (lx0, y, lx1 - 12, y + 52), str(n), manual[n], 'chosen' if chosen == ('manual', n) else 'normal')
        y += 57
    scrollbar(c, lx1 - 6, top, bottom, 0, 4, len(manual))
    px0, py0, px1 = lx0 + 374, ly0, r[2] - 24
    if chosen == 'new':
        lib_page(c, r, px0, py0, px1, 'New save · ' + date_text(NOW['week']), NOW, save_mode, 'manual', new=True)
    elif chosen[0] == 'manual':
        s = manual[chosen[1]]
        lib_page(c, r, px0, py0, px1, 'No. ' + str(chosen[1]) + ' · ' + date_text(s['week']), s, save_mode, 'manual')
    else:
        s = autos[chosen[1]]
        lib_page(c, r, px0, py0, px1, date_text(s['week']), s, save_mode, 'auto')
    c.save(name, caption, note)


# ---------------------------------------------------------------- round 5: the star and the sweep (§10)

def star(c, x, y, r, fill=GOLD):
    """A five-pointed star centred at (x, y), its points at radius r."""
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        rr = r if i % 2 == 0 else r * 0.45
        pts.append((x + rr * math.cos(a), y + rr * math.sin(a)))
    c.poly(pts, fill=fill)


def notes_row(c, r, n, s, state, gone=False):
    """A manual save's row as built (§9, no order to choose); a save with a comment has a gold star on its seal's
    top left corner (the right one is too near the date). gone: a row the sweep would delete, dimmed and struck through while the question is open."""
    x0, y0, x1, y1 = r
    h = y1 - y0
    row_box(c, r, state if state in ('chosen', 'hover') else 'normal')
    sx, sy = x0 + 8, y0 + h / 2 - 12
    seal(c, sx, sy, str(n), 24)
    tx = x0 + 42
    c.text((tx, y0 + 16), date_text(s['week'], short=True), cinzel(15, 700), fill=NAME, anchor='lm')
    c.text((x1 - 10, y0 + 16), s['time'].strftime('%d %b, %H:%M'), font(14), fill=VALUE, anchor='rm')
    comment = s.get('comment', '')
    if comment:
        f = font(12)
        while c.width('“' + comment + '”', f) > x1 - tx - 22:
            comment = comment[:-2] + '…'
        c.text((tx, y0 + 37), '“' + comment.rstrip('…') + ('…' if comment.endswith('…') else '') + '”', f, fill=QUIET, anchor='lm')
        star(c, sx + 1, sy + 1, 9, fill=(26, 18, 12, 255))             # a dark rim, then the gold star over the corner
        star(c, sx + 1, sy + 1, 7)
    if gone:
        c.overlay(r, (14, 10, 8, 150))
        c.line((x0 + 6, (y0 + y1) / 2), (x1 - 6, (y0 + y1) / 2), fill=VERMILION, width=1.5)


def sweep_button(c, r, count, state='normal'):
    """Delete every manual save without a comment, counting them: quiet, as Delete; off when every save has one."""
    off = count == 0
    row_box(c, r, 'off' if off else state)
    x0, y0, x1, y1 = r
    label = 'Delete all without' if x1 - x0 < 240 else 'Delete every manual save without'
    f, tail, my = font(12), ' (%d)' % count, (y0 + y1) / 2
    x = (x0 + x1) / 2 - (c.width(label + tail, f) + 24) / 2
    colour = OFF_TEXT if off else (NAME if state == 'hover' else TEXT)
    c.text((x, my), label, f, fill=colour, anchor='lm')
    x += c.width(label, f) + 12
    star(c, x, my, 6, fill=OFF_TEXT if off else GOLD)
    c.text((x + 8, my), tail, f, fill=colour, anchor='lm')


def callout_side(c, cell, lines, answers):
    """The question beside its button, the tip pointing left at it, so the list stays in view."""
    x0, y0, x1, y1 = cell
    w, h = 440, 40 + 24 * len(lines) + 44
    left = x1 + 18
    top = min(max((y0 + y1) / 2 - h / 2, 40), H - 40 - h)
    lq_panel(c, (left, top, left + w, top + h))
    my = (y0 + y1) / 2
    c.poly([(left + 2, my - 10), (left + 2, my + 10), (left - 12, my)], fill=GOLD)
    for i, line in enumerate(lines):
        c.text((left + 22, top + 24 + 24 * i), line, font(16), fill=TEXT)
    by = top + 30 + 24 * len(lines)
    red_button(c, (left + w - 232, by, left + w - 112, by + 30), answers[0])
    small_button(c, (left + w - 102, by, left + w - 22, by + 30), answers[1])


def notes_screen(name, place, caption, note, save_mode=False, chosen=12, asking=False, swept=False):
    """The screen as built (§9) with the star on a commented save's seal and the sweep: in the list's heading
    (place 'head', where "as many as you keep" was) or under the list, the width of a row (place 'foot')."""
    _, manual = library()
    if swept:
        manual = {n: s for n, s in manual.items() if s.get('comment')}
    c, r = lib_frame(save_mode)
    lx0, lx1, ly0 = 52, 382, 84
    segment(c, (lx0, ly0, lx1, ly0 + 30), ('Manual saves · ' + str(len(manual)), 'Auto-saves · 10'), 0)
    c.text((lx0, 141), 'NEWEST FIRST', cinzel(11, 700), fill=SUB, anchor='lm')
    bare = [n for n, s in manual.items() if not s.get('comment')]
    top, bottom, rw = 162, 756, lx0 + 318
    if place == 'head':
        button = (lx1 - 176, 128, lx1, 154)
    else:
        bottom = 712
        button = (lx0, 722, rw, 752)
        c.text((lx1, 141), 'as many as you keep', font(11), fill=LABEL, anchor='rm')
    y = top
    if save_mode:
        new_row(c, (lx0, y, rw, y + 44), 'normal')
        y += 50
    shown = 0
    for n in sorted(manual, key=lambda n: manual[n]['time'], reverse=True):
        if y + 52 > bottom:
            break
        notes_row(c, (lx0, y, rw, y + 52), n, manual[n], 'chosen' if n == chosen else 'normal', gone=asking and n in bare)
        y += 57
        shown += 1
    scrollbar(c, lx1 - 6, top, bottom, 0, shown, max(shown, len(manual)))
    sweep_button(c, button, len(bare), 'hover' if asking else 'normal')
    s = manual[chosen]
    lib_page(c, r, lx0 + 374, ly0, r[2] - 24, 'No. ' + str(chosen) + ' · ' + date_text(s['week']), s, save_mode, 'manual')
    if asking:
        callout_side(c, button, ['Delete the %d manual saves without a comment?' % len(bare),
                                 'The %d with one (★) stay. It cannot be undone.' % (len(manual) - len(bare))],
                     ('Delete %d' % len(bare), 'Keep'))
        pointer(c, button[0] + 90, button[1] + 14)
    c.save(name, caption, note)


if ROUND == 'library':
    made_up = 'The saves are made up from the owner\'s slots.'
    g_screen('G1-manual-load.png', False, 'manual', 12, 'G1 — two lists as a switch: Manual saves (Load)',
             'Fourteen manual saves, newest first, scrolling; a row\'s second line is its comment. ' + made_up)
    g_screen('G2-auto-load.png', False, 'auto', 0, 'G2 — the switch on Auto-saves (Load)',
             'The ten the game keeps, newest first, no order to choose; each can be deleted. ' + made_up)
    g_screen('G3-new-save.png', True, 'manual', 'new', 'G3 — Save: "New save" first, then the saves to save over',
             'A save never has to replace another; the list grows as long as the player wants. ' + made_up)
    g_screen('G4-delete.png', True, 'manual', 12, 'G4 — Delete asks first, in a callout at the button',
             'The same in Load and Save, for a manual save and an auto-save alike. ' + made_up, delete_q=True)
    g_screen('G5-comment.png', False, 'manual', 12, 'G5 — the comment written in place (Edit)',
             'Enter keeps it, Esc leaves the old one; the row shows it at once. ' + made_up,
             editing='try the Monsters\' Cave with Rance alone, then Oaks')
    g_screen('G6-auto-save-tab.png', True, 'auto', 2, 'G6 — Save on the Auto-saves side: no Save, Delete only',
             'Auto-saves are the game\'s own: the red button is off here, Delete still works. ' + made_up)
    h_screen('H1-stacked-load.png', False, ('manual', 12), 'H1 — the two lists stacked (Load)',
             'All ten auto-saves as short rows on top; the manual saves under them, about four in view. ' + made_up)
    h_screen('H2-stacked-auto.png', False, ('auto', 1), 'H2 — stacked, an auto-save chosen (Load)',
             'One chosen save across both lists; the page is the same as G\'s. ' + made_up)
    h_screen('H3-stacked-save.png', True, 'new', 'H3 — stacked (Save): "New save" at the top of the manual saves',
             'Fewer manual saves in view than in G: the auto-saves take the column\'s top half. ' + made_up)
elif ROUND == 'notes':
    made_up = 'Made-up saves: five of the fourteen have a comment.'
    notes_screen('N1-star-head.png', 'head', 'N1 — a star on a commented save\'s seal; the sweep in the list\'s heading',
                 'The button takes the place of "as many as you keep" and counts the saves it would delete. ' + made_up)
    notes_screen('N2-star-foot.png', 'foot', 'N2 — the same star; the sweep under the list, the width of a row',
                 'One row fewer in view; the heading keeps its line. ' + made_up)
    notes_screen('N3-ask-head.png', 'head', 'N3 — N1 asking: the question beside the button, the saves to go struck',
                 'Delete 9 deletes them; Keep (or a right click) leaves everything. ' + made_up, asking=True)
    notes_screen('N4-ask-foot.png', 'foot', 'N4 — N2 asking: the same question beside the button at the foot',
                 'The rows to go are dimmed and struck through while the question is open. ' + made_up, asking=True)
    notes_screen('N5-after.png', 'head', 'N5 — after Delete 9: the five saves with a comment, the button off',
                 'Lit again once there is a manual save without a comment. ' + made_up, swept=True)
    notes_screen('N6-save-head.png', 'head', 'N6 — N1 in Save: the button works in Save and Load alike',
                 '"+ New save" heads the list as built. ' + made_up, save_mode=True)
elif ROUND == 'sort':
    for n, (day, hour, minute) in SORT_TIMES.items():
        if n in SLOT:
            SLOT[n]['time'] = datetime.datetime(2026, 10, day, hour, minute)
    saving = {n: s for n, s in SLOT.items() if n != 7}               # one slot emptied to show where the empty ones go
    made_up = 'The times are made up for the mock-up.'
    d_screen('D1-number.png', 'No.', False, SLOT, 9, 'D1 — sorted by number (Load), the switch in the title band',
             'As built, plus "Order: No. | Date" left of the tabs. ' + made_up)
    d_screen('D2-date.png', 'Date', False, SLOT, 9, 'D2 — sorted by date (Load): Auto first, then the newest',
             'Auto stays the first row; the rest newest first, the time the bright line. ' + made_up)
    d_screen('D3-date-save.png', 'Date', True, saving, 7, 'D3 — sorted by date (Save): the empty slots at the end',
             'Slot 6 emptied for the mock-up: by date the empty slots go last, in number order. ' + made_up)
    e_screen('E1-number.png', 'No.', False, SLOT, 9, 'E1 — Auto in a panel of its own (Load, by number)',
             'Auto over the list in a gold frame; "Your saves" with the switch next to them, nine rows. ' + made_up)
    e_screen('E2-date.png', 'Date', False, SLOT, 9, 'E2 — Auto in a panel of its own (Load, by date)',
             'The nine sorted newest first; Auto stays in its panel whatever the order. ' + made_up)
    e_screen('E3-date-save.png', 'Date', True, saving, 7, 'E3 — Auto in a panel of its own (Save, by date)',
             'Auto greyed (the game writes it), the empty slots last. Slot 6 emptied for the mock-up. ' + made_up)
    # Round 3: the owner's E, the first button "Slot", each button two directions (a second click turns it).
    e3 = dict(heading='MANUAL SAVES', first='Slot')
    e_screen('F1-slot-up.png', 'Slot', False, SLOT, 9, 'F1 — E as chosen: by slot, 1 at the top (the default)',
             'As the original lists them. "Slot" for "No.", "Manual saves" for "Your saves". ' + made_up, desc=False, **e3)
    e_screen('F2-slot-down.png', 'Slot', False, SLOT, 9, 'F2 — Slot clicked again: 9 at the top',
             'The active button shows its direction; a second click turns it. ' + made_up, desc=True, **e3)
    e_screen('F3-date-down.png', 'Date', False, SLOT, 9, 'F3 — by date: the newest at the top',
             'Date opens newest first; the empty slots stay last either way. ' + made_up, desc=True, **e3)
    e_screen('F4-date-up.png', 'Date', True, saving, 7, 'F4 — Date clicked again: the oldest at the top (Save)',
             'Slot 6 emptied for the mock-up: it stays last. The order is kept across launches. ' + made_up, desc=False, **e3)
else:
    for save_mode in (False, True):
        a_screen(save_mode)
        b_screen(save_mode)
        c_screen(save_mode)
