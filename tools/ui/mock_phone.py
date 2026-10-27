"""Mock-ups of the phone held upright (owner, 2026-10-27: an Android build for phones in both orientations, the Xiaomi
Poco X6 Pro the reference; docs/ui/phone.md, round P1), drawn at the phone's own pixels: 1220×2712, 446 ppi.

From the repository's root (Pillow and numpy; godot/fonts):
  python -I tools/ui/mock_phone.py <gallery dir> <look dir> <extract dir> <out dir> < /dev/null
- <gallery dir>: the screen gallery (`screen-gallery` at the root; tools/ui/gallery.ps1 -Touch and the Lacquer pass):
  touch/current (battle-turn, province-menu, scene-menu, command-menu-building) and lacquer/current (battle-turn).
- <look dir>: the key waits of the week-18 look in a window, our interface, without --touch (CLAUDE.md, "A look at one
  screen", `--until battle`): Ex's scene before the attack on Oaks (6) — 00058 (Ex's "I see...", our speech window at the
  top right), 00059 (the narration under it), 00060 (Ex's "Intercept them..."). Frames of another run may be numbered
  otherwise: the constants LOOK_* below.
- <extract dir>: KICHIKUOU_EXTRACT_DIR — scenario/SET_MAP.ADV (the provinces' names, first owners and map points; the
  dungeons' points) and cg_png (the map's six pictures 812/802/804 over 801/803/805, the flags 820-830).
Keep <out dir> outside the repository.

The map is drawn from the map's pictures and flags at the phone's scale; the owners at week 18 are read from the
gallery's touch `map` dump where it shows them (the camera at map x 1280-1920, y 208-608), elsewhere the game's first
owners (made up for the mock-up). The plates are ours, drawn larger.

Variants (docs/ui/phone.md): P0 today's layout turned upright; PS1-PS3 the scenes; PM1-PM2 the map; PB1-PB2 the
battle (P for portrait: touch.md's B1, M1… are the landscape phone's). Round P2: PB3 (PB1 with the owner's changes —
our army on top, Skip between the cards and the deck), PB3r the retreat question, PB3x an exchange; the battle's inputs
also touch/current's exchange-counters. Round P3: PD1-PD3 the deployment table, PU1-PU2 the Subordinates screen; their
inputs touch/current's deployment and the units' face CGs from <extract dir>/cg_png. Round P4: PA1-PA2 the
enemy attack (touch/current's enemy-attack-scouted, Maris's and Kanami's face CGs), PW1-PW2 the week report.
`--round P3` before the arguments draws only that round's variants (the look dir then unread, '-' will do).
The pink lines and text are the mock-ups' notes, not part of a screen.

For the screen gallery's phone tab (tools/ui/gallery.ps1 -Phone; docs/ui/gallery.md, §9) only the owner's chosen base
versions, without the notes, into <gallery dir>/phone/current with a manifest.json as the game's passes write it:
  python -I tools/ui/mock_phone.py --gallery <gallery dir> <extract dir> < /dev/null
"""
import sys, os, re, json, hashlib, datetime
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ARGS = sys.argv[1:]
ROUND = None
if ARGS[0] == '--round':                # one round's variants alone (--round P3); the look dir is not read
    ROUND, ARGS = ARGS[1].upper(), ARGS[2:]
GALLERY_MODE = ARGS[0] == '--gallery'
if GALLERY_MODE:
    GALLERY, EXTRACT = ARGS[1:3]
    LOOK, OUT = None, os.path.join(GALLERY, 'phone', 'current')
else:
    GALLERY, LOOK, EXTRACT, OUT = ARGS[0:4]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
TOUCH, LACQ = os.path.join(GALLERY, 'touch', 'current'), os.path.join(GALLERY, 'lacquer', 'current')
os.makedirs(OUT, exist_ok=True)
LOOK_SPEECH, LOOK_NARRATION, LOOK_INTERCEPT = '00058-message-speech-left.png', '00059-key.png', '00060-key.png'

PW, PH = 1220, 2712                     # the Poco X6 Pro's screen upright: 69.5 × 154.5 mm
PX_MM = 446 / 25.4                      # 17.6 phone pixels a millimetre
TOP = 110                               # the camera's hole and the status bar's room: nothing to read or tap above
BAR = (2504, 2652)                      # the bottom bar; the gesture zone under it
K = PW / 1280                           # a 1280-wide dump at the phone's width

# Lacquer (godot/Strategy/UiPalette.cs).
PANEL, BACKDROP = (22, 16, 12), (14, 10, 8)
GOLD, DIM_GOLD, HOVER_LINE = (196, 160, 86), (110, 86, 46), (222, 186, 104)
NAME, SUBTITLE, LABEL = (232, 199, 122), (160, 140, 104), (150, 134, 108)
TEXT, QUIET, OFF_TEXT = (244, 232, 206), (176, 160, 130), (98, 88, 74)
ROW, OFF_TAG = (34, 26, 20), (52, 46, 40)
THOUGHT_FILL, NARRATION_FILL, NARRATION_TEXT = (30, 26, 30), (52, 38, 24), (226, 204, 150)
VERMILION, VERMILION_LINE = (170, 40, 28), (214, 150, 90)
BAR_GOOD, BAR_BAD, PLAYER_BLUE, ENEMY_RED = (70, 150, 70), (170, 50, 36), (60, 110, 200), (200, 60, 50)
NOTE = (255, 92, 160)                   # the mock-ups' own notes


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size)))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)
CINZEL = lambda size, w=600: font('Cinzel-VariableFont_wght.ttf', size, w)


def mm(px):
    v = px / PX_MM
    return f'{v:.2f} mm' if v < 1 else f'{v:.1f} mm'


def fit(d, s, face, size, width):
    while size > 10 and d.textlength(s, font=face(size)) > width:
        size -= 1
    return face(size)


def note(d, xy, s, size=34, anchor='la'):
    if GALLERY_MODE:
        return
    d.multiline_text(xy, s, font=GOTHIC(size), fill=NOTE, anchor=anchor, stroke_width=5, stroke_fill=(0, 0, 0),
                     spacing=8)


def arrow(d, a, b):
    if GALLERY_MODE:
        return
    d.line([a, b], fill=NOTE, width=4)
    d.ellipse((b[0] - 8, b[1] - 8, b[0] + 8, b[1] + 8), fill=NOTE)


# --- the house's parts ---------------------------------------------------------------------------------------------

def panel(d, box, fill=PANEL, frame=GOLD):
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=fill)
    d.rectangle(box, outline=frame, width=3)
    d.rectangle((x0 + 8, y0 + 8, x1 - 8, y1 - 8), outline=DIM_GOLD, width=2)
    for cx, cy in ((x0 + 8, y0 + 8), (x1 - 8, y0 + 8), (x0 + 8, y1 - 8), (x1 - 8, y1 - 8)):
        d.polygon([(cx, cy - 7), (cx + 7, cy), (cx, cy + 7), (cx - 7, cy)], fill=frame)


def row_box(d, box, label, size=40, face=GOTHIC, off=False, centred=False, tag=None):
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=ROW, outline=OFF_TAG if off else DIM_GOLD, width=3)
    tx = x0 + 24
    if tag:
        word, colour = tag
        tw = 150
        d.rectangle((x0 + 14, y0 + 16, x0 + 14 + tw, y1 - 16), fill=OFF_TAG if off else colour)
        d.text((x0 + 14 + tw / 2, (y0 + y1) / 2), word, font=GOTHIC(30), fill=OFF_TEXT if off else TEXT, anchor='mm')
        tx = x0 + 14 + tw + 22
    f = fit(d, label, face, size, x1 - tx - 16)
    if centred:
        d.text(((x0 + x1) / 2, (y0 + y1) / 2), label, font=f, fill=OFF_TEXT if off else TEXT, anchor='mm')
    else:
        d.text((tx, (y0 + y1) / 2), label, font=f, fill=OFF_TEXT if off else TEXT, anchor='lm')


def red_button(d, box, label, size=44):
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=(112, 26, 20))
    d.rectangle((x0, y0, x1, (y0 + y1) // 2), fill=(134, 36, 26))
    d.rectangle(box, outline=GOLD, width=3)
    d.rectangle((x0 + 7, y0 + 7, x1 - 7, y1 - 7), outline=VERMILION_LINE, width=1)
    d.text(((x0 + x1) / 2, (y0 + y1) / 2), label, font=fit(d, label, BRUSH, size, x1 - x0 - 30), fill=NAME,
           anchor='mm')


def gold_button(d, box, label, forecast=None):
    x0, y0, x1, y1 = box
    strip = 70 if forecast else 0
    for y in range(y0, y1 - strip):
        t = (y - y0) / max(1, y1 - strip - y0)
        d.line([(x0, y), (x1, y)], fill=tuple(round(a + (b - a) * t) for a, b in zip((222, 184, 96), (170, 128, 52))))
    d.rectangle(box, outline=(250, 222, 150), width=3)
    d.text(((x0 + x1) / 2, (y0 + y1 - strip) / 2), label, font=BRUSH(64), fill=(44, 26, 10), anchor='mm')
    if forecast:
        d.rectangle((x0 + 3, y1 - strip, x1 - 3, y1 - 3), fill=(30, 22, 16))
        troops, hp = forecast
        d.text(((x0 + x1) / 2 - 20, y1 - strip / 2), troops, font=GOTHIC(36), fill=(150, 196, 110), anchor='rm')
        d.text(((x0 + x1) / 2 + 20, y1 - strip / 2), hp, font=GOTHIC(36), fill=(222, 110, 90), anchor='lm')


def triangle(d, x, y, size=26, fill=GOLD):
    d.polygon([(x - size, y - size * 0.6), (x + size, y - size * 0.6), (x, y + size * 0.6)], fill=fill)


def backdrop(img, box):
    """The wave backdrop (G1/G3): a faint gold seigaiha on lacquer."""
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    layer = Image.new('RGB', (w, h), BACKDROP)
    dd = ImageDraw.Draw(layer)
    r = 54
    for row, y in enumerate(range(-r, h + r, r // 2)):
        off = (row % 2) * r
        for x in range(-2 * r, w + 2 * r, 2 * r):
            cx = x + off
            dd.pieslice((cx - r, y - r, cx + r, y + r), 180, 360, fill=BACKDROP)
            for k in (r, r - 13, r - 26, r - 39):
                dd.arc((cx - k, y - k, cx + k, y + k), 180, 360, fill=(38, 29, 19), width=2)
    img.paste(layer, (x0, y0))


def icon(d, kind, cx, cy, colour=GOLD):
    if kind == 'menu':
        for dy in (-16, 0, 16):
            d.rectangle((cx - 26, cy + dy - 4, cx + 26, cy + dy + 4), fill=colour)
    elif kind == 'log':
        for dy, w in ((-16, 26), (0, 26), (16, 14)):
            d.rectangle((cx - 26, cy + dy - 4, cx - 26 + 2 * w, cy + dy + 4), fill=colour)
    elif kind == 'skip':
        for dx in (-22, 4):
            d.polygon([(cx + dx, cy - 20), (cx + dx + 26, cy), (cx + dx, cy + 20)], fill=colour)
    elif kind == 'close':
        d.line([(cx - 20, cy - 20), (cx + 20, cy + 20)], fill=colour, width=8)
        d.line([(cx - 20, cy + 20), (cx + 20, cy - 20)], fill=colour, width=8)


def bottom_bar(d, right=None, cells=('menu', 'log', 'skip'), skip_on=False, log_on=True):
    """The bottom bar: Menu (the menu bar's commands), Log, Skip at the left, the screen's main button at the right."""
    y0, y1 = BAR
    x = 16
    for kind in cells:
        w = 170
        lit = kind == 'skip' and skip_on
        panel(d, (x, y0, x + w, y1), fill=(96, 72, 30) if lit else PANEL)
        off = kind == 'log' and not log_on
        icon(d, kind, x + w // 2, y0 + 58, OFF_TEXT if off else (NAME if lit else GOLD))
        label = {'menu': 'Menu', 'log': 'Log', 'skip': 'Skip: ON' if lit else 'Skip'}[kind]
        d.text((x + w // 2, y0 + 116), label, font=GOTHIC(28), fill=OFF_TEXT if off else TEXT, anchor='mm')
        x += w + 14
    if right:
        label, kind = right
        x0 = PW - 16 - 440
        panel(d, (x0, y0, PW - 16, y1))
        icon(d, kind, x0 + 70, (y0 + y1) // 2)
        d.text((x0 + 250, (y0 + y1) // 2), label, font=BRUSH(48), fill=NAME, anchor='mm')


def chrome(d):
    """The camera's hole; the gesture zone's handle (hidden in full screen, its swipe still there)."""
    d.ellipse((PW / 2 - 24, 30, PW / 2 + 24, 78), fill=(0, 0, 0), outline=(70, 70, 76), width=3)
    d.rounded_rectangle((PW / 2 - 130, 2682, PW / 2 + 130, 2692), radius=5, fill=(120, 120, 124))


def reach(img, label=True, shade=True):
    """The right thumb's comfortable reach, one hand (~70 mm round a grip low on the right side)."""
    if GALLERY_MODE:
        return
    over = Image.new('RGBA', img.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(over)
    cx, cy, r = PW + 30, PH - 560, round(70 * PX_MM)
    if shade:
        od.pieslice((cx - r, cy - r, cx + r, cy + r), 155, 270, fill=(255, 92, 160, 24))
    od.arc((cx - r, cy - r, cx + r, cy + r), 155, 270, fill=(255, 92, 160, 220), width=5)
    img.paste(Image.alpha_composite(img.convert('RGBA'), over).convert('RGB'))
    if label:
        note(ImageDraw.Draw(img), (PW - 30, cy - r - 70), 'right thumb, one hand: ~70 mm', 30, 'ra')


def phone():
    img = Image.new('RGB', (PW, PH), (0, 0, 0))
    return img, ImageDraw.Draw(img)


SHEETS = {}
# The gallery's phone tab: the owner's chosen base versions (docs/ui/phone.md), by mock-up name.
GALLERY_ENTRIES = {
    'PM1a': ('pm1-map', 'Map — PM1', 'The map wait', 'the map\'s whole height, the status on top, Commands at the thumb'),
    'PM1b': ('pm1-commands', 'Map — PM1', 'The command panel', 'a sheet over the map\'s foot, Close where Commands was'),
    'PM1c': ('pm1-province', 'Map — PM1', 'A province', 'Oaks (6) ringed, its card and commands as a sheet; the map '
                                                          'scrolled up'),
    'PB3': ('pb3-turn', 'Battle — PB3', 'The player\'s turn', 'our army on top, the enemy\'s at the thumb, Skip over '
                                                               'the deck'),
    'PB3r': ('pb3-retreat', 'Battle — PB3', 'The retreat question', 'at Retreat, over the deck\'s right column'),
    'PB3x': ('pb3-exchange', 'Battle — PB3', 'An exchange', 'Ex (commander 1) on Rance (unit 1); the deck empty'),
}
DUMPED = []


def save_gallery(img, name):
    """The phone's screen in its body, as the gallery's entry; compared with the former picture as the game's passes do
    (GalleryScreen.Compare: a channel more than 8 apart, changed past 600 pixels)."""
    if name not in GALLERY_ENTRIES:
        return
    ident, section, title, note_ = GALLERY_ENTRIES[name]
    B = 34
    W, H = PW + 2 * B, PH + 2 * B
    body = Image.new('RGB', (W, H), (20, 17, 16))
    mask = Image.new('L', (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, W - 1, H - 1), radius=130, fill=255)
    body.paste(Image.new('RGB', (W, H), (28, 28, 32)), (0, 0), mask)
    smask = Image.new('L', (PW, PH), 0)
    ImageDraw.Draw(smask).rounded_rectangle((0, 0, PW - 1, PH - 1), radius=100, fill=255)
    body.paste(img, (B, B), smask)
    file = ident + '.png'
    body.save(os.path.join(OUT, file))
    since, differing = None, 0
    previous = os.path.join(GALLERY, 'phone', 'previous')
    if os.path.isdir(previous):
        before = os.path.join(previous, file)
        if not os.path.exists(before):
            since = 'new'
        else:
            a = np.asarray(Image.open(before).convert('RGB'), dtype=np.int16)
            b = np.asarray(body, dtype=np.int16)
            differing = a.shape[0] * a.shape[1] if a.shape != b.shape else int((np.abs(a - b) > 8).any(axis=2).sum())
            since = 'changed' if differing > 600 else 'same'
    with open(os.path.join(OUT, file), 'rb') as f:
        digest = hashlib.sha256(f.read()).hexdigest()
    DUMPED.append({'id': ident, 'section': section, 'title': title, 'save': 'mock-up', 'note': note_, 'file': file,
                   'sha256': digest, 'error': None, 'since': since, 'differing': differing, 'box': None})


def save(img, name, title, group, side=None):
    """`side`: the notes in a column right of the phone, for screens too full to bear them (round P3 on)."""
    chrome(ImageDraw.Draw(img))
    if GALLERY_MODE:
        save_gallery(img, name)
        return
    B, head = 34, 170
    W, H = PW + 2 * B, PH + 2 * B
    sheet = Image.new('RGB', (W + 80 + (640 if side else 0), H + head + 50), (232, 228, 220))
    if side:
        sd = ImageDraw.Draw(sheet)
        y = head + 60
        for para in side:
            sd.multiline_text((W + 80, y), para, font=GOTHIC(30), fill=(150, 30, 90), spacing=10)
            y += sd.multiline_textbbox((W + 80, y), para, font=GOTHIC(30), spacing=10)[3] - y + 44
    mask = Image.new('L', (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, W - 1, H - 1), radius=130, fill=255)
    sheet.paste(Image.new('RGB', (W, H), (28, 28, 32)), (40, head), mask)
    smask = Image.new('L', (PW, PH), 0)
    ImageDraw.Draw(smask).rounded_rectangle((0, 0, PW - 1, PH - 1), radius=100, fill=255)
    sheet.paste(img, (40 + B, head + B), smask)
    sd = ImageDraw.Draw(sheet)
    sd.text((40, 40), f'{name} — {title}', font=GOTHIC(54), fill=(30, 30, 30))
    sd.text((40, 112), 'Poco X6 Pro upright, 1220×2712 at 446 ppi (69.5 × 154.5 mm); the pink is the mock-up\'s notes',
            font=GOTHIC(30), fill=(110, 110, 110))
    sheet.save(os.path.join(OUT, name + '.png'))
    SHEETS.setdefault(group, []).append((name, title, sheet))


def overviews():
    for group, sheets in SHEETS.items():
        thumbs = [s.resize((s.width * 2 // 5, s.height * 2 // 5), Image.LANCZOS) for _, _, s in sheets]
        w = sum(t.width for t in thumbs) + 30 * (len(thumbs) + 1)
        h = max(t.height for t in thumbs) + 60
        o = Image.new('RGB', (w, h), (232, 228, 220))
        x = 30
        for t in thumbs:
            o.paste(t, (x, 30))
            x += t.width + 30
        o.save(os.path.join(OUT, f'overview-{group}.png'))


# --- the pictures --------------------------------------------------------------------------------------------------

def dump(folder, name):
    return Image.open(os.path.join(folder, name)).convert('RGB')


def look(name):
    return Image.open(os.path.join(LOOK, name)).convert('RGB').crop((0, 24, 1280, 824))   # the menu bar off


def at_width(im, width=PW):
    return im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)


def scene_clean():
    """Ex's scene with no window of ours: the top of 00059 (its narration at the foot) over the foot of 00058."""
    pic = look(LOOK_NARRATION)
    pic.paste(look(LOOK_SPEECH).crop((0, 300, 1280, 800)), (0, 300))
    return at_width(pic)


# --- the map -------------------------------------------------------------------------------------------------------

def read_map():
    lines = open(os.path.join(EXTRACT, 'scenario', 'SET_MAP.ADV'), encoding='utf-8', errors='replace').read().splitlines()
    names, owners, cur, coords = {}, {}, None, []
    for i, l in enumerate(lines):
        if m := re.match(r'\s*!VAR0158 : (\d+)!', l):
            cur = int(m.group(1))
        elif (m := re.match(r'\s*MS 8, "(.*)":', l)) and cur:
            names[cur] = m.group(1)
        elif (m := re.match(r'\s*!VAR0130 : (\d+)!', l)) and cur and cur not in owners:
            owners[cur] = int(m.group(1))
        elif l.strip().startswith('*L_03bf6'):
            coords = [int(v) for v in re.findall(r'-?\d+', lines[i + 1])]
    points = {p: (coords[2 * p - 2], coords[2 * p - 1]) for p in names if 2 * p <= len(coords)}
    return names, owners, points


def flag(owner):
    fl = Image.open(os.path.join(EXTRACT, 'cg_png', f'CG{owner + 819:04d}.png'))
    return fl.convert('RGB'), Image.fromarray((np.array(fl) != 40).astype(np.uint8) * 255)   # index 40 the key


def build_map():
    names, owners, points = read_map()
    full = Image.new('RGB', (1920, 800))
    for r, row in enumerate([[812, 802, 804], [801, 803, 805]]):
        for c, n in enumerate(row):
            full.paste(Image.open(os.path.join(EXTRACT, 'cg_png', f'CG{n:04d}.png')).convert('RGB'), (c * 640, r * 400))
    # Week 18's owners where the gallery's dump shows them (camera at 1280, 208; the strip covers its first 25 rows).
    seen = np.asarray(dump(TOUCH, 'map.png').resize((640, 400), Image.NEAREST), dtype=np.int16)
    flags = {o: flag(o) for o in range(1, 12)}
    for p, (x, y) in points.items():
        vx, vy = x - 1280, y - 18 - 208
        if 0 <= vx and vx + 24 <= 640 and 26 <= vy and vy + 19 <= 400:
            for o, (fl, m) in flags.items():
                a, k = np.asarray(fl, dtype=np.int16), np.asarray(m) > 0
                if np.abs(seen[vy:vy + 19, vx:vx + 24][k] - a[k]).max() == 0:
                    owners[p] = o
                    break
    for p, (x, y) in points.items():
        if owners.get(p):
            fl, m = flags[owners[p]]
            full.paste(fl, (x, y - 18), m)
    return full, names, points


MAP, PROV_NAMES, PROV_POINTS = None, None, None
# The dungeons the dump shows (SET_MAP.ADV :2177-2260; docs/strategy/survey-dungeon.md §2.1), lit or dimmed.
DUNGEONS = [("Monsters' Cave", (1461, 461), True), ("Thieves' Cave", (1601, 438), True),
            ("Summit's Mouth", (1423, 295), False), ('Titi Lake', (1396, 553), False)]


def draw_map(img, box, x0, y0, scale, plate_size, ring=None, dim=0):
    """The map's view from map point (x0, y0) at `scale` phone pixels a map pixel into `box`, our plates over it."""
    bx0, by0, bx1, by1 = box
    w, h = (bx1 - bx0) / scale, (by1 - by0) / scale
    part = MAP.crop((x0, y0, round(x0 + w), round(y0 + h)))
    part = part.resize((part.width * 2, part.height * 2), Image.NEAREST).resize((bx1 - bx0, by1 - by0), Image.LANCZOS)
    img.paste(part, (bx0, by0))
    over = Image.new('RGBA', img.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(over)
    f = GOTHIC(plate_size)

    def plate(name, mx, my, dy, lit):
        px, py = bx0 + (mx - x0) * scale, by0 + (my - y0 + dy) * scale
        tw = od.textlength(name, font=f)
        pw, ph = tw + plate_size * 0.9, plate_size * 1.45
        if px + pw / 2 < bx0 or px - pw / 2 > bx1 or py - ph / 2 < by0 or py + ph / 2 > by1:
            return
        box_ = (px - pw / 2, py - ph / 2, px + pw / 2, py + ph / 2)
        od.rounded_rectangle(box_, radius=ph * 0.22, fill=(40, 42, 48, 235) if lit else (70, 70, 74, 150),
                             outline=(126, 130, 138, 255) if lit else (110, 110, 114, 150), width=2)
        od.text((px, py), name, font=f, fill=(240, 240, 240, 255) if lit else (170, 170, 170, 200), anchor='mm')

    for p, (mx, my) in sorted(PROV_POINTS.items(), key=lambda kv: kv[1][1]):
        if PROV_NAMES.get(p):
            plate(PROV_NAMES[p], mx, my, 28, True)
    for name, (mx, my), lit in DUNGEONS:
        plate(name, mx, my, 13, lit)
    if dim:
        od.rectangle(box, fill=(0, 0, 0, dim))
    img.paste(Image.alpha_composite(img.convert('RGBA'), over).convert('RGB'))
    if ring:
        mx, my = PROV_POINTS[ring]
        px, py = bx0 + (mx - x0) * scale, by0 + (my - y0) * scale
        d = ImageDraw.Draw(img)
        r = 74
        d.ellipse((px - r, py - r + 8, px + r, py + r + 8), outline=GOLD, width=6)
        d.ellipse((px - r - 8, py - r, px + r + 8, py + r + 16), outline=(250, 222, 150), width=2)


def point_on(box, x0, y0, scale, p):
    mx, my = PROV_POINTS[p]
    return box[0] + (mx - x0) * scale, box[1] + (my - y0) * scale


# --- P0: today's layout turned upright -----------------------------------------------------------------------------

def p0():
    img, d = phone()
    y = (PH - 762) // 2
    img.paste(at_width(dump(TOUCH, 'battle-turn.png')), (0, y))
    reach(img)
    d = ImageDraw.Draw(img)
    note(d, (40, y - 250), f'The whole 1280×800 layout at the phone\'s width:\n69.5 × 43.4 mm, one layout pixel {mm(K)}')
    note(d, (40, y + 790), f'A card\'s text (17 px): {mm(17 * K)} high\n'
                           f'A rail button (116×54): {mm(116 * K)} × {mm(54 * K)}\n'
                           f'A finger needs 7-9 mm; Android\'s smallest text 12 sp ≈ 1.9 mm\n'
                           f'The scenes\' own text (48 px em): {mm(48 * K)} — readable')
    arrow(d, (500, y + 800), (560, y + 520))
    note(d, (40, 200), f'{mm(y - TOP)} unused above the picture …')
    note(d, (40, PH - 200), f'… and {mm(PH - y - 762 - 60)} under it')
    note(d, (PW / 2 + 50, 40), 'camera')
    note(d, (PW / 2 + 150, PH - 60), 'gesture zone', 28)
    save(img, 'P0', 'today\'s touch layout, turned upright', 'frame')


# --- the scenes ----------------------------------------------------------------------------------------------------

def speech_window(d, box, lines, speaker=None, size=64, tail=None, kind='speech', read=False, triangle_on=True):
    x0, y0, x1, y1 = box
    fill = {'speech': PANEL, 'thought': THOUGHT_FILL, 'narration': NARRATION_FILL}[kind]
    if kind == 'thought':
        d.rounded_rectangle(box, radius=36, fill=fill, outline=DIM_GOLD, width=3)
    else:
        panel(d, box, fill)
    if tail:                                    # the speech's tail, up to its speaker in the picture
        tx = tail
        d.polygon([(tx - 30, y0 + 2), (tx + 30, y0 + 2), (tx - 10, y0 - 44)], fill=fill)
        d.line([(tx - 30, y0), (tx - 10, y0 - 44), (tx + 30, y0)], fill=GOLD, width=3)
    if speaker:
        f = CINZEL(34, 600)
        pw = d.textlength(speaker, font=f) + 60
        panel(d, (x0 + 40, y0 - 34, x0 + 40 + pw, y0 + 30))
        d.text((x0 + 40 + pw / 2, y0 - 2), speaker, font=f, fill=NAME, anchor='mm')
    colour = (236, 132, 118) if read else (NARRATION_TEXT if kind == 'narration' else TEXT)
    for i, line in enumerate(lines):
        d.text((x0 + 44, y0 + (56 if speaker else 34) + i * size * 1.22), line, font=GOTHIC(size), fill=colour)
    if triangle_on:
        triangle(d, x1 - 50, y1 - 44, 20)


INTERCEPT = ['Intercept them.', 'All units to their assigned', 'positions. Please prepare to', 'attack.']


def d1():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = (TOP + BAR[0]) // 2 - 381
    img.paste(at_width(look(LOOK_INTERCEPT)), (0, y))
    d = ImageDraw.Draw(img)
    bottom_bar(d)
    note(d, (40, y - 190), 'The picture and our windows as on the PC,\nthe text 2.6 mm (16 sp): readable')
    note(d, (40, y + 800), 'A tap anywhere is the click that goes on;\nthe drawer\'s Skip and Log, and the menu bar\'s\n'
                           'commands, in the bar at the thumb')
    note(d, (40, 2330), 'Menu: File, Screen, Option, MsgSkip as finger rows', 28)
    save(img, 'PS1', 'the picture as on the PC, the bar under it', 'scenes')


def d2():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    img.paste(scene_clean(), (0, TOP))
    d = ImageDraw.Draw(img)
    pic_foot = TOP + 762
    speech_window(d, (16, pic_foot + 64, PW - 16, pic_foot + 64 + 420), INTERCEPT, speaker='EX', tail=230)
    bottom_bar(d)
    note(d, (40, pic_foot + 520), 'The window under the picture: the portraits and\nthe CG whole, the speaker on a plate;\n'
                                 'the speech\'s lines (32 cells) at 3.6 mm,\nthe script\'s breaks kept')
    note(d, (40, 1900), 'The window keeps four lines\' height,\nso nothing under it moves')
    note(d, (40, 2240), 'A tap anywhere goes on', 30)
    save(img, 'PS2', 'the window under the picture', 'scenes')


def d2b():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    d.rectangle((0, TOP, PW, TOP + 762), fill=(58, 58, 62))
    for x in range(-762, PW, 40):
        d.line([(x, TOP + 762), (x + 762, TOP)], fill=(72, 72, 76), width=3)
    d.text((PW / 2, TOP + 381), 'the scene\'s CG (masked)', font=GOTHIC(44), fill=(200, 200, 200), anchor='mm')
    pic_foot = TOP + 762
    speech_window(d, (16, pic_foot + 64, PW - 16, pic_foot + 64 + 330), ['The preparations are complete, King Rance.'],
                  speaker='MARIS', size=46)
    bottom_bar(d)
    note(d, (40, pic_foot + 430), 'Window 131 under its CG: the CG whole\n(on the PC the window covers its lower quarter);\n'
                                 '131\'s lines (49 cells) at 2.6 mm, as on the PC')
    save(img, 'PS2b', 'window 131 under its CG', 'scenes')


def d2m():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    d.rectangle((0, TOP, PW, TOP + 762), fill=(0, 0, 0))
    d.text((PW / 2, TOP + 381), 'the gallery\'s plain backdrop', font=GOTHIC(36), fill=(90, 90, 90), anchor='mm')
    pic_foot = TOP + 762
    speech_window(d, (16, pic_foot + 64, PW - 16, pic_foot + 64 + 420),
                  ['The rebels gather near Oaks.', 'What shall we do, King Rance?'], speaker='MARIS', tail=990,
                  triangle_on=False)
    items = ['Attack at dawn', 'Wait for the scouts', 'Ask Maris']
    h, gap = 136, 16
    y = BAR[1] - len(items) * h - (len(items) - 1) * gap
    panel(d, (16, y - 30, PW - 16, BAR[1] + 14))
    for i, item in enumerate(items):
        row_box(d, (40, y + i * (h + gap), PW - 40, y + i * (h + gap) + h), item, size=48)
    note(d, (40, pic_foot + 520), 'A `]` menu: finger rows at the thumb,\nin the script\'s order, in the bar\'s place\n'
                                 f'(rows {mm(h)} high); the question stays readable\n(made up: the gallery\'s scene menu)')
    save(img, 'PS2m', 'a dialogue menu', 'scenes')


CHRONICLE = [
    ('EX', 'thought', ['Either way, there’s nothing', 'left after this for me.', 'I’d sooner die fighting than',
                       'commit suicide.']),
    ('EX', 'thought', ['If I don’t, everyone that died', 'believing in me will turn over', 'in their graves.']),
    ('EX', 'thought', ['All I’m worried about is', 'Leazas...']),
    ('EX', 'thought', ['If I could just stop the day', 'of Leazas’s ruin that I fear', 'from coming...']),
    ('EX', 'thought', ['That’s all I care about...']),
    ('LEAZAS WHITE KNIGHT', 'speech', ['Excuse me!!']),
    ('', 'narration', ['The door was slammed open and', 'a soldier barged in.']),
    ('LEAZAS WHITE KNIGHT', 'speech', ['General Ex!', 'The enemy’s here!!']),
    ('EX', 'speech', ['I see...']),
    ('', 'narration', ['Ex slowly stood up from his', 'chair.']),
    ('EX', 'speech', INTERCEPT),
]


def d3():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    pic_foot = TOP + 762
    top, foot = pic_foot + 20, BAR[0] - 24
    d.rectangle((0, top, PW, foot), fill=(18, 13, 10))
    size, lh = 46, 58                           # 131's 49 cells fit the width with the breaks kept
    y = foot - 30
    for i, (who, kind, lines) in enumerate(reversed(CHRONICLE)):
        current = i == 0
        block = len(lines) * lh + (44 if who else 0) + 34
        y -= block
        if y < top - 200:
            break
        colour = TEXT if current else (NARRATION_TEXT if kind == 'narration' else QUIET)
        if not current:
            colour = tuple(round(c * 0.78) for c in colour)
        if current:
            d.rectangle((14, y - 10, PW - 14, y + block - 24), fill=(30, 22, 16), outline=DIM_GOLD, width=2)
        if who:
            d.text((44, y), who + ('  · thinking' if kind == 'thought' else ''), font=CINZEL(30, 600),
                   fill=NAME if current else (150, 112, 70))
        ty = y + (44 if who else 0)
        if kind == 'thought':
            for yy in range(ty, ty + len(lines) * lh - 10, 18):
                d.line([(30, yy), (30, yy + 9)], fill=DIM_GOLD, width=3)
        elif kind == 'speech':
            d.line([(30, ty), (30, ty + len(lines) * lh - 10)], fill=GOLD if current else DIM_GOLD, width=3)
        for k, line in enumerate(lines):
            d.text((50, ty + k * lh), line, font=GOTHIC(size), fill=colour)
        if current:
            triangle(d, PW - 60, y + block - 60, 20)
    fade = Image.new('L', (PW, 260))
    for k in range(260):
        ImageDraw.Draw(fade).line([(0, k), (PW, k)], fill=round(255 * (1 - k / 260)))
    img.paste(Image.new('RGB', (PW, 260), (18, 13, 10)), (0, top), fade)
    img.paste(scene_clean(), (0, TOP))
    d = ImageDraw.Draw(img)
    d.line([(0, pic_foot), (PW, pic_foot)], fill=GOLD, width=3)
    bottom_bar(d)
    note(d, (PW - 30, TOP + 16), 'The scene\'s pages run on under the picture,\nthe page waiting for the tap '
                                 'at the foot;\na drag down reads back (the log merged in);\ntext 2.6 mm, the breaks kept',
         30, 'ra')
    save(img, 'PS3', 'the chronicle under the picture', 'scenes')


# --- the map -------------------------------------------------------------------------------------------------------

TAGS = [('CASTLE', [('Construction', (46, 62, 104)), ('Harem', (128, 44, 70)), ('Merchant', (36, 92, 96))]),
        ('SUBORDINATES', [('Troops', (90, 110, 40)), ('Talk', (90, 110, 40))]),
        ('MAP', [('Own city', (140, 40, 34)), ('Attack', (140, 40, 34)), ('Dungeon', (36, 92, 96))])]
COMMANDS = [('Build', (46, 62, 104), 'Construction'), ('Harem', (128, 44, 70), 'Harem'),
            ('Items', (138, 104, 34), 'Treasure'), ('Trade', (36, 92, 96), 'Merchant'),
            ('Units', (70, 96, 52), 'Subordinates')]
LINKS = ['→ Leazas Castle', 'Load', 'Save', 'All Data', 'Think']
DATE = 'LP 3 · Month 5 · Week 2'


def status_top(d, y0, y1, tags=True):
    panel(d, (0, y0, PW, y1))
    d.text((32, y0 + 26), DATE, font=CINZEL(38, 700), fill=NAME)
    d.text((PW - 32, y0 + 30), '116,400K GOLD', font=GOTHIC(38), fill=TEXT, anchor='ra')
    d.text((32, y0 + 86), 'Troops 6,251', font=GOTHIC(34), fill=QUIET)
    if tags:
        x, y = 32, y0 + 146
        for group, items in TAGS:
            for word, colour in items:
                w = d.textlength(word, font=GOTHIC(28)) + 26
                if x + w > PW - 32:
                    x, y = 32, y + 58
                d.rectangle((x, y, x + w, y + 48), fill=colour)
                d.text((x + w / 2, y + 24), word, font=GOTHIC(28), fill=TEXT, anchor='mm')
                x += w + 8
            x += 22


def tag_row(d, box, word, colour, label, chip=None, off=False):
    row_box(d, box, label, size=40, tag=(word, colour), off=off)
    if chip:
        x1, y0, y1 = box[2], box[1], box[3]
        d.ellipse((x1 - 76, (y0 + y1) / 2 - 26, x1 - 24, (y0 + y1) / 2 + 26), fill=(60, 44, 24), outline=GOLD, width=3)
        d.text((x1 - 50, (y0 + y1) / 2), chip, font=GOTHIC(30), fill=NAME, anchor='mm')


def command_panel(d, top, foot, h=124, gap=14, close=False):
    """The command panel's two columns, filled from the foot up; Turn END at the right's top (the tablet's rails);
    with `close` the map's button, reading Close, at the right's foot."""
    colw = (PW - 16 * 3) // 2
    lx, rx = 16, 16 * 2 + colw
    d.text((lx + 6, top + h / 2), 'THIS WEEK', font=CINZEL(32, 700), fill=SUBTITLE, anchor='lm')
    red_button(d, (rx, top, rx + colw, top + h), 'Turn END', 52)
    for i, (word, colour, name) in enumerate(COMMANDS):
        y = foot - (i + 1) * h - i * gap
        tag_row(d, (lx, y, lx + colw, y + h), word, colour, name, chip='5' if name == 'Construction' else None)
    skip = 1 if close else 0
    for i, name in enumerate(LINKS):
        y = foot - (i + 1 + skip) * h - (i + skip) * gap
        row_box(d, (rx, y, rx + colw, y + h), name, size=40)
    if close:
        panel(d, (rx, foot - h, rx + colw, foot))
        icon(d, 'close', rx + 70, foot - h // 2)
        d.text((rx + colw / 2 + 30, foot - h / 2), 'Close', font=BRUSH(48), fill=NAME, anchor='mm')


def m1():
    img, d = phone()
    status_top(d, TOP - 10, 390)
    box, scale = (0, 390, PW, BAR[0] - 12), (BAR[0] - 12 - 390) / 800
    draw_map(img, box, 1290, 0, scale, 36)
    d = ImageDraw.Draw(img)
    d.rectangle((0, BAR[0] - 12, PW, PH), fill=BACKDROP)
    bottom_bar(d, right=('Commands', 'menu'))
    note(d, (30, 420), f'The map\'s whole height at {scale:.2f} phone pixels a map pixel\n'
                       '(the landscape phone: 3.05): it scrolls sideways only.\n'
                       f'A flag {mm(24 * scale)} wide; plates 2 mm text; a tap\'s area\n'
                       'the 8 mm round a marker, the nearest wins', 30)
    note(d, (30, 2230), 'The status on top (read-only); the week\'s uses\nas the strip\'s tags', 30)
    save(img, 'PM1a', 'the map fills the screen — the map wait', 'map')

    img, d = phone()
    status_top(d, TOP - 10, 390)
    draw_map(img, box, 1290, 0, scale, 36, dim=150)
    d = ImageDraw.Draw(img)
    sheet_top = 1610
    panel(d, (0, sheet_top, PW, BAR[0] - 12))
    command_panel(d, sheet_top + 24, BAR[0] - 40)
    d.rectangle((0, BAR[0] - 12, PW, PH), fill=BACKDROP)
    bottom_bar(d, right=('Close', 'close'), cells=())
    note(d, (30, sheet_top - 230), 'Commands opens the panel as a sheet over the map\'s\nfoot: the tablet\'s rails side by side, '
                                  'filled from\nthe foot up, Turn END at the top right', 30)
    note(d, (30, BAR[0] + 30), f'rows {mm(124)} high\nClose where Commands was', 30)
    save(img, 'PM1b', 'the map fills the screen — Commands', 'map')

    img, d = phone()
    status_top(d, TOP - 10, 390)
    x0, y0 = 1340, 88                   # scrolled up past the map's foot, so Oaks stays over the sheet
    draw_map(img, box, x0, y0, scale, 36, ring=6)
    d = ImageDraw.Draw(img)
    sheet_top = 1690
    province_sheet(d, img, sheet_top, BAR[0] - 12)
    d.rectangle((0, BAR[0] - 12, PW, PH), fill=BACKDROP)
    bottom_bar(d, right=('Return', 'close'), cells=())
    note(d, (30, 2020), 'A tap on Oaks rings it; its card and commands rise\nas a sheet, and the map scrolls up by the sheet\'s\n'
                        'height to keep it in view', 28)
    save(img, 'PM1c', 'the map fills the screen — a province', 'map')


def province_sheet(d, img, top, foot):
    panel(d, (0, top, PW, foot))
    pic = dump(TOUCH, 'province-menu.png').crop((568, 388, 838, 562))
    pic = pic.resize((420, round(420 * pic.height / pic.width)), Image.LANCZOS)
    img.paste(pic, (30, top + 34))
    d.rectangle((30, top + 34, 30 + pic.width, top + 34 + pic.height), outline=GOLD, width=3)
    tx = 30 + pic.width + 36
    d.text((tx, top + 40), 'Oaks', font=BRUSH(64), fill=NAME)
    d.text((tx, top + 126), 'REBEL ARMY', font=CINZEL(30, 600), fill=SUBTITLE)
    for i, label in enumerate(['Economy', 'Defense', 'Battle Area']):
        d.text((tx, top + 182 + i * 50), label, font=GOTHIC(32), fill=LABEL)
        d.text((PW - 40, top + 182 + i * 50), '??', font=GOTHIC(32), fill=TEXT, anchor='ra')
    h, gap = 128, 14
    rows = [('Ninja', (84, 60, 120), 'Ninja Maneuver'), ('Battle', (128, 34, 30), 'Attack')]
    for i, (word, colour, name) in enumerate(reversed(rows)):
        y = foot - 24 - (i + 1) * h - i * gap
        tag_row(d, (24, y, PW - 24, y + h), word, colour, name)


def m2_frame(img, d):
    d.rectangle((0, 0, PW, TOP + 96), fill=PANEL)
    d.line([(0, TOP + 96), (PW, TOP + 96)], fill=GOLD, width=3)
    d.text((32, TOP + 48), DATE, font=CINZEL(38, 700), fill=NAME, anchor='lm')
    d.text((PW - 32, TOP + 48), '116,400K GOLD', font=GOTHIC(36), fill=TEXT, anchor='rm')


M2_MAP = (0, TOP + 98, PW, TOP + 98 + 1525)            # the map's whole height at 1.906 (640 map pixels wide)


def m2():
    scale = 1525 / 800
    deck = (0, M2_MAP[3] + 6, PW, PH)
    img, d = phone()
    m2_frame(img, d)
    draw_map(img, M2_MAP, 1280, 0, scale, 30)
    d = ImageDraw.Draw(img)
    panel(d, (0, deck[1], PW, BAR[0] - 14))
    y = deck[1] + 30
    d.text((32, y), 'Troops 6,251', font=GOTHIC(38), fill=TEXT)
    d.text((PW - 32, y + 4), 'THIS WEEK\'S USES', font=CINZEL(28, 700), fill=SUBTITLE, anchor='ra')
    y += 84
    for group, items in TAGS:
        d.text((32, y + 30), group, font=CINZEL(26, 700), fill=SUBTITLE, anchor='lm')
        x = 330
        for word, colour in items:
            w = d.textlength(word, font=GOTHIC(34)) + 34
            d.rectangle((x, y, x + w, y + 60), fill=colour)
            d.text((x + w / 2, y + 30), word, font=GOTHIC(34), fill=TEXT, anchor='mm')
            x += w + 12
        y += 76
    d.rectangle((0, BAR[0] - 14, PW, PH), fill=BACKDROP)
    bottom_bar(d, right=('Commands', 'menu'))
    reach(img, label=False, shade=False)
    d = ImageDraw.Draw(img)
    note(d, (30, M2_MAP[1] + 20), 'The map\'s whole height at the picture\'s width\n(1.91; the landscape phone 3.05): '
                                  'the PC\'s view,\nits rows all shown; it scrolls sideways only', 30)
    note(d, (30, M2_MAP[3] - 150), f'plates 1.7 mm text; a flag {mm(24 * scale)} wide', 30)
    note(d, (PW - 30, deck[1] - 70), 'The deck under the map: the status, large', 30, 'ra')
    save(img, 'PM2a', 'the map and the deck — the map wait', 'map')

    img, d = phone()
    m2_frame(img, d)
    draw_map(img, M2_MAP, 1280, 0, scale, 30)
    d = ImageDraw.Draw(img)
    panel(d, (0, deck[1], PW, PH - 30))
    command_panel(d, deck[1] + 14, BAR[1], h=118, gap=10, close=True)
    note(d, (30, deck[1] - 150), 'Commands turns the deck into the panel;\nthe map stays whole above it', 30)
    note(d, (30, deck[1] + 110), f'rows {mm(118)} high: the deck\'s height\nholds no more under the whole map', 28)
    save(img, 'PM2b', 'the map and the deck — Commands', 'map')

    img, d = phone()
    m2_frame(img, d)
    draw_map(img, M2_MAP, 1280, 0, scale, 30, ring=6)
    d = ImageDraw.Draw(img)
    province_sheet(d, img, deck[1], BAR[0] - 14)
    d.rectangle((0, BAR[0] - 14, PW, PH), fill=BACKDROP)
    bottom_bar(d, right=('Return', 'close'), cells=())
    note(d, (30, deck[1] - 110), 'A province\'s card and commands in the deck;\nnothing covers the map', 30)
    save(img, 'PM2c', 'the map and the deck — a province', 'map')


# --- the battle ----------------------------------------------------------------------------------------------------

FACES = {'Rance': (308, 456), 'Rick': (308, 614), 'Ex': (859, 456), 'Sakanaku': (859, 614),
         'Melfeis': (1151, 456), 'Dodge': (1151, 614)}
UNITS = {
    'Rance': ('1', 'Leazas Knights', 451, 1000, (8, 8), 'Acting', '10 6 0 13'),
    'Rick': ('2', 'Leazas Knights', 773, 800, (8, 8), 'toggles', '10 6 0 20'),
    'Ex': ('1', 'Leazas Knights', 92, 1000, (1, 4), 'Acted', '10 6 0 1'),
    'Sakanaku': ('2', 'Leazas Soldiers', 60, 1000, (4, 6), 'Acted', '8 4 0 2'),
    'Melfeis': ('3', 'Leazas Mages', 300, 300, (4, 4), 'Acted', '3 3 7 9'),
    'Dodge': ('4', 'Leazas Soldiers', 932, 1000, (6, 6), '', '8 4 0 2'),
}


def face(name, size):
    x, y = FACES[name]
    return dump(LACQ, 'battle-turn.png').crop((x, y, x + 112, y + 112)).resize((size, size), Image.LANCZOS)


def bar(d, x, y, w, v, m, colour):
    d.rectangle((x, y, x + w, y + 10), fill=(40, 34, 28))
    d.rectangle((x, y, x + round(w * v / m), y + 10), fill=colour)


def toggles(d, x, y, w, size=30, h=54):
    bw = (w - 10) // 2
    for i, word in enumerate(['Auto-Atk', 'Auto-Def']):
        row_box(d, (x + i * (bw + 10), y, x + i * (bw + 10) + bw, y + h), word, size=size, centred=True)


def wide_card(d, img, box, name, player, acting=False, target=False):
    x0, y0, x1, y1 = box
    num, kind, trp, mtrp, (hp, mhp), state, stats = UNITS[name]
    d.rectangle(box, fill=(88, 72, 50) if acting else (28, 22, 18), outline=(240, 230, 210) if acting else DIM_GOLD,
                width=4 if acting else 2)
    fs = y1 - y0 - 54
    img.paste(face(name, fs), (x0 + 12, y0 + 12))
    d.text((x0 + 12 + fs / 2, y1 - 22), stats, font=GOTHIC(28), fill=TEXT, anchor='mm')
    tx, tw = x0 + fs + 32, x1 - x0 - fs - 48
    d.text((tx, y0 + 12), f'{num}  {name}', font=GOTHIC(40), fill=TEXT)
    d.text((tx, y0 + 58), kind, font=GOTHIC(32), fill=QUIET)
    d.text((tx, y0 + 96), f'Trp. {trp:,}/{mtrp:,}', font=GOTHIC(36), fill=TEXT)
    bar(d, tx, y0 + 138, tw, trp, mtrp, PLAYER_BLUE if player else ENEMY_RED)
    d.text((tx, y0 + 152), f'HP {hp}/{mhp}', font=GOTHIC(36), fill=TEXT)
    bar(d, tx, y0 + 194, tw, hp, mhp, BAR_GOOD)
    if state == 'toggles':
        toggles(d, tx, y1 - 64, tw, 28)
    elif state:
        d.text((tx, y0 + 210), state, font=GOTHIC(32), fill=QUIET)
    if target:
        brackets(d, box)


def brackets(d, box):
    x0, y0, x1, y1 = box[0] - 8, box[1] - 8, box[2] + 8, box[3] + 8
    L = 56
    for (cx, cy, sx, sy) in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        d.line([(cx, cy + sy * L), (cx, cy), (cx + sx * L, cy)], fill=GOLD, width=8)
    mx = (box[0] + box[2]) / 2
    d.polygon([(mx, y0 - 18), (mx + 16, y0), (mx, y0 + 18), (mx - 16, y0)], fill=(250, 222, 150))


def empty_slot(d, box):
    x0, y0, x1, y1 = box
    for x in range(x0, x1, 28):
        d.line([(x, y0), (min(x + 14, x1), y0)], fill=DIM_GOLD, width=2)
        d.line([(x, y1), (min(x + 14, x1), y1)], fill=DIM_GOLD, width=2)
    for y in range(y0, y1, 28):
        d.line([(x0, y), (x0, min(y + 14, y1))], fill=DIM_GOLD, width=2)
        d.line([(x1, y), (x1, min(y + 14, y1))], fill=DIM_GOLD, width=2)


def battle_scene(img):
    scene = dump(TOUCH, 'battle-turn.png').crop((128, 0, 1152, 410))
    img.paste(at_width(scene), (0, TOP))
    return TOP + round(410 * PW / 1024)


def battle_deck(d, top, skip=True, empty=False):
    panel(d, (0, top, PW, PH - 40))
    if empty:
        return None
    colw = (PW - 48) // 2
    lx, rx = 16, 32 + colw
    d.text((lx + 20, top + 46), 'RANCE', font=BRUSH(44), fill=NAME, anchor='lm')
    d.text((rx + 10, top + 46), 'TARGET', font=CINZEL(26, 700), fill=SUBTITLE, anchor='lm')
    d.text((rx + 160, top + 46), 'Ex', font=BRUSH(44), fill=(250, 222, 150), anchor='lm')
    d.line([(24, top + 88), (PW - 24, top + 88)], fill=DIM_GOLD, width=2)
    foot = PH - 70
    rows = ['Defend', 'Wait', 'Auto-Atk', 'Auto-Def']
    n = len(rows) + 1
    h = min(132, (foot - top - 110 - (n - 1) * 12) // n)
    for i, name in enumerate(rows):
        y = foot - (i + 1) * h - i * 12
        row_box(d, (lx, y, lx + colw, y + h), name, size=42, centred=True)
    y = foot - n * h - (n - 1) * 12
    red_button(d, (lx, y, lx + colw, y + h), 'Retreat', 46)
    gold_button(d, (rx, foot - 250, rx + colw, foot), 'Melee', ('Trp. 29-37', 'HP 0-1'))
    if skip:
        pill = (rx + colw - 220, top + 112, rx + colw, top + 190)
        panel(d, pill)
        icon(d, 'skip', pill[0] + 52, (pill[1] + pill[3]) // 2)
        d.text((pill[0] + 140, (pill[1] + pill[3]) // 2), 'Skip', font=GOTHIC(32), fill=TEXT, anchor='mm')
    return h


def b1():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = battle_scene(img)
    d = ImageDraw.Draw(img)
    cw, ch, gap = (PW - 48) // 2, 248, 12
    lx, rx = 16, 32 + cw
    d.text((24, y + 30), 'Leazas Rebel Army', font=CINZEL(32, 700), fill=TEXT, anchor='lm')
    d.text((PW - 24, y + 30), 'Walls +8%', font=GOTHIC(32), fill=(230, 196, 110), anchor='rm')
    y += 60
    wide_card(d, img, (lx, y, lx + cw, y + ch), 'Melfeis', False)
    wide_card(d, img, (rx, y, rx + cw, y + ch), 'Dodge', False)
    y += ch + gap
    wide_card(d, img, (lx, y, lx + cw, y + ch), 'Ex', False, target=True)
    wide_card(d, img, (rx, y, rx + cw, y + ch), 'Sakanaku', False)
    y += ch + 14
    # The round's gauge, turned across.
    for i in range(4):
        x = PW / 2 - 250 + i * 110
        d.rounded_rectangle((x, y + 12, x + 90, y + 62), radius=10, fill=(150, 120, 60), outline=GOLD, width=3)
    d.text((PW / 2 + 230, y + 37), '4  TURNS LEFT', font=CINZEL(30, 700), fill=NAME, anchor='lm')
    y += 84
    wide_card(d, img, (lx, y, lx + cw, y + ch), 'Rance', True, acting=True)
    wide_card(d, img, (rx, y, rx + cw, y + ch), 'Rick', True)
    y += ch + gap
    empty_slot(d, (lx, y, lx + cw, y + ch))
    empty_slot(d, (rx, y, rx + cw, y + ch))
    y += ch + 4
    d.text((24, y + 24), 'Rance Army', font=CINZEL(32, 700), fill=TEXT, anchor='lm')
    top = y + 56
    battle_deck(d, top)
    note(d, (640, top + 230), 'The scene at the width (1.19×);\nthe armies stacked, the enemy above:\n'
                              'its front row faces ours\nacross the gauge.\n'
                              f'Cards {mm(cw)} × {mm(ch)},\ntext 2.0-2.3 mm', 28)
    save(img, 'PB1', 'the armies stacked, the deck at the thumbs', 'battle')


def tall_card(d, img, box, name, player, acting=False, target=False):
    x0, y0, x1, y1 = box
    num, kind, trp, mtrp, (hp, mhp), state, stats = UNITS[name]
    d.rectangle(box, fill=(88, 72, 50) if acting else (28, 22, 18), outline=(240, 230, 210) if acting else DIM_GOLD,
                width=4 if acting else 2)
    fs = 150
    img.paste(face(name, fs), (x0 + 10, y0 + 10))
    d.text((x0 + 10 + fs + 14, y0 + 16), num, font=BRUSH(48), fill=NAME)
    for i, v in enumerate(stats.split()):
        d.text((x0 + fs + 24 + (i % 2) * 48, y0 + 84 + (i // 2) * 36), v, font=GOTHIC(28), fill=TEXT)
    w = x1 - x0 - 20
    d.text((x0 + 10, y0 + 172), name, font=fit(d, name, GOTHIC, 38, w), fill=TEXT)
    d.text((x0 + 10, y0 + 218), kind, font=fit(d, kind, GOTHIC, 30, w), fill=QUIET)
    d.text((x0 + 10, y0 + 260), f'Trp. {trp:,}/{mtrp:,}', font=fit(d, f'Trp. {trp:,}/{mtrp:,}', GOTHIC, 32, w),
           fill=TEXT)
    bar(d, x0 + 10, y0 + 300, w, trp, mtrp, PLAYER_BLUE if player else ENEMY_RED)
    d.text((x0 + 10, y0 + 318), f'HP {hp}/{mhp}', font=GOTHIC(32), fill=TEXT)
    bar(d, x0 + 10, y0 + 358, w, hp, mhp, BAR_GOOD)
    if state == 'toggles':
        toggles(d, x0 + 10, y1 - 66, w, 22)
    elif state:
        d.text((x0 + 10, y0 + 378), state, font=GOTHIC(30), fill=QUIET)
    if target:
        brackets(d, box)


def b2():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = battle_scene(img)
    d = ImageDraw.Draw(img)
    d.text((24, y + 34), 'Rance Army', font=CINZEL(30, 700), fill=TEXT, anchor='lm')
    d.text((PW - 24, y + 34), 'Leazas Rebel Army', font=CINZEL(30, 700), fill=TEXT, anchor='rm')
    for i in range(4):
        x = PW / 2 - 118 + i * 60
        d.rounded_rectangle((x, y + 14, x + 50, y + 54), radius=8, fill=(150, 120, 60), outline=GOLD, width=3)
    d.text((PW / 2, y + 80), '4 TURNS LEFT · Walls +8%', font=GOTHIC(26), fill=(230, 196, 110), anchor='mm')
    y += 112
    cw, ch, gap = 284, 444, 12
    cols = [16, 16 + cw + 10, PW - 16 - 2 * cw - 10, PW - 16 - cw]
    layout = [[None, 'Rance', 'Ex', 'Melfeis'], [None, 'Rick', 'Sakanaku', 'Dodge']]
    for r, row in enumerate(layout):
        for c, name in enumerate(row):
            box = (cols[c], y + r * (ch + gap), cols[c] + cw, y + r * (ch + gap) + ch)
            if name is None:
                empty_slot(d, box)
            else:
                tall_card(d, img, box, name, c < 2, acting=name == 'Rance', target=name == 'Ex')
    y += 2 * ch + gap + 20
    battle_deck(d, y)
    note(d, (640, y + 230), 'The armies side by side as in\nthe scene: ours left, theirs right.\n'
                            f'Cards {mm(cw)} × {mm(ch)},\ntext 1.7-2.2 mm', 28)
    save(img, 'PB2', 'the armies side by side, tall cards', 'battle')


# --- round P2: PB1 with the owner's changes (2026-10-27) -------------------------------------------------------------

# The exchange of the gallery's exchange-counters: Ex (commander 1) on Rance (unit 1), both by Melee.
EXCHANGE = dict(UNITS, Rance=('1', 'Leazas Knights', 560, 1000, (8, 8), 'pill', '10 6 0 13'),
                Rick=('2', 'Leazas Knights', 800, 800, (8, 8), 'toggles', '10 6 0 20'),
                Ex=('1', 'Leazas Knights', 92, 1000, (1, 4), 'pill', '10 6 0 1'),
                Sakanaku=('2', 'Leazas Soldiers', 177, 1000, (6, 6), '', '8 4 0 2'),
                Melfeis=('3', 'Leazas Mages', 300, 300, (4, 4), '', '3 3 7 9'))


def slim_card(img, box, name, player, data, acting=False, target=False, dim=False):
    """PB1's card at 230 high: the face left with its four figures, the text right."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    num, kind, trp, mtrp, (hp, mhp), state, stats = data
    d.rectangle(box, fill=(88, 72, 50) if acting else (28, 22, 18),
                outline=(240, 230, 210) if acting else (GOLD if state == 'pill' else DIM_GOLD), width=4 if acting else 2)
    fs = y1 - y0 - 52
    img.paste(face(name, fs), (x0 + 10, y0 + 10))
    d.text((x0 + 10 + fs / 2, y1 - 22), stats, font=GOTHIC(28), fill=TEXT, anchor='mm')
    tx, tw = x0 + fs + 28, x1 - x0 - fs - 44
    d.text((tx, y0 + 8), f'{num}  {name}', font=GOTHIC(38), fill=TEXT)
    d.text((tx, y0 + 50), kind, font=GOTHIC(30), fill=QUIET)
    d.text((tx, y0 + 86), f'Trp. {trp:,}/{mtrp:,}', font=GOTHIC(34), fill=TEXT)
    bar(d, tx, y0 + 124, tw, trp, mtrp, PLAYER_BLUE if player else ENEMY_RED)
    d.text((tx, y0 + 138), f'HP {hp}/{mhp}', font=GOTHIC(34), fill=TEXT)
    bar(d, tx, y0 + 176, tw, hp, mhp, BAR_GOOD)
    if state == 'toggles':
        toggles(d, tx, y1 - 46, tw, 24, h=38)
    elif state == 'pill':                       # the exchange's action pill
        p = (x1 - 196, y1 - 54, x1 - 14, y1 - 12)
        d.rounded_rectangle(p, radius=21, fill=(226, 186, 96))
        d.line([(p[0] + 22, p[3] - 10), (p[0] + 46, p[1] + 10)], fill=(44, 26, 10), width=5)
        d.text((p[0] + 112, (p[1] + p[3]) / 2), 'Melee', font=GOTHIC(30), fill=(44, 26, 10), anchor='mm')
    elif state:
        d.text((tx, y0 + 190), state, font=GOTHIC(30), fill=QUIET)
    if dim:
        part = img.crop(box)
        img.paste(Image.blend(part, Image.new('RGB', part.size), 0.5), box[:2])
    if target:
        brackets(ImageDraw.Draw(img), box)


def pb3_board(img, scene, data, acting=None, target=None, fighters=()):
    """Our army on top, the enemy's under it at the thumb, Skip between the cards and the deck; the deck's top."""
    backdrop(img, (0, 0, PW, PH))
    img.paste(at_width(dump(TOUCH, scene).crop((128, 0, 1152, 410))), (0, TOP))
    d = ImageDraw.Draw(img)
    y = TOP + round(410 * PW / 1024)
    cw, ch, gap = (PW - 48) // 2, 230, 10
    xs = (16, 32 + cw)
    d.text((24, y + 26), 'Rance Army', font=CINZEL(32, 700), fill=TEXT, anchor='lm')
    y += 52
    for row in ([None, None], ['Rance', 'Rick'], 'gauge', ['Ex', 'Sakanaku'], ['Melfeis', 'Dodge']):
        if row == 'gauge':                      # the round's gauge, turned across; the front rows face each other here
            for i in range(4):
                x = PW / 2 - 250 + i * 110
                d.rounded_rectangle((x, y + 14, x + 90, y + 58), radius=10, fill=(150, 120, 60), outline=GOLD, width=3)
            d.text((PW / 2 + 230, y + 36), '4  TURNS LEFT', font=CINZEL(30, 700), fill=NAME, anchor='lm')
            y += 82
            continue
        for name, x in zip(row, xs):
            box = (x, y, x + cw, y + ch)
            if name is None:
                empty_slot(d, box)
            else:
                slim_card(img, box, name, name in ('Rance', 'Rick'), data[name], acting=name == acting,
                          target=name == target, dim=bool(fighters) and name not in fighters)
        d = ImageDraw.Draw(img)
        y += ch + gap
    d.text((24, y + 18), 'Leazas Rebel Army', font=CINZEL(32, 700), fill=TEXT, anchor='lm')
    d.text((PW - 24, y + 18), 'Walls +8%', font=GOTHIC(32), fill=(230, 196, 110), anchor='rm')
    y += 44
    skip = (16, y, 316, y + 120)
    panel(d, skip)
    icon(d, 'skip', skip[0] + 80, y + 60)
    d.text((skip[0] + 190, y + 60), 'Skip', font=GOTHIC(40), fill=TEXT, anchor='mm')
    return y + 132


def pb3():
    img, d = phone()
    top = pb3_board(img, 'battle-turn.png', UNITS, acting='Rance', target='Ex')
    d = ImageDraw.Draw(img)
    h = battle_deck(d, top, skip=False)
    note(d, (640, top + 110), 'PB1 with the owner\'s changes:\nour army on top, the enemy\'s at\nthe thumb '
                              '(a tap moves the target);\nSkip between the cards and the deck.\n'
                              f'Cards {mm(PW // 2 - 24)} × {mm(230)}; rows {mm(h)}', 28)
    note(d, (340, top - 100), 'Skip at the left, as the tablet\'s', 26)
    save(img, 'PB3', 'the player\'s turn: our army on top', 'battle-p2')


def pb3r():
    img, d = phone()
    top = pb3_board(img, 'battle-turn.png', UNITS, acting='Rance', target='Ex')
    d = ImageDraw.Draw(img)
    h = battle_deck(d, top, skip=False)
    colw = (PW - 48) // 2
    rx, foot = 32 + colw, PH - 70
    ry = foot - 5 * h - 4 * 12                  # Retreat, at the left column's top
    box = (rx - 6, ry, PW - 16, ry + 330)
    panel(d, box, fill=(30, 22, 16))
    d.polygon([(rx - 6, ry + h / 2 - 22), (rx - 30, ry + h / 2), (rx - 6, ry + h / 2 + 22)], fill=GOLD)
    d.text((rx + 30, ry + 36), 'Retreat the whole army?', font=fit(d, 'Retreat the whole army?', GOTHIC, 38, colw - 60),
           fill=TEXT)
    d.text((rx + 30, ry + 90), 'The battle is lost.', font=GOTHIC(32), fill=QUIET)
    d.line([(rx + 24, ry + 146), (PW - 40, ry + 146)], fill=DIM_GOLD, width=2)
    bw = (PW - 16 - rx - 30 - 16) // 2
    row_box(d, (rx + 18, ry + 168, rx + 18 + bw, ry + 306), 'Cease Retreat', size=34, centred=True)
    red_button(d, (rx + 30 + bw, ry + 168, PW - 34, ry + 306), 'Retreat', 40)
    note(d, (340, top - 116), 'The question at Retreat (the tablet\'s A2), over\nthe deck\'s right column; '
                              f'answers {mm(138)} high,\nCease Retreat nearer the button, Retreat last', 25)
    save(img, 'PB3r', 'the retreat question', 'battle-p2')


def pb3x():
    img, d = phone()
    top = pb3_board(img, 'exchange-counters.png', EXCHANGE, acting='Ex', fighters=('Rance', 'Ex'))
    d = ImageDraw.Draw(img)
    battle_deck(d, top, empty=True)
    note(d, (40, top + 60), 'An exchange: the counters at the scene\'s foot (4 mm figures),\n'
                            'the fighters\' cards lit with their action\'s pill, the rest dimmed;\n'
                            'the deck empty while it plays (as the tablet\'s rails),\nSkip in its place', 28)
    save(img, 'PB3x', 'an exchange', 'battle-p2')


# --- round P3: the deployment table and Subordinates upright (2026-10-27) ---------------------------------------------

PAPER, PAPER_DARK, PARTY_ROW, CHOSEN_ROW = (224, 208, 172), (196, 176, 136), (217, 195, 147), (214, 172, 140)
INK, INK_QUIET, INK_HEAD, PAPER_LINE = (44, 34, 24), (122, 104, 80), (150, 40, 28), (190, 172, 138)

# The week-18 save's units as the gallery's deployment and Subordinates dumps show them: №, name, type, rows, troops,
# their maximum, Refill's price (None: full), the cost, the place in the party sent against Oaks (6).
LEDGER = [(1, 'Rance', 'Leazas Knights', 'Front', 1000, 1000, None, '300K', 1),
          (5, 'Leila', 'Royal Guards', 'Front', 800, 800, None, '1,200K', None),
          (6, 'Barres', 'Leazas Knights', 'Front', 980, 1000, '100K', '1,100K', 4),
          (7, 'Rick', 'Leazas Knights', 'Front', 612, 800, '300K', '1,000K', None),
          (8, 'Cordoba', 'Leazas Knights', 'Front', 1000, 1000, None, '1,100K', 3),
          (9, 'Kinkaid', 'Leazas Soldiers', 'Front', 901, 1500, '1,100K', '900K', None),
          (10, 'Menad', 'Leazas Soldiers', 'Front', 649, 1250, '1,200K', '800K', None),
          (11, 'Asuka', 'Leazas Mages', 'Back', 100, 100, None, '100K', None),
          (12, 'Haurein', 'Leazas Soldiers', 'Front', 209, 1400, '2,300K', '600K', 2)]
UNIT = {u[0]: u for u in LEDGER}
# The Troops tab, sorted by T.Atk as the gallery's deployment-troops: T.Atk, T.Def, Strikes, Mag, then the offense's,
# the defense's and the dungeon's Atk and Def.
TROOPS_TAB = {1: '10 6 14 0 0 0 0 +1 +1 0', 6: '10 6 3 0 +2 +1 +2 +1 −1 0', 7: '10 6 20 0 +3 0 0 −2 −2 −2',
              8: '10 6 7 0 −1 −1 0 +2 −2 −2', 5: '9 5 8 1 +1 +1 +1 +1 +1 +1', 9: '8 4 3 0 0 0 0 0 0 −2',
              10: '8 4 8 1 0 0 0 0 −2 0', 12: '8 4 6 0 0 0 +1 0 0 0', 11: '3 3 20 6 0 0 0 0 0 0'}
# The Commander tab as the gallery's subordinates-commander: Strat%, HP, Lvl, EXP, C.Atk, C.Def, the item.
COMMANDER_TAB = {1: '0% 8/8 1 0/130 22 18 L. Holy Sword', 5: '8% 5/5 0 15/100 20 13 —', 6: '15% 5/5 0 54/100 14 7 —',
                 7: '0% 8/8 0 47/100 23 18 By Lord', 8: '0% 9/9 0 49/100 17 15 —', 9: '2% 7/7 0 19/100 14 10 —',
                 10: '8% 4/4 0 6/100 12 9 —', 11: '0% 1/1 0 25/100 1 1 —', 12: '5% 5/5 0 0/100 13 9 —'}
# Each unit's face CG (SET_CHR.ADV, Unit_FaceCg) and its square (UnitFaces.Exceptions).
FACE_CG = {1: (1, (33, 7, 128)), 5: (7, (28, 7, 136)), 6: (402, (15, 9, 136)), 7: (403, (44, 7, 136)),
           8: (404, (28, 7, 128)), 9: (406, (38, 7, 112)), 10: (8, (31, 7, 128)), 11: (430, (26, 45, 144)),
           12: (10, (32, 7, 128))}
NUMBER_ORDER = [u[0] for u in LEDGER]
TROOPS_ORDER = list(TROOPS_TAB)
TABS = ['Overview', 'Troops', 'Commander']
# Add New Troops for Rance (the gallery's subordinates) and Haurein (subordinates-question shows +400 and +800; the
# cheaper ones derived from them, each step three times the one before as Rance's are); greyed above the budget.
ADD_NEW = {1: ['11,400K', '34,200K', '102,600K', '307,800K', '923,400K', '2,770,200K'],
           12: ['5,400K', '16,200K', '48,600K', '145,800K', '437,400K', '1,312,200K']}
BUDGET = 116_400


def unit_face(n, size):
    cg, (x, y, s) = FACE_CG[n]
    im = np.array(Image.open(os.path.join(EXTRACT, 'cg_png', f'CG{cg:04d}.png')).convert('RGB'))
    key = (im[..., 0] > 200) & (im[..., 1] < 60) & (im[..., 2] > 200)          # the palette's key, magenta
    im[key] = (52, 58, 74)
    return Image.fromarray(im).crop((x, y, x + s, y + s)).resize((size, size), Image.LANCZOS)


def arrow_mark(d, x, y, way, size=10, fill=INK_HEAD):
    """▲ ▼ ▶: Cinzel has no arrows."""
    s = size
    pts = {'up': [(x - s, y + s), (x + s, y + s), (x, y - s)], 'down': [(x - s, y - s), (x + s, y - s), (x, y + s)],
           'right': [(x - s, y - s), (x - s, y + s), (x + s, y)]}[way]
    d.polygon(pts, fill=fill)


def divider(d, y, x0=24, x1=PW - 24):
    d.line([(x0, y), (x1, y)], fill=DIM_GOLD, width=2)
    m = (x0 + x1) / 2
    d.polygon([(m, y - 9), (m + 9, y), (m, y + 9), (m - 9, y)], fill=GOLD)


def title_band(d, title, subtitle, y=TOP + 4):
    """The title band: Shojumaru title, the date right, the Cinzel subtitle under them, a divider."""
    d.text((30, y), title, font=BRUSH(62), fill=NAME)
    d.text((PW - 30, y + 22), DATE, font=CINZEL(32, 700), fill=NAME, anchor='ra')
    d.text((32, y + 92), subtitle, font=CINZEL(30, 600), fill=SUBTITLE)
    divider(d, y + 144)
    return y + 156


def paper_tabs(d, y, active, h=116, w=340, gap=14):
    """The ledger's tabs: the chosen one paper with a vermilion top, joined to the sheet; the others darker."""
    for i, name in enumerate(TABS):
        x = 24 + i * (w + gap)
        on = i == active
        d.rectangle((x, y + (0 if on else 14), x + w, y + h + (2 if on else 0)), fill=PAPER if on else PAPER_DARK,
                    outline=None if on else PAPER_LINE)
        if on:
            d.rectangle((x, y, x + w, y + 9), fill=VERMILION)
        d.text((x + w / 2, y + h / 2 + (4 if on else 10)), name, font=CINZEL(40, 700),
               fill=INK_HEAD if on else INK_QUIET, anchor='mm')
    return y + h


def bar_colour(v, m):
    r = v / m
    return (96, 140, 64) if r >= 0.75 else (190, 136, 46) if r >= 1 / 3 else (184, 56, 38)


def strength_bar(d, x, y, w, v, m, h=8, track=(200, 184, 150)):
    d.rectangle((x, y, x + w, y + h), fill=track)
    d.rectangle((x, y, x + round(w * v / m), y + h), fill=bar_colour(v, m))


def seal(d, x, y, n, s=60):
    """A place in the party on the ledger: a vermilion seal."""
    d.rectangle((x, y, x + s, y + s), fill=(176, 64, 40), outline=(120, 30, 20), width=2)
    d.rectangle((x + 6, y + 6, x + s - 6, y + s - 6), outline=(236, 170, 130), width=2)
    d.text((x + s / 2, y + s / 2 + 1), str(n), font=GOTHIC(round(s * 0.62)), fill=(250, 236, 214), anchor='mm')


def badge(d, cx, cy, n, r=26):
    """A place on a party card: the number on a gold diamond."""
    d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=(46, 34, 20), outline=GOLD, width=3)
    d.text((cx, cy + 1), str(n), font=GOTHIC(round(r * 1.15)), fill=NAME, anchor='mm')


def close_box(d, x1, y0, s=88):
    b = (x1 - s - 10, y0 + 10, x1 - 10, y0 + 10 + s)
    d.rectangle(b, fill=(44, 24, 18), outline=(150, 62, 44), width=2)
    cx, cy = (b[0] + b[2]) / 2, (b[1] + b[3]) / 2
    for sx in (-1, 1):
        d.line([(cx - 17, cy - 17 * sx), (cx + 17, cy + 17 * sx)], fill=(222, 96, 74), width=5)


def refill_button(d, box, price):
    d.rounded_rectangle(box, radius=10, fill=(232, 218, 186), outline=(196, 128, 98), width=2)
    d.text(((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), price, font=GOTHIC(36), fill=INK_HEAD, anchor='mm')


TWO_LINE_HEADS = {
    0: [('TROOPS', 184, 'la'), ('REFILL', 690, 'ma'), ('COST', PW - 40, 'ra')],
    1: [('T.ATK', 280, 'ra'), ('T.DEF', 390, 'ra'), ('STRIKES', 525, 'ra'), ('MAG', 600, 'ra'), ('ATK', 700, 'ra'),
        ('DEF', 790, 'ra'), ('ATK', 900, 'ra'), ('DEF', 990, 'ra'), ('ATK', 1100, 'ra'), ('DEF', PW - 40, 'ra')],
    2: [('STRAT%', 320, 'ra'), ('HP', 430, 'ra'), ('LVL', 510, 'ra'), ('EXP', 690, 'ra'), ('C.ATK', 810, 'ra'),
        ('C.DEF', 920, 'ra'), ('ITEM', 950, 'la')],
}
TROOP_GROUPS = [('OFFENSE', 715), ('DEFENSE', 915), ('DUNGEON', 1110)]


def ledger_head(d, y, tab, chooser=True):
    """The two-line rows' header: the identity's columns over line 1, the tab's over line 2."""
    f = CINZEL(27, 700)
    if chooser:
        d.ellipse((50, y + 22, 66, y + 38), fill=INK_HEAD)
    d.text((150, y + 14), 'No.', font=f, fill=INK_HEAD, anchor='ra')
    if tab != 1:
        arrow_mark(d, 164, y + 30, 'up', 7)
    else:
        arrow_mark(d, 290, y + 70, 'down', 6)
    d.text((184, y + 14), 'NAME', font=f, fill=INK_HEAD)
    if tab == 0:
        d.text((560, y + 14), 'TYPE', font=f, fill=INK_HEAD)
        d.text((PW - 40, y + 14), 'ROWS', font=f, fill=INK_HEAD, anchor='ra')
    if tab == 1:
        for name, cx in TROOP_GROUPS:
            d.text((cx, y + 16), name, font=CINZEL(22, 600), fill=INK_QUIET, anchor='ma')
            d.line([(cx - 84, y + 46), (cx + 84, y + 46)], fill=PAPER_LINE, width=2)
    for name, x, anchor in TWO_LINE_HEADS[tab]:
        d.text((x, y + 56), name, font=CINZEL(23, 700) if tab == 1 else f, fill=INK_HEAD, anchor=anchor)
    d.line([(16, y + 98), (PW - 16, y + 98)], fill=INK_HEAD, width=2)
    return y + 100


def ledger_row(d, box, n, tab, chosen=False, party=True, refill=True):
    """A unit in two lines: its seal, № and name (and type and rows on Overview) over the tab's figures."""
    x0, y0, x1, y1 = box
    no, name, kind, rows, trp, mtrp, price, cost, place = UNIT[n]
    picked = party and place
    fill = CHOSEN_ROW if chosen else PARTY_ROW if picked else PAPER
    d.rectangle(box, fill=fill)
    if chosen:
        d.rectangle((x0, y0, x0 + 8, y1), fill=VERMILION)
        d.polygon([(58, y0 + 40), (68, y0 + 50), (58, y0 + 60), (48, y0 + 50)], fill=INK_HEAD)
    if picked:
        seal(d, 28, y0 + 18, place)
    d.line([(x0, y1), (x1, y1)], fill=PAPER_LINE, width=2)
    l1, l2 = y0 + 18, y0 + (y1 - y0) // 2 + 14
    d.text((170, l1), str(no), font=GOTHIC(44), fill=INK, anchor='ra')
    d.text((184, l1), name, font=GOTHIC(44), fill=INK)
    if tab == 0:
        d.text((560, l1 + 6), kind, font=GOTHIC(36), fill=INK_QUIET)
        d.text((PW - 40, l1 + 6), rows, font=GOTHIC(36), fill=INK_QUIET, anchor='ra')
        d.text((184, l2), f'{trp:,}/{mtrp:,}', font=GOTHIC(38), fill=INK)
        strength_bar(d, 184, l2 + 46, 260, trp, mtrp)
        if price and refill:
            refill_button(d, (590, l2 - 6, 790, l2 + 52), price)
        elif price:
            d.text((690, l2), price, font=GOTHIC(38), fill=INK_HEAD, anchor='ma')
        else:
            d.text((690, l2), '—', font=GOTHIC(38), fill=INK_QUIET, anchor='ma')
        d.text((PW - 40, l2), cost, font=GOTHIC(38), fill=INK, anchor='ra')
    elif tab == 1:
        for v, (_, x, _) in zip(TROOPS_TAB[n].split(), TWO_LINE_HEADS[1]):
            d.text((x, l2), v, font=GOTHIC(38), fill=INK_QUIET if v == '0' else INK, anchor='ra')
    else:
        parts = COMMANDER_TAB[n].split(' ', 6)
        for v, (_, x, anchor) in zip(parts, TWO_LINE_HEADS[2]):
            d.text((x, l2), v, font=GOTHIC(38 if anchor == 'ra' else 34), fill=INK_QUIET if v == '—' else INK,
                   anchor=anchor)


def ledger(d, top, foot, tab, order, row_h, chosen=None, party=True, refill=True):
    d.rectangle((16, top, PW - 16, foot), fill=PAPER)
    y = ledger_head(d, top, tab, chooser=party)
    for n in order:
        ledger_row(d, (16, y, PW - 16, y + row_h), n, tab, chosen=n == chosen, party=party, refill=refill)
        y += row_h
    return y


def party_card(img, box, n, place, face_size=None, kind=True):
    """A party card on lacquer: the face, the place's badge and the name, the troops with their bar, the cost, ✕."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    _, name, kind_, _, trp, mtrp, _, cost, _ = UNIT[n]
    d.rectangle(box, fill=ROW, outline=DIM_GOLD, width=2)
    fs = face_size or y1 - y0 - 24
    img.paste(unit_face(n, fs), (x0 + 12, y0 + 12))
    d.rectangle((x0 + 12, y0 + 12, x0 + 12 + fs, y0 + 12 + fs), outline=DIM_GOLD, width=2)
    tx, tw = x0 + fs + 32, x1 - x0 - fs - 46
    badge(d, tx + 24, y0 + 44, place)
    d.text((tx + 60, y0 + 44), name, font=fit(d, name, GOTHIC, 42, tw - 160), fill=TEXT, anchor='lm')
    close_box(d, x1, y0, 84)
    y = y0 + 84
    if kind:
        d.text((tx, y), kind_, font=GOTHIC(30), fill=QUIET)
        y += 44
    d.text((tx, y), f'{trp:,}/{mtrp:,}', font=GOTHIC(34), fill=TEXT)
    strength_bar(d, tx, y + 44, tw - 6 if kind else tw - 200, trp, mtrp, track=(60, 50, 40))
    d.text((x1 - 16, y1 - 14), cost, font=GOTHIC(32), fill=TEXT, anchor='rd')
    d.text((x1 - 16 - d.textlength(cost, font=GOTHIC(32)) - 14, y1 - 18), 'COST', font=CINZEL(22, 600),
           fill=SUBTITLE, anchor='rd')


PARTY = {1: 1, 2: 12, 3: 8, 4: 6}          # place → unit: Rance, Haurein, Cordoba, Barres


def party_totals(d, y, budget_line=False):
    d.text((32, y), 'TROOPS', font=CINZEL(26, 600), fill=SUBTITLE, anchor='lm')
    d.text((170, y), '3,189', font=GOTHIC(38), fill=TEXT, anchor='lm')
    d.text((PW - 32, y), '3,100K', font=GOTHIC(38), fill=TEXT, anchor='rm')
    d.text((PW - 32 - d.textlength('3,100K', font=GOTHIC(38)) - 16, y), 'WAR EXPENSES', font=CINZEL(26, 600),
           fill=SUBTITLE, anchor='rm')


def budget(d, x1, y, size=40):
    d.text((x1, y), 'K GOLD', font=CINZEL(24, 600), fill=SUBTITLE, anchor='rm')
    x = x1 - d.textlength('K GOLD', font=CINZEL(24, 600)) - 12
    d.text((x, y), f'{BUDGET:,}', font=BRUSH(size), fill=NAME, anchor='rm')
    x -= d.textlength(f'{BUDGET:,}', font=BRUSH(size)) + 14
    d.text((x, y), 'BUDGET', font=CINZEL(24, 600), fill=SUBTITLE, anchor='rm')


def stop_attack(d, left='Stop', right='Attack'):
    y0, y1 = BAR
    panel(d, (16, y0, 420, y1))
    d.text((218, (y0 + y1) / 2), left, font=BRUSH(48), fill=TEXT, anchor='mm')
    red_button(d, (436, y0, PW - 16, y1), right, 60)


def pd1():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = title_band(d, 'Deployment', 'Attack · up to 4 units')
    y = paper_tabs(d, y + 6, 0)
    row_h, dock = 140, 1800
    ledger(d, y, dock - 14, 0, NUMBER_ORDER, row_h)
    panel(d, (0, dock, PW, BAR[0] - 16))
    d.text((32, dock + 52), 'Party', font=BRUSH(48), fill=NAME, anchor='lm')
    budget(d, PW - 32, dock + 52)
    cw, ch = (PW - 32 - 14 - 44) // 2, 230
    lx, rx = 16, 16 + cw + 14
    d.text((lx + cw / 2, dock + 118), 'BACK ROW', font=CINZEL(26, 700), fill=SUBTITLE, anchor='mm')
    d.text((rx + cw / 2, dock + 118), 'FRONT ROW', font=CINZEL(26, 700), fill=SUBTITLE, anchor='mm')
    arrow_mark(d, rx + cw / 2 + 100, dock + 118, 'right', 8, SUBTITLE)
    cy = dock + 146
    for row in range(2):
        for x, place in ((lx, 3 + row), (rx, 1 + row)):
            party_card(img, (x, cy + row * (ch + 12), x + cw, cy + row * (ch + 12) + ch), PARTY[place], place)
    d = ImageDraw.Draw(img)
    tx = rx + cw + 10                            # the way the party faces
    d.line([(tx, cy), (tx + 10, cy), (tx + 10, cy + 2 * ch + 12), (tx, cy + 2 * ch + 12)], fill=GOLD, width=3)
    d.polygon([(tx + 16, cy + ch - 34), (tx + 40, cy + ch + 6), (tx + 16, cy + ch + 46)], fill=GOLD)
    party_totals(d, cy + 2 * ch + 12 + 40)
    stop_attack(d)
    reach(img, label=False, shade=False)
    d = ImageDraw.Draw(img)
    save(img, 'PD1', 'the ledger on top, the party at the thumb', 'deployment', side=[
        'The ledger as two-line rows:\nthe unit (№, name, type, rows)\nover the tab\'s figures — the tabs\n'
        'change line 2 only.',
        f'Rows {mm(row_h)} high; the names\n{mm(44)}, the figures {mm(38)}.',
        'A tap picks a unit or puts it\nback; a long press shows its\npicture (PD2d). A longer list\nscrolls '
        'under the finger.',
        f'The party at the thumb as on\nthe PC: the back row left, the\nfront row right, facing right.\n'
        f'Cards {mm(cw)} × {mm(ch)}; the cross {mm(84)};\na drag swaps places.',
        'Stop and Attack in the bar,\nAttack at the right thumb.'])


def pd2_base(img, d, tab=1):
    backdrop(img, (0, 0, PW, PH))
    y = title_band(d, 'Deployment', 'Attack · up to 4 units')
    top = y + 4
    panel(d, (0, top, PW, top + 600))
    d.text((32, top + 50), 'Party', font=BRUSH(48), fill=NAME, anchor='lm')
    budget(d, PW - 32, top + 50)
    cw, ch = (PW - 32 - 14) // 2, 170
    xs = (16, 16 + cw + 14)
    y = top + 96
    for label, places in (('BACK ROW', (3, 4)), ('FRONT ROW', (1, 2))):
        d.text((PW / 2, y + 20), label, font=CINZEL(26, 700), fill=SUBTITLE, anchor='mm')
        if places == (1, 2):                    # the way the party faces: down, as our army in PB3
            arrow_mark(d, PW / 2 + 100, y + 20, 'down', 8, SUBTITLE)
        y += 42
        for x, place in zip(xs, places):
            party_card(img, (x, y, x + cw, y + ch), PARTY[place], place, kind=False)
        d = ImageDraw.Draw(img)
        y += ch + 10
    party_totals(d, y + 30)
    y = top + 600
    y = paper_tabs(d, y + 14, tab)
    row_h = 150
    ledger(d, y, BAR[0] - 20, tab, TROOPS_ORDER if tab == 1 else NUMBER_ORDER, row_h)
    stop_attack(d)
    return d, top, row_h


def pd2():
    img, d = phone()
    d, top, row_h = pd2_base(img, d)
    reach(img, label=False, shade=False)
    d = ImageDraw.Draw(img)
    save(img, 'PD2', 'the party on top, the ledger at the thumb', 'deployment', side=[
        'The party on top as the battle\nshows our army (PB3): the back\nrow, then the front row, facing\ndown.',
        'The ledger under it at the thumb,\nas the enemy\'s cards are in PB3:\nwhat is tapped most is nearest\n'
        'the thumb.',
        f'Shown: the Troops tab — line 2\nholds its ten figures; rows\n{mm(row_h)}, figures {mm(38)}.',
        'Cards 33 × 9.7 mm; the cross and\ndrag as PD1\'s, out of the\nthumb\'s reach.'])


def pd2d():
    img, d = phone()
    d, top, row_h = pd2_base(img, d)
    veil = Image.new('RGB', (PW, PH - top), (0, 0, 0))
    img.paste(Image.blend(img.crop((0, top, PW, PH)), veil, 0.55), (0, top))
    d = ImageDraw.Draw(img)
    pic = dump(TOUCH, 'deployment.png').crop((716, 16, 1264, 358))
    pic = pic.resize((PW - 32, round((PW - 32) * pic.height / pic.width)), Image.LANCZOS)
    sheet = BAR[1] - pic.height - 150
    panel(d, (0, sheet, PW, BAR[1] + 20))
    d.text((32, sheet + 60), 'Rance', font=BRUSH(52), fill=NAME, anchor='lm')
    d.text((250, sheet + 64), 'LEAZAS KNIGHTS · 1,000/1,000', font=CINZEL(28, 600), fill=SUBTITLE, anchor='lm')
    img.paste(pic, (16, sheet + 116))
    d.rectangle((16, sheet + 116, PW - 16, sheet + 116 + pic.height), outline=GOLD, width=3)
    side = ['A long press on Rance\'s row:\nhis picture and the original\'s\nwords about him rise over the\nledger '
            '(the PC shows them\nwhile his row is hovered).',
            'At the screen\'s width, 2.2×:\nthe words 1.9 mm.', 'A tap anywhere puts it away.']
    save(img, 'PD2d', 'a long press: the unit\'s picture', 'deployment', side=side)


def pd3():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = title_band(d, 'Deployment', 'Attack · up to 4 units')
    y = paper_tabs(d, y + 6, 0)
    dock, ttop = 1930, y
    d.rectangle((16, y, PW - 16, dock - 14), fill=PAPER)
    f, fh, row_h = GOTHIC(40), CINZEL(27, 700), 112
    frozen = 392
    cols = [('TYPE', 408, 'la'), ('ROWS', 728, 'la'), ('TROOPS', 850, 'la'), ('REFILL', 1170, 'ma'), ('COST', 1400, 'ra')]
    d.ellipse((50, y + 22, 66, y + 38), fill=INK_HEAD)
    d.text((136, y + 14), 'No.', font=fh, fill=INK_HEAD, anchor='ra')
    arrow_mark(d, 150, y + 30, 'up', 7)
    d.text((164, y + 14), 'NAME', font=fh, fill=INK_HEAD)
    for name, x, anchor in cols:
        d.text((x, y + 14), name, font=fh, fill=INK_HEAD, anchor=anchor)
    d.line([(16, y + 58), (PW - 16, y + 58)], fill=INK_HEAD, width=2)
    y += 60
    for n in NUMBER_ORDER:
        no, name, kind, rows, trp, mtrp, price, cost, place = UNIT[n]
        d.rectangle((16, y, PW - 16, y + row_h), fill=PARTY_ROW if place else PAPER)
        d.line([(16, y + row_h), (PW - 16, y + row_h)], fill=PAPER_LINE, width=2)
        if place:
            seal(d, 26, y + 28, place, 56)
        ty = y + row_h / 2
        d.text((150, ty), str(no), font=f, fill=INK, anchor='rm')
        d.text((164, ty), name, font=f, fill=INK, anchor='lm')
        d.text((408, ty), kind, font=GOTHIC(36), fill=INK_QUIET, anchor='lm')
        d.text((728, ty), rows, font=GOTHIC(36), fill=INK_QUIET, anchor='lm')
        d.text((850, ty - 6), f'{trp:,}/{mtrp:,}', font=GOTHIC(38), fill=INK, anchor='lm')
        strength_bar(d, 850, ty + 24, 220, trp, mtrp)
        if price:
            refill_button(d, (1080, ty - 30, 1260, ty + 30), price)
        else:
            d.text((1170, ty), '—', font=GOTHIC(38), fill=INK_QUIET, anchor='mm')
        y += row_h
    d.rectangle((PW - 16, ttop, PW, dock), fill=BACKDROP)      # the sheet's right edge, the rest scrolled off
    fade = Image.new('L', (90, dock - 14 - ttop))
    for k in range(90):
        ImageDraw.Draw(fade).line([(k, 0), (k, fade.height)], fill=round(230 * k / 90))
    img.paste(Image.new('RGB', fade.size, PAPER), (PW - 16 - 90, ttop), fade)
    d = ImageDraw.Draw(img)
    d.line([(frozen, ttop), (frozen, y)], fill=INK_HEAD, width=2)
    d.rounded_rectangle((frozen + 20, dock - 34, frozen + 520, dock - 24), radius=5, fill=(150, 130, 96))
    # The party as a strip of four at the thumb.
    panel(d, (0, dock, PW, BAR[0] - 16))
    d.text((32, dock + 50), 'Party', font=BRUSH(48), fill=NAME, anchor='lm')
    budget(d, PW - 32, dock + 50)
    sw, sh = (PW - 32 - 3 * 12 - 40) // 4, 300
    sy = dock + 140
    d.text((16 + sw + 6, sy - 30), 'BACK ROW', font=CINZEL(26, 700), fill=SUBTITLE, anchor='mm')
    d.text((16 + 2 * (sw + 12) + 40 + sw + 6, sy - 30), 'FRONT ROW', font=CINZEL(26, 700), fill=SUBTITLE,
           anchor='mm')
    arrow_mark(d, 16 + 2 * (sw + 12) + 40 + sw + 106, sy - 30, 'right', 8, SUBTITLE)
    for i, place in enumerate((3, 4, 1, 2)):
        x = 16 + i * (sw + 12) + (40 if i >= 2 else 0)
        n = PARTY[place]
        _, name, _, _, trp, mtrp, _, cost, _ = UNIT[n]
        d.rectangle((x, sy, x + sw, sy + sh), fill=ROW, outline=DIM_GOLD, width=2)
        fs = 150
        img.paste(unit_face(n, fs), (x + 12, sy + 12))
        d = ImageDraw.Draw(img)
        badge(d, x + 12 + fs + 30, sy + 44, place)
        close_box(d, x + sw, sy + 80, 84)
        d.text((x + 14, sy + 176), name, font=fit(d, name, GOTHIC, 40, sw - 28), fill=TEXT)
        d.text((x + 14, sy + 226), f'{trp:,}', font=GOTHIC(34), fill=TEXT)
        strength_bar(d, x + 14, sy + 270, sw - 28, trp, mtrp, track=(60, 50, 40))
    mx = 16 + 2 * sw + 12 + 20
    d.polygon([(mx - 10, sy + sh / 2 - 24), (mx + 12, sy + sh / 2), (mx - 10, sy + sh / 2 + 24)], fill=GOLD)
    party_totals(d, sy + sh + 50)
    stop_attack(d)
    reach(img, label=False, shade=False)
    d = ImageDraw.Draw(img)
    save(img, 'PD3', 'the table in one-line rows, scrolled sideways', 'deployment', side=[
        f'The PC\'s table in one-line rows,\n{mm(row_h)} high, text {mm(40)}: 13\nrows in view.',
        'The seal, № and name stay; the\nrest scrolls sideways under a\nfinger (the tablet\'s drag) —\n'
        'Refill and Cost a drag away.',
        'The party as a strip of four at\nthe thumb: the back row, then\nthe front row, facing right.',
        'The picture on a long press,\nas PD2d.'])


# --- Subordinates ---

def add_new_grid(d, x0, y0, x1, n, h=128, gap=12, cover=None):
    d.text((x0 + 4, y0 + 22), 'ADD NEW TROOPS', font=CINZEL(28, 700), fill=SUBTITLE, anchor='lm')
    d.line([(x0 + 330, y0 + 22), (x1, y0 + 22)], fill=DIM_GOLD, width=2)
    y0 += 52
    w = (x1 - x0 - 2 * gap) // 3
    for i, (step, price) in enumerate(zip(('+25', '+50', '+100', '+200', '+400', '+800'), ADD_NEW[n])):
        x, y = x0 + (i % 3) * (w + gap), y0 + (i // 3) * (h + gap)
        off = int(price[:-1].replace(',', '')) > BUDGET
        d.rectangle((x, y, x + w, y + h), fill=ROW if not off else (30, 26, 22), outline=OFF_TAG if off else DIM_GOLD,
                    width=3)
        d.text((x + w / 2, y + 46), step, font=BRUSH(48), fill=OFF_TEXT if off else TEXT, anchor='mm')
        d.text((x + w / 2, y + 100), price, font=GOTHIC(32), fill=OFF_TEXT if off else QUIET, anchor='mm')
    return y0 + 2 * h + gap


def replenish(d, box, figures=None):
    x0, y0, x1, y1 = box
    if figures:
        red_button(d, box, '', 52)
        d.text(((x0 + x1) / 2, y0 + 46), 'Replenish', font=BRUSH(52), fill=NAME, anchor='mm')
        d.text(((x0 + x1) / 2, y1 - 30), figures, font=GOTHIC(32), fill=TEXT, anchor='mm')
    else:
        d.rectangle(box, fill=(40, 34, 28), outline=OFF_TAG, width=3)
        d.text(((x0 + x1) / 2, y0 + 46), 'Replenish', font=BRUSH(52), fill=OFF_TEXT, anchor='mm')
        d.text(((x0 + x1) / 2, y1 - 30), 'full', font=GOTHIC(32), fill=OFF_TEXT, anchor='mm')


def talk_end(img, n, right='End'):
    d = ImageDraw.Draw(img)
    y0, y1 = BAR
    panel(d, (16, y0, 620, y1))
    img.paste(unit_face(n, y1 - y0 - 28), (30, y0 + 14))
    d = ImageDraw.Draw(img)
    d.text((30 + (y1 - y0 - 28) + (620 - 30 - (y1 - y0 - 28)) / 2, y0 + 56), 'Talk', font=BRUSH(48), fill=NAME,
           anchor='mm')
    d.text((30 + (y1 - y0 - 28) + (620 - 30 - (y1 - y0 - 28)) / 2, y0 + 108), f'WITH {UNIT[n][1].upper()}',
           font=CINZEL(24, 600), fill=SUBTITLE, anchor='mm')
    panel(d, (PW - 16 - 420, y0, PW - 16, y1))
    d.text((PW - 16 - 210, (y0 + y1) / 2), right, font=BRUSH(52), fill=NAME, anchor='mm')
    return d


def pu1_base(img, d, n, figures):
    backdrop(img, (0, 0, PW, PH))
    y = title_band(d, 'Subordinates', '9 units ready')
    y = paper_tabs(d, y + 6, 0)
    orders = 1868
    row_h = 140
    ledger(d, y, orders - 14, 0, NUMBER_ORDER, row_h, chosen=n, party=False, refill=False)
    panel(d, (0, orders, PW, BAR[0] - 16))
    d.text((32, orders + 50), 'Orders', font=BRUSH(46), fill=NAME, anchor='lm')
    d.text((32 + d.textlength('Orders', font=BRUSH(46)) + 20, orders + 54), '· ' + UNIT[n][1].upper(),
           font=CINZEL(30, 700), fill=TEXT, anchor='lm')
    budget(d, PW - 32, orders + 50)
    rep = (24, orders + 100, PW - 24, orders + 236)
    replenish(d, rep, figures)
    add_new_grid(d, 24, orders + 258, PW - 24, n)
    talk_end(img, n)
    return orders, row_h, rep


def pu1():
    img, d = phone()
    orders, row_h, _ = pu1_base(img, d, 1, None)
    reach(img, label=False, shade=False)
    d = ImageDraw.Draw(img)
    save(img, 'PU1', 'the ledger on top, the orders at the thumb', 'subordinates', side=[
        'The ledger as the deployment\ntable\'s (PD1): the same rows\nand tabs, the chosen unit\'s\nrow vermilion.',
        'A tap chooses the unit; its\norders below always show it.',
        f'The orders at the thumb:\nReplenish ({mm(136)}), Add New\nTroops ({mm(128)} buttons; greyed\nabove the '
        'budget, its reason\non a long press).',
        'Talk (with the unit\'s face) and\nEnd in the bar, End at the\nright thumb.',
        'Rance is full: Replenish greyed,\n"full" as on the PC.'])


def pu1q():
    img, d = phone()
    orders, row_h, rep = pu1_base(img, d, 12, '+1,191 troops · 2,300K')
    cw, chh = 760, 316
    box = (rep[0] + 40, rep[3] + 26, rep[0] + 40 + cw, rep[3] + 26 + chh)
    panel(d, box, fill=(30, 22, 16))
    d.polygon([(box[0] + 120, box[1] + 2), (box[0] + 160, box[1] - 26), (box[0] + 200, box[1] + 2)], fill=GOLD)
    d.text((box[0] + 44, box[1] + 64), 'Are you sure?', font=GOTHIC(48), fill=TEXT, anchor='lm')
    bw = (cw - 44 * 2 - 24) // 2
    red_button(d, (box[0] + 44, box[1] + 140, box[0] + 44 + bw, box[1] + 280), 'Yes', 48)
    row_box(d, (box[0] + 44 + bw + 24, box[1] + 140, box[2] - 44, box[1] + 280), 'No', size=48, face=BRUSH,
            centred=True)
    reach(img, label=False, shade=False)
    d = ImageDraw.Draw(img)
    save(img, 'PU1q', 'Replenish asks first (Haurein)', 'subordinates', side=[
        'Haurein (unit 12) chosen:\nReplenish +1,191 troops for\n2,300K (the gallery\'s figures).',
        'A troop order asks first in a\ncallout under its button (the\nhouse\'s C3, as on the PC);\n'
        'Haurein\'s row stays seen.',
        f'Yes / No {mm(140)} high, within\nthe thumb; No is also Back.',
        'Her Add New Troops prices:\n+400 and +800 the gallery\'s,\nthe cheaper derived (each step\nthree times '
        'the one before).'])


def pu2():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = title_band(d, 'Subordinates', '9 units ready')
    y = paper_tabs(d, y + 6, 2)
    row_h = 150
    ledger(d, y, BAR[0] - 20, 2, NUMBER_ORDER, row_h, chosen=1, party=False)
    sheet = 1720
    panel(d, (0, sheet, PW, BAR[0] - 16))
    fs = 210
    img.paste(unit_face(1, fs), (28, sheet + 28))
    d = ImageDraw.Draw(img)
    d.rectangle((28, sheet + 28, 28 + fs, sheet + 28 + fs), outline=GOLD, width=3)
    tx = 28 + fs + 32
    d.text((tx, sheet + 34), 'Rance', font=BRUSH(62), fill=NAME)
    d.text((tx, sheet + 116), 'LEAZAS KNIGHTS', font=CINZEL(28, 600), fill=SUBTITLE)
    d.text((tx, sheet + 160), '1,000/1,000', font=GOTHIC(40), fill=TEXT)
    strength_bar(d, tx, sheet + 212, 330, 1000, 1000, h=10, track=(60, 50, 40))
    d.text((PW - 32, sheet + 166), 'COST 300K', font=GOTHIC(36), fill=QUIET, anchor='ra')
    budget(d, PW - 32, sheet + 60, 36)
    rep = (24, sheet + 266, PW - 24, sheet + 396)
    replenish(d, rep)
    add_new_grid(d, 24, sheet + 418, PW - 24, 1)
    talk_end(img, 1, right='Return')
    reach(img, label=False, shade=False)
    d = ImageDraw.Draw(img)
    save(img, 'PU2', 'the ledger whole, a unit\'s sheet', 'subordinates', side=[
        'The ledger fills the screen\n(shown: the Commander tab);\nEnd in the bar while no unit\nis chosen.',
        'A tap on a row raises the unit\'s\nsheet over the ledger\'s foot,\nas a province\'s on the map\n(PM1c): its '
        'face, figures and\norders; Return where End was.',
        'More of the ledger while no\nsheet is up; one tap more\nthan PU1 per unit.'])


def p3():
    pd1(); pd2(); pd2d(); pd3()
    pu1(); pu1q(); pu2()


# --- round P4: the enemy attack and the week report upright (2026-10-27) ---------------------------------------------

ATTACK_DUMP = 'enemy-attack-scouted.png'
# The rebels on Ys (5) as the gallery's enemy-attack-scouted shows them: name, place, type, troops, the face's cell.
ATTACKERS = {1: ('Ex', '—', 'none', (842, 238)), 2: ('Sakanaku', 'Leazas Soldiers', '820', (842, 350)),
             3: ('Dodge', 'Leazas Soldiers', '336', (1100, 238)), 4: ('Jibul', 'Leazas Soldiers', '400', (1100, 350))}
MARIS_LINES = ["King Rance, if we don't act,", 'Ys will be', 'occupied.', 'What should we do?']
YS_FIGURES = [('Walls in a city battle', '+4%'), ('City battle limit', '330'), ('Plains battle limit', '5,000'),
              ('Revenue', '800K GOLD')]


def cg_face(cg, size, crop=(24, 7, 144)):
    """A face CG's usual square (UnitFaces.Default): Maris 6, Kanami 9."""
    x, y, s = crop
    im = np.array(Image.open(os.path.join(EXTRACT, 'cg_png', f'CG{cg:04d}.png')).convert('RGB'))
    im[(im[..., 0] > 200) & (im[..., 1] < 60) & (im[..., 2] > 200)] = (52, 58, 74)
    return Image.fromarray(im).crop((x, y, x + s, y + s)).resize((size, size), Image.LANCZOS)


def dump_face(xy, size):
    x, y = xy
    return dump(TOUCH, ATTACK_DUMP).crop((x + 1, y + 1, x + 87, y + 87)).resize((size, size), Image.LANCZOS)


def framed(img, pic, xy):
    img.paste(pic, xy)
    ImageDraw.Draw(img).rectangle((xy[0], xy[1], xy[0] + pic.width, xy[1] + pic.height), outline=DIM_GOLD, width=2)


def attacker_card(img, box, place):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    name, kind, troops, cell = ATTACKERS[place]
    d.rectangle(box, fill=ROW, outline=DIM_GOLD, width=2)
    fs = min(y1 - y0 - 24, 200)
    framed(img, dump_face(cell, fs), (x1 - 12 - fs, y0 + 12))
    d = ImageDraw.Draw(img)
    badge(d, x0 + 40, y0 + 44, place)
    d.text((x0 + 80, y0 + 44), name, font=GOTHIC(42), fill=TEXT, anchor='lm')
    d.text((x0 + 22, y0 + 92), kind, font=fit(d, kind, GOTHIC, 30, x1 - x0 - fs - 50), fill=QUIET)
    d.line([(x0 + 20, y1 - 22), (x1 - fs - 30, y1 - 22)], fill=DIM_GOLD, width=1)
    d.text((x0 + 22, y1 - 30), 'TROOPS', font=CINZEL(22, 600), fill=SUBTITLE, anchor='ld')
    d.text((x1 - fs - 30, y1 - 28), troops, font=GOTHIC(38), fill=TEXT, anchor='rd')


def flag_icon(img, xy, size, source='enemy'):
    box = (54, 86, 90, 122) if source == 'enemy' else (374, 197, 412, 235)
    pic = dump(TOUCH, ATTACK_DUMP if source == 'enemy' else 'week-report.png').crop(box)
    img.paste(pic.resize((size, size), Image.LANCZOS), xy)


def danger_button(d, box, label, sub=None):
    """Abandon City: dark red with a vermilion frame and a white flag (the owner's P1)."""
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=(54, 18, 14), outline=(196, 70, 50), width=3)
    d.rectangle((x0 + 7, y0 + 7, x1 - 7, y1 - 7), outline=(120, 40, 30), width=1)
    cy = (y0 + y1) / 2
    d.line([(x0 + 36, cy - 26), (x0 + 36, cy + 26)], fill=(236, 226, 210), width=4)
    d.polygon([(x0 + 38, cy - 26), (x0 + 72, cy - 18), (x0 + 38, cy - 6)], fill=(236, 226, 210))
    d.text((x0 + 92 + (x1 - x0 - 100) / 2, cy), label, font=fit(d, label, BRUSH, 38, x1 - x0 - 110),
           fill=(236, 150, 110), anchor='mm')


def two_line_button(d, box, title, sub, red=False):
    x0, y0, x1, y1 = box
    if red:
        red_button(d, box, '')
    else:
        panel(d, box)
    d.text(((x0 + x1) / 2, y0 + 56), title, font=fit(d, title, BRUSH, 44, x1 - x0 - 40), fill=NAME, anchor='mm')
    d.text(((x0 + x1) / 2, y1 - 38), sub, font=GOTHIC(28), fill=TEXT if red else QUIET, anchor='mm')


# The answers in two rows: Abandon City (top left, farthest from the thumb) and Ninja Scout, then Intercept in the bar.
ROW1 = BAR[0] - 168
MARIS_TOP = ROW1 - 20 - 330
ANSWERS = ((16, PW // 2 - 8), (PW // 2 + 8, PW - 16))


def attack_answers(d, ninjas='1 of 1 free'):
    danger_button(d, (ANSWERS[0][0], ROW1 + 10, ANSWERS[0][1], ROW1 + 138), 'Abandon City')
    two_line_button(d, (ANSWERS[1][0], ROW1, ANSWERS[1][1], ROW1 + 148), 'Ninja Scout', ninjas)
    two_line_button(d, (16, BAR[0], PW - 16, BAR[1]), 'Intercept', 'choose the units', red=True)


def maris(img, box, size=42):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    panel(d, box)
    fs = min(170, y1 - y0 - 40)
    framed(img, cg_face(6, fs), (x0 + 24, y0 + 20))
    d = ImageDraw.Draw(img)
    tx = x0 + 24 + fs + 30
    d.text((tx, y0 + 26), 'MARIS', font=CINZEL(28, 600), fill=SUBTITLE)
    for i, line in enumerate(MARIS_LINES):
        d.text((tx, y0 + 66 + i * size * 1.22), line, font=GOTHIC(size), fill=TEXT)


def attack_head(img, d, y=TOP + 6):
    flag_icon(img, (24, y + 6), 78)
    d = ImageDraw.Draw(img)
    d.text((120, y), 'Enemy Attack', font=BRUSH(60), fill=NAME)
    d.text((PW - 30, y + 22), DATE, font=CINZEL(30, 700), fill=NAME, anchor='ra')
    d.text((30, y + 104), 'The Leazas Rebel Army appeared around Ys.', font=GOTHIC(40), fill=TEXT)
    divider(d, y + 172)
    return y + 186


def pa1_base(img, d):
    backdrop(img, (0, 0, PW, PH))
    y = attack_head(img, d)
    d = ImageDraw.Draw(img)
    panel(d, (16, y, PW - 16, y + 540))
    d.text((44, y + 30), 'DEFENDING · YS', font=CINZEL(28, 600), fill=SUBTITLE)
    pic = dump(TOUCH, ATTACK_DUMP).crop((120, 204, 416, 400)).resize((600, 397), Image.LANCZOS)
    framed(img, pic, (40, y + 84))
    d = ImageDraw.Draw(img)
    fx = 670
    for i, (label, value) in enumerate(YS_FIGURES):
        ry = y + 100 + i * 88
        d.text((fx, ry), label, font=GOTHIC(31), fill=QUIET)
        d.text((PW - 44, ry + 36), value, font=GOTHIC(38), fill=TEXT, anchor='ra')
        d.line([(fx, ry + 82), (PW - 44, ry + 82)], fill=DIM_GOLD, width=1)
    y += 540
    d.text((PW / 2, y + 40), 'VS', font=BRUSH(56), fill=GOLD, anchor='mm')
    y += 80
    top = y
    panel(d, (16, y, PW - 16, MARIS_TOP - 20))
    d.text((44, y + 30), 'ATTACKING · LEAZAS REBEL ARMY', font=CINZEL(28, 600), fill=SUBTITLE)
    cw, ch = (PW - 64 - 14) // 2, 270
    xs = (32, 32 + cw + 14)
    y += 84
    for label, places, way in (('FRONT ROW', (1, 2), 'up'), ('BACK ROW', (3, 4), None)):
        d.text((PW / 2, y + 16), label, font=CINZEL(26, 700), fill=SUBTITLE, anchor='mm')
        if way:                                 # the rebels face Ys, above them
            arrow_mark(d, PW / 2 + 100, y + 16, way, 8, SUBTITLE)
        y += 40
        for x, place in zip(xs, places):
            attacker_card(img, (x, y, x + cw, y + ch), place)
        d = ImageDraw.Draw(img)
        y += ch + 14
    d.text((44, y + 30), 'COMMANDERS', font=CINZEL(24, 600), fill=SUBTITLE, anchor='lm')
    d.text((250, y + 30), '4', font=GOTHIC(38), fill=TEXT, anchor='lm')
    d.text((PW - 44, y + 30), '1,556', font=GOTHIC(38), fill=TEXT, anchor='rm')
    d.text((PW - 44 - d.textlength('1,556', font=GOTHIC(38)) - 16, y + 30), 'TOTAL TROOPS', font=CINZEL(24, 600),
           fill=SUBTITLE, anchor='rm')
    d.line([(44, y + 64), (PW - 44, y + 64)], fill=DIM_GOLD, width=1)
    d.text((44, y + 76), "Kanami's report", font=GOTHIC(30), fill=QUIET)
    y = MARIS_TOP
    maris(img, (16, y, PW - 16, ROW1 - 20))
    d = ImageDraw.Draw(img)
    attack_answers(d)
    return d


def pa1():
    img, d = phone()
    pa1_base(img, d)
    reach(img, label=False, shade=False)
    save(img, 'PA1', 'a screen of its own, Ys over the rebels', 'attack', side=[
        'The PC\'s screen (B) stacked:\nthe news and the date; Ys, its\npicture and figures; the rebels\n'
        'under it, their front row facing\nup at Ys, as the battle\'s (PB3)\nenemy faces our army.',
        'The rebels\' cards 35 × 15.3 mm,\nnames 2.4 mm; after Kanami\'s\nscouting (the gallery\'s), the\n'
        'places filled.',
        'Maris\'s question in her own\nbreaks, 2.4 mm.',
        'The answers in two rows: Abandon\nCity (the danger button, a white\nflag) top left, farthest from the\n'
        'thumb, Ninja Scout beside it;\nIntercept the bar\'s width at\nthe thumb. 7.3 and 8.4 mm.'])


def pa1n():
    img, d = phone()
    d = pa1_base(img, d)
    x0, x1 = 440, PW - 16
    rh = 150
    y1 = ROW1 - 30
    y0 = y1 - rh - 110
    panel(d, (x0, y0, x1, y1), fill=(30, 22, 16))
    cx = (ANSWERS[1][0] + ANSWERS[1][1]) / 2
    d.polygon([(cx - 24, y1 - 2), (cx + 24, y1 - 2), (cx, y1 + 22)], fill=GOLD)
    d.text((x0 + 30, y0 + 46), 'SEND TO SCOUT', font=CINZEL(28, 600), fill=SUBTITLE, anchor='lm')
    row = (x0 + 24, y0 + 86, x1 - 24, y0 + 86 + rh)
    d.rectangle(row, fill=ROW, outline=DIM_GOLD, width=2)
    framed(img, cg_face(9, rh - 24), (row[0] + 12, row[1] + 12))
    d = ImageDraw.Draw(img)
    d.text((row[0] + rh + 20, row[1] + 46), 'KANAMI', font=CINZEL(40, 700), fill=NAME, anchor='lm')
    d.text((row[0] + rh + 20, row[1] + 102), 'free this week', font=GOTHIC(32), fill=QUIET, anchor='lm')
    reach(img, label=False, shade=False)
    save(img, 'PA1n', 'Ninja Scout\'s list', 'attack', side=[
        'Ninja Scout opens its list above\nthe button (the owner\'s B4): a\nrow per ninja in service, the\n'
        f'spent ones dimmed; rows {mm(rh)},\na tap on a free one sends her.',
        'Shown before the scouting (the\nplaces would still be "??" —\nhere the gallery\'s filled ones).',
        'Abandon City asks first the\nsame way, its callout above it.'])


def pa2():
    img, d = phone()
    box = (0, 300, PW, 1250)
    scale = 2.64
    mx, my = PROV_POINTS[5]
    w, h = (box[2] - box[0]) / scale, (box[3] - box[1]) / scale
    x0 = max(0, min(1920 - w, mx - w / 2))
    y0 = max(0, min(800 - h, my - h * 0.45))
    draw_map(img, box, x0, y0, scale, 36, ring=5)
    d = ImageDraw.Draw(img)
    panel(d, (0, TOP - 20, PW, 300))
    flag_icon(img, (24, TOP + 10), 70)
    d = ImageDraw.Draw(img)
    d.text((112, TOP + 4), 'Enemy Attack', font=BRUSH(56), fill=NAME)
    d.text((PW - 30, TOP + 22), DATE, font=CINZEL(30, 700), fill=NAME, anchor='ra')
    d.text((30, TOP + 100), 'The Leazas Rebel Army appeared around Ys.', font=GOTHIC(38), fill=TEXT)
    sheet = 1250
    panel(d, (0, sheet, PW, BAR[0] - 16))
    y = sheet + 24
    d.text((32, y + 20), 'YS', font=BRUSH(44), fill=NAME, anchor='lm')
    for i, (label, value) in enumerate(YS_FIGURES):
        x = 130 + (i % 2) * 540
        ry = y + (i // 2) * 52
        d.text((x, ry + 20), label, font=GOTHIC(28), fill=QUIET, anchor='lm')
        d.text((x + 500, ry + 20), value, font=GOTHIC(32), fill=TEXT, anchor='rm')
    y += 120
    divider(d, y)
    y += 20
    d.text((32, y + 24), 'LEAZAS REBEL ARMY', font=CINZEL(28, 700), fill=SUBTITLE, anchor='lm')
    d.text((PW - 32, y + 24), '4 commanders · 1,556 troops', font=GOTHIC(30), fill=TEXT, anchor='rm')
    y += 56
    rh = 118
    for place in (1, 2, 3, 4):
        name, kind, troops, cell = ATTACKERS[place]
        r = (24, y, PW - 24, y + rh)
        d.rectangle(r, fill=ROW, outline=DIM_GOLD, width=2)
        framed(img, dump_face(cell, rh - 16), (r[0] + 8, y + 8))
        d = ImageDraw.Draw(img)
        badge(d, r[0] + rh + 40, y + rh / 2, place)
        d.text((r[0] + rh + 84, y + rh / 2), name, font=GOTHIC(40), fill=TEXT, anchor='lm')
        d.text((560, y + rh / 2), kind, font=GOTHIC(30), fill=QUIET, anchor='lm')
        d.text((PW - 44, y + rh / 2), troops, font=GOTHIC(38), fill=TEXT, anchor='rm')
        d.text((PW - 150, y + rh / 2), 'FRONT' if place <= 2 else 'BACK', font=CINZEL(22, 600), fill=SUBTITLE,
               anchor='rm')
        y += rh + 8
    maris(img, (16, y + 10, PW - 16, ROW1 - 20), size=38)
    d = ImageDraw.Draw(img)
    attack_answers(d)
    reach(img, label=False, shade=False)
    save(img, 'PA2', 'over the map: Ys ringed, the attack as a sheet', 'attack', side=[
        'The map (PM1\'s scale) with Ys\nringed, under the news; the\nattack rises as a sheet, as a\n'
        'province\'s does (PM1c).',
        'Ys\'s figures in two columns; the\nrebels as rows (face, place,\nname, type, row, troops), 6.7 mm.',
        'Maris\'s question and the\nanswers as PA1\'s.',
        'Smaller type than PA1 (the\nfigures 1.8 mm); the map says\nwhere at a glance.'])


# --- the week report ---

REPORT = [('', 'BUDGET', 'last week', '102,700', TEXT), ('+', 'REVENUE', 'provinces', '+20,200', (150, 196, 110)),
          ('−', 'AL CHURCH', 'special expense', '−6,500', (222, 110, 90))]


def week_card(img, box, triangle_at=None):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    panel(d, box)
    flag_icon(img, (x0 + 34, y0 + 36), 74, 'report')
    d = ImageDraw.Draw(img)
    d.text((x0 + 130, y0 + 28), 'Leazas Army Phase', font=fit(d, 'Leazas Army Phase', BRUSH, 62, x1 - x0 - 160),
           fill=NAME)
    d.text((x0 + 132, y0 + 110), DATE, font=CINZEL(32, 700), fill=NAME)
    divider(d, y0 + 176, x0 + 30, x1 - 30)
    y = y0 + 200
    d.text((x0 + 34, y), 'TREASURY', font=CINZEL(28, 600), fill=SUBTITLE)
    y += 50
    for sign, label, sub, value, colour in REPORT:
        d.text((x0 + 40, y + 40), sign, font=GOTHIC(34), fill=QUIET, anchor='lm')
        d.text((x0 + 80, y + 40), label, font=CINZEL(34, 600), fill=TEXT, anchor='lm')
        d.text((x0 + 92 + d.textlength(label, font=CINZEL(34, 600)), y + 44), sub, font=CINZEL(24, 600),
               fill=SUBTITLE, anchor='lm')
        d.text((x1 - 40, y + 44), 'K GOLD', font=CINZEL(24, 600), fill=SUBTITLE, anchor='rm')
        d.text((x1 - 150, y + 40), value, font=GOTHIC(44), fill=colour, anchor='rm')
        for xx in range(x0 + 30, x1 - 30, 14):
            d.line([(xx, y + 82), (xx + 6, y + 82)], fill=DIM_GOLD, width=1)
        y += 86
    y += 12
    b = (x0 + 30, y, x1 - 30, y + 116)
    d.rectangle(b, fill=(30, 22, 16), outline=GOLD, width=2)
    d.text((b[0] + 30, y + 58), 'BUDGET', font=CINZEL(34, 700), fill=NAME, anchor='lm')
    d.text((b[2] - 30, y + 64), 'K GOLD', font=CINZEL(24, 600), fill=SUBTITLE, anchor='rm')
    d.text((b[2] - 140, y + 58), '116,400', font=BRUSH(62), fill=NAME, anchor='rm')
    y += 150
    d.text((x0 + 34, y), 'TROOPS', font=CINZEL(28, 600), fill=SUBTITLE)
    y += 50
    b = (x0 + 30, y, x1 - 30, y + 100)
    d.rectangle(b, fill=(30, 22, 16), outline=DIM_GOLD, width=2)
    d.text((b[0] + 30, y + 50), 'Replenished Troops', font=CINZEL(34, 600), fill=TEXT, anchor='lm')
    d.text((b[2] - 30, y + 50), '+1,385', font=BRUSH(48), fill=(150, 196, 110), anchor='rm')
    tx, ty = triangle_at or (x1 - 60, y1 - 50)
    triangle(d, tx, ty, 24)
    return y + 100


def report_map(img):
    scale = (BAR[0] - 12) / 800
    draw_map(img, (0, 0, PW, BAR[0] - 12), 1290, 0, scale, 36, dim=110)
    d = ImageDraw.Draw(img)
    d.rectangle((0, BAR[0] - 12, PW, PH), fill=BACKDROP)
    bottom_bar(d)
    return d


def pw1():
    img, d = phone()
    report_map(img)
    h = 930
    y0 = (TOP + BAR[0]) // 2 - h // 2
    week_card(img, (16, y0, PW - 16, y0 + h))
    reach(img, label=False, shade=False)
    save(img, 'PW1', 'the report over the map, in the middle', 'report', side=[
        'The week report as on the PC,\nat the screen\'s width over the\nmap: the banner, the treasury\n'
        'as an account, the budget, the\ntroops replenished.',
        'The banner 3.5 mm, the account\'s\nfigures 2.5 mm, the budget\n3.5 mm.',
        'A tap anywhere goes on (the\ntriangle asks for it); the bar\'s\nMenu, Log and Skip as in the\nscenes.',
        'The thieves\' week: the banner\nalone, the same card.'])


def pw2():
    img, d = phone()
    report_map(img)
    h = 930
    y1 = BAR[0] - 24
    week_card(img, (0, y1 - h, PW, y1), triangle_at=(PW - 70, y1 - 46))
    reach(img, label=False, shade=False)
    save(img, 'PW2', 'the report as a sheet at the thumb', 'report', side=[
        'The same card as a sheet over\nthe map\'s foot, as the command\npanel rises in PM1b; the map\nabove '
        'stays to be seen.',
        'The triangle at the right thumb;\na tap anywhere goes on.',
        'Where the command panel will\nrise next, so the eye stays\nlow from the report to the\ncommands.'])


def p4():
    pa1(); pa1n(); pa2()
    pw1(); pw2()


def main():
    global MAP, PROV_NAMES, PROV_POINTS
    if ROUND == 'P3':
        p3()
        overviews()
        return
    MAP, PROV_NAMES, PROV_POINTS = build_map()
    if ROUND == 'P4':
        p4()
        overviews()
        return
    if GALLERY_MODE:
        m1()
        pb3(); pb3r(); pb3x()
        manifest = {'written': datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S'), 'palette': 'lacquer',
                    'font': 'BizUdGothic', 'touch': True, 'mockup': True, 'entries': DUMPED}
        with open(os.path.join(OUT, 'manifest.json'), 'w', encoding='utf-8') as f:
            json.dump(manifest, f, ensure_ascii=False, indent=2)
        return
    p0()
    d1(); d2(); d2b(); d2m(); d3()
    m1(); m2()
    b1(); b2()
    pb3(); pb3r(); pb3x()
    overviews()


main()
