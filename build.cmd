@echo off
powershell.exe -ExecutionPolicy Bypass -File %~dp0build.ps1
pause
exit %errorlevel%
