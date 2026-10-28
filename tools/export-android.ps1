<#
.SYNOPSIS
Exports the Android test build (the "Android" preset of godot/export_presets.cfg) as a debug APK, and installs it.

.DESCRIPTION
Runs Godot's export (KICHIKUOU_GODOT_EXE: its environment variable, else its line in local.env) headless with
--export-debug. Godot needs JDK 17 and the Android SDK (platform-tools, build-tools 35.0.1, platforms;android-35)
set in its editor settings (export/android/java_sdk_path, export/android/android_sdk_path); it makes the debug
keystore itself. The export templates 4.7.2 mono must be installed.

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
    # The APK file to write (its folder is created when missing); by default in %LOCALAPPDATA%\KichikuouGodot\android.
    [string]$Out = (Join-Path $env:LOCALAPPDATA 'KichikuouGodot\android\Kichikuou.apk'),
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

$Out = [IO.Path]::GetFullPath($Out)
if (($Out + '\').StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw "The APK must go outside the repository ($root)."
}
$remote = "$remoteFiles/game"

if ($Install -or $PushGame -or $Launch) {
    $script:adb = Get-Adb
    Assert-Device
}

if (-not $NoExport) {
    $exe = Get-LocalPath 'KICHIKUOU_GODOT_EXE'
    if (-not $exe -or -not (Test-Path $exe)) {
        throw "Godot not found: set KICHIKUOU_GODOT_EXE in local.env (see local.env.example)."
    }
    New-Item -ItemType Directory -Force (Split-Path $Out) | Out-Null
    Write-Host "Exporting the Android debug build to $Out ..."
    # As in export-release.ps1: no build servers left holding the console's output open.
    $env:MSBUILDDISABLENODEREUSE = '1'
    $env:UseSharedCompilation = 'false'
    & $exe --headless --path (Join-Path $root 'godot') --export-debug 'Android' $Out
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $Out)) {
        throw "The export failed (exit code $LASTEXITCODE)."
    }
}

if ($Install) {
    if (-not (Test-Path $Out)) { throw "No APK to install: $Out." }
    Write-Host "Installing $Out ..."
    Invoke-Adb @('install', '-r', $Out)
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
