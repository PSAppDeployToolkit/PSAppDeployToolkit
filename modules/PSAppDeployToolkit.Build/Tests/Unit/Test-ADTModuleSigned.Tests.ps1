BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    # The signature is taken from whichever file carries the module's opening code: ImportsFirst.ps1 in a
    # development tree, the psm1 itself once built. Read freshly here so the assertion is an independent
    # answer rather than the same value the module already recorded.
    $script:ModuleBase = (Get-Module -Name PSAppDeployToolkit).ModuleBase
    $script:EntryFile = if (Test-Path -LiteralPath "$script:ModuleBase\ImportsFirst.ps1" -PathType Leaf)
    {
        "$script:ModuleBase\ImportsFirst.ps1"
    }
    else
    {
        "$script:ModuleBase\PSAppDeployToolkit.psm1"
    }
    $script:EntryFileIsSigned = (Get-AuthenticodeSignature -LiteralPath $script:EntryFile).Status -eq [System.Management.Automation.SignatureStatus]::Valid
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Test-ADTModuleSigned' {
    Context 'Functionality' {
        It 'Agrees with the signature on the file the module was loaded from' {
            # Signing gates real behaviour - a signed module in a secure path is what lets the client run
            # with UIAccess - so reporting this wrongly would silently change which client is launched.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Expected = $script:EntryFileIsSigned } {
                Test-ADTModuleSigned | Should -Be $Expected
            }
        }

        It 'Treats anything short of a valid signature as unsigned' {
            # Present but untrusted is not the same as signed, and the distinction matters because a
            # tampered or expired signature must not buy the trust a valid one does.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Status = (Get-AuthenticodeSignature -LiteralPath $script:EntryFile).Status } {
                Test-ADTModuleSigned | Should -Be ($Status -eq [System.Management.Automation.SignatureStatus]::Valid)
            }
        }

        It 'Answers without the module having been initialized' {
            # Settled at import, like the compiled flag, because the client/server launcher has to choose
            # between the signed and compatible executables before any deployment starts.
            Test-ADTModuleInitialized | Should -BeFalse
            InModuleScope -ModuleName PSAppDeployToolkit {
                Test-ADTModuleSigned | Should -BeOfType ([System.Boolean])
            }
        }
    }
}
