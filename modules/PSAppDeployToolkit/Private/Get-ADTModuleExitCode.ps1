#-----------------------------------------------------------------------------
#
# MARK: Get-ADTModuleExitCode
#
#-----------------------------------------------------------------------------

function Private:Get-ADTModuleExitCode
{
    [CmdletBinding()]
    param
    (
    )

    try
    {
        return (Get-ADTModuleState).LastExitCode
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
