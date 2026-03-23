BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTServiceStartMode' {
    BeforeAll {
        $services = Get-Service

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'bootService', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
        $bootService = $services | & { process { if ($_.StartType -eq [System.ServiceProcess.ServiceStartMode]::Boot) { return $_ } } } | Select-Object -First 1
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'systemService', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
        $systemService = $services | & { process { if ($_.StartType -eq [System.ServiceProcess.ServiceStartMode]::System) { return $_ } } } | Select-Object -First 1
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'automaticService', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
        $automaticService = $null
        $delayedAutomaticService = $null
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'manualService', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
        $manualService = $services | & { process { if ($_.StartType -eq [System.ServiceProcess.ServiceStartMode]::Manual) { return $_ } } } | Select-Object -First 1
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'disabledService', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
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
                Get-ADTServiceStartMode -Service $bootService | Should -Be ([System.ServiceProcess.ServiceStartMode]::Boot)
            }
            if ($systemService)
            {
                Get-ADTServiceStartMode -Service $systemService | Should -Be ([System.ServiceProcess.ServiceStartMode]::System)
            }
            if ($automaticService)
            {
				Get-ADTServiceStartMode -Service $automaticService | Should -Be ([System.ServiceProcess.ServiceStartMode]::Automatic)
            }
            if ($delayedAutomaticService)
            {
				Get-ADTServiceStartMode -Service $delayedAutomaticService | Should -Be 'Automatic (Delayed Start)'
            }
            if ($manualService)
            {
                Get-ADTServiceStartMode -Service $manualService | Should -Be ([System.ServiceProcess.ServiceStartMode]::Manual)
            }
            if ($disabledService)
            {
                Get-ADTServiceStartMode -Service $disabledService | Should -Be ([System.ServiceProcess.ServiceStartMode]::Disabled)
            }
        }
    }

    Context 'Input Validation' {
        It 'Should verify that Service is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Get-ADTServiceStartMode -Service $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Get-ADTServiceStartMode'
            { Get-ADTServiceStartMode -Service '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Get-ADTServiceStartMode'
            { Get-ADTServiceStartMode -Service ' ' } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Get-ADTServiceStartMode'
        }
    }
}
