"""Summarise a golden capture (docs/golden-tests.md): what battle it is and how it ended.

    python tools/golden/describe.py <capture.golden>

Prints the province, the parties with troops/HP before and after, the battle variables, the
FIGT_SEL commands and the event counts, so the owner can check a capture against what was played.
"""
import csv
import re
import sys
from collections import Counter
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]


def load_names():
    with open(REPO / "docs" / "variables.tsv", encoding="utf-8") as f:
        return {(r["name"].strip() or r["var"]): int(r["var"][3:]) for r in csv.DictReader(f, delimiter="\t")}


def load_fields():
    text = (REPO / "src/Kichikuou.Core/State/VariableCatalog.g.cs").read_text(encoding="utf-8")
    fields = {}
    for m in re.finditer(r'new\((\d+), (\d+), (\d+), (\d+), (\d+), "(\w+)", "(\w+)"\)', text):
        _, page, off, _, _, table, field = m.groups()
        fields.setdefault((table, field), (int(page), int(off)))
    return fields


def unescape(s):
    return re.sub(r"\\x([0-9a-f]{2})", lambda m: chr(int(m.group(1), 16)), s).replace("\\\\", "\\")


def parse(path):
    dumps, events, header, cur = {}, [], [], None
    for line in path.read_text(encoding="utf-8").splitlines():
        t = line.split(" ")
        if t[0] == "mem":
            cur = {"v": {}, "p": {}, "s": {}}
            dumps.setdefault(t[1], cur)
        elif t[0] == "end-mem":
            cur = None
        elif cur is not None:
            if t[0] == "v":
                for i, x in enumerate(t[2:]):
                    cur["v"][int(t[1]) + i] = int(x)
            elif t[0] == "p":
                for i, x in enumerate(t[3:]):
                    cur["p"][(int(t[1]), int(t[2]) + i)] = int(x)
            elif t[0] == "str":
                cur["s"][int(t[1])] = unescape(line.split(" ", 2)[2])
        elif t[0] in ("golden", "seed", "root", "end"):
            header.append(line)
        else:
            events.append(t)
    return header, dumps, events


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    names, fields = load_names(), load_fields()
    header, dumps, events = parse(Path(sys.argv[1]))

    def v(d, name):
        return d["v"].get(names[name], 0)

    def f(d, table, field, rec):
        page, off = fields[(table, field)]
        return d["p"].get((page, off + rec), 0)

    def s(d, n):
        return d["s"].get(n, "")

    print("\n".join(header))
    for tag in ("enter", "exit"):
        d = dumps[tag]
        prov = v(d, "Prov_Cur")
        print(f"--- {tag}: week {v(d, 'Week')}, story stage {v(d, 'StoryStage')}, gold {v(d, 'Gold')}, "
              f"province {prov} '{s(d, v(d, 'Prov_StrBase') + prov - 1)}' owner {f(d, 'Provs', 'Owner', prov)}")
        print("   " + "  ".join(f"{k}={v(d, k)}" for k in (
            "Bt_Kind", "Bt_WallBonus", "Bt_TroopLimit", "Bt_IsDuel", "Bt_EnemyRetreatMode", "Bt_Result",
            "Bt_PlayerLosses", "Bt_EnemyLosses")))
        for i in range(1, 5):
            u = v(d, f"Bt_PlayerParty{i}")
            if u:
                base = v(d, "Unit_StrBase") + (u - 1) * 3
                print(f"   P{i} unit {u} '{s(d, base)}' ({s(d, base + 1)}): troops {f(d, 'Units', 'Troops', u)}/"
                      f"{f(d, 'Units', 'MaxTroops', u)}, HP {f(d, 'Units', 'HP', u)}/{f(d, 'Units', 'MaxHP', u)}, "
                      f"level {f(d, 'Units', 'Level', u)}, EXP {f(d, 'Units', 'Exp', u)}, status {f(d, 'Units', 'Status', u)}")
        for i in range(1, 5):
            c = v(d, f"Bt_EnemyParty{i}")
            if c:
                base = v(d, "Cmdr_StrBase") + (c - 1) * 3
                print(f"   E{i} commander {c} '{s(d, base)}' ({s(d, base + 1)}): troops {f(d, 'Cmdrs', 'Troops', c)}/"
                      f"{f(d, 'Cmdrs', 'MaxTroops', c)}, HP {f(d, 'Cmdrs', 'HP', c)}/{f(d, 'Cmdrs', 'MaxHP', c)}, "
                      f"status {f(d, 'Cmdrs', 'Status', c)}")
    print("--- FIGT_SEL (command, argument):", [(int(e[1]), int(e[2])) for e in events if e[0] == "pick"])
    print("--- hooks:", [e[1] for e in events if e[0] == "call"])
    print("--- events:", dict(Counter(e[0] for e in events if e[0] in
                                       ("rnd", "key", "wait", "menu", "music", "pick", "call", "jump"))))


if __name__ == "__main__":
    main()
