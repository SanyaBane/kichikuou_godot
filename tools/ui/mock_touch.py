"""Mock-ups of the touch mode (owner, 2026-10-24: Android on phones too; docs/ui/touch.md): the battle's choice with an
action bar (B0-B3) and the map (M0-M2), drawn over window dumps of the week-18 save.

The dumps (a window; the run moves the system pointer):
  <Godot console exe> --path godot -- --load --auto --quit --seed 2 --saves user://saves-look-touch
      --slots-from user://saves-week18 --until round-2 --interface new --shots <dump dir>
(00081-order-flash.png: Rance's turn; 00083-banners-E1.png: his banner on Sakanaku; 00004-map-markers.png: the first
map wait; 00008-command-menu.png: the command panel; 00015-province-menu.png: Oaks's menu). Then, from the repository's
root (Pillow; godot/fonts):
  python -I tools/ui/mock_touch.py <dump dir> <out dir> < /dev/null
The images hold the units' portraits: keep <out dir> outside the repository.

A phone is a 6.5" 20:9 screen in landscape: 150.5 x 67.7 mm. The game's 1280x824 (the menu bar and the 1280x800 screen)
is fitted to its height, 0.0822 mm a pixel; the frame is then 824 * 20 / 9 = 1831 pixels wide, the screen centred
(GameScreen's view keeps the aspect), 275 free on each side. A tablet is the owner's Galaxy Tab S7 FE: 12.4" 16:10, 0.203 mm a pixel, no
free side. Everything here is in the frame's pixels, drawn at 2x and scaled down for smooth edges.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

DUMPS, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2

PHONE_W, H = 1831, 824
SIDE = (PHONE_W - 1280) // 2            # 275: the free room on each side of the screen on a 20:9 phone
MM = 0.0822                             # mm a frame pixel on the phone
TABLET_MM = 0.203

PANEL = (43, 45, 49)                    # the battle panel's colour (steel palette)
CARD = (59, 62, 68)
GOLD = (196, 158, 84)
GOLD_LIGHT = (242, 199, 92)
TEXT = (232, 232, 236)
DIM_TEXT = (150, 154, 162)
CYAN = (70, 205, 255)
BANNER_TOP, BANNER_FOOT = (255, 228, 90), (226, 128, 20)
HOT_TOP, HOT_FOOT = (255, 170, 90), (196, 70, 30)
RED_TOP, RED_FOOT = (150, 44, 36), (96, 22, 18)
PURPLE_TOP, PURPLE_FOOT = (110, 80, 150), (66, 44, 98)
STEEL_TOP, STEEL_FOOT = (78, 86, 100), (52, 56, 66)
LACQUER = (24, 18, 14)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)
INK = (34, 16, 4)


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), int(size * S))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
CINZEL = lambda size, w=700: font('Cinzel-VariableFont_wght.ttf', size, w)


def dump(name):
    return Image.open(os.path.join(DUMPS, name)).convert('RGB')


class Frame:
    """A frame of the device's pixels: the dump at x0, black around it."""

    def __init__(self, base, width=PHONE_W, x0=SIDE, menu_bar=False):
        self.w = width
        img = Image.new('RGB', (width, H), (0, 0, 0))
        img.paste(base, (x0, 0))
        if not menu_bar:
            ImageDraw.Draw(img).rectangle((0, 0, width, 23), fill=(0, 0, 0))
        self.x0 = x0
        self.img = img.resize((width * S, H * S), Image.LANCZOS).convert('RGBA')

    def layer(self):
        lay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay):
        self.img = Image.alpha_composite(self.img, lay)

    def x(self, v):
        """A dump's x in the frame."""
        return v + self.x0

    def save(self, name):
        out = self.img.resize((self.w, H), Image.LANCZOS).convert('RGB')
        out.save(os.path.join(OUT, name))
        return out


def sc(*v):
    return [a * S for a in v]


def text(f, x, y, t, fnt, colour, anchor='la', stroke=0, stroke_colour=INK):
    lay, d = f.layer()
    d.text((x * S, y * S), t, font=fnt, fill=colour, anchor=anchor, stroke_width=int(stroke * S),
           stroke_fill=stroke_colour)
    f.add(lay)


def rect(f, r, fill=None, outline=None, width=1, radius=0):
    lay, d = f.layer()
    d.rounded_rectangle(sc(*r), radius=radius * S, fill=fill, outline=outline, width=int(width * S))
    f.add(lay)


def gradient(f, r, top, foot, radius=8, alpha=255, outline=(255, 255, 255), width=2, shape=None):
    """A box (or a pennant: shape='pennant') filled top to foot."""
    x0, y0, x1, y1 = r
    g = Image.new('RGBA', ((x1 - x0) * S, (y1 - y0) * S))
    gd = ImageDraw.Draw(g)
    for y in range(g.height):
        t = y / max(1, g.height - 1)
        gd.line((0, y, g.width, y), fill=tuple(int(a + (b - a) * t) for a, b in zip(top, foot)) + (alpha,))
    mask = Image.new('L', g.size, 0)
    md = ImageDraw.Draw(mask)
    if shape == 'pennant':
        w, h, p = g.width, g.height, 18 * S
        poly = [(12 * S, 0), (w - p, 0), (w - 1, h // 2), (w - p, h - 1), (0, h - 1)]
        md.polygon(poly, fill=255)
    else:
        md.rounded_rectangle((0, 0, g.width - 1, g.height - 1), radius=radius * S, fill=255)
    lay = Image.new('RGBA', f.img.size, (0, 0, 0, 0))
    shadow = Image.new('RGBA', g.size, (0, 0, 0, 90))
    lay.paste(shadow, ((x0 + 3) * S, (y0 + 4) * S), mask)
    lay.paste(g, (x0 * S, y0 * S), mask)
    f.add(lay)
    if outline is not None:
        lay, d = f.layer()
        if shape == 'pennant':
            d.polygon([((x0 * S) + a, (y0 * S) + b) for a, b in poly], outline=outline, width=int(width * S))
        else:
            d.rounded_rectangle(sc(x0, y0, x1 - 1, y1 - 1), radius=radius * S, outline=outline, width=int(width * S))
        f.add(lay)


def button(f, r, label, kind='steel', sub=None, hot=False, size=26):
    top, foot = {'steel': (STEEL_TOP, STEEL_FOOT), 'gold': (BANNER_TOP, BANNER_FOOT), 'red': (RED_TOP, RED_FOOT),
                 'purple': (PURPLE_TOP, PURPLE_FOOT)}[kind]
    if hot:
        top, foot = (HOT_TOP, HOT_FOOT) if kind == 'gold' else (tuple(min(255, c + 40) for c in top), foot)
        glow(f, r, (255, 184, 108), 14)
    border = (255, 255, 255) if kind == 'gold' else GOLD
    gradient(f, r, top, foot, radius=10, outline=border, width=2)
    cx, cy = (r[0] + r[2]) / 2, (r[1] + r[3]) / 2
    colour = (255, 255, 255)
    if sub:
        text(f, cx, cy - 11, label, GOTHIC(size), colour, 'mm', stroke=3 if kind == 'gold' else 0)
        text(f, cx, cy + 22, sub, GOTHIC(15), (255, 240, 210) if kind == 'gold' else DIM_TEXT, 'mm',
             stroke=2 if kind == 'gold' else 0)
    else:
        text(f, cx, cy, label, GOTHIC(size), colour, 'mm', stroke=4 if kind == 'gold' else 0)


def glow(f, r, colour, spread):
    lay, d = f.layer()
    d.rounded_rectangle(sc(r[0] - spread // 2, r[1] - spread // 2, r[2] + spread // 2, r[3] + spread // 2),
                        radius=12 * S, outline=colour + (150,), width=int(spread * S))
    f.add(lay.filter(ImageFilter.GaussianBlur(6 * S)))


def brackets(f, r, colour=CYAN):
    """The target's frame: cyan brackets on the card's corners (BattleCommandLayer.DrawTargetFrame)."""
    x0, y0, x1, y1 = r[0] - 3, r[1] - 3, r[2] + 3, r[3] + 3
    for blur, width, alpha in ((8, 14, 90), (0, 4, 255)):
        lay, d = f.layer()
        for (cx, cy, hx, vy) in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
            d.line(sc(cx, cy + vy * 46, cx, cy, cx + hx * 60, cy), fill=colour + (alpha,), width=int(width * S),
                   joint='curve')
        f.add(lay.filter(ImageFilter.GaussianBlur(blur * S)) if blur else lay)


def dim(f, r, alpha=150):
    rect(f, r, fill=(16, 17, 20, alpha))


def rail(f, x0, x1, top=24):
    rect(f, (x0, top, x1, H), fill=PANEL + (255,))
    lay, d = f.layer()
    edge = x1 - 1 if x0 == 0 else x0
    d.line(sc(edge, top, edge, H), fill=GOLD + (200,), width=2 * S)
    f.add(lay)


def menu_button(f, x, y):
    """The menu bar's replacement on touch: one button for File / Screen / Option / MsgSkip."""
    gradient(f, (x, y, x + 84, y + 72), STEEL_TOP, STEEL_FOOT, radius=10, outline=GOLD)
    for i in range(3):
        rect(f, (x + 24, y + 22 + i * 13, x + 60, y + 27 + i * 13), fill=TEXT + (255,), radius=2)


def note(f, x, y, t, anchor='la', size=17):
    text(f, x, y, t, GOTHIC(size), NOTE, anchor, stroke=3, stroke_colour=(0, 0, 0))


def measure(f, r, label, side='right'):
    """A note's frame round a target and its size."""
    lay, d = f.layer()
    d.rectangle(sc(*r), outline=NOTE + (255,), width=2 * S)
    f.add(lay)
    if side == 'right':
        note(f, r[2] + 6, (r[1] + r[3]) / 2, label, 'lm')
    elif side == 'left':
        note(f, r[0] - 6, (r[1] + r[3]) / 2, label, 'rm')
    elif side == 'below':
        note(f, (r[0] + r[2]) / 2, r[3] + 6, label, 'ma')
    else:
        note(f, (r[0] + r[2]) / 2, r[1] - 6, label, 'md')


def finger(f, x, y, scale=MM):
    """A finger's tip, 8 mm across, for scale."""
    d_px = 8 / scale
    lay, d = f.layer()
    d.ellipse(sc(x - d_px / 2, y - d_px / 2, x + d_px / 2, y + d_px / 2), fill=(255, 210, 180, 110),
              outline=NOTE + (255,), width=2 * S)
    f.add(lay)
    note(f, x, y + d_px / 2 + 4, 'finger 8 mm', 'ma', 15)


def mm(px, scale=MM):
    return f'{px * scale:.1f} mm'


# ---------------------------------------------------------------------------------------------------- the battle
# Card rectangles in the dump (the menu bar's 24 included).
P_BACK1, P_FRONT1 = (10, 474, 292, 624), (302, 474, 584, 624)
P_BACK2, P_FRONT2 = (10, 632, 292, 782), (302, 632, 584, 782)
E_FRONT1, E_BACK1 = (696, 474, 978, 624), (988, 474, 1270, 624)
E_FRONT2, E_BACK2 = (696, 632, 978, 782), (988, 632, 1270, 782)


def at(f, r):
    return (f.x(r[0]), r[1], f.x(r[2]), r[3])


def battle_now():
    f = Frame(dump('00083-banners-E1.png'), menu_bar=True)
    measure(f, (f.x(674), 483, f.x(876), 525), f'banner 40 px = {mm(40)}', 'above')
    measure(f, (f.x(0), 0, f.x(330), 24), f'menu bar 24 px = {mm(24)}', 'right')
    measure(f, at(f, P_FRONT2), f'card 150 px = {mm(150)}', 'below')
    note(f, f.x(10), 448, f'card text 17-20 px = {mm(17)}-{mm(20)} high', 'ld')
    note(f, SIDE / 2, 380, f'{SIDE} px free', 'mm', 20)
    note(f, SIDE / 2, 410, f'= {mm(SIDE)}', 'mm', 20)
    note(f, PHONE_W - SIDE / 2, 380, f'{SIDE} px free', 'mm', 20)
    note(f, PHONE_W - SIDE / 2, 410, f'= {mm(SIDE)}', 'mm', 20)
    finger(f, SIDE / 2, 560)
    return f.save('B0-phone-now.png')


def battle_rails(tablet=False):
    """B1 (phone) / B2 (tablet): the target first, two rails — the acting unit's own commands left, the attacks on the
    target right; a tap on an enemy card makes it the target."""
    base = dump('00081-order-flash.png')
    if tablet:
        f = Frame(base, width=1280, x0=0)
    else:
        f = Frame(base)
    brackets(f, at(f, E_FRONT1))
    dim(f, at(f, E_BACK1))
    note(f, f.x(1129), 600, 'out of reach', 'mm', 16) if not tablet else None
    if tablet:
        # the scene's blurred sides (128 each) hold the rails
        lx0, lx1, rx0, rx1, top, bottom = 0, 128, 1152, 1280, 24, 434
        rect(f, (lx0, top, lx1, bottom), fill=PANEL + (235,))
        rect(f, (rx0, top, rx1, bottom), fill=PANEL + (235,))
        menu_button(f, 22, 32)
        bw, bh, gap = 112, 56, 8
        y = 118
        text(f, 64, y, "Rance", GOTHIC(17), GOLD_LIGHT, 'mm')
        y += 18
        for label, kind in (('Defend', 'steel'), ('Wait', 'steel'), ('Auto-Atk', 'steel'), ('Auto-Def', 'steel'),
                            ('Retreat', 'red')):
            button(f, (8, y, 8 + bw, y + bh), label, kind, size=18)
            y += bh + gap
        text(f, 1216, 40, 'TARGET', CINZEL(13), DIM_TEXT, 'mm')
        text(f, 1216, 64, 'Sakanaku', GOTHIC(18), (150, 225, 255), 'mm')
        y = 92
        for label in ('Bow', 'Melee', 'W. Shot'):
            button(f, (1160, y, 1160 + bw, y + 64), label, 'gold', size=20)
            y += 64 + 10
        note(f, 640, 448, f'tablet: rails in the scene\'s blurred sides; a button {bw}x{bh} = '
                          f'{bw * TABLET_MM:.0f}x{bh * TABLET_MM:.0f} mm', 'mm')
        return f.save('B2-tablet-rails.png')
    # the phone: the free sides are the rails
    rail(f, 0, SIDE)
    rail(f, PHONE_W - SIDE, PHONE_W)
    menu_button(f, 16, 12)
    face = base.crop((309, 481, 421, 593)).resize((84, 84), Image.LANCZOS)
    lay, _ = f.layer()
    lay.paste(face.resize((84 * S, 84 * S), Image.LANCZOS), (18 * S, 248 * S))
    f.add(lay)
    rect(f, (18, 248, 102, 332), outline=GOLD_LIGHT + (255,), width=2, radius=0)
    text(f, 116, 270, "Rance", GOTHIC(26), GOLD_LIGHT, 'lm')
    text(f, 116, 306, 'acting', GOTHIC(18), DIM_TEXT, 'lm')
    bw, bh, gap = SIDE - 32, 82, 10
    y = H - 16 - 5 * bh - 4 * gap
    for label, kind, sub in (('Defend', 'steel', None), ('Wait', 'steel', '◆ focus +1'), ('Auto-Atk', 'steel', None),
                             ('Auto-Def', 'steel', None), ('Retreat', 'red', None)):
        button(f, (16, y, 16 + bw, y + bh), label, kind, sub=sub)
        y += bh + gap
    rx = PHONE_W - SIDE
    text(f, rx + SIDE / 2, 214, 'TARGET', CINZEL(17), DIM_TEXT, 'mm')
    text(f, rx + SIDE / 2, 252, 'Sakanaku', GOTHIC(30), (150, 225, 255), 'mm')
    text(f, rx + SIDE / 2, 290, 'tap an enemy card', GOTHIC(16), DIM_TEXT, 'mm')
    text(f, rx + SIDE / 2, 312, 'to change it', GOTHIC(16), DIM_TEXT, 'mm')
    bh2, gap2 = 96, 12
    y = H - 16 - 3 * bh2 - 2 * gap2
    for label in ('Bow', 'Melee', 'W. Shot'):
        button(f, (rx + 16, y, rx + 16 + bw, y + bh2), label, 'gold', size=30)
        y += bh2 + gap2
    note(f, PHONE_W / 2, 446, f'a rail button {bw}x{bh} = {mm(bw)} x {mm(bh)}; an attack {mm(bh2)} high', 'mm')
    return f.save('B1-phone-rails.png')


def battle_action_first():
    """B3 (phone): the action first — one rail at the right thumb with every command; an attack chosen lights the
    cards it reaches and a tap on one of them attacks."""
    base = dump('00081-order-flash.png')
    f = Frame(base)
    rail(f, 0, SIDE)
    rail(f, PHONE_W - SIDE, PHONE_W)
    menu_button(f, 16, 12)
    for r in (E_FRONT1, E_FRONT2):
        brackets(f, at(f, r))
    dim(f, at(f, E_BACK1))
    text(f, f.x(837), 452, 'tap a target', GOTHIC(20), (150, 225, 255), 'mm', stroke=3, stroke_colour=(0, 0, 0))
    bw = SIDE - 32
    button(f, (16, H - 16 - 96, 16 + bw, H - 16), 'Cancel', 'steel', sub='back to the commands')
    rx = PHONE_W - SIDE
    text(f, rx + SIDE / 2, 100, "Rance's turn", GOTHIC(24), GOLD_LIGHT, 'mm')
    y, bh, gap = 132, 74, 8
    for label, kind, hot in (('Bow', 'gold', False), ('Melee', 'gold', True), ('W. Shot', 'gold', False)):
        button(f, (rx + 16, y, rx + 16 + bw, y + bh), label, kind, hot=hot, size=28)
        y += bh + gap
    y += 14
    for label, kind in (('Defend', 'steel'), ('Wait', 'steel'), ('Orders…', 'steel')):
        button(f, (rx + 16, y, rx + 16 + bw, y + bh), label, kind, size=24)
        y += bh + gap
    note(f, rx + SIDE / 2, y + 4, 'Orders: Auto-Atk, Auto-Def,', 'ma', 15)
    note(f, rx + SIDE / 2, y + 24, 'Retreat in a second list', 'ma', 15)
    return f.save('B3-phone-action-first.png')


# ---------------------------------------------------------------------------------------------------- the map
OAKS_PLATE = (887, 600, 937, 630)


def map_now():
    f = Frame(dump('00004-map-markers.png'), menu_bar=True)
    measure(f, (f.x(887), 600, f.x(937), 630), f'plate 30 px = {mm(30)}', 'below')
    measure(f, (f.x(493), 678, f.x(536), 708), f'plate {mm(30)}', 'left')
    note(f, f.x(640), 790, 'no right button: the command panel only by Android\'s Back', 'mm')
    note(f, SIDE / 2, 380, f'{SIDE} px free', 'mm', 20)
    note(f, PHONE_W - SIDE / 2, 380, f'{SIDE} px free', 'mm', 20)
    finger(f, PHONE_W - SIDE / 2, 560)
    return f.save('M0-phone-now.png')


def map_rails():
    """M1 (phone): left rail the week's status and Commands; a tap on a province selects it (its plate lit, a hit area
    of a finger round its marker) and the right rail shows its card and its actions."""
    f = Frame(dump('00004-map-markers.png'))
    menu = dump('00015-province-menu.png')
    rail(f, 0, SIDE)
    rail(f, PHONE_W - SIDE, PHONE_W)
    menu_button(f, 16, 12)
    # the status strip's lines, stacked
    lines = (('LP 3 · Month 5', CINZEL(20), GOLD_LIGHT), ('Week 2', CINZEL(20), GOLD_LIGHT),
             ('Budget', GOTHIC(16), DIM_TEXT), ('116,400K GOLD', GOTHIC(22), TEXT),
             ('Troops', GOTHIC(16), DIM_TEXT), ('6,251', GOTHIC(22), TEXT))
    y = 118
    for t, fnt, colour in lines:
        text(f, 22, y, t, fnt, colour, 'lm')
        y += 32 if fnt.size > 18 * S else 26
    bw = SIDE - 32
    button(f, (16, H - 16 - 2 * 96 - 12, 16 + bw, H - 16 - 96 - 12), 'Commands', 'steel', size=28,
           sub='Harem, Troops, Save…')
    button(f, (16, H - 16 - 96, 16 + bw, H - 16), 'Turn END', 'red', size=28)
    # Oaks selected: its plate lit, a finger's hit area round its marker
    ox0, oy0, ox1, oy1 = OAKS_PLATE
    glow(f, (f.x(ox0), oy0, f.x(ox1), oy1), (255, 200, 110), 10)
    rect(f, (f.x(ox0), oy0, f.x(ox1), oy1), outline=GOLD_LIGHT + (255,), width=3, radius=4)
    lay, d = f.layer()
    cx, cy, rr = f.x(925), 592, 8 / MM / 2
    d.ellipse(sc(cx - rr, cy - rr, cx + rr, cy + rr), outline=NOTE + (255,), width=2 * S)
    f.add(lay)
    note(f, cx, cy + rr + 4, 'hit area', 'ma', 15)
    rx = PHONE_W - SIDE
    pic = menu.crop((423, 289, 649, 438)).resize((SIDE - 32, int((SIDE - 32) * 149 / 226)), Image.LANCZOS)
    lay, _ = f.layer()
    lay.paste(pic.resize((pic.width * S, pic.height * S), Image.LANCZOS), ((rx + 16) * S, 40 * S))
    f.add(lay)
    rect(f, (rx + 16, 40, rx + 16 + pic.width, 40 + pic.height), outline=GOLD + (255,), width=2)
    y = 40 + pic.height + 34
    text(f, rx + 22, y, 'Oaks', CINZEL(28), GOLD_LIGHT, 'lm')
    text(f, rx + 22, y + 32, 'Rebel Army', CINZEL(15, 500), DIM_TEXT, 'lm')
    y += 66
    for k, v in (('Economy', '??'), ('Defense', '??'), ('Battle Area', '??')):
        text(f, rx + 22, y, k, GOTHIC(17), DIM_TEXT, 'lm')
        text(f, PHONE_W - 22, y, v, GOTHIC(19), TEXT, 'rm')
        y += 30
    button(f, (rx + 16, H - 16 - 2 * 96 - 12, rx + 16 + bw, H - 16 - 96 - 12), 'Attack', 'red', size=30)
    button(f, (rx + 16, H - 16 - 96, rx + 16 + bw, H - 16), 'Ninja Maneuver', 'purple', size=24)
    return f.save('M1-phone-rails.png')


def map_floating():
    """M2 (phone): the map as now, two floating buttons in the free sides; a tap on a province opens its menu as now,
    the menu half as large again."""
    f = Frame(dump('00015-province-menu.png'))
    base_map = dump('00004-map-markers.png')
    menu_box = (405, 270, 876, 580)
    f2 = Frame(base_map)
    f.img = f2.img                                  # the map without the menu, the menu drawn larger below
    big = dump('00015-province-menu.png').crop(menu_box)
    k = 1.5
    big = big.resize((int(big.width * k), int(big.height * k)), Image.LANCZOS)
    x, y = (PHONE_W - big.width) // 2, 24 + (800 - big.height) // 2
    rect(f, (0, 24, PHONE_W, H), fill=(0, 0, 0, 110))
    lay, _ = f.layer()
    lay.paste(big.resize((big.width * S, big.height * S), Image.LANCZOS), (x * S, y * S))
    f.add(lay)
    menu_button(f, 16, 12)
    gradient(f, (PHONE_W - 16 - 200, H - 16 - 120, PHONE_W - 16, H - 16), STEEL_TOP, STEEL_FOOT, radius=60,
             outline=GOLD)
    text(f, PHONE_W - 116, H - 76, 'Commands', GOTHIC(28), TEXT, 'mm')
    note(f, PHONE_W / 2, y + big.height + 8, f'the province menu x{k}: a row 30 px -> {int(30 * k)} px = {mm(30 * k)}', 'ma')
    return f.save('M2-phone-floating.png')


def map_tablet():
    """M3 (tablet): the map and the menu bar as now, one round Commands button at the bottom right."""
    f = Frame(dump('00004-map-markers.png'), width=1280, x0=0, menu_bar=True)
    r = (1280 - 16 - 190, H - 16 - 84, 1280 - 16, H - 16)
    gradient(f, r, STEEL_TOP, STEEL_FOOT, radius=42, outline=GOLD)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2, 'Commands', GOTHIC(26), TEXT, 'mm')
    note(f, r[2], r[1] - 8, f'190x84 = {190 * TABLET_MM:.0f}x{84 * TABLET_MM:.0f} mm', 'rd', 15)
    measure(f, (887, 600, 937, 630), f'plate {30 * TABLET_MM:.1f} mm', 'left')
    return f.save('M3-tablet-map.png')


def sheet(names, out, scale=0.5):
    imgs = [Image.open(os.path.join(OUT, n)) for n in names]
    w = max(i.width for i in imgs)
    tiles = [i.resize((int(i.width * scale), int(i.height * scale)), Image.LANCZOS) for i in imgs]
    hgt = sum(t.height for t in tiles) + 12 * (len(tiles) - 1)
    s = Image.new('RGB', (int(w * scale), hgt), (255, 255, 255))
    y = 0
    for t in tiles:
        s.paste(t, (0, y))
        y += t.height + 12
    s.save(os.path.join(OUT, out))


battle_now()
battle_rails()
battle_rails(tablet=True)
battle_action_first()
map_now()
map_rails()
map_floating()
map_tablet()
print('done')
