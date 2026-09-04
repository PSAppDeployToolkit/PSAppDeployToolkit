#-----------------------------------------------------------------------------
#
# MARK: Test-ADTInstallationProgressOpen
#
#-----------------------------------------------------------------------------

function Private:Test-ADTInstallationProgressOpen
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Foundation.RunAsActiveUser]$RunAsActiveUser
    )
    return Invoke-ADTClientServerOperation -ProgressDialogOpen -User $RunAsActiveUser
}
