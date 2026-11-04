"""Mock-ups of the results plate's level-up rows with the longest text they can hold (owner, 2026-11-04): the gallery's
info-level-up shows four rows of the longest names and gains (FIGT.ADV:3065-3160), and the gains are cut with "…" on
every device. Round 2, the owner's A redesigned (docs/ui/messages.md, §7): each row in two columns —

  the left one LEVEL UP over the name, the name wrapped at its spaces (never inside a word);
  the right one the gains whole, wrapped at their spaces, every line at the column's left (never further left).

A row is as tall as its taller column; the plate grows with its rows (the PC's from its top down as the game places it,
held landscape about its middle, upright up from its foot). Drawn over the gallery's dumps of the PC, the phone held
landscape and upright.

The dumps: tools/ui/gallery.ps1 -PhoneLand -PhonePortrait -Only info-level-up (docs/ui/gallery.md). Then, from the
repository's root (Pillow; godot/fonts):
  python -I tools/ui/mock_level_ups.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository. A plate that runs past the screen is reported.
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

FONTS = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'godot', 'fonts')
CINZEL = os.path.join(FONTS, 'Cinzel-VariableFont_wght.ttf')

ROW_FILL, ROW_LINE = (34, 26, 20), (110, 86, 46)
ACCENT, NAME, VALUE = (242, 199, 92), (232, 199, 122), (236, 226, 204)   # BattleInfoLayer.Accent, Ui.Name, Ui.Value

ROWS = [  # the gallery's made-up rows: the longest names and gains
    ('Mars the Great', ['Leader Attacks +1', 'The Popularity Staff increased troops by 250.']),
    ('Seizel Sisters', ['Leader Attacks +1', 'Def in Dungeons UP']),
    ('Mill', ['Leader Attacks +1', 'Summoned 50 more phantom beasts.']),
    ('Aristoles', ['Leader Attacks +1', 'Atk in Dungeons UP']),
]
LONGEST_WORD = 'Aristoles'     # the widest single word of the units' names (SET_CHR's MS 1): the name column's least width

# Each device's plate in its dump (pixels, measured off the dumps and BattleInfoLayer's drawing): the plate's box, the
# rows' left and right, the first row's top, the rows' pitch and height in the game now, the outline's width, a line of
# the plate with nothing on it (to stretch), the types (LEVEL UP, the name, the gains), the row's room at its left, top
# and right, the room between the columns and between the rows, and where the taller plate stands ('top': the PC's
# max(8, 200 - h / 2); 'middle': about the old plate's middle; 'foot': its foot kept). Held landscape the row is so wide
# that the names need no wrap (whole_names: the column as wide as the widest name), and the plate's head gives up some of
# its empty bands (screen rows) so that four rows fit the screen.
DEVICES = {
    'pc': dict(file='lacquer', plate=(332, 8, 946, 464), rows=(369, 910), top=275, pitch=44, height=37, line=1, blank=270,
               word=15, name=19, gains=16, left=20, pad=7, right=14, gap=24, between=8, place='top'),
    'phone-landscape': dict(file='phone-landscape', plate=(438, 13, 1372, 767), rows=(477, 1332), top=519, pitch=48.33,
                            height=44, line=2, blank=508, word=20, name=27, gains=23, left=20, pad=8, right=16, gap=30,
                            between=5, place='middle', whole_names=True,
                            bands=[(110, 124), (156, 166), (188, 198), (380, 405), (448, 485)]),
    'phone-portrait': dict(file='phone-portrait', plate=(12, 256, 599, 870), rows=(31, 579), top=635, pitch=46, height=39, line=2,
                          blank=626, word=15, name=20, gains=17, left=12, pad=6, right=10, gap=16, between=7, place='foot'),
}


def font(size, weight):
    f = ImageFont.truetype(CINZEL, size)
    f.set_variation_by_axes([weight])
    return f


def width(s, f):
    return f.getlength(s)


def wrap(words, f, room):
    """Words into lines no wider than the room, broken only between words (a word wider than the room alone on its line)."""
    lines, line = [], ''
    for w in words:
        trial = w if not line else line + ' ' + w
        if line and width(trial, f) > room:
            lines.append(line)
            line = w
        else:
            line = trial
    return lines + ([line] if line else [])


def gain_words(gains):
    """The gains as words to wrap, the separator kept with the word after it (no line starts or ends on it alone)."""
    words = []
    for i, gain in enumerate(gains):
        parts = gain.split(' ')
        if i > 0:
            parts[0] = ' ·  ' + parts[0]     # with the joining space: two spaces each side, as the game's "  ·  "
        words += parts
    return words


def cap(f):
    _, y0, _, y1 = f.getbbox('H', anchor='ls')
    return y1 - y0


def text_lines(d, x, top, lines, f, pitch, colour):
    """Lines from <top>, each <pitch> high, the capitals centred in it."""
    for i, s in enumerate(lines):
        d.text((x, top + i * pitch + (pitch + cap(f)) / 2), s, font=f, fill=colour, anchor='ls')


def row_box(d, box, line):
    d.rectangle(box, fill=ROW_FILL)
    for i in range(line):
        d.rectangle((box[0] + i, box[1] + i, box[2] - i, box[3] - i), outline=ROW_LINE)


OSWALD = os.path.join(FONTS, 'Oswald-VariableFont_wght.ttf')
QUIET, BADGE_TOP, BADGE_FOOT, BADGE_INK = (176, 160, 130), (226, 190, 110), (176, 134, 58), (40, 28, 12)

# Round 3: the label LEVEL UP over the name, apart from it (owner, 2026-11-04: it ran into the name). Its type as a part of
# the device's word type, its colour, its face and weight, the room between its letters (a part of its type), a badge.
LABELS = {
    'A2': dict(scale=1.0, colour=ACCENT, face=CINZEL, weight=700, tracking=0.0, badge=False,
               note='as round 2: the word type, gold, Cinzel'),
    'L1': dict(scale=0.75, colour=ACCENT, face=CINZEL, weight=700, tracking=0.18, badge=False,
               note='smaller, spaced, gold'),
    'L2': dict(scale=0.75, colour=QUIET, face=CINZEL, weight=600, tracking=0.14, badge=False,
               note='smaller, spaced, muted'),
    'L3': dict(scale=0.68, colour=BADGE_INK, face=CINZEL, weight=800, tracking=0.08, badge=True,
               note='a small gold badge'),
    'L4': dict(scale=0.85, colour=ACCENT, face=OSWALD, weight=500, tracking=0.12, badge=False,
               note='Oswald, spaced, gold'),
}


def label_font(dev, style):
    f = ImageFont.truetype(style['face'], max(8, round(dev['word'] * style['scale'])))
    f.set_variation_by_axes([style['weight']])
    return f


def label_width(s, f, style):
    track = f.size * style['tracking']
    return sum(width(c, f) for c in s) + track * (len(s) - 1) + (f.size * 0.9 if style['badge'] else 0)


def draw_label(img, d, x, top, pitch, s, f, style):
    """The label on its line, its letters apart; a badge round it when the style has one."""
    track = f.size * style['tracking']
    base = top + (pitch + cap(f)) / 2
    if style['badge']:
        pad_x, pad_y = f.size * 0.45, f.size * 0.42
        box = (round(x), round(base - cap(f) - pad_y), round(x + label_width(s, f, style)), round(base + pad_y))
        for yy in range(box[1], box[3] + 1):
            t = (yy - box[1]) / max(1, box[3] - box[1])
            d.line((box[0], yy, box[2], yy), fill=tuple(round(a + (b - a) * t) for a, b in zip(BADGE_TOP, BADGE_FOOT)))
        x += pad_x
    for c in s:
        d.text((x, base), c, font=f, fill=style['colour'], anchor='ls')
        x += width(c, f) + track


def layout(dev, rows, style):
    """Each row's columns: the name's lines and the gains' lines, the line pitches and the row's height."""
    fw, fn, fg = label_font(dev, style), font(dev['name'], 600), font(dev['gains'], 500)
    longest = max((u for u, _ in rows), key=lambda u: width(u, fn)) if dev.get('whole_names') else LONGEST_WORD
    column = max(label_width('LEVEL UP', fw, style), width(longest, fn))
    x0, x1 = dev['rows']
    gx = x0 + dev['left'] + column + dev['gap']
    room = x1 - dev['right'] - gx
    pw, pn, pg = fw.size * (1.75 if style['badge'] else 1.3), dev['name'] * 1.08, dev['gains'] * 1.3
    out = []
    for unit, gains in rows:
        names = wrap(unit.split(' '), fn, column)
        lines = wrap(gain_words(gains), fg, room)
        left_h, right_h = pw + len(names) * pn, len(lines) * pg
        out.append(dict(names=names, lines=lines, left_h=left_h, right_h=right_h,
                        height=round(2 * dev['pad'] + max(left_h, right_h))))
    return dict(fonts=(fw, fn, fg), pitches=(pw, pn, pg), column=column, gx=gx, rows=out)


def draw(src, dev, rows, report, name, style):
    """The plate rebuilt with the new rows, as tall as they need; the image and the rows' band in it."""
    px0, py0, px1, py1 = dev['plate']
    n = len(rows)
    old_foot = dev['top'] + (n - 1) * dev['pitch'] + dev['height']
    plate = src.crop((px0, py0, px1, py1))
    head = plate.crop((0, 0, plate.width, dev['top'] - py0))
    for b0, b1 in sorted(dev.get('bands', []), reverse=True):     # the head's empty bands taken out, the lowest first
        cut = Image.new('RGB', (plate.width, head.height - (b1 - b0)))
        cut.paste(head.crop((0, 0, plate.width, b0 - py0)), (0, 0))
        cut.paste(head.crop((0, b1 - py0, plate.width, head.height)), (0, b0 - py0))
        head = cut
    foot = plate.crop((0, round(old_foot) - py0, plate.width, plate.height))
    blank = plate.crop((0, dev['blank'] - py0, plate.width, dev['blank'] - py0 + 1))
    lay = layout(dev, rows, style)
    rows_h = sum(r['height'] for r in lay['rows']) + (n - 1) * dev['between']
    new = Image.new('RGB', (plate.width, head.height + rows_h + foot.height))
    new.paste(head, (0, 0))
    new.paste(blank.resize((plate.width, rows_h)), (0, head.height))
    new.paste(foot, (0, head.height + rows_h))

    d = ImageDraw.Draw(new)
    fw, fn, fg = lay['fonts']
    pw, pn, pg = lay['pitches']
    x0, x1 = dev['rows'][0] - px0, dev['rows'][1] - px0
    gx = lay['gx'] - px0
    top = head.height
    for r in lay['rows']:
        row_box(d, (x0, top, x1, top + r['height'] - 1), dev['line'])
        inner = r['height'] - 2 * dev['pad']
        lt = top + dev['pad'] + (inner - r['left_h']) / 2
        draw_label(new, d, x0 + dev['left'], lt, pw, 'LEVEL UP', fw, style)
        text_lines(d, x0 + dev['left'], lt + pw, r['names'], fn, pn, NAME)
        text_lines(d, gx, top + dev['pad'] + (inner - r['right_h']) / 2, r['lines'], fg, pg, VALUE)
        top += r['height'] + dev['between']

    h, screen = new.height, src.height
    if dev['place'] == 'top':
        y = max(8, round(200 - h / 2))
    elif dev['place'] == 'middle':
        y = round((py0 + py1) / 2 - h / 2)
    else:
        y = py1 - h
    if y + h > screen:
        report.append(f'{name}: the plate runs {y + h - screen} px past the screen\'s foot')
    if y < 0:
        report.append(f'{name}: the plate runs {-y} px past the screen\'s top')
    report.append(f'{name}: plate {h} px high (was {py1 - py0}), rows ' + ', '.join(str(r['height']) for r in lay['rows']))
    out = src.copy()
    out.paste(new, (px0, y))
    return out, (px0, max(0, y + head.height - 12), px1, min(screen, y + head.height + rows_h + 12))


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
        bands = []
        for key, style in LABELS.items():
            whole, band = draw(src, dev, ROWS, report, f'{name} {key}', style)
            label(whole, f'{key} ({name})')
            whole.save(os.path.join(out, f'{key}-{name}.png'))
            bands.append((key, style['note'], whole.crop(band)))
        # The sheet: each variant's rows one under another, its name over them.
        f = font(max(16, bands[0][2].width // 34), 700)
        head = f.size + 16
        sheet = Image.new('RGB', (bands[0][2].width, sum(b[2].height + head for b in bands)), (0, 0, 0))
        y = 0
        for key, note, img in bands:
            ImageDraw.Draw(sheet).text((12, y + head / 2), f'{key} — {note}', font=f, fill=ACCENT, anchor='lm')
            sheet.paste(img, (0, y + head))
            y += head + img.height
        sheet.save(os.path.join(out, f'labels-{name}.png'))
    print('\n'.join(report))


main()
