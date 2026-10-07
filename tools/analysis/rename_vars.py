"""Copy a decompiled scenario, replacing VARnnnn with names from docs/variables.tsv.

Only variables that have a name in the dictionary are renamed; the rest stay VARnnnn. Quoted
message and string literals are left untouched. The output is for reading only — it is never
recompiled, and it lives outside the repository like all extracted data.

    python rename_vars.py <decompiled scenario dir> <output dir> [--dict docs/variables.tsv]
"""
import argparse
import csv
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
VAR = re.compile(r"\bVAR(\d{4})\b")
QUOTED = re.compile(r"('[^']*'|\"[^\"]*\")")


IDENT = re.compile(r"^[A-Za-z][A-Za-z0-9_]*$")


def load_names(path: Path) -> dict[str, str]:
    """Read var -> name from the dictionary, rejecting invalid or duplicate names."""
    with open(path, encoding="utf-8", newline="") as f:
        rows = [row for row in csv.DictReader(f, delimiter="\t") if row["name"].strip()]
    names, owner, errors = {}, {}, []
    for row in rows:
        var, name = row["var"], row["name"].strip()
        if not IDENT.match(name):
            errors.append(f"{var}: invalid name {name!r}")
        elif name in owner:
            errors.append(f"{var}: name {name!r} already used by {owner[name]}")
        else:
            owner[name] = var
            names[var] = name
    if errors:
        sys.exit("dictionary errors:\n  " + "\n  ".join(errors))
    return names


def rename_line(line: str, names: dict[str, str]) -> str:
    parts = QUOTED.split(line)
    for i in range(0, len(parts), 2):  # even indices are outside quotes
        parts[i] = VAR.sub(lambda m: names.get(m.group(0), m.group(0)), parts[i])
    return "".join(parts)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("scenario", type=Path)
    ap.add_argument("output", type=Path)
    ap.add_argument("--dict", type=Path, default=REPO / "docs" / "variables.tsv")
    args = ap.parse_args()

    if args.output.resolve().is_relative_to(REPO):
        sys.exit("refusing to write extracted data inside the repository")
    names = load_names(args.dict)
    args.output.mkdir(parents=True, exist_ok=True)
    count = 0
    for path in sorted(args.scenario.glob("*.ADV")):
        text = path.read_text(encoding="utf-8")
        renamed = "\n".join(rename_line(l, names) for l in text.split("\n"))
        (args.output / path.name).write_text(renamed, encoding="utf-8", newline="")
        count += 1
    print(f"{count} files, {len(names)} named variables -> {args.output}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
