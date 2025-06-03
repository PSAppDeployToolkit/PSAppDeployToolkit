#-----------------------------------------------------------------------------
#
# MARK: Set-ADTPermissionsForDisplayServer
#
#-----------------------------------------------------------------------------

function Private:Set-ADTPermissionsForDisplayServer
{
    # Get the current config, we'll need this for processing the asset permissions.
    $adtConfig = Get-ADTConfig

    # Set required permissions on this module's library files and assets first.
    $builtinUsersSid = [System.Security.Principal.SecurityIdentifier]::new([System.Security.Principal.WellKnownSidType]::BuiltinUsersSid, $null)
    $saipParams = @{ User = "*$($builtinUsersSid.Value)"; Permission = 'ReadAndExecute'; PermissionType = 'Allow'; Method = 'AddAccessRule'; InformationAction = 'SilentlyContinue' }
    Set-ADTItemPermission @saipParams -Path $Script:PSScriptRoot\lib -Inheritance ObjectInherit -Propagation InheritOnly
    Set-ADTItemPermission @saipParams -Path $adtConfig.Assets.Logo
    Set-ADTItemPermission @saipParams -Path $adtConfig.Assets.Banner
}
