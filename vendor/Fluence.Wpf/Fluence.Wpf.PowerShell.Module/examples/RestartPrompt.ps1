# RestartPrompt.ps1 - A deployment-style restart prompt: Restart now / Restart later with a countdown on
# the Restart now button. The script only reports the outcome; it never restarts the machine.
# Run: pwsh -File RestartPrompt.ps1   OR   powershell.exe -File RestartPrompt.ps1

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

$outcome = Show-FluenceRestartPrompt -Title 'Contoso Suite' -Message 'Contoso Suite has been installed.', 'Restart your computer to complete the installation.' -Countdown 30

switch ($outcome)
{
    'Restart' { Write-Output 'The user chose to restart now (this example does not restart).' }
    'TimedOut' { Write-Output 'Nobody answered within the countdown; a deployment would restart here.' }
    default { Write-Output 'The user deferred the restart.' }
}
