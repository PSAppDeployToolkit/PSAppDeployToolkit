#-----------------------------------------------------------------------------
#
# MARK: Close-ADTClientServerProcess
#
#-----------------------------------------------------------------------------

function Private:Close-ADTClientServerProcess
{
    # Dispose and nullify the client/server process if there's one in use.
    if (!$Script:ADT.ClientServerProcess)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("There is currently no client/server process active.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ClientServerProcessNull'
            TargetObject = $Script:ADT.ClientServerProcess
        }
        throw (New-ADTErrorRecord @naerParams)
    }
    if (!$Script:ADT.ClientServerProcess.IsRunning)
    {
        Write-ADTLogEntry -Message 'Closing and disposing of tombstoned client/server instance.'
    }
    else
    {
        Write-ADTLogEntry -Message 'Closing user client/server process.'
    }
    try
    {
        $Script:ADT.ClientServerProcess.Close()
        $Script:ADT.ClientServerProcess.Dispose()
    }
    finally
    {
        $Script:ADT.ClientServerProcess = $null
        Remove-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.($MyInvocation.MyCommand.Name)
    }
}
