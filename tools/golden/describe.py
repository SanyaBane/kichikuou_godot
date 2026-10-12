"""Summarise golden captures (docs/golden-tests.md): what each recorded and how it ended.

    python tools/golden/describe.py <capture.golden> [...]

Prints the root and its group, the week / story stage / gold / province at entry and exit, the
hook calls, the event counts and the mouse presses. For a battle also the parties with troops/HP
before and after, the battle variables and the FIGT_SEL commands, so the owner can check a capture
against what was played.
"""
import csv
import re
import sys
from collections import Counter
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]

EVENTS = ("rnd", "key", "wait", "menu", "music", "pick", "mouse", "cursor", "cdpos", "date", "timer", "call", "jump")


def load_names():
    with open(REPO / "docs" / "variables.tsv", encoding="utf-8") as f:
        return {(r["name"].strip() or r["var"]): int(r["var"][3:]) for r in csv.DictReader(f, delimiter="\t")}


def load_fields():
    text = (REPO / "src/Kichikuou.Original/State/VariableCatalog.g.cs").read_text(encoding="utf-8")
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
        elif t[0] in ("golden", "seed", "root", "group", "end"):
            header.append(line)
        else:
            events.append(t)
    return header, dumps, events


def describe(path, names, fields):
    header, dumps, events = parse(path)
    group = next((h.split(" ")[1] for h in header if h.startswith("group ")), "battle")

    def v(d, name):
        return d["v"].get(names[name], 0)

    def f(d, table, field, rec):
        page, off = fields[(table, field)]
        return d["p"].get((page, off + rec), 0)

    def s(d, n):
        return d["s"].get(n, "")

    print(f"== {path.name}")
    print("\n".join(header))
    if not any(h.startswith("end ") for h in header):
        print("--- no end record: incomplete capture")
    for tag in ("enter", "exit"):
        d = dumps.get(tag)
        if d is None:
            print(f"--- no {tag} dump")
            continue
        prov = v(d, "Prov_Cur")
        name = s(d, v(d, "Prov_StrBase") + prov - 1) if prov else ""
        owner = f(d, "Provs", "Owner", prov) if prov and ("Provs", "Owner") in fields else "-"
        print(f"--- {tag}: week {v(d, 'Week')}, story stage {v(d, 'StoryStage')}, gold {v(d, 'Gold')}, "
              f"province {prov} '{name}' owner {owner}")
        if group != "battle":
            continue
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
    if group == "battle":
        print("--- FIGT_SEL (command, argument):", [(int(e[1]), int(e[2])) for e in events if e[0] == "pick"])
    print("--- hooks:", [f"{e[1]} at {e[2]}" for e in events if e[0] == "call"])
    counts = Counter(e[0] for e in events if e[0] in EVENTS)
    polls = sum(int(e[4]) for e in events if e[0] == "mouse")
    if polls:
        counts["mouse polls"] = polls
    print("--- events:", dict(counts))
    # IM stores the button state in RetVal: 16 = left button, 32 = right button (xsystem35 event.c).
    presses, last = [], 0
    for e in events:
        if e[0] == "mouse":
            button = int(e[1])
            if button and not last:
                presses.append(f"{button}@({e[2]},{e[3]}) {e[5]}")
            last = button
    if presses:
        print(f"--- mouse presses ({len(presses)}):", ", ".join(presses[:30]), "…" if len(presses) > 30 else "")


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    names, fields = load_names(), load_fields()
    for i, arg in enumerate(sys.argv[1:]):
        if i:
            print()
        describe(Path(arg), names, fields)


if __name__ == "__main__":
    main()
