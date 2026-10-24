"""Mock-ups of the enemy attack screen's army cards (owner, 2026-10-24): the deployment screen's cards for the enemy.

The owner: the army's cards (the scout report's, grey and large) sit badly on the lacquer; they should look like our
party's cards on the deployment screen, with a bracket and its arrowhead left of them (the enemy's front row faces us).
Draws variants of the army panel over a screen dump of our enemy attack screen (a window's dump, 1280x824 with the menu
bar's 24 pixels on top, the army scouted): the panel under its heading is painted over and drawn again, the commanders'
faces taken from the dump's own cards. The images stay outside the repository.

    python tools/ui/mock_enemy_cards.py <scouted attack dump.png> <out.png>

The week-18 save's --scout look (CLAUDE.md, "Commands") dumps such a screen as enemy-attack-back: the Leazas Rebel Army
at Ys (5), Ex (commander 1), Sakanaku (commander 6), Dodge (commander 5) and Jibul (commander 7).
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
BRUSH = REPO + '/godot/fonts/Shojumaru-Regular.ttf'

# Lacquer's colours (godot/Strategy/Lacquer.cs)
GOLD = (196, 160, 86)
DIM_GOLD = (110, 86, 46)
NAME = (232, 199, 122)
LABEL = (150, 134, 108)
VALUE = (236, 226, 204)
TEXT = (244, 232, 206)
QUIET = (176, 160, 130)
ROW = (34, 26, 20)
OFF_TEXT = (98, 88, 74)
OFF_LINE = (66, 58, 48)
BADGE = (90, 66, 34)
VERMILION = (170, 40, 28)
RED_LINE = (120, 46, 34)
RED_NAME = (226, 120, 96)

BAR = 24
PANEL = (600, 116, 1248, 612)                 # EnemyAttackView.ArmyPanel in the 1280x800 screen
OLD_CARD = (282, 150)


def old_card(slot):
    return 632 + (slot - 1) // 2 * 300, 170 + (slot - 1) % 2 * 166


# The army: slot, name, troop type, troops (the dump's report).
ARMY = {1: ('Ex', 'Leazas Knights', 240), 2: ('Sakanaku', 'Leazas Soldiers', 970),
        3: ('Dodge', 'Leazas Soldiers', 486), 4: ('Jibul', 'Leazas Soldiers', 400)}


def font(path, size):
    return ImageFont.truetype(path, size)


GOTHIC13, GOTHIC14, GOTHIC15, GOTHIC17, GOTHIC18 = (font(GOTHIC, n) for n in (13, 14, 15, 17, 18))
GOTHIC22 = font(GOTHIC, 22)
CINZEL11, CINZEL12, CINZEL13, CINZEL22 = (font(CINZEL, n) for n in (11, 12, 13, 22))
BRUSH30 = font(BRUSH, 30)


def local(x, y):
    return round(x - PANEL[0]), round(y - PANEL[1])


def box(x0, y0, x1, y1):
    return (*local(x0, y0), *local(x1, y1))


def faces(dump):
    """Each commander's face as the dump's report card shows it (118 square, 8 from the card's right, centred)."""
    out = {}
    for slot in ARMY:
        x, y = old_card(slot)
        fx, fy = x + OLD_CARD[0] - 8 - 118, y + (OLD_CARD[1] - 118) // 2 + BAR
        out[slot] = dump.crop((fx, fy, fx + 118, fy + 118)).convert('RGBA')
    return out


def cleared(dump):
    panel = dump.crop((PANEL[0], PANEL[1] + BAR, PANEL[2], PANEL[3] + BAR)).convert('RGBA')
    fill = panel.getpixel(local(PANEL[0] + 30, PANEL[3] - 30))
    ImageDraw.Draw(panel).rectangle(box(PANEL[0] + 16, PANEL[1] + 34, PANEL[2] - 16, PANEL[3] - 16), fill=fill)
    return panel, fill


def dashed(d, rect, colour, dash=6, gap=4):
    x0, y0, x1, y1 = rect
    for a, b, horizontal, at in ((x0, x1, True, y0), (x0, x1, True, y1), (y0, y1, False, x0), (y0, y1, False, x1)):
        p = a
        while p < b:
            q = min(p + dash, b)
            d.line(box(p, at, q, at) if horizontal else box(at, p, at, q), fill=colour, width=1)
            p = q + gap


def badge(d, cx, cy, number, line=GOLD, fill=BADGE, text=NAME):
    r = 10
    d.polygon([local(cx, cy - r), local(cx + r, cy), local(cx, cy + r), local(cx - r, cy)], fill=fill, outline=line)
    d.text(local(cx, cy + 1), str(number), font=CINZEL12, fill=text, anchor='mm')


def right_text(d, right, baseline, text, f, colour):
    x = right - d.textlength(text, font=f)
    d.text(local(x, baseline), text, font=f, fill=colour, anchor='ls')
    return x


# ---------------------------------------------------------------- the layout

class Layout:
    """Where the cards and the bracket go: two columns, front row (1, 2) next to the bracket on the left."""

    def __init__(self, card_w=252, card_h=104, gap=8):
        self.w, self.h, self.gap = card_w, card_h, gap
        # the headers, the cards and the totals centred in the panel under its heading
        free_top, free_bottom = PANEL[1] + 34, PANEL[3] - 16
        block = 24 + 2 * card_h + gap + 80
        self.top = free_top + (free_bottom - free_top - block) / 2 + 24
        strip, strip_gap, col_gap = 30, 6, 6
        whole = strip + strip_gap + card_w + col_gap + card_w
        self.strip_x0 = PANEL[0] + (PANEL[2] - PANEL[0] - whole) / 2
        self.front_x = self.strip_x0 + strip + strip_gap
        self.back_x = self.front_x + card_w + col_gap

    def card(self, slot):
        x = self.front_x if slot in (1, 2) else self.back_x
        y = self.top + (0 if slot in (1, 3) else self.h + self.gap)
        return x, y, x + self.w, y + self.h

    @property
    def bottom(self):
        return self.top + 2 * self.h + self.gap


def bracket(d, lay, colour=GOLD, head=NAME):
    """DeploymentPanels.DrawFacing mirrored: the front row's two cards held from the left, the head pointing at us."""
    top, bottom = lay.card(1), lay.card(2)
    x = lay.front_x - 6 - 1
    ends = top[0] - 2
    tip = lay.strip_x0 - 4
    y0, y1, cy = top[1] + 1, bottom[3] - 1, (top[3] + bottom[1]) / 2
    for arm in ([(ends, y0), (x, y0), (x, cy - 16), (x - 10, cy - 6)], [(ends, y1), (x, y1), (x, cy + 16), (x - 10, cy + 6)]):
        d.line([local(*p) for p in arm], fill=colour, width=3, joint='curve')
    d.polygon([local(x - 6, cy - 14), local(tip, cy), local(x - 6, cy + 14)], fill=head)


def headers(d, lay):
    y = lay.top - 12
    for x0, label, arrow in ((lay.front_x, 'Front row', True), (lay.back_x, 'Back row', False)):
        cx = x0 + lay.w / 2
        w = d.textlength(label, font=CINZEL13)
        d.text(local(cx, y), label, font=CINZEL13, fill=LABEL, anchor='ms')
        if arrow:
            ax = cx - w / 2 - 10
            d.polygon([local(ax + 4, y - 9), local(ax - 2, y - 5), local(ax + 4, y - 1)], fill=LABEL)


def totals(d, lay, known=True, commanders=4, troops=2096, note="Kanami's report"):
    y = lay.bottom + 40
    left = lay.front_x
    d.text(local(left, y), 'Commanders', font=CINZEL12, fill=LABEL, anchor='ls')
    lw = d.textlength('Commanders', font=CINZEL12)
    d.text(local(left + lw + 10, y), str(commanders) if known else '??', font=GOTHIC17, fill=VALUE if known else OFF_TEXT, anchor='ls')
    figure = f'{troops:,}' if known else '??'
    fx = right_text(d, lay.back_x + lay.w, y, figure, GOTHIC17, VALUE if known else OFF_TEXT)
    right_text(d, fx - 10, y, 'Total troops', CINZEL12, LABEL)
    d.line(box(left, y + 14, lay.back_x + lay.w, y + 14), fill=DIM_GOLD, width=1)
    d.text(local(left, y + 36), note, font=GOTHIC13, fill=QUIET, anchor='ls')


def paste_face(panel, face, rect):
    x0, y0, x1, y1 = rect
    size = round(x1 - x0)
    panel.alpha_composite(face.resize((size, size), Image.LANCZOS), local(x0, y0))


# ---------------------------------------------------------------- the cards

def card_ours(panel, d, lay, slot, face, mirrored=False, enemy=False):
    """Our party's card (DeploymentPanels.DrawCard) without × and Cost: face, place, name, type, troops."""
    x0, y0, x1, y1 = lay.card(slot)
    name, kind, troops = ARMY[slot]
    d.rectangle(box(x0, y0, x1, y1), fill=ROW, outline=RED_LINE if enemy else DIM_GOLD)
    size = lay.h - 16
    fx = x1 - 8 - size if mirrored else x0 + 8
    face_rect = (fx, y0 + 8, fx + size, y0 + 8 + size)
    paste_face(panel, face, face_rect)
    d.rectangle(box(*face_rect), outline=GOLD)
    tx = x0 + 12 if mirrored else face_rect[2] + 12
    right = face_rect[0] - 12 if mirrored else x1 - 12
    if enemy:
        badge(d, tx + 10, y0 + 19, slot, line=VERMILION, fill=(60, 22, 16), text=RED_NAME)
    else:
        badge(d, tx + 10, y0 + 19, slot)
    d.text(local(tx + 28, y0 + 25), name, font=GOTHIC18, fill=NAME, anchor='ls')
    d.text(local(tx, y0 + 47), kind, font=GOTHIC14, fill=QUIET, anchor='ls')
    big = lay.h > 110
    base = y1 - (16 if big else 14)
    d.text(local(tx, base), 'Troops', font=CINZEL11, fill=LABEL, anchor='ls')
    if big:
        right_text(d, right, base + 2, f'{troops:,}', GOTHIC22, VALUE)
    else:
        right_text(d, right, base, f'{troops:,}', GOTHIC17, VALUE)
    d.line(box(tx, base + 5, right, base + 5), fill=DIM_GOLD, width=1)


def card_unknown(d, lay, slot):
    """Not scouted: our empty place's dashes, the place's number, a '?' where the face goes."""
    x0, y0, x1, y1 = lay.card(slot)
    d.rectangle(box(x0, y0, x1, y1), fill=ROW + (150,))
    dashed(d, (x0, y0, x1, y1), DIM_GOLD)
    size = lay.h - 16
    face = (x0 + 8, y0 + 8, x0 + 8 + size, y0 + 8 + size)
    dashed(d, face, OFF_LINE, 4, 4)
    d.text(local((face[0] + face[2]) / 2, (face[1] + face[3]) / 2 + 4), '?', font=BRUSH30, fill=DIM_GOLD, anchor='mm')
    tx = face[2] + 12
    badge(d, tx + 10, y0 + 19, slot, line=DIM_GOLD, fill=ROW, text=DIM_GOLD)
    d.text(local(tx + 28, y0 + 25), 'Unknown', font=CINZEL13, fill=OFF_TEXT, anchor='ls')
    d.text(local(tx, y1 - 14), 'Troops', font=CINZEL11, fill=OFF_TEXT, anchor='ls')
    right_text(d, x1 - 12, y1 - 14, '??', GOTHIC17, OFF_TEXT)


def card_empty(d, lay, slot):
    """No commander in the place: our empty place exactly."""
    x0, y0, x1, y1 = lay.card(slot)
    d.rectangle(box(x0, y0, x1, y1), fill=ROW + (150,))
    dashed(d, (x0, y0, x1, y1), DIM_GOLD)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    d.text(local(cx, cy + 2), str(slot), font=CINZEL22, fill=DIM_GOLD, anchor='ms')
    d.text(local(cx, cy + 24), 'EMPTY', font=CINZEL12, fill=DIM_GOLD, anchor='ms')


# ---------------------------------------------------------------- the variants

def variant(dump, lay, draw_card, colour=GOLD, head=NAME, known=True, empty=(), **totals_args):
    panel, _ = cleared(dump)
    face = faces(dump)
    d = ImageDraw.Draw(panel, 'RGBA')
    headers(d, lay)
    for slot in ARMY:
        if slot in empty:
            card_empty(d, lay, slot)
        elif not known:
            card_unknown(d, lay, slot)
        else:
            draw_card(panel, d, lay, slot, face[slot])
    bracket(d, lay, colour, head)
    totals(d, lay, known=known, **totals_args)
    return panel


def e1(dump):
    """E1: our party's cards as they are, the bracket in gold."""
    return variant(dump, Layout(), card_ours)


def e2(dump):
    """E2: E1 in the enemy's colour: vermilion badges and frame, the bracket in vermilion."""
    return variant(dump, Layout(), lambda p, d, l, s, f: card_ours(p, d, l, s, f, enemy=True), VERMILION, RED_NAME)


def e3(dump):
    """E3: E1 mirrored, the faces on the cards' right (the enemy across the VS looks at us)."""
    return variant(dump, Layout(), lambda p, d, l, s, f: card_ours(p, d, l, s, f, mirrored=True))


def e4(dump):
    """E4: E1 taller (130), filling the panel; the troops larger."""
    return variant(dump, Layout(card_h=130), card_ours)


def e1_unknown(dump):
    """E1 before a scouting: dashed places, a '?' face, 'Unknown'."""
    return variant(dump, Layout(), card_ours, known=False, note='Send a ninja to learn them.')


def e1_empty(dump):
    """E1 with a place empty (three commanders), as the rebels' Pegasus, Dodge and Melfeis."""
    return variant(dump, Layout(), card_ours, empty=(4,), commanders=3, troops=1696)


VARIANTS = [
    ('E1  our cards, gold', e1),
    ('E2  the enemy in vermilion', e2),
    ('E3  faces on the right', e3),
    ('E4  taller cards', e4),
    ('E1  before a scouting', e1_unknown),
    ('E1  a place empty', e1_empty),
]


def main():
    dump = Image.open(sys.argv[1])
    pw, ph = PANEL[2] - PANEL[0], PANEL[3] - PANEL[1]
    gap, head, cols = 24, 40, 3
    rows = (len(VARIANTS) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * pw + (cols + 1) * gap, rows * (ph + head) + (rows + 1) * gap), (24, 19, 15, 255))
    d = ImageDraw.Draw(sheet)
    title = font(CINZEL, 22)
    for i, (name, draw) in enumerate(VARIANTS):
        x = gap + (i % cols) * (pw + gap)
        y = gap + (i // cols) * (ph + head + gap)
        d.text((x, y + 28), name, font=title, fill=NAME, anchor='ls')
        sheet.paste(draw(dump), (x, y + head))
    sheet.convert('RGB').save(sys.argv[2])


if __name__ == '__main__':
    main()
