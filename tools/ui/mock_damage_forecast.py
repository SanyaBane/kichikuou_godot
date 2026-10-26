"""Mock-ups of the damage forecast at the battle's attack banners (owner's request 2026-10-26; docs/ui/damage-forecast.md):
an option that shows, beside a target's banners, what the attack is likely to do, as Heroes of Might and Magic V does.

The dumps: tools/ui/gallery.ps1 -Touch -Only banners-E1,battle-turn (docs/ui/gallery.md). Then, from the repository's
root (Pillow; godot/fonts; the parts are mock_touch_panel.py's):
  python -I tools/ui/mock_damage_forecast.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository.

The moment is the gallery's battle turn (the capture 20261010-213758-114-FIGT_A, round 1's first FIGT_SEL): Rance (unit 1)
acting, the pointer on Ex (commander 1). The figures are the forecast's own, from the real exchange run 4,000 times on a copy
of that state (docs/ui/damage-forecast.md, §3; the 5th-95th percentiles): Melee — Ex's troops lose 29-37 of 92, Ex falls
(HP 1 to 0) in 78 % of the runs, the counter costs Rance's troops 30-38 of 451, Rance never falls; our hit 15 % (the walls
-8), theirs 39 %.
Made up: Rance Attack, as if Rance had focus (Ex falls in 99.7 % of the runs; no counter).

  A1  on each banner: a second row under the label, the attacks side by side;
  A2  at the scene's foot, where the exchange's counters roll its result: the counters' preview for the hovered banner;
  A3  a tooltip under the hovered banner, in words, with the hit chances;
  T1  the touch mode: on the right rail's attack buttons (no hover there).

Round B (the owner, 2026-10-26: the PC first, A1 without the counter and without a fall's chance — the damage dealt alone,
the troops' and, where the attack can reach him, the commander's), on Ex with a bowman's three banners (made up but Melee)
and on Sakanaku (commander 6) with Melee alone (all his 60 troops, 1-4 of his 4 HP):

  B1  two rows, the label as today and the damage under it (banners 52 px; three stand 20 px over the card);
  B2  two rows within the card (banners 46 px, the label 17);
  B3  one row, the label left and the damage right (banners 40 px as today, the label 16).
"""
import sys, os
sys.dont_write_bytecode = True                  # no __pycache__ in the repository for the borrowed module
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mock_touch_panel import (S, GOLD, DIM_GOLD, TEXT, QUIET, NAME, PANEL, GOTHIC, BRUSH, CINZEL, Frame, load, sc, text,
                              width, note, caption, rect, panel, rule, font)
from PIL import Image, ImageDraw, ImageFilter

# BattleCommandLayer: the banners and the target's frame (layout pixels of the 1280x800 screen).
CARD_E1 = (697, 452, 977, 598)
BANNER_W, BANNER_H, GAP, OUT_, SLANT, POINT, BANNER_TEXT = 200, 40, 5, 22, 12, 18, 20
BANNER_TOP, BANNER_FOOT = (255, 228, 90), (226, 128, 20)
HOT_TOP, HOT_FOOT = (255, 170, 90), (196, 70, 30)
TEXT_OUTLINE = (56, 26, 5)

# The forecast's colours: the enemy's loss red, ours player blue, a fall vermilion on its own chip (style.md, §2).
LOSS_INK = (255, 128, 112)                     # a loss is a fall (style.md, §2: down red), on either side
FALL_FILL, FALL_LINE = (118, 30, 24), (214, 150, 90)
MINT = (158, 219, 158)
OSWALD = lambda size, w=600: font('Oswald-VariableFont_wght.ttf', size, w)


def outlined(f, x, y, t, fnt, colour, outline=TEXT_OUTLINE, anchor='mm', stroke=2):
    lay, d = f.draw()
    d.text((x * S, y * S), t, font=fnt, fill=colour + (255,), anchor=anchor, stroke_width=stroke * S // 2,
           stroke_fill=outline + (240,))
    f.add(lay)


def pennant(r):
    x0, y0, x1, y1 = r
    return [(x0 + SLANT, y0), (x1 - POINT, y0), (x1, (y0 + y1) / 2), (x1 - POINT, y1), (x0, y1)]


def banner(f, r, label, hot, rows=None, label_size=BANNER_TEXT, label_at=None):
    """A banner as BattleCommandLayer.DrawBanner draws it; rows = the forecast's second row (A1), label_at = (x, y,
    anchor) of the label where the forecast moves it (round B)."""
    x0, y0, x1, y1 = r
    lay, d = f.draw()
    d.polygon(sc(*[v for p in pennant((x0 + 2, y0 + 3, x1 + 2, y1 + 3)) for v in p]), fill=(0, 0, 0, 120))
    f.add(lay, blur=3)
    if hot:
        lay, d = f.draw()
        d.polygon(sc(*[v for p in pennant(r) for v in p]), fill=(255, 184, 107, 150))
        f.add(lay, blur=6)
    top, foot = (HOT_TOP, HOT_FOOT) if hot else (BANNER_TOP, BANNER_FOOT)
    grad = Image.new('RGBA', f.img.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(grad)
    for y in range(round(y0 * S), round(y1 * S)):
        t = (y / S - y0) / (y1 - y0)
        gd.line((x0 * S, y, x1 * S, y), fill=tuple(round(a + (b - a) * t) for a, b in zip(top, foot)) + (204,))
    mask = Image.new('L', f.img.size, 0)
    ImageDraw.Draw(mask).polygon(sc(*[v for p in pennant(r) for v in p]), fill=255)
    f.img = Image.composite(Image.alpha_composite(f.img, grad), f.img, mask)
    lay, d = f.draw()
    d.polygon(sc(*[v for p in pennant(r) for v in p]), outline=(255, 255, 255, 255), width=S)
    f.add(lay)
    label_y = (y0 + y1) / 2 if rows is None else y0 + 19
    lx, ly, anchor = label_at or ((x0 + x1 - POINT + SLANT) / 2, label_y, 'mm')
    outlined(f, lx, ly, label, GOTHIC(label_size), (255, 255, 255), anchor=anchor)
    if rows:
        rows(f, x0, y0, x1, y1)


def patch(f, base, r):
    """Puts back the board as it is without banners (the dump of the turn) under r."""
    crop = base.crop(r).resize(((r[2] - r[0]) * S, (r[3] - r[1]) * S), Image.LANCZOS).convert('RGBA')
    f.img.paste(crop, (r[0] * S, r[1] * S))


def fall_chip(f, x, y, t, size=13, anchor='lm'):
    """A fall's chance on a vermilion chip (the cause of a unit's loss, docs/ui/damage-forecast.md §6)."""
    fnt = GOTHIC(size)
    w = width(t, fnt) + 10
    x0 = x if anchor[0] == 'l' else x - w if anchor[0] == 'r' else x - w / 2
    rect(f, (x0, y - size / 2 - 3, x0 + w, y + size / 2 + 3), FALL_FILL, outline=FALL_LINE)
    text(f, x0 + w / 2, y, t, fnt, (255, 236, 220))
    return w


# --- A1: on each banner ---------------------------------------------------------------------------------------------------

def layout_a1(dump, turn):
    f = Frame(dump)
    patch(f, turn, (668, 452, 884, 598))
    h = 58
    top = (CARD_E1[1] + CARD_E1[3]) / 2 - (2 * h + GAP) / 2       # two banners centred on the card (three: 3 x 58 + 10)
    x0 = CARD_E1[0] - OUT_

    def melee_rows(f, x0, y0, x1, y1):
        y = y0 + 42
        outlined(f, x0 + 22, y, '−29–37', GOTHIC(14), (255, 220, 210), anchor='lm')
        fall_chip(f, x0 + 84, y, '78%', 12)
        outlined(f, x1 - POINT - 8, y, '← −30–38', GOTHIC(13), (255, 220, 210), anchor='rm')

    def rance_rows(f, x0, y0, x1, y1):
        y = y0 + 42
        outlined(f, x0 + 22, y, '0', GOTHIC(14), (255, 236, 220), anchor='lm')
        fall_chip(f, x0 + 40, y, '99.7%', 12)
        outlined(f, x1 - POINT - 8, y, 'no counter', GOTHIC(12), (236, 230, 220), anchor='rm')

    banner(f, (x0, top, x0 + BANNER_W, top + h), 'Melee', True, melee_rows)
    banner(f, (x0, top + h + GAP, x0 + BANNER_W, top + 2 * h + GAP), 'Rance Attack', False, rance_rows)
    caption(f, ['A1  On each banner: a second row', 'Ex\'s troops lost · Ex falls (chip) · ← the counter on Rance\'s troops',
                'every attack\'s at once; three banners stand 184 px, 38 over the card\'s 146',
                'Made up: the Rance Attack banner (as if Rance had focus)'], y=62)
    f.save('A1-on-banners.png')


# --- A2: at the scene's foot, the counters' preview ---------------------------------------------------------------------

def counter_side(f, player, troops, loss, hp, fall=None):
    """ExchangeCountersView's layout, quieter: the figure as it stands, the forecast's loss beside it (A2)."""
    edge = 128 + 28 if player else 1152 - 28
    sign = 1 if player else -1
    anchor = 'l' if player else 'r'
    # the gold label and the standing figure (dimmer than the counters' white: nothing has happened yet)
    outlined(f, edge, 306, 'TROOPS', CINZEL(14, 700), (226, 184, 76), anchor=anchor + 'm', stroke=2)
    big = OSWALD(44, 600)
    outlined(f, edge, 342, troops, big, (226, 220, 206), anchor=anchor + 'm', stroke=3)
    x = edge + sign * (width(troops, big) + 10)
    loss_f = OSWALD(28, 600)
    outlined(f, x, 348, loss, loss_f, LOSS_INK, anchor=anchor + 'm', stroke=3)
    x += sign * (width(loss, loss_f) + 16)
    outlined(f, x, 352, 'HP', CINZEL(14, 700), MINT, anchor=anchor + 'm', stroke=2)
    x += sign * (width('HP', CINZEL(14, 700)) + 6)
    outlined(f, x, 350, hp, OSWALD(26, 600), MINT, anchor=anchor + 'm', stroke=3)
    if fall:
        x += sign * (width(hp, OSWALD(26, 600)) + 12)
        fall_chip(f, x, 350, fall, 15, anchor=anchor + 'm')


def layout_a2(dump, turn):
    f = Frame(dump)
    patch(f, turn, (668, 452, 884, 598))
    x0, top = CARD_E1[0] - OUT_, (CARD_E1[1] + CARD_E1[3]) / 2 - (2 * BANNER_H + GAP) / 2
    banner(f, (x0, top, x0 + BANNER_W, top + BANNER_H), 'Melee', True)
    banner(f, (x0, top + BANNER_H + GAP, x0 + BANNER_W, top + 2 * BANNER_H + GAP), 'Rance Attack', False)
    counter_side(f, True, '451', '−30–38', '8')
    counter_side(f, False, '92', '−29–37', '1', 'Falls 78%')
    # the forecast's word at the scene's foot centre, above the order strip
    plate = (560, 336, 720, 366)
    rect(f, plate, PANEL, outline=DIM_GOLD, alpha=215)
    text(f, 640, 351, 'MELEE · LIKELY', CINZEL(13, 700), NAME)
    caption(f, ['A2  At the scene\'s foot, where the exchange\'s counters roll its result', 'the hovered banner\'s forecast in the '
                'counters\' places: the troops as they stand, the likely loss beside them, a fall on its chip',
                'Rance Attack hovered instead: Ex\'s troops −0, "Falls 99.7%", no figure on our side but "No counter"',
                'Made up: the Rance Attack banner'], y=62)
    f.save('A2-scene-foot.png')


# --- A3: a tooltip under the hovered banner ----------------------------------------------------------------------------

def layout_a3(dump, turn):
    f = Frame(dump)
    patch(f, turn, (668, 452, 884, 598))
    x0, top = CARD_E1[0] - OUT_, (CARD_E1[1] + CARD_E1[3]) / 2 - (2 * BANNER_H + GAP) / 2
    banner(f, (x0, top, x0 + BANNER_W, top + BANNER_H), 'Melee', True)
    banner(f, (x0, top + BANNER_H + GAP, x0 + BANNER_W, top + 2 * BANNER_H + GAP), 'Rance Attack', False)
    r = (x0 + 30, top + BANNER_H + 14, x0 + 30 + 400, top + BANNER_H + 14 + 214)
    panel(f, r)
    x, y = r[0] + 18, r[1] + 22
    text(f, x, y, 'MELEE ON EX', CINZEL(15, 700), NAME, anchor='lm')
    rule(f, r[0] + 14, r[2] - 14, y + 16)
    g = GOTHIC(16)
    rows = [("Ex's troops", '−29 to −37', '92 → 55–63', LOSS_INK), ('Ex falls', '78%', 'HP 1', None),
            ("Rance's troops", '−30 to −38', '451 → 413–421', LOSS_INK), ('Rance falls', 'no', 'HP 8', QUIET)]
    y += 38
    for i, (label, figure, aside, ink) in enumerate(rows):
        if i == 2:
            text(f, x, y - 2, 'COUNTER', CINZEL(11, 700), QUIET, anchor='lm')
            y += 18
        text(f, x, y, label, g, TEXT, anchor='lm')
        if ink is None:
            fall_chip(f, x + 150, y, figure, 15)
        else:
            text(f, x + 150, y, figure, g, ink, anchor='lm')
        text(f, r[2] - 18, y, aside, GOTHIC(14), QUIET, anchor='rm')
        y += 24
    text(f, x, r[3] - 16, 'Hit 15% (Walls −8%) · Ex\'s troops hit 39%', GOTHIC(14), QUIET, anchor='lm')
    caption(f, ['A3  A tooltip under the hovered banner, in words', 'as the advantages\' tooltips (AdvantageTip): '
                'instant, gone when the pointer leaves the banner', 'the two sides, the figures before and after, '
                'the hit chances that make them', 'Made up: the Rance Attack banner'], y=62)
    f.save('A3-tooltip.png')


# --- T1: the touch mode's rail buttons -------------------------------------------------------------------------------

def rail_button(f, r, label, lines, hot):
    top, foot = ((238, 200, 116), (186, 140, 60)) if hot else ((88, 68, 40), (58, 44, 26))
    grad = Image.new('RGBA', f.img.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(grad)
    for y in range(round(r[1] * S), round(r[3] * S)):
        t = (y / S - r[1]) / (r[3] - r[1])
        gd.line((r[0] * S, y, r[2] * S, y), fill=tuple(round(a + (b - a) * t) for a, b in zip(top, foot)) + (255,))
    f.img = Image.alpha_composite(f.img, grad)
    rect(f, r, outline=GOLD, w=2)
    ink = (40, 28, 12) if hot else NAME
    cx = (r[0] + r[2]) / 2
    text(f, cx, r[1] + 17, label, BRUSH(11 if len(label) > 8 else 17), ink)
    y = r[1] + 40
    for t, kind in lines:
        if kind == 'fall':
            fall_chip(f, cx, y, t, 12, anchor='mm')
        else:
            text(f, cx, y, t, GOTHIC(13), ink)
        y += 17


def layout_t1(touch):
    f = Frame(touch)
    rect(f, (1150, 610, 1270, 795), (20, 15, 11), alpha=255)
    rail_button(f, (1152, 708, 1268, 790), 'Melee', [('Ex −29–37', 'plain'), ('falls 78%', 'fall'), ('← −30–38', 'plain')],
                True)
    rail_button(f, (1152, 618, 1268, 700), 'Rance Attack', [('Ex −0', 'plain'), ('falls 99.7%', 'fall'),
                                                            ('no counter', 'plain')], False)
    caption(f, ['T1  The touch mode: on the right rail\'s attack buttons', 'no hover on a tablet: each attack button carries its '
                'own forecast,', 'the target chosen first (TouchTargets), the button tapped knowing it',
                'Made up: the Rance Attack button'], y=62, x=150)
    f.save('T1-touch-rail.png')


# --- Round B (the owner, 2026-10-26: A1 on the PC, the damage dealt alone) -----------------------------------------------
# No counter, no fall's chance: the target's troops lost and, where the attack can reach him, its commander's HP lost, each
# in a place of its own on the banner (left empty when the attack cannot reach it). The 5th-95th percentiles, the HP no more
# than the commander has.

CARD_E2 = (697, 610, 977, 756)
FIGURE, FIGURE_TAG = (255, 248, 236), (255, 222, 170)
# A bowman's three banners on Ex (his own attack first, then Melee, then his special); made up but Melee, their figures
# the real exchange's on Rance's state: Bow's streams are Melee's but the counter, Whirlwind Shot takes 1 HP and nothing else.
EX_THREE = [('Bow', '29–37', '0–1'), ('Melee', '29–37', '0–1'), ('Whirlwind Shot', None, '1')]


def target_frame(f, card):
    """BattleCommandLayer.DrawTargetFrame: a cyan L on each corner of the target's card."""
    cyan = (70, 205, 255)
    out, w, ax, ay = 3, 4, 60, 46
    x0, y0, x1, y1 = card[0] - out, card[1] - out, card[2] + out, card[3] + out
    arms = []
    for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        arms.append((min(cx, cx + sx * ax), min(cy, cy + sy * w), max(cx, cx + sx * ax), max(cy, cy + sy * w)))
        arms.append((min(cx, cx + sx * w), min(cy, cy + sy * ay), max(cx, cx + sx * w), max(cy, cy + sy * ay)))
    lay, d = f.draw()
    for a in arms:
        d.rectangle(sc(a[0] - 4, a[1] - 4, a[2] + 4, a[3] + 4), fill=cyan + (150,))
    f.add(lay, blur=5)
    for a in arms:
        rect(f, a, cyan)
        rect(f, (a[0] + 1.5, a[1] + 1.5, a[2] - 1.5, a[3] - 1.5), (255, 255, 255), alpha=215)


def damage(f, y, x_troops, x_hp, troops, hp, size):
    """The damage's figures: Trp. and HP in their own places, each left out where the attack cannot reach it."""
    tag = GOTHIC(size - 2)
    for x, word, value in ((x_troops, 'Trp.', troops), (x_hp, 'HP', hp)):
        if value is None:
            continue
        outlined(f, x, y, word, tag, FIGURE_TAG, anchor='lm')
        outlined(f, x + width(word + ' ', tag), y, value, GOTHIC(size), FIGURE, anchor='lm')


def stack(f, card, items, hot, h, label_size, label_dy, figure_size, figure_dy, x_troops, x_hp, one_row=False):
    top = (card[1] + card[3]) / 2 - (len(items) * h + (len(items) - 1) * GAP) / 2
    x0 = card[0] - OUT_
    for i, (label, troops, hp) in enumerate(items):
        y0 = top + i * (h + GAP)
        r = (x0, y0, x0 + BANNER_W, y0 + h)
        if one_row:
            at = (x0 + SLANT + 6, y0 + h / 2, 'lm')
        else:
            at = ((x0 + x0 + BANNER_W - POINT + SLANT) / 2, y0 + label_dy, 'mm')
        banner(f, r, label, i == hot,
               lambda f, x0, y0, x1, y1, t=troops, p=hp: damage(f, y0 + figure_dy, x0 + x_troops, x0 + x_hp, t, p,
                                                               figure_size),
               label_size=label_size, label_at=at)


def board_e1(dump, turn):
    f = Frame(dump)
    patch(f, turn, (668, 452, 884, 598))
    target_frame(f, CARD_E1)
    return f


MADE_UP_THREE = 'Made up: Bow and Whirlwind Shot, a bowman\'s three banners (the figures the real exchange\'s on Rance\'s state)'


def layout_b1(dump, turn):
    f = board_e1(dump, turn)
    stack(f, CARD_E1, EX_THREE, 1, 52, 20, 17, 15, 38, 20, 112)
    caption(f, ['B1  Two rows: the label as today, the damage under it (banners 52 px)',
                'Trp. the troops lost · HP the commander\'s, only where the attack can reach him',
                'three banners stand 166 px, 20 over the card\'s 146', MADE_UP_THREE], y=62)
    f.save('B1-two-rows.png')


def layout_b2(dump, turn):
    f = board_e1(dump, turn)
    stack(f, CARD_E1, EX_THREE, 1, 46, 17, 15, 14, 33, 20, 108)
    caption(f, ['B2  Two rows within the card: the label 17 instead of 20 (banners 46 px)',
                'Trp. the troops lost · HP the commander\'s, only where the attack can reach him',
                'three banners stand 148 px, the card\'s height', MADE_UP_THREE], y=62)
    f.save('B2-two-rows-fitted.png')


def layout_b3(dump, turn):
    f = board_e1(dump, turn)
    stack(f, CARD_E1, EX_THREE, 1, BANNER_H, 16, 0, 13, BANNER_H / 2, 70, 134, one_row=True)
    caption(f, ['B3  One row: the label left, the damage right (banners 40 px as today, the label 16)',
                'Trp. the troops lost · HP the commander\'s, only where the attack can reach him',
                'the longest names are tight: "T2 Soaring Wave" with both figures does not fit', MADE_UP_THREE], y=62)
    f.save('B3-one-row.png')


def layout_b2_sakanaku(turn):
    f = Frame(turn)
    target_frame(f, CARD_E2)
    stack(f, CARD_E2, [('Melee', '60', '1–4')], 0, 46, 17, 15, 14, 33, 20, 108)
    caption(f, ['B2 on Sakanaku (commander 6): Melee takes all his 60 troops and 1-4 of his 4 HP',
                'the figures the real exchange\'s (5th-95th percentile); the HP no more than he has',
                'the scene would show Sakanaku; the dump of the turn shows no enemy'], y=62)
    f.save('B2-sakanaku.png')


GALLERY = sys.argv[1]
layout_b1(load('lacquer', 'current', 'banners-E1.png'), load('lacquer', 'current', 'battle-turn.png'))
layout_b2(load('lacquer', 'current', 'banners-E1.png'), load('lacquer', 'current', 'battle-turn.png'))
layout_b3(load('lacquer', 'current', 'banners-E1.png'), load('lacquer', 'current', 'battle-turn.png'))
layout_b2_sakanaku(load('lacquer', 'current', 'battle-turn.png'))
layout_a1(load('lacquer', 'current', 'banners-E1.png'), load('lacquer', 'current', 'battle-turn.png'))
layout_a2(load('lacquer', 'current', 'banners-E1.png'), load('lacquer', 'current', 'battle-turn.png'))
layout_a3(load('lacquer', 'current', 'banners-E1.png'), load('lacquer', 'current', 'battle-turn.png'))
layout_t1(load('touch', 'current', 'battle-turn.png'))
