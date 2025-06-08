#-----------------------------------------------------------------------------
#
# MARK: Open-ADTClientServerProcess
#
#-----------------------------------------------------------------------------

function Private:Open-ADTClientServerProcess
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.TerminalServices.SessionInfo]$User = (Get-ADTRunAsActiveUser -InformationAction SilentlyContinue)
    )

    # Throw if there's already a client/server process present. This is an unexpected scenario.
    if ($Script:ADT.ClientServerProcess)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("There is already a client/server process active.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ClientServerProcessAlreadyActive'
            TargetObject = $Script:ADT.ClientServerProcess
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

    # Set the required file permissions to ensure the user can open the client/server process.
    Write-ADTLogEntry -Message 'Instantiating user client/server process.'
    Set-ADTClientServerProcessPermissions -User $User

    # Instantiate a new ClientServerProcess object as required, then add the necessary callback.
    $Script:ADT.ClientServerProcess = [PSADT.ClientServer.ServerInstance]::new($User)
    try
    {
        $Script:ADT.ClientServerProcess.Open()
    }
    catch [System.IO.InvalidDataException]
    {
        $naerParams = @{
            TargetObject = $clientResult = $Script:ADT.ClientServerProcess.GetClientProcessResult()
            Exception = [System.IO.InvalidDataException]::new("Failed to open the instantiated client/server process.$(if (!$clientResult.ExitCode.Equals([PSADT.Execution.ProcessManager]::TimeoutExitCode)) { " Exit Code: [$($clientResult.ExitCode)]." })$(if ($clientResult.StdErr) { " Error Output: [$([System.String]::Join("`n", $clientResult.StdErr))]" })", $_.Exception)
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'ClientServerProcessOpenFailure'
        }
        $Script:ADT.ClientServerProcess.Dispose()
        $Script:ADT.ClientServerProcess = $null
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    catch
    {
        $Script:ADT.ClientServerProcess.Dispose()
        $Script:ADT.ClientServerProcess = $null
        $PSCmdlet.ThrowTerminatingError($_)
    }

    # Ensure we properly close the client/server process upon the closure of the last active session.
    Add-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.'Close-ADTClientServerProcess'
}
