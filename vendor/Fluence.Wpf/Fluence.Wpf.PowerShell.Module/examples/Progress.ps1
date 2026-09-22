# Progress.ps1 - A five-step deployment loop behind a non-modal progress window: indeterminate while
# preparing, then determinate with a message and detail line per step, then closed.
# Run: pwsh -File Progress.ps1   OR   powershell.exe -File Progress.ps1

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

$steps = @(
    'Checking prerequisites',
    'Downloading package',
    'Installing components',
    'Configuring settings',
    'Cleaning up'
)

$progress = Show-FluenceProgress -Title 'Contoso Suite' -Message 'Preparing installation...' -Detail 'This takes a moment.' -Position BottomRight
try
{
    Start-Sleep -Seconds 2

    for ($i = 0; $i -lt $steps.Count; $i++)
    {
        $percent = [int](($i / $steps.Count) * 100)
        Update-FluenceProgress -Handle $progress -Message $steps[$i] -Detail "Step $($i + 1) of $($steps.Count)" -PercentComplete $percent
        Start-Sleep -Seconds 1
    }

    Update-FluenceProgress -Handle $progress -Message 'Installation complete' -Detail '' -PercentComplete 100
    Start-Sleep -Seconds 1
}
finally
{
    Close-FluenceProgress -Handle $progress
}

Write-Output 'Done.'
