BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTDeferHistory' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Forwarding behaviour' {
        It 'Returns the value produced by the session GetDeferHistory() method' {
            $expected = [PSAppDeployToolkit.Foundation.DeferHistory]::new([System.UInt32]3, ([System.DateTime]'2026-01-01T00:00:00Z'), $null)
            $fakeSession = [PSCustomObject]@{ }
            $fakeSession | Add-Member -MemberType ScriptMethod -Name GetDeferHistory -Value { return $expected }.GetNewClosure()
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $fakeSession }

            $result = Get-ADTDeferHistory
            $result | Should -BeOfType ([PSAppDeployToolkit.Foundation.DeferHistory])
            $result.DeferTimesRemaining | Should -Be 3
        }

        It 'Invokes Get-ADTSession exactly once' {
            $fakeSession = [PSCustomObject]@{ }
            $fakeSession | Add-Member -MemberType ScriptMethod -Name GetDeferHistory -Value { return $null }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $fakeSession }

            Get-ADTDeferHistory | Out-Null
            Should -Invoke -ModuleName PSAppDeployToolkit Get-ADTSession -Times 1 -Exactly
        }

        It 'Returns nothing when the session reports no deferral history' {
            $fakeSession = [PSCustomObject]@{ }
            $fakeSession | Add-Member -MemberType ScriptMethod -Name GetDeferHistory -Value { return $null }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $fakeSession }

            $result = Get-ADTDeferHistory
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Error handling' {
        It 'Surfaces a terminating error when no active session exists' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { throw [System.InvalidOperationException]::new('No active session.') }
            { Get-ADTDeferHistory } | Should -Throw -ExpectedMessage 'No active session.'
        }

        It 'Surfaces a terminating error when the session method throws' {
            $fakeSession = [PSCustomObject]@{ }
            $fakeSession | Add-Member -MemberType ScriptMethod -Name GetDeferHistory -Value { throw [System.InvalidOperationException]::new('GetDeferHistory failed.') }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $fakeSession }

            { Get-ADTDeferHistory } | Should -Throw -ExpectedMessage '*GetDeferHistory failed.*'
        }
    }

    Context 'Contract' {
        It 'Declares an output type of PSAppDeployToolkit.Foundation.DeferHistory' {
            (Get-Command Get-ADTDeferHistory).OutputType.Type | Should -Contain ([PSAppDeployToolkit.Foundation.DeferHistory])
        }

        It 'Accepts no parameters beyond the common ones' {
            $declared = (Get-Command Get-ADTDeferHistory).Parameters.Keys.Where({ -not [System.Management.Automation.PSCmdlet]::CommonParameters.Contains($_) })
            $declared | Should -BeNullOrEmpty
        }
    }
}
