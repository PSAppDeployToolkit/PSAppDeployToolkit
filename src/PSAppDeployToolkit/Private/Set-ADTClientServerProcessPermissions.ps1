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
    try
    {
        [PSADT.ClientServer.ClientPermissions]::Remediate($User, [System.IO.FileInfo[]]$(if (Test-ADTModuleInitialized) { ($adtConfig = Get-ADTConfig).Assets.Logo; $adtConfig.Assets.LogoDark; $adtConfig.Assets.Banner }))
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
