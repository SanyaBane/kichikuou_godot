"""Mock-ups of the result plate after a troop order, held upright (owner, 2026-10-31; docs/ui/phone.md, "Round P22").

The plate (godot/Strategy/OrderResultPlate.cs, the owner's R4/R5 of docs/ui/subordinates.md, §6) is the PC's 780×150
scaled to the room's width: 230 reference pixels high, its lines 1.1-2.4 mm. These redraw it at the phone's type.

The dumps: the screen gallery's portrait pass (tools/ui/gallery.ps1 -PhonePortrait -Only subordinates,subordinates-result;
docs/ui/gallery.md). Then, from the repository's root (Pillow and numpy; godot/fonts):
  python -I tools/ui/mock_result_plate.py <gallery dir> <extract dir> <out dir> < /dev/null
<extract dir> is KICHIKUOU_EXTRACT_DIR (cg_png: Haurein's face). Keep <out dir> outside the repository: the dumps hold
the player's CGs.

Round P22, over subordinates-result (Haurein (unit 12) replenished, the week-18 save): each variant after Replenish (R4,
the dump's figures) and after Add New Troops (R5, its +100 step; the troops and maximum after it are made up, marked).
PU1r1 a card in the middle, the PC's plate stacked: the title on top, the face and the unit, the troops before and
after with the men gained, a bar of the troops (the gain in green, R5's old maximum ticked), the cost and the budget in
two columns. PU1r2 the same at the thumb, a sheet over the orders' panel, the ledger above it lit. PU1r3 the week
report's account: the face on top, then label and figure rows (Troops, Gained, Cost, Budget). The agent recommends
PU1r1.

Round P23 (the owner chose PU1r1, the rest of the screen darker): PU1r1 with the screen under it dimmed 50% (PU1r1d1),
65% (PU1r1d2, the agent's recommendation) and 80% (PU1r1d3) beside round P22's 27%, after Replenish.
"""
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

GALLERY, EXTRACT, OUT = sys.argv[1:4]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
UP = os.path.join(GALLERY, 'phone-portrait', 'current')
os.makedirs(OUT, exist_ok=True)

PW, PH = 1220, 2712                     # the Poco X6 Pro upright, reference pixels
PX_MM = 446 / 25.4                      # 17.6 a millimetre
TOP, BAR = 110, (2504, 2652)            # the camera's strip; the bottom bar
ROOM_MID = (TOP + BAR[0]) // 2
ORDERS = (4, 1868, 1216, 2484)          # the orders' panel in the dump, doubled

# Lacquer (godot/Strategy/UiPalette.cs).
PANEL, GOLD, DIM_GOLD = (22, 16, 12), (196, 160, 86), (110, 86, 46)
NAME, SUBTITLE, LABEL, VALUE = (232, 199, 122), (160, 140, 104), (150, 134, 108), (236, 226, 204)
PLUS, MINUS, QUIET = (150, 196, 110), (222, 110, 90), (176, 160, 130)
TRACK, BAR_LOW = (52, 44, 34), (170, 50, 36)
NOTE = (255, 92, 160)                   # the mock-ups' own notes
DIM = 0.27                              # OrderResultPlate.PlateDim


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size)))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)
CINZEL = lambda size, w=700: font('Cinzel-VariableFont_wght.ttf', size, w)


def mm(px):
    return f'{px / PX_MM:.1f} mm'


def note(d, xy, s, size=30, anchor='la'):
    d.multiline_text(xy, s, font=GOTHIC(size), fill=NOTE, anchor=anchor, stroke_width=4, stroke_fill=(0, 0, 0), spacing=6)


# The two plates' figures: R4 the dump's (Haurein's replenish), R5 its +100 step (troops and maximum after it made up).
R4 = dict(title='Troops Replenished', name='Haurein', type='Leazas Soldiers', before=209, after=1400, max_before=1400,
          max_after=1400, gain='+1,191', cost='−2,300K', budget='114,100K', made_up=False)
R5 = dict(title='New Troops', name='Haurein', type='Leazas Soldiers', before=209, after=309, max_before=1400,
          max_after=1500, gain='+100', cost='−48,600K', budget='67,800K', made_up=True)


def number(n):
    return f'{n:,}'


def troops_parts(r):
    """The troops line's parts around its arrow: "209" → "1,400"; R5 with the maxima."""
    if r is R4:
        return number(r['before']), number(r['after'])
    return f"{number(r['before'])}/{number(r['max_before'])}", f"{number(r['after'])}/{number(r['max_after'])}"


# --- the screen under the plate ------------------------------------------------------------------------------------

def clean_screen():
    """subordinates-result without its plate and its dim: the band under the plate from subordinates (the same rows,
    neither chosen), the rest brightened back."""
    res = np.asarray(Image.open(os.path.join(UP, 'subordinates-result.png')).convert('RGB')).astype(np.float32)
    plain = np.asarray(Image.open(os.path.join(UP, 'subordinates.png')).convert('RGB')).astype(np.float32)
    out = np.clip(res / (1 - DIM), 0, 255)
    out[632:756] = plain[632:756]
    return Image.fromarray(out.astype(np.uint8)).resize((PW, PH), Image.LANCZOS)


def dimmed(img, box=None, amount=DIM):
    a = np.asarray(img).astype(np.float32)
    if box is None:
        a *= 1 - amount
    else:
        x0, y0, x1, y1 = box
        a[y0:y1, x0:x1] *= 1 - amount
    return Image.fromarray(a.astype(np.uint8))


def haurein(size):
    im = np.array(Image.open(os.path.join(EXTRACT, 'cg_png', 'CG0010.png')).convert('RGB'))
    im[(im[..., 0] > 200) & (im[..., 1] < 60) & (im[..., 2] > 200)] = (52, 58, 74)        # the palette's key
    return Image.fromarray(im).crop((32, 7, 160, 135)).resize((size, size), Image.LANCZOS)


# --- the house's parts ---------------------------------------------------------------------------------------------

def panel(d, box):
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=PANEL)
    d.rectangle(box, outline=GOLD, width=3)
    d.rectangle((x0 + 8, y0 + 8, x1 - 8, y1 - 8), outline=DIM_GOLD, width=2)
    for cx, cy in ((x0 + 8, y0 + 8), (x1 - 8, y0 + 8), (x0 + 8, y1 - 8), (x1 - 8, y1 - 8)):
        d.polygon([(cx, cy - 7), (cx + 7, cy), (cx, cy + 7), (cx - 7, cy)], fill=GOLD)


def divider(d, x0, x1, y):
    mid = (x0 + x1) / 2
    d.line([(x0, y), (mid - 18, y)], fill=DIM_GOLD, width=2)
    d.line([(mid + 18, y), (x1, y)], fill=DIM_GOLD, width=2)
    d.polygon([(mid, y - 8), (mid + 8, y), (mid, y + 8), (mid - 8, y)], fill=GOLD)


def triangle(d, x, y, size=24):
    d.polygon([(x - size, y - size * 0.6), (x + size, y - size * 0.6), (x, y + size * 0.6)], fill=GOLD)


def face(img, d, xy, size):
    img.paste(haurein(size), xy)
    d.rectangle((xy[0], xy[1], xy[0] + size, xy[1] + size), outline=GOLD, width=3)


def troops_line(d, x, y, r, size, anchor='l', gain_size=None, width=None):
    """The troops before → after in Cinzel, the arrow in the game's font (Cinzel has none), the gain after it in green
    (under it when the line would pass `width`); y is the line's middle. Returns the bottom of what it drew."""
    a, b = troops_parts(r)
    f, arrow = CINZEL(size), GOTHIC(size - 4)
    gap = size * 0.22
    widths = [d.textlength(a, font=f), d.textlength('→', font=arrow), d.textlength(b, font=f)]
    line_w = sum(widths) + 4 * gap
    gf = CINZEL(gain_size or round(size * 0.7))
    gain_w = d.textlength(r['gain'], font=gf)
    beside = width is None or line_w + 1.2 * gap + gain_w <= width
    total = line_w + (1.2 * gap + gain_w if beside else 0)
    cx = x - total / 2 if anchor == 'm' else x
    d.text((cx, y), a, font=f, fill=VALUE, anchor='lm')
    cx += widths[0] + 2 * gap
    d.text((cx, y + size * 0.04), '→', font=arrow, fill=GOLD, anchor='lm')
    cx += widths[1] + 2 * gap
    d.text((cx, y), b, font=f, fill=VALUE, anchor='lm')
    cx += widths[2]
    if beside:
        d.text((cx + 1.2 * gap, y + size * 0.06), r['gain'], font=gf, fill=PLUS, anchor='lm')
        return y + size * 0.6
    gx = x if anchor != 'm' else x - gain_w / 2
    d.text((gx, y + size * 1.05), r['gain'], font=gf, fill=PLUS, anchor='lm')
    return y + size * 1.05 + size * 0.5


def troops_bar(d, x0, x1, y, r, h=22):
    """The troops after the order on a bar as wide as the new maximum: the troops before in the ledger's low red, the
    men gained in green, R5's old maximum ticked."""
    m = r['max_after']
    d.rectangle((x0, y, x1, y + h), fill=TRACK)
    xb = x0 + (x1 - x0) * r['before'] / m
    xa = x0 + (x1 - x0) * r['after'] / m
    d.rectangle((x0, y, xb, y + h), fill=BAR_LOW)
    d.rectangle((xb, y, xa, y + h), fill=PLUS)
    if r['max_before'] != m:
        xt = x0 + (x1 - x0) * r['max_before'] / m
        d.line([(xt, y - 8), (xt, y + h + 8)], fill=NAME, width=3)
    d.rectangle((x0, y, x1, y + h), outline=DIM_GOLD, width=2)


def two_columns(d, x0, x1, y, r, label=32, figure=56):
    """Cost | Budget: the labels over the figures, a rule between."""
    mid = (x0 + x1) / 2
    d.line([(mid, y - 10), (mid, y + label + figure + 34)], fill=DIM_GOLD, width=2)
    for cx, word, value, colour in ((x0 + (mid - x0) / 2, 'COST', r['cost'], MINUS),
                                    (mid + (x1 - mid) / 2, 'BUDGET', r['budget'], NAME)):
        d.text((cx, y + label / 2), word, font=CINZEL(label, 600), fill=LABEL, anchor='mm')
        d.text((cx, y + label + 24 + figure / 2), value, font=CINZEL(figure), fill=colour, anchor='mm')
    return y + label + 24 + figure


def made_up(r):
    return ['R5: the troops and maximum after +100 made up'] if r['made_up'] else []


def fitted(d, s, face, size, width):
    while size > 10 and d.textlength(s, font=face(size)) > width:
        size -= 1
    return face(size), size


# --- the variants --------------------------------------------------------------------------------------------------

def pu1r1(r, dim=DIM):
    """A card in the middle: the PC's plate stacked at the phone's type."""
    img = dimmed(clean_screen(), amount=dim)
    d = ImageDraw.Draw(img)
    h = 800
    x0, x1, y0 = 16, PW - 16, ROOM_MID - 400
    y1 = y0 + h
    panel(d, (x0, y0, x1, y1))
    title, size = fitted(d, r['title'], BRUSH, 72, x1 - x0 - 120)
    d.text((PW / 2, y0 + 92), r['title'], font=title, fill=NAME, anchor='mm')
    divider(d, x0 + 40, x1 - 40, y0 + 166)
    fs = 230
    face(img, d, (x0 + 48, y0 + 202), fs)
    tx = x0 + 48 + fs + 40
    d.text((tx, y0 + 250), r['name'], font=BRUSH(60), fill=NAME, anchor='lm')
    d.text((tx + 2, y0 + 316), r['type'].upper(), font=CINZEL(34, 600), fill=SUBTITLE, anchor='lm')
    troops_line(d, tx, y0 + 396, r, 60, gain_size=44, width=x1 - 48 - tx)
    troops_bar(d, x0 + 48, x1 - 48, y0 + 476, r)
    d.text((x0 + 48, y0 + 534), 'TROOPS', font=CINZEL(28, 600), fill=LABEL, anchor='lm')
    d.text((x1 - 48, y0 + 534), f"{number(r['after'])} / {number(r['max_after'])}", font=CINZEL(30, 600), fill=QUIET,
           anchor='rm')
    divider(d, x0 + 40, x1 - 40, y0 + 590)
    two_columns(d, x0 + 40, x1 - 40, y0 + 626, r)
    triangle(d, x1 - 46, y1 - 40)
    return img, [f'{h} px ({mm(h)}) high in the room\'s middle, the room less 16 a side, the screen dimmed',
                 f'title {size} px · name 60 · face {fs} ({mm(fs)}) · troops 60 · cost and budget 56 '
                 f'({mm(56 * 0.7)} digits) · labels 28-34'] + made_up(r)


def pu1r2(r):
    """At the thumb: a sheet over the orders' panel, where the finger pressed; the ledger above it stays lit."""
    img = clean_screen()
    img = dimmed(img, (0, 0, PW, ORDERS[1]), 0.18)
    img = dimmed(img, (0, ORDERS[1], PW, PH), 0.5)
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = 16, ORDERS[1] - 16, PW - 16, BAR[0] - 12
    panel(d, (x0, y0, x1, y1))
    fs = 196
    face(img, d, (x0 + 40, y0 + 40), fs)
    tx = x0 + 40 + fs + 36
    d.text((tx, y0 + 82), r['title'], font=BRUSH(64), fill=NAME, anchor='lm')
    d.text((tx + 2, y0 + 148), f"{r['name']} · {r['type']}".upper(), font=CINZEL(32, 600), fill=SUBTITLE, anchor='lm')
    troops_line(d, tx, y0 + 214, r, 52, gain_size=40, width=x1 - 40 - tx)
    troops_bar(d, x0 + 40, x1 - 40, y0 + 276, r)
    d.text((x0 + 40, y0 + 330), 'TROOPS', font=CINZEL(28, 600), fill=LABEL, anchor='lm')
    d.text((x1 - 40, y0 + 330), f"{number(r['after'])} / {number(r['max_after'])}", font=CINZEL(30, 600), fill=QUIET,
           anchor='rm')
    divider(d, x0 + 40, x1 - 40, y0 + 384)
    two_columns(d, x0 + 40, x1 - 40, y0 + 436, r)
    triangle(d, x1 - 46, y1 - 40)
    return img, [f'{y1 - y0} px ({mm(y1 - y0)}) high over the orders\' panel, where the finger pressed; the ledger '
                 f'above dimmed less, the chosen row in view',
                 f'title 64 px · face {fs} ({mm(fs)}) · troops 52 · cost and budget 56 · labels 28-32'] + made_up(r)


def pu1r3(r):
    """The week report's account: the face on top, then a row a figure."""
    img = dimmed(clean_screen())
    d = ImageDraw.Draw(img)
    h = 1010
    x0, x1, y0 = 16, PW - 16, ROOM_MID - h // 2
    y1 = y0 + h
    panel(d, (x0, y0, x1, y1))
    fs = 300
    face(img, d, ((PW - fs) // 2, y0 + 48), fs)
    d.text((PW / 2, y0 + 48 + fs + 70), r['title'], font=BRUSH(72), fill=NAME, anchor='mm')
    d.text((PW / 2, y0 + 48 + fs + 140), f"{r['name']} · {r['type']}".upper(), font=CINZEL(34, 600), fill=SUBTITLE,
           anchor='mm')
    divider(d, x0 + 40, x1 - 40, y0 + 48 + fs + 196)
    lx, rx, y = x0 + 64, x1 - 64, y0 + 48 + fs + 262
    a, b = troops_parts(r)
    rows = [('Troops', f'{a}  →  {b}', VALUE), ('Gained', r['gain'], PLUS), (None, None, None),
            ('Cost', r['cost'], MINUS), ('Budget', r['budget'], NAME)]
    for label, value, colour in rows:
        if label is None:
            d.line([(lx, y - 20), (rx, y - 20)], fill=DIM_GOLD, width=2)
            y += 24
            continue
        d.text((lx, y), label, font=CINZEL(40, 600), fill=LABEL, anchor='lm')
        if '→' in value:
            f, arrow = CINZEL(54), GOTHIC(50)
            wb, wa = d.textlength(b, font=f), d.textlength('→', font=arrow)
            d.text((rx, y), b, font=f, fill=colour, anchor='rm')
            d.text((rx - wb - 16, y + 2), '→', font=arrow, fill=GOLD, anchor='rm')
            d.text((rx - wb - wa - 32, y), a, font=f, fill=colour, anchor='rm')
        else:
            d.text((rx, y), value, font=CINZEL(54), fill=colour, anchor='rm')
        y += 92
    triangle(d, x1 - 46, y1 - 40)
    return img, [f'{h} px ({mm(h)}) high in the room\'s middle; the week report\'s rows, a label left and its figure '
                 f'right', f'face {fs} ({mm(fs)}) · title 72 px · labels 40 · figures 54'] + made_up(r)


def now(r):
    """As built (the gallery's dump), doubled; R5 not dumped."""
    if r is R5:
        img = dimmed(clean_screen())
        d = ImageDraw.Draw(img)
        note(d, (PW / 2, ROOM_MID), 'R5 is not in the gallery\n(the same plate, "New Troops")', size=40, anchor='mm')
        return img, ['R5 as built: R4\'s plate with "New Troops" and the maxima']
    img = Image.open(os.path.join(UP, 'subordinates-result.png')).convert('RGB').resize((PW, PH), Image.LANCZOS)
    return img, ['the PC\'s 780×150 at the width: 232 px (13.2 mm) high', 'its lines 20-43 px (1.1-2.4 mm)']


VARIANTS = [('Now', now), ('PU1r1 — a card in the middle (recommended)', pu1r1),
            ('PU1r2 — at the thumb, over the orders', pu1r2), ('PU1r3 — the week report\'s account', pu1r3)]


def wrapped(d, text, face, width):
    words, lines = text.split(' '), ['']
    for w in words:
        trial = (lines[-1] + ' ' + w).strip()
        if d.textlength(trial, font=face) <= width or not lines[-1]:
            lines[-1] = trial
        else:
            lines.append(w)
    return lines


def sheet(r, name, title, variants=VARIANTS):
    drawn = [(label, *f(r)) for label, f in variants]
    gap, head, foot = 24, 110, 190
    w = PW // 2
    out = Image.new('RGB', (len(drawn) * w + (len(drawn) + 1) * gap, head + PH // 2 + foot), (40, 40, 40))
    d = ImageDraw.Draw(out)
    d.text((gap, 18), title, font=GOTHIC(34), fill=(255, 255, 255))
    for i, (label, im, notes) in enumerate(drawn):
        x = gap + i * (w + gap)
        d.text((x, head - 14), label, font=GOTHIC(24), fill=(255, 255, 255), anchor='ld')
        out.paste(im.resize((w, PH // 2), Image.LANCZOS), (x, head))
        y = head + PH // 2 + 14
        for n in notes:
            for line in wrapped(d, n, GOTHIC(20), w):
                d.text((x, y), line, font=GOTHIC(20), fill=NOTE)
                y += 26
            y += 6
    out.save(os.path.join(OUT, name + '.png'))
    print(os.path.join(OUT, name + '.png'), out.size)


if __name__ == '__main__':
    for r, key in ((R4, 'r4'), (R5, 'r5')):
        for label, f in VARIANTS[1:]:
            f(r)[0].save(os.path.join(OUT, f'{label.split()[0].lower()}-{key}.png'))
    sheet(R4, 'p22-r4', 'Round P22 — the result plate upright, after Replenish (R4): Haurein (unit 12), the week-18 save')
    sheet(R5, 'p22-r5', 'Round P22 — after Add New Troops (R5): +100 for 48,600K (the troops and maximum after it made up)')


    def darker(dim):
        def draw(r):
            img, notes = pu1r1(r, dim)
            return img, [f'the screen under the plate dimmed {round(dim * 100)}% (now {round(DIM * 100)}%)'] + notes[1:]
        return draw


    P23 = [('PU1r1 — as round P22, 27%', darker(DIM)), ('PU1r1d1 — 50%', darker(0.5)),
           ('PU1r1d2 — 65% (recommended)', darker(0.65)), ('PU1r1d3 — 80%', darker(0.8))]
    for label, f in P23[1:]:
        f(R4)[0].save(os.path.join(OUT, f'{label.split()[0].lower()}-r4.png'))
    sheet(R4, 'p23-r4', 'Round P23 — PU1r1 with the rest of the screen darker, after Replenish (R4)', P23)
