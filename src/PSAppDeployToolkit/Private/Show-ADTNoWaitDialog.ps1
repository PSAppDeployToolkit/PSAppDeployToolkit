#-----------------------------------------------------------------------------
#
# MARK: Show-ADTNoWaitDialog
#
#-----------------------------------------------------------------------------

function Private:Show-ADTNoWaitDialog
{
    [CmdletBinding()]
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
        [System.Collections.Hashtable]$Options
    )

    # Serialise the incoming options.
    $optionsString = switch ($Type)
    {
        ([PSADT.UserInterface.Dialogs.DialogType]::DialogBox)
        {
            [PSADT.Utilities.SerializationUtilities]::SerializeToString([PSADT.UserInterface.DialogOptions.DialogBoxOptions]$Options, [PSADT.UserInterface.DialogOptions.DialogBoxOptions])
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::HelpConsole)
        {
            [PSADT.Utilities.SerializationUtilities]::SerializeToString([PSADT.UserInterface.DialogOptions.HelpConsoleOptions]$Options, [PSADT.UserInterface.DialogOptions.HelpConsoleOptions])
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::InputDialog)
        {
            [PSADT.Utilities.SerializationUtilities]::SerializeToString([PSADT.UserInterface.DialogOptions.InputDialogOptions]$Options, [PSADT.UserInterface.DialogOptions.InputDialogOptions])
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::CustomDialog)
        {
            [PSADT.Utilities.SerializationUtilities]::SerializeToString([PSADT.UserInterface.DialogOptions.CustomDialogOptions]$Options, [PSADT.UserInterface.DialogOptions.CustomDialogOptions])
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::RestartDialog)
        {
            [PSADT.Utilities.SerializationUtilities]::SerializeToString([PSADT.UserInterface.DialogOptions.RestartDialogOptions]$Options, [PSADT.UserInterface.DialogOptions.RestartDialogOptions])
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

    # Ensure the permissions are correct on all files before proceeding.
    Set-ADTClientServerProcessPermissions -User $User

    # Farm this out to a new process.
    Start-ADTProcessAsUser -Username $User.NTAccount -FilePath "$Script:PSScriptRoot\lib\PSADT.ClientServer.Client.exe" -ArgumentList "/SingleDialog -DialogType $Type -DialogStyle $Style -DialogOptions $optionsString" -MsiExecWaitTime 1 -CreateNoWindow -NoWait -InformationAction SilentlyContinue
}
