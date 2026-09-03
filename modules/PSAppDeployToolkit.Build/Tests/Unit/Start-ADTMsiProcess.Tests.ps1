BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # Installing and removing a package needs elevation, so anything that needs the product present is
    # gated on it. Everything that stops before handing the package over runs either way.
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

    # A package built for this: one feature, one component, no files, and a single empty registry key. It
    # installs and removes cleanly, which is what makes it safe to hand to Windows Installer here. Anything
    # already on the machine would mean changing software somebody is using.
    $script:Package = "$PSScriptRoot\..\Assets\PSAppDeployToolkit Test MSI.msi"

    # Read from the package rather than written down, so that rebuilding it does not silently leave these
    # tests asserting against a product that no longer exists.
    $script:Properties = Get-ADTMsiTableProperty -LiteralPath $script:Package -Table Property
    $script:ProductCode = $script:Properties['ProductCode']
    $script:ProductName = $script:Properties['ProductName']

    # The package is 32-bit, so its key lands in the WOW6432Node view of the registry.
    $script:ProductKey = 'HKLM:\SOFTWARE\WOW6432Node\PSAppDeployToolkit TEST Reg'

    # The package creates this key with a Registry row of '+', which by design leaves it behind when the
    # product is removed, so installing here would otherwise add a key to the machine that nothing takes
    # away again.

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

        # By product code rather than by path, so that this works whatever a test did to its own copy, and
        # quietly, since it runs as cleanup after tests that have already removed the product themselves.
        if (Test-ProductInstalled)
        {
            $null = Start-ADTMsiProcess -Action Uninstall -ProductCode $script:ProductCode -InformationAction SilentlyContinue
        }
    }

    function Copy-TestPackage
    {
        [CmdletBinding()]
        [OutputType([System.String])]
        param
        (
            [Parameter(Mandatory = $true)]
            [System.String]$Destination,

            [Parameter(Mandatory = $false)]
            [System.Management.Automation.SwitchParameter]$NewProductCode
        )

        Copy-Item -LiteralPath $script:Package -Destination $Destination -Force
        if (!$NewProductCode)
        {
            return $Destination
        }

        # Stamped with a product code nothing has ever installed, which is what makes the branches for a
        # package that is not present reachable without removing the one that is.
        $installer = New-Object -ComObject WindowsInstaller.Installer
        $database = $installer.GetType().InvokeMember('OpenDatabase', 'InvokeMethod', $null, $installer, @($Destination, 1))
        try
        {
            Set-ADTMsiProperty -Database $database -PropertyName 'ProductCode' -PropertyValue "{$([System.Guid]::NewGuid().ToString().ToUpperInvariant())}"
            $null = $database.GetType().InvokeMember('Commit', 'InvokeMethod', $null, $database, $null)
        }
        finally
        {
            $null = [System.Runtime.InteropServices.Marshal]::ReleaseComObject($database)
            $null = [System.Runtime.InteropServices.Marshal]::ReleaseComObject($installer)
        }
        return $Destination
    }
}

AfterAll {
    # Whatever was installed here is removed, and the key the package leaves behind goes with it, so the
    # run finishes with the machine as it started. Removed whether or not it was already there, as nothing
    # but this package ever creates it: sparing one that pre-existed meant a run which once failed to clean
    # up kept the key for good, and every run after it read the leftover as the machine's own.
    Remove-TestProduct
    if (Test-Path -LiteralPath $script:ProductKey)
    {
        Remove-Item -LiteralPath $script:ProductKey -Recurse -Force
    }
    Import-ADTModuleUnderTest -Force
}

Describe 'Start-ADTMsiProcess' {
    Context 'Input Validation' {
        It 'Refuses a package that is not there' {
            { Start-ADTMsiProcess -Action Install -FilePath "$TestDrive\NeverExisted.msi" } | Should -Throw -ErrorId 'FilePathNotFound,Start-ADTMsiProcess'
        }

        It 'Refuses an action it does not know' {
            { Start-ADTMsiProcess -Action 'Frobnicate' -FilePath "$TestDrive\NeverExisted.msi" } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTMsiProcess'
        }

        It 'Requires something to work on' {
            # A package can be named by path or by product code, and neither has a default.
            { Start-ADTMsiProcess -Action Install } | Should -Throw -ErrorId 'AmbiguousParameterSet,Start-ADTMsiProcess'
        }

        It 'Refuses a product code that is not a GUID' {
            { Start-ADTMsiProcess -Action Uninstall -ProductCode 'not-a-guid' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a product code for an install' {
            # There is nothing to install from a product code: it names something already registered, so it
            # only makes sense for removing, repairing or patching.
            { Start-ADTMsiProcess -Action Install -ProductCode $script:ProductCode -WhatIf } | Should -Throw -ErrorId 'ProductCodeInstallActionNotSupported,Start-ADTMsiProcess'
        }

        It 'Refuses a transform that is not there' {
            # Windows Installer would fail the install anyway, but only after the package had begun.
            { Start-ADTMsiProcess -Action Install -FilePath $script:Package -Transforms "$TestDrive\NeverExisted.mst" -WhatIf } | Should -Throw -ErrorId 'InvalidTransformPathParameterValue,Start-ADTMsiProcess'
        }
    }

    Context 'Deciding what to do' {
        # Everything here stops at the point of handing the package over, so nothing is installed, repaired
        # or removed. What is under test is the whole of the decision made before that: which product the
        # arguments name, whether it is already present, and what msiexec would be asked to do.
        It 'Reports nothing to do for a product that is not installed' {
            # 1605 is Windows Installer's own code for a product it does not know, so a caller checking
            # exit codes gets the same answer they would from msiexec.
            $result = Start-ADTMsiProcess -Action Uninstall -ProductCode "{$([System.Guid]::NewGuid().ToString())}" -PassThru -WhatIf
            $result | Should -BeOfType ([PSADT.ProcessManagement.ProcessResult])
            $result.ExitCode | Should -Be 1605
        }

        It 'Carries on with an install of a package that is not present' {
            $stranger = Copy-TestPackage -Destination "$TestDrive\Stranger.msi" -NewProductCode
            { Start-ADTMsiProcess -Action Install -FilePath $stranger -WhatIf } | Should -Not -Throw
        }

        It 'Recognises the package it already has installed' -Skip:(!$script:IsElevated) {
            # The already-installed check is what stops a deployment reinstalling something needlessly, so
            # it has to recognise the product from the package file alone.
            try
            {
                $null = Start-ADTMsiProcess -Action Install -FilePath $script:Package -InformationAction SilentlyContinue
                { Start-ADTMsiProcess -Action Install -FilePath $script:Package -WhatIf } | Should -Not -Throw
                Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*already installed*' }
            }
            finally
            {
                Remove-TestProduct
            }
        }

        It 'Takes a relative transform alongside the package' {
            # A deployment keeps its transform next to its package, and refers to it by name.
            $null = New-ADTMsiTransform -MsiPath $script:Package -NewTransformPath "$TestDrive\Relative.mst" -TransformProperties @{ ADTTESTONLY = 'a value' }
            $package = Copy-TestPackage -Destination "$TestDrive\Relative.msi"
            { Start-ADTMsiProcess -Action Install -FilePath $package -Transforms 'Relative.mst' -WhatIf } | Should -Not -Throw
        }

        It 'Takes arguments as one string or as several' {
            # Both forms turn up in deployment scripts, and a single string has to be split the way a
            # command line would be rather than passed through as one argument.
            { Start-ADTMsiProcess -Action Install -FilePath $script:Package -ArgumentList '/qn /norestart' -WhatIf } | Should -Not -Throw
            { Start-ADTMsiProcess -Action Install -FilePath $script:Package -ArgumentList '/qn', '/norestart' -WhatIf } | Should -Not -Throw
            { Start-ADTMsiProcess -Action Install -FilePath $script:Package -AdditionalArgumentList 'A=1 B=2' -WhatIf } | Should -Not -Throw
            { Start-ADTMsiProcess -Action Install -FilePath $script:Package -AdditionalArgumentList 'A=1', 'B=2' -WhatIf } | Should -Not -Throw
        }

        It 'Takes a log file name with or without an extension' {
            { Start-ADTMsiProcess -Action Install -FilePath $script:Package -LogFileName 'ADTTestOnly' -WhatIf } | Should -Not -Throw
            { Start-ADTMsiProcess -Action Install -FilePath $script:Package -LogFileName 'ADTTestOnly.log' -WhatIf } | Should -Not -Throw
        }

        It 'Leaves a log file name that is already a full path where it is' {
            # A caller who named a path meant that path, so no log directory is chosen on their behalf.
            { Start-ADTMsiProcess -Action Install -FilePath $script:Package -LogFileName "$TestDrive\Named\ADTTestOnly.log" -WhatIf } | Should -Not -Throw
        }

        It 'Logs to the MSI log path when the config names one' {
            # An MSI log path in the config overrides both the session's and the toolkit's, and is created
            # if it is not there, since a deployment cannot be expected to make it first.
            $config = Get-ADTConfig
            $original = $config.MSI.LogPath
            try
            {
                $config.MSI.LogPath = "$TestDrive\MsiLogs"
                { Start-ADTMsiProcess -Action Install -FilePath $script:Package -WhatIf } | Should -Not -Throw
                Test-Path -LiteralPath $config.MSI.LogPath -PathType Container | Should -BeTrue
            }
            finally
            {
                $config.MSI.LogPath = $original
            }
        }

        It 'Accepts the logging options it is given' {
            { Start-ADTMsiProcess -Action Install -FilePath $script:Package -LoggingOptions '/l*v' -WhatIf } | Should -Not -Throw
        }

        It 'Prepares it for the logged-on user' {
            # A package run in the user's own session logs under their name, so that two users installing
            # the same thing do not write over each other's log.
            $user = InModuleScope PSAppDeployToolkit { Get-ADTClientServerUser }
            $user | Should -Not -BeNullOrEmpty
            { Start-ADTMsiProcess -Action Install -FilePath $script:Package -RunAsActiveUser $user -WhatIf } | Should -Not -Throw
        }
    }

    Context 'Deciding what to do about a product that is installed' -Skip:(!$script:IsElevated) {
        # Every action other than an install stops early for a product that is not there, so none of them
        # reach the point of deciding what msiexec would be asked to do without one that is. Installed once
        # for the whole context, and every test within it stops short of running anything.
        BeforeAll {
            Remove-TestProduct
            Start-ADTMsiProcess -Action Install -FilePath $script:Package -InformationAction SilentlyContinue
        }

        AfterAll {
            Remove-TestProduct
        }

        It 'Prepares a <Action>' -ForEach @(
            @{ Action = 'Uninstall' }
            @{ Action = 'Repair' }
            @{ Action = 'ActiveSetup' }
            @{ Action = 'Patch' }
        ) {
            { Start-ADTMsiProcess -Action $Action -FilePath $script:Package -WhatIf } | Should -Not -Throw
        }

        It 'Prepares a repair in <RepairMode> mode' -ForEach @(
            @{ RepairMode = 'Reinstall' }
            @{ RepairMode = 'Repair' }
        ) {
            # Reinstall goes back through an install with REINSTALL set, where Repair uses msiexec's own
            # repair switch, so the two produce entirely different command lines.
            { Start-ADTMsiProcess -Action Repair -FilePath $script:Package -RepairMode $RepairMode -WhatIf } | Should -Not -Throw
        }

        It 'Prepares a repair from source' {
            { Start-ADTMsiProcess -Action Repair -FilePath $script:Package -RepairMode Repair -RepairFromSource -WhatIf } | Should -Not -Throw
        }

        It 'Takes patches to apply' {
            Set-Content -LiteralPath "$TestDrive\Dummy.msp" -Value 'not a patch'
            { Start-ADTMsiProcess -Action Uninstall -FilePath $script:Package -Patches "$TestDrive\Dummy.msp" -WhatIf } | Should -Not -Throw
        }

        It 'Takes a relative patch alongside the package' {
            $package = Copy-TestPackage -Destination "$TestDrive\WithPatch.msi"
            Set-Content -LiteralPath "$TestDrive\Relative.msp" -Value 'not a patch'
            { Start-ADTMsiProcess -Action Uninstall -FilePath $package -Patches 'Relative.msp' -WhatIf } | Should -Not -Throw
        }

        It 'Names the log after an application it was handed' {
            # Handed an installed application rather than a package, the name has to come off that object,
            # since there is no package file to read a product name out of.
            $application = Get-ADTApplication -ProductCode $script:ProductCode -InformationAction SilentlyContinue
            $application | Should -Not -BeNullOrEmpty
            { $application | Start-ADTMsiProcess -Action Uninstall -WhatIf } | Should -Not -Throw
        }

        It 'Prepares a removal by product code' {
            { Start-ADTMsiProcess -Action Uninstall -ProductCode $script:ProductCode -WhatIf } | Should -Not -Throw
        }
    }

    Context 'Within a deployment session' {
        BeforeAll {
            $script:Deploy = "$TestDrive\Deploy"
            $null = New-Item -Path "$script:Deploy\Files" -ItemType Directory -Force
            Copy-Item -LiteralPath $script:Package -Destination "$script:Deploy\Files\Deployed.msi"
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'MsiSession' -DeployMode Silent -ScriptDirectory $script:Deploy -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Finds a package named by its file name alone' {
            # A deployment refers to its package by name, and it sits in the deployment's own Files folder
            # rather than anywhere on the path.
            { Start-ADTMsiProcess -Action Install -FilePath 'Deployed.msi' -WhatIf } | Should -Not -Throw
        }

        It 'Still reports a name that is nowhere' {
            { Start-ADTMsiProcess -Action Install -FilePath 'NeverExisted.msi' -WhatIf } | Should -Throw -ErrorId 'FilePathNotFound,Start-ADTMsiProcess'
        }
    }

    Context 'Handing the package to Windows Installer' -Skip:(!$script:IsElevated) {
        # The only tests here that change the machine, and the only ones that reach msiexec at all. The
        # package installs one empty registry key and its own registration, and every test removes it again
        # whether it passed or not. The suite leaves it removed.
        BeforeEach {
            Remove-TestProduct
        }

        AfterEach {
            Remove-TestProduct
        }

        It 'Installs the package' {
            Start-ADTMsiProcess -Action Install -FilePath $script:Package -InformationAction SilentlyContinue
            Test-ProductInstalled | Should -BeTrue
            Test-Path -LiteralPath $script:ProductKey | Should -BeTrue
        }

        It 'Reports the exit code with -PassThru' {
            $result = Start-ADTMsiProcess -Action Install -FilePath $script:Package -PassThru -InformationAction SilentlyContinue
            $result | Should -BeOfType ([PSADT.ProcessManagement.ProcessResult])
            $result.ExitCode | Should -Be 0
        }

        It 'Writes a log where the config points' {
            # The MSI log path is empty by default and there is no session here, so this is the toolkit's
            # own log path, which the test module has already pointed under TestDrive.
            Start-ADTMsiProcess -Action Install -FilePath $script:Package -LogFileName 'ADTTestOnlyInstall.log' -InformationAction SilentlyContinue
            @(Get-ChildItem -LiteralPath (Get-ADTConfig).Toolkit.LogPath -Recurse -File -Filter 'ADTTestOnlyInstall*').Count | Should -BeGreaterThan 0
        }

        It 'Names the log after the product and the action' {
            # Two deployments logging to the same folder must not write over one another, so the name is
            # taken from the product rather than being fixed.
            Start-ADTMsiProcess -Action Install -FilePath $script:Package -InformationAction SilentlyContinue
            @(Get-ChildItem -LiteralPath (Get-ADTConfig).Toolkit.LogPath -Recurse -File -Filter "*$($script:ProductName -replace '\s+')*Install*").Count | Should -BeGreaterThan 0
        }

        It 'Reports an install of what is already there' {
            # 1638 is Windows Installer's own code for a product already present at this version, so a
            # caller checking exit codes gets the same answer msiexec would have given.
            Start-ADTMsiProcess -Action Install -FilePath $script:Package -InformationAction SilentlyContinue
            $result = Start-ADTMsiProcess -Action Install -FilePath $script:Package -PassThru -InformationAction SilentlyContinue
            $result.ExitCode | Should -Be 1638
        }

        It 'Installs it again anyway when told to skip the check' {
            Start-ADTMsiProcess -Action Install -FilePath $script:Package -InformationAction SilentlyContinue
            $result = Start-ADTMsiProcess -Action Install -FilePath $script:Package -SkipMSIAlreadyInstalledCheck -PassThru -InformationAction SilentlyContinue
            $result.ExitCode | Should -Be 0
            Test-ProductInstalled | Should -BeTrue
        }

        It 'Repairs what is installed' {
            Start-ADTMsiProcess -Action Install -FilePath $script:Package -InformationAction SilentlyContinue
            $result = Start-ADTMsiProcess -Action Repair -FilePath $script:Package -SkipMSIAlreadyInstalledCheck -PassThru -InformationAction SilentlyContinue
            $result.ExitCode | Should -Be 0
            Test-ProductInstalled | Should -BeTrue
        }

        It 'Removes it by package' {
            # Only the registration is checked. The package authors its key with a Registry row of '+',
            # which creates the key on install and leaves it in place on removal, so its absence afterwards
            # is not something this function can be held to.
            Start-ADTMsiProcess -Action Install -FilePath $script:Package -InformationAction SilentlyContinue
            Start-ADTMsiProcess -Action Uninstall -FilePath $script:Package -InformationAction SilentlyContinue
            Test-ProductInstalled | Should -BeFalse
        }

        It 'Removes it by product code' {
            # A deployment removing an earlier version has the code but not the package it came from.
            Start-ADTMsiProcess -Action Install -FilePath $script:Package -InformationAction SilentlyContinue
            Start-ADTMsiProcess -Action Uninstall -ProductCode $script:ProductCode -InformationAction SilentlyContinue
            Test-ProductInstalled | Should -BeFalse
        }

        It 'Removes what it was handed' {
            # Reading what is installed, deciding from it, and passing it straight back is the ordinary way
            # round for an uninstall, and saves the caller taking the code back out of the object.
            Start-ADTMsiProcess -Action Install -FilePath $script:Package -InformationAction SilentlyContinue
            Get-ADTApplication -ProductCode $script:ProductCode -InformationAction SilentlyContinue | Start-ADTMsiProcess -Action Uninstall -InformationAction SilentlyContinue
            Test-ProductInstalled | Should -BeFalse
        }
    }
}
