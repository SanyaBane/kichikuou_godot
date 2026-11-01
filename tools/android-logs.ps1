<#
.SYNOPSIS
Collects what the Android device knows about the game into a folder on the PC, for a frozen game above all.

.DESCRIPTION
docs/hang-watch.md. Run it while the frozen game is still open: with the game's process alive it first saves what its
threads are doing (top, and each thread's wait and call through run-as, which the debug build allows) and asks the
runtime for their stacks. Then, alive or not: the frame watch's reports, Godot's log files of the last launches, the whole
logcat (it rolls over within minutes of ordinary use), how the game's last processes ended and the audio players' events
(the sign of a frozen main thread: no "paused" when the game went to the background). Last, the game having been open, the
stacks, which only a bug report gives without root: ~3 minutes; only their file stays, a bug report holds all the phone's
logs. Needs USB debugging on.

pull-android-logs.cmd at the root runs it for a double-click.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\android-logs.ps1 -Out C:\temp\poco
#>
param(
    # The folder to write into, by default %LOCALAPPDATA%\KichikuouGodot\android\logs\<time>.
    [string]$Out,
    # Without the stacks and their bug report.
    [switch]$NoStack
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'android-adb.ps1')

$script:adb = Get-Adb
Assert-Device
if (-not $Out) { $Out = Join-Path $env:LOCALAPPDATA "KichikuouGodot\android\logs\$(Get-Date -Format 'yyyyMMdd-HHmmss')" }
New-Item -ItemType Directory -Force $Out | Out-Null

function Save-Output([string]$Name, [string[]]$Arguments) {
    & $script:adb @Arguments | Out-File -Encoding utf8 (Join-Path $Out $Name)
}

$gamePid = & $script:adb shell "pidof $package"
if ($gamePid) {
    $gamePid = $gamePid.Trim()
    Write-Host "The game is running (process $gamePid): its threads first, 3 s ..."
    Save-Output 'threads-top.txt' @('shell', "top -H -b -n 2 -d 3 -p $gamePid")
    $each = 'run-as {0} sh -c ''for t in /proc/{1}/task/*; do echo $(cut -c1-60 $t/stat) / $(cat $t/wchan) / $(cat $t/syscall); done''' -f $package, $gamePid
    Save-Output 'threads-wait.txt' @('shell', $each)
    if (-not $NoStack) { Invoke-Adb @('shell', "run-as $package kill -3 $gamePid") }
}
else {
    Write-Host 'The game is not running.'
}

$reports = & $script:adb shell "ls '$remoteFiles/hang-reports' 2>/dev/null"
if ($reports) {
    Write-Host "The frame watch's reports: $(@($reports).Count) ..."
    Invoke-Adb @('pull', "$remoteFiles/hang-reports", $Out)
}
else {
    Write-Host 'The frame watch has no reports.'
}

$logs = & $script:adb shell "run-as $package ls files/logs 2>/dev/null"
foreach ($name in @($logs | ForEach-Object { $_.Trim() } | Where-Object { $_ })) {
    Save-Output "godot-$name" @('shell', "run-as $package cat files/logs/$name")
}

Write-Host 'The logcat and the system''s records ...'
Save-Output 'logcat.txt' @('logcat', '-d', '-b', 'all', '-v', 'threadtime')
Save-Output 'exit-info.txt' @('shell', "dumpsys activity exit-info $package")
Save-Output 'audio.txt' @('shell', 'dumpsys audio')

if ($gamePid -and -not $NoStack) {
    Write-Host 'The stacks, from a bug report: ~3 minutes ...'
    & $script:adb bugreport $Out | Out-Null
    $bugreport = Get-ChildItem -LiteralPath $Out -Filter 'bugreport-*.zip' | Select-Object -First 1
    if ($bugreport) {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [IO.Compression.ZipFile]::OpenRead($bugreport.FullName)
        foreach ($entry in @($archive.Entries | Where-Object { $_.FullName -like 'FS/data/anr/trace_*' })) {
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $Out "stacks-$($entry.Name).txt"), $true)
        }
        $archive.Dispose()
        Remove-Item -LiteralPath $bugreport.FullName
    }
}
Write-Host "Done: $Out"
