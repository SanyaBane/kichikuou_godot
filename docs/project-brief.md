# Project brief — Kichikuou Rance on a Sengoku Rance–style engine

This document records the decisions made before any code was written (2026-10-07), and the
reasoning behind them. Facts marked **(unverified)** came from memory or forum posts and must be
checked before anything is built on them.

## Goal

Kichikuou Rance (AliceSoft, 1996, System 3.x engine) is a great game with an interface that is
very dated by modern standards. Sengoku Rance (AliceSoft, 2006, System 4.0) has the interface and
strategic-layer feel the owner wants. Porting Kichikuou into the Sengoku engine itself is not an
option (the engine and its tooling are Japanese-only and closed).

So: build our own Sengoku Rance–style engine and import Kichikuou Rance's content into it —
dialogue, CG, music, and the game rules (damage formulas, AI, turn logic) extracted from the
original scenario.

## Scope decisions

1. **Faithful port first.** Kichikuou's mechanics are reproduced one-to-one; only the interface is
   modernised in the spirit of Sengoku Rance. The original's balance is the reference.
2. **Hybrid later, on a separate branch.** A version with Sengoku Rance mechanics and Kichikuou
   content may be started only after the *whole* original has been ported. Design for it now:
   rules live in replaceable modules of the core, never inside UI code.
3. **Remaster / remake is a possible future goal** (videos, animated characters, effects, heavy
   high-res assets, maybe Steam). This is why a real game engine was chosen over a web stack.

Developer model: the owner directs; an AI agent writes ~99% of the code.

## Technology decision: Godot 4 + C#

Chosen: **Godot 4 (.NET build) with C#**, desktop target. IDE is JetBrains Rider.

### Architecture rule: engine-independent core

- **`Kichikuou.Core`** — a plain .NET class library with **no Godot dependency**: world model
  (nations, provinces, commanders, troops), turn rules, battle, AI, event interpreter, save/load.
- **Godot project** — presentation and input only: map, commander cards, dialogue, animation.
  It calls the core and renders its state.
- Core is covered by fast unit tests that run without the engine.

Why: tests run in seconds; golden tests against the original (see below) become plain unit
tests; the hybrid becomes a module swap; the presentation layer can be replaced without touching
the rules.

### Alternatives considered and rejected

| Option | Why not |
|---|---|
| Godot + GDScript | Weakest for an agent-written project: less training data, agents mix Godot 3 and Godot 4 APIs (`yield` vs `await`, renamed methods/signals), weaker static typing, tests need the engine runtime. |
| TypeScript + web (PixiJS + HTML/CSS UI) | Strong runner-up: best agent feedback loop, native browser, excellent UI and video. Rejected because a future remake favours a game engine (shaders, particles, 3D, heavy assets, Steam integration). |
| Unreal | Overkill for 2D; binary `.uasset`, Blueprint/UMG are opaque to an agent. |
| Unity | No advantage over Godot; heavy scene/prefab files; licensing history. |
| Ren'Py | Fine for visual-novel parts, painful for the strategy layer. |

### Known Godot limitations (checked 2026-10-07)

- **No web export for C# projects in Godot 4**, including 4.7. Godot devs give no timeline;
  community expects 4.8 at the earliest. Acceptable: the target is desktop. If web ever becomes
  necessary, the engine-free core could be compiled to WebAssembly separately.
- **Video:** built-in playback is Ogg Theora only **(unverified for 4.6/4.7)**. Modern codecs
  need a third-party FFmpeg-based extension. Matters only for a remake.

## What is known about the source game

The spike's surface analysis (2026-10-07) confirmed most of this; details, measurements and
local paths are in [spike-findings.md](spike-findings.md).

- AliceSoft released Kichikuou Rance as freeware. **(unverified)**
- **Source is the English fan translation only.** The Japanese original is out of scope. Write
  code so that localisation stays possible later, without going out of the way for it.
- It runs on **System 3.9** (confirmed). **xsys35c 1.13** (`xsys35dc`) decompiles the scenario
  cleanly; xsystem35 2.20 supports the English version.
- Resources are in `.ALD` archives: 1222 PMS CGs (8 bpp), 51 WAV effects; music is 28 MP3
  tracks played through `_inmm` (confirmed). `alice-tools` 0.13 and xsys35c's `ald`/`pms`
  unpack and convert them. `F:\sources\Rance\alice-tools` stays a read-only reference.
- Original resolution 640×480, 256 colours. **(unverified)**
- **Game logic lives in the scenario script**, not in the executable: no DLLs, no library calls;
  battle formulas, AI, turn order and event conditions are all readable (confirmed).

## Complexity assessment

| Part | Difficulty | Notes |
|---|---|---|
| Unpacking CG and music | Low | Existing tools; a converter to PNG/OGG is quick. |
| Dialogue text | Low–medium | Interleaved with CG/choice/flag commands. |
| **Event conversion** | **High** | System 3 scenario is imperative bytecode with jumps and flags mixed with UI calls. Needs either a translator into our own event format or an interpreter for the used opcode subset. The core of the whole project. |
| Formulas and balance | Medium | Extractable exactly; faithful port avoids rebalancing. |
| Strategic engine | Medium–high | See below. |
| Sengoku-style UI | Medium–high | Design decisions are the owner's; visual polish needs human eyes. |
| 640×480 / 256-colour art | Medium | Upscaling, frames, new HUD. |

Why the strategic engine is still hard in a faithful port (rules are given, not designed):

1. **No engine separate from the script in the original.** Rules, battle, AI, events and screen
   drawing are one script sharing global numeric variables. They must be reverse-engineered and
   split into model / turn rules / battle / AI / events. Probably the hardest intellectual work.
2. **Event ↔ game-state interface.** Events mutate the world (grant/kill commanders, change
   province owners, unlock actions). The full list of operations is known only after reading the
   whole scenario.
3. **Many interacting systems** — bugs live at the seams (a commander dies mid-turn with queued
   actions, etc.).
4. **Proving fidelity.** Plan: instrument xsystem35 to log game state, capture golden
   input→output pairs from the real game, replay them as unit tests against the core. RNG must
   either be reproduced exactly or tests made statistical — decide during the spike.
5. **Save format** for a large, long-lived campaign state — design for versioning from day one.

## Rough timeline (evenings, with an agent)

- **Spike, 1–3 days** — unpack the game, decompile the scenario, measure event volume, read how
  battle and AI are implemented. This turns the estimates above into measurements.
- **Vertical slice, 1–2 months** — map, one nation's turn, battle, ~10 events, save/load.
- **Full game** — six months or more; the bottleneck is event volume and branching, not engine code.

## Risks

- Motivation fade mid-project → ship playable slices early.
- Rights: fine for personal use. For any public release, distribute only our engine; the player
  supplies their own game files (the xsystem35 model). **Never commit original game assets.**
- "Engine for the engine's sake" → convert events first, polish later.

## Cheap alternative (noted, not chosen)

Decompile with xsys35c, fix the worst screens in the original scenario, recompile — a
quality-of-life patch in weeks, but capped by System 3 limits (resolution, palette).

## Next step

The spike's surface analysis is done (see [spike-findings.md](spike-findings.md)): game files
live outside the repo; System 3.9 decompiles cleanly; ≈650–700 scenes, 126k message lines,
66 distinct commands; battle, AI and turn order located; RNG is MT19937 in xsystem35, seeded
from the process id.

**Decided (2026-10-07): hybrid.** Dialogue/event files go through an automatic converter into
our own event format; rule files are hand-ported to C# in `Kichikuou.Core` and verified by golden
tests. Work order: variable dictionary ([variables.tsv](variables.tsv)) → solution skeleton
(saturating 16-bit integer type, MT19937-1998 port) → battle as the first rules slice → golden-test
harness on a patched xsystem35; an event-converter prototype runs in parallel with the battle slice.

The options that were weighed:

- Dialogue/event files (`G*`, `I*`, ≈85% of the scenario) are linear text plus flags, CG and
  BGM commands — a natural fit for an automatic converter into our own event format.
- Rule files (`MAIN`, `COMMAND`, `C*`, `T*`, `FIGT*`, `SET_*`, ≈1 MB) can either be
  hand-ported to C# (clean model, swappable modules, verified by golden tests) or run through an
  interpreter for the small command subset (fastest fidelity, but the script also drives the
  old screens).

Still to decide: how golden tests get deterministic RNG (patched xsystem35 with a fixed seed vs.
recorded `Y 4` draws).
