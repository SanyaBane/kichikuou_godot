"""Mock-ups of the touch mode's province menu within the thumbs' reach (owner, 2026-10-25, after the command panel's rails;
docs/ui/touch.md, "The province menu within the thumbs' reach"). The owner's doubt: the tap on a province already takes
the hand out of the thumb's comfort, so the menu may belong where the finger is rather than at the rails.

The dumps: tools/ui/gallery.ps1 -Tablet -Only map,province-menu,province-menu-own-five (docs/ui/gallery.md). Then, from the
repository's root (Pillow; godot/fonts; the parts are mock_touch_panel.py's):
  python -I tools/ui/mock_touch_province.py <gallery dir> <out dir> < /dev/null
The images hold the provinces' pictures: keep <out dir> outside the repository.

Three layouts, each with the tap (a pink finger) and the tapped province ringed in gold (ours, today nothing marks it):
A the command panel's rails — the province's card on the left rail, its commands on the right from the foot up, Return
at the foot; B the card at the province, the commands on the right rail; C one callout at the province, the card and the
commands together, beside the finger. A and B for Leazas Castle (1) with two buildings (five commands), C for it and for
Oaks (6), the rebels' (two commands, its figures unscouted).
"""
import sys, os
sys.dont_write_bytecode = True                  # no __pycache__ in the repository for the borrowed module
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mock_touch_panel import (W, H, S, GOLD, DIM_GOLD, NAME, SUBTITLE, TEXT, QUIET, ROW, NOTE, GOTHIC, BRUSH, CINZEL, Frame,
                              load, sc, text, width, note, caption, rect, shadow, panel, rule, command, link, reach, mm)

LABEL, VALUE = (150, 134, 108), (236, 226, 204)
PICTURE = (345, 221, 611, 395)          # the province's picture inside its frame in ProvinceMenuView's touch layout

CASTLE = dict(
    dump='province-menu-own-five.png', name='Leazas Castle', army='LEAZAS ARMY', at=(715, 428),
    figures=[('Economy', '20 (2,000K GOLD)'), ('Defense', '10 (Walls +20%)'), ('Battle Area', '180 troops')],
    commands=[('Levy', (150, 52, 36), 'Temporary Levy', None), ('Build', (46, 62, 104), 'Bolster Defense', None),
              ('Visit', (70, 96, 52), 'Military Academy', None), ('Visit', (70, 96, 52), 'Genius Hospital', None),
              ('Visit', (70, 96, 52), 'Dungeon', None)])
OAKS = dict(
    dump='province-menu.png', name='Oaks', army='REBEL ARMY', at=(920, 574),
    figures=[('Economy', '??'), ('Defense', '??'), ('Battle Area', '??')],
    commands=[('Battle', (128, 30, 30), 'Attack', None), ('Ninja', (74, 50, 96), 'Ninja Maneuver', None)])


def frame():
    return Frame(load('tablet', 'current', 'map.png'))


def picture(f, p, r):
    """The province's picture from its menu's dump, in a dim gold frame."""
    part = load('tablet', 'current', p['dump']).crop(PICTURE)
    part = part.resize((round((r[2] - r[0]) * S), round((r[3] - r[1]) * S))).convert('RGBA')
    f.img.paste(part, (round(r[0] * S), round(r[1] * S)))
    rect(f, (r[0] - 2, r[1] - 2, r[2] + 2, r[3] + 2), outline=GOLD)


def card(f, p, x, y, w, pic_h, compact=False):
    """The card ProvinceMenuView draws: the picture, the name, the army, the figures (label over value). Its bottom."""
    picture(f, p, (x, y, x + w, y + pic_h))
    y += pic_h + 26
    text(f, x, y, p['name'], BRUSH(20 if compact else 22), NAME, anchor='lm')
    y += 22
    text(f, x, y, p['army'], CINZEL(11), SUBTITLE, anchor='lm')
    y += 20
    for label, value in p['figures']:
        if compact:
            text(f, x, y, label, GOTHIC(12), LABEL, anchor='lm')
            text(f, x + w, y, value, GOTHIC(14), VALUE, anchor='rm')
            y += 22
        else:
            text(f, x, y, label, GOTHIC(12), LABEL, anchor='lm')
            text(f, x, y + 17, value, GOTHIC(15), VALUE, anchor='lm')
            y += 36
    return y


def ret(f, r, size=18):
    shadow(f, r, 3)
    rect(f, r, ROW, outline=DIM_GOLD)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2, 'Return', GOTHIC(size), QUIET)


def tapped(f, at):
    """The tapped province ringed in gold (ours), and the finger that tapped it."""
    lay, d = f.draw()
    x, y = at
    d.ellipse(sc(x - 34, y - 26, x + 34, y + 26), outline=GOLD + (255,), width=3 * S)
    d.ellipse(sc(x - 39, y - 31, x + 39, y + 31), outline=DIM_GOLD + (200,), width=S)
    d.ellipse(sc(x + 6, y + 14, x + 34, y + 42), fill=NOTE + (90,), outline=NOTE + (230,), width=2 * S)
    f.add(lay)


def pointer(f, tip, base_x, y0, y1):
    """A callout's triangle from its edge at base_x (y0-y1) to the tip."""
    lay, d = f.draw()
    d.polygon(sc(base_x, y0, tip[0], tip[1], base_x, y1), fill=(22, 16, 12, 245), outline=GOLD + (255,))
    f.add(lay)


def right_rail(f, p):
    """The command panel's right rail: the commands from the foot up, Return at the foot."""
    n = len(p['commands'])
    top = 664 - (n - 1) * 54 - 12
    panel(f, (1032, top, 1276, 796))
    for i, item in enumerate(p['commands']):
        y = 664 - i * 54
        command(f, (1044, y, 1264, y + 46), item)
    ret(f, (1044, 726, 1264, 786))


def layout_a(p, name):
    f = frame()
    tapped(f, p['at'])
    panel(f, (4, 452, 248, 796))
    card(f, p, 16, 466, 220, 144)
    right_rail(f, p)
    reach(f)
    caption(f, ['A — the command panel\'s rails: the province\'s card left, its commands right from the foot up',
                f'commands 220x46 = {mm(220)} x {mm(46)}, Return at the foot where Close stands in the command panel;',
                'the tapped province ringed in gold, the map\'s middle open; the finger goes back to the thumbs after the tap'],
            y=20, x=250)
    f.save(name)


def layout_b(p, name):
    f = frame()
    tapped(f, p['at'])
    x, w = p['at'][0] - 60 - 300, 300
    top = p['at'][1] - 180
    panel(f, (x, top, x + w, top + 362))
    card(f, p, x + 14, top + 14, w - 28, 176)
    pointer(f, (p['at'][0] - 38, p['at'][1]), x + w - 1, p['at'][1] - 16, p['at'][1] + 16)
    right_rail(f, p)
    reach(f)
    caption(f, ['B — the card at the province, where the eyes are; the commands on the right rail, at the thumb',
                'the card beside the tapped province (the side with room), its figures as the PC\'s hover card;',
                'the commands as A\'s; the finger goes back to the right thumb'], y=20, x=250)
    f.save(name)


def layout_c(p, name, side):
    """C: one callout beside the province, the card left, the commands right; on `side` of the province."""
    f = frame()
    tapped(f, p['at'])
    n = len(p['commands'])
    w, card_w = 520, 190
    rows_h = n * 54 + 54
    h = max(rows_h, 300) + 28
    ax, ay = p['at']
    x = ax + 60 if side == 'right' else ax - 60 - w
    x = min(max(x, 8), W - 8 - w)
    top = min(max(ay - h / 2, 8), 796 - h)
    panel(f, (x, top, x + w, top + h))
    card(f, p, x + 14, top + 14, card_w, 124, compact=True)
    lay, d = f.draw()
    d.line(sc(x + card_w + 26, top + 16, x + card_w + 26, top + h - 16), fill=DIM_GOLD + (255,), width=S)
    f.add(lay)
    cx = x + card_w + 38
    for i, item in enumerate(p['commands']):
        y = top + 14 + i * 54
        command(f, (cx, y, x + w - 14, y + 46), item)
    ret(f, (x + w - 14 - 150, top + h - 14 - 46, x + w - 14, top + h - 14))
    if side == 'right':
        pointer(f, (ax + 38, ay), x + 1, ay - 16, ay + 16)
    else:
        pointer(f, (ax - 38, ay), x + w - 1, ay - 16, ay + 16)
    reach(f)
    def far(point):
        return ((point[0] - ax) ** 2 + (point[1] - ay) ** 2) ** 0.5
    near = far((cx + 110, top + 14 + 23))
    rail = far((1154, 664 + 23))                                          # A's and B's first command on the right rail
    caption(f, ['C — one callout at the province: the card and the commands together, beside the finger',
                f'its first command {mm(near)} from the tap (A\'s and B\'s on the rail: {mm(rail)});',
                'the callout on the side with room, its triangle at the province; the house style\'s "ask at the click";',
                'the rails stay the command panel\'s, which is not tied to a place'], y=20, x=250)
    f.save(name)


layout_a(CASTLE, 'A-rails.png')
layout_b(CASTLE, 'B-card-at-province.png')
layout_c(CASTLE, 'C-callout-castle.png', 'right')
layout_c(OAKS, 'C-callout-oaks.png', 'left')
