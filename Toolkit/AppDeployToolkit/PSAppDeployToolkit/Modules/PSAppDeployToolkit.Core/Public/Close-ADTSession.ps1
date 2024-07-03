function Close-ADTSession
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.Int32]]$ExitCode
    )

    begin {
        Initialize-ADTFunction -Cmdlet $PSCmdlet
    }

    process {
        # Close the Installation Progress Dialog if running.
        if (($adtData = Get-ADT).Sessions.Count.Equals(1) -and (Get-Module -Name PSAppDeployToolkit.Dialogs))
        {
            Close-ADTInstallationProgress
        }

        # Close out the active session and clean up session state.
        ($adtSession = Get-ADTSession).Close($ExitCode)
        [System.Void]$adtData.Sessions.Remove($adtSession)

        # If this was the last session, exit out with our code.
        if (!$adtData.Sessions.Count)
        {
            exit $adtData.LastExitCode
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
