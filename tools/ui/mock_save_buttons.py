"""Mock-ups of the Save/Load screen's buttons (docs/ui/save-load.md, §11): the right-click hint gone, a Back button, a
Delete that stands out — drawn over dumps of the screen as built. The dumps come from the --saveload look
(CLAUDE.md, "Our Save/Load screen"): its "save-load-title", "save-load-saved" and "save-load" dumps, 1280x824 with
the menu bar's 24 pixels on top. From the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_save_buttons.py <title dump> <saved dump> <load dump> <out dir>
Round B: B1/B2 Back in the title band, Delete enlarged in its place; B3/B4 Back at the foot's left, Delete as tall as
the red button, the Save line on the red button; B5-B7 Delete at the page's title, Back at the foot's left.
The images hold the player's CGs: keep <out dir> outside the repository. Everything is in the 1280x800 screen's pixels
(the dump's menu bar cut off, plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

TITLE_DUMP, SAVED_DUMP, LOAD_DUMP, OUT = sys.argv[1:5]
sys.stdout.reconfigure(encoding='utf-8')
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
S = 2
W, H, CAPTION, BAR = 1280, 800, 64, 24

# Lacquer.cs (as tools/ui/mock_saves.py)
PANEL, GOLD, DIM = (22, 16, 12, 255), (196, 160, 86, 255), (110, 86, 46, 255)
NAME, SUB, LABEL = (232, 199, 122, 255), (160, 140, 104, 255), (150, 134, 108, 255)
VALUE, ROW, HOVER = (236, 226, 204, 255), (34, 26, 20, 255), (64, 48, 28, 255)
HOVER_LINE, TEXT, QUIET = (222, 186, 104, 255), (244, 232, 206, 255), (176, 160, 130, 255)
VERMILION = (170, 40, 28, 255)
DANGER_FILL, DANGER_TEXT = (52, 20, 14, 255), (236, 132, 104, 255)

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


class Canvas:
    def __init__(self, dump):
        img = Image.open(dump).convert('RGBA').crop((0, BAR, W, BAR + H))
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (14, 10, 8, 255))
        self.im.paste(img.resize((W * S, H * S), Image.LANCZOS), (0, 0))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill, outline=outline, width=round(width * S))

    def text(self, xy, s, f, fill=TEXT, anchor='la'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=DIM, width=1):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=round(width * S))

    def poly(self, pts, fill=None, outline=None):
        self.d.polygon([(x * S, y * S) for x, y in pts], fill=fill, outline=outline)

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=DIM)
        self.text((16, H + 10), caption, cinzel(20), fill=NAME)
        self.text((16, H + 38), note, font(15), fill=QUIET)
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
        print(name)


def erase(c, r):
    c.rect(r, fill=PANEL)


def row_box(c, r, hover=False):
    c.rect(r, fill=HOVER if hover else ROW, outline=HOVER_LINE if hover else DIM, width=2 if hover else 1)


def red_button(c, r, title, sub=None):
    x0, y0, x1, y1 = r
    top, bottom = (128, 32, 24), (80, 16, 12)
    for i in range(round(y1 - y0)):
        t = i / max(1, y1 - y0 - 1)
        c.line((x0, y0 + i), (x1, y0 + i), fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,))
    c.rect(r, outline=GOLD, width=2)
    c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(150, 96, 52, 255), width=1)
    cx = (x0 + x1) / 2
    if sub:
        c.text((cx, y0 + (y1 - y0) * 0.38), title, brush(20), fill=NAME, anchor='mm')
        c.text((cx, y0 + (y1 - y0) * 0.76), sub, font(12), fill=(236, 206, 160, 255), anchor='mm')
    else:
        c.text((cx, (y0 + y1) / 2), title, brush(22), fill=NAME, anchor='mm')


def back_button(c, r, label='Back'):
    """A quiet row box with a gold arrow: the way out the right click also takes."""
    row_box(c, r)
    x0, y0, x1, y1 = r
    f = cinzel(16)
    w = c.width(label, f) + 18
    x, my = (x0 + x1) / 2 - w / 2, (y0 + y1) / 2
    c.poly([(x + 8, my - 6), (x + 8, my + 6), (x, my)], fill=GOLD)
    c.text((x + 18, my), label, f, fill=TEXT, anchor='lm')


def bin_icon(c, x, y, colour):
    """A small waste bin, 14 wide and 16 high, its top left at (x, y)."""
    c.line((x, y + 3), (x + 14, y + 3), fill=colour, width=1.6)
    c.line((x + 5, y), (x + 9, y), fill=colour, width=1.6)
    c.poly([(x + 2, y + 5), (x + 12, y + 5), (x + 11, y + 16), (x + 3, y + 16)], outline=colour)
    c.poly([(x + 2.6, y + 5.6), (x + 11.4, y + 5.6), (x + 10.5, y + 15.4), (x + 3.5, y + 15.4)], outline=colour)
    for lx in (x + 5.5, x + 8.5):
        c.line((lx, y + 7.5), (lx, y + 13.5), fill=colour, width=1)


def delete_button(c, r, size=16):
    """Delete as a danger button: dark red lacquer in a vermilion frame, light vermilion text after a bin."""
    x0, y0, x1, y1 = r
    c.rect(r, fill=DANGER_FILL, outline=VERMILION, width=2)
    c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(110, 40, 28, 255), width=1)
    f = cinzel(size)
    w = c.width('Delete', f) + 22
    x, my = (x0 + x1) / 2 - w / 2, (y0 + y1) / 2
    bin_icon(c, x, my - 9, DANGER_TEXT)
    c.text((x + 22, my + 1), 'Delete', f, fill=DANGER_TEXT, anchor='lm')


def tabs(c, active):
    """Load | Save as built (TabRect)."""
    for i, label in enumerate(('Load', 'Save')):
        r = (1030 + i * 96, 35, 1120 + i * 96, 65)
        if label == active:
            c.rect(r, fill=(60, 30, 22, 255), outline=VERMILION, width=2)
            c.text(((r[0] + r[2]) / 2, 50), label, cinzel(15), fill=NAME, anchor='mm')
        else:
            c.rect(r, fill=ROW, outline=DIM, width=1)
            c.text(((r[0] + r[2]) / 2, 50), label, cinzel(15), fill=QUIET, anchor='mm')


FOOT = (420, 690, 1232, 756)          # the page's foot: the hint, Delete, the red button (ButtonRect 896,694 330x58)
BUTTON = (896, 694, 1226, 752)
SAVING = ('Saving: LP 3 · Month 5 · Week 2', '116,400K GOLD · 15 provinces · 6,251 troops')


def saving_lines(c, x=426):
    c.text((x, 710), SAVING[0], font(15), fill=TEXT, anchor='lm')
    c.text((x, 734), SAVING[1], font(14), fill=QUIET, anchor='lm')


# ---- B1/B2: Back in the title band (left of Load | Save from the map, in their place from the title); Delete in its
# place, as tall as the red button.

def top_back(c, with_tabs):
    if with_tabs:
        erase(c, (900, 32, 1232, 68))
        back_button(c, (906, 35, 1010, 65))
        c.line((1020, 39), (1020, 61), fill=DIM)
        tabs(c, 'Load')
    else:
        back_button(c, (1112, 35, 1216, 65))


def b1(dump, name, caption, note, with_tabs, mode):
    c = Canvas(dump)
    top_back(c, with_tabs)
    erase(c, FOOT)
    if mode == 'save':
        saving_lines(c)
        red_button(c, BUTTON, 'Save over No. 1')
    else:
        red_button(c, BUTTON, 'Load')
    delete_button(c, (726, 694, 882, 752), 17)
    c.save(name, caption, note)


# ---- B3/B4: Back at the foot's left, Delete as tall as the red button beside it; in Save what is saved goes on the
# red button's second line.

def b3(dump, name, caption, note, mode, back_label):
    c = Canvas(dump)
    erase(c, FOOT)
    back_button(c, (426, 694, 596, 752), back_label)
    delete_button(c, (726, 694, 882, 752), 17)
    if mode == 'save':
        red_button(c, BUTTON, 'Save over No. 1', 'now: LP 3 · Month 5 · Week 2 · 116,400K GOLD')
    else:
        red_button(c, BUTTON, 'Load')
    c.save(name, caption, note)


# ---- B5-B7: Delete at the page's title, right (it belongs to the save shown, away from the red button); Back at the
# foot's left.

def b5(dump, name, caption, note, mode, back_label, asking=False):
    c = Canvas(dump)
    erase(c, FOOT)
    delete_button(c, (1090, 84, 1226, 114), 15)
    if mode == 'save':
        back_button(c, (426, 694, 566, 752), back_label)
        c.text((586, 710), SAVING[0], font(15), fill=TEXT, anchor='lm')
        c.text((586, 734), SAVING[1], font(13), fill=QUIET, anchor='lm')
        red_button(c, BUTTON, 'Save over No. 1')
    else:
        back_button(c, (426, 694, 626, 752), back_label)
        red_button(c, BUTTON, 'Load')
    if asking:
        r = (806, 126, 1226, 236)
        c.rect((r[0] + 4, r[1] + 8, r[2] + 4, r[3] + 8), fill=(0, 0, 0, 255))
        c.rect(r, fill=PANEL)
        c.rect((r[0] + 1, r[1] + 1, r[2] - 1, r[3] - 1), outline=GOLD, width=2)
        c.rect((r[0] + 5, r[1] + 5, r[2] - 5, r[3] - 5), outline=DIM, width=1)
        tip = 1158
        c.poly([(tip - 10, r[1] + 2), (tip + 10, r[1] + 2), (tip, r[1] - 12)], fill=GOLD)
        c.text((r[0] + 22, r[1] + 30), 'Delete No. 1, LP 3 · Month 5 · Week 2?', font(16), fill=TEXT, anchor='lm')
        c.text((r[0] + 22, r[1] + 54), 'It cannot be undone.', font(16), fill=TEXT, anchor='lm')
        red_button(c, (r[2] - 222, r[3] - 40, r[2] - 112, r[3] - 10), 'Delete')
        row_box(c, (r[2] - 102, r[3] - 40, r[2] - 22, r[3] - 10))
        c.text((r[2] - 62, r[3] - 25), 'Keep', cinzel(14), fill=TEXT, anchor='mm')
    c.save(name, caption, note)


os.makedirs(OUT, exist_ok=True)
b1(LOAD_DUMP, 'B1-top-back-load.png', 'B1 — Back in the title band, Delete as tall as the red button (Load from the map)',
   'Back left of the Load | Save tabs; Delete in dark red with a bin, where it was, 156 wide. A right click still goes back.',
   True, 'load')
b1(TITLE_DUMP, 'B2-top-back-title.png', 'B2 — B1 from the title: no tabs, Back alone at the band\'s right',
   'The same Delete on an auto-save (Load).', False, 'load')
b1(SAVED_DUMP, 'B2s-top-back-save.png', 'B2s — B1 in Save: the line of what is saved stays at the foot\'s left',
   'Nothing else moves at the foot.', False, 'save')
b3(LOAD_DUMP, 'B3-foot-back-load.png', 'B3 — Back at the foot\'s left, Delete beside the red button, all three as tall',
   'The foot is a row of buttons: Back · · · Delete | Load.', 'load', 'Back to the map')
b3(SAVED_DUMP, 'B4-foot-back-save.png', 'B4 — B3 in Save: what is saved moves onto the red button\'s second line',
   'The foot keeps its three buttons in Load and Save alike.', 'save', 'Back to the map')
b5(LOAD_DUMP, 'B5-delete-at-title-load.png', 'B5 — Delete at the save\'s title, right; Back at the foot\'s left',
   'Delete sits with the save it deletes, far from Load; the foot is Back and the red button.', 'load', 'Back to the map')
b5(SAVED_DUMP, 'B6-delete-at-title-save.png', 'B6 — B5 in Save: Back, the line of what is saved, the red button',
   'The same Delete at the title, on the save chosen.', 'save', 'Back')
b5(LOAD_DUMP, 'B7-delete-at-title-ask.png', 'B7 — B5 asking: the question hangs under Delete, over the map',
   'Delete | Keep as built; Keep or a right click closes it.', 'load', 'Back to the map', asking=True)
