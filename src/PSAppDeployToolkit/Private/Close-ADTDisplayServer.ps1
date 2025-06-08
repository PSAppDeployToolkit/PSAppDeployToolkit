#-----------------------------------------------------------------------------
#
# MARK: Close-ADTDisplayServer
#
#-----------------------------------------------------------------------------

function Private:Close-ADTDisplayServer
{
    # Dispose and nullify the display server if there's one in use.
    if (!$Script:ADT.DisplayServer)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("There is currently no display server active.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'DisplayServerNull'
            TargetObject = $Script:ADT.DisplayServer
        }
        throw (New-ADTErrorRecord @naerParams)
    }
    Write-ADTLogEntry -Message 'Closing user interface display server.'
    try
    {
        $Script:ADT.DisplayServer.Dispose()
    }
    finally
    {
        $Script:ADT.DisplayServer = $null
        Remove-ADTModuleCallback -Hookpoint OnFinish -Callback $MyInvocation.MyCommand
    }
}
