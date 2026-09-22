# QuickStart.ps1 - Minimal one-liner message dialog.
# Run: pwsh -File QuickStart.ps1   OR   powershell.exe -File QuickStart.ps1

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

Show-FluenceMessage -Message 'Hello from PowerShell!' -Icon Success
