"""Mock-ups of a long harem name on two lines in the landscape phone's picker grid (owner, 2026-11-04: Royal Guards (13)
shrinks to half the other names' size on one line). Drawn over the gallery's landscape dump of the harem (the window at two
thirds of the reference phone's pixels), its Royal Guards cell redrawn, cut with its neighbours and enlarged 3x.

Usage: python mock_harem_name_lines.py <phone-landscape harem.png> <out dir>
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONTS = ROOT / 'godot' / 'fonts'
K = 2 / 3                       # dump pixels per reference pixel
Z = 3                           # the sheet's enlargement

CELL = (840, 352, 980, 432)     # the Royal Guards cell in the dump (its outer frame)
CUT = (830, 255, 1290, 445)     # Soul and Anasel above it for comparison
ROW_HOVER = (52, 40, 30)
HOVER_LINE = (222, 186, 104)
NAME = (232, 199, 122)
PLUM_DIM = (110, 60, 80)
SHEET_BACK = (24, 20, 18)


def cinzel(px):
    f = ImageFont.truetype(str(FONTS / 'Cinzel-VariableFont_wght.ttf'), max(1, round(px * Z)))
    f.set_variation_by_axes([700])
    return f


def heart(d, cx, cy, size):
    """The cell's heart: two lobes and a point, size the reference's 12 at the dump's scale."""
    s = size * Z
    r = s * 0.3
    d.ellipse((cx - s * 0.55, cy - s * 0.45, cx - s * 0.55 + 2 * r, cy - s * 0.45 + 2 * r), fill=PLUM_DIM)
    d.ellipse((cx + s * 0.55 - 2 * r, cy - s * 0.45, cx + s * 0.55, cy - s * 0.45 + 2 * r), fill=PLUM_DIM)
    d.polygon([(cx - s * 0.56, cy - s * 0.12), (cx + s * 0.56, cy - s * 0.12), (cx, cy + s * 0.55)], fill=PLUM_DIM)


# (id, title, the lines' type in reference px, the heart beside: 'block' (the two lines' middle) or 'first', centred block)
VARIANTS = [
    ('A', 'Two lines at the names\' size, the heart at their middle', 36, 'block', False),
    ('B', 'Two lines at the names\' size, the heart beside the first', 36, 'first', False),
    ('C', 'Two lines a little smaller (30), the heart at their middle', 30, 'block', False),
]


def variant(base, size_ref, heart_at):
    img = base.crop(CUT).resize(((CUT[2] - CUT[0]) * Z, (CUT[3] - CUT[1]) * Z), Image.LANCZOS)
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = [(v - o) * Z for v, o in zip(CELL, (CUT[0], CUT[1], CUT[0], CUT[1]))]
    d.rectangle((x0, y0, x1, y1), fill=ROW_HOVER, outline=HOVER_LINE, width=2 * Z)
    font = cinzel(size_ref * K)
    pitch = size_ref * K * 1.05 * Z
    mid = (y0 + y1) / 2
    lines = ['Royal', 'Guards']
    tops = [mid - pitch / 2, mid + pitch / 2]
    name_x = x0 + 44 * K * Z
    for line, cy in zip(lines, tops):
        d.text((name_x, cy), line.upper() if False else line, font=font, fill=NAME, anchor='lm')
    hy = mid if heart_at == 'block' else tops[0]
    heart(d, x0 + 24 * K * Z, hy, 12 * K * 1.7)                         # the game draws it this large (DrawHeart)
    return img


def main():
    base = Image.open(sys.argv[1]).convert('RGB')
    out = Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    cells = [(v, variant(base, v[2], v[3])) for v in VARIANTS]
    label = cinzel(6)
    w, h = cells[0][1].size
    head = 30 * Z // 2
    sheet = Image.new('RGB', (w + 20, (h + head) * len(cells) + 10), SHEET_BACK)
    d = ImageDraw.Draw(sheet)
    y = 10
    for (vid, title, *_), img in cells:
        d.text((10, y + head // 2), f'{vid}   {title}', font=label, fill=NAME, anchor='lm')
        sheet.paste(img, (10, y + head))
        y += h + head
    path = out / 'harem-name-lines-round-1.png'
    sheet.save(path)
    print(path)


if __name__ == '__main__':
    main()
