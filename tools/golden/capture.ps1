<#
.SYNOPSIS
Starts the golden-test harness (patched xsystem35, branch golden-harness) on Kichikuou Rance.

.DESCRIPTION
Every run of a configured root (tools/golden/classes.txt) in this session is written as one
.golden file: battles to OutDir, the other groups to subfolders named after them (boot, week,
command, ...). See docs/golden-tests.md. Captures contain game data: keep them outside the
repository.

The folders default to the local paths: each key's environment variable, else its line in local.env
at the repository root (local.env.example lists the keys).

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\golden\capture.ps1
#>
param(
    # Default: KICHIKUOU_GAME_DIR.
    [string]$GameDir = '',
    # Default: KICHIKUOU_GOLDEN_DIR, else golden in KICHIKUOU_EXTRACT_DIR.
    [string]$OutDir = '',
    # Default: xsys35dc.hed in KICHIKUOU_SCENARIO_DIR, else in scenario in KICHIKUOU_EXTRACT_DIR.
    [string]$Hed = '',
    # Default: out\debug\src\xsystem35.exe in KICHIKUOU_XSYSTEM35_DIR (the golden-harness checkout).
    [string]$Exe = '',
    # Default: ucrt64\bin in KICHIKUOU_MSYS2_DIR.
    [string]$MsysBin = '',
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

# The local paths, as Kichikuou.Core.LocalPaths reads them: the environment, else local.env.
$localPaths = @{}
$localFile = Join-Path $PSScriptRoot '..\..\local.env'
if (Test-Path $localFile) {
    foreach ($line in Get-Content -Encoding UTF8 $localFile) {
        $line = $line.Trim()
        if ($line -match '^([^#=][^=]*)=(.*)$') {
            $value = $Matches[2].Trim()
            if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
                $value = $value.Substring(1, $value.Length - 2)
            }
            $localPaths[$Matches[1].Trim()] = $value
        }
    }
}
function Get-LocalPath([string]$Key, [string]$Below = '') {
    $value = [Environment]::GetEnvironmentVariable($Key)
    if (-not $value) { $value = $localPaths[$Key] }
    if (-not $value) { return '' }
    if ($Below) { return [System.IO.Path]::Combine($value, $Below) }
    return $value
}

if (-not $GameDir) { $GameDir = Get-LocalPath 'KICHIKUOU_GAME_DIR' }
if (-not $OutDir) { $OutDir = Get-LocalPath 'KICHIKUOU_GOLDEN_DIR' }
if (-not $OutDir) { $OutDir = Get-LocalPath 'KICHIKUOU_EXTRACT_DIR' 'golden' }
if (-not $Hed) { $Hed = Get-LocalPath 'KICHIKUOU_SCENARIO_DIR' 'xsys35dc.hed' }
if (-not $Hed) { $Hed = Get-LocalPath 'KICHIKUOU_EXTRACT_DIR' 'scenario\xsys35dc.hed' }
if (-not $Exe) { $Exe = Get-LocalPath 'KICHIKUOU_XSYSTEM35_DIR' 'out\debug\src\xsystem35.exe' }
if (-not $MsysBin) { $MsysBin = Get-LocalPath 'KICHIKUOU_MSYS2_DIR' 'ucrt64\bin' }
foreach ($name in @('GameDir', 'OutDir', 'Hed', 'Exe', 'MsysBin')) {
    if (-not (Get-Variable $name -ValueOnly)) { throw "No -$name and no local path for it (see local.env.example)" }
}
foreach ($path in @($GameDir, $Hed, $Exe, $MsysBin)) {
    if (-not (Test-Path $path)) { throw "Not found: $path" }
}
New-Item -ItemType Directory -Force $OutDir | Out-Null
# The harness cannot create a group's folder itself on a drive path (xsystem35's mkdir_p gives up
# at "E:", whose mkdir fails with EACCES), so the folder of every group in classes.txt is made here.
foreach ($line in Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'classes.txt')) {
    if ($line -match '^group\s+(\S+)(?:\s+(\S+))?') {
        $folder = if ($Matches[2]) { $Matches[2] } else { $Matches[1] }
        New-Item -ItemType Directory -Force (Join-Path $OutDir $folder) | Out-Null
    }
}

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
