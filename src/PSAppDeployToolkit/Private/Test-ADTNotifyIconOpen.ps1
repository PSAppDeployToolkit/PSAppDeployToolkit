#-----------------------------------------------------------------------------
#
# MARK: Test-ADTNotifyIconOpen
#
#-----------------------------------------------------------------------------

function Private:Test-ADTNotifyIconOpen
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Foundation.RunAsActiveUser]$RunAsActiveUser
    )
    return Invoke-ADTClientServerOperation -NotifyIconOpen -User $RunAsActiveUser
}
