@echo off
rem Collects the game's logs and hang reports from the connected Android device into a folder on the PC (tools\android-logs.ps1,
rem docs\hang-watch.md); run it while a frozen game is still open. Double-click it, or pass -Out <folder>.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\android-logs.ps1" %*
pause
