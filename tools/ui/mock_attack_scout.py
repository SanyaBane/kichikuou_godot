"""Mock-ups of the scouting ninja on the enemy attack screen after a scouting (docs/ui/enemy-attack.md, §13): the owner
finds the line "Kanami's report" under the army's totals too little and wants the ninja who scouted shown by her
portrait. Drawn over the screen gallery's dump of the screen as built (docs/ui/gallery.md;
tools/ui/gallery.ps1 -Only enemy-attack-scouted), 1280x800, with Kanami's CG 9 read from the extract. From the
repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_attack_scout.py <screen-gallery>/pc/current/enemy-attack-scouted.png <cg_png dir> <out dir>
Round K: K1 her face square in the note's row under the totals, the note her name; K2 her face as a seal at the army
panel's heading, "Scouted by Kanami" before it; K3 her face on the Ninja Scout button, which tells who went; K4 her
framed portrait between the panels under "VS", her name on a plate. Each full screen, then a sheet of the part each
changes, at the layout's size and at a window twice that. The images hold the player's CGs: keep <out dir> outside the
repository. Everything is in the 1280x800 screen's pixels (plus a caption band below); drawn at 2x and scaled down.
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
NAME, SUBTITLE, LABEL, ROW = (232, 199, 122, 255), (160, 140, 104, 255), (150, 134, 108, 255), (34, 26, 20, 255)
TEXT, QUIET, FACE_BACK = (244, 232, 206, 255), (176, 160, 130, 255), (52, 40, 28, 255)
OFF_TEXT = (98, 88, 74, 255)
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


# EnemyAttackView.cs as built (the dump's pixels): the army panel, its cards' block, the totals' row and the note.
ARMY = (602, 162, 1248, 562)
CARDS_LEFT, CARDS_RIGHT = 686, 1196
TOTALS_Y, RULE_Y, NOTE_Y = 483, 497, 518
SCOUT_BUTTON = (822, 612, 1012, 704)
GAP = (502, 162, 602, 562)                 # the map between the two panels, "VS" in its middle
KANAMI_CG = 9

# Palette.Initial: the entries no CG loads (ags.c's initPal).
RESERVED = {0: (0, 0, 0), 1: (128, 0, 0), 2: (0, 128, 0), 3: (128, 128, 0), 4: (0, 0, 128), 5: (128, 0, 128),
            6: (0, 128, 128), 7: (192, 192, 192), 8: (192, 220, 192), 9: (166, 202, 240), 246: (255, 251, 240),
            247: (160, 160, 164), 248: (128, 128, 128), 249: (255, 0, 0), 250: (0, 255, 0), 251: (255, 255, 0),
            252: (0, 0, 255), 253: (255, 0, 255), 254: (0, 255, 255), 255: (255, 255, 255)}


def portrait():
    """CG 9 as `G n, 40` draws it: palette index 40 transparent."""
    src = Image.open(os.path.join(CG_DIR, f'CG{KANAMI_CG:04d}.png'))
    pal = src.getpalette()
    for i, rgb in RESERVED.items():
        pal[i * 3:i * 3 + 3] = rgb
    src.putpalette(pal)
    rgba = src.convert('RGBA')
    rgba.putalpha(Image.frombytes('L', src.size, bytes(0 if i == 40 else 255 for i in src.tobytes())))
    return rgba


PORTRAIT = None


def face_square(size):
    """FacePictures.Square: the built-in square (24, 7, 144) shrunk premultiplied."""
    crop = PORTRAIT.crop((24, 7, 24 + 144, 7 + 144)).convert('RGBa')
    return crop.resize((size, size), Image.LANCZOS).convert('RGBA')


def whole_portrait(w, h):
    return PORTRAIT.convert('RGBa').resize((w, h), Image.LANCZOS).convert('RGBA')


class Canvas:
    def __init__(self, back):
        self.back = back.convert('RGBA')
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (14, 10, 8, 255))
        self.im.alpha_composite(self.back.resize((W * S, H * S), Image.LANCZOS), (0, 0))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill, outline=outline, width=round(width * S))

    def text(self, xy, s, f, fill=TEXT, anchor='ls'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=DIM, width=1.0):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=round(width * S))

    def poly(self, pts, fill=None, outline=None):
        self.d.polygon([(x * S, y * S) for x, y in pts], fill=fill, outline=outline)

    def paste(self, image, x, y):
        self.im.alpha_composite(image, (round(x * S), round(y * S)))

    def crop(self, r):
        return self.im.crop(tuple(v * S for v in r))

    def panel_fill(self, at):
        return self.back.getpixel(at)

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=DIM)
        self.text((16, H + 10), caption, cinzel(20, 700), fill=NAME, anchor='la')
        self.text((16, H + 38), note.replace('’', "'"), font(15), fill=QUIET, anchor='la')
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
        print(name)


def face(c, r):
    """A face square as the army's cards draw it: FaceBack, the square, a gold line round it."""
    x0, y0, x1, y1 = r
    c.rect(r, fill=FACE_BACK)
    c.paste(face_square(round((x1 - x0) * S)), x0, y0)
    c.rect(r, outline=GOLD, width=1)


def plate(c, r, name, size=13):
    """A speaker's plate (message window 131's): lacquer in a gold line, a dim one inside, the name in gold capitals."""
    x0, y0, x1, y1 = r
    c.rect(r, fill=PLATE, outline=GOLD, width=1.5)
    c.rect((x0 + 3, y0 + 3, x1 - 3, y1 - 3), outline=DIM, width=1)
    for cx, cy in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
        c.poly([(cx, cy - 3), (cx + 3, cy), (cx, cy + 3), (cx - 3, cy)], fill=GOLD)
    c.text(((x0 + x1) / 2, (y0 + y1) / 2 + 1), name, cinzel(size, 700), fill=NAME, anchor='mm')


def clear_note(c):
    """The note's row painted over with the panel's own colour."""
    fill = c.panel_fill((900, 540))
    c.rect((CARDS_LEFT - 2, RULE_Y + 3, CARDS_RIGHT + 2, ARMY[3] - 6), fill=fill)


def k1(c):
    # The note's row: her face square 44 at the cards' left, her name in gold, what she did quieter beside it.
    clear_note(c)
    face(c, (CARDS_LEFT, RULE_Y + 8, CARDS_LEFT + 46, RULE_Y + 54))
    x = CARDS_LEFT + 58
    c.text((x, RULE_Y + 25), 'SCOUTED BY', cinzel(11), fill=LABEL)
    c.text((x, RULE_Y + 47), 'Kanami', font(18), fill=NAME)
    c.text((x + c.width('Kanami', font(18)) + 12, RULE_Y + 47), 'reported the army this week', font(13), fill=QUIET)


def k2(c):
    # The heading's right end: "Scouted by Kanami" and her face as a seal across the panel's top line.
    clear_note(c)
    fill = c.panel_fill((900, 540))
    # Shorten the heading's rule; the seal sits at the panel's top right, over its frame.
    seal = (1166, 150, 1226, 210)
    c.rect((930, 172, 1240, 194), fill=fill)
    words = 1154 - c.width('Kanami', font(15)) - 8
    c.text((1154, 189), 'Kanami', font(15), fill=NAME, anchor='rs')
    c.text((words, 189), 'SCOUTED BY', cinzel(11), fill=LABEL, anchor='rs')
    c.line((930, 183), (words - c.width('SCOUTED BY', cinzel(11)) - 14, 183), fill=DIM)
    c.rect((seal[0] - 3, seal[1] - 3, seal[2] + 3, seal[3] + 3), fill=PLATE, outline=GOLD, width=1.5)
    face(c, seal)


def k3(c):
    # The Ninja Scout button, spent: her face and "Kanami scouted", the ninjas left under it.
    x0, y0, x1, y1 = SCOUT_BUTTON
    fill = c.panel_fill((x0 + 6, y0 + 6))
    c.rect((x0 + 2, y0 + 2, x1 - 2, y1 - 2), fill=fill)
    face(c, (x0 + 14, y0 + 18, x0 + 70, y0 + 74))
    tx = x0 + 82
    c.text((tx, y0 + 34), 'NINJA SCOUT', cinzel(12, 700), fill=SUBTITLE)
    c.text((tx, y0 + 58), 'Kanami went', font(15), fill=NAME)
    c.text((tx, y0 + 78), '0 of 1 free', font(13), fill=OFF_TEXT)
    # The note in the army panel names her too, as built.


def k4(c):
    # Between the panels: "VS" raised, her portrait framed under it, her name on a plate, "reported" under the plate.
    x0, y0, x1, y1 = GAP
    vs = c.back.crop((520, 335, 590, 385))
    # Cover the built "VS" with the map under it (taken just above), then draw it higher.
    patch = c.back.crop((520, 280, 590, 330))
    c.paste(patch.resize((70 * S, 50 * S)), 520, 335)
    c.paste(vs.resize((70 * S, 50 * S), Image.LANCZOS), 520, 250)
    pw, ph = 84, 112
    px, py = (x0 + x1) / 2 - pw / 2, 318
    c.rect((px - 4, py - 4, px + pw + 4, py + ph + 4), fill=PLATE, outline=GOLD, width=1.5)
    c.paste(whole_portrait(pw * S, ph * S), px, py)
    c.rect((px, py, px + pw, py + ph), outline=DIM, width=1)
    plate(c, (px - 8, py + ph + 10, px + pw + 8, py + ph + 32), 'KANAMI', 12)


VARIANTS = [
    ('K1', 'K1 — her face square in the note’s row', 'Under the totals, where “Kanami’s report” is: the square 46×46 at the '
     'cards’ left, “Scouted by Kanami”, what she did quieter.', k1, (602, 440, 1248, 562)),
    ('K2', 'K2 — her face as a seal at the panel’s heading', '“Scouted by Kanami” at the heading’s right end, her square '
     'set over the panel’s top frame as a seal; the note row empty.', k2, (900, 140, 1248, 262)),
    ('K3', 'K3 — her face on the Ninja Scout button', 'The button tells who went: her square, “Kanami went”, the ninjas '
     'left; the note under the totals as built.', k3, (790, 590, 1040, 726)),
    ('K4', 'K4 — her portrait between the panels, under “VS”', 'Her whole 192×256 portrait at 84×112 in a gold frame, '
     'her name on a speaker’s plate under it; the note under the totals as built.', k4, (490, 230, 700, 520)),
]


def main():
    global PORTRAIT
    os.makedirs(OUT, exist_ok=True)
    PORTRAIT = portrait()
    back = Image.open(DUMP)
    rows = []
    for key, caption, note, draw, part in VARIANTS:
        now = Canvas(back)
        rows.append((f'{key}: now', now.crop(part)))
        c = Canvas(back)
        draw(c)
        rows.append((key, c.crop(part)))
        c.save(f'{key}.png', caption, note)

    # The changed parts, as built and as drawn: at the layout's size and at a window twice that.
    label, gap, head = 110, 16, 40
    lw = max(r.width for _, r in rows) // S
    sheet_w = label + lw * 3 + gap * 3
    heights = [r.height for _, r in rows]
    sheet = Image.new('RGBA', (sheet_w, head + sum(heights) + gap * len(rows)), (14, 10, 8, 255))
    d = ImageDraw.Draw(sheet)

    def say(xy, s, f, fill=NAME, anchor='la'):
        d.text(xy, s, font=f.font_variant(size=round(f.size / S)), fill=fill, anchor=anchor)

    say((label, 12), 'window 1280×800', cinzel(16, 700))
    say((label + lw + gap, 12), 'window 2560×1600', cinzel(16, 700))
    y = head
    for key, big in rows:
        say((12, y + big.height // 4), key, cinzel(16, 700), anchor='lm')
        sheet.alpha_composite(big.resize((big.width // S, big.height // S), Image.LANCZOS), (label, y))
        sheet.alpha_composite(big, (label + lw + gap, y))
        y += big.height + gap
    sheet.convert('RGB').save(os.path.join(OUT, 'K-parts.png'))
    print('K-parts.png')


main()
