#-----------------------------------------------------------------------------
#
# MARK: Test-ADTCallerIsInteractiveSystemProcess
#
#-----------------------------------------------------------------------------

function Private:Test-ADTCallerIsInteractiveSystemProcess
{
    return [PSADT.AccountManagement.AccountUtilities]::CallerIsSystemInteractive
}
