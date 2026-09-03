BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    function Open-Probe
    {
        param
        (
            [Parameter(Mandatory = $false)]
            [System.Collections.Hashtable]$Splat = @{}
        )

        $defaults = @{
            SessionState = $ExecutionContext.SessionState
            AppName = 'OpenProbe'
            DeployMode = 'Silent'
            PassThru = $true
            InformationAction = 'SilentlyContinue'
        }
        foreach ($pair in $Splat.GetEnumerator())
        {
            $defaults[$pair.Key] = $pair.Value
        }
        return Open-ADTSession @defaults
    }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Open-ADTSession' {
    Context 'Functionality' {
        AfterEach {
            # Whatever a test opened, unwind it so the next one starts from nothing.
            while (Test-ADTSessionActive)
            {
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            }
        }

        It 'Returns the session with -PassThru' {
            Open-Probe | Should -BeOfType ([PSAppDeployToolkit.Foundation.DeploymentSession])
        }

        It 'Returns nothing without -PassThru' {
            Open-Probe -Splat @{ PassThru = $false } | Should -BeNullOrEmpty
            Test-ADTSessionActive | Should -BeTrue
        }

        It 'Builds the install name from the vendor, name and version' {
            # The install name is what the log file and the deferral registry key are named after, so its
            # composition is part of the contract rather than cosmetic.
            (Open-Probe -Splat @{ AppVendor = 'Vendor'; AppName = 'Product'; AppVersion = '2.5' }).InstallName | Should -BeExactly 'Vendor_Product_2.5'
        }

        It 'Writes its log where the config points' {
            $null = Open-Probe -Splat @{ AppName = 'LogProbe' }
            @(Get-ChildItem -LiteralPath (Get-ADTConfig).Toolkit.LogPath -Recurse -File -Filter '*LogProbe*').Count | Should -BeGreaterThan 0
        }

        It 'Records the deployment type it was told' {
            (Open-Probe -Splat @{ DeploymentType = 'Uninstall' }).DeploymentType | Should -Be ([PSAppDeployToolkit.Foundation.DeploymentType]::Uninstall)
        }

        It 'Defaults the deployment type to an install' {
            (Open-Probe).DeploymentType | Should -Be ([PSAppDeployToolkit.Foundation.DeploymentType]::Install)
        }

        It 'Runs in the deploy mode it was given' {
            (Open-Probe -Splat @{ DeployMode = 'Silent' }).DeployMode | Should -Be ([PSAppDeployToolkit.Foundation.DeployMode]::Silent)
        }

        It 'Stacks a nested session on top rather than replacing it' {
            $outer = Open-Probe -Splat @{ AppName = 'Outer' }
            $inner = Open-Probe -Splat @{ AppName = 'Inner' }
            InModuleScope PSAppDeployToolkit { $ADT.Sessions.Count } | Should -Be 2
            (Get-ADTSession).InstallName | Should -BeExactly $inner.InstallName
            $outer.InstallName | Should -Not -BeExactly $inner.InstallName
        }

        It 'Hands the session back in the execution phase' {
            # Initialization covers the banner it writes while opening; by the time the caller has it, the
            # deployment proper has started, and Close-ADTSession moves it on to Finalization.
            (Open-Probe).InstallPhase | Should -BeExactly 'Execution'
        }
    }

    Context 'Carrying extra values onto the session' {
        AfterEach {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Puts an argument it does not recognise onto the session' {
            # This is how a deployment script declares its own values once and reads them back off the
            # session everywhere else, rather than threading them through by hand.
            $session = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ExtraValues' -DeployMode Silent -PassThru -InformationAction SilentlyContinue -SomethingOfMyOwn 'a value of my own'
            $session.SomethingOfMyOwn | Should -BeExactly 'a value of my own'
        }

        It 'Leaves the values it does recognise where they belong' {
            $session = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ExtraValues' -DeployMode Silent -PassThru -InformationAction SilentlyContinue -SomethingOfMyOwn 'a value of my own'
            $session.AppName | Should -BeExactly 'ExtraValues'
        }

        It 'Opens against a process of the other architecture when allowed to' {
            # Refused by default, because a 32-bit PowerShell on a 64-bit machine sees a different
            # registry and a different Program Files, which is rarely what a deployment wanted.
            { Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'WowAllowed' -DeployMode Silent -AllowWowProcess -InformationAction SilentlyContinue } | Should -Not -Throw
        }
    }

    Context 'Callbacks' {
        It 'Runs the callbacks registered for either side of a session opening' {
            # PreOpen runs before the session exists and PostOpen once it does, which is what lets an
            # extension prepare something the session needs and then attach to the session itself.
            $script:Order = [System.Collections.Generic.List[System.String]]::new()
            function Test-PreOpenCallback
            {
                $script:Order.Add("pre:$(InModuleScope PSAppDeployToolkit { $ADT.Sessions.Count })")
            }
            function Test-PostOpenCallback
            {
                $script:Order.Add("post:$(InModuleScope PSAppDeployToolkit { $ADT.Sessions.Count })")
            }
            Add-ADTModuleCallback -Hookpoint PreOpen -Callback (Get-Command Test-PreOpenCallback)
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback (Get-Command Test-PostOpenCallback)
            try
            {
                $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'OpenCallbacks' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
                $script:Order | Should -Be @('pre:0', 'post:1')
            }
            finally
            {
                Clear-ADTModuleCallback -Hookpoint PreOpen
                Clear-ADTModuleCallback -Hookpoint PostOpen
            }
        }

        It 'Opens no session at all when a PreOpen callback throws' {
            # PreOpen is where an extension says the deployment cannot go ahead, so the session must not be
            # left half open with nothing to close it.
            function Test-FailingPreOpenCallback
            {
                throw 'The callback failed deliberately.'
            }
            Add-ADTModuleCallback -Hookpoint PreOpen -Callback (Get-Command Test-FailingPreOpenCallback)
            try
            {
                { Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'FailedPreOpen' -DeployMode Silent -InformationAction SilentlyContinue } | Should -Throw -ExpectedMessage 'The callback failed deliberately.'
                Test-ADTSessionActive | Should -BeFalse
            }
            finally
            {
                Clear-ADTModuleCallback -Hookpoint PreOpen
                while (Test-ADTSessionActive)
                {
                    Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
                }
            }
        }

        It 'Closes the session again when a PostOpen callback throws' {
            # The session is already open and registered by this point, so the failure has to unwind it as
            # well as be reported, or the deployment carries on holding a session nothing will close.
            function Test-FailingPostOpenCallback
            {
                throw 'The callback failed deliberately.'
            }
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback (Get-Command Test-FailingPostOpenCallback)
            try
            {
                Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'FailedPostOpen' -DeployMode Silent -InformationAction SilentlyContinue -ErrorVariable openErrors 2>$null

                # The error variable also collects the intermediate objects each record passes through on its
                # way out, and those carry no exception of their own, so the records are picked out first.
                $records = @($openErrors | & { process { if ($_ -is [System.Management.Automation.ErrorRecord]) { return $_ } } })
                $records.Exception.Message | Should -Contain 'The callback failed deliberately.'
                Test-ADTSessionActive | Should -BeFalse
            }
            finally
            {
                Clear-ADTModuleCallback -Hookpoint PostOpen
                while (Test-ADTSessionActive)
                {
                    Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
                }
            }
        }
        It 'Runs the callbacks registered for the start of a deployment' {
            # Extensions hook here to set themselves up once the session exists but before the deployment
            # gets going.
            $script:Started = 0
            function Test-OnStartCallback
            {
                $script:Started++
            }
            Add-ADTModuleCallback -Hookpoint OnStart -Callback (Get-Command Test-OnStartCallback)
            try
            {
                $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'StartCallback' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
                $script:Started | Should -Be 1
            }
            finally
            {
                Clear-ADTModuleCallback -Hookpoint OnStart
            }
        }
    }
    Context 'Input Validation' {
        It 'Rejects a script directory that does not exist' {
            { Open-Probe -Splat @{ ScriptDirectory = "$TestDrive\NoSuchDirectory" } } | Should -Throw
        }

        It 'Rejects an empty application name' {
            { Open-Probe -Splat @{ AppName = '' } } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Rejects a deploy mode it does not know' {
            { Open-Probe -Splat @{ DeployMode = 'NotAMode' } } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Rejects a blank script directory' {
            { Open-Probe -Splat @{ ScriptDirectory = '   ' } } | Should -Throw -ErrorId 'InvalidScriptDirectoryParameterValue,Open-ADTSession'
        }

        It 'Rejects a <Parameter> that is <Description>' -ForEach @(
            @{ Parameter = 'DirFiles'; Description = 'blank'; Value = '   ' }
            @{ Parameter = 'DirFiles'; Description = 'not there'; Value = 'NoSuchDirectory' }
            @{ Parameter = 'DirSupportFiles'; Description = 'blank'; Value = '   ' }
            @{ Parameter = 'DirSupportFiles'; Description = 'not there'; Value = 'NoSuchDirectory' }
        ) {
            # A deployment reads its content from these, so being pointed at nothing has to be reported
            # when the session opens rather than when something first tries to read from it.
            $value = if ($Value.Equals('NoSuchDirectory')) { "$TestDrive\NoSuchDirectory" } else { $Value }
            { Open-Probe -Splat @{ $Parameter = $value } } | Should -Throw -ErrorId "Invalid$($Parameter)ParameterValue,Open-ADTSession"
        }

        It 'Rejects a blank log name' {
            { Open-Probe -Splat @{ LogName = '   ' } } | Should -Throw -ErrorId 'InvalidLogNameParameterValue,Open-ADTSession'
        }

        It 'Rejects a log name the log viewer would not open' {
            # The name has to end in an extension the toolkit writes, so that what it produces is picked
            # up as a log rather than left as an unknown file.
            { Open-Probe -Splat @{ LogName = 'NoExtensionAtAll' } } | Should -Throw -ErrorId 'InvalidLogNameParameterValue,Open-ADTSession'
        }

        It 'Rejects a session class that is not one' {
            # The class is instantiated and handed back to the caller, so it has to be a deployment
            # session or nothing downstream will work.
            { Open-Probe -Splat @{ SessionClass = [System.String] } } | Should -Throw -ErrorId 'InvalidSessionClassParameterValue,Open-ADTSession'
        }

        It 'Rejects a null session class' {
            # Called directly rather than through the helper, so that the refusal is reported against the
            # function under test rather than against the helper doing the splatting.
            { Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'NullClass' -DeployMode Silent -SessionClass $null -InformationAction SilentlyContinue } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Open-ADTSession'
        }
    }

    Context 'Without being told whose scope it is opening in' {
        AfterEach {
            while (Test-ADTSessionActive)
            {
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            }
        }

        It 'Falls back to the calling scope' {
            # A deployment script calls this without naming a session state, and the environment variables
            # have to land somewhere for the rest of the script to read them.
            $session = Open-ADTSession -AppName 'NoSessionState' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
            $session | Should -BeOfType ([PSAppDeployToolkit.Foundation.DeploymentSession])
        }
    }
}
