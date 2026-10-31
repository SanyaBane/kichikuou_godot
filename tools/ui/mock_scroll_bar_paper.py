"""Mock-ups of the reference scroll bar on the paper screens upright (owner, 2026-10-31; docs/ui/phone.md, "Round P32").

The owner chose round P31's S2 (tools/ui/mock_scroll_bar.py) as the reference for every scroll bar: a groove 20 reference
pixels wide (1.1 mm on the Poco X6 Pro) in a dim gold rim, the gold thumb with a lit edge, 10 px right of its list. On
the lacquer (Treasure, the Harem, the save lists) it stands as drawn; on the ledger's paper (the message log, the options'
sheet, the deployment and Subordinates ledgers) the gold on beige would read weakly, so these draw the reference's size
and shape over the paper in three inks:

  PA the lacquer's own: the dark groove, the dim gold rim, the gold thumb (the reference as it is);
  PB vermilion: a groove of the paper's head in a faint ink rim, the thumb in the vermilion ink (the log's thumb now, the
     chosen tab's top, the seals) with a lighter edge — the agent's recommendation;
  PC brown ink: the same groove, the thumb in the quiet ink (the ledger's bars now) with a grey edge.

The dumps: the screen gallery's upright pass (tools/ui/gallery.ps1 -PhoneUp; docs/ui/gallery.md): message-log (scrolled
to its foot), options-display (the one tab that scrolls on the reference phone, at its top) and deployment (the week-18
save's ledger, scrolled neither way: its rows a little past the foot, its columns past the right). Then, from the
repository's root (Pillow and numpy; godot/fonts):
  python -I tools/ui/mock_scroll_bar_paper.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository: the dumps hold the player's CGs.

The places are the dumps' (half the reference) read in reference pixels; the thumbs keep the old ones' share and place.
Each screen's old bar is painted over with the paper and the new groove drawn where the build would put it; what the
build would move (the log's rules and the options' column ending 10 px before the groove, the ledger's rows over its
bars) is cut, not laid out again.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

GALLERY, OUT = sys.argv[1:3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)

PX_MM = 446 / 25.4                      # the Poco X6 Pro: 17.6 reference pixels a millimetre
NOTE = (255, 92, 160)                   # the mock-ups' own notes
SIZE = (1220, 2712)                     # the reference phone upright; the dump is half of it

# The reference (Devices/PhoneScrollBar): the groove's width, the rim, the thumb's inset.
WIDTH, RIM, INSET = 20, 2, 3

# The palette's colours (godot/Strategy/UiPalette.cs, lacquer).
PAPER, PAPER_HEAD = (224, 208, 172), (196, 176, 136)
INK_FAINT, INK_QUIET, INK_GREY, VERMILION_INK = (130, 112, 86), (100, 80, 56), (150, 132, 104), (150, 52, 30)
GROOVE, DIM_GOLD, GOLD, NAME = (40, 32, 24), (110, 86, 46), (196, 160, 86), (232, 199, 122)

# A scheme: the groove's fill, its rim, the thumb, the thumb's lit edge.
SCHEMES = {
    'gold': (GROOVE, DIM_GOLD, GOLD, NAME),
    'vermilion': (PAPER_HEAD, INK_FAINT, VERMILION_INK, (200, 104, 72)),
    'ink': (PAPER_HEAD, INK_FAINT, INK_QUIET, INK_GREY),
}


def font(name, size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size)))


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)


def mm(px):
    return f'{px / PX_MM:.1f} mm'


def dump(name):
    small = Image.open(os.path.join(GALLERY, 'phone-upright', 'current', name + '.png')).convert('RGB')
    return small.resize(SIZE, Image.LANCZOS)


def bar(img, groove, share, at, scheme, vertical=True):
    """The reference bar in `groove` (x0, y0, x1, y1), its thumb `share` of it long at `at` of its way (0 the start)."""
    fill, rim, thumb, lit = SCHEMES[scheme]
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = groove
    r = WIDTH / 2
    d.rounded_rectangle((x0, y0, x1 - 1, y1 - 1), radius=r, fill=rim)
    d.rounded_rectangle((x0 + RIM, y0 + RIM, x1 - 1 - RIM, y1 - 1 - RIM), radius=r - RIM, fill=fill)
    length = (y1 - y0 if vertical else x1 - x0) - 2 * INSET
    h = max(60, length * share)
    start = INSET + (length - h) * at
    if vertical:
        box = (x0 + INSET, y0 + start, x1 - 1 - INSET, y0 + start + h)
        d.rounded_rectangle(box, radius=r - INSET, fill=thumb)
        lx = x0 + INSET + WIDTH / 6
        d.line((lx, box[1] + r, lx, box[3] - r), fill=lit, width=round(WIDTH / 10))
    else:
        box = (x0 + start, y0 + INSET, x0 + start + h, y1 - 1 - INSET)
        d.rounded_rectangle(box, radius=r - INSET, fill=thumb)
        ly = y0 + INSET + WIDTH / 6
        d.line((box[0] + r, ly, box[2] - r, ly), fill=lit, width=round(WIDTH / 10))


def paint(img, box, colour):
    ImageDraw.Draw(img).rectangle((box[0], box[1], box[2] - 1, box[3] - 1), fill=colour)


# Each screen: its dump, what it paints over with the paper, its bars as (groove, share, at, vertical), its notes.
SCREENS = {
    'log': ('message-log',
            # the old track (4 wide) and thumb (6) at 1180-1192, and the rules' last 30 px
            [((1160, 218, 1198, 2468), PAPER)],
            [((1170, 220, 1190, 2464), 0.924, 1.0, True)],
            'the groove 12 in from the paper\'s gold frame, the rules ending 10 before it (they ran to 1178); now: a '
            'track 4 px wide (0.2 mm) in the paper\'s head, the thumb 6 px in the vermilion ink at 70%'),
    'options': ('options-display',
                # the old track and thumb (6 wide) at 1178-1184
                [((1168, 344, 1194, 2472), PAPER)],
                [((1170, 344, 1190, 2472), 0.729, 0.0, True)],
                'the groove 6 in from the sheet\'s right; built, the column 8 px narrower (it ends 10 before the groove); '
                'now: 6 px (0.3 mm), the thumb in the grey ink on the paper\'s rule'),
    'ledger': ('deployment',
               # the old vertical bar (8 wide at the paper's right) and horizontal (8 high at its foot), the corner
               [((1180, 2460, 1204, 2484), PAPER_HEAD)],
               [((1184, 1054, 1204, 2460), 0.985, 0.0, True), ((18, 2464, 1180, 2484), 0.89, 0.0, False)],
               'Godot\'s bars restyled as the reference, both ways: down at the paper\'s right, sideways at its foot, '
               '20 px each (the rows and columns end over them); now: 8 px (0.5 mm) in the quiet ink on the paper\'s '
               'head'),
}

VARIANTS = [('Now', None), ('PA — the lacquer\'s gold', 'gold'), ('PB — vermilion (recommended)', 'vermilion'),
            ('PC — brown ink', 'ink')]


def variant(key, scheme):
    name, paints, bars, _ = SCREENS[key]
    img = dump(name)
    if scheme is None:
        return img
    for box, colour in paints:
        paint(img, box, colour)
    for groove, share, at, vertical in bars:
        bar(img, groove, share, at, scheme, vertical)
    return img


def notes(key, scheme):
    if scheme is None:
        return [SCREENS[key][3].split('now: ')[1] if 'now: ' in SCREENS[key][3] else 'as built']
    if scheme == 'gold':
        what = 'the reference as on the lacquer: a dark groove in a dim gold rim, the gold thumb'
    elif scheme == 'vermilion':
        what = ('a groove of the paper\'s head in a faint ink rim, the thumb in the vermilion ink (the log\'s thumb, the '
                'chosen tab\'s top, the seals)')
    else:
        what = 'a groove of the paper\'s head in a faint ink rim, the thumb in the quiet ink (the ledger\'s bars now)'
    return [f'{WIDTH} px ({mm(WIDTH)}) wide: {what}', SCREENS[key][3].split('; now:')[0]]


def wrapped(d, text, face, width):
    words, lines = text.split(' '), ['']
    for w in words:
        trial = (lines[-1] + ' ' + w).strip()
        if d.textlength(trial, font=face) <= width or not lines[-1]:
            lines[-1] = trial
        else:
            lines.append(w)
    return lines


def sheet(name, title, rows, scale, crops, foot):
    """For each screen in `rows`, the variants side by side (each cropped to its `crops` box and scaled), notes under."""
    drawn = []
    for key in rows:
        row = []
        for label, scheme in VARIANTS:
            im = variant(key, scheme).crop(crops[key])
            row.append((label, im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS),
                        notes(key, scheme)))
        drawn.append(row)
    gap, head = 24, 110
    w = max(im.width for row in drawn for _, im, _ in row)
    heights = [max(im.height for _, im, _ in row) for row in drawn]
    out = Image.new('RGB', (len(VARIANTS) * w + (len(VARIANTS) + 1) * gap, head + sum(h + foot + 50 for h in heights)),
                    (40, 40, 40))
    d = ImageDraw.Draw(out)
    d.text((gap, 18), title, font=GOTHIC(34), fill=(255, 255, 255))
    y = head
    for row, h in zip(drawn, heights):
        for i, (label, im, ns) in enumerate(row):
            x = gap + i * (w + gap)
            d.text((x, y - 14), label, font=GOTHIC(26), fill=(255, 255, 255), anchor='ld')
            out.paste(im, (x, y))
            ty = y + h + 12
            for n in ns:
                for t in wrapped(d, n, GOTHIC(20), w):
                    d.text((x, ty), t, font=GOTHIC(20), fill=NOTE)
                    ty += 26
                ty += 6
        y += h + foot + 50
    out.save(os.path.join(OUT, name + '.png'))
    print(os.path.join(OUT, name + '.png'), out.size)


WHOLE = (0, 0) + SIZE
for key, label in [('log', 'the message log, scrolled to its foot'),
                   ('options', 'Options → Display, at its top'),
                   ('ledger', 'the deployment table\'s ledger, the week-18 save')]:
    sheet(f'p32-{key}', f'Round P32 — the reference scroll bar on the paper, upright: {label}', [key], 0.5,
          {key: WHOLE}, foot=200)
sheet('p32-detail', 'Round P32 — the bars at the phone\'s pixels: the log\'s and the options\' right edge, the ledger\'s '
      'foot', ['log', 'options', 'ledger'], 0.75,
      {'log': (760, 160, 1220, 1360), 'options': (760, 300, 1220, 1500), 'ledger': (560, 1900, 1220, 2540)}, foot=200)
