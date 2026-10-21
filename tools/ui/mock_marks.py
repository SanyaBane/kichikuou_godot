"""Mock-ups of the marks on Talk and the harem (docs/ui/talk-marks.md, decision 30): whether a talk or a visit can bring
anything new, drawn over dumps of our Harem and Subordinates screens.

The dumps come from the kept save saves-week18 in a window (our interface, screen dumps on):
  <Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look-marks
      --slots-from user://saves-week18 --harem 9 --until deployment --interface new --shots <dir>
(the first "picker" dump is the harem, "subordinates" the Subordinates screen). Then, from the repository's root
(Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_marks.py <harem dump> <subordinates dump> <out dir> < /dev/null
The marks shown are made up for the look; the real ones come from the dry run. Keep <out dir> outside the repository
(the dumps hold the player's pictures). Everything is in the dumps' pixels, drawn at 2x and scaled down.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

HAREM_DUMP, SUBS_DUMP, OUT = sys.argv[1:4]
sys.stdout.reconfigure(encoding='utf-8')
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
CAPTION = 64

# Lacquer.cs
GOLD, DIM = (196, 160, 86, 255), (110, 86, 46, 255)
NAME, QUIET, TEXT = (232, 199, 122, 255), (176, 160, 130, 255), (244, 232, 206, 255)
OFF_TEXT = (98, 88, 74, 255)
NEW = (150, 196, 110, 255)                      # HaremView's "new"
MAYBE = (226, 168, 72, 255)                     # amber: a draw decides
INK_GREEN, VERMILION_INK, INK_QUIET = (52, 104, 40, 255), (150, 52, 30, 255), (100, 80, 56, 255)
PAPER = (224, 208, 172, 255)

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
_fonts = {}


def font(size, path=CINZEL, weight=700):
    key = (size, path, weight)
    if key not in _fonts:
        f = ImageFont.truetype(path, round(size * S))
        if path == CINZEL:
            f.set_variation_by_axes([weight])
        _fonts[key] = f
    return _fonts[key]


class Canvas:
    def __init__(self, dump):
        src = Image.open(dump).convert('RGBA')
        self.w, self.h = src.size
        self.src = src
        self.im = Image.new('RGBA', (self.w * S, (self.h + CAPTION) * S), (23, 24, 27, 255))
        self.im.paste(src.resize((self.w * S, self.h * S), Image.NEAREST), (0, 0))
        self.d = ImageDraw.Draw(self.im)

    def at(self, x, y):
        return self.src.getpixel((x, y))

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle((x0 * S, y0 * S, x1 * S - 1, y1 * S - 1), fill=fill, outline=outline, width=width * S)

    def rounded(self, r, radius, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rounded_rectangle((x0 * S, y0 * S, x1 * S - 1, y1 * S - 1), radius * S, fill=fill, outline=outline,
                                 width=width * S)

    def text(self, xy, s, f, fill, anchor='la'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def polygon(self, pts, fill=None, outline=None):
        self.d.polygon([(x * S, y * S) for x, y in pts], fill=fill, outline=outline)

    def ellipse(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.ellipse((x0 * S, y0 * S, x1 * S, y1 * S), fill=fill, outline=outline, width=width * S)

    def save(self, name, caption, note):
        self.rect((0, self.h, self.w, self.h + CAPTION), fill=(23, 24, 27, 255))
        self.text((16, self.h + 10), caption, font(16, GOTHIC), (236, 226, 204, 255))
        self.text((16, self.h + 36), note, font(13, GOTHIC), (176, 160, 130, 255))
        self.im.resize((self.w, self.h + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
        print(name)


def tag(c, right, mid, label, colour, ink=(24, 18, 12, 255), size=10, filled=True):
    """A small pill ending at x = right, centred on y = mid."""
    f = font(size)
    w = c.width(label, f) + 10
    r = (right - w, mid - 7, right, mid + 7)
    if filled:
        c.rounded(r, 7, fill=colour)
        c.text(((r[0] + r[2]) / 2, mid + 0.5), label, f, ink, 'mm')
    else:
        c.rounded(r, 7, outline=colour)
        c.text(((r[0] + r[2]) / 2, mid + 0.5), label, f, colour, 'mm')
    return r


def big_tag(c, right, mid, label, colour, ink=(24, 18, 12, 255)):
    """A larger pill for a large button."""
    f = font(14)
    w = c.width(label, f) + 18
    c.rounded((right - w, mid - 11, right, mid + 11), 11, fill=colour)
    c.text((right - w / 2, mid + 0.5), label, f, ink, 'mm')


# ---- the harem ---------------------------------------------------------------------------------------------------
# Tablets (HaremView.TabletRect) in the dump: three columns, 32 px rows from y 126.
COLS = [(705, 877), (880, 1051), (1054, 1225)]
GIRLS = {  # cell (column, row) -> name, visits, mark (made up)
    (1, 0): ('Lia', 0, 'new'),
    (2, 0): ('Maris', 3, 'read'),
    (2, 1): ('Kanami', 2, 'maybe'),
    (2, 2): ('Haurein', 1, 'new'),
}


def tablet(col, row):
    x0, x1 = COLS[col]
    y0 = 126 + row * 35
    return x0, y0, x1, y0 + 32


def clear_slot(c, r, w=56):
    x0, y0, x1, y1 = r
    bg = c.at(x1 - 4, y0 + 4)
    c.rect((x1 - w, y0 + 3, x1 - 3, y1 - 3), fill=bg)


def summon_line(c, label, colour, value):
    """A third figure in the summon panel, after Visits and Last summoned."""
    c.text((370, 601), label.upper(), font(9, CINZEL, 600), (150, 134, 108, 255))
    c.text((370, 617), value, font(17, GOTHIC), colour)


def harem_slot():
    c = Canvas(HAREM_DUMP)
    for (col, row), (name, visits, mark) in GIRLS.items():
        r = tablet(col, row)
        clear_slot(c, r)
        mid = (r[1] + r[3]) / 2
        if mark == 'new':
            c.text((r[2] - 10, mid + 1), 'NEW', font(12), NEW, 'rm')
        elif mark == 'maybe':
            c.text((r[2] - 10, mid + 1), '???', font(12), MAYBE, 'rm')
        else:
            c.text((r[2] - 10, mid + 1), f'{visits}×', font(12, CINZEL, 600), QUIET, 'rm')
    summon_line(c, 'This visit', NEW, 'Something new')
    c.save('HA-slot.png', 'HA — the slot: NEW, ??? or the visits',
           'The tablet\'s right slot, where "new" (never summoned) is now: NEW green, ??? amber, read shows the visits. '
           'The panel says it in words.')


def harem_corner():
    c = Canvas(HAREM_DUMP)
    for (col, row), (name, visits, mark) in GIRLS.items():
        r = tablet(col, row)
        clear_slot(c, r)
        mid = (r[1] + r[3]) / 2
        c.text((r[2] - 22, mid + 1), f'{visits}×' if visits else '—', font(12, CINZEL, 600), QUIET, 'rm')
        x1, y0 = r[2], r[1]
        if mark == 'new':
            c.polygon([(x1 - 16, y0), (x1, y0), (x1, y0 + 16)], fill=NEW)
        elif mark == 'maybe':
            c.polygon([(x1 - 18, y0), (x1, y0), (x1, y0 + 18)], fill=MAYBE)
            c.text((x1 - 5, y0 + 5), '?', font(9, CINZEL, 900), (24, 18, 12, 255), 'mm')
    summon_line(c, 'This visit', NEW, 'Something new')
    c.save('HB-corner.png', 'HB — a corner: green or amber, nothing when read',
           'A folded corner at the tablet\'s top right; the slot always shows the visits (— for none). '
           'For ??? the panel would say "Something may happen" in amber.')


def harem_dim():
    c = Canvas(HAREM_DUMP)
    for (col, row), (name, visits, mark) in GIRLS.items():
        r = tablet(col, row)
        clear_slot(c, r)
        mid = (r[1] + r[3]) / 2
        if mark == 'read':
            bg = c.at(r[0] + 4, r[1] + 4)
            c.rect((r[0] + 22, r[1] + 3, r[2] - 3, r[3] - 3), fill=bg)
            c.text((r[0] + 26, mid + 1), name, font(15, CINZEL, 800), OFF_TEXT, 'lm')
            c.text((r[2] - 10, mid + 1), f'{visits}×', font(12, CINZEL, 600), OFF_TEXT, 'rm')
        elif mark == 'new':
            tag(c, r[2] - 6, mid, 'NEW', NEW)
        else:
            tag(c, r[2] - 6, mid, '???', MAYBE)
    summon_line(c, 'This visit', NEW, 'Something new')
    c.save('HC-dim.png', 'HC — read tablets dimmed, a pill on the others',
           'Nothing to gain: the tablet fades (still a click to summon). NEW and ??? as filled pills.')


# ---- the Subordinates screen -----------------------------------------------------------------------------------------
# Ledger rows: 24 px from y 114 (Rance) to 330 (Haurein); the margin left of No. is free but for the chosen row's diamond.
UNITS = ['new', 'new', 'read', 'new', 'read', 'read', 'new', 'read', 'maybe']   # Rance … Haurein (made up)
TALK = (700, 604, 913, 686)


def row_mid(i):
    return 126 + i * 24


def talk_button(c, mark):
    x0, y0, x1, y1 = TALK
    if mark == 'new':
        tag(c, x1 - 6, y0 + 12, 'NEW', NEW)
    elif mark == 'maybe':
        tag(c, x1 - 6, y0 + 12, '???', MAYBE)
    else:
        bg = c.at(800, 668)
        c.rect((782, 655, 908, 674), fill=bg)
        c.text((844, 665), 'NOTHING NEW', font(9, CINZEL, 600), OFF_TEXT, 'mm')


def subs_pill():
    c = Canvas(SUBS_DUMP)
    for i, mark in enumerate(UNITS):
        mid = row_mid(i)
        if mark == 'new':
            tag(c, 108, mid, 'NEW', INK_GREEN, ink=PAPER, size=9)
        elif mark == 'maybe':
            tag(c, 108, mid, '???', VERMILION_INK, ink=PAPER, size=9)
    talk_button(c, UNITS[0])
    c.save('SA-pill.png', 'SA — a pill in the ledger\'s margin, the same on Talk',
           'NEW green, ??? vermilion, nothing when read; the chosen unit\'s Talk carries its mark '
           '(Rance\'s first talk here).')


def bubble(c, x, y, fill=None, outline=None, label=None, ink=PAPER):
    c.rounded((x - 11, y - 7, x + 11, y + 6), 5, fill=fill, outline=outline)
    c.polygon([(x - 6, y + 5), (x - 1, y + 5), (x - 8, y + 10)], fill=fill or outline)
    if label:
        c.text((x, y - 0.5), label, font(9, CINZEL, 900), ink, 'mm')


def subs_bubble():
    c = Canvas(SUBS_DUMP)
    for i, mark in enumerate(['read'] + UNITS[1:]):                 # Rance read here, as his Talk below
        mid = row_mid(i)
        if mark == 'new':
            bubble(c, 92, mid, fill=INK_GREEN, label='!')
        elif mark == 'maybe':
            bubble(c, 92, mid, fill=VERMILION_INK, label='?')
        else:
            bubble(c, 92, mid, outline=(150, 132, 104, 160))
    talk_button(c, 'read')
    c.save('SB-bubble.png', 'SB — a speech bubble per unit: the mark is about talking',
           'Filled green "!" new, vermilion "?" a draw decides, an empty outline when read; '
           'Talk for a read unit says "nothing new" (Rance read here).')


def harem_chosen():
    """The owner's choice (round 2): HA without the visits — a read tablet's slot stays empty."""
    c = Canvas(HAREM_DUMP)
    for (col, row), (name, visits, mark) in GIRLS.items():
        r = tablet(col, row)
        clear_slot(c, r)
        mid = (r[1] + r[3]) / 2
        if mark == 'new':
            c.text((r[2] - 10, mid + 1), 'NEW', font(12), NEW, 'rm')
        elif mark == 'maybe':
            c.text((r[2] - 10, mid + 1), '???', font(12), MAYBE, 'rm')
    summon_line(c, 'This visit', NEW, 'Something new')
    big_tag(c, 652, 691, 'NEW', NEW)                     # the chosen girl's mark on Summon, as Talk's pill
    c.save('H-chosen.png', 'H — HA without the visits, the mark on Summon too (the owner\'s choice)',
           'NEW green, ??? amber, a read tablet\'s slot empty (Maris); Summon carries the chosen girl\'s pill, none when '
           'read; the visits stay in the panel.')


def subs_chosen():
    """The owner's choice (round 2): SB's bubbles in the ledger, SA's pill on Talk."""
    c = Canvas(SUBS_DUMP)
    for i, mark in enumerate(UNITS):
        mid = row_mid(i)
        if mark == 'new':
            bubble(c, 92, mid, fill=INK_GREEN, label='!')
        elif mark == 'maybe':
            bubble(c, 92, mid, fill=VERMILION_INK, label='?')
        else:
            bubble(c, 92, mid, outline=(150, 132, 104, 160))
    talk_button(c, UNITS[0])
    c.save('S-chosen.png', 'S — SB\'s bubbles in the ledger, SA\'s pill on Talk (the owner\'s choice)',
           'Rance new here: a green bubble in his row and NEW on Talk; a read unit\'s Talk has no pill.')


harem_chosen()
subs_chosen()
harem_slot()
harem_corner()
harem_dim()
subs_pill()
subs_bubble()
