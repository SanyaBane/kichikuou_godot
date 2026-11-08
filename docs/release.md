# Releases

How a release of the port is numbered, built and checked. What a release still lacks is in
[backlog.md](backlog.md), "Release".

## Versions

Since 2026-11-08 the port has one version number, **`application/config/version` in
`godot/project.godot`** (now `0.1`). Everything else takes it from there:

- **The Windows exe**: its file and product version (`0.1.0.0`, Windows wants four parts). The preset
  "Windows Desktop" leaves `application/file_version` and `application/product_version` empty, so Godot
  fills them from the project's version, and `application/modify_resources` is on.
- **The Android APK**: its version name (`0.1`): the preset "Android" leaves `version/name` empty.
  Its **`version/code`** is the one number kept by hand: an integer that grows by one with every APK
  given out (Android installs an update only over a lower code). `0.1` is code 1.
- **The game's log**: every launch starts with `Kichikuou <version> (<debug|release> build, <OS>)`
  (`Main.Version`), so a log or a hang report ([hang-watch.md](hang-watch.md)) names its build.
- **Git**: the commit a release is built from gets an annotated tag `v<version>` (`v0.1`), made when the
  build is published, not before.

The numbers are `major.minor[.patch]`, digits only (Windows refuses anything else, so no `-beta`): a fix
of a published release raises the patch (`0.1.1`), anything else the minor (`0.2`). The version is
raised in the commit that prepares the next release, not after one.

## Building a release

1. **A settled commit**: the work tree clean, the build and every test project green with the full
   counts (`KICHIKUOU_FULL_TESTS=1`, CLAUDE.md, "Commands").
2. **Windows**: `powershell -ExecutionPolicy Bypass -File tools/export-release.ps1 -Out <folder>`, the
   folder outside the repository (~2 minutes). It holds `Kichikuou.exe`, `Kichikuou.pck`,
   `data_Kichikuou_windows_x86_64\` (the .NET assemblies), `LICENSE` and `licenses\` (the fonts' OFL
   texts); all of it together is the build. The download is that folder zipped as
   `Kichikuou-<version>-windows-x64.zip`.
3. **Android**: a release APK, `Kichikuou-<version>.apk`. Not scripted yet:
   `tools/export-android.ps1` makes the debug APK, signed with Godot's debug key (backlog.md, "Release").
4. **The checks** below, on the files that will be published.
5. **The tag** `v<version>` on the commit, pushed, and the files published with their SHA-256 and a
   short list of what is in the release.

Godot's Android exporter runs adb in any export, the Windows one too; the export scripts start adb's
server themselves first (`Start-AdbServer`, `tools/android-adb.ps1`), else the server Godot starts holds
the console's output open and the script never ends (2026-11-08).

## Checks before publishing

- **Windows, a first launch**: with `%APPDATA%\KichikuouGodot` moved aside (the build's own data folder;
  without it the launch is a new player's), the exe started from a folder outside the repository shows the
  import screen, imports the game folder, and a new game and a load play. The log
  (`%APPDATA%\KichikuouGodot\logs\godot.log`) names the version.
- **Android**: the release APK installed on a device without the app, the game folder copied into the
  app's folder (`Android/data/org.kichikuou.port/files/game`, what a player does by USB), the first start
  imports it, a new game and a load play, both orientations.
- **The README's "Installing"** matches the build: where to get it, what the player copies where.
