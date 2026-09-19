#-----------------------------------------------------------------------------
#
# MARK: Get-ADTClientServerInstance
#
#-----------------------------------------------------------------------------

function Private:Get-ADTClientServerInstance
{
    [CmdletBinding()]
    param
    (
    )

    if (!($clientServerInstance = Get-Variable -Name ClientServerInstance -ValueOnly -Scope Script -ErrorAction Ignore))
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("There is currently no active client/server instance.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ClientServerInstanceNotFoundError'
            RecommendedAction = "Please report this error to the PSAppDeployToolkit team."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    return $clientServerInstance
}
