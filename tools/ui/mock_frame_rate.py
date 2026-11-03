"""Mock-ups of the frame rate as an option (owner, 2026-11-03; docs/ui/options.md, "Frame rate"): 30, 60 or the screen's own
rate, 30 by default on Android and 60 on the PC, to save the phone's battery (docs/memory.md).

Drawn over the screen gallery's dumps of the options window (docs/ui/gallery.md; tools/ui/gallery.ps1 -PhoneLand -PhoneUp):
options-display.png and options-game.png of the lacquer, phone-landscape and phone-upright passes.
    python -I tools/ui/mock_frame_rate.py <gallery dir> <out dir> < /dev/null
A: in Display, which is full to its buttons on the PC and the phone held landscape: on the PC the palette's row shared
with the frame rate, held landscape the margins' field halved and the frame rate's tiles beside it, held upright a section
after the margins (what follows moves down). B: a section of its own in Game, nearly empty on every device, whose
Defaults then has something to reset. The pink text is the mock-ups' notes, not part of a screen.
Round 2 (the owner chose A, 2026-11-03, with Display in one column on the PC and held landscape, scrolled; nothing
squeezed to save room): A2, the sections one under another in the upright's order — held landscape the orientation, the
margins and the frame rate, then the font, the palette, the thousands and the row heights; on the PC the frame rate, then
the rest as now —, the sheet scrolled between the tabs and the foot, the reference scroll bar at its right; each shown at
its top and at its end.
The images hold the player's CGs (the map behind the window): keep <out dir> outside the repository.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

GALLERY, OUT = sys.argv[1:3]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2                                   # drawn at twice the dump's pixels, then shrunk

# The options window's paper (sampled from the dumps; godot/Strategy/UiPalette.cs).
PAPER, FIELD, FIELD_LINE, RULE = (224, 208, 172, 255), (236, 224, 194, 255), (150, 132, 104, 255), (192, 170, 132, 255)
BUTTON, LACQUER = (196, 176, 136, 255), (21, 16, 12, 255)
INK, INK_QUIET, RADIO_LINE = (40, 28, 18, 255), (100, 80, 56, 255), (150, 132, 104, 255)
HEAD_INK, VERMILION, VERMILION_INK = (120, 40, 26, 255), (170, 40, 28, 255), (150, 52, 30, 255)
CREAM, NAME, QUIET, NOTE = (236, 226, 204, 255), (232, 199, 122, 255), (176, 160, 130, 255), (255, 92, 160, 255)

GOTHIC = os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf')
CINZEL = os.path.join(REPO, 'godot', 'fonts', 'Cinzel-VariableFont_wght.ttf')
_fonts = {}


def font(size, path=GOTHIC, weight=None):
    key = (size, path, weight)
    if key not in _fonts:
        f = ImageFont.truetype(path, round(size * S))
        if weight is not None:
            f.set_variation_by_axes([weight])
        _fonts[key] = f
    return _fonts[key]


def cinzel(size, weight=700):
    return font(size, CINZEL, weight)


class Canvas:
    """A dump at S times its pixels, with a caption band under it."""

    def __init__(self, path, caption=64):
        dump = Image.open(path).convert('RGB')
        self.w, self.h, self.caption = dump.width, dump.height, caption
        self.im = Image.new('RGB', ((self.w) * S, (self.h + caption) * S), (23, 24, 27))
        self.im.paste(dump.resize((self.w * S, self.h * S), Image.LANCZOS))
        self.d = ImageDraw.Draw(self.im, 'RGBA')

    def rect(self, r, fill=None, outline=None, width=1):
        x0, y0, x1, y1 = r
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill, outline=outline, width=round(width * S))

    def text(self, xy, s, f, fill=INK, anchor='ls'):
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=fill, anchor=anchor)

    def width(self, s, f):
        return self.d.textlength(s, font=f) / S

    def line(self, a, b, fill, width=1):
        self.d.line([a[0] * S, a[1] * S, b[0] * S, b[1] * S], fill=fill, width=round(width * S))

    def circle(self, c, r, fill=None, outline=None, width=1):
        self.d.ellipse([(c[0] - r) * S, (c[1] - r) * S, (c[0] + r) * S, (c[1] + r) * S], fill=fill, outline=outline,
                       width=round(width * S))

    def shift(self, top, bottom, by, keep_right=0):
        """Moves the dump's rows top..bottom down by `by`, cut at bottom (a section inserted at top)."""
        band = self.im.crop((0, top * S, (self.w - keep_right) * S, (bottom - by) * S))
        self.im.paste(band, (0, (top + by) * S))

    def radio(self, x, y, r, label, f, chosen=False):
        self.circle((x, y), r, fill=FIELD, outline=RADIO_LINE, width=max(1, r / 7))
        if chosen:
            self.circle((x, y), r * 0.5, fill=VERMILION)
        self.text((x + r + r * 0.9, y), label, f, INK, 'lm')

    def save(self, name, caption, note, size):
        self.rect((0, self.h, self.w, self.h + self.caption), fill=(23, 24, 27, 255))
        self.text((16, self.h + 26), caption, cinzel(size), NAME)
        self.text((16, self.h + 26 + size * 1.5), note, font(size * 0.75), QUIET)
        self.im.resize((self.w, self.h + self.caption), Image.LANCZOS).save(os.path.join(OUT, name))
        print(os.path.join(OUT, name))


def head(c, x, y, x1, title, size, rule=True):
    """A section's head: Cinzel capitals in the head's ink, a rule under them (OptionsView.SectionHead)."""
    c.text((x, y), title.upper(), cinzel(size), HEAD_INK)
    if rule:
        c.line((x, y + size * 0.6), (x1, y + size * 0.6), RULE)


HINT = 'Fewer frames draw less power; more move more smoothly.'


def pc_a():
    """The palette's row on the PC halved: the palette at the left, the frame rate at the right."""
    c = Canvas(os.path.join(GALLERY, 'lacquer', 'current', 'options-display.png'))
    c.rect((166, 286, 1116, 312), fill=PAPER)
    head(c, 166, 303, 586, 'Palette', 13)
    x = 636
    head(c, x, 303, 1114, 'Frame rate', 13)
    f = font(17)
    for label, chosen in (('30 fps', False), ('60 fps - default', True), ('Screen: 144 Hz', False)):
        c.radio(x + 10, 348, 10, label, f, chosen)
        x += 10 + 22 + c.width(label, f) + 34
    c.text((646, 388), HINT, font(13), INK_QUIET)
    c.text((640, 422 - 8), '', font(12), NOTE)
    c.save('frame-rate-A-pc.png', 'A - PC: Display, the frame rate beside the palette',
           'The screen\'s rate is read at run time (here a 144 Hz monitor as an example). 60 is the PC\'s default.', 20)


def pc_b():
    """A section of its own in Game on the PC; Defaults is lit, it has something to reset."""
    c = Canvas(os.path.join(GALLERY, 'lacquer', 'current', 'options-game.png'))
    head(c, 166, 301, 1114, 'Frame rate', 13)
    c.text((1114, 301), HINT, font(13), INK_QUIET, 'rs')
    x, f = 176, font(17)
    for label, chosen in (('30 fps', False), ('60 fps - default', True), ('Screen: 144 Hz', False)):
        c.radio(x, 336, 10, label, f, chosen)
        x += 190
    c.rect((140, 690, 230, 720), fill=PAPER)
    c.text((149, 711), 'Defaults', font(15), VERMILION_INK)
    c.save('frame-rate-B-pc.png', 'B - PC: Game, a section of its own',
           'Game is nearly empty on every device; its Defaults then resets the frame rate.', 20)


def land_a():
    """Held landscape: the margins' field halved, the frame rate's tiles beside it (the palette's and row height's kind)."""
    c = Canvas(os.path.join(GALLERY, 'phone-landscape', 'current', 'options-display.png'), caption=96)
    c.rect((100, 662, 890, 756), fill=PAPER)
    c.rect((104, 666, 488, 752), fill=FIELD, outline=FIELD_LINE, width=1.5)
    c.text((122, 706), 'Screen margins', font(27), INK)
    c.text((122, 738), 'Landscape: none · Upright: none', font(19), INK_QUIET)
    c.text((472, 709), 'Adjust…', font(24), VERMILION_INK, 'rm')
    c.rect((500, 666, 884, 752), fill=FIELD, outline=FIELD_LINE, width=1.5)
    c.text((516, 706), 'Frame rate', font(27), INK)
    c.text((516, 738), 'fps; 30 - default', font(19), INK_QUIET)
    x = 676
    for label, chosen in (('30', True), ('60', False), ('120', False)):
        c.rect((x, 680, x + 60, 738), fill=PAPER, outline=VERMILION if chosen else FIELD_LINE, width=3 if chosen else 1.5)
        c.text((x + 30, 709), label, font(26), INK, 'mm')
        x += 68
    c.save('frame-rate-A-landscape.png', 'A - phone held landscape: Display, the margins\' field halved',
           'Tiles as the palette\'s and the row height\'s, the chosen framed in vermilion; 120 is the screen\'s own rate.', 30)


def land_b():
    c = Canvas(os.path.join(GALLERY, 'phone-landscape', 'current', 'options-game.png'), caption=96)
    head(c, 104, 330, 1704, 'Frame rate', 22)
    c.text((1704, 330), HINT, font(20), INK_QUIET, 'rs')
    x, f = 120, font(27)
    for label, chosen in (('30 fps - default', True), ('60 fps', False), ('Screen: 120 Hz', False)):
        c.radio(x, 392, 16, label, f, chosen)
        x += 420
    c.rect((930, 690, 1170, 730), fill=BUTTON)
    c.text((1050, 710), 'DEFAULTS', cinzel(24), HEAD_INK, 'mm')
    c.save('frame-rate-B-landscape.png', 'B - phone held landscape: Game, a section of its own',
           'The radios as Display\'s thousands; Defaults lit.', 30)


ROWS = (('30 fps - default', 'Saves the battery.', True), ('60 fps', 'Smoother effects and scrolling.', False),
        ('Screen: 120 Hz', 'As fast as the screen: the most power.', False))


def boxes(c, y):
    """The orientation's kind of rows held upright: a box each, the radio, the name and a hint under it."""
    for name, hint, chosen in ROWS:
        c.rect((27, y, 581, y + 70), fill=FIELD, outline=FIELD_LINE, width=1)
        c.radio(49, y + 26, 11, '', font(19), chosen)
        c.text((76, y + 33), name, font(19), INK)
        c.text((76, y + 57), hint, font(14), INK_QUIET)
        y += 75
    return y


def up_a():
    """Held upright: a section after the margins; what follows moves down, the sheet scrolls."""
    c = Canvas(os.path.join(GALLERY, 'phone-upright', 'current', 'options-display.png'), caption=120)
    top, end = 690, 1240
    c.shift(top, end, 297, keep_right=30)
    c.rect((12, top, 580, top + 297), fill=PAPER)
    head(c, 27, top + 14, 581, 'Frame rate', 14)
    boxes(c, top + 43)
    c.save('frame-rate-A-upright.png', 'A - phone held upright: Display',
           'After the margins, as the orientation\'s rows.', 22)


def up_b():
    c = Canvas(os.path.join(GALLERY, 'phone-upright', 'current', 'options-game.png'), caption=120)
    head(c, 27, 395, 581, 'Frame rate', 14)
    boxes(c, 414)
    c.rect((14, 1270, 204, 1306), fill=LACQUER)
    c.text((109, 1288), 'DEFAULTS', cinzel(17), CREAM, 'mm')
    c.save('frame-rate-B-upright.png', 'B - phone held upright: Game', 'The same rows under the game folder; Defaults lit.', 22)


PAPER_HEAD, INK_FAINT, THUMB_LINE = (196, 176, 136, 255), (130, 112, 86, 255), (182, 113, 97, 255)


class Sheet(Canvas):
    """A tab's column taller than its sheet, on the paper, in the dump's places at no scroll: bands cut from the dump and new sections."""

    def __init__(self, w, h):
        self.w, self.h, self.caption = w, h, 0
        self.im = Image.new('RGB', (w * S, h * S), PAPER[:3])
        self.d = ImageDraw.Draw(self.im, 'RGBA')

    def band(self, c, box, at):
        """The pristine dump's box (x0, y0, x1, y1) of canvas c pasted with its top left at `at`."""
        x0, y0, x1, y1 = box
        self.im.paste(c.im.crop((x0 * S, y0 * S, x1 * S, y1 * S)), (round(at[0] * S), round(at[1] * S)))


def scroll_bar(c, right, top, bottom, width, inset, rim, least, shown, at):
    """The reference scroll bar on the paper (UiPalette.DrawScrollBar, paper: true): the groove, the thumb and its lit line."""
    x0, r = right - width, width / 2
    c.d.rounded_rectangle([x0 * S, top * S, right * S, bottom * S], radius=r * S, fill=INK_FAINT)
    c.d.rounded_rectangle([(x0 + rim) * S, (top + rim) * S, (right - rim) * S, (bottom - rim) * S], radius=(r - rim) * S,
                          fill=PAPER_HEAD)
    room = bottom - top - 2 * inset
    h = min(room, max(least, room * shown))
    ty = top + inset + (room - h) * at
    c.d.rounded_rectangle([(x0 + inset) * S, ty * S, (right - inset) * S, (ty + h) * S], radius=(r - inset) * S,
                          fill=VERMILION_INK)
    lx = x0 + inset + width / 6
    c.line((lx, ty + r), (lx, ty + h - r), THUMB_LINE, width / 10)


def show(c, sheet, view, end, scroll, bar):
    """The column through `view` (x0, y0, x1, y1) scrolled by `scroll` (0 or its end), its last line at `end`; `bar` the scroll bar's figures."""
    x0, y0, x1, y1 = view
    c.im.paste(sheet.im.crop((x0 * S, (y0 + scroll) * S, x1 * S, (y1 + scroll) * S)), (x0 * S, y0 * S))
    longest = end - y1
    right, width, inset, rim, least = bar
    scroll_bar(c, right, y0 + 8, y1 - 4, width, inset, rim, least, (y1 - y0) / (end - y0), scroll / longest)


PC_RATES = (('30 fps', 'Draws half as often: the least power.', False), ('60 fps - default', 'Smooth effects and scrolling.', True),
            ('Screen: 144 Hz', 'As often as the screen: the most power.', False))


def pc_a2():
    """The PC: the frame rate on top, the font, the palette, the thousands and the row heights under it as now; the wheel scrolls."""
    path = os.path.join(GALLERY, 'lacquer', 'current', 'options-display.png')
    pristine = Canvas(path)
    sheet = Sheet(pristine.w, 860)
    head(sheet, 166, 171, 1114, 'Frame rate', 13)
    x = 176
    for label, hint, chosen in PC_RATES:
        sheet.radio(x, 205, 10, label, font(17), chosen)
        sheet.text((x + 19, 236), hint, font(13), INK_QUIET)
        x += 330
    pitch = 133                                     # the font's head to the palette's in the dump: a section of two lines
    for top, bottom in ((150, 262), (285, 398), (418, 488), (520, 684)):
        sheet.band(pristine, (127, top, 1152, bottom), (127, top + pitch))
    view, end = (127, 128, 1152, 674), 684 + pitch + 14
    for state, scroll in (('top', 0), ('end', end - view[3])):
        c = Canvas(path)
        c.rect((127, 128, 1152, 685), fill=PAPER)
        show(c, sheet, view, end, scroll, (1146, 8, 1, 1, 24))
        c.save(f'frame-rate-A2-pc-{state}.png', f'A2 - PC: Display in one column, at its {state}',
               'The frame rate on top, the rest as now under it; the wheel scrolls the sheet between the tabs and the foot, '
               'the bar the layout\'s 8 px.' if state == 'top' else
               'Scrolled to its end: the row heights whole above Defaults and Close, which stay at the foot.', 20)


LAND_RATES = (('30 fps - default', 'Saves the battery.', True), ('60 fps', 'Smoother effects and scrolling.', False),
              ('Screen: 120 Hz', 'As fast as the screen: the most power.', False))


def land_a2():
    """Held landscape: the upright's order across the whole width; Defaults | Close at the foot as now, the margins gone from it."""
    path = os.path.join(GALLERY, 'phone-landscape', 'current', 'options-display.png')
    pristine = Canvas(path, caption=96)
    sheet = Sheet(pristine.w, 1400)
    head(sheet, 104, 131, 1704, 'Screen orientation', 22)
    x = 120
    for label, chosen in (('System', True), ('Upright', False), ('Landscape 1', False), ('Landscape 2', False)):
        sheet.radio(x, 183, 16, label, font(27), chosen)
        x += 400
    sheet.text((104, 236), 'As the phone is held, unless its rotation lock holds it.', font(19), INK_QUIET)
    head(sheet, 104, 294, 1704, 'Screen margins', 22)          # the field across the width as the upright's, its own way first
    sheet.rect((104, 322, 1704, 408), fill=FIELD, outline=FIELD_LINE, width=1.5)
    sheet.text((122, 362), 'Landscape: none', font(27), INK)
    sheet.text((122, 394), 'Upright: none', font(19), INK_QUIET)
    sheet.text((1684, 365), 'Adjust…', font(24), VERMILION_INK, 'rm')
    head(sheet, 104, 470, 1704, 'Frame rate', 22)
    x = 120
    for label, hint, chosen in LAND_RATES:
        sheet.radio(x, 522, 16, label, font(27), chosen)
        sheet.text((x + 30, 565), hint, font(19), INK_QUIET)
        x += 540
    head(sheet, 104, 630, 1704, 'Font', 22)
    sheet.band(pristine, (95, 150, 890, 262), (95, 651))                # the dump's font under its head, 501 lower
    head(sheet, 104, 802, 1704, 'Palette', 22)
    sheet.band(pristine, (915, 150, 1190, 275), (95, 823))              # the palette's tiles at the column's left, 673 lower
    sheet.band(pristine, (80, 285, 1728, 395), (80, 958))               # the thousands with their head, 673 lower
    sheet.band(pristine, (80, 405, 1728, 660), (80, 1078))              # the row heights with theirs
    view, end = (80, 92, 1728, 652), 1333 + 12
    for state, scroll in (('top', 0), ('end', end - view[3])):
        c = Canvas(path, caption=96)
        c.rect((80, 92, 1728, 664), fill=PAPER)
        c.rect((100, 662, 890, 756), fill=PAPER)                        # the margins' field left the foot
        show(c, sheet, view, end, scroll, (1724, 13.3, 2, 1.3, 40))
        c.save(f'frame-rate-A2-landscape-{state}.png', f'A2 - phone held landscape: Display in one column, at its {state}',
               'The upright\'s order across the width: orientation, margins, frame rate, then font, palette, thousands, row '
               'height; a finger scrolls it.' if state == 'top' else
               'Scrolled to its end; Defaults | Close stay at the foot, at the right thumb as now.', 30)


class Parts:
    """The PC's parts (OptionsView) on a sheet: their units k dump pixels each from (ox, oy), the column w units wide."""

    def __init__(self, sheet, k, ox, oy, w):
        self.s, self.k, self.ox, self.oy, self.w = sheet, k, ox, oy, w

    def X(self, x):
        return self.ox + self.k * x

    def Y(self, y):
        return self.oy + self.k * y

    def text(self, x, y, s, size, fill=INK, anchor='ls'):
        self.s.text((self.X(x), self.Y(y)), s, font(size * self.k), fill, anchor)

    def head(self, y, title):
        """SectionHead: the title at y + 14, the rule at y + 22 across the column."""
        self.s.text((self.X(0), self.Y(y + 14)), title.upper(), cinzel(13 * self.k), HEAD_INK)
        self.s.line((self.X(0), self.Y(y + 22)), (self.X(self.w), self.Y(y + 22)), RULE, self.k)

    def radio(self, x, y, label, on):
        centre = (self.X(x + 10), self.Y(y + 10))
        self.s.circle(centre, 10 * self.k, fill=FIELD, outline=RADIO_LINE, width=1.5 * self.k)
        if on:
            self.s.circle(centre, 5 * self.k, fill=VERMILION_INK)
        self.text(x + 30, y + 17, label, 18)

    def check(self, x, y, label, hint, on=True):
        self.s.rect((self.X(x), self.Y(y), self.X(x + 20), self.Y(y + 20)), fill=FIELD, outline=RADIO_LINE, width=1.5 * self.k)
        if on:
            points = [(self.X(x + 4) * S, self.Y(y + 10) * S), (self.X(x + 8.5) * S, self.Y(y + 15) * S),
                      (self.X(x + 16) * S, self.Y(y + 5) * S)]
            self.s.d.line(points, fill=VERMILION_INK, width=round(3 * self.k * S), joint='curve')
        self.text(x + 32, y + 16, label, 17)
        if hint:
            self.text(x + 32, y + 35, hint, 13, INK_QUIET)

    def slider(self, x0, x1, y, fraction, label=None):
        """DrawSlider: the track, filled to the knob, the knob, its value over it when given."""
        self.s.rect((self.X(x0), self.Y(y - 3), self.X(x1), self.Y(y + 3)), fill=FIELD, outline=RADIO_LINE, width=self.k)
        kx = x0 + (x1 - x0) * fraction
        self.s.rect((self.X(x0 + 1), self.Y(y - 2), self.X(kx), self.Y(y + 2)), fill=VERMILION_INK)
        self.s.circle((self.X(kx), self.Y(y)), 9 * self.k, fill=VERMILION, outline=INK, width=1.5 * self.k)
        if label:
            self.text(kx, y - 16, label, 13, INK, 'ms')

    def paste(self, c, box, x, y):
        """The dump's box (its pixels) with its top left at the parts' (x, y)."""
        self.s.band(c, box, (self.X(x), self.Y(y)))


PC = dict(path=('lacquer', 'options-'), k=1, ox=166, oy=156, w=948, view=(127, 128, 1152, 674), bar=(1146, 8, 1, 1, 24),
          caption=64, size=20, blank=[(127, 128, 1152, 685)])
LAND = dict(path=('phone-landscape', 'options-'), k=1.547, ox=104.7, oy=108, w=1033, view=(80, 92, 1728, 652),
            bar=(1724, 13.3, 2, 1.3, 40), caption=96, size=30, blank=[(80, 92, 1728, 664), (80, 652, 915, 768)])


def column_tab(device, tab, draw, what):
    """A tab in one column on `device` (PC or LAND), drawn by draw(parts, pristine dump, pc), at its top and, scrolled, at its end."""
    where, prefix = device['path']
    path = os.path.join(GALLERY, where, 'current', prefix + tab + '.png')
    pristine = Canvas(path, caption=device['caption'])
    sheet = Sheet(pristine.w, 2000)
    p = Parts(sheet, device['k'], device['ox'], device['oy'], device['w'])
    end = p.Y(draw(p, pristine, device is PC)) + 14
    view = device['view']
    states = [('top', 0)] + ([('end', end - view[3])] if end - view[3] > 60 else [])
    name = 'pc' if device is PC else 'landscape'
    for state, scroll in states:
        c = Canvas(path, caption=device['caption'])
        for box in device['blank']:
            c.rect(box, fill=PAPER)
        x0, y0, x1, y1 = view
        c.im.paste(sheet.im.crop((x0 * S, (y0 + scroll) * S, x1 * S, (y1 + scroll) * S)), (x0 * S, y0 * S))
        if end > y1:
            right, width, inset, rim, least = device['bar']
            scroll_bar(c, right, y0 + 8, y1 - 4, width, inset, rim, least, (y1 - y0) / (end - y0), scroll / (end - y1))
        device_name = 'PC' if device is PC else 'phone held landscape'
        c.save(f'frame-rate-A2-{name}-{tab}-{state}.png', f'A2 - {device_name}: {tab.capitalize()} in one column, at its {state}',
               what if state == 'top' else 'Scrolled to its end.' if end > y1 else what, device['size'])


def messages(p, c, pc):
    """
    Messages (round 2, the owner): the text speed and the message skip in one row of two columns as now, under them in the
    column the mouse (the PC's) and the read text.
    """
    if pc:
        p.paste(c, (127, 140, 1152, 445), (127 - p.ox) / p.k, (140 - p.oy) / p.k)
        p.s.rect((660, 360, 1152, 460), fill=PAPER)                     # the mouse leaves the right column
        y = 330
    else:
        p.paste(c, (80, 100, 1728, 412), (80 - p.ox) / p.k, (100 - p.oy) / p.k)
        y = (412 - p.oy) / p.k + 24
    if pc:
        p.head(y, 'Mouse')
        p.check(0, y + 38, 'Mouse movement', 'After a menu, the pointer glides back where it was.')
        y += 110
    p.head(y, 'Already-read text')
    if pc:
        p.paste(c, (160, 518, 806, 576), -6, y + 34)
    else:                                                               # the phone's two rows of tiles in one
        p.paste(c, (100, 466, 772, 546), -3, y + 36)
        p.paste(c, (100, 548, 675, 622), -3 + 675 / p.k, y + 36 + 5 / p.k)
    p.text(0, y + 128, 'Salmon', 17)
    p.text(0, y + 150, 'A message window you have read before, in any game, shows its text in this colour.', 13, INK_QUIET)
    p.text(0, y + 170, 'The battle\'s balloons too, darker on white; menus and our screens keep theirs. Forget them… counts them unread.',
           13, INK_QUIET)
    return y + 176


TIMINGS = (('Armies: slide', 0.048, '0.15 s'), ('Spotlight: darkness', 0.5, '50 %'), ('Cards: counters', 0.198, '0.60 s'),
           ('Counters: run', 0.43, '1.30 s'), ('Results: hold', 0.238, '1.20 s'), ('Opening: appear', 0.115, '0.35 s'),
           ('Opening: leave', 0.166, '0.50 s'), ('Cards: slide in', 0.33, '1.00 s'), ('Strip: fly in', 0.15, '0.15 s'))


def battle(p, c, pc):
    """Battle: the effects, the timings one under another across the column, in their order."""
    w = p.w
    p.head(0, 'Effects')
    p.check(0, 36, 'Exchange effects',
            'Our attack effects over the scene from an exchange\'s shots, after Sengoku Rance\'s (melee, bow, magic). None while skipping.')
    p.head(96, 'Timings')
    for i, (label, fraction, value) in enumerate(TIMINGS):
        cy = 132 + i * 46
        p.text(0, cy + 13, label, 14)
        p.slider(0, w - 80, cy + 29, fraction)
        p.text(w, cy + 34, value, 14, INK, 'rs')
    return 132 + len(TIMINGS) * 46 - 6


def tweaks(p, c, pc):
    """Tweaks held landscape as the PC's: the battle's three, the choices across, the scenes."""
    w, tx = p.w, 32
    p.text(0, 14, 'Each setting here changes how the original game plays. Everything else keeps the original\'s rules.', 14, INK_QUIET)
    y = 44
    p.head(y, 'Battle')
    p.check(0, y + 38, 'Damage forecast',
            'Each attack banner shows the damage it is likely to deal: the target\'s troops and its commander\'s HP, 9 times in 10.')
    p.text(tx, y + 120, 'Chaos and Nikkou reach demons', 17)
    p.text(tx, y + 139, 'Against a demon commander, Chaos\'s or Nikkou\'s holder fights in person even when his troops exceed the '
           'battle\'s limit.', 13, INK_QUIET)
    for i, label in enumerate(('Off — as the original', 'On the field', 'Out of reach')):
        p.radio(tx + i * ((w - tx) / 3), y + 154, label, i == 0)
    p.text(tx, y + 200, 'On the field: the demon\'s troops may shoot at him, as at any commander in the fight. Out of reach: they '
           'never do.', 13, INK_QUIET)
    p.text(tx, y + 247, 'Ninja follow-up', 17)
    p.text(tx, y + 266, 'After Menad, Hubert, Gandhi or Magic attacks, their ninja may strike the enemy commander for 1 HP. Ninja '
           'never kills.', 13, INK_QUIET)
    for i, label in enumerate(('33% — as the original', '67%', '100%')):
        p.radio(tx + i * ((w - tx) / 3), y + 281, label, i == 0)
    y += 338
    p.head(y, 'Scenes')
    p.check(0, y + 38, 'New scenes first', 'Talks and harem visits play unread scenes first, with a few hints and a choice of ours.')
    return y + 84


for draw in (pc_a, pc_b, land_a, land_b, up_a, up_b, pc_a2, land_a2):
    draw()
column_tab(PC, 'messages', messages, 'The text speed and the message skip side by side as now; the mouse and the read text under them.')
column_tab(PC, 'battle', battle, 'The timings one under another, each across the column; it scrolls a little.')
column_tab(LAND, 'messages', messages, 'The text speed and the message skip side by side as now; the read text across under them, its tiles in one row.')
column_tab(LAND, 'battle', battle, 'As the PC\'s at the phone\'s type.')
column_tab(LAND, 'tweaks', tweaks, 'The PC\'s one column at the phone\'s type: the choices across, the hints on one line.')
