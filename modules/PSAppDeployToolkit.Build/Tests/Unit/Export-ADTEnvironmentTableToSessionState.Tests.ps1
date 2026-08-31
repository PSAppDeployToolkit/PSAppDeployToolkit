BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Export-ADTEnvironmentTableToSessionState' {
    Context 'Before initialisation' {
        It 'Refuses to export an environment that was never built' {
            { Export-ADTEnvironmentTableToSessionState } | Should -Throw -ErrorId 'ADTEnvironmentDatabaseEmpty,Export-ADTEnvironmentTableToSessionState'
        }
    }

    Context 'After initialisation' {
        BeforeAll {
            Initialize-ADTTestModule -Path $TestDrive
        }

        It 'Creates a variable for each environment entry' {
            # This is what lets a deployment script write $envComputerName without asking for it, so the
            # variables have to land in the caller's own scope rather than the module's.
            function Test-Export
            {
                Export-ADTEnvironmentTableToSessionState -SessionState $ExecutionContext.SessionState
                return Get-Variable -Name 'envComputerName' -ValueOnly -ErrorAction Ignore
            }
            Test-Export | Should -BeExactly (Get-ADTEnvironmentTable).envComputerName
        }

        It 'Exports the whole table, not a selection' {
            function Test-ExportCount
            {
                Export-ADTEnvironmentTableToSessionState -SessionState $ExecutionContext.SessionState
                $exported = (Get-ADTEnvironmentTable).PSObject.Properties.Name | & { process { if (Get-Variable -Name $_ -ErrorAction Ignore) { return $_ } } }
                return @($exported).Count
            }
            Test-ExportCount | Should -Be @((Get-ADTEnvironmentTable).PSObject.Properties).Count
        }

        It 'Makes the exported variables read-only' {
            # A deployment script overwriting $envWinDir by accident would be very hard to trace, so the
            # export locks them.
            function Test-ExportReadOnly
            {
                Export-ADTEnvironmentTableToSessionState -SessionState $ExecutionContext.SessionState
                try { Set-Variable -Name 'envWinDir' -Value 'changed' -ErrorAction Stop; return 'WRITABLE' } catch { return $_.FullyQualifiedErrorId }
            }
            Test-ExportReadOnly | Should -BeExactly 'VariableNotWritable,Microsoft.PowerShell.Commands.SetVariableCommand'
        }

        It 'Defaults to the caller when no session state is given' {
            function Test-ExportDefault
            {
                Export-ADTEnvironmentTableToSessionState
                return Get-Variable -Name 'envComputerName' -ValueOnly -ErrorAction Ignore
            }
            Test-ExportDefault | Should -BeExactly (Get-ADTEnvironmentTable).envComputerName
        }
    }
}
