BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTServiceExists' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'realServiceName', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
        $realServiceName = Get-Service | Select-Object -First 1 -ExpandProperty Name

        while ($true)
        {
            $fakeServiceName = [System.Guid]::NewGuid().ToString()

            try
            {
                Get-Service -Name $fakeServiceName
            }
            catch [Microsoft.PowerShell.Commands.ServiceCommandException]
            {
                if ($_.CategoryInfo.Category -eq [System.Management.Automation.ErrorCategory]::ObjectNotFound)
                {
                    break
                }

                throw
            }
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

    Context 'Input Validation' {
        It 'Should verify that Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Test-ADTServiceExists -Name $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Test-ADTServiceExists'
            { Test-ADTServiceExists -Name '' } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Test-ADTServiceExists'
            { Test-ADTServiceExists -Name " `f`n`r`t`v" } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Test-ADTServiceExists'
        }
    }
}
