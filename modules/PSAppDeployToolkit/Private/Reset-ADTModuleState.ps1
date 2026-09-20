#-----------------------------------------------------------------------------
#
# MARK: Reset-ADTModuleState
#
#-----------------------------------------------------------------------------

function Private:Reset-ADTModuleState
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force
    )

    # This can't be called when there's still a session in play.
    if ((Test-ADTSessionActive) -and !$Force)
    {
        $naerParams = @{
            Exception = [System.InvalidProgramException]::new("The module state cannot be reset while there is an active deployment session.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ModuleStateResetWhileInUse'
            TargetObject = (Get-PSCallStack)
            RecommendedAction = "Please close the active deployment session before resetting the module state and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    $Script:Module.State = $null
}
