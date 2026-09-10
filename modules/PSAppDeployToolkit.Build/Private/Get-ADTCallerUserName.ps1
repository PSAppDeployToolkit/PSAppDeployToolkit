#-----------------------------------------------------------------------------
#
# MARK: Get-ADTCallerUserName
#
#-----------------------------------------------------------------------------

function Get-ADTCallerUserName
{
    $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    try
    {
        return $currentUser.Name;
    }
    finally
    {
        $currentUser.Dispose()
    }
}
