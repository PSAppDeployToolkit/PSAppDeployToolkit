#-----------------------------------------------------------------------------
#
# MARK: Set-ADTClientServerProcessPermissions
#
#-----------------------------------------------------------------------------

function Private:Set-ADTClientServerProcessPermissions
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Module.RunAsActiveUser]$User
    )

    # If we're running under the active user's account, return early as the user already has access.
    if ([PSADT.AccountManagement.AccountUtilities]::CallerSid.Equals($User.SID))
    {
        return
    }

    # Set required permissions on this module's library files.
    $builtinUsersSid = [PSADT.AccountManagement.AccountUtilities]::GetWellKnownSid([System.Security.Principal.WellKnownSidType]::BuiltinUsersSid)
    $saipParams = @{ User = "*$($builtinUsersSid.Value)"; Permission = 'ReadAndExecute'; PermissionType = 'Allow'; Method = 'AddAccessRule'; InformationAction = 'SilentlyContinue' }
    Set-ADTItemPermission @saipParams -Path $Script:PSScriptRoot\lib -Inheritance ObjectInherit -Propagation InheritOnly

    # Set required permissions on the initialised assets.
    if (Test-ADTModuleInitialized)
    {
        Set-ADTItemPermission @saipParams -Path ($adtConfig = Get-ADTConfig).Assets.Logo
        Set-ADTItemPermission @saipParams -Path $adtConfig.Assets.LogoDark
        Set-ADTItemPermission @saipParams -Path $adtConfig.Assets.Banner
    }
}
