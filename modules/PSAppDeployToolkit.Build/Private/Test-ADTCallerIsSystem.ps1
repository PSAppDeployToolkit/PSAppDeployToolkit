#-----------------------------------------------------------------------------
#
# MARK: Test-ADTCallerIsSystem
#
#-----------------------------------------------------------------------------

function Test-ADTCallerIsSystem
{
    $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    try
    {
        return $currentUser.IsSystem
    }
    finally
    {
        $currentUser.Dispose()
    }
}
