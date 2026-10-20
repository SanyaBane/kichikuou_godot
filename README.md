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
- **An engine-independent core.** The rules live in `Kichikuou.Original`, a plain .NET library with
  no Godot dependency, covered by unit tests, on `Kichikuou.Engine`, the runtime any System 3.9
  game shares. `Kichikuou.Content` reads the player's data and emulates the engine's drawing
  surface; `Kichikuou.Original.Content` draws the original's screens on it. The Godot project only
  draws the state and forwards input.

This is an unofficial fan project. It is not affiliated with or endorsed by AliceSoft, and it
contains no part of the original game.

## Installing

**What you need: your own copy of the game with the English translation.** The port reads one version,
Kichikuou Rance with the **Kichikuou Rance English Translation v1.0** applied: a folder holding
`system39.exe`, `KICHIKUSA.ALD`, `KICHIKUGA.ALD`, `KICHIKUGB.ALD`, `KICHIKUWA.ALD`, `System39.ain`,
`_inmm.ini` and the music in `bgm\`. Getting the game and the translation is up to you; none of their
files are here, and nothing else of theirs has to be installed or run (the `_inmm` music driver neither).

There are no release builds yet. To build and play from the source (Windows):

1. Install the **.NET 10 SDK** and **Godot 4.7.2 .NET** (the portable build will do).
2. Copy `local.env.example` to `local.env` and set `KICHIKUOU_GODOT_EXE` to Godot's console executable.
3. Run `run-game.cmd` (it builds the game and starts it in a window).

**The first start imports your game.** The game asks for the game folder (the system's folder dialog),
checks that its files are the translation v1.0 (by their SHA-256; another version or a missing file is
refused with the file's name), and reads the scenario once into its own data folder
(`%APPDATA%\Godot\app_userdata\Kichikuou\import\` on Windows), which takes about a second. The pictures,
the sounds and the music are read from your folder as you play, so keep it where it is; choosing it again
is only needed if it moves. The details are in [docs/import.md](docs/import.md).

Developers can point the game at a folder of data extracted with xsys35c's tools instead
(`KICHIKUOU_EXTRACT_DIR`, [docs/spike-findings.md](docs/spike-findings.md), "Local layout"); the tests read
that one.

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
- **[xsys35c](https://github.com/kichikuou/xsys35c)** by **kichikuou**: its decompiler `xsys35dc`
  (1.13) is translated into the port, which decompiles the player's scenario with it at the first
  start; its Shift JIS table and its readers of the archives and of `System39.ain` are translated too,
  and its tools made the extract the port is developed and tested on.
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
- **Claude Code** (Anthropic), the AI agent that writes most of the code under the project
  owner's direction.

**Fonts**

- **MS Gothic** (Microsoft), the game's font. It is read from the Windows fonts and not bundled.
- **[BIZ UDGothic](https://github.com/googlefonts/morisawa-biz-ud-gothic)** by **Morisawa** and
  **Type Bank**, the font of the game and of our own screens where MS Gothic is not installed (or when
  chosen in Option → Fonts). Bundled under the SIL Open Font License 1.1 (`godot/fonts/OFL.txt`).
- **[Shojumaru](https://fonts.google.com/specimen/Shojumaru)** by **Astigmatic**, the armies' names in
  the battle's opening. Bundled under the SIL Open Font License 1.1 (`godot/fonts/Shojumaru-OFL.txt`).
- **[Cinzel](https://github.com/NDISCOVER/Cinzel)** by **Natanael Gama** and the Cinzel Project
  Authors, the plate of the battle's opening. Bundled under the SIL Open Font License 1.1
  (`godot/fonts/Cinzel-OFL.txt`).
- **[Oswald](https://github.com/googlefonts/OswaldFont)** by **Vernon Adams** and the Oswald Project
  Authors, the exchange's troops and HP counters on our battle screen. Bundled under the SIL Open Font
  License 1.1 (`godot/fonts/Oswald-OFL.txt`).

## License

KichikuouGodot is free software: you can redistribute it and/or modify it under the terms of the
GNU General Public License as published by the Free Software Foundation, either version 2 of the
License, or (at your option) any later version. It is distributed in the hope that it will be
useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See [LICENSE](LICENSE) for the full text.

A few files contain code translated from xsystem35 (GPL-2.0-or-later), from xsys35c
(GPL-2.0-or-later) and from the Mersenne Twister's reference code `mt19937-1.c` (LGPL-2.0-or-later). The header of each of those files
names what it comes from and who holds the copyright.

The license covers the project's own work only. *Kichikuou Rance* belongs to AliceSoft, and the
English translation's text belongs to its translators; this license grants no rights to either.
None of their files are in this repository: the port needs the player's own copy of the game.
