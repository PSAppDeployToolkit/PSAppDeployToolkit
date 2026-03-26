BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    $script:TestExtensionID = 'abcdefghijklmnopabcdefghijklmnop'
}

Describe 'Remove-ADTEdgeExtension' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Extension not configured — early return' {
        BeforeAll {
            # Return an object with a DIFFERENT extension ID so that the source's
            # -notcontains check is true and the function returns early without writing
            # to the registry.  We cannot return $null or [PSCustomObject]@{} because
            # the source accesses .PSObject.Properties on the result, which throws in
            # Set-StrictMode -Version Latest for both null and empty-property objects.
            Mock -ModuleName PSAppDeployToolkit Get-ADTEdgeExtensions {
                [PSCustomObject]@{ aaaabbbbccccddddaaaabbbbccccdddd = @{ installation_mode = 'force_installed' } }
            }
            # Must be registered so Should -Invoke can check it was never called.
            Mock -ModuleName PSAppDeployToolkit Set-ADTRegistryKey { }
        }

        It 'Does not throw when the extension is not configured' {
            { Remove-ADTEdgeExtension -ExtensionID $script:TestExtensionID } | Should -Not -Throw
        }

        It 'Does not call Set-ADTRegistryKey when extension is not configured' {
            Remove-ADTEdgeExtension -ExtensionID $script:TestExtensionID
            Should -Invoke Set-ADTRegistryKey -ModuleName PSAppDeployToolkit -Times 0 -Scope It
        }
    }

    Context 'Extension configured — WhatIf prevents registry write' {
        BeforeAll {
            # Return a PSCustomObject that has the test extension as a property.
            Mock -ModuleName PSAppDeployToolkit Get-ADTEdgeExtensions {
                [PSCustomObject]@{
                    $script:TestExtensionID = @{ installation_mode = 'force_installed'; update_url = 'https://edge.microsoft.com/extensionwebstorebase/v1/crx' }
                }
            }
            Mock -ModuleName PSAppDeployToolkit Set-ADTRegistryKey { }
        }

        It 'Does not throw when extension is found and -WhatIf is specified' {
            { Remove-ADTEdgeExtension -ExtensionID $script:TestExtensionID -WhatIf } | Should -Not -Throw
        }

        It 'Does not call Set-ADTRegistryKey when -WhatIf is specified' {
            Remove-ADTEdgeExtension -ExtensionID $script:TestExtensionID -WhatIf
            Should -Invoke Set-ADTRegistryKey -ModuleName PSAppDeployToolkit -Times 0 -Scope It
        }
    }

    Context 'Input validation' {
        It 'Throws when -ExtensionID is an empty string' {
            { Remove-ADTEdgeExtension -ExtensionID '' } | Should -Throw
        }
    }
}
