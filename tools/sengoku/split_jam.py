"""Splits alice-tools' code dump of Sengoku Rance (`alice ain dump -c`) into one file per original
source, as docs/sengoku/ cites it: every source ends with a line `EOF <path>`, so the lines before
it go to `<out>/<path>.jam`.

Usage: python -I tools/sengoku/split_jam.py <code.jam> <out dir>
"""
import os
import sys


def main(src: str, out: str) -> None:
    lines: list[str] = []
    files = 0
    with open(src, encoding="utf-8", errors="replace") as dump:
        for line in dump:
            if not line.startswith("EOF "):
                lines.append(line)
                continue
            path = os.path.join(out, line[4:].strip().replace("\\", "/") + ".jam")
            os.makedirs(os.path.dirname(path), exist_ok=True)
            with open(path, "w", encoding="utf-8") as part:
                part.writelines(lines)
            lines = []
            files += 1
    print(f"{files} files; {len(lines)} lines after the last EOF")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
