"""Mock-ups of the "K" of K GOLD drawn as a part of the figure (owner, 2026-11-04: in "Budget 116,400 K GOLD" the K is
small and in another face, so it is not seen). Drawn over the screen gallery's dumps of Subordinates (the PC's orders'
header, the phone upright's budget at the bar) and the week report (the PC's treasury), each in three variants:

  A   "116,400K" in the figure's face and size, "GOLD" after it small and quiet, as the unit is now
  B   "116,400K" alone, as the tables, the cards and the buttons write gold
  C   "116,400K GOLD" whole in the figure's face and size

The deployment screen's party header (every mode) is the same drawing as Subordinates' and would follow it.

Usage: python mock_budget_k.py <screen-gallery folder> <out dir>
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONTS = ROOT / 'godot' / 'fonts'

NAME = (232, 199, 122)
SUBTITLE = (160, 140, 104)
QUIET = (176, 160, 130)
VALUE = (236, 226, 204)
GAIN = (159, 201, 138)
LOSS = (217, 118, 94)
SHEET = (24, 20, 18)
TEXT = (244, 232, 206)
MINUS = '−'

VARIANTS = [('now', 'Now'), ('A', 'A  "116,400K" + small "GOLD"'), ('B', 'B  "116,400K" alone'),
            ('C', 'C  "116,400K GOLD" whole')]


def cinzel(px, weight):
    f = ImageFont.truetype(str(FONTS / 'Cinzel-VariableFont_wght.ttf'), px)
    f.set_variation_by_axes([weight])
    return f


def amount(d, right, baseline, figure, font, colour, unit_font, unit_colour, variant, gap):
    """An amount ending at right on baseline, in the variant's manner; returns its left edge."""
    if variant == 'A':
        main, unit = figure + 'K', 'GOLD'
    elif variant == 'B':
        main, unit = figure + 'K', None
    else:
        main, unit = figure + 'K GOLD', None
    x = right
    if unit:
        uw = d.textlength(unit, font=unit_font)
        d.text((x - uw, baseline), unit, font=unit_font, fill=unit_colour, anchor='ls')
        x -= uw + gap
    w = d.textlength(main, font=font)
    d.text((x - w, baseline), main, font=font, fill=colour, anchor='ls')
    return x - w


def fill(d, im, box, at):
    d.rectangle(box, fill=im.getpixel(at))


def subordinates_pc(src, variant):
    im = src.copy()
    if variant == 'now':
        return im
    d = ImageDraw.Draw(im)
    fill(d, im, (1060, 380, 1249, 404), (1058, 392))
    x = amount(d, 1245, 398, '116,400', cinzel(20, 700), NAME, cinzel(11, 600), SUBTITLE, variant, 6)
    label = cinzel(12, 600)
    lw = d.textlength('Budget', font=label)
    d.text((x - 10 - lw, 395), 'Budget', font=label, fill=(150, 134, 108), anchor='ls')
    return im


def subordinates_upright(src, variant):
    im = src.copy()
    if variant == 'now':
        return im
    # The wave backdrop under the old figure, patched from the backdrop right of it (same rows).
    im.paste(src.crop((200, 1286, 380, 1318)), (18, 1286))
    d = ImageDraw.Draw(im)
    font, unit = cinzel(27, 700), cinzel(12, 600)
    main = '116,400K' if variant != 'C' else '116,400K GOLD'
    d.text((22, 1310), main, font=font, fill=NAME, anchor='ls')
    if variant == 'A':
        d.text((22 + d.textlength(main, font=font) + 8, 1308), 'GOLD', font=unit, fill=SUBTITLE, anchor='ls')
    return im


def week_report(src, variant):
    im = src.copy()
    if variant == 'now':
        return im
    d = ImageDraw.Draw(im)
    bold, unit = cinzel(20, 700), cinzel(13, 600)
    for baseline, figure, colour in [(339, '102,700', VALUE), (381, '+20,200', GAIN), (423, MINUS + '6,500', LOSS)]:
        fill(d, im, (740, baseline - 17, 903, baseline + 3), (742, baseline - 20))
        amount(d, 900, baseline, figure, bold, colour, unit, QUIET, variant, 6)
    fill(d, im, (600, 448, 902, 488), (602, 446))
    amount(d, 888, 477, '116,400', cinzel(30, 800), NAME, unit, QUIET, variant, 6)
    return im


SCREENS = [
    ('subordinates', 'Subordinates, the orders\' header (PC)', 'lacquer/current/subordinates.png', subordinates_pc,
     (1000, 368, 1262, 412), 3),
    ('subordinates-upright', 'Subordinates, the budget at the bar (phone upright)',
     'phone-upright/current/subordinates.png', subordinates_upright, (0, 1250, 390, 1330), 2),
    ('week-report', 'The week report\'s treasury (PC)', 'lacquer/current/week-report.png', week_report,
     (360, 288, 920, 500), 2),
]


def sheet(gallery, out, key, title, path, draw, crop, zoom):
    src = Image.open(gallery / path).convert('RGB')
    pieces = []
    for v, caption in VARIANTS:
        im = draw(src, v)
        im.save(out / f'{key}-{v}.png')
        part = im.crop(crop)
        pieces.append((caption, part.resize((part.width * zoom, part.height * zoom), Image.LANCZOS)))
    head, gap = 44, 14
    w = max(p.width for _, p in pieces) + 2 * gap
    h = 56 + sum(head + p.height + gap for _, p in pieces)
    s = Image.new('RGB', (w, h), SHEET)
    d = ImageDraw.Draw(s)
    d.text((gap, 14), title, font=cinzel(22, 700), fill=NAME)
    y = 56
    for caption, p in pieces:
        d.text((gap, y + 10), caption, font=cinzel(18, 600), fill=TEXT)
        y += head
        s.paste(p, (gap, y))
        y += p.height + gap
    s.save(out / f'sheet-{key}.png')


def main():
    gallery, out = Path(sys.argv[1]), Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    for screen in SCREENS:
        sheet(gallery, out, *screen)


if __name__ == '__main__':
    main()
