BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Send-ADTKeys' {
    # Only windows that cannot exist are addressed. Sending keystrokes to a real window would type into
    # whatever the machine running the tests happens to have open.
    Context 'When no window matches' {
        BeforeAll {
            $script:AbsentTitle = "ADTNoSuchWindow$([System.Guid]::NewGuid().ToString('N'))"
        }

        It 'Does not object' {
            { Send-ADTKeys -WindowTitle $script:AbsentTitle -Keys 'abc' } | Should -Not -Throw
        }

        It 'Says it found no window' {
            Send-ADTKeys -WindowTitle $script:AbsentTitle -Keys 'abc'
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*window*' }
        }

        It 'Returns nothing' {
            Send-ADTKeys -WindowTitle $script:AbsentTitle -Keys 'abc' | Should -BeNullOrEmpty
        }

        It 'Does not object to a window handle nothing owns' {
            # A handle that has gone stale between being looked up and being used is the ordinary case,
            # not an exceptional one.
            { Send-ADTKeys -WindowHandle ([System.IntPtr]::new(-1)) -Keys 'abc' } | Should -Not -Throw
        }
    }

    Context 'Input Validation' {
        It 'Requires keys to send' {
            Test-ADTParameterSetSatisfied -Command (Get-Command Send-ADTKeys) -Parameter WindowTitle | Should -BeFalse
        }

        It 'Requires a window to send them to' {
            Test-ADTParameterSetSatisfied -Command (Get-Command Send-ADTKeys) -Parameter Keys | Should -BeFalse
        }

        It 'Refuses a title and a handle together' {
            { Send-ADTKeys -WindowTitle 'Anything' -WindowHandle ([System.IntPtr]::new(1)) -Keys 'abc' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a wait that is not a duration' {
            { Send-ADTKeys -WindowTitle 'Anything' -Keys 'abc' -WaitDuration 'a while' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
