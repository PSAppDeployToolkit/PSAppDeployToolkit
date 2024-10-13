#-----------------------------------------------------------------------------
#
# MARK: Get-ADTParentProcesses
#
#-----------------------------------------------------------------------------

function Get-ADTParentProcesses
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    param
    (
    )

    # Open object to store all parents for returning. This also avoids an infinite loop situation.
    $parents = [System.Collections.Generic.List[Microsoft.Management.Infrastructure.CimInstance]]::new()

    # Get all processes from the system. WMI consistently gives us the parent on PowerShell 5.x and Core targets.
    $processes = Get-CimInstance -ClassName Win32_Process
    $process = $processes | & { process { if ($_.ProcessId -eq $PID) { return $_ } } } | Select-Object -First 1

    # Get all parents for the currently stored process.
    while ($process = $processes | & { process { if ($_.ProcessId -eq $process.ParentProcessId) { return $_ } } } | Select-Object -First 1)
    {
        if ($parents.Contains($process))
        {
            break
        }
        $parents.Add($process)
    }

    # Return all parents to the caller.
    return $parents
}
