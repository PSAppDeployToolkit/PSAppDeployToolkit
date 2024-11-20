#-----------------------------------------------------------------------------
#
# MARK: Test-ADTUserIsBusy
#
#-----------------------------------------------------------------------------

function Test-ADTUserIsBusy
{
    <#
    .SYNOPSIS
        Tests whether PowerPoint is running in either fullscreen slideshow mode or presentation mode, or the device's microphone is in use.

    .DESCRIPTION
        Tests whether someone is presenting using PowerPoint in either fullscreen slideshow mode or presentation mode. This function checks if the PowerPoint process has a window with a title that begins with "PowerPoint Slide Show" or "PowerPoint-" for non-English language systems. There is a possibility of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show". If the previous detection method does not detect PowerPoint in fullscreen mode, it checks if PowerPoint is in Presentation Mode (only works on Windows Vista or higher).

        Additionally, it also tests whether someone is using the microphone on their device. This could be within Teams, Zoom, a game, or any other app that uses a microphone.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true if PowerPoint is running in either fullscreen slideshow mode or presentation mode, or the device's microphone is in use; otherwise returns $false.

    .EXAMPLE
        Test-ADTUserIsBusy

        Checks if PowerPoint is running in either fullscreen slideshow mode or presentation mode, or the device's microphone is in use, and returns true or false.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
    )

    try
    {
        return ((Test-ADTMicrophoneInUse) -or (Test-ADTPowerPoint))
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
