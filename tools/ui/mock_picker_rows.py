"""Sheets comparing the upright phone's picker grids at their row height now and lower (owner, 2026-11-04: Harem's and
Treasure's rows 30 % lower, more of them in view). The pictures are the screen gallery's upright dumps of the game itself,
dumped once as it is and once each with the rows lowered (PhonePickersPortrait.HaremRow / TreasureRow changed for the dump
alone): side by side per entry, each labelled.

Usage: python mock_picker_rows.py <out dir> <label>=<phone-upright folder> ... -- <entry> ...
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONT = ROOT / 'godot' / 'fonts' / 'Cinzel-VariableFont_wght.ttf'
BACK = (24, 20, 18)
NAME = (232, 199, 122)


def main():
    args = sys.argv[1:]
    out = Path(args[0])
    split = args.index('--')
    columns = [a.split('=', 1) for a in args[1:split]]
    entries = args[split + 1:]
    font = ImageFont.truetype(str(FONT), 28)
    font.set_variation_by_axes([700])
    out.mkdir(parents=True, exist_ok=True)
    for entry in entries:
        pictures = [(label, Image.open(Path(folder) / f'{entry}.png').convert('RGB')) for label, folder in columns]
        w, h = pictures[0][1].size
        head, gap = 50, 20
        sheet = Image.new('RGB', (len(pictures) * (w + gap) + gap, h + head + gap), BACK)
        d = ImageDraw.Draw(sheet)
        for i, (label, img) in enumerate(pictures):
            x = gap + i * (w + gap)
            d.text((x + w / 2, head / 2), label, font=font, fill=NAME, anchor='mm')
            sheet.paste(img, (x, head))
        path = out / f'picker-rows-{entry}.png'
        sheet.save(path)
        print(path)


if __name__ == '__main__':
    main()
