BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTServiceStartMode' {
    BeforeAll {
        $services = Get-Service

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'bootService', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $bootService = $services | & { process { if ($_.StartType -eq [System.ServiceProcess.ServiceStartMode]::Boot) { return $_ } } } | Select-Object -First 1
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'systemService', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $systemService = $services | & { process { if ($_.StartType -eq [System.ServiceProcess.ServiceStartMode]::System) { return $_ } } } | Select-Object -First 1
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'automaticService', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $automaticService = $null
        $delayedAutomaticService = $null
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'manualService', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $manualService = $services | & { process { if ($_.StartType -eq [System.ServiceProcess.ServiceStartMode]::Manual) { return $_ } } } | Select-Object -First 1
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'disabledService', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $disabledService = $services | & { process { if ($_.StartType -eq [System.ServiceProcess.ServiceStartMode]::Disabled) { return $_ } } } | Select-Object -First 1

        foreach ($service in $services)
        {
            if ($service.StartType -ne [System.ServiceProcess.ServiceStartMode]::Automatic)
            {
                continue
            }

            if ((Get-ItemProperty -LiteralPath "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\$($Service.Name)" -ErrorAction Ignore | Select-Object -ExpandProperty DelayedAutoStart -ErrorAction Ignore) -eq 1)
            {
                $delayedAutomaticService = $service
            }
            else
            {
                $automaticService = $service
                if ($delayedAutomaticService)
                {
                    break
                }
            }
        }

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should return the service start mode' {
            if ($bootService)
            {
                Get-ADTServiceStartMode -InputObject $bootService | Should -Be ([System.ServiceProcess.ServiceStartMode]::Boot)
            }
            if ($systemService)
            {
                Get-ADTServiceStartMode -InputObject $systemService | Should -Be ([System.ServiceProcess.ServiceStartMode]::System)
            }
            if ($automaticService)
            {
                Get-ADTServiceStartMode -InputObject $automaticService | Should -Be ([System.ServiceProcess.ServiceStartMode]::Automatic)
            }
            if ($delayedAutomaticService)
            {
                Get-ADTServiceStartMode -InputObject $delayedAutomaticService | Should -Be 'Automatic (Delayed Start)'
            }
            if ($manualService)
            {
                Get-ADTServiceStartMode -InputObject $manualService | Should -Be ([System.ServiceProcess.ServiceStartMode]::Manual)
            }
            if ($disabledService)
            {
                Get-ADTServiceStartMode -InputObject $disabledService | Should -Be ([System.ServiceProcess.ServiceStartMode]::Disabled)
            }
        }
        It 'Should accept ServiceController objects through the pipeline' {
            if ($bootService)
            {
                $bootService | Get-ADTServiceStartMode | Should -Be ([System.ServiceProcess.ServiceStartMode]::Boot)
            }
            if ($systemService)
            {
                $systemService | Get-ADTServiceStartMode | Should -Be ([System.ServiceProcess.ServiceStartMode]::System)
            }
            if ($automaticService)
            {
                $automaticService | Get-ADTServiceStartMode | Should -Be ([System.ServiceProcess.ServiceStartMode]::Automatic)
            }
            if ($delayedAutomaticService)
            {
                $delayedAutomaticService | Get-ADTServiceStartMode | Should -Be 'Automatic (Delayed Start)'
            }
            if ($manualService)
            {
                $manualService | Get-ADTServiceStartMode | Should -Be ([System.ServiceProcess.ServiceStartMode]::Manual)
            }
            if ($disabledService)
            {
                $disabledService | Get-ADTServiceStartMode | Should -Be ([System.ServiceProcess.ServiceStartMode]::Disabled)
            }
        }
        It 'Should return the start mode of one service at a time' {
            Get-ADTServiceStartMode -Name * | Should -HaveCount 1
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
            { Get-ADTServiceStartMode -InputObject " `f`n`r`t`v" } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Get-ADTServiceStartMode'
        }
    }
}
