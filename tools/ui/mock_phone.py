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
enemy attack (touch/current's enemy-attack-scouted, Maris's and Kanami's face CGs), PW1-PW2 the week report. Round P5: PH1-PH2
Harem, PT1-PT2 Treasure, PL1-PL2 Save/Load (touch/current's harem, treasure-items, save-load). Round
P6: PO1-PO2 the options window, PF1-PF2 the dungeon's menus (options-messages, options-tweaks, floor-ladder-middle).
Round P7: PB4o1-PB4o2 the opening, PB4s the scout report, PB4c the strategy's cut-in, PB4r the results plate,
PB4b a fighter's line (opening, scout-report, strategy, battle-speech).
Round P8, the phone held landscape (2712×1220, the layout fitted to the height with black sides): L0b, L0m today's
tablet layout at that size; LM1-LM2 the map (map-markers, province-menu), LB1-LB2 the battle (battle-turn).
Round P9: LD1-LD2 the deployment table, LU1-LU2 the Subordinates screen, landscape. Round P10: LA1-LA2 the enemy
attack (enemy-attack-scouted), LW1-LW2 the week report (week-report and the map's pictures), landscape. Round P11:
LH1-LH2 Harem, LT1-LT2 Treasure, LL1-LL2 Save/Load, landscape (harem, treasure-items, save-load). Round P12:
LO1-LO2 the options window, LF1-LF2 the dungeon's menus, landscape (options-messages, floor-ladder-middle,
dungeon-entry, map). Round P13, the battle's other states on LB2: LB2r1-LB2r2 the retreat question, LB2x an exchange,
LB3o1-LB3o2 the opening, LB3s the scout report, LB3c the strategy's cut-in, LB3r the results plate, LB3b a fighter's
line (battle-turn, exchange-counters, opening, scout-report, strategy, info-results, battle-speech).
Round P14, the scenes again: PS2 as the owner changed it — the picture in the screen's middle, the upper speaker's
window above it (PS2), the lower one's under it (PS2l), and PS2b, PS2m with the picture there too (the look dir).
Round P15, the scenes landscape: LS1 the frame as on the PC (LS1b window 131, LS1m a dialogue menu at the thumb), LS2
upright's PS2 laid sideways (the look dir; lacquer/current's message-cg-bar-speech and scene-menu).
`--round P3` before the arguments draws only that round's variants (the look dir then unread, '-' will do; P14 and
P15 read it).
The pink lines and text are the mock-ups' notes, not part of a screen.

For the screen gallery's phone tabs (tools/ui/gallery.ps1 -Phone; docs/ui/gallery.md, §9) only the owner's chosen base
versions, without the notes, into <gallery dir>/phone/current (upright) and <gallery dir>/phone-land/current
(landscape), each with a manifest.json as the game's passes write it; the scenes' PS2 and PS2l only when the look's
frames LOOK_SPEECH and LOOK_NARRATION are copied into <gallery dir>/phone-look:
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
LOOK_SPEECH, LOOK_NARRATION, LOOK_INTERCEPT = '00058-message-speech-left.png', '00059-key.png', '00060-key.png'
if GALLERY_MODE:                        # the scenes' look frames, if copied beside the passes (<gallery dir>/phone-look)
    GALLERY, EXTRACT = ARGS[1:3]
    LOOK, OUT = os.path.join(GALLERY, 'phone-look'), os.path.join(GALLERY, 'phone', 'current')
    if not all(os.path.exists(os.path.join(LOOK, f)) for f in (LOOK_SPEECH, LOOK_NARRATION)):
        LOOK = None
else:
    GALLERY, LOOK, EXTRACT, OUT = ARGS[0:4]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
TOUCH, LACQ = os.path.join(GALLERY, 'touch', 'current'), os.path.join(GALLERY, 'lacquer', 'current')
os.makedirs(OUT, exist_ok=True)

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
    d.text(((x0 + x1) / 2, (y0 + y1 - strip) / 2), label, font=fit(d, label, BRUSH, 64, x1 - x0 - 30),
           fill=(44, 26, 10), anchor='mm')
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
    # Round P14's choice (owner, 2026-10-28; docs/ui/phone.md, §18): the scenes, PS2 changed.
    'PS2': ('ps2-upper', 'Scenes — PS2', 'The upper speaker', 'the picture in the middle, Ex\'s window above it'),
    'PS2l': ('ps2-lower', 'Scenes — PS2', 'The lower speaker', 'the Leazas White Knight\'s window under the picture'),
    'PS2b': ('ps2-cg', 'Scenes — PS2', 'Window 131 under its CG', 'the CG (masked) in the middle, the speaker on a '
                                                                  'plate'),
    'PS2m': ('ps2-menu', 'Scenes — PS2', 'A dialogue menu', 'the rows at the thumb (made up)'),
    'PM1a': ('pm1-map', 'Map — PM1', 'The map wait', 'the map\'s whole height, the status on top, Commands at the thumb'),
    'PM1b': ('pm1-commands', 'Map — PM1', 'The command panel', 'a sheet over the map\'s foot, Close where Commands was'),
    'PM1c': ('pm1-province', 'Map — PM1', 'A province', 'Oaks (6) ringed, its card and commands as a sheet; the map '
                                                          'scrolled up'),
    'PB3': ('pb3-turn', 'Battle — PB3', 'The player\'s turn', 'our army on top, the enemy\'s at the thumb, Skip over '
                                                               'the deck'),
    'PB3r': ('pb3-retreat', 'Battle — PB3', 'The retreat question', 'at Retreat, over the deck\'s right column'),
    'PB3x': ('pb3-exchange', 'Battle — PB3', 'An exchange', 'Ex (commander 1) on Rance (unit 1); the deck empty'),
    'PB4s': ('pb4-scout', 'Battle — PB3', 'The scout report', 'Kanami\'s report before the field, one tap'),
    'PB4c': ('pb4-strategy', 'Battle — PB3', 'A strategy\'s cut-in', 'Barres (unit 6), his line under the band'),
    'PB4r': ('pb4-results', 'Battle — PB3', 'The results plate', 'over the dimmed board at the screen\'s width'),
    'PB4b': ('pb4-line', 'Battle — PB3', 'A fighter\'s line', 'our balloon over the scene\'s foot, rewrapped'),
    # Round P3-P7's choices (owner, 2026-10-27; docs/ui/phone.md, §5).
    'PD2': ('pd2-deployment', 'Deployment — PD2', 'The deployment table', 'the party on top, the ledger at the thumb '
                                                                           '(how the ledger scrolls is still open)'),
    'PD2d': ('pd2-picture', 'Deployment — PD2', 'A unit\'s picture', 'a long press on a row'),
    'PU1': ('pu1-subordinates', 'Subordinates — PU1', 'The orders', 'Replenish and Talk on one row, End alone (its '
                                                                    'place still open)'),
    'PU1q': ('pu1-question', 'Subordinates — PU1', 'Replenish asks first', 'a callout under its button (Haurein)'),
    'PA1': ('pa1-attack', 'Enemy attack — PA1', 'The attack on Ys', 'a screen of its own; Abandon City narrower'),
    'PA1n': ('pa1-ninjas', 'Enemy attack — PA1', 'Ninja Scout\'s list', 'above its button'),
    'PW1': ('pw1-report', 'Week report — PW1', 'The week report', 'the card in the middle over the dimmed map'),
    'PH2': ('ph2-harem', 'Harem — PH2', 'The harem', 'the picture on top, the grid scrolled'),
    'PT2': ('pt2-treasure', 'Treasure — PT2', 'The treasure', 'the item\'s picture on top, the grid scrolled'),
    'PL2': ('pl2-saves', 'Save/Load — PL2', 'Load', 'one page: the list down to the save\'s page on the bar'),
    'PO1': ('po1-options', 'Options — PO1', 'The options window', 'the paper sheet, its tabs on top'),
    'PF1': ('pf1-ladder', 'Dungeon — PF1', 'The floor ladder', 'the tablet\'s rail at the right thumb'),
    'PF1e': ('pf1-entry', 'Dungeon — PF1', 'Before a search', 'Start Search in Descend\'s corner'),
}
# The gallery's landscape phone tab (phone-land): the owner's chosen landscape base versions (round P8 on).
GALLERY_ENTRIES_LAND = {
    'LM1a': ('lm1-map', 'Map — LM1', 'The map wait', 'the frame kept, the black sides as rails'),
    'LM1b': ('lm1-commands', 'Map — LM1', 'The command panel', 'in the sides, two-line rows; Close where Commands was'),
    'LM1c': ('lm1-province', 'Map — LM1', 'A province', 'Oaks (6)\'s card beside it, its commands on the right side'),
    'LB2': ('lb2-turn', 'Battle — LB2', 'The player\'s turn', 'the scene lower, the cards taller; the rails in the sides'),
    'LD2': ('ld2-deployment', 'Deployment — LD2', 'The deployment table', 'the ledger at the left, the party two a '
                                                                           'row at the right'),
    'LU1': ('lu1-subordinates', 'Subordinates — LU1', 'The orders', 'Add New Troops left; Talk, Replenish, End right'),
    'LU1q': ('lu1-question', 'Subordinates — LU1', 'Replenish asks first', 'a callout left of its button (Haurein)'),
    # Round P10's choices (owner, 2026-10-27; docs/ui/phone.md, §14).
    'LA2': ('la2-attack', 'Enemy attack — LA2', 'The attack on Ys', 'across the whole width, the answers in a column '
                                                                      'at the right thumb'),
    'LA2n': ('la2-ninjas', 'Enemy attack — LA2', 'Ninja Scout\'s list', 'five ninjas in two columns (made up)'),
    'LW1': ('lw1-report', 'Week report — LW1', 'The week report', 'the card in the middle over the dimmed map'),
    # Round P11's choices (owner, 2026-10-28; docs/ui/phone.md, §15).
    'LH2': ('lh2-harem', 'Harem — LH2', 'The harem', 'the grid folded in two, every girl in view'),
    'LT2': ('lt2-treasure', 'Treasure — LT2', 'The treasure', 'the grid folded in two, every item in view'),
    'LL1': ('ll1-saves', 'Save/Load — LL1', 'Load', 'the PC\'s split: the list left, the save\'s page right'),
    # Round P12's choices (owner, 2026-10-28; docs/ui/phone.md, §16).
    'LO1': ('lo1-options', 'Options — LO1', 'The options window', 'the paper sheet across the width, its tabs on top'),
    'LF2': ('lf2-ladder', 'Dungeon — LF2', 'The floor ladder', 'the tablet\'s rail kept in the frame'),
    'LF2e': ('lf2-entry', 'Dungeon — LF2', 'Before a search', 'Return over Start Search, both in one line'),
    # Round P13's choices (owner, 2026-10-28; docs/ui/phone.md, §17).
    'LB2r1': ('lb2-retreat', 'Battle — LB2', 'The retreat question', 'a callout beside Retreat, in the frame'),
    'LB2x': ('lb2-exchange', 'Battle — LB2', 'An exchange', 'Ex (commander 1) on Rance (unit 1); the counters in the '
                                                           'scene\'s blurred sides'),
    'LB3o1': ('lb3-opening', 'Battle — LB2', 'The opening', 'at the scene\'s place, laid out at the frame\'s width'),
    'LB3s': ('lb3-scout', 'Battle — LB2', 'The scout report', 'Kanami\'s report across the whole width, one tap'),
    'LB3c': ('lb3-strategy', 'Battle — LB2', 'A strategy\'s cut-in', 'the PC\'s fitted to the frame, the band to the '
                                                                     'edges'),
    'LB3r': ('lb3-results', 'Battle — LB2', 'The results plate', 'in the frame\'s middle over the dimmed board'),
    'LB3b': ('lb3-line', 'Battle — LB2', 'A fighter\'s line', 'the portrait in the scene\'s blurred side, our balloon'),
}
DUMPED = []
DUMPED_LAND = []


def save_gallery(img, name):
    """The phone's screen in its body, as the gallery's entry; compared with the former picture as the game's passes do
    (GalleryScreen.Compare: a channel more than 8 apart, changed past 600 pixels). A landscape entry goes to its own
    tab's folder (phone-land)."""
    landscape = name in GALLERY_ENTRIES_LAND
    if name not in GALLERY_ENTRIES and not landscape:
        return
    ident, section, title, note_ = (GALLERY_ENTRIES_LAND if landscape else GALLERY_ENTRIES)[name]
    tab = 'phone-land' if landscape else 'phone'
    out = os.path.join(GALLERY, tab, 'current')
    os.makedirs(out, exist_ok=True)
    B = 34
    W, H = img.width + 2 * B, img.height + 2 * B
    body = Image.new('RGB', (W, H), (20, 17, 16))
    mask = Image.new('L', (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, W - 1, H - 1), radius=130, fill=255)
    body.paste(Image.new('RGB', (W, H), (28, 28, 32)), (0, 0), mask)
    smask = Image.new('L', img.size, 0)
    ImageDraw.Draw(smask).rounded_rectangle((0, 0, img.width - 1, img.height - 1), radius=100, fill=255)
    body.paste(img, (B, B), smask)
    file = ident + '.png'
    body.save(os.path.join(out, file))
    since, differing = None, 0
    previous = os.path.join(GALLERY, tab, 'previous')
    if os.path.isdir(previous):
        before = os.path.join(previous, file)
        if not os.path.exists(before):
            since = 'new'
        else:
            a = np.asarray(Image.open(before).convert('RGB'), dtype=np.int16)
            b = np.asarray(body, dtype=np.int16)
            differing = a.shape[0] * a.shape[1] if a.shape != b.shape else int((np.abs(a - b) > 8).any(axis=2).sum())
            since = 'changed' if differing > 600 else 'same'
    with open(os.path.join(out, file), 'rb') as f:
        digest = hashlib.sha256(f.read()).hexdigest()
    (DUMPED_LAND if landscape else DUMPED).append({
        'id': ident, 'section': section, 'title': title, 'save': 'mock-up', 'note': note_, 'file': file,
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
        cols = 2 if thumbs[0].width > thumbs[0].height else len(thumbs)     # the landscape phone's sheets: two a row
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


# --- the pictures --------------------------------------------------------------------------------------------------

def dump(folder, name):
    return Image.open(os.path.join(folder, name)).convert('RGB')


def look(name):
    return Image.open(os.path.join(LOOK, name)).convert('RGB').crop((0, 24, 1280, 824))   # the menu bar off


def at_width(im, width=PW):
    return im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)


def scene_clean_raw():
    """Ex's scene with no window of ours: the top of 00059 (its narration at the foot) over the foot of 00058."""
    pic = look(LOOK_NARRATION)
    pic.paste(look(LOOK_SPEECH).crop((0, 300, 1280, 800)), (0, 300))
    return pic


def scene_clean():
    return at_width(scene_clean_raw())


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


def draw_map(img, box, x0, y0, scale, plate_size, ring=None, dim=0, plates=True):
    """The map's view from map point (x0, y0) at `scale` phone pixels a map pixel into `box`, our plates over it
    (none before COMMAND's map wait: `plates`)."""
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

    for p, (mx, my) in sorted(PROV_POINTS.items(), key=lambda kv: kv[1][1]) if plates else ():
        if PROV_NAMES.get(p):
            plate(PROV_NAMES[p], mx, my, 28, True)
    for name, (mx, my), lit in DUNGEONS if plates else ():
        plate(name, mx, my, 13, lit)
    if dim:
        od.rectangle(box, fill=(0, 0, 0, dim))
    for outside in ((0, 0, img.width, by0), (0, by1, img.width, img.height), (0, 0, bx0, img.height),
                    (bx1, 0, img.width, img.height)):   # a plate across the box's edge is cut there
        if outside[2] > outside[0] and outside[3] > outside[1]:
            over.paste((0, 0, 0, 0), outside)
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

def speech_window(d, box, lines, speaker=None, size=64, tail=None, kind='speech', read=False, triangle_on=True,
                  tail_down=False):
    x0, y0, x1, y1 = box
    fill = {'speech': PANEL, 'thought': THOUGHT_FILL, 'narration': NARRATION_FILL}[kind]
    if kind == 'thought':
        d.rounded_rectangle(box, radius=36, fill=fill, outline=DIM_GOLD, width=3)
    else:
        panel(d, box, fill)
    if tail and tail_down:                      # down to its speaker in the picture under the window
        tx = tail
        d.polygon([(tx - 30, y1 - 2), (tx + 30, y1 - 2), (tx - 10, y1 + 44)], fill=fill)
        d.line([(tx - 30, y1), (tx - 10, y1 + 44), (tx + 30, y1)], fill=GOLD, width=3)
    elif tail:                                  # the speech's tail, up to its speaker in the picture
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


SCENE_Y = (TOP + BAR[0]) // 2 - 381    # the picture in the screen's middle, between the camera's strip and the bar
SPEECH_H = 420                          # a speech window keeps four lines' height, so the picture never moves
GENERAL_EX = ['General Ex!', 'The enemy’s here!!']


def d2():
    """PS2 as the owner changed it (2026-10-28): the picture in the middle, the upper portrait's speech above it."""
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    img.paste(scene_clean(), (0, SCENE_Y))
    d = ImageDraw.Draw(img)
    speech_window(d, (16, SCENE_Y - 64 - SPEECH_H, PW - 16, SCENE_Y - 64), INTERCEPT, tail=230, tail_down=True)
    bottom_bar(d)
    save(img, 'PS2', 'the upper speaker\'s window above the picture', 'scenes', side=[
        'PS2 with the owner\'s changes:\nthe picture and its portraits in\nthe screen\'s middle (between the\n'
        'camera\'s strip and the bar).',
        'The upper portrait\'s speech (Ex)\nin a window above the picture,\nits tail down at him; no name\n'
        'plate — his name is on his\nportrait.',
        f'The speech\'s lines (32 cells) at\n{mm(64)}, the script\'s breaks kept;\nthe window keeps four lines\'\n'
        f'height ({mm(SPEECH_H)}), so the picture\nnever moves.',
        'A tap anywhere goes on; Menu,\nLog and Skip in the bar.'])


def d2l():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    img.paste(scene_clean(), (0, SCENE_Y))
    d = ImageDraw.Draw(img)
    foot = SCENE_Y + 762
    speech_window(d, (16, foot + 64, PW - 16, foot + 64 + SPEECH_H), GENERAL_EX, tail=990)
    bottom_bar(d)
    save(img, 'PS2l', 'the lower speaker\'s window under the picture', 'scenes', side=[
        'The lower portrait\'s speech (the\nLeazas White Knight) in a window\nunder the picture, its tail up\n'
        'at him; no name plate.',
        'The picture stays where it was:\nthe upper window\'s room is\nempty while the lower one\nspeaks.',
        'The agent\'s choice: a thought\nby its thinker\'s portrait the\nsame way; the narration and a\n'
        'speaker with no portrait under\nthe picture.'])


def d2b():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    d.rectangle((0, SCENE_Y, PW, SCENE_Y + 762), fill=(58, 58, 62))
    for x in range(-762, PW, 40):
        d.line([(x, SCENE_Y + 762), (x + 762, SCENE_Y)], fill=(72, 72, 76), width=3)
    d.text((PW / 2, SCENE_Y + 381), 'the scene\'s CG (masked)', font=GOTHIC(44), fill=(200, 200, 200), anchor='mm')
    foot = SCENE_Y + 762
    speech_window(d, (16, foot + 64, PW - 16, foot + 64 + 330), ['The preparations are complete, King Rance.'],
                  speaker='MARIS', size=46)
    bottom_bar(d)
    save(img, 'PS2b', 'window 131 under its CG', 'scenes', side=[
        'Window 131 under its CG, the CG\nin the screen\'s middle as PS2\'s\npicture: the CG whole (on the PC\n'
        'the window covers its lower\nquarter).',
        f'131\'s lines (49 cells) at {mm(46)},\nas on the PC; its speaker on a\nplate (no portrait shows the\nname here).'])


def d2m():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    d.rectangle((0, SCENE_Y, PW, SCENE_Y + 762), fill=(0, 0, 0))
    d.text((PW / 2, SCENE_Y + 381), 'the gallery\'s plain backdrop', font=GOTHIC(36), fill=(90, 90, 90), anchor='mm')
    foot = SCENE_Y + 762
    speech_window(d, (16, foot + 64, PW - 16, foot + 64 + SPEECH_H),
                  ['The rebels gather near Oaks.', 'What shall we do, King Rance?'], speaker='MARIS', tail=990,
                  triangle_on=False)
    items = ['Attack at dawn', 'Wait for the scouts', 'Ask Maris']
    h, gap = 130, 14
    y = BAR[1] - len(items) * h - (len(items) - 1) * gap
    panel(d, (16, y - 30, PW - 16, BAR[1] + 14))
    for i, item in enumerate(items):
        row_box(d, (40, y + i * (h + gap), PW - 40, y + i * (h + gap) + h), item, size=48)
    save(img, 'PS2m', 'a dialogue menu', 'scenes', side=[
        'A `]` menu: finger rows at the\nthumb, in the script\'s order, in\nthe bar\'s place '
        f'(rows {mm(h)} high).',
        'The picture and the question\'s\nwindow where PS2 has them (the\npicture in the screen\'s middle);\n'
        'the question stays readable over\nthe rows.',
        'Made up: the gallery\'s scene\nmenu.'])


def p14():
    d2(); d2l(); d2b(); d2m()


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
    """▲ ▼ ▶ ◀: Cinzel has no arrows."""
    s = size
    pts = {'up': [(x - s, y + s), (x + s, y + s), (x, y - s)], 'down': [(x - s, y - s), (x + s, y - s), (x, y + s)],
           'right': [(x - s, y - s), (x - s, y + s), (x + s, y)],
           'left': [(x + s, y - s), (x + s, y + s), (x - s, y)]}[way]
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
    word = fit(d, 'Replenish', BRUSH, 52, x1 - x0 - 30)
    if figures:
        red_button(d, box, '', 52)
        d.text(((x0 + x1) / 2, y0 + 46), 'Replenish', font=word, fill=NAME, anchor='mm')
        d.text(((x0 + x1) / 2, y1 - 30), figures, font=fit(d, figures, GOTHIC, 32, x1 - x0 - 24), fill=TEXT,
               anchor='mm')
    else:
        d.rectangle(box, fill=(40, 34, 28), outline=OFF_TAG, width=3)
        d.text(((x0 + x1) / 2, y0 + 46), 'Replenish', font=word, fill=OFF_TEXT, anchor='mm')
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


def talk_button(img, box, n):
    """Talk with the unit, its face at the left: beside Replenish (owner, 2026-10-27), no longer in the bar."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    panel(d, box)
    fs = y1 - y0 - 28
    img.paste(unit_face(n, fs), (x0 + 14, y0 + 14))
    d = ImageDraw.Draw(img)
    cx = x0 + 14 + fs + (x1 - x0 - 14 - fs) / 2
    d.text((cx, y0 + 46), 'Talk', font=BRUSH(52), fill=NAME, anchor='mm')
    d.text((cx, y1 - 30), f'WITH {UNIT[n][1].upper()}', font=CINZEL(26, 600), fill=SUBTITLE, anchor='mm')


def end_alone(d, right='End'):
    """The bar with End alone at the right thumb; where End goes is the owner's open question (2026-10-27)."""
    y0, y1 = BAR
    panel(d, (PW - 16 - 420, y0, PW - 16, y1))
    d.text((PW - 16 - 210, (y0 + y1) / 2), right, font=BRUSH(52), fill=NAME, anchor='mm')


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
    rep = (24, orders + 100, PW // 2 - 10, orders + 236)       # Replenish and Talk on one row (owner, 2026-10-27)
    replenish(d, rep, figures)
    talk_button(img, (PW // 2 + 10, rep[1], PW - 24, rep[3]), n)
    d = ImageDraw.Draw(img)
    add_new_grid(d, 24, orders + 258, PW - 24, n)
    end_alone(d)
    return orders, row_h, rep


def pu1():
    img, d = phone()
    orders, row_h, _ = pu1_base(img, d, 1, None)
    reach(img, label=False, shade=False)
    d = ImageDraw.Draw(img)
    save(img, 'PU1', 'the ledger on top, the orders at the thumb', 'subordinates', side=[
        'The ledger as the deployment\ntable\'s (PD1): the same rows\nand tabs, the chosen unit\'s\nrow vermilion.',
        'A tap chooses the unit; its\norders below always show it.',
        f'The orders at the thumb:\nReplenish and Talk (with the\nunit\'s face) on one row, {mm(136)}\n'
        '(the owner\'s change); Add New\n'
        f'Troops ({mm(128)} buttons; greyed\nabove the budget, its reason\non a long press).',
        'End alone in the bar, at the\nright thumb; where End goes is\nstill the owner\'s question.',
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


def attacker_card(img, box, place, face_max=200):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    name, kind, troops, cell = ATTACKERS[place]
    d.rectangle(box, fill=ROW, outline=DIM_GOLD, width=2)
    fs = min(y1 - y0 - 24, face_max)
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
    low = y1 - y0 < 140                     # a low button (LA2's Intercept, 130): the two lines closer
    ty, sy = (y0 + (y1 - y0) * 0.36, y1 - (y1 - y0) * 0.2) if low else (y0 + 56, y1 - 38)
    d.text(((x0 + x1) / 2, ty), title, font=fit(d, title, BRUSH, 44, x1 - x0 - 40), fill=NAME, anchor='mm')
    d.text(((x0 + x1) / 2, sy), sub, font=GOTHIC(26 if low else 28), fill=TEXT if red else QUIET, anchor='mm')


# The answers in two rows: Abandon City (top left, farthest from the thumb) and Ninja Scout, then Intercept in the bar.
ROW1 = BAR[0] - 168
MARIS_TOP = ROW1 - 20 - 330
ANSWERS = ((16, PW // 2 - 8), (PW // 2 + 8, PW - 16))
# Abandon City 10% narrower, the room it gives up widening the gap to Ninja Scout (owner, 2026-10-27).
ABANDON = (16, 16 + round((ANSWERS[0][1] - 16) * 0.9))


def attack_answers(d, ninjas='1 of 1 free'):
    danger_button(d, (ABANDON[0], ROW1 + 10, ABANDON[1], ROW1 + 138), 'Abandon City')
    two_line_button(d, (ANSWERS[1][0], ROW1, ANSWERS[1][1], ROW1 + 148), 'Ninja Scout', ninjas)
    two_line_button(d, (16, BAR[0], PW - 16, BAR[1]), 'Intercept', 'choose the units', red=True)


def maris(img, box, size=42, face_max=170):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    panel(d, box)
    fs = min(face_max, y1 - y0 - 40)
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
        'thumb, Ninja Scout beside it;\nIntercept the bar\'s width at\nthe thumb. 7.3 and 8.4 mm.',
        f'Abandon City 10% narrower, the\ngap to Ninja Scout {mm(ANSWERS[1][0] - ABANDON[1])} (the\nowner\'s change).'])


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


# --- round P5: Harem, Treasure and Save/Load upright (2026-10-27) ----------------------------------------------------

GRID_ROWS = 17                                  # the pickers' 3 × 17 cells (Sﾊｰﾚﾑ, Sｱｲﾃﾑ)
HAREM_CELLS = {(0, 0): 'Sill', (3, 1): 'Soul', (3, 2): 'Anasel'}
ITEM_CELLS = {(1, 2): ('L. Holy Sword', 'Rance'), (2, 0): ('By Lord', 'Rick')}
CHARACTER_CELLS = {(0, 0): 'Rance', (1, 1): 'Leila', (1, 2): 'Barres', (2, 0): 'Rick', (2, 1): 'Cordoba',
                   (2, 2): 'Kinkaid', (3, 0): 'Menad', (3, 1): 'Asuka', (3, 2): 'Haurein'}


def heart(d, x, y, s=12, fill=(176, 60, 70)):
    d.ellipse((x - s, y - s, x, y), fill=fill)
    d.ellipse((x, y - s, x + s, y), fill=fill)
    d.polygon([(x - s, y - s / 2), (x + s, y - s / 2), (x, y + s)], fill=fill)


def picker_grid(d, box, rows, draw_cell, row_h):
    """The picker's cells, three to a row; `draw_cell(d, cell_box, r, c)` fills one."""
    x0, y0, x1, y1 = box
    w = (x1 - x0 - 2 * 8) / 3
    for r in range(rows):
        y = y0 + r * (row_h + 6)
        if y + row_h > y1:
            break
        for c in range(3):
            cb = (round(x0 + c * (w + 8)), y, round(x0 + c * (w + 8) + w), y + row_h)
            d.rectangle(cb, fill=(28, 22, 18), outline=(44, 36, 28), width=2)
            draw_cell(d, cb, r, c)
    return y0 + rows * (row_h + 6)


def harem_cell(chosen):
    def draw(d, cb, r, c):
        name = HAREM_CELLS.get((r, c))
        if not name:
            return
        on = name == chosen
        d.rectangle(cb, fill=(70, 54, 30) if on else ROW, outline=GOLD if on else DIM_GOLD, width=3 if on else 2)
        narrow = cb[2] - cb[0] < 300                # the folded grid's cells (LH2): the heart and the name closer
        hx, nx = (24, 44) if narrow else (34, 62)
        heart(d, cb[0] + hx, (cb[1] + cb[3]) / 2 - 2, 12)
        d.text((cb[0] + nx, (cb[1] + cb[3]) / 2), name,
               font=fit(d, name, lambda s: CINZEL(s, 700), 36, cb[2] - cb[0] - nx - 12), fill=NAME if on else TEXT,
               anchor='lm')
    return draw


def plain_title(d, title, y=TOP + 4):
    d.text((30, y), title, font=BRUSH(62), fill=NAME)
    d.text((PW - 30, y + 22), DATE, font=CINZEL(32, 700), fill=NAME, anchor='ra')
    divider(d, y + 100)
    return y + 112


def harem_picture(width):
    pic = dump(TOUCH, 'harem.png').crop((19, 90, 682, 502))
    return pic.resize((width, round(width * pic.height / pic.width)), Image.LANCZOS)


def end_and(d, label, right_w=760):
    y0, y1 = BAR
    panel(d, (16, y0, PW - 32 - right_w, y1))
    d.text(((16 + PW - 32 - right_w) / 2, (y0 + y1) / 2), 'End', font=BRUSH(52), fill=TEXT, anchor='mm')
    red_button(d, (PW - 16 - right_w, y0, PW - 16, y1), label, 54)


def girl_details(d, x, y, x1):
    heart(d, x + 16, y + 34, 16)
    d.text((x + 46, y + 34), 'Anasel', font=BRUSH(58), fill=NAME, anchor='lm')
    d.text((x, y + 92), 'VISITS', font=CINZEL(24, 600), fill=SUBTITLE)
    d.text((x, y + 128), '2', font=GOTHIC(40), fill=TEXT)
    d.text((x + 180, y + 92), 'LAST SUMMONED', font=CINZEL(24, 600), fill=SUBTITLE)
    d.text((x + 180, y + 128), 'LP 3 · Month 3 · Week 3', font=fit(d, 'LP 3 · Month 3 · Week 3', GOTHIC, 38,
                                                                    x1 - x - 190), fill=TEXT)


def ph1():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = plain_title(d, 'Harem')
    row_h = 94
    foot = picker_grid(d, (16, y + 6, PW - 16, 2000), GRID_ROWS, harem_cell('Anasel'), row_h)
    top = foot + 10
    panel(d, (0, top, PW, BAR[0] - 16))
    pic = harem_picture(480)
    img.paste(pic, (24, top + 28))
    d = ImageDraw.Draw(img)
    d.rectangle((24, top + 28, 24 + pic.width, top + 28 + pic.height), outline=GOLD, width=3)
    girl_details(d, 540, top + 40, PW - 30)
    end_and(d, 'Summon Anasel')
    reach(img, label=False, shade=False)
    save(img, 'PH1', 'the grid whole, the girl at the thumb', 'harem', side=[
        'The picker\'s grid whole, three\nto a row as on the PC (the\nowner kept the original\'s\n'
        f'numbered grid): cells {mm(row_h)} high,\nthe names {mm(36)}.',
        'A tap chooses a girl; her\npicture, visits and last summons\nat the thumb (the picture\'s\nwords 1 mm: '
        'a long press\nshows it whole, as PD2d).',
        'End | Summon Anasel in the bar,\nSummon at the right thumb —\nthe choice then the red button\n'
        'is the confirmation.'])


def ph2():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = plain_title(d, 'Harem')
    pic = harem_picture(PW - 32)
    img.paste(pic, (16, y + 6))
    d = ImageDraw.Draw(img)
    d.rectangle((16, y + 6, PW - 16, y + 6 + pic.height), outline=GOLD, width=3)
    y += pic.height + 20
    girl_details(d, 30, y, PW - 30)
    y += 190
    row_h = 128
    picker_grid(d, (16, y, PW - 16, BAR[0] - 20), GRID_ROWS, harem_cell('Anasel'), row_h)
    end_and(d, 'Summon Anasel')
    reach(img, label=False, shade=False)
    save(img, 'PH2', 'the picture on top, the grid scrolled', 'harem', side=[
        'The girl\'s picture at the\nscreen\'s width (2.9 ×, its\nwords 2.4 mm), her visits\nunder it.',
        f'The grid under it in rows of\n{mm(row_h)}: eight rows in view, the\nrest a drag away.',
        'End | Summon as PH1.'])


# --- Treasure ---

def treasure_cell(chosen, items=True):
    def draw(d, cb, r, c):
        if items:
            entry = ITEM_CELLS.get((r, c))
            if not entry:
                return
            name, holder = entry
        else:
            name, holder = CHARACTER_CELLS.get((r, c)), None
            if not name:
                return
        on = name == chosen
        d.rectangle(cb, fill=(70, 54, 30) if on else ROW, outline=GOLD if on else DIM_GOLD, width=3 if on else 2)
        d.text((cb[0] + 18, (cb[1] + cb[3]) / 2), name, font=fit(d, name, GOTHIC, 34, cb[2] - cb[0] - 110),
               fill=TEXT, anchor='lm')
        if holder:
            d.text((cb[2] - 14, (cb[1] + cb[3]) / 2 + 4), holder, font=GOTHIC(24), fill=QUIET, anchor='rm')
        elif name in ('Rance', 'Rick'):              # holds an item
            d.polygon([(cb[2] - 30, (cb[1] + cb[3]) / 2 - 9), (cb[2] - 21, (cb[1] + cb[3]) / 2),
                       (cb[2] - 30, (cb[1] + cb[3]) / 2 + 9), (cb[2] - 39, (cb[1] + cb[3]) / 2)], fill=GOLD)
    return draw


def treasure_picture(width):
    pic = dump(TOUCH, 'treasure-items.png').crop((19, 90, 682, 502))
    return pic.resize((width, round(width * pic.height / pic.width)), Image.LANCZOS)


def lacquer_tabs(d, y, names, active, x0=16, x1=PW - 16, h=110):
    w = (x1 - x0 - (len(names) - 1) * 12) / len(names)
    for i, name in enumerate(names):
        b = (round(x0 + i * (w + 12)), y, round(x0 + i * (w + 12) + w), y + h)
        on = i == active
        d.rectangle(b, fill=(70, 54, 30) if on else ROW, outline=GOLD if on else DIM_GOLD, width=3 if on else 2)
        d.text(((b[0] + b[2]) / 2, (b[1] + b[3]) / 2), name, font=CINZEL(34, 700), fill=NAME if on else QUIET,
               anchor='mm')
    return y + h


def item_details(d, x, y, x1, box_right):
    d.text((x, y + 30), 'L. Holy Sword', font=fit(d, 'L. Holy Sword', BRUSH, 52, x1 - x), fill=NAME, anchor='lm')
    d.text((x, y + 82), 'EQUIPPABLE · HELD BY', font=CINZEL(24, 600), fill=SUBTITLE)
    d.text((x, y + 118), 'Rance', font=GOTHIC(40), fill=TEXT)


def pt1():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = plain_title(d, 'Treasure')
    y = lacquer_tabs(d, y + 6, ['Characters', 'Items'], 1) + 14
    row_h = 84
    foot = picker_grid(d, (16, y, PW - 16, 2000), GRID_ROWS, treasure_cell('L. Holy Sword'), row_h)
    top = foot + 10
    panel(d, (0, top, PW, BAR[0] - 16))
    pic = treasure_picture(480)
    img.paste(pic, (24, top + 28))
    d = ImageDraw.Draw(img)
    d.rectangle((24, top + 28, 24 + pic.width, top + 28 + pic.height), outline=GOLD, width=3)
    item_details(d, 540, top + 40, PW - 30, None)
    end_and(d, 'Take Off')
    reach(img, label=False, shade=False)
    save(img, 'PT1', 'the grid whole, the item at the thumb', 'treasure', side=[
        'Characters | Items as two tabs\nunder the title; the grid whole\n'
        f'(cells {mm(row_h)}, names {mm(34)}), the\nholder\'s name in the cell.',
        'A tap chooses (the touch mode\'s\nround X); the red button in the\nbar acts — Take Off here, Give\n'
        'once a character is chosen —,\ngreyed with the refusal\'s reason.',
        'The item\'s words are in its\npicture: 1 mm here, a long press\nshows it whole.'])


def pt2():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = plain_title(d, 'Treasure')
    pic = treasure_picture(PW - 32)
    img.paste(pic, (16, y + 6))
    d = ImageDraw.Draw(img)
    d.rectangle((16, y + 6, PW - 16, y + 6 + pic.height), outline=GOLD, width=3)
    y += pic.height + 20
    item_details(d, 30, y, PW - 30, None)
    y += 180
    y = lacquer_tabs(d, y, ['Characters', 'Items'], 1) + 14
    row_h = 118
    picker_grid(d, (16, y, PW - 16, BAR[0] - 20), GRID_ROWS, treasure_cell('L. Holy Sword'), row_h)
    end_and(d, 'Take Off')
    reach(img, label=False, shade=False)
    save(img, 'PT2', 'the picture on top, the grid scrolled', 'treasure', side=[
        'The item\'s picture at the\nscreen\'s width: its words\n(what it does) 2.4 mm.',
        f'The tabs and the grid under it,\ncells {mm(row_h)}: seven rows in view,\nthe rest a drag away.',
        'The red button as PT1\'s.'])


# --- Save/Load ---

MANUAL_SAVES = [(4, 'LP 3 · M5 · W2', '20 Oct, 22:30', 'before the attack on Oaks'),
                (3, 'LP 3 · M4 · W4', '18 Oct, 22:30', None), (2, 'LP 3 · M4 · W2', '16 Oct, 22:30', None),
                (1, 'LP 3 · M3 · W4', '14 Oct, 22:30', "Lia's scene next week")]
SAVE_FIGURES = [('STAGE', 'Stage 1 · Leazas civil war'), ('BUDGET', '116,400K GOLD'), ('REALM', '15 provinces'),
                ('UNITS', '9'), ('TROOPS', '6,251'), ('WRITTEN', '20 Oct 2026, 22:30')]


def star(d, cx, cy, r=14, fill=GOLD):
    import math
    pts = [(cx + (r if k % 2 == 0 else r * 0.45) * math.sin(k * math.pi / 5),
            cy - (r if k % 2 == 0 else r * 0.45) * math.cos(k * math.pi / 5)) for k in range(10)]
    d.polygon(pts, fill=fill)


def save_title(d, chosen_tab='Load'):
    y = TOP + 4
    d.text((30, y), chosen_tab, font=BRUSH(62), fill=NAME)
    for i, name in enumerate(('Load', 'Save')):
        b = (PW - 16 - (2 - i) * 210 - (1 - i) * 10, y + 4, PW - 16 - (1 - i) * 220, y + 92)
        on = name == chosen_tab
        d.rectangle(b, fill=(112, 26, 20) if on else ROW, outline=GOLD if on else DIM_GOLD, width=3 if on else 2)
        d.text(((b[0] + b[2]) / 2, (b[1] + b[3]) / 2), name, font=CINZEL(32, 700), fill=NAME if on else QUIET,
               anchor='mm')
    divider(d, y + 112)
    return y + 124


def save_row(d, box, save_, chosen=False, two_lines=True):
    x0, y0, x1, y1 = box
    no, date, written, comment = save_
    d.rectangle(box, fill=(70, 54, 30) if chosen else ROW, outline=GOLD if chosen else DIM_GOLD,
                width=3 if chosen else 2)
    sy = y0 + 22 if two_lines else (y0 + y1) / 2 - 32
    seal(d, x0 + 20, sy, no, 64)
    if comment:
        star(d, x0 + 20, sy, 16)
    d.text((x0 + 108, sy + 32), date, font=GOTHIC(42), fill=TEXT, anchor='lm')
    d.text((x1 - 22, sy + 32), written, font=GOTHIC(32), fill=QUIET, anchor='rm')
    if comment and two_lines:
        d.text((x0 + 108, y1 - 34), f'"{comment}"', font=GOTHIC(32), fill=QUIET, anchor='lm')


def save_list(d, y, rows_h, chosen=None, two_lines=True, foot=None, saves=MANUAL_SAVES):
    """`foot`: the list's room ends there; the rows that do not fit are a drag away (a scroll bar shows it)."""
    y = lacquer_tabs(d, y, [f'Manual saves · {len(saves)}', 'Auto-saves · 20'], 0, h=104) + 18
    d.text((24, y + 30), 'NEWEST FIRST', font=CINZEL(26, 600), fill=SUBTITLE, anchor='lm')
    b = (PW - 16 - 470, y, PW - 16, y + 64)
    d.rectangle(b, fill=ROW, outline=DIM_GOLD, width=2)
    d.text(((b[0] + b[2]) / 2, y + 32), 'Delete all without', font=GOTHIC(28), fill=TEXT, anchor='rm')
    star(d, (b[0] + b[2]) / 2 + 22, y + 32, 12)
    d.text(((b[0] + b[2]) / 2 + 42, y + 32), f'({sum(1 for s in saves if not s[3])})', font=GOTHIC(28), fill=TEXT,
           anchor='lm')
    y += 84
    shown = len(saves) if foot is None else min(len(saves), (foot - y + 10) // (rows_h + 10))
    x1 = PW - 16 if shown == len(saves) else PW - 48
    if shown < len(saves):
        track = (PW - 34, y, PW - 22, y + shown * (rows_h + 10) - 10)
        d.rounded_rectangle(track, radius=6, fill=(40, 32, 24))
        d.rounded_rectangle((track[0], track[1], track[2], track[1] + (track[3] - track[1]) * shown // len(saves)),
                            radius=6, fill=DIM_GOLD)
    for s in saves[:shown]:
        save_row(d, (16, y, x1, y + rows_h), s, chosen=s[0] == chosen, two_lines=two_lines)
        y += rows_h + 10
    return y


def save_page(img, y, compact=False, number=4):
    d = ImageDraw.Draw(img)
    d.text((24, y + 40), f'No. {number} · LP 3 · Month 5 · Week 2', font=CINZEL(34, 700), fill=NAME, anchor='lm')
    b = (PW - 16 - 230, y, PW - 16, y + 84)
    d.rectangle(b, fill=(54, 18, 14), outline=(196, 70, 50), width=3)
    d.rectangle((b[0] + 30, y + 30, b[0] + 52, y + 60), outline=(236, 150, 110), width=3)
    d.line([(b[0] + 24, y + 28), (b[0] + 58, y + 28)], fill=(236, 150, 110), width=3)
    d.text((b[0] + 140, y + 42), 'Delete', font=CINZEL(30, 700), fill=(236, 150, 110), anchor='mm')
    y += 100
    pic = dump(TOUCH, 'save-load.png').crop((424, 117, 1227, 450))
    pic = pic.resize((PW - 32, round((PW - 32) * pic.height / pic.width)), Image.LANCZOS)
    if compact:
        pic = pic.crop((0, 0, pic.width, round(pic.height * 0.8)))
    img.paste(pic, (16, y))
    d = ImageDraw.Draw(img)
    d.rectangle((16, y, PW - 16, y + pic.height), outline=GOLD, width=3)
    y += pic.height + 20
    for i, (label, value) in enumerate(SAVE_FIGURES):
        x = 24 + (i % 2) * 600
        ry = y + (i // 2) * 96
        d.text((x, ry), label, font=CINZEL(24, 600), fill=SUBTITLE)
        d.text((x, ry + 34), value, font=fit(d, value, GOTHIC, 38, 570), fill=TEXT)
    y += 3 * 96 + 10
    d.text((24, y), 'COMMENT', font=CINZEL(24, 600), fill=SUBTITLE)
    b = (16, y + 36, PW - 16 - 220, y + 140)
    d.rectangle(b, fill=(30, 22, 16), outline=DIM_GOLD, width=2)
    d.text((40, y + 88), 'before the attack on Oaks', font=GOTHIC(38), fill=TEXT, anchor='lm')
    row_box(d, (PW - 16 - 204, y + 36, PW - 16, y + 140), 'Edit', size=40, face=BRUSH, centred=True)
    return y + 140


def back_and(d, label):
    y0, y1 = BAR
    panel(d, (16, y0, 400, y1))
    d.polygon([(110, (y0 + y1) / 2 - 18), (78, (y0 + y1) / 2), (110, (y0 + y1) / 2 + 18)], fill=GOLD)
    d.text((240, (y0 + y1) / 2), 'Back', font=BRUSH(50), fill=TEXT, anchor='mm')
    red_button(d, (416, y0, PW - 16, y1), label, 58)


def pl1():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = save_title(d)
    rows_h = 172
    save_list(d, y + 6, rows_h)
    y0, y1 = BAR
    panel(d, (16, y0, 400, y1))
    d.polygon([(110, (y0 + y1) / 2 - 18), (78, (y0 + y1) / 2), (110, (y0 + y1) / 2 + 18)], fill=GOLD)
    d.text((240, (y0 + y1) / 2), 'Back', font=BRUSH(50), fill=TEXT, anchor='mm')
    reach(img, label=False, shade=False)
    save(img, 'PL1', 'the list, then the save\'s page — the list', 'saves', side=[
        'Two pages. The list: Load | Save\nat the title (from COMMAND),\nthe manual saves and the auto-\n'
        'saves as two tabs, newest first.',
        f'Rows {mm(rows_h)}: the number on its seal\n(a gold star with a comment), the\ndate, the time written, the\n'
        'comment as the second line.',
        'A tap on a row opens its page\n(PL1p); in Save, "+ New save"\nheads the list. The sweep\n'
        '"Delete all without ★" asks in\na callout beside it.',
        'Back at the left of the bar.'])


def pl1p():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = save_title(d)
    save_page(img, y + 10)
    d = ImageDraw.Draw(img)
    back_and(d, 'Load')
    reach(img, label=False, shade=False)
    save(img, 'PL1p', 'the list, then the save\'s page — a save', 'saves', side=[
        'The save\'s page: its title and\nDelete (the danger button, its\ncallout under it) on top; its map\n'
        'as All Data draws it, at the\nscreen\'s width; its figures in\ntwo columns; the comment\nand Edit.',
        'Back to the list at the left,\nLoad (red) at the right thumb —\nthe choice then the red button\n'
        'is the confirmation.'])


# PL2's longer list (owner, 2026-10-27: the room under the page goes to the list): the gallery's four saves renumbered
# 12-9, the eight older ones made up.
PL2_SAVES = [(12 - i,) + s[1:] for i, s in enumerate(MANUAL_SAVES)] + [
    (8, 'LP 3 · M3 · W2', '12 Oct, 21:10', None), (7, 'LP 3 · M3 · W1', '11 Oct, 23:05', None),
    (6, 'LP 3 · M2 · W4', '10 Oct, 20:40', None), (5, 'LP 3 · M2 · W2', '9 Oct, 22:15', None),
    (4, 'LP 3 · M2 · W1', '9 Oct, 21:50', None), (3, 'LP 3 · M1 · W4', '8 Oct, 22:30', None),
    (2, 'LP 3 · M1 · W2', '8 Oct, 21:00', None), (1, 'LP 3 · M1 · W1', '7 Oct, 20:30', 'a new game')]


def pl2():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    y = save_title(d)
    page_h = save_page(Image.new('RGB', (PW, PH)), 0, compact=True)
    page = BAR[0] - 24 - page_h                 # the page stands on the bar, the list takes the room above it
    rows_top = y + 6 + 104 + 18 + 84            # under the tabs and the list's heading (save_list)
    y = save_list(d, y + 6, 104, chosen=12, two_lines=False, foot=page - 36, saves=PL2_SAVES)
    divider(d, page - 18)
    save_page(img, page, compact=True, number=12)
    d = ImageDraw.Draw(img)
    back_and(d, 'Load')
    reach(img, label=False, shade=False)
    rows = (y - rows_top) // 114
    save(img, 'PL2', 'one page: the list over the save', 'saves', side=[
        'One page as the PC\'s: the list\nin one-line rows (5.9 mm,\nthe comment only on the save\'s\n'
        'page), scrolling in its room;\nthe chosen save\'s page under\nit, its map cut lower.',
        f'The page stands on the bar, the\nlist takes the room above it\n(the owner\'s change): {rows} rows\n'
        'in view, the rest a drag away.',
        'Twelve manual saves: the\ngallery\'s four (here 12-9) and\neight older ones made up.'])


def p5():
    ph1(); ph2()
    pt1(); pt2()
    pl1(); pl1p(); pl2()


# --- round P6: the options window and the dungeon's menus upright (2026-10-27) ---------------------------------------

OPTION_TABS = ['Messages', 'Display', 'Battle', 'Game', 'Tweaks']
SWATCHES = [(236, 132, 118), (236, 120, 170), (226, 110, 226), (176, 120, 236), (110, 120, 236), (110, 170, 236),
            (90, 220, 236), (120, 236, 170), (110, 220, 110)]
PREVIEW = ['I believe that we should first', 'prepare our cities for their', 'attacks, then defend them with',
           'urban warfare.']


def checkbox(d, x, y, on=True, s=46):
    d.rectangle((x, y, x + s, y + s), fill=(246, 238, 220), outline=INK_QUIET, width=3)
    if on:
        d.line([(x + 9, y + s / 2), (x + s / 2 - 3, y + s - 10), (x + s - 7, y + 8)], fill=INK_HEAD, width=6)


def radio(d, x, y, on, s=44):
    d.ellipse((x, y, x + s, y + s), fill=(246, 238, 220), outline=INK_QUIET, width=3)
    if on:
        d.ellipse((x + 11, y + 11, x + s - 11, y + s - 11), fill=INK_HEAD)


def option_row(d, x, y, x1, title, lines, kind='check', on=True):
    """A setting as a finger row: its control, its name, its explanation under it."""
    if kind == 'check':
        checkbox(d, x, y + 4, on)
    d.text((x + 70 if kind == 'check' else x, y), title, font=GOTHIC(40), fill=INK)
    y += 58
    for line in lines:
        d.text((x + 70 if kind == 'check' else x, y), line, font=GOTHIC(30), fill=INK_QUIET)
        y += 40
    return y + 30


def section(d, x, y, x1, name):
    d.text((x, y), name, font=CINZEL(28, 700), fill=INK_HEAD)
    d.line([(x, y + 42), (x1, y + 42)], fill=PAPER_LINE, width=2)
    return y + 64


def messages_tab(d, x, y, x1):
    y = text_speed(d, x, y, x1)
    y = message_skip(d, x, y, x1)
    return read_text(d, x, y, x1)


def text_speed(d, x, y, x1):
    y = section(d, x, y, x1, 'TEXT SPEED')
    d.text((x, y + 30), 'Slow', font=GOTHIC(30), fill=INK_QUIET, anchor='lm')
    d.text((x1, y + 30), 'Instant', font=GOTHIC(30), fill=INK_QUIET, anchor='rm')
    sx0, sx1 = x + 100, x1 - 130
    d.rectangle((sx0, y + 26, sx1, y + 34), fill=(190, 172, 138))
    d.rectangle((sx0, y + 26, sx1 - 40, y + 34), fill=INK_HEAD)
    d.ellipse((sx1 - 66, y + 4, sx1 - 14, y + 56), fill=INK_HEAD, outline=(246, 238, 220), width=4)
    d.text((sx1 - 40, y - 18), '10', font=GOTHIC(26), fill=INK, anchor='mm')
    y += 84
    box = (x, y, x1, y + 4 * 58 + 50)
    d.rectangle(box, fill=(22, 16, 12), outline=INK_QUIET, width=3)
    for i, line in enumerate(PREVIEW):
        d.text((x + 24, y + 24 + i * 58), line, font=GOTHIC(48), fill=(236, 132, 118))
    triangle(d, x1 - 30, box[3] - 24, 12)
    y = box[3] + 14
    d.text((x, y), 'The preview counts as read.', font=GOTHIC(28), fill=INK_QUIET)
    return y + 70


def message_skip(d, x, y, x1):
    y = section(d, x, y, x1, 'MESSAGE SKIP')
    y = option_row(d, x, y, x1, 'Skip unseen text', ['MsgSkip and Ctrl skip unread ones too.'])
    y = option_row(d, x, y, x1, 'MsgSkip speeds up fades', ['Fades and waits run fast.'])
    d.text((x, y), '0 messages seen.', font=GOTHIC(30), fill=INK_QUIET)
    d.text((x1, y), 'Forget them…', font=GOTHIC(30), fill=INK_QUIET, anchor='ra')
    return y + 80


def read_text(d, x, y, x1):
    y = section(d, x, y, x1, 'ALREADY-READ TEXT')
    b = (x, y, x + 300, y + 100)
    d.rectangle(b, fill=(22, 16, 12))
    d.text(((b[0] + b[2]) / 2, y + 50), "Don't change", font=GOTHIC(32), fill=TEXT, anchor='mm')
    sw = (x1 - x - 320 - 4 * 14) // 5
    for i, c in enumerate(SWATCHES):
        sx = x + 320 + (i % 5) * (sw + 14) if i < 4 else x + (i - 4) * (sw + 14)
        sy = y if i < 4 else y + 116
        d.rectangle((sx, sy, sx + sw, sy + 100), fill=(22, 16, 12), outline=INK_HEAD if i == 0 else INK_QUIET,
                    width=6 if i == 0 else 2)
        d.rectangle((sx + 18, sy + 18, sx + sw - 18, sy + 82), fill=c)
    y += 240
    d.text((x, y), 'Salmon', font=GOTHIC(38), fill=INK)
    for i, line in enumerate(['A message window you have read before, in any', 'game, shows its text in this colour.']):
        d.text((x, y + 54 + i * 40), line, font=GOTHIC(30), fill=INK_QUIET)
    return y + 140


def tweaks_tab(d, x, y, x1):
    for line in ['Each setting here changes how the original game', 'plays. Everything else keeps the original\'s rules.']:
        d.text((x, y), line, font=GOTHIC(30), fill=INK_QUIET)
        y += 42
    y += 30
    y = section(d, x, y, x1, 'BATTLE')
    y = option_row(d, x, y, x1, 'Damage forecast', ['Each attack banner shows the damage it is', 'likely to deal: '
                   'the target\'s troops and its', 'commander\'s HP, 9 times in 10.'])
    d.text((x, y), 'Chaos and Nikkou reach demons', font=GOTHIC(40), fill=INK)
    y += 58
    for line in ['Against a demon commander, Chaos\'s or Nikkou\'s', 'holder fights in person even when his troops',
                 'exceed the battle\'s limit.']:
        d.text((x, y), line, font=GOTHIC(30), fill=INK_QUIET)
        y += 40
    y += 16
    for i, label in enumerate(['Off — as the original', 'On the field', 'Out of reach']):
        r = (x, y, x1, y + 124)
        d.rectangle(r, fill=(232, 220, 190) if i == 0 else PAPER, outline=PAPER_LINE, width=2)
        radio(d, x + 24, y + 40, i == 0)
        d.text((x + 96, y + 62), label, font=GOTHIC(38), fill=INK, anchor='lm')
        y += 134
    for line in ['On the field: the demon\'s troops may shoot at', 'him, as at any commander in the fight.',
                 'Out of reach: they never do.']:
        d.text((x, y), line, font=GOTHIC(30), fill=INK_QUIET)
        y += 40
    y += 40
    y = section(d, x, y, x1, 'SCENES')
    return option_row(d, x, y, x1, 'New scenes first', ['Talks and harem visits play unread scenes', 'first, '
                      'with a few hints and a choice of ours.'])


def options_sheet(d, top, foot, active):
    panel(d, (0, top, PW, foot))
    d.rectangle((24, top + 24, PW - 24, foot - 24), fill=PAPER)
    return 24, top + 24, PW - 24, foot - 24


def defaults_close(d):
    y0, y1 = BAR
    panel(d, (16, y0, 420, y1))
    d.text((218, (y0 + y1) / 2), 'Defaults', font=CINZEL(36, 700), fill=QUIET, anchor='mm')
    red_button(d, (436, y0, PW - 16, y1), 'Close', 56)


def po1():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    d.text((30, TOP + 4), 'Options', font=BRUSH(62), fill=NAME)
    y = TOP + 112
    w, gap = (PW - 48 - 4 * 8) / 5, 8
    for i, name in enumerate(OPTION_TABS):
        x = 24 + i * (w + gap)
        on = i == 0
        d.rectangle((x, y + (0 if on else 12), x + w, y + 110), fill=PAPER if on else PAPER_DARK)
        if on:
            d.rectangle((x, y, x + w, y + 9), fill=VERMILION)
        d.text((x + w / 2, y + 60), name, font=fit(d, name.upper(), lambda s: CINZEL(s, 700), 30, w - 16),
               fill=INK_HEAD if on else INK_QUIET, anchor='mm')
    sx0, sy0, sx1, sy1 = 24, y + 110, PW - 24, BAR[0] - 20
    d.rectangle((sx0, sy0, sx1, sy1), fill=PAPER)
    messages_tab(d, sx0 + 30, sy0 + 40, sx1 - 30)
    defaults_close(d)
    reach(img, label=False, shade=False)
    save(img, 'PO1', 'the paper sheet, its tabs on top', 'options', side=[
        'The options window as a full\npaper sheet; its five tabs on\ntop as on the PC (6.3 mm).',
        'One column: each setting a row\nwith its control at the left\n(the boxes 2.6 mm, the row\'s whole '
        'height\ntaps), its name 2.3 mm, its\nexplanation 1.7 mm under it.',
        'The text speed\'s preview the\ngame\'s window at 2.7 mm; the\nread-text colours in two rows.',
        'Defaults | Close in the bar;\nsettings apply at once.'])


def po2():
    img, d = phone()
    backdrop(img, (0, 0, PW, PH))
    d.text((30, TOP + 4), 'Options', font=BRUSH(62), fill=NAME)
    d.text((PW - 30, TOP + 26), 'TWEAKS', font=CINZEL(34, 700), fill=NAME, anchor='ra')
    sx0, sy0, sx1, sy1 = 24, TOP + 112, PW - 24, BAR[0] - 160
    d.rectangle((sx0, sy0, sx1, sy1), fill=PAPER)
    tweaks_tab(d, sx0 + 30, sy0 + 40, sx1 - 30)
    ty = BAR[0] - 150
    w = (PW - 32 - 4 * 8) / 5
    for i, name in enumerate(OPTION_TABS):
        x = 16 + i * (w + 8)
        on = i == 4
        d.rectangle((x, ty, x + w, ty + 130), fill=(70, 54, 30) if on else ROW, outline=GOLD if on else DIM_GOLD,
                    width=3 if on else 2)
        d.text((x + w / 2, ty + 65), name, font=fit(d, name.upper(), lambda s: CINZEL(s, 700), 28, w - 16),
               fill=NAME if on else QUIET, anchor='mm')
    defaults_close(d)
    reach(img, label=False, shade=False)
    save(img, 'PO2', 'the tabs at the thumb (Tweaks)', 'options', side=[
        'The tabs as a bar at the thumb\n(7.4 mm), over Defaults | Close;\nthe chosen tab\'s name at the\n'
        'title\'s right.',
        'Shown: Tweaks — its radio\nchoices as finger rows (7 mm),\nthe chosen one lit.',
        'The phone\'s usual tab bar; the\nPC\'s paper tabs left for a bar\nof lacquer cells.'])


# --- the dungeon's floor ladder and entry menu ---

FLOOR_LINES = ["Monsters' Cave", '  7F', 'Rance Unit', '  (1000)']
ENTRY_LINES = ["[Monsters' Cave]", '', '(Unsearched)']


def dungeon_picture(width):
    pic = dump(TOUCH, 'floor-ladder-middle.png').crop((364, 204, 916, 532))
    return pic.resize((width, round(width * pic.height / pic.width)), Image.LANCZOS)


def info_window(d, box, lines, size=60):
    panel(d, box, fill=(44, 34, 22))
    for i, line in enumerate(lines):
        d.text((box[0] + 34, box[1] + 30 + i * size * 1.2), line, font=GOTHIC(size), fill=(226, 204, 150))


def ladder_rung(d, box, word, sub, way, off=False):
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=ROW, outline=OFF_TAG if off else DIM_GOLD, width=3)
    cy = (y0 + y1) / 2
    if way:
        arrow_mark(d, x0 + 50, cy, way, 20, OFF_TEXT if off else GOLD)
    d.text((x0 + 96, cy - 20), word, font=fit(d, word, GOTHIC, 46, x1 - x0 - 110), fill=OFF_TEXT if off else TEXT,
           anchor='lm')
    d.text((x0 + 98, cy + 30), sub, font=GOTHIC(28), fill=OFF_TEXT if off else QUIET, anchor='lm')


def floor_rail(d, x0, x1, foot, entry=False):
    """The touch mode's rail: Retreat (or Return) on top, Descend (or Start Search) in the corner."""
    h, gap = 150, 14
    if entry:
        ladder_rung(d, (x0, foot - h, x1, foot), 'Start Search', 'a unit goes down', 'down')
        y = foot - 2 * h - gap
        row_box(d, (x0, y, x1, y + h), 'Return', size=46, centred=True)
        return y
    ladder_rung(d, (x0, foot - h, x1, foot), 'Descend', 'deeper · to 8F', 'down')
    y = foot - h - gap - 110
    d.text(((x0 + x1) / 2, y + 26), 'FLOOR', font=CINZEL(24, 600), fill=SUBTITLE, anchor='mm')
    d.text(((x0 + x1) / 2, y + 74), '7F', font=BRUSH(56), fill=NAME, anchor='mm')
    y -= h + gap
    ladder_rung(d, (x0, y, x1, y + h), 'Ascend', 'back · to 6F', 'up')
    y -= h + gap
    red_button(d, (x0, y, x1, y + h), 'Retreat', 52)
    return y


def pf_frame(img, foot):
    """The map wait's map under the dungeon's picture (as on the PC), the picture's foot at `foot`."""
    draw_map(img, (0, 0, PW, BAR[0] - 12), 1290, 0, (BAR[0] - 12) / 800, 36, dim=120)
    d = ImageDraw.Draw(img)
    d.rectangle((0, BAR[0] - 12, PW, PH), fill=BACKDROP)
    pic = dungeon_picture(PW - 32)
    top = foot - pic.height
    img.paste(pic, (16, top))
    d = ImageDraw.Draw(img)
    d.rectangle((16, top, PW - 16, foot), outline=GOLD, width=3)
    return d, foot


def pf1():
    img, d = phone()
    d, pic_foot = pf_frame(img, BAR[1] - 640)
    rail_x0 = PW - 16 - 440
    top = floor_rail(d, rail_x0, PW - 16, BAR[1])
    info_window(d, (16, top, rail_x0 - 16, BAR[1]), FLOOR_LINES, 58)
    reach(img, label=False, shade=False)
    save(img, 'PF1', 'the ladder as the tablet\'s rail', 'dungeon', side=[
        'The floor\'s picture at the\nscreen\'s width (2.1 ×); the\nscript\'s window under it at its\n'
        'own breaks (3.3 mm).',
        'The ladder as the tablet\'s rail\n(touch.md\'s B) at the right\nthumb: Retreat on top, Ascend,\n'
        'the floor, Descend in the\ncorner; rungs 8.5 mm. A way\nnot offered keeps its room,\nhidden.'])


def pf1e():
    img, d = phone()
    d, pic_foot = pf_frame(img, BAR[1] - 360)
    rail_x0 = PW - 16 - 440
    top = floor_rail(d, rail_x0, PW - 16, BAR[1], entry=True)
    info_window(d, (16, top, rail_x0 - 16, BAR[1]), ENTRY_LINES, 52)
    reach(img, label=False, shade=False)
    save(img, 'PF1e', 'the search\'s menu on the rail', 'dungeon', side=[
        'Before a search: Return on top,\nStart Search in Descend\'s\ncorner (touch.md\'s §6); with\n'
        'four items (Merim, God\'s Door)\nthe rail grows upwards.'])


def pf2():
    img, d = phone()
    d, pic_foot = pf_frame(img, BAR[1] - 770)
    info_window(d, (16, pic_foot + 30, PW - 16, pic_foot + 30 + 340), FLOOR_LINES, 62)
    y0 = BAR[1] - 170
    w = (PW - 32 - 2 * 12) / 3
    red_button(d, (16, y0 - 180, PW - 16, y0 - 30), 'Retreat', 52)
    ladder_rung(d, (16, y0, 16 + w, BAR[1]), 'Ascend', 'back · to 6F', 'up')
    d.rectangle((16 + w + 12, y0, 16 + 2 * w + 12, BAR[1]), fill=PANEL, outline=DIM_GOLD, width=2)
    d.text((16 + 1.5 * w + 12, y0 + 46), 'FLOOR', font=CINZEL(24, 600), fill=SUBTITLE, anchor='mm')
    d.text((16 + 1.5 * w + 12, y0 + 112), '7F', font=BRUSH(64), fill=NAME, anchor='mm')
    ladder_rung(d, (16 + 2 * w + 24, y0, PW - 16, BAR[1]), 'Descend', 'deeper · to 8F', 'down')
    reach(img, label=False, shade=False)
    save(img, 'PF2', 'the ladder laid across the foot', 'dungeon', side=[
        'The script\'s window under the\npicture at the screen\'s width\n(3.5 mm); the ladder laid\n'
        'across the foot: Ascend, the\nfloor, Descend; Retreat over\nthem the screen\'s width.',
        'Wider rungs, but Ascend at the\nleft thumb\'s side.'])


def p6():
    po1(); po2()
    pf1(); pf1e(); pf2()


# --- round P7: the battle's other states on PB3 (2026-10-27) ---------------------------------------------------------

def veil(img, box, alpha=0.6):
    part = img.crop(box)
    img.paste(Image.blend(part, Image.new('RGB', part.size), alpha), box[:2])


def balloon(d, box, lines, size=50, tail=None, triangle_on=True):
    """Our speech balloon: white, dark ink; `tail` the x its tail points up from."""
    x0, y0, x1, y1 = box
    d.rounded_rectangle(box, radius=34, fill=(246, 244, 238), outline=(150, 140, 120), width=3)
    if tail:
        d.polygon([(tail - 26, y0 + 2), (tail + 26, y0 + 2), (tail - 6, y0 - 40)], fill=(246, 244, 238))
    for i, line in enumerate(lines):
        d.text((x0 + 40, y0 + 30 + i * size * 1.25), line, font=GOTHIC(size), fill=(30, 26, 22))
    if triangle_on:
        triangle(d, x1 - 46, y1 - 34, 16, fill=(196, 160, 86))


def board(img, scene='battle-turn.png'):
    top = pb3_board(img, scene, UNITS)
    battle_deck(ImageDraw.Draw(img), top, empty=True)
    return top


def pb4o1():
    img, d = phone()
    top = board(img, 'opening.png')
    d = ImageDraw.Draw(img)
    scene_h = round(410 * PW / 1024)
    d.rectangle((0, TOP, PW, TOP + scene_h), fill=(0, 0, 0))
    img.paste(at_width(dump(TOUCH, 'opening.png').crop((0, 0, 1280, 410))), (0, TOP + (scene_h - 391) // 2))
    reach(img, label=False, shade=False)
    save(img, 'PB4o1', 'the opening at the scene\'s place', 'battle-states', side=[
        'The opening as on the PC, at\nthe scene\'s place (0.95 ×): the\narmies\' names 3.2 mm, but the\n'
        'plate\'s place and limit 0.9 mm.',
        'The board under it as PB3\'s,\nthe deck empty; a tap goes on.'])


def pb4o2():
    img, d = phone()
    top = board(img, 'opening.png')
    d = ImageDraw.Draw(img)
    mid = (TOP + top) // 2
    for y in range(TOP, top):                          # ours blue above, the enemy's red below, as the board
        t = (y - TOP) / (top - TOP)
        c = (round(40 + 110 * t), round(60 - 30 * t), round(140 - 100 * t)) if t < 0.5 else \
            (round(150 + 20 * (t - .5)), round(40 - 10 * (t - .5)), round(40 - 10 * (t - .5)))
        d.line([(0, y), (PW, y)], fill=c)
    d.text((PW / 2, TOP + 300), 'Rance Army', font=BRUSH(120), fill=(246, 238, 220), anchor='mm',
           stroke_width=6, stroke_fill=(20, 20, 40))
    plate = (110, mid - 190, PW - 110, mid + 10)
    panel(d, plate)
    d.text((PW / 2, plate[1] + 56), 'Oaks — Offensive Battle', font=CINZEL(46, 700), fill=TEXT, anchor='mm')
    d.text((PW / 2, plate[1] + 118), 'City · Battle limit: up to 232', font=CINZEL(36, 700), fill=TEXT, anchor='mm')
    d.text((PW / 2, plate[1] + 166), '4 turns', font=CINZEL(30, 600), fill=QUIET, anchor='mm')
    d.text((PW / 2, mid + 130), 'VS', font=BRUSH(130), fill=GOLD, anchor='mm', stroke_width=5,
           stroke_fill=(40, 26, 10))
    d.text((PW / 2, top - 520), 'Leazas', font=BRUSH(120), fill=(246, 238, 220), anchor='mm', stroke_width=6,
           stroke_fill=(40, 10, 10))
    d.text((PW / 2, top - 380), 'Rebel Army', font=BRUSH(120), fill=(246, 238, 220), anchor='mm', stroke_width=6,
           stroke_fill=(40, 10, 10))
    d.text((PW / 2, top - 230), 'Walls +8%', font=BRUSH(60), fill=(240, 200, 110), anchor='mm', stroke_width=4,
           stroke_fill=(40, 10, 10))
    reach(img, label=False, shade=False)
    save(img, 'PB4o2', 'the opening turned upright, over the board', 'battle-states', side=[
        'The opening laid over the scene\nand the board as PB3 stands:\nours blue on top, the enemy red\n'
        'at the foot, the plate between\n(the place 2.6 mm, the limit\n2 mm), VS at the gauge\'s place.',
        'The cards fly in under it as\nthe opening fades (as on the\nPC); a tap goes on.'])


def pb4s():
    img, d = phone()
    d.rectangle((0, 0, PW, PH), fill=(8, 8, 10))
    sr = dump(TOUCH, 'scout-report.png')
    kanami = sr.crop((40, 40, 340, 440)).resize((330, 440), Image.LANCZOS)
    framed(img, kanami, (24, TOP + 20))
    d = ImageDraw.Draw(img)
    balloon(d, (380, TOP + 30, PW - 16, TOP + 330), ['The Approaching Enemy', '[ Leazas Rebel Army ]',
                                                     'Commanders 4   Total troops 1,556'], 40, triangle_on=False)
    y = TOP + 500
    cw, ch = (PW - 32 - 14) // 2, 250
    for label, places in (('FRONT ROW', (1, 2)), ('BACK ROW', (3, 4))):
        d.text((PW / 2, y + 18), label, font=CINZEL(26, 700), fill=SUBTITLE, anchor='mm')
        y += 40
        for x, place in zip((16, 16 + cw + 14), places):
            attacker_card(img, (x, y, x + cw, y + ch), place)
        d = ImageDraw.Draw(img)
        y += ch + 14
    rance = sr.crop((40, 574, 194, 774)).resize((231, 300), Image.LANCZOS)
    ry = BAR[1] - 300
    framed(img, rance, (24, ry))
    d = ImageDraw.Draw(img)
    balloon(d, (290, ry + 40, PW - 16, ry + 200), ['Mhm... I see.'], 52)
    reach(img, label=False, shade=False)
    save(img, 'PB4s', 'the ninja\'s scout report', 'battle-states', side=[
        'The scout report before the\nfield as a screen of its own:\nKanami and her report on top,\n'
        'the rebels\' cards as the enemy\nattack\'s (PA1), front row first;\nRance\'s answer at the foot.',
        'The balloons\' text 2.3-3 mm;\none tap goes on (the report\'s\nwindows collapsed into one, as\non the PC).'])


def pb4c():
    img, d = phone()
    d.rectangle((0, 0, PW, PH), fill=(8, 8, 10))
    st = dump(TOUCH, 'strategy.png')
    d.text((PW / 2, 760), "ALLY ARMY'S STRATEGY WAS SUCCESSFUL.", font=CINZEL(32, 700), fill=(220, 190, 110),
           anchor='mm')
    d.text((PW / 2, 870), 'Success', font=BRUSH(120), fill=(246, 238, 220), anchor='mm')
    band = (0, 1000, PW, 1520)
    d.polygon([(0, band[1] + 60), (PW, band[1]), (PW, band[3] - 60), (0, band[3])], fill=(40, 70, 140))
    barres = st.crop((70, 200, 370, 596)).resize((372, 491), Image.LANCZOS)
    framed(img, barres, (40, band[1] + 14))
    d = ImageDraw.Draw(img)
    d.text((440 + (PW - 440) / 2, (band[1] + band[3]) / 2), 'Strategy +10%', font=BRUSH(72), fill=(240, 200, 110),
           anchor='mm', stroke_width=4, stroke_fill=(20, 20, 40))
    balloon(d, (16, 1600, PW - 16, 1890), ['It was a very basic strategy, but', 'it worked. Always know the basics.',
                                          'Now, begin!'], 46, tail=220)
    reach(img, label=False, shade=False)
    save(img, 'PB4c', 'the strategy\'s cut-in', 'battle-states', side=[
        'The cut-in stacked: the result\nand its word on top, the\nstrategist on the band with\n'
        'what it does (4 mm), his line\nunder it at 2.6 mm; one tap.',
        'Barres (unit 6), his line the\ngallery\'s; the enemy\'s cut-in\nthe same in red.'])


def pb4r():
    img, d = phone()
    top = board(img)
    veil(img, (0, TOP, PW, top), 0.55)
    d = ImageDraw.Draw(img)
    plate = (24, 1000, PW - 24, 1720)
    panel(d, plate)
    d.text((PW / 2, plate[1] + 90), 'Night Falls', font=BRUSH(90), fill=NAME, anchor='mm')
    d.text((PW / 2, plate[1] + 180), 'THE BATTLE DRAGGED ON AND IT BECAME NIGHT.', font=CINZEL(30, 600),
           fill=SUBTITLE, anchor='mm')
    divider(d, plate[1] + 236, plate[0] + 40, plate[2] - 40)
    for cx, army, value in ((PW / 4 + 10, 'LEAZAS', '557'), (3 * PW / 4 - 10, 'LEAZAS REB', '1,229')):
        d.text((cx, plate[1] + 300), army, font=CINZEL(36, 600), fill=QUIET, anchor='mm')
        d.text((cx, plate[1] + 400), value, font=BRUSH(110), fill=(222, 110, 90), anchor='mm')
        d.text((cx, plate[1] + 490), 'DAMAGES', font=CINZEL(28, 600), fill=SUBTITLE, anchor='mm')
    d.line([(PW / 2, plate[1] + 270), (PW / 2, plate[1] + 520)], fill=DIM_GOLD, width=2)
    d.text((PW / 2, plate[1] + 610), '2 EXP OBTAINED', font=CINZEL(44, 700), fill=TEXT, anchor='mm')
    triangle(d, plate[2] - 60, plate[3] - 50, 22)
    reach(img, label=False, shade=False)
    save(img, 'PB4r', 'the results plate', 'battle-states', side=[
        'The results plate over the\ndimmed board, at the screen\'s\nwidth: the title 5 mm, the\n'
        'damages 6 mm; a tap goes on.',
        'A level-up\'s page after it on\nthe same plate; a fall\'s box\nand our panel the same way.'])


def pb4b():
    img, d = phone()
    top = pb3_board(img, 'battle-speech.png', UNITS, acting='Rance')
    battle_deck(ImageDraw.Draw(img), top, empty=True)
    d = ImageDraw.Draw(img)
    img.paste(at_width(dump(TOUCH, 'battle-speech.png').crop((0, 0, 1280, 410))), (0, TOP))
    d = ImageDraw.Draw(img)
    scene_foot = TOP + round(410 * PW / 1024)
    d.rectangle((0, TOP + 391, PW, scene_foot), fill=(8, 8, 10))
    b = (300, TOP + 250, PW - 16, TOP + 440)
    balloon(d, b, ["Let's go!"], 62)
    reach(img, label=False, shade=False)
    save(img, 'PB4b', 'a fighter\'s line', 'battle-states', side=[
        'The speaker\'s portrait at the\nscene\'s left as on the PC; our\nballoon over the scene\'s foot,\n'
        'larger than the PC\'s (3.5 mm),\nrewrapped as ever.',
        'A defence\'s ribbon and caption\nthe same way: the caption\nover the scene\'s foot at\n2.4 mm.'])


def p7():
    pb4o1(); pb4o2(); pb4s(); pb4c(); pb4r(); pb4b()


# --- round P8: the landscape phone — the frame, the map, the battle (2026-10-27) -------------------------------------

LW, LH = 2712, 1220                     # the Poco X6 Pro held landscape: 154.5 × 69.5 mm
LK = LH / 800                           # the 1280×800 layout fitted to the height: 1.525 phone px a layout pixel
FX0, FX1 = (LW - 1952) // 2, (LW + 1952) // 2           # the layout's frame, black sides of 380 px (21.6 mm)
SIDE = 110                              # either side may hold the camera's hole: nothing to read or tap within it
FOOT = LH - 50                          # the gesture zone under it
RAIL_L, RAIL_R = (SIDE + 6, FX0 - 10), (FX1 + 10, LW - SIDE - 6)       # the black sides' rails, 254 px (14.5 mm)


def land():
    img = Image.new('RGB', (LW, LH), (0, 0, 0))
    return img, ImageDraw.Draw(img)


def chrome_land(d):
    """The camera's hole at the left side's middle (the phone turned anticlockwise; turned the other way it is at the
    right); the gesture zone's handle at the foot."""
    d.ellipse((30, LH / 2 - 24, 78, LH / 2 + 24), fill=(0, 0, 0), outline=(70, 70, 76), width=3)
    d.rounded_rectangle((LW / 2 - 130, LH - 30, LW / 2 + 130, LH - 20), radius=5, fill=(120, 120, 124))


def reach_land(img):
    """Both thumbs, the phone held at its sides (~70 mm round a grip low on each side, as the tablet's)."""
    if GALLERY_MODE:
        return
    over = Image.new('RGBA', img.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(over)
    r, cy = round(70 * PX_MM), round(LH * 0.8)
    for cx, a0, a1 in ((-30, 270, 360), (LW + 30, 180, 270)):
        od.arc((cx - r, cy - r, cx + r, cy + r), a0, a1, fill=(255, 92, 160, 200), width=5)
    img.paste(Image.alpha_composite(img.convert('RGBA'), over).convert('RGB'))


def save_land(img, name, title, group, notes=()):
    """A landscape sheet: the phone's body, the notes in columns under it."""
    chrome_land(ImageDraw.Draw(img))
    if GALLERY_MODE:
        save_gallery(img, name)
        return
    B, head = 34, 170
    W, H = LW + 2 * B, LH + 2 * B
    cols = [notes[i::3] for i in range(3)] if notes else []
    probe = ImageDraw.Draw(Image.new('RGB', (1, 1)))
    note_h = max([sum(probe.multiline_textbbox((0, 0), p, font=GOTHIC(30), spacing=10)[3] + 40 for p in c)
                  for c in cols] + [0])
    sheet = Image.new('RGB', (W + 80, H + head + 60 + note_h), (232, 228, 220))
    mask = Image.new('L', (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, W - 1, H - 1), radius=130, fill=255)
    sheet.paste(Image.new('RGB', (W, H), (28, 28, 32)), (40, head), mask)
    smask = Image.new('L', (LW, LH), 0)
    ImageDraw.Draw(smask).rounded_rectangle((0, 0, LW - 1, LH - 1), radius=100, fill=255)
    sheet.paste(img, (40 + B, head + B), smask)
    sd = ImageDraw.Draw(sheet)
    sd.text((40, 40), f'{name} — {title}', font=GOTHIC(54), fill=(30, 30, 30))
    sd.text((40, 112), 'Poco X6 Pro landscape, 2712×1220 at 446 ppi (154.5 × 69.5 mm); the pink is the mock-up\'s notes',
            font=GOTHIC(30), fill=(110, 110, 110))
    for i, c in enumerate(cols):
        y = head + H + 40
        for para in c:
            sd.multiline_text((40 + i * (W // 3), y), para, font=GOTHIC(30), fill=(150, 30, 90), spacing=10)
            y = sd.multiline_textbbox((40 + i * (W // 3), y), para, font=GOTHIC(30), spacing=10)[3] + 40
    sheet.save(os.path.join(OUT, name + '.png'))
    SHEETS.setdefault(group, []).append((name, title, sheet))


def lm(px):
    """A size in layout pixels on the landscape phone."""
    return mm(px * LK)


def frame_dump(img, name, box=(0, 0, 1280, 800), at=(FX0, 0)):
    pic = dump(TOUCH, name).crop(box)
    img.paste(pic.resize((round(pic.width * LK), round(pic.height * LK)), Image.LANCZOS), at)


def l0():
    img, d = land()
    frame_dump(img, 'battle-turn.png')
    reach_land(img)
    d = ImageDraw.Draw(img)
    note(d, (SIDE + 10, 40), f'black side\n{mm(FX0)}', 30)
    note(d, (FX1 + 20, 40), f'black side\n{mm(FX0)}', 30)
    save_land(img, 'L0b', 'today\'s tablet layout, fitted to the height — the battle', 'land-frame', notes=[
        f'The 1280×800 layout at the phone\'s\nheight: 1952×1220 (111 × 69.5 mm),\none layout pixel {lm(1)}; black sides\n'
        f'of {mm(FX0)} on either hand.',
        f'A card 223×150: {lm(223)} × {lm(150)};\nits text (17 px) {lm(17)} — below\nAndroid\'s 12 sp (1.9 mm).',
        f'A rail button 116×54: {lm(116)} × {lm(54)}\n(a finger needs 7 mm); Melee 116×76:\n{lm(76)} high. '
        f'The cards\' order toggles\n66×32: {lm(66)} × {lm(32)}.',
        f'The scene\'s own type stays readable\n(48 px em: {lm(48)}); the battle\'s Skip\n150×34: {lm(34)} high.',
        'The thumbs (pink, ~70 mm round a\ngrip low on each side) reach all\nbut the top\'s middle: the height\n'
        'is no problem, the sizes are.',
        f'The camera\'s hole at a side\'s middle\n(either side, as the phone is\nturned): {mm(SIDE)} kept free on\n'
        'both sides; the gesture zone at\nthe foot (3 mm).'])

    img, d = land()
    frame_dump(img, 'map-markers.png')
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'L0m', 'today\'s tablet layout, fitted to the height — the map', 'land-frame', notes=[
        f'The map at 3.05 phone px a map\npixel (the PC\'s view, 640 × 400):\na flag {mm(24 * 3.05)} wide.',
        f'A plate ~30 px high: {lm(30)};\nits text {lm(16)}.',
        f'Commands 220×72: {lm(220)} × {lm(72)};\nthe drawer\'s cells 50×84: {lm(50)} ×\n{lm(84)} — '
        'narrow, but high enough.',
        f'The status strip\'s text (18 px):\n{lm(18)}; the command panel\'s rows\n(46 px) {lm(46)} high.',
        'So: the scenes and the map\'s\npicture work as they are; every\nscreen of ours needs larger type\nand '
        'targets — as upright, but the\nheight, not the width, is short.'])


def rail_cell(d, box, title, sub=None, colour=None, off=False):
    """A rail's row in the black side (254 wide): the command's tag over its name, as two lines."""
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=ROW, outline=OFF_TAG if off else DIM_GOLD, width=3)
    if colour:
        tw = d.textlength(sub, font=GOTHIC(26)) + 24
        d.rectangle((x0 + 14, y0 + 14, x0 + 14 + tw, y0 + 50), fill=OFF_TAG if off else colour)
        d.text((x0 + 14 + tw / 2, y0 + 32), sub, font=GOTHIC(26), fill=OFF_TEXT if off else TEXT, anchor='mm')
        d.text((x0 + 16, y1 - 34), title, font=fit(d, title, GOTHIC, 36, x1 - x0 - 30), fill=OFF_TEXT if off else TEXT,
               anchor='lm')
    else:
        d.text(((x0 + x1) / 2, (y0 + y1) / 2), title, font=fit(d, title, GOTHIC, 38, x1 - x0 - 30),
               fill=OFF_TEXT if off else TEXT, anchor='mm')


def drawer_cells(d, x0, x1, foot, skip_on=False):
    """The scenes' Skip and Log, each a plate of its own (the tablet's G3), at the left thumb."""
    h = 118
    for i, kind in enumerate(('log', 'skip')):
        y1 = foot - i * (h + 16)
        box = (x0, y1 - h, x1, y1)
        lit = kind == 'skip' and skip_on
        panel(d, box, fill=(96, 72, 30) if lit else PANEL)
        icon(d, kind, x0 + 60, y1 - h / 2, NAME if lit else GOLD)
        d.text((x0 + 150, y1 - h / 2), {'log': 'Log', 'skip': 'Skip'}[kind], font=GOTHIC(36), fill=TEXT, anchor='mm')
    return foot - 2 * h - 16


def menu_cell(d, box):
    """The menu bar's commands (File, Screen, Option, MsgSkip) behind one cell, as upright."""
    panel(d, box)
    icon(d, 'menu', box[0] + 60, (box[1] + box[3]) / 2)
    d.text((box[0] + 150, (box[1] + box[3]) / 2), 'Menu', font=GOTHIC(36), fill=TEXT, anchor='mm')


def side_status(d, x0, x1, y):
    """The status window's lines stacked in a black side."""
    d.text((x0 + 6, y), 'LP 3', font=CINZEL(34, 700), fill=NAME)
    d.text((x0 + 6, y + 46), 'Month 5 · Week 2', font=fit(d, 'Month 5 · Week 2', lambda s: CINZEL(s, 700), 30,
                                                         x1 - x0 - 12), fill=NAME)
    d.text((x0 + 6, y + 104), '116,400K', font=GOTHIC(42), fill=TEXT)
    d.text((x0 + 6, y + 154), 'GOLD', font=CINZEL(24, 600), fill=SUBTITLE)
    d.text((x0 + 6, y + 196), 'Troops 6,251', font=GOTHIC(32), fill=QUIET)
    return y + 246


def side_tags(d, x0, x1, y):
    """The week's uses, the strip's tags, stacked by group in a black side."""
    for group, items in TAGS:
        d.text((x0 + 6, y), group, font=CINZEL(22, 700), fill=SUBTITLE)
        y += 36
        for word, colour in items:
            d.rectangle((x0 + 6, y, x1 - 6, y + 46), fill=colour)
            d.text(((x0 + x1) / 2, y + 23), word, font=GOTHIC(28), fill=TEXT, anchor='mm')
            y += 54
        y += 12
    return y


def commands_cell(d, box, label='Commands', kind='menu'):
    """The map's button in a side: the icon over the label, the side being narrow."""
    panel(d, box)
    cx = (box[0] + box[2]) / 2
    icon(d, kind, cx, box[1] + 48)
    d.text((cx, box[3] - 40), label, font=fit(d, label, BRUSH, 36, box[2] - box[0] - 30), fill=NAME, anchor='mm')


LAND_VIEW = (1280, 208)                 # the gallery's map view (the touch dump's camera)


def lm1_frame(img, dim=0, ring=None, view=LAND_VIEW, plates=True):
    draw_map(img, (FX0, 0, FX1, LH), view[0], view[1], LK * 2, 36, ring=ring, dim=dim, plates=plates)
    return ImageDraw.Draw(img)


def lm1():
    img, d = land()
    d = lm1_frame(img)
    x0, x1 = RAIL_L
    menu_cell(d, (x0, 16, x1, 126))
    side_status(d, x0, x1, 160)
    drawer_cells(d, x0, x1, FOOT - 10)
    x0, x1 = RAIL_R
    side_tags(d, x0, x1, 24)
    commands_cell(d, (x0, FOOT - 10 - 150, x1, FOOT - 10))
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LM1a', 'the frame kept, the black sides as rails — the map wait', 'land-map', notes=[
        'The map as on the tablet, at the\nPC\'s view (640 × 400 map pixels,\n3.05 phone px each); our plates\n'
        'with 2 mm text (36 px), larger\nthan the layout\'s.',
        f'Left side: Menu (the menu bar\'s\ncommands) on top, the status\nstacked (date, budget, troops),\n'
        f'the drawer\'s Skip and Log at the\nthumb ({mm(118)} high).',
        'Right side: the week\'s uses as the\nstrip\'s tags by group; Commands\nat the right thumb.',
        f'The sides\' rails are {mm(RAIL_L[1] - RAIL_L[0])} wide:\nwhat is in them never covers\nthe map.',
        f'{mm(SIDE)} kept free at each edge for\nthe camera\'s hole (drawn left).'])

    img, d = land()
    d = lm1_frame(img, dim=150)
    land_panel_rails(img, d, close=True, inner=0)
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LM1b', 'the frame kept, the black sides as rails — Commands', 'land-map', notes=[
        'Commands fills both sides as the\ntablet\'s rails: the week\'s\ncommands left, the other items\n'
        'right, from the foot up; Turn END\non the right\'s top, Close where\nCommands was.',
        f'The rows {mm(123)} high and {mm(254)}\nwide: the tag over the name, two\nlines (the side has no room\n'
        'for both in one).',
        'The date and the budget stay at\nthe left\'s top; the map dimmed,\na tap on it closes.'])

    img, d = land()
    d = lm1_frame(img, ring=6)
    province_card(img, d, point_on((FX0, 0, FX1, LH), LAND_VIEW[0], LAND_VIEW[1], LK * 2, 6), (FX0 + 20, FX1 - 20))
    d = ImageDraw.Draw(img)
    x0, x1 = RAIL_L
    menu_cell(d, (x0, 16, x1, 126))
    side_status(d, x0, x1, 160)
    x0, x1 = RAIL_R
    province_rail(d, x0, x1)
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LM1c', 'the frame kept, the black sides as rails — a province', 'land-map', notes=[
        'A tap on Oaks rings it; its card\nbeside it as on the tablet (the\nowner\'s B), its type 2-2.5 mm;\n'
        'the commands on the right side,\nReturn where Commands was.',
        'The drawer hides under the menu,\nas on the tablet; the status\nstays.'])


def land_panel_rails(img, d, close=True, inner=0):
    """The command panel in the sides: THIS WEEK left (the date and budget over it), Turn END and the other items
    right, Close at the foot. `inner` widens the rails into the picture (LM2: lacquer over the map)."""
    lx0, lx1 = RAIL_L[0], RAIL_L[1] + inner
    rx0, rx1 = RAIL_R[0] - inner, RAIL_R[1]
    top = 150 if inner else 0           # under LM2's strip
    if inner:
        panel(d, (lx0 - 6, top, lx1 + 6, LH + 20))
        panel(d, (rx0 - 6, top, rx1 + 6, LH + 20))
    h, gap = 123, 12
    if not inner:                       # the strip's figures stay over the commands (LM2 keeps its strip)
        d.text((lx0 + 6, 20), 'LP 3', font=CINZEL(30, 700), fill=NAME)
        d.text((lx0 + 6, 60), 'Month 5 · Week 2',
               font=fit(d, 'Month 5 · Week 2', lambda s: CINZEL(s, 700), 28, lx1 - lx0 - 12), fill=NAME)
        d.text((lx0 + 6, 106), '116,400K GOLD', font=fit(d, '116,400K GOLD', GOTHIC, 34, lx1 - lx0 - 12), fill=TEXT)
    d.text((lx0 + 6, 176), 'THIS WEEK', font=CINZEL(26, 700), fill=SUBTITLE)
    for i, (word, colour, name) in enumerate(COMMANDS):
        y = FOOT - 10 - (i + 1) * h - i * gap
        if inner:
            tag_row(d, (lx0, y, lx1, y + h), word, colour, name, chip='5' if name == 'Construction' else None)
        else:
            rail_cell(d, (lx0, y, lx1, y + h), name, word, colour)
    red_button(d, (rx0, top + 16, rx1, top + 156), 'Turn END', 44)
    skip = 1 if close else 0
    for i, name in enumerate(LINKS):
        y = FOOT - 10 - (i + 1 + skip) * h - (i + skip) * gap
        if inner:
            row_box(d, (rx0, y, rx1, y + h), name, size=40)
        else:
            rail_cell(d, (rx0, y, rx1, y + h), name)
    if close:
        commands_cell(d, (rx0, FOOT - 10 - h, rx1, FOOT - 10), 'Close', 'close')


def province_card(img, d, at, span, w=560):
    """The tablet's card beside the tapped province (the owner's B), larger: left of it when there is room."""
    px, py = round(at[0]), round(at[1])
    x1 = px - 90
    x0 = x1 - w
    if x0 < span[0]:
        x0 = px + 90
        x1 = x0 + w
    y0 = max(20, min(py - 300, FOOT - 620))
    box = (x0, y0, x1, y0 + 600)
    panel(d, box)
    tip = x1 if x1 < px else x0
    d.polygon([(tip, py - 30), (tip + (40 if x1 < px else -40), py), (tip, py + 30)], fill=GOLD)
    pic = dump(TOUCH, 'province-menu.png').crop((568, 388, 838, 562))
    pic = pic.resize((w - 48, round((w - 48) * pic.height / pic.width)), Image.LANCZOS)
    img.paste(pic, (x0 + 24, y0 + 24))
    d = ImageDraw.Draw(img)
    d.rectangle((x0 + 24, y0 + 24, x0 + 24 + pic.width, y0 + 24 + pic.height), outline=GOLD, width=3)
    y = y0 + 24 + pic.height + 16
    d.text((x0 + 28, y), 'Oaks', font=BRUSH(52), fill=NAME)
    d.text((x1 - 28, y + 20), 'REBEL ARMY', font=CINZEL(26, 600), fill=SUBTITLE, anchor='ra')
    for i, label in enumerate(['Economy', 'Defense', 'Battle Area']):
        d.text((x0 + 28, y + 80 + i * 44), label, font=GOTHIC(34), fill=LABEL)
        d.text((x1 - 28, y + 80 + i * 44), '??', font=GOTHIC(34), fill=TEXT, anchor='ra')
    return box


def province_rail(d, x0, x1, inner=False):
    h, gap = 123, 12
    rows = [('Ninja', (84, 60, 120), 'Ninja Maneuver'), ('Battle', (128, 34, 30), 'Attack')]
    for i, (word, colour, name) in enumerate(reversed(rows)):
        y = FOOT - 10 - (i + 2) * h - (i + 1) * gap
        if inner:
            tag_row(d, (x0, y, x1, y + h), word, colour, name)
        else:
            rail_cell(d, (x0, y, x1, y + h), name, word, colour)
    commands_cell(d, (x0, FOOT - 10 - h, x1, FOOT - 10), 'Return', 'close')


def land_strip(d, x0, x1):
    """LM2's status strip over the map's top, in two rows: the status window's lines, then the week's uses."""
    panel(d, (0, 0, LW, 150))
    d.text((x0, 20), DATE, font=CINZEL(36, 700), fill=NAME)
    d.text((x0 + 560, 22), '116,400K GOLD', font=GOTHIC(38), fill=TEXT)
    d.text((x0 + 900, 26), 'Troops 6,251', font=GOTHIC(34), fill=QUIET)
    x, y = x0, 84
    for group, items in TAGS:
        d.text((x, y + 21), group, font=CINZEL(22, 700), fill=SUBTITLE, anchor='lm')
        x += d.textlength(group, font=CINZEL(22, 700)) + 14
        for word, colour in items:
            w = d.textlength(word, font=GOTHIC(28)) + 26
            d.rectangle((x, y, x + w, y + 44), fill=colour)
            d.text((x + w / 2, y + 22), word, font=GOTHIC(28), fill=TEXT, anchor='mm')
            x += w + 8
        x += 34


def lm2_map(img, dim=0, ring=None, view=None):
    view = view or (1920 - round(LW / (LK * 2)), 208 - 24)
    draw_map(img, (0, 0, LW, LH), view[0], view[1], LK * 2, 36, ring=ring, dim=dim)
    return ImageDraw.Draw(img), view


def lm2():
    img, d = land()
    d, view = lm2_map(img)
    land_strip(d, SIDE + 20, LW - SIDE - 20)
    menu_cell(d, (LW - SIDE - 270, 20, LW - SIDE - 16, 130))
    drawer_cells(d, SIDE + 6, SIDE + 260, FOOT - 10)
    commands_cell(d, (RAIL_R[0], FOOT - 10 - 150, RAIL_R[1], FOOT - 10))
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LM2a', 'the map across the whole screen — the map wait', 'land-map', notes=[
        f'The map over the whole screen at\nthe same scale (3.05): {LW / (LK * 2):.0f} × 400\nmap pixels '
        'in view, two fifths\nmore than the PC\'s 640.',
        'The status strip over the map\'s\ntop in two rows (2-2.2 mm): the\nstatus window\'s lines, then the\n'
        'week\'s uses by group; Menu at\nits right.',
        f'The drawer at the left thumb and\nCommands at the right one\n({mm(150)} high), as on the tablet.',
        'The map left of x 1280 shows the\ngame\'s first owners (made up);\nthe right part is the gallery\'s\n'
        'week 18.',
        f'Plates and flags may lie under\nthe camera\'s hole ({mm(SIDE)} at a\nside): a drag moves them out.'])

    img, d = land()
    d, view = lm2_map(img, dim=150)
    land_strip(d, SIDE + 20, LW - SIDE - 20)
    land_panel_rails(img, d, close=True, inner=260)
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LM2b', 'the map across the whole screen — Commands', 'land-map', notes=[
        f'The tablet\'s rails as lacquer over\nthe map\'s sides, {mm(514)} wide: the\ncommands in one line with their\n'
        f'tag, rows {mm(123)} high; Construction\'s\nweeks on its chip.',
        'Turn END on the right\'s top, Close\nwhere Commands was; the map\ndimmed, a tap on it closes.',
        'The strip stays: the week\'s uses\nand the commands both seen.'])

    img, d = land()
    d, view = lm2_map(img, ring=6)
    land_strip(d, SIDE + 20, LW - SIDE - 20)
    province_card(img, d, point_on((0, 0, LW, LH), view[0], view[1], LK * 2, 6), (SIDE + 20, LW - 500))
    d = ImageDraw.Draw(img)
    panel(d, (RAIL_R[0] - 12, 700, LW + 20, LH + 20))
    province_rail(d, *RAIL_R)
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LM2c', 'the map across the whole screen — a province', 'land-map', notes=[
        'Oaks ringed, its card beside it\n(the owner\'s B on the tablet),\nits commands on a lacquer rail\n'
        'at the right thumb, Return where\nCommands was.',
        f'The rail only {mm(254)} wide here\n(two-line rows, as LM1\'s): Oaks\nlies near the map\'s east edge,\n'
        'which the view cannot pass, so\na wider rail would cover it.'])


# The battle: the tablet's layout (touch.md, "The battle within the thumbs' reach") on the landscape phone.

LAND_CARDS = {                          # the touch dump's cards: our back row empty, Rance and Rick in front
    'player': [[None, 'Rance'], [None, 'Rick']],
    'enemy': [['Ex', 'Melfeis'], ['Sakanaku', 'Dodge']],
}


def land_card(img, box, name, player, acting=False, target=False, size=34, fs=150, toggles_h=0, units=UNITS,
              dim=False):
    """A battle card on the landscape phone: the face top left with its four figures under it, the text right;
    a fighter's card in an exchange has its action's pill at its foot (state 'pill'), the others `dim`."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    num, kind, trp, mtrp, (hp, mhp), state, stats = units[name]
    d.rectangle(box, fill=(88, 72, 50) if acting else (28, 22, 18),
                outline=(240, 230, 210) if acting else (GOLD if state == 'pill' else DIM_GOLD), width=4 if acting else 2)
    img.paste(face(name, fs), (x0 + 10, y0 + 10))
    d = ImageDraw.Draw(img)
    d.text((x0 + 10 + fs / 2, y0 + fs + 36), stats, font=GOTHIC(round(size * 0.8)), fill=TEXT, anchor='mm')
    tx, tw = x0 + fs + 24, x1 - x0 - fs - 38
    line = size * 1.25
    d.text((tx, y0 + 8), f'{num}  {name}', font=GOTHIC(round(size * 1.12)), fill=TEXT)
    y = y0 + 8 + size * 1.3
    d.text((tx, y), kind, font=fit(d, kind, GOTHIC, round(size * 0.9), tw), fill=QUIET)
    y += line
    d.text((tx, y), f'Trp. {trp:,}/{mtrp:,}', font=fit(d, f'Trp. {trp:,}/{mtrp:,}', GOTHIC, size, tw), fill=TEXT)
    y += line
    bar(d, tx, y, tw, trp, mtrp, PLAYER_BLUE if player else ENEMY_RED)
    y += 16
    d.text((tx, y), f'HP {hp}/{mhp}', font=GOTHIC(size), fill=TEXT)
    compact = y + line + 18 + size > y1 - 6         # no room for the state's own line: at the HP line's end
    if state and state != 'toggles' and compact:
        d.text((tx + tw, y), state, font=GOTHIC(round(size * 0.9)), fill=QUIET, anchor='ra')
    y += line
    bar(d, tx, y, tw, hp, mhp, BAR_GOOD)
    if state == 'toggles' and toggles_h:            # across the card's foot
        toggles(d, x0 + 10, y1 - toggles_h - 10, x1 - x0 - 20, round(size * 0.8), h=toggles_h)
    elif state == 'pill':                           # the exchange's action pill, at the card's foot right
        p = (x1 - 250, y1 - 74, x1 - 14, y1 - 16)
        d.rounded_rectangle(p, radius=29, fill=(226, 186, 96))
        d.line([(p[0] + 26, p[3] - 14), (p[0] + 56, p[1] + 14)], fill=(44, 26, 10), width=6)
        d.text((p[0] + 142, (p[1] + p[3]) / 2), 'Melee', font=GOTHIC(round(size * 0.95)), fill=(44, 26, 10),
               anchor='mm')
    elif state and state != 'toggles' and not compact:
        d.text((tx, y + 18), state, font=GOTHIC(round(size * 0.9)), fill=QUIET)
    if dim:
        veil(img, box, 0.5)
    if target:
        brackets(ImageDraw.Draw(img), box)


def land_board(img, top, card_h, size, fs, toggles_h, units=UNITS, cards=LAND_CARDS, acting='Rance', target='Ex',
               fighters=(), turns='4'):
    """The board under the scene in the frame: our two columns left, the gauge, the enemy's right; in an exchange
    the cards but the `fighters`' dimmed."""
    backdrop(img, (FX0, top, FX1, LH))
    d = ImageDraw.Draw(img)
    d.text((FX0 + 16, top + 24), 'Rance Army', font=CINZEL(30, 700), fill=TEXT, anchor='lm')
    d.text((FX1 - 16, top + 24), 'Leazas Rebel Army', font=CINZEL(30, 700), fill=TEXT, anchor='rm')
    d.text((FX1 - 16 - d.textlength('Leazas Rebel Army', font=CINZEL(30, 700)) - 24, top + 24), 'Walls +8%',
           font=GOTHIC(30), fill=(230, 196, 110), anchor='rm')
    gx = (FX0 + FX1) / 2
    gw, gap = 90, 12
    cw = (gx - gw / 2 - gap - FX0 - 12 - gap) // 2
    xs = [FX0 + 12, FX0 + 12 + cw + gap, gx + gw / 2 + gap, gx + gw / 2 + gap + cw + gap]
    y0 = top + 50
    for r in range(2):
        y = y0 + r * (card_h + gap)
        for c, name in enumerate(cards['player'][r] + cards['enemy'][r]):
            box = (round(xs[c]), y, round(xs[c] + cw), y + card_h)
            if name is None:
                empty_slot(d, box)
            else:
                land_card(img, box, name, c < 2, acting=name == acting, target=name == target, size=size, fs=fs,
                          toggles_h=toggles_h, units=units, dim=bool(fighters) and name not in fighters)
            d = ImageDraw.Draw(img)
    gh = 2 * card_h + gap
    for i in range(4):
        y = y0 + i * (gh - 60) / 4
        d.rounded_rectangle((gx - gw / 2 + 8, y, gx + gw / 2 - 8, y + (gh - 60) / 4 - 12), radius=10,
                            fill=(150, 120, 60) if turns != '0' else (40, 34, 28), outline=GOLD, width=3)
    d.text((gx, y0 + gh - 30), turns, font=BRUSH(44), fill=NAME, anchor='mm')
    return cw


def land_rails(img, d, skip=True):
    """The tablet's rails in the black sides: the acting unit's commands left, the target's attacks right, both from
    the foot up; the battle's Skip on the left rail's top."""
    x0, x1 = RAIL_L
    panel(d, (x0 - 6, 0, x1 + 6, LH))
    if skip:
        land_skip(d)
    d.text(((x0 + x1) / 2, 180), 'RANCE', font=BRUSH(46), fill=NAME, anchor='mm')
    d.line([(x0 + 20, 222), (x1 - 20, 222)], fill=DIM_GOLD, width=2)
    h, gap = 123, 12
    for i, name in enumerate(['Defend', 'Wait', 'Auto-Atk', 'Auto-Def']):
        y = FOOT - 10 - (i + 1) * h - i * gap
        row_box(d, (x0 + 8, y, x1 - 8, y + h), name, size=40, centred=True)
    y = FOOT - 10 - 5 * h - 4 * gap
    red_button(d, (x0 + 8, y, x1 - 8, y + h), 'Retreat', 42)
    x0, x1 = RAIL_R
    panel(d, (x0 - 6, 0, x1 + 6, LH))
    d.text(((x0 + x1) / 2, 60), 'TARGET', font=CINZEL(28, 700), fill=SUBTITLE, anchor='mm')
    d.text(((x0 + x1) / 2, 122), 'Ex', font=BRUSH(56), fill=(250, 222, 150), anchor='mm')
    d.line([(x0 + 20, 172), (x1 - 20, 172)], fill=DIM_GOLD, width=2)
    gold_button(d, (x0 + 8, FOOT - 10 - 260, x1 - 8, FOOT - 10), 'Melee', ('29-37', '0-1'))


def land_skip(d):
    """The battle's Skip on the left rail's top, where it stays while the rails are gone."""
    x0, x1 = RAIL_L
    b = (x0 + 8, 20, x1 - 8, 120)
    panel(d, b)
    icon(d, 'skip', b[0] + 56, 70)
    d.text((b[0] + 150, 70), 'Skip', font=GOTHIC(36), fill=TEXT, anchor='mm')


def land_scene(img, height, scene=None):
    """The scene in the frame: whole (625 px, the tablet's) or lower, its own 1024-wide picture sharp and its sides
    blurred out to the frame; `scene` a 1280×410 band (battle-turn's by default)."""
    scene = scene or dump(TOUCH, 'battle-turn.png').crop((0, 0, 1280, 410))
    if height >= round(410 * LK):
        img.paste(scene.resize((1952, round(410 * LK)), Image.LANCZOS), (FX0, 0))
        return round(410 * LK)
    sharp = scene.crop((128, 0, 1152, 410))
    w = round(1024 * height / 410)
    blur = sharp.resize((1952, height), Image.LANCZOS).filter(ImageFilter.GaussianBlur(18))
    blur = Image.blend(blur, Image.new('RGB', blur.size), 0.35)
    img.paste(blur, (FX0, 0))
    img.paste(sharp.resize((w, height), Image.LANCZOS), ((LW - w) // 2, 0))
    return height


def lb1():
    img, d = land()
    top = land_scene(img, 9999)
    land_board(img, top, 229, 34, 150, 0)
    d = ImageDraw.Draw(img)
    land_rails(img, d)
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LB1', 'the rails in the black sides, the PC\'s board', 'land-battle', notes=[
        f'The scene whole, as on every\ndevice (111 × {mm(625)}); the\nboard under it at the PC\'s width\n'
        'again, since the rails left the\nframe for the black sides.',
        f'The cards {mm(447)} × {mm(229)}: the face\nleft, the text 1.9-2.2 mm\n(34-38 px), the four figures\n'
        f'{mm(27)}.',
        f'The rails {mm(254)} wide, the rows\n{mm(123)} high, from the foot up as\nthe tablet\'s; Retreat on the\n'
        f'left\'s top, Melee ({mm(260)} with the\nforecast) at the right thumb.',
        'The battle\'s Skip on the left\nrail\'s top: it stays there while\nthe exchanges play and the\nrails are gone.',
        f'The order toggles ({lm(32)} high on\nthe phone) do not fit a finger:\nthe other own cards\' orders by\n'
        'the long press, as before them.'])


def lb2():
    img, d = land()
    top = land_scene(img, 438)
    land_board(img, top, 334, 40, 160, 80)
    d = ImageDraw.Draw(img)
    land_rails(img, d)
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LB2', 'the rails in the black sides, the scene lower and the cards taller', 'land-battle', notes=[
        f'The scene\'s own picture {mm(438)} high\n(70%), its sides blurred out to\nthe frame; the cards get the\n'
        'height.',
        f'The cards {mm(447)} × {mm(334)}: the text\n2.3-2.5 mm (40-45 px); Rick\'s\norder toggles {mm(80)} high, '
        'across\nthe card\'s foot (the tablet\'s\n6.5 mm).',
        'The rails as LB1\'s.',
        'The fighters\' sprites smaller\nthan on any other device.'])


def p8():
    l0()
    lm1(); lm2()
    lb1(); lb2()


# --- round P9: the deployment table and Subordinates, landscape (2026-10-27) ----------------------------------------

LAND_ROW = 112                          # a one-line ledger row: 6.4 mm


def side_talk(img, box, n):
    """Talk in a narrow place: the unit's face, the word beside it (the face says with whom)."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    panel(d, box)
    fs = y1 - y0 - 28
    img.paste(unit_face(n, fs), (x0 + 14, y0 + 14))
    d = ImageDraw.Draw(img)
    cx = (x0 + 14 + fs + x1) / 2
    d.text((cx, (y0 + y1) / 2), 'Talk', font=fit(d, 'Talk', BRUSH, 48, x1 - x0 - fs - 40), fill=NAME, anchor='mm')


def land_tabs(d, x0, y, active, w=300, h=88):
    for i, name in enumerate(TABS):
        x = x0 + i * (w + 12)
        on = i == active
        d.rectangle((x, y + (0 if on else 10), x + w, y + h + (2 if on else 0)), fill=PAPER if on else PAPER_DARK,
                    outline=None if on else PAPER_LINE)
        if on:
            d.rectangle((x, y, x + w, y + 8), fill=VERMILION)
        d.text((x + w / 2, y + h / 2 + (4 if on else 8)), name, font=CINZEL(34, 700),
               fill=INK_HEAD if on else INK_QUIET, anchor='mm')
    return y + h


# The one-line ledger's columns (x from the box's left; anchor): Overview and Troops.
LAND_COLS = {
    0: [('NO.', 150, 'ra'), ('NAME', 170, 'la'), ('TYPE', 440, 'la'), ('ROWS', 800, 'la'), ('TROOPS', 930, 'la'),
        ('REFILL', 1370, 'ma'), ('COST', -24, 'ra')],
    1: [('NO.', 150, 'ra'), ('NAME', 170, 'la')] + [(h, 560 + i * 136, 'ra') for i, h in enumerate(
        ['T.ATK', 'T.DEF', 'STRIKES', 'MAG', 'ATK', 'DEF', 'ATK', 'DEF', 'ATK', 'DEF'])],
}


def land_ledger(d, box, tab, order, chosen=None, party=True, refill=True):
    """The PC's ledger in one-line rows at the phone's type (2.2 mm): every column of a tab in one line."""
    x0, y0, x1, y1 = box
    k = min(1, (x1 - x0) / 1928)                    # a narrower ledger draws its columns closer, the type the same
    sx = lambda x: x0 + round(x * k)
    d.rectangle(box, fill=PAPER)
    head = 96 if tab == 1 else 64
    if tab == 1:
        for name, i in (('OFFENSE', 4), ('DEFENSE', 6), ('DUNGEON', 8)):
            cx = sx(560 + i * 136 + 68 - 50)
            d.text((cx, y0 + 12), name, font=CINZEL(22, 600), fill=INK_QUIET, anchor='ma')
            d.line([(cx - 110 * k, y0 + 40), (cx + 110 * k, y0 + 40)], fill=PAPER_LINE, width=2)
    for name, x, anchor in LAND_COLS[tab]:
        d.text((sx(x) if x > 0 else x1 + x, y0 + head - 40), name, font=CINZEL(26, 700), fill=INK_HEAD, anchor=anchor)
    arrow_mark(d, sx(164) if tab == 0 else sx(560) + 14, y0 + head - 26, 'up' if tab == 0 else 'down', 7)
    d.line([(x0, y0 + head), (x1, y0 + head)], fill=INK_HEAD, width=2)
    y = y0 + head + 2
    drawn = 0
    for n in order:
        if y + LAND_ROW > y1:
            break
        drawn += 1
        no, name, kind, rows, trp, mtrp, price, cost, place = UNIT[n]
        picked = party and place
        d.rectangle((x0, y, x1, y + LAND_ROW), fill=CHOSEN_ROW if n == chosen else PARTY_ROW if picked else PAPER)
        if n == chosen:
            d.rectangle((x0, y, x0 + 8, y + LAND_ROW), fill=VERMILION)
        if picked:
            seal(d, x0 + 20, y + 28, place)
        d.line([(x0, y + LAND_ROW), (x1, y + LAND_ROW)], fill=PAPER_LINE, width=2)
        cy = y + LAND_ROW / 2
        d.text((sx(150), cy), str(no), font=GOTHIC(40), fill=INK, anchor='rm')
        d.text((sx(170), cy), name, font=GOTHIC(40), fill=INK, anchor='lm')
        if tab == 0:
            d.text((sx(440), cy), kind, font=GOTHIC(36), fill=INK_QUIET, anchor='lm')
            d.text((sx(800), cy), rows, font=GOTHIC(36), fill=INK_QUIET, anchor='lm')
            d.text((sx(930), cy - 10), f'{trp:,}/{mtrp:,}', font=GOTHIC(38), fill=INK, anchor='lm')
            strength_bar(d, sx(930), cy + 22, round(260 * k), trp, mtrp)
            if price and refill:
                refill_button(d, (sx(1370) - 100, cy - 34, sx(1370) + 100, cy + 34), price)
            else:
                d.text((sx(1370), cy), price or '—', font=GOTHIC(38), fill=INK_HEAD if price else INK_QUIET,
                       anchor='mm')
            d.text((x1 - 24, cy), cost, font=GOTHIC(38), fill=INK, anchor='rm')
        else:
            for v, (_, x, _) in zip(TROOPS_TAB[n].split(), LAND_COLS[1][2:]):
                d.text((sx(x), cy), v, font=GOTHIC(38), fill=INK_QUIET if v == '0' else INK, anchor='rm')
        y += LAND_ROW
    if drawn < len(order):                          # more rows below: the scroll bar at the right
        d.rounded_rectangle((x1 - 14, y0 + head + 8, x1 - 4, y1 - 8), radius=5, fill=PAPER_LINE)
        d.rounded_rectangle((x1 - 14, y0 + head + 8, x1 - 4, y0 + head + 8 + (y1 - y0 - head - 16) * drawn / len(order)),
                            radius=5, fill=INK_QUIET)
    return y


def side_title(d, title, subtitle, x0, x1, y=20):
    """A full screen's title band, stacked in the left side."""
    d.text((x0 + 4, y), title, font=fit(d, title, BRUSH, 48, x1 - x0 - 8), fill=NAME)
    d.text((x0 + 4, y + 70), subtitle, font=fit(d, subtitle, lambda s: CINZEL(s, 600), 26, x1 - x0 - 8),
           fill=SUBTITLE)
    d.text((x0 + 4, y + 112), 'LP 3', font=CINZEL(30, 700), fill=NAME)
    d.text((x0 + 4, y + 150), 'Month 5 · Week 2', font=fit(d, 'Month 5 · Week 2', lambda s: CINZEL(s, 700), 28,
                                                            x1 - x0 - 8), fill=NAME)
    d.line([(x0 + 4, y + 200), (x1 - 4, y + 200)], fill=DIM_GOLD, width=2)
    d.text((x0 + 4, y + 220), 'BUDGET', font=CINZEL(24, 600), fill=SUBTITLE)
    d.text((x0 + 4, y + 254), f'{BUDGET:,}K', font=BRUSH(42), fill=NAME)
    return y + 320


def side_button(d, box, label, red=False, size=44):
    if red:
        red_button(d, box, label, size)
    else:
        panel(d, box)
        d.text(((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), label, font=fit(d, label, BRUSH, size, box[2] - box[0] - 30),
               fill=TEXT, anchor='mm')


def side_party_card(img, box, n, place):
    """A party card in a black side (254 wide): the face, the place's badge and the name beside it, the troops under."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    _, name, _, _, trp, mtrp, _, cost, _ = UNIT[n]
    d.rectangle(box, fill=ROW, outline=DIM_GOLD, width=2)
    fs = 96
    img.paste(unit_face(n, fs), (x0 + 10, y0 + 10))
    d = ImageDraw.Draw(img)
    badge(d, x0 + fs + 40, y0 + 36, place, r=22)
    d.text((x0 + fs + 70, y0 + 36), cost, font=GOTHIC(26), fill=QUIET, anchor='lm')
    d.text((x0 + fs + 22, y0 + 82), name, font=fit(d, name, GOTHIC, 36, x1 - x0 - fs - 34), fill=TEXT, anchor='lm')
    d.text((x0 + 12, y1 - 44), f'{trp:,}/{mtrp:,}', font=GOTHIC(32), fill=TEXT, anchor='lm')
    strength_bar(d, x0 + 12, y1 - 18, x1 - x0 - 24, trp, mtrp, track=(60, 50, 40))


def ld1():
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    x0, x1 = RAIL_L
    side_title(d, 'Deployment', 'Attack · up to 4', x0, x1)
    side_button(d, (x0, FOOT - 10 - 140, x1, FOOT - 10), 'Stop')
    x0, x1 = RAIL_R
    for i, (label, value) in enumerate((('TROOPS', '3,189'), ('WAR EXPENSES', '3,100K'))):
        d.text((x0 + 4, 30 + i * 100), label, font=CINZEL(24, 600), fill=SUBTITLE)
        d.text((x0 + 4, 62 + i * 100), value, font=GOTHIC(40), fill=TEXT)
    red_button(d, (x0, FOOT - 10 - 200, x1, FOOT - 10), 'Attack', 52)
    # The party across the frame's top as the battle lays our army out: the back row left, the front row right.
    cw, ch = (1952 - 24 - 3 * 12 - 60) // 4, 200
    y = 16
    d.text((FX0 + 12, y + 14), 'BACK ROW', font=CINZEL(24, 700), fill=SUBTITLE, anchor='lm')
    d.text((FX0 + 12 + 2 * cw + 12 + 60, y + 14), 'FRONT ROW', font=CINZEL(24, 700), fill=SUBTITLE, anchor='lm')
    arrow_mark(d, FX1 - 30, y + 14, 'right', 9, SUBTITLE)
    y += 34
    xs = [FX0 + 12, FX0 + 12 + cw + 12, FX0 + 12 + 2 * cw + 24 + 60, FX0 + 12 + 3 * cw + 36 + 60]
    for x, place in zip(xs, (3, 4, 1, 2)):
        party_card(img, (x, y, x + cw, y + ch), PARTY[place], place, face_size=150, kind=False)
    d = ImageDraw.Draw(img)
    top = y + ch + 20
    land_tabs(d, FX0 + 12, top, 0)
    land_ledger(d, (FX0 + 12, top + 88, FX1 - 12, LH - 12), 0, NUMBER_ORDER)
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LD1', 'the party across the top, the ledger under it', 'land-deployment', notes=[
        'The frame holds the screen: the\nparty as the battle lays our army\nout (the back row left, the front\n'
        f'row right; cards {mm(cw)} × {mm(ch)},\n✕ {mm(84)}), the ledger under it.',
        f'The ledger in one-line rows ({mm(LAND_ROW)},\ntext 2.2 mm): Overview\'s every\ncolumn in one line, no '
        f'sideways\nscroll; {(LH - 12 - top - 88 - 64) // LAND_ROW} rows in view, the rest a\ndrag away.',
        'The sides: the title, the date and\nthe budget over Stop at the left\nthumb; the troops and war\n'
        'expenses over Attack at the\nright one.',
        'A tap on a row picks the unit or\nputs it back; a long press shows\nits picture (PD2d).'])


LD2_SPLIT = 1700                        # LD2 changed: the ledger left of it, everything else right of it


def ld2():
    """LD2 with the owner's change (2026-10-27): the party two cards a row as on the battle screen, which widens the
    right part; so the ledger stands wholly at the left and the rest of the screen's information goes right."""
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    lx0, rx0, rx1 = SIDE + 6, LD2_SPLIT + 16, LW - SIDE - 6
    land_tabs(d, lx0, 16, 0)
    land_ledger(d, (lx0, 104, LD2_SPLIT, LH - 12), 0, NUMBER_ORDER)
    d.text((rx0, 14), 'Deployment', font=BRUSH(56), fill=NAME)
    d.text((rx1, 30), DATE, font=CINZEL(28, 700), fill=NAME, anchor='ra')
    d.text((rx0 + 2, 92), 'ATTACK · UP TO 4 UNITS', font=CINZEL(26, 600), fill=SUBTITLE)
    budget(d, rx1, 108, 36)
    divider(d, 146, rx0, rx1)
    cw, ch = (rx1 - rx0 - 12) // 2, 214
    y = 166
    d.text((rx0 + 4, y + 14), 'BACK ROW', font=CINZEL(24, 700), fill=SUBTITLE, anchor='lm')
    d.text((rx0 + cw + 16, y + 14), 'FRONT ROW', font=CINZEL(24, 700), fill=SUBTITLE, anchor='lm')
    arrow_mark(d, rx1 - 12, y + 14, 'right', 9, SUBTITLE)
    y += 34
    for r, places in enumerate(((3, 1), (4, 2))):   # as our army stands in the battle: the back row left
        for c, place in enumerate(places):
            x = rx0 + c * (cw + 12)
            party_card(img, (x, y, x + cw, y + ch), PARTY[place], place, face_size=150, kind=False)
        y += ch + 12
    d = ImageDraw.Draw(img)
    y += 10
    d.text((rx0 + 4, y + 24), 'TROOPS', font=CINZEL(26, 600), fill=SUBTITLE, anchor='lm')
    d.text((rx0 + 140, y + 24), '3,189', font=GOTHIC(40), fill=TEXT, anchor='lm')
    d.text((rx1, y + 24), '3,100K', font=GOTHIC(40), fill=TEXT, anchor='rm')
    d.text((rx1 - d.textlength('3,100K', font=GOTHIC(40)) - 16, y + 24), 'WAR EXPENSES', font=CINZEL(26, 600),
           fill=SUBTITLE, anchor='rm')
    side_button(d, (rx0, FOOT - 10 - 150, rx0 + 300, FOOT - 10), 'Stop')
    red_button(d, (rx0 + 316, FOOT - 10 - 150, rx1, FOOT - 10), 'Attack', 60)
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LD2', 'the ledger at the left, the party two a row at the right', 'land-deployment', notes=[
        f'The ledger wholly at the left in\none-line rows ({mm(LAND_ROW)}, text 2.2 mm):\nOverview\'s every column, '
        'no\nsideways scroll; nine rows in\nview of the week-18 save\'s nine.',
        'The owner\'s change: the party two\ncards a row as the battle shows\nour army (the back row left, the\n'
        f'front row right; cards {mm(cw)} ×\n{mm(ch)}, ✕ {mm(84)}), so the right part\nholds all the rest.',
        'At the right: the title, the date\nand the budget over the party,\nthe troops and the war\n'
        'expenses under it, Stop | Attack\nat the right thumb.'])


def lu_orders_side(img, d, n, figures):
    """The orders in the sides (LU1 with the owner's change, 2026-10-27: the sides swapped, Replenish kept right):
    Add New Troops left, from the foot up, the dearest on top; the title, Talk, Replenish and End right."""
    x0, x1 = RAIL_R
    side_title(d, 'Subordinates', '9 units ready', x0, x1)
    rep = (x0, FOOT - 10 - 140 - 14 - 150, x1, FOOT - 10 - 140 - 14)
    side_talk(img, (x0, rep[1] - 14 - 140, x1, rep[1] - 14), n)
    d = ImageDraw.Draw(img)
    replenish(d, rep, figures)
    side_button(d, (x0, FOOT - 10 - 140, x1, FOOT - 10), 'End')
    x0, x1 = RAIL_L
    d.text((x0 + 4, 26), 'ORDERS', font=BRUSH(40), fill=NAME)
    d.text((x0 + 4, 84), UNIT[n][1].upper(), font=CINZEL(30, 700), fill=TEXT)
    d.text((x0 + 4, 150), 'ADD NEW TROOPS', font=fit(d, 'ADD NEW TROOPS', lambda s: CINZEL(s, 700), 24, x1 - x0 - 8),
           fill=SUBTITLE)
    h, gap = 118, 10
    for i, (step, price) in enumerate(zip(('+25', '+50', '+100', '+200', '+400', '+800'), ADD_NEW[n])):
        y = FOOT - 10 - (i + 1) * h - i * gap
        off = int(price[:-1].replace(',', '')) > BUDGET
        d.rectangle((x0, y, x1, y + h), fill=ROW if not off else (30, 26, 22), outline=OFF_TAG if off else DIM_GOLD,
                    width=3)
        d.text((x0 + 20, y + 40), step, font=BRUSH(40), fill=OFF_TEXT if off else TEXT, anchor='lm')
        d.text((x1 - 16, y + h - 30), price, font=GOTHIC(30), fill=OFF_TEXT if off else QUIET, anchor='rm')
    return rep


def lu1():
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    lu_orders_side(img, d, 1, None)
    d = ImageDraw.Draw(img)
    land_tabs(d, FX0 + 12, 16, 1)
    land_ledger(d, (FX0 + 12, 104, FX1 - 12, LH - 12), 1, TROOPS_ORDER, chosen=1, party=False)
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LU1', 'the ledger fills the frame, the orders in the sides', 'land-subordinates', notes=[
        'The ledger as LD2\'s over the whole\nframe (shown: the Troops tab, its\nten figures in one line under\n'
        'their groups); the chosen unit\'s\nrow vermilion.',
        'The owner\'s change: the sides\nswapped. The left: the chosen\nunit\'s six Add New Troops from\n'
        'the foot up, the dearest on top;\ngreyed above the budget.',
        f'The right: the title, the date and\nthe budget; Talk (the unit\'s face),\nReplenish (greyed and "full" for\n'
        f'Rance) and End at the thumb,\n{mm(140)}-{mm(150)} high.'])

    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    rep = lu_orders_side(img, d, 12, '+1,191 · 2,300K')
    d = ImageDraw.Draw(img)
    land_tabs(d, FX0 + 12, 16, 1)
    land_ledger(d, (FX0 + 12, 104, FX1 - 12, LH - 12), 1, TROOPS_ORDER, chosen=12, party=False)
    cw, chh = 620, 300
    box = (rep[0] - 40 - cw, rep[3] - chh, rep[0] - 40, rep[3])
    panel(d, box, fill=(30, 22, 16))
    d.polygon([(box[2] - 2, box[3] - 100), (box[2] + 28, box[3] - 75), (box[2] - 2, box[3] - 50)], fill=GOLD)
    d.text((box[0] + 40, box[1] + 60), 'Are you sure?', font=GOTHIC(46), fill=TEXT, anchor='lm')
    bw = (cw - 40 * 2 - 24) // 2
    row_box(d, (box[0] + 40, box[1] + 130, box[0] + 40 + bw, box[1] + 260), 'No', size=46, face=BRUSH, centred=True)
    red_button(d, (box[0] + 40 + bw + 24, box[1] + 130, box[2] - 40, box[1] + 260), 'Yes', 46)
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LU1q', 'the ledger fills the frame — Replenish asks first', 'land-subordinates', notes=[
        'Haurein (unit 12) chosen:\nReplenish +1,191 troops for\n2,300K (the gallery\'s figures).',
        'The question in a callout left of\nthe button (the house\'s C3), over\nthe ledger\'s foot; Yes nearer\n'
        f'the thumb, No beside it, {mm(130)}\nhigh.',
        'Her Add New Troops prices: +400\nand +800 the gallery\'s, the\ncheaper ones derived.'])


def lu2():
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    x0, x1 = RAIL_L
    side_title(d, 'Subordinates', '9 units ready', x0, x1)
    x0, x1 = RAIL_R
    side_button(d, (x0, FOOT - 10 - 140, x1, FOOT - 10), 'End')
    land_tabs(d, FX0 + 12, 16, 1)
    deck = LH - 12 - 330
    land_ledger(d, (FX0 + 12, 104, FX1 - 12, deck - 12), 1, TROOPS_ORDER, chosen=1, party=False)
    panel(d, (FX0, deck, FX1, LH - 6))
    d.text((FX0 + 30, deck + 44), 'Orders', font=BRUSH(44), fill=NAME, anchor='lm')
    d.text((FX0 + 30 + d.textlength('Orders', font=BRUSH(44)) + 20, deck + 48), '· RANCE', font=CINZEL(30, 700),
           fill=TEXT, anchor='lm')
    rw = 300                                    # Replenish and Talk on one row (the owner's PU1)
    replenish(d, (FX0 + 24, deck + 90, FX0 + 24 + rw, deck + 220))
    talk_button(img, (FX0 + 24 + rw + 16, deck + 90, FX0 + 24 + 2 * rw + 16, deck + 220), 1)
    d = ImageDraw.Draw(img)
    gx0 = FX0 + 24 + 2 * rw + 40
    d.text((gx0, deck + 44), 'ADD NEW TROOPS', font=CINZEL(26, 700), fill=SUBTITLE, anchor='lm')
    w = (FX1 - 24 - gx0 - 2 * 12) // 3
    for i, (step, price) in enumerate(zip(('+25', '+50', '+100', '+200', '+400', '+800'), ADD_NEW[1])):
        x, y = gx0 + (i % 3) * (w + 12), deck + 90 + (i // 3) * 118
        off = int(price[:-1].replace(',', '')) > BUDGET
        d.rectangle((x, y, x + w, y + 106), fill=ROW if not off else (30, 26, 22), outline=OFF_TAG if off else DIM_GOLD,
                    width=3)
        d.text((x + 24, y + 53), step, font=BRUSH(44), fill=OFF_TEXT if off else TEXT, anchor='lm')
        d.text((x + w - 20, y + 53), price, font=GOTHIC(32), fill=OFF_TEXT if off else QUIET, anchor='rm')
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LU2', 'the ledger over the orders, as PU1', 'land-subordinates', notes=[
        'The owner\'s PU1 laid across: the\nledger on top (five rows in\nview), the orders under it —\n'
        'Replenish and Talk on one row\n(the owner\'s change), Add New\nTroops in two rows of three.',
        f'Replenish and Talk {mm(130)} high, the\nAdd New buttons {mm(106)}.',
        'End alone in the right side at\nthe thumb; the title, the date\nand the budget in the left.'])


def p9():
    ld1(); ld2()
    lu1(); lu2()


# --- round P10: the enemy attack and the week report, landscape (2026-10-27) ----------------------------------------

def side_danger(d, box, label='Abandon City'):
    """Abandon City in a black side (254 wide): the white flag over the label in two lines (the owner's P1)."""
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=(54, 18, 14), outline=(196, 70, 50), width=3)
    d.rectangle((x0 + 7, y0 + 7, x1 - 7, y1 - 7), outline=(120, 40, 30), width=1)
    cx = (x0 + x1) / 2
    d.line([(cx - 18, y0 + 30), (cx - 18, y0 + 92)], fill=(236, 226, 210), width=4)
    d.polygon([(cx - 16, y0 + 30), (cx + 24, y0 + 40), (cx - 16, y0 + 54)], fill=(236, 226, 210))
    for i, word in enumerate(label.split()):
        d.text((cx, y0 + 134 + i * 50), word, font=fit(d, word, BRUSH, 40, x1 - x0 - 30), fill=(236, 150, 110),
               anchor='mm')


def side_two_line(d, box, title, sub, red=False):
    """A main answer in a black side: its words in two lines, the sub-line under them."""
    x0, y0, x1, y1 = box
    if red:
        red_button(d, box, '')
    else:
        panel(d, box)
    words = title.split()
    cx = (x0 + x1) / 2
    top = y0 + (y1 - y0) * 0.38 - 27 * (len(words) - 1)     # in the red button's lighter half
    for i, word in enumerate(words):
        d.text((cx, top + i * 54), word, font=fit(d, word, BRUSH, 48, x1 - x0 - 30), fill=NAME, anchor='mm')
    d.text((cx, y1 - 40), sub, font=fit(d, sub, GOTHIC, 28, x1 - x0 - 24), fill=TEXT if red else QUIET, anchor='mm')


def ys_panel(img, d, box, pic_w, row_h=64, centre=False):
    """The defended province: Ys's picture and its four figures under it; `centre`: the two in the panel's middle
    (the caption stays at its top)."""
    x0, y0, x1, y1 = box
    panel(d, box)
    d.text((x0 + 28, y0 + 26), 'DEFENDING · YS', font=CINZEL(28, 600), fill=SUBTITLE)
    pic = dump(TOUCH, ATTACK_DUMP).crop((120, 204, 416, 400))
    pic = pic.resize((pic_w, round(pic_w * pic.height / pic.width)), Image.LANCZOS)
    top = y0 + 76
    if centre:
        top = max(top, round((y0 + y1) / 2 - (pic.height + 18 + len(YS_FIGURES) * row_h) / 2))
    framed(img, pic, (round((x0 + x1 - pic_w) / 2), top))
    d = ImageDraw.Draw(img)
    y = top + pic.height + 18
    for label, value in YS_FIGURES:
        d.text((x0 + 30, y + row_h / 2), label, font=GOTHIC(31), fill=QUIET, anchor='lm')
        d.text((x1 - 30, y + row_h / 2), value, font=GOTHIC(38), fill=TEXT, anchor='rm')
        d.line([(x0 + 30, y + row_h), (x1 - 30, y + row_h)], fill=DIM_GOLD, width=1)
        y += row_h
    return d


def rebels_panel(img, d, box, ch, face_max=200, centre=False):
    """The attackers as the PC shows them: the front row left, nearer Ys and facing it, the back row right;
    `centre`: the cards and the totals in the panel's middle (the caption stays at its top)."""
    x0, y0, x1, y1 = box
    panel(d, box)
    d.text((x0 + 28, y0 + 26), 'ATTACKING · LEAZAS REBEL ARMY', font=CINZEL(28, 600), fill=SUBTITLE)
    cx0 = x0 + 64                               # room for the triangle pointing at Ys
    cw = (x1 - 24 - cx0 - 16) // 2
    y = y0 + 76
    if centre:                                  # the row labels, the cards, the totals and the report's source
        y = max(y, round((y0 + y1) / 2 - (40 + 2 * ch + 14 + 20 + 106) / 2))
    for c, label in enumerate(('FRONT ROW', 'BACK ROW')):
        d.text((cx0 + c * (cw + 16) + cw / 2, y + 14), label, font=CINZEL(26, 700), fill=SUBTITLE, anchor='mm')
    arrow_mark(d, cx0 + cw / 2 - 120, y + 14, 'left', 8, SUBTITLE)
    y += 40
    for r in range(2):
        for c, place in enumerate((1 + r, 3 + r)):
            x = cx0 + c * (cw + 16)
            attacker_card(img, (x, y + r * (ch + 14), x + cw, y + r * (ch + 14) + ch), place, face_max)
    d = ImageDraw.Draw(img)
    my = y + ch + 7
    d.polygon([(x0 + 24, my), (x0 + 54, my - 34), (x0 + 54, my + 34)], fill=GOLD)
    y += 2 * ch + 14 + 20
    d.text((cx0, y + 28), 'COMMANDERS', font=CINZEL(24, 600), fill=SUBTITLE, anchor='lm')
    d.text((cx0 + 210, y + 28), '4', font=GOTHIC(38), fill=TEXT, anchor='lm')
    d.text((x1 - 28, y + 28), '1,556', font=GOTHIC(38), fill=TEXT, anchor='rm')
    d.text((x1 - 28 - d.textlength('1,556', font=GOTHIC(38)) - 16, y + 28), 'TOTAL TROOPS', font=CINZEL(24, 600),
           fill=SUBTITLE, anchor='rm')
    d.line([(cx0, y + 62), (x1 - 28, y + 62)], fill=DIM_GOLD, width=1)
    d.text((cx0, y + 76), "Kanami's report", font=GOTHIC(30), fill=QUIET)
    return d


KANAMI_ALONE = [('Kanami', 9, True)]           # the week-18 save's one ninja
# Five ninjas in service (made up: the week-18 save has Kanami alone): the scout menu's order and portraits
# (Battle.Flow.Defence.cs, FIGT_B.ADV:348-372), two of them spent this week.
FIVE_NINJAS = [('Kanami', 9, True), ('Freya', 449, False), ('Kaoru', 72, True), ('Wichita', 71, True),
               ('Saitou', 94, False)]


def ninja_callout(img, d, box, tip, way, ninjas=KANAMI_ALONE, rh=150):
    """Ninja Scout's list (the owner's B4): a row per ninja in service; `tip` the point of its triangle at the
    button, `way` the side the button lies on."""
    x0, y0, x1, y1 = box
    panel(d, box, fill=(30, 22, 16))
    tx, ty = tip
    if way == 'left':
        d.polygon([(x0 + 2, ty - 26), (tx, ty), (x0 + 2, ty + 26)], fill=GOLD)
    else:
        d.polygon([(tx - 26, y1 - 2), (tx + 26, y1 - 2), (tx, ty)], fill=GOLD)
    d.text((x0 + 30, y0 + 46), 'SEND TO SCOUT', font=CINZEL(28, 600), fill=SUBTITLE, anchor='lm')
    gap = 12
    cols = 2 if len(ninjas) > 1 else 1          # five ninjas in two columns: every one in view, the rows kept
    cw = (x1 - x0 - 48 - (cols - 1) * 16) // cols
    for i, (name, cg, free) in enumerate(ninjas):
        rx, ry = x0 + 24 + (i % cols) * (cw + 16), y0 + 86 + (i // cols) * (rh + gap)
        row = (rx, ry, rx + cw, ry + rh)
        d.rectangle(row, fill=ROW if free else (30, 26, 22), outline=DIM_GOLD if free else OFF_TAG, width=2)
        pic = cg_face(cg, rh - 24)
        if not free:
            pic = Image.blend(pic, Image.new('RGB', pic.size, (30, 26, 22)), 0.6)
        framed(img, pic, (row[0] + 12, row[1] + 12))
        d = ImageDraw.Draw(img)
        tx, tw = row[0] + rh + 12, row[2] - row[0] - rh - 24
        d.text((tx, row[1] + rh * 0.31), name.upper(), font=fit(d, name.upper(), lambda s: CINZEL(s, 700), 40, tw),
               fill=NAME if free else OFF_TEXT, anchor='lm')
        state = 'free this week' if free else 'spent this week'
        d.text((tx, row[1] + rh * 0.68), state, font=fit(d, state, GOTHIC, 32, tw), fill=QUIET if free else OFF_TEXT,
               anchor='lm')
    return rh


LA1_NINJA = (RAIL_L[0], FOOT - 10 - 300, RAIL_L[1], FOOT - 10)
LA1_INTERCEPT = (RAIL_R[0], FOOT - 10 - 400, RAIL_R[1], FOOT - 10)


def la1_base(img, d):
    """The PC's screen in the frame at the phone's type; the answers in the sides at the thumbs."""
    backdrop(img, (0, 0, LW, LH))
    x0, x1 = FX0 + 16, FX1 - 16
    flag_icon(img, (x0 + 8, 26), 78)
    d = ImageDraw.Draw(img)
    d.text((x0 + 104, 18), 'Enemy Attack', font=BRUSH(60), fill=NAME)
    tx = x0 + 104 + d.textlength('Enemy Attack', font=BRUSH(60)) + 44
    d.text((tx, 64), 'The Leazas Rebel Army appeared around Ys.', font=GOTHIC(40), fill=TEXT, anchor='lm')
    divider(d, 132, x0, x1)
    top, foot = 150, 862
    d = ys_panel(img, d, (x0, top, x0 + 650, foot), 500)
    d.text((x0 + 650 + 50, (top + foot) / 2), 'VS', font=BRUSH(40), fill=GOLD, anchor='mm')
    d = rebels_panel(img, d, (x0 + 750, top, x1, foot), 210)
    maris(img, (x0, foot + 14, x1, LH - 14), size=44)
    d = ImageDraw.Draw(img)
    # The sides: the date over Abandon City at the left's top, Ninja Scout at the left thumb; Intercept at the right.
    sx0, sx1 = RAIL_L
    side_danger(d, (sx0, 16, sx1, 256))
    side_two_line(d, LA1_NINJA, 'Ninja Scout', '1 of 1 free')
    sx0, sx1 = RAIL_R
    d.text((sx0 + 6, 24), 'LP 3', font=CINZEL(34, 700), fill=NAME)
    d.text((sx0 + 6, 70), 'Month 5 · Week 2', font=fit(d, 'Month 5 · Week 2', lambda s: CINZEL(s, 700), 30,
                                                       sx1 - sx0 - 12), fill=NAME)
    side_two_line(d, LA1_INTERCEPT, 'Intercept', 'choose the units', red=True)
    return d


def la1():
    img, d = land()
    la1_base(img, d)
    reach_land(img)
    save_land(img, 'LA1', 'the PC\'s screen in the frame, the answers in the sides', 'land-attack', notes=[
        'The frame holds the PC\'s screen\n(B) at the phone\'s type: the news,\nYs (its picture and four figures)\n'
        'and the rebels\' cards as on the PC\n— the front row left, facing Ys —,\nMaris\'s question across the foot.',
        f'The rebels\' cards {mm(533)} × {mm(210)},\nnames 2.4 mm; Ys\'s figures\n2.2 mm; Maris\'s lines in her\n'
        'own breaks, 2.5 mm.',
        f'The answers in the sides: Intercept\nat the right thumb ({mm(400)} high),\nNinja Scout at the left one, '
        f'Abandon\nCity (the danger button) at the\nleft\'s top, out of the thumbs\'\nway; {mm(254)} wide.',
        'The date at the right side\'s top,\nas the status stands in LM1.',
        'A tap on a rebel\'s card does\nnothing (as on the PC); Abandon\nCity asks first in a callout\n'
        'right of it, as Ninja Scout\'s list\nopens (LA1n).'])


def la1n():
    img, d = land()
    d = la1_base(img, d)
    b = LA1_NINJA
    box = (b[2] + 40, b[3] - 290, b[2] + 40 + 700, b[3])
    rh = ninja_callout(img, d, box, (b[2] + 4, (b[1] + b[3]) // 2 + 40), 'left')
    reach_land(img)
    save_land(img, 'LA1n', 'the PC\'s screen in the frame — Ninja Scout\'s list', 'land-attack', notes=[
        'Ninja Scout opens its list right\nof the button (the owner\'s B4),\nover Maris\'s question and Ys\'s\n'
        f'figures: a row per ninja in\nservice, the spent ones dimmed;\nrows {mm(rh)}.',
        'A tap on a free one sends her;\nthe places fill in as on the PC\n(here already the gallery\'s).',
        'Abandon City\'s question the\nsame way, right of its button.'])


LA2_COL = 1792                          # LA2: Ys and the rebels left of it, the news, Maris and the answers right


def la2_base(img, d, ninjas='1 of 1 free'):
    """The screen across the whole width, as LD2's own split: the facts at the left, the news, Maris's question and
    the answers in a column at the right thumb."""
    backdrop(img, (0, 0, LW, LH))
    x0, x1 = SIDE + 6, LW - SIDE - 6
    top, foot = 16, LH - 16
    d = ys_panel(img, d, (x0, top, x0 + 580, foot), 540, row_h=88, centre=True)
    d.text((x0 + 580 + 50, (top + foot) / 2), 'VS', font=BRUSH(40), fill=GOLD, anchor='mm')
    d = rebels_panel(img, d, (x0 + 680, top, LA2_COL - 16, foot), 300, face_max=170, centre=True)
    cx0 = LA2_COL
    flag_icon(img, (cx0 + 4, 24), 70)
    d = ImageDraw.Draw(img)
    d.text((cx0 + 92, 16), 'Enemy Attack', font=BRUSH(56), fill=NAME)
    d.text((x1, 108), DATE, font=CINZEL(28, 700), fill=NAME, anchor='ra')
    d.text((cx0 + 4, 166), 'The Leazas Rebel Army', font=GOTHIC(40), fill=TEXT)
    d.text((cx0 + 4, 216), 'appeared around Ys.', font=GOTHIC(40), fill=TEXT)
    divider(d, 286, cx0, x1)
    ans = FOOT - 10 - 130                   # Intercept half as high as first drawn (the owner's change)
    row1 = ans - 16 - 180
    maris(img, (cx0, row1 - 16 - 320, x1, row1 - 16), size=40, face_max=140)
    d = ImageDraw.Draw(img)
    half = (x1 - cx0 - 16) // 2
    danger_button(d, (cx0, row1 + 18, cx0 + round(half * 0.9), row1 + 162), 'Abandon City')     # 10% lower
    two_line_button(d, (cx0 + half + 16, row1, x1, row1 + 180), 'Ninja Scout', ninjas)
    two_line_button(d, (cx0, ans, x1, FOOT - 10), 'Intercept', 'choose the units', red=True)
    return d, (cx0 + half + 16, row1, x1, row1 + 180)


def la2():
    img, d = land()
    la2_base(img, d)
    reach_land(img)
    save_land(img, 'LA2', 'across the whole width, the answers in a column at the right thumb', 'land-attack', notes=[
        'As LD2 re-split its screen: the\nfacts at the left over the whole\nheight — Ys and the rebels as on\n'
        'the PC, the front row facing Ys —,\nthe news, Maris\'s question and\nthe answers in a column at the right.',
        f'The rebels\' cards {mm(438)} × {mm(300)}\n(taller than LA1\'s), names\n2.4 mm; Ys\'s picture {mm(540)}\n'
        'wide. The owner\'s change: both\npanels\' content in their middle,\nthe captions at their top.',
        'The right column as upright\'s\nPA1 foot: Maris\'s question, then\nAbandon City (10% narrower and\n'
        f'10% lower, {mm(144)}: the owner\'s\nchanges) | Ninja Scout ({mm(180)}),\nIntercept at the thumb, half as\n'
        f'high as first drawn ({mm(130)} ×\n{mm(LW - SIDE - 6 - LA2_COL)}).',
        'One thumb answers; the question\nstands right over its answers, as\non the PC and upright.',
        f'{mm(SIDE)} kept free at both edges\nfor the camera\'s hole.'])


def la2n():
    img, d = land()
    d, b = la2_base(img, d, '3 of 5 free')
    cx = (b[0] + b[2]) // 2
    rh = 128                                # three rows of two under the news, the column's width: all five in view
    h = 86 + 3 * rh + 2 * 12 + 24
    box = (LA2_COL, b[1] - 30 - h, LW - SIDE - 6, b[1] - 30)
    ninja_callout(img, d, box, (cx, b[1] - 6), 'down', FIVE_NINJAS, rh)
    reach_land(img)
    save_land(img, 'LA2n', 'across the whole width — Ninja Scout\'s list', 'land-attack', notes=[
        'Ninja Scout\'s list above its\nbutton, over Maris\'s question\n(as upright\'s PA1n): a row per\n'
        f'ninja in service, rows {mm(rh)}; the\nspent ones dimmed.',
        'All five ninjas the game has\n(Kanami, Freya, Kaoru, Wichita,\nSaitou — made up here: the\n'
        'week-18 save has Kanami alone)\nfit in two columns, in the\nscout menu\'s order: no scrolling.',
        'Abandon City\'s question the\nsame way, above its button.'])


# The week report: LM1's map under it (the report comes before COMMAND's map wait).

def lw1():
    img, d = land()
    d = lm1_frame(img, dim=110, plates=False)
    x0, x1 = RAIL_L
    menu_cell(d, (x0, 16, x1, 126))
    drawer_cells(d, x0, x1, FOOT - 10)
    w, h = 1320, 940
    bx0, by0 = (LW - w) // 2, (LH - h) // 2
    week_card(img, (bx0, by0, bx0 + w, by0 + h))
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LW1', 'the report in the middle over the dimmed map', 'land-report', notes=[
        'The PC\'s card (and upright\'s PW1)\nin the middle of the frame over\nthe dimmed map: the banner, the\n'
        'treasury as an account, the\nbudget, the troops replenished.',
        f'The card {mm(w)} × {mm(h)}: the banner\n3.5 mm, the account\'s figures\n2.5 mm, the budget 3.5 mm.',
        'A tap anywhere goes on (the\ntriangle asks for it); the left\nside as in the scenes: Menu on\n'
        'top, Skip and Log at the thumb.',
        'The thieves\' week: the banner\nalone on the same card.'])


def lw2():
    img, d = land()
    d = lm1_frame(img, plates=False)
    x0, x1 = RAIL_L
    menu_cell(d, (x0, 16, x1, 126))
    flag_icon(img, (x0 + 4, 160), 64, 'report')
    d = ImageDraw.Draw(img)
    for i, word in enumerate(('Leazas', 'Army Phase')):
        d.text((x0 + 4, 240 + i * 58), word, font=fit(d, word, BRUSH, 46, x1 - x0 - 8), fill=NAME)
    d.text((x0 + 6, 366), 'LP 3', font=CINZEL(32, 700), fill=NAME)
    d.text((x0 + 6, 408), 'Month 5 · Week 2', font=fit(d, 'Month 5 · Week 2', lambda s: CINZEL(s, 700), 30,
                                                        x1 - x0 - 12), fill=NAME)
    drawer_cells(d, x0, x1, FOOT - 10)
    x0, x1 = RAIL_R
    d.text((x0 + 4, 22), 'TREASURY', font=CINZEL(26, 700), fill=SUBTITLE)
    y = 64
    for sign, label, sub, value, colour in REPORT:
        d.text((x0 + 4, y), f'{sign} {label}'.strip(), font=fit(d, f'{sign} {label}'.strip(),
                                                                lambda s: CINZEL(s, 600), 28, x1 - x0 - 8), fill=TEXT)
        d.text((x0 + 4, y + 38), sub.upper(), font=fit(d, sub.upper(), lambda s: CINZEL(s, 600), 24, x1 - x0 - 8),
               fill=SUBTITLE)
        d.text((x1 - 4, y + 70), value, font=GOTHIC(44), fill=colour, anchor='ra')
        for xx in range(x0 + 4, x1 - 4, 14):
            d.line([(xx, y + 128), (xx + 6, y + 128)], fill=DIM_GOLD, width=1)
        y += 140
    b = (x0, y + 4, x1, y + 150)
    d.rectangle(b, fill=(30, 22, 16), outline=GOLD, width=2)
    d.text((x0 + 16, y + 20), 'BUDGET', font=CINZEL(28, 700), fill=NAME)
    d.text((x1 - 14, y + 64), '116,400', font=BRUSH(50), fill=NAME, anchor='ra')
    d.text((x1 - 14, y + 120), 'K GOLD', font=CINZEL(24, 600), fill=SUBTITLE, anchor='ra')
    y += 180
    d.text((x0 + 4, y), 'TROOPS', font=CINZEL(26, 700), fill=SUBTITLE)
    b = (x0, y + 40, x1, y + 170)
    d.rectangle(b, fill=(30, 22, 16), outline=DIM_GOLD, width=2)
    d.text((x0 + 16, y + 58), 'Replenished', font=CINZEL(26, 600), fill=TEXT)
    d.text((x1 - 14, y + 104), '+1,385', font=BRUSH(48), fill=(150, 196, 110), anchor='ra')
    triangle(d, (x0 + x1) / 2, FOOT - 60, 30)
    reach_land(img)
    d = ImageDraw.Draw(img)
    save_land(img, 'LW2', 'the report in the sides, the map clear', 'land-report', notes=[
        'LM1\'s principle as far as it goes:\nthe frame shows the map as the\nweek starts, undimmed (no plates\n'
        'before the map wait, as on the\nPC); the report stands in the\nblack sides.',
        'Left: Menu, the banner and the\ndate where LM1\'s status will be,\nSkip and Log at the thumb.',
        f'Right: the treasury as an account\nstacked down the side (the\nfigures {mm(44)}), the budget {mm(50)},\n'
        'the troops replenished, the\ntriangle at the right thumb —\nwhere Commands comes next.',
        'A tap anywhere goes on. The\nthieves\' week: the banner alone,\nthe right side empty.'])


def p10():
    la1(); la1n(); la2(); la2n()
    lw1(); lw2()


# --- round P11: Harem, Treasure and Save/Load, landscape (2026-10-28) -----------------------------------------------

LX0, LX1 = SIDE + 6, LW - SIDE - 6         # the whole width between the hole's strips
FOLD = 9                                   # the folded grid: the picker's rows 1-9 left, 10-17 right
PIC_W1, PIC_W2 = 1240, 1104                # the picture's width: beside the scrolled grid, beside the folded one


def words_mm(width):
    """The picture's own words (its description column) at a picture `width` wide: 2.4 mm at upright's 1188 (PH2)."""
    return f'{2.4 * width / 1188:.1f} mm'


def gilt(img, pic, xy):
    img.paste(pic, xy)
    d = ImageDraw.Draw(img)
    d.rectangle((xy[0], xy[1], xy[0] + pic.width, xy[1] + pic.height), outline=GOLD, width=3)
    return d


def column_title(d, title, x0, x1, y=14):
    """A title band over one column: the Shojumaru title, the date right, a divider."""
    d.text((x0 + 4, y), title, font=BRUSH(56), fill=NAME)
    d.text((x1, y + 22), DATE, font=CINZEL(28, 700), fill=NAME, anchor='ra')
    divider(d, y + 96, x0, x1)
    return y + 110


def scroll_bar(d, x, y0, y1, shown, total):
    d.rounded_rectangle((x, y0, x + 12, y1), radius=6, fill=(40, 32, 24))
    d.rounded_rectangle((x, y0, x + 12, y0 + (y1 - y0) * shown // total), radius=6, fill=DIM_GOLD)


def scrolled_grid(d, box, draw_cell, row_h=118):
    """The picker's grid in finger-sized cells, the rows that do not fit a drag away (its scroll bar at the right)."""
    x0, y0, x1, y1 = box
    picker_grid(d, (x0, y0, x1 - 30, y1), GRID_ROWS, draw_cell, row_h)
    shown = (y1 - y0 + 6) // (row_h + 6)
    scroll_bar(d, x1 - 14, y0, y0 + shown * (row_h + 6) - 6, shown, GRID_ROWS)
    return shown


def folded_grid(d, box, draw_cell, cells=3 * GRID_ROWS, gap=40):
    """The picker's 3 × 17 grid cut in two after its ninth row: rows 1-9 left, 10-17 right, every cell at its
    number's place and in view. `cells`: the harem's grid has 50 (the 51st is the script's resting place)."""
    x0, y0, x1, y1 = box
    bw = (x1 - x0 - gap) / 2
    w = (bw - 2 * 8) / 3
    row_h = (y1 - y0 + 6) // FOLD - 6
    for r in range(GRID_ROWS):
        bx, rr = (x0, r) if r < FOLD else (x0 + bw + gap, r - FOLD)
        y = y0 + rr * (row_h + 6)
        for c in range(3):
            if r * 3 + c >= cells:
                continue
            cb = (round(bx + c * (w + 8)), y, round(bx + c * (w + 8) + w), y + row_h)
            d.rectangle(cb, fill=(28, 22, 18), outline=(44, 36, 28), width=2)
            draw_cell(d, cb, r, c)
    mx = x0 + bw + gap / 2
    d.line([(mx, y0 + 12), (mx, y1 - 12)], fill=DIM_GOLD, width=2)
    return row_h, round(w)


def treasure_cell_two(chosen):
    """A narrow cell (the folded grid): the item's name over its holder, no room for both in one line."""
    def draw(d, cb, r, c):
        entry = ITEM_CELLS.get((r, c))
        if not entry:
            return
        name, holder = entry
        on = name == chosen
        d.rectangle(cb, fill=(70, 54, 30) if on else ROW, outline=GOLD if on else DIM_GOLD, width=3 if on else 2)
        h = cb[3] - cb[1]
        d.text((cb[0] + 14, cb[1] + h * 0.36), name, font=fit(d, name, GOTHIC, 34, cb[2] - cb[0] - 26), fill=TEXT,
               anchor='lm')
        d.text((cb[2] - 14, cb[1] + h * 0.74), holder, font=GOTHIC(26), fill=QUIET, anchor='rm')
    return draw


def lh1():
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    pic = harem_picture(PIC_W1)
    d = gilt(img, pic, (LX0, 16))
    top = 16 + pic.height + 14
    panel(d, (LX0, top, LX0 + PIC_W1, FOOT - 10))
    girl_details(d, LX0 + 34, top + 18, LX0 + PIC_W1 - 34)
    red_button(d, (LX0 + 26, FOOT - 10 - 26 - 136, LX0 + PIC_W1 - 26, FOOT - 10 - 26), 'Summon Anasel', 56)
    rx0 = LX0 + PIC_W1 + 30
    y = column_title(d, 'Harem', rx0, LX1)
    end = (LX1 - 360, FOOT - 10 - 140, LX1, FOOT - 10)
    shown = scrolled_grid(d, (rx0, y + 6, LX1, end[1] - 16), harem_cell('Anasel'))
    side_button(d, end, 'End')
    reach_land(img)
    save_land(img, 'LH1', 'the PC\'s split, the grid scrolled', 'land-harem', notes=[
        'The PC\'s screen across the whole\nwidth: the girl\'s picture at the\nleft, her details and Summon under\n'
        'it; the title, the date and the\ngrid at the right, End at its foot.',
        f'The picture {mm(PIC_W1)} wide (its words\n{words_mm(PIC_W1)}, as upright\'s PH2); Summon\nAnasel {mm(136)} high '
        'under her\ndetails, at the left thumb.',
        f'The grid in cells of {mm(118)}: {shown} of its\n17 rows in view, the rest a drag\naway (its scroll bar at '
        'the right);\nEnd at the right thumb.',
        'A tap chooses a girl (her picture\nand details at the left); the red\nbutton summons — the choice\n'
        'then the red button is the\nconfirmation.'])


def lh2():
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    y = column_title(d, 'Harem', LX0, LX0 + PIC_W2)
    pic = harem_picture(PIC_W2)
    d = gilt(img, pic, (LX0, y + 4))
    girl_details(d, LX0 + 14, y + 4 + pic.height + 12, LX0 + PIC_W2)
    foot = (FOOT - 10 - 140, FOOT - 10)
    side_button(d, (LX0, foot[0], LX0 + 320, foot[1]), 'End')
    red_button(d, (LX0 + 336, foot[0], LX0 + PIC_W2, foot[1]), 'Summon Anasel', 56)
    row_h, w = folded_grid(d, (LX0 + PIC_W2 + 40, 16, LX1, FOOT - 10), harem_cell('Anasel'), cells=50)
    reach_land(img)
    save_land(img, 'LH2', 'the grid folded in two, every girl in view', 'land-harem', notes=[
        'The picker\'s grid cut in two after\nits ninth row: rows 1-9 at the\nleft, 10-17 at the right — '
        'each\ngirl still at her number\'s cell,\nthe order read down the left\nhalf, then the right.',
        f'All 50 cells in view, no drag:\n{mm(row_h)} × {mm(w)} each, the names\nshrunk to fit where long; the grid\n'
        'at the right thumb over the\nwhole height.',
        f'The title, the picture ({mm(PIC_W2)} wide,\nits words {words_mm(PIC_W2)}) and her details\nat the left; End | '
        f'Summon Anasel\nunder them at the left thumb\n({mm(140)} high).'])


def lt1():
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    pic = treasure_picture(PIC_W1)
    d = gilt(img, pic, (LX0, 16))
    top = 16 + pic.height + 14
    panel(d, (LX0, top, LX0 + PIC_W1, FOOT - 10))
    item_details(d, LX0 + 34, top + 20, LX0 + PIC_W1 - 500, None)
    red_button(d, (LX0 + PIC_W1 - 26 - 440, FOOT - 10 - 26 - 150, LX0 + PIC_W1 - 26, FOOT - 10 - 26), 'Take Off', 54)
    d.text((LX0 + 34, top + 232), 'Tap a character, then the item —', font=GOTHIC(30), fill=QUIET)
    d.text((LX0 + 34, top + 274), 'or an item, then the character.', font=GOTHIC(30), fill=QUIET)
    rx0 = LX0 + PIC_W1 + 30
    y = column_title(d, 'Treasure', rx0, LX1)
    foot = (FOOT - 10 - 130, FOOT - 10)
    shown = scrolled_grid(d, (rx0, y + 6, LX1, foot[0] - 16), treasure_cell('L. Holy Sword'))
    lacquer_tabs(d, foot[0], ['Characters', 'Items'], 1, rx0, LX1 - 380, h=foot[1] - foot[0])
    side_button(d, (LX1 - 360, foot[0], LX1, foot[1]), 'End')
    reach_land(img)
    save_land(img, 'LT1', 'the PC\'s split, the grid scrolled', 'land-treasure', notes=[
        'The PC\'s screen across the whole\nwidth: the item\'s picture at the\nleft, its name, its holder and the\n'
        'red button under it; the grid at\nthe right, Characters | Items and\nEnd at its foot (as on the PC).',
        f'The picture {mm(PIC_W1)} wide: its words\n(what the item does) {words_mm(PIC_W1)}, as\nupright\'s PT2. The '
        'red button\nat the panel\'s right as the\ntablet\'s (round X), Take Off here.',
        f'The grid in cells of {mm(118)}: {shown} of its\n17 rows in view, the rest a drag\naway; the tabs and End '
        f'{mm(130)}\nhigh at the right thumb.'])


def lt2():
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    y = column_title(d, 'Treasure', LX0, LX0 + PIC_W2)
    pic = treasure_picture(PIC_W2)
    d = gilt(img, pic, (LX0, y + 4))
    y += 4 + pic.height + 12
    item_details(d, LX0 + 14, y, LX0 + PIC_W2 - 470, None)
    red_button(d, (LX0 + PIC_W2 - 440, y + 10, LX0 + PIC_W2, y + 160), 'Take Off', 54)
    foot = (FOOT - 10 - 130, FOOT - 10)
    lacquer_tabs(d, foot[0], ['Characters', 'Items'], 1, LX0, LX0 + PIC_W2 - 336, h=foot[1] - foot[0])
    side_button(d, (LX0 + PIC_W2 - 320, foot[0], LX0 + PIC_W2, foot[1]), 'End')
    row_h, w = folded_grid(d, (LX0 + PIC_W2 + 40, 16, LX1, FOOT - 10), treasure_cell_two('L. Holy Sword'))
    long_name = fit(d, 'L. Holy Sword', GOTHIC, 34, w - 26).size
    reach_land(img)
    save_land(img, 'LT2', 'the grid folded in two, every item in view', 'land-treasure', notes=[
        'The grid folded as LH2\'s: rows\n1-9 at the left, 10-17 at the\nright, all 51 cells in view at\n'
        f'the right thumb ({mm(row_h)} × {mm(w)}).',
        'The cells being narrow, an\nitem\'s name stands over its\nholder (Rance, Rick); a long\n'
        f'name shrinks (L. Holy Sword\n{mm(long_name)}, the others {mm(34)}).',
        f'The picture {mm(PIC_W2)} wide (its words\n{words_mm(PIC_W2)}); Take Off beside the\nitem\'s details; '
        'Characters | Items\nand End at the left thumb.'])


def land_back(d, box):
    x0, y0, x1, y1 = box
    panel(d, box)
    cy = (y0 + y1) / 2
    ax = 40 if x1 - x0 < 300 else 52           # a black side's Back (LL2): the triangle nearer the edge
    d.polygon([(x0 + ax + 32, cy - 18), (x0 + ax, cy), (x0 + ax + 32, cy + 18)], fill=GOLD)
    d.text(((x0 + ax + 44 + x1) / 2, cy), 'Back', font=fit(d, 'Back', BRUSH, 50, x1 - x0 - ax - 70), fill=TEXT,
           anchor='mm')


def bin_button(d, box, stacked=False):
    """Delete: the danger button with its bin (the owner's B5); `stacked`: the bin over the word, for a black side."""
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=(54, 18, 14), outline=(196, 70, 50), width=3)
    ink = (236, 150, 110)
    bx, by = ((x0 + x1) / 2 - 14, y0 + 34) if stacked else (x0 + 30, (y0 + y1) / 2 - 12)
    s = 1.6 if stacked else 1
    d.rectangle((bx, by + 2 * s, bx + 22 * s, by + 32 * s), outline=ink, width=3)
    d.line([(bx - 6 * s, by), (bx + 28 * s, by)], fill=ink, width=3)
    if stacked:
        d.text(((x0 + x1) / 2, y1 - 44), 'Delete', font=CINZEL(34, 700), fill=ink, anchor='mm')
    else:
        d.text(((x0 + 70 + x1) / 2, (y0 + y1) / 2), 'Delete', font=CINZEL(30, 700), fill=ink, anchor='mm')


def land_save_list(d, box, rows_h=104, chosen=12, saves=PL2_SAVES, tabs=True):
    """PL2's list in its room: the two lists' tabs, the heading with the sweep, one-line rows newest first, the rows
    that do not fit a drag away (the scroll bar)."""
    x0, y0, x1, y1 = box
    y = y0
    if tabs:
        y = lacquer_tabs(d, y, [f'Manual saves · {len(saves)}', 'Auto-saves · 20'], 0, x0, x1, h=96) + 16
    d.text((x0 + 8, y + 32), 'NEWEST FIRST', font=CINZEL(26, 600), fill=SUBTITLE, anchor='lm')
    b = (x1 - 440, y, x1, y + 64)
    d.rectangle(b, fill=ROW, outline=DIM_GOLD, width=2)
    d.text(((b[0] + b[2]) / 2 + 6, y + 32), 'Delete all without', font=GOTHIC(28), fill=TEXT, anchor='rm')
    star(d, (b[0] + b[2]) / 2 + 28, y + 32, 12)
    d.text(((b[0] + b[2]) / 2 + 48, y + 32), f'({sum(1 for s in saves if not s[3])})', font=GOTHIC(28), fill=TEXT,
           anchor='lm')
    y += 84
    shown = min(len(saves), (y1 - y + 10) // (rows_h + 10))
    rx1 = x1 if shown == len(saves) else x1 - 30
    if shown < len(saves):
        scroll_bar(d, x1 - 14, y, y + shown * (rows_h + 10) - 10, shown, len(saves))
    for s in saves[:shown]:
        save_row(d, (x0, y, rx1, y + rows_h), s, chosen=s[0] == chosen, two_lines=False)
        y += rows_h + 10
    return shown


def land_save_page(img, box, cols=3, number=12, delete=True, foot=0):
    """The chosen save's page in `box`: its title (and Delete beside it unless a black side holds it), its map at
    the box's width — cut lower to leave the figures, the comment and `foot` px under it —, the figures in `cols`
    columns, the comment and Edit."""
    x0, y0, x1, y1 = box
    d = ImageDraw.Draw(img)
    y = y0
    d.text((x0 + 4, y + 40), f'No. {number} · LP 3 · Month 5 · Week 2', font=CINZEL(36, 700), fill=NAME, anchor='lm')
    if delete:
        bin_button(d, (x1 - 240, y, x1, y + 84))
    y += 100
    rows = -(-len(SAVE_FIGURES) // cols)
    under = 18 + rows * 92 + 10 + 136 + foot
    pic = dump(TOUCH, 'save-load.png').crop((424, 117, 1227, 450))
    pic = pic.resize((x1 - x0, round((x1 - x0) * pic.height / pic.width)), Image.LANCZOS)
    cut = min(pic.height, y1 - y - under)
    pic = pic.crop((0, 0, pic.width, cut))
    d = gilt(img, pic, (x0, y))
    y += pic.height + 18
    cw = (x1 - x0) / cols
    for i, (label, value) in enumerate(SAVE_FIGURES):
        x, ry = x0 + 8 + (i % cols) * cw, y + (i // cols) * 92
        d.text((x, ry), label, font=CINZEL(24, 600), fill=SUBTITLE)
        d.text((x, ry + 32), value, font=fit(d, value, GOTHIC, 38, cw - 48), fill=TEXT)
    y += rows * 92 + 10
    d.text((x0 + 8, y), 'COMMENT', font=CINZEL(24, 600), fill=SUBTITLE)
    b = (x0, y + 36, x1 - 220, y + 136)
    d.rectangle(b, fill=(30, 22, 16), outline=DIM_GOLD, width=2)
    d.text((x0 + 24, y + 86), 'before the attack on Oaks', font=GOTHIC(38), fill=TEXT, anchor='lm')
    row_box(d, (x1 - 204, y + 36, x1, y + 136), 'Edit', size=40, face=BRUSH, centred=True)
    return cut, y + 136


def load_save_chips(d, x0, y, w=200, h=80, stacked=False):
    """Load | Save (from COMMAND): the chosen one red."""
    for i, name in enumerate(('Load', 'Save')):
        b = (x0, y + i * (h + 12), x0 + w, y + i * (h + 12) + h) if stacked else \
            (x0 + i * (w + 12), y, x0 + i * (w + 12) + w, y + h)
        on = name == 'Load'
        d.rectangle(b, fill=(112, 26, 20) if on else ROW, outline=GOLD if on else DIM_GOLD, width=3 if on else 2)
        d.text(((b[0] + b[2]) / 2, (b[1] + b[3]) / 2), name, font=CINZEL(32, 700), fill=NAME if on else QUIET,
               anchor='mm')


LL1_SPLIT = 1000                        # LL1: the list left of it, the save's page right of it


def ll1():
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    d.text((LX0 + 4, 14), 'Load', font=BRUSH(56), fill=NAME)
    load_save_chips(d, LL1_SPLIT - 412, 18)
    divider(d, 110, LX0, LL1_SPLIT)
    rows = land_save_list(d, (LX0, 124, LL1_SPLIT, FOOT - 10))
    px0 = LL1_SPLIT + 40
    foot = (FOOT - 10 - 150, FOOT - 10)
    cut, _ = land_save_page(img, (px0, 14, LX1, foot[0] - 16), foot=0)
    d = ImageDraw.Draw(img)
    land_back(d, (px0, foot[0], px0 + 360, foot[1]))
    red_button(d, (px0 + 376, foot[0], LX1, foot[1]), 'Load', 64)
    reach_land(img)
    save_land(img, 'LL1', 'the PC\'s split: the list left, the save\'s page right', 'land-saves', notes=[
        'The PC\'s screen across the whole\nwidth: the title, Load | Save and\nthe two lists at the left; the\n'
        'chosen save\'s page at the right,\nBack | Load at its foot (as on the\nPC), Load at the right thumb.',
        f'The list as upright\'s PL2: one-line\nrows ({mm(104)}), the comment only on\nthe page; {rows} of the twelve '
        'in\nview, the rest a drag away (the\nscroll bar).',
        f'The map at the page\'s width\n({mm(LX1 - px0)}), cut lower to {mm(cut)} for\nthe figures in three columns '
        'and\nthe comment; Delete at the save\'s\ntitle (the owner\'s B5).',
        'Twelve manual saves, as PL2:\nthe gallery\'s four (here 12-9)\nand eight older ones made up.'])


def ll2():
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    sx0, sx1 = RAIL_L
    d.text((sx0 + 4, 14), 'Load', font=BRUSH(56), fill=NAME)
    load_save_chips(d, sx0, 104, w=sx1 - sx0, h=84, stacked=True)
    d.line([(sx0 + 4, 316), (sx1 - 4, 316)], fill=DIM_GOLD, width=2)
    d.text((sx0 + 4, 336), 'SAVES', font=CINZEL(24, 600), fill=SUBTITLE)
    for i, (title, sub) in enumerate((('Manual', '12 saves'), ('Auto', '20 saves'))):
        b = (sx0, 380 + i * 142, sx1, 380 + i * 142 + 130)
        on = i == 0
        d.rectangle(b, fill=(70, 54, 30) if on else ROW, outline=GOLD if on else DIM_GOLD, width=3 if on else 2)
        d.text(((b[0] + b[2]) / 2, b[1] + 46), title, font=CINZEL(36, 700), fill=NAME if on else QUIET, anchor='mm')
        d.text(((b[0] + b[2]) / 2, b[3] - 36), sub, font=GOTHIC(28), fill=TEXT if on else QUIET, anchor='mm')
    land_back(d, (sx0, FOOT - 10 - 150, sx1, FOOT - 10))
    lx1 = FX0 + 12 + 720
    rows = land_save_list(d, (FX0 + 12, 16, lx1, FOOT - 10), tabs=False)
    cut, _ = land_save_page(img, (lx1 + 32, 14, FX1 - 12, FOOT - 10), cols=2, delete=False)
    d = ImageDraw.Draw(img)
    sx0, sx1 = RAIL_R
    bin_button(d, (sx0, 16, sx1, 216), stacked=True)
    red_button(d, (sx0, FOOT - 10 - 260, sx1, FOOT - 10), 'Load', 56)
    reach_land(img)
    save_land(img, 'LL2', 'the frame holds the list and the page, the sides the rest', 'land-saves', notes=[
        'LM1\'s principle: the frame holds\nthe list and the chosen save\'s\npage; the black sides the title,\n'
        'the switches and the buttons.',
        'Left side: the title, Load | Save\n(from COMMAND) stacked, the two\nlists as two cells (Manual 12,\n'
        'Auto 20), Back at the left thumb.',
        f'The list in one-line rows ({mm(104)}):\n{rows} of the twelve in view, the rest\na drag away. The page: the map at\n'
        f'its whole height ({mm(cut)}), the\nfigures in two columns, the\ncomment and Edit.',
        'Right side: Delete (the danger\nbutton, its bin) at the top,\nbeside the save\'s title and out\n'
        f'of the thumbs\' way; Load at the\nright thumb ({mm(260)} high).',
        'Twelve manual saves, as LL1.'])


def p11():
    lh1(); lh2()
    lt1(); lt2()
    ll1(); ll2()


# --- round P12: the options window and the dungeon's menus, landscape (2026-10-28) ---------------------------------

def messages_columns(d, box):
    """The Messages tab in two columns: the text speed and the read text's colours left, the message skip right."""
    x0, y0, x1, y1 = box
    mid = round((x0 + x1) / 2)
    y = text_speed(d, x0 + 40, y0 + 36, mid - 30)
    read_text(d, x0 + 40, y, mid - 30)
    message_skip(d, mid + 30, y0 + 36, x1 - 40)
    d.line([(mid, y0 + 40), (mid, y1 - 40)], fill=PAPER_LINE, width=2)
    return mid


def paper_button(d, box, label):
    d.rectangle(box, fill=PAPER_DARK, outline=INK_QUIET, width=2)
    d.text(((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), label, font=CINZEL(36, 700), fill=INK_HEAD, anchor='mm')


def lo1():
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    h, w = 110, 330
    for i, name in enumerate(OPTION_TABS):          # the paper tabs on top, as on the PC and PO1
        x = LX0 + i * (w + 10)
        on = i == 0
        d.rectangle((x, 16 + (0 if on else 12), x + w, 16 + h), fill=PAPER if on else PAPER_DARK)
        if on:
            d.rectangle((x, 16, x + w, 25), fill=VERMILION)
        d.text((x + w / 2, 16 + h / 2 + 6), name, font=fit(d, name.upper(), lambda s: CINZEL(s, 700), 34, w - 20),
               fill=INK_HEAD if on else INK_QUIET, anchor='mm')
    d.text((LX1 - 4, 20), 'Options', font=BRUSH(62), fill=NAME, anchor='ra')
    sheet = (LX0, 16 + h, LX1, FOOT - 10)
    d.rectangle(sheet, fill=PAPER)
    mid = messages_columns(d, sheet)
    foot = (FOOT - 10 - 30 - 130, FOOT - 10 - 30)
    paper_button(d, (mid + 30, foot[0], mid + 30 + 380, foot[1]), 'Defaults')
    red_button(d, (mid + 30 + 400, foot[0], LX1 - 40, foot[1]), 'Close', 56)
    reach_land(img)
    save_land(img, 'LO1', 'the paper sheet across the width, its tabs on top', 'land-options', notes=[
        'The owner\'s PO1 laid across: the\npaper sheet over the whole width,\nits five tabs on top as on the PC\n'
        f'({mm(h)}), the title at their right.',
        'The tab in two columns: the text\nspeed (its preview the game\'s\nwindow at 2.7 mm) and the read\n'
        'text\'s colours left, the message\nskip right — the whole tab in\nview, no scrolling.',
        'The type as PO1\'s: the names\n2.3 mm, the explanations 1.7 mm,\nthe boxes 2.6 mm (the row\'s '
        f'whole\nheight taps). Defaults | Close at\nthe right thumb ({mm(130)}).',
        'The other tabs split the same\nway (Tweaks: Battle left, Scenes\nright).'])


def lo2():
    img, d = land()
    backdrop(img, (0, 0, LW, LH))
    sx0, sx1 = RAIL_L
    d.text((sx0 + 4, 14), 'Options', font=fit(d, 'Options', BRUSH, 56, sx1 - sx0 - 8), fill=NAME)
    h, gap = 128, 12
    for i, name in enumerate(OPTION_TABS):          # the tabs as a rail of lacquer cells in the left side
        b = (sx0, 110 + i * (h + gap), sx1, 110 + i * (h + gap) + h)
        on = i == 0
        d.rectangle(b, fill=(70, 54, 30) if on else ROW, outline=GOLD if on else DIM_GOLD, width=3 if on else 2)
        d.text(((b[0] + b[2]) / 2, (b[1] + b[3]) / 2), name,
               font=fit(d, name.upper(), lambda s: CINZEL(s, 700), 32, sx1 - sx0 - 24), fill=NAME if on else QUIET,
               anchor='mm')
    panel(d, (sx0, FOOT - 10 - 130, sx1, FOOT - 10))
    d.text(((sx0 + sx1) / 2, FOOT - 10 - 65), 'Defaults', font=CINZEL(34, 700), fill=QUIET, anchor='mm')
    sheet = (FX0 + 12, 16, FX1 - 12, FOOT - 10)
    d.rectangle(sheet, fill=PAPER)
    messages_columns(d, sheet)
    red_button(d, (RAIL_R[0], FOOT - 10 - 200, RAIL_R[1], FOOT - 10), 'Close', 56)
    reach_land(img)
    save_land(img, 'LO2', 'the sheet in the frame, the tabs in the left side', 'land-options', notes=[
        'LM1\'s principle: the paper sheet\nfills the frame, the black sides\nhold the title, the tabs and the\n'
        'buttons.',
        f'The five tabs as a rail of lacquer\ncells in the left side ({mm(h)} each),\nthe chosen one lit; Defaults at '
        'the\nleft thumb, Close at the right one.',
        'The sheet\'s two columns as LO1\'s,\neach narrower; the tabs nearer\nthe thumb, farther from the PC\'s\n'
        'and upright\'s place.'])


def lf_frame(img, entry):
    """The floor's picture and the script's window in the frame as the touch dumps have them; the tablet's rail
    taken out — black under it on a floor, the map wait's map (the same camera) before a search."""
    frame_dump(img, 'dungeon-entry.png' if entry else 'floor-ladder-middle.png')
    x0, y0 = 1036, 574 if entry else 420
    if entry:
        frame_dump(img, 'map.png', (x0, y0, 1280, 800), (FX0 + round(x0 * LK), round(y0 * LK)))
    else:
        ImageDraw.Draw(img).rectangle((FX0 + round(x0 * LK), round(y0 * LK), FX1, LH), fill=(0, 0, 0))
    d = ImageDraw.Draw(img)
    x0, x1 = RAIL_L
    menu_cell(d, (x0, 16, x1, 126))
    side_status(d, x0, x1, 160)
    return d


def side_rung(d, box, words, sub, way):
    """A ladder rung in a black side (254 wide): the arrow at the left, the word beside it — a name too long for the
    side in two lines —, the sub-line under them."""
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=ROW, outline=DIM_GOLD, width=3)
    lines = words.split() if d.textlength(words, font=GOTHIC(42)) > x1 - x0 - 80 else [words]
    arrow_mark(d, x0 + 34, y0 + 22 + 24 * len(lines), way, 16, GOLD)
    for i, word in enumerate(lines):
        d.text((x0 + 64, y0 + 20 + i * 50), word, font=GOTHIC(42), fill=TEXT)
    d.text((x0 + 64, y1 - 20), sub, font=fit(d, sub, GOTHIC, 26, x1 - x0 - 76), fill=QUIET, anchor='ls')


def side_ladder(d, x0, x1, foot, entry=False):
    """PF1's rail in the right side: Retreat (or Return) on top, Descend (or Start Search) in the corner."""
    h, gap = 150, 14
    if entry:
        side_rung(d, (x0, foot - 200, x1, foot), 'Start Search', 'a unit goes down', 'down')
        y = foot - 200 - gap - h
        row_box(d, (x0, y, x1, y + h), 'Return', size=44, centred=True)
        return y
    side_rung(d, (x0, foot - h, x1, foot), 'Descend', 'deeper · to 8F', 'down')
    y = foot - h - gap - 110
    d.text(((x0 + x1) / 2, y + 26), 'FLOOR', font=CINZEL(24, 600), fill=SUBTITLE, anchor='mm')
    d.text(((x0 + x1) / 2, y + 74), '7F', font=BRUSH(56), fill=NAME, anchor='mm')
    y -= h + gap
    side_rung(d, (x0, y, x1, y + h), 'Ascend', 'back · to 6F', 'up')
    y -= h + gap
    red_button(d, (x0, y, x1, y + h), 'Retreat', 44)
    return y


def lf1():
    img, d = land()
    d = lf_frame(img, False)
    side_ladder(d, *RAIL_R, FOOT - 10)
    reach_land(img)
    save_land(img, 'LF1', 'the frame kept, the ladder in the right side', 'land-dungeon', notes=[
        'The frame as on the PC: the\nfloor\'s picture and the script\'s\nwindow at their places (the\n'
        f'window\'s text {lm(48)}); the tablet\'s\nrail leaves the frame.',
        'The ladder in the right side at\nthe thumb, from the foot up as\nthe tablet\'s (touch.md\'s B):\n'
        'Descend in the corner, the\nfloor, Ascend, Retreat on top;\n'
        f'rungs {mm(150)} × {mm(RAIL_R[1] - RAIL_R[0])}. A way not\noffered keeps its room, hidden.',
        'Where LM1 has Commands and a\nprovince\'s commands. The left\nside as LM1c\'s while a menu is\n'
        'up: Menu and the status, the\ndrawer hidden.'])


def lf1e():
    img, d = land()
    d = lf_frame(img, True)
    side_ladder(d, *RAIL_R, FOOT - 10, entry=True)
    reach_land(img)
    save_land(img, 'LF1e', 'the frame kept — before a search', 'land-dungeon', notes=[
        'Before a search: Return over\nStart Search in Descend\'s\ncorner (touch.md\'s §6), its\n'
        f'two words in two lines ({mm(200)}\nhigh).',
        'With four items (Merim, God\'s\nDoor) the rail grows upwards.',
        'The map under the picture as the\ngallery\'s dump has it.'])


def lf2_rail(d, foot, entry=False):
    """The tablet's rail where the tablet has it, at the frame's right foot, at the phone's sizes; its panel grows
    upwards."""
    x1 = FX1 - 16
    x0 = x1 - 356
    top = foot - (314 if entry else 602)
    panel(d, (x0 - 14, top - 14, x1 + 14, foot + 14))
    floor_rail(d, x0, x1, foot, entry)
    return x1 - x0


def lf2():
    img, d = land()
    d = lf_frame(img, False)
    w = lf2_rail(d, FOOT - 10)
    reach_land(img)
    save_land(img, 'LF2', 'the tablet\'s layout kept, the ladder in the frame', 'land-dungeon', notes=[
        'The tablet\'s layout kept: the\nladder in the frame where the\ntablet has it, at the phone\'s\n'
        f'sizes — rungs {mm(150)} × {mm(w)}, the\nwords 2.6 mm in one line, the\nsub-lines 1.6 mm.',
        'Its panel grows upwards over\nthe black, beside the window.',
        'The right side stays empty; the\nleft as LF1\'s (Menu and the\nstatus).'])


def lf2e():
    img, d = land()
    d = lf_frame(img, True)
    lf2_rail(d, FOOT - 10, entry=True)
    reach_land(img)
    save_land(img, 'LF2e', 'the tablet\'s layout kept — before a search', 'land-dungeon', notes=[
        'Before a search: Return over\nStart Search, both in one line,\nat the frame\'s right foot as on\n'
        'the tablet.',
        'The rail\'s panel over the map\'s\ncorner, as the tablet\'s.'])


def p12():
    lo1(); lo2()
    lf1(); lf1e(); lf2(); lf2e()


# --- round P13: the battle's other states on LB2, landscape (2026-10-28) ---------------------------------------------

LB2_SCENE, LB2_CARD = 438, 334                  # LB2: the scene's own picture 70% high, the cards under it
SHARP_W = round(1024 * LB2_SCENE / 410)         # that picture's width; its sides blurred out to the frame
SHARP_X0 = (LW - SHARP_W) // 2
CX = (FX0 + FX1) // 2                           # the frame's middle: the gauge
OSWALD = lambda size, w=600: font('Oswald-VariableFont_wght.ttf', size, w)
BALLOON_INK = (150, 140, 120)

# The opening's figures (the gallery's opening): no one acting yet, both our cards with their order toggles.
OPENING_UNITS = dict(UNITS, Rance=('1', 'Leazas Knights', 606, 1000, (8, 8), 'toggles', '10 6 0 13'),
                     Rick=('2', 'Leazas Knights', 800, 800, (8, 8), 'toggles', '10 6 0 20'),
                     Ex=('1', 'Leazas Knights', 122, 1000, (1, 4), '', '10 6 0 1'),
                     Sakanaku=('2', 'Leazas Soldiers', 177, 1000, (6, 6), '', '8 4 0 2'),
                     Melfeis=('3', 'Leazas Mages', 300, 300, (4, 4), '', '3 3 7 9'))
# A fighter's line (the gallery's battle-speech): Rance on Sakanaku, both by Melee.
SPEECH_UNITS = dict(UNITS, Rance=('1', 'Leazas Knights', 451, 1000, (8, 8), 'pill', '10 6 0 13'),
                    Sakanaku=('2', 'Leazas Soldiers', 60, 1000, (4, 6), 'pill', '8 4 0 2'))
# The results (the gallery's info-results): night fell with Dodge alone left, renumbered 1; Rick acting.
RESULTS_UNITS = dict(UNITS, Rance=('1', 'Leazas Knights', 266, 1000, (8, 8), 'Acted', '10 6 0 13'),
                     Rick=('2', 'Leazas Knights', 583, 800, (8, 8), 'Acting', '10 6 0 20'),
                     Dodge=('1', 'Leazas Soldiers', 294, 1000, (6, 6), 'Acted', '8 4 0 2'))
RESULTS_CARDS = {'player': [[None, 'Rance'], [None, 'Rick']], 'enemy': [['Dodge', None], [None, None]]}
NO_CARDS = {'player': [[None, None], [None, None]], 'enemy': [[None, None], [None, None]]}
RETREAT_BOX = (RAIL_L[0] + 8, FOOT - 10 - 5 * 123 - 4 * 12, RAIL_L[1] - 8, FOOT - 10 - 4 * 123 - 4 * 12)


def scene_band(name, patches=()):
    """A dump's 1280×410 scene with `patches` (boxes) taken from battle-turn's, the same field and armies: what lies
    under the exchange's counters, a portrait or a balloon there."""
    band = dump(TOUCH, name).crop((0, 0, 1280, 410))
    turn = dump(TOUCH, 'battle-turn.png')
    for box in patches:
        band.paste(turn.crop(box), box[:2])
    return band


CLEAN_EXCHANGE = ((140, 290, 330, 410), (950, 290, 1152, 410))     # the exchange's counters' corners


def lb2_base(img, scene=None, units=UNITS, cards=LAND_CARDS, acting='Rance', target='Ex', fighters=(), turns='4',
             rails=True):
    """LB2 as chosen: the scene 70% high, the board under it; the rails in the black sides, or only the battle's Skip
    on the left one's top while they are gone."""
    top = land_scene(img, LB2_SCENE, scene)
    land_board(img, top, LB2_CARD, 40, 160, 80, units, cards, acting, target, fighters, turns)
    d = ImageDraw.Draw(img)
    if rails:
        land_rails(img, d)
    else:
        land_skip(d)
    return ImageDraw.Draw(img)


def left_balloon(d, box, lines, size, tail_y):
    """Our balloon with its tail pointing left at the speaker."""
    balloon(d, box, lines, size, triangle_on=False)
    x0 = box[0]
    d.polygon([(x0 + 3, tail_y - 26), (x0 + 3, tail_y + 26), (x0 - 40, tail_y)], fill=(246, 244, 238))
    d.line([(x0 + 1, tail_y - 26), (x0 - 40, tail_y), (x0 + 1, tail_y + 26)], fill=BALLOON_INK, width=3)


def retreat_lit(img):
    """While the question is up: Retreat lit, the left rail's other rows and the target's rail dimmed."""
    veil(img, (RAIL_L[0] - 6, RETREAT_BOX[3] + 6, RAIL_L[1] + 6, LH), 0.6)
    veil(img, (RAIL_R[0] - 6, 0, RAIL_R[1] + 6, LH), 0.6)
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = RETREAT_BOX
    d.rectangle((x0 - 5, y0 - 5, x1 + 5, y1 + 5), outline=HOVER_LINE, width=5)
    return d


def lb2r1():
    img, d = land()
    lb2_base(img)
    d = retreat_lit(img)
    cy = (RETREAT_BOX[1] + RETREAT_BOX[3]) // 2
    w, h = 660, 350
    x0, y0, x1, y1 = box = (FX0 + 44, cy - h // 2, FX0 + 44 + w, cy + h // 2)
    panel(d, box, fill=(30, 22, 16))
    d.polygon([(x0 + 2, cy - 30), (x0 - 46, cy), (x0 + 2, cy + 30)], fill=GOLD)
    d.text((x0 + 40, y0 + 34), 'Retreat the whole army?', font=GOTHIC(44), fill=TEXT)
    d.text((x0 + 40, y0 + 100), 'The battle is lost.', font=GOTHIC(38), fill=QUIET)
    d.line([(x0 + 30, y0 + 166), (x1 - 30, y0 + 166)], fill=DIM_GOLD, width=2)
    bw, by = (w - 80 - 16) // 2, y0 + 186
    row_box(d, (x0 + 40, by, x0 + 40 + bw, by + 138), 'Cease Retreat', size=40, centred=True)
    red_button(d, (x0 + 56 + bw, by, x1 - 40, by + 138), 'Retreat', 50)
    reach_land(img)
    save_land(img, 'LB2r1', 'the retreat question in the frame, beside Retreat', 'land-battle-turn', notes=[
        'The question at Retreat, in the\nframe beside the button (the\ntablet\'s A2, the house\'s callout\n'
        f'at the click, {mm(w)} wide): over our\nback row while it asks.',
        f'The question {mm(44)}, the answers\n{mm(138)} high in one row, as on the\nPC and upright (PB3r): Cease\n'
        'Retreat nearer the button,\nRetreat red and last.',
        'The rails\' other rows dimmed\nwhile it asks; a tap off it is\nCease Retreat (No).'])


def lb2r2():
    img, d = land()
    lb2_base(img)
    veil(img, (RAIL_R[0] - 6, 0, RAIL_R[1] + 6, LH), 0.6)
    d = ImageDraw.Draw(img)
    x0, x1 = RETREAT_BOX[0] - 2, RETREAT_BOX[2] + 2
    panel(d, (x0, RETREAT_BOX[1] - 20, x1, FOOT - 2), fill=(30, 22, 16))
    d.text(((x0 + x1) / 2, RETREAT_BOX[1] + 22), 'RETREAT', font=CINZEL(30, 700), fill=VERMILION_LINE, anchor='mm')
    d.line([(x0 + 24, RETREAT_BOX[1] + 52), (x1 - 24, RETREAT_BOX[1] + 52)], fill=DIM_GOLD, width=2)
    q = fit(d, 'whole army?', GOTHIC, 44, x1 - x0 - 40)
    y = RETREAT_BOX[1] + 76
    for line in ('Retreat the', 'whole army?'):
        d.text((x0 + 22, y), line, font=q, fill=TEXT)
        y += q.size + 12
    for line in ('The battle', 'is lost.'):
        d.text((x0 + 22, y + 14), line, font=GOTHIC(34), fill=QUIET)
        y += 44
    h = 150
    retreat = (x0 + 10, FOOT - 12 - h, x1 - 10, FOOT - 12)
    cease = (x0 + 10, retreat[1] - 16 - h, x1 - 10, retreat[1] - 16)
    d.rectangle(cease, fill=ROW, outline=DIM_GOLD, width=3)
    for i, word in enumerate(('Cease', 'Retreat')):
        d.text(((cease[0] + cease[2]) / 2, (cease[1] + cease[3]) / 2 + (i - 0.5) * 50), word, font=GOTHIC(40),
               fill=TEXT, anchor='mm')
    red_button(d, retreat, 'Retreat', 46)
    reach_land(img)
    save_land(img, 'LB2r2', 'the retreat question over the left rail', 'land-battle-turn', notes=[
        'The question over the left rail,\nwhere its commands were: the\nboard stays whole in view.',
        f'Its words wrapped in the rail\'s\n{mm(254)} ({mm(q.size)}); Cease Retreat\nnearer the button, Retreat red\n'
        f'and last, at the thumb ({mm(h)}\nhigh each, Cease in two lines).',
        'The target\'s rail dimmed; a tap\noff it is Cease Retreat (No).'])


def exchange_counter(d, troops, hp, right=False):
    """The exchange's counters out of the picture, in the scene's blurred side beside their army: TROOPS over the
    figure, HP beside it (ours after the figure, the enemy's before it, as on the PC)."""
    big, small, label = OSWALD(124, 600), OSWALD(70, 600), CINZEL(26, 700)
    base = LB2_SCENE - 40
    tw = d.textlength(troops, font=big)
    hpw = d.textlength('HP', font=label) + 8 + d.textlength(hp, font=small)
    w = tw + 22 + hpw
    x0 = SHARP_X0 + SHARP_W + 30 if right else SHARP_X0 - 30 - w
    fx, hx = (x0 + hpw + 22, x0) if right else (x0, x0 + tw + 22)
    d.text((fx + tw if right else fx, base - 118), 'TROOPS', font=label, fill=GOLD, anchor='rs' if right else 'ls')
    d.line([(fx, base - 108), (fx + tw, base - 108)], fill=GOLD, width=3)
    d.text((fx, base), troops, font=big, fill=(250, 232, 170), anchor='ls', stroke_width=5, stroke_fill=(40, 26, 10))
    green = (150, 214, 120)
    d.text((hx, base - 6), 'HP', font=label, fill=green, anchor='ls', stroke_width=3, stroke_fill=(20, 30, 14))
    d.text((hx + d.textlength('HP', font=label) + 8, base), hp, font=small, fill=green, anchor='ls', stroke_width=4,
           stroke_fill=(20, 30, 14))
    box = d.textbbox((fx, base), troops, font=big, anchor='ls')
    return box[3] - box[1]


def lb2x():
    img, d = land()
    d = lb2_base(img, scene_band('exchange-counters.png', CLEAN_EXCHANGE), EXCHANGE, acting='Ex', target=None,
                 fighters=('Rance', 'Ex'), rails=False)
    fh = exchange_counter(d, '560', '8')
    exchange_counter(d, '92', '1', right=True)
    reach_land(img)
    save_land(img, 'LB2x', 'an exchange', 'land-battle-turn', notes=[
        'An exchange (the gallery\'s: Ex\n(commander 1) on Rance (unit 1),\nboth by Melee): the fighters\'\n'
        'cards lit with their action\'s\npill, the others dimmed.',
        f'The counters out of the picture\ninto its blurred sides, beside\ntheir army: the figures {mm(fh)}\n'
        f'high (in the picture at its 70%\nthey would be {mm(48)}).',
        'The rails gone while it plays,\nas the tablet\'s; the battle\'s\nSkip stays on the left rail\'s\ntop.'])


def opening_tint(img, box, split, alpha, blur=0):
    """The opening's colours over `box`: blue left of the slanted split (x at the box's top, x at its foot), red right
    of it, a white line on the split."""
    x0, y0, x1, y1 = box
    part = img.crop(box)
    if blur:
        part = part.filter(ImageFilter.GaussianBlur(blur))
    blue = Image.blend(part, Image.new('RGB', part.size, (44, 84, 206)), alpha)
    red = Image.blend(part, Image.new('RGB', part.size, (198, 56, 42)), alpha)
    mask = Image.new('L', part.size, 0)
    ImageDraw.Draw(mask).polygon([(0, 0), (split[0] - x0, 0), (split[1] - x0, y1 - y0), (0, y1 - y0)], fill=255)
    img.paste(Image.composite(blue, red, mask), (x0, y0))
    d = ImageDraw.Draw(img)
    d.line([(split[0], y0), (split[1], y1)], fill=(246, 244, 238), width=6)
    return d


def opening_plate(d, y0, w=940):
    box = (CX - w // 2, y0, CX + w // 2, y0 + 196)
    panel(d, box)
    d.text((CX, y0 + 58), 'Oaks — Offensive Battle', font=CINZEL(50, 700), fill=TEXT, anchor='mm')
    d.text((CX, y0 + 122), 'City · Battle limit: up to 232', font=CINZEL(38, 700), fill=TEXT, anchor='mm')
    d.text((CX, y0 + 166), '4 turns', font=CINZEL(36, 600), fill=QUIET, anchor='mm')
    return box


def army_name(d, lines, cx, y, size, width, gap=0):
    """An army's name in the opening's brush, white on a dark rim, a line under the other."""
    f = BRUSH(min(fit(d, s, BRUSH, size, width).size for s in lines))
    for line in lines:
        d.text((cx, y), line, font=f, fill=(246, 238, 220), anchor='mm', stroke_width=6, stroke_fill=(24, 18, 30))
        y += f.size + gap
    return f.size, y


def lb3o1():
    img, d = land()
    clean = scene_band('exchange-counters.png', CLEAN_EXCHANGE)
    lb2_base(img, clean, OPENING_UNITS, acting=None, target=None, rails=False)
    d = opening_tint(img, (FX0, 0, FX1, LB2_SCENE), (CX + 60, CX - 60), 0.5, blur=2)
    plate = opening_plate(d, 18)
    side = plate[0] - FX0 - 60
    size, _ = army_name(d, ('Rance', 'Army'), (FX0 + plate[0]) // 2, 170, 104, side, 6)
    army_name(d, ('Leazas', 'Rebel', 'Army'), (plate[2] + FX1) // 2, 82, 92, side, 0)
    d.text(((plate[2] + FX1) // 2, 384), 'Walls +8%', font=BRUSH(46), fill=(240, 200, 110), anchor='mm',
           stroke_width=4, stroke_fill=(40, 10, 10))
    d.text((CX, 324), 'VS', font=BRUSH(120), fill=GOLD, anchor='mm', stroke_width=5, stroke_fill=(40, 26, 10))
    reach_land(img)
    save_land(img, 'LB3o1', 'the opening at the scene\'s place', 'land-battle-states', notes=[
        'The opening at the scene\'s place,\nas on the PC and the tablet, laid\nout again at the frame\'s width:\n'
        f'the armies\' names {mm(size)}, the place\n{mm(50)}, the limit {mm(38)}, the turns {mm(36)}.',
        'The board under it as LB2\'s (the\nopening\'s figures), the rails not\nup yet, the battle\'s Skip on the\n'
        'left rail\'s top; a tap goes on.',
        'Upright the same choice is still\nopen (PB4o1 or PB4o2).'])


def lb3o2():
    img, d = land()
    clean = scene_band('exchange-counters.png', CLEAN_EXCHANGE)
    lb2_base(img, clean, OPENING_UNITS, cards=NO_CARDS, acting=None, target=None, rails=False)
    d = opening_tint(img, (FX0, 0, FX1, LH), (CX + 110, CX - 110), 0.74, blur=3)
    opening_plate(d, 40)
    half = CX - FX0 - 80
    size, _ = army_name(d, ('Rance', 'Army'), (FX0 + CX) // 2, 600, 150, half, 10)
    _, y = army_name(d, ('Leazas', 'Rebel', 'Army'), (CX + FX1) // 2, 470, 130, half, 0)
    d.text(((CX + FX1) // 2, y + 10), 'Walls +8%', font=BRUSH(64), fill=(240, 200, 110), anchor='mm',
           stroke_width=4, stroke_fill=(40, 10, 10))
    d.text((CX, 820), 'VS', font=BRUSH(150), fill=GOLD, anchor='mm', stroke_width=6, stroke_fill=(40, 26, 10))
    reach_land(img)
    save_land(img, 'LB3o2', 'the opening over the scene and the board', 'land-battle-states', notes=[
        'The opening laid over the scene\nand the board as LB2 stands:\nours blue at the left, the enemy\n'
        'red at the right, VS at the\ngauge\'s place.',
        f'The names {mm(size)}; the plate over\nthe scene (the place {mm(50)}, the\nlimit {mm(38)}); the cards fly in '
        'on\nthe board under it as it fades.',
        'Upright the same choice (PB4o2);\nthere it is still open.'])


def lb3s():
    img, d = land()
    d.rectangle((0, 0, LW, LH), fill=(8, 8, 10))
    sr = dump(TOUCH, 'scout-report.png')
    kanami = sr.crop((40, 40, 340, 440)).resize((345, 460), Image.LANCZOS)
    framed(img, kanami, (LX0 + 14, 30))
    d = ImageDraw.Draw(img)
    bx0 = LX0 + 14 + 345 + 60
    left_balloon(d, (bx0, 40, LX1 - 10, 246), ['The Approaching Enemy   [ Leazas Rebel Army ]',
                                              'Commanders 4        Total troops 1,556'], 54, 110)
    cw, ch, gap = 700, 270, 40
    cx0 = bx0 + (LX1 - 10 - bx0 - 2 * cw - gap) // 2
    for c, (label, places) in enumerate((('FRONT ROW', (1, 2)), ('BACK ROW', (3, 4)))):
        x = cx0 + c * (cw + gap)
        d.text((x + cw / 2, 300), label, font=CINZEL(30, 700), fill=SUBTITLE, anchor='mm')
        for r, place in enumerate(places):
            attacker_card(img, (x, 330 + r * (ch + 20), x + cw, 330 + r * (ch + 20) + ch), place)
        d = ImageDraw.Draw(img)
    rance = sr.crop((40, 574, 194, 774)).resize((246, 320), Image.LANCZOS)
    framed(img, rance, (LX0 + 14, FOOT - 10 - 320))
    d = ImageDraw.Draw(img)
    left_balloon(d, (LX0 + 14 + 246 + 60, FOOT - 210, 1500, FOOT - 50), ['Mhm... I see.'], 60, FOOT - 150)
    reach_land(img)
    save_land(img, 'LB3s', 'the ninja\'s scout report', 'land-battle-states', notes=[
        'The scout report before the field\nacross the whole width, as on the\nPC: Kanami and her report on top,\n'
        'the rebels\' cards under it (the\nfront row left), Rance\'s answer\nat the foot.',
        f'The report {mm(54)}, Rance\'s line\n{mm(60)}; the cards as the enemy\nattack\'s (LA2): the names {mm(42)},\n'
        f'the faces {mm(200)}.',
        'One tap goes on (the report\'s\nwindows collapsed into one, as\non the PC); with no button the\n'
        'sides hold nothing of their own.'])


def lb3c():
    img, d = land()
    frame_dump(img, 'strategy.png')
    d = ImageDraw.Draw(img)
    rule = (222, 186, 104)

    def y_at(px, y_left):                       # the band's slant: up 120 layout px across the PC's screen
        return (y_left - 120 / 1280 * (px - FX0) / LK) * LK

    top, foot = 435.5, 557.5                    # its gold rules at the PC's left edge
    d.polygon([(0, y_at(0, top)), (FX0, y_at(FX0, top)), (FX0, y_at(FX0, foot)), (0, y_at(0, foot))],
              fill=(63, 107, 201))
    for y in (top, foot):                       # the blue runs on at the left; at the right it has faded to black
        d.line([(0, y_at(0, y)), (FX0, y_at(FX0, y))], fill=rule, width=3)
        d.line([(FX1, y_at(FX1, y)), (LW, y_at(LW, y))], fill=rule, width=3)
    reach_land(img)
    save_land(img, 'LB3c', 'the strategy\'s cut-in', 'land-battle-states', notes=[
        'The PC\'s cut-in fitted to the\nframe as it is: every line already\n2 mm or more — the caption '
        f'{lm(26)},\n"Success" {lm(64)}, the strategist\'s\nline {lm(30)}, what it does {lm(38)}.',
        'The band runs on across the\nblack sides to the screen\'s edges,\nas on the PC.',
        'Barres (unit 6), his line the\ngallery\'s; the enemy\'s cut-in the\nsame in red. One tap goes on.'])


def lb3r():
    img, d = land()
    lb2_base(img, None, RESULTS_UNITS, RESULTS_CARDS, acting='Rick', target=None, turns='0', rails=False)
    veil(img, (FX0, 0, FX1, LH), 0.55)
    d = ImageDraw.Draw(img)
    w, h = 1400, 720
    x0, y0, x1, y1 = plate = (CX - w // 2, (LH - h) // 2 - 20, CX + w // 2, (LH - h) // 2 - 20 + h)
    panel(d, plate)
    d.text((CX, y0 + 100), 'Night Falls', font=BRUSH(100), fill=NAME, anchor='mm')
    d.text((CX, y0 + 196), 'THE BATTLE DRAGGED ON AND IT BECAME NIGHT.', font=CINZEL(36, 600), fill=SUBTITLE,
           anchor='mm')
    divider(d, y0 + 250, x0 + 60, x1 - 60)
    for cx, army, value in ((CX - w // 4, 'LEAZAS', '557'), (CX + w // 4, 'LEAZAS REB', '1,229')):
        d.text((cx, y0 + 318), army, font=CINZEL(40, 600), fill=QUIET, anchor='mm')
        d.text((cx, y0 + 426), value, font=BRUSH(120), fill=(222, 110, 90), anchor='mm')
        d.text((cx, y0 + 520), 'DAMAGES', font=CINZEL(30, 600), fill=SUBTITLE, anchor='mm')
    d.line([(CX, y0 + 290), (CX, y0 + 540)], fill=DIM_GOLD, width=2)
    d.text((CX, y0 + 628), '2 EXP OBTAINED', font=CINZEL(50, 700), fill=TEXT, anchor='mm')
    triangle(d, x1 - 64, y1 - 54, 24)
    reach_land(img)
    save_land(img, 'LB3r', 'the results plate', 'land-battle-states', notes=[
        'The results plate in the frame\'s\nmiddle over the dimmed scene and\nboard, larger than the PC\'s: the\n'
        f'title {mm(100)}, the damages {mm(120)},\nthe line under the title {mm(36)}.',
        'A tap anywhere goes on; a\nlevel-up\'s page after it on the\nsame plate, a fall\'s box and our\n'
        'narration panel the same way.',
        'The board as the gallery\'s\ninfo-results has it (night fell\nwith Dodge alone left); the rails\n'
        'not up, Skip on the left rail\'s\ntop.'])


def lb3b():
    img, d = land()
    band = scene_band('battle-speech.png', ((128, 0, 330, 410), (330, 330, 920, 410)))  # its portrait, its balloon
    d = lb2_base(img, band, SPEECH_UNITS, acting='Rance', target=None, fighters=('Rance', 'Sakanaku'), rails=False)
    portrait = dump(TOUCH, 'battle-speech.png').crop((10, 5, 305, 405)).resize((316, 428), Image.LANCZOS)
    img.paste(portrait, (SHARP_X0 - 316 - 24, 5))
    d = ImageDraw.Draw(img)
    left_balloon(d, (SHARP_X0 + 50, 296, SHARP_X0 + 1050, 486), ["Let's go!"], 62, 380)
    reach_land(img)
    save_land(img, 'LB3b', 'a fighter\'s line', 'land-battle-states', notes=[
        'The speaker\'s portrait in the\nscene\'s blurred left side (on the\nPC it covers the picture\'s left\n'
        'edge): every sprite stays seen;\nan enemy\'s in the right side.',
        f'Our balloon over the scene\'s\nfoot, larger than the PC\'s\n({mm(62)}), rewrapped as ever, two\n'
        'lines at most; one tap goes on.',
        'A defence\'s ribbon and caption\nthe same way: the caption over\nthe scene\'s foot at 2.4 mm. The\n'
        'rails gone, Skip on the left\nrail\'s top.'])


def p13():
    lb2r1(); lb2r2(); lb2x()
    lb3o1(); lb3o2(); lb3s(); lb3c(); lb3r(); lb3b()


# --- round P15: the scenes, landscape (2026-10-28) -------------------------------------------------------------------

def scene_side(d, drawer=True):
    """The scenes' left side as LW1's: Menu on top, Skip and Log at the left thumb (hidden under a menu)."""
    x0, x1 = RAIL_L
    menu_cell(d, (x0, 16, x1, 126))
    if drawer:
        drawer_cells(d, x0, x1, FOOT - 10)


def ls1():
    img, d = land()
    img.paste(look(LOOK_INTERCEPT).resize((1952, LH), Image.LANCZOS), (FX0, 0))
    d = ImageDraw.Draw(img)
    scene_side(d)
    reach_land(img)
    save_land(img, 'LS1', 'the frame as on the PC, the left side as LW1\'s', 'land-scenes', notes=[
        'LM1\'s principle: the scene as on\nthe PC and the tablet in the\nframe, our windows over the\n'
        f'picture at their places — the\nscript\'s text {lm(48)}, larger than\nupright\'s PS2 (3.6 mm).',
        'The left side as LW1\'s: Menu on\ntop, Skip and Log at the left\nthumb; the right side empty. A\n'
        'tap anywhere goes on.',
        'Speech, thought, narration and a\nplace\'s title at their own places\nas on the PC; no name plate but\n'
        '131\'s.'])


def ls1b():
    img, d = land()
    cg = Image.new('RGB', (1952, LH), (58, 58, 62))
    cd = ImageDraw.Draw(cg)
    for x in range(-LH, 1952, 40):
        cd.line([(x, LH), (x + LH, 0)], fill=(72, 72, 76), width=3)
    cd.text((976, 420), 'the scene\'s CG (masked)', font=GOTHIC(56), fill=(200, 200, 200), anchor='mm')
    img.paste(cg, (FX0, 0))
    bar = dump(LACQ, 'message-cg-bar-speech.png')
    win, plate = bar.crop((0, 588, 1280, 800)), bar.crop((30, 560, 196, 588))
    img.paste(win.resize((1952, round(212 * LK)), Image.LANCZOS), (FX0, LH - round(212 * LK)))
    img.paste(plate.resize((round(166 * LK), round(28 * LK)), Image.LANCZOS),
              (FX0 + round(30 * LK), LH - round(240 * LK)))
    d = ImageDraw.Draw(img)
    scene_side(d)
    reach_land(img)
    save_land(img, 'LS1b', 'window 131 over its CG, as on the PC', 'land-scenes', notes=[
        'Window 131 over its CG\'s lower\nquarter as on the PC, its speaker\non a plate; its lines (49 cells)\n'
        f'at {lm(32)}.',
        'The CG the frame\'s whole height;\nthe left side as LS1\'s.'])


def ls1m():
    img, d = land()
    pic = dump(LACQ, 'scene-menu.png')
    pic.paste(Image.new('RGB', (420, 190)), (470, 76))     # the PC's rows taken off: ours are at the thumb
    img.paste(pic.resize((1952, LH), Image.LANCZOS), (FX0, 0))
    d = ImageDraw.Draw(img)
    scene_side(d, drawer=False)
    items = ['Attack at dawn', 'Wait for the scouts', 'Ask Maris']
    h, gap = 123, 12
    x1 = FX1 - 30
    x0 = x1 - 500
    y = FOOT - 10 - len(items) * h - (len(items) - 1) * gap
    panel(d, (x0 - 16, y - 16, x1 + 16, FOOT - 10 + 16))
    for i, item in enumerate(items):
        row_box(d, (x0, y + i * (h + gap), x1, y + i * (h + gap) + h), item, size=48)
    reach_land(img)
    save_land(img, 'LS1m', 'a dialogue menu at the right thumb', 'land-scenes', notes=[
        'A `]` menu: the question\'s window\nat its place, our rows at the\nframe\'s right foot, at the thumb\n'
        f'(as LF2\'s ladder): rows {mm(h)} high,\nthe items {mm(48)}, in the script\'s\norder.',
        'On the PC the rows stand at the\nselection box\'s place (40 px\nrows: 3.5 mm here, too low for\n'
        'a finger).',
        'The drawer hides under the menu\n(the house\'s touch rule); Menu\nstays. Made up: the gallery\'s\n'
        'scene menu.'])


def ls2():
    img, d = land()
    pw, ph = round(1952 * 0.66), round(LH * 0.66)
    px0, py0 = (LW - pw) // 2, (LH - ph) // 2
    img.paste(scene_clean_raw().resize((pw, ph), Image.LANCZOS), (px0, py0))
    d = ImageDraw.Draw(img)
    scene_side(d)
    x0, x1 = RAIL_L[0], px0 - 44
    box = (x0, py0, x1, py0 + 290)
    speech_window(d, box, INTERCEPT, size=36)
    ty = py0 + 110                                  # the tail right, at Ex's portrait
    d.polygon([(x1 - 2, ty - 28), (x1 - 2, ty + 28), (x1 + 40, ty)], fill=PANEL)
    d.line([(x1, ty - 28), (x1 + 40, ty), (x1, ty + 28)], fill=GOLD, width=3)
    reach_land(img)
    save_land(img, 'LS2', 'upright\'s PS2 laid sideways: the window beside its speaker', 'land-scenes', notes=[
        'The owner\'s upright principle\nturned: the picture uncovered in\nthe middle, at 66% '
        f'({mm(ph)} high);\nthe left portrait\'s speech (Ex)\nin the left column at his height,\nits tail at him.',
        f'The script\'s breaks kept: in the\ncolumn\'s {mm(x1 - x0)} the speech\'s 32\ncells are {mm(36)} '
        '— half LS1\'s.',
        'The right portrait\'s speech in\nthe right column at its height;\nthe narration under Ex\'s window;\n'
        'Menu, Skip and Log as LS1\'s.'])


def p15():
    ls1(); ls1b(); ls1m(); ls2()


def main():
    global MAP, PROV_NAMES, PROV_POINTS
    if ROUND in ('P3', 'P5', 'P7', 'P9', 'P11', 'P13', 'P14', 'P15'):
        {'P3': p3, 'P5': p5, 'P7': p7, 'P9': p9, 'P11': p11, 'P13': p13, 'P14': p14, 'P15': p15}[ROUND]()
        overviews()
        return
    MAP, PROV_NAMES, PROV_POINTS = build_map()
    if ROUND in ('P4', 'P6', 'P8', 'P10', 'P12'):
        {'P4': p4, 'P6': p6, 'P8': p8, 'P10': p10, 'P12': p12}[ROUND]()
        overviews()
        return
    if GALLERY_MODE:
        if LOOK:                                # PS2's picture is the week-18 look's (Ex's scene)
            d2(); d2l()
        else:
            print('No phone-look folder beside the passes: PS2 and PS2l left out (docs/ui/gallery.md, §9).')
        d2b(); d2m()
        m1()
        pb3(); pb3r(); pb3x()
        p3(); p4(); p5(); p6(); p7()
        lm1(); lb2()
        lb2r1(); lb2x(); lb3o1(); lb3s(); lb3c(); lb3r(); lb3b()
        ld2(); lu1()
        la2(); la2n(); lw1()
        lh2(); lt2(); ll1()
        lo1(); lf2(); lf2e()
        for tab, entries in (('phone', DUMPED), ('phone-land', DUMPED_LAND)):
            manifest = {'written': datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S'), 'palette': 'lacquer',
                        'font': 'BizUdGothic', 'touch': True, 'mockup': True, 'entries': entries}
            with open(os.path.join(GALLERY, tab, 'current', 'manifest.json'), 'w', encoding='utf-8') as f:
                json.dump(manifest, f, ensure_ascii=False, indent=2)
        return
    p0()
    d1(); d2(); d2l(); d2b(); d2m(); d3()
    m1(); m2()
    b1(); b2()
    pb3(); pb3r(); pb3x()
    overviews()


main()
