"""Mock-ups of the message log (docs/ui/messages.md, §5 step 6): four ways to read back the pages of a scene (L1-L4) and
the variants of the chosen L3 (L3a-L3e), drawn in the lacquer and gold of godot/Strategy/Lacquer.cs over a screen dump of
the kept save saves-week26 (Maris's scene at the week's start, Gﾏﾘｽ.ADV:400-470, the page "I believe that we should
first..." open; L3d holds GAL.ADV's church question).

The dumps (headless, masked; the same run as tools/ui/mock_messages.py's):
  <Godot console exe> --headless --path godot -- --load --auto --fast --quit --seed 2
      --saves user://saves-look-log --slots-from user://saves-week26 --until deployment --shots <shots dir>
Then, from the repository's root (Pillow; the fonts of Windows and godot/fonts; the portraits from the extract):
  python -I tools/ui/mock_log.py <shots dir> <cg_png dir> <out dir> < /dev/null
The images hold the player's CGs: keep <out dir> outside the repository.
Everything is in the 1280x800 screen's pixels; drawn at 2x and scaled down for smooth edges.
"""
import sys, os, math
from PIL import Image, ImageDraw, ImageFont

SHOTS, CGS, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
WIN = 'C:/Windows/Fonts/'

PANEL = (22, 16, 12, 255)
GOLD = (196, 160, 86, 255)
DIM_GOLD = (110, 86, 46, 255)
NAME = (232, 199, 122, 255)
SUBTITLE = (160, 140, 104, 255)
LABEL = (150, 134, 108, 255)
TEXT = (244, 232, 206, 255)
OLD_TEXT = (214, 202, 178, 255)
QUIET = (176, 160, 130, 255)
NARR = (52, 38, 24, 255)
NARR_TEXT = (226, 204, 150, 255)
ROW = (34, 26, 20, 255)
PAPER = (224, 208, 172, 255)
PAPER_HEAD = (196, 176, 136, 255)
PAPER_RULE = (150, 120, 80, 110)
INK = (40, 28, 18, 255)
INK_QUIET = (100, 80, 56, 255)
VERMILION = (150, 52, 30, 255)
HEAD_INK = (120, 40, 26, 255)


def font(path, size, weight=None):
    f = ImageFont.truetype(path, int(size * S))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font(WIN + 'msgothic.ttc', size)
CINZEL = lambda size, w=500: font(REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf', size, w)
BRUSH = lambda size: font(REPO + '/godot/fonts/Shojumaru-Regular.ttf', size)


class Canvas:
    def __init__(self, shot):
        self.base = Image.open(os.path.join(SHOTS, shot)).convert('RGB').resize((1280, 800), Image.LANCZOS).convert('RGBA')
        self.ov = Image.new('RGBA', (1280 * S, 800 * S), (0, 0, 0, 0))

    def draw(self):
        lay = Image.new('RGBA', self.ov.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay, clip=None):
        if clip is not None:
            x0, y0, x1, y1 = clip
            mask = Image.new('L', lay.size, 0)
            ImageDraw.Draw(mask).rectangle(s(x0, y0, x1, y1), fill=255)
            alpha = Image.composite(lay.getchannel('A'), Image.new('L', lay.size, 0), mask)
            lay.putalpha(alpha)
        self.ov = Image.alpha_composite(self.ov, lay)

    def dim(self, amount):
        lay = Image.new('RGBA', self.ov.size, (0, 0, 0, int(255 * amount)))
        self.add(lay)

    def save(self, name):
        img = Image.alpha_composite(self.base, self.ov.resize((1280, 800), Image.LANCZOS))
        img.convert('RGB').save(os.path.join(OUT, name))


def s(*v):
    return [x * S for x in v]


def pts(p):
    return [(x * S, y * S) for x, y in p]


def shadow(c, x, y, w, h):
    for i in range(6, 0, -1):
        lay, d = c.draw()
        d.rectangle(s(x - i + 2, y - i + 6, x + w + i + 2, y + h + i + 6), fill=(0, 0, 0, 15))
        c.add(lay)


def corners(d, x, y, w, h, colour=GOLD):
    for cx, cy, sx, sy in [(x, y, 1, 1), (x + w, y, -1, 1), (x, y + h, 1, -1), (x + w, y + h, -1, -1)]:
        d.line(pts([(cx + sx * 9, cy + sy * 2), (cx + sx * 9, cy + sy * 9), (cx + sx * 2, cy + sy * 9)]), fill=colour, width=2 * S)
        d.polygon(pts([(cx + sx * 9, cy + sy * 14), (cx + sx * 14, cy + sy * 9), (cx + sx * 9, cy + sy * 4), (cx + sx * 4, cy + sy * 9)]), fill=colour)


def panel(c, x, y, w, h, fill=PANEL):
    """Lacquer.DrawPanel."""
    shadow(c, x, y, w, h)
    lay, d = c.draw()
    d.rectangle(s(x, y, x + w, y + h), fill=fill)
    d.rectangle(s(x + 1, y + 1, x + w - 1, y + h - 1), outline=GOLD, width=2 * S)
    d.rectangle(s(x + 5.5, y + 5.5, x + w - 5.5, y + h - 5.5), outline=DIM_GOLD, width=1 * S)
    corners(d, x, y, w, h)
    c.add(lay)


def speech_tail(c, edge_x, y0, y1, tip, side, fill=PANEL):
    lay, d = c.draw()
    inward = -7 if side == 'right' else 7
    d.polygon(pts([(edge_x + inward, y0 + 2), (edge_x + inward, y1 - 2), tip]), fill=fill[:3] + (255,))
    d.polygon(pts([(edge_x, y0), (edge_x, y1), tip]), fill=fill[:3] + (255,))
    d.line(pts([(edge_x, y0), tip]), fill=GOLD, width=2 * S)
    d.line(pts([(edge_x, y1), tip]), fill=GOLD, width=2 * S)
    ix = edge_x + (-5.5 if side == 'right' else 5.5)
    d.line(pts([(ix, y0 + 1), (ix, y1 - 1)]), fill=fill[:3] + (255,), width=3 * S)
    c.add(lay)


def triangle(c, x, y, down=True, colour=GOLD):
    lay, d = c.draw()
    p = [(x - 8, y - 6), (x + 8, y - 6), (x, y + 7)] if down else [(x - 8, y + 6), (x + 8, y + 6), (x, y - 7)]
    d.polygon(pts(p), fill=colour)
    d.line(pts(p + [p[0]]), fill=NAME, width=1 * S)
    c.add(lay)


def text(d, x, y, s_, f, colour, bold=False, anchor='la'):
    d.text((x * S, y * S), s_, font=f, fill=colour, anchor=anchor, stroke_width=(1 if bold else 0), stroke_fill=colour)


def divider(d, x0, x1, y, colour=DIM_GOLD, diamond=GOLD):
    m = (x0 + x1) / 2
    d.line(pts([(x0, y), (m - 12, y)]), fill=colour, width=1 * S)
    d.line(pts([(m + 12, y), (x1, y)]), fill=colour, width=1 * S)
    d.polygon(pts([(m, y - 5), (m + 5, y), (m, y + 5), (m - 5, y)]), fill=diamond)


# ------------------------------------------------------------------ the portraits

FACE = {1: (33, 7, 128)}          # UnitFaces.Exceptions (Rance); the rest UnitFaces.Default
PLATE = (48, 220, 190, 254)       # the name plate baked into a portrait's bottom right (every WAO_SUB portrait, 192x256)


def cg(n):
    return Image.open(os.path.join(CGS, f'CG{n:04d}.png')).convert('RGBA')


def face(n, size):
    x, y, w = FACE.get(n, (24, 7, 144))
    return cg(n).crop((x, y, x + w, y + w)).resize((size * S, size * S), Image.LANCZOS)


def plate(n, scale=1.0):
    im = cg(n).crop(PLATE)
    return im.resize((int(im.width * scale * S), int(im.height * scale * S)), Image.LANCZOS)


def paste(lay, im, x, y, alpha=1.0):
    if alpha < 1:
        im = im.copy()
        im.putalpha(im.getchannel('A').point(lambda a: int(a * alpha)))
    lay.alpha_composite(im, (int(x * S), int(y * S)))


# ------------------------------------------------------------------ the scene's pages (Gﾏﾘｽ.ADV:400-470)

RANCE, LIA, MARIS = (1, 'Rance'), (5, 'Lia'), (6, 'Maris')
PAGES = [
    ('place', 'Leazas Castle', 'Audience Room'),
    ('narr', None, ['Rance was lying on the carpet', 'in front of the throne, with', 'Lia sitting and drinking tea', 'at his side.']),
    ('say', RANCE, ['I go to the trouble of', 'becoming king, and these', 'people start a rebellion on me.', 'Idiots, all of them.']),
    ('say', LIA, ['That’s true, Darling.', 'Sorry.']),
    ('say', RANCE, ['There’s no reason for you to', 'apologize.']),
    ('say', LIA, ['But... If I could do', 'something...']),
    ('say', MARIS, ['King Rance.', 'May I interrupt?']),
    ('narr', None, ['Maris showed up. She was', 'holding a document in one hand.']),
    ('say', RANCE, ['Yeah, what is it?']),
    ('say', MARIS, ['I’d like to give my opinion on', 'how to deal with the rebel', 'army.']),
    ('say', RANCE, ['Alright, go ahead.']),
    ('say', MARIS, ['Thank you very much.']),
    ('say', MARIS, ['Sir Ex is an excellent', 'commander. I believe it would', 'be to our disadvantage to', 'fight him head on.']),
    ('say', MARIS, ['I believe that we should first', 'prepare our cities for their', 'attacks, then defend them with', 'urban warfare.']),
]

R121 = (58, 512, 822, 254)


def current_window(c, lines=PAGES[-1][2]):
    """Our 121 with the open page, under the log (step 1's panel)."""
    x, y, w, h = R121
    panel(c, x, y, w, h)
    speech_tail(c, x + w, y + 150, y + 196, (x + w + 30, y + 188), 'right')
    lay, d = c.draw()
    f = GOTHIC(48)
    for i, line in enumerate(lines):
        text(d, x + 30, y + 22 + i * 46, line, f, TEXT, bold=True)
    c.add(lay)


def runs(pages):
    """Consecutive pages of one speaker as one run."""
    out = []
    for p in pages:
        if out and p[0] == 'say' and out[-1][0] == 'say' and out[-1][1] == p[1]:
            out[-1][2].append(p[2])
        else:
            out.append((p[0], p[1], [p[2]] if p[0] in ('say', 'narr') else p[2]))
    return out


def scrollbar(d, x, y0, y1, at, size):
    d.rounded_rectangle(s(x - 3, y0, x + 3, y1), radius=3 * S, fill=ROW)
    h = (y1 - y0) * size
    top = y0 + (y1 - y0 - h) * at
    d.rounded_rectangle(s(x - 4, top, x + 4, top + h), radius=4 * S, fill=DIM_GOLD, outline=GOLD, width=1 * S)


def log_panel(c, title, hint, at=1.0):
    x, y, w, h = 190, 28, 900, 744
    panel(c, x, y, w, h)
    lay, d = c.draw()
    text(d, x + 36, y + 22, title, BRUSH(30), NAME)
    text(d, x + w - 36, y + 34, hint, CINZEL(15, 500), LABEL, anchor='ra')
    divider(d, x + 30, x + w - 30, y + 70)
    scrollbar(d, x + w - 24, y + 92, y + h - 24, at, 0.34)
    c.add(lay)
    return (x + 24, y + 80, x + w - 40, y + h - 12)


# ------------------------------------------------------------------ L1: the scroll, faces and the portraits' own plates

def l1(at_top=False):
    c = Canvas('00015-key.png')
    current_window(c)
    c.dim(0.55)
    clip = log_panel(c, 'Message Log', 'WHEEL  SCROLLS   ·   RIGHT CLICK  CLOSES', 0.0 if at_top else 1.0)
    lay, d = c.draw()
    f = GOTHIC(28)
    pitch, gap = 32, 12
    fx, fs, tx = 226, 80, 330
    items = runs(PAGES)
    def height(it):
        kind, who, body = it
        if kind == 'place':
            return 70
        lines = sum(len(p) for p in body) * pitch + (len(body) - 1) * gap
        if kind == 'narr':
            return lines + 20
        return max(fs + 8, 52 + lines)
    placed = []
    if at_top:                                   # scrolled to the scene's start
        y = clip[1] + 18
        for i, it in enumerate(items):
            placed.append((y, it, i == len(items) - 1))
            y += height(it) + 22
    else:                                        # opened at the newest page, laid out from the bottom up
        y = clip[3] - 14
        for i, it in enumerate(reversed(items)):
            y -= height(it)
            placed.append((y, it, i == 0))
            y -= 22
    for top, (kind, who, body), newest in placed:
        if kind == 'place':
            divider(d, 280, 1000, top + 38)
            text(d, 640, top + 28, who.upper(), CINZEL(22, 600), NAME, anchor='ms')
            text(d, 640, top + 62, body, CINZEL(16, 500), SUBTITLE, anchor='ms')
            continue
        if kind == 'narr':
            n = sum(len(p) for p in body)
            d.rounded_rectangle(s(tx - 14, top, 1040, top + n * pitch + 18), radius=8 * S, fill=NARR[:3] + (200,))
            for j, line in enumerate(l for p in body for l in p):
                text(d, tx, top + 9 + j * pitch, line, f, NARR_TEXT, bold=True)
            continue
        n, name = who
        paste(lay, face(n, fs), fx, top + 4)
        d.rectangle(s(fx - 1, top + 3, fx + fs, top + 4 + fs), outline=GOLD, width=1 * S)
        paste(lay, plate(n, 1.3), tx - 6, top - 2)
        yy = top + 52
        for k, page in enumerate(body):
            last = newest and k == len(body) - 1
            if last:
                d.rectangle(s(tx - 16, yy - 2, tx - 12, yy + len(page) * pitch - 4), fill=GOLD)
            for line in page:
                text(d, tx, yy, line, f, TEXT if last else OLD_TEXT, bold=True)
                yy += pitch
            if k < len(body) - 1:
                d.line(pts([(tx, yy + gap / 2 - 2), (tx + 40, yy + gap / 2 - 2)]), fill=DIM_GOLD, width=1 * S)
            yy += gap
    c.add(lay, clip=clip)
    c.save('L1b-scroll-faces-start.png' if at_top else 'L1-scroll-faces.png')


# ------------------------------------------------------------------ L2: compact, names in our letters

def l2():
    c = Canvas('00015-key.png')
    current_window(c)
    c.dim(0.55)
    clip = log_panel(c, 'Message Log', 'WHEEL  SCROLLS   ·   RIGHT CLICK  CLOSES')
    lay, d = c.draw()
    f = GOTHIC(28)
    pitch = 32
    nx, tx = 372, 396
    rows = []          # (kind, name or None, lines, newest)
    prev = None
    for p in PAGES:
        if p[0] == 'place':
            rows.append(('place', None, p, False))
            prev = None
            continue
        name = p[1][1] if p[0] == 'say' else None
        rows.append((p[0], name if name != prev else '', p[2], False))
        prev = name
    rows[-1] = rows[-1][:3] + (True,)
    rows.insert(0, ('week', None, None, False))
    def height(r):
        return {'place': 66, 'week': 50}.get(r[0], len(r[2] or []) * pitch + 14)
    y = clip[3] - 12
    placed = []
    for r in reversed(rows):
        y -= height(r)
        placed.append((y, r))
    for top, (kind, name, lines, newest) in placed:
        if kind == 'week':
            text(d, 640, top + 32, 'WEEK 26', CINZEL(18, 600), LABEL, anchor='ms')
            continue
        if kind == 'place':
            divider(d, 280, 1000, top + 34)
            text(d, 640, top + 24, 'LEAZAS CASTLE', CINZEL(20, 600), NAME, anchor='ms')
            text(d, 640, top + 58, 'Audience Room', CINZEL(15, 500), SUBTITLE, anchor='ms')
            continue
        if name:
            text(d, nx, top + 4, name.upper(), CINZEL(19, 700), NAME, anchor='ra')
            d.line(pts([(tx - 12, top + 2), (tx - 12, top + len(lines) * pitch - 2)]), fill=DIM_GOLD, width=1 * S)
        elif kind == 'say':
            d.line(pts([(tx - 12, top + 2), (tx - 12, top + len(lines) * pitch - 2)]), fill=DIM_GOLD, width=1 * S)
        if newest:
            d.rectangle(s(tx - 14, top, tx - 10, top + len(lines) * pitch - 4), fill=GOLD)
        colour = NARR_TEXT if kind == 'narr' else (TEXT if newest else OLD_TEXT)
        for j, line in enumerate(lines):
            text(d, tx, top + j * pitch, line, f, colour, bold=True)
    c.add(lay, clip=clip)
    c.save('L2-compact-names.png')


# ------------------------------------------------------------------ L3: the chronicle, a paper page

FADED_RULE = (150, 120, 80, 45)   # the owner (2026-10-19): the rules a little paler than the ledger's

# GAL.ADV:900-1000 (the church's donations, week 21): Maris's advice, Rance, the question (Keep Donating chosen).
GAL = [
    ('narr', None, ['Salbenaot bowed very deeply to', 'Rance.']),
    ('say', MARIS, ['King Rance...', 'Our country’s not the only one', 'that has ALists.']),
    ('say', MARIS, ['Helman and Zeth both have a', 'large number of them as well,', 'and donate a huge sum of money', 'to the AL Church.']),
    ('say', MARIS, ['Most of the population is', 'AList, so going against the', 'church would be very bad for', 'our public order.']),
    ('say', MARIS, ['All we can do is continue to', 'pay them as we have.']),
    ('say', MARIS, ['Is that all right with you,', 'King Rance?']),
    ('narr', None, ['Maris whispered secretly into', 'Rance’s ear.']),
    ('say', RANCE, ['Hmmmmmm.']),
    ('choice', None, (['Keep Donating', 'Stop Donating'], 0)),
]


def chronicle_lines(pages, gaps):
    """The pages as ruled lines: (kind, margin label — a name or a portrait's CG —, text, newest)."""
    lines, prev = [], None
    for i, (kind, who, body) in enumerate(pages):
        newest = i == len(pages) - 1
        speaker = who if kind == 'say' else None
        new_run = kind != 'say' or speaker != prev
        if gaps and lines and new_run:
            lines.append(('gap', None, '', False))
        if kind == 'place':
            lines.append(('place', None, f'{who} — {body}', False))
        elif kind == 'choice':
            items, chosen = body
            for j, item in enumerate(items):
                lines.append(('chosen' if j == chosen else 'unchosen', 'CHOICE' if j == 0 else None, item, newest))
        else:
            for j, l in enumerate(body):
                lines.append((kind, speaker if (j == 0 and new_run) else None, l, newest))
        prev = speaker
    return lines


def l3(name, pages=PAGES, rule=FADED_RULE, gaps=False, plates=False, mark=False, at_top=False, hint=True):
    c = Canvas('00015-key.png')
    current_window(c)
    c.dim(0.55)
    x, y, w, h = 190, 28, 900, 744
    panel(c, x, y, w, h)
    lay, d = c.draw()
    px0, py0, px1, py1 = x + 18, y + 18, x + w - 18, y + h - 18
    d.rectangle(s(px0, py0, px1, py1), fill=PAPER)
    d.rectangle(s(px0, py0, px1, py0 + 58), fill=PAPER_HEAD)
    text(d, px0 + 24, py0 + 14, 'CHRONICLE', CINZEL(26, 700), HEAD_INK)
    if hint:
        text(d, px1 - 24, py0 + 22, 'WHEEL SCROLLS   ·   RIGHT CLICK CLOSES', CINZEL(14, 600), INK_QUIET, anchor='ra')
    c.add(lay)
    lay, d = c.draw()                          # the rules on a layer of their own, so their alpha blends over the paper
    margin = px0 + 200
    d.line(pts([(margin, py0 + 58), (margin, py1)]), fill=VERMILION[:3] + (110,), width=1 * S)
    f = GOTHIC(28)
    pitch = 34
    body_top = py0 + 70
    for ry in range(int(body_top + pitch - 4), int(py1), pitch):
        d.line(pts([(px0 + 8, ry), (px1 - 8, ry)]), fill=rule, width=1 * S)
    c.add(lay)
    lay, d = c.draw()
    lines = chronicle_lines(pages, gaps)
    visible = int((py1 - body_top) // pitch)
    shown = lines[:visible] if at_top else lines[-visible:]
    for i, (kind, label, l, newest) in enumerate(shown):
        ly = body_top + i * pitch
        if kind == 'gap':
            continue
        if kind == 'place':
            text(d, (margin + px1) / 2, ly + 24, l.upper(), CINZEL(18, 700), HEAD_INK, anchor='ms')
            continue
        if label == 'CHOICE':
            text(d, margin - 16, ly + 9, 'CHOICE', CINZEL(15, 700), INK_QUIET, anchor='ra')
        elif label and plates:
            im = plate(label[0], 1.0)
            paste(lay, im, margin - 10 - im.width / S, ly - 3)
        elif label:
            text(d, margin - 16, ly + 8, label[1].upper(), CINZEL(18, 700), VERMILION, anchor='ra')
        if mark and newest:
            d.rectangle(s(margin + 4, ly + 1, margin + 8, ly + pitch - 5), fill=VERMILION)
        if kind == 'chosen':
            text(d, margin + 18, ly + 2, '◆ ' + l, f, VERMILION, bold=True)
        elif kind == 'unchosen':
            text(d, margin + 18, ly + 2, '◇ ' + l, f, (150, 132, 104, 255))
        elif kind == 'narr':
            text(d, margin + 18 + (28 if gaps else 0), ly + 2, l, f, INK_QUIET)
        else:
            text(d, margin + 18, ly + 2, l, f, INK, bold=True)
    d.rectangle(s(px1 - 10, body_top, px1 - 6, py1 - 8), fill=PAPER_HEAD)
    thumb = (body_top, body_top + 222) if at_top else (py1 - 230, py1 - 8)
    d.rectangle(s(px1 - 11, thumb[0], px1 - 5, thumb[1]), fill=VERMILION[:3] + (180,))
    c.add(lay)
    c.save(name)


# ------------------------------------------------------------------ L4: back in the window itself

def l4():
    c = Canvas('00015-key.png')
    x, y, w, h = R121
    back = (34, 28, 30, 255)
    panel(c, x, y, w, h, fill=back)          # no tail: the page is not the present portrait's
    lay, d = c.draw()
    for i, line in enumerate(PAGES[3][2]):
        text(d, x + 30, y + 34 + i * 46, line, GOTHIC(48), QUIET, bold=True)
    # the logged speaker's own plate on the top edge (the portrait on the screen is the present one's)
    paste(lay, plate(5, 1.3), x + 24, y - 24)
    # the page's place in the log on the top edge, the way back at the bottom
    lw = 210
    d.rectangle(s(x + w - lw - 24, y - 16, x + w - 24, y + 16), fill=PANEL, outline=GOLD, width=1 * S)
    text(d, x + w - 24 - lw / 2, y + 8, 'LOG   4 / 14', CINZEL(17, 700), NAME, anchor='ms')
    text(d, x + w - 70, y + h - 26, 'WHEEL UP  OLDER   ·   DOWN  NEWER   ·   CLICK  NOW', CINZEL(13, 600), LABEL, anchor='rs')
    c.add(lay)
    triangle(c, x + w - 40, y + h - 46, down=False)
    triangle(c, x + w - 40, y + h - 24, down=True)
    c.save('L4-back-in-window.png')


l1()
l1(at_top=True)
l2()
l3('L3-chronicle-paper.png', rule=(150, 120, 80, 110))
# The owner chose L3 with paler rules (2026-10-19); its variants:
l3('L3a-faded-rules.png')
l3('L3b-gaps-mark.png', gaps=True, mark=True)
l3('L3c-portrait-plates.png', gaps=True, plates=True, mark=True)
l3('L3d-choice.png', pages=GAL, gaps=True, mark=True)
l3('L3e-scene-start.png', gaps=True, at_top=True)
l4()
print('done')
