BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Invoke-ADTRegSvr32' {
    Context 'Input Validation' {
        It 'Refuses a library that is not there' {
            { Invoke-ADTRegSvr32 -FilePath "$TestDrive\NeverExisted.dll" -Action Register } | Should -Throw -ErrorId 'InvalidFilePathParameterValue,Invoke-ADTRegSvr32'
        }

        It 'Requires a library to work on' {
            Test-ADTMandatoryParameter -Command (Get-Command Invoke-ADTRegSvr32) -Parameter FilePath | Should -BeTrue
        }

        It 'Refuses an action it does not know' {
            # Register and Unregister are opposites, so anything else would have to guess which was meant.
            $library = "$TestDrive\Library.dll"
            Set-Content -LiteralPath $library -Value 'not a library'
            { Invoke-ADTRegSvr32 -FilePath $library -Action 'Frobnicate' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Requires an action' {
            $library = "$TestDrive\Library.dll"
            Set-Content -LiteralPath $library -Value 'not a library'
            Test-ADTMandatoryParameter -Command (Get-Command Invoke-ADTRegSvr32) -Parameter Action | Should -BeTrue
        }

        It 'Refuses an unsupported extension' {
            $library = "$TestDrive\Library.txt"
            Set-Content -LiteralPath $library -Value 'not a library'
            { Invoke-ADTRegSvr32 -FilePath $library -Action Register } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }

    Context 'Registration' {
        It 'Invokes regsvr32 for <Command> with a <Extension> file' -ForEach @(
            @{ Command = 'Register-ADTDll'; Action = 'Register'; Extension = 'ocx' }
            @{ Command = 'Unregister-ADTDll'; Action = 'Unregister'; Extension = 'OCX' }
            @{ Command = 'Invoke-ADTRegSvr32'; Action = 'Register'; Extension = 'dll' }
            @{ Command = 'Invoke-ADTRegSvr32'; Action = 'Unregister'; Extension = 'ocx' }
        ) {
            $library = "$TestDrive\Library.$Extension"
            Set-Content -LiteralPath $library -Value 'not a library'
            Mock -ModuleName PSAppDeployToolkit Get-ADTPEFileArchitecture { [PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_I386 }
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { }

            if ($Command -eq 'Invoke-ADTRegSvr32') {
                Invoke-ADTRegSvr32 -FilePath $library -Action $Action -Confirm:$false -ErrorAction Stop
            }
            else {
                & $Command -FilePath $library -Confirm:$false -ErrorAction Stop
            }

            $expectedArguments = if ($Action -eq 'Register') { "/s `"$library`"" } else { "/s /u `"$library`"" }
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 1 -Exactly -ParameterFilter { $ArgumentList -eq $expectedArguments }
        }
    }
}
