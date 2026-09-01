BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Uninstall-ADTApplication' {
    Context 'When nothing matches' {
        # Only searches that cannot match anything installed are performed. Uninstalling software from the
        # machine running the tests is not something a test run gets to do, so the removal itself is left
        # to a deployment.
        BeforeAll {
            $script:Absent = "ADTNoSuchApplication$([System.Guid]::NewGuid().ToString('N'))"
        }

        It 'Removes nothing and says nothing' {
            Uninstall-ADTApplication -Name $script:Absent | Should -BeNullOrEmpty
        }

        It 'Does not object' {
            # A deployment uninstalls a previous version that may never have been there.
            { Uninstall-ADTApplication -Name $script:Absent } | Should -Not -Throw
        }

        It 'Finds nothing to remove for a product code nothing carries' {
            { Uninstall-ADTApplication -ProductCode ([System.Guid]::NewGuid()) } | Should -Not -Throw
        }

        It 'Finds nothing to remove for a filter that matches nothing' {
            { Uninstall-ADTApplication -FilterScript { $false } } | Should -Not -Throw
        }

        It 'Accepts each way of matching a name' -ForEach @(
            @{ Mode = 'Contains' }
            @{ Mode = 'Exact' }
            @{ Mode = 'Wildcard' }
            @{ Mode = 'Regex' }
        ) {
            { Uninstall-ADTApplication -Name $script:Absent -NameMatch $Mode } | Should -Not -Throw
        }
    }

    Context 'Input Validation' {
        It 'Refuses a way of matching it does not know' {
            { Uninstall-ADTApplication -Name 'Anything' -NameMatch 'Fuzzy' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses an application type it does not know' {
            { Uninstall-ADTApplication -Name 'Anything' -ApplicationType 'MSP' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a product code that is not a GUID' {
            { Uninstall-ADTApplication -ProductCode 'not-a-guid' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a search alongside an application it was handed' {
            # The two parameter sets are the two ways of naming what to remove, and mixing them would
            # leave it ambiguous which one won.
            { Uninstall-ADTApplication -Name 'Anything' -InstalledApplication (New-Object -TypeName PSObject) } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
