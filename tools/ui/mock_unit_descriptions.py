"""Mock-ups of the units' descriptions set in our font over the status CG's panel (owner, 2026-11-04;
docs/ui/unit-descriptions.md, "Mock-ups"): the CG kept, its panel's text read (Original.Content/Strategy/UnitDescription)
and drawn again.

    python -I tools/ui/mock_unit_descriptions.py <gallery dir> <cg_png dir> <descriptions.txt> <out dir> < /dev/null

<gallery dir> is the screen gallery (docs/ui/gallery.md; tools/ui/gallery.ps1 with -PhonePortrait): deployment.png and
treasure-hover.png of its pc pass, treasure-hover.png of phone-portrait. <cg_png dir> is the extract's cg_png (KICHIKUOU_EXTRACT_DIR):
CG 600, the empty grey panel, and the CGs shown. <descriptions.txt> is the texts as UnitDescriptionTests writes them with
KICHIKUOU_DESCRIPTIONS_OUT set (each CG's number on a line, then its paragraphs a line each).

A: the panel as it is (its grey pattern, CG 600's empty panel), the text white with a dark outline in BIZ UDGothic.
B: our lacquer, a thin gold line inside the panel's own border, the text cream, the special attack's name gold.
C: our paper (the options window's), the text in ink, the special attack's name vermilion.
Each screen in one size, the largest the longest text of all (CG 622) fits in, shown with Rance's short text (CG 601),
General Barres's (CG 606) and that longest one. The images hold the player's CGs: keep <out dir> outside the repository.
"""
import os
import re
import sys

from PIL import Image, ImageDraw, ImageFont

GALLERY, CGS, TEXTS, OUT = sys.argv[1:5]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)

GOTHIC = os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf')
OSWALD = os.path.join(REPO, 'godot', 'fonts', 'Oswald-VariableFont_wght.ttf')


def body(size):
    f = ImageFont.truetype(OSWALD, size)
    f.set_variation_by_axes([400])
    return f

# Our palette (godot/Strategy/UiPalette.cs, sampled as the other mock-ups do).
LACQUER, GOLD, GOLD_DIM = (24, 19, 15), (200, 160, 92), (120, 94, 56)
CREAM, NAME = (236, 226, 204), (232, 199, 122)
PAPER, INK, VERMILION_INK, RULE = (224, 208, 172), (40, 28, 18), (150, 52, 30), (192, 170, 132)
NOTE = (255, 92, 160)
FRAME = (255, 227, 148)                   # the CG's own outer frame colour

# The panel's inside in a CG's pixels: columns 207-301, rows 1-184 of a 304x186 CG, 6-189 of a 304x196 (docs/ui/unit-descriptions.md).
# A 304x196 CG is cut to 304x186 first (its five rows of band above and below), so one box serves both.
PANEL = (207, 1, 302, 185)


def read_texts(path):
    texts, number = {}, None
    for line in open(path, encoding='utf-8').read().splitlines():
        if line.isdigit():
            number = int(line)
            texts[number] = []
        elif number is not None and line:
            texts[number].append(line)
    return texts


def cg(n):
    im = Image.open(os.path.join(CGS, f'CG{n:04d}.png')).convert('RGB')
    return im.crop((0, 5, 304, 191)) if im.height == 196 else im


def frame_of(dump):
    """The CG's place in a dump: the bounding box of its outer frame colour, and its scale."""
    px = dump.load()
    w, h = dump.size
    xs, ys = [], []
    for y in range(0, h, 1):
        for x in range(0, w, 1):
            r, g, b = px[x, y]
            if abs(r - FRAME[0]) + abs(g - FRAME[1]) + abs(b - FRAME[2]) < 30:
                xs.append(x)
                ys.append(y)
    # keep the columns and rows that are long runs (the frame's sides)
    from collections import Counter
    cx, cy = Counter(xs), Counter(ys)
    cols = [x for x, c in cx.items() if c > 60]
    rows = [y for y, c in cy.items() if c > 100]
    x0, x1, y0, y1 = min(cols), max(cols), min(rows), max(rows)
    return x0, y0, (x1 - x0 + 1) / 304, (y1 - y0 + 1) / 186


def place(box, at):
    x0, y0, sx, sy = at
    l, t, r, b = box
    return round(x0 + l * sx), round(y0 + t * sy), round(x0 + r * sx), round(y0 + b * sy)


def paste_cg(dump, at, image):
    x0, y0, sx, sy = at
    dump.paste(image.resize((round(304 * sx), round(186 * sy)), Image.NEAREST), (x0, y0))


def wrap(text, f, width):
    words, lines, line = text.split(' '), [], ''
    for w in words:
        trial = (line + ' ' + w) if line else w
        if f.getlength(trial) <= width or not line:
            line = trial
        else:
            lines.append(line)
            line = w
    if line:
        lines.append(line)
    return lines


def layout(paragraphs, width, height, top_size, bottom_size):
    """The largest size from top_size down whose wrapped text fits: (size, font, lines with paragraph breaks)."""
    size = top_size
    while True:
        f = body(size)
        step, gap = round(size * 1.22), round(size * 0.5)
        rows = []
        for i, p in enumerate(paragraphs):
            if i:
                rows.append(None)
            rows += wrap(p, f, width)
        need = sum(step if r is not None else gap for r in rows)
        if need <= height or size <= bottom_size:
            return size, f, rows, step, gap
        size -= 1


SPECIAL = re.compile(r'"[^"]+"')


def draw_line(d, x, y, text, f, ink, accent, outline=None):
    """A line, the quoted names in the accent colour."""
    pos = 0
    for m in list(SPECIAL.finditer(text)) + [None]:
        end = m.start() if m else len(text)
        for part, colour in ((text[pos:end], ink), (text[end:m.end()] if m else '', accent)):
            if not part:
                continue
            if outline:
                d.text((x, y), part, font=f, fill=colour, stroke_width=1, stroke_fill=outline)
            else:
                d.text((x, y), part, font=f, fill=colour)
            x += f.getlength(part)
        if m:
            pos = m.end()


def screen_size(at):
    """One size for a screen: the largest the longest text of all (CG 622) fits in, so every unit reads alike."""
    l, t, r, b = place(PANEL, at)
    sx = at[2]
    pad = round(4 * sx)
    return layout(texts[622], r - l - 2 * pad - round(2 * sx), b - t - 2 * pad, round(10 * sx), round(5 * sx))[0]


def variant(dump, at, paragraphs, kind):
    im = dump.copy()
    l, t, r, b = place(PANEL, at)
    d = ImageDraw.Draw(im)
    sx = at[2]
    pad = round(4 * sx)
    if kind == 'A':
        empty = cg(600).crop(PANEL).resize((r - l, b - t), Image.NEAREST)
        im.paste(empty, (l, t))
        ink, accent, outline = (255, 255, 255), (255, 255, 255), (0, 0, 0)
    elif kind == 'B':
        d.rectangle([l, t, r - 1, b - 1], fill=LACQUER)
        inset = round(2 * sx)
        d.rectangle([l + inset, t + inset, r - 1 - inset, b - 1 - inset], outline=GOLD_DIM)
        ink, accent, outline = CREAM, NAME, None
    else:
        d.rectangle([l, t, r - 1, b - 1], fill=PAPER)
        inset = round(2 * sx)
        d.rectangle([l + inset, t + inset, r - 1 - inset, b - 1 - inset], outline=RULE)
        ink, accent, outline = INK, VERMILION_INK, None
    width, height = r - l - 2 * pad - round(2 * sx), b - t - 2 * pad
    size, f, rows, step, gap = layout(paragraphs, width, height, screen_size(at), round(5 * sx))
    y = t + pad + round(1 * sx)
    for row in rows:
        if row is None:
            y += gap
            continue
        draw_line(d, l + pad + round(1 * sx), y, row, f, ink, accent, outline)
        y += step
    return im, size


def crop_of(im, at, margin=10):
    x0, y0, sx, sy = at
    return im.crop((max(0, x0 - margin), max(0, y0 - margin),
                    min(im.width, round(x0 + 304 * sx) + margin), min(im.height, round(y0 + 186 * sy) + margin)))


def label(im, text, size=18):
    out = Image.new('RGB', (im.width, im.height + size + 12), (23, 24, 27))
    out.paste(im, (0, size + 12))
    ImageDraw.Draw(out).text((6, 4), text, font=ImageFont.truetype(GOTHIC, size), fill=NOTE)
    return out


def sheet(rows, path):
    """rows: lists of labelled images, laid out in a grid."""
    gap = 12
    widths = [max(row[i].width for row in rows if i < len(row)) for i in range(max(len(r) for r in rows))]
    heights = [max(im.height for im in row) for row in rows]
    out = Image.new('RGB', (sum(widths) + gap * (len(widths) + 1), sum(heights) + gap * (len(rows) + 1)), (40, 40, 46))
    y = gap
    for row, h in zip(rows, heights):
        x = gap
        for i, im in enumerate(row):
            out.paste(im, (x, y))
            x += widths[i] + gap
        y += h + gap
    out.save(path)


texts = read_texts(TEXTS)
cases = [
    ('pc', 'deployment', 601, 'deployment, Rance (CG 601)'),
    ('pc', 'deployment', 606, 'deployment, General Barres (CG 606)'),
    ('pc', 'deployment', 622, 'deployment, the longest text (CG 622)'),
    ('pc', 'treasure-hover', 601, 'Treasure, Rance'),
    ('phone-portrait', 'treasure-hover', 601, 'phone upright, Treasure, Rance'),
]
rows = []
for passname, screen, n, title in cases:
    dump = Image.open(os.path.join(GALLERY, passname, 'current', screen + '.png')).convert('RGB')
    at = frame_of(dump)
    if n != 601:
        paste_cg(dump, at, cg(n))
    row = [label(crop_of(dump, at), f'now: {title}')]
    for kind in 'ABC':
        im, size = variant(dump, at, texts[n], kind)
        im.save(os.path.join(OUT, f'{passname}-{screen}-{n}-{kind}.png'))
        row.append(label(crop_of(im, at), f'{kind}  ({size} px)'))
    dump.save(os.path.join(OUT, f'{passname}-{screen}-{n}-now.png'))
    rows.append(row)
sheet(rows[:3], os.path.join(OUT, 'sheet-deployment.png'))
sheet(rows[3:], os.path.join(OUT, 'sheet-treasure.png'))
