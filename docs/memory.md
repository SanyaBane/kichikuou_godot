# Memory on Android

Owner, 2026-11-01/02: the Android build uses far too much memory for a 640×400 8-bit game, 500 MB being what the owner
expects at most. Is that Godot's price or ours? **Mostly ours.** An empty Godot C# project costs 355 MB on the phone (328 MB
as a release build); the game cost 1.9 GB after one battle and 2.9 GB after a few minutes of play, and most of that came
from a handful of things in our own code. No old game stays alive after a Restart.

Cuts 1–4 of [the plan](#the-reduction-plan) are built: **~1.2 GB after a battle, ~1.3 GB in a long session, 584 MB at the
title** ([as built](#the-cuts-as-built)). What is left the owner picks from the plan. The sections from "Where the memory
goes" to "Growth" describe the game before the cuts unless they say otherwise.

## How it was measured

Xiaomi Poco X6 Pro, 2026-11-02: 12 GB RAM, Android 16 / HyperOS, Mali-G615, Godot 4.7.2 Vulkan Forward Mobile, .NET 9 on Mono,
the debug export, held upright unless a line says otherwise. **PSS** is `dumpsys meminfo`'s total. Its parts: *native*
(Native Heap), *graphics* (GL and EGL mtrack, the GPU's memory), *unknown* (mostly Mono's managed heap and JIT), *code*.
*static*, *tex*, *buf*, *video* are Godot's `Performance` monitors (`static` counts in a debug build only).

The scenario is the kept week-18 save played unattended, as CLAUDE.md's "A look at one screen" does: `--load --auto --seed 2`
to round 2 of the attack on Oaks (6), so it passes the map, Subordinates, the deployment table and the battle. Two runs of one
build agreed within 3 MB (913 and 910). Unattended play's screen exercises were off (`--no-exercises`): on the phone held
upright they waited on the map without end. A phone held landscape was not run; the PC's table below shows its layout.

The tools were kept out of the repository (owner's rule on diagnostics in the shipped build):
`%LOCALAPPDATA%\KichikuouGodot\memprobe.patch` on commit 386a68b is a temporary `godot/Strategy/MemProbe.cs` (samples every
5 s, a file of commands, the launch's arguments from a file) with a walker of what our fields hold, and
`%LOCALAPPDATA%\KichikuouGodot\memprobe-tools` holds the adb scripts. The empty project is
`%LOCALAPPDATA%\KichikuouGodot\floor`, exported under the game's package name because MIUI refuses a new package over USB
without a tap on the phone. `memprobe-built.patch`, next to it, is the probe with the built cuts in and a temporary
`--orient upright|landscape` that pins the orientation for a launch: unpinned, the phone's sensor decides, and the phone on
the desk chose differently from one launch to the next. Only the runs in [the cuts as built](#the-cuts-as-built) are pinned.

## The cuts as built

Measured 2026-11-02 with the built code (the first three rows' "before" as under "Where the memory goes", the fourth's the
restarts' table), pinned as a row says, in MB. A battle loaded directly is the scenario above; **through the title** is the
same game opened from the title in one launch (`restarts.ps1`), which is what a player does.

| Point on the phone | Before | Cuts 1–4 | Native | Graphics | Unknown | static | tex | buf | video |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| The title | 744 | **584** | 172 | 170 | 126 | 55 | 31 | 6 | 66 |
| A battle's round 2, loaded directly, upright | 1874–1920 | **912–915** | 295 | 302 | 197–199 | 159 | 163 | 6 | 222 |
| The same, landscape | not run | **911–922** | 289 | 306–319 | 199 | 154 | 142 | 6 | 184 |
| A battle's round 2, through the title, upright | 2062–2244 | **1204–1210** | 311–318 | 557–559 | 214 | 168 | 169–175 | 6 | 228–234 |
| Long session, upright | 2928 | **1279–1285** | 362 | 573–576 | 225–228 | 222 | 201 | 6 | 260 |
| Long session, landscape | not run | **1256** | 336 | 575 | 228 | 265 | 181 | 6 | 223 |

- **The baked backdrop saves what the first measurement promised.** The temporary switches gave 913 MB for the battle and
  1292 MB for the long session, leaving the pattern out; baked it is 912–915 and 1279–1285. The tiles are 50 kB, `buf` stays
  at 6 MB from the title on, and the steps of `static` per screen are gone.
- **Restarts:** battle 1204, 1209, 1210; the title after them 1114, 1112, 1114 after a forced collection (1470–1652 before the
  cuts). It levels off after the first round, as before.
- **Frame times** did not change: in the held battle mean 9.5–11.5 ms, p95 13–16 ms, max 86–150 ms.
- **`ArmySlide`'s images:** disposed in the pinned runs, not in the first unpinned ones (915, 1279 and 1285 MB). `static_max` is
  the same in both, 173.0 in the battle and 270.0 in the long session, so they do not make these peaks.

The 292 MB between the two upright battles is mostly the GPU's next block (graphics +256), which the title's textures push
them across: the next section.

## The GPU's memory goes in steps of up to 256 MB

Graphics does not follow Godot's `video`: at `video` 222 it is 302 MB, at 223 it is 575 MB. With `dumpsys` once a second over a
long session the total jumps by +64 MB, ~+130 MB (net of EGL's fall) and at last +260 MB, each within a second, the last in the
second the battle's textures arrive (`tex` +73 MB, `video` to 241). The steps fit VMA, Godot's Vulkan allocator, which takes
blocks of growing size up to 256 MB, 224 MB in all before the first of that size (the library's sizes as remembered, the
steps as measured). The runs put the edge between `video` 221.5 (no block) and 223.3 (the block); the next would come near 480.

So **a run over ~222 MB pays 256 MB at once, for a few MB of use.** That is most of the difference between a battle loaded
directly (222: 912 MB) and one through the title (228–234: 1204 MB), and 256 of a long session's 1280. A battle loaded directly
has 1 MB to spare, so its figure is luck, not a level. The block stays while anything is allocated in it: the title after a
Restart keeps graphics at 557 MB with `video` at 181.

Godot 4.7.2 has no setting for the block size (`ProjectSettings`, `rendering/rendering_device/`), so the way out is using less.
The largest parts of `video` in the held battle, upright: the glyph atlases 45 MB, `BoardStones` 36, `UprightMapView` 17 (its
pictures 11, its texture 6), the screen's and the battle's seven pictures ~20, ~14 of the empty project's textures, and ~50 that
is neither `tex` nor `buf` and not traced (~36 landscape; 6 in the empty project).

**A turn of the phone** adds to it. Turned to landscape on the map: 913 MB, nothing paid (`video` 182). Turned in the battle: 1153
MB (graphics 558, `video` 209 afterwards, at least 219 at the turn), since the old layout's textures and the new one's live
together for a moment and the block then stays.

The launch that first showed all this (1206 MB, `video` 242, not repeated) also waited 172 s between Godot's start and its
first script, cause unknown: not the shaders (nothing in `shader_cache` or `vulkan` was written then) and not the install (two
later installs started at once). The phone's screen may have been off; nothing was logged to tell.

## Where the memory goes

| Point on the phone | PSS | Native | Graphics | Unknown | Code | static | tex | buf | video |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Empty project (Godot + Mono) | 355 | 109 | 122 | 5 | 82 | 36 | 14 | 6 | 26 |
| Empty project, release export | 328 | 102 | 122 | 5 | 81 | | 14 | 6 | 26 |
| Empty project, GL Compatibility | 313 | 95 | 92 | 5 | 85 | 30 | 25 | 6 | 32 |
| The title | 744 | 307 | 174 | 137 | 88 | 186 | 31 | 6 | 66 |
| The save loaded, on the map | 1118 | 431 | 414 | 154 | 82 | 269 | 49 | 33 | 170 |
| A battle's round 2 | 1874–1920 | 782–801 | 775–794 | 205–212 | 76 | 553 | 174 | 165 | 503 |
| Long session (below) | 2928 | 996 | 1284 | 219 | 66 | 1008 | 248 | 335 | 747 |

All in MB. The owner's earlier figures, 1.88 GB and 3.09 GB (`am_pss`), match the last two rows.

**The PC** runs the same scenario (`static`, `tex`, `buf`, `video` at its end) and shows the phone's excess is not the
platform's: the same layout costs about the same there.

| PC window | static | tex | buf | video |
| --- | --- | --- | --- | --- |
| 1280×800 | 291 | 87 | 47 | 170 |
| `--phone landscape` | 429 | 147 | 88 | 383 |
| `--phone upright` | 593 | 109 | 207 | 393 |

## What holds it

### The wave backdrop: ~95 MB of native and 66 MB of GPU buffers per screen

`UiPalette.DrawWaveBackdrop` draws the faint seigaiha pattern as a filled circle and three antialiased arcs of 40 points for
every wave, each a polygon of its own with its own vertex buffer in Godot. The track of `static` and `buf` shows one step per
screen that draws it, upright: +81 to +102 MB of `static` and +65.7 MB of `buf`, in one frame, kept for as long as its layer
is not redrawn, hidden or not. The battle run has two steps (Subordinates, the deployment table); the long session has three
more, at the visits to Harem, Treasure and Save/Load (matched by time). Options, the enemy attack, All Data and the import
screen call it too (read, not run). **This is the growth in a long session:** each new screen adds a step.
Without the circles (`--no-waves`) the battle scenario falls by ~528 MB, ~264 MB a backdrop with the driver's share.

### Stray copies of the fonts: 161 MB

Each view loads its own `FontFile`: 103 of them at a battle, 27 being BIZ UDGothic at 4.56 MB each, seen as 27 native blocks
of exactly 4560 kB. Each has its own glyph cache and atlases too, on the CPU and again on the GPU. One `FontFile` per path
(`--share-fonts`) takes PSS from 1874 to 1713 MB, (font, size) pairs from 251 to 105 and the atlases from 59 to 44 MB.

A size itself is cheap in the font engine: 40 more sizes of BIZ UDGothic cost 15.7 MB, ~390 kB each, nearly all of it the
atlas (the same again on the GPU). The fits that try size after size are what make the sizes many: one view's list is
`16,8,29,48,52,36,35,34,33,51,50,49,47,46,32,31`.

### The staging buffers: 276 MB of GPU memory

Godot's upload buffers may grow to `rendering/rendering_device/staging_buffer/max_size_mb`, 128 MB by default. With 16 the
battle's graphics fall from 770 to 494 MB. That is one line in `project.godot`, and the battle's frame times did not change
(mean 9.7–11.3 ms, p95 13–17 ms, max 97–145 ms, with and without).

### The uploads' images: swings, not a level

Every changed picture makes a 4 MB `Image` (`GameScreen`'s view, `BattleBoardView`'s windows and scene layers) that nobody
disposes, so the native copy lives until the collector happens to finalize the small C# wrapper. `static` climbs by 3.9 MB a
frame in a fade and drops by up to 277 MB at once at a collection (long session). `image.Dispose()` after the upload, tried at
`GameScreen`'s and `BattleBoardView.Upload` alone, takes `static_max` from 463 to 414 MB in the battle run (fonts shared in
both), while the level at the held moment stays the same.

### The managed heap: 100 MB in two surfaces

Of the title's ~125 MB, the walker finds two arrays of 39 + 20 MB (10.24 M pixels: the engine's surface by its size, 6400×1600,
as RGBA and 16-bit) and two of 26 + 13 MB (6.84 M pixels: the battle scene's, by elimination; not traced to the field), under
`GameScreen._presenter`, and 4 MB buffers such as `GameScreen._frame`. All are there at the title, whether a battle comes or
not. The effect masks are small (43 of them, 5 MB in all); no cache of decoded CGs shows among the large items.

### Smaller ones, by size

| What | MB | Source |
| --- | --- | --- |
| `BoardStones` (2560×2656 and 2560×1016, on the GPU) | 36 | walker |
| `UprightMapView._pictures`: 29 images and 31 textures, each picture on the CPU and on the GPU | 23 | walker |
| The exchange counters' and effects' SubViewports | 4 | walker |
| What the driver holds beyond Godot's `video` count, in a battle | ~280 (floor: ~95) | `dumpsys` − monitors |

## Growth: caches, no leak

**Restarts** (one launch, a game from the save to round 2, Restart, each five times):

| Round | Battle PSS | Title PSS after | After a forced collection |
| --- | --- | --- | --- |
| Start | | 719 | |
| 1 | 2227 | 1582 | 1470 |
| 2 | 2244 | 1764 | 1652 |
| 3 | 2242 | 1770 | 1651 |
| 4 | 2062 | 1583 | 1471 |
| 5 | 2082 | 1596 | 1475 |

It levels off after the first round, and after a Restart the counts come back to the title's: 208 nodes, no orphans, ~2340
objects, 120 MB managed, none of the old screen's nodes reachable from our fields (the static events and registries among
them). **No old game stays alive.** The title stays 750–1050 MB above a fresh one, mostly on the GPU (graphics 748–928 against
171): Godot and the driver keep what they have grown to (`video` 249 against 66, static caches such as `BoardStones`), and the
native heap stays ~120 MB above too (410–470 against 293).

**Long session** (`--fast`, weeks 18–24, with Harem, Treasure, Save/Load, Subordinates and the battles): PSS 2928 MB, and it
reached its plateau within a minute, `static` ~1 GB and `buf` 335 MB, five backdrops' worth. With the first four cuts together
it is 1279–1285 MB ([as built](#the-cuts-as-built)).

## The reduction plan

Cuts 1–4 are built (owner, 2026-11-02; `godot/Strategy/WaveTile`, `Lacquer.LoadFont`, `project.godot`, `image.Dispose()` in
`GameScreen`, `BattleBoardView.Upload` and `ArmySlide`). On the PC they change the backdrop's pixels by at most 4 of 255 and text
not at all; on the Poco the owner saw no seam in the tile (Subordinates, Harem).

Each line was measured on the phone unless it says "by size", cuts 1–4 against 1874 MB. Cut 10 is new: the savings of 5–9
below are what they free on their own, and any of them that brings `video` under the edge adds the block's 256 MB.

| # | Cut | Saves | Effort | Risk |
| --- | --- | --- | --- | --- |
| 1 | The wave backdrop baked once into one shared texture (built) | ~528 MB in the battle, ~1.3 GB in a long session | Small | Low: the pattern must look the same |
| 2 | The staging buffers' cap at 16 MB (built) | 276 MB | One line | Low: a hitch where a frame uploads more; none seen |
| 3 | One `FontFile` per path (built) | 161 MB | Small | Low |
| 4 | `Dispose` the uploads' images (built) | Peaks only: 49 MB of `static_max` from two sites; swings reach 277 MB | Small | None |
| | **1–4 together, as built** | **912–915 MB in a battle loaded directly, 1204–1210 through the title, 1279–1285 in the long session, 584 at the title** | | |
| 5 | The surfaces stored at 1× where nothing draws at 2× | ~70 MB, by size | Large | Medium: the engine's drawing, its own survey first |
| 6 | Fewer font sizes: fit by one measure and scale | ~60 MB, by size | Medium | Low |
| 7 | `BoardStones` at half size, no CPU copy of `UprightMapView`'s pictures | ~35 MB, by size (27 of it on the GPU) | Small | Low: the stones a little softer |
| 8 | The GL Compatibility renderer | 45 MB (910 → 865), before the block | Small to try, large to verify | Medium: every screen's look to check |
| 9 | The release export instead of the debug one | 27 MB, measured on the empty project | Comes with a release | None |
| 10 | Keep `video` under the edge of ~222 MB, so [the 256 MB block](#the-gpus-memory-goes-in-steps-of-up-to-256-mb) is never taken: −12 MB for a battle through the title, −2 for a long session in landscape, −38 for one upright, more for a margin and a turn of the phone | 256 MB, by the steps seen, not yet tried | Small to medium: cut 7's stones give 27 MB; the rest from cut 6's atlases, `UprightMapView`'s pictures or the ~50 MB not traced | Low |

**Is 500 MB reachable?** Not in a battle with these cuts: through the title 1–4 give ~1205 MB, cut 10 would bring that to ~950,
and 5–9 to ~700 if all their savings add up. The floor leaves 145–170 MB for everything of ours, and the two surfaces alone are
100 MB. The title, at 584 MB with 1–4, could come close. Getting a battle to 500 MB needs more than this list: a redesign of the
surfaces and far fewer textures and font sizes.
