"""Mock-ups of the armies' facing in the modes other than upright (owner, 2026-10-31; docs/ui/phone.md, "Round P35").

Upright, the deployment table, the enemy attack and the scout report hold their army's front row in a gold bracket whose
head points the way the army faces (PD2f2, PA1f2a, PB4sf2m2; Devices/PhoneBracket, godot/Strategy/FacingBracket). The
PC and the tablet hold the deployment table's and the enemy attack's in the bracket it was turned from (the owner's F5,
DeploymentPanels.DrawFacing, EnemyAttackView.DrawArmyBracket) but mark nothing on the scout report; the phone held
landscape marks the deployment table's with an 18 px triangle at the caption row's right end, the enemy attack's with a
lone pointer (PhoneAttack.Pointer) and the scout report's with nothing. These draw the bracket there too.

The dumps: the screen gallery's PC and landscape passes (tools/ui/gallery.ps1 -PhoneLand -Only deployment,
deployment-search,enemy-attack,enemy-attack-scouted,scout-report; docs/ui/gallery.md). Then, from the repository's root
(Pillow and numpy; godot/fonts):
  python -I tools/ui/mock_facing_modes.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository: the dumps hold the player's CGs.

Round P35, the week-18 save. Held landscape (the dump scaled to the reference phone's 2712 x 1220): LD2f1 the part
widened into the ledger by the bracket's strip, the cards as now (the agent's recommendation), and LD2f1s the same in the
dungeon search; LD2f2 the cards 11% smaller, the part as now; LA2f1 the enemy attack's pointer become the bracket, the
cards 6% smaller; LB3sf1 the scout report's bracket left of the front row, nothing moved. The PC and the tablet: Cf1 the
scout report's bracket at the PC's size, the cards 24 px further right; F5t the deployment table and the enemy attack
without the small triangle beside "Front row", the caption centred over its cards.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

GALLERY, OUT = sys.argv[1:3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
LAND = os.path.join(GALLERY, 'phone-landscape', 'current')
PC = os.path.join(GALLERY, 'pc', 'current')
os.makedirs(OUT, exist_ok=True)

LW, LH = 2712, 1220                     # the Poco X6 Pro held landscape, reference pixels (the dump times 1.5)
PX_MM = 446 / 25.4                      # 17.6 a millimetre

# Lacquer (godot/Strategy/UiPalette.cs).
FILL, GOLD, NAME = (21, 16, 12), (196, 160, 86), (232, 199, 122)
NOTE = (255, 92, 160)                   # the mock-ups' own notes

# The bracket upright (Devices/PhoneBracket), reference pixels: the line's width, its ends short of the cards, each arm's
# end beside the middle and its bend, the head's base, tip and half width; its line this far off the cards (the PC's 7).
WIDTH, ENDS, ARM, BEND_IN, BEND_OUT, HEAD_BASE, HEAD_TIP, HEAD_HALF, OFF = 5.7, 4.5, 36, 14, 23, 14, 75, 32, 16
PC_SCALE = 252 / 573                    # the PC's bracket: the upright one over the cards' scale (573 / 252)


def font(name, size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size)))


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)


def mm(px):
    return f'{px / PX_MM:.1f} mm'


def land(name):
    return Image.open(os.path.join(LAND, name + '.png')).convert('RGB').resize((LW, LH), Image.LANCZOS)


def pc(name):
    return Image.open(os.path.join(PC, name + '.png')).convert('RGB')


SS = 4


def smooth(img, box, colour, draw):
    """draw(d, p, s) on a mask four times finer over box, p mapping reference pixels into it; the mask laid on img in colour."""
    x0, y0, x1, y1 = box
    mask = Image.new('L', ((x1 - x0) * SS, (y1 - y0) * SS), 0)
    draw(ImageDraw.Draw(mask), lambda x, y: ((x - x0) * SS, (y - y0) * SS), SS)
    mask = mask.resize((x1 - x0, y1 - y0), Image.BOX)
    img.paste(Image.new('RGB', mask.size, colour), (x0, y0), mask)


def side_bracket(img, edge, y0, y1, way, k=1.0):
    """The bracket holding a column of cards from its side: their outer edge at edge, from y0 to y1; way 1 points right, -1 left."""
    line, mid, ends = edge + way * OFF * k, (y0 + y1) / 2, edge + way * ENDS * k
    reach = line + way * HEAD_TIP * k
    box = (int(min(ends, reach) - 12), int(y0 - 12), int(max(ends, reach) + 12), int(y1 + 12))

    def arms(d, p, s):
        w = max(1, round(WIDTH * k * s))
        for end, sign in ((y0, -1), (y1, 1)):
            d.line([p(ends, end), p(line, end), p(line, mid + sign * ARM * k), p(line + way * BEND_OUT * k, mid + sign * BEND_IN * k)],
                   fill=255, width=w, joint='curve')
    smooth(img, box, GOLD, arms)
    base = line + way * HEAD_BASE * k
    smooth(img, box, NAME, lambda d, p, s: d.polygon([p(base, mid - HEAD_HALF * k), p(reach, mid), p(base, mid + HEAD_HALF * k)], fill=255))
    return line, reach


def note(d, xy, s, size=26, anchor='la'):
    d.multiline_text(xy, s, font=GOTHIC(size), fill=NOTE, anchor=anchor, stroke_width=4, stroke_fill=(0, 0, 0), spacing=6)


# --- the backdrop: the screens' wave pattern repeats every 104 reference pixels both ways -----------------------------

PERIOD = 104


def backdrop(src, patch):
    """The whole screen filled with the pattern of the clean patch (x0, y0, x1, y1) of src, in its phase."""
    a = np.asarray(src)
    x0, y0, x1, y1 = patch
    ys = y0 + (np.arange(src.height) - y0) % PERIOD
    xs = x0 + (np.arange(src.width) - x0) % PERIOD
    assert y0 + PERIOD <= y1 and x0 + PERIOD <= x1
    return Image.fromarray(a[ys][:, xs])


def ink(src, bg, box):
    """A soft mask of what src draws over the backdrop bg inside box (both images)."""
    a = np.asarray(src.crop(box)).astype(int)
    b = np.asarray(bg.crop(box)).astype(int)
    diff = np.abs(a - b).sum(axis=2)
    return Image.fromarray((np.clip((diff - 8) / 28, 0, 1) * 255).astype(np.uint8))


def move_ink(img, src, bg, box, dx=0, dy=0):
    """What src draws in box over its backdrop, laid on img dx, dy off."""
    img.paste(src.crop(box), (box[0] + dx, box[1] + dy), ink(src, bg, box))


def widen(im, at, count):
    """im with column at repeated count more times (a plain stretch of a line or a button)."""
    a = np.asarray(im)
    return Image.fromarray(np.concatenate([a[:, :at], np.repeat(a[:, at:at + 1], count, axis=1), a[:, at:]], axis=1))


# --- the deployment table held landscape (PhoneLedger) -------------------------------------------------------------------
# The part: 1716-2596 (880 wide, 6 in from the hole's strip); the cards two a row, 434 wide, 12 apart: the back row (3, 4)
# at 1716, the front row (1, 2) at 2162, 200-414 and 426-640; the ledger 116-1700. The bracket needs 95 px beside the
# front row: its line 16 off the cards, its head's tip 75 beyond (the upright figures, as the cards' scale is near).

PART = (1716, 2596)
LD_CARDS = {3: (1716, 200, 2150, 414), 4: (1716, 426, 2150, 640), 1: (2162, 200, 2596, 414), 2: (2162, 426, 2596, 640)}
STRIP = round(OFF + HEAD_TIP + 4)       # 95: the line 16 off the cards, the head's tip 75 beyond it, 4 short of the part's edge
LEDGER_EDGE = (1670, 1702)              # the ledger's right side: its scroll bar and border
LD_PATCH = (1716, 740, 2596, 990)       # a clean stretch of the backdrop under the totals


def ld_now(name):
    return land(name), ['as built: the front row\'s mark is the triangle at the caption row\'s right end, 18 px (1.0 mm)']


def ld_wide(name, places=(1, 2, 3, 4)):
    """LD2f1: the part widened into the ledger by the bracket's strip; the title band and the bar widened with it."""
    src = land(name)
    bg = backdrop(src, LD_PATCH)
    img = src.copy()
    d = STRIP
    img.paste(bg.crop((LEDGER_EDGE[0] - d, 0, PART[1], LH)), (LEDGER_EDGE[0] - d, 0))
    img.paste(src.crop((LEDGER_EDGE[0], 100, LEDGER_EDGE[1], LH)), (LEDGER_EDGE[0] - d, 100))
    move_ink(img, src, bg, (1700, 0, 2200, 136), -d)                 # the title and the subtitle, from the part's left
    move_ink(img, src, bg, (2200, 0, 2600, 136))                     # the date and the budget stay at its right
    divider = src.crop((1716, 136, 2598, 156))
    mask = ink(src, bg, (1716, 136, 2598, 156))
    divider, mask = widen(widen(divider, 600, d - d // 2), 100, d // 2), widen(widen(mask, 600, d - d // 2), 100, d // 2)
    img.paste(divider, (1716 - d, 136), mask)
    move_ink(img, src, bg, (1700, 160, 2000, 198), -d)               # "Back row"
    move_ink(img, src, bg, (2150, 160, 2540, 198), -d)               # "Front row", its triangle left out
    for place in places:
        x0, y0, x1, y1 = LD_CARDS[place]
        img.paste(src.crop((x0 - 2, y0 - 2, x1 + 2, y1 + 2)), (x0 - 2 - d, y0 - 2))
    move_ink(img, src, bg, (1700, 650, 2100, 730), -d)               # the troops from the part's left
    move_ink(img, src, bg, (2100, 650, 2600, 730))                   # the war expenses at its right
    send = src.crop((1712, 1004, 2284, 1166))
    img.paste(widen(widen(send, 520, d - d // 2), 40, d // 2), (1712 - d, 1004))
    img.paste(src.crop((2292, 1004, 2600, 1166)), (2292, 1004))
    y1 = LD_CARDS[2 if 2 in places else 1][3]
    line, reach = side_bracket(img, PART[1] - d, LD_CARDS[1][1] + 1, y1 - 1, 1)
    notes = [f'the part {d} px ({mm(d)}) wider into the ledger, the cards as now: the bracket right of the front row, its line '
             f'{OFF} px off the cards, its head ({mm(2 * HEAD_HALF)} wide, {mm(HEAD_TIP - HEAD_BASE)} deep) 4 px short of the hole\'s strip',
             'the title band, the totals and the bar as wide as the part; the ledger as much narrower at its right (it scrolls '
             'sideways); the small triangle gone']
    if len(places) == 1:
        notes = ['the dungeon search: the bracket holds its one card, its head at the card\'s middle'] + notes[:1]
    return img, notes


def ld_small(name):
    """LD2f2: the cards 11% smaller, the part as now, the bracket in the room they leave."""
    src = land(name)
    bg = backdrop(src, LD_PATCH)
    img = src.copy()
    scale = (PART[1] - PART[0] - STRIP) / (PART[1] - PART[0])
    img.paste(bg.crop((PART[0] - 4, 160, PART[1] + 4, 730)), (PART[0] - 4, 160))
    block = src.crop((PART[0] - 2, 198, PART[1] + 2, 642))
    block = block.resize((round(block.width * scale), round(block.height * scale)), Image.LANCZOS)
    img.paste(block, (PART[0] - 2, 198))
    front = PART[0] + (LD_CARDS[1][0] - PART[0]) * scale
    move_ink(img, src, bg, (1700, 160, 2000, 198))                   # "Back row" at its cards' left as now
    move_ink(img, src, bg, (2150, 160, 2540, 198), round(front - LD_CARDS[1][0]))
    rise = round(440 * (1 - scale))
    move_ink(img, src, bg, (1700, 650, 2600, 730), 0, -rise)         # the totals under the cards
    bottom = 200 + 440 * scale
    side_bracket(img, PART[0] + (PART[1] - PART[0]) * scale, 201, bottom - 1, 1)
    return img, [f'the cards {round((1 - scale) * 100)}% smaller ({round(434 * scale)} x {round(214 * scale)}), the part as now: '
                 'the bracket in the room they leave, the head as LD2f1\'s',
                 f'the names\' type 42 px → {round(42 * scale)}; the totals {rise} px higher; the small triangle gone']


# --- the enemy attack held landscape (PhoneAttack) -------------------------------------------------------------------------
# The army's panel 796-1776; the cards from 860 (64 in, past the pointer: its tip 24 in, 30 deep, 68 high), 438 wide, 16
# apart, 300 high, 14 apart, from 260; the rows' captions 26 over them.

LA_CARDS = (860, 260, 1752, 874)
LA_POINTER = (812, 520, 857, 615)
LA_TIP = 820


def la_now(name):
    return land(name), ['as built: a gold pointer left of the cards (30 px deep, 68 high) and the small triangle beside '
                        '"FRONT ROW"']


def la_bracket(name):
    """LA2f1: the pointer become the bracket, its head's tip where the pointer's is; the cards 6% smaller to make the room."""
    src = land(name)
    img = src.copy()
    dr = ImageDraw.Draw(img)
    edge = LA_TIP + HEAD_TIP + OFF                                   # 911: the cards' left edge
    scale = (LA_CARDS[2] - edge) / (LA_CARDS[2] - LA_CARDS[0])
    dr.rectangle(LA_POINTER, fill=FILL)
    dr.rectangle((LA_CARDS[0] - 4, 210, LA_CARDS[2] + 4, LA_CARDS[3] + 4), fill=FILL)
    block = src.crop((LA_CARDS[0] - 2, LA_CARDS[1] - 2, LA_CARDS[2] + 2, LA_CARDS[3] + 2))
    block = block.resize((round(block.width * scale), round(block.height * scale)), Image.LANCZOS)
    top = LA_CARDS[1] + (LA_CARDS[3] - LA_CARDS[1]) * (1 - scale) / 2
    img.paste(block, (round(edge - 2 * scale), round(top - 2 * scale)))
    w = (LA_CARDS[2] - LA_CARDS[0] - 16) / 2 * scale
    for (x0, x1), centre in (((990, 1168), edge + w / 2), ((1455, 1612), edge + w * 1.5 + 16 * scale)):
        caption = src.crop((x0, 214, x1, 250))                       # "FRONT ROW" without its triangle (951-967), "BACK ROW"
        img.paste(caption, (round(centre - (x1 - x0) / 2), round(214 + top - LA_CARDS[1])))
    side_bracket(img, edge, top, top + (LA_CARDS[3] - LA_CARDS[1]) * scale, -1)
    return img, [f'the pointer become the bracket: its head\'s tip where the pointer\'s is, its line {OFF} px off the cards, '
                 f'the head {mm(2 * HEAD_HALF)} wide, {mm(HEAD_TIP - HEAD_BASE)} deep (the pointer\'s 3.9 x 1.7 mm)',
                 f'the cards {round((1 - scale) * 100)}% smaller ({round(438 * scale)} x {round(300 * scale)}), centred on the '
                 'rows\' middle as now; the small triangle gone, "FRONT ROW" centred over its cards']


# --- the scout report held landscape (LB3s) ------------------------------------------------------------------------------

def scout_now(name):
    return land(name), ['as built: "FRONT ROW" with no mark of the way the army faces']


def lb_bracket(name):
    """LB3sf1: the bracket left of the front row in the black beside it; nothing moved."""
    img = land(name)
    a = np.asarray(img).astype(int)
    rows = np.nonzero(np.abs(a[300:900, 900]).sum(axis=1) > 60)[0] + 300
    top, bottom = rows.min(), rows.max()                             # the front row's cards, 841 from the left
    side_bracket(img, 841, top, bottom, -1)
    return img, ['the bracket left of the front row (Ex, Sakanaku), its head pointing left as the enemy attack\'s; the black '
                 'beside the cards has the room, nothing moves']


# --- the PC and the tablet --------------------------------------------------------------------------------------------------
# The scout report (ScoutReportView): the cards 282 x 150 in columns at 380 and 680, rows at 190 and 350, on black; "front
# row" and "back row" under them at 520. Kanami's portrait ends at 340: the bracket (33 px to its head's tip) needs the
# cards 24 px further right.

def c_now(name):
    return pc(name), ['as built: no mark of the facing; "front row" and "back row" under the cards']


def c_bracket(name):
    """Cf1: the PC's bracket left of the front row, the cards and their captions 24 px right."""
    src = pc(name)
    img = src.copy()
    dx = 24
    ImageDraw.Draw(img).rectangle((370, 180, 1000, 545), fill=(0, 0, 0))
    img.paste(src.crop((372, 182, 970, 545)), (372 + dx, 182))
    _, reach = side_bracket(img, 380 + dx, 190, 500, -1, PC_SCALE)
    return img, [f'the PC\'s bracket (as the enemy attack\'s) left of the front row, its head pointing left; the cards and '
                 f'their captions {dx} px further right, the head {round(reach - 340)} px clear of Kanami\'s portrait']


def f5_now(name):
    return pc(name), ['as built: the bracket, and a small triangle beside "Front row"']


def f5_plain(caption, triangle, dx):
    """F5t: the small triangle beside "Front row" gone, the caption centred over its cards."""
    def draw(name):
        src = pc(name)
        img = src.copy()
        fill = tuple(int(v) for v in np.asarray(src)[caption[1] - 6, caption[0] - 10])
        ImageDraw.Draw(img).rectangle((min(caption[0], triangle[0]) - 1, caption[1] - 2, max(caption[2], triangle[2]) + 1,
                                       caption[3] + 2), fill=fill)
        img.paste(src.crop(caption), (caption[0] + dx, caption[1]))
        return img, ['the small triangle gone, as upright: the bracket alone says the way; "Front row" centred over its cards']
    return draw


# --- the sheets ------------------------------------------------------------------------------------------------------------

def wrapped(d, text, face, width):
    words, lines = text.split(' '), ['']
    for w in words:
        trial = (lines[-1] + ' ' + w).strip()
        if d.textlength(trial, font=face) <= width or not lines[-1]:
            lines[-1] = trial
        else:
            lines.append(w)
    return lines


def sheet(name, title, cells, columns, cell_w, crops=None):
    """cells: (label, dump, variant); each picture at cell_w (cropped to crops[dump] first when given), its notes under it."""
    drawn = []
    for label, dump, f in cells:
        im, notes = f(dump)
        im.save(os.path.join(OUT, f'{label.split()[0].lower()}-{dump}.png'))
        if crops and dump in crops:
            im = im.crop(crops[dump])
        drawn.append((label, im.resize((cell_w, round(im.height * cell_w / im.width)), Image.LANCZOS), notes))
    gap, head, label_h, notes_h = 24, 80, 40, 150
    rows = (len(drawn) + columns - 1) // columns
    row_h = [label_h + max(im.height for _, im, _ in drawn[r * columns:(r + 1) * columns]) + notes_h for r in range(rows)]
    out = Image.new('RGB', (columns * cell_w + (columns + 1) * gap, head + sum(row_h) + rows * gap), (40, 40, 40))
    d = ImageDraw.Draw(out)
    d.text((gap, 20), title, font=GOTHIC(30), fill=(255, 255, 255))
    y = head
    for r in range(rows):
        for c, (label, im, notes) in enumerate(drawn[r * columns:(r + 1) * columns]):
            x = gap + c * (cell_w + gap)
            d.text((x, y + label_h - 10), label, font=GOTHIC(24), fill=(255, 255, 255), anchor='ld')
            out.paste(im, (x, y + label_h))
            ny = y + label_h + im.height + 10
            for n in notes:
                for line in wrapped(d, n, GOTHIC(19), cell_w):
                    d.text((x, ny), line, font=GOTHIC(19), fill=NOTE)
                    ny += 24
                ny += 4
        y += row_h[r] + gap
    out.save(os.path.join(OUT, name + '.png'))
    print(os.path.join(OUT, name + '.png'), out.size)


PARTY_CROP = (1500, 0, LW, LH)          # the deployment table's right: the ledger's edge, the part, the hole's strip
sheet('p35-landscape-deployment', 'Round P35 — the party\'s facing held landscape: the week-18 save\'s attack on Oaks (6), '
      'the dungeon search last', [
          ('Now', 'deployment', ld_now),
          ('LD2f1 — the part wider into the ledger (recommended)', 'deployment', ld_wide),
          ('LD2f2 — the cards smaller', 'deployment', ld_small),
          ('LD2f1s — LD2f1, the dungeon search', 'deployment-search', lambda n: ld_wide(n, places=(1,)))],
      2, 1000, {'deployment': PARTY_CROP, 'deployment-search': PARTY_CROP})
sheet('p35-landscape-deployment-whole', 'Round P35 — LD2f1 on the whole screen held landscape (the ledger narrower at its right)', [
          ('Now', 'deployment', ld_now), ('LD2f1 — the part wider into the ledger', 'deployment', ld_wide)], 1, 1356)

sheet('p35-landscape-attack', 'Round P35 — the attacking army\'s facing held landscape: the rebels on Ys (5), before and '
      'after Kanami\'s scouting; the scout report', [
          ('Now', 'enemy-attack', la_now), ('LA2f1 — the pointer become the bracket', 'enemy-attack', la_bracket),
          ('Now', 'enemy-attack-scouted', la_now), ('LA2f1 — the same, scouted', 'enemy-attack-scouted', la_bracket),
          ('Now', 'scout-report', scout_now),
          ('LB3sf1 — the scout report\'s bracket', 'scout-report', lb_bracket)],
      2, 1100)

sheet('p35-pc', 'Round P35 — the PC and the tablet: the scout report\'s facing; the small triangle beside "Front row"', [
          ('Now', 'scout-report', c_now), ('Cf1 — the scout report\'s bracket', 'scout-report', c_bracket),
          ('Now', 'deployment', f5_now),
          ('F5t — the deployment table without the triangle', 'deployment',
           f5_plain((1047, 422, 1130, 436), (1130, 422, 1138, 436), 8)),
          ('Now', 'enemy-attack', f5_now),
          ('F5t — the enemy attack without the triangle', 'enemy-attack',
           f5_plain((779, 208, 861, 221), (763, 208, 777, 221), -8))],
      2, 1100, {'deployment': (700, 360, 1280, 800), 'enemy-attack': (590, 150, 1260, 570)})

sheet('p35-landscape-detail', 'Round P35 — held landscape at the dump\'s pixels: the army\'s panel, the scout report\'s cards', [
          ('Now', 'enemy-attack-scouted', la_now), ('LA2f1', 'enemy-attack-scouted', la_bracket),
          ('Now', 'scout-report', scout_now), ('LB3sf1', 'scout-report', lb_bracket)],
      2, 680, {'enemy-attack-scouted': (780, 150, 1800, 1100), 'scout-report': (680, 250, 1700, 960)})
