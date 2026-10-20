# Importing the game — reading the player's game folder directly (plan, 2026-10-20)

Owner's decision 28 ([strategy/README.md](strategy/README.md)): the release reads the player's own game
folder, with no tools of ours or anyone else's to run first. Today the port needs the developer's extract
(`KICHIKUOU_EXTRACT_DIR`, "_extract"), made by the xsys35c 1.13 tools: the scenario decompiled by
`xsys35dc`, the CGs converted by `pms.exe`, the sounds taken out of their archive. The import replaces
all three with C# that runs on every platform the game is built for (Android cannot run a bundled
native tool, [backlog.md](backlog.md), "Release").

Status: **under way** (2026-10-20): steps 1-7 ✓, step 8 part 1 ✓. The facts come from a read-only sweep of the code and
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
3. **Sounds from the archive** ✓ (2026-10-20). `Content/Audio/SoundLibrary` gives a sound's WAV bytes by
   its number from the game's sound archive (the sound n at index n − 1) or from the extract's `wav\`
   (under `SoundTable`'s names, which are the archive's); `BattleAudio` builds the stream from the bytes
   (`AudioStreamWav.LoadFromBuffer`, in Godot 4.7.2) instead of a file. Test:
   `GameArchiveTests.Every_sound_from_the_archive_equals_its_wav` — the 51 sounds byte for byte, nothing
   where the table links nothing. The game still reads `wav\`; the switch is step 8's.
4. **The `.ain` reader** ✓ (2026-10-20). `Content/Archives/AinFile`, after xsys35c `decompiler/ain.c`: the
   magic `AINI`/`AIN2` and the version (1 or 2) read as stored, then every byte after the first 4 rotated
   left by 2 bits; the sections `VARI` and `MSGI` (a count and NUL-terminated strings, kept as raw Shift JIS
   until step 5); `HEL0` and `FUNC` must be empty, as in this game (the decompiler reads no game with them).
   The player's file: version 1, 806 variables, 126,306 messages (85,024 of them plain ASCII). Tests:
   `AinFileTests` (core, files written here) and `GameArchiveTests.Ain_matches_the_dump` (content). The
   extract's dump `aindump\System39.ain.txt` is lossy: its Shift JIS bytes were read as UTF-8 on the way to
   the file, so every non-ASCII run is a U+FFFD there; the test compares the same reading of our bytes
   (a run of U+FFFD as one) and one message decoded in full. The full check of the texts is step 6's.
5. **SJIS text** ✓ (2026-10-20). `Content/Text/Sjis`, after xsys35c `common/sjisutf.c`: `Decode` is
   `sjis2utf` (ASCII as is, 0xa0-0xdf as U+FF60-U+FF9F — the file names such as `Cﾒｲｷｭｳ.ADV` —, a pair
   through the table, anything else an error), with the checks the decompiler uses (`IsValid`,
   `IsUnicodeSafe` — no gaiji, none of the code points several pairs map to —, `IsCompacted`/`Expand`
   for the compiler's half-width forms of full-width text). The table is xsys35c's own `common/s2utbl.h`,
   generated into `SjisTable.g.cs` by `tools/codegen/gen_sjis_table.py` (the header is not in the
   repository; the generator says where to get it). It agrees with .NET's code page 932 wherever both map
   a pair, and maps 587 pairs more that .NET reads as "?" (ゔ, NEC symbols in row 0x87, the user-defined
   rows 0xeb-0xee and 0xfa) — measured by `SjisTests`, so a platform decoder would not do. Content tests:
   the archive's names decode to `xsys35dc.hed`'s, every `.ain` message decodes, and all 577 distinct
   non-ASCII characters of `_extract\scenario` are characters of the table.
6. **The decompiler** ✓ (2026-10-20). `Kichikuou.Content/Decompiler/`, translated from xsys35c 1.13
   `decompiler/` (`decompile.c`, `preprocess.c`, `cali.c`, `xsys35dc.c`'s `sco_new`): `ScenarioDecompiler.Decompile`
   turns the scenario archive and the `.ain` (or a list of pages and the `.ain`'s texts and names) into the files,
   each named by its page's header (`Cﾒｲｷｭｳ.ADV`), UTF-8 without a byte order mark, CRLF; `WriteTo` writes them.
   As xsys35dc it builds each page in Shift JIS and converts it at the end (`DecompileOutput`, `Sjis.Decode`), marks
   every byte (code, data, label and a construct type) first by the byte scans of `preprocess.c` (the `#` tables and
   `2F 80`, kept exactly), then by decompiling every page without output until no pass changes a mark an earlier
   part of a page depends on (the `analyzed` flag; the sticky `disable_else`), then writes. Expressions are
   `Cali.cs`. The command table is xsys35dc's whole (`ScenarioDecompiler.Commands.cs`), not only the ~70 commands
   this game uses: its analysis passes read data as code before they know it is data, and a command it knows there
   is not retried as data, so a shorter table could mark a page otherwise. Not translated, each a failure with the
   file and the address: pages other than `S380`, a Unicode scenario, functions (`**`, `~`, the commands taking a
   function), DLL calls, a page missing from the archive (xsys35dc's `_missing<n>.adv`), the side files
   (`xsys35dc.hed`, `xsys35c.cfg`, `variables.txt`). Content test `DecompilerTests`: all 201 files byte-identical
   to `_extract\scenario` (503,161 lines), in the order of `xsys35dc.hed`; the whole decompile, the archive read
   included, takes ~0.6 s on the owner's machine, in 2 analysis passes, with no warning. Core tests
   `ScenarioDecompilerTests` on pages written there (expressions, a condition and an `.ain` text, a data table found
   in the second pass, an unknown command read as data, the refusals).
7. **The version check** ✓ (2026-10-20). `Kichikuou.Content/GameFiles`: `Check(folder)` hashes (SHA-256)
   `KICHIKUSA.ALD`, `System39.ain`, `KICHIKUGA.ALD`, `KICHIKUGB.ALD`, `KICHIKUWA.ALD` and `_inmm.ini` (names matched
   without case) against the table of the one version we read, `GameFiles.EnglishTranslation10` (the hashes of the
   player's files are not their content, so the table is committed). The result, `GameFolder`, holds each file's
   path and `Key`, 16 hex digits of the version's hash lines, for step 8's cache. A known file missing, or another
   hash, throws `GameFolderException` with a message that names the file and the version expected (both hashes for
   a different file) — before any reader runs. The decompile's own checks back it up for a version whose hash we
   lack. Tests: `GameFilesTests` (core, folders and tables written there) and
   `GameArchiveTests.The_game_folder_is_the_known_version` (content; the check takes ~60 ms on the owner's machine).
8. **The import** (~1-1.5 days). On the first launch, or when the folder changes: pick the game folder
   (Godot's native dialog), check the version, decompile the scenario into `user://import/<hash>/scenario/`
   (the cache keyed by the files' hashes and the importer's version; the readers stay directory-based),
   and keep the folder for the CGs, sounds and music, read from the archives at run time. A progress
   screen while it runs; a clear message when it fails. `GameData` takes the import when there is no
   `KICHIKUOU_EXTRACT_DIR`; the developer's extract wins where it is set.
   - **Part 1 ✓** (2026-10-20): the import without its screen. `Kichikuou.Content/GameImport.Import(folder, cache)`
     checks the version (step 7), then decompiles the scenario into `<cache>/<GameFolder.Key>-<ImporterVersion>/scenario`
     unless a whole one is there (a marker `import.done` written last; a folder cut short is done again), and
     reports `ImportProgress` (a stage and a fraction that never goes back); `ImportedGame` opens the CGs and the
     sounds from the folder's archives. Godot's `GameData` has the two sources: the extract wins where one is set
     and holds `scenario\`, else `Imported` (opened at the start by `GameData.OpenImport` from the saved
     `game_dir` when its cache is whole); `GameData.Cgs()` and `Sounds()` give the libraries either way.
     `--import [DIR]` makes a launch read the import even where an extract is set, importing first (~0.8 s).
     Content test `DecompilerTests.The_import_caches_the_extracts_scenario`; a headless run from the kept save
     `user://saves-week18` with `--import --until battle` plays the week's attack on Oaks (6) as the extract does.
   - Part 2: the import screen, at the start when there is no data (no extract, no whole import of the saved
     folder). Mock-up round 1 (2026-10-20, `tools/ui/mock_import.py`, drawn on our wave backdrop: nothing of the
     game can be drawn before the import), three looks in three states each — 1 choose the folder (Godot's native
     folder dialog), 2 importing (the stage and the bar), 3 refused (`GameFolderException`'s message):
     **A** a lacquer panel in the middle (the folder as a row, the steps with ticks, a gold bar, the refusal in a
     vermilion box); **B** a paper sheet in a lacquer frame (the steps as a numbered list 一 二 三, a vermilion bar,
     a stamp on a refusal); **C** a title card (the name large, one line and one button, a thin bar along the
     screen's foot). Awaiting the owner's choice.
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
