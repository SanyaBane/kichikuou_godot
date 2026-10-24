"""Mock-ups of a more compact enemy attack screen (owner, 2026-10-24): the panels shorter, the province's picture smaller.

After the army became our party's cards (docs/ui/enemy-attack.md, §8) its panel is mostly empty; the owner asked for the
panels made smaller, the army's above all and the province's with a smaller picture, the cards as they are. Builds whole
screens from two window dumps (1280x824, the menu bar's 24 pixels on top) of the week-18 save's --scout look: the map
before the attack (the backdrop, blurred and dimmed as EnemyAttackView's shader does) and the scouted attack screen
(its parts: the title band, the picture, the province's rows, the army's block, the foot). Each panel is the dump's army
panel stretched by its frame (corners kept). The images stay outside the repository.

    python tools/ui/mock_attack_layout.py <map dump.png> <scouted attack dump.png> <out.png>
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
BRUSH = REPO + '/godot/fonts/Shojumaru-Regular.ttf'

GOLD = (196, 160, 86)
DIM_GOLD = (110, 86, 46)
SUBTITLE = (160, 140, 104)
FILL = (23, 17, 13)
NAME = (232, 199, 122)

BAR = 24
ARMY_PANEL = (600, 116, 1248, 612)
CORNER = 22


def screen(path):
    return Image.open(path).convert('RGB').crop((0, BAR, 1280, 800 + BAR))


def backdrop(map_shot):
    """textureLod(screen, 3) * (1 - 0.66): the map an eighth in size, scaled back, dimmed."""
    small = map_shot.resize((160, 100), Image.BOX).resize((1280, 800), Image.BILINEAR)
    return small.point(lambda v: round(v * 0.34))


def panel(source, w, h):
    """The army panel of the dump stretched to w x h by its frame: corners kept, edges stretched, the inside flat."""
    x0, y0, x1, y1 = ARMY_PANEL
    src = source.crop((x0, y0, x1, y1))
    sw, sh = src.size
    c = CORNER
    out = Image.new('RGB', (w, h), FILL)
    out.paste(src.crop((0, 0, c, c)), (0, 0))
    out.paste(src.crop((sw - c, 0, sw, c)), (w - c, 0))
    out.paste(src.crop((0, sh - c, c, sh)), (0, h - c))
    out.paste(src.crop((sw - c, sh - c, sw, sh)), (w - c, h - c))
    col = sw - 12 - c                              # a column of the edges with nothing on it (right of the heading's rule)
    out.paste(src.crop((col, 0, col + 1, c)).resize((w - 2 * c, c)), (c, 0))
    out.paste(src.crop((col, sh - c, col + 1, sh)).resize((w - 2 * c, c)), (c, h - c))
    row = sh - 40
    out.paste(src.crop((0, row, c, row + 1)).resize((c, h - 2 * c)), (0, c))
    out.paste(src.crop((sw - c, row, sw, row + 1)).resize((c, h - 2 * c)), (w - c, c))
    return out


def shadow(base, rect):
    x0, y0, x1, y1 = rect
    layer = Image.new('L', base.size, 0)
    ImageDraw.Draw(layer).rectangle((x0 + 4, y0 + 6, x1 + 4, y1 + 6), fill=150)
    layer = layer.filter(ImageFilter.GaussianBlur(8))
    base.paste(Image.new('RGB', base.size, (0, 0, 0)), (0, 0), layer)


def put_panel(base, source, rect):
    shadow(base, rect)
    x0, y0, x1, y1 = rect
    base.paste(panel(source, x1 - x0, y1 - y0), (x0, y0))


def heading(d, x, y, text, right):
    f = ImageFont.truetype(CINZEL, 13)
    try:
        f.set_variation_by_axes([600])
    except OSError:
        pass
    d.text((x, y + 12), text, font=f, fill=SUBTITLE, anchor='ls')
    end = x + d.textlength(text, font=f) + 10
    d.line((end, y + 8, right, y + 8), fill=DIM_GOLD, width=1)


def vs(d, cx, cy):
    f = ImageFont.truetype(BRUSH, 34)
    d.text((cx, cy), 'VS', font=f, fill=GOLD, anchor='mm')


class Parts:
    """The scouted attack screen's parts, cut from the dump (EnemyAttackView's layout)."""

    def __init__(self, s):
        self.s = s
        self.title = s.crop((24, 18, 1256, 104))                  # the title band with its shadow's margin
        self.picture = s.crop((54, 158, 482, 443))                # 428x285, its gold frame included
        self.rows = [s.crop((54, 456 + i * 36, 482, 456 + i * 36 + 32)) for i in range(4)]
        self.army = s.crop((640, 208, 1206, 532))                 # the headers, the bracket, the cards, the totals, the note
        self.foot = s.crop((24, 626, 1256, 790))


def picture(parts, w):
    h = round(w * 285 / 428)
    img = parts.picture.crop((1, 1, 427, 284)).resize((w - 2, h - 2), Image.LANCZOS)
    framed = Image.new('RGB', (w, h), GOLD)
    framed.paste(img, (1, 1))
    return framed


def province(base, d, parts, rect, pic_w, pitch=36):
    """The province's panel: heading, the picture centred, the four rows under it (each `pitch` apart)."""
    x0, y0, x1, y1 = rect
    put_panel(base, parts.s, rect)
    heading(d, x0 + 22, y0 + 16, 'DEFENDING · YS', x1 - 22)
    pic = picture(parts, pic_w)
    base.paste(pic, (x0 + (x1 - x0 - pic_w) // 2, y0 + 42))
    rows_x = x0 + (x1 - x0 - 428) // 2
    y = y0 + 42 + pic.height + 12
    for i, row in enumerate(parts.rows):
        part = row if pitch >= 32 else row.crop((0, 0, 428, pitch))
        base.paste(part, (rows_x, y + i * pitch))
        if pitch < 32 and i < 3:
            d.line((rows_x, y + i * pitch + pitch - 1, rows_x + 428, y + i * pitch + pitch - 1), fill=(60, 46, 30), width=1)
    return y + 4 * pitch


def army(base, d, parts, rect):
    """The army's panel: heading, the block centred under it."""
    x0, y0, x1, y1 = rect
    put_panel(base, parts.s, rect)
    heading(d, x0 + 22, y0 + 16, 'ATTACKING · LEAZAS REBEL ARMY', x1 - 22)
    bw, bh = parts.army.size
    top = y0 + 34 + (y1 - y0 - 34 - 12 - bh) // 2
    base.paste(parts.army, (x0 + (x1 - x0 - bw) // 2, top))


def foot(base, parts, y):
    base.paste(parts.foot, (24, y - 6))


def title(base, parts, y):
    base.paste(parts.title, (24, y - 6))


# ---------------------------------------------------------------- the layouts

def now(back, parts):
    return parts.s.copy()


def a_block_centred(back, parts):
    """A: both panels 404 tall (the province's picture 300x200), the same widths; title, panels and foot one block,
    centred on the screen."""
    base = back.copy()
    d = ImageDraw.Draw(base)
    h, gap = 404, 20
    top = (800 - (72 + gap + h + gap + 148)) // 2
    title(base, parts, top)
    py = top + 72 + gap
    province(base, d, parts, (32, py, 504, py + h), 300)
    vs(d, 552, py + h // 2)
    army(base, d, parts, (600, py, 1248, py + h))
    foot(base, parts, py + h + gap)
    return base


def b_army_narrower(back, parts):
    """B: as A, the army's panel narrowed to its block (600) and the province's widened (520) to keep the width; the
    picture 270x180 and the rows 30 apart, the panels 380 tall."""
    base = back.copy()
    d = ImageDraw.Draw(base)
    h, gap = 380, 20
    top = (800 - (72 + gap + h + gap + 148)) // 2
    title(base, parts, top)
    py = top + 72 + gap
    province(base, d, parts, (32, py, 552, py + h), 270, pitch=30)
    vs(d, 600, py + h // 2)
    army(base, d, parts, (648, py, 1248, py + h))
    foot(base, parts, py + h + gap)
    return base


def c_edges_kept(back, parts):
    """C: the title band and the foot where they are; the panels 404 tall (the picture 300x200) centred between them."""
    base = back.copy()
    d = ImageDraw.Draw(base)
    h = 404
    title(base, parts, 24)
    py = 96 + (632 - 96 - h) // 2
    province(base, d, parts, (32, py, 504, py + h), 300)
    vs(d, 552, py + h // 2)
    army(base, d, parts, (600, py, 1248, py + h))
    foot(base, parts, 632)
    return base


LAYOUTS = [
    ('Now', now),
    ('A  one block, centred', a_block_centred),
    ('B  the army narrower', b_army_narrower),
    ('C  title and foot kept', c_edges_kept),
]


def main():
    back = backdrop(screen(sys.argv[1]))
    parts = Parts(screen(sys.argv[2]))
    scale, gap, head, cols = 0.75, 24, 40, 2
    w, h = round(1280 * scale), round(800 * scale)
    rows = (len(LAYOUTS) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * w + (cols + 1) * gap, rows * (h + head) + (rows + 1) * gap), (24, 19, 15))
    d = ImageDraw.Draw(sheet)
    f = ImageFont.truetype(CINZEL, 22)
    for i, (name, make) in enumerate(LAYOUTS):
        x = gap + (i % cols) * (w + gap)
        y = gap + (i // cols) * (h + head + gap)
        d.text((x, y + 28), name, font=f, fill=NAME, anchor='ls')
        sheet.paste(make(back, parts).resize((w, h), Image.LANCZOS), (x, y + head))
    sheet.save(sys.argv[3])


if __name__ == '__main__':
    main()
