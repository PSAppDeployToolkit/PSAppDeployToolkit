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

        [Parameter(Mandatory = $true, ParameterSetName = 'InitCloseAppsDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'PromptToCloseApps')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ProgressDialogOpen')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowProgressDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UpdateProgressDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'CloseProgressDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowBalloonTip')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetProcessWindowInfo')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetUserNotificationState')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetForegroundWindowProcessId')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RefreshDesktopAndEnvironmentVariables')]
        [Parameter(Mandatory = $true, ParameterSetName = 'MinimizeAllWindows')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RestoreAllWindows')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SendKeys')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetEnvironmentVariable')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RemoveEnvironmentVariable')]
        [ValidateNotNullOrEmpty()]
        [PSADT.Module.RunAsActiveUser]$User,

        [Parameter(Mandatory = $false, ParameterSetName = 'InitCloseAppsDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.ProcessManagement.ProcessDefinition[]]$CloseProcesses,

        [Parameter(Mandatory = $true, ParameterSetName = 'PromptToCloseApps')]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$PromptToCloseTimeout,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogType]$DialogType,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowProgressDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogStyle]$DialogStyle,

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
        [PSADT.UserInterface.Dialogs.DialogMessageAlignment]$MessageAlignment,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetEnvironmentVariable')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RemoveEnvironmentVariable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Variable,

        [Parameter(Mandatory = $true, ParameterSetName = 'SetEnvironmentVariable')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Value,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowProgressDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowBalloonTip')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetProcessWindowInfo')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SendKeys')]
        [ValidateNotNullOrEmpty()]
        [System.Object]$Options,

        [Parameter(Mandatory = $false, ParameterSetName = 'ShowModalDialog')]
        [Parameter(Mandatory = $false, ParameterSetName = 'ShowBalloonTip')]
        [System.Management.Automation.SwitchParameter]$NoWait
    )

    # If the client/server process is instantiated but no longer running, clean up before continuing.
    if ($Script:ADT.ClientServerProcess -and !$Script:ADT.ClientServerProcess.IsRunning)
    {
        Write-ADTLogEntry -Message 'Existing client/server process closed outside of our control.'
        Close-ADTClientServerProcess
    }

    # Ensure the permissions are correct on all files before proceeding.
    Set-ADTClientServerProcessPermissions -User $User

    # Go into client/server mode if a session is active and we're not asked to wait.
    if (($PSCmdlet.ParameterSetName -match '^(InitCloseAppsDialog|PromptToCloseApps|ProgressDialogOpen|ShowProgressDialog|UpdateProgressDialog|CloseProgressDialog|MinimizeAllWindows|RestoreAllWindows)$') -or
        [PSADT.UserInterface.Dialogs.DialogType]::CloseAppsDialog.Equals($DialogType) -or
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
            catch [System.IO.InvalidDataException]
            {
                # Get the result from the client/server process. This is safe as this catch means it died.
                $clientResult = $Script:ADT.ClientServerProcess.GetClientProcessResult($true)

                # Construct an ErrorRecord using an exception from the client/server process if possible.
                $naerParams = @{
                    Exception = if ($clientResult.StdErr.Count)
                    {
                        [System.ApplicationException]::new("Failed to open the instantiated client/server process.", [PSADT.ClientServer.DataSerialization]::DeserializeFromString($return.StdErr))
                    }
                    else
                    {
                        [System.ApplicationException]::new("Failed to open the instantiated client/server process.$(if (!$clientResult.ExitCode.Equals([PSADT.ProcessManagement.ProcessManager]::TimeoutExitCode)) { " Exit Code: [$($clientResult.ExitCode)]." })$(if ($clientResult.StdOut) { " Console Output: [$([System.String]::Join("`n", $clientResult.StdOut))]" })", $_.Exception)
                    }
                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                    ErrorId = 'ClientServerProcessOpenFailure'
                    TargetObject = $clientResult
                }
                $Script:ADT.ClientServerProcess.Dispose()
                $Script:ADT.ClientServerProcess = $null
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
            catch
            {
                $Script:ADT.ClientServerProcess.Dispose()
                $Script:ADT.ClientServerProcess = $null
                $PSCmdlet.ThrowTerminatingError($_)
            }

            # Ensure we properly close the client/server process upon the closure of the last active session.
            Add-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.'Close-ADTClientServerProcess'
        }

        # Invoke the right method depending on the mode.
        try
        {
            if ([PSADT.UserInterface.Dialogs.DialogType]::DialogBox.Equals($DialogType))
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
                $result = $Script:ADT.ClientServerProcess.SetEnvironmentVariable($Variable, $Value)
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
        catch [System.IO.InvalidDataException]
        {
            # Get the result from the client/server process. This is safe as this catch means it died.
            $result = $_; $clientResult = $Script:ADT.ClientServerProcess.GetClientProcessResult($true);

            # Construct an ErrorRecord using an exception from the client/server process if possible.
            $naerParams = @{
                Exception = if ($clientResult.StdErr.Count)
                {
                    [System.ApplicationException]::new("Failed to invoke the requested client/server command.", [PSADT.ClientServer.DataSerialization]::DeserializeFromString($return.StdErr))
                }
                else
                {
                    [System.ApplicationException]::new("Failed to invoke the requested client/server command.$(if (!$clientResult.ExitCode.Equals([PSADT.ProcessManagement.ProcessManager]::TimeoutExitCode)) { " Exit Code: [$($clientResult.ExitCode)]." })$(if ($clientResult.StdOut) { " Console Output: [$([System.String]::Join("`n", $clientResult.StdOut))]" })", $_.Exception)
                }
                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId = 'ClientServerProcessCommandFailure'
                TargetObject = $clientResult
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError(($result = $_))
        }
        finally
        {
            if (($result -is [System.Management.Automation.ErrorRecord]) -and ($result.Exception -is [System.IO.InvalidDataException]))
            {
                Close-ADTClientServerProcess
            }
        }
    }
    else
    {
        # Sanitise $PSBoundParameters, we'll use it to generate our arguments.
        $null = $PSBoundParameters.Remove($PSCmdlet.ParameterSetName)
        $null = $PSBoundParameters.Remove('NoWait')
        $null = $PSBoundParameters.Remove('User')
        if ($PSBoundParameters.ContainsKey('Options'))
        {
            $PSBoundParameters.Options = [PSADT.ClientServer.DataSerialization]::SerializeToString($Options)
        }

        # Set up the parameters for Start-ADTProcessAsUser.
        $sapauParams = @{
            Username = $User.NTAccount
            UseHighestAvailableToken = $true
            ArgumentList = $("/$($PSCmdlet.ParameterSetName)"; if ($PSBoundParameters.Count -gt 0) { $PSBoundParameters.GetEnumerator() | & { process { "-$($_.Key)"; $_.Value } } })
            WorkingDirectory = [System.Environment]::SystemDirectory
            MsiExecWaitTime = 1
            CreateNoWindow = $true
            InformationAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
        }

        # Farm this out to a new process.
        $return = try
        {
            if ($NoWait)
            {
                Start-ADTProcessAsUser @sapauParams -FilePath "$Script:PSScriptRoot\lib\PSADT.ClientServer.Client.Launcher.exe" -NoWait
                return
            }
            else
            {
                Start-ADTProcessAsUser @sapauParams -FilePath "$Script:PSScriptRoot\lib\PSADT.ClientServer.Client.exe" -PassThru
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
                Exception = [System.ApplicationException]::new("Failed to invoke the requested client/server command.", [PSADT.ClientServer.DataSerialization]::DeserializeFromString($return.StdErr))
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
                Exception = [System.InvalidOperationException]::new("The client/server process failed with exit code [$($return.ExitCode)] ($(if ([System.Enum]::IsDefined([PSADT.ClientServer.ClientExitCode], $return.ExitCode)) { [PSADT.ClientServer.ClientExitCode]$return.ExitCode } else { $return.ExitCode })).")
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

        # Deserialise the result for returning to the caller.
        $result = [PSADT.ClientServer.DataSerialization]::DeserializeFromString($return.StdOut)
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
    if ($PSCmdlet.ParameterSetName -match '^(InitCloseAppsDialog|ProgressDialogOpen|ShowModalDialog|GetProcessWindowInfo|GetUserNotificationState|GetForegroundWindowProcessId|GetEnvironmentVariable)$')
    {
        return $result
    }
}
