"""Mock-ups of more room between the touch mode's Skip and Log (owner, 2026-10-25: in the gallery's Touch tab the two
cells sit too close together; docs/ui/touch.md, "The dialogue buttons, round 2"), drawn over the screen gallery's dumps
(docs/ui/gallery.md): the PC pass's windows, which have no drawer, and the touch pass's 131 with its dock.

Every variant keeps Skip where it is (its cell 50x84 at y 592, the same over every window and on 131's dock, the owner's
B1/B2) and grows the drawer downwards, so the room never moves Skip and still fits 131's dock (its frame y 588-790).

  powershell -ExecutionPolicy Bypass -File tools/ui/gallery.ps1 -Touch
  python -I tools/ui/mock_drawer_gap.py <gallery dir> <out dir> < /dev/null

(Pillow; godot/fonts.) <gallery dir> is screen-gallery at the root unless KICHIKUOU_UI_GALLERY_DIR says otherwise; keep
<out dir> outside the repository. Drawn after godot/Strategy/SkipDrawer, in the screen's 1280x800 pixels.
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

CELL_W, CELL_H, CELL_TOP = 50, 84, 592
DOCK = (0, 588, 56, 790)                     # 131's dock in the touch pass (frame), measured


def font(name, size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), int(size * S))


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)


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


def caption(f, lines, y=300):
    for i, t in enumerate(lines):
        note(f, 80, y + i * 24, t, size=17 if i == 0 else 15)


def measure(f, x, y0, y1, label):
    lay, d = f.draw()
    d.line(sc(x, y0, x, y1), fill=NOTE, width=S)
    d.line(sc(x - 4, y0, x + 4, y0), fill=NOTE, width=S)
    d.line(sc(x - 4, y1, x + 4, y1), fill=NOTE, width=S)
    f.add(lay)
    note(f, x + 8, (y0 + y1) / 2, label, anchor='lm', size=13)


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


def blank_dock(f):
    """The touch pass's dock and its cells painted out (the screen's black left of 131)."""
    lay, d = f.draw()
    d.rectangle(sc(0, DOCK[1] - 4, DOCK[2] + 8, DOCK[3] + 2), fill=(0, 0, 0, 255))
    f.add(lay)


def divider(f, y, x0=8, x1=CELL_W - 8):
    lay, d = f.draw()
    d.line(sc(x0, y, x1, y), fill=DIM_GOLD, width=S)
    m = CELL_W / 2
    d.polygon(sc(m, y - 3, m + 3, y, m, y + 3, m - 3, y), fill=GOLD)
    f.add(lay)


def cell(f, top, h, kind):
    """A cell's icon at 32 px from its top and its label at 64 (SkipDrawer), scaled with a shorter cell."""
    lay, d = f.draw()
    k = h / CELL_H
    cx, cy, size = CELL_W / 2, top + 32 * k, 24
    if kind == 'skip':
        for dx in (-size * 0.55, size * 0.05):
            d.polygon(sc(cx + 2 + dx, cy - size / 2, cx + 2 + dx + size * 0.6, cy, cx + 2 + dx, cy + size / 2), fill=GOLD)
        label = 'Skip'
    else:
        for i, w in enumerate((1, 1, 0.6)):
            y = cy - size * 0.4 + i * size * 0.4
            d.rounded_rectangle(sc(cx - size / 2, y - size * 0.08, cx - size / 2 + size * w, y + size * 0.08),
                                radius=size * 0.08 * S, fill=GOLD)
        label = 'Log'
    d.text((cx * S, (top + 64 * k + 1) * S), label, font=GOTHIC(13), fill=QUIET, anchor='mm')
    f.add(lay)


# Each variant: (name, cell height, room between the cells, separate plates, the caption's lines).
VARIANTS = [
    ('G0', CELL_H, 0, False, ['G0 - now: the cells touch, a gold rule between them']),
    ('G1', CELL_H, 16, False, ['G1 - one drawer, 16 px (3.2 mm) between the cells, the rule in the middle of it',
                               'cells as now (50x84); the drawer 16 px longer, downwards']),
    ('G2', 80, 28, False, ['G2 - one drawer, 28 px (5.7 mm) between the cells, the rule in the middle of it',
                           'cells 4 px lower (50x80, 16.2 mm) so it still fits 131\'s dock']),
    ('G3', CELL_H, 16, True, ['G3 - two plates, 16 px (3.2 mm) of the scene between them',
                              'cells as now; each a button of its own; over 131 two docks instead of one']),
]


def cells(cell_h, gap):
    skip = (CELL_TOP, CELL_TOP + cell_h)
    log = (skip[1] + gap, skip[1] + gap + cell_h)
    return skip, log


def draw_variant(f, cell_h, gap, separate, docked):
    skip, log = cells(cell_h, gap)
    if separate:
        plate(f, (0, skip[0] - 2, CELL_W + 2, skip[1] + 2))
        plate(f, (0, log[0] - 2, CELL_W + 2, log[1] + 2))
    else:
        plate(f, DOCK if docked else (0, skip[0] - 2, CELL_W + 2, log[1] + 2))
        divider(f, (skip[1] + log[0]) / 2)
    cell(f, skip[0], cell_h, 'skip')
    cell(f, log[0], cell_h, 'log')
    return skip, log


def main():
    pc = os.path.join(GALLERY, 'lacquer', 'current')
    touch = os.path.join(GALLERY, 'touch', 'current')
    frames = []
    for name, cell_h, gap, separate, lines in VARIANTS:
        for docked in (False, True):
            if name == 'G0':
                f = Frame(os.path.join(touch, 'message-cg-bar-speech.png' if docked else 'message-speech-right.png'))
                skip, log = cells(cell_h, gap)
            else:
                f = Frame(os.path.join(touch, 'message-cg-bar-speech.png') if docked
                          else os.path.join(pc, 'message-speech-right.png'))
                if docked:
                    blank_dock(f)
                skip, log = draw_variant(f, cell_h, gap, separate, docked)
            if gap:
                measure(f, CELL_W + 14, skip[1], log[0], f'{gap} px')
            caption(f, lines + (['over 131: the window drawn smaller beside the dock'] if docked else []))
            out = f'{name}-{"131" if docked else "speech"}.png'
            img = f.final()
            img.save(os.path.join(OUT, out))
            frames.append((name, docked, img))
    sheet(frames)


def sheet(frames):
    """The drawers side by side, enlarged twice: a speech window's on top, 131's under them."""
    crop = (0, 560, 150, 800)
    z = 2
    cw, ch = (crop[2] - crop[0]) * z, (crop[3] - crop[1]) * z
    pad, head = 16, 34
    names = [v[0] for v in VARIANTS]
    out = Image.new('RGB', (len(names) * (cw + pad) + pad, 2 * (ch + head) + pad), (30, 30, 30))
    d = ImageDraw.Draw(out)
    fnt = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf'), 18)
    for name, docked, img in frames:
        i, row = names.index(name), 1 if docked else 0
        x, y = pad + i * (cw + pad), row * (ch + head) + head
        out.paste(img.crop(crop).resize((cw, ch), Image.LANCZOS), (x, y))
        d.text((x, y - 26), f'{name} - {"131" if docked else "speech"}', font=fnt, fill=NOTE)
    out.save(os.path.join(OUT, 'sheet.png'))


main()
