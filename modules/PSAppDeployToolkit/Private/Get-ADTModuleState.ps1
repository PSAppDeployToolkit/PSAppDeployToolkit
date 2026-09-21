#-----------------------------------------------------------------------------
#
# MARK: Get-ADTModuleState
#
#-----------------------------------------------------------------------------

function Private:Get-ADTModuleState
{
    [CmdletBinding()]
    param
    (
    )

    if (!(Test-ADTModuleInitialized))
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("Cannot retrieve the module state while the module is not initialized.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ADTModuleNotInitialized'
            RecommendedAction = "Please initialize the module with [Initialize-ADTModule] and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    return $Script:Module.State
}
