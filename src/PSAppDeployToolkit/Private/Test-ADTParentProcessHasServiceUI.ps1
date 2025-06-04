#-----------------------------------------------------------------------------
#
# MARK: Test-ADTParentProcessHasServiceUI
#
#-----------------------------------------------------------------------------

function Private:Test-ADTParentProcessHasServiceUI
{
    foreach ($process in [PSADT.ProcessManagement.ProcessUtilities]::GetParentProcesses())
    {
        if ($process.ProcessName -eq 'ServiceUI')
        {
            return $true
        }
    }
    return $false
}
