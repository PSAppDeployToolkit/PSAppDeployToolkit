BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Add-ADTEdgeExtension' {
    BeforeAll {
        # Mock Convert-ADTRegistryPath to redirect registry paths to TestRegistry:\
        Mock -ModuleName PSAppDeployToolkit Convert-ADTRegistryPath {
            $output = & (Get-Command -Source PSAppDeployToolkit -CommandType Function -Name 'Convert-ADTRegistryPath') @PesterBoundParameters
            $testRegistryRoot = (Get-PSDrive -Name TestRegistry).Root
            $mockedOutput = $output -replace '^Microsoft\.PowerShell\.Core\\Registry::', "Microsoft.PowerShell.Core\Registry::$testRegistryRoot\"
            return $mockedOutput
        }

        # Mock Set-ADTPreferenceVariables due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'RedirectedEdgeKey', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $RedirectedEdgeKey = 'TestRegistry:\HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Edge'
    }

    Context 'Functionality' {
        It 'Should add an extension to a non-existent registry key' {
            $extensionId = 'abc123'
            $updateUrl = 'https://edge.microsoft.com/blah'
            $installationMode = 'force_installed'
            $minimumVersionRequired = '1.0'
            Add-ADTEdgeExtension -ExtensionId $extensionId -UpdateUrl $updateUrl -InstallationMode $installationMode -MinimumVersionRequired $minimumVersionRequired

            $Extensions = Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | ConvertFrom-Json
            $Extensions.$extensionId.update_url | Should -Be $updateUrl
            $Extensions.$extensionId.installation_mode | Should -Be $installationMode
            $Extensions.$extensionId.minimum_version_required | Should -Be $minimumVersionRequired
			($Extensions.PSObject.Properties.Name | Measure-Object).Count | Should -Be 1
        }

        It 'Should update an existing extension registration, removing minimum version required' {

            New-Item -Path $RedirectedEdgeKey -Force | Out-Null
            New-ItemProperty -Path $RedirectedEdgeKey -Name 'ExtensionSettings' -Value '{"abc123":{"installation_mode":"blocked","update_url":"https://edge.microsoft.com/old","minimum_version_required":"1.0"}}' -Force | Out-Null

            $extensionId = 'abc123'
            $updateUrl = 'https://edge.microsoft.com/blah'
            $installationMode = 'force_installed'

            Add-ADTEdgeExtension -ExtensionId $extensionId -UpdateUrl $updateUrl -InstallationMode $installationMode

            $Extensions = Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | ConvertFrom-Json
            $Extensions.$extensionId.update_url | Should -Be $updateUrl
            $Extensions.$extensionId.installation_mode | Should -Be $installationMode
            $Extensions.$extensionId | Select-Object -ExpandProperty minimum_version_required -ErrorAction Ignore | Should -BeNullOrEmpty
			($Extensions.PSObject.Properties.Name | Measure-Object).Count | Should -Be 1
        }

        It 'Should preserve existing extensions' {

            New-Item -Path $RedirectedEdgeKey -Force | Out-Null
            New-ItemProperty -Path $RedirectedEdgeKey -Name 'ExtensionSettings' -Value '{"xyz789":{"installation_mode":"blocked","update_url":"https://edge.microsoft.com/old"}}' -Force | Out-Null

            $extensionId = 'abc123'
            $updateUrl = 'https://edge.microsoft.com/blah'
            $installationMode = 'force_installed'

            Add-ADTEdgeExtension -ExtensionId $extensionId -UpdateUrl $updateUrl -InstallationMode $installationMode

            $Extensions = Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | ConvertFrom-Json
            $Extensions.$extensionId.update_url | Should -Be $updateUrl
            $Extensions.$extensionId.installation_mode | Should -Be $installationMode

            $Extensions.xyz789.update_url | Should -Be 'https://edge.microsoft.com/old'
            $Extensions.xyz789.installation_mode | Should -Be 'blocked'

			($Extensions.PSObject.Properties.Name | Measure-Object).Count | Should -Be 2
        }
    }

    Context 'Input Validation' {
        It 'Should only accept InstallationMode as: blocked, allowed, removed, force_installed, normal_installed' {
            foreach ($mode in 'blocked', 'allowed', 'removed', 'force_installed', 'normal_installed')
            {
                { Add-ADTEdgeExtension -ExtensionId 'abc123' -UpdateUrl 'https://edge.microsoft.com/blah' -InstallationMode $mode } | Should -Not -Throw
            }

            { Add-ADTEdgeExtension -ExtensionId 'abc123' -UpdateUrl 'https://edge.microsoft.com/blah' -InstallationMode 'invalid' } | Should -Throw
        }

        It 'Should only accept valid URLs for UpdateUrl' {
            { Add-ADTEdgeExtension -ExtensionId 'abc123' -UpdateUrl 'https://edge.microsoft.com/blah' -InstallationMode 'force_installed' } | Should -Not -Throw
            { Add-ADTEdgeExtension -ExtensionId 'abc123' -UpdateUrl 'invalid' -InstallationMode 'force_installed' } | Should -Throw
        }
    }
}
