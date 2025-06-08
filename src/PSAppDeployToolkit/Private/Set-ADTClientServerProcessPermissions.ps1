#-----------------------------------------------------------------------------
#
# MARK: Set-ADTClientServerProcessPermissions
#
#-----------------------------------------------------------------------------

function Private:Set-ADTClientServerProcessPermissions
{
    # If there's no active user on the device, return early.
    if (!($runAsActiveUser = Get-ADTRunAsActiveUser -InformationAction SilentlyContinue))
    {
        return
    }

    # If we're running under the active user's account, return early as the user already has access.
    $currentWindowsIdentity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    try
    {
        if ($runAsActiveUser.SID.Equals($currentWindowsIdentity.User))
        {
            return
        }
    }
    finally
    {
        $currentWindowsIdentity.Dispose()
        $currentWindowsIdentity = $null
    }

    # Initialize the module if it's not already so we can retrieve the asset paths.
    $null = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet

    # Set required permissions on this module's library and configured asset files.
    $builtinUsersSid = [System.Security.Principal.SecurityIdentifier]::new([System.Security.Principal.WellKnownSidType]::BuiltinUsersSid, $null)
    $saipParams = @{ User = "*$($builtinUsersSid.Value)"; Permission = 'ReadAndExecute'; PermissionType = 'Allow'; Method = 'AddAccessRule'; InformationAction = 'SilentlyContinue' }
    Set-ADTItemPermission @saipParams -Path $Script:PSScriptRoot\lib -Inheritance ObjectInherit -Propagation InheritOnly
    Set-ADTItemPermission @saipParams -Path ($adtConfig = Get-ADTConfig).Assets.Logo
    Set-ADTItemPermission @saipParams -Path $adtConfig.Assets.Banner
}
