"""Mock-ups of the province menu with up to fifteen commands (owner, 2026-10-30; docs/ui/command-screen.md, "The province
menu"; docs/ui/touch.md, "The province menu within the thumbs' reach"). Leazas Castle (1)'s own-city menu
(Cｼﾊｲﾄｼ.ADV:39-290) can hold fifteen commands and Return at once: the PC's panel then runs past the screen's foot, the
tablet's rail past its top, and the phone's rows are squeezed to about 4 mm.

The dumps: tools/ui/gallery.ps1 -Touch -PhoneLand -Only map,province-menu-own-all (docs/ui/gallery.md). Then, from the
repository's root (Pillow; godot/fonts):
  python -I tools/ui/mock_province_long.py <gallery dir> <extract cg_png dir> <out dir> < /dev/null
Keep <out dir> outside the repository: the mock-ups hold the player's CGs (Leazas Castle's picture is CG 1001, Prov.BgCg).

Round 2 (round 1 was the rows' height, A0-D, 2026-10-25). Each variant at fifteen commands (the gallery's made-up
province-menu-own-all) and at a typical six (two commands and four places); a sheet per variant puts both side by side.
The PC: E two columns past nine, F the card beside one list past eleven, G the rows lower to fit, H the places under one
Visit row, I a scrolled list. The tablet: TL the rest on the left rail, TE a second column inward, TG the rows lower to
fit. The phone held landscape: PL the rest in the left side, PE two columns in the frame's right edge past seven, PS a
sheet across the frame. The sizes are ProvinceMenuView's (the PC's A1, the touch mode's B) and PhoneMap's (LM1c).
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

GALLERY, CGS, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2                                   # drawn at twice the dump's pixels, saved at the dump's
STRIP = 50                              # the status strip's foot on the PC and the tablet
TABLET_MM, PHONE_MM = 0.203, 1.5 / 17.6 # mm a pixel: the tablet's layout pixel, the phone dump's (2/3 of the reference's)
R = 2 / 3                               # the phone: the dump's pixels a reference pixel

# Lacquer (godot/Strategy/UiPalette.cs).
PANEL = (22, 16, 12)
GOLD, DIM_GOLD, HOVER_LINE = (196, 160, 86), (110, 86, 46), (222, 186, 104)
NAME, SUBTITLE, LABEL, VALUE = (232, 199, 122), (160, 140, 104), (150, 134, 108), (236, 226, 204)
ROW, ROW_HOVER = (34, 26, 20), (64, 48, 28)
TEXT, QUIET, OFF_TEXT = (244, 232, 206), (176, 160, 130), (98, 88, 74)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)

LEVY, BUILD, PLOT, VISIT = ('Levy', (150, 52, 36)), ('Build', (46, 62, 104)), ('Plot', (104, 70, 34)), ('Visit', (70, 96, 52))
PLACES = ['Tulip Research', 'Military Academy', 'Genius Hospital', 'Magic Research', 'Giant Library', 'S&M Tower',
          'Aoi Rehab', 'Dungeon', "Demon Lord's Room", 'Special Room', 'Lady Caravan']
ALL = [(LEVY, 'Temporary Levy'), (BUILD, 'Bolster Defense'), (PLOT, 'Dig Giant Hole'), (PLOT, 'Set Poison Food')] + \
      [(VISIT, p) for p in PLACES]
SIX = [(LEVY, 'Temporary Levy'), (BUILD, 'Bolster Defense')] + \
      [(VISIT, p) for p in ('Tulip Research', 'Military Academy', 'Genius Hospital', 'Dungeon')]
FIGURES = [('Economy', '20 (2,000K GOLD)'), ('Defense', '10 (Walls +20%)'), ('Battle Area', '180 troops')]


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size * S)))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)
CINZEL = lambda size, w=600: font('Cinzel-VariableFont_wght.ttf', size, w)


def load(*parts):
    return Image.open(os.path.join(GALLERY, *parts)).convert('RGB')


def castle():
    return Image.open(os.path.join(CGS, 'CG1001.png')).convert('RGB')


class Frame:
    def __init__(self, base):
        self.w, self.h = base.size
        self.img = base.resize((self.w * S, self.h * S), Image.LANCZOS).convert('RGBA')

    def draw(self):
        lay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay, blur=0):
        self.img = Image.alpha_composite(self.img, lay.filter(ImageFilter.GaussianBlur(blur * S)) if blur else lay)

    def paste(self, picture, r):
        x0, y0, x1, y1 = r
        self.img.paste(picture.resize((round((x1 - x0) * S), round((y1 - y0) * S)), Image.LANCZOS).convert('RGBA'),
                       (round(x0 * S), round(y0 * S)))

    def image(self):
        return self.img.resize((self.w, self.h), Image.LANCZOS).convert('RGB')


def sc(*v):
    return [round(a * S) for a in v]


def text(f, x, y, t, fnt, colour, anchor='lm'):
    lay, d = f.draw()
    d.text((x * S, y * S), t, font=fnt, fill=colour, anchor=anchor)
    f.add(lay)


def width(t, fnt):
    return fnt.getlength(t) / S


def fit(t, make, size, room, least=8):
    while size > least and width(t, make(size)) > room:
        size -= 0.5
    return size


def rect(f, r, fill=None, outline=None, w=1, alpha=255):
    lay, d = f.draw()
    d.rectangle(sc(*r), fill=fill + (alpha,) if fill else None, outline=outline + (255,) if outline else None,
                width=max(1, round(w * S)))
    f.add(lay)


def dim(f, r, alpha):
    rect(f, r, (0, 0, 0), alpha=alpha)


def shadow(f, r, spread=6):
    lay, d = f.draw()
    d.rectangle(sc(r[0] - 2, r[1] + 2, r[2] + 4, r[3] + 6), fill=(0, 0, 0, 110))
    f.add(lay, blur=spread * 0.6)


def panel(f, r, k=1.0):
    """UiPalette.DrawPanel: the shadow, the lacquer, the double gold frame and its corner studs."""
    shadow(f, r)
    rect(f, r, PANEL, alpha=245)
    rect(f, (r[0] + k, r[1] + k, r[2] - k, r[3] - k), outline=GOLD, w=2 * k)
    rect(f, (r[0] + 5.5 * k, r[1] + 5.5 * k, r[2] - 5.5 * k, r[3] - 5.5 * k), outline=DIM_GOLD, w=k)
    lay, d = f.draw()
    for cx, cy in ((r[0], r[1]), (r[2], r[1]), (r[0], r[3]), (r[2], r[3])):
        d.polygon(sc(cx, cy - 5 * k, cx + 5 * k, cy, cx, cy + 5 * k, cx - 5 * k, cy), fill=GOLD)
    f.add(lay)


def vrule(f, x, y0, y1):
    lay, d = f.draw()
    m = (y0 + y1) / 2
    d.line(sc(x, y0, x, m - 10), fill=DIM_GOLD + (255,), width=S)
    d.line(sc(x, m + 10, x, y1), fill=DIM_GOLD + (255,), width=S)
    d.polygon(sc(x, m - 4, x + 4, m, x, m + 4, x - 4, m), fill=GOLD + (255,))
    f.add(lay)


def row_box(f, r, hover=False, k=1.0):
    """UiPalette.DrawRowBox."""
    rect(f, r, ROW_HOVER if hover else ROW, outline=HOVER_LINE if hover else DIM_GOLD, w=(2 if hover else 1) * k)


def tag(f, r, word, colour, size):
    """UiPalette.DrawTag."""
    rect(f, r, colour, outline=GOLD)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2, word, GOTHIC(size), TEXT, 'mm')


def note(f, x, y, t, anchor='la', size=15):
    """A note on a dark plate, so it reads over the map."""
    lay, d = f.draw()
    fnt = GOTHIC(size)
    l, t0, r, b = d.textbbox((x * S, y * S), t, font=fnt, anchor=anchor)
    d.rectangle((l - 5 * S, t0 - 3 * S, r + 5 * S, b + 3 * S), fill=(0, 0, 0, 215))
    d.text((x * S, y * S), t, font=fnt, fill=NOTE, anchor=anchor)
    f.add(lay)


def notes(f, lines, x, y, size=14, step=21):
    for i, t in enumerate(lines):
        note(f, x, y + i * step, t, size=size + 2 if i == 0 else size)


def measure(f, x, y0, y1, label, left=False):
    lay, d = f.draw()
    for seg in ((x, y0, x, y1), (x - 4, y0, x + 4, y0), (x - 4, y1, x + 4, y1)):
        d.line(sc(*seg), fill=NOTE + (255,), width=2 * S)
    f.add(lay)
    note(f, x - 10 if left else x + 10, (y0 + y1) / 2, label, anchor='rm' if left else 'lm', size=13)


# ------------------------------------------------------------------------------------------------------- the PC
PC_ROW, PC_PITCH, PC_TEXT, PC_TAG, PC_TAG_TEXT, PC_TEXT_LEFT = 38, 45, 19, 74, 15, 93
PC_RETURN = (120, 36)


def pc_base():
    m = load('lacquer', 'current', 'map.png')
    m.paste(load('lacquer', 'current', 'province-menu-own-all.png').crop((0, 0, 1280, STRIP + 2)), (0, 0))
    return Frame(m)


def pc_header(f, x, y, beside=True):
    """The A1 header: the picture 234x156, the name, the army and the figures right of it (beside) or under it."""
    pic = (x + 18, y + 18, x + 18 + 234, y + 18 + 156)
    f.paste(castle(), pic)
    rect(f, (pic[0] - 1, pic[1] - 1, pic[2] + 1, pic[3] + 1), outline=DIM_GOLD)
    tx, ty = (pic[2] + 18, y + 16) if beside else (pic[0], pic[3] + 14)
    text(f, tx, ty, 'Leazas Castle', BRUSH(21), NAME, 'la')
    text(f, tx + 1, ty + 32, 'Leazas Army', CINZEL(14, 500), SUBTITLE, 'la')
    sy = ty + 58
    for label, value in FIGURES:
        text(f, tx, sy, label, GOTHIC(14), LABEL, 'la')
        text(f, tx, sy + 16, value, GOTHIC(17), VALUE, 'la')
        sy += 36


def pc_row(f, r, item, hover=False, size=PC_TEXT, tag_w=PC_TAG, tag_text=PC_TAG_TEXT, text_left=PC_TEXT_LEFT, inset=6,
           more=None):
    (word, colour), name = item
    row_box(f, r, hover)
    tag(f, (r[0] + inset, r[1] + inset, r[0] + inset + tag_w, r[3] - inset), word, colour, tag_text)
    text(f, r[0] + text_left, (r[1] + r[3]) / 2, name, GOTHIC(size), TEXT)
    if more:
        text(f, r[2] - 12, (r[1] + r[3]) / 2, more, GOTHIC(size - 2), NAME if hover else QUIET, 'rm')


def pc_return(f, r, size=17):
    row_box(f, r)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2, 'Return', GOTHIC(size), QUIET, 'mm')


def pc_columns(items, per):
    return [items[i:i + per] for i in range(0, len(items), per)]


def pc_a1(items, title=None):
    """Today's A1 (one column under the header), centred on the screen; what E, F, G and I keep up to their count."""
    f = pc_base()
    n = len(items)
    h = 192 + n * PC_PITCH + 1 + PC_RETURN[1] + 14
    w = 500
    x, y = round((1280 - w) / 2), round((800 - h) / 2)
    panel(f, (x, y, x + w, y + h))
    pc_header(f, x, y)
    for i, item in enumerate(items):
        ry = y + 192 + i * PC_PITCH
        pc_row(f, (x + 16, ry, x + w - 16, ry + PC_ROW), item)
    ry = y + 192 + n * PC_PITCH + 1
    pc_return(f, (x + w - 16 - PC_RETURN[0], ry, x + w - 16, ry + PC_RETURN[1]))
    return f, (x, y, x + w, y + h)


def pc_today_six():
    f, box = pc_a1(SIX)
    notes(f, ['Six commands: E, F, G and I as today (A1)',
              'two commands and four places, Leazas Castle (1) in a usual late game'], 16, 64)
    return f


def pc_today_fifteen():
    """Today at fifteen, for the sheets' reference: the panel runs past the screen's foot."""
    f, box = pc_a1(ALL)
    notes(f, ['Today at fifteen: 918 px high, Return below the screen'], 16, 64)
    return f


def pc_e(items):
    """E: past nine, two columns under the header — down the left column, then the right; Return under the right one."""
    n = len(items)
    if n <= 9:
        return None
    per = (n + 1) // 2
    col = 400
    w = 16 + col + 16 + col + 16
    h = 192 + per * PC_PITCH + 1 + PC_RETURN[1] + 14
    f = pc_base()
    x, y = round((1280 - w) / 2), round((800 - h) / 2)
    panel(f, (x, y, x + w, y + h))
    pc_header(f, x, y)
    for c, column in enumerate(pc_columns(items, per)):
        cx = x + 16 + c * (col + 16)
        for i, item in enumerate(column):
            ry = y + 192 + i * PC_PITCH
            pc_row(f, (cx, ry, cx + col, ry + PC_ROW), item, hover=(c == 1 and i == 4))
    ry = y + 192 + per * PC_PITCH + 1
    pc_return(f, (x + w - 16 - PC_RETURN[0], ry, x + w - 16, ry + PC_RETURN[1]))
    notes(f, ['E — two columns past nine: down the left column, then the right',
              f'the rows A1\'s (38 px, text 19); the panel {w} x {h}, centred; up to nine as today'], 16, y + h + 14)
    measure(f, x - 14, y, y + h, f'{h} px', left=True)
    return f


def pc_f(items):
    """F: past eleven, the card left of one list — the picture, the name, the army, the figures in a column."""
    n = len(items)
    if n <= 11:
        return None
    pitch = 44
    w = 270 + 468 + 16
    h = 18 + n * pitch + 2 + PC_RETURN[1] + 14
    f = pc_base()
    x, y = round((1280 - w) / 2), round(STRIP + (800 - STRIP - h) / 2)
    panel(f, (x, y, x + w, y + h))
    pc_header(f, x, y, beside=False)
    vrule(f, x + 262, y + 24, y + h - 24)
    for i, item in enumerate(items):
        ry = y + 18 + i * pitch
        pc_row(f, (x + 270, ry, x + 270 + 468, ry + PC_ROW), item, hover=(i == 9))
    ry = y + 18 + n * pitch + 2
    pc_return(f, (x + w - 16 - PC_RETURN[0], ry, x + w - 16, ry + PC_RETURN[1]))
    notes(f, ['F — the card beside one list past eleven', 'the rows A1\'s 38 px, 44 apart (45 today);',
              f'the panel {w} x {h} under the strip; up to eleven as today'], 16, 64)
    measure(f, x - 14, y, y + h, f'{h} px', left=True)
    return f


def pc_g(items):
    """G: today's panel, the rows lower as many as they must for the panel to fit under the strip."""
    n = len(items)
    if n <= 11:
        return None
    pitch, row, size, tag_w, tag_text, ret = 32, 27, 16, 62, 12, 28
    w = 500
    h = 192 + n * pitch + 1 + ret + 14
    f = pc_base()
    x, y = round((1280 - w) / 2), round(STRIP + (800 - STRIP - h) / 2)
    panel(f, (x, y, x + w, y + h))
    pc_header(f, x, y)
    for i, item in enumerate(items):
        ry = y + 192 + i * pitch
        pc_row(f, (x + 16, ry, x + w - 16, ry + row), item, hover=(i == 9), size=size, tag_w=tag_w, tag_text=tag_text,
               text_left=78, inset=4)
    ry = y + 192 + n * pitch + 1
    pc_return(f, (x + w - 16 - 100, ry, x + w - 16, ry + ret), size=15)
    notes(f, ['G — the rows lower to fit: 27 px, 32 apart, text 16 at fifteen',
              'round 1\'s A0 size, which the owner found low; up to eleven as today'], 16, 64)
    measure(f, x - 14, y, y + h, f'{h} px', left=True)
    return f


def pc_h(items):
    """H: the places under one Visit row; its click opens them in a second panel beside it, the answer still the place."""
    commands = [i for i in items if i[0] is not VISIT]
    places = [i for i in items if i[0] is VISIT]
    n = len(commands) + 1
    w, fw = 500, 330
    h = 192 + n * PC_PITCH + 1 + PC_RETURN[1] + 14
    fh = 16 + 30 + len(places) * PC_PITCH - 7 + 16
    f = pc_base()
    x = round((1280 - w - 10 - fw) / 2)
    y = round((800 - h) / 2)
    panel(f, (x, y, x + w, y + h))
    pc_header(f, x, y)
    rows = commands + [(VISIT, 'Visit a place')]
    for i, item in enumerate(rows):
        ry = y + 192 + i * PC_PITCH
        last = i == len(rows) - 1
        pc_row(f, (x + 16, ry, x + w - 16, ry + PC_ROW), item, hover=last, more=f'{len(places)} places ▶' if last else None)
    visit_y = y + 192 + (n - 1) * PC_PITCH
    ry = y + 192 + n * PC_PITCH + 1
    pc_return(f, (x + w - 16 - PC_RETURN[0], ry, x + w - 16, ry + PC_RETURN[1]))
    fx = x + w + 10
    fy = min(max(visit_y + PC_ROW / 2 - 16 - 30 - PC_ROW / 2, STRIP + 8), 800 - 8 - fh)
    panel(f, (fx, fy, fx + fw, fy + fh))
    text(f, fx + fw / 2, fy + 16 + 13, 'VISIT', CINZEL(14, 600), NAME, 'mm')
    for i, item in enumerate(places):
        ry = fy + 16 + 30 + i * PC_PITCH
        row_box(f, (fx + 14, ry, fx + fw - 14, ry + PC_ROW), hover=(i == 1))
        text(f, fx + 30, ry + PC_ROW / 2, item[1], GOTHIC(PC_TEXT), TEXT)
    lay, d = f.draw()                                                     # the open row's tie to its panel
    d.polygon(sc(x + w - 16, visit_y + 8, fx + 1, visit_y + 2, fx + 1, visit_y + PC_ROW - 2, x + w - 16, visit_y + PC_ROW - 8),
              fill=(64, 48, 28, 255))
    f.add(lay)
    notes(f, ['H — the places under one Visit row, opened beside it',
              f'{len(commands)} commands and Visit a place ({len(places)}); a place\'s click answers the script\'s menu',
              'two clicks for a visit; the rows A1\'s'], 16, 64)
    return f


def pc_i(items):
    """I: today's panel up to nine rows, the rest scrolled by the wheel; a thin bar and a fade say there are more."""
    n = len(items)
    if n <= 9:
        return None
    shown = 9
    w = 500
    h = 192 + shown * PC_PITCH + 1 + PC_RETURN[1] + 14
    f = pc_base()
    x, y = round((1280 - w) / 2), round((800 - h) / 2)
    panel(f, (x, y, x + w, y + h))
    pc_header(f, x, y)
    for i, item in enumerate(items[:shown]):
        ry = y + 192 + i * PC_PITCH
        pc_row(f, (x + 16, ry, x + w - 30, ry + PC_ROW), item)
    lay, d = f.draw()                                                     # the fade over the last row shown
    top = y + 192 + (shown - 1) * PC_PITCH
    for k in range(PC_ROW):
        d.line(sc(x + 10, top + k, x + w - 26, top + k), fill=PANEL + (round(200 * k / PC_ROW),), width=S)
    f.add(lay)
    track = (x + w - 22, y + 192, x + w - 16, y + 192 + shown * PC_PITCH - 7)
    rect(f, track, (40, 32, 24), outline=DIM_GOLD)
    th = (track[3] - track[1]) * shown / n
    rect(f, (track[0], track[1], track[2], track[1] + th), GOLD)
    ry = y + 192 + shown * PC_PITCH + 1
    text(f, x + 20, ry + PC_RETURN[1] / 2, f'▼ {n - shown + 1} more', GOTHIC(15), SUBTITLE)
    pc_return(f, (x + w - 16 - PC_RETURN[0], ry, x + w - 16, ry + PC_RETURN[1]))
    notes(f, ['I — a scrolled list: nine rows shown, the wheel for the rest',
              'the lower rows unseen until the list is scrolled'], 16, 64)
    return f


def pc_h6():
    return pc_h(SIX)


# ------------------------------------------------------------------------------------------------------- the tablet
CARD_CROP = (346, 200, 770, 604)        # the card, its triangle and the ring in the touch dump (Leazas Castle at 716, 402)
T_X, T_W, T_ROW_W, T_ROW_H, T_PITCH, T_FOOT, T_RETURN = 1032, 244, 220, 46, 54, 664, (726, 60)
REACH, GRIP_Y = 345, 640


def t_base():
    m = load('touch', 'current', 'map.png')
    dump = load('touch', 'current', 'province-menu-own-all.png')
    m.paste(dump.crop((0, 0, 1280, STRIP + 2)), (0, 0))
    m.paste(dump.crop(CARD_CROP), CARD_CROP[:2])
    return Frame(m)


def t_row(f, r, item, size):
    (word, colour), name = item
    shadow(f, r, 3)
    row_box(f, r)
    tag(f, (r[0] + 7, r[1] + 8, r[0] + 7 + 62, r[3] - 8), word, colour, 12)
    text(f, r[0] + 78, (r[1] + r[3]) / 2, name, GOTHIC(size), TEXT)


def t_return(f, r, size=18):
    shadow(f, r, 3)
    row_box(f, r)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2, 'Return', GOTHIC(size), QUIET, 'mm')


def t_size(items, room=T_ROW_W - 78 - 8, top=19):
    return min(fit(name, GOTHIC, top, room) for _, name in items)


def reach(f):
    lay, d = f.draw()
    for cx in (0, 1280):
        d.ellipse(sc(cx - REACH, GRIP_Y - REACH, cx + REACH, GRIP_Y + REACH), outline=NOTE + (170,), width=2 * S)
    f.add(lay)


def t_rail(f, items, x, foot, pitch=T_PITCH, row_h=T_ROW_H, ret=True, size=None):
    """A rail at x: the rows from foot up (index 0 lowest), Return at the foot when ret; its top."""
    n = len(items)
    top = (foot - (n - 1) * pitch if n else T_RETURN[0]) - 12
    panel(f, (x, top, x + T_W, 796))
    size = size or t_size(items)
    for i, item in enumerate(items):
        ry = foot - i * pitch
        t_row(f, (x + 12, ry, x + 12 + T_ROW_W, ry + row_h), item, size)
    if ret:
        t_return(f, (x + 12, T_RETURN[0], x + 12 + T_ROW_W, T_RETURN[0] + T_RETURN[1]))
    return top


def t_today(items, caption):
    f = t_base()
    t_rail(f, items, T_X, T_FOOT)
    reach(f)
    notes(f, caption, 16, 64)
    return f


def t_tl(items):
    """TL: past what the right rail holds (twelve), the first half on the right rail and the rest on the left one."""
    n = len(items)
    if n <= 12:
        return None
    first = (n + 1) // 2
    f = t_base()
    t_rail(f, items[:first], T_X, T_FOOT)
    t_rail(f, items[first:], 4, 732, ret=False, size=t_size(items))
    reach(f)
    notes(f, ['TL — the rest on the left rail: the first half right, the rest left',
              f'both from the foot up, as the command panel\'s; rows 220x46 ({46 * TABLET_MM:.1f} mm);',
              'past twelve (what the right rail holds); the card between the rails'], 260, 64)
    return f


def t_te(items):
    """TE: a second column inward on the right, the overflow from the foot up beside the first."""
    n = len(items)
    if n <= 12:
        return None
    first = (n + 1) // 2
    f = t_base()
    inner = T_X - T_W - 4
    top = T_FOOT - (first - 1) * T_PITCH - 12
    panel(f, (T_X, top, T_X + T_W, 796))
    panel(f, (inner, 732 - (n - first - 1) * T_PITCH - 12, inner + T_W, 796))
    size = t_size(items)
    for i, item in enumerate(items[:first]):
        ry = T_FOOT - i * T_PITCH
        t_row(f, (T_X + 12, ry, T_X + 12 + T_ROW_W, ry + T_ROW_H), item, size)
    t_return(f, (T_X + 12, T_RETURN[0], T_X + 12 + T_ROW_W, T_RETURN[0] + T_RETURN[1]))
    for i, item in enumerate(items[first:]):
        ry = 732 - i * T_PITCH
        t_row(f, (inner + 12, ry, inner + 12 + T_ROW_W, ry + T_ROW_H), item, size)
    reach(f)
    notes(f, ['TE — a second column inward: the first half at the edge, the rest beside it',
              f'both from the foot up; rows 220x46; the inner column {T_W * TABLET_MM:.0f} mm further in',
              'past twelve'], 16, 64)
    return f


def t_tg(items):
    """TG: one rail, its rows lower as many as they must for the rail to fit under the strip."""
    n = len(items)
    if n <= 12:
        return None
    row_h, pitch = 36, 44
    foot = T_RETURN[0] - 8 - row_h
    f = t_base()
    top = foot - (n - 1) * pitch - 12
    panel(f, (T_X, top, T_X + T_W, 796))
    size = t_size(items)
    for i, item in enumerate(items):
        ry = foot - i * pitch
        t_row(f, (T_X + 12, ry, T_X + 12 + T_ROW_W, ry + row_h), item, size)
    t_return(f, (T_X + 12, T_RETURN[0], T_X + 12 + T_ROW_W, T_RETURN[0] + T_RETURN[1]))
    reach(f)
    notes(f, ['TG — one rail, the rows lower to fit: 36 px, 44 apart',
              f'{36 * TABLET_MM:.1f} mm, the ledgers\' rows (the owner\'s E2); text {size:g};',
              'past twelve; the top rows past the thumb\'s reach'], 260, 64)
    return f


# ------------------------------------------------------------------------------------------------------- the phone
P_CARD_CROP = (540, 185, 1045, 628)     # the card, its triangle and the ring in the phone's dump (Leazas Castle)
P_RIGHT, P_LEFT, P_RAIL_W = 1561, 77, 170
P_ROW, P_GAP = 123 * R, 12 * R
P_FOOT_CELL = (773 - 123 * R, 773)      # Return, where Commands stands
P_FRAME = (254, 0, 1554, 813)


def p_base(status=True, card_shift=0):
    m = load('phone-landscape', 'current', 'map.png')
    dump = load('phone-landscape', 'current', 'province-menu-own-all.png')
    if card_shift:                                  # the card alone, moved left; its triangle and the ring left out
        m.paste(dump.crop((P_CARD_CROP[0], P_CARD_CROP[1], 922, P_CARD_CROP[3])), (P_CARD_CROP[0] - card_shift, P_CARD_CROP[1]))
    else:
        m.paste(dump.crop(P_CARD_CROP), P_CARD_CROP[:2])
    if status:
        m.paste(dump.crop((0, 0, 254, 300)), (0, 0))
    return Frame(m)


def p_row(f, r, item, top=36):
    """SideCells.DrawRow: the tag's band over the name, the name fitted to the row's width."""
    (word, colour), name = item
    row_box(f, r, k=R * 2)
    s = min(1, (r[3] - r[1]) / P_ROW)
    tsize = 26 * R * s
    tw = width(word, GOTHIC(tsize)) + 24 * R * s
    band = (r[0] + 14 * R * s, r[1] + 14 * R * s, r[0] + 14 * R * s + tw, r[1] + 14 * R * s + 36 * R * s)
    tag(f, band, word, colour, tsize)
    size = fit(name, GOTHIC, top * R * s, (r[2] - r[0]) - 30 * R, least=4)
    cy = r[3] - 34 * R * s
    text(f, r[0] + 16 * R, cy, name, GOTHIC(size), TEXT)
    return size / R


def p_return(f, r=None):
    """SideCells.DrawButton with the Close icon: Return at the right side's foot."""
    r = r or (P_RIGHT, P_FOOT_CELL[0], P_RIGHT + P_RAIL_W, P_FOOT_CELL[1])
    panel(f, r, k=R * 1.5)
    cx, cy = (r[0] + r[2]) / 2, r[1] + 32
    lay, d = f.draw()
    d.line(sc(cx - 14, cy - 14, cx + 14, cy + 14), fill=GOLD + (255,), width=4 * S)
    d.line(sc(cx - 14, cy + 14, cx + 14, cy - 14), fill=GOLD + (255,), width=4 * S)
    f.add(lay)
    text(f, cx, r[3] - 22, 'Return', BRUSH(24), NAME, 'mm')


def p_stack(f, items, x, foot, w=P_RAIL_W, row=P_ROW, gap=P_GAP, top_name=36):
    sizes = []
    for i, item in enumerate(items):
        y1 = foot - i * (row + gap)
        sizes.append(p_row(f, (x, y1 - row, x + w, y1), item, top=top_name))
    return sizes


def mm_text(ref):
    return f'{ref / 17.6:.1f} mm'


def mm_range(sizes):
    a, b = mm_text(min(sizes)), mm_text(max(sizes))
    return a if a == b else f'{a[:-3]}-{b}'


def p_today(items, caption, squeeze=False):
    f = p_base()
    n = len(items)
    foot = P_FOOT_CELL[0] - P_GAP
    row, gap = P_ROW, P_GAP
    room = foot - (16 * R)
    if n * row + (n - 1) * gap > room:
        k = room / (n * row + (n - 1) * gap)
        row, gap = row * k, gap * k
    sizes = p_stack(f, items, P_RIGHT, foot, row=row, gap=gap)
    p_return(f)
    notes(f, caption + [f'rows {mm_text(row / R)}, the names {mm_range(sizes)}'],
          270, 20, size=16, step=24)
    return f


def p_pl(items):
    """PL: the first seven in the right side over Return, the rest in the left side; the Menu cell and the status give way."""
    n = len(items)
    if n <= 7:
        return None
    f = p_base(status=False)
    right = 7
    sizes = p_stack(f, items[:right], P_RIGHT, P_FOOT_CELL[0] - P_GAP)
    sizes += p_stack(f, items[right:], P_LEFT, 773)
    p_return(f)
    notes(f, ['PL — the rest in the left side: seven right, eight left, from the foot up',
              f'rows 7.0 mm as the command panel\'s; the Menu cell and the status give way while it is open;',
              f'the names fit the side\'s 14.5 mm: {mm_text(min(sizes))} for the longest, {mm_text(max(sizes))} the shortest'],
          270, 20, size=16, step=24)
    return f


def p_pe(items):
    """PE: past seven, the commands in two columns in the frame's right edge at the phone's type; Return in the side."""
    n = len(items)
    if n <= 7:
        return None
    f = p_base(card_shift=60)
    per = (n + 1) // 2
    col = 300
    gap = 10
    x1 = P_FRAME[2] - 14
    x0 = x1 - 2 * col - gap - 28
    foot = 759
    top = foot - per * P_ROW - (per - 1) * P_GAP - 28
    panel(f, (x0, top, x1, foot + 14), k=R * 1.5)
    sizes = []
    for c, column in enumerate(pc_columns(items, per)):
        cx = x0 + 14 + c * (col + gap)
        for i, item in enumerate(column):
            ry = top + 14 + i * (P_ROW + P_GAP)
            sizes.append(p_row(f, (cx, ry, cx + col, ry + P_ROW), item, top=40))
    p_return(f)
    notes(f, ['PE — past seven, two columns in the frame\'s right edge, at the right thumb',
              f'down the left column, then the right; rows 7.0 mm, {col / R / 17.6:.1f} mm wide (the side\'s 14.5);',
              'Leazas Castle (1) lies under the panel here: its ring hidden, the card moved left;',
              f'the names {mm_range(sizes)}; up to seven as today'],
          270, 690, size=16, step=24)
    return f


def p_ps(items, n_label):
    """PS: a sheet across the frame — the card left, the commands in three columns right; Return in the side."""
    f = p_base()
    dim(f, P_FRAME, 150)
    n = len(items)
    per = (n + 2) // 3
    sheet_h = max(14 + per * P_ROW + (per - 1) * P_GAP + 14 + 28, 500)
    x0, x1 = P_FRAME[0] + 16, P_FRAME[2] - 16
    y0 = round((813 - sheet_h) / 2)
    panel(f, (x0, y0, x1, y0 + sheet_h), k=R * 1.5)
    pic = (x0 + 24, y0 + 24, x0 + 24 + 341, y0 + 24 + 225)
    f.paste(castle(), pic)
    rect(f, (pic[0] - 1, pic[1] - 1, pic[2] + 1, pic[3] + 1), outline=GOLD, w=2)
    ty = pic[3] + 14
    text(f, pic[0], ty + 18, 'Leazas Castle', BRUSH(fit('Leazas Castle', BRUSH, 52 * R, 341)), NAME)
    text(f, pic[0], ty + 46, 'LEAZAS ARMY', CINZEL(26 * R, 600), SUBTITLE)
    fy = ty + 78
    for label, value in FIGURES:
        text(f, pic[0], fy, label, GOTHIC(34 * R), LABEL)
        text(f, pic[2], fy, value, GOTHIC(34 * R), VALUE, 'rm')
        fy += 44 * R
    gx0 = pic[2] + 28
    col = (x1 - 18 - gx0 - 2 * 10) / 3
    vrule(f, pic[2] + 14, y0 + 24, y0 + sheet_h - 24)
    sizes = []
    for c, column in enumerate(pc_columns(items, per)):
        cx = gx0 + c * (col + 10)
        for i, item in enumerate(column):
            ry = y0 + 22 + i * (P_ROW + P_GAP)
            sizes.append(p_row(f, (cx, ry, cx + col, ry + P_ROW), item, top=40))
    p_return(f)
    notes(f, [f'PS — a sheet across the frame ({n_label})', 'the card left, the commands in three columns, down then across;',
              f'rows 7.0 mm, {col / R / 17.6:.1f} mm wide, the names {mm_range(sizes)}; the map dimmed'],
          270, 20, size=16, step=24)
    return f


# ------------------------------------------------------------------------------------------------------- the sheets
def sheet(name, left, right, left_label, right_label):
    """Two pictures side by side over a caption band: fifteen commands left, six right."""
    a, b = left.image(), right.image()
    band = 34
    out = Image.new('RGB', (a.width + 16 + b.width, band + max(a.height, b.height)), (12, 10, 9))
    out.paste(a, (0, band))
    out.paste(b, (a.width + 16, band))
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf'), 20)
    d.text((10, band / 2), left_label, font=f, fill=NOTE, anchor='lm')
    d.text((a.width + 26, band / 2), right_label, font=f, fill=NOTE, anchor='lm')
    out.save(os.path.join(OUT, name))
    a.save(os.path.join(OUT, name.replace('.png', '-15.png')))
    b.save(os.path.join(OUT, name.replace('.png', '-6.png')))


FIFTEEN, SIX_LABEL = 'Fifteen commands (made up: every command at once)', 'Six commands (a usual late castle)'

pc6 = pc_today_six()
sheet('pc-0-today.png', pc_today_fifteen(), pc6, 'Today at fifteen', 'Today at six (A1)')
sheet('pc-E-two-columns.png', pc_e(ALL), pc_today_six(), FIFTEEN, SIX_LABEL + ': as today')
sheet('pc-F-card-beside.png', pc_f(ALL), pc_today_six(), FIFTEEN, SIX_LABEL + ': as today')
sheet('pc-G-lower-rows.png', pc_g(ALL), pc_today_six(), FIFTEEN, SIX_LABEL + ': as today')
sheet('pc-H-visit-group.png', pc_h(ALL), pc_h6(), FIFTEEN, SIX_LABEL)
sheet('pc-I-scrolled.png', pc_i(ALL), pc_today_six(), FIFTEEN, SIX_LABEL + ': as today')

t6 = lambda: t_today(SIX, ['Six commands: TL, TE and TG as today (the owner\'s B)',
                           'the rail from the foot up, Return at the foot'])
sheet('tablet-TL-left-rail.png', t_tl(ALL), t6(), FIFTEEN, SIX_LABEL + ': as today')
sheet('tablet-TE-inner-column.png', t_te(ALL), t6(), FIFTEEN, SIX_LABEL + ': as today')
sheet('tablet-TG-lower-rows.png', t_tg(ALL), t6(), FIFTEEN, SIX_LABEL + ': as today')

p6 = lambda: p_today(SIX, ['Six commands: PL and PE as today (LM1c)'])
sheet('phone-0-today.png', p_today(ALL, ['Today at fifteen: the rows squeezed']), p6(), 'Today at fifteen', 'Today at six')
sheet('phone-PL-left-side.png', p_pl(ALL), p6(), FIFTEEN, SIX_LABEL + ': as today')
sheet('phone-PE-frame-columns.png', p_pe(ALL), p6(), FIFTEEN, SIX_LABEL + ': as today')
sheet('phone-PS-sheet.png', p_ps(ALL, 'fifteen'), p_ps(SIX, 'six'), FIFTEEN, SIX_LABEL)
print('written to', OUT)
