#-----------------------------------------------------------------------------
#
# MARK: Get-ADTModuleData
#
#-----------------------------------------------------------------------------

function Get-ADTModuleData
{
    # When a PowerShell module is re-loaded, its cached data in incorrectly validated. This results in duplicate classes, etc.
    # The issue has been fixed in PowerShell 7.x, however this is our work around for PowerShell 5.x clients.
    # See: https://stackoverflow.com/a/42878789
    # See: https://github.com/PowerShell/PowerShell/issues/2505#issuecomment-263105859
    & (Get-Module -FullyQualifiedName @{ ModuleName = $MyInvocation.MyCommand.Module.Name; Guid = $MyInvocation.MyCommand.Module.Guid; ModuleVersion = $MyInvocation.MyCommand.Module.Version }) { $ADT }
}
