@echo off
rem Builds a release (tools\release.ps1): the Windows zip and the Android release APK of the version in
rem godot\project.godot with their SHA-256, in %LOCALAPPDATA%\KichikuouGodot\release\<version>. It asks for the
rem release key's password first. Double-click it, or pass more switches (docs\release.md).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\release.ps1" %*
pause
