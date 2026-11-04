"""Mock-ups of a larger scroll bar on the phone held upright (owner, 2026-10-31; docs/ui/phone.md, "Round P31").

The scroll bars of our phone screens are 6 reference pixels wide (0.3 mm on the Poco X6 Pro) and drawn in the cells'
own dim gold, at the screen's very edge: a player hardly sees that a list scrolls. The owner asked for a larger bar,
drawn first on Treasure upright (PhonePickersPortrait.ScrollBar), its size then the reference for the others.

These draw it wider, in the lacquer's gold on a groove of its own, its right edge at the picture's, the tabs' and End's
(the room's side margin, 16 px in from the screen's edge, out of Android's back gesture at the edge); the grid is narrowed
by the bar and a gap, each cell giving up the same width from its middle. The bar stays a sign only, as now: the finger
scrolls the grid anywhere.

The dump: the screen gallery's portrait pass (tools/ui/gallery.ps1 -PhonePortrait -Only treasure; docs/ui/gallery.md) from before
S2 was built (2026-10-31: the thin bar at the screen's edge, which track_rows finds). Then,
from the repository's root (Pillow and numpy; godot/fonts):
  python -I tools/ui/mock_scroll_bar.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository: the dumps hold the player's CGs.

Round P31, over treasure (the week-18 save, the Characters view, the grid at its top: 9 of its 17 rows in view): S1 a bar
12 px wide (0.7 mm, Android's own bar's 4 dp), S2 20 px (1.1 mm), S3 28 px (1.6 mm). Each also as the whole screen at
the phone's pixels (p31-<variant>-phone.png), to be looked at on the phone itself.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

GALLERY, OUT = sys.argv[1:3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)

PX_MM = 446 / 25.4                      # the Poco X6 Pro: 17.6 reference pixels a millimetre
NOTE = (255, 92, 160)                   # the mock-ups' own notes
SIZE = (1220, 2712)                     # the reference phone upright; the dump is half of it

# The lacquer (godot/Strategy/UiPalette.cs): the thumb's gold and its lit edge, the groove's line and fill.
GOLD, NAME, DIM_GOLD, OFF_LINE = (196, 160, 86), (232, 199, 122), (110, 86, 46), (66, 58, 48)
GROOVE = (24, 20, 16)

# PhonePickersPortrait: the grid's sides (SideIn) and the gap between its cells (CellGap), its rows; the bar now.
SIDE_IN, CELL_GAP, ROWS, SHOWN, ROW_PITCH, ROW_GAP = 16, 8, 17, 9, 124, 6
NOW_WIDTH, NOW_IN, THUMB_LEAST = 6, 11, 60
BAR_GAP = 10                            # the new bar's distance from the grid


def font(name, size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size)))


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)


def mm(px):
    return f'{px / PX_MM:.1f} mm'


def dump(name):
    return Image.open(os.path.join(GALLERY, 'phone-portrait', 'current', name + '.png')).convert('RGB')


def track_rows(small):
    """The old bar's track in the dump (half the reference): its top and bottom in reference pixels."""
    a = np.asarray(small).astype(int)
    x = (SIZE[0] - NOW_IN + NOW_WIDTH // 2) // 2
    lit = np.where(a[:, x].sum(axis=1) > 60)[0]
    return int(lit.min()) * 2, int(lit.max() + 1) * 2


def columns(right):
    """The grid's three columns from the room's side to `right`, as integer [left, right) pairs."""
    w = (right - SIDE_IN - 2 * CELL_GAP) / 3
    return [(round(SIDE_IN + i * (w + CELL_GAP)), round(SIDE_IN + i * (w + CELL_GAP) + w)) for i in range(3)]


def narrowed(img, top, bottom, right):
    """The grid between `top` and `bottom` laid out again up to `right`: each cell gives up columns from its middle, the
    gaps keep theirs, the rest to the room's side is the backdrop (the margin's near-black column by the old grid)."""
    old, new = columns(SIZE[0] - SIDE_IN), columns(right)
    src = np.asarray(img)
    out = src.copy()
    mapping = {}
    for (ol, orr), (nl, nr) in zip(old, new):
        half = (nr - nl) // 2
        for x in range(nl, nr):
            mapping[x] = ol + (x - nl) if x - nl < half else orr - (nr - x)
    for i in range(2):
        for x in range(new[i][1], new[i + 1][0]):
            mapping[x] = old[i][1] + (x - new[i][1])
    for x in range(new[2][1], SIZE[0]):
        mapping[x] = old[2][1] + 1                          # the backdrop between the old grid and its bar
    for x, sx in mapping.items():
        out[top:bottom, x] = src[top:bottom, sx]
    return Image.fromarray(out)


def bar(img, top, width):
    """The new bar: a groove with a dim gold line as tall as the rows in view (the old track ran on to the grid area's
    foot, past the last row), the thumb in gold inside it, at the grid's top."""
    bottom = top + SHOWN * ROW_PITCH - ROW_GAP
    d = ImageDraw.Draw(img)
    x1 = SIZE[0] - SIDE_IN
    x0 = x1 - width
    r = width / 2
    d.rounded_rectangle((x0, top, x1 - 1, bottom - 1), radius=r, fill=GROOVE, outline=DIM_GOLD, width=2)
    inset = 3 if width >= 16 else 2
    h = max(THUMB_LEAST, (bottom - top) * SHOWN / ROWS)
    t0 = top + inset
    d.rounded_rectangle((x0 + inset, t0, x1 - 1 - inset, round(top + h) - inset), radius=r - inset, fill=GOLD)
    # the lit edge: a line of the name's gold down the thumb's left, under its rounding
    lx = x0 + inset + max(2, round(width / 6))
    d.line((lx, t0 + r, lx, round(top + h) - inset - r), fill=NAME, width=max(1, round(width / 10)))


def variant(width):
    small = dump('treasure')
    img = small.resize(SIZE, Image.LANCZOS)
    if width is None:
        return img
    top, bottom = track_rows(small)
    img = narrowed(img, top - 2, bottom + 2, SIZE[0] - SIDE_IN - width - BAR_GAP)
    bar(img, top, width)
    return img


def notes(width):
    if width is None:
        return [f'as built: {NOW_WIDTH} px ({mm(NOW_WIDTH)}) wide, {NOW_IN - NOW_WIDTH} px from the screen\'s edge, the '
                'thumb in the cells\' own dim gold on a faint track: it reads as one more cell line']
    narrower = width + BAR_GAP
    return [f'{width} px ({mm(width)}) wide: a groove with a dim gold line, the thumb in the lacquer\'s gold with a lit '
            'edge, rounded, as tall as the rows in view; its right edge at the picture\'s, the tabs\' and End\'s (16 px in '
            'from the screen\'s edge)',
            f'the grid {narrower} px narrower ({mm(narrower)}: the bar and a {BAR_GAP} px gap), each cell '
            f'{narrower / 3:.0f} px narrower from its middle; the bar a sign only, as now — the finger scrolls the grid']


VARIANTS = [('Now', None), ('S1 — 12 px', 12), ('S2 — 20 px (recommended)', 20), ('S3 — 28 px', 28)]


def wrapped(d, text, face, width):
    words, lines = text.split(' '), ['']
    for w in words:
        trial = (lines[-1] + ' ' + w).strip()
        if d.textlength(trial, font=face) <= width or not lines[-1]:
            lines[-1] = trial
        else:
            lines.append(w)
    return lines


def sheet(name, title, scale, crop=None, foot=230):
    """The variants side by side, each scaled (after crop, reference pixels), with their labels and notes."""
    drawn = []
    for label, width in VARIANTS:
        im = variant(width)
        if crop:
            im = im.crop(crop)
        drawn.append((label, im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS), notes(width)))
    w, h = drawn[0][1].size
    gap, head = 24, 110
    out = Image.new('RGB', (len(drawn) * w + (len(drawn) + 1) * gap, head + h + foot), (40, 40, 40))
    d = ImageDraw.Draw(out)
    d.text((gap, 18), title, font=GOTHIC(34), fill=(255, 255, 255))
    for i, (label, im, ns) in enumerate(drawn):
        x, y = gap + i * (w + gap), head
        d.text((x, y - 14), label, font=GOTHIC(26), fill=(255, 255, 255), anchor='ld')
        out.paste(im, (x, y))
        y += h + 12
        for n in ns:
            for t in wrapped(d, n, GOTHIC(20), w):
                d.text((x, y), t, font=GOTHIC(20), fill=NOTE)
                y += 26
            y += 6
    out.save(os.path.join(OUT, name + '.png'))
    print(os.path.join(OUT, name + '.png'), out.size)


sheet('p31-up', 'Round P31 — a larger scroll bar upright: Treasure, the week-18 save, 9 of the grid\'s 17 rows in view',
      scale=0.5)
sheet('p31-up-detail', 'Round P31 — the grid\'s right column and the bar at the phone\'s pixels', scale=0.75,
      crop=(640, 1180, 1220, 2480), foot=330)
for label, width in VARIANTS:
    key = 'now' if width is None else label.split(' ')[0].lower()
    path = os.path.join(OUT, f'p31-{key}-phone.png')
    variant(width).save(path)
    print(path)
