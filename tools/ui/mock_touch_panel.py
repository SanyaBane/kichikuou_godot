"""Mock-ups of the touch mode's command panel within the thumbs' reach (owner, 2026-10-25: the panel's choices at the
screen's lower left and right, as the battle's rails; docs/ui/touch.md, "The command panel within the thumbs' reach"),
drawn over the screen gallery's dumps of the week-18 COMMAND save with the Military Academy under way, 5 weeks left.

The dumps: tools/ui/gallery.ps1 -Touch -Only map,command-menu-building (docs/ui/gallery.md). Then, from the repository's
root (Pillow; godot/fonts):
  python -I tools/ui/mock_touch_panel.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository.

The map is the gallery's `map` (no plates: GameScreen.UpdateMapMarkers hides them while a menu is open), the status strip
the touch pass's command-menu-building's.

Three layouts: A two rails at the sides' lower halves (the battle's), B a bar along the screen's foot, C a fan at each
lower corner, along the thumb's arc. The pink arcs are the thumbs' reach on the owner's tablet (Galaxy Tab S7 FE, 0.203 mm
a layout pixel): about 70 mm from where a hand holds each side.
"""
import sys, os, math
from PIL import Image, ImageDraw, ImageFont, ImageFilter

GALLERY, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H = 1280, 800
TABLET_MM = 0.203
REACH, GRIP_Y = 345, 640                # ~70 mm round the point a hand holds each side at

# Lacquer (godot/Strategy/UiPalette.cs).
PANEL = (22, 16, 12)
GOLD, DIM_GOLD = (196, 160, 86), (110, 86, 46)
NAME, SUBTITLE, TEXT, QUIET = (232, 199, 122), (160, 140, 104), (244, 232, 206), (176, 160, 130)
ROW, OFF_TAG, BADGE = (34, 26, 20), (52, 46, 40), (60, 44, 24)
VERMILION, VERMILION_LINE = (118, 30, 24), (214, 150, 90)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)

# CommandMenuView.TagOf; the Military Academy under way (the owner's A, 2026-10-25).
COMMANDS = [('Build', (46, 62, 104), 'Construction', 5), ('Harem', (128, 44, 70), 'Harem', None),
            ('Items', (138, 104, 34), 'Treasure', None), ('Trade', (36, 92, 96), 'Merchant', None),
            ('Units', (70, 96, 52), 'Subordinates', None)]
LINKS = ['→Leazas Castle', 'Load', 'Save', 'All Data', 'Think']


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size * S)))
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
    def __init__(self, base):
        self.img = base.resize((W * S, H * S), Image.LANCZOS).convert('RGBA')

    def draw(self):
        lay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay, blur=0):
        self.img = Image.alpha_composite(self.img, lay.filter(ImageFilter.GaussianBlur(blur * S)) if blur else lay)

    def save(self, name):
        self.img.resize((W, H), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def sc(*v):
    return [round(a * S) for a in v]


def text(f, x, y, t, fnt, colour, anchor='mm'):
    lay, d = f.draw()
    d.text((x * S, y * S), t, font=fnt, fill=colour, anchor=anchor)
    f.add(lay)


def width(t, fnt):
    return fnt.getlength(t) / S


def note(f, x, y, t, anchor='la', size=15):
    lay, d = f.draw()
    fnt = GOTHIC(size)
    l, t0, r, b = d.textbbox((x * S, y * S), t, font=fnt, anchor=anchor)
    d.rectangle((l - 5 * S, t0 - 3 * S, r + 5 * S, b + 3 * S), fill=(0, 0, 0, 215))
    d.text((x * S, y * S), t, font=fnt, fill=NOTE, anchor=anchor)
    f.add(lay)


def caption(f, lines, y=62, x=16):
    for i, t in enumerate(lines):
        note(f, x, y + i * 23, t, size=17 if i == 0 else 14)


def rect(f, r, fill=None, outline=None, w=1, alpha=255):
    lay, d = f.draw()
    d.rectangle(sc(*r), fill=fill + (alpha,) if fill else None, outline=outline + (255,) if outline else None,
                width=max(1, round(w * S)))
    f.add(lay)


def shadow(f, r, spread=6):
    lay, d = f.draw()
    d.rectangle(sc(r[0] - 2, r[1] + 2, r[2] + 4, r[3] + 6), fill=(0, 0, 0, 110))
    f.add(lay, blur=spread * 0.6)


def studs(f, r):
    lay, d = f.draw()
    for cx, cy in ((r[0], r[1]), (r[2], r[1]), (r[0], r[3]), (r[2], r[3])):
        d.polygon(sc(cx, cy - 5, cx + 5, cy, cx, cy + 5, cx - 5, cy), fill=GOLD)
    f.add(lay)


def panel(f, r):
    """Our lacquer panel: the double gold frame and its studs (UiPalette.DrawPanel)."""
    shadow(f, r)
    rect(f, r, PANEL, alpha=245)
    rect(f, (r[0] + 1, r[1] + 1, r[2] - 1, r[3] - 1), outline=GOLD, w=2)
    rect(f, (r[0] + 5.5, r[1] + 5.5, r[2] - 5.5, r[3] - 5.5), outline=DIM_GOLD)
    studs(f, r)


def rule(f, x0, x1, y):
    lay, d = f.draw()
    d.line(sc(x0, y, x1, y), fill=DIM_GOLD + (255,), width=S)
    d.polygon(sc((x0 + x1) / 2, y - 3, (x0 + x1) / 2 + 3, y, (x0 + x1) / 2, y + 3, (x0 + x1) / 2 - 3, y), fill=GOLD + (255,))
    f.add(lay)


def mix(a, b, t):
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))


def tag(f, r, word, colour, weeks, size):
    """UiPalette.DrawTag, or DrawBusyTag with its chip while a building is under way."""
    if weeks is None:
        rect(f, r, colour, outline=GOLD)
        text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2, word, GOTHIC(size), TEXT)
        return
    chip = width(str(weeks), GOTHIC(size - 1)) + 10
    rect(f, r, mix(colour, OFF_TAG, 0.55), outline=DIM_GOLD)
    text(f, (r[0] + r[2] - chip) / 2, (r[1] + r[3]) / 2, word, GOTHIC(size), QUIET)
    c = (r[2] - chip - 1, r[1] + 3, r[2] - 3, r[3] - 3)
    rect(f, c, BADGE, outline=GOLD)
    text(f, (c[0] + c[2]) / 2, (c[1] + c[3]) / 2, str(weeks), GOTHIC(size - 1), NAME)


def command(f, r, item, size=18, tag_w=66):
    """A command row: the row box, its tag, its name (quiet while its building is under way)."""
    word, colour, name, weeks = item
    shadow(f, r, 3)
    rect(f, r, ROW, outline=DIM_GOLD)
    h = r[3] - r[1]
    tag(f, (r[0] + 6, r[1] + 7, r[0] + 6 + tag_w, r[3] - 7), word, colour, weeks, 13 if h >= 44 else 12)
    fnt = GOTHIC(size)
    while width(name, fnt) > r[2] - r[0] - tag_w - 24 and size > 12:
        size -= 1
        fnt = GOTHIC(size)
    text(f, r[0] + tag_w + 16, (r[1] + r[3]) / 2, name, fnt, QUIET if weeks else TEXT, anchor='lm')


def link(f, r, name, size=18):
    shadow(f, r, 3)
    rect(f, r, ROW, outline=DIM_GOLD)
    text(f, r[0] + 14, (r[1] + r[3]) / 2, name, GOTHIC(size), QUIET, anchor='lm')


def turn_end(f, r, size=22):
    shadow(f, r, 4)
    rect(f, r, VERMILION, outline=GOLD, w=2)
    rect(f, (r[0] + 4, r[1] + 4, r[2] - 4, r[3] - 4), outline=VERMILION_LINE)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2 + 1, 'Turn END', BRUSH(size), (250, 226, 160))


def close(f, r, size=19):
    """The map's Commands button as Close (CommandsButton.Closes): a lacquer plate, a gold cross, the brush face."""
    panel(f, r)
    cy, x = (r[1] + r[3]) / 2, r[0] + 30
    lay, d = f.draw()
    d.line(sc(x - 9, cy - 9, x + 9, cy + 9), fill=GOLD + (255,), width=3 * S)
    d.line(sc(x - 9, cy + 9, x + 9, cy - 9), fill=GOLD + (255,), width=3 * S)
    f.add(lay)
    text(f, (r[0] + 44 + r[2]) / 2, cy + 1, 'Close', BRUSH(size), NAME)


def reach(f):
    lay, d = f.draw()
    for cx in (0, W):
        d.ellipse(sc(cx - REACH, GRIP_Y - REACH, cx + REACH, GRIP_Y + REACH), outline=NOTE + (200,), width=3 * S)
    f.add(lay)
    note(f, 12, GRIP_Y - REACH - 14, f'thumb ≈ {mm(REACH)}', size=13)


def base():
    m = load('touch', 'current', 'map.png')
    strip = load('touch', 'current', 'command-menu-building.png').crop((0, 0, W, 52))
    m.paste(strip, (0, 0))
    return m


MAP = None


def frame():
    global MAP
    MAP = MAP or base()
    return Frame(MAP)


# ------------------------------------------------------------------------------------------------- the layouts
def layout_a():
    """A: two rails in the sides' lower halves, filled from the foot up as the battle's; Close where Commands stands."""
    f = frame()
    left, right = (4, 466, 238, 796), (1042, 352, 1276, 796)
    panel(f, left)
    panel(f, right)
    text(f, 121, 488, 'THIS WEEK', CINZEL(13), SUBTITLE)
    rule(f, 20, 222, 504)
    for i, item in enumerate(COMMANDS):                                   # Construction lowest, as the battle's Defend
        y = 732 - i * 54
        command(f, (14, y, 228, y + 46), item)
    turn_end(f, (1052, 364, 1266, 426))                                   # furthest from the thumb, as Retreat
    rule(f, 1060, 1258, 440)
    for i, name in enumerate(LINKS):
        y = 668 - i * 54
        link(f, (1052, y, 1266, y + 46), name)
    close(f, (1052, 726, 1266, 786))
    reach(f)
    caption(f, ['A — two rails in the sides\' lower halves, as the battle\'s; the map\'s middle stays open',
                f'rows 214x46 = {mm(214)} x {mm(46)}, filled from the foot up; Turn END on the right rail\'s top, '
                f'{mm(214)} x {mm(62)}, furthest from the thumb;',
                'Close where the Commands button stands; a tap on the map closes too. The building\'s weeks on the tag\'s chip',
                '(the rail has no room for "5 weeks left"). The thieves\' week: two and three rows.'], x=250)
    f.save('A-rails.png')


def layout_b():
    """B: a bar along the screen's foot, a group at each thumb, Turn END in the middle."""
    f = frame()
    bar = (4, 680, 1276, 796)
    panel(f, bar)
    cells = [(14 + c * 170, 690 + r * 52) for r in range(2) for c in range(3)]
    items = COMMANDS + [None]
    for (x, y), item in zip(cells, items):
        r = (x, y, x + 164, y + 46)
        if item is None:
            link(f, r, LINKS[0], size=15)
        else:
            command(f, r, item, size=16, tag_w=54)
    turn_end(f, (548, 694, 732, 782), size=26)
    right = [(762 + c * 170, 690 + r * 52) for r in range(2) for c in range(3)]
    for (x, y), name in zip(right[:4], LINKS[1:]):
        link(f, (x, y, x + 164, y + 46), name, size=16)
    close(f, (1102, 742, 1266, 788), size=16)
    reach(f)
    caption(f, ['B — a bar along the foot: the week\'s commands at the left thumb, the other items at the right',
                f'cells 164x46 = {mm(164)} x {mm(46)}, in 2 rows of 3; the third columns lie at the reach\'s edge or past it;',
                'Turn END in the middle, where no thumb reaches: a hand let go to end the week, on purpose;',
                'the bar covers the map\'s foot (Zeo, the south\'s cities) while it is open'], x=250)
    f.save('B-bar.png')


def arc_x(r, dy):
    return math.sqrt(max(0, r * r - dy * dy))


def sector(f, cx, cy, r, start, end):
    """A translucent lacquer quarter under a fan, its arc in gold."""
    lay, d = f.draw()
    box = sc(cx - r, cy - r, cx + r, cy + r)
    d.pieslice(box, start, end, fill=PANEL + (205,))
    d.arc(box, start, end, fill=GOLD + (255,), width=2 * S)
    d.arc(sc(cx - r + 6, cy - r + 6, cx + r - 6, cy + r - 6), start, end, fill=DIM_GOLD + (255,), width=S)
    f.add(lay)


def layout_c():
    """C: a fan at each lower corner, the rows' ends along the thumb's arc; Close the right fan's hub."""
    f = frame()
    R, HUB = 340, 100
    sector(f, 0, 800, R + 44, 270, 360)
    sector(f, W, 800, R + 44, 180, 270)
    tops = [746 - i * 50 for i in range(6)]
    for i, item in enumerate(COMMANDS):
        y = tops[i]
        right = arc_x(R, 800 - (y + 22))
        command(f, (12, y, right, y + 44), item, size=17)
    for i, y in enumerate(tops):
        left = W - arc_x(R, 800 - (y + 22))
        hub = arc_x(HUB + 8, 800 - (y + 44))                              # the rows clear the hub
        r = (left, y, W - 12 - hub if hub else W - 12, y + 44)
        if i < len(LINKS):
            link(f, r, LINKS[i], size=17)
        else:
            turn_end(f, r, size=20)
    lay, d = f.draw()
    d.pieslice(sc(W - HUB, H - HUB, W + HUB, H + HUB), 180, 270, fill=PANEL + (250,))
    d.arc(sc(W - HUB, H - HUB, W + HUB, H + HUB), 180, 270, fill=GOLD + (255,), width=2 * S)
    d.line(sc(W - 62, H - 66, W - 40, H - 44), fill=GOLD + (255,), width=3 * S)
    d.line(sc(W - 62, H - 44, W - 40, H - 66), fill=GOLD + (255,), width=3 * S)
    f.add(lay)
    text(f, W - 46, H - 24, 'Close', BRUSH(15), NAME)
    reach(f)
    caption(f, ['C — a fan at each lower corner: the rows\' inner ends along the thumb\'s arc',
                f'rows 44 high = {mm(44)}, their length set by the arc ({mm(190)}-{mm(330)}); Turn END the right fan\'s top,',
                'Close its hub in the corner where Commands stands; the fans\' quarters see-through over the map;',
                'the rows shorten as they rise, so a long name (a ×-row\'s note, a translation) has the least room at the top'],
            x=250)
    f.save('C-fans.png')


if __name__ == '__main__':                   # mock_touch_province.py borrows the parts above
    layout_a()
    layout_b()
    layout_c()
