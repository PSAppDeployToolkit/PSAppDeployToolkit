#-----------------------------------------------------------------------------
#
# MARK: Convert-ADTUsernameToRunAsActiveUser
#
#-----------------------------------------------------------------------------

function Private:Convert-ADTUsernameToRunAsActiveUser
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowNull()]
        [System.Security.Principal.NTAccount]$Username,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$AllowSystemFallback
    )

    # Find the correct RunAsActiveUser and return it to the caller.
    $runAsActiveUser = if ($Username)
    {
        if ($Username.Value.Contains('\'))
        {
            (Get-ADTLoggedOnUser).GetEnumerator() | & { process { if ($_.NTAccount -eq $Username) { return [PSADT.Module.RunAsActiveUser]::new($_) } } } | Select-Object -First 1
        }
        else
        {
            (Get-ADTLoggedOnUser).GetEnumerator() | & { process { if ($_.Username -eq $Username) { return [PSADT.Module.RunAsActiveUser]::new($_) } } } | Select-Object -First 1
        }
    }
    else
    {
        Get-ADTClientServerUser -AllowSystemFallback:$AllowSystemFallback
    }
    if ($runAsActiveUser)
    {
        return $runAsActiveUser
    }
}
