#-----------------------------------------------------------------------------
#
# MARK: Show-ADTModalDialog
#
#-----------------------------------------------------------------------------

function Private:Show-ADTModalDialog
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Security.Principal.NTAccount]$Username = (Get-ADTRunAsActiveUser -InformationAction SilentlyContinue | Select-Object -ExpandProperty NTAccount),

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

    # Get the current config, we'll need this for processing the asset permissions.
    $adtConfig = Get-ADTConfig

    # Set required permissions on this module's library files and assets first.
    $builtinUsersSid = [System.Security.Principal.SecurityIdentifier]::new([System.Security.Principal.WellKnownSidType]::BuiltinUsersSid, $null)
    $saipParams = @{ User = "*$($builtinUsersSid.Value)"; Permission = 'ReadAndExecute'; PermissionType = 'Allow'; Method = 'AddAccessRule'; InformationAction = 'SilentlyContinue' }
    Set-ADTItemPermission @saipParams -Path $Script:PSScriptRoot\lib -Inheritance ObjectInherit -Propagation InheritOnly
    Set-ADTItemPermission @saipParams -Path $adtConfig.Assets.Logo
    Set-ADTItemPermission @saipParams -Path $adtConfig.Assets.Banner

    # Serialise the incoming options.
    $optionsString = switch ($Type)
    {
        ([PSADT.UserInterface.Dialogs.DialogType]::InputDialog)
        {
            [PSADT.UserInterface.Utilities.SerializationUtilities]::SerializeToString([PSADT.UserInterface.DialogOptions.InputDialogOptions]$Options, [PSADT.UserInterface.DialogOptions.InputDialogOptions])
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::CustomDialog)
        {
            [PSADT.UserInterface.Utilities.SerializationUtilities]::SerializeToString([PSADT.UserInterface.DialogOptions.CustomDialogOptions]$Options, [PSADT.UserInterface.DialogOptions.CustomDialogOptions])
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::RestartDialog)
        {
            [PSADT.UserInterface.Utilities.SerializationUtilities]::SerializeToString([PSADT.UserInterface.DialogOptions.RestartDialogOptions]$Options, [PSADT.UserInterface.DialogOptions.RestartDialogOptions])
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

    # Farm this out to a new process.
    $null = Start-ADTProcessAsUser -Username $Username -FilePath "$Script:PSScriptRoot\lib\PSADT.UserInterface.exe" -ArgumentList "-DialogType $Type -DialogStyle $Style -DialogOptions $optionsString" -WindowStyle Hidden -NoWait:$NoWait -InformationAction SilentlyContinue
}
