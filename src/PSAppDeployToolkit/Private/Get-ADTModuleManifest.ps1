#-----------------------------------------------------------------------------
#
# MARK: Get-ADTModuleManifest
#
#-----------------------------------------------------------------------------

function Get-ADTModuleManifest
{
    return (Import-LocalizedData -BaseDirectory $Script:PSScriptRoot -FileName $MyInvocation.MyCommand.Module.Name)
}
