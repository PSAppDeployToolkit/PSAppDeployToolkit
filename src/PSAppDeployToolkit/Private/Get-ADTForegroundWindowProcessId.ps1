#-----------------------------------------------------------------------------
#
# MARK: Get-ADTForegroundWindowProcessId
#
#-----------------------------------------------------------------------------

function Private:Get-ADTForegroundWindowProcessId
{
    # Bypass if no one's logged onto the device.
    if (!($runAsActiveUser = Get-ADTClientServerUser))
    {
        Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
        return
    }
    return (Invoke-ADTClientServerOperation -GetForegroundWindowProcessId -User $runAsActiveUser)
}
