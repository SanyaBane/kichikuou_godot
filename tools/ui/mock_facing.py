"""Mock-ups of the deployment screen's facing mark (owner, 2026-10-24): the way the player's army faces.

Draws variants of the mark over a screen dump of our deployment screen (a window's dump, 1280x824 with the
menu bar's 24 pixels on top, the party filled): the party panel is cut out, the current gold triangle painted
over with the panel's colour, and each variant drawn in its place; the panels side by side with their names.
The images stay outside the repository.

    python tools/ui/mock_facing.py <deployment dump.png> <out.png> [round]

Round 1 is F0-F7; round 2 (the owner asked for more of F5 and F6) is B1-B5 and T1-T4; round 3 (more of T3) X1-X8;
round 4 (T3, X1-X3 without the red, as X7) D1-D4.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
BRUSH = REPO + '/godot/fonts/Shojumaru-Regular.ttf'

# Lacquer's colours (godot/Strategy/Lacquer.cs)
GOLD = (196, 160, 86)
DIM_GOLD = (110, 86, 46)
NAME = (232, 199, 122)
LABEL = (150, 134, 108)
VERMILION = (170, 40, 28)
DARK = (90, 66, 34)

# The party panel in the 1280x800 screen (DeploymentPanels): the cards, the facing strip right of them.
BAR = 24
PANEL = (690, 368, 1264, 784)
CARD_X0, CARD_X1 = 704, 1214
BACK_X1, FRONT_X0 = 956, 962
ROWS = [(440, 544), (552, 656)]
STRIP = (1220, 1250)
MID = (ROWS[0][1] + ROWS[1][0]) / 2
LABELS_Y = 428


def font(path, size, weight=None):
    f = ImageFont.truetype(path, size)
    if weight is not None:
        try:
            f.set_variation_by_axes([weight])
        except OSError:
            pass
    return f


def local(x, y):
    return x - PANEL[0], y - PANEL[1]


def cut(dump):
    panel = dump.crop((PANEL[0], PANEL[1] + BAR, PANEL[2], PANEL[3] + BAR)).convert('RGBA')
    fill = panel.getpixel(local(STRIP[0] + 4, ROWS[0][0] + 4))
    d = ImageDraw.Draw(panel)
    x0, y0 = local(STRIP[0] - 2, ROWS[0][0])
    x1, y1 = local(STRIP[1] + 2, ROWS[1][1])
    d.rectangle((x0, y0, x1, y1), fill=fill)         # the current triangle gone
    return panel, fill


def layer(panel):
    return Image.new('RGBA', panel.size, (0, 0, 0, 0))


def over(panel, top):
    return Image.alpha_composite(panel, top)


def behind(panel, top, fill, tolerance=18):
    """`top` only where the panel shows its own colour: drawn under the cards."""
    out = panel.copy()
    pp, tp, op = panel.load(), top.load(), out.load()
    for y in range(panel.height):
        for x in range(panel.width):
            r, g, b, a = tp[x, y]
            if a == 0:
                continue
            pr, pg, pb, _ = pp[x, y]
            if abs(pr - fill[0]) + abs(pg - fill[1]) + abs(pb - fill[2]) > tolerance:
                continue
            k = a / 255
            op[x, y] = (round(pr + (r - pr) * k), round(pg + (g - pg) * k), round(pb + (b - pb) * k), 255)
    return out


def poly(d, points, fill=None, outline=None, width=1):
    d.polygon([local(x, y) for x, y in points], fill=fill, outline=outline, width=width)


def line(d, points, fill, width):
    d.line([local(x, y) for x, y in points], fill=fill, width=width, joint='curve')


# ---------------------------------------------------------------- the variants

def v0_current(panel, fill):
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x0, x1, cy = STRIP[0], STRIP[1], MID
    poly(d, [(x0 + 2, cy - 18), (x1, cy), (x0 + 2, cy + 18)], fill=GOLD)
    poly(d, [(x0 + 6, cy - 10), (x1 - 7, cy), (x0 + 6, cy + 10)], fill=DARK)
    return over(panel, top)


def v1_chevrons(panel, fill):
    """Three double chevrons down the strip, the middle one lit: a march's road marks."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    for cy, colour in ((ROWS[0][0] + 40, DIM_GOLD), (MID, NAME), (ROWS[1][1] - 40, DIM_GOLD)):
        for dx in (0, 12):
            x = STRIP[0] + 2 + dx
            line(d, [(x, cy - 11), (x + 11, cy), (x, cy + 11)], colour + (255,), 4)
    return over(panel, top)


def v2_front_line(panel, fill):
    """The front line: a gold rule along the front cards' right edge, its teeth towards the enemy."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x = STRIP[0] + 2
    y0, y1 = ROWS[0][0] + 4, ROWS[1][1] - 4
    d.rectangle((*local(x, y0), *local(x + 3, y1)), fill=GOLD + (255,))
    y = y0 + 6
    while y + 14 <= y1:
        poly(d, [(x + 3, y), (x + 17, y + 7), (x + 3, y + 14)], fill=GOLD + (255,))
        y += 22
    return over(panel, top)


def v3_axis(panel, fill):
    """The axis of advance (a map's arrow): one broad faint arrow behind the cards, its head past the front row."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    y0, y1 = ROWS[0][0] + 22, ROWS[1][1] - 22
    head0 = STRIP[0] - 40
    shaft = [(CARD_X0 - 6, MID - 34), (head0, MID - 34), (head0, y0 - 18), (STRIP[1] + 2, MID), (head0, y1 + 18),
             (head0, MID + 34), (CARD_X0 - 6, MID + 34)]
    poly(d, shaft, fill=GOLD + (46,), outline=GOLD + (150,), width=2)
    return behind(panel, top, fill)


def v4_banner(panel, fill):
    """The army's banner: a gold pole in the strip, a swallow-tailed vermilion pennant flying towards the enemy."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    px = STRIP[0] + 3
    y0, y1 = ROWS[0][0] + 18, ROWS[1][1] - 18
    d.rectangle((*local(px, y0), *local(px + 2, y1)), fill=GOLD + (255,))
    d.ellipse((*local(px - 3, y0 - 6), *local(px + 5, y0 + 2)), fill=NAME + (255,))
    fy0, fy1 = y0 + 6, y0 + 92
    flag = [(px + 3, fy0), (STRIP[1] + 6, fy0 + 10), (STRIP[1] - 4, (fy0 + fy1) / 2), (STRIP[1] + 6, fy1 - 10), (px + 3, fy1)]
    poly(d, flag, fill=VERMILION + (255,), outline=GOLD + (255,), width=2)
    poly(d, [(px + 9, (fy0 + fy1) / 2 - 8), (px + 21, (fy0 + fy1) / 2), (px + 9, (fy0 + fy1) / 2 + 8)], fill=NAME + (255,))
    return over(panel, top)


def v5_bracket(panel, fill):
    """A gold bracket holding the front row's two cards, its middle drawn out into an arrowhead."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x = STRIP[0] + 1
    y0, y1 = ROWS[0][0] + 2, ROWS[1][1] - 2
    line(d, [(CARD_X1 + 2, y0), (x, y0), (x, MID - 16), (x + 10, MID - 6)], GOLD + (255,), 3)   # the ends short of the cards (owner)
    line(d, [(CARD_X1 + 2, y1), (x, y1), (x, MID + 16), (x + 10, MID + 6)], GOLD + (255,), 3)
    poly(d, [(x + 6, MID - 14), (STRIP[1] + 4, MID), (x + 6, MID + 14)], fill=NAME + (255,))
    return over(panel, top)


def v6_enemy_tab(panel, fill):
    """A vermilion tab on the panel's right edge: "ENEMY" upright along it, an arrow above and below."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x0, x1 = STRIP[0] + 2, STRIP[1] + 4
    y0, y1 = ROWS[0][0] + 6, ROWS[1][1] - 6
    d.rectangle((*local(x0, y0), *local(x1, y1)), fill=VERMILION + (235,), outline=GOLD + (255,), width=1)
    word = Image.new('RGBA', (y1 - y0, x1 - x0), (0, 0, 0, 0))
    f = font(CINZEL, 15, 700)
    wd = ImageDraw.Draw(word)
    text = 'E N E M Y'
    tw = wd.textlength(text, font=f)
    wd.text(((word.width - tw) / 2, (word.height - 17) / 2 - 1), text, font=f, fill=NAME)
    word = word.rotate(-90, expand=True)
    top.alpha_composite(word, local(x0, y0))
    for cy in (y0 + 22, y1 - 22):
        poly(d, [(x0 + 8, cy - 7), (x1 - 7, cy), (x0 + 8, cy + 7)], fill=NAME + (255,))
    return over(panel, top)


def v7_header(panel, fill):
    """The rows' header as one arrow across the panel: BACK ROW → FRONT ROW → ENEMY; the strip left empty."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x0, y0 = local(PANEL[0] + 12, LABELS_Y - 10)
    x1, y1 = local(PANEL[2] - 12, LABELS_Y + 10)
    d.rectangle((x0, y0, x1, y1), fill=fill)               # the old labels gone
    y = LABELS_Y
    line(d, [(CARD_X0 + 4, y), (STRIP[1] - 4, y)], DIM_GOLD + (255,), 2)
    poly(d, [(STRIP[1] - 12, y - 7), (STRIP[1] + 2, y), (STRIP[1] - 12, y + 7)], fill=GOLD + (255,))
    f = font(CINZEL, 13, 600)
    for text, cx, colour in (('BACK ROW', (CARD_X0 + BACK_X1) / 2, LABEL), ('FRONT ROW', (FRONT_X0 + CARD_X1) / 2, LABEL)):
        tw = d.textlength(text, font=f)
        lx, ly = local(cx - tw / 2, y - 9)
        d.rectangle((lx - 8, ly - 1, lx + tw + 8, ly + 18), fill=fill)
        d.text((lx, ly), text, font=f, fill=colour)
    fe = font(CINZEL, 12, 700)
    tw = d.textlength('ENEMY', font=fe)
    ex, ey = local(STRIP[1] - 16 - tw, y - 8)
    d.rectangle((ex - 8, ey - 1, ex + tw + 4, ey + 17), fill=fill)
    d.text((ex, ey), 'ENEMY', font=fe, fill=(214, 96, 76))
    return over(panel, top)


# ---------------------------------------------------------------- round 2: more of F5 (the bracket) and F6 (the tab)

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
DEEP_RED = (120, 26, 18)
RED_TEXT = (214, 96, 76)


def upright(top, text, f, colour, x0, y0, x1, y1, spacing=0):
    """`text` read top to bottom (each letter upright) centred in the box."""
    d = ImageDraw.Draw(top)
    sizes = [d.textbbox((0, 0), ch, font=f) for ch in text]
    pitch = max(b[3] - b[1] for b in sizes) + spacing
    total = pitch * len(text) - spacing
    y = (y0 + y1) / 2 - total / 2
    for ch, b in zip(text, sizes):
        w = b[2] - b[0]
        lx, ly = local((x0 + x1) / 2 - w / 2 - b[0], y - b[1])
        d.text((lx, ly), ch, font=f, fill=colour)
        y += pitch


def rotated(top, text, f, colour, x0, y0, x1, y1):
    """`text` turned a quarter clockwise (read top to bottom), centred in the box."""
    word = Image.new('RGBA', (y1 - y0, x1 - x0), (0, 0, 0, 0))
    wd = ImageDraw.Draw(word)
    b = wd.textbbox((0, 0), text, font=f)
    wd.text(((word.width - (b[2] - b[0])) / 2 - b[0], (word.height - (b[3] - b[1])) / 2 - b[1]), text, font=f, fill=colour)
    top.alpha_composite(word.rotate(-90, expand=True), tuple(int(v) for v in local(x0, y0)))


def arrowhead(d, x, cy, w, h, fill, inner=None):
    poly(d, [(x, cy - h), (x + w, cy), (x, cy + h)], fill=fill)
    if inner is not None:
        poly(d, [(x + 4, cy - h + 8), (x + w - 7, cy), (x + 4, cy + h - 8)], fill=inner)


def b1_bracket_triangle(panel, fill):
    """The bracket in thin gold, the old triangle's gold-and-dark head at its middle."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x, y0, y1 = STRIP[0], ROWS[0][0] + 2, ROWS[1][1] - 2
    line(d, [(CARD_X1 - 10, y0), (x, y0), (x, MID - 20)], GOLD + (255,), 2)
    line(d, [(CARD_X1 - 10, y1), (x, y1), (x, MID + 20)], GOLD + (255,), 2)
    arrowhead(d, x - 1, MID, 32, 20, GOLD + (255,), DARK + (255,))
    return over(panel, top)


def b2_double(panel, fill):
    """Two gold rules side by side, as the panels' frames, opening round a lacquer head with a gold edge."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    y0, y1 = ROWS[0][0] + 2, ROWS[1][1] - 2
    for x, colour, w in ((STRIP[0] - 1, GOLD, 2), (STRIP[0] + 4, DIM_GOLD, 1)):
        dy = 0 if x < STRIP[0] else 5
        line(d, [(CARD_X1 - 10, y0 + dy), (x, y0 + dy), (x, MID - 22 + dy)], colour + (255,), w)
        line(d, [(CARD_X1 - 10, y1 - dy), (x, y1 - dy), (x, MID + 22 - dy)], colour + (255,), w)
    poly(d, [(STRIP[0] - 2, MID - 22), (STRIP[1] + 4, MID), (STRIP[0] - 2, MID + 22)], fill=(34, 26, 20, 255),
         outline=GOLD + (255,), width=2)
    poly(d, [(STRIP[0] + 5, MID - 9), (STRIP[0] + 19, MID), (STRIP[0] + 5, MID + 9)], fill=NAME + (255,))
    return over(panel, top)


def b3_brace(panel, fill):
    """A curly brace in gold, its point drawn out into the head."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x0, y0, y1 = STRIP[0] - 2, ROWS[0][0] + 2, ROWS[1][1] - 2
    xm = x0 + 10
    pts_top = [(CARD_X1 - 10, y0), (x0 + 2, y0), (x0 + 8, y0 + 8), (x0 + 10, y0 + 22), (xm, MID - 18), (xm + 4, MID - 6)]
    pts_bottom = [(CARD_X1 - 10, y1), (x0 + 2, y1), (x0 + 8, y1 - 8), (x0 + 10, y1 - 22), (xm, MID + 18), (xm + 4, MID + 6)]
    line(d, pts_top, GOLD + (255,), 3)
    line(d, pts_bottom, GOLD + (255,), 3)
    arrowhead(d, xm + 2, MID, STRIP[1] + 4 - xm - 2, 12, NAME + (255,))
    return over(panel, top)


def b4_lit_front(panel, fill):
    """The bracket with a glow towards the enemy: the front row's edge lit in gold, the head bright."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    y0, y1 = ROWS[0][0] + 2, ROWS[1][1] - 2
    for i in range(14):
        a = int(70 * (1 - i / 14))
        d.rectangle((*local(STRIP[0] + i, y0), *local(STRIP[0] + i, y1)), fill=GOLD + (a // 3,))
    line(d, [(CARD_X1 - 10, y0), (STRIP[0], y0), (STRIP[0], y1), (CARD_X1 - 10, y1)], GOLD + (255,), 2)
    arrowhead(d, STRIP[0] + 2, MID, 30, 15, NAME + (255,))
    return over(panel, top)


def t1_seal(panel, fill):
    """A vermilion seal with 敵 (the enemy) in the strip's middle, chevrons above and below it — Sengoku Rance's way."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x0, x1 = STRIP[0] - 1, STRIP[1] + 3
    s0, s1 = MID - 17, MID + 17
    d.rounded_rectangle((*local(x0, s0), *local(x1, s1)), radius=4, fill=VERMILION + (255,), outline=(214, 96, 76, 255), width=1)
    upright(top, '敵', font(GOTHIC, 22), (250, 226, 160), x0, s0, x1, s1)
    for cy in (ROWS[0][0] + 36, ROWS[1][1] - 36):
        for dx in (0, 10):
            line(d, [(STRIP[0] + 4 + dx, cy - 8), (STRIP[0] + 12 + dx, cy), (STRIP[0] + 4 + dx, cy + 8)], DIM_GOLD + (255,), 3)
    return over(panel, top)


def t2_arrow_tab(panel, fill):
    """The tab shaped as an arrow: its right side drawn to a point at the rows' middle, ENEMY upright on it."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x0, x1 = STRIP[0], STRIP[1] - 6
    y0, y1 = ROWS[0][0] + 4, ROWS[1][1] - 4
    shape = [(x0, y0), (x1, y0), (x1, MID - 26), (STRIP[1] + 4, MID), (x1, MID + 26), (x1, y1), (x0, y1)]
    poly(d, shape, fill=DEEP_RED + (255,), outline=GOLD + (255,), width=2)
    f = font(CINZEL, 13, 700)
    upright(top, 'ENEMY', f, NAME, x0, y0 + 4, x1, MID - 30, spacing=1)
    upright(top, 'ENEMY', f, NAME, x0, MID + 30, x1, y1 - 4, spacing=1)
    return over(panel, top)


def t3_swords(panel, fill, back=None, blades=(236, 222, 190), inner=False):
    """The tab without a word: crossed swords at its middle, a head above and below pointing at the enemy."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x0, x1 = STRIP[0] + 1, STRIP[1] + 3
    y0, y1 = ROWS[0][0] + 6, ROWS[1][1] - 6
    d.rectangle((*local(x0, y0), *local(x1, y1)), fill=(back or DEEP_RED) + (255,), outline=GOLD + (255,), width=1)
    if inner:
        d.rectangle((*local(x0 + 3, y0 + 3), *local(x1 - 3, y1 - 3)), outline=DIM_GOLD + (255,), width=1)
    cx = (x0 + x1) / 2
    for sx in (-1, 1):
        line(d, [(cx - 9 * sx, MID - 11), (cx + 9 * sx, MID + 11)], blades + (255,), 3)
        line(d, [(cx + 5 * sx, MID + 4), (cx + 1 * sx, MID + 8)], GOLD + (255,), 4)
    for cy in (y0 + 30, y1 - 30):
        arrowhead(d, x0 + 7, cy, 15, 9, NAME + (255,))
    return over(panel, top)


def t4_tab_rotated(panel, fill):
    """F6 in darker lacquer, two gold rules, the word turned and spaced, a gold head at each end."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x0, x1 = STRIP[0] + 1, STRIP[1] + 3
    y0, y1 = ROWS[0][0] + 6, ROWS[1][1] - 6
    d.rectangle((*local(x0, y0), *local(x1, y1)), fill=DEEP_RED + (255,))
    d.rectangle((*local(x0, y0), *local(x1, y1)), outline=GOLD + (255,), width=1)
    d.rectangle((*local(x0 + 3, y0 + 3), *local(x1 - 3, y1 - 3)), outline=DIM_GOLD + (255,), width=1)
    rotated(top, 'T O   T H E   E N E M Y', font(CINZEL, 12, 700), NAME, x0, y0 + 34, x1, y1 - 34)
    for cy in (y0 + 18, y1 - 18):
        arrowhead(d, x0 + 8, cy, 13, 8, GOLD + (255,))
    return over(panel, top)


def t5_bracket_tab(panel, fill):
    """F5 and F6 together: the bracket holding the front row runs into a short vermilion tab with 敵 and a head."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x, y0, y1 = STRIP[0] - 1, ROWS[0][0] + 2, ROWS[1][1] - 2
    line(d, [(CARD_X1 - 10, y0), (x, y0), (x, MID - 40)], GOLD + (255,), 2)
    line(d, [(CARD_X1 - 10, y1), (x, y1), (x, MID + 40)], GOLD + (255,), 2)
    tab = [(x - 2, MID - 40), (STRIP[1] - 4, MID - 40), (STRIP[1] - 4, MID - 20), (STRIP[1] + 5, MID), (STRIP[1] - 4, MID + 20),
           (STRIP[1] - 4, MID + 40), (x - 2, MID + 40)]
    poly(d, tab, fill=VERMILION + (255,), outline=GOLD + (255,), width=2)
    upright(top, '敵', font(GOTHIC, 20), (250, 226, 160), x - 2, MID - 14, STRIP[1] - 4, MID + 14)
    return over(panel, top)


ROUND_2 = [
    ('F5  round 1', v5_bracket),
    ('F6  round 1', v6_enemy_tab),
    ('B1  bracket, the old head', b1_bracket_triangle),
    ('T1  seal 敵 and chevrons', t1_seal),
    ('B2  double rule, lacquer head', b2_double),
    ('T2  arrow-shaped tab', t2_arrow_tab),
    ('B3  brace', b3_brace),
    ('T3  crossed swords', t3_swords),
    ('B4  lit front edge', b4_lit_front),
    ('T4  "to the enemy", framed', t4_tab_rotated),
    ('B5  bracket into a 敵 tab', t5_bracket_tab),
]

# ---------------------------------------------------------------- round 3: more of T3 (the crossed swords)

STEEL = (214, 214, 222)
STEEL_EDGE = (150, 150, 164)
GRIP = (58, 36, 22)
TAB = (STRIP[0] + 1, STRIP[1] + 3)
TAB_Y = (ROWS[0][0] + 6, ROWS[1][1] - 6)


def sword(d, pommel, tip, blade=3.2, guard=7, steel=STEEL):
    """A sword from its pommel to its tip: a gold pommel, a dark grip, a gold guard across, the blade to a point."""
    import math
    (px, py), (tx, ty) = pommel, tip
    length = math.hypot(tx - px, ty - py)
    ux, uy = (tx - px) / length, (ty - py) / length
    nx, ny = -uy, ux
    g = 0.24                                                   # the grip's share, the guard at its end
    gx, gy = px + ux * length * g, py + uy * length * g
    point = length * 0.86
    bx, by = px + ux * point, py + uy * point
    half = blade / 2
    poly(d, [(gx + nx * half, gy + ny * half), (bx + nx * half, by + ny * half), (tx, ty), (bx - nx * half, by - ny * half),
             (gx - nx * half, gy - ny * half)], fill=steel + (255,), outline=STEEL_EDGE + (255,))
    line(d, [(px, py), (gx, gy)], GRIP + (255,), 3)
    line(d, [(gx + nx * guard / 2, gy + ny * guard / 2), (gx - nx * guard / 2, gy - ny * guard / 2)], GOLD + (255,), 3)
    r = 2.2
    d.ellipse((*local(px - r, py - r), *local(px + r, py + r)), fill=NAME + (255,))


def crossed(d, cx, cy, w, h, **kw):
    """Two swords crossed at (cx, cy), their tips up: one from lower left to upper right, one mirrored."""
    sword(d, (cx - w / 2, cy + h / 2), (cx + w / 2, cy - h / 2), **kw)
    sword(d, (cx + w / 2, cy + h / 2), (cx - w / 2, cy - h / 2), **kw)


def tab(d, fill=DEEP_RED, frame=True, inner=False, shape=None):
    x0, x1 = TAB
    y0, y1 = TAB_Y
    points = shape or [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    poly(d, points, fill=fill + (255,), outline=GOLD + (255,) if frame else None, width=1)
    if inner:
        d.rectangle((*local(x0 + 3, y0 + 3), *local(x1 - 3, y1 - 3)), outline=DIM_GOLD + (255,), width=1)


def x1_proper_swords(panel, fill, back=DEEP_RED, steel=STEEL):
    """T3 with proper swords (gold guards and pommels, steel blades), a gold head above and below."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    tab(d, fill=back, inner=True)
    cx = (TAB[0] + TAB[1]) / 2
    crossed(d, cx, MID, 22, 40, steel=steel)
    for cy in (TAB_Y[0] + 26, TAB_Y[1] - 26):
        arrowhead(d, TAB[0] + 9, cy, 14, 9, GOLD + (255,))
    return over(panel, top)


def x2_arrow_tab(panel, fill, back=DEEP_RED, steel=STEEL, inner=False):
    """The tab drawn to a point at the rows' middle (T2's shape), the swords crossed in the point."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    x0, x1 = TAB[0], TAB[1] - 7
    y0, y1 = TAB_Y
    tab(d, fill=back, shape=[(x0, y0), (x1, y0), (x1, MID - 30), (STRIP[1] + 6, MID), (x1, MID + 30), (x1, y1), (x0, y1)])
    if inner:                                                  # X7's second rule, 3 pixels in
        poly(d, [(x0 + 3, y0 + 3), (x1 - 3, y0 + 3), (x1 - 3, MID - 31), (STRIP[1] + 1, MID), (x1 - 3, MID + 31), (x1 - 3, y1 - 3),
                 (x0 + 3, y1 - 3)], outline=DIM_GOLD + (255,), width=1)
    crossed(d, (x0 + x1) / 2 + 2, MID, 20, 40, steel=steel)
    for cy in (y0 + 22, y1 - 22):
        for dx in (0, 8):
            line(d, [(x0 + 6 + dx, cy - 6), (x0 + 12 + dx, cy), (x0 + 6 + dx, cy + 6)], GOLD + (255,), 2)
    return over(panel, top)


def x3_medallion(panel, fill, back=DEEP_RED, steel=STEEL):
    """No tab: the swords on a round vermilion medallion with a gold ring, double chevrons above and below."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    cx, r = (STRIP[0] + STRIP[1]) / 2 + 2, 19
    d.ellipse((*local(cx - r, MID - r), *local(cx + r, MID + r)), fill=back + (255,), outline=GOLD + (255,), width=2)
    d.ellipse((*local(cx - r + 4, MID - r + 4), *local(cx + r - 4, MID + r - 4)), outline=DIM_GOLD + (255,), width=1)
    crossed(d, cx, MID, 18, 22, blade=2.6, guard=6, steel=steel)
    for cy in (ROWS[0][0] + 34, ROWS[1][1] - 34):
        for dx in (0, 10):
            line(d, [(STRIP[0] + 4 + dx, cy - 8), (STRIP[0] + 12 + dx, cy), (STRIP[0] + 4 + dx, cy + 8)], GOLD + (255,), 3)
    return over(panel, top)


def x4_shield(panel, fill):
    """The swords crossed behind a small vermilion heater shield at the middle, heads above and below on the lacquer."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    cx = (STRIP[0] + STRIP[1]) / 2 + 2
    crossed(d, cx, MID - 2, 30, 46)
    w, h, y = 18, 22, MID - 8
    shield = [(cx - w / 2, y), (cx + w / 2, y), (cx + w / 2, y + h * 0.45), (cx, y + h), (cx - w / 2, y + h * 0.45)]
    poly(d, shield, fill=VERMILION + (255,), outline=GOLD + (255,), width=2)
    for cy in (ROWS[0][0] + 30, ROWS[1][1] - 30):
        arrowhead(d, STRIP[0] + 6, cy, 18, 11, GOLD + (255,), DARK + (255,))
    return over(panel, top)


def x5_chevron_banner(panel, fill):
    """The tab as a war banner: the swords at its middle, gold chevrons filling it above and below."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    tab(d)
    cx = (TAB[0] + TAB[1]) / 2
    crossed(d, cx, MID, 22, 38)
    for y in list(range(TAB_Y[0] + 12, int(MID) - 30, 16)) + list(range(int(MID) + 38, TAB_Y[1] - 6, 16)):
        line(d, [(TAB[0] + 9, y - 6), (TAB[0] + 17, y), (TAB[0] + 9, y + 6)], GOLD + (170,), 2)
    return over(panel, top)


def x6_spear_head(panel, fill):
    """No tab: two spears from the rows' outer corners meet at the strip's middle, their heads forming the arrow."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    tip = (STRIP[1] + 4, MID)
    for start in ((STRIP[0] - 2, ROWS[0][0] + 8), (STRIP[0] - 2, ROWS[1][1] - 8)):
        sword(d, start, tip, blade=4, guard=0, steel=STEEL)
    d.ellipse((*local(tip[0] - 15, MID - 4), *local(tip[0] - 7, MID + 4)), fill=VERMILION + (255,))   # the tassel where they meet
    return over(panel, top)


def x7_gold_only(panel, fill):
    """T3 without the red: dark lacquer framed in gold, the swords in gold, a gold head above and below."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    tab(d, fill=(30, 22, 16), inner=True)
    cx = (TAB[0] + TAB[1]) / 2
    crossed(d, cx, MID, 22, 40, steel=NAME)
    for cy in (TAB_Y[0] + 26, TAB_Y[1] - 26):
        arrowhead(d, TAB[0] + 9, cy, 14, 9, GOLD + (255,))
    return over(panel, top)


def x8_swords_point(panel, fill):
    """The swords not crossed but both pointing at the enemy, one over the other, on the red tab."""
    top = layer(panel)
    d = ImageDraw.Draw(top)
    tab(d, inner=True)
    y0, y1, n = TAB_Y[0] + 24, TAB_Y[1] - 24, 5
    for i in range(n):
        cy = y0 + (y1 - y0) * i / (n - 1)
        sword(d, (TAB[0] + 5, cy), (TAB[1] - 4, cy), blade=3.4, guard=10)
    return over(panel, top)


ROUND_3 = [
    ('T3  round 2', t3_swords),
    ('X1  proper swords, framed', x1_proper_swords),
    ('X2  arrow-shaped tab', x2_arrow_tab),
    ('X3  medallion, chevrons', x3_medallion),
    ('X4  shield on the swords', x4_shield),
    ('X5  banner of chevrons', x5_chevron_banner),
    ('X6  spears as the arrow', x6_spear_head),
    ('X7  gold, no red', x7_gold_only),
    ('X8  swords at the enemy', x8_swords_point),
]

# ---------------------------------------------------------------- round 4: T3, X1, X2, X3 without the red, as X7

LACQUER_TAB = (30, 22, 16)

ROUND_4 = [
    ('X7  round 3', x7_gold_only),
    ('D3  T3 on lacquer', lambda p, f: t3_swords(p, f, back=LACQUER_TAB, blades=NAME, inner=True)),
    ('D1  X1 on lacquer', lambda p, f: x1_proper_swords(p, f, back=LACQUER_TAB, steel=NAME)),
    ('D2  X2 on lacquer', lambda p, f: x2_arrow_tab(p, f, back=LACQUER_TAB, steel=NAME, inner=True)),
    ('D4  X3 on lacquer', lambda p, f: x3_medallion(p, f, back=LACQUER_TAB, steel=NAME)),
]

VARIANTS = [
    ('F0  now: the triangle', v0_current),
    ('F1  chevrons', v1_chevrons),
    ('F2  front line', v2_front_line),
    ('F3  axis of advance', v3_axis),
    ('F4  banner', v4_banner),
    ('F5  bracket', v5_bracket),
    ('F6  enemy tab', v6_enemy_tab),
    ('F7  header arrow', v7_header),
]


def main(dump_path, out_path, round_=1):
    variants = {2: ROUND_2, 3: ROUND_3, 4: ROUND_4}.get(round_, VARIANTS)
    dump = Image.open(dump_path).convert('RGBA')
    panel, fill = cut(dump)
    columns, gap, caption = 2, 16, 34
    w, h = panel.size
    rows = (len(variants) + columns - 1) // columns
    sheet = Image.new('RGBA', (columns * w + (columns + 1) * gap, rows * (h + caption) + (rows + 1) * gap), (8, 6, 4, 255))
    d = ImageDraw.Draw(sheet)
    f = font(GOTHIC, 20)
    for i, (name, draw) in enumerate(variants):
        x = gap + (i % columns) * (w + gap)
        y = gap + (i // columns) * (h + caption + gap)
        d.text((x + 4, y + 4), name, font=f, fill=NAME)
        sheet.alpha_composite(draw(panel, fill), (x, y + caption))
    sheet.convert('RGB').save(out_path)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else 1)
