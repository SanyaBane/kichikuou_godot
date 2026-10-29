"""Copies the screen gallery's current pictures as WebP, with its page, into a new folder for a website.

The gallery (tools/ui/gallery.ps1, docs/ui/gallery.md) is read, never written. Its index.html names every picture it
shows, <pass>/current/<id>.png; each becomes <pass>/<id>.webp in the output folder — lossy at the quality given or
lossless, whichever is smaller — and the page is written again for them: the pictures in WebP, loaded as they scroll
into view, and nothing of the former pictures, which are not copied (the changed and new badges, the passes' "old"
marks). tools/ui/gallery-deploy.ps1 runs it.

Usage: gallery_deploy.py <gallery> <out> [--quality 85]
"""
import argparse
import io
import os
import re
import sys
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

from PIL import Image

# A picture of the gallery's page: an <img>'s src or the link around it.
PICTURE = re.compile(r'(src|href)="([\w-]+)/current/([^"/]+)\.png"')
IMG = re.compile(r'<img src="([\w-]+)/current/([^"/]+)\.png"')
INTRO = re.compile(r'(<h1>Screen gallery</h1>\s*)<p>.*?</p>', re.S)


def encode(job):
    """Writes one picture as WebP; returns its size in pixels, both files' sizes in bytes and whether it is lossless."""
    source, target, quality = job
    with Image.open(source) as image:
        image.load()
        size = image.size
        alpha = image.mode in ('RGBA', 'LA', 'PA') or 'transparency' in image.info
        image = image.convert('RGBA' if alpha else 'RGB')
        # The dumps are opaque: an alpha channel of 255 alone would only cost bytes.
        if alpha and image.getchannel('A').getextrema() == (255, 255):
            image = image.convert('RGB')
        lossy, exact = io.BytesIO(), io.BytesIO()
        image.save(lossy, format='WEBP', quality=quality, method=6)
        # The screens drawn on the original's 256-colour pictures, and the flat ones, come out smaller without loss
        # (2026-10-29: 121 of the 127 whose lossy copy kept half the PNG or more). quality is the effort here.
        image.save(exact, format='WEBP', lossless=True, quality=50, method=4)
    best = min(lossy, exact, key=lambda data: data.tell())
    target.write_bytes(best.getvalue())
    return size, os.path.getsize(source), best.tell(), best is exact


def rewrite(html, sizes):
    """The gallery's page for the WebP copy: lazily loaded pictures, no trace of the former ones."""
    def img(match):
        width, height = sizes[(match[1], match[2])]
        return f'<img loading=lazy decoding=async width={width} height={height} src="{match[1]}/{match[2]}.webp"'

    html = IMG.sub(img, html)
    html = PICTURE.sub(lambda m: f'{m[1]}="{m[2]}/{m[3]}.webp"', html)
    # A changed entry's badge links its former picture; a new one's, a tab's count and a pass's "old" mark are text.
    html = re.sub(r' ?<a class=changed href="[^"]*"[^>]*>.*?</a>', '', html)
    html = re.sub(r' ?<b class=(?:changed|new|stale)>[^<]*</b>', '', html)
    html = re.sub(r'<p class=stale>.*?</p>', '', html, flags=re.S)
    html = re.sub(r', \d+ changed or new</small>', '</small>', html)
    html = INTRO.sub(r'\1<p>The port&rsquo;s own screens, shown from kept saves. A click opens a picture at full size.</p>',
                     html, count=1)
    # Width and height reserve each picture's place before it loads; the width the grid gives sets the height.
    return html.replace('</style>', '  figure img { height: auto; }\n</style>', 1)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('gallery', type=Path)
    parser.add_argument('out', type=Path)
    parser.add_argument('--quality', type=int, default=85)
    args = parser.parse_args()

    index = args.gallery / 'index.html'
    if not index.is_file():
        sys.exit(f'No gallery page: {index} (tools/ui/gallery.ps1 writes it).')
    html = index.read_text(encoding='utf-8-sig')
    pictures = sorted({(m[2], m[3]) for m in PICTURE.finditer(html)})
    if not pictures:
        sys.exit(f'{index} shows no picture.')
    sources = {key: args.gallery / key[0] / 'current' / f'{key[1]}.png' for key in pictures}
    missing = [str(path) for path in sources.values() if not path.is_file()]
    if missing:
        sys.exit('The gallery page shows pictures that are not there:\n  ' + '\n  '.join(missing))
    if args.out.exists() and any(args.out.iterdir()):
        sys.exit(f'{args.out} is not empty.')

    for name in {key[0] for key in pictures}:
        (args.out / name).mkdir(parents=True, exist_ok=True)
    jobs = [(sources[key], args.out / key[0] / f'{key[1]}.webp', args.quality) for key in pictures]
    print(f'{len(jobs)} pictures to WebP at quality {args.quality} ...', flush=True)
    with ProcessPoolExecutor() as pool:
        results = list(pool.map(encode, jobs, chunksize=4))

    page = rewrite(html, {key: result[0] for key, result in zip(pictures, results)})
    # Every picture must now be a copied one: a form of the page this script does not know would leave a PNG behind.
    left = sorted(set(re.findall(r'"[^"]*(?:/current/|/previous/)[^"]*"|"[^"]*\.png"', page)))
    if left:
        sys.exit('The gallery page links pictures this script did not copy:\n  ' + '\n  '.join(left[:10]))
    (args.out / 'index.html').write_text(page, encoding='utf-8')

    png = sum(result[1] for result in results)
    webp = sum(result[2] for result in results)
    exact = sum(result[3] for result in results)
    print(f'{png / 2**20:.0f} MB of PNG, {webp / 2**20:.0f} MB of WebP ({100 * webp / png:.0f}%; {exact} lossless): {args.out}')


if __name__ == '__main__':
    main()
