#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTModulePaths
{
    # Return the PSModuleInfo from this module.
    return (& $Script:CommandTable.'Get-Module' -Name "$($MyInvocation.MyCommand.Module.Name)*").ModuleBase
}
