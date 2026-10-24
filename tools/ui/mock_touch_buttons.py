"""Mock-ups of the touch mode's own buttons on the map and the battle screen, round 1 (owner, 2026-10-24: a new look for
the map's Commands/Close button, the battle's rails and its target frame; the battle's Skip stays as it is;
docs/ui/touch.md, "The map's and the battle's buttons, round 1"), drawn over window dumps of the week-18 save on the
tablet's frame (1280x824, the menu bar included).

The dumps (a window; the runs move the system pointer):
  <Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look
      --slots-from user://saves-week18 --until battle --shots <clean dir>
(00004-map-markers.png: the first map wait; 00073-retreat.png, 00074-advantage-tip-1.png, 00075-banners-E1.png: Rance's
turn, pieced together into one without a tip, a dialog or the banners), and
  <Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look-touch
      --slots-from user://saves-week18 --until round-2 --touch --shots <touch dir>
(00010-touch-close.png: the command panel with today's Close; 00076-touch-rails.png: the battle's Skip as it is). Then,
from the repository's root (Pillow; godot/fonts):
  python -I tools/ui/mock_touch_buttons.py <clean dir> <touch dir> <out dir> < /dev/null
The images hold the units' portraits: keep <out dir> outside the repository.

Three looks, each on the map (Commands, Close) and in the battle (the rails and the target): A the lacquer and gold of
our menus and the dialogue's drawer (godot/Strategy/Lacquer), B the battle screen's steel (DeploymentPalette.Steel)
made crisper, C round seals with icons first. In the dump's pixels: the 1280x800 screen starts at y 24.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageStat

CLEAN, TOUCH, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H, BAR = 1280, 824, 24
TABLET_MM = 0.203

# Lacquer (godot/Strategy/Lacquer.cs).
PANEL = (22, 16, 12)
GOLD, DIM_GOLD = (196, 160, 86), (110, 86, 46)
NAME, SUBTITLE, TEXT, QUIET = (232, 199, 122), (160, 140, 104), (244, 232, 206), (176, 160, 130)
ROW = (34, 26, 20)
VERMILION, VERMILION_LINE = (118, 30, 24), (214, 150, 90)
LIT_TOP, LIT_FOOT, LIT_INK = (255, 214, 110), (214, 140, 40), (40, 24, 8)
# The steel palette (DeploymentPalette.Steel) and the battle's colours.
STEEL, STEEL_CARD, STEEL_TOP, STEEL_FOOT = (43, 45, 49), (59, 62, 68), (78, 86, 100), (52, 56, 66)
STEEL_GOLD, STEEL_TEXT, STEEL_DIM = (201, 168, 90), (244, 230, 192), (150, 154, 162)
RED_TOP, RED_FOOT = (196, 72, 56), (126, 30, 24)
CYAN = (90, 210, 255)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)

SCENE = (128, BAR, 1152, BAR + 410)    # the battle scene's own picture; the rails stand in the blurred sides
LEFT_RAIL, RIGHT_RAIL = (0, BAR, 128, BAR + 410), (1152, BAR, 1280, BAR + 410)
TARGET_CARD = (696, 474, 978, 624)      # Sakanaku (commander 6), the preselected target
ACTING = 'Rance'
COMMANDS = ['Defend', 'Wait', 'Auto-Atk', 'Auto-Def', 'Retreat']
ATTACKS = ['Melee']


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), int(size * S))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)
CINZEL = lambda size, w=600: font('Cinzel-VariableFont_wght.ttf', size, w)


def mm(px):
    return f'{px * TABLET_MM:.1f} mm'


class Frame:
    def __init__(self, img):
        self.img = img.resize((W * S, H * S), Image.LANCZOS).convert('RGBA')

    def draw(self):
        lay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay, blur=0):
        self.img = Image.alpha_composite(self.img, lay.filter(ImageFilter.GaussianBlur(blur * S)) if blur else lay)

    def save(self, name):
        self.img.resize((W, H), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def sc(*v):
    return [round(a * S) for a in v]


def load(folder, name):
    return Image.open(os.path.join(folder, name)).convert('RGB')


def text(f, x, y, t, fnt, colour, anchor='mm'):
    lay, d = f.draw()
    d.text((x * S, y * S), t, font=fnt, fill=colour, anchor=anchor)
    f.add(lay)


def note(f, x, y, t, anchor='la', size=15):
    lay, d = f.draw()
    fnt = GOTHIC(size)
    l, t0, r, b = d.textbbox((x * S, y * S), t, font=fnt, anchor=anchor)
    d.rectangle((l - 5 * S, t0 - 3 * S, r + 5 * S, b + 3 * S), fill=(0, 0, 0, 215))
    d.text((x * S, y * S), t, font=fnt, fill=NOTE, anchor=anchor)
    f.add(lay)


def caption(f, lines, y=40, x=16):
    for i, t in enumerate(lines):
        note(f, x, y + i * 23, t, size=17 if i == 0 else 14)


def shadow(f, r, radius=0, spread=4):
    lay, d = f.draw()
    d.rounded_rectangle(sc(r[0] + 2, r[1] + 4, r[2] + 2, r[3] + 4), radius=radius * S, fill=(0, 0, 0, 130))
    f.add(lay, blur=spread)


def gradient(f, r, top, foot, radius=0, alpha=255, ellipse=False):
    x0, y0, x1, y1 = [round(v) for v in r]
    g = Image.new('RGBA', ((x1 - x0) * S, (y1 - y0) * S))
    gd = ImageDraw.Draw(g)
    for y in range(g.height):
        t = y / max(1, g.height - 1)
        gd.line((0, y, g.width, y), fill=tuple(int(a + (b - a) * t) for a, b in zip(top, foot)) + (alpha,))
    mask = Image.new('L', g.size, 0)
    md = ImageDraw.Draw(mask)
    if ellipse:
        md.ellipse((0, 0, g.width - 1, g.height - 1), fill=255)
    else:
        md.rounded_rectangle((0, 0, g.width - 1, g.height - 1), radius=radius * S, fill=255)
    lay = Image.new('RGBA', f.img.size, (0, 0, 0, 0))
    lay.paste(g, (x0 * S, y0 * S), mask)
    f.add(lay)


def outline(f, r, colour, width=2, radius=0, ellipse=False):
    lay, d = f.draw()
    if ellipse:
        d.ellipse(sc(*r), outline=colour + (255,), width=round(width * S))
    else:
        d.rounded_rectangle(sc(*r), radius=radius * S, outline=colour + (255,), width=round(width * S))
    f.add(lay)


def studs(f, r):
    lay, d = f.draw()
    for cx, cy in ((r[0], r[1]), (r[2], r[1]), (r[0], r[3]), (r[2], r[3])):
        d.polygon(sc(cx, cy - 4, cx + 4, cy, cx, cy + 4, cx - 4, cy), fill=GOLD)
    f.add(lay)


def glow(f, r, colour, spread=8, radius=0, ellipse=False, alpha=190):
    lay, d = f.draw()
    box = sc(r[0] - spread, r[1] - spread, r[2] + spread, r[3] + spread)
    if ellipse:
        d.ellipse(box, outline=colour + (alpha,), width=round(spread * 1.4 * S))
    else:
        d.rounded_rectangle(box, radius=(radius + spread) * S, outline=colour + (alpha,), width=round(spread * 1.4 * S))
    f.add(lay, blur=spread * 0.8)


# ------------------------------------------------------------------------------------------------- icons
def icon(f, kind, cx, cy, s, colour):
    """Small icons drawn with lines and polygons (no font glyphs): s is the icon's height."""
    lay, d = f.draw()
    c = colour + (255,)
    w = max(2, s * 0.12)
    h = s / 2
    if kind == 'Defend':                                    # a shield
        d.polygon(sc(cx - h * 0.8, cy - h, cx + h * 0.8, cy - h, cx + h * 0.8, cy, cx, cy + h, cx - h * 0.8, cy), fill=c)
    elif kind == 'Wait':                                    # an hourglass
        d.polygon(sc(cx - h * 0.7, cy - h, cx + h * 0.7, cy - h, cx, cy, cx + h * 0.7, cy + h, cx - h * 0.7, cy + h, cx, cy),
                  fill=c)
    elif kind in ('Auto-Atk', 'Melee'):                     # a sword, point up
        d.line(sc(cx, cy - h, cx, cy + h * 0.5), fill=c, width=round(w * S))
        d.line(sc(cx - h * 0.5, cy + h * 0.35, cx + h * 0.5, cy + h * 0.35), fill=c, width=round(w * S))
        d.line(sc(cx, cy + h * 0.5, cx, cy + h), fill=c, width=round(w * 1.4 * S))
    elif kind == 'Auto-Def':                                # a shield with a bar
        d.polygon(sc(cx - h * 0.8, cy - h, cx + h * 0.8, cy - h, cx + h * 0.8, cy, cx, cy + h, cx - h * 0.8, cy), outline=c,
                  width=round(w * S))
        d.line(sc(cx, cy - h * 0.7, cx, cy + h * 0.6), fill=c, width=round(w * S))
    elif kind == 'Retreat':                                 # a flag
        d.line(sc(cx - h * 0.6, cy - h, cx - h * 0.6, cy + h), fill=c, width=round(w * S))
        d.polygon(sc(cx - h * 0.6, cy - h, cx + h * 0.8, cy - h * 0.55, cx - h * 0.6, cy - h * 0.1), fill=c)
    elif kind == 'Commands':                                # three rows
        for i in range(3):
            y = cy - h * 0.7 + i * h * 0.7
            d.rounded_rectangle(sc(cx - h, y - w / 2, cx + h, y + w / 2), radius=w / 2 * S, fill=c)
    elif kind == 'Close':                                   # a cross
        d.line(sc(cx - h * 0.7, cy - h * 0.7, cx + h * 0.7, cy + h * 0.7), fill=c, width=round(w * 1.2 * S))
        d.line(sc(cx - h * 0.7, cy + h * 0.7, cx + h * 0.7, cy - h * 0.7), fill=c, width=round(w * 1.2 * S))
    f.add(lay)


# ------------------------------------------------------------------------------------------------- the bases
def battle_base():
    """Rance's turn without a tip, a dialog, banners or rails: the scene with the target's army (the banner dump), the
    cards from the tip's dump, Jibul's card under the tip from the retreat dump with its dimming undone, today's Skip."""
    tip, banners, retreat = load(CLEAN, '00074-advantage-tip-1.png'), load(CLEAN, '00075-banners-E1.png'), \
        load(CLEAN, '00073-retreat.png')
    img = tip.copy()
    img.paste(banners.crop((0, BAR, W, BAR + 410)), (0, BAR))
    # The dialog's dimming: the same factor everywhere on the cards (Cordoba's card measured).
    probe = (16, 480, 290, 620)
    a, b = tip.crop(probe), retreat.crop(probe)
    ka = ImageStat.Stat(a).sum
    kb = ImageStat.Stat(b).sum
    factor = [x / max(1, y) for x, y in zip(ka, kb)]
    patch = retreat.crop((916, 474, W, 614))
    patch = Image.merge('RGB', [ch.point(lambda v, k=k: min(255, round(v * k))) for ch, k in zip(patch.split(), factor)])
    img.paste(patch, (916, 474))
    img.paste(load(TOUCH, '00076-touch-rails.png').crop((0, 780, 200, H)), (0, 780))
    return img


def map_base():
    return load(CLEAN, '00004-map-markers.png')


def close_base():
    """The command panel in the touch mode with today's Close painted out by the same corner of the map."""
    img = load(TOUCH, '00010-touch-close.png')
    img.paste(map_base().crop((1060, 708, W, H)), (1060, 708))
    return img


# ------------------------------------------------------------------------------------------------- the map's button
MAP_BUTTON = (1044, 736, 1264, 808)     # A and B: 220x72, 16 in from the picture's right and bottom


def map_button_a(f, closes):
    r = MAP_BUTTON
    shadow(f, r, spread=5)
    gradient(f, r, (40, 30, 22), PANEL, alpha=245)
    outline(f, r, GOLD, 2)
    outline(f, (r[0] + 5, r[1] + 5, r[2] - 5, r[3] - 5), DIM_GOLD, 1)
    studs(f, r)
    cy = (r[1] + r[3]) / 2
    icon(f, 'Close' if closes else 'Commands', r[0] + 36, cy, 24, GOLD)
    text(f, (r[0] + 60 + r[2] - 10) / 2, cy + 1, 'Close' if closes else 'Commands', BRUSH(19), NAME)


def map_button_b(f, closes):
    r = MAP_BUTTON
    shadow(f, r, radius=10, spread=5)
    gradient(f, r, STEEL_TOP, STEEL_FOOT, radius=10)
    outline(f, r, STEEL_GOLD, 2, radius=10)
    lay, d = f.draw()
    d.line(sc(r[0] + 12, r[1] + 3, r[2] - 12, r[1] + 3), fill=(255, 255, 255, 50), width=S)   # the steel's sheen
    f.add(lay)
    cy = (r[1] + r[3]) / 2
    icon(f, 'Close' if closes else 'Commands', r[0] + 34, cy, 24, STEEL_GOLD)
    text(f, (r[0] + 58 + r[2] - 10) / 2, cy + 1, 'Close' if closes else 'Commands', GOTHIC(23), STEEL_TEXT)


SEAL_R = 46                             # C: a round seal 92 across (18.7 mm)


def seal(f, cx, cy, r, kind=None, lit=False, red=False):
    box = (cx - r, cy - r, cx + r, cy + r)
    shadow(f, box, radius=r, spread=5)
    if lit:
        glow(f, box, (255, 190, 90), spread=6, ellipse=True)
        gradient(f, box, LIT_TOP, LIT_FOOT, ellipse=True)
    elif red:
        gradient(f, box, RED_TOP, RED_FOOT, ellipse=True)
    else:
        gradient(f, box, (44, 34, 26), PANEL, ellipse=True)
    outline(f, box, (255, 244, 210) if lit else GOLD, 2.5, ellipse=True)
    outline(f, (box[0] + 5, box[1] + 5, box[2] - 5, box[3] - 5), (150, 100, 30) if lit else DIM_GOLD, 1, ellipse=True)
    return box


def map_button_c(f, closes):
    cx, cy = W - 16 - SEAL_R, H - 16 - SEAL_R - 16
    seal(f, cx, cy, SEAL_R)
    icon(f, 'Close' if closes else 'Commands', cx, cy - 8, 30, NAME)
    text(f, cx, cy + 24, 'Close' if closes else 'Commands', GOTHIC(13), QUIET)
    return (cx - SEAL_R, cy - SEAL_R, cx + SEAL_R, cy + SEAL_R)


def maps():
    for v, draw, name, desc in (
            ('A', map_button_a, 'lacquer', 'the lacquer and gold of our menus: a plate with the double gold frame and '
                                           'its studs, the brush font of Turn END'),
            ('B', map_button_b, 'steel', 'the battle screen\'s steel, crisper: a rounded plate, a sheen, an icon'),
            ('C', map_button_c, 'seal', 'a round lacquer seal, the icon first, a small label under it')):
        f = Frame(map_base())
        draw(f, False)
        size = (220, 72) if v != 'C' else (2 * SEAL_R, 2 * SEAL_R)
        caption(f, [f'{v}1 — the map\'s Commands: {desc}',
                    f'{size[0]}x{size[1]} = {mm(size[0])} x {mm(size[1])} (now 190x84 = 38.6 x 17.1 mm), '
                    'the same corner'])
        f.save(f'{v}1-map-commands-{name}.png')
        f = Frame(close_base())
        draw(f, True)
        caption(f, [f'{v}2 — the command panel open: the same button reads Close, its tap closes the panel'], y=640)
        f.save(f'{v}2-map-close-{name}.png')


# ------------------------------------------------------------------------------------------------- the battle
def rail_rows(rail, top, count, height, gap, width=112):
    x = (rail[0] + rail[2]) / 2 - width / 2
    return [(x, top + i * (height + gap), x + width, top + i * (height + gap) + height) for i in range(count)]


def brackets(f, r, colour, out=4, arm=26, width=4, blur_colour=None):
    """Corner brackets out of the card: an L at each corner."""
    x0, y0, x1, y1 = r[0] - out, r[1] - out, r[2] + out, r[3] + out
    lay, d = f.draw()
    for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        d.line(sc(cx, cy + sy * arm, cx, cy, cx + sx * arm, cy), fill=(blur_colour or colour) + (230,),
               width=round(width * 3 * S), joint='curve')
    f.add(lay, blur=4)
    lay, d = f.draw()
    for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        d.line(sc(cx, cy + sy * arm, cx, cy, cx + sx * arm, cy), fill=colour + (255,), width=round(width * S),
               joint='curve')
    f.add(lay)


def battle_a():
    """A: lacquer columns with the double gold frame; rows as the command panel's; Retreat vermilion; the attacks lit
    gold in the brush font; gold brackets with a diamond on the target."""
    f = Frame(battle_base())
    for rail in (LEFT_RAIL, RIGHT_RAIL):
        r = (rail[0] + 4, rail[1] + 4, rail[2] - 4, rail[3] - 4)
        gradient(f, r, (32, 24, 18), PANEL, alpha=242)
        outline(f, r, GOLD, 2)
        outline(f, (r[0] + 5, r[1] + 5, r[2] - 5, r[3] - 5), DIM_GOLD, 1)
        studs(f, r)
    text(f, 64, BAR + 30, ACTING, BRUSH(19), NAME)
    lay, d = f.draw()
    d.line(sc(20, BAR + 52, 108, BAR + 52), fill=DIM_GOLD + (255,), width=S)
    f.add(lay)
    for row, name in zip(rail_rows(LEFT_RAIL, BAR + 64, len(COMMANDS), 56, 10, 104), COMMANDS):
        if name == 'Retreat':
            gradient(f, row, VERMILION, (88, 22, 18))
            outline(f, row, GOLD, 2)
            outline(f, (row[0] + 4, row[1] + 4, row[2] - 4, row[3] - 4), VERMILION_LINE, 1)
            text(f, (row[0] + row[2]) / 2, (row[1] + row[3]) / 2 + 1, name, BRUSH(15), (250, 226, 160))
        else:
            gradient(f, row, ROW, ROW)
            outline(f, row, DIM_GOLD, 1)
            text(f, (row[0] + row[2]) / 2, (row[1] + row[3]) / 2, name, GOTHIC(18), TEXT)
    text(f, 1216, BAR + 24, 'TARGET', CINZEL(13), SUBTITLE)
    text(f, 1216, BAR + 48, 'Sakanaku', BRUSH(17), (150, 225, 255))
    lay, d = f.draw()
    d.line(sc(1172, BAR + 68, 1260, BAR + 68), fill=DIM_GOLD + (255,), width=S)
    f.add(lay)
    for row, name in zip(rail_rows(RIGHT_RAIL, BAR + 80, len(ATTACKS), 66, 10, 108), ATTACKS):
        glow(f, row, (255, 190, 90), spread=5, alpha=120)
        gradient(f, row, LIT_TOP, LIT_FOOT)
        outline(f, row, (255, 244, 210), 2)
        outline(f, (row[0] + 4, row[1] + 4, row[2] - 4, row[3] - 4), (150, 100, 30), 1)
        text(f, (row[0] + row[2]) / 2, (row[1] + row[3]) / 2 + 1, name, BRUSH(19), LIT_INK)
    brackets(f, TARGET_CARD, GOLD, blur_colour=(255, 200, 100))
    lay, d = f.draw()
    cx, y = (TARGET_CARD[0] + TARGET_CARD[2]) / 2, TARGET_CARD[1] - 4
    d.polygon(sc(cx, y - 7, cx + 7, y, cx, y + 7, cx - 7, y), fill=GOLD + (255,), outline=(255, 244, 210, 255))
    f.add(lay)
    caption(f, ['A — lacquer and gold: the rails as our panels (double gold frame, studs), the rows as the command '
                'panel\'s,', 'Retreat vermilion as Turn END, the attacks lit gold in the brush font; the target framed '
                'in gold, a diamond on top', f'rows 104x56 = {mm(104)} x {mm(56)}, attacks 108x66 = {mm(108)} x '
                f'{mm(66)}; the battle\'s Skip as it is'], y=318, x=140)
    f.save('A3-battle-lacquer.png')


def battle_b():
    """B: steel rails flush with the scene, the cards' buttons with icons; the attacks as the banners' gold chevrons
    pointing into the scene; the cyan brackets with a TARGET tab."""
    f = Frame(battle_base())
    for rail, edge in ((LEFT_RAIL, LEFT_RAIL[2] - 1), (RIGHT_RAIL, RIGHT_RAIL[0])):
        gradient(f, rail, STEEL, STEEL, alpha=240)
        lay, d = f.draw()
        d.line(sc(edge, rail[1], edge, rail[3]), fill=STEEL_GOLD + (220,), width=2 * S)
        f.add(lay)
    text(f, 64, BAR + 22, ACTING, GOTHIC(17), STEEL_GOLD)
    for row, name in zip(rail_rows(LEFT_RAIL, BAR + 42, len(COMMANDS), 62, 8, 112), COMMANDS):
        red = name == 'Retreat'
        gradient(f, row, RED_TOP if red else STEEL_TOP, RED_FOOT if red else STEEL_FOOT, radius=6)
        outline(f, row, STEEL_GOLD, 1.5, radius=6)
        icon(f, name, row[0] + 20, (row[1] + row[3]) / 2, 20, STEEL_TEXT if red else STEEL_GOLD)
        text(f, row[0] + 36, (row[1] + row[3]) / 2, name, GOTHIC(16), STEEL_TEXT, 'lm')
    text(f, 1216, BAR + 22, 'TARGET', GOTHIC(13), STEEL_DIM)
    text(f, 1216, BAR + 44, 'Sakanaku', GOTHIC(17), CYAN)
    for row, name in zip(rail_rows(RIGHT_RAIL, BAR + 66, len(ATTACKS), 66, 10, 116), ATTACKS):
        x0, y0, x1, y1 = row
        tip = x0 - 14                                        # the chevron points left, into the scene
        shape = (tip, (y0 + y1) / 2, x0, y0, x1, y0, x1, y1, x0, y1)
        lay, d = f.draw()
        d.polygon(sc(*shape), fill=(0, 0, 0, 120))
        f.add(lay, blur=3)
        g = Image.new('RGBA', f.img.size, (0, 0, 0, 0))
        gd = ImageDraw.Draw(g)
        for y in range(round(y0 * S), round(y1 * S)):
            t = (y / S - y0) / (y1 - y0)
            gd.line((round(tip * S), y, round(x1 * S), y),
                    fill=tuple(int(a + (b - a) * t) for a, b in zip((255, 228, 90), (226, 128, 20))) + (255,))
        mask = Image.new('L', f.img.size, 0)
        ImageDraw.Draw(mask).polygon(sc(*shape), fill=255)
        f.img.paste(g, (0, 0), mask)
        lay, d = f.draw()
        d.polygon(sc(*shape), outline=(255, 255, 255, 255), width=2 * S)
        f.add(lay)
        icon(f, name, x0 + 12, (y0 + y1) / 2, 22, (60, 30, 6))
        text(f, (x0 + 26 + x1) / 2, (y0 + y1) / 2 + 1, name, GOTHIC(20), (40, 20, 4))
    brackets(f, TARGET_CARD, CYAN)
    tab = (TARGET_CARD[0] + 96, TARGET_CARD[1] - 22, TARGET_CARD[0] + 186, TARGET_CARD[1] - 2)
    gradient(f, tab, (40, 120, 160), (24, 80, 112), radius=4)
    outline(f, tab, CYAN, 1.5, radius=4)
    text(f, (tab[0] + tab[2]) / 2, (tab[1] + tab[3]) / 2, 'TARGET', GOTHIC(12), (230, 248, 255))
    caption(f, ['B — the battle\'s steel, crisper: the rails flush with the scene, buttons with icons as the cards\' '
                'colours,', 'the attacks as the banners\' gold chevrons pointing at the scene; the cyan brackets with a '
                'TARGET tab', f'rows 112x62 = {mm(112)} x {mm(62)}, attacks 116x66; the battle\'s Skip as it is'], y=318, x=140)
    f.save('B3-battle-steel.png')


def battle_c():
    """C: round seals with icons — the commands two to a row on the left, the attacks large gold seals on the right;
    a round reticle over the target's portrait."""
    f = Frame(battle_base())
    for rail in (LEFT_RAIL, RIGHT_RAIL):
        gradient(f, rail, (20, 14, 10), (20, 14, 10), alpha=150)
    text(f, 64, BAR + 22, ACTING, BRUSH(17), NAME)
    r = 27                                                  # 54 across = 11 mm
    places = [(36, BAR + 76), (92, BAR + 76), (36, BAR + 172), (92, BAR + 172), (64, BAR + 286)]
    for (cx, cy), name in zip(places, COMMANDS):
        rr = 34 if name == 'Retreat' else r
        seal(f, cx, cy, rr, red=name == 'Retreat')
        icon(f, name, cx, cy, 24 if name != 'Retreat' else 28, (250, 226, 160) if name == 'Retreat' else NAME)
        text(f, cx, cy + rr + 12, name, GOTHIC(12), QUIET)
    text(f, 1216, BAR + 22, 'TARGET', CINZEL(12), SUBTITLE)
    text(f, 1216, BAR + 44, 'Sakanaku', BRUSH(15), (150, 225, 255))
    for i, name in enumerate(ATTACKS):
        cx, cy = 1216, BAR + 120 + i * 120
        seal(f, cx, cy, 48, lit=True)
        icon(f, name, cx, cy - 10, 30, LIT_INK)
        text(f, cx, cy + 22, name, BRUSH(15), LIT_INK)
    # The reticle over the target's portrait.
    px, py = 915, 538
    lay, d = f.draw()
    for rad, alpha in ((58, 230), (50, 120)):
        d.ellipse(sc(px - rad, py - rad, px + rad, py + rad), outline=(255, 200, 100, alpha), width=3 * S)
    for a, b in (((px, py - 70), (px, py - 50)), ((px, py + 50), (px, py + 70)), ((px - 70, py), (px - 50, py)),
                 ((px + 50, py), (px + 70, py))):
        d.line(sc(*a, *b), fill=(255, 220, 140, 255), width=3 * S)
    f.add(lay)
    outline(f, (TARGET_CARD[0] - 3, TARGET_CARD[1] - 3, TARGET_CARD[2] + 3, TARGET_CARD[3] + 3), (255, 200, 100), 2)
    caption(f, ['C — round seals: the commands two to a row, icons first (54 = 11 mm), Retreat a red seal of its own;',
                'the attacks large gold seals (96 = 19.5 mm); a round reticle over the target\'s portrait, the card '
                'outlined gold', 'the labels small under the seals; the battle\'s Skip as it is'], y=318, x=140)
    f.save('C3-battle-seals.png')


maps()
battle_a()
battle_b()
battle_c()
print('done')
