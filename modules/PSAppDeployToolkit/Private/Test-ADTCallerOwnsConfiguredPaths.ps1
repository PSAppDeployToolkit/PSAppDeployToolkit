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
    $adtEnv = Get-ADTEnvironmentTable
    if ($Config.Toolkit.PathsBasedOnSystemContext)
    {
        return $adtEnv.IsLocalSystemAccount
    }
    return $adtEnv.IsAdmin
}
