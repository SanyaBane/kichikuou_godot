"""Mock-ups of the attacking army's facing on the enemy attack screen held upright (owner, 2026-10-31; docs/ui/phone.md,
"Round P24").

Held upright the army's front row stands on top, facing up at the province (PA1), but the only mark of it is the small
triangle beside "FRONT ROW" (EnemyAttackView.UprightArmy: 16 reference pixels, 0.9 mm). The PC holds the front row in a
gold bracket whose head points at the "VS" (docs/ui/enemy-attack.md, §8), the phone held landscape has a gold pointer
left of the cards (PhoneAttack.Pointer: 30 deep, 68 wide). These draw the facing upright as plainly.

The dumps: the screen gallery's upright pass (tools/ui/gallery.ps1 -PhoneUp -Only enemy-attack,enemy-attack-scouted;
docs/ui/gallery.md). Then, from the repository's root (Pillow and numpy; godot/fonts):
  python -I tools/ui/mock_attack_facing.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository: the dumps hold the player's CGs.

Round P24, over enemy-attack-scouted (the rebels on Ys (5) after Kanami's scouting) and enemy-attack (before it), the
week-18 save: PA1f1 the landscape's pointer turned up, over the front row's caption; PA1f2 the PC's bracket turned up,
holding the front row from above, its head at the "VS"; PA1f3 a pointer each side of the "VS", between the panels;
PA1f4 the "VS" on an arrow rising from the army's panel at the province's. The agent recommends PA1f2.

Round P25 (the owner chose PA1f2 with "FRONT ROW" under the bracket): the bracket's line over the caption, its ends down
past it to the cards' corners; PA1f2a the caption in the middle under the head and "BACK ROW" in the middle as now (the
agent's recommendation), PA1f2b both captions at the cards' left.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

GALLERY, OUT = sys.argv[1:3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
UP = os.path.join(GALLERY, 'phone-upright', 'current')
os.makedirs(OUT, exist_ok=True)

PW, PH = 1220, 2712                     # the Poco X6 Pro upright, reference pixels (the dump doubled)
PX_MM = 446 / 25.4                      # 17.6 a millimetre

# Lacquer (godot/Strategy/UiPalette.cs).
FILL, GOLD, DIM_GOLD, NAME = (21, 16, 12), (196, 160, 86), (110, 86, 46), (232, 199, 122)
NOTE = (255, 92, 160)                   # the mock-ups' own notes

# The dump's parts, reference pixels (measured on the upright pass).
MID = 610                               # the room's and the panels' middle across
GAP = (842, 922)                        # between the province's panel and the army's (its outer gold at 922)
ARMY_TOP = 916                          # the army's top border with its caption, to 1000
INSIDE = (24, 1196)                     # inside the army's inner line
CONTENT = (1060, 1830)                  # the row captions, the cards, the totals and the report
FRONT_CAPTION = (526, 1066, 698, 1094)  # "FRONT ROW", its small triangle right of it (702-718)
SMALL_ARROW = (698, 1064, 724, 1096)
BACK_CAPTION = (532, 1390, 690, 1418)
CARDS = (30, 1104, 1188)                # the cards' left, the front row's top, the cards' right
VS = (556, 852, 660, 910)


def font(name, size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size)))


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)


def mm(px):
    return f'{px / PX_MM:.1f} mm'


def base(name):
    return Image.open(os.path.join(UP, name + '.png')).convert('RGB').resize((PW, PH), Image.LANCZOS)


SS = 4


def smooth(img, box, colour, draw):
    """draw(d, p, s) on a mask four times finer over box, p mapping reference pixels into it; the mask laid on img in colour."""
    x0, y0, x1, y1 = box
    mask = Image.new('L', ((x1 - x0) * SS, (y1 - y0) * SS), 0)
    draw(ImageDraw.Draw(mask), lambda x, y: ((x - x0) * SS, (y - y0) * SS), SS)
    mask = mask.resize((x1 - x0, y1 - y0), Image.BOX)
    img.paste(Image.new('RGB', mask.size, colour), (x0, y0), mask)


def pointer(img, cx, tip, depth, half, colour=GOLD):
    """A pointer up: its tip at (cx, tip), its base depth under it, half wide."""
    smooth(img, (int(cx - half - 4), int(tip - 4), int(cx + half + 4), int(tip + depth + 4)), colour,
           lambda d, p, s: d.polygon([p(cx, tip), p(cx + half, tip + depth), p(cx - half, tip + depth)], fill=255))


def without_small_arrow(img):
    ImageDraw.Draw(img).rectangle(SMALL_ARROW, fill=FILL)


def shifted(img, dy):
    """The army's content (its row captions, cards, totals and report) moved down by dy inside its panel."""
    block = img.crop((INSIDE[0], CONTENT[0], INSIDE[1], CONTENT[1]))
    ImageDraw.Draw(img).rectangle((INSIDE[0], CONTENT[0], INSIDE[1], CONTENT[1] + dy), fill=FILL)
    img.paste(block, (INSIDE[0], CONTENT[0] + dy))


def note(d, xy, s, size=26, anchor='la'):
    d.multiline_text(xy, s, font=GOTHIC(size), fill=NOTE, anchor=anchor, stroke_width=4, stroke_fill=(0, 0, 0), spacing=6)


# --- the variants ----------------------------------------------------------------------------------------------------

def now(name):
    return base(name), ['as built: the front row\'s mark is the triangle right of "FRONT ROW", 16 px (0.9 mm)']


F1_DEPTH, F1_HALF, F1_DY = 40, 46, 40


def pa1f1(name):
    """The landscape's pointer turned up: centred over the front row's caption, the cards 40 lower."""
    img = base(name)
    without_small_arrow(img)
    shifted(img, F1_DY)
    base_y = FRONT_CAPTION[1] + F1_DY - 18
    pointer(img, MID, base_y - F1_DEPTH, F1_DEPTH, F1_HALF)
    return img, [f'the landscape\'s gold pointer turned up, {mm(2 * F1_HALF)} wide, {mm(F1_DEPTH)} deep, over "FRONT ROW"',
                 'the small triangle gone; the cards 40 px lower (the panel had the room under the report)']


# The PC's bracket (EnemyAttackView.DrawArmyBracket) turned up and scaled with the cards (573 / 252).
S = 573 / 252
F2_DY = 30


def pa1f2(name):
    """The PC's bracket turned up: the front row held from above, its head at the "VS"; the row captions at the left."""
    img = base(name)
    without_small_arrow(img)
    front = img.crop(FRONT_CAPTION)
    back = img.crop(BACK_CAPTION)
    shifted(img, F2_DY)
    d = ImageDraw.Draw(img)
    for box in (FRONT_CAPTION, BACK_CAPTION):
        d.rectangle((box[0], box[1] + F2_DY, box[2], box[3] + F2_DY), fill=FILL)
    top = CARDS[1] + F2_DY
    x0, x1 = CARDS[0] + 1, CARDS[2] - 1
    line = top - 7 * S
    # "FRONT ROW" over the bracket's left arm, "BACK ROW" over its row at the same left.
    caption_x = CARDS[0] + 14 - (FRONT_CAPTION[0] - 526) - 0
    img.paste(front, (caption_x - 8, int(line - 22 - (FRONT_CAPTION[3] - FRONT_CAPTION[1]))))
    img.paste(back, (caption_x - 8 - 6, BACK_CAPTION[1] + F2_DY))
    bracket(img, top, line)
    return img, [f'the PC\'s bracket turned up: a gold line {mm(2.5 * S)} over the front row from end to end, its head '
                 f'{mm(28 * S)} wide and {mm(27 * S)} deep in the middle at the "VS"',
                 'the row captions at the cards\' left; the cards 30 px lower']


def bracket(img, top, line):
    """The bracket's line at line from the front row's left end to its right, its ends down to the cards' top, its head up."""
    x0, x1 = CARDS[0] + 1, CARDS[2] - 1

    def arms(dr, p, s):
        w = round(2.5 * S * s)
        for side in (-1, 1):
            end = x0 if side < 0 else x1
            dr.line([p(end, top - 2 * S), p(end, line), p(MID + side * 16 * S, line), p(MID + side * 6 * S, line - 10 * S)],
                    fill=255, width=w, joint='curve')
    smooth(img, (0, int(line - 60), PW, int(top + 10)), GOLD, arms)
    head_base, tip = line - 6 * S, line - 33 * S
    smooth(img, (int(MID - 40), int(tip - 6), int(MID + 40), int(head_base + 6)), NAME,
           lambda dr, p, s: dr.polygon([p(MID - 14 * S, head_base), p(MID, tip), p(MID + 14 * S, head_base)], fill=255))


# Round P25 (the owner chose PA1f2 with "FRONT ROW" under the bracket): the bracket over the caption, its ends down past
# it to the cards' corners.
F2U_DY, F2U_LINE = 40, 30               # the cards' shift; the line's height over the caption's middle


def pa1f2_under(left):
    def draw(name):
        img = base(name)
        without_small_arrow(img)
        front = img.crop(FRONT_CAPTION)
        back = img.crop(BACK_CAPTION)
        shifted(img, F2U_DY)
        middle = (FRONT_CAPTION[1] + FRONT_CAPTION[3]) / 2 + F2U_DY
        if left:
            d = ImageDraw.Draw(img)
            for box in (FRONT_CAPTION, BACK_CAPTION):
                d.rectangle((box[0], box[1] + F2U_DY, box[2], box[3] + F2U_DY), fill=FILL)
            img.paste(front, (CARDS[0] + 20, FRONT_CAPTION[1] + F2U_DY))
            img.paste(back, (CARDS[0] + 14, BACK_CAPTION[1] + F2U_DY))
        top = CARDS[1] + F2U_DY
        bracket(img, top, middle - F2U_LINE)
        where = 'at the cards\' left inside the bracket, "BACK ROW" at the same left' if left else \
            'in the middle under the head, "BACK ROW" in the middle as now'
        return img, [f'PA1f2\'s bracket over "FRONT ROW": its line {mm(F2U_LINE)} over the caption\'s middle, its ends '
                     f'down past it to the cards\' corners ({mm(top - middle + F2U_LINE)})',
                     f'"FRONT ROW" {where}; the cards 40 px lower']
    return draw


F3_DEPTH, F3_HALF, F3_OFF = 40, 46, 150


def pa1f3(name):
    """A pointer each side of the "VS", between the panels."""
    img = base(name)
    without_small_arrow(img)
    mid_y = (VS[1] + VS[3]) / 2 + 2
    for side in (-1, 1):
        pointer(img, MID + side * F3_OFF, mid_y - F3_DEPTH / 2 - 4, F3_DEPTH, F3_HALF)
    return img, [f'two gold pointers beside the "VS", {mm(2 * F3_HALF)} wide, {mm(F3_DEPTH)} deep, pointing at Ys',
                 'the army\'s panel as it is, the small triangle gone']


F4_DOWN = 60


def pa1f4(name):
    """The "VS" on an arrow rising from the army's panel at the province's: the panel 60 lower at its top."""
    img = base(name)
    without_small_arrow(img)
    src = img.copy()
    # The gap's backdrop without the "VS": its left part tiled over it, then the gap made 60 taller.
    gap = src.crop((0, GAP[0], PW, GAP[1]))
    patch = gap.crop((VS[0] - 160, 0, VS[2] - 160, GAP[1] - GAP[0]))
    gap.paste(patch, (VS[0], 0))
    clean = gap.crop((0, 4, PW, 74))                                        # clear of both panels' borders
    for y in range(GAP[0], GAP[1] + F4_DOWN, clean.height):
        img.paste(clean.crop((0, 0, PW, min(clean.height, GAP[1] + F4_DOWN - y))), (0, y))
    # The panel's top border and caption 60 lower, its content 30 lower.
    img.paste(src.crop((0, ARMY_TOP, PW, 1000)), (0, ARMY_TOP + F4_DOWN))
    ImageDraw.Draw(img).rectangle((INSIDE[0], 1000 + F4_DOWN, INSIDE[1], CONTENT[0]), fill=FILL)
    block = src.crop((INSIDE[0], CONTENT[0], INSIDE[1], CONTENT[1]))
    ImageDraw.Draw(img).rectangle((INSIDE[0], CONTENT[0], INSIDE[1], CONTENT[1] + F4_DOWN // 2), fill=FILL)
    img.paste(block, (INSIDE[0], CONTENT[0] + F4_DOWN // 2))

    tip, neck, foot = GAP[0] + 10, GAP[0] + 62, GAP[1] + F4_DOWN + 8
    head, shaft = 120, 78
    outline = [(MID - shaft, foot), (MID - shaft, neck), (MID - head, neck), (MID, tip), (MID + head, neck),
               (MID + shaft, neck), (MID + shaft, foot)]
    box = (MID - head - 8, tip - 8, MID + head + 8, foot + 2)
    smooth(img, box, FILL, lambda dr, p, s: dr.polygon([p(*q) for q in outline], fill=255))
    smooth(img, box, GOLD, lambda dr, p, s: dr.line([p(*q) for q in outline], fill=255, width=3 * s, joint='curve'))
    d = ImageDraw.Draw(img)
    d.text((MID, (neck + GAP[1] + F4_DOWN) / 2 + 2), 'VS', font=BRUSH(56), fill=GOLD, anchor='mm')
    return img, [f'"VS" on a lacquer arrow {mm(2 * head)} wide, its tip at Ys\'s panel, its shaft rising from the army\'s',
                 'the army\'s panel 60 px lower at its top (the room it had over its cards), the cards 30 px lower']


VARIANTS = [('Now', now), ('PA1f1 — the landscape\'s pointer, turned up', pa1f1),
            ('PA1f2 — the PC\'s bracket, turned up (recommended)', pa1f2), ('PA1f3 — pointers beside the VS', pa1f3),
            ('PA1f4 — the VS on an arrow', pa1f4)]


def wrapped(d, text, face, width):
    words, lines = text.split(' '), ['']
    for w in words:
        trial = (lines[-1] + ' ' + w).strip()
        if d.textlength(trial, font=face) <= width or not lines[-1]:
            lines[-1] = trial
        else:
            lines.append(w)
    return lines


def sheet(dump, name, title, variants=VARIANTS):
    drawn = [(label, *f(dump)) for label, f in variants]
    gap, head, foot = 24, 110, 170
    w = PW // 2
    out = Image.new('RGB', (len(drawn) * w + (len(drawn) + 1) * gap, head + PH // 2 + foot), (40, 40, 40))
    d = ImageDraw.Draw(out)
    d.text((gap, 18), title, font=GOTHIC(34), fill=(255, 255, 255))
    for i, (label, im, notes) in enumerate(drawn):
        x = gap + i * (w + gap)
        d.text((x, head - 14), label, font=GOTHIC(24), fill=(255, 255, 255), anchor='ld')
        out.paste(im.resize((w, PH // 2), Image.LANCZOS), (x, head))
        y = head + PH // 2 + 14
        for n in notes:
            for line in wrapped(d, n, GOTHIC(20), w):
                d.text((x, y), line, font=GOTHIC(20), fill=NOTE)
                y += 26
            y += 6
        im.save(os.path.join(OUT, f'{label.split()[0].lower()}-{dump}.png'))
    out.save(os.path.join(OUT, name + '.png'))
    print(os.path.join(OUT, name + '.png'), out.size)


def detail(dump, name, title, variants=VARIANTS):
    """The middle of each variant at the phone's pixels halved: from Ys's panel's foot to the back row."""
    crop = (0, 700, PW, 1520)
    drawn = [(label, f(dump)[0].crop(crop)) for label, f in variants]
    gap, head = 24, 110
    w, h = (crop[2] - crop[0]) // 2, (crop[3] - crop[1]) // 2
    out = Image.new('RGB', (len(drawn) * w + (len(drawn) + 1) * gap, head + h + gap), (40, 40, 40))
    d = ImageDraw.Draw(out)
    d.text((gap, 18), title, font=GOTHIC(34), fill=(255, 255, 255))
    for i, (label, im) in enumerate(drawn):
        x = gap + i * (w + gap)
        d.text((x, head - 14), label, font=GOTHIC(24), fill=(255, 255, 255), anchor='ld')
        out.paste(im.resize((w, h), Image.LANCZOS), (x, head))
    out.save(os.path.join(OUT, name + '.png'))
    print(os.path.join(OUT, name + '.png'), out.size)


sheet('enemy-attack-scouted', 'p24-scouted',
      'Round P24 — the army\'s facing upright: the rebels on Ys (5) after Kanami\'s scouting, the week-18 save')
sheet('enemy-attack', 'p24-unknown', 'Round P24 — the same before the scouting (the places unknown)')
detail('enemy-attack-scouted', 'p24-detail', 'Round P24 — the middle of the screen at the dump\'s pixels: Ys\'s panel\'s foot to the back row')

P25 = [('Now', now), ('PA1f2 — as round P24', pa1f2), ('PA1f2a — FRONT ROW under, centred (recommended)', pa1f2_under(False)),
       ('PA1f2b — FRONT ROW under, at the left', pa1f2_under(True))]
sheet('enemy-attack-scouted', 'p25-scouted', 'Round P25 — PA1f2 with "FRONT ROW" under the bracket: the rebels on Ys (5) after the scouting', P25)
sheet('enemy-attack', 'p25-unknown', 'Round P25 — the same before the scouting (the places unknown)', P25)
detail('enemy-attack-scouted', 'p25-detail', 'Round P25 — the middle of the screen at the dump\'s pixels', P25)
