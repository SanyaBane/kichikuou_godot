"""Mock-ups of our own screen for an enemy attack on a province of ours (FIGT_B.ADV:5-64; docs/ui/enemy-attack.md),
in the lacquer look of our other screens (godot/Strategy/Lacquer; docs/ui/pickers.md, docs/ui/save-load.md).

The case is the rebels' attack on Ys (5) the week-18 save meets after Turn END, scouted by Kanami:
  <Godot console exe> --path godot -- --load --auto --quit --seed 2 --saves user://saves-scout
      --slots-from user://saves-week18 --turn-end --scout --until deployment --shots <dumps dir>
(a window, without --fast: its "00014-key" dump is the scout report). Then, from the repository's root (Pillow; the
fonts of godot/fonts):
  python -I tools/ui/mock_enemy_attack.py <dumps dir> <cg_png dir> <out dir> < /dev/null
The map panned to Ys is put together from the run's dumps (the Rebel Army's banner, the announcement, Rance's line
at the intercept, the first map wait), the scout report's cards and faces are cut from its dump. The figures are the
save's: Ys has Defense 2 (Walls +4%), Battle Area 330 and Economy 8 (800K GOLD of the week's revenue); only Kanami is
a ninja in service and free. Round 1: A (a card over the map), B (a screen of its own: Ys against the army), C (the
original's scene with our menu); each before the scouting and after it, A with the Abandon question.
The images hold the player's CGs: keep <out dir> outside the repository. Everything is in the 1280x800 screen's pixels
(plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

DUMPS, CGS, OUT = sys.argv[1:4]
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
VERMILION = (170, 40, 28, 255)
ALARM = (214, 58, 40, 255)

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


def bold(size):
    """The emphasis of a line: BIZ UDGothic has no bold among our fonts, so the colour carries it."""
    return font(size)


def cinzel(size, weight=600):
    return font(size, CINZEL, weight)


def brush(size):
    return font(size, SHOJUMARU)


# ---------------------------------------------------------------- the case (the save and the scout report)

ARMY, PROVINCE, DATE = 'Leazas Rebel Army', 'Ys', 'LP 3 · Month 5 · Week 2'
YS = (893, 404)                                   # Ys's town on the map panned to it
FIGURES = [('Walls', '+4%', 'Defense 2: the enemy hits 4% less'),
           ('City battle limit', '330', 'troops a unit fights with'),
           ('Plains battle limit', '5,000', 'troops a unit fights with'),
           ('Revenue', '800K GOLD', 'a week')]
# The scout report (00014-key): place, name, troop type, troops; the card and the face square in the dump's content.
ENEMY = [(1, 'Ex', 'Leazas Knights', 240, (380, 190, 662, 340), (536, 206, 654, 324)),
         (2, 'Sakanaku', 'Leazas Soldiers', 970, (380, 350, 662, 500), (536, 366, 654, 484)),
         (3, 'Dodge', 'Leazas Soldiers', 486, (680, 190, 962, 340), (836, 206, 954, 324)),
         (4, 'Jibul', 'Leazas Soldiers', 400, (680, 350, 962, 500), (836, 366, 954, 484))]
TOTAL = '2,096'
MARIS_LINE = ['King Rance, if we don\'t act,', 'Ys will be occupied.', 'What should we do?']   # FIGT_B.ADV:35-45


# ---------------------------------------------------------------- drawing

class Canvas:
    def __init__(self, back):
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (14, 10, 8, 255))
        self.im.alpha_composite(back.convert('RGBA').resize((W * S, H * S), Image.LANCZOS), (0, 0))
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

    def blur(self, r, radius=6):
        x0, y0, x1, y1 = (round(v * S) for v in r)
        part = self.im.crop((x0, y0, x1, y1)).filter(ImageFilter.GaussianBlur(radius * S))
        self.im.paste(part, (x0, y0))
        self.d = ImageDraw.Draw(self.im)

    def text(self, xy, s, f, fill=TEXT, anchor='la'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=DIM, width=1):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=round(width * S))

    def paste(self, img, xy, size=None):
        img = img.convert('RGBA')
        if size:
            img = img.resize((round(size[0] * S), round(size[1] * S)), Image.LANCZOS)
        else:
            img = img.resize((img.size[0] * S, img.size[1] * S), Image.LANCZOS)
        self.im.alpha_composite(img, (round(xy[0] * S), round(xy[1] * S)))
        self.d = ImageDraw.Draw(self.im)

    def poly(self, pts, fill=None, outline=None):
        self.d.polygon([(x * S, y * S) for x, y in pts], fill=fill, outline=outline)

    def ellipse(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.ellipse([x0 * S, y0 * S, x1 * S, y1 * S], fill=fill, outline=outline, width=round(width * S))

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


def content(name):
    """A dump without the window's menu bar."""
    im = Image.open(os.path.join(DUMPS, name)).convert('RGBA')
    return im.crop((0, im.size[1] - 800, 1280, im.size[1]))


PHASE, ANNOUNCE, INTERCEPT = content('00009-key.png'), content('00010-key.png'), content('00016-key.png')
FIRST_MAP, SCOUT = content('00004-map-markers.png'), content('00014-key.png')


def ys_map():
    """The map panned to Ys, as the announcement shows it, without the picture and the window: the banner's dump is the
    same map 130 px lower, the intercept's the same pan, the first map wait's 32 px higher (its plates are left out)."""
    out = ANNOUNCE.copy()
    out.paste(PHASE.crop((0, 191 - 130, 1280, 800 - 130)), (0, 191))
    out.paste(INTERCEPT.crop((240, 666, 897, 800)), (240, 666))
    out.paste(FIRST_MAP.crop((897, 666 + 32, 1032, 800)), (897, 666))
    out.paste(out.crop((897, 736, 1032, 768)), (897, 768))
    return out


def scene_back():
    """C's scene: the intercept's dump (the portraits and the picture) without its message window."""
    out = INTERCEPT.copy()
    out.paste(MAP.crop((383, 0, 1280, 264)), (383, 0))           # the window and its tail
    out.paste(ANNOUNCE.crop((383, 192, 928, 264)), (383, 192))   # Ys's picture under it
    return out


MAP = ys_map()
SCENE = scene_back()


def face(n):
    return SCOUT.crop(ENEMY[n][5])


def portrait_face(cg_n, box):
    return cg(cg_n).crop(box)


RANCE_FACE, MARIS_FACE, KANAMI_FACE = (portrait_face(1, (44, 14, 144, 114)), portrait_face(6, (48, 14, 140, 106)),
                                       portrait_face(9, (50, 22, 140, 112)))


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


def heading(c, x, y, s, x1=None):
    c.text((x, y), s, cinzel(13, 700), fill=SUB)
    if x1:
        c.line((x + c.width(s, cinzel(13, 700)) + 10, y + 9), (x1, y + 9), fill=DIM)


def row_box(c, r, state='normal'):
    if state == 'off':
        c.rect(r, fill=OFF, outline=OFF_LINE, width=1)
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
    c.rect(r, outline=HOVER_LINE if hover else GOLD, width=2)
    c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(150, 96, 52, 255), width=1)
    cx = (x0 + x1) / 2
    if sub:
        c.text((cx, y0 + (y1 - y0) * 0.40), title, brush(22), fill=NAME, anchor='mm')
        c.text((cx, y0 + (y1 - y0) * 0.76), sub, font(13), fill=(236, 206, 160, 255), anchor='mm')
    else:
        c.text((cx, (y0 + y1) / 2), title, brush(18), fill=NAME, anchor='mm')


def button(c, r, title, sub=None, state='normal'):
    row_box(c, r, state)
    x0, y0, x1, y1 = r
    colour = OFF_TEXT if state == 'off' else (NAME if state == 'hover' else TEXT)
    cx = (x0 + x1) / 2
    if sub:
        c.text((cx, y0 + (y1 - y0) * 0.40), title, cinzel(17, 700), fill=colour, anchor='mm')
        c.text((cx, y0 + (y1 - y0) * 0.76), sub, font(13), fill=OFF_TEXT if state == 'off' else QUIET, anchor='mm')
    else:
        c.text((cx, (y0 + y1) / 2), title, cinzel(17, 700), fill=colour, anchor='mm')


def framed(c, img, r, line=GOLD):
    x0, y0, x1, y1 = r
    c.paste(img, (x0, y0), (x1 - x0, y1 - y0))
    c.rect(r, outline=line, width=1)


def ninja_chip(c, x, y, used=False, hover=False):
    """A ninja to send: the face, the name, free or used this week (the original lists only the free ones)."""
    r = (x, y, x + 190, y + 52)
    row_box(c, r, 'off' if used else ('hover' if hover else 'normal'))
    img = KANAMI_FACE if not used else Image.blend(KANAMI_FACE, Image.new('RGBA', KANAMI_FACE.size, (40, 35, 30, 255)), 0.6)
    framed(c, img, (x + 6, y + 6, x + 46, y + 46), DIM)
    c.text((x + 56, y + 9), 'Kanami', cinzel(16, 700), fill=OFF_TEXT if used else (NAME if hover else TEXT))
    c.text((x + 56, y + 30), 'scouted this week' if used else 'free this week', font(13), fill=OFF_TEXT if used else QUIET)
    return r


def pointer(c, x, y):
    p = [(x, y), (x, y + 22), (x + 6, y + 17), (x + 10, y + 26), (x + 14, y + 24), (x + 10, y + 15), (x + 17, y + 15)]
    c.poly(p, fill=(255, 255, 255, 255), outline=(0, 0, 0, 255))


def callout(c, target, lines, answers, above=True, w=430):
    """Our question at the button, as the Delete of the Save/Load screen and the Subordinates screen's orders."""
    x0, y0, x1, y1 = target
    h = 40 + 24 * len(lines) + 44
    cx = min(max((x0 + x1) / 2 - w / 2, 24), W - 24 - w)
    top = y0 - 14 - h if above else y1 + 14
    r = (cx, top, cx + w, top + h)
    lq_panel(c, r)
    tip = (x0 + x1) / 2
    if above:
        c.poly([(tip - 10, r[3] - 2), (tip + 10, r[3] - 2), (tip, r[3] + 12)], fill=GOLD)
    else:
        c.poly([(tip - 10, top + 2), (tip + 10, top + 2), (tip, top - 12)], fill=GOLD)
    for i, line in enumerate(lines):
        c.text((cx + 22, top + 22 + 24 * i), line, bold(16) if i == 0 else font(16), fill=TEXT if i == 0 else QUIET)
    by = top + 30 + 24 * len(lines)
    red_button(c, (cx + w - 252, by, cx + w - 122, by + 32), answers[0])
    button(c, (cx + w - 112, by, cx + w - 22, by + 32), answers[1])


def ys_mark(c, plate=True):
    """Ys on the map: an alarm ring round its town and its plate as our map markers draw it."""
    x, y = YS
    for rad, a in ((46, 70), (36, 140)):
        c.ellipse((x - rad, y - rad * 0.62, x + rad, y + rad * 0.62), outline=ALARM[:3] + (a,), width=3)
    if plate:
        f = font(15)
        w = c.width(PROVINCE, f) + 22
        r = (x - w / 2, y + 28, x + w / 2, y + 54)
        c.rect(r, fill=(150, 34, 24, 235), outline=GOLD, width=1, radius=4)
        c.text((x, y + 41), PROVINCE, f, fill=TEXT, anchor='mm')


def flag(c, x, y, k=2):
    c.paste(cg(821), (x, y), (24 * k, 19 * k))


def enemy_unknown(c, r, place):
    """A place of the attacking army before the scouting: nothing but the place."""
    x0, y0, x1, y1 = r
    c.rect(r, fill=(30, 26, 24, 255), outline=OFF_LINE, width=1)
    c.text((x0 + 10, y0 + 8), str(place), cinzel(14, 700), fill=OFF_TEXT)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2 + 4
    c.text((cx, cy), '?', brush(30), fill=(84, 74, 62, 255), anchor='mm')


def enemy_row(c, r, n):
    """A scouted commander in a ledger row: face, place and name, troop type, troops — only what the report says."""
    place, name, kind, troops = ENEMY[n][:4]
    x0, y0, x1, y1 = r
    c.rect(r, fill=ROW, outline=DIM, width=1)
    framed(c, face(n), (x0 + 6, y0 + 6, x0 + 6 + (y1 - y0 - 12), y1 - 6), DIM)
    tx = x0 + (y1 - y0) + 4
    c.text((tx, y0 + 8), f'{place}  ', cinzel(14, 700), fill=SUB)
    c.text((tx + 18, y0 + 7), name, bold(16), fill=TEXT)
    c.text((tx, y0 + 30), kind, font(13), fill=QUIET)
    c.text((x1 - 10, (y0 + y1) / 2), f'{troops:,}', cinzel(20, 700), fill=VALUE, anchor='rm')


def figures(c, x, y, x1, pitch=34, notes=True):
    for i, (label, value, note) in enumerate(FIGURES):
        ry = y + i * pitch
        c.text((x, ry), label, font(15), fill=LABEL)
        c.text((x1, ry), value, cinzel(17, 700), fill=VALUE, anchor='ra')
        if notes:
            c.text((x, ry + 17), note, font(11), fill=OFF_TEXT)
        if i < len(FIGURES) - 1:
            c.line((x, ry + pitch - 5), (x1, ry + pitch - 5), fill=(60, 46, 30, 255))


def quote(c, x, y, w, img, lines, who):
    """A line of the scene's, small: the speaker's face and the words (the script's text)."""
    framed(c, img, (x, y, x + 56, y + 56), DIM)
    c.text((x + 68, y), who, cinzel(13, 700), fill=SUB)
    for i, line in enumerate(lines):
        c.text((x + 68, y + 18 + 19 * i), line, font(15), fill=TEXT)


# ---------------------------------------------------------------- A: the card over the map

A_CARD = (64, 92, 776, 708)


def mock_a(state):
    c = Canvas(MAP)
    c.overlay((0, 0, W, H), (0, 0, 0, 70))
    ys_mark(c)
    x0, y0, x1, y1 = A_CARD
    c.line((x1, 300), (YS[0] - 48, YS[1]), fill=GOLD, width=1)          # the card points at Ys
    lq_panel(c, A_CARD)
    flag(c, x0 + 22, y0 + 18)
    c.text((x0 + 84, y0 + 36), 'Enemy Attack', brush(26), fill=NAME, anchor='lm')
    c.text((x1 - 22, y0 + 38), DATE, cinzel(13, 600), fill=SUB, anchor='rm')
    divider(c, x0 + 16, x1 - 16, y0 + 66)
    c.text((x0 + 22, y0 + 80), 'The ', font(19), fill=TEXT)
    t = x0 + 22 + c.width('The ', font(19))
    c.text((t, y0 + 80), ARMY, bold(19), fill=NAME)
    t += c.width(ARMY, bold(19))
    c.text((t, y0 + 80), ' appeared around ', font(19), fill=TEXT)
    t += c.width(' appeared around ', font(19))
    c.text((t, y0 + 80), PROVINCE + '.', bold(19), fill=(240, 120, 96, 255))

    # Ys: its picture (Prov_BgCg) and what it holds.
    lx, top = x0 + 22, y0 + 120
    heading(c, lx, top, 'YS · OURS', lx + 296)
    framed(c, cg(1003), (lx, top + 24, lx + 296, top + 24 + 197))
    figures(c, lx, top + 238, lx + 296, pitch=38, notes=False)

    # The enemy: unknown until a ninja scouts it.
    rx, rx1 = x0 + 346, x1 - 22
    heading(c, rx, top, 'THE ENEMY', rx1)
    if state == 'scouted':
        for n in range(4):
            enemy_row(c, (rx, top + 24 + n * 52, rx1, top + 72 + n * 52), n)
        c.text((rx, top + 238), 'Commanders 4', font(15), fill=LABEL)
        c.text((rx1, top + 236), 'Total troops ' + TOTAL, cinzel(17, 700), fill=VALUE, anchor='ra')
        c.text((rx, top + 262), 'front row 1-2 · back row 3-4 · Kanami\'s report', font(12), fill=OFF_TEXT)
    else:
        for n in range(4):
            col, row = divmod(n, 2)
            enemy_unknown(c, (rx + col * 172, top + 24 + row * 100, rx + col * 172 + 164, top + 116 + row * 100), n + 1)
        c.text((rx, top + 230), 'front row', font(12), fill=OFF_TEXT)
        c.text((rx + 172, top + 230), 'back row', font(12), fill=OFF_TEXT)
        c.text((rx, top + 256), 'Commanders and troops: send a ninja to learn them.', font(13), fill=QUIET)

    # Maris's line (FIGT_B.ADV:35-45) in place of her two windows.
    quote(c, rx, top + 296, rx1 - rx, MARIS_FACE, MARIS_LINE, 'MARIS')

    # The answers: FIGT_B's menu, codes 1-3.
    by = y1 - 96
    divider(c, x0 + 16, x1 - 16, by - 18)
    intercept = (x0 + 22, by, x0 + 262, by + 70)
    red_button(c, intercept, 'Intercept', 'choose the units', hover=state == 'hover')
    heading(c, x0 + 284, by - 4, 'NINJA SCOUT')
    chip = ninja_chip(c, x0 + 284, by + 18, used=state == 'scouted')
    abandon = (x1 - 172, by + 18, x1 - 22, by + 70)
    button(c, abandon, 'Abandon City', state='hover' if state == 'abandon' else 'normal')
    if state == 'hover':
        pointer(c, intercept[0] + 200, by + 22)
    if state == 'abandon':
        callout(c, abandon, ['Abandon Ys to the Leazas Rebel Army?', 'No battle: the city and its 800K GOLD',
                             'of revenue a week go to the enemy.'], ('Abandon', 'Keep'), w=400)
        pointer(c, abandon[0] + 90, by + 46)
    return c


# ---------------------------------------------------------------- B: a screen of its own

def mock_b(state):
    c = Canvas(MAP)
    c.blur((0, 0, W, H), 5)
    c.overlay((0, 0, W, H), (8, 6, 4, 170))
    # The title band.
    lq_panel(c, (32, 24, 1248, 96))
    flag(c, 54, 41)
    c.text((116, 60), 'Enemy Attack', brush(28), fill=NAME, anchor='lm')
    t = 116 + c.width('Enemy Attack', brush(28)) + 24
    c.text((t, 62), f'The {ARMY} appeared around {PROVINCE}.', font(19), fill=TEXT, anchor='lm')
    c.text((1226, 62), DATE, cinzel(13, 600), fill=SUB, anchor='rm')

    # Ys: the defended city.
    left = (32, 116, 504, 612)
    lq_panel(c, left)
    heading(c, 54, 134, 'DEFENDING · YS', 482)
    framed(c, cg(1003), (54, 158, 482, 158 + 285))
    figures(c, 54, 456, 482, pitch=36, notes=False)

    c.text((552, 364), 'VS', brush(34), fill=GOLD, anchor='mm')

    # The army: its four places, as the scout report's cards once a ninja has seen them.
    right = (600, 116, 1248, 612)
    lq_panel(c, right)
    heading(c, 622, 134, 'ATTACKING · ' + ARMY.upper(), 1226)
    for n in range(4):
        col, row = divmod(n, 2)
        x, y = 632 + col * 300, 170 + row * 166
        if state == 'scouted':
            c.paste(SCOUT.crop(ENEMY[n][4]), (x, y), (282, 150))
        else:
            enemy_unknown(c, (x, y, x + 282, y + 150), n + 1)
    c.text((632, 504), 'front row', font(13), fill=OFF_TEXT)
    c.text((932, 504), 'back row', font(13), fill=OFF_TEXT)
    if state == 'scouted':
        c.text((632, 540), 'Commanders 4', font(17), fill=LABEL)
        c.text((1216, 538), 'Total troops ' + TOTAL, cinzel(20, 700), fill=VALUE, anchor='ra')
        c.text((632, 566), 'Kanami\'s report, as the scout report shows it', font(13), fill=OFF_TEXT)
    else:
        c.text((632, 540), 'Commanders ??', font(17), fill=OFF_TEXT)
        c.text((1216, 538), 'Total troops ??', cinzel(20, 700), fill=OFF_TEXT, anchor='ra')
        c.text((632, 566), 'Send a ninja to learn them.', font(13), fill=QUIET)

    # The foot: Maris asks, the three answers.
    foot = (32, 632, 1248, 780)
    lq_panel(c, foot)
    framed(c, cg(6).crop((0, 0, 192, 190)), (50, 646, 50 + 122, 646 + 120))
    c.text((188, 650), 'MARIS', cinzel(13, 700), fill=SUB)
    for i, line in enumerate(MARIS_LINE):
        c.text((188, 672 + 24 * i), line, font(18), fill=TEXT)
    intercept = (548, 660, 800, 752)
    red_button(c, intercept, 'Intercept', 'choose the units', hover=state == 'hover')
    c.text((822, 652), 'NINJA SCOUT', cinzel(13, 700), fill=SUB)
    ninja_chip(c, 822, 676, used=state == 'scouted')
    button(c, (1030, 676, 1226, 728), 'Abandon City')
    if state == 'hover':
        pointer(c, 760, 676)
    return c


# ---------------------------------------------------------------- C: the original's scene, our menu

MENU_BOX = (488, 86, 820, 227)          # the selection box FIGT_B's menu opens at (GamePresenter.MenuBoxArea)


def menu_panel(c, items, hover=0, marks=None):
    """SceneMenuView: the panel at the box's corner, rows 36 high at a pitch of 44, the text at 30."""
    pad, pitch, rh = 14, 44, 36
    w = max(MENU_BOX[2] - MENU_BOX[0] + 2 * pad, max(c.width(i, font(30)) for i in items) + 2 * pad + 36 + (34 if marks else 0))
    x, y = MENU_BOX[0] - pad, MENU_BOX[1] - pad
    h = 2 * pad + len(items) * pitch - (pitch - rh)
    lq_panel(c, (x, y, x + w, y + h))
    for i, item in enumerate(items):
        r = (x + pad, y + pad + i * pitch, x + w - pad, y + pad + i * pitch + rh)
        row_box(c, r, 'hover' if i == hover else 'normal')
        tx = r[0] + 18
        if marks and marks[i] is not None:
            framed(c, marks[i], (r[0] + 6, r[1] + 3, r[0] + 36, r[1] + 33), DIM)
            tx = r[0] + 46
        c.text((tx, (r[1] + r[3]) / 2), item, font(30), fill=NAME if i == hover else TEXT, anchor='lm')
    return x, y, w, h


def mock_c(state):
    c = Canvas(SCENE)
    if state == 'ninja':
        x, y, w, h = menu_panel(c, ['Kanami', 'None'], hover=0, marks=[KANAMI_FACE, None])
        pointer(c, x + 160, y + 34)
    else:
        x, y, w, h = menu_panel(c, ['Intercept', 'Ninja Scout', 'Abandon City'], hover=0)
        pointer(c, x + 170, y + 34)
    return c


os.makedirs(OUT, exist_ok=True)
mock_a('hover').save('A1-card.png', 'A1 — the card over the map',
                     'Ys ringed on the map; the announcement and Maris\'s line in one card; the enemy unknown; FIGT_B\'s menu as three answers.')
mock_a('scouted').save('A2-card-scouted.png', 'A2 — after Kanami\'s report',
                       'Back from the scout report (ours already): the card keeps what it said; Kanami used for the week.')
mock_a('abandon').save('A3-card-abandon.png', 'A3 — Abandon City asks first',
                       'Our question at the button (the original abandons at once); Keep returns to the card, as a cancel of the menu.')
mock_b('hover').save('B1-screen.png', 'B1 — a screen of its own: Ys against the army',
                     'The map blurred; Ys left, the army\'s four places right (unknown), Maris and the answers at the foot.')
mock_b('scouted').save('B2-screen-scouted.png', 'B2 — after Kanami\'s report',
                       'The places show the scout report\'s cards; the overlay could be left out, the cards filled in place.')
mock_c('menu').save('C1-scene-menu.png', 'C1 — the original\'s scene with our menu',
                    'The portraits and Ys\'s picture as now, Maris\'s lines in our windows as now; the menu is our SceneMenuView.')
mock_c('ninja').save('C2-scene-ninja.png', 'C2 — the ninja menu, the same way',
                     'After Rance\'s "Yeah, this is a good time to use a ninja." (our window): the free ninjas with their faces, None.')
