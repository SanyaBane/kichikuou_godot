<#
.SYNOPSIS
The adb helpers the Android scripts share (export-android.ps1, copy-saves.ps1); dot-source it.
#>

$script:package = 'org.kichikuou.port'     # GameData.AndroidPackage, the preset's package/unique_name
# The app's own external folder: the game folder (GameData.AndroidGameFolder) and the saves (GameData.AndroidSaveFolder).
$script:remoteFiles = "/storage/emulated/0/Android/data/$script:package/files"

# adb: the SDK's platform-tools (ANDROID_HOME, else the SDK's default folder), else the PATH.
function Get-Adb {
    $sdk = $env:ANDROID_HOME
    if (-not $sdk) { $sdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk' }
    $adb = Join-Path $sdk 'platform-tools\adb.exe'
    if (Test-Path $adb) { return $adb }
    $found = Get-Command adb -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    throw "adb not found: install the Android SDK's platform-tools ($sdk) or set ANDROID_HOME."
}

function Invoke-Adb([string[]]$Arguments) {
    & $script:adb @Arguments
    if ($LASTEXITCODE -ne 0) { throw "adb $($Arguments -join ' ') failed (exit code $LASTEXITCODE)." }
}

# The one device adb can use (ANDROID_SERIAL's, when set), before any time is spent.
function Assert-Device {
    $lines = & $script:adb devices | Select-Object -Skip 1 | Where-Object { $_.Trim() }
    $ready = @($lines | Where-Object { $_ -match "`tdevice$" } | ForEach-Object { ($_ -split "`t")[0] })
    $unauthorized = @($lines | Where-Object { $_ -match "`tunauthorized$" })
    if ($env:ANDROID_SERIAL) {
        if ($ready -notcontains $env:ANDROID_SERIAL) { throw "The device $env:ANDROID_SERIAL (ANDROID_SERIAL) is not connected." }
        return
    }
    if ($ready.Count -eq 1) { return }
    if ($ready.Count -gt 1) { throw "Several devices are connected ($($ready -join ', ')): set ANDROID_SERIAL to one." }
    if ($unauthorized.Count -gt 0) { throw 'The device does not allow this computer yet: accept the USB debugging prompt on it.' }
    throw 'No device: connect it by USB with USB debugging on (Settings > Developer options).'
}

# What adb put in the app's external folder opened to the app: a folder adb creates there is adb's (the shell user's)
# with no access for others, so on Android 16 (the Poco X6 Pro, 2026-10-28) the app could not read the pushed game folder;
# an older Android let it. adb's folders and files under $Remote become everyone's; the app's own stay as they are.
function Grant-AppAccess([string]$Remote) {
    Invoke-Adb @('shell', "find '$Remote' -user shell -type d -exec chmod 777 {} + ; find '$Remote' -user shell -type f -exec chmod 666 {} +")
}

# The names of the files in a folder on the device (none when it is missing).
function Get-RemoteFiles([string]$Remote) {
    $listing = & $script:adb shell "ls -1p '$Remote' 2>/dev/null"
    if ($LASTEXITCODE -ne 0) { return @() }
    return @($listing | ForEach-Object { $_.Trim() } | Where-Object { $_ -and -not $_.EndsWith('/') })
}
