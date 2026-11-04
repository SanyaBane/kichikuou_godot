"""Mock-ups of our Treasure screen in the touch mode (owner, 2026-10-27: no hover on Android, so a tap only selects —
the entry's picture and figures in the panel — and a button under the picture acts; docs/ui/pickers.md, "Round X"),
drawn over the screen gallery's tablet dumps of the week-18 save (docs/ui/gallery.md).

The dumps: tools/ui/gallery.ps1 -Tablet -Only treasure-hover,treasure-items,treasure-which-item. Then, from the
repository's root (Pillow; godot/fonts; the parts are mock_touch_panel.py's):
  python -I tools/ui/mock_touch_treasure.py <gallery dir> <out dir> <cg_png dir> < /dev/null
<cg_png dir> is the extract's cg_png (By Lord's picture, CG 757, which no dump shows). Keep <out dir> outside the
repository: the images hold the player's CGs.

The week-18 stock is two items, both held: the L. Holy Sword (item 6) on Rance (unit 1) and By Lord (item 7) on Rick
(unit 7). The button stands at the info panel's right, under the picture's description column; its label never names
the selected entry (the owner): Choose an item, Choose a character, Equip, Take off.
"""
import sys, os
sys.dont_write_bytecode = True                  # no __pycache__ in the repository for the borrowed module
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw
from mock_touch_panel import (W, H, S, GOLD, DIM_GOLD, NAME, SUBTITLE, TEXT, QUIET, NOTE, GOTHIC, BRUSH, CINZEL, OUT,
                              Frame, sc, text, width, note, rect, shadow, studs, load)

CGS = sys.argv[3]
CAPTION = 64

# TreasureView's places, in the 1280x800 screen's pixels.
PICTURE = (16, 88, 682, 504)
INFO = (16, 516, 682, 784)
BUTTON = (432, 541, 664, 611)                   # the action: under the picture's description column, 232x70
BUTTON_TYPE = 18                                # every label alike: "Choose a character" fits at 18
LABEL, VALUE = (150, 134, 108), (236, 226, 204)
W_TEXT = (238, 222, 176)
OFF, OFF_LINE, OFF_TEXT, MINUS = (40, 35, 30), (66, 58, 48), (98, 88, 74), (222, 110, 90)
VERMILION, CHOSEN = (170, 40, 28), (60, 30, 22)
YES_LINE, NAME_ON_RED = (150, 96, 52), (250, 226, 160)


def cell(n):
    """TreasureView.CellRect: Sｱｲﾃﾑ's 3x17 grid, left to right, top to bottom."""
    x, y = 708 + (n - 1) % 3 * 180, 106 + (n - 1) // 3 * 36
    return (x, y, x + 176, y + 32)


def dump(name):
    return load('tablet', 'current', name + '.png')


def frame(base, *patches):
    """A dump with rectangles of other dumps pasted over it: (dump, box)."""
    img = base.copy()
    for other, box in patches:
        img.paste(other.crop(box), box[:2])
    return Frame(img)


def save(f, name, caption, note_line):
    """The frame with a caption band under it, as mock_pickers.py's."""
    out = Image.new('RGBA', (W * S, (H + CAPTION) * S), (23, 24, 27, 255))
    out.paste(f.img, (0, 0))
    d = ImageDraw.Draw(out)
    d.line(sc(0, H, W, H), fill=DIM_GOLD + (255,), width=S)
    d.text(sc(16, H + 10), caption, font=CINZEL(20, 700), fill=NAME + (255,))
    d.text(sc(16, H + 38), note_line, font=GOTHIC(15), fill=QUIET + (255,))
    out.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
    print(name)


def fill_like(f, box, at):
    """Covers box with the colour the screen has at `at` (the lacquer panel's own)."""
    colour = f.img.getpixel((at[0] * S, at[1] * S))[:3]
    rect(f, box, colour)


def finger(f, x, y):
    lay, d = f.draw()
    d.ellipse(sc(x - 15, y - 15, x + 15, y + 15), fill=NOTE + (90,), outline=NOTE + (230,), width=2 * S)
    f.add(lay)


def item_picture(f, n):
    """The item's CG over the picture panel, where FacePictures.PicturePanel(750 + n, 819, 692) draws it: the CG at
    script (8, 6), the panel the cut (6, 6) 314x196 scaled into PICTURE."""
    kx, ky = (PICTURE[2] - PICTURE[0]) / 314, (PICTURE[3] - PICTURE[1]) / 196
    picture = Image.open(os.path.join(CGS, 'CG%04d.png' % (750 + n))).convert('RGBA')
    picture = picture.resize((round(304 * kx * S), round(196 * ky * S)), Image.NEAREST)
    f.img.alpha_composite(picture, (round((PICTURE[0] + 2 * kx) * S), PICTURE[1] * S))


def details(f, name, subtitle, figures, subtitle_off=False):
    """TreasureView.DrawInfo's top: the name in the brush face, the subtitle, the figures."""
    fill_like(f, (28, 528, 446, 626), (430, 600))
    text(f, 40, 557, name, BRUSH(24), NAME, anchor='ls')
    text(f, 40 + width(name, BRUSH(24)) + 14, 556, subtitle, CINZEL(13, 700), MINUS if subtitle_off else SUBTITLE, anchor='ls')
    x = 40
    for label, value in figures:
        text(f, x, 590, label, CINZEL(11, 700), LABEL, anchor='ls')
        text(f, x, 614, value, GOTHIC(18), VALUE, anchor='ls')
        x += max(110, width(value, GOTHIC(18)) + 26)


def top_hint(f, line):
    fill_like(f, (28, 528, 446, 626), (430, 600))
    text(f, 40, 580, line, GOTHIC(15), QUIET, anchor='lm')


def tap_hint(f):
    """The lower half's hint, its verb a tap's."""
    fill_like(f, (28, 640, 670, 774), (600, 700))
    text(f, 40, 668, 'Tap a character, then the item to equip —', GOTHIC(14), QUIET, anchor='ls')
    text(f, 40, 690, 'or an item, then the character to equip it.', GOTHIC(14), QUIET, anchor='ls')


def selected(f, n, name, holder):
    """A cell selected by a tap: the PC's hover look, kept (RowHover, HoverLine 2 px, the name lit)."""
    r = cell(n)
    rect(f, r, (64, 48, 28), outline=(222, 186, 104), w=2)
    mid = (r[1] + r[3]) / 2
    text(f, r[2] - 8, mid + 4, holder, GOTHIC(10), QUIET, anchor='rs')
    text(f, r[0] + 10, mid + 5, name, GOTHIC(14), NAME, anchor='ls')


def asked(f, n, name, holder):
    """The cell a callout asks about (TreasureView.Asked): vermilion."""
    r = cell(n)
    rect(f, r, CHOSEN, outline=VERMILION, w=2)
    mid = (r[1] + r[3]) / 2
    text(f, r[2] - 8, mid + 4, holder, GOTHIC(10), QUIET, anchor='rs')
    text(f, r[0] + 10, mid + 5, name, GOTHIC(14), NAME, anchor='ls')


def fitted(label, size, room):
    while width(label, CINZEL(size, 700)) > room and size > 12:
        size -= 1
    return CINZEL(size, 700)


def red(f, r, label, size=BUTTON_TYPE):
    """UiPalette.DrawRedButton: red lacquer framed in gold (Summon, the callout's Yes)."""
    shadow(f, r, 4)
    lay, d = f.draw()
    top, bottom = (128, 32, 24), (80, 16, 12)
    for i in range(round((r[3] - r[1]) * S)):
        t = i / max(1, (r[3] - r[1]) * S - 1)
        d.line((r[0] * S, r[1] * S + i, r[2] * S - 1, r[1] * S + i), fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (255,))
    f.add(lay)
    rect(f, r, outline=GOLD, w=2)
    rect(f, (r[0] + 4, r[1] + 4, r[2] - 4, r[3] - 4), outline=YES_LINE)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2 + 1, label, fitted(label, size, r[2] - r[0] - 28), NAME_ON_RED)


def off(f, r, label, reason, size=BUTTON_TYPE):
    """The button greyed: the script would refuse; its reason in the button, under the label."""
    rect(f, r, OFF, outline=OFF_LINE)
    h = r[3] - r[1]
    text(f, (r[0] + r[2]) / 2, r[1] + h * 0.38, label, fitted(label, size, r[2] - r[0] - 28), OFF_TEXT)
    text(f, (r[0] + r[2]) / 2, r[1] + h * 0.74, reason, GOTHIC(14), MINUS)


def plain(f, r, label, size=17):
    rect(f, r, (34, 26, 20), outline=DIM_GOLD)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2 + 1, label, CINZEL(size, 700), TEXT)


def callout_left_of(f, target, lines):
    """TreasureCallout beside a button: the lacquer box at its left, its tail on the button, the script's lines and Yes/No."""
    w_box = 384
    cy = (target[1] + target[3]) / 2
    x1 = target[0] - 16
    y0 = max(cy - 46, 530)
    h_box = max(30 + len(lines) * 28 + 14 + 38 + 24, 772 - y0)     # down to the panel's foot: the prompt hidden whole
    r = (x1 - w_box, y0, x1, y0 + h_box)
    shadow(f, r, 8)
    rect(f, r, (30, 22, 16))
    rect(f, (r[0] + 1, r[1] + 1, r[2] - 1, r[3] - 1), outline=GOLD, w=2)
    rect(f, (r[0] + 5.5, r[1] + 5.5, r[2] - 5.5, r[3] - 5.5), outline=DIM_GOLD)
    studs(f, r)
    lay, d = f.draw()                           # the tail, pointing right at the button
    d.polygon(sc(x1 - 2, cy - 13, x1 + 13, cy, x1 - 2, cy + 13), fill=GOLD + (255,))
    d.polygon(sc(x1 - 3, cy - 10, x1 + 8, cy, x1 - 3, cy + 10), fill=(30, 22, 16, 255))
    f.add(lay)
    for i, line in enumerate(lines):
        text(f, r[0] + 26, r[1] + 30 + i * 28, line, GOTHIC(19), W_TEXT, anchor='lm')
    ay = r[3] - 24 - 38
    red(f, (r[0] + 26, ay, r[0] + 26 + 136, ay + 38), 'Yes', 17)
    plain(f, (r[0] + 26 + 150, ay, r[0] + 26 + 286, ay + 38), 'No')


# ---------------------------------------------------------------- the round

def x_round():
    hover, items, which = dump('treasure-hover'), dump('treasure-items'), dump('treasure-which-item')
    lower = (22, 634, 678, 780)                 # the info panel's lower half: the first pick's prompt and Back

    # X1 — a character tapped: Rance selected, his details, "Choose an item"
    f = frame(hover)
    tap_hint(f)
    red(f, BUTTON, 'Choose an item')
    finger(f, cell(1)[0] + 120, cell(1)[1] + 18)
    save(f, 'X1-character.png', 'X1 — a tap selects: Rance\'s cell lit, his picture and figures, "Choose an item" under the picture',
         'The selection is the PC\'s hover, kept; nothing reaches the script yet. The button: 232x70 (47x14 mm on the tablet).')

    # X2 — after Choose an item: the items' view, nothing tapped yet
    f = frame(which)
    top_hint(f, 'Tap an item to see it.')
    save(f, 'X2-which-item.png', 'X2 — "Choose an item" pressed: the view turns, Rance is the first pick, no item selected yet',
         'As built: the empty picture, the prompt with his face and Back; no button until an item is tapped.')

    # X3 — an item tapped: By Lord (Rick's), "Equip"
    f = frame(which)
    item_picture(f, 7)
    details(f, 'By Lord', 'Equippable', [('Held by', 'Rick')])
    selected(f, 7, 'By Lord', 'Rick')
    red(f, BUTTON, 'Equip')
    finger(f, cell(7)[0] + 120, cell(7)[1] + 18)
    save(f, 'X3-item.png', 'X3 — By Lord tapped: its picture with its description, "Held by Rick", Equip under it',
         'The description is read where the button is: the eyes go from the picture\'s right column straight down to Equip.')

    # X4 — Equip pressed: the script's question (Sｱｲﾃﾑ.ADV:339-350) in the callout beside the button
    f = frame(which)
    item_picture(f, 7)
    details(f, 'By Lord', 'Equippable', [('Held by', 'Rick')])
    asked(f, 7, 'By Lord', 'Rick')
    red(f, BUTTON, 'Equip')
    callout_left_of(f, BUTTON, ['By Lord is equipped to', 'Rick, but would you', 'like to remove it and equip', 'Rance instead?'])
    save(f, 'X4-question.png', 'X4 — Equip on an item another unit holds: the script\'s question beside the button, not at the cell',
         'The callout (W2\'s) stands at the last tap, now the button: over the panel\'s figures and prompt; By Lord stays marked in '
         'the grid. Yes equips, No keeps the selection.')

    # X5 — a held item tapped with no pick: "Take off"
    f = frame(items)
    tap_hint(f)
    red(f, BUTTON, 'Take off')
    finger(f, cell(6)[0] + 120, cell(6)[1] + 18)
    save(f, 'X5-take-off.png', 'X5 — the items\' view, no pick: Rance\'s L. Holy Sword tapped, "Take off"',
         'The original\'s click on a held item with no pick (its question "would you like to unequip it?" follows, in the '
         'callout beside the button).')

    # X6 — a pick the script refuses, greyed in advance: Rance picked, his own sword tapped
    f = frame(items, (which, lower))
    off(f, BUTTON, 'Equip', 'Already equipped.')
    finger(f, cell(6)[0] + 120, cell(6)[1] + 18)
    save(f, 'X6-refused.png', 'X6 — Rance picked, his own L. Holy Sword tapped: Equip greyed, the script\'s refusal written in it',
         'So no refusal window to tap away; the same for Chaos and Nikkou ("Can\'t be unequipped.") and an item without effect '
         '("Can\'t be equipped.").')

    # X7 — the button's labels: what each selection offers
    f = Frame(Image.new('RGB', (W, H), (14, 10, 8)))
    cases = [('Choose an item', 'a character,', 'nothing picked', None),
             ('Choose a character', 'an item in stock,', 'nothing picked', None),
             ('Equip', 'the second pick', '(an item or a character)', None),
             ('Take off', 'a held item,', 'nothing picked', None),
             ('Equip', 'a pick the script', 'would refuse', 'Already equipped.')]
    bw = BUTTON[2] - BUTTON[0]
    for i, (label, a, b, reason) in enumerate(cases):
        x = 24 + i * (bw + 16)
        r = (x, 360, x + bw, 430)
        note(f, x + bw / 2, 300, a, anchor='mm', size=15)
        note(f, x + bw / 2, 326, b, anchor='mm', size=15)
        if reason:
            off(f, r, label, reason)
        else:
            red(f, r, label)
    text(f, 640, 200, 'The button under the picture: its label by what is selected', CINZEL(22, 700), NAME)
    text(f, 640, 236, 'It never names the selection (the owner): the picture and the figures above it do.', GOTHIC(16), QUIET)
    save(f, 'X7-labels.png', 'X7 — the button\'s labels: one red button, its word by the selection and the pick in progress',
         'Choose an item / Choose a character turn the view (the original\'s first pick); Equip and Take off act; greyed where '
         'the script refuses.')


x_round()
