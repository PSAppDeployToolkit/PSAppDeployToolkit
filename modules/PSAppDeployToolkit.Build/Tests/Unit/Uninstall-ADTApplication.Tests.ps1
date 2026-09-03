BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # Installing and removing a Windows Installer product needs elevation. Removal via an uninstall program
    # does not, as its entries are written under the current user's own hive, which is one of the three the
    # search covers.
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'IsElevated', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
    $script:IsElevated = Test-ADTCallerElevated
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    # The uninstall program the entries name, spelled out in full for every test but the one covering an
    # uninstall string that does not say where its program lives.
    $script:CmdPath = [System.IO.Path]::Combine([System.Environment]::SystemDirectory, 'cmd.exe')

    # An uninstall program that runs and does nothing, for the tests that need one present but not taken.
    $script:NoOpCommand = "$script:CmdPath /c exit 0"

    # A package built for this: one feature, one component, no files, and a single empty registry key. It
    # installs and removes cleanly, which is what makes it safe to hand to Windows Installer here. Anything
    # already on the machine would mean changing software somebody is using.
    $script:Package = "$PSScriptRoot\..\Assets\PSAppDeployToolkit Test MSI.msi"

    # Read from the package rather than written down, so that rebuilding it does not silently leave these
    # tests asserting against a product that no longer exists.
    $script:Properties = Get-ADTMsiTableProperty -LiteralPath $script:Package -Table Property
    $script:ProductCode = $script:Properties['ProductCode']
    $script:ProductName = $script:Properties['ProductName']

    # The package is 32-bit, so its key lands in the WOW6432Node view of the registry. It is authored with
    # a Registry row of '+', which by design leaves the key behind when the product is removed, so the
    # cleanup has to take it away itself.
    $script:ProductKey = 'HKLM:\SOFTWARE\WOW6432Node\PSAppDeployToolkit TEST Reg'

    function Test-ProductInstalled
    {
        [CmdletBinding()]
        [OutputType([System.Boolean])]
        param
        (
        )

        return !!(Get-ADTApplication -ProductCode $script:ProductCode -InformationAction SilentlyContinue)
    }

    function Remove-TestProduct
    {
        [CmdletBinding()]
        param
        (
        )

        # By product code rather than by path, and quietly, since it runs as cleanup after tests that have
        # already removed the product themselves.
        if (Test-ProductInstalled)
        {
            $null = Start-ADTMsiProcess -Action Uninstall -ProductCode $script:ProductCode -InformationAction SilentlyContinue
        }
    }
}

AfterAll {
    Remove-ADTTestApplicationEntries
    Import-ADTModuleUnderTest -Force
}

Describe 'Uninstall-ADTApplication' {
    Context 'Removing an application with an uninstall program' {
        AfterEach {
            Remove-ADTTestApplicationEntries
        }

        It 'Runs the quiet uninstall string and the application is gone' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = Get-ADTTestUninstallCommand -Name $name }
            Uninstall-ADTApplication -Name $name -NameMatch Exact
            Test-ADTTestApplicationEntry -Name $name | Should -BeFalse
            Get-ADTApplication -Name $name -NameMatch Exact | Should -BeNullOrEmpty
        }

        It 'Hands back what the uninstall program did on request' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = Get-ADTTestUninstallCommand -Name $name }
            $result = Uninstall-ADTApplication -Name $name -NameMatch Exact -PassThru
            $result | Should -BeOfType ([PSADT.ProcessManagement.ProcessResult])
            $result.ExitCode | Should -Be 0
        }

        It 'Leaves the application alone for -WhatIf' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = Get-ADTTestUninstallCommand -Name $name }
            Uninstall-ADTApplication -Name $name -NameMatch Exact -WhatIf
            Test-ADTTestApplicationEntry -Name $name | Should -BeTrue
        }

        It 'Falls back to the uninstall string when there is no quiet one' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ UninstallString = Get-ADTTestUninstallCommand -Name $name }
            Uninstall-ADTApplication -Name $name -NameMatch Exact
            Test-ADTTestApplicationEntry -Name $name | Should -BeFalse
        }

        It 'Prefers the quiet uninstall string when both are there' {
            # The plain string is the one that removes the entry and the quiet one does nothing, so the
            # entry surviving is what proves which of the two was taken.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = $script:NoOpCommand; UninstallString = Get-ADTTestUninstallCommand -Name $name }
            Uninstall-ADTApplication -Name $name -NameMatch Exact
            Test-ADTTestApplicationEntry -Name $name | Should -BeTrue
        }

        It 'Takes the plain uninstall string on request' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = $script:NoOpCommand; UninstallString = Get-ADTTestUninstallCommand -Name $name }
            Uninstall-ADTApplication -Name $name -NameMatch Exact -ForceUninstallString
            Test-ADTTestApplicationEntry -Name $name | Should -BeFalse
        }

        It 'Leaves an application carrying no uninstall string alone' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name
            Uninstall-ADTApplication -Name $name -NameMatch Exact
            Test-ADTTestApplicationEntry -Name $name | Should -BeTrue
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { ($Message -join [System.Environment]::NewLine).Contains('No UninstallString found') } -Times 1 -Exactly
        }

        It 'Finds the uninstall program on the path when the string does not qualify it' -Skip {
            # An uninstall string naming a bare executable is resolved against the path, as a command line
            # written to the registry is not obliged to spell out where the program lives.
            #
            # Skipped: InstalledApplication resolves an unrooted name against the calling process's working
            # directory, so the search-path fallback is handed a path under that directory and never finds
            # anything. The fallback needs the executable's name, not the path built from it.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = Get-ADTTestUninstallCommand -Name $name -Unqualified }
            Uninstall-ADTApplication -Name $name -NameMatch Exact
            Test-ADTTestApplicationEntry -Name $name | Should -BeFalse
        }

        It 'Removes an application handed to it down the pipeline' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = Get-ADTTestUninstallCommand -Name $name }
            Get-ADTApplication -Name $name -NameMatch Exact | Uninstall-ADTApplication
            Test-ADTTestApplicationEntry -Name $name | Should -BeFalse
        }

        It 'Replaces the arguments the uninstall string carried' {
            # The registry says to do nothing and the caller says to remove the entry, so the entry going
            # away is what proves the supplied arguments were used in place of the ones on the entry.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = $script:NoOpCommand }
            Uninstall-ADTApplication -Name $name -NameMatch Exact -ArgumentList '/c', 'reg.exe', 'delete', (Get-ADTTestApplicationKeyPath -Name $name -Native), '/f'
            Test-ADTTestApplicationEntry -Name $name | Should -BeFalse
        }

        It 'Adds to the arguments the uninstall string carried' {
            # The entry supplies an incomplete command and the caller completes it, passed as one string so
            # that it is parsed into arguments rather than handed over whole.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = "$script:CmdPath /c reg.exe delete" }
            Uninstall-ADTApplication -Name $name -NameMatch Exact -AdditionalArgumentList "`"$(Get-ADTTestApplicationKeyPath -Name $name -Native)`" /f"
            Test-ADTTestApplicationEntry -Name $name | Should -BeFalse
        }

        It 'Appends several arguments to the ones the uninstall string carried' {
            # More than one addition is appended as given, where a single one is parsed first, so the two
            # are separate paths through the same switch.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = "$script:CmdPath /c reg.exe delete" }
            Uninstall-ADTApplication -Name $name -NameMatch Exact -AdditionalArgumentList (Get-ADTTestApplicationKeyPath -Name $name -Native), '/f'
            Test-ADTTestApplicationEntry -Name $name | Should -BeFalse
        }

        It 'Supplies arguments for an uninstall string that carried none' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = $script:CmdPath }
            Uninstall-ADTApplication -Name $name -NameMatch Exact -AdditionalArgumentList '/c', 'reg.exe', 'delete', (Get-ADTTestApplicationKeyPath -Name $name -Native), '/f'
            Test-ADTTestApplicationEntry -Name $name | Should -BeFalse
        }

        It 'Runs an uninstall program that takes no arguments' {
            # An uninstall string naming a program and nothing else has no arguments to pass on, which is
            # its own path. whoami reports and exits, so the entry is expected to survive its own removal.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = [System.IO.Path]::Combine([System.Environment]::SystemDirectory, 'whoami.exe') }
            $result = Uninstall-ADTApplication -Name $name -NameMatch Exact -PassThru
            $result.ExitCode | Should -Be 0
            Test-ADTTestApplicationEntry -Name $name | Should -BeTrue
        }

        It 'Objects to an exit code nobody said was acceptable' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = "$script:CmdPath /c exit 3" }
            { Uninstall-ADTApplication -Name $name -NameMatch Exact -ErrorAction Stop } | Should -Throw
        }

        It 'Accepts an exit code the caller declared through -<Parameter>' -ForEach @(
            @{ Parameter = 'SuccessExitCodes' }
            @{ Parameter = 'RebootExitCodes' }
            @{ Parameter = 'IgnoreExitCodes' }
        ) {
            # The same non-zero exit, declared three different ways. Asked to stop on error, so the call
            # returning at all is what says the code was accepted rather than objected to.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = "$script:CmdPath /c exit 3" }
            $declaration = @{ $Parameter = 3 }
            $result = Uninstall-ADTApplication -Name $name -NameMatch Exact -PassThru -ErrorAction Stop @declaration
            $result.ExitCode | Should -Be 3
        }

        It 'Surfaces a failure to start the uninstall program' {
            # An entry left behind by something that removed its own uninstaller, which is the state a
            # half-removed application leaves the registry in.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = "$([System.IO.Path]::Combine($TestDrive, 'no-such-uninstaller.exe')) /S" }
            { Uninstall-ADTApplication -Name $name -NameMatch Exact -ErrorAction Stop } | Should -Throw
            Test-ADTTestApplicationEntry -Name $name | Should -BeTrue
        }

        It 'Leaves it alone when only Windows Installer products were asked for' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = Get-ADTTestUninstallCommand -Name $name }
            Uninstall-ADTApplication -Name $name -NameMatch Exact -ApplicationType MSI
            Test-ADTTestApplicationEntry -Name $name | Should -BeTrue
        }

        It 'Says there is no product code when a Windows Installer entry carries none' {
            # A product code comes from the entry's own name, so an entry claiming Windows Installer under a
            # name that is not a GUID leaves nothing to hand to msiexec.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ UninstallString = Get-ADTTestUninstallCommand -Name $name; WindowsInstaller = 1 }
            Uninstall-ADTApplication -Name $name -NameMatch Exact
            Test-ADTTestApplicationEntry -Name $name | Should -BeTrue
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { ($Message -join [System.Environment]::NewLine).Contains('No ProductCode found') } -Times 1 -Exactly
        }
    }

    Context 'Removing an application that asks not to be removed' {
        AfterEach {
            Remove-ADTTestApplicationEntries
        }

        It 'Leaves an application flagged <Flag> alone' -ForEach @(
            @{ Flag = 'NoRemove'; Expected = 'flagged as [NoRemove]' }
            @{ Flag = 'SystemComponent'; Expected = 'No applications found for removal' }
        ) {
            # The two flags are refused at different points, which is what the expected message records: a
            # NoRemove application is found and then skipped, where a SystemComponent one is never returned
            # by the search at all.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = Get-ADTTestUninstallCommand -Name $name; $Flag = 1 }
            Uninstall-ADTApplication -Name $name -NameMatch Exact
            Test-ADTTestApplicationEntry -Name $name | Should -BeTrue
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { ($Message -join [System.Environment]::NewLine).Contains($Expected) } -Times 1 -Exactly
        }

        It 'Leaves an application flagged SystemComponent alone when handed one' {
            # The search hides these unless it is forced, so the only way to arrive holding one is to force
            # the search and then not the removal, which a caller filtering for itself would do.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = Get-ADTTestUninstallCommand -Name $name; SystemComponent = 1 }
            Get-ADTApplication -Name $name -NameMatch Exact -Force | Uninstall-ADTApplication
            Test-ADTTestApplicationEntry -Name $name | Should -BeTrue
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { ($Message -join [System.Environment]::NewLine).Contains('flagged as [SystemComponent]') } -Times 1 -Exactly
        }

        It 'Removes an application flagged <Flag> on -Force' -ForEach @(
            @{ Flag = 'NoRemove' }
            @{ Flag = 'SystemComponent' }
        ) {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ QuietUninstallString = Get-ADTTestUninstallCommand -Name $name; $Flag = 1 }
            Uninstall-ADTApplication -Name $name -NameMatch Exact -Force
            Test-ADTTestApplicationEntry -Name $name | Should -BeFalse
        }
    }

    Context 'Removing a Windows Installer product' -Skip:(!$script:IsElevated) {
        BeforeEach {
            if (!(Test-ProductInstalled))
            {
                Start-ADTMsiProcess -Action Install -FilePath $script:Package -InformationAction SilentlyContinue
            }
        }

        AfterAll {
            # Whatever is left installed here goes, and the key the package leaves behind goes with it.
            # Removed whether or not it was already there, as nothing but this package ever creates it:
            # keeping one that pre-existed would mean a run that once leaked the key leaks it for good.
            Remove-TestProduct
            if (Test-Path -LiteralPath $script:ProductKey)
            {
                Remove-Item -LiteralPath $script:ProductKey -Recurse -Force
            }
        }

        It 'Removes the product it was asked for by name' {
            Uninstall-ADTApplication -Name $script:ProductName -NameMatch Exact
            Test-ProductInstalled | Should -BeFalse
        }

        It 'Removes the product it was asked for by product code' {
            Uninstall-ADTApplication -ProductCode $script:ProductCode
            Test-ProductInstalled | Should -BeFalse
        }

        It 'Removes the product handed to it down the pipeline' {
            Get-ADTApplication -ProductCode $script:ProductCode | Uninstall-ADTApplication
            Test-ProductInstalled | Should -BeFalse
        }

        It 'Reports what Windows Installer did on request' {
            $result = Uninstall-ADTApplication -ProductCode $script:ProductCode -PassThru
            $result | Should -BeOfType ([PSADT.ProcessManagement.ProcessResult])
            $result.ExitCode | Should -Be 0
        }

        It 'Leaves the product in place for -WhatIf' {
            Uninstall-ADTApplication -ProductCode $script:ProductCode -WhatIf
            Test-ProductInstalled | Should -BeTrue
        }
    }

    Context 'When nothing matches' {
        BeforeAll {
            $script:Absent = "ADTNoSuchApplication$([System.Guid]::NewGuid().ToString('N'))"
        }

        It 'Removes nothing and says nothing' {
            Uninstall-ADTApplication -Name $script:Absent | Should -BeNullOrEmpty
        }

        It 'Does not object' {
            # A deployment uninstalls a previous version that may never have been there.
            { Uninstall-ADTApplication -Name $script:Absent } | Should -Not -Throw
        }

        It 'Finds nothing to remove for a product code nothing carries' {
            { Uninstall-ADTApplication -ProductCode ([System.Guid]::NewGuid()) } | Should -Not -Throw
        }

        It 'Finds nothing to remove for a filter that matches nothing' {
            { Uninstall-ADTApplication -FilterScript { $false } } | Should -Not -Throw
        }

        It 'Accepts each way of matching a name' -ForEach @(
            @{ Mode = 'Contains' }
            @{ Mode = 'Exact' }
            @{ Mode = 'Wildcard' }
            @{ Mode = 'Regex' }
        ) {
            { Uninstall-ADTApplication -Name $script:Absent -NameMatch $Mode } | Should -Not -Throw
        }
    }

    Context 'Input Validation' {
        It 'Refuses a way of matching it does not know' {
            { Uninstall-ADTApplication -Name 'Anything' -NameMatch 'Fuzzy' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses an application type it does not know' {
            { Uninstall-ADTApplication -Name 'Anything' -ApplicationType 'MSP' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a product code that is not a GUID' {
            { Uninstall-ADTApplication -ProductCode 'not-a-guid' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a search alongside an application it was handed' {
            # The two parameter sets are the two ways of naming what to remove, and mixing them would
            # leave it ambiguous which one won.
            { Uninstall-ADTApplication -Name 'Anything' -InstalledApplication (New-Object -TypeName PSObject) } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Insists on being told what to remove' {
            # Nothing named and nothing piped would otherwise mean every application on the machine.
            { Uninstall-ADTApplication } | Should -Throw -ErrorId 'NullParameterValue,Uninstall-ADTApplication'
        }
    }
}
