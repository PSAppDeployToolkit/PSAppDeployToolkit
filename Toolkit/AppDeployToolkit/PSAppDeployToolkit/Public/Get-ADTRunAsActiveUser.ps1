#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Get-ADTRunAsActiveUser
{
    <#

    .NOTES
    This function can be called without an active ADT session.

    #>

    [CmdletBinding()]
    param
    (
    )

    # Determine the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account.
    # The active console user will be chosen first. Failing that, for multi-session operating systems, the first logged on user will be used instead.
    try
    {
        $SessionInfoMember = if (Test-ADTIsMultiSessionOS) {'IsCurrentSession'} else {'IsActiveUserSession'}
        return [PSADT.QueryUser]::GetUserSessionInfo().Where({$_.NTAccount -and $_.$SessionInfoMember}, 'First', 1)
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}
