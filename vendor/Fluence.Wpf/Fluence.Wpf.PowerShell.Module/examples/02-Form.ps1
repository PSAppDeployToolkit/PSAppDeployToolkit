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


# 02-Form.ps1 - Mixed form with Text, Number, Choice, Date, and Checkbox prompts.
# Run: pwsh -File 02-Form.ps1   OR   powershell.exe -File 02-Form.ps1

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
