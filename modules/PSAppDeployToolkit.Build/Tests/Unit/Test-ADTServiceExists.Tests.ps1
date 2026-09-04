BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}
Describe 'Test-ADTServiceExists' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'realServiceName', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $realServiceName = Get-Service | Select-Object -First 1 -ExpandProperty Name

        # Get-Service reports a missing service non-terminatingly, so -ErrorAction Stop is what makes the
        # catch reachable at all. The attempt bound keeps a behaviour change from hanging the run again.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'fakeServiceName', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $fakeServiceName = $null
        $attempts = 0

        while (!$fakeServiceName -and (++$attempts -le 10))
        {
            $candidate = [System.Guid]::NewGuid().ToString()

            try
            {
                Get-Service -Name $candidate -ErrorAction Stop
            }
            catch [Microsoft.PowerShell.Commands.ServiceCommandException]
            {
                if ($_.CategoryInfo.Category -ne [System.Management.Automation.ErrorCategory]::ObjectNotFound)
                {
                    throw
                }

                $fakeServiceName = $candidate
            }
        }

        if (!$fakeServiceName)
        {
            throw "Could not find an unused service name after $attempts attempts."
        }

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should return $true' {
            Test-ADTServiceExists -Name $realServiceName | Should -BeTrue
            Test-ADTServiceExists -Name $realServiceName -UseCIM | Should -BeTrue
        }
        It 'Should return $false' {
            Test-ADTServiceExists -Name $fakeServiceName | Should -BeFalse
            Test-ADTServiceExists -Name $fakeServiceName -UseCIM | Should -BeFalse
            Test-ADTServiceExists -Name $fakeServiceName -PassThru | Should -BeFalse
            Test-ADTServiceExists -Name $fakeServiceName -UseCIM -PassThru | Should -BeFalse
        }
        It 'Should pass through the service object' {
            Test-ADTServiceExists -Name $realServiceName -PassThru | Should -BeOfType ([System.ServiceProcess.ServiceController])

            $service = Test-ADTServiceExists -Name $realServiceName -UseCIM -PassThru
            $service | Should -BeOfType ([Microsoft.Management.Infrastructure.CimInstance])
            $service.PSObject.TypeNames | Should -Contain 'Microsoft.Management.Infrastructure.CimInstance#ROOT/cimv2/Win32_BaseService'
        }
    }

    Context 'Wildcard patterns' {
        BeforeAll {
            # Escaped and then suffixed, so the pattern is certain to match this one service and is not
            # thrown off by a bracket appearing in a name or a display name.
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'namePattern', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $namePattern = "$([System.Management.Automation.WildcardPattern]::Escape($realServiceName))*"

            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'displayNamePattern', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $displayNamePattern = "$([System.Management.Automation.WildcardPattern]::Escape((Get-Service -Name $realServiceName).DisplayName))*"
        }

        It 'Refuses a pattern in CIM mode' {
            # CIM matches on equality, so a pattern there would quietly report the service as missing rather
            # than finding it, which is worse than being told the pattern cannot be used.
            { Test-ADTServiceExists -Name $namePattern -UseCIM } | Should -Throw -ErrorId 'UseCimModeNoWildcardSupport,Test-ADTServiceExists'
        }

        It 'Matches a service by a display name pattern' {
            Test-ADTServiceExists -DisplayName $displayNamePattern | Should -BeTrue
        }

        It 'Matches a service by a name pattern' {
            Test-ADTServiceExists -Name $namePattern | Should -BeTrue
        }

        It 'Reports no match for a name pattern nothing answers' {
            Test-ADTServiceExists -Name "$fakeServiceName*" | Should -BeFalse
        }
    }
    Context 'Input Validation' {
        It 'Should verify that -Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Test-ADTServiceExists'
            }
            { Test-ADTServiceExists -Name $null } | Should @shouldParams
            { Test-ADTServiceExists -Name '' } | Should @shouldParams
            { Test-ADTServiceExists -Name " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that -DisplayName is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Test-ADTServiceExists'
            }
            { Test-ADTServiceExists -DisplayName $null } | Should @shouldParams
            { Test-ADTServiceExists -DisplayName '' } | Should @shouldParams
            { Test-ADTServiceExists -DisplayName " `f`n`r`t`v" } | Should @shouldParams
        }
    }
}
