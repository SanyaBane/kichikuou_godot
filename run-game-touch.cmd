@echo off
rem Builds the game and starts it in a window with the Android interface (tools\run-game.ps1 --touch: the touch mode,
rem the mouse playing a finger, no hover; docs/ui/touch.md); double-click it, or pass more of the game's switches.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\run-game.ps1" --touch %*
if errorlevel 1 pause
