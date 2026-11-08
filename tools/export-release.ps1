<#
.SYNOPSIS
Exports the Windows release build (the "Windows Desktop" preset of godot/export_presets.cfg) into a folder.

.DESCRIPTION
Runs Godot's export (KICHIKUOU_GODOT_EXE: its environment variable, else its line in local.env) headless with
--export-release, which publishes the C# assemblies next to the exe (data_Kichikuou_windows_x86_64), then
copies the licenses beside it: LICENSE and the bundled fonts' OFL texts in "licenses". The export templates
4.7.2 mono must be installed (Editor -> Manage Export Templates).

The folder must lie outside the repository: Kichikuou.Engine.LocalPaths looks for local.env up from the exe,
and an exe inside the repository would read the developer's extract and game folder. The exported build keeps
its data in a user folder of its own (%APPDATA%\KichikuouGodot, project.godot's ".template" overrides), apart from
the development runs' app_userdata\Kichikuou, so it starts as a new player's would.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\export-release.ps1 -Out D:\Builds\Kichikuou
#>
param(
    # The folder the build goes into (created when missing).
    [Parameter(Mandatory = $true)]
    [string]$Out
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

$exe = Get-LocalPath 'KICHIKUOU_GODOT_EXE'
if (-not $exe -or -not (Test-Path $exe)) {
    throw "Godot not found: set KICHIKUOU_GODOT_EXE in local.env (see local.env.example)."
}

$Out = [IO.Path]::GetFullPath($Out)
if (($Out + '\').StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw "The build must go outside the repository ($root): inside it, the game would read local.env."
}
New-Item -ItemType Directory -Force $Out | Out-Null

$target = Join-Path $Out 'Kichikuou.exe'
Write-Host "Exporting the release build to $target ..."
# The export's dotnet publish would leave servers behind — MSBuild's nodes and Roslyn's VBCSCompiler — that
# inherit the console exe's output and hold it open, so the export never seems to end.
$env:MSBUILDDISABLENODEREUSE = '1'
$env:UseSharedCompilation = 'false'
Start-AdbServer     # nor adb's server, which Godot's Android exporter would start (android-adb.ps1)
& $exe --headless --path (Join-Path $root 'godot') --export-release 'Windows Desktop' $target
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $target)) {
    throw "The export failed (exit code $LASTEXITCODE)."
}

$licenses = Join-Path $Out 'licenses'
New-Item -ItemType Directory -Force $licenses | Out-Null
Copy-Item (Join-Path $root 'LICENSE') $Out
Copy-Item (Join-Path $root 'godot\fonts\*.txt') $licenses
Write-Host "Done: $target"
