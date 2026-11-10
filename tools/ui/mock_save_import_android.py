r"""Mock-ups of importing the original game's saves on Android (docs/save-import.md, §8; 2026-11-10): the PC's File ->
Import Original Save... opens a native file dialog, whose answer on Android is a content:// URI, and Android's picker
hides Android/data, where the player's copy of the game folder (and the original's saves in it) lives. So on Android
the saves the game folder holds are offered by the game itself.

Drawn over the screen gallery's dumps (docs/ui/gallery.md): the phone's Menu sheet held landscape and upright
(`menu-sheet`, LN3 and PN1), the phone's Save/Load screen (`save-load-original`, LL1) and the tablet's map (`map`). From
the repository's root (Pillow; BIZ UDGothic, Cinzel and Shojumaru of godot/fonts):
  python -I tools/ui/mock_save_import_android.py <screen-gallery dir> <out dir> < /dev/null
Round 3: N1 / N1p the Menu sheet with a tile (a row upright) "Import original save" between Screenshot and Restart, as
on the PC's File menu; S1 / S1p its page "Original saves", the game folder's saves as tiles (rows) under < Menu, S1q /
S1qp the question at a save's tile, S1r a save that cannot be imported, S1n none found; S2 the alternative, a screen of
its own in Save/Load's look (a list and the chosen save's page with its map); T1 the tablet: the menu bar's item opens a
stock dialog listing the same saves. The saves are the owner's ten (docs/save-import.md, §4); S2's map and figures are
the dump's (week 18, which slot E is). The images hold the player's CGs: keep <out dir> outside the repository's
tracked files.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

GALLERY, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
sys.stdout.reconfigure(encoding='utf-8')
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
S = 2                                   # drawn at 2x, scaled down
CAPTION = 70

PANEL, GOLD, DIM = (24, 18, 14, 255), (196, 160, 86, 255), (110, 86, 46, 255)
NAME, TEXT, QUIET, OFF = (232, 199, 122, 255), (236, 226, 204, 255), (176, 160, 130, 255), (98, 88, 74, 255)
ROW, VERMILION, CHOSEN, SEAL_TEXT = (34, 26, 20, 255), (170, 40, 28, 255), (60, 30, 22, 255), (246, 230, 206, 255)
RED_TOP, RED_BOTTOM = (136, 30, 24, 255), (98, 18, 16, 255)
VERMILION_INK = (214, 96, 70, 255)

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
BRUSH = REPO + '/godot/fonts/Shojumaru-Regular.ttf'
SANS = 'C:/Windows/Fonts/segoeui.ttf' if os.path.exists('C:/Windows/Fonts/segoeui.ttf') else GOTHIC
_fonts = {}


def font(size, path=GOTHIC, weight=None):
    key = (round(size * S), path, weight)
    if key not in _fonts:
        f = ImageFont.truetype(path, round(size * S))
        if weight is not None:
            f.set_variation_by_axes([weight])
        _fonts[key] = f
    return _fonts[key]


def cinzel(size):
    return font(size, CINZEL, 700)


def brush(size):
    return font(size, BRUSH)


# the owner's ten (docs/save-import.md, §4): slot, week, stage, month, week of the month
SAVES = [('A', 22, 2, 6, 1), ('B', 20, 1, 5, 3), ('C', 15, 1, 4, 2), ('D', 17, 1, 4, 4), ('E', 18, 1, 5, 1),
         ('F', 19, 2, 5, 2), ('G', 22, 2, 6, 1), ('H', 21, 2, 5, 4), ('I', 16, 1, 4, 3), ('J', 15, 1, 4, 1)]
STAGES = {1: 'Stage 1 · Leazas civil war', 2: 'Stage 2 · wars of humanity'}
FOLDER = 'Android/data/org.kichikuou.port/files/game'


def short(s):
    return f'LP 3 · M{s[3]} · W{s[4]}'


def long(s):
    return f'LP 3 · Month {s[3]} · Week {s[4]}'


class Canvas:
    def __init__(self, dump, size=None, fit=None):
        img = Image.open(os.path.join(GALLERY, dump + '.png')).convert('RGBA')
        if size is None:
            size = img.size
        self.w, self.h = size
        self.src = img
        self.im = Image.new('RGBA', (self.w * S, (self.h + CAPTION) * S), (0, 0, 0, 255))
        if fit is None:
            self.im.paste(img.resize((self.w * S, self.h * S), Image.LANCZOS), (0, 0))
        else:
            x, y, w, h = fit
            self.im.paste(img.resize((round(w * S), round(h * S)), Image.LANCZOS), (round(x * S), round(y * S)))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill, outline=outline,
                         width=max(1, round(width * S)) if outline else 0)

    def text(self, xy, s, f, fill=TEXT, anchor='lm'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=DIM, width=1.0):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=max(1, round(width * S)))

    def poly(self, pts, fill=None, outline=None, width=1.0):
        self.d.polygon([(x * S, y * S) for x, y in pts], fill=fill)
        if outline:
            self.d.line([(x * S, y * S) for x, y in pts], fill=outline, width=max(1, round(width * S)))

    def veil(self, r, alpha=0.6):
        layer = Image.new('RGBA', self.im.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).rectangle([r[0] * S, r[1] * S, r[2] * S, r[3] * S], fill=(0, 0, 0, round(255 * alpha)))
        self.im.alpha_composite(layer)
        self.d = ImageDraw.Draw(self.im)

    def save(self, name, caption, note):
        self.rect((0, self.h, self.w, self.h + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, self.h), (self.w, self.h), fill=DIM)
        size = 20 if self.w >= 1200 else 15
        self.text((14, self.h + 22), caption, cinzel(size), fill=NAME)
        y = self.h + 46
        for part in note.split('\n'):
            self.text((14, y), part, font(size * 0.72), fill=QUIET)
            y += size * 0.95
        self.im.resize((self.w, self.h + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
        print(name)


def panel(c, r, k):
    """Ui.DrawPanel: the lacquer panel, a gold line and its corner studs."""
    c.rect(r, fill=PANEL, outline=GOLD, width=max(1, 2 * k))
    for x, y in ((r[0], r[1]), (r[2], r[1]), (r[0], r[3]), (r[2], r[3])):
        d = 7 * k
        c.poly([(x - d, y), (x, y - d), (x + d, y), (x, y + d)], fill=GOLD)


def tile_box(c, r, k, held=False, off=False):
    c.rect(r, fill=ROW, outline=DIM, width=max(1, 2 * k))
    if held:
        c.rect((r[0] + 4 * k, r[1] + 4 * k, r[2] - 4 * k, r[3] - 4 * k), fill=(56, 46, 36, 255))


def red_button(c, r, label, size):
    x0, y0, x1, y1 = r
    c.rect(r, fill=RED_TOP, outline=GOLD, width=1.5)
    c.rect((x0 + 3, (y0 + y1) / 2, x1 - 3, y1 - 3), fill=RED_BOTTOM)
    c.text(((x0 + x1) / 2, (y0 + y1) / 2), label, cinzel(size), fill=NAME, anchor='mm')


def plain_button(c, r, label, size):
    c.rect(r, fill=ROW, outline=DIM, width=1.5)
    c.text(((r[0] + r[2]) / 2, (r[1] + r[3]) / 2), label, cinzel(size), fill=TEXT, anchor='mm')


def callout(c, box, tip, below, k, title, lines, confirm, ink=TEXT):
    """PhoneMenu.QuestionAt's callout: the panel, its tip at the tile, the title in the brush, the lines, the red button | Cancel."""
    panel(c, box, k)
    tx, ty = tip
    edge = box[1] if below else box[3]
    w = 30 * k
    c.poly([(tx - w, edge), (tx, ty), (tx + w, edge)], fill=PANEL)
    c.line((tx - w, edge), (tx, ty), fill=GOLD, width=max(1, 2 * k))
    c.line((tx, ty), (tx + w, edge), fill=GOLD, width=max(1, 2 * k))
    x = box[0] + 40 * k
    c.text((x, box[1] + 64 * k), title, brush(52 * k), fill=NAME)
    y = box[1] + 132 * k
    for line in lines:
        c.text((x, y), line[0], font(40 * k), fill=line[1] if len(line) > 1 else ink)
        y += 50 * k
    bm, bh, bg = 40 * k, 130 * k, 8 * k
    by = box[3] - bm - bh
    mid = (box[0] + box[2]) / 2
    if confirm:
        red_button(c, (box[0] + bm, by, mid - bg, by + bh), confirm, 46 * k)
        plain_button(c, (mid + bg, by, box[2] - bm, by + bh), 'Cancel', 46 * k)
    else:
        plain_button(c, (mid + bg, by, box[2] - bm, by + bh), 'OK', 46 * k)


# ---- held landscape: the dump is at 2/3 of the reference phone's pixels (docs/ui/gallery.md, §9)

KL = 2 / 3
PANEL_L = (362, 102, 1446, 738)          # PhoneMenu.Panel in the dump


def land_tiles(count):
    """PhoneMenu.Tiles(f, count): four a row filling the panel under its title."""
    p, k = PANEL_L, KL
    x0, y0, gap = p[0] + 30 * k, p[1] + 140 * k, 16 * k
    width, height = p[2] - 30 * k - x0, p[3] - 30 * k - y0
    rows = max(1, (count + 3) // 4)
    w, h = (width - 3 * gap) / 4, (height - (rows - 1) * gap) / rows
    return [(x0 + i % 4 * (w + gap), y0 + i // 4 * (h + gap), x0 + i % 4 * (w + gap) + w, y0 + i // 4 * (h + gap) + h)
            for i in range(count)]


def land_sheet(c, title, clear=True):
    p, k = PANEL_L, KL
    if clear:
        panel(c, p, k)
    c.text((p[0] + 40 * k, p[1] + 70 * k), title, brush(56 * k), fill=NAME)


def word_tile(c, r, label, sub='', k=KL, lit=False, off=False, held=False, arrow=False):
    tile_box(c, r, k, held=held)
    if lit:
        c.rect(r, fill=(96, 72, 30, 255), outline=GOLD, width=max(1, 2 * k))
    cx, cy = (r[0] + r[2]) / 2, (r[1] + r[3]) / 2
    f = font(44 * k)
    words = [label]
    if c.width(label, f) > r[2] - r[0] - 30 * k and ' ' in label:
        mid = len(label) // 2
        at = min((i for i, ch in enumerate(label) if ch == ' '), key=lambda i: abs(i - mid))
        words = [label[:at], label[at + 1:]]
    top = cy - 26 * k - (len(words) - 1) * 50 * k
    for i, w in enumerate(words):
        c.text((cx, top + i * 50 * k), w, f, fill=OFF if off else TEXT, anchor='mm')
    if sub:
        c.text((cx, cy + 40 * k), sub, font((48 if arrow else 32) * k), fill=OFF if off else NAME if lit else QUIET, anchor='mm')


def save_tile(c, r, s, k=KL, off=False, held=False, why=''):
    """S1: a save's tile — its slot's letter, the date, the stage; one that cannot be imported greyed, its reason short."""
    tile_box(c, r, k, held=held)
    cx, cy = (r[0] + r[2]) / 2, (r[1] + r[3]) / 2
    c.text((cx, cy - 42 * k), f'Save {s[0]}', font(44 * k), fill=OFF if off else TEXT, anchor='mm')
    if off:
        c.text((cx, cy + 14 * k), why, font(30 * k), fill=OFF, anchor='mm')
        c.text((cx, cy + 56 * k), 'cannot be imported', font(30 * k), fill=OFF, anchor='mm')
        return
    c.text((cx, cy + 14 * k), short(s), font(32 * k), fill=QUIET, anchor='mm')
    c.text((cx, cy + 58 * k), f'Stage {s[2]}', font(30 * k), fill=QUIET, anchor='mm')


MAIN_L = [('Options', ''), ('Message skip', 'Off'), ('Sound', 'On'), ('Screenshot', ''),
          ('Import original save', '›'), ('Restart', 'asks first'), ('Exit', 'asks first'), ('Debug', '›')]


def n1():
    c = Canvas('phone-landscape/current/menu-sheet')
    boxes = land_tiles(8)
    for i, (label, sub) in enumerate(MAIN_L):
        c.rect(boxes[i], fill=PANEL)
        word_tile(c, boxes[i], label, sub, lit=label == 'Sound', arrow=sub == '›', held=label.startswith('Import'))
    c.save('N1.png', 'N1 — the Menu sheet held landscape: "Import original save ›" between Screenshot and Restart',
           'As on the PC\'s File menu. A page tile (›): its tap opens the page of the saves (S1). A release build has no Debug,'
           ' so its second row is Import original save, Restart, Exit.')


def page_l(c, saves, held=None, off=None):
    land_sheet(c, 'Original saves')
    boxes = land_tiles(len(saves) + 1)
    word_tile(c, boxes[0], '‹ Menu')
    for i, s in enumerate(saves):
        save_tile(c, boxes[i + 1], s, held=held == s[0], off=off == s[0], why='another version' if off == s[0] else '')
    return boxes


def s1():
    c = Canvas('phone-landscape/current/menu-sheet')
    page_l(c, SAVES)
    c.save('S1.png', 'S1 — its page: the game folder\'s saves as tiles, ‹ Menu first (as Debug\'s page)',
           'Each tile the slot\'s letter (KICHIKUSA.asd is save A), the week\'s date and the story stage, read when the page opens.'
           '\nA tap asks first (S1q). Up to ten saves (the original writes slots A-J) fit three rows.')


def s1q():
    c = Canvas('phone-landscape/current/menu-sheet')
    boxes = page_l(c, SAVES, held='E')
    t = boxes[5]
    k = KL
    w, h = 1100 * k, 470 * k
    p = PANEL_L
    x = max(p[0] + 30 * k, min(t[0], p[2] - 30 * k - w))
    box = (x, t[1] - 30 * k - h, x + w, t[1] - 30 * k)
    callout(c, box, ((t[0] + t[2]) / 2, t[1]), False, k, "Load the original's save?",
            [('Original save E: LP 3 · Month 5 · Week 1',), ('KICHIKUSE.asd, saved 10 Oct 2026, 21:40', QUIET),
             ('Any unsaved progress will be lost.',)], 'Load')
    c.save('S1q.png', 'S1q — a save\'s tap: the PC\'s question in the sheet\'s callout, Load | Cancel',
           'The same words as the PC\'s box (docs/save-import.md, §7). Load: the game starts again from the boot and loads it at once,'
           '\nnothing written; Cancel: back to the page.')


def s1r():
    c = Canvas('phone-landscape/current/menu-sheet')
    boxes = page_l(c, SAVES, off='J')
    t = boxes[10]
    k = KL
    w, h = 1100 * k, 460 * k
    p = PANEL_L
    x = max(p[0] + 30 * k, min(t[0], p[2] - 30 * k - w))
    box = (x, t[1] - 30 * k - h, x + w, t[1] - 30 * k)
    callout(c, box, ((t[0] + t[2]) / 2, t[1]), False, k, 'KICHIKUSJ.asd cannot be imported.',
            [('It is not a save of the version of Kichikuou Rance', QUIET), ('this game reads (the English translation 1.0).', QUIET)], None)
    c.save('S1r.png', 'S1r — a save that cannot be imported (a sample: J): greyed, its reason a word; a tap says why, OK',
           'The reader\'s own words, as the PC\'s message box (another version, another game, not made at a week\'s start).')


def s1n():
    c = Canvas('phone-landscape/current/menu-sheet')
    land_sheet(c, 'Original saves')
    boxes = land_tiles(4)
    word_tile(c, boxes[0], '‹ Menu')
    k = KL
    x, y = boxes[1][0] + 30 * k, boxes[1][1] + 50 * k
    lines = [('No saves of the original in the game folder.', TEXT),
             ('The original writes them next to KICHIKUSA.ALD,', QUIET),
             ('as KICHIKUSA.asd … KICHIKUSJ.asd. Copy them', QUIET),
             ('with the game into', QUIET), (FOLDER + '.', NAME)]
    for text, ink in lines:
        c.text((x, y), text, font(34 * k), fill=ink)
        y += 52 * k
    c.save('S1n.png', 'S1n — no save found: the page says where the original keeps them and where they go',
           'The folder\'s path as the README names it ("Installing").')


# ---- upright: the dump is at half the reference phone's pixels

KP = 0.5
BAR_Y = 1251                             # PhoneFrame.Bar's top in the dump
ROOM_W = 610


def up_rows(n, rule_over=None):
    """PhoneMenu.Tiles upright: rows 130 high, 12 apart, ending 40 over the bar; a rule's 40 over row `rule_over` (top down)."""
    k = KP
    stack = n * (130 + 12) * k - 12 * k + (40 * k if rule_over is not None else 0)
    y = BAR_Y - 40 * k - stack
    rows = []
    for i in range(n):
        if i == rule_over:
            y += 40 * k
        rows.append((40 * k, y, ROOM_W - 40 * k, y + 130 * k))
        y += 142 * k
    return rows


def up_sheet(c, rows, title):
    k = KP
    top = rows[0][1] - 130 * k
    p = (8, top, ROOM_W - 8, BAR_Y - 14 * k)
    panel(c, p, k)
    c.text((p[0] + 32 * k, p[1] + 66 * k), title, brush(56 * k), fill=NAME)
    return p


def up_row(c, r, word, right='', held=False, arrow=False, sw=None):
    k = KP
    tile_box(c, r, k, held=held)
    cy = (r[1] + r[3]) / 2
    c.text((r[0] + 36 * k, cy), word, font(46 * k), fill=TEXT)
    if arrow:
        c.text((r[2] - 40 * k, cy), '›', font(72 * k), fill=GOLD, anchor='rm')
    if sw is not None:
        w, h = 150 * k, 72 * k
        x1 = r[2] - 28 * k
        pill = (x1 - w, cy - h / 2, x1, cy + h / 2)
        c.d.rounded_rectangle([v * S for v in pill], radius=h / 2 * S, fill=(150, 116, 52, 255) if sw else (40, 32, 26, 255),
                              outline=GOLD if sw else DIM, width=S)
        kx = pill[2] - h / 2 if sw else pill[0] + h / 2
        rr = h / 2 - 9 * k
        c.d.ellipse([(kx - rr) * S, (cy - rr) * S, (kx + rr) * S, (cy + rr) * S], fill=NAME if sw else QUIET)
        c.text((pill[0] - 18 * k, cy), 'On' if sw else 'Off', font(34 * k), fill=TEXT if sw else QUIET, anchor='rm')


def n1p():
    c = Canvas('phone-portrait/current/menu-sheet')
    words = [('Debug', 'page'), ('Exit…', ''), ('Restart…', ''), ('Import original save', 'page'), ('Screenshot', ''),
             ('Sound', True), ('Message skip', False), ('Options', '')]
    rows = up_rows(len(words), rule_over=5)
    up_sheet(c, rows, 'Menu')
    for r, (w, kind) in zip(rows, words):
        up_row(c, r, w, held=w.startswith('Import'), arrow=kind == 'page', sw=kind if isinstance(kind, bool) else None)
    k = KP
    y = (rows[4][3] + rows[5][1]) / 2
    c.line((rows[4][0] + 20 * k, y), (rows[4][2] - 20 * k, y), fill=DIM, width=1)
    c.save('N1p.png', 'N1p — upright: the row between Screenshot and Restart…',
           'A page\'s row (›), as Debug\'s.\nIts page: S1p.')


def up_page(c, saves, held=None):
    rows = up_rows(len(saves) + 1)
    up_sheet(c, rows, 'Original saves')
    up_row(c, rows[0], '‹ Menu')
    k = KP
    for r, s in zip(rows[1:], saves):
        tile_box(c, r, k, held=held == s[0])
        cy = (r[1] + r[3]) / 2
        c.text((r[0] + 36 * k, cy), f'Save {s[0]}', font(46 * k), fill=TEXT)
        c.text((r[0] + 250 * k, cy), short(s), font(40 * k), fill=QUIET)
        c.text((r[2] - 32 * k, cy), f'Stage {s[2]}', font(36 * k), fill=QUIET, anchor='rm')
    return rows


def s1p():
    c = Canvas('phone-portrait/current/menu-sheet')
    up_page(c, SAVES)
    c.save('S1p.png', 'S1p — upright: the saves as rows top down,',
           '‹ Menu on top (as Debug\'s page): the letter,\nthe date, the stage at the right.')


def s1qp():
    c = Canvas('phone-portrait/current/menu-sheet')
    rows = up_page(c, SAVES, held='E')
    t = rows[5]
    k = KP
    h = 560 * k
    box = (60 * k, t[1] - 44 * k - h, ROOM_W - 60 * k, t[1] - 44 * k)
    callout(c, box, ((t[0] + t[2]) / 2, t[1]), False, k, "Load the original's save?",
            [('Original save E:',), ('LP 3 · Month 5 · Week 1',), ('KICHIKUSE.asd, 10 Oct 2026, 21:40', QUIET)], 'Load')
    c.text((box[0] + 40 * k, box[1] + 132 * k + 3 * 50 * k), 'Any unsaved progress will be lost.', font(40 * k), fill=TEXT)
    c.save('S1qp.png', 'S1qp — upright: the question over its row',
           'PN1q\'s callout, the lines wrapped\nat the phone\'s width.')


# ---- S2: a screen of its own in Save/Load's look (LL1), over `save-load-original`

def s2():
    c = Canvas('phone-landscape/current/save-load-original')
    c.rect((60, 0, 680, 205), fill=PANEL)                         # the title, the tabs, the switch and the heading
    c.text((80, 37), 'ORIGINAL SAVES', cinzel(30), fill=NAME)
    c.text((80, 90), 'IN THE GAME FOLDER · 10', cinzel(16), fill=QUIET)
    c.text((666, 90), 'KICHIKUSA … J.asd', font(18), fill=QUIET, anchor='rm')
    c.line((77, 116), (666, 116), fill=DIM)
    c.rect((60, 120, 680, 800), fill=PANEL)                       # the list
    top, pitch, h = 130, 76, 70
    order = SAVES
    for i, s in enumerate(order[:9]):
        y = top + i * pitch
        r = (77, y, 646, y + h)
        chosen = s[0] == 'E'
        c.rect(r, fill=CHOSEN if chosen else ROW, outline=VERMILION if chosen else DIM, width=2 if chosen else 1)
        c.rect((92, y + 17, 130, y + 53), fill=VERMILION)
        c.text((111, y + 36), s[0], cinzel(22), fill=SEAL_TEXT, anchor='mm')
        c.text((150, y + 35), short(s), font(28), fill=TEXT)
        c.text((632, y + 35), f'Stage {s[2]}', font(24), fill=QUIET, anchor='rm')
    c.rect((652, top, 662, top + 9 * pitch - 6), fill=(40, 32, 24, 255), outline=DIM, width=1)
    c.rect((653, top + 1, 661, top + (9 * pitch - 6) * 0.9), fill=GOLD)
    c.rect((690, 0, 1808, 70), fill=PANEL)                        # the page's title, no Delete
    c.text((697, 36), 'ORIGINAL SAVE E · LP 3 · MONTH 5 · WEEK 1', cinzel(24), fill=NAME)
    c.rect((1380, 500, 1740, 560), fill=PANEL)                    # written: the file's time
    c.text((1391, 514), 'WRITTEN', cinzel(13), fill=QUIET)
    c.text((1391, 539), '10 Oct 2026, 21:40', font(26), fill=TEXT)
    c.rect((690, 570, 1740, 666), fill=PANEL)                     # the comment: the file and what a load does
    c.text((699, 583), 'FILE', cinzel(13), fill=QUIET)
    c.text((709, 615), 'KICHIKUSE.asd, in the game folder', font(26), fill=TEXT)
    c.text((709, 650), 'Loaded at once, nothing written. Any unsaved progress will be lost.', font(22), fill=QUIET)
    c.save('S2.png', 'S2 (the alternative) — a screen of its own in Save/Load\'s look: the list, the chosen save\'s page with its map',
           'The Menu sheet\'s tile opens it; Load (the red button) loads at once, Back closes it. More to build than S1 (the screen in both'
           ' orientations); the map and figures here are the dump\'s week 18, which save E is.')


# ---- T1: the tablet keeps the menu bar; on Android its item opens a stock dialog listing the saves

def t1():
    W, H, BAR = 1280, 800, 24
    pw = H - BAR
    c = Canvas('tablet/current/map', size=(W, H), fit=((W - pw * 1.6) / 2, BAR, pw * 1.6, pw))
    c.rect((0, 0, W, BAR), fill=(240, 240, 240, 255))
    x = 8
    for item in ('File', 'Screen', 'Options', 'Sound[on]', 'MsgSkip[off]'):
        c.text((x + 8, BAR / 2), item, font(14, SANS), fill=(0, 0, 0, 255))
        x += c.width(item, font(14, SANS)) + 16
    dw, dh = 620, 470
    x0, y0 = (W - dw) / 2, (H - dh) / 2
    c.rect((x0, y0, x0 + dw, y0 + dh), fill=(54, 61, 74, 255), outline=(90, 100, 118, 255), width=1)
    c.rect((x0, y0, x0 + dw, y0 + 30), fill=(40, 45, 56, 255))
    c.text((x0 + dw / 2, y0 + 15), 'Import a save of the original game', font(15, SANS), fill=(230, 232, 236, 255), anchor='mm')
    c.text((x0 + 14, y0 + 50), 'The saves in the game folder:', font(14, SANS), fill=(210, 214, 222, 255))
    lx0, ly0, lx1, ly1 = x0 + 14, y0 + 66, x0 + dw - 14, y0 + dh - 60
    c.rect((lx0, ly0, lx1, ly1), fill=(33, 37, 45, 255), outline=(80, 88, 104, 255), width=1)
    for i, s in enumerate(SAVES):
        y = ly0 + 6 + i * 32
        if s[0] == 'E':
            c.rect((lx0 + 3, y, lx1 - 3, y + 30), fill=(70, 100, 150, 255))
        c.text((lx0 + 12, y + 15), f'Original save {s[0]}', font(15, SANS), fill=(236, 238, 242, 255))
        c.text((lx0 + 170, y + 15), long(s), font(15, SANS), fill=(236, 238, 242, 255))
        c.text((lx1 - 12, y + 15), STAGES[s[2]], font(14, SANS), fill=(176, 184, 198, 255), anchor='rm')
    for label, bx in (('Load', x0 + dw / 2 - 130), ('Cancel', x0 + dw / 2 + 20)):
        c.d.rounded_rectangle([bx * S, (y0 + dh - 46) * S, (bx + 110) * S, (y0 + dh - 14) * S], radius=4 * S,
                              fill=(68, 76, 92, 255), outline=(100, 110, 130, 255), width=S)
        c.text((bx + 55, y0 + dh - 30), label, font(15, SANS), fill=(236, 238, 242, 255), anchor='mm')
    c.save('T1.png', 'T1 — the tablet: File > Import Original Save… opens a stock dialog listing the saves',
           'Stock as the menu bar\'s other boxes (Restart\'s): a save picked and Load, then the PC\'s question; the same list as S1.'
           '\nThe menu bar is drawn over the dump, which lacks it.')


for draw in (n1, s1, s1q, s1r, s1n, n1p, s1p, s1qp, s2, t1):
    draw()
