"""Mock-ups of the deployment screen's party cards (owner, 2026-10-24): the troops a line higher, as current/maximum.

Draws variants of the cards' lower half over a screen dump of our deployment screen (a window's dump, 1280x824 with
the menu bar's 24 pixels on top, the four places filled): each card's lines under its troop type are painted over with
the card's colour and drawn again; the party panels side by side with their names, scaled 1.5x. The figures are the
dump's party, given here as the table shows them. The images stay outside the repository.

    python tools/ui/mock_cards.py <deployment dump.png> <out.png>

The week-18 save's --scout look (CLAUDE.md, "Commands") ends on such a dump: Barres (unit 6) 980/1,000, Rance (unit 1)
1,000/1,000, Kinkaid (unit 9) 901/1,500, Cordoba (unit 8) 1,000/1,000.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'
GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'

# Lacquer's colours (godot/Strategy/Lacquer.cs)
NAME = (232, 199, 122)
TEXT = (244, 232, 206)
QUIET = (176, 160, 130)
PLUS = (150, 196, 110)
AMBER = (214, 170, 80)
MINUS = (222, 110, 90)

BAR = 24
PANEL = (690, 368, 1264, 784)
CARDS = {  # place: (x0, y0, x1, y1) in the 1280x800 screen (DeploymentPanels.CardRect)
    3: (704, 440, 956, 544), 1: (962, 440, 1214, 544),
    4: (704, 552, 956, 656), 2: (962, 552, 1214, 656),
}
PARTY = {3: (980, 1000, 1100), 1: (1000, 1000, 300), 4: (901, 1500, 900), 2: (1000, 1000, 1100)}


def font(path, size):
    return ImageFont.truetype(path, size)


GOTHIC15 = font(GOTHIC, 15)
CINZEL11 = font(CINZEL, 11)


def number(n):
    return f'{n:,}'


def gold(k):
    return f'{k:,}K'


def strength(troops, maximum):
    f = troops / maximum
    return PLUS if f >= 0.75 else AMBER if f >= 0.35 else MINUS


def local(x, y):
    return x - PANEL[0], y - PANEL[1]


def text_area(card):
    x0, y0, x1, y1 = card
    return x0 + 108, x1 - 12


def bar(d, x, y, w, troops, maximum):
    d.rectangle((*local(x, y), *local(x + w - 1, y + 2)), fill=(0, 0, 0, 120))
    fw = max(1, round(w * min(1, troops / maximum)))
    d.rectangle((*local(x, y), *local(x + fw - 1, y + 2)), fill=strength(troops, maximum) + (255,))


def cost_right(d, right, baseline, k100):
    figure = gold(k100)
    fx = right - d.textlength(figure, font=GOTHIC15)
    d.text(local(fx, baseline), figure, font=GOTHIC15, fill=TEXT, anchor='ls')
    lx = fx - 6 - d.textlength('Cost', font=CINZEL11)
    d.text(local(lx, baseline), 'Cost', font=CINZEL11, fill=QUIET, anchor='ls')
    return lx


def troops_text(d, x, baseline, troops, maximum, quiet_max=False):
    now = number(troops)
    d.text(local(x, baseline), now, font=GOTHIC15, fill=TEXT, anchor='ls')
    x += d.textlength(now, font=GOTHIC15)
    d.text(local(x, baseline), '/' + number(maximum), font=GOTHIC15, fill=QUIET if quiet_max else TEXT, anchor='ls')


def cut(dump):
    return dump.crop((PANEL[0], PANEL[1] + BAR, PANEL[2], PANEL[3] + BAR)).convert('RGBA')


def cleared(panel):
    """The panel with every card's lines under the troop type painted over with the card's colour."""
    out = panel.copy()
    d = ImageDraw.Draw(out)
    for card in CARDS.values():
        x0, y0, x1, y1 = card
        tx, right = text_area(card)
        fill = out.getpixel(local(tx + 70, y0 + 62))
        d.rectangle((*local(tx - 1, y0 + 54), *local(right + 4, y1 - 3)), fill=fill)
    return out


# ---------------------------------------------------------------- the variants

def now(panel):
    return panel


def a_bar_under_troops(panel):
    """A: the troops as current/maximum on the third line with the strength bar under them; Cost alone at the foot."""
    out = cleared(panel)
    d = ImageDraw.Draw(out, 'RGBA')
    for place, card in CARDS.items():
        troops, maximum, cost = PARTY[place]
        tx, right = text_area(card)
        y0 = card[1]
        troops_text(d, tx, y0 + 68, troops, maximum)
        bar(d, tx, y0 + 72, right - tx, troops, maximum)
        cost_right(d, right, y0 + 94, cost)
    return out


def b_bar_at_foot(panel):
    """B: the troops on the third line; Cost on the fourth as now and the bar under it, at the card's foot."""
    out = cleared(panel)
    d = ImageDraw.Draw(out, 'RGBA')
    for place, card in CARDS.items():
        troops, maximum, cost = PARTY[place]
        tx, right = text_area(card)
        y0 = card[1]
        troops_text(d, tx, y0 + 68, troops, maximum)
        cost_right(d, right, y0 + 88, cost)
        bar(d, tx, y0 + 92, right - tx, troops, maximum)
    return out


def c_quiet_maximum(panel):
    """C: as A, the maximum quieter than the current figure (the figure that changes stands out)."""
    out = cleared(panel)
    d = ImageDraw.Draw(out, 'RGBA')
    for place, card in CARDS.items():
        troops, maximum, cost = PARTY[place]
        tx, right = text_area(card)
        y0 = card[1]
        troops_text(d, tx, y0 + 68, troops, maximum, quiet_max=True)
        bar(d, tx, y0 + 72, right - tx, troops, maximum)
        cost_right(d, right, y0 + 94, cost)
    return out


def d_bar_beside_troops(panel):
    """D: the troops on the third line with the bar right of them, on the same line; Cost alone at the foot."""
    out = cleared(panel)
    d = ImageDraw.Draw(out, 'RGBA')
    for place, card in CARDS.items():
        troops, maximum, cost = PARTY[place]
        tx, right = text_area(card)
        y0 = card[1]
        troops_text(d, tx, y0 + 70, troops, maximum)
        end = tx + d.textlength(f'{number(troops)}/{number(maximum)}', font=GOTHIC15)
        bar(d, round(end) + 8, y0 + 64, right - round(end) - 8, troops, maximum)
        cost_right(d, right, y0 + 93, cost)
    return out


VARIANTS = [
    ('Now', now),
    ('A  bar under the troops', a_bar_under_troops),
    ('B  bar at the foot', b_bar_at_foot),
    ('C  as A, maximum quieter', c_quiet_maximum),
    ('D  bar beside the troops', d_bar_beside_troops),
]


def main():
    dump = Image.open(sys.argv[1])
    panel = cut(dump)
    title = font(CINZEL, 22)
    pw, ph = panel.size
    gap, head = 24, 40
    cols = 3
    rows = (len(VARIANTS) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * pw + (cols + 1) * gap, rows * (ph + head) + (rows + 1) * gap), (24, 19, 15, 255))
    d = ImageDraw.Draw(sheet)
    for i, (name, draw) in enumerate(VARIANTS):
        x = gap + (i % cols) * (pw + gap)
        y = gap + (i // cols) * (ph + head + gap)
        d.text((x, y + 28), name, font=title, fill=NAME, anchor='ls')
        sheet.paste(draw(panel), (x, y + head))
    sheet = sheet.resize((round(sheet.width * 1.5), round(sheet.height * 1.5)), Image.LANCZOS)
    sheet.convert('RGB').save(sys.argv[2])


if __name__ == '__main__':
    main()
