BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTApplication' {
    Context 'Functionality' {
        BeforeAll {
            $script:Applications = @(Get-ADTApplication)
            $script:Sample = $script:Applications | & { process { if (![System.String]::IsNullOrWhiteSpace($_.DisplayName)) { return $_ } } } | Select-Object -First 1
        }

        It 'Finds the applications installed on this machine' {
            $script:Applications.Count | Should -BeGreaterThan 0
            $script:Sample | Should -Not -BeNullOrEmpty
        }

        It 'Gives every result a display name and the key it came from' {
            foreach ($application in $script:Applications)
            {
                $application.DisplayName | Should -Not -BeNullOrEmpty
                $application.PSChildName | Should -Not -BeNullOrEmpty
            }
        }

        It 'Leaves updates and hotfixes out unless asked for them' {
            @(Get-ADTApplication -IncludeUpdatesAndHotfixes).Count | Should -BeGreaterOrEqual $script:Applications.Count
        }

        It 'Matches a name by <NameMatch>' -ForEach @(
            @{ NameMatch = 'Contains' }
            @{ NameMatch = 'Exact' }
            @{ NameMatch = 'Wildcard' }
            @{ NameMatch = 'Regex' }
        ) {
            # The same installed application has to be findable under each mode, with the needle written the
            # way that mode expects.
            $name = $script:Sample.DisplayName
            $needle = switch ($NameMatch)
            {
                'Contains' { $name }
                'Exact' { $name }
                'Wildcard' { "*$name*" }
                'Regex' { [System.Text.RegularExpressions.Regex]::Escape($name) }
            }
            $found = @(Get-ADTApplication -Name $needle -NameMatch $NameMatch)
            $found.DisplayName | Should -Contain $name
        }

        It 'Returns nothing for a name nothing matches' {
            Get-ADTApplication -Name 'ADTNoSuchApplicationIsInstalled12345' | Should -BeNullOrEmpty
        }

        It 'Applies a -FilterScript' {
            $script:WantedName = $script:Sample.DisplayName
            $found = @(Get-ADTApplication -FilterScript { $_.DisplayName -eq $script:WantedName })
            $found.DisplayName | Should -Contain $script:WantedName
        }

        It 'Narrows to <ApplicationType> installers' -ForEach @(
            @{ ApplicationType = 'MSI' }
            @{ ApplicationType = 'EXE' }
        ) {
            # An MSI entry is the one with a product code behind it, which is what decides whether the
            # toolkit uninstalls by code or by running the uninstall string.
            foreach ($application in @(Get-ADTApplication -ApplicationType $ApplicationType))
            {
                if ($ApplicationType -eq 'MSI')
                {
                    $application.WindowsInstaller | Should -BeTrue
                }
                else
                {
                    $application.WindowsInstaller | Should -BeFalse
                }
            }
        }

        It 'Rejects a match mode it does not know' {
            { Get-ADTApplication -Name 'anything' -NameMatch 'NotAMatchMode' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
