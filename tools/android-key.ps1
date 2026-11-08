<#
.SYNOPSIS
The Android release key's helpers the release scripts share (export-android.ps1 -Release, release.ps1); dot-source it.
#>

# The JDK: JAVA_HOME's, else Godot's own (export/android/java_sdk_path in its newest editor settings); keytool is not on
# the PATH on the owner's machine (2026-11-08). None found: $null.
function Get-JavaHome {
    if ($env:JAVA_HOME -and (Test-Path (Join-Path $env:JAVA_HOME 'bin\keytool.exe'))) { return $env:JAVA_HOME }
    $settings = Get-ChildItem (Join-Path $env:APPDATA 'Godot') -Filter 'editor_settings-*.tres' -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending
    foreach ($file in $settings) {
        $line = Select-String -Path $file.FullName -Pattern '^export/android/java_sdk_path = "(.+)"$' | Select-Object -First 1
        if ($line) {
            $jdk = $line.Matches[0].Groups[1].Value
            if (Test-Path (Join-Path $jdk 'bin\keytool.exe')) { return $jdk }
        }
    }
    return $null
}

# Sets the variables Godot's Android export reads the release key from (GODOT_ANDROID_KEYSTORE_RELEASE_PATH, _USER and
# _PASSWORD), for this process and those it starts; a variable already set wins. The password is asked once when it is
# not set and never written anywhere. The key is opened with keytool before any time is spent, when a JDK is found.
function Set-ReleaseKey([string]$Keystore, [string]$Alias) {
    if ($env:GODOT_ANDROID_KEYSTORE_RELEASE_PATH) { $Keystore = $env:GODOT_ANDROID_KEYSTORE_RELEASE_PATH }
    if ($env:GODOT_ANDROID_KEYSTORE_RELEASE_USER) { $Alias = $env:GODOT_ANDROID_KEYSTORE_RELEASE_USER }
    if (-not $Alias) { $Alias = 'kichikuou' }
    if (-not $Keystore) {
        throw 'No release key: set KICHIKUOU_ANDROID_KEYSTORE in local.env (see local.env.example and docs/release.md).'
    }
    if (-not (Test-Path $Keystore)) { throw "The release key is not found: $Keystore." }
    $env:GODOT_ANDROID_KEYSTORE_RELEASE_PATH = (Resolve-Path $Keystore).Path
    $env:GODOT_ANDROID_KEYSTORE_RELEASE_USER = $Alias
    if (-not $env:GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD) {
        try { $secure = Read-Host -AsSecureString "Password of the release key ($Alias)" }
        catch { throw 'The release key needs its password: run this in a console, or set GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD.' }
        $env:GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD = (New-Object PSCredential 'key', $secure).GetNetworkCredential().Password
    }

    $jdk = Get-JavaHome
    if (-not $jdk) {
        Write-Host 'No JDK found (JAVA_HOME, Godot''s settings): the release key is not tried before the export.'
        return
    }
    # keytool reads the password from the variable, never from its command line; its errors on stderr are its answer.
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $answer = & (Join-Path $jdk 'bin\keytool.exe') -list -keystore $env:GODOT_ANDROID_KEYSTORE_RELEASE_PATH -alias $Alias `
        -storepass:env GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD 2>&1 | Out-String
    $code = $LASTEXITCODE
    $ErrorActionPreference = $previous
    if ($code -ne 0) {
        Remove-Item Env:GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD     # a wrong one is asked again on the next try
        throw "The release key does not open with that password and alias '$Alias': $($answer.Trim())"
    }
    Write-Host "The release key opens: $($env:GODOT_ANDROID_KEYSTORE_RELEASE_PATH) ($Alias)."
}

# That an APK is signed with the release key Set-ReleaseKey set: the SHA-256 of its signer's certificate, as apksigner
# reports it (the SDK's newest build-tools, run on the JDK found), is the key's, as keytool reports it. Godot's own debug
# key is "CN=Godot, OU=Godot Engine", nothing a name check could tell apart. Without apksigner or a JDK, nothing is checked.
function Assert-ReleaseSigned([string]$Apk) {
    $sdk = $env:ANDROID_HOME
    if (-not $sdk) { $sdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk' }
    $signer = Get-ChildItem (Join-Path $sdk 'build-tools') -Directory -ErrorAction SilentlyContinue |
        Sort-Object { [version]($_.Name -replace '[^0-9.].*$', '') } -Descending |
        ForEach-Object { Join-Path $_.FullName 'apksigner.bat' } | Where-Object { Test-Path $_ } | Select-Object -First 1
    $jdk = Get-JavaHome
    if (-not $signer -or -not $jdk) { Write-Host 'apksigner or a JDK not found: the APK''s signer is not checked.'; return }

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $key = & (Join-Path $jdk 'bin\keytool.exe') -list -keystore $env:GODOT_ANDROID_KEYSTORE_RELEASE_PATH `
        -alias $env:GODOT_ANDROID_KEYSTORE_RELEASE_USER -storepass:env GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD 2>&1 | Out-String
    $keyCode = $LASTEXITCODE
    $javaHome = $env:JAVA_HOME
    $env:JAVA_HOME = $jdk
    $answer = & $signer verify --print-certs $Apk 2>&1 | Out-String
    $code = $LASTEXITCODE
    $env:JAVA_HOME = $javaHome
    $ErrorActionPreference = $previous

    if ($keyCode -ne 0 -or $key -notmatch 'SHA-256\):\s*([0-9A-Fa-f:]+)') { throw "keytool does not list the release key: $($key.Trim())" }
    $expected = ($Matches[1] -replace ':', '').ToLowerInvariant()
    if ($code -ne 0) { throw "apksigner does not verify $Apk`: $($answer.Trim())" }
    $dn = ($answer -split "`r?`n" | Where-Object { $_ -match 'certificate DN:' } | Select-Object -First 1)
    if ($answer -notmatch 'certificate SHA-256 digest:\s*([0-9a-f]+)') { throw "apksigner names no signer of $Apk`: $($answer.Trim())" }
    if ($Matches[1] -ne $expected) { throw "$Apk is not signed with the release key: $($dn.Trim())" }
    Write-Host "Signed with the release key: $($dn.Trim())"
}
