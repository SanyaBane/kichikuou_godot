"""Mock-ups of the command tags coloured by the week's use they spend (owner, 2026-10-31; docs/ui/command-screen.md,
"The tags' colours"). The status strip's tags are coloured by the week's flag (StatusStripView.ColourOf), the command
panel's and the province menu's by the kind of command (CommandMenuView.TagOf, ProvinceMenuView.TagOf): of the commands
that spend the own city's action (Ui_CmdOwnCityAvail) only Levy has its colour, and Merchant and Dungeon share one teal.

The dumps: the screen gallery's lacquer pass (tools/ui/gallery.ps1; docs/ui/gallery.md). Then, from the repository's
root (Pillow and numpy; godot/fonts):
  python -I tools/ui/mock_tag_colours.py <gallery dir> <out dir> [round] < /dev/null
Keep <out dir> outside the repository: the dumps hold the player's map and CGs. The round is 2 unless given.

A tag is a fill with its word over it in Text and a gold rim (UiPalette.DrawTag), so a pixel in it is the fill blended
toward the word's colour or the rim's: it keeps its blend over the new fill.

Round 1 (over the dumps before TC2 was built). TC1: every command that spends the own city's action in the own city's
brick, Dungeon and the Ruins Search in a brown of their own (Plot's, freed), Merchant teal. TC2: the own city's action a
clearer orange, away from Attack's crimson; Merchant and Trade gold, Treasure's Items brown, Dungeon and Search teal. The
two choices mix. TR1: the deployment screen's Refill chips, which spend Troops, in Troops' green instead of the ledger's
vermilion (whose ink is the own city's brick). The owner chose TC2 and TR1, built.

Round 2 (over TC2 and TR1 as built). TC3, the owner's swap: Construction in the own city's orange, Troops and Talk in
Construction's blue, the own city in Troops' green; the commands that spend each follow it — the panel's Build and Units,
the province menu's own-city tags, the Refill chips, Construction's half-lit tag while a building is under way.
"""
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

GALLERY, OUT = sys.argv[1], sys.argv[2]
ROUND = int(sys.argv[3]) if len(sys.argv) > 3 else 2
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2                                   # the key is drawn at twice its pixels, saved at its own

# Lacquer (godot/Strategy/UiPalette.cs).
PANEL, GOLD, DIM_GOLD = (22, 16, 12), (196, 160, 86), (110, 86, 46)
NAME, QUIET, TEXT, OFF_TAG = (232, 199, 122), (176, 160, 130), (244, 232, 206), (52, 46, 40)
PAPER, VERMILION, VERMILION_INK = (224, 208, 172), (170, 40, 28), (150, 52, 30)
INK_GREEN = (52, 104, 40)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)

# The tags' colours before round 1.
BLUE, PLUM, TEAL, GREEN = (46, 62, 104), (128, 44, 70), (36, 92, 96), (70, 96, 52)
BRICK, CRIMSON, BROWN, CELL = (150, 52, 36), (128, 30, 30), (104, 70, 34), (96, 40, 58)
NINJA, ITEMS = (74, 50, 96), (138, 104, 34)
# Round 1's own; TC2's orange is the own city's since it was built (godot/Strategy/TagColours.cs).
ORANGE = (172, 84, 30)
GREEN_INK = (58, 86, 42)                # TR1's Refill on paper as drawn in round 1


def busy(colour):
    """UiPalette.DrawBusyTag's fill: the colour halfway (55 %) to the spent grey."""
    return tuple(round(c + 0.55 * (o - c)) for c, o in zip(colour, OFF_TAG))


# The strip's tags in round 1's dumps (the week-18 save, every use free): their x ranges at the strip's row.
STRIP_Y = (0, 50)
STRIP_BOX = (0, 0, 1280, 50)
STRIP = {'Merchant': (500, 574), 'Own city': (720, 795), 'Dungeon': (852, 920)}
MENU = (0, 50, 1280, 800)
TAG_TOPS = (TEXT, GOLD)                 # a lit tag's word and rim
BUSY_TOPS = (QUIET, DIM_GOLD)           # a busy tag's


def load(name):
    return Image.open(os.path.join(GALLERY, 'lacquer', 'current', name + '.png')).convert('RGB')


def components(mask):
    """The bounding boxes (x0, y0, x1, y1) and sizes of mask's 4-connected runs."""
    seen = np.zeros(mask.shape, bool)
    h, w = mask.shape
    boxes = []
    for y, x in zip(*np.nonzero(mask)):
        if seen[y, x]:
            continue
        stack, seen[y, x] = [(y, x)], True
        x0 = x1 = x
        y0 = y1 = y
        n = 0
        while stack:
            cy, cx = stack.pop()
            n += 1
            x0, x1, y0, y1 = min(x0, cx), max(x1, cx), min(y0, cy), max(y1, cy)
            for ny, nx in ((cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)):
                if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    stack.append((ny, nx))
        boxes.append((x0, y0, x1 + 1, y1 + 1, n))
    return boxes


def blend_over(px, base, tops):
    """px as base blended toward one of tops: (t, top) of the closest, or None when px lies on none of the lines."""
    best = None
    for top in tops:
        d = top - base
        t = float(np.clip(np.dot(px - base, d) / max(1e-6, np.dot(d, d)), 0, 1))
        miss = float(np.linalg.norm(px - (base + t * d)))
        if best is None or miss < best[0]:
            best = (miss, t, top)
    return (best[1], best[2]) if best[0] < 9 else None


def retint(img, changes):
    """The tags of each change (old colour, new colour, box[, tops]) drawn again in the new colour: a tag is one whose fill
    of exactly the old colour fills most of a tag-sized box inside box (x0, y0, x1, y1); its word and rim (tops, a lit
    tag's unless given) keep their blend."""
    a = np.asarray(img).astype(np.float32)
    out = a.copy()
    found = []
    for change in changes:
        old, new, (bx0, by0, bx1, by1) = change[:3]
        tops = [np.array(t, np.float32) for t in (change[3] if len(change) > 3 else TAG_TOPS)]
        f, n = np.array(old, np.float32), np.array(new, np.float32)
        mask = np.zeros(a.shape[:2], bool)
        mask[by0:by1, bx0:bx1] = np.all(np.abs(a[by0:by1, bx0:bx1] - f) <= 1, axis=2)
        for x0, y0, x1, y1, size in components(mask):
            w, h = x1 - x0, y1 - y0
            if size < 150 or not (10 <= h <= 60 and 24 <= w <= 200) or size < 0.4 * w * h:
                continue
            found.append((old, new, (x0, y0, x1, y1)))
            for y in range(max(0, y0 - 1), min(a.shape[0], y1 + 1)):
                for x in range(max(0, x0 - 1), min(a.shape[1], x1 + 1)):
                    hit = blend_over(a[y, x], f, tops)
                    if hit:
                        t, top = hit
                        out[y, x] = n + t * (top - n)
    return Image.fromarray(np.clip(out + 0.5, 0, 255).astype(np.uint8)), found


def strip(word, old, new):
    x0, x1 = STRIP[word]
    return old, new, (x0, STRIP_Y[0], x1, STRIP_Y[1])


def menu(old, new):
    return old, new, MENU


def whole_strip(old, new, tops=TAG_TOPS):
    return old, new, STRIP_BOX, tops


# ---------------------------------------------------------------------------------------------------------------------
# The deployment screen's Refill chips
# ---------------------------------------------------------------------------------------------------------------------

REFILL = (474, 140, 546, 360)           # the Refill column's chips in deployment-refill-question
REFILL_BG_X = 547                       # a pixel of the row's paper right of the chips


def refill_chips(img, old, new):
    """The Refill chips from old to new ((ink, fill) each): an idle chip is the ink (its rim at 60 %) over the row's
    paper, the lit one the fill with the rim's ink and its word in paper; each pixel keeps its blend."""
    a = np.asarray(img).astype(np.float32)
    out = a.copy()
    (ink, fill), (n_ink, n_fill) = ([np.array(c, np.float32) for c in pair] for pair in (old, new))
    paper = np.array(PAPER, np.float32)
    changed = 0
    x0, y0, x1, y1 = REFILL
    for y in range(y0, y1):
        bg = a[y, REFILL_BG_X]
        for x in range(x0, x1):
            px = a[y, x]
            if np.abs(px - bg).max() <= 6:                             # the row's paper
                continue
            pairs = [(bg, ink, bg, n_ink), (fill, paper, n_fill, paper), (fill, ink, n_fill, n_ink), (bg, fill, bg, n_fill)]
            best = None
            for base, top, nbase, ntop in pairs:
                d = top - base
                t = float(np.clip(np.dot(px - base, d) / max(1e-6, np.dot(d, d)), 0, 1))
                miss = float(np.linalg.norm(px - (base + t * d)))
                if best is None or miss < best[0]:
                    best = (miss, nbase + t * (ntop - nbase))
            if best[0] < 14:
                out[y, x] = best[1]
                changed += 1
    assert changed > 300, f'Refill: {changed} pixels recoloured; the dump is laid out otherwise'
    return Image.fromarray(np.clip(out + 0.5, 0, 255).astype(np.uint8))


# ---------------------------------------------------------------------------------------------------------------------
# The rounds: each variant's changes per dump, and the tags each dump must have changed (so that a dump laid out
# otherwise fails rather than shows half a variant)
# ---------------------------------------------------------------------------------------------------------------------

TC1_STRIP = [strip('Dungeon', TEAL, BROWN)]
TC2_STRIP = [strip('Own city', BRICK, ORANGE), strip('Merchant', TEAL, ITEMS)]
ROUND_1 = {
    'TC1': {
        'province-menu-own-all': (15, TC1_STRIP + [menu(BLUE, BRICK), menu(BROWN, BRICK), menu(GREEN, BRICK)]),
        'province-menu-cells-full': (6, TC1_STRIP + [menu(CELL, BRICK), menu(GREEN, BRICK)]),
        'command-menu': (1, TC1_STRIP),
    },
    'TC2': {
        'province-menu-own-all': (17, TC2_STRIP + [menu(BRICK, ORANGE), menu(BLUE, ORANGE), menu(BROWN, ORANGE),
                                                   menu(GREEN, ORANGE)]),
        'province-menu-cells-full': (7, TC2_STRIP + [menu(CELL, ORANGE), menu(GREEN, ORANGE)]),
        'command-menu': (4, TC2_STRIP + [menu(TEAL, ITEMS), menu(ITEMS, BROWN)]),
    },
}

# TC3 swaps three colours; each mask is read from the dump as it was, so a swap needs no order.
TC3_STRIP = [whole_strip(BLUE, ORANGE), whole_strip(GREEN, BLUE), whole_strip(ORANGE, GREEN)]
TC3_PANEL = [menu(BLUE, ORANGE), menu(GREEN, BLUE)]
ROUND_2 = {
    'TC3': {
        'command-menu': (6, TC3_STRIP + TC3_PANEL),
        'province-menu-own-all': (19, TC3_STRIP + [menu(ORANGE, GREEN)]),
        'command-menu-building': (6, [whole_strip(busy(BLUE), busy(ORANGE), BUSY_TOPS)] + TC3_STRIP[1:] +
                                  [(busy(BLUE), busy(ORANGE), MENU, BUSY_TOPS), menu(GREEN, BLUE)]),
    },
}


def made(variant):
    """Each dump of variant as the variant draws it, checked for the tags it changes."""
    out = {}
    for name, (want, changes) in variant.items():
        out[name], found = retint(load(name), changes)
        assert len(found) == want, f'{name}: {len(found)} tags changed, {want} expected'
    return out


# ---------------------------------------------------------------------------------------------------------------------
# Sheets
# ---------------------------------------------------------------------------------------------------------------------

def font(name, size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size * S)))


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)


def sheet(cells, columns, title, notes):
    """The images of cells ((caption, image)) in columns under a title, the notes under them."""
    w, h = cells[0][1].size
    gap, head, cap = 24, 64, 34
    rows = (len(cells) + columns - 1) // columns
    note_h = 26 * len(notes) + 20
    out = Image.new('RGB', (columns * w + (columns + 1) * gap, head + rows * (cap + h + gap) + note_h), (14, 11, 9))
    d = ImageDraw.Draw(out)
    plain = lambda size: ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf'), size)
    d.text((gap, 34), title, font=plain(26), fill=NAME, anchor='lm')
    for i, (caption, image) in enumerate(cells):
        x = gap + (i % columns) * (w + gap)
        y = head + (i // columns) * (cap + h + gap)
        d.text((x, y + cap / 2), caption, font=plain(20), fill=QUIET if caption.startswith('Now') else NOTE, anchor='lm')
        out.paste(image, (x, y + cap))
    y = head + rows * (cap + h + gap)
    for i, n in enumerate(notes):
        d.text((gap, y + 13 + 26 * i), n, font=plain(20), fill=QUIET, anchor='lm')
    return out


def round_1_sheets():
    now = {name: load(name) for name in ROUND_1['TC1']}
    notes = {
        'TC1': ['TC1: every command that spends the own city\'s action in its brick (Levy, Build, Plot, Visit, Cell, Look);',
                'Dungeon (and the Ruins Search\'s Search) brown, apart from Merchant\'s teal. The words stay; spent tags stay grey.'],
        'TC2': ['TC2: the own city\'s action a clearer orange, away from Attack\'s crimson, and every command that spends it;',
                'Merchant and the panel\'s Trade gold, Treasure\'s Items brown; Dungeon and Search keep the teal.'],
    }
    for key, variant in ROUND_1.items():
        drawn = made(variant)
        sheet([('Now: Leazas Castle (1)', now['province-menu-own-all']),
               (f'{key}: Leazas Castle (1)', drawn['province-menu-own-all']),
               (f'{key}: the cells', drawn['province-menu-cells-full']),
               (f'{key}: the command panel', drawn['command-menu'])], 2,
              f'The tags by the week\'s use they spend — {key}', notes[key]).save(os.path.join(OUT, f'tags-{key}.png'))
    refill = load('deployment-refill-question')
    sheet([('Now: Refill in the ledger\'s vermilion', refill),
           ('TR1: Refill in Troops\' green', refill_chips(refill, (VERMILION_INK, VERMILION), (GREEN_INK, GREEN)))], 2,
          'The deployment screen\'s Refill, which spends Troops — TR1',
          ['TR1: the idle chips\' rim and figures in green ink, the asked one filled green; the seals stay vermilion.',
           'The Subordinates screen\'s orders have no colour of their own (their Refill column is the costs\' red ink).']
          ).save(os.path.join(OUT, 'tags-TR1.png'))


def round_2_sheets():
    drawn = made(ROUND_2['TC3'])
    sheet([('Now: the command panel', load('command-menu')), ('TC3: the command panel', drawn['command-menu']),
           ('Now: Leazas Castle (1)', load('province-menu-own-all')), ('TC3: Leazas Castle (1)', drawn['province-menu-own-all']),
           ('Now: a building under way', load('command-menu-building')),
           ('TC3: a building under way', drawn['command-menu-building'])], 2,
          'The owner\'s swap of three colours — TC3',
          ['TC3: Construction and the panel\'s Build in the own city\'s orange (half-lit while a building is under way); Troops,',
           'Talk and the panel\'s Units in Construction\'s blue; the own city and every command that spends it in Troops\' green.']
          ).save(os.path.join(OUT, 'tags-TC3.png'))
    refill = load('deployment-refill-question')
    sheet([('Now: Refill in Troops\' green', refill),
           ('TC3: Refill in Troops\' blue', refill_chips(refill, (INK_GREEN, GREEN), (BLUE, BLUE)))], 2,
          'The deployment screen\'s Refill, which spends Troops — TC3',
          ['TC3: the idle chips\' rim and figures in the blue, the asked one filled with it; the seals stay vermilion.']
          ).save(os.path.join(OUT, 'tags-TC3-refill.png'))


# ---------------------------------------------------------------------------------------------------------------------
# The key: every week's use, what spends it, its tags in each column
# ---------------------------------------------------------------------------------------------------------------------

OWN_WORDS = ['Levy', 'Build', 'Plot', 'Visit', 'Cell', 'Look']
OWN_BEFORE = [('Levy', BRICK), ('Build', BLUE), ('Plot', BROWN), ('Visit', GREEN), ('Cell', CELL), ('Look', GREEN)]
SPENT_BY = {
    'Construction': 'Construction (the panel)',
    'Harem': 'Harem (the panel)',
    'Merchant': 'Merchant (the panel)',
    'Troops': 'Subordinates (the panel); Refill on the deployment screen',
    'Own city': 'Temporary Levy, Bolster Defense, the two schemes, a city\'s places, the cells, Look Around; '
                'Go to AL Church at Temple AL HQ (77)',
    'Attack': 'Attack (Pillage in the prologue) and the independent cities\' talks behind it',
    'Dungeon': 'A dungeon\'s Start Search (no tag); the Ruins Search at Ancient Ruins (35)',
    None: 'Treasure; Ninja Maneuver (each ninja\'s own week)',
}
LABELS = {'Troops': 'Troops · Talk', None: 'No week\'s use'}


def same(row):
    return lambda *columns: [row] * len(columns)


def key_rows(columns):
    """The key's rows: (label, strip word, what spends it, one (strip colour, tags) a column)."""
    def row(word, *cells):
        return LABELS.get(word, word), word, SPENT_BY[word], list(cells)

    def own(colour):
        return colour, [(w, colour) for w in OWN_WORDS]

    if columns == ['Now', 'TC1', 'TC2']:
        return [
            row('Construction', *[(BLUE, [('Build', BLUE)])] * 3),
            row('Harem', *[(PLUM, [('Harem', PLUM)])] * 3),
            row('Merchant', (TEAL, [('Trade', TEAL)]), (TEAL, [('Trade', TEAL)]), (ITEMS, [('Trade', ITEMS)])),
            row('Troops', *[(GREEN, [('Units', GREEN)])] * 3),
            row('Own city', (BRICK, OWN_BEFORE), own(BRICK), own(ORANGE)),
            row('Attack', *[(CRIMSON, [('Battle', CRIMSON)])] * 3),
            row('Dungeon', (TEAL, [('Search', TEAL)]), (BROWN, [('Search', BROWN)]), (TEAL, [('Search', TEAL)])),
            row(None, (None, [('Items', ITEMS), ('Ninja', NINJA)]), (None, [('Items', ITEMS), ('Ninja', NINJA)]),
                (None, [('Items', BROWN), ('Ninja', NINJA)])),
        ]
    return [
        row('Construction', (BLUE, [('Build', BLUE)]), (ORANGE, [('Build', ORANGE)])),
        row('Harem', *[(PLUM, [('Harem', PLUM)])] * 2),
        row('Merchant', *[(ITEMS, [('Trade', ITEMS)])] * 2),
        row('Troops', (GREEN, [('Units', GREEN), ('Refill', GREEN)]), (BLUE, [('Units', BLUE), ('Refill', BLUE)])),
        row('Own city', own(ORANGE), own(GREEN)),
        row('Attack', *[(CRIMSON, [('Battle', CRIMSON)])] * 2),
        row('Dungeon', *[(TEAL, [('Search', TEAL)])] * 2),
        row(None, *[(None, [('Items', BROWN), ('Ninja', NINJA)])] * 2),
    ]


def key_sheet(columns, title, notes, file):
    rows = key_rows(columns)
    label_w, desc_w, cell_w, row_h, head = 150, 300, 300, 74, 92
    width = 24 + label_w + desc_w + len(columns) * cell_w + 24
    height = head + len(rows) * row_h + 30 + 16 * len(notes)
    img = Image.new('RGB', (width * S, height * S), (14, 11, 9))
    d = ImageDraw.Draw(img)

    def box(r, fill=None, outline=None, w=1):
        d.rectangle([round(v * S) for v in r], fill=fill, outline=outline, width=max(1, round(w * S)))

    def text(x, y, t, fnt, colour, anchor='lm'):
        d.text((x * S, y * S), t, font=fnt, fill=colour, anchor=anchor)

    def tag(x, y, w, word, colour):
        box((x, y, x + w, y + 24), fill=colour)
        box((x, y, x + w, y + 24), outline=GOLD)
        text(x + w / 2, y + 12.5, word, GOTHIC(12), TEXT, 'mm')

    def wrap(t, fnt, room):
        lines, line = [], ''
        for word in t.split(' '):
            trial = (line + ' ' + word).strip()
            if fnt.getlength(trial) / S > room and line:
                lines.append(line)
                line = word
            else:
                line = trial
        return lines + [line]

    text(24, 30, title, GOTHIC(22), NAME)
    text(24, 58, 'The strip\'s tag first, then the tags of the commands that spend it; a pink rim where a variant changes them.',
         GOTHIC(13), QUIET)
    x_desc, x_cells = 24 + label_w, 24 + label_w + desc_w
    for i, caption in enumerate(columns):
        text(x_cells + i * cell_w + 8, head - 10, caption, GOTHIC(15), NAME)
    for r, (label, word, spent_by, cells) in enumerate(rows):
        y = head + r * row_h
        box((24, y, width - 24, y + row_h - 6), fill=PANEL)
        text(32, y + 20, label, GOTHIC(14), TEXT)
        for j, line in enumerate(wrap(spent_by, GOTHIC(11.5), desc_w - 16)[:4]):
            text(x_desc, y + 14 + 15 * j, line, GOTHIC(11.5), QUIET)
        for i, (strip_colour, tags) in enumerate(cells):
            x = x_cells + i * cell_w + 8
            if i and cells[i] != cells[0]:
                box((x - 5, y + 3, x + cell_w - 12, y + row_h - 9), outline=NOTE, w=1.5)
            cx, cy = x, y + 8
            if word:
                tag(cx, cy, 84, word, strip_colour)
                text(cx + 92, cy + 12, '←', GOTHIC(13), QUIET, 'lm')
                cx += 108
            for w, colour in tags:
                if cx + 52 > x + cell_w - 16:
                    cx, cy = x + (108 if word else 0), cy + 30
                tag(cx, cy, 52, w, colour)
                cx += 56
    y = head + len(rows) * row_h + 14
    for j, n in enumerate(notes):
        text(24, y + 16 * j, n, GOTHIC(12.5), QUIET)
    img.resize((width, height), Image.LANCZOS).save(os.path.join(OUT, file))


if ROUND == 1:
    round_1_sheets()
    key_sheet(['Now', 'TC1', 'TC2'], 'The tags by the week\'s use they spend — the key', [
        'The words stay; only the colours change. Spent tags stay grey and the building\'s half-lit tag follows its colour.',
        'TC1 and TC2 hold two choices that mix: the own city brick (TC1) or orange (TC2); and Dungeon brown, Merchant teal',
        '(TC1), or Merchant gold, Dungeon teal and Treasure brown (TC2). The PC, the tablet\'s rails and the phone\'s sheets',
        'read the same three tables, so all change alike.'], 'tags-key.png')
else:
    round_2_sheets()
    key_sheet(['Now', 'TC3'], 'The owner\'s swap of three colours — the key', [
        'Now is TC2 and TR1 as built. TC3 swaps three colours: Construction takes the own city\'s orange, Troops and Talk',
        'Construction\'s blue, the own city Troops\' green; each command follows the use it spends (TagColours), the',
        'Refill chips too. The PC, the tablet\'s rails and the phone\'s sheets change alike.'], 'tags-key-2.png')
print('written to', OUT)
