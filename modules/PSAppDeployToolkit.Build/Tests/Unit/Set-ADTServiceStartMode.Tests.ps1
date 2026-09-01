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
