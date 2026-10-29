<#
.SYNOPSIS
Copies the screen gallery's current pictures as WebP, with its page, into a new folder for a website.

.DESCRIPTION
Reads the screen gallery (tools/ui/gallery.ps1, docs/ui/gallery.md, §10) and never writes it. Every picture its
index.html shows, <pass>\current\<id>.png, becomes <pass>\<id>.webp (lossy at -Quality, 85 unless given, or lossless,
whichever is smaller) in a new folder named by the time, <Out>\yyyy-MM-dd_HH-mm-ss, beside the gallery's page written
again for it: the pictures in WebP,
loaded as they scroll into view, nothing of the former pictures (previous is not copied; the changed and new badges and
the passes' "old" marks are left out). The gallery is -From, else KICHIKUOU_UI_GALLERY_DIR (its environment variable,
else its line in local.env), else screen-gallery at the repository's root, as gallery.ps1 finds it; -Out is
screen-gallery-deploy at the repository's root, which git ignores (the pictures hold the player's CGs); one inside the
repository must be ignored by git. The pictures are encoded by tools/ui/gallery_deploy.py on the Python of
KICHIKUOU_PYTHON (Pillow with WebP).

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\ui\gallery-deploy.ps1
#>
param(
    # The gallery's folder; default KICHIKUOU_UI_GALLERY_DIR, else screen-gallery at the repository's root.
    [string]$From = '',
    # The folder the timed copies go into; default screen-gallery-deploy at the repository's root.
    [string]$Out = '',
    # WebP's quality, 1-100.
    [ValidateRange(1, 100)][int]$Quality = 85
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

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

function Get-FullPath([string]$Path) {
    if (-not [System.IO.Path]::IsPathRooted($Path)) { $Path = Join-Path (Get-Location) $Path }
    return [System.IO.Path]::GetFullPath($Path)
}

if (-not $From) { $From = Get-LocalPath 'KICHIKUOU_UI_GALLERY_DIR' }
if (-not $From) { $From = Join-Path $root 'screen-gallery' }
$From = Get-FullPath $From
if (-not (Test-Path (Join-Path $From 'index.html'))) {
    throw "No screen gallery in ${From}: tools\ui\gallery.ps1 dumps it."
}

if (-not $Out) { $Out = Join-Path $root 'screen-gallery-deploy' }
$Out = Get-FullPath $Out
if ($Out.StartsWith($root + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    & git -C $root check-ignore -q (Join-Path $Out 'index.html')
    if ($LASTEXITCODE -ne 0) {
        throw "The folder $Out lies in the repository and git does not ignore it: the pictures hold the player's CGs."
    }
}
$target = Join-Path $Out (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss')
if (Test-Path $target) { throw "$target exists already." }

$python = Get-LocalPath 'KICHIKUOU_PYTHON'
if (-not $python) { $python = 'python' }
& $python -I (Join-Path $PSScriptRoot 'gallery_deploy.py') $From $target --quality $Quality
if ($LASTEXITCODE -ne 0) { throw "The copy failed (exit code $LASTEXITCODE; Pillow with WebP in $python?)." }
