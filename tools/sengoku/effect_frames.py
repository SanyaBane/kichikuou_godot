"""Extract Sengoku Rance's battle effects from the player's own game, as references for ours.

Each effect is a run of CGs in Rance7GA.ald (docs/sengoku/04-screen.md, section 2.4): this writes
every frame of the effects asked for as PNGs into <out>/<first CG>/NN.png, and a contact sheet of
every third frame over a dark grey, <out>/sheet-<first CG>.png. The output is the game's art: keep
it outside the repository (the scratchpad), never commit it.

    python -I tools/sengoku/effect_frames.py <alice.exe> <Rance7GA.ald> <out> 40000 40260 40360 [--frames 30]

alice.exe is alice-tools' (KICHIKUOU_ALICE_TOOLS_RELEASE_DIR in local.env), Rance7GA.ald is in
SENGOKU_RANCE_GAME_DIR. The sheets need Pillow.
"""
import argparse
import os
import subprocess

from PIL import Image


def extract(alice, archive, out, first, frames):
    folder = os.path.join(out, str(first))
    os.makedirs(folder, exist_ok=True)
    for frame in range(frames):
        # alice's index is the archive's 0-based link number: index n holds CG n + 1.
        subprocess.run([alice, 'ar', 'extract', '-i', str(first + frame - 1), '--image-format', 'png', '-f',
                        '-o', os.path.join(folder, f'{frame:02d}.png'), archive],
                       check=False, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return folder


def sheet(folder, path):
    frames = sorted(f for f in os.listdir(folder) if f.endswith('.png'))[::3]
    width, height = 400, 157
    out = Image.new('RGB', (width * 2, height * ((len(frames) + 1) // 2)), (48, 48, 48))
    for k, name in enumerate(frames):
        # The frames are opaque, black where nothing is: the game adds them over the scene (draw method 1).
        frame = Image.open(os.path.join(folder, name)).convert('RGB').resize((width, height))
        out.paste(frame, ((k % 2) * width, (k // 2) * height))
    out.save(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('alice')
    parser.add_argument('archive')
    parser.add_argument('out')
    parser.add_argument('first', type=int, nargs='+', help='the first CG of each effect')
    parser.add_argument('--frames', type=int, default=30)
    args = parser.parse_args()
    for first in args.first:
        folder = extract(args.alice, args.archive, args.out, first, args.frames)
        sheet(folder, os.path.join(args.out, f'sheet-{first}.png'))
        print(f'{first}: {len(os.listdir(folder))} frames')


if __name__ == '__main__':
    main()
