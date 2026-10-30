"""Mock-ups of the command tags coloured by the week's use they spend (owner, 2026-10-31; docs/ui/command-screen.md,
"The tags' colours"). The status strip's tags are coloured by the week's flag (StatusStripView.ColourOf), the command
panel's and the province menu's by the kind of command (CommandMenuView.TagOf, ProvinceMenuView.TagOf): of the commands
that spend the own city's action (Ui_CmdOwnCityAvail) only Levy has its colour, and Merchant and Dungeon share one teal.

The dumps: the screen gallery's lacquer pass (tools/ui/gallery.ps1; docs/ui/gallery.md). Then, from the repository's
root (Pillow and numpy; godot/fonts):
  python -I tools/ui/mock_tag_colours.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository: the dumps hold the player's map and CGs.

Round 1. A tag is a fill with its word over it in Text and a gold rim (UiPalette.DrawTag), so a pixel in it is the fill
blended toward the word's colour or the rim's: it keeps its blend over the new fill. TC1: every command that spends the
own city's action in the own city's brick, Dungeon and the Ruins Search in a brown of their own (Plot's, freed), Merchant
teal. TC2: the own city's action a clearer orange, away from Attack's crimson; Merchant and Trade gold, Treasure's Items
brown, Dungeon and Search teal. The two choices mix. TR1: the deployment screen's Refill chips, which spend Troops, in
Troops' green instead of the ledger's vermilion (whose ink is the own city's brick).
"""
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

GALLERY, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2                                   # the key is drawn at twice its pixels, saved at its own

# Lacquer (godot/Strategy/UiPalette.cs).
PANEL, GOLD, DIM_GOLD = (22, 16, 12), (196, 160, 86), (110, 86, 46)
NAME, QUIET, TEXT, OFF_TAG = (232, 199, 122), (176, 160, 130), (244, 232, 206), (52, 46, 40)
PAPER, VERMILION, VERMILION_INK = (224, 208, 172), (170, 40, 28), (150, 52, 30)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)

# The tags' colours now.
BLUE, PLUM, TEAL, GREEN = (46, 62, 104), (128, 44, 70), (36, 92, 96), (70, 96, 52)
BRICK, CRIMSON, BROWN, CELL = (150, 52, 36), (128, 30, 30), (104, 70, 34), (96, 40, 58)
NINJA, ITEMS = (74, 50, 96), (138, 104, 34)
# Ours for the round.
ORANGE = (172, 84, 30)                  # TC2's own city
GREEN_INK = (58, 86, 42)                # TR1's Refill on paper

# The strip's tags in the gallery's dumps (the week-18 save, every use free): their x ranges at the strip's row.
STRIP_Y = (0, 50)
STRIP = {'Merchant': (500, 574), 'Own city': (720, 795), 'Dungeon': (852, 920)}
MENU = (0, 50, 1280, 800)


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
    """The tags of each change (old colour, new colour, box) drawn again in the new colour: a tag is one whose fill of
    exactly the old colour fills most of a tag-sized box inside box (x0, y0, x1, y1); its word and rim keep their blend."""
    a = np.asarray(img).astype(np.float32)
    out = a.copy()
    found = []
    for old, new, (bx0, by0, bx1, by1) in changes:
        f, n = np.array(old, np.float32), np.array(new, np.float32)
        mask = np.zeros(a.shape[:2], bool)
        mask[by0:by1, bx0:bx1] = np.all(np.abs(a[by0:by1, bx0:bx1] - f) <= 1, axis=2)
        for x0, y0, x1, y1, size in components(mask):
            w, h = x1 - x0, y1 - y0
            if size < 150 or not (10 <= h <= 60 and 24 <= w <= 200) or size < 0.4 * w * h:
                continue
            found.append((old, new, (x0, y0, x1, y1)))
            tops = [np.array(TEXT, np.float32), np.array(GOLD, np.float32)]
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


# Each variant's changes per dump. A dump the variant leaves alone is not listed.
TC1_STRIP = [strip('Dungeon', TEAL, BROWN)]
TC2_STRIP = [strip('Own city', BRICK, ORANGE), strip('Merchant', TEAL, ITEMS)]
VARIANTS = {
    'TC1': {
        'province-menu-own-all': TC1_STRIP + [menu(BLUE, BRICK), menu(BROWN, BRICK), menu(GREEN, BRICK)],
        'province-menu-cells-full': TC1_STRIP + [menu(CELL, BRICK), menu(GREEN, BRICK)],
        'command-menu': TC1_STRIP,
    },
    'TC2': {
        'province-menu-own-all': TC2_STRIP + [menu(BRICK, ORANGE), menu(BLUE, ORANGE), menu(BROWN, ORANGE),
                                              menu(GREEN, ORANGE)],
        'province-menu-cells-full': TC2_STRIP + [menu(CELL, ORANGE), menu(GREEN, ORANGE)],
        'command-menu': TC2_STRIP + [menu(TEAL, ITEMS), menu(ITEMS, BROWN)],
    },
}
# The tags each dump must have changed, so that a dump laid out otherwise fails rather than shows half a variant.
EXPECTED = {'province-menu-own-all': 15, 'province-menu-cells-full': 6, 'command-menu': 1}
EXPECTED_TC2 = {'province-menu-own-all': 17, 'province-menu-cells-full': 7, 'command-menu': 4}


# ---------------------------------------------------------------------------------------------------------------------
# The deployment screen's Refill chips (TR1)
# ---------------------------------------------------------------------------------------------------------------------

REFILL = (474, 140, 546, 360)           # the Refill column's chips in deployment-refill-question
REFILL_BG_X = 547                       # a pixel of the row's paper right of the chips


def refill_green(img):
    """The Refill chips in Troops' green: an idle chip is the vermilion ink (its rim at 60 %) over the row's paper, the
    lit one the vermilion fill with the rim's ink and its word in paper; each pixel keeps its blend."""
    a = np.asarray(img).astype(np.float32)
    out = a.copy()
    ink, fill, paper = (np.array(c, np.float32) for c in (VERMILION_INK, VERMILION, PAPER))
    g_ink, g_fill = np.array(GREEN_INK, np.float32), np.array(GREEN, np.float32)
    changed = 0
    x0, y0, x1, y1 = REFILL
    for y in range(y0, y1):
        bg = a[y, REFILL_BG_X]
        for x in range(x0, x1):
            px = a[y, x]
            if px[0] - px[1] < 25:                                     # not reddish: the paper, the dashes
                continue
            pairs = [(bg, ink, bg, g_ink), (fill, paper, g_fill, paper), (fill, ink, g_fill, g_ink), (bg, fill, bg, g_fill)]
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
    d.text((gap, 34), title, font=ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf'), 26),
           fill=NAME, anchor='lm')
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf'), 20)
    for i, (caption, image) in enumerate(cells):
        x = gap + (i % columns) * (w + gap)
        y = head + (i // columns) * (cap + h + gap)
        d.text((x, y + cap / 2), caption, font=f, fill=QUIET if caption.startswith('Now') else NOTE, anchor='lm')
        out.paste(image, (x, y + cap))
    y = head + rows * (cap + h + gap)
    for i, n in enumerate(notes):
        d.text((gap, y + 13 + 26 * i), n, font=f, fill=QUIET, anchor='lm')
    return out


def variant_sheets():
    now = {name: load(name) for name in EXPECTED}
    for key, dumps in VARIANTS.items():
        made = {}
        for name, changes in dumps.items():
            made[name], found = retint(now[name], changes)
            want = (EXPECTED if key == 'TC1' else EXPECTED_TC2)[name]
            assert len(found) == want, f'{key} {name}: {len(found)} tags changed, {want} expected'
        notes = {
            'TC1': ['TC1: every command that spends the own city\'s action in its brick (Levy, Build, Plot, Visit, Cell, Look);',
                    'Dungeon (and the Ruins Search\'s Search) brown, apart from Merchant\'s teal. The words stay; spent tags stay grey.'],
            'TC2': ['TC2: the own city\'s action a clearer orange, away from Attack\'s crimson, and every command that spends it;',
                    'Merchant and the panel\'s Trade gold, Treasure\'s Items brown; Dungeon and Search keep the teal.'],
        }[key]
        sheet([('Now: Leazas Castle (1)', now['province-menu-own-all']),
               (f'{key}: Leazas Castle (1)', made['province-menu-own-all']),
               (f'{key}: the cells', made['province-menu-cells-full']),
               (f'{key}: the command panel', made['command-menu'])], 2,
              f'The tags by the week\'s use they spend — {key}', notes).save(os.path.join(OUT, f'tags-{key}.png'))


def refill_sheet():
    now = load('deployment-refill-question')
    sheet([('Now: Refill in the ledger\'s vermilion', now), ('TR1: Refill in Troops\' green', refill_green(now))], 2,
          'The deployment screen\'s Refill, which spends Troops — TR1',
          ['TR1: the idle chips\' rim and figures in green ink, the asked one filled green; the seals stay vermilion.',
           'The Subordinates screen\'s orders have no colour of their own (their Refill column is the costs\' red ink).']
          ).save(os.path.join(OUT, 'tags-TR1.png'))


# ---------------------------------------------------------------------------------------------------------------------
# The key: every week's use, what spends it, its tags now, in TC1 and in TC2
# ---------------------------------------------------------------------------------------------------------------------

def tc(now, tc1=None, tc2=None):
    return now, tc1 or now, tc2 or now


OWN = [('Levy', BRICK), ('Build', BLUE), ('Plot', BROWN), ('Visit', GREEN), ('Cell', CELL), ('Look', GREEN)]
KEY = [
    ('Construction', 'Construction', 'Construction (the panel)',
     tc((BLUE, [('Build', BLUE)]))),
    ('Harem', 'Harem', 'Harem (the panel)',
     tc((PLUM, [('Harem', PLUM)]))),
    ('Merchant', 'Merchant', 'Merchant (the panel)',
     tc((TEAL, [('Trade', TEAL)]), None, (ITEMS, [('Trade', ITEMS)]))),
    ('Troops · Talk', 'Troops', 'Subordinates (the panel); Refill on the deployment screen (TR1)',
     tc((GREEN, [('Units', GREEN)]))),
    ('Own city', 'Own city', 'Temporary Levy, Bolster Defense, the two schemes, a city\'s places, the cells, Look Around; '
                             'Go to AL Church at Temple AL HQ (77)',
     tc((BRICK, OWN), (BRICK, [(w, BRICK) for w, _ in OWN]), (ORANGE, [(w, ORANGE) for w, _ in OWN]))),
    ('Attack', 'Attack', 'Attack (Pillage in the prologue) and the independent cities\' talks behind it',
     tc((CRIMSON, [('Battle', CRIMSON)]))),
    ('Dungeon', 'Dungeon', 'A dungeon\'s Start Search (no tag); the Ruins Search at Ancient Ruins (35)',
     tc((TEAL, [('Search', TEAL)]), (BROWN, [('Search', BROWN)]))),
    ('No week\'s use', None, 'Treasure; Ninja Maneuver (each ninja\'s own week)',
     tc((None, [('Items', ITEMS), ('Ninja', NINJA)]), None, (None, [('Items', BROWN), ('Ninja', NINJA)]))),
]


def key_sheet():
    label_w, desc_w, cell_w, row_h, head = 150, 300, 300, 74, 92
    width = 24 + label_w + desc_w + 3 * cell_w + 24
    height = head + len(KEY) * row_h + 110
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

    text(24, 30, 'The tags by the week\'s use they spend — the key', GOTHIC(22), NAME)
    text(24, 58, 'The strip\'s tag first, then the tags of the commands that spend it; a pink rim where a variant changes them.',
         GOTHIC(13), QUIET)
    x_desc, x_cells = 24 + label_w, 24 + label_w + desc_w
    for i, caption in enumerate(('Now', 'TC1', 'TC2')):
        text(x_cells + i * cell_w + 8, head - 10, caption, GOTHIC(15), NAME)
    for r, (label, word, spent_by, cells) in enumerate(KEY):
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
            for k, (w, colour) in enumerate(tags):
                if cx + 52 > x + cell_w - 16:
                    cx, cy = x + (108 if word else 0), cy + 30
                tag(cx, cy, 52, w, colour)
                cx += 56
    y = head + len(KEY) * row_h + 14
    for j, n in enumerate([
            'The words stay; only the colours change. Spent tags stay grey and the building\'s half-lit tag follows its colour.',
            'TC1 and TC2 hold two choices that mix: the own city brick (TC1) or orange (TC2); and Dungeon brown, Merchant teal',
            '(TC1), or Merchant gold, Dungeon teal and Treasure brown (TC2). The PC, the tablet\'s rails and the phone\'s sheets',
            'read the same three tables, so all change alike.']):
        text(24, y + 16 * j, n, GOTHIC(12.5), QUIET)
    img.resize((width, height), Image.LANCZOS).save(os.path.join(OUT, 'tags-key.png'))


variant_sheets()
refill_sheet()
key_sheet()
print('written to', OUT)
