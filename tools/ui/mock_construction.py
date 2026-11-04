"""Mock-ups of a third state for the Construction tag (owner, 2026-10-25): while a building is under way
(BuildWeeksLeft > 0; Cﾃｲﾄ.ADV:50 refuses then, Maris's dialogue only) the status strip's tag and the command panel's
row show it, from the week the building is ordered on (the owner's decision 1) — in the panel too, its click still
playing Maris's refusal (decision 2). Drawn over the screen gallery's command-menu.png (docs/ui/gallery.md): the
strip's three groups and the panel's Construction row are drawn again by this script as StatusStripView and
CommandMenuView draw them, the Construction tag in the new state. The building in the mock-ups is the Military
Academy (16 weeks), 5 weeks left.

  powershell -ExecutionPolicy Bypass -File tools/ui/gallery.ps1 -Only command-menu
  python -I tools/ui/mock_construction.py screen-gallery/pc/current <out dir> < /dev/null

Keep <out dir> outside the repository. Variant A: the tag half-lit (its colour halfway to the spent grey, a dim gold
rim) with the weeks left on a chip. Variant B: the tag spent-grey with a gold bar along its foot (weeks built of the
building's whole) and the chip. Variant C: the tag hatched in its colour like a site's tape, with the chip. Each full
screen shows the hover's tooltip under the tag; compare.png sets the variants' tags side by side in their states.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

SHOTS, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H = 1280, 800

# Lacquer's colours (godot/Strategy/UiPalette.cs).
BG, BACKDROP = (21, 15, 12), (14, 10, 8)
GOLD, DIM_GOLD, NAME, SUBTITLE, LABEL = (196, 160, 86), (110, 86, 46), (232, 199, 122), (160, 140, 104), (150, 134, 108)
ROW, TEXT, QUIET, OFF_TEXT, OFF_TAG = (34, 26, 20), (244, 232, 206), (176, 160, 130), (98, 88, 74), (52, 46, 40)
BADGE, GROOVE, RIM = (60, 44, 24), (40, 32, 24), (26, 18, 12)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)

# StatusStripView.ColourOf.
BUILD, HAREM, TRADE = (46, 62, 104), (128, 44, 70), (36, 92, 96)
UNITS, CITY, ATTACK = (70, 96, 52), (150, 52, 36), (128, 30, 30)

LEFT, TOTAL, BUILDING = 5, 16, 'Military Academy'


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size)))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


def gothic(size):
    return font('BIZUDGothic-Regular.ttf', size)


def cinzel(size):
    return font('Cinzel-VariableFont_wght.ttf', size, 500)


def mix(a, b, t):
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))


class Pen:
    """Draws in screen units at zoom z from an origin, onto an RGB image."""

    def __init__(self, img, z, ox=0, oy=0):
        self.img, self.d, self.z, self.ox, self.oy = img, ImageDraw.Draw(img), z, ox, oy

    def p(self, x, y):
        return self.ox + x * self.z, self.oy + y * self.z

    def box(self, x, y, w, h):
        x0, y0 = self.p(x, y)
        x1, y1 = self.p(x + w, y + h)
        return [round(x0), round(y0), round(x1) - 1, round(y1) - 1]

    def rect(self, x, y, w, h, fill=None, outline=None, width=1):
        self.d.rectangle(self.box(x, y, w, h), fill=fill, outline=outline, width=max(1, round(width * self.z)))

    def line(self, pts, fill, width=1):
        self.d.line([self.p(*q) for q in pts], fill=fill, width=max(1, round(width * self.z)))

    def text(self, x, y, s, size, fill, f=gothic, anchor='lm', stroke=None):
        kw = dict(stroke_width=max(1, round(self.z)), stroke_fill=stroke) if stroke else {}
        self.d.text(self.p(x, y), s, font=f(size * self.z), fill=fill, anchor=anchor, **kw)

    def width(self, s, size, f=gothic):
        return f(size * self.z).getlength(s) / self.z

    def hatch(self, x, y, w, h, base, stripe, step=8, band=4):
        """Diagonal stripes of stripe over base, clipped to the rectangle."""
        x0, y0, x1, y1 = self.box(x, y, w, h)
        tile = Image.new('RGB', (x1 - x0 + 1, y1 - y0 + 1), base)
        d = ImageDraw.Draw(tile)
        s, b = step * self.z, band * self.z
        t = -tile.size[1]
        while t < tile.size[0] + tile.size[1]:
            d.polygon([(t, tile.size[1]), (t + b, tile.size[1]), (t + b + tile.size[1], 0), (t + tile.size[1], 0)], fill=stripe)
            t += s
        self.img.paste(tile, (x0, y0))


# --- the tags ---------------------------------------------------------------------------------------------------------

def chip_width(p, size, left=LEFT):
    return p.width(str(left), size - 1) + 8


def tag_width(p, word, state, size=13, left=LEFT):
    w = p.width(word, size) + 16
    return w + chip_width(p, size, left) + 2 if state == 'busy' else w


def chip(p, x, y, h, left, size):
    """The weeks left on a small dark chip in a gold rim at the tag's right end."""
    cw = p.width(str(left), size - 1) + 8
    p.rect(x - cw - 3, y + 3, cw, h - 6, fill=BADGE, outline=GOLD)
    p.text(x - cw / 2 - 3, y + h / 2, str(left), size - 1, NAME, anchor='mm')
    return cw


def tag(p, x, y, w, h, word, colour, state, variant=None, left=LEFT, size=13, with_chip=True):
    """UiPalette.DrawTag: lit (its colour, a gold rim) or spent (grey); 'busy' is the variant's third state."""
    if state == 'free':
        p.rect(x, y, w, h, fill=colour)
        p.rect(x, y, w, h, outline=GOLD)
        p.text(x + w / 2, y + h / 2, word, size, TEXT, anchor='mm')
        return
    if state == 'spent':
        p.rect(x, y, w, h, fill=OFF_TAG)
        p.text(x + w / 2, y + h / 2, word, size, OFF_TEXT, anchor='mm')
        return
    cw = chip_width(p, size, left) + 2 if with_chip else 0
    ink, stroke = QUIET, None
    if variant == 'A':
        p.rect(x, y, w, h, fill=mix(colour, OFF_TAG, 0.55))
        p.rect(x, y, w, h, outline=DIM_GOLD)
    elif variant == 'B':
        p.rect(x, y, w, h, fill=OFF_TAG)
        done = (TOTAL - left) / TOTAL
        p.rect(x + 2, y + h - 4, w - 4, 2, fill=GROOVE)
        if done > 0:
            p.rect(x + 2, y + h - 4, (w - 4) * done, 2, fill=GOLD)
        ink = LABEL
    else:
        p.hatch(x, y, w, h, OFF_TAG, mix(colour, OFF_TAG, 0.25))
        p.rect(x, y, w, h, outline=DIM_GOLD)
        ink, stroke = TEXT, RIM
    p.text(x + (w - cw) / 2, y + h / 2 - (1 if variant == 'B' else 0), word, size, ink, anchor='mm', stroke=stroke)
    if with_chip:
        chip(p, x + w, y, h - (2 if variant == 'B' else 0), left, size)


# --- the strip --------------------------------------------------------------------------------------------------------

GROUPS = [
    ('Castle', [('Construction', BUILD), ('Harem', HAREM), ('Merchant', TRADE)]),
    ('Subordinates', [('Troops', UNITS), ('Talk', UNITS)]),
    ('Map', [('Own city', CITY), ('Attack', ATTACK), ('Dungeon', TRADE)]),
]


def strip(p, variant, x0=351):
    """StatusStripView's groups from x0: the tags in a 22-high row at y 10, brackets and captions under them. Returns
    the Construction tag's rectangle."""
    cx, at = x0, None
    for caption, uses in GROUPS:
        start = cx
        for word, colour in uses:
            state = 'busy' if word == 'Construction' else 'free'
            cw = tag_width(p, word, state)
            tag(p, cx, 10, cw, 22, word, colour, state, variant)
            if word == 'Construction':
                at = (cx, 10, cw, 22)
            cx += cw + 4
        end, by = cx - 4, 38
        p.line([(start, by - 3), (start, by), (end, by), (end, by - 3)], DIM_GOLD)
        tw = p.width(caption.upper(), 10, cinzel)
        mid = (start + end) / 2
        p.rect(mid - tw / 2 - 5, by - 2, tw + 10, 4, fill=BG)
        p.text(mid, by, caption.upper(), 10, SUBTITLE, cinzel, anchor='mm')
        cx += 22
    return at


def tooltip(p, x, y):
    """The hover's tooltip under the tag: the building and its weeks."""
    lines = [(BUILDING, 15, NAME), (f'{LEFT} weeks left', 13, QUIET)]       # the owner: no "11 of 16 built"
    w = max(p.width(s, n) for s, n, _ in lines) + 24
    p.rect(x, y, w, 52, fill=(26, 19, 14), outline=GOLD)
    p.rect(x + 3, y + 3, w - 6, 46, outline=DIM_GOLD)
    p.text(x + 12, y + 17, lines[0][0], lines[0][1], lines[0][2])
    p.text(x + 12, y + 36, lines[1][0], lines[1][1], lines[1][2])


# --- the panel's row --------------------------------------------------------------------------------------------------

ROW_RECT = (474, 143, 332, 38)


def panel_row(p, x, y, state, variant=None):
    """CommandMenuView.DrawCommand at 38: the row's box, the tag 74x26 inset 6 (its word 15), the text 19, the note 13."""
    w, h = ROW_RECT[2], ROW_RECT[3]
    p.rect(x, y, w, h, fill=ROW, outline=DIM_GOLD)
    tag(p, x + 6, y + 6, 74, 26, 'Build', BUILD, state, variant, size=15, with_chip=False)
    p.text(x + 93, y + h / 2, 'Construction', 19, TEXT if state == 'free' else OFF_TEXT if state == 'spent' else QUIET)
    note = {'spent': 'used this week', 'busy': f'{LEFT} weeks left'}.get(state)
    if note:
        p.text(x + w - 10, y + h / 2, note, 13, OFF_TEXT if state == 'spent' else QUIET, anchor='rm')


# --- the frames -------------------------------------------------------------------------------------------------------

def screen(variant):
    base = Image.open(os.path.join(SHOTS, 'command-menu.png')).convert('RGB').resize((W * S, H * S), Image.LANCZOS)
    p = Pen(base, S)
    p.rect(345, 3, 667, 44, fill=BG)                                    # the strip's groups, drawn again
    x, y, w, h = strip(p, variant)
    panel_row(p, *ROW_RECT[:2], 'busy', variant)
    tooltip(p, x, y + h + 22)
    base.resize((W, H), Image.LANCZOS).save(os.path.join(OUT, f'construction-{variant}.png'))


TITLES = {
    'A': 'A  half-lit, the weeks on a chip',
    'B': 'B  spent grey, a gold bar of the weeks built, the chip',
    'C': 'C  hatched like a site\'s tape, the chip',
}


def compare():
    """The three variants' tags side by side: free, spent, ordered this week (16), 5 left, 1 left; the panel's row
    spent and under way."""
    z, ss = 2, 2
    img = Image.new('RGB', (W * ss, 640 * ss), BACKDROP)
    top = Pen(img, ss)
    top.text(24, 22, 'Construction: a building under way', 20, NAME, cinzel)
    states = [('free', 'Free', None), ('spent', 'Spent this week', None), ('busy', 'Ordered this week', 16),
              ('busy', '5 weeks left', 5), ('busy', '1 week left', 1)]
    y = 56
    for v in 'ABC':
        top.text(24, y + 12, TITLES[v], 16, GOLD, cinzel)
        y += 34
        x = 24
        for state, label, left in states:
            p = Pen(img, z * ss, x * ss, y * ss)
            word = 'Construction'
            w = tag_width(p, word, state, left=left or LEFT)
            tag(p, 0, 0, w, 22, word, BUILD, state, v, left=left or LEFT)
            top.text(x, y + 22 * z + 14, label, 12, NOTE)
            x += w * z + 26
        y += 22 * z + 32
        x = 24
        for state in ('spent', 'busy'):
            p = Pen(img, 1.2 * ss, x * ss, y * ss)
            panel_row(p, 0, 0, state, v)
            x += ROW_RECT[2] * 1.2 + 30
        y += round(ROW_RECT[3] * 1.2) + 24
    img.resize((W, 640), Image.LANCZOS).save(os.path.join(OUT, 'compare.png'))


for v in 'ABC':
    screen(v)
compare()
