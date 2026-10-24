"""Mock-ups of a backdrop under our battle screen's cards (owner, 2026-10-24: Sengoku Rance's card area is not a flat
fill; docs/ui/battle-screen.md, "The board's backdrop"): A the screens' wave pattern, B the battlefield blurred,
C a texture drawn by code, D the sides' tones, E an empty place as an outline — alone and combined.

The dump: the week-18 save's battle on Oaks (6) in a window (CLAUDE.md, "Commands", a look at one screen):
  <Godot console exe> --path godot -- --load --auto --quit --seed 2 --saves user://saves-look-board
      --slots-from user://saves-week18 --until round-2 --shots <dumps dir>
whose 00095-key.png (Haurein acting in round 1, Ex fallen: the enemy's place 4 empty) is the base. Then, from the
repository's root (Pillow; godot/fonts):
  python -I tools/ui/mock_board_backdrop.py <dump.png> <out dir> < /dev/null
The board's flat fill is found by its colour (each row's at the left edge, the separator's shadow included) and the
new backdrop shows wherever the dump has it, so the cards, the gauge and the labels stay as drawn. The images hold the
player's portraits: keep <out dir> outside the repository. The dump is a window's, 1280x824 with the menu bar's 24
pixels on top; a caption band is added below.
"""
import math
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

DUMP, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
sys.stdout.reconfigure(encoding='utf-8')
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'

BAR = 24                                    # the window's menu bar above the 1280x800 screen
TOP = BAR + 414                             # the board's flat fill starts under the separator (screen y 414)
W, H = 1280, 824 - TOP
CAPTION = 64
SCENE = (128, BAR, 1152, BAR + 408)         # the battle's picture in the dump

# godot/Strategy/UiPalette.cs (Lacquer) and godot/Battles/BattleBoardView.cs
BACKDROP, GOLD, DIM_GOLD = (14, 10, 8), (196, 160, 86), (110, 86, 46)
NAME, QUIET = (232, 199, 122), (176, 160, 130)
PLAYER, ENEMY = (69, 117, 219), (209, 77, 77)
EMPTY_CARD = (988, 632, 1270, 782)          # the enemy's place 4 in the dump, its 2-pixel frame included

dump = Image.open(DUMP).convert('RGB')
board = dump.crop((0, TOP, W, TOP + H))


def font(size, path=GOTHIC, weight=None):
    f = ImageFont.truetype(path, size)
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


# ---- the flat fill's mask -------------------------------------------------------------------------------------------

refs = [board.getpixel((5, y)) for y in range(H)]                  # each row's fill: the left edge is bare
FLAT = refs[-1]
row_fill = Image.new('RGB', (1, H))
row_fill.putdata(refs)
row_fill = row_fill.resize((W, H), Image.NEAREST)
diff = ImageChops.difference(board, row_fill).split()
diff = ImageChops.lighter(ImageChops.lighter(diff[0], diff[1]), diff[2])
keep = diff.point(lambda d: 0 if d <= 1 else min(255, (d - 1) * 64))   # the cards' fill (17,13,10) is 6 off: kept
# the separator's shadow: the new backdrop darkened as the fill is, row by row
shade = Image.new('L', (1, H))
shade.putdata([min(255, round(255 * sum(r) / sum(FLAT))) for r in refs])
shade = shade.resize((W, H), Image.NEAREST)


def compose(back, empty_outline=False):
    """The dump with <back> (RGB, W x H) where the board's fill was; with empty_outline, the empty place drawn as an outline."""
    back = ImageChops.multiply(back.convert('RGB'), Image.merge('RGB', (shade, shade, shade)))
    mask = keep
    if empty_outline:
        mask = keep.copy()
        x0, y0, x1, y1 = EMPTY_CARD
        ImageDraw.Draw(mask).rectangle((x0, y0 - TOP, x1 - 1, y1 - 1 - TOP), fill=0)
    out = dump.copy()
    out.paste(Image.composite(board, back, mask), (0, TOP))
    if empty_outline:
        draw_outline(out)
    return out


def draw_outline(im):
    """An empty place as Sengoku Rance's free grid cell: a thin line around it, gold corners, its number."""
    S = 2
    x0, y0, x1, y1 = EMPTY_CARD
    w, h = x1 - x0, y1 - y0
    layer = Image.new('RGBA', (w * S, h * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    dash, gap = 10, 7
    for (ax, ay, bx, by) in [(1, 1, w - 1, 1), (1, h - 1, w - 1, h - 1), (1, 1, 1, h - 1), (w - 1, 1, w - 1, h - 1)]:
        length = max(bx - ax, by - ay)
        t = 0
        while t < length:
            e = min(length, t + dash)
            if ax == bx:
                d.line(((ax * S, (ay + t) * S), (ax * S, (ay + e) * S)), fill=DIM_GOLD + (200,), width=S)
            else:
                d.line((((ax + t) * S, ay * S), ((ax + e) * S, ay * S)), fill=DIM_GOLD + (200,), width=S)
            t = e + gap
    c = 16
    for (cx, cy, sx, sy) in [(1, 1, 1, 1), (w - 1, 1, -1, 1), (1, h - 1, 1, -1), (w - 1, h - 1, -1, -1)]:
        d.line(((cx * S, (cy + sy * c) * S), (cx * S, cy * S), ((cx + sx * c) * S, cy * S)), fill=GOLD + (255,), width=2 * S)
    f = font(40 * S, CINZEL, 600)
    d.text((w * S / 2, h * S / 2), '4', font=f, fill=GOLD + (70,), anchor='mm')
    layer = layer.resize((w, h), Image.LANCZOS)
    im.paste(layer, (x0, y0), layer)


# ---- the backdrops (W x H, board coordinates: screen y = TOP - BAR + y) -------------------------------------------------

def flat():
    return Image.new('RGB', (W, H), FLAT)


def waves(alpha):
    """The screens' backdrop (UiPalette.DrawWaveBackdrop): seigaiha in gold over dark lacquer, at the screen's phase."""
    S = 2
    im = Image.new('RGB', (W * S, H * S), BACKDROP)
    d = ImageDraw.Draw(im, 'RGBA')
    R = 34
    wave = GOLD + (round(255 * alpha),)
    y_screen0 = TOP - BAR
    for row in range(-1, int((800 + R) / (R / 2)) + 1):
        y = row * R / 2 - y_screen0
        if y < -R or y > H + R:
            continue
        off = 0 if row % 2 == 0 else R
        x = -2 * R + off
        while x < W + 2 * R:
            box = lambda r: ((x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S)
            d.ellipse(box(R), fill=BACKDROP)
            for r in (R, R * 0.72, R * 0.44):
                d.ellipse(box(r), outline=wave, width=S)
            x += 2 * R
    return im.resize((W, H), Image.LANCZOS)


def field(dim, warm):
    """The battle's picture continued: its lower half mirrored down, blurred away and dimmed (as the scene's sides are)."""
    x0, y0, x1, y1 = SCENE
    ground = dump.crop((x0, y1 - 200, x1, y1)).transpose(Image.FLIP_TOP_BOTTOM)
    ground = ground.resize((W, H), Image.BICUBIC).filter(ImageFilter.GaussianBlur(26))
    ground = ground.point(lambda v: round(v * dim))
    if warm:
        ground = Image.blend(ground, Image.new('RGB', (W, H), (40, 26, 16)), warm)
    return ground


def vignette(im, strength=0.55):
    """Darker towards the board's foot and sides, so the texture recedes behind the cards."""
    m = Image.new('L', (W, H))
    px = []
    for y in range(H):
        fy = y / H
        for x in range(0, W, 8):
            fx = abs(x / W - 0.5) * 2
            v = 1 - strength * min(1, 0.6 * fy * fy + 0.5 * fx ** 3)
            px.extend([round(255 * v)] * 8)
    m.putdata(px[:W * H] if len(px) >= W * H else px + [255] * (W * H - len(px)))
    return ImageChops.multiply(im, Image.merge('RGB', (m, m, m)))


def flagstones():
    """Paving stones in dark warm greys, staggered rows, sunk joints; lit faintly from the top."""
    rnd = random.Random(7)
    im = Image.new('RGB', (W, H), (12, 10, 8))                      # the joints
    d = ImageDraw.Draw(im)
    y = -10
    while y < H:
        h = rnd.randint(46, 62)
        x = -rnd.randint(0, 80)
        while x < W:
            w = rnd.randint(78, 150)
            base = rnd.randint(30, 40)
            col = (base + 4, base, base - 5)
            d.rounded_rectangle((x + 2, y + 2, x + w - 2, y + h - 2), radius=5, fill=col)
            d.line((x + 6, y + 3, x + w - 6, y + 3), fill=tuple(c + 9 for c in col), width=1)      # the lit edge
            d.line((x + 6, y + h - 3, x + w - 6, y + h - 3), fill=tuple(c - 10 for c in col), width=1)
            for _ in range(w * h // 90):                                                         # grain
                px, py = x + rnd.randint(4, w - 4), y + rnd.randint(4, h - 4)
                v = rnd.randint(-7, 7)
                d.point((px, py), fill=tuple(max(0, c + v) for c in col))
            x += w
        y += h
    im = im.filter(ImageFilter.GaussianBlur(0.6))
    return vignette(im)


def wood():
    """A lacquered floor: long dark boards across, their grain in faint lines, the joints between them sunk."""
    rnd = random.Random(11)
    im = Image.new('RGB', (W, H), (10, 6, 4))
    d = ImageDraw.Draw(im)
    plank = 38
    for i, y in enumerate(range(-6, H, plank)):
        x = -rnd.randint(0, 300)
        while x < W:
            w = rnd.randint(260, 520)
            base = rnd.randint(30, 38)
            col = (base + 12, base - 2, base - 12)
            d.rectangle((x + 1, y + 1, x + w - 2, y + plank - 2), fill=col)
            for g in range(5):                                                               # the grain
                gy = y + 5 + g * (plank - 10) / 4 + rnd.uniform(-2, 2)
                amp, per, ph = rnd.uniform(1, 3), rnd.uniform(90, 220), rnd.uniform(0, 6.28)
                pts = [(px, gy + amp * math.sin(px / per * 6.28 + ph)) for px in range(x + 2, x + w - 2, 6)]
                if len(pts) > 1:
                    shade_v = rnd.choice((-6, -4, 5))
                    d.line(pts, fill=tuple(max(0, c + shade_v) for c in col), width=1)
            d.line((x + 2, y + 2, x + w - 3, y + 2), fill=tuple(c + 10 for c in col), width=1)  # the varnish's sheen
            x += w
    im = im.filter(ImageFilter.GaussianBlur(0.5))
    return vignette(im)


CARDS = [(x, y, x + 282, y + 150) for x in (10, 302, 696, 988) for y in (474, 632)]   # in the dump, frames included


def rubble(stone, gap, seed=5, bright=1.0):
    """Sengoku Rance's card area (its CG 10200) in spirit: broken stones packed edge to edge, big ones first, small
    ones in what is left, dark gaps between; each stone lit from the top left with a shaded rim, the whole grained."""
    S = 2
    rnd = random.Random(seed)
    w, h = W * S, H * S
    im = Image.new('RGB', (w, h), gap)
    d = ImageDraw.Draw(im)
    taken = Image.new('L', (w, h), 0)
    t = ImageDraw.Draw(taken)
    shade = lambda c, f: tuple(max(0, min(255, round(v * f))) for v in c)
    grey = tuple(round(sum(stone) / 3) for _ in range(3))
    for radius, tries in ((46, 500), (34, 1000), (26, 1800), (19, 3000), (13, 5000), (9, 7000), (6, 9000), (4, 9000)):
        r0 = radius * S
        for _ in range(tries):
            cx, cy = rnd.uniform(-r0, w + r0), rnd.uniform(-r0, h + r0)
            r = r0 * rnd.uniform(0.75, 1.2)
            n = rnd.randint(4, 7)
            start, squash, turn = rnd.uniform(0, 6.28), rnd.uniform(0.5, 1.0), rnd.uniform(0, 3.14)
            pts = []
            for k in range(n):
                a = start + k * 2 * math.pi / n + rnd.uniform(-0.4, 0.4)
                rr = r * rnd.uniform(0.6, 1.0)
                ex, ey = rr * math.cos(a), rr * squash * math.sin(a)
                pts.append((cx + ex * math.cos(turn) - ey * math.sin(turn), cy + ex * math.sin(turn) + ey * math.cos(turn)))
            probe = [(cx, cy)] + [(cx + (px - cx) * f, cy + (py - cy) * f) for px, py in pts for f in (0.55, 0.92)]
            if any(0 <= px < w and 0 <= py < h and taken.getpixel((int(px), int(py))) for px, py in probe):
                continue
            t.polygon(pts, fill=255)
            t.line(pts + [pts[0]], fill=255, width=2 * S)                          # the gap round it
            tone = tuple(round(s + (g - s) * rnd.uniform(0, 0.45)) for s, g in zip(stone, grey))   # some greyer
            base = shade(tone, rnd.uniform(0.74, 1.14) * bright)
            d.polygon(pts, fill=shade(base, 0.62))                                # the shaded rim
            lit = [(cx + (px - cx) * 0.86 - r * 0.07, cy + (py - cy) * 0.86 - r * 0.07) for px, py in pts]
            d.polygon(lit, fill=base)
            top = [(cx + (px - cx) * 0.55 - r * 0.2, cy + (py - cy) * 0.5 - r * 0.22) for px, py in pts]
            d.polygon(top, fill=shade(base, 1.07))                                # the light on its face
    im = im.resize((W, H), Image.LANCZOS).filter(ImageFilter.GaussianBlur(0.7))
    grain = Image.effect_noise((W, H), 22).filter(ImageFilter.GaussianBlur(0.5))
    grain = Image.merge('RGB', (grain, grain, grain))
    return ImageChops.overlay(im, Image.blend(Image.new('RGB', (W, H), (128, 128, 128)), grain, 0.35))


def legible(im):
    """Darker under the armies' names along the top and under the gauge's "n TURNS LEFT", so they read on stone."""
    m = Image.new('L', (W, H), 255)
    d = ImageDraw.Draw(m)
    for y in range(40):
        d.line((0, y, W, y), fill=round(255 * (0.35 + 0.65 * y / 40)))
    soft = Image.new('L', (W, H), 0)
    ImageDraw.Draw(soft).rounded_rectangle((600, 700 - TOP, 680, 800 - TOP), radius=30, fill=255)
    soft = soft.filter(ImageFilter.GaussianBlur(14)).point(lambda v: 255 - round(v * 0.55))
    m = ImageChops.multiply(m, soft)
    return ImageChops.multiply(im, Image.merge('RGB', (m, m, m)))


def cells(im, line, every=True):
    """Sengoku Rance's grid: a thin line round each place, a little outside the card's frame; the empty place's alone."""
    S = 2
    layer = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for box in CARDS if every else [EMPTY_CARD]:
        x0, y0, x1, y1 = box
        o = 3 if every else 0
        d.rectangle(((x0 - o) * S, (y0 - o - TOP) * S, (x1 - 1 + o) * S, (y1 - 1 + o - TOP) * S), outline=line + (230,),
                    width=round(1.5 * S))
    layer = layer.resize((W, H), Image.LANCZOS)
    out = im.convert('RGBA')
    out.alpha_composite(layer)
    return out.convert('RGB')


LABELS = [(0, 444, 130, 474), (1020, 444, 1280, 474), (606, 712, 676, 792)]   # the armies' names, "n TURNS LEFT"


def compose_cells(back, line, every):
    """The dump over <back> with the empty place opened (its frame and words gone), then the grid's lines on top; the
    cards whole (their figures' dark outlines are the fill's colour), the labels with a shadow under them."""
    back = ImageChops.multiply(back.convert('RGB'), Image.merge('RGB', (shade, shade, shade)))
    mask = keep.copy()
    md = ImageDraw.Draw(mask)
    for x0, y0, x1, y1 in CARDS:
        if (x0, y0) != EMPTY_CARD[:2]:
            md.rectangle((x0, y0 - TOP, x1 - 1, y1 - 1 - TOP), fill=255)
    x0, y0, x1, y1 = EMPTY_CARD
    md.rectangle((x0, y0 - TOP, x1 - 1, y1 - 1 - TOP), fill=0)
    text = Image.new('L', (W, H), 0)
    for x0, y0, x1, y1 in LABELS:
        box = (x0, y0 - TOP, x1, y1 - TOP)
        md.rectangle((box[0], box[1], box[2] - 1, box[3] - 1), fill=0)
        text.paste(diff.crop(box).point(lambda d: min(255, max(0, (d - 8) * 6))), box[:2])
    halo = text.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(2.5)).point(lambda v: min(255, v * 2))
    back = Image.composite(Image.new('RGB', (W, H), (6, 5, 4)), back, halo.point(lambda v: round(v * 0.85)))
    back = Image.composite(board, back, text)
    board_new = Image.composite(board, back, mask)
    board_new = cells(board_new, line, every)
    # the cards drawn again over the lines that ran under their frames' outer pixels
    board_new = Image.composite(board, board_new, mask.point(lambda v: 255 if v == 255 else 0))
    out = dump.copy()
    out.paste(board_new, (0, TOP))
    return out


def sides(im, alpha=0.13):
    """The armies' tones: the player's blue on the left half, the enemy's red on the right, crossing under the gauge."""
    tint = Image.new('RGB', (W, H))
    m = Image.new('L', (W, H))
    dt, dm = ImageDraw.Draw(tint), ImageDraw.Draw(m)
    a, b = 560, 720
    for x in range(W):
        t = 0 if x <= a else 1 if x >= b else (x - a) / (b - a)
        col = tuple(round(p + (e - p) * t) for p, e in zip(PLAYER, ENEMY))
        dt.line((x, 0, x, H), fill=col)
        strength = alpha * (0.35 + 0.65 * abs(t - 0.5) * 2)        # fainter where the two cross
        dm.line((x, 0, x, H), fill=round(255 * strength))
    return Image.composite(tint, im, m)


# ---- the sheets -------------------------------------------------------------------------------------------------------

def save(im, name, caption, note):
    sheet = Image.new('RGB', (im.width, im.height + CAPTION), (23, 24, 27))
    sheet.paste(im, (0, 0))
    d = ImageDraw.Draw(sheet)
    d.text((16, im.height + 8), caption, font=font(22, CINZEL, 700), fill=NAME)
    d.text((16, im.height + 38), note, font=font(16), fill=QUIET)
    sheet.save(os.path.join(OUT, name))
    print(name)


save(dump, '01-now.png', 'Now', 'The board\'s flat fill under the cards.')
save(compose(waves(0.05)), '02-A1-waves.png', 'A1  The wave pattern',
     'Our screens\' backdrop (Subordinates, Harem, Treasure, deployment) as faint as there: gold 5%.')
save(compose(waves(0.11)), '03-A2-waves-stronger.png', 'A2  The wave pattern, stronger', 'The same, gold 11%.')
save(compose(field(0.42, 0)), '04-B1-field.png', 'B1  The battlefield continued',
     'The picture\'s lower half mirrored down, blurred, dimmed as the scene\'s sides are (45%).')
save(compose(field(0.3, 0.35)), '05-B2-field-dark.png', 'B2  The battlefield, darker and warmer',
     'Dimmed to 30% and drawn a third of the way to the lacquer\'s brown.')
save(compose(flagstones()), '06-C1-flagstones.png', 'C1  Paving stones, drawn by code',
     'Staggered stones in dark warm greys, the joints sunk; darker towards the foot and the sides.')
save(compose(wood()), '07-C2-wood.png', 'C2  A lacquered wooden floor, drawn by code',
     'Long boards across with their grain; darker towards the foot and the sides.')
save(compose(sides(flat(), 0.16)), '08-D-sides.png', 'D  The sides\' tones on the flat fill',
     'The player\'s blue under the left half, the enemy\'s red under the right, crossing under the gauge.')
save(compose(sides(waves(0.11))), '09-A2-D.png', 'A2 + D', 'The stronger wave pattern with the sides\' tones.')
save(compose(sides(field(0.42, 0))), '10-B1-D.png', 'B1 + D', 'The battlefield continued with the sides\' tones.')
save(compose(sides(flagstones()), empty_outline=True), '11-C1-D-E.png', 'C1 + D + E',
     'Paving stones, the sides\' tones; the empty place (the enemy\'s 4) as an outline with gold corners.')
save(compose(sides(waves(0.11)), empty_outline=True), '12-A2-D-E.png', 'A2 + D + E',
     'The stronger wave pattern, the sides\' tones, the empty place as an outline.')

# Round 2 (owner, 2026-10-24: round 1 all looked poor; Sengoku Rance's own background, roughly): its broken stones
# and its grid's green lines, in its colours (sampled from CG 10200: stones about 90,114,88, gaps 42,51,52, the
# lines 80,175,75) and in ours.
SR_STONE, SR_GAP, SR_LINE = (90, 114, 88), (40, 50, 48), (80, 175, 75)
WARM_STONE, WARM_GAP = (96, 86, 72), (30, 24, 19)
STEEL_STONE, STEEL_GAP = (90, 94, 100), (32, 34, 38)
save(compose_cells(legible(rubble(SR_STONE, SR_GAP)), SR_LINE, True), '13-S1-sr.png', 'S1  Sengoku Rance\'s stones',
     'Broken stones in its green-grey, as bright as its; its green line round every place, the empty one open.')
save(compose_cells(legible(rubble(SR_STONE, SR_GAP)), SR_LINE, False), '14-S1-sr-empty-only.png',
     'S1\'  The same, the line round the empty place only', 'The cards\' gold frames alone round the others.')
save(compose_cells(legible(rubble(SR_STONE, tuple(round(v * 0.6) for v in SR_GAP), bright=0.6)), SR_LINE, True),
     '15-S2-sr-dim.png', 'S2  Sengoku Rance\'s stones, dimmer', 'The same at 60%, nearer our dark cards.')
save(compose_cells(legible(rubble(WARM_STONE, WARM_GAP, bright=0.75)), DIM_GOLD, True), '16-S3-warm.png',
     'S3  The stones in the lacquer\'s warm grey', 'Warm grey-brown stones, the line in the frames\' dim gold.')
save(compose_cells(legible(rubble(STEEL_STONE, STEEL_GAP, bright=0.75)), SR_LINE, True), '17-S4-steel.png',
     'S4  The stones in steel grey', 'Cool grey stones (the Steel palette\'s), Sengoku Rance\'s green line.')
