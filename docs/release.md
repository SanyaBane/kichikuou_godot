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

## The Android release key

The release APK is signed with the owner's own key (made 2026-11-08 with `keytool` from the `bin` folder
of JDK 17, the JDK Godot's editor settings name in `export/android/java_sdk_path`; it is not on the PATH).
The keystore lies outside the repository, backed up: every later APK must be signed with the same key,
or Android refuses it as an update of the installed game. `local.env` names it,
`KICHIKUOU_ANDROID_KEYSTORE` (its alias `KICHIKUOU_ANDROID_KEY_ALIAS`, else `kichikuou`). Its password is
written nowhere: the scripts ask for it once a run and open the key with `keytool` before the export, and
pass it to Godot in `GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD` (set that variable to skip the question).
After the export they check that the APK's signer is that key (apksigner; `tools/android-key.ps1`).

The debug build (`run-android.cmd`) stays signed with Godot's debug key, so a device holding it refuses
the release APK as an update, and the other way round: the game has to be uninstalled first, which deletes
its saves and game folder on the device — copy the saves off first (`copy-saves-from-tablet.cmd`).

## Building a release

1. **A settled commit**: the work tree clean, the build and every test project green with the full
   counts (`KICHIKUOU_FULL_TESTS=1`, CLAUDE.md, "Commands"), `version/code` raised in the Android preset
   when an APK of an earlier version was given out.
2. **`release.cmd`** at the root (a double-click), or
   `powershell -ExecutionPolicy Bypass -File tools/release.ps1 [-Out <folder>]`: it asks for the release
   key's password, then builds both downloads into `%LOCALAPPDATA%\KichikuouGodot\release\<version>` (or
   `-Out`, outside the repository), ~5 minutes:
   - `Kichikuou-<version>-windows-x64\` — the Windows build (`tools/export-release.ps1`): `Kichikuou.exe`,
     `Kichikuou.pck`, `data_Kichikuou_windows_x86_64\` (the .NET assemblies), `LICENSE` and `licenses\`
     (the fonts' OFL texts); all of it together is the build;
   - `Kichikuou-<version>-windows-x64.zip` — that folder zipped, the download;
   - `Kichikuou-<version>.apk` — the release APK (`tools/export-android.ps1 -Release`);
   - `SHA256SUMS.txt` — the two downloads' SHA-256.

   It refuses a work tree with changes not committed (`-AllowDirty` for a trial) and a folder that holds
   that version's files already; `-NoWindows` / `-NoAndroid` leave one part out. The release APK alone:
   `tools/export-android.ps1 -Release [-Install]`.
3. **The checks** below, on the files that will be published.
4. **The tag** `v<version>` on the commit, pushed, and the files published with `SHA256SUMS.txt` and a
   short list of what is in the release.

Godot's Android exporter runs adb in any export, the Windows one too; the export scripts start adb's
server themselves first (`Start-AdbServer`, `tools/android-adb.ps1`), else the server Godot starts holds
the console's output open and the script never ends (2026-11-08).

## Checks before publishing

- **Windows, a first launch**: with `%APPDATA%\KichikuouGodot` moved aside (the build's own data folder;
  without it the launch is a new player's), the exe started from a folder outside the repository shows the
  import screen, imports the game folder, and a new game and a load play, the menu bar without Debug (a
  release build has none). The log (`%APPDATA%\KichikuouGodot\logs\godot.log`) names the version and
  "release build".
- **Android**: the release APK installed on a device without the app, the game folder copied into the
  app's folder (`Android/data/org.kichikuou.port/files/game`, what a player does by USB), the first start
  imports it, a new game and a load play, both orientations.
- **The README's "Installing"** matches the build: where to get it, what the player copies where.
