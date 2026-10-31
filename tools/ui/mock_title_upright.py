"""Mock-ups of the title held upright again (owner, 2026-10-31; docs/ui/phone.md, "Round P33").

The title upright is PE2 as built (step P8d): the title's picture at the screen's width in the middle, black bands above
and below, a lacquer row for each button under it, the button's own word alone. The owner found it poor and asked for:
the picture over the whole screen, keeping its proportions; the buttons' characters off the picture and into the rows;
in the rows the characters without the words the picture prints by them (Start, Load...).

What the title's CGs hold (ｽﾀMENU.ADV:10-14, :62-75, :805):
- CG 301 (at 640,0): the background alone — the stone texture, its frieze at the top and the foot, and the logo. The
  texture is drawn in nine palette entries (TEXTURE below); the logo in others, so it is cut out by its colours.
- CG 303 (at 640,400): every button as the title shows it unlit — each character and its word on the same background,
  faded; CG 302 (at 0,400): the same lit, in full colour. A button's box (`:805`, BOXES) is copied whole from 303 onto
  301 at its place, from 302 while the pointer is over it: the word is in the same picture, inside the same box.
- Where 302 differs from 301 is the button's own pixels. In every box the word is a speech-bubble plate of its own, apart
  from the character: seven of the eight are two separate groups of pixels (Destiny three: the girl on her saucer, the
  small creature at her left, the word); Spy's plate touches her skirt with its tail alone (rows 358-360), so the cut is
  the row under the skirt. So each character is cut out exactly, with no hand painting: the pixels of 302 that differ
  from 301, less the word's group (CAPTIONS).

From the repository's root (Pillow, numpy and scipy; godot/fonts):
  python -I tools/ui/mock_title_upright.py <gallery dir> <extract dir> <out dir> < /dev/null
- <gallery dir>: the screen gallery (`screen-gallery` at the root): phone-upright/current's title and title-clears, the
  build as it stands (tools/ui/gallery.ps1 -PhoneUp -Only title,title-clears).
- <extract dir>: KICHIKUOU_EXTRACT_DIR — cg_png/CG0301.png, CG0302.png, CG0303.png.
Keep <out dir> outside the repository: the sheets hold the player's CGs.

Round P33: PE3 the owner's words as they stand — the whole picture scaled to cover the screen (its middle third shown,
the logo cut); PE4 the texture alone covering the screen, the logo laid whole over it; PE5 the texture at the picture's
own scale (the screen's width) continued over the whole screen, the logo laid whole. Each after a clear (every button,
the rows' order PE2's) and at a first launch (Alice, Load, Start: PE3n, PE4n, PE5n). `cgs` shows the CGs and the cut.
Round P34, PE5 as the owner changed it (the characters in one column nearer the middle, the words left-aligned right of
it): PE6 the characters' middles on one line, PE7 their right edges on one line (PE6n, PE7n at a first launch).
The pink lines and text are the mock-ups' notes, not part of a screen.
"""
import os
import sys
import textwrap

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

GALLERY, EXTRACT, OUT = sys.argv[1:4]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)

PW, PH = 1220, 2712                     # the Poco X6 Pro upright: 69.5 × 154.5 mm
PX_MM = 446 / 25.4
TOP = 110                               # the camera's hole and the status bar
BAR = (2504, 2652)                      # the bottom bar; the gesture zone under it

# Lacquer (godot/Strategy/UiPalette.cs).
PANEL, ROW, ROW_FILL_ALPHA = (22, 16, 12), (34, 26, 20), 224
GOLD, DIM_GOLD, NAME, TEXT = (196, 160, 86), (110, 86, 46), (232, 199, 122), (244, 232, 206)
VERMILION_LINE = (214, 150, 90)
NOTE = (255, 92, 160)

# ｽﾀMENU.ADV:805: the buttons' boxes at 640×400 (x, y, w, h); 8-10 do nothing, 11 is never loaded.
BOXES = {0: (0, 0, 240, 154), 1: (248, 0, 187, 127), 2: (434, 2, 202, 100), 3: (545, 105, 92, 122),
         4: (0, 272, 96, 117), 5: (95, 270, 151, 130), 6: (261, 263, 173, 137), 7: (463, 238, 177, 162)}
WORDS = {0: 'Destiny', 1: 'B.G.M', 2: 'Alice', 3: 'Please', 4: 'Spy', 5: 'Load', 6: 'Start', 7: 'C.G'}
# The word's plate in each box (x0, y0, x1, y1), its own group of pixels; Spy's touches her skirt by its tail, so her
# box is cut under the skirt (SPY_CUT) first.
CAPTIONS = {0: (135, 21, 238, 51), 1: (353, 60, 433, 89), 2: (445, 4, 520, 32), 3: (550, 107, 634, 136),
            4: (21, 358, 78, 386), 5: (97, 275, 170, 303), 6: (375, 315, 430, 343), 7: (465, 331, 526, 354)}
SPY_CUT = 358
ORDER = [4, 3, 0, 1, 7, 2, 5, 6]        # PE2's rows top down: the jokes, the extras, Alice, Load, Start at the thumb
FIRST = [2, 5, 6]                       # a first launch: Alice, Load, Start
START = 6

TEXTURE = [30, 31, 169, 170, 171, 172, 191, 198, 206]   # CG 301's stone texture: every other entry is the logo's
INNER = (16, 380)                       # CG 301's rows between its friezes
LOGO_BAND = (124, 276)                  # the rows the logo and its ink splashes take
LOGO_LEAST = 300                        # the logo's letters are five groups of 683-15955 pixels; the rest are strays

ROW_H, ROW_GAP, ROW_IN, ROWS_OVER = 208, 14, 40, 20     # the rows: 11.8 mm, at PE2's places
SLOT_W, ICON_MAX = 250, 2.0             # the character's slot at a row's left; at most twice its CG pixels
LOGO_K = 2.2                            # the logo laid whole: 902 px wide, the screen's width less 159 each side
WORD_SIZE, WORD_GAP = 66, 44            # a row's word, the brush face; round P34: its distance from the column


def font(name, size):
    return ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), max(1, round(size)))


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)


def mm(px):
    return f'{px / PX_MM:.1f} mm'


def cg(n):
    return Image.open(os.path.join(EXTRACT, 'cg_png', f'CG{n:04d}.png'))


BG = cg(301)
BG_RGB = np.asarray(BG.convert('RGB'))
LIT = np.asarray(cg(302).convert('RGB'))
UNLIT = np.asarray(cg(303).convert('RGB'))


# --- the cut ---------------------------------------------------------------------------------------------------------

def own_pixels(sheet):
    """The pixels of a button sheet (302 or 303) that differ from the background 301."""
    return np.abs(sheet.astype(int) - BG_RGB.astype(int)).sum(axis=2) > 0


def character(n, sheet=LIT):
    """Button n's character without its word: an RGBA image cropped to it, and its box at 640×400."""
    x, y, w, h = BOXES[n]
    mine = np.zeros(BG_RGB.shape[:2], bool)
    mine[y:y + h, x:x + w] = own_pixels(sheet)[y:y + h, x:x + w]
    if n == 4:
        mine[SPY_CUT:, :] = False
    labels, count = ndimage.label(mine, structure=np.ones((3, 3)))
    cx0, cy0, cx1, cy1 = CAPTIONS[n]
    keep = np.zeros_like(mine)
    for i, part in enumerate(ndimage.find_objects(labels), start=1):
        ys, xs = part
        inside = xs.start >= cx0 - 1 and xs.stop <= cx1 + 1 and ys.start >= cy0 - 1 and ys.stop <= cy1 + 1
        if not inside:
            keep |= ndimage.binary_fill_holes(labels == i)
    ys, xs = np.where(keep)
    bx0, by0, bx1, by1 = xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
    rgba = np.zeros((by1 - by0, bx1 - bx0, 4), np.uint8)
    rgba[..., :3] = sheet[by0:by1, bx0:bx1]
    rgba[..., 3] = keep[by0:by1, bx0:bx1] * 255
    return Image.fromarray(rgba, 'RGBA'), (bx0, by0, bx1, by1)


CHARACTERS = {n: character(n)[0] for n in BOXES}


def logo():
    """The logo cut from CG 301 by its colours, with a dark edge two pixels wide where the texture's dark ink was."""
    idx = np.asarray(BG)
    mine = ~np.isin(idx, TEXTURE)
    mine[:LOGO_BAND[0]] = mine[LOGO_BAND[1]:] = False
    labels, _ = ndimage.label(mine, structure=np.ones((3, 3)))
    sizes = np.bincount(labels.ravel())
    sizes[0] = 0
    mine = sizes[labels] >= LOGO_LEAST              # the letters; the odd pixels of the splashes stay texture
    ys, xs = np.where(mine)
    x0, y0, x1, y1 = xs.min() - 3, ys.min() - 3, xs.max() + 4, ys.max() + 4
    m = mine[y0:y1, x0:x1]
    edge = ndimage.binary_dilation(m, iterations=2) & ~m
    rgba = np.zeros((y1 - y0, x1 - x0, 4), np.uint8)
    rgba[..., :3] = BG_RGB[y0:y1, x0:x1]
    rgba[edge, :3] = (18, 12, 10)
    rgba[..., 3] = (m | edge) * 255
    return Image.fromarray(rgba, 'RGBA')


LOGO = logo()


def bare_texture():
    """CG 301's texture between its friezes with the logo's band filled from the texture over and under it."""
    t = BG_RGB.copy()
    a, b = LOGO_BAND
    mid = (a + b) // 2
    t[a:mid] = BG_RGB[40:40 + mid - a]
    t[mid:b] = BG_RGB[290:290 + b - mid]
    return Image.fromarray(t[INNER[0]:INNER[1]])


TEXTURE_IMG = bare_texture()
FRIEZE_TOP = Image.fromarray(BG_RGB[:INNER[0]])
FRIEZE_FOOT = Image.fromarray(BG_RGB[INNER[1]:])


# --- the backgrounds -------------------------------------------------------------------------------------------------

def cover(im, smooth=Image.BICUBIC):
    """`im` scaled to cover the screen, its proportions kept, the middle shown."""
    k = max(PW / im.width, PH / im.height)
    big = im.resize((round(im.width * k), round(im.height * k)), smooth)
    x, y = (big.width - PW) // 2, (big.height - PH) // 2
    return big.crop((x, y, x + PW, y + PH)), k


def background_pe3():
    """The owner's words as they stand: the whole picture (its logo too), covering the screen."""
    return cover(BG.convert('RGB'))[0]


def background_pe4():
    return cover(TEXTURE_IMG)[0]


def background_pe5():
    """The texture at the picture's own scale (the screen's width), mirrored over and under itself; its friezes at the
    screen's top and foot."""
    k = PW / TEXTURE_IMG.width
    tile = TEXTURE_IMG.resize((PW, round(TEXTURE_IMG.height * k)), Image.LANCZOS)
    flipped = tile.transpose(Image.FLIP_TOP_BOTTOM)
    img = Image.new('RGB', (PW, PH))
    y, i = 0, 0
    while y < PH:
        img.paste(tile if i % 2 == 0 else flipped, (0, y))
        y += tile.height
        i += 1
    top = FRIEZE_TOP.resize((PW, round(FRIEZE_TOP.height * k)), Image.LANCZOS)
    foot = FRIEZE_FOOT.resize((PW, round(FRIEZE_FOOT.height * k)), Image.LANCZOS)
    img.paste(top, (0, 0))
    img.paste(foot, (0, PH - foot.height))
    return img


# --- the parts -------------------------------------------------------------------------------------------------------

def panel(d, box, fill=PANEL, frame=GOLD):
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=fill)
    d.rectangle(box, outline=frame, width=3)
    d.rectangle((x0 + 8, y0 + 8, x1 - 8, y1 - 8), outline=DIM_GOLD, width=2)
    for cx, cy in ((x0 + 8, y0 + 8), (x1 - 8, y0 + 8), (x0 + 8, y1 - 8), (x1 - 8, y1 - 8)):
        d.polygon([(cx, cy - 7), (cx + 7, cy), (cx, cy + 7), (cx - 7, cy)], fill=frame)


def menu_cell(d):
    """The bar's Menu cell (PK1), as built: the menu bar's commands behind it."""
    x0, y0, x1, y1 = 16, BAR[0], 186, BAR[1]
    panel(d, (x0, y0, x1, y1))
    cx = (x0 + x1) // 2
    for dy in (-16, 0, 16):
        d.rectangle((cx - 26, y0 + 58 + dy - 4, cx + 26, y0 + 58 + dy + 4), fill=GOLD)
    d.text((cx, y0 + 116), 'Menu', font=GOTHIC(28), fill=TEXT, anchor='mm')


def rows_top(count):
    return BAR[0] - ROWS_OVER - count * ROW_H - (count - 1) * ROW_GAP


def row_box(i, count):
    y = rows_top(count) + i * (ROW_H + ROW_GAP)
    return ROW_IN, y, PW - ROW_IN, y + ROW_H


def icon_scale(n):
    ch = CHARACTERS[n]
    return min(ICON_MAX, (SLOT_W - 20) / ch.width, (ROW_H - 22) / ch.height)


def column():
    """Round P34's column: the widest character, the gap, the widest word, centred in a row (the same for every count of
    rows, so nothing moves when a button is unlocked). Its left edge and the words' left edge."""
    widest = max(round(CHARACTERS[n].width * icon_scale(n)) for n in BOXES)
    probe = ImageDraw.Draw(Image.new('RGB', (1, 1)))
    word = max(probe.textlength(WORDS[n], font=BRUSH(WORD_SIZE)) for n in BOXES)
    left = round(PW / 2 - (widest + WORD_GAP + word) / 2)
    return left, widest, left + widest + WORD_GAP


def rows(img, buttons, layout=None):
    """A lacquer plate for each button, the character at its left (lit, its word off), the button's word in the rest;
    Start red. The plates let the texture through a little. `layout`: None PE5's (the character in a slot at the row's
    left, the word in the middle of the rest); 'axis' (PE6) the characters' middles on one line and 'flush' (PE7) their
    right edges on one, in round P34's column nearer the middle, the words left-aligned right of it."""
    over = Image.new('RGBA', img.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(over)
    for i, n in enumerate(buttons):
        x0, y0, x1, y1 = row_box(i, len(buttons))
        if n == START:
            od.rectangle((x0, y0, x1, y1), fill=(112, 26, 20, 240))
            od.rectangle((x0, y0, x1, (y0 + y1) // 2), fill=(134, 36, 26, 240))
            od.rectangle((x0, y0, x1, y1), outline=GOLD, width=3)
            od.rectangle((x0 + 7, y0 + 7, x1 - 7, y1 - 7), outline=VERMILION_LINE, width=1)
        else:
            od.rectangle((x0, y0, x1, y1), fill=ROW + (ROW_FILL_ALPHA,))
            od.rectangle((x0, y0, x1, y1), outline=DIM_GOLD, width=3)
    img.paste(Image.alpha_composite(img.convert('RGBA'), over).convert('RGB'))
    d = ImageDraw.Draw(img)
    left, widest, words = column()
    for i, n in enumerate(buttons):
        x0, y0, x1, y1 = row_box(i, len(buttons))
        ch = CHARACTERS[n]
        k = icon_scale(n)
        icon = ch.resize((round(ch.width * k), round(ch.height * k)), Image.LANCZOS)
        iy = (y0 + y1 - icon.height) // 2
        if layout is None:
            img.paste(icon, (x0 + 14 + (SLOT_W - icon.width) // 2, iy), icon)
            d.text(((x0 + 14 + SLOT_W + x1) / 2, (y0 + y1) / 2), WORDS[n], font=BRUSH(WORD_SIZE), fill=NAME,
                   anchor='mm')
            continue
        sx = left + (widest - icon.width) // 2 if layout == 'axis' else left + widest - icon.width
        img.paste(icon, (sx, iy), icon)
        d.text((words, (y0 + y1) / 2), WORDS[n], font=BRUSH(WORD_SIZE), fill=NAME, anchor='lm')


def lay_logo(img, count):
    """The logo whole, in the middle of the room over the rows (at most at the top of the room)."""
    lg = LOGO.resize((round(LOGO.width * LOGO_K), round(LOGO.height * LOGO_K)), Image.LANCZOS)
    room = (TOP + 40, rows_top(count) - 40)
    y = (room[0] + room[1] - lg.height) // 2
    img.paste(lg, ((PW - lg.width) // 2, y), lg)
    return lg, y


def note(d, xy, s, size=34, anchor='la'):
    d.multiline_text(xy, s, font=GOTHIC(size), fill=NOTE, anchor=anchor, stroke_width=5, stroke_fill=(0, 0, 0),
                     spacing=8)


def chrome(d):
    d.ellipse((PW / 2 - 24, 30, PW / 2 + 24, 78), fill=(0, 0, 0), outline=(70, 70, 76), width=3)
    d.rounded_rectangle((PW / 2 - 130, 2682, PW / 2 + 130, 2692), radius=5, fill=(120, 120, 124))


def reach(img):
    """The right thumb's comfortable reach, one hand (~70 mm round a grip low on the right side)."""
    over = Image.new('RGBA', img.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(over)
    cx, cy, r = PW + 30, PH - 560, round(70 * PX_MM)
    od.pieslice((cx - r, cy - r, cx + r, cy + r), 155, 270, fill=(255, 92, 160, 24))
    od.arc((cx - r, cy - r, cx + r, cy + r), 155, 270, fill=(255, 92, 160, 220), width=5)
    img.paste(Image.alpha_composite(img.convert('RGBA'), over).convert('RGB'))


SHEETS = {}                             # each phone drawn, by name: its title and its body


def paras(*texts, width=36):
    return [textwrap.fill(t, width) for t in texts]


def phone_body(img):
    B = 34
    W, H = PW + 2 * B, PH + 2 * B
    body = Image.new('RGB', (W, H), (232, 228, 220))
    mask = Image.new('L', (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, W - 1, H - 1), radius=130, fill=255)
    body.paste(Image.new('RGB', (W, H), (28, 28, 32)), (0, 0), mask)
    smask = Image.new('L', (PW, PH), 0)
    ImageDraw.Draw(smask).rounded_rectangle((0, 0, PW - 1, PH - 1), radius=100, fill=255)
    body.paste(img, (B, B), smask)
    return body


def save(img, name, title, side, rnd='P33'):
    chrome(ImageDraw.Draw(img))
    body = phone_body(img)
    head = 170
    sheet = Image.new('RGB', (body.width + 80 + 640, body.height + head + 50), (232, 228, 220))
    sheet.paste(body, (40, head))
    sd = ImageDraw.Draw(sheet)
    y = head + 60
    for para in side:
        sd.multiline_text((body.width + 80, y), para, font=GOTHIC(30), fill=(150, 30, 90), spacing=10)
        y = sd.multiline_textbbox((body.width + 80, y), para, font=GOTHIC(30), spacing=10)[3] + 44
    sd.text((40, 40), f'{name} — {title}', font=GOTHIC(54), fill=(30, 30, 30))
    sd.text((40, 112), 'Poco X6 Pro upright, 1220×2712 at 446 ppi (69.5 × 154.5 mm); the pink is the mock-up\'s notes',
            font=GOTHIC(30), fill=(110, 110, 110))
    sheet.save(os.path.join(OUT, f'{rnd.lower()}-{name}.png'))
    SHEETS[name] = (title, body)


# --- the variants ----------------------------------------------------------------------------------------------------

def variant(name, background, buttons, title, side, laid_logo=True, layout=None, rnd='P33'):
    img = background()
    if laid_logo:
        lay_logo(img, len(buttons))
    rows(img, buttons, layout)
    menu_cell(ImageDraw.Draw(img))
    reach(img)
    save(img, name, title, side, rnd)


def row_notes(layout=None):
    icon_mm = mm(ROW_H - 22)
    if layout is None:
        place = ('the character in a slot at the row\'s left, at most '
                 f'{icon_mm} high (each at most twice its CG pixels), lit (CG 302) without its word; the button\'s word '
                 f'in the brush face, {mm(WORD_SIZE)}, in the rest. Start red at the right thumb.')
    else:
        left, widest, words = column()
        place = (f'the characters lit (CG 302), at most {icon_mm} high, as PE5\'s, in a column {mm(widest)} wide '
                 f'({widest} px, the widest of the eight: Alice) — ' +
                 ('their middles on one line' if layout == 'axis' else 'their right edges on one line') +
                 f'; the words left-aligned {mm(WORD_GAP)} right of the column, in the brush face, {mm(WORD_SIZE)}. '
                 f'The column and the words centred in a row as one (the column from {left} px, the words from '
                 f'{words}; the same at every count of rows). Start red at the right thumb.')
    return (f'The rows at PE2\'s places, taller for the characters: {mm(ROW_H)} (PE2 {mm(130)}), {mm(ROW_GAP)} apart; '
            + place,
            'The plates let the background through a little (88%); no panel round them.',
            'A tap on a row is the click on its button as now; a long press the right click, its description '
            '(PEd): the picture no longer carries the characters, so their targets are the rows alone.')


def pe3(buttons, name):
    k = PH / 400
    variant(name, background_pe3, buttons, 'the whole picture covering the screen', paras(
        f'{name} — the owner\'s words as they stand: CG 301 (the title\'s background, the characters off it) scaled '
        f'to cover the screen, its proportions kept: ×{k:.2f} of its pixels, its middle {PW / k:.0f} of 640 '
        'columns shown — the logo\'s middle letters alone, cut at the screen\'s sides; the friezes '
        f'{mm(16 * k)} high at the top and the foot.',
        *row_notes()), laid_logo=False)


def pe4(buttons, name):
    k = PH / TEXTURE_IMG.height
    variant(name, background_pe4, buttons, 'the texture covering the screen, the logo laid whole', paras(
        f'{name} — CG 301\'s texture alone, its logo painted out (the rows over and under it), scaled to cover the '
        f'screen: ×{k:.2f}, its middle {PW / k:.0f} of 640 columns, smoothed — the stone grain coarse.',
        f'The logo cut from CG 301 by its colours (they are not the texture\'s) and laid whole at ×{LOGO_K} '
        f'({mm(LOGO.width * LOGO_K)} wide), in the middle of the room over the rows.',
        *row_notes()))


def pe5(buttons, name):
    k = PW / TEXTURE_IMG.width
    variant(name, background_pe5, buttons, 'the texture at its own scale over the screen, the logo laid whole', paras(
        f'{name} — CG 301\'s texture at the picture\'s own scale (×{k:.2f}, as PE2 shows it at the screen\'s '
        'width), its logo painted out, continued over and under itself by its mirror image; the friezes at the '
        'screen\'s top and foot. The grain as sharp as now.',
        f'The logo laid whole at ×{LOGO_K} ({mm(LOGO.width * LOGO_K)} wide), in the middle of the room over the '
        'rows.',
        *row_notes()))


def pe6(buttons, name, layout):
    """Round P34: PE5 with the characters in one column nearer the row's middle, the words left-aligned right of it."""
    title = ('PE5, the characters\' middles on one line, the words at their right' if layout == 'axis' else
             'PE5, the characters\' right edges on one line, the words at their right')
    variant(name, background_pe5, buttons, title, paras(
        f'{name} — PE5 as the owner changed it: the characters in one column nearer the middle, the words left-aligned '
        'right of them (not in the middle of the row). ' +
        ('Each character centred on the column\'s middle, so the narrow ones (Please, Spy) leave a gap to their word.'
         if layout == 'axis' else
         'Each character\'s right edge at the column\'s, so every word stands the same distance from its character; '
         'the narrow ones (Please, Spy) start further right.'),
        'The background and the logo PE5\'s.',
        *row_notes(layout)), layout=layout, rnd='P34')


def gallery_dump(name):
    return Image.open(os.path.join(GALLERY, 'phone-upright', 'current', name + '.png')).convert('RGB').resize(
        (PW, PH), Image.LANCZOS)


def now(name, key):
    SHEETS[key] = ('PE2 as built', phone_body(gallery_dump(name)))


def cgs_sheet():
    """The title's CGs and the cut: 301, 303 and 302 at twice their pixels, the words' plates marked; the characters cut
    from 302 and 303."""
    k = 2
    pad, head = 40, 120
    w = 640 * k
    sheet = Image.new('RGB', (pad * 3 + 2 * w, head + 3 * (400 * k + 90) + 560), (232, 228, 220))
    sd = ImageDraw.Draw(sheet)
    sd.text((pad, 36), 'The title\'s CGs (ｽﾀMENU.ADV:10-14) and the characters cut without their words',
            font=GOTHIC(48), fill=(30, 30, 30))
    panels = [(301, BG.convert('RGB'), 'CG 301 at (640, 0): the background alone — texture, friezes, logo'),
              (303, cg(303).convert('RGB'), 'CG 303 at (640, 400): every button unlit, faded — the words in the picture'),
              (302, cg(302).convert('RGB'), 'CG 302 at (0, 400): every button lit — a box copied whole while hovered')]
    y = head
    for n, im, caption in panels:
        big = im.resize((w, 400 * k), Image.NEAREST)
        sheet.paste(big, (pad, y + 50))
        sd.text((pad, y), caption, font=GOTHIC(34), fill=(30, 30, 30))
        if n != 301:
            for b, (bx, by, bw, bh) in BOXES.items():
                sd.rectangle((pad + bx * k, y + 50 + by * k, pad + (bx + bw) * k, y + 50 + (by + bh) * k),
                             outline=(60, 160, 255), width=3)
            for b, (cx0, cy0, cx1, cy1) in CAPTIONS.items():
                sd.rectangle((pad + cx0 * k - 3, y + 50 + cy0 * k - 3, pad + cx1 * k + 3, y + 50 + cy1 * k + 3),
                             outline=NOTE, width=5)
        else:
            sd.rectangle((pad + 116 * k, y + 50 + 132 * k, pad + 527 * k, y + 50 + 266 * k), outline=NOTE, width=5)
        y += 400 * k + 90
    # Right column: the notes.
    x = pad * 2 + w
    ty = head
    for para in paras(
            'Blue: the buttons\' boxes (ｽﾀMENU.ADV:805) the title copies whole — from 303 at the start, from 302 '
            'while the pointer is over one. Pink: each button\'s word, a speech-bubble plate drawn into the same '
            'picture, inside the same box.',
            'So the words are part of the button CGs, not text and not sprites of their own. But each is a separate '
            'group of pixels beside its character: where 302 differs from 301, Alice, B.G.M, C.G, Load, Please and '
            'Start are two groups each (character, word), Destiny three (the girl on her saucer, the small creature, '
            'the word). Spy\'s plate touches her skirt by its tail alone (3 rows), cut under the skirt.',
            'So every character is cut out exactly from the player\'s CG at run time — no hand painting, nothing '
            'shipped: 302\'s pixels that differ from 301, less the word\'s group (below, at twice their pixels).',
            'The logo on 301 (pink) is drawn in palette entries the texture never uses (the texture: nine entries), '
            'so it too is cut out exactly, and the texture can be shown without it.', width=46):
        sd.multiline_text((x, ty), para, font=GOTHIC(32), fill=(150, 30, 90), spacing=10)
        ty = sd.multiline_textbbox((x, ty), para, font=GOTHIC(32), spacing=10)[3] + 40
    # The characters cut from 302 (the rows' pick: 303 is the same faded), on a lacquer row's colour.
    y += 10
    sd.text((pad, y), 'Cut from CG 302 (lit), the rows\' pick — 303 is the same, faded', font=GOTHIC(34),
            fill=(30, 30, 30))
    x = pad
    y += 56
    tallest = max(CHARACTERS[n].height for n in ORDER) * k + 16
    for n in ORDER:
        lit = CHARACTERS[n]
        cw = lit.width * k + 30
        cell = Image.new('RGB', (cw, tallest), ROW)
        a = lit.resize((lit.width * k, lit.height * k), Image.NEAREST)
        cell.paste(a, ((cw - a.width) // 2, tallest - 8 - a.height), a)
        sheet.paste(cell, (x, y))
        sd.text((x + cw // 2, y + tallest + 24), WORDS[n], font=GOTHIC(30), fill=(30, 30, 30), anchor='mm')
        x += cw + 20
    sheet.save(os.path.join(OUT, 'p33-cgs.png'))


def overview(file, names, scale=0.4):
    """The phones `names` side by side, each under its name and title."""
    tw, th = (round(v * scale) for v in SHEETS[names[0]][1].size)
    o = Image.new('RGB', (len(names) * (tw + 30) + 30, th + 150), (232, 228, 220))
    od = ImageDraw.Draw(o)
    for i, name in enumerate(names):
        title, body = SHEETS[name]
        x = 30 + i * (tw + 30)
        o.paste(body.resize((tw, th), Image.LANCZOS), (x, 120))
        od.text((x, 30), name.split(' ')[0], font=GOTHIC(40), fill=(30, 30, 30))
        od.text((x, 80), textwrap.shorten(title, round(44 * scale / 0.4)), font=GOTHIC(22), fill=(90, 90, 90))
    o.save(os.path.join(OUT, file))


now('title-clears', 'now')
now('title', 'now (first launch)')
for buttons, suffix in ((ORDER, ''), (FIRST, 'n')):
    pe3(buttons, 'PE3' + suffix)
    pe4(buttons, 'PE4' + suffix)
    pe5(buttons, 'PE5' + suffix)
    pe6(buttons, 'PE6' + suffix, 'axis')
    pe6(buttons, 'PE7' + suffix, 'flush')
cgs_sheet()
overview('p33-overview-clears.png', ['now', 'PE3', 'PE4', 'PE5'])
overview('p33-overview-first.png', ['now (first launch)', 'PE3n', 'PE4n', 'PE5n'])
overview('p34-overview-clears.png', ['PE5', 'PE6', 'PE7'], 0.5)
overview('p34-overview-first.png', ['PE5n', 'PE6n', 'PE7n'], 0.5)
print('sheets in', OUT)
