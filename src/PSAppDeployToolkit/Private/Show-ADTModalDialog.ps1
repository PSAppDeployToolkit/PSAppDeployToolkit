#-----------------------------------------------------------------------------
#
# MARK: Show-ADTModalDialog
#
#-----------------------------------------------------------------------------

function Private:Show-ADTModalDialog
{
    [CmdletBinding()]
    [OutputType([PSADT.UserInterface.DialogResults.DialogBoxResult])]
    [OutputType([PSADT.UserInterface.DialogResults.InputDialogResult])]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.TerminalServices.SessionInfo]$User = (Get-ADTRunAsActiveUser -InformationAction SilentlyContinue),

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogType]$Type,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogStyle]$Style,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Options,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait
    )

    # Instantiate a new DisplayServer object if one's not already present.
    if (!$NoWait -and !$Script:ADT.DisplayServer)
    {
        Open-ADTDisplayServer -User $User
    }

    # Switch on the dialog type.
    switch ($Type)
    {
        ([PSADT.UserInterface.Dialogs.DialogType]::DialogBox)
        {
            return $Script:ADT.DisplayServer.ShowDialogBox($Options)
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::InputDialog)
        {
            return $Script:ADT.DisplayServer.ShowInputDialog($Style, $Options)
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::CustomDialog)
        {
            return $Script:ADT.DisplayServer.ShowCustomDialog($Style, $Options)
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::RestartDialog)
        {
            return $Script:ADT.DisplayServer.ShowRestartDialog($Style, $Options)
        }
        default
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The specified dialog type [$Type] is not supported for modal display.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'InvalidModalDialog'
                TargetObject = $Type
                RecommendedAction = "Please review the specified dialog type, then try again."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }
    }
}
