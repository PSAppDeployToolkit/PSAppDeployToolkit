function Get-ADTModuleInfo
{
    # Return the PSModuleInfo from the parent module.
    return (Get-Module -Name $MyInvocation.MyCommand.Module.Name.Split('.')[0])
}
