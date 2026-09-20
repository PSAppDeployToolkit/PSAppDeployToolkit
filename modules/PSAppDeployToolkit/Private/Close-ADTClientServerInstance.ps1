#-----------------------------------------------------------------------------
#
# MARK: Close-ADTClientServerInstance
#
#-----------------------------------------------------------------------------

function Private:Close-ADTClientServerInstance
{
    [CmdletBinding()]
    param
    (
    )

    # Dispose and nullify the client/server process if there's one in use. Rethrown from
    # here so the error reports the caller's own line rather than one inside this module.
    $clientServerInstance = try
    {
        Get-ADTClientServerInstance
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
    if (!$clientServerInstance.IsRunning)
    {
        Write-ADTLogEntry -Message 'Closing and disposing of tombstoned client/server instance.'
    }
    else
    {
        Write-ADTLogEntry -Message 'Closing user client/server instance.'
    }
    try
    {
        $null = $clientServerInstance.DisposeAsync().ConfigureAwait($false).GetAwaiter().GetResult()
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
    finally
    {
        Remove-Variable -Name ClientServerInstance -Scope Script -Force -Confirm:$false
        Remove-ADTModuleCallback -Hookpoint OnFinish -Callback (Get-ADTCommand -Name $MyInvocation.MyCommand.Name)
    }
}
