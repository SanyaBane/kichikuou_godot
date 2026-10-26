"""Mock-ups of the already-read text's colour on the battle's speech balloons (owner's request 2026-10-26; docs/ui/read-text.md):
a line the player read before shows in the colour chosen in Option -> Messages -> "Already-read text", as our message
windows do. The balloon is white with dark ink (a thought dark with white ink), where the nine colours, made for the
lacquer, read poorly; so three ways:

  B1  the chosen colour as it is, as on the windows;
  B2  the chosen colour darkened on the white balloon until it reads (4.5:1 against the paper), as it is on a thought;
  B3  the ink as it is, the balloon's paper tinted with the colour and its rim in it.

The dumps: the screen gallery's battle lines (a window; nothing moves the pointer):
  powershell -ExecutionPolicy Bypass -File tools/ui/gallery.ps1 -Only battle-speech,battle-speech-enemy
Then, from the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_battle_read.py screen-gallery/lacquer/current <out dir> < /dev/null
The images hold the player's pictures: keep <out dir> outside the repository. No capture the gallery replays has a
thought, so the thought is the player's balloon redrawn dark (SpeechBalloon's ThinkFill, ThinkEdge, ThinkInk).
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops

SHOTS, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
CAPTION = 64

GOLD, NAME, QUIET = (196, 160, 86), (232, 199, 122), (176, 160, 130)

# SpeechBalloon.cs (layout pixels of the 1280x800 screen).
WIDTH, BOTTOM, SIDE, RADIUS = 580, 436, 330, 16
TAIL_DEPTH, TAIL_HALF, TAIL_FROM_TOP = 18, 13, 44
TALK_INK = (24, 24, 28)
THINK_FILL, THINK_EDGE, THINK_INK = (30, 34, 44), (199, 204, 214), (255, 255, 255)

COLOURS = [('Salmon', (240, 120, 120)), ('Rose', (240, 128, 184)), ('Magenta', (255, 128, 255)),
           ('Violet', (192, 128, 255)), ('Periwinkle', (128, 128, 255)), ('Sky', (128, 184, 255)),
           ('Cyan', (128, 255, 255)), ('Mint', (128, 255, 192)), ('Green', (128, 255, 128))]


def font(path, size, weight=None):
    f = ImageFont.truetype(path, size)
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font(REPO + '/godot/fonts/BIZUDGothic-Regular.ttf', size)
CINZEL = lambda size, w=600: font(REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf', size, w)


def lum(c):
    def lin(v):
        v /= 255
        return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4
    return 0.2126 * lin(c[0]) + 0.7152 * lin(c[1]) + 0.0722 * lin(c[2])


def contrast(a, b):
    la, lb = sorted((lum(a), lum(b)), reverse=True)
    return (la + 0.05) / (lb + 0.05)


PAPER = (245, 245, 245)                     # the talk balloon's fill (white at 0.93) over the scene


def on_paper(c, ratio=4.5):
    """B2: the colour scaled towards black, its hue kept, until it reads on the white balloon."""
    k = 1.0
    while k > 0 and contrast(tuple(v * k for v in c), PAPER) < ratio:
        k -= 0.01
    return tuple(round(v * k) for v in c)


def mix(a, b, t):
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))


class Balloon:
    """The balloon in a dump: its box (found from its left side) and its shape as a mask."""

    def __init__(self, img, player):
        self.x = SIDE if player else 1280 - SIDE - WIDTH
        self.left = player
        px = img.load()
        top = BOTTOM - 4
        while top > 0 and sum(px[self.x + 12, top - 1]) > 600:
            top -= 1
        self.top = top
        self.box = (self.x, top, self.x + WIDTH, BOTTOM)

    def mask(self, grow=0):
        k = 4
        m = Image.new('L', (1280 * k, 800 * k), 0)
        d = ImageDraw.Draw(m)
        x0, y0, x1, y1 = self.box
        d.rounded_rectangle([(x0 + grow) * k, (y0 + grow) * k, (x1 - grow) * k, (y1 - grow) * k], radius=(RADIUS - grow) * k, fill=255)
        ty, rim, out = y0 + TAIL_FROM_TOP, (x0 if self.left else x1), (-1 if self.left else 1)
        d.polygon([((rim - out * (2 + grow)) * k, (ty - TAIL_HALF + grow) * k), ((rim + out * (TAIL_DEPTH - grow * 2)) * k, ty * k),
                   ((rim - out * (2 + grow)) * k, (ty + TAIL_HALF - grow) * k)], fill=255)
        return m.resize((1280, 800), Image.LANCZOS)

    def recolour(self, img, fill=None, edge=None, ink=None, read=None):
        """A copy of the dump with the balloon's paper, rim and ink replaced (None: as they are); read: the ink's colour."""
        out = img.copy()
        src, dst = img.load(), out.load()
        inner, outer = self.mask(2).load(), self.mask(0).load()
        x0, y0, x1, y1 = self.box
        for y in range(y0 - 4, y1 + 4):
            for x in range(x0 - TAIL_DEPTH - 4, x1 + TAIL_DEPTH + 4):
                a_out = outer[x, y] / 255
                if a_out == 0:
                    continue
                r, g, b = src[x, y]
                marker = x > x1 - 42 and y > y1 - 34 and max(r, g, b) - min(r, g, b) > 40
                if marker:
                    continue                                    # the click's gold marker stays
                t = min(1.0, max(0.0, (245 - (r + g + b) / 3) / (245 - 26)))   # how much of the pixel is ink
                paper = fill or (245, 245, 245)
                colour = read or ink or TALK_INK
                if fill is None and ink is None:
                    new = tuple(round(v + t * (c - i)) for v, c, i in zip((r, g, b), colour, TALK_INK))
                else:
                    new = mix(paper, colour, t)
                a_in = inner[x, y] / 255
                if edge is not None:
                    new = mix(edge, new, a_in)                  # the rim, 2 px, under the paper
                if a_out < 1:
                    new = mix((r, g, b), new, a_out)
                dst[x, y] = new
        return out

    def crop(self, img):
        x0, y0, x1, y1 = self.box
        return img.crop((x0 - TAIL_DEPTH - 6, y0 - 8, x1 + TAIL_DEPTH + 6, y1 + 12))


def save(img, name, caption, note):
    page = Image.new('RGB', (1280, 800 + CAPTION), (23, 18, 14))
    page.paste(img, (0, 0))
    d = ImageDraw.Draw(page)
    d.line((0, 800, 1280, 800), fill=GOLD, width=1)
    d.text((16, 810), caption, font=CINZEL(20, 700), fill=NAME)
    d.text((16, 838), note, font=GOTHIC(15), fill=QUIET)
    page.save(os.path.join(OUT, name))


def variants(colour):
    """Each variant's (talk, thought) recolouring for the chosen colour."""
    dark = on_paper(colour)
    return {
        'B1': (dict(read=colour), dict(fill=THINK_FILL, edge=THINK_EDGE, ink=THINK_INK, read=colour)),
        'B2': (dict(read=dark), dict(fill=THINK_FILL, edge=THINK_EDGE, ink=THINK_INK, read=colour)),
        'B3': (dict(fill=mix(PAPER, colour, 0.22), edge=dark, ink=TALK_INK),
               dict(fill=mix(THINK_FILL, colour, 0.28), edge=colour, ink=THINK_INK)),
    }


NOTES = {
    'B1': 'B1: the chosen colour as it is, as on our message windows.',
    'B2': 'B2: on the white balloon the colour darkened until it reads (4.5:1); on a thought as it is.',
    'B3': 'B3: the ink as it is; the paper tinted with the colour, the rim in it.',
}

player = Image.open(os.path.join(SHOTS, 'battle-speech.png')).convert('RGB')
enemy = Image.open(os.path.join(SHOTS, 'battle-speech-enemy.png')).convert('RGB')
pb, eb = Balloon(player, True), Balloon(enemy, False)
salmon = COLOURS[0][1]
thought = pb.recolour(player, fill=THINK_FILL, edge=THINK_EDGE, ink=THINK_INK)

save(player, 'B0-unread.png', 'B0 - a line not read before', 'As today: the balloon\'s own ink. Every variant leaves an unread line so.')
save(thought, 'B0-thought-unread.png', 'B0 - a thought not read before',
     'The player\'s balloon redrawn as a thought (no capture in the gallery has one).')
for key, (talk, think) in variants(salmon).items():
    save(pb.recolour(player, **talk), f'{key}-speech.png', f'{key} - a line read before (Salmon)', NOTES[key])
    save(eb.recolour(enemy, **talk), f'{key}-speech-enemy.png', f'{key} - an enemy\'s line read before (Salmon)', NOTES[key])
    save(pb.recolour(player, **think), f'{key}-thought.png', f'{key} - a thought read before (Salmon)', NOTES[key])

# The nine colours: a row each, B1 | B2 | B3.
cells = []
for name, colour in COLOURS:
    v = variants(colour)
    cells.append((name, [pb.crop(pb.recolour(player, **v[k][0])) for k in ('B1', 'B2', 'B3')]))
cw, ch = cells[0][1][0].size
scale = 0.62
w, h = round(cw * scale), round(ch * scale)
head, label = 70, 110
sheet = Image.new('RGB', (label + 3 * (w + 12) + 12, head + 9 * (h + 8) + CAPTION), (23, 18, 14))
d = ImageDraw.Draw(sheet)
for i, key in enumerate(('B1', 'B2', 'B3')):
    d.text((label + 12 + i * (w + 12), 20), key, font=CINZEL(24, 700), fill=NAME)
for row, (name, crops) in enumerate(cells):
    y = head + row * (h + 8)
    d.text((12, y + h // 2 - 10), name, font=GOTHIC(17), fill=QUIET)
    for i, c in enumerate(crops):
        sheet.paste(c.resize((w, h), Image.LANCZOS), (label + 12 + i * (w + 12), y))
y = head + 9 * (h + 8)
d.line((0, y, sheet.width, y), fill=GOLD, width=1)
d.text((16, y + 10), 'The nine colours on a line read before', font=CINZEL(20, 700), fill=NAME)
d.text((16, y + 38), 'B1 as they are, B2 darkened on the white paper, B3 the paper tinted.', font=GOTHIC(15), fill=QUIET)
sheet.save(os.path.join(OUT, 'B-colours.png'))
print('Wrote', OUT)
