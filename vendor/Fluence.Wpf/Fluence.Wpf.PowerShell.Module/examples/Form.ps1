# Form.ps1 - Mixed form with Text, Number, Choice, Date, and Checkbox prompts.
# Run: pwsh -File Form.ps1   OR   powershell.exe -File Form.ps1

Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

[object[]]$prompts = @(
    New-FluencePrompt -Name FullName -Message 'Full name' -InputType Text -ValidateNotEmpty
    New-FluencePrompt -Name Age -Message 'Age' -InputType Number -DefaultValue 30
    New-FluencePrompt -Name Country -Message 'Country' -InputType Choice -ValidateSet 'Australia', 'Canada', 'United Kingdom', 'United States' -DefaultValue 'United States'
    New-FluencePrompt -Name StartDate -Message 'Start date' -InputType Date
    New-FluencePrompt -Name AcceptTerms -Message 'I accept the terms and conditions' -InputType Checkbox
)

$result = Show-FluenceDialog -Title 'Registration' -Prompts $prompts -Buttons OK, Cancel

if ($result.Cancelled)
{
    Write-Output 'Form cancelled.'
}
else
{
    Write-Output "Name:         $($result.FullName)"
    Write-Output "Age:          $($result.Age)"
    Write-Output "Country:      $($result.Country)"
    Write-Output "Start date:   $($result.StartDate)"
    Write-Output "Accepted:     $($result.AcceptTerms)"
}
