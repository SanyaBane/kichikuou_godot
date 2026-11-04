"""Mock-ups of the touch battle screen with every control in the screen's lower half (owner, 2026-10-25: the tablet is
held in landscape with both hands, and the thumbs should not have to reach up; docs/ui/touch.md, "The battle within the
thumbs' reach"), drawn over the screen gallery's dumps of round 1's first turn (Rance's, the target Ex).

The dumps: tools/ui/gallery.ps1 -Tablet (docs/ui/gallery.md), its entries battle-turn (both passes) and retreat. Then, from
the repository's root (Pillow; godot/fonts):
  python -I tools/ui/mock_battle_low.py <gallery dir> <out dir> < /dev/null
The images hold the units' portraits: keep <out dir> outside the repository.

Three layouts: A the rails moved down beside the cards (the board narrower), B a bar along the screen's foot (the scene
smaller), C the halves swapped (the cards on top, the scene with today's rails below); A2 is A's retreat question at the
left thumb. The pink arcs are the thumbs' reach on the owner's tablet (Galaxy Tab S7 FE, 0.203 mm a layout pixel): about
70 mm from where a hand holds each side.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

GALLERY, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H = 1280, 800
TABLET_MM = 0.203
REACH, GRIP_Y = 345, 640                # ~70 mm round the point a hand holds each side at

# Lacquer (godot/Strategy/Lacquer.cs).
PANEL = (22, 16, 12)
GOLD, DIM_GOLD = (196, 160, 86), (110, 86, 46)
NAME, SUBTITLE, TEXT = (232, 199, 122), (160, 140, 104), (244, 232, 206)
ROW = (34, 26, 20)
VERMILION, VERMILION_LINE = (118, 30, 24), (214, 150, 90)
LIT_TOP, LIT_FOOT, LIT_INK = (255, 214, 110), (214, 140, 40), (40, 24, 8)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)

COMMANDS = ['Defend', 'Wait', 'Auto-Atk', 'Auto-Def', 'Retreat']
SCENE_SRC = (0, 0, 1280, 408)           # the scene with its blurred sides, above the board's gold rule
BOARD_SRC = (0, 414, 1280, 800)         # the board: the armies' names, the cards, the order strip


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), int(size * S))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)
CINZEL = lambda size, w=600: font('Cinzel-VariableFont_wght.ttf', size, w)


def mm(px):
    return f'{px * TABLET_MM:.0f} mm'


def load(*parts):
    return Image.open(os.path.join(GALLERY, *parts)).convert('RGB')


class Frame:
    def __init__(self):
        self.img = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 255))

    def draw(self):
        lay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay, blur=0):
        self.img = Image.alpha_composite(self.img, lay.filter(ImageFilter.GaussianBlur(blur * S)) if blur else lay)

    def put(self, picture, box, at, size=None, blur=0, dim=0):
        """The dump's box drawn at `at` (layout pixels), scaled to `size`; blurred and darkened if asked."""
        part = picture.crop(box)
        w, h = size or (box[2] - box[0], box[3] - box[1])
        part = part.resize((round(w * S), round(h * S)), Image.LANCZOS).convert('RGBA')
        if blur:
            part = part.filter(ImageFilter.GaussianBlur(blur * S))
        if dim:
            part = Image.alpha_composite(part, Image.new('RGBA', part.size, (0, 0, 0, round(255 * dim))))
        self.img.paste(part, (round(at[0] * S), round(at[1] * S)))

    def save(self, name):
        self.img.resize((W, H), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def sc(*v):
    return [round(a * S) for a in v]


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


def gradient(f, r, top, foot, radius=0, alpha=255):
    x0, y0, x1, y1 = [round(v) for v in r]
    g = Image.new('RGBA', ((x1 - x0) * S, (y1 - y0) * S))
    gd = ImageDraw.Draw(g)
    for y in range(g.height):
        t = y / max(1, g.height - 1)
        gd.line((0, y, g.width, y), fill=tuple(int(a + (b - a) * t) for a, b in zip(top, foot)) + (alpha,))
    mask = Image.new('L', g.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, g.width - 1, g.height - 1), radius=radius * S, fill=255)
    lay = Image.new('RGBA', f.img.size, (0, 0, 0, 0))
    lay.paste(g, (x0 * S, y0 * S), mask)
    f.add(lay)


def outline(f, r, colour, width=2, radius=0):
    lay, d = f.draw()
    d.rounded_rectangle(sc(*r), radius=radius * S, outline=colour + (255,), width=round(width * S))
    f.add(lay)


def studs(f, r):
    lay, d = f.draw()
    for cx, cy in ((r[0], r[1]), (r[2], r[1]), (r[0], r[3]), (r[2], r[3])):
        d.polygon(sc(cx, cy - 4, cx + 4, cy, cx, cy + 4, cx - 4, cy), fill=GOLD)
    f.add(lay)


def glow(f, r, colour, spread=5, alpha=120):
    lay, d = f.draw()
    d.rounded_rectangle(sc(r[0] - spread, r[1] - spread, r[2] + spread, r[3] + spread), radius=spread * S,
                        outline=colour + (alpha,), width=round(spread * 1.4 * S))
    f.add(lay, blur=spread * 0.8)


def panel(f, r, alpha=242):
    """Our lacquer panel: the double gold frame and its studs (the touch rails' look, round 1's A)."""
    gradient(f, r, (32, 24, 18), PANEL, alpha=alpha)
    outline(f, r, GOLD, 2)
    outline(f, (r[0] + 5, r[1] + 5, r[2] - 5, r[3] - 5), DIM_GOLD, 1)
    studs(f, r)


def rule(f, x0, x1, y):
    lay, d = f.draw()
    d.line(sc(x0, y, x1, y), fill=DIM_GOLD + (255,), width=S)
    f.add(lay)


def command(f, r, name, size=18):
    """A command as the rails' rows: Retreat vermilion as Turn END, the rest as the command panel's row boxes."""
    if name == 'Retreat':
        gradient(f, r, VERMILION, (88, 22, 18))
        outline(f, r, GOLD, 2)
        outline(f, (r[0] + 4, r[1] + 4, r[2] - 4, r[3] - 4), VERMILION_LINE, 1)
        text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2 + 1, name, BRUSH(size - 3), (250, 226, 160))
    else:
        gradient(f, r, ROW, ROW)
        outline(f, r, DIM_GOLD, 1)
        text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2, name, GOTHIC(size), TEXT)


def attack(f, r, name, size=19):
    glow(f, r, (255, 190, 90))
    gradient(f, r, LIT_TOP, LIT_FOOT)
    outline(f, r, (255, 244, 210), 2)
    outline(f, (r[0] + 4, r[1] + 4, r[2] - 4, r[3] - 4), (150, 100, 30), 1)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2 + 1, name, BRUSH(size), LIT_INK)


def reach(f):
    """The thumbs' reach: an arc round the point each hand holds its side at."""
    lay, d = f.draw()
    for cx in (0, W):
        d.ellipse(sc(cx - REACH, GRIP_Y - REACH, cx + REACH, GRIP_Y + REACH), outline=NOTE + (200,), width=3 * S)
    f.add(lay)
    note(f, 12, GRIP_Y - REACH - 14, f'thumb ≈ {mm(REACH)}', size=13)


def gold_rule(f, y):
    lay, d = f.draw()
    d.rectangle(sc(0, y, W, y + 3), fill=GOLD + (255,))
    f.add(lay)


# ------------------------------------------------------------------------------------------------- the layouts
def layout_a(retreat=False):
    """A: the scene as now, full width; the rails moved down beside the cards, the board between them narrower."""
    pc, touch = load('pc', 'current', 'battle-turn.png'), load('tablet', 'current', 'battle-turn.png')
    f = Frame()
    f.put(pc, (0, 0, 1280, 414), (0, 0))
    f.put(touch, BOARD_SRC, (0, 414), (1280, 386), blur=6, dim=0.35)       # the stones behind, as the board's own
    k = 1000 / 1280
    bh = (BOARD_SRC[3] - BOARD_SRC[1]) * k
    f.put(touch, BOARD_SRC, (140, 414 + (386 - bh) / 2), (1000, bh))
    left, right = (4, 418, 136, 796), (1144, 418, 1276, 796)
    panel(f, left)
    panel(f, right)
    text(f, 70, 440, 'Rance', BRUSH(19), NAME)
    rule(f, 18, 122, 462)
    for i, name in enumerate(COMMANDS):
        y = 476 + i * 62
        command(f, (12, y, 128, y + 54), name)
    text(f, 1210, 438, 'TARGET', CINZEL(13), SUBTITLE)
    text(f, 1210, 462, 'Ex', BRUSH(17), (150, 225, 255))
    rule(f, 1158, 1262, 484)
    attack(f, (1152, 706, 1268, 782), 'Melee')
    note(f, 1158, 672, 'more attacks', anchor='ld', size=12)
    note(f, 1158, 692, 'stack upward', anchor='ld', size=12)
    if retreat:
        dim = Image.new('RGBA', f.img.size, (0, 0, 0, 0))
        ImageDraw.Draw(dim).rectangle(sc(140, 0, W, H), fill=(0, 0, 0, 120))
        f.add(dim)
        q = (146, 596, 476, 790)
        panel(f, q, alpha=250)
        text(f, 166, 626, 'Retreat the whole army?', GOTHIC(19), TEXT, anchor='lm')
        text(f, 166, 654, 'The battle is lost.', GOTHIC(16), SUBTITLE, anchor='lm')
        rule(f, 160, 462, 678)
        command(f, (160, 694, 304, 774), 'Cease Retreat', size=16)       # under the thumb: a second tap is safe
        command(f, (316, 694, 462, 774), 'Retreat', size=20)
        lay, d = f.draw()
        d.polygon(sc(136, 728, 146, 718, 146, 738), fill=GOLD + (255,))           # pointing at the rail's Retreat
        f.add(lay)
        reach(f)
        caption(f, ['A2 — the retreat question at the left thumb, beside the rail\'s Retreat',
                    f'its answers 144x80 = {mm(144)} x {mm(80)} (today\'s are rows of 30 in the scene\'s middle, {mm(30)} high);',
                    'Cease nearer the rail, so a double tap on Retreat does not confirm it; the long press\'s menu stays at its card'],
                y=40, x=150)
        f.save('A2-retreat.png')
        return
    reach(f)
    caption(f, ['A — the rails moved down beside the cards; the scene above as now, full width',
                f'commands 116x54 = {mm(116)} x {mm(54)}, the attacks from the rail\'s foot up 116x76 = {mm(116)} x {mm(76)};',
                'the board 78% as wide: drawn here scaled, built it would keep the text and give up portrait width'],
            y=40, x=150)
    f.save('A-rails-low.png')


def layout_b():
    """B: the scene 80% as large, the board raised under it, a bar along the screen's foot with a group at each thumb."""
    pc, touch = load('pc', 'current', 'battle-turn.png'), load('tablet', 'current', 'battle-turn.png')
    f = Frame()
    sh = 322
    f.put(pc, SCENE_SRC, (0, 0), (1280, sh), blur=10, dim=0.3)                # the sides: the scene blurred
    f.put(pc, SCENE_SRC, ((1280 - 1280 * sh / 408) / 2, 0), (1280 * sh / 408, sh))
    gold_rule(f, sh)
    f.put(touch, (0, 414, 1280, 770), (0, sh + 4))
    bar = (4, 692, 1276, 796)
    panel(f, bar)
    cells = [(14 + c * 124, 702 + r * 46) for r in range(2) for c in range(3)]
    for (x, y), name in zip(cells, COMMANDS):
        command(f, (x, y, x + 118, y + 42), name, size=17)
    text(f, 640, 728, 'Rance  →  Ex', GOTHIC(22), NAME)
    text(f, 640, 760, 'Melee reaches Ex', GOTHIC(14), SUBTITLE)
    text(f, 990, 724, 'TARGET', CINZEL(13), SUBTITLE)
    text(f, 990, 750, 'Ex', BRUSH(19), (150, 225, 255))
    attack(f, (1062, 702, 1266, 786), 'Melee', size=22)
    note(f, 1062, 686, 'more attacks line up leftward', anchor='ld', size=12)
    reach(f)
    caption(f, ['B — a bar along the foot: the commands at the left thumb, the attacks at the right; the scene 80% as large',
                f'commands 118x42 = {mm(118)} x {mm(42)}, Melee 204x84 = {mm(204)} x {mm(84)}; the cards as now, raised;',
                'the bar\'s middle only tells (no button there: no thumb reaches it)'], y=30, x=150)
    f.save('B-foot-bar.png')


def layout_c():
    """C: the halves swapped — the cards on top, the scene with today's rails under them."""
    touch = load('tablet', 'current', 'battle-turn.png')
    f = Frame()
    f.put(touch, BOARD_SRC, (0, 0))
    gold_rule(f, 388)
    f.put(touch, (0, 0, 1280, 408), (0, 392))
    reach(f)
    caption(f, ['C — the halves swapped: the cards on top, the scene with today\'s rails at the thumbs',
                'nothing new to draw: today\'s rails and sizes; the target is preselected, so a card is rarely tapped,',
                'but a tap on an enemy or a long press on our own reaches up; the retreat question in the scene, low'],
            y=440, x=150)
    f.save('C-halves-swapped.png')


layout_a()
layout_a(retreat=True)
layout_b()
layout_c()
