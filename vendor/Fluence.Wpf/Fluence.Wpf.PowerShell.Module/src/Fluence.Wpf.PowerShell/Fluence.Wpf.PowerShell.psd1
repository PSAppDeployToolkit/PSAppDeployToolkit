@{
    RootModule           = 'Fluence.Wpf.PowerShell.psm1'
    ModuleVersion        = '0.9.0'
    GUID                 = 'ad4e53a0-2f63-4f2a-b613-0816b85d3164'
    Author               = 'Dan Cunningham'
    CompanyName          = 'Dan Cunningham'
    Copyright            = 'Copyright (c) 2026 Dan Cunningham. All rights reserved.'
    Description          = 'Declarative Fluent (Windows 11) dialogs, prompts, progress and windows for PowerShell 5.1 and 7, built on Fluence.Wpf.'
    PowerShellVersion    = '5.1'
    CompatiblePSEditions = @('Desktop', 'Core')
    FunctionsToExport    = @(
        'Show-FluenceDialog',
        'Show-FluenceWindow',
        'New-FluencePrompt',
        'New-FluenceButton',
        'Show-FluenceMessage',
        'Get-FluenceInput',
        'Set-FluenceTheme',
        'Set-FluenceAccent',
        'Set-FluenceBackdrop',
        'Close-FluenceWindow',
        'Get-FluenceTheme',
        'Show-FluenceProgress',
        'Update-FluenceProgress',
        'Close-FluenceProgress',
        'Show-FluenceRestartPrompt',
        'Show-FluenceListSelection'
    )
    CmdletsToExport      = @()
    VariablesToExport    = @()
    AliasesToExport      = @()
    FormatsToProcess     = @('Formats/Fluence.Format.ps1xml')
    PrivateData          = @{
        PSData = @{
            Prerelease   = 'pre'
            Tags         = @('GUI', 'WPF', 'Fluent', 'Windows11', 'Dialog', 'PSADT', 'Windows', 'PSEdition_Desktop', 'PSEdition_Core')
            ProjectUri   = 'https://github.com/sintaxasn/Fluence.Wpf'
            LicenseUri   = 'https://github.com/sintaxasn/Fluence.Wpf/blob/main/LICENSE'
            ReleaseNotes = 'https://github.com/sintaxasn/Fluence.Wpf/blob/main/CHANGELOG.md'
        }
    }
}
