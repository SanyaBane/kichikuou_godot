"""Mock-ups of the screen margins (owner, 2026-10-31; docs/ui/screen-margins.md, round 1).

On Android a screen's rounded corners and a protective film whose edges are not clear hide a strip of the screen that the
system's safe area does not report. The player sets margins from the screen's edges, upright and landscape each its own,
and the game lays every screen out inside them as if the screen were that much smaller.

The dumps: the screen gallery's phone passes (tools/ui/gallery.ps1 -PhoneLand -PhoneUp; docs/ui/gallery.md) —
phone-landscape/current (1808×813, two thirds of the reference) and phone-upright/current (610×1356, half of it):
map-markers and options-display. Then, from the repository's root (Pillow; godot/fonts):
  python -I tools/ui/mock_screen_margins.py <gallery dir> <out dir> < /dev/null
Keep <out dir> outside the repository: the dumps hold the player's CGs.

Every picture is drawn at the reference phone's pixels (the Poco X6 Pro, 1220×2712 at 446 ppi) and shown on a phone whose
screen is simulated: corners rounded at 5.5 mm and a film whose edge is opaque for 1.5 mm (FILM, RADIUS; made up). The
margins in the pictures are 2.8 mm on every edge (MARGIN): the film alone needs 1.5, the corners decide.

Round 1: P the screens today on that phone (the map upright and landscape); R the same with the margins (the layout as
if the screen were smaller: approximate, the dump scaled into the margins). How the player sets them:
- A the margins in Option → Display itself, − / + steppers for the way the phone is held now, a dashed line round the
  whole screen while the tab is open;
- B a screen of its own (Display's "Adjust…", E): black, the margins as a gold frame with corner marks, a tab on each
  edge to drag, the steppers in a panel in the middle, Reset | Done;
- C the same frame and tabs over the screen the player came from, laid out live inside the margins, the panel smaller.
The pink lines and text are the mock-ups' notes, not part of a screen.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

GALLERY, OUT = sys.argv[1:3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
LAND = os.path.join(GALLERY, 'phone-landscape', 'current')
UP = os.path.join(GALLERY, 'phone-upright', 'current')
os.makedirs(OUT, exist_ok=True)

LONG, SHORT = 2712, 1220                # the reference phone's screen
PX_MM = 446 / 25.4                      # 17.6 reference pixels a millimetre
HOLE, GESTURE = 120, 50                 # the Poco's camera strip (as it reports it) and the gesture zone
RADIUS = round(5.5 * PX_MM)             # the simulated screen's rounded corners (made up)
FILM = round(1.5 * PX_MM)               # the simulated film's opaque edge (made up)
MARGIN = round(2.8 * PX_MM)             # the margins set in the pictures: past the film and the corners

# Lacquer and paper (godot/Strategy/UiPalette.cs).
PANEL, BACKDROP = (22, 16, 12), (14, 10, 8)
GOLD, DIM_GOLD = (196, 160, 86), (110, 86, 46)
NAME, TEXT, QUIET, OFF_TEXT = (232, 199, 122), (244, 232, 206), (176, 160, 130), (98, 88, 74)
ROW = (34, 26, 20)
VERMILION, VERMILION_LINE = (170, 40, 28), (214, 150, 90)
PAPER, PAPER_HEAD, PAPER_RULE, INK = (224, 208, 172), (196, 176, 136), (192, 170, 132), (40, 28, 18)
PAPER_ROW, HEAD_INK, VERMILION_INK = (234, 222, 192), (120, 40, 26), (150, 52, 30)
NOTE = (255, 92, 160)
BODY = (28, 28, 32)                     # the phone's body round the screen
FILM_FILL = (52, 52, 58)                # the film's opaque edge


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size)))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)
CINZEL = lambda size, w=700: font('Cinzel-VariableFont_wght.ttf', size, w)


def mm(px):
    return f'{px / PX_MM:.1f} mm'


def dump(folder, name, size):
    return Image.open(os.path.join(folder, name + '.png')).convert('RGB').resize(size, Image.LANCZOS)


# --- the house's parts ---------------------------------------------------------------------------------------------

def panel(d, box, fill=PANEL, frame=GOLD):
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=fill)
    d.rectangle(box, outline=frame, width=3)
    d.rectangle((x0 + 8, y0 + 8, x1 - 8, y1 - 8), outline=DIM_GOLD, width=2)
    for cx, cy in ((x0 + 8, y0 + 8), (x1 - 8, y0 + 8), (x0 + 8, y1 - 8), (x1 - 8, y1 - 8)):
        d.polygon([(cx, cy - 7), (cx + 7, cy), (cx, cy + 7), (cx - 7, cy)], fill=frame)


def red_button(d, box, label, size=48):
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=(112, 26, 20))
    d.rectangle((x0, y0, x1, (y0 + y1) // 2), fill=(134, 36, 26))
    d.rectangle(box, outline=GOLD, width=3)
    d.rectangle((x0 + 7, y0 + 7, x1 - 7, y1 - 7), outline=VERMILION_LINE, width=1)
    d.text(((x0 + x1) / 2, (y0 + y1) / 2), label, font=BRUSH(size), fill=NAME, anchor='mm')


def dark_button(d, box, label, size=40):
    panel(d, box)
    d.text(((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), label, font=CINZEL(size), fill=TEXT, anchor='mm')


def step_button(d, box, sign, paper=False):
    x0, y0, x1, y1 = box
    cx, cy, a = (x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) * 0.22
    if paper:
        d.rectangle(box, fill=PAPER_HEAD, outline=(150, 120, 80), width=3)
        ink = INK
    else:
        d.rectangle(box, fill=ROW, outline=GOLD, width=3)
        ink = NAME
    d.rectangle((cx - a, cy - 4, cx + a, cy + 4), fill=ink)
    if sign == '+':
        d.rectangle((cx - 4, cy - a, cx + 4, cy + a), fill=ink)


def stepper(d, x, y, label, value, w, h=112, paper=False, above=False):
    """A label at the left (`above`: over the row, its buttons spread across `w`), [−] value [+] at the right."""
    ink = INK if paper else TEXT
    if above:
        d.text((x + w / 2, y - 14), label, font=GOTHIC(40), fill=ink, anchor='ms')
        step_button(d, (x, y, x + h, y + h), '−', paper)
        d.text((x + w / 2, y + h / 2), value, font=GOTHIC(48), fill=VERMILION_INK if paper else NAME, anchor='mm')
        step_button(d, (x + w - h, y, x + w, y + h), '+', paper)
        return
    d.text((x, y + h / 2), label, font=GOTHIC(44), fill=ink, anchor='lm')
    right = x + w
    step_button(d, (right - h, y, right, y + h), '+', paper)
    vw = 230
    d.text((right - h - vw / 2, y + h / 2), value, font=GOTHIC(48), fill=VERMILION_INK if paper else NAME, anchor='mm')
    step_button(d, (right - h - vw - h, y, right - h - vw, y + h), '−', paper)


def hatch(img, box, label=None, step=26):
    """A zone the system keeps free anyway (the camera's strip, the gesture zone): faint diagonal lines."""
    x0, y0, x1, y1 = [round(v) for v in box]
    if x1 <= x0 or y1 <= y0:
        return
    tile = Image.new('RGB', (x1 - x0, y1 - y0), (0, 0, 0))
    td = ImageDraw.Draw(tile)
    for k in range(-(y1 - y0), x1 - x0, step):
        td.line([(k, y1 - y0), (k + (y1 - y0), 0)], fill=(44, 44, 50), width=3)
    img.paste(tile, (x0, y0))
    if label:
        d = ImageDraw.Draw(img)
        d.text((x0 + (x1 - x0) * 0.72, (y0 + y1) / 2 + 20), label, font=GOTHIC(30), fill=(120, 120, 128), anchor='mm',
               stroke_width=6, stroke_fill=(0, 0, 0))


def edge_frame(d, box, dashed=False, colour=GOLD, width=5):
    x0, y0, x1, y1 = box
    if not dashed:
        d.rectangle(box, outline=colour, width=width)
        return
    for a, b in (((x0, y0), (x1, y0)), ((x1, y0), (x1, y1)), ((x0, y1), (x1, y1)), ((x0, y0), (x0, y1))):
        n = int(max(abs(b[0] - a[0]), abs(b[1] - a[1])) // 40) + 1
        for i in range(0, n, 2):
            t0, t1 = i / n, min(1, (i + 1) / n)
            d.line([(a[0] + (b[0] - a[0]) * t0, a[1] + (b[1] - a[1]) * t0),
                    (a[0] + (b[0] - a[0]) * t1, a[1] + (b[1] - a[1]) * t1)], fill=colour, width=width)


def corner_marks(d, box, arm=110, width=12):
    x0, y0, x1, y1 = box
    for cx, cy, sx, sy in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        d.line([(cx, cy + sy * arm), (cx, cy), (cx + sx * arm, cy)], fill=NAME, width=width, joint='curve')


def grip(d, x, y, way, reach=round(9 * PX_MM), thick=round(7 * PX_MM)):
    """A tab hanging inward from the edge at (x, y), `way` the unit vector into the screen: the finger takes it at its
    inner end, 9 mm in, out of Android's back gesture at the sides and its home gesture at the foot (the drag moves the
    edge by the finger's move, not to the finger)."""
    dx, dy = way
    if dx == 0:
        box = (x - thick / 2, min(y, y + dy * reach), x + thick / 2, max(y, y + dy * reach))
    else:
        box = (min(x, x + dx * reach), y - thick / 2, max(x, x + dx * reach), y + thick / 2)
    d.rounded_rectangle(box, radius=thick / 3, fill=(64, 50, 26), outline=GOLD, width=4)
    ex, ey = x + dx * reach * 0.62, y + dy * reach * 0.62          # the ridges near the inner end
    for k in (-26, 0, 26):
        if dx == 0:
            d.line([(ex - thick / 3, ey + k), (ex + thick / 3, ey + k)], fill=NAME, width=6)
        else:
            d.line([(ex + k, ey - thick / 3), (ex + k, ey + thick / 3)], fill=NAME, width=6)


def grips(d, box, upright):
    """A tab in each edge's middle; on the camera's edge (the top upright, the left landscape) at a third, off the hole."""
    x0, y0, x1, y1 = box
    grip(d, x0 + (x1 - x0) * (0.3 if upright else 0.5), y0, (0, 1))
    grip(d, (x0 + x1) / 2, y1, (0, -1))
    grip(d, x0, y0 + (y1 - y0) * (0.5 if upright else 0.3), (1, 0))
    grip(d, x1, (y0 + y1) / 2, (-1, 0))


def chrome(d, w, h):
    """The camera's hole: at the top upright, at the left landscape (Landscape 1)."""
    if w < h:
        d.ellipse((w / 2 - 24, 30, w / 2 + 24, 78), fill=(0, 0, 0), outline=(70, 70, 76), width=3)
    else:
        d.ellipse((30, h / 2 - 24, 78, h / 2 + 24), fill=(0, 0, 0), outline=(70, 70, 76), width=3)


def inside(img, margins):
    """The screen laid out inside the margins (approximate: the whole picture scaled into them and centred)."""
    w, h = img.size
    left, top, right, bottom = margins
    room = (w - left - right, h - top - bottom)
    k = min(room[0] / w, room[1] / h)
    small = img.resize((round(w * k), round(h * k)), Image.LANCZOS)
    out = Image.new('RGB', (w, h), (0, 0, 0))
    out.paste(small, (round(left + (room[0] - small.width) / 2), round(top + (room[1] - small.height) / 2)))
    return out


def margins_box(w, h, m=MARGIN):
    return (m, m, w - 1 - m, h - 1 - m)


# --- the simulated phone and the sheets ----------------------------------------------------------------------------

SHEETS = {}


def on_phone(img):
    """The screen as the simulated phone shows it: the corners rounded off, the film's opaque edge over it."""
    w, h = img.size
    screen = img.copy()
    chrome(ImageDraw.Draw(screen), w, h)
    ring = Image.new('L', (w, h), 0)
    rd = ImageDraw.Draw(ring)
    rd.rounded_rectangle((0, 0, w - 1, h - 1), radius=RADIUS, fill=255)
    rd.rounded_rectangle((FILM, FILM, w - 1 - FILM, h - 1 - FILM), radius=RADIUS - FILM, fill=0)
    screen.paste(Image.new('RGB', (w, h), FILM_FILL), (0, 0), ring)
    ImageDraw.Draw(screen).rounded_rectangle((FILM, FILM, w - 1 - FILM, h - 1 - FILM), radius=RADIUS - FILM,
                                             outline=(84, 84, 92), width=2)
    return screen


def save(img, name, title, group, notes):
    w, h = img.size
    B, head, side = 40, 170, 760
    W, H = w + 2 * B, h + 2 * B
    sheet = Image.new('RGB', (W + 80 + side, H + head + 50), (232, 228, 220))
    mask = Image.new('L', (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, W - 1, H - 1), radius=RADIUS + B, fill=255)
    sheet.paste(Image.new('RGB', (W, H), BODY), (40, head), mask)
    smask = Image.new('L', (w, h), 0)
    ImageDraw.Draw(smask).rounded_rectangle((0, 0, w - 1, h - 1), radius=RADIUS, fill=255)
    sheet.paste(on_phone(img), (40 + B, head + B), smask)
    sd = ImageDraw.Draw(sheet)
    sd.text((40, 40), f'{name} — {title}', font=GOTHIC(54), fill=(30, 30, 30))
    sd.text((40, 112), f'Poco X6 Pro {"upright" if w < h else "landscape"}, simulated: corners rounded at '
                       f'{mm(RADIUS)}, the film opaque {mm(FILM)} from the edge (made up); the pink is the notes',
            font=GOTHIC(30), fill=(110, 110, 110))
    y = head + 40
    for para in notes:
        sd.multiline_text((W + 80, y), para, font=GOTHIC(32), fill=(150, 30, 90), spacing=10)
        y = sd.multiline_textbbox((W + 80, y), para, font=GOTHIC(32), spacing=10)[3] + 40
    sheet.save(os.path.join(OUT, name + '.png'))
    SHEETS.setdefault(group, []).append(sheet)


def overviews():
    for group, sheets in SHEETS.items():
        thumbs = [s.resize((s.width * 2 // 5, s.height * 2 // 5), Image.LANCZOS) for s in sheets]
        cols = 2 if thumbs[0].width > thumbs[0].height else len(thumbs)
        rows = [thumbs[i:i + cols] for i in range(0, len(thumbs), cols)]
        w = max(sum(t.width for t in r) for r in rows) + 30 * (cols + 1)
        h = sum(max(t.height for t in r) for r in rows) + 30 * (len(rows) + 1)
        o = Image.new('RGB', (w, h), (232, 228, 220))
        y = 30
        for r in rows:
            x = 30
            for t in r:
                o.paste(t, (x, y))
                x += t.width + 30
            y += max(t.height for t in r) + 30
        o.save(os.path.join(OUT, f'overview-{group}.png'))


def note(d, xy, s, size=34, anchor='la'):
    d.multiline_text(xy, s, font=GOTHIC(size), fill=NOTE, anchor=anchor, stroke_width=5, stroke_fill=(0, 0, 0),
                     spacing=8)


# --- the pictures --------------------------------------------------------------------------------------------------

UP_SIZE, LAND_SIZE = (SHORT, LONG), (LONG, SHORT)
ALL = (MARGIN,) * 4
VALUE = mm(MARGIN)


def map_up():
    return dump(UP, 'map-markers', UP_SIZE)


def map_land():
    return dump(LAND, 'map-markers', LAND_SIZE)


def p():
    save(map_up(), 'P-up', 'today, upright', 'up', [
        'The map as it is laid out today\non a phone whose corners are\nrounded and whose film has an\nopaque edge.',
        'The status band\'s corners, the\nmap\'s sides and the bar\'s outer\ncells (Menu, Commands) are\npartly hidden.',
        'The system\'s safe area does\nnot report either: only the\ncamera\'s cutout.'])
    save(map_land(), 'P-land', 'today, landscape', 'land', [
        'The map held landscape today.',
        'The picture fills the height:\nits top and bottom rows are\nunder the film; Menu\'s and\nCommands\' outer corners too.',
        'The camera\'s strip (6.8 mm) on\nboth short sides already keeps\nthe sides\' controls off the\nedge there.'])


def r():
    save(inside(map_up(), ALL), 'R-up', 'with margins of 2.8 mm, upright', 'up', [
        f'The same screen with margins of\n{VALUE} on every edge: the\nfilm\'s edge alone needs 1.5 mm,\nthe rounded corners decide.',
        'Every screen lays itself out as\nif the phone\'s screen were that\nmuch smaller: everything a little\nsmaller (the unit follows the\nwidth upright), outside the\nmargins black.',
        'The camera\'s strip and the\ngesture zone stay where the\nsystem has them: a margin\nsmaller than them changes\nnothing there.',
        'Approximate: the dump scaled\ninto the margins.'])
    save(inside(map_land(), ALL), 'R-land', 'with margins of 2.8 mm, landscape', 'land', [
        f'Landscape with margins of\n{VALUE}: the picture shrinks\nto the height between the top\nand bottom margins (92%), the\nsides follow.',
        'The sides\' margin is within the\ncamera\'s strip here, so it moves\nnothing.',
        'Approximate: the dump scaled\ninto the margins.'])


def options_section_up(img, entry):
    """Option → Display upright with the margins' section under the orientation (A: the steppers; E: Adjust…)."""
    d = ImageDraw.Draw(img)
    top, x0, x1 = 1060, 52, 1150
    below = img.crop((0, 1110, SHORT, 2470))
    d.rectangle((26, top, 1162, 2472), fill=PAPER)
    y = top + 30
    d.text((x0, y), 'SCREEN MARGINS', font=CINZEL(30), fill=HEAD_INK, anchor='ls')
    d.text((x1, y), 'Upright', font=GOTHIC(30), fill=VERMILION_INK, anchor='rs')
    d.line([(x0, y + 14), (x1, y + 14)], fill=PAPER_RULE, width=2)
    y += 40
    if entry:
        d.rectangle((x0, y, x1, y + 210), fill=PAPER_ROW, outline=(192, 170, 132), width=3)
        d.text((x0 + 24, y + 56), 'Upright', font=GOTHIC(40), fill=INK, anchor='lm')
        d.text((x0 + 24, y + 104), f'{VALUE} all round', font=GOTHIC(30), fill=(110, 90, 66), anchor='lm')
        d.text((x0 + 24, y + 156), f'Landscape  {VALUE} all round', font=GOTHIC(30), fill=(110, 90, 66), anchor='lm')
        bx = (x1 - 300, y + 50, x1 - 24, y + 160)
        d.rectangle(bx, fill=PANEL, outline=GOLD, width=3)
        d.text(((bx[0] + bx[2]) / 2, (bx[1] + bx[3]) / 2), 'Adjust…', font=CINZEL(36), fill=TEXT, anchor='mm')
        y += 210 + 30
    else:
        d.text((x0, y + 20), 'For the way the phone is held now; turn it\nfor Landscape. The dashed line shows them.',
               font=GOTHIC(28), fill=(110, 90, 66), spacing=8)
        y += 110
        for label in ('Top', 'Bottom', 'Sides'):
            stepper(d, x0 + 10, y, label, VALUE, x1 - x0 - 10, h=104, paper=True)
            y += 104 + 22
        y += 10
    img.paste(below.crop((0, 0, SHORT, 2470 - y)), (0, y))
    return img


def options_section_land(img, entry):
    """Option → Display landscape: the margins' section at Thousands' place (the rows below move down; the tab scrolls)."""
    d = ImageDraw.Draw(img)
    y0, y1, x0, x1 = 430, 612, 150, 2560
    d.rectangle((x0 - 10, y0, x1 + 10, y1), fill=PAPER)
    y = y0 + 40
    d.text((x0 + 6, y), 'SCREEN MARGINS', font=CINZEL(34), fill=HEAD_INK, anchor='ls')
    d.text((x0 + 380, y), 'Landscape', font=GOTHIC(32), fill=VERMILION_INK, anchor='ls')
    d.line([(x0 + 6, y + 14), (x1, y + 14)], fill=PAPER_RULE, width=2)
    y += 36
    if entry:
        d.text((x0 + 6, y + 60), f'Landscape  {VALUE} all round      Upright  {VALUE} all round', font=GOTHIC(40),
               fill=INK, anchor='lm')
        bx = (x1 - 360, y + 12, x1, y + 104)
        d.rectangle(bx, fill=PANEL, outline=GOLD, width=3)
        d.text(((bx[0] + bx[2]) / 2, (bx[1] + bx[3]) / 2), 'Adjust…', font=CINZEL(38), fill=TEXT, anchor='mm')
    else:
        cw = (x1 - x0 - 120) / 3
        for i, label in enumerate(('Top', 'Bottom', 'Sides')):
            stepper(d, x0 + 6 + i * (cw + 60), y + 8, label, VALUE, cw, h=92, paper=True)
    return img


def a():
    img = options_section_up(dump(UP, 'options-display', UP_SIZE), entry=False)
    img = inside(img, ALL)
    d = ImageDraw.Draw(img)
    edge_frame(d, margins_box(*UP_SIZE), dashed=True, colour=(214, 70, 50), width=5)
    save(img, 'A-up', 'the steppers in Option → Display, upright', 'up', [
        'A: a section of the Display\ntab, under Screen orientation:\nTop, Bottom, Sides for the way\nthe phone is held now, − / + by\n0.1 mm (0-8 mm).',
        'While Display is open a dashed\nline runs round the whole screen\nat the margins; the sheet lays\nitself out again at each step.',
        'Landscape\'s are set by turning\nthe phone (or picking Landscape\nabove). Defaults resets them\nwith the tab.',
        'Simple to build; but the line is\nthin to judge by, and nothing\nelse of the game is in view.'])
    img = options_section_land(dump(LAND, 'options-display', LAND_SIZE), entry=False)
    img = inside(img, ALL)
    d = ImageDraw.Draw(img)
    edge_frame(d, margins_box(*LAND_SIZE), dashed=True, colour=(214, 70, 50), width=5)
    save(img, 'A-land', 'the steppers in Option → Display, landscape', 'land', [
        'A held landscape: the section\nin a row of three steppers;\nthe tab\'s rows below it move\ndown and the tab scrolls.',
        'The dashed line round the\nscreen at the margins.'])


def calibration(size, live=None):
    """B (live=None): the margins' own screen; C: the same over `live`, the screen laid out inside the margins."""
    w, h = size
    upright = w < h
    box = margins_box(w, h)
    if live is None:
        img = Image.new('RGB', size, (0, 0, 0))
        if upright:
            hatch(img, (0, 0, w, HOLE), 'camera')
            hatch(img, (0, h - GESTURE, w, h))
        else:
            hatch(img, (0, 0, HOLE, h), None)
            hatch(img, (w - HOLE, 0, w, h), None)
            hatch(img, (0, h - GESTURE, w, h))
    else:
        img = inside(live, ALL)
        img = Image.blend(img, Image.new('RGB', size, (0, 0, 0)), 0.55)
    d = ImageDraw.Draw(img)
    edge_frame(d, box)
    corner_marks(d, box)
    grips(d, box, upright)
    compact = live is not None
    if upright:
        pw = 800                                    # clear of the side tabs
        ph = 820 if compact else 960
        px0, py0 = (w - pw) / 2, (h - ph) / 2 - (0 if compact else 60)
        panel(d, (px0, py0, px0 + pw, py0 + ph))
        y = py0 + 70
        d.text((w / 2, y), 'SCREEN MARGINS', font=CINZEL(52), fill=NAME, anchor='mm')
        y += 70
        d.text((w / 2, y), 'Upright', font=GOTHIC(40), fill=QUIET, anchor='mm')
        y += 60
        if not compact:
            d.multiline_text((px0 + 50, y), 'Drag each gold edge in until\nit and its corners are wholly\n'
                             'in sight. Turn the phone to\nset Landscape.', font=GOTHIC(36), fill=TEXT, spacing=14)
            y += 260
        for label in ('Top', 'Bottom', 'Sides'):
            stepper(d, px0 + 50, y, label, VALUE, pw - 100, h=112)
            y += 112 + 26
        if compact:
            y += 60
        bar_y = py0 + ph - 150 if compact else h - MARGIN - round(9 * PX_MM) - 40 - 148
        if compact:
            dark_button(d, (px0 + 60, bar_y, px0 + 60 + 300, bar_y + 112), 'Reset', 36)
            red_button(d, (px0 + 60 + 320, bar_y, px0 + pw - 60, bar_y + 112), 'Done', 44)
        else:
            left, right = MARGIN + 16, w - MARGIN - 16
            dark_button(d, (left, bar_y, left + 380, bar_y + 148), 'Reset', 44)
            red_button(d, (left + 400, bar_y, right, bar_y + 148), 'Done', 56)
    else:
        pw = 1500 if compact else 1760
        ph = 640 if compact else 760
        px0, py0 = (w - pw) / 2, (h - ph) / 2
        panel(d, (px0, py0, px0 + pw, py0 + ph))
        y = py0 + 70
        d.text((px0 + 60, y), 'SCREEN MARGINS', font=CINZEL(52), fill=NAME, anchor='lm')
        d.text((px0 + pw - 60, y), 'Landscape', font=GOTHIC(40), fill=QUIET, anchor='rm')
        y += 70
        if not compact:
            d.multiline_text((px0 + 60, y), 'Drag each gold edge in until it and its corners are wholly in sight.\n'
                             'Turn the phone to set Upright. Sides within the camera\'s strip move nothing.',
                             font=GOTHIC(36), fill=TEXT, spacing=14)
            y += 190
        else:
            y += 60
        cw = (pw - 120 - 2 * 70) / 3
        for i, label in enumerate(('Top', 'Bottom', 'Sides')):
            stepper(d, px0 + 60 + i * (cw + 70), y, label, VALUE, cw, h=112, above=True)
        y += 112 + 60
        dark_button(d, (px0 + pw - 60 - 700 - 20, py0 + ph - 60 - 130, px0 + pw - 60 - 460 - 20, py0 + ph - 60),
                    'Reset', 40)
        red_button(d, (px0 + pw - 60 - 460, py0 + ph - 60 - 130, px0 + pw - 60, py0 + ph - 60), 'Done', 52)
    return img


def b():
    save(calibration(UP_SIZE), 'B-up', 'a screen of its own, upright', 'up', [
        'B: Option → Display holds one\nrow, the margins in words and\nAdjust… (E); Adjust… opens this\nscreen, black.',
        'The margins are a gold frame\nwith thick corner marks: the\nplayer drags a tab on an edge\n(both sides move together) or\nsteps it by 0.1 mm, until the\nframe and every corner mark\nare wholly in sight.',
        'The tabs hang 9 mm inward: the\nfinger takes them out of\nAndroid\'s back gesture at the\nsides and its home gesture at\nthe foot; the edge follows the\nfinger\'s move.',
        'Hatched: what the system keeps\nfree anyway (the camera\'s strip,\nthe gesture zone).',
        'Turning the phone shows the\nother set. Reset: all to 0.\nDone (or Back) keeps them.'])
    save(calibration(LAND_SIZE), 'B-land', 'a screen of its own, landscape', 'land', [
        'B held landscape: the steppers\nin a row, Reset | Done at the\nright thumb.',
        'The camera\'s strips on both\nsides hatched: a side margin\nwithin them moves nothing.'])


def c():
    save(calibration(UP_SIZE, live=map_up()), 'C-up', 'over the screen it came from, upright', 'up', [
        'C: B\'s frame and tabs over the\nscreen the player opened Option\nfrom, dimmed, laid out live\ninside the margins as they\nchange: the player sees the\nscreen\'s own buttons come into\nview.',
        'The panel smaller, in the middle.',
        'Opened from the title, the title\nis under it.'])
    save(calibration(LAND_SIZE, live=map_land()), 'C-land', 'over the screen it came from, landscape', 'land', [
        'C held landscape, over the map.'])


def e():
    img = inside(options_section_up(dump(UP, 'options-display', UP_SIZE), entry=True), ALL)
    save(img, 'E-up', 'Display\'s row for B and C, upright', 'up', [
        'E: the Display tab\'s row for B\nand C — both ways\' margins in\nwords, Adjust… opens the\nmargins\' screen.'])
    img = inside(options_section_land(dump(LAND, 'options-display', LAND_SIZE), entry=True), ALL)
    save(img, 'E-land', 'Display\'s row for B and C, landscape', 'land', [
        'E held landscape: the section at\nThousands\' place; the rows below\nmove down, the tab scrolls.'])


p()
r()
a()
e()
b()
c()
overviews()
