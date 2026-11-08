<#
.SYNOPSIS
Exports the Android build (the "Android" preset of godot/export_presets.cfg) as an APK, the debug one by default, and
installs it.

.DESCRIPTION
Runs Godot's export (KICHIKUOU_GODOT_EXE: its environment variable, else its line in local.env) headless with
--export-debug. Godot needs JDK 17 and the Android SDK (platform-tools, build-tools 35.0.1, platforms;android-35)
set in its editor settings (export/android/java_sdk_path, export/android/android_sdk_path); it makes the debug
keystore itself. The export templates 4.7.2 mono must be installed.

-Release exports the release APK instead (--export-release), signed with the owner's release key: the keystore
KICHIKUOU_ANDROID_KEYSTORE in local.env, its alias KICHIKUOU_ANDROID_KEY_ALIAS (else "kichikuou"), its password asked
once (or GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD) and tried before the export (android-key.ps1); the APK's signer is
checked after it. Its default file is Kichikuou-<version>.apk. A device with the debug build refuses it as an update
(another key): the error says what to do. docs/release.md.

-Install puts the APK on the one device adb sees (USB debugging on; with several, ANDROID_SERIAL names one), keeping
the app's data, and copies the game folder too when the device has none. -PushGame copies the player's game folder
(KICHIKUOU_GAME_DIR) into the app's own external folder, where the build looks for it
(GameData.AndroidGameFolder): no permission is needed there, and the app imports it at its first start; uninstalling
the app deletes it. -Launch starts the game on the device. The device is checked before the export, so a missing one
fails at once.

run-android.cmd at the root is -Install -Launch with the default APK, for a double-click.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\export-android.ps1 -Install -Launch
.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\export-android.ps1 -Out D:\Builds\Kichikuou.apk -Install -PushGame
#>
param(
    # The APK file to write (its folder is created when missing); by default in %LOCALAPPDATA%\KichikuouGodot\android,
    # Kichikuou.apk, or Kichikuou-<version>.apk with -Release.
    [string]$Out = '',
    # The release APK, signed with the release key, in place of the debug one.
    [switch]$Release,
    # Install the APK with adb after the export (and the game folder when the device has none).
    [switch]$Install,
    # Copy the game folder to the device, also when it has one.
    [switch]$PushGame,
    # Start the game on the device at the end.
    [switch]$Launch,
    # Skip the export: install, push or launch what is there.
    [switch]$NoExport
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'android-adb.ps1')
. (Join-Path $PSScriptRoot 'android-key.ps1')

# The local paths, as Kichikuou.Engine.LocalPaths reads them: the environment, else local.env.
function Get-LocalPath([string]$Key) {
    $value = [Environment]::GetEnvironmentVariable($Key)
    if ($value) { return $value }
    $localFile = Join-Path $root 'local.env'
    if (-not (Test-Path $localFile)) { return '' }
    foreach ($line in Get-Content -Encoding UTF8 $localFile) {
        $line = $line.Trim()
        if ($line -match '^([^#=][^=]*)=(.*)$' -and $Matches[1].Trim() -eq $Key) {
            $value = $Matches[2].Trim()
            if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
                $value = $value.Substring(1, $value.Length - 2)
            }
            return $value
        }
    }
    return ''
}

# Whether the app's game folder on the device holds anything.
function Test-RemoteGame([string]$Remote) {
    $listing = & $script:adb shell "ls '$Remote' 2>/dev/null"
    return $LASTEXITCODE -eq 0 -and ($listing | Where-Object { $_.Trim() })
}

# The game's version, application/config/version in project.godot (docs/release.md, "Versions").
function Get-ProjectVersion {
    $line = Select-String -Path (Join-Path $root 'godot\project.godot') -Pattern '^config/version="(.+)"$' | Select-Object -First 1
    if (-not $line) { throw 'No config/version in godot/project.godot.' }
    return $line.Matches[0].Groups[1].Value
}

# adb install, with what to do when the device has the app signed with another key.
function Install-Apk([string]$Apk) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $answer = & $script:adb install -r $Apk 2>&1 | Out-String
    $code = $LASTEXITCODE
    $ErrorActionPreference = $previous
    Write-Host $answer.Trim()
    if ($code -eq 0) { return }
    if ($answer -match 'INSTALL_FAILED_UPDATE_INCOMPATIBLE') {
        throw ('The device has the game signed with another key (the debug build and the release one differ), so it ' +
            'takes this APK only after the game is uninstalled there, which deletes its saves and game folder: copy the ' +
            'saves off first (copy-saves-from-tablet.cmd), uninstall the game on the device, then install again.')
    }
    throw "adb install failed (exit code $code)."
}

if (-not $Out) {
    $name = if ($Release) { "Kichikuou-$(Get-ProjectVersion).apk" } else { 'Kichikuou.apk' }
    $Out = Join-Path $env:LOCALAPPDATA "KichikuouGodot\android\$name"
}
$Out = [IO.Path]::GetFullPath($Out)
if (($Out + '\').StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw "The APK must go outside the repository ($root)."
}
$remote = "$remoteFiles/game"

if ($Install -or $PushGame -or $Launch) {
    $script:adb = Get-Adb
    Assert-Device
}
if ($Release -and -not $NoExport) {
    Set-ReleaseKey -Keystore (Get-LocalPath 'KICHIKUOU_ANDROID_KEYSTORE') -Alias (Get-LocalPath 'KICHIKUOU_ANDROID_KEY_ALIAS')
}

if (-not $NoExport) {
    $exe = Get-LocalPath 'KICHIKUOU_GODOT_EXE'
    if (-not $exe -or -not (Test-Path $exe)) {
        throw "Godot not found: set KICHIKUOU_GODOT_EXE in local.env (see local.env.example)."
    }
    New-Item -ItemType Directory -Force (Split-Path $Out) | Out-Null
    $kind = if ($Release) { 'release' } else { 'debug' }
    Write-Host "Exporting the Android $kind build to $Out ..."
    # As in export-release.ps1: no build servers left holding the console's output open.
    $env:MSBUILDDISABLENODEREUSE = '1'
    $env:UseSharedCompilation = 'false'
    Start-AdbServer     # and adb's server started apart from Godot's console (android-adb.ps1)
    $started = Get-Date
    & $exe --headless --path (Join-Path $root 'godot') "--export-$kind" 'Android' $Out
    # An APK left from an earlier export is no proof of this one.
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $Out) -or (Get-Item $Out).LastWriteTime -lt $started) {
        throw "The export failed (exit code $LASTEXITCODE)."
    }
    if ($Release) { Assert-ReleaseSigned $Out }
}

if ($Install) {
    if (-not (Test-Path $Out)) { throw "No APK to install: $Out." }
    Write-Host "Installing $Out ..."
    Install-Apk $Out
    if (-not $PushGame -and -not (Test-RemoteGame $remote)) {
        Write-Host 'The device has no game folder yet.'
        $PushGame = $true
    }
}
if ($PushGame) {
    $game = Get-LocalPath 'KICHIKUOU_GAME_DIR'
    if (-not $game -or -not (Test-Path $game)) {
        throw "The game folder is not found: set KICHIKUOU_GAME_DIR in local.env."
    }
    Write-Host "Copying $game to $remote ..."
    Invoke-Adb @('shell', 'mkdir', '-p', $remote)
    # The folder's contents, not the folder itself, into the remote one.
    Invoke-Adb @('push', (Join-Path $game '.'), $remote)
    Grant-AppAccess $remote
}
if ($Launch) {
    Write-Host "Starting $package ..."
    # Stopped first, so a running game starts again from the new build.
    Invoke-Adb @('shell', 'am', 'force-stop', $package)
    & $script:adb shell monkey -p $package -c android.intent.category.LAUNCHER 1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The game did not start on the device (adb monkey, exit code $LASTEXITCODE)." }
}
Write-Host 'Done.'
