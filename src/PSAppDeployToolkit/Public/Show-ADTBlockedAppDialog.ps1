#-----------------------------------------------------------------------------
#
# MARK: Show-ADTBlockedAppDialog
#
#-----------------------------------------------------------------------------

function Show-ADTBlockedAppDialog
{
    <#
    .SYNOPSIS
        Displays a dialog to inform the user about a blocked application.

    .DESCRIPTION
        Displays a dialog to inform the user that an application is blocked. This function ensures that only one instance of the blocked application dialog is shown at a time by using a mutex. If another instance of the dialog is already open, the function exits without displaying a new dialog.

    .PARAMETER Title
        The title of the blocked application dialog.

    .PARAMETER UnboundArguments
        Captures any additional arguments passed to the function.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Show-ADTBlockedAppDialog -Title 'Blocked Application'

        Displays a dialog with the title 'Blocked Application' to inform the user about a blocked application.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UnboundArguments', Justification = "This parameter is just to trap any superfluous input at the end of the function's call.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    begin
    {
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Return early if someone happens to call this in a non-async mode.
        if ($adtSession)
        {
            return
        }

        try
        {
            try
            {
                # Create a mutex and specify a name without acquiring a lock on the mutex.
                $showBlockedAppDialogMutexName = "Global\$((Get-ADTEnvironment).appDeployToolkitName)_ShowBlockedAppDialog_Message"
                $showBlockedAppDialogMutex = [System.Threading.Mutex]::new($false, $showBlockedAppDialogMutexName)

                # Attempt to acquire an exclusive lock on the mutex, attempt will fail after 1 millisecond if unable to acquire exclusive lock.
                if ((Test-ADTIsMutexAvailable -MutexName $showBlockedAppDialogMutexName) -and $showBlockedAppDialogMutex.WaitOne(1))
                {
                    Write-ADTLogEntry -Message "Unable to acquire an exclusive lock on mutex [$showBlockedAppDialogMutexName] because another blocked application dialog window is already open. Exiting script..." -Severity 2
                    return
                }
                Show-ADTInstallationPrompt -Title $Title -Message (Get-ADTStringTable).BlockExecution.Message -Icon Warning -ButtonRightText OK
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
