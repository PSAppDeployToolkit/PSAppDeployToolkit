# Launch Scripts for Testing

## Description

These are helper scripts designed for use in development and testing of your deployment scripts. Note that they are not intended for production deployment, where it is recommended to call Deploy-Application.exe directly.

The _psexec versions will attempt to run PsExec64.exe from the same location to launch the toolkit in Local System context (just as your deployment system most likely would). This is an essential part of testing your deployment scripts, since some applications can exhibit unusual behaviour when run in this manner.

## Scripts

- install_interactive.cmd
- install_interactive_psexec.cmd
- install_silent.cmd
- install_silent_psexec.cmd
- repair_interactive.cmd
- repair_interactive_psexec.cmd
- repair_silent.cmd
- repair_silent_psexec.cmd
- uninstall_interactive.cmd
- uninstall_interactive_psexec.cmd
- uninstall_silent.cmd
- uninstall_silent_psexec.cmd

## Notes

- Why batch files when this is a PowerShell based toolkit?! Because they are the best tool for the job here - you can run them with a simple double-click, or right-click and get the option to run them as admin.
- Unlike Deploy-Application.exe, PsExec is not configured to request admin rights, so the following lines will relaunch the script with admin rights:

```bat
net session >nul 2>&1
if ERRORLEVEL 1 powershell.exe -NoProfile -Command Start-Process -FilePath '%~0' -Verb RunAs & exit
```
