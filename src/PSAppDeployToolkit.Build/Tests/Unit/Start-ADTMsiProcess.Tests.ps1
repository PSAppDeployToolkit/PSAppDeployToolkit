BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
    Import-Module "$PSScriptRoot\..\Support\TestFixtures.psm1" -Force

    # Author a real, valid MSI on disk so argument construction and log-path derivation run against
    # a genuine Property table. The MSI is authored, never installed - the launch seam is mocked.
    $script:MsiProductCode = '{B2C3D4E5-6F70-4811-9233-445566778899}'
    $script:MsiPath = New-ADTTestMsiDatabase -Path (Join-Path $TestDrive 'fixture.msi') -ProductName 'Fixture App' -ProductCode $script:MsiProductCode -Properties @{ ProductVersion = '1.2.3' }
    $script:MsiReadPath = Join-Path $TestDrive 'fixture_read.msi'
    Copy-Item -LiteralPath $script:MsiPath -Destination $script:MsiReadPath

    # Build a genuine InstalledApplication helper. The source assigns Get-ADTApplication's result back to
    # its typed [PSADT.AppManagement.InstalledApplication] variable, which forces a real cast, so a
    # PSCustomObject won't bind - the actual type is required.
    function global:New-FixtureInstalledApp ([System.String]$ProductCode)
    {
        return [PSADT.AppManagement.InstalledApplication]::new(
            "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$ProductCode",
            'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
            $ProductCode,
            [System.Guid]$ProductCode,
            'Fixture App', '1.2.3',
            "msiexec /x$ProductCode", "msiexec /x$ProductCode /qn",
            $null, $null, $null, 'PSADT', $null, $null, $false, $true, $null
        )
    }
}

AfterAll {
    Remove-Item Function:\New-FixtureInstalledApp -ErrorAction SilentlyContinue
}

Describe 'Start-ADTMsiProcess' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input Validation' {
        It 'Has a mandatory FilePath parameter in the FilePath parameter set' {
            (Get-Command Start-ADTMsiProcess).Parameters['FilePath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] -and $_.ParameterSetName -eq 'FilePath' }).Mandatory | Should -Contain $true
        }

        It 'Throws a parameter binding error when FilePath has an unsupported extension' {
            { Start-ADTMsiProcess -Action Install -FilePath 'installer.exe' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTMsiProcess'
        }

        It 'Throws ProductCodeInstallActionNotSupported when a ProductCode is used with the Install action' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.InvalidOperationException]
                ErrorId       = 'ProductCodeInstallActionNotSupported,Start-ADTMsiProcess'
            }
            { Start-ADTMsiProcess -Action Install -ProductCode '{11111111-1111-1111-1111-111111111111}' } | Should @shouldParams
        }

        It 'Throws a parameter binding error when ProductCode is not a valid GUID' {
            { Start-ADTMsiProcess -Action Uninstall -ProductCode 'not-a-guid' } | Should -Throw -ErrorId 'ParameterArgumentTransformationError,Start-ADTMsiProcess'
        }
    }

    Context 'Argument construction (Install)' {
        BeforeEach {
            Mock -ModuleName PSAppDeployToolkit Get-ADTApplication { }
            Mock -ModuleName PSAppDeployToolkit Update-ADTDesktop { }
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { return [PSADT.ProcessManagement.ProcessResult]::new(0) }
        }

        It 'Invokes msiexec.exe via Start-ADTProcess' {
            Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $FilePath -match 'msiexec\.exe$'
            }
        }

        It 'Passes the /i install option and the MSI path as msiexec arguments' {
            Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ArgumentList -contains '/i') -and ($ArgumentList -contains $script:MsiPath)
            }
        }

        It 'Includes a logging option and a log file path argument' {
            Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ArgumentList -join ' ') -match '\.log'
            }
        }

        It 'Replaces the default arguments when ArgumentList is supplied' {
            Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath -ArgumentList '/quiet'
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $ArgumentList -contains '/quiet'
            }
        }

        It 'Appends additional arguments when AdditionalArgumentList is supplied' {
            Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath -AdditionalArgumentList 'ALLUSERS=1'
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $ArgumentList -contains 'ALLUSERS=1'
            }
        }
    }

    Context 'Argument construction (Uninstall by ProductCode)' {
        BeforeEach {
            # Report the product as installed so the Uninstall action proceeds.
            Mock -ModuleName PSAppDeployToolkit Get-ADTApplication {
                return New-FixtureInstalledApp '{11111111-1111-1111-1111-111111111111}'
            }
            Mock -ModuleName PSAppDeployToolkit Update-ADTDesktop { }
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { return [PSADT.ProcessManagement.ProcessResult]::new(0) }
        }

        It 'Passes the /x uninstall option and the product code as msiexec arguments' {
            Start-ADTMsiProcess -Action Uninstall -ProductCode '{11111111-1111-1111-1111-111111111111}'
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ArgumentList -contains '/x') -and (($ArgumentList -join ' ') -match '11111111-1111-1111-1111-111111111111')
            }
        }
    }

    Context 'Already-installed handling' {
        It 'Skips the Install action and returns exit code 1638 when the product is already installed' {
            Mock -ModuleName PSAppDeployToolkit Update-ADTDesktop { }
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { return [PSADT.ProcessManagement.ProcessResult]::new(0) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTApplication {
                return New-FixtureInstalledApp '{B2C3D4E5-6F70-4811-9233-445566778899}'
            }
            $result = Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath -PassThru
            $result.ExitCode | Should -Be 1638
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'Desktop refresh' {
        BeforeEach {
            Mock -ModuleName PSAppDeployToolkit Get-ADTApplication { }
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { return [PSADT.ProcessManagement.ProcessResult]::new(0) }
        }

        It 'Refreshes the desktop after a successful install by default' {
            Mock -ModuleName PSAppDeployToolkit Update-ADTDesktop { }
            Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath
            Should -Invoke -CommandName Update-ADTDesktop -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Does not refresh the desktop when NoDesktopRefresh is specified' {
            Mock -ModuleName PSAppDeployToolkit Update-ADTDesktop { }
            Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath -NoDesktopRefresh
            Should -Invoke -CommandName Update-ADTDesktop -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'WhatIf handling' {
        It 'Does not invoke Start-ADTProcess when WhatIf is specified' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTApplication { }
            Mock -ModuleName PSAppDeployToolkit Update-ADTDesktop { }
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { return [PSADT.ProcessManagement.ProcessResult]::new(0) }
            Start-ADTMsiProcess -Action Install -FilePath $script:MsiPath -WhatIf
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }
}
