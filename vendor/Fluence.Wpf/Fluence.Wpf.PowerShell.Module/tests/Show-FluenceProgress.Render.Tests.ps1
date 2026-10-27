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


#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Opt-in render tests for the progress window. Every It is tagged UI and skips unless
# FLUENCE_PS_UI=1. The same cases run on every host shape: inline STA (powershell.exe -STA, pwsh)
# where the window shares the test thread, and pwsh -mta where it lives on the module-owned UI
# runspace behind the queue-serviced pump. Control properties are dependency properties and can only
# be read on the owning thread, so the assertions on the MTA host go through the shared State and the
# handle; the inline host additionally reads the live controls.

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force
    $script:IsInlineHost = [System.Threading.Thread]::CurrentThread.GetApartmentState() -eq [System.Threading.ApartmentState]::STA
}

Describe 'Show-FluenceProgress render' -Tag UI {

    It 'opens, updates three times, and closes, tracking state on the handle' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $handle = Show-FluenceProgress -Title 'Render Progress' -Message 'Starting' -Theme Light -Backdrop None -Position BottomRight
        try
        {
            $handle.PSObject.TypeNames[0] | Should -Be 'Fluence.ProgressHandle'
            $handle.IsOpen | Should -BeTrue
            $handle.Mode | Should -BeIn @('Inline', 'Runspace')
            $handle.State.Indeterminate | Should -BeTrue

            Start-Sleep -Milliseconds 300
            Update-FluenceProgress -Handle $handle -Message 'Step one' -PercentComplete 33
            $handle.State.Message | Should -Be 'Step one'
            $handle.State.PercentComplete | Should -Be 33
            $handle.State.Indeterminate | Should -BeFalse

            Start-Sleep -Milliseconds 300
            Update-FluenceProgress -Handle $handle -Detail 'Copying files' -PercentComplete 150
            $handle.State.Detail | Should -Be 'Copying files'
            $handle.State.PercentComplete | Should -Be 100

            Start-Sleep -Milliseconds 300
            Update-FluenceProgress -Handle $handle -Message 'Finishing' -Indeterminate
            $handle.State.Indeterminate | Should -BeTrue

            if ($script:IsInlineHost)
            {
                $handle.Parts.Window.IsVisible | Should -BeTrue
                $handle.Parts.MessageText.Text | Should -Be 'Finishing'
                $handle.Parts.DetailText.Text | Should -Be 'Copying files'
                $handle.Parts.Bar.ProgressMode | Should -Be ([Fluence.Wpf.ProgressBarMode]::Indeterminate)
            }
            Start-Sleep -Milliseconds 300
        }
        finally
        {
            Close-FluenceProgress -Handle $handle
        }

        $handle.IsOpen | Should -BeFalse
        if ($script:IsInlineHost)
        {
            $handle.Parts.Window.IsVisible | Should -BeFalse
        }
    }

    It 'refuses a second progress window while one is open, and allows one after close' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $first = Show-FluenceProgress -Message 'First' -Theme Light -Backdrop None
        try
        {
            { Show-FluenceProgress -Message 'Second' -Theme Light -Backdrop None } | Should -Throw '*already open*'
        }
        finally
        {
            Close-FluenceProgress -Handle $first
        }

        $second = Show-FluenceProgress -Message 'Second' -Theme Light -Backdrop None
        try
        {
            $second.IsOpen | Should -BeTrue
        }
        finally
        {
            Close-FluenceProgress -Handle $second
        }
        $second.IsOpen | Should -BeFalse
    }

    It 'shows a message dialog while the progress window is open' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        # On an MTA host the dialog is queued to the pump that keeps the progress window live; on the
        # inline host ShowDialog pumps the shared thread. Either way the dialog must open, close and
        # return, and the progress window must survive it. The dialog is closed by its own timeout.
        $handle = Show-FluenceProgress -Message 'Long running' -Theme Light -Backdrop None
        try
        {
            $answer = Show-FluenceMessage -Message 'Dialog over progress' -Buttons OK -Timeout 1 -Theme Light -Backdrop None
            $answer | Should -Be 'OK'
            $handle.IsOpen | Should -BeTrue
            Update-FluenceProgress -Handle $handle -Message 'Still here' -PercentComplete 90
            $handle.State.Message | Should -Be 'Still here'
        }
        finally
        {
            Close-FluenceProgress -Handle $handle
        }
    }
}
