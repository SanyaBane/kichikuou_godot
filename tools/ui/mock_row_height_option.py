"""Mock-ups of the ledgers' row height as an option (owner, 2026-10-26; docs/ui/unit-ledger.md §8): Option -> Display gets
a section for the height of the unit tables' rows (Subordinates and deployment), on the PC and in the touch mode alike,
each device keeping its own (24 pixels on the PC, 36 in the touch mode by default).

Drawn over the screen gallery's dumps of the options window's Display tab (docs/ui/gallery.md; tools/ui/gallery.ps1,
the Lacquer and touch passes): options-display.png from each pass folder.
    python -I tools/ui/mock_row_height_option.py <lacquer dump dir> <touch dump dir> <out dir> < /dev/null
O1: a slider from 24 to 48 pixels with a preview of two ledger rows at the height chosen; O2: five tiles, each two
ledger rows at its height, the chosen one framed in vermilion. Each on the PC (24 chosen) and on the tablet (36).
The images hold the player's CGs (the map behind the window): keep <out dir> outside the repository.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

LACQUER, TOUCH, OUT = sys.argv[1:4]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H, CAPTION = 1280, 800, 64

# UiPalette.Lacquer (godot/Strategy/UiPalette.cs), as tools/ui/mock_unit_ledger.py has them
NAME, QUIET, DIM = (232, 199, 122, 255), (176, 160, 130, 255), (110, 86, 46, 255)
PAPER, PAPER_HEAD, PAPER_RULE = (224, 208, 172, 255), (196, 176, 136, 255), (150, 120, 80, 110)
INK, INK_QUIET, INK_GREY = (40, 28, 18, 255), (100, 80, 56, 255), (150, 132, 104, 255)
HEAD_INK, VERMILION, VERMILION_INK = (120, 40, 26, 255), (170, 40, 28, 255), (150, 52, 30, 255)
PARTY_ROW, FIELD = (200, 160, 80, 70), (236, 224, 196, 255)
BAR_GOOD, BAR_LOW = (70, 110, 50, 255), (160, 110, 30, 255)

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
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


class Canvas:
    """The dump at 2x, drawn on opaque with the colours' alpha blended (an RGB image under an RGBA draw)."""

    def __init__(self, dump):
        self.im = Image.open(dump).convert('RGB').resize((W * S, H * S), Image.LANCZOS)
        self.im = self.im.crop((0, 0, W * S, (H + CAPTION) * S))
        self.d = ImageDraw.Draw(self.im, 'RGBA')

    def rect(self, r, fill=None, outline=None, width=1, radius=0):
        x0, y0, x1, y1 = r
        box = [x0 * S, y0 * S, x1 * S - 1, y1 * S - 1]
        if radius:
            self.d.rounded_rectangle(box, radius * S, fill=fill, outline=outline, width=round(width * S))
        else:
            self.d.rectangle(box, fill=fill, outline=outline, width=round(width * S))

    def text(self, xy, s, f, fill=INK, anchor='la'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill, width=1):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=round(width * S))

    def circle(self, c, r, fill=None, outline=None, width=1):
        self.d.ellipse([(c[0] - r) * S, (c[1] - r) * S, (c[0] + r) * S, (c[1] + r) * S], fill=fill, outline=outline,
                       width=round(width * S))

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=DIM)
        self.text((16, H + 10), caption, cinzel(20), fill=NAME)
        self.text((16, H + 38), note, font(15), fill=QUIET)
        self.im.resize((W, H + CAPTION), Image.LANCZOS).save(os.path.join(OUT, name))


# OptionsView: the sheet's content from x 166, 948 wide; Display's sections every 132 from y 155 (Font, Palette,
# Thousands); the new one after Thousands.
X, CW = 166, 948
Y = 155 + 264 + 104
HEIGHTS = [24, 30, 36, 42, 48]
ROWS = [(1, 'Rance', 'Leazas Knights', '1,000/1,000', 1.0, '300K', 1), (5, 'Leila', 'Royal Guards', '800/800', 1.0, '1,200K', 0),
        (6, 'Barres', 'Leazas Knights', '980/1,000', 0.98, '1,100K', 4)]


def section_head(c, y, title):
    """OptionsView.SectionHead: Cinzel capitals in the headers' ink over a rule."""
    c.text((X, y + 14), title.upper(), cinzel(13), fill=HEAD_INK, anchor='ls')
    c.line((X, y + 22), (X + CW, y + 22), PAPER_RULE)


def seal(c, x, y, n, size=19):
    c.rect((x, y, x + size, y + size), fill=VERMILION, radius=3)
    c.rect((x + 2, y + 2, x + size - 2, y + size - 2), outline=(236, 196, 170, 140), radius=2)
    c.text((x + size / 2, y + size / 2 + 1), str(n), cinzel(13), fill=(246, 230, 206, 255), anchor='mm')


def ledger_rows(c, r, h, rows, wide=True):
    """Ledger rows as DeploymentTableView draws them (the touch mode's Content: marks and figures at the text's
    size, centred), at rows of h; wide with Type and Troops, else No., Name and Cost."""
    x0, y0, x1, y1 = r
    f = font(17)
    y = y0
    for n, name, kind, troops, frac, cost, place in rows:
        mid = y + h / 2
        if place:
            c.rect((x0, y, x1, y + h), fill=PARTY_ROW)
            seal(c, x0 + 6, mid - 9.5, place)
        c.line((x0, y + h - 1), (x1, y + h - 1), PAPER_RULE)
        c.text((x0 + 58, mid), str(n), f, fill=INK, anchor='rm')
        c.text((x0 + 64, mid), name, f, fill=INK, anchor='lm')
        if wide:
            c.text((x0 + 134, mid), kind, f, fill=INK_QUIET, anchor='lm')
            c.text((x0 + 270, mid - 1.5), troops, f, fill=INK, anchor='lm')
            c.rect((x0 + 270, mid + 8, x0 + 364, mid + 11), fill=PAPER_RULE)
            c.rect((x0 + 270, mid + 8, x0 + 270 + 94 * frac, mid + 11), fill=BAR_GOOD if frac >= 0.75 else BAR_LOW)
        else:
            c.text((x1 - 8, mid), cost, f, fill=INK, anchor='rm')
        y += h


def preview(c, r, h):
    """A small ledger: the header as the tables' (Cinzel in the headers' ink on the darker paper) and two rows."""
    x0, y0, x1, _ = r
    head = 28
    y1 = y0 + head + 2 * h
    c.rect((x0, y0, x1, y1), fill=PAPER, outline=DIM)
    c.rect((x0 + 1, y0 + 1, x1 - 1, y0 + head), fill=PAPER_HEAD)
    c.line((x0 + 1, y0 + head), (x1 - 1, y0 + head), HEAD_INK)
    for label, x, anchor in (('No.', x0 + 58, 'rm'), ('Name', x0 + 64, 'lm'), ('Type', x0 + 134, 'lm'), ('Troops', x0 + 270, 'lm')):
        c.text((x, y0 + head / 2 + 1), label, cinzel(12), fill=HEAD_INK, anchor=anchor)
    ledger_rows(c, (x0 + 1, y0 + head + 1, x1 - 1, y1), h, ROWS[:2])


def slider(c, x0, x1, y, fraction, label):
    """OptionsView.DrawSlider: a track filled up to the vermilion knob, the value over it."""
    c.rect((x0, y - 3, x1, y + 3), fill=FIELD, outline=INK_GREY)
    kx = x0 + (x1 - x0) * fraction
    if kx > x0 + 2:
        c.rect((x0 + 1, y - 2, kx, y + 2), fill=VERMILION_INK)
    c.circle((kx, y), 9, fill=VERMILION, outline=INK, width=1.5)
    c.text((kx, y - 16), label, font(13), fill=INK, anchor='ms')


HINT = 'Subordinates and deployment. Each device keeps its own.'


def o1(dump, h, default, name, device):
    c = Canvas(dump)
    section_head(c, Y, 'Unit tables: row height')
    c.text((X + CW, Y + 14), HINT, font(13), fill=INK_QUIET, anchor='rs')
    lw = 420
    sy = Y + 76
    slider(c, X + 80, X + lw - 60, sy, (h - 24) / 24, f'{h} px')
    c.text((X, sy + 5), 'Compact', font(14), fill=INK_QUIET, anchor='ls')
    c.text((X + lw, sy + 5), 'Large', font(14), fill=INK_QUIET, anchor='rs')
    c.text((X, sy + 44), f'Default: {default} px on this {device}.', font(13), fill=INK_QUIET, anchor='ls')
    preview(c, (X + lw + 60, Y + 34, X + CW, 0), h)
    c.save(name, f'O1 — a slider with a preview ({device}, {h} px)',
           'From 24 to 48 pixels in steps of 2; the preview: two rows at the height chosen, the tables change at once.')


def o2(dump, chosen, default, name, device):
    c = Canvas(dump)
    section_head(c, Y, 'Unit tables: row height')
    c.text((X + CW, Y + 14), HINT, font(13), fill=INK_QUIET, anchor='rs')
    tw, gap, top = 176, 17, Y + 58
    for i, h in enumerate(HEIGHTS):
        x = X + i * (tw + gap)
        r = (x, top, x + tw, top + 2 * h + 2)
        c.rect(r, fill=PAPER, outline=INK_GREY)
        ledger_rows(c, (x + 1, top + 1, x + tw - 1, r[3] - 1), h, ROWS[:2], wide=False)
        if h == chosen:
            c.rect((x - 3, top - 3, x + tw + 3, r[3] + 3), outline=VERMILION, width=3)
        label = f'{h} px' + (' — default' if h == default else '')
        c.text((x + tw / 2, top - 10), label, font(14), fill=INK if h == chosen else INK_QUIET, anchor='ms')
    c.save(name, f'O2 — five tiles ({device}, {chosen} px)',
           "Each tile two rows at its height, the chosen one framed in vermilion as the palette's swatches.")


o1(os.path.join(LACQUER, 'options-display.png'), 24, 24, 'O1-pc.png', 'PC')
o1(os.path.join(TOUCH, 'options-display.png'), 36, 36, 'O1-tablet.png', 'tablet')
o2(os.path.join(LACQUER, 'options-display.png'), 24, 24, 'O2-pc.png', 'PC')
o2(os.path.join(TOUCH, 'options-display.png'), 36, 36, 'O2-tablet.png', 'tablet')
