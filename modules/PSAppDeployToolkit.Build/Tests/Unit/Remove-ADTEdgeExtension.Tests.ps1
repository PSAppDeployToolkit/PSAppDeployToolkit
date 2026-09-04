BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Remove-ADTEdgeExtension' {
    BeforeAll {
        # Every registry path the module builds is rewritten to sit under TestRegistry, so the machine's
        # own Edge policy is neither read nor written. Get-ADTEdgeExtensions seeds a value when it finds
        # none, which without this would be a change to machine policy just to run the tests.
        Mock -ModuleName PSAppDeployToolkit Convert-ADTRegistryPath {
            $output = & (Get-Command -Source PSAppDeployToolkit -CommandType Function -Name 'Convert-ADTRegistryPath') @PesterBoundParameters
            return $output -replace '^Microsoft\.PowerShell\.Core\\Registry::', "Microsoft.PowerShell.Core\Registry::$((Get-PSDrive -Name TestRegistry).Root)\"
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'RedirectedEdgeKey', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $RedirectedEdgeKey = 'TestRegistry:\HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Edge'
    }

    BeforeEach {
        Remove-Item -LiteralPath $RedirectedEdgeKey -Recurse -Force -ErrorAction Ignore
    }

    Context 'Removing an extension' {
        BeforeEach {
            $null = New-Item -Path $RedirectedEdgeKey -Force
            $null = New-ItemProperty -Path $RedirectedEdgeKey -Name 'ExtensionSettings' -Value '{"keepme":{"installation_mode":"force_installed"},"removeme":{"installation_mode":"blocked"}}' -Force
        }

        It 'Removes the extension it was given' {
            Remove-ADTEdgeExtension -ExtensionID 'removeme'
            (Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | ConvertFrom-Json).PSObject.Properties.Name | Should -Not -Contain 'removeme'
        }

        It 'Leaves the other extensions configured' {
            # The policy value holds every extension the machine is told about, so removing one must not
            # take the rest of them with it.
            Remove-ADTEdgeExtension -ExtensionID 'removeme'
            (Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | ConvertFrom-Json).keepme.installation_mode | Should -BeExactly 'force_installed'
        }

        It 'Leaves the policy alone when the extension was never configured' {
            Remove-ADTEdgeExtension -ExtensionID 'nevertherea'
            @((Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | ConvertFrom-Json).PSObject.Properties).Count | Should -Be 2
        }

        It 'Says the removal was not required' {
            Remove-ADTEdgeExtension -ExtensionID 'nevertherea'
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Removal not required*' }
        }

        It 'Removes nothing with -WhatIf' {
            Remove-ADTEdgeExtension -ExtensionID 'removeme' -WhatIf
            (Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | ConvertFrom-Json).PSObject.Properties.Name | Should -Contain 'removeme'
        }

        It 'Leaves an empty object behind when the last one goes' {
            Remove-ADTEdgeExtension -ExtensionID 'keepme'
            Remove-ADTEdgeExtension -ExtensionID 'removeme'
            Get-ItemPropertyValue -Path $RedirectedEdgeKey -Name 'ExtensionSettings' | Should -BeExactly '{}'
        }
    }

    Context 'With no extension policy configured' {
        It 'Does not object' {
            # Nothing configured means nothing to remove, which is a result rather than a failure.
            { Remove-ADTEdgeExtension -ExtensionID 'anything' } | Should -Not -Throw
        }

        It 'Keeps working once the policy value has been seeded' {
            # The first call through here seeds the policy value, so a second one reads back whatever the
            # first wrote. That round trip is where this used to come apart.
            Remove-ADTEdgeExtension -ExtensionID 'anything'
            { Remove-ADTEdgeExtension -ExtensionID 'anything' } | Should -Not -Throw
        }
    }

    Context 'Input Validation' {
        It 'Requires an extension to remove' {
            Test-ADTMandatoryParameter -Command (Get-Command Remove-ADTEdgeExtension) -Parameter ExtensionID | Should -BeTrue
        }

        It 'Refuses a blank extension' {
            { Remove-ADTEdgeExtension -ExtensionID '   ' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
