<#
.SYNOPSIS
Copies the game's saves from the PC to the Android device (-ToDevice) or back (-FromDevice), over adb.

.DESCRIPTION
The saves are files of one format on both (docs/backlog.md, "Android"): the auto-saves auto-NNNN.ksav (a turn's start)
and end-NNNN.ksav (its Turn END), the manual saves save-NNNN.ksav, the persistent page persistent.ksav (the gallery) and the seen messages msgskip.txt. The
Android build keeps them in the app's own external folder (GameData.AndroidSaveFolder); the PC's are the development
runs' (%APPDATA%\Godot\app_userdata\Kichikuou\saves), -Release the release build's (%APPDATA%\KichikuouGodot\saves),
or -PcSaves names a folder.

The source's saves replace the destination's whole (the two sides' saves are numbered apart, so they are not
merged). The destination's saves are never deleted: they move first into a backup on the destination itself, a
folder named by the time in save-backups next to its saves folder (on the device
Android/data/org.kichikuou.port/files/save-backups\<time>, on the PC <the saves folder's parent>\save-backups\<time>);
nothing prunes them (a save is ~60 KB). A source with no saves changes nothing. -ToDevice stops the game on the device
first; close the game on the PC before -FromDevice. Needs USB debugging on (any USB mode).

copy-saves-to-tablet.cmd and copy-saves-from-tablet.cmd at the root run it for a double-click.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\copy-saves.ps1 -ToDevice
.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\copy-saves.ps1 -FromDevice -Release
#>
param(
    # Copy the PC's saves to the device.
    [switch]$ToDevice,
    # Copy the device's saves to the PC.
    [switch]$FromDevice,
    # The release build's saves on the PC, not the development runs'.
    [switch]$Release,
    # The PC's save folder, instead of either default.
    [string]$PcSaves
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'android-adb.ps1')

if ($ToDevice -eq $FromDevice) { throw 'Give one of -ToDevice and -FromDevice.' }
if (-not $PcSaves) {
    $PcSaves = if ($Release) { Join-Path $env:APPDATA 'KichikuouGodot\saves' } else { Join-Path $env:APPDATA 'Godot\app_userdata\Kichikuou\saves' }
}
$PcSaves = [IO.Path]::GetFullPath($PcSaves)
$remote = "$remoteFiles/saves"
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'

# Whether a file name is one of the saves' (the folder's legacy\ and anything else stay).
function Test-SaveName([string]$Name) {
    return $Name -like '*.ksav' -or $Name -eq 'msgskip.txt'
}

function Get-PcSaveFiles {
    if (-not (Test-Path -LiteralPath $PcSaves -PathType Container)) { return @() }
    return @(Get-ChildItem -LiteralPath $PcSaves -File | Where-Object { Test-SaveName $_.Name })
}

$script:adb = Get-Adb
Assert-Device
$remoteSaves = @(Get-RemoteFiles $remote | Where-Object { Test-SaveName $_ })
$pcSaveFiles = Get-PcSaveFiles

if ($ToDevice) {
    if ($pcSaveFiles.Count -eq 0) { throw "No saves to copy in $PcSaves." }
    if ($remoteSaves.Count -eq 0) {
        # An earlier build kept them in the app's internal folder (the debug build lets run-as read it); the game
        # moves them out at its next start, unless the copy is there first.
        $internal = & $adb shell "run-as $package ls files/saves 2>/dev/null"
        if ($LASTEXITCODE -eq 0 -and ($internal | Where-Object { Test-SaveName $_.Trim() })) {
            throw 'The device has saves in the app''s internal folder still: start the game on it once (it moves them out), then copy.'
        }
    }
    Write-Host "Stopping $package on the device ..."
    Invoke-Adb @('shell', 'am', 'force-stop', $package)
    if ($remoteSaves.Count -gt 0) {
        $target = "$remoteFiles/save-backups/$stamp"
        Write-Host "Moving the device's $($remoteSaves.Count) save file(s) into $target ..."
        $quoted = ($remoteSaves | ForEach-Object { "'$remote/$_'" }) -join ' '
        Invoke-Adb @('shell', "mkdir -p '$target' && mv $quoted '$target/'")
    }
    Write-Host "Copying $($pcSaveFiles.Count) save file(s) from $PcSaves to $remote ..."
    Invoke-Adb @('shell', 'mkdir', '-p', $remote)
    Invoke-Adb (@('push') + @($pcSaveFiles | ForEach-Object { $_.FullName }) + @("$remote/"))
    Grant-AppAccess $remoteFiles
}
else {
    if ($remoteSaves.Count -eq 0) { throw "No saves to copy on the device ($remote)." }
    # Pulled into a folder of their own first, so a broken transfer leaves the PC's saves as they were.
    $incoming = Join-Path ([IO.Path]::GetTempPath()) "kichikuou-saves-$stamp"
    New-Item -ItemType Directory -Force $incoming | Out-Null
    Write-Host "Copying $($remoteSaves.Count) save file(s) from $remote ..."
    foreach ($name in $remoteSaves) { Invoke-Adb @('pull', "$remote/$name", (Join-Path $incoming $name)) }
    if ($pcSaveFiles.Count -gt 0) {
        $target = Join-Path (Split-Path $PcSaves) "save-backups\$stamp"
        New-Item -ItemType Directory -Force $target | Out-Null
        Write-Host "Moving the PC's $($pcSaveFiles.Count) save file(s) into $target ..."
        foreach ($file in $pcSaveFiles) { Move-Item -LiteralPath $file.FullName -Destination (Join-Path $target $file.Name) }
    }
    New-Item -ItemType Directory -Force $PcSaves | Out-Null
    Write-Host "Moving them into $PcSaves ..."
    foreach ($file in Get-ChildItem -LiteralPath $incoming -File) {
        Move-Item -LiteralPath $file.FullName -Destination (Join-Path $PcSaves $file.Name)
    }
    Remove-Item -LiteralPath $incoming
}
Write-Host 'Done.'
