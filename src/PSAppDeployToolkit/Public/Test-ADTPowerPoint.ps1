#-----------------------------------------------------------------------------
#
# MARK: Test-ADTPowerPoint
#
#-----------------------------------------------------------------------------

function Test-ADTPowerPoint
{
    <#
    .SYNOPSIS
        Tests whether PowerPoint is running in either fullscreen slideshow mode or presentation mode.

    .DESCRIPTION
        Tests whether someone is presenting using PowerPoint in either fullscreen slideshow mode or presentation mode. This function checks if the PowerPoint process has a window with a title that begins with "PowerPoint Slide Show" or "PowerPoint-" for non-English language systems. There is a possibility of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show". If the previous detection method does not detect PowerPoint in fullscreen mode, it checks if PowerPoint is in Presentation Mode (only works on Windows Vista or higher).

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true if PowerPoint is running in either fullscreen slideshow mode or presentation mode, otherwise returns $false.

    .EXAMPLE
        Test-ADTPowerPoint

        Checks if PowerPoint is running in either fullscreen slideshow mode or presentation mode and returns true or false.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function can only execute detection logic if the process is in interactive mode.

        There is a possibility of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show".

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Test-ADTPowerPoint
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        Write-ADTLogEntry -Message 'Checking if PowerPoint is in either fullscreen slideshow mode or presentation mode...'
        try
        {
            try
            {
                # Bypass if no one's logged onto the device.
                if (!(Get-ADTClientServerUser))
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
                    return
                }

                # Return early if we're not running PowerPoint or we can't interactively check.
                if (!(Get-Process -Name POWERPNT -ErrorAction Ignore))
                {
                    Write-ADTLogEntry -Message 'There is no instance of PowerPoint running on this system.'
                    return $false
                }

                # Check if "POWERPNT" process has a window with a title that begins with "PowerPoint Slide Show" or "PowerPoint-" for non-English language systems.
                # There is a possiblity of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show".
                if (Get-ADTWindowTitle -ParentProcess POWERPNT -WindowTitle '^PowerPoint(-| Slide Show)' -InformationAction SilentlyContinue)
                {
                    Write-ADTLogEntry -Message "Detected a PowerPoint process with a window title indicating a slide show is active."
                    return $true
                }

                # If previous detection method did not detect PowerPoint in fullscreen mode, then check if PowerPoint is in Presentation Mode (check only works on Windows Vista or higher).
                # Note: The below method does not detect PowerPoint presentation mode if the presentation is on a monitor that does not have current mouse input control.
                switch (Get-ADTUserNotificationState -InformationAction SilentlyContinue)
                {
                    ([PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_PRESENTATION_MODE)
                    {
                        Write-ADTLogEntry -Message 'Detected the user's notification state is presentation mode.'
                        return $true
                    }
                    ([PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_BUSY)
                    {
                        Write-ADTLogEntry -Message 'Detected the user's notification state is busy.'
                        return $true
                    }
                }
                Write-ADTLogEntry -Message 'Unable to detect any indication of an ongoing presentation.'
                return $false
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
