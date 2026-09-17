#-----------------------------------------------------------------------------
#
# MARK: Test-ADTCallerOwnsConfiguredPaths
#
#-----------------------------------------------------------------------------

function Private:Test-ADTCallerOwnsConfiguredPaths
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Config = (Get-ADTConfig)
    )

    # Admin rights are enough to own the configured paths by default, while PathsBasedOnSystemContext narrows
    # it to LocalSystem so that an administrator's deployment doesn't write where LocalSystem's already has.
    if ($Config.Toolkit.PathsBasedOnSystemContext)
    {
        return [PSADT.AccountManagement.AccountUtilities]::CallerIsLocalSystem
    }
    return [PSADT.AccountManagement.AccountUtilities]::CallerIsAdmin
}
