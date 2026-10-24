"""Mock-ups of the touch mode's Skip and Log buttons in the dialogues, round 2 (owner, 2026-10-24: the buttons' place
and look in the scenes first — today's steel pills cover the portraits' name plates and the map and move from window
to window; the battle's Skip stays as it is; docs/ui/touch.md, "The dialogue buttons, round 2"), drawn over window
dumps of the week-18 save on the tablet's frame (1280x824, the menu bar included).

The dumps (a window, without --touch so today's pills are not in them; the run moves the system pointer):
  <Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look-131
      --slots-from user://saves-week18 --harem 6 --until deployment --shots <dump dir>
(00010-message-place-title.png, 00011-message-speech-left.png, 00012-message-speech-right.png,
00034-message-cg-bar-speech.png: window 131 under the masked event CG). Then, from the repository's root (Pillow;
godot/fonts):
  python -I tools/ui/mock_dialogue_buttons.py <dump dir> <out dir> < /dev/null
The images hold the units' portraits: keep <out dir> outside the repository.

The buttons are drawn after godot/Strategy/Lacquer (the panel's lacquer, its double gold frame), the style of our
message windows, in the dump's pixels: the 1280x800 screen starts at y 24, under the menu bar.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

DUMPS, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H, BAR = 1280, 824, 24
TABLET_MM = 0.203

# Lacquer's colours (godot/Strategy/Lacquer.cs) and the lit state (TouchPill's gold).
PANEL = (22, 16, 12, 240)
GOLD, DIM_GOLD = (196, 160, 86), (110, 86, 46)
TEXT, QUIET, OFF = (244, 232, 206), (176, 160, 130), (98, 88, 74)
LIT_TOP, LIT_FOOT, LIT_INK = (255, 214, 110), (214, 140, 40), (40, 24, 8)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)

SPEECH_RIGHT, SPEECH_LEFT = '00012-message-speech-right.png', '00011-message-speech-left.png'
PLACE, BAR_131 = '00010-message-place-title.png', '00034-message-cg-bar-speech.png'

# The windows' frames in those dumps (x0, y0, x1, y1; measured).
WIN = {SPEECH_RIGHT: (58, 536, 892, 790), SPEECH_LEFT: (436, 28, 1270, 282), PLACE: (248, 560, 1032, 818),
       BAR_131: (0, 612, 1280, 824)}


def font(name, size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), int(size * S))


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
CINZEL = lambda size: font('Cinzel-VariableFont_wght.ttf', size)


def mm(px):
    return f'{px * TABLET_MM:.1f} mm'


class Frame:
    def __init__(self, name):
        base = Image.open(os.path.join(DUMPS, name)).convert('RGB')
        self.img = base.resize((W * S, H * S), Image.LANCZOS).convert('RGBA')

    def draw(self):
        lay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay):
        self.img = Image.alpha_composite(self.img, lay)

    def save(self, name):
        self.img.resize((W, H), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def sc(*v):
    return [round(a * S) for a in v]


def text(f, x, y, t, fnt, colour, anchor='mm'):
    lay, d = f.draw()
    d.text((x * S, y * S), t, font=fnt, fill=colour, anchor=anchor)
    f.add(lay)


def note(f, x, y, t, anchor='la', size=15):
    """A note on a dark plate, so it reads over the picture."""
    lay, d = f.draw()
    fnt = GOTHIC(size)
    l, t0, r, b = d.textbbox((x * S, y * S), t, font=fnt, anchor=anchor)
    d.rectangle((l - 5 * S, t0 - 3 * S, r + 5 * S, b + 3 * S), fill=(0, 0, 0, 210))
    d.text((x * S, y * S), t, font=fnt, fill=NOTE, anchor=anchor)
    f.add(lay)


def caption(f, lines, y=40):
    for i, t in enumerate(lines):
        note(f, 16, y + i * 24, t, size=17 if i == 0 else 15)


def measure(f, x, y0, y1, label, left=False):
    lay, d = f.draw()
    d.line(sc(x, y0, x, y1), fill=NOTE, width=2 * S)
    d.line(sc(x - 4, y0, x + 4, y0), fill=NOTE, width=2 * S)
    d.line(sc(x - 4, y1, x + 4, y1), fill=NOTE, width=2 * S)
    f.add(lay)
    note(f, x - 10 if left else x + 10, (y0 + y1) / 2, label, anchor='rm' if left else 'lm', size=14)


def shadow(f, r, radius=0):
    lay, d = f.draw()
    d.rounded_rectangle(sc(r[0] + 2, r[1] + 4, r[2] + 2, r[3] + 4), radius=radius * S, fill=(0, 0, 0, 120))
    f.add(lay.filter(ImageFilter.GaussianBlur(4 * S)))


def gradient(f, r, top, foot, radius=0, alpha=255):
    x0, y0, x1, y1 = [round(v) for v in r]
    g = Image.new('RGBA', ((x1 - x0) * S, (y1 - y0) * S))
    gd = ImageDraw.Draw(g)
    for y in range(g.height):
        t = y / max(1, g.height - 1)
        gd.line((0, y, g.width, y), fill=tuple(int(a + (b - a) * t) for a, b in zip(top, foot)) + (alpha,))
    mask = Image.new('L', g.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, g.width - 1, g.height - 1), radius=radius * S, fill=255)
    lay = Image.new('RGBA', f.img.size, (0, 0, 0, 0))
    lay.paste(g, (x0 * S, y0 * S), mask)
    f.add(lay)


def glow(f, r, radius):
    lay, d = f.draw()
    d.rounded_rectangle(sc(r[0] - 6, r[1] - 6, r[2] + 6, r[3] + 6), radius=(radius + 6) * S,
                        outline=(255, 190, 90, 190), width=10 * S)
    f.add(lay.filter(ImageFilter.GaussianBlur(6 * S)))


def lacquer(f, r, lit=False, radius=0, corners=True):
    """A lacquer plate in our windows' manner: the lacquer, the gold frame, a dim inner line, gold corner studs;
    lit, the plate is gold (the skip on)."""
    x0, y0, x1, y1 = r
    if lit:
        glow(f, r, radius)
    shadow(f, r, radius)
    if lit:
        gradient(f, r, LIT_TOP, LIT_FOOT, radius)
    else:
        gradient(f, r, (40, 30, 22), PANEL[:3], radius, PANEL[3])
    lay, d = f.draw()
    d.rounded_rectangle(sc(x0, y0, x1, y1), radius=radius * S, outline=(255, 244, 210) if lit else GOLD, width=2 * S)
    if not lit and radius < 12:
        d.rounded_rectangle(sc(x0 + 4.5, y0 + 4.5, x1 - 4.5, y1 - 4.5), radius=max(0, radius - 4) * S,
                            outline=DIM_GOLD, width=S)
    if corners and radius < 12:
        for cx, cy in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
            d.polygon(sc(cx, cy - 4, cx + 4, cy, cx, cy + 4, cx - 4, cy), fill=GOLD)
    f.add(lay)


def arrows(f, cx, cy, size, colour):
    """The skip's icon: two triangles pointing right."""
    lay, d = f.draw()
    h = size / 2
    for dx in (-size * 0.55, size * 0.05):
        d.polygon(sc(cx + dx, cy - h, cx + dx + size * 0.6, cy, cx + dx, cy + h), fill=colour + (255,))
    f.add(lay)


def lines(f, cx, cy, size, colour):
    """The log's icon: three lines of text, the last shorter."""
    lay, d = f.draw()
    for i, w in enumerate((1, 1, 0.6)):
        y = cy - size * 0.4 + i * size * 0.4
        d.rounded_rectangle(sc(cx - size / 2, y - size * 0.08, cx - size / 2 + size * w, y + size * 0.08),
                            radius=size * 0.08 * S, fill=colour + (255,))
    f.add(lay)


def menu_icon(f, cx, cy, size, colour):
    """The quick menu's seal: a gold diamond over three short strokes (a closed fan)."""
    lay, d = f.draw()
    for i in range(3):
        y = cy - size * 0.3 + i * size * 0.3
        d.rounded_rectangle(sc(cx - size * 0.42, y - size * 0.07, cx + size * 0.42, y + size * 0.07),
                            radius=size * 0.07 * S, fill=colour + (255,))
    f.add(lay)


def tab(f, r, label, icon, lit=False, disabled=False, size=18):
    """A button with its icon left of its label (variants A and the quick menu's rows)."""
    lacquer(f, r, lit, radius=6)
    ink = LIT_INK if lit else OFF if disabled else TEXT
    h = r[3] - r[1]
    cy = (r[1] + r[3]) / 2
    icon(f, r[0] + h * 0.55, cy, h * 0.38, LIT_INK if lit else OFF if disabled else GOLD)
    text(f, (r[0] + h * 0.95 + r[2] - 8) / 2, cy + 1, label, GOTHIC(size), ink)


# ------------------------------------------------------------------------------------------------- A: on the window
TAB_H, SKIP_W, LOG_W, TAB_GAP, TAB_IN = 46, 140, 112, 10, 22


def window_tabs(name, below=False, lit=False):
    """A: the pair rides on its window's edge, at the right end, as 131's speaker plate does at the left: on the top
    edge, else (a window at the screen's top) hanging from the bottom edge."""
    x0, y0, x1, y1 = WIN[name]
    right = min(x1, W) - TAB_IN
    y = y1 - 2 if below else y0 - TAB_H + 2
    log = (right - LOG_W, y, right, y + TAB_H)
    skip = (log[0] - TAB_GAP - SKIP_W, y, log[0] - TAB_GAP, y + TAB_H)
    f = Frame(name)
    tab(f, skip, 'Skip: ON' if lit else 'Skip', arrows, lit=lit)
    tab(f, log, 'Log', lines)
    return f, skip, log


def variant_a():
    f, skip, log = window_tabs(SPEECH_RIGHT)
    caption(f, ['A1 — on the window: the pair rides on its top edge at the right end, in the window\'s lacquer',
                f'Skip {SKIP_W}x{TAB_H}, Log {LOG_W}x{TAB_H} = {mm(TAB_H)} high; nothing over a portrait or the map',
                'the right speech: over the scene\'s picture, beside the portrait\'s name plate'])
    measure(f, log[2] + 12, log[1], log[3], mm(TAB_H))
    f.save('A1-window-speech-right.png')

    f, skip, log = window_tabs(SPEECH_LEFT, below=True, lit=True)
    caption(f, ['A2 — a window at the screen\'s top: the pair hangs from its bottom edge; the skip on (lit gold)',
                'while skipping, the windows come and go: the pair moves with each, so Skip is a moving target',
                'between windows (no window up) it stays where the last one left it'], y=620)
    f.save('A2-window-top-skipping.png')

    f, skip, log = window_tabs(BAR_131)
    caption(f, ['A3 — window 131: on its top edge at the right, the speaker\'s plate at the left',
                'the CG loses a 46-px strip at its foot\'s right end'])
    f.save('A3-window-131.png')


# ------------------------------------------------------------------------------------------------- B: the left edge
EDGE_W, CELL_H = 52, 84
EDGE_Y = BAR + 590                          # the drawer's top in the screen's 800 (dump y 614)
EDGE_Y_131 = BAR + 380                      # over 131: above its window and clear of its speaker plate


def drawer(f, top, lit=False, log_off=False):
    """B: a lacquer drawer glued to the screen's left edge, Skip over Log, icons over small labels."""
    r = (-14, top, EDGE_W, top + 2 * CELL_H + 4)
    lacquer(f, r, radius=10, corners=False)
    skip = (0, top + 2, EDGE_W - 2, top + 2 + CELL_H)
    log = (0, skip[3], EDGE_W - 2, skip[3] + CELL_H)
    if lit:
        lacquer(f, (skip[0] + 3, skip[1] + 3, skip[2] - 3, skip[3] - 3), lit=True, radius=8, corners=False)
    lay, d = f.draw()
    d.line(sc(8, skip[3], EDGE_W - 10, skip[3]), fill=DIM_GOLD, width=S)
    d.polygon(sc(EDGE_W / 2 - 1, skip[3] - 3, EDGE_W / 2 + 2, skip[3], EDGE_W / 2 - 1, skip[3] + 3, EDGE_W / 2 - 4,
                 skip[3]), fill=GOLD)
    f.add(lay)
    cx = EDGE_W / 2 - 1
    arrows(f, cx + 2, skip[1] + 32, 24, LIT_INK if lit else GOLD)
    text(f, cx, skip[1] + 64, 'ON' if lit else 'Skip', GOTHIC(13), LIT_INK if lit else QUIET)
    lines(f, cx, log[1] + 32, 24, OFF if log_off else GOLD)
    text(f, cx, log[1] + 64, 'Log', GOTHIC(13), OFF if log_off else QUIET)
    return skip, log


def variant_b():
    f = Frame(SPEECH_RIGHT)
    skip, log = drawer(f, EDGE_Y)
    caption(f, ['B1 — the left edge: a lacquer drawer glued to the screen\'s edge, always at the same place',
                f'two cells {EDGE_W}x{CELL_H} = {mm(EDGE_W)} x {mm(CELL_H)}, under the left thumb; '
                'between the edge and every window (the right speech starts at x 58)',
                'never over a portrait, a name plate or a window\'s text'])
    f.save('B1-edge-speech-right.png')

    f = Frame(PLACE)
    drawer(f, EDGE_Y, lit=True, log_off=True)
    caption(f, ['B2 — the skip on: the Skip cell lit gold, "ON"; it stays where it was, so a tap stops the skip',
                'whatever window flashes by; Log greyed while no key wait shows (a place\'s title, the skip)'])
    f.save('B2-edge-skipping.png')

    f = Frame(BAR_131)
    drawer(f, EDGE_Y_131)
    caption(f, ['B3 — window 131 spans the foot: the drawer rises above it on the same edge (the one other place),',
                'clear of the speaker\'s plate; it covers 52 px of the CG\'s left edge'])
    f.save('B3-edge-131.png')

    f = Frame(SPEECH_LEFT)
    drawer(f, EDGE_Y)
    caption(f, ['B4 — a window at the top: the drawer as everywhere, under the left portrait'])
    f.save('B4-edge-speech-left.png')


# ------------------------------------------------------------------------------------------------- C: one seal
SEAL = 60


def seal(f, top, lit=False):
    """C: one round lacquer seal on the left edge (the quick menu); lit with the skip's arrows while skipping."""
    r = (-20, top, SEAL - 6, top + SEAL)
    lacquer(f, r, lit=lit, radius=SEAL / 2, corners=False)
    cx, cy = (r[2] - 2 - SEAL / 2 + 14), top + SEAL / 2
    if lit:
        arrows(f, cx + 2, cy, 24, LIT_INK)
    else:
        menu_icon(f, cx, cy, 26, GOLD)
    return r


def variant_c():
    top = EDGE_Y + 50
    f = Frame(SPEECH_RIGHT)
    r = seal(f, top)
    caption(f, ['C1 — one seal on the left edge: the quick menu, the smallest footprint',
                f'{SEAL - 6}x{SEAL} = {mm(SEAL - 6)} x {mm(SEAL)}; a tap opens its rows, a second tap chooses '
                '(Log takes two taps)'])
    f.save('C1-seal-speech-right.png')

    f = Frame(SPEECH_RIGHT)
    seal(f, top)
    rows_x, row_w = 16, 200
    y1 = top - 12
    rows = [('Skip', arrows), ('Log', lines)]
    menu = (rows_x - 8, y1 - len(rows) * (TAB_H + 8) - 8, rows_x + row_w + 8, y1)
    lacquer(f, menu, radius=4)
    for i, (label, icon) in enumerate(rows):
        y = menu[1] + 8 + i * (TAB_H + 8)
        tab(f, (rows_x, y, rows_x + row_w, y + TAB_H), label, icon)
    caption(f, ['C2 — the seal tapped: its rows above it (room for more later: Auto, Save...), '
                'a tap off them closes it'])
    f.save('C2-seal-open.png')

    f = Frame(PLACE)
    seal(f, top, lit=True)
    caption(f, ['C3 — the skip on: the seal itself lit gold with the skip\'s arrows; one tap on it stops the skip'])
    f.save('C3-seal-skipping.png')




variant_a()
variant_b()
variant_c()
print('done')
