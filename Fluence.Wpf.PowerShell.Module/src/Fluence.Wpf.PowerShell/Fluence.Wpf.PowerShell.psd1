<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


@{
    RootModule           = 'Fluence.Wpf.PowerShell.psm1'
    ModuleVersion        = '0.9.5'
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
            Prerelease   = ''
            Tags         = @('GUI', 'WPF', 'Fluent', 'Windows11', 'Dialog', 'Windows', 'PSEdition_Desktop', 'PSEdition_Core')
            ProjectUri   = 'https://github.com/sintaxasn/Fluence.Wpf'
            LicenseUri   = 'https://github.com/sintaxasn/Fluence.Wpf/blob/main/LICENSE'
            ReleaseNotes = 'https://github.com/sintaxasn/Fluence.Wpf/blob/main/CHANGELOG.md'
        }
    }
}
