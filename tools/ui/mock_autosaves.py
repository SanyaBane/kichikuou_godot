"""Mock-ups of the auto-saves at a turn's start and at its Turn END (docs/ui/save-load.md, §12): how the Save/Load
screen's auto-saves tell the two apart. Drawn over the screen gallery's "save-load-auto" dump (docs/ui/gallery.md; the
Load list from COMMAND on its auto-saves side, 1280x800). From the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_autosaves.py <save-load-auto dump> <out dir>
Round J: J1/J1s one list of twenty, a seal per kind (a rising sun, a moon); J2 a row a week, its start and its end side
by side; J3 the two kinds as two lists of ten behind a three-way switch. The saves are made up (one evening's play).
The images hold the player's CGs: keep <out dir> outside the repository. Everything is in the 1280x800 screen's pixels
(plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os, math
from PIL import Image, ImageDraw, ImageFont

DUMP, OUT = sys.argv[1:3]
sys.stdout.reconfigure(encoding='utf-8')
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
S = 2
W, H, CAPTION = 1280, 800, 64

# UiPalette.Lacquer and SaveLoadView's own colours
PANEL, GOLD, DIM = (21, 15, 12, 255), (196, 160, 86, 255), (110, 86, 46, 255)
NAME, SUB, LABEL = (232, 199, 122, 255), (160, 140, 104, 255), (150, 134, 108, 255)
VALUE, ROW, SHADE = (236, 226, 204, 255), (34, 26, 20, 255), (26, 20, 16, 255)
TEXT, QUIET, OFF = (244, 232, 206, 255), (176, 160, 130, 255), (98, 88, 74, 255)
VERMILION, CHOSEN, RIM = (170, 40, 28, 255), (60, 30, 22, 255), (26, 18, 12, 255)
AUTO_GOLD, SEAL_TEXT, GROOVE = (138, 104, 34, 255), (246, 230, 206, 255), (40, 32, 24, 255)

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
_fonts = {}


def font(size, path=GOTHIC, weight=None):
    key = (size, path, weight)
    if key not in _fonts:
        f = ImageFont.truetype(path, round(size * S))
        if weight is not None:
            f.set_variation_by_axes([weight])
        _fonts[key] = f
    return _fonts[key]


def cinzel(size, weight=700):
    return font(size, CINZEL, weight)


class Canvas:
    def __init__(self):
        img = Image.open(DUMP).convert('RGBA').crop((0, 0, W, H))
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (14, 10, 8, 255))
        self.im.paste(img.resize((W * S, H * S), Image.LANCZOS), (0, 0))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill, outline=outline, width=round(width * S))

    def text(self, xy, s, f, fill=TEXT, anchor='lm'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=DIM, width=1.0):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=max(1, round(width * S)))

    def ellipse(self, cx, cy, r, fill):
        self.d.ellipse([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S], fill=fill)

    def pieslice(self, cx, cy, r, start, end, fill):
        self.d.pieslice([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S], start, end, fill=fill)

    def dashed(self, r, colour, dash=6, step=10):
        x0, y0, x1, y1 = r
        for a, b, horizontal in (((x0, y0), (x1, y0), True), ((x0, y1), (x1, y1), True),
                                 ((x0, y0), (x0, y1), False), ((x1, y0), (x1, y1), False)):
            length = (b[0] - a[0]) if horizontal else (b[1] - a[1])
            t = 0
            while t < length:
                e = min(t + dash, length)
                p = (a[0] + t, a[1]) if horizontal else (a[0], a[1] + t)
                q = (a[0] + e, a[1]) if horizontal else (a[0], a[1] + e)
                self.line(p, q, fill=colour)
                t += step

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=DIM)
        self.text((16, H + 20), caption, cinzel(20), fill=NAME)
        self.text((16, H + 46), note, font(15), fill=QUIET)
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
        print(name)


# ---- the glyphs: a rising sun for a week's start, a moon for its Turn END

def sun(c, cx, cy, r, colour):
    """A sun rising over a horizon: a half disc of radius r on the line through (cx, cy + r/2), rays above it."""
    hy = cy + r * 0.45
    c.pieslice(cx, hy, r * 0.62, 180, 360, colour)
    for angle in (-160, -125, -90, -55, -20):
        a = math.radians(angle)
        c.line((cx + math.cos(a) * r * 0.85, hy + math.sin(a) * r * 0.85),
               (cx + math.cos(a) * r * 1.2, hy + math.sin(a) * r * 1.2), fill=colour, width=1.4)
    c.line((cx - r * 1.2, hy + 0.6), (cx + r * 1.2, hy + 0.6), fill=colour, width=1.4)


def moon(c, cx, cy, r, colour, behind):
    """A crescent: a disc of radius r with a disc of the background's colour laid over its upper right."""
    c.ellipse(cx, cy, r, colour)
    c.ellipse(cx + r * 0.5, cy - r * 0.32, r * 0.86, behind)


def seal(c, x, y, end):
    """The row's 24x24 seal: the week's start gold with a white sun, its Turn END dark in a gold frame with a gold moon."""
    if end:
        c.rect((x, y, x + 24, y + 24), fill=RIM, outline=AUTO_GOLD, width=2)
        moon(c, x + 11.5, y + 12.5, 6.4, NAME, RIM)
    else:
        c.rect((x, y, x + 24, y + 24), fill=AUTO_GOLD)
        c.rect((x + 2, y + 2, x + 22, y + 22), outline=(190, 168, 130, 255), width=1)
        sun(c, x + 12, y + 11, 7, SEAL_TEXT)


# ---- the made-up saves: one evening's play, newest week first (its start, its Turn END or None)

WEEKS = [
    ((5, 2), '21:30', None), ((5, 1), '21:12', '21:27'), ((4, 4), '20:55', '21:09'), ((4, 3), '20:38', '20:52'),
    ((4, 2), '20:21', '20:35'), ((4, 1), '20:04', '20:18'), ((3, 4), '19:47', '20:01'), ((3, 3), '19:30', '19:44'),
    ((3, 2), '19:13', '19:27'), ((3, 1), '18:56', '19:10'), ((2, 4), None, '18:53'),
]


def short(mw):
    return f'LP 3 · M{mw[0]} · W{mw[1]}'


def long(mw):
    return f'LP 3 · Month {mw[0]} · Week {mw[1]}'


def saves():
    """Every auto-save, newest first: (month and week, Turn END?, time)."""
    out = []
    for mw, start, end in WEEKS:
        if end:
            out.append((mw, True, end))
        if start:
            out.append((mw, False, start))
    return out


# ---- the screen's parts as built (SaveLoadView), redrawn where a variant changes them

LIST_X, LIST_W, LIST_TOP, AUTO_H, AUTO_PITCH, HEAD_Y = 52, 330, 162, 55, 60, 141
PAGE_X, PAGE_Y = 426, 84


def erase(c, r):
    c.rect(r, fill=PANEL)


def side_switch(c, labels, chosen):
    erase(c, (LIST_X - 2, 82, LIST_X + LIST_W + 2, 116))
    n = len(labels)
    w = (LIST_W - 4 * (n - 1)) / n
    for i, label in enumerate(labels):
        r = (LIST_X + i * (w + 4), 84, LIST_X + i * (w + 4) + w, 114)
        if i == chosen:
            c.rect(r, fill=CHOSEN, outline=VERMILION, width=2)
        else:
            c.rect(r, fill=ROW, outline=DIM, width=1)
        c.text(((r[0] + r[2]) / 2, 99), label, cinzel(14 if n == 2 else 12), fill=NAME if i == chosen else QUIET, anchor='mm')


def heading(c, right):
    erase(c, (LIST_X - 2, 126, LIST_X + LIST_W + 2, 158))
    c.text((LIST_X, HEAD_Y), 'NEWEST FIRST', cinzel(11), fill=SUB)
    c.text((LIST_X + LIST_W, HEAD_Y), right, font(11), fill=LABEL, anchor='rm')


def clear_list(c):
    erase(c, (LIST_X - 2, LIST_TOP - 2, LIST_X + LIST_W + 2, 760))


def save_row(c, r, mw, end, time, chosen):
    """A row as built (seal, date, time written, second line), the seal and the second line by the save's kind."""
    x0, y0, x1, y1 = r
    if chosen:
        c.rect(r, fill=CHOSEN, outline=VERMILION, width=2)
    else:
        c.rect(r, fill=ROW, outline=AUTO_GOLD, width=1)
    seal(c, x0 + 8, (y0 + y1) / 2 - 12, end)
    tx = x0 + 42
    c.text((tx, y0 + 16), short(mw), cinzel(15), fill=NAME)
    c.text((x1 - 10, y0 + 16), f'20 Oct, {time}', font(14), fill=VALUE, anchor='rm')
    c.text((tx, y0 + 38), 'at Turn END' if end else 'start of the week', font(12), fill=LABEL)


def groove(c, shown, total, first=0):
    top, span, x = LIST_TOP, 756 - LIST_TOP, LIST_X + LIST_W - 6
    c.rect((x, top, x + 5, top + span), fill=GROOVE)
    a, b = top + span * first / total, top + span * min(total, first + shown) / total
    c.rect((x, a, x + 5, b), fill=DIM)


def tag(c, x, y, end):
    """The page's tag after the title: the week's start gold with a sun, its Turn END dark in gold with a moon."""
    label = 'Auto · Turn END' if end else 'Auto · week start'
    f = font(12)
    w = c.width(label, f) + 34
    r = (x, y, x + w, y + 22)
    if end:
        c.rect(r, fill=RIM, outline=AUTO_GOLD, width=1.5)
        moon(c, x + 14, y + 11, 6, NAME, RIM)
        c.text((x + 26, y + 11), label, f, fill=NAME)
    else:
        c.rect(r, fill=AUTO_GOLD)
        sun(c, x + 14, y + 10.5, 5.8, SEAL_TEXT)
        c.text((x + 26, y + 11), label, f, fill=SEAL_TEXT)


def stat(c, i, value):
    """The figure under the map in place i (as DrawPage lays them out), its label left as dumped."""
    sx, sy = PAGE_X + i % 3 * 280, 465 + i // 3 * 46
    erase(c, (sx, sy + 17, sx + 236, sy + 44))
    ascent = font(17).getmetrics()[0] / S
    c.text((sx, sy + 18 + ascent), value, font(17), fill=VALUE, anchor='ls')


START_LINES = ("Written at the start of the week. Loading it plays the week's start again:",
               "the budget, then the commands. Loading leaves the list as it is.")
END_LINES = ("Written when Turn END was chosen. Loading it brings back the map with the week's",
             "commands as they were then; Turn END goes on to the nations' turns.")


def page(c, mw, end, time, budget):
    erase(c, (PAGE_X - 2, PAGE_Y, 1080, PAGE_Y + 30))
    title = long(mw)
    c.text((PAGE_X, PAGE_Y + 14), title, cinzel(22), fill=NAME)
    tag(c, PAGE_X + c.width(title, cinzel(22)) + 16, PAGE_Y + 3, end)
    stat(c, 1, budget)
    stat(c, 5, f'20 Oct 2026, {time}')
    erase(c, (PAGE_X - 2, 560, 1228, 614))
    for k, line in enumerate(END_LINES if end else START_LINES):
        c.text((PAGE_X, 575 + 22 * k), line, font(15), fill=TEXT)


ALL = saves()                                          # twenty, newest first


# ---- J1: one list of twenty, newest first, scrolling as the manual saves do; the seal and the second line tell the kind

def j1(name, chosen, caption, note):
    c = Canvas()
    side_switch(c, ('Manual saves · 4', f'Auto-saves · {len(ALL)}'), 1)
    heading(c, 'the game keeps the last ten weeks')
    clear_list(c)
    for i, (mw, end, time) in enumerate(ALL[:10]):
        y = LIST_TOP + i * AUTO_PITCH
        save_row(c, (LIST_X, y, LIST_X + 318, y + AUTO_H), mw, end, time, i == chosen)
    groove(c, 10, len(ALL))
    mw, end, time = ALL[chosen]
    page(c, mw, end, time, '98,150K GOLD' if end else '116,400K GOLD')
    c.save(name, caption, note)


# ---- J2: a row a week, its start and its Turn END side by side; a half is chosen

def half(c, r, end, time, chosen):
    x0, y0, x1, y1 = r
    if time is None:
        c.dashed(r, DIM)
        moon(c, x0 + 14, (y0 + y1) / 2, 5.2, OFF, PANEL if False else ROW)
        c.text((x0 + 26, (y0 + y1) / 2), 'Turn END', font(13), fill=OFF)
        c.text((x1 - 8, (y0 + y1) / 2), '—', font(13), fill=OFF, anchor='rm')
        return
    fill = CHOSEN if chosen else SHADE
    c.rect(r, fill=fill, outline=VERMILION if chosen else DIM, width=2 if chosen else 1)
    my = (y0 + y1) / 2
    if end:
        moon(c, x0 + 14, my, 5.2, NAME, fill)
    else:
        sun(c, x0 + 14, my - 0.5, 5, NAME)
    c.text((x0 + 26, my), 'Turn END' if end else 'Start', font(13), fill=NAME if chosen else QUIET)
    c.text((x1 - 8, my), time, font(13), fill=VALUE, anchor='rm')


def j2(name, chosen_week, chosen_end, caption, note):
    c = Canvas()
    side_switch(c, ('Manual saves · 4', f'Auto-saves · {len(ALL)}'), 1)
    heading(c, 'one row a week · the last ten weeks')
    clear_list(c)
    for i, (mw, start, end) in enumerate(WEEKS[:10]):
        y = LIST_TOP + i * AUTO_PITCH
        x0, x1 = LIST_X, LIST_X + 318
        c.rect((x0, y, x1, y + AUTO_H), fill=ROW, outline=AUTO_GOLD, width=1)
        c.text((x0 + 10, y + 14), short(mw), cinzel(15), fill=NAME)
        c.text((x1 - 10, y + 14), '20 Oct', font(13), fill=LABEL, anchor='rm')
        mid = (x0 + x1) / 2
        half(c, (x0 + 6, y + 27, mid - 2, y + 50), False, start, i == chosen_week and not chosen_end)
        half(c, (mid + 2, y + 27, x1 - 6, y + 50), True, end, i == chosen_week and chosen_end)
    groove(c, 10, len(WEEKS))
    mw, start, end = WEEKS[chosen_week]
    page(c, mw, chosen_end, end if chosen_end else start, '98,150K GOLD' if chosen_end else '116,400K GOLD')
    c.save(name, caption, note)


# ---- J3: the two kinds as two lists of ten, behind a three-way switch

def j3(name, caption, note):
    c = Canvas()
    ends = [s for s in ALL if s[1]]
    starts = [s for s in ALL if not s[1]]
    side_switch(c, ('Manual · 4', f'Week start · {len(starts)}', f'Turn END · {len(ends)}'), 2)
    heading(c, 'the game keeps the last ten')
    clear_list(c)
    for i, (mw, end, time) in enumerate(ends[:10]):
        y = LIST_TOP + i * AUTO_PITCH
        save_row(c, (LIST_X, y, LIST_X + LIST_W, y + AUTO_H), mw, end, time, i == 0)
    mw, end, time = ends[0]
    page(c, mw, end, time, '98,150K GOLD')
    c.save(name, caption, note)


os.makedirs(OUT, exist_ok=True)
j1('J1.png', 1, 'J1 — one list of twenty, newest first; the seal tells the kind',
   'A rising sun on gold: the week\'s start. A moon on dark in gold: its Turn END. The list scrolls (ten in view). Chosen: a Turn END.')
j1('J1s.png', 0, 'J1s — the same list, the newest week\'s start chosen',
   'The page\'s tag and its two lines follow the kind; the week now under way has no Turn END yet.')
j2('J2.png', 1, True, 'J2 — a row a week: its start and its Turn END side by side',
   'Ten weeks in view, a half chosen; eleven weeks hold the twenty saves, so it scrolls a row. The week under way: its Turn END dashed, "—".')
j3('J3.png', 'J3 — two lists of ten behind a three-way switch',
   'Manual · Week start · Turn END: each list as today\'s auto-saves, its seal by its kind. Chosen: the newest Turn END.')
