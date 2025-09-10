#-----------------------------------------------------------------------------
#
# MARK: Get-ADTRunningProcesses
#
#-----------------------------------------------------------------------------

function Get-ADTRunningProcesses
{
    <#
    .SYNOPSIS
        Gets the processes that are running from a list of process objects.

    .DESCRIPTION
        Gets the processes that are running from a list of process objects.

    .PARAMETER ProcessObjects
        One or more process objects to search for.

    .INPUTS
        None.

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Collections.Generic.IReadOnlyList`1[[PSADT.ProcessManagement.RunningProcess]].

        Returns one or more RunningProcess objects representing each running process.

    .EXAMPLE
        Get-ADTRunningProcesses -ProcessObjects $processObjects

        Returns a list of running processes. If nothing is found nothing will be returned.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTServiceStartMode
    #>

    [CmdletBinding()]
    [OutputType([System.Collections.Generic.IReadOnlyList[PSADT.ProcessManagement.RunningProcess]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.ProcessManagement.ProcessDefinition[]]$ProcessObjects
    )

    # Process provided process objects and return any output.
    Write-ADTLogEntry -Message "Checking for running processes: ['$([System.String]::Join("', '", $ProcessObjects.Name))']"
    if (!($runningProcesses = [PSADT.ProcessManagement.ProcessUtilities]::GetRunningProcesses($ProcessObjects)))
    {
        Write-ADTLogEntry -Message 'Specified processes are not running.'
        return
    }
    Write-ADTLogEntry -Message "The following processes are running: ['$([System.String]::Join("', '", ($runningProcesses.Process.ProcessName | Select-Object -Unique)))']."
    return $runningProcesses
}
