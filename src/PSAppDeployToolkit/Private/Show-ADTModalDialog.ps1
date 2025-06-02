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
        ([PSADT.UserInterface.Dialogs.DialogType]::DialogBox)
        {
            [PSADT.UserInterface.Utilities.SerializationUtilities]::SerializeToString([PSADT.UserInterface.DialogOptions.DialogBoxOptions]$Options, [PSADT.UserInterface.DialogOptions.DialogBoxOptions])
        }
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
    $result = Start-ADTProcessAsUser -Username $Username -FilePath "$Script:PSScriptRoot\lib\PSADT.UserInterface.exe" -ArgumentList "/SingleDialog -DialogType $Type -DialogStyle $Style -DialogOptions $optionsString" -CreateNoWindow -NoWait:$NoWait -InformationAction SilentlyContinue -PassThru -ErrorAction SilentlyContinue
    if ($NoWait)
    {
        return
    }

    # Confirm we were successful in our operation.
    if ($result -isnot [PSADT.Execution.ProcessResult])
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("The call to the display server failed.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'DisplayServerInvocationFailure'
            TargetObject = $result
            RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    if ($result.StdErr.Count -ne 0)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new($($result.StdErr))
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'DisplayServerResultError'
            TargetObject = $result
            RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    if ($result.ExitCode -ne 0)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("The call to the display server failed with exit code [$($result.ExitCode)].")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'DisplayServerRuntimeFailure'
            TargetObject = $result
            RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    if ($result.StdOut.Count -eq 0)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("The call to the display server returned no result.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'DisplayServerResultNull'
            TargetObject = $result
            RecommendedAction = "Please raise an issue with the PSAppDeployToolkit team for further review."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

    # Return the result to the caller.
    switch ($Type)
    {
        ([PSADT.UserInterface.Dialogs.DialogType]::DialogBox)
        {
            return [PSADT.UserInterface.DialogResults.DialogBoxResult][PSADT.UserInterface.Utilities.SerializationUtilities]::DeserializeFromString($($result.StdOut), [PSADT.UserInterface.DialogResults.DialogBoxResult])
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::InputDialog)
        {
            return [PSADT.UserInterface.DialogResults.InputDialogResult][PSADT.UserInterface.Utilities.SerializationUtilities]::DeserializeFromString($($result.StdOut), [PSADT.UserInterface.DialogResults.InputDialogResult])
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::CustomDialog)
        {
            return [System.String][PSADT.UserInterface.Utilities.SerializationUtilities]::DeserializeFromString($($result.StdOut), [System.String])
        }
        ([PSADT.UserInterface.Dialogs.DialogType]::RestartDialog)
        {
            return [System.String][PSADT.UserInterface.Utilities.SerializationUtilities]::DeserializeFromString($($result.StdOut), [System.String])
        }
    }
}
