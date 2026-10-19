"""Mock-ups of our Harem (Sﾊｰﾚﾑ) and Treasure (Sｱｲﾃﾑ) screens in the lacquer look of our Subordinates and deployment
screens (docs/ui/subordinates.md, §5; docs/ui/deployment-table.md, "The lacquer look"; docs/ui/pickers.md).

The state is the kept save saves-week18 (week 18), read from its slot; the original's screens are dumps of the same
save (the harem picker, headless) and of an unattended new game at week 26 (the item screen, headless):
  <Godot console exe> --headless --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look-harem
      --slots-from user://saves-week18 --harem 6 --until deployment --shots <harem dir>
  <Godot console exe> --headless --path godot -- --newgame --auto --fast --quit --seed 2 --weeks 27
      --saves user://saves-look-items --shots <items dir>
(the harem's first "picker" dump; the item screen's first "picker" dump in the item view). Then, from the repository's
root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_pickers.py <slot01.ksav> <cg_png dir> <harem dump> <items dump> <out dir> [s|t|u|v|w] < /dev/null
Round 1 (no sixth argument): H0-H3, I0-I3; round s: S1-S5, the treasure in Sengoku Rance's manner; round t: T1-T3, S5 with
icons of our own (emblems, or squares cut out of the items' pictures) and an item-first screen of picture cards; round u:
U1-U3, a showcase of one item, character cards with a chooser, the Subordinates ledger with an Item column; round v:
V1-V3, the original's screen in lacquer, as the harem's H2; round w: W1-W4b, the script's question and its Yes/No in one
place over V (a dialog, a callout at the cell, the answers in the message window, the info panel). Rounds s-w read
neither dump.
The images hold the player's CGs: keep <out dir> outside the repository. Everything is in the 1280x800 screen's pixels
(plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os, re, struct
from PIL import Image, ImageDraw, ImageFont

SAVE, CGS, HAREM_DUMP, ITEMS_DUMP, OUT = sys.argv[1:6]
sys.stdout.reconfigure(encoding='utf-8')
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
PLUM, PLUM_DARK = (128, 44, 70, 255), (70, 22, 38, 255)        # the command panel's Harem colour

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


# ---------------------------------------------------------------- the save (Script/SaveData's slot format)

def read_save(path):
    gen = open(os.path.join(REPO, 'src/Kichikuou.Original/State/GameState.g.cs'), encoding='utf-8').read()

    def record(name):
        body = gen[gen.index('public sealed class ' + name):]
        body = body[:body.index('\n}\n')]
        return {m.group(1): (int(m.group(2)), int(m.group(3))) for m in re.finditer(
            r'public Sys3Int (\w+)\s*\{\s*get => memory\.ReadPage\((\d+), (\d+) \+ index\)', body)}

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
    i32(), rstr(), i32()
    variables = vals()
    pages = {}
    for _ in range(i32()):
        p = i32()
        pages[p] = vals()
    strings = [rstr() for _ in range(i32())]

    def rec(fields, idx):
        return {k: pages[p][o + idx] for k, (p, o) in fields.items() if p in pages and o + idx < len(pages[p])}

    uf, itf, hf = record('UnitRecord'), record('ItemRecord'), record('HistRecord')
    ub, ib = variables[198], variables[201]
    units = {}
    for n in range(1, 51):
        r = rec(uf, n)
        if r.get('Status'):
            r.update(Index=n, Name=strings[ub + (n - 1) * 3], Type=strings[ub + (n - 1) * 3 + 1])
            units[n] = r
    items = {n: dict(rec(itf, n), Index=n, Name=strings[ib + n - 1]) for n in range(1, 51)}
    hist = {n: rec(hf, n) for n in range(1, 51)}
    return units, items, hist, variables[517], variables[519]


UNITS, ITEMS, HIST, WEEK, GOLD_K = read_save(SAVE)

# Sﾊｰﾚﾑ.ADV's string table (L_0084c): the girls by their picker number.
GIRLS = ['Sill', 'Lia', 'Maris', 'Suzume', 'Wendy', 'Kanami', 'Leila', 'Menad', 'Haurein', 'Melfeis', 'Soul', 'Anasel',
         'Royal Guards', 'Maria', 'Shizuka', 'Ran', 'Milli', 'Mill', 'Mokomoko', 'Sayori Unga', 'Merim', 'Raylee', 'Aoi',
         'Shariela', 'Pink Cow', 'Amiran', 'Katyusha', 'Saulnia', 'Satella', 'Julia', 'Rafalia', 'Arlcoate', 'Cream',
         'Ruberan', 'Sheila', 'Chizuko', 'Magic', 'Nagi', 'Makoto', 'Warg', 'Kaoru', 'Wichita', 'Elisabeth', 'Isoroku',
         'Merci', 'Kou', 'Rebecca', 'Annis', 'Elena', 'Saitou']
# The girls Cﾊｰﾚﾑ offers at the save's week (the harem dump: Lia, Maris, Kanami, Haurein); her unit, if she leads one.
OFFER = [2, 3, 6, 9]
GIRL_UNIT = {7: 5, 8: 10, 9: 12, 1: 2, 11: 4, 14: 16, 15: 17, 16: 23, 17: 24, 18: 25}
# A later game's offer for the layout's sake (made up: who is on offer and the counts are not from a save).
LATE_OFFER = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 15, 16, 17, 18, 21, 22, 23, 26, 33]
LATE_HIST = {1: (6, 70), 2: (9, 77), 3: (4, 61), 4: (2, 52), 5: (1, 44), 6: (5, 73), 7: (3, 66), 8: (2, 58), 9: (7, 76),
             10: (1, 40), 11: (3, 64), 12: (4, 69), 14: (2, 71), 15: (3, 74), 16: (1, 49), 17: (2, 63), 18: (0, 0),
             21: (1, 67), 22: (0, 0), 23: (1, 72), 26: (0, 0), 33: (1, 75)}
LATE_WEEK = 78
FIXED = (1, 2)                  # Chaos and Nikkou never move (Sｱｲﾃﾑ.ADV:240-375)
# A later game's stock for the layout's sake (made up): item -> holder (100 the stock).
LATE_STOCK = {1: 1, 6: 6, 7: 7, 3: 100, 10: 9, 21: 100, 23: 12, 11: 100, 43: 100, 12: 100, 35: 8, 48: 100}

# UnitFaces: the owner's squares by face CG (x, y, size; 24, 7, 144 otherwise).
CROPS = {1: (33, 7, 128), 7: (28, 7, 136), 8: (31, 7, 128), 10: (32, 7, 128), 402: (15, 9, 136), 403: (44, 7, 136),
         404: (28, 7, 128), 406: (38, 7, 112), 430: (26, 45, 144)}


def date(week):
    """L_007c3: 48 weeks a year from LP 3, 4 weeks a month."""
    y, w = divmod(week - 1, 48)
    m, w = divmod(w, 4)
    return 3 + y, m + 1, w + 1


def date_text(week, short=False):
    if not week:
        return '—'
    y, m, w = date(week)
    return f'LP {y} · M{m} · W{w}' if short else f'LP {y} · Month {m} · Week {w}'


# ---------------------------------------------------------------- drawing

class Canvas:
    def __init__(self, clear=False):
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (0, 0, 0, 0) if clear else (14, 10, 8, 255))
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


def cg(n):
    return Image.open(os.path.join(CGS, 'CG%04d.png' % n)).convert('RGBA')


def picture(n, background, at=(8, 6), with_empty=None):
    """The picture panel as the script draws it: the screen's background (G 818 / 819), the CG at its own place, cut as
    FacePictures.PicturePanel cuts it: script pixels (6, 6), 314x196."""
    screen = cg(background).copy()
    if with_empty:
        screen.alpha_composite(cg(with_empty), (8, 11))
    if n:
        screen.alpha_composite(cg(n), at)
    return screen.crop((6, 6, 320, 202))


def put_picture(c, img, r):
    x0, y0, x1, y1 = r
    shadow(c, r, 16)
    c.paste(img.resize((round((x1 - x0) * S), round((y1 - y0) * S)), Image.LANCZOS), (x0, y0))


def face_image(n, size, back=(52, 40, 28), alpha=1.0):
    """A face's square (UnitFaces) as G n, 40 draws it: palette index 40 transparent, on the card's backing colour."""
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


def backdrop(c, colour=(196, 160, 86, 13), radius=34, strength=200):
    """Lacquer.DrawWaveBackdrop: a faint gold seigaiha, darkened towards the edges."""
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


def title_band(c, r, title, subtitle, right=None):
    x0, y0, x1, _ = r
    c.text((x0 + 22, y0 + 25), title, brush(24), fill=NAME, anchor='lm')
    c.text((x0 + 22 + c.width(title, brush(24)) + 14, y0 + 27), subtitle, cinzel(13, 600), fill=SUB, anchor='lm')
    if right:
        c.text((x1 - 22, y0 + 27), right, cinzel(15, 700), fill=NAME, anchor='rm')
    divider(c, x0 + 16, x1 - 16, y0 + 44)


def row_box(c, r, state='normal'):
    if state == 'off':
        c.rect(r, fill=OFF, outline=OFF_LINE, width=1)
    elif state == 'chosen':
        c.rect(r, fill=(60, 30, 22, 255), outline=VERMILION, width=2)
    else:
        hover = state == 'hover'
        c.rect(r, fill=HOVER if hover else ROW, outline=HOVER_LINE if hover else DIM, width=2 if hover else 1)


def red_button(c, r, title, sub=None, hover=False, tf=None, sf=None):
    """Lacquer.DrawRedButton: red lacquer framed in gold (as Turn END)."""
    x0, y0, x1, y1 = r
    top, bottom = ((150, 40, 30), (96, 20, 14)) if hover else ((128, 32, 24), (80, 16, 12))
    for i in range(round(y1 - y0)):
        t = i / max(1, y1 - y0 - 1)
        c.line((x0, y0 + i), (x1, y0 + i), fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,))
    c.rect(r, outline=GOLD, width=2)
    c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(150, 96, 52, 255), width=1)
    tf, sf = tf or brush(24), sf or font(14)
    cx = (x0 + x1) / 2
    if sub:
        c.text((cx, y0 + (y1 - y0) * 0.40), title, tf, fill=NAME, anchor='mm')
        c.text((cx, y0 + (y1 - y0) * 0.76), sub, sf, fill=(236, 206, 160, 255), anchor='mm')
    else:
        c.text((cx, (y0 + y1) / 2), title, tf, fill=NAME, anchor='mm')


def small_button(c, r, label, state='normal', f=None):
    row_box(c, r, state)
    x0, y0, x1, y1 = r
    c.text(((x0 + x1) / 2, (y0 + y1) / 2), label, f or cinzel(16, 700),
           fill=OFF_TEXT if state == 'off' else (NAME if state == 'hover' else TEXT), anchor='mm')


def seal(c, x, y, s, size=20, fill=VERMILION):
    c.rect((x, y, x + size, y + size), fill=fill, radius=3)
    c.rect((x + 2, y + 2, x + size - 2, y + size - 2), outline=(236, 196, 170, 140), radius=2)
    c.text((x + size / 2, y + size / 2 + 1), s, cinzel(round(size * 0.62), 700), fill=(246, 230, 206, 255), anchor='mm')


def heart(c, x, y, r, fill):
    """A small heart (the harem's mark), drawn: two circles and a point."""
    c.d.ellipse([(x - r) * S, (y - r * 0.6) * S, x * S, (y + r * 0.4) * S], fill=fill)
    c.d.ellipse([x * S, (y - r * 0.6) * S, (x + r) * S, (y + r * 0.4) * S], fill=fill)
    c.poly([(x - r * 0.97, y + 0.0), (x + r * 0.97, y + 0.0), (x, y + r * 1.05)], fill=fill)


def lock(c, x, y, fill):
    """A small padlock: an item that never moves."""
    c.d.arc([(x - 4) * S, (y - 9) * S, (x + 4) * S, (y - 1) * S], 180, 360, fill=fill, width=round(1.6 * S))
    c.rect((x - 6, y - 4, x + 6, y + 5), fill=fill, radius=1)


def pointer(c, x, y):
    p = [(x, y), (x, y + 22), (x + 6, y + 17), (x + 10, y + 26), (x + 14, y + 24), (x + 10, y + 15), (x + 17, y + 15)]
    c.poly(p, fill=(255, 255, 255, 255), outline=(0, 0, 0, 255))


def ledger(c, r, columns, rows, row_h=27, size=15, chosen=None, hover=None, greyed=(), marks=None, sort=None):
    """A paper ledger (DeploymentTableView with ledger: true): headers in Cinzel in vermilion ink over a rule, the rows'
    rules, the chosen row faintly vermilion with a vermilion bar and diamond in the first column. columns: (label, width,
    key, align); rows: (id, {key: text}); marks: id -> a seal's text in the first column."""
    x0, y0, x1, y1 = r
    t = Canvas(clear=True)
    t.rect(r, fill=PAPER, outline=DIM)
    head = row_h - 1
    t.rect((x0, y0, x1, y0 + head), fill=PAPER_HEAD)
    t.line((x0, y0 + head), (x1, y0 + head), fill=HEAD_INK)
    x = x0 + 4
    for label, w, key, align in columns:
        if align == 'l':
            t.text((x + 4, y0 + head / 2), label, cinzel(13, 700), fill=HEAD_INK, anchor='lm')
        elif label:
            mark = 14 if label == sort else 0
            t.text((x + w - 6 - mark, y0 + head / 2), label, cinzel(13, 700), fill=HEAD_INK, anchor='rm')
            if mark:
                t.text((x + w - 4, y0 + head / 2), '↑', font(12), fill=HEAD_INK, anchor='rm')
        x += w
    y = y0 + head + 2
    for rid, vals in rows:
        mid = y + row_h / 2
        if rid == chosen:
            t.overlay((x0 + 1, y, x1 - 1, y + row_h), (170, 40, 28, 40))
            t.rect((x0 + 1, y, x0 + 4, y + row_h), fill=VERMILION)
        elif rid == hover:
            t.overlay((x0 + 1, y, x1 - 1, y + row_h), (170, 40, 28, 18))
        t.line((x0 + 6, y + row_h), (x1 - 6, y + row_h), fill=PAPER_RULE)
        grey = rid in greyed
        x = x0 + 4
        for label, w, key, align in columns:
            v = vals.get(key, '')
            if key == 'mark':
                if marks and rid in marks:
                    seal(t, x + (w - 18) / 2, mid - 9, marks[rid], 18)
                elif rid == chosen:
                    diamond(t, x + w / 2, mid, 6, VERMILION)
            elif key == 'name':
                t.text((x + 4, mid), v, font(size), fill=INK_GREY if grey else INK, anchor='lm')
            elif align == 'l':
                ink = INK_GREY if grey else (VERMILION_INK if v.startswith('!') else INK_QUIET)
                t.text((x + 4, mid), v.lstrip('!'), font(size - 1), fill=ink, anchor='lm')
            else:
                t.text((x + w - 6, mid), v, font(size), fill=INK_GREY if grey else INK, anchor='rm')
            x += w
        y += row_h
    fit = int((y1 - y0 - head - 2) // row_h)
    if len(rows) > fit:                                 # more rows than the paper holds: a scroll bar of paper and ink
        t.rect((x1 - 12, y0 + head + 4, x1 - 4, y1 - 4), fill=PAPER_HEAD)
        span = y1 - y0 - head - 8
        t.rect((x1 - 12, y0 + head + 4, x1 - 4, y0 + head + 4 + span * fit / len(rows)), fill=INK_QUIET)
    c.paste(t.im.crop((x0 * S, y0 * S, x1 * S, y1 * S)), (x0, y0))


def stat(c, x, y, label, value, vf=None, lf=None):
    c.text((x, y), label, lf or cinzel(11, 700), fill=LABEL, anchor='lm')
    c.text((x, y + 20), value, vf or font(18), fill=VALUE, anchor='lm')


def original(dump, name, caption, note):
    c = Canvas()
    c.paste(Image.open(dump).convert('RGBA').resize((W * S, H * S), Image.NEAREST), (0, 0))
    c.save(name, caption, note)


# ---------------------------------------------------------------- the harem

HEADER_DATE = date_text(WEEK)


def girl_rows(offer, hist):
    rows = []
    for g in offer:
        count, last = hist(g)
        unit = UNITS.get(GIRL_UNIT.get(g, 0))
        rows.append((g, {'n': str(g), 'name': GIRLS[g - 1], 'count': str(count) if count else '—',
                         'last': date_text(last) if last else '!Never',
                         'unit': f"{unit['Name']}'s unit · {unit['Type']}" if unit else '—'}))
    return rows


def save_hist(g):
    h = HIST[g]
    return h.get('Harem_SexCount', 0), h.get('Harem_LastWeek', 0)


def late_hist(g):
    return LATE_HIST.get(g, (0, 0))


def harem_info(c, r, g, hist, offer_count):
    """The chosen girl's panel: her name, her figures, Summon, End."""
    x0, y0, x1, y1 = r
    lq_panel(c, r)
    c.text((x0 + 22, y0 + 26), 'Summon', brush(22), fill=NAME, anchor='lm')
    c.text((x1 - 22, y0 + 28), '1 visit this week', cinzel(13, 700), fill=SUB, anchor='rm')
    divider(c, x0 + 16, x1 - 16, y0 + 46)
    count, last = hist(g)
    heart(c, x0 + 34, y0 + 84, 11, PLUM)
    c.text((x0 + 56, y0 + 84), GIRLS[g - 1], brush(34), fill=NAME, anchor='lm')
    stat(c, x0 + 26, y0 + 136, 'Visits', str(count) if count else 'None yet', font(20))
    stat(c, x0 + 200, y0 + 136, 'Last summoned', date_text(last) if last else 'Never', font(20))
    unit = UNITS.get(GIRL_UNIT.get(g, 0))
    stat(c, x0 + 26, y0 + 196, 'Leads', f"{unit['Name']}'s unit · {unit['Type']}" if unit else 'No unit', font(17))
    red_button(c, (x0 + 18, y1 - 150, x1 - 18, y1 - 72), f'Summon {GIRLS[g - 1]}', 'Her scene plays · the week\'s visit',
               hover=True, tf=brush(26))
    small_button(c, (x1 - 18 - 140, y1 - 60, x1 - 18, y1 - 18), 'End')


def h1_ledger():
    c = Canvas()
    backdrop(c)
    top = (16, 16, 1264, 352)
    lq_panel(c, top)
    title_band(c, top, 'Harem', f'{len(OFFER)} girls on offer', HEADER_DATE)
    cols = [('', 30, 'mark', 'r'), ('No.', 46, 'n', 'r'), ('Name', 220, 'name', 'l'), ('Visits', 90, 'count', 'r'),
            ('Last summoned', 300, 'last', 'l'), ('Leads', 520, 'unit', 'l')]
    ledger(c, (30, 66, 1250, 338), cols, girl_rows(OFFER, save_hist), row_h=30, size=16, chosen=6)
    put_picture(c, picture(706, 818), (16, 368, 682, 784))
    harem_info(c, (690, 368, 1264, 784), 6, save_hist, len(OFFER))
    c.save('H1-ledger.png', 'H1 — the ledger (the Subordinates screen\'s layout)',
           'The girls on offer as a paper ledger across the top; her picture below left; Summon and End right. A click on a row '
           'chooses her, Summon plays her scene.')


def tablet(c, r, g, hist, state='normal', compact=False):
    """A girl's name tablet: a dark lacquer plaque, a plum heart, her name in the brush face, her visits and last date."""
    x0, y0, x1, y1 = r
    row_box(c, r, state)
    if state == 'hover':
        c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=GOLD, width=1)
    count, last = hist(g)
    heart(c, x0 + 18, y0 + 20, 7, PLUM if count else (90, 60, 66, 255))
    c.text((x0 + 32, y0 + 20), GIRLS[g - 1], brush(17 if not compact else 15), fill=NAME if state == 'hover' else TEXT,
           anchor='lm')
    line = y1 - 15
    c.text((x0 + 14, line), f'{count}×' if count else 'new', cinzel(12, 700), fill=QUIET if count else GREEN, anchor='lm')
    c.text((x1 - 12, line), date_text(last, short=True) if last else '—', font(12), fill=QUIET, anchor='rm')


def h2_tablets(offer=OFFER, hist=save_hist, hovered=6, name='H2-tablets.png', week=WEEK, late=False):
    c = Canvas()
    backdrop(c)
    top = (16, 16, 1264, 72)
    lq_panel(c, top)
    c.text((38, 44), 'Harem', brush(24), fill=NAME, anchor='lm')
    c.text((38 + c.width('Harem', brush(24)) + 14, 46), f'{len(offer)} girls on offer · 1 visit this week', cinzel(13, 600),
           fill=SUB, anchor='lm')
    c.text((1242, 46), date_text(week), cinzel(15, 700), fill=NAME, anchor='rm')
    put_picture(c, picture(700 + hovered, 818), (16, 88, 682, 504))
    info = (16, 516, 682, 784)
    lq_panel(c, info)
    count, last = hist(hovered)
    heart(c, 44, 556, 11, PLUM)
    c.text((66, 556), GIRLS[hovered - 1], brush(32), fill=NAME, anchor='lm')
    stat(c, 42, 604, 'Visits', str(count) if count else 'None yet', font(20))
    stat(c, 170, 604, 'Last summoned', date_text(last) if last else 'Never', font(20))
    unit = UNITS.get(GIRL_UNIT.get(hovered, 0))
    stat(c, 480, 604, 'Leads', f"{unit['Name']}'s unit" if unit else 'No unit', font(18))
    red_button(c, (34, 668, 664, 766), f'Summon {GIRLS[hovered - 1]}', 'A click on her tablet does the same',
               hover=False, tf=brush(28))
    grid = (690, 88, 1264, 784)
    lq_panel(c, grid)
    gx0, gy0 = 708, 106
    cols = 3
    tw, th, gap = 176, 56, 8
    for i, g in enumerate(offer):
        x = gx0 + (i % cols) * (tw + gap)
        y = gy0 + (i // cols) * (th + gap)
        tablet(c, (x, y, x + tw, y + th), g, hist, 'hover' if g == hovered else 'normal')
    small_button(c, (1264 - 18 - 140, 784 - 60, 1264 - 18, 784 - 18), 'End')
    if late:
        i = offer.index(hovered)
        pointer(c, gx0 + (i % cols) * (tw + gap) + 92, gy0 + (i // cols) * (th + gap) + 26)
        c.save(name, 'H2b — the tablets with a later game\'s offer (made up)',
               f'{len(offer)} girls, their counts and dates invented for the layout: three columns hold 33 before a scroll. '
               'Plum heart = summoned before, "new" = never.')
    else:
        c.save(name, 'H2 — name tablets, her picture large',
               'Each girl a lacquer tablet (her visits, the last date); the hovered one\'s picture and figures left; a click on a '
               'tablet summons her at once, as the original\'s pick.')


def h3_stage():
    c = Canvas()
    backdrop(c)
    pic = (176, 16, 1104, 596)          # the CG x2.95: 928x580
    put_picture(c, picture(709, 818), pic)
    strip = (16, 612, 1264, 784)
    lq_panel(c, strip)
    c.text((38, 638), 'Harem', brush(22), fill=NAME, anchor='lm')
    c.text((38 + c.width('Harem', brush(22)) + 12, 640), f'{len(OFFER)} on offer · 1 visit this week · {HEADER_DATE}',
           cinzel(12, 600), fill=SUB, anchor='lm')
    divider(c, 32, 1248, 658)
    x = 34
    for g in OFFER:
        tablet(c, (x, 672, x + 176, 728), g, save_hist, 'hover' if g == 9 else 'normal')
        x += 184
    for ax, sx in ((x + 14, -1), (x + 40, 1)):          # the strip's scroll arrows, greyed while it fits
        c.poly([(ax - 7 * sx, 700 - 9), (ax + 7 * sx, 700), (ax - 7 * sx, 700 + 9)], fill=OFF_LINE)
    red_button(c, (936, 672, 1100, 766), 'Summon', 'Haurein', hover=False, tf=brush(24))
    small_button(c, (1110, 672, 1246, 766), 'End')
    c.text((34, 754), 'Hover a tablet: her picture above. Click it, or Summon.', font(13), fill=QUIET, anchor='lm')
    c.save('H3-stage.png', 'H3 — her picture as the stage, the girls in a strip below',
           'The picture at nearly 3x over the top; the girls a strip of tablets that scrolls when they outgrow it; Summon and End '
           'at its right.')


# ---------------------------------------------------------------- the treasure

def holder_text(item, short=False):
    h = item['Holder']
    if item['Index'] in FIXED and h:
        return f"{UNITS[h]['Name']} · fixed" if h in UNITS else 'fixed'
    if h == 100:
        return 'In stock'
    if h in UNITS:
        return UNITS[h]['Name'] if short else f"On {UNITS[h]['Name']}"
    return '—'


def owned(items):
    return [it for it in items.values() if it['Holder']]


def items_with(stock):
    out = {n: dict(it) for n, it in ITEMS.items()}
    for n in out:
        out[n]['Holder'] = stock.get(n, 0)
    return out


def unit_rows(item_of):
    rows = []
    for n, u in UNITS.items():
        it = item_of.get(n)
        rows.append((n, {'n': str(n), 'name': u['Name'], 'type': u['Type'],
                         'item': it['Name'] if it else '—'}))
    return rows


def item_rows(items):
    rows = []
    for it in sorted(owned(items), key=lambda i: i['Index']):
        kind = 'Fixed' if it['Index'] in FIXED else ('Equip' if it['Effect'] else '!Can\'t equip')
        rows.append((it['Index'], {'n': str(it['Index']), 'name': it['Name'], 'kind': kind, 'held': holder_text(it, True)}))
    return rows


def item_of(items):
    return {it['Holder']: it for it in owned(items) if it['Holder'] in UNITS}


def unit_picture(n):
    return picture(UNITS[n]['PortraitCg'], 819, at=(8, 11), with_empty=600)


def item_picture(n):
    return picture(750 + n, 819)


def caption_plate(c, r, title, sub):
    x0, y0, x1, y1 = r
    c.text((x0 + 4, y0 + 10), title, cinzel(12, 700), fill=LABEL, anchor='lm')
    c.text((x1 - 4, y0 + 10), sub, font(13), fill=QUIET, anchor='rm')


def actions(c, r, unit, item, items):
    """The orders: Equip (the item on the unit; from its holder), Unequip (back to stock), End."""
    x0, y0, x1, y1 = r
    lq_panel(c, r)
    c.text((x0 + 20, y0 + 26), 'Orders', brush(20), fill=NAME, anchor='lm')
    divider(c, x0 + 14, x1 - 14, y0 + 46)
    it = items[item]
    u = UNITS[unit]
    c.text((x0 + 20, y0 + 70), u['Name'], font(17), fill=TEXT, anchor='lm')
    c.text((x0 + 20, y0 + 92), f"{item_of(items)[unit]['Name']} back to stock" if unit in item_of(items) else 'holds nothing',
           font(13), fill=QUIET, anchor='lm')
    c.text((x0 + 20, y0 + 122), it['Name'], font(17), fill=TEXT, anchor='lm')
    c.text((x0 + 20, y0 + 144), holder_text(it), font(13), fill=QUIET, anchor='lm')
    from_unit = it['Holder'] in UNITS and it['Holder'] != unit
    red_button(c, (x0 + 14, y0 + 170, x1 - 14, y0 + 246), 'Equip',
               f"from {UNITS[it['Holder']]['Name']}" if from_unit else 'from the stock', hover=True, tf=brush(24))
    small_button(c, (x0 + 14, y0 + 256, x1 - 14, y0 + 296), f"Unequip {u['Name']}", 'normal' if unit in item_of(items) else 'off',
                 cinzel(13, 700))
    small_button(c, (x1 - 14 - 110, y1 - 54, x1 - 14, y1 - 14), 'End')


def i1_two_ledgers(items=ITEMS, unit=6, item=6, name='I1-two-ledgers.png', late=False):
    c = Canvas()
    backdrop(c)
    top = (16, 16, 1264, 400)
    lq_panel(c, top)
    title_band(c, top, 'Treasure', f'{len(owned(items))} items · {len(UNITS)} characters', HEADER_DATE)
    c.text((34, 78), 'Characters', cinzel(12, 700), fill=LABEL, anchor='lm')
    c.text((660, 78), 'Items', cinzel(12, 700), fill=LABEL, anchor='lm')
    ucols = [('', 26, 'mark', 'r'), ('No.', 40, 'n', 'r'), ('Name', 110, 'name', 'l'), ('Type', 170, 'type', 'l'),
             ('Item', 260, 'item', 'l')]
    ledger(c, (30, 90, 640, 386), ucols, unit_rows(item_of(items)), row_h=29, chosen=unit)
    icols = [('', 26, 'mark', 'r'), ('No.', 40, 'n', 'r'), ('Name', 180, 'name', 'l'), ('Kind', 120, 'kind', 'l'),
             ('Held by', 220, 'held', 'l')]
    rows = item_rows(items)
    ledger(c, (650, 90, 1250, 386), icols, rows, row_h=29, chosen=item,
           greyed=[rid for rid, v in rows if v['kind'] in ('Fixed', '!Can\'t equip')])
    put_picture(c, unit_picture(unit), (16, 424, 506, 730))
    caption_plate(c, (16, 736, 506, 756), 'Character', UNITS[unit]['Name'])
    put_picture(c, item_picture(item), (522, 424, 1012, 730))
    caption_plate(c, (522, 736, 1012, 756), 'Item', items[item]['Name'])
    actions(c, (1028, 424, 1264, 784), unit, item, items)
    if late:
        c.save(name, 'I1b — the two ledgers with a later game\'s stock (made up)',
               'Twelve items for the layout (invented): Chaos fixed on Rance, the unequippable ones in grey ink, the stock "In stock".')
    else:
        c.save(name, 'I1 — two ledgers: characters and items side by side',
               'Choose a character and an item (both vermilion), then Equip; the item leaves its holder at once (the original asks Yes/No). '
               'The two pictures below, the orders right.')


def unit_card(c, r, n, items, state='normal'):
    """A character with an item slot: face, name, type, the slot (the item, or empty)."""
    x0, y0, x1, y1 = r
    row_box(c, r, 'hover' if state == 'drop' else 'normal')
    fs = y1 - y0 - 16
    c.rect((x0 + 8, y0 + 8, x0 + 8 + fs, y0 + 8 + fs), fill=(52, 40, 28, 255))
    c.paste(face_image(UNITS[n]['FaceCg'], fs), (x0 + 8, y0 + 8))
    c.rect((x0 + 8, y0 + 8, x0 + 8 + fs, y0 + 8 + fs), outline=GOLD, width=1)
    tx = x0 + fs + 18
    c.text((tx, y0 + 18), UNITS[n]['Name'], font(17), fill=NAME, anchor='lm')
    c.text((tx, y0 + 38), UNITS[n]['Type'], font(12), fill=QUIET, anchor='lm')
    slot = (tx, y1 - 38, x1 - 10, y1 - 10)
    it = item_of(items).get(n)
    if state == 'drop':
        for i in range(6, 0, -1):
            c.overlay((slot[0] - i, slot[1] - i, slot[2] + i, slot[3] + i), (232, 199, 122, 10))
        c.rect(slot, fill=(80, 60, 30, 255), outline=HOVER_LINE, width=2)
        c.text(((slot[0] + slot[2]) / 2, (slot[1] + slot[3]) / 2), 'Equip here', font(13), fill=NAME, anchor='mm')
    elif it:
        c.rect(slot, fill=(46, 34, 22, 255), outline=GOLD, width=1)
        diamond(c, slot[0] + 12, (slot[1] + slot[3]) / 2, 5, GOLD)
        c.text((slot[0] + 24, (slot[1] + slot[3]) / 2), it['Name'], font(13), fill=TEXT, anchor='lm')
        if it['Index'] in FIXED:
            lock(c, slot[2] - 12, (slot[1] + slot[3]) / 2 + 2, QUIET)
    else:
        for x in range(round(slot[0]), round(slot[2]), 10):
            c.line((x, slot[1]), (min(x + 6, slot[2]), slot[1]), fill=DIM)
            c.line((x, slot[3] - 1), (min(x + 6, slot[2]), slot[3] - 1), fill=DIM)
        c.text(((slot[0] + slot[2]) / 2, (slot[1] + slot[3]) / 2), 'no item', cinzel(11, 600), fill=DIM, anchor='mm')


def item_tablet(c, r, it, state='normal'):
    x0, y0, x1, y1 = r
    fixed, cant = it['Index'] in FIXED, not it['Effect']
    row_box(c, r, 'off' if (fixed or cant) and state == 'normal' else state)
    if state == 'dragged':
        c.overlay(r, (14, 10, 8, 160))
    mid = (y0 + y1) / 2
    diamond(c, x0 + 16, mid, 5, GOLD if not (fixed or cant) else OFF_LINE)
    c.text((x0 + 28, mid), it['Name'], font(15), fill=OFF_TEXT if fixed or cant else TEXT, anchor='lm')
    sub = 'never moves' if fixed else ("can't be equipped" if cant else holder_text(it))
    c.text((x1 - (30 if fixed else 12), mid), sub, font(12), fill=OFF_TEXT if fixed or cant else QUIET, anchor='rm')
    if fixed:
        lock(c, x1 - 16, mid + 2, OFF_TEXT)


def i2_slots(items=None):
    items = items or items_with(LATE_STOCK)
    c = Canvas()
    backdrop(c)
    left = (16, 16, 848, 784)
    lq_panel(c, left)
    title_band(c, left, 'Treasure', f'{len(UNITS)} characters', HEADER_DATE)
    cw, ch, gap = 262, 132, 8
    for i, n in enumerate(UNITS):
        x = 30 + (i % 3) * (cw + gap)
        y = 72 + (i // 3) * (ch + gap)
        unit_card(c, (x, y, x + cw, y + ch), n, items, 'drop' if n == 8 else 'normal')
    right = (856, 16, 1264, 784)
    lq_panel(c, right)
    c.text((878, 44), 'Items', brush(22), fill=NAME, anchor='lm')
    c.text((1242, 46), f'{len(owned(items))} owned', cinzel(13, 600), fill=SUB, anchor='rm')
    divider(c, 872, 1248, 62)
    put_picture(c, item_picture(21), (872, 74, 1248, 309))
    y = 322
    for it in sorted(owned(items), key=lambda i: i['Index']):
        item_tablet(c, (872, y, 1248, y + 28), it, 'dragged' if it['Index'] == 21 else 'normal')
        y += 32
    small_button(c, (1248 - 110, 784 - 50, 1248, 784 - 14), 'End')
    gx, gy = 640, 420                       # the dragged item under the pointer
    shadow(c, (gx - 110, gy - 14, gx + 110, gy + 14), 20)
    item_tablet(c, (gx - 110, gy - 14, gx + 110, gy + 14), items[21], 'hover')
    pointer(c, gx + 40, gy + 4)
    c.save('I2-slots.png', 'I2 — characters with item slots, items dragged onto them (a later game\'s stock, made up)',
           'Each character a card with a slot; drag an item (or click it, then a card) to equip; a slot\'s item dragged out goes back '
           'to stock. Fixed and unequippable items greyed.')


def tabs(c, x, y, labels, active):
    for i, label in enumerate(labels):
        w = c.width(label, cinzel(14, 700)) + 34
        r = (x, y - 15, x + w, y + 15)
        if i == active:
            c.rect(r, fill=(60, 30, 22, 255), outline=GOLD, width=1)
            c.text(((r[0] + r[2]) / 2, y), label, cinzel(14, 700), fill=NAME, anchor='mm')
        else:
            c.rect(r, fill=ROW, outline=DIM, width=1)
            c.text(((r[0] + r[2]) / 2, y), label, cinzel(14, 700), fill=QUIET, anchor='mm')
        x += w + 6


def i3_tabs():
    items = items_with(LATE_STOCK)
    c = Canvas()
    backdrop(c)
    top = (16, 16, 1264, 400)
    lq_panel(c, top)
    c.text((38, 41), 'Treasure', brush(24), fill=NAME, anchor='lm')
    tabs(c, 38 + c.width('Treasure', brush(24)) + 22, 43, ['Characters', 'Items'], 1)
    c.text((1242, 43), HEADER_DATE, cinzel(15, 700), fill=NAME, anchor='rm')
    divider(c, 32, 1248, 60)
    icols = [('', 30, 'mark', 'r'), ('No.', 46, 'n', 'r'), ('Name', 260, 'name', 'l'), ('Kind', 200, 'kind', 'l'),
             ('Held by', 680, 'held', 'l')]
    rows = item_rows(items)
    ledger(c, (30, 72, 1250, 386), icols, rows, row_h=26, chosen=21,
           greyed=[rid for rid, v in rows if v['kind'] in ('Fixed', '!Can\'t equip')])
    put_picture(c, item_picture(21), (16, 416, 554, 752))
    prompt = (570, 416, 1264, 784)
    lq_panel(c, prompt)
    c.text((592, 444), 'Equipping', brush(22), fill=NAME, anchor='lm')
    divider(c, 586, 1248, 464)
    fs = 96
    c.rect((594, 482, 594 + fs, 482 + fs), fill=(52, 40, 28, 255))
    c.paste(face_image(UNITS[8]['FaceCg'], fs), (594, 482))
    c.rect((594, 482, 594 + fs, 482 + fs), outline=GOLD, width=1)
    c.text((710, 500), UNITS[8]['Name'], brush(26), fill=NAME, anchor='lm')
    held = item_of(items).get(8)
    c.text((710, 532), f"{UNITS[8]['Type']} · " + (f"holds {held['Name']} (back to stock)" if held else 'holds nothing'),
           font(14), fill=QUIET, anchor='lm')
    c.text((710, 560), 'Which item? Choose one in the list.', font(16), fill=TEXT, anchor='lm')
    red_button(c, (590, 620, 1080, 700), 'Equip Crystal Ring', 'from the stock', hover=True, tf=brush(24))
    small_button(c, (1090, 620, 1244, 700), 'Back', f=cinzel(15, 700))
    small_button(c, (1244 - 110, 740, 1244, 772), 'End', f=cinzel(14, 700))
    c.save('I3-tabs.png', 'I3 — the original\'s two views as tabs (a later game\'s stock, made up)',
           'One ledger at a time: Characters, or Items; a character picked turns to Items with "Equipping Cordoba — which item?", '
           'as the original\'s prompt. Back drops the pick.')


# ---------------------------------------------------------------- the treasure in Sengoku Rance's manner (round s)

def sprite_image(n, scale=3):
    """A unit's own battle sprite (Unit_CgAlone, 320x256, palette index 40 transparent) cut to its pixels, scaled up
    whole pixels."""
    raw = Image.open(os.path.join(CGS, 'CG%04d.png' % UNITS[n]['CgAlone']))
    img = raw.convert('RGBA')
    key = Image.new('L', raw.size)
    key.putdata([0 if i == 40 else 255 for i in raw.getdata()])
    img.putalpha(key)
    img = img.crop(key.getbbox())
    return img.resize((img.width * scale * S, img.height * scale * S), Image.NEAREST)


def glow(c, cx, cy, rx, ry, colour, steps=14, alpha=10):
    for i in range(steps, 0, -1):
        k = i / steps
        layer = Image.new('RGBA', c.im.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).ellipse([(cx - rx * k) * S, (cy - ry * k) * S, (cx + rx * k) * S, (cy + ry * k) * S],
                                      fill=colour + (alpha,))
        c.im = Image.alpha_composite(c.im, layer)
    c.d = ImageDraw.Draw(c.im)


def char_list(c, r, items, chosen, hover=None):
    """Every character, a lacquer row each: face, name, type, the item held (a padlock on Chaos and Nikkou)."""
    x0, y0, x1, y1 = r
    lq_panel(c, r)
    c.text((x0 + 20, y0 + 26), 'Characters', brush(20), fill=NAME, anchor='lm')
    c.text((x1 - 20, y0 + 28), f'{len(UNITS)}', cinzel(14, 700), fill=SUB, anchor='rm')
    divider(c, x0 + 14, x1 - 14, y0 + 46)
    held = item_of(items)
    y, rh = y0 + 58, 72
    for n, u in UNITS.items():
        row = (x0 + 12, y, x1 - 12, y + rh - 6)
        row_box(c, row, 'chosen' if n == chosen else ('hover' if n == hover else 'normal'))
        fs = rh - 18
        c.rect((row[0] + 6, y + 6, row[0] + 6 + fs, y + 6 + fs), fill=(52, 40, 28, 255))
        c.paste(face_image(u['FaceCg'], fs), (row[0] + 6, y + 6))
        c.rect((row[0] + 6, y + 6, row[0] + 6 + fs, y + 6 + fs), outline=GOLD if n == chosen else DIM, width=1)
        tx = row[0] + fs + 16
        c.text((tx, y + 19), u['Name'], font(17), fill=NAME if n == chosen else TEXT, anchor='lm')
        c.text((tx, y + 41), u['Type'], font(12), fill=QUIET, anchor='lm')
        it = held.get(n)
        if it:
            w = c.width(it['Name'], font(13))
            diamond(c, row[2] - 18 - w - 12, y + 41, 4, GOLD)
            c.text((row[2] - 14, y + 41), it['Name'], font(13), fill=TEXT, anchor='rm')
            if it['Index'] in FIXED:
                lock(c, row[2] - 14, y + 20, QUIET)
        else:
            c.text((row[2] - 14, y + 41), '—', font(13), fill=DIM, anchor='rm')
        if n == chosen:
            c.poly([(row[0] - 10, y + rh / 2 - 9), (row[0] - 1, y + rh / 2 - 3), (row[0] - 10, y + rh / 2 + 3)], fill=GOLD)
        y += rh


def slot_window(c, r, n, items, state='normal', dragged=None):
    """The equipment window beside the sprite: its title, the item's picture (the original's item CG) and name, or a
    dashed field to drop on; 'drop' glows gold over the dragged item's picture, 'locked' shows the padlock."""
    x0, y0, x1, y1 = r
    lq_panel(c, r, fill=(30, 20, 14, 250))
    c.text((x0 + 18, y0 + 24), 'Equipment', brush(18), fill=NAME, anchor='lm')
    divider(c, x0 + 12, x1 - 12, y0 + 42)
    it = item_of(items).get(n)
    ph = min(round((x1 - x0 - 28) * 196 / 314), y1 - y0 - 54 - 76)     # the picture's height, leaving room for its name
    pic = (x0 + 14, y0 + 54, x0 + 14 + round(ph * 314 / 196), y0 + 54 + ph)
    if state == 'drop':
        put_picture(c, item_picture(dragged), pic)
        c.overlay(pic, (232, 199, 122, 40))
        for i in range(8, 0, -1):
            c.overlay((pic[0] - i, pic[1] - i, pic[2] + i, pic[3] + i), (232, 199, 122, 8))
        c.rect(pic, outline=HOVER_LINE, width=3)
        c.rect(((pic[0] + pic[2]) / 2 - 64, pic[3] - 14, (pic[0] + pic[2]) / 2 + 64, pic[3] + 14), fill=(60, 44, 24, 255),
               outline=GOLD)
        c.text(((pic[0] + pic[2]) / 2, pic[3]), 'Equip here', font(15), fill=NAME, anchor='mm')
        c.text((x0 + 18, pic[3] + 34), items[dragged]['Name'], font(18), fill=NAME, anchor='lm')
        c.text((x0 + 18, pic[3] + 58), f"{it['Name']} goes back to stock" if it else 'from the stock', font(13),
               fill=QUIET, anchor='lm')
    elif it:
        put_picture(c, item_picture(it['Index']), pic)
        c.text((x0 + 18, pic[3] + 22), it['Name'], font(18), fill=NAME, anchor='lm')
        if state == 'locked':
            lock(c, x1 - 26, pic[3] + 24, QUIET)
            c.text((x0 + 18, pic[3] + 46), 'Never leaves him', font(13), fill=QUIET, anchor='lm')
        else:
            c.text((x0 + 18, pic[3] + 46), 'Drag it out to unequip, or ×', font(13), fill=QUIET, anchor='lm')
            c.rect((x1 - 40, pic[3] + 12, x1 - 16, pic[3] + 36), fill=(40, 22, 16, 255), outline=(150, 70, 52, 255))
            c.text((x1 - 28, pic[3] + 24), '×', font(16), fill=RED, anchor='mm')
    else:
        c.overlay(pic, (34, 26, 20, 160))
        for x in range(round(pic[0]), round(pic[2]), 14):
            c.line((x, pic[1]), (min(x + 8, pic[2]), pic[1]), fill=DIM)
            c.line((x, pic[3] - 1), (min(x + 8, pic[2]), pic[3] - 1), fill=DIM)
        for y in range(round(pic[1]), round(pic[3]), 14):
            c.line((pic[0], y), (pic[0], min(y + 8, pic[3])), fill=DIM)
            c.line((pic[2] - 1, y), (pic[2] - 1, min(y + 8, pic[3])), fill=DIM)
        c.text(((pic[0] + pic[2]) / 2, (pic[1] + pic[3]) / 2 - 10), 'No item', cinzel(18, 700), fill=DIM, anchor='mm')
        c.text(((pic[0] + pic[2]) / 2, (pic[1] + pic[3]) / 2 + 16), 'Drag an item here', font(13), fill=QUIET, anchor='mm')


def stage(c, r, n, items, slot='normal', dragged=None, slot_at='right'):
    """The chosen character's stage: name and type, a strip of figures, the sprite large on a lit floor, the equipment
    window beside it (or under it)."""
    x0, y0, x1, y1 = r
    lq_panel(c, r, fill=(18, 13, 10, 250))
    u = UNITS[n]
    c.text((x0 + 24, y0 + 32), u['Name'], brush(32), fill=NAME, anchor='lm')
    c.text((x0 + 28 + c.width(u['Name'], brush(32)) + 14, y0 + 36), u['Type'], cinzel(15, 700), fill=SUB, anchor='lm')
    figures = [('Troops', f"{u['Troops']}/{u['MaxTroops']}"), ('Cost', f"{u['DeployCost'] * 100:,}K"), ('Lv', str(u['Level'])),
               ('HP', f"{u['HP']}/{u['MaxHP']}"), ('Atk', str(u['Atk'])), ('Def', str(u['Def'])), ('Act', str(u['Attacks'])),
               ('Mag', str(u['Magic']))]
    fx = x0 + 24
    for label, v in figures:
        c.text((fx, y0 + 66), label, cinzel(11, 700), fill=LABEL, anchor='lm')
        c.text((fx, y0 + 86), v, font(16), fill=VALUE, anchor='lm')
        fx += max(58, c.width(v, font(16)) + 22)
    divider(c, x0 + 16, x1 - 16, y0 + 108)
    sw = 470 if slot_at == 'right' else 0
    sx0, sx1 = x0 + 16, x1 - 16 - sw
    floor = y1 - 70 if slot_at == 'right' else y0 + 330
    cx = (sx0 + sx1) / 2
    glow(c, cx, floor - 80, 160, 120, (232, 199, 122), alpha=7)
    glow(c, cx, floor, 110, 18, (0, 0, 0), alpha=40)
    c.line((sx0 + 30, floor + 1), (sx1 - 30, floor + 1), fill=(110, 86, 46, 120))
    spr = sprite_image(n, 2)
    w, h = spr.width / S, spr.height / S
    if h > floor - y0 - 130:                                       # a tall sprite: scale it down to the room
        k = (floor - y0 - 130) / h
        spr = spr.resize((round(spr.width * k), round(spr.height * k)), Image.NEAREST)
        w, h = spr.width / S, spr.height / S
    c.paste(spr, (cx - w / 2, floor - h + 6))
    if slot_at == 'right':
        slot_window(c, (x1 - 16 - sw + 6, y0 + 124, x1 - 16, y1 - 16), n, items, slot, dragged)
    return (sx0, sx1, floor)


def shelf(c, r, items, dragged=None, hover=None, cols=3):
    """The items owned as tablets (the name, where it is), three to a row; Chaos and Nikkou and the unequippable greyed."""
    x0, y0, x1, y1 = r
    lq_panel(c, r)
    c.text((x0 + 20, y0 + 24), 'Items', brush(20), fill=NAME, anchor='lm')
    c.text((x0 + 20 + c.width('Items', brush(20)) + 12, y0 + 26), f'{len(owned(items))} owned · drag one onto the equipment',
           cinzel(12, 600), fill=SUB, anchor='lm')
    divider(c, x0 + 14, x1 - 14, y0 + 44)
    tw = (x1 - x0 - 28 - (cols - 1) * 8) / cols
    for i, it in enumerate(sorted(owned(items), key=lambda v: v['Index'])):
        x = x0 + 14 + (i % cols) * (tw + 8)
        y = y0 + 56 + (i // cols) * 34
        item_tablet(c, (round(x), y, round(x + tw), y + 28), it,
                    'dragged' if it['Index'] == dragged else ('hover' if it['Index'] == hover else 'normal'))


def s_layout(name, caption, note, items, n, slot='normal', dragged=None, ghost_at=None, title_sub=None):
    c = Canvas()
    backdrop(c)
    stage(c, (16, 16, 856, 540), n, items, slot, dragged)
    shelf(c, (16, 552, 856, 784), items, dragged)
    char_list(c, (872, 16, 1264, 728), items, n)
    small_button(c, (1264 - 130, 740, 1264, 784), 'End')
    if ghost_at:
        gx, gy = ghost_at
        shadow(c, (gx - 110, gy - 14, gx + 110, gy + 14), 20)
        item_tablet(c, (gx - 110, gy - 14, gx + 110, gy + 14), items[dragged], 'hover')
        pointer(c, gx + 40, gy + 4)
    c.save(name, caption, note)


def s_column(name, items, n, dragged, ghost_at):
    """S5: the items a column at the left, the stage in the middle with the equipment window under the sprite."""
    c = Canvas()
    backdrop(c)
    left = (16, 16, 300, 784)
    lq_panel(c, left)
    c.text((36, 42), 'Items', brush(20), fill=NAME, anchor='lm')
    c.text((280, 44), f'{len(owned(items))}', cinzel(14, 700), fill=SUB, anchor='rm')
    divider(c, 30, 286, 62)
    y = 74
    for it in sorted(owned(items), key=lambda v: v['Index']):
        item_tablet(c, (30, y, 286, y + 28), it, 'dragged' if it['Index'] == dragged else 'normal')
        y += 32
    mid = (308, 16, 864, 784)
    sx0, sx1, floor = stage(c, mid, n, items, slot_at='under')
    slot_window(c, (324, floor + 30, 848, 768), n, items, 'drop', dragged)
    char_list(c, (872, 16, 1264, 728), items, n)
    small_button(c, (1264 - 130, 740, 1264, 784), 'End')
    gx, gy = ghost_at
    shadow(c, (gx - 110, gy - 14, gx + 110, gy + 14), 20)
    item_tablet(c, (gx - 110, gy - 14, gx + 110, gy + 14), items[dragged], 'hover')
    pointer(c, gx + 40, gy + 4)
    c.save(name, 'S5 — the items a column at the left, the equipment window under the sprite (a later game\'s stock, made up)',
           'Three columns: items | the chosen character (sprite, figures, the window to drop on) | every character. Here Crystal Ring '
           'dragged onto Cordoba.')


def s_round():
    late = items_with(LATE_STOCK)
    s_layout('S1-stage.png', 'S1 — Sengoku Rance\'s manner: the characters right, the chosen one\'s sprite and equipment left',
             'Barres chosen (week 18: two items, both held). His sprite on a lit floor, his figures above; the equipment window '
             'beside him takes an item dragged from the shelf below.', ITEMS, 6)
    s_layout('S2-holding.png', 'S2 — a character holding an item (a later game\'s stock, made up)',
             'Cordoba holds Magic Barrier: its picture and description in the window; dragged out (or ×) it goes back to stock.',
             late, 8)
    s_layout('S3-drag.png', 'S3 — a drag: Crystal Ring onto Cordoba (a later game\'s stock, made up)',
             'The window glows gold over the dragged item\'s picture, "Equip here"; on release Crystal Ring is his and Magic '
             'Barrier goes back to stock.', late, 8, 'drop', 21, (500, 412))
    s_layout('S4-locked.png', 'S4 — Rance with Chaos (a later game\'s stock, made up)',
             'Chaos never leaves him: the window shows it with a padlock and takes no drop; Chaos and the unequippable items are '
             'greyed on the shelf.', late, 1, 'locked')
    s_column('S5-column.png', late, 8, 21, (470, 650))


# ---------------------------------------------------------------- the round

# ---------------------------------------------------------------- round t: the treasure with icons of its own, or item-first

# The items have no icons of their own: each CG 750 + n is a maid presenting the item (in her hands, on a table, or in a
# thought bubble) beside its description. T1 draws an emblem by the item's kind (a table of ours); T2 cuts a square out
# of the item's picture (a table of ours, as the faces' squares): item -> (x, y, size) in the CG's 304x196 pixels.
KINDS = {1: 'sword', 2: 'sword', 3: 'staff', 6: 'sword', 7: 'spear', 10: 'plant', 11: 'charm', 12: 'sword', 21: 'ring',
         23: 'ring', 35: 'staff', 43: 'fruit', 48: 'vial'}
KIND_TINT = {'sword': (150, 52, 30), 'spear': (150, 52, 30), 'staff': (70, 90, 150), 'ring': (70, 90, 150),
             'charm': (120, 40, 90), 'plant': (60, 110, 60), 'fruit': (60, 110, 60), 'vial': (60, 110, 60)}
ICON_CROPS = {1: (47, 56, 112), 6: (70, 55, 120), 7: (50, 50, 120), 3: (55, 55, 110), 10: (62, 62, 84), 21: (22, 14, 58),
              23: (20, 14, 58), 11: (18, 14, 62), 43: (72, 92, 62), 12: (65, 55, 120), 35: (60, 60, 112), 48: (20, 12, 60)}


def emblem(c, box, kind, off=False):
    """T1: the item's kind as a gold emblem on a dark roundel tinted by kind (weapons red, rods and rings blue, charms
    plum, things that are eaten or grown green)."""
    x0, y0, x1, y1 = box
    cx, cy, r = (x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) / 2
    tint = (60, 54, 46) if off else KIND_TINT.get(kind, (90, 70, 40))
    c.d.ellipse([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S], fill=tuple(v // 3 for v in tint) + (255,),
                outline=tint + (255,), width=2 * S)
    c.d.ellipse([(cx - r + 5) * S, (cy - r + 5) * S, (cx + r - 5) * S, (cy + r - 5) * S], outline=(GOLD if not off else OFF_LINE)[:3] + (90,),
                width=S)
    g = OFF_TEXT if off else NAME
    k = r / 30

    def P(pts):
        return [(cx + px * k, cy + py * k) for px, py in pts]
    if kind in ('sword', 'spear', 'staff'):
        if kind == 'sword':
            c.poly(P([(-15, 15), (12, -12), (16, -16), (14, -10), (-13, 17)]), fill=g)                # the blade
            c.poly(P([(-19, 7), (-7, 19), (-9, 21), (-21, 9)]), fill=g)                             # the guard
            c.line(*P([(-15, 15), (-22, 22)]), fill=g, width=3 * k)
            c.d.ellipse([(cx - 24 * k) * S, (cy + 21 * k) * S, (cx - 20 * k) * S, (cy + 25 * k) * S], fill=g)
        else:
            c.line(*P([(-20, 20), (14, -14)]), fill=g, width=2.4 * k)
            if kind == 'spear':
                c.poly(P([(12, -12), (21, -21), (16, -9)]), fill=g)
                c.poly(P([(12, -12), (21, -21), (9, -16)]), fill=g)
            else:
                c.d.ellipse([(cx + 11 * k) * S, (cy - 22 * k) * S, (cx + 22 * k) * S, (cy - 11 * k) * S], fill=g)
                c.d.ellipse([(cx + 7 * k) * S, (cy - 26 * k) * S, (cx + 26 * k) * S, (cy - 7 * k) * S], outline=g, width=round(1.4 * k * S))
    elif kind == 'ring':
        c.d.ellipse([(cx - 15 * k) * S, (cy - 6 * k) * S, (cx + 15 * k) * S, (cy + 20 * k) * S], outline=g, width=round(4 * k * S))
        diamond(c, cx, cy - 10 * k, 8 * k, g)
    elif kind == 'charm':
        c.rect((cx - 10 * k, cy - 12 * k, cx + 10 * k, cy + 20 * k), outline=g, width=2.5 * k)
        c.d.arc([(cx - 7 * k) * S, (cy - 24 * k) * S, (cx + 7 * k) * S, (cy - 8 * k) * S], 180, 360, fill=g, width=round(2 * k * S))
        for i in range(3):
            c.line((cx - 5 * k, cy - 4 * k + i * 7 * k), (cx + 5 * k, cy - 4 * k + i * 7 * k), fill=g, width=2 * k)
    elif kind == 'plant':
        c.poly(P([(-11, 10), (11, 10), (8, 21), (-8, 21)]), fill=g)
        c.line(*P([(0, 10), (-3, 0)]), fill=g, width=2.5 * k)
        c.line(*P([(-3, 0), (4, -8)]), fill=g, width=2.5 * k)
        for px, py, pr in ((-10, -6, 7), (6, -14, 8), (12, -3, 6), (-2, -18, 6)):
            c.d.ellipse([(cx + (px - pr) * k) * S, (cy + (py - pr) * k) * S, (cx + (px + pr) * k) * S, (cy + (py + pr) * k) * S], fill=g)
    elif kind == 'fruit':
        c.d.ellipse([(cx - 15 * k) * S, (cy - 10 * k) * S, (cx + 15 * k) * S, (cy + 18 * k) * S], fill=g)
        c.poly(P([(0, -10), (12, -22), (14, -14)]), fill=g)
    elif kind == 'vial':
        c.rect((cx - 4 * k, cy - 22 * k, cx + 4 * k, cy - 8 * k), outline=g, width=2 * k)
        c.d.ellipse([(cx - 13 * k) * S, (cy - 10 * k) * S, (cx + 13 * k) * S, (cy + 18 * k) * S], outline=g, width=round(2.5 * k * S))
        c.d.chord([(cx - 11 * k) * S, (cy - 8 * k) * S, (cx + 11 * k) * S, (cy + 16 * k) * S], 0, 180, fill=g)


def crop_icon(n, size, off=False):
    """T2: the item's own square cut out of its picture (CG 750 + n)."""
    x, y, s = ICON_CROPS[n]
    img = cg(750 + n).crop((x, y, x + s, y + s)).resize((round(size * S), round(size * S)), Image.LANCZOS)
    if off:
        grey = img.convert('L').convert('RGBA')
        img = Image.blend(grey, Image.new('RGBA', img.size, (20, 16, 12, 255)), 0.45)
    return img


def draw_icon(c, style, box, n, off=False):
    x0, y0, x1, y1 = box
    if style == 'emblem':
        emblem(c, box, KINDS.get(n, 'sword'), off)
    else:
        c.rect(box, fill=(10, 8, 6, 255))
        c.paste(crop_icon(n, x1 - x0, off), (x0, y0))
        c.rect(box, outline=OFF_LINE if off else GOLD, width=1)


def face_badge(c, x, y, unit, size=26):
    c.rect((x - 1, y - 1, x + size + 1, y + size + 1), fill=(52, 40, 28, 255), outline=GOLD, width=1)
    c.paste(face_image(UNITS[unit]['FaceCg'], size), (x, y))


def icon_tile(c, r, it, style, state='normal'):
    """An item's tile in the icon grid: its icon, the holder's face in the corner, a padlock on Chaos and Nikkou, greyed
    when it cannot be dragged; the name under it."""
    x0, y0, x1, y1 = r
    fixed, cant = it['Index'] in FIXED, not it['Effect']
    held = it['Holder'] in UNITS
    off = fixed or cant
    row_box(c, (x0, y0, x1, y0 + (x1 - x0)), 'hover' if state in ('hover', 'ghost') else ('off' if off else 'normal'))
    pad = 6
    draw_icon(c, style, (x0 + pad, y0 + pad, x1 - pad, y0 + (x1 - x0) - pad), it['Index'], off)
    if state == 'dragged':
        c.overlay((x0, y0, x1, y0 + (x1 - x0)), (14, 10, 8, 170))
    if held and state != 'ghost':
        face_badge(c, x1 - 28, y0 + (x1 - x0) - 28, it['Holder'], 24)
    if fixed:
        c.rect((x1 - 22, y0 + 4, x1 - 4, y0 + 22), fill=(20, 16, 12, 230))
        lock(c, x1 - 13, y0 + 15, QUIET)
    if state == 'ghost':
        return
    name = it['Name']
    f = font(11)
    words, lines = name.split(), ['']
    for w in words:
        trial = (lines[-1] + ' ' + w).strip()
        if c.width(trial, f) > x1 - x0 + 6 and lines[-1]:
            lines.append(w)
        else:
            lines[-1] = trial
    for i, line in enumerate(lines[:2]):
        c.text(((x0 + x1) / 2, y0 + (x1 - x0) + 11 + i * 13), line, f, fill=OFF_TEXT if off else (TEXT if held or True else QUIET),
               anchor='mm')


def icon_grid(c, r, items, style, dragged=None, hover=None):
    x0, y0, x1, y1 = r
    lq_panel(c, r)
    c.text((x0 + 20, y0 + 26), 'Items', brush(20), fill=NAME, anchor='lm')
    c.text((x1 - 20, y0 + 28), f'{len(owned(items))}', cinzel(14, 700), fill=SUB, anchor='rm')
    divider(c, x0 + 14, x1 - 14, y0 + 46)
    tw, gap = 80, 8
    for i, it in enumerate(sorted(owned(items), key=lambda v: v['Index'])):
        x = x0 + 14 + (i % 3) * (tw + gap)
        y = y0 + 58 + (i // 3) * 116
        icon_tile(c, (x, y, x + tw, y + tw + 30), it, style,
                  'dragged' if it['Index'] == dragged else ('hover' if it['Index'] == hover else 'normal'))
    return y0 + 58 + ((len(owned(items)) + 2) // 3) * 116


def hovered_item(c, r, it, items):
    """Under the grid: the hovered item's name, where it is and what it does (the picture's own description panel)."""
    x0, y0, x1, y1 = r
    divider(c, x0, x1, y0)
    c.text((x0 + 4, y0 + 22), it['Name'], brush(17), fill=NAME, anchor='lm')
    c.text((x0 + 4, y0 + 44), holder_text(it), font(13), fill=QUIET, anchor='lm')
    desc = cg(750 + it['Index']).crop((206, 0, 304, 196))
    w = x1 - x0
    h = min(y1 - y0 - 62, round(w * 196 / 98))
    dw = round(h * 98 / 196)
    c.paste(desc.resize((round(dw * S), round(h * S)), Image.LANCZOS), (x0 + (w - dw) / 2, y0 + 58))


def char_rows(c, r, items, chosen, style, target=None):
    """The characters as compact rows (the owner, 2026-10-19): name, type, the held item's icon and name."""
    x0, y0, x1, y1 = r
    lq_panel(c, r)
    c.text((x0 + 20, y0 + 26), 'Characters', brush(20), fill=NAME, anchor='lm')
    c.text((x1 - 20, y0 + 28), f'{len(UNITS)}', cinzel(14, 700), fill=SUB, anchor='rm')
    divider(c, x0 + 14, x1 - 14, y0 + 46)
    held = item_of(items)
    y = y0 + 58
    for n, u in UNITS.items():
        row = (x0 + 14, y, x1 - 14, y + 40)
        if n == target:
            for i in range(6, 0, -1):
                c.overlay((row[0] - i, row[1] - i, row[2] + i, row[3] + i), (232, 199, 122, 10))
        row_box(c, row, 'chosen' if n == chosen else ('hover' if n == target else 'normal'))
        mid = y + 20
        c.text((row[0] + 10, mid), u['Name'], font(15), fill=NAME if n == chosen else TEXT, anchor='lm')
        c.text((row[0] + 112, mid + 1), u['Type'], font(11), fill=QUIET, anchor='lm')
        it = held.get(n)
        if n == target:
            c.text((row[2] - 10, mid), 'Equip here', font(13), fill=NAME, anchor='rm')
        elif it:
            c.text((row[2] - 10, mid), it['Name'], font(12), fill=TEXT, anchor='rm')
            w = c.width(it['Name'], font(12))
            draw_icon(c, style, (row[2] - 10 - w - 36, mid - 15, row[2] - 10 - w - 6, mid + 15), it['Index'], it['Index'] in FIXED)
        else:
            c.text((row[2] - 10, mid), '—', font(13), fill=DIM, anchor='rm')
        if n == chosen:
            c.poly([(row[0] - 10, mid - 8), (row[0] - 1, mid), (row[0] - 10, mid + 8)], fill=GOLD)
        y += 44


def t_icons(name, style, caption, note):
    """T1/T2: S5's three columns with the items as an icon grid, as Sengoku Rance's inventory."""
    late = items_with(LATE_STOCK)
    c = Canvas()
    backdrop(c)
    left = (16, 16, 300, 784)
    bottom = icon_grid(c, left, late, style, dragged=21, hover=None)
    hovered_item(c, (30, bottom + 4, 286, 770), late[35], late)
    _, _, floor = stage(c, (308, 16, 864, 784), 8, late, slot_at='under')
    slot_window(c, (324, floor + 30, 848, 768), 8, late, 'drop', 21)
    char_rows(c, (872, 16, 1264, 728), late, 8, style)
    small_button(c, (1264 - 130, 740, 1264, 784), 'End')
    gx, gy = 520, 560
    shadow(c, (gx - 40, gy - 40, gx + 40, gy + 40), 20)
    icon_tile(c, (gx - 40, gy - 40, gx + 40, gy + 70), late[21], style, 'ghost')
    pointer(c, gx + 18, gy + 14)
    c.save(name, caption, note)


def card_art(n, w):
    """T3: the item's card — the scene half of its picture (the maid presenting it, its name in the corner)."""
    img = cg(750 + n).crop((0, 0, 206, 196))
    return img.resize((round(w * S), round(w * 196 / 206 * S)), Image.LANCZOS)


def item_card(c, x, y, it, state='normal', w=128):
    h = round(w * 196 / 206)
    fixed, cant = it['Index'] in FIXED, not it['Effect']
    off = fixed or cant
    if state == 'chosen':
        for i in range(6, 0, -1):
            c.overlay((x - i, y - i, x + w + i, y + h + 22 + i), (232, 199, 122, 12))
    art = card_art(it['Index'], w)
    if off:
        grey = art.convert('L').convert('RGBA')
        art = Image.blend(grey, Image.new('RGBA', art.size, (20, 16, 12, 255)), 0.4)
    c.paste(art, (x, y))
    if state == 'dragged':
        c.overlay((x, y, x + w, y + h), (14, 10, 8, 170))
    c.rect((x, y, x + w, y + h), outline=HOVER_LINE if state == 'chosen' else (OFF_LINE if off else GOLD), width=2 if state == 'chosen' else 1)
    plate = (x, y + h + 2, x + w, y + h + 22)
    c.rect(plate, fill=(60, 30, 22, 255) if state == 'chosen' else (OFF if off else ROW), outline=DIM, width=1)
    if it['Holder'] in UNITS:
        face_badge(c, x + w - 30, y + h - 30, it['Holder'], 26)
        c.text((x + 6, y + h + 12), UNITS[it['Holder']]['Name'], font(12), fill=NAME if state == 'chosen' else TEXT, anchor='lm')
    else:
        c.text((x + 6, y + h + 12), "can't equip" if cant else 'In stock', font(12), fill=OFF_TEXT if off else QUIET, anchor='lm')
    if fixed:
        c.rect((x + w - 24, y + 4, x + w - 4, y + 24), fill=(20, 16, 12, 230))
        lock(c, x + w - 14, y + 16, QUIET)


def t_cards():
    """T3: item-first — the pictures are the items' art; two shelves of cards, the chosen card large, the characters."""
    late = items_with(LATE_STOCK)
    c = Canvas()
    backdrop(c)
    left = (16, 16, 860, 784)
    lq_panel(c, left)
    title_band(c, left, 'Treasure', f'{len(owned(late))} items', HEADER_DATE)
    shelves = [('Equipped', [it for it in sorted(owned(late), key=lambda v: v['Index']) if it['Holder'] in UNITS],
                'drag a card here to take it off'),
               ('In stock', [it for it in sorted(owned(late), key=lambda v: v['Index']) if it['Holder'] == 100],
                'drag a card onto a character to equip')]
    y = 76
    for title, cards, hint in shelves:
        c.text((36, y + 12), title, cinzel(14, 700), fill=LABEL, anchor='lm')
        c.text((840, y + 12), hint, font(12), fill=DIM, anchor='rm')
        c.line((36 + c.width(title, cinzel(14, 700)) + 12, y + 12), (840 - c.width(hint, font(12)) - 12, y + 12), fill=DIM)
        for i, it in enumerate(cards):
            x = 32 + (i % 6) * 136
            cy = y + 30 + (i // 6) * 152
            item_card(c, x, cy, it, 'dragged' if it['Index'] == 21 else ('chosen' if it['Index'] == 35 else 'normal'))
        y += 30 + ((len(cards) + 5) // 6) * 152 + 16
    right = (872, 16, 1264, 784)
    lq_panel(c, right)
    sel = late[35]
    c.text((892, 42), sel['Name'], brush(20), fill=NAME, anchor='lm')
    c.text((1244, 44), holder_text(sel), font(13), fill=QUIET, anchor='rm')
    divider(c, 886, 1250, 62)
    put_picture(c, item_picture(35), (886, 72, 1250, 307))
    held = item_of(late)
    c.text((892, 332), 'Characters', cinzel(13, 700), fill=LABEL, anchor='lm')
    yy = 346
    for n, u in UNITS.items():
        row = (886, yy, 1250, yy + 36)
        target = n == 8
        if target:
            for i in range(6, 0, -1):
                c.overlay((row[0] - i, row[1] - i, row[2] + i, row[3] + i), (232, 199, 122, 10))
        row_box(c, row, 'hover' if target else 'normal')
        c.rect((row[0] + 4, yy + 4, row[0] + 32, yy + 32), fill=(52, 40, 28, 255))
        c.paste(face_image(u['FaceCg'], 28), (row[0] + 4, yy + 4))
        c.text((row[0] + 42, yy + 18), u['Name'], font(15), fill=NAME if target else TEXT, anchor='lm')
        it = held.get(n)
        if target:
            c.text((row[2] - 10, yy + 18), 'Equip here · Magic Barrier to stock', font(12), fill=NAME, anchor='rm')
        else:
            c.text((row[2] - 10, yy + 18), it['Name'] if it else '—', font(12), fill=TEXT if it else DIM, anchor='rm')
        yy += 40
    small_button(c, (1250 - 120, 740, 1250, 772), 'End', f=cinzel(14, 700))
    gx, gy = 1010, 700
    shadow(c, (gx - 64, gy - 61, gx + 64, gy + 61), 20)
    c.paste(card_art(21, 128), (gx - 64, gy - 61))
    c.rect((gx - 64, gy - 61, gx + 64, gy + 61), outline=HOVER_LINE, width=2)
    pointer(c, gx + 30, gy + 20)
    c.save('T3-cards.png', 'T3 — item-first: the pictures as cards (a later game\'s stock, made up)',
           'Two shelves, Equipped (the holder\'s face on the card) and In stock; a card dragged onto a character equips it, onto '
           'In stock takes it off. The chosen card\'s full picture top right. Here Crystal Ring onto Cordoba.')


def t_round():
    t_icons('T1-emblems.png', 'emblem', 'T1 — S5 with emblems of our own by the item\'s kind (a later game\'s stock, made up)',
            'Each item a gold emblem drawn by code (sword, spear, rod, ring, charm, plant, fruit, vial…) from a kind table of ours; '
            'the holder\'s face on the tile; Crystal Ring dragged onto Cordoba.')
    t_icons('T2-cut-icons.png', 'cut', 'T2 — S5 with icons cut out of the items\' pictures (a later game\'s stock, made up)',
            'A square of each item\'s own CG (the maid holding it, the table, the bubble) — a crop table of ours, as the faces\' '
            'squares; the holder\'s face on the tile.')
    t_cards()


# ---------------------------------------------------------------- round u: other ideas (the owner rejected S5 and round T)

def thumb(c, n, box, off=False):
    """An item's scene (the left two thirds of its picture) shrunk into box, framed."""
    x0, y0, x1, y1 = box
    art = cg(750 + n).crop((0, 0, 206, 196)).resize((round((x1 - x0) * S), round((y1 - y0) * S)), Image.LANCZOS)
    if off:
        art = Image.blend(art.convert('L').convert('RGBA'), Image.new('RGBA', art.size, (20, 16, 12, 255)), 0.45)
    c.paste(art, (x0, y0))
    c.rect(box, outline=OFF_LINE if off else DIM, width=1)


def arrow_button(c, cx, cy, left):
    c.d.ellipse([(cx - 24) * S, (cy - 24) * S, (cx + 24) * S, (cy + 24) * S], fill=ROW, outline=GOLD, width=2 * S)
    k = -1 if left else 1
    c.line((cx - 5 * k, cy - 10), (cx + 6 * k, cy), fill=NAME, width=3)
    c.line((cx + 6 * k, cy), (cx - 5 * k, cy + 10), fill=NAME, width=3)


def tooltip(c, x, y, lines):
    w = max(c.width(s, font(13)) for s in lines) + 24
    r = (x, y, x + w, y + 12 + 20 * len(lines))
    shadow(c, r, 18)
    c.rect(r, fill=(22, 16, 12, 250), outline=GOLD, width=1)
    for i, s in enumerate(lines):
        c.text((x + 12, y + 16 + 20 * i), s, font(13), fill=VALUE if i == 0 else QUIET, anchor='lm')


def u1_showcase():
    """U1: one item at a time, its picture large, the owned items a film strip, the bearer chosen among faces."""
    late = items_with(LATE_STOCK)
    order = [it['Index'] for it in sorted(owned(late), key=lambda v: v['Index'])]
    sel = 35
    c = Canvas()
    backdrop(c)
    frame = (16, 16, 1264, 784)
    lq_panel(c, frame)
    title_band(c, frame, 'Treasure', f'{order.index(sel) + 1} of {len(order)}', HEADER_DATE)
    put_picture(c, item_picture(sel), (84, 80, 784, 531))
    arrow_button(c, 50, 305, True)
    arrow_button(c, 818, 305, False)
    it = late[sel]
    x = 862
    c.text((x, 104), it['Name'], brush(28), fill=NAME, anchor='lm')
    c.text((x, 138), 'Equippable', cinzel(13, 700), fill=LABEL, anchor='lm')
    divider(c, x, 1244, 162)
    c.text((x, 188), 'Borne by', cinzel(13, 700), fill=LABEL, anchor='lm')
    c.rect((x, 204, x + 112, 316), fill=(52, 40, 28, 255))
    c.paste(face_image(UNITS[8]['FaceCg'], 112), (x, 204))
    c.rect((x, 204, x + 112, 316), outline=GOLD, width=2)
    c.text((x + 128, 228), UNITS[8]['Name'], brush(24), fill=NAME, anchor='lm')
    c.text((x + 128, 258), UNITS[8]['Type'], font(13), fill=QUIET, anchor='lm')
    small_button(c, (x + 128, 280, 1244, 316), 'Return to stock', f=cinzel(13, 700))
    c.text((x, 352), 'Pick another bearer below: his own', font(13), fill=QUIET, anchor='lm')
    c.text((x, 372), 'item goes back to stock.', font(13), fill=QUIET, anchor='lm')
    # the film strip
    y = 552
    divider(c, 30, 1250, y - 8)
    tw, th = 86, 82
    x0 = (1280 - len(order) * (tw + 10) + 10) / 2
    for i, n in enumerate(order):
        bx = x0 + i * (tw + 10)
        item = late[n]
        off = n in FIXED or not item['Effect']
        if n == sel:
            for k in range(6, 0, -1):
                c.overlay((bx - k, y - k, bx + tw + k, y + th + k), (232, 199, 122, 14))
        thumb(c, n, (bx, y, bx + tw, y + th), off)
        if n == sel:
            c.rect((bx, y, bx + tw, y + th), outline=HOVER_LINE, width=2)
        if item['Holder'] in UNITS:
            face_badge(c, bx + tw - 22, y + th - 22, item['Holder'], 20)
        if n in FIXED:
            lock(c, bx + 12, y + 14, QUIET)
    # the bearers
    y = 656
    divider(c, 30, 1250, y - 8)
    held = item_of(late)
    fs = 72
    x0 = (1280 - len(UNITS) * 118 + 46) / 2
    for i, (n, u) in enumerate(UNITS.items()):
        bx = x0 + i * 118
        locked = n in held and held[n]['Index'] in FIXED
        c.rect((bx, y, bx + fs, y + fs), fill=(52, 40, 28, 255))
        c.paste(face_image(u['FaceCg'], fs, alpha=0.45 if locked else 1.0), (bx, y))
        bearer, hover = n == 8, n == 9
        c.rect((bx, y, bx + fs, y + fs), outline=VERMILION if bearer else (HOVER_LINE if hover else DIM), width=3 if bearer or hover else 1)
        if bearer:
            c.rect((bx + 4, y - 9, bx + fs - 4, y + 7), fill=VERMILION)
            c.text((bx + fs / 2, y - 1), 'BEARER', cinzel(9, 700), fill=(246, 230, 206, 255), anchor='mm')
        if locked:
            lock(c, bx + fs - 12, y + 16, QUIET)
        c.text((bx + fs / 2, y + fs + 13), u['Name'], font(13), fill=NAME if bearer else (OFF_TEXT if locked else TEXT), anchor='mm')
        c.text((bx + fs / 2, y + fs + 29), held[n]['Name'] if n in held else '—', font(10), fill=OFF_TEXT if locked else QUIET, anchor='mm')
        if hover:
            tip = (bx, y)
    pointer(c, tip[0] + 44, tip[1] + 40)
    tooltip(c, tip[0] + fs + 8, tip[1] + 4, ['Give Magic Barrier to Kinkaid', 'Safety Bonsai goes back to stock'])
    small_button(c, (1244 - 120, 470, 1244, 506), 'End', f=cinzel(14, 700))
    c.save('U1-showcase.png', 'U1 — one item at a time: its picture large, a film strip of the items, the bearer among faces',
           'The arrows or the strip page through the items; a click on a face gives it the item (his own back to stock); Return to '
           'stock takes it off. Chaos\'s bearer greyed with a padlock.')


def u2_roster():
    """U2: the characters as portrait cards with an item slot; a click on a slot opens a chooser of the items."""
    late = items_with(LATE_STOCK)
    held = item_of(late)
    c = Canvas()
    backdrop(c)
    left = (16, 16, 856, 784)
    lq_panel(c, left)
    title_band(c, left, 'Treasure', f'{len(UNITS)} characters · {len(owned(late))} items', HEADER_DATE)
    cw, ch, gap = 196, 160, 8
    slot_of = {}
    for i, (n, u) in enumerate(UNITS.items()):
        x = 30 + (i % 4) * (cw + gap)
        y = 72 + (i // 4) * (ch + gap)
        open_ = n == 8
        row_box(c, (x, y, x + cw, y + ch), 'chosen' if open_ else 'normal')
        c.rect((x + 8, y + 8, x + 80, y + 80), fill=(52, 40, 28, 255))
        c.paste(face_image(u['FaceCg'], 72), (x + 8, y + 8))
        c.rect((x + 8, y + 8, x + 80, y + 80), outline=GOLD if open_ else DIM, width=1)
        c.text((x + 90, y + 24), u['Name'], font(17), fill=NAME if open_ else TEXT, anchor='lm')
        c.text((x + 90, y + 46), u['Type'], font(11), fill=QUIET, anchor='lm')
        c.text((x + 90, y + 66), f"Lv {u['Level']} · Atk {u['Atk']}", font(11), fill=QUIET, anchor='lm')
        slot = (x + 8, y + 92, x + cw - 8, y + ch - 8)
        slot_of[n] = slot
        it = held.get(n)
        if it:
            c.rect(slot, fill=(46, 34, 22, 255), outline=HOVER_LINE if open_ else GOLD, width=2 if open_ else 1)
            thumb(c, it['Index'], (slot[0] + 4, slot[1] + 4, slot[0] + 4 + 60, slot[3] - 4), it['Index'] in FIXED)
            c.text((slot[0] + 72, (slot[1] + slot[3]) / 2 - 8), it['Name'], font(13), fill=TEXT, anchor='lm')
            c.text((slot[0] + 72, (slot[1] + slot[3]) / 2 + 12), 'never leaves him' if it['Index'] in FIXED else 'click to change',
                   font(10), fill=QUIET, anchor='lm')
            if it['Index'] in FIXED:
                lock(c, slot[2] - 12, slot[1] + 16, QUIET)
        else:
            c.rect(slot, fill=(30, 24, 18, 255), outline=DIM, width=1)
            c.text(((slot[0] + slot[2]) / 2, (slot[1] + slot[3]) / 2), '+ Equip an item', cinzel(12, 700), fill=DIM, anchor='mm')
    # the stock at the foot
    y = 72 + 3 * (ch + gap) + 6
    c.text((36, y + 10), 'In stock', cinzel(13, 700), fill=LABEL, anchor='lm')
    stock = [it for it in sorted(owned(late), key=lambda v: v['Index']) if it['Holder'] == 100]
    for i, it in enumerate(stock):
        bx, by = 36 + (i % 3) * 270, y + 26 + (i // 3) * 46
        off = not it['Effect']
        thumb(c, it['Index'], (bx, by, bx + 42, by + 40), off)
        c.text((bx + 52, by + 20), it['Name'], font(14), fill=OFF_TEXT if off else TEXT, anchor='lm')
    # the chooser on Cordoba's slot
    sx0, sy0, sx1, sy1 = slot_of[8]
    pop = (sx1 + 10, sy0 - 120, sx1 + 10 + 330, sy0 - 120 + 330)
    shadow(c, pop, 24)
    c.rect(pop, fill=(26, 19, 14, 252), outline=GOLD, width=2)
    c.poly([(sx1 + 10, sy0 + 20), (sx1 - 2, sy0 + 30), (sx1 + 10, sy0 + 40)], fill=GOLD)
    c.text((pop[0] + 16, pop[1] + 22), 'Equip on Cordoba', cinzel(14, 700), fill=NAME, anchor='lm')
    divider(c, pop[0] + 12, pop[2] - 12, pop[1] + 40)
    yy = pop[1] + 50
    for it in stock:
        off = not it['Effect']
        hover = it['Index'] == 21
        r = (pop[0] + 10, yy, pop[2] - 10, yy + 38)
        if hover:
            c.rect(r, fill=HOVER, outline=HOVER_LINE, width=1)
        thumb(c, it['Index'], (r[0] + 4, yy + 3, r[0] + 38, yy + 35), off)
        c.text((r[0] + 48, yy + 19), it['Name'], font(14), fill=OFF_TEXT if off else (NAME if hover else TEXT), anchor='lm')
        c.text((r[2] - 8, yy + 19), "can't be equipped" if off else 'In stock', font(11), fill=OFF_TEXT if off else QUIET, anchor='rm')
        yy += 40
    c.line((pop[0] + 14, yy + 4), (pop[2] - 14, yy + 4), fill=DIM)
    c.text((pop[0] + 24, yy + 24), 'Take off Magic Barrier', font(14), fill=RED, anchor='lm')
    pointer(c, pop[0] + 200, pop[1] + 50 + 2 * 40 + 22)
    # the item looked at
    right = (872, 16, 1264, 784)
    lq_panel(c, right)
    c.text((892, 42), late[21]['Name'], brush(20), fill=NAME, anchor='lm')
    c.text((1244, 44), 'In stock', font(13), fill=QUIET, anchor='rm')
    divider(c, 886, 1250, 62)
    put_picture(c, item_picture(21), (886, 76, 1250, 310))
    c.text((892, 340), 'For Cordoba', cinzel(13, 700), fill=LABEL, anchor='lm')
    c.text((892, 366), 'Magic Barrier goes back to stock.', font(14), fill=QUIET, anchor='lm')
    small_button(c, (1250 - 120, 740, 1250, 772), 'End', f=cinzel(14, 700))
    c.save('U2-roster.png', 'U2 — the characters as cards with an item slot; a click on the slot opens a chooser',
           'No dragging: Cordoba\'s slot clicked, the items in stock listed with their pictures, one click equips (his own back to '
           'stock), "Take off" at the foot. The hovered item\'s picture right; the stock under the cards.')


def u3_ledger():
    """U3: the Subordinates screen's ledger with an Item column; the item cell opens a list."""
    late = items_with(LATE_STOCK)
    held = item_of(late)
    c = Canvas()
    backdrop(c)
    # below: the character and the item (first: the open list lies over them)
    put_picture(c, unit_picture(8), (16, 440, 506, 746))
    caption_plate(c, (16, 752, 506, 772), 'Character', UNITS[8]['Name'])
    put_picture(c, item_picture(21), (530, 440, 1020, 746))
    caption_plate(c, (530, 752, 1020, 772), 'Item', late[21]['Name'])
    small_button(c, (1264 - 130, 740, 1264, 784), 'End')
    top = (16, 16, 1264, 420)
    lq_panel(c, top)
    title_band(c, top, 'Treasure', f'{len(owned(late))} items · {sum(1 for it in owned(late) if it["Holder"] == 100)} in stock', HEADER_DATE)
    cols = [('', 26, 'mark', 'r'), ('No.', 36, 'n', 'r'), ('Name', 120, 'name', 'l'), ('Type', 150, 'type', 'l'),
            ('Troops', 100, 'troops', 'r'), ('Lv', 44, 'lv', 'r'), ('Atk', 50, 'atk', 'r'), ('Def', 50, 'def', 'r'),
            ('Act', 50, 'act', 'r'), ('Mag', 50, 'mag', 'r'), ('', 20, 'gap', 'l'), ('Item', 400, 'item', 'l')]
    rows = []
    for n, u in UNITS.items():
        it = held.get(n)
        rows.append((n, {'n': str(n), 'name': u['Name'], 'type': u['Type'], 'troops': f"{u['Troops']}/{u['MaxTroops']}",
                         'lv': str(u['Level']), 'atk': str(u['Atk']), 'def': str(u['Def']), 'act': str(u['Attacks']),
                         'mag': str(u['Magic']), 'item': (it['Name'] + (' · never moves' if it['Index'] in FIXED else '')) if it else '!— none'}))
    ledger(c, (30, 70, 1250, 406), cols, rows, row_h=34, size=15, chosen=8)
    # the Item cell of Cordoba, open
    idx = list(UNITS).index(8)
    cx0 = 30 + 4 + sum(w for _, w, _, _ in cols[:-1])
    cy0 = 70 + 33 + 2 + idx * 34
    c.rect((cx0, cy0 + 2, 1246, cy0 + 32), outline=VERMILION, width=2)
    c.poly([(1230, cy0 + 13), (1240, cy0 + 13), (1235, cy0 + 20)], fill=VERMILION_INK)
    stock = [it for it in sorted(owned(late), key=lambda v: v['Index']) if it['Holder'] == 100]
    drop = (cx0, cy0 + 34, 1246, cy0 + 34 + 30 * (len(stock) + 1) + 12)
    shadow(c, drop, 20)
    c.rect(drop, fill=PAPER, outline=HEAD_INK, width=1)
    yy = drop[1] + 6
    for it in stock:
        off = not it['Effect']
        if it['Index'] == 21:
            c.overlay((drop[0] + 2, yy, drop[2] - 2, yy + 30), (170, 40, 28, 40))
        c.text((drop[0] + 14, yy + 15), it['Name'], font(15), fill=INK_GREY if off else INK, anchor='lm')
        c.text((drop[2] - 12, yy + 15), "can't be equipped" if off else 'in stock', font(12), fill=INK_GREY if off else INK_QUIET, anchor='rm')
        yy += 30
    c.line((drop[0] + 8, yy + 2), (drop[2] - 8, yy + 2), fill=PAPER_RULE)
    c.text((drop[0] + 14, yy + 17), 'Take off Magic Barrier', font(15), fill=VERMILION_INK, anchor='lm')
    pointer(c, drop[0] + 160, drop[1] + 6 + 2 * 30 + 10)
    c.save('U3-ledger.png', 'U3 — the Subordinates screen\'s ledger with an Item column; its cell opens a list',
           'Every character a row of the paper ledger as on the Subordinates screen; Cordoba\'s Item cell clicked: the stock as a '
           'list, one click equips, "Take off" at its foot. His status picture and the hovered item\'s picture below.')


def u_round():
    u1_showcase()
    u2_roster()
    u3_ledger()


# ---------------------------------------------------------------- round v: the original's screen in lacquer (as the harem's H2)

CLASSES = {0: 'Assault Troops', 1: 'Archers', 2: 'Mages', 3: 'Magic Warriors', 4: 'Phantom Beasts', 10: 'Demon Assault',
           11: 'Demon Magic'}
V_TITLE, V_PICTURE, V_INFO, V_GRID = (16, 16, 1264, 72), (16, 88, 682, 504), (16, 516, 682, 784), (690, 88, 1264, 784)


def v_cell(n):
    """The picker's cell n (Sｱｲﾃﾑ's 3x17 grid, left to right, top to bottom), as the harem's tablets."""
    col, row = (n - 1) % 3, (n - 1) // 3
    x, y = 708 + col * 180, 106 + row * 36
    return (x, y, x + 176, y + 32)


def v_base(view, hover, picked=None, items=None, hint=True, marked=None):
    """V's screen without the pointer: hint=False leaves the info panel's foot empty; marked is a cell drawn chosen."""
    items = items or ITEMS
    held = item_of(items)
    c = Canvas()
    backdrop(c)
    lq_panel(c, V_TITLE)
    c.text((38, 44), 'Treasure', brush(24), fill=NAME, anchor='lm')
    c.text((1242, 45), HEADER_DATE, cinzel(15, 700), fill=NAME, anchor='rm')
    # the picture: what the pointer is on, as the original's panel
    if view == 0:
        put_picture(c, unit_picture(hover), (V_PICTURE[0], V_PICTURE[1], V_PICTURE[2], V_PICTURE[3]))
    else:
        put_picture(c, item_picture(hover), V_PICTURE)
    lq_panel(c, V_INFO)
    x0, y0, x1, _ = V_INFO
    if view == 0:
        u = UNITS[hover]
        c.text((x0 + 24, y0 + 32), u['Name'], brush(24), fill=NAME, anchor='lm')
        c.text((x0 + 24 + c.width(u['Name'], brush(24)) + 14, y0 + 35), CLASSES.get(u.get('Class', 0), ''), cinzel(13, 700),
               fill=SUB, anchor='lm')
        stat(c, x0 + 24, y0 + 70, 'War cost', f"{u['DeployCost'] * 100:,}K")
        stat(c, x0 + 150, y0 + 70, 'Troops', str(u['Troops']))
        stat(c, x0 + 260, y0 + 70, 'Equipping', held[hover]['Name'] if hover in held else '—')
    else:
        it = items[hover]
        c.text((x0 + 24, y0 + 32), it['Name'], brush(24), fill=NAME, anchor='lm')
        ok = bool(it['Effect'])
        c.text((x0 + 24 + c.width(it['Name'], brush(24)) + 14, y0 + 35), 'Equippable' if ok else 'Unequippable', cinzel(13, 700),
               fill=SUB if ok else RED, anchor='lm')
        who = ('Nobody (in stock)' if it['Holder'] == 100 else UNITS[it['Holder']]['Name']) if ok else '—'
        stat(c, x0 + 24, y0 + 70, 'Held by', who)
    divider(c, x0 + 16, x1 - 16, y0 + 116)
    if picked:
        kind, n = picked
        img = unit_picture(n) if kind == 'unit' else item_picture(n)
        put_picture(c, img, (x0 + 24, y0 + 130, x0 + 24 + 186 * 1.1, y0 + 130 + 116 * 1.1))
        tx = x0 + 24 + 186 * 1.1 + 24
        if kind == 'unit':
            c.text((tx, y0 + 160), UNITS[n]['Name'], brush(22), fill=NAME, anchor='lm')
            c.text((tx, y0 + 190), 'is equipping —', font(16), fill=TEXT, anchor='lm')
            c.text((tx, y0 + 216), 'Which item?', font(16), fill=TEXT, anchor='lm')
        else:
            c.text((tx, y0 + 160), 'Equipping', font(16), fill=TEXT, anchor='lm')
            c.text((tx, y0 + 188), items[n]['Name'], brush(22), fill=NAME, anchor='lm')
            c.text((tx, y0 + 216), 'Which character?', font(16), fill=TEXT, anchor='lm')
        small_button(c, (x1 - 24 - 110, y0 + 214, x1 - 24, y0 + 248), 'Back', f=cinzel(14, 700))
    elif hint:
        c.text((x0 + 24, y0 + 148), 'Pick a character, then the item to equip —', font(14), fill=QUIET, anchor='lm')
        c.text((x0 + 24, y0 + 170), 'or an item, then who is to equip it.', font(14), fill=QUIET, anchor='lm')
    # the grid
    lq_panel(c, V_GRID)
    for n in range(1, 52):
        r = v_cell(n)
        entry = None
        if view == 0 and n in UNITS:
            entry = ('unit', n)
        if view == 1 and items.get(n, {}).get('Holder'):
            entry = ('item', n)
        if n == 51 or entry is None:
            c.overlay(r, (40, 35, 30, 140))
            c.rect(r, outline=OFF_LINE + (0,) if False else (66, 58, 48, 150), width=1)
            continue
        is_hover = n == hover
        is_picked = (picked is not None and picked[1] == n and picked[0] == entry[0]) or n == marked
        row_box(c, r, 'chosen' if is_picked else ('hover' if is_hover else 'normal'))
        mid = (r[1] + r[3]) / 2
        if view == 0:
            u = UNITS[n]
            c.text((r[0] + 10, mid), u['Name'], font(14), fill=NAME if is_hover else TEXT, anchor='lm')
            if n in held:
                diamond(c, r[2] - 12, mid, 4, OFF_LINE if held[n]['Index'] in FIXED else GOLD)
        else:
            it = items[n]
            cant, fixed = not it['Effect'], n in FIXED
            ink = OFF_TEXT if cant else (NAME if it['Holder'] in UNITS else TEXT)
            c.text((r[0] + 10, mid), it['Name'], font(14), fill=ink, anchor='lm')
            if it['Holder'] in UNITS:
                c.text((r[2] - 8, mid + 1), UNITS[it['Holder']]['Name'], font(10), fill=QUIET, anchor='rm')
            if fixed:
                lock(c, r[2] - 12 - c.width(UNITS[it['Holder']]['Name'], font(10)) - 10, mid + 3, QUIET)
    # the bar: the view's tabs and End
    for i, label in enumerate(('Characters', 'Items')):
        r = (708 + i * 182, 726, 708 + i * 182 + 176, 766)
        on = i == view
        c.rect(r, fill=(60, 30, 22, 255) if on else ROW, outline=GOLD if on else DIM, width=2 if on else 1)
        c.text(((r[0] + r[2]) / 2, (r[1] + r[3]) / 2), label, cinzel(15, 700), fill=NAME if on else QUIET, anchor='mm')
    small_button(c, (1106, 726, 1246, 766), 'End', f=cinzel(16, 700))
    return c


def v_screen(name, caption, note, view, hover, picked=None, items=None, question=None):
    c = v_base(view, hover, picked, items)
    hx, hy = (v_cell(hover)[0] + 120, v_cell(hover)[1] + 18)
    if question:
        # the script's own question, in our message window and menu (docs/ui/messages.md)
        q = (120, 300, 760, 420)
        shadow(c, q, 22)
        lq_panel(c, q)
        for i, line in enumerate(question):
            c.text((q[0] + 30, q[1] + 34 + i * 26), line, font(17), fill=TEXT, anchor='lm')
        m = (hx - 40, hy + 18, hx + 120, hy + 18 + 84)
        shadow(c, m, 18)
        c.rect(m, fill=(26, 19, 14, 252), outline=GOLD, width=2)
        for i, label in enumerate(('Yes', 'No')):
            rr = (m[0] + 8, m[1] + 8 + i * 36, m[2] - 8, m[1] + 8 + i * 36 + 32)
            row_box(c, rr, 'hover' if i == 0 else 'normal')
            c.text((rr[0] + 16, (rr[1] + rr[3]) / 2), label, font(16), fill=NAME if i == 0 else TEXT, anchor='lm')
        pointer(c, m[0] + 60, m[1] + 22)
    else:
        pointer(c, hx, hy)
    c.save(name, caption, note)


def v_round():
    late = items_with(LATE_STOCK)
    v_screen('V1-characters.png', 'V1 — the original\'s screen in lacquer: the characters\' view (week 18)',
             'As the harem\'s screen: the picture panel of what the pointer is on, its line under it, the 3x17 grid with each '
             'character at his number\'s cell; the bar\'s Item/Character as two tabs, End.', 0, 8)
    v_screen('V2-which-item.png', 'V2 — Cordoba picked: the items\' view asks "Which item?" (a later game\'s stock, made up)',
             'The view turns as in the original, the first pick shrunk below with its prompt and Back (the original\'s right '
             'click); the items at their numbers\' cells, the held ones in gold with their holder.', 1, 21, ('unit', 8), late)
    v_screen('V3-question.png', 'V3 — an item another unit holds: the original\'s question, in our window and menu',
             'Refusals and questions stay the script\'s own, drawn as our message windows and menus.', 1, 7, ('unit', 8), late,
             ['By Lord is equipped to Rick, but would you', 'like to remove it and equip Cordoba instead?'])


# ---------------------------------------------------------------- round w: the script's question and its Yes/No in one place

UNEQUIP = ['By Lord is equipped to', 'Rick, but would you', 'like to unequip it?']          # Sｱｲﾃﾑ.ADV:320-328
REPLACE = ['By Lord is equipped to', 'Rick, but would you', 'like to remove it and equip', 'Cordoba instead?']  # :339-350
W_TEXT = (238, 222, 176, 255)


def click_triangle(c, x, y):
    """Lacquer's triangle asking for the click (the key wait)."""
    c.poly([(x - 7, y - 5), (x + 7, y - 5), (x, y + 5)], fill=GOLD)


def w_lines(c, x, y, lines, size=19, step=28, anchor='lm'):
    for i, line in enumerate(lines):
        c.text((x, y + i * step), line, font(size), fill=W_TEXT, anchor=anchor)


def w_answers(c, x, y, w=136, h=38, gap=14, vertical=False, hover=0):
    """Yes (red lacquer, the script's first item) and No, side by side or stacked."""
    for i, label in enumerate(('Yes', 'No')):
        dx, dy = (0, i * (h + gap)) if vertical else (i * (w + gap), 0)
        r = (x + dx, y + dy, x + dx + w, y + dy + h)
        if i == 0:
            red_button(c, r, label, hover=hover == 0, tf=cinzel(17, 700))
        else:
            small_button(c, r, label, 'hover' if hover == 1 else 'normal', f=cinzel(17, 700))
    return (x + 30, y + h / 2)


def w_popover(c, cell, lines, answers=True, width=440):
    """A lacquer callout under the clicked cell, its tail on the cell; the question and its answers inside."""
    x0 = min(cell[0] - 8, 1256 - width)
    y0 = cell[3] + 14
    h = 28 + len(lines) * 28 + (64 if answers else 30)
    r = (x0, y0, x0 + width, y0 + h)
    lq_panel(c, r, fill=(30, 22, 16, 250))
    tx = cell[0] + 60
    c.poly([(tx - 13, y0 + 2), (tx, y0 - 13), (tx + 13, y0 + 2)], fill=GOLD)
    c.poly([(tx - 10, y0 + 3), (tx, y0 - 8), (tx + 10, y0 + 3)], fill=(30, 22, 16, 255))
    w_lines(c, x0 + 26, y0 + 30, lines)
    if answers:
        return w_answers(c, x0 + 26, y0 + h - 56)
    click_triangle(c, x0 + width - 26, y0 + h - 22)
    return None


def w_round():
    late = items_with(LATE_STOCK)
    cell = v_cell(7)
    at_cell = (cell[0] + 120, cell[1] + 18)

    # W1 — a dialog in the middle of the screen, the rest dimmed
    c = v_base(1, 7, marked=7)
    c.overlay((0, 0, W, H), (0, 0, 0, 120))
    r = (370, 290, 910, 480)
    lq_panel(c, r, fill=(30, 22, 16, 252))
    w_lines(c, 640, 334, UNEQUIP, anchor='mm')
    w_answers(c, 640 - 143, 418, hover=None)
    pointer(c, *at_cell)
    c.save('W1-dialog.png', 'W1 — one dialog in the middle: the question over its Yes and No, the screen dimmed',
           'As the battle\'s retreat dialog: the asked item stays marked in the grid; Yes in red lacquer, No beside it. '
           'The pointer has to travel from the cell to the middle.')

    # W2 — a callout at the clicked cell
    c = v_base(1, 7, marked=7, hint=False)
    yes = w_popover(c, cell, UNEQUIP)
    pointer(c, *yes)
    c.save('W2-callout.png', 'W2 — a callout at the clicked cell: the question and Yes/No where the click was',
           'Its tail on the asked item; the eyes and the pointer stay by the cell, as the original\'s menu at the pointer. '
           'It covers part of the grid, never the picture.')

    # W2b — the longer question, a unit picked first (a later game's stock, made up)
    c = v_base(1, 7, ('unit', 8), late, marked=7)
    yes = w_popover(c, cell, REPLACE)
    pointer(c, *yes)
    c.save('W2b-callout-replace.png', 'W2b — W2 with the longer question: Cordoba picked, By Lord held by Rick (made-up stock)',
           'Four lines of the script\'s text; the callout grows down. Cordoba\'s pick stays in the panel at the left.')

    # W2c — a refusal in the same callout: a key wait, any click closes it
    cordoba_item = v_cell(35)
    c = v_base(1, 35, ('unit', 8), late, marked=35)
    w_popover(c, cordoba_item, ['Already equipped.'], answers=False, width=300)
    pointer(c, cordoba_item[0] + 120, cordoba_item[1] + 18)
    c.save('W2c-callout-refusal.png', f'W2c — a refusal in the same callout: "Already equipped." (Cordoba\'s own {late[35]["Name"]})',
           'The script\'s key wait: the triangle asks for a click anywhere, as our message windows do.')

    # W3 — the message window keeps its place, the answers move into it
    c = v_base(1, 7, marked=7)
    r = (262, 560, 1020, 790)
    lq_panel(c, r, fill=(44, 32, 22, 250))
    w_lines(c, 300, 612, UNEQUIP, size=22, step=34)
    c.line((842, 586), (842, 764), fill=DIM)
    yes = w_answers(c, 866, 612, w=128, h=42, gap=16, vertical=True)
    pointer(c, *yes)
    c.save('W3-window-answers.png', 'W3 — the message window where the script opens it, Yes and No inside it at its right',
           'The smallest change: no menu at the pointer any more; the answers stand beside the text they answer.')

    # W4 — no window: the question in the info panel, under the asked item's figures
    c = v_base(1, 7, marked=7, hint=False)
    x0, y0, _, _ = V_INFO
    seal(c, x0 + 24, y0 + 136, '?', size=22)
    w_lines(c, x0 + 60, y0 + 147, [' '.join(UNEQUIP[:2]), UNEQUIP[2]])
    yes = w_answers(c, x0 + 60, y0 + 206)
    pointer(c, *yes)
    c.save('W4-panel.png', 'W4 — no window: the question in the info panel, under the asked item\'s name and holder',
           'The panel already tells By Lord and Rick; the question and Yes/No take the hint\'s place, the item marked in the '
           'grid. Nothing covers the screen.')

    # W4b — a refusal in the panel
    c = v_base(1, 35, ('unit', 8), late, marked=35)
    r = (x0 + 24 + 186 * 1.1 + 14, y0 + 134, 666, y0 + 204)
    c.rect(r, fill=(60, 30, 22, 255), outline=VERMILION, width=2)
    seal(c, r[0] + 12, (r[1] + r[3]) / 2 - 11, '!', size=22)
    c.text((r[0] + 46, (r[1] + r[3]) / 2), 'Already equipped.', font(19), fill=W_TEXT, anchor='lm')
    click_triangle(c, r[2] - 22, (r[1] + r[3]) / 2)
    pointer(c, cordoba_item[0] + 120, cordoba_item[1] + 18)
    c.save('W4b-panel-refusal.png', 'W4b — a refusal in the panel: a vermilion strip over the pick\'s prompt, any click closes it',
           f'Cordoba picked, his own {late[35]["Name"]} clicked (made-up stock).')


if len(sys.argv) > 6 and sys.argv[6] == 'w':
    w_round()
    sys.exit()
if len(sys.argv) > 6 and sys.argv[6] == 'v':
    v_round()
    sys.exit()
if len(sys.argv) > 6 and sys.argv[6] == 'u':
    u_round()
    sys.exit()
if len(sys.argv) > 6 and sys.argv[6] == 's':
    s_round()
    sys.exit()
if len(sys.argv) > 6 and sys.argv[6] == 't':
    t_round()
    sys.exit()
original(HAREM_DUMP, 'H0-original.png', 'H0 — the original\'s harem picker (week 18)',
         'Sﾊｰﾚﾑ: the girls by their number in a 3x16 grid, her picture and her count/last date when hovered, End.')
original(ITEMS_DUMP, 'I0-original.png', 'I0 — the original\'s item screen (the bot\'s game, week 26)',
         'Sｱｲﾃﾑ: one view at a time (characters or items, the bar\'s Item/Character turns it), a pick then the other view, '
         'Yes/No when an item is held.')
h1_ledger()
h2_tablets()
h2_tablets(LATE_OFFER, late_hist, 9, 'H2b-tablets-late.png', LATE_WEEK, late=True)
h3_stage()
i1_two_ledgers()
i1_two_ledgers(items_with(LATE_STOCK), unit=8, item=21, name='I1b-two-ledgers-late.png', late=True)
i2_slots()
i3_tabs()
