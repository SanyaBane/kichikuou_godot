"""Mock-ups of the upright phone's deployment ledger, Overview tab, with Rows under the troop type (owner, 2026-11-04:
the Rows column's content moves into the fixed Name · Type cell, under the type at the type's size; the rows grow a
little taller, the scrolled part gains Rows' width). Drawn over the screen gallery's upright dump of `deployment`
(half the reference phone's pixels): each row cut at the type's foot and a line inserted there, the Rows column left
out, Troops moved left, Refill and Cost drawn after it with the figures of the week-18 save.

  A   the rows in the type's quiet ink
  B   the rows in the ledger's ink, the type stays quiet

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
RULE = (192, 170, 132)
VERMILION = (160, 52, 36)
TROOPS_BLUE = (52, 88, 168)
SHEET = (24, 20, 18)
NAME = (232, 199, 122)
TEXT = (244, 232, 206)

# The dump's ledger (pixels of the upright dump): the paper's left and right, the fixed part's right, the header's
# foot, the rows' pitch and the first row's top, the type's foot within a row, the view's foot (the bar's top).
PAPER = (9, 592)
FIXED = 260
TEXT_X = 137                   # the name's and the type's column within the fixed part (the face left of it)
TOP = 570
PITCH = 75
CUT = 57
FOOT = 1232
SHADOW = (578, 592)
LINE = 16                      # the inserted line: the type's size
TROOPS = (395, 548)            # the Troops column in the dump (the Rows column 260-395 left out)

# The rows in view (week-18 save): Rows, Refill (K, 0 = none), Cost (K).
ROWS = [('Front', 0, 300), ('Front', 0, 0), ('Front', 0, 0), ('Front', 0, 1200), ('Front', 100, 1100),
        ('Front', 300, 1000), ('Front', 0, 1100), ('Front', 1100, 900), ('Front', 1200, 800)]


def gothic(px):
    return ImageFont.truetype(str(FONTS / 'BIZUDGothic-Regular.ttf'), px)


def cinzel(px, weight=700):
    f = ImageFont.truetype(str(FONTS / 'Cinzel-VariableFont_wght.ttf'), px)
    f.set_variation_by_axes([weight])
    return f


def k(n):
    return f'{n:,}K'


def ledger(src, rows_ink):
    im = src.copy()
    d = ImageDraw.Draw(im)
    # The header: Rows left out, Troops moved left, Refill and Cost after it (the ▸ at the right kept).
    d.rectangle((FIXED, 545, SHADOW[0] - 1, 568), fill=HEAD_PAPER)
    head = cinzel(13)
    d.text((FIXED + 3 + 402 - TROOPS[0], 565), 'Troops', font=head, fill=HEAD_INK, anchor='ls')
    refill_right, cost_right = 478, 572
    for word, right in [('Refill', refill_right), ('Cost', cost_right)]:
        d.text((right - d.textlength(word, font=head), 565), word, font=head, fill=HEAD_INK, anchor='ls')
    # The rows, each taller by a line.
    d.rectangle((PAPER[0], TOP, PAPER[1] - 1, FOOT), fill=(224, 208, 172))
    y = TOP
    cells = gothic(23)
    small = gothic(16)
    for i, (rows, refill, cost) in enumerate(ROWS):
        if y >= FOOT:
            break
        y0 = TOP + PITCH * i
        fixed_bg = src.getpixel((FIXED - 5, y0 + 3))
        scrolled_bg = src.getpixel((300, y0 + 3))
        h = PITCH + LINE
        d.rectangle((PAPER[0], y, PAPER[1] - 1, y + h - 1), fill=scrolled_bg)
        d.rectangle((PAPER[0], y, FIXED - 1, y + h - 1), fill=fixed_bg)
        # № and the face whole, a half line lower; the name and the type cut at the type's foot, the line inserted there.
        im.paste(src.crop((PAPER[0], y0, TEXT_X, y0 + PITCH - 1)), (PAPER[0], y + LINE // 2))
        im.paste(src.crop((TEXT_X, y0, FIXED, y0 + CUT)), (TEXT_X, y))
        im.paste(src.crop((TEXT_X, y0 + CUT, FIXED, y0 + PITCH)), (TEXT_X, y + CUT + LINE))
        d.text((141, y + CUT + LINE - 4), rows, font=small, fill=rows_ink, anchor='ls')
        # Troops, centred in the taller row.
        im.paste(src.crop((TROOPS[0], y0, TROOPS[1], y0 + PITCH - 1)), (FIXED + 3, y + LINE // 2))
        mid = y + h // 2 + 8
        if refill:
            text = k(refill)
            w = d.textlength(text, font=cells)
            box = (refill_right - w - 10, y + h // 2 - 15, refill_right + 4, y + h // 2 + 15)
            d.rounded_rectangle(box, radius=5, fill=(232, 220, 192), outline=(120, 140, 172), width=2)
            d.text((refill_right - 3 - w, mid), text, font=cells, fill=TROOPS_BLUE, anchor='ls')
        else:
            d.text((refill_right - d.textlength('–', font=cells), mid), '–', font=cells, fill=INK, anchor='ls')
        text = k(cost)
        d.text((cost_right - d.textlength(text, font=cells), mid), text, font=cells, fill=INK, anchor='ls')
        for x in range(SHADOW[0], SHADOW[1]):                     # the shadow of the columns beyond the view's right
            t = (x - SHADOW[0]) / (SHADOW[1] - SHADOW[0]) * 0.22
            d.line((x, y, x, y + h - 2), fill=tuple(round(c * (1 - t)) for c in scrolled_bg))
        d.line((PAPER[0], y + h - 1, SHADOW[0] - 1, y + h - 1), fill=RULE)
        y += h
    # The view's foot as it was: the horizontal scroll bar and the bar below.
    im.paste(src.crop((0, FOOT, src.width, src.height)), (0, FOOT))
    return im


def main():
    src_dir, out = Path(sys.argv[1]), Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    src = Image.open(src_dir / 'deployment.png').convert('RGB')
    shots = [('Now', src), ('A  rows in the type\'s ink', ledger(src, INK_QUIET)), ('B  rows in the ledger\'s ink', ledger(src, INK))]
    for name, im in shots[1:]:
        im.save(out / f'deployment-rows-{name[0]}.png')
    gap, head = 24, 50
    sheet = Image.new('RGB', (len(shots) * (src.width + gap) + gap, src.height + head + gap), SHEET)
    d = ImageDraw.Draw(sheet)
    for i, (name, im) in enumerate(shots):
        x = gap + i * (src.width + gap)
        d.text((x, 14), name, font=cinzel(22), fill=NAME if i == 0 else TEXT)
        sheet.paste(im, (x, head))
    sheet.save(out / 'sheet-deployment-rows.png')


if __name__ == '__main__':
    main()
