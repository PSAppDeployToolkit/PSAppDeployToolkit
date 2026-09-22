#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Regression test for the Show-FluenceWindow MTA transport contract. On an MTA host (the default
# `pwsh`) the UI thread is a separate module-owned STA runspace, so Resolve-FluenceUserBlock recreates
# the -Content / -Initialize block from its .ToString() text on that runspace: closures and any
# caller-defined commands are NOT visible there. Two facts about that path are pinned here.
#
# Positive case (UI-gated): the only sanctioned channel back out of the recreated block is the -Data
# hashtable, which is passed by reference across the transport. The block mutates $Data.CrossedBack on
# the UI runspace and closes with that value as the dialog result; both the mutation seen by the caller
# and the returned result are asserted. It shows a window and so is tagged UI and FLUENCE_PS_UI-gated.
# It passes on STA and MTA; the MTA run (where $Data crosses a runspace boundary) is the meaningful one.
#
# Negative case (MTA only): a -Content block that references a caller-scope function must surface, not
# swallow, the failure. On MTA the recreated block cannot resolve the caller function, so it throws
# CommandNotFoundException; Set-FluenceWindowContent records it on $State.Error and Show-FluenceWindowCore
# rethrows it BEFORE ShowDialog. Because the build-time failure rethrows before any window is shown, the
# case never blocks on a modal loop and is headless-safe, so it belongs in the logic lane on an MTA host.
# It is gated to MTA: on an STA host the live block would resolve the function and the window would block
# with no auto-close, so the case is skipped there and does not change the STA logic-lane baseline.

# The negative case's $content block keeps the canonical param($Window, $Data) signature shared by every
# -Content / -Initialize block in the suite, but it deliberately fails before touching $Data, so the
# parameter is unread there. The signature is intentional, not a defect.
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Data',
    Justification = 'Content blocks keep the canonical ($Window, $Data) signature; the negative case fails before reading $Data.')]
param()

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force
}

Describe 'Show-FluenceWindow MTA transport' {

    It 'crosses a value back through -Data from the content block' -Tag UI -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $data = @{}
        $content = {
            param($Window, $Data)
            $Data.CrossedBack = 'from-ui-runspace'
            $textBlock = [System.Windows.Controls.TextBlock]::new()
            $textBlock.Text = 'mta positive'
            $Window.Content = $textBlock
            $timer = [System.Windows.Threading.DispatcherTimer]::new()
            $timer.Interval = [timespan]::FromMilliseconds(500)
            $timer.add_Tick({
                $timer.Stop()
                Close-FluenceWindow -Window $Window -Result $Data.CrossedBack
            }.GetNewClosure())
            $timer.Start()
        }
        $result = Show-FluenceWindow -Content $content -Theme Light -Backdrop None -Data $data
        $data.CrossedBack | Should -Be 'from-ui-runspace'
        $result | Should -Be 'from-ui-runspace'
    }

    It 'surfaces a content-block failure that references caller scope (MTA only)' -Skip:([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne [System.Threading.ApartmentState]::MTA) {
        function Get-FluenceCallerOnlyValue
        {
            return 'caller-secret'
        }
        $content = {
            param($Window, $Data)
            # On an MTA host this block is recreated from text in the module's session state on the
            # UI runspace and cannot resolve the caller-defined function, so the call throws
            # CommandNotFoundException, which Show-FluenceWindow must surface (not swallow).
            $null = Get-FluenceCallerOnlyValue
            $Window.Content = [System.Windows.Controls.TextBlock]::new()
        }
        { Show-FluenceWindow -Content $content -Theme Light -Backdrop None } | Should -Throw
    }
}
