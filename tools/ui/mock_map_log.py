"""Mock-ups of the touch mode's way to the message log from the map (owner, 2026-10-26: on the PC the wheel up opens it
on COMMAND's waiting map; docs/ui/messages.md, §5 step 6), which a finger has no wheel for — docs/ui/touch.md, "The
message log from the map, round L". Drawn over the screen gallery's touch pass (docs/ui/gallery.md): the map's wait with
its plates and the Commands button (map-markers), the command panel on its rails (command-menu).

  powershell -ExecutionPolicy Bypass -File tools/ui/gallery.ps1 -Touch
  python -I tools/ui/mock_map_log.py <gallery dir> <out dir> < /dev/null

(Pillow; godot/fonts.) <gallery dir> is screen-gallery at the root unless KICHIKUOU_UI_GALLERY_DIR says otherwise; keep
<out dir> outside the repository. In the screen's 1280x800 pixels: the drawer after godot/Strategy/SkipDrawer and
Original.Content/Strategy/SkipButtons, the button after godot/Strategy/CommandsButton (its frame cut from the dump), the
row after godot/Strategy/CommandMenuView's right rail.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

GALLERY, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H = 1280, 800
TABLET_MM = 0.203

PANEL = (21, 16, 12)
GOLD, DIM_GOLD = (196, 160, 86), (110, 86, 46)
QUIET = (176, 160, 130)
NOTE = (255, 92, 160)

CELL_W, CELL_H = 50, 84
SKIP_TOP, LOG_TOP = 592, 696                 # the cells (SkipButtons, the owner's G3)
COMMANDS = (1044, 712, 1264, 784)            # CommandsButton: 220x72, 16 in from the picture's right and bottom
LOG_W = 132                                  # L3's button, beside it
ROW_X, ROW_W, ROW_H, PITCH = 1044, 220, 46, 54
RAIL_TOP, SAVE_TOP = 340, 548                # the right rail's frame top and its Save row (CommandMenuView)


def font(name, size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), int(size * S))


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)


def mm(px):
    return f'{px * TABLET_MM:.1f} mm'


def sc(*v):
    return [round(a * S) for a in v]


class Frame:
    def __init__(self, path):
        base = Image.open(path).convert('RGB')
        self.img = base.resize((W * S, H * S), Image.LANCZOS).convert('RGBA')

    def draw(self):
        lay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay):
        self.img = Image.alpha_composite(self.img, lay)

    def final(self):
        return self.img.resize((W, H), Image.LANCZOS).convert('RGB')


def note(f, x, y, t, anchor='la', size=15):
    lay, d = f.draw()
    fnt = GOTHIC(size)
    l, t0, r, b = d.textbbox((x * S, y * S), t, font=fnt, anchor=anchor)
    d.rectangle((l - 5 * S, t0 - 3 * S, r + 5 * S, b + 3 * S), fill=(0, 0, 0, 210))
    d.text((x * S, y * S), t, font=fnt, fill=NOTE, anchor=anchor)
    f.add(lay)


def caption(f, lines, x=300, y=40):
    for i, t in enumerate(lines):
        note(f, x, y + i * 24, t, size=17 if i == 0 else 15)


def ring(f, r, pad=6):
    """A pink outline round what is new."""
    lay, d = f.draw()
    x0, y0, x1, y1 = r
    d.rounded_rectangle(sc(x0 - pad, y0 - pad, x1 + pad, y1 + pad), radius=12 * S, outline=NOTE, width=2 * S)
    f.add(lay)


def plate(f, r):
    """SkipDrawer.DrawPlate: lacquer glued to the left edge, gold on three sides, the right corners rounded, a dim
    inner line; its shadow under it."""
    x0, y0, x1, y1 = r
    lay, d = f.draw()
    d.rounded_rectangle(sc(x0 - 12 + 2, y0 + 4, x1 + 2, y1 + 4), radius=10 * S, fill=(0, 0, 0, 90))
    f.add(lay.filter(ImageFilter.GaussianBlur(4 * S)))
    lay, d = f.draw()
    d.rounded_rectangle(sc(x0 - 12, y0, x1, y1), radius=10 * S, fill=PANEL + (255,), outline=GOLD, width=2 * S)
    d.rounded_rectangle(sc(x0 - 12, y0 + 4.5, x1 - 4.5, y1 - 4.5), radius=6 * S, outline=DIM_GOLD, width=S)
    f.add(lay)


def log_icon(d, cx, cy, size=24, colour=GOLD):
    """SkipDrawer.Lines: three lines of text, the last shorter."""
    for i, w in enumerate((1, 1, 0.6)):
        y = cy - size * 0.4 + i * size * 0.4
        d.rounded_rectangle(sc(cx - size / 2, y - size * 0.08, cx - size / 2 + size * w, y + size * 0.08),
                            radius=size * 0.08 * S, fill=colour)


def balloon_icon(d, cx, cy, colour=GOLD):
    """L3's icon: a speech balloon with two lines in it (the drawer's three lines would repeat Commands' bars)."""
    x0, y0, x1, y1 = cx - 14, cy - 11, cx + 14, cy + 7
    d.polygon(sc(x0 + 5, y1 - 1, x0 + 4, y1 + 7, x0 + 12, y1 - 1), fill=colour)
    d.rounded_rectangle(sc(x0, y0, x1, y1), radius=5 * S, outline=colour, width=round(2.5 * S))
    for i, w in enumerate((16, 10)):
        y = cy - 5.5 + i * 6
        d.rounded_rectangle(sc(x0 + 6, y - 1.2, x0 + 6 + w, y + 1.2), radius=1.2 * S, fill=colour)


def cell(f, top, kind):
    """A cell's icon at 32 px from its top and its label at 64 (SkipDrawer)."""
    lay, d = f.draw()
    cx, cy, size = CELL_W / 2, top + 32, 24
    if kind == 'skip':
        for dx in (-size * 0.55, size * 0.05):
            d.polygon(sc(cx + 2 + dx, cy - size / 2, cx + 2 + dx + size * 0.6, cy, cx + 2 + dx, cy + size / 2), fill=GOLD)
        label = 'Skip'
    else:
        log_icon(d, cx, cy, size)
        label = 'Log'
    d.text((cx * S, (top + 65) * S), label, font=GOTHIC(13), fill=QUIET, anchor='mm')
    f.add(lay)


def drawer(f, skip):
    if skip:
        plate(f, (0, SKIP_TOP - 2, CELL_W + 2, SKIP_TOP + CELL_H + 2))
        cell(f, SKIP_TOP, 'skip')
    plate(f, (0, LOG_TOP - 2, CELL_W + 2, LOG_TOP + CELL_H + 2))
    cell(f, LOG_TOP, 'log')


def brightest(img, box):
    """The lightest colour in box (2x pixels): a label's ink."""
    region = img.crop(box).convert('RGB')
    pixels = getattr(region, 'get_flattened_data', region.getdata)()
    return max(pixels, key=lambda c: c[0] + c[1] + c[2])


def log_button(f):
    """L3: CommandsButton's frame cut from the dump and narrowed to LOG_W (its two ends and a column of its middle,
    clear of the icon and the label), placed left of it on the same line; a balloon and "Log" in its brush face."""
    x0, y0, x1, y1 = sc(*COMMANDS)
    src = f.img.crop((x0, y0, x1, y1))
    ink = brightest(src, sc(80, 20, 200, 52))
    end, w = 14 * S, LOG_W * S
    button = Image.new('RGBA', (w, src.height))
    button.paste(src.crop((0, 0, end, src.height)), (0, 0))
    button.paste(src.crop((56 * S, 0, 56 * S + 1, src.height)).resize((w - 2 * end, src.height)), (end, 0))
    button.paste(src.crop((src.width - end, 0, src.width, src.height)), (w - end, 0))
    left = COMMANDS[0] - 16 - LOG_W
    lay, d = f.draw()
    d.rounded_rectangle(sc(left + 2, COMMANDS[1] + 4, left + LOG_W + 2, COMMANDS[3] + 4), radius=6 * S, fill=(0, 0, 0, 110))
    f.add(lay.filter(ImageFilter.GaussianBlur(4 * S)))
    lay, d = f.draw()
    lay.paste(button, (left * S, y0))
    balloon_icon(d, left + 36, (COMMANDS[1] + COMMANDS[3]) / 2)
    d.text(((left + 60 + (LOG_W - 70) / 2) * S, (COMMANDS[1] + COMMANDS[3]) / 2 * S), 'Log', font=BRUSH(19), fill=ink, anchor='mm')
    f.add(lay)
    return (left, COMMANDS[1], left + LOG_W, COMMANDS[3])


def log_row(f):
    """L4: the right rail one row taller, Log after All Data — the rail's top (Turn END, Think, All Data) moved up a
    pitch, a row cut from Save's put in the room, its text Log."""
    band = f.img.crop(sc(1032, RAIL_TOP, 1280, SAVE_TOP))
    row = f.img.crop(sc(ROW_X, SAVE_TOP, ROW_X + ROW_W, SAVE_TOP + ROW_H))
    ink = brightest(row, sc(10, 8, 70, 38))
    back = row.getpixel((180 * S, 23 * S))
    f.img.paste(band, sc(1032, RAIL_TOP - PITCH)[:2])
    top = SAVE_TOP - PITCH
    f.img.paste(row, sc(ROW_X, top)[:2])
    lay, d = f.draw()
    d.rectangle(sc(ROW_X + 8, top + 6, ROW_X + 120, top + ROW_H - 6), fill=back)
    d.text(((ROW_X + 14) * S, (top + ROW_H / 2) * S), 'Log', font=GOTHIC(18), fill=ink, anchor='lm')
    f.add(lay)
    return (ROW_X, top, ROW_X + ROW_W, top + ROW_H)


VARIANTS = [
    ('L1', 'map-markers', ['L1 - the scenes\' drawer on the map: Skip over Log at the left edge, where the thumb finds them',
                           'in the scenes; Skip turns MsgSkip on before Turn END, as the menu bar\'s MsgSkip on the PC',
                           f'each cell 50x84 ({mm(50)} x {mm(84)}); a tap opens the log over the map, which waits',
                           'the drawer covers the map\'s left edge there (Rockearth\'s plate); a drag moves the map from under it']),
    ('L2', 'map-markers', ['L2 - Log alone at the left edge, in its scene place; Skip\'s place left to the map',
                           f'the cell 50x84 ({mm(50)} x {mm(84)}); the drawer whole again in the scenes']),
    ('L3', 'map-markers', ['L3 - a Log button beside Commands, in its lacquer and gold: both under the right thumb',
                           f'{LOG_W}x72 ({mm(LOG_W)} x {mm(72)}), 16 px left of Commands; nothing at the left edge',
                           'a speech balloon for its icon: the drawer\'s three lines would repeat Commands\' bars']),
    ('L4', 'command-menu', ['L4 - no button on the map: a Log row in the command panel\'s right rail, after All Data',
                            'two taps (Commands, Log); the log over the panel, the panel again when it closes',
                            'the rail one row taller upwards, as each link makes it']),
]


def main():
    touch = os.path.join(GALLERY, 'touch', 'current')
    frames = []
    for name, dump, lines in VARIANTS:
        f = Frame(os.path.join(touch, dump + '.png'))
        if name == 'L1':
            drawer(f, skip=True)
            ring(f, (0, SKIP_TOP - 2, CELL_W + 2, LOG_TOP + CELL_H + 2))
        elif name == 'L2':
            drawer(f, skip=False)
            ring(f, (0, LOG_TOP - 2, CELL_W + 2, LOG_TOP + CELL_H + 2))
        elif name == 'L3':
            ring(f, log_button(f))
        else:
            ring(f, log_row(f), pad=4)
        caption(f, lines, x=300 if name != 'L4' else 300, y=40 if name != 'L4' else 100)
        img = f.final()
        img.save(os.path.join(OUT, f'{name}.png'))
        frames.append((name, img))
    sheet(frames)


def sheet(frames, scale=0.5):
    """The four side by side at half size, two a row."""
    w, h = int(W * scale), int(H * scale)
    pad, head = 12, 30
    out = Image.new('RGB', (2 * (w + pad) + pad, 2 * (h + head) + pad), (30, 30, 30))
    d = ImageDraw.Draw(out)
    fnt = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf'), 18)
    for i, (name, img) in enumerate(frames):
        x, y = pad + (i % 2) * (w + pad), (i // 2) * (h + head) + head
        out.paste(img.resize((w, h), Image.LANCZOS), (x, y))
        d.text((x, y - 24), name, font=fnt, fill=NOTE)
    out.save(os.path.join(OUT, 'sheet.png'))


main()
