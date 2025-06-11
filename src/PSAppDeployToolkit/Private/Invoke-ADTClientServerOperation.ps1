#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTClientServerOperation
#
#-----------------------------------------------------------------------------

function Private:Invoke-ADTClientServerOperation
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'InitCloseAppsDialog')]
        [System.Management.Automation.SwitchParameter]$InitCloseAppsDialog,

        [Parameter(Mandatory = $true, ParameterSetName = 'PromptToCloseApps')]
        [System.Management.Automation.SwitchParameter]$PromptToCloseApps,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [System.Management.Automation.SwitchParameter]$ShowModalDialog,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowBalloonTip')]
        [System.Management.Automation.SwitchParameter]$ShowBalloonTip,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetProcessWindowInfo')]
        [System.Management.Automation.SwitchParameter]$GetProcessWindowInfo,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetUserNotificationState')]
        [System.Management.Automation.SwitchParameter]$GetUserNotificationState,

        [Parameter(Mandatory = $true, ParameterSetName = 'RefreshDesktopAndEnvironmentVariables')]
        [System.Management.Automation.SwitchParameter]$RefreshDesktopAndEnvironmentVariables,

        [Parameter(Mandatory = $true, ParameterSetName = 'MinimizeAllWindows')]
        [System.Management.Automation.SwitchParameter]$MinimizeAllWindows,

        [Parameter(Mandatory = $true, ParameterSetName = 'RestoreAllWindows')]
        [System.Management.Automation.SwitchParameter]$RestoreAllWindows,

        [Parameter(Mandatory = $true, ParameterSetName = 'SendKeys')]
        [System.Management.Automation.SwitchParameter]$SendKeys,

        [Parameter(Mandatory = $true, ParameterSetName = 'InitCloseAppsDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'PromptToCloseApps')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowBalloonTip')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetProcessWindowInfo')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetUserNotificationState')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RefreshDesktopAndEnvironmentVariables')]
        [Parameter(Mandatory = $true, ParameterSetName = 'MinimizeAllWindows')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RestoreAllWindows')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SendKeys')]
        [ValidateNotNullOrEmpty()]
        [PSADT.TerminalServices.SessionInfo]$User,

        [Parameter(Mandatory = $false, ParameterSetName = 'InitCloseAppsDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.ProcessManagement.ProcessDefinition[]]$CloseProcesses,

        [Parameter(Mandatory = $true, ParameterSetName = 'PromptToCloseApps')]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$PromptToCloseTimeout,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogType]$DialogType,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogStyle]$DialogStyle,

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

    # Ensure the permissions are correct on all files before proceeding.
    Set-ADTClientServerProcessPermissions -User $User

    # Go into client/server mode if a session is active and we're not asked to wait.
    if (($PSCmdlet.ParameterSetName -match '^(InitCloseAppsDialog|PromptToCloseApps|MinimizeAllWindows|RestoreAllWindows)$') -or
        [PSADT.UserInterface.Dialogs.DialogType]::CloseAppsDialog.Equals($DialogType) -or
        ((Test-ADTSessionActive) -and $User.Equals((Get-ADTEnvironmentTable).RunAsActiveUser) -and !$NoWait))
    {
        # Instantiate a new ClientServerProcess object if one's not already present.
        if (!$Script:ADT.ClientServerProcess)
        {
            Open-ADTClientServerProcess -User $User
        }

        # Invoke the right method depending on the mode.
        $result = if ($PSCmdlet.ParameterSetName.Equals('ShowModalDialog'))
        {
            $Script:ADT.ClientServerProcess."Show$($DialogType)"($DialogStyle, $Options)
        }
        elseif ($PSCmdlet.ParameterSetName.Equals('InitCloseAppsDialog'))
        {
            $Script:ADT.ClientServerProcess.InitCloseAppsDialog($CloseProcesses)
        }
        elseif ($PSCmdlet.ParameterSetName.Equals('PromptToCloseApps'))
        {
            $Script:ADT.ClientServerProcess.PromptToCloseApps($PromptToCloseTimeout)
        }
        elseif ($PSBoundParameters.ContainsKey('Options'))
        {
            $Script:ADT.ClientServerProcess.($PSCmdlet.ParameterSetName)($Options)
        }
        else
        {
            $Script:ADT.ClientServerProcess.($PSCmdlet.ParameterSetName)()
        }
        if (!$result)
        {
            $naerParams = @{
                Exception = [System.ApplicationException]::new("Failed to perform the $($PSCmdlet.ParameterSetName) operation for an unknown reason.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId = "$($PSCmdlet.ParameterSetName)Error"
                RecommendedAction = "Please report this issue to the PSAppDeployToolkit development team."
            }
            throw (New-ADTErrorRecord @naerParams)
        }
        $PSCmdlet.WriteObject($result, $false); return
    }

    # Sanitise $PSBoundParameters, we'll use it to generate our arguments.
    $null = $PSBoundParameters.Remove($PSCmdlet.ParameterSetName)
    $null = $PSBoundParameters.Remove('NoWait')
    $null = $PSBoundParameters.Remove('User')
    if ($PSBoundParameters.ContainsKey('Options'))
    {
        $PSBoundParameters.Options = [PSADT.Utilities.SerializationUtilities]::SerializeToString($Options)
    }

    # Set up the parameters for Start-ADTProcessAsUser.
    $sapauParams = @{
        Username = $User.NTAccount
        FilePath = "$Script:PSScriptRoot\lib\PSADT.ClientServer.Client.exe"
        ArgumentList = $("/$($PSCmdlet.ParameterSetName)"; if ($PSBoundParameters.Count -gt 0) { $PSBoundParameters.GetEnumerator() | & { process { "-$($_.Key)"; $_.Value } } })
        MsiExecWaitTime = 1
        CreateNoWindow = $true
        InformationAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
    }

    # Farm this out to a new process.
    $result = if ($NoWait)
    {
        Start-ADTProcessAsUser @sapauParams -NoWait
        return
    }
    else
    {
        Start-ADTProcessAsUser @sapauParams -PassThru
    }

    # Confirm we were successful in our operation.
    if ($result -isnot [PSADT.Execution.ProcessResult])
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("The client/server process failed to start.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'ClientServerInvocationFailure'
            TargetObject = $result
            RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    if ($result.StdErr.Count -ne 0)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new($($result.StdErr))
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'ClientServerResultError'
            TargetObject = $result
            RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    if ($result.ExitCode -ne 0)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("The client/server process failed with exit code [$($result.ExitCode)].")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'ClientServerRuntimeFailure'
            TargetObject = $result
            RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    if ($result.StdOut.Count -eq 0)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("The client/server process returned no result.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'ClientServerResultNull'
            TargetObject = $result
            RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

    # Return the result to the caller. Don't let PowerShell enumerate collections/lists!
    if (($return = [PSADT.Utilities.SerializationUtilities]::DeserializeFromString([System.String]::Join([System.String]::Empty, $result.StdOut))))
    {
        $PSCmdlet.WriteObject($return, $false)
    }
}
