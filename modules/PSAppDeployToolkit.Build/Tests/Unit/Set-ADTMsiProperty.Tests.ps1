BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    function Copy-TestPackage
    {
        [CmdletBinding()]
        [OutputType([System.String])]
        param
        (
            [Parameter(Mandatory = $true)]
            [System.String]$Destination
        )

        # The committed test package, as Windows' own cached copies need elevation to read.
        Copy-Item -LiteralPath "$PSScriptRoot\..\Assets\PSAppDeployToolkit Test MSI.msi" -Destination $Destination -Force
        return $Destination
    }
}
Describe 'Set-ADTMsiProperty' {
    BeforeAll {
        function Invoke-AgainstDatabase
        {
            [CmdletBinding()]
            [OutputType([System.Collections.Generic.IReadOnlyDictionary[System.String, System.Object]])]
            param
            (
                [Parameter(Mandatory = $true)]
                [System.String]$Path,

                [Parameter(Mandatory = $true)]
                [System.Management.Automation.ScriptBlock]$Action
            )

            # Opened in transacted mode so that nothing reaches the file until Commit is called, which is
            # how the toolkit's own callers drive this.
            $installer = New-Object -ComObject WindowsInstaller.Installer
            $database = $installer.GetType().InvokeMember('OpenDatabase', 'InvokeMethod', $null, $installer, @($Path, 1))
            try
            {
                & $Action $database
                $null = $database.GetType().InvokeMember('Commit', 'InvokeMethod', $null, $database, $null)
            }
            finally
            {
                $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($database)
                $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
                [System.GC]::Collect()
                [System.GC]::WaitForPendingFinalizers()
            }
            return Get-ADTMsiTableProperty -LiteralPath $Path
        }
    }

    BeforeEach {
        $script:Package = Copy-TestPackage -Destination "$TestDrive\Package$([System.Guid]::NewGuid().ToString('N')).msi"
    }

    Context 'Functionality' {
        It 'Adds a property the package did not have' {
            # Transforms are built by adding properties an installer will read at run time, so a property
            # that is not already in the table has to be inserted rather than skipped.
            (Invoke-AgainstDatabase -Path $script:Package -Action { Set-ADTMsiProperty -Database $args[0] -PropertyName 'ADTTESTONLY' -PropertyValue 'a value' }).ADTTESTONLY | Should -BeExactly 'a value'
        }

        It 'Replaces a property the package already had' {
            $before = (Get-ADTMsiTableProperty -LiteralPath $script:Package).ProductName
            $after = (Invoke-AgainstDatabase -Path $script:Package -Action { Set-ADTMsiProperty -Database $args[0] -PropertyName 'ProductName' -PropertyValue 'A Replaced Name' }).ProductName
            $after | Should -BeExactly 'A Replaced Name'
            $after | Should -Not -BeExactly $before
        }

        It 'Leaves the other properties alone' {
            $before = Get-ADTMsiTableProperty -LiteralPath $script:Package
            $after = Invoke-AgainstDatabase -Path $script:Package -Action { Set-ADTMsiProperty -Database $args[0] -PropertyName 'ADTTESTONLY' -PropertyValue 'a value' }
            $after.ProductCode | Should -BeExactly $before.ProductCode
            $after.Count | Should -Be ($before.Count + 1)
        }

        It 'Sets several properties against the one database' {
            $after = Invoke-AgainstDatabase -Path $script:Package -Action {
                Set-ADTMsiProperty -Database $args[0] -PropertyName 'ADTFIRST' -PropertyValue 'first'
                Set-ADTMsiProperty -Database $args[0] -PropertyName 'ADTSECOND' -PropertyValue 'second'
            }
            $after.ADTFIRST | Should -BeExactly 'first'
            $after.ADTSECOND | Should -BeExactly 'second'
        }

        It 'Survives a value carrying a single quote' {
            # The Windows Installer query engine has no escape sequence for a quote inside a literal, so a
            # value carrying an apostrophe only survives if it never goes into the statement text.
            (Invoke-AgainstDatabase -Path $script:Package -Action { Set-ADTMsiProperty -Database $args[0] -PropertyName 'ADTQUOTED' -PropertyValue "Vendor's Product" }).ADTQUOTED | Should -BeExactly "Vendor's Product"
        }

        It 'Writes nothing with -WhatIf' {
            (Invoke-AgainstDatabase -Path $script:Package -Action { Set-ADTMsiProperty -Database $args[0] -PropertyName 'ADTTESTONLY' -PropertyValue 'a value' -WhatIf }).ContainsKey('ADTTESTONLY') | Should -BeFalse
        }
    }

    Context 'Releasing the installer it creates' {
        # The release is invisible from outside, so the installer is handed in by mock and kept here;
        # using a released object throws. The closure lets the module-scoped mock reach the variable.
        It 'Releases it once the property is set' {
            $installer = [System.Activator]::CreateInstance([System.Type]::GetTypeFromProgID('WindowsInstaller.Installer'))
            Mock -ModuleName PSAppDeployToolkit New-Object { $installer }.GetNewClosure() -ParameterFilter { $ComObject -eq 'WindowsInstaller.Installer' }

            $null = Invoke-AgainstDatabase -Path $script:Package -Action { Set-ADTMsiProperty -Database $args[0] -PropertyName 'ADTTESTONLY' -PropertyValue 'a value' }
            { $installer.GetType().InvokeMember('CreateRecord', 'InvokeMethod', $null, $installer, @(1)) } | Should -Throw -ExceptionType ([System.Runtime.InteropServices.InvalidComObjectException])
        }

        It 'Releases it when the error is raised as terminating' {
            # The path that used to leak: a terminating error unwinds straight out, so an end block never
            # runs. The database passed is a COM object the parameter takes and the query engine refuses.
            $installer = [System.Activator]::CreateInstance([System.Type]::GetTypeFromProgID('WindowsInstaller.Installer'))
            Mock -ModuleName PSAppDeployToolkit New-Object { $installer }.GetNewClosure() -ParameterFilter { $ComObject -eq 'WindowsInstaller.Installer' }
            $notADatabase = [System.Activator]::CreateInstance([System.Type]::GetTypeFromProgID('WindowsInstaller.Installer'))
            try
            {
                { Set-ADTMsiProperty -Database $notADatabase -PropertyName 'ADTTESTONLY' -PropertyValue 'a value' -ErrorAction Stop } | Should -Throw
                { $installer.GetType().InvokeMember('CreateRecord', 'InvokeMethod', $null, $installer, @(1)) } | Should -Throw -ExceptionType ([System.Runtime.InteropServices.InvalidComObjectException])
            }
            finally
            {
                $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($notADatabase)
            }
        }
    }

    Context 'Input Validation' {
        It 'Requires a database to work against' {
            Test-ADTParameterSetSatisfied -Command (Get-Command Set-ADTMsiProperty) -Parameter PropertyName, PropertyValue | Should -BeFalse
        }

        It 'Requires something that is actually a database' {
            { Set-ADTMsiProperty -Database 'not a database' -PropertyName 'ADTTESTONLY' -PropertyValue 'a value' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Requires a property name' {
            Test-ADTParameterSetSatisfied -Command (Get-Command Set-ADTMsiProperty) -Parameter Database, PropertyValue | Should -BeFalse
        }
    }
}
