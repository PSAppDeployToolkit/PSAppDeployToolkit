BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Uninstall-ADTApplication' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Mock uninstall executors so no real processes are launched.
        Mock -ModuleName PSAppDeployToolkit Start-ADTMsiProcess { }
        Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { }

        # Create a fake MSI InstalledApplication object.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'FakeMsiApp', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $FakeMsiApp = [PSADT.AppManagement.InstalledApplication]::new(
            'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{12345678-1234-1234-1234-123456789012}',
            'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
            '{12345678-1234-1234-1234-123456789012}',
            [System.Nullable[System.Guid]]([System.Guid]::new('12345678-1234-1234-1234-123456789012')),
            'Fake MSI Application',
            '1.0.0',
            'MsiExec.exe /I{12345678-1234-1234-1234-123456789012}',
            [System.Management.Automation.Language.NullString]::Value,
            $null,
            $null,
            [System.Nullable[System.DateTime]]([System.DateTime]::Now),
            'Fake Publisher',
            $null,
            $null,
            $false,
            $true,
            [System.Nullable[System.Boolean]]($false)
        )

        # Create a fake EXE InstalledApplication object using a real local executable path.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'FakeExeApp', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $FakeExeApp = [PSADT.AppManagement.InstalledApplication]::new(
            'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FakeExeApp',
            'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
            'FakeExeApp',
            $null,
            'Fake EXE Application',
            '2.0.0',
            "$env:SystemRoot\System32\cmd.exe /S",
            [System.Management.Automation.Language.NullString]::Value,
            $null,
            $null,
            [System.Nullable[System.DateTime]]([System.DateTime]::Now),
            'Fake EXE Publisher',
            $null,
            $null,
            $false,
            $false,
            [System.Nullable[System.Boolean]]($false)
        )
    }

    Context 'MSI application uninstall' {
        It 'Invokes Start-ADTMsiProcess when given a MSI InstalledApplication via pipeline' {
            $FakeMsiApp | Uninstall-ADTApplication
            Should -Invoke -CommandName Start-ADTMsiProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Does not invoke Start-ADTProcess when given a MSI InstalledApplication' {
            $FakeMsiApp | Uninstall-ADTApplication
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }

        It 'Invokes Start-ADTMsiProcess with Action Uninstall' {
            $FakeMsiApp | Uninstall-ADTApplication
            Should -Invoke -CommandName Start-ADTMsiProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Action -eq 'Uninstall'
            }
        }

        It 'Produces no output by default for MSI uninstall' {
            $result = $FakeMsiApp | Uninstall-ADTApplication
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'EXE application uninstall' {
        It 'Invokes Start-ADTProcess when given an EXE InstalledApplication via pipeline' {
            $FakeExeApp | Uninstall-ADTApplication
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Does not invoke Start-ADTMsiProcess when given an EXE InstalledApplication' {
            $FakeExeApp | Uninstall-ADTApplication
            Should -Invoke -CommandName Start-ADTMsiProcess -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }

        It 'Invokes Start-ADTProcess with the UninstallStringFilePath as the FilePath' {
            $FakeExeApp | Uninstall-ADTApplication
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $FilePath -eq $FakeExeApp.UninstallStringFilePath.FullName
            }
        }
    }

    Context 'No-op when no application found' {
        It 'Does not invoke Start-ADTMsiProcess or Start-ADTProcess when InstalledApplication list is empty' {
            # Pass an empty array as InstalledApplication param via the Search parameter set (no apps match).
            Mock -ModuleName PSAppDeployToolkit Get-ADTApplication { return $null }
            Uninstall-ADTApplication -Name 'zzzzThisApplicationCannotExist99999'
            Should -Invoke -CommandName Start-ADTMsiProcess -ModuleName PSAppDeployToolkit -Times 0 -Exactly
            Should -Invoke -CommandName Start-ADTProcess -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory InstalledApplication parameter in InstalledApplication parameter set' {
            (Get-Command Uninstall-ADTApplication).Parameters['InstalledApplication'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] -and $_.ParameterSetName -eq 'InstalledApplication' }).Mandatory | Should -Contain $true
        }

        It 'Throws when Search parameter set is used with no Name, ProductCode, or FilterScript' {
            { Uninstall-ADTApplication } | Should -Throw -ExceptionType ([System.InvalidOperationException])
        }

        It 'NameMatch only accepts Contains, Exact, Wildcard, or Regex' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Uninstall-ADTApplication'
            }
            { Uninstall-ADTApplication -Name 'App' -NameMatch 'InvalidMatch' } | Should @shouldParams
        }

        It 'ApplicationType only accepts All, MSI, or EXE' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Uninstall-ADTApplication'
            }
            { Uninstall-ADTApplication -Name 'App' -ApplicationType 'InvalidType' } | Should @shouldParams
        }
    }
}
