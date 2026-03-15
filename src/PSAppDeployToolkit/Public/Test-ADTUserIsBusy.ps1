#-----------------------------------------------------------------------------
#
# MARK: Test-ADTUserIsBusy
#
#-----------------------------------------------------------------------------

function Test-ADTUserIsBusy
{
    <#
    .SYNOPSIS
        Tests whether the device's microphone is in use, the user has manually turned on presentation mode, or PowerPoint is running in either fullscreen slideshow mode or presentation mode.

    .DESCRIPTION
        Tests whether the device's microphone is in use, the user has manually turned on presentation mode, or PowerPoint is running in either fullscreen slideshow mode or presentation mode.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true if the device's microphone is in use, the user has manually turned on presentation mode, or PowerPoint is running in either fullscreen slideshow mode or presentation mode, otherwise $false.

    .EXAMPLE
        Test-ADTUserIsBusy

        Tests whether the device's microphone is in use, the user has manually turned on presentation mode, or PowerPoint is running in either fullscreen slideshow mode or presentation mode, and returns true or false.

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
