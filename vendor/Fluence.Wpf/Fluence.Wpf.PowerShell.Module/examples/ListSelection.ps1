# ListSelection.ps1 - Pick one item from a list, then pick several with -MultiSelect.
# Run: pwsh -File ListSelection.ps1   OR   powershell.exe -File ListSelection.ps1

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

$region = Show-FluenceListSelection -Title 'Region' -Message 'Choose the deployment region' -Items 'Europe', 'Americas', 'Asia Pacific' -DefaultValue 'Europe'
if ($null -eq $region)
{
    Write-Output 'No region chosen.'
    return
}
Write-Output "Region: $region"

$features = Show-FluenceListSelection -Title 'Features' -Message 'Select the features to install' -Items 'Core', 'Documentation', 'Samples', 'Developer tools' -MultiSelect -DefaultValue 'Core'
if ($null -eq $features)
{
    Write-Output 'No features chosen.'
    return
}
Write-Output "Features ($($features.Count)): $($features -join ', ')"
