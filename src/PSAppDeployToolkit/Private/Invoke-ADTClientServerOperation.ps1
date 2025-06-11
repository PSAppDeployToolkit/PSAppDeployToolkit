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
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [System.Management.Automation.SwitchParameter]$ShowModalDialog,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetProcessWindowInfo')]
        [System.Management.Automation.SwitchParameter]$GetProcessWindowInfo,

        [Parameter(Mandatory = $true, ParameterSetName = 'GetUserNotificationState')]
        [System.Management.Automation.SwitchParameter]$GetUserNotificationState,

        [Parameter(Mandatory = $true, ParameterSetName = 'RefreshDesktopAndEnvironmentVariables')]
        [System.Management.Automation.SwitchParameter]$RefreshDesktopAndEnvironmentVariables,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetProcessWindowInfo')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetUserNotificationState')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RefreshDesktopAndEnvironmentVariables')]
        [ValidateNotNullOrEmpty()]
        [PSADT.TerminalServices.SessionInfo]$User,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogType]$DialogType,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogStyle]$DialogStyle,

        [Parameter(Mandatory = $true, ParameterSetName = 'ShowModalDialog')]
        [Parameter(Mandatory = $true, ParameterSetName = 'GetProcessWindowInfo')]
        [ValidateNotNullOrEmpty()]
        [System.Object]$Options,

        [Parameter(Mandatory = $false, ParameterSetName = 'ShowModalDialog')]
        [System.Management.Automation.SwitchParameter]$NoWait
    )

    # Ensure the permissions are correct on all files before proceeding.
    Set-ADTClientServerProcessPermissions -User $User

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
    if (($return = [PSADT.Utilities.SerializationUtilities]::DeserializeFromString($($result.StdOut))))
    {
        $PSCmdlet.WriteObject($return, $false)
    }
}
