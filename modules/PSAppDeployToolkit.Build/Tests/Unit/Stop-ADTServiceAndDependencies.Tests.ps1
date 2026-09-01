BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # These tests need a running service with more than one running dependent, which not every
    # machine has. Found at discovery so that the tests skip where there is none, rather than every one of
    # them failing on a null service the way they used to.
    $script:SubjectName = $null
    foreach ($service in Get-Service)
    {
        if (!$service.Status.Equals([System.ServiceProcess.ServiceControllerStatus]::Running) -or !$service.DependentServices)
        {
            continue
        }
        $matching = 0
        foreach ($dependent in $service.DependentServices)
        {
            if ($dependent.Status.Equals([System.ServiceProcess.ServiceControllerStatus]::Running))
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

    # Mock Stop-Service so that tests can be performed without admin rights or altering the state of the host
    Mock Stop-Service { if ($PesterBoundParameters['PassThru']) { return $PesterBoundParameters.InputObject } } -ModuleName PSAppDeployToolkit

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock Write-ADTLogEntry { } -ModuleName PSAppDeployToolkit
}

Describe 'Stop-ADTServiceAndDependencies' {
    # The service found at discovery is handed in rather than read from a script variable, which is not the
    # same scope once the tests are actually running.
    Context 'Functionality' -Skip:(!$script:SubjectName) -ForEach @(@{ SubjectName = $script:SubjectName }) {
        BeforeAll {
            $script:Subject = Get-Service -Name $SubjectName
            $script:DependentCount = 0
            foreach ($dependent in $script:Subject.DependentServices)
            {
                if ($dependent.Status.Equals([System.ServiceProcess.ServiceControllerStatus]::Running))
                {
                    $script:DependentCount++
                }
            }
        }
        It 'Should stop all dependent services' {
            # One forced call is all this function makes: Stop-Service -Force is what brings the dependents
            # down with the parent, rather than the function stopping each of them itself.
            Stop-ADTServiceAndDependencies -InputObject $script:Subject
            Should -Invoke -CommandName Stop-Service -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }
        It 'Should not attempt to stop services with running dependents when -SkipDependentServices is provided' {
            # Refusing outright is the point: stopping the parent would take the dependents with it, which
            # is the very thing the caller asked to avoid.
            $script:DependentCount | Should -BeGreaterThan 1
            Stop-ADTServiceAndDependencies -InputObject $script:Subject -SkipDependentServices
            Should -Not -Invoke -CommandName Stop-Service -ModuleName PSAppDeployToolkit
        }
        It 'Should accept ServiceController objects through the pipeline' {
            $script:Subject | Stop-ADTServiceAndDependencies
            Should -Invoke -CommandName Stop-Service -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }
        It 'Should return the specified service when -PassThru is provided' {
            $return = Stop-ADTServiceAndDependencies -InputObject $script:Subject -PassThru
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
                ErrorId = 'ParameterArgumentValidationError,Stop-ADTServiceAndDependencies'
            }
            { Stop-ADTServiceAndDependencies -Name $null } | Should @shouldParams
            { Stop-ADTServiceAndDependencies -Name '' } | Should @shouldParams
            { Stop-ADTServiceAndDependencies -Name " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that -DisplayName is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Stop-ADTServiceAndDependencies'
            }
            { Stop-ADTServiceAndDependencies -DisplayName $null } | Should @shouldParams
            { Stop-ADTServiceAndDependencies -DisplayName '' } | Should @shouldParams
            { Stop-ADTServiceAndDependencies -DisplayName " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that -InputObject is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Stop-ADTServiceAndDependencies -InputObject $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Stop-ADTServiceAndDependencies'
            { Stop-ADTServiceAndDependencies -InputObject '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Stop-ADTServiceAndDependencies'
            # White space converts to a ServiceController with no ServiceName, so the parameter's own
            # ValidateScript reports it. It surfaced as a plain binding failure only while
            # New-ADTValidateScriptErrorRecord threw part way through building that error.
            { Stop-ADTServiceAndDependencies -InputObject " `f`n`r`t`v" } | Should -Throw -ExceptionType ([System.ArgumentException]) -ErrorId 'InvalidInputObjectParameterValue,Stop-ADTServiceAndDependencies'
        }
    }
}
