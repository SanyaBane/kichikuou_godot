"""Mock-ups of PK2 with a larger square (docs/ui/enemy-attack.md, §15): upright, the scouting ninja's seal inside the
army panel's top right corner under the frame, "SCOUTED BY Kanami" on the heading's baseline before it. Drawn over the
screen gallery's upright dump of the screen as built with PK1 (tools/ui/gallery.ps1 -PhonePortrait
-Only enemy-attack-scouted, 610x1356 at half the reference phone's pixels): PK1's seal and words are painted over first
(the backdrop and the panel's frame taken from further left on the same rows, the words' room in the panel's colour).
Kanami's CG 9 from the extract. From the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_attack_scout_pk2.py <screen-gallery>/phone-portrait/current/enemy-attack-scouted.png <cg_png dir> <out dir>
Round PK2: PK2a a 50 square (PK1's size), PK2b 56, PK2c 60 (the most the corner holds over the bracket's line), the
dump's pixels (twice that in the reference's). Each full screen, then a sheet of the corner as built (PK1) and as each
variant, enlarged. The images hold the player's CGs: keep <out dir> outside the repository. Drawn at 2x, scaled down.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

DUMP, CG_DIR, OUT = sys.argv[1:4]
sys.stdout.reconfigure(encoding='utf-8')
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
S = 2
CAPTION = 64

GOLD = (196, 160, 86, 255)
NAME, LABEL, QUIET = (232, 199, 122, 255), (150, 134, 108, 255), (176, 160, 130, 255)
FACE_BACK, PLATE = (52, 40, 28, 255), (22, 16, 12, 255)
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


def cinzel(size, weight=600):
    return font(size, CINZEL, weight)


RESERVED = {0: (0, 0, 0), 1: (128, 0, 0), 2: (0, 128, 0), 3: (128, 128, 0), 4: (0, 0, 128), 5: (128, 0, 128),
            6: (0, 128, 128), 7: (192, 192, 192), 8: (192, 220, 192), 9: (166, 202, 240), 246: (255, 251, 240),
            247: (160, 160, 164), 248: (128, 128, 128), 249: (255, 0, 0), 250: (0, 255, 0), 251: (255, 255, 0),
            252: (0, 0, 255), 253: (255, 0, 255), 254: (0, 255, 255), 255: (255, 255, 255)}


def portrait():
    src = Image.open(os.path.join(CG_DIR, 'CG0009.png'))
    pal = src.getpalette()
    for i, rgb in RESERVED.items():
        pal[i * 3:i * 3 + 3] = rgb
    src.putpalette(pal)
    rgba = src.convert('RGBA')
    rgba.putalpha(Image.frombytes('L', src.size, bytes(0 if i == 40 else 255 for i in src.tobytes())))
    return rgba


PORTRAIT = None

# The dump as built with PK1: the army panel's frame top at 462, its right at 600, the heading's baseline 489, the
# bracket's line over the front row at 537; PK1's seal over 538-594 x 441-497, its words 417-530.
HEADING = 489
PANEL_RIGHT = 600


class Canvas:
    def __init__(self, back):
        self.back = back.convert('RGBA')
        self.w, self.h = back.size
        self.im = Image.new('RGBA', (self.w * S, (self.h + CAPTION) * S), (14, 10, 8, 255))
        self.im.alpha_composite(self.back.resize((self.w * S, self.h * S), Image.LANCZOS), (0, 0))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill, outline=outline, width=round(width * S))

    def text(self, xy, s, f, fill, anchor='ls'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def paste(self, image, x, y):
        self.im.alpha_composite(image, (round(x * S), round(y * S)))

    def crop(self, r):
        return self.im.crop(tuple(v * S for v in r))

    def save(self, name, caption, note):
        self.rect((0, self.h, self.w, self.h + CAPTION), fill=(23, 24, 27, 255))
        self.text((12, self.h + 10), caption, cinzel(14, 700), NAME, anchor='la')
        self.text((12, self.h + 36), note.replace('’', "'"), font(10), QUIET, anchor='la')
        self.im.resize((self.w, self.h + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
        print(name)


def unbuild(c):
    """PK1's seal and words painted over: the rows' backdrop, frame and panel from x 335-401, the words' room filled."""
    src = c.back.crop((335, 436, 401, 500))
    c.paste(src.resize((src.width * S, src.height * S), Image.LANCZOS), 532, 436)
    c.rect((405, 470, 532, 497), fill=c.back.getpixel((450, 520)))
    x, y, r = 598, 465, 3                                      # the frame's corner diamond
    c.d.polygon([((x) * S, (y - r) * S), ((x + r) * S, y * S), (x * S, (y + r) * S), ((x - r) * S, y * S)], fill=GOLD)


def pk2(c, size):
    unbuild(c)
    right, top = PANEL_RIGHT - 14, 470
    seal = (right - size, top, right, top + size)
    pad = 3 * size / 60
    c.rect((seal[0] - pad, seal[1] - pad, seal[2] + pad, seal[3] + pad), fill=PLATE, outline=GOLD, width=1)
    c.rect(seal, fill=FACE_BACK)
    face = PORTRAIT.crop((24, 7, 24 + 144, 7 + 144)).convert('RGBa').resize((size * S, size * S), Image.LANCZOS)
    c.paste(face.convert('RGBA'), seal[0], seal[1])
    c.rect(seal, outline=GOLD, width=1)
    words = seal[0] - 10
    c.text((words, HEADING), 'Kanami', font(14), NAME, anchor='rs')
    c.text((words - c.width('Kanami', font(14)) - 6, HEADING), 'SCOUTED BY', cinzel(10), LABEL, anchor='rs')


VARIANTS = [
    ('PK2a', 50, 'PK2a — the seal inside the corner, 50 (PK1’s size)',
     'Under the panel’s top frame, 14 in from its right; the words on the heading’s baseline 10 left of it.'),
    ('PK2b', 56, 'PK2b — the seal inside the corner, 56',
     'The same place, the square 12% larger; its foot 11 over the bracket’s line.'),
    ('PK2c', 60, 'PK2c — the seal inside the corner, 60',
     'The most the corner holds: its foot 7 over the bracket’s line.'),
]
PART = (330, 425, 610, 560)


def main():
    global PORTRAIT
    os.makedirs(OUT, exist_ok=True)
    PORTRAIT = portrait()
    back = Image.open(DUMP)
    rows = [('PK1 (built)', Canvas(back).crop(PART))]
    for key, size, caption, note in VARIANTS:
        c = Canvas(back)
        pk2(c, size)
        rows.append((key, c.crop(PART)))
        c.save(f'{key}.png', caption, note)
    label, gap = 90, 12
    sheet = Image.new('RGBA', (label + rows[0][1].width, gap + sum(r.height + gap for _, r in rows)), (14, 10, 8, 255))
    d = ImageDraw.Draw(sheet)
    y = gap
    for key, big in rows:
        d.text((10, y + big.height // 2), key, font=cinzel(8, 700), fill=NAME, anchor='lm')
        sheet.alpha_composite(big, (label, y))
        y += big.height + gap
    sheet.convert('RGB').save(os.path.join(OUT, 'PK2-corners.png'))
    print('PK2-corners.png')


main()
