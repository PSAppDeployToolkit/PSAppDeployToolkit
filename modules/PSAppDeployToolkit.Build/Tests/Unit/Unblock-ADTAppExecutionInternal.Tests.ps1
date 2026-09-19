BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Unblock-ADTAppExecutionInternal' {
    # Contract only. This is the worker Unblock-ADTAppExecution calls and the scheduled task it registers
    # runs at startup, so everything it does is to the machine's execution options and task library.
    Context 'Input Validation' {
        It 'Refuses a blank task name' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Unblock-ADTAppExecutionInternal -TaskName '   ' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }

        It 'Refuses a name and a set of tasks together' {
            # They are the two ways of naming what to clean up, and one of them has to win.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Unblock-ADTAppExecutionInternal -TaskName 'anything' -Tasks @() } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }
    }

    Context 'Ownership of Image File Execution Options keys' {
        # The enumeration is a wildcard over the whole hive, so it returns keys other products wrote and
        # the Debugger test is the only thing keeping their configuration intact. Driven through mocked
        # registry cmdlets because the real ones would be the machine's own execution options.
        BeforeEach {
            Mock -ModuleName PSAppDeployToolkit Get-ChildItem { }
            Mock -ModuleName PSAppDeployToolkit Get-ItemProperty { } -ParameterFilter { $null -ne $LiteralPath }
            Mock -ModuleName PSAppDeployToolkit Remove-ItemProperty { }
            Mock -ModuleName PSAppDeployToolkit Remove-Item { }
            Mock -ModuleName PSAppDeployToolkit Get-ScheduledTask { }
        }

        It 'Leaves a key alone when it carries no Debugger value' {
            # Windows' own per-path filtering writes MyFilter\FilterFullPath with no Debugger alongside it,
            # and -Name returns the key when either value is present, so the absent one has to be tolerated.
            Mock -ModuleName PSAppDeployToolkit Get-ItemProperty -ParameterFilter { $null -ne $Path } -MockWith {
                [pscustomobject]@{
                    FilterFullPath = 'C:\Program Files\Other Product\app.exe'
                    PSParentPath = 'TestRegistry:\NotOurs'
                    PSPath = 'TestRegistry:\NotOurs\MyFilter'
                    PSChildName = 'MyFilter'
                }
            }
            InModuleScope -ModuleName PSAppDeployToolkit { Unblock-ADTAppExecutionInternal -TaskName 'ADTTestOnlyNeverRegistered' }
            Should -Invoke -ModuleName PSAppDeployToolkit Remove-ItemProperty -Times 0 -Exactly
            Should -Invoke -ModuleName PSAppDeployToolkit Remove-Item -Times 0 -Exactly
        }

        It 'Leaves a key alone when its Debugger belongs to something else' {
            Mock -ModuleName PSAppDeployToolkit Get-ItemProperty -ParameterFilter { $null -ne $Path } -MockWith {
                [pscustomobject]@{
                    Debugger = 'C:\Program Files\Other Product\debugger.exe'
                    PSParentPath = 'TestRegistry:\NotOurs'
                    PSPath = 'TestRegistry:\NotOurs\app.exe'
                    PSChildName = 'app.exe'
                }
            }
            InModuleScope -ModuleName PSAppDeployToolkit { Unblock-ADTAppExecutionInternal -TaskName 'ADTTestOnlyNeverRegistered' }
            Should -Invoke -ModuleName PSAppDeployToolkit Remove-ItemProperty -Times 0 -Exactly
            Should -Invoke -ModuleName PSAppDeployToolkit Remove-Item -Times 0 -Exactly
        }

        It 'Removes a key the toolkit wrote' {
            Mock -ModuleName PSAppDeployToolkit Get-ItemProperty -ParameterFilter { $null -ne $Path } -MockWith {
                [pscustomobject]@{
                    Debugger = '"C:\Program Files\PSAppDeployToolkit\PSADT.ClientServer.Client.Launcher.Compatible.exe" /smd'
                    PSParentPath = 'TestRegistry:\Ours'
                    PSPath = 'TestRegistry:\Ours\app.exe'
                    PSChildName = 'app.exe'
                }
            }
            InModuleScope -ModuleName PSAppDeployToolkit { Unblock-ADTAppExecutionInternal -TaskName 'ADTTestOnlyNeverRegistered' }
            Should -Invoke -ModuleName PSAppDeployToolkit Remove-ItemProperty -Times 1 -Exactly -ParameterFilter { $Name -eq 'Debugger' }
        }

        It 'Removes a key the toolkit wrote whose value name is stored in another case' {
            # A registry value name comes back as whatever case it was written in, and the registry treats
            # them all as one name, so the test for its presence cannot be the one thing that does not.
            Mock -ModuleName PSAppDeployToolkit Get-ItemProperty -ParameterFilter { $null -ne $Path } -MockWith {
                [pscustomobject]@{
                    debugger = '"C:\Program Files\PSAppDeployToolkit\PSADT.ClientServer.Client.Launcher.Compatible.exe" /smd'
                    PSParentPath = 'TestRegistry:\Ours'
                    PSPath = 'TestRegistry:\Ours\app.exe'
                    PSChildName = 'app.exe'
                }
            }
            InModuleScope -ModuleName PSAppDeployToolkit { Unblock-ADTAppExecutionInternal -TaskName 'ADTTestOnlyNeverRegistered' }
            Should -Invoke -ModuleName PSAppDeployToolkit Remove-ItemProperty -Times 1 -Exactly -ParameterFilter { $Name -eq 'Debugger' }
        }

        It 'Removes the filter subkey of a key the toolkit wrote' {
            Mock -ModuleName PSAppDeployToolkit Get-ItemProperty -ParameterFilter { $null -ne $Path } -MockWith {
                [pscustomobject]@{
                    Debugger = '"C:\Program Files\PSAppDeployToolkit\PSADT.ClientServer.Client.Launcher.Compatible.exe" /smd'
                    FilterFullPath = 'C:\Program Files\Vendor\app.exe'
                    PSParentPath = 'TestRegistry:\Ours'
                    PSPath = 'TestRegistry:\Ours\MyFilter'
                    PSChildName = 'MyFilter'
                }
            }
            InModuleScope -ModuleName PSAppDeployToolkit { Unblock-ADTAppExecutionInternal -TaskName 'ADTTestOnlyNeverRegistered' }
            Should -Invoke -ModuleName PSAppDeployToolkit Remove-ItemProperty -Times 1 -Exactly -ParameterFilter { $Name -eq 'UseFilter' }
            Should -Invoke -ModuleName PSAppDeployToolkit Remove-Item -Times 1 -Exactly -ParameterFilter { $LiteralPath -eq 'TestRegistry:\Ours\MyFilter' }
        }
    }
}
