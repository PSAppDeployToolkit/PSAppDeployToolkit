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
            Exception = [System.InvalidProgramException]::new("Cannot retrieve the module state while the module is not initialized.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ADTModuleNotInitialized'
            RecommendedAction = "Please report this error to the PSAppDeployToolkit team."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    return $Script:Module.State
}
