# Message.ps1 - YesNo confirmation dialog that branches on the returned button name.
# Run: pwsh -File Message.ps1   OR   powershell.exe -File Message.ps1

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

[string]$answer = Show-FluenceMessage -Message 'Proceed with setup?' -Icon Question -Buttons YesNo

if ($answer -eq 'Yes')
{
    Write-Output 'Proceeding'
}
else
{
    Write-Output 'Cancelled'
}
