"""Mock-ups of round P39 (owner, 2026-11-05): the landscape phone's Treasure screen as round P38's B (five columns, the
buttons under the grid, the picture at the column's width), its rows kept at 7.1 mm as now, so the grid scrolls; at its
right the reference scroll bar (S2, docs/ui/phone.md, section 41) with ticks for the rows that hold a filled cell, and a
sign at the grid's edge while filled cells lie beyond it:

  R1  the sign a gold line along the cut edge with a chevron in its middle;
  R2  the sign a pill with the chevron and the number of filled cells beyond;
  R3  the sign on the bar itself: a chevron at each end of the groove, lit while filled cells lie beyond that way.

Each is drawn in four states: Characters at the grid's top and at its end (the week-18 save), and Items of a later game
(illustrative: more items owned) at the top and halfway.

Usage: python mock_treasure_scroll.py <out dir> <dir of the phone-landscape dumps>
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.dont_write_bytecode = True                 # no __pycache__ in the repository
sys.path.insert(0, str(Path(__file__).resolve().parent))
import mock_treasure_five as five  # noqa: E402

K = five.K
GAP = 4
COLUMNS, CELLS = 5, 51
ROWS = (CELLS + COLUMNS - 1) // COLUMNS
CELL_H, PITCH = 83, 85.25                     # 7.1 mm on the reference phone, as now
BAR_W, BAR_GAP = round(20 * K), round(10 * K)  # the reference bar (S2) and its gap
BAR_RIGHT = five.GRID_RIGHT
GRID_LEFT = five.GRID_LEFT
GRID_RIGHT = BAR_RIGHT - BAR_W - BAR_GAP
CELL_W = (GRID_RIGHT - GRID_LEFT - (COLUMNS - 1) * GAP) / COLUMNS
VIEW_TOP = five.GRID_TOP
CONTENT_H = ROWS * PITCH - GAP

DIM_GOLD = (110, 86, 46)
GOLD = (196, 160, 86)
NAME_GOLD = (232, 199, 122)
GROOVE = (18, 14, 11)
DARK = (44, 30, 18)
PILL = (30, 22, 16)

CHARACTERS = five.CONTENTS['treasure-hover']
LATER_ITEMS = {1: ('Chaos', '', False), 6: ('L. Holy Sword', 'Rance', False), 7: ('By Lord', 'Rick', False),
               10: ('Safety Bonsai', '', False), 11: ('Snow Charm', '', False), 20: ('Crystal Sword', 'Menad', False),
               29: ('Happiness Ring', '', False), 36: ('Bird Corpse', '', False), 41: ('Hawaiian Sword', '', False),
               48: ('Doping Drug', '', False), 50: ('E. Nurse', '', False)}


def styles(base, items):
    """The cells' looks cut from the dump: (crop, fill, ink, quiet) for empty, plain, and the hovered or chosen one."""
    def look(n, ink_box=None, quiet_box=None):
        ox, oy = five.old_cell(n)
        crop = base.crop((ox, oy, ox + five.OLD_W, oy + five.OLD_H))
        fill = base.getpixel((ox + 6, oy + five.OLD_H - 8))
        ink = five.brightest(base, (ox + ink_box[0], oy + ink_box[1], ox + ink_box[2], oy + ink_box[3])) if ink_box else None
        quiet = five.brightest(base, (ox + quiet_box[0], oy + quiet_box[1], ox + quiet_box[2], oy + quiet_box[3])) if quiet_box else None
        return crop, fill, ink, quiet
    if items:
        return {'empty': look(2), 'plain': look(7, (4, 8, 138, 46), (60, 50, 138, 77)),
                'chosen': look(6, (4, 8, 138, 46), (60, 50, 138, 77))}
    return {'empty': look(2), 'plain': look(5, (4, 20, 100, 62)), 'chosen': look(1, (4, 20, 100, 62))}


def draw_cell(layer, d, x, y, look, entry, gold):
    crop, fill, ink, quiet = look
    w = round(CELL_W)
    layer.paste(crop.resize((w, CELL_H), Image.LANCZOS), (round(x), round(y)))
    d.rectangle((x + 3, y + 3, x + w - 4, y + CELL_H - 4), fill=fill)
    if not entry:
        return
    name, holder, diamond = entry
    left, right = x + 14 * K, x + w - 12 * K
    if holder or ink and quiet:
        if holder:
            d.text((x + w - 14 * K, y + CELL_H * 0.74), holder, font=five.gothic(26 * K), fill=quiet, anchor='rm')
        d.text((left, y + CELL_H * 0.36), name, font=five.gothic(five.fit(name, 34 * K, right - left)), fill=ink, anchor='lm')
        return
    if diamond:
        r = 4 * 34 * K / 14
        cx, cy = right - r, y + CELL_H / 2
        d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=gold)
        right -= 12 * 34 * K / 14
    d.text((left, y + CELL_H / 2), name, font=five.gothic(five.fit(name, 34 * K, right - left)), fill=ink, anchor='lm')


def row_of(n):
    return (n - 1) // COLUMNS


def grid(out, base, contents, chosen, items, view_bottom, offset, sign):
    view_h = view_bottom - VIEW_TOP
    looks = styles(base, items)
    gold = five.brightest(base, (five.OLD_X[2] - 40, 30, five.OLD_X[2], 52)) if not items else GOLD
    gold = NAME_GOLD if not items else gold
    layer = Image.new('RGB', (GRID_RIGHT - GRID_LEFT, view_h))
    layer.paste(out.crop((GRID_LEFT, VIEW_TOP, GRID_RIGHT, view_bottom)))
    d = ImageDraw.Draw(layer)
    for n in range(1, CELLS + 1):
        row, column = divmod(n - 1, COLUMNS)
        y = row * PITCH - offset
        if y + CELL_H < 0 or y > view_h:
            continue
        entry = contents.get(n)
        look = looks['chosen'] if n == chosen else looks['plain'] if entry else looks['empty']
        draw_cell(layer, d, column * (CELL_W + GAP), y, look, entry, gold)
    out.paste(layer, (GRID_LEFT, VIEW_TOP))

    # Filled cells beyond the view: in rows not wholly in it.
    above = sum(1 for n in contents if row_of(n) * PITCH < offset)
    below = sum(1 for n in contents if row_of(n) * PITCH + CELL_H > offset + view_h)
    od = ImageDraw.Draw(out)
    scroll_bar(od, contents, view_bottom, offset, sign, above, below)
    if sign == 'R3':
        return
    if above:
        edge_sign(od, VIEW_TOP, True, above, sign)
    if below:
        edge_sign(od, view_bottom, False, below, sign)


def scroll_bar(d, contents, view_bottom, offset, sign, above, below):
    """The reference bar (S2): a groove with a dim gold rim, the gold thumb; a tick across it for each row with a filled cell.
    R3 keeps a cap at each end of the groove for a chevron, lit while filled cells lie beyond that way."""
    view_h = view_bottom - VIEW_TOP
    x0, x1 = BAR_RIGHT - BAR_W, BAR_RIGHT
    cap = 26 if sign == 'R3' else 0
    top, bottom = VIEW_TOP + cap, view_bottom - cap
    if cap:
        cx = (x0 + x1) / 2
        for up, lit, cy in ((True, above, VIEW_TOP + cap / 2 - 2), (False, below, view_bottom - cap / 2 + 2)):
            if lit:
                chevron(d, cx, cy, up, 9, NAME_GOLD)
            else:
                s = 9
                pts = [(cx - s, cy + s / 2), (cx, cy - s / 2), (cx + s, cy + s / 2)] if up else                       [(cx - s, cy - s / 2), (cx, cy + s / 2), (cx + s, cy - s / 2)]
                d.line(pts, fill=DIM_GOLD, width=1)
    d.rounded_rectangle((x0, top, x1, bottom), radius=BAR_W / 2, fill=GROOVE, outline=DIM_GOLD, width=1)
    inset = 3 * K
    room = bottom - top - 2 * inset
    shown = view_h / CONTENT_H
    th = max(60 * K, room * shown)
    at = offset / (CONTENT_H - view_h)
    ty = top + inset + (room - th) * at
    d.rounded_rectangle((x0 + inset, ty, x1 - inset, ty + th), radius=(BAR_W - 2 * inset) / 2, fill=GOLD,
                        outline=NAME_GOLD, width=1)
    for row in sorted({row_of(n) for n in contents}):
        y = top + inset + room * (row * PITCH + CELL_H / 2) / CONTENT_H
        on_thumb = ty <= y <= ty + th
        d.rectangle((x0 + 2, y - 1, x1 - 2, y + 1), fill=DARK if on_thumb else NAME_GOLD)


def chevron(d, cx, cy, up, size, fill):
    s = size
    pts = [(cx - s, cy + s / 2), (cx, cy - s / 2), (cx + s, cy + s / 2)] if up else \
          [(cx - s, cy - s / 2), (cx, cy + s / 2), (cx + s, cy - s / 2)]
    d.polygon(pts, fill=fill)


def edge_sign(d, edge, up, count, sign):
    cx = (GRID_LEFT + GRID_RIGHT) / 2
    if sign == 'R1':
        y = edge + (1 if up else -2)
        d.rectangle((GRID_LEFT, y, GRID_RIGHT, y + 1), fill=NAME_GOLD)
        cy = edge + (12 if up else -12)
        d.ellipse((cx - 17, cy - 17, cx + 17, cy + 17), fill=PILL, outline=GOLD, width=2)
        chevron(d, cx, cy, up, 8, NAME_GOLD)
        return
    w, h = 96, 36
    top = edge + 6 if up else edge - 6 - h
    d.rounded_rectangle((cx - w / 2, top, cx + w / 2, top + h), radius=h / 2, fill=PILL, outline=GOLD, width=2)
    chevron(d, cx - 18, top + h / 2, up, 8, NAME_GOLD)
    d.text((cx + 6, top + h / 2), str(count), font=five.cinzel(24), fill=NAME_GOLD, anchor='lm')


def screen(base, contents, chosen, items, offset_rows, sign):
    back = five.backdrop(base)
    five.BACK[id(base)] = back
    out = back.copy()
    put, cut, solid = five.put, five.cut, five.solid
    put(out, cut(base, five.TITLE), five.TITLE[:2])
    put(out, cut(base, five.DATE), (five.COLUMN_RIGHT - (five.DATE[2] - five.DATE[0]), five.DATE[1]))
    put(out, five.divider(base, five.COLUMN_W), (five.COLUMN_LEFT, five.DIVIDER[1]))
    pic = solid(base, five.PICTURE).resize((five.COLUMN_W, round(five.COLUMN_W * five.PANEL_SHAPE)), Image.LANCZOS)
    put(out, pic, (five.COLUMN_LEFT, five.PICTURE[1]))
    under = five.PICTURE[1] + pic.height + 10
    put(out, cut(base, five.DETAILS), (five.DETAILS[0], under))
    tabs = [solid(base, b) for b in five.TABS]
    gap = 10
    factor = min(1, (BAR_RIGHT - GRID_LEFT - 2 * gap) / sum(t.width for t in tabs))
    tabs = [five.scaled(t, factor) for t in tabs]
    x, y = GRID_LEFT, five.FOOT - tabs[0].height
    act = solid(base, five.ACTION)
    put(out, act, (five.COLUMN_RIGHT - act.width, y + tabs[0].height - act.height))   # its foot at the buttons' (owner, P39)
    for t in tabs:
        put(out, t, (x, y))
        x += t.width + gap
    view_bottom = y - 12
    view_h = view_bottom - VIEW_TOP
    offset = min(offset_rows * PITCH, CONTENT_H - view_h) if offset_rows is not None else CONTENT_H - view_h
    grid(out, base, contents, chosen, items, view_bottom, offset, sign)
    return out


STATES = [
    ('characters-top', 'treasure-hover', CHARACTERS, 1, False, 0, 'Characters, the grid at its top'),
    ('characters-end', 'treasure-hover', CHARACTERS, 1, False, None, 'Characters, scrolled to the end'),
    ('items-top', 'treasure-items', LATER_ITEMS, 6, True, 0, 'Items of a later game, at the top'),
    ('items-middle', 'treasure-items', LATER_ITEMS, 6, True, 2, 'Items of a later game, scrolled two rows'),
]


def main():
    out = Path(sys.argv[1])
    src = Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    label = five.cinzel(26)
    for sign, title in (('R1', 'R1  a gold line and a chevron at the cut edge'),
                        ('R2', 'R2  a pill with the chevron and the number beyond'),
                        ('R3', 'R3  chevrons at the ends of the bar, lit while filled cells lie beyond')):
        shots = []
        for stem, dump, contents, chosen, items, rows, caption in STATES:
            base = Image.open(src / f'{dump}.png').convert('RGB')
            img = screen(base, contents, chosen, items, rows, sign)
            img.save(out / f'{sign}-{stem}.png')
            shots.append((caption, img))
        w, h = shots[0][1].size
        head, pad = 44, 16
        sheet = Image.new('RGB', (2 * w + 3 * pad, 2 * (h + head) + pad + head), five.SHEET_BACK)
        d = ImageDraw.Draw(sheet)
        d.text((pad, pad + head // 2), title, font=five.cinzel(32), fill=five.LABEL, anchor='lm')
        for i, (caption, img) in enumerate(shots):
            x = pad + (i % 2) * (w + pad)
            y = pad + head + (i // 2) * (h + head)
            d.text((x, y + head // 2), caption, font=label, fill=five.LABEL, anchor='lm')
            sheet.paste(img, (x, y + head))
        sheet.save(out / f'sheet-{sign}.png')


if __name__ == '__main__':
    main()
