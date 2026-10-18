"""Mock-ups of the message windows, variant A (docs/ui/messages.md, §4): the original's windows redrawn in
the lacquer and gold of godot/Strategy/Lacquer.cs over screen dumps of the kept save saves-week26.

The dumps (Lia's and Maris's scene at the week's start, Cﾌﾞｶ's menu; headless, masked):
  <Godot console exe> --headless --path godot -- --load --auto --fast --quit --seed 2
      --saves user://saves-look-msg --slots-from user://saves-week26 --until deployment --shots <shots dir>
Then, from the repository's root (Pillow; the fonts of Windows and godot/fonts):
  python -I tools/ui/mock_messages.py <shots dir> <out dir> < /dev/null
Window 131 (A-7 to A-9) over a harem scene of the kept save saves-week18 (Kanami, the picker's cell 6;
00029-key.png is a key wait with 131 open, the CG masked):
  <Godot console exe> --headless --path godot -- --load --auto --fast --quit --seed 2
      --saves user://saves-look-131 --slots-from user://saves-week18 --harem 6 --until deployment --shots <harem dir>
  python -I tools/ui/mock_messages.py <shots dir> <out dir> <harem dir> 00029-key.png < /dev/null
The images hold the player's CGs: keep <out dir> outside the repository.
Everything is in the 1280x800 screen's pixels; drawn at 2x and scaled down for smooth edges.
"""
import sys, os, math, re
from PIL import Image, ImageDraw, ImageFont

SHOTS, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
WIN = 'C:/Windows/Fonts/'

PANEL = (22, 16, 12, 255)  # opaque in the mock-ups: the original window under it is not drawn in the game
GOLD = (196, 160, 86, 255)
DIM_GOLD = (110, 86, 46, 255)
NAME = (232, 199, 122, 255)
SUBTITLE = (160, 140, 104, 255)
LABEL = (150, 134, 108, 255)
VALUE = (236, 226, 204, 255)
ROW = (34, 26, 20, 255)
ROW_HOVER = (64, 48, 28, 255)
HOVER_LINE = (222, 186, 104, 255)
TEXT = (244, 232, 206, 255)
QUIET = (176, 160, 130, 255)
OFF_TEXT = (98, 88, 74, 255)
NARR = (52, 38, 24, 255)        # narration (113): a warmer, browner lacquer than speech
NARR_TEXT = (226, 204, 150, 255)


def font(path, size, weight=None):
    f = ImageFont.truetype(path, size * S)
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font(WIN + 'msgothic.ttc', size)
SERIF = lambda size: font(WIN + 'cambria.ttc', size)  # lining figures, for the table lines
SERIF_I = lambda size: font(WIN + 'georgiai.ttf', size)
CINZEL = lambda size, w=500: font(REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf', size, w)
BRUSH = lambda size: font(REPO + '/godot/fonts/Shojumaru-Regular.ttf', size)


class Canvas:
    def __init__(self, shot, dim=0.0):
        base = Image.open(os.path.join(SHOTS, shot)).convert('RGB').resize((1280, 800), Image.LANCZOS).convert('RGBA')
        if dim:
            base = Image.alpha_composite(base, Image.new('RGBA', base.size, (0, 0, 0, int(255 * dim))))
        self.base = base
        self.ov = Image.new('RGBA', (1280 * S, 800 * S), (0, 0, 0, 0))

    def draw(self):
        lay = Image.new('RGBA', self.ov.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay):
        self.ov = Image.alpha_composite(self.ov, lay)

    def save(self, name):
        img = Image.alpha_composite(self.base, self.ov.resize((1280, 800), Image.LANCZOS))
        img.convert('RGB').save(os.path.join(OUT, name))
        return img


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


def speech_tail(c, edge_x, y0, y1, tip, side):
    """A tail out of the panel's side: lacquer over the frame between y0 and y1, gold along its two edges."""
    lay, d = c.draw()
    inward = -7 if side == 'right' else 7
    d.polygon(pts([(edge_x + inward, y0 + 2), (edge_x + inward, y1 - 2), tip]), fill=PANEL[:3] + (255,))
    d.polygon(pts([(edge_x, y0), (edge_x, y1), tip]), fill=PANEL[:3] + (255,))
    d.line(pts([(edge_x, y0), tip]), fill=GOLD, width=2 * S)
    d.line(pts([(edge_x, y1), tip]), fill=GOLD, width=2 * S)
    # the inner dim frame stops at the tail too
    ix = edge_x + (-5.5 if side == 'right' else 5.5)
    d.line(pts([(ix, y0 + 1), (ix, y1 - 1)]), fill=PANEL[:3] + (255,), width=3 * S)
    c.add(lay)


def thought_panel(c, x, y, w, h, bubbles):
    """A thought: dimmer lacquer, a dashed dim-gold frame, no corner diamonds, a trail of bubbles."""
    shadow(c, x, y, w, h)
    lay, d = c.draw()
    fill = (30, 26, 30, 255)
    d.rounded_rectangle(s(x, y, x + w, y + h), radius=18 * S, fill=fill)
    # dashed frame on a rounded rect: dashes along the straight parts, arcs at the corners
    r = 18
    dash, gap = 14, 8
    def dashes(a, b):
        (ax, ay), (bx, by) = a, b
        n = math.hypot(bx - ax, by - ay)
        t = 0
        while t < n:
            e = min(t + dash, n)
            d.line(pts([(ax + (bx - ax) * t / n, ay + (by - ay) * t / n), (ax + (bx - ax) * e / n, ay + (by - ay) * e / n)]), fill=GOLD, width=2 * S)
            t += dash + gap
    dashes((x + r, y + 1), (x + w - r, y + 1))
    dashes((x + r, y + h - 1), (x + w - r, y + h - 1))
    dashes((x + 1, y + r), (x + 1, y + h - r))
    dashes((x + w - 1, y + r), (x + w - 1, y + h - r))
    for bx, by, start in [(x + 1, y + 1, 180), (x + w - 2 * r - 1, y + 1, 270), (x + 1, y + h - 2 * r - 1, 90), (x + w - 2 * r - 1, y + h - 2 * r - 1, 0)]:
        d.arc(s(bx, by, bx + 2 * r, by + 2 * r), start, start + 90, fill=GOLD, width=2 * S)
    for (bx, by, br) in bubbles:
        d.ellipse(s(bx - br, by - br, bx + br, by + br), fill=fill[:3] + (240,), outline=GOLD, width=2 * S)
    c.add(lay)


def triangle(c, x, y, phase=0.0):
    """The click's triangle (WeekReportView.DrawTriangle), drawn at one phase of its bob."""
    bob = math.sin(phase * math.pi * 2) * 3
    alpha = int(255 * (0.65 + 0.35 * math.sin(phase * math.pi * 2 + 1)))
    lay, d = c.draw()
    p = [(x - 8, y - 6 + bob), (x + 8, y - 6 + bob), (x, y + 7 + bob)]
    d.polygon(pts(p), fill=GOLD[:3] + (alpha,))
    d.line(pts(p + [p[0]]), fill=NAME[:3] + (alpha,), width=1 * S)
    c.add(lay)


def text(c, x, y, s_, f, colour, bold=False, anchor='la'):
    lay, d = c.draw()
    d.text((x * S, y * S), s_, font=f, fill=colour, anchor=anchor, stroke_width=(1 if bold else 0), stroke_fill=colour)
    c.add(lay)


def mono_lines(c, x, y, lines, size=48, pitch=46, colour=TEXT):
    """The text as the engine places it now: MS Gothic, half-width cells, the script's own line breaks."""
    f = GOTHIC(size)
    for i, line in enumerate(lines):
        text(c, x, y + i * pitch, line, f, colour, bold=True)


def wrap(f, words, width):
    lines, cur = [], ''
    for w in words:
        trial = (cur + ' ' + w).strip()
        if f.getlength(trial) / S <= width or not cur:
            cur = trial
        else:
            lines.append(cur)
            cur = w
    if cur:
        lines.append(cur)
    return lines


def flow(c, x, y, lines, width, f, pitch, colour):
    """The script's lines joined into one paragraph and wrapped to the panel (BattleSpeech.Flow)."""
    out = wrap(f, ' '.join(l.strip() for l in lines).split(), width)
    for i, line in enumerate(out):
        text(c, x, y + i * pitch, line, f, colour)
    return out


# The windows' places (screen px). 121: the right speaker's box; 111/112: the left speaker's; 113: narration.
R121 = (58, 512, 822, 254)
R111 = (436, 4, 834, 254)
R113 = (248, 536, 784, 258)

MARIS = ['I believe that we should first', 'prepare our cities for their', 'attacks, then defend them with', 'urban warfare.']
RANCE = ['I go to the trouble of', 'becoming king, and these', 'people start a rebellion on me.', 'Idiots, all of them.']
THOUGHT = ['Mhm, I see. Maybe that would', "work... I'd expect no less of", 'the person that governs this', 'country from the shadows.']
NARRATION = ['Rance was lying on the carpet', 'in front of the throne, with', 'Lia sitting and drinking tea', 'at his side.']
STATUS = ["Barres's unit.", 'What will you do?', 'HP    4/    5', 'Troops   102/ 1000']


def speech_right(shot, lines, name, mode='mono'):
    c = Canvas(shot)
    x, y, w, h = R121
    panel(c, x, y, w, h)
    speech_tail(c, x + w, y + 150, y + 196, (x + w + 30, y + 188), 'right')
    if mode == 'mono':
        mono_lines(c, x + 30, y + 22, lines)
    else:
        flow(c, x + 34, y + 30, lines, w - 76, SERIF(38), 50, TEXT)
    triangle(c, x + w - 30, y + h - 28)
    return c.save(name)


def speech_left(shot, lines, name):
    c = Canvas(shot)
    x, y, w, h = R111
    panel(c, x, y, w, h)
    speech_tail(c, x, y + 96, y + 142, (x - 30, y + 104), 'left')
    mono_lines(c, x + 34, y + 22, lines)
    triangle(c, x + w - 30, y + h - 28)
    return c.save(name)


def thought(shot, lines, name):
    c = Canvas(shot)
    x, y, w, h = R111
    thought_panel(c, x, y, w, h, [(x - 12, y + 128, 12), (x - 34, y + 150, 8), (x - 50, y + 166, 5)])
    mono_lines(c, x + 34, y + 22, lines, colour=QUIET)
    triangle(c, x + w - 30, y + h - 28)
    return c.save(name)


def narration(shot, lines, name):
    c = Canvas(shot)
    x, y, w, h = R113
    panel(c, x, y, w, h, fill=NARR)
    mono_lines(c, x + 32, y + 22, lines, colour=NARR_TEXT)
    triangle(c, x + w - 30, y + h - 28)
    return c.save(name)


def place_title(shot, name):
    """113 at ZM 32 (a location's title): Cinzel, centred, the two lines parted by a divider."""
    c = Canvas(shot)
    x, y, w, h = R113
    panel(c, x, y, w, h, fill=NARR)
    cx = x + w / 2
    text(c, cx, y + 92, 'LEAZAS CASTLE', CINZEL(50, 600), NAME, anchor='ms')
    lay, d = c.draw()
    m, ly = cx, y + 128
    d.line(pts([(x + 160, ly), (m - 12, ly)]), fill=DIM_GOLD, width=1 * S)
    d.line(pts([(m + 12, ly), (x + w - 160, ly)]), fill=DIM_GOLD, width=1 * S)
    d.polygon(pts([(m, ly - 5), (m + 5, ly), (m, ly + 5), (m - 5, ly)]), fill=GOLD)
    c.add(lay)
    text(c, cx, y + 186, 'Audience Room', CINZEL(34, 500), SUBTITLE, anchor='ms')
    triangle(c, x + w - 30, y + h - 28)
    return c.save(name)


def menu(shot, name):
    """A ']' menu in our rows next to the window that asks; the ○ items carry a gold ring."""
    c = Canvas(shot)
    # the question's window (121) under it, as in the original
    x, y, w, h = R121
    panel(c, x, y, w, h)
    speech_tail(c, x + w, y + 150, y + 196, (x + w + 30, y + 188), 'right')
    mono_lines(c, x + 30, y + 22, STATUS)
    items = [('Info', False), ('Replenish Troops', True), ('Add New Troops', True), ('Talk', True), ('Do Nothing', False)]
    mx, my, mw, pitch, rh = 470, 70, 380, 44, 36
    mh = 28 + len(items) * pitch
    panel(c, mx, my, mw, mh)
    f = SERIF(26)
    for i, (label, ring) in enumerate(items):
        ry = my + 16 + i * pitch
        hover = i == 1
        lay, d = c.draw()
        d.rectangle(s(mx + 16, ry, mx + mw - 16, ry + rh), fill=ROW_HOVER if hover else ROW,
                    outline=HOVER_LINE if hover else DIM_GOLD, width=(2 if hover else 1) * S)
        if ring:
            cx_, cy_ = mx + 38, ry + rh / 2
            d.ellipse(s(cx_ - 7, cy_ - 7, cx_ + 7, cy_ + 7), outline=GOLD, width=2 * S)
        c.add(lay)
        text(c, mx + 60, ry + rh / 2, label, f, TEXT if hover else VALUE, anchor='lm')
    return c.save(name)


def compare():
    """Point 2: the same window in three ways of setting its text, plus a line with columns."""
    rows = []
    x, y, w, h = R121

    def cut(c, title):
        img = Image.alpha_composite(c.base, c.ov.resize((1280, 800), Image.LANCZOS))
        part = img.crop((x - 20, y - 20, x + w + 60, y + h + 14)).convert('RGB')
        band = Image.new('RGB', (part.width, part.height + 44), (16, 12, 10))
        band.paste(part, (0, 44))
        ImageDraw.Draw(band).text((14, 8), title, font=ImageFont.truetype(WIN + 'segoeui.ttf', 26), fill=(232, 199, 122))
        rows.append(band)

    def base():
        c = Canvas('00015-key.png')
        panel(c, x, y, w, h)
        speech_tail(c, x + w, y + 150, y + 196, (x + w + 30, y + 188), 'right')
        triangle(c, x + w - 30, y + h - 28)
        return c

    c = base(); mono_lines(c, x + 30, y + 22, MARIS)
    cut(c, '1. Now: monospace (MS Gothic), the script\'s line breaks')
    c = base()
    for i, line in enumerate(MARIS):
        text(c, x + 34, y + 30 + i * 50, line, SERIF(38), TEXT)
    cut(c, '2. Proportional font, the script\'s line breaks: ragged lines')
    c = base(); flow(c, x + 34, y + 30, MARIS, w - 76, SERIF(38), 50, TEXT)
    cut(c, '3. Proportional font, lines joined and rewrapped to the window')
    c = base()
    f = SERIF(38)
    # columns kept: a run after 2+ spaces starts at its original cell (19 px a cell at this size)
    for i, line in enumerate(STATUS):
        for m_ in re.finditer(r'\S+(?: \S+)*', line):
            text(c, x + 34 + m_.start() * 19, y + 30 + i * 50, m_.group(), f, TEXT)
    cut(c, '4. A table line (HP, Troops): kept as lines, each column at its cell')
    width = max(r.width for r in rows)
    sheet = Image.new('RGB', (width, sum(r.height for r in rows) + 6 * len(rows)), (8, 6, 5))
    yy = 0
    for r in rows:
        sheet.paste(r, (0, yy)); yy += r.height + 6
    sheet.save(os.path.join(OUT, 'A-point2-text.png'))


def speed_dialog():
    """Option → Text Speed…: a slider and a live preview window under it."""
    c = Canvas('00025-menu.png', dim=0.45)
    x, y, w, h = 300, 150, 680, 470
    panel(c, x, y, w, h)
    cx = x + w / 2
    text(c, cx, y + 58, 'Text Speed', BRUSH(34), NAME, anchor='ms')
    lay, d = c.draw()
    ly = y + 82
    d.line(pts([(x + 60, ly), (cx - 12, ly)]), fill=DIM_GOLD, width=1 * S)
    d.line(pts([(cx + 12, ly), (x + w - 60, ly)]), fill=DIM_GOLD, width=1 * S)
    d.polygon(pts([(cx, ly - 5), (cx + 5, ly), (cx, ly + 5), (cx - 5, ly)]), fill=GOLD)
    # the slider: 10 steps, the last one Instant
    tx0, tx1, ty = x + 90, x + w - 90, y + 150
    d.rounded_rectangle(s(tx0, ty - 4, tx1, ty + 4), radius=4 * S, fill=ROW, outline=DIM_GOLD, width=1 * S)
    knob = tx0 + (tx1 - tx0) * 6 / 9
    d.rounded_rectangle(s(tx0, ty - 4, knob, ty + 4), radius=4 * S, fill=(150, 118, 58, 255))
    for i in range(10):
        px = tx0 + (tx1 - tx0) * i / 9
        d.line(pts([(px, ty + 12), (px, ty + 20)]), fill=GOLD if i == 9 else DIM_GOLD, width=(2 if i == 9 else 1) * S)
    d.polygon(pts([(knob, ty - 16), (knob + 14, ty), (knob, ty + 16), (knob - 14, ty)]), fill=GOLD, outline=NAME)
    c.add(lay)
    small = CINZEL(18, 600)
    text(c, tx0, ty + 46, 'SLOW', small, LABEL, anchor='ms')
    text(c, tx1, ty + 46, 'INSTANT', small, NAME, anchor='ms')
    text(c, knob, ty - 30, '7', CINZEL(22, 700), NAME, anchor='ms')
    # the preview: a speech window as in the game, typing its line at the chosen speed, again and again
    text(c, x + 52, y + 208, 'PREVIEW', small, SUBTITLE)
    px, py, pw, ph = x + 50, y + 240, w - 100, 150
    lay, d = c.draw()
    d.rectangle(s(px, py, px + pw, py + ph), fill=(14, 10, 8, 255), outline=DIM_GOLD, width=1 * S)
    c.add(lay)
    bx, by, bw, bh = px + 24, py + 18, pw - 48, ph - 36
    panel(c, bx, by, bw, bh)
    shown = 'I believe that we should first prepare our ci'
    f = GOTHIC(26)
    lines = ['I believe that we should first', 'prepare our ci']
    for i, line in enumerate(lines):
        text(c, bx + 22, by + 18 + i * 28, line, f, TEXT, bold=True)
    caret_x = bx + 22 + f.getlength(lines[1]) / S + 2
    lay, d = c.draw()
    d.rectangle(s(caret_x, by + 18 + 28 + 2, caret_x + 3, by + 18 + 28 + 24), fill=GOLD)
    c.add(lay)
    # the buttons
    for label, bx_, hover in [('Default', x + 60, False), ('OK', x + w - 240, True), ('Cancel', x + w - 130, False)]:
        bw_ = 100 if label != 'Default' else 120
        lay, d = c.draw()
        d.rectangle(s(bx_, y + h - 58, bx_ + bw_, y + h - 26), fill=ROW_HOVER if hover else ROW,
                    outline=HOVER_LINE if hover else DIM_GOLD, width=(2 if hover else 1) * S)
        c.add(lay)
        text(c, bx_ + bw_ / 2, y + h - 42, label, SERIF(20), TEXT, anchor='mm')
    c.save('A-text-speed.png')


# Window 131: the full-width bar under the event CGs (frame 815 at (0,588), 1280x212); the text at (16,610),
# lines 52 apart, 52 cells; the speaker's name in the text: Name「…」, a thought Name（…）, the lines under
# the first indented to the text's start.
R131 = (0, 588, 1280, 212)
BAR_TEXT = (16, 610)
BAR_PITCH = 52
SPEECH_131 = ['Rance「Right now, we aren’t a king and a ninja.', '      We’re an interrogator and a prisoner. So...」']
THOUGHT_131 = ['Rance（They’re so persistent!', '      Whether it’s morning or night or I’m taking a', '      shit, I don’t get a break!）']
NARRATION_131 = ['Rance was on the outskirts of Golak.', 'He was alone.']


def split_name(lines):
    """Name「…」 / Name（…）: the name, whether a thought, the lines without the name, the brackets and the indent."""
    m = re.match(r'^(\S+?)([「（])(.*)$', lines[0])
    if not m:
        return None, False, lines, 0
    name, bracket, first = m.groups()
    body = [first] + [l.lstrip() for l in lines[1:]]
    body[-1] = re.sub(r'[」）]$', '', body[-1])
    return name, bracket == '（', body, len(name) + 1   # the prefix's cells: the name and the bracket


def name_tab(c, x, y, name, thought=False):
    """The speaker's plate on the bar's top edge: a small lacquer tab, half over the CG's foot."""
    f = CINZEL(30, 600)
    w = f.getlength(name.upper()) / S + 64
    h = 46
    if thought:
        lay, d = c.draw()
        d.rounded_rectangle(s(x, y, x + w, y + h), radius=12 * S, fill=(30, 26, 30, 255))
        c.add(lay)
        lay, d = c.draw()
        t, dash, gap = 0, 10, 6
        # dashes along the tab's outline (a rounded rectangle, straight parts only)
        for (ax, ay, bx, by) in [(x + 12, y + 1, x + w - 12, y + 1), (x + 12, y + h - 1, x + w - 12, y + h - 1)]:
            n = bx - ax
            t = 0
            while t < n:
                d.line(pts([(ax + t, ay), (ax + min(t + dash, n), by)]), fill=GOLD, width=2 * S)
                t += dash + gap
        for (bx, by, start) in [(x + 1, y + 1, 180), (x + w - 25, y + 1, 270), (x + 1, y + h - 25, 90), (x + w - 25, y + h - 25, 0)]:
            d.arc(s(bx, by, bx + 24, by + 24), start, start + 90, fill=GOLD, width=2 * S)
        c.add(lay)
        text(c, x + w / 2, y + h / 2 + 1, name.upper(), f, QUIET, anchor='mm')
    else:
        panel(c, x, y, w, h)
        text(c, x + w / 2, y + h / 2 + 1, name.upper(), f, NAME, anchor='mm')


def bar(shot, lines, out, mode='tab'):
    """131 as one lacquer bar the width of the screen; the name split off onto a tab (mode 'tab') or kept at
    its cells in gold without the brackets (mode 'inline'); narration in the narration's colours."""
    c = Canvas(shot)
    x, y, w, h = R131
    panel(c, x, y, w, h)
    name, thought, body, prefix = split_name(lines)
    tx, ty = BAR_TEXT
    colour = NARR_TEXT if name is None else QUIET if thought else TEXT
    if name is not None and mode == 'tab':
        name_tab(c, 32, y - 26, name, thought)
        mono_lines(c, tx, ty + 8, body, pitch=BAR_PITCH, colour=colour)
    elif name is not None:
        # the name at its own cells, gold; the text after it at its cells (the prefix's cells kept)
        f = GOTHIC(48)
        text(c, tx, ty + 8, name, f, NAME, bold=True)
        cells = [' ' * prefix + body[0]] + [' ' * prefix + l for l in body[1:]]
        mono_lines(c, tx, ty + 8, cells, pitch=BAR_PITCH, colour=colour)
    else:
        mono_lines(c, tx, ty + 8, body, pitch=BAR_PITCH, colour=colour)
    triangle(c, x + w - 30, y + h - 26)
    return c.save(out)


# ---------------------------------------------------------------- the ']' menus (step 3, 2026-10-18)
# Over window dumps of our own interface (the Godot window, 1280x800 with the menu bar): Cﾌﾞｶ's troop menu and its
# Do / Don't over our 121, as the original interface still shows them (the Subordinates screen covers them in ours,
# but they are the scene menus' shape: a question in a window, the answers in a box above).
#   python -I tools/ui/mock_messages.py <shots dir> <out dir> menus <dump with 5 items> <dump with Do / Don't> <clean dump> < /dev/null
# The original's lilac box is cleared from the picture with the map from a dump of the same view without a menu (the key
# wait just before them: "Rance gained experience?" in the week-18 run with --harem 6, 00082-key.png).

def window_area(path):
    """The game's 640x400 area of a window dump (under the 24-px menu bar, centred), at 1280x800."""
    im = Image.open(path).convert('RGBA')
    k = (im.height - 24) / 400
    left = (im.width - 640 * k) / 2
    return im.crop((round(left), 24, round(left + 640 * k), im.height)).resize((1280, 800), Image.LANCZOS)


BOX = (480, 78, 830, 304)          # the lilac box of the five items, with its frame (screen px)


def cleared(path, clean):
    """The dump with the original's menu box painted out: the map under it from a dump of the same view without it."""
    base = window_area(path)
    base.paste(window_area(clean).crop(BOX), BOX[:2])
    return base


class WindowCanvas(Canvas):
    def __init__(self, image, dim=0.0, keep=()):
        base = image
        if dim:
            dark = Image.alpha_composite(base, Image.new('RGBA', base.size, (0, 0, 0, int(255 * dim))))
            for r in keep:                                  # what stays lit: the asking window, its speaker
                dark.paste(base.crop(r), r[:2])
            base = dark
        self.base = base
        self.ov = Image.new('RGBA', (1280 * S, 800 * S), (0, 0, 0, 0))


MENU5 = ['Info', '○Replenish Troops', '○Add New Troops', '○Talk', 'Do Nothing']
MENU2 = ['Do', "Don’t"]
ROW_FONT = lambda: GOTHIC(30)


def ring(d, cx, cy, r=7):
    d.ellipse(s(cx - r, cy - r, cx + r, cy + r), outline=GOLD, width=2 * S)


def menu_rows(c, x, y, w, items, hover, pitch=44, rh=36):
    """Rows as the command panel's: a dark box each, the hovered one lit; ○ a gold ring, the text the game's font."""
    f = ROW_FONT()
    for i, label in enumerate(items):
        ry = y + i * pitch
        lit = i == hover
        lay, d = c.draw()
        d.rectangle(s(x, ry, x + w, ry + rh), fill=ROW_HOVER if lit else ROW,
                    outline=HOVER_LINE if lit else DIM_GOLD, width=(2 if lit else 1) * S)
        marked = label.startswith('○')
        if marked:
            ring(d, x + 22, ry + rh / 2)
        c.add(lay)
        text(c, x + (40 if marked else 16), ry + rh / 2 + 1, label.lstrip('○'), f, NAME if lit else TEXT, anchor='lm')


def m1_in_place(path, twin, items, name, hover=1):
    """M1: the box's own place, a lacquer panel of rows."""
    c = WindowCanvas(cleared(path, twin))
    pitch, rh = 44, 36
    w = 360
    h = 28 + len(items) * pitch - (pitch - rh)
    x, y = 480, 80
    panel(c, x, y, w, h)
    menu_rows(c, x + 14, y + 14, w - 28, items, hover, pitch, rh)
    return c.save(name)


def m2_docked(path, twin, items, name, hover=1):
    """M2: the answers grow out of the asking window's top edge on its speaker's side, one lacquer piece."""
    c = WindowCanvas(cleared(path, twin))
    wx, wy, ww, wh = 76, 524, 794, 240             # our 121 as the game draws it (window_area px)
    pitch, rh = 44, 36
    w = 360
    h = 26 + len(items) * pitch - (pitch - rh) + 10
    x, y = wx + ww - w - 24, wy - h + 8
    panel(c, x, y, w, h)
    lay, d = c.draw()                              # the seam: lacquer over both frames, gold down its sides
    d.rectangle(s(x + 2, wy - 6, x + w - 2, wy + 8), fill=PANEL)
    for sx in (x + 1, x + w - 1):
        d.line(pts([(sx, wy - 8), (sx, wy + 2)]), fill=GOLD, width=2 * S)
    for sx in (x, x + w):
        d.polygon(pts([(sx, wy - 4), (sx + 4, wy), (sx, wy + 4), (sx - 4, wy)]), fill=GOLD)
    c.add(lay)
    menu_rows(c, x + 14, y + 14, w - 28, items, hover, pitch, rh)
    return c.save(name)


def m3_banners(path, twin, items, name, hover=1):
    """M3: the scene dimmed but the window and its speaker; the answers as wide banners in the middle."""
    keep = [(58, 512, 884, 772), (884, 300, 1280, 800)]
    c = WindowCanvas(cleared(path, twin), dim=0.5, keep=keep)
    bw, bh, pitch = 560, 50, 62
    x = 640 - bw / 2 - 60
    y0 = 300 - (len(items) * pitch) / 2
    f = CINZEL(26, 600)
    for i, label in enumerate(items):
        y = y0 + i * pitch
        lit = i == hover
        lay, d = c.draw()
        top, bottom = ((150, 40, 30), (96, 20, 14)) if lit else ((40, 30, 22), (22, 16, 12))
        for j in range(bh * S):
            t = j / (bh * S - 1)
            d.line([(x * S, y * S + j), ((x + bw) * S, y * S + j)],
                   fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)) + (240,))
        d.line(pts([(x, y), (x + bw, y)]), fill=HOVER_LINE if lit else GOLD, width=2 * S)
        d.line(pts([(x, y + bh), (x + bw, y + bh)]), fill=HOVER_LINE if lit else GOLD, width=2 * S)
        for ex, sx in ((x, -1), (x + bw, 1)):        # the banner's pointed ends
            d.polygon(pts([(ex, y), (ex + sx * 18, y + bh / 2), (ex, y + bh)]), fill=bottom + (240,))
            d.line(pts([(ex, y), (ex + sx * 18, y + bh / 2), (ex, y + bh)]), fill=HOVER_LINE if lit else GOLD, width=2 * S)
        marked = label.startswith('○')
        tw = d.textlength(label.lstrip('○'), font=f) / S
        tx = x + bw / 2 - tw / 2 + (14 if marked else 0)
        if marked:
            ring(d, tx - 22, y + bh / 2)
        c.add(lay)
        text(c, tx, y + bh / 2 + 2, label.lstrip('○'), f, (250, 226, 160, 255) if lit else TEXT, anchor='lm')
    return c.save(name)


# ---------------------------------------------------------------- letter by letter (step 4, 2026-10-18)
# Over window dumps of our own interface (--interface new, the kept save saves-week18 or saves-week26: a 121 key wait,
# message-speech-right, and the map's plates, map-markers):
#   python -I tools/ui/mock_messages.py <shots dir> <out dir> speed <121 dump> <map dump> < /dev/null
# T1-T3: three ways of showing the text while it types (our 121 redrawn over the dump's); S1-S2: Option → Text Speed….

CELL = 24                       # a half-width cell at the game's 48-px font (screen px)
SPEEDS = [8, 12, 18, 25, 35, 50, 70, 100, 150]   # letters a second at steps 1-9; step 10 is Instant


def typed(c, x, y, lines, count, style, size=48, pitch=46, cell=CELL):
    """The first `count` characters of the lines at their cells; 'plain' cuts them off, 'caret' adds a gold bar at the
    next cell, 'fade' brings the last few in from transparent. Returns where the next character goes."""
    f = GOTHIC(size)
    shown, nx, ny = 0, x, y
    fade = 6
    for i, line in enumerate(lines):
        ly = y + i * pitch
        for j, ch in enumerate(line):
            if shown >= count:
                break
            alpha = 255
            if style == 'fade' and count - shown <= fade:
                alpha = round(255 * (count - shown) / (fade + 1))
            text(c, x + j * cell, ly, ch, f, TEXT[:3] + (alpha,), bold=True)
            shown += 1
            nx, ny = x + (j + 1) * cell, ly
        if shown >= count:
            break
        nx, ny = x, ly + pitch          # the next line's start, if this one was whole
    return nx, ny


def window_121(c, lines, count, style, done=False, tail=True):
    x, y, w, h = R121
    panel(c, x, y, w, h)
    if tail:
        speech_tail(c, x + w, y + 150, y + 196, (x + w + 30, y + 188), 'right')
    nx, ny = typed(c, x + 30, y + 22, lines, count, style)
    if style == 'caret' and not done:
        lay, d = c.draw()
        d.rectangle(s(nx + 3, ny + 6, nx + 7, ny + 46), fill=GOLD)
        c.add(lay)
    if done:
        triangle(c, x + w - 30, y + h - 28)


def typing(path, style, name, count):
    c = WindowCanvas(window_area(path))
    window_121(c, MARIS, count, style)
    return c.save(name)


def typing_frames(path, name):
    """T1 in three moments: just opened, halfway, whole with the triangle (the panel's crops stacked)."""
    total = sum(len(l) for l in MARIS)
    crops = []
    for count, done in [(14, False), (total // 2 + 6, False), (total, True)]:
        c = WindowCanvas(window_area(path))
        window_121(c, MARIS, count, 'plain', done)
        img = Image.alpha_composite(c.base, c.ov.resize((1280, 800), Image.LANCZOS))
        crops.append(img.crop((40, 496, 920, 780)))
    sheet = Image.new('RGB', (880, 3 * 284 + 2 * 12), (12, 10, 8))
    for i, im in enumerate(crops):
        sheet.paste(im.convert('RGB'), (0, i * 296))
    sheet.save(os.path.join(OUT, name))


def divider(c, x0, x1, y):
    lay, d = c.draw()
    m = (x0 + x1) / 2
    d.line(pts([(x0, y), (m - 12, y)]), fill=DIM_GOLD, width=1 * S)
    d.line(pts([(m + 12, y), (x1, y)]), fill=DIM_GOLD, width=1 * S)
    d.polygon(pts([(m, y - 5), (m + 5, y), (m, y + 5), (m - 5, y)]), fill=GOLD)
    c.add(lay)


def slider(c, x0, x1, y, step):
    """Ten steps, the last Instant; the knob a gold diamond with its step over it, its speed under the track."""
    lay, d = c.draw()
    d.rounded_rectangle(s(x0, y - 4, x1, y + 4), radius=4 * S, fill=ROW, outline=DIM_GOLD, width=1 * S)
    knob = x0 + (x1 - x0) * (step - 1) / 9
    d.rounded_rectangle(s(x0, y - 4, knob, y + 4), radius=4 * S, fill=(150, 118, 58, 255))
    for i in range(10):
        px = x0 + (x1 - x0) * i / 9
        d.line(pts([(px, y + 12), (px, y + 20)]), fill=GOLD if i == 9 else DIM_GOLD, width=(2 if i == 9 else 1) * S)
    d.polygon(pts([(knob, y - 16), (knob + 14, y), (knob, y + 16), (knob - 14, y)]), fill=GOLD, outline=NAME)
    c.add(lay)
    small = CINZEL(18, 600)
    text(c, x0, y + 46, 'SLOW', small, LABEL, anchor='ms')
    text(c, x1, y + 46, 'INSTANT', small, NAME, anchor='ms')
    text(c, knob, y - 26, str(step), CINZEL(22, 700), NAME, anchor='ms')


def buttons(c, x, y, w):
    for label, bx, bw, lit in [('Default', x + 40, 120, False), ('OK', x + w - 250, 100, True), ('Cancel', x + w - 140, 100, False)]:
        lay, d = c.draw()
        d.rectangle(s(bx, y, bx + bw, y + 34), fill=ROW_HOVER if lit else ROW,
                    outline=HOVER_LINE if lit else DIM_GOLD, width=(2 if lit else 1) * S)
        c.add(lay)
        text(c, bx + bw / 2, y + 18, label, GOTHIC(24), NAME if lit else TEXT, bold=True, anchor='mm')


def speed_small(path, name, step=9):
    """S1: one dialog in the middle over the dimmed map; the preview a small speech panel inside it."""
    c = WindowCanvas(window_area(path), dim=0.45)
    x, y, w, h = 300, 140, 680, 500
    panel(c, x, y, w, h)
    text(c, x + w / 2, y + 58, 'Text Speed', BRUSH(34), NAME, anchor='ms')
    divider(c, x + 60, x + w - 60, y + 82)
    slider(c, x + 90, x + w - 90, y + 150, step)
    text(c, x + 52, y + 226, 'PREVIEW', CINZEL(18, 600), SUBTITLE)
    px, py, pw, ph = x + 50, y + 252, w - 100, 170
    lay, d = c.draw()
    d.rectangle(s(px, py, px + pw, py + ph), fill=(14, 10, 8, 255), outline=DIM_GOLD, width=1 * S)
    c.add(lay)
    bx, by, bw, bh = px + 20, py + 16, pw - 70, ph - 32
    panel(c, bx, by, bw, bh)
    speech_tail(c, bx + bw, by + 60, by + 90, (bx + bw + 22, by + 86), 'right')
    typed(c, bx + 20, by + 16, MARIS[:3], 44, 'plain', size=30, pitch=32, cell=15)
    buttons(c, x, y + h - 58, w)
    return c.save(name)


def speed_real(path, name, step=9):
    """S2: the dialog over the map's top; the preview the game's own 121 at its place and size, typing over and over."""
    c = WindowCanvas(window_area(path), dim=0.45)
    x, y, w, h = 330, 90, 620, 300
    panel(c, x, y, w, h)
    text(c, x + w / 2, y + 58, 'Text Speed', BRUSH(34), NAME, anchor='ms')
    divider(c, x + 60, x + w - 60, y + 82)
    slider(c, x + 80, x + w - 80, y + 150, step)
    buttons(c, x, y + h - 58, w)
    window_121(c, MARIS, 52, 'plain', tail=False)
    xx, yy, ww, hh = R121
    lay, d = c.draw()                       # the preview's label: a small plate on the panel's top edge
    d.rectangle(s(xx + 40, yy - 15, xx + 190, yy + 15), fill=PANEL, outline=GOLD, width=2 * S)
    c.add(lay)
    text(c, xx + 115, yy + 1, 'PREVIEW', CINZEL(18, 600), NAME, anchor='mm')
    return c.save(name)


# ---------------------------------------------------------------- the battle's info boxes 143/153 (2026-10-18)
# Over window dumps of our battle screen (--interface new, the week-18 save played to the week's end in a window:
# --load --auto --fast --quit --seed 2 --saves user://saves-look-after2 --slots-from user://saves-week18 --weeks 18
# --interface new --shots <dir>), in window pixels (the dumps' own 1280x800 with the menu bar):
#   python -I tools/ui/mock_messages.py <shots dir> <out dir> boxes <prepared> <fell> <night> <tally> <clean> <clean fell> < /dev/null
# The original's cream box is painted out with the scene of a dump of the same battle without it. K1: our narration
# panel in the scene; K2: a banner across the scene in the battle screen's steel; K3: the events on the cards, the end
# as one results plate.

BOX143 = (512, 214, 770, 418)               # the original's box over the scene (window px), down to the scene's foot
SCENE = (143, 24, 1136, 418)                # the battle scene's picture
STEEL = dict(back=(43, 45, 49), card=(59, 62, 68), border=(168, 138, 74), text=(240, 240, 238), muted=(165, 167, 171),
             warning=(226, 114, 91), accent=(242, 199, 92))
BARRES_CARD = (30, 615, 302, 757)
SAKANAKU_CARD = (695, 462, 967, 605)

PREPARED = ['Barres prepared for the', "enemy's attack.", 'Defense Increase: 2']
FELL = ['Sakanaku fell in battle.']
NIGHT = ['The battle dragged', 'on and it became', 'night.']
TALLY = ['Leazas: Damages', ' (822)', 'Leazas Reb: Damages', ' (1105)', '', '2 EXP obtained.']


def boxless(path, clean):
    base = Image.open(path).convert('RGBA')
    base.paste(Image.open(clean).convert('RGBA').crop(BOX143), BOX143[:2])
    return base


def joined(lines):
    """The box's own line breaks (made for its 20 cells) joined into sentences; an empty line parts them."""
    out, cur = [], []
    for l in lines + ['']:
        if l.strip():
            cur.append(l.strip())
        elif cur:
            out.append(' '.join(cur))
            cur = []
    return out


def k1_panel(base, lines, name, rows=None):
    """K1: our narration panel (113's lacquer) in the scene's middle, the lines joined, the triangle."""
    c = WindowCanvas(base)
    f = GOTHIC(30)
    rows = rows or joined(lines)
    w = max(f.getlength(r) / S for r in rows) + 80
    h = 40 + len(rows) * 40 + 20
    x, y = (SCENE[0] + SCENE[2]) / 2 - w / 2, 150 - h / 2 + 60
    panel(c, x, y, w, h, fill=NARR)
    for i, r in enumerate(rows):
        text(c, x + 40, y + 26 + i * 40, r, f, NARR_TEXT, bold=True)
    triangle(c, x + w - 24, y + h - 20)
    return c.save(name)


def steel_band(c, y, h):
    x0, x1 = SCENE[0], SCENE[2]
    lay, d = c.draw()
    for j in range(int(h * S)):                                   # darker at the edges, the scene showing faintly through
        t = abs(j / (h * S) - 0.5) * 2
        d.line([(x0 * S, y * S + j), (x1 * S, y * S + j)], fill=STEEL['back'] + (int(225 + 25 * t),))
    for yy in (y, y + h):
        d.line(pts([(x0, yy), (x1, yy)]), fill=STEEL['border'] + (255,), width=2 * S)
    c.add(lay)


def k2_band(base, lines, name, tally=False):
    """K2: a band across the scene in the battle screen's steel: one sentence centred, or the tally in two columns."""
    c = WindowCanvas(base)
    f = CINZEL(26, 600)
    if tally:
        y, h = 196, 132
        steel_band(c, y, h)
        cx = (SCENE[0] + SCENE[2]) / 2
        for side, (army, dmg) in enumerate([('LEAZAS', '822'), ('LEAZAS REB', '1105')]):
            ax = cx - 230 if side == 0 else cx + 230
            text(c, ax, y + 40, army, CINZEL(20, 600), STEEL['muted'] + (255,), anchor='ms')
            text(c, ax, y + 82, 'Damages ' + dmg, CINZEL(30, 700), STEEL['warning'] + (255,), anchor='ms')
        lay, d = c.draw()
        d.line(pts([(cx, y + 20), (cx, y + 92)]), fill=STEEL['border'] + (255,), width=1 * S)
        c.add(lay)
        text(c, cx, y + 118, '2 EXP obtained', CINZEL(22, 600), STEEL['accent'] + (255,), anchor='ms')
    else:
        rows = joined(lines)
        y, h = 230, 30 + len(rows) * 38
        steel_band(c, y, h)
        for i, r in enumerate(rows):
            text(c, (SCENE[0] + SCENE[2]) / 2, y + 42 + i * 38, r, f, STEEL['text'] + (255,), anchor='ms')
    triangle(c, SCENE[2] - 30, y + h - 18)
    return c.save(name)


def card_glow(c, rect, colour):
    x0, y0, x1, y1 = rect
    lay, d = c.draw()
    for i in range(6, 0, -1):
        d.rectangle(s(x0 - i, y0 - i, x1 + i, y1 + i), outline=colour + (int(30 + 20 * (6 - i)),), width=S)
    d.rectangle(s(x0, y0, x1, y1), outline=colour + (255,), width=2 * S)
    c.add(lay)


def k3_prepared(base, name):
    """K3a: the unit's card lit, a badge 'DEF +2' over its portrait, the sentence on a small plate above the card."""
    c = WindowCanvas(base)
    card_glow(c, BARRES_CARD, STEEL['accent'])
    x0, y0, x1, y1 = BARRES_CARD
    lay, d = c.draw()                                             # the badge over the portrait's top right
    bx, by = x0 + 70, y0 + 10
    d.rounded_rectangle(s(bx, by, bx + 92, by + 30), radius=15 * S, fill=(52, 92, 150, 245), outline=STEEL['accent'] + (255,), width=2 * S)
    c.add(lay)
    text(c, bx + 46, by + 16, 'DEF +2', CINZEL(18, 700), (255, 255, 255, 255), anchor='mm')
    f = GOTHIC(24)
    msg = "Prepared for the enemy's attack"
    w = f.getlength(msg) / S + 40
    px, py = x0, y0 - 52
    panel(c, px, py, w, 40, fill=NARR)
    text(c, px + 20, py + 9, msg, f, NARR_TEXT, bold=True)
    triangle(c, px + w - 16, py + 26)
    return c.save(name)


def k3_fell(base, name):
    """K3b: a red 'FELL IN BATTLE' stamp across the commander's card, the card dimmed; the click's triangle by it."""
    c = WindowCanvas(base)
    x0, y0, x1, y1 = SAKANAKU_CARD
    lay, d = c.draw()
    d.rectangle(s(x0, y0, x1, y1), fill=(0, 0, 0, 120))
    c.add(lay)
    stamp = Image.new('RGBA', (300 * S, 70 * S), (0, 0, 0, 0))
    sd = ImageDraw.Draw(stamp)
    sd.rounded_rectangle((4 * S, 4 * S, 296 * S, 66 * S), radius=8 * S, outline=(200, 40, 30, 235), width=4 * S)
    sd.text((150 * S, 36 * S), 'FELL IN BATTLE', font=CINZEL(30, 700), fill=(210, 46, 34, 235), anchor='mm')
    stamp = stamp.rotate(-8, resample=Image.BICUBIC, expand=True)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    c.ov.alpha_composite(stamp, (int(cx * S - stamp.width / 2), int(cy * S - stamp.height / 2)))
    triangle(c, x1 - 18, y1 - 16)
    return c.save(name)


def k3_results(base, name):
    """K3c: the end as one plate over the scene: the reason as its title, each army's damages under its name, the EXP."""
    c = WindowCanvas(base, dim=0.0)
    lay, d = c.draw()
    d.rectangle(s(*SCENE), fill=(0, 0, 0, 110))
    c.add(lay)
    w, h = 620, 250
    x, y = (SCENE[0] + SCENE[2]) / 2 - w / 2, 92
    panel(c, x, y, w, h)
    cx = x + w / 2
    text(c, cx, y + 50, 'Night Falls', BRUSH(32), NAME, anchor='ms')
    text(c, cx, y + 80, 'The battle dragged on and it became night.', SERIF_I(20), SUBTITLE, anchor='ms')
    divider(c, x + 60, x + w - 60, y + 100)
    for side, (army, dmg) in enumerate([('Leazas', '822'), ('Leazas Reb', '1105')]):
        ax = cx - 150 if side == 0 else cx + 150
        text(c, ax, y + 138, army.upper(), CINZEL(20, 600), LABEL, anchor='ms')
        text(c, ax, y + 180, dmg, CINZEL(38, 700), (222, 110, 90, 255), anchor='ms')
        text(c, ax, y + 200, 'damages', SERIF_I(18), QUIET, anchor='ms')
    lay, d = c.draw()
    d.line(pts([(cx, y + 118), (cx, y + 200)]), fill=DIM_GOLD, width=1 * S)
    c.add(lay)
    text(c, cx, y + 234, '2 EXP obtained', CINZEL(22, 600), NAME, anchor='ms')
    triangle(c, x + w - 26, y + h - 22)
    return c.save(name)


LEVEL_UPS = [('Barres', 1, ['Leader Attacks +1', 'Def on Defense UP']),
             ('Haurein', 1, ['Leader Attacks +1', 'Atk on Offense UP'])]


def k3_results_levels(base, name):
    """K3d: the plate grown by the level-ups, one row each under the EXP (each the click of its own box)."""
    c = WindowCanvas(base, dim=0.0)
    lay, d = c.draw()
    d.rectangle(s(*SCENE), fill=(0, 0, 0, 110))
    c.add(lay)
    w, h = 620, 250 + 30 + 44 * len(LEVEL_UPS)
    x, y = (SCENE[0] + SCENE[2]) / 2 - w / 2, 40
    panel(c, x, y, w, h)
    cx = x + w / 2
    text(c, cx, y + 50, 'Night Falls', BRUSH(32), NAME, anchor='ms')
    text(c, cx, y + 80, 'The battle dragged on and it became night.', SERIF_I(20), SUBTITLE, anchor='ms')
    divider(c, x + 60, x + w - 60, y + 100)
    for side, (army, dmg) in enumerate([('Leazas', '822'), ('Leazas Reb', '1105')]):
        ax = cx - 150 if side == 0 else cx + 150
        text(c, ax, y + 138, army.upper(), CINZEL(20, 600), LABEL, anchor='ms')
        text(c, ax, y + 180, dmg, CINZEL(38, 700), (222, 110, 90, 255), anchor='ms')
        text(c, ax, y + 200, 'damages', SERIF_I(18), QUIET, anchor='ms')
    lay, d = c.draw()
    d.line(pts([(cx, y + 118), (cx, y + 200)]), fill=DIM_GOLD, width=1 * S)
    c.add(lay)
    text(c, cx, y + 234, '2 EXP obtained', CINZEL(22, 600), NAME, anchor='ms')
    divider(c, x + 60, x + w - 60, y + 254)
    for i, (unit, level, gains) in enumerate(LEVEL_UPS):
        ry = y + 268 + i * 44
        lay, d = c.draw()
        d.rounded_rectangle(s(x + 40, ry, x + w - 40, ry + 36), radius=6 * S, fill=ROW)
        c.add(lay)
        text(c, x + 60, ry + 26, 'LEVEL UP', CINZEL(16, 700), STEEL['accent'] + (255,), anchor='ls')
        text(c, x + 160, ry + 27, unit, CINZEL(20, 600), NAME, anchor='ls')
        text(c, x + 280, ry + 26, '  ·  '.join(gains), SERIF(18), VALUE, anchor='ls')
    triangle(c, x + w - 26, y + h - 22)
    return c.save(name)


def k3_card_level(base, name):
    """K3e: a level-up on the unit's card: the card lit, a 'LEVEL UP' badge, the gains on a small plate above it."""
    c = WindowCanvas(base)
    card_glow(c, BARRES_CARD, STEEL['accent'])
    x0, y0, x1, y1 = BARRES_CARD
    lay, d = c.draw()
    bx, by = x0 + 54, y0 + 10
    d.rounded_rectangle(s(bx, by, bx + 124, by + 30), radius=15 * S, fill=(150, 112, 30, 245), outline=(255, 226, 140, 255), width=2 * S)
    c.add(lay)
    text(c, bx + 62, by + 16, 'LEVEL UP', CINZEL(18, 700), (255, 255, 255, 255), anchor='mm')
    f = GOTHIC(24)
    msg = 'Leader Attacks +1 / Def on Defense UP'
    w = f.getlength(msg) / S + 40
    px, py = x0, y0 - 52
    panel(c, px, py, w, 40, fill=NARR)
    text(c, px + 20, py + 9, msg, f, NARR_TEXT, bold=True)
    triangle(c, px + w - 16, py + 26)
    return c.save(name)


if len(sys.argv) > 3 and sys.argv[3] == 'boxes':
    prepared, fell, night, tally, clean, clean_fell = (os.path.join(SHOTS, a) for a in sys.argv[4:10])
    k1_panel(boxless(night, clean), NIGHT, 'K1-panel-night.png')
    k1_panel(boxless(tally, clean), TALLY, 'K1b-panel-tally.png',
             rows=['Leazas: Damages (822)', 'Leazas Reb: Damages (1105)', '2 EXP obtained.'])
    k1_panel(boxless(prepared, clean), PREPARED, 'K1c-panel-prepared.png')
    k2_band(boxless(night, clean), NIGHT, 'K2-band-night.png')
    k2_band(boxless(tally, clean), TALLY, 'K2b-band-tally.png', tally=True)
    k3_prepared(boxless(prepared, clean), 'K3-card-prepared.png')
    k3_fell(boxless(fell, clean_fell), 'K3b-card-fell.png')
    k3_results(boxless(tally, clean), 'K3c-results.png')
    k3_results_levels(boxless(tally, clean), 'K3d-results-levels.png')
    k3_card_level(boxless(tally, clean), 'K3e-card-level.png')
    print('done')
    sys.exit(0)

if len(sys.argv) > 3 and sys.argv[3] == 'speed':
    speech, map_ = (os.path.join(SHOTS, a) for a in sys.argv[4:6])
    total = sum(len(l) for l in MARIS)
    typing(speech, 'plain', 'T1-typing-plain.png', 70)
    typing(speech, 'caret', 'T2-typing-caret.png', 70)
    typing(speech, 'fade', 'T3-typing-fade.png', 70)
    typing_frames(speech, 'T1b-typing-plain-frames.png')
    speed_small(map_, 'S1-speed-dialog.png')
    speed_real(map_, 'S2-speed-real-preview.png')
    print('done')
    sys.exit(0)

if len(sys.argv) > 3 and sys.argv[3] == 'menus':
    five, two, clean = (os.path.join(SHOTS, a) for a in sys.argv[4:7])
    m1_in_place(five, clean, MENU5, 'M1-in-place.png')
    m1_in_place(two, clean, MENU2, 'M1b-in-place-2.png', hover=0)
    m2_docked(five, clean, MENU5, 'M2-docked.png')
    m2_docked(two, clean, MENU2, 'M2b-docked-2.png', hover=0)
    m3_banners(five, clean, MENU5, 'M3-banners.png')
    m3_banners(two, clean, MENU2, 'M3b-banners-2.png', hover=0)
    print('done')
    sys.exit(0)

if len(sys.argv) > 3:
    # The 131 set over a dump of a harem scene (the third argument: that run's shots, the fourth: one of its
    # key waits with 131 open); see the header for the run.
    SHOTS, shot = sys.argv[3], sys.argv[4]
    bar(shot, SPEECH_131, 'A-7-cg-bar-speech.png')
    bar(shot, SPEECH_131, 'A-7b-cg-bar-speech-inline.png', mode='inline')
    bar(shot, THOUGHT_131, 'A-8-cg-bar-thought.png')
    bar(shot, NARRATION_131, 'A-9-cg-bar-narration.png')
    print('done')
    sys.exit(0)

speech_right('00015-key.png', MARIS, 'A-1-speech-right.png')
speech_left('00004-key.png', RANCE, 'A-2-speech-left.png')
thought('00020-key.png', THOUGHT, 'A-3-thought.png')
narration('00003-key.png', NARRATION, 'A-4-narration.png')
place_title('00002-key.png', 'A-5-place-title.png')
menu('00027-menu.png', 'A-6-menu.png')
compare()
speed_dialog()
print('done')
