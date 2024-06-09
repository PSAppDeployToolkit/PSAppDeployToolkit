function Close-ADTModernInstallationProgress
{
    <#

    .SYNOPSIS
    Closes the dialog created by Show-ADTInstallationProgress.

    .DESCRIPTION
    Closes the dialog created by Show-ADTInstallationProgress.

    This function is called by the Close-ADTSession function to close a running instance of the progress dialog if found.

    .PARAMETER WaitingTime
    How many seconds to wait, at most, for the InstallationProgress window to be initialized, before the function returns, without closing anything. Range: 1 - 60  Default: 5

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Close-ADTModernInstallationProgress

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [ValidateRange(1, 60)]
        [System.UInt32]$WaitingTime = 5
    )

    begin {
        $adtSession = Get-ADTSession
        Write-ADTDebugHeader
    }

    process {
        # Return early if we're silent, a window wouldn't have ever opened.
        if ($adtSession.DeployModeSilent)
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.GetPropertyValue('deployMode'))]"
            return
        }

        # Dispose of the window object.
        if ($Script:ProgressWindow.Window)
        {
            Write-ADTLogEntry -Message 'Closing the installation progress dialog.'
            $Script:ProgressWindow.Window.HideDialog()
        }

        # Reset the state bool.
        $Script:ProgressWindow.Running = $false
    }

    end {
        Write-ADTDebugFooter
    }
}
