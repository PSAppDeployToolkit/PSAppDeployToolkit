#-----------------------------------------------------------------------------
#
# MARK: Update-ADTConfigAccessiblePaths
#
#-----------------------------------------------------------------------------

function Private:Update-ADTConfigAccessiblePaths
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Config
    )

    # Change paths to user accessible ones if the caller doesn't own the configured ones.
    if (!(Test-ADTCallerOwnsConfiguredPaths -Config $Config))
    {
        if (![System.String]::IsNullOrWhiteSpace($Config.Toolkit.TempPathNoAdminRights))
        {
            $Config.Toolkit.TempPath = $Config.Toolkit.TempPathNoAdminRights
        }
        if (![System.String]::IsNullOrWhiteSpace($Config.Toolkit.RegPathNoAdminRights))
        {
            $Config.Toolkit.RegPath = $Config.Toolkit.RegPathNoAdminRights
        }
        if (![System.String]::IsNullOrWhiteSpace($Config.Toolkit.LogPathNoAdminRights))
        {
            $Config.Toolkit.LogPath = $Config.Toolkit.LogPathNoAdminRights
        }
        if (![System.String]::IsNullOrWhiteSpace($Config.Toolkit.CachePathNoAdminRights))
        {
            $Config.Toolkit.CachePath = $Config.Toolkit.CachePathNoAdminRights
        }
        if (![System.String]::IsNullOrWhiteSpace($Config.MSI.LogPathNoAdminRights))
        {
            $Config.MSI.LogPath = $Config.MSI.LogPathNoAdminRights
        }
    }

    # Append the toolkit's name onto the temporary path.
    $Config.Toolkit.TempPath = Join-Path -Path $Config.Toolkit.TempPath -ChildPath $MyInvocation.MyCommand.Module.Name
}
