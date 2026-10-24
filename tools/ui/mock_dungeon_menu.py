"""Mock-ups of the dungeon's floor menu (owner, 2026-10-25: Cﾒｲｷｭｳ.ADV:858-893 offers Ascend / Descend / Retreat only
where they apply, so the rows move between floors and a click on the same spot can pick Ascend where Descend stood the
floor before — and Ascend costs a floor back with an encounter roll; docs/ui/dungeon-menu.md), drawn over two window
screenshots of the floor menu in our interface: the Monsters' Cave (dungeon 2) on floor 1 (floor1.png: Descend,
Retreat) and on floor 2 (floor2.png: Ascend, Descend, Retreat), the window 1414x940 with its title and menu bar.

The screenshots come from a window at that size (the 1280x800 screen scaled into the client area under the menu bar):
  <Godot console exe> --path godot -- --load --saves user://saves-look-dungeon --slots-from user://saves-week18
and a search of the Monsters' Cave by hand. Then, from the repository's root (Pillow; godot/fonts):
  python -I tools/ui/mock_dungeon_menu.py <screenshot dir> <out dir> < /dev/null
Keep <out dir> outside the repository.

Variant 1 keeps the menu where it is and always shows its three rows, the one the script does not offer dimmed; the
rows carry the floor they lead to, Retreat stands apart. Variant 3 replaces the menu by a ladder right of the dungeon's
picture: up and down as arrows around the floor, the way deeper in gold, Retreat under them.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

SHOTS, OUT = sys.argv[1], sys.argv[2]
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
os.makedirs(OUT, exist_ok=True)
S = 2
W, H = 1280, 800

# Lacquer's colours (godot/Strategy/UiPalette.cs).
PANEL = (22, 16, 12, 255)                # opaque here: today's menu is under it in the screenshots
GOLD, DIM_GOLD, HOVER_LINE = (196, 160, 86), (110, 86, 46), (222, 186, 104)
NAME, SUBTITLE = (232, 199, 122), (160, 140, 104)
ROW, ROW_HOVER, TEXT, QUIET = (34, 26, 20), (64, 48, 28), (244, 232, 206), (176, 160, 130)
OFF_TEXT, OFF_BUTTON, OFF_LINE = (98, 88, 74), (40, 35, 30), (66, 58, 48)
RETREAT, RETREAT_LINE, RETREAT_TEXT = (96, 30, 24), (214, 150, 90), (250, 226, 160)
NOTE = (255, 92, 160)                   # the mock-ups' own notes (not part of a screen)


def font(name, size, weight=None):
    f = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', name), int(size * S))
    if weight is not None:
        f.set_variation_by_axes([weight])
    return f


GOTHIC = lambda size: font('BIZUDGothic-Regular.ttf', size)
BRUSH = lambda size: font('Shojumaru-Regular.ttf', size)
CINZEL = lambda size, w=500: font('Cinzel-VariableFont_wght.ttf', size, w)


def screen(name):
    """The screenshot's client area under the menu bar, scaled back to the 1280x800 screen."""
    return Image.open(os.path.join(SHOTS, name)).convert('RGB').crop((1, 57, 1413, 939)).resize((W, H), Image.LANCZOS)


def without_menu(img):
    """Floor 1's screen with today's menu taken away: black above the picture's frame, the frame's top line carried
    across from its left of the menu (the frame's centre ornament, under the menu, is lost)."""
    img = img.copy()
    ImageDraw.Draw(img).rectangle((478, 70, 832, 186), fill=(0, 0, 0))
    strip = img.crop((468, 186, 469, 200))
    for x in range(478, 833):
        img.paste(strip, (x, 186))
    return img


class Frame:
    def __init__(self, base):
        self.img = base.resize((W * S, H * S), Image.LANCZOS).convert('RGBA')

    def draw(self):
        lay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        return lay, ImageDraw.Draw(lay)

    def add(self, lay):
        self.img = Image.alpha_composite(self.img, lay)

    def save(self, name):
        self.img.resize((W, H), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, name))


def sc(*v):
    return [round(a * S) for a in v]


def text(f, x, y, t, fnt, colour, anchor='lm'):
    lay, d = f.draw()
    d.text((x * S, y * S), t, font=fnt, fill=colour, anchor=anchor)
    f.add(lay)


def box(f, r, fill=None, outline=None, width=1):
    lay, d = f.draw()
    d.rectangle(sc(*r), fill=fill, outline=outline, width=round(width * S))
    f.add(lay)


def triangle(f, cx, cy, size, up, colour):
    lay, d = f.draw()
    h = size * 0.87
    if up:
        d.polygon(sc(cx - size / 2, cy + h / 2, cx + size / 2, cy + h / 2, cx, cy - h / 2), fill=colour)
    else:
        d.polygon(sc(cx - size / 2, cy - h / 2, cx + size / 2, cy - h / 2, cx, cy + h / 2), fill=colour)
    f.add(lay)


def panel(f, x, y, w, h):
    """Lacquer.DrawPanel: the shadow, the lacquer, the double gold frame and its corners."""
    lay, d = f.draw()
    for i in range(6, 0, -1):
        d.rectangle(sc(x - i + 2, y - i + 6, x + w + i + 2, y + h + i + 6), fill=(0, 0, 0, 15))
    d.rectangle(sc(x, y, x + w, y + h), fill=PANEL)
    d.rectangle(sc(x + 1, y + 1, x + w - 1, y + h - 1), outline=GOLD, width=2 * S)
    d.rectangle(sc(x + 5.5, y + 5.5, x + w - 5.5, y + h - 5.5), outline=DIM_GOLD, width=S)
    for cx, cy, sx, sy in ((x, y, 1, 1), (x + w, y, -1, 1), (x, y + h, 1, -1), (x + w, y + h, -1, -1)):
        d.line(sc(cx + sx * 9, cy + sy * 2, cx + sx * 9, cy + sy * 9, cx + sx * 2, cy + sy * 9), fill=GOLD, width=2 * S)
        d.polygon(sc(cx + sx * 9, cy + sy * 14, cx + sx * 14, cy + sy * 9, cx + sx * 9, cy + sy * 4, cx + sx * 4, cy + sy * 9),
                  fill=GOLD)
    f.add(lay)


def divider(f, x0, x1, y):
    lay, d = f.draw()
    m = (x0 + x1) / 2
    d.line(sc(x0, y, m - 10, y), fill=DIM_GOLD, width=S)
    d.line(sc(m + 10, y, x1, y), fill=DIM_GOLD, width=S)
    d.polygon(sc(m, y - 4, m + 4, y, m, y + 4, m - 4, y), fill=GOLD)
    f.add(lay)


def note(f, x, y, t, anchor='la', size=15):
    """A note on a dark plate, so it reads over the picture."""
    lay, d = f.draw()
    fnt = GOTHIC(size)
    l, t0, r, b = d.textbbox((x * S, y * S), t, font=fnt, anchor=anchor)
    d.rectangle((l - 5 * S, t0 - 3 * S, r + 5 * S, b + 3 * S), fill=(0, 0, 0, 200))
    d.text((x * S, y * S), t, font=fnt, fill=NOTE, anchor=anchor)
    f.add(lay)


# ---------------------------------------------------------------------------------------------- the window under it
WINDOW_TEXT = (226, 204, 150)


def window_line(f, base, x, y, t):
    """Rewrites one line of the place's window (WAO 113: the dungeon, the floor, the unit, its troops), in a narrowed
    BIZ UDGothic standing in for the game's font."""
    fill = base.getpixel((900, 700))
    box(f, (272, y - 26, 1000, y + 26), fill=fill)
    fnt = ImageFont.truetype(os.path.join(REPO, 'godot', 'fonts', 'BIZUDGothic-Regular.ttf'), 50 * S)
    probe = ImageDraw.Draw(Image.new('L', (1, 1)))
    l, t0, r, b = probe.textbbox((0, 0), t, font=fnt)
    glyphs = Image.new('RGBA', (r - l + 4, b - t0 + 4), (0, 0, 0, 0))
    ImageDraw.Draw(glyphs).text((2 - l, 2 - t0), t, font=fnt, fill=WINDOW_TEXT, stroke_width=3,
                                stroke_fill=WINDOW_TEXT)
    glyphs = glyphs.resize((round(glyphs.width * 0.78), glyphs.height), Image.LANCZOS)
    placed = Image.new('RGBA', f.img.size, (0, 0, 0, 0))
    placed.paste(glyphs, (round(x * S), round(y * S - glyphs.height / 2)))
    f.add(placed)


def place(f, base, dungeon=None, floor=None):
    if dungeon is not None:
        window_line(f, base, 280, 583, dungeon)
    if floor is not None:
        window_line(f, base, 330, 635, floor)


# -------------------------------------------------------------------------------------------------------- the floors
# A floor's moves as the script offers them (Cﾒｲｷｭｳ.ADV:864-889): in a cave (FloorMode 1) Ascend is back, Descend
# deeper; in a tower (FloorMode 2) Ascend is deeper, Descend back. Back is offered above floor 1, deeper below the last.
def moves(floor, last, tower):
    back = (floor - 1) if floor != 1 else None
    deeper = (floor + 1) if floor < last else None
    if tower:
        return dict(Ascend=(deeper, True, 'the top floor'), Descend=(back, False, 'the entrance'))
    return dict(Ascend=(back, False, 'the entrance'), Descend=(deeper, True, 'the deepest floor'))


# ------------------------------------------------------------------------------------------------------- variant 1
# The original's box is at (501, 100) on the screen; SceneMenuView's panel starts 14 above and left of it, its rows 40
# high and 9 apart at the default dialogue height (40).
BOX_X, BOX_Y, PAD, ROW_H, ROW_GAP, PANEL_W = 487, 86, 14, 40, 9, 335


def menu_row(f, r, label, on=True, hover=False, hint=None):
    box(f, r, fill=ROW_HOVER if hover else ROW, outline=HOVER_LINE if hover else (DIM_GOLD if on else OFF_LINE),
        width=2 if hover else 1)
    x0, y0, x1, y1 = r
    text(f, x0 + 14, (y0 + y1) / 2, label, GOTHIC(30), TEXT if on else OFF_TEXT)
    if hint:
        text(f, x1 - 12, (y0 + y1) / 2 + 1, hint, GOTHIC(15), QUIET if on else OFF_TEXT, 'rm')


def variant_1(base, floor, last, tower, hover=None, hints=True):
    f = Frame(base)
    m = moves(floor, last, tower)
    apart = 9 if hints else 0
    h = 2 * PAD + 3 * ROW_H + 2 * ROW_GAP + 2 * apart
    panel(f, BOX_X, BOX_Y, PANEL_W, h)
    x0, x1, y = BOX_X + PAD, BOX_X + PANEL_W - PAD, BOX_Y + PAD
    for label in ('Ascend', 'Descend'):
        to, _, why = m[label]
        on = to is not None
        hint = (f'to {to}F' if on else why) if hints else None
        menu_row(f, (x0, y, x1, y + ROW_H), label, on, hover == label, hint)
        y += ROW_H + ROW_GAP
    if hints:
        divider(f, x0 + 10, x1 - 10, y + apart - ROW_GAP / 2)
        y += 2 * apart
    menu_row(f, (x0, y, x1, y + ROW_H), 'Retreat', True, hover == 'Retreat', 'leave' if hints else None)
    return f


# ------------------------------------------------------------------------------------------------------- variant 3
LAD_X, LAD_Y, LAD_W, LAD_H = 948, 196, 248, 340


def arrow_button(f, r, up, label, to, deeper, why, hover=False, hide=False):
    x0, y0, x1, y1 = r
    on = to is not None
    if hide and not on:
        return                          # its place left empty: the other button and Retreat do not move
    box(f, r, fill=(ROW_HOVER if hover else ROW) if on else OFF_BUTTON,
        outline=HOVER_LINE if hover else (DIM_GOLD if on else OFF_LINE), width=2 if hover else 1)
    cy = (y0 + y1) / 2
    triangle(f, x0 + 34, cy, 30, up, (GOLD if deeper else QUIET) if on else OFF_LINE)
    text(f, x0 + 62, cy - 11, label, GOTHIC(24), TEXT if on else OFF_TEXT)
    if on:
        sub = f'{"deeper" if deeper else "back"} · to {to}F'
    else:
        sub = why
    text(f, x0 + 63, cy + 17, sub, GOTHIC(14), (NAME if deeper else QUIET) if on else OFF_TEXT)


def variant_3(base, floor, last, tower, hover=None, hide=False):
    f = Frame(base)
    m = moves(floor, last, tower)
    panel(f, LAD_X, LAD_Y, LAD_W, LAD_H)
    x0, x1 = LAD_X + PAD + 2, LAD_X + LAD_W - PAD - 2
    up, down = m['Ascend'], m['Descend']
    arrow_button(f, (x0, LAD_Y + 18, x1, LAD_Y + 90), True, 'Ascend', up[0], up[1], up[2], hover == 'Ascend', hide)
    # The floor between the arrows.
    text(f, (x0 + x1) / 2, LAD_Y + 112, 'FLOOR', CINZEL(12, 600), SUBTITLE, 'mm')
    text(f, (x0 + x1) / 2, LAD_Y + 142, f'{floor}F', BRUSH(34), NAME, 'mm')
    arrow_button(f, (x0, LAD_Y + 174, x1, LAD_Y + 246), False, 'Descend', down[0], down[1], down[2], hover == 'Descend',
                 hide)
    divider(f, x0 + 8, x1 - 8, LAD_Y + 266)
    r = (x0, LAD_Y + 284, x1, LAD_Y + LAD_H - 16)
    box(f, r, fill=RETREAT, outline=GOLD, width=2)
    box(f, (r[0] + 4, r[1] + 4, r[2] - 4, r[3] - 4), outline=RETREAT_LINE)
    text(f, (r[0] + r[2]) / 2, (r[1] + r[3]) / 2 + 1, 'Retreat', BRUSH(20), RETREAT_TEXT, 'mm')
    return f


# ---------------------------------------------------------------------------------------------------------- frames
floor1, floor2 = screen('floor1.png'), screen('floor2.png')
clean1 = without_menu(floor1)

f = variant_1(floor1, 1, 14, False, hints=False)
note(f, 16, 14, '1 (plain) — always three rows, the one not offered dimmed: the Monsters\' Cave, 1F', size=17)
note(f, 16, 38, 'Descend and Retreat stay where they are on every floor; a dimmed row takes no click and no key')
f.save('1-plain-cave-1F.png')

f = variant_1(floor1, 1, 14, False)
note(f, 16, 14, '1a — three rows, each with where it leads; Retreat apart: the Monsters\' Cave, 1F', size=17)
note(f, 16, 38, 'Ascend dimmed ("the entrance"): floor 1 has no way up; the rows\' places never change')
f.save('1a-cave-1F.png')

f = variant_1(floor2, 2, 14, False, hover='Descend')
note(f, 16, 14, '1b — the Monsters\' Cave, 2F: all three offered, Descend under the pointer', size=17)
note(f, 16, 38, 'the same pixel as on 1F still reads Descend')
f.save('1b-cave-2F.png')

f = variant_1(floor2, 14, 14, False)
place(f, floor2, floor='14F')
note(f, 16, 14, '1c — the Monsters\' Cave, 14F (its last floor): Descend dimmed', size=17)
note(f, 16, 38, 'the original shows Ascend and Retreat here; "the deepest floor" says only what the missing row says')
f.save('1c-cave-last.png')

f = variant_1(floor2, 2, 30, True)
place(f, floor2, dungeon='Rising Dragon Mountain', floor='2F')
note(f, 16, 14, '1d — a tower, Rising Dragon Mountain (dungeon 11), 2F: Ascend goes deeper, Descend back', size=17)
note(f, 16, 38, 'the script\'s order is the same (Ascend, Descend, Retreat); the hints tell which way is deeper')
note(f, 16, 62, 'the cave\'s picture standing in for the mountain\'s')
f.save('1d-tower-2F.png')

f = variant_3(clean1, 1, 14, False)
note(f, 16, 14, '3a — a ladder right of the picture, no menu over it: the Monsters\' Cave, 1F', size=17)
note(f, 16, 38, 'up and down around the floor; the way deeper has the gold arrow; Retreat under them, apart')
note(f, 16, 62, 'Ascend dimmed: floor 1 has no way up; a right click opens the menu again, as the original\'s')
f.save('3a-cave-1F.png')

f = variant_3(clean1, 2, 14, False, hover='Descend')
place(f, floor1, floor='2F')
note(f, 16, 14, '3b — the Monsters\' Cave, 2F: both ways open, Descend under the pointer', size=17)
f.save('3b-cave-2F.png')

f = variant_3(clean1, 14, 14, False)
place(f, floor1, floor='14F')
note(f, 16, 14, '3c — the Monsters\' Cave, 14F (its last floor): Descend dimmed', size=17)
f.save('3c-cave-last.png')

f = variant_3(clean1, 2, 30, True)
place(f, floor1, dungeon='Rising Dragon Mountain', floor='2F')
note(f, 16, 14, '3d — a tower, Rising Dragon Mountain (dungeon 11), 2F: the gold arrow points up', size=17)
note(f, 16, 38, 'up is deeper in a tower, down is back; the cave\'s picture standing in for the mountain\'s')
f.save('3d-tower-2F.png')

# Round 2 (owner, 2026-10-25): the ladder with a way not offered hidden instead of dimmed, its place left empty.
f = variant_3(clean1, 1, 14, False, hide=True)
note(f, 16, 14, '3e — the ladder, a way not offered hidden: the Monsters\' Cave, 1F', size=17)
note(f, 16, 38, 'no Ascend on floor 1: its place stays empty, so Descend and Retreat stand where they do on 2F (3b)')
f.save('3e-hidden-cave-1F.png')

f = variant_3(clean1, 14, 14, False, hide=True)
place(f, floor1, floor='14F')
note(f, 16, 14, '3f — the Monsters\' Cave, 14F (its last floor): no Descend', size=17)
note(f, 16, 38, 'the floor plate and Retreat keep their places')
f.save('3f-hidden-cave-last.png')

f = variant_3(clean1, 1, 30, True, hide=True)
place(f, floor1, dungeon='Rising Dragon Mountain', floor='1F')
note(f, 16, 14, '3g — a tower, Rising Dragon Mountain (dungeon 11), 1F: no Descend, the gold arrow up', size=17)
note(f, 16, 38, 'the cave\'s picture standing in for the mountain\'s')
f.save('3g-hidden-tower-1F.png')
