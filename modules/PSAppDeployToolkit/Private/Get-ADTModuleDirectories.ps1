#-----------------------------------------------------------------------------
#
# MARK: Get-ADTModuleDirectories
#
#-----------------------------------------------------------------------------

function Private:Get-ADTModuleDirectories
{
    [CmdletBinding()]
    param
    (
    )

    try
    {
        return (Get-ADTModuleState).Directories
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
