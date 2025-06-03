#-----------------------------------------------------------------------------
#
# MARK: Open-ADTDisplayServer
#
#-----------------------------------------------------------------------------

function Private:Open-ADTDisplayServer
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.TerminalServices.SessionInfo]$User = (Get-ADTRunAsActiveUser -InformationAction SilentlyContinue)
    )

    # Instantiate a new DisplayServer object if one's not already present.
    if ($Script:ADT.DisplayServer)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("There is already a display server active.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'DisplayServerAlreadyActive'
            TargetObject = $Script:ADT.DisplayServer
        }
        throw (New-ADTErrorRecord @naerParams)
    }
    ($Script:ADT.DisplayServer = [PSADT.UserInterface.ClientServer.DisplayServer]::new($User)).Open()
    Add-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.'Close-ADTDisplayServer'
}
