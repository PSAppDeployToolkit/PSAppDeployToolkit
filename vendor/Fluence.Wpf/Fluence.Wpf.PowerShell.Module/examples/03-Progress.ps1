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


# 03-Progress.ps1 - A five-step deployment loop behind a non-modal progress window: indeterminate while
# preparing, then determinate with a message and detail line per step, then closed.
# Run: pwsh -File 03-Progress.ps1   OR   powershell.exe -File 03-Progress.ps1

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
