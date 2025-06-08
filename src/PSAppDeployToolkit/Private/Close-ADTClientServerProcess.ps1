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
    Write-ADTLogEntry -Message 'Closing user client/server process.'
    try
    {
        $Script:ADT.ClientServerProcess.Dispose()
    }
    finally
    {
        $Script:ADT.ClientServerProcess = $null
        Remove-ADTModuleCallback -Hookpoint OnFinish -Callback $MyInvocation.MyCommand
    }
}
