"""Mock-ups of the commander's HP in the phone ledger's Name cell (owner, 2026-10-31; docs/ui/phone.md, "Round P29").

The Name cell on the phone shows the unit's face and a bar of its troops without figures: held landscape under the name,
upright under the name and the type (phone.md, §35). A commander's HP comes back only 2 a week from stage 1
(MAIN.ADV:134-167) and a commander at 0 falls, but the ledger shows HP only in the Commander tab; these draw it in the
Name cell too, under the troops' bar, in a colour of its own (an indigo ink: the troops' bar is green, amber or red).

The dumps: the screen gallery's phone passes (tools/ui/gallery.ps1 -PhoneLand -PhonePortrait -Only deployment,subordinates;
docs/ui/gallery.md), after the Name cell's build. Then, from the repository's root (Pillow and numpy; godot/fonts):
  python -I tools/ui/mock_hp_bar.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository: the dumps hold the player's CGs.

Round P29, over deployment and subordinates (the week-18 save's Overview tab, held landscape and upright): nobody is
wounded on the save, so the HP are made up — Rance (unit 1) 6/8, Rick (unit 7) 3/8, Kinkaid (unit 9) 5/7, Haurein
(unit 12) 1/5, the rest full at the Commander tab's maximum. H1 a bar as long as the troops' under it, thinner; H2 a pip
a point of HP, filled for the HP left (the agent's recommendation); H3 H2's pips only while the commander is wounded.

Round P30 (the owner: the battle's colours, no pips): H4 the troops' bar under the name in the battle's blue (our side's,
BattleBoardView.PlayerColour) and the HP's bar under it in the battle's green (HpColour), as thick as each other as on a
battle card; the Troops column keeps its green, amber and red. Drawn beside Now and H2.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

GALLERY, OUT = sys.argv[1:3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)

PX_MM = 446 / 25.4                      # the Poco X6 Pro: 17.6 reference pixels a millimetre
NOTE = (255, 92, 160)                   # the mock-ups' own notes

# The ledger's paper (godot/Strategy/UiPalette.cs, lacquer): the troops' bar's colours, the paper's rule under a bar.
TROOP_BARS = [(70, 110, 50), (160, 110, 30), (170, 50, 36)]
RULE, RULE_ALPHA = (150, 120, 80), 110 / 255
HP_INK = (44, 70, 128)                  # the HP's own colour: an indigo ink, apart from the troops' three

# The rows in the dumps' order (by number) and their HP: the maximum as the Commander tab shows it on the week-18 save,
# the HP left made up (nobody is wounded on the save).
UNITS = [1, 5, 6, 7, 8, 9, 10, 11, 12]
MAX_HP = {1: 8, 5: 5, 6: 5, 7: 8, 8: 9, 9: 7, 10: 4, 11: 1, 12: 5}
HP = {**MAX_HP, 1: 6, 7: 3, 9: 5, 12: 1}
MADE_UP = 'HP made up (nobody is wounded on the save): Rance 6/8, Rick 3/8, Kinkaid 5/7, Haurein 1/5, the rest full'

# The troops as the dumps show them (the week-18 save), for the bar drawn again in H4.
TROOPS = {1: (1000, 1000), 5: (800, 800), 6: (980, 1000), 7: (612, 800), 8: (1000, 1000), 9: (901, 1500), 10: (649, 1250),
          11: (100, 100), 12: (209, 1400)}

# The battle's gauges (godot/Battles/BattleBoardView.cs): our side's troops, the HP.
BATTLE_TROOPS = (69, 117, 219)          # PlayerColour (0.27, 0.46, 0.86)
BATTLE_HP = (107, 194, 107)             # HpColour (0.42, 0.76, 0.42)


def font(name, size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size)))


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)


def mm(px):
    return f'{px / PX_MM:.1f} mm'


class Held:
    """How the phone is held: the pass's folder, the reference size, the troops' bar's thickness, the HP's sizes."""

    def __init__(self, folder, size, bar, search, gap, line, pip, pip_gap, battle_gap):
        self.folder, self.size, self.bar, self.search = folder, size, bar, search
        self.gap, self.line, self.pip, self.pip_gap, self.battle_gap = gap, line, pip, pip_gap, battle_gap


# Held landscape the dump is two thirds of the reference (2712 × 1220), upright half of it (1220 × 2712). The troops'
# bar is the Troops cell's held landscape (about 5 px), PhoneLedgerPortrait.BarHeight upright (8).
LAND = Held('phone-landscape', (2712, 1220), bar=5, search={'deployment': (400, 560), 'subordinates': (730, 890)},
            gap=4, line=6, pip=10, pip_gap=3, battle_gap=3)
UP = Held('phone-portrait', (1220, 2712), bar=8, search={'deployment': (280, 530), 'subordinates': (280, 530)},
          gap=4, line=6, pip=10, pip_gap=4, battle_gap=4)


def base(held, dump):
    path = os.path.join(GALLERY, held.folder, 'current', dump + '.png')
    return Image.open(path).convert('RGB').resize(held.size, Image.LANCZOS)


def troop_bars(img, held, dump):
    """The troops' bars under the names, a row each in the rows' order: (left, right, bottom) in reference pixels."""
    x0, x1 = held.search[dump]
    a = np.asarray(img).astype(int)
    mask = np.zeros(a.shape[:2], bool)
    for c in TROOP_BARS:
        mask |= np.abs(a - np.array(c)).sum(axis=2) < 24
    region = mask[:, x0:x1]
    ys = np.where(region.sum(axis=1) > 6)[0]
    bands = []
    for y in ys:
        if bands and y - bands[-1][1] <= 3:
            bands[-1][1] = y
        else:
            bands.append([y, y])
    lefts = [x0 + np.where(region[b0:b1 + 1].any(axis=0))[0].min() for b0, b1 in bands]
    left = int(np.median(lefts))
    rows = [(b0, b1) for (b0, b1), l in zip(bands, lefts) if abs(l - left) <= 4]   # not a party card's bar
    right = max(x0 + np.where(region[b0:b1 + 1].any(axis=0))[0].max() for b0, b1 in rows)
    if len(rows) != len(UNITS):
        raise SystemExit(f'{dump}: {len(rows)} troops\' bars found, {len(UNITS)} expected')
    return [(left, right + 1, round((b0 + b1) / 2 + held.bar / 2)) for b0, b1 in rows]


def blend(img, box, colour, alpha):
    x0, y0, x1, y1 = box
    region = img.crop(box)
    img.paste(Image.blend(region, Image.new('RGB', region.size, colour), alpha), (x0, y0))


def line(img, box, hp, max_hp, ink=HP_INK):
    """H1: the paper's rule as long as the troops' bar, the HP left in the HP's ink from its left."""
    x0, y0, x1, y1 = box
    blend(img, box, RULE, RULE_ALPHA)
    w = max(1, round((x1 - x0) * hp / max_hp))
    ImageDraw.Draw(img).rectangle((x0, y0, x0 + w - 1, y1 - 1), fill=ink)


def battle_bars(img, held, unit, left, right, bottom):
    """H4: the troops' bar under the name drawn again in the battle's blue, the HP's under it in the battle's green."""
    top = bottom - held.bar
    under = img.getpixel((right - 3, bottom + 3))                   # the row's paper or tint, clear of the bar
    ImageDraw.Draw(img).rectangle((left - 2, top - 2, right + 1, bottom + 1), fill=under)
    troops, max_troops = TROOPS[unit]
    line(img, (left, top, right, bottom), troops, max_troops, BATTLE_TROOPS)
    hp_top = bottom + held.battle_gap
    line(img, (left, hp_top, right, hp_top + held.bar), HP[unit], MAX_HP[unit], BATTLE_HP)


def pips(img, box, hp, max_hp, gap):
    """H2: a pip a point of HP across the troops' bar's length, those left filled in the HP's ink, the rest the rule."""
    x0, y0, x1, y1 = box
    w = (x1 - x0 - (max_hp - 1) * gap) / max_hp
    for i in range(max_hp):
        a, b = round(x0 + i * (w + gap)), round(x0 + i * (w + gap) + w)
        if i < hp:
            ImageDraw.Draw(img).rectangle((a, y0, b - 1, y1 - 1), fill=HP_INK)
        else:
            blend(img, (a, y0, b, y1), RULE, RULE_ALPHA)


def variant(kind):
    def draw(held, dump):
        img = base(held, dump)
        if kind == 'now':
            return img
        for unit, (left, right, bottom) in zip(UNITS, troop_bars(img, held, dump)):
            hp, max_hp = HP[unit], MAX_HP[unit]
            top = bottom + held.gap
            if kind == 'battle':
                battle_bars(img, held, unit, left, right, bottom)
            elif kind == 'line':
                line(img, (left, top, right, top + held.line), hp, max_hp)
            elif kind == 'pips' or (kind == 'wounded' and hp < max_hp):
                pips(img, (left, top, right, top + held.pip), hp, max_hp, held.pip_gap)
        return img
    return draw


def notes(held, kind):
    if kind == 'now':
        return ['as built: the face, the name, the troops\' bar under it; HP only in the Commander tab']
    if kind == 'line':
        return [f'a bar of the HP in an indigo ink, {held.line} px ({mm(held.line)}) high, {held.gap} px under the '
                'troops\' bar and as long; the HP left from its left', MADE_UP]
    common = (f'pips {held.pip} px ({mm(held.pip)}) high, {held.pip_gap} px apart, {held.gap} px under the troops\' bar '
              'and as long as it: a pip a point of HP, the HP left filled in an indigo ink')
    if kind == 'pips':
        return [common + '; "6 of 8" read at a glance, and the pips never look like the troops\' bar', MADE_UP]
    if kind == 'battle':
        return [f'the battle card\'s colours: the troops\' bar under the name in our side\'s blue, the HP\'s under it in the '
                f'battle\'s green, both {held.bar} px ({mm(held.bar)}) high, {held.battle_gap} px apart, as long '
                'as each other; the Troops column keeps its green, amber and red', MADE_UP]
    return [common + ', only while the commander is wounded: a whole commander\'s row stays as now', MADE_UP]


VARIANTS = [('Now', 'now'), ('H1 — a bar of the HP', 'line'), ('H2 — a pip a point (recommended)', 'pips'),
            ('H3 — pips while wounded', 'wounded')]


def wrapped(d, text, face, width):
    words, lines = text.split(' '), ['']
    for w in words:
        trial = (lines[-1] + ' ' + w).strip()
        if d.textlength(trial, font=face) <= width or not lines[-1]:
            lines[-1] = trial
        else:
            lines.append(w)
    return lines


def sheet(held, dump, name, title, cols, scale, crop=None, variants=VARIANTS):
    """The variants in a grid of cols, each scaled (after crop, reference pixels), with their labels and notes."""
    drawn = []
    for label, kind in variants:
        im = variant(kind)(held, dump)
        if crop:
            im = im.crop(crop)
        drawn.append((label, im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS), notes(held, kind)))
    w, h = drawn[0][1].size
    gap, head, foot = 24, 110, 150
    rows = (len(drawn) + cols - 1) // cols
    out = Image.new('RGB', (cols * w + (cols + 1) * gap, head + rows * (h + foot + 40)), (40, 40, 40))
    d = ImageDraw.Draw(out)
    d.text((gap, 18), title, font=GOTHIC(34), fill=(255, 255, 255))
    for i, (label, im, ns) in enumerate(drawn):
        x, y = gap + i % cols * (w + gap), head + i // cols * (h + foot + 40)
        d.text((x, y - 14), label, font=GOTHIC(26), fill=(255, 255, 255), anchor='ld')
        out.paste(im, (x, y))
        y += h + 12
        for n in ns:
            for t in wrapped(d, n, GOTHIC(20), w):
                d.text((x, y), t, font=GOTHIC(20), fill=NOTE)
                y += 26
            y += 6
    out.save(os.path.join(OUT, name + '.png'))
    print(os.path.join(OUT, name + '.png'), out.size)


sheet(LAND, 'deployment', 'p29-land', 'Round P29 — the commander\'s HP in the Name cell, held landscape: the week-18 '
      'save\'s attack on Oaks (6)', cols=2, scale=0.5)
sheet(LAND, 'deployment', 'p29-land-detail', 'Round P29 — held landscape, the ledger at the phone\'s pixels: the faces, '
      'the names and the Troops column', cols=2, scale=1, crop=(117, 190, 1260, 1250))
sheet(LAND, 'subordinates', 'p29-land-subordinates', 'Round P29 — held landscape: Subordinates, Rance chosen', cols=2,
      scale=0.5)
sheet(UP, 'deployment', 'p29-up', 'Round P29 — the commander\'s HP in the Name cell, upright: the week-18 save\'s attack '
      'on Oaks (6)', cols=4, scale=0.5)
sheet(UP, 'deployment', 'p29-up-detail', 'Round P29 — upright, the ledger at the phone\'s pixels: the fixed part and the '
      'Troops column', cols=4, scale=0.75, crop=(0, 1130, 900, 2480))
sheet(UP, 'subordinates', 'p29-up-subordinates', 'Round P29 — upright: Subordinates, Rance chosen', cols=4, scale=0.5)

P30 = [('Now', 'now'), ('H2 — a pip a point (round P29)', 'pips'), ('H4 — the battle\'s colours', 'battle')]
sheet(LAND, 'deployment', 'p30-land-detail', 'Round P30 — held landscape, the ledger at the phone\'s pixels: the battle\'s '
      'colours beside H2', cols=3, scale=0.8, crop=(117, 190, 1260, 1250), variants=P30)
sheet(LAND, 'subordinates', 'p30-land-subordinates', 'Round P30 — held landscape: Subordinates, Rance chosen', cols=3,
      scale=0.5, variants=P30)
sheet(UP, 'deployment', 'p30-up-detail', 'Round P30 — upright, the ledger at the phone\'s pixels: the battle\'s colours '
      'beside H2', cols=3, scale=0.75, crop=(0, 1130, 900, 2480), variants=P30)
sheet(UP, 'subordinates', 'p30-up-subordinates', 'Round P30 — upright: Subordinates, Rance chosen', cols=3, scale=0.5,
      variants=P30)
