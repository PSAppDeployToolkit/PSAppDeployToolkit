#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Regression test for C1: a scriptblock transported to the module-owned STA runspace (the path an
# MTA host such as the default `pwsh` takes) must run inside the module session state so that
# module-PRIVATE functions resolve. This mirrors the production path (Show-FluenceDialog routes
# `Invoke-OnFluenceUi -Script { Invoke-FluenceWindow ... }`, where Invoke-FluenceWindow is private),
# which is exactly what the render harness deliberately bridges around with `& (Get-Module){}`.
#
# The contract under test: the INNER scriptblock handed to Invoke-OnFluenceUi must NOT contain its
# own `& (Get-Module){}` bridge. It calls the private Test-FluenceInput directly and relies on the
# host (Invoke-InFluenceStaRunspace) to provide module scope. Without the C1 fix this throws
# CommandNotFoundException on the MTA runspace branch (the term 'Test-FluenceInput' is not
# recognized); with the fix it resolves and returns IsValid = $true. The test passes a non-empty
# Prompts array because Test-FluenceInput's -Prompts parameter is mandatory and rejects @(); an
# empty array would fail parameter binding even with module scope and would not isolate C1.
#
# Reaching the private host Invoke-OnFluenceUi from the test body via `& (Get-Module){}` is the test
# harness bridging to a private command (allowed); the bug under test is whether the host then makes
# the module's OWN private functions visible to the transported inner block (it must). These run
# under STA (the inline path) and under `pwsh -mta` (the runspace-transport path); only the latter
# exercises the defect, so the test genuinely fails without the fix on MTA.

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force
}

Describe 'Invoke-OnFluenceUi private-scope transport (C1 regression)' {

    It 'resolves a module-private function inside the transported scriptblock' {
        # Inner block calls ONLY the private Test-FluenceInput by name, with no bridge of its own, so
        # it isolates exactly the C1 contract. A bare no-rule prompt object validates IsValid = $true.
        # On an MTA host this is transported by text to the STA runspace; the host must run it in
        # module scope or it throws CommandNotFoundException (the C1 defect).
        $result = & (Get-Module Fluence.Wpf.PowerShell) {
            Invoke-OnFluenceUi -Script {
                $prompt = [pscustomobject]@{
                    Name             = 'X'
                    ValidateNotEmpty = $false
                    ValidatePattern  = $null
                    ValidateScript   = $null
                }
                Test-FluenceInput -Prompts @($prompt) -Values @{ X = 'anything' }
            } -ArgumentList @()
        }

        # Invoke-OnFluenceUi yields the bare result hashtable (it unwraps the single-element runspace
        # collection internally), so the result is asserted directly.
        $result.IsValid | Should -BeTrue
    }

    It 'passes ArgumentList through the transport to the private function in module scope' {
        # Exercises the ArgumentList splat through the transport with a private call that consumes
        # the argument, again with no bridge inside the inner block. The Values hashtable is passed
        # as ArgumentList (the production shape: Show-FluenceDialog passes @($spec)), so this does not
        # depend on hoisting a Pester-scoped variable into the module block.
        $result = & (Get-Module Fluence.Wpf.PowerShell) {
            Invoke-OnFluenceUi -Script {
                param($values)
                $prompt = [pscustomobject]@{
                    Name             = 'U'
                    ValidateNotEmpty = $true
                    ValidatePattern  = $null
                    ValidateScript   = $null
                }
                Test-FluenceInput -Prompts @($prompt) -Values $values
            } -ArgumentList @(@{ U = 'present' })
        }

        $result.IsValid | Should -BeTrue
    }
}
