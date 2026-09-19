#-----------------------------------------------------------------------------
#
# MARK: Get-ADTDeploymentSessions
#
#-----------------------------------------------------------------------------

function Private:Get-ADTDeploymentSessions
{
    [CmdletBinding()]
    param
    (
    )

    try
    {
        $PSCmdlet.WriteObject((Get-ADTModuleState).Sessions, $false)
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
