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
        [System.Security.Principal.SecurityIdentifier]$SID
    )

    # If we're running under the active user's account, return early as the user already has access.
    if ([PSADT.AccountManagement.AccountUtilities]::CallerSid.Equals($SID))
    {
        return
    }

    # Set required permissions on this module's library files.
    $saipParams = @{ User = "*$($SID.Value)"; Permission = [System.Security.AccessControl.FileSystemRights]::ReadAndExecute; PermissionType = 'Allow'; Method = 'AddAccessRule'; InformationAction = 'SilentlyContinue' }
    try
    {
        foreach ($path in (Get-ChildItem -LiteralPath $("$Script:PSScriptRoot\lib"; if (Test-ADTModuleInitialized) { ($adtConfig = Get-ADTConfig).Assets.Logo; $adtConfig.Assets.LogoDark; $adtConfig.Assets.Banner })).FullName)
        {
            if (![PSADT.FileSystem.FileSystemUtilities]::TestEffectiveAccess($path, $SID, $saipParams.Permission))
            {
                Set-ADTItemPermission @saipParams -Path $path
            }
        }
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
