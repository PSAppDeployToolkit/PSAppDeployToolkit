#-----------------------------------------------------------------------------
#
# MARK: Get-ADTForegroundWindowProcessId
#
#-----------------------------------------------------------------------------

function Private:Get-ADTForegroundWindowProcessId
{
    return (Invoke-ADTClientServerOperation -GetForegroundWindowProcessId -User (Get-ADTClientServerUser -AllowSystemFallback))
}
