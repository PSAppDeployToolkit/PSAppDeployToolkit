#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Get-ADTModulePaths
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    param
    (
    )

    # Return the PSModuleInfo from this module.
    return (& $Script:CommandTable.'Get-Module' -Name "$($MyInvocation.MyCommand.Module.Name)*").ModuleBase
}
