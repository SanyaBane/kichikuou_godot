<#
.SYNOPSIS
Builds the game and starts it in a window, without Rider or the Godot editor.

.DESCRIPTION
Runs `dotnet build godot/Kichikuou.csproj` (the four libraries and the Godot project), then starts Godot on
the godot folder. Godot is KICHIKUOU_GODOT_EXE (its environment variable, else its line in local.env at the
repository root); without -Console the exe next to it without "_console" in its name is used, so no
console window stays open. A failed build starts nothing. Any other arguments go to the game after "--"
(the development switches in CLAUDE.md, "Commands").

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\run-game.ps1

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\run-game.ps1 -Console --load --saves user://saves-look --slots-from user://saves-week18
#>
param(
    # Start the console exe (the log in a console next to the window) and wait for the game to end.
    [switch]$Console,
    # Skip the build: start what was built last.
    [switch]$NoBuild,
    # The game's own switches, passed after "--".
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$GameArgs = @()
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')

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

$exe = Get-LocalPath 'KICHIKUOU_GODOT_EXE'
if (-not $exe -or -not (Test-Path $exe)) {
    throw "Godot not found: set KICHIKUOU_GODOT_EXE in local.env (see local.env.example)."
}
if (-not $Console) {
    $windowed = $exe -replace '_console(\.exe)$', '$1'
    if (Test-Path $windowed) { $exe = $windowed }
}

if (-not $NoBuild) {
    Write-Host 'Building godot/Kichikuou.csproj ...'
    & dotnet build (Join-Path $root 'godot\Kichikuou.csproj') --nologo -v quiet
    if ($LASTEXITCODE -ne 0) {
        throw "The build failed (exit code $LASTEXITCODE): the game is not started."
    }
}

$arguments = @('--path', (Join-Path $root 'godot'))
if ($GameArgs.Count -gt 0) { $arguments += @('--') + $GameArgs }
Write-Host "Starting $(Split-Path $exe -Leaf) $($arguments -join ' ')"
if ($Console) {
    & $exe @arguments
} else {
    Start-Process -FilePath $exe -ArgumentList ($arguments | ForEach-Object { if ($_ -match '\s') { "`"$_`"" } else { $_ } })
}
