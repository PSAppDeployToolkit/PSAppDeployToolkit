function Close-ADTFluentInstallationProgress
{
    <#

    .SYNOPSIS
    Closes the dialog created by Show-ADTFluentInstallationProgress.

    .DESCRIPTION
    Closes the dialog created by Show-ADTFluentInstallationProgress.

    This function is called by the Close-ADTSession function to close a running instance of the progress dialog if found.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Close-ADTFluentInstallationProgress

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

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
