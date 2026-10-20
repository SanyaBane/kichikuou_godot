"""Mock-ups of a defence on our battle screen (owner, 2026-10-21: the "DEF +n" badge is hard to read and the sentence's
plate above the card covers the card above it, or a speech): D1-D5 drawn over screen dumps of the week-18 save's battle
(Barres (unit 6) defends in round 1) with our badge and plate left out.

The dumps: godot/Battles/BattleInfoLayer.cs's DrawDefence skipped for the run (a local edit, not committed), then
  <Godot console exe> --path godot -- --load --auto --quit --seed 2 --saves user://saves-look-bare
      --slots-from user://saves-week18 --until round-2 --interface new --shots <bare dir>
(00092-effect-guard-player.png: the Guard effect; 00093-info-defence.png: the defence's wait), and the same run without
the edit for the screen as it is now (<now dir>). Then, from the repository's root (Pillow; godot/fonts):
  python -I tools/ui/mock_defend.py <bare dir> <now dir> <out dir> < /dev/null
The images hold the player's portraits: keep <out dir> outside the repository.
Everything is in the 1280x800 screen's pixels; drawn at 2x and scaled down for smooth edges.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

BARE, NOW, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2

ACCENT = (242, 199, 92)                 # BattleInfoLayer.Accent
STEEL_TOP, STEEL_FOOT = (78, 132, 204), (30, 58, 108)
DEFEND_GLOW = (110, 170, 240)           # a defending card's glow: blue, so it never reads as the acting card's gold
DEFEND_TEXT = (170, 216, 255)
INK = (10, 20, 40)
PANEL = (22, 16, 12)
NAME = (232, 199, 122)
NARR_TEXT = (226, 204, 150)

BARRES_CARD = (30, 616, 302, 758)
PORTRAIT = (35, 621, 145, 729)
SENTENCE = "Prepared for the enemy's attack"


def font(path, size, weight=None):
    f = ImageFont.truetype(path, int(size * S))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


CINZEL = lambda size, w=700: font(REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf', size, w)
GOTHIC = lambda size: font(REPO + '/godot/fonts/BIZUDGothic-Regular.ttf', size)


def s(*v):
    return [x * S for x in v]


class Canvas:
    def __init__(self, path):
        self.base = Image.open(path).convert('RGB').resize((1280, 800), Image.LANCZOS).convert('RGBA')
        self.ov = Image.new('RGBA', (1280 * S, 800 * S), (0, 0, 0, 0))

    def draw(self):
        lay = Image.new('RGBA', self.ov.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay):
        self.ov = Image.alpha_composite(self.ov, lay)

    def image(self):
        return Image.alpha_composite(self.base, self.ov.resize((1280, 800), Image.LANCZOS))

    def save(self, name):
        img = self.image()
        img.convert('RGB').save(os.path.join(OUT, name))
        return img


def text(c, x, y, t, f, colour, anchor='la', stroke=0, stroke_colour=INK, alpha=255):
    lay, d = c.draw()
    d.text((x * S, y * S), t, font=f, fill=colour + (alpha,), anchor=anchor,
           stroke_width=int(stroke * S), stroke_fill=stroke_colour + (alpha,))
    c.add(lay)


def glow(c, rect, colour):
    x0, y0, x1, y1 = rect
    lay, d = c.draw()
    for i in range(7, 0, -1):
        d.rectangle(s(x0 - i, y0 - i, x1 + i, y1 + i), outline=colour + (int(30 + 22 * (7 - i)),), width=S)
    d.rectangle(s(x0, y0, x1, y1), outline=colour + (255,), width=2 * S)
    c.add(lay)


def shield_points(cx, y, w, h, steps=24):
    """A heater shield: a flat top, straight sides down to 40 %, curves to the tip."""
    left, right = cx - w / 2, cx + w / 2
    pts = [(left, y), (right, y), (right, y + h * 0.4)]
    for i in range(1, steps + 1):                       # right side to the tip (a quadratic curve)
        t = i / steps
        p0, p1, p2 = (right, y + h * 0.4), (right, y + h * 0.78), (cx, y + h)
        pts.append(((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
                    (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]))
    for i in range(1, steps + 1):                       # the tip up the left side
        t = i / steps
        p0, p1, p2 = (cx, y + h), (left, y + h * 0.78), (left, y + h * 0.4)
        pts.append(((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
                    (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]))
    return pts


def shield(c, cx, y, w, h, rim=3):
    """A steel-blue shield with a gold rim, lit from the top, its shadow under it."""
    lay, d = c.draw()
    d.polygon([(px * S + 3 * S, py * S + 4 * S) for px, py in shield_points(cx, y, w, h)], fill=(0, 0, 0, 110))
    d.polygon([(px * S, py * S) for px, py in shield_points(cx, y, w, h)], fill=ACCENT + (255,))
    inner = shield_points(cx, y + rim, w - 2 * rim, h - 2 * rim * 1.3)
    rows = int(h * S)
    grad = Image.new('RGBA', lay.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(grad)
    for i in range(rows):                               # top to foot
        t = i / rows
        col = tuple(int(STEEL_TOP[k] + (STEEL_FOOT[k] - STEEL_TOP[k]) * t) for k in range(3))
        gd.line([(0, y * S + i), (lay.size[0], y * S + i)], fill=col + (255,))
    mask = Image.new('L', lay.size, 0)
    ImageDraw.Draw(mask).polygon([(px * S, py * S) for px, py in inner], fill=255)
    lay.paste(grad, (0, 0), mask)
    d = ImageDraw.Draw(lay)
    d.line([(px * S, py * S) for px, py in shield_points(cx, y + rim + 3, w - 2 * rim - 8, h * 0.35, 4)[:2]],
           fill=(255, 255, 255, 70), width=2 * S)       # a highlight along the top
    c.add(lay)


def lacquer_panel(c, x, y, w, h):
    lay, d = c.draw()
    d.rectangle(s(x + 3, y + 4, x + w + 3, y + h + 4), fill=(0, 0, 0, 100))
    d.rectangle(s(x, y, x + w, y + h), fill=PANEL + (245,), outline=ACCENT + (255,), width=2 * S)
    d.rectangle(s(x + 4, y + 4, x + w - 4, y + h - 4), outline=(110, 86, 46, 255), width=S)
    c.add(lay)


def card_line(c, base, x, y, w, h, t, f, colour):
    """Writes over a line of the card: its background sampled right of the line, then the text."""
    bg = base.getpixel((x + w + 2, y + h // 2))[:3]
    lay, d = c.draw()
    d.rectangle(s(x, y, x + w, y + h), fill=bg + (255,))
    c.add(lay)
    text(c, x, y + h / 2, t, f, colour, anchor='lm')


# ---------------------------------------------------------------------------------------------------- the variants

def d0_now():
    """D0: the screen as it is (for the comparison)."""
    c = Canvas(os.path.join(NOW, '00093-info-defence.png'))
    return c.save('D0-now.png')


def d1_shield():
    """D1: a shield over the portrait's lower right with the increase in it; the card glows blue; no plate (the log
    keeps the sentence)."""
    c = Canvas(os.path.join(BARE, '00093-info-defence.png'))
    glow(c, BARRES_CARD, DEFEND_GLOW)
    cx, y, w, h = 114, 646, 68, 82
    shield(c, cx, y, w, h)
    text(c, cx, y + 17, 'DEF', CINZEL(13, 800), ACCENT, anchor='mm', stroke=1.5)
    text(c, cx, y + 44, '+5', CINZEL(30, 900), (255, 255, 255), anchor='mm', stroke=2.5)
    return c.save('D1-shield.png')


def d2_ribbon():
    """D2: a ribbon across the card's middle, its tails past the card's edges: a shield, DEFEND, the increase."""
    c = Canvas(os.path.join(BARE, '00093-info-defence.png'))
    glow(c, BARRES_CARD, DEFEND_GLOW)
    x0, _, x1, _ = BARRES_CARD
    y0, y1 = 664, 710
    lay, d = c.draw()
    for side in (-1, 1):                                # the tails, darker, notched
        ex = x0 - 16 if side < 0 else x1 + 16
        bx = x0 + 6 if side < 0 else x1 - 6
        d.polygon(s(bx, y0 + 6, ex, y0 + 6, ex + 8 * side * -1, (y0 + y1) / 2 + 3, ex, y1 + 6, bx, y1 + 6), fill=(24, 44, 84, 255))
    d.rectangle(s(x0 - 6 + 3, y0 + 4, x1 + 6 + 3, y1 + 4), fill=(0, 0, 0, 110))
    c.add(lay)
    lay, d = c.draw()
    for i in range(int((y1 - y0) * S)):
        t = i / ((y1 - y0) * S)
        col = tuple(int(STEEL_TOP[k] + (STEEL_FOOT[k] - STEEL_TOP[k]) * t) for k in range(3))
        d.line([((x0 - 6) * S, y0 * S + i), ((x1 + 6) * S, y0 * S + i)], fill=col + (250,))
    d.line(s(x0 - 6, y0 + 2, x1 + 6, y0 + 2), fill=ACCENT + (255,), width=2 * S)
    d.line(s(x0 - 6, y1 - 2, x1 + 6, y1 - 2), fill=ACCENT + (255,), width=2 * S)
    c.add(lay)
    shield(c, x0 + 26, y0 + 7, 26, 32, rim=2)
    text(c, x0 + 48, (y0 + y1) / 2 + 1, 'DEFEND', CINZEL(25, 800), (255, 255, 255), anchor='lm', stroke=1.5)
    text(c, x1 - 10, (y0 + y1) / 2 + 1, 'DEF +5', CINZEL(22, 800), ACCENT, anchor='rm', stroke=1.5)
    return c.save('D2-ribbon.png')


def d3_scene():
    """D3: the event over the defending army in the scene, as the Guard effect plays: a large shield with the increase,
    DEFEND, the sentence under it; the card glows blue."""
    c = Canvas(os.path.join(BARE, '00092-effect-guard-player.png'))
    glow(c, BARRES_CARD, DEFEND_GLOW)
    cx, y, w, h = 340, 62, 92, 108
    shield(c, cx, y, w, h, rim=4)
    text(c, cx, y + 22, 'DEF', CINZEL(17, 800), ACCENT, anchor='mm', stroke=2)
    text(c, cx, y + 58, '+5', CINZEL(40, 900), (255, 255, 255), anchor='mm', stroke=3)
    text(c, cx, y + h + 26, 'DEFEND', CINZEL(32, 800), (255, 255, 255), anchor='mm', stroke=3)
    caption = 'Barres: ' + SENTENCE.lower()
    cw = GOTHIC(19).getlength(caption) / S + 36
    lay, d = c.draw()                                   # the sentence on a dark pill, the scene behind it
    d.rounded_rectangle(s(cx - cw / 2, y + h + 46, cx + cw / 2, y + h + 76), radius=15 * S, fill=(8, 14, 28, 190))
    c.add(lay)
    text(c, cx, y + h + 61, caption, GOTHIC(19), DEFEND_TEXT, anchor='mm')
    return c.save('D3-scene.png')


def d4_floating():
    """D4: "DEF +5" rises over the portrait in large outlined letters (as a damage number) and fades; the card glows
    blue and its order line says Defending; no plate."""
    c = Canvas(os.path.join(BARE, '00093-info-defence.png'))
    glow(c, BARRES_CARD, DEFEND_GLOW)
    card_line(c, c.base, 156, 697, 140, 17, 'Manual · Defending', GOTHIC(15), DEFEND_TEXT)
    cx = (PORTRAIT[0] + PORTRAIT[2]) / 2 + 20
    for dy, a in ((16, 70),):                           # its rise's trail
        text(c, cx, 650 + dy, 'DEF +5', CINZEL(34, 900), DEFEND_TEXT, anchor='mm', stroke=4, alpha=a)
    text(c, cx, 650, 'DEF +5', CINZEL(34, 900), DEFEND_TEXT, anchor='mm', stroke=4)
    return c.save('D4-floating.png')


def d5_caption():
    """D5: the sentence as a caption plate at the scene's foot on the defender's side (above the order strip): a shield,
    the unit's name, the sentence, the increase large at its end; the card glows blue."""
    c = Canvas(os.path.join(BARE, '00093-info-defence.png'))
    glow(c, BARRES_CARD, DEFEND_GLOW)
    name_w = CINZEL(19, 700).getlength('Barres') / S
    sentence_w = GOTHIC(18).getlength(SENTENCE) / S
    x, y, h = 150, 334, 50
    w = 50 + name_w + 12 + sentence_w + 20 + 40 + 4 + 34 + 14
    lacquer_panel(c, x, y, w, h)
    shield(c, x + 26, y + 9, 28, 33, rim=2)
    text(c, x + 50, y + h / 2, 'Barres', CINZEL(19, 700), NAME, anchor='lm')
    text(c, x + 50 + name_w + 12, y + h / 2, SENTENCE, GOTHIC(18), NARR_TEXT, anchor='lm')
    text(c, x + w - 14, y + h / 2, '+5', CINZEL(28, 900), DEFEND_TEXT, anchor='rm', stroke=1)
    text(c, x + w - 54, y + h / 2 + 1, 'DEF', CINZEL(14, 800), DEFEND_TEXT, anchor='rm')
    return c.save('D5-caption.png')


def sheet(images):
    """The lower left of each (the card and the scene over it), two to a row, labelled."""
    crop = (0, 24, 640, 770)
    cw, ch = (crop[2] - crop[0]) // 2 * 2, crop[3] - crop[1]
    k = 0.75
    tw, th = int(cw * k), int(ch * k)
    label = 34
    out = Image.new('RGB', (2 * tw + 30, 3 * (th + label) + 20), (16, 16, 18))
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(REPO + '/godot/fonts/BIZUDGothic-Regular.ttf', 22)
    for i, (name, img) in enumerate(images):
        col, row = i % 2, i // 2
        x, y = 10 + col * (tw + 10), 10 + row * (th + label)
        d.text((x + 4, y + 4), name, font=f, fill=(236, 226, 204))
        out.paste(img.crop(crop).convert('RGB').resize((tw, th), Image.LANCZOS), (x, y + label))
    out.save(os.path.join(OUT, 'D-sheet.png'))


if __name__ == '__main__':
    shots = [('D0 now', d0_now()), ('D1 shield on the portrait', d1_shield()), ('D2 ribbon across the card', d2_ribbon()),
             ('D3 over the army in the scene', d3_scene()), ('D4 rising number', d4_floating()),
             ('D5 caption at the scene\'s foot', d5_caption())]
    sheet(shots)
