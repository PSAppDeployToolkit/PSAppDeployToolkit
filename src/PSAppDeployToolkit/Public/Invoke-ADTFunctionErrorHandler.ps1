#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTFunctionErrorHandler
#
#-----------------------------------------------------------------------------

function Invoke-ADTFunctionErrorHandler
{
    <#
    .SYNOPSIS
        Handles errors within ADT functions by logging and optionally passing through the error.

    .DESCRIPTION
        This function handles errors within ADT functions by logging the error message and optionally passing through the error record. It recovers the true ErrorActionPreference set by the caller and sets it within the function. If a log message is provided, it appends the resolved error record to the log message. Depending on the ErrorActionPreference, it either throws a terminating error or writes a non-terminating error.

    .PARAMETER Cmdlet
        The cmdlet that is calling this function.

    .PARAMETER SessionState
        The session state of the calling cmdlet.

    .PARAMETER ErrorRecord
        The error record to handle.

    .PARAMETER LogMessage
        The error message to write to the active ADTSession's log file.

    .PARAMETER DisableErrorResolving
        If specified, the function will not append the resolved error record to the log message.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_

        Handles the error within the calling cmdlet and logs it.

    .EXAMPLE
        Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "An error occurred" -DisableErrorResolving

        Handles the error within the calling cmdlet, logs a custom message without resolving the error record, and logs it.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding(DefaultParameterSetName = 'None')]
    [OutputType([System.Management.Automation.ErrorRecord])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ErrorRecord]$ErrorRecord,

        [Parameter(Mandatory = $true, ParameterSetName = 'LogMessage')]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogMessage,

        [Parameter(Mandatory = $false, ParameterSetName = 'LogMessage')]
        [System.Management.Automation.SwitchParameter]$DisableErrorResolving
    )

    # Recover true ErrorActionPreference the caller may have set,
    # unless an ErrorAction was specifically provided to this function.
    $ErrorActionPreference = if ($PSBoundParameters.ContainsKey('ErrorAction'))
    {
        $PSBoundParameters.ErrorAction
    }
    elseif ($SessionState.Equals($ExecutionContext.SessionState))
    {
        Get-Variable -Name OriginalErrorAction -Scope 1 -ValueOnly
    }
    else
    {
        $SessionState.PSVariable.Get('OriginalErrorAction').Value
    }

    # If the caller hasn't specified a LogMessage, use the ErrorRecord's message.
    if ([System.String]::IsNullOrWhiteSpace($LogMessage))
    {
        $LogMessage = $ErrorRecord.Exception.Message
    }

    # Write-Error enforces its own name against the Activity, let's re-write it.
    if ($ErrorRecord.CategoryInfo.Activity -match '^Write-Error$')
    {
        $ErrorRecord.CategoryInfo.Activity = $Cmdlet.MyInvocation.MyCommand.Name
    }

    # Write out the error to the log file.
    if (!$DisableErrorResolving)
    {
        $LogMessage += "`n$(Resolve-ADTErrorRecord -ErrorRecord $ErrorRecord)"
    }
    Write-ADTLogEntry -Message $LogMessage -Source $Cmdlet.MyInvocation.MyCommand.Name -Severity 3

    # If we're stopping, throw a terminating error. While WriteError will terminate if stopping,
    # this can also write out an [System.Management.Automation.ActionPreferenceStopException] object.
    if ($ErrorActionPreference.Equals([System.Management.Automation.ActionPreference]::Stop))
    {
        $Cmdlet.ThrowTerminatingError($ErrorRecord)
    }
    elseif (!(Test-ADTSessionActive) -or ($ErrorActionPreference -notmatch '^(SilentlyContinue|Ignore)$'))
    {
        $Cmdlet.WriteError($ErrorRecord)
    }
}
