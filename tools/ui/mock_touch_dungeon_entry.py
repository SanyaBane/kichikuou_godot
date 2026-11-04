"""Mock-ups of the touch mode's dungeon menu before a search (Cﾒｲｷｭｳ.ADV:238-305: Start Search, Bring or Leave Merim,
Bring Pink Cow, God's Door, Return) within the right thumb's reach (owner, 2026-10-26; docs/ui/dungeon-menu.md, §6).
Today it is our plain `]` menu at the boot's selection box, over the dungeon's picture at the screen's top.

The dumps: tools/ui/gallery.ps1 -Tablet -Only map,dungeon-entry (docs/ui/gallery.md). Then, from the repository's root
(Pillow; godot/fonts; the parts are mock_touch_panel.py's):
  python -I tools/ui/mock_touch_dungeon_entry.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository.

Two menus each: the Monsters' Cave (dungeon 2) unsearched (Start Search, Return) and the Ancient Ruins (dungeon 30)
with Merim and God's Door found (Start Search, Bring Merim, God's Door, Return; the cave's picture standing in for the
ruins'). A the province menu's rail — the rows from the foot up, Return at the foot; B the floor ladder's rail — Start
Search large in the corner, where Descend stands once the search begins, Return at the top as Retreat; C Start Search
in the right corner, Return alone at the left thumb.
"""
import sys, os
sys.dont_write_bytecode = True                  # no __pycache__ in the repository for the borrowed module
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw, ImageFont
from mock_touch_panel import (W, H, S, REPO, GOLD, DIM_GOLD, NAME, TEXT, QUIET, ROW, NOTE, GOTHIC, Frame, load, sc, text,
                              note, rect, shadow, panel, reach, mm)

WINDOW_TEXT = (226, 204, 150)
CAVE = dict(name="Monsters' Cave", state='(Unsearched)', extras=[])
RUINS = dict(name='Ancient Ruins', state='(Search in Progress)', extras=['Bring Merim', "God's Door"])


def screen(d):
    """The gallery's dungeon-entry with our `]` menu taken away: the map under it from the gallery's map, the picture
    frame's top line carried across from its left of the menu (its centre ornament, under the menu, is lost)."""
    img = load('tablet', 'current', 'dungeon-entry.png')
    under = load('tablet', 'current', 'map.png')
    img.paste(under.crop((480, 80, 830, 193)), (480, 80))
    strip = img.crop((470, 193, 471, 204))
    for x in range(480, 830):
        img.paste(strip, (x, 193))
    if d is not CAVE:
        window(img, d)
    return img


def window(img, d):
    """Window 113's lines rewritten: the name in brackets, a blank line, the state, in a narrowed BIZ UDGothic standing in
    for the game's font."""
    fill = img.getpixel((900, 760))
    ImageDraw.Draw(img).rectangle((270, 556, 1010, 776), fill=fill)
    fnt = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf'), 46)
    for y, line in ((562, f'[{d["name"]}]'), (666, d['state'])):
        probe = ImageDraw.Draw(Image.new('L', (1, 1)))
        l, t0, r, b = probe.textbbox((0, 0), line, font=fnt)
        glyphs = Image.new('RGBA', (r - l + 4, b - t0 + 4), (0, 0, 0, 0))
        ImageDraw.Draw(glyphs).text((2 - l, 2 - t0), line, font=fnt, fill=WINDOW_TEXT)
        glyphs = glyphs.resize((round(glyphs.width * 0.82), glyphs.height), Image.LANCZOS)
        img.paste(glyphs, (282, y), glyphs)


def triangle_down(f, cx, cy, size, colour):
    lay, dr = f.draw()
    h = size * 0.8
    dr.polygon(sc(cx - size / 2, cy - h / 2, cx + size / 2, cy - h / 2, cx, cy + h / 2), fill=colour)
    f.add(lay)


def row(f, r, name, size=19):
    shadow(f, r, 3)
    rect(f, r, ROW, outline=DIM_GOLD)
    text(f, r[0] + 16, (r[1] + r[3]) / 2, name, GOTHIC(size), TEXT, anchor='lm')


def ret(f, r, size=19):
    shadow(f, r, 3)
    rect(f, r, ROW, outline=DIM_GOLD)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2, 'Return', GOTHIC(size), QUIET)


def start(f, r, large=True):
    """Start Search as the ladder's Descend: the gold arrow down, the name, a caption."""
    shadow(f, r, 3)
    rect(f, r, ROW, outline=GOLD, w=2)
    cy = (r[1] + r[3]) / 2
    if large:
        triangle_down(f, r[0] + 36, cy, 36, GOLD)
        text(f, r[0] + 66, cy - 12, 'Start Search', GOTHIC(22), TEXT, anchor='lm')
        text(f, r[0] + 67, cy + 19, 'a unit goes down', GOTHIC(14), NAME, anchor='lm')
    else:
        triangle_down(f, r[0] + 26, cy, 22, GOLD)
        text(f, r[0] + 46, cy, 'Start Search', GOTHIC(19), TEXT, anchor='lm')


def finger(f, x, y):
    lay, d = f.draw()
    d.ellipse(sc(x - 15, y - 15, x + 15, y + 15), fill=NOTE + (90,), outline=NOTE + (230,), width=2 * S)
    f.add(lay)


def caption(f, lines):
    for i, t in enumerate(lines):
        note(f, 16, 14 + i * 23, t, size=17 if i == 0 else 14)


X0, X1 = 1040, 1276                             # right of window 113 (to x 1032), to the screen's edge
IN0, IN1 = X0 + 12, X1 - 12
DESCEND = (IN0, 680, IN1, 784)                  # the floor ladder's Descend (the owner's B)


def layout_a(d, tag):
    """A: the province menu's rail — the items from the foot up, Start Search lowest, Return 212x60 at the foot."""
    f = Frame(screen(d))
    items = d['extras']
    n = 1 + len(items)
    top = 664 - (n - 1) * 54 - 12
    panel(f, (X0, top, X1, 796))
    start(f, (IN0, 664, IN1, 710), large=False)
    for i, name in enumerate(items):
        y = 664 - (i + 1) * 54
        row(f, (IN0, y, IN1, y + 46), name)
    ret(f, (IN0, 724, IN1, 784))
    finger(f, 1240, 687)
    reach(f)
    caption(f, [f'A — the province menu\'s rail: {d["name"]}',
                f'rows {IN1 - IN0}x46 = {mm(IN1 - IN0)} x {mm(46)} from the foot up, Start Search lowest; Return at the foot, '
                'as the map\'s Close and the province menu\'s Return;',
                'the rail as high as its items; once the search begins the ladder stands in the same corner'])
    f.save(f'A-rail-{tag}.png')


def layout_b(d, tag):
    """B: the floor ladder's rail — Start Search where Descend will stand, Return at the top as Retreat."""
    f = Frame(screen(d))
    items = d['extras']
    top = DESCEND[1] - 14 - len(items) * 54 - 74 - 14
    panel(f, (X0, top, X1, 796))
    ret(f, (IN0, top + 14, IN1, top + 74))
    for i, name in enumerate(items):
        y = top + 88 + i * 54
        row(f, (IN0, y, IN1, y + 46), name)
    start(f, DESCEND)
    finger(f, 1240, 750)
    reach(f)
    caption(f, [f'B — the floor ladder\'s rail: {d["name"]}',
                f'Start Search {IN1 - IN0}x104 = {mm(IN1 - IN0)} x {mm(104)} in the corner, exactly where Descend stands once the '
                'search begins:',
                'the thumb taps one place to go in and down; Return on top, as the ladder\'s Retreat; Merim, the cow and God\'s Door',
                'between them, the rail growing upwards'])
    f.save(f'B-ladder-{tag}.png')


def layout_c(d, tag):
    """C: Start Search and the other items at the right thumb, Return alone at the left."""
    f = Frame(screen(d))
    items = d['extras']
    top = DESCEND[1] - 14 - len(items) * 54
    panel(f, (X0, top, X1, 796))
    for i, name in enumerate(items):
        y = top + 14 + i * 54
        row(f, (IN0, y, IN1, y + 46), name)
    start(f, DESCEND)
    panel(f, (4, 712, 240, 796))
    ret(f, (16, 724, 228, 784))
    finger(f, 1240, 750)
    reach(f)
    caption(f, [f'C — Start Search at the right thumb, Return at the left: {d["name"]}',
                'Start Search in the ladder\'s Descend place; Return alone in the lower left corner, left of the place\'s window,',
                'where the ladder\'s round T1 put Retreat (C): leaving takes the other hand'])
    f.save(f'C-split-{tag}.png')


for d, tag in ((CAVE, 'cave'), (RUINS, 'ruins')):
    layout_a(d, tag)
    layout_b(d, tag)
    layout_c(d, tag)
