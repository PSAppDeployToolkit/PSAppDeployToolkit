BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # One service per start mode, found from the registry rather than from ServiceController.StartType,
    # which is the very property the function returns. Start maps 0 to Boot, 1 to System, 2 to Automatic,
    # 3 to Manual and 4 to Disabled, with DelayedAutoStart telling the two automatic modes apart.
    $script:StartModeCases = @(
        @{ Mode = 'Boot'; Start = 0; Delayed = $false }
        @{ Mode = 'System'; Start = 1; Delayed = $false }
        @{ Mode = 'Automatic'; Start = 2; Delayed = $false }
        @{ Mode = 'Automatic (Delayed Start)'; Start = 2; Delayed = $true }
        @{ Mode = 'Manual'; Start = 3; Delayed = $false }
        @{ Mode = 'Disabled'; Start = 4; Delayed = $false }
    ) | & {
        begin
        {
            $keys = Get-ChildItem -LiteralPath 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services'
            $known = Get-Service | & { process { $_.ServiceName } }
        }
        process
        {
            $case = $_
            $match = $keys | & {
                process
                {
                    if (($known -notcontains $_.PSChildName) -or ($_.GetValue('Start') -ne $case.Start) -or (!!$_.GetValue('DelayedAutoStart') -ne $case.Delayed))
                    {
                        return
                    }
                    return $_.PSChildName
                }
            } | Select-Object -First 1
            return $case + @{ ServiceName = $match }
        }
    }

}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    # Any service will do for the tests about how one is named rather than about which mode comes back.
    # Established here rather than at discovery, since that scope is not visible from inside a test.
    $script:AnyService = Get-Service | Select-Object -First 1
}

Describe 'Get-ADTServiceStartMode' {
    Context 'Reporting the start mode' {
        It 'Reports <Mode>' -ForEach $script:StartModeCases {
            # Skipped by name rather than passing silently, so that a mode this machine has no service
            # for is visible in the results instead of looking like it was checked.
            if (!$ServiceName)
            {
                Set-ItResult -Skipped -Because "this machine has no service with a start mode of [$Mode]"
            }
            Get-ADTServiceStartMode -Name $ServiceName | Should -Be $Mode
        }
    }

    Context 'Naming the service' {
        It 'Accepts a service name' {
            Get-ADTServiceStartMode -Name $script:AnyService.ServiceName | Should -Not -BeNullOrEmpty
        }

        It 'Accepts a display name' {
            # The display name is handed to Get-Service, which treats it as a pattern, so it is escaped
            # rather than trusted to contain nothing that looks like one.
            Get-ADTServiceStartMode -DisplayName ([System.Management.Automation.WildcardPattern]::Escape($script:AnyService.DisplayName)) | Should -Be (Get-ADTServiceStartMode -Name $script:AnyService.ServiceName)
        }

        It 'Accepts a service object' {
            Get-ADTServiceStartMode -InputObject $script:AnyService | Should -Be (Get-ADTServiceStartMode -Name $script:AnyService.ServiceName)
        }

        It 'Accepts a service object from the pipeline' {
            $script:AnyService | Get-ADTServiceStartMode | Should -Be (Get-ADTServiceStartMode -Name $script:AnyService.ServiceName)
        }

        It 'Reports one mode per service when given several' {
            # Deployments pipe a filtered list of services in, so the results have to line up one for one.
            $services = Get-Service | Select-Object -First 3
            @($services | Get-ADTServiceStartMode).Count | Should -Be 3
        }

        It "Should thow when the name provided doesn't exist" {
            $shouldParams = @{
                Throw = $true
                Exception = [Microsoft.PowerShell.Commands.ServiceCommandException]
            }
            { Get-ADTServiceStartMode -Name * } | Should @shouldParams -ErrorId 'NoServiceFoundForGivenName,Get-ADTServiceStartMode'
            { Get-ADTServiceStartMode -DisplayName * } | Should @shouldParams -ErrorId 'NoServiceFoundForGivenDisplayName,Get-ADTServiceStartMode'
        }
    }

    Context 'Input Validation' {
        It 'Should verify that -Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTServiceStartMode'
            }
            { Get-ADTServiceStartMode -Name $null } | Should @shouldParams
            { Get-ADTServiceStartMode -Name '' } | Should @shouldParams
            { Get-ADTServiceStartMode -Name " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that -DisplayName is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTServiceStartMode'
            }
            { Get-ADTServiceStartMode -DisplayName $null } | Should @shouldParams
            { Get-ADTServiceStartMode -DisplayName '' } | Should @shouldParams
            { Get-ADTServiceStartMode -DisplayName " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that -InputObject is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Get-ADTServiceStartMode -InputObject $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Get-ADTServiceStartMode'
            { Get-ADTServiceStartMode -InputObject '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Get-ADTServiceStartMode'
            # White space converts to a ServiceController with no ServiceName, so the parameter's own
            # ValidateScript reports it. It surfaced as a plain binding failure only while
            # New-ADTValidateScriptErrorRecord threw part way through building that error.
            { Get-ADTServiceStartMode -InputObject " `f`n`r`t`v" } | Should -Throw -ExceptionType ([System.ArgumentException]) -ErrorId 'InvalidInputObjectParameterValue,Get-ADTServiceStartMode'
        }
    }
}
