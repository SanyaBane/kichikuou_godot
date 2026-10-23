"""Mock-ups of the already-read text's colour (owner's request 2026-10-23, docs/ui/read-text.md): lines the player has
read before are drawn in a colour of their choice in our message windows, chosen in Option -> Messages from a row of
swatches after Rance X's ("Already-read text": "Don't change" and nine colours).

The dumps: the options window (a window; nothing moves the pointer) and the week-26 save's opening scenes (headless,
masked; our interface covers the original's windows, so the key waits show the scene without them):
  <Godot console exe> --path godot -- --options --shots <options dir> --quit
  <Godot console exe> --headless --path godot -- --load --auto --fast --quit --seed 2
      --saves user://saves-look-msg --slots-from user://saves-week26 --until deployment --shots <shots dir>
Then, from the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_read_text.py <options dir> <shots dir> <out dir> < /dev/null
The images hold the player's CGs: keep <out dir> outside the repository.
Everything is in the 1280x800 screen's pixels; drawn at 2x and scaled down for smooth edges.
"""
import sys, os, math
from PIL import Image, ImageDraw, ImageFont

OPTIONS, SHOTS, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
CAPTION = 64

# Lacquer.cs
PANEL = (22, 16, 12, 255)
GOLD, DIM_GOLD, NAME = (196, 160, 86, 255), (110, 86, 46, 255), (232, 199, 122, 255)
TEXT, QUIET = (244, 232, 206, 255), (176, 160, 130, 255)
NARR, NARR_TEXT = (52, 38, 24, 255), (226, 204, 150, 255)
INK, INK_QUIET, INK_GREY = (40, 28, 18, 255), (100, 80, 56, 255), (150, 132, 104, 255)
VERMILION, HEAD_INK, PAPER_RULE = (170, 40, 28, 255), (120, 40, 26, 255), (150, 120, 80, 110)

# The nine colours, after Rance X's row (left to right).
COLOURS = [('Salmon', (240, 120, 120)), ('Rose', (240, 128, 184)), ('Magenta', (255, 128, 255)),
           ('Violet', (192, 128, 255)), ('Periwinkle', (128, 128, 255)), ('Sky', (128, 184, 255)),
           ('Cyan', (128, 255, 255)), ('Mint', (128, 255, 192)), ('Green', (128, 255, 128))]
CHOSEN = 5                                    # Sky: the proposed default
THOUGHT_DIM = 0.72                            # a thought's read text: the colour as dim as Quiet is against Text


def font(path, size, weight=None):
    f = ImageFont.truetype(path, round(size * S))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font(REPO + '/godot/fonts/BIZUDGothic-Regular.ttf', size)
CINZEL = lambda size, w=600: font(REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf', size, w)


def s(*v):
    return [x * S for x in v]


def pts(p):
    return [(x * S, y * S) for x, y in p]


def rgba(c, a=255):
    return tuple(c[:3]) + (a,)


def dim(c, k):
    return tuple(int(v * k) for v in c[:3]) + (255,)


class Canvas:
    def __init__(self, path):
        base = Image.open(path).convert('RGB').resize((1280, 800), Image.LANCZOS).convert('RGBA')
        self.base = base
        self.ov = Image.new('RGBA', (1280 * S, 800 * S), (0, 0, 0, 0))

    def draw(self):
        lay = Image.new('RGBA', self.ov.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay):
        self.ov = Image.alpha_composite(self.ov, lay)

    def image(self):
        return Image.alpha_composite(self.base, self.ov.resize((1280, 800), Image.LANCZOS))

    def save(self, name, caption, note):
        img = Image.new('RGBA', (1280 * S, (800 + CAPTION) * S), (23, 18, 14, 255))
        img.paste(self.image().resize((1280 * S, 800 * S), Image.LANCZOS), (0, 0))
        d = ImageDraw.Draw(img)
        d.line(s(0, 800, 1280, 800), fill=GOLD, width=S)
        d.text(s(16, 810), caption, font=CINZEL(20, 700), fill=NAME)
        d.text(s(16, 838), note, font=GOTHIC(15), fill=QUIET)
        img.resize((1280, 800 + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


# ---------------------------------------------------------------- the windows (mock_messages.py's variant A)

def shadow(c, x, y, w, h):
    for i in range(6, 0, -1):
        lay, d = c.draw()
        d.rectangle(s(x - i + 2, y - i + 6, x + w + i + 2, y + h + i + 6), fill=(0, 0, 0, 15))
        c.add(lay)


def corners(d, x, y, w, h):
    for cx, cy, sx, sy in [(x, y, 1, 1), (x + w, y, -1, 1), (x, y + h, 1, -1), (x + w, y + h, -1, -1)]:
        d.line(pts([(cx + sx * 9, cy + sy * 2), (cx + sx * 9, cy + sy * 9), (cx + sx * 2, cy + sy * 9)]), fill=GOLD, width=2 * S)
        d.polygon(pts([(cx + sx * 9, cy + sy * 14), (cx + sx * 14, cy + sy * 9), (cx + sx * 9, cy + sy * 4),
                       (cx + sx * 4, cy + sy * 9)]), fill=GOLD)


def panel(c, x, y, w, h, fill=PANEL):
    shadow(c, x, y, w, h)
    lay, d = c.draw()
    d.rectangle(s(x, y, x + w, y + h), fill=fill)
    d.rectangle(s(x + 1, y + 1, x + w - 1, y + h - 1), outline=GOLD, width=2 * S)
    d.rectangle(s(x + 5.5, y + 5.5, x + w - 5.5, y + h - 5.5), outline=DIM_GOLD, width=1 * S)
    corners(d, x, y, w, h)
    c.add(lay)


def tail(c, edge_x, y0, y1, tip, side):
    lay, d = c.draw()
    inward = -7 if side == 'right' else 7
    d.polygon(pts([(edge_x + inward, y0 + 2), (edge_x + inward, y1 - 2), tip]), fill=PANEL)
    d.polygon(pts([(edge_x, y0), (edge_x, y1), tip]), fill=PANEL)
    d.line(pts([(edge_x, y0), tip]), fill=GOLD, width=2 * S)
    d.line(pts([(edge_x, y1), tip]), fill=GOLD, width=2 * S)
    ix = edge_x + (-5.5 if side == 'right' else 5.5)
    d.line(pts([(ix, y0 + 1), (ix, y1 - 1)]), fill=PANEL, width=3 * S)
    c.add(lay)


def thought_panel(c, x, y, w, h, bubbles):
    shadow(c, x, y, w, h)
    lay, d = c.draw()
    fill = (30, 26, 30, 255)
    d.rounded_rectangle(s(x, y, x + w, y + h), radius=18 * S, fill=fill)
    r, dash, gap = 18, 14, 8

    def dashes(a, b):
        (ax, ay), (bx, by) = a, b
        n = math.hypot(bx - ax, by - ay)
        t = 0
        while t < n:
            e = min(t + dash, n)
            d.line(pts([(ax + (bx - ax) * t / n, ay + (by - ay) * t / n), (ax + (bx - ax) * e / n, ay + (by - ay) * e / n)]),
                   fill=GOLD, width=2 * S)
            t += dash + gap
    dashes((x + r, y + 1), (x + w - r, y + 1))
    dashes((x + r, y + h - 1), (x + w - r, y + h - 1))
    dashes((x + 1, y + r), (x + 1, y + h - r))
    dashes((x + w - 1, y + r), (x + w - 1, y + h - r))
    for bx, by, start in [(x + 1, y + 1, 180), (x + w - 2 * r - 1, y + 1, 270), (x + 1, y + h - 2 * r - 1, 90),
                          (x + w - 2 * r - 1, y + h - 2 * r - 1, 0)]:
        d.arc(s(bx, by, bx + 2 * r, by + 2 * r), start, start + 90, fill=GOLD, width=2 * S)
    for bx, by, br in bubbles:
        d.ellipse(s(bx - br, by - br, bx + br, by + br), fill=fill[:3] + (240,), outline=GOLD, width=2 * S)
    c.add(lay)


def triangle(c, x, y):
    lay, d = c.draw()
    p = [(x - 8, y - 6), (x + 8, y - 6), (x, y + 7)]
    d.polygon(pts(p), fill=GOLD)
    d.line(pts(p + [p[0]]), fill=NAME, width=1 * S)
    c.add(lay)


def lines(c, x, y, rows, size=48, pitch=46):
    """The text at its cells: (line, colour) rows, the game's font, one colour per script line."""
    f = GOTHIC(size)
    lay, d = c.draw()
    for i, (line, colour) in enumerate(rows):
        d.text(((x) * S, (y + i * pitch) * S), line, font=f, fill=colour, stroke_width=1, stroke_fill=colour)
    c.add(lay)


R121 = (58, 512, 822, 254)
R111 = (436, 4, 834, 254)
R113 = (248, 536, 784, 258)

MARIS = ['I believe that we should first', 'prepare our cities for their', 'attacks, then defend them with', 'urban warfare.']
RANCE = ['I go to the trouble of', 'becoming king, and these', 'people start a rebellion on me.', 'Idiots, all of them.']
THOUGHT = ['Mhm, I see. Maybe that would', "work... I'd expect no less of", 'the person that governs this', 'country from the shadows.']
NARRATION = ['Rance was lying on the carpet', 'in front of the throne, with', 'Lia sitting and drinking tea', 'at his side.']


def speech_right(c, rows):
    x, y, w, h = R121
    panel(c, x, y, w, h)
    tail(c, x + w, y + 150, y + 196, (x + w + 30, y + 188), 'right')
    lines(c, x + 30, y + 22, rows)
    triangle(c, x + w - 30, y + h - 28)


def speech_left(c, rows):
    x, y, w, h = R111
    panel(c, x, y, w, h)
    tail(c, x, y + 96, y + 142, (x - 30, y + 104), 'left')
    lines(c, x + 34, y + 22, rows)
    triangle(c, x + w - 30, y + h - 28)


def thought(c, rows):
    x, y, w, h = R111
    thought_panel(c, x, y, w, h, [(x - 12, y + 128, 12), (x - 34, y + 150, 8), (x - 50, y + 166, 5)])
    lines(c, x + 34, y + 22, rows)
    triangle(c, x + w - 30, y + h - 28)


def narration(c, rows):
    x, y, w, h = R113
    panel(c, x, y, w, h, fill=NARR)
    lines(c, x + 32, y + 22, rows)
    triangle(c, x + w - 30, y + h - 28)


def read(text_lines, colour, plain, read_count=None):
    n = len(text_lines) if read_count is None else read_count
    return [(line, rgba(colour) if i < n else plain) for i, line in enumerate(text_lines)]


# ---------------------------------------------------------------- the options window

def recolour(img, box, colour, bg=PANEL, fg=TEXT):
    """The preview's lines in the dump drawn again in colour: each pixel's share of the text's colour over the panel's."""
    px = img.load()
    lb = sum(bg[:3]) / 3
    lf = sum(fg[:3]) / 3
    for yy in range(box[1], box[3]):
        for xx in range(box[0], box[2]):
            r, g, b, a = px[xx, yy]
            t = max(0.0, min(1.0, ((r + g + b) / 3 - lb) / (lf - lb)))
            if t <= 0.02:
                continue
            px[xx, yy] = tuple(int(bg[i] + (colour[i] - bg[i]) * t) for i in range(3)) + (a,)


def swatch_row(c, x, y, chosen, hover=None):
    """'Don't change' and the nine colours: lacquer tiles (the colour seen on the windows' own lacquer), the chosen one
    framed in vermilion as the palette's swatches are."""
    lay, d = c.draw()
    tiles = [('none', 150)] + [(name, 44) for name, _ in COLOURS]
    cx = x
    for i, (name, w) in enumerate(tiles):
        r = (cx, y, cx + w, y + 44)
        d.rectangle(s(*r), fill=PANEL, outline=DIM_GOLD, width=1 * S)
        if i == 0:
            d.text(s(cx + w / 2, y + 22), "Don't change", font=GOTHIC(16), fill=TEXT, anchor='mm')
        else:
            col = COLOURS[i - 1][1]
            d.rectangle(s(cx + 8, y + 8, cx + w - 8, y + 36), fill=rgba(col))
        if i == chosen:
            d.rectangle(s(cx - 3, y - 3, cx + w + 3, y + 47), outline=VERMILION, width=3 * S)
        elif i == hover:
            d.rectangle(s(cx - 2, y - 2, cx + w + 2, y + 46), outline=HEAD_INK, width=2 * S)
        cx += w + 10
    c.add(lay)
    return cx


def options(name, chosen, caption, note, hover=None):
    c = Canvas(os.path.join(OPTIONS, '00000-options-messages.png'))
    # the preview's first two lines count as read (screen px of the dump's 121)
    if chosen > 0:
        recolour(c.base, (186, 278, 630, 337), COLOURS[chosen - 1][1])
    lay, d = c.draw()
    d.text(s(180, 432), 'The first two lines count as read.' if chosen > 0 else 'Read lines keep the window\'s colour.',
           font=GOTHIC(13), fill=INK_QUIET)
    x, y, w = 166, 486, 948
    d.text(s(x, y + 14), 'ALREADY-READ TEXT', font=CINZEL(13, 800), fill=HEAD_INK, anchor='ls')
    d.line(s(x, y + 22, x + w, y + 22), fill=PAPER_RULE, width=1 * S)
    c.add(lay)
    swatch_row(c, x, y + 40, chosen, hover)
    lay, d = c.draw()
    label = 'Don\'t change' if chosen == 0 else COLOURS[chosen - 1][0]
    d.text(s(x, y + 112), label, font=GOTHIC(17), fill=INK)
    d.text(s(x, y + 138), 'Lines you have read before, in any game, show in this colour in the message windows.',
           font=GOTHIC(13), fill=INK_QUIET)
    d.text(s(x, y + 158), 'The battle\'s lines, the menus and our screens keep their own. Forget them… counts them unread.',
           font=GOTHIC(13), fill=INK_QUIET)
    c.add(lay)
    c.save(name, caption, note)


# ---------------------------------------------------------------- the images

def r1():
    options('R1-options.png', CHOSEN + 1, 'R1 — Option, Messages: "Already-read text", Sky chosen',
            'A row after Rance X\'s: "Don\'t change" and nine colours on lacquer tiles; the preview\'s first two lines show the choice.')
    options('R1b-options-off.png', 0, 'R1b — the same, "Don\'t change" chosen (Rose under the pointer)',
            'Read lines keep the window\'s own colour; a click on a tile chooses it at once, as the palette does.', hover=2)


def r2():
    sky = COLOURS[CHOSEN][1]
    c = Canvas(os.path.join(SHOTS, '00015-key.png'))
    speech_right(c, read(MARIS, sky, TEXT, 2))
    c.save('R2-speech-mixed.png', 'R2 — a speech (121) read in part: two lines read before, two new',
           'One colour per script line: a page that runs from read lines into new ones shows where the new text starts.')


def r3():
    sky = COLOURS[CHOSEN][1]
    c = Canvas(os.path.join(SHOTS, '00004-key.png'))
    speech_left(c, read(RANCE, sky, TEXT))
    c.save('R3-speech-read.png', 'R3 — a speech (111) read before, whole',
           'Rance\'s line as it shows when the scene is played again (a load, a second game).')


def r4():
    sky = COLOURS[CHOSEN][1]
    c = Canvas(os.path.join(SHOTS, '00020-key.png'))
    thought(c, read(THOUGHT, dim(sky, THOUGHT_DIM), QUIET))
    c.save('R4-thought-read.png', 'R4 — a thought (112) read before: the colour dimmed as a thought\'s text is',
           'A thought keeps its quieter look: the chosen colour at the strength Quiet has against the speech\'s text.')


def r5():
    sky = COLOURS[CHOSEN][1]
    c = Canvas(os.path.join(SHOTS, '00003-key.png'))
    narration(c, read(NARRATION, sky, NARR_TEXT, 3))
    c.save('R5-narration.png', 'R5 — narration (113): three lines read, the last new',
           'Narration\'s browner lacquer and its own text colour for the new line; read lines in the chosen colour.')


def r6():
    """The nine colours side by side on the same page, Maris's speech read whole, 'Don't change' first."""
    tiles = [("Don't change", None)] + COLOURS
    base = Image.open(os.path.join(SHOTS, '00015-key.png')).convert('RGB').resize((1280, 800), Image.LANCZOS)
    sheet = Image.new('RGBA', (1280, 800), (23, 18, 14, 255))
    tw, th = 412, 150
    for i, (name, col) in enumerate(tiles):
        c = Canvas(os.path.join(SHOTS, '00015-key.png'))
        speech_right(c, read(MARIS, col, TEXT) if col else read(MARIS, TEXT, TEXT))
        x, y, w, h = R121
        crop = c.image().crop((x - 8, y - 8, x + w + 8, y + h + 8)).resize((tw, int(tw * (h + 16) / (w + 16))), Image.LANCZOS)
        gx, gy = 16 + (i % 3) * (tw + 8), 12 + (i // 3) * (th + 46)
        sheet.alpha_composite(crop, (gx, gy))
        ImageDraw.Draw(sheet).text((gx + 4, gy + crop.size[1] + 6), name, font=ImageFont.truetype(
            REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf', 16), fill=NAME)
    c = Canvas(os.path.join(SHOTS, '00015-key.png'))
    c.base = sheet
    c.ov = Image.new('RGBA', (1280 * S, 800 * S), (0, 0, 0, 0))
    c.save('R6-colours.png', 'R6 — the nine colours on a whole page read before, "Don\'t change" first',
           'The colours after Rance X\'s; each against the lacquer and the cream of the new text.')
    del base


for f in (r1, r2, r3, r4, r5, r6):
    f()
print('wrote', len(os.listdir(OUT)), 'images to', OUT)
