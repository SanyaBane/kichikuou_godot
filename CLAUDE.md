# KichikuouGodot

A faithful port of **Kichikuou Rance** (AliceSoft, 1996, System 3.x) onto our own engine with a
**Sengoku Rance**–style interface, built in **Godot 4 + C#**. The owner directs; the agent writes
almost all code.

**Read [docs/project-brief.md](docs/project-brief.md) first** — it holds the decisions, the
reasoning, rejected alternatives, unverified facts about the source game, and the next step.

## Status

No code yet. Spike surface analysis done — see [docs/spike-findings.md](docs/spike-findings.md)
for measurements, data model, local paths of the game and tools. Next: decide how to port the
logic (see "Next step" in the brief).

## Rules

- **Faithful port first.** Reproduce the original's mechanics and balance one-to-one; modernise
  only the interface. Do not "improve" rules. A Sengoku-mechanics hybrid comes later, on a
  separate branch, only after the full port.
- **Engine-independent core.** Game rules live in `Kichikuou.Core`, a .NET class library with no
  Godot references, covered by unit tests. The Godot project only renders state and forwards input.
  Never put rules in UI code.
- **Godot 4 only.** Use Godot 4 C# APIs; never Godot 3 idioms (`yield`, old method/signal names).
  When unsure about an API, check the docs for the installed version.
- **Never commit original game assets or extracted data** — the player supplies their own files.
- **Verify before relying.** Facts marked "unverified" in the brief are hypotheses; confirm them
  and update the brief.
- `F:\sources\Rance\alice-tools` is a read-only reference; do not modify it.
- IDE is JetBrains Rider.
