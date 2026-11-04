"""Mock-ups of Mill's (unit 25) Indirect attack (owner, 2026-11-04): her troops are Phantom Beasts, so instead of the bow she
sends many small phantoms flying at the enemy level across the field, about her head's size or a little more, in the
manner of her troop's creatures (pastel, round, translucent; drawn here as our own, not traced from her sprites).

The scene is composed as our battle scene shows it between exchanges (CG 900, Ex's (commander 1) Leazas Knights CG 1131,
Mill's troops below the limit with her among them CG 1180, the overlay 901; 640x256) at x2, from the player's own CGs
(KICHIKUOU_EXTRACT_DIR's cg_png). From the repository's root (Pillow):
  python -I tools/ui/mock_mill_shots.py <cg_png dir> <out dir>
Keep <out dir> outside the repository: the sheets hold the game's art.

Each variant is one sheet: the field mid-volley (the frame the stream reaches the enemy) and the projectile drawn large
in its four colours.
  A  Bubbles: plain glowing orbs, a highlight, a short fading after-image trail.
  B  Little spirits: the orb with two dot eyes and a wisp tail streaming behind it (a comet), leaning into the flight.
  C  Jelly drops: squashed by their speed, dot eyes and a small smile, a trail of sparkles, bobbing on a low wave.
"""
import math
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFilter

CGS, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
S = 2
W, H = 640, 256

# The troop's pastels (body, rim) — green, blue, pink, orange.
COLOURS = [((184, 228, 120), (120, 176, 60)), ((190, 216, 244), (110, 150, 210)),
           ((246, 170, 204), (210, 100, 150)), ((248, 190, 110), (214, 130, 50))]
EYE = (40, 44, 70)


def cg(n):
    return Image.open(os.path.join(CGS, f'CG{n:04d}.png')).convert('RGBA')


def keyed(im):
    """The sprites' green key (palette entry 40 in the engine) made transparent."""
    px = im.load()
    key = px[0, 0]
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y][:3] == key[:3]:
                px[x, y] = (0, 0, 0, 0)
    return im


def scene():
    field = cg(900).resize((W, H))
    for n in (1131, 1180, 901):
        sprite = keyed(cg(n))
        field.alpha_composite(sprite, (0, 0) if n != 1131 else (W - sprite.width, 0))
    return field.resize((W * S, H * S), Image.NEAREST)


def orb(size, colour, variant, phase=0.0):
    """One projectile on a transparent square of 3x its size (room for the trail), flying right."""
    body, rim = colour
    pad = size * 3
    im = Image.new('RGBA', (pad, pad), (0, 0, 0, 0))
    cx, cy = pad * 2 // 3, pad // 2
    glow = Image.new('RGBA', (pad, pad), (0, 0, 0, 0))
    g = ImageDraw.Draw(glow)
    if variant == 'B':
        # the wisp tail: a tapering stream of fading circles behind the body
        for k in range(14):
            t = k / 13
            r = size / 2 * (1 - 0.8 * t)
            x = cx - size * 0.35 - t * size * 1.5
            y = cy + math.sin(t * 3.2 + phase) * size * 0.12
            g.ellipse((x - r, y - r, x + r, y + r), fill=body + (int(150 * (1 - t)),))
    if variant == 'A':
        for k in (3, 2, 1):
            x = cx - k * size * 0.45
            r = size / 2 * (1 - 0.1 * k)
            g.ellipse((x - r, cy - r, x + r, cy + r), fill=body + (int(70 / k),))
    if variant == 'C':
        for k in range(7):
            x = cx - size * 0.6 - k * size * 0.28
            y = cy + math.sin(k * 1.3 + phase) * size * 0.25
            r = size * 0.07
            g.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 240, int(220 * (1 - k / 7))))
    # the soft halo
    r = size * 0.72
    g.ellipse((cx - r, cy - r, cx + r, cy + r), fill=body + (60,))
    glow = glow.filter(ImageFilter.GaussianBlur(size * 0.08))
    im.alpha_composite(glow)
    d = ImageDraw.Draw(im)
    # the body: squashed along the flight for the jelly drops, a little for the spirits
    sx, sy = {'A': (1.0, 1.0), 'B': (1.08, 0.94), 'C': (1.25, 0.8)}[variant]
    rx, ry = size / 2 * sx, size / 2 * sy
    d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=body + (215,), outline=rim + (255,), width=max(1, size // 18))
    # the inner shade toward the lower back and the highlight toward the upper front, each blended on a layer of its own
    # (a fill with alpha on the image itself would replace the body's pixels, not blend over them)
    shade = Image.new('RGBA', im.size, (0, 0, 0, 0))
    ImageDraw.Draw(shade).ellipse((cx - rx * 0.8, cy - ry * 0.1, cx + rx * 0.35, cy + ry * 0.85), fill=rim + (90,))
    im.alpha_composite(shade.filter(ImageFilter.GaussianBlur(size * 0.06)))
    shine = Image.new('RGBA', im.size, (0, 0, 0, 0))
    s = ImageDraw.Draw(shine)
    hx, hy = cx + rx * 0.25, cy - ry * 0.45
    s.ellipse((hx - rx * 0.32, hy - ry * 0.2, hx + rx * 0.22, hy + ry * 0.16), fill=(255, 255, 255, 200))
    s.ellipse((hx + rx * 0.3, hy + ry * 0.05, hx + rx * 0.42, hy + ry * 0.17), fill=(255, 255, 255, 220))
    im.alpha_composite(shine)
    d = ImageDraw.Draw(im)
    if variant in ('B', 'C'):
        ex, ey, er = cx + rx * 0.42, cy + ry * 0.02, max(1.5, size * 0.065)
        for dx in (0, -rx * 0.36):
            d.ellipse((ex + dx - er, ey - er * 1.3, ex + dx + er, ey + er * 1.3), fill=EYE + (255,))
        if variant == 'C':
            mx, my = ex - rx * 0.18, ey + ry * 0.32
            d.arc((mx - rx * 0.14, my - ry * 0.12, mx + rx * 0.14, my + ry * 0.12), 20, 160, fill=EYE + (255,),
                  width=max(1, size // 22))
    return im


def volley(base, variant, seed):
    """The field mid-volley: phantoms streaming from Mill's hand toward the knights, the leading ones arriving."""
    random.seed(seed)
    im = base.copy()
    hand = (118 * S, 150 * S)
    for k in range(16):
        t = k / 15                                     # 0 the last one out, 1 the first, at the knights
        x = hand[0] + 40 * S + t * (470 - 118 - 40) * S
        lane = random.uniform(-38, 38) * S
        bob = math.sin(t * 9 + k) * 6 * S if variant == 'C' else 0
        y = hand[1] - 10 * S + lane * (0.3 + 0.7 * t) + bob
        size = int(random.uniform(26, 34) * S)         # her head ~22 px in the sprite: a little more
        p = orb(size, COLOURS[k % 4], variant, phase=k)
        im.alpha_composite(p, (int(x - p.width * 2 // 3), int(y - p.height // 2)))
    # the leading phantoms burst into pastel puffs on the knights
    burst = Image.new('RGBA', im.size, (0, 0, 0, 0))
    b = ImageDraw.Draw(burst)
    for k in range(10):
        x, y = random.uniform(470, 560) * S, random.uniform(110, 200) * S
        r = random.uniform(8, 18) * S
        b.ellipse((x - r, y - r, x + r, y + r), fill=COLOURS[k % 4][0] + (120,))
    im.alpha_composite(burst.filter(ImageFilter.GaussianBlur(6 * S)))
    return im


def sheet(variant, title_seed):
    base = scene()
    field = volley(base, variant, title_seed)
    big = 120
    row = Image.new('RGBA', (field.width, big * 3), (40, 40, 40, 255))
    for k, colour in enumerate(COLOURS):
        p = orb(big, colour, variant, phase=k)
        row.alpha_composite(p, (40 + k * (field.width - 80) // 4, 0))
    out = Image.new('RGBA', (field.width, field.height + row.height), (40, 40, 40, 255))
    out.alpha_composite(field, (0, 0))
    out.alpha_composite(row, (0, field.height))
    out.convert('RGB').save(os.path.join(OUT, f'mill-shots-{variant}.png'))


for v, seed in (('A', 1), ('B', 2), ('C', 3)):
    sheet(v, seed)
print('ok')
