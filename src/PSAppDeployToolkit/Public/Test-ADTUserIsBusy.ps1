#-----------------------------------------------------------------------------
#
# MARK: Test-ADTUserIsBusy
#
#-----------------------------------------------------------------------------

function Test-ADTUserIsBusy
{
    <#
    .SYNOPSIS
        Tests whether the device is considered to be in a busy state, such as when a user is using the microphone, device is in focus mode, presenting a PowerPoint slide deck, and more.

    .DESCRIPTION
        This function tests whether the device is considered to be in a busy state using the following metrics in the following order:
        * Device's microphone is in use.
        * User has entered "focus mode".
        * User has enabled "do not disturb" mode.
        * User's notification state indicates their busy, presenting, etc.
        * Whether PowerPoint is open and they're presenting in full screen.

        If any of these tests return true, then all other tests thereafter are not ran as they're not necessary.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true if the device is considered to be in a busy state, such as when a user is using the microphone, device is in focus mode, presenting a PowerPoint slide deck, and more.

    .EXAMPLE
        Test-ADTUserIsBusy

        Tests whether the device is considered to be in a busy state, such as when a user is using the microphone, device is in focus mode, presenting a PowerPoint slide deck, and more.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Test-ADTUserIsBusy
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
    )

    Write-ADTLogEntry -Message "Running tests to determine whether the active user is busy or not..."
    try
    {
        # Bypass if no one's logged onto the device.
        if (!(Get-ADTClientServerUser))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
            return $false
        }
        if (Test-ADTMicrophoneInUse)
        {
            return $true
        }
        if (Test-ADTUserInFocusMode)
        {
            return $true
        }
        if ((Get-ADTUserToastNotificationMode) -gt 0)
        {
            return $true
        }
        if ((($gauns = Get-ADTUserNotificationState) -ne [PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_ACCEPTS_NOTIFICATIONS) -and ($gauns -ne [PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_APP))
        {
            return $true
        }
        if (Test-ADTPowerPoint)
        {
            return $true
        }
        return $false
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
