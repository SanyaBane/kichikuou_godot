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
    ribbon(c, lambda x1, mid: text(c, x1 - 10, mid, 'DEF +5', CINZEL(22, 800), ACCENT, anchor='rm', stroke=1.5))
    return c.save('D2-ribbon.png')


def ribbon(c, right, left='DEFEND', y0=664, h=46, left_size=25, badge=True, left_y=None, badge_x=26, left_x=48):
    """D2's ribbon on Barres's card; right(x1, mid) draws its right-hand label (left None: no word; badge False: no shield)."""
    glow(c, BARRES_CARD, DEFEND_GLOW)
    x0, _, x1, _ = BARRES_CARD
    y1 = y0 + h
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
    if badge:
        shield(c, x0 + badge_x, y0 + 7, 26, 32, rim=2)
    if left:
        text(c, x0 + left_x, left_y or (y0 + y1) / 2 + 1, left, CINZEL(left_size, 800), (255, 255, 255), anchor='lm', stroke=1.5)
    right(x1, (y0 + y1) / 2 + 1)


# ---------------------------------------------------------------------------------------------------- the ribbon's label

# What "DEF +n" means (docs/battle/01-flow.md §8.3): n percentage points off the enemy's hit chance against the unit till the
# round's end, not the Defense stat. L1-L6: the label in other words (owner, 2026-10-21); MINUS is U+2212.
MINUS = '\u2212'
CAPTION_X, CAPTION_Y, CAPTION_H = 150, 334, 50


def label(name, right, left='DEFEND', caption=None):
    c = Canvas(os.path.join(BARE, '00093-info-defence.png'))
    ribbon(c, lambda x1, mid: right(c, x1, mid), left)
    sentence = caption or SENTENCE
    name_w = CINZEL(19, 700).getlength('Barres') / S
    w = 50 + name_w + 12 + GOTHIC(18).getlength(sentence) / S + 24
    lacquer_panel(c, CAPTION_X, CAPTION_Y, w, CAPTION_H)
    shield(c, CAPTION_X + 26, CAPTION_Y + 9, 28, 33, rim=2)
    text(c, CAPTION_X + 50, CAPTION_Y + CAPTION_H / 2, 'Barres', CINZEL(19, 700), NAME, anchor='lm')
    text(c, CAPTION_X + 50 + name_w + 12, CAPTION_Y + CAPTION_H / 2, sentence, GOTHIC(18), NARR_TEXT, anchor='lm')
    return c.save(name)


def gold(t, size=22):
    return lambda c, x1, mid: text(c, x1 - 10, mid, t, CINZEL(size, 800), ACCENT, anchor='rm', stroke=1.5)


def stacked(small, big):
    """A small line over a large one, right-aligned."""
    def draw(c, x1, mid):
        text(c, x1 - 10, mid - 10, small, CINZEL(11, 800), ACCENT, anchor='rm', stroke=1)
        text(c, x1 - 10, mid + 7, big, CINZEL(24, 800), ACCENT, anchor='rm', stroke=1.5)
    return draw


def labels():
    shots = [
        ('L0 now: DEF +5', label('L0-def.png', gold('DEF +5'))),
        ('L1 ' + MINUS + '5% HIT', label('L1-minus-hit.png', gold(MINUS + '5% HIT'))),
        ('L2 HIT ' + MINUS + '5%', label('L2-hit-minus.png', gold('HIT ' + MINUS + '5%'))),
        ('L3 GUARD +5', label('L3-guard.png', gold('+5', 26), left='GUARD')),
        ('L4 ENEMY HIT over ' + MINUS + '5%', label('L4-stacked.png', stacked('ENEMY HIT', MINUS + '5%'))),
        ('L5 GUARD ' + MINUS + '5% HIT', label('L5-guard-hit.png', gold(MINUS + '5% HIT'), left='GUARD')),
        ('L6 ' + MINUS + '5%, the caption says what', label('L6-caption.png', gold(MINUS + '5%', 26),
                                                     caption="Prepared: enemy hit chance " + MINUS + "5%")),
        ('L7 DEFEND alone, the caption says it', label('L7-caption-only.png', lambda c, x1, mid: None,
                                                       caption="Prepared: enemy hit chance " + MINUS + "5%")),
    ]
    crop = (0, 320, 640, 770)
    w, h = crop[2] - crop[0], crop[3] - crop[1]
    top = 34
    out = Image.new('RGB', (2 * w + 30, 4 * (h + top) + 20), (16, 16, 18))
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(REPO + '/godot/fonts/BIZUDGothic-Regular.ttf', 22)
    for i, (title, img) in enumerate(shots):
        x, y = 10 + (i % 2) * (w + 10), 10 + (i // 2) * (h + top)
        d.text((x + 4, y + 4), title, font=f, fill=(236, 226, 204))
        out.paste(img.crop(crop).convert('RGB'), (x, y + top))
    out.save(os.path.join(OUT, 'L-sheet.png'))


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


# ---------------------------------------------------------------------------------------------------- "ENEMY HIT" readable

# The owner (2026-10-21) likes L4's "ENEMY HIT" (whose hit chance falls), but it is too small: M1-M6 make room for it.

def framed(name, draw, caption=None):
    """A frame with the caption at the scene's foot; draw(c) draws the card's part."""
    c = Canvas(os.path.join(BARE, '00093-info-defence.png'))
    draw(c)
    sentence = caption or SENTENCE
    name_w = CINZEL(19, 700).getlength('Barres') / S
    w = 50 + name_w + 12 + GOTHIC(18).getlength(sentence) / S + 24
    lacquer_panel(c, CAPTION_X, CAPTION_Y, w, CAPTION_H)
    shield(c, CAPTION_X + 26, CAPTION_Y + 9, 28, 33, rim=2)
    text(c, CAPTION_X + 50, CAPTION_Y + CAPTION_H / 2, 'Barres', CINZEL(19, 700), NAME, anchor='lm')
    text(c, CAPTION_X + 50 + name_w + 12, CAPTION_Y + CAPTION_H / 2, sentence, GOTHIC(18), NARR_TEXT, anchor='lm')
    return c.save(name)


def hit_line(c, x, mid, size, anchor='lm', white=True):
    """'ENEMY HIT' (white) and the value (gold) on one line from x."""
    f = CINZEL(size, 800)
    if anchor == 'rm':
        vw = f.getlength(' ' + MINUS + '5%') / S
        text(c, x, mid, ' ' + MINUS + '5%', f, ACCENT, anchor='rm', stroke=1.5)
        text(c, x - vw, mid, 'ENEMY HIT', f, (255, 255, 255) if white else ACCENT, anchor='rm', stroke=1.5)
        return
    text(c, x, mid, 'ENEMY HIT', f, (255, 255, 255) if white else ACCENT, anchor='lm', stroke=1.5)
    text(c, x + f.getlength('ENEMY HIT ') / S, mid, MINUS + '5%', f, ACCENT, anchor='lm', stroke=1.5)


def m1(c):
    """M1: no word: the shield says DEFEND; ENEMY HIT −5% across the ribbon."""
    x0 = BARRES_CARD[0]
    ribbon(c, lambda x1, mid: hit_line(c, x0 + 48, mid, 20), left=None)


def m2(c):
    """M2: GUARD (shorter than DEFEND) and ENEMY HIT −5% smaller after it."""
    ribbon(c, lambda x1, mid: hit_line(c, x1 - 10, mid + 1, 14, anchor='rm', white=False), left='GUARD', left_size=21)


def m3(c):
    """M3: two lines at the right, the top one larger than L4's (14 px, not 11)."""
    def right(x1, mid):
        text(c, x1 - 10, mid - 10, 'ENEMY HIT', CINZEL(14, 800), (255, 255, 255), anchor='rm', stroke=1)
        text(c, x1 - 10, mid + 9, MINUS + '5%', CINZEL(22, 800), ACCENT, anchor='rm', stroke=1.5)
    ribbon(c, right, left_size=23)


def m4(c):
    """M4: a large shield over the ribbon's left end holds the value; ENEMY HIT large after it."""
    x0 = BARRES_CARD[0]
    ribbon(c, lambda x1, mid: text(c, x0 + 70, mid, 'ENEMY HIT', CINZEL(22, 800), (255, 255, 255), anchor='lm', stroke=1.5),
           left=None, badge=False)
    cx, y = x0 + 30, 652
    shield(c, cx, y, 52, 62, rim=3)
    text(c, cx, y + 26, MINUS + '5%', CINZEL(17, 900), (255, 255, 255), anchor='mm', stroke=2)


def m5(c):
    """M5: DEFEND on the ribbon, ENEMY HIT −5% on a gold tag hanging from its lower edge at the right."""
    x0, _, x1, _ = BARRES_CARD
    ribbon(c, lambda x1_, mid: None)
    f = CINZEL(15, 800)
    t = 'ENEMY HIT ' + MINUS + '5%'
    w = f.getlength(t) / S + 20
    tx, ty = x1 - 4 - w, 708
    lay, d = c.draw()
    d.rectangle(s(tx + 2, ty + 3, tx + w + 2, ty + 25), fill=(0, 0, 0, 110))
    d.rectangle(s(tx, ty, tx + w, ty + 22), fill=ACCENT + (255,), outline=(120, 90, 36, 255), width=S)
    c.add(lay)
    text(c, tx + w / 2, ty + 11, t, f, INK, anchor='mm')


def m6(c):
    """M6: a taller ribbon in two rows: DEFEND, then ENEMY HIT −5% under it."""
    x0 = BARRES_CARD[0]
    ribbon(c, lambda x1, mid: hit_line(c, x0 + 48, mid + 13, 16), y0=656, h=60, left_size=23, left_y=673)


def enemy_hit():
    shots = [('L4 (the last round)', label('L4-stacked.png', stacked('ENEMY HIT', MINUS + '5%'))),
             ('M1 shield + ENEMY HIT ' + MINUS + '5%', framed('M1-no-word.png', m1)),
             ('M2 GUARD + ENEMY HIT ' + MINUS + '5%', framed('M2-guard.png', m2)),
             ('M3 two lines, larger', framed('M3-two-lines.png', m3)),
             ('M4 the value in a large shield', framed('M4-big-shield.png', m4)),
             ('M5 a gold tag under the ribbon', framed('M5-tag.png', m5)),
             ('M6 a taller ribbon, two rows', framed('M6-two-rows.png', m6))]
    crop = (0, 560, 330, 770)
    k = 2
    w, h = (crop[2] - crop[0]) * k, (crop[3] - crop[1]) * k
    top = 34
    out = Image.new('RGB', (2 * w + 30, 4 * (h + top) + 20), (16, 16, 18))
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(REPO + '/godot/fonts/BIZUDGothic-Regular.ttf', 22)
    for i, (title, img) in enumerate(shots):
        x, y = 10 + (i % 2) * (w + 10), 10 + (i // 2) * (h + top)
        d.text((x + 4, y + 4), title, font=f, fill=(236, 226, 204))
        out.paste(img.crop(crop).convert('RGB').resize((w, h), Image.LANCZOS), (x, y + top))
    out.save(os.path.join(OUT, 'M-sheet.png'))



# ---------------------------------------------------------------------------------------------------- M2 with DEFEND

# The owner (2026-10-21): M2's one line, but DEFEND, not GUARD; DEFEND smaller or the shield further left if it does not fit.
# Each is drawn with the smallest and the largest value (−2% and −21%, RND(20) + 1), so the fit is the worst case's.

def n_variant(badge_x, defend_size, hit_size, value, hit_white=False):
    x0, _, x1, _ = BARRES_CARD
    left_x = badge_x + 22
    f = CINZEL(hit_size, 800)
    t = 'ENEMY HIT ' + MINUS + value + '%'

    def draw(c):
        def right(x1_, mid):
            vw = f.getlength(' ' + MINUS + value + '%') / S
            text(c, x1 - 10, mid + 1, ' ' + MINUS + value + '%', f, ACCENT, anchor='rm', stroke=1.5)
            text(c, x1 - 10 - vw, mid + 1, 'ENEMY HIT', f, (255, 255, 255) if hit_white else ACCENT, anchor='rm', stroke=1.5)
        ribbon(c, right, left_size=defend_size, badge_x=badge_x, left_x=left_x)
    gap = (x1 - 10 - f.getlength(t) / S) - (x0 + left_x + CINZEL(defend_size, 800).getlength('DEFEND') / S)
    return draw, gap


def n_sheet():
    variants = [
        ('N1 DEFEND 21, ENEMY HIT 14', 26, 21, 14, False),
        ('N2 shield left, DEFEND 20, ENEMY HIT 15', 14, 20, 15, False),
        ('N3 shield left, DEFEND 18, ENEMY HIT 16', 14, 18, 16, False),
        ('N4 as N3, ENEMY HIT white', 14, 18, 16, True),
        ('N5 shield on the tail, DEFEND 19, ENEMY HIT 16', 4, 19, 16, False),
        ('N6 shield on the tail, DEFEND 17, ENEMY HIT 17', 4, 17, 17, False),
    ]
    rows = []
    for title, bx, ds, hs, white in variants:
        pair = []
        for value in ('2', '21'):
            draw, gap = n_variant(bx, ds, hs, value, white)
            pair.append(framed('N-%s-%s.png' % (title.split()[0], value), draw))
            print('%s  value %s: gap %.0f px' % (title, value, gap))
        rows.append((title, pair))
    crop = (0, 600, 330, 770)
    k = 1.5
    w, h = int((crop[2] - crop[0]) * k), int((crop[3] - crop[1]) * k)
    top = 34
    out = Image.new('RGB', (2 * w + 30, len(rows) * (h + top) + 20), (16, 16, 18))
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(REPO + '/godot/fonts/BIZUDGothic-Regular.ttf', 22)
    for r, (title, pair) in enumerate(rows):
        y = 10 + r * (h + top)
        d.text((14, y + 4), title + '   (' + MINUS + '2% | ' + MINUS + '21%)', font=f, fill=(236, 226, 204))
        for i, img in enumerate(pair):
            out.paste(img.crop(crop).convert('RGB').resize((w, h), Image.LANCZOS), (10 + i * (w + 10), y + top))
    out.save(os.path.join(OUT, 'N-sheet.png'))
    # the same at the game's size, for reading
    k = 1
    w, h = crop[2] - crop[0], crop[3] - crop[1]
    out = Image.new('RGB', (2 * w + 30, len(rows) * (h + top) + 20), (16, 16, 18))
    d = ImageDraw.Draw(out)
    for r, (title, pair) in enumerate(rows):
        y = 10 + r * (h + top)
        d.text((14, y + 4), title.split()[0], font=f, fill=(236, 226, 204))
        for i, img in enumerate(pair):
            out.paste(img.crop(crop).convert('RGB'), (10 + i * (w + 10), y + top))
    out.save(os.path.join(OUT, 'N-sheet-1x.png'))


if __name__ == '__main__':
    shots = [('D0 now', d0_now()), ('D1 shield on the portrait', d1_shield()), ('D2 ribbon across the card', d2_ribbon()),
             ('D3 over the army in the scene', d3_scene()), ('D4 rising number', d4_floating()),
             ('D5 caption at the scene\'s foot', d5_caption())]
    sheet(shots)
    labels()
    enemy_hit()
    n_sheet()
