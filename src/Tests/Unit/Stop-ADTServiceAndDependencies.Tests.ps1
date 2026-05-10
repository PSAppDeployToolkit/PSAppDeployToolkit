BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Stop-ADTServiceAndDependencies' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'serviceWithMultipleRunningDependentServices', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $serviceWithMultipleRunningDependentServices = $null
        $dependentServices = [System.Collections.Generic.List[System.ServiceProcess.ServiceController]]::new()

        # Find a service that has more than one running dependent service
        foreach ($service in Get-Service)
        {
            if ($service.Status -ne [System.ServiceProcess.ServiceControllerStatus]::Running)
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
                if ($dependentService.Status -eq [System.ServiceProcess.ServiceControllerStatus]::Running)
                {
                    $dependentServices.Add($dependentService)
                }
            }

            if ($dependentServices.Count -gt 1)
            {
                $serviceWithMultipleRunningDependentServices = $service
                break
            }
        }

        # Mock Stop-Service so that tests can be performed without admin rights or altering the state of the host
        Mock Stop-Service { if ($PesterBoundParameters['PassThru']) { return $PesterBoundParameters.InputObject } } -ModuleName PSAppDeployToolkit

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock Write-ADTLogEntry { } -ModuleName PSAppDeployToolkit
    }

    Context 'Functionality' {
        It 'Should stop all dependent services' {
            Stop-ADTServiceAndDependencies -InputObject $serviceWithMultipleRunningDependentServices
            Should -Invoke -CommandName Stop-Service -ModuleName PSAppDeployToolkit -Times ($dependentServices.Count + 1) -Exactly
        }
        It 'Should not stop depedent services when -SkipDependentServices is provided' {
            Stop-ADTServiceAndDependencies -InputObject $serviceWithMultipleRunningDependentServices -SkipDependentServices
            Should -Invoke -CommandName Stop-Service -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }
        It 'Should accept ServiceController objects through the pipeline' {
            { $serviceWithMultipleRunningDependentServices | Stop-ADTServiceAndDependencies } | Should -Not -Throw
            #$serviceWithMultipleRunningDependentServices | Stop-ADTServiceAndDependencies
            Should -Invoke -CommandName Stop-Service -ModuleName PSAppDeployToolkit -Times ($dependentServices.Count + 1) -Exactly
        }
        It 'Should return the specified service when -PassThru is provided' {
            $return = Stop-ADTServiceAndDependencies -InputObject $serviceWithMultipleRunningDependentServices -PassThru
            $return | Should -HaveCount 1
            $return | Should -BeOfType ([System.ServiceProcess.ServiceController])
            $return.ServiceName | Should -BeExactly $serviceWithMultipleRunningDependentServices.ServiceName
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
            { Stop-ADTServiceAndDependencies -InputObject " `f`n`r`t`v" } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Stop-ADTServiceAndDependencies'
        }
    }
}
