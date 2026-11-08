<#
.SYNOPSIS
Builds a release: the Windows build zipped and the Android release APK of the version in godot/project.godot, with
their SHA-256.

.DESCRIPTION
docs/release.md, "Building a release". Into -Out (by default %LOCALAPPDATA%\KichikuouGodot\release\<version>), a
folder outside the repository that must not hold this version's files yet:

  Kichikuou-<version>-windows-x64\      the Windows build (export-release.ps1)
  Kichikuou-<version>-windows-x64.zip   that folder zipped: the download
  Kichikuou-<version>.apk               the Android release APK (export-android.ps1 -Release)
  SHA256SUMS.txt                        the downloads' SHA-256

The release key's password is asked first, before the exports (unless GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD is set),
and tried; the key is KICHIKUOU_ANDROID_KEYSTORE in local.env. A work tree with changes not committed is refused (a
release is a commit's) unless -AllowDirty. The tag is not made here: it comes when the files are published.

release.cmd at the root runs it for a double-click.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\release.ps1
.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\release.ps1 -NoAndroid -AllowDirty -Out D:\Builds\try
#>
param(
    # The folder the release goes into (created when missing).
    [string]$Out = '',
    # Build from a work tree with changes not committed (a trial, not a release).
    [switch]$AllowDirty,
    # Leave the Windows build out (its files from an earlier run, if any, are still hashed).
    [switch]$NoWindows,
    # Leave the Android APK out (likewise).
    [switch]$NoAndroid
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
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

# A setting's value in one of the Godot project's files ("key=value" or key="value").
function Get-GodotSetting([string]$File, [string]$Key) {
    $line = Select-String -Path (Join-Path $root "godot\$File") -Pattern ('^' + [regex]::Escape($Key) + '="?([^"]*)"?$') |
        Select-Object -First 1
    if (-not $line) { throw "No $Key in godot/$File." }
    return $line.Matches[0].Groups[1].Value
}

$version = Get-GodotSetting 'project.godot' 'config/version'
$versionCode = Get-GodotSetting 'export_presets.cfg' 'version/code'
if (-not $Out) { $Out = Join-Path $env:LOCALAPPDATA "KichikuouGodot\release\$version" }
$Out = [IO.Path]::GetFullPath($Out)
if (($Out + '\').StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw "The release must go outside the repository ($root)."
}
$windowsDir = Join-Path $Out "Kichikuou-$version-windows-x64"
$zip = "$windowsDir.zip"
$apk = Join-Path $Out "Kichikuou-$version.apk"
$building = @()
if (-not $NoWindows) { $building += $windowsDir, $zip }
if (-not $NoAndroid) { $building += $apk }
foreach ($path in $building) {
    if (Test-Path $path) { throw "$path is there already: move it away, or choose another -Out." }
}

$changes = git -C $root status --porcelain
if ($LASTEXITCODE -ne 0) { throw 'git status failed.' }
if ($changes -and -not $AllowDirty) {
    throw 'The work tree has changes not committed: a release is built from a commit (or pass -AllowDirty for a trial).'
}
$commit = git -C $root rev-parse --short HEAD
$note = if ($changes) { ', with changes not committed' } else { '' }
Write-Host "Kichikuou $version (Android version code $versionCode) from commit $commit$note, into $Out"

New-Item -ItemType Directory -Force $Out | Out-Null
if (-not $NoAndroid) {
    # Asked and tried now, not after the Windows export's minutes.
    Set-ReleaseKey -Keystore (Get-LocalPath 'KICHIKUOU_ANDROID_KEYSTORE') -Alias (Get-LocalPath 'KICHIKUOU_ANDROID_KEY_ALIAS')
}
if (-not $NoWindows) {
    & (Join-Path $PSScriptRoot 'export-release.ps1') -Out $windowsDir
    Write-Host "Zipping $zip ..."
    # .NET's own zip, the folder itself inside, the entries' paths with slashes: Windows PowerShell's Compress-Archive,
    # and ZipFile without the switch on its old .NET Framework, write backslashes, which other systems' unzip tools do
    # not take as folders. The switch is read at ZipFile's first use in the process.
    [AppContext]::SetSwitch('Switch.System.IO.Compression.ZipFile.UseBackslash', $false)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($windowsDir, $zip, [IO.Compression.CompressionLevel]::Optimal, $true)
}
if (-not $NoAndroid) {
    & (Join-Path $PSScriptRoot 'export-android.ps1') -Release -Out $apk
}

$sums = foreach ($file in @($zip, $apk)) {
    if (Test-Path $file) { '{0}  {1}' -f (Get-FileHash -Algorithm SHA256 $file).Hash.ToLowerInvariant(), (Split-Path -Leaf $file) }
}
Set-Content -Encoding ASCII (Join-Path $Out 'SHA256SUMS.txt') $sums
$sums | ForEach-Object { Write-Host $_ }
Write-Host "Done: $Out"
