"""Mock-ups of the reference scroll bar on the tablet (2026-10-31; docs/ui/touch.md, "Round T: the reference scroll bar").

The owner chose round P31's S2 as the reference for every scroll bar (tools/ui/mock_scroll_bar.py; docs/ui/phone.md
§40-§41) and round P32's PB for the paper (tools/ui/mock_scroll_bar_paper.py, §42-§43): a rounded groove in a rim, the
thumb inside it with a lit edge — gold on the lacquer, the vermilion ink on the paper. On the phone it is 1.1 mm wide.
The tablet (the touch pass: the 1280x800 layout, 0.203 mm a layout pixel on the owner's Galaxy Tab S7 FE, two of its
pixels to one) still draws the old bars on its three lists that scroll: the save lists (a 5 px groove, the gold thumb),
the message log (a 4 px track, a 6 px thumb in the vermilion ink at 70%) and the ledgers (Godot's bars, 8 px, the thumb
in the quiet ink). These draw the reference's look over them in two widths:

  T1 6 layout px (1.2 mm), the phone's millimetres;
  T2 8 layout px (1.6 mm), the phone's bar as the eye sees it at the tablet's longer reach (~40 cm against ~30) — the
     agent's recommendation; the ledgers' bars keep their thickness.

The rim is one tablet pixel, the thumb two in from the groove (the reference's 0.1 and 0.2 mm). The dumps: the screen
gallery's touch pass (tools/ui/gallery.ps1 -Touch; docs/ui/gallery.md): save-load (twelve manual saves), message-log
(scrolled to its foot) and deployment (the gallery's long roster). Then, from the repository's root (Pillow; godot/fonts):
  python -I tools/ui/mock_scroll_bar_tablet.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository: the dumps hold the player's CGs.

Everything is drawn at the tablet's own pixels (the dump doubled); the thumbs keep the old ones' share and place. Each
old bar is painted over with what lies beside it and the new groove drawn where the build would put it: its right edge
where the old bar's was.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

GALLERY, OUT = sys.argv[1:3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)

K = 2                                   # tablet pixels a layout pixel
LAYOUT_MM = 0.203                       # docs/ui/touch.md
NOTE = (255, 92, 160)                   # the mock-ups' own notes
RIM, INSET = 1, 2                       # tablet pixels

# The palette's colours (godot/Strategy/UiPalette.cs, lacquer).
PAPER, PAPER_HEAD, INK_FAINT, VERMILION_INK = (224, 208, 172), (196, 176, 136), (130, 112, 86), (150, 52, 30)
LACQUER, GROOVE, DIM_GOLD, GOLD, NAME = (21, 15, 12), (40, 32, 24), (110, 86, 46), (196, 160, 86), (232, 199, 122)

# A scheme: the groove's fill, its rim, the thumb, the thumb's lit edge (UiPalette.DrawScrollBar).
SCHEMES = {
    'gold': (GROOVE, DIM_GOLD, GOLD, NAME),
    'paper': (PAPER_HEAD, INK_FAINT, VERMILION_INK, tuple(round(c + (255 - c) * 0.3) for c in VERMILION_INK)),
}


def font(name, size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size)))


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)


def dump(name):
    small = Image.open(os.path.join(GALLERY, 'touch', 'current', name + '.png')).convert('RGB')
    return small.resize((small.width * K, small.height * K), Image.LANCZOS)


def bar(img, groove, share, at, scheme):
    """The reference bar in `groove` (x0, y0, x1, y1; tablet pixels), its thumb `share` of it long at `at` of its way."""
    fill, rim, thumb, lit = SCHEMES[scheme]
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = groove
    vertical = y1 - y0 >= x1 - x0
    w = x1 - x0 if vertical else y1 - y0
    r = w / 2
    d.rounded_rectangle((x0, y0, x1 - 1, y1 - 1), radius=r, fill=rim)
    d.rounded_rectangle((x0 + RIM, y0 + RIM, x1 - 1 - RIM, y1 - 1 - RIM), radius=r - RIM, fill=fill)
    length = (y1 - y0 if vertical else x1 - x0) - 2 * INSET
    h = max(2 * w, length * share)
    start = INSET + (length - h) * at
    width = max(1, round(w / 10))
    if vertical:
        box = (x0 + INSET, y0 + start, x1 - 1 - INSET, y0 + start + h)
        d.rounded_rectangle(box, radius=r - INSET, fill=thumb)
        lx = x0 + INSET + w / 6
        d.line((lx, box[1] + r, lx, box[3] - r), fill=lit, width=width)
    else:
        box = (x0 + start, y0 + INSET, x0 + start + h, y1 - 1 - INSET)
        d.rounded_rectangle(box, radius=r - INSET, fill=thumb)
        ly = y0 + INSET + w / 6
        d.line((box[0] + r, ly, box[2] - r, ly), fill=lit, width=width)


def paint(img, box, colour):
    """Paints `box` (layout pixels, x0 y0 x1 y1 exclusive) with `colour`."""
    ImageDraw.Draw(img).rectangle((box[0] * K, box[1] * K, box[2] * K - 1, box[3] * K - 1), fill=colour)


def smear(img, box, source, vertical):
    """Fills `box` (layout pixels) with copies of the column (vertical) or row at `source` beside it."""
    x0, y0, x1, y1 = (v * K for v in box)
    if vertical:
        strip = img.crop((source * K, y0, source * K + 1, y1))
        for x in range(x0, x1):
            img.paste(strip, (x, y0))
    else:
        strip = img.crop((x0, source * K, x1, source * K + 1))
        for y in range(y0, y1):
            img.paste(strip, (x0, y))


def save_list(img, width):
    # the old groove (5 px at 376-380, 162-755) on the lacquer; the new one's right at the list's (382), the tabs' right
    paint(img, (376, 162, 381, 756), LACQUER)
    bar(img, ((382 - width) * K, 162 * K, 382 * K, 756 * K), 10 / 12, 0.0, 'gold')


def message_log(img, width):
    # the old track (4 px at 1062-1065) and thumb (6 px at 1061-1066), the rules' last 20 px, to the paper's edge
    paint(img, (1052, 104, 1072, 754), PAPER)
    bar(img, ((1067 - width) * K, 116 * K, 1067 * K, 746 * K), 0.581, 1.0, 'paper')


def ledger(img, width):
    # Godot's bars: down at 685-692 (113-760), sideways at 761-768 (31-684), the paper's corner between them
    cut = 8 - width
    if cut:
        smear(img, (685, 113, 685 + cut, 761), 684, True)
        smear(img, (31, 761, 685, 761 + cut), 760, False)
        paint(img, (685, 761, 693, 769), PAPER)
    bar(img, ((693 - width) * K, 113 * K, 693 * K, 761 * K), 0.517, 0.0, 'paper')
    bar(img, (31 * K, (769 - width) * K, 685 * K, 769 * K), 0.972, 0.0, 'paper')


SCREENS = {
    'save': ('save-load', save_list,
             'the groove\'s right at the list\'s and the tabs\'; the rows (318 px) keep their width',
             'now: a groove 5 px (1.0 mm), the thumb in flat gold'),
    'log': ('message-log', message_log,
            'the groove\'s right where the old thumb\'s was, 5 in from the paper\'s edge; built, the rules end 6 before it',
            'now: a track 4 px (0.8 mm) in the paper\'s head, the thumb 6 px in the vermilion ink at 70%'),
    'ledger': ('deployment', ledger,
               'Godot\'s bars restyled both ways; T1 gives the rows and columns 2 px back',
               'now: Godot\'s bars 8 px (1.6 mm), flat, the thumb in the quiet ink'),
}

VARIANTS = [('Now', None), ('T1 — 6 px (1.2 mm)', 6), ('T2 — 8 px (1.6 mm, recommended)', 8)]


def variant(key, width):
    name, draw, _, _ = SCREENS[key]
    img = dump(name)
    if width is not None:
        draw(img, width)
    return img


def notes(key, width):
    _, _, built, now = SCREENS[key]
    if width is None:
        return [now]
    look = 'gold on the lacquer' if key == 'save' else 'the vermilion ink on the paper\'s head'
    return [f'{width} layout px = {width * K} tablet px ({width * LAYOUT_MM:.1f} mm): the reference, {look}', built]


def wrapped(d, text, face, width):
    words, lines = text.split(' '), ['']
    for w in words:
        trial = (lines[-1] + ' ' + w).strip()
        if d.textlength(trial, font=face) <= width or not lines[-1]:
            lines[-1] = trial
        else:
            lines.append(w)
    return lines


def sheet(name, title, rows, scale, crops, foot):
    """For each screen in `rows`, the variants side by side (each cropped to its `crops` box and scaled), notes under."""
    drawn = []
    for key in rows:
        row = []
        for label, width in VARIANTS:
            im = variant(key, width).crop(crops[key])
            row.append((label, im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS),
                        notes(key, width)))
        drawn.append(row)
    gap, head = 24, 110
    w = max(im.width for row in drawn for _, im, _ in row)
    heights = [max(im.height for _, im, _ in row) for row in drawn]
    out = Image.new('RGB', (len(VARIANTS) * w + (len(VARIANTS) + 1) * gap, head + sum(h + foot + 50 for h in heights)),
                    (40, 40, 40))
    d = ImageDraw.Draw(out)
    d.text((gap, 18), title, font=GOTHIC(34), fill=(255, 255, 255))
    y = head
    for row, h in zip(drawn, heights):
        for i, (label, im, ns) in enumerate(row):
            x = gap + i * (w + gap)
            d.text((x, y - 14), label, font=GOTHIC(26), fill=(255, 255, 255), anchor='ld')
            out.paste(im, (x, y))
            ty = y + h + 12
            for n in ns:
                for t in wrapped(d, n, GOTHIC(20), w):
                    d.text((x, ty), t, font=GOTHIC(20), fill=NOTE)
                    ty += 26
                ty += 6
        y += h + foot + 50
    out.save(os.path.join(OUT, name + '.png'))
    print(os.path.join(OUT, name + '.png'), out.size)


WHOLE = (0, 0, 1280 * K, 800 * K)
for key, label in [('save', 'Save/Load, twelve manual saves'),
                   ('log', 'the message log, scrolled to its foot'),
                   ('ledger', 'the deployment table\'s ledger, the gallery\'s long roster')]:
    sheet(f't-{key}', f'Round T — the reference scroll bar on the tablet: {label}', [key], 0.5, {key: WHOLE}, foot=120)
sheet('t-detail', 'Round T — at the tablet\'s own pixels', ['save', 'log', 'ledger'], 1.0,
      {'save': (440, 280, 840, 1080), 'log': (1800, 160, 2200, 960), 'ledger': (1000, 760, 1400, 1560)}, foot=190)

# Each screen whole at the tablet's pixels, to be looked at on the tablet itself.
for key in SCREENS:
    for label, width in VARIANTS:
        tag = 'now' if width is None else label.split(' ')[0]
        path = os.path.join(OUT, f'tablet-{key}-{tag}.png')
        variant(key, width).save(path)
        print(path)
