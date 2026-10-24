@echo off
rem Copies the PC's saves (the development runs') to the connected Android device, replacing its saves after a backup
rem (tools\copy-saves.ps1 -ToDevice); double-click it, or pass more switches (-Release, -PcSaves DIR).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\copy-saves.ps1" -ToDevice %*
pause
