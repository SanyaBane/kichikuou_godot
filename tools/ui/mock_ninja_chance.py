"""Mock-ups of the ninja follow-up's chance in Option → Tweaks (owner's request 2026-10-29): the original's roll after a
player's exchange (`FIGT.ADV:1915`, `Y 4, 3` and `RetVal = 1`, BRS-11) in thirds, the owner's choice — Never, 1 in 3 (the
original's, the default), 2 in 3, Always — as radios under "Chaos and Nikkou reach demons" in the Battle section.

Drawn over the screen gallery's dumps of the Tweaks tab (`options-tweaks`, the PC pass and the landscape phone's pass), the
Scenes section moved down to make room. From the repository's root (Pillow; the fonts of godot/fonts):
  python -I tools/ui/mock_ninja_chance.py <PC options-tweaks.png> <phone options-tweaks.png> <out dir> < /dev/null
Round 1: N1 (the chance low to high), N2 (the original's first, as the row above it), NP (the phone held landscape: the
Battle section across both columns, the row in the right column over Scenes, the radios' words short). The owner chose
N1 and NP, then left Never out and named the thirds 33%, 67%, 100% (built so, docs/tweaks.md, decision 33).
Everything is in the dumps' own pixels, plus a caption band below.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

PC, PHONE, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(OUT, exist_ok=True)
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'

# UiPalette's lacquer, the paper sheet's parts (tools/ui/mock_options.py)
PAPER, INK, INK_QUIET = (224, 208, 172), (40, 28, 18), (100, 80, 56)
BAND, GOLD, NAME, QUIET = (23, 18, 14), (196, 160, 86), (232, 199, 122), (176, 160, 130)
_fonts = {}

LABEL = 'Ninja follow-up'
HINT = ('After Menad, Hubert, Gandhi or Magic attacks, their ninja may strike the enemy commander for 1 HP. '
        'Ninja never kills.')
NOTE = "Never also keeps away one of Hubert's events: it comes after six of Freya's strikes."
ORIGINAL = '1 in 3 — as the original'


def font(size, path=GOTHIC, weight=None):
    key = (size, path, weight)
    if key not in _fonts:
        f = ImageFont.truetype(path, round(size))
        if weight is not None:
            f.set_variation_by_axes([weight])
        _fonts[key] = f
    return _fonts[key]


def crop_round(im, cx, cy, r):
    return im.crop((round(cx - r), round(cy - r), round(cx + r), round(cy + r)))


def wrap(d, text, x, baseline, width, size, pitch, fill):
    """Greedy word wrap as OptionsView.Wrap; returns the last line's baseline."""
    f, line, y = font(size), '', baseline
    for word in text.split(' '):
        trial = word if not line else line + ' ' + word
        if line and f.getlength(trial) > width:
            d.text((x, y), line, font=f, fill=fill, anchor='ls')
            line, y = word, y + pitch
        else:
            line = trial
    d.text((x, y), line, font=f, fill=fill, anchor='ls')
    return y


def band(im, caption, note, scale=1.0):
    w, h = im.size
    tall = round(64 * scale)
    sheet = Image.new('RGB', (w, h + tall), BAND)
    sheet.paste(im, (0, 0))
    d = ImageDraw.Draw(sheet)
    d.line((0, h, w, h), fill=GOLD, width=max(1, round(scale)))
    d.text((16 * scale, h + 10 * scale), caption, font=font(20 * scale, CINZEL, 700), fill=NAME)
    d.text((16 * scale, h + 38 * scale), note, font=font(15 * scale), fill=QUIET)
    return sheet


def pc(name, radios, caption, note):
    """OptionsView.DrawTweaks with the row after the reach note (its spacing the reach block's), Scenes 127 px lower."""
    im = Image.open(PC).convert('RGB')
    on, off = crop_round(im, 208, 363, 13), crop_round(im, 513, 363, 13)   # the reach row's radios
    scenes = im.crop((126, 432, 1154, 528))                                  # Scenes' head and New scenes first
    d = ImageDraw.Draw(im)
    d.rectangle((126, 418, 1153, 672), fill=PAPER)
    im.paste(scenes, (126, 432 + 127))
    tx = 198
    d.text((tx, 446), LABEL, font=font(17), fill=INK, anchor='ls')
    d.text((tx, 465), HINT, font=font(13), fill=INK_QUIET, anchor='ls')
    x, ry = tx, 480
    for label, chosen in radios:
        im.paste(on if chosen else off, (x + 10 - 13, ry + 10 - 13))
        d.text((x + 30, ry + 17), label, font=font(18), fill=INK, anchor='ls')
        x += 30 + round(font(18).getlength(label)) + 56
    d.text((tx, 526), NOTE, font=font(13), fill=INK_QUIET, anchor='ls')
    band(im, caption, note).save(os.path.join(OUT, name))


def phone(name, radios, note_text, caption, note):
    """OptionsView.DrawTweaksPhone at the dump's scale (K px a canvas unit): Battle's head across both columns, the row
    at the top of the right column, Scenes under it."""
    k = 56 / 36
    im = Image.open(PHONE).convert('RGB')
    on, off = crop_round(im, 168, 461, 19), crop_round(im, 168, 516.5, 19)  # the reach radios, one under another
    head_rule = im.getpixel((500, 210))
    scenes = im.crop((905, 172, 1726, 330))
    d = ImageDraw.Draw(im)
    d.rectangle((905, 168, 1726, 662), fill=PAPER)
    d.rectangle((900, 168, 907, 226), fill=PAPER)                           # the column rule starts under the head
    d.rectangle((104, 209, 1703, 210), fill=head_rule)                      # the head's line, two rows as drawn
    x0, x1 = 924 + 32 * k, 1703
    d.text((x0, 260), LABEL, font=font(17 * k), fill=INK, anchor='ls')
    last = wrap(d, HINT, x0, 289, x1 - x0, 13 * k, 13 * 1.45 * k, INK_QUIET)
    ry = last + 14 * k
    widths = [30 * k + font(18 * k).getlength(label) for label, _ in radios]
    gap = max(30 * k, (x1 - x0 - sum(widths)) / (len(radios) - 1))
    x = x0
    for (label, chosen), w in zip(radios, widths):
        im.paste(on if chosen else off, (round(x + 10 * k - 19), round(ry + 10 * k - 19)))
        d.text((x + 30 * k, ry + 17 * k), label, font=font(18 * k), fill=INK, anchor='ls')
        x += w + gap
    last = wrap(d, note_text, x0, ry + 50 * k, x1 - x0, 13 * k, 13 * 1.45 * k, INK_QUIET)
    top = last + 32 * k                                                     # Scenes' section, its head line at top + 22
    im.paste(scenes, (905, round(top + 22 * k - 210 + 172)))
    band(im, caption, note, 1.4).save(os.path.join(OUT, name))


pc('N1.png', [('Never', False), (ORIGINAL, True), ('2 in 3', False), ('Always', False)],
   'N1 — the chance from low to high',
   "Tweaks → Battle: under Chaos and Nikkou; the original's 1 in 3 is the default. Scenes moves down.")
pc('N2.png', [(ORIGINAL, True), ('Never', False), ('2 in 3', False), ('Always', False)],
   "N2 — the original's first, as in the row above",
   "Tweaks → Battle: under Chaos and Nikkou; the original's 1 in 3 is the default. Scenes moves down.")
phone('NP.png', [('Never', False), ('1 in 3', True), ('2 in 3', False), ('Always', False)],
      "1 in 3 is the original's. " + NOTE,
      'NP — the phone held landscape',
      "Battle across both columns; the row at the right column's top, Scenes under it; the radios' words short.")
