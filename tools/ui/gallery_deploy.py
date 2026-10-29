"""Copies the screen gallery's current pictures as WebP, with its page, into a new folder for a website.

The gallery (tools/ui/gallery.ps1, docs/ui/gallery.md) is read, never written. Its index.html names every picture it
shows, <pass>/current/<id>.png; each becomes <pass>/<id>.webp in the output folder — lossy at the quality given or
lossless, whichever is smaller — and the page is written again for them: the pictures in WebP (the page already loads
them as they scroll into view, its own sizes theirs), and nothing of the former pictures, which are not copied (the
changed and new badges, the viewer's data on what changed, the passes' "old" marks). tools/ui/gallery-deploy.ps1 runs it.

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
IMG = '<img loading=lazy src='
INTRO = re.compile(r'(<h1>Screen gallery</h1>\s*)<p>.*?</p>', re.S)


def encode(job):
    """Writes one picture as WebP; returns both files' sizes in bytes and whether it is lossless."""
    source, target, quality = job
    with Image.open(source) as image:
        image.load()
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
    return os.path.getsize(source), best.tell(), best is exact


def rewrite(html):
    """The gallery's page for the WebP copy: the pictures decoded off the page's thread, no trace of the former ones."""
    html = html.replace(IMG, '<img loading=lazy decoding=async src=')
    html = PICTURE.sub(lambda m: f'{m[1]}="{m[2]}/{m[3]}.webp"', html)
    # A changed entry's badge links its former picture; a new one's, a tab's count and a pass's "old" mark are text.
    html = re.sub(r' ?<a class=changed href="[^"]*"[^>]*>.*?</a>', '', html)
    html = re.sub(r' ?<b class=(?:changed|new|stale)>[^<]*</b>', '', html)
    html = re.sub(r'<p class=stale>.*?</p>', '', html, flags=re.S)
    html = re.sub(r', \d+ changed or new</small>', '</small>', html)
    # The viewer's data on what changed: without them it offers no former picture and the bar no filter of changes.
    html = re.sub(r' data-(?:since|differing|box)="[^"]*"', '', html)
    return INTRO.sub(r'\1<p>The port&rsquo;s own screens, shown from kept saves. A click opens a picture in the viewer: '
                     r'&larr;/&rarr; the screens, &uarr;/&darr; the same screen in the other tabs.</p>', html, count=1)


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
    if IMG not in html:
        sys.exit(f'{index} is older than this script: tools/ui/gallery.ps1 -IndexOnly writes it again.')
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

    page = rewrite(html)
    # Every picture must now be a copied one: a form of the page this script does not know would leave a PNG behind.
    left = sorted(set(re.findall(r'"[^"]*(?:/current/|/previous/)[^"]*"|"[^"]*\.png"', page)))
    if left:
        sys.exit('The gallery page links pictures this script did not copy:\n  ' + '\n  '.join(left[:10]))
    (args.out / 'index.html').write_text(page, encoding='utf-8')

    png = sum(result[0] for result in results)
    webp = sum(result[1] for result in results)
    exact = sum(result[2] for result in results)
    print(f'{png / 2**20:.0f} MB of PNG, {webp / 2**20:.0f} MB of WebP ({100 * webp / png:.0f}%; {exact} lossless): {args.out}')


if __name__ == '__main__':
    main()
