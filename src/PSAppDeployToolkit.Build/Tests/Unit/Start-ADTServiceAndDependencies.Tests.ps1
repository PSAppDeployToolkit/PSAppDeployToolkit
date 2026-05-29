BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Start-ADTServiceAndDependencies' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'serviceWithMultipleStoppedDependentServices', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $serviceWithMultipleStoppedDependentServices = $null
        $dependentServices = [System.Collections.Generic.List[System.ServiceProcess.ServiceController]]::new()

        # Find a service that has more than one running dependent service
        foreach ($service in Get-Service)
        {
            if ($service.Status -ne [System.ServiceProcess.ServiceControllerStatus]::Stopped)
            {
                continue
            }

            if (!$service.DependentServices)
            {
                continue
            }

            $dependentServices.Clear()

            foreach ($dependentService in $service.DependentServices)
            {
                if ($dependentService.Status -eq [System.ServiceProcess.ServiceControllerStatus]::Stopped)
                {
                    $dependentServices.Add($dependentService)
                }
            }

            if ($dependentServices.Count -gt 1)
            {
                $serviceWithMultipleStoppedDependentServices = $service
                break
            }
        }

        # Mock Start-Service so that tests can be performed without admin rights or altering the state of the host
        Mock Start-Service { if ($PesterBoundParameters['PassThru']) { return $PesterBoundParameters.InputObject } } -ModuleName PSAppDeployToolkit

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock Write-ADTLogEntry { } -ModuleName PSAppDeployToolkit
    }

    Context 'Functionality' {
        It 'Should start all dependent services' {
            Start-ADTServiceAndDependencies -InputObject $serviceWithMultipleStoppedDependentServices
            Should -Invoke -CommandName Start-Service -ModuleName PSAppDeployToolkit -Times ($dependentServices.Count + 1) -Exactly
        }
        It 'Should not start dependent services when -SkipDependentServices is provided' {
            Start-ADTServiceAndDependencies -InputObject $serviceWithMultipleStoppedDependentServices -SkipDependentServices
            Should -Invoke -CommandName Start-Service -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }
        It 'Should accept ServiceController objects through the pipeline' {
            $serviceWithMultipleStoppedDependentServices | Start-ADTServiceAndDependencies
            Should -Invoke -CommandName Start-Service -ModuleName PSAppDeployToolkit -Times ($dependentServices.Count + 1) -Exactly
        }
        It 'Should return the specified service when -PassThru is provided' {
            $return = Start-ADTServiceAndDependencies -InputObject $serviceWithMultipleStoppedDependentServices -PassThru
            $return | Should -HaveCount 1
            $return | Should -BeOfType ([System.ServiceProcess.ServiceController])
            $return.ServiceName | Should -BeExactly $serviceWithMultipleStoppedDependentServices.ServiceName
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
            { Start-ADTServiceAndDependencies -InputObject " `f`n`r`t`v" } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Start-ADTServiceAndDependencies'
        }
    }
}
