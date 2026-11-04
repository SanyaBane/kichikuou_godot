"""Mock-ups of Maris on the enemy attack's foot as her face square (docs/ui/enemy-attack.md, §11): the miniature our
cards show (UnitFaces, Debug -> Portraits) instead of the top of her 192x256 portrait squeezed into 122x120. Drawn over
the screen gallery's dump of the screen as built (docs/ui/gallery.md; tools/ui/gallery.ps1 -Only enemy-attack),
1280x800, with her CG read from the extract. From the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_attack_advisor.py <screen-gallery>/pc/current/enemy-attack.png <cg_png dir> <out dir>
Round M: M1 the square in the portrait's place, 120x120; M2 the square at the army's cards' face size (88), the line
moved left; M3 a 100 square with her name on a plate under it, the line given the foot's height; M4 M2 with the line in
a speech box whose tail points at her, the name on a plate across its top as message window 131's speaker. Each full
screen, then one sheet of the foot's left part as it is and as M1-M4, at the layout's size and at a window twice that
(the face square is 144 of the CG's pixels: 120 on the layout is a reduction, 240 on a 2560x1600 window an
enlargement). The images hold the player's CGs: keep <out dir> outside the repository. Everything is in the 1280x800
screen's pixels (plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

DUMP, CG_DIR, OUT = sys.argv[1:4]
sys.stdout.reconfigure(encoding='utf-8')
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
S = 2
W, H, CAPTION = 1280, 800, 64

# UiPalette.cs, Lacquer
GOLD, DIM = (196, 160, 86, 255), (110, 86, 46, 255)
NAME, SUBTITLE, ROW = (232, 199, 122, 255), (160, 140, 104, 255), (34, 26, 20, 255)
TEXT, QUIET, FACE_BACK = (244, 232, 206, 255), (176, 160, 130, 255), (52, 40, 28, 255)
PLATE = (30, 22, 16, 255)

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


# EnemyAttackView.cs: FootTop = 584; AdvisorArea, the name and the line as built.
FOOT = (32, 584, 1248, 732)
LEFT = (32, 584, 540, 732)
ADVISOR = (50, 598, 172, 718)
MARIS_CG = 6
LINES = ['King Rance, if we don\'t act,','Ys will be', 'occupied.', 'What should we do?']

# Palette.Initial: the entries no CG loads (ags.c's initPal).
RESERVED = {0: (0, 0, 0), 1: (128, 0, 0), 2: (0, 128, 0), 3: (128, 128, 0), 4: (0, 0, 128), 5: (128, 0, 128),
            6: (0, 128, 128), 7: (192, 192, 192), 8: (192, 220, 192), 9: (166, 202, 240), 246: (255, 251, 240),
            247: (160, 160, 164), 248: (128, 128, 128), 249: (255, 0, 0), 250: (0, 255, 0), 251: (255, 255, 0),
            252: (0, 0, 255), 253: (255, 0, 255), 254: (0, 255, 255), 255: (255, 255, 255)}


def portrait():
    """CG 6 as `G n, 40` draws it: palette index 40 transparent (its colour, magenta, kept under alpha 0)."""
    src = Image.open(os.path.join(CG_DIR, f'CG{MARIS_CG:04d}.png'))
    pal = src.getpalette()
    for i, rgb in RESERVED.items():
        pal[i * 3:i * 3 + 3] = rgb
    src.putpalette(pal)
    rgba = src.convert('RGBA')
    rgba.putalpha(Image.frombytes('L', src.size, bytes(0 if i == 40 else 255 for i in src.tobytes())))
    return rgba


PORTRAIT = None


def face_square(size):
    """FacePictures.Square: the built-in square (24, 7, 144) shrunk (or grown) premultiplied, so no magenta bleeds in."""
    crop = PORTRAIT.crop((24, 7, 24 + 144, 7 + 144)).convert('RGBa')
    return crop.resize((size, size), Image.LANCZOS).convert('RGBA')


def portrait_top(w, h):
    """The screen as built: the portrait's top 192x190 drawn into the advisor's area with the texture's linear filter,
    not premultiplied — the colour key's magenta bleeds into the transparent checker, which then reads purple."""
    top = PORTRAIT.crop((0, 0, 192, 190))
    rgb = top.convert('RGB').resize((w, h), Image.BILINEAR)
    a = top.getchannel('A').resize((w, h), Image.BILINEAR)
    rgb.putalpha(a)
    return rgb


class Canvas:
    def __init__(self, back):
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (14, 10, 8, 255))
        self.im.alpha_composite(back.convert('RGBA').resize((W * S, H * S), Image.LANCZOS), (0, 0))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill, outline=outline, width=round(width * S))

    def text(self, xy, s, f, fill=TEXT, anchor='ls'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def line(self, a, b, fill=DIM, width=1.0):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=round(width * S))

    def poly(self, pts, fill=None, outline=None):
        self.d.polygon([(x * S, y * S) for x, y in pts], fill=fill, outline=outline)

    def paste(self, image, x, y):
        self.im.alpha_composite(image, (round(x * S), round(y * S)))

    def crop(self, r):
        return self.im.crop(tuple(v * S for v in r))

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=DIM)
        self.text((16, H + 10), caption, cinzel(20, 700), fill=NAME, anchor='la')
        self.text((16, H + 38), note.replace('’', "'"), font(15), fill=QUIET, anchor='la')
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
        print(name)


def clear(c, back):
    """The foot's left part painted over with the panel's own colour, as the dump shows it right of the line."""
    fill = back.convert('RGBA').getpixel((530, 650))
    c.rect((40, 592, 536, 725), fill=fill)


def face(c, r):
    """A face square as the army's cards draw it: FaceBack, the square, a gold line round it."""
    x0, y0, x1, y1 = r
    c.rect(r, fill=FACE_BACK)
    c.paste(face_square(round((x1 - x0) * S)), x0, y0)
    c.rect(r, outline=GOLD, width=1)


def name_and_lines(c, x, name_y=614, first=642, step=24):
    c.text((x, name_y), 'MARIS', cinzel(13), fill=SUBTITLE)
    for i, s in enumerate(LINES):
        c.text((x, first + i * step), s, font(18))


def plate(c, r, size=14):
    """A speaker's plate (message window 131's): lacquer in a gold line, a dim one inside, the name in gold capitals."""
    x0, y0, x1, y1 = r
    c.rect(r, fill=PLATE, outline=GOLD, width=1.5)
    c.rect((x0 + 3, y0 + 3, x1 - 3, y1 - 3), outline=DIM, width=1)
    for cx, cy in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
        c.poly([(cx, cy - 3), (cx + 3, cy), (cx, cy + 3), (cx - 3, cy)], fill=GOLD)
    c.text(((x0 + x1) / 2, (y0 + y1) / 2 + 1), 'MARIS', cinzel(size, 700), fill=NAME, anchor='mm')


def now(c):
    x0, y0, x1, y1 = ADVISOR
    c.paste(portrait_top((x1 - x0) * S, (y1 - y0) * S), x0, y0)
    c.rect(ADVISOR, outline=DIM, width=1)
    name_and_lines(c, 188)


def m1(c):
    face(c, (51, 598, 171, 718))
    name_and_lines(c, 188)


def m2(c):
    face(c, (50, 614, 138, 702))
    name_and_lines(c, 156)


def m3(c):
    face(c, (56, 596, 156, 696))
    plate(c, (50, 700, 162, 722))
    for i, s in enumerate(LINES):
        c.text((180, 627 + i * 24), s, font(18))


def m4(c):
    face(c, (50, 614, 138, 702))
    box = (160, 606, 528, 722)
    c.rect(box, fill=ROW, outline=DIM, width=1)
    # The tail, from the box's left side to the face: drawn over the box's line so the two read as one shape.
    c.poly([(160.5, 640), (144, 654), (160.5, 666)], fill=ROW)
    c.line((160, 640), (144, 654), fill=DIM)
    c.line((144, 654), (160, 666), fill=DIM)
    plate(c, (172, 595, 256, 617), 13)
    for i, s in enumerate(LINES):
        c.text((176, 640 + i * 23), s, font(18))


VARIANTS = [
    ('M1', 'M1 — the face square in the portrait’s place, 120×120',
     'Maris as the army’s cards show a face (Debug → Portraits’ square), as large as the portrait was; '
     'the name and the line where they are.', m1),
    ('M2', 'M2 — the face square at the cards’ size, 88×88',
     'The same size as the commanders’ faces on the army’s cards; the line moves 32 left.', m2),
    ('M3', 'M3 — a 100 square with her name on a plate under it',
     'The name leaves the line’s heading for a speaker’s plate (message window 131’s); the line gets '
     'the foot’s height.', m3),
    ('M4', 'M4 — M2 speaking: the line in a box whose tail points at her',
     'Our message windows’ manner: the name on a plate across the box’s top, as 131’s speaker.', m4),
]


def main():
    global PORTRAIT
    os.makedirs(OUT, exist_ok=True)
    PORTRAIT = portrait()
    back = Image.open(DUMP)
    rows = []
    c = Canvas(back)
    clear(c, back)
    now(c)
    rows.append(('now', c.crop(LEFT)))
    for key, caption, note, draw in VARIANTS:
        c = Canvas(back)
        clear(c, back)
        draw(c)
        rows.append((key, c.crop(LEFT)))
        c.save(f'{key}.png', caption, note)

    # The foot's left part: the layout's size (the full screens' scale) and a window twice that (the canvas as drawn).
    lw, lh = LEFT[2] - LEFT[0], LEFT[3] - LEFT[1]
    label, gap, head = 64, 16, 40
    sheet = Image.new('RGBA', (label + lw + lw * 2 + gap * 3, head + (lh * 2 + gap) * len(rows)), (14, 10, 8, 255))
    d = ImageDraw.Draw(sheet)

    def say(xy, s, f, fill=NAME, anchor='la'):
        d.text(xy, s, font=f.font_variant(size=round(f.size / S)), fill=fill, anchor=anchor)

    say((label, 12), 'window 1280×800', cinzel(16, 700))
    say((label + lw + gap, 12), 'window 2560×1600 (the face square enlarged from 144 to 240 in M1)', cinzel(16, 700))
    for i, (key, big) in enumerate(rows):
        y = head + i * (lh * 2 + gap)
        say((12, y + lh), key, cinzel(18, 700), anchor='lm')
        sheet.alpha_composite(big.resize((lw, lh), Image.LANCZOS), (label, y))
        sheet.alpha_composite(big, (label + lw + gap, y))
    sheet.convert('RGB').save(os.path.join(OUT, 'M-feet.png'))
    print('M-feet.png')


main()
