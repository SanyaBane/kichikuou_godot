"""Translate the dispatch body of FIGT_M.ADV (battle flavour text) into C#.

FIGT_M.ADV is ~10k lines, almost all of it dialogue windows chosen by nested `{cond: ...}`
blocks. Its control flow uses a handful of statement kinds only, so this tool translates it
mechanically and statement by statement into `Battle.Flavour.g.cs`; the result is a faithful
port of every condition, assignment, goto and `Y 4` draw, with each text run replaced by its
line number (`ScriptRef("FIGT_M.ADV", line)`) — no dialogue is copied; only the two names that
`MS` sets (:10099, :10101) pass through as string literals.

Hand-written in `Battle.Flavour.cs` (this tool only checks that the script still says what
that code assumes): the entry, lines 1-23 (`FlavourTextAsync`), and the window procedures
`L_0c4c1`..`L_0c576` (10306-10353), which call WAO_SUB.ADV.

    python tools/codegen/gen_figt_m.py <raw scenario dir>            # writes the .g.cs
    python tools/codegen/gen_figt_m.py <raw scenario dir> --check    # fails if it differs

`<raw scenario dir>` is the raw decompile (`_extract\\scenario`, variables as VARnnnn); the
C# names come from `GameState.g.cs`, so the output always matches the generated state.
"""
import argparse
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
STATE = REPO / "src" / "Kichikuou.Original" / "State" / "GameState.g.cs"
OUT = REPO / "src" / "Kichikuou.Original" / "Combat" / "Battle.Flavour.g.cs"
FILE = "FIGT_M.ADV"

# Generated range: L_000b5 (player side) .. the end of L_0c0fc (Sieg event).
FIRST, LAST = 24, 10305

# Hand-ported lines and the exact statements they must still contain (text excluded).
EXPECTED = {
    (1, 23): [
        "!VAR0621 : 0!", "{(VAR0618 = 1) * (VAR0619 = 2):", "!VAR0618 : 2!", "}", "Y 4, 2:",
        "{VAR0618 = 1:", "!VAR0000 : 1!", "}", "{VAR0618 = 2:", "!VAR0000 : 2!", "}",
        "{VAR0618 = 3:", "!VAR0000 : 1!", "}", "{VAR0618 = 4:", "!VAR0000 : 2!", "}",
        "{VAR0000 = 1:", "@L_000b5:", "}", "{VAR0000 = 2:", "@L_036f2:", "}",
    ],
    (10306, 10353): [
        "*L_0c4c1:", "!VAR0521 : 141!", "%#WAO_SUB.ADV:", "\\0:", "!VAR0521 : 241!", "%#WAO_SUB.ADV:", "\\0:",
        "*L_0c4dd:", "!VAR0521 : 142!", "%#WAO_SUB.ADV:", "\\0:", "!VAR0521 : 242!", "%#WAO_SUB.ADV:", "\\0:",
        "!VAR0521 : 143!", "%#WAO_SUB.ADV:", "\\0:", "!VAR0521 : 243!", "%#WAO_SUB.ADV:", "\\0:",
        "*L_0c515:", "!VAR0521 : 151!", "%#WAO_SUB.ADV:", "\\0:", "!VAR0521 : 251!", "%#WAO_SUB.ADV:", "\\0:",
        "*L_0c531:", "!VAR0521 : 152!", "%#WAO_SUB.ADV:", "\\0:", "!VAR0521 : 252!", "%#WAO_SUB.ADV:", "\\0:",
        "!VAR0521 : 153!", "%#WAO_SUB.ADV:", "\\0:", "!VAR0521 : 253!", "%#WAO_SUB.ADV:", "\\0:",
        "*L_0c569:", "!VAR0521 : 10!", "%#WAO_SUB.ADV:", "\\0:",
        "*L_0c576:", "!VAR0521 : 11!", "%#WAO_SUB.ADV:", "\\0:",
    ],
}

# Window procedures (hand-written in Battle.Flavour.cs).
OPEN = {"L_0c4c1": "FlavourWindow.Left", "L_0c4dd": "FlavourWindow.LeftAlt",
        "L_0c515": "FlavourWindow.Right", "L_0c531": "FlavourWindow.RightAlt"}
WAIT, CLOSE = "L_0c576", "L_0c569"

# Labelled regions that are not a unit/commander dispatch target: name and description.
NAMED = {
    "L_0ba24": ("FlavourSatellaVsCmdr91Async", "unit 29 vs commander 91, one-shot scene (sets <c>F_Satella_LineCamilla</c>)"),
    "L_0bb6d": ("FlavourSatellaVsCmdr92Async", "unit 29 vs commander 92, one-shot scene (sets <c>F_Satella_LineLei</c>)"),
    "L_0bc93": ("FlavourUnit30VsCmdr93Async", "unit 30 vs commander 93, one-shot scene (sets <c>F_Meglass_LineSieg</c>)"),
    "L_0bddd": ("FlavourFreakSceneAsync", "unit 20 vs commander 130 (advances <c>N_Freak_Stage</c>)"),
    "L_0c0fc": ("FlavourSiegEventAsync", "Rance vs commander 93 while <c>F_Sieg_State</c> is 2/3: Chaos changes hands"),
}

# Top-level dispatch blocks whose condition is not a plain `Unit_Cur = n` / `Cmdr_Cur = n`.
COMPOUND_NAMES = {
    "(VAR0154 = 48) * (VAR0730 = 1)": ("Unit48", "unit 48 when <c>Unit48_Identity</c> = 1"),
    "(VAR0156 = 31) * (VAR0518 = 0)": ("Cmdr31Stage0", "commander 31 at <c>StoryStage</c> 0"),
    "(VAR0156 = 58) * (VAR0518 = 0)": ("Cmdr58Stage0", "commander 58 at <c>StoryStage</c> 0"),
    "(VAR0156 > 190) * (VAR0156 < 200)": ("Cmdr191To199", "commanders 191-199"),
}

# Quirks of spec 02 §10 that live in the generated range: line -> comment appended to it.
Q13 = "QUIRK: Q13 — tests RetVal, which the key wait before it overwrites"
QUIRKS = {
    8326: "QUIRK: Q8 — `*` binds tighter: Mill state applies to slot 4 only (RPN in FIGT_M.SCO at 0xa07d)",
    8845: "QUIRK: Q18 — dead: Bt_MsgMode 2 always speaks from the enemy side",
    8870: Q13, 8879: Q13,
    5016: Q13, 5024: Q13, 5030: Q13, 5048: Q13, 5056: Q13, 5066: Q13,
    10113: "QUIRK: Q21 — Rance's previous item keeps him as its holder",
}

RECORD_ACCESSOR = {"UnitRecord": "Unit", "CmdrRecord": "Cmdr", "ItemRecord": "Item", "ProvRecord": "Prov"}


# ---------------------------------------------------------------------------------------------
# Variable names


def load_names() -> dict[int, str]:
    """VAR number -> C# accessor (`State.X` for globals, `Unit.X` etc. for record fields)."""
    # VAR0000 is the engine's ScriptState.RetVal, not in the generated state.
    names: dict[int, str] = {0: "State.RetVal"}
    record = None
    pending = None
    for line in STATE.read_text(encoding="utf-8").splitlines():
        m = re.match(r"^public sealed (?:partial )?class (\w+)", line)
        if m:
            record = RECORD_ACCESSOR.get(m.group(1), m.group(1)) if m.group(1) != "GameState" else None
            continue
        m = re.match(r"^\s*/// <summary>VAR(\d{4}) ", line)
        if m:
            pending = int(m.group(1))
            continue
        m = re.match(r"^\s*public (?:ref )?Sys3Int (\w+)", line)
        if m and pending is not None:
            if record is None:
                names.setdefault(pending, f"State.{m.group(1)}")
            elif record in RECORD_ACCESSOR.values():
                names[pending] = f"{record}.{m.group(1)}"
            pending = None
    return names


NAMES: dict[int, str] = {}


def var(token: str) -> str:
    n = int(token[3:])
    if n not in NAMES:
        sys.exit(f"no C# name for {token}")
    return NAMES[n]


# ---------------------------------------------------------------------------------------------
# Expressions


TOKEN = re.compile(r"\s*(VAR\d{4}|\d+|<=|>=|[-+*/()=<>\\])")


def tokenize(text: str) -> list[str]:
    out, pos = [], 0
    while pos < len(text):
        m = TOKEN.match(text, pos)
        if not m:
            sys.exit(f"cannot tokenize {text!r} at {pos}")
        out.append(m.group(1))
        pos = m.end()
    return out


@dataclass
class Expr:
    code: str
    boolean: bool  # value is a 0/1 comparison result (or an and/or of such)


class Parser:
    """Decompiled condition syntax: `+`/`-` below `*`/`/`, comparisons in parentheses."""

    CMP = {"=": "==", "\\": "!=", "<": "<", ">": ">", "<=": "<=", ">=": ">="}

    def __init__(self, text: str):
        self.toks = tokenize(text)
        self.i = 0
        self.text = text

    def peek(self):
        return self.toks[self.i] if self.i < len(self.toks) else None

    def take(self):
        t = self.toks[self.i]
        self.i += 1
        return t

    def parse(self) -> Expr:
        e = self.compare()
        if self.peek() is not None:
            sys.exit(f"trailing tokens in {self.text!r}")
        return e

    def compare(self) -> Expr:
        left = self.sum()
        if self.peek() in self.CMP:
            op = self.CMP[self.take()]
            right = self.sum()
            if left.boolean or right.boolean:
                sys.exit(f"comparison of a comparison in {self.text!r}")
            return Expr(f"{left.code} {op} {right.code}", True)
        return left

    def sum(self) -> Expr:
        e = self.product()
        while self.peek() in ("+", "-"):
            op = self.take()
            r = self.product()
            if e.boolean and r.boolean and op == "+":
                # Both sides are 0/1, so a saturating sum is nonzero iff either is: OR.
                e = Expr(f"{wrap_and(e.code)} || {wrap_and(r.code)}", True)
            elif not e.boolean and not r.boolean:
                e = Expr(f"{e.code} {op} {r.code}", False)
            else:
                sys.exit(f"mixed arithmetic in {self.text!r}")
        return e

    def product(self) -> Expr:
        e = self.atom()
        while self.peek() in ("*", "/"):
            op = self.take()
            r = self.atom()
            if e.boolean and r.boolean and op == "*":
                # Both sides are 0/1: the product is nonzero iff both are: AND.
                e = Expr(f"{wrap_or(e.code)} && {wrap_or(r.code)}", True)
            elif not e.boolean and not r.boolean:
                e = Expr(f"{e.code} {op} {r.code}", False)
            else:
                sys.exit(f"mixed arithmetic in {self.text!r}")
        return e

    def atom(self) -> Expr:
        t = self.take()
        if t == "(":
            e = self.compare()
            if self.take() != ")":
                sys.exit(f"unbalanced parentheses in {self.text!r}")
            return Expr(f"({e.code})" if not e.boolean or "||" in e.code or "&&" in e.code else e.code, e.boolean)
        if t.startswith("VAR"):
            return Expr(var(t), False)
        if t.isdigit():
            return Expr(t, False)
        sys.exit(f"unexpected {t!r} in {self.text!r}")


def wrap_and(code: str) -> str:
    """Parenthesise an AND that becomes an operand of OR (same meaning, explicit precedence)."""
    return f"({code})" if "&&" in code and not (code.startswith("(") and code.endswith(")")) else code


def wrap_or(code: str) -> str:
    return f"({code})" if "||" in code and not (code.startswith("(") and code.endswith(")")) else code


def condition(text: str) -> str:
    e = Parser(text).parse()
    return e.code if e.boolean else f"{e.code} != 0"


def value(text: str) -> str:
    e = Parser(text).parse()
    if e.boolean:
        sys.exit(f"comparison used as a value: {text!r}")
    return e.code


# ---------------------------------------------------------------------------------------------
# Statements


@dataclass
class Stmt:
    kind: str           # if, return, goto, call, ret, rnd, assign, text, ms, mm, newline
    line: int
    arg: str = ""
    arg2: str = ""
    body: list["Stmt"] = field(default_factory=list)
    end: int = 0        # last line of an if block


TEXT = re.compile(r"^'.*'$")


def parse_block(lines: list[str], i: int, stop: int) -> tuple[list[Stmt], int]:
    """Parses statements from 0-based index i until a closing `}` or `stop`; returns (stmts, next)."""
    out: list[Stmt] = []
    while i < stop:
        s = lines[i].strip()
        ln = i + 1
        if s == "}":
            return out, i
        if TEXT.match(s) or s == "R":
            j = i
            while j < stop and (TEXT.match(lines[j].strip()) or lines[j].strip() == "R"):
                j += 1
            has_text = any(TEXT.match(lines[k].strip()) for k in range(i, j))
            out.append(Stmt("text" if has_text else "newline", ln, end=j))
            i = j
            continue
        if s == "ZH 1:":
            i += 1
            continue
        m = re.match(r"^\{(.*):$", s)
        if m:
            body, j = parse_block(lines, i + 1, stop)
            if j >= stop or lines[j].strip() != "}":
                sys.exit(f"unclosed block at {ln}")
            out.append(Stmt("if", ln, m.group(1), body=body, end=j + 1))
            i = j + 1
            continue
        if s == "%0:":
            out.append(Stmt("return", ln))
        elif s == "\\0:":
            out.append(Stmt("ret", ln))
        elif m := re.match(r"^@(L_\w+):$", s):
            out.append(Stmt("goto", ln, m.group(1)))
        elif m := re.match(r"^\\(L_\w+):$", s):
            out.append(Stmt("call", ln, m.group(1)))
        elif m := re.match(r"^Y 4, (\d+):$", s):
            out.append(Stmt("rnd", ln, m.group(1)))
        elif m := re.match(r"^!(VAR\d{4}) : (.+)!$", s):
            out.append(Stmt("assign", ln, m.group(1), m.group(2)))
        elif m := re.match(r'^MS (\d+), "([^"]*)":$', s):
            out.append(Stmt("ms", ln, m.group(1), m.group(2)))
        elif m := re.match(r"^MM (.+), (\d+):$", s):
            out.append(Stmt("mm", ln, m.group(1), m.group(2)))
        elif s.startswith("*"):
            sys.exit(f"label inside a block at {ln}")
        else:
            sys.exit(f"unsupported statement at {ln}: {s!r}")
        i += 1
    return out, i


def terminates(stmts: list[Stmt]) -> bool:
    return bool(stmts) and stmts[-1].kind in ("return", "goto", "ret")


# ---------------------------------------------------------------------------------------------
# Code generation


class Gen:
    def __init__(self, label_names: dict[str, str]):
        self.labels = label_names
        self.methods: list[str] = []

    def emit_body(self, stmts: list[Stmt], indent: int, out: list[str], returns_bool: bool):
        for depth, code, comment in self.render(stmts, returns_bool):
            m = re.match(r"^:(\d+)", comment)
            if m and int(m.group(1)) in QUIRKS:
                comment += f" {QUIRKS[int(m.group(1))]}"
            pad = "    " * (indent + depth)
            out.append(f"{pad}{code}{f' // {comment}' if comment else ''}")

    def render(self, stmts: list[Stmt], returns_bool: bool) -> list[tuple[int, str, str]]:
        """Statements as (relative depth, code, comment) lines; short blocks become one line."""
        ret = f"return{' true' if returns_bool else ''};"
        out: list[tuple[int, str, str]] = []
        i = 0
        while i < len(stmts):
            st = stmts[i]
            # Standard window: open, (text, wait)+, close  ->  one SayAsync call.
            if st.kind == "call" and st.arg in OPEN:
                j = i + 1
                refs = []
                while j + 1 < len(stmts) and stmts[j].kind == "text" and stmts[j + 1].kind == "call" and stmts[j + 1].arg == WAIT:
                    refs.append(stmts[j].line)
                    j += 2
                if refs and j < len(stmts) and stmts[j].kind == "call" and stmts[j].arg == CLOSE:
                    out.append((0, f"await SayAsync({OPEN[st.arg]}, {', '.join(map(str, refs))});", ""))
                    i = j + 1
                    continue
            if st.kind == "if":
                head = f"if ({condition(st.arg)})"
                inner = self.render(st.body, returns_bool)
                one = " ".join(code for _, code, _ in inner)
                notes = [c for _, _, c in inner if c]
                comment = f":{st.line}" + (f" {'; '.join(notes)}" if notes else "")
                if all(d == 0 and not c.startswith("//") for d, c, _ in inner) and len(inner) <= 3 and len(head) + len(one) + len(comment) < 130:
                    out.append((0, f"{head} {{ {one} }}" if inner else f"{head} {{ }}", comment))
                else:
                    out.append((0, head, f":{st.line}"))
                    out.append((0, "{", ""))
                    out += [(d + 1, c, n) for d, c, n in inner]
                    out.append((0, "}", ""))
            elif st.kind == "return":
                out.append((0, ret, ""))
            elif st.kind == "ret":
                out.append((0, ret, ""))
            elif st.kind == "goto":
                target = self.labels.get(st.arg) or sys.exit(f"unknown goto target {st.arg}")
                # Every goto target ends with %0, so the goto is a tail call.
                out.append((0, f"await {target}();", f"@{st.arg}"))
                out.append((0, ret, ""))
            elif st.kind == "call":
                if st.arg in OPEN:
                    out.append((0, f"await OpenWindowAsync({OPEN[st.arg]});", ""))
                elif st.arg == WAIT:
                    out.append((0, "await WaitKeyAsync();", ""))
                elif st.arg == CLOSE:
                    out.append((0, "await CloseWindowAsync();", ""))
                else:
                    target = self.labels.get(st.arg) or sys.exit(f"unknown call target {st.arg}")
                    out.append((0, f"await {target}();", "\\" + st.arg))
            elif st.kind == "text":
                out.append((0, f"await ShowTextAsync({st.line});", ""))
            elif st.kind == "newline":
                out.append((0, f"// :{st.line} R: a bare newline outside any window (display only, no event).", ""))
            elif st.kind == "rnd":
                out.append((0, f"Rnd({st.arg});", f":{st.line}"))
            elif st.kind == "assign":
                rhs = st.arg2
                rhs = var(rhs) if rhs.startswith("VAR") else value(rhs)
                out.append((0, f"{var(st.arg)} = {rhs};", f":{st.line}"))
            elif st.kind == "ms":
                out.append((0, f"State.Strings[{st.arg}] = \"{st.arg2}\";", f":{st.line} MS, a name"))
            elif st.kind == "mm":
                out.append((0, f"State.Strings[(int)({value(st.arg)})] = State.Strings[{st.arg2}];", f":{st.line} MM"))
            else:
                sys.exit(f"cannot emit {st.kind}")
            i += 1
        return out

    def method(self, name: str, summary: str, first: int, last: int, stmts: list[Stmt], spec: str):
        returns_bool = not terminates(stmts)
        out = [
            f"    /// <summary>",
            f"    /// FlavourText — {summary}: <c>{FILE}:{first}-{last}</c> ({spec}).",
        ]
        if returns_bool:
            out.append("    /// Returns true when the script returned (<c>%0</c>), false when it fell through.")
        out += [
            f"    /// </summary>",
            f"    private async Task{'<bool>' if returns_bool else ''} {name}()",
            "    {",
        ]
        if not returns_bool and stmts and stmts[-1].kind in ("return", "ret"):
            stmts = stmts[:-1]  # the closing %0 / \0 is the end of the method
        self.emit_body(stmts, 2, out, returns_bool)
        if returns_bool:
            out.append("        return false;")
        out.append("    }")
        self.methods.append("\n".join(out))


def dispatch_name(cond_raw: str, side: str) -> tuple[str, str]:
    m = re.match(r"^VAR0154 = (\d+)$", cond_raw)
    if m and side == "player":
        return f"Unit{m.group(1)}", f"player side, unit {m.group(1)}"
    m = re.match(r"^VAR0156 = (\d+)$", cond_raw)
    if m and side == "enemy":
        return f"Cmdr{m.group(1)}", f"enemy side, commander {m.group(1)}"
    if cond_raw in COMPOUND_NAMES:
        n, d = COMPOUND_NAMES[cond_raw]
        return n, f"{side} side, {d}"
    sys.exit(f"no name for dispatch condition {cond_raw!r}")


def generate(scenario: Path) -> str:
    global NAMES
    NAMES = load_names()
    lines = (scenario / FILE).read_text(encoding="utf-8").split("\n")
    lines = [l.rstrip("\r") for l in lines]

    for (a, b), want in EXPECTED.items():
        got = [l.strip() for l in lines[a - 1:b] if l.strip()]
        if got != want:
            sys.exit(f"{FILE}:{a}-{b} no longer matches the hand-written port:\n{got}")

    # Split the generated range into labelled regions.
    regions: list[tuple[str, int, int]] = []
    starts = [(i + 1, lines[i].strip()[1:-1]) for i in range(FIRST - 1, LAST) if lines[i].startswith("*")]
    for k, (ln, label) in enumerate(starts):
        end = starts[k + 1][0] - 1 if k + 1 < len(starts) else LAST
        regions.append((label, ln, end))

    parsed = {}
    for label, a, b in regions:
        stmts, j = parse_block(lines, a, b)  # skip the label line itself
        if j != b:
            sys.exit(f"stray '}}' in {label} at {j + 1}")
        if not terminates(stmts):
            sys.exit(f"region {label} does not end with %0 / \\0")
        parsed[label] = (a, b, stmts)

    # Name every region: the two dispatchers, dispatch targets, then the named scenes.
    label_names = {"L_000b5": "FlavourPlayerSideAsync", "L_036f2": "FlavourEnemySideAsync"}
    descriptions = {}
    for label in ("L_000b5", "L_036f2"):
        side = "player" if label == "L_000b5" else "enemy"
        for st in parsed[label][2]:
            if st.kind == "if" and len(st.body) == 1 and st.body[0].kind == "goto":
                base, desc = dispatch_name(st.arg, side)
                label_names[st.body[0].arg] = f"Flavour{base}Async"
                descriptions[st.body[0].arg] = desc
    for label, (name, desc) in NAMED.items():
        label_names[label] = name
        descriptions[label] = desc
    # A scene reached by goto from inside a dispatch block (C157 -> L_0b958) is named above
    # only when its block is a bare goto; anything left is an error.
    for label in parsed:
        if label not in label_names:
            sys.exit(f"no name for region {label}")

    gen = Gen(label_names)
    spec_side = {"player": "spec 02 §5.2", "enemy": "spec 02 §5.3"}

    for label in ("L_000b5", "L_036f2"):
        a, b, stmts = parsed[label]
        side = "player" if label == "L_000b5" else "enemy"
        dispatch: list[Stmt] = []
        for st in stmts:
            if st.kind == "if" and not (len(st.body) == 1 and st.body[0].kind == "goto"):
                base, desc = dispatch_name(st.arg, side)
                name = f"Flavour{base}Async"
                gen.method(name, desc, st.line, st.end, st.body, spec_side[side])
                bool_method = not terminates(st.body)
                dispatch.append(Stmt("dispatch", st.line, st.arg, name, end=1 if bool_method else 0))
            else:
                dispatch.append(st)
        # The dispatcher itself.
        out = [
            "    /// <summary>",
            f"    /// FlavourText — {'player side: dispatch on <c>Unit_Cur</c>' if side == 'player' else 'enemy side: dispatch on <c>Cmdr_Cur</c>'}"
            f" (<c>{label}</c>): <c>{FILE}:{a}-{b}</c> ({spec_side[side]}).",
            "    /// </summary>",
            f"    private async Task {label_names[label]}()",
            "    {",
        ]
        body: list[str] = []
        if dispatch and dispatch[-1].kind == "return":
            dispatch = dispatch[:-1]  # the closing %0 is the end of the method
        for st in dispatch:
            if st.kind == "dispatch":
                if st.end:
                    body.append(f"        if ({condition(st.arg)} && await {st.arg2}()) return; // :{st.line}")
                else:
                    body.append(f"        if ({condition(st.arg)}) {{ await {st.arg2}(); return; }} // :{st.line}")
            else:
                gen.emit_body([st], 2, body, False)
        out += body
        out.append("    }")
        gen.methods.insert(0 if side == "player" else 1, "\n".join(out))

    for label, (a, b, stmts) in parsed.items():
        if label in ("L_000b5", "L_036f2"):
            continue
        spec = "spec 02 §5.4" if label in NAMED else (spec_side["player"] if label_names[label].startswith("FlavourUnit") else spec_side["enemy"])
        gen.method(label_names[label], f"{descriptions[label]} (<c>{label}</c>)", a, b, stmts[:], spec)

    header = f"""// <auto-generated>
// Generated by tools/codegen/gen_figt_m.py from {FILE} (lines {FIRST}-{LAST}). Do not edit by hand.
// Every statement is translated one-to-one; `// :n` is the line in the raw decompile.
// </auto-generated>
namespace Kichikuou.Original.Combat;

public sealed partial class Battle
{{
"""
    return header + "\n\n".join(gen.methods) + "\n}\n"


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("scenario", type=Path, help="raw decompiled scenario directory")
    ap.add_argument("--check", action="store_true", help="fail if the committed output differs")
    args = ap.parse_args()
    text = generate(args.scenario)
    if args.check:
        current = OUT.read_text(encoding="utf-8") if OUT.exists() else ""
        if current != text:
            sys.exit(f"{OUT} is out of date; re-run without --check")
        print("up to date")
        return
    OUT.write_text(text, encoding="utf-8", newline="\n")
    print(f"wrote {OUT}")


if __name__ == "__main__":
    main()
