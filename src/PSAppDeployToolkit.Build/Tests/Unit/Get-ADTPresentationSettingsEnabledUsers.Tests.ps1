BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTPresentationSettingsEnabledUsers' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Basic invocation' {
        It 'Does not throw when called with no parameters' {
            { Get-ADTPresentationSettingsEnabledUsers } | Should -Not -Throw
        }

        It 'Accepts no parameters' {
            { Get-ADTPresentationSettingsEnabledUsers } | Should -Not -Throw
        }
    }

    Context 'Return value' {
        It 'Returns $null or a non-empty collection' {
            # On most machines no user has presentation mode enabled; result may be null.
            $result = Get-ADTPresentationSettingsEnabledUsers
            # The result is either null, or a non-empty collection of UserProfileInfo.
            if ($null -ne $result)
            {
                $resultArray = @($result)
                $resultArray.Count | Should -BeGreaterThan 0
            }
        }

        It 'Any returned objects are of type PSADT.Types.UserProfileInfo' {
            $result = @(Get-ADTPresentationSettingsEnabledUsers)
            foreach ($item in $result)
            {
                $item | Should -BeOfType ([PSADT.Types.UserProfileInfo])
            }
        }

        It 'Any returned objects have a non-null ProfilePath' {
            $result = @(Get-ADTPresentationSettingsEnabledUsers)
            foreach ($item in $result)
            {
                $item.ProfilePath | Should -Not -BeNull
            }
        }
    }
}
