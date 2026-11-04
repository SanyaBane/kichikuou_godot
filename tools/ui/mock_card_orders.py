"""Mock-ups of the touch mode's standing orders at each own card's foot (owner, 2026-10-25: during the player's turn every
card of the player's army offers Auto-Atk or Auto-Def in the room under its state line; docs/ui/touch.md, "The standing
orders on the cards"). Today the touch mode reaches them only by a long press (BattleCommands.ContextMenu(own): Manual,
Auto-Atk, Auto-Def, FIGT_SEL's 50-52).

The dump: tools/ui/gallery.ps1 -Tablet -Only battle-turn (docs/ui/gallery.md). Then, from the repository's root (Pillow;
godot/fonts; the parts are mock_touch_panel.py's):
  python -I tools/ui/mock_card_orders.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository.

Two layouts: A three segments Manual | Atk | Def, the order lit; B two toggles Auto-Atk | Auto-Def, the lit one tapped again
going back to Manual. Both drop the order's word from the state line (the strip shows it) and leave the acting card
without a strip (the left rail's Auto-Atk and Auto-Def hand its turn to the automaton, a different thing). Each image
shows Rick (unit 7)'s card in its three states enlarged over the scene.
"""
import sys, os
sys.dont_write_bytecode = True                  # no __pycache__ in the repository for the borrowed module
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mock_touch_panel import (S, GOLD, DIM_GOLD, TEXT, QUIET, ROW, NOTE, GOTHIC, Frame, load, sc, text, width, note,
                              caption, rect, mm)
from PIL import Image

CARD_FILL, ACTING_FILL = (34, 26, 20), (90, 74, 45)
LIT_TOP, LIT_FOOT, LIT_INK = (238, 200, 116), (186, 140, 60), (40, 28, 12)
STATE_INK = (128, 116, 96)

# The cards in the touch layout (BattleBoardView.CardArea, Narrow): the text column from the face's right to the padding.
TEXT_X0, TEXT_X1 = 459, 596
RANCE = dict(card=(377, 450, 600, 600), state_y=543, strip_y=558, fill=ACTING_FILL, state='Acting')
RICK = dict(card=(377, 608, 600, 758), state_y=701, strip_y=716, fill=CARD_FILL, state='')
STRIP_H = 32


def state_line(f, c):
    """The state line without the order's word: what remains of it, or nothing."""
    rect(f, (TEXT_X0 - 1, c['state_y'] - 10, TEXT_X1 + 1, c['state_y'] + 12), c['fill'])
    if c['state']:
        text(f, TEXT_X0, c['state_y'], c['state'], GOTHIC(17), STATE_INK, anchor='lm')


def gradient(f, r, top, foot):
    lay = Image.new('RGBA', f.img.size, (0, 0, 0, 0))
    x0, y0, x1, y1 = sc(*r)
    for y in range(y0, y1):
        t = (y - y0) / max(1, y1 - y0 - 1)
        colour = tuple(round(a + (b - a) * t) for a, b in zip(top, foot)) + (255,)
        lay.paste(colour, (x0, y, x1, y + 1))
    f.add(lay)


def segment(f, r, label, lit, size=13):
    if lit:
        gradient(f, r, LIT_TOP, LIT_FOOT)
        rect(f, r, outline=GOLD)
    fnt = GOTHIC(size)
    while width(label, fnt) > r[2] - r[0] - 6 and size > 10:
        size -= 1
        fnt = GOTHIC(size)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2 + 1, label, fnt, LIT_INK if lit else QUIET)


def strip_a(f, c, order):
    """A: one bar across the text column, three segments, the order lit."""
    y0, y1 = c['strip_y'], c['strip_y'] + STRIP_H
    rect(f, (TEXT_X0, y0, TEXT_X1, y1), ROW, outline=DIM_GOLD)
    cuts = [TEXT_X0, TEXT_X0 + 55, TEXT_X0 + 96, TEXT_X1]
    for i, (label, key) in enumerate([('Manual', 'manual'), ('Atk', 'attack'), ('Def', 'defend')]):
        r = (cuts[i] + 1, y0 + 1, cuts[i + 1] - 1, y1 - 1)
        segment(f, r, label, order == key)
        if i > 0 and order not in (key, ['manual', 'attack', 'defend'][i - 1]):
            rect(f, (cuts[i], y0 + 5, cuts[i], y1 - 5), outline=DIM_GOLD)


def strip_b(f, c, order):
    """B: two toggles side by side, the order's lit; Manual is neither lit."""
    y0, y1 = c['strip_y'], c['strip_y'] + STRIP_H
    gap = 5
    w = (TEXT_X1 - TEXT_X0 - gap) / 2
    for i, (label, key) in enumerate([('Auto-Atk', 'attack'), ('Auto-Def', 'defend')]):
        x = TEXT_X0 + i * (w + gap)
        r = (x, y0, x + w, y1)
        rect(f, r, ROW, outline=GOLD if order == key else DIM_GOLD)
        segment(f, (r[0] + 1, r[1] + 1, r[2] - 1, r[3] - 1), label, order == key, size=14)


def board(strip, rick_order):
    f = Frame(load('tablet', 'current', 'battle-turn.png'))
    state_line(f, RANCE)
    state_line(f, RICK)
    strip(f, RICK, rick_order)
    return f


def enlarged(f, states, strip, labels):
    """Rick's card in each state, 1.5x, side by side over the scene."""
    x, y, k = 128, 100, 1.5
    c = RICK['card']
    box = sc(c[0] - 3, c[1] - 3, c[2] + 3, c[3] + 3)
    for order, label in zip(states, labels):
        part = board(strip, order).img.crop(box)
        w, h = round(part.width * k / 2), round(part.height * k / 2)
        part = part.resize((w * S, h * S), Image.LANCZOS)
        rect(f, (x - 3, y - 3, x + w + 3, y + h + 3), (0, 0, 0), outline=GOLD)
        f.img.paste(part, (round(x * S), round(y * S)))
        note(f, x + w / 2, y + h + 14, label, anchor='mm', size=14)
        x += w + 18


def finger(f, x, y):
    lay, d = f.draw()
    d.ellipse(sc(x - 14, y - 14, x + 14, y + 14), fill=NOTE + (90,), outline=NOTE + (230,), width=2 * S)
    f.add(lay)


def layout_a():
    f = board(strip_a, 'manual')
    finger(f, TEXT_X0 + 96 - 4, RICK['strip_y'] + STRIP_H + 4)
    enlarged(f, ['manual', 'attack', 'defend'], strip_a, ['Manual (today\'s order)', 'tapped Atk', 'tapped Def'])
    caption(f, ['A — three segments under the state line: Manual | Atk | Def, the unit\'s order lit in gold',
                f'137x{STRIP_H} across the text column; Manual 55, Atk and Def 41 wide = {mm(55)} / {mm(41)} x {mm(STRIP_H)};',
                'one tap gives the order (FIGT_SEL 50-52, as the long press\'s menu); the order\'s word leaves the state line;',
                'no strip on the acting card (Rance): the left rail\'s Auto-Atk / Auto-Def hand its turn over, not its order'],
            y=8, x=140)
    f.save('A-three-segments.png')


def layout_b():
    f = board(strip_b, 'attack')
    finger(f, TEXT_X0 + 62, RICK['strip_y'] + STRIP_H + 4)
    enlarged(f, ['manual', 'attack', 'defend'], strip_b, ['Manual: neither lit', 'Auto-Atk lit', 'Auto-Def lit'])
    caption(f, ['B — two toggles under the state line: Auto-Atk | Auto-Def, the order\'s lit, Manual when neither is',
                f'66x{STRIP_H} each = {mm(66)} x {mm(STRIP_H)}; a tap on the unlit one gives that order, a tap on the lit one',
                'gives Manual back (FIGT_SEL 50-52); the order\'s word leaves the state line;',
                'no strip on the acting card (Rance): the left rail\'s Auto-Atk / Auto-Def hand its turn over, not its order'],
            y=8, x=140)
    f.save('B-two-toggles.png')


layout_a()
layout_b()
