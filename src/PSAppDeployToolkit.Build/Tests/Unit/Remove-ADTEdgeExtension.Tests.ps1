BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTEdgeExtension' {
    BeforeAll {
        # Mock Convert-ADTRegistryPath to redirect registry paths to TestRegistry:\
        Mock -ModuleName PSAppDeployToolkit Convert-ADTRegistryPath {
            $output = & (Get-Command -Source PSAppDeployToolkit -CommandType Function -Name 'Convert-ADTRegistryPath') @PesterBoundParameters
            $testRegistryRoot = (Get-PSDrive -Name TestRegistry).Root
            $mockedOutput = $output -replace '^Microsoft\.PowerShell\.Core\\Registry::', "Microsoft.PowerShell.Core\Registry::$testRegistryRoot\"
            return $mockedOutput
        }

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'RedirectedEdgeKey', Justification = "This variable is used within script blocks that PSScriptAnalyzer has no visibility of.")]
        $RedirectedEdgeKey = 'TestRegistry:\HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Edge'
    }

    Context 'Functionality' {
        It 'Should remove the target extension and preserve other extensions' {
            New-Item -Path $RedirectedEdgeKey -Force | Out-Null
            New-ItemProperty -Path $RedirectedEdgeKey -Name 'ExtensionSettings' -Value '{"abc123":{"installation_mode":"force_installed","update_url":"https://edge.microsoft.com/blah"},"xyz789":{"installation_mode":"blocked","update_url":"https://edge.microsoft.com/old"}}' -Force | Out-Null

            Remove-ADTEdgeExtension -ExtensionID 'abc123'

            $Extensions = Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | ConvertFrom-Json
            $Extensions.PSObject.Properties.Name | Should -Not -Contain 'abc123'
            $Extensions.xyz789.installation_mode | Should -Be 'blocked'
            $Extensions.xyz789.update_url | Should -Be 'https://edge.microsoft.com/old'
            ($Extensions.PSObject.Properties.Name | Measure-Object).Count | Should -Be 1
        }

        It 'Should empty the policy value when removing the only configured extension' {
            New-Item -Path $RedirectedEdgeKey -Force | Out-Null
            New-ItemProperty -Path $RedirectedEdgeKey -Name 'ExtensionSettings' -Value '{"abc123":{"installation_mode":"force_installed","update_url":"https://edge.microsoft.com/blah"}}' -Force | Out-Null

            Remove-ADTEdgeExtension -ExtensionID 'abc123'

            $Extensions = Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | ConvertFrom-Json
            ($Extensions.PSObject.Properties.Name | Measure-Object).Count | Should -Be 0
        }

        It 'Should not modify the policy value when the target extension is not configured' {
            New-Item -Path $RedirectedEdgeKey -Force | Out-Null
            New-ItemProperty -Path $RedirectedEdgeKey -Name 'ExtensionSettings' -Value '{"xyz789":{"installation_mode":"blocked","update_url":"https://edge.microsoft.com/old"}}' -Force | Out-Null

            Remove-ADTEdgeExtension -ExtensionID 'abc123'

            $Extensions = Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | ConvertFrom-Json
            $Extensions.xyz789.installation_mode | Should -Be 'blocked'
            ($Extensions.PSObject.Properties.Name | Measure-Object).Count | Should -Be 1
        }

        It 'Should not write the policy value when -WhatIf is specified' {
            New-Item -Path $RedirectedEdgeKey -Force | Out-Null
            New-ItemProperty -Path $RedirectedEdgeKey -Name 'ExtensionSettings' -Value '{"abc123":{"installation_mode":"force_installed","update_url":"https://edge.microsoft.com/blah"}}' -Force | Out-Null

            Remove-ADTEdgeExtension -ExtensionID 'abc123' -WhatIf

            $Extensions = Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | ConvertFrom-Json
            $Extensions.PSObject.Properties.Name | Should -Contain 'abc123'
            ($Extensions.PSObject.Properties.Name | Measure-Object).Count | Should -Be 1
        }
    }

    Context 'Input Validation' {
        It 'Should verify that ExtensionID is mandatory' {
            (Get-Command Remove-ADTEdgeExtension).Parameters['ExtensionID'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should verify that ExtensionID is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTEdgeExtension'
            }
            { Remove-ADTEdgeExtension -ExtensionID $null } | Should @shouldParams
            { Remove-ADTEdgeExtension -ExtensionID '' } | Should @shouldParams
            { Remove-ADTEdgeExtension -ExtensionID " `f`n`r`t`v" } | Should @shouldParams
        }
    }
}
