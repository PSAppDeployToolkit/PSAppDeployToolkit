BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # These tests need a stopped service with more than one stopped dependent, which not every
    # machine has. Found at discovery so that the tests skip where there is none, rather than every one of
    # them failing on a null service the way they used to.
    $script:SubjectName = $null
    foreach ($service in Get-Service)
    {
        if (!$service.Status.Equals([System.ServiceProcess.ServiceControllerStatus]::Stopped) -or !$service.DependentServices)
        {
            continue
        }
        $matching = 0
        foreach ($dependent in $service.DependentServices)
        {
            if ($dependent.Status.Equals([System.ServiceProcess.ServiceControllerStatus]::Stopped))
            {
                $matching++
            }
        }
        if ($matching -gt 1)
        {
            $script:SubjectName = $service.ServiceName
            break
        }
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Start-Service so that tests can be performed without admin rights or altering the state of the host
    Mock Start-Service { if ($PesterBoundParameters['PassThru']) { return $PesterBoundParameters.InputObject } } -ModuleName PSAppDeployToolkit

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock Write-ADTLogEntry { } -ModuleName PSAppDeployToolkit
}

Describe 'Start-ADTServiceAndDependencies' {
    # The service found at discovery is handed in rather than read from a script variable, which is not the
    # same scope once the tests are actually running.
    Context 'Functionality' -Skip:(!$script:SubjectName) -ForEach @(@{ SubjectName = $script:SubjectName }) {
        BeforeAll {
            $script:Subject = Get-Service -Name $SubjectName
            $script:DependentCount = 0
            foreach ($dependent in $script:Subject.DependentServices)
            {
                if ($dependent.Status.Equals([System.ServiceProcess.ServiceControllerStatus]::Stopped))
                {
                    $script:DependentCount++
                }
            }
        }
        It 'Should start all dependent services' {
            # This function starts each stopped dependent itself, in order, before the parent, so the call
            # count follows the number of them.
            Start-ADTServiceAndDependencies -InputObject $script:Subject
            Should -Invoke -CommandName Start-Service -ModuleName PSAppDeployToolkit -Times ($script:DependentCount + 1) -Exactly
        }
        It 'Should not start dependent services when -SkipDependentServices is provided' {
            Start-ADTServiceAndDependencies -InputObject $script:Subject -SkipDependentServices
            Should -Invoke -CommandName Start-Service -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }
        It 'Should accept ServiceController objects through the pipeline' {
            $script:Subject | Start-ADTServiceAndDependencies
            Should -Invoke -CommandName Start-Service -ModuleName PSAppDeployToolkit -Times ($script:DependentCount + 1) -Exactly
        }
        It 'Should return the specified service when -PassThru is provided' {
            $return = Start-ADTServiceAndDependencies -InputObject $script:Subject -PassThru
            $return | Should -HaveCount 1
            $return | Should -BeOfType ([System.ServiceProcess.ServiceController])
            $return.ServiceName | Should -BeExactly $script:Subject.ServiceName
        }
    }
    Context 'Input Validation' {
        It 'Should verify that -Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Start-ADTServiceAndDependencies'
            }
            { Start-ADTServiceAndDependencies -Name $null } | Should @shouldParams
            { Start-ADTServiceAndDependencies -Name '' } | Should @shouldParams
            { Start-ADTServiceAndDependencies -Name " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that -DisplayName is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Start-ADTServiceAndDependencies'
            }
            { Start-ADTServiceAndDependencies -DisplayName $null } | Should @shouldParams
            { Start-ADTServiceAndDependencies -DisplayName '' } | Should @shouldParams
            { Start-ADTServiceAndDependencies -DisplayName " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that -InputObject is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Start-ADTServiceAndDependencies -InputObject $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Start-ADTServiceAndDependencies'
            { Start-ADTServiceAndDependencies -InputObject '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Start-ADTServiceAndDependencies'
            # White space converts to a ServiceController with no ServiceName, so the parameter's own
            # ValidateScript reports it. It surfaced as a plain binding failure only while
            # New-ADTValidateScriptErrorRecord threw part way through building that error.
            { Start-ADTServiceAndDependencies -InputObject " `f`n`r`t`v" } | Should -Throw -ExceptionType ([System.ArgumentException]) -ErrorId 'InvalidInputObjectParameterValue,Start-ADTServiceAndDependencies'
        }
    }
}
