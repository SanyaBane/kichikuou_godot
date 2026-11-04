"""Mock-ups of the result plate after a troop order on the phone held landscape (owner, 2026-11-04; docs/ui/phone.md,
"Round P36").

Held landscape the plate is the PC's 780x150 in the frame (godot/Strategy/OrderResultPlate.cs): its lines ~1.1-2.4 mm,
too small. These redraw it from the upright card (PU1r1, OrderResultPlate.Portrait.cs) at the phone's type, over the
screen gallery's landscape pass (tools/ui/gallery.ps1 -PhoneLand -Only subordinates-result,subordinates-new-troops,
deployment-refill-done). From the repository's root (Pillow and numpy; godot/fonts):
  python -I tools/ui/mock_result_plate_land.py <gallery dir> <extract dir> <out dir> < /dev/null
<extract dir> is KICHIKUOU_EXTRACT_DIR (cg_png: Haurein's face). Keep <out dir> outside the repository.

LU1r1 the upright card as it is (800 reference pixels high, the room's width upright) in the middle of the screen.
LU1r2 the same parts at the same type laid out wide: the title over the face and the unit, the troops and their bar;
right of a rule Cost over Budget. Both over the screen dimmed 65% as now.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import mock_result_plate as m                                           # noqa: E402  (reads the same arguments)

LAND = os.path.join(m.GALLERY, 'phone-landscape', 'current')
LW, LH = 2712, 1220                    # the Poco X6 Pro held landscape, reference pixels (the dumps are 2/3 of it)
CX, CY = LW // 2, LH // 2

# The dumps' plate, in reference pixels (the dump's 509..1299 x 322..471 times 1.5), painted over by the screen under it.
OLD = (748, 470, 1964, 722)

SCREENS = [('subordinates-result', 'subordinates', m.R4, 'Subordinates, Replenish (R4)'),
           ('subordinates-new-troops', 'subordinates', m.R5, 'Subordinates, Add New Troops +100 (R5)'),
           ('deployment-refill-done', 'deployment-refill-question', m.R4, 'Deployment, Refill (decision 27)')]


def load(name):
    return Image.open(os.path.join(LAND, name + '.png')).convert('RGB').resize((LW, LH), Image.LANCZOS)


def screen(dump, plain):
    """The dump without its plate: the band under it from the plain screen dimmed 65% as the dump is, where there is one;
    else the dump as it is (the new plates cover the old one whole)."""
    img = load(dump)
    if plain is None:
        return img
    a = np.asarray(img).astype(np.float32)
    p = np.asarray(load(plain)).astype(np.float32) * 0.35
    x0, y0, x1, y1 = OLD
    a[y0:y1, x0:x1] = p[y0:y1, x0:x1]
    return Image.fromarray(a.astype(np.uint8))


def lu1r1(base, r):
    """The upright card as it is, in the middle of the screen."""
    img = base.copy()
    d = ImageDraw.Draw(img)
    w, h = 1188, 800
    x0, y0 = CX - w // 2, CY - h // 2
    x1, y1 = x0 + w, y0 + h
    m.panel(d, (x0, y0, x1, y1))
    title, size = m.fitted(d, r['title'], m.BRUSH, 72, w - 120)
    d.text((CX, y0 + 92), r['title'], font=title, fill=m.NAME, anchor='mm')
    m.divider(d, x0 + 40, x1 - 40, y0 + 166)
    fs = 230
    face(img, d, (x0 + 48, y0 + 202), fs)
    tx = x0 + 48 + fs + 40
    d.text((tx, y0 + 250), r['name'], font=m.BRUSH(60), fill=m.NAME, anchor='lm')
    d.text((tx + 2, y0 + 316), r['type'].upper(), font=m.CINZEL(34, 600), fill=m.SUBTITLE, anchor='lm')
    m.troops_line(d, tx, y0 + 396, r, 60, gain_size=44, width=x1 - 48 - tx)
    m.troops_bar(d, x0 + 48, x1 - 48, y0 + 476, r)
    d.text((x0 + 48, y0 + 534), 'TROOPS', font=m.CINZEL(28, 600), fill=m.LABEL, anchor='lm')
    d.text((x1 - 48, y0 + 534), f"{m.number(r['after'])} / {m.number(r['max_after'])}", font=m.CINZEL(30, 600),
           fill=m.QUIET, anchor='rm')
    m.divider(d, x0 + 40, x1 - 40, y0 + 590)
    m.two_columns(d, x0 + 40, x1 - 40, y0 + 626, r)
    m.triangle(d, x1 - 46, y1 - 40)
    return img, [f'{w}x{h} px ({m.mm(w)} x {m.mm(h)}), the upright card unchanged, in the screen\'s middle',
                 f'title {size} px · name 60 · face {fs} ({m.mm(fs)}) · troops 60 · cost and budget 56 · labels 28-34']


def lu1r2(base, r):
    """The upright card's parts at its type, laid out wide: Cost over Budget right of a rule."""
    img = base.copy()
    d = ImageDraw.Draw(img)
    w, h = 1720, 560
    x0, y0 = CX - w // 2, CY - h // 2
    x1, y1 = x0 + w, y0 + h
    m.panel(d, (x0, y0, x1, y1))
    rule = x1 - 520                                     # the figures' column
    lx1 = rule - 48                                     # the left part's right edge
    title, size = m.fitted(d, r['title'], m.BRUSH, 72, w - 120)
    d.text((CX, y0 + 86), r['title'], font=title, fill=m.NAME, anchor='mm')
    m.divider(d, x0 + 40, x1 - 40, y0 + 154)
    fs = 230
    face(img, d, (x0 + 48, y0 + 190), fs)
    tx = x0 + 48 + fs + 40
    d.text((tx, y0 + 236), r['name'], font=m.BRUSH(60), fill=m.NAME, anchor='lm')
    d.text((tx + 2, y0 + 300), r['type'].upper(), font=m.CINZEL(34, 600), fill=m.SUBTITLE, anchor='lm')
    m.troops_line(d, tx, y0 + 378, r, 60, gain_size=44, width=lx1 - tx)
    m.troops_bar(d, tx, lx1, y0 + 436, r)
    d.text((tx, y0 + 490), 'TROOPS', font=m.CINZEL(28, 600), fill=m.LABEL, anchor='lm')
    d.text((lx1, y0 + 490), f"{m.number(r['after'])} / {m.number(r['max_after'])}", font=m.CINZEL(30, 600),
           fill=m.QUIET, anchor='rm')
    d.line([(rule, y0 + 196), (rule, y1 - 48)], fill=m.DIM_GOLD, width=2)
    col = (rule + x1) / 2
    for top, word, value, colour in ((y0 + 206, 'COST', r['cost'], m.MINUS), (y0 + 356, 'BUDGET', r['budget'], m.NAME)):
        d.text((col, top + 16), word, font=m.CINZEL(32, 600), fill=m.LABEL, anchor='mm')
        d.text((col, top + 32 + 24 + 28), value, font=m.CINZEL(56), fill=colour, anchor='mm')
    m.triangle(d, x1 - 46, y1 - 40)
    return img, [f'{w}x{h} px ({m.mm(w)} x {m.mm(h)}), across the frame and into the sides',
                 f'the upright card\'s type: title {size} px · name 60 · face {fs} · troops 60 · cost and budget 56']


def face(img, d, xy, size):
    img.paste(m.haurein(size), xy)
    d.rectangle((xy[0], xy[1], xy[0] + size, xy[1] + size), outline=m.GOLD, width=3)


def now(base, r, dump):
    return load(dump), ['as built: the PC\'s 780x150 in the frame, ~1190x225 px (13 mm high)',
                        'its lines 20-43 layout px, 1.1-2.4 mm']


VARIANTS = [('Now', None), ('LU1r1 — the upright card as it is', lu1r1),
            ('LU1r2 — the same type, laid out wide (recommended)', lu1r2)]


def sheet():
    gap, head, cap, foot = 24, 90, 60, 120
    w, h = LW // 2, LH // 2
    rows = []
    for dump, plain, r, label in SCREENS:
        base = screen(dump, plain)
        row = []
        for vlabel, f in VARIANTS:
            img, notes = now(base, r, dump) if f is None else f(base, r)
            if r['made_up'] and f is not None:
                notes = notes + m.made_up(r)
            row.append((vlabel, img, notes))
            if f is not None:
                img.save(os.path.join(m.OUT, f"{vlabel.split()[0].lower()}-{dump}.png"))
        rows.append((label, row))
    W = len(VARIANTS) * w + (len(VARIANTS) + 1) * gap
    H = head + len(rows) * (cap + h + foot)
    out = Image.new('RGB', (W, H), (40, 40, 40))
    d = ImageDraw.Draw(out)
    d.text((gap, 20), 'Round P36 — the result plate held landscape, from the upright card (PU1r1); Haurein (unit 12), '
                      'the week-18 save', font=m.GOTHIC(34), fill=(255, 255, 255))
    y = head
    for label, row in rows:
        for i, (vlabel, img, notes) in enumerate(row):
            x = gap + i * (w + gap)
            d.text((x, y + cap - 12), f'{vlabel} · {label}', font=m.GOTHIC(24), fill=(255, 255, 255), anchor='ld')
            out.paste(img.resize((w, h), Image.LANCZOS), (x, y + cap))
            ny = y + cap + h + 10
            for n in notes:
                d.text((x, ny), n, font=m.GOTHIC(20), fill=m.NOTE)
                ny += 26
        y += cap + h + foot
    path = os.path.join(m.OUT, 'p36.png')
    out.save(path)
    print(path, out.size)


sheet()
