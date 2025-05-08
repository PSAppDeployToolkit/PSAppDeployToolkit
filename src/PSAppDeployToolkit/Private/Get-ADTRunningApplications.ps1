#-----------------------------------------------------------------------------
#
# MARK: Get-ADTRunningApplications
#
#-----------------------------------------------------------------------------

function Private:Get-ADTRunningApplications
{
    <#

    .SYNOPSIS
    Gets the processes that are running from a custom list of process objects.

    .DESCRIPTION
    Gets the processes that are running from a custom list of process objects.

    .PARAMETER ProcessObjects
    Custom object containing the process objects to search for.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    PSADT.Types.RunningApplication. Returns one or more RunningApplication objects representing each running application.

    .EXAMPLE
    Get-ADTRunningApplications -ProcessObjects $processObjects

    .NOTES
    This is an internal script function and should typically not be called directly.

    .NOTES
    An active ADT session is NOT required to use this function.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    [OutputType([PSADT.Types.RunningApplication])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowNull()][AllowEmptyCollection()]
        [PSADT.ProcessManagement.ProcessDefinition[]]$ProcessObjects
    )

    # Return early if we've received no input.
    if ($null -eq $ProcessObjects)
    {
        return
    }

    # Process provided process objects.
    Write-ADTLogEntry -Message "Checking for running applications: ['$([System.String]::Join("', '", ($processNames = $ProcessObjects.Name)))']"
    $runningApps = $ProcessObjects | & {
        begin
        {
            # Process the cached processes into proper process names (i.e. remove paths).
            $processNames = $processNames | & {
                process
                {
                    if ([System.IO.Path]::IsPathRooted($_))
                    {
                        return [System.IO.Path]::GetFileNameWithoutExtension($_)
                    }
                    return $_
                }
            }

            # Cache all running processes.
            $allProcesses = Get-Process -Name $processNames -ErrorAction Ignore

            # Cache process info from WMI as it gives us our command line and associated arguments.
            $wmiProcesses = Get-CimInstance -ClassName Win32_Process -Verbose:$false

            # Member lookup table.
            $member = 'Name', 'Path'
        }

        process
        {
            # Get all processes that matches the object and add in extra properties.
            $processes = foreach ($process in $allProcesses)
            {
                # Continue if this isn't our process or it's ended since we cached it.
                if (($process.($member[[System.IO.Path]::IsPathRooted($_.Name)]) -notlike $_.Name) -or (!$process.Refresh() -and $process.HasExited))
                {
                    continue
                }

                # Calculate a description for the running application.
                $appDesc = if (![System.String]::IsNullOrWhiteSpace($_.Description))
                {
                    # The description of the process provided with the object.
                    $_.Description
                }
                elseif (![System.String]::IsNullOrWhiteSpace($process.Description))
                {
                    # If the process already has a description field specified, then use it.
                    $process.Description
                }
                else
                {
                    # Fall back on the process name if no description is provided by the process or as a parameter to the function.
                    $process.ProcessName
                }

                # Output a RunningApplication object for collection.
                [PSADT.Types.RunningApplication]::new(
                    $process,
                    ($wmiProcess = $wmiProcesses | & { process { if ($_.ProcessId -eq $process.Id) { return $_ } } } | Select-Object -First 1),
                    $appDesc,
                    $wmiProcess.CommandLine.Trim(),
                    $wmiProcess.CommandLine.Replace($wmiProcess.ExecutablePath, $null).TrimStart('"').Trim()
                )
            }

            # Return early if we have nothing.
            if (!$processes)
            {
                return
            }

            # Return filtered list of processes if there is one.
            if ($null -eq $_.Filter)
            {
                return $processes
            }
            return $processes | Where-Object -FilterScript $_.Filter
        }
    }

    # Return output if there's any.
    if ($runningApps)
    {
        Write-ADTLogEntry -Message "The following processes are running: [$(($runningApps.Process.ProcessName | Select-Object -Unique) -join ',')]."
        return ($runningApps | Sort-Object -Property Description)
    }
    Write-ADTLogEntry -Message 'Specified applications are not running.'
}
