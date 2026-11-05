"""Mock-ups of a larger upright seal (docs/ui/enemy-attack.md, §16): the scouting ninja's face square over the army
panel's top right frame as built (PK2c's size over the frame, §15), larger, the army's block — the bracket with its head,
the rows' captions, the cards and the totals — moved down where the square needs the room (the panel has ~75 to spare
under the totals). Drawn over the screen gallery's upright dump of the screen as built (tools/ui/gallery.ps1
-PhonePortrait -Only enemy-attack-scouted, 610x1356 at half the reference phone's pixels): the built seal and words are
painted over first (the backdrop and the panel's frame taken from further left on the same rows, the words' room in the
panel's colour). Kanami's CG 9 from the extract. From the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_attack_scout_pk3.py <screen-gallery>/phone-portrait/current/enemy-attack-scouted.png <cg_png dir> <out dir>
Round PK3, the dump's pixels (twice that in the reference's): PK3a 72, the block where it is; PK3b 84, the block 12
lower; PK3c 96, the block 24 lower; PK3d 108, the block 36 lower, the words in two lines as held landscape's. Each full
screen, then a sheet of the panel's top as built and as each variant. The images hold the player's CGs: keep <out dir>
outside the repository. Drawn at 2x, scaled down.
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

# The dump as built: the army panel's frame top at 462, its right at 600, the heading's baseline 489; the army's block
# from the bracket's head (498) to the totals' rule (899), the panel's inside 14-596 across; the built seal 532-592 x
# 444-504 on its plate, its words from 405.
HEADING, PANEL_TOP, PANEL_RIGHT = 489, 462, 600
BLOCK = (14, 494, 596, 904)
SEAL_TOP, SEAL_IN = 444, 14


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


def panel_fill(c):
    return c.back.getpixel((450, 940))


def unbuild(c):
    """The built seal and words painted over: the rows' backdrop, frame and panel from x 328-401, the words' room filled."""
    src = c.back.crop((328, 436, 401, 494))
    c.paste(src.resize((src.width * S, src.height * S), Image.LANCZOS), 522, 436)
    c.rect((395, 470, 524, 494), fill=panel_fill(c))
    x, y, r = 598, 465, 3                                      # the frame's corner diamond
    c.d.polygon([(x * S, (y - r) * S), ((x + r) * S, y * S), (x * S, (y + r) * S), ((x - r) * S, y * S)], fill=GOLD)


def lower_block(c, by):
    """The army's block moved down <by>, its old top filled with the panel's colour."""
    if by == 0:
        return
    x0, y0, x1, y1 = BLOCK
    block = c.back.crop(BLOCK)
    c.rect((x0, y0, x1, y0 + by), fill=panel_fill(c))
    c.paste(block.resize((block.width * S, block.height * S), Image.LANCZOS), x0, y0 + by)


def seal(c, size, two_lines=False):
    right = PANEL_RIGHT - SEAL_IN
    box = (right - size, SEAL_TOP, right, SEAL_TOP + size)
    pad = 3 * size / 60
    c.rect((box[0] - pad, box[1] - pad, box[2] + pad, box[3] + pad), fill=PLATE, outline=GOLD, width=1)
    c.rect(box, fill=FACE_BACK)
    face = PORTRAIT.crop((24, 7, 24 + 144, 7 + 144)).convert('RGBa').resize((size * S, size * S), Image.LANCZOS)
    c.paste(face.convert('RGBA'), box[0], box[1])
    c.rect(box, outline=GOLD, width=1)
    words = box[0] - 10
    if two_lines:
        c.text((words, HEADING), 'SCOUTED BY', cinzel(10), LABEL, anchor='rs')
        c.text((words, HEADING + 24), 'Kanami', font(18), NAME, anchor='rs')
        return
    c.text((words, HEADING), 'Kanami', font(14), NAME, anchor='rs')
    c.text((words - c.width('Kanami', font(14)) - 6, HEADING), 'SCOUTED BY', cinzel(10), LABEL, anchor='rs')


VARIANTS = [
    ('PK3a', 72, 0, False, 'PK3a — the seal 72, the block where it is',
     'Over the top frame as built, 14 in from the right; its foot 21 over the bracket’s line.'),
    ('PK3b', 84, 12, False, 'PK3b — the seal 84, the block 12 lower',
     'The bracket, the cards and the totals 12 lower; the room under the totals 63.'),
    ('PK3c', 96, 24, False, 'PK3c — the seal 96, the block 24 lower',
     'The block 24 lower; the room under the totals 51.'),
    ('PK3d', 108, 36, True, 'PK3d — the seal 108, the block 36 lower, the words in two lines',
     'As held landscape: “SCOUTED BY” on the heading’s line, the name larger under it; the room under the totals 39.'),
]
PART = (0, 420, 610, 1000)


def main():
    global PORTRAIT
    os.makedirs(OUT, exist_ok=True)
    PORTRAIT = portrait()
    back = Image.open(DUMP)
    rows = [('built', Canvas(back).crop(PART))]
    for key, size, by, two, caption, note in VARIANTS:
        c = Canvas(back)
        unbuild(c)
        lower_block(c, by)
        seal(c, size, two)
        rows.append((key, c.crop(PART)))
        c.save(f'{key}.png', caption, note)
    # The panel's top, as built and as each variant, side by side at the dump's pixels.
    gap, head = 12, 28
    w = rows[0][1].width // S
    h = rows[0][1].height // S
    sheet = Image.new('RGBA', (gap + len(rows) * (w + gap), head + h + gap), (14, 10, 8, 255))
    d = ImageDraw.Draw(sheet)
    for i, (key, big) in enumerate(rows):
        x = gap + i * (w + gap)
        d.text((x, 8), key, font=cinzel(8, 700), fill=NAME, anchor='la')
        sheet.alpha_composite(big.resize((w, h), Image.LANCZOS), (x, head))
    sheet.convert('RGB').save(os.path.join(OUT, 'PK3-panels.png'))
    print('PK3-panels.png')


main()
