"""Mock-ups of the results plate's level-up rows with the longest text they can hold (owner, 2026-11-04): the gallery's
info-level-up shows four rows of the longest names and gains (FIGT.ADV:3065-3160), and the gains are cut with "…" on
every device. Two ways out, drawn over the gallery's dumps of the PC, the phone held landscape and upright:

  A  two lines a row: LEVEL UP and the name with "Leader Attacks +1" at the row's right, the rest of the gains whole on
     the second line (under the name, or from the row's left where it would not fit there); the plate taller.
  B  one line a row as now, the gains in our own shorter words ("Leader Atk +1 · Troops +250").

The dumps: tools/ui/gallery.ps1 -PhoneLand -PhoneUp -Only info-level-up (docs/ui/gallery.md). Then, from the
repository's root (Pillow; godot/fonts):
  python -I tools/ui/mock_level_ups.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository. A row a text does not fit is cut with "…" as the game cuts it, and reported.
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

FONTS = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'godot', 'fonts')
CINZEL = os.path.join(FONTS, 'Cinzel-VariableFont_wght.ttf')

PLATE_FILL, ROW_FILL, ROW_LINE = (23, 17, 13), (34, 26, 20), (110, 86, 46)
ACCENT, NAME, VALUE = (242, 199, 92), (232, 199, 122), (236, 226, 204)   # BattleInfoLayer.Accent, Ui.Name, Ui.Value

ROWS = [  # the gallery's made-up rows: the longest names and gains
    ('Mars the Great', ['Leader Attacks +1', 'The Popularity Staff increased troops by 250.']),
    ('Seizel Sisters', ['Leader Attacks +1', 'Def in Dungeons UP']),
    ('Mill', ['Leader Attacks +1', 'Summoned 50 more phantom beasts.']),
    ('Aristoles', ['Leader Attacks +1', 'Atk in Dungeons UP']),
]
SHORT = [  # B: our own words for the same gains
    ('Mars the Great', ['Leader Atk +1', 'Troops +250']),
    ('Seizel Sisters', ['Leader Atk +1', 'Dungeon Def +1']),
    ('Mill', ['Leader Atk +1', 'Beasts +50']),
    ('Aristoles', ['Leader Atk +1', 'Dungeon Atk +1']),
]
SHORT_SEPARATOR = ' · '      # B: the gains closer together than the game's "  ·  "

# Each device's plate in its dump (pixels, measured off the dumps and BattleInfoLayer's drawing): the plate's box, the
# rows' left and right, the first row's top, the rows' pitch and height, the outline's width, a line of the plate with
# nothing on it (to stretch), the types (LEVEL UP, the name, the gains) and the insides (the word's inset, the gap after
# it, the gains' least start from the row's left, the room at the row's right), a band of the plate with nothing on it
# that A may take out, and A's row height and pitch.
DEVICES = {
    'pc': dict(file='lacquer', plate=(332, 8, 946, 464), rows=(369, 910), top=275, pitch=44, height=37, line=1, blank=270,
               word=15, name=19, gains=16, inset=20, gap=None, name_x=120, least=240, end=14, band=None, a_height=60, a_pitch=66,
               a_shift=0, b=dict(name_x=None, gap=20, end=10)),   # B: the name right after LEVEL UP, as on the phones
    'phone-landscape': dict(file='phone-landscape', plate=(438, 13, 1372, 767), rows=(477, 1332), top=519, pitch=48.33,
                            height=44, line=2, blank=508, word=20, name=27, gains=23, inset=20, gap=27, name_x=None, least=333,
                            end=13, band=(452, 482), a_height=66, a_pitch=70, a_shift=-11),
    'phone-upright': dict(file='phone-upright', plate=(12, 256, 599, 870), rows=(31, 579), top=635, pitch=46, height=39, line=2,
                          blank=626, word=15, name=20, gains=17, inset=12, gap=15, name_x=None, least=210, end=8, band=None,
                          a_height=60, a_pitch=66, a_shift=-80, b=dict(gap=11)),   # A's foot where it was; B's columns closer
}


def font(size, weight):
    f = ImageFont.truetype(CINZEL, size)
    f.set_variation_by_axes([weight])
    return f


def width(s, f):
    return f.getlength(s)


def fit(s, f, room, report, where):
    """The game's Fit: the text cut with "…" to fit the room."""
    if width(s, f) <= room:
        return s
    report.append(f'{where}: "{s}" does not fit ({width(s, f):.0f} > {room:.0f} px)')
    while len(s) > 1 and width(s + '…', f) > room:
        s = s[:-1]
    return s.rstrip() + '…'


def baseline(top, height, f):
    """Lacquer.Baseline: the capitals centred in the row."""
    _, y0, _, y1 = f.getbbox('H', anchor='ls')
    return top + height / 2 + (y1 - y0) / 2


def row_box(d, box, line):
    d.rectangle(box, fill=ROW_FILL)
    for i in range(line):
        d.rectangle((box[0] + i, box[1] + i, box[2] - i, box[3] - i), outline=ROW_LINE)


def columns(dev, fw, fn, unit, x0):
    """Where the name and the gains start: the PC's fixed name column, the phones' after LEVEL UP."""
    wx = x0 + dev['inset']
    nx = x0 + dev['name_x'] if dev['name_x'] is not None else wx + width('LEVEL UP', fw) + dev['gap']
    gap = 20 if dev['name_x'] is not None else dev['gap']
    gx = max(x0 + dev['least'], nx + width(unit, fn) + gap)
    return wx, nx, gx


def one_line(img, dev, rows, report, name):
    """B: the rows as the game draws them now, over the dump's rows."""
    dev = {**dev, **dev.get('b', {})}
    d = ImageDraw.Draw(img)
    fw, fn, fg = font(dev['word'], 700), font(dev['name'], 600), font(dev['gains'], 500)
    x0, x1 = dev['rows']
    for i, (unit, gains) in enumerate(rows):
        top = round(dev['top'] + i * dev['pitch'])
        box = (x0, top, x1, top + dev['height'] - 1)
        row_box(d, box, dev['line'])
        wx, nx, gx = columns(dev, fw, fn, unit, x0)
        d.text((wx, baseline(top, dev['height'], fw)), 'LEVEL UP', font=fw, fill=ACCENT, anchor='ls')
        d.text((nx, baseline(top, dev['height'], fn)), unit, font=fn, fill=NAME, anchor='ls')
        text = fit(SHORT_SEPARATOR.join(gains), fg, x1 - dev['end'] - gx, report, f'{name} B row {i + 1}')
        d.text((gx, baseline(top, dev['height'], fg)), text, font=fg, fill=VALUE, anchor='ls')


def two_lines(src, dev, rows, report, name):
    """A: the plate rebuilt taller, each row two lines."""
    px0, py0, px1, py1 = dev['plate']
    n = len(rows)
    old_rows = dev['top'] + (n - 1) * dev['pitch'] + dev['height']        # the last row's foot
    plate = src.crop((px0, py0, px1, py1))
    top_part = plate.crop((0, 0, plate.width, dev['top'] - py0))
    if dev['band']:
        b0, b1 = dev['band'][0] - py0, dev['band'][1] - py0
        cut = Image.new('RGB', (plate.width, top_part.height - (b1 - b0)))
        cut.paste(top_part.crop((0, 0, plate.width, b0)), (0, 0))
        cut.paste(top_part.crop((0, b1, plate.width, top_part.height)), (0, b0))
        top_part = cut
    foot = plate.crop((0, round(old_rows) - py0, plate.width, plate.height))
    blank = plate.crop((0, dev['blank'] - py0, plate.width, dev['blank'] - py0 + 1))
    rows_h = round((n - 1) * dev['a_pitch'] + dev['a_height'])
    new = Image.new('RGB', (plate.width, top_part.height + rows_h + foot.height))
    new.paste(top_part, (0, 0))
    new.paste(blank.resize((plate.width, rows_h)), (0, top_part.height))
    new.paste(foot, (0, top_part.height + rows_h))

    d = ImageDraw.Draw(new)
    fw, fn, fg = font(dev['word'], 700), font(dev['name'], 600), font(dev['gains'], 500)
    x0, x1 = dev['rows'][0] - px0, dev['rows'][1] - px0
    line_h = dev['a_height'] / 2
    for i, (unit, gains) in enumerate(rows):
        top = top_part.height + round(i * dev['a_pitch'])
        row_box(d, (x0, top, x1, top + dev['a_height'] - 1), dev['line'])
        wx, nx, _ = columns(dev, fw, fn, unit, x0)
        first = top + 3
        d.text((wx, baseline(first, line_h, fw)), 'LEVEL UP', font=fw, fill=ACCENT, anchor='ls')
        d.text((nx, baseline(first, line_h, fn)), unit, font=fn, fill=NAME, anchor='ls')
        d.text((x1 - dev['end'], baseline(first, line_h, fg)), gains[0], font=fg, fill=VALUE, anchor='rs')
        rest = '  ·  '.join(gains[1:])
        second = top + line_h - 3
        gx = nx if width(rest, fg) <= x1 - dev['end'] - nx else wx
        d.text((gx, baseline(second, line_h, fg)), fit(rest, fg, x1 - dev['end'] - gx, report, f'{name} A row {i + 1}'),
               font=fg, fill=VALUE, anchor='ls')

    out = src.copy()
    y = py0 + dev['a_shift']
    if y + new.height > out.height:
        report.append(f'{name} A: the plate runs {y + new.height - out.height} px past the screen\'s foot')
    if y < 0:
        report.append(f'{name} A: the plate runs {-y} px past the screen\'s top')
    out.paste(new, (px0, y))
    return out


def label(img, text):
    """The mock-up's name in the image's lower left corner, off the plate."""
    d = ImageDraw.Draw(img)
    f = font(max(14, min(img.width, img.height) // 36), 700)
    h = f.size + 12
    d.rectangle((0, img.height - h, width(text, f) + 20, img.height), fill=(0, 0, 0))
    d.text((10, img.height - h / 2), text, font=f, fill=ACCENT, anchor='lm')


def main():
    gallery, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    report = []
    for name, dev in DEVICES.items():
        src = Image.open(os.path.join(gallery, dev['file'], 'current', 'info-level-up.png')).convert('RGB')
        a = two_lines(src, dev, ROWS, report, name)
        label(a, f'A — two lines a row ({name})')
        a.save(os.path.join(out, f'A-{name}.png'))
        b = src.copy()
        one_line(b, dev, SHORT, report, name)
        label(b, f'B — our shorter words ({name})')
        b.save(os.path.join(out, f'B-{name}.png'))
    print('\n'.join(report) if report else 'Every row fits.')


main()
