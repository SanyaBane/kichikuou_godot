# Game assets — how the original engine finds and draws them (2026-10-08)

What a runtime loader in the Godot project needs to know about the player's own extracted
files (`E:\Modding games\Modding Rance\Kichikou\_extract\`, see
[spike-findings.md](spike-findings.md) for the local layout). Facts only; each one cites the
xsystem35 source (`F:\sources\Rance\xsystem35-sdl2\src`, branch `golden-harness`, commit
`1d7d3fe`), a byte dump, or a count made by a script over the extracted data. Items that were
**not** verified are marked **[unverified]**.

The analysis scripts and their TSV outputs live outside the repository (session scratchpad,
`...\scratchpad\assets\`): `pms_headers.py` → `cg_headers.tsv`, `ald_links.py` →
`cg_links.tsv` / `sound_links.tsv`, `sound_map.py` → `sound_map.tsv`, `pcm_calls.py`. They hold
no game data, but they are not committed.

## 1. CG numbering

- The script number is **1-based**; the engine subtracts one and looks the result up in the
  archive's link table: `G n:` → `cg_load(num - 1, -1)` (`cmdg.c:38`), `G n, s:` →
  `cg_load(num - 1, sprite)` (`cmdg.c:53`), `cg.c:273` `ald_getdata(DRIFILE_CG, no)`.
  `G 0` only clears a pending J offset (`cmdg.c:32-36`).
- ALD link table (System 3.9, `dri.c:33-76`): bytes 0-2 / 3-5 of the first volume (`*GA.ALD`)
  give the sizes of the offset table and of the link table in 256-byte sectors. The link table
  holds 3 bytes per resource index `i` = (volume 1=A, 2=B, 0=absent; u16 index into that
  volume's offset table). Only the **first** volume's link table is used (`dri.c:47-57`).
  An entry starts with `u32 header size, u32 data size`, the file name at +16 (`dri.c:147-190`).
- `KICHIKUGA.ALD` link table: 1792 slots, **1222 linked** (1048 in GA, 174 in GB), numbers
  **1..1748**. `cg_manifest.txt` (`disk,number,file`, CRLF) is identical to the link table
  (`ald_links.py`, 1222 = 1222 triples). For every entry the stored name is `CG%04d.PMS` of its
  own number, and `cg_png\` holds exactly the matching `CG%04d.png` set: **`CGnnnn.png` is CG
  number `nnnn`**, no duplicates.
- Gaps: 526 unused numbers in 22 ranges — 194, 241-245, 264-269, 284-297, 305-349, 398-400,
  518-599, 655-691, 813, 816, 832-835, 858-860, 863-869, 871-886, 920-1000, 1065-1099, 1456,
  1459-1462, 1477, 1508, 1531-1699, 1721-1729. A missing number makes `G` a silent no-op
  (`dri.c:147-152` returns NULL, `cg.c:392-394` returns).
- Of 345 literal `G n` numbers in the scenario only `G 340` hits a gap
  (`ﾌﾟﾛﾛｰｸﾞ.ADV:139`, inside `PC 6` … `PC 7`, i.e. a palette-only load) — a no-op in the original.

## 2. Formats and default placement

- **Every** CG in the archive is an 8-bit PMS: all 1222 start with `50 4D 01 00` (`PM`, version
  1), header size 48, bpp 8 (`cg_links.tsv`, `cg_headers.tsv`). There are no QNT, VSP, BMP or
  16-bit PMS images, so only `pms256_extract` matters (`cg.c:285-287`).
- PMS header, as read by `pms.c:54-72` (`extract_header`), all little-endian:

  | Offset | Size | Field (`pms.h:31-45`) | Kichikuou values |
  |---|---|---|---|
  | 0 | 2 | magic `PM` | always |
  | 2 | 2 | version | 1 |
  | 4 | 2 | header size | 48 |
  | 6 | 1 | bpp | 8 |
  | 7 | 1 | shadow bpp (16-bit only) | 0 |
  | 8 | 1 | sprite flag ("not used?") | 0 |
  | 10 | 2 | palette bank mask | `0xFFFF` ×1220, `0x0000` ×2 (CG 396, 815) |
  | 16 | 4 | x0 — default display x | |
  | 20 | 4 | y0 — default display y | |
  | 24 | 4 | width | |
  | 28 | 4 | height | |
  | 32 | 4 | offset of pixel data | |
  | 36 | 4 | offset of palette (256 × RGB, 768 bytes; `pms.c:78-87`) | |
  | 40 | 4 | offset of comment | 0 |

  Example, `CG0807.PMS` (window frame, drawn with `G Ui_WinFrameCg, 40`):
  `504d 0100 3000 0800 0000 ffff 0000 0000 | c800 0000 0300 0000 9801 0000 7a00 0000 | 3003 0000 3000 0000 0000 0000`
  → x0 = 200, y0 = 3, 408×122, data at 0x330, palette at 0x30.
  Pixel data is the PMS 8-bit RLE of `pms.c:95-128`; a Python port of it (`pms_headers.py`)
  reproduces the PNG indices exactly on a sample of 31 CGs.
- Default position: 431 CGs have a non-zero (x0, y0), 791 have (0, 0). The draw position is
  chosen in `set_display_loc` (`cg.c:203-223`): with no pending J command (`OFFSET_NOMOVE`)
  it is (x0, y0); `J0 x,y` / `J2` = absolute (x, y); `J1` / `J3` = (x0 + x, y0 + y) — relative to
  the header position; `J0`/`J1` apply to the next `G` only, `J2`/`J3` until `J4`
  (`cmdj.c:30-63`, `cg.c:228-244`, `cg.c:367-379`). The battle uses `J1 640, 0` before its
  `G`s (`FIGT.ADV:1628-1717`), i.e. header position + (640, 0) on the 3200×800 virtual screen
  set by `WW 3200, 800, 8` (`ｽﾀｰﾄ.ADV:852`).
- **The PNG conversion kept the position.** xsys35c `pms.exe` writes a standard `oFFs` chunk
  (int32 x, int32 y, unit 0 = pixels) for exactly the 431 CGs with non-zero position, and it
  matches the header in all 431 cases; images at (0, 0) have no `oFFs`. Example `CG0807.png`
  at 0x32d: `00 00 00 09 6f 46 46 73 | 00 00 00 c8 | 00 00 00 03 | 00` → (200, 3).
  It also writes a private `pmSk` chunk (2 bytes) for the two CGs whose palette mask is not
  `0xFFFF` (CG 396, 815: `pmSk` = `0000`). `pms.exe --encode` takes `--position` and
  `--palette-mask` options for the reverse direction (`pms.exe --help`).
- PNGs are colour type 3 (indexed), bit depth 8, `PLTE` with 256 entries that equal the PMS
  palette byte-for-byte in all 1222 files, and **no `tRNS`** chunk.

## 3. Transparency and palettes

### The screen is 8-bit indexed

- `ags_init` sets `world_depth = SYS35_DEFAULT_DEPTH` = 8 (`ags.c:132`, `xsystem35.h:37`) and
  the scenario keeps it at 8: `WW 3200, 800, 8` (`ｽﾀｰﾄ.ADV:852`, `ｽﾀMENU.ADV:180`),
  `WW 640 * 2, 400, 8` (`XSPEC.ADV:1`). An 8-bit world is an `SDL_PIXELFORMAT_INDEX8` surface
  sharing the global palette (`gfx_video.c:129-149`).
- So pixels on screen are **palette indices**, and their colour is whatever the single
  screen palette says at display time. Loading a CG that brings a palette re-colours
  everything already on screen that uses the changed entries.

### What `G n` does with the palette (`cg.c:386-453`)

- Behaviour is controlled by `PC m` (`cmdp.c:136-142`), bits: 1 = draw pixels, 2 = push the
  palette to the display, 4 = copy the CG palette into the engine palette (`cg.c:63-65`);
  default 7 (`cg.c:349`). The scenario uses `PC 7` ×36, `PC 6` ×19 (palette-only load, e.g.
  `ｵｰﾌﾟﾆﾝｸﾞ.ADV:37-39`), `PC 1` ×17 (pixels without palette, opening and prologue only).
- With bit 4 set, an 8-bit PMS copies only the banks named in its mask, never the "Windows
  reserved" entries 0-9 and 246-255 (`cg.c:415-425`): mask bit 0 → entries 10-15, bit 15 →
  240-245, bits 1-14 → 16 entries each. For mask `0xFFFF` (1220 CGs) that is **entries
  10-245**. CG 396 and 815 (mask 0) leave the palette untouched and draw with the current one.
- Entries 0-9 and 246-255 come from `initPal` (`ags.c:58-87`: 0 = black, 7 = (192,192,192),
  246 = (255,251,240), 255 = white, …) and the scenario never changes them (no `PS`, `PP`,
  `PG` in the scenario). 1220 of 1222 CGs use reserved indices (mostly 0 and 255); in 18 PNGs
  the stored palette disagrees with `initPal` at a reserved index the image uses
  (e.g. index 246 stored as white instead of (255,251,240) in 6 files). The original shows the
  `initPal` colour there.
- Palettes are shared by design within a screen: 199 distinct palettes over entries 10-245, but
  across the battle sets — faces 1-100, backgrounds/UI 887-919, unit art 1100-1530 (557 CGs,
  309,692 ordered pairs) — **no pair disagrees on any index 10-245 that the earlier CG uses,
  except index 40**. Across the whole archive every index has several colours, so outside
  these sets this does not hold in general.

### `G n, 40` — colour key by palette index

- `commandG1` passes the second argument as `sprite_color` (`cmdg.c:43-53`) → `cg_load` →
  `gfx_drawImage8` sets `SDL_SetColorKey(s, SDL_TRUE, sprite_color)` on the 8-bit source
  surface (`gfx_draw.c:224`). On an indexed surface the key is a **pixel index**: every pixel
  whose index is 40 is skipped, whatever colour entry 40 has.
- Every two-argument `G` in the scenario uses 40 (121 of 966 `G` commands). There is no `GX`
  (alpha) and no `GS` in the scenario.
- Entry 40 is a "don't care" colour that differs between CGs (164 different RGB values; the most
  common are (16,169,103) ×424 and magenta (255,0,255) ×307). Index 40 covers e.g. 95% of
  unit art 1161 and 97% of overlay 901.
- The PNGs carry no transparency: no `tRNS`, the index is only implicit in the pixel data.

### What a Godot loader must do

1. Keep the **index**, not the RGB. Godot's PNG loader expands indexed PNGs to RGB(A)
   **[unverified — from Godot's `png_driver_common.cpp`, not checked against 4.7.2]**; then
   only the colour remains. Keying by the RGB of `PLTE[40]` works for 877 of the 883 CGs that
   use index 40, but in 6 (CG 110, 125, 230, 367, 397, 503) another used index has the same RGB.
   Safer options: decode the PNG `PLTE`/`IDAT` (or the PMS RLE, 34 lines in `pms.c:95-128`)
   yourself and build an RGBA texture with alpha 0 where index == 40 for `G n, 40`, alpha 255
   otherwise; a plain `G n` draws index 40 opaque.
2. Take the RGB of entries 0-9 and 246-255 from `initPal`, not from the file.
3. Take the position from `oFFs` (or the PMS header) — Godot ignores `oFFs`
   **[unverified]** — and apply the J-command rules above.
4. Per-CG palettes reproduce the original exactly for the battle sets checked above. Faithful
   emulation in general would need an indexed back buffer plus one shared palette (last
   `G` wins for entries 10-245). Not needed for the battle screen as far as checked.

## 4. Sound effects

- Number → file: `SP n, loop` → `mus_pcm_start(n)` (`cmds.c:229-245`, `music.c:130-135`),
  `SQ l, r, loop` → `mus_pcm_mix` (`cmds.c:296-314`, `music.c:143-148`), `SM n` →
  `mus_pcm_load(n)` (`cmds.c:335-341`). All end in `load_asset(DRIFILE_WAVE, no)`, which reads
  **`ald_getdata(type, no - 1)`** (`pcm.sdlmixer.c:67-82`) — the same 1-based link-table lookup
  as CGs, in `KICHIKUWA.ALD` (header `0100 0004 0000`: 1-sector offset table, link table to
  sector 4 → 256 slots, 51 linked).
- The WAV name is the ALD entry name; the extracted `wav\` folder uses the same names (all 51
  present, sizes equal the ALD data sizes, no extra files). Full table: `sound_map.tsv`.

  | n | file | n | file | n | file |
  |---|---|---|---|---|---|
  | 1 | R5FEF01.WAV | 17 | R5ATK03D.WAV | 41 | R5SAK06A.WAV |
  | 2 | R5FEF02.WAV | 18 | R5ATK03E.WAV | 42 | R5SAK06B.WAV |
  | 5 | DUMMY.WAV | 19 | R5ATK03F.WAV | 43 | R5SAK07A.WAV |
  | 6 | R5ATK01A.WAV | 20 | R5ATK04A.WAV | 44 | R5SAK07B.WAV |
  | 7 | R5ATK01B.WAV | 21 | R5ATK04B.WAV | 45 | R5SAK08A.WAV |
  | 8 | R5ATK01C.WAV | 31 | R5SAK01A.WAV | 46 | R5SAK08B.WAV |
  | 9 | R5ATK01D.WAV | 32 | R5SAK01B.WAV | 51 | R5ESE001.WAV |
  | 10 | R5ATK02A.WAV | 33 | R5SAK02A.WAV | 63 | R5ESE013.WAV |
  | 11 | R5ATK02B.WAV | 34 | R5SAK02B.WAV | 70 | PDEMO003.WAV |
  | 12 | R5ATK02C.WAV | 35 | R5SAK03A.WAV | 74 | R5ESE024.WAV |
  | 13 | R5ATK02D.WAV | 36 | R5SAK03B.WAV | 80 | R5ESE030.WAV |
  | 14 | R5ATK03A.WAV | 37 | R5SAK04A.WAV | 89 | R5ESE039.WAV |
  | 15 | R5ATK03B.WAV | 38 | R5SAK04B.WAV | 90 | R5ESE040.WAV |
  | 16 | R5ATK03C.WAV | 39 | R5SAK05A.WAV | 101-105 | R5ESE051..055.WAV |
  |  |  | 40 | R5SAK05B.WAV | 108-111 | R5ESE058..061.WAV |

  The `R5ESEnnn` names follow `n - 50` (e.g. 109 → `R5ESE059.WAV`). All are PCM mono 22050 Hz,
  8-bit except 45, 46 and 70 (16-bit).
- Numbers the scenario uses (literal values only, `pcm_calls.py` and `!Bt_Sfx* : n!`
  assignments): battle `SM`/`SQ` via `Bt_SfxAtk1/2`, `Bt_SfxDef1/2` = 0, 5-14, 19-21, 31-44;
  `PCMPLAY.ADV` (`SP RetVal, loop`) = 1, 2, 51, 63, 70, 74, 80, 89, 90, 101-104, 108-111, and
  `SP 5, 1` for `CallArg = 3`. Every used number is linked. 15-18, 45, 46 and 105 were not
  found as literals **[unverified whether they are reachable through computed values]**.
- Semantics relevant to the battle (`FIGTATTK.ADV:338-350`):
  - `SQ l, r, loop` plays WAV `l` on the **left** channel and WAV `r` on the right: both are
    converted to the 44.1 kHz stereo S16 mix format and the other channel is copied sample by
    sample, the shorter one padded with silence (`pcm.sdlmixer.c:104-134`).
    `SQ` with a number < 1 stops playback instead (`cmds.c:308-311`).
  - `loop` = play count, 0 = endless (`music.c:126-129`). Both `SQ` calls in the battle use 1.
  - `SM n` only loads `n` into PCM slot 0 (`pcm.sdlmixer.c:163-184`); the following `SQ`
    replaces that slot, so `SM` is inaudible in xsystem35.
  - 5 = `DUMMY.WAV` (1068 bytes) is the "no sound" value; the second `SQ` runs only when
    `Bt_SfxDef2` or `Bt_SfxAtk2` is not 5 (`FIGTATTK.ADV:349`).
- There is no `.WAI` volume file; xsystem35 then uses the SE volume channel for every slot
  (`pcm.sdlmixer.c:172-178`).

## 5. Music

- `SS n` plays **CD track n + 1**: `muscd_start(num + 1, next_cdrom_loopcnt)`
  (`cmds.c:37-50`); `SS 0` stops. `SL m` sets the play count of the next `SS`, 0 = endless
  (`cmds.c:121-128`; `cdrom.mp3.c:131`).
- The game has no MIDI or BGM archive (no `*MA.ALD`/`*BA.ALD`; with no `*BA.ALD` xsystem35 uses
  its CD back-end, `music_cdrom.c:46-55`). The original 1996 release played CD-DA (track 1 is
  data **[unverified for this edition]**); this distribution (the English fan patch) ships
  `bgm\kichiku_2.mp3` … `kichiku_29.mp3` (28 files, MP3 with ID3 tags) and `_inmm.ini`, a list
  of those 28 paths, one per line, in track order from 2 (`bgm\kichiku_2.mp3` on the first
  line). `_inmm238.exe` is the CD-audio-to-file replacement the readme asks the player to
  install for the original `system39.exe`.
- xsystem35 maps tracks through a playlist: `playlist.txt` (default name,
  `CMakeLists.txt:186`; `-playlist` option or `playlist` in the rc file, `xsystem35.c:236-352`)
  whose **first line is skipped**, or, if it is missing, `_inmm.ini` read from its first line;
  line k (0-based) becomes track k + 2 (`cdrom.mp3.c:51-86`). Hence **`SS n` →
  `bgm\kichiku_{n+1}.mp3`**.
- The scenario uses `SS 0..28` as literals (all of 1-28 except 22; `SS 24` ×186 is the most
  common) plus computed values (`CurrentBgm` ×428, `Tmp0001 - 1` in `FIGT_A/B`). 1-28 covers the
  28 files exactly.
- A player's copy keeps the music next to the game: `<game dir>\bgm\kichiku_N.mp3`, listed in
  `<game dir>\_inmm.ini`. A loader should read `_inmm.ini` rather than hard-code the names.

## 6. Fonts

- No font ships with the game (no `.ttf/.ttc/.otf/.fon` in the game folder). The original
  `system39.exe` contains the GDI face names `ＭＳ ゴシック` and `ＭＳ 明朝` (Shift-JIS bytes found
  once each), i.e. it used the Windows system fonts.
- xsystem35: default type is gothic (`ags.c:138`); the mincho switch `ZZ13` never occurs in the
  scenario, so all text is gothic. Default weight bold (`ags.c:141`), `ZB w` → bold if w > 5
  (`cmdz.c:113-118`; scenario uses `ZB 1` and `ZB 7`, 8 times each).
- Font file: `DEFAULT_GOTHIC_TTF` = `MTLc3m.ttf`, `DEFAULT_MINCHO_TTF` = `mincho.ttf`
  (`config.h.in:17-18`, `xsystem35.c:86`). **On Windows xsystem35 first opens
  `C:/Windows/Fonts/msgothic.ttc` (face index 0 = MS Gothic)** and falls back to the embedded
  `MTLc3m.ttf` (`font.c:108-124`). `MTLc3m.ttf` is the Android Japanese font (Apache 2.0,
  `licenses/MTLc3m.txt`); `mincho.ttf` is an IPA Mincho subset (IPA Font License).
- Size: in pixels, 1 pt = 1 px (`font.c:160-163`). Default message size 16 (`message.c:63`) and
  selection size 16 (`selection.c:96`); `ZM n` sets the message size (`cmdz.c:90-102`), `ZS`
  the selection size. Scenario: `ZM 24` ×1208, `ZM 32` ×1157, `ZM 12` ×22, `ZM 20` ×17,
  `ZM 8` ×15, `ZM 16` ×14, `ZM 14` ×2, `ZS 16` ×1. The battle (`FIGT.ADV`) uses `ZM 16` and
  `ZM 24`. Text colour is a palette index (`ZC 1, 255` etc.).
- `ZH 1` (66,189 times) selects full-width → half-width conversion of message text
  (`nact.c:59-86`, `zen2han`).
- Glyph placement follows GDI: cell top from `usWinAscent` (`font.c:172-183`), synthetic bold
  imitating GDI (`font.c:191-210`).

## Not verified

- How Godot 4.7.2 imports indexed PNGs (index loss, `oFFs` ignored) — check before relying.
- Whether sound numbers 15-18, 45, 46, 105 are reachable at run time.
- Whether the original CD edition had track 1 as data and the same track order.
- Palette agreement was checked only for the battle sets listed in §3; other screens
  (events, maps) may depend on the shared-palette behaviour.
