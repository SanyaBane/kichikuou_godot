"""Mock-ups of the landscape phone's Treasure screen with its grid in five columns (owner, 2026-11-05, round P38): the
pickers' 3 x 17 grid no longer folded into a right half but laid out five cells a row (11 rows, the last with one cell),
and the room it frees at the left given to the picture panel, grown to the column's new width; where the picture then
lacks the height the buttons move:

  A  the buttons in the title band (the date under the title), the details and the action under the picture;
  B  the buttons under the grid at the right thumb, the grid's rows shorter;
  C  the buttons at the left thumb as now, the touch mode's action joining their row, the details in one row over it,
     the picture a little short of the column's width.

Drawn over the gallery's landscape dumps (the window at two thirds of the reference phone's pixels): the column's parts
cut from the dump and placed anew, the cells drawn again at their new size (BIZ UDGothic, the dump's own colours).

Usage: python mock_treasure_five.py <out dir> <dir of the phone-landscape dumps>
Writes <variant>-<dump>.png and sheet-<dump>.png (now | A | B | C).
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONTS = ROOT / 'godot' / 'fonts'
K = 2 / 3                                # dump pixels per reference pixel

# ---------------------------------------------------------------- the dump as it is (measured)
OLD_X = [839, 985, 1131, 1298, 1444, 1590]
OLD_Y0, OLD_PITCH, OLD_W, OLD_H = 10, 85.25, 142, 83
COLUMN_LEFT, OLD_COLUMN_RIGHT = 77, 813
GRID_RIGHT, GRID_TOP, FOOT = 1731, 10, 774
TITLE = (77, 12, 315, 60)
DATE = (590, 22, 815, 50)
DIVIDER = (77, 66, 814, 80)
PICTURE = (77, 85, 814, 546)
DETAILS = (84, 552, 512, 668)            # the name and the figures
ACTION = (518, 557, 815, 661)            # the touch mode's red button
TABS = [(76, 685, 331, 775), (336, 685, 591, 775), (599, 685, 814, 775)]  # Characters, Items, End

GAP = 4                                  # between two cells, as the dump's
NEW_COLUMNS, CELLS = 5, 51
CELL_W = 144
GRID_LEFT = GRID_RIGHT - (NEW_COLUMNS * CELL_W + (NEW_COLUMNS - 1) * GAP)
GRID_GAP = round(40 * K)
COLUMN_RIGHT = GRID_LEFT - GRID_GAP
COLUMN_W = COLUMN_RIGHT - COLUMN_LEFT
PANEL_SHAPE = 416 / 666

# The cells' contents per dump: number -> (name, holder or '', diamond).
CONTENTS = {
    'treasure-hover': {1: ('Rance', '', True), 5: ('Leila', '', False), 6: ('Barres', '', False), 7: ('Rick', '', True),
                       8: ('Cordoba', '', False), 9: ('Kinkaid', '', False), 10: ('Menad', '', False),
                       11: ('Asuka', '', False), 12: ('Haurein', '', False)},
    'treasure-items': {6: ('L. Holy Sword', 'Rance', False), 7: ('By Lord', 'Rick', False)},
}

SHEET_BACK = (24, 20, 18)
LABEL = (232, 199, 122)


def gothic(px):
    return ImageFont.truetype(str(FONTS / 'BIZUDGothic-Regular.ttf'), round(px))


def cinzel(px):
    f = ImageFont.truetype(str(FONTS / 'Cinzel-VariableFont_wght.ttf'), round(px))
    f.set_variation_by_axes([700])
    return f


def old_cell(n):
    row, column = divmod(n - 1, 3)
    right = row >= 9
    return OLD_X[column + (3 if right else 0)], round(OLD_Y0 + (row - 9 if right else row) * OLD_PITCH)


def backdrop(img):
    """The screen painted with the backdrop: each row interpolated between the screen's two sides."""
    out = img.copy()
    px = out.load()
    w, h = img.size
    for y in range(h):
        a, b = img.getpixel((30, y)), img.getpixel((w - 40, y))
        for x in range(40, w - 50):
            t = (x - 30) / (w - 70)
            px[x, y] = tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))
    return out


def brightest(img, box):
    best, colour = -1, (0, 0, 0)
    for y in range(box[1], box[3]):
        for x in range(box[0], box[2]):
            p = img.getpixel((x, y))
            if sum(p) > best:
                best, colour = sum(p), p
    return colour


BACK = {}


def cut(img, box):
    """A part of the dump; where it lies on the backdrop, the backdrop left out (its alpha), so it sits on any place."""
    part = img.crop(box).convert('RGBA')
    flat = BACK.get(id(img))
    if flat is None:
        return part
    ref = flat.crop(box)
    a = Image.new('L', part.size)
    pa, pr, pm = part.load(), ref.load(), a.load()
    for y in range(part.height):
        for x in range(part.width):
            d = max(abs(pa[x, y][i] - pr[x, y][i]) for i in range(3))
            pm[x, y] = max(0, min(255, (d - 8) * 24))
    part.putalpha(a)
    return part


def solid(img, box):
    return img.crop(box)


def put(out, part, at):
    out.paste(part, at, part if part.mode == 'RGBA' else None)


def scaled(part, factor):
    return part.resize((round(part.width * factor), round(part.height * factor)), Image.LANCZOS)


def fit(text, size, room):
    while size > 8 and gothic(size).getlength(text) > room:
        size -= 0.5
    return size


def draw_grid(base, out, contents, top, bottom):
    """The 51 cells five a row between top and bottom, each drawn again from its old cell at the new size."""
    d = ImageDraw.Draw(out)
    rows = (CELLS + NEW_COLUMNS - 1) // NEW_COLUMNS
    pitch = (bottom - top + GAP) / rows
    h = round(pitch - GAP)
    for n in range(1, CELLS + 1):
        ox, oy = old_cell(n)
        old = base.crop((ox, oy, ox + OLD_W, oy + OLD_H))
        row, column = divmod(n - 1, NEW_COLUMNS)
        x, y = GRID_LEFT + column * (CELL_W + GAP), round(top + row * pitch)
        put(out, old.resize((CELL_W, h), Image.LANCZOS), (x, y))
        fill = base.getpixel((ox + 6, oy + OLD_H - 8))
        d.rectangle((x + 3, y + 3, x + CELL_W - 4, y + h - 4), fill=fill)
        entry = contents.get(n)
        if not entry:
            continue
        name, holder, diamond = entry
        left, right = x + 14 * K, x + CELL_W - 12 * K
        if holder:
            ink = brightest(base, (ox + 4, oy + 8, ox + OLD_W - 4, oy + 46))
            quiet = brightest(base, (ox + 60, oy + 50, ox + OLD_W - 4, oy + OLD_H - 6))
            hf = gothic(26 * K)
            d.text((x + CELL_W - 14 * K, y + h * 0.74), holder, font=hf, fill=quiet, anchor='rm')
            d.text((left, y + h * 0.36), name, font=gothic(fit(name, 34 * K, right - left)), fill=ink, anchor='lm')
        else:
            ink = brightest(base, (ox + 4, oy + 20, ox + 100, oy + 62))
            if diamond:
                gold = brightest(base, (ox + OLD_W - 30, oy + 30, ox + OLD_W - 4, oy + 52))
                cx, cy, r = right - 4 * 34 * K / 14, y + h / 2, 4 * 34 * K / 14
                d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=gold)
                right -= 12 * 34 * K / 14
            d.text((left, y + h / 2), name, font=gothic(fit(name, 34 * K, right - left)), fill=ink, anchor='lm')


def divider(img, width):
    part = cut(img, DIVIDER)
    return part.resize((width, part.height), Image.LANCZOS)


def variant_a(base, back, contents):
    """The buttons in the title band; the picture at the column's width; the details and the action under it."""
    out = back.copy()
    put(out, cut(base, TITLE), (TITLE[0], TITLE[1] - 6))
    date = scaled(cut(base, DATE), 0.8)
    put(out, date, (COLUMN_LEFT + 4, 52))
    tabs = [scaled(cut(base, b), 0.66) for b in TABS]
    x = COLUMN_RIGHT - sum(t.width for t in tabs) - 2 * 8
    for t in tabs:
        put(out, t, (x, 8))
        x += t.width + 8
    put(out, divider(base, COLUMN_W), (COLUMN_LEFT, 74))
    pic = solid(base, PICTURE).resize((COLUMN_W, round(COLUMN_W * PANEL_SHAPE)), Image.LANCZOS)
    put(out, pic, (COLUMN_LEFT, 90))
    under = 90 + pic.height + 8
    put(out, cut(base, DETAILS), (DETAILS[0], under))
    act = solid(base, ACTION)
    put(out, act, (COLUMN_RIGHT - act.width, under + 4))
    draw_grid(base, out, contents, GRID_TOP, FOOT)
    return out


def variant_b(base, back, contents):
    """The buttons under the grid at the right thumb; the picture at the column's width; the title band as now."""
    out = back.copy()
    put(out, cut(base, TITLE), TITLE[:2])
    put(out, cut(base, DATE), (COLUMN_RIGHT - (DATE[2] - DATE[0]), DATE[1]))
    put(out, divider(base, COLUMN_W), (COLUMN_LEFT, DIVIDER[1]))
    pic = solid(base, PICTURE).resize((COLUMN_W, round(COLUMN_W * PANEL_SHAPE)), Image.LANCZOS)
    put(out, pic, (COLUMN_LEFT, PICTURE[1]))
    under = PICTURE[1] + pic.height + 10
    put(out, cut(base, DETAILS), (DETAILS[0], under))
    act = solid(base, ACTION)
    put(out, act, (COLUMN_RIGHT - act.width, under + 4))
    tabs = [solid(base, b) for b in TABS]
    gap = 10
    room = GRID_RIGHT - GRID_LEFT - 2 * gap
    factor = min(1, room / sum(t.width for t in tabs))
    tabs = [scaled(t, factor) for t in tabs]
    x, y = GRID_LEFT, FOOT - tabs[0].height
    for t in tabs:
        put(out, t, (x, y))
        x += t.width + gap
    draw_grid(base, out, contents, GRID_TOP, y - 12)
    return out


def variant_c(base, back, contents, name_box, figures_box):
    """The buttons at the left thumb, the action in their row; the details in one row; the picture as tall as is left."""
    out = back.copy()
    put(out, cut(base, TITLE), TITLE[:2])
    put(out, cut(base, DATE), (COLUMN_RIGHT - (DATE[2] - DATE[0]), DATE[1]))
    put(out, divider(base, COLUMN_W), (COLUMN_LEFT, DIVIDER[1]))
    parts = [solid(base, ACTION)] + [solid(base, b) for b in TABS]
    gap = 10
    factor = (COLUMN_W - 3 * gap) / sum(p.width for p in parts)
    parts = [scaled(p, factor) for p in parts]
    y = FOOT - parts[0].height
    x = COLUMN_LEFT
    for p in parts:
        put(out, p, (x, FOOT - p.height))
        x += p.width + gap
    name, figures = cut(base, name_box), cut(base, figures_box)
    row_h = max(name.height, figures.height)
    row_top = y - 10 - row_h
    put(out, name, (DETAILS[0], row_top + (row_h - name.height) // 2))
    put(out, figures, (DETAILS[0] + name.width + 30, row_top + (row_h - figures.height) // 2))
    pic_h = row_top - 8 - PICTURE[1]
    pic_w = min(COLUMN_W, round(pic_h / PANEL_SHAPE))
    pic = solid(base, PICTURE).resize((pic_w, round(pic_w * PANEL_SHAPE)), Image.LANCZOS)
    put(out, pic, (COLUMN_LEFT, PICTURE[1]))
    draw_grid(base, out, contents, GRID_TOP, FOOT)
    return out


# The details' two parts per dump for C's one row: the name (with its kind) and the figures.
DETAIL_PARTS = {
    'treasure-hover': ((84, 552, 380, 594), (84, 604, 480, 666)),
    'treasure-items': ((84, 552, 500, 594), (84, 604, 160, 666)),
}


def main():
    out = Path(sys.argv[1])
    src = Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    label = cinzel(26)
    for stem, contents in CONTENTS.items():
        base = Image.open(src / f'{stem}.png').convert('RGB')
        back = backdrop(base)
        BACK[id(base)] = back
        name_box, figures_box = DETAIL_PARTS[stem]
        shots = [('Now', base),
                 ('A  buttons in the title band', variant_a(base, back, contents)),
                 ('B  buttons under the grid', variant_b(base, back, contents)),
                 ('C  buttons at the left thumb, the action in their row', variant_c(base, back, contents, name_box, figures_box))]
        for (title, img) in shots[1:]:
            img.save(out / f'{title[0]}-{stem}.png')
        w, h = base.size
        head, pad = 44, 16
        sheet = Image.new('RGB', (2 * w + 3 * pad, 2 * (h + head) + pad), SHEET_BACK)
        d = ImageDraw.Draw(sheet)
        for i, (title, img) in enumerate(shots):
            x = pad + (i % 2) * (w + pad)
            y = pad + (i // 2) * (h + head)
            d.text((x, y + head // 2), title, font=label, fill=LABEL, anchor='lm')
            sheet.paste(img, (x, y + head))
        sheet.save(out / f'sheet-{stem}.png')


if __name__ == '__main__':
    main()
