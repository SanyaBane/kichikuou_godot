@echo off
rem Copies the connected Android device's saves to the PC (the development runs'), replacing the PC's after a backup
rem (tools\copy-saves.ps1 -FromDevice); close the game on the PC first. Double-click it, or pass more switches
rem (-Release, -PcSaves DIR).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\copy-saves.ps1" -FromDevice %*
pause
