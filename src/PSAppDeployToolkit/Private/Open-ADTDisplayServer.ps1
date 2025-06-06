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
        [PSADT.TerminalServices.SessionInfo]$User = (Get-ADTRunAsActiveUser -InformationAction SilentlyContinue),

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExcludeAssets
    )

    # Throw if there's already a display server present. This is an unexpected scenario.
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

    # Set the required file permissions to ensure the user can open the display server.
    Write-ADTLogEntry -Message 'Instantiating user interface display server.'
    Set-ADTPermissionsForDisplayServer -ExcludeAssets:$ExcludeAssets

    # Instantiate a new DisplayServer object as required, then add the necessary callback.
    ($Script:ADT.DisplayServer = [PSADT.UserInterface.ClientServer.DisplayServer]::new($User)).Open()
    Add-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.'Close-ADTDisplayServer'
}
