BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Invoke-ADTClientServerOperation' {
    Context 'Rendering choice for an existing client' {
        It 'Rejects a changed choice before closing or invoking the client' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Test-ADTModuleInitialized { $true }
                Mock Get-ADTConfig { @{ UI = @{ ForceSoftwareRendering = $true } } }
                Mock Test-ADTClientServerActive { $true }
                Mock Get-ADTClientServerInstance { [pscustomobject]@{ IsRunning = $true; ForceSoftwareRendering = $false } }
                Mock Close-ADTClientServerInstance { }

                $user = [PSADT.AccountManagement.AccountUtilities]::CallerRunAsActiveUser
                { Invoke-ADTClientServerOperation -ProgressDialogOpen -User $user } | Should -Throw -ExpectedMessage '*UI.ForceSoftwareRendering differs from the running UI client*'
                Should -Invoke Close-ADTClientServerInstance -Times 0 -Exactly
            }
        }

        It 'Reuses a running client with the same rendering choice without closing or opening another' -ForEach @(
            @{ Choice = $false }
            @{ Choice = $true }
        ) {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Choice = $Choice } {
                $user = [PSADT.AccountManagement.AccountUtilities]::CallerRunAsActiveUser
                $originalVariable = Get-Variable -Name ClientServerInstance -Scope Script -ErrorAction Ignore
                $Script:RequestedRendering = $Choice
                $fake = [pscustomobject]@{ IsRunning = $true; ForceSoftwareRendering = $Choice; RunAsActiveUser = $user }
                $fake | Add-Member -MemberType ScriptMethod -Name ProgressDialogOpenAsync -Value { [System.Threading.Tasks.ValueTask[bool]]::new($true) }
                $fake | Add-Member -MemberType ScriptMethod -Name GetLogWriterException -Value { $null }
                $Script:ClientServerInstance = $fake
                Mock Test-ADTModuleInitialized { $true }
                Mock Get-ADTConfig { @{ UI = @{ ForceSoftwareRendering = $Script:RequestedRendering } } }
                Mock Test-ADTClientServerActive { $true }
                Mock Get-ADTClientServerInstance { $Script:ClientServerInstance }
                Mock Test-ADTSessionActive { $false }
                Mock Close-ADTClientServerInstance { throw 'A matching client must not close.' }
                Mock Add-ADTModuleCallback { throw 'A matching client must not register a new lifecycle callback.' }
                Mock Set-ADTClientServerFilePermissions { throw 'A matching client must not prepare a new launch.' }
                try
                {
                    { Invoke-ADTClientServerOperation -ProgressDialogOpen -User $user } | Should -Not -Throw
                    Should -Invoke Close-ADTClientServerInstance -Times 0 -Exactly
                    Should -Invoke Add-ADTModuleCallback -Times 0 -Exactly
                    Should -Invoke Set-ADTClientServerFilePermissions -Times 0 -Exactly
                    [object]::ReferenceEquals($Script:ClientServerInstance, $fake) | Should -BeTrue
                }
                finally
                {
                    if ($originalVariable)
                    {
                        $Script:ClientServerInstance = $originalVariable.Value
                    }
                    else
                    {
                        Remove-Variable -Name ClientServerInstance -Scope Script -ErrorAction Ignore
                    }
                }
            }
        }
    }

    Context 'Rendering choice for a new client' {
        It 'Passes the effective Boolean to the exact unlaunched constructor expression' -ForEach @(
            @{ Choice = $false }
            @{ Choice = $true }
        ) {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Choice = $Choice } {
                $Script:RequestedRendering = $Choice
                Mock Test-ADTModuleInitialized { $true }
                Mock Get-ADTConfig { @{ UI = @{ ForceSoftwareRendering = $Script:RequestedRendering } } }
                $ast = (Get-Command Invoke-ADTClientServerOperation).ScriptBlock.Ast
                $assignment = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -eq '$forceSoftwareRendering' }, $true))
                $constructor = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.InvokeMemberExpressionAst] -and $node.Extent.Text -match '^\[PSADT\.ClientServer\.ServerInstance\]::new\(' }, $true))
                $assignment.Count | Should -Be 1
                $constructor.Count | Should -Be 1

                # Run only these two nodes selected from the module's own AST, without launching the client.
                $assignmentBlock = [System.Management.Automation.ScriptBlock]::Create($assignment[0].Extent.Text)
                $constructorBlock = [System.Management.Automation.ScriptBlock]::Create($constructor[0].Extent.Text)
                $User = [PSADT.AccountManagement.AccountUtilities]::CallerRunAsActiveUser
                . $assignmentBlock
                $instance = & $constructorBlock
                try
                {
                    $instance.ForceSoftwareRendering | Should -Be $Choice
                    $instance.RunAsActiveUser | Should -Be $User
                    $instance.IsRunning | Should -BeFalse
                }
                finally
                {
                    $instance.DisposeAsync().AsTask().GetAwaiter().GetResult()
                }
            }
        }
    }

    Context 'Rendering choice for a standalone launch' {
        It 'Serializes the effective Boolean before any client launch' -ForEach @(
            @{ Choice = $false; Serialized = 'False' }
            @{ Choice = $true; Serialized = 'True' }
        ) {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Choice = $Choice; Serialized = $Serialized } {
                $Script:RequestedRendering = $Choice
                $Script:CapturedArguments = $null
                Mock Test-ADTModuleInitialized { $true }
                Mock Get-ADTConfig { @{ UI = @{ ForceSoftwareRendering = $Script:RequestedRendering } } }
                Mock Test-ADTClientServerActive { $false }
                Mock Test-ADTSessionActive { $false }
                Mock Set-ADTClientServerFilePermissions { }
                Mock Set-ADTRegistryKey {
                    $Script:CapturedArguments = $Value
                    throw 'CapturedBeforeLaunch'
                }

                $user = [PSADT.AccountManagement.AccountUtilities]::CallerRunAsActiveUser
                { Invoke-ADTClientServerOperation -GetForegroundWindowProcessId -User $user } | Should -Throw -ExpectedMessage '*CapturedBeforeLaunch*'
                Should -Invoke Set-ADTRegistryKey -Times 1 -Exactly
                $Script:CapturedArguments | Should -Not -BeNullOrEmpty
                $type = [System.Collections.ObjectModel.ReadOnlyDictionary[string, string]]
                $arguments = [PSADT.ClientServer.DataSerialization]::DeserializeFromString($Script:CapturedArguments, $type)
                $arguments['ForceSoftwareRendering'] | Should -BeExactly $Serialized
            }
        }

        It 'Serializes the pre-initialization default or policy choice without reading initialized config' -ForEach @(
            @{ Choice = $false; Serialized = 'False' }
            @{ Choice = $true; Serialized = 'True' }
        ) {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Choice = $Choice; Serialized = $Serialized } {
                $Script:RequestedRendering = $Choice
                $Script:CapturedArguments = $null
                Mock Test-ADTModuleInitialized { $false }
                Mock Get-ADTDefaultConfig { @{ UI = @{ ForceSoftwareRendering = $Script:RequestedRendering } } }
                Mock Get-ADTConfig { throw 'Initialized config must not be read before initialization.' }
                Mock Test-ADTClientServerActive { $false }
                Mock Test-ADTSessionActive { $false }
                Mock Set-ADTClientServerFilePermissions { }
                Mock Set-ADTRegistryKey {
                    $Script:CapturedArguments = $Value
                    throw 'CapturedBeforeLaunch'
                }

                $user = [PSADT.AccountManagement.AccountUtilities]::CallerRunAsActiveUser
                { Invoke-ADTClientServerOperation -GetForegroundWindowProcessId -User $user } | Should -Throw -ExpectedMessage '*CapturedBeforeLaunch*'
                Should -Invoke Get-ADTDefaultConfig -Times 1 -Exactly
                Should -Invoke Get-ADTConfig -Times 0 -Exactly
                Should -Invoke Set-ADTRegistryKey -Times 1 -Exactly
                $type = [System.Collections.ObjectModel.ReadOnlyDictionary[string, string]]
                $arguments = [PSADT.ClientServer.DataSerialization]::DeserializeFromString($Script:CapturedArguments, $type)
                $arguments['ForceSoftwareRendering'] | Should -BeExactly $Serialized
            }
        }
    }

    # Contract only. Every operation this offers reaches the client running in the logged-on user's
    # session, and the ones worth asserting against are covered through the functions that call it:
    # Test-ADTNotifyIconOpen, Test-ADTInstallationProgressOpen and Start-ADTProcessAsUser among them.
    Context 'Input Validation' {
        It 'Requires an operation to perform' {
            # Each operation is its own switch in its own parameter set, so a call naming none of them
            # cannot resolve to anything.
            Test-ADTParameterSetSatisfied -Command (InModuleScope PSAppDeployToolkit { Get-Command Invoke-ADTClientServerOperation }) | Should -BeFalse
        }

        It 'Refuses two operations at once' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Invoke-ADTClientServerOperation -ProgressDialogOpen -NotifyIconOpen -User (Get-ADTClientServerUser) } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }

        It 'Requires a user to perform it as' {
            # The client belongs to a logged-on session, so there is nowhere to run an operation without
            # naming whose session it is.
            Test-ADTParameterSetSatisfied -Command (InModuleScope PSAppDeployToolkit { Get-Command Invoke-ADTClientServerOperation }) -Parameter ProgressDialogOpen | Should -BeFalse
        }

        It 'Refuses something that is not a user' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Invoke-ADTClientServerOperation -ProgressDialogOpen -User 'not a user' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }

        It 'Requires a silent restart to be asked for without waiting' {
            # SilentRestart has no counterpart on ServerInstance, so it only works via the argv route that
            # -NoWait selects. Without it the call reaches a SilentRestartAsync() that does not exist,
            # which being mandatory turns into a binding failure instead.
            Test-ADTParameterSetSatisfied -Command (InModuleScope PSAppDeployToolkit { Get-Command Invoke-ADTClientServerOperation }) -Parameter SilentRestart, User, Options | Should -BeFalse
            Test-ADTParameterSetSatisfied -Command (InModuleScope PSAppDeployToolkit { Get-Command Invoke-ADTClientServerOperation }) -Parameter SilentRestart, User, Options, NoWait | Should -BeTrue
        }

        It 'Requires a silent restart to say what it is restarting for' {
            # The countdown, the reason and the force-close switch all travel as one serialized object
            # now, so a call without it reaches the client with nothing to act on.
            Test-ADTParameterSetSatisfied -Command (InModuleScope PSAppDeployToolkit { Get-Command Invoke-ADTClientServerOperation }) -Parameter SilentRestart, User, NoWait | Should -BeFalse
        }
    }
}
