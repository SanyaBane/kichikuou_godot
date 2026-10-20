# Importing the game — reading the player's game folder directly (plan, 2026-10-20)

Owner's decision 28 ([strategy/README.md](strategy/README.md)): the release reads the player's own game
folder, with no tools of ours or anyone else's to run first. Today the port needs the developer's extract
(`KICHIKUOU_EXTRACT_DIR`, "_extract"), made by the xsys35c 1.13 tools: the scenario decompiled by
`xsys35dc`, the CGs converted by `pms.exe`, the sounds taken out of their archive. The import replaces
all three with C# that runs on every platform the game is built for (Android cannot run a bundled
native tool, [backlog.md](backlog.md), "Release").

Status: **under way** (2026-10-20): steps 1-2 ✓. The facts come from a read-only sweep of the code and
of xsys35c 1.13's source on 2026-10-20, at commit `3ce2455`.

## Why the decompiler is translated, not replaced (option B)

The port identifies scenario code by **the line of xsys35dc 1.13's output**:

- `ScriptRef(File, Line)` (`Engine/Script/ScriptInterfaces.cs`) is the location type of the runtime; the
  parser gives each statement its line (`ScenarioParser.cs:42`).
- `ScenarioText.Read` finds a message by its line; the presenters show every message that way.
- The hand ports pass lines to `TextAsync`, `MessageAsync`, `SpeakAsync`, `ValuesAsync`, `Literal`,
  `RunBlockAsync`, `MenuItem` and the like (~1,500 calls), and `src/` holds ~800 literal `"X.ADV", n`.
- `src/` and `docs/` cite `.ADV:n` ~6,700 times.
- The saves keep `Resume.File`/`Resume.Line` (`SaveData.cs`), and the message skip keys the messages
  it has seen as `file:line` in a file of its own (`MessageSkip.cs:155`).

So whatever reads the `.SCO` bytecode must produce **xsys35dc 1.13's text, line for line**. Three
options were weighed (2026-10-20):

| | What | Verdict |
|---|---|---|
| A | Bundle `xsys35dc.exe` and run it at the import | Rejected: native, no Android |
| **B** | **Translate xsys35dc to C#, the part this game reaches, and feed its text to the parser we have** | **Chosen** |
| C | Our own `.SCO` loader that builds `ScriptProgram` directly | Rejected: it still needs the lines — either it re-does xsys35dc's line layout (then it is B plus a second reader) or every line reference, doc citation and save moves to byte addresses |

B keeps the parser, every hand port, the docs and the saves as they are, and it has a perfect oracle:
the 201 `.ADV` files of `_extract\scenario`, compared byte for byte.

## What the game folder holds (English translation v1.0)

| File | What | Read today from |
|---|---|---|
| `KICHIKUSA.ALD` | scenario, 201 `.SCO` (all `S380`) | `_extract\scenario\*.ADV` (xsys35dc) |
| `System39.ain` | the English text (`MSGI`, ~126k messages) and the variable slots (`VARI`, 806); `HEL0`, `FUNC` empty | (through xsys35dc) |
| `KICHIKUGA.ALD` + `KICHIKUGB.ALD` | one CG archive in two volumes, 1222 PMS | `_extract\cg_png\CGnnnn.png` (`pms.exe`) |
| `KICHIKUWA.ALD` | 51 WAV sound effects | `_extract\wav\` |
| `_inmm.ini`, `bgm\*.mp3` | the music, 28 tracks | the game folder already (`MusicTable`) |

The English text lives only in the `.ain`: a scenario text is `2F 7C` plus a message number, so the
decompiler cannot run without it. Nothing at run time reads `aindump`, `scenario_named`, `cg_pms` or the
decompiler's side files (`xsys35dc.hed`, `variables.txt`, `xsys35c.cfg`).

## Principles

- **Nothing changes for the player.** The imported data must equal the extract the port is tested on:
  every `.ADV` byte for byte, every CG's indices, palette, position and palette mask, every WAV's bytes.
  Each reader step ends with a test comparing all of it against `_extract`.
- **Fail loudly, never guess.** A construct, a command or an archive layout the translation does not know
  stops the import with its file and address. The subset is this game's; a mod's scenario may need more
  of xsys35dc, and adds it then.
- **Translated code carries its origin.** Each file translated from xsys35c names the source file and its
  copyright holder in its header (xsys35c: "Copyright (C) 2020 <KichikuouChrome@gmail.com>",
  GPL-2.0-or-later), as
  `Sys3/Mt19937Legacy.cs` and `Graphics/Pms.cs` do.
- **Engine-independent.** The readers live in `Kichikuou.Content` (no Godot); Godot only picks the folder,
  shows the progress and hands the paths over.
- **The extract stays for development.** `KICHIKUOU_EXTRACT_DIR` keeps working (the tests, the golden
  captures, the analysis tools); the release reads the import.

## Steps

Each step is one or a few commits, with the build, the core tests and its own tests green. Effort is in
agent days, rough.

1. **ALD reader** ✓ (2026-10-20). `Kichikuou.Content/Archives/AldArchive`: the 256-byte sectors, the
   pointer table (3-byte sector numbers), the link table (volume byte + 16-bit index), each entry's header
   (its size, the data's size, a Windows file time, the name in Shift JIS, kept as raw bytes until step 5);
   the data read from the file on demand. As xsystem35 opens a set (`dri.c`): the volume is the file's
   letter (`<prefix>A.ALD` = 1, matched without case), and only volume A's link table counts (System 3.9);
   the footer's signature is checked as xsys35c does. The link table is padded to whole sectors, so it is
   longer than the resources (256 for the scenario's 201 pages, 1792 for the CGs); the indices past them
   are gaps. Confirmed on the player's files: the scenario page n is index n (each `S380` header names
   it), the CG n and the sound n are index n − 1. Tests: `AldArchiveTests` (core, archives written as
   `ald_write` does) and `GameArchiveTests` (content): the 201 pages in `xsys35dc.hed`'s order, the 1222
   CGs equal to `_extract\cg_pms` in `cg_manifest.txt`'s volumes, the 51 sounds equal to `_extract\wav`
   at `SoundTable`'s numbers.
2. **CGs from the archive** ✓ (2026-10-20). `CgLibrary` has two sources: the extract's PNG folder
   (`new CgLibrary(directory)`, as before) or the game's CG archive set (`new CgLibrary(AldArchive)`, the CG
   n at index n − 1 decoded by `Graphics/Pms.cs`); `Numbers` lists the CGs there are and `SizeOf` reads a
   size from the header alone (the portrait viewer's list of faces, which read the PNG headers itself
   before). The PMS header gives what `pms.exe` wrote as `oFFs` (the default position) and `pmSk` (the
   palette mask). Test: `GameArchiveTests.Every_cg_from_the_archive_equals_its_png` — all 1222 CGs equal
   their PNG (size, position, mask, the 256 palette entries, indices), the same gaps, numbers and sizes.
   The game and the tests still read the PNGs; the switch is step 8's.
3. **Sounds from the archive** (~0.5 day). `SoundTable`'s names are the archive's; `BattleAudio` builds the
   stream from bytes (`AudioStreamWav.LoadFromBuffer`, to be checked in 4.7) instead of a file. Test: the
   51 entries equal `_extract\wav`.
4. **The `.ain` reader** (~0.5 day). The bytes after the first 4 rotated left by 2 bits; the sections
   `HEL0`, `FUNC`, `VARI`, `MSGI`; the messages SJIS. After xsys35c `decompiler/ain.c`. Test: the messages
   and variables against `_extract\aindump\System39.ain.txt`.
5. **SJIS text** (~0.5 day). xsys35c's own table (`common/s2utbl.h`, `common/sjisutf.c`), translated —
   not .NET's code page 932, so the output cannot drift with the platform; the half-width forms as
   xsys35dc writes them (file names such as `Cﾒｲｷｭｳ.ADV`). Test: every non-ASCII character of the
   extract round-trips.
6. **The decompiler** (~3-4 days), `Kichikuou.Content/Decompiler/`, translated from xsys35c
   `decompiler/` (`xsys35dc.c`, `preprocess.c`, `decompile.c`, `cali.c`; ~2,800 lines of C, of which this
   game reaches ~1,200-1,800): the `S380` header; the pre-pass marking code, data and labels (its byte
   scans for `#` tables and `2F 80` kept exactly); the passes repeated until nothing changes (the
   `analyzed` flag, the sticky `disable_else`); labels `L_%05x`; the command table with its argument
   signatures (the ~76 commands and constructs this game uses; any other fails); expressions (`cali`);
   the data blocks split into `"string"` and `[words]` lines; texts cut at marked bytes, a following
   `R`/`A` on the same line, the escapes of `"`, `\` and `<`; UTF-8 out with CRLF; file names from the
   archive. Not translated: `**` functions and `~` calls, `else`/`while`/`for` reconstruction beyond what
   the passes need, `pragma`, the `<0x…>` escapes, the side files — this game has none of them (measured
   on the extract), so each stays a loud failure. Test (content): all 201 files byte-identical to
   `_extract\scenario`; and the time the whole decompile takes.
7. **The version check** (~0.5 day). `Kichikuou.Content/GameFiles`: the SHA-256 of `KICHIKUSA.ALD`,
   `System39.ain`, `KICHIKUGA.ALD`, `KICHIKUGB.ALD`, `KICHIKUWA.ALD` and `_inmm.ini` against the table of
   the one version we read (hashes of the player's files are not their content, so the table is
   committed). A known file missing, or an unknown hash, refuses the folder with a message that names the
   file and the version expected — before any parser runs, instead of failing somewhere inside it. The
   decompile's own checks back it up for a version whose hash we lack.
8. **The import** (~1-1.5 days). On the first launch, or when the folder changes: pick the game folder
   (Godot's native dialog), check the version, decompile the scenario into `user://import/<hash>/scenario/`
   (the cache keyed by the files' hashes and the importer's version; the readers stay directory-based),
   and keep the folder for the CGs, sounds and music, read from the archives at run time. A progress
   screen while it runs; a clear message when it fails. `GameData` takes the import when there is no
   `KICHIKUOU_EXTRACT_DIR`; the developer's extract wins where it is set.
9. **Docs** (~0.5 day). CLAUDE.md, spike-findings ("Local layout": the extract is for development),
   README (installing: where to get the game and the translation, what to point the game at), the
   backlog.

Total: ~8-10 agent days. Steps 1-5 can go in any order; 6 needs 1, 4 and 5; 7 and 8 come last.

## Not in this plan

- The folder picker's look and the first-launch screen beyond what step 8 needs (the release's own items,
  [backlog.md](backlog.md), "Release").
- Android's access to the player's files (scoped storage) — the import is ready for it, the way the files
  reach the device is decided with the Android build.
- Another version or a mod (the Sparrow Mod, [mods/sparrow-mod.md](mods/sparrow-mod.md)): a new hash
  table row and whatever of xsys35dc its scenario reaches.
- Dropping the extract from the tests: the content tests keep reading `_extract`; they can move to the
  import once it is proven.
