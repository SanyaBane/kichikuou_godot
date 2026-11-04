"""Mock-ups of the deployment ledger's Name · Type cell in the other modes (owner, 2026-11-04: "the same design" as the
upright phone's A2 — tools/ui/mock_rows_under_type.py — on the PC, the tablet and the phone held landscape). One cell
replaces the Name, Type and Rows columns: the face from the name's top to the HP bar's foot, the name, the type and
the rows under it from one left edge, the troops' and the HP bars under them. The proportions are A2's, scaled so the
type is to the mode's ledger text as on the upright phone (34 to 46): the PC's and the tablet's text 17, held
landscape's 26 (the dumps' pixels). Troops is cut from each dump; Refill (its button half the row, as upright),
Cost and Item are drawn after it with the week-18 save's figures.

Drawn over the screen gallery's `deployment` dumps (lacquer, touch, phone-landscape); the faces from the landscape
dump, Asuka's (unit 11) from her CG 430, the bars from the landscape dump too.

Usage: python mock_name_cell.py <screen-gallery folder> <out dir> <cg_png folder>
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONTS = ROOT / 'godot' / 'fonts'

INK = (43, 31, 20)
INK_QUIET = (100, 80, 56)
HEAD_INK = (125, 50, 34)
PAPER = (224, 208, 172)
RULE = (192, 170, 132)
TROOPS_BLUE = (52, 88, 168)
HP_GREEN = (76, 146, 76)
FACE_BACK = (72, 66, 60)                # the faces' backdrop where a CG is transparent
SHEET = (24, 20, 18)
NAME = (232, 199, 122)
TEXT = (244, 232, 206)

# A2 in the upright dump's pixels (half the reference's): the row, the name's size and middle, the type's size and
# middle, the rows' middle, the face's top and side, the gap after it, the bars' tops and height, the gap between the
# name's right and the cell's.
A2 = dict(row=83, name=22, name_mid=19, type=17, type_mid=39, rows_mid=56, face_top=8, face=68, face_gap=8,
          bar=66, hp=72, bar_h=4, right=6)

# The rows: № and the landscape dump's row (faces), the upright dump's row (bars), name, type, rows, troops, max troops,
# refill (K, 0 none), cost (K), item; Asuka (unit 11) from her CG with the square the screens cut (UnitFaces).
ROWS = [
    (1, 0, 'Rance', 'Leazas Knights', 'Front', 0, 300, 'L. Holy Sword'),
    (3, 1, 'Bound', 'Thieves', 'Front', 0, 0, ''),
    (4, 2, 'Soul', 'Thieves', 'Front', 0, 0, ''),
    (5, 3, 'Leila', 'Royal Guards', 'Front', 0, 1200, ''),
    (6, 4, 'Barres', 'Leazas Knights', 'Front', 100, 1100, ''),
    (7, 5, 'Rick', 'Leazas Knights', 'Front', 300, 1000, 'By Lord'),
    (8, 6, 'Cordoba', 'Leazas Knights', 'Front', 0, 1100, ''),
    (9, 7, 'Kinkaid', 'Leazas Soldiers', 'Front', 1100, 900, ''),
    (10, 8, 'Menad', 'Leazas Soldiers', 'Front', 1200, 800, ''),
    (11, None, 'Asuka', 'Leazas Mages', 'Back', 0, 100, ''),
]
ASUKA_CG, ASUKA_CROP = 430, (26, 45, 144)
LAND = dict(top=130, pitch=74, face=(183, 8, 243, 67), bars=(254, 50, 438, 60))   # the landscape dump's rows, face, bars

# Each mode's dump: the paper's left and right, the seal's columns, №'s right, the cell's left (where Name began), the
# header's band and titles' baseline, the rows' top, pitch and the view's foot, Troops' columns in the dump, the text's
# size, the header's size, the Refill button's height now.
MODES = {
    'pc': dict(file='lacquer/current/deployment.png', paper=(32, 686), seal=(34, 60), number=88, cell=90,
               head=(118, 139), head_base=134, top=141, pitch=24, foot=762, troops=(457, 562), text=17, head_size=12.5,
               chip=20, title='PC'),
    'tablet': dict(file='touch/current/deployment.png', paper=(32, 686), seal=(34, 60), number=88, cell=90,
                   head=(126, 151), head_base=146, top=153, pitch=36, foot=762, troops=(457, 562), text=17,
                   head_size=12.5, chip=26, title='Tablet'),
    'land': dict(file='phone-landscape/current/deployment.png', paper=(80, 1055), seal=(84, 122), number=170, cell=174,
                 head=(92, 128), head_base=118, top=130, pitch=74, foot=790, troops=(815, 985), text=26.3,
                 head_size=19, chip=58, title='Phone, landscape'),
}


def gothic(px):
    return ImageFont.truetype(str(FONTS / 'BIZUDGothic-Regular.ttf'), px)


def cinzel(px, weight=700):
    f = ImageFont.truetype(str(FONTS / 'Cinzel-VariableFont_wght.ttf'), px)
    f.set_variation_by_axes([weight])
    return f


def k(n):
    return f'{n:,}K'


def middle(d, x, mid, text, font, fill, anchor='l'):
    """Text from x (or ending at x, anchor 'r'), its ascent-to-descent box centred on mid, as Lacquer's Middle."""
    ascent, descent = font.getmetrics()
    baseline = mid + (ascent - descent) / 2
    if anchor == 'r':
        x -= d.textlength(text, font=font)
    d.text((x, baseline), text, font=font, fill=fill, anchor='ls')


def asuka_face(cgs, side):
    x, y, size = ASUKA_CROP
    cg = Image.open(cgs / f'CG{ASUKA_CG:04d}.png').convert('RGB').crop((x, y, x + size, y + size))
    px = cg.load()
    for j in range(cg.height):
        for i in range(cg.width):
            r, g, b = px[i, j]
            if r > 240 and g < 20 and b > 240:
                px[i, j] = FACE_BACK
    return cg.resize((side, side), Image.LANCZOS)


def draw(mode, gallery, cgs):
    m = MODES[mode]
    src = Image.open(gallery / m['file']).convert('RGB')
    land = Image.open(gallery / 'phone-landscape/current/deployment.png').convert('RGB')
    s = m['text'] * 34 / 46 / A2['type']                          # A2's type is to the text as 34 to 46
    g = {key: v * s for key, v in A2.items()}
    row, face = round(g['row']), round(g['face'])
    name_font, type_font, cells = gothic(g['name']), gothic(g['type']), gothic(m['text'])
    text_x = m['cell'] + 4 + face + g['face_gap']
    room = max(type_font.getlength('Leazas Soldiers'), name_font.getlength('Cordoba'))
    cell_right = text_x + room + g['right']
    troops_w = m['troops'][1] - m['troops'][0]
    troops_x = cell_right + 2
    refill_right = troops_x + troops_w + cells.getlength('2,300K') + 14
    cost_right = refill_right + cells.getlength('1,200K') + 14
    item_x = cost_right + m['text'] * 0.75 + 8
    im = src.copy()
    d = ImageDraw.Draw(im)
    left, right = m['paper']
    # The header: "Name · Type" over the names, Troops, Refill, Cost and Item after the cell.
    head_paper = src.getpixel((m['cell'] + 2, m['head'][0] + 3))
    d.rectangle((m['cell'], m['head'][0], right - 1, m['head'][1]), fill=head_paper)
    hf = cinzel(m['head_size'])
    d.text((text_x, m['head_base']), 'Name · Type', font=hf, fill=HEAD_INK, anchor='ls')
    d.text((troops_x + 4, m['head_base']), 'Troops', font=hf, fill=HEAD_INK, anchor='ls')
    for word, r in [('Refill', refill_right), ('Cost', cost_right)]:
        d.text((r - d.textlength(word, font=hf), m['head_base']), word, font=hf, fill=HEAD_INK, anchor='ls')
    d.text((item_x, m['head_base']), 'Item', font=hf, fill=HEAD_INK, anchor='ls')
    # The rows.
    d.rectangle((left, m['top'], right - 1, m['foot']), fill=PAPER)
    y = m['top']
    for i, (number, at, name, kind, rows, refill, cost, item) in enumerate(ROWS):
        if y >= m['foot']:
            break
        y0 = m['top'] + m['pitch'] * i
        bg = src.getpixel((m['troops'][0] - 3, y0 + 2))
        d.rectangle((left, y, right - 1, y + row - 1), fill=bg)
        seal = src.crop((m['seal'][0], y0 + 2, m['seal'][1], y0 + m['pitch'] - 2))   # inside the row's rules
        im.paste(seal, (m['seal'][0], y + (row - m['pitch']) // 2 + 2))
        name_mid = y + g['name_mid']
        middle(d, m['number'], name_mid, str(number), cells, INK, anchor='r')
        fy = y + round(g['face_top'])
        if at is not None:
            ly = LAND['top'] + LAND['pitch'] * at
            f = LAND['face']
            pic = land.crop((f[0], ly + f[1], f[2], ly + f[3])).resize((face, face), Image.LANCZOS)
            b = LAND['bars']
            bars = land.crop((b[0], ly + b[1], b[2], ly + b[3]))
        else:
            pic = asuka_face(cgs, face)
            bars = Image.new('RGB', (LAND['bars'][2] - LAND['bars'][0], LAND['bars'][3] - LAND['bars'][1]), PAPER)
            bd = ImageDraw.Draw(bars)
            bd.rectangle((0, 0, bars.width - 1, 3), fill=TROOPS_BLUE)
            bd.rectangle((0, 6, bars.width - 1, 9), fill=HP_GREEN)
        im.paste(pic, (m['cell'] + 4, fy))
        d.rectangle((m['cell'] + 4, fy, m['cell'] + 4 + face - 1, fy + face - 1), outline=INK_QUIET)
        middle(d, text_x, name_mid, name, name_font, INK)
        middle(d, text_x, y + g['type_mid'], kind, type_font, INK_QUIET)
        middle(d, text_x, y + g['rows_mid'], rows, type_font, INK_QUIET)
        bars_h = round(g['hp'] - g['bar'] + g['bar_h'])
        bars = bars.resize((round(room), bars_h), Image.NEAREST)
        im.paste(bars, (round(text_x), y + round(g['bar'])))
        # Troops from the dump, centred; Refill, Cost and Item drawn.
        troops = src.crop((m['troops'][0], y0 + 2, m['troops'][1], y0 + m['pitch'] - 2))   # inside the row's rules
        im.paste(troops, (round(troops_x), y + (row - m['pitch']) // 2 + 2))
        mid = y + row / 2
        if refill:
            text = k(refill)
            w = d.textlength(text, font=cells)
            ch = row * 0.5                                        # the upright's proportion: 83 in a row of 166
            d.rounded_rectangle((refill_right - w - 10, mid - ch / 2, refill_right + 4, mid + ch / 2),
                                radius=max(3, ch * 0.18), fill=(232, 220, 192), outline=(120, 140, 172),
                                width=max(1, round(m['text'] / 12)))
            middle(d, refill_right - 3, mid, text, cells, TROOPS_BLUE, anchor='r')
        else:
            middle(d, refill_right - 3, mid, '–', cells, INK, anchor='r')
        middle(d, cost_right, mid, k(cost), cells, INK, anchor='r')
        middle(d, item_x, mid, item or '—', cells, INK_QUIET)
        d.line((left, y + row - 1, right - 1, y + row - 1), fill=RULE)
        y += row
    im.paste(src.crop((left, m['foot'] + 1, right, src.height)), (left, m['foot'] + 1))
    return src, im


def main():
    gallery, out, cgs = Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3])
    out.mkdir(parents=True, exist_ok=True)
    for mode in MODES:
        before, after = draw(mode, gallery, cgs)
        after.save(out / f'deployment-{mode}.png')
        gap, head = 24, 50
        s = Image.new('RGB', (before.width * 2 + gap * 3, before.height + head + gap), SHEET)
        sd = ImageDraw.Draw(s)
        for i, (caption, im) in enumerate([('Now', before), (MODES[mode]['title'] + ': the Name · Type cell', after)]):
            x = gap + i * (before.width + gap)
            sd.text((x, 14), caption, font=cinzel(22), fill=NAME if i == 0 else TEXT)
            s.paste(im, (x, head))
        s.save(out / f'sheet-{mode}.png')


if __name__ == '__main__':
    main()
