r"""Mock-ups of the import screen's note when the chosen copy has no music (docs/backlog.md, "Release", the import's open
ends; 2026-11-10): the music's playlist `_inmm.ini` left the version check, so a copy without `bgm\` imports and plays
silent — the screen says so once, at the import, and waits for Play instead of opening the title by itself.

Drawn over the screen gallery's dumps (docs/ui/gallery.md) `import-done` (the done phase) and `import-choose` (its red
button, relabelled). From the repository's root (Pillow; the gallery's dumps; BIZ UDGothic, the default font):
  python -I tools/ui/mock_no_music.py <gallery pc/current dir> <out dir> < /dev/null
Round 1: A the note under the Play step, B the music a row of its own between the scenario and Play, C B's partial
case (some tracks missing). Everything in the 1280x800 screen's pixels, plus a caption band below.
"""
import sys, os
from PIL import Image, ImageDraw, ImageFont

DUMPS, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
W, H, CAPTION = 1280, 800, 70

# UiPalette's settings part (lacquer), as tools/ui/mock_import.py has them
INK, INK_QUIET, INK_GREY = (40, 28, 18), (100, 80, 56), (150, 132, 104)
VERMILION_INK, VERMILION, SEAL_TEXT = (150, 52, 30), (170, 40, 28), (246, 230, 206)
STEP_X, TEXT_X, TEXT_RIGHT = 316, 364, 964

GOTHIC = REPO + '/godot/fonts/BIZUDGothic-Regular.ttf'
CINZEL = REPO + '/godot/fonts/Cinzel-VariableFont_wght.ttf'


def gothic(size):
    return ImageFont.truetype(GOTHIC, size)


def cinzel(size, weight=700):
    f = ImageFont.truetype(CINZEL, size)
    f.set_variation_by_axes([weight])
    return f


def load(name):
    return Image.open(os.path.join(DUMPS, name + '.png')).convert('RGB')


def paper_colour(img):
    return img.getpixel((640, 560))


def clear(img, top, bottom):
    ImageDraw.Draw(img).rectangle((290, top, 990, bottom), fill=paper_colour(img))


def wrap(d, text, x, baseline, width, size, colour):
    f = gothic(size)
    line = ''
    for word in text.split(' '):
        tried = word if not line else line + ' ' + word
        if line and d.textlength(tried, font=f) > width:
            d.text((x, baseline), line, font=f, fill=colour, anchor='ls')
            baseline += round(size * 1.45)
            line = word
        else:
            line = tried
    if line:
        d.text((x, baseline), line, font=f, fill=colour, anchor='ls')
        baseline += round(size * 1.45)
    return baseline - size


def step(d, y, mark, title, state, body=None):
    """ImportScreen.Step: the seal (a numeral, or the music's note), the title, the body wrapped under it."""
    seal = (STEP_X + 15, y + 13)
    colour = {'now': INK, 'warn': VERMILION_INK, 'next': INK_GREY}[state]
    if state == 'now':
        d.ellipse((seal[0] - 15, seal[1] - 15, seal[0] + 15, seal[1] + 15), fill=VERMILION)
    ring = VERMILION_INK if state in ('now', 'warn') else INK_GREY
    d.ellipse((seal[0] - 14, seal[1] - 14, seal[0] + 14, seal[1] + 14), outline=ring, width=2)
    d.text(seal, mark, font=gothic(16), fill=SEAL_TEXT if state == 'now' else colour, anchor='mm')
    d.text((TEXT_X, y + 20), title, font=gothic(19), fill=colour, anchor='ls')
    if body is None:
        return y + 40
    return wrap(d, body, TEXT_X, y + 50, TEXT_RIGHT - TEXT_X, 15, INK_QUIET)


def red_button(img, label):
    """The Import button of import-choose, its word painted over row by row and the label written in its place."""
    box = (540, 588, 740, 636)
    button = load('import-choose').crop(box)
    px = button.load()
    for yy in range(8, button.height - 8):
        fill = px[14, yy]
        for xx in range(14, button.width - 14):
            px[xx, yy] = fill
    d = ImageDraw.Draw(button)
    d.text((button.width / 2, button.height / 2 + 1), label, font=cinzel(16), fill=SEAL_TEXT, anchor='mm')
    img.paste(button, box[:2])


def note(d, top, title, body):
    """A margin note: a vermilion rule at its left, the title in vermilion ink, the body quiet."""
    x = TEXT_X + 14
    d.text((x, top + 18), title, font=gothic(17), fill=VERMILION_INK, anchor='ls')
    bottom = wrap(d, body, x, top + 44, TEXT_RIGHT - x, 15, INK_QUIET)
    d.line((TEXT_X, top, TEXT_X, bottom + 6), fill=VERMILION, width=3)


NONE_BODY = (r'No bgm\kichiku_2.mp3 … kichiku_29.mp3 in the folder: the game plays without music. '
             'Copy the bgm folder of your copy here to hear it.')
SOME_TITLE = '3 of the 28 tracks are missing'
SOME_BODY = (r'bgm\kichiku_7.mp3, kichiku_12.mp3 and kichiku_20.mp3 are not in the folder: the game plays the others '
             'and is silent where these would play.')


def a_note():
    img = load('import-done')
    d = ImageDraw.Draw(img)
    clear(img, 480, 655)
    note(d, 486, 'No music found', NONE_BODY)
    red_button(img, 'Play')
    return img


def b_row(title, body):
    img = load('import-done')
    d = ImageDraw.Draw(img)
    clear(img, 434, 655)
    y = step(d, 438, '♪', title, 'warn', body)
    step(d, y + 16, '三', 'Play', 'now')
    red_button(img, 'Play')
    return img


SHEETS = [
    ('A', a_note(), 'A: the note under Play',
     'The steps as now, Play sealed; under it a margin note (a vermilion rule). The screen waits for the red Play.'),
    ('B', b_row('No music found', NONE_BODY), 'B: the music a row of its own',
     'Between the scenario and Play a row with ♪ in its ring, in vermilion ink. The screen waits for the red Play.'),
    ('C', b_row(SOME_TITLE, SOME_BODY), 'C: B when some tracks are missing',
     'The same row naming the missing files (A would say the same in its note).'),
]

for key, img, title, line in SHEETS:
    sheet = Image.new('RGB', (W, H + CAPTION), (24, 20, 16))
    sheet.paste(img, (0, 0))
    d = ImageDraw.Draw(sheet)
    d.text((16, H + 26), title, font=gothic(20), fill=(236, 226, 204), anchor='ls')
    d.text((16, H + 54), line, font=gothic(15), fill=(176, 160, 130), anchor='ls')
    path = os.path.join(OUT, f'no-music-{key}.png')
    sheet.save(path)
    print(path)
