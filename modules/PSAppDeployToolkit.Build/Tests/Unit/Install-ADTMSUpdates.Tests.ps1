BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Install-ADTMSUpdates' {
    # Installing an update is a change to the machine that outlives the test run, so only the paths that
    # are rejected before anything is installed are covered.
    Context 'Input Validation' {
        It 'Refuses a directory holding no updates' {
            # Pointing it at the wrong folder is a silent no-op otherwise, and a deployment would report
            # success having installed nothing.
            $empty = "$TestDrive\NoUpdates"
            $null = New-Item -Path $empty -ItemType Directory -Force
            { Install-ADTMSUpdates -LiteralPath $empty } | Should -Throw -ErrorId 'InvalidLiteralPathParameterValue,Install-ADTMSUpdates'
        }

        It 'Refuses a file that is not an update' {
            $notAnUpdate = "$TestDrive\NotAnUpdate.txt"
            Set-Content -LiteralPath $notAnUpdate -Value 'not an update'
            { Install-ADTMSUpdates -LiteralPath $notAnUpdate } | Should -Throw -ErrorId 'InvalidLiteralPathParameterValue,Install-ADTMSUpdates'
        }

        It 'Refuses a path that is not there' {
            { Install-ADTMSUpdates -LiteralPath "$TestDrive\NeverExisted" } | Should -Throw -ErrorId 'InvalidLiteralPathParameterValue,Install-ADTMSUpdates'
        }

        It 'Requires a path' {
            { Install-ADTMSUpdates } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Answers to its former parameter name' {
            # -Directory is what deployments written against 3.x call it, and the alias is what keeps
            # those scripts working.
            { Install-ADTMSUpdates -Directory "$TestDrive\NeverExisted" } | Should -Throw -ErrorId 'InvalidLiteralPathParameterValue,Install-ADTMSUpdates'
        }
    }
}
