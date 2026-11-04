"""Mock-ups of a hint that the picker grid holds filled cells out of view (owner, 2026-11-04: at the top of the list with a
filled cell further down, the player may take the rest for empty and never scroll). Drawn over the screen gallery's
upright dumps of Harem and Treasure's Items (the window at half the reference phone's pixels), two filled cells assumed
below the rows in view:

  1   a pill at the grid's foot, "▼ 2 more below" (1a; and, scrolled down, "▲ 3 more above" at its top, 1b)
  2   gold ticks on the scroll bar at the rows holding a filled cell
  3   1 and 2 together

Usage: python mock_more_cells.py <phone-upright folder> <out dir>
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONTS = ROOT / 'godot' / 'fonts'
Z = 2                                   # the sheet's enlargement of the dump

ROW = (34, 26, 20)
HOVER_LINE = (222, 186, 104)
NAME = (232, 199, 122)
TEXT = (244, 232, 206)
INK = (20, 14, 10)
SHEET_BACK = (24, 20, 18)

# Each screen: its dump, the grid's top, its rows' pitch and height, the rows in view, the scroll groove's x and its top and
# foot (dump pixels), the rows holding a filled cell — those in view and the two assumed below them — and the count above
# for 1b.
SCREENS = [
    ('harem', 'Harem', dict(top=589, pitch=54, height=51, shown=12, x0=7, x1=587, groove=(592, 602), track=(590, 1233),
                            filled=[0, 3, 4, 13, 15], below=2, above=3, cut=(540, 1356))),
    ('treasure-items', 'Treasure, Items', dict(top=646, pitch=50, height=47, shown=11, x0=7, x1=587, groove=(592, 602),
                                                track=(646, 1193), filled=[1, 2, 12, 14], below=2, above=2,
                                                cut=(590, 1356))),
]
ROWS = 17


def cinzel(px, weight=700):
    f = ImageFont.truetype(str(FONTS / 'Cinzel-VariableFont_wght.ttf'), round(px * Z))
    f.set_variation_by_axes([weight])
    return f


def triangle(d, cx, cy, size, down):
    s = size * Z
    pts = [(cx - s, cy - s * 0.6), (cx + s, cy - s * 0.6), (cx, cy + s * 0.7)] if down else \
          [(cx - s, cy + s * 0.6), (cx + s, cy + s * 0.6), (cx, cy - s * 0.7)]
    d.polygon(pts, fill=NAME)


def pill(d, g, at_top, count):
    """A lacquer pill over the grid's edge row, centred: the arrow and "n more below/above"."""
    word = 'below' if not at_top else 'above'
    text = f'{count} more {word}'
    font = cinzel(14)
    tw = d.textlength(text, font=font)
    h, pad, arrow = 34 * Z, 18 * Z, 9 * 2 * Z
    w = tw + 2 * pad + arrow
    cx = (g['x0'] + g['x1']) / 2 * Z
    if at_top:
        cy = (g['top'] + 4) * Z + h / 2
    else:
        cy = (g['top'] + (g['shown'] - 1) * g['pitch'] + g['height'] - 4) * Z - h / 2
    box = (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)
    d.rounded_rectangle((box[0] + 2 * Z, box[1] + 4 * Z, box[2] + 2 * Z, box[3] + 4 * Z), radius=h / 2, fill=(0, 0, 0))
    d.rounded_rectangle(box, radius=h / 2, fill=ROW, outline=HOVER_LINE, width=2 * Z)
    triangle(d, box[0] + pad + 4 * Z, cy, 5.5, down=not at_top)
    d.text((box[0] + pad + arrow, cy), text, font=font, fill=TEXT, anchor='lm')


def ticks(d, g):
    """A gold tick across the groove (and a little out of it, left) at each row holding a filled cell."""
    gx0, gx1 = g['groove']
    t0, t1 = g['track']
    per = (t1 - t0) / ROWS
    for row in g['filled']:
        y = (t0 + (row + 0.5) * per) * Z
        d.rectangle(((gx0 - 4) * Z, y - 2 * Z, (gx1 + 1) * Z, y + 2 * Z), fill=INK)
        d.rectangle(((gx0 - 3) * Z, y - 1.2 * Z, gx1 * Z, y + 1.2 * Z), fill=TEXT)


def frame(base, g, kind):
    img = base.resize((base.width * Z, base.height * Z), Image.LANCZOS)
    d = ImageDraw.Draw(img)
    if kind in ('1a', '3'):
        pill(d, g, False, g['below'])
    if kind == '1b':
        pill(d, g, True, g['above'])
    if kind in ('2', '3'):
        ticks(d, g)
    top, foot = g['cut']
    return img.crop((0, top * Z, img.width, foot * Z))


LABELS = [('1a', '1  Pill at the foot'), ('1b', '1  Scrolled: pill on top'), ('2', '2  Ticks on the bar'),
          ('3', '3  Pill and ticks')]


def main():
    folder, out = Path(sys.argv[1]), Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    label = cinzel(13)
    for entry, title, g in SCREENS:
        base = Image.open(folder / f'{entry}.png').convert('RGB')
        frames = [(text, frame(base, g, kind)) for kind, text in LABELS]
        w, h = frames[0][1].size
        head, gap = 44 * Z, 16 * Z
        sheet = Image.new('RGB', (len(frames) * (w + gap) + gap, h + head + gap), SHEET_BACK)
        d = ImageDraw.Draw(sheet)
        for i, (text, img) in enumerate(frames):
            x = gap + i * (w + gap)
            d.text((x + w / 2, head / 2), text, font=label, fill=NAME, anchor='mm')
            sheet.paste(img, (x, head))
        path = out / f'more-cells-{entry}.png'
        sheet.save(path)
        print(path)


if __name__ == '__main__':
    main()
