<#
.SYNOPSIS
Starts the golden-test harness (patched xsystem35, branch golden-harness) on Kichikuou Rance.

.DESCRIPTION
Every run of a configured root (tools/golden/classes.txt) in this session is written as one
.golden file: battles to OutDir, the other groups to subfolders named after them (boot, week,
command, ...). See docs/golden-tests.md. Captures contain game data: keep them outside the
repository.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\golden\capture.ps1
#>
param(
    [string]$GameDir = 'E:\Modding games\Modding Rance\Kichikou\Kichikuou Rance',
    [string]$OutDir = 'E:\Modding games\Modding Rance\Kichikou\_extract\golden',
    [string]$Hed = 'E:\Modding games\Modding Rance\Kichikou\_extract\scenario\xsys35dc.hed',
    [string]$Exe = 'F:\sources\Rance\xsystem35-sdl2\out\debug\src\xsystem35.exe',
    [string]$MsysBin = 'C:\msys64\ucrt64\bin',
    # sgenrand() seed; default: a new one per session (printed, and stored in every capture).
    [long]$Seed = -1,
    # Also log every page call of the session to OutDir\trace.log.
    [switch]$Trace,
    # Print every random draw to this console ("rnd k: Y 4, n = result page@addr") and write it to
    # RngLogFile, to compare with the port's --rnglog (tools/golden/rngwatch.py watches both files).
    [switch]$RngLog,
    [string]$RngLogFile = (Join-Path $env:APPDATA 'Godot\app_userdata\Kichikuou\rnglog-original.txt'),
    # Extra arguments for xsystem35 (e.g. '-savedir', 'D:\saves').
    [string[]]$ExtraArgs = @()
)

$ErrorActionPreference = 'Stop'
foreach ($path in @($GameDir, $Hed, $Exe, $MsysBin)) {
    if (-not (Test-Path $path)) { throw "Not found: $path" }
}
New-Item -ItemType Directory -Force $OutDir | Out-Null

if ($Seed -lt 0) { $Seed = Get-Random -Minimum 1 -Maximum 2147483647 }
$env:XSYS35_SEED = "$Seed"
$env:XSYS35_GOLDEN_DIR = $OutDir
$env:XSYS35_GOLDEN_HED = $Hed
$env:XSYS35_GOLDEN_CLASSES = Join-Path $PSScriptRoot 'classes.txt'
$env:XSYS35_GOLDEN_TRACE = if ($Trace) { '1' } else { '' }
$env:XSYS35_RNGLOG = if ($RngLog) { '1' } else { '' }
$env:XSYS35_RNGLOG_FILE = if ($RngLog) { $RngLogFile } else { '' }
if ($RngLog) {
    New-Item -ItemType Directory -Force (Split-Path $RngLogFile) | Out-Null
    Write-Host "Random draws are logged to $RngLogFile"
}
$env:PATH = "$MsysBin;$env:PATH"
# The page names in the draw lines are UTF-8.
if ($RngLog) { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 }

Write-Host "Seed $Seed; captures go to $OutDir"
# xsystem35 finds the game in the working directory (-gamedir is Android-only).
Push-Location $GameDir
try {
    if ($RngLog) {
        # A Windows GUI program: its stdout reaches this console only through a pipe (which also waits for it).
        & $Exe @ExtraArgs | ForEach-Object { $_ }
    }
    else {
        & $Exe @ExtraArgs
    }
}
finally {
    Pop-Location
}
