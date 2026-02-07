#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTClientServerOperation
#
#-----------------------------------------------------------------------------

function Private:Invoke-ADTClientServerOperation
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'InitCloseAppsDialog')]
        [System.Management.Automation.SwitchParameter]$InitCloseAppsDialog,

        [Parameter(Mandatory = $true, ParameterSetName = 'PromptToCloseApps')]
        [System.Management.Automation.SwitchParameter]$PromptToCloseApps,

        [Parameter(Mandatory = $true, ParameterSetName = 'ProgressDialogOpen')]
        [System.Management.Automation.SwitchParameter]$ProgressDialogOpen,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowProgressDialog')]
        [System.Management.Automation.SwitchParameter]$ShowProgressDialog,

        [Parameter(Mandatory = $true, ParameterSetName = 'UpdateProgressDialog')]
        [System.Management.Automation.SwitchParameter]$UpdateProgressDialog,

        [Parameter(Mandatory = $true, ParameterSetName = 'CloseProgressDialog')]
        [System.Management.Automation.SwitchParameter]$CloseProgressDialog,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [System.Management.Automation.SwitchParameter]$ShowModalDialog,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowBalloonTip')]
        [System.Management.Automation.SwitchParameter]$ShowBalloonTip,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetProcessWindowInfo')]
        [System.Management.Automation.SwitchParameter]$GetProcessWindowInfo,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetUserNotificationState')]
        [System.Management.Automation.SwitchParameter]$GetUserNotificationState,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetForegroundWindowProcessId')]
        [System.Management.Automation.SwitchParameter]$GetForegroundWindowProcessId,

        [Parameter(Mandatory = $true, ParameterSetName = 'RefreshDesktopAndEnvironmentVariables')]
        [System.Management.Automation.SwitchParameter]$RefreshDesktopAndEnvironmentVariables,

        [Parameter(Mandatory = $true, ParameterSetName = 'MinimizeAllWindows')]
        [System.Management.Automation.SwitchParameter]$MinimizeAllWindows,

        [Parameter(Mandatory = $true, ParameterSetName = 'RestoreAllWindows')]
        [System.Management.Automation.SwitchParameter]$RestoreAllWindows,

        [Parameter(Mandatory = $true, ParameterSetName = 'SendKeys')]
        [System.Management.Automation.SwitchParameter]$SendKeys,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetEnvironmentVariable')]
        [System.Management.Automation.SwitchParameter]$GetEnvironmentVariable,

        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [System.Management.Automation.SwitchParameter]$SetEnvironmentVariable,

        [Parameter(Mandatory = $true, ParameterSetName = 'RemoveEnvironmentVariable')]
        [System.Management.Automation.SwitchParameter]$RemoveEnvironmentVariable,

        [Parameter(Mandatory = $true, ParameterSetName = 'GroupPolicyUpdate')]
        [System.Management.Automation.SwitchParameter]$GroupPolicyUpdate,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Foundation.RunAsActiveUser]$User,

        [Parameter(Mandatory = $false, ParameterSetName = 'InitCloseAppsDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.ProcessManagement.ProcessDefinition[]]$CloseProcesses,

        [Parameter(Mandatory = $true, ParameterSetName = 'PromptToCloseApps')]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$PromptToCloseTimeout,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.DialogType]$DialogType,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowProgressDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.DialogStyle]$DialogStyle,

        [Parameter(Mandatory = $false, ParameterSetName = 'UpdateProgressDialog')]
        [ValidateNotNullOrEmpty()]
        [System.String]$ProgressMessage = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, ParameterSetName = 'UpdateProgressDialog')]
        [ValidateNotNullOrEmpty()]
        [System.String]$ProgressDetailMessage = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false, ParameterSetName = 'UpdateProgressDialog')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.Double]]$ProgressPercentage,

        [Parameter(Mandatory = $false, ParameterSetName = 'UpdateProgressDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.DialogMessageAlignment]$MessageAlignment,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetEnvironmentVariable')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RemoveEnvironmentVariable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Variable,

        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Value,

        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [System.Management.Automation.SwitchParameter]$Append,

        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [System.Management.Automation.SwitchParameter]$Remove,

        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [System.Management.Automation.SwitchParameter]$Expandable,

        [Parameter(Mandatory = $true, ParameterSetName = 'GroupPolicyUpdate')]
        [System.Management.Automation.SwitchParameter]$Force,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowProgressDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowBalloonTip')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetProcessWindowInfo')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SendKeys')]
        [ValidateNotNullOrEmpty()]
        [System.Object]$Options,

        [Parameter(Mandatory = $false, ParameterSetName = 'ShowModalDialog')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ShowBalloonTip')]
        [Parameter(Mandatory = $false, ParameterSetName = 'GroupPolicyUpdate')]
        [System.Management.Automation.SwitchParameter]$NoWait
    )

    # Internal worker function to extract client/server client process result from the exception.
    function Get-ADTClientServerClientProcessResult
    {
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Management.Automation.ErrorRecord]$ErrorRecord
        )

        # Return early if we don't have an InnerException.
        if (!($innerException = $ErrorRecord.Exception | Select-Object -ExpandProperty InnerException -ErrorAction Ignore))
        {
            return
        }

        # Return early if we don't have a ClientProcess.
        if (!($clientProcess = $innerException | Select-Object -ExpandProperty ClientProcess -ErrorAction Ignore))
        {
            return
        }

        # Return the client process's result to the caller.
        if ($clientResult = $clientProcess.Task.GetAwaiter().GetResult())
        {
            return $clientResult
        }
    }

    # If the client/server process is instantiated but no longer running, clean up before continuing.
    if ($Script:ADT.ClientServerProcess -and !$Script:ADT.ClientServerProcess.IsRunning)
    {
        Write-ADTLogEntry -Message 'Existing client/server process closed outside of our control.'
        Close-ADTClientServerProcess
    }

    # Ensure the permissions are correct on all files before proceeding.
    if (!$Script:ADT.ClientServerProcess)
    {
        Set-ADTClientServerProcessPermissions -User $User
    }

    # Go into client/server mode if a session is active and we're not asked to wait.
    if (($PSCmdlet.ParameterSetName -match '^(InitCloseAppsDialog|PromptToCloseApps|ProgressDialogOpen|ShowProgressDialog|UpdateProgressDialog|CloseProgressDialog|MinimizeAllWindows|RestoreAllWindows)$') -or
        [PSADT.UserInterface.DialogType]::CloseAppsDialog.Equals($DialogType) -or
        ((Test-ADTSessionActive) -and $User.Equals((Get-ADTEnvironmentTable).RunAsActiveUser) -and !$NoWait) -or
        ($Script:ADT.ClientServerProcess -and $Script:ADT.ClientServerProcess.RunAsActiveUser.Equals($User) -and !$NoWait))
    {
        # Instantiate a new ClientServerProcess object if one's not already present.
        if (!$Script:ADT.ClientServerProcess)
        {
            # No point proceeding further for this operation.
            if ($PSCmdlet.ParameterSetName.Equals('ProgressDialogOpen'))
            {
                return $false
            }
            if ($PSCmdlet.ParameterSetName.Equals('CloseProgressDialog'))
            {
                return
            }

            # Instantiate a new ClientServerProcess object as required, then add the necessary callback.
            Write-ADTLogEntry -Message 'Instantiating user client/server process.'
            $Script:ADT.ClientServerProcess = [PSADT.ClientServer.ServerInstance]::new($User)
            try
            {
                $Script:ADT.ClientServerProcess.Open()
            }
            catch
            {
                # Construct an ErrorRecord using an exception from the client/server process if possible.
                if ($result = Get-ADTClientServerClientProcessResult -ErrorRecord $_)
                {
                    $naerParams = @{
                        Exception = if ($result.StdErr.Count)
                        {
                            [System.ApplicationException]::new("Failed to open the instantiated client/server process.", [PSADT.ClientServer.DataSerialization]::DeserializeFromString($return.StdErr[0], [System.Exception]))
                        }
                        else
                        {
                            [System.ApplicationException]::new("Failed to open the instantiated client/server process.$(if (!$result.ExitCode.Equals([PSADT.ProcessManagement.ProcessManager]::TimeoutExitCode) -and !$_.Exception.InnerException.Message.Contains($result.ExitCode)) { " Exit Code: [$($result.ExitCode)]." })$(if ($result.StdOut) { " Console Output: [$([System.String]::Join("`n", $result.StdOut))]" })", $_.Exception.InnerException)
                        }
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'ClientServerProcessOpenFailure'
                        TargetObject = $result
                    }
                    $Script:ADT.ClientServerProcess.Dispose()
                    $Script:ADT.ClientServerProcess = $null
                    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                }
                else
                {
                    $Script:ADT.ClientServerProcess.Dispose()
                    $Script:ADT.ClientServerProcess = $null
                    $PSCmdlet.ThrowTerminatingError($_)
                }
            }

            # Ensure we properly close the client/server process upon the closure of the last active session.
            Add-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.'Close-ADTClientServerProcess'
        }

        # Invoke the right method depending on the mode.
        try
        {
            if ([PSADT.UserInterface.DialogType]::DialogBox.Equals($DialogType))
            {
                $result = $Script:ADT.ClientServerProcess.ShowDialogBox($Options)
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('ShowModalDialog'))
            {
                $result = $Script:ADT.ClientServerProcess."Show$($DialogType)"($DialogStyle, $Options)
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('InitCloseAppsDialog'))
            {
                $result = $Script:ADT.ClientServerProcess.InitCloseAppsDialog($CloseProcesses)
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('PromptToCloseApps'))
            {
                $result = $Script:ADT.ClientServerProcess.PromptToCloseApps($PromptToCloseTimeout)
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('ShowProgressDialog'))
            {
                $result = $Script:ADT.ClientServerProcess.ShowProgressDialog($DialogStyle, $Options)
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('UpdateProgressDialog'))
            {
                $result = $Script:ADT.ClientServerProcess.UpdateProgressDialog($ProgressMessage, $ProgressDetailMessage, $ProgressPercentage, $MessageAlignment)
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('GetEnvironmentVariable') -or $PSCmdlet.ParameterSetName.Equals('RemoveEnvironmentVariable'))
            {
                $result = $Script:ADT.ClientServerProcess.($PSCmdlet.ParameterSetName)($Variable)
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('SetEnvironmentVariable'))
            {
                $result = $Script:ADT.ClientServerProcess.SetEnvironmentVariable($Variable, $Value, !!$Expandable, !!$Append, !!$Remove)
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('GroupPolicyUpdate'))
            {
                $result = $Script:ADT.ClientServerProcess.GroupPolicyUpdate(!!$Force)
            }
            elseif ($PSBoundParameters.ContainsKey('Options'))
            {
                $result = $Script:ADT.ClientServerProcess.($PSCmdlet.ParameterSetName)($Options)
            }
            else
            {
                $result = $Script:ADT.ClientServerProcess.($PSCmdlet.ParameterSetName)()
            }

            # If the log writer gave up the ghost, throw its exception.
            if ($loggingException = $Script:ADT.ClientServerProcess.GetLogWriterException())
            {
                $naerParams = @{
                    Exception = [System.ApplicationException]::new("The log writer failed and was unable to continue execution.", $loggingException)
                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                    ErrorId = 'ClientServerProcessLoggingFailure'
                    TargetObject = $loggingException
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
        }
        catch
        {
            # Construct an ErrorRecord using an exception from the client/server process if possible.
            if ($result = Get-ADTClientServerClientProcessResult -ErrorRecord $_)
            {
                $naerParams = @{
                    Exception = if ($result.StdErr.Count)
                    {
                        [System.ApplicationException]::new("Failed to invoke the requested client/server command.", [PSADT.ClientServer.DataSerialization]::DeserializeFromString($result.StdErr[0], [System.Exception]))
                    }
                    else
                    {
                        [System.ApplicationException]::new("Failed to invoke the requested client/server command.$(if (!$result.ExitCode.Equals([PSADT.ProcessManagement.ProcessManager]::TimeoutExitCode) -and !$_.Exception.InnerException.Message.Contains($result.ExitCode)) { " Exit Code: [$($result.ExitCode)]." })$(if ($result.StdOut) { " Console Output: [$([System.String]::Join("`n", $result.StdOut))]" })", $_.Exception.InnerException)
                    }
                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                    ErrorId = 'ClientServerProcessCommandFailure'
                    TargetObject = $result
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
            else
            {
                $PSCmdlet.ThrowTerminatingError($_)
            }
        }
        finally
        {
            if (($result -is [PSADT.ProcessManagement.ProcessResult]) -and $result.LaunchInfo.FilePath.EndsWith('\PSADT.ClientServer.Client.exe'))
            {
                Close-ADTClientServerProcess
            }
        }
    }
    else
    {
        # Get the expected data return type for the given parameter set. This
        # way we can throw before doing anything if there's a setup issue.
        $type = switch ($PSCmdlet.ParameterSetName)
        {
            ShowModalDialog
            {
                switch ($DialogType)
                {
                    CloseAppsDialog
                    {
                        [PSADT.UserInterface.DialogResults.CloseAppsDialogResult]
                        break
                    }
                    DialogBox
                    {
                        [PSADT.UserInterface.DialogResults.DialogBoxResult]
                        break
                    }
                    HelpConsole
                    {
                        [System.Int32]
                        break
                    }
                    InputDialog
                    {
                        [PSADT.UserInterface.DialogResults.InputDialogResult]
                        break
                    }
                    CustomDialog
                    {
                        [System.String]
                        break
                    }
                    RestartDialog
                    {
                        [System.String]
                        break
                    }
                    default
                    {
                        $naerParams = @{
                            Exception = [System.InvalidOperationException]::new("The requested dialog type [$DialogType] is not supported in standalone mode.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                            ErrorId = "$($PSCmdlet.ParameterSetName)Error"
                            TargetObject = $PSBoundParameters
                            RecommendedAction = "Please report this issue to the PSAppDeployToolkit development team."
                        }
                        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                    }
                }
            }
            ShowBalloonTip
            {
                [System.Boolean]
                break
            }
            GetProcessWindowInfo
            {
                [System.Collections.ObjectModel.ReadOnlyCollection[PSADT.WindowManagement.WindowInfo]]
                break
            }
            GetUserNotificationState
            {
                [PSADT.LibraryInterfaces.QUERY_USER_NOTIFICATION_STATE]
                break
            }
            GetForegroundWindowProcessId
            {
                [System.UInt32]
                break
            }
            RefreshDesktopAndEnvironmentVariables
            {
                [System.Boolean]
                break
            }
            MinimizeAllWindows
            {
                [System.Boolean]
                break
            }
            RestoreAllWindows
            {
                [System.Boolean]
                break
            }
            SendKeys
            {
                [System.Boolean]
                break
            }
            GetEnvironmentVariable
            {
                [System.String]
                break
            }
            SetEnvironmentVariable
            {
                [System.Boolean]
                break
            }
            RemoveEnvironmentVariable
            {
                [System.Boolean]
                break
            }
            GroupPolicyUpdate
            {
                [PSADT.ProcessManagement.ProcessResult]
                break
            }
            default
            {
                $naerParams = @{
                    Exception = [System.InvalidOperationException]::new("The requested client/server operation [$($PSCmdlet.ParameterSetName)] is not supported.")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                    ErrorId = "$($PSCmdlet.ParameterSetName)Error"
                    TargetObject = $PSBoundParameters
                    RecommendedAction = "Please report this issue to the PSAppDeployToolkit development team."
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
        }

        # Sanitise $PSBoundParameters, we'll use it to generate our arguments.
        $null = $PSBoundParameters.Remove($PSCmdlet.ParameterSetName)
        $null = $PSBoundParameters.Remove('NoWait')
        $null = $PSBoundParameters.Remove('User')
        if ($PSBoundParameters.ContainsKey('Options'))
        {
            $PSBoundParameters.Options = [PSADT.ClientServer.DataSerialization]::SerializeToString($Options)
        }

        # Build out parameters to store in the user's registry. When using Base64 logos, the path length can easily by exceeded.
        $csoArguments = if ($PSBoundParameters.Count -gt 0)
        {
            # Copy everything into a new dictionary as Newtonsoft won't handle a PSBoundParametersDictionary properly.
            $csArgsDictionary = [System.Collections.Generic.Dictionary[System.String, System.String]]::new()
            $PSBoundParameters.GetEnumerator() | & {
                process
                {
                    $csArgsDictionary.Add($_.Key, $_.Value)
                }
            }
            Set-ADTRegistryKey -LiteralPath ([PSADT.ClientServer.ClientServerUtilities]::UserRegistryPath) -Name ($csArgsRegValue = Get-Random) -Value ([PSADT.ClientServer.DataSerialization]::SerializeToString([System.Collections.ObjectModel.ReadOnlyDictionary[System.String, System.String]]$csArgsDictionary)) -SID $User.SID -InformationAction SilentlyContinue
            @{
                ArgumentsDictionary = "$([PSADT.ClientServer.ClientServerUtilities]::UserRegistryPath)\$csArgsRegValue"
                RemoveArgumentsDictionaryStorage = $true
            }
        }

        # Set up the parameters for Start-ADTProcessAsUser.
        $sapauParams = @{
            RunAsActiveUser = $User
            UseHighestAvailableToken = $true
            DenyUserTermination = $true
            ArgumentList = $("/$($PSCmdlet.ParameterSetName)"; if ($csoArguments) { $csoArguments.GetEnumerator() | & { process { "-$($_.Key)"; $_.Value } } })
            WorkingDirectory = [System.Environment]::SystemDirectory
            MsiExecWaitTime = 1
            CreateNoWindow = $true
            InformationAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
            PassThru = $true
        }

        # Farm this out to a new process.
        $return = try
        {
            # For -NoWait operations, we want to ensure the operation was successful before continuing.
            # Some platforms clean up the local cache before a dialog can appears, causing breaks.
            if ($NoWait)
            {
                # Remove any previous success flags before starting the process.
                $arkParams = @{
                    InformationAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
                    WarningAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
                    LiteralPath = [PSADT.ClientServer.ClientServerUtilities]::UserRegistryPath
                    Name = [PSADT.ClientServer.ClientServerUtilities]::OperationSuccessRegistryValueName
                    SID = $User.SID
                }
                Remove-ADTRegistryKey @arkParams
                $sapResult = Start-ADTProcess @sapauParams -FilePath "$Script:PSScriptRoot\lib\PSADT.ClientServer.Client.Launcher.exe" -NoWait;

                # Wait for the success flag. When found, remove it to clean up house and break to continue.
                $noWaitTimer = [System.Diagnostics.Stopwatch]::StartNew()
                while ($true)
                {
                    if ((Get-ADTRegistryKey @arkParams) -eq 1)
                    {
                        Remove-ADTRegistryKey @arkParams
                        break
                    }
                    if ($noWaitTimer.ElapsedMilliseconds -ge 15000)
                    {
                        $naerParams = @{
                            Exception = [System.TimeoutException]::new("Timed out waiting for the -NoWait client/server operation to report success.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                            ErrorId = 'ClientServerNoWaitTimeoutExceeded'
                            TargetObject = $sapResult
                            RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
                        }
                        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                    }
                    [System.Threading.Thread]::Sleep(1000)
                }
                return
            }
            else
            {
                Start-ADTProcess @sapauParams -FilePath "$Script:PSScriptRoot\lib\PSADT.ClientServer.Client.exe" -IgnoreExitCodes *
            }
        }
        catch [System.Runtime.InteropServices.ExternalException]
        {
            $_.TargetObject
        }

        # Confirm we were successful in our operation.
        if ($return -isnot [PSADT.ProcessManagement.ProcessResult])
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The client/server process failed to start.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId = 'ClientServerInvocationFailure'
                TargetObject = $return
                RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
        if ($return.StdErr.Count -ne 0)
        {
            $naerParams = @{
                Exception = [System.ApplicationException]::new("Failed to invoke the requested client/server command.", [PSADT.ClientServer.DataSerialization]::DeserializeFromString($return.StdErr[0], [System.Exception]))
                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId = 'ClientServerResultError'
                TargetObject = $return
                RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
        if ($return.ExitCode -ne 0)
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The client/server process failed with exit code [$($return.ExitCode)]$(if ([System.Enum]::IsDefined([PSADT.ClientServer.ClientExitCode], $return.ExitCode)) { " ($([PSADT.ClientServer.ClientExitCode]$return.ExitCode))" }).")
                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId = 'ClientServerRuntimeFailure'
                TargetObject = $return
                RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
        if ($return.StdOut.Count -eq 0)
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The client/server process returned no result.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId = 'ClientServerResultNull'
                TargetObject = $return
                RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
        if ($return.StdOut.Count -gt 1)
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The client/server process returned an invalid result.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId = 'ClientServerResultInvalid'
                TargetObject = $return
                RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }

        # Deserialise the result for returning to the caller.
        $result = [PSADT.ClientServer.DataSerialization]::DeserializeFromString($return.StdOut[0], $type)
    }

    # Test that the received result is valid and expected.
    if (($null -eq $result) -or (($result -is [System.Boolean]) -and !$result.Equals($true) -and !$PSCmdlet.ParameterSetName.Equals('ProgressDialogOpen')))
    {
        $naerParams = @{
            Exception = [System.ApplicationException]::new("Failed to perform the $($PSCmdlet.ParameterSetName) operation for an unknown reason.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = "$($PSCmdlet.ParameterSetName)Error"
            TargetObject = $result
            RecommendedAction = "Please report this issue to the PSAppDeployToolkit development team."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

    # Only write a result out for modes where we're expecting a result.
    if (![System.String]::IsNullOrWhiteSpace(($result | Out-String)) -and ![PSADT.ClientServer.ServerInstance]::SuccessSentinel.Equals($result) -and ($PSCmdlet.ParameterSetName -match '^(InitCloseAppsDialog|ProgressDialogOpen|ShowModalDialog|GetProcessWindowInfo|GetUserNotificationState|GetForegroundWindowProcessId|GetEnvironmentVariable|GroupPolicyUpdate)$'))
    {
        return $result
    }
}
