"""Usage profile of every scenario variable in a decompiled System 3.9 scenario.

For each VARnnnn: how often it is assigned, tested in conditions and read elsewhere, in which
files, and which literal values it is assigned. Output is TSV, one row per variable.

    python varusage.py <decompiled scenario dir> [-o out.tsv]
"""
import argparse
import collections
import re
import sys
from pathlib import Path

VAR = re.compile(r"\bVAR(\d{4})\b")
# `!VAR0001 : expr!` or compound `!VAR0001 +: expr!`
ASSIGN = re.compile(r"^!VAR(\d{4})\s*([-+*/%&|^]?):\s*(.*)!$")
LITERAL = re.compile(r"^\d+$")


def strip_text(line: str) -> str:
    """Drop quoted message/string literals so their contents are never scanned."""
    return re.sub(r"'[^']*'|\"[^\"]*\"", "", line)


def profile(scenario: Path):
    writes = collections.Counter()
    conds = collections.Counter()
    reads = collections.Counter()
    files = collections.defaultdict(collections.Counter)
    values = collections.defaultdict(collections.Counter)

    for path in sorted(scenario.glob("*.ADV")):
        name = path.stem
        for raw in path.read_text(encoding="utf-8").splitlines():
            line = strip_text(raw.strip())
            if "VAR" not in line:
                continue
            m = ASSIGN.match(line)
            rest = line
            if m:
                var = m.group(1)
                writes[var] += 1
                files[var][name] += 1
                rhs = m.group(3).strip()
                if not m.group(2) and LITERAL.match(rhs):
                    values[var][int(rhs)] += 1
                rest = rhs
            target = conds if line.startswith("{") else reads
            for var in VAR.findall(rest):
                target[var] += 1
                files[var][name] += 1
    return writes, conds, reads, files, values


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("scenario", type=Path)
    ap.add_argument("-o", "--output", type=Path)
    args = ap.parse_args()

    names = (args.scenario / "variables.txt").read_text(encoding="utf-8").split()
    writes, conds, reads, files, values = profile(args.scenario)

    out = open(args.output, "w", encoding="utf-8", newline="\n") if args.output else sys.stdout
    out.write("var\twrites\tconds\treads\tnfiles\tfiles\tliteral_values\n")
    for index in range(len(names)):
        var = f"{index:04d}"
        top_files = ", ".join(f"{f}:{n}" for f, n in files[var].most_common(8))
        vals = values[var]
        shown = ", ".join(str(v) for v in sorted(vals)[:12]) + (" ..." if len(vals) > 12 else "")
        out.write(f"VAR{var}\t{writes[var]}\t{conds[var]}\t{reads[var]}\t{len(files[var])}\t{top_files}\t{shown}\n")
    if out is not sys.stdout:
        out.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
