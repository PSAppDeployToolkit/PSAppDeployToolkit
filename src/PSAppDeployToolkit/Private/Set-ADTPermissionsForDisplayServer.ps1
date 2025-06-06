#-----------------------------------------------------------------------------
#
# MARK: Set-ADTPermissionsForDisplayServer
#
#-----------------------------------------------------------------------------

function Private:Set-ADTPermissionsForDisplayServer
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExcludeAssets
    )

    # Set required permissions on this module's library files first.
    $builtinUsersSid = [System.Security.Principal.SecurityIdentifier]::new([System.Security.Principal.WellKnownSidType]::BuiltinUsersSid, $null)
    $saipParams = @{ User = "*$($builtinUsersSid.Value)"; Permission = 'ReadAndExecute'; PermissionType = 'Allow'; Method = 'AddAccessRule'; InformationAction = 'SilentlyContinue' }
    Set-ADTItemPermission @saipParams -Path $Script:PSScriptRoot\lib -Inheritance ObjectInherit -Propagation InheritOnly

    # Set the permissions on assets if permitted to do so.
    if (!$ExcludeAssets)
    {
        $adtConfig = Get-ADTConfig
        Set-ADTItemPermission @saipParams -Path $adtConfig.Assets.Logo
        Set-ADTItemPermission @saipParams -Path $adtConfig.Assets.Banner
    }
}
