<#
.SYNOPSIS
Dumps the screen gallery (our screens from the kept saves) and writes an index marking what changed.

.DESCRIPTION
Builds godot/Kichikuou.csproj, then runs the game's screen gallery (docs/ui/gallery.md) in a window for each pass
asked for: Lacquer always, -Steel and -Touch add theirs, -PhoneLand the phone held landscape (--phone landscape: the
whole window dumped at two thirds of the reference phone's 2712×1220, 1808×813, its black sides with it;
docs/ui/phone-build.md). A pass's former pictures move from <pass>\current to <pass>\previous first, and the gallery
compares every new picture with them (manifest.json says new, changed or same).
With -Only the pass stays whole: only the chosen entries' former pictures are copied to previous, their new ones replace
them in current, and the manifest keeps the other entries as they were; the changes of several partial runs stay
marked until a full run (each compared with its picture before the first of them).
Last it writes index.html in the gallery's folder: a tab for each pass dumped so far (one shown at a time; one not dumped
this time marked old), its sections, each entry's picture in its own place at full size, loaded as it scrolls into view;
a changed one carries a small badge, a new one a badge alone. A bar finds entries by id, title and note, keeps the
changed and new ones alone, jumps to a section and sizes the pictures; a picture's click opens the viewer (←/→ the
entries, ↑/↓ the same entry in the other passes, Space the former picture with the box round what changed; a badge's
click opens it on the former picture). Its style and script are tools/ui/gallery-index.css and gallery-index.js,
inlined. -IndexOnly writes the index alone, the passes' "old" marks as the last run left them.
The folder is -Out, else KICHIKUOU_UI_GALLERY_DIR (its environment variable, else its line in local.env), else
screen-gallery at the repository's root, which git ignores; one inside the repository must be ignored by git (the
pictures hold the player's CGs). Nothing moves the system pointer; a window opens for each pass.
-Phone adds two tabs of mock-ups, not dumps: the phone held upright and held landscape as docs/ui/phone.md's chosen
base versions, drawn by tools/ui/mock_phone.py from the touch and lacquer passes' pictures, compared with their former
drawings the same way; -PhoneOnly draws those tabs and the index alone, with no build and no game pass.
-Vfx adds the Combat VFX tab's pass (--combat-vfx: each exchange effect as a grid of its frames, docs/ui/gallery.md, §11),
-VfxOnly dumps it alone. Only when asked for (owner, 2026-10-29: the effects hardly ever change): a run without it leaves
the tab as it was, unmarked.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\ui\gallery.ps1

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\ui\gallery.ps1 -Steel -Only treasure,treasure-replace

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\ui\gallery.ps1 -PhoneOnly

.EXAMPLE
powershell -ExecutionPolicy Bypass -File tools\ui\gallery.ps1 -IndexOnly
#>
param(
    # The gallery's folder; default KICHIKUOU_UI_GALLERY_DIR, else screen-gallery at the repository's root.
    [string]$Out = '',
    # Also dump every entry in the Steel palette.
    [switch]$Steel,
    # Also dump every entry in the touch mode (--touch).
    [switch]$Touch,
    # Also dump every entry on the phone held landscape (--phone landscape, the window 1808×813).
    [switch]$PhoneLand,
    # Also draw the phone's mock-ups, not built yet (tools/ui/mock_phone.py --gallery: the owner's chosen base versions
    # of docs/ui/phone.md, from this gallery's touch and lacquer dumps; a Python with Pillow and numpy, KICHIKUOU_PYTHON).
    [switch]$Phone,
    # Only the phone's mock-ups and the index: no build, no game pass.
    [switch]$PhoneOnly,
    # Also dump the Combat VFX tab (--combat-vfx), only when asked for.
    [switch]$Vfx,
    # Only the Combat VFX tab and the index.
    [switch]$VfxOnly,
    # Only the index, from the pictures and manifests as they are: no build, no pass.
    [switch]$IndexOnly,
    # Only these entries (their ids, comma-separated).
    [string[]]$Only = @(),
    # Skip the build.
    [switch]$NoBuild
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

if ($PhoneOnly) { $Phone = $true; $NoBuild = $true }
if ($VfxOnly) { $Vfx = $true }
if ($IndexOnly) { $NoBuild = $true; $Phone = $false; $Vfx = $false }
$exe = Get-LocalPath 'KICHIKUOU_GODOT_EXE'
if (-not $PhoneOnly -and -not $IndexOnly -and (-not $exe -or -not (Test-Path $exe))) {
    throw "Godot not found: set KICHIKUOU_GODOT_EXE in local.env (see local.env.example)."
}
if (-not $Out) { $Out = Get-LocalPath 'KICHIKUOU_UI_GALLERY_DIR' }
if (-not $Out) { $Out = Join-Path $root 'screen-gallery' }
if (-not [System.IO.Path]::IsPathRooted($Out)) { $Out = Join-Path (Get-Location) $Out }
$Out = [System.IO.Path]::GetFullPath($Out)
if ($Out.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
    & git -C $root check-ignore -q (Join-Path $Out 'index.html')
    if ($LASTEXITCODE -ne 0) {
        throw "The gallery's folder $Out lies in the repository and git does not ignore it: its pictures hold the player's CGs."
    }
}
New-Item -ItemType Directory -Force $Out | Out-Null

if (-not $NoBuild) {
    Write-Host 'Building godot/Kichikuou.csproj ...'
    & dotnet build (Join-Path $root 'godot\Kichikuou.csproj') --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "The build failed (exit code $LASTEXITCODE)." }
}

$allPasses = @(
    @{ Name = 'lacquer'; Title = 'Lacquer'; Args = @() },
    @{ Name = 'steel'; Title = 'Steel'; Args = @('--palette', 'steel') },
    @{ Name = 'touch'; Title = 'Touch (Lacquer)'; Args = @('--touch') },
    @{ Name = 'phone-landscape'; Title = 'Smartphone, landscape'; Args = @('--phone', 'landscape', '--window', '1808x813');
       Held = 'landscape'; Switch = 'PhoneLand' },
    @{ Name = 'phone'; Title = 'Smartphone, upright (mock-ups)'; Mockup = $true; Held = 'upright'; Size = '1220&times;2712' },
    @{ Name = 'phone-land'; Title = 'Smartphone, landscape (mock-ups)'; Mockup = $true; Held = 'landscape';
       Size = '2712&times;1220' },
    @{ Name = 'combat-vfx'; Title = 'Combat VFX'; Args = @('--combat-vfx'); Switch = 'Vfx'; OnRequest = $true })
$passes = @($allPasses | Where-Object { ($_.Mockup -and $Phone) -or ($_.Name -eq 'combat-vfx' -and $Vfx) -or
    (-not $PhoneOnly -and -not $VfxOnly -and -not $IndexOnly -and
    ($_.Name -eq 'lacquer' -or ($_.Name -eq 'steel' -and $Steel) -or ($_.Name -eq 'touch' -and $Touch) -or
     ($_.Name -eq 'phone-landscape' -and $PhoneLand))) })

$failed = $false
foreach ($pass in $passes) {
    if ($pass.Mockup) { continue }
    $folder = Join-Path $Out $pass.Name
    $current = Join-Path $folder 'current'
    $previous = Join-Path $folder 'previous'
    New-Item -ItemType Directory -Force $folder | Out-Null
    if ($Only.Count -gt 0) {
        # Only some entries: the pass stays whole, the chosen entries' former pictures go to previous to be compared —
        # but not one an earlier partial run found changed or new: it is compared with the picture before that change
        # (or none), so the changes of several partial runs stay marked until a full run.
        New-Item -ItemType Directory -Force $previous | Out-Null
        $marked = @()
        $manifestPath = Join-Path $current 'manifest.json'
        if (Test-Path $manifestPath) {
            $marked = @((Get-Content -Raw -Encoding UTF8 $manifestPath | ConvertFrom-Json).entries |
                Where-Object { $_.since -eq 'changed' -or $_.since -eq 'new' } | ForEach-Object { $_.id })
        }
        foreach ($id in (($Only -join ',') -split ',')) {
            $picture = Join-Path $current "$id.png"
            if ((Test-Path $picture) -and $marked -notcontains $id) { Copy-Item -LiteralPath $picture -Destination $previous -Force }
        }
    } elseif (Test-Path (Join-Path $current 'manifest.json')) {
        if (Test-Path $previous) { Remove-Item -LiteralPath $previous -Recurse -Force }
        Move-Item -LiteralPath $current -Destination $previous
    }
    $arguments = @('--path', (Join-Path $root 'godot'), '--', '--screen-gallery')
    if ($Only.Count -gt 0) { $arguments += ($Only -join ',') }
    $arguments += @('--shots', $current, '--quit') + $pass.Args
    if (Test-Path $previous) { $arguments += @('--previous', $previous) }
    Write-Host "The $($pass.Title) pass ..."
    & $exe @arguments | Where-Object { $_ -match 'Kichikuou|ERROR' } | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "The $($pass.Title) pass ended with exit code $LASTEXITCODE (an entry not shown, or no window)."
        $failed = $true
    }
}

# The phone's mock-ups: drawn, not dumped — whole every time, compared with the former drawing as a pass is.
if ($Phone) {
    $python = Get-LocalPath 'KICHIKUOU_PYTHON'
    if (-not $python) { $python = 'python' }
    $extract = Get-LocalPath 'KICHIKUOU_EXTRACT_DIR'
    foreach ($tab in @('phone', 'phone-land')) {
        $folder = Join-Path $Out $tab
        $current = Join-Path $folder 'current'
        $previous = Join-Path $folder 'previous'
        New-Item -ItemType Directory -Force $folder | Out-Null
        if (Test-Path (Join-Path $current 'manifest.json')) {
            if (Test-Path $previous) { Remove-Item -LiteralPath $previous -Recurse -Force }
            Move-Item -LiteralPath $current -Destination $previous
        }
        New-Item -ItemType Directory -Force $current | Out-Null
    }
    Write-Host 'The phone mock-ups ...'
    & $python -I (Join-Path $root 'tools\ui\mock_phone.py') --gallery $Out $extract
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "The phone mock-ups ended with exit code $LASTEXITCODE (the touch and lacquer passes dumped? Pillow and numpy in $python?)."
        $failed = $true
    }
}

# ---------------------------------------------------------------- index.html

function Html([string]$text) { return [System.Net.WebUtility]::HtmlEncode($text) }

# A PNG's width and height from its header, so that the grid keeps each picture's place before it loads; none for a
# picture that is not there.
function Get-PngSize([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    $bytes = New-Object byte[] 24
    $stream = [System.IO.File]::OpenRead($path)
    try { [void]$stream.Read($bytes, 0, 24) } finally { $stream.Dispose() }
    $width = ([int]$bytes[16] -shl 24) -bor ([int]$bytes[17] -shl 16) -bor ([int]$bytes[18] -shl 8) -bor [int]$bytes[19]
    $height = ([int]$bytes[20] -shl 24) -bor ([int]$bytes[21] -shl 16) -bor ([int]$bytes[22] -shl 8) -bor [int]$bytes[23]
    return @($width, $height)
}

# The passes marked old by the last run that dumped, which -IndexOnly keeps.
$stalePath = Join-Path $Out 'stale-passes.txt'
$keptStale = @(if ($IndexOnly -and (Test-Path $stalePath)) { Get-Content -Encoding UTF8 $stalePath })
$staleNames = @()

# One tab a pass, one pass shown at a time; a pass not dumped this time says so on its tab and above its pictures.
$tabs = New-Object System.Text.StringBuilder
$body = New-Object System.Text.StringBuilder
foreach ($pass in $allPasses) {
    $manifestPath = Join-Path $Out "$($pass.Name)\current\manifest.json"
    if (-not (Test-Path $manifestPath)) { continue }
    $manifest = Get-Content -Raw -Encoding UTF8 $manifestPath | ConvertFrom-Json
    $entries = @($manifest.entries)
    $changed = @($entries | Where-Object { $_.since -eq 'changed' -or $_.since -eq 'new' })
    # A run of the mock-ups alone dumps no game pass, so it leaves them unmarked; nor is a pass dumped only on request.
    $isStale = if ($IndexOnly) { $keptStale -contains $pass.Name } else {
        $passes.Name -notcontains $pass.Name -and -not ($PhoneOnly -and -not $pass.Mockup) -and -not $pass.OnRequest }
    if ($isStale) { $staleNames += $pass.Name }
    $mark = if ($isStale) { ' <b class=stale>old</b>' } elseif ($changed.Count) { " <b class=changed>$($changed.Count)</b>" } else { '' }
    [void]$tabs.Append("<a class=tab href=`"#$($pass.Name)`" data-pass=`"$($pass.Name)`">$(Html $pass.Title)$mark</a>")
    $class = if ($pass.Mockup) { "pass mockup $($pass.Held)" } else { "pass $($pass.Held)" }
    [void]$body.Append("<section class=`"$class`" id=`"$($pass.Name)`" data-title=`"$(Html $pass.Title)`"><h2>$(Html $pass.Title) <small>$(Html $manifest.written) &middot; " +
        "font $(Html $manifest.font) &middot; $($entries.Count) entries, $($changed.Count) changed or new</small></h2>")
    if ($pass.Mockup) {
        [void]$body.Append("<p class=mockup><b>Not built yet.</b> Mock-ups of the phone held $($pass.Held) (Xiaomi Poco X6 Pro, " +
            "$($pass.Size)): the owner&rsquo;s chosen base versions of <code>docs/ui/phone.md</code>, drawn by " +
            '<code>tools/ui/mock_phone.py</code> over this gallery&rsquo;s dumps.</p>')
    }
    if ($pass.OnRequest) {
        [void]$body.Append("<p>Dumped only when asked for (<code>gallery.ps1 -$($pass.Switch)</code>): each exchange effect as six " +
            'of its frames over the battlefield, the armies of <code>docs/ui/gallery.md</code>, &sect;11.</p>')
    }
    if ($isStale) {
        [void]$body.Append("<p class=stale>Not dumped by the last run: these pictures are from $(Html $manifest.written) and may be " +
            "out of date (<code>gallery.ps1 -$(if ($pass.Mockup) { 'Phone' } elseif ($pass.Switch) { $pass.Switch } else { $pass.Name.Substring(0, 1).ToUpper() + $pass.Name.Substring(1) })</code> dumps them again).</p>")
    }
    # The sections in the order they first come, each entry in its own place at full size whatever changed (the owner,
    # 2026-10-26): a changed one's badge opens the former picture, a new one's is a badge alone. The figure's data are
    # the viewer's: the entry's id across the passes, since and the box round what changed (the website's copy drops them).
    $sections = New-Object System.Collections.Generic.List[string]
    foreach ($entry in $entries) { if (-not $sections.Contains($entry.section)) { $sections.Add($entry.section) } }
    foreach ($name in $sections) {
        [void]$body.Append("<h3>$(Html $name)</h3><div class=grid>")
        foreach ($entry in @($entries | Where-Object { $_.section -eq $name })) {
            $before = "$($pass.Name)/previous/$($entry.file)"
            $badge = switch ($entry.since) {
                'changed' { "<a class=changed href=`"$before`" title=`"The former picture`">changed &middot; $($entry.differing) px</a>" }
                'new' { '<b class=new>new</b>' }
                default { '' }
            }
            $data = "data-id=`"$(Html $entry.id)`""
            if ($entry.since) { $data += " data-since=`"$($entry.since)`" data-differing=`"$($entry.differing)`"" }
            if ($entry.since -eq 'changed' -and $entry.box) { $data += " data-box=`"$($entry.box -join ',')`"" }
            [void]$body.Append("<figure id=`"$($pass.Name)-$(Html $entry.id)`" $data><figcaption><span class=title>$(Html $entry.title)</span> $badge<br><code>$(Html $entry.id)</code>")
            if ($entry.note) { [void]$body.Append("<span class=note> &mdash; $(Html $entry.note)</span>") }
            if ($entry.error) { [void]$body.Append("<br><b class=error>$(Html $entry.error)</b>") }
            [void]$body.Append('</figcaption>')
            if ($entry.file) {
                $now = "$($pass.Name)/current/$($entry.file)"
                $size = Get-PngSize (Join-Path $Out "$($pass.Name)\current\$($entry.file)")
                $sizes = if ($size) { " width=$($size[0]) height=$($size[1])" } else { '' }
                [void]$body.Append("<a href=`"$now`"><img loading=lazy src=`"$now`"$sizes alt=`"$(Html $entry.id)`"></a>")
            }
            [void]$body.Append('</figure>')
        }
        [void]$body.Append('</div>')
    }
    [void]$body.Append('</section>')
}

$css = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'gallery-index.css')
$js = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'gallery-index.js')
$html = @"
<!doctype html>
<html lang=en>
<meta charset=utf-8>
<meta name=viewport content="width=device-width, initial-scale=1">
<title>Screen Gallery</title>
<style>
$css</style>
<h1>Screen gallery</h1>
<p>Our screens from the kept saves (docs/ui/gallery.md). Each in its place; a click opens it in the viewer, a changed one&rsquo;s badge on its former picture.</p>
<nav><div class=tabs>$($tabs.ToString())</div></nav>
$($body.ToString())
<script>
$js</script>
</html>
"@
Set-Content -Encoding UTF8 -Path (Join-Path $Out 'index.html') -Value $html
if (-not $IndexOnly) { [System.IO.File]::WriteAllLines($stalePath, [string[]]$staleNames) }
Write-Host "The index: $(Join-Path $Out 'index.html')"
if ($failed) { exit 1 }
