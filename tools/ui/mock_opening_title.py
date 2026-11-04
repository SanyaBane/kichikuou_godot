"""Mock-ups of the battle opening's title on two lines (docs/ui/battle-screen.md, "The opening"): the place over the
battle's kind, told apart by size, colour or face (owner, 2026-11-04: the two lines of the same size and colour run
together). Each variant is the PC's plate (480 x the height it takes) drawn at 2x for the Great Wall of Helman A (49)
and for Oaks (6), cut from the gallery's opening dump.

Usage: python mock_opening_title.py <opening.png> <out dir>
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONTS = ROOT / 'godot' / 'fonts'
S = 2

ROW = (34, 26, 20)
BAR = tuple(int(c * 0.8) for c in ROW)
HOVER_LINE = (222, 186, 104)
TEXT = (244, 232, 206)
QUIET = (176, 160, 130)
GOLD = (196, 160, 86)
NAME = (232, 199, 122)
DIM_GOLD = (110, 86, 46)
SHEET_BACK = (24, 20, 18)


def cinzel(size, weight=700):
    f = ImageFont.truetype(str(FONTS / 'Cinzel-VariableFont_wght.ttf'), int(size * S))
    f.set_variation_by_axes([weight])
    return f


def oswald(size, weight=500):
    f = ImageFont.truetype(str(FONTS / 'Oswald-VariableFont_wght.ttf'), int(size * S))
    f.set_variation_by_axes([weight])
    return f


def centred(d, cx, cy, text, font, fill, spacing=0):
    if spacing:
        widths = [d.textlength(ch, font=font) for ch in text]
        total = sum(widths) + spacing * S * (len(text) - 1)
        x = cx - total / 2
        for ch, w in zip(text, widths):
            d.text((x, cy), ch, font=font, fill=fill, anchor='lm')
            x += w + spacing * S
        return total
    d.text((cx, cy), text, font=font, fill=fill, anchor='mm')
    return d.textlength(text, font=font)


# Each variant: (id, title, the kind's font, colour, letter spacing, its middle under the place, the bar's height,
# rules beside the kind).
VARIANTS = [
    ('A', 'The kind smaller, in gold', lambda: cinzel(20), GOLD, 1, 34, 86, False),
    ('B', 'The kind smaller and quiet, between gold rules', lambda: cinzel(19), QUIET, 1, 34, 86, True),
    ('C', 'The place in gold, the kind smaller in the text\'s colour', lambda: cinzel(20), TEXT, 1, 34, 86, False),
    ('D', 'The kind in Oswald, spaced capitals, gold', lambda: oswald(18), GOLD, 3, 32, 84, False),
]


def plate(background, place, kind, variant):
    vid, _, kind_font, kind_colour, spacing, pitch, bar_h, rules = variant
    # As the game lays it out: the bar from 8, the limit 26 under its foot, the turns 31 under that, the plate 19 under them.
    w = 480
    limit_at = 8 + bar_h + 26
    h = limit_at + 31 + 19
    img = background.resize((background.width * S, background.height * S), Image.LANCZOS).crop(
        (380 * S, 0, 900 * S, (h + 40) * S))
    d = ImageDraw.Draw(img, 'RGBA')
    x0, y0 = 20 * S, 16 * S
    d.rounded_rectangle((x0, y0, x0 + w * S, y0 + h * S), radius=10 * S, fill=ROW + (252,), outline=HOVER_LINE, width=3 * S)
    d.rounded_rectangle((x0 + 8 * S, y0 + 8 * S, x0 + (w - 8) * S, y0 + (8 + bar_h) * S), radius=6 * S, fill=BAR)
    cx = x0 + w * S / 2
    place_colour = NAME if vid == 'C' else TEXT
    centred(d, cx, y0 + 33 * S, place.upper() if False else place, cinzel(30), place_colour)
    ky = y0 + (33 + pitch) * S
    width = centred(d, cx, ky, kind, kind_font(), kind_colour, spacing)
    if rules:
        gap, length = 14 * S, 40 * S
        for side in (-1, 1):
            a = cx + side * (width / 2 + gap)
            d.line((a, ky, a + side * length, ky), fill=GOLD, width=2 * S)
            d.polygon([(a, ky - 3 * S), (a + side * 3 * S, ky), (a, ky + 3 * S), (a - side * 3 * S, ky)], fill=GOLD)
    limit_y = y0 + limit_at * S
    centred(d, cx, limit_y, 'City · Battle Limit: up to 232', cinzel(22), TEXT)
    centred(d, cx, limit_y + 31 * S, '4 turns', cinzel(20), QUIET)
    return img


def main():
    background = Image.open(sys.argv[1]).convert('RGB')
    out = Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    rows = []
    for v in VARIANTS:
        pair = [plate(background, p, 'Offensive Battle', v) for p in ('Great Wall of Helman A', 'Oaks')]
        rows.append((v, pair))
    label = cinzel(16)
    cell_w, cell_h = rows[0][1][0].width, rows[0][1][0].height
    title_h = 40 * S
    sheet = Image.new('RGB', (cell_w * 2 + 30 * S, (cell_h + title_h) * len(rows) + 10 * S), SHEET_BACK)
    d = ImageDraw.Draw(sheet)
    y = 10 * S
    for (vid, title, *_), pair in rows:
        d.text((10 * S, y + 12 * S), f'{vid}   {title}', font=label, fill=NAME)
        for i, img in enumerate(pair):
            sheet.paste(img, (10 * S + i * (cell_w + 10 * S), y + title_h))
        y += cell_h + title_h
    sheet.save(out / 'opening-title-round-1.png')
    print(out / 'opening-title-round-1.png')


if __name__ == '__main__':
    main()
