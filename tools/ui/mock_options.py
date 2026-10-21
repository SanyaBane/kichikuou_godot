"""Mock-ups of the options window (owner's request 2026-10-21): every item of the menu bar's Option menu — Mouse Movement,
Skip Unseen Text, MsgSkip Speeds Up Fades, Forget Seen Messages, Battle Screen, Text Speed, Game Folder, Fonts, Palette —
in one modal window of our own, in the lacquer look (godot/Strategy/Lacquer.cs), over the game, which waits.

Drawn over a screen dump of the map (the week-18 save's first map wait), dimmed. From the repository's root (Pillow; the
fonts of godot/fonts):
  python -I tools/ui/mock_options.py <map dump.png> <out dir> < /dev/null
Round 1: A (a lacquer panel, the sections as an index on its left), B (one paper sheet with every setting, the battle's
timings on a second sheet), C (a wide panel, the sections as tabs across its top), two states each.
Everything is in the 1280x800 screen's pixels (plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageEnhance

DUMP, OUT = sys.argv[1], sys.argv[2]
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
SEAL_TEXT, FIELD = (246, 230, 206, 255), (236, 224, 194, 255)

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
MS_GOTHIC = r'C:\Windows\Fonts\msgothic.ttc'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
SHOJUMARU = REPO + '/godot/fonts/Shojumaru-Regular.ttf'
_fonts = {}

FOLDER = r'D:\Games\Kichikuou Rance'
PALETTES = [('Navy', (20, 26, 56), (242, 204, 89)), ('Gold', (46, 36, 24), (214, 170, 80)),
            ('Steel', (61, 64, 70), (200, 170, 100)), ('Parchment', (216, 199, 159), (120, 80, 40)),
            ('Crimson', (61, 26, 32), (210, 170, 90))]
# BattleScreenSettings.All: label, value, min, max, unit
BATTLE = [('Order strip: move', 1.0, 0, 3, 's'), ('Order strip: hurry', 0.15, 0, 1, 's'),
          ('Order strip: drop and rise', 0.20, 0.05, 0.45, '%'), ('Strip: fly in', 0.25, 0, 1, 's'),
          ('Armies: slide', 0.2, 0, 3, 's'), ('Spotlight: darkness', 0.5, 0, 1, '%'),
          ('Cards: counters', 0.6, 0, 3, 's'), ('Cards: slide in', 1.0, 0, 3, 's'),
          ('Counters: run', 1.3, 0, 3, 's'), ('Opening: appear', 0.35, 0, 3, 's'),
          ('Opening: leave', 0.5, 0, 3, 's'), ('Results: hold', 1.2, 0, 5, 's')]
SPEED_STEP = 6
SAMPLE = ['I believe that we should first', 'prepare our cities for their', 'attacks, then defend them']


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


def fmt(value, unit):
    return f'{value * 100:.0f} %' if unit == '%' else f'{value:.2f} s'


class Canvas:
    def __init__(self):
        dump = Image.open(DUMP).convert('RGBA').resize((W * S, H * S), Image.LANCZOS)
        dump = ImageEnhance.Brightness(dump).enhance(0.42)
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (0, 0, 0, 255))
        self.im.alpha_composite(dump, (0, 0))
        self.d = ImageDraw.Draw(self.im)
        self.menu_bar()

    def menu_bar(self):
        """GameScreen.MenuBar: the light bar over the picture, Option pressed."""
        self.rect((0, 0, W, 24), fill=(240, 240, 240, 255))
        x = 4
        for item in ('File', 'Screen', 'Option', 'MsgSkip[off]', 'Debug'):
            w = self.width(item, font(14)) + 16
            if item == 'Option':
                self.rect((x, 0, x + w, 24), fill=(204, 224, 247, 255))
            self.text((x + 8, 12), item, font(14), fill=(0, 0, 0, 255), anchor='lm')
            x += w

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

    def circle(self, c, r, fill=None, outline=None, width=1):
        self.d.ellipse([(c[0] - r) * S, (c[1] - r) * S, (c[0] + r) * S, (c[1] + r) * S], fill=fill, outline=outline,
                       width=round(width * S))

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
        c.d.line([((cx + sx * 9) * S, (cy + sy * 2) * S), ((cx + sx * 9) * S, (cy + sy * 9) * S),
                  ((cx + sx * 2) * S, (cy + sy * 9) * S)], fill=GOLD, width=2 * S)
        c.d.polygon([((cx + sx * 9) * S, (cy + sy * 14) * S), ((cx + sx * 14) * S, (cy + sy * 9) * S),
                     ((cx + sx * 9) * S, (cy + sy * 4) * S), ((cx + sx * 4) * S, (cy + sy * 9) * S)], fill=GOLD)


def row_box(c, r, hover=False):
    c.rect(r, fill=HOVER if hover else ROW, outline=HOVER_LINE if hover else DIM, width=2 if hover else 1)


def red_button(c, r, label, hover=False, size=20):
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


def plain_button(c, r, label, hover=False, size=15):
    row_box(c, r, hover)
    c.text(((r[0] + r[2]) / 2, (r[1] + r[3]) / 2), label, font(size), fill=TEXT if hover else VALUE, anchor='mm')


def divider(c, x0, x1, y):
    c.line((x0, y), (x1, y), fill=DIM)
    cx = (x0 + x1) / 2
    c.d.polygon([(cx * S, (y - 4) * S), ((cx + 4) * S, y * S), (cx * S, (y + 4) * S), ((cx - 4) * S, y * S)], fill=GOLD)


def check_box(c, x, y, on, label, hint=None, paper=False):
    """A square with a gold (on paper: vermilion) tick; the label right of it, a hint under it."""
    edge, tick, fg, quiet = (INK_GREY, VERMILION_INK, INK, INK_QUIET) if paper else (DIM, GOLD, VALUE, LABEL)
    c.rect((x, y, x + 20, y + 20), fill=FIELD if paper else ROW, outline=edge, width=1.5)
    if on:
        c.d.line([((x + 4) * S, (y + 10) * S), ((x + 8.5) * S, (y + 15) * S), ((x + 16) * S, (y + 5) * S)], fill=tick, width=3 * S)
    c.text((x + 32, y + 10), label, font(17), fill=fg, anchor='lm')
    if hint:
        c.text((x + 32, y + 30), hint, font(13), fill=quiet, anchor='lm')


def radio(c, x, y, on, label, sample_font=None, paper=False):
    edge, dot, fg = (INK_GREY, VERMILION_INK, INK) if paper else (DIM, GOLD, VALUE)
    c.circle((x + 10, y + 10), 10, fill=FIELD if paper else ROW, outline=edge, width=1.5)
    if on:
        c.circle((x + 10, y + 10), 5, fill=dot)
    c.text((x + 30, y + 10), label, sample_font or font(17), fill=fg, anchor='lm')


def slider(c, x0, x1, y, fraction, paper=False, knob_label=None):
    """A track with the filled part and a knob (TextSpeedView's look)."""
    track, fill, knob = (INK_GREY, VERMILION_INK, VERMILION) if paper else (OFF_LINE, GOLD, NAME)
    c.rect((x0, y - 3, x1, y + 3), fill=FIELD if paper else ROW, outline=track, width=1)
    kx = x0 + (x1 - x0) * fraction
    c.rect((x0 + 1, y - 2, kx, y + 2), fill=fill)
    c.circle((kx, y), 9, fill=knob, outline=(INK if paper else PANEL), width=2)
    if knob_label:
        c.text((kx, y - 16), knob_label, font(13), fill=INK if paper else NAME, anchor='mb')


def swatch(c, x, y, name, back, accent, on, paper=False):
    c.rect((x, y, x + 64, y + 40), fill=back + (255,), outline=(VERMILION if paper else HOVER_LINE) if on else (INK_GREY if paper else DIM),
           width=3 if on else 1)
    c.rect((x + 8, y + 26, x + 56, y + 31), fill=accent + (255,))
    c.text((x + 32, y + 54), name, font(13), fill=(INK if on else INK_QUIET) if paper else (NAME if on else LABEL), anchor='mm')


def sample_window(c, r):
    """The game's 121 as the text speed preview: a dark window with its text half typed."""
    c.rect(r, fill=(16, 20, 40, 235), outline=(200, 200, 220, 255), width=2, radius=6)
    y = r[1] + 16
    typed = 52
    for line in SAMPLE:
        shown = line[:max(0, typed)]
        typed -= len(line)
        c.text((r[0] + 18, y), shown, font(16), fill=(240, 240, 240, 255))
        y += 24


def section_head(c, x, y, title, paper=False, width=None):
    c.text((x, y), title.upper(), cinzel(13, 700), fill=HEAD_INK if paper else LABEL)
    if width:
        c.line((x, y + 22), (x + width, y + 22), fill=PAPER_RULE if paper else OFF_LINE)


# ---------------------------------------------------------------- the pages, shared by A and C

def messages_page(c, x, y, w):
    section_head(c, x, y, 'Text speed', width=w)
    slider(c, x + 70, x + w - 70, y + 66, (SPEED_STEP - 1) / 9, knob_label=str(SPEED_STEP))
    c.text((x, y + 66), 'Slow', font(14), fill=LABEL, anchor='lm')
    c.text((x + w, y + 66), 'Instant', font(14), fill=LABEL, anchor='rm')
    sample_window(c, (x + 70, y + 92, x + w - 70, y + 186))
    y += 216
    section_head(c, x, y, 'Message skip', width=w)
    check_box(c, x, y + 38, False, 'Skip unseen text', 'MsgSkip and Ctrl skip messages not read yet too.')
    check_box(c, x, y + 92, True, 'MsgSkip speeds up fades', 'Fades and waits run fast while skipping.')
    plain_button(c, (x + w - 230, y + 146, x + w, y + 178), 'Forget seen messages…')
    c.text((x, y + 162), '3 412 messages seen so far.', font(14), fill=LABEL, anchor='lm')
    y += 204
    section_head(c, x, y, 'Mouse', width=w)
    check_box(c, x, y + 38, True, 'Mouse movement', 'The pointer moves to the chosen item, as xsystem35 does.')


def battle_page(c, x, y, w, rows=BATTLE):
    section_head(c, x, y, 'Effects', width=w)
    check_box(c, x, y + 36, True, 'Exchange effects', 'Our effects over the exchange, after Sengoku Rance\'s.')
    y += 92
    section_head(c, x, y, 'Timings', width=w)
    y += 36
    col_w = (w - 30) / 2
    for i, (label, value, lo, hi, unit) in enumerate(rows):
        cx = x + (i % 2) * (col_w + 30)
        cy = y + (i // 2) * 44
        c.text((cx, cy + 8), label, font(14), fill=VALUE, anchor='lm')
        slider(c, cx, cx + col_w - 70, cy + 28, (value - lo) / (hi - lo))
        c.text((cx + col_w, cy + 28), fmt(value, unit), font(14), fill=NAME, anchor='rm')
    return y + (len(rows) + 1) // 2 * 44


def strip_preview(c, x, y, w):
    """The order strip's chips (OrderStrip) as the timing's preview."""
    c.rect((x, y, x + w, y + 46), fill=(30, 32, 36, 255), outline=OFF_LINE, width=1)
    names = [('Melfeis', 'E'), ('Leila', 'P'), ('Menad', 'P'), ('Rance', 'P'), ('Barres', 'P'), ('Dodge', 'E')]
    cw = (w - 16 - 5 * 6) / 6
    for i, (name, side) in enumerate(names):
        cx = x + 8 + i * (cw + 6)
        colour = (120, 40, 34, 255) if side == 'E' else (40, 70, 120, 255)
        c.rect((cx, y + 8, cx + cw, y + 38), fill=colour, outline=GOLD if i == 0 else (90, 90, 100, 255), width=2 if i == 0 else 1)
        c.text((cx + cw / 2, y + 23), name, font(13), fill=TEXT, anchor='mm')


def display_page(c, x, y, w):
    section_head(c, x, y, 'Font', width=w)
    ms = font(18, MS_GOTHIC) if os.path.exists(MS_GOTHIC) else font(18)
    radio(c, x, y + 38, False, 'MS Gothic — 鬼畜王ランス, Rance', ms)
    radio(c, x, y + 74, True, 'BIZ UDGothic — 鬼畜王ランス, Rance', font(18))
    y += 132
    section_head(c, x, y, 'Palette of the battle screen and the map\'s plates', width=w)
    for i, (name, back, accent) in enumerate(PALETTES):
        swatch(c, x + i * 84, y + 40, name, back, accent, name == 'Steel')


def game_page(c, x, y, w):
    section_head(c, x, y, 'Game folder', width=w)
    row_box(c, (x, y + 36, x + w, y + 76))
    c.text((x + 14, y + 56), FOLDER, font(17), fill=VALUE, anchor='lm')
    c.text((x + w - 14, y + 56), 'Change…', font(15), fill=HOVER_LINE, anchor='rm')
    c.text((x, y + 98), 'Kichikuou Rance English Translation v1.0 — imported. Another folder opens the import screen.',
           font(13), fill=LABEL, anchor='lm')


# ---------------------------------------------------------------- A: a lacquer panel with an index on its left

def a_frame(c, current):
    r = (190, 60, 1090, 760)
    panel(c, r)
    c.text((640, 104), 'Options', brush(34), fill=NAME, anchor='mm')
    divider(c, 230, 1050, 136)
    sections = ['Messages', 'Display', 'Battle', 'Game']
    for i, name in enumerate(sections):
        y = 166 + i * 52
        row = (220, y, 400, y + 40)
        if name == current:
            c.rect(row, fill=HOVER, outline=HOVER_LINE, width=2)
            c.d.polygon([(400 * S, (y + 12) * S), (410 * S, (y + 20) * S), (400 * S, (y + 28) * S)], fill=HOVER_LINE)
        c.text((238, y + 20), name, cinzel(17, 700 if name == current else 600), fill=NAME if name == current else SUB, anchor='lm')
    c.line((424, 160), (424, 690), fill=OFF_LINE)
    plain_button(c, (220, 700, 330, 736), 'Defaults')
    red_button(c, (930, 696, 1060, 740), 'Close', size=18)
    return 452, 166, 600


def a1():
    c = Canvas()
    x, y, w = a_frame(c, 'Messages')
    messages_page(c, x, y, w)
    c.save('options-A1-messages.png', 'A1 — a lacquer panel, the sections as an index on its left: Messages',
           'Option opens it at once (no drop-down); the game waits. Each change applies at once; Close, Esc or a right click shut it.')


def a2():
    c = Canvas()
    x, y, w = a_frame(c, 'Battle')
    end = battle_page(c, x, y, w)
    strip_preview(c, x, end + 8, w - 110)
    plain_button(c, (x + w - 96, end + 14, x + w, end + 48), 'Preview')
    c.save('options-A2-battle.png', 'A2 — the same panel: Battle',
           'Battle Screen\'s sliders in two columns, the order strip as their preview; Defaults resets the page shown.')


# ---------------------------------------------------------------- B: one paper sheet with every setting

def b_sheet(c, heading, sub):
    r = (110, 50, 1170, 770)
    panel(c, r)
    paper = (136, 76, 1144, 744)
    c.rect(paper, fill=PAPER, outline=PAPER_HEAD, width=1)
    c.text((640, 112), heading, brush(32), fill=HEAD_INK, anchor='mm')
    c.text((640, 146), sub, cinzel(14, 600), fill=INK_QUIET, anchor='mm')
    c.line((160, 164), (1120, 164), fill=PAPER_RULE, width=2)
    return paper


def b1():
    c = Canvas()
    b_sheet(c, 'Options', 'Kichikuou Rance English Translation v1.0')
    # left column: messages and the mouse
    x, w, y = 176, 440, 186
    section_head(c, x, y, 'Text speed', paper=True, width=w)
    slider(c, x + 60, x + w - 70, y + 62, (SPEED_STEP - 1) / 9, paper=True, knob_label=str(SPEED_STEP))
    c.text((x, y + 62), 'Slow', font(14), fill=INK_QUIET, anchor='lm')
    c.text((x + w, y + 62), 'Instant', font(14), fill=INK_QUIET, anchor='rm')
    sample_window(c, (x, y + 84, x + w, y + 176))
    y += 202
    section_head(c, x, y, 'Message skip', paper=True, width=w)
    check_box(c, x, y + 36, False, 'Skip unseen text', 'MsgSkip and Ctrl skip unread messages too.', paper=True)
    check_box(c, x, y + 88, True, 'MsgSkip speeds up fades', 'Fades and waits run fast while skipping.', paper=True)
    c.text((x, y + 152), '3 412 messages seen.', font(14), fill=INK_QUIET, anchor='lm')
    c.text((x + w, y + 152), 'Forget them…', font(15), fill=VERMILION_INK, anchor='rm')
    y += 186
    section_head(c, x, y, 'Mouse', paper=True, width=w)
    check_box(c, x, y + 36, True, 'Mouse movement', 'The pointer moves to the chosen item.', paper=True)
    # right column: display, battle, game folder
    x, w, y = 664, 440, 186
    section_head(c, x, y, 'Font', paper=True, width=w)
    ms = font(17, MS_GOTHIC) if os.path.exists(MS_GOTHIC) else font(17)
    radio(c, x, y + 36, False, 'MS Gothic — 鬼畜王ランス', ms, paper=True)
    radio(c, x, y + 70, True, 'BIZ UDGothic — 鬼畜王ランス', font(17), paper=True)
    y += 116
    section_head(c, x, y, 'Palette', paper=True, width=w)
    for i, (name, back, accent) in enumerate(PALETTES):
        swatch(c, x + i * 86, y + 36, name, back, accent, name == 'Steel', paper=True)
    y += 124
    section_head(c, x, y, 'Battle screen', paper=True, width=w)
    check_box(c, x, y + 36, True, 'Exchange effects', None, paper=True)
    c.text((x + w, y + 46), 'Timings…', font(15), fill=VERMILION_INK, anchor='rm')
    y += 84
    section_head(c, x, y, 'Game folder', paper=True, width=w)
    c.rect((x, y + 34, x + w, y + 70), fill=FIELD, outline=INK_GREY, width=1)
    c.text((x + 12, y + 52), FOLDER, font(16), fill=INK, anchor='lm')
    c.text((x + w - 12, y + 52), 'Change…', font(14), fill=VERMILION_INK, anchor='rm')
    red_button(c, (560, 690, 720, 732), 'Close', size=18)
    c.save('options-B1-sheet.png', 'B1 — one paper sheet (the import screen\'s look) with every setting at once',
           'Two columns, nothing behind a tab; the battle\'s twelve timings behind "Timings…" (B2). Changes apply at once.')


def b2():
    c = Canvas()
    b_sheet(c, 'Battle screen timings', 'Options · Battle screen')
    x, w, y = 176, 928, 190
    col_w = (w - 40) / 2
    for i, (label, value, lo, hi, unit) in enumerate(BATTLE):
        cx = x + (i % 2) * (col_w + 40)
        cy = y + (i // 2) * 54
        c.text((cx, cy + 8), label, font(15), fill=INK, anchor='lm')
        slider(c, cx, cx + col_w - 80, cy + 32, (value - lo) / (hi - lo), paper=True)
        c.text((cx + col_w, cy + 32), fmt(value, unit), font(15), fill=INK, anchor='rm')
    y += 6 * 54 + 10
    strip_preview(c, x, y, w - 130)
    c.rect((x + w - 110, y + 6, x + w, y + 40), fill=FIELD, outline=INK_GREY, width=1)
    c.text((x + w - 55, y + 23), 'Preview', font(15), fill=VERMILION_INK, anchor='mm')
    c.text((x, 712), '← Back to the options', font(15), fill=INK_QUIET, anchor='lm')
    c.text((x + w, 712), 'Defaults', font(15), fill=VERMILION_INK, anchor='rm')
    c.save('options-B2-timings.png', 'B2 — the second sheet: the battle screen\'s timings',
           'Battle Screen\'s sliders as now, the order strip\'s preview under them; back to the first sheet by the link, Esc or a right click.')


# ---------------------------------------------------------------- C: a wide panel with tabs across its top

def c_frame(c, current):
    r = (100, 70, 1180, 750)
    panel(c, r)
    tabs = ['Messages', 'Display', 'Battle', 'Game']
    x = 130
    for name in tabs:
        tw = c.width(name, cinzel(18, 700)) + 48
        tab = (x, 92, x + tw, 136)
        if name == current:
            c.rect(tab, fill=HOVER, outline=HOVER_LINE, width=2)
            c.rect((x + 2, 132, x + tw - 2, 140), fill=HOVER)
        else:
            c.rect(tab, fill=ROW, outline=DIM, width=1)
        c.text((x + tw / 2, 114), name, cinzel(18, 700 if name == current else 600), fill=NAME if name == current else SUB, anchor='mm')
        x += tw + 8
    c.text((1150, 114), 'Options', brush(26), fill=NAME, anchor='rm')
    c.line((120, 137), (1160, 137), fill=HOVER_LINE, width=2)
    red_button(c, (1020, 690, 1150, 732), 'Close', size=18)
    plain_button(c, (130, 694, 240, 730), 'Defaults')
    return 150, 166, 980


def c1():
    c = Canvas()
    x, y, w = c_frame(c, 'Display')
    display_page(c, x, y, w)
    game_page(c, x, y + 300, w)
    c.save('options-C1-display.png', 'C1 — a wide panel, the sections as tabs across its top: Display',
           'The same pages as A on tabs; the window wider, so the font samples and the palettes have room. Here Game\'s folder shares the page.')


def c2():
    c = Canvas()
    x, y, w = c_frame(c, 'Messages')
    # the speed and its preview on the left, the switches on the right
    lw = 470
    section_head(c, x, y, 'Text speed', width=lw)
    slider(c, x + 60, x + lw - 70, y + 66, (SPEED_STEP - 1) / 9, knob_label=str(SPEED_STEP))
    c.text((x, y + 66), 'Slow', font(14), fill=LABEL, anchor='lm')
    c.text((x + lw, y + 66), 'Instant', font(14), fill=LABEL, anchor='rm')
    sample_window(c, (x, y + 94, x + lw, y + 190))
    rx, rw = x + lw + 50, w - lw - 50
    section_head(c, rx, y, 'Message skip', width=rw)
    check_box(c, rx, y + 38, False, 'Skip unseen text', 'MsgSkip and Ctrl skip unread ones too.')
    check_box(c, rx, y + 92, True, 'MsgSkip speeds up fades', 'Fades and waits run fast.')
    plain_button(c, (rx, y + 148, rx + 230, y + 180), 'Forget seen messages…')
    section_head(c, rx, y + 220, 'Mouse', width=rw)
    check_box(c, rx, y + 258, True, 'Mouse movement', 'The pointer moves to the chosen item.')
    c.save('options-C2-messages.png', 'C2 — the same panel: Messages',
           'Two columns on a page: the speed with its preview left, the switches right. Changes apply at once; Close, Esc or a right click.')


# ---------------------------------------------------------------- D (round 2): C's tabs in B's paper

def d_frame(c, current):
    """The lacquer frame round a paper sheet; the tabs are paper too, the current one joined to the sheet."""
    r = (100, 56, 1180, 760)
    panel(c, r)
    paper = (126, 126, 1154, 734)
    tabs = ['Messages', 'Display', 'Battle', 'Game']
    x = 126
    for name in tabs:
        tw = c.width(name, cinzel(18, 700)) + 52
        on = name == current
        tab = (x, 82 if on else 88, x + tw, 127)
        c.rect(tab, fill=PAPER if on else PAPER_HEAD, outline=INK_GREY if not on else PAPER_HEAD, width=1)
        if on:
            c.rect((x + 1, 120, x + tw - 1, 130), fill=PAPER)
            c.rect((x, 82, x + tw, 86), fill=VERMILION)
        c.text((x + tw / 2, 107 if on else 109), name, cinzel(18, 700 if on else 600), fill=HEAD_INK if on else INK_QUIET, anchor='mm')
        x += tw + 6
    c.rect(paper, fill=None, outline=PAPER_HEAD, width=1)
    c.rect((paper[0] + 1, paper[1], paper[2] - 1, paper[3] - 1), fill=PAPER)
    # the current tab's foot opens into the sheet
    x = 126
    for name in tabs:
        tw = c.width(name, cinzel(18, 700)) + 52
        if name == current:
            c.rect((x + 1, 124, x + tw - 1, 130), fill=PAPER)
        x += tw + 6
    c.text((1150, 104), 'Options', brush(28), fill=NAME, anchor='rm')
    c.text((150, 706), 'Defaults', font(15), fill=VERMILION_INK, anchor='lm')
    red_button(c, (1004, 686, 1134, 726), 'Close', size=17)
    return 166, 156, 948


def d_messages(c, x, y, w):
    lw = 470
    section_head(c, x, y, 'Text speed', paper=True, width=lw)
    slider(c, x + 60, x + lw - 70, y + 66, (SPEED_STEP - 1) / 9, paper=True, knob_label=str(SPEED_STEP))
    c.text((x, y + 66), 'Slow', font(14), fill=INK_QUIET, anchor='lm')
    c.text((x + lw, y + 66), 'Instant', font(14), fill=INK_QUIET, anchor='rm')
    sample_window(c, (x, y + 94, x + lw, y + 190))
    rx, rw = x + lw + 50, w - lw - 50
    section_head(c, rx, y, 'Message skip', paper=True, width=rw)
    check_box(c, rx, y + 38, False, 'Skip unseen text', 'MsgSkip and Ctrl skip unread ones too.', paper=True)
    check_box(c, rx, y + 92, True, 'MsgSkip speeds up fades', 'Fades and waits run fast.', paper=True)
    c.text((rx, y + 160), '3 412 messages seen.', font(14), fill=INK_QUIET, anchor='lm')
    c.text((rx + rw, y + 160), 'Forget them…', font(15), fill=VERMILION_INK, anchor='rm')
    section_head(c, rx, y + 220, 'Mouse', paper=True, width=rw)
    check_box(c, rx, y + 258, True, 'Mouse movement', 'The pointer moves to the chosen item.', paper=True)


def d1():
    c = Canvas()
    x, y, w = d_frame(c, 'Messages')
    d_messages(c, x, y, w)
    c.save('options-D1-messages.png', 'D1 — C\'s tabs in B\'s paper: Messages',
           'The sheet in the lacquer frame, the tabs paper too (the current one joined to the sheet, a vermilion edge on it). Defaults resets the tab shown.')


def d2():
    c = Canvas()
    x, y, w = d_frame(c, 'Display')
    section_head(c, x, y, 'Font', paper=True, width=w)
    ms = font(18, MS_GOTHIC) if os.path.exists(MS_GOTHIC) else font(18)
    radio(c, x, y + 38, False, 'MS Gothic — 鬼畜王ランス, Rance', ms, paper=True)
    radio(c, x, y + 74, True, 'BIZ UDGothic — 鬼畜王ランス, Rance', font(18), paper=True)
    y += 132
    section_head(c, x, y, 'Palette of the battle screen and the map\'s plates', paper=True, width=w)
    for i, (name, back, accent) in enumerate(PALETTES):
        swatch(c, x + i * 90, y + 40, name, back, accent, name == 'Steel', paper=True)
    c.save('options-D2-display.png', 'D2 — the same: Display',
           'The fonts with a sample each, the palettes as swatches (the chosen one framed in vermilion).')


def d3():
    c = Canvas()
    x, y, w = d_frame(c, 'Battle')
    section_head(c, x, y, 'Effects', paper=True, width=w)
    check_box(c, x, y + 36, True, 'Exchange effects', 'Our effects over the exchange, after Sengoku Rance\'s.', paper=True)
    y += 96
    section_head(c, x, y, 'Timings', paper=True, width=w)
    y += 36
    col_w = (w - 40) / 2
    for i, (label, value, lo, hi, unit) in enumerate(BATTLE):
        cx = x + (i % 2) * (col_w + 40)
        cy = y + (i // 2) * 46
        c.text((cx, cy + 8), label, font(14), fill=INK, anchor='lm')
        slider(c, cx, cx + col_w - 80, cy + 29, (value - lo) / (hi - lo), paper=True)
        c.text((cx + col_w, cy + 29), fmt(value, unit), font(14), fill=INK, anchor='rm')
    y += 6 * 46 + 6
    strip_preview(c, x, y, w - 130)
    c.rect((x + w - 110, y + 6, x + w, y + 40), fill=FIELD, outline=INK_GREY, width=1)
    c.text((x + w - 55, y + 23), 'Preview', font(15), fill=VERMILION_INK, anchor='mm')
    c.save('options-D3-battle.png', 'D3 — the same: Battle',
           'Effects, the twelve timings in two columns, the order strip as their preview (Preview makes Barres jump ahead and back).')


def d4():
    c = Canvas()
    x, y, w = d_frame(c, 'Game')
    section_head(c, x, y, 'Game folder', paper=True, width=w)
    c.rect((x, y + 36, x + w, y + 76), fill=FIELD, outline=INK_GREY, width=1)
    folder_x = x + 12
    c.d.polygon([(folder_x * S, (y + 49) * S), ((folder_x + 7) * S, (y + 49) * S), ((folder_x + 9) * S, (y + 52) * S),
                 ((folder_x + 20) * S, (y + 52) * S), ((folder_x + 20) * S, (y + 64) * S), (folder_x * S, (y + 64) * S)],
                outline=INK_QUIET, width=2 * S)
    c.text((x + 44, y + 56), FOLDER, font(17), fill=INK, anchor='lm')
    c.text((x + w - 14, y + 56), 'Change…', font(15), fill=VERMILION_INK, anchor='rm')
    c.text((x, y + 100), 'Kichikuou Rance English Translation v1.0, imported. Another folder opens the import screen '
                         '(the game is left for the title).', font(14), fill=INK_QUIET, anchor='lm')
    c.save('options-D4-game.png', 'D4 — the same: Game',
           'The game folder; Change… asks first ("Any unsaved progress will be lost."), then the import screen.')


for f in (a1, a2, b1, b2, c1, c2, d1, d2, d3, d4):
    f()
print('wrote', len(os.listdir(OUT)), 'images to', OUT)
