"""Mock-ups of the import screen (docs/import.md, step 8, part 2): what the game shows at its first start, before it
has any of the player's data — choose the game folder, the version check and the decompile with a progress bar, a
refusal. In the lacquer look of our other screens (godot/Strategy/Lacquer.cs).

There is no screen dump under it: at that point nothing of the game can be drawn yet, so the background is our own
wave backdrop (Lacquer.DrawWaveBackdrop), drawn here. From the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_import.py <out dir> < /dev/null
Round 1: A (a lacquer panel in the middle), B (a paper sheet, the steps written on it), C (a title card: the name large,
one line and a thin bar at the foot), each in three states — 1 choose the folder, 2 importing, 3 refused.
Everything is in the 1280x800 screen's pixels (plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os, math
from PIL import Image, ImageDraw, ImageFont

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
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
BAR_GOOD, MINUS, SEAL_TEXT = (70, 110, 50, 255), (222, 110, 90, 255), (246, 230, 206, 255)
LACQUER = (14, 10, 8, 255)

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
SHOJUMARU = REPO + '/godot/fonts/Shojumaru-Regular.ttf'
_fonts = {}

VERSION = 'Kichikuou Rance English Translation v1.0'
FOLDER = r'D:\Games\Kichikuou Rance'
REFUSAL = f'KICHIKUSA.ALD in {FOLDER} is not the file of {VERSION}, the version this game reads.'
HASHES = 'its SHA-256 is 3f2a…91c0, 699d0d5b…c031ba is expected'
STEPS = ['Check the game files', 'Decompile the scenario', 'Ready']


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


class Canvas:
    def __init__(self):
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), LACQUER)
        self.d = ImageDraw.Draw(self.im)
        self.backdrop()

    def rect(self, r, fill=None, outline=None, width=1, radius=0):
        x0, y0, x1, y1 = r
        box = [x0 * S, y0 * S, x1 * S - 1, y1 * S - 1]
        if radius:
            self.d.rounded_rectangle(box, radius * S, fill=fill, outline=outline, width=round(width * S))
        else:
            self.d.rectangle(box, fill=fill, outline=outline, width=round(width * S))

    def overlay(self, draw):
        layer = Image.new('RGBA', self.im.size, (0, 0, 0, 0))
        draw(ImageDraw.Draw(layer))
        self.im = Image.alpha_composite(self.im, layer)
        self.d = ImageDraw.Draw(self.im)

    def text(self, xy, s, f, fill=TEXT, anchor='la'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=GOLD, width=1):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=round(width * S))

    def wrap(self, xy, s, f, width, fill=TEXT, leading=1.45):
        x, y = xy
        words, line = s.split(' '), ''
        size = f.size / S
        for w in words:
            t = (line + ' ' + w).strip()
            if self.width(t, f) > width and line:
                self.text((x, y), line, f, fill)
                y += size * leading
                line = w
            else:
                line = t
        if line:
            self.text((x, y), line, f, fill)
            y += size * leading
        return y

    def backdrop(self):
        """Lacquer.DrawWaveBackdrop: a faint gold seigaiha, darkened towards the edges."""
        R = 34
        wave = GOLD[:3] + (13,)
        layer = Image.new('RGBA', self.im.size, (0, 0, 0, 0))
        dd = ImageDraw.Draw(layer)
        row = -1
        while row * R / 2 < H + R:
            y = row * R / 2
            off = 0 if row % 2 == 0 else R
            x = -2 * R + off
            while x < W + 2 * R:
                dd.ellipse([(x - R) * S, (y - R) * S, (x + R) * S, (y + R) * S], fill=LACQUER)
                for r in (R, R * 0.72, R * 0.44):
                    dd.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], outline=wave, width=S)
                x += 2 * R
            row += 1
        self.im = Image.alpha_composite(self.im, layer)
        edge = Image.new('L', (W, H), 0)
        px = edge.load()
        inset_x, inset_y = 220, 220 * 0.62
        for yy in range(H):
            for xx in range(W):
                t = max(max(0, inset_x - xx, xx - (W - inset_x)) / inset_x, max(0, inset_y - yy, yy - (H - inset_y)) / inset_y)
                px[xx, yy] = int(255 * 0.7 * min(1, t))
        edge = edge.resize((W * S, H * S))
        black = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 255))
        black.putalpha(edge)
        self.im.alpha_composite(black, (0, 0))
        self.d = ImageDraw.Draw(self.im)

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 18, 14, 255))
        self.line((0, H), (W, H), fill=GOLD)
        self.text((16, H + 10), caption, cinzel(20, 700), fill=NAME)
        self.text((16, H + 38), note, font(15), fill=QUIET)
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


# ---------------------------------------------------------------- Lacquer's parts

def panel(c, r):
    """Lacquer.DrawPanel: shadow, lacquer, the double gold frame and its corners."""
    x0, y0, x1, y1 = r

    def shadow(dd):
        for i in range(6, 0, -1):
            dd.rectangle([(x0 - i + 2) * S, (y0 - i + 6) * S, (x1 + i + 2) * S, (y1 + i + 6) * S], fill=(0, 0, 0, 15))
    c.overlay(shadow)
    c.rect(r, fill=PANEL)
    c.rect((x0 + 1, y0 + 1, x1 - 1, y1 - 1), outline=GOLD, width=2)
    c.rect((x0 + 5.5, y0 + 5.5, x1 - 5.5, y1 - 5.5), outline=DIM, width=1)
    for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        c.d.line([((cx + sx * 9) * S, (cy + sy * 2) * S), ((cx + sx * 9) * S, (cy + sy * 9) * S), ((cx + sx * 2) * S, (cy + sy * 9) * S)], fill=GOLD, width=2 * S)
        c.d.polygon([((cx + sx * 9) * S, (cy + sy * 14) * S), ((cx + sx * 14) * S, (cy + sy * 9) * S), ((cx + sx * 9) * S, (cy + sy * 4) * S), ((cx + sx * 4) * S, (cy + sy * 9) * S)], fill=GOLD)


def row_box(c, r, hover=False):
    c.rect(r, fill=HOVER if hover else ROW, outline=HOVER_LINE if hover else DIM, width=2 if hover else 1)


def red_button(c, r, label, hover=False, size=22):
    """Lacquer.DrawRedButton."""
    x0, y0, x1, y1 = r
    top = (150, 40, 30) if hover else (128, 32, 24)
    bottom = (96, 20, 14) if hover else (80, 16, 12)
    for yy in range(int(y0 * S), int(y1 * S)):
        t = (yy - y0 * S) / ((y1 - y0) * S)
        col = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        c.d.line([(x0 * S, yy), (x1 * S - 1, yy)], fill=col)
    c.rect(r, outline=HOVER_LINE if hover else GOLD, width=2)
    c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(150, 96, 52, 255), width=1)
    c.text(((x0 + x1) / 2, (y0 + y1) / 2), label, cinzel(size, 700), fill=SEAL_TEXT, anchor='mm')


def plain_button(c, r, label, hover=False, size=17):
    row_box(c, r, hover)
    c.text(((r[0] + r[2]) / 2, (r[1] + r[3]) / 2), label, font(size), fill=TEXT if hover else VALUE, anchor='mm')


def divider(c, x0, x1, y):
    c.line((x0, y), (x1, y), fill=DIM)
    cx = (x0 + x1) / 2
    c.d.polygon([(cx * S, (y - 4) * S), ((cx + 4) * S, y * S), (cx * S, (y + 4) * S), ((cx - 4) * S, y * S)], fill=GOLD)


def bar(c, r, fraction, fill=GOLD, back=ROW, edge=DIM):
    x0, y0, x1, y1 = r
    c.rect(r, fill=back, outline=edge, width=1)
    c.rect((x0 + 2, y0 + 2, x0 + 2 + (x1 - x0 - 4) * fraction, y1 - 2), fill=fill)


def folder_icon(c, x, y, colour=GOLD):
    c.d.polygon([(x * S, (y + 3) * S), ((x + 7) * S, (y + 3) * S), ((x + 9) * S, (y + 6) * S), ((x + 20) * S, (y + 6) * S),
                 ((x + 20) * S, (y + 18) * S), (x * S, (y + 18) * S)], outline=colour, width=2 * S)


def check(c, x, y, colour):
    c.d.line([((x - 6) * S, y * S), ((x - 2) * S, (y + 5) * S), ((x + 7) * S, (y - 6) * S)], fill=colour, width=3 * S)


# ---------------------------------------------------------------- A: a lacquer panel

def a_frame(c, subtitle):
    r = (290, 150, 990, 650)
    panel(c, r)
    c.text((640, 214), 'Kichikuou Rance', brush(46), fill=NAME, anchor='mm')
    c.text((640, 262), subtitle, cinzel(17, 600), fill=SUB, anchor='mm')
    divider(c, 340, 940, 292)
    return r


def a_folder_row(c, y, path, hover=False, off=False):
    c.text((340, y), 'GAME FOLDER', cinzel(12, 700), fill=LABEL)
    row = (340, y + 20, 940, y + 62)
    row_box(c, row, hover)
    folder_icon(c, 354, y + 31, GOLD if not off else OFF_TEXT)
    c.text((388, y + 41), path or 'not chosen yet', font(17), fill=(VALUE if path else OFF_TEXT) if not off else OFF_TEXT, anchor='lm')
    if not off:
        c.text((926, y + 41), 'Choose…', font(15), fill=HOVER_LINE if hover else QUIET, anchor='rm')


def a1():
    c = Canvas()
    a_frame(c, 'First start — import your game')
    c.wrap((340, 316), f'This port plays your own copy of {VERSION}. Choose its folder: the one with '
                       'system39.exe, KICHIKUSA.ALD and System39.ain. Its files are checked, then the scenario is '
                       'read once into the game\'s own folder; the pictures, sounds and music stay where they are.',
           font(16), 600, fill=TEXT)
    a_folder_row(c, 436, FOLDER, hover=True)
    red_button(c, (520, 560, 760, 612), 'Import')
    c.save('import-A1-choose.png', 'A1 — a lacquer panel: choose the folder',
           'The folder row opens the system\'s folder dialog (the path shown once chosen); Import starts the check and the decompile.')


def a_steps(c, y, active, fraction, failed=False):
    for i, step in enumerate(STEPS):
        if failed and i > active:
            break
        yy = y + i * 40
        done = i < active
        cur = i == active
        colour = NAME if cur else VALUE if done else OFF_TEXT
        if done:
            check(c, 356, yy + 10, BAR_GOOD)
        elif cur and failed:
            c.line((350, yy + 4), (362, yy + 16), fill=MINUS, width=3)
            c.line((362, yy + 4), (350, yy + 16), fill=MINUS, width=3)
        elif cur:
            c.d.ellipse([(350) * S, (yy + 4) * S, (362) * S, (yy + 16) * S], fill=GOLD)
        else:
            c.d.ellipse([(350) * S, (yy + 4) * S, (362) * S, (yy + 16) * S], outline=OFF_LINE, width=2 * S)
        c.text((380, yy + 10), step, font(17), fill=MINUS if cur and failed else colour, anchor='lm')
        if cur and not failed and fraction is not None:
            c.text((940, yy + 10), f'{round(fraction * 100)}%', font(16), fill=NAME, anchor='rm')


def a2():
    c = Canvas()
    a_frame(c, 'Importing your game')
    a_folder_row(c, 316, FOLDER, off=True)
    a_steps(c, 410, 1, 0.64)
    bar(c, (340, 548, 940, 566), 0.64)
    c.text((640, 600), 'A second or so; it is done once for this folder.', font(14), fill=QUIET, anchor='mm')
    c.save('import-A2-progress.png', 'A2 — a lacquer panel: importing',
           'The three steps with the current one lit, its percentage and the bar; nothing to click. Then the title opens.')


def a3():
    c = Canvas()
    a_frame(c, 'This folder cannot be read')
    a_folder_row(c, 316, FOLDER, hover=False)
    a_steps(c, 410, 0, None, failed=True)
    box = (340, 456, 940, 548)
    c.rect(box, fill=(60, 20, 14, 255), outline=VERMILION, width=1)
    y = c.wrap((356, 468), REFUSAL, font(15), 568, fill=TEXT)
    c.text((356, y + 2), HASHES, font(12), fill=QUIET)
    red_button(c, (500, 572, 780, 620), 'Choose another folder', hover=True, size=18)
    c.save('import-A3-refused.png', 'A3 — a lacquer panel: refused',
           'The check\'s own message (the file, the version, both hashes small) in a vermilion box; the button opens the folder dialog again.')


# ---------------------------------------------------------------- B: a paper sheet

def b_sheet(c, heading):
    r = (250, 110, 1030, 690)
    panel(c, r)
    paper = (276, 136, 1004, 664)
    c.rect(paper, fill=PAPER, outline=PAPER_HEAD, width=1)
    c.line((paper[0] + 24, 240), (paper[2] - 24, 240), fill=PAPER_RULE, width=2)
    c.text((640, 182), heading, brush(34), fill=HEAD_INK, anchor='mm')
    c.text((640, 222), VERSION, cinzel(15, 600), fill=INK_QUIET, anchor='mm')
    return paper


def b_numbered(c, y, i, title, body, state):
    marks = '一二三'
    colour = {'done': INK_QUIET, 'now': INK, 'next': INK_GREY, 'failed': VERMILION_INK}[state]
    c.d.ellipse([(316) * S, (y - 2) * S, (346) * S, (y + 28) * S], fill=VERMILION if state in ('now', 'failed') else None,
                outline=VERMILION_INK if state != 'next' else INK_GREY, width=2 * S)
    c.text((331, y + 13), marks[i], font(16), fill=SEAL_TEXT if state in ('now', 'failed') else colour, anchor='mm')
    c.text((364, y + 13), title, font(19), fill=colour, anchor='lm')
    if state == 'done':
        check(c, 960, y + 12, BAR_GOOD)
    if body:
        return c.wrap((364, y + 38), body, font(15), 600, fill=INK_QUIET if state != 'failed' else VERMILION_INK)
    return y + 40


def b1():
    c = Canvas()
    b_sheet(c, 'Import your game')
    y = b_numbered(c, 262, 0, 'Choose the game folder',
                   'The folder of your copy, with system39.exe, KICHIKUSA.ALD and System39.ain in it.', 'now')
    c.rect((364, y + 4, 964, y + 44), fill=(236, 224, 194, 255), outline=INK_GREY, width=1)
    folder_icon(c, 376, y + 14, INK_QUIET)
    c.text((408, y + 24), FOLDER, font(17), fill=INK, anchor='lm')
    c.text((952, y + 24), 'Choose…', font(15), fill=VERMILION_INK, anchor='rm')
    y = b_numbered(c, y + 70, 1, 'The files are checked and the scenario read', 'Once for this folder, a second or so.', 'next')
    b_numbered(c, y + 14, 2, 'Play', None, 'next')
    red_button(c, (540, 588, 740, 636), 'Import')
    c.save('import-B1-choose.png', 'B1 — a paper sheet: choose the folder',
           'The steps written as a numbered list on the ledger\'s paper (一 二 三, the current one sealed in red); the path field opens the folder dialog.')


def b2():
    c = Canvas()
    b_sheet(c, 'Importing your game')
    y = b_numbered(c, 262, 0, 'Game folder', FOLDER, 'done')
    y = b_numbered(c, y + 14, 1, 'Decompiling the scenario', None, 'now')
    # a brush stroke that fills
    x0, x1, yy = 364, 964, y + 6
    c.rect((x0, yy, x1, yy + 22), fill=(236, 224, 194, 255), outline=INK_GREY, width=1)
    c.rect((x0 + 3, yy + 3, x0 + 3 + (x1 - x0 - 6) * 0.64, yy + 19), fill=VERMILION_INK)
    c.text((x1, yy + 40), '64%', font(16), fill=INK, anchor='rm')
    b_numbered(c, yy + 64, 2, 'Play', None, 'next')
    c.save('import-B2-progress.png', 'B2 — a paper sheet: importing',
           'The first step ticked with its folder, the current one sealed, a vermilion bar under it; then the title opens.')


def b3():
    c = Canvas()
    b_sheet(c, 'This folder cannot be read')
    y = b_numbered(c, 262, 0, 'Game folder: ' + FOLDER, REFUSAL, 'failed')
    c.text((364, y + 2), HASHES, font(12), fill=INK_GREY)
    # a stamp
    cx, cy = 880, 520
    c.d.ellipse([(cx - 62) * S, (cy - 62) * S, (cx + 62) * S, (cy + 62) * S], outline=VERMILION + (0,) if False else VERMILION, width=4 * S)
    c.text((cx, cy - 8), 'NOT', cinzel(22, 800), fill=VERMILION, anchor='mm')
    c.text((cx, cy + 20), 'v1.0', cinzel(18, 800), fill=VERMILION, anchor='mm')
    red_button(c, (500, 588, 780, 636), 'Choose another folder', size=18)
    c.save('import-B3-refused.png', 'B3 — a paper sheet: refused',
           'The check\'s message under the first step in vermilion ink, a stamp beside it; the button opens the folder dialog again.')


# ---------------------------------------------------------------- C: a title card

def c_card(c):
    c.text((640, 300), 'Kichikuou Rance', brush(84), fill=NAME, anchor='mm')
    c.text((640, 372), 'the faithful port', cinzel(20, 500), fill=SUB, anchor='mm')
    divider(c, 470, 810, 410)


def c1():
    c = Canvas()
    c_card(c)
    c.text((640, 470), f'To start, choose the folder of your copy of {VERSION}.', font(17), fill=TEXT, anchor='mm')
    c.text((640, 498), 'The one with system39.exe, KICHIKUSA.ALD and System39.ain.', font(15), fill=QUIET, anchor='mm')
    red_button(c, (500, 552, 780, 604), 'Choose the game folder', hover=True, size=19)
    c.text((640, 760), 'Read once, a second or so. Nothing of the game is in this program.', font(13), fill=LABEL, anchor='mm')
    c.save('import-C1-choose.png', 'C1 — a title card: choose the folder',
           'The name large, one line, one button that opens the folder dialog; the import starts as soon as a folder is chosen.')


def c2():
    c = Canvas()
    c_card(c)
    c.text((640, 470), 'Decompiling the scenario', cinzel(19, 600), fill=NAME, anchor='mm')
    c.text((640, 500), FOLDER, font(15), fill=QUIET, anchor='mm')
    bar(c, (0, 784, W, 800), 0.64, back=(20, 15, 11, 255), edge=(20, 15, 11, 255))
    c.text((1264, 770), '64%', font(14), fill=NAME, anchor='rs')
    c.save('import-C2-progress.png', 'C2 — a title card: importing',
           'The stage under the name, the folder below it, a thin gold bar along the screen\'s foot; then the title opens.')


def c3():
    c = Canvas()
    c_card(c)
    y = 458
    for line in (f'KICHIKUSA.ALD in {FOLDER} is not the file of', f'{VERSION}, the version this game reads.'):
        c.text((640, y), line, font(17), fill=MINUS, anchor='mm')
        y += 28
    c.text((640, y + 2), HASHES, font(12), fill=LABEL, anchor='mm')
    red_button(c, (500, 552, 780, 604), 'Choose another folder', hover=True, size=19)
    c.save('import-C3-refused.png', 'C3 — a title card: refused',
           'The check\'s message in red under the name, the hashes small; the same button opens the folder dialog again.')


for f in (a1, a2, a3, b1, b2, b3, c1, c2, c3):
    f()
print('wrote', len(os.listdir(OUT)), 'images to', OUT)
