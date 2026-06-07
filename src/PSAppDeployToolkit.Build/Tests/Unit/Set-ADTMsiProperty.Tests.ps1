BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Set-ADTMsiProperty' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Helper: read a single property value back from an open MSI database handle.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'ReadMsiProperty', Justification = 'Used inside It blocks.')]
        $ReadMsiProperty = {
            param(
                [System.__ComObject]$Database,
                [System.__ComObject]$Installer,
                [System.String]$PropertyName
            )
            $sv = $Database.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Database, @('SELECT Value FROM Property WHERE Property = ?'))
            $sr = $Installer.GetType().InvokeMember('CreateRecord', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Installer, @(1))
            $null = $sr.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $sr, @(1, $PropertyName))
            $null = $sv.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $sv, @($sr))
            $f = $sv.GetType().InvokeMember('Fetch', [System.Reflection.BindingFlags]::InvokeMethod, $null, $sv, @())
            $null = $sv.GetType().InvokeMember('Close', [System.Reflection.BindingFlags]::InvokeMethod, $null, $sv, @())
            if ($null -ne $f)
            {
                $f.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::GetProperty, $null, $f, @(1))
            }
            else
            {
                $null
            }
        }
    }

    # Each It block gets a fresh in-memory MSI so operations are fully isolated.
    BeforeEach {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestInstaller', Justification = 'Used in It and AfterEach blocks.')]
        $TestInstaller = New-Object -ComObject WindowsInstaller.Installer

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestMsiPath', Justification = 'Used in AfterEach.')]
        $TestMsiPath = "$env:TEMP\SetMsiProp_$(New-Guid).msi"

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestDb', Justification = 'Used in It and AfterEach blocks.')]
        $TestDb = $TestInstaller.GetType().InvokeMember('OpenDatabase', [System.Reflection.BindingFlags]::InvokeMethod, $null, $TestInstaller, @([string]$TestMsiPath, 3))

        # Create the Property table.
        $ctv = $TestDb.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $TestDb, @('CREATE TABLE Property (Property CHAR(72) NOT NULL, Value CHAR(0) NOT NULL PRIMARY KEY Property)'))
        $null = $ctv.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $ctv, @([System.Reflection.Missing]::Value))

        # Seed a known row: ProductName = 'Original Name'.
        $iv = $TestDb.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $TestDb, @('INSERT INTO Property (Property, Value) VALUES (?, ?)'))
        $ir = $TestInstaller.GetType().InvokeMember('CreateRecord', [System.Reflection.BindingFlags]::InvokeMethod, $null, $TestInstaller, @(2))
        $null = $ir.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $ir, @(1, 'ProductName'))
        $null = $ir.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $ir, @(2, 'Original Name'))
        $null = $iv.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $iv, @($ir))
        $null = $TestDb.GetType().InvokeMember('Commit', [System.Reflection.BindingFlags]::InvokeMethod, $null, $TestDb, @())
    }

    AfterEach {
        # Release COM handles explicitly before GC so the MSI file lock is freed.
        if ($null -ne $TestDb) { $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($TestDb) }
        if ($null -ne $TestInstaller) { $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($TestInstaller) }
        [System.GC]::Collect()
        [System.GC]::WaitForPendingFinalizers()
        if (Test-Path -LiteralPath $TestMsiPath) { Remove-Item -LiteralPath $TestMsiPath -Force -ErrorAction SilentlyContinue }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory Database parameter' {
            (Get-Command Set-ADTMsiProperty).Parameters['Database'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a mandatory PropertyName parameter' {
            (Get-Command Set-ADTMsiProperty).Parameters['PropertyName'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a mandatory PropertyValue parameter' {
            (Get-Command Set-ADTMsiProperty).Parameters['PropertyValue'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should throw ValidateNotNullOrEmpty when Database is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Set-ADTMsiProperty'
            }
            { Set-ADTMsiProperty -Database $null -PropertyName 'ALLUSERS' -PropertyValue '1' } | Should @shouldParams
        }

        It 'Should throw ValidateNotNullOrWhiteSpace when PropertyName is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Set-ADTMsiProperty'
            }
            { Set-ADTMsiProperty -Database $TestDb -PropertyName $null -PropertyValue '1' } | Should @shouldParams
        }

        It 'Should throw ValidateNotNullOrWhiteSpace when PropertyName is empty or whitespace' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Set-ADTMsiProperty'
            }
            { Set-ADTMsiProperty -Database $TestDb -PropertyName '' -PropertyValue '1' } | Should @shouldParams
            { Set-ADTMsiProperty -Database $TestDb -PropertyName " `f`n`r`t`v" -PropertyValue '1' } | Should @shouldParams
        }

        It 'Should throw ValidateNotNullOrWhiteSpace when PropertyValue is null or whitespace' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Set-ADTMsiProperty'
            }
            { Set-ADTMsiProperty -Database $TestDb -PropertyName 'ALLUSERS' -PropertyValue $null } | Should @shouldParams
            { Set-ADTMsiProperty -Database $TestDb -PropertyName 'ALLUSERS' -PropertyValue " `f`n`r`t`v" } | Should @shouldParams
        }
    }

    Context 'Behavioural' {
        It 'Updates an existing property and the new value round-trips correctly' {
            Set-ADTMsiProperty -Database $TestDb -PropertyName 'ProductName' -PropertyValue 'New Name'
            $null = $TestDb.GetType().InvokeMember('Commit', [System.Reflection.BindingFlags]::InvokeMethod, $null, $TestDb, @())
            $val = & $ReadMsiProperty -Database $TestDb -Installer $TestInstaller -PropertyName 'ProductName'
            $val | Should -Be 'New Name'
        }

        It 'Inserts a new property when it does not already exist' {
            Set-ADTMsiProperty -Database $TestDb -PropertyName 'ALLUSERS' -PropertyValue '1'
            $null = $TestDb.GetType().InvokeMember('Commit', [System.Reflection.BindingFlags]::InvokeMethod, $null, $TestDb, @())
            $val = & $ReadMsiProperty -Database $TestDb -Installer $TestInstaller -PropertyName 'ALLUSERS'
            $val | Should -Be '1'
        }

        It 'Produces no output on success' {
            $result = Set-ADTMsiProperty -Database $TestDb -PropertyName 'ProductName' -PropertyValue 'NoOut'
            $result | Should -BeNullOrEmpty
        }

        It 'Logs a deprecation warning on invocation' {
            Set-ADTMsiProperty -Database $TestDb -PropertyName 'ProductName' -PropertyValue 'DeprecTest'
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter {
                $Severity -eq 'Warning' -and $Message -like '*deprecated*'
            }
        }

        It 'Single-quote in PropertyValue round-trips correctly via INSERT (regression for broken Replace escaping)' {
            Set-ADTMsiProperty -Database $TestDb -PropertyName 'Author' -PropertyValue "Tim O'Reilly"
            $null = $TestDb.GetType().InvokeMember('Commit', [System.Reflection.BindingFlags]::InvokeMethod, $null, $TestDb, @())
            $val = & $ReadMsiProperty -Database $TestDb -Installer $TestInstaller -PropertyName 'Author'
            $val | Should -Be "Tim O'Reilly"
        }

        It "Single-quote in PropertyName round-trips correctly via INSERT (regression for broken Replace escaping)" {
            Set-ADTMsiProperty -Database $TestDb -PropertyName "Author's Choice" -PropertyValue 'SomeValue'
            $null = $TestDb.GetType().InvokeMember('Commit', [System.Reflection.BindingFlags]::InvokeMethod, $null, $TestDb, @())
            $val = & $ReadMsiProperty -Database $TestDb -Installer $TestInstaller -PropertyName "Author's Choice"
            $val | Should -Be 'SomeValue'
        }

        It 'Single-quote in both PropertyName and PropertyValue round-trips correctly via UPDATE (regression for broken Replace escaping)' {
            # Insert the row first, then update to exercise the UPDATE path with single-quote data.
            Set-ADTMsiProperty -Database $TestDb -PropertyName "Author's Choice" -PropertyValue 'Initial'
            $null = $TestDb.GetType().InvokeMember('Commit', [System.Reflection.BindingFlags]::InvokeMethod, $null, $TestDb, @())
            Set-ADTMsiProperty -Database $TestDb -PropertyName "Author's Choice" -PropertyValue "Tim O'Reilly"
            $null = $TestDb.GetType().InvokeMember('Commit', [System.Reflection.BindingFlags]::InvokeMethod, $null, $TestDb, @())
            $val = & $ReadMsiProperty -Database $TestDb -Installer $TestInstaller -PropertyName "Author's Choice"
            $val | Should -Be "Tim O'Reilly"
        }
    }
}
