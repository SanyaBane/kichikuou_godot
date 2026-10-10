"""Compact listings and C# drafts of a range of a decompiled scenario file, for hand ports.

The command files and scenes are mostly text runs between window stubs (`\\L_x:` calling a label
whose body is `!Ui_WaoOp : n! %#WAO_SUB.ADV: \\0:`). This tool finds a file's stubs by their
bodies and reads a line range as:

    --list   a compact listing: statements with their line numbers, each text run as T<line>
             (its first text command, an `R` included; `ZH 1:` skipped; X/H/HH/MP noted in
             brackets), each stub call as W<op>, each menu item as M<line>->label;
    default  a C# draft in the idioms of `CommandFile`: `SpeakAsync(window, runs…)` for a speaker
             window followed by runs each closed by op 1 (`close: false` when no op 0 follows),
             `WaoAsync(op)` for the other stubs (the label named on its first use), `State.CallArg
             = n;`, `Script.MapCallAsync(n)` for `!CallArg : n! %#M.ADV:`, `return Outcome.Done;`
             for `%0:`; anything else as a `// ??? line: statement` comment to port by hand
             (menus, conditions, jumps, record writes).

The draft is a starting point: read every label yourself, check it against the listing, and let
the differential test (the file run by SceneRunner) judge the port.

    python -I port_draft.py <decompiled scenario dir> <file> <from> <to> [--list]
"""
import argparse
import re
import sys
from pathlib import Path

SPEAKERS = {111, 112, 113, 121, 122, 123}
TEXT = re.compile(r"^(X|H|HH|MP) ")


def stubs(lines):
    """Label → window op for every `*L_x: !Ui_WaoOp : n! %#WAO_SUB.ADV: \\0:` of the file."""
    found = {}
    for i, line in enumerate(lines[:-3]):
        m = re.match(r"^\*(L_\w+):$", line.strip())
        op = re.match(r"^!Ui_WaoOp : (\d+)!$", lines[i + 1].strip())
        if m and op and lines[i + 2].strip() == "%#WAO_SUB.ADV:" and lines[i + 3].strip() == "\\0:":
            found[m.group(1)] = int(op.group(1))
    return found


def tokens(lines, lo, hi, wao):
    """The range as tokens: ['T', line, extras, indent], ['W', op, line, indent], ['M', line, label, indent], ['S', text, line, indent]."""
    out, run, i = [], None, lo
    while i <= hi:
        raw = lines[i - 1]
        s = raw.strip()
        ind = len(raw) - len(raw.lstrip("\t"))
        if s.startswith("'") or s == "R" or TEXT.match(s):
            if run is None:
                run = ["T", i, [], ind]
                out.append(run)
            if not (s.startswith("'") or s == "R"):
                run[2].append(s.rstrip(":"))
            i += 1
            continue
        if s == "ZH 1:":
            i += 1
            continue
        run = None
        m = re.match(r"^\$(L_\w+)\$$", s)
        if m:
            out.append(["M", i, m.group(1), ind])
            while lines[i].strip() != "$":      # the item's text, up to its closing `$`
                i += 1
            i += 2
            continue
        m = re.match(r"^\\(L_\w+):$", s)
        if m and m.group(1) in wao:
            out.append(["W", wao[m.group(1)], i, ind])
        elif s:
            out.append(["S", s, i, ind])
        i += 1
    return out


def listing(toks):
    rows = []
    for t in toks:
        if t[0] == "T":
            text, ind = f"T{t[1]}" + (f"[{', '.join(t[2])}]" if t[2] else ""), t[3]
        elif t[0] == "W":
            text, ind = f"W{t[1]}", t[3]
        elif t[0] == "M":
            text, ind = f"M{t[1]}->{t[2]}", t[3]
        else:
            text, ind = (f"{t[2]}:{t[1]}", 0) if t[1].startswith("*L_") else (f"{t[2]}:{t[1]}", t[3])
        if rows and rows[-1][0] == ind and text[0] in "TW" and rows[-1][1][0] in "TW":
            rows[-1] = (ind, rows[-1][1] + " " + text)
        else:
            rows.append((ind, text))
    return "\n".join("  " * ind + text for ind, text in rows)


def draft(toks, names):
    out, seen = [], set()

    def emit(ind, code, comment=""):
        line = "    " * (ind + 1) + code
        out.append(line.ljust(72) + comment if comment else line)

    def first(op):
        if op in (0, 1) or op in seen:
            return ""
        seen.add(op)
        return f"// \\{names[op]}"

    k = 0
    while k < len(toks):
        t = toks[k]
        if t[0] == "W" and t[1] in SPEAKERS:
            runs, j = [], k + 1
            while j + 1 < len(toks) and toks[j][0] == "T" and not toks[j][2] and toks[j + 1][:2] == ["W", 1]:
                runs.append(toks[j][1])
                j += 2
            if runs:
                close = j < len(toks) and toks[j][:2] == ["W", 0]
                args = ", ".join(map(str, runs))
                emit(t[3], f"await SpeakAsync({t[1]}, {'' if close else 'close: false, '}{args});", first(t[1]))
                k = j + (1 if close else 0)
                continue
        if t[0] == "W":
            emit(t[3], f"await WaoAsync({t[1]});", first(t[1]))
        elif t[0] == "T":
            emit(t[3], f"await ValuesAsync({t[1]}, /* {'; '.join(t[2])} */);" if t[2] else f"await TextAsync({t[1]});")
        elif t[0] == "M":
            emit(t[3], f"// ??? {t[1]}: menu item -> {t[2]} (MenuItem({t[1]}))")
        else:
            s, nxt = t[1], toks[k + 1] if k + 1 < len(toks) else None
            m = re.match(r"^!CallArg : (\d+)!$", s)
            if m and nxt and nxt[0] == "S" and nxt[1] == "%#M.ADV:":
                emit(t[3], f"await Script.MapCallAsync({m.group(1)});")
                k += 2
                continue
            if m:
                emit(t[3], f"State.CallArg = {m.group(1)};")
            elif s == "%0:":
                emit(t[3], "return Outcome.Done;")
            elif s.startswith("*L_"):
                out.append(f"        // {t[2]}: {s}")
            else:
                emit(t[3], f"// ??? {t[2]}: {s}")
        k += 1
    return "\n".join(out)


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("scenario", type=Path, help="the decompiled scenario directory (scenario_named)")
    ap.add_argument("file", help="e.g. TJAPAN.ADV")
    ap.add_argument("lo", type=int)
    ap.add_argument("hi", type=int)
    ap.add_argument("--list", action="store_true", help="a compact listing instead of a C# draft")
    args = ap.parse_args()
    lines = (args.scenario / args.file).read_text(encoding="utf-8").split("\n")
    wao = stubs(lines)
    names = {}
    for label, op in wao.items():
        names.setdefault(op, label)
    toks = tokens(lines, args.lo, min(args.hi, len(lines)), wao)
    print(listing(toks) if args.list else draft(toks, names))


if __name__ == "__main__":
    main()
