#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTModulePaths
{
    # Return the PSModuleInfo from this module.
    return (Get-Module -Name "$($MyInvocation.MyCommand.Module.Name)*").ModuleBase
}
