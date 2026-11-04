"""Mock-ups of the enemy attack's three buttons in colour (docs/ui/enemy-attack.md, §10): Abandon City no longer a
neutral row box — drawn over the screen gallery's dump of the screen as built (Ninja Scout already as tall as
Intercept). The dump is the gallery's "enemy-attack" entry (docs/ui/gallery.md; tools/ui/gallery.ps1 -Only
enemy-attack), 1280x800. From the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_attack_buttons.py <screen-gallery>/pc/current/enemy-attack.png <out dir>
Round P: P1 Abandon City as the danger button (dark red lacquer in vermilion, a white flag before its label), Intercept
red as it is; P2 the same with Abandon City as tall as the others; P3 Abandon City red and Intercept gold; P4 Abandon
City red and Intercept in the player's blue. Each full screen, then one sheet of the foot as it is and hovered.
The images hold the player's CGs: keep <out dir> outside the repository. Everything is in the 1280x800 screen's pixels
(plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

DUMP, OUT = sys.argv[1:3]
sys.stdout.reconfigure(encoding='utf-8')
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
S = 2
W, H, CAPTION = 1280, 800, 64

# UiPalette.cs, Lacquer
GOLD, DIM = (196, 160, 86, 255), (110, 86, 46, 255)
NAME, ROW, HOVER = (232, 199, 122, 255), (34, 26, 20, 255), (64, 48, 28, 255)
HOVER_LINE, TEXT, QUIET = (222, 186, 104, 255), (244, 232, 206, 255), (176, 160, 130, 255)
VERMILION = (170, 40, 28, 255)
DANGER_FILL, DANGER_HOVER, DANGER_LINE = (52, 20, 14, 255), (80, 28, 18, 255), (110, 40, 28, 255)
DANGER_TEXT = (236, 132, 104, 255)
RED_SUB = (236, 206, 160, 255)

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


def cinzel(size, weight=600):
    return font(size, CINZEL, weight)


def brush(size):
    return font(size, SHOJUMARU)


# EnemyAttackView.cs: FootTop = 584
FOOT = (32, 584, 1248, 732)
INTERCEPT = (548, 612, 800, 704)
SCOUT = (822, 612, 1012, 704)
ABANDON = (1030, 628, 1226, 680)
ABANDON_TALL = (1030, 612, 1226, 704)


class Canvas:
    def __init__(self, back):
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (14, 10, 8, 255))
        self.im.alpha_composite(back.convert('RGBA').resize((W * S, H * S), Image.LANCZOS), (0, 0))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill, outline=outline, width=round(width * S))

    def text(self, xy, s, f, fill=TEXT, anchor='la'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=DIM, width=1.0):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=round(width * S))

    def poly(self, pts, fill=None):
        self.d.polygon([(x * S, y * S) for x, y in pts], fill=fill)

    def gradient(self, r, top, bottom):
        x0, y0, x1, y1 = r
        for i in range(round((y1 - y0) * S)):
            t = i / max(1, (y1 - y0) * S - 1)
            colour = tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,)
            self.d.line([x0 * S, y0 * S + i, x1 * S - 1, y0 * S + i], fill=colour)

    def foot(self):
        return self.im.crop(tuple(v * S for v in FOOT))

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=DIM)
        self.text((16, H + 10), caption, cinzel(20, 700), fill=NAME)
        self.text((16, H + 38), note, font(15), fill=QUIET)
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
        print(name)


def centred(c, r, title, f, colour, at=0.5):
    x0, y0, x1, y1 = r
    c.text(((x0 + x1) / 2, y0 + (y1 - y0) * at), title, f, fill=colour, anchor='mm')


def lacquer_button(c, r, top, bottom, frame, inner, title, title_font, title_colour, sub=None, sub_colour=None):
    """UiPalette.DrawRedButton's build: a lacquer gradient, an outer frame and an inner line, the title at 0.40."""
    x0, y0, x1, y1 = r
    c.gradient(r, top, bottom)
    c.rect(r, outline=frame, width=2)
    c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=inner, width=1)
    if sub:
        centred(c, r, title, title_font, title_colour, 0.40)
        centred(c, r, sub, font(13), sub_colour, 0.76)
    else:
        centred(c, r, title, title_font, title_colour)


def red(c, r, title, sub=None, hover=False, size=22):
    top, bottom = ((150, 40, 30), (96, 20, 14)) if hover else ((128, 32, 24), (80, 16, 12))
    lacquer_button(c, r, top, bottom, HOVER_LINE if hover else GOLD, (150, 96, 52, 255), title, brush(size), NAME,
                   sub, RED_SUB)


def gold(c, r, title, sub=None, hover=False):
    """Gold leaf in a bright frame, the ink dark: the committing button turned gold."""
    top, bottom = ((226, 190, 112), (170, 128, 62)) if hover else ((206, 168, 92), (146, 108, 48))
    lacquer_button(c, r, top, bottom, (250, 224, 160, 255) if hover else NAME, (120, 86, 40, 255), title, brush(22),
                   (52, 28, 12, 255), sub, (74, 44, 20, 255))


def blue(c, r, title, sub=None, hover=False):
    """The player's blue lacquer in gold."""
    top, bottom = ((44, 82, 146), (24, 46, 92)) if hover else ((34, 66, 122), (18, 36, 74))
    lacquer_button(c, r, top, bottom, HOVER_LINE if hover else GOLD, (92, 112, 150, 255), title, brush(22), NAME,
                   sub, (196, 210, 236, 255))


def row(c, r, title, sub=None, hover=False):
    """UiPalette.DrawRowBox with EnemyAttackView's labels."""
    c.rect(r, fill=HOVER if hover else ROW, outline=HOVER_LINE if hover else DIM, width=2 if hover else 1)
    if sub:
        centred(c, r, title, cinzel(17), NAME if hover else TEXT, 0.40)
        centred(c, r, sub, font(13), QUIET, 0.76)
    else:
        centred(c, r, title, cinzel(17), NAME if hover else TEXT)


def white_flag(c, x, y, colour):
    """A white flag on its pole, 16 wide and 18 high, its top left at (x, y): giving the city up."""
    c.line((x + 2, y), (x + 2, y + 18), fill=colour, width=1.6)
    c.poly([(x + 3, y + 1), (x + 9, y + 3), (x + 16, y + 1), (x + 16, y + 10), (x + 9, y + 12), (x + 3, y + 10)],
           fill=colour)


def danger(c, r, title, sub=None, hover=False):
    """SaveLoadView.DrawDelete's look: dark red lacquer in a vermilion frame, an icon before the label."""
    x0, y0, x1, y1 = r
    c.rect(r, fill=DANGER_HOVER if hover else DANGER_FILL)
    c.rect(r, outline=VERMILION, width=2)
    c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=DANGER_LINE, width=1)
    colour = NAME if hover else DANGER_TEXT
    f = cinzel(17)
    w = c.width(title, f) + 24
    cx, at = (x0 + x1) / 2, 0.40 if sub else 0.5
    cy = y0 + (y1 - y0) * at
    white_flag(c, cx - w / 2, cy - 10, TEXT)
    c.text((cx - w / 2 + 24, cy), title, f, fill=colour, anchor='lm')
    if sub:
        centred(c, r, sub, font(13), (200, 120, 96, 255) if not hover else NAME, 0.76)


def clear(c, back):
    """The foot's buttons painted over with the panel's own colour, as the dump shows it left of Intercept."""
    fill = back.convert('RGBA').getpixel((530, 650))
    c.rect((540, 604, 1238, 712), fill=fill)


def variant(back, intercept, abandon, hover):
    c = Canvas(back)
    clear(c, back)
    intercept(c, hover == 'intercept')
    row(c, SCOUT, 'Ninja Scout', '1 of 1 free', hover == 'scout')
    abandon(c, hover == 'abandon')
    return c


VARIANTS = [
    ('P1', 'P1 — Abandon City as the danger button (Delete\'s look, a white flag), Intercept red',
     'The style\'s two reds: red lacquer in gold is the committing action, dark red in vermilion is losing something.',
     lambda c, h: red(c, INTERCEPT, 'Intercept', 'choose the units', h),
     lambda c, h: danger(c, ABANDON, 'Abandon City', hover=h)),
    ('P2', 'P2 — P1 with Abandon City as tall as the others',
     'Three buttons of one height; Abandon City says what it costs under its name.',
     lambda c, h: red(c, INTERCEPT, 'Intercept', 'choose the units', h),
     lambda c, h: danger(c, ABANDON_TALL, 'Abandon City', 'no battle, the city is lost', h)),
    ('P3', 'P3 — Abandon City red, Intercept gold',
     'The owner\'s idea: red for giving up; Intercept in gold leaf with dark ink.',
     lambda c, h: gold(c, INTERCEPT, 'Intercept', 'choose the units', h),
     lambda c, h: red(c, ABANDON, 'Abandon City', hover=h, size=17)),
    ('P4', 'P4 — Abandon City red, Intercept in the player\'s blue',
     'Our army goes out in our colour, the city goes to the enemy in his (the style\'s player blue, enemy red).',
     lambda c, h: blue(c, INTERCEPT, 'Intercept', 'choose the units', h),
     lambda c, h: red(c, ABANDON, 'Abandon City', hover=h, size=17)),
]


def main():
    os.makedirs(OUT, exist_ok=True)
    back = Image.open(DUMP)
    feet = []
    for key, caption, note, intercept, abandon in VARIANTS:
        variant(back, intercept, abandon, None).save(f'{key}.png', caption, note)
        plain = variant(back, intercept, abandon, None).foot()
        lit = variant(back, intercept, abandon, 'abandon').foot()
        feet.append((key, plain, lit))
    now = Canvas(back)
    feet.insert(0, ('now', now.foot(), None))

    fw, fh = (FOOT[2] - FOOT[0]) * S, (FOOT[3] - FOOT[1]) * S
    label, gap = 56 * S, 10 * S
    sheet = Image.new('RGBA', (label + fw * 2 + gap * 3, (fh + gap) * len(feet) + gap + 40 * S), (14, 10, 8, 255))
    d = ImageDraw.Draw(sheet)
    d.text((label + gap, gap), 'as it is', font=cinzel(16, 700), fill=NAME)
    d.text((label + fw + gap * 2, gap), 'Abandon City hovered', font=cinzel(16, 700), fill=NAME)
    for i, (key, plain, lit) in enumerate(feet):
        y = 40 * S + i * (fh + gap)
        d.text((gap, y + fh // 2), key, font=cinzel(18, 700), fill=NAME, anchor='lm')
        sheet.alpha_composite(plain, (label + gap, y))
        if lit:
            sheet.alpha_composite(lit, (label + fw + gap * 2, y))
    sheet.resize((sheet.size[0] // S, sheet.size[1] // S), Image.LANCZOS).convert('RGB').save(
        os.path.join(OUT, 'P-feet.png'))
    print('P-feet.png')


main()
