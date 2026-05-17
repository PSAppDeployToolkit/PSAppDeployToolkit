@echo off
powershell.exe -ExecutionPolicy Bypass -File "%~dp0pester.ps1"
pause
exit %errorlevel%
