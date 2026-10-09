"""Watch the random draws of the original (patched xsystem35) and of the port side by side.

Play both with the same seed:
  powershell -File tools/golden/capture.ps1 -Seed 12345 -RngLog
  <Godot> --path godot -- --seed 12345 --rnglog
and run this in a third console:
  python tools/golden/rngwatch.py

Both write one line per draw, "rnd <k>: Y 4, <n> = <result>" (the original adds the script
location): the original to rnglog-original.txt, the port to rnglog.txt, both in
%APPDATA%\\Godot\\app_userdata\\Kichikuou and emptied when the program starts. The watcher compares
draw k of both as soon as both have it; at the first difference it prints both lines with the
matching draws before them, beeps and opens a message box on top of the games. A program
started again (its file emptied) starts the comparison over.
"""

import argparse
import ctypes
import os
import re
import sys
import threading
import time

LINE = re.compile(r"^rnd (\d+): Y 4, (\d+) = (\d+)\s*(.*)$")
DEFAULT_DIR = os.path.join(os.environ.get("APPDATA", ""), "Godot", "app_userdata", "Kichikuou")

RED, GREEN, YELLOW, RESET = "\x1b[91m", "\x1b[92m", "\x1b[93m", "\x1b[0m"


class Log:
    """One side's file, read as it grows; reset when the file is emptied or replaced."""

    def __init__(self, name, path):
        self.name, self.path = name, path
        self.offset, self.rest, self.draws = 0, b"", []
        self.restarted = False

    def poll(self):
        try:
            size = os.path.getsize(self.path)
        except OSError:
            return
        if size < self.offset:
            self.offset, self.rest, self.draws = 0, b"", []
            self.restarted = True
        if size == self.offset:
            return
        with open(self.path, "rb") as f:
            f.seek(self.offset)
            data = f.read()
        self.offset += len(data)
        lines = (self.rest + data).split(b"\n")
        self.rest = lines.pop()
        for raw in lines:
            text = raw.decode("utf-8", "replace").strip()
            m = LINE.match(text)
            if not m:
                continue
            k, n, result, where = int(m[1]), int(m[2]), int(m[3]), m[4]
            if k != len(self.draws) + 1:
                print(f"{YELLOW}{self.name}: draw {k} after {len(self.draws)} draws (lines missing?){RESET}")
            self.draws.append((n, result, where, text))


def alert(title, text, popup):
    try:
        import winsound
        winsound.MessageBeep(0x10)
    except Exception:
        sys.stdout.write("\a")
    if popup and os.name == "nt":
        flags = 0x10 | 0x40000 | 0x10000  # MB_ICONERROR | MB_TOPMOST | MB_SETFOREGROUND
        threading.Thread(target=ctypes.windll.user32.MessageBoxW, args=(None, text, title, flags), daemon=True).start()


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--original", default=os.path.join(DEFAULT_DIR, "rnglog-original.txt"))
    parser.add_argument("--port", default=os.path.join(DEFAULT_DIR, "rnglog.txt"))
    parser.add_argument("--no-popup", action="store_true", help="no message box, only the console and a beep")
    args = parser.parse_args()
    if os.name == "nt":
        os.system("")  # turns on the console's colour codes

    original, port = Log("original", args.original), Log("port", args.port)
    print(f"Watching\n  original: {original.path}\n  port:     {port.path}\n(Ctrl+C to stop)")
    checked, diverged, shown = 0, False, None
    while True:
        for log in (original, port):
            log.poll()
            if log.restarted:
                log.restarted = False
                checked, diverged = 0, False
                print(f"\n{YELLOW}{log.name} started again: comparing from the first draw{RESET}")
        if not diverged:
            while checked < min(len(original.draws), len(port.draws)):
                a, b = original.draws[checked], port.draws[checked]
                if (a[0], a[1]) != (b[0], b[1]):
                    diverged = True
                    k = checked + 1
                    before = "".join(f"  {original.draws[i][3]}\n" for i in range(max(0, checked - 3), checked))
                    report = (f"First difference at draw {k}:\n"
                              f"  original: {a[3]}\n  port:     {b[3]}\n"
                              + (f"after the matching draws\n{before}" if before else "(no draw before it)\n"))
                    print(f"\n{RED}{report}{RESET}", end="")
                    alert("Random draws differ", report, not args.no_popup)
                    break
                checked += 1
        state = (len(original.draws), len(port.draws), checked, diverged)
        if state != shown:
            shown = state
            verdict = (f"{RED}DIFFERENT at draw {checked + 1}{RESET}" if diverged
                       else f"{GREEN}{checked} draws match{RESET}")
            sys.stdout.write(f"\r{verdict}; original {len(original.draws)}, port {len(port.draws)}      ")
            sys.stdout.flush()
        time.sleep(0.2)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print()
