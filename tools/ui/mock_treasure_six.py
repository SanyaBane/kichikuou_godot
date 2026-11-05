"""Mock-up of the landscape phone's Treasure grid in six columns (owner, 2026-11-05): the pickers' 3 x 17 grid is no
longer folded after its ninth row into a right half; its rows are laid two by two side by side, six cells a row, nine
rows (the last with three cells), every cell at its number's place read left to right, top to bottom. Drawn over the
gallery's landscape dumps (the window at two thirds of the reference phone's pixels): each cell of the dump moved to
its new place, the rule between the halves gone, the gaps all the cells' own.

Usage: python mock_treasure_six.py <out dir> <phone-landscape treasure.png> [<more dumps of the same screen> ...]
Writes <name>-six.png for each dump and sheet.png (before | after for each).
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONTS = ROOT / 'godot' / 'fonts'

# The folded grid as the dump draws it (measured): the halves' columns, the rows, a cell's size.
OLD_X = [839, 985, 1131, 1298, 1444, 1590]
ROW_Y0, ROW_PITCH, ROWS_SHOWN = 10, 85.25, 9
CELL_W, CELL_H = 142, 83
GRID = (839, 10, 1731, 774)          # the grid's box (left, top, right, bottom)
GAP = 4                              # between two cells, as the dump's
OLD_COLUMNS, NEW_COLUMNS, CELLS, FOLD = 3, 6, 51, 9
SHEET_BACK = (24, 20, 18)
LABEL = (232, 199, 122)


def old_cell(n):
    row, column = divmod(n - 1, OLD_COLUMNS)
    right = row >= FOLD
    x = OLD_X[column + (OLD_COLUMNS if right else 0)]
    y = ROW_Y0 + (row - FOLD if right else row) * ROW_PITCH
    return round(x), round(y)


def new_cell(n):
    row, column = divmod(n - 1, NEW_COLUMNS)
    w = (GRID[2] - GRID[0] - (NEW_COLUMNS - 1) * GAP) / NEW_COLUMNS
    return round(GRID[0] + column * (w + GAP)), round(ROW_Y0 + row * ROW_PITCH), round(w)


def backdrop(img):
    """The grid's room painted over with the backdrop, interpolated across from its columns left and right of the grid."""
    left_x, right_x = GRID[0] - 12, GRID[2] + 20
    out = img.copy()
    px = out.load()
    for y in range(GRID[1] - 4, GRID[3] + 4):
        a, b = img.getpixel((left_x, y)), img.getpixel((right_x, y))
        for x in range(GRID[0] - 4, GRID[2] + 8):
            t = (x - left_x) / (right_x - left_x)
            px[x, y] = tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))
    return out


def six(img):
    out = backdrop(img)
    for n in range(1, CELLS + 1):
        ox, oy = old_cell(n)
        cell = img.crop((ox, oy, ox + CELL_W, oy + CELL_H))
        nx, ny, w = new_cell(n)
        out.paste(cell.resize((w, CELL_H), Image.LANCZOS), (nx, ny))
    return out


def font(px):
    f = ImageFont.truetype(str(FONTS / 'Cinzel-VariableFont_wght.ttf'), px)
    f.set_variation_by_axes([700])
    return f


def main():
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    pairs = []
    for path in map(Path, sys.argv[2:]):
        before = Image.open(path).convert('RGB')
        after = six(before)
        after.save(out / f'{path.stem}-six.png')
        pairs.append((path.stem, before, after))
    w, h = pairs[0][1].size
    head, pad = 44, 16
    sheet = Image.new('RGB', (2 * w + 3 * pad, len(pairs) * (h + head) + pad), SHEET_BACK)
    d = ImageDraw.Draw(sheet)
    label = font(26)
    for i, (stem, before, after) in enumerate(pairs):
        y = pad + i * (h + head)
        d.text((pad, y + head // 2), f'{stem}: now (folded after row 9)', font=label, fill=LABEL, anchor='lm')
        d.text((2 * pad + w, y + head // 2), f'{stem}: six columns', font=label, fill=LABEL, anchor='lm')
        sheet.paste(before, (pad, y + head))
        sheet.paste(after, (2 * pad + w, y + head))
    sheet.save(out / 'sheet.png')


if __name__ == '__main__':
    main()
