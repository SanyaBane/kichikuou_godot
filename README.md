# KichikuouGodot

## About

KichikuouGodot is a port of **Kichikuou Rance** (AliceSoft, 1996) from its original System 3.9
engine to a new engine of its own, written in C# on **Godot 4**. The first aim is a port as close
to 100% faithful to the original as possible. The second is to add modern quality-of-life features
that make the game more convenient to play.

- **Faithful first.** The game's rules are reproduced one-to-one from the original scenario
  script: the battle formulas, the nations' AI, the week loop, the events, the endings. The
  original's bugs and oddities are kept as they are and listed in [docs/quirks.md](docs/quirks.md),
  so that some can be offered later as optional fixes. Convenience features come on top of the
  original game and leave its rules unchanged.
- **Checked against the original.** A patched build of xsystem35 records sessions played in the
  original game. The port replays the same runs from the same memory, with the same random draws
  and inputs, and must end with the same memory, variable by variable
  ([docs/golden-tests.md](docs/golden-tests.md)).
- **No game data in the repository.** The player supplies their own copy of the game. The port
  reads the scenario, pictures, sounds and music from the player's files at run time. The version
  ported is the English fan translation (v1.0).
- **An engine-independent core.** The rules live in `Kichikuou.Core`, a plain .NET library with no
  Godot dependency, covered by unit tests. `Kichikuou.Content` reads the player's data and
  emulates the original's drawing surface. The Godot project only draws the state and forwards
  input.

This is an unofficial fan project. It is not affiliated with or endorsed by AliceSoft, and it
contains no part of the original game.

## Credits

**The game**

- **AliceSoft** made *Kichikuou Rance* (1996). Its story, characters, art, music and design belong
  to AliceSoft.

**The English translation** (Kichikuou Rance English Translation v1.0). It is the version this port
reproduces, and every line of text the port shows is theirs.

- **Arunaru**: translation.
- **Pi-R**: editing.
- **Vodka**: image editing.
- **SomeLoliCatgirl** and **Toolbox**: hacking tools and assistance.

**The engine and its tools**

- **[xsystem35-sdl2](https://github.com/kichikuou/xsystem35-sdl2)** by **kichikuou** is a
  multi-platform port of **xsystem35**, the free implementation of AliceSoft's System 3.x engine
  by **Masaki Chikama (Wren)**, with **Fumihiko Murata**, **TAJIRI Yasuhiro** and other
  contributors. It is our reference for how the engine behaves, and a patched fork of it records
  the captures for the golden tests.
- **[xsys35c](https://github.com/kichikuou/xsys35c)** by **kichikuou** decompiled the scenario
  that the port reads, and its tools unpack the game's archives and pictures.
- **[alice-tools](https://github.com/nunuhara/alice-tools)** by **Nunuhara Cabbage** (with
  contributions from kichikuou) unpacks and converts AliceSoft's file formats. It was our
  reference for those formats.
- **Makoto Matsumoto** and **Takuji Nishimura** wrote the Mersenne Twister. xsystem35 draws the
  game's random numbers with their 1998 reference code (`mt19937-1.c`). The port reproduces that
  code exactly, so for the same seed its random draws match xsystem35's.

**Built with**

- **[Godot Engine](https://godotengine.org)** 4 (.NET), created by Juan Linietsky and Ariel
  Manzur and developed by the Godot community.
- **.NET** and **C#**, **xUnit.net** for the tests, **Python** for the analysis tools, and
  **MSYS2**, **CMake** and **Ninja** to build the harness.
- **MS Gothic** (Microsoft), the game's font. It is read from the Windows fonts and not bundled.
- **Claude Code** (Anthropic), the AI agent that writes most of the code under the project
  owner's direction.
