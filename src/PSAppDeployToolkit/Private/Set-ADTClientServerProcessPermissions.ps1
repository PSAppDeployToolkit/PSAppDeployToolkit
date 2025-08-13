#-----------------------------------------------------------------------------
#
# MARK: Set-ADTClientServerProcessPermissions
#
#-----------------------------------------------------------------------------

function Private:Set-ADTClientServerProcessPermissions
{
    # If we're running under anything other than SYSTEM, assume we have access.
    if (![PSADT.AccountManagement.AccountUtilities]::CallerIsLocalSystem)
    {
        return
    }

    # Set required permissions on this module's library files.
    $builtinUsersSid = [PSADT.AccountManagement.AccountUtilities]::GetWellKnownSid([System.Security.Principal.WellKnownSidType]::BuiltinUsersSid)
    $saipParams = @{ User = "*$($builtinUsersSid.Value)"; Permission = [System.Security.AccessControl.FileSystemRights]::ReadAndExecute; PermissionType = 'Allow'; Method = 'AddAccessRule'; InformationAction = 'SilentlyContinue' }
    Get-ChildItem -LiteralPath $("$Script:PSScriptRoot\lib"; if (Test-ADTModuleInitialized) { ($adtConfig = Get-ADTConfig).Assets.Logo; $adtConfig.Assets.LogoDark; $adtConfig.Assets.Banner }) | & {
        process
        {
            if (!((Get-Acl -LiteralPath $_.FullName).Access | & { process { if ($_.IdentityReference.Translate([System.Security.Principal.SecurityIdentifier]).Equals($builtinUsersSid) -and ($_.FileSystemRights -band $saipParams.Permission)) { return $_ } } }))
            {
                Set-ADTItemPermission @saipParams -Path $_.FullName
            }
        }
    }
}
