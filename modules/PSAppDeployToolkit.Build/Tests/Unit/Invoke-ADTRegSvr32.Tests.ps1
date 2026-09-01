BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Invoke-ADTRegSvr32' {
    # Contract only. This is what Register-ADTDll and Unregister-ADTDll both call, and either way round it
    # changes the machine's class registrations.
    Context 'Input Validation' {
        It 'Refuses a library that is not there' {
            { Invoke-ADTRegSvr32 -FilePath "$TestDrive\NeverExisted.dll" -Action Register } | Should -Throw -ErrorId 'InvalidFilePathParameterValue,Invoke-ADTRegSvr32'
        }

        It 'Requires a library to work on' {
            { Invoke-ADTRegSvr32 -Action Register } | Should -Throw -ErrorId 'MissingMandatoryParameter,Invoke-ADTRegSvr32'
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
            { Invoke-ADTRegSvr32 -FilePath $library } | Should -Throw -ErrorId 'MissingMandatoryParameter,Invoke-ADTRegSvr32'
        }
    }
}
