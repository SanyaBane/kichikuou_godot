"""Mock-ups of round P40 (owner, 2026-11-05): the landscape phone's Harem screen after Treasure's round P39 (docs/ui/phone.md,
section 53-54) — the grid five cells a row at the right thumb, scrolled under the finger with the reference scroll bar, its
ticks and the pills at filled cells out of view, the picture grown to the column's new width:

  H1  Summon | Close under the grid, Summon (red) in the place of Treasure's tabs;
  H2  Summon at the column's right with its foot on Close's, as Treasure's touch mode button; Close alone under the grid.

Each in two states from the gallery's harem (the week-18 save, Royal Guards (13) chosen; Kaoru (41), Rebecca (47) and
Saitou (50) on offer far down): the grid at its top and scrolled to its end. Drawn over the gallery's landscape dumps:
the harem's parts cut from its dump, the new grid's places and Close cut from the built Treasure screen's
(`treasure-hover`), each filled cell's insides moved from the folded grid as they are.

Usage: python mock_harem_scroll.py <out dir> <dir of the phone-landscape dumps>
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.dont_write_bytecode = True                 # no __pycache__ in the repository
sys.path.insert(0, str(Path(__file__).resolve().parent))
import mock_treasure_five as five  # noqa: E402

K = five.K
# The built Treasure screen's places (measured on its dump).
CELL_X0, CELL_PITCH_X, CELL_W = 989, 145.25, 141
ROW_Y0, ROW_PITCH, ROW_H = 10, 95.2, 93
COLUMNS, ROWS, SHOWN = 5, 11, 7
GROOVE = (1717, 10, 1731, 674)
GRID_RIGHT = 1710
COLUMN_RIGHT = 963
PICTURE_W = COLUMN_RIGHT - five.COLUMN_LEFT
BUTTONS_TOP, FOOT = 686, 774
TABS_RIGHT = 1507
CLOSE = (1517, 686, 1731, 774)
EMPTY = (1280, 486, 1421, 579)                 # an empty cell of the built grid

FILLED = [1, 11, 12, 13, 41, 47, 50]
CHOSEN = 13

DIM_GOLD = (110, 86, 46)
GOLD = (196, 160, 86)
NAME_GOLD = (232, 199, 122)
HOVER_LINE = (222, 186, 104)
TEXT = (232, 220, 200)
INK = (12, 9, 7)
ROW = (40, 33, 27)
GROOVE_FILL = (14, 11, 9)


def cell_box(n, scroll):
    row, column = divmod(n - 1, COLUMNS)
    row -= scroll
    if row < 0 or row >= SHOWN:
        return None
    return round(CELL_X0 + column * CELL_PITCH_X), round(ROW_Y0 + row * ROW_PITCH)


def harem_cell(base, n):
    """The folded grid's cell n, laid into the new cell's size: its frame stretched, its insides as they are, centred."""
    ox, oy = five.old_cell(n)
    old = base.crop((ox, oy, ox + five.OLD_W, oy + five.OLD_H))
    cell = old.resize((CELL_W, ROW_H), Image.LANCZOS)
    inside = old.crop((3, 3, 3 + CELL_W - 6, five.OLD_H - 3))
    fill = base.getpixel((ox + 6, oy + five.OLD_H - 6))
    d = ImageDraw.Draw(cell)
    d.rectangle((3, 3, CELL_W - 4, ROW_H - 4), fill=fill)
    cell.paste(inside, (3, (ROW_H - inside.height) // 2))
    return cell


def grid(out, base, built, scroll):
    empty = built.crop(EMPTY).resize((CELL_W, ROW_H))
    for n in range(1, 52):
        at = cell_box(n, scroll)
        if at is None:
            continue
        out.paste(harem_cell(base, n) if n in FILLED else empty, at)
    d = ImageDraw.Draw(out)
    bar(d, scroll)
    above = sum(1 for n in FILLED if (n - 1) // COLUMNS < scroll)
    below = sum(1 for n in FILLED if (n - 1) // COLUMNS >= scroll + SHOWN)
    if above:
        pill(out, f'{above} more above', True)
    if below:
        pill(out, f'{below} more below', False)


def bar(d, scroll):
    x0, y0, x1, y1 = GROOVE
    d.rounded_rectangle(GROOVE, radius=(x1 - x0) / 2, fill=GROOVE_FILL, outline=DIM_GOLD, width=1)
    inset = 2
    room = y1 - y0 - 2 * inset
    th = room * SHOWN / ROWS
    ty = y0 + inset + (room - th) * scroll / (ROWS - SHOWN)
    d.rounded_rectangle((x0 + inset, ty, x1 - inset, ty + th), radius=(x1 - x0 - 2 * inset) / 2, fill=GOLD, outline=NAME_GOLD)
    for row in sorted({(n - 1) // COLUMNS for n in FILLED}):
        y = y0 + (row + 0.5) * (y1 - y0) / ROWS
        d.rectangle((x0 - 4 - 1, y - 2.5, x1, y + 2.5), fill=INK)
        d.rectangle((x0 - 4, y - 1.5, x1 - 1, y + 1.5), fill=TEXT)


def pill(out, text, above):
    font = five.cinzel(28 * K)
    d = ImageDraw.Draw(out)
    tw = d.textlength(text.upper(), font=font)
    h, pad, arrow, half = 68 * K, 36 * K, 36 * K, 11 * K
    w = tw + 2 * pad + arrow
    x = (CELL_X0 + GRID_RIGHT - w) / 2
    edge_bottom = ROW_Y0 + SHOWN * ROW_PITCH - (ROW_PITCH - ROW_H)
    y = ROW_Y0 + 8 * K if above else edge_bottom - 8 * K - h
    shadow = Image.new('RGBA', out.size, (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle((x + 3, y + 5, x + w + 3, y + h + 5), radius=h / 2, fill=(0, 0, 0, 215))
    out.paste(shadow, (0, 0), shadow)
    d.rounded_rectangle((x, y, x + w, y + h), radius=h / 2, fill=ROW, outline=HOVER_LINE, width=3)
    cy, ax = y + h / 2, x + pad + half
    pts = [(ax - half, cy + half * 0.6), (ax + half, cy + half * 0.6), (ax, cy - half * 0.7)] if above else \
          [(ax - half, cy - half * 0.6), (ax + half, cy - half * 0.6), (ax, cy + half * 0.7)]
    d.polygon(pts, fill=NAME_GOLD)
    d.text((x + pad + arrow, cy), text.upper(), font=font, fill=TEXT, anchor='lm')


def column(out, base):
    put, cut = five.put, five.cut
    put(out, cut(base, five.TITLE), five.TITLE[:2])
    date = cut(base, five.DATE)
    put(out, date, (COLUMN_RIGHT - date.width, five.DATE[1]))
    put(out, five.divider(base, PICTURE_W + 1), (five.COLUMN_LEFT, five.DIVIDER[1]))
    pic = base.crop((77, 85, 813, 545)).resize((PICTURE_W, round(PICTURE_W * five.PANEL_SHAPE)), Image.LANCZOS)
    out.paste(pic, (five.COLUMN_LEFT, 85))
    details = cut(base, (84, 552, 520, 670))
    put(out, details, (84, 85 + pic.height + 8))


SUMMON = (76, 679, 591, 775)


def variant(base, built, name, scroll):
    back = five.backdrop(base)
    five.BACK[id(base)] = back
    out = back.copy()
    column(out, base)
    summon = base.crop(SUMMON)
    if name == 'H1':
        out.paste(summon.resize((TABS_RIGHT - CELL_X0, FOOT - BUTTONS_TOP), Image.LANCZOS), (CELL_X0, BUTTONS_TOP))
    else:
        left = 500
        h = round(150 * K)
        out.paste(summon.resize((COLUMN_RIGHT - left, h), Image.LANCZOS), (left, FOOT - h))
    out.paste(built.crop(CLOSE), CLOSE[:2])
    grid(out, base, built, scroll)
    return out


def main():
    out_dir = Path(sys.argv[1])
    src = Path(sys.argv[2])
    out_dir.mkdir(parents=True, exist_ok=True)
    base = Image.open(src / 'harem.png').convert('RGB')
    built = Image.open(src / 'treasure-hover.png').convert('RGB')
    shots = []
    for name, title in (('H1', 'H1  Summon | Close under the grid'),
                        ('H2', 'H2  Summon at the column\'s foot, Close under the grid')):
        for scroll, state in ((0, 'at the top'), (ROWS - SHOWN, 'scrolled to the end')):
            img = variant(base, built, name, scroll)
            img.save(out_dir / f'{name}-{"top" if scroll == 0 else "end"}.png')
            shots.append((f'{title}, {state}', img))
    w, h = base.size
    head, pad = 44, 16
    sheet = Image.new('RGB', (2 * w + 3 * pad, 3 * (h + head) + pad), five.SHEET_BACK)
    d = ImageDraw.Draw(sheet)
    label = five.cinzel(26)
    shots.insert(0, ('Now (folded after row 9)', base))
    shots.insert(1, ('Treasure as built (round P39)', built))
    for i, (caption, img) in enumerate(shots):
        x = pad + (i % 2) * (w + pad)
        y = pad + (i // 2) * (h + head)
        d.text((x, y + head // 2), caption, font=label, fill=five.LABEL, anchor='lm')
        sheet.paste(img, (x, y + head))
    sheet.save(out_dir / 'sheet.png')


if __name__ == '__main__':
    main()
