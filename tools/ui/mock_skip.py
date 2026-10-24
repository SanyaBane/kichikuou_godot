"""Mock-ups of the touch mode's Skip and Log buttons (owner, 2026-10-24: a large skip button at the bottom left, as
the map's Commands button is at the bottom right; then: a tap, not a hold, and a Log button beside it;
docs/ui/touch.md, "The Skip and Log buttons"), drawn over window dumps of the week-18 save on the tablet's frame
(1280x824, the menu bar included).

The dumps (a window; the runs move the system pointer):
  <Godot console exe> --path godot -- --load --auto --quit --seed 2 --saves user://saves-look
      --slots-from user://saves-week18 --until battle --interface new --shots <dump dir>
(00002-message-narration.png, 00057-message-speech-left.png, 00074-key.png: a battle's speech);
  <Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look-touch
      --slots-from user://saves-week18 --until round-2 --touch --shots <dump dir>
(00074-touch-rails.png: Rance's turn with the rails), and the 131 window under an event CG from the --harem look
(00028-message-cg-bar-speech.png, masked), copied into the same folder. Then, from the repository's root (Pillow;
godot/fonts):
  python -I tools/ui/mock_skip.py <dump dir> <out dir> < /dev/null
The images hold the units' portraits: keep <out dir> outside the repository.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

DUMPS, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H = 1280, 824
TABLET_MM = 0.203

# The steel palette's button (DeploymentPalette.Steel), as the Commands button draws it.
FILL, BORDER, LABEL, DIM_LABEL = (0x45, 0x48, 0x4f), (0xc9, 0xa8, 0x5a), (0xf4, 0xe6, 0xc0), (190, 180, 150)
GOLD_TOP, GOLD_FOOT = (255, 214, 110), (214, 140, 40)
INK = (40, 24, 8)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)


def font(size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf'), int(size * S))


class Frame:
    def __init__(self, name):
        base = Image.open(os.path.join(DUMPS, name)).convert('RGB')
        self.img = base.resize((W * S, H * S), Image.LANCZOS).convert('RGBA')

    def layer(self):
        lay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay):
        self.img = Image.alpha_composite(self.img, lay)

    def save(self, name):
        self.img.resize((W, H), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def sc(*v):
    return [a * S for a in v]


def text(f, x, y, t, size, colour, anchor='mm', stroke=0):
    lay, d = f.layer()
    d.text((x * S, y * S), t, font=font(size), fill=colour, anchor=anchor, stroke_width=int(stroke * S),
           stroke_fill=(0, 0, 0))
    f.add(lay)


def note(f, x, y, t, anchor='la', size=15):
    text(f, x, y, t, size, NOTE, anchor, stroke=3)


def glow(f, r, colour, spread, radius):
    lay, d = f.layer()
    d.rounded_rectangle(sc(r[0] - spread / 2, r[1] - spread / 2, r[2] + spread / 2, r[3] + spread / 2),
                        radius=(radius + spread / 2) * S, outline=colour + (170,), width=int(spread * S))
    f.add(lay.filter(ImageFilter.GaussianBlur(7 * S)))


def pill(f, r, on=False):
    """The Commands button's body: steel and gold, or lit gold while the skip is on."""
    x0, y0, x1, y1 = r
    radius = (y1 - y0) / 2
    if on:
        glow(f, r, (255, 190, 90), 14, radius)
    lay, d = f.layer()
    d.rounded_rectangle(sc(x0 + 2, y0 + 3, x1 + 2, y1 + 3), radius=radius * S, fill=(0, 0, 0, 90))
    f.add(lay.filter(ImageFilter.GaussianBlur(4 * S)))
    g = Image.new('RGBA', ((x1 - x0) * S, (y1 - y0) * S))
    gd = ImageDraw.Draw(g)
    for y in range(g.height):
        t = y / max(1, g.height - 1)
        c = tuple(int(a + (b - a) * t) for a, b in zip(GOLD_TOP, GOLD_FOOT)) if on else FILL
        gd.line((0, y, g.width, y), fill=c + (255,))
    mask = Image.new('L', g.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, g.width - 1, g.height - 1), radius=radius * S, fill=255)
    lay = Image.new('RGBA', f.img.size, (0, 0, 0, 0))
    lay.paste(g, (x0 * S, y0 * S), mask)
    f.add(lay)
    lay, d = f.layer()
    d.rounded_rectangle(sc(x0, y0, x1 - 1, y1 - 1), radius=radius * S,
                        outline=((255, 255, 255) if on else BORDER) + (255,), width=2 * S)
    f.add(lay)


def arrows(f, cx, cy, size, colour):
    """The skip's icon: two triangles pointing right (no font glyph needed)."""
    lay, d = f.layer()
    h = size / 2
    for dx in (-size * 0.55, size * 0.05):
        d.polygon(sc(cx + dx, cy - h, cx + dx + size * 0.6, cy, cx + dx, cy + h), fill=colour + (255,))
    f.add(lay)


def lines(f, cx, cy, size, colour):
    """The log's icon: three lines of text."""
    lay, d = f.layer()
    for i, w in enumerate((1, 1, 0.6)):
        y = cy - size * 0.4 + i * size * 0.4
        d.rounded_rectangle(sc(cx - size / 2, y - size * 0.08, cx - size / 2 + size * w, y + size * 0.08),
                            radius=size * 0.08 * S, fill=colour + (255,))
    f.add(lay)


def button(f, r, label, icon, on=False, size=24, sub=None):
    """A pill with its icon left of its label (sub: a second, smaller line)."""
    pill(f, r, on)
    colour = INK if on else LABEL
    h = r[3] - r[1]
    cy = (r[1] + r[3]) / 2
    icon(f, r[0] + h * 0.5, cy, h * 0.27, colour)
    tx = (r[0] + h * 0.85 + r[2] - h * 0.2) / 2
    if sub:
        text(f, tx, cy - 10, label, size, colour)
        text(f, tx, cy + 18, sub, 15, INK if on else DIM_LABEL)
    else:
        text(f, tx, cy, label, size, colour)


def size_note(f, r, anchor='la', at=None):
    w, h = r[2] - r[0], r[3] - r[1]
    x, y = at or (r[0], r[1] - 8)
    note(f, x, y, f'{w}x{h} = {w * TABLET_MM:.0f}x{h * TABLET_MM:.1f} mm', anchor)


def caption(f, t):
    """The mock-up's name and what it shows, over the menu bar."""
    lay, d = f.layer()
    d.rectangle(sc(0, 0, W, 24), fill=(0, 0, 0, 230))
    f.add(lay)
    note(f, 8, 12, t, 'lm')


def pair(f, skip, log, on=False, size=24):
    button(f, skip, 'Skip: ON' if on else 'Skip', arrows, on, size)
    button(f, log, 'Log', lines, size=size)


# The scenes: bottom left, mirroring Commands (16 in from the picture's left and bottom edges, 84 high).
SKIP = (16, H - 100, 16 + 190, H - 16)
LOG = (SKIP[2] + 14, SKIP[1], SKIP[2] + 14 + 150, SKIP[3])


def scenes():
    f = Frame('00057-message-speech-left.png')
    caption(f, 'S1 - a key wait: Skip (a tap turns MsgSkip on) and Log (the message log), bottom left')
    pair(f, SKIP, LOG)
    size_note(f, SKIP)
    size_note(f, LOG)
    f.save('S1-scene.png')

    f = Frame('00002-message-narration.png')
    caption(f, 'S2 - after the tap: lit until a tap on it, a menu or an unseen text stops it; '
               'Log above Skip where a window leaves no room beside it')
    # The narration window starts at x 250: no room for the pair side by side.
    pair(f, (16, H - 100, 16 + 210, H - 16), (16, H - 196, 16 + 150, H - 112), on=True)
    f.save('S2-scene-skipping.png')

    f = Frame('00028-message-cg-bar-speech.png')
    caption(f, 'S3 - window 131 spans the bottom: the pair rides above it and its speaker\'s plate')
    pair(f, (16, 476, 16 + 190, 560), (16 + 204, 476, 16 + 354, 560))
    f.save('S3-over-131.png')


# The battle: the strip under the cards (y 782-824) is free on every battle screen, the turn's included.
B_SKIP = (12, 786, 12 + 150, 820)
B_LOG = (B_SKIP[2] + 10, 786, B_SKIP[2] + 10 + 110, 820)


def battle():
    f = Frame('00074-touch-rails.png')
    caption(f, 'B1 - the player\'s turn: the pair in the free strip under the player\'s cards (bottom left as in scenes)')
    pair(f, B_SKIP, B_LOG, size=20)
    size_note(f, B_LOG, 'lm', (B_LOG[2] + 10, (B_LOG[1] + B_LOG[3]) / 2))
    f.save('B1-battle-turn.png')

    f = Frame('00074-key.png')
    caption(f, 'B1 - a speech in the battle: the same place')
    pair(f, B_SKIP, B_LOG, on=True, size=20)
    f.save('B1-battle-speech.png')

    f = Frame('00074-touch-rails.png')
    caption(f, 'B2 - the player\'s turn: the pair in the armies\' heading, over the turn counter\'s column')
    r1, r2 = (494, 437, 634, 471), (646, 437, 776, 471)
    pair(f, r1, r2, size=20)
    size_note(f, r1, 'la', (r1[0], 30))
    f.save('B2-battle-heading.png')


def sheet(names, out, scale=0.5):
    imgs = [Image.open(os.path.join(OUT, n)) for n in names]
    tiles = [i.resize((int(i.width * scale), int(i.height * scale)), Image.LANCZOS) for i in imgs]
    cols = 2
    tw, th = tiles[0].width, tiles[0].height
    rows = (len(tiles) + cols - 1) // cols
    s = Image.new('RGB', (cols * tw + 12, rows * th + 12 * (rows - 1)), (255, 255, 255))
    for i, t in enumerate(tiles):
        s.paste(t, ((i % cols) * (tw + 12), (i // cols) * (th + 12)))
    s.save(os.path.join(OUT, out))


scenes()
battle()
sheet(['S1-scene.png', 'S2-scene-skipping.png', 'S3-over-131.png'], 'sheet-scenes.png')
sheet(['B1-battle-turn.png', 'B1-battle-speech.png', 'B2-battle-heading.png'], 'sheet-battle.png')
print('done')
