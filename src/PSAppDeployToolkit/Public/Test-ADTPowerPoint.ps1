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

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $procName = 'POWERPNT'
        $presenting = 'Unknown'
    }

    process
    {
        & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Checking if PowerPoint is in either fullscreen slideshow mode or presentation mode...'
        try
        {
            try
            {
                # Return early if we're not running PowerPoint or we can't interactively check.
                if (!($PowerPointProcess = & $Script:CommandTable.'Get-Process' -Name $procName -ErrorAction Ignore))
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message 'PowerPoint application is not running.'
                    return ($presenting = $false)
                }
                if (![System.Environment]::UserInteractive)
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Unable to run check to see if PowerPoint is in fullscreen mode or Presentation Mode because current process is not interactive. Configure script to run in interactive mode in your deployment tool. If using SCCM Application Model, then make sure "Allow users to view and interact with the program installation" is selected. If using SCCM Package Model, then make sure "Allow users to interact with this program" is selected.' -Severity 2
                    return
                }

                # Check if "POWERPNT" process has a window with a title that begins with "PowerPoint Slide Show" or "Powerpoint-" for non-English language systems.
                # There is a possiblity of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show".
                if (& $Script:CommandTable.'Get-ADTWindowTitle' -GetAllWindowTitles | & { process { if (($_.ParentProcess -eq $procName) -and ($_.WindowTitle -match '^PowerPoint(-| Slide Show)')) { return $_ } } } | & $Script:CommandTable.'Select-Object' -First 1)
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message "Detected that PowerPoint process [$procName] has a window with a title that beings with [PowerPoint Slide Show] or [PowerPoint-]."
                    return ($presenting = $true)
                }
                & $Script:CommandTable.'Write-ADTLogEntry' -Message "Detected that PowerPoint process [$procName] does not have a window with a title that beings with [PowerPoint Slide Show] or [PowerPoint-]."
                & $Script:CommandTable.'Write-ADTLogEntry' -Message "PowerPoint process [$procName] has process ID(s) [$(($PowerPointProcessIDs = $PowerPointProcess.Id) -join ', ')]."

                # If previous detection method did not detect PowerPoint in fullscreen mode, then check if PowerPoint is in Presentation Mode (check only works on Windows Vista or higher).
                # Note: The below method does not detect PowerPoint presentation mode if the presentation is on a monitor that does not have current mouse input control.
                & $Script:CommandTable.'Write-ADTLogEntry' -Message "Detected user notification state [$(($UserNotificationState = [PSADT.GUI.UiAutomation]::GetUserNotificationState()))]."
                switch ($UserNotificationState)
                {
                    PresentationMode
                    {
                        & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Detected that system is in [Presentation Mode].'
                        return ($presenting = $true)
                    }
                    FullScreenOrPresentationModeOrLoginScreen
                    {
                        if ($PowerPointProcessIDs -contains [PSADT.GUI.UiAutomation]::GetWindowThreadProcessId([PSADT.GUI.UiAutomation]::GetForegroundWindow()))
                        {
                            & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Detected a fullscreen foreground window matches a PowerPoint process ID.'
                            return ($presenting = $true)
                        }
                        & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Unable to find a fullscreen foreground window that matches a PowerPoint process ID.'
                        break
                    }
                }
                return ($presenting = $false)
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        & $Script:CommandTable.'Write-ADTLogEntry' -Message "PowerPoint is running in fullscreen mode [$presenting]."
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
