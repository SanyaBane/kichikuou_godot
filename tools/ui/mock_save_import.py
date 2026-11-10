"""Mock-ups of importing the original game's saves (docs/save-import.md, §6): where the player finds the saves the
original wrote (KICHIKUSA.asd ...) and how they look once they are in. Drawn over the screen gallery's dumps
(docs/ui/gallery.md; 1280x800): "save-load-title" (Load from the title), "options-game" (Option -> Game) and
"import-done" (the first launch's import screen). From the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_save_import.py <gallery's pc/current dir> <out dir>
Round 1: A1 a third list on the Save/Load screen, read from the game folder and loaded in place; B1/B2 an Import in
Option -> Game, before and after; C1 a step of the first launch's import screen; M1 the imported saves among the manual
saves (after B or C). The saves are the owner's ten (slots A-J, their weeks and stages as read); their other figures
and the map are made up (the dumps' week 18).
Round 2 (the owner's §7: File -> Import Original Save..., loaded at once; the game marked, decision 30 off in it): how
the mark shows on that game's saves — R1 a paper tag "Original" on the row and the page, R2 an indigo seal and an indigo
tag; each over the manual saves ("save-load-title") and the auto-saves ("save-load-auto-end"), six of the ten rows
the imported game's, the rest an older game's. O1: Option -> Tweaks ("options-tweaks") in a marked game, "New scenes
first" greyed with its reason.
The images hold the player's CGs: keep <out dir> outside the repository. Everything is in the 1280x800 screen's pixels
(plus a caption band below); drawn at 2x and scaled down.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

DUMPS, OUT = sys.argv[1:3]
sys.stdout.reconfigure(encoding='utf-8')
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
S = 2
W, H, CAPTION = 1280, 800, 64

# UiPalette.Lacquer and SaveLoadView's own colours (as tools/ui/mock_autosaves.py)
PANEL, GOLD, DIM = (21, 15, 12, 255), (196, 160, 86, 255), (110, 86, 46, 255)
NAME, SUB, LABEL = (232, 199, 122, 255), (160, 140, 104, 255), (150, 134, 108, 255)
VALUE, ROW = (236, 226, 204, 255), (34, 26, 20, 255)
TEXT, QUIET, OFF = (244, 232, 206, 255), (176, 160, 130, 255), (98, 88, 74, 255)
VERMILION, CHOSEN = (170, 40, 28, 255), (60, 30, 22, 255)
SEAL_TEXT, GROOVE = (246, 230, 206, 255), (40, 32, 24, 255)
# the paper (OptionsView, ImportScreen)
INK, INK_QUIET, INK_GREY = (40, 28, 18, 255), (100, 80, 56, 255), (150, 132, 104, 255)
VERMILION_INK, HEAD_INK = (150, 52, 30, 255), (120, 40, 26, 255)
PAPER_RULE = (150, 120, 80, 255)
TICK = (70, 110, 50, 255)
# ours for the original's saves: an indigo seal (A1), a paper tag framed in gold (M1)
INDIGO, INDIGO_RIM = (44, 52, 92, 255), (120, 132, 186, 255)
TAG_PAPER = (222, 206, 168, 255)
RED_TOP, RED_BOTTOM = (136, 30, 24, 255), (98, 18, 16, 255)

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
    def __init__(self, dump):
        img = Image.open(os.path.join(DUMPS, dump + '.png')).convert('RGBA').crop((0, 0, W, H))
        self.src = img
        self.im = Image.new('RGBA', (W * S, (H + CAPTION) * S), (14, 10, 8, 255))
        self.im.paste(img.resize((W * S, H * S), Image.LANCZOS), (0, 0))
        self.d = ImageDraw.Draw(self.im)

    def pixel(self, x, y):
        return self.src.getpixel((x, y))

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill, outline=outline, width=round(width * S))

    def text(self, xy, s, f, fill=TEXT, anchor='lm'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill=DIM, width=1.0):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=max(1, round(width * S)))

    def circle(self, cx, cy, r, fill=None, outline=None, width=1):
        self.d.ellipse([(cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S], fill=fill, outline=outline,
                       width=round(width * S))

    def save(self, name, caption, note):
        self.rect((0, H, W, H + CAPTION), fill=(23, 24, 27, 255))
        self.line((0, H), (W, H), fill=DIM)
        self.text((16, H + 20), caption, cinzel(20), fill=NAME)
        self.text((16, H + 46), note, font(15), fill=QUIET)
        self.im.resize((W, H + CAPTION), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))
        print(name)


def red_button(c, r, label, size=20, on=True):
    """DrawRedButton: red lacquer in a gold frame; off, it is grey."""
    x0, y0, x1, y1 = r
    if on:
        c.rect(r, fill=RED_TOP, outline=GOLD, width=2)
        c.rect((x0 + 4, (y0 + y1) / 2, x1 - 4, y1 - 4), fill=RED_BOTTOM)
        c.rect((x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(190, 70, 50, 255), width=1)
        c.text(((x0 + x1) / 2, (y0 + y1) / 2), label, cinzel(size), fill=SEAL_TEXT, anchor='mm')
    else:
        c.rect(r, fill=(70, 62, 52, 255), outline=(120, 110, 96, 255), width=1)
        c.text(((x0 + x1) / 2, (y0 + y1) / 2), label, cinzel(size), fill=(150, 140, 124, 255), anchor='mm')


def paper_button(c, r, label, size=15, quiet=False):
    """A secondary button on the paper: a frame in the ink's grey, the word in ink (Defaults' look)."""
    c.rect(r, fill=c.paper_light, outline=INK_GREY, width=1)
    c.text(((r[0] + r[2]) / 2, (r[1] + r[3]) / 2), label, cinzel(size), fill=INK_QUIET if quiet else INK, anchor='mm')


# ---- the owner's ten saves as read (docs/save-import.md, §4): slot, week, stage, month, week of the month

SAVES = [('A', 22, 2, 6, 1), ('B', 20, 1, 5, 3), ('C', 15, 1, 4, 2), ('D', 17, 1, 4, 4), ('E', 18, 1, 5, 1),
         ('F', 19, 2, 5, 2), ('G', 22, 2, 6, 1), ('H', 21, 2, 5, 4), ('I', 16, 1, 4, 3), ('J', 15, 1, 4, 1)]
FILE_TIME = '10 Oct, 21:40'
STAGES = {1: 'Stage 1 · Leazas civil war', 2: 'Stage 2 · wars of humanity'}


def short(s):
    return f'LP 3 · M{s[3]} · W{s[4]}'


def long(s):
    return f'LP 3 · Month {s[3]} · Week {s[4]}'


def by_week(saves):
    """Newest week first; slot A heads its week (the original's own autosave)."""
    return sorted(saves, key=lambda s: (s[1], s[0] == 'A', s[0]), reverse=True)   # B's numbering, reversed


# ---- the Save/Load screen's parts as built (SaveLoadView), redrawn where a variant changes them

LIST_X, LIST_W, LIST_TOP, ROW_H, PITCH = 52, 330, 162, 52, 57
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


def heading(c, left, right=None, button=None):
    erase(c, (LIST_X - 2, 124, LIST_X + LIST_W + 2, 158))
    c.text((LIST_X, 141), left, cinzel(11), fill=SUB)
    if right:
        c.text((LIST_X + LIST_W, 141), right, font(11), fill=LABEL, anchor='rm')
    if button:
        # "Delete all without ★ (n)": Cinzel has no star, so the ★ is the game's font in gold, as built
        r = (159, 128, 382, 153)
        c.rect(r, fill=ROW, outline=DIM, width=1)
        before, after = button.split('*')
        parts = ((before, cinzel(12), TEXT), ('★', font(13), GOLD), (after, cinzel(12), TEXT))
        x = (r[0] + r[2]) / 2 - sum(c.width(s, f) for s, f, _ in parts) / 2
        for s, f, colour in parts:
            c.text((x, 141), s, f, fill=colour)
            x += c.width(s, f)


def clear_list(c):
    erase(c, (LIST_X - 2, LIST_TOP - 2, LIST_X + LIST_W + 2, 760))


def groove(c, shown, total):
    top, span, x = LIST_TOP, 756 - LIST_TOP, LIST_X + LIST_W - 8
    c.rect((x, top, x + 6, top + span), fill=GROOVE, outline=DIM, width=1)
    c.rect((x + 1, top + 1, x + 5, top + span * min(1, shown / total) - 1), fill=GOLD)


def number_seal(c, x, y, n):
    """A manual save's seal as built: its number on vermilion in a pale inner line."""
    c.rect((x, y, x + 24, y + 24), fill=VERMILION)
    c.rect((x + 2, y + 2, x + 22, y + 22), outline=(214, 150, 120, 255), width=1)
    c.text((x + 12, y + 13), str(n), cinzel(13 if n < 10 else 11), fill=SEAL_TEXT, anchor='mm')


def letter_seal(c, x, y, letter):
    """A1: the original's slot letter on an indigo seal, so the third list never reads as ours."""
    c.rect((x, y, x + 24, y + 24), fill=INDIGO)
    c.rect((x + 2, y + 2, x + 22, y + 22), outline=INDIGO_RIM, width=1)
    c.text((x + 12, y + 13), letter, cinzel(14), fill=SEAL_TEXT, anchor='mm')


def original_tag(c, right, my, label, size=12):
    """M1: "Original A" — a paper tag framed in gold where an auto-save's row has its Turn Start / Turn End; its left edge."""
    f = font(size)
    w = c.width(label, f) + 14
    h = size + 7
    r = (right - w, my - h / 2, right, my + h / 2)
    c.rect(r, fill=TAG_PAPER, outline=GOLD, width=1)
    c.text((right - w / 2, my + 0.5), label, f, fill=INK, anchor='mm')
    return right - w


def row_box(c, r, chosen):
    if chosen:
        c.rect(r, fill=CHOSEN, outline=VERMILION, width=2)
    else:
        c.rect(r, fill=ROW, outline=DIM, width=1)


def stat(c, i, value):
    """The figure under the map in place i (as DrawPage lays them out), its label left as dumped."""
    sx, sy = PAGE_X + i % 3 * 280, 465 + i // 3 * 46
    erase(c, (sx, sy + 17, sx + 250, sy + 44))
    ascent = font(17).getmetrics()[0] / S
    c.text((sx, sy + 18 + ascent), value, font(17), fill=VALUE, anchor='ls')


def page_title(c, title, tag_label):
    erase(c, (PAGE_X - 2, PAGE_Y, 1080, PAGE_Y + 30))
    c.text((PAGE_X, PAGE_Y + 14), title, cinzel(22), fill=NAME)
    x = PAGE_X + c.width(title, cinzel(22)) + 16
    original_tag(c, x + c.width(tag_label, font(13)) + 14, PAGE_Y + 14, tag_label, 13)


def page_lines(c, top, lines, colour=QUIET):
    for k, line in enumerate(lines):
        c.text((PAGE_X, top + 20 * k), line, font(14), fill=colour)


# ---- A1: a third list, read from the game folder and loaded in place

def a1():
    c = Canvas('save-load-title')
    side_switch(c, ('Manual · 12', 'Auto · 20', 'Original · 10'), 2)
    heading(c, 'IN THE GAME FOLDER', 'KICHIKUSA.asd – KICHIKUSJ.asd')
    clear_list(c)
    order = sorted(SAVES, key=lambda s: s[0])
    for i, s in enumerate(order):
        y = LIST_TOP + i * PITCH
        if y + ROW_H > 760:
            break
        r = (LIST_X, y, LIST_X + 318, y + ROW_H)
        row_box(c, r, i == 0)
        letter_seal(c, LIST_X + 8, y + ROW_H / 2 - 12, s[0])
        tx = LIST_X + 42
        c.text((tx, y + 16), short(s), cinzel(15), fill=NAME)
        c.text((LIST_X + 308, y + 16), FILE_TIME, font(14), fill=VALUE, anchor='rm')
        c.text((tx, y + 37), f'slot {s[0]} · KICHIKUS{s[0]}.asd' + (' · its auto-save' if s[0] == 'A' else ''),
               font(12), fill=QUIET)
    groove(c, 10, 10)
    s = order[0]
    page_title(c, long(s), 'Original · slot A')
    erase(c, (1086, 80, 1230, 118))                      # no Delete: the original's files stay as they are
    stat(c, 0, STAGES[s[2]])
    stat(c, 5, '10 Oct 2026, 21:40')
    erase(c, (PAGE_X - 2, 560, 1228, 690))
    page_lines(c, 575, ("A save of the original game, read from its folder (KICHIKUSA.asd). Loading it plays",
                        "the week's start again; your next saves go into your own lists. The file is left as it is."),
               TEXT)
    c.save('A1.png', 'A1 — a third list: the original\'s saves read from the game folder, loaded in place',
           'Its slots by letter on an indigo seal, the file\'s time (the same for all: ENG-07). No Delete, no comment; '
           'in Save the red button is off.')


# ---- B1, B2: Option -> Game, an Import under the game folder

def paper_table(c, top, rows, after):
    """The folder's saves in a small table on the paper: slot, date, stage, file, and what happens (or happened)."""
    x0, x1 = 166, 1113
    cols = (x0 + 8, x0 + 70, x0 + 280, x0 + 520, x0 + 700)
    for k, head in enumerate(('SLOT', 'DATE', 'STAGE', 'FILE', 'IMPORTED AS' if after else 'WILL BE')):
        c.text((cols[k], top + 8), head, cinzel(11), fill=HEAD_INK)
    c.line((x0, top + 18), (x1, top + 18), fill=PAPER_RULE)
    y = top + 34
    for s, outcome in rows:
        refused = outcome.startswith('refused')
        ink = INK_GREY if refused else INK
        c.text((cols[0], y), s[0], font(14), fill=ink)
        c.text((cols[1], y), short(s), font(14), fill=ink)
        c.text((cols[2], y), STAGES[s[2]].split(' · ')[0] if s[2] in STAGES else '—', font(14), fill=ink)
        c.text((cols[3], y), f'KICHIKUS{s[0]}.asd', font(14), fill=INK_QUIET)
        if refused:
            c.text((cols[4], y), outcome, font(14), fill=VERMILION_INK)
        elif after and outcome.startswith('Manual'):
            c.text((cols[4], y), '✓', font(15), fill=TICK)
            c.text((cols[4] + 20, y), outcome, font(15), fill=INK)
        else:
            c.text((cols[4], y), outcome, font(15), fill=INK_QUIET if outcome.startswith('already') else INK)
        y += 22
    return y


def b(name, after):
    c = Canvas('options-game')
    paper = c.pixel(640, 450)
    c.paper_light = tuple(min(255, v + 10) for v in paper[:3]) + (255,)
    c.rect((150, 268, 1130, 676), fill=paper)
    x0, x1 = 166, 1113
    c.text((x0, 300), 'SAVES OF THE ORIGINAL GAME', cinzel(13), fill=HEAD_INK)
    c.line((x0, 312), (x1, 312), fill=PAPER_RULE)
    if not after:
        c.text((x0, 330), '10 saves found in the game folder, weeks 15 to 22. They are copied into your manual saves;',
               font(15), fill=INK)
        c.text((x0, 350), 'the files stay as they are, and the gallery and the clears they hold are added to yours.',
               font(14), fill=INK_QUIET)
        rows = [(s, f'Manual save {n}') for n, s in zip(range(13, 23), sorted(SAVES, key=lambda s: (s[1], s[0] == 'A', s[0])))]
        rows.sort(key=lambda r: r[0][0])
        bottom = paper_table(c, 362, rows, False)
        red_button(c, (x0, 616, x0 + 260, 654), 'Import 10 saves', 17)
        paper_button(c, (x0 + 276, 616, x0 + 276 + 230, 654), 'Choose another folder…')
        c.save(name, 'B1 — Option, Game tab: the original\'s saves found in the game folder, imported by one button',
               'Each becomes a manual save after the highest (No. 13-22 here), numbered by week; the gallery and the '
               'clears merged into ours. Made up: the numbers.')
    else:
        c.text((x0, 330), '9 saves imported into your manual saves (No. 13-21); the gallery and the clears added to yours.',
               font(15), fill=INK)
        c.text((x0, 350), 'An import again adds only saves the original wrote since.', font(14), fill=INK_QUIET)
        order = sorted(SAVES, key=lambda s: (s[1], s[0] == 'A', s[0]))
        numbers, n = {}, 13
        for s in order:
            if s[0] == 'J':
                continue
            numbers[s[0]] = n
            n += 1
        rows = []
        for s in sorted(SAVES, key=lambda s: s[0]):
            rows.append((s, 'refused: another version of the game' if s[0] == 'J' else f'Manual save {numbers[s[0]]}'))
        paper_table(c, 362, rows, True)
        red_button(c, (x0, 616, x0 + 260, 654), 'Import 10 saves', 17, on=False)
        paper_button(c, (x0 + 276, 616, x0 + 276 + 230, 654), 'Choose another folder…')
        c.save(name, 'B2 — the same after the import: what became of each save',
               'Slot J refused as a sample (a save of another version: the reason in words). The button off until the folder '
               'holds a save not imported yet.')


# ---- C1: the first launch's import screen, a step for the saves before Play

def numeral(c, cx, cy, ch, state):
    if state == 'now':
        c.circle(cx, cy, 15, fill=VERMILION_INK)
        c.text((cx, cy + 1), ch, font(17), fill=SEAL_TEXT, anchor='mm')
    else:
        colour = VERMILION_INK if state == 'done' else INK_GREY
        c.circle(cx, cy, 15, outline=colour, width=2)
        c.text((cx, cy + 1), ch, font(17), fill=colour, anchor='mm')


def c1():
    c = Canvas('import-done')
    paper = c.pixel(640, 600)
    c.paper_light = tuple(min(255, v + 10) for v in paper[:3]) + (255,)
    c.rect((300, 434, 992, 660), fill=paper)
    numeral(c, 331, 455, '三', 'now')
    c.text((364, 455), 'Saves of the original game', font(20), fill=INK)
    c.text((364, 487), '10 saves in this folder, LP 3 · Month 4 · Week 1 to Month 6 · Week 1.', font(14), fill=INK_QUIET)
    c.text((364, 509), 'Copied into your manual saves; the files stay as they are.', font(14), fill=INK_QUIET)
    red_button(c, (364, 530, 604, 570), 'Import them', 17)
    paper_button(c, (620, 530, 780, 570), 'Not now', 17)
    numeral(c, 331, 610, '四', 'next')
    c.text((364, 610), 'Play', font(20), fill=INK_GREY)
    c.save('C1.png', 'C1 — the first launch: the import screen offers the saves the folder holds, before Play',
           'Import them | Not now (the way back at the right); shown only when the folder holds a save. Option, Game '
           'keeps the same Import for later.')


# ---- M1: the imported saves among the manual saves (a first launch: only they are there)

def m1():
    c = Canvas('save-load-title')
    order = by_week(SAVES)                               # newest week first, as NewestFirst shows them
    numbers = {s[0]: 10 - i for i, s in enumerate(order)}
    side_switch(c, ('Manual saves · 10', 'Auto-saves · 0'), 0)
    heading(c, 'NEWEST FIRST', button='Delete all without * (10)')
    clear_list(c)
    for i, s in enumerate(order):
        y = LIST_TOP + i * PITCH
        r = (LIST_X, y, LIST_X + 318, y + ROW_H)
        row_box(c, r, i == 0)
        number_seal(c, LIST_X + 8, y + ROW_H / 2 - 12, numbers[s[0]])
        tx = LIST_X + 42
        c.text((tx, y + 16), short(s), cinzel(15), fill=NAME)
        c.text((LIST_X + 308, y + 16), FILE_TIME, font(14), fill=VALUE, anchor='rm')
        original_tag(c, LIST_X + 308, y + 38, f'Original {s[0]}')
    groove(c, 10, 10)
    s = order[0]
    page_title(c, f'No. {numbers[s[0]]} · {long(s)}', 'Original · slot A')
    stat(c, 0, STAGES[s[2]])
    stat(c, 5, '10 Oct 2026, 21:40')
    erase(c, (PAGE_X - 2, 560, 1228, 690))
    c.text((PAGE_X, 571), 'Comment', font(13), fill=LABEL)
    c.rect((426, 585, 1122, 619), fill=ROW, outline=DIM, width=1)
    c.text((436, 602), 'none — Edit writes one', font(16), fill=LABEL)
    c.rect((1132, 585, 1226, 619), fill=ROW, outline=DIM, width=1)
    c.text((1179, 602), 'Edit', cinzel(14), fill=TEXT, anchor='mm')
    page_lines(c, 642, ("Imported from the original game's slot A (KICHIKUSA.asd), its auto-save. Loading it plays",
                        "the week's start again: the budget, then the commands."))
    c.save('M1.png', 'M1 — after B or C: the imported saves are manual saves, tagged "Original" with their slot',
           'Numbered by week, so the newest heads the list; the file\'s time. Delete, comment, sweep as any manual save. '
           'Made up: figures, map.')


# ---- Round 2: the mark of an imported game on its saves (R1 a paper tag, R2 an indigo seal), the greyed option (O1)

import math

MARKED_ROWS = 6                      # the list's first six rows are the imported game's saves, the rest an older game's
KINDS = [False, True, False, True, False, True, False, True, False, True]     # save-load-auto-end: Start, End, ...
COMMENTED = {'manual': (0, 3, 6), 'auto': (1,)}                                # rows with a ★ on the seal, as dumped
AUTO_GOLD = (138, 104, 34, 255)


def star(c, cx, cy, r, colour):
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        rr = r if i % 2 == 0 else r * 0.45
        pts.append(((cx + math.cos(a) * rr) * S, (cy + math.sin(a) * rr) * S))
    c.d.polygon(pts, fill=colour)


def indigo_seal(c, x, y, word, starred):
    c.rect((x, y, x + 24, y + 24), fill=INDIGO)
    c.rect((x + 2, y + 2, x + 22, y + 22), outline=INDIGO_RIM, width=1)
    c.text((x + 12, y + 13), word, cinzel(13 if len(word) == 1 else 11), fill=SEAL_TEXT, anchor='mm')
    if starred:
        star(c, x + 1, y + 1, 9, (26, 18, 12, 255))
        star(c, x + 1, y + 1, 7, GOLD)


def indigo_tag(c, x, my, label, size=13):
    """R2's page tag: indigo framed in its rim; its right edge."""
    f = font(size)
    w = c.width(label, f) + 14
    h = size + 7
    c.rect((x, my - h / 2, x + w, my + h / 2), fill=INDIGO, outline=INDIGO_RIM, width=1)
    c.text((x + w / 2, my + 0.5), label, f, fill=SEAL_TEXT, anchor='mm')
    return x + w


def kind_width(end):
    return font(12).getlength('Turn End' if end else 'Turn Start') / S + 14


def r_list(style, which):
    dump = 'save-load-title' if which == 'manual' else 'save-load-auto-end'
    c = Canvas(dump)
    for i in range(MARKED_ROWS):
        y = LIST_TOP + i * PITCH
        if style == 'tag-right':
            # the owner's R1 with "Original" at the row's right edge: the kind's tag moved left of it
            row_fill = CHOSEN if i == 1 else ROW
            kw = kind_width(KINDS[i])
            c.rect((LIST_X + 308 - kw - 2, y + 28, LIST_X + 310, y + 49), fill=row_fill)
            left = original_tag(c, LIST_X + 308, y + 38, 'Original')
            kr = (left - 6 - kw, y + 38 - 9.5, left - 6, y + 38 + 9.5)
            c.rect(kr, fill=(118, 30, 24, 255) if KINDS[i] else (70, 110, 50, 255), outline=GOLD, width=1)
            c.text(((kr[0] + kr[2]) / 2, y + 38.5), 'Turn End' if KINDS[i] else 'Turn Start', font(12), fill=TEXT,
                   anchor='mm')
            left = kr[0]
            if i in COMMENTED['auto']:
                tx = LIST_X + 42
                c.rect((tx - 2, y + 29, left - 1, y + 47), fill=row_fill)
                f = font(12)
                text = "before the rebels' turn"
                while c.width('“' + text + '…”', f) > left - tx - 8:
                    text = text[:-1]
                c.text((tx, y + 37), '“' + text + '…”', f, fill=QUIET)
        elif style == 'tag':
            right = LIST_X + 308
            if which == 'auto':
                right -= kind_width(KINDS[i]) + 6
            left = original_tag(c, right, y + 38, 'Original')
            if which == 'auto' and i in COMMENTED['auto']:
                # the comment cut before the tags, as the built row cuts it before its kind's tag
                tx = LIST_X + 42
                c.rect((tx - 2, y + 29, left - 1, y + 47), fill=CHOSEN if i == 1 else ROW)
                f = font(12)
                text = "before the rebels' turn"
                while c.width('“' + text + '…”', f) > left - tx - 8:
                    text = text[:-1]
                c.text((tx, y + 37), '“' + text + '…”', f, fill=QUIET)
        else:
            word = str(12 - i) if which == 'manual' else 'A'
            indigo_seal(c, LIST_X + 9, y + ROW_H / 2 - 12, word, i in COMMENTED[which])
    if which == 'manual':
        title, chosen_x = 'No. 12 · LP 3 · Month 5 · Week 2', None
        x = PAGE_X + c.width(title, cinzel(22)) + 16
    else:
        x = 830                                           # after the dumped "Auto" and "Turn End" tags
    if style in ('tag', 'tag-right'):
        original_tag(c, x + c.width('Original', font(13)) + 14, PAGE_Y + 14, 'Original', 13)
    else:
        indigo_tag(c, x, PAGE_Y + 14, 'Original')
    note = ("A game imported from the original's save: \"New scenes first\" and the talk marks are off in it.")
    if which == 'manual':
        erase(c, (PAGE_X - 2, 630, 1228, 684))
        c.text((PAGE_X, 650), note, font(14), fill=QUIET)
    else:
        erase(c, (PAGE_X - 2, 630, 1228, 684))
        c.text((PAGE_X, 642), "Written when Turn END was chosen. Loading it brings back the map with the week's commands as "
               "they were.", font(14), fill=QUIET)
        c.text((PAGE_X, 662), note, font(14), fill=QUIET)
    return c


def r_round():
    caption_notes = {
        ('tag', 'manual'): ('R1 — a paper tag "Original" on every save of the imported game: the manual saves',
                            'Six newest rows the imported game\'s, the rest an older game\'s. The page: the tag after the '
                            'title, one quiet line saying what it means.'),
        ('tag', 'auto'): ('R1a — the same on the auto-saves: "Original" left of Turn Start / Turn End',
                          'The kind\'s tag stays where it is; the mark goes before it, so the row\'s left keeps the '
                          'comment.'),
        ('seal', 'manual'): ('R2 — the seal in indigo for the imported game\'s saves: the manual saves',
                             'The row keeps its layout, the number on indigo instead of vermilion; the page\'s tag '
                             'indigo too. Quieter than R1.'),
        ('seal', 'auto'): ('R2a — the same on the auto-saves: "A" on indigo instead of gold',
                           'Indigo is the colour R2 gives the original. The star of a commented save stays on the seal.'),
        ('tag-right', 'auto'): ('R1b — the owner\'s R1: on the auto-saves "Original" at the right, Turn Start / End left of it',
                                'The row\'s right edge is the mark on every save of the imported game; the kind\'s tag '
                                'moves left, a comment is cut before both.'),
    }
    names = {('tag', 'manual'): 'R1.png', ('tag', 'auto'): 'R1a.png', ('seal', 'manual'): 'R2.png',
             ('seal', 'auto'): 'R2a.png', ('tag-right', 'auto'): 'R1b.png'}
    for key, (caption, note) in caption_notes.items():
        c = r_list(*key)
        c.save(names[key], caption, note)


def o1():
    c = Canvas('options-tweaks')
    paper = c.pixel(640, 640)
    c.rect((160, 574, 1120, 650), fill=paper)
    c.rect((166, 576, 186, 596), outline=INK_GREY, width=2)                    # the check box, greyed, empty
    c.text((198, 586), 'New scenes first', font(17), fill=INK_GREY)
    c.text((198, 606), 'Talks and harem visits play unread scenes first, with a few hints and a choice of ours.',
           font(13), fill=INK_GREY)
    c.text((198, 630), 'Off in this game: it was imported from a save of the original, which keeps no record of the '
           'scenes read.', font(13), fill=VERMILION_INK)
    c.save('O1.png', 'O1 — Option → Tweaks in a game imported from the original: "New scenes first" greyed'.replace('→', '-'),
           'Empty and greyed while the marked game runs; the player\'s own setting comes back in a game without the mark.')


os.makedirs(OUT, exist_ok=True)
r_round()
o1()
a1()
b('B1.png', False)
b('B2.png', True)
c1()
m1()
