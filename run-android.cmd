@echo off
rem Exports the Android test build, installs it on the connected device and starts it (tools\export-android.ps1
rem -Install -Launch; the game folder is copied too when the device has none); double-click it, or pass more switches.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\export-android.ps1" -Install -Launch %*
if errorlevel 1 pause
