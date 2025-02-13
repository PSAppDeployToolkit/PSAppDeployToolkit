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
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Test-ADTPowerPoint
    #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $procName = 'POWERPNT'
        $presenting = 'Unknown'
    }

    process
    {
        Write-ADTLogEntry -Message 'Checking if PowerPoint is in either fullscreen slideshow mode or presentation mode...'
        try
        {
            try
            {
                # Return early if we're not running PowerPoint or we can't interactively check.
                if (!($PowerPointProcess = Get-Process -Name $procName -ErrorAction Ignore))
                {
                    Write-ADTLogEntry -Message 'PowerPoint application is not running.'
                    return ($presenting = $false)
                }
                if (![System.Environment]::UserInteractive)
                {
                    Write-ADTLogEntry -Message 'Unable to run check to see if PowerPoint is in fullscreen mode or Presentation Mode because current process is not interactive. Configure script to run in interactive mode in your deployment tool. If using SCCM Application Model, then make sure "Allow users to view and interact with the program installation" is selected. If using SCCM Package Model, then make sure "Allow users to interact with this program" is selected.' -Severity 2
                    return
                }

                # Check if "POWERPNT" process has a window with a title that begins with "PowerPoint Slide Show" or "Powerpoint-" for non-English language systems.
                # There is a possiblity of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show".
                if (Get-ADTWindowTitle -GetAllWindowTitles | & { process { if (($_.ParentProcess -eq $procName) -and ($_.WindowTitle -match '^PowerPoint(-| Slide Show)')) { return $_ } } } | Select-Object -First 1)
                {
                    Write-ADTLogEntry -Message "Detected that PowerPoint process [$procName] has a window with a title that beings with [PowerPoint Slide Show] or [PowerPoint-]."
                    return ($presenting = $true)
                }
                Write-ADTLogEntry -Message "Detected that PowerPoint process [$procName] does not have a window with a title that beings with [PowerPoint Slide Show] or [PowerPoint-]."
                Write-ADTLogEntry -Message "PowerPoint process [$procName] has process ID(s) [$(($PowerPointProcessIDs = $PowerPointProcess.Id) -join ', ')]."

                # If previous detection method did not detect PowerPoint in fullscreen mode, then check if PowerPoint is in Presentation Mode (check only works on Windows Vista or higher).
                # Note: The below method does not detect PowerPoint presentation mode if the presentation is on a monitor that does not have current mouse input control.
                Write-ADTLogEntry -Message "Detected user notification state [$(($UserNotificationState = [PSADT.GUI.UiAutomation]::GetUserNotificationState()))]."
                switch ($UserNotificationState)
                {
                    QUNS_PRESENTATION_MODE
                    {
                        Write-ADTLogEntry -Message 'Detected that system is in [Presentation Mode].'
                        return ($presenting = $true)
                    }
                    QUNS_BUSY
                    {
                        if ($PowerPointProcessIDs -contains [PSADT.GUI.UiAutomation]::GetWindowThreadProcessId([PSADT.LibraryInterfaces.User32]::GetForegroundWindow()))
                        {
                            Write-ADTLogEntry -Message 'Detected a fullscreen foreground window matches a PowerPoint process ID.'
                            return ($presenting = $true)
                        }
                        Write-ADTLogEntry -Message 'Unable to find a fullscreen foreground window that matches a PowerPoint process ID.'
                        break
                    }
                }
                return ($presenting = $false)
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
        Write-ADTLogEntry -Message "PowerPoint is running in fullscreen mode [$presenting]."
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
