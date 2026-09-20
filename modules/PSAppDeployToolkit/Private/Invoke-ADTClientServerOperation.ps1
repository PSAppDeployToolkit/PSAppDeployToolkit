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

        [Parameter(Mandatory = $true, ParameterSetName = 'NotifyIconOpen')]
        [System.Management.Automation.SwitchParameter]$NotifyIconOpen,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowNotifyIcon')]
        [System.Management.Automation.SwitchParameter]$ShowNotifyIcon,

        [Parameter(Mandatory = $true, ParameterSetName = 'UpdateNotifyIcon')]
        [System.Management.Automation.SwitchParameter]$UpdateNotifyIcon,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowBalloonTip')]
        [System.Management.Automation.SwitchParameter]$ShowBalloonTip,

        [Parameter(Mandatory = $true, ParameterSetName = 'CloseNotifyIcon')]
        [System.Management.Automation.SwitchParameter]$CloseNotifyIcon,

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

        [Parameter(Mandatory = $true, ParameterSetName = 'ShellExecuteProcess')]
        [System.Management.Automation.SwitchParameter]$ShellExecuteProcess,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetUserFocusModeState')]
        [System.Management.Automation.SwitchParameter]$GetUserFocusModeState,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetUserToastNotificationMode')]
        [System.Management.Automation.SwitchParameter]$GetUserToastNotificationMode,

        [Parameter(Mandatory = $true, ParameterSetName = 'SilentRestart')]
        [System.Management.Automation.SwitchParameter]$SilentRestart,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Foundation.RunAsActiveUser]$User,

        [Parameter(Mandatory = $false, ParameterSetName = 'InitCloseAppsDialog')]
        [ValidateNotNullOrEmpty()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [PSADT.ProcessManagement.ProcessDefinition[]]$CloseProcesses,

        [Parameter(Mandatory = $true, ParameterSetName = 'PromptToCloseApps')]
        [PSAppDeployToolkit.Attributes.ValidateGreaterThanZero()]
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
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$ProgressMessage,

        [Parameter(Mandatory = $false, ParameterSetName = 'UpdateProgressDialog')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$ProgressDetailMessage,

        [Parameter(Mandatory = $false, ParameterSetName = 'UpdateProgressDialog')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.Double]]$ProgressPercentage,

        [Parameter(Mandatory = $false, ParameterSetName = 'UpdateProgressDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.DialogMessageAlignment]$MessageAlignment,

        [Parameter(Mandatory = $false, ParameterSetName = 'UpdateNotifyIcon')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$MessageText,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetEnvironmentVariable')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RemoveEnvironmentVariable')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Variable,

        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Value,

        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [System.Management.Automation.SwitchParameter]$Append,

        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [System.Management.Automation.SwitchParameter]$Remove,

        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [System.Management.Automation.SwitchParameter]$Expandable,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowProgressDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowNotifyIcon')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowBalloonTip')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetProcessWindowInfo')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShellExecuteProcess')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SendKeys')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SilentRestart')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.Object]$Options,

        [Parameter(Mandatory = $false, ParameterSetName = 'ShowModalDialog')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ShowBalloonTip')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ShellExecuteProcess')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SilentRestart')]
        [System.Management.Automation.SwitchParameter]$NoWait
    )

    # Internal worker function to extract client/server client process result from the exception.
    function Get-ADTClientServerProcessResult
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
        if ($clientResult = $clientProcess.ConfigureAwait($false).GetAwaiter().GetResult())
        {
            return $clientResult
        }
    }

    # If the client/server process is instantiated but no longer running, clean up before continuing.
    if ((Test-ADTClientServerActive) -and !(Get-ADTClientServerInstance).IsRunning)
    {
        Write-ADTLogEntry -Message 'Existing client/server process closed outside of our control.'
        Close-ADTClientServerInstance
    }

    # Ensure the permissions are correct on all files before proceeding.
    if (!(Test-ADTClientServerActive))
    {
        Set-ADTClientServerFilePermissions -User $User
    }

    # Establish conditions for whether to go the client/server route, or standalone.
    $mustUseClientServer = ($PSCmdlet.ParameterSetName -match '^(InitCloseAppsDialog|PromptToCloseApps|ProgressDialogOpen|ShowProgressDialog|UpdateProgressDialog|CloseProgressDialog|NotifyIconOpen|ShowNotifyIcon|UpdateNotifyIcon|CloseNotifyIcon|MinimizeAllWindows|RestoreAllWindows)$') -or [PSADT.UserInterface.DialogType]::CloseAppsDialog.Equals($DialogType) -or [PSADT.UserInterface.DialogType]::SecureInputDialog.Equals($DialogType)
    $canUseClientServer = !$PSCmdlet.ParameterSetName.Equals('ShellExecuteProcess') -and !$NoWait -and (((Test-ADTSessionActive) -and $User.Equals((Get-ADTEnvironmentTable).RunAsActiveUser)) -or ((Test-ADTClientServerActive) -and (Get-ADTClientServerInstance).RunAsActiveUser.Equals($User)))

    # Go into client/server mode if a session is active and we're not asked to wait.
    if ($mustUseClientServer -or $canUseClientServer)
    {
        # Instantiate a new ServerInstance object if one's not already present.
        $clientServerProcessResult = $null
        if (!(Test-ADTClientServerActive))
        {
            # No point proceeding further for this operation.
            if ($PSCmdlet.ParameterSetName.Equals('ProgressDialogOpen'))
            {
                return $false
            }
            if ($PSCmdlet.ParameterSetName.Equals('NotifyIconOpen'))
            {
                return $false
            }
            if ($PSCmdlet.ParameterSetName.Equals('CloseProgressDialog'))
            {
                return
            }

            # Instantiate a new ServerInstance object as required, then add the necessary callback.
            Write-ADTLogEntry -Message 'Instantiating user client/server process.'
            $clientServerInstance = $Script:ClientServerInstance = [PSADT.ClientServer.ServerInstance]::new($User)
            try
            {
                $null = $clientServerInstance.OpenAsync().ConfigureAwait($false).GetAwaiter().GetResult()
            }
            catch
            {
                # Construct an ErrorRecord using an exception from the client/server process if possible.
                if ($clientServerProcessResult = Get-ADTClientServerProcessResult -ErrorRecord $_)
                {
                    try
                    {
                        $naerParams = @{
                            Exception = if ($clientServerProcessResult.StdErr.Count)
                            {
                                [System.ApplicationException]::new("Failed to open the instantiated client/server process.", [PSADT.ClientServer.DataSerialization]::DeserializeExceptionFromStdErr($clientServerProcessResult))
                            }
                            else
                            {
                                [System.ApplicationException]::new("Failed to open the instantiated client/server process.$(if (!$clientServerProcessResult.ExitCode.Equals([PSADT.ProcessManagement.ProcessManager]::TimeoutExitCode) -and !$_.Exception.InnerException.Message.Contains($clientServerProcessResult.ExitCode)) { " Exit Code: [$($clientServerProcessResult.ExitCode)]." })$(if ($clientServerProcessResult.StdOut) { " Console Output: [$([System.String]::Join([System.Environment]::NewLine, $clientServerProcessResult.StdOut))]" })", $_.Exception.InnerException)
                            }
                            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                            ErrorId = 'ClientServerInstanceOpenFailure'
                            TargetObject = $clientServerProcessResult
                        }
                        Close-ADTClientServerInstance -InformationAction SilentlyContinue
                        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                    }
                    finally
                    {
                        $clientServerProcessResult.Dispose()
                    }
                }
                else
                {
                    Close-ADTClientServerInstance -InformationAction SilentlyContinue
                    $PSCmdlet.ThrowTerminatingError($_)
                }
            }

            # Ensure we properly close the client/server process upon the closure of the last active session.
            Add-ADTModuleCallback -Hookpoint OnFinish -Callback (Get-ADTCommand -Name Close-ADTClientServerInstance)
        }

        # Invoke the right method depending on the mode.
        try
        {
            if ([PSADT.UserInterface.DialogType]::DialogBox.Equals($DialogType))
            {
                $result = $clientServerInstance.ShowDialogBoxAsync($Options).ConfigureAwait($false).GetAwaiter().GetResult()
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('ShowModalDialog'))
            {
                $result = $clientServerInstance."Show$($DialogType)Async"($DialogStyle, $Options).ConfigureAwait($false).GetAwaiter().GetResult()
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('InitCloseAppsDialog'))
            {
                $result = $clientServerInstance.InitCloseAppsDialogAsync($CloseProcesses).ConfigureAwait($false).GetAwaiter().GetResult()
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('PromptToCloseApps'))
            {
                $result = $clientServerInstance.PromptToCloseAppsAsync($PromptToCloseTimeout).ConfigureAwait($false).GetAwaiter().GetResult()
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('ShowProgressDialog'))
            {
                $result = $clientServerInstance.ShowProgressDialogAsync($DialogStyle, $Options).ConfigureAwait($false).GetAwaiter().GetResult()
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('UpdateProgressDialog'))
            {
                $result = $clientServerInstance.UpdateProgressDialogAsync($ProgressMessage, $ProgressDetailMessage, $ProgressPercentage, $MessageAlignment).ConfigureAwait($false).GetAwaiter().GetResult()
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('UpdateNotifyIcon'))
            {
                $result = $clientServerInstance.UpdateNotifyIconAsync($MessageText).ConfigureAwait($false).GetAwaiter().GetResult()
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('GetEnvironmentVariable') -or $PSCmdlet.ParameterSetName.Equals('RemoveEnvironmentVariable'))
            {
                $result = $clientServerInstance."$($PSCmdlet.ParameterSetName)Async"($Variable).ConfigureAwait($false).GetAwaiter().GetResult()
            }
            elseif ($PSCmdlet.ParameterSetName.Equals('SetEnvironmentVariable'))
            {
                $result = $clientServerInstance.SetEnvironmentVariableAsync($Variable, $Value, !!$Expandable, !!$Append, !!$Remove).ConfigureAwait($false).GetAwaiter().GetResult()
            }
            elseif ($PSBoundParameters.ContainsKey('Options'))
            {
                $result = $clientServerInstance."$($PSCmdlet.ParameterSetName)Async"($Options).ConfigureAwait($false).GetAwaiter().GetResult()
            }
            else
            {
                $result = $clientServerInstance."$($PSCmdlet.ParameterSetName)Async"().ConfigureAwait($false).GetAwaiter().GetResult()
            }

            # If the log writer gave up the ghost, throw its exception.
            if ($loggingException = $clientServerInstance.GetLogWriterException())
            {
                $naerParams = @{
                    Exception = [System.ApplicationException]::new("The log writer failed and was unable to continue execution.", $loggingException)
                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                    ErrorId = 'ClientServerInstanceLoggingFailure'
                    TargetObject = $loggingException
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
        }
        catch
        {
            # Construct an ErrorRecord using an exception from the client/server process if possible.
            if ($clientServerProcessResult = Get-ADTClientServerProcessResult -ErrorRecord $_)
            {
                try
                {
                    $naerParams = @{
                        Exception = if ($clientServerProcessResult.StdErr.Count)
                        {
                            [System.ApplicationException]::new("Failed to invoke the requested client/server command.", [PSADT.ClientServer.DataSerialization]::DeserializeExceptionFromStdErr($clientServerProcessResult))
                        }
                        else
                        {
                            [System.ApplicationException]::new("Failed to invoke the requested client/server command.$(if (!$clientServerProcessResult.ExitCode.Equals([PSADT.ProcessManagement.ProcessManager]::TimeoutExitCode) -and !$_.Exception.InnerException.Message.Contains($clientServerProcessResult.ExitCode)) { " Exit Code: [$($clientServerProcessResult.ExitCode)]." })$(if ($clientServerProcessResult.StdOut) { " Console Output: [$([System.String]::Join([System.Environment]::NewLine, $clientServerProcessResult.StdOut))]" })", $_.Exception.InnerException)
                        }
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'ClientServerInstanceCommandFailure'
                        TargetObject = $clientServerProcessResult
                    }
                    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                }
                finally
                {
                    $clientServerProcessResult.Dispose()
                }
            }
            else
            {
                $PSCmdlet.ThrowTerminatingError($_)
            }
        }
        finally
        {
            if ($null -ne $clientServerProcessResult)
            {
                Close-ADTClientServerInstance
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
                    CustomDialog
                    {
                        [PSADT.UserInterface.DialogResults.CustomDialogResult]
                        break
                    }
                    DialogBox
                    {
                        [PSADT.UserInterface.DialogResults.DialogBoxResult]
                        break
                    }
                    HelpConsole
                    {
                        [PSADT.UserInterface.DialogResults.DialogBoxResult]
                        break
                    }
                    InputDialog
                    {
                        [PSADT.UserInterface.DialogResults.InputDialogResult]
                        break
                    }
                    ListSelectionDialog
                    {
                        [PSADT.UserInterface.DialogResults.ListSelectionDialogResult]
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
                            Exception = [System.ArgumentException]::new("The requested dialog type [$DialogType] is not supported in standalone mode.", 'DialogType')
                            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                            ErrorId = "$($PSCmdlet.ParameterSetName)Error"
                            TargetObject = $PSBoundParameters
                            RecommendedAction = "Please report this issue to the PSAppDeployToolkit development team."
                        }
                        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                    }
                }
            }
            GetProcessWindowInfo
            {
                [System.Collections.ObjectModel.ReadOnlyCollection[PSADT.WindowManagement.WindowInfo]]
                break
            }
            GetUserNotificationState
            {
                [PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]
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
            ShellExecuteProcess
            {
                [PSADT.ProcessManagement.ProcessResult]
                break
            }
            GetUserFocusModeState
            {
                [System.Int32]
                break
            }
            GetUserToastNotificationMode
            {
                [System.Int32]
                break
            }
            SilentRestart
            {
                [System.Boolean]
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

        # Define base parameters used for all registry operations. We've got a few here.
        $arkBaseParams = @{
            InformationAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
            WarningAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
            LiteralPath = [PSADT.Foundation.ClientServerUtilities]::UserRegistryPath
            SID = $User.SID
        }

        # Ensure any ShellExecute actions happen with no elevation specified.
        $elevatedTokenType = if ($PSCmdlet.ParameterSetName.Equals('ShellExecuteProcess'))
        {
            [PSADT.Security.ElevatedTokenType]::None
        }

        # Sanitise $PSBoundParameters, we'll use it to generate our arguments.
        $null = $PSBoundParameters.Remove($PSCmdlet.ParameterSetName)
        $null = $PSBoundParameters.Remove('NoWait')
        $null = $PSBoundParameters.Remove('User')
        if ($PSBoundParameters.ContainsKey('Options'))
        {
            $PSBoundParameters.Options = [PSADT.ClientServer.DataSerialization]::SerializeToString($Options)
        }

        # Build out parameters to store in the user's registry if we have any. When using Base64 logos, the path length can easily by exceeded.
        $arkArgsParams = $null; [System.String[]]$argumentList = if ($PSBoundParameters.Count -gt 0)
        {
            $PSBoundParameters.GetEnumerator() | & {
                begin
                {
                    $dict = [System.Collections.Generic.Dictionary[System.String, System.String]]::new()
                }
                process
                {
                    $dict.Add($_.Key, $_.Value)
                }
                end
                {
                    (Get-Variable -Name arkArgsParams).Value = $arkBaseParams.Clone(); $arkArgsParams.Add('Name', (Get-Random))
                    Set-ADTRegistryKey @arkArgsParams -Value ([PSADT.ClientServer.DataSerialization]::SerializeToString([System.Collections.ObjectModel.ReadOnlyDictionary[System.String, System.String]]$dict))
                    return "/$($PSCmdlet.ParameterSetName)", "-ArgumentsDictionary", "$($arkArgsParams.LiteralPath)\$($arkArgsParams.Name)"
                }
            }
        }
        else
        {
            "/$($PSCmdlet.ParameterSetName)"
        }

        # For -NoWait operations, we want to ensure the operation was successful before continuing.
        # Some platforms clean up the local cache before a dialog can appears, causing breaks.
        $return = try
        {
            if ($NoWait)
            {
                # Remove any previous success flags before starting the process.
                $arkSuccessParams = $arkBaseParams.Clone(); $arkSuccessParams.Add('Name', [PSADT.Foundation.ClientServerUtilities]::OperationSuccessRegistryProperty)
                Remove-ADTRegistryKey @arkSuccessParams; $cspHandle = [PSADT.Foundation.ClientServerUtilities]::StartClientOperationAsync($argumentList, $User, $elevatedTokenType)

                # Wait for the success flag. When found, remove it to clean up house and break to continue.
                $noWaitTimer = [System.Diagnostics.Stopwatch]::StartNew()
                while ($true)
                {
                    if ((Get-ADTRegistryKey @arkSuccessParams) -eq 1)
                    {
                        Remove-ADTRegistryKey @arkSuccessParams
                        return
                    }
                    if ($cspHandle.IsCompleted)
                    {
                        $cspHandle.ConfigureAwait($false).GetAwaiter().GetResult()
                        break
                    }
                    if ($noWaitTimer.Elapsed -ge [PSADT.Foundation.ClientServerUtilities]::ClientOperationTimeout)
                    {
                        $naerParams = @{
                            Exception = [System.TimeoutException]::new("Timed out waiting for the -NoWait client/server operation to report success.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                            ErrorId = 'ClientServerNoWaitTimeoutExceeded'
                            TargetObject = $cspHandle
                            RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
                        }
                        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                    }
                    [System.Threading.Thread]::Sleep(1)
                }
            }
            else
            {
                [PSADT.Foundation.ClientServerUtilities]::StartClientOperationAsync($argumentList, $User, $elevatedTokenType).ConfigureAwait($false).GetAwaiter().GetResult()
            }
        }
        finally
        {
            if ($arkArgsParams)
            {
                Remove-ADTRegistryKey @arkArgsParams
            }
        }

        # Deserialise the result for returning to the caller.
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
        try
        {
            # Confirm we were successful in our operation.
            if ($return.StdErr.Count -ne 0)
            {
                $naerParams = @{
                    Exception = [System.ApplicationException]::new("Failed to invoke the requested client/server command.", [PSADT.ClientServer.DataSerialization]::DeserializeExceptionFromStdErr($return))
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
            $result = [PSADT.ClientServer.DataSerialization]::DeserializeFromString($return.StdOut[0], $type)
        }
        finally
        {
            $return.Dispose()
        }
    }

    # Test that the received result is valid and expected.
    if (($null -eq $result) -or (($result -is [System.Boolean]) -and !$result.Equals($true) -and !$PSCmdlet.ParameterSetName.Equals('ProgressDialogOpen') -and !$PSCmdlet.ParameterSetName.Equals('NotifyIconOpen')))
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
    if (($result | Out-ADTString) -and ![PSADT.ClientServer.ServerInstance]::SuccessSentinel.Equals($result) -and ($PSCmdlet.ParameterSetName -match '^(InitCloseAppsDialog|ProgressDialogOpen|ShowModalDialog|NotifyIconOpen|GetProcessWindowInfo|GetUserNotificationState|GetForegroundWindowProcessId|GetEnvironmentVariable|ShellExecuteProcess|GetUserFocusModeState|GetUserToastNotificationMode)$') -and ![PSADT.UserInterface.DialogType]::HelpConsole.Equals($DialogType) -and (($result -isnot [PSADT.ProcessManagement.ProcessResult]) -or !$result.ExitCode.Equals([PSADT.Foundation.ClientServerUtilities]::ShellExecuteProcessSuccessCode)))
    {
        return $result
    }
}
