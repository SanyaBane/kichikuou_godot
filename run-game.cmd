@echo off
rem Builds the game and starts it in a window (tools\run-game.ps1); double-click it, or pass the game's switches.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\run-game.ps1" %*
if errorlevel 1 pause
