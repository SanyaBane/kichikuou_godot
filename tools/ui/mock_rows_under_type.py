"""Mock-ups of the upright phone's deployment ledger, Overview tab, with Rows under the troop type (owner, 2026-11-04:
the Rows column's content moves into the fixed Name · Type cell, under the type at the type's size). Drawn over the
screen gallery's upright dump of `deployment` (half the reference phone's pixels).

Round 1:  A  the rows in the type's quiet ink;  B  in the ledger's ink.
Round 2 (the owner took A, with changes):  A2 — the type and the rows from one left edge, the name ~13% smaller, the
portrait from the name's top to the HP bar's foot (wider too), Refill's buttons 30% taller.

Usage: python mock_rows_under_type.py <phone-upright folder> <out dir>
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONTS = ROOT / 'godot' / 'fonts'

INK = (43, 31, 20)
INK_QUIET = (100, 80, 56)
HEAD_INK = (125, 50, 34)
HEAD_PAPER = (196, 176, 136)
PAPER = (224, 208, 172)
RULE = (192, 170, 132)
TROOPS_BLUE = (52, 88, 168)
SHEET = (24, 20, 18)
NAME = (232, 199, 122)
TEXT = (244, 232, 206)

# The dump's ledger (pixels of the upright dump): the paper's left and right, the fixed part's right, the header's
# foot, the rows' pitch and the first row's top, the view's foot (the bar's top), the right edge's shadow.
LEFT, RIGHT = 9, 592
FIXED = 260
TOP = 570
PITCH = 75
FOOT = 1232
SHADOW = (578, 592)
TROOPS = (395, 548)            # the Troops column in the dump (the Rows column 260-395 left out)
TYPE_SIZE = 14.7               # the type's size in the dump: "Leazas Soldiers" 110 px wide (BIZ UDGothic, half-width)
NAME_SIZE = 24.3               # the name's: "Kinkaid" 85 px wide
FACE = (71, 10, 133, 65)       # the face's box within a row (x0, y0, x1, y1)
TEXT_X = 142                   # the name's and the type's left in the dump
BARS = (141, 59, 254, 69)      # both bars within a row

# The rows: their place among the dump's rows (None: drawn from the records), №, name, type, Rows, Refill (K, 0 =
# none), Cost (K); Eleanor (unit 23) with her face CG and the square the screens cut from it (UnitFaces).
ROWS = [
    (0, 1, 'Rance', 'Leazas Knights', 'Front', 0, 300),
    (1, 3, 'Bound', 'Thieves', 'Front', 0, 0),
    (2, 4, 'Soul', 'Thieves', 'Front', 0, 0),
    (3, 5, 'Leila', 'Royal Guards', 'Front', 0, 1200),
    (4, 6, 'Barres', 'Leazas Knights', 'Front', 100, 1100),
    (5, 7, 'Rick', 'Leazas Knights', 'Front', 300, 1000),
    (6, 8, 'Cordoba', 'Leazas Knights', 'Front', 0, 1100),
    (7, 9, 'Kinkaid', 'Leazas Soldiers', 'Front', 1100, 900),
    (8, 10, 'Menad', 'Leazas Soldiers', 'Front', 1200, 800),
]


def gothic(px):
    return ImageFont.truetype(str(FONTS / 'BIZUDGothic-Regular.ttf'), px)


def cinzel(px, weight=700):
    f = ImageFont.truetype(str(FONTS / 'Cinzel-VariableFont_wght.ttf'), px)
    f.set_variation_by_axes([weight])
    return f


def k(n):
    return f'{n:,}K'


class Layout:
    """A round's row: its height, the name's size and baseline, the type's and the rows' baselines, the face's box, the
    bars' top, the text's left, the fixed part's right, Refill's button height and its right, Cost's right."""

    def __init__(self, round2):
        self.round2 = round2
        if round2:
            self.height = 82
            self.name_size, self.name_base = NAME_SIZE * 0.87, 27
            self.type_base, self.rows_base = 44, 60
            self.bars_top = 65
            name_top = self.name_base + gothic(self.name_size).getbbox('R', anchor='ls')[1]
            face_h = self.bars_top + (BARS[3] - BARS[1]) - name_top
            face_w = round(face_h * (FACE[2] - FACE[0]) / (FACE[3] - FACE[1]))
            self.face = (FACE[0], name_top, FACE[0] + face_w, name_top + face_h)
            self.text_x = self.face[2] + (TEXT_X - FACE[2])
            self.chip = 39
        else:
            self.height = PITCH + 16
            self.name_size, self.name_base = None, None
            self.rows_base = 57 + 16 - 4
            self.face = None
            self.text_x = TEXT_X
            self.chip = 30
        self.fixed = self.text_x + (BARS[2] - BARS[0]) + (FIXED - BARS[2])
        self.shift = self.fixed - FIXED
        self.refill_right = 478 + self.shift
        self.cost_right = 574 if round2 else 572


def ledger(src, rows_ink, lay):
    im = src.copy()
    d = ImageDraw.Draw(im)
    head = cinzel(13)
    d.rectangle((FIXED - 2, 545, SHADOW[0] - 1, 568), fill=HEAD_PAPER)
    d.text((lay.fixed + 3 + 402 - TROOPS[0], 565), 'Troops', font=head, fill=HEAD_INK, anchor='ls')
    for word, right in [('Refill', lay.refill_right), ('Cost', lay.cost_right)]:
        d.text((right - d.textlength(word, font=head), 565), word, font=head, fill=HEAD_INK, anchor='ls')
    d.rectangle((LEFT, TOP, RIGHT - 1, FOOT), fill=PAPER)
    cells, small = gothic(23), gothic(TYPE_SIZE)
    name_font = gothic(lay.name_size) if lay.name_size else None
    y = TOP
    for at, number, name, kind, line, refill, cost in ROWS:
        if y >= FOOT:
            break
        h = lay.height
        y0 = TOP + PITCH * at
        fixed_bg = src.getpixel((FIXED - 5, y0 + 3))
        scrolled_bg = src.getpixel((300, y0 + 3))
        d.rectangle((LEFT, y, RIGHT - 1, y + h - 1), fill=scrolled_bg)
        d.rectangle((LEFT, y, lay.fixed - 1, y + h - 1), fill=fixed_bg)
        if not lay.round2:
            im.paste(src.crop((LEFT, y0, 137, y0 + PITCH - 1)), (LEFT, y + 8))
            im.paste(src.crop((137, y0, FIXED, y0 + 57)), (137, y))
            im.paste(src.crop((137, y0 + 57, FIXED, y0 + PITCH)), (137, y + 57 + 16))
            d.text((141, y + lay.rows_base), line, font=small, fill=rows_ink, anchor='ls')
        else:
            # The seal and №, a little lower; the face from the name's top to the HP bar's foot; the name, the type and
            # the rows from one left edge; the bars under them.
            fw, fh = lay.face[2] - lay.face[0], lay.face[3] - lay.face[1]
            im.paste(src.crop((LEFT, y0, FACE[0], y0 + PITCH - 1)), (LEFT, y + 3))
            face = src.crop((FACE[0], y0 + FACE[1], FACE[2], y0 + FACE[3])).resize((fw, fh), Image.LANCZOS)
            bars = src.crop((BARS[0], y0 + BARS[1], BARS[2], y0 + BARS[3]))
            im.paste(face, (lay.face[0], y + lay.face[1]))
            d.text((lay.text_x, y + lay.name_base), name, font=name_font, fill=INK, anchor='ls')
            d.text((lay.text_x, y + lay.type_base), kind, font=small, fill=INK_QUIET, anchor='ls')
            d.text((lay.text_x, y + lay.rows_base), line, font=small, fill=rows_ink, anchor='ls')
            im.paste(bars, (lay.text_x - 1, y + lay.bars_top))
        # Troops, centred in the taller row.
        troops = src.crop((TROOPS[0], y0, TROOPS[1], y0 + PITCH - 1))
        im.paste(troops, (lay.fixed + 3, y + (h - PITCH) // 2))
        mid = y + h // 2 + 8
        if refill:
            text = k(refill)
            w = d.textlength(text, font=cells)
            box = (lay.refill_right - w - 10, y + h // 2 - lay.chip // 2, lay.refill_right + 4, y + h // 2 + lay.chip // 2)
            d.rounded_rectangle(box, radius=5, fill=(232, 220, 192), outline=(120, 140, 172), width=2)
            d.text((lay.refill_right - 3 - w, mid), text, font=cells, fill=TROOPS_BLUE, anchor='ls')
        else:
            d.text((lay.refill_right - d.textlength('–', font=cells), mid), '–', font=cells, fill=INK, anchor='ls')
        text = k(cost)
        d.text((lay.cost_right - d.textlength(text, font=cells), mid), text, font=cells, fill=INK, anchor='ls')
        for x in range(SHADOW[0], SHADOW[1]):                     # the shadow of the columns beyond the view's right
            t = (x - SHADOW[0]) / (SHADOW[1] - SHADOW[0]) * 0.22
            d.line((x, y, x, y + h - 2), fill=tuple(round(c * (1 - t)) for c in scrolled_bg))
        d.line((LEFT, y + h - 1, SHADOW[0] - 1, y + h - 1), fill=RULE)
        y += h
    im.paste(src.crop((0, FOOT, src.width, src.height)), (0, FOOT))
    return im


def sheet(out, name, shots, width):
    gap, head = 24, 50
    s = Image.new('RGB', (len(shots) * (width + gap) + gap, shots[0][1].height + head + gap), SHEET)
    d = ImageDraw.Draw(s)
    for i, (caption, im) in enumerate(shots):
        x = gap + i * (width + gap)
        d.text((x, 14), caption, font=cinzel(22), fill=NAME if i == 0 else TEXT)
        s.paste(im, (x, head))
    s.save(out / name)


def main():
    src_dir, out = Path(sys.argv[1]), Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    src = Image.open(src_dir / 'deployment.png').convert('RGB')
    a = ledger(src, INK_QUIET, Layout(False))
    b = ledger(src, INK, Layout(False))
    a2 = ledger(src, INK_QUIET, Layout(True))
    for key, im in [('A', a), ('B', b), ('A2', a2)]:
        im.save(out / f'deployment-rows-{key}.png')
    sheet(out, 'sheet-deployment-rows.png', [('Now', src), ('A  rows in the type\'s ink', a),
                                             ('B  rows in the ledger\'s ink', b)], src.width)
    sheet(out, 'sheet-deployment-rows-2.png', [('Now', src), ('A', a), ('A2  the owner\'s changes', a2)], src.width)


if __name__ == '__main__':
    main()
