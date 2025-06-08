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
    Set-ADTPermissionsForDisplayServer

    # Instantiate a new DisplayServer object as required, then add the necessary callback.
    $Script:ADT.DisplayServer = [PSADT.UserInterface.ClientServer.DisplayServer]::new($User)
    try
    {
        $Script:ADT.DisplayServer.Open()
    }
    catch [System.IO.InvalidDataException]
    {
        $naerParams = @{
            TargetObject = $clientResult = $Script:ADT.DisplayServer.GetClientProcessResult()
            Exception = [System.IO.InvalidDataException]::new("Failed to open the instantiated display server.$(if (!$clientResult.ExitCode.Equals([PSADT.Execution.ProcessManager]::TimeoutExitCode)) { " Exit Code: [$($clientResult.ExitCode)]." })$(if ($clientResult.StdErr) { " Error Output: [$([System.String]::Join("`n", $clientResult.StdErr))]" })", $_.Exception)
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'DisplayServerOpenFailure'
        }
        $Script:ADT.DisplayServer.Dispose()
        $Script:ADT.DisplayServer = $null
        throw (New-ADTErrorRecord @naerParams)
    }
    catch
    {
        $Script:ADT.DisplayServer.Dispose()
        $Script:ADT.DisplayServer = $null
        throw
    }
    Add-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.'Close-ADTDisplayServer'
}
