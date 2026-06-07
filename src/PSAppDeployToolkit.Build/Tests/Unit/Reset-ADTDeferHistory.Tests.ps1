BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Reset-ADTDeferHistory' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Forwarding behaviour' {
        It 'Invokes the session ResetDeferHistory() method' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'callCount', Justification = 'Mutated inside a Pester scriptblock.')]
            $callCount = [PSCustomObject]@{ Value = 0 }
            $fakeSession = [PSCustomObject]@{ }
            $fakeSession | Add-Member -MemberType ScriptMethod -Name ResetDeferHistory -Value { $callCount.Value++ }.GetNewClosure()
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $fakeSession }

            Reset-ADTDeferHistory
            $callCount.Value | Should -Be 1
        }

        It 'Invokes Get-ADTSession exactly once' {
            $fakeSession = [PSCustomObject]@{ }
            $fakeSession | Add-Member -MemberType ScriptMethod -Name ResetDeferHistory -Value { }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $fakeSession }

            Reset-ADTDeferHistory
            Should -Invoke -ModuleName PSAppDeployToolkit Get-ADTSession -Times 1 -Exactly
        }

        It 'Returns no output' {
            $fakeSession = [PSCustomObject]@{ }
            $fakeSession | Add-Member -MemberType ScriptMethod -Name ResetDeferHistory -Value { }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $fakeSession }

            $result = Reset-ADTDeferHistory
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Error handling' {
        It 'Surfaces a terminating error when no active session exists' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { throw [System.InvalidOperationException]::new('No active session.') }
            { Reset-ADTDeferHistory } | Should -Throw -ExpectedMessage 'No active session.'
        }

        It 'Surfaces a terminating error when the session method throws' {
            $fakeSession = [PSCustomObject]@{ }
            $fakeSession | Add-Member -MemberType ScriptMethod -Name ResetDeferHistory -Value { throw [System.InvalidOperationException]::new('ResetDeferHistory failed.') }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $fakeSession }

            { Reset-ADTDeferHistory } | Should -Throw -ExpectedMessage '*ResetDeferHistory failed.*'
        }
    }

    Context 'Contract' {
        It 'Accepts no parameters beyond the common ones' {
            $declared = (Get-Command Reset-ADTDeferHistory).Parameters.Keys.Where({ -not [System.Management.Automation.PSCmdlet]::CommonParameters.Contains($_) })
            $declared | Should -BeNullOrEmpty
        }
    }
}
