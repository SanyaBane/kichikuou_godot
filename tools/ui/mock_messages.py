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
