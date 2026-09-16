BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # A service already set to something other than an automatic mode. Asking for the mode it already has
    # is the one path through this function that reaches no further than a log entry, since anything else
    # would reconfigure a service on the machine running the tests.
    $script:AlreadySetName = $null
    foreach ($service in Get-Service)
    {
        if ($service.StartType.Equals([System.ServiceProcess.ServiceStartMode]::Manual) -or $service.StartType.Equals([System.ServiceProcess.ServiceStartMode]::Disabled))
        {
            $script:AlreadySetName = $service.ServiceName
            break
        }
    }
}
BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}
Describe 'Set-ADTServiceStartMode' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'When the mode is already what was asked for' -Skip:(!$script:AlreadySetName) -ForEach @(@{ AlreadySetName = $script:AlreadySetName }) {
        BeforeAll {
            $script:Subject = Get-Service -Name $AlreadySetName
        }

        It 'Returns without reconfiguring anything' {
            Set-ADTServiceStartMode -Name $script:Subject.ServiceName -StartMode $script:Subject.StartType.ToString()
            (Get-Service -Name $script:Subject.ServiceName).StartType | Should -Be $script:Subject.StartType
        }

        It 'Says the mode was already set' {
            Set-ADTServiceStartMode -Name $script:Subject.ServiceName -StartMode $script:Subject.StartType.ToString()
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*is already set to*' }
        }

        It 'Returns the service with -PassThru' {
            $return = Set-ADTServiceStartMode -Name $script:Subject.ServiceName -StartMode $script:Subject.StartType.ToString() -PassThru
            $return | Should -HaveCount 1
            $return.ServiceName | Should -BeExactly $script:Subject.ServiceName
        }

        It 'Returns nothing without -PassThru' {
            Set-ADTServiceStartMode -Name $script:Subject.ServiceName -StartMode $script:Subject.StartType.ToString() | Should -BeNullOrEmpty
        }

        It 'Reconfigures nothing with -WhatIf' {
            # The only guard between this function and sc.exe, so it is worth knowing it holds.
            Set-ADTServiceStartMode -Name $script:Subject.ServiceName -StartMode Automatic -WhatIf
            (Get-Service -Name $script:Subject.ServiceName).StartType | Should -Be $script:Subject.StartType
        }
    }
    Context 'What it reports either side of sc.exe' -Skip:(!$script:AlreadySetName) -ForEach @(@{ AlreadySetName = $script:AlreadySetName }) {
        BeforeAll {
            # sc.exe is stood in for so that what this function reports can be tested without reconfiguring
            # a service on the machine running the tests. The mode asked for is deliberately not the one the
            # service already has, since that path returns before sc.exe is reached at all.
            $script:Subject = Get-Service -Name $AlreadySetName
            $script:WantedMode = if ($script:Subject.StartType.Equals([System.ServiceProcess.ServiceStartMode]::Disabled)) { 'Manual' } else { 'Disabled' }
        }

        It 'Reconfigures nothing while sc.exe is stood in for' {
            # Proves the rest of this context is testing the reporting rather than the machine.
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { [PSADT.ProcessManagement.ProcessResult]::new(0, $null, $null, $null) }
            Set-ADTServiceStartMode -Name $script:Subject.ServiceName -StartMode $script:WantedMode
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 1 -Exactly
            (Get-Service -Name $script:Subject.ServiceName).StartType | Should -Be $script:Subject.StartType
        }

        It 'Says it set the mode when sc.exe succeeded' {
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { [PSADT.ProcessManagement.ProcessResult]::new(0, $null, $null, $null) }
            Set-ADTServiceStartMode -Name $script:Subject.ServiceName -StartMode $script:WantedMode
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Successfully set service*' }
        }

        It 'Says what sc.exe reported when it failed' {
            # The exit code alone says nothing about why a service would not change, so sc.exe's own words
            # have to reach the log. It writes them to standard error, which is what used to be lost.
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { [PSADT.ProcessManagement.ProcessResult]::new(5, $null, [System.String[]]('[SC] OpenService FAILED 5:'), [System.String[]]('[SC] OpenService FAILED 5:')) }
            Set-ADTServiceStartMode -Name $script:Subject.ServiceName -StartMode $script:WantedMode -ErrorAction SilentlyContinue
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*OpenService FAILED 5*' }
        }

        It 'Surfaces the failure to the caller' {
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { [PSADT.ProcessManagement.ProcessResult]::new(5, $null, [System.String[]]('[SC] OpenService FAILED 5:'), [System.String[]]('[SC] OpenService FAILED 5:')) }
            Set-ADTServiceStartMode -Name $script:Subject.ServiceName -StartMode $script:WantedMode -ErrorAction SilentlyContinue -ErrorVariable scError
            # The error variable also collects the intermediate objects each record passes through on its way
            # out, one of which is not an ErrorRecord at all, so the reported failure has to be picked out of
            # it rather than read off the whole collection.
            $reported = @($scError | & { process { if (($_ -is [System.Management.Automation.ErrorRecord]) -and $_.FullyQualifiedErrorId.Equals('ScConfigFailure,Set-ADTServiceStartMode')) { return $_ } } })
            $reported | Should -Not -BeNullOrEmpty
            $reported[0].Exception | Should -BeOfType ([PSADT.ProcessManagement.ProcessException])
            $reported[0].Exception.Message | Should -BeLike '*OpenService FAILED 5*'
        }

        It 'Returns nothing for a service it could not set' {
            # -PassThru saying a service was set when sc.exe refused would have a caller reading a mode the
            # service never took.
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { [PSADT.ProcessManagement.ProcessResult]::new(5, $null, $null, $null) }
            Set-ADTServiceStartMode -Name $script:Subject.ServiceName -StartMode $script:WantedMode -PassThru -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }
    }
    Context 'Input Validation' {
        It 'Should verify that -Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTServiceStartMode'
            }
            { Set-ADTServiceStartMode -Name $null -StartMode 'Automatic' } | Should @shouldParams
            { Set-ADTServiceStartMode -Name '' -StartMode 'Automatic' } | Should @shouldParams
            { Set-ADTServiceStartMode -Name " `f`n`r`t`v" -StartMode 'Automatic' } | Should @shouldParams
        }
        It 'Should verify that -DisplayName is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTServiceStartMode'
            }
            { Set-ADTServiceStartMode -DisplayName $null -StartMode 'Automatic' } | Should @shouldParams
            { Set-ADTServiceStartMode -DisplayName '' -StartMode 'Automatic' } | Should @shouldParams
            { Set-ADTServiceStartMode -DisplayName " `f`n`r`t`v" -StartMode 'Automatic' } | Should @shouldParams
        }
        It 'Should verify that -InputObject is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Set-ADTServiceStartMode -InputObject $null -StartMode 'Automatic' } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Set-ADTServiceStartMode'
            { Set-ADTServiceStartMode -InputObject '' -StartMode 'Automatic' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Set-ADTServiceStartMode'
            # White space converts to a ServiceController with no ServiceName, so the parameter's own
            # ValidateScript reports it. It surfaced as a plain binding failure only while
            # New-ADTValidateScriptErrorRecord threw part way through building that error.
            { Set-ADTServiceStartMode -InputObject " `f`n`r`t`v" -StartMode 'Automatic' } | Should -Throw -ExceptionType ([System.ArgumentException]) -ErrorId 'InvalidInputObjectParameterValue,Set-ADTServiceStartMode'
        }
    }
}
