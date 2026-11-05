"""Mock-ups of the scouting ninja's seal (the owner's K2, docs/ui/enemy-attack.md, §13) on the phone's enemy attack
screen, held landscape (LA2) and upright (PA1), after a scouting (docs/ui/enemy-attack.md, §14). Drawn over the screen
gallery's phone dumps of the screen as built (docs/ui/gallery.md; tools/ui/gallery.ps1 -PhoneLand -PhonePortrait
-Only enemy-attack-scouted: 1808x813 at two thirds of the reference phone's pixels, 610x1356 at half), with Kanami's
CG 9 read from the extract. From the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_attack_scout_phone.py <screen-gallery> <cg_png dir> <out dir>
Round KP: LK1 held landscape, the seal inside the army panel's top right corner on the heading's row, "SCOUTED BY
Kanami" before it; LK2 a larger seal there, the words in two lines before it in the room over the cards; PK1 upright,
the seal over the panel's top right frame as the PC's, the words on the heading's baseline; PK2 upright, the seal inside
the corner. The note "Kanami's report" goes in each. Each full screen, then a sheet of the panel's top right part as it
is and as drawn, enlarged. The images hold the player's CGs: keep <out dir> outside the repository. Drawn at 2x and
scaled down; the dumps' pixels.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

GALLERY, CG_DIR, OUT = sys.argv[1:4]
sys.stdout.reconfigure(encoding='utf-8')
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
S = 2
CAPTION = 64

GOLD, DIM = (196, 160, 86, 255), (110, 86, 46, 255)
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
PORTRAIT = None


def portrait():
    src = Image.open(os.path.join(CG_DIR, 'CG0009.png'))
    pal = src.getpalette()
    for i, rgb in RESERVED.items():
        pal[i * 3:i * 3 + 3] = rgb
    src.putpalette(pal)
    rgba = src.convert('RGBA')
    rgba.putalpha(Image.frombytes('L', src.size, bytes(0 if i == 40 else 255 for i in src.tobytes())))
    return rgba


def face_square(size):
    crop = PORTRAIT.crop((24, 7, 24 + 144, 7 + 144)).convert('RGBa')
    return crop.resize((size, size), Image.LANCZOS).convert('RGBA')


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

    def fill_at(self, at):
        return self.back.getpixel(at)

    def save(self, name, caption, note):
        self.rect((0, self.h, self.w, self.h + CAPTION), fill=(23, 24, 27, 255))
        size = 20 if self.w > 1000 else 14
        self.text((16, self.h + 10), caption, cinzel(size, 700), NAME, anchor='la')
        self.text((16, self.h + 38), note.replace('’', "'"), font(size - 5), QUIET, anchor='la')
        self.im.resize((self.w, self.h + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
        print(name)


def seal(c, r, line=1.5):
    """The PC's seal: the face square on a plate 3 wider in a gold line, the square's own gold line."""
    x0, y0, x1, y1 = r
    pad = 3 * (x1 - x0) / 60
    c.rect((x0 - pad, y0 - pad, x1 + pad, y1 + pad), fill=PLATE, outline=GOLD, width=line)
    c.rect(r, fill=FACE_BACK)
    c.paste(face_square(round((x1 - x0) * S)), x0, y0)
    c.rect(r, outline=GOLD, width=1)


def words(c, right, baseline, label, name, gap):
    """"SCOUTED BY" and the name right-aligned at <right> on <baseline>."""
    c.text((right, baseline), 'Kanami', name, NAME, anchor='rs')
    c.text((right - c.width('Kanami', name) - gap, baseline), 'SCOUTED BY', label, LABEL, anchor='rs')


def clear(c, r, at):
    c.rect(r, fill=c.fill_at(at))


# Held landscape (the dump at two thirds of the reference's pixels): the army panel 530-1183 x 12-800, its heading's
# baseline 47 (Cinzel ~19), the cards from 172, the note at 657.
def lk1(c):
    clear(c, (600, 640, 1170, 676), (900, 700))
    seal(c, (1100, 26, 1166, 92))
    words(c, 1086, 47, cinzel(16), font(23), 10)


def lk2(c):
    clear(c, (600, 640, 1170, 676), (900, 700))
    seal(c, (1056, 26, 1166, 136))
    c.text((1040, 47), 'SCOUTED BY', cinzel(16), LABEL, anchor='rs')
    c.text((1040, 84), 'Kanami', font(28), NAME, anchor='rs')


# Upright (the dump at half the reference's pixels): the army panel 10-601 x 461-982, its heading's baseline 489
# (Cinzel ~11), the bracket's head from ~500 at the middle, the note at 913.
def pk1(c):
    clear(c, (18, 900, 595, 926), (300, 950))
    seal(c, (540, 444, 590, 494), 1)
    words(c, 530, 489, cinzel(10), font(14), 6)


def pk2(c):
    clear(c, (18, 900, 595, 926), (300, 950))
    seal(c, (548, 472, 588, 512), 1)
    words(c, 538, 489, cinzel(10), font(14), 6)


VARIANTS = [
    ('LK1', 'phone-landscape', 'LK1 — held landscape: the seal in the panel’s corner on the heading’s row',
     'A 66 square inside the army panel’s top right corner, “SCOUTED BY Kanami” on the heading’s baseline before it; '
     'the note under the totals gone.', lk1, (820, 0, 1200, 200)),
    ('LK2', 'phone-landscape', 'LK2 — held landscape: a larger seal, the words in two lines',
     'A 110 square in the room over the cards, “SCOUTED BY” on the heading’s row and her name larger under it.',
     lk2, (820, 0, 1200, 200)),
    ('PK1', 'phone-portrait', 'PK1 — upright: the seal over the panel’s frame',
     'As the PC’s: a 50 square over the top right frame, the words on the heading’s baseline.', pk1, (300, 420, 610, 560)),
    ('PK2', 'phone-portrait', 'PK2 — upright: the seal inside the corner',
     'A 40 square inside the corner under the frame, the words on the heading’s baseline.', pk2, (300, 420, 610, 560)),
]


def main():
    global PORTRAIT
    os.makedirs(OUT, exist_ok=True)
    PORTRAIT = portrait()
    rows = []
    for key, folder, caption, note, draw, part in VARIANTS:
        back = Image.open(os.path.join(GALLERY, folder, 'current', 'enemy-attack-scouted.png'))
        rows.append((f'{key}: now', Canvas(back).crop(part)))
        c = Canvas(back)
        draw(c)
        rows.append((key, c.crop(part)))
        c.save(f'{key}.png', caption, note)

    # The changed corners, as built and as drawn, at twice the dump's pixels.
    label, gap, head = 110, 16, 16
    sheet_w = label + max(r.width for _, r in rows)
    sheet = Image.new('RGBA', (sheet_w, head + sum(r.height + gap for _, r in rows)), (14, 10, 8, 255))
    d = ImageDraw.Draw(sheet)
    y = head
    for key, big in rows:
        d.text((12, y + big.height // 2), key, font=cinzel(8, 700), fill=NAME, anchor='lm')
        sheet.alpha_composite(big, (label, y))
        y += big.height + gap
    sheet.convert('RGB').save(os.path.join(OUT, 'KP-corners.png'))
    print('KP-corners.png')


main()
