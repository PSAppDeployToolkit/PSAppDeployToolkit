#-----------------------------------------------------------------------------
#
# MARK: Get-ADTModuleAssemblyPath
#
#-----------------------------------------------------------------------------

function Get-ADTModuleAssemblyPath
{
    return "$($Script:PSScriptRoot)\$((Get-ADTModuleManifest).RequiredAssemblies | & { process { if ($_.EndsWith('\PSADT.dll')) { return $_ } } } | Select-Object -First 1)"
}
