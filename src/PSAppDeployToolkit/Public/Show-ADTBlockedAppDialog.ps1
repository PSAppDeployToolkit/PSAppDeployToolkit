﻿#-----------------------------------------------------------------------------
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
        The title for the blocked application dialog.

    .PARAMETER Message
        The message for the blocked application dialog.

    .PARAMETER UnboundArguments
        Captures any additional arguments passed to the function.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Show-ADTBlockedAppDialog -Title 'Blocked Application' -Message 'Blocked Application'

        Displays a dialog with the title and message of 'Blocked Application' to inform the user about a blocked application.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
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

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Message,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Return early if someone happens to call this in a non-async mode.
        if (Test-ADTSessionActive)
        {
            return
        }

        try
        {
            try
            {
                # Create a mutex and specify a name without acquiring a lock on the mutex.
                $showBlockedAppDialogMutexName = "Global\$($MyInvocation.MyCommand.Name)_ShowBlockedAppDialog"
                $showBlockedAppDialogMutex = [System.Threading.Mutex]::new($false, $showBlockedAppDialogMutexName)

                # Attempt to acquire an exclusive lock on the mutex, attempt will fail after 1 millisecond if unable to acquire exclusive lock.
                if ((Test-ADTMutexAvailability -MutexName $showBlockedAppDialogMutexName) -and $showBlockedAppDialogMutex.WaitOne(1))
                {
                    Write-ADTLogEntry -Message "Unable to acquire an exclusive lock on mutex [$showBlockedAppDialogMutexName] because another blocked application dialog window is already open. Exiting script..." -Severity 2
                    return
                }
                Show-ADTInstallationPrompt -Title $Title -Message $Message -Icon Warning -ButtonRightText OK
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
